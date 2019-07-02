// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.Compression
{
    #region using   
        using System;
        using System.IO;
        using System.Xml;
        using System.Data;
        using System.Text;
        using System.Collections;
        using System.Security.Permissions;
        using System.Security.Cryptography;
        using System.Runtime.Serialization;
        using System.Runtime.InteropServices;
        using System.Text.RegularExpressions;
        using Microsoft.Test.Compression.Cab;
    #endregion

    #region Enums
        /// <summary>
        /// The type of the Archive to be created
        /// </summary>
        public enum ArcType 
        { 
            /// <summary>
            /// Archive is a sequence
            /// </summary>
            Sequence, 
            /// <summary>
            /// Archive is a generic type
            /// </summary>
            GenericArchive, 
            /// <summary>
            /// Archive is of unknown type
            /// </summary>
            Unknown 
        };
    #endregion

    #region Interfaces
        /// <summary>
        /// Interface for accessing the Archive
        /// </summary>
        public interface IArchiveItemsAdapter 
        {
            /// <summary>
            /// Get/set the name of the archive to open
            /// </summary>
            /// <value></value>
            string ArchiveName
            {
                get;
            }
            /// <summary>
            /// Get/set the value associated with this index as an array of byte.
            /// Note : The change made using the indexer setter, won't be serialized until Update() is called
            /// </summary>
            /// <value></value>
            byte[] this[string key]
            {
                get;
                set;
            }
            /// <summary>
            /// Get a collection of all the keys defined in this archive
            /// Note : Keys are sorted 
            /// </summary>
            /// <value></value>
            string[] Keys
            {
                get;
            }
            /// <summary>
            /// Serialize the change (change made thru the indexer setter are commited)
            /// </summary>
            void Update();
        }
    #endregion

    #region CompressionException
        /// <summary>
        /// Represents errors that occur during Compression.
        /// </summary>
        public class CompressionException: Exception
        {
            /// <summary>
            /// Initializes a new instance of the CompressionException class.
            /// </summary>
            public CompressionException() : base()
            {
            }
            /// <summary>
            /// Initializes a new instance of the CompressionException class with a specified error message.
            /// </summary>
            /// <param name="message">The error message that explains the reason for the exception.</param>
            public CompressionException(string message) : base(message)
            { 
            }
            /// <summary>
            /// Initializes a new instance of the CompressionException class with a specified error message and a reference to the inner exception that is the cause of this exception.
            /// </summary>
            /// <param name="message">The error message that explains the reason for the exception.</param>
            /// <param name="innerException">The exception that is the cause of the current exception. If the innerException parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block that handles the inner exception.</param>
            public CompressionException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    #endregion  CompressionException

    #region FileEntryDescriptor storage Helper class
        /// <summary>
        /// Storage helper for files in the archives
        /// </summary>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public class FileEntryDescriptor 
        {
            #region Properties
                internal Archiver Root = null;
                /// <summary>
                /// Set to true will delete the file after cabing
                /// If set to false, the file will remain on the disk.
                /// </summary>
                public  bool Cleanup = false;
                /// <summary>
                /// The index associated with the file
                /// NOTE : When adding FileEntryDescriptor to the Archiver, Index MUST be UNIQUE
                /// </summary>
                /// <value></value>
                public string Index
                {
                    get 
                    {
                        return _index;
                    }
                    set 
                    {
                        if (value == null || value.Trim() == string.Empty)
                        { 
                            throw new ArgumentNullException("Value canot be null nor empty / whitespaces");
                        }
                        _index = value;
                    }
                }
                /// <summary>
                /// Get/set the Name of the File
                /// </summary>
                /// <value></value>
                public string Name
                {
                    get
                    {
                        return _name;
                    }
                    set
                    {                    
                        if (value == null || value.Trim() == string.Empty)
                        { 
                            throw new ArgumentNullException("Name must be set to a valid string (null or empty string passed in)");
                        }
                        if (Path.GetDirectoryName(value) != string.Empty)
                        {
                            if (Path.GetFileName(value) == string.Empty)
                            {
                                throw new ArgumentException("The Name value should contain a file name (currently set to a directory)");
                            }
                            _fullName = value;
                            _name = Path.GetFileName(value);
                        }
                        else 
                        {
                            _fullName = Path.Combine(Directory.GetCurrentDirectory(), value);
                            _name = value;
                        }
                    }
                }
                /// <summary>
                /// The full name (Path + Name) of the file
                /// </summary>
                /// <value></value>
                public string FullName
                {
                    get
                    {
                        return _fullName;
                    }
                    set
                    {
                        if (value == null || value.Trim() == string.Empty)
                        {
                            throw new ArgumentNullException("FullName must be set to a valid string (null or empty string passed in)");
                        }
                        if (Path.GetFileName(value) == string.Empty)
                        { 
                            throw new ArgumentException("The FullName value should contain a file name (currently set to a directory)");
                        }
                        if (Path.GetDirectoryName(value) == string.Empty)
                        {
                            _fullName = Path.Combine(Directory.GetCurrentDirectory(), value);
                        }
                        else 
                        {
                            _fullName = value;
                        }
                        _fullName = value;
                        _name = Path.GetFileName(_fullName);
                    }
                }

                private string _index = string.Empty;
                private string _name = string.Empty;
                private string _fullName = string.Empty;
            #endregion Properties

            #region constructors
                /// <summary>
                /// constructor
                /// </summary>
                public FileEntryDescriptor(string name, string index) 
                {
                    // @ review : Variables checked in property setter, check anyway ?
                    Index = index;
                    Name = name;
                }
            #endregion constructors
        }
    #endregion

    #region Archiver    
        /// <summary>
        /// The main calss for hierarchic archive management
        /// </summary>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public class Archiver: IArchiveItemsAdapter
        {
            #region const definition
                private const string KEY = "key";
                private const string INDEX = "index";
                private const string CRC = "CRC";
                private const string NAME = "name";
                private const string ENTRY = "Entry";
                private const string ROOT = "root";
                private const string ARCHIVE = "Archive";
                private const char SEPARATOR = '/';
                private const string XMLEXTENSION = ".xml";
            #endregion
            
            #region Properties 
                #region Private Properties 
                    private XmlDocument _xmlDoc = new XmlDocument();
                    private static MD5 _md5 = null;
                    private ArrayList _subArchives = null;
                    private ArrayList _files = null;
                    private Archiver _root = null;
                    private string _name = string.Empty;
                    private string _directoryName = string.Empty;
                    private string _fullName = string.Empty;
                    private bool _directoryCleanup = false;
                    private CabinetInfo _cabInfo = null;
                    private Hashtable _archiveHash = null;
                    private ArcType _Type = ArcType.GenericArchive;
                    private bool _ignoreCrc = true;
                    private string _archivePath = string.Empty;
                    private bool _isDirty = true;
                #endregion Private Properties 
                #region Static Properties 
                    static private string GenerateNewArchivePath
                    {
                        get
                        {
                            return Path.Combine(Path.GetTempPath(), ARCHIVE + Guid.NewGuid().ToString());
                        }
                    }
                #endregion Static Properties 
                #region Public Properties 
                    /// <summary>
                    /// Direct archiver to bypass the CRC security check (protecting 
                    /// against file tampering).
                    /// </summary>
                    /// <value>
                    /// Set to true to ignore the CRC check.
                    /// If set to false and the file has been tampered with, a 
                    /// CryptographicException will be thrown during archive access.
                    ///</value>
                    public bool IgnoreCrc
                    { 
                        get 
                        {
                            return _ignoreCrc;
                        }
                        set 
                        {
                            _ignoreCrc = value;
                        }
                    }
                #endregion Public Properties 
            #endregion Properties

            #region Constructors / Finalizer
                /// <summary>
                /// Instantiate an Archiver class
                /// </summary>
                private Archiver()
                { 
                    _archivePath = GenerateNewArchivePath;
                    _directoryName = _archivePath;
                    _directoryCleanup = true;
                    _md5 = new MD5CryptoServiceProvider();
                    _subArchives = new ArrayList();
                    _files = new ArrayList();
                    _archiveHash = new Hashtable();
                }
                /// <summary>
                /// Access or create a new Archive
                /// </summary>
                /// <param name="archiveName">The full name of the archive to create or to access</param>
                private Archiver(string archiveName) : this()
                {
                    System.Diagnostics.Debug.Assert(archiveName != null && archiveName.Trim() != string.Empty, "The name of the archive cannot be null nor empty / whitespace only");

                    _name = Path.GetFileName(archiveName);
                    if (Path.GetDirectoryName(archiveName) != string.Empty)
                    {
                        _directoryName = Path.GetDirectoryName(archiveName);
                        _directoryCleanup = false;
                    }
                    _fullName = Path.Combine(_directoryName, _name);
                    _cabInfo = new CabinetInfo(_fullName);
                }
                /// <summary>
                /// Using a Finalizer instead of implementing IDisposable since
                /// this is not critical ie: does not bind activally anything.
                /// Just for clean up purpose, this should happen ...eventually.
                /// </summary>
                ~Archiver()
                {
                    if (_directoryCleanup)
                    { 
                        if (Directory.Exists(_directoryName))
                        {
                            try
                            {
                                // ignore potential problem (sharing violation, directory not empty, ...)
                                Directory.Delete(_directoryName);
                            }
                            catch (IOException e)
                            {
                                System.Diagnostics.Debug.WriteLine("Couldn't delete the Directory '" + _directoryName + "'. Exception : " + e.ToString());
                            }
                        }
                    }
                }
            #endregion Constructors / Finalizer

            #region IArchiveItemsAdapter Implementation
                /// <summary>
                /// Get/set the value associated with this index as an array of byte.
                /// Note : The change made using the indexer setter, won't be serialized until Update() is called
                /// </summary>
                /// <value></value>
                byte[] IArchiveItemsAdapter.this[string key]
                {
                    get
                    {
                        byte[] retVal = null;
                        if (_archiveHash.Contains(key) == false)
                        {
                            // @ review : return null or throw ?
                            throw new CompressionException("Key '" + key + "' cannot be found in this archive");
                        }

                        string path = (string)_archiveHash[key];
                        if (path == null || path.Trim() == string.Empty)
                        {
                            // @ review : return null or throw ?
                            throw new ApplicationException("The path associated with key '" + key + "' is empty/null");
                        }
#if CLR_VERSION_BELOW_2
                        FileStream fs = null;
                        try
                        {
                            fs = File.OpenRead(path);
                            fs.Read(retVal, 0, (int)fs.Length);
                        }
                        finally
                        {
                            if(fs != null)
                            {
                                fs.Close();
                            }
                        }
#else
                        retVal = File.ReadAllBytes(path);                        
#endif

                        return retVal;
                    }
                    set
                    {
                        if (value == null || value.Length == 0)
                        { 
                            throw new ArgumentException ("cannot pass a null or empty value");
                        }
                        Archiver fileOwner = this;
                        if (_archiveHash.Contains(key))
                        {
                            fileOwner = RemoveItem(key);
                        }

                        fileOwner.AddBuffer(key, value);
                    }
                }
                /// <summary>
                /// Get/set the name of the archive to open
                /// </summary>
                /// <value></value>
                string IArchiveItemsAdapter.ArchiveName
                {
                    get
                    {
                        return _fullName;
                    }
                }
                /// <summary>
                /// Serialize the change (change made thru the indexer setter are commited)
                /// </summary>
                void IArchiveItemsAdapter.Update()
                {
                    GenerateArchive();
                }
                /// <summary>
                /// Get a collection of all the keys defined in this archive
                /// Note : Keys are sorted 
                /// </summary>
                /// <value></value>
                public string[] Keys
                {
                    get
                    {
                        if (_archiveHash == null)
                        { 
                            // @Review : return an empty array or null ? (might consider throw as well)
                            return new string[0];
                        }

                        string[] retVal = new string[_archiveHash.Keys.Count];
                        _archiveHash.Keys.CopyTo(retVal, 0);
                        // @Review : Do we want to sort the keys ? Can take a long time if there a lots of entries
                        Array.Sort(retVal);
                        return retVal;
                    }
                }
            #endregion IIMageSequenceInterface Implementation
        
            #region Static APIs (public & private)
                /// <summary>
                /// Load an Archive and extract the files
                /// </summary>
                /// <param name="archiverName">The name of the archive to open</param>
                /// <returns>An instance of the Archiver object</returns>
                static public Archiver Load(string archiverName)
                {
                    Archiver retVal = null;
                    if (archiverName == null || archiverName.Trim() == string.Empty)
                    { 
                        throw new ArgumentNullException("archiverName", "An invalid value was passed in as parameter (null / empty / whitespace)");
                    }
                    archiverName = TranslateEnvironmentVariable(archiverName);
                    if (File.Exists(archiverName) == false)
                    {
                        throw new FileNotFoundException("the specified file was not found", archiverName);
                    }

                    retVal = new Archiver(archiverName);
                    XmlDocument xmlDoc = new XmlDocument();

                    // Get its description file
                    retVal.CreateDirectory(retVal._archivePath);
                    retVal._cabInfo.ExtractAll(retVal._archivePath, true, null, null);
                    xmlDoc.Load(Path.Combine(retVal._archivePath, retVal._cabInfo.Name) + XMLEXTENSION);

                    // Populate _files arraylist
                    XmlNodeList nodeListFiles = xmlDoc.SelectNodes("*/" + ENTRY);
                    foreach(XmlNode node in nodeListFiles)
                    {
                        FileEntryDescriptor fileDesc = new FileEntryDescriptor(Path.Combine(retVal._archivePath, node.Attributes[NAME].Value), node.Attributes[KEY].Value);
                        fileDesc.Root = retVal;
                        retVal._files.Add(fileDesc);
                        retVal._archiveHash.Add(fileDesc.Index, fileDesc.FullName);
                    }

                    // Populate _subArchive arraylist
                    XmlNodeList nodeListArchive = xmlDoc.SelectNodes("*/" + ARCHIVE);
                    foreach (XmlNode node in nodeListArchive)
                    {
                        Archiver subArchive = Archiver.Load(Path.Combine(retVal._archivePath, node.Attributes[NAME].Value));
                        retVal._subArchives.Add(subArchive);
                        // copy hash of sub archive in this hash
                        IDictionaryEnumerator iter = subArchive._archiveHash.GetEnumerator();
                        while (iter.MoveNext())
                        {
                            retVal._archiveHash.Add(iter.Key, iter.Value);
                        }
                    }

                    retVal.CheckArchiveCrc();

                    return retVal;
                }
                /// <summary>
                /// Create a new Archive
                /// </summary>
                /// <param name="archiverName">The name of the archive to create</param>
                /// <returns>An instance of the new Archiver created</returns>
                static public Archiver Create(string archiverName)
                {
                    if (archiverName == null || archiverName.Trim() == string.Empty)
                    {
                        throw new ArgumentNullException("archiverName", "An invalid value was passed in as parameter (null / empty / whitespace)");
                    }
                    return Create(archiverName, false);
                }
                /// <summary>
                /// Create a new Archive
                /// </summary>
                /// <param name="archiverName">The name of the archive to create</param>
                /// <param name="overwriteExistingFile">Set to true to overwrite an existing archive</param>
                /// <returns>An instance of the new Archiver created</returns>
                static public Archiver Create(string archiverName, bool overwriteExistingFile)
                {
                    if (archiverName == null || archiverName.Trim() == string.Empty)
                    {
                        throw new ArgumentNullException("archiverName", "An invalid value was passed in as parameter (null / empty / whitespace)");
                    }
                    archiverName = TranslateEnvironmentVariable(archiverName);
                    if (File.Exists(archiverName) == true)
                    {
                        if (overwriteExistingFile == false)
                        {
                            throw new IOException("File already exists, will not overwrite file by default, if you need to overwrite it delete it or use the overloaded Method with the overwrite param set to true");
                        }

                        // @Review : System set the File as read only;  overwrite anyway ?
                        FileAttributes fileAttributes = File.GetAttributes(archiverName);

                        if ((fileAttributes & FileAttributes.ReadOnly) != 0)
                        {
                            File.SetAttributes(archiverName, fileAttributes ^ FileAttributes.ReadOnly);
                        }

                        // IOException might occur (file sharing exception), let exception be passed to caller
                        File.Delete(archiverName);
                    }

                    return new Archiver(archiverName);
                }
                static private string TranslateEnvironmentVariable(string environmentVar)
                {
                    MatchCollection matches = Regex.Matches(environmentVar, "(%.+?%)");
                    for (int t = 0; t < matches.Count; t++)
                    {
                        string val = matches[t].Value;
                        string translatedVariable = System.Environment.GetEnvironmentVariable(val.Substring(1, val.Length - 2));
                        environmentVar = Regex.Replace(environmentVar, matches[t].Value, translatedVariable);
                    }
                    return environmentVar;
                }
            #endregion Static APIs (public & private)

            #region Public APIs

                /// <summary>
                /// Add a sub archive to the archive
                /// </summary>
                public void AddArchive(Archiver arc)
                {
                    _subArchives.Add(arc);
                    arc._root = this;
                }
                /// <summary>
                /// Add an array of byte to the archive 
                /// </summary>
                /// <param name="key">The unique key to be used in the archive</param>
                /// <param name="buffer">The array of byte to be added to the archive</param>
                public void AddBuffer(string key, byte[] buffer)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException("The indexing key is null");
                    }

                    object val = System.Text.ASCIIEncoding.ASCII.GetString(buffer);

                    AddStuffObjectInternal(key, val, "buffer_");
                }
                /// <summary>
                /// Add a FileEntryDescriptor to the archive
                /// </summary>
                public void AddFile(FileEntryDescriptor fileEntryDescriptor) 
                {
                    if (fileEntryDescriptor == null)
                    { 
                        throw new ArgumentNullException("fileEntryDescriptor", "Must be set to a valid instance of FileEntryDescriptor");
                    }
                    AddFileInternal(fileEntryDescriptor);
                }
                /// <summary>
                /// Add a File to to the archive
                /// </summary>
                /// <param name="fileName">The file name</param>
                /// <param name="index">The index number (must be unique)</param>
                /// <param name="cleanup">Delete the file after compression</param>
                public void AddFile(string fileName, string index, bool cleanup)
                {
                    FileEntryDescriptor fed = new FileEntryDescriptor(fileName, index);
                    fed.Cleanup = cleanup;
                    AddFile(fed);
                }
                /// <summary>
                /// Add an streamed object to the archive (obj.ToString() returns the serialization)
                /// need to move to CLR serialization if necessary
                /// </summary>
                public void AddObject(string key, object streamed)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException("The indexing key is null");
                    }
                    string streamSerialized = string.Empty;
                    // Prototype code not working properly, disable for now.
/*                    
                    // BUGBUG : prototype test code
                    ISerializable streamedSerialized = streamed as ISerializable;
                    if (streamedSerialized != null)
                    {
                        FormatterConverter fc = new FormatterConverter();
                        SerializationInfo info = new SerializationInfo(streamed.GetType(), fc);
                        StreamingContext context = new StreamingContext ();
                        streamedSerialized.GetObjectData(info, context);
                        SerializationInfoEnumerator iter = info.GetEnumerator ();
                        while (iter.MoveNext ())
                        { 
                            string name = iter.Current.Name;
                            string val = fc.ToString(iter.Current.Value);   // BUGBUG : for a bitmap it returns "System.Byte[]" instead of actual value.
                            streamSerialized += "[" + name + "] : " + val + "\n";
                        }
                    }
                    else 
                    {
                        streamSerialized = streamed.ToString();
                    }
                    // END BUGBUG : prototype test code
*/
                    streamSerialized = streamed.ToString();

                    AddStuffObjectInternal(key, streamSerialized, "object_");
                }
                /// <summary>
                /// Add a stream to the archive 
                /// </summary>
                /// <param name="key">The unique key to be used in the archive</param>
                /// <param name="stream">The stream to be added to the archive</param>
                public void AddStream(string key, Stream stream)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException("The indexing key is null");
                    }
                    object val = null;
                    byte[] buffer = null;
                    MemoryStream memoryStream = stream as MemoryStream;
                    if (memoryStream != null)
                    {
                        // For some reason 'Read' do not work on MemoryStream, need to use 'GetBuffer'
                        buffer = memoryStream.GetBuffer ();
                    }
                    else 
                    { 
                        buffer = new byte[(int)stream.Length];
                        if (stream.Position != 0)
                        {
                            stream.Seek (0, SeekOrigin.Begin);
                        }
                        stream.Read(buffer, 0, (int)stream.Length);
                    }
                    val = System.Text.ASCIIEncoding.ASCII.GetString (buffer, 0, (int)stream.Length);
                    AddStuffObjectInternal (key, val, "stream_");
                }
                /// <summary>
                /// Serialize the archive
                /// </summary>
                public void GenerateArchive()
                {
                    GenArchive(string.Empty);
                    if(this._root != null)
                    {
                        // this archive has a parent, parent needs to be regenerated.
                        this._root.GenerateArchive();
                    }
                }
                /// <summary>
                /// Return the full path to the file associated with the key.
                /// </summary>
                /// <param name="key">The key that map to the file (key collection can be retrieve by calling the "Keys" property)</param>
                /// <returns>The full path to the file</returns>
                public string GetFileLocation(string key)
                {
                    string retVal = string.Empty;

                    if (_archiveHash == null)
                    {
                        throw new CompressionException("Internal Error : _archiveHash should not be null ! (Load / create should initialize this variable)");
                    }
                    if (_archiveHash.Contains(key) == false)
                    {
                        return string.Empty;
                    }
                    else 
                    {
                        retVal = _archiveHash[key].ToString();
                    }

                    return retVal;
                }
                /// <summary>
                /// Remove an item from the Cab
                /// Note : You will need to call GenerateArchive() to commit the changes
                /// </summary>
                /// <param name="key">the key to find</param>
                /// <returns>The direct Archiver than was containing this key</returns>
                public Archiver RemoveItem(string key)
                {
                    Archiver fileOwner = null;

                    // BUGBUG : SLOW, need to improve this.
                    // remove entry from _files ArrayList
                    FileEntryDescriptor fileEntry = FindFileEntry(key);
                    if (fileEntry == null)
                    { 
                        throw new CompressionException("The specified key awsa not found, therefore, the element cannot be remoted");
                    }
                    fileOwner = fileEntry.Root;
                    fileEntry.Root._files.Remove(fileEntry);

                    // remove entry from all _argumentHash Hashtables
                    Archiver ptr = fileOwner;
                    while (ptr != null)
                    {
                        ptr._archiveHash.Remove(key);
                        ptr = ptr._root;
                    }
                    return fileOwner;
                }
                /// <summary>
                /// Returns a pointer to all the files extracted from the cab
                /// </summary>
                /// <returns></returns>
                public string[] GetAllExtractedFiles()
                {
                    string[] retVal = new string[_archiveHash.Count];
                    _archiveHash.Values.CopyTo(retVal, 0);
                    for (int t = 0; t < retVal.Length; t++)
                    { 
                        if(Path.GetDirectoryName(retVal[t]) == string.Empty)
                        {
                            retVal[t] = Path.Combine(Directory.GetCurrentDirectory(), retVal[t]);
                        }
                    }
                    // @Review : Do we need to sort ? Might looks better but will slow down the execution.
                    Array.Sort(retVal);
                    return retVal;
                }
                /*

                /// <summary>
                /// xml serialization of string by escaping offending chars - to be intergated
                /// </summary>
                public static string EncodeOffendingChars(string astr)
                {
                    //BUGBUG to be integrated in xml streaming
                    if (astr == null) { astr = string.Empty; }

                    StringBuilder vret = new StringBuilder();
                    char[] acht = astr.ToCharArray();

                    for (int j = 0; j < acht.Length; j++)
                    {
                        if (acht[j] == '<') { vret.Append("&lt;"); }
                        else if (acht[j] == '>') { vret.Append("&gt;"); }
                        else if (acht[j] == '&') { vret.Append("&amp;"); }
                        else if (acht[j] == '"') { vret.Append("&quot;"); }
                        else if (acht[j] == '\'') { vret.Append("&apos;"); }
                        else { vret.Append(acht[j]); }
                    }

                    return vret.ToString();
                }
                */

            #endregion
        
            #region Privates
                private void PopulateArchiveHash(XmlNode node)
                { 
                    XmlNodeList xmlNodeListArchive = node.SelectNodes(ARCHIVE);
                    foreach (XmlNode childNode in xmlNodeListArchive)
                    { 
                        _archiveHash.Add(childNode.Attributes[NAME].Value, childNode.Attributes[INDEX].Value);
                        PopulateArchiveHash(childNode);
                    }
                    XmlNodeList xmlNodeListEntry = node.SelectNodes(ENTRY);
                    foreach (XmlNode childNode in xmlNodeListEntry)
                    {
                        if (_archiveHash[childNode.Attributes[ROOT].Value] == null)
                        {
                            _archiveHash.Add(childNode.Attributes[KEY].Value, childNode.Attributes[NAME].Value);
                        }
                        else
                        {
                            _archiveHash.Add(childNode.Attributes[KEY].Value, _archiveHash[childNode.Attributes[ROOT].Value].ToString() + "/" + childNode.Attributes[NAME].Value);
                        }
                    }
                }
                private XmlElement GenArchive(string accessPath)
                {
                    if (this._isDirty)
                    {
                        _archiveHash.Clear();
                        this._isDirty = false;
                    }
                    // Create description file (xml file describing the cab file)
                    XmlElement node = _xmlDoc.CreateElement(ARCHIVE);
                    XmlAttribute indexAttribute = _xmlDoc.CreateAttribute(INDEX);
                    if(accessPath != null && accessPath.Trim() != string.Empty)
                    {
                        accessPath = accessPath.Trim() + SEPARATOR + _name;
                    }
                    else
                    {
                        accessPath = _name;
                    }
                    indexAttribute.Value = accessPath;
                    XmlAttribute nameAttribute = _xmlDoc.CreateAttribute(NAME);
                    nameAttribute.Value = _name;
                    XmlAttribute typeAttribute = _xmlDoc.CreateAttribute("type");
                    typeAttribute.Value = _Type.ToString();
                    node.Attributes.Append(indexAttribute);
                    node.Attributes.Append(nameAttribute);
                    node.Attributes.Append(typeAttribute);


                    foreach (FileEntryDescriptor ftd in _files)
                    {
                        XmlElement fileNode = _xmlDoc.CreateElement(ENTRY);
                        XmlAttribute keyAttribute = _xmlDoc.CreateAttribute(KEY);
                        keyAttribute.Value = ftd.Index;
                        XmlAttribute fileNameAttribute = _xmlDoc.CreateAttribute(NAME);
                        fileNameAttribute.Value = ftd.Name;
                        XmlAttribute rootAttribute = _xmlDoc.CreateAttribute(ROOT);
                        rootAttribute.Value = ftd.Root._name;
                        fileNode.Attributes.Append(keyAttribute);
                        fileNode.Attributes.Append(fileNameAttribute);
                        fileNode.Attributes.Append(rootAttribute);
                        node.AppendChild(fileNode);
                    }

                    ArrayList filesToCab = new ArrayList();

                    // Recurse in sub archive
                    foreach (Archiver subArchive in _subArchives)
                    {
                        XmlElement childNode = subArchive.GenArchive(/*archivePath,*/ accessPath);
                        node.AppendChild(childNode);
                        // Add sub-Archive to CAB
//                        filesToCab.Add(Path.Combine(/*archivePath*/subArchive._directoryName, subArchive._name));
                        string subArchivePath = Path.Combine(subArchive._directoryName, subArchive._name);
                        FileEntryDescriptor ArchiveEntry = new FileEntryDescriptor(subArchivePath, Path.GetFileName(subArchivePath));
                        ArchiveEntry.Cleanup = true;
                        _files.Add(ArchiveEntry);
                        // copy hashtable entry to this hashtable
                        IDictionaryEnumerator iter = subArchive._archiveHash.GetEnumerator();
                        while (iter.MoveNext())
                        {
                            if (_archiveHash.Contains(iter.Key))
                            { 
                                throw new DuplicateNameException("Index must be unique, index '" + iter.Key.ToString() + "' already exist");
                            }
                            _archiveHash.Add(iter.Key, iter.Value); // check for duplicate index
                        }
                    }

                    // Add files to CAB
                    foreach (FileEntryDescriptor fileDesc in _files)
                    {
                        if (File.Exists(fileDesc.FullName) == false)
                        { 
                            throw new FileNotFoundException("The file specified does not exist", fileDesc.Name);
                        }
                        filesToCab.Add(fileDesc.FullName);
                        if (_archiveHash.Contains(fileDesc.Index))
                        { 
                            throw new DuplicateNameException("Index must be unique, index '" + fileDesc.Index + "' already exist");
                        }
                        _archiveHash.Add(fileDesc.Index, fileDesc.FullName);
                    }
                    
                   // Compute CRC
                    string crc = CompFileCrc((string[])filesToCab.ToArray(typeof(string)));

                    // Add CRC And save file description (xml file describing the cab file)
                    XmlAttribute crcAttribute = _xmlDoc.CreateAttribute(CRC);
                    crcAttribute.Value = crc;
                    node.Attributes.Append(crcAttribute);
                    _xmlDoc.LoadXml(node.OuterXml);
                    CreateDirectory(_archivePath);
                    string xmlFile = Path.Combine(_archivePath, _name + XMLEXTENSION);
                    _xmlDoc.Save(xmlFile);
                    _xmlDoc.RemoveAll();
                    FileEntryDescriptor fed = new FileEntryDescriptor(xmlFile, xmlFile);
                    fed.Cleanup = true;
                    _files.Add(fed);
                    filesToCab.Add(xmlFile);

                    // Generate CAB
                    _cabInfo.CompressFiles(null, (string[])filesToCab.ToArray(typeof(string)), null);

                    // Clean up
                    foreach (FileEntryDescriptor fileDesc in _files)
                    {
                        if (fileDesc.Cleanup == true)
                        {
                            try
                            {
                                // @ review : Bypass system readonly setting if user want to delete a readonly file ?
                                FileAttributes fileAttribute = File.GetAttributes(fileDesc.FullName);
                                if ((fileAttribute & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                { 
                                    fileAttribute ^= FileAttributes.ReadOnly;
                                    File.SetAttributes(fileDesc.FullName, fileAttribute);
                                }
                                File.Delete(fileDesc.FullName);
                            }
                            catch (IOException ioe)
                            { 
                                // file might be in use
                                Console.WriteLine("Cannot delete file -- reason : " + ioe.Message);
                            }
                        }
                    }
                    // BUGBUG : Clean up sub archives
                    return node;
                }
                private void CreateDirectory(string directoryPath)
                {
                    if (Directory.Exists(directoryPath) == false)
                    {
                        // Get all subfolders
#if CLR_VERSION_BELOW_2
                        string[] allFolders = directoryPath.Split(new char[] { '\\' });
#else
                        string[] allFolders = directoryPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
#endif
                        string currentPath = string.Empty;

                        // Check if subfolder exist, if not create it
                        foreach (string folder in allFolders)
                        {
                            currentPath += folder + "\\";
                            if (Directory.Exists(currentPath) == false)
                            {
                                Directory.CreateDirectory(currentPath);
                            }
                        }
                    }
                }
                private void AddStuffObjectInternal(string key, object streamed, string prefix)
                {
                    if (key == null || key.Trim() == string.Empty)
                    {
                        throw new ArgumentNullException("The indexing key is null / empty / whitespace");
                    }
                    // Check if index already exist
                    if (_archiveHash.Contains(key))
                    { 
                        throw new DuplicateNameException("Index '" + key + "' already exist, indexes must be unique");
                    }

                    CreateDirectory(_archivePath);

                    // Create file so it can be added to the cab
                    string cntName = Path.Combine (_archivePath, prefix + Guid.NewGuid().ToString () + ".cnt");
                    StreamWriter strw = null;
                    try
                    {
                        strw = new StreamWriter(cntName);
                        strw.Write(streamed);
                    }
                    finally
                    {
                        if (strw != null)
                        {
                            strw.Close();
                        }
                    }

                    FileEntryDescriptor fileDescription = new FileEntryDescriptor(cntName, key);
                    fileDescription.Cleanup = true;
                    AddFileInternal(fileDescription);
                }
                private void AddFileInternal(FileEntryDescriptor fileEntry)
                {
                    // Check if index already exist
                    if (_archiveHash.Contains(fileEntry.Index))
                    {
                        throw new DuplicateNameException("Index '" + fileEntry.Index + "'  (existing 1 : '" + _archiveHash[fileEntry.Index].ToString() + "' file being added : '" + fileEntry.FullName + "') already exist, indexes must be unique");
                    }
                    // Check if file exist at specified location
                    if (File.Exists(fileEntry.FullName) == false)
                    { 
                        throw new FileNotFoundException("The specified file ('" + fileEntry.Name + "') was not found", fileEntry.FullName);
                    }
                    Archiver ptr = this;
                    fileEntry.Root = this;
                    _files.Add(fileEntry);
                    // Cabinet needs to be recreated
                    while (ptr != null)
                    {
                        ptr._isDirty = true;
                        ptr = ptr._root;
                    }
                }

                private string CompFileCrc(FileEntryDescriptor fileDescriptor)
                {
                    return CompFileCrc(new FileEntryDescriptor[] { fileDescriptor });
                }
                private string CompFileCrc(FileEntryDescriptor[] fileDescriptors)
                {
                    string[] fileNames = new string[fileDescriptors.Length];
                    for( int t = 0; t < fileDescriptors.Length; t++)
                    { 
                        fileNames[t] = fileDescriptors[t].Name;
                    }
                    return CompFileCrc(fileNames);
                }
                private string CompFileCrc(string fileName) 
                {
                    return CompFileCrc(new string[]{fileName});
                }
                private string CompFileCrc(string[] fileNames) 
                {
                    string[] keys = new string[fileNames.Length];
                    for(int t = 0; t < fileNames.Length; t++)
                    {
                        keys[t] = Path.GetFileName(fileNames[t]);
                    }
                    Array.Sort(keys, fileNames);
                    string bufferAsString = null;
                    int size = fileNames.Length;
                    StreamReader[] streamReaders = new StreamReader[size];
                    try
                    {
                        for (int t = 0; t < size; t++)
                        {
                            streamReaders[t] = new StreamReader(fileNames[t]);
                            bufferAsString += streamReaders[t].ReadToEnd();
                        }
                    }
                    finally
                    {
                        for (int t = 0; t < streamReaders.Length;  t++)
                        {
                            if (streamReaders[t] != null)
                            {
                                streamReaders[t].Close();
                                streamReaders[t] = null;
                            }
                        }
                        streamReaders  = null;
                    }
                    return CompCrc(bufferAsString);
                }
                private string CompCrc(string cnt)
                {
                    byte[] rawBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(cnt);
                    return CompCrc(rawBytes);
                }
                private string CompCrc(byte[] rawBytes)
                {
                    byte[] hashData = _md5.ComputeHash(rawBytes);
                    StringBuilder stb = new StringBuilder(hashData.Length);
                    foreach (Byte byt in hashData)
                    {
                        stb.Append(byt);
                    }

                    return stb.ToString();
                }
                private void CheckArchiveCrc()
                {
                    if(IgnoreCrc == false)
                    {
                        CreateDirectory(_archivePath);
                        string descriptionFile = _name + XMLEXTENSION;
                        _cabInfo.ExtractAll(_archivePath, true, null, null);
                        _xmlDoc.Load(Path.Combine(_archivePath, descriptionFile));
                        string storedCrc = _xmlDoc.DocumentElement.Attributes[CRC].Value;
                        XmlNodeList entryList = _xmlDoc.SelectNodes("*/" + ENTRY);
                        XmlNodeList archiveList = _xmlDoc.SelectNodes("*/"  + ARCHIVE);
                        string[] files = new string[entryList.Count + archiveList.Count];
                        int index = 0;
                        for(index = 0; index < entryList.Count; index++)
                        {
                            files[index] = Path.Combine(_archivePath, entryList[index].Attributes[NAME].Value);
                        }
                        for(int t = 0; t < archiveList.Count; t++)
                        {
                            files[index+t] = Path.Combine(_archivePath, archiveList[t].Attributes[NAME].Value);
                        }
                        string actualCrc = CompFileCrc(files);
                        if (storedCrc != actualCrc)
                        { 
                            throw new CryptographicException("CRC does not match, file has been tampered with !");
                        }
                    }
                }
                
                private FileEntryDescriptor FindFileEntry(string key)
                {
                    FileEntryDescriptor retVal = null;
                    for (int t = 0; t < this._files.Count; t++)
                    {
                        if (((FileEntryDescriptor)this._files[t]).Index == key)
                        {
                            return (FileEntryDescriptor)this._files[t];
                        }
                    }
                    foreach(Archiver archive in this._subArchives)
                    {
                        retVal = archive.FindFileEntry(key);
                        if(retVal != null)
                        {
                            return retVal;
                        }
                    }
                    return null;
                }
            #endregion
        }
    #endregion

}
