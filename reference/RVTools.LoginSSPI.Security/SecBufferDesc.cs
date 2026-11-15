using System;
using System.Runtime.InteropServices;

namespace RVTools.LoginSSPI.Security;

public struct SecBufferDesc : IDisposable
{
	public int ulVersion;

	public int cBuffers;

	public IntPtr pBuffers;

	public SecBufferDesc(int bufferSize)
	{
		ulVersion = 0;
		cBuffers = 1;
		SecBuffer structure = new SecBuffer(bufferSize);
		pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, pBuffers, fDeleteOld: false);
	}

	public SecBufferDesc(byte[] secBufferBytes)
	{
		ulVersion = 0;
		cBuffers = 1;
		SecBuffer structure = new SecBuffer(secBufferBytes);
		pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, pBuffers, fDeleteOld: false);
	}

	public SecBufferDesc(MultipleSecBufferHelper[] secBufferBytesArray)
	{
		if (secBufferBytesArray == null || secBufferBytesArray.Length == 0)
		{
			throw new ArgumentException("secBufferBytesArray cannot be null or 0 length");
		}
		ulVersion = 0;
		cBuffers = secBufferBytesArray.Length;
		pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SecBuffer)) * cBuffers);
		for (int i = 0; i < secBufferBytesArray.Length; i++)
		{
			SecBuffer secBuffer = new SecBuffer(secBufferBytesArray[i].Buffer, secBufferBytesArray[i].BufferType);
			int num = i * Marshal.SizeOf(typeof(SecBuffer));
			Marshal.WriteInt32(pBuffers, num, secBuffer.cbBuffer);
			Marshal.WriteInt32(pBuffers, num + Marshal.SizeOf(secBuffer.cbBuffer), secBuffer.BufferType);
			Marshal.WriteIntPtr(pBuffers, num + Marshal.SizeOf(secBuffer.cbBuffer) + Marshal.SizeOf(secBuffer.BufferType), secBuffer.pvBuffer);
		}
	}

	public void Dispose()
	{
		if (!(pBuffers != IntPtr.Zero))
		{
			return;
		}
		if (cBuffers == 1)
		{
			((SecBuffer)Marshal.PtrToStructure(pBuffers, typeof(SecBuffer))).Dispose();
		}
		else
		{
			for (int i = 0; i < cBuffers; i++)
			{
				int num = i * Marshal.SizeOf(typeof(SecBuffer));
				Marshal.FreeHGlobal(Marshal.ReadIntPtr(pBuffers, num + Marshal.SizeOf(typeof(int)) + Marshal.SizeOf(typeof(int))));
			}
		}
		Marshal.FreeHGlobal(pBuffers);
		pBuffers = IntPtr.Zero;
	}

	public byte[] GetSecBufferByteArray()
	{
		byte[] array = null;
		if (pBuffers == IntPtr.Zero)
		{
			throw new InvalidOperationException("Object has already been disposed!!!");
		}
		if (cBuffers == 1)
		{
			SecBuffer secBuffer = (SecBuffer)Marshal.PtrToStructure(pBuffers, typeof(SecBuffer));
			if (secBuffer.cbBuffer > 0)
			{
				array = new byte[secBuffer.cbBuffer];
				Marshal.Copy(secBuffer.pvBuffer, array, 0, secBuffer.cbBuffer);
			}
		}
		else
		{
			int num = 0;
			for (int i = 0; i < cBuffers; i++)
			{
				int ofs = i * Marshal.SizeOf(typeof(SecBuffer));
				num += Marshal.ReadInt32(pBuffers, ofs);
			}
			array = new byte[num];
			int j = 0;
			int num2 = 0;
			for (; j < cBuffers; j++)
			{
				int num3 = j * Marshal.SizeOf(typeof(SecBuffer));
				int num4 = Marshal.ReadInt32(pBuffers, num3);
				Marshal.Copy(Marshal.ReadIntPtr(pBuffers, num3 + Marshal.SizeOf(typeof(int)) + Marshal.SizeOf(typeof(int))), array, num2, num4);
				num2 += num4;
			}
		}
		return array;
	}
}
