using System;
using System.Security.Principal;

namespace RVTools.LoginSSPI.Security;

internal class SspiHelper
{
	private const int MAX_TOKEN_SIZE = 12288;

	public const int STANDARD_CONTEXT_ATTRIBUTES = 2076;

	public const int TOKEN_QUERY = 8;

	private readonly string _sAccountName;

	private bool _bGotClientCredentials;

	private bool _bGotServerContext;

	private bool _bGotServerCredentials;

	private SECURITY_HANDLE _hClientContext = new SECURITY_HANDLE(0);

	private SECURITY_HANDLE _hInboundCred = new SECURITY_HANDLE(0);

	private SECURITY_HANDLE _hOutboundCred = new SECURITY_HANDLE(0);

	private SECURITY_HANDLE _hServerContext = new SECURITY_HANDLE(0);

	public SspiHelper()
	{
		WindowsIdentity current = WindowsIdentity.GetCurrent();
		if (current != null)
		{
			_sAccountName = current.Name;
		}
	}

	public SspiHelper(string sRemotePrincipal)
	{
		_sAccountName = sRemotePrincipal;
	}

	public void InitializeClient(out byte[] clientToken, byte[] serverToken, out bool bContinueProcessing, SspiPackageType sspiPackageType = SspiPackageType.Negotiate)
	{
		clientToken = null;
		bContinueProcessing = true;
		SECURITY_INTEGER ptsExpiry = new SECURITY_INTEGER(0);
		if (!_bGotClientCredentials)
		{
			int num = NativeMethods.AcquireCredentialsHandle(_sAccountName, sspiPackageType.ToString(), 2, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, ref _hOutboundCred, ref ptsExpiry);
			if (num != 0)
			{
				throw new SspiException("Couldn't acquire client credentials", num);
			}
			_bGotClientCredentials = true;
		}
		SecBufferDesc pOutput = new SecBufferDesc(12288);
		int num2;
		try
		{
			uint pfContextAttr;
			if (serverToken == null)
			{
				num2 = NativeMethods.InitializeSecurityContext(ref _hOutboundCred, IntPtr.Zero, _sAccountName, 2076, 0, 16, IntPtr.Zero, 0, out _hClientContext, out pOutput, out pfContextAttr, out ptsExpiry);
			}
			else
			{
				SecBufferDesc secBufferDesc = new SecBufferDesc(serverToken);
				try
				{
					num2 = NativeMethods.InitializeSecurityContext(ref _hOutboundCred, ref _hClientContext, _sAccountName, 2076, 0, 16, ref secBufferDesc, 0, out _hClientContext, out pOutput, out pfContextAttr, out ptsExpiry);
				}
				finally
				{
					secBufferDesc.Dispose();
				}
			}
			if (num2 != 0 && num2 != 590610)
			{
				throw new SspiException("InitializeSecurityContext() failed!!!", num2);
			}
			clientToken = pOutput.GetSecBufferByteArray();
		}
		finally
		{
			pOutput.Dispose();
		}
		bContinueProcessing = num2 != 0;
	}

	public void InitializeServer(byte[] clientToken, out byte[] serverToken, out bool bContinueProcessing, SspiPackageType sspiPackageType = SspiPackageType.Negotiate)
	{
		serverToken = null;
		bContinueProcessing = true;
		SECURITY_INTEGER ptsExpiry = new SECURITY_INTEGER(0);
		if (!_bGotServerCredentials)
		{
			int num = NativeMethods.AcquireCredentialsHandle(_sAccountName, sspiPackageType.ToString(), 1, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, ref _hInboundCred, ref ptsExpiry);
			if (num != 0)
			{
				throw new SspiException("Couldn't acquire server credentials handle!!!", num);
			}
			_bGotServerCredentials = true;
		}
		SecBufferDesc pOutput = new SecBufferDesc(12288);
		SecBufferDesc pInput = new SecBufferDesc(clientToken);
		try
		{
			int num2 = (_bGotServerContext ? NativeMethods.AcceptSecurityContext(ref _hInboundCred, ref _hServerContext, ref pInput, 2076u, 16u, out _hServerContext, out pOutput, out var pfContextAttr, out ptsExpiry) : NativeMethods.AcceptSecurityContext(ref _hInboundCred, IntPtr.Zero, ref pInput, 2076u, 16u, out _hServerContext, out pOutput, out pfContextAttr, out ptsExpiry));
			if (num2 != 0 && num2 != 590610)
			{
				throw new SspiException("AcceptSecurityContext() failed!!!", num2);
			}
			if (!_bGotServerContext)
			{
				_bGotServerContext = true;
			}
			serverToken = pOutput.GetSecBufferByteArray();
			bContinueProcessing = num2 != 0;
		}
		finally
		{
			pInput.Dispose();
			pOutput.Dispose();
		}
	}

	public void EncryptMessage(byte[] message, bool bUseClientContext, out byte[] encryptedBuffer)
	{
		encryptedBuffer = null;
		SECURITY_HANDLE phContext = _hServerContext;
		if (bUseClientContext)
		{
			phContext = _hClientContext;
		}
		int num = NativeMethods.QueryContextAttributes(ref phContext, 0u, out var pContextAttributes);
		if (num != 0)
		{
			throw new SspiException("QueryContextAttribute() failed!!!", num);
		}
		SecBufferDesc pMessage = new SecBufferDesc(new MultipleSecBufferHelper[2]
		{
			new MultipleSecBufferHelper(message, SecBufferType.SECBUFFER_DATA),
			new MultipleSecBufferHelper(new byte[pContextAttributes.cbSecurityTrailer], SecBufferType.SECBUFFER_TOKEN)
		});
		try
		{
			num = NativeMethods.EncryptMessage(ref phContext, 0u, ref pMessage, 0u);
			if (num != 0)
			{
				throw new SspiException("EncryptMessage() failed!!!", num);
			}
			encryptedBuffer = pMessage.GetSecBufferByteArray();
		}
		finally
		{
			pMessage.Dispose();
		}
	}

	public void DecryptMessage(int messageLength, byte[] encryptedBuffer, bool bUseClientContext, out byte[] decryptedBuffer)
	{
		decryptedBuffer = null;
		SECURITY_HANDLE phContext = _hServerContext;
		if (bUseClientContext)
		{
			phContext = _hClientContext;
		}
		byte[] array = new byte[messageLength];
		Array.Copy(encryptedBuffer, 0, array, 0, messageLength);
		int num = encryptedBuffer.Length - messageLength;
		byte[] array2 = new byte[num];
		Array.Copy(encryptedBuffer, messageLength, array2, 0, num);
		SecBufferDesc pMessage = new SecBufferDesc(new MultipleSecBufferHelper[2]
		{
			new MultipleSecBufferHelper(array, SecBufferType.SECBUFFER_DATA),
			new MultipleSecBufferHelper(array2, SecBufferType.SECBUFFER_TOKEN)
		});
		try
		{
			int num2 = NativeMethods.DecryptMessage(ref phContext, ref pMessage, 0u, out var _);
			if (num2 != 0)
			{
				throw new SspiException("DecryptMessage() failed!!!", num2);
			}
			decryptedBuffer = new byte[messageLength];
			Array.Copy(pMessage.GetSecBufferByteArray(), 0, decryptedBuffer, 0, messageLength);
		}
		finally
		{
			pMessage.Dispose();
		}
	}

	public void SignMessage(byte[] message, bool bUseClientContext, out byte[] signedBuffer, ref SECURITY_HANDLE hServerContext)
	{
		signedBuffer = null;
		SECURITY_HANDLE phContext = _hServerContext;
		if (bUseClientContext)
		{
			phContext = _hClientContext;
		}
		int num = NativeMethods.QueryContextAttributes(ref phContext, 0u, out var pContextAttributes);
		if (num != 0)
		{
			throw new SspiException("QueryContextAttribute() failed!!!", num);
		}
		SecBufferDesc pMessage = new SecBufferDesc(new MultipleSecBufferHelper[2]
		{
			new MultipleSecBufferHelper(message, SecBufferType.SECBUFFER_DATA),
			new MultipleSecBufferHelper(new byte[pContextAttributes.cbMaxSignature], SecBufferType.SECBUFFER_TOKEN)
		});
		try
		{
			num = NativeMethods.MakeSignature(ref phContext, 0u, ref pMessage, 0u);
			if (num != 0)
			{
				throw new SspiException("MakeSignature() failed!!!", num);
			}
			NativeMethods.VerifySignature(ref _hServerContext, ref pMessage, 0u, out var _);
			signedBuffer = pMessage.GetSecBufferByteArray();
		}
		finally
		{
			pMessage.Dispose();
		}
	}

	public void VerifyMessage(int messageLength, byte[] signedBuffer, bool bUseClientContext, out byte[] verifiedBuffer)
	{
		verifiedBuffer = null;
		SECURITY_HANDLE phContext = _hServerContext;
		if (bUseClientContext)
		{
			phContext = _hClientContext;
		}
		byte[] array = new byte[messageLength];
		Array.Copy(signedBuffer, 0, array, 0, messageLength);
		int num = signedBuffer.Length - messageLength;
		byte[] array2 = new byte[num];
		Array.Copy(signedBuffer, messageLength, array2, 0, num);
		SecBufferDesc pMessage = new SecBufferDesc(new MultipleSecBufferHelper[2]
		{
			new MultipleSecBufferHelper(array, SecBufferType.SECBUFFER_DATA),
			new MultipleSecBufferHelper(array2, SecBufferType.SECBUFFER_TOKEN)
		});
		try
		{
			int num2 = NativeMethods.VerifySignature(ref phContext, ref pMessage, 0u, out var _);
			if (num2 != 0)
			{
				throw new SspiException("VerifySignature() failed!!!", num2);
			}
			verifiedBuffer = new byte[messageLength];
			Array.Copy(pMessage.GetSecBufferByteArray(), 0, verifiedBuffer, 0, messageLength);
		}
		finally
		{
			pMessage.Dispose();
		}
	}
}
