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
using System.Security.Permissions;


#if !CABEXTRACTONLY
/// <summary>
/// Specifies the compression level ranging from 0 compresion to maximum compression.
/// </summary>
/// <remarks>
/// Although only four values are enumerated, any integral value between
/// <see cref="CabinetCompressionLevel.Min"/> and <see cref="CabinetCompressionLevel.Max"/> can also be used.
/// </remarks>
internal enum CabinetCompressionLevel : int
{
	/// <summary>Do not compress files, only store.</summary>
	None   = 0,
	/// <summary>Minimum compression; fastest.</summary>
	Min    = 1,
	/// <summary>Maximum compression; slowest.</summary>
	Max    = Min + ((FCI.TCOMP.LZX_WINDOW_HI - FCI.TCOMP.LZX_WINDOW_LO) >> FCI.TCOMP.SHIFT_LZX_WINDOW),
	/// <summary>Compromize between speed and compression efficiency.</summary>
	Normal = (Min + Max) / 2
}

/// <summary>
/// For Stream-based processing of a cabinet chain, reports the name of a particular cabinet number.
/// </summary>
/// <param name="whichCab">The cabinet number.  The first cab is number 0.</param>
/// <param name="nameContext">User context object.</param>
/// <returns>The name of the cabinet. (Not necessarily the name of the cabinet file on disk.)</returns>
internal delegate string CabinetNameHandler(int whichCab, object nameContext);

/// <summary>
/// For Stream-based cabinet creation, gets a read stream for a particular file.
/// </summary>
/// <param name="name">The name of the file in the cabinet (not the external filename).
/// Also includes the internal path of the file, if any.</param>
/// <param name="attributes">Attributes of the file, to be stored in the cabinet.</param>
/// <param name="lastWriteTime">Modify date of the file, to be stored in the cabinet.</param>
/// <param name="openFileContext">User context object.</param>
/// <returns>Stream for reading the bytes of the file.</returns>
internal delegate Stream CabinetCreateOpenFileHandler(string name, out FileAttributes attributes, out DateTime lastWriteTime, object openFileContext);

/// <summary>
/// For Stream-based cabinet creation, closes a Stream that was returned by the <see cref="CabinetCreateOpenFileHandler"/>.
/// </summary>
/// <param name="name">The name of the file in the cabinet (not the external filename).
/// Also includes the internal path of the file, if any.</param>
/// <param name="stream">The stream that was used to read the file, now ready to be closed.</param>
/// <param name="openFileContext">User context object.</param>
internal delegate void CabinetCreateCloseFileHandler(string name, Stream stream, object openFileContext);
#endif // !CABEXTRACTONLY

/// <summary>
/// For Stream-based cabinet extraction, opens a write stream for a particular file.
/// </summary>
/// <param name="name">The name of the file in the cabinet (not the external filename).
/// Also includes the internal path of the file, if any.</param>
/// <param name="fileSize">Size of the file.</param>
/// <param name="lastWriteTime">Modify date of the file that was stored in the cabinet,
/// or DateTime.MinValue if no date was stored.</param>
/// <param name="openFileContext">User context object.</param>
/// <returns>Stream for writing the bytes of the file, or null if this file should be skipped.</returns>
/// <remarks>An implementation may use <paramref name="lastWriteTime"/>to decide that the file on disk is already
/// the same or newer and thus doesn't need to be re-extracted.
/// <p>The implementation should compare the <paramref name="lastWriteTime"/> to
/// DateTime.MinValue before attempting to use the value anywhere.</p></remarks>
internal delegate Stream CabinetExtractOpenFileHandler(string name, long fileSize, DateTime lastWriteTime, object openFileContext);

/// <summary>
/// For Stream-based cabinet extraction, closes a Stream that was returned by the <see cref="CabinetExtractOpenFileHandler"/>.
/// </summary>
/// <param name="name">The name of the file in the cabinet (not the external filename).
/// Also includes the internal path of the file, if any.</param>
/// <param name="stream">The stream that was used to write the file, now ready to be closed.</param>
/// <param name="attributes">Attributes of the file.</param>
/// <param name="lastWriteTime">Modify date of the file that was stored in the cabinet,
/// or DateTime.MinValue if no date was stored.</param>
/// <param name="openFileContext">User context object.</param>
/// <returns>Stream for writing the bytes of the file.</returns>
/// <remarks>The implementation should compare the <paramref name="lastWriteTime"/> to
/// DateTime.MinValue before attempting to use the value anywhere.</remarks>
internal delegate void CabinetExtractCloseFileHandler(string name, Stream stream, FileAttributes attributes, DateTime lastWriteTime, object openFileContext);

/// <summary>
/// For Stream-based cabinet extraction, filters the list of files to be extracted.
/// </summary>
/// <param name="folder">The folder number containing the file.</param>
/// <param name="name">The name of the file in the cabinet (not the external filename).
/// Also includes the internal path of the file, if any.</param>
/// <param name="filterContext">User context object.</param>
/// <returns>True if the file should be extracted; false otherwise.</returns>
internal delegate bool CabinetFilterFileHandler(int folder, string name, object filterContext);

/// <summary>
/// For Stream-based cabinet processing, opens a cabinet file.
/// </summary>
/// <param name="whichCab">The cabinet number of the cabinet within the chain (if processing a chain).</param>
/// <param name="cabName">The name name of the cabinet. (Not necessarily the name of the cabinet file on disk.)</param>
/// <param name="openCabContext">User context object.</param>
/// <returns>A Stream for accessing the cabinet file.  If creating a cabinet, the stream should be opened for writing.
/// If extracting, the stream should be opened for reading.</returns>
internal delegate Stream CabinetOpenCabHandler(int whichCab, string cabName, object openCabContext);

/// <summary>
/// For Stream-based cabinet processing, closes a Stream that was returned by the <see cref="CabinetOpenCabHandler"/>.
/// </summary>
/// <param name="whichCab">The cabinet number of the cabinet within the chain (if processing a chain).</param>
/// <param name="cabName">The name name of the cabinet. (Not necessarily the name of the cabinet file on disk.)</param>
/// <param name="stream">The stream that was used to read or write the cabinet file, now ready to be closed.</param>
/// <param name="openCabContext">User context object.</param>
/// <returns>A Stream for accessing the cabinet file.  If creating a cabinet, the stream should be opened for writing.
/// If extracting, the stream should be opened for reading.</returns>
internal delegate void CabinetCloseCabHandler(int whichCab, string cabName, Stream stream, object openCabContext);

/// <summary>
/// This class provides low-level, Stream-based access to all supported cab functions.
/// </summary>
/// <remarks>
/// SECURITY NOTE: The core cabinet operations contain security assertions to allow them to call unmanaged code
/// (the cabinet engine) even if the calling assembly doesn't have permission. (Of course, this assembly must be
/// trusted for the assertions to work.)  Assertions are not propogated through calls to provided delegates,
/// so there is no way that a partially-trusted malicious client could trick a trusted Cabinet class into executing
/// its own unmanaged code.
/// </remarks>
internal sealed class Cabinet
{
	private Cabinet() { }

	#if !CABMINIMAL
	/// <summary>
	/// Checks whether a Stream begins with a header that indicates it is a valid cabinet file.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <returns>True if the stream is a valid cabinet file; false otherwise.</returns>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static bool IsCabinet(Stream stream)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using(CabExtractor cabInstance = new CabExtractor())
		{
			ushort id;
			int folderCount, fileCount;
			return cabInstance.IsCabinet(stream, out id, out folderCount, out fileCount);
		}
	}

	/// <summary>
	/// Gets the number of folders in a cabinet Stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <returns>The number of folders in the cabinet file.</returns>
	/// <exception cref="CabinetExtractException">The stream is not a valid cabinet file.</exception>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static int GetFolderCount(Stream stream)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using(CabExtractor cabInstance = new CabExtractor())
		{
			ushort id;
			int folderCount, fileCount;
			if(!cabInstance.IsCabinet(stream, out id, out folderCount, out fileCount))
			{
				throw new CabinetExtractException((int) FDI.ERROR.NOT_A_CABINET, 0);
			}
			return folderCount;
		}
	}

	/// <summary>
	/// Gets the offset of a cabinet file that is positioned 0 or more bytes from the start of the Stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <returns>The offset in bytes of the cabinet file, or -1 if no cabinet file is found in the Stream.</returns>
	/// <remarks>The cabinet must begin on a 4-byte boundary.</remarks>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static long FindCabinetOffset(Stream stream)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using(CabExtractor cabInstance = new CabExtractor())
		{

			ushort id;
			int folderCount, fileCount;
			long sectionSize = 4;
			long offset;
			long length = stream.Length;
			for(offset = 0; offset < length; offset += sectionSize)
			{
				stream.Seek(offset, SeekOrigin.Begin);
				if(cabInstance.IsCabinet(stream, out id, out folderCount, out fileCount))
				{
					return offset;
				}
			}
			return -1;
		}
	}

	internal static CabinetFileInfo[] GetFiles(Stream stream, CabinetFilterFileHandler filterFileHandler, object filterContext)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		return GetFiles(new CabinetOpenCabHandler(Cabinet.DefaultOpenCabHandler),
			new CabinetCloseCabHandler(Cabinet.DefaultCloseCabHandler), stream, false, filterFileHandler, filterContext);
	}
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	internal static CabinetFileInfo[] GetFiles(CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler,
		object openCabContext, bool autoChain, CabinetFilterFileHandler filterFileHandler, object filterContext)
	{
		using(CabExtractor cabInstance = new CabExtractor())
		{
			cabInstance.filterFileHandler = filterFileHandler;
			cabInstance.filterFileContext = filterContext;
			cabInstance.openCabHandler = openCabHandler;
			cabInstance.closeCabHandler = closeCabHandler;
			cabInstance.openCabContext = openCabContext;
			cabInstance.nextCabinetName = "";
			cabInstance.listFiles = new ArrayList();
			for(short iCab = 0; (autoChain || iCab == 0) && cabInstance.nextCabinetName != null; iCab++)
			{
				cabInstance.cabNumbers[""] = iCab;
				cabInstance.Process();
			}
			return (CabinetFileInfo[]) cabInstance.listFiles.ToArray(typeof(CabinetFileInfo));
		}
	}
	#endif // !CABMINIMAL

	/// <summary>
	/// Gets the list of files in a cabinet Stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <returns>An array containing the names of all files contained in the cabinet file.</returns>
	/// <exception cref="CabinetExtractException">The stream is not a valid cabinet file.</exception>
	public static string[] GetFiles(Stream stream)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		return GetFiles(new CabinetOpenCabHandler(Cabinet.DefaultOpenCabHandler),
			new CabinetCloseCabHandler(Cabinet.DefaultCloseCabHandler), stream, false);
	}

	/// <summary>
	/// Gets the list of files in a cabinet or cabinet chain.
	/// </summary>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="autoChain">True to automatically process multiple cabinets in a chain.  If false, the
	/// aggregate list of files may still be obtained by getting the file list from each cab individually.</param>
	/// <returns>An array containing the names of all files contained in the cabinet chain.</returns>
	/// <exception cref="CabinetExtractException">A stream returned by the <paramref name="openCabHandler"/>
	/// is not a valid cabinet file.</exception>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static string[] GetFiles(CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler,
		object openCabContext, bool autoChain)
	{
		using(CabExtractor cabInstance = new CabExtractor())
		{
			cabInstance.openCabHandler = openCabHandler;
			cabInstance.closeCabHandler = closeCabHandler;
			cabInstance.openCabContext = openCabContext;
			cabInstance.nextCabinetName = "";
			cabInstance.listFiles = new ArrayList();
			for(short iCab = 0; (autoChain || iCab == 0) && cabInstance.nextCabinetName != null; iCab++)
			{
				cabInstance.cabNumbers[""] = iCab;
				cabInstance.Process();
			}
			#if !CABMINIMAL
				string[] fileNames = new string[cabInstance.listFiles.Count];
				for(int i = 0; i < cabInstance.listFiles.Count; i++)
				{
					fileNames[i] = ((CabinetFileInfo) cabInstance.listFiles[i]).Name;
				}
				return fileNames;
			#else
				return (string[]) cabInstance.listFiles.ToArray(typeof(string));
			#endif
		}
	}

	/// <summary>
	/// Reads a single file from a cabinet stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <param name="name">Name of the file within the cabinet (not the external file path).</param>
	/// <returns>A stream for reading the extracted file, or null if the file does not exist in the cabinet.</returns>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	/// <remarks>This method may fail if the file size is greater than available memory.</remarks>
	public static Stream Extract(Stream stream, string name)
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		return Cabinet.Extract(new CabinetOpenCabHandler(Cabinet.DefaultOpenCabHandler),
			new CabinetCloseCabHandler(Cabinet.DefaultCloseCabHandler), stream, false, name);
	}

	/// <summary>
	/// Reads a single file from a cabinet or cabinet chain.
	/// </summary>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="autoChain">True to automatically process multiple cabinets in a chain.  If false, the
	/// aggregate list of files may still be obtained by getting the file list from each cab individually.</param>
	/// <param name="name">Name of the file within the cabinet (not the external file path).</param>
	/// <returns>A stream for reading the extracted file, or null if the file does not exist in the cabinet.</returns>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	/// <remarks>This method may fail if the file size is greater than available memory.</remarks>
	public static Stream Extract(CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler,
		object openCabContext, bool autoChain, string name)
	{
		object[] openFileContext = new object[1];
		Cabinet.Extract(openCabHandler, closeCabHandler, openCabContext, autoChain,
			new CabinetExtractOpenFileHandler(Cabinet.ExtractBytesOpenFileHandler),
			null, openFileContext, new CabinetFilterFileHandler(Cabinet.ExtractBytesFilterFileHandler), name
			#if !CABMINIMAL
			, null, null
			#endif // !CABMINIMAL
			);
		MemoryStream extractStream = (MemoryStream) openFileContext[0];
		if(extractStream != null) extractStream.Position = 0;
		return extractStream;
	}
	private static Stream ExtractBytesOpenFileHandler(string name, long fileSize,
		DateTime lastWriteTime, object openFileContext)
	{
		MemoryStream extractStream = new MemoryStream(new byte[fileSize], 0, (int) fileSize, true, true);
		((object[]) openFileContext)[0] = extractStream;
		return extractStream;
	}
	private static bool ExtractBytesFilterFileHandler(int folder, string name, object filterContext)
	{
		return name.ToLower(CultureInfo.InvariantCulture)
			== ((string) filterContext).ToLower(CultureInfo.InvariantCulture);
	}

	#if CABMINIMAL
	/// <summary>
	/// Extracts files from a cabinet stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <param name="openFileHandler">Callback for opening streams for writing extracted files.</param>
	/// <param name="closeFileHandler">Callback for closing extracted file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="filterFileHandler">Callback for filtering the list of extracted files.  If null,
	/// all files will be extracted.</param>
	/// <param name="filterContext">User context object that will be passed to the
	/// <paramref name="filterFileHandler"/>.</param>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	public static void Extract(Stream stream, CabinetExtractOpenFileHandler openFileHandler,
		CabinetExtractCloseFileHandler closeFileHandler, object openFileContext,
		CabinetFilterFileHandler filterFileHandler, object filterContext)
	#else // !CABMINIMAL
	
	/// <summary>
	/// Extracts files from a cabinet stream.
	/// </summary>
	/// <param name="stream">Stream for reading the cabinet file.</param>
	/// <param name="openFileHandler">Callback for opening streams for writing extracted files.</param>
	/// <param name="closeFileHandler">Callback for closing extracted file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="filterFileHandler">Callback for filtering the list of extracted files.  If null,
	/// all files will be extracted.</param>
	/// <param name="filterContext">User context object that will be passed to the
	/// <paramref name="filterFileHandler"/>.</param>
	/// <param name="statusCallback">Callback for reporting extraction status.  This may be null
	/// if status is not desired.</param>
	/// <param name="statusContext">User context object passed to the <paramref name="statusCallback"/>.</param>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	public static void Extract(Stream stream, CabinetExtractOpenFileHandler openFileHandler,
		CabinetExtractCloseFileHandler closeFileHandler, object openFileContext,
		CabinetFilterFileHandler filterFileHandler, object filterContext,
		CabinetStatusCallback statusCallback, object statusContext)
	#endif
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		Cabinet.Extract(new CabinetOpenCabHandler(Cabinet.DefaultOpenCabHandler),
			new CabinetCloseCabHandler(Cabinet.DefaultCloseCabHandler), stream, false, openFileHandler,
			closeFileHandler, openFileContext, filterFileHandler, filterContext
			#if !CABMINIMAL
			, statusCallback, statusContext
			#endif // !CABMINIMAL
			);
	}

	#if CABMINIMAL
	/// <summary>
	/// Extracts files from a cabinet or cabinet chain.
	/// </summary>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="autoChain">True to automatically process multiple cabinets in a chain.  If false, the
	/// aggregate set of files may still be extracted by extracting the files from each cab individually.</param>
	/// <param name="openFileHandler">Callback for opening streams for writing extracted files.</param>
	/// <param name="closeFileHandler">Callback for closing extracted file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="filterFileHandler">Callback for filtering the list of extracted files.  If null,
	/// all files will be extracted.</param>
	/// <param name="filterContext">User context object that will be passed to the
	/// <paramref name="filterFileHandler"/>.</param>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static void Extract(CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler,
		object openCabContext, bool autoChain, CabinetExtractOpenFileHandler openFileHandler,
		CabinetExtractCloseFileHandler closeFileHandler, object openFileContext,
		CabinetFilterFileHandler filterFileHandler, object filterContext)
	#else // !CABMINIMAL
	/// <summary>
	/// Extracts files from a cabinet or cabinet chain.
	/// </summary>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="autoChain">True to automatically process multiple cabinets in a chain.  If false, the
	/// aggregate set of files may still be extracted by extracting the files from each cab individually.</param>
	/// <param name="openFileHandler">Callback for opening streams for writing extracted files.</param>
	/// <param name="closeFileHandler">Callback for closing extracted file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="filterFileHandler">Callback for filtering the list of extracted files.  If null,
	/// all files will be extracted.</param>
	/// <param name="filterContext">User context object that will be passed to the
	/// <paramref name="filterFileHandler"/>.</param>
	/// <param name="statusCallback">Callback for reporting extraction progress.  This may be null
	/// if status is not desired.</param>
	/// <param name="statusContext">User context object passed to the <paramref name="statusCallback"/>.</param>
	/// <exception cref="CabinetExtractException">The stream is not a cabinet file or the cabinet file is corrupt.</exception>
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static void Extract(CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler,
		object openCabContext, bool autoChain, CabinetExtractOpenFileHandler openFileHandler,
		CabinetExtractCloseFileHandler closeFileHandler, object openFileContext,
		CabinetFilterFileHandler filterFileHandler, object filterContext,
		CabinetStatusCallback statusCallback, object statusContext)
	#endif // !CABMINIMAL
	{
		using(CabExtractor cabInstance = new CabExtractor())
		{
			cabInstance.openCabHandler = openCabHandler;
			cabInstance.closeCabHandler = closeCabHandler;
			cabInstance.openCabContext = openCabContext;
			cabInstance.openFileHandler = openFileHandler;
			cabInstance.closeFileHandler = closeFileHandler;
			cabInstance.openFileContext = openFileContext;
			cabInstance.filterFileHandler = filterFileHandler;
			cabInstance.filterFileContext = filterContext;
			cabInstance.fdiNotifyHandler = new FDI.PFNNOTIFY(cabInstance.CabExtractNotify);
			cabInstance.nextCabinetName = "";

			#if !CABMINIMAL
			cabInstance.statusCallback = statusCallback;
			cabInstance.statusContext = statusContext;

			if(statusCallback != null)
			{
				CabinetFileInfo[] files = Cabinet.GetFiles(openCabHandler, closeCabHandler, openCabContext,
					autoChain, filterFileHandler, filterContext);

				cabInstance.status.totalFiles = files.Length;
				cabInstance.status.totalFileBytes = 0;

				int prevFolder = -1, prevCabinet = -1;
				for(int i = 0; i < files.Length; i++)
				{
					cabInstance.status.totalFileBytes += files[i].Length;
					if(files[i].CabinetFolderNumber != prevFolder)  // Assumes the results of GetFiles are grouped by folders
					{
						prevFolder = files[i].CabinetFolderNumber;
						cabInstance.status.totalFolders++;
					}
					if(files[i].CabinetNumber != prevCabinet)
					{
						prevCabinet = files[i].CabinetNumber;
						cabInstance.status.totalCabinets++;
					}
				}
			}
			#endif // !CABMINIMAL

			for(short iCab = 0; (autoChain || iCab == 0) && cabInstance.nextCabinetName != null; iCab++)
			{
				cabInstance.cabNumbers[""] = iCab;
				cabInstance.Process();
			}
		}
	}

	#if !CABEXTRACTONLY
	#if CABMINIMAL
	/// <summary>
	/// Creates a cabinet and writes it to a stream.
	/// </summary>
	/// <param name="stream">The stream for writing the cabinet.</param>
	/// <param name="files">The names of the files in the cabinet (not external file paths).</param>
	/// <param name="compLevel">The cabinet compression level.</param>
	/// <param name="openFileHandler">Callback for opening streams for reading included files.</param>
	/// <param name="closeFileHandler">Callback for closing included file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <exception cref="CabinetCreateException">The cabinet could not be created.</exception>
	public static void Create(Stream stream, string[] files, CabinetCompressionLevel compLevel,
		CabinetCreateOpenFileHandler openFileHandler, CabinetCreateCloseFileHandler closeFileHandler,
		object openFileContext)
	#else // !CABMINIMAL
	/// <summary>
	/// Creates a cabinet and writes it to a stream.
	/// </summary>
	/// <param name="stream">The stream for writing the cabinet.</param>
	/// <param name="files">The names of the files in the cabinet (not external file paths).</param>
	/// <param name="compLevel">The cabinet compression level.</param>
	/// <param name="openFileHandler">Callback for opening streams for reading included files.</param>
	/// <param name="closeFileHandler">Callback for closing included file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="statusCallback">Callback for reporting creation progress.  This may be null
	/// if status is not desired.</param>
	/// <param name="statusContext">User context object passed to the <paramref name="statusCallback"/>.</param>
	/// <exception cref="CabinetCreateException">The cabinet could not be created.</exception>
	public static void Create(Stream stream, string[] files, CabinetCompressionLevel compLevel,
		CabinetCreateOpenFileHandler openFileHandler, CabinetCreateCloseFileHandler closeFileHandler,
		object openFileContext, CabinetStatusCallback statusCallback, object statusContext)
	#endif // !CABMINIMAL
	{
		if(stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		Cabinet.Create(null, null, 0, 0, new CabinetOpenCabHandler(Cabinet.DefaultOpenCabHandler),
			new CabinetCloseCabHandler(Cabinet.DefaultCloseCabHandler), stream, new string[][] { files }, compLevel,
			openFileHandler, closeFileHandler, openFileContext
			#if !CABMINIMAL
			, statusCallback, statusContext
			#endif // !CABMINIMAL
			);
	}

	#if CABMINIMAL
	/// <summary>
	/// Creates a cabinet or chain of cabinets.
	/// </summary>
	/// <param name="nameHandler">Callback for getting the name of each cabinet.</param>
	/// <param name="nameContext">User context object passed to the <paramref name="nameHandler"/>.</param>
	/// <param name="maxCabSize">Maximum size of a single cabinet before starting a new cabinet. For unlimited size specify 0.</param>
	/// <param name="maxFolderSize">Maximum size of a single folder before starting a new folder. For unlimited size specify 0.</param>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="foldersAndFiles">An array of string arrays.  Each string array is a list of files in a folder.</param>
	/// <param name="compLevel">The cabinet compression level.</param>
	/// <param name="openFileHandler">Callback for opening streams for reading included files.</param>
	/// <param name="closeFileHandler">Callback for closing included file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <exception cref="CabinetCreateException">The cabinet could not be created.</exception>
	[CLSCompliant(false)]
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static void Create(CabinetNameHandler nameHandler, object nameContext, long maxCabSize, long maxFolderSize,
		CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler, object openCabContext,
		string[][] foldersAndFiles, CabinetCompressionLevel compLevel, CabinetCreateOpenFileHandler openFileHandler,
		CabinetCreateCloseFileHandler closeFileHandler, object openFileContext)
	#else // !CABMINIMAL
	/// <summary>
	/// Creates a cabinet or chain of cabinets.
	/// </summary>
	/// <param name="nameHandler">Callback for getting the name of each cabinet.</param>
	/// <param name="nameContext">User context object passed to the <paramref name="nameHandler"/>.</param>
	/// <param name="maxCabSize">Maximum size of a single cabinet before starting a new cabinet. For unlimited size specify 0.</param>
	/// <param name="maxFolderSize">Maximum size of a single folder before starting a new folder. For unlimited size specify 0.</param>
	/// <param name="openCabHandler">Callback for opening cabinet streams.</param>
	/// <param name="closeCabHandler">Callback for closing cabinet streams.  This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openCabContext">User context object that will be passed to the
	/// <paramref name="openCabHandler"/> and <paramref name="closeCabHandler"/>.</param>
	/// <param name="foldersAndFiles">An array of string arrays.  Each string array is a list of files in a folder.</param>
	/// <param name="compLevel">The cabinet compression level.</param>
	/// <param name="openFileHandler">Callback for opening streams for reading included files.</param>
	/// <param name="closeFileHandler">Callback for closing included file streams. This may be null, in which
	/// case the stream's <see cref="Stream.Close"/> method will be called.</param>
	/// <param name="openFileContext">User context object that will be passed to the
	/// <paramref name="openFileHandler"/> and <paramref name="closeFileHandler"/>.</param>
	/// <param name="statusCallback">Callback for reporting creation progress.  This may be null
	/// if status is not desired.</param>
	/// <param name="statusContext">User context object passed to the <paramref name="statusCallback"/>.</param>
	/// <exception cref="CabinetCreateException">The cabinet could not be created.</exception>
//	[CLSCompliant(false)]
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
	public static void Create(CabinetNameHandler nameHandler, object nameContext, long maxCabSize, long maxFolderSize,
		CabinetOpenCabHandler openCabHandler, CabinetCloseCabHandler closeCabHandler, object openCabContext,
		string[][] foldersAndFiles, CabinetCompressionLevel compLevel, CabinetCreateOpenFileHandler openFileHandler,
		CabinetCreateCloseFileHandler closeFileHandler, object openFileContext,
		CabinetStatusCallback statusCallback, object statusContext)
	#endif // !CABMINIMAL
	{
		using(CabCreator cabInstance = new CabCreator(maxCabSize, maxFolderSize))
		{
			cabInstance.nameHandler = nameHandler;
			cabInstance.nameContext = nameContext;
			cabInstance.openCabHandler = openCabHandler;
			cabInstance.closeCabHandler = closeCabHandler;
			cabInstance.openCabContext = openCabContext;
			cabInstance.openFileHandler = openFileHandler;
			cabInstance.closeFileHandler = closeFileHandler;
			cabInstance.openFileContext = openFileContext;

			#if !CABMINIMAL
			cabInstance.statusCallback = statusCallback;
			cabInstance.statusContext = statusContext;

			if(cabInstance.statusCallback != null)
			{
				cabInstance.status.totalFolders = (short) foldersAndFiles.Length;
				for(int iFolder = 0; iFolder < foldersAndFiles.Length; iFolder++)
				{
					string[] files = foldersAndFiles[iFolder];
					for(int iFile = 0; iFile < files.Length; iFile++)
					{
						FileAttributes attributes;
						DateTime lastWriteTime;
						Stream fileStream = openFileHandler(files[iFile], out attributes, out lastWriteTime, openFileContext);
						if(fileStream != null)
						{
							cabInstance.status.totalFileBytes += fileStream.Length;
							cabInstance.status.totalFiles++;
						}
						closeFileHandler(files[iFile], fileStream, openFileContext);
					}
				}
			}
			#endif // !CABMINIMAL

			for(int iFolder = 0; iFolder < foldersAndFiles.Length; iFolder++)
			{
				string[] files = foldersAndFiles[iFolder];
				for(int iFile = 0; iFile < files.Length; iFile++)
				{
					FileAttributes attributes;
					DateTime lastWriteTime;
					Stream fileStream = openFileHandler(files[iFile], out attributes, out lastWriteTime, openFileContext);
					if(fileStream != null)
					{
						#if !CABMINIMAL
						if(cabInstance.statusCallback != null)
						{
							if(cabInstance.status.currentFolderTotalBytes > 0)
							{
								cabInstance.status.currentFolderBytesProcessed = cabInstance.status.currentFolderTotalBytes;
								cabInstance.status.statusType = CabinetStatusType.FinishFolder;
								cabInstance.statusCallback(cabInstance.status, cabInstance.statusContext);
								cabInstance.status.currentFolderBytesProcessed = cabInstance.status.currentFolderTotalBytes = 0;

								if(!(iFolder == 0 && iFile == 0))
								{
									cabInstance.status.currentFolderNumber++;
									if(cabInstance.status.totalFolders <= cabInstance.status.currentFolderNumber)
									{
										cabInstance.status.totalFolders = (short) (cabInstance.status.currentFolderNumber + 1);
									}
								}
							}
						}
						#endif // !CABMINIMAL
						cabInstance.status.currentFileName = files[iFile];
						if(!(iFolder == 0 && iFile == 0))
						{
							cabInstance.status.currentFileNumber++;
						}
						#if !CABMINIMAL
						if(cabInstance.statusCallback != null)
						{
							cabInstance.status.currentFileTotalBytes = fileStream.Length;
							cabInstance.status.currentFileBytesProcessed = 0;
							cabInstance.status.statusType = CabinetStatusType.StartFile;
							cabInstance.statusCallback(cabInstance.status, cabInstance.statusContext);
						}
						#endif // !CABMINIMAL

						cabInstance.AddFile(files[iFile], fileStream, attributes, lastWriteTime, false, compLevel);
					}
				}
				cabInstance.FlushFolder();
			}
			cabInstance.FlushCabinet();
		}
	}
	#endif // !CABEXTRACTONLY

	private static Stream DefaultOpenCabHandler(int whichCab, string cabName, object context)
	{
		if(whichCab != 0) return null;
		return new DuplicateStream((Stream) context);
	}

	private static void DefaultCloseCabHandler(int whichCab, string cabName, Stream stream, object context)
	{
		// Don't close the stream. (A null handler indicates auto-close.)
	}
}
}
