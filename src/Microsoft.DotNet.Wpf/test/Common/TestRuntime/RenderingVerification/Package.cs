// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region Usings
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Security.Permissions;
        using Microsoft.Test.RenderingVerification;    
        using Microsoft.Test.Compression;
    #endregion Usings

    #region Enums
        /// <summary>
        /// The type of comparison(s) to take place
        /// </summary>
        [FlagsAttribute]
        public enum PackageCompareTypes
        {
            /// <summary>
            /// Comparison not set
            /// </summary>
            None = 0x0,
            /// <summary>
            /// Compare using an Image compare viewer
            /// </summary>
            ImageCompare = 0x1,
            /// <summary>
            /// Compare using an Analitical model viewer
            /// </summary>
            ModelAnalytical = 0x2,
            /// <summary>
            /// Compare using a Synthetical model viewer.
            /// </summary>
            ModelSynthetical = 0x4
        }
    #endregion Enums
    
    /// <summary>
    /// Summary description for Package.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class Package: IDisposable
    {
        #region Constants
            /// <summary>
            /// The Bitmap acting as master
            /// </summary>
            private const string MASTERBMP = "MasterBitmap.tif";
            /// <summary>
            /// The Model acting as master
            /// </summary>
            private const string MASTERMODEL= "MasterModel.stream";
            /// <summary>
            /// The captured Bitmap
            /// </summary>
            private const string CAPTUREBMP= "CapturedBitmap.tif";
            /// <summary>
            /// The model build on the captured model
            /// </summary>
            private const string CAPTUREMODEL = "CapturedModel.stream";
            /// <summary>
            /// The captured Bitamp before filtering
            /// </summary>
            private const string CAPTUREBMP_NOFILTER= "CapturedBitmapNoFilter.tif";
            /// <summary>
            /// The model build on the captured model before filtrering
            /// </summary>
            private const string CAPTUREMODEL_NOFILTER = "CapturedModelNoFilter.stream";
            /// <summary>
            /// The Differencw between 2 models
            /// </summary>
            private const string XMLDIFF = "xmlDiff.xml";
            /// <summary>
            ///  The Tolerance (set of ControlPoints) to be used by image compare
            /// </summary>
            private const string TOLERANCE= "Tolerance.xml";
            /// <summary>
            /// The file containing the filters to apply
            /// </summary>
            private const string FILTERSFILE = "Filters.xml";
            /// <summary>
            /// Extra info logged by the loader (command line, file used ...)
            /// </summary>
            private const string LOADERINFO = "LoaderInfo.xml";
            private const string OTHERINFO = "OtherInfo.xml";
            private const BindingFlags GETBINDFLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;
            private const BindingFlags SETBINDFLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty;
        #endregion Constants

        #region Properties
            #region Private Properties
                private static object _staticLock = new Object();
                private XmlDocument _xmlDoc = null;
                private Archiver _archiver = null;
                private ArrayList _keys = null;
                private PackageCompareTypes _packageCompareTypes = PackageCompareTypes.None;
                private ChannelCompareMode _channelCompareMode = ChannelCompareMode.Unknown;
                private string _packageName = string.Empty;
                private Hashtable _entries = new Hashtable();
                private string _masterSDLocation = string.Empty;
                private string _commandline= string.Empty;
                private ArrayList _extraFiles = new ArrayList();
                private bool _isFailureAnalysis = false;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Retrieve is the package is a failure analysis package (ie : contains rendered information)
                /// </summary>
                /// <value></value>
                public bool IsFailureAnalysis
                {
                    get { return _isFailureAnalysis; }
                }
                /// <summary>
                /// The type of comparison that should occur
                /// </summary>
                /// <value></value>
                public PackageCompareTypes PackageCompare
                {
                    get
                    {
                        return _packageCompareTypes;
                    }
                    set 
                    {
                        _packageCompareTypes = value;
                    }
                }
                /// <summary>
                /// Get/set the Mode or capture (What channels to use)
                /// </summary>
                /// <value></value>
                public ChannelCompareMode ChannelCompare
                {
                    get
                    {
                        return _channelCompareMode;
                    }
                    set
                    {
                        _channelCompareMode = value;
                    }
                }
                /// <summary>
                /// The location where the master is located on Source Depot
                /// </summary>
                /// <value></value>
                public string MasterSDLocation
                {
                    get 
                    {
                        return _masterSDLocation;
                    }
                    set 
                    {
                        if (Path.GetFileName(value) == string.Empty)
                        { 
                            throw new PackageException("The MasterSDLocation Must contains the master's file name");
                        }
                        if (Path.GetExtension(value) == string.Empty)
                        {
                            throw new PackageException("The MasterSDLocation Must contains the master's file extension");
                        }
                        _masterSDLocation = value;
                    }
                }
                /// <summary>
                /// The Command Line used to start the exe
                /// </summary>
                /// <value></value>
                public string CommandLine
                {
                    get 
                    {
                        return _commandline;
                    }
                    set 
                    {
                        _commandline = value;
                    }
                }
                /// <summary>
                /// The name of the package
                /// </summary>
                /// <value></value>
                public string PackageName
                { 
                    get 
                    {
                        return _packageName;
                    }
                    set 
                    {
                        if (value == null || value.Trim() == string.Empty)
                        { 
                            throw new ArgumentNullException("Cannot use a null/empty/whitespace PackageName");
                        }
                        _packageName = value;
                    }
                }
                /// <summary>
                /// Get/set the master bitmap
                /// Note : This will take a copy of the bitmap, user should call Dispose on the original Bitmap ASAP
                /// </summary>
                /// <value></value>
                public Bitmap MasterBitmap
                {
                    get
                    {
                        return (Bitmap)_entries[MASTERBMP];
                    }
                    set
                    {
                        if (_entries.Contains(MASTERBMP))
                        { 
                            ((Bitmap)_entries[MASTERBMP]).Dispose();
                        }
                        if (value == null)
                        {
                            _entries.Remove(MASTERBMP);
                        }
                        else
                        {
                            _entries[MASTERBMP] = value.Clone();
                        }
                    }
                }
                /// <summary>
                /// Get/set the master model
                /// </summary>
                /// <value></value>
                public Stream MasterModel
                {
                    get
                    {
                        return (Stream)_entries[MASTERMODEL];
                    }
                    set
                    {
                        _entries[MASTERMODEL] = value;
                    }
                }
                /// <summary>
                /// Get/set the captured bitmap before filtering
                /// Note : This will take a copy of the bitmap, user should call Dispose on the original Bitmap ASAP
                /// </summary>
                /// <value></value>
                public Bitmap CapturedBitmapBeforeFilters
                {
                    get
                    {
                        return (Bitmap)_entries[CAPTUREBMP_NOFILTER];
                    }
                    set
                    {
                        if (_entries.Contains(CAPTUREBMP_NOFILTER))
                        {
                            ((Bitmap)_entries[CAPTUREBMP_NOFILTER]).Dispose();
                        }
                        if (value == null)
                        {
                            _entries.Remove(CAPTUREBMP_NOFILTER);
                        }
                        else
                        {
                            _entries[CAPTUREBMP_NOFILTER] = value.Clone();
                        }
                    }
                }
                /// <summary>
                /// Get/set the captured model before filtering
                /// </summary>
                /// <value></value>
                public Stream CapturedModelBeforeFilters
                {
                    get
                    {
                        return (Stream)_entries[CAPTUREMODEL_NOFILTER];
                    }
                    set
                    {
                        _entries[CAPTUREMODEL_NOFILTER] = value;
                    }
                }
                /// <summary>
                /// Get/set the captured bitmap
                /// Note : This will take a copy of the bitmap, user should call Dispose on the original Bitmap ASAP
                /// </summary>
                /// <value></value>
                public Bitmap CapturedBitmap
                { 
                    get
                    {
                        return (Bitmap)_entries[CAPTUREBMP];
                    }
                    set
                    {
                        if (_entries.Contains(CAPTUREBMP))
                        {
                            ((Bitmap)_entries[CAPTUREBMP]).Dispose();
                        }
                        if (value == null)
                        {
                            _entries.Remove(CAPTUREBMP);
                        }
                        else
                        {
                            _entries[CAPTUREBMP] = value.Clone();
                        }
                    }
                }
                /// <summary>
                /// Get/set the captured model
                /// </summary>
                /// <value></value>
                public Stream CapturedModel
                { 
                    get
                    {
                        return (Stream)_entries[CAPTUREMODEL];
                    }
                    set
                    {
                        _entries[CAPTUREMODEL] = value;
                    }
                }
                /// <summary>
                /// Get/set the xmldiff (output produced by comparing 2 models)
                /// </summary>
                /// <value></value>
                public XmlNode XmlDiff
                { 
                    get
                    {
                        if (_entries.Contains(XMLDIFF) == false)
                        {
                            return null;
                        }

                        if (_entries[XMLDIFF] is MemoryStream)
                        {
                            XmlDocument xmlDoc = new XmlDocument();
                            string xmlString = System.Text.ASCIIEncoding.ASCII.GetString(((MemoryStream)_entries[XMLDIFF]).GetBuffer());
                            xmlDoc.LoadXml(xmlString);
                            return xmlDoc.DocumentElement;
/*
                            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlNode));
                            object xmlNode = serializer.Deserialize((Stream)_entries[XMLDIFF]);
                            return (XmlNode)xmlNode;
*/
                        }
                        return (XmlNode)_entries[XMLDIFF];
                    }
                    set
                    {
                        _entries[XMLDIFF] = value;
                    }
                }
                /// <summary>
                /// Get/set the Tolerance (file containing the ControlPoints)
                /// </summary>
                /// <value></value>
                public XmlNode Tolerance
                { 
                    get
                    {
                        if (_entries.Contains(TOLERANCE) == false)
                        {
                            return null;
                        }

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(System.Text.ASCIIEncoding.ASCII.GetString(((MemoryStream)_entries[TOLERANCE]).GetBuffer()));
                        return (XmlNode)xmlDoc.DocumentElement;
                    }
                    set
                    {
                        if (value == null) { _entries.Remove(TOLERANCE); }
                        byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(value.OuterXml);
                        MemoryStream memoryStream = new MemoryStream(buffer);
                        _entries[TOLERANCE] = memoryStream;
                    }
                }
                /// <summary>
                /// Get/set the filters to be used on the captured image
                /// </summary>
                /// <value></value>
                public XmlNode Filters
                { 
                    get
                    {
                        if (_entries.Contains(FILTERSFILE) == false)
                        {
                            return null;
                        }

                        return (XmlNode)_entries[FILTERSFILE];
                    }
                    set
                    {
                        _entries[FILTERSFILE] = value;
                    }
                }
                /// <summary>
                /// Get/set extra info added by the Loader
                /// </summary>
                /// <value></value>
                private XmlNode LoaderInfo
                {
                    get
                    {
                        if (_entries.Contains(LOADERINFO) == false)
                        {
                            return null;
                        }
                        XmlDocument xmlDoc = new XmlDocument();
Stream stream = (Stream)_entries[LOADERINFO];
byte[] buffer = null;
if (stream.GetType() == typeof(MemoryStream))
{
     buffer = ((MemoryStream)stream).GetBuffer();
}
else
{
    stream.Seek(0, SeekOrigin.Begin);
    buffer = new byte[stream.Length];
    stream.Read(buffer, 0, buffer.Length);
}
string xmlString = System.Text.ASCIIEncoding.ASCII.GetString(buffer);
try
{
    xmlDoc.LoadXml(xmlString);
}
catch (XmlException)
{
    // Remove the Byte Ordering Mark (BOM) from here
    xmlDoc.LoadXml(xmlString.Substring(3));
}
/*
                        XmlTextReader xmltextReader = new XmlTextReader((Stream)_entries[LOADERINFO]);
                        xmlDoc.Load(xmltextReader);
//                        xmltextReader.Close();
*/
                        return xmlDoc.DocumentElement;
                    }
                    set
                    {
                        _entries[LOADERINFO] = value;
                        XmlNode nodeIsFailureAnalysis = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("IsFailureAnalysis");
                        XmlNode nodeCompareType = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("PackageCompareTypes");
                        XmlNode nodeChannelCompare = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("ChannelCompareMode");
                        XmlNode nodeMasterSDLocation = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("MasterSDLocation");
                        XmlNode nodeCommandLine = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("CommandLine");

                        if (nodeIsFailureAnalysis != null)
                        {
                            _isFailureAnalysis = bool.Parse(nodeIsFailureAnalysis.InnerText);
                        }
// Backward support
                        else
                        {
                            _isFailureAnalysis = true;
                        }

                        if (nodeCompareType == null)
                        {
                            nodeCompareType = ((XmlNode)_entries[LOADERINFO]).SelectSingleNode("VScanCompareType");
                        }
// End backward support

                        if (nodeCompareType != null)
                        {
                            PackageCompare = (PackageCompareTypes)int.Parse(nodeCompareType.InnerText);
                        }
                        if (nodeChannelCompare != null)
                        {
                            ChannelCompare = (ChannelCompareMode)int.Parse(nodeChannelCompare.InnerText);
                        }
                        if ( (nodeMasterSDLocation != null) && (nodeMasterSDLocation.InnerText != string.Empty) )
                        {
                            MasterSDLocation = nodeMasterSDLocation.InnerText;
                        }
                        if (nodeCommandLine != null)
                        {
                            CommandLine = nodeCommandLine.InnerText;
                        }
                    }
                }
                /// <summary>
                /// Get/set any extra file added.
                /// </summary>
                /// <value></value>
                public ArrayList ExtraFiles
                { 
                    get
                    {
                        throw new NotImplementedException("TO BE DONE...");
//                        return _extraFiles;
//                        return (string[])_keys.ToArray(typeof(string));
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Block Instanciatation of Package
            /// </summary>
            private Package()
             {
                 _xmlDoc = new XmlDocument();
                 _keys = new ArrayList();
             }
            private Package(Archiver archiver) : this()
            {
                XmlDocument xmlDoc = new XmlDocument();
                _archiver = archiver;
                string[] files = _archiver.GetAllExtractedFiles();
                _keys.AddRange(((IArchiveItemsAdapter)_archiver).Keys);

                // BUGBUG : bad way of doing this, improve when will have time
                for (int t = 0; t < files.Length; t++) 
                {
                    string file = files[t];
                    MemoryStream memoryStream = null;
                    byte[] buffer = null;

                    try
                    {
                        FileStream fileStream = null;
                        try
                        {
                            // Bitmap
                            fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            buffer = new byte[fileStream.Length];
                            fileStream.Read(buffer, 0, buffer.Length);
                            Bitmap bmpDeepCopy = null;
                            using (Bitmap bmpStream = (Bitmap)Bitmap.FromStream(fileStream))
                            {
                                ImageUtility imageUtility = new ImageUtility(bmpStream);
                                using (Bitmap bmpTemp = imageUtility.Bitmap32Bits)
                                {
                                    bmpDeepCopy = (Bitmap)bmpTemp.Clone();
                                }

                            }
                            _entries.Add(Path.GetFileName(file), bmpDeepCopy);
                        }
                        finally 
                        {
                            // Can close the stream since we perform a deep copy of the image
                            if (fileStream != null) { fileStream.Close(); fileStream = null; }
                        }
                    }
                    catch(ArgumentException)
                    {
                        try
                        {
                            // Stream
                            byte[] bufferStream = new byte[buffer.Length];
                            buffer.CopyTo(bufferStream, 0);
                            memoryStream = new MemoryStream(bufferStream, 0, bufferStream.Length, false, true);
                            _entries.Add(Path.GetFileName(file), memoryStream);
                        }
                        catch (Exception e)//ArgumentException)
                        {
object debug = e;
                            // Xml
                            _entries.Add(Path.GetFileName(file), GetXmlNode(file));
                        }
                    }
                }
                LoaderInfo = LoaderInfo;
                PackageName = ((IArchiveItemsAdapter)archiver).ArchiveName;
            }
        #endregion Constructors

        #region Methods
            #region Static Methods
                /// <summary>
                /// Load an existing package
                /// </summary>
                /// <param name="packageName">The name of the package to be loaded</param>
                /// <returns></returns>
                static public Package Load(string packageName)
                {
                    Archiver archiver = Archiver.Load(packageName);
                    return new Package(archiver);                    
                }
                /// <summary>
                /// Create a new package
                /// Note : user will need to name the package before calling saving it
                /// </summary>
                /// <returns></returns>
                static public Package Create(bool isFailureAnalysis)
                {
                    Package retVal = new Package();
                    retVal._isFailureAnalysis = isFailureAnalysis;
                    return retVal;
                }
                /// <summary>
                /// Create a new package
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="isFailureAnalysis">Inform the API if the package to be created is a Failure analysis package</param>
                /// <returns></returns>
                static public Package Create(string packageName, bool isFailureAnalysis)
                {
                    Package retVal = Create(isFailureAnalysis);
                    retVal._packageName = packageName;
                    return retVal;

                }
                /// <summary>
                /// Create a master package for image compare
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="masterBmp">The bitmap to act as master</param>
                /// <returns></returns>
                static public Package Create(string packageName, Bitmap masterBmp)
                {
                    Package retVal = Create(packageName, false);
                    retVal.MasterBitmap = masterBmp;
                    return retVal;

                }
                /// <summary>
                /// Create a master package for analytical model compare
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="masterBmp">The bitmap used to create the master model</param>
                /// <param name="masterModel">The model to act as master</param>
                /// <returns></returns>
                static public Package Create(string packageName, Bitmap masterBmp, Stream masterModel)
                {
                    Package retVal = Create(packageName, false);
                    retVal.PackageCompare = PackageCompareTypes.ModelAnalytical;
                    retVal.MasterBitmap = masterBmp;
                    retVal.MasterModel = masterModel;
                    return retVal;
                }
                /// <summary>
                /// Create a new FailureResolution package containg Image compare results
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="masterBmp">The master bitmap</param>
                /// <param name="capturedBmp">The bitmap rendered on screen</param>
                /// <returns></returns>
                static public Package Create(string packageName, Bitmap masterBmp, Bitmap capturedBmp)
                {
                    Package retVal = Create(packageName, true);
                    retVal.MasterBitmap = masterBmp;
                    retVal.CapturedBitmap = capturedBmp;
                    retVal.PackageCompare = PackageCompareTypes.ImageCompare;
                    return retVal;
                }
                /// <summary>
                /// Create a new FailureResolution package containg Image compare results (include tolerance)
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="masterBmp">The master bitmap</param>
                /// <param name="capturedBmp">The bitmap rendered on screen</param>
                /// <param name="tolerance">The tolerance (control points) used</param>
                /// <returns></returns>
                static public Package Create(string packageName, Bitmap masterBmp, Bitmap capturedBmp, XmlNode tolerance)
                {
                    Package retVal = Create(packageName, masterBmp, capturedBmp);
                    retVal.Tolerance = tolerance;
                    return retVal;
                }
                /// <summary>
                /// Create a new FailureResolution package containg analytical model results
                /// </summary>
                /// <param name="packageName">The name of the package to be created</param>
                /// <param name="masterBmp">The master bitmap</param>
                /// <param name="masterModel">The model associated with the master bitmap</param>
                /// <param name="capturedBmp">The bitmap rendered on screen</param>
                /// <param name="xmlDiff">The difference between the two models</param>
                /// <returns></returns>
                static public Package Create(string packageName, Bitmap masterBmp, Stream masterModel, Bitmap capturedBmp, XmlNode xmlDiff)
                {
                    Package retVal = Create(packageName, masterBmp, capturedBmp);
                    retVal.PackageCompare = PackageCompareTypes.ModelAnalytical;
                    retVal.MasterModel = masterModel;
                    retVal.XmlDiff = xmlDiff;
                    return retVal;

                }
            #endregion Static Methods
            #region Private Methods
                private void AddBitmapFile(Bitmap bmp, string bitmapName)
                {
                    if (bmp == null)
                    {
                        throw new ArgumentNullException("Bitmap must be valid ('null' value passed in as MasterBitmap)");
                    }

                    Bitmap bmpClone = (Bitmap)bmp.Clone();

                    // BUGBUG : Saving bitmap as BMP will drop most metadata info
                    // FIX : Save as image as TIFF (retains all EXIF info).
                    bmpClone.Save(bitmapName, System.Drawing.Imaging.ImageFormat.Tiff);
                    bmpClone.Dispose();
                    _archiver.AddFile(bitmapName, bitmapName, true);

                }
                private void AddStreamFile(Stream stream, string streamName)
                {
                    if (stream == null)
                    {
                        throw new IOException("Stream passed in cannot be null");
                    }
                    if (stream.CanRead == false)
                    {
                        throw new IOException("Cannot access the Stream named '" + streamName + "' (was it previously closed ?)");
                    }

                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(streamName, FileMode.Create);
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        fileStream.Write(buffer, 0, buffer.Length);
                        _archiver.AddFile(streamName, streamName, true);
                    }
                    finally 
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                }
                private void AddXmlNode(XmlNode node, string xmlName)
                {
                    if (node == null)
                    {
                        throw new IOException("Node passed in cannot be null");
                    }
                    FileStream fileStream = null;
                    try
                    {
                        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlNode));
                        fileStream = new FileStream(Path.GetFileName(xmlName), FileMode.Create);
                        serializer.Serialize(fileStream, node);
                        fileStream.Flush();
                        _archiver.AddFile(Path.GetFileName(xmlName), Path.GetFileName(xmlName), true);
                    }
                    finally
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                }
                private Bitmap GetBitmapFile(string bitmapName)
                {
                    if (_keys.Count == 0)
                    { 
                        throw new ApplicationException("You need to call LoadPackage before trying to access it");
                    }
                    if (_keys.Contains(MASTERBMP) == false)
                    {
                        return null;
                    }
                    int index = _keys.IndexOf(MASTERBMP);

                    return ImageUtility.GetUnlockedBitmap(_keys[index].ToString());
                }
                private Stream GetStreamFile(string streamName)
                {
                    if (_keys.Count == 0)
                    {
                        throw new ApplicationException("You need to call LoadPackage before trying to access it");
                    }

                    if (_keys.Contains(streamName) == false)
                    {
                        return null;
                    }

                    int index = _keys.IndexOf(streamName);

                    FileStream fileStream = new FileStream(streamName, FileMode.Open);
                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    MemoryStream memoryStream = new MemoryStream(buffer, 0, buffer.Length, false, true);

                    return memoryStream;
                }
                private XmlNode GetXmlNode(string xmlName)
                {
                    if (_keys.Count == 0)
                    {
                        throw new ApplicationException("You need to call LoadPackage before trying to access it");
                    }
                    if (_keys.Contains(Path.GetFileName(xmlName)) == false)
                    {
                        return null;
                    }

                    int index = _keys.IndexOf(Path.GetFileName(xmlName));
                    XmlNode retVal = null;

                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlNode));
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(xmlName, FileMode.Open);
                        retVal = (XmlNode)serializer.Deserialize(fileStream);
                    }
                    finally 
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                    return retVal;
                }
                private void CheckWellFormedPackage()
                {
                    // Check if Package is well formed
                    if (_packageName == null || _packageName.Trim() == string.Empty)
                    {
                        throw new PackageException("You must give a valid name to the package (null/empty/whitespace are invalid)");
                    }
                    if (_packageCompareTypes == PackageCompareTypes.None)
                    {
                        throw new PackageException("You must set the comparison type (one or many) before saving the package");
                    }

                    // Saving a standard Package
                    if (_isFailureAnalysis == false)
                    {
                        if (MasterBitmap == null)
                        {
                            throw new PackageException("The package must contains a master Bitmap before being saved");
                        }
                        if( (PackageCompareTypes.ModelAnalytical & PackageCompare) != 0)
                        {
                            if (MasterModel == null)
                            {
                                throw new PackageException("The package must contains a master model before being saved");
                            }
                        }
                    }
                    else
                    {
                        // TODO : Check if the following is still true : "Master can be missing (on capture run)"

                        // saving a Failure Analysis package, more requirement.
                        if (CapturedBitmap == null)
                        {
                            throw new PackageException("The package must contains a Captured Bitmap before being saved");
                        }
                        if ( (PackageCompare & PackageCompareTypes.ModelAnalytical) != 0 )
                        {
                            if(/*CapturedModel == null ||*/ XmlDiff == null)
                            {
                                throw new PackageException("The package must contains a rendered model and a XmlDiff file before being saved");
                            }
                        }
                    }
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Save the package on disk
                /// </summary>
                public void Save()
                {
                    CheckWellFormedPackage();

                    // BUGBUG : Not MultiThreaded safe because it dumps all resources to disk
                    // FIX : flushing of files and saving if package.
                    lock (_staticLock)
                    {
                        // Create archiver
                        _archiver = Archiver.Create(_packageName, true);
                        // Add all entries
                        IDictionaryEnumerator iter = _entries.GetEnumerator();
                        while (iter.MoveNext())
                        {
                            string key = (string)iter.Key;
                            Stream stream = iter.Value as Stream;
                            Bitmap bitmap = iter.Value as Bitmap;
                            XmlNode node = iter.Value as XmlNode;
                            Enum enumerator = iter.Value as Enum;

                            if (stream != null)
                            {
                                AddStreamFile(stream, key);
                            }
                            if (bitmap != null)
                            {
                                AddBitmapFile(bitmap, key);
                            }
                            if (node != null)
                            {
                                AddXmlNode(node, key);
                            }
                            if (enumerator != null)
                            {
                                // Add entry to Extra xml
                            }
                        }

                        // Add extra info xml
                        XmlTextWriter textWriter = null;
                        try
                        {
                            textWriter = new XmlTextWriter(LOADERINFO, System.Text.UTF8Encoding.UTF8);
                            textWriter.Formatting = Formatting.Indented;
                            //                        textWriter.Settings.OmitXmlDeclaration = true;
                            textWriter.WriteStartElement("LoaderInfo");
                            textWriter.WriteElementString("IsFailureAnalysis", IsFailureAnalysis.ToString());
                            textWriter.WriteElementString("PackageCompareTypes", ((int)PackageCompare).ToString());
                            textWriter.WriteElementString("ChannelCompareMode", ((int)ChannelCompare).ToString());
                            if (MasterSDLocation != null && MasterSDLocation != String.Empty)
                            {
                                textWriter.WriteElementString("MasterSDLocation", MasterSDLocation);
                            }
                            if (CommandLine != null && CommandLine != string.Empty)
                            {
                                textWriter.WriteElementString("CommandLine", CommandLine);
                            }
                            textWriter.WriteEndElement();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            if (textWriter != null)
                            {
                                textWriter.Flush();
                                textWriter.Close();
                            }
                        }
                        _archiver.AddFile(LOADERINFO, LOADERINFO, true);

                        // Generate package
                        _archiver.GenerateArchive();

                    } // File has been saved to disk, we can now release the Lock

                }
            #endregion Public Methods
        #endregion Methods

        #region IDisposable implementation
            /// <summary>
            /// Release all allocated/locked resources
            /// </summary>
            public void Dispose()
            {
                if (MasterBitmap != null) {  MasterBitmap.Dispose(); MasterBitmap = null; }
                if (CapturedBitmapBeforeFilters != null) { CapturedBitmapBeforeFilters.Dispose(); CapturedBitmapBeforeFilters = null; }
                if (CapturedBitmap != null) {  CapturedBitmap.Dispose(); CapturedBitmap = null; }

                if(MasterModel != null) { MasterModel.Close(); MasterModel = null;}
                if(CapturedModelBeforeFilters != null) { CapturedModelBeforeFilters.Close(); CapturedModelBeforeFilters = null;}
                if(CapturedModel != null) { CapturedModel.Close(); CapturedModel = null;}

            }
        #endregion IDisposable implementation
    }
}
