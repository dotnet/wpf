// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace MS.Win32
{
    internal partial class UnsafeNativeMethods
    {
        [DllImport("msctf.dll")]
        internal static extern HRESULT TF_CreateThreadMgr(out ITfThreadMgr threadManager);

        [DllImport("msctf.dll")]
        public static extern HRESULT TF_CreateInputProcessorProfiles(out ITfInputProcessorProfiles profiles);

        [DllImport("msctf.dll")]
        public static extern HRESULT TF_CreateDisplayAttributeMgr(out ITfDisplayAttributeMgr dam);

        [DllImport("msctf.dll")]
        public static extern HRESULT TF_CreateCategoryMgr(out ITfCategoryMgr catmgr);

        public const int TF_CLIENTID_NULL = 0;

        public const char TS_CHAR_EMBEDDED = (char)0xfffc; // unicode 2.1 object replacement character
        public const char TS_CHAR_REGION = (char)0x0000; // region boundary
        public const char TS_CHAR_REPLACEMENT = (char)0xfffd; // hidden text placeholder char, Unicode replacement character
        public const int TS_DEFAULT_SELECTION = -1;
        public const int TS_S_ASYNC = 0x00040300;
        public const int TS_E_NOSELECTION = unchecked((int)0x80040205);
        public const int TS_E_NOLAYOUT = unchecked((int)0x80040206);
        public const int TS_E_INVALIDPOINT = unchecked((int)0x80040207);
        public const int TS_E_SYNCHRONOUS = unchecked((int)0x80040208);
        public const int TS_E_READONLY = unchecked((int)0x80040209);
        public const int TS_E_FORMAT = unchecked((int)0x8004020a);
        public const int TF_INVALID_COOKIE = -1;
        public const int TF_DICTATION_ON = 0x00000001;
        public const int TF_COMMANDING_ON = 0x00000008;

        public static readonly Guid IID_ITextStoreACPSink = new Guid(0x22d44c94, 0xa419, 0x4542, 0xa2, 0x72, 0xae, 0x26, 0x09, 0x3e, 0xce, 0xcf);
        public static readonly Guid IID_ITfThreadFocusSink = new Guid(0xc0f1db0c, 0x3a20, 0x405c, 0xa3, 0x03, 0x96, 0xb6, 0x01, 0x0a, 0x88, 0x5f);
        public static readonly Guid IID_ITfTextEditSink = new Guid(0x8127d409, 0xccd3, 0x4683, 0x96, 0x7a, 0xb4, 0x3d, 0x5b, 0x48, 0x2b, 0xf7);
        public static readonly Guid IID_ITfLanguageProfileNotifySink = new Guid(0x43c9fe15, 0xf494, 0x4c17, 0x9d, 0xe2, 0xb8, 0xa4, 0xac, 0x35, 0x0a, 0xa8);
        public static readonly Guid IID_ITfCompartmentEventSink = new Guid(0x743abd5f, 0xf26d, 0x48df, 0x8c, 0xc5, 0x23, 0x84, 0x92, 0x41, 0x9b, 0x64);
        public static readonly Guid IID_ITfTransitoryExtensionSink = new Guid(0xa615096f, 0x1c57, 0x4813, 0x8a, 0x15, 0x55, 0xee, 0x6e, 0x5a, 0x83, 0x9c);
        public static readonly Guid GUID_TFCAT_TIP_KEYBOARD = new Guid(0x34745c63, 0xb2f0, 0x4784, 0x8b, 0x67, 0x5e, 0x12, 0xc8, 0x70, 0x1a, 0x31);
        public static readonly Guid GUID_PROP_ATTRIBUTE = new Guid(0x34b45670, 0x7526, 0x11d2, 0xa1, 0x47, 0x00, 0x10, 0x5a, 0x27, 0x99, 0xb5);
        public static readonly Guid GUID_PROP_LANGID = new Guid(0x3280ce20, 0x8032, 0x11d2, 0xb6, 0x03, 0x00, 0x10, 0x5a, 0x27, 0x99, 0xb5);
        public static readonly Guid GUID_PROP_READING = new Guid(0x5463f7c0, 0x8e31, 0x11d2, 0xbf, 0x46, 0x00, 0x10, 0x5a, 0x27, 0x99, 0xb5);
        public static readonly Guid GUID_PROP_INPUTSCOPE = new Guid(0x1713dd5a, 0x68e7, 0x4a5b, 0x9a, 0xf6, 0x59, 0x2a, 0x59, 0x5c, 0x77, 0x8d);
        public static readonly Guid GUID_COMPARTMENT_KEYBOARD_DISABLED = new Guid(0x71a5b253, 0x1951, 0x466b, 0x9f, 0xbc, 0x9c, 0x88, 0x08, 0xfa, 0x84, 0xf2);
        public static Guid GUID_COMPARTMENT_KEYBOARD_OPENCLOSE = new Guid(0x58273aad, 0x01bb, 0x4164, 0x95, 0xc6, 0x75, 0x5b, 0xa0, 0xb5, 0x16, 0x2d);
        public static readonly Guid GUID_COMPARTMENT_HANDWRITING_OPENCLOSE = new Guid(0xf9ae2c6b, 0x1866, 0x4361, 0xaf, 0x72, 0x7a, 0xa3, 0x09, 0x48, 0x89, 0x0e);
        public static readonly Guid GUID_COMPARTMENT_SPEECH_DISABLED = new Guid(0x56c5c607, 0x0703, 0x4e59, 0x8e, 0x52, 0xcb, 0xc8, 0x4e, 0x8b, 0xbe, 0x35);
        public static readonly Guid GUID_COMPARTMENT_SPEECH_OPENCLOSE = new Guid(0x544d6a63, 0xe2e8, 0x4752, 0xbb, 0xd1, 0x00, 0x09, 0x60, 0xbc, 0xa0, 0x83);
        public static readonly Guid GUID_COMPARTMENT_SPEECH_GLOBALSTATE = new Guid(0x2a54fe8e, 0x0d08, 0x460c, 0xa7, 0x5d, 0x87, 0x03, 0x5f, 0xf4, 0x36, 0xc5);
        public static readonly Guid GUID_COMPARTMENT_KEYBOARD_INPUTMODE_CONVERSION = new Guid(0xccf05dd8, 0x4a87, 0x11d7, 0xa6, 0xe2, 0x00, 0x06, 0x5b, 0x84, 0x43, 0x5c);
        public static readonly Guid GUID_COMPARTMENT_KEYBOARD_INPUTMODE_SENTENCE = new Guid(0xccf05dd9, 0x4a87, 0x11d7, 0xa6, 0xe2, 0x00, 0x06, 0x5b, 0x84, 0x43, 0x5c);
        public static readonly Guid GUID_COMPARTMENT_TRANSITORYEXTENSION = new Guid(0x8be347f5, 0xc7a0, 0x11d7, 0xb4, 0x08, 0x00, 0x06, 0x5b, 0x84, 0x43, 0x5c);
        public static readonly Guid GUID_COMPARTMENT_TRANSITORYEXTENSION_DOCUMENTMANAGER = new Guid(0x8be347f7, 0xc7a0, 0x11d7, 0xb4, 0x08, 0x00, 0x06, 0x5b, 0x84, 0x43, 0x5c);
        public static readonly Guid GUID_COMPARTMENT_TRANSITORYEXTENSION_PARENT = new Guid(0x8be347f8, 0xc7a0, 0x11d7, 0xb4, 0x08, 0x00, 0x06, 0x5b, 0x84, 0x43, 0x5c);
        public static readonly Guid Clsid_SpeechTip = new Guid(0xdcbd6fa8, 0x032f, 0x11d3, 0xb5, 0xb1, 0x00, 0xc0, 0x4f, 0xc3, 0x24, 0xa1);
        public static readonly Guid Guid_Null = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        public static readonly Guid IID_ITfFnCustomSpeechCommand = new Guid(0xfca6c349, 0xa12f, 0x43a3, 0x8d, 0xd6, 0x5a, 0x5a, 0x42, 0x82, 0x57, 0x7b);
        public static readonly Guid IID_ITfFnReconversion = new Guid("4cea93c0-0a58-11d3-8df0-00105a2799b5");
        public static readonly Guid IID_ITfFnConfigure = new Guid(0x88f567c6, 0x1757, 0x49f8, 0xa1, 0xb2, 0x89, 0x23, 0x4c, 0x1e, 0xef, 0xf9);
        public static readonly Guid IID_ITfFnConfigureRegisterWord = new Guid(0xbb95808a, 0x6d8f, 0x4bca, 0x84, 0x00, 0x53, 0x90, 0xb5, 0x86, 0xae, 0xdf);
        public static readonly Guid TSATTRID_Font_FaceName = new Guid(0xb536aeb6, 0x053b, 0x4eb8, 0xb6, 0x5a, 0x50, 0xda, 0x1e, 0x81, 0xe7, 0x2e);
        public static readonly Guid TSATTRID_Font_SizePts = new Guid(0xc8493302, 0xa5e9, 0x456d, 0xaf, 0x04, 0x80, 0x05, 0xe4, 0x13, 0x0f, 0x03);
        public static readonly Guid TSATTRID_Font_Style_Height = new Guid(0x7e937477, 0x12e6, 0x458b, 0x92, 0x6a, 0x1f, 0xa4, 0x4e, 0xe8, 0xf3, 0x91);
        public static readonly Guid TSATTRID_Text_VerticalWriting = new Guid(0x6bba8195, 0x046f, 0x4ea9, 0xb3, 0x11, 0x97, 0xfd, 0x66, 0xc4, 0x27, 0x4b);
        public static readonly Guid TSATTRID_Text_Orientation = new Guid(0x6bab707f, 0x8785, 0x4c39, 0x8b, 0x52, 0x96, 0xf8, 0x78, 0x30, 0x3f, 0xfb);
        public static readonly Guid TSATTRID_Text_ReadOnly = new Guid(0x85836617, 0xde32, 0x4afd, 0xa5, 0x0f, 0xa2, 0xdb, 0x11, 0x0e, 0x6e, 0x4d);
        public static readonly Guid GUID_SYSTEM_FUNCTIONPROVIDER = new Guid("9a698bb0-0f21-11d3-8df1-00105a2799b5");


        [Flags]
        public enum PopFlags
        {
            TF_POPF_ALL = 0x0001,
        }

        [Flags]
        public enum CreateContextFlags
        {
            // TF_PLAINTEXTTSI is undocumented
        }

        [Flags]
        public enum SetTextFlags
        {
            TS_ST_CORRECTION = 0x1,
        }

        [Flags]
        public enum InsertEmbeddedFlags
        {
            TS_IE_CORRECTION = 0x1,
        }

        [Flags]
        public enum InsertAtSelectionFlags
        {
            TS_IAS_NOQUERY = 0x1,
            TS_IAS_QUERYONLY = 0x2,
        }

        [Flags]
        public enum AdviseFlags
        {
            TS_AS_TEXT_CHANGE = 0x01,
            TS_AS_SEL_CHANGE = 0x02,
            TS_AS_LAYOUT_CHANGE = 0x04,
            TS_AS_ATTR_CHANGE = 0x08,
            TS_AS_STATUS_CHANGE = 0x10,
        }

        [Flags]
        public enum LockFlags
        {
            TS_LF_SYNC = 0x1,
            TS_LF_READ = 0x2,
            TS_LF_WRITE = 0x4,
            TS_LF_READWRITE = 0x6,
        }

        [Flags]
        public enum DynamicStatusFlags
        {
            TS_SD_READONLY = 0x001,
            TS_SD_LOADING = 0x002,
        }

        [Flags]
        public enum StaticStatusFlags
        {
            TS_SS_DISJOINTSEL = 0x001,
            TS_SS_REGIONS = 0x002,
            TS_SS_TRANSITORY = 0x004,
            TS_SS_NOHIDDENTEXT = 0x008,
        }

        [Flags]
        public enum AttributeFlags
        {
            TS_ATTR_FIND_BACKWARDS = 0x0001,
            TS_ATTR_FIND_WANT_OFFSET = 0x0002,
            TS_ATTR_FIND_UPDATESTART = 0x0004,
            TS_ATTR_FIND_WANT_VALUE = 0x0008,
            TS_ATTR_FIND_WANT_END = 0x0010,
            TS_ATTR_FIND_HIDDEN = 0x0020,
        }

        [Flags]
        public enum GetPositionFromPointFlags
        {
            GXFPF_ROUND_NEAREST = 0x1,
            GXFPF_NEAREST = 0x2,
        }

        public enum TsActiveSelEnd
        {
            TS_AE_NONE = 0,
            TS_AE_START = 1,
            TS_AE_END = 2,
        }

        public enum TsRunType
        {
            TS_RT_PLAIN = 0,
            TS_RT_HIDDEN = 1,
            TS_RT_OPAQUE = 2,
        }

        [Flags]
        public enum OnTextChangeFlags
        {
            TS_TC_CORRECTION = 0x1,
        }

        public enum TsLayoutCode
        {
            TS_LC_CREATE = 0,
            TS_LC_CHANGE = 1,
            TS_LC_DESTROY = 2
        }

        public enum TfGravity
        {
            TF_GR_BACKWARD = 0,
            TF_GR_FORWARD = 1,
        };

        public enum TfShiftDir
        {
            TF_SD_BACKWARD = 0,
            TF_SD_FORWARD = 1,
        };

        public enum TfAnchor
        {
            TF_ANCHOR_START = 0,
            TF_ANCHOR_END = 1,
        }

        public enum TF_DA_COLORTYPE
        {
            TF_CT_NONE = 0,
            TF_CT_SYSCOLOR = 1,
            TF_CT_COLORREF = 2
        }

        public enum TF_DA_LINESTYLE
        {
            TF_LS_NONE = 0,
            TF_LS_SOLID = 1,
            TF_LS_DOT = 2,
            TF_LS_DASH = 3,
            TF_LS_SQUIGGLE = 4
        }

        public enum TF_DA_ATTR_INFO
        {
            TF_ATTR_INPUT = 0,
            TF_ATTR_TARGET_CONVERTED = 1,
            TF_ATTR_CONVERTED = 2,
            TF_ATTR_TARGET_NOTCONVERTED = 3,
            TF_ATTR_INPUT_ERROR = 4,
            TF_ATTR_FIXEDCONVERTED = 5,
            TF_ATTR_OTHER = -1
        }

        [Flags]
        public enum ConversionModeFlags
        {
            TF_CONVERSIONMODE_ALPHANUMERIC = 0x0000,
            TF_CONVERSIONMODE_NATIVE = 0x0001,
            TF_CONVERSIONMODE_KATAKANA = 0x0002,
            TF_CONVERSIONMODE_FULLSHAPE = 0x0008,
            TF_CONVERSIONMODE_ROMAN = 0x0010,
            TF_CONVERSIONMODE_CHARCODE = 0x0020,
            TF_CONVERSIONMODE_NOCONVERSION = 0x0100,
            TF_CONVERSIONMODE_EUDC = 0x0200,
            TF_CONVERSIONMODE_SYMBOL = 0x0400,
            TF_CONVERSIONMODE_FIXED = 0x0800,
        }

        [Flags]
        public enum SentenceModeFlags
        {
            TF_SENTENCEMODE_NONE = 0x0000,
            TF_SENTENCEMODE_PLAURALCLAUSE = 0x0001,
            TF_SENTENCEMODE_SINGLECONVERT = 0x0002,
            TF_SENTENCEMODE_AUTOMATIC = 0x0004,
            TF_SENTENCEMODE_PHRASEPREDICT = 0x0008,
            TF_SENTENCEMODE_CONVERSATION = 0x0010,
        }

        public enum TfCandidateResult
        {
            CAND_FINALIZED = 0x0,
            CAND_SELECTED = 0x1,
            CAND_CANCELED = 0x2,
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_STATUS
        {
            public DynamicStatusFlags dynamicFlags;
            public StaticStatusFlags staticFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_SELECTIONSTYLE
        {
            public TsActiveSelEnd ase;
            [MarshalAs(UnmanagedType.Bool)]
            public bool interimChar;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_SELECTION_ACP
        {
            public int start;
            public int end;
            public TS_SELECTIONSTYLE style;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_RUNINFO
        {
            public int count;
            public TsRunType type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_TEXTCHANGE
        {
            public int start;
            public int oldEnd;
            public int newEnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TS_ATTRVAL
        {
            public Guid attributeId;
            public Int32 overlappedId;

            // Let val's offset 0x18. Though default pack is 8...
            public Int32 reserved;

            [MarshalAs(UnmanagedType.Struct)]
            public NativeMethods.VARIANT val;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TF_PRESERVEDKEY
        {
            public int vKey;
            public int modifiers;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TF_DA_COLOR
        {
            public TF_DA_COLORTYPE type;
            public Int32 indexOrColorRef; // TF_CT_SYSCOLOR/TF_CT_COLORREF union
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TF_DISPLAYATTRIBUTE
        {
            public TF_DA_COLOR crText;
            public TF_DA_COLOR crBk;
            public TF_DA_LINESTYLE lsStyle;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fBoldLine;
            public TF_DA_COLOR crLine;
            public TF_DA_ATTR_INFO bAttr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TF_RENDERINGMARKUP
        {
            public ITfRange range;
            public TF_DISPLAYATTRIBUTE tfDisplayAttr;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TF_LANGUAGEPROFILE
        {
            internal Guid clsid;        // CLSID of tip
            internal short langid;      // language id
            internal Guid catid;         // category of tip
            [MarshalAs(UnmanagedType.Bool)]
            internal bool fActive;       // activated profile
            internal Guid guidProfile;   // profile description
        }

        #region Interfaces

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8f1b8ad8-0b6b-4874-90c5-bd76011e8f7c")]
        internal interface ITfMessagePump
        {
            //HRESULT PeekMessageA([out] LPMSG pMsg,
            //                     [in] HWND hwnd,
            //                     [in] UINT wMsgFilterMin,
            //                     [in] UINT wMsgFilterMax,
            //                     [in] UINT wRemoveMsg,
            //                     [out] BOOL *pfResult);
            void PeekMessageA(ref System.Windows.Interop.MSG msg,
                IntPtr hwnd,
                int msgFilterMin,
                int msgFilterMax,
                int removeMsg,
                out int result);

            //HRESULT GetMessageA([out] LPMSG pMsg,
            //                    [in] HWND hwnd,
            //                    [in] UINT wMsgFilterMin,
            //                    [in] UINT wMsgFilterMax,
            //                    [out] BOOL *pfResult);
            void GetMessageA(ref System.Windows.Interop.MSG msg,
                IntPtr hwnd,
                int msgFilterMin,
                int msgFilterMax,
                out int result);

            //HRESULT PeekMessageW([out] LPMSG pMsg,
            //                     [in] HWND hwnd,
            //                     [in] UINT wMsgFilterMin,
            //                     [in] UINT wMsgFilterMax,
            //                     [in] UINT wRemoveMsg,
            //                     [out] BOOL *pfResult);
            void PeekMessageW(ref System.Windows.Interop.MSG msg,
                IntPtr hwnd,
                int msgFilterMin,
                int msgFilterMax,
                int removeMsg,
                out int result);

            //HRESULT GetMessageW([out] LPMSG pMsg,
            //                    [in] HWND hwnd,
            //                    [in] UINT wMsgFilterMin,
            //                    [in] UINT wMsgFilterMax,
            //                    [out] BOOL *pfResult);
            void GetMessageW(ref System.Windows.Interop.MSG msg,
                IntPtr hwnd,
                int msgFilterMin,
                int msgFilterMax,
                out int result);
        };

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("e2449660-9542-11d2-bf46-00105a2799b5")]
        public interface ITfProperty /* : ITfReadOnlyProperty */
        {
            //HRESULT GetType([out] GUID *pguid);
            void GetType(out Guid type);

            //HRESULT EnumRanges([in] TfEditCookie ec,
            //                [out] IEnumTfRanges **ppEnum,
            //                [in] ITfRange *pTargetRange);
            [PreserveSig]
            int EnumRanges(int editcookie, out IEnumTfRanges ranges, ITfRange targetRange);

            //HRESULT GetValue([in] TfEditCookie ec,
            //                [in] ITfRange *pRange,
            //                [out] VARIANT *pvarValue);
            void GetValue(int editCookie, ITfRange range, out object value);

            //HRESULT GetContext([out] ITfContext **ppContext);
            void GetContext(out ITfContext context);

            //HRESULT FindRange([in] TfEditCookie ec,
            //                [in] ITfRange *pRange,
            //                [out] ITfRange **ppRange,
            //                [in] TfAnchor aPos);
            void FindRange(int editCookie, ITfRange inRange, out ITfRange outRange, TfAnchor position);

            //HRESULT SetValueStore([in] TfEditCookie ec,
            //                    [in] ITfRange *pRange,
            //                    [in] ITfPropertyStore *pPropStore);
            void stub_SetValueStore();

            //HRESULT SetValue([in] TfEditCookie ec,
            //                [in] ITfRange *pRange,
            //                [in] const VARIANT *pvarValue);
            void SetValue(int editCookie, ITfRange range, object value);

            //HRESULT Clear([in] TfEditCookie ec,
            //            [in] ITfRange *pRange);
            void Clear(int editCookie, ITfRange range);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e7fd-2021-11d2-93e0-0060b067b86e")]
        public interface ITfContext
        {
            //const DWORD TF_ES_ASYNCDONTCARE   = 0x0;
            //const DWORD TF_ES_SYNC            = 0x1;
            //const DWORD TF_ES_READ            = 0x2;
            //const DWORD TF_ES_READWRITE       = 0x6;
            //const DWORD TF_ES_ASYNC           = 0x8;


            //HRESULT RequestEditSession([in] TfClientId tid,
            //                        [in] ITfEditSession *pes,
            //                        [in] DWORD dwFlags,
            //                        [out] HRESULT *phrSession);
            int stub_RequestEditSession();


            //HRESULT InWriteSession([in] TfClientId tid,
            //                    [out] BOOL *pfWriteSession);
            void InWriteSession(int clientId, [MarshalAs(UnmanagedType.Bool)] out bool inWriteSession);

            //typedef [uuid(1690be9b-d3e9-49f6-8d8b-51b905af4c43)] enum { TF_AE_NONE = 0, TF_AE_START = 1, TF_AE_END = 2 } TfActiveSelEnd;

            //typedef [uuid(36ae42a4-6989-4bdc-b48a-6137b7bf2e42)] struct TF_SELECTIONSTYLE
            //{
            //    TfActiveSelEnd ase;
            //    BOOL fInterimChar;
            //} TF_SELECTIONSTYLE;

            //typedef [uuid(75eb22f2-b0bf-46a8-8006-975a3b6efcf1)] struct TF_SELECTION
            //{
            //    ITfRange *range;
            //    TF_SELECTIONSTYLE style;
            //} TF_SELECTION;

            //const ULONG TF_DEFAULT_SELECTION = TS_DEFAULT_SELECTION;


            //HRESULT GetSelection([in] TfEditCookie ec,
            //                    [in] ULONG ulIndex,
            //                    [in] ULONG ulCount,
            //                    [out, size_is(ulCount), length_is(*pcFetched)] TF_SELECTION *pSelection,
            //                    [out] ULONG *pcFetched);
            void stub_GetSelection();


            //HRESULT SetSelection([in] TfEditCookie ec, 
            //                    [in] ULONG ulCount,
            //                    [in, size_is(ulCount)] const TF_SELECTION *pSelection);
            void stub_SetSelection();

            //HRESULT GetStart([in] TfEditCookie ec,
            //                [out] ITfRange **ppStart);

            void GetStart(int ec, out ITfRange range);


            //HRESULT GetEnd([in] TfEditCookie ec,
            //            [out] ITfRange **ppEnd);
            void stub_GetEnd();

            // bit values for TF_STATUS's dwDynamicFlags field
            //const DWORD TF_SD_READONLY        = TS_SD_READONLY;       // if set, document is read only; writes will fail
            //const DWORD TF_SD_LOADING         = TS_SD_LOADING;        // if set, document is loading, expect additional inserts
            // bit values for TF_STATUS's dwStaticFlags field
            //const DWORD TF_SS_DISJOINTSEL     = TS_SS_DISJOINTSEL;    // if set, the document supports multiple selections
            //const DWORD TF_SS_REGIONS         = TS_SS_REGIONS;        // if clear, the document will never contain multiple regions
            //const DWORD TF_SS_TRANSITORY      = TS_SS_TRANSITORY;     // if set, the document is expected to have a short lifespan

            //typedef [uuid(bc7d979a-846a-444d-afef-0a9bfa82b961)] TS_STATUS TF_STATUS;


            //HRESULT GetActiveView([out] ITfContextView **ppView);
            void stub_GetActiveView();


            //HRESULT EnumViews([out] IEnumTfContextViews **ppEnum);
            void stub_EnumViews();


            //HRESULT GetStatus([out] TF_STATUS *pdcs);
            void stub_GetStatus();

            //HRESULT GetProperty([in] REFGUID guidProp,
            //                    [out] ITfProperty **ppProp);
            void GetProperty(ref Guid guid, out ITfProperty property);


            //HRESULT GetAppProperty([in] REFGUID guidProp,
            //                    [out] ITfReadOnlyProperty **ppProp);
            void stub_GetAppProperty();


            //HRESULT TrackProperties([in, size_is(cProp)] const GUID **prgProp,
            //                        [in] ULONG cProp,
            //                        [in, size_is(cAppProp)] const GUID **prgAppProp,
            //                        [in] ULONG cAppProp,   
            //                        [out] ITfReadOnlyProperty **ppProperty);
            void stub_TrackProperties();


            //HRESULT EnumProperties([out] IEnumTfProperties **ppEnum);
            void stub_EnumProperties();


            //HRESULT GetDocumentMgr([out] ITfDocumentMgr **ppDm);
            void stub_GetDocumentMgr();


            //HRESULT CreateRangeBackup([in] TfEditCookie ec,
            //                        [in] ITfRange *pRange,
            //                        [out] ITfRangeBackup **ppBackup);
            void stub_CreateRangeBackup();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e7f4-2021-11d2-93e0-0060b067b86e")]
        public interface ITfDocumentMgr
        {
            //HRESULT CreateContext([in] TfClientId tidOwner,
            //                      [in] DWORD dwFlags,
            //                      [in, unique] IUnknown *punk,
            //                      [out] ITfContext **ppic,
            //                      [out] TfEditCookie *pecTextStore);
            void CreateContext(int clientId, CreateContextFlags flags, [MarshalAs(UnmanagedType.Interface)] object obj, out ITfContext context, out int editCookie);

            //HRESULT Push([in] ITfContext *pic);
            void Push(ITfContext context);

            //HRESULT Pop([in] DWORD dwFlags);
            void Pop(PopFlags flags);

            //HRESULT GetTop([out] ITfContext **ppic);
            void GetTop(out ITfContext context);

            //HRESULT GetBase([out] ITfContext **ppic);
            void GetBase(out ITfContext context);

            //HRESULT EnumContexts([out] IEnumTfContexts **ppEnum);
            void EnumContexts([MarshalAs(UnmanagedType.Interface)] out /*IEnumTfContexts*/ object enumContexts);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e808-2021-11d2-93e0-0060b067b86e")]
        public interface IEnumTfDocumentMgrs
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("101d6610-0990-11d3-8df0-00105a2799b5")]
        public interface ITfFunctionProvider
        {

            //HRESULT GetType([out] GUID *pguid);
            void GetType(out Guid guid);


            //HRESULT GetDescription([out] BSTR *pbstrDesc);
            void GetDescription([MarshalAs(UnmanagedType.BStr)] out string desc);

            // HRESULT GetFunction([in] REFGUID rguid,
            //                    [in] REFIID riid,
            //                    [out, iid_is(riid)] IUnknown **ppunk);

            [PreserveSig]
            int GetFunction(ref Guid guid, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object obj);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("581f317e-fd9d-443f-b972-ed00467c5d40")]
        public interface ITfCandidateString
        {
            // HRESULT GetString([out] BSTR *pbstr);
            void GetString([MarshalAs(UnmanagedType.BStr)] out string funcName);

            // HRESULT GetIndex([out] ULONG *pnIndex);
            void GetIndex(out int nIndex);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("a3ad50fb-9bdb-49e3-a843-6c76520fbf5d")]
        public interface ITfCandidateList
        {
            // HRESULT EnumCandidates([out] IEnumTfCandidates **ppEnum);
            void EnumCandidates(out object enumCand);

            // HRESULT GetCandidate([in] ULONG nIndex,
            //                      [out] ITfCandidateString **ppCand);
            void GetCandidate(int nIndex, out ITfCandidateString candstring);

            // HRESULT GetCandidateNum([out] ULONG *pnCnt);
            void GetCandidateNum(out int nCount);

            // HRESULT SetResult([in] ULONG nIndex,
            //                   [in] TfCandidateResult imcr);
            void SetResult(int nIndex, TfCandidateResult result);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("4cea93c0-0a58-11d3-8df0-00105a2799b5")]
        public interface ITfFnReconversion
        {
            // HRESULT GetDisplayName([out] BSTR *pbstrName);
            void GetDisplayName([MarshalAs(UnmanagedType.BStr)] out string funcName);

            // HRESULT QueryRange([in] ITfRange *pRange,
            //                    [in, out, unique] ITfRange **ppNewRange,
            //                    [out] BOOL *pfConvertable);
            [PreserveSig]
            int QueryRange(ITfRange range,
                           out ITfRange newRange,
                           [MarshalAs(UnmanagedType.Bool)] out bool isConvertable);

            // HRESULT GetReconversion([in] ITfRange *pRange,
            //                         [out] ITfCandidateList **ppCandList);
            [PreserveSig]
            int GetReconversion(ITfRange range, out ITfCandidateList candList);

            /// HRESULT Reconvert([in] ITfRange *pRange);
            [PreserveSig]
            int Reconvert(ITfRange range);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("88f567c6-1757-49f8-a1b2-89234c1eeff9")]
        public interface ITfFnConfigure
        {
            // HRESULT GetDisplayName([out] BSTR *pbstrName);
            void GetDisplayName([MarshalAs(UnmanagedType.BStr)] out string funcName);

            // HRESULT Show([in] HWND hwndParent,
            //              [in] LANGID langid,
            //              [in] REFGUID rguidProfile);
            [PreserveSig]
            int Show(IntPtr hwndParent, short langid, ref Guid guidProfile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("bb95808a-6d8f-4bca-8400-5390b586aedf")]
        public interface ITfFnConfigureRegisterWord
        {
            // HRESULT GetDisplayName([out] BSTR *pbstrName);
            void GetDisplayName([MarshalAs(UnmanagedType.BStr)] out string funcName);

            // HRESULT Show([in] HWND hwndParent,
            //              [in] LANGID langid,
            //              [in] REFGUID rguidProfile,
            //              [in, unique] BSTR bstrRegistered);
            [PreserveSig]
            int Show(IntPtr hwndParent,
                     short langid,
                     ref Guid guidProfile,
                     [MarshalAs(UnmanagedType.BStr)] string bstrRegistered);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("e4b24db0-0990-11d3-8df0-00105a2799b5")]
        public interface IEnumTfFunctionProviders
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("bb08f7a9-607a-4384-8623-056892b64371")]
        public interface ITfCompartment
        {
            //HRESULT SetValue([in] TfClientId tid,
            //                 [in] const VARIANT *pvarValue);
            [PreserveSig]
            int SetValue(int tid, ref object varValue);

            void GetValue(out object varValue);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("743abd5f-f26d-48df-8cc5-238492419b64")]
        public interface ITfCompartmentEventSink
        {
            //HRESULT OnChange([in] REFGUID rguid);
            void OnChange(ref Guid rguid);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("7dcf57ac-18ad-438b-824d-979bffb74b7c")]
        public interface ITfCompartmentMgr
        {
            // <summary></summary>
            //HRESULT GetCompartment([in] REFGUID rguid,
            //                       [out] ITfCompartment **ppcomp);
            void GetCompartment(ref Guid guid, out ITfCompartment comp);

            //HRESULT ClearCompartment([in] TfClientId tid,
            //                        [in] REFGUID rguid);
            void ClearCompartment(int tid, Guid guid);

            //HRESULT EnumCompartments([out] IEnumGUID **ppEnum);
            void EnumCompartments(out object /*IEnumGUID*/ enumGuid);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e801-2021-11d2-93e0-0060b067b86e")]
        internal interface ITfThreadMgr
        {
            //HRESULT Activate([out] TfClientId *ptid);
            void Activate(out int clientId);

            //HRESULT Deactivate();
            void Deactivate();

            //HRESULT CreateDocumentMgr([out] ITfDocumentMgr **ppdim);
            void CreateDocumentMgr(out ITfDocumentMgr docMgr);

            //HRESULT EnumDocumentMgrs([out] IEnumTfDocumentMgrs **ppEnum);
            void EnumDocumentMgrs(out IEnumTfDocumentMgrs enumDocMgrs);

            //HRESULT GetFocus([out] ITfDocumentMgr **ppdimFocus);
            void GetFocus(out ITfDocumentMgr docMgr);

            //HRESULT SetFocus([in] ITfDocumentMgr *pdimFocus);
            void SetFocus(ITfDocumentMgr docMgr);

            //HRESULT AssociateFocus([in] HWND hwnd,
            //                       [in, unique] ITfDocumentMgr *pdimNew,
            //                       [out] ITfDocumentMgr **ppdimPrev);
            void AssociateFocus(IntPtr hwnd, ITfDocumentMgr newDocMgr, out ITfDocumentMgr prevDocMgr);

            //HRESULT IsThreadFocus([out] BOOL *pfThreadFocus);
            void IsThreadFocus([MarshalAs(UnmanagedType.Bool)] out bool isFocus);

            //HRESULT GetFunctionProvider([in] REFCLSID clsid,
            //                            [out] ITfFunctionProvider **ppFuncProv);
            [PreserveSig]
            int GetFunctionProvider(ref Guid classId, out ITfFunctionProvider funcProvider);

            //HRESULT EnumFunctionProviders([out] IEnumTfFunctionProviders **ppEnum);
            void EnumFunctionProviders(out IEnumTfFunctionProviders enumProviders);

            //HRESULT GetGlobalCompartment([out] ITfCompartmentMgr **ppCompMgr);
            void GetGlobalCompartment(out ITfCompartmentMgr compartmentMgr);
        }



        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("28888fe3-c2a0-483a-a3ea-8cb1ce51ff3d")]
        public interface ITextStoreACP
        {
            //HRESULT AdviseSink([in] REFIID riid,
            //                   [in, iid_is(riid)] IUnknown *punk,
            //                   [in] DWORD dwMask);
            void AdviseSink(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] object obj, AdviseFlags flags);

            //HRESULT UnadviseSink([in] IUnknown *punk);
            void UnadviseSink([MarshalAs(UnmanagedType.Interface)] object obj);

            //HRESULT RequestLock([in] DWORD dwLockFlags,
            //                    [out] HRESULT *phrSession);
            void RequestLock(LockFlags flags, out int hrSession);

            //HRESULT GetStatus([out] TS_STATUS *pdcs);
            void GetStatus(out TS_STATUS status);

            //HRESULT QueryInsert([in] LONG acpTestStart,
            //                    [in] LONG acpTestEnd,
            //                    [in] ULONG cch,
            //                    [out] LONG *pacpResultStart,
            //                    [out] LONG *pacpResultEnd);
            void QueryInsert(int start, int end, int cch, out int startResult, out int endResult);

            //HRESULT GetSelection([in] ULONG ulIndex,
            //                     [in] ULONG ulCount,
            //                     [out, size_is(ulCount), length_is(*pcFetched)] TS_SELECTION_ACP *pSelection,
            //                     [out] ULONG *pcFetched);
            void GetSelection(int index, int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] TS_SELECTION_ACP[] selection, out int fetched);

            //HRESULT SetSelection([in] ULONG ulCount,
            //                     [in, size_is(ulCount)] const TS_SELECTION_ACP *pSelection);
            void SetSelection(int count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] TS_SELECTION_ACP[] selection);

            //HRESULT GetText([in] LONG acpStart,
            //                [in] LONG acpEnd,
            //                [out, size_is(cchPlainReq), length_is(*pcchPlainRet)] WCHAR *pchPlain,
            //                [in] ULONG cchPlainReq,
            //                [out] ULONG *pcchPlainRet,
            //                [out, size_is(cRunInfoReq), length_is(*pcRunInfoRet)] TS_RUNINFO *prgRunInfo,
            //                [in] ULONG cRunInfoReq,
            //                [out] ULONG *pcRunInfoRet,
            //                [out] LONG *pacpNext);
            void GetText(int start, int end,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] text,
                int cchReq, out int charsCopied,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] TS_RUNINFO[] runInfo,
                int cRunInfoReq, out int cRunInfoRcv,
                out int nextCp);

            //HRESULT SetText([in] DWORD dwFlags,
            //                [in] LONG acpStart,
            //                [in] LONG acpEnd,
            //                [in, size_is(cch)] const WCHAR *pchText,
            //                [in] ULONG cch,
            //                [out] TS_TEXTCHANGE *pChange);
            void SetText(SetTextFlags flags, int start, int end,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] char[] text,
                int cch, out TS_TEXTCHANGE change);

            //HRESULT GetFormattedText([in] LONG acpStart,
            //                         [in] LONG acpEnd,
            //                         [out] IDataObject **ppDataObject);
            void GetFormattedText(int start, int end, [MarshalAs(UnmanagedType.Interface)] out object obj);

            //HRESULT GetEmbedded([in] LONG acpPos,
            //                    [in] REFGUID rguidService,
            //                    [in] REFIID riid,
            //                    [out, iid_is(riid)] IUnknown **ppunk);
            void GetEmbedded(int position, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object obj);

            //HRESULT QueryInsertEmbedded([in] const GUID *pguidService,
            //                            [in] const FORMATETC *pFormatEtc,
            //                            [out] BOOL *pfInsertable);
            void QueryInsertEmbedded(ref Guid guidService, IntPtr /*ref Win32.FORMATETC*/ formatEtc, [MarshalAs(UnmanagedType.Bool)] out bool insertable);

            //HRESULT InsertEmbedded([in] DWORD dwFlags,
            //                       [in] LONG acpStart,
            //                       [in] LONG acpEnd,
            //                       [in] IDataObject *pDataObject,
            //                       [out] TS_TEXTCHANGE *pChange);
            void InsertEmbedded(InsertEmbeddedFlags flags, int start, int end, [MarshalAs(UnmanagedType.Interface)] object obj, out TS_TEXTCHANGE change);

            //HRESULT InsertTextAtSelection([in] DWORD dwFlags,
            //                              [in, size_is(cch)] const WCHAR *pchText,
            //                              [in] ULONG cch,
            //                              [out] LONG *pacpStart,
            //                              [out] LONG *pacpEnd,
            //                              [out] TS_TEXTCHANGE *pChange);
            void InsertTextAtSelection(InsertAtSelectionFlags flags,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] char[] text,
                int cch,
                out int start, out int end, out TS_TEXTCHANGE change);

            //HRESULT InsertEmbeddedAtSelection([in] DWORD dwFlags,
            //                                  [in] IDataObject *pDataObject,
            //                                  [out] LONG *pacpStart,
            //                                  [out] LONG *pacpEnd,
            //                                  [out] TS_TEXTCHANGE *pChange);
            void InsertEmbeddedAtSelection(InsertAtSelectionFlags flags, [MarshalAs(UnmanagedType.Interface)] object obj,
                                        out int start, out int end, out TS_TEXTCHANGE change);

            //HRESULT RequestSupportedAttrs([in] DWORD dwFlags,
            //                              [in] ULONG cFilterAttrs,
            //                              [in, size_is(cFilterAttrs), unique] const TS_ATTRID *paFilterAttrs);
            [PreserveSig]
            int RequestSupportedAttrs(AttributeFlags flags, int count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Guid[] filterAttributes);

            //HRESULT RequestAttrsAtPosition([in] LONG acpPos,
            //                               [in] ULONG cFilterAttrs,
            //                               [in, size_is(cFilterAttrs), unique] const TS_ATTRID *paFilterAttrs,
            //                               [in] DWORD dwFlags);
            [PreserveSig]
            int RequestAttrsAtPosition(int position, int count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Guid[] filterAttributes,
                AttributeFlags flags);

            //HRESULT RequestAttrsTransitioningAtPosition([in] LONG acpPos,
            //                                            [in] ULONG cFilterAttrs,
            //                                            [in, size_is(cFilterAttrs), unique] const TS_ATTRID *paFilterAttrs,
            //                                            [in] DWORD dwFlags);
            void RequestAttrsTransitioningAtPosition(int position, int count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Guid[] filterAttributes,
                AttributeFlags flags);

            //HRESULT FindNextAttrTransition([in] LONG acpStart,
            //                               [in] LONG acpHalt,
            //                               [in] ULONG cFilterAttrs,
            //                               [in, size_is(cFilterAttrs), unique] const TS_ATTRID *paFilterAttrs,
            //                               [in] DWORD dwFlags,
            //                               [out] LONG *pacpNext,
            //                               [out] BOOL *pfFound,
            //                               [out] LONG *plFoundOffset);
            void FindNextAttrTransition(int start, int halt, int count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Guid[] filterAttributes,
                AttributeFlags flags, out int acpNext, [MarshalAs(UnmanagedType.Bool)] out bool found, out int foundOffset);

            //HRESULT RetrieveRequestedAttrs([in] ULONG ulCount,
            //                               [out, size_is(ulCount), length_is(*pcFetched)] TS_ATTRVAL *paAttrVals,
            //                               [out] ULONG *pcFetched);
            void RetrieveRequestedAttrs(int count,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] TS_ATTRVAL[] attributeVals,
                out int countFetched);

            //HRESULT GetEnd([out] LONG *pacp);
            void GetEnd(out int end);

            //HRESULT GetActiveView([out] TsViewCookie *pvcView);
            void GetActiveView(out int viewCookie);

            //HRESULT GetACPFromPoint([in] TsViewCookie vcView,
            //                        [in] const POINT *ptScreen,
            //                        [in] DWORD dwFlags, [out] LONG *pacp);
            void GetACPFromPoint(int viewCookie, ref POINT point, GetPositionFromPointFlags flags, out int position);

            //HRESULT GetTextExt([in] TsViewCookie vcView,
            //                   [in] LONG acpStart,
            //                   [in] LONG acpEnd,
            //                   [out] RECT *prc,
            //                   [out] BOOL *pfClipped);
            void GetTextExt(int viewCookie, int start, int end, out RECT rect, [MarshalAs(UnmanagedType.Bool)] out bool clipped);

            //HRESULT GetScreenExt([in] TsViewCookie vcView,
            //                     [out] RECT *prc);
            void GetScreenExt(int viewCookie, out RECT rect);

            //HRESULT GetWnd([in] TsViewCookie vcView,
            //               [out] HWND *phwnd);
            void GetWnd(int viewCookie, out IntPtr hwnd);
        };


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("22d44c94-a419-4542-a272-ae26093ececf")]
        public interface ITextStoreACPSink
        {

            //HRESULT OnTextChange([in] DWORD dwFlags,
            //                     [in] const TS_TEXTCHANGE *pChange);
            void OnTextChange(OnTextChangeFlags flags, ref TS_TEXTCHANGE change);

            //HRESULT OnSelectionChange();
            void OnSelectionChange();

            //HRESULT OnLayoutChange([in] TsLayoutCode lcode, [in] TsViewCookie vcView);
            void OnLayoutChange(TsLayoutCode lcode, int viewCookie);

            //HRESULT OnStatusChange([in] DWORD dwFlags);
            void OnStatusChange(DynamicStatusFlags flags);

            //HRESULT OnAttrsChange([in] LONG acpStart,
            //                      [in] LONG acpEnd,
            //                      [in] ULONG cAttrs,
            //                      [in, size_is(cAttrs)] const TS_ATTRID *paAttrs);
            void OnAttrsChange(int start, int end, int count, Guid[] attributes);

            //HRESULT OnLockGranted([in] DWORD dwLockFlags);
            [PreserveSig]
            int OnLockGranted(LockFlags flags);

            //HRESULT OnStartEditTransaction();
            void OnStartEditTransaction();

            //HRESULT OnEndEditTransaction();
            void OnEndEditTransaction();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("c0f1db0c-3a20-405c-a303-96b6010a885f")]
        public interface ITfThreadFocusSink
        {
            //HRESULT OnSetThreadFocus();
            void OnSetThreadFocus();

            //HRESULT OnKillThreadFocus();
            void OnKillThreadFocus();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("4ea48a35-60ae-446f-8fd6-e6a8d82459f7")]
        public interface ITfSource
        {
            //HRESULT AdviseSink([in] REFIID riid,
            //                   [in, iid_is(riid)] IUnknown *punk,
            //                   [out] DWORD *pdwCookie);
            void AdviseSink(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] object obj, out int cookie);

            //HRESULT UnadviseSink([in] DWORD dwCookie);
            void UnadviseSink(int cookie);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e7f0-2021-11d2-93e0-0060b067b86e")]
        public interface ITfKeystrokeMgr
        {
            //HRESULT AdviseKeyEventSink([in] TfClientId tid,
            //                           [in] ITfKeyEventSink *pSink,
            //                           [in] BOOL fForeground);
            void AdviseKeyEventSink(int clientId, [MarshalAs(UnmanagedType.Interface)] object obj/*ITfKeyEventSink sink*/, [MarshalAs(UnmanagedType.Bool)] bool fForeground);

            //HRESULT UnadviseKeyEventSink([in] TfClientId tid);
            void UnadviseKeyEventSink(int clientId);

            //HRESULT GetForeground([out] CLSID *pclsid);
            void GetForeground(out Guid clsid);

            //HRESULT TestKeyDown([in] WPARAM wParam,
            //                    [in] LPARAM lParam,
            //                    [out] BOOL *pfEaten);
            // int should be ok here, bit fields are well defined for this call as 32 bit, no pointers
            void TestKeyDown(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten);

            //HRESULT TestKeyUp([in] WPARAM wParam,
            //                  [in] LPARAM lParam,
            //                  [out] BOOL *pfEaten);
            // int should be ok here, bit fields are well defined for this call as 32 bit, no pointers
            void TestKeyUp(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten);

            //HRESULT KeyDown([in] WPARAM wParam,
            //                [in] LPARAM lParam,
            //                [out] BOOL *pfEaten);
            // int should be ok here, bit fields are well defined for this call as 32 bit, no pointers
            void KeyDown(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten);

            //HRESULT KeyUp([in] WPARAM wParam,
            //              [in] LPARAM lParam,
            //              [out] BOOL *pfEaten);
            // int should be ok here, bit fields are well defined for this call as 32 bit, no pointers
            void KeyUp(int wParam, int lParam, [MarshalAs(UnmanagedType.Bool)] out bool eaten);

            //HRESULT GetPreservedKey([in] ITfContext *pic,
            //                        [in] const TF_PRESERVEDKEY *pprekey,
            //                        [out] GUID *pguid);
            void GetPreservedKey(ITfContext context, ref TF_PRESERVEDKEY key, out Guid guid);

            //HRESULT IsPreservedKey([in] REFGUID rguid,
            //                       [in] const TF_PRESERVEDKEY *pprekey,
            //                       [out] BOOL *pfRegistered);
            void IsPreservedKey(ref Guid guid, ref TF_PRESERVEDKEY key, [MarshalAs(UnmanagedType.Bool)] out bool registered);

            //HRESULT PreserveKey([in] TfClientId tid,
            //                    [in] REFGUID rguid,
            //                    [in] const TF_PRESERVEDKEY *prekey,
            //                    [in, size_is(cchDesc)] const WCHAR *pchDesc,
            //                    [in] ULONG cchDesc);
            void PreserveKey(int clientId, ref Guid guid, ref TF_PRESERVEDKEY key,
                            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] char[] desc, int descCount);

            //HRESULT UnpreserveKey([in] REFGUID rguid, 
            //                      [in] const TF_PRESERVEDKEY *pprekey);
            void UnpreserveKey(ref Guid guid, ref TF_PRESERVEDKEY key);

            //HRESULT SetPreservedKeyDescription([in] REFGUID rguid,
            //                                   [in, size_is(cchDesc)] const WCHAR *pchDesc,
            //                                   [in] ULONG cchDesc);
            void SetPreservedKeyDescription(ref Guid guid,
                                            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] char[] desc, int descCount);

            //HRESULT GetPreservedKeyDescription([in] REFGUID rguid,
            //                                   [out] BSTR *pbstrDesc);
            void GetPreservedKeyDescription(ref Guid guid, [MarshalAs(UnmanagedType.BStr)] out string desc);

            //HRESULT SimulatePreservedKey([in] ITfContext *pic,
            //                             [in] REFGUID rguid,
            //                             [out] BOOL *pfEaten);
            void SimulatePreservedKey(ITfContext context, ref Guid guid, [MarshalAs(UnmanagedType.Bool)] out bool eaten);
        };

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e7ff-2021-11d2-93e0-0060b067b86e")]
        public interface ITfRange
        {
            //HRESULT GetText([in] TfEditCookie ec,
            //                [in] DWORD dwFlags,
            //                [out, size_is(cchMax), length_is(*pcch)] WCHAR *pchText,
            //                [in] ULONG cchMax,
            //                [out] ULONG *pcch);
            void GetText(int ec, /*GetTextFlags*/int flags,
                        [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] text,
                        int countMax, out int count);

            //HRESULT SetText([in] TfEditCookie ec,
            //                [in] DWORD dwFlags,
            //                [in, size_is(cch), unique] const WCHAR *pchText,
            //                [in] LONG cch);
            void SetText(int ec, /*SetTextFlags*/ int flags,
                        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] text,
                        int count);

            //HRESULT GetFormattedText([in] TfEditCookie ec,
            //                         [out] IDataObject **ppDataObject);
            void GetFormattedText(int ec, [MarshalAs(UnmanagedType.Interface)] out object data);

            //HRESULT GetEmbedded([in] TfEditCookie ec,
            //                    [in] REFGUID rguidService,
            //                    [in] REFIID riid,
            //                    [out, iid_is(riid)] IUnknown **ppunk);
            void GetEmbedded(int ec, ref Guid guidService, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);

            //HRESULT InsertEmbedded([in] TfEditCookie ec,
            //                       [in] DWORD dwFlags,
            //                       [in] IDataObject *pDataObject);
            void InsertEmbedded(int ec, int flags, [MarshalAs(UnmanagedType.Interface)] object data);

            //HRESULT ShiftStart([in] TfEditCookie ec,
            //                   [in] LONG cchReq,
            //                   [out] LONG *pcch,
            //                   [in, unique] const TF_HALTCOND *pHalt);
            void ShiftStart(int ec, int count, out int result, IntPtr pHalt);

            //HRESULT ShiftEnd([in] TfEditCookie ec,
            //                 [in] LONG cchReq,
            //                 [out] LONG *pcch,
            //                 [in, unique] const TF_HALTCOND *pHalt);
            void ShiftEnd(int ec, int count, out int result, IntPtr pHalt);

            //HRESULT ShiftStartToRange([in] TfEditCookie ec,
            //                          [in] ITfRange *pRange,
            //                          [in] TfAnchor aPos);
            void ShiftStartToRange(int ec, ITfRange range, TfAnchor position);

            //HRESULT ShiftEndToRange([in] TfEditCookie ec,
            //                        [in] ITfRange *pRange,
            //                        [in] TfAnchor aPos);
            void ShiftEndToRange(int ec, ITfRange range, TfAnchor position);

            //HRESULT ShiftStartRegion([in] TfEditCookie ec,
            //                         [in] TfShiftDir dir,
            //                         [out] BOOL *pfNoRegion);
            void ShiftStartRegion(int ec, TfShiftDir dir, [MarshalAs(UnmanagedType.Bool)] out bool noRegion);

            //HRESULT ShiftEndRegion([in] TfEditCookie ec,
            //                       [in] TfShiftDir dir,
            //                       [out] BOOL *pfNoRegion);
            void ShiftEndRegion(int ec, TfShiftDir dir, [MarshalAs(UnmanagedType.Bool)] out bool noRegion);

            //HRESULT IsEmpty([in] TfEditCookie ec,
            //                [out] BOOL *pfEmpty);
            void IsEmpty(int ec, [MarshalAs(UnmanagedType.Bool)] out bool empty);

            //HRESULT Collapse([in] TfEditCookie ec,
            //                 [in] TfAnchor aPos);
            void Collapse(int ec, TfAnchor position);

            //HRESULT IsEqualStart([in] TfEditCookie ec,
            //                     [in] ITfRange *pWith,
            //                     [in] TfAnchor aPos,
            //                     [out] BOOL *pfEqual);
            void IsEqualStart(int ec, ITfRange with, TfAnchor position, [MarshalAs(UnmanagedType.Bool)] out bool equal);

            //HRESULT IsEqualEnd([in] TfEditCookie ec,
            //                   [in] ITfRange *pWith,
            //                   [in] TfAnchor aPos,
            //                   [out] BOOL *pfEqual);
            void IsEqualEnd(int ec, ITfRange with, TfAnchor position, [MarshalAs(UnmanagedType.Bool)] out bool equal);

            //HRESULT CompareStart([in] TfEditCookie ec,
            //                     [in] ITfRange *pWith,
            //                     [in] TfAnchor aPos,
            //                     [out] LONG *plResult);
            void CompareStart(int ec, ITfRange with, TfAnchor position, out int result);

            //HRESULT CompareEnd([in] TfEditCookie ec,
            //                   [in] ITfRange *pWith,
            //                   [in] TfAnchor aPos,
            //                   [out] LONG *plResult);
            void CompareEnd(int ec, ITfRange with, TfAnchor position, out int result);

            //HRESULT AdjustForInsert([in] TfEditCookie ec,
            //                        [in] ULONG cchInsert,
            //                        [out] BOOL *pfInsertOk);
            void AdjustForInsert(int ec, int count, [MarshalAs(UnmanagedType.Bool)] out bool insertOk);

            //HRESULT GetGravity([out] TfGravity *pgStart,
            //                   [out] TfGravity *pgEnd);
            void GetGravity(out TfGravity start, out TfGravity end);

            //HRESULT SetGravity([in] TfEditCookie ec,
            //                   [in] TfGravity gStart,
            //                   [in] TfGravity gEnd);
            void SetGravity(int ec, TfGravity start, TfGravity end);

            //HRESULT Clone([out] ITfRange **ppClone);
            void Clone(out ITfRange clone);

            //HRESULT GetContext([out] ITfContext **ppContext);
            void GetContext(out ITfContext context);
        };


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("057a6296-029b-4154-b79a-0d461d4ea94c")]
        public interface ITfRangeACP /*: ITfRange*/ // derivation isn't working, calls to GetExtent go to ITfRange::GetText/vtbl[0]
        {
            //HRESULT GetText([in] TfEditCookie ec,
            //                [in] DWORD dwFlags,
            //                [out, size_is(cchMax), length_is(*pcch)] WCHAR *pchText,
            //                [in] ULONG cchMax,
            //                [out] ULONG *pcch);
            void GetText(int ec, /*GetTextFlags*/int flags,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] text,
                int countMax, out int count);

            //HRESULT SetText([in] TfEditCookie ec,
            //                [in] DWORD dwFlags,
            //                [in, size_is(cch), unique] const WCHAR *pchText,
            //                [in] LONG cch);
            void SetText(int ec, /*SetTextFlags*/ int flags,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] char[] text,
                int count);

            //HRESULT GetFormattedText([in] TfEditCookie ec,
            //                         [out] IDataObject **ppDataObject);
            void GetFormattedText(int ec, [MarshalAs(UnmanagedType.Interface)] out object data);

            //HRESULT GetEmbedded([in] TfEditCookie ec,
            //                    [in] REFGUID rguidService,
            //                    [in] REFIID riid,
            //                    [out, iid_is(riid)] IUnknown **ppunk);
            void GetEmbedded(int ec, ref Guid guidService, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);

            //HRESULT InsertEmbedded([in] TfEditCookie ec,
            //                       [in] DWORD dwFlags,
            //                       [in] IDataObject *pDataObject);
            void InsertEmbedded(int ec, int flags, [MarshalAs(UnmanagedType.Interface)] object data);

            //HRESULT ShiftStart([in] TfEditCookie ec,
            //                   [in] LONG cchReq,
            //                   [out] LONG *pcch,
            //                   [in, unique] const TF_HALTCOND *pHalt);
            void ShiftStart(int ec, int count, out int result, IntPtr pHalt);

            //HRESULT ShiftEnd([in] TfEditCookie ec,
            //                 [in] LONG cchReq,
            //                 [out] LONG *pcch,
            //                 [in, unique] const TF_HALTCOND *pHalt);
            void ShiftEnd(int ec, int count, out int result, IntPtr pHalt);

            //HRESULT ShiftStartToRange([in] TfEditCookie ec,
            //                          [in] ITfRange *pRange,
            //                          [in] TfAnchor aPos);
            void ShiftStartToRange(int ec, ITfRange range, TfAnchor position);

            //HRESULT ShiftEndToRange([in] TfEditCookie ec,
            //                        [in] ITfRange *pRange,
            //                        [in] TfAnchor aPos);
            void ShiftEndToRange(int ec, ITfRange range, TfAnchor position);

            //HRESULT ShiftStartRegion([in] TfEditCookie ec,
            //                         [in] TfShiftDir dir,
            //                         [out] BOOL *pfNoRegion);
            void ShiftStartRegion(int ec, TfShiftDir dir, [MarshalAs(UnmanagedType.Bool)] out bool noRegion);

            //HRESULT ShiftEndRegion([in] TfEditCookie ec,
            //                       [in] TfShiftDir dir,
            //                       [out] BOOL *pfNoRegion);
            void ShiftEndRegion(int ec, TfShiftDir dir, [MarshalAs(UnmanagedType.Bool)] out bool noRegion);

            //HRESULT IsEmpty([in] TfEditCookie ec,
            //                [out] BOOL *pfEmpty);
            void IsEmpty(int ec, [MarshalAs(UnmanagedType.Bool)] out bool empty);

            //HRESULT Collapse([in] TfEditCookie ec,
            //                 [in] TfAnchor aPos);
            void Collapse(int ec, TfAnchor position);

            //HRESULT IsEqualStart([in] TfEditCookie ec,
            //                     [in] ITfRange *pWith,
            //                     [in] TfAnchor aPos,
            //                     [out] BOOL *pfEqual);
            void IsEqualStart(int ec, ITfRange with, TfAnchor position, [MarshalAs(UnmanagedType.Bool)] out bool equal);

            //HRESULT IsEqualEnd([in] TfEditCookie ec,
            //                   [in] ITfRange *pWith,
            //                   [in] TfAnchor aPos,
            //                   [out] BOOL *pfEqual);
            void IsEqualEnd(int ec, ITfRange with, TfAnchor position, [MarshalAs(UnmanagedType.Bool)] out bool equal);

            //HRESULT CompareStart([in] TfEditCookie ec,
            //                     [in] ITfRange *pWith,
            //                     [in] TfAnchor aPos,
            //                     [out] LONG *plResult);
            void CompareStart(int ec, ITfRange with, TfAnchor position, out int result);

            //HRESULT CompareEnd([in] TfEditCookie ec,
            //                   [in] ITfRange *pWith,
            //                   [in] TfAnchor aPos,
            //                   [out] LONG *plResult);
            void CompareEnd(int ec, ITfRange with, TfAnchor position, out int result);

            //HRESULT AdjustForInsert([in] TfEditCookie ec,
            //                        [in] ULONG cchInsert,
            //                        [out] BOOL *pfInsertOk);
            void AdjustForInsert(int ec, int count, [MarshalAs(UnmanagedType.Bool)] out bool insertOk);

            //HRESULT GetGravity([out] TfGravity *pgStart,
            //                   [out] TfGravity *pgEnd);
            void GetGravity(out TfGravity start, out TfGravity end);

            //HRESULT SetGravity([in] TfEditCookie ec,
            //                   [in] TfGravity gStart,
            //                   [in] TfGravity gEnd);
            void SetGravity(int ec, TfGravity start, TfGravity end);

            //HRESULT Clone([out] ITfRange **ppClone);
            void Clone(out ITfRange clone);

            //HRESULT GetContext([out] ITfContext **ppContext);
            void GetContext(out ITfContext context);

            //HRESULT GetExtent([out] LONG *pacpAnchor,
            //                  [out] LONG *pcch);
            void GetExtent(out int start, out int count);

            //HRESULT SetExtent([in] LONG acpAnchor,
            //                  [in] LONG cch);
            void SetExtent(int start, int count);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("D7540241-F9A1-4364-BEFC-DBCD2C4395B7")]
        public interface ITfCompositionView
        {
            //HRESULT GetOwnerClsid([out] CLSID *pclsid);
            void GetOwnerClsid(out Guid clsid);

            //HRESULT GetRange([out] ITfRange **ppRange);
            void GetRange(out ITfRange range);
        };


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("5F20AA40-B57A-4F34-96AB-3576F377CC79")]
        public interface ITfContextOwnerCompositionSink
        {
            //HRESULT OnStartComposition([in] ITfCompositionView *pComposition,
            //                           [out] BOOL *pfOk);
            void OnStartComposition(ITfCompositionView view, [MarshalAs(UnmanagedType.Bool)] out bool ok);

            //HRESULT OnUpdateComposition([in] ITfCompositionView *pComposition,
            //                            [in] ITfRange *pRangeNew);
            void OnUpdateComposition(ITfCompositionView view, ITfRange rangeNew);

            //HRESULT OnEndComposition([in] ITfCompositionView *pComposition);
            void OnEndComposition(ITfCompositionView view);
        };


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("D40C8AAE-AC92-4FC7-9A11-0EE0E23AA39B")]
        public interface ITfContextComposition
        {
            //HRESULT StartComposition([in] TfEditCookie ecWrite,
            //                         [in] ITfRange *pCompositionRange,
            //                         [in] ITfCompositionSink *pSink,
            //                         [out] ITfComposition **ppComposition);
            void StartComposition(int ecWrite, ITfRange range, [MarshalAs(UnmanagedType.Interface)] object /*ITfCompositionSink */sink, [MarshalAs(UnmanagedType.Interface)] out object /*ITfComposition */composition);

            //HRESULT EnumCompositions([out] IEnumITfCompositionView **ppEnum);
            void EnumCompositions([MarshalAs(UnmanagedType.Interface)] out IEnumITfCompositionView enumView);

            //HRESULT FindComposition([in] TfEditCookie ecRead,
            //                        [in] ITfRange *pTestRange,
            //                        [out] IEnumITfCompositionView **ppEnum);
            void FindComposition(int ecRead, ITfRange testRange, [MarshalAs(UnmanagedType.Interface)] out object /*IEnumITfCompositionView*/ enumView);

            //HRESULT TakeOwnership([in] TfEditCookie ecWrite,
            //                      [in] ITfCompositionView *pComposition,
            //                      [in] ITfCompositionSink *pSink,
            //                      [out] ITfComposition **ppComposition);
            void TakeOwnership(int ecWrite, ITfCompositionView view, [MarshalAs(UnmanagedType.Interface)] object /*ITfCompositionSink */ sink,
                            [MarshalAs(UnmanagedType.Interface)] out object /*ITfComposition*/ composition);
        };

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("86462810-593B-4916-9764-19C08E9CE110")]
        public interface ITfContextOwnerCompositionServices /*: ITfContextComposition*/
        {
            //HRESULT StartComposition([in] TfEditCookie ecWrite,
            //                         [in] ITfRange *pCompositionRange,
            //                         [in] ITfCompositionSink *pSink,
            //                         [out] ITfComposition **ppComposition);
            void StartComposition(int ecWrite, ITfRange range, [MarshalAs(UnmanagedType.Interface)] object /*ITfCompositionSink */sink, [MarshalAs(UnmanagedType.Interface)] out object /*ITfComposition */composition);

            //HRESULT EnumCompositions([out] IEnumITfCompositionView **ppEnum);
            void EnumCompositions([MarshalAs(UnmanagedType.Interface)] out object /*IEnumITfCompositionView*/ enumView);

            //HRESULT FindComposition([in] TfEditCookie ecRead,
            //                        [in] ITfRange *pTestRange,
            //                        [out] IEnumITfCompositionView **ppEnum);
            void FindComposition(int ecRead, ITfRange testRange, [MarshalAs(UnmanagedType.Interface)] out object /*IEnumITfCompositionView*/ enumView);

            //HRESULT TakeOwnership([in] TfEditCookie ecWrite,
            //                      [in] ITfCompositionView *pComposition,
            //                      [in] ITfCompositionSink *pSink,
            //                      [out] ITfComposition **ppComposition);
            void TakeOwnership(int ecWrite, ITfCompositionView view, [MarshalAs(UnmanagedType.Interface)] object /*ITfCompositionSink */ sink,
                            [MarshalAs(UnmanagedType.Interface)] out object /*ITfComposition*/ composition);

            //HRESULT TerminateComposition([in] ITfCompositionView *pComposition);
            [PreserveSig]
            int TerminateComposition(ITfCompositionView view);
        };

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("5EFD22BA-7838-46CB-88E2-CADB14124F8F")]
        internal interface IEnumITfCompositionView
        {
            //HRESULT Clone([out] IEnumTfRanges **ppEnum);
            void Clone(out IEnumTfRanges ranges);

            //HRESULT Next([in] ULONG ulCount,
            //            [out, size_is(ulCount), length_is(*pcFetched)] ITfRange **ppRange,
            //            [out] ULONG *pcFetched);
            [PreserveSig]
            unsafe int Next(int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ITfCompositionView[] compositionview, out int fetched);

            //HRESULT Reset();
            void Reset();

            //HRESULT Skip(ULONG ulCount);
            [PreserveSig]
            int Skip(int count);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("f99d3f40-8e32-11d2-bf46-00105a2799b5")]
        public interface IEnumTfRanges
        {
            //HRESULT Clone([out] IEnumTfRanges **ppEnum);
            void Clone(out IEnumTfRanges ranges);

            //HRESULT Next([in] ULONG ulCount,
            //            [out, size_is(ulCount), length_is(*pcFetched)] ITfRange **ppRange,
            //            [out] ULONG *pcFetched);
            [PreserveSig]
            unsafe int Next(int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ITfRange[] ranges, out int fetched);

            //HRESULT Reset();
            void Reset();

            //HRESULT Skip(ULONG ulCount);
            [PreserveSig]
            int Skip(int count);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("42d4d099-7c1a-4a89-b836-6c6f22160df0")]
        public interface ITfEditRecord
        {
            //HRESULT GetSelectionStatus([out] BOOL *pfChanged);
            void GetSelectionStatus([MarshalAs(UnmanagedType.Bool)] out bool selectionChanged);

            //HRESULT GetTextAndPropertyUpdates([in] DWORD dwFlags,
            //                                  [in, size_is(cProperties)] const GUID **prgProperties,
            //                                  [in] ULONG cProperties,
            //                                  [out] IEnumTfRanges **ppEnum);
            //
            //
            // Use "ref IntPtr" Temporarily.
            // See the comment in InputMethodProperty.GetPropertyUpdate().
            unsafe void GetTextAndPropertyUpdates(int flags,
                                                /*[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)]*/ /*Guid ** */ ref IntPtr properties,
                                                int count,
                                                out IEnumTfRanges ranges);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8127d409-ccd3-4683-967a-b43d5b482bf7")]
        public interface ITfTextEditSink
        {
            //HRESULT OnEndEdit([in] ITfContext *pic, [in] TfEditCookie ecReadOnly, [in] ITfEditRecord *pEditRecord);
            void OnEndEdit(ITfContext context, int ecReadOnly, ITfEditRecord editRecord);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8c03d21b-95a7-4ba0-ae1b-7fce12a72930")]
        public interface IEnumTfRenderingMarkup
        {
            //HRESULT Clone([out] IEnumTfRenderingMarkup **ppClone);
            void Clone(out IEnumTfRenderingMarkup clone);

            //HRESULT Next([in] ULONG ulCount,
            //            [out, size_is(ulCount), length_is(*pcFetched)] TF_RENDERINGMARKUP *rgMarkup,
            //            [out] ULONG *pcFetched);
            [PreserveSig]
            int Next(int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] TF_RENDERINGMARKUP[] markup, out int fetched);

            //HRESULT Reset();
            void Reset();

            //HRESULT Skip([in] ULONG ulCount);
            [PreserveSig]
            int Skip(int count);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("1F02B6C5-7842-4EE6-8A0B-9A24183A95CA")]
        public interface ITfInputProcessorProfiles
        {
            // HRESULT Register([in] REFCLSID rclsid);
            void stub_Register();

            // HRESULT Unregister([in] REFCLSID rclsid);
            void stub_Unregister();

            // HRESULT AddLanguageProfile([in] REFCLSID rclsid,
            //                            [in] LANGID langid,
            //                            [in] REFGUID guidProfile,
            //                            [in, size_is(cchDesc)] const WCHAR *pchDesc,
            //                            [in] ULONG cchDesc,
            //                            [in, size_is(cchFile)] const WCHAR *pchIconFile,
            //                            [in] ULONG cchFile,
            //                            [in] ULONG uIconIndex);
            void stub_AddLanguageProfile();

            // HRESULT RemoveLanguageProfile([in] REFCLSID rclsid,
            //                               [in] LANGID langid,
            //                               [in] REFGUID guidProfile);
            void stub_RemoveLanguageProfile();

            // HRESULT EnumInputProcessorInfo([out] IEnumGUID **ppEnum);
            void stub_EnumInputProcessorInfo();

            // HRESULT GetDefaultLanguageProfile([in] LANGID langid,
            //                                  [in] REFGUID catid,
            //                                  [out] CLSID *pclsid,
            //                                  [out] GUID *pguidProfile);
            void stub_GetDefaultLanguageProfile();

            // HRESULT SetDefaultLanguageProfile([in] LANGID langid,
            //                                   [in] REFCLSID rclsid,
            //                                   [in] REFGUID guidProfiles);
            void stub_SetDefaultLanguageProfile();

            // HRESULT ActivateLanguageProfile([in] REFCLSID rclsid,
            //                                 [in] LANGID langid,
            //                                 [in] REFGUID guidProfiles);
            void ActivateLanguageProfile(ref Guid clsid, short langid, ref Guid guidProfile);

            // HRESULT GetActiveLanguageProfile([in] REFCLSID rclsid,
            //                                  [out] LANGID *plangid,
            //                                  [out] GUID *pguidProfile);
            [PreserveSig]
            int GetActiveLanguageProfile(ref Guid clsid, out short langid, out Guid profile);

            // HRESULT GetLanguageProfileDescription([in] REFCLSID rclsid,
            //                                       [in] LANGID langid,
            //                                       [in] REFGUID guidProfile,
            //                                       [out] BSTR *pbstrProfile);
            void stub_GetLanguageProfileDescription();

            // HRESULT GetCurrentLanguage([out] LANGID *plangid);
            void GetCurrentLanguage(out short langid);

            // HRESULT ChangeCurrentLanguage([in] LANGID langid);
            [PreserveSig]
            int ChangeCurrentLanguage(short langid);

            // HRESULT GetLanguageList([out] LANGID **ppLangId,
            //                         [out] ULONG *pulCount);
            [PreserveSig]
            int GetLanguageList(out IntPtr langids, out int count);


            // HRESULT EnumLanguageProfiles([in] LANGID langid,
            //                              [out] IEnumTfLanguageProfiles **ppEnum);
            void EnumLanguageProfiles(short langid, out IEnumTfLanguageProfiles enumIPP);


            // HRESULT EnableLanguageProfile([in] REFCLSID rclsid,
            //                               [in] LANGID langid,
            //                               [in] REFGUID guidProfile,
            //                               [in] BOOL fEnable);
            void stub_EnableLanguageProfile();

            // HRESULT IsEnabledLanguageProfile([in] REFCLSID rclsid,
            //                                  [in] LANGID langid,
            //                                  [in] REFGUID guidProfile,
            //                                  [out] BOOL *pfEnable);
            void stub_IsEnabledLanguageProfile();

            // HRESULT EnableLanguageProfileByDefault([in] REFCLSID rclsid,
            //                                        [in] LANGID langid,
            //                                        [in] REFGUID guidProfile,
            //                                        [in] BOOL fEnable);
            void stub_EnableLanguageProfileByDefault();

            // HRESULT SubstituteKeyboardLayout([in] REFCLSID rclsid,
            //                                  [in] LANGID langid,
            //                                  [in] REFGUID guidProfile,
            //                                  [in] HKL hKL);
            void stub_SubstituteKeyboardLayout();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3d61bf11-ac5f-42c8-a4cb-931bcc28c744")]
        internal interface IEnumTfLanguageProfiles
        {
            // HRESULT Clone([out] IEnumTfLanguageProfiles **ppEnum);
            void Clone(out IEnumTfLanguageProfiles enumIPP);

            // HRESULT Next([in] ULONG ulCount,
            //              [out, size_is(ulCount), length_is(*pcFetch)] TF_LANGUAGEPROFILE *pProfile,
            //              [out] ULONG *pcFetch);
            [PreserveSig]
            int Next(int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] TF_LANGUAGEPROFILE[] profiles, out int fetched);

            // HRESULT Reset();
            void Reset();

            // HRESULT Skip([in] ULONG ulCount);
            void Skip(int count);
        }


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43c9fe15-f494-4c17-9de2-b8a4ac350aa8")]
        public interface ITfLanguageProfileNotifySink
        {
            // HRESULT OnLanguageChange([in] LANGID langid,
            //                          [out] BOOL *pfAccept);
            void OnLanguageChange(short langid, [MarshalAs(UnmanagedType.Bool)] out bool bAccept);

            // HRESULT OnLanguageChanged();
            void OnLanguageChanged();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8ded7393-5db1-475c-9e71-a39111b0ff67")]
        public interface ITfDisplayAttributeMgr
        {
            // HRESULT OnUpdateInfo();
            void OnUpdateInfo();

            // HRESULT EnumDisplayAttributeInfo([out] IEnumTfDisplayAttributeInfo **ppEnum);
            void stub_EnumDisplayAttributeInfo();

            // HRESULT GetDisplayAttributeInfo([in] REFGUID guid,
            //                         [out] ITfDisplayAttributeInfo **ppInfo,
            //                         [out] CLSID *pclsidOwner);
            void GetDisplayAttributeInfo(ref Guid guid, out ITfDisplayAttributeInfo info, out Guid clsid);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("70528852-2f26-4aea-8c96-215150578932")]
        public interface ITfDisplayAttributeInfo
        {
            // HRESULT GetGUID([out] GUID *pguid);
            void stub_GetGUID();

            // HRESULT GetDescription([out] BSTR *pbstrDesc);
            void stub_GetDescription();

            // HRESULT GetAttributeInfo([out] TF_DISPLAYATTRIBUTE *pda);
            void GetAttributeInfo(out TF_DISPLAYATTRIBUTE attr);

            // HRESULT SetAttributeInfo([in] const TF_DISPLAYATTRIBUTE *pda);
            void stub_SetAttributeInfo();

            // HRESULT Reset();
            void stub_Reset();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("c3acefb5-f69d-4905-938f-fcadcf4be830")]
        public interface ITfCategoryMgr
        {
            // HRESULT RegisterCategory([in] REFCLSID rclsid,
            //                          [in] REFGUID rcatid,
            //                          [in] REFGUID rguid);
            void stub_RegisterCategory();

            // HRESULT UnregisterCategory([in] REFCLSID rclsid,
            //                            [in] REFGUID rcatid,
            //                            [in] REFGUID rguid);
            void stub_UnregisterCategory();

            // HRESULT EnumCategoriesInItem([in] REFGUID rguid,
            //                              [out] IEnumGUID **ppEnum);
            void stub_EnumCategoriesInItem();

            // HRESULT EnumItemsInCategory([in] REFGUID rcatid,
            //                             [out] IEnumGUID **ppEnum);
            void stub_EnumItemsInCategory();

            // HRESULT FindClosestCategory([in] REFGUID rguid,
            //                             [out] GUID *pcatid,
            //                             [in, size_is(ulCount)] const GUID **ppcatidList,
            //                             [in] ULONG ulCount);
            void stub_FindClosestCategory();

            // HRESULT RegisterGUIDDescription([in] REFCLSID rclsid,
            //                                 [in] REFGUID rguid,
            //                                 [in, size_is(cch)] const WCHAR *pchDesc,
            //                                 [in] ULONG cch);
            void stub_RegisterGUIDDescription();

            // HRESULT UnregisterGUIDDescription([in] REFCLSID rclsid,
            //                                   [in] REFGUID rguid);
            void stub_UnregisterGUIDDescription();

            // HRESULT GetGUIDDescription([in] REFGUID rguid,
            //                            [out] BSTR *pbstrDesc);
            void stub_GetGUIDDescription();

            // HRESULT RegisterGUIDDWORD([in] REFCLSID rclsid,
            //                           [in] REFGUID rguid,
            //                           [in] DWORD dw);
            void stub_RegisterGUIDDWORD();

            // HRESULT UnregisterGUIDDWORD([in] REFCLSID rclsid,
            //                             [in] REFGUID rguid);
            void stub_UnregisterGUIDDWORD();

            // HRESULT GetGUIDDWORD([in] REFGUID rguid,
            //                      [out] DWORD *pdw);
            void stub_GetGUIDDWORD();

            // HRESULT RegisterGUID([in] REFGUID rguid,
            //                      [out] TfGuidAtom *pguidatom);
            void stub_RegisterGUID();

            // HRESULT GetGUID([in] TfGuidAtom guidatom,
            //                 [out] GUID *pguid);
            [PreserveSig]
            int GetGUID(Int32 guidatom, out Guid guid);

            // HRESULT IsEqualTfGuidAtom([in] TfGuidAtom guidatom,
            //                           [in] REFGUID rguid,
            //                           [out] BOOL *pfEqual);
            void stub_IsEqualTfGuidAtom();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e80c-2021-11d2-93e0-0060b067b86e")]
        public interface ITfContextOwner
        {
            // HRESULT GetACPFromPoint([in] const POINT *ptScreen,
            //                         [in] DWORD dwFlags,
            //                         [out] LONG *pacp);
            void GetACPFromPoint(ref POINT point, GetPositionFromPointFlags flags, out int position);

            // HRESULT GetTextExt([in] LONG acpStart,
            //                    [in] LONG acpEnd,
            //                    [out] RECT *prc,
            //                    [out] BOOL *pfClipped);
            void GetTextExt(int start, int end, out RECT rect, [MarshalAs(UnmanagedType.Bool)] out bool clipped);

            // HRESULT GetScreenExt([out] RECT *prc);
            void GetScreenExt(out RECT rect);

            // HRESULT GetStatus([out] TF_STATUS *pdcs);
            void GetStatus(out TS_STATUS status);

            // HRESULT GetWnd([out] HWND *phwnd);
            void GetWnd(out IntPtr hwnd);

            // HRESULT GetAttribute([in] REFGUID rguidAttribute, [out] VARIANT *pvarValue);
            void GetValue(ref Guid guidAttribute, out object varValue);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("a615096f-1c57-4813-8a15-55ee6e5a839c")]
        public interface ITfTransitoryExtensionSink
        {

            // HRESULT OnTransitoryExtensionUpdated([in] ITfContext *pic,
            //                                      [in] TfEditCookie ecReadOnly,
            //                                      [in] ITfRange *pResultRange,
            //                                      [in] ITfRange *pCompositionRange,
            //                                      [out] BOOL *pfDeleteResultRange);
            void OnTransitoryExtensionUpdated(ITfContext context, int ecReadOnly, ITfRange rangeResult, ITfRange rangeComposition, [MarshalAs(UnmanagedType.Bool)] out bool fDeleteResultRange);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("fde1eaee-6924-4cdf-91e7-da38cff5559d")]
        public interface ITfInputScope
        {
            // HRESULT GetInputScopes([out] InputScope **pprgInputScopes,
            //                        [out] UINT *pcCount);
            void GetInputScopes(out IntPtr ppinputscopes, out int count);

            // HRESULT GetPhrase([out] BSTR **ppbstrPhrases,
            //                   [out] UINT *pcCount);
            [PreserveSig]
            int GetPhrase(out IntPtr ppbstrPhrases, out int count);

            // HRESULT GetRegularExpression([out] BSTR *pbstrRegExp);
            [PreserveSig]
            int GetRegularExpression([Out, MarshalAs(UnmanagedType.BStr)] out string desc);

            // HRESULT GetSRGS([out] BSTR *pbstrSRGS);
            [PreserveSig]
            int GetSRGC([Out, MarshalAs(UnmanagedType.BStr)] out string desc);

            // HRESULT GetXML([out] BSTR *pbstrXML);
            [PreserveSig]
            int GetXML([Out, MarshalAs(UnmanagedType.BStr)] out string desc);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3bdd78e2-c16e-47fd-b883-ce6facc1a208")]
        public interface ITfMouseTrackerACP
        {
            // HRESULT AdviseMouseSink([in] ITfRangeACP *range,
            //                         [in] ITfMouseSink *pSink,
            //                         [out] DWORD *pdwCookie);
            [PreserveSig]
            int AdviceMouseSink(ITfRangeACP range, ITfMouseSink sink, out int dwCookie);

            // HRESULT UnadviseMouseSink([in] DWORD dwCookie);
            [PreserveSig]
            int UnadviceMouseSink(int dwCookie);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("a1adaaa2-3a24-449d-ac96-5183e7f5c217")]
        public interface ITfMouseSink
        {
            // HRESULT OnMouseEvent([in] ULONG uEdge,
            //                      [in] ULONG uQuadrant,
            //                      [in] DWORD dwBtnStatus,
            //                      [out] BOOL *pfEaten);

            [PreserveSig]
            int OnMouseEvent(int edge, int quadrant, int btnStatus, [MarshalAs(UnmanagedType.Bool)] out bool eaten);
        }

        #endregion Interfaces
    }
}
