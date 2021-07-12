// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Interfaces and enums are taken from ShObjIdl.idl

namespace MS.Internal.AppModel
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Security;
    using System.Text;
    using MS.Win32;
    using MS.Internal.Interop;

    // Some COM interfaces and Win32 structures are already declared in the framework.
    // Interesting ones to remember in System.Runtime.InteropServices.ComTypes are:
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
    using IPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;
    using IStream = System.Runtime.InteropServices.ComTypes.IStream;

    #region Structs

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct COMDLG_FILTERSPEC
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszSpec;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct PKEY
    {
        /// <summary>fmtid</summary>
        private readonly Guid _fmtid;
        /// <summary>pid</summary>
        private readonly uint _pid;

        private PKEY(Guid fmtid, uint pid)
        {
            _fmtid = fmtid;
            _pid = pid;
        }

        /// <summary>PKEY_Title</summary>
        public static readonly PKEY Title = new PKEY(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);
        /// <summary>PKEY_AppUserModel_ID</summary>
        public static readonly PKEY AppUserModel_ID = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
        /// <summary>PKEY_AppUserModel_IsDestListSeparator</summary>
        public static readonly PKEY AppUserModel_IsDestListSeparator = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 6);
        /// <summary>PKEY_AppUserModel_RelaunchCommand</summary>
        public static readonly PKEY AppUserModel_RelaunchCommand = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 2);
        /// <summary>PKEY_AppUserModel_RelaunchDisplayNameResource</summary>
        public static readonly PKEY AppUserModel_RelaunchDisplayNameResource = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 4);
        /// <summary>PKEY_AppUserModel_RelaunchIconResource</summary>
        public static readonly PKEY AppUserModel_RelaunchIconResource = new PKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 3);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    internal struct THUMBBUTTON
    {
        /// <summary>
        /// WPARAM value for a THUMBBUTTON being clicked.
        /// </summary>
        public const int THBN_CLICKED = 0x1800;

        public THB dwMask;
        public uint iId;
        public uint iBitmap;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTip;
        public THBF dwFlags;
    }

    #endregion

    #region Interfaces

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.EnumIdList),
    ]
    internal interface IEnumIDList
    {
        [PreserveSig()]
        HRESULT Next(uint celt, out IntPtr rgelt, out int pceltFetched);
        [PreserveSig()]
        HRESULT Skip(uint celt);
        void Reset();
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumIDList Clone();
    }

    /// <summary>Unknown Object Array</summary>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ObjectArray),
    ]
    internal interface IObjectArray
    {
        uint GetCount();
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetAt([In] uint uiIndex, [In] ref Guid riid);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ObjectArray),
    ]
    interface IObjectCollection : IObjectArray
    {
        #region IObjectArray redeclarations
        new uint GetCount();
        [return: MarshalAs(UnmanagedType.IUnknown)]
        new object GetAt([In] uint uiIndex, [In] ref Guid riid);
        #endregion

        void AddObject([MarshalAs(UnmanagedType.IUnknown)] object punk);
        void AddFromArray(IObjectArray poaSource);
        void RemoveObjectAt(uint uiIndex);
        void Clear();
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.PropertyStore)
    ]
    internal interface IPropertyStore
    {
        uint GetCount();
        PKEY GetAt(uint iProp);

        void GetValue([In] ref PKEY pkey, [In, Out] PROPVARIANT pv);

        void SetValue([In] ref PKEY pkey, PROPVARIANT pv);

        void Commit();
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellFolder),
    ]
    internal interface IShellFolder
    {
        void ParseDisplayName(
            IntPtr hwnd,
            IBindCtx pbc,
            [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
            [In, Out] ref int pchEaten,
            out IntPtr ppidl,
            [In, Out] ref uint pdwAttributes);

        IEnumIDList EnumObjects(
            IntPtr hwnd,
            SHCONTF grfFlags);

        // returns an instance of a sub-folder which is specified by the IDList (pidl).
        // IShellFolder or derived interfaces
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToObject(
            IntPtr pidl,
            IBindCtx pbc,
            [In] ref Guid riid);

        // produces the same result as BindToObject()
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToStorage(IntPtr pidl, IBindCtx pbc, [In] ref Guid riid);

        // compares two IDLists and returns the result. The shell
        // explorer always passes 0 as lParam, which indicates 'sort by name'.
        // It should return 0 (as CODE of the scode), if two id indicates the
        // same object; negative value if pidl1 should be placed before pidl2;
        // positive value if pidl2 should be placed before pidl1.
        // use the macro ResultFromShort() to extract the result comparison
        // it deals with the casting and type conversion issues for you
        [PreserveSig]
        HRESULT CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

        // creates a view object of the folder itself. The view
        // object is a difference instance from the shell folder object.
        // 'hwndOwner' can be used  as the owner window of its dialog box or
        // menu during the lifetime of the view object.
        // This member function should always create a new
        // instance which has only one reference count. The explorer may create
        // more than one instances of view object from one shell folder object
        // and treat them as separate instances.
        // returns IShellView derived interface
        [return: MarshalAs(UnmanagedType.Interface)]
        object CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid);

        // returns the attributes of specified objects in that
        // folder. 'cidl' and 'apidl' specifies objects. 'apidl' contains only
        // simple IDLists. The explorer initializes *prgfInOut with a set of
        // flags to be evaluated. The shell folder may optimize the operation
        // by not returning unspecified flags.
        void GetAttributesOf(
            uint cidl,
            IntPtr apidl,
            [In, Out] ref SFGAO rgfInOut);

        // creates a UI object to be used for specified objects.
        // The shell explorer passes either IID_IDataObject (for transfer operation)
        // or IID_IContextMenu (for context menu operation) as riid
        // and many other interfaces
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetUIObjectOf(
            IntPtr hwndOwner,
            uint cidl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysInt, SizeParamIndex = 1)] IntPtr apidl,
            [In] ref Guid riid,
            [In, Out] ref uint rgfReserved);

        // returns the display name of the specified object.
        // If the ID contains the display name (in the locale character set),
        // it returns the offset to the name. Otherwise, it returns a pointer
        // to the display name string (UNICODE), which is allocated by the
        // task allocator, or fills in a buffer.
        // use the helper APIS StrRetToStr() or StrRetToBuf() to deal with the different
        // forms of the STRRET structure
        void GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out IntPtr pName);

        // sets the display name of the specified object.
        // If it changes the ID as well, it returns the new ID which is
        // alocated by the task allocator.
        void SetNameOf(IntPtr hwnd,
            IntPtr pidl,
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            SHGDN uFlags,
            out IntPtr ppidlOut);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellItem),
    ]
    internal interface IShellItem
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToHandler(IBindCtx pbc, [In] ref Guid bhid, [In] ref Guid riid);

        IShellItem GetParent();

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetDisplayName(SIGDN sigdnName);

        uint GetAttributes(SFGAO sfgaoMask);

        int Compare(IShellItem psi, SICHINT hint);
    }

    /// <summary>
    /// Shell Namespace helper 2
    /// </summary>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellItem2),
    ]
    interface IShellItem2 : IShellItem
    {
        #region IShellItem redeclarations
        [return: MarshalAs(UnmanagedType.Interface)]
        new object BindToHandler(IBindCtx pbc, [In] ref Guid bhid, [In] ref Guid riid);
        new IShellItem GetParent();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        new string GetDisplayName(SIGDN sigdnName);
        new SFGAO GetAttributes(SFGAO sfgaoMask);
        new int Compare(IShellItem psi, SICHINT hint);
        #endregion

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyStore(
            GPS flags,
            [In] ref Guid riid);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyStoreWithCreateObject(
            GPS flags,
            [MarshalAs(UnmanagedType.IUnknown)] object punkCreateObject,   // factory for low-rights creation of type ICreateObject
            [In] ref Guid riid);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyStoreForKeys(
            IntPtr rgKeys,
            uint cKeys,
            GPS flags,
            [In] ref Guid riid);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyDescriptionList(
            IntPtr keyType,
            [In] ref Guid riid);

        // Ensures any cached information in this item is up to date, or returns __HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) if the item does not exist.
        void Update(IBindCtx pbc);

        void GetProperty(IntPtr key, [In, Out] PROPVARIANT pv);

        Guid GetCLSID(IntPtr key);

        FILETIME GetFileTime(IntPtr key);

        int GetInt32(IntPtr key);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetString(IntPtr key);

        uint GetUInt32(IntPtr key);

        ulong GetUInt64(IntPtr key);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool GetBool(IntPtr key);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellItemArray),
    ]
    internal interface IShellItemArray
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToHandler(IBindCtx pbc, [In] ref Guid rbhid, [In] ref Guid riid);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyStore(int flags, [In] ref Guid riid);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetPropertyDescriptionList([In] ref PKEY keyType, [In] ref Guid riid);

        uint GetAttributes(SIATTRIBFLAGS dwAttribFlags, uint sfgaoMask);

        uint GetCount();

        IShellItem GetItemAt(uint dwIndex);

        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumItems();
    }

    [
        ComImport,
        InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellLink),
    ]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, [In, Out] WIN32_FIND_DATAW pfd, SLGP fFlags);
        IntPtr GetIDList();
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        short GetHotKey();
        void SetHotKey(short wHotKey);
        uint GetShowCmd();
        void SetShowCmd(uint iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [
        ComImport,
        InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ShellLinkDataList),
    ]
    internal interface IShellLinkDataList
    {
        [PreserveSig]
        Int32 AddDataBlock(IntPtr pDataBlock);

        [PreserveSig]
        Int32 CopyDataBlock(uint dwSig, out IntPtr ppDataBlock);

        [PreserveSig]
        Int32 RemoveDataBlock(uint dwSig);

        void GetFlags(out uint pdwFlags);
        void SetFlags(uint dwFlags);
    }

    /// <SecurityNote>
    /// Critical: Suppresses unmanaged code security.
    /// </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything), SuppressUnmanagedCodeSecurity]
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.FileDialogEvents),
    ]
    internal interface IFileDialogEvents
    {
        [PreserveSig]
        HRESULT OnFileOk(IFileDialog pfd);

        [PreserveSig]
        HRESULT OnFolderChanging(IFileDialog pfd, IShellItem psiFolder);

        [PreserveSig]
        HRESULT OnFolderChange(IFileDialog pfd);

        [PreserveSig]
        HRESULT OnSelectionChange(IFileDialog pfd);

        [PreserveSig]
        HRESULT OnShareViolation(IFileDialog pfd, IShellItem psi, out FDESVR pResponse);

        [PreserveSig]
        HRESULT OnTypeChange(IFileDialog pfd);

        [PreserveSig]
        HRESULT OnOverwrite(IFileDialog pfd, IShellItem psi, out FDEOR pResponse);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ModalWindow),
    ]
    internal interface IModalWindow
    {
        [PreserveSig]
        HRESULT Show(IntPtr parent);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.FileDialog),
    ]
    internal interface IFileDialog : IModalWindow
    {
        #region IModalWindow redeclarations
        [PreserveSig]
        new HRESULT Show(IntPtr parent);
        #endregion

        void SetFileTypes(uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] rgFilterSpec);

        void SetFileTypeIndex(uint iFileType);

        uint GetFileTypeIndex();

        uint Advise(IFileDialogEvents pfde);

        void Unadvise(uint dwCookie);

        void SetOptions(FOS fos);

        FOS GetOptions();

        void SetDefaultFolder(IShellItem psi);

        void SetFolder(IShellItem psi);

        IShellItem GetFolder();

        IShellItem GetCurrentSelection();

        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetFileName();

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        IShellItem GetResult();

        void AddPlace(IShellItem psi, FDAP alignment);

        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        void Close([MarshalAs(UnmanagedType.Error)] int hr);

        void SetClientGuid([In] ref Guid guid);

        void ClearClientData();

        void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.FileOpenDialog),
    ]
    internal interface IFileOpenDialog : IFileDialog
    {
        #region IFileDialog redeclarations

        #region IModalDialog redeclarations
        [PreserveSig]
        new HRESULT Show(IntPtr parent);
        #endregion

        new void SetFileTypes(uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);
        new void SetFileTypeIndex(uint iFileType);
        new uint GetFileTypeIndex();
        new uint Advise(IFileDialogEvents pfde);
        new void Unadvise(uint dwCookie);
        new void SetOptions(FOS fos);
        new FOS GetOptions();
        new void SetDefaultFolder(IShellItem psi);
        new void SetFolder(IShellItem psi);
        new IShellItem GetFolder();
        new IShellItem GetCurrentSelection();
        new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        new void GetFileName();
        new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        new IShellItem GetResult();
        new void AddPlace(IShellItem psi, FDAP fdcp);
        new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        new void Close([MarshalAs(UnmanagedType.Error)] int hr);
        new void SetClientGuid([In] ref Guid guid);
        new void ClearClientData();
        new void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter);

        #endregion

        IShellItemArray GetResults();

        IShellItemArray GetSelectedItems();
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.FileSaveDialog),
    ]
    internal interface IFileSaveDialog : IFileDialog
    {
        #region IFileDialog redeclarations

        #region IModalDialog redeclarations
        [PreserveSig]
        new HRESULT Show(IntPtr parent);
        #endregion

        new void SetFileTypes(uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);
        new void SetFileTypeIndex(uint iFileType);
        new uint GetFileTypeIndex();
        new uint Advise(IFileDialogEvents pfde);
        new void Unadvise(uint dwCookie);
        new void SetOptions(FOS fos);
        new FOS GetOptions();
        new void SetDefaultFolder(IShellItem psi);
        new void SetFolder(IShellItem psi);
        new IShellItem GetFolder();
        new IShellItem GetCurrentSelection();
        new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        new void GetFileName();
        new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        new IShellItem GetResult();
        new void AddPlace(IShellItem psi, FDAP fdcp);
        new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        new void Close([MarshalAs(UnmanagedType.Error)] int hr);
        new void SetClientGuid([In] ref Guid guid);
        new void ClearClientData();
        new void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter);

        #endregion

        void SetSaveAsItem(IShellItem psi);

        void SetProperties([In, MarshalAs(UnmanagedType.Interface)] object pStore);

        void SetCollectedProperties([In, MarshalAs(UnmanagedType.Interface)] object pList, [In] int fAppendDefault);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetProperties();

        void ApplyProperties(IShellItem psi, [MarshalAs(UnmanagedType.Interface)] object pStore, [In] ref IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] object pSink);
    }

    // Used to remove items from the automatic destination lists created when apps or the system call SHAddToRecentDocs to report usage of a document.
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ApplicationDestinations)
    ]
    internal interface IApplicationDestinations
    {
        // Set the App User Model ID for the application removing destinations from its list.  If an AppID is not provided 
        // via this method, the system will use a heuristically determined ID.  This method must be called before
        // RemoveDestination or RemoveAllDestinations.
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        // Remove an IShellItem or an IShellLink from the automatic destination list
        void RemoveDestination([MarshalAs(UnmanagedType.IUnknown)] object punk);

        // Clear the frequent and recent destination lists for this application.
        void RemoveAllDestinations();
    }

    /// <summary>
    /// Allows an application to retrieve the most recent and frequent documents opened in that app, as reported via SHAddToRecentDocs
    /// </summary>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ApplicationDocumentLists)
    ]
    internal interface IApplicationDocumentLists
    {
        /// <summary>
        /// Set the App User Model ID for the application retrieving this list.  If an AppID is not provided via this method,
        /// the system will use a heuristically determined ID.  This method must be called before GetList. 
        /// </summary>
        /// <param name="pszAppID">App Id.</param>
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        /// <summary>
        /// Retrieve an IEnumObjects or IObjectArray for IShellItems and/or IShellLinks. 
        /// Items may appear in both the frequent and recent lists.  
        /// </summary>
        /// <param name="listtype">Which of the known list types to retrieve</param>
        /// <param name="cItemsDesired">The number of items desired.</param>
        /// <param name="riid">The interface Id that the return value should be queried for.</param>
        /// <returns>A COM object based on the IID passed for the riid parameter.</returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetList(ADLT listtype, uint cItemsDesired, [In] ref Guid riid);
    }

    // Custom Destination List
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.CustomDestinationList)
    ]
    internal interface ICustomDestinationList
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

        // Retrieve IObjectArray of IShellItems or IShellLinks that represent removed destinations
        [return: MarshalAs(UnmanagedType.Interface)]
        object BeginList(out uint pcMaxSlots, [In] ref Guid riid);

        // PreserveSig because this will return custom errors when attempting to add unregistered ShellItems.
        // Can't readily detect that case without just trying to append it.
        [PreserveSig]
        HRESULT AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string pszCategory, IObjectArray poa);
        void AppendKnownCategory(KDC category);
        [PreserveSig]
        HRESULT AddUserTasks(IObjectArray poa);
        void CommitList();

        // Retrieve IObjectCollection of IShellItems
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetRemovedDestinations([In] ref Guid riid);
        void DeleteList([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
        void AbortList();
    }

    /// <summary>
    /// Provides access to the App User Model ID on objects supporting this value.
    /// </summary>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ObjectWithAppUserModelId)
    ]
    internal interface IObjectWithAppUserModelId
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetAppID();
    };

    /// <summary>
    /// Provides access to the ProgID associated with an object 
    /// </summary>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.ObjectWithProgId)
    ]
    internal interface IObjectWithProgId
    {
        void SetProgID([MarshalAs(UnmanagedType.LPWStr)] string pszProgID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetProgID();
    };

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.TaskbarList),
    ]
    internal interface ITaskbarList
    {
        /// <summary>
        /// This function must be called first to validate use of other members.
        /// </summary>
        void HrInit();

        /// <summary>
        /// This function adds a tab for hwnd to the taskbar.
        /// </summary>
        /// <param name="hwnd">The HWND for which to add the tab.</param>
        void AddTab(IntPtr hwnd);

        /// <summary>
        /// This function deletes a tab for hwnd from the taskbar.
        /// </summary>
        /// <param name="hwnd">The HWND for which the tab is to be deleted.</param>
        void DeleteTab(IntPtr hwnd);

        /// <summary>
        /// This function activates the tab associated with hwnd on the taskbar.
        /// </summary>
        /// <param name="hwnd">The HWND for which the tab is to be activated.</param>
        void ActivateTab(IntPtr hwnd);

        /// <summary>
        /// This function marks hwnd in the taskbar as the active tab.
        /// </summary>
        /// <param name="hwnd">The HWND to activate.</param>
        void SetActiveAlt(IntPtr hwnd);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.TaskbarList2),
    ]
    internal interface ITaskbarList2 : ITaskbarList
    {
        #region ITaskbarList redeclaration
        new void HrInit();
        new void AddTab(IntPtr hwnd);
        new void DeleteTab(IntPtr hwnd);
        new void ActivateTab(IntPtr hwnd);
        new void SetActiveAlt(IntPtr hwnd);
        #endregion

        /// <summary>
        /// Marks a window as full-screen.
        /// </summary>
        /// <param name="hwnd">The handle of the window to be marked.</param>
        /// <param name="fFullscreen">A Boolean value marking the desired full-screen status of the window.</param>
        /// <remarks>
        /// Setting the value of fFullscreen to true, the Shell treats this window as a full-screen window, and the taskbar
        /// is moved to the bottom of the z-order when this window is active.  Setting the value of fFullscreen to false
        /// removes the full-screen marking, but <i>does not</i> cause the Shell to treat the window as though it were
        /// definitely not full-screen.  With a false fFullscreen value, the Shell depends on its automatic detection facility
        /// to specify how the window should be treated, possibly still flagging the window as full-screen.
        /// </remarks>
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
    }

    /// <remarks>
    /// Methods on this interface are marked as PreserveSig because the implementation inconsistently
    /// surfaces errors in Explorer.  Many of these methods are implemented by posting messages
    /// to the desktop window, but if explorer is not running or currently busy then we get back
    /// error codes that must be handled by the caller.
    /// </remarks>
    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.TaskbarList3),
    ]
    internal interface ITaskbarList3 : ITaskbarList2
    {
        #region ITaskbarList2 redeclaration

        #region ITaskbarList redeclaration
        new void HrInit();
        new void AddTab(IntPtr hwnd);
        new void DeleteTab(IntPtr hwnd);
        new void ActivateTab(IntPtr hwnd);
        new void SetActiveAlt(IntPtr hwnd);
        #endregion

        new void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        #endregion

        [PreserveSig]
        HRESULT SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);

        [PreserveSig]
        HRESULT SetProgressState(IntPtr hwnd, TBPF tbpFlags);

        [PreserveSig]
        HRESULT RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);

        [PreserveSig]
        HRESULT UnregisterTab(IntPtr hwndTab);

        [PreserveSig]
        HRESULT SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);

        [PreserveSig]
        HRESULT SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);

        [PreserveSig]
        HRESULT ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] THUMBBUTTON[] pButtons);

        [PreserveSig]
        HRESULT ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] THUMBBUTTON[] pButtons);

        [PreserveSig]
        HRESULT ThumbBarSetImageList(IntPtr hwnd, [MarshalAs(UnmanagedType.IUnknown)] object himl);

        [PreserveSig]
        HRESULT SetOverlayIcon(IntPtr hwnd, NativeMethods.IconHandle hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);

        [PreserveSig]
        HRESULT SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);

        // Using RefRECT to making passing NULL possible.  Removes clipping from the HWND.
        [PreserveSig]
        HRESULT SetThumbnailClip(IntPtr hwnd, NativeMethods.RefRECT prcClip);
    }

    [
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid(IID.TaskbarList3),
    ]
    internal interface ITaskbarList4 : ITaskbarList3
    {
        #region ITaskbarList3 redeclaration

        #region ITaskbarList2 redeclaration

        #region ITaskbarList redeclaration
        new void HrInit();
        new void AddTab(IntPtr hwnd);
        new void DeleteTab(IntPtr hwnd);
        new void ActivateTab(IntPtr hwnd);
        new void SetActiveAlt(IntPtr hwnd);
        #endregion

        new void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        #endregion

        [PreserveSig] new HRESULT SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        [PreserveSig] new HRESULT SetProgressState(IntPtr hwnd, TBPF tbpFlags);
        [PreserveSig] new HRESULT RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        [PreserveSig] new HRESULT UnregisterTab(IntPtr hwndTab);
        [PreserveSig] new HRESULT SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        [PreserveSig] new HRESULT SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        [PreserveSig] new HRESULT ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] THUMBBUTTON[] pButtons);
        [PreserveSig] new HRESULT ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] THUMBBUTTON[] pButtons);
        [PreserveSig] new HRESULT ThumbBarSetImageList(IntPtr hwnd, [MarshalAs(UnmanagedType.IUnknown)] object himl);
        [PreserveSig] new HRESULT SetOverlayIcon(IntPtr hwnd, NativeMethods.IconHandle hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
        [PreserveSig] new HRESULT SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
        [PreserveSig] new HRESULT SetThumbnailClip(IntPtr hwnd, NativeMethods.RefRECT prcClip);

        #endregion

        [PreserveSig]
        HRESULT SetTabProperties(IntPtr hwndTab, STPF stpFlags);
    }

    #endregion

    /// <remarks>
    /// Methods in this class will only work on Vista and above.
    /// </remarks>
    internal static class ShellUtil
    {
        public static string GetPathFromShellItem(IShellItem item)
        {
            return item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING);
        }

        public static string GetPathForKnownFolder(Guid knownFolder)
        {
            if (knownFolder == default(Guid))
            {
                return null;
            }

            var pathBuilder = new StringBuilder(NativeMethods.MAX_PATH);
            HRESULT hr = NativeMethods2.SHGetFolderPathEx(ref knownFolder, 0, IntPtr.Zero, pathBuilder, (uint)pathBuilder.Capacity);
            // If we failed to find a path for the known folder then just ignore it.
            return hr.Succeeded
                ? pathBuilder.ToString()
                : null;
        }

        public static IShellItem2 GetShellItemForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // Internal function.  Should have verified this before calling if we cared.
                return null;
            }

            Guid iidShellItem2 = new Guid(IID.ShellItem2);
            object unk;
            HRESULT hr = NativeMethods2.SHCreateItemFromParsingName(path, null, ref iidShellItem2, out unk);

            // Silently absorb errors such as ERROR_FILE_NOT_FOUND, ERROR_PATH_NOT_FOUND.
            // Let others pass through
            if (hr == (HRESULT)Win32Error.ERROR_FILE_NOT_FOUND || hr == (HRESULT)Win32Error.ERROR_PATH_NOT_FOUND)
            {
                hr = HRESULT.S_OK;
                unk = null;
            }

            hr.ThrowIfFailed();

            return (IShellItem2)unk;
        }
    }

    internal static class NativeMethods2
    {
        [DllImport(ExternDll.Shell32, EntryPoint = "SHAddToRecentDocs")]
        private static extern void SHAddToRecentDocsString(SHARD uFlags, [MarshalAs(UnmanagedType.LPWStr)] string pv);

        // This overload is required.  There's a cast in the Shell code that causes the wrong vtbl to be used
        // if we let the marshaller convert the parameter to an IUnknown.
        [DllImport(ExternDll.Shell32, EntryPoint = "SHAddToRecentDocs")]
        private static extern void SHAddToRecentDocs_ShellLink(SHARD uFlags, IShellLinkW pv);

        internal static void SHAddToRecentDocs(string path)
        {
            SHAddToRecentDocsString(SHARD.PATHW, path);
        }

        // Win7 only.
        internal static void SHAddToRecentDocs(IShellLinkW shellLink)
        {
            SHAddToRecentDocs_ShellLink(SHARD.LINK, shellLink);
        }

        // Vista only
        [DllImport(ExternDll.Shell32)]
        internal static extern HRESULT SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        // Vista only.  Also inconsistently doced on MSDN.  It was available in some versions of the SDK, and it mentioned on several pages, but isn't specifically doced.
        [DllImport(ExternDll.Shell32)]
        internal static extern HRESULT SHGetFolderPathEx([In] ref Guid rfid, KF_FLAG dwFlags, [In, Optional] IntPtr hToken, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath, uint cchPath);

        /// <summary>
        /// Sets the User Model AppID for the current process, enabling Windows to retrieve this ID
        /// </summary>
        /// <param name="AppID"></param>
        [DllImport(ExternDll.Shell32, PreserveSig = false)]
        internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        /// <summary>
        /// Retrieves the User Model AppID that has been explicitly set for the current process via SetCurrentProcessExplicitAppUserModelID
        /// </summary>
        /// <param name="AppID"></param>
        [DllImport(ExternDll.Shell32)]
        internal static extern HRESULT GetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] out string AppID);
    }
}
