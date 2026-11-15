using System;
using System.Runtime.InteropServices;

namespace RVTools.LoginSSPI.Security;

internal static class NativeMethods
{
	[DllImport("secur32", CharSet = CharSet.Unicode)]
	internal static extern int AcquireCredentialsHandle(string pszPrincipal, string pszPackage, int fCredentialUse, IntPtr pAuthenticationId, IntPtr pAuthData, int pGetKeyFn, IntPtr pvGetKeyArgument, ref SECURITY_HANDLE phCredential, ref SECURITY_INTEGER ptsExpiry);

	[DllImport("secur32", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern int InitializeSecurityContext(ref SECURITY_HANDLE phCredential, IntPtr phContext, string pszTargetName, int fContextReq, int reserved1, int targetDataRep, IntPtr pInput, int reserved2, out SECURITY_HANDLE phNewContext, out SecBufferDesc pOutput, out uint pfContextAttr, out SECURITY_INTEGER ptsExpiry);

	[DllImport("secur32", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern int InitializeSecurityContext(ref SECURITY_HANDLE phCredential, ref SECURITY_HANDLE phContext, string pszTargetName, int fContextReq, int reserved1, int targetDataRep, ref SecBufferDesc secBufferDesc, int reserved2, out SECURITY_HANDLE phNewContext, out SecBufferDesc pOutput, out uint pfContextAttr, out SECURITY_INTEGER ptsExpiry);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int AcceptSecurityContext(ref SECURITY_HANDLE phCredential, IntPtr phContext, ref SecBufferDesc pInput, uint fContextReq, uint targetDataRep, out SECURITY_HANDLE phNewContext, out SecBufferDesc pOutput, out uint pfContextAttr, out SECURITY_INTEGER ptsTimeStamp);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int AcceptSecurityContext(ref SECURITY_HANDLE phCredential, ref SECURITY_HANDLE phContext, ref SecBufferDesc pInput, uint fContextReq, uint targetDataRep, out SECURITY_HANDLE phNewContext, out SecBufferDesc pOutput, out uint pfContextAttr, out SECURITY_INTEGER ptsTimeStamp);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int ImpersonateSecurityContext(ref SECURITY_HANDLE phContext);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int QueryContextAttributes(ref SECURITY_HANDLE phContext, uint ulAttribute, out SecPkgContext_Sizes pContextAttributes);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int EncryptMessage(ref SECURITY_HANDLE phContext, uint fQop, ref SecBufferDesc pMessage, uint messageSeqNo);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int DecryptMessage(ref SECURITY_HANDLE phContext, ref SecBufferDesc pMessage, uint messageSeqNo, out uint pfQop);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int MakeSignature(ref SECURITY_HANDLE phContext, uint fQop, ref SecBufferDesc pMessage, uint messageSeqNo);

	[DllImport("secur32.Dll", CharSet = CharSet.Auto)]
	internal static extern int VerifySignature(ref SECURITY_HANDLE phContext, ref SecBufferDesc pMessage, uint messageSeqNo, out uint pfQop);
}
