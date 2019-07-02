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
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using Handle = System.Int32;
using Pointer = System.IntPtr;


/// <summary>
/// Extracts cabinet files.
/// </summary>
internal class CabExtractor : CabBase
{
	internal Pointer fdiHandle;

	private FDI.PFNALLOC fdiAllocMemHandler;
	private FDI.PFNFREE  fdiFreeMemHandler;
	private FDI.PFNOPEN  fdiOpenStreamHandler;
	private FDI.PFNREAD  fdiReadStreamHandler;
	private FDI.PFNWRITE fdiWriteStreamHandler;
	private FDI.PFNCLOSE fdiCloseStreamHandler;
	private FDI.PFNSEEK  fdiSeekStreamHandler;
	internal FDI.PFNNOTIFY  fdiNotifyHandler;

	internal CabinetExtractOpenFileHandler  openFileHandler;
	internal CabinetExtractCloseFileHandler closeFileHandler;
	internal object                         openFileContext;

	internal IDictionary    cabNumbers;

	internal int folderCount;
	internal ArrayList listFiles;

	internal string nextCabinetName;
	internal ushort folderId;

	internal CabExtractor()
	{
		this.fdiAllocMemHandler    = new FDI.PFNALLOC (this.CabAllocMem);
		this.fdiFreeMemHandler     = new FDI.PFNFREE  (this.CabFreeMem);
		this.fdiOpenStreamHandler  = new FDI.PFNOPEN  (this.CabOpenStream);
		this.fdiReadStreamHandler  = new FDI.PFNREAD  (this.CabReadStream);
		this.fdiWriteStreamHandler = new FDI.PFNWRITE (this.CabWriteStream);
		this.fdiCloseStreamHandler = new FDI.PFNCLOSE (this.CabCloseStream);
		this.fdiSeekStreamHandler  = new FDI.PFNSEEK  (this.CabSeekStream);
		this.fdiNotifyHandler      = new FDI.PFNNOTIFY(this.CabExtractListNotify);
		
		this.cabStream = null;
		this.cabNumbers = new Hashtable(1);
		this.folderCount = 0;
		this.listFiles = null;
		this.fdiHandle = FDI.Create(this.fdiAllocMemHandler, this.fdiFreeMemHandler,
			this.fdiOpenStreamHandler, this.fdiReadStreamHandler, this.fdiWriteStreamHandler,
			this.fdiCloseStreamHandler, this.fdiSeekStreamHandler, FDI.CPU_80386, this.erf);
		if(this.erf.fError)
		{
			int error = this.erf.erfOper;
			int errorCode = this.erf.erfType;
			this.erf.Dispose();
			this.erf = null;
            this.fdiHandle = Pointer.Zero;
			throw new CabinetExtractException(error, errorCode, this.abortException);
		}
	}

	protected override void Dispose(bool disposing) 
	{
		if(disposing) 
		{
			// Dispose managed objects.
		}

		// Dispose unmanaged objects.
        if (this.fdiHandle != Pointer.Zero)
		{
			FDI.Destroy(this.fdiHandle);
			this.erf.Dispose();
			this.erf = null;
			this.cabStream = null;
			this.fileStream = null;
            this.fdiHandle = Pointer.Zero;
		}
	}

	internal void Process()
	{
		this.erf.Clear();
        FDI.Copy(this.fdiHandle, "", "", 0, this.fdiNotifyHandler, Pointer.Zero);
		if(this.erf.fError)
		{
			throw new CabinetExtractException(this.erf.erfOper, this.erf.erfType, this.abortException);
		}
	}

	#if !CABMINIMAL
	internal bool IsCabinet(Stream cabStream, out ushort id, out int folderCount, out int fileCount)
	{
		Handle hStream = streamHandles.AllocHandle(cabStream);
		try
		{
			this.erf.Clear();
			FDI.CABINFO fdici;
			bool isCabinet = 0 != FDI.IsCabinet(this.fdiHandle, hStream, out fdici);
			if(this.erf.fError && ((FDI.ERROR) this.erf.erfOper) == FDI.ERROR.UNKNOWN_CABINET_VERSION)
			{
				this.erf.fError = false;
				isCabinet = false;
			}
			if(this.erf.fError)
			{
				throw new CabinetExtractException(this.erf.erfOper, this.erf.erfType, this.abortException);
			}
			id = fdici.setID;
			folderCount = (int) fdici.cFolders;
			fileCount = (int) fdici.cFiles;
			return isCabinet;
		}
		finally
		{
			streamHandles.FreeHandle(hStream);
		}
	}
	#endif

	/*
	internal void TruncateCabinet(Stream cabStream, ushort folderToDelete)
	{
		this.cabStream = cabStream;
		try
		{
			this.erf.Clear();
			FDI.TruncateCabinet(this.fdiHandle, "", folderToDelete);
			if(this.erf.fError)
			{
				throw new CabinetExtractException(this.erf.erfOper, this.erf.erfType, this.abortException);
			}
		}
		finally
		{
			this.cabStream = null;
		}
	}
	*/

    protected override Handle CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, Pointer hContext)
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
					if(path.Length == 0 && this.status.currentCabinetName == null)
					{
						path = this.nextCabinetName;
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
						this.status.currentCabinetTotalBytes = stream.Length;
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
		return base.CabOpenStreamEx(path, openFlags, shareMode, out err, hContext);
	}

	#if !CABMINIMAL
    protected override uint CabReadStreamEx(Handle hStream, Pointer memory, uint cb, out int err, Pointer hContext)
	{
		uint count = base.CabReadStreamEx(hStream, memory, cb, out err, hContext);
		if(err == 0 && this.statusCallback != null && this.cabStream != null)
		{
			Stream stream = (Stream) streamHandles[hStream];
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
		return count;
	}

    protected override uint CabWriteStreamEx(Handle hStream, Pointer memory, uint cb, out int err, Pointer hContext)
	{
		uint count = base.CabWriteStreamEx(hStream, memory, cb, out err, hContext);
		if(count > 0 && err == 0 && this.statusCallback != null)
		{
			Stream stream = (Stream) streamHandles[hStream];
			if(stream == this.fileStream)
			{
				this.status.currentFileBytesProcessed += cb;
				this.status.fileBytesProcessed += cb;
				this.status.statusType = CabinetStatusType.PartialFile;
				this.statusCallback(status, this.statusContext);
			}
		}
		return count;
	}
	#endif // !CABMINIMAL

    protected override int CabCloseStreamEx(Handle hStream, out int err, Pointer hContext)
	{
		Stream stream = (Stream) streamHandles[hStream];
		if(stream is DuplicateStream) stream = ((DuplicateStream) stream).Source;

		if(this.cabStream == stream ||
			(this.cabStream is DuplicateStream && ((DuplicateStream) this.cabStream).Source == stream))
		{
			try
			{
				if(stream.CanWrite) stream.Flush();
				streamHandles.FreeHandle(hStream);

				#if !CABMINIMAL
				if(this.statusCallback != null)
				{
					if(this.status.currentFileNumber > 0 &&
						this.status.currentCabinetNumber == this.status.totalCabinets - 1)
					{
						this.status.statusType = CabinetStatusType.FinishFolder;
						this.statusCallback(this.status, this.statusContext);
					}
					this.status.statusType = CabinetStatusType.FinishCab;
					this.statusCallback(this.status, this.statusContext);

					this.status.currentCabinetBytesProcessed = this.status.currentCabinetTotalBytes = 0;
				}
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
			catch(Exception ex)
			{
				if(this.abortException == null) this.abortException = ex;
				err = Marshal.GetHRForException(ex);
				return 0;
			}
		}
		else
		{
			return base.CabCloseStreamEx(hStream, out err, hContext);
		}
	}

	internal Handle CabExtractListNotify(FDI.NOTIFICATIONTYPE notificationType, FDI.NOTIFICATION notification)
	{
		try
		{
			switch(notificationType)
			{
				case FDI.NOTIFICATIONTYPE.CABINET_INFO:
				{
					this.nextCabinetName = (notification.psz1.Length != 0 ? notification.psz1 : null);
					return (Handle)0;  // Continue
				}
				case FDI.NOTIFICATIONTYPE.ENUMERATE:
				{
                    return (Handle)0;  // Continue
				}
				case FDI.NOTIFICATIONTYPE.PARTIAL_FILE:
				{
					// This notification can occur when examining the contents of a non-first cab file.
                    return (Handle)0;  // Continue
				}
				case FDI.NOTIFICATIONTYPE.COPY_FILE:
				{
					if(notification.iFolder != this.folderId || this.folderCount == 0)
					{
						this.folderId = notification.iFolder;
						this.folderCount++;
					}
                    bool utfName = (notification.attribs & (ushort)FileAttributes.Normal) != 0;  // _A_NAME_IS_UTF
                    //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

					string name = notification.psz1;
                    if (utfName)
                    {
                        byte[] nameBytes = Encoding.Default.GetBytes(name);
                        name = Encoding.UTF8.GetString(nameBytes);
                    }
					if(this.filterFileHandler == null || this.filterFileHandler(this.folderCount-1, name, filterFileContext))
					{
						if(this.listFiles != null)
						{
							#if !CABMINIMAL
								FileAttributes attributes = (FileAttributes)notification.attribs &
                                    (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                                if (attributes == (FileAttributes)0)
                                {
                                    attributes = FileAttributes.Normal;
                                }
                                DateTime lastWriteTime;
								FDI.CabDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);
								long length = notification.cb;

								CabinetFileInfo fileInfo = new CabinetFileInfo(Path.GetFileName(name),
									Path.GetDirectoryName(name), this.folderCount-1, notification.iCabinet,
									attributes, lastWriteTime, length);
								listFiles.Add(fileInfo);
							#else
								listFiles.Add(name);
							#endif // CABMINIMAL
						}
					}
                    return (Handle)0;  // Continue
                }
				default:
				{
					// Should never get any other notification types here.
					throw new CabinetExtractException((int) FDI.ERROR.UNKNOWN_CABINET_VERSION, 0);
				}
			}
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			return (Handle) (-1);
		}
	}

	internal Handle CabExtractNotify(FDI.NOTIFICATIONTYPE notificationType, FDI.NOTIFICATION notification)
	{
		try
		{
			switch(notificationType)
			{
				case FDI.NOTIFICATIONTYPE.CABINET_INFO:
				{
					this.nextCabinetName = (notification.psz1.Length != 0 ? notification.psz1 : null);
                    return (Handle)0;  // Continue
                }
				case FDI.NOTIFICATIONTYPE.PARTIAL_FILE:
				{
                    return (Handle)0;  // Continue
                }
				case FDI.NOTIFICATIONTYPE.NEXT_CABINET:
				{
					this.cabNumbers[notification.psz1] = (short) notification.iCabinet;
                    return (Handle)0;  // Continue
                }
				case FDI.NOTIFICATIONTYPE.ENUMERATE:
				{
                    return (Handle)0;  // Continue
                }
				case FDI.NOTIFICATIONTYPE.COPY_FILE:
				{
					if(notification.iFolder != this.folderId ||
						(this.status.currentCabinetNumber == 0 && this.status.currentFileNumber == 0))
					{
						if(this.status.currentCabinetNumber != 0 || this.status.currentFileNumber != 0)
						{
							#if !CABMINIMAL
							if(this.statusCallback != null)
							{
								this.status.statusType = CabinetStatusType.FinishFolder;
								this.statusCallback(this.status, this.statusContext);
							}
							#endif // !CABMINIMAL
							this.status.currentFolderNumber++;
						}
						this.folderId = notification.iFolder;
						#if !CABMINIMAL
						if(this.statusCallback != null)
						{
							this.status.statusType = CabinetStatusType.StartFolder;
							this.statusCallback(this.status, this.statusContext);
						}
						#endif // !CABMINIMAL
					}

                    bool utfName = (notification.attribs & (ushort)FileAttributes.Normal) != 0;  // _A_NAME_IS_UTF
                    //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

                    string name = notification.psz1;
                    if (utfName)
                    {
                        byte[] nameBytes = Encoding.Default.GetBytes(name);
                        name = Encoding.UTF8.GetString(nameBytes);
                    }
                    if ((this.filterFileHandler == null || this.filterFileHandler(this.status.currentFolderNumber, name, filterFileContext)) &&
						this.openFileHandler != null)
					{
						this.status.currentFileName = name;

						#if !CABMINIMAL
						if(this.statusCallback != null)
						{
							this.status.currentFileBytesProcessed = 0;
							this.status.currentFileTotalBytes = notification.cb;
							this.status.statusType = CabinetStatusType.StartFile;
							this.statusCallback(this.status, this.statusContext);
						}
						#endif // !CABMINIMAL

						DateTime lastWriteTime;
						FDI.CabDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);

						Stream stream = this.openFileHandler(name, notification.cb, lastWriteTime, openFileContext);
						if(stream != null)
						{
							Handle hStream = streamHandles.AllocHandle(stream);
							return hStream;
						}
						else
						{
							#if !CABMINIMAL
							if(this.statusCallback != null)
							{
								this.status.fileBytesProcessed += notification.cb;
								this.status.statusType = CabinetStatusType.FinishFile;
								this.statusCallback(this.status, this.statusContext);
							}
							#endif // !CABMINIMAL
							this.status.currentFileName = null;
							this.status.currentFileNumber++;
						}
					}
                    return (Handle)0;  // Continue
                }
				case FDI.NOTIFICATIONTYPE.CLOSE_FILE_INFO:
				{
					Stream stream = (Stream) streamHandles[notification.hf];
					streamHandles.FreeHandle(notification.hf);

                    bool utfName = (notification.attribs & (ushort)FileAttributes.Normal) != 0;  // _A_NAME_IS_UTF
                    //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

                    string name = notification.psz1;
                    if (utfName)
                    {
                        byte[] nameBytes = Encoding.Default.GetBytes(name);
                        name = Encoding.UTF8.GetString(nameBytes);
                    }
                    FileAttributes attributes = (FileAttributes)notification.attribs &
                        (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                    if (attributes == (FileAttributes)0)
                    {
                        attributes = FileAttributes.Normal;
                    }
                    DateTime lastWriteTime;
					FDI.CabDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);
					long length = notification.cb;

					stream.Flush();
					if(this.closeFileHandler != null)
					{
						this.closeFileHandler(name, stream, attributes, lastWriteTime, this.openFileContext);
					}

					#if !CABMINIMAL
					if(this.statusCallback != null)
					{
						long remainder = this.status.currentFileTotalBytes - this.status.currentFileBytesProcessed;
						this.status.currentFileBytesProcessed += remainder;
						this.status.fileBytesProcessed += remainder;
						this.status.statusType = CabinetStatusType.FinishFile;
						this.statusCallback(this.status, this.statusContext);
					}
					#endif // !CABMINIMAL
					this.status.currentFileName = null;
					this.status.currentFileNumber++;

					return (Handle) 1;  // Continue
				}
				default:
				{
					// Should never get any other notification types here.
					throw new CabinetExtractException((int) FDI.ERROR.UNKNOWN_CABINET_VERSION, 0);
				}
			}
		}
		catch(Exception ex)
		{
			if(this.abortException == null) this.abortException = ex;
			return (Handle) (-1);
		}
	}
}
}
