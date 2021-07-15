// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//
// A consolidated file of native values and enums.
//
// The naming convention for enums is to use the native prefix of common types
// as the enum name, where this makes sense to do.
// One exception is WM_, which is called WindowMessage here.
//

namespace MS.Internal.Interop
{
    using System;

    /// <summary>
    /// APPDOCLISTTYPE.  ADLT_*
    /// </summary>
    internal enum ADLT
    {
        RECENT = 0,   // The recently used documents list
        FREQUENT,     // The frequently used documents list
    }

    /// <summary>
    /// DATAOBJ_GET_ITEM_FLAGS.  DOGIF_*.
    /// </summary>
    [Flags]
    internal enum DOGIF
    {
        DEFAULT = 0x0000,
        TRAVERSE_LINK = 0x0001,    // if the item is a link get the target
        NO_HDROP = 0x0002,    // don't fallback and use CF_HDROP clipboard format
        NO_URL = 0x0004,    // don't fallback and use URL clipboard format
        ONLY_IF_ONE = 0x0008,    // only return the item if there is one item in the array
    }
    
    /// <summary>FileDialog AddPlace options.  FDAP_*</summary>
    internal enum FDAP : uint
    {
        BOTTOM = 0x00000000,
        TOP    = 0x00000001,
    }

    /// <summary>IFileDialog options.  FOS_*</summary>
    [Flags]
    internal enum FOS : uint
    {
        OVERWRITEPROMPT    = 0x00000002,
        STRICTFILETYPES    = 0x00000004,
        NOCHANGEDIR        = 0x00000008,
        PICKFOLDERS        = 0x00000020,
        FORCEFILESYSTEM    = 0x00000040,
        ALLNONSTORAGEITEMS = 0x00000080,
        NOVALIDATE         = 0x00000100,
        ALLOWMULTISELECT   = 0x00000200,
        PATHMUSTEXIST      = 0x00000800,
        FILEMUSTEXIST      = 0x00001000,
        CREATEPROMPT       = 0x00002000,
        SHAREAWARE         = 0x00004000,
        NOREADONLYRETURN   = 0x00008000,
        NOTESTFILECREATE   = 0x00010000,
        HIDEMRUPLACES      = 0x00020000,
        HIDEPINNEDPLACES   = 0x00040000,
        NODEREFERENCELINKS = 0x00100000,
        DONTADDTORECENT    = 0x02000000,
        FORCESHOWHIDDEN    = 0x10000000,
        DEFAULTNOMINIMODE  = 0x20000000,
        FORCEPREVIEWPANEON = 0x40000000,
    }

    /// <summary>FDE_OVERWRITE_RESPONSE.  FDEOR_*</summary>
    internal enum FDEOR 
    {
        DEFAULT = 0x00000000,
        ACCEPT  = 0x00000001,
        REFUSE  = 0x00000002,
    }

    /// <summary>FDE_SHAREVIOLATION_RESPONSE.  FDESVR_*</summary>
    internal enum FDESVR
    {
        DEFAULT = 0x00000000,
        ACCEPT  = 0x00000001,
        REFUSE  = 0x00000002,
    }

    /// <summary>
    /// GetPropertyStoreFlags.  GPS_*.
    /// </summary>
    /// <remarks>
    /// These are new for Vista, but are used in downlevel components
    /// </remarks>
    [Flags]
    internal enum GPS
    {
        // If no flags are specified (GPS_DEFAULT), a read-only property store is returned that includes properties for the file or item.
        // In the case that the shell item is a file, the property store contains:
        //     1. properties about the file from the file system
        //     2. properties from the file itself provided by the file's property handler, unless that file is offline,
        //         see GPS_OPENSLOWITEM
        //     3. if requested by the file's property handler and supported by the file system, properties stored in the
        //     alternate property store.
        //
        // Non-file shell items should return a similar read-only store
        //
        // Specifying other GPS_ flags modifies the store that is returned
        DEFAULT = 0x00000000,
        HANDLERPROPERTIESONLY = 0x00000001,   // only include properties directly from the file's property handler
        READWRITE = 0x00000002,   // Writable stores will only include handler properties
        TEMPORARY = 0x00000004,   // A read/write store that only holds properties for the lifetime of the IShellItem object
        FASTPROPERTIESONLY = 0x00000008,   // do not include any properties from the file's property handler (because the file's property handler will hit the disk)
        OPENSLOWITEM = 0x00000010,   // include properties from a file's property handler, even if it means retrieving the file from offline storage.
        DELAYCREATION = 0x00000020,   // delay the creation of the file's property handler until those properties are read, written, or enumerated
        BESTEFFORT = 0x00000040,   // For readonly stores, succeed and return all available properties, even if one or more sources of properties fails. Not valid with GPS_READWRITE.
        NO_OPLOCK = 0x00000080,   // some data sources protect the read property store with an oplock, this disables that
        MASK_VALID = 0x000000FF,
    }

    /// <summary>Flags for Known Folder APIs.  KF_FLAG_*</summary>
    /// <remarks>native enum was called KNOWN_FOLDER_FLAG</remarks>
    [Flags]
    internal enum KF_FLAG : uint
    {
        DEFAULT         = 0x00000000,
        
        // Make sure that the folder already exists or create it and apply security specified in folder definition
        // If folder can not be created then function will return failure and no folder path (IDList) will be returned
        // If folder is located on the network the function may take long time to execute
        CREATE          = 0x00008000,
        
        // If this flag is specified then the folder path is returned and no verification is performed
        // Use this flag is you want to get folder's path (IDList) and do not need to verify folder's existence
        //
        // If this flag is NOT specified then Known Folder API will try to verify that the folder exists
        //     If folder does not exist or can not be accessed then function will return failure and no folder path (IDList) will be returned
        //     If folder is located on the network the function may take long time to execute
        DONT_VERIFY     = 0x00004000,

        // Set folder path as is and do not try to substitute parts of the path with environments variables.
        // If flag is not specified then Known Folder will try to replace parts of the path with some
        // known environment variables (%USERPROFILE%, %APPDATA% etc.)
        DONT_UNEXPAND   = 0x00002000,

        // Get file system based IDList if available. If the flag is not specified the Known Folder API
        // will try to return aliased IDList by default. Example for FOLDERID_Documents -
        // Aliased - [desktop]\[user]\[Documents] - exact location is determined by shell namespace layout and might change
        // Non aliased - [desktop]\[computer]\[disk_c]\[users]\[user]\[Documents] - location is determined by folder location in the file system
        NO_ALIAS        = 0x00001000,

        // Initialize the folder with desktop.ini settings
        // If folder can not be initialized then function will return failure and no folder path will be returned
        // If folder is located on the network the function may take long time to execute
        INIT            = 0x00000800,

        // Get the default path, will also verify folder existence unless KF_FLAG_DONT_VERIFY is also specified
        DEFAULT_PATH    = 0x00000400,
        
        // Get the not-parent-relative default path. Only valid with KF_FLAG_DEFAULT_PATH
        NOT_PARENT_RELATIVE = 0x00000200,

        // Build simple IDList
        SIMPLE_IDLIST   = 0x00000100,
        
        // only return the aliased IDLists, don't fallback to file system path
        ALIAS_ONLY      = 0x80000000,
    }

    /// <summary>
    /// KNOWNDESTCATEGORY.  KDC_*
    /// </summary>
    internal enum KDC
    {
        FREQUENT = 1,
        RECENT,
    }

    /// <summary>
    /// MSGFLT_*, for ChangeWindowMessageFilter[Ex].
    /// </summary>
    /// <remarks>New in Vista.  Realiased in Windows 7.</remarks>
    internal enum MSGFLT
    {
        RESET = 0,
        // MSGFLT_ADD in Vista.
        ALLOW = 1,
        // MSGFLT_REMOVE in Vista.
        DISALLOW = 2,
    }

    internal enum MSGFLTINFO
    {
        NONE = 0,
        ALREADYALLOWED_FORWND = 1,
        ALREADYDISALLOWED_FORWND = 2,
        ALLOWED_HIGHER = 3,
    }
    
    // IShellFolder::GetAttributesOf flags
    [Flags]
    internal enum SFGAO : uint
    {
        /// <summary>Objects can be copied</summary>
        /// <remarks>DROPEFFECT_COPY</remarks>
        CANCOPY = 0x1,
        /// <summary>Objects can be moved</summary>
        /// <remarks>DROPEFFECT_MOVE</remarks>
        CANMOVE = 0x2,
        /// <summary>Objects can be linked</summary>
        /// <remarks>
        /// DROPEFFECT_LINK.
        /// 
        /// If this bit is set on an item in the shell folder, a
        /// 'Create Shortcut' menu item will be added to the File
        /// menu and context menus for the item.  If the user selects
        /// that command, your IContextMenu::InvokeCommand() will be called
        /// with 'link'.
        /// That flag will also be used to determine if 'Create Shortcut'
        /// should be added when the item in your folder is dragged to another
        /// folder.
        /// </remarks>
        CANLINK = 0x4,
        /// <summary>supports BindToObject(IID_IStorage)</summary>
        STORAGE = 0x00000008,
        /// <summary>Objects can be renamed</summary>
        CANRENAME = 0x00000010,
        /// <summary>Objects can be deleted</summary>
        CANDELETE = 0x00000020,
        /// <summary>Objects have property sheets</summary>
        HASPROPSHEET = 0x00000040,

        // unused = 0x00000080,

        /// <summary>Objects are drop target</summary>
        DROPTARGET = 0x00000100,
        CAPABILITYMASK = 0x00000177,
        // unused = 0x00000200,
        // unused = 0x00000400,
        // unused = 0x00000800,
        // unused = 0x00001000,
        /// <summary>Object is encrypted (use alt color)</summary>
        ENCRYPTED = 0x00002000,
        /// <summary>'Slow' object</summary>
        ISSLOW = 0x00004000,
        /// <summary>Ghosted icon</summary>
        GHOSTED = 0x00008000,
        /// <summary>Shortcut (link)</summary>
        LINK = 0x00010000,
        /// <summary>Shared</summary>
        SHARE = 0x00020000,
        /// <summary>Read-only</summary>
        READONLY = 0x00040000,
        /// <summary> Hidden object</summary>
        HIDDEN = 0x00080000,
        DISPLAYATTRMASK = 0x000FC000,
        /// <summary> May contain children with SFGAO_FILESYSTEM</summary>
        FILESYSANCESTOR = 0x10000000,
        /// <summary>Support BindToObject(IID_IShellFolder)</summary>
        FOLDER = 0x20000000,
        /// <summary>Is a win32 file system object (file/folder/root)</summary>
        FILESYSTEM = 0x40000000,
        /// <summary>May contain children with SFGAO_FOLDER (may be slow)</summary>
        HASSUBFOLDER = 0x80000000,
        CONTENTSMASK = 0x80000000,
        /// <summary>Invalidate cached information (may be slow)</summary>
        VALIDATE = 0x01000000,
        /// <summary>Is this removeable media?</summary>
        REMOVABLE = 0x02000000,
        /// <summary> Object is compressed (use alt color)</summary>
        COMPRESSED = 0x04000000,
        /// <summary>Supports IShellFolder, but only implements CreateViewObject() (non-folder view)</summary>
        BROWSABLE = 0x08000000,
        /// <summary>Is a non-enumerated object (should be hidden)</summary>
        NONENUMERATED = 0x00100000,
        /// <summary>Should show bold in explorer tree</summary>
        NEWCONTENT = 0x00200000,
        /// <summary>Obsolete</summary>
        CANMONIKER = 0x00400000,
        /// <summary>Obsolete</summary>
        HASSTORAGE = 0x00400000,
        /// <summary>Supports BindToObject(IID_IStream)</summary>
        STREAM = 0x00400000,
        /// <summary>May contain children with SFGAO_STORAGE or SFGAO_STREAM</summary>
        STORAGEANCESTOR = 0x00800000,
        /// <summary>For determining storage capabilities, ie for open/save semantics</summary>
        STORAGECAPMASK = 0x70C50008,
        /// <summary>
        /// Attributes that are masked out for PKEY_SFGAOFlags because they are considered
        /// to cause slow calculations or lack context
        /// (SFGAO_VALIDATE | SFGAO_ISSLOW | SFGAO_HASSUBFOLDER and others)
        /// </summary>
        PKEYSFGAOMASK = 0x81044000,
    }

    /// <summary>
    /// SHAddToRecentDocuments flags.  SHARD_*
    /// </summary>
    internal enum SHARD
    {
        /// <summary>
        /// The pv parameter points to a pointer to an item identifier list (PIDL) that identifies the document's file object.
        /// PIDLs that identify non-file objects are not accepted.
        /// </summary>
        PIDL = 0x00000001,

        /// <summary>
        /// The pv parameter points to a null-terminated ANSI string with the path and file name of the object.
        /// </summary>
        PATHA = 0x00000002,

        /// <summary>
        /// The pv parameter points to a null-terminated Unicode string with the path and file name of the object.
        /// </summary>
        PATHW = 0x00000003,

        /// <summary>
        ///  The pv parameter points to a SHARDAPPIDINFO structure that pairs an IShellItem that identifies the item
        ///  with an Application User Model ID (AppID) that associates it with a particular process or application.
        /// </summary>
        /// <remarks>
        /// Windows 7 and later.
        /// </remarks>
        APPIDINFO = 0x00000004,

        /// <summary>
        /// The pv parameter points to a SHARDAPPIDINFOIDLIST structure that pairs an absolute PIDL that identifies the item
        /// with an AppID that associates it with a particular process or application.
        /// </summary>
        /// <remarks>
        /// Windows 7 and later.
        /// </remarks>
        APPIDINFOIDLIST = 0x00000005,
        /// <summary>
        /// The pv parameter is an interface pointer to an IShellLink object.
        /// </summary>
        /// <remarks>
        /// Windows 7 and later.
        /// </remarks>
        LINK = 0x00000006, // indicates the data type is a pointer to an IShellLink instance

        /// <summary>
        /// The pv parameter points to a SHARDAPPIDINFOLINK structure that pairs an IShellLink that identifies the item
        /// with an AppID that associates it with a particular process or application.
        /// </summary>
        /// <remarks>
        /// Windows 7 and later.
        /// </remarks>
        APPIDINFOLINK = 0x00000007,

        /// <summary>
        /// The pv parameter is an interface pointer to an IShellItem object.
        /// </summary>
        /// <remarks>
        /// Windows 7 and later.
        /// </remarks>
        SHELLITEM = 0x00000008,
    }

    /// <summary>
    /// IShellFolder::EnumObjects grfFlags bits.  Also called SHCONT
    /// </summary>
    [Flags]
    internal enum SHCONTF
    {
        CHECKING_FOR_CHILDREN = 0x0010,   // hint that client is checking if (what) child items the folder contains - not all details (e.g. short file name) are needed
        FOLDERS = 0x0020,   // only want folders enumerated (SFGAO_FOLDER)
        NONFOLDERS = 0x0040,   // include non folders (items without SFGAO_FOLDER)
        INCLUDEHIDDEN = 0x0080,   // show items normally hidden (items with SFGAO_HIDDEN)
        INIT_ON_FIRST_NEXT = 0x0100,   // DEFUNCT - this is always assumed
        NETPRINTERSRCH = 0x0200,   // hint that client is looking for printers
        SHAREABLE = 0x0400,   // hint that client is looking sharable resources (local drives or hidden root shares)
        STORAGE = 0x0800,   // include all items with accessible storage and their ancestors
        NAVIGATION_ENUM = 0x1000,   // mark child folders to indicate that they should provide a "navigation" enumeration by default
        FASTITEMS = 0x2000,   // hint that client is only interested in items that can be enumerated quickly
        FLATLIST = 0x4000,   // enumerate items as flat list even if folder is stacked
        ENABLE_ASYNC = 0x8000,   // inform enumerator that client is listening for change notifications so enumerator does not need to be complete, items can be reported via change notifications
    }

    /// <summary>
    /// IShellFolder::GetDisplayNameOf/SetNameOf uFlags.  Also called SHGDNF.
    /// </summary>
    /// <remarks>
    /// For compatibility with SIGDN, these bits must all sit in the LOW word.
    /// </remarks>
    [Flags]
    internal enum SHGDN
    {
        SHGDN_NORMAL = 0x0000,  // default (display purpose)
        SHGDN_INFOLDER = 0x0001,  // displayed under a folder (relative)
        SHGDN_FOREDITING = 0x1000,  // for in-place editing
        SHGDN_FORADDRESSBAR = 0x4000,  // UI friendly parsing name (remove ugly stuff)
        SHGDN_FORPARSING = 0x8000,  // parsing name for ParseDisplayName()
    }
    
    /// <summary>ShellItem attribute flags.  SIATTRIBFLAGS_*</summary>
    internal enum SIATTRIBFLAGS
    {
        AND       = 0x00000001,
        OR        = 0x00000002,
        APPCOMPAT = 0x00000003,
    }

    /// <summary>
    /// SHELLITEMCOMPAREHINTF.  SICHINT_*.
    /// </summary>
    [Flags]
    internal enum SICHINT : uint
    {
        /// <summary>iOrder based on display in a folder view</summary>
        DISPLAY = 0x00000000,
        /// <summary>exact instance compare</summary>
        ALLFIELDS = 0x80000000,
        /// <summary>iOrder based on canonical name (better performance)</summary>
        CANONICAL = 0x10000000,
        /// <summary>Windows 7 and later.</summary>
        TEST_FILESYSPATH_IF_NOT_EQUAL = 0x20000000,
    };
    
    /// <summary>ShellItem GetDisplayName options.  SIGDN_*</summary>
    internal enum SIGDN : uint
    {                                         // lower word (& with 0xFFFF)
        NORMALDISPLAY = 0x00000000,           // SHGDN_NORMAL
        PARENTRELATIVEPARSING = 0x80018001,   // SHGDN_INFOLDER | SHGDN_FORPARSING
        DESKTOPABSOLUTEPARSING = 0x80028000,  // SHGDN_FORPARSING
        PARENTRELATIVEEDITING = 0x80031001,   // SHGDN_INFOLDER | SHGDN_FOREDITING
        DESKTOPABSOLUTEEDITING = 0x8004c000,  // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
        FILESYSPATH = 0x80058000,             // SHGDN_FORPARSING
        URL = 0x80068000,                     // SHGDN_FORPARSING
        PARENTRELATIVEFORADDRESSBAR = 0x8007c001,     // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
        PARENTRELATIVE = 0x80080001,           // SHGDN_INFOLDER
    }

    /// <summary>IShellLinkW::GetPath flags.  SLGP_*</summary>
    [Flags]
    internal enum SLGP
    {
        SHORTPATH = 0x1,
        UNCPRIORITY = 0x2,
        RAWPATH = 0x4
    }

    /// <summary>
    /// SystemMetrics.  SM_*
    /// </summary>
    internal enum SM
    {
        CXSCREEN = 0,
        CYSCREEN = 1,
        CXVSCROLL = 2,
        CYHSCROLL = 3,
        CYCAPTION = 4,
        CXBORDER = 5,
        CYBORDER = 6,
        CXFIXEDFRAME = 7,
        CYFIXEDFRAME = 8,
        CYVTHUMB = 9,
        CXHTHUMB = 10,
        CXICON = 11,
        CYICON = 12,
        CXCURSOR = 13,
        CYCURSOR = 14,
        CYMENU = 15,
        CXFULLSCREEN = 16,
        CYFULLSCREEN = 17,
        CYKANJIWINDOW = 18,
        MOUSEPRESENT = 19,
        CYVSCROLL = 20,
        CXHSCROLL = 21,
        DEBUG = 22,
        SWAPBUTTON = 23,
        CXMIN = 28,
        CYMIN = 29,
        CXSIZE = 30,
        CYSIZE = 31,
        CXFRAME = 32,
        CXSIZEFRAME = CXFRAME,
        CYFRAME = 33,
        CYSIZEFRAME = CYFRAME,
        CXMINTRACK = 34,
        CYMINTRACK = 35,
        CXDOUBLECLK = 36,
        CYDOUBLECLK = 37,
        CXICONSPACING = 38,
        CYICONSPACING = 39,
        MENUDROPALIGNMENT = 40,
        PENWINDOWS = 41,
        DBCSENABLED = 42,
        CMOUSEBUTTONS = 43,
        SECURE = 44,
        CXEDGE = 45,
        CYEDGE = 46,
        CXMINSPACING = 47,
        CYMINSPACING = 48,
        CXSMICON = 49,
        CYSMICON = 50,
        CYSMCAPTION = 51,
        CXSMSIZE = 52,
        CYSMSIZE = 53,
        CXMENUSIZE = 54,
        CYMENUSIZE = 55,
        ARRANGE = 56,
        CXMINIMIZED = 57,
        CYMINIMIZED = 58,
        CXMAXTRACK = 59,
        CYMAXTRACK = 60,
        CXMAXIMIZED = 61,
        CYMAXIMIZED = 62,
        NETWORK = 63,
        CLEANBOOT = 67,
        CXDRAG = 68,
        CYDRAG = 69,
        SHOWSOUNDS = 70,
        CXMENUCHECK = 71,
        CYMENUCHECK = 72,
        SLOWMACHINE = 73,
        MIDEASTENABLED = 74,
        MOUSEWHEELPRESENT = 75,
        XVIRTUALSCREEN = 76,
        YVIRTUALSCREEN = 77,
        CXVIRTUALSCREEN = 78,
        CYVIRTUALSCREEN = 79,
        CMONITORS = 80,
        SAMEDISPLAYFORMAT = 81,
        IMMENABLED = 82,
        CXFOCUSBORDER =   83,
        CYFOCUSBORDER =   84,
        TABLETPC = 86,
        MEDIACENTER = 87,
        REMOTESESSION = 0x1000,
        REMOTECONTROL = 0x2001,
    }
    
    /// <summary>
    /// Flags for ITaskbarList3 SetTabProperties.  STPF_*
    /// </summary>
    /// <remarks>The native enum was called STPFLAG.</remarks>
    [Flags]
    internal enum STPF
    {
        NONE                       = 0x00000000,
        USEAPPTHUMBNAILALWAYS      = 0x00000001,
        USEAPPTHUMBNAILWHENACTIVE  = 0x00000002,
        USEAPPPEEKALWAYS           = 0x00000004,
        USEAPPPEEKWHENACTIVE       = 0x00000008,
    }

    /// <summary>
    /// STR_GPS_*
    /// </summary>
    /// <remarks>
    /// When requesting a property store through IShellFolder, you can specify the equivalent of
    /// GPS_DEFAULT by passing in a null IBindCtx parameter.
    ///
    /// You can specify the equivalent of GPS_READWRITE by passing a mode of STGM_READWRITE | STGM_EXCLUSIVE
    /// in the bind context
    ///
    /// Here are the string versions of GPS_ flags, passed to IShellFolder::BindToObject() via IBindCtx::RegisterObjectParam()
    /// These flags are valid when requesting an IPropertySetStorage or IPropertyStore handler
    ///
    /// The meaning of these flags are described above.
    ///
    /// There is no STR_ equivalent for GPS_TEMPORARY because temporary property stores
    /// are provided by IShellItem2 only -- not by the underlying IShellFolder.
    /// </remarks>
    internal static class STR_GPS
    {
        public const string HANDLERPROPERTIESONLY = "GPS_HANDLERPROPERTIESONLY";
        public const string FASTPROPERTIESONLY = "GPS_FASTPROPERTIESONLY";
        public const string OPENSLOWITEM = "GPS_OPENSLOWITEM";
        public const string DELAYCREATION = "GPS_DELAYCREATION";
        public const string BESTEFFORT = "GPS_BESTEFFORT";
        public const string NO_OPLOCK = "GPS_NO_OPLOCK";
    }
    
    /// <summary>
    /// Flags for Setting Taskbar Progress state.  TBPF_*
    /// </summary>
    /// <remarks>
    /// The native enum was called TBPFLAG.
    /// </remarks>
    [Flags]
    internal enum TBPF
    {
        NOPROGRESS = 0x00000000,
        INDETERMINATE = 0x00000001,
        NORMAL = 0x00000002,
        ERROR = 0x00000004,
        PAUSED = 0x00000008,
    }

    /// <summary>
    /// THUMBBUTTON mask.  THB_*
    /// </summary>
    [Flags]
    internal enum THB : uint
    {
        BITMAP = 0x0001,
        ICON = 0x0002,
        TOOLTIP = 0x0004,
        FLAGS = 0x0008,
    }

    /// <summary>
    /// THUMBBUTTON flags.  THBF_*
    /// </summary>
    [Flags]
    internal enum THBF : uint
    {
        ENABLED = 0x0000,
        DISABLED = 0x0001,
        DISMISSONCLICK = 0x0002,
        NOBACKGROUND = 0x0004,
        HIDDEN = 0x0008,
        // Added post-beta
        NONINTERACTIVE = 0x0010,
    }
    
    /// <summary>
    /// Window message values, WM_*
    /// </summary>
    internal enum WindowMessage
    {
        WM_NULL = 0x0000,
        WM_CREATE = 0x0001,
        WM_DESTROY = 0x0002,
        WM_MOVE = 0x0003,
        WM_SIZE = 0x0005,
        WM_ACTIVATE = 0x0006,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,
        WM_ENABLE = 0x000A,
        WM_SETREDRAW = 0x000B,
        WM_SETTEXT = 0x000C,
        WM_GETTEXT = 0x000D,
        WM_GETTEXTLENGTH = 0x000E,
        WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010,
        WM_QUERYENDSESSION = 0x0011,
        WM_QUIT = 0x0012,
        WM_QUERYOPEN = 0x0013,
        WM_ERASEBKGND = 0x0014,
        WM_SYSCOLORCHANGE = 0x0015,
        WM_ENDSESSION = 0x0016,
        WM_SHOWWINDOW = 0x0018,
        WM_CTLCOLOR = 0x0019,
        WM_WININICHANGE = 0x001A,
        WM_SETTINGCHANGE = 0x001A,
        WM_DEVMODECHANGE = 0x001B,
        WM_ACTIVATEAPP = 0x001C,
        WM_FONTCHANGE = 0x001D,
        WM_TIMECHANGE = 0x001E,
        WM_CANCELMODE = 0x001F,
        WM_SETCURSOR = 0x0020,
        WM_MOUSEACTIVATE =0x0021,
        WM_CHILDACTIVATE = 0x0022,
        WM_QUEUESYNC = 0x0023,
        WM_GETMINMAXINFO = 0x0024,
        WM_PAINTICON = 0x0026,
        WM_ICONERASEBKGND = 0x0027,
        WM_NEXTDLGCTL = 0x0028,
        WM_SPOOLERSTATUS = 0x002A,
        WM_DRAWITEM = 0x002B,
        WM_MEASUREITEM = 0x002C,
        WM_DELETEITEM = 0x002D,
        WM_VKEYTOITEM = 0x002E,
        WM_CHARTOITEM = 0x002F,
        WM_SETFONT = 0x0030,
        WM_GETFONT = 0x0031,
        WM_SETHOTKEY = 0x0032,
        WM_GETHOTKEY = 0x0033,
        WM_QUERYDRAGICON = 0x0037,
        WM_COMPAREITEM = 0x0039,
        WM_GETOBJECT = 0x003D,
        WM_COMPACTING = 0x0041,
        WM_COMMNOTIFY = 0x0044,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_POWER = 0x0048,
        WM_COPYDATA = 0x004A,
        WM_CANCELJOURNAL = 0x004B,
        WM_NOTIFY = 0x004E,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE = 0x0051,
        WM_TCARD = 0x0052,
        WM_HELP = 0x0053,
        WM_USERCHANGED = 0x0054,
        WM_NOTIFYFORMAT = 0x0055,

        WM_CONTEXTMENU = 0x007B,
        WM_STYLECHANGING = 0x007C,
        WM_STYLECHANGED = 0x007D,
        WM_DISPLAYCHANGE = 0x007E,
        WM_GETICON = 0x007F,
        WM_SETICON = 0x0080,
        WM_NCCREATE = 0x0081,
        WM_NCDESTROY = 0x0082,
        WM_NCCALCSIZE = 0x0083,
        WM_NCHITTEST = 0x0084,
        WM_NCPAINT = 0x0085,
        WM_NCACTIVATE = 0x0086,
        WM_GETDLGCODE = 0x0087,
        WM_SYNCPAINT = 0x0088,
        WM_MOUSEQUERY = 0x009B,
        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9,
        WM_NCXBUTTONDOWN = 0x00AB,
        WM_NCXBUTTONUP = 0x00AC,
        WM_NCXBUTTONDBLCLK = 0x00AD,
        WM_INPUT = 0x00FF,
        WM_KEYFIRST = 0x0100,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
        WM_DEADCHAR = 0x0103,

        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSCHAR = 0x0106,
        WM_SYSDEADCHAR = 0x0107,
        WM_KEYLAST = 0x0108,
        WM_IME_STARTCOMPOSITION = 0x010D,
        WM_IME_ENDCOMPOSITION = 0x010E,
        WM_IME_COMPOSITION = 0x010F,
        WM_IME_KEYLAST = 0x010F,
        WM_INITDIALOG = 0x0110,

        WM_COMMAND = 0x0111,
        WM_SYSCOMMAND = 0x0112,
        WM_TIMER = 0x0113,
        WM_HSCROLL = 0x0114,
        WM_VSCROLL = 0x0115,
        WM_INITMENU = 0x0116,
        WM_INITMENUPOPUP = 0x0117,
        WM_MENUSELECT = 0x011F,
        WM_MENUCHAR = 0x0120,
        WM_ENTERIDLE = 0x0121,
        WM_UNINITMENUPOPUP = 0x0125,
        WM_CHANGEUISTATE = 0x0127,
        WM_UPDATEUISTATE = 0x0128,
        WM_QUERYUISTATE = 0x0129,
        WM_CTLCOLORMSGBOX = 0x0132,
        WM_CTLCOLOREDIT = 0x0133,
        WM_CTLCOLORLISTBOX = 0x0134,
        WM_CTLCOLORBTN = 0x0135,
        WM_CTLCOLORDLG = 0x0136,
        WM_CTLCOLORSCROLLBAR = 0x0137,
        WM_CTLCOLORSTATIC = 0x0138,

        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEFIRST = WM_MOUSEMOVE,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_MOUSEWHEEL = 0x020A,
        WM_XBUTTONDOWN = 0x020B,
        WM_XBUTTONUP = 0x020C,
        WM_XBUTTONDBLCLK = 0x020D,
        WM_MOUSEHWHEEL = 0x020E,
        WM_MOUSELAST = WM_MOUSEHWHEEL,
        WM_PARENTNOTIFY = 0x0210,
        WM_ENTERMENULOOP = 0x0211,
        WM_EXITMENULOOP = 0x0212,
        WM_NEXTMENU = 0x0213,
        WM_SIZING = 0x0214,
        WM_CAPTURECHANGED = 0x0215,
        WM_MOVING = 0x0216,
        WM_POWERBROADCAST = 0x0218,
        WM_DEVICECHANGE = 0x0219,
        WM_POINTERDEVICECHANGE = 0X0238,
        WM_POINTERDEVICEINRANGE = 0x0239,
        WM_POINTERDEVICEOUTOFRANGE = 0x023A,
        WM_POINTERUPDATE = 0x0245,
        WM_POINTERDOWN = 0x0246,
        WM_POINTERUP = 0x0247,
        WM_POINTERENTER = 0x0249,
        WM_POINTERLEAVE = 0x024A,
        WM_POINTERACTIVATE = 0x024B,
        WM_POINTERCAPTURECHANGED = 0x024C,
        WM_IME_SETCONTEXT = 0x0281,
        WM_IME_NOTIFY = 0x0282,
        WM_IME_CONTROL = 0x0283,
        WM_IME_COMPOSITIONFULL = 0x0284,
        WM_IME_SELECT = 0x0285,
        WM_IME_CHAR  = 0x0286,
        WM_IME_REQUEST = 0x0288,
        WM_IME_KEYDOWN = 0x0290,
        WM_IME_KEYUP = 0x0291,
        WM_MDICREATE = 0x0220,
        WM_MDIDESTROY = 0x0221,
        WM_MDIACTIVATE = 0x0222,
        WM_MDIRESTORE = 0x0223,
        WM_MDINEXT = 0x0224,
        WM_MDIMAXIMIZE = 0x0225,
        WM_MDITILE = 0x0226,
        WM_MDICASCADE = 0x0227,
        WM_MDIICONARRANGE = 0x0228,
        WM_MDIGETACTIVE = 0x0229,
        WM_MDISETMENU = 0x0230,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_DROPFILES = 0x0233,
        WM_MDIREFRESHMENU = 0x0234,
        WM_MOUSEHOVER = 0x02A1,
        WM_NCMOUSELEAVE = 0x02A2,
        WM_MOUSELEAVE = 0x02A3,
        
        WM_WTSSESSION_CHANGE = 0x02b1,

        WM_TABLET_DEFBASE = 0x02C0,
        WM_TABLET_MAXOFFSET = 0x20,
        WM_TABLET_ADDED = WM_TABLET_DEFBASE + 8,
        WM_TABLET_DELETED = WM_TABLET_DEFBASE + 9,
        WM_TABLET_FLICK = WM_TABLET_DEFBASE + 11,
        WM_TABLET_QUERYSYSTEMGESTURESTATUS = WM_TABLET_DEFBASE + 12,

        WM_DPICHANGED = 0x02E0,
        WM_DPICHANGED_BEFOREPARENT = 0x02E2,
        WM_DPICHANGED_AFTERPARENT = 0x02E3,

        WM_CUT = 0x0300,
        WM_COPY = 0x0301,
        WM_PASTE = 0x0302,
        WM_CLEAR = 0x0303,
        WM_UNDO = 0x0304,
        WM_RENDERFORMAT = 0x0305,
        WM_RENDERALLFORMATS = 0x0306,
        WM_DESTROYCLIPBOARD = 0x0307,
        WM_DRAWCLIPBOARD = 0x0308,
        WM_PAINTCLIPBOARD = 0x0309,
        WM_VSCROLLCLIPBOARD = 0x030A,
        WM_SIZECLIPBOARD = 0x030B,
        WM_ASKCBFORMATNAME = 0x030C,
        WM_CHANGECBCHAIN = 0x030D,
        WM_HSCROLLCLIPBOARD = 0x030E,
        WM_QUERYNEWPALETTE = 0x030F,
        WM_PALETTEISCHANGING = 0x0310,
        WM_PALETTECHANGED = 0x0311,
        WM_HOTKEY = 0x0312,
        WM_PRINT = 0x0317,
        WM_PRINTCLIENT = 0x0318,
        WM_APPCOMMAND = 0x0319,
        WM_THEMECHANGED = 0x031A,

        WM_DWMCOMPOSITIONCHANGED = 0x031E,
        WM_DWMNCRENDERINGCHANGED = 0x031F,
        WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
        WM_HANDHELDFIRST = 0x0358,
        WM_HANDHELDLAST = 0x035F,
        WM_AFXFIRST = 0x0360,
        WM_AFXLAST = 0x037F,
        WM_PENWINFIRST = 0x0380,
        WM_PENWINLAST = 0x038F,

        #region Windows 7
        WM_DWMSENDICONICTHUMBNAIL = 0x0323,
        WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
        #endregion

        WM_USER = 0x0400,

        WM_APP = 0x8000,
    }

    /// <summary>
    /// Common native constants.
    /// </summary>
    internal static class Win32Constant
    {
        internal const int MAX_PATH = 260;
        internal const int INFOTIPSIZE = 1024;
        internal const int TRUE = 1;
        internal const int FALSE = 0;
    }
}
