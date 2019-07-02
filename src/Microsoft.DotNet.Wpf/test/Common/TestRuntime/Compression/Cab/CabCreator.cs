// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#if !CABEXTRACTONLY

#if CABMINIMAL
namespace Microsoft.Test.Compression.Cab.Mini
#else
namespace Microsoft.Test.Compression.Cab
#endif
{
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using Handle = System.Int32;
using Pointer = System.IntPtr;


/// <summary>
/// Creates cabinet files.
/// </summary>
internal class CabCreator : CabBase
{
	private  Pointer   fciHandle;
	internal FCI.CCAB ccab;

	private  FCI.PFNALLOC fciAllocMemHandler;
	private  FCI.PFNFREE  fciFreeMemHandler;
	private  FCI.PFNOPEN  fciOpenStreamHandler;
	private  FCI.PFNREAD  fciReadStreamHandler;
	private  FCI.PFNWRITE fciWriteStreamHandler;
	private  FCI.PFNCLOSE fciCloseStreamHandler;
	private  FCI.PFNSEEK  fciSeekStreamHandler;
	internal FCI.PFNFILEPLACED     fciFilePlacedHandler;
	internal FCI.PFNDELETE         fciDeleteFileHandler;
	internal FCI.PFNGETTEMPFILE    fciGetTempFileHandler;
	internal FCI.PFNGETOPENINFO    fciGetOpenInfoHandler;
	internal FCI.PFNGETNEXTCABINET fciGetNextCabinetHandler;
	internal FCI.PFNSTATUS         fciStatusHandler;

	internal CabinetCreateOpenFileHandler   openFileHandler;
	internal CabinetCreateCloseFileHandler  closeFileHandler;
	internal object                         openFileContext;
	internal CabinetNameHandler             nameHandler;
	internal object                         nameContext;

	internal long maxCabSize;
	internal long maxFolderSize;

	private FileAttributes fileAttributes;
	private DateTime       fileLastWriteTime;

	internal IDictionary    cabNumbers;

	internal CabCreator() : this(0, 0) { }
	internal CabCreator(long maxCabSize, long maxFolderSize) : base()
	{
		this.fciAllocMemHandler       = new FCI.PFNALLOC(this.CabAllocMem);
		this.fciFreeMemHandler        = new FCI.PFNFREE (this.CabFreeMem);
		this.fciOpenStreamHandler     = new FCI.PFNOPEN (this.CabOpenStreamEx);
		this.fciReadStreamHandler     = new FCI.PFNREAD (this.CabReadStreamEx);
		this.fciWriteStreamHandler    = new FCI.PFNWRITE(this.CabWriteStreamEx);
		this.fciCloseStreamHandler    = new FCI.PFNCLOSE(this.CabCloseStreamEx);
		this.fciSeekStreamHandler     = new FCI.PFNSEEK (this.CabSeekStreamEx);
		this.fciFilePlacedHandler     = new FCI.PFNFILEPLACED(this.CabFilePlaced);
		this.fciGetOpenInfoHandler    = new FCI.PFNGETOPENINFO(this.CabGetOpenInfo);
		this.fciGetNextCabinetHandler = new FCI.PFNGETNEXTCABINET(this.CabGetNextCabinet);
		this.fciStatusHandler         = new FCI.PFNSTATUS(this.CabCreateStatus);
		this.fciDeleteFileHandler     = new FCI.PFNDELETE(this.CabDeleteFile);
		this.fciGetTempFileHandler    = new FCI.PFNGETTEMPFILE(this.CabGetTempFile);

		this.cabStream = null;
		this.cabNumbers = new Hashtable(1);

		this.ccab = new FCI.CCAB();
		if(maxCabSize > 0 && maxCabSize < ccab.cb) ccab.cb = (uint) maxCabSize;
		if(maxFolderSize > 0 && maxFolderSize < ccab.cbFolderThresh) ccab.cbFolderThresh = (uint) maxFolderSize;
		this.maxCabSize = ccab.cb;
		this.maxFolderSize = ccab.cbFolderThresh;

		this.fciHandle = FCI.Create(this.erf, this.fciFilePlacedHandler, this.fciAllocMemHandler,
			this.fciFreeMemHandler, this.fciOpenStreamHandler, this.fciReadStreamHandler,
			this.fciWriteStreamHandler, this.fciCloseStreamHandler, this.fciSeekStreamHandler,
            this.fciDeleteFileHandler, this.fciGetTempFileHandler, ccab, Pointer.Zero);
		if(this.erf.fError)
		{
			int error = this.erf.erfOper;
			int errorCode = this.erf.erfType;
			this.erf.Dispose();
			this.erf = null;
            this.fciHandle = Pointer.Zero;
			throw new CabinetCreateException(error, errorCode, this.abortException);
		}
	}

	protected override void Dispose(bool disposing) 
	{
		if(disposing) 
		{
			// Dispose managed objects.
		}

		// Dispose unmanaged objects.
        if (this.fciHandle != Pointer.Zero)
		{
			FCI.Destroy(this.fciHandle);
			this.erf.Dispose();
			this.erf = null;
			this.cabStream = null;
			this.fileStream = null;
            this.fciHandle = Pointer.Zero;
		}
	}

	internal static FCI.TCOMP GetCompressionType(CabinetCompressionLevel compLevel)
	{
		if(compLevel == CabinetCompressionLevel.None)
		{
			return FCI.TCOMP.TYPE_NONE;
		}
		else
		{
			if(compLevel < CabinetCompressionLevel.None) compLevel = CabinetCompressionLevel.None;
			if(compLevel > CabinetCompressionLevel.Max ) compLevel = CabinetCompressionLevel.Max;
			return (FCI.TCOMP) ((int) FCI.TCOMP.TYPE_LZX | ((int) FCI.TCOMP.LZX_WINDOW_LO +
				((compLevel - CabinetCompressionLevel.Min) << (int) FCI.TCOMP.SHIFT_LZX_WINDOW)));
		}
	}

	internal void AddFile(string name, Stream stream, FileAttributes attributes, DateTime lastWriteTime,
		bool execute, CabinetCompressionLevel compLevel)
	{
		this.fileStream = stream;
        this.fileAttributes = attributes &
            (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
        this.fileLastWriteTime = lastWriteTime;
		this.status.currentFileName = name;
		try
		{
			this.erf.Clear();
            FCI.TCOMP tcomp = GetCompressionType(compLevel);

            if (Encoding.UTF8.GetByteCount(name) > name.Length)
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(name);
                name = Encoding.Default.GetString(nameBytes);
                this.fileAttributes |= FileAttributes.Normal;  // _A_NAME_IS_UTF
            }

			FCI.AddFile(this.fciHandle, CabBase.STREAM_FILE, name, execute, this.fciGetNextCabinetHandler,
				this.fciStatusHandler, this.fciGetOpenInfoHandler, tcomp);
			if(this.erf.fError)
			{
				throw new CabinetCreateException(this.erf.erfOper, this.erf.erfType, this.abortException);
			}
		}
		finally
		{
			this.fileStream = null;
			this.status.currentFileName = null;
		}
	}

	internal void FlushFolder()
	{
		this.erf.Clear();
		FCI.FlushFolder(this.fciHandle, this.fciGetNextCabinetHandler, this.fciStatusHandler);
		if(this.erf.fError)
		{
			throw new CabinetCreateException(this.erf.erfOper, this.erf.erfType, this.abortException);
		}
	}

	internal void FlushCabinet()
	{
		this.erf.Clear();
		FCI.FlushCabinet(this.fciHandle, false, this.fciGetNextCabinetHandler, this.fciStatusHandler);
		if(this.erf.fError)
		{
			throw new CabinetCreateException(this.erf.erfOper, this.erf.erfType, this.abortException);
		}
	}

	protected override Handle CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, Pointer pv)
	{
		if(path.Length == 0 || path == CabBase.STREAM_CAB || this.cabNumbers.Contains(path))
		{
			try
			{
				object cabNumber = this.cabNumbers[path];
				short iCab = (cabNumber != null ? (short) cabNumber : (short) 0);

				Stream stream = this.cabStream;
				if(stream == null)
				{
					#if !CABMINIMAL
					if(this.statusCallback != null && this.status.currentFolderTotalBytes > 0)
					{
						this.status.currentFolderBytesProcessed = this.status.currentFolderTotalBytes;
						this.status.statusType = CabinetStatusType.FinishFolder;
						this.statusCallback(this.status, this.statusContext);
						this.status.currentFolderBytesProcessed = this.status.currentFolderTotalBytes = 0;
					}
					#endif // !CABMINIMAL

					if(path.Length == 0 && this.nameHandler != null)
					{
						path = this.nameHandler(iCab, this.nameContext);
						if(path.Length != 0)
						{
							this.cabNumbers[path] = iCab;
						}
					}
					stream = this.openCabHandler(iCab, path.Length != 0 ? path : null, this.openCabContext);
					if(stream == null)
					{
						throw new FileNotFoundException(String.Format(CultureInfo.InvariantCulture, "Cabinet {0} not provided.", iCab));
					}
					this.status.currentCabinetName = path;
					this.status.currentCabinetNumber = iCab;
					if(this.status.totalCabinets <= this.status.currentCabinetNumber)
					{
						this.status.totalCabinets = (short) (this.status.currentCabinetNumber + 1);
					}

					#if !CABMINIMAL
					if(this.statusCallback != null)
					{
						this.status.currentCabinetTotalBytes = this.maxCabSize;
						this.status.currentCabinetBytesProcessed = 0;

						this.status.statusType = CabinetStatusType.StartCab;
						this.statusCallback(this.status, this.statusContext);
					}
					#endif // !CABMINIMAL
					this.cabStream = stream;
				}
				path = CabBase.STREAM_CAB;
			}
			catch(Exception ex)
			{
				if(this.abortException == null) this.abortException = ex;
				err = Marshal.GetHRForException(ex);
				return (Handle)0;
			}
		}
		return base.CabOpenStreamEx(path, openFlags, shareMode, out err, pv);
	}

	#if !CABMINIMAL
    protected override uint CabWriteStreamEx(Handle hStream, Pointer memory, uint cb, out int err, Pointer pv)
	{
		uint count = base.CabWriteStreamEx(hStream, memory, cb, out err, pv);
		if(count > 0 && err == 0 && this.statusCallback != null)
		{
			Stream stream = (Stream) streamHandles[hStream];
			if(this.cabStream != null)
			{
				Stream cabSource = ((DuplicateStream) this.cabStream).Source;
				if(stream == cabSource || (stream is DuplicateStream &&
					((DuplicateStream) stream).Source == cabSource))
				{
					this.status.currentCabinetBytesProcessed += cb;
					if(this.status.currentCabinetBytesProcessed > this.status.currentCabinetTotalBytes)
					{
						this.status.currentCabinetBytesProcessed = this.status.currentCabinetTotalBytes;
					}
				}
			}
		}
		return count;
	}
	#endif // !CABMINIMAL

    protected override int CabCloseStreamEx(Handle hStream, out int err, Pointer pv)
	{
		try
		{
			Stream stream = (Stream) streamHandles[hStream];
			if(stream is DuplicateStream) stream = ((DuplicateStream) stream).Source;

			if(this.fileStream == stream ||
				(this.fileStream is DuplicateStream && ((DuplicateStream) this.fileStream).Source == stream))
			{
				if(stream.CanWrite) stream.Flush();
				streamHandles.FreeHandle(hStream);

				if(this.closeFileHandler != null)
				{
					this.closeFileHandler(this.status.currentFileName, stream, this.openFileContext);
				}
				else
				{
					stream.Close();
				}
				this.fileStream = null;
				#if !CABMINIMAL
				if(this.statusCallback != null)
				{
					long remainder = this.status.currentFileTotalBytes - this.status.currentFileBytesProcessed;
					this.status.currentFileBytesProcessed += remainder;
					this.status.fileBytesProcessed += remainder;
					this.status.statusType = CabinetStatusType.FinishFile;
					this.statusCallback(this.status, this.statusContext);

					this.status.currentFileTotalBytes = 0;
					this.status.currentFileBytesProcessed = 0;
				}
				#endif // !CABMINIMAL
				this.status.currentFileName = null;
				err = 0;
				return 0;
			}
			else if(this.cabStream == stream ||
				(this.cabStream is DuplicateStream && ((DuplicateStream) this.cabStream).Source == stream))
			{
				if(stream.CanWrite) stream.Flush();
				streamHandles.FreeHandle(hStream);

				#if !CABMINIMAL
				if(this.statusCallback != null)
				{
					this.status.statusType = CabinetStatusType.FinishCab;
					this.statusCallback(this.status, this.statusContext);

					this.status.currentCabinetBytesProcessed = this.status.currentCabinetTotalBytes = 0;
				}
				this.totalFolderBytesProcessedInCurrentCab = 0;
				#endif // !CABMINIMAL

				if(this.closeCabHandler != null)
				{
					this.closeCabHandler(this.status.currentCabinetNumber, this.status.currentCabinetName, stream, this.openCabContext);
				}
				else
				{
					stream.Close();
				}
				this.status.currentCabinetName = null;
				this.cabStream = null;
				err = 0;
				return 0;
			}
			else
			{
				return base.CabCloseStreamEx(hStream, out err, pv);
			}
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			return 0;
		}
	}

    internal Handle CabGetOpenInfo(string file, out ushort date, out ushort time, out ushort attribs, out int err, Pointer pv)
	{
		try
		{
			if(file == CabBase.STREAM_FILE)
			{
				FCI.DateTimeToCabDateAndTime(this.fileLastWriteTime, out date, out time);
				attribs = (ushort) this.fileAttributes;

				Stream stream = this.fileStream;
				this.fileStream = new DuplicateStream(stream);
				Handle hStream = streamHandles.AllocHandle(stream);
				err = 0;
				return hStream;
			}
			else
			{
				throw new NotSupportedException("Cab engine is trying to open a file other than the primary cab or target files.");
			}
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			err = Marshal.GetHRForException(ex);
			date = 0;
			time = 0;
			attribs = 0;
			return (Handle) (-1);
		}
	}

    internal int CabFilePlaced(FCI.CCAB pccab, string pszFile, long cbFile, int fContinuation, Pointer pv)
	{
		return 0;
	}

    internal int CabGetNextCabinet(Pointer pccab, uint cbPrevCab, Pointer pv)
	{
		try
		{
			FCI.CCAB ccab = new FCI.CCAB();
			Marshal.PtrToStructure(pccab, ccab);

			ccab.szDisk = "";
			ccab.szCab = "";
			if(this.nameHandler != null)
			{
				ccab.szCab = this.nameHandler(ccab.iCab, this.nameContext);
			}
			this.cabNumbers[ccab.szCab] = (short) ccab.iCab;

			Marshal.StructureToPtr(ccab, pccab, false);
			return 1;
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			return 0;
		}
	}

	#if !CABMINIMAL
	private int totalFolderBytesProcessedInCurrentCab;
	#endif

    internal int CabCreateStatus(FCI.STATUS typeStatus, uint cb1, uint cb2, Pointer pv)
	{
		#if !CABMINIMAL
		if(this.statusCallback != null)
		{
			try
			{
				switch(typeStatus)
				{
					case FCI.STATUS.FILE:
					{
						if(cb2 > 0 && this.status.currentFileBytesProcessed < this.status.CurrentFileTotalBytes)
						{
							if(this.status.currentFileBytesProcessed + cb2 > this.status.currentFileTotalBytes)
							{
								cb2 = (uint) (this.status.currentFileTotalBytes - this.status.currentFileBytesProcessed);
							}
							this.status.currentFileBytesProcessed += cb2;
							this.status.fileBytesProcessed += cb2;

							this.status.statusType = CabinetStatusType.PartialFile;
							this.statusCallback(this.status, this.statusContext);
						}
					} break;

					case FCI.STATUS.FOLDER:
					{

						if(this.status.currentCabinetName != null)
						{
							if(0 < cb1 && cb1 < cb2)
							{
								this.status.currentCabinetBytesProcessed = cb1;
								this.status.currentCabinetTotalBytes = cb2;
								this.status.statusType = CabinetStatusType.PartialCab;
								this.statusCallback(this.status, this.statusContext);
							}
						}
						else
						{
							this.status.currentFolderBytesProcessed = cb1;
							if(cb1 == 0)
							{
								this.status.currentFolderTotalBytes = cb2 - this.totalFolderBytesProcessedInCurrentCab;
								this.totalFolderBytesProcessedInCurrentCab = (int) cb2;
								this.status.statusType = CabinetStatusType.StartFolder;
								this.statusCallback(this.status, this.statusContext);
							}
							else if(cb1 < cb2)
							{
								this.status.statusType = CabinetStatusType.PartialFolder;
								this.statusCallback(this.status, this.statusContext);
							}
						}
					} break;

					case FCI.STATUS.CABINET:
					{
					} break;
				}
			}
			catch(Exception ex)
			{
				if(this.abortException == null) this.abortException = ex;
				return -1;
			}
		}
		#endif // !CABMINIMAL
		return 0;
	}
}
}
#endif // !CABEXTRACTONLY
