// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;

namespace Microsoft.Test.Loaders 
{

    public static class IENavigationHelper
    {
        private static IntPtr ieHwnd = IntPtr.Zero;

        /// <summary>
        /// Given a valid MainWindowHandle from an IE Process (should work for 6-8), extracts IWebBrowser2 interface and navigates the browser.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="hwnd"></param>
        public static void NavigateInternetExplorer(string url, IntPtr hwnd)
        {
            // Could just create a custom struct to hold a string and IntPtr but this works just as well...
            ieHwnd = hwnd;

            // COM stuff like this has to happen on a STA thread or InvalidCastExceptions occur.
            ParameterizedThreadStart workerThread = new ParameterizedThreadStart(navigateOnSTAThread);
            Thread thread = new Thread(workerThread);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start((object)url);
            thread.Join();
        }


        /// <summary>
        /// Handles Browser Application error messages
        /// </summary>
        /// <param name="topLevelhWnd"></param>
        /// <param name="hwnd"></param>
        /// <param name="process"></param>
        /// <param name="title"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        private static void navigateOnSTAThread(object navigateUrl)
        {
            IWebBrowser2 iWebBrowser2 = IWebBrowser2FromHandle(ieHwnd);
            object empty = null;
            // Can allow more parameters later if needed.
            iWebBrowser2.Navigate((string)navigateUrl, ref empty, ref  empty, ref empty, ref empty);
        }

        #region Private Implementation
        /// <summary>
        /// Callback method for checking whether the Internet Explorer_Server Class has been found
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static bool FindIEServer(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder sb = new StringBuilder(200);
            GetClassName(hWnd, sb, 200);
            if (sb.ToString().Equals("Internet Explorer_Server"))
            {
                // Found it, make a note of its handle
                IEServerHwnd = hWnd;
                return false;
            }
            // Didn't find it
            return true;
        }

        // Stores IE Server instance once found.
        private static IntPtr IEServerHwnd = IntPtr.Zero;

        private static IWebBrowser2 IWebBrowser2FromHandle(IntPtr hWnd)
        {
            IntPtr lRes;

            uint message = RegisterWindowMessage("WM_HTML_GETOBJECT");

            EnumChildWindows(hWnd, FindIEServer, IEServerHwnd);

            if (message != 0)
            {
                IntPtr result = SendMessageTimeout(IEServerHwnd, message, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_BLOCK, 1000, out lRes);

                if (!(lRes == IntPtr.Zero))
                {
                    Guid iid_HTML = IID_IHTMLDocument;
                    Guid iid_IWeb = IID_IWebBrowser2;
                    IENavigationHelper.IHTMLDocument2 theHtmlDocument = (IENavigationHelper.IHTMLDocument2)ObjectFromLresult(lRes, iid_HTML, IntPtr.Zero);

                    if (theHtmlDocument != null)
                    {
                        IServiceProvider serviceProvider = ((IServiceProvider)theHtmlDocument.GetParentWindow());
                        Guid serviceGuid = new Guid(SWebBrowserApp);
                        Guid iid = new Guid(IID_IWebBrowser2.ToString());
                        return (IWebBrowser2)serviceProvider.QueryService(ref serviceGuid, ref iid);
                    }
                }
            }
            return null;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        // Note that some percentage of these imports/pInvokes/Constants are available elsewhere
        // but since this code works with any IE ever, it's not going to get changed
        // and by having its own copy, it's less likely to be accidentally modified / broken.
        #region Native Imports and associated constants

        /// <summary>SID_SWebBrowserApp</summary>
        private const string SWebBrowserApp = "0002DF05-0000-0000-C000-000000000046";


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private static Guid IID_IHTMLDocument = new Guid("626FC520-A41E-11CF-A731-00A0C9082637");
        private static Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");

        [DllImport("oleacc.dll", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object ObjectFromLresult(IntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);

        [Flags]
        private enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        private static extern IntPtr SendMessageTimeout(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        internal interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        private enum OLECMDEXECOPT
        {
            OLECMDEXECOPT_DODEFAULT = 0,
            OLECMDEXECOPT_PROMPTUSER = 1,
            OLECMDEXECOPT_DONTPROMPTUSER = 2,
            OLECMDEXECOPT_SHOWHELP = 3
        }
        private enum OLECMDF
        {
            OLECMDF_SUPPORTED = 1,
            OLECMDF_ENABLED = 2,
            OLECMDF_LATCHED = 4,
            OLECMDF_NINCHED = 8,
            OLECMDF_INVISIBLE = 16,
            OLECMDF_DEFHIDEONCTXTMENU = 32
        }
        private enum OLECMDID
        {
            OLECMDID_OPEN = 1,
            OLECMDID_NEW = 2,
            OLECMDID_SAVE = 3,
            OLECMDID_SAVEAS = 4,
            OLECMDID_SAVECOPYAS = 5,
            OLECMDID_PRINT = 6,
            OLECMDID_PRINTPREVIEW = 7,
            OLECMDID_PAGESETUP = 8,
            OLECMDID_SPELL = 9,
            OLECMDID_PROPERTIES = 10,
            OLECMDID_CUT = 11,
            OLECMDID_COPY = 12,
            OLECMDID_PASTE = 13,
            OLECMDID_PASTESPECIAL = 14,
            OLECMDID_UNDO = 15,
            OLECMDID_REDO = 16,
            OLECMDID_SELECTALL = 17,
            OLECMDID_CLEARSELECTION = 18,
            OLECMDID_ZOOM = 19,
            OLECMDID_GETZOOMRANGE = 20,
            OLECMDID_UPDATECOMMANDS = 21,
            OLECMDID_REFRESH = 22,
            OLECMDID_STOP = 23,
            OLECMDID_HIDETOOLBARS = 24,
            OLECMDID_SETPROGRESSMAX = 25,
            OLECMDID_SETPROGRESSPOS = 26,
            OLECMDID_SETPROGRESSTEXT = 27,
            OLECMDID_SETTITLE = 28,
            OLECMDID_SETDOWNLOADSTATE = 29,
            OLECMDID_STOPDOWNLOAD = 30,
            OLECMDID_ONTOOLBARACTIVATED = 31,
            OLECMDID_FIND = 32,
            OLECMDID_DELETE = 33,
            OLECMDID_HTTPEQUIV = 34,
            OLECMDID_HTTPEQUIV_DONE = 35,
            OLECMDID_ENABLE_INTERACTION = 36,
            OLECMDID_ONUNLOAD = 37,
            OLECMDID_PROPERTYBAG2 = 38,
            OLECMDID_PREREFRESH = 39,
            OLECMDID_SHOWSCRIPTERROR = 40,
            OLECMDID_SHOWMESSAGE = 41,
            OLECMDID_SHOWFIND = 42,
            OLECMDID_SHOWPAGESETUP = 43,
            OLECMDID_SHOWPRINT = 44,
            OLECMDID_CLOSE = 45,
            OLECMDID_ALLOWUILESSSAVEAS = 46,
            OLECMDID_DONTDOWNLOADCSS = 47,
            OLECMDID_UPDATEPAGESTATUS = 48,
            OLECMDID_PRINT2 = 49,
            OLECMDID_PRINTPREVIEW2 = 50,
            OLECMDID_SETPRINTTEMPLATE = 51,
            OLECMDID_GETPRINTTEMPLATE = 52,
            OLECMDID_PAGEACTIONBLOCKED = 55,
            OLECMDID_PAGEACTIONUIQUERY = 56,
            OLECMDID_FOCUSVIEWCONTROLS = 57,
            OLECMDID_FOCUSVIEWCONTROLSQUERY = 58,
            OLECMDID_SHOWPAGEACTIONMENU = 59
        }

        [ComImport, DefaultMember("Name"), Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch), SuppressUnmanagedCodeSecurity]
        private interface IWebBrowser2
        {
            [DispId(100)]
            void GoBack();
            [DispId(0x65)]
            void GoForward();
            [DispId(0x66)]
            void GoHome();
            [DispId(0x67)]
            void GoSearch();
            [DispId(0x68)]
            void Navigate([MarshalAs(UnmanagedType.BStr)] string URL, [In] ref object Flags, [In] ref object TargetFrameName, [In] ref object PostData, [In] ref object Headers);
            [DispId(-550)]
            void Refresh();
            [DispId(0x69)]
            void Refresh2([In] ref object Level);
            [DispId(0x6a)]
            void Stop();
            [DispId(300)]
            void Quit();
            [DispId(0x12d)]
            void ClientToWindow([In, Out] ref int pcx, [In, Out] ref int pcy);
            [DispId(0x12e)]
            void PutProperty([MarshalAs(UnmanagedType.BStr)] string Property, object vtValue);
            [DispId(0x12f)]
            object GetProperty([MarshalAs(UnmanagedType.BStr)] string Property);
            [DispId(500)]
            void Navigate2([In] ref object URL, [In] ref object Flags, [In] ref object TargetFrameName, [In] ref object PostData, [In] ref object Headers);
            [DispId(0x1f5)]
            OLECMDF QueryStatusWB(OLECMDID cmdID);
            [DispId(0x1f6)]
            void ExecWB(OLECMDID cmdID, OLECMDEXECOPT cmdexecopt, [In] ref object pvaIn, [In, Out] ref object pvaOut);
            [DispId(0x1f7)]
            void ShowBrowserBar([In] ref object pvaClsid, [In] ref object pvarShow, [In] ref object pvarSize);
            bool AddressBar { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x22b)] get; [DispId(0x22b)] set; }
            object Application { [return: MarshalAs(UnmanagedType.IDispatch)] [DispId(200)] get; }
            bool Busy { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0xd4)] get; }
            object Container { [return: MarshalAs(UnmanagedType.IDispatch)] [DispId(0xca)] get; }
            object Document { [return: MarshalAs(UnmanagedType.IDispatch)] [DispId(0xcb)] get; }
            string FullName { [return: MarshalAs(UnmanagedType.BStr)] [DispId(400)] get; }
            bool FullScreen { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x197)] get; [DispId(0x197)] set; }
            int Height { [DispId(0xd1)] get; [DispId(0xd1)] set; }
            int HWND { [DispId(-515)] get; }
            int Left { [DispId(0xce)] get; [DispId(0xce)] set; }
            string LocationName { [return: MarshalAs(UnmanagedType.BStr)] [DispId(210)] get; }
            string LocationURL { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0xd3)] get; }
            bool MenuBar { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x196)] get; [DispId(0x196)] set; }
            string Name { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0)] get; }
            bool Offline { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(550)] get; [DispId(550)] set; }
            object Parent { [return: MarshalAs(UnmanagedType.IDispatch)] [DispId(0xc9)] get; }
            string Path { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x191)] get; }
            object ReadyState { [DispId(-525)] get; }
            bool RegisterAsBrowser { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x228)] get; [DispId(0x228)] set; }
            bool RegisterAsDropTarget { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x229)] get; [DispId(0x229)] set; }
            bool Resizable { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x22c)] get; [DispId(0x22c)] set; }
            bool Silent { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x227)] get; [DispId(0x227)] set; }
            bool StatusBar { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x193)] get; [DispId(0x193)] set; }
            string StatusText { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0x194)] get; [DispId(0x194)] set; }
            bool TheaterMode { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x22a)] get; [DispId(0x22a)] set; }
            int ToolBar { [DispId(0x195)] get; [DispId(0x195)] set; }
            int Top { [DispId(0xcf)] get; [DispId(0xcf)] set; }
            bool TopLevelContainer { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0xcc)] get; }
            string Type { [return: MarshalAs(UnmanagedType.BStr)] [DispId(0xcd)] get; }
            bool Visible { [return: MarshalAs(UnmanagedType.VariantBool)] [DispId(0x192)] get; [DispId(0x192)] set; }
            int Width { [DispId(0xd0)] get; [DispId(0xd0)] set; }
        }

        [ComImport, Guid("332C4425-26CB-11D0-B483-00C04FD90119"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        private interface IHTMLDocument2
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetScript();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetAll();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetBody();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetActiveElement();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetImages();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetApplets();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetLinks();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetForms();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetAnchors();

            void SetTitle(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetTitle();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetScripts();

            void SetDesignMode(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetDesignMode();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetSelection();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetReadyState();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetFrames();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetEmbeds();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetPlugins();

            void SetAlinkColor(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetAlinkColor();

            void SetBgColor(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetBgColor();

            void SetFgColor(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetFgColor();

            void SetLinkColor(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetLinkColor();

            void SetVlinkColor(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetVlinkColor();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetReferrer();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetLocation();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetLastModified();

            void SetURL(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetURL();

            void SetDomain(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetDomain();

            void SetCookie(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetCookie();

            void SetExpando(bool p);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool GetExpando();

            void SetCharset(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetCharset();

            void SetDefaultCharset(string p);

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetDefaultCharset();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetMimeType();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetFileSize();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetFileCreatedDate();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetFileModifiedDate();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetFileUpdatedDate();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetSecurity();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetProtocol();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetNameProp();

            void DummyWrite(int psarray);

            void DummyWriteln(int psarray);

            [return: MarshalAs(UnmanagedType.Interface)]
            object Open(string URL, object name, object features, object replace);

            void Close();

            void Clear();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryCommandSupported(string cmdID);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryCommandEnabled(string cmdID);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryCommandState(string cmdID);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryCommandIndeterm(string cmdID);

            [return: MarshalAs(UnmanagedType.BStr)]
            string QueryCommandText(string cmdID);

            [return: MarshalAs(UnmanagedType.Struct)]
            object QueryCommandValue(string cmdID);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool ExecCommand(string cmdID, bool showUI, object value);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool ExecCommandShowHelp(string cmdID);

            [return: MarshalAs(UnmanagedType.Interface)]
            object CreateElement(string eTag);

            void SetOnhelp(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnhelp();

            void SetOnclick(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnclick();

            void SetOndblclick(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOndblclick();

            void SetOnkeyup(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnkeyup();

            void SetOnkeydown(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnkeydown();

            void SetOnkeypress(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnkeypress();

            void SetOnmouseup(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnmouseup();

            void SetOnmousedown(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnmousedown();

            void SetOnmousemove(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnmousemove();

            void SetOnmouseout(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnmouseout();

            void SetOnmouseover(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnmouseover();

            void SetOnreadystatechange(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnreadystatechange();

            void SetOnafterupdate(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnafterupdate();

            void SetOnrowexit(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnrowexit();

            void SetOnrowenter(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnrowenter();

            void SetOndragstart(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOndragstart();

            void SetOnselectstart(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnselectstart();

            [return: MarshalAs(UnmanagedType.Interface)]
            object ElementFromPoint(int x, int y);

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetParentWindow();

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetStyleSheets();

            void SetOnbeforeupdate(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnbeforeupdate();

            void SetOnerrorupdate(object p);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOnerrorupdate();

            [return: MarshalAs(UnmanagedType.BStr)]
            string toString();

            [return: MarshalAs(UnmanagedType.Interface)]
            object CreateStyleSheet(string bstrHref, int lIndex);
        }

        #endregion

    }
}
