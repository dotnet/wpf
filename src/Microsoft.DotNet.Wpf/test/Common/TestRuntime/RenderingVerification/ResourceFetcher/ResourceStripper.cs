// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.ResourceFetcher
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
        using System.Resources;
        using System.Reflection;
        using System.Collections;
        using System.Globalization;
        using System.Windows.Forms;
        using System.Drawing.Imaging;
        using System.Runtime.InteropServices;
        using Microsoft.Test.RenderingVerification.ResourceFetcher;
        using Microsoft.Test.RenderingVerification;
    #endregion usings


    /// <summary>
    /// Describe a type of resource
    /// (Mapping from winuser.h)
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Type for Cursors
        /// </summary>
        Cursor          =   1,
        /// <summary>
        /// Type for Bitmaps
        /// </summary>
        Bitmap          =   2,
        /// <summary>
        /// Type for Icons
        /// </summary>
        Icon            =   3,
        /// <summary>
        /// Type for Menus
        /// </summary>
        Menu            =   4,
        /// <summary>
        /// Type for Dialogs
        /// </summary>
        Dialog          =   5,
        /// <summary>
        /// Type for Strings
        /// </summary>
        StringTable     =   6,
        /// <summary>
        /// Type for Font directories
        /// </summary>
        FontDirectory   =   7,
        /// <summary>
        /// Type for Fonts
        /// </summary>
        Font            =   8,
        /// <summary>
        /// Type for Accelerator keys
        /// </summary>
        Accelerator     =   9,
        /// <summary>
        /// Type for User Defined Data
        /// </summary>
        RawData         =   10,
        /// <summary>
        /// Type for Messages
        /// </summary>
        MessageTable    =   11,
        /// <summary>
        /// ???
        /// </summary>
        GroupCursor     =   12,
        /// <summary>
        /// ???
        /// </summary>
        GroupIcon       =   14,
        /// <summary>
        /// Type for Versions
        /// </summary>
        Version         =   16,
        /// <summary>
        /// Type for DlgDialogs
        /// </summary>
        DlgInclude      =   17,
        /// <summary>
        /// Type for Plug-and-plays
        /// </summary>
        PlugAndPlay     =   19,
        /// <summary>
        /// Type for VXDs files
        /// </summary>
        Vxd             =   20,
        /// <summary>
        /// Type for Animated Cursors
        /// </summary>
        AnimatedCursor  =   21,
        /// <summary>
        /// Type for Animated Icons
        /// </summary>
        AnimatedIcon    =   22,
        /// <summary>
        /// Type for Html files
        /// </summary>
        Html            =   23,
        /// <summary>
        /// Type for Manifests Xml files(require OS > XP)
        /// </summary>
        Manifest        =   24
   }

    /// <summary>
    /// Summary description for ResourceStripper.
    /// </summary>
    internal class ResourceStripper : IDisposable
    {
        #region Properties
            #region Private Properties
                private string _fileName = string.Empty;
                private IntPtr _hInstance = IntPtr.Zero;
                private AppDomain _appDomain = null;
                private Assembly _assembly = null;
                private ArrayList _extractedResourceNames = null;
                private bool IsManagedCode
                {
                    get { return (_assembly != null); }
                }
                private bool IsNativeCode
                {
                    get { return (_hInstance != IntPtr.Zero); }
                }
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get/set the file name of the exe/dll/ocx to grab the resources from
                /// </summary>
                /// <value></value>
                public string FileName
                {
                    get { return _fileName; }
                    set 
                    {
                        if (value.Trim().ToLower(CultureInfo.InvariantCulture) == _fileName.Trim().ToLower(CultureInfo.InvariantCulture)) { return; }
                        if (File.Exists(value) == false) { throw new FileNotFoundException("The specified file was not found, check the path (network issue ?)", value); }

                        if (_hInstance != IntPtr.Zero) { FreeLibrary(); }
                        if (_appDomain != null) { AppDomain.Unload(_appDomain); _appDomain = null; }

                        _fileName = value;
                        _assembly = null;
                        _hInstance = IntPtr.Zero;

                        try
                        {
                            _appDomain = AppDomain.CreateDomain("_appDomain");
                            _assembly = _appDomain.Load(Assembly.LoadFile(_fileName).GetName());
                        }
                        catch(BadImageFormatException)
                        {
                            AppDomain.Unload(_appDomain);
                            _appDomain = null;
                            LoadLibrary();
                        }
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instanciate a new object of this type
            /// </summary>
            /// <param name="fileName"></param>
            public ResourceStripper(string fileName)
            {
                // Load the file containing the resources
                FileName = fileName;
                _extractedResourceNames = new ArrayList();
            }
            /// <summary>
            /// Finalizer for ResourceStripper, will get eventually called in case caller forgot to call Dispose
            /// </summary>
            ~ResourceStripper()
            {
                if (_hInstance != IntPtr.Zero)
                {
                    FreeLibrary();
                    _fileName = null;
                }
                _assembly = null;
                if (_appDomain != null) { AppDomain.Unload(_appDomain); _appDomain = null; }
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void CreateDirectory(string folderName)
                {
                    folderName = folderName.Trim().ToLower(CultureInfo.InvariantCulture);
                    string[] folders = folderName.Split('\\');
                    string currentFolder = string.Empty;
                    for (int t = 0; t < folders.Length; t++) 
                    {
                        currentFolder += folders[t] + "\\";
                        if (Directory.Exists(currentFolder) == false)
                        {
                            Log("Directory '" + currentFolder + "' does not exist creating it...");
                            Directory.CreateDirectory(currentFolder);
                        }
                    }
                }
                private void Log(string message)
                {
                    Console.WriteLine(message);
                }
                private void FlushStreamToDisk(Stream stream, string folderName, short resourceLang, IntPtr resourceType, IntPtr resourceName)
                {
                    bool unknownType = true;

                    string fileName = Path.Combine(folderName, resourceLang.ToString() + "\\");
                    CreateDirectory(fileName);

                    string name = ConvertToIntPtrToObject(resourceName).ToString();
                    fileName += name;
                    object type = ConvertToIntPtrToObject(resourceType);
                    string extension = "." + type.ToString();

                    if ((type is string) == false)
                    {
                        if (Enum.IsDefined(typeof(ResourceType), type))
                        {

                            switch ((ResourceType)type)
                            {
                                case ResourceType.AnimatedIcon:
                                case ResourceType.Icon:
                                    using (Bitmap bmp = (Bitmap)Image.FromStream(stream))
                                    {
                                        bmp.Save(fileName + ".ico");
                                    }
                                    unknownType = false;
                                    break;
                                case ResourceType.AnimatedCursor:
                                case ResourceType.Cursor:
                                    unknownType = false;
                                    using (Bitmap cur = (Bitmap)Image.FromStream(stream))
                                    {
                                        cur.Save(fileName + ".cur");
                                    }
                                    break;
                                case ResourceType.Bitmap:
                                    unknownType = false;
                                    using (Bitmap bmp = (Bitmap)Bitmap.FromStream(stream))
                                    {
                                        bmp.Save(fileName + ".bmp");
                                    }
                                    break;
                                case ResourceType.Accelerator: extension = ".acc"; break;
                                case ResourceType.Dialog: extension = ".dlg"; break;
                                case ResourceType.DlgInclude: extension = ".dlgInc"; break;
                                case ResourceType.Font: extension = ".font"; break;
                                case ResourceType.FontDirectory: extension = ".fontDir"; break;
                                case ResourceType.Html: extension = ".html"; break;
                                case ResourceType.Manifest: extension = ".xml"; break;
                                case ResourceType.Menu: extension = ".menu"; break;
                                case ResourceType.MessageTable: extension = ".msgTable"; break;
                                case ResourceType.PlugAndPlay: extension = ".pnp"; break;
                                case ResourceType.RawData: extension = ".raw"; break;
                                case ResourceType.StringTable: extension = ".stringTable"; break;
                                case ResourceType.Version: extension = ".ver"; break;
                                case ResourceType.Vxd: extension = ".vxd"; break;
                                case ResourceType.GroupCursor:
                                case ResourceType.GroupIcon:
                                    break;
                                default: throw new NotSupportedException("Unsupported argument passed in");
                            }
                        }
                    }

                    if (unknownType)
                    {
                        fileName += extension;
                        FileStream fs = null;
                        byte[] buffer = new byte[stream.Length];
                        long position = 0;
                        position = stream.Position;
                        try
                        {
                            stream.Flush();
                            stream.Position = 0;
                            stream.Read(buffer, 0, buffer.Length);
                            fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                            fs.Write(buffer, 0, buffer.Length);
                        }
                        finally
                        {
                            if (fs != null) { fs.Close(); fs = null; }
                            stream.Position = position;
                        }
                    }
                }

                #region Native Interop extraction methods
                    private void LoadLibrary()
                    {
                        if(_hInstance != IntPtr.Zero) { throw new ApplicationException("You need to call FreeLibrary before calling LoadLibrary one more time");}
                        _hInstance = Kernel32.LoadLibrary(_fileName);
                    }
                    private bool FreeLibrary()
                    {
                        int retVal = 0;
                        if (_hInstance == IntPtr.Zero) { throw new ApplicationException("You need to call LoadLibrary before calling FreeLibrary (or FreeLibrary already called)"); }
                        retVal = Kernel32.FreeLibrary(_hInstance);
                        _hInstance = IntPtr.Zero;
                        return (retVal != 0);
                    }

                    private Stream NativeGetResource(IntPtr resourceType, IntPtr resourceName, short resourceLanguage)
                    {

                        IntPtr hResInfo = IntPtr.Zero;
                        IntPtr hGlobal = IntPtr.Zero;
                        IntPtr pResource = IntPtr.Zero;
                        MemoryStream memoryStream = null;

                        try
                        {
                            hResInfo = Kernel32.FindResourceEx(_hInstance, resourceType, resourceName, resourceLanguage);
                            if (hResInfo == IntPtr.Zero) { throw new ExternalException("Call to Native API ('FindResourceEx') failed. Check the parameters passed in."); } ;
                            hGlobal = Kernel32.LoadResource(_hInstance, hResInfo);
                            if (hGlobal == IntPtr.Zero) { throw new ExternalException("Call to Native API ('LoadResource') failed."); } ;
                            pResource = Kernel32.LockResource(hGlobal);
                            if (pResource == IntPtr.Zero) { throw new ExternalException("Call to Native API ('LockResource') failed."); } ;
                            int size = Kernel32.SizeofResource(_hInstance, hResInfo);
                            if (size == 0) { throw new ExternalException("Call to Native API ('SizeOfResource') failed."); } ;

                            byte[] buffer = new byte[size];
                            System.Runtime.InteropServices.Marshal.Copy(pResource, buffer, 0, buffer.Length);
                            object  resource = ConvertToIntPtrToObject(resourceType);
                            if (resource is int)
                            {
                                switch ((ResourceType)resource)
                                { 
                                    case ResourceType.AnimatedCursor:
                                    case ResourceType.Cursor:
                                        memoryStream = SaveIconCursorToStream(buffer, true);
                                        break;
                                    case ResourceType.AnimatedIcon:
                                    case ResourceType.Icon:
                                        memoryStream = SaveIconCursorToStream(buffer, false);
                                        break;
                                    case ResourceType.Bitmap:
                                        memoryStream = SaveBitmapToStream(pResource);
                                        break;
                                }
                            }
                            if (memoryStream == null) { memoryStream = new MemoryStream(buffer); }
                        }
                        finally
                        {
                            if (hGlobal != IntPtr.Zero) { Kernel32.FreeResource(hGlobal); hGlobal = IntPtr.Zero; }
                        }
                        return memoryStream;
                    }
                    private object[] NativeGetResourceTypes()
                    {
                        _extractedResourceNames.Clear();
                        Kernel32.EnumResourceTypeProc callback = new Kernel32.EnumResourceTypeProc(NativeEnumResourceTypeProc);
                        Kernel32.EnumResourceTypes(_hInstance, callback, IntPtr.Zero);
                        return _extractedResourceNames.ToArray();
                    }
                    private object[] NativeGetResourceNames(IntPtr resourceType)
                    {
                        _extractedResourceNames.Clear();
                        Kernel32.EnumResourceNamesProc callback = new Kernel32.EnumResourceNamesProc(NativeEnumResourceNamesProc);
                        Kernel32.EnumResourceNames(_hInstance, resourceType, callback, IntPtr.Zero);
                        return _extractedResourceNames.ToArray();
                    }
                    private object[] NativeGetResourceLanguages(IntPtr resourceType, IntPtr name)
                    {
                        _extractedResourceNames.Clear();
                        Kernel32.EnumResourceLanguagesProc callback = new Kernel32.EnumResourceLanguagesProc(NativeEnumResourceLanguagesProc);
                        Kernel32.EnumResourceLanguages(_hInstance, resourceType, name, callback, IntPtr.Zero);
                        return _extractedResourceNames.ToArray();
                    }

                    private int NativeEnumResourceTypeProc(IntPtr hModule, IntPtr type, IntPtr lParam)
                    {
                        _extractedResourceNames.Add(type);
                        return Win32StdConst.TRUE;
                    }
                    private int NativeEnumResourceNamesProc(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam)
                    {
                        _extractedResourceNames.Add(name);
                        return Win32StdConst.TRUE;
                    }
                    private int NativeEnumResourceLanguagesProc(IntPtr hModule, IntPtr type, IntPtr name, Int16 idDLanguage, IntPtr lParam)
                    {
                        _extractedResourceNames.Add(idDLanguage);
                        return Win32StdConst.TRUE;
                    }

                    private object ConvertToIntPtrToObject(IntPtr resourcePtr)
                    {
                        if ((resourcePtr.ToInt64() & 0xFFFF) == resourcePtr.ToInt64())
                        {
                            // This is an index
                            return resourcePtr.ToInt32();    // ToInt32 should work on 64 bits OS as well since value < 65535
                        }
                        else
                        {
                            // This is a pointer to string
                            return System.Runtime.InteropServices.Marshal.PtrToStringAuto(resourcePtr);
                        }
                    }
                    private IntPtr ManagedToNativeString(string stringToReference)
                    {
                        return Marshal.StringToCoTaskMemUni(stringToReference);
                    }
                    private MemoryStream SaveBitmapToStream(IntPtr pBits)
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        IntPtr hdc = IntPtr.Zero;
                        IntPtr hPalette = IntPtr.Zero;
                        try
                        {
                            hdc = User32.GetDC(IntPtr.Zero);
                            if (hdc == IntPtr.Zero) { throw new ExternalException("Call to native API (GetDC) failed"); }


                            // Compute bitmap size and create it if needed
                            int paletteSize = 0;

                            IntPtr structBmpInfoPtr = new IntPtr(pBits.ToInt64() + Marshal.OffsetOf(typeof(GDI32.BITMAPINFOHEADER), "biSize").ToInt64());
                            int structBmpInfoSize = Marshal.ReadInt32(structBmpInfoPtr);

                            IntPtr bitCountPtr = new IntPtr(pBits.ToInt64() + Marshal.OffsetOf(typeof(GDI32.BITMAPINFOHEADER), "biBitCount").ToInt64());
                            int bitCount = Marshal.ReadInt32(bitCountPtr);
                            if (bitCount <= 8)
                            {
                                paletteSize = 1 << bitCount;
                                IntPtr colorUsedPtr = new IntPtr(pBits.ToInt64() + Marshal.OffsetOf(typeof(GDI32.BITMAPINFOHEADER), "biClrUsed").ToInt64());
                                int colorUsed = Marshal.ReadInt32(colorUsedPtr);
                                if (colorUsed > 0)
                                {
                                    paletteSize = colorUsed;
                                }
                                paletteSize *= 4;  // 4 -> sizeof(RGBQUAD)

                                // Create palette
                                IntPtr palettePtr = IntPtr.Zero;
                                try
                                {
                                    int structLogPaletteSize = Marshal.SizeOf(typeof(GDI32.LOGPALETTE));
                                    palettePtr = Marshal.AllocHGlobal(structLogPaletteSize + paletteSize);
                                    IntPtr palVersion = new IntPtr(palettePtr.ToInt64() + Marshal.OffsetOf(typeof(GDI32.LOGPALETTE), "palVersion").ToInt64());
                                    IntPtr palNumEntries = new IntPtr(palettePtr.ToInt64() + Marshal.OffsetOf(typeof(GDI32.LOGPALETTE), "palNumEntries").ToInt64());
                                    Marshal.WriteInt16(palVersion, 0x300);
                                    Marshal.WriteInt16(palNumEntries, (short)(paletteSize / 4));
                                    for (int t = 0; t < paletteSize; t+=4)
                                    {
                                        IntPtr pRgbfBits = new IntPtr(pBits.ToInt64() + structBmpInfoSize + t);
                                        IntPtr pRgbfPallette = new IntPtr(palettePtr.ToInt64() + structLogPaletteSize + t);
                                        Marshal.WriteInt32(pRgbfPallette, Marshal.ReadInt32(pRgbfBits));    // BUGBUG : "Flag" byte should be set to 0 
                                    }
                                    hPalette = GDI32.CreatePalette(palettePtr);
                                    if (hPalette == IntPtr.Zero) { throw new ExternalException("Call to native API (CreatePalette) failed"); }
                                }
                                finally
                                {
                                    if (palettePtr != IntPtr.Zero) { Marshal.FreeHGlobal(palettePtr); palettePtr = IntPtr.Zero; }
                                }
                            }

                            // Create hBitmap, create Bitmap from it and save as stream
                            IntPtr offsetToBits = new IntPtr(pBits.ToInt64() + structBmpInfoSize + paletteSize);
                            IntPtr hBitmap = GDI32.CreateDIBitmap(hdc, pBits, GDI32.CBM_INIT, offsetToBits, pBits, GDI32.DIB_RGB_COLORS);
                            if (hBitmap == IntPtr.Zero) { throw new ExternalException("Call to native API (CreateDIBitmap) failed"); }
                            using (Bitmap bmp = (hPalette == IntPtr.Zero) ? Bitmap.FromHbitmap(hBitmap) : Bitmap.FromHbitmap(hBitmap, hPalette))
                            {
                                bmp.Save(memoryStream, ImageFormat.Png);
                            }
                            // BUGBUG : Should I release the Handle to bitmap ?
                        }
                        finally
                        {
                            if (hdc != IntPtr.Zero) { User32.ReleaseDC(IntPtr.Zero, hdc); hdc = IntPtr.Zero; }
                            if (hPalette != IntPtr.Zero) { GDI32.DeleteObject(hPalette); hPalette = IntPtr.Zero; }
                        }
                        return memoryStream;
                    }
                    private MemoryStream SaveIconCursorToStream(byte[] buffer, bool isCursor)
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        IntPtr hIcon = User32.CreateIconFromResource(buffer, isCursor);
                        if (hIcon == IntPtr.Zero) { throw new ExternalException("Native call API failed (CreateIconFromResource)"); }
                        using (Icon icon = Icon.FromHandle(hIcon))
                        {
                            icon.Save(memoryStream);
                        }
                        // BUGBUG : Should I release the Handle to Icon/Cursor ?
                        return memoryStream;
                    }
                #endregion Native Interop extraction methods
                #region Managed Extraction Methods
                    private Stream ManagedGetResource(IntPtr resourceType, IntPtr resourceName, int resourceLanguage)
                    {
                        MemoryStream retVal = new MemoryStream();
                        object resourceObject = null;

                        string resourceNameString = System.Runtime.InteropServices.Marshal.PtrToStringUni(resourceName);

                        string[] res = resourceNameString.Split('/');
                        if (res.Length != 2) { throw new ArgumentException("Unexpected syntax, The ResourceName must contain the full type and the name of the resource (expl : 'System.MyAssembly.Class1/welcome.bmp')"); }

                        Type type = _assembly.GetType(res[0], true, true);
                        ResourceManager resources = new ResourceManager(type);
                        try
                        {
                            resourceObject = resources.GetObject(res[1]);
                            switch (resourceType.ToInt32())
                            {
                                case (int)ResourceType.Bitmap: ((Bitmap)resourceObject).Save(retVal, ImageFormat.Png); break;
                                case (int)ResourceType.Icon: ((Icon)resourceObject).Save(retVal); break;
                                case (int)ResourceType.Cursor:                               
                                    using ( Icon cursor = Icon.FromHandle(((Cursor)resourceObject).Handle) )
                                    {
                                        cursor.Save(retVal);
                                    }
                                    break;
                                default: 
                                    throw new NotImplementedException();
                            }
                        }
                        finally 
                        {
                            if (resourceObject != null && resourceObject is IDisposable) { ((IDisposable)resourceObject).Dispose();}
                            resourceObject = null;
                        }

                        return retVal;
                    }
                    private object[] ManagedGetResourceTypes()
                    {
                        throw new NotImplementedException();
                    }
                    private object[] ManagedGetResourceNames(IntPtr resourceType)
                    {
                        throw new NotImplementedException();
                    }
                    private object[] ManagedGetResourceLanguage(IntPtr resourceType, IntPtr resourceName)
                    {
                        throw new NotImplementedException();
                    }
                #endregion Managed Extraction Methods
                #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Retrieve all the type of resources used in this file
                /// </summary>
                /// <returns></returns>
                public IntPtr[] GetResourceTypes()
                {
                    ArrayList list = new ArrayList();
                    if (IsManagedCode) { list.AddRange(ManagedGetResourceTypes()); }
                    if (IsNativeCode) { list.AddRange(NativeGetResourceTypes()); }
                    return (IntPtr[])list.ToArray(typeof(IntPtr));
                }
                /// <summary>
                /// Get an array of all the resource exisiting in this library/executable/assembly
                /// </summary>
                /// <param name="resourceType">The type of resource to get</param>
                /// <returns>An array of object containg the name (or index for some native resources) of the resources</returns>
                public object[] GetResourceNames(ResourceType resourceType)
                {
                    object[] retVal = null;

                    IntPtr[] result = GetResourceNames(new IntPtr((int)resourceType));
                    retVal = new object[result.Length];
                    for (int t = 0; t < retVal.Length; t++)
                    {
                        retVal[t] = ConvertToIntPtrToObject(result[t]);
                    }
                    return retVal;
                }
                /// <summary>
                /// Get an array of all the resource exisiting in this library/executable/assembly
                /// </summary>
                /// <param name="resourceType"></param>
                /// <returns></returns>
                public IntPtr[] GetResourceNames(IntPtr resourceType)
                {
                    ArrayList list = new ArrayList();
                    if (IsManagedCode) { list.AddRange(ManagedGetResourceNames(resourceType)); }
                    if (IsNativeCode) { list.AddRange(NativeGetResourceNames(resourceType)); }
                    return (IntPtr[]) list.ToArray(typeof(IntPtr));
                }
                /// <summary>
                /// Return the extracted resource as stream
                /// </summary>
                /// <param name="resourceType"></param>
                /// <param name="resourceName"></param>
                /// <param name="culture"></param>
                /// <returns></returns>
                public Stream GetResource(ResourceType resourceType, object resourceName, CultureInfo culture)
                {
                    Stream retVal = null;
                    IntPtr resourcePtr = IntPtr.Zero;
                    try
                    {
                        if (resourceName is string) { resourcePtr = ManagedToNativeString((string)resourceName); }
                        else { resourcePtr = new IntPtr((int)resourcePtr); }

                        retVal = GetResource(new IntPtr((int)resourceType), resourcePtr, (short)culture.LCID);
                    }
                    finally
                    {
                        if (resourceName is string && resourcePtr != IntPtr.Zero) { Marshal.FreeBSTR(resourcePtr); resourcePtr = IntPtr.Zero; }
                    }
                    return retVal;
                }
                /// <summary>
                /// Return the extracted resource as stream
                /// </summary>
                /// <param name="resourceType"></param>
                /// <param name="resourceName"></param>
                /// <param name="CultureInfoLCID"></param>
                /// <returns></returns>
                public Stream GetResource(IntPtr resourceType, IntPtr resourceName, int CultureInfoLCID)
                {
                    Stream retVal = null;
                    if (IsManagedCode) { retVal = ManagedGetResource(resourceType, resourceName, CultureInfoLCID); }
                    if (retVal == null && IsNativeCode) { retVal = NativeGetResource(resourceType, resourceName, (short)CultureInfoLCID); }
                    return retVal;
                }
                /// <summary>
                /// Dump all the existing resource to the specified folder, mainly for debugging and inspection purposes
                /// </summary>
                /// <param name="folderName">Location of where to dump the resources. Folder must exist.</param>
                /// <param name="createFolder">Create the directory if it does not exist</param>
                public void DumpAllResources(string folderName, bool createFolder)
                {
                    // check param
                    if (folderName == null) { throw new ArgumentNullException("folderName", "parameter must be set to a valid instance (null was passed in)"); }
                    folderName = System.Environment.ExpandEnvironmentVariables(folderName);
                    if (Directory.Exists(folderName) == false) 
                    {
                        if (createFolder == false) { throw new DirectoryNotFoundException("The specified directory ('" + folderName + "') does not exist, create it first"); }
                        CreateDirectory(folderName);
                    }
                    if (folderName.EndsWith("\\") == false) { folderName = folderName + "\\"; }

                    foreach(IntPtr type in NativeGetResourceTypes())
                    {
                        foreach (IntPtr  name in NativeGetResourceNames(type))
                        {
                            foreach (Int16 language in NativeGetResourceLanguages(type, name))
                            {
                                Stream stream = GetResource(type, name, language);
                                FlushStreamToDisk(stream, folderName, language, type, name);
                            }
                        }
                    }
                }
            #endregion Public Methods
        #endregion Methods

        #region IDisposable Members
            /// <summary>
            /// Release all resources associated with this class
            /// </summary>
            public void Dispose()
            {
                if (_hInstance != IntPtr.Zero)
                {
                    FreeLibrary();
                    _fileName = null;
                    GC.SuppressFinalize(this);
                }
                _assembly = null;
                if (_appDomain != null) { AppDomain.Unload(_appDomain); _appDomain = null; }
            }
        #endregion
    }
}
