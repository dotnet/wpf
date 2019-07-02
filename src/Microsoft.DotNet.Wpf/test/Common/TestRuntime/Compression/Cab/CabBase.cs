// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if CABMINIMAL
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.MiniExtract
#else
namespace Microsoft.Test.Compression.Cab.Mini
#endif
#else
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.Extract
#else
namespace Microsoft.Test.Compression.Cab
#endif
#endif
{
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Security;
using System.Collections;
using System.Runtime.InteropServices;

using Handle = System.Int32;
using Pointer = System.IntPtr;


/// <summary>
/// Provides basic implementations of cab I/O functions:
///		alloc, free, open, read, write, close, seek, delete, gettemp,
///		dos datetime conversions.
///	Also includes members for callbacks that apply to both creation and extraction.
/// </summary>
internal abstract class CabBase : IDisposable
{
	protected HandleManager         streamHandles;
	protected Stream                fileStream;
	protected Stream                cabStream;

	protected Exception             abortException;
	protected ERF                   erf;

	internal  CabinetStatus         status;
	#if !CABMINIMAL
	internal  CabinetStatusCallback statusCallback;
	internal  object                statusContext;
	#endif

	internal CabinetOpenCabHandler          openCabHandler;
	internal CabinetCloseCabHandler         closeCabHandler;
	internal object                         openCabContext;
	internal CabinetFilterFileHandler       filterFileHandler;
	internal object                         filterFileContext;

	private byte[] buf;

	protected CabBase()
	{
		this.streamHandles = new HandleManager();
		this.erf = new ERF();
		this.status = new CabinetStatus();

		// 32K seems to be the size of the largest chunks processed by cabinet.dll.
		// But just in case, this buffer will auto-enlarge.
		this.buf = new byte[32768];
	}

	~CabBase()
	{
		Dispose(false);
	}

	protected virtual void Dispose(bool disposing) 
	{
		if(disposing) 
		{
			// Dispose managed objects.
			if(this.erf != null)
			{
				this.erf.Dispose();
				this.erf = null;
			}
		}

		// Dispose unmanaged objects.
	}

	public void Dispose() 
	{
		Dispose(true);
		GC.SuppressFinalize(this); 
	}

	protected Pointer CabAllocMem(uint byteCount)
	{
		try
		{
			Pointer memPointer = Marshal.AllocHGlobal((IntPtr) byteCount);
			return memPointer;
		}
		catch(OutOfMemoryException)
		{
			return Pointer.Zero;
		}
	}

	protected void CabFreeMem(Pointer memPointer)
	{
		Marshal.FreeHGlobal(memPointer);
	}

	protected Handle CabOpenStream(string path, int openFlags, int shareMode)
	{
		int err; return CabOpenStreamEx(path, openFlags, shareMode, out err, Pointer.Zero);
	}
	protected virtual Handle CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, Pointer pv)
	{
		try
		{
			Stream stream = null;
			path = path.Trim();
			if(path == CabBase.STREAM_CAB)
			{
				stream = this.cabStream;
				this.cabStream = new DuplicateStream(stream);
			}
			else if(path == CabBase.STREAM_FILE)
			{
				stream = this.fileStream;
				this.fileStream = new DuplicateStream(stream);
			}
			#if !CABEXTRACTONLY
			else if(path == CabBase.STREAM_TEMP)
			{
				// Opening memory stream for a temp file.
				stream = new MemoryStream();
				this.tempStreams.Add(stream);
			}
			else
			{
				// Opening a file on disk for a temp file.
				path = Path.Combine(Path.GetTempPath(), path);
				stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
				this.tempStreams.Add(stream);
				// TODO: figure out why this breaks if not wrapped in a DuplicateStream
				stream = new DuplicateStream(stream);
			}
			#endif
			Handle hStream = streamHandles.AllocHandle(stream);
			err = 0;
			return hStream;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return (Handle)0;
		}
	}

	protected uint CabReadStream(Handle hStream, Pointer memory, uint cb)
	{
		int err; return CabReadStreamEx(hStream, memory, cb, out err, Pointer.Zero);
	}
	protected virtual uint CabReadStreamEx(Handle hStream, Pointer memory, uint cb, out int err, Pointer pv)
	{
		try
		{
			Stream stream = (Stream) streamHandles[hStream];
			int count = (int) cb;
			if(count > buf.Length) buf = new byte[count];
			count = stream.Read(buf, 0, count);
			Marshal.Copy(buf, 0, memory, count);
			err = 0;
			return (uint) count;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return 0;
		}
	}

	protected uint CabWriteStream(Handle hStream, Pointer memory, uint cb)
	{
		int err; return CabWriteStreamEx(hStream, memory, cb, out err, Pointer.Zero);
	}
    protected virtual uint CabWriteStreamEx(Handle hStream, Pointer memory, uint cb, out int err, Pointer pv)
	{
		try
		{
			Stream stream = (Stream) streamHandles[hStream];
			int count = (int) cb;
			if(count == 0)
			{
				// FdiTruncate expectes write(0) to truncate the file.
				stream.SetLength(stream.Position);
			}
			else
			{
				if(count > buf.Length) buf = new byte[count];
				Marshal.Copy(memory, buf, 0, count);
				stream.Write(buf, 0, count);
			}
			err = 0;
			return cb;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return 0;
		}
	}

	protected int CabCloseStream(Handle hStream)
	{
        int err; return CabCloseStreamEx(hStream, out err, Pointer.Zero);
	}
    protected virtual int CabCloseStreamEx(Handle hStream, out int err, Pointer pv)
	{
		try
		{
			Stream stream = (Stream) streamHandles[hStream];
			if(stream is DuplicateStream) stream = ((DuplicateStream) stream).Source;

			if(stream != null)
			{
				if(stream.CanWrite) stream.Flush();
				streamHandles.FreeHandle(hStream);

				if(this.fileStream == stream ||
					(this.fileStream is DuplicateStream && ((DuplicateStream) this.fileStream).Source == stream))
				{
					stream.Close();
					this.fileStream = null;
				}
				#if !CABEXTRACTONLY
				else if(this.tempStreams.Contains(stream))
				{
					stream.Close();
					this.tempStreams.Remove(stream);
				}
				#endif
			}
			err = 0;
			return 0;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return 0;
		}
	}

	protected int CabSeekStream(Handle hStream, int offset, int seekOrigin)
	{
        int err; return CabSeekStreamEx(hStream, offset, seekOrigin, out err, Pointer.Zero);
	}
    protected virtual int CabSeekStreamEx(Handle hStream, int offset, int seekOrigin, out int err, Pointer pv)
	{
		try
		{
			Stream stream = (Stream) streamHandles[hStream];
			offset = (int) stream.Seek(offset, (SeekOrigin) seekOrigin);
			err = 0;
			return offset;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return -1;
		}
	}

	protected const string STREAM_CAB  = "%%CAB%%";
	protected const string STREAM_FILE = "%%FILE%%";

#if !CABEXTRACTONLY

	protected const string STREAM_TEMP = "%%TEMP%%";
	private ArrayList tempStreams = new ArrayList();

    protected virtual int CabDeleteFile(string path, out int err, Pointer pv)
	{
		try
		{
			// Deleting a temp file - don't bother if it is only a memory stream.
			if(path != CabBase.STREAM_TEMP)
			{
				path = Path.Combine(Path.GetTempPath(), path);
				File.Delete(path);
			}
		}
		catch
		{
			// Failure to delete a temp file is not fatal.
		}
		err = 0;
		return 1;
	}

    protected virtual int CabGetTempFile(Pointer pTempName, int cbTempName, Pointer pv)
	{
		try
		{
			string tempFileName = CabBase.STREAM_TEMP;
			try
			{
				tempFileName = Path.GetFileName(Path.GetTempFileName());
			}
			catch(IOException) { }
			catch(SecurityException) { }
	
			byte[] tempNameBytes = Encoding.ASCII.GetBytes(tempFileName);
			Marshal.Copy(tempNameBytes, 0, pTempName, tempNameBytes.Length);
			Marshal.WriteByte(pTempName, tempNameBytes.Length, 0);  // null-terminator
			return 1;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			return 0;
		}
	}
	#endif // !CABEXTRACTONLY
}
}
