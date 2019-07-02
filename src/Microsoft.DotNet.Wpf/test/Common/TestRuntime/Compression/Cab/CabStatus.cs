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


#if !CABMINIMAL
/// <summary>
/// Handles status messages generated during creation or extraction of a cabinet file.
/// </summary>
/// <param name="status">Status data.</param>
/// <param name="context">User context object.</param>
/// <remarks>
/// The handler may choose to ignore some types of message types.  For example, if the handler
/// will only list each file as it is compressed/extracted, it can ignore messages that
/// are not of type <see cref="CabinetStatusType.FinishFile"/>.
/// </remarks>
internal delegate void CabinetStatusCallback(CabinetStatus status, object context);

/// <summary>
/// The type of status message.
/// </summary>
/// <remarks>
/// <p>EXTRACTION EXAMPLE: The following sequence of messages might be received when
/// extracting a simple cabinet file with 2 files.</p>
/// <list type="table">
/// <listheader><term>Message Type</term><description>Description</description></listheader>
/// <item><term>StartCab</term>     <description>Begin extracting cabinet</description></item>
/// <item><term>StartFolder</term>  <description>Begin extracting cabinet folder</description></item>
/// <item><term>StartFile</term>    <description>Begin extracting first file</description></item>
/// <item><term>PartialFile</term>  <description>Extracting first file</description></item>
/// <item><term>PartialFile</term>  <description>Extracting first file</description></item>
/// <item><term>FinishFile</term>   <description>Finished extracting first file</description></item>
/// <item><term>StartFile</term>    <description>Begin extracting second file</description></item>
/// <item><term>PartialFile</term>  <description>Extracting second file</description></item>
/// <item><term>FinishFile</term>   <description>Finished extracting second file</description></item>
/// <item><term>FinishFolder</term> <description>Finished extracting cabinet folder</description></item>
/// <item><term>FinishCab</term>    <description>Finished extracting cabinet</description></item>
/// </list>
/// <p></p>
/// <p>CREATION EXAMPLE:  Cabbing 3 files into 2 cabs, where the second file is
///	continued to the second cab, and the third file has its own folder.</p>
/// <list type="table">
/// <listheader><term>Message Type</term><description>Description</description></listheader>
/// <item><term>StartFile</term>    <description>Begin compressing first file</description></item>
/// <item><term>FinishFile</term>   <description>Finished compressing first file</description></item>
/// <item><term>StartFile</term>    <description>Begin compressing second file</description></item>
/// <item><term>PartialFile</term>  <description>Compressing second file</description></item>
/// <item><term>PartialFile</term>  <description>Compressing second file</description></item>
/// <item><term>FinishFile</term>   <description>Finished compressing second file</description></item>
/// <item><term>StartFolder</term>  <description>Begin creating first folder</description></item>
/// <item><term>PartialFolder</term><description>Creating first folder</description></item>
/// <item><term>FinishFolder</term> <description>Finished creating first folder</description></item>
/// <item><term>StartCab</term>     <description>Begin writing first cab</description></item>
/// <item><term>PartialCab</term>   <description>Writing first cab</description></item>
/// <item><term>FinishCab</term>    <description>Finished writing first cab</description></item>
/// <item><term>StartFolder</term>  <description>Begin creating continuation of first folder</description></item>
/// <item><term>PartialFolder</term><description>Creating continuation of first folder</description></item>
/// <item><term>FinishFolder</term> <description>Finished creating continuation of first folder</description></item>
/// <item><term>StartFile</term>    <description>Begin compressing third file</description></item>
/// <item><term>PartialFile</term>  <description>Compressing third file</description></item>
/// <item><term>FinishFile</term>   <description>Finished compressing third file</description></item>
/// <item><term>StartFolder</term>  <description>Begin creating second folder</description></item>
/// <item><term>PartialFolder</term><description>Creating second folder</description></item>
/// <item><term>FinishFolder</term> <description>Finished creating second folder</description></item>
/// <item><term>StartCab</term>     <description>Begin writing second cab</description></item>
/// <item><term>PartialCab</term>   <description>Writing second cab</description></item>
/// <item><term>FinishCab</term>    <description>Finished writing second cab</description></item>
/// </list>
/// </remarks>
internal enum CabinetStatusType : int
{
	/// <summary>Status message before beginning the compression or extraction of an individual file.</summary>
	StartFile,
	/// <summary>Status message (possibly reported multiple times) during the process of compressing or extracting a file.</summary>
	PartialFile,
	/// <summary>Status message after completion of the compression or extraction of an individual file.</summary>
	FinishFile,
	/// <summary>Status message before beginning the compression or extraction of a folder in the cabinet.</summary>
	StartFolder,
	/// <summary>Status message (possibly reported multiple times) during the process of compressing or extracting a folder in the cabinet.</summary>
	PartialFolder,
	/// <summary>Status message after completion of the compression or extraction of an individual file.</summary>
	FinishFolder,
	/// <summary>Status message before beginning the compression or extraction of a cabinet file.</summary>
	StartCab,
	/// <summary>Status message (possibly reported multiple times) during the process of compressing or extracting a cabinet file.</summary>
	PartialCab,
	/// <summary>Status message after completion of the compression or extraction of an individual file.</summary>
	FinishCab,
	/// <summary>Status message sent while analyzing input parameters before compresion.</summary>
	Analyzing,
	/// <summary>Status message sent while deconstructing files before compresion.</summary>
	Deconstructing,
	/// <summary>Status message sent while reconstructing files after decompression.</summary>
	Reconstructing,
}

/// <summary>
/// Contains the data reported in a cabinet status message.
/// </summary>
internal class CabinetStatus
#else
internal class CabinetStatus
#endif // CABMINIMAL
{
	internal CabinetStatus()
	{
	}

	#if !CABMINIMAL
	/// <summary>Creates a new CabinetStatus object.</summary>
	protected internal CabinetStatus(CabinetStatusType statusType,
		string currentFileName, int currentFileNumber, int totalFiles,
		long currentFileBytesProcessed, long currentFileTotalBytes,
		short currentFolderNumber, short totalFolders,
		long currentFolderBytesProcessed, long currentFolderTotalBytes,
		string currentCabinetName, short currentCabinetNumber, short totalCabinets,
		long fileBytesProcessed, long totalFileBytes) : this()
	{
		this.statusType = statusType;
		this.currentFileName = currentFileName;
		this.currentFileNumber = currentFileNumber;
		this.totalFiles = totalFiles;
		this.currentFileBytesProcessed = currentFileBytesProcessed;
		this.currentFileTotalBytes = currentFileTotalBytes;
		this.currentFolderNumber = currentFolderNumber;
		this.totalFolders = totalFolders;
		this.currentFolderBytesProcessed = currentFolderBytesProcessed;
		this.currentFolderTotalBytes = currentFolderTotalBytes;
		this.currentCabinetName = currentCabinetName;
		this.currentCabinetNumber = currentCabinetNumber;
		this.totalCabinets = totalCabinets;
		this.fileBytesProcessed = fileBytesProcessed;
		this.totalFileBytes = totalFileBytes;
	}

	internal CabinetStatusType statusType;
	#endif // !CABMINIMAL

	internal string currentFileName;
	internal int   currentFileNumber;
	#if !CABMINIMAL
	internal int   totalFiles;
	internal long  currentFileBytesProcessed;
	internal long  currentFileTotalBytes;
	#endif // !CABMINIMAL

	internal short currentFolderNumber;
	#if !CABMINIMAL
	internal short totalFolders;
	internal long  currentFolderBytesProcessed;
	internal long  currentFolderTotalBytes;
	#endif // !CABMINIMAL

	internal string currentCabinetName;
	internal short currentCabinetNumber;
	internal short totalCabinets;
	#if !CABMINIMAL
	internal long  currentCabinetBytesProcessed;
	internal long  currentCabinetTotalBytes;

	internal long  fileBytesProcessed;
	internal long  totalFileBytes;

	/// <summary>
	/// The type of status message.
	/// </summary>
	/// <remarks>
	/// The handler may choose to ignore some types of message types.  For example, if the handler
	/// will only list each file as it is compressed/extracted, it can ignore messages that
	/// are not of type <see cref="CabinetStatusType.FinishFile"/>.
	/// </remarks>
	public CabinetStatusType StatusType        { get { return statusType; } }

	/// <summary>
	/// The name of the file being processed. (The name of the file within the cabinet; not the external
	/// file path.) Also includes the internal path of the file, if any.  Valid for
	/// <see cref="CabinetStatusType.StartFile"/>, <see cref="CabinetStatusType.PartialFile"/>,
	/// and <see cref="CabinetStatusType.FinishFile"/> messages.
	/// </summary>
	public string CurrentFileName              { get { return currentFileName; } }

	/// <summary>
	/// The number of the current file being processed. The first file is number 0, and the last file
	/// is <see cref="TotalFiles"/>-1. Valid for <see cref="CabinetStatusType.StartFile"/>,
	/// <see cref="CabinetStatusType.PartialFile"/>, and <see cref="CabinetStatusType.FinishFile"/> messages.
	/// </summary>
	public int    CurrentFileNumber            { get { return currentFileNumber; } }
	
	/// <summary>
	/// The total number of files to be processed.  Valid for all message types.
	/// </summary>
	public int    TotalFiles                   { get { return totalFiles; } }
	
	/// <summary>
	/// The number of bytes processed so far when compressing or extracting a file.  Valid for
	/// <see cref="CabinetStatusType.StartFile"/>, <see cref="CabinetStatusType.PartialFile"/>,
	/// and <see cref="CabinetStatusType.FinishFile"/> messages.
	/// </summary>
	public long   CurrentFileBytesProcessed    { get { return currentFileBytesProcessed; } }
	
	/// <summary>
	/// The total number of bytes in the current file.  Valid for <see cref="CabinetStatusType.StartFile"/>,
	/// <see cref="CabinetStatusType.PartialFile"/>, and <see cref="CabinetStatusType.FinishFile"/> messages.
	/// </summary>
	public long   CurrentFileTotalBytes        { get { return currentFileTotalBytes; } }

	/// <summary>
	/// The number of the current folder being processed. The first folder is number 0, and the last folder
	/// is <see cref="TotalFolders"/>-1. (Simple cabinets only have one folder.) Valid for
	/// <see cref="CabinetStatusType.StartFile"/>, <see cref="CabinetStatusType.PartialFile"/>,
	/// <see cref="CabinetStatusType.FinishFile"/>, <see cref="CabinetStatusType.StartFolder"/>,
	/// <see cref="CabinetStatusType.PartialFolder"/>, and <see cref="CabinetStatusType.FinishFolder"/> messages.
	/// </summary>
	public short  CurrentFolderNumber          { get { return currentFolderNumber; } }
	
	/// <summary>
	/// The total number folders to be processed.  Valid for all message types.
	/// </summary>
	public short  TotalFolders                 { get { return totalFolders; } }
	
	/// <summary>
	/// The number of bytes processed so far when flushing a folder.  Valid for
	/// <see cref="CabinetStatusType.PartialFolder"/> compression status messages.
	/// </summary>
	public long   CurrentFolderBytesProcessed  { get { return currentFolderBytesProcessed; } }
	
	/// <summary>
	/// The total number of bytes to be processed when flushing a folder.  Valid for
	/// <see cref="CabinetStatusType.PartialFolder"/> compression status messages.
	/// </summary>
	public long   CurrentFolderTotalBytes      { get { return currentFolderTotalBytes; } }

	/// <summary>
	/// The name of the current cabinet.  Not necessarily the name of the cabinet on disk.
	/// Valid for all message types.
	/// </summary>
	public string CurrentCabinetName           { get { return currentCabinetName; } }
	
	/// <summary>
	/// The current cabinet number, when processing a chained set of cabinets. The first cabinet is
	/// number 0, and the last cabinet is <see cref="TotalCabinets"/>-1. Valid for all message types.
	/// </summary>
	public short  CurrentCabinetNumber         { get { return currentCabinetNumber; } }
	
	/// <summary>
	/// The total number of cabinets in a chained set. Valid for all message types.  However when
	/// using the compression feature to auto-split into multiple cabinets based on data size,
	/// this value will not be accurate until the end.
	/// </summary>
	public short  TotalCabinets                { get { return totalCabinets; } }
	
	/// <summary>
	/// The number of compressed bytes processed so far during an extraction. Valid for all
	/// extraction messages.
	/// </summary>
	public long   CurrentCabinetBytesProcessed { get { return currentCabinetBytesProcessed; } }
	
	/// <summary>
	/// The total number of compressed bytes to be processed during an extraction. Valid for all
	/// extraction messages.
	/// </summary>
	public long   CurrentCabinetTotalBytes     { get { return currentCabinetTotalBytes; } }

	/// <summary>
	/// The number of uncompressed file bytes processed so far. Valid for all message types.  When
	/// compared to <see cref="TotalFileBytes"/>, this can be used as a measure of overall progress.
	/// </summary>
	public long   FileBytesProcessed           { get { return fileBytesProcessed; } }
	
	/// <summary>
	/// The total number of uncompressed file bytes to be processed.  Valid for all message types.
	/// </summary>
	public long   TotalFileBytes               { get { return totalFileBytes; } }
	#endif // !CABMINIMAL
}

}
