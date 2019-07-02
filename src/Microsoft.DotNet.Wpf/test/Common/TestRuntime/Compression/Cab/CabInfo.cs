// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

#if !CABMINIMAL
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.Extract
#else
namespace Microsoft.Test.Compression.Cab
#endif
{

/// <summary>
/// Object representing a cabinet file on disk; provides access to file-based operations on the cabinet file.
/// </summary>
/// <remarks>
/// Generally, the methods on this class are much easier to use than the low-level stream-based methods
/// provided by the <see cref="Cabinet"/> class.
/// </remarks>
internal class CabinetInfo : FileSystemInfo
{
	private FileInfo fileInfo;

	/// <summary>
	/// Creates a new CabinetInfo object representing a cabinet file in a specified path.
	/// </summary>
	/// <param name="path">Path to the cabinet file. When creating a cabinet file, this file may not
	/// necessarily exist yet.</param>
	internal CabinetInfo(string path) : base()
	{
		this.fileInfo = new FileInfo(path);

		// protected instance members inherited from FileSystemInfo:
		this.OriginalPath = path;
		this.FullPath = this.fileInfo.FullName;
	}

	/// <summary>
	/// Gets the directory that contains the cabinet file.
	/// </summary>
    internal DirectoryInfo Directory { get { return this.fileInfo.Directory; } }

	/// <summary>
	/// Gets the name of the directory that contains the cabinet file.
	/// </summary>
    internal string DirectoryName { get { return this.fileInfo.DirectoryName; } }
	
	/// <summary>
	/// Gets the size of the cabinet file.
	/// </summary>
    internal long Length { get { return this.fileInfo.Length; } }
	
	/// <summary>
	/// Gets the filename of the cabinet file.
	/// </summary>
    public override string Name { get { return this.fileInfo.Name; } }
	
	/// <summary>
	/// Checks if the cabinet file exists.
	/// </summary>
    public override bool Exists { get { return File.Exists(this.FullName); } }

	
	/// <summary>
	/// Gets the full path of the cabinet file.
	/// </summary>
	/// <returns>The full path of the cabinet file.</returns>
    public override string ToString() { return this.FullName; }

	
	/// <summary>
	/// Deletes the cabinet file.
	/// </summary>
    public override void Delete() { this.fileInfo.Delete(); }

	
	/// <summary>
	/// Copies an existing cabinet file to another location.
	/// </summary>
	/// <param name="destFileName">The destination file path.</param>
    internal void CopyTo(string destFileName) { this.fileInfo.CopyTo(destFileName); }
	
	/// <summary>
	/// Copies an existing cabinet file to another location, optionally overwriting the destination file.
	/// </summary>
	/// <param name="destFileName">The destination file path.</param>
	/// <param name="overwrite">If true, the destination file will be overwritten if it exists.</param>
    internal void CopyTo(string destFileName, bool overwrite) { this.fileInfo.CopyTo(destFileName, overwrite); }
	
	/// <summary>
	/// Moves an existing cabinet file to another location.
	/// </summary>
	/// <param name="destFileName">The destination file path.</param>
    internal void MoveTo(string destFileName) { this.fileInfo.MoveTo(destFileName); }

	
	/// <summary>
	/// Checks if the cabinet file contains a valid cabinet header.
	/// </summary>
	/// <returns>True if the file is a valid cabinet file; false otherwise.</returns>
    internal virtual bool IsValid()
    {
		using(Stream stream = this.fileInfo.OpenRead())
		{
			return Cabinet.FindCabinetOffset(stream) >= 0;
		}
	}

	/// <summary>
	/// Geats a stream for reading from the cabinet file. For use by subclasses.
	/// </summary>
    protected internal Stream GetCabinetReadStream()
    {
		Stream stream = this.fileInfo.OpenRead();
		long offset = Cabinet.FindCabinetOffset(new DuplicateStream(stream));
		if(offset > 0)
		{
			stream = new OffsetStream(stream, offset);
		}
		return stream;
	}

	/// <summary>
	/// Geats a stream for writing to the cabinet file. For use by subclasses.
	/// </summary>
    protected internal Stream GetCabinetWriteStream()
    {
		Stream stream = this.fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);
		long offset = Cabinet.FindCabinetOffset(new DuplicateStream(stream));

		// If this is not a cabinet file, append the cab to it.
		if(offset < 0) offset = stream.Length;
		
		if(offset > 0)
		{
			stream = new OffsetStream(stream, offset);
		}
		return stream;
	}
	
	/// <summary>
	/// Gets information about the files contained in the cabinet file.
	/// </summary>
	/// <returns>An array of <see cref="CabinetFileInfo"/> objects, each containing information about a file in the cabinet.</returns>
    internal CabinetFileInfo[] GetFiles()
    {
		return this.GetFiles(null, null);
	}

	/// <summary>
	/// Gets information about the certain files contained in the cabinte file.
	/// </summary>
	/// <param name="searchPattern">The search string, such as &quot;*.txt&quot;.</param>
	/// <returns>An array of <see cref="CabinetFileInfo"/> objects, each containing information about a file in the cabinet.</returns>
    internal CabinetFileInfo[] GetFiles(string searchPattern)
    {
		return this.GetFiles(new CabinetFilterFileHandler(CabinetInfo.RegexFilterFileHandler), CabinetInfo.FilePatternToRegex(searchPattern));
	}

	/// <summary>
	/// Gets information about the files contained in the cabinet file. For use by subclasses.
	/// </summary>
	/// <param name="filterFileHandler"></param>
	/// <param name="filterContext"></param>
	/// <returns>An array of <see cref="CabinetFileInfo"/> objects, each containing information about a file in the cabinet.</returns>
	protected internal virtual CabinetFileInfo[] GetFiles(CabinetFilterFileHandler filterFileHandler, object filterContext)
	{
		using(Stream stream = this.GetCabinetReadStream())
		{
			CabinetFileInfo[] files = Cabinet.GetFiles(stream, filterFileHandler, filterContext);
			for(int i = 0; i < files.Length; i++)
			{
				files[i].Cabinet = this;
			}
			return files;
		}
	}

	private static Regex FilePatternToRegex(string pattern)
	{
		return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	}

	private static bool RegexFilterFileHandler(int folder, string name, object filterContext)
	{
		Regex pattern = (Regex) filterContext;
		return pattern.IsMatch(name);
	}

	internal CabinetFileInfo GetFile(string path)
	{
		CabinetFileInfo[] files = this.GetFiles(
			new CabinetFilterFileHandler(this.SingleFileFilterFileHandler), path.ToLower(CultureInfo.InvariantCulture));
		return (files != null && files.Length > 0 ? files[0] : null);
	}

	private bool SingleFileFilterFileHandler(int folder, string name, object filterContext)
	{
		return name.ToLower(CultureInfo.InvariantCulture) == (string) filterContext;
	}
	
	/// <summary>
	/// Extracts all files from a cabinet to a destination directory.
	/// </summary>
	/// <param name="destDirectory">Directory where the files are to be extracted.</param>
    internal void ExtractAll(string destDirectory)
    {
		this.ExtractAll(destDirectory, false, null, null);
	}
	
	/// <summary>
	/// Extracts all files from a cabinet to a destination directory, optionally extracting only newer files.
	/// </summary>
	/// <param name="destDirectory">Directory where the files are to be extracted.</param>
	/// <param name="update">Specify true to only extract files when the timestamp in the cabinet is newer than
	/// the timestamp of the file on disk, when the file already exists.</param>
	/// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
	/// <param name="context">User context object passed to the <paramref name="callback"/>, may be null.</param>
    internal virtual void ExtractAll(string destDirectory, bool update, CabinetStatusCallback callback, object context)
    {
		using(Stream stream = this.GetCabinetReadStream())
		{
			object[] fileContext = new Object[] { destDirectory, null, update };
			Cabinet.Extract(stream,
				new CabinetExtractOpenFileHandler(CabinetInfo.ExtractOpenFileHandler),
				new CabinetExtractCloseFileHandler(CabinetInfo.ExtractCloseFileHandler), fileContext, null, null, callback, context);
		}
	}

	
	/// <summary>
	/// Extracts a single file from the cabinet.
	/// </summary>
	/// <param name="fileName">The name of the file in the cabinet. Also includes the
	/// internal path of the file, if any. File name matching is case-insensitive.</param>
	/// <param name="destFileName">The path where the file is to be extracted on disk.</param>
	/// <remarks>If <paramref name="destFileName"/> already exists, it will be overwritten.</remarks>
    internal void ExtractFile(string fileName, string destFileName)
    {
		if(fileName == null || destFileName == null)
		{
			throw new ArgumentNullException();
		}
		this.ExtractFiles(new string[] { fileName }, null, new string[] { destFileName });
	}

	
	/// <summary>
	/// Extracts multiple files from the cabinet.
	/// </summary>
	/// <param name="fileNames">The names of the files in the cabinet. Each name includes the internal
	/// path of the file, if any. File name matching is case-insensitive.</param>
	/// <param name="destDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="destFileNames"/>.</param>
	/// <param name="destFileNames">The paths where the files are to be extracted on disk. If this parameter is null,
	/// the files will be extracted with the names from the cabinet.</param>
	/// <remarks>
	/// If any extracted files already exist on disk, they will be overwritten.
	/// <p>The <paramref name="destDirectory"/> and <paramref name="destFileNames"/> parameters cannot both be null.</p>
	/// </remarks>
    internal void ExtractFiles(string[] fileNames, string destDirectory, string[] destFileNames)
    {
		this.ExtractFiles(fileNames, destDirectory, destFileNames, false, null, null);
	}
	
	/// <summary>
	/// Extracts multiple files from the cabinet, optionally extracting only newer files.
	/// </summary>
	/// <param name="fileNames">The names of the files in the cabinet. Each name includes the internal
	/// path of the file, if any. File name matching is case-insensitive.</param>
	/// <param name="destDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="destFileNames"/>.</param>
	/// <param name="destFileNames">The paths where the files are to be extracted on disk. If this parameter is null,
	/// the files will be extracted with the names from the cabinet.</param>
	/// <param name="update">Specify true to only extract files when the timestamp in the cabinet is newer than
	/// the timestamp of the file on disk, when the file already exists.</param>
	/// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
	/// <param name="context">User context object passed to the <paramref name="callback"/>, may be null.</param>
	/// <remarks>
	/// If any extracted files already exist on disk, they will be overwritten.
	/// <p>The <paramref name="destDirectory"/> and <paramref name="destFileNames"/> parameters cannot both be null.</p>
	/// </remarks>
    internal void ExtractFiles(string[] fileNames, string destDirectory, string[] destFileNames, bool update, CabinetStatusCallback callback, object context)
	{
		if(fileNames == null)
		{
			throw new ArgumentNullException("fileNames");
		}
		if(destFileNames == null)
		{
			if(destDirectory == null)
			{
				throw new ArgumentNullException("destFileNames");
			}
			destFileNames = fileNames;
		}
		this.ExtractFileSet(CreateStringDictionary(fileNames, destFileNames), destDirectory, update, callback, context);
	}

	
	/// <summary>
	/// Extracts multiple files from the cabinet.
	/// </summary>
	/// <param name="filenameMap">A mapping from internal file paths to external file paths.
	/// Case-senstivity when matching internal paths depends on the IDictionary implementation.</param>
	/// <param name="destDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative external paths in fileNameMap"</param>
	/// <remarks>
	/// If any extracted files already exist on disk, they will be overwritten.
	/// </remarks>
    internal void ExtractFileSet(IDictionary filenameMap, string destDirectory)
    {
		this.ExtractFileSet(filenameMap, destDirectory, false, null, null);
	}

    /// <summary>
    /// Extracts multiple files from the cabinet. If any extracted files already exist on disk, they will be overwritten.
    /// </summary>
    /// <param name="filenameMap">A mapping from internal file paths to external file paths.
    /// Case-senstivity when matching internal paths depends on the IDictionary implementation.</param>
    /// <param name="destDirectory">This parameter may be null, but if specified it is the root directory
    /// for any relative external paths in <paramref name="filenameMap"/>.</param>
    /// <param name="update">Specify true to only extract files when the timestamp in the cabinet is newer than
    /// the timestamp of the file on disk, when the file already exists.</param>
    /// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
    /// <param name="context">User context object passed to the <paramref name="callback"/>, may be null.</param>
    internal virtual void ExtractFileSet(IDictionary filenameMap, string destDirectory, bool update, CabinetStatusCallback callback, Object context)
    {
		if(filenameMap == null)
		{
			throw new ArgumentNullException("filenameMap");
		}
		using(Stream stream = this.GetCabinetReadStream())
		{
			object[] fileContext = new Object[] { destDirectory, filenameMap, update };
			Cabinet.Extract(stream, new CabinetExtractOpenFileHandler(CabinetInfo.ExtractOpenFileHandler),
				new CabinetExtractCloseFileHandler(CabinetInfo.ExtractCloseFileHandler), fileContext,
				new CabinetFilterFileHandler(this.FilterFileHandler), filenameMap, callback, context);
		}
	}

	#if !CABEXTRACTONLY
	/// <summary>
	/// Compresses all files in a directory into the cabinet file. Does not include subdirectories.
	/// </summary>
	/// <param name="sourceDirectory">The directory containing the files to be included.</param>
	/// <remarks>
	/// Uses maximum compression level.
	/// </remarks>
    internal void CompressDirectory(string sourceDirectory)
    {
		this.CompressDirectory(sourceDirectory, false, CabinetCompressionLevel.Max, null, null);
	}

    /// <summary>
    /// Compresses all files in a directory into the cabinet file, optionally including subdirectories.
    /// The files are stored in the cabinet using their relative file paths in the directory tree.
    /// Note, while this library fully supports an internal directory structure in cabinets,
    /// some extraction tools do not.
    /// </summary>
    /// <param name="sourceDirectory">This parameter may be null, but if specified it is the root directory
    /// for any relative paths in source file.</param>
    /// <param name="includeSubdirectories">If true, recursively include files in subdirectories.</param>
    /// <param name="compLevel">The compression level used when creating the cabinet.</param>
    /// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
    /// <param name="context">User context object passed to the callback, may be null.</param>   
    internal void CompressDirectory(string sourceDirectory, bool includeSubdirectories, CabinetCompressionLevel compLevel, CabinetStatusCallback callback, object context)
	{
		string[] files = GetRelativeFilePathsInDirectoryTree(sourceDirectory, includeSubdirectories);
		this.CompressFiles(sourceDirectory, files, files, compLevel, callback, context);
	}

	internal static string[] GetRelativeFilePathsInDirectoryTree(string dir, bool includeSubdirectories)
	{
		ArrayList fileList = new ArrayList();
		GetRelativeFilePathsInDirectoryTree(dir, "", includeSubdirectories, fileList);
		return (string[]) fileList.ToArray(typeof(string));
	}
	private static void GetRelativeFilePathsInDirectoryTree(string dir, string relativeDir,
		bool includeSubdirectories, IList fileList)
	{
		foreach(string file in System.IO.Directory.GetFiles(dir))
		{
			string fileName = Path.GetFileName(file);
			fileList.Add(Path.Combine(relativeDir, fileName));
		}
		if(includeSubdirectories)
		{
			foreach(string subDir in System.IO.Directory.GetDirectories(dir))
			{
				string subDirName = Path.GetFileName(subDir);
				GetRelativeFilePathsInDirectoryTree(Path.Combine(dir, subDirName),
					Path.Combine(relativeDir, subDirName), includeSubdirectories, fileList);
			}
		}
	}
	
	/// <summary>
	/// Compresses files into the cabinet file, specifying the names used to store the files in the cabinet.
	/// </summary>
	/// <param name="sourceDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="sourceFileNames"/>.</param>
	/// <param name="sourceFileNames">The list of files to be included in the cabinet.</param>
	/// <param name="fileNames">The names of the files as they are stored in the cabinet. Each name
	/// includes the internal path of the file, if any. This parameter may be null, in which case the
	/// files are stored in the cabinet with their source file names and no path information.</param>
	/// <remarks>
	/// Uses maximum compression level.
	/// <p>Duplicate items in the <paramref name="fileNames"/> array will cause a
	/// <see cref="CabinetCreateException"/>.</p>
	/// </remarks>
    internal void CompressFiles(string sourceDirectory, string[] sourceFileNames, string[] fileNames)
    {
		this.CompressFiles(sourceDirectory, sourceFileNames, fileNames, CabinetCompressionLevel.Max, null, null);
	}
	
	/// <summary>
	/// Compresses files into the cabinet file, specifying the names used to store the files in the cabinet.
	/// </summary>
	/// <param name="sourceDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="sourceFileNames"/>.</param>
	/// <param name="sourceFileNames">The list of files to be included in the cabinet.</param>
	/// <param name="fileNames">The names of the files as they are stored in the cabinet. Each name
	/// includes the internal path of the file, if any. This parameter may be null, in which case the
	/// files are stored in the cabinet with their source file names and no path information.</param>
	/// <param name="compLevel">The compression level used when creating the cabinet.</param>
	/// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
	/// <param name="context">User context object passed to the <paramref name="callback"/>, may be null.</param>
	/// <remarks>
	/// Duplicate items in the <paramref name="fileNames"/> array will cause a <see cref="CabinetCreateException"/>.
	/// </remarks>
    internal virtual void CompressFiles(string sourceDirectory, string[] sourceFileNames, string[] fileNames, CabinetCompressionLevel compLevel, CabinetStatusCallback callback, object context)
	{
		if(sourceFileNames == null)
		{
			if(sourceDirectory == null)
			{
				throw new ArgumentNullException("sourceDirectory", "Either sourceDirectory or sourceFileNames must be non-null.");
			}
			sourceFileNames = System.IO.Directory.GetFiles(sourceDirectory);
		}
		if(fileNames == null)
		{
			fileNames = new string[sourceFileNames.Length];
			for(int i = 0; i < sourceFileNames.Length; i++)
			{
				fileNames[i] = Path.GetFileName(sourceFileNames[i]);
			}
		}

		using(Stream stream = this.GetCabinetWriteStream())
		{
			Cabinet.Create(stream, fileNames, compLevel,
				new CabinetCreateOpenFileHandler(CabinetInfo.CreateOpenFileHandler),
				new CabinetCreateCloseFileHandler(CabinetInfo.CreateCloseFileHandler),
				new object[] { sourceDirectory, CreateStringDictionary(fileNames, sourceFileNames) }, callback, context);
		}
	}
	
	/// <summary>
	/// Compresses files into the cabinet file, specifying the names used to store the files in the cabinet.
	/// </summary>
	/// <param name="sourceDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="filenameMap"/>.</param>
	/// <param name="filenameMap">A mapping from internal file paths to external file paths.</param>
	/// <remarks>
	/// Uses maximum compression level.
	/// </remarks>
    internal void CompressFileSet(string sourceDirectory, IDictionary filenameMap)
    {
		this.CompressFileSet(sourceDirectory, filenameMap, CabinetCompressionLevel.Max, null, null);
	}
	
	/// <summary>
	/// Compresses files into the cabinet file, specifying the names used to store the files in the cabinet.
	/// </summary>
	/// <param name="sourceDirectory">This parameter may be null, but if specified it is the root directory
	/// for any relative paths in <paramref name="filenameMap"/>.</param>
	/// <param name="filenameMap">A mapping from internal file paths to external file paths.</param>
	/// <param name="compLevel">The compression level used when creating the cabinet.</param>
	/// <param name="callback">Handler for receiving progress information; this may be null if progress is not desired.</param>
	/// <param name="context">User context object passed to the <paramref name="callback"/>, may be null.</param>
    internal virtual void CompressFileSet(string sourceDirectory, IDictionary filenameMap, CabinetCompressionLevel compLevel, CabinetStatusCallback callback, object context)
	{
		if(filenameMap == null)
		{
			throw new ArgumentNullException("filenameMap");
		}

		string[] fileNames = new string[filenameMap.Count];
		filenameMap.Keys.CopyTo(fileNames, 0);

		using(Stream stream = this.GetCabinetWriteStream())
		{
			Cabinet.Create(stream, fileNames, compLevel,
				new CabinetCreateOpenFileHandler(CabinetInfo.CreateOpenFileHandler),
				new CabinetCreateCloseFileHandler(CabinetInfo.CreateCloseFileHandler),
				new object[] { sourceDirectory, filenameMap }, callback, context);
		}
	}
	#endif // !CABEXTRACTONLY

	private static IDictionary CreateStringDictionary(string[] keys, string[] values)
	{
		if(keys.Length != values.Length) throw new ArgumentOutOfRangeException();
                #pragma warning disable 618
		IDictionary stringDict = new Hashtable(
			new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture),
			new CaseInsensitiveComparer(CultureInfo.InvariantCulture));
                #pragma warning restore 618
		for(int i = 0; i < keys.Length; i++) stringDict[keys[i]] = values[i];
		return stringDict;
	}

	/// <summary>
	/// Callback which opens a file being extracted from a cabinet.  For use by subclasses.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="fileSize"></param>
	/// <param name="lastWriteTime"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	protected internal static Stream ExtractOpenFileHandler(string name, long fileSize, DateTime lastWriteTime, object context)
	{
		string destDir = (string) ((object[]) context)[0];
		IDictionary filenameMap = (IDictionary) ((object[]) context)[1];
		bool update = (Boolean) ((object[]) context)[2];

		string filePath = name;
		if(filenameMap != null && filenameMap[name] != null)
		{
			filePath = filenameMap[name].ToString();
		}
		if(destDir != null)
		{
			filePath = Path.Combine(destDir, filePath);
		}
		FileInfo fileInfo = new FileInfo(filePath);

		if(fileInfo.Exists)
		{
			if(update && lastWriteTime != DateTime.MinValue)
			{
				DateTime diskFileLastWriteTime = fileInfo.LastWriteTime;
				if(diskFileLastWriteTime >= lastWriteTime)
				{
					return null;
				}
			}
			if((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
			{
				fileInfo.Attributes &= ~FileAttributes.ReadOnly;
			}
		}
		if(!fileInfo.Directory.Exists)
		{
			fileInfo.Directory.Create();
		}
		return File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
	}

	/// <summary>
	/// Callback which closes a file that was extracted from a cabinet.  For use by subclasses.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="stream"></param>
	/// <param name="attributes"></param>
	/// <param name="lastWriteTime"></param>
	/// <param name="context"></param>
	protected internal static void ExtractCloseFileHandler(string name, Stream stream, FileAttributes attributes, DateTime lastWriteTime, object context)
	{
		stream.Close();

		string destDir = (string) ((object[]) context)[0];
		IDictionary filenameMap = (IDictionary) ((object[]) context)[1];

		string filePath = name;
		if(filenameMap != null && filenameMap[name] != null)
		{
			filePath = filenameMap[name].ToString();
		}
		if(destDir != null)
		{
			filePath = Path.Combine(destDir, filePath);
		}
		FileInfo fileInfo = new FileInfo(filePath);
		if(lastWriteTime != DateTime.MinValue)
		{
			try { fileInfo.LastWriteTime = lastWriteTime; } 
			catch(ArgumentException) { }
			catch(IOException) { }
		}
		try { fileInfo.Attributes = attributes; }
		catch(IOException) { }
	}

	#if !CABEXTRACTONLY
	/// <summary>
	/// Callback which opens a file for inclusion in a cabinet.  For use by subclasses.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="attributes"></param>
	/// <param name="lastWriteTime"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	protected internal static Stream CreateOpenFileHandler(string name, out FileAttributes attributes, out DateTime lastWriteTime, object context)
	{
		string sourceDir = (string) ((object[]) context)[0];
		IDictionary filenameMap = (IDictionary) ((object[]) context)[1];

		string path = name;
		if(filenameMap != null && filenameMap[name] != null)
		{
			path = (string) filenameMap[name];
		}

		if(path.Length == 0)
		{
			attributes = FileAttributes.Normal;
			lastWriteTime = DateTime.Now;
			return null;
		}

		if(sourceDir != null)
		{
			path = Path.Combine(sourceDir, path);
		}

		attributes = File.GetAttributes(path);
		lastWriteTime = File.GetLastWriteTime(path);
		return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
	}

	/// <summary>
	/// Callback which closes a files that was included in a cabinet.  For use by subclasses.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="stream"></param>
	/// <param name="context"></param>
	protected internal static void CreateCloseFileHandler(string name, Stream stream, object context)
	{
		stream.Close();
	}
	#endif // !CABEXTRACTONLY

	private bool FilterFileHandler(int folder, string name, object filterContext)
	{
		// Whether this is case-sensitive is up to the IDictionary implementation.
		IDictionary filenameMap = (IDictionary) filterContext;
		return filenameMap.Contains(name);
	}
}


/// <summary>
/// Object representing a compressed file within a cabinet file; provides operations for getting
/// the file properties and extracting the file.
/// </summary>
internal class CabinetFileInfo : FileSystemInfo
{
	private CabinetInfo       cabinetInfo;
	private string            name;
	private string            path;
	private bool              initialized;
	private bool              exists;
	private int               cabFolderNumber;
	private FileAttributes    attributes;
	private DateTime          lastWriteTime;
	private long              length;
	private int               cabNumber;

	/// <summary>
	/// Creates a new CabinetFileInfo object. For use by subclasses.
	/// </summary>
	protected internal CabinetFileInfo(string name, string path, int folderNumber, int cabNumber,
		FileAttributes attributes, DateTime lastWriteTime, long length) : base()
	{
		this.cabinetInfo    = null;
		this.name           = name;
		this.path           = path;
		this.initialized    = true;
		this.exists         = true;
		this.cabFolderNumber= folderNumber;
		this.cabNumber      = cabNumber;
		this.attributes     = attributes;
		this.lastWriteTime  = lastWriteTime;
		this.length         = length;

		this.OriginalPath   = null;
		this.FullPath       = null;
	}
	
	/// <summary>
	/// Creates a new CabinetFileInfo object representing a file within a cabinet in a specified path.
	/// </summary>
	/// <param name="cabinetInfo">Object representing the cabinet containing the file.</param>
	/// <param name="filePath">Path to the file within the cabinet. Usually, this is a simple file
	/// name, but if the cabinet contains a directory structure this may include the directory.</param>
	public CabinetFileInfo(CabinetInfo cabinetInfo, string filePath) : base()
	{
		this.Cabinet = cabinetInfo;

		if(filePath == null)
		{
			throw new ArgumentNullException("filePath");
		}

		this.name            = System.IO.Path.GetFileName(filePath);
		this.path            = System.IO.Path.GetDirectoryName(filePath);
		this.initialized     = false;
		this.exists          = false;
		this.cabFolderNumber = 0;
		this.cabNumber       = 0;
		this.attributes      = FileAttributes.Normal;
		this.lastWriteTime   = DateTime.MinValue;
		this.length          = 0;
	}

	
	/// <summary>
	/// Gets or sets the cabinet that contains this file.
	/// </summary>
	public CabinetInfo Cabinet
	{
		get
		{
			return this.cabinetInfo;
		}
		set
		{
			if(value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.cabinetInfo = value;

			// protected instance members inherited from FileSystemInfo:
			this.OriginalPath = value.FullName;
			this.FullPath     = value.FullName;
		}
	}
	
	/// <summary>
	/// Gets the file name of the cabinet that contains this file.
	/// </summary>
	public string CabinetName      { get { return this.Cabinet != null ? this.Cabinet.FullName : null; } }
	
	/// <summary>
	/// Gets the number of the cabinet that contains this file, if the cabinet is part of a chain.
	/// </summary>
	public int CabinetNumber       { get { return this.cabNumber; } }
	
	/// <summary>
	/// Gets the folder number within the cabinet that contains this file.
	/// </summary>
	public int CabinetFolderNumber { get { if(!initialized) this.Refresh(); return this.cabFolderNumber; } }

	
	/// <summary>
	/// Gets the name of the file.
	/// </summary>
	public override string Name    { get { return this.name; } }
	
	/// <summary>
	/// Gets the internal path of the file in the cab (not including the file name).
	/// </summary>
	public string Path             { get { return this.path; } }
	
	/// <summary>
	/// Gets the full path to the file.
	/// </summary>
	/// <remarks>
	/// For example, the path <c>"C:\archive.cab\file.txt"</c> refers to a file "file.txt" inside the
	/// cabinet "archive.cab".
	/// </remarks>
	public override string FullName  { get { return System.IO.Path.Combine(this.CabinetName, System.IO.Path.Combine(this.Path, this.Name)); } }
	
	/// <summary>
	/// Checks if the file exists within the cabinet.
	/// </summary>
	public override bool   Exists    { get { if(!initialized) this.Refresh(); return this.exists; } }
	
	/// <summary>
	/// Gets the uncompressed size of the file.
	/// </summary>
	public long            Length    { get { if(!initialized) this.Refresh(); return this.length; } }

	
	/// <summary>
	/// Gets the attributes of the file.
	/// </summary>
	public new FileAttributes Attributes    { get { if(!initialized) this.Refresh(); return this.attributes; } }
	
	/// <summary>
	/// Gets the last modification time of the file.
	/// </summary>
	public new DateTime       LastWriteTime { get { if(!initialized) this.Refresh(); return this.lastWriteTime; } }

	
	/// <summary>
	/// Gets the full path to the file.
	/// </summary>
	/// <returns><see cref="FullName"/></returns>
	public override string ToString() { return this.FullName; }

	
	/// <summary>
	/// Deletes the file.  NOT SUPPORTED.
	/// </summary>
	/// <exception cref="NotSupportedException">Files cannot be deleted from an existing cabinet.</exception>
	public override void Delete() { throw new NotSupportedException(); }

	
	/// <summary>
	/// Refreshes the attributes and other cached information about the file,
	/// by re-reading the information from the cabinet.
	/// </summary>
	public new void Refresh()
	{
		base.Refresh();

		string filePath = System.IO.Path.Combine(this.Path, this.Name);
		CabinetFileInfo updatedFile = Cabinet.GetFile(filePath);
		if(updatedFile == null)
		{
			throw new FileNotFoundException("File not found in cabinet.", filePath);
		}

		this.exists = updatedFile.exists;
		this.length = updatedFile.length;
		this.attributes = updatedFile.attributes;
		this.lastWriteTime = updatedFile.lastWriteTime;
		this.cabFolderNumber = updatedFile.cabFolderNumber;
	}

	/// <summary>
	/// Extracts the file.
	/// </summary>
	/// <param name="destFileName">The destination path where the file will be extracted.</param>
	public void CopyTo(string destFileName) { CopyTo(destFileName, false); }
	
	/// <summary>
	/// Extracts the file, optionally overwriting any existing file.
	/// </summary>
	/// <param name="destFileName">The destination path where the file will be extracted.</param>
	/// <param name="overwrite">If true, <paramref name="destFileName"/> will be overwritten if it exists.</param>
	/// <exception cref="IOException"><paramref name="overwrite"/> is false and <paramref name="destFileName"/> exists.</exception>
	public void CopyTo(string destFileName, bool overwrite)
	{
		if(destFileName == null)
		{
			throw new ArgumentNullException("destFileName");
		}

		if(!overwrite && File.Exists(destFileName))
		{
			throw new IOException("File already exists; not overwriting.");
		}

		Cabinet.ExtractFile(System.IO.Path.Combine(this.Path, this.Name), destFileName);
	}
}
}
#endif // !CABMINIMAL
