// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Collections;
using Microsoft.Win32;

namespace Microsoft.Test.Utilities
{
    internal class Win32Helper
    {

        /// <summary>
        /// Get the hwnd for IE's ErrorPage (when Avalon dies)
        /// </summary>
        /// <returns>null if it didn't work</returns>
        internal static IntPtr GetErrorPageHwnd()
        {
            int ieVer = IEHelper.GetIEVersion();

            //unfortunately FindWindow only finds one level down at best, so we have to do one level at a time
            IntPtr hwnd = FindWindowEx(IntPtr.Zero, 0, "IEFrame", null);

            if (ieVer > 7) //need to account for an extra frame class
            {
                hwnd = FindWindowEx(hwnd, 0, "Frame Tab", null);
            }
            if (ieVer > 6) //need to account for tabs
            {
                hwnd = FindWindowEx(hwnd, 0, "TabWindowClass", null);
            }
            hwnd = FindWindowEx(hwnd, 0, "Shell DocObject View", null);
            hwnd = FindWindowEx(hwnd, 0, "DocObject_Top_Class", null);
            hwnd = FindWindowEx(hwnd, 0, "ErrorPage", null);
            hwnd = FindWindowEx(hwnd, 0, "Internet Explorer_Server", null);
            return hwnd;
        }

        /// <summary>
        /// Get current foreground's window text (~current app user is interacting with)
        /// </summary>
        /// <returns></returns>
        internal static string GetTopWindowText()
        {
            // get focused window
            IntPtr focused = GetForegroundWindow();
            ILog log = LogFactory.Create();
            log.StatusMessage = "GetForegroundWindow:" + focused.ToInt32().ToString();

            // get the top level window that's a parent of the focused one
            IntPtr top = GetTopWindow(focused);
            log.StatusMessage = "GetTopWindow:" + top.ToInt32().ToString();

            // get text
            const int count = 256;//maxpath
            StringBuilder text = new StringBuilder(count);
            int result = GetWindowText(top, text, count);
            if (result != 0)
            {
                // return it
                return (text.ToString());
            }

            // not found
            return (null);
        }

        /// <summary>
        /// IsWow64Process
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        internal static bool IsWow64Process(string processName)
        {
            Process[] pList = Process.GetProcessesByName(processName);
            if (pList.Length != 1)
            {
                throw (new ArgumentException("'" + processName + "' has #" + pList.Length + " instances - This call requires only one."));
            }
            Process p = pList[0];
            bool isWow64 = false;
            if (Win32Helper.IsWow64Process(p.Handle, ref isWow64) == 0)
            {
                throw (new Exception("IsWow64Process call failed"));
            }
            return (isWow64);
        }

        /// <summary>
        /// IsFunctionDefined
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        internal static bool IsFunctionDefined(string moduleName, string function)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                h = LoadLibrary(moduleName);
                if (h == IntPtr.Zero)
                {
                    return (false);
                }

                return (GetProcAddress(h, function) != IntPtr.Zero);
            }
            finally
            {
                if (h != IntPtr.Zero)
                {
                    FreeLibrary(h);
                }
            }
        }

        /// <summary>
        /// EnumChildrenCallback
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        internal delegate bool EnumChildrenCallback(IntPtr hwnd, IntPtr lParam);

        /// <summary>
        /// GetForegroundWindow
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// EnumChildWindows
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lpEnumFunc"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern bool EnumChildWindows(IntPtr hwnd, EnumChildrenCallback lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// GetWindowText
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="text"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder text, int count);

        /// <summary>
        /// GetWindowText
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        internal static string GetWindowText(IntPtr hWnd)
        {
            // get text
            const int count = 256;//maxpath
            StringBuilder text = new StringBuilder(count);
            int result = GetWindowText(hWnd, text, count);
            if (result != 0)
            {
                // return it
                return (text.ToString());
            }

            // not found
            return (null);
        }

        /// <summary>
        /// FindResource
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="sFilename"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        internal static extern IntPtr FindResource(IntPtr mod, String sFilename, int type);

        /// <summary>
        /// GetClassName
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="text"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetClassName(IntPtr hWnd, [Out] StringBuilder text, int count);

        /// <summary>
        /// GetFocus
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetFocus();

        /// <summary>
        /// GetModuleHandle
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(String moduleName);

        /// <summary>
        /// GetTopWindow
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetTopWindow(IntPtr hWnd);

        /// <summary>
        /// LoadLibrary
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadLibrary(String lpFileName);

        /// <summary>
        /// FreeLibrary
        /// </summary>
        /// <param name="hModule"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// GetProcAddress
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="lpProcName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// LoadLibraryEx
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="reservedHandle"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport("Kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadLibraryEx(string fileName, IntPtr reservedHandle, int flags);

        /// <summary>
        /// LoadResource
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="hresinfo"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr LoadResource(IntPtr mod, IntPtr hresinfo);

        /// <summary>
        /// LoadString
        /// </summary>
        /// <param name="moduleHandle"></param>
        /// <param name="stringId"></param>
        /// <param name="outBuffer"></param>
        /// <param name="bufferMaximumSize"></param>
        /// <returns></returns>
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int LoadString(IntPtr moduleHandle, int stringId, StringBuilder outBuffer, int bufferMaximumSize);

        /// <summary>
        /// LockResource
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr LockResource(IntPtr h);

        /// <summary>
        /// SizeofResource
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="hResInfo"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

        /// <summary>
        /// LoadKeyboardLayout
        /// </summary>
        /// <param name="pwszKLID"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadKeyboardLayout(string pwszKLID, int flags);

        /// <summary>
        /// ActivateKeyboardLayout
        /// </summary>
        /// <param name="hkl"></param>
        /// <param name="uFlags"></param>
        /// <returns></returns>
        [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, int uFlags);

        /// <summary>
        /// IsWow64Process
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="isWow64"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern int IsWow64Process(IntPtr handle, ref bool isWow64);

        /// <summary>
        /// IsChild
        /// </summary>
        /// <param name="hWndParent"></param>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool IsChild(IntPtr hWndParent, IntPtr hwnd);

        /// <summary>
        /// GetDesktopWindow
        /// </summary>
        /// <returns></returns>
        [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// RegDisableReflectionKey
        /// </summary>
        /// <param name="hBase"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int RegDisableReflectionKey(IntPtr hBase);

        /// <summary>
        /// RegEnableReflectionKey
        /// </summary>
        /// <param name="hBase"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int RegEnableReflectionKey(IntPtr hBase);

        /// <summary>
        /// RegOpenKeyEx
        /// </summary>
        /// <param name="hKey"></param>
        /// <param name="lpSubKey"></param>
        /// <param name="ulOptions"></param>
        /// <param name="samDesired"></param>
        /// <param name="phkResult"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        internal static extern Int32 RegOpenKeyEx(IntPtr hKey, String lpSubKey, Int32 ulOptions, Int32 samDesired, out IntPtr phkResult);

        /// <summary>
        /// RegCloseKey
        /// </summary>
        /// <param name="hKey"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll")]
        internal static extern Int32 RegCloseKey(IntPtr hKey);

        /// <summary>
        /// RegisterWindowMessage
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int RegisterWindowMessage(string msg);

        /// <summary>
        /// SendMessageTimeout
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="flags"></param>
        /// <param name="timeout"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int SendMessageTimeout(IntPtr hwnd, int msg, int wParam, int lParam,
            int flags, int timeout, out int result);

        /// <summary>
        /// ObjectFromLresult
        /// </summary>
        /// <param name="lResult"></param>
        /// <param name="riid"></param>
        /// <param name="wParam"></param>
        /// <param name="ppvObject"></param>
        /// <returns></returns>
        //[DllImport("oleacc.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        //public static extern int ObjectFromLresult(int lResult, ref Guid riid, int wParam, out HTMLDocument ppvObject);

        /// <summary>
        /// FindWindow
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="rel"></param>
        /// <param name="clsName"></param>
        /// <param name="winName"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern IntPtr FindWindowEx(IntPtr hwnd, int rel, string clsName, string winName);

        /// <summary>
        /// FindWindow
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="uCmd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern IntPtr GetWindow(IntPtr hwnd, int uCmd);


        /// <summary>
        /// Constants
        /// </summary>
        internal class Constants
        {
            internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        }
    }

    /// <summary>
    /// Win32Error
    /// </summary>
    internal struct Win32Error
    {
        internal const int KEY_ALL_ACCESS = 0xF003F;
        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_FILE_NOT_FOUND = 2;
    }
    //  [ComVisible(true), ComImport(), Guid("25336920-03F9-11CF-8FD0-00AA00686F13")]
    //  internal class HTMLDocument
    //  {

    //  }

    //  [ComVisible(true), Guid("626FC520-A41E-11CF-A731-00A0C9082637"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    //  internal interface IHTMLDocument
    //  {

    //      [return: MarshalAs(UnmanagedType.Interface)]
    //      object GetScript();
    //  }

    //  [ComVisible(true), Guid("3050f485-98b5-11cf-bb82-00aa00bdce0b"),
    //  InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual),
    //  TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)]
    //  internal interface IHTMLDocument3
    //  {
    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_RELEASECAPTURE)]
    //      void releaseCapture();

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_RECALC)]
    //      void recalc([In, MarshalAs(UnmanagedType.VariantBool)] bool fForce);

    //      void temp1();
    //      //removing to avoid bringing in too much unneeded stuff
    //      //[DispId(dispids.DISPID_IHTMLDOCUMENT3_CREATETEXTNODE)]
    //      //IHTMLDOMNode createTextNode([In] String text);

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_DOCUMENTELEMENT)]
    //      IHTMLElement documentElement();

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_UNIQUEID),
    //         TypeLibFunc(TypeLibFuncFlags.FHidden)]
    //      String uniqueID();

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ATTACHEVENT)]
    //      bool attachEvent([In] String sEvent, [In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_DETACHEVENT)]
    //      void detachEvent([In] String sEvent, [In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONROWSDELETE)]
    //      object onrowsdelete { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONROWSINSERTED)]
    //      object onrowsinserted { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONCELLCHANGE)]
    //      object oncellchange { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONDATASETCHANGED)]
    //      object ondatasetchanged { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONDATAAVAILABLE)]
    //      object ondataavailable { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONDATASETCOMPLETE)]
    //      object ondatasetcomplete { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONPROPERTYCHANGE)]
    //      object onpropertychange { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_DIR)]
    //      object dir { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONCONTEXTMENU)]
    //      object oncontextmenu { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONSTOP)]
    //      object onstop { set; get; }

    //      void temp2();
    //      //removing to avoid bringing in too much unneeded stuff
    //      //[DispId(dispids.DISPID_IHTMLDOCUMENT3_CREATEDOCUMENTFRAGMENT)]
    //      //IHTMLDocument2 createDocumentFragment();

    //      void temp3();
    //      //removing to avoid bringing in too much unneeded stuff
    //      //[DispId(dispids.DISPID_IHTMLDOCUMENT3_PARENTDOCUMENT)] //hidden, restricted
    //      //IHTMLDocument2 parentDocument();

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ENABLEDOWNLOAD)] //hidden, restricted
    //      bool enableDownload { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_BASEURL)] //hidden, restricted
    //      String baseUrl { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_CHILDNODES)]
    //      object childNodes(); //IDispatch retval

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_INHERITSTYLESHEETS)] //hidden,restricted
    //      bool inheritStyleSheets { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_ONBEFOREEDITFOCUS)]
    //      object onbeforeeditfocus { set; get; }

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_GETELEMENTSBYNAME)]
    //      IHTMLElementCollection getElementsByName([In] String v);

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_GETELEMENTBYID)]
    //      IHTMLElement getElementById([In] String v);

    //      [DispId(dispids.DISPID_IHTMLDOCUMENT3_GETELEMENTSBYTAGNAME)]
    //      IHTMLElementCollection getElementsByTagName([In] String v);

    //  }

    //  [ComImport, ComVisible(true), Guid("3050F1FF-98B5-11CF-BB82-00AA00BDCE0B"),
    //  InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual),
    //  TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)]
    //  internal interface IHTMLElement
    //  {
    //      [DispId(dispids.DISPID_IHTMLELEMENT_SETATTRIBUTE)]
    //      void SetAttribute(
    //           [In, MarshalAs(UnmanagedType.BStr)]
    //string strAttributeName,
    //           [In]
    //Object AttributeValue,
    //           [In, MarshalAs(UnmanagedType.I4)]
    //int lFlags);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_GETATTRIBUTE)]
    //      object GetAttribute(
    //          [In, MarshalAs(UnmanagedType.BStr)]
    //string strAttributeName,
    //          [In, MarshalAs(UnmanagedType.I4)]
    //int lFlags);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_REMOVEATTRIBUTE)]
    //      [return: MarshalAs(UnmanagedType.Bool)]
    //      bool RemoveAttribute(
    //          [In, MarshalAs(UnmanagedType.BStr)]
    //string strAttributeName,
    //          [In, MarshalAs(UnmanagedType.I4)]
    //int lFlags);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_CLASSNAME)]
    //      string className { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ID)]
    //      string id { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_TAGNAME)]
    //      string tagName { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_PARENTELEMENT)]
    //      IHTMLElement parentElement { [return: MarshalAs(UnmanagedType.Interface)] get; }

    //      void temp3();
    //      //[DispId(dispids.DISPID_IHTMLELEMENT_STYLE)]
    //      //IHTMLStyle style { [return: MarshalAs(UnmanagedType.Interface)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONHELP)]
    //      object onhelp { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONCLICK)]
    //      object onclick { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONDBLCLICK)]
    //      object ondblclick { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONKEYDOWN)]
    //      object onkeydown { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONKEYUP)]
    //      object onkeyup { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONKEYPRESS)]
    //      object onkeypress { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONMOUSEOUT)]
    //      object onmouseout { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONMOUSEOVER)]
    //      object onmouseover { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONMOUSEMOVE)]
    //      object onmousemove { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONMOUSEDOWN)]
    //      object onmousedown { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONMOUSEUP)]
    //      object onmouseup { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_DOCUMENT)]
    //      object document { [return: MarshalAs(UnmanagedType.IDispatch)]get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_TITLE)]
    //      string title { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_LANGUAGE)]
    //      string language { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONSELECTSTART)]
    //      object onselectstart { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_SCROLLINTOVIEW)]
    //      void scrollIntoView([Optional, In] object varargStart);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_CONTAINS)]
    //      [return: MarshalAs(UnmanagedType.VariantBool)]
    //      bool Contains(
    //           [In, MarshalAs(UnmanagedType.Interface)]
    //IHTMLElement pChild);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_SOURCEINDEX)]
    //      int sourceIndex { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_RECORDNUMBER)]
    //      object recordNumber { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_LANG)]
    //      string lang { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OFFSETLEFT)]
    //      int offsetLeft { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OFFSETTOP)]
    //      int offsetTop { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OFFSETWIDTH)]
    //      int offsetWidth { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OFFSETHEIGHT)]
    //      int offsetHeight { get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OFFSETPARENT)]
    //      IHTMLElement offsetParent { [return: MarshalAs(UnmanagedType.Interface)]get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_INNERHTML)]
    //      string innerHTML { [param: MarshalAs(UnmanagedType.BStr)] set; [return: MarshalAs(UnmanagedType.BStr)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_INNERTEXT)]
    //      string innerText { [param: MarshalAs(UnmanagedType.BStr)] set; [return: MarshalAs(UnmanagedType.BStr)] get; }


    //      [DispId(dispids.DISPID_IHTMLELEMENT_OUTERHTML)]
    //      string outerHTML { [param: MarshalAs(UnmanagedType.BStr)] set; [return: MarshalAs(UnmanagedType.BStr)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_OUTERTEXT)]
    //      string outerText { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_INSERTADJACENTHTML)]
    //      void insertAdjacentHTML([In, MarshalAs(UnmanagedType.BStr)] string where, [In, MarshalAs(UnmanagedType.BStr)] string html);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_INSERTADJACENTTEXT)]
    //      void insertAdjacentText([In, MarshalAs(UnmanagedType.BStr)] string where, [In, MarshalAs(UnmanagedType.BStr)] string text);

    //      [DispId(dispids.DISPID_IHTMLELEMENT_PARENTTEXTEDIT)]
    //      IHTMLElement parentTextEdit { [return: MarshalAs(UnmanagedType.Interface)]get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ISTEXTEDIT)]
    //      bool isTextEdit { [return: MarshalAs(UnmanagedType.Bool)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_CLICK)]
    //      void Click();

    //      [DispId(dispids.DISPID_IHTMLELEMENT_FILTERS)]
    //      object filters { [return: MarshalAs(UnmanagedType.Interface)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONDRAGSTART)]
    //      object ondragstart { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_TOSTRING)]
    //      [return: MarshalAs(UnmanagedType.BStr)]
    //      string toString();

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONBEFOREUPDATE)]
    //      object onbeforeupdate { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONAFTERUPDATE)]
    //      object onafterupdate { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONERRORUPDATE)]
    //      object onerrorupdate { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONROWEXIT)]
    //      object onrowexit { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONROWENTER)]
    //      object onrowenter { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONDATASETCHANGED)]
    //      object ondatasetchanged { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONDATAAVAILABLE)]
    //      object ondataavailable { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONDATASETCOMPLETE)]
    //      object ondatasetcomplete { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ONFILTERCHANGE)]
    //      object onfilterchange { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_CHILDREN)]
    //      object children { [return: MarshalAs(UnmanagedType.IDispatch)] get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENT_ALL)]
    //      object all { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
    //  }

    //  [ComImport, ComVisible(true), Guid("3050F21F-98B5-11CF-BB82-00AA00BDCE0B"),
    //  InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual),
    //  TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)]


    //  internal interface IHTMLElementCollection : IEnumerable
    //  {
    //      [DispId(dispids.DISPID_IHTMLELEMENTCOLLECTION_TOSTRING)]
    //      [return: MarshalAs(UnmanagedType.BStr)]
    //      string toString();

    //      [DispId(dispids.DISPID_IHTMLELEMENTCOLLECTION_LENGTH)]
    //      int length { set; get; }

    //      [DispId(dispids.DISPID_IHTMLELEMENTCOLLECTION__NEWENUM),
    //      TypeLibFuncAttribute(TypeLibFuncFlags.FRestricted),
    //      MethodImpl(MethodImplOptions.InternalCall,
    //          MethodCodeType = MethodCodeType.Runtime)]
    //      [return: MarshalAs(UnmanagedType.CustomMarshaler,
    //          MarshalTypeRef = typeof(EnumeratorToEnumVariantMarshaler))]
    //      new IEnumerator GetEnumerator();

    //      [return: MarshalAs(UnmanagedType.Interface)]
    //      object Item(
    //          [In, MarshalAs(UnmanagedType.Struct)]
    //object name,
    //          [In, MarshalAs(UnmanagedType.Struct)]
    //object index);

    //      [DispId(dispids.DISPID_IHTMLELEMENTCOLLECTION_ITEM)]
    //      [return: MarshalAs(UnmanagedType.IDispatch)]
    //      object item([Optional, In] object name, [Optional, In] object index);

    //      [DispId(dispids.DISPID_IHTMLELEMENTCOLLECTION_TAGS)]
    //      [return: MarshalAs(UnmanagedType.IDispatch)]
    //      object tags([In] object tagName);
    //  }

    class dispids
    {
        //useful DISPIDs
        public const int DISPID_UNKNOWN = -1;

        public const int DISPID_XOBJ_MIN = -2147418111;

        public const int DISPID_XOBJ_MAX = -2147352577;
        public const int DISPID_XOBJ_BASE = DISPID_XOBJ_MIN;
        public const int DISPID_HTMLOBJECT = (DISPID_XOBJ_BASE + 500);
        public const int DISPID_ELEMENT = (DISPID_HTMLOBJECT + 500);
        public const int DISPID_SITE = (DISPID_ELEMENT + 1000);
        public const int DISPID_OBJECT = (DISPID_SITE + 1000);
        public const int DISPID_STYLE = (DISPID_OBJECT + 1000);
        public const int DISPID_ATTRS = (DISPID_STYLE + 1000);
        public const int DISPID_EVENTS = (DISPID_ATTRS + 1000);
        public const int DISPID_XOBJ_EXPANDO = (DISPID_EVENTS + 1000);
        public const int DISPID_XOBJ_ORDINAL = (DISPID_XOBJ_EXPANDO + 1000);

        public const int DISPID_AMBIENT_DLCONTROL = -5512;

        public const int STDDISPID_XOBJ_ONBLUR = (DISPID_XOBJ_BASE);
        public const int STDDISPID_XOBJ_ONFOCUS = (DISPID_XOBJ_BASE + 1);
        public const int STDDISPID_XOBJ_BEFOREUPDATE = (DISPID_XOBJ_BASE + 4);
        public const int STDDISPID_XOBJ_AFTERUPDATE = (DISPID_XOBJ_BASE + 5);
        public const int STDDISPID_XOBJ_ONROWEXIT = (DISPID_XOBJ_BASE + 6);
        public const int STDDISPID_XOBJ_ONROWENTER = (DISPID_XOBJ_BASE + 7);
        public const int STDDISPID_XOBJ_ONMOUSEOVER = (DISPID_XOBJ_BASE + 8);
        public const int STDDISPID_XOBJ_ONMOUSEOUT = (DISPID_XOBJ_BASE + 9);
        public const int STDDISPID_XOBJ_ONHELP = (DISPID_XOBJ_BASE + 10);
        public const int STDDISPID_XOBJ_ONDRAGSTART = (DISPID_XOBJ_BASE + 11);
        public const int STDDISPID_XOBJ_ONSELECTSTART = (DISPID_XOBJ_BASE + 12);
        public const int STDDISPID_XOBJ_ERRORUPDATE = (DISPID_XOBJ_BASE + 13);
        public const int STDDISPID_XOBJ_ONDATASETCHANGED = (DISPID_XOBJ_BASE + 14);
        public const int STDDISPID_XOBJ_ONDATAAVAILABLE = (DISPID_XOBJ_BASE + 15);
        public const int STDDISPID_XOBJ_ONDATASETCOMPLETE = (DISPID_XOBJ_BASE + 16);
        public const int STDDISPID_XOBJ_ONFILTER = (DISPID_XOBJ_BASE + 17);
        public const int STDDISPID_XOBJ_ONLOSECAPTURE = (DISPID_XOBJ_BASE + 18);
        public const int STDDISPID_XOBJ_ONPROPERTYCHANGE = (DISPID_XOBJ_BASE + 19);
        public const int STDDISPID_XOBJ_ONDRAG = (DISPID_XOBJ_BASE + 20);
        public const int STDDISPID_XOBJ_ONDRAGEND = (DISPID_XOBJ_BASE + 21);
        public const int STDDISPID_XOBJ_ONDRAGENTER = (DISPID_XOBJ_BASE + 22);
        public const int STDDISPID_XOBJ_ONDRAGOVER = (DISPID_XOBJ_BASE + 23);
        public const int STDDISPID_XOBJ_ONDRAGLEAVE = (DISPID_XOBJ_BASE + 24);
        public const int STDDISPID_XOBJ_ONDROP = (DISPID_XOBJ_BASE + 25);
        public const int STDDISPID_XOBJ_ONCUT = (DISPID_XOBJ_BASE + 26);
        public const int STDDISPID_XOBJ_ONCOPY = (DISPID_XOBJ_BASE + 27);
        public const int STDDISPID_XOBJ_ONPASTE = (DISPID_XOBJ_BASE + 28);
        public const int STDDISPID_XOBJ_ONBEFORECUT = (DISPID_XOBJ_BASE + 29);
        public const int STDDISPID_XOBJ_ONBEFORECOPY = (DISPID_XOBJ_BASE + 30);
        public const int STDDISPID_XOBJ_ONBEFOREPASTE = (DISPID_XOBJ_BASE + 31);
        public const int STDDISPID_XOBJ_ONROWSDELETE = (DISPID_XOBJ_BASE + 32);
        public const int STDDISPID_XOBJ_ONROWSINSERTED = (DISPID_XOBJ_BASE + 33);
        public const int STDDISPID_XOBJ_ONCELLCHANGE = (DISPID_XOBJ_BASE + 34);

        public const int DISPID_CLICK = (-600);
        public const int DISPID_DBLCLICK = (-601);
        public const int DISPID_KEYDOWN = (-602);
        public const int DISPID_KEYPRESS = (-603);
        public const int DISPID_KEYUP = (-604);
        public const int DISPID_MOUSEDOWN = (-605);
        public const int DISPID_MOUSEMOVE = (-606);
        public const int DISPID_MOUSEUP = (-607);
        public const int DISPID_ERROREVENT = (-608);
        public const int DISPID_READYSTATECHANGE = (-609);
        public const int DISPID_CLICK_VALUE = (-610);
        public const int DISPID_RIGHTTOLEFT = (-611);
        public const int DISPID_TOPTOBOTTOM = (-612);
        public const int DISPID_THIS = (-613);

        //  Standard dispatch ID constants
        public const int DISPID_AUTOSIZE = (-500);
        public const int DISPID_BACKCOLOR = (-501);
        public const int DISPID_BACKSTYLE = (-502);
        public const int DISPID_BORDERCOLOR = (-503);
        public const int DISPID_BORDERSTYLE = (-504);
        public const int DISPID_BORDERWIDTH = (-505);
        public const int DISPID_DRAWMODE = (-507);
        public const int DISPID_DRAWSTYLE = (-508);
        public const int DISPID_DRAWWIDTH = (-509);
        public const int DISPID_FILLCOLOR = (-510);
        public const int DISPID_FILLSTYLE = (-511);
        public const int DISPID_FONT = (-512);
        public const int DISPID_FORECOLOR = (-513);
        public const int DISPID_ENABLED = (-514);
        public const int DISPID_HWND = (-515);
        public const int DISPID_TABSTOP = (-516);
        public const int DISPID_TEXT = (-517);
        public const int DISPID_CAPTION = (-518);
        public const int DISPID_BORDERVISIBLE = (-519);
        public const int DISPID_APPEARANCE = (-520);
        public const int DISPID_MOUSEPOINTER = (-521);
        public const int DISPID_MOUSEICON = (-522);
        public const int DISPID_PICTURE = (-523);
        public const int DISPID_VALID = (-524);
        public const int DISPID_READYSTATE = (-525);
        public const int DISPID_LISTINDEX = (-526);
        public const int DISPID_SELECTED = (-527);
        public const int DISPID_LIST = (-528);
        public const int DISPID_COLUMN = (-529);
        public const int DISPID_LISTCOUNT = (-531);
        public const int DISPID_MULTISELECT = (-532);
        public const int DISPID_MAXLENGTH = (-533);
        public const int DISPID_PASSWORDCHAR = (-534);
        public const int DISPID_SCROLLBARS = (-535);
        public const int DISPID_WORDWRAP = (-536);
        public const int DISPID_MULTILINE = (-537);
        public const int DISPID_NUMBEROFROWS = (-538);
        public const int DISPID_NUMBEROFCOLUMNS = (-539);
        public const int DISPID_DISPLAYSTYLE = (-540);
        public const int DISPID_GROUPNAME = (-541);
        public const int DISPID_IMEMODE = (-542);
        public const int DISPID_ACCELERATOR = (-543);
        public const int DISPID_ENTERKEYBEHAVIOR = (-544);
        public const int DISPID_TABKEYBEHAVIOR = (-545);
        public const int DISPID_SELTEXT = (-546);
        public const int DISPID_SELSTART = (-547);
        public const int DISPID_SELLENGTH = (-548);

        public const int DISPID_REFRESH = (-550);
        public const int DISPID_DOCLICK = (-551);
        public const int DISPID_ABOUTBOX = (-552);
        public const int DISPID_ADDITEM = (-553);
        public const int DISPID_CLEAR = (-554);
        public const int DISPID_REMOVEITEM = (-555);
        public const int DISPID_NORMAL_FIRST = 1000;

        public const int DISPID_ONABORT = (DISPID_NORMAL_FIRST);
        public const int DISPID_ONCHANGE = (DISPID_NORMAL_FIRST + 1);
        public const int DISPID_ONERROR = (DISPID_NORMAL_FIRST + 2);
        public const int DISPID_ONLOAD = (DISPID_NORMAL_FIRST + 3);
        public const int DISPID_ONSELECT = (DISPID_NORMAL_FIRST + 6);
        public const int DISPID_ONSUBMIT = (DISPID_NORMAL_FIRST + 7);
        public const int DISPID_ONUNLOAD = (DISPID_NORMAL_FIRST + 8);
        public const int DISPID_ONBOUNCE = (DISPID_NORMAL_FIRST + 9);
        public const int DISPID_ONFINISH = (DISPID_NORMAL_FIRST + 10);
        public const int DISPID_ONSTART = (DISPID_NORMAL_FIRST + 11);
        public const int DISPID_ONLAYOUT = (DISPID_NORMAL_FIRST + 13);
        public const int DISPID_ONSCROLL = (DISPID_NORMAL_FIRST + 14);
        public const int DISPID_ONRESET = (DISPID_NORMAL_FIRST + 15);
        public const int DISPID_ONRESIZE = (DISPID_NORMAL_FIRST + 16);
        public const int DISPID_ONBEFOREUNLOAD = (DISPID_NORMAL_FIRST + 17);
        public const int DISPID_ONCHANGEFOCUS = (DISPID_NORMAL_FIRST + 18);
        public const int DISPID_ONCHANGEBLUR = (DISPID_NORMAL_FIRST + 19);
        public const int DISPID_ONPERSIST = (DISPID_NORMAL_FIRST + 20);
        public const int DISPID_ONPERSISTSAVE = (DISPID_NORMAL_FIRST + 21);
        public const int DISPID_ONPERSISTLOAD = (DISPID_NORMAL_FIRST + 22);
        public const int DISPID_ONCONTEXTMENU = (DISPID_NORMAL_FIRST + 23);
        public const int DISPID_ONBEFOREPRINT = (DISPID_NORMAL_FIRST + 24);
        public const int DISPID_ONAFTERPRINT = (DISPID_NORMAL_FIRST + 25);
        public const int DISPID_ONSTOP = (DISPID_NORMAL_FIRST + 26);
        public const int DISPID_ONBEFOREEDITFOCUS = (DISPID_NORMAL_FIRST + 27);
        public const int DISPID_ONMOUSEHOVER = (DISPID_NORMAL_FIRST + 28);
        public const int DISPID_ONCONTENTREADY = (DISPID_NORMAL_FIRST + 29);
        public const int DISPID_ONLAYOUTCOMPLETE = (DISPID_NORMAL_FIRST + 30);
        public const int DISPID_ONPAGE = (DISPID_NORMAL_FIRST + 31);
        public const int DISPID_ONLINKEDOVERFLOW = (DISPID_NORMAL_FIRST + 32);
        public const int DISPID_ONMOUSEWHEEL = (DISPID_NORMAL_FIRST + 33);
        public const int DISPID_ONBEFOREDEACTIVATE = (DISPID_NORMAL_FIRST + 34);
        public const int DISPID_ONMOVE = (DISPID_NORMAL_FIRST + 35);
        public const int DISPID_ONCONTROLSELECT = (DISPID_NORMAL_FIRST + 36);
        public const int DISPID_ONSELECTIONCHANGE = (DISPID_NORMAL_FIRST + 37);
        public const int DISPID_ONMOVESTART = (DISPID_NORMAL_FIRST + 38);
        public const int DISPID_ONMOVEEND = (DISPID_NORMAL_FIRST + 39);
        public const int DISPID_ONRESIZESTART = (DISPID_NORMAL_FIRST + 40);
        public const int DISPID_ONRESIZEEND = (DISPID_NORMAL_FIRST + 41);
        public const int DISPID_ONMOUSEENTER = (DISPID_NORMAL_FIRST + 42);
        public const int DISPID_ONMOUSELEAVE = (DISPID_NORMAL_FIRST + 43);
        public const int DISPID_ONACTIVATE = (DISPID_NORMAL_FIRST + 44);
        public const int DISPID_ONDEACTIVATE = (DISPID_NORMAL_FIRST + 45);
        public const int DISPID_ONMULTILAYOUTCLEANUP = (DISPID_NORMAL_FIRST + 46);
        public const int DISPID_ONBEFOREACTIVATE = (DISPID_NORMAL_FIRST + 47);
        public const int DISPID_ONFOCUSIN = (DISPID_NORMAL_FIRST + 48);
        public const int DISPID_ONFOCUSOUT = (DISPID_NORMAL_FIRST + 49);

        public const int DISPID_EVPROP_ONMOUSEOVER = (DISPID_EVENTS + 0);
        public const int DISPID_EVMETH_ONMOUSEOVER = STDDISPID_XOBJ_ONMOUSEOVER;
        public const int DISPID_EVPROP_ONMOUSEOUT = (DISPID_EVENTS + 1);
        public const int DISPID_EVMETH_ONMOUSEOUT = STDDISPID_XOBJ_ONMOUSEOUT;
        public const int DISPID_EVPROP_ONMOUSEDOWN = (DISPID_EVENTS + 2);
        public const int DISPID_EVMETH_ONMOUSEDOWN = DISPID_MOUSEDOWN;
        public const int DISPID_EVPROP_ONMOUSEUP = (DISPID_EVENTS + 3);
        public const int DISPID_EVMETH_ONMOUSEUP = DISPID_MOUSEUP;
        public const int DISPID_EVPROP_ONMOUSEMOVE = (DISPID_EVENTS + 4);
        public const int DISPID_EVMETH_ONMOUSEMOVE = DISPID_MOUSEMOVE;
        public const int DISPID_EVPROP_ONKEYDOWN = (DISPID_EVENTS + 5);
        public const int DISPID_EVMETH_ONKEYDOWN = DISPID_KEYDOWN;
        public const int DISPID_EVPROP_ONKEYUP = (DISPID_EVENTS + 6);
        public const int DISPID_EVMETH_ONKEYUP = DISPID_KEYUP;
        public const int DISPID_EVPROP_ONKEYPRESS = (DISPID_EVENTS + 7);
        public const int DISPID_EVMETH_ONKEYPRESS = DISPID_KEYPRESS;
        public const int DISPID_EVPROP_ONCLICK = (DISPID_EVENTS + 8);
        public const int DISPID_EVMETH_ONCLICK = DISPID_CLICK;
        public const int DISPID_EVPROP_ONDBLCLICK = (DISPID_EVENTS + 9);
        public const int DISPID_EVMETH_ONDBLCLICK = DISPID_DBLCLICK;
        public const int DISPID_EVPROP_ONSELECT = (DISPID_EVENTS + 10);
        public const int DISPID_EVMETH_ONSELECT = DISPID_ONSELECT;
        public const int DISPID_EVPROP_ONSUBMIT = (DISPID_EVENTS + 11);
        public const int DISPID_EVMETH_ONSUBMIT = DISPID_ONSUBMIT;
        public const int DISPID_EVPROP_ONRESET = (DISPID_EVENTS + 12);
        public const int DISPID_EVMETH_ONRESET = DISPID_ONRESET;
        public const int DISPID_EVPROP_ONHELP = (DISPID_EVENTS + 13);
        public const int DISPID_EVMETH_ONHELP = STDDISPID_XOBJ_ONHELP;
        public const int DISPID_EVPROP_ONFOCUS = (DISPID_EVENTS + 14);
        public const int DISPID_EVMETH_ONFOCUS = STDDISPID_XOBJ_ONFOCUS;
        public const int DISPID_EVPROP_ONBLUR = (DISPID_EVENTS + 15);
        public const int DISPID_EVMETH_ONBLUR = STDDISPID_XOBJ_ONBLUR;
        public const int DISPID_EVPROP_ONROWEXIT = (DISPID_EVENTS + 18);
        public const int DISPID_EVMETH_ONROWEXIT = STDDISPID_XOBJ_ONROWEXIT;
        public const int DISPID_EVPROP_ONROWENTER = (DISPID_EVENTS + 19);
        public const int DISPID_EVMETH_ONROWENTER = STDDISPID_XOBJ_ONROWENTER;
        public const int DISPID_EVPROP_ONBOUNCE = (DISPID_EVENTS + 20);
        public const int DISPID_EVMETH_ONBOUNCE = DISPID_ONBOUNCE;
        public const int DISPID_EVPROP_ONBEFOREUPDATE = (DISPID_EVENTS + 21);
        public const int DISPID_EVMETH_ONBEFOREUPDATE = STDDISPID_XOBJ_BEFOREUPDATE;
        public const int DISPID_EVPROP_ONAFTERUPDATE = (DISPID_EVENTS + 22);
        public const int DISPID_EVMETH_ONAFTERUPDATE = STDDISPID_XOBJ_AFTERUPDATE;
        public const int DISPID_EVPROP_ONBEFOREDRAGOVER = (DISPID_EVENTS + 23);
        //public const int  DISPID_EVMETH_ONBEFOREDRAGOVER =  EVENTID_CommonCtrlEvent_BeforeDragOver;
        public const int DISPID_EVPROP_ONBEFOREDROPORPASTE = (DISPID_EVENTS + 24);
        //public const int  DISPID_EVMETH_ONBEFOREDROPORPASTE = EVENTID_CommonCtrlEvent_BeforeDropOrPaste;
        public const int DISPID_EVPROP_ONREADYSTATECHANGE = (DISPID_EVENTS + 25);
        public const int DISPID_EVMETH_ONREADYSTATECHANGE = DISPID_READYSTATECHANGE;
        public const int DISPID_EVPROP_ONFINISH = (DISPID_EVENTS + 26);
        public const int DISPID_EVMETH_ONFINISH = DISPID_ONFINISH;
        public const int DISPID_EVPROP_ONSTART = (DISPID_EVENTS + 27);
        public const int DISPID_EVMETH_ONSTART = DISPID_ONSTART;
        public const int DISPID_EVPROP_ONABORT = (DISPID_EVENTS + 28);
        public const int DISPID_EVMETH_ONABORT = DISPID_ONABORT;
        public const int DISPID_EVPROP_ONERROR = (DISPID_EVENTS + 29);
        public const int DISPID_EVMETH_ONERROR = DISPID_ONERROR;
        public const int DISPID_EVPROP_ONCHANGE = (DISPID_EVENTS + 30);
        public const int DISPID_EVMETH_ONCHANGE = DISPID_ONCHANGE;
        public const int DISPID_EVPROP_ONSCROLL = (DISPID_EVENTS + 31);
        public const int DISPID_EVMETH_ONSCROLL = DISPID_ONSCROLL;
        public const int DISPID_EVPROP_ONLOAD = (DISPID_EVENTS + 32);
        public const int DISPID_EVMETH_ONLOAD = DISPID_ONLOAD;
        public const int DISPID_EVPROP_ONUNLOAD = (DISPID_EVENTS + 33);
        public const int DISPID_EVMETH_ONUNLOAD = DISPID_ONUNLOAD;
        public const int DISPID_EVPROP_ONLAYOUT = (DISPID_EVENTS + 34);
        public const int DISPID_EVMETH_ONLAYOUT = DISPID_ONLAYOUT;
        public const int DISPID_EVPROP_ONDRAGSTART = (DISPID_EVENTS + 35);
        public const int DISPID_EVMETH_ONDRAGSTART = STDDISPID_XOBJ_ONDRAGSTART;
        public const int DISPID_EVPROP_ONRESIZE = (DISPID_EVENTS + 36);
        public const int DISPID_EVMETH_ONRESIZE = DISPID_ONRESIZE;
        public const int DISPID_EVPROP_ONSELECTSTART = (DISPID_EVENTS + 37);
        public const int DISPID_EVMETH_ONSELECTSTART = STDDISPID_XOBJ_ONSELECTSTART;
        public const int DISPID_EVPROP_ONERRORUPDATE = (DISPID_EVENTS + 38);
        public const int DISPID_EVMETH_ONERRORUPDATE = STDDISPID_XOBJ_ERRORUPDATE;
        public const int DISPID_EVPROP_ONBEFOREUNLOAD = (DISPID_EVENTS + 39);
        //public const int  DISPID_EVMETH_ONBEFOREUNLOAD  = DISPID_ONBEFOREUNLOAD;
        public const int DISPID_EVPROP_ONDATASETCHANGED = (DISPID_EVENTS + 40);
        public const int DISPID_EVMETH_ONDATASETCHANGED = STDDISPID_XOBJ_ONDATASETCHANGED;
        public const int DISPID_EVPROP_ONDATAAVAILABLE = (DISPID_EVENTS + 41);
        public const int DISPID_EVMETH_ONDATAAVAILABLE = STDDISPID_XOBJ_ONDATAAVAILABLE;
        public const int DISPID_EVPROP_ONDATASETCOMPLETE = (DISPID_EVENTS + 42);
        public const int DISPID_EVMETH_ONDATASETCOMPLETE = STDDISPID_XOBJ_ONDATASETCOMPLETE;
        public const int DISPID_EVPROP_ONFILTER = (DISPID_EVENTS + 43);
        public const int DISPID_EVMETH_ONFILTER = STDDISPID_XOBJ_ONFILTER;
        public const int DISPID_EVPROP_ONCHANGEFOCUS = (DISPID_EVENTS + 44);
        public const int DISPID_EVMETH_ONCHANGEFOCUS = DISPID_ONCHANGEFOCUS;
        public const int DISPID_EVPROP_ONCHANGEBLUR = (DISPID_EVENTS + 45);
        public const int DISPID_EVMETH_ONCHANGEBLUR = DISPID_ONCHANGEBLUR;
        public const int DISPID_EVPROP_ONLOSECAPTURE = (DISPID_EVENTS + 46);
        public const int DISPID_EVMETH_ONLOSECAPTURE = STDDISPID_XOBJ_ONLOSECAPTURE;
        public const int DISPID_EVPROP_ONPROPERTYCHANGE = (DISPID_EVENTS + 47);
        public const int DISPID_EVMETH_ONPROPERTYCHANGE = STDDISPID_XOBJ_ONPROPERTYCHANGE;
        public const int DISPID_EVPROP_ONPERSISTSAVE = (DISPID_EVENTS + 48);
        public const int DISPID_EVMETH_ONPERSISTSAVE = DISPID_ONPERSISTSAVE;
        public const int DISPID_EVPROP_ONDRAG = (DISPID_EVENTS + 49);
        public const int DISPID_EVMETH_ONDRAG = STDDISPID_XOBJ_ONDRAG;
        public const int DISPID_EVPROP_ONDRAGEND = (DISPID_EVENTS + 50);
        public const int DISPID_EVMETH_ONDRAGEND = STDDISPID_XOBJ_ONDRAGEND;
        public const int DISPID_EVPROP_ONDRAGENTER = (DISPID_EVENTS + 51);
        public const int DISPID_EVMETH_ONDRAGENTER = STDDISPID_XOBJ_ONDRAGENTER;
        public const int DISPID_EVPROP_ONDRAGOVER = (DISPID_EVENTS + 52);
        public const int DISPID_EVMETH_ONDRAGOVER = STDDISPID_XOBJ_ONDRAGOVER;
        public const int DISPID_EVPROP_ONDRAGLEAVE = (DISPID_EVENTS + 53);
        public const int DISPID_EVMETH_ONDRAGLEAVE = STDDISPID_XOBJ_ONDRAGLEAVE;
        public const int DISPID_EVPROP_ONDROP = (DISPID_EVENTS + 54);
        public const int DISPID_EVMETH_ONDROP = STDDISPID_XOBJ_ONDROP;
        public const int DISPID_EVPROP_ONCUT = (DISPID_EVENTS + 55);
        public const int DISPID_EVMETH_ONCUT = STDDISPID_XOBJ_ONCUT;
        public const int DISPID_EVPROP_ONCOPY = (DISPID_EVENTS + 56);
        public const int DISPID_EVMETH_ONCOPY = STDDISPID_XOBJ_ONCOPY;
        public const int DISPID_EVPROP_ONPASTE = (DISPID_EVENTS + 57);
        public const int DISPID_EVMETH_ONPASTE = STDDISPID_XOBJ_ONPASTE;
        public const int DISPID_EVPROP_ONBEFORECUT = (DISPID_EVENTS + 58);
        public const int DISPID_EVMETH_ONBEFORECUT = STDDISPID_XOBJ_ONBEFORECUT;
        public const int DISPID_EVPROP_ONBEFORECOPY = (DISPID_EVENTS + 59);
        public const int DISPID_EVMETH_ONBEFORECOPY = STDDISPID_XOBJ_ONBEFORECOPY;
        public const int DISPID_EVPROP_ONBEFOREPASTE = (DISPID_EVENTS + 60);
        public const int DISPID_EVMETH_ONBEFOREPASTE = STDDISPID_XOBJ_ONBEFOREPASTE;
        public const int DISPID_EVPROP_ONPERSISTLOAD = (DISPID_EVENTS + 61);
        public const int DISPID_EVMETH_ONPERSISTLOAD = DISPID_ONPERSISTLOAD;
        public const int DISPID_EVPROP_ONROWSDELETE = (DISPID_EVENTS + 62);
        public const int DISPID_EVMETH_ONROWSDELETE = STDDISPID_XOBJ_ONROWSDELETE;
        public const int DISPID_EVPROP_ONROWSINSERTED = (DISPID_EVENTS + 63);
        public const int DISPID_EVMETH_ONROWSINSERTED = STDDISPID_XOBJ_ONROWSINSERTED;
        public const int DISPID_EVPROP_ONCELLCHANGE = (DISPID_EVENTS + 64);
        public const int DISPID_EVMETH_ONCELLCHANGE = STDDISPID_XOBJ_ONCELLCHANGE;
        public const int DISPID_EVPROP_ONCONTEXTMENU = (DISPID_EVENTS + 65);
        public const int DISPID_EVMETH_ONCONTEXTMENU = DISPID_ONCONTEXTMENU;
        public const int DISPID_EVPROP_ONBEFOREPRINT = (DISPID_EVENTS + 66);
        public const int DISPID_EVMETH_ONBEFOREPRINT = DISPID_ONBEFOREPRINT;
        public const int DISPID_EVPROP_ONAFTERPRINT = (DISPID_EVENTS + 67);
        public const int DISPID_EVMETH_ONAFTERPRINT = DISPID_ONAFTERPRINT;
        public const int DISPID_EVPROP_ONSTOP = (DISPID_EVENTS + 68);
        public const int DISPID_EVMETH_ONSTOP = DISPID_ONSTOP;
        public const int DISPID_EVPROP_ONBEFOREEDITFOCUS = (DISPID_EVENTS + 69);
        public const int DISPID_EVMETH_ONBEFOREEDITFOCUS = DISPID_ONBEFOREEDITFOCUS;
        public const int DISPID_EVPROP_ONATTACHEVENT = (DISPID_EVENTS + 70);
        public const int DISPID_EVPROP_ONMOUSEHOVER = (DISPID_EVENTS + 71);
        public const int DISPID_EVMETH_ONMOUSEHOVER = DISPID_ONMOUSEHOVER;
        public const int DISPID_EVPROP_ONCONTENTREADY = (DISPID_EVENTS + 72);
        public const int DISPID_EVMETH_ONCONTENTREADY = DISPID_ONCONTENTREADY;
        public const int DISPID_EVPROP_ONLAYOUTCOMPLETE = (DISPID_EVENTS + 73);
        public const int DISPID_EVMETH_ONLAYOUTCOMPLETE = DISPID_ONLAYOUTCOMPLETE;
        public const int DISPID_EVPROP_ONPAGE = (DISPID_EVENTS + 74);
        public const int DISPID_EVMETH_ONPAGE = DISPID_ONPAGE;
        public const int DISPID_EVPROP_ONLINKEDOVERFLOW = (DISPID_EVENTS + 75);
        public const int DISPID_EVMETH_ONLINKEDOVERFLOW = DISPID_ONLINKEDOVERFLOW;
        public const int DISPID_EVPROP_ONMOUSEWHEEL = (DISPID_EVENTS + 76);
        public const int DISPID_EVMETH_ONMOUSEWHEEL = DISPID_ONMOUSEWHEEL;
        public const int DISPID_EVPROP_ONBEFOREDEACTIVATE = (DISPID_EVENTS + 77);
        public const int DISPID_EVMETH_ONBEFOREDEACTIVATE = DISPID_ONBEFOREDEACTIVATE;
        public const int DISPID_EVPROP_ONMOVE = (DISPID_EVENTS + 78);
        public const int DISPID_EVMETH_ONMOVE = DISPID_ONMOVE;
        public const int DISPID_EVPROP_ONCONTROLSELECT = (DISPID_EVENTS + 79);
        public const int DISPID_EVMETH_ONCONTROLSELECT = DISPID_ONCONTROLSELECT;
        public const int DISPID_EVPROP_ONSELECTIONCHANGE = (DISPID_EVENTS + 80);
        public const int DISPID_EVMETH_ONSELECTIONCHANGE = DISPID_ONSELECTIONCHANGE;
        public const int DISPID_EVPROP_ONMOVESTART = (DISPID_EVENTS + 81);
        public const int DISPID_EVMETH_ONMOVESTART = DISPID_ONMOVESTART;
        public const int DISPID_EVPROP_ONMOVEEND = (DISPID_EVENTS + 82);
        public const int DISPID_EVMETH_ONMOVEEND = DISPID_ONMOVEEND;
        public const int DISPID_EVPROP_ONRESIZESTART = (DISPID_EVENTS + 83);
        public const int DISPID_EVMETH_ONRESIZESTART = DISPID_ONRESIZESTART;
        public const int DISPID_EVPROP_ONRESIZEEND = (DISPID_EVENTS + 84);
        public const int DISPID_EVMETH_ONRESIZEEND = DISPID_ONRESIZEEND;
        public const int DISPID_EVPROP_ONMOUSEENTER = (DISPID_EVENTS + 85);
        public const int DISPID_EVMETH_ONMOUSEENTER = DISPID_ONMOUSEENTER;
        public const int DISPID_EVPROP_ONMOUSELEAVE = (DISPID_EVENTS + 86);
        public const int DISPID_EVMETH_ONMOUSELEAVE = DISPID_ONMOUSELEAVE;
        public const int DISPID_EVPROP_ONACTIVATE = (DISPID_EVENTS + 87);
        public const int DISPID_EVMETH_ONACTIVATE = DISPID_ONACTIVATE;
        public const int DISPID_EVPROP_ONDEACTIVATE = (DISPID_EVENTS + 88);
        public const int DISPID_EVMETH_ONDEACTIVATE = DISPID_ONDEACTIVATE;
        public const int DISPID_EVPROP_ONMULTILAYOUTCLEANUP = (DISPID_EVENTS + 89);
        public const int DISPID_EVMETH_ONMULTILAYOUTCLEANUP = DISPID_ONMULTILAYOUTCLEANUP;
        public const int DISPID_EVPROP_ONBEFOREACTIVATE = (DISPID_EVENTS + 90);
        public const int DISPID_EVMETH_ONBEFOREACTIVATE = DISPID_ONBEFOREACTIVATE;
        public const int DISPID_EVPROP_ONFOCUSIN = (DISPID_EVENTS + 91);
        public const int DISPID_EVMETH_ONFOCUSIN = DISPID_ONFOCUSIN;
        public const int DISPID_EVPROP_ONFOCUSOUT = (DISPID_EVENTS + 92);
        public const int DISPID_EVMETH_ONFOCUSOUT = DISPID_ONFOCUSOUT;

        public const int STDPROPID_XOBJ_CONTROLTIPTEXT = (DISPID_XOBJ_BASE + 0x45);

        public const int DISPID_A_LANGUAGE = (DISPID_A_FIRST + 100);
        public const int DISPID_A_LANG = (DISPID_A_FIRST + 9);
        public const int STDPROPID_XOBJ_PARENT = (DISPID_XOBJ_BASE + 0x8);
        public const int STDPROPID_XOBJ_STYLE = (DISPID_XOBJ_BASE + 0x4A);

        public const int DISPID_IHTMLELEMENT_SETATTRIBUTE = DISPID_HTMLOBJECT + 1;
        public const int DISPID_IHTMLELEMENT_GETATTRIBUTE = DISPID_HTMLOBJECT + 2;
        public const int DISPID_IHTMLELEMENT_REMOVEATTRIBUTE = DISPID_HTMLOBJECT + 3;
        public const int DISPID_IHTMLELEMENT_CLASSNAME = DISPID_ELEMENT + 1;
        public const int DISPID_IHTMLELEMENT_ID = DISPID_ELEMENT + 2;
        public const int DISPID_IHTMLELEMENT_TAGNAME = DISPID_ELEMENT + 4;
        public const int DISPID_IHTMLELEMENT_PARENTELEMENT = STDPROPID_XOBJ_PARENT;
        public const int DISPID_IHTMLELEMENT_STYLE = STDPROPID_XOBJ_STYLE;
        public const int DISPID_IHTMLELEMENT_ONHELP = DISPID_EVPROP_ONHELP; //-2147412098
        public const int DISPID_IHTMLELEMENT_ONCLICK = DISPID_EVPROP_ONCLICK; //-2147412103
        public const int DISPID_IHTMLELEMENT_ONDBLCLICK = DISPID_EVPROP_ONDBLCLICK;//-2147412102
        public const int DISPID_IHTMLELEMENT_ONKEYDOWN = DISPID_EVPROP_ONKEYDOWN; //-2147412106
        public const int DISPID_IHTMLELEMENT_ONKEYUP = DISPID_EVPROP_ONKEYUP;
        public const int DISPID_IHTMLELEMENT_ONKEYPRESS = DISPID_EVPROP_ONKEYPRESS; //-2147412104
        public const int DISPID_IHTMLELEMENT_ONMOUSEOUT = DISPID_EVPROP_ONMOUSEOUT; //-2147412110
        public const int DISPID_IHTMLELEMENT_ONMOUSEOVER = DISPID_EVPROP_ONMOUSEOVER; //-2147412111
        public const int DISPID_IHTMLELEMENT_ONMOUSEMOVE = DISPID_EVPROP_ONMOUSEMOVE; // -2147412107
        public const int DISPID_IHTMLELEMENT_ONMOUSEDOWN = DISPID_EVPROP_ONMOUSEDOWN;
        public const int DISPID_IHTMLELEMENT_ONMOUSEUP = DISPID_EVPROP_ONMOUSEUP;
        public const int DISPID_IHTMLELEMENT_DOCUMENT = DISPID_ELEMENT + 18;
        public const int DISPID_IHTMLELEMENT_TITLE = STDPROPID_XOBJ_CONTROLTIPTEXT;
        public const int DISPID_IHTMLELEMENT_LANGUAGE = DISPID_A_LANGUAGE;
        public const int DISPID_IHTMLELEMENT_ONSELECTSTART = DISPID_EVPROP_ONSELECTSTART;
        public const int DISPID_IHTMLELEMENT_SCROLLINTOVIEW = DISPID_ELEMENT + 19;
        public const int DISPID_IHTMLELEMENT_CONTAINS = DISPID_ELEMENT + 20;
        public const int DISPID_IHTMLELEMENT_SOURCEINDEX = DISPID_ELEMENT + 24;
        public const int DISPID_IHTMLELEMENT_RECORDNUMBER = DISPID_ELEMENT + 25;
        public const int DISPID_IHTMLELEMENT_LANG = DISPID_A_LANG;
        public const int DISPID_IHTMLELEMENT_OFFSETLEFT = DISPID_ELEMENT + 8;
        public const int DISPID_IHTMLELEMENT_OFFSETTOP = DISPID_ELEMENT + 9;
        public const int DISPID_IHTMLELEMENT_OFFSETWIDTH = DISPID_ELEMENT + 10;
        public const int DISPID_IHTMLELEMENT_OFFSETHEIGHT = DISPID_ELEMENT + 11;
        public const int DISPID_IHTMLELEMENT_OFFSETPARENT = DISPID_ELEMENT + 12;
        public const int DISPID_IHTMLELEMENT_INNERHTML = DISPID_ELEMENT + 26;
        public const int DISPID_IHTMLELEMENT_INNERTEXT = DISPID_ELEMENT + 27;
        public const int DISPID_IHTMLELEMENT_OUTERHTML = DISPID_ELEMENT + 28;
        public const int DISPID_IHTMLELEMENT_OUTERTEXT = DISPID_ELEMENT + 29;
        public const int DISPID_IHTMLELEMENT_INSERTADJACENTHTML = DISPID_ELEMENT + 30;
        public const int DISPID_IHTMLELEMENT_INSERTADJACENTTEXT = DISPID_ELEMENT + 31;
        public const int DISPID_IHTMLELEMENT_PARENTTEXTEDIT = DISPID_ELEMENT + 32;
        public const int DISPID_IHTMLELEMENT_ISTEXTEDIT = DISPID_ELEMENT + 34;
        public const int DISPID_IHTMLELEMENT_CLICK = DISPID_ELEMENT + 33;
        public const int DISPID_IHTMLELEMENT_FILTERS = DISPID_ELEMENT + 35;
        public const int DISPID_IHTMLELEMENT_ONDRAGSTART = DISPID_EVPROP_ONDRAGSTART;
        public const int DISPID_IHTMLELEMENT_TOSTRING = DISPID_ELEMENT + 36;
        public const int DISPID_IHTMLELEMENT_ONBEFOREUPDATE = DISPID_EVPROP_ONBEFOREUPDATE;
        public const int DISPID_IHTMLELEMENT_ONAFTERUPDATE = DISPID_EVPROP_ONAFTERUPDATE;
        public const int DISPID_IHTMLELEMENT_ONERRORUPDATE = DISPID_EVPROP_ONERRORUPDATE;
        public const int DISPID_IHTMLELEMENT_ONROWEXIT = DISPID_EVPROP_ONROWEXIT;
        public const int DISPID_IHTMLELEMENT_ONROWENTER = DISPID_EVPROP_ONROWENTER;
        public const int DISPID_IHTMLELEMENT_ONDATASETCHANGED = DISPID_EVPROP_ONDATASETCHANGED;
        public const int DISPID_IHTMLELEMENT_ONDATAAVAILABLE = DISPID_EVPROP_ONDATAAVAILABLE;
        public const int DISPID_IHTMLELEMENT_ONDATASETCOMPLETE = DISPID_EVPROP_ONDATASETCOMPLETE;
        public const int DISPID_IHTMLELEMENT_ONFILTERCHANGE = DISPID_EVPROP_ONFILTER;
        public const int DISPID_IHTMLELEMENT_CHILDREN = DISPID_ELEMENT + 37;
        public const int DISPID_IHTMLELEMENT_ALL = DISPID_ELEMENT + 38;

        //  DISPIDs for interface IHTMLElement2
        public const int DISPID_IHTMLELEMENT2_SCOPENAME = DISPID_ELEMENT + 39;
        public const int DISPID_IHTMLELEMENT2_SETCAPTURE = DISPID_ELEMENT + 40;
        public const int DISPID_IHTMLELEMENT2_RELEASECAPTURE = DISPID_ELEMENT + 41;
        public const int DISPID_IHTMLELEMENT2_ONLOSECAPTURE = DISPID_EVPROP_ONLOSECAPTURE;
        public const int DISPID_IHTMLELEMENT2_COMPONENTFROMPOINT = DISPID_ELEMENT + 42;
        public const int DISPID_IHTMLELEMENT2_DOSCROLL = DISPID_ELEMENT + 43;
        public const int DISPID_IHTMLELEMENT2_ONSCROLL = DISPID_EVPROP_ONSCROLL;
        public const int DISPID_IHTMLELEMENT2_ONDRAG = DISPID_EVPROP_ONDRAG;
        public const int DISPID_IHTMLELEMENT2_ONDRAGEND = DISPID_EVPROP_ONDRAGEND;
        public const int DISPID_IHTMLELEMENT2_ONDRAGENTER = DISPID_EVPROP_ONDRAGENTER;
        public const int DISPID_IHTMLELEMENT2_ONDRAGOVER = DISPID_EVPROP_ONDRAGOVER;
        public const int DISPID_IHTMLELEMENT2_ONDRAGLEAVE = DISPID_EVPROP_ONDRAGLEAVE;
        public const int DISPID_IHTMLELEMENT2_ONDROP = DISPID_EVPROP_ONDROP;
        public const int DISPID_IHTMLELEMENT2_ONBEFORECUT = DISPID_EVPROP_ONBEFORECUT;
        public const int DISPID_IHTMLELEMENT2_ONCUT = DISPID_EVPROP_ONCUT;
        public const int DISPID_IHTMLELEMENT2_ONBEFORECOPY = DISPID_EVPROP_ONBEFORECOPY;
        public const int DISPID_IHTMLELEMENT2_ONCOPY = DISPID_EVPROP_ONCOPY;
        public const int DISPID_IHTMLELEMENT2_ONBEFOREPASTE = DISPID_EVPROP_ONBEFOREPASTE;
        public const int DISPID_IHTMLELEMENT2_ONPASTE = DISPID_EVPROP_ONPASTE;
        public const int DISPID_IHTMLELEMENT2_CURRENTSTYLE = DISPID_ELEMENT + 7;
        public const int DISPID_IHTMLELEMENT2_ONPROPERTYCHANGE = DISPID_EVPROP_ONPROPERTYCHANGE;
        public const int DISPID_IHTMLELEMENT2_GETCLIENTRECTS = DISPID_ELEMENT + 44;
        public const int DISPID_IHTMLELEMENT2_GETBOUNDINGCLIENTRECT = DISPID_ELEMENT + 45;
        public const int DISPID_IHTMLELEMENT2_SETEXPRESSION = DISPID_HTMLOBJECT + 4;
        public const int DISPID_IHTMLELEMENT2_GETEXPRESSION = DISPID_HTMLOBJECT + 5;
        public const int DISPID_IHTMLELEMENT2_REMOVEEXPRESSION = DISPID_HTMLOBJECT + 6;
        //public const int  DISPID_IHTMLELEMENT2_TABINDEX  = STDPROPID_XOBJ_TABINDEX;
        public const int DISPID_IHTMLELEMENT2_FOCUS = DISPID_SITE + 0;
        public const int DISPID_IHTMLELEMENT2_ACCESSKEY = DISPID_SITE + 5;
        public const int DISPID_IHTMLELEMENT2_ONBLUR = DISPID_EVPROP_ONBLUR;
        public const int DISPID_IHTMLELEMENT2_ONFOCUS = DISPID_EVPROP_ONFOCUS;
        public const int DISPID_IHTMLELEMENT2_ONRESIZE = DISPID_EVPROP_ONRESIZE;
        public const int DISPID_IHTMLELEMENT2_BLUR = DISPID_SITE + 2;
        public const int DISPID_IHTMLELEMENT2_ADDFILTER = DISPID_SITE + 17;
        public const int DISPID_IHTMLELEMENT2_REMOVEFILTER = DISPID_SITE + 18;
        public const int DISPID_IHTMLELEMENT2_CLIENTHEIGHT = DISPID_SITE + 19;
        public const int DISPID_IHTMLELEMENT2_CLIENTWIDTH = DISPID_SITE + 20;
        public const int DISPID_IHTMLELEMENT2_CLIENTTOP = DISPID_SITE + 21;
        public const int DISPID_IHTMLELEMENT2_CLIENTLEFT = DISPID_SITE + 22;
        public const int DISPID_IHTMLELEMENT2_ATTACHEVENT = DISPID_HTMLOBJECT + 7;
        public const int DISPID_IHTMLELEMENT2_DETACHEVENT = DISPID_HTMLOBJECT + 8;
        //public const int  DISPID_IHTMLELEMENT2_READYSTATE  = DISPID_A_READYSTATE;
        public const int DISPID_IHTMLELEMENT2_ONREADYSTATECHANGE = DISPID_EVPROP_ONREADYSTATECHANGE;
        public const int DISPID_IHTMLELEMENT2_ONROWSDELETE = DISPID_EVPROP_ONROWSDELETE;
        public const int DISPID_IHTMLELEMENT2_ONROWSINSERTED = DISPID_EVPROP_ONROWSINSERTED;
        public const int DISPID_IHTMLELEMENT2_ONCELLCHANGE = DISPID_EVPROP_ONCELLCHANGE;
        //public const int  DISPID_IHTMLELEMENT2_DIR = DISPID_A_DIR;
        public const int DISPID_IHTMLELEMENT2_CREATECONTROLRANGE = DISPID_ELEMENT + 56;
        public const int DISPID_IHTMLELEMENT2_SCROLLHEIGHT = DISPID_ELEMENT + 57;
        public const int DISPID_IHTMLELEMENT2_SCROLLWIDTH = DISPID_ELEMENT + 58;
        public const int DISPID_IHTMLELEMENT2_SCROLLTOP = DISPID_ELEMENT + 59;
        public const int DISPID_IHTMLELEMENT2_SCROLLLEFT = DISPID_ELEMENT + 60;
        public const int DISPID_IHTMLELEMENT2_CLEARATTRIBUTES = DISPID_ELEMENT + 62;
        public const int DISPID_IHTMLELEMENT2_MERGEATTRIBUTES = DISPID_ELEMENT + 63;
        public const int DISPID_IHTMLELEMENT2_ONCONTEXTMENU = DISPID_EVPROP_ONCONTEXTMENU;
        public const int DISPID_IHTMLELEMENT2_INSERTADJACENTELEMENT = DISPID_ELEMENT + 69;
        public const int DISPID_IHTMLELEMENT2_APPLYELEMENT = DISPID_ELEMENT + 65;
        public const int DISPID_IHTMLELEMENT2_GETADJACENTTEXT = DISPID_ELEMENT + 70;
        public const int DISPID_IHTMLELEMENT2_REPLACEADJACENTTEXT = DISPID_ELEMENT + 71;
        public const int DISPID_IHTMLELEMENT2_CANHAVECHILDREN = DISPID_ELEMENT + 72;
        public const int DISPID_IHTMLELEMENT2_ADDBEHAVIOR = DISPID_ELEMENT + 80;
        public const int DISPID_IHTMLELEMENT2_REMOVEBEHAVIOR = DISPID_ELEMENT + 81;
        public const int DISPID_IHTMLELEMENT2_RUNTIMESTYLE = DISPID_ELEMENT + 64;
        public const int DISPID_IHTMLELEMENT2_BEHAVIORURNS = DISPID_ELEMENT + 82;
        public const int DISPID_IHTMLELEMENT2_TAGURN = DISPID_ELEMENT + 83;
        public const int DISPID_IHTMLELEMENT2_ONBEFOREEDITFOCUS = DISPID_EVPROP_ONBEFOREEDITFOCUS;
        public const int DISPID_IHTMLELEMENT2_READYSTATEVALUE = DISPID_ELEMENT + 84;
        public const int DISPID_IHTMLELEMENT2_GETELEMENTSBYTAGNAME = DISPID_ELEMENT + 85;

        //    DISPIDs for interface IHTMLElementCollection
        public const int DISPID_IHTMLELEMENTCOLLECTION_TOSTRING = DISPID_COLLECTION + 1;
        public const int DISPID_IHTMLELEMENTCOLLECTION_LENGTH = DISPID_COLLECTION;
        public const int DISPID_IHTMLELEMENTCOLLECTION__NEWENUM = DISPID_NEWENUM;
        public const int DISPID_IHTMLELEMENTCOLLECTION_ITEM = DISPID_VALUE;
        public const int DISPID_IHTMLELEMENTCOLLECTION_TAGS = DISPID_COLLECTION + 2;


        //    DISPIDs for interface IHTMLEventObj
        public const int DISPID_EVENTOBJ = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLEVENTOBJ_SRCELEMENT = DISPID_EVENTOBJ + 1;
        public const int DISPID_IHTMLEVENTOBJ_ALTKEY = DISPID_EVENTOBJ + 2;
        public const int DISPID_IHTMLEVENTOBJ_CTRLKEY = DISPID_EVENTOBJ + 3;
        public const int DISPID_IHTMLEVENTOBJ_SHIFTKEY = DISPID_EVENTOBJ + 4;
        public const int DISPID_IHTMLEVENTOBJ_RETURNVALUE = DISPID_EVENTOBJ + 7;
        public const int DISPID_IHTMLEVENTOBJ_CANCELBUBBLE = DISPID_EVENTOBJ + 8;
        public const int DISPID_IHTMLEVENTOBJ_FROMELEMENT = DISPID_EVENTOBJ + 9;
        public const int DISPID_IHTMLEVENTOBJ_TOELEMENT = DISPID_EVENTOBJ + 10;
        public const int DISPID_IHTMLEVENTOBJ_KEYCODE = DISPID_EVENTOBJ + 11;
        public const int DISPID_IHTMLEVENTOBJ_BUTTON = DISPID_EVENTOBJ + 12;
        public const int DISPID_IHTMLEVENTOBJ_TYPE = DISPID_EVENTOBJ + 13;
        public const int DISPID_IHTMLEVENTOBJ_QUALIFIER = DISPID_EVENTOBJ + 14;
        public const int DISPID_IHTMLEVENTOBJ_REASON = DISPID_EVENTOBJ + 15;
        public const int DISPID_IHTMLEVENTOBJ_X = DISPID_EVENTOBJ + 5;
        public const int DISPID_IHTMLEVENTOBJ_Y = DISPID_EVENTOBJ + 6;
        public const int DISPID_IHTMLEVENTOBJ_CLIENTX = DISPID_EVENTOBJ + 20;
        public const int DISPID_IHTMLEVENTOBJ_CLIENTY = DISPID_EVENTOBJ + 21;
        public const int DISPID_IHTMLEVENTOBJ_OFFSETX = DISPID_EVENTOBJ + 22;
        public const int DISPID_IHTMLEVENTOBJ_OFFSETY = DISPID_EVENTOBJ + 23;
        public const int DISPID_IHTMLEVENTOBJ_SCREENX = DISPID_EVENTOBJ + 24;
        public const int DISPID_IHTMLEVENTOBJ_SCREENY = DISPID_EVENTOBJ + 25;
        public const int DISPID_IHTMLEVENTOBJ_SRCFILTER = DISPID_EVENTOBJ + 26;

        public const int DISPID_A_FIRST = DISPID_ATTRS;
        public const int DISPID_A_DIR = DISPID_A_FIRST + 117;

        // DISPIDs for interface IHTMLDocument3
        public const int DISPID_IHTMLDOCUMENT3_RELEASECAPTURE = DISPID_OMDOCUMENT + 72;
        public const int DISPID_IHTMLDOCUMENT3_RECALC = DISPID_OMDOCUMENT + 73;
        public const int DISPID_IHTMLDOCUMENT3_CREATETEXTNODE = DISPID_OMDOCUMENT + 74;
        public const int DISPID_IHTMLDOCUMENT3_DOCUMENTELEMENT = DISPID_OMDOCUMENT + 75;
        public const int DISPID_IHTMLDOCUMENT3_UNIQUEID = DISPID_OMDOCUMENT + 77;
        public const int DISPID_IHTMLDOCUMENT3_ATTACHEVENT = DISPID_HTMLOBJECT + 7;
        public const int DISPID_IHTMLDOCUMENT3_DETACHEVENT = DISPID_HTMLOBJECT + 8;
        public const int DISPID_IHTMLDOCUMENT3_ONROWSDELETE = DISPID_EVPROP_ONROWSDELETE;
        public const int DISPID_IHTMLDOCUMENT3_ONROWSINSERTED = DISPID_EVPROP_ONROWSINSERTED;
        public const int DISPID_IHTMLDOCUMENT3_ONCELLCHANGE = DISPID_EVPROP_ONCELLCHANGE;
        public const int DISPID_IHTMLDOCUMENT3_ONDATASETCHANGED = DISPID_EVPROP_ONDATASETCHANGED;
        public const int DISPID_IHTMLDOCUMENT3_ONDATAAVAILABLE = DISPID_EVPROP_ONDATAAVAILABLE;
        public const int DISPID_IHTMLDOCUMENT3_ONDATASETCOMPLETE = DISPID_EVPROP_ONDATASETCOMPLETE;
        public const int DISPID_IHTMLDOCUMENT3_ONPROPERTYCHANGE = DISPID_EVPROP_ONPROPERTYCHANGE;
        public const int DISPID_IHTMLDOCUMENT3_DIR = DISPID_A_DIR;
        public const int DISPID_IHTMLDOCUMENT3_ONCONTEXTMENU = DISPID_EVPROP_ONCONTEXTMENU;
        public const int DISPID_IHTMLDOCUMENT3_ONSTOP = DISPID_EVPROP_ONSTOP;
        public const int DISPID_IHTMLDOCUMENT3_CREATEDOCUMENTFRAGMENT = DISPID_OMDOCUMENT + 76;
        public const int DISPID_IHTMLDOCUMENT3_PARENTDOCUMENT = DISPID_OMDOCUMENT + 78;
        public const int DISPID_IHTMLDOCUMENT3_ENABLEDOWNLOAD = DISPID_OMDOCUMENT + 79;
        public const int DISPID_IHTMLDOCUMENT3_BASEURL = DISPID_OMDOCUMENT + 80;
        public const int DISPID_IHTMLDOCUMENT3_CHILDNODES = DISPID_ELEMENT + 49;
        public const int DISPID_IHTMLDOCUMENT3_INHERITSTYLESHEETS = DISPID_OMDOCUMENT + 82;
        public const int DISPID_IHTMLDOCUMENT3_ONBEFOREEDITFOCUS = DISPID_EVPROP_ONBEFOREEDITFOCUS;
        public const int DISPID_IHTMLDOCUMENT3_GETELEMENTSBYNAME = DISPID_OMDOCUMENT + 86;
        public const int DISPID_IHTMLDOCUMENT3_GETELEMENTBYID = DISPID_OMDOCUMENT + 88;
        public const int DISPID_IHTMLDOCUMENT3_GETELEMENTSBYTAGNAME = DISPID_OMDOCUMENT + 87;

        //    DISPIDs for interface IHTMLDocument4
        public const int DISPID_OMDOCUMENT = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLDOCUMENT4_FOCUS = DISPID_OMDOCUMENT + 89;
        public const int DISPID_IHTMLDOCUMENT4_HASFOCUS = DISPID_OMDOCUMENT + 90;
        public const int DISPID_IHTMLDOCUMENT4_ONSELECTIONCHANGE = DISPID_EVPROP_ONSELECTIONCHANGE;
        public const int DISPID_IHTMLDOCUMENT4_NAMESPACES = DISPID_OMDOCUMENT + 91;
        public const int DISPID_IHTMLDOCUMENT4_CREATEDOCUMENTFROMURL = DISPID_OMDOCUMENT + 92;
        public const int DISPID_IHTMLDOCUMENT4_MEDIA = DISPID_OMDOCUMENT + 93;
        public const int DISPID_IHTMLDOCUMENT4_CREATEEVENTOBJECT = DISPID_OMDOCUMENT + 94;
        public const int DISPID_IHTMLDOCUMENT4_FIREEVENT = DISPID_OMDOCUMENT + 95;
        public const int DISPID_IHTMLDOCUMENT4_CREATERENDERSTYLE = DISPID_OMDOCUMENT + 96;
        public const int DISPID_IHTMLDOCUMENT4_ONCONTROLSELECT = DISPID_EVPROP_ONCONTROLSELECT;
        public const int DISPID_IHTMLDOCUMENT4_URLUNENCODED = DISPID_OMDOCUMENT + 97;

        //    DISPIDs for interface IHTMLDocument5
        public const int DISPID_IHTMLDOCUMENT5_ONMOUSEWHEEL = DISPID_EVPROP_ONMOUSEWHEEL;
        public const int DISPID_IHTMLDOCUMENT5_DOCTYPE = DISPID_OMDOCUMENT + 98;
        public const int DISPID_IHTMLDOCUMENT5_IMPLEMENTATION = DISPID_OMDOCUMENT + 99;
        public const int DISPID_IHTMLDOCUMENT5_CREATEATTRIBUTE = DISPID_OMDOCUMENT + 100;
        public const int DISPID_IHTMLDOCUMENT5_CREATECOMMENT = DISPID_OMDOCUMENT + 101;
        public const int DISPID_IHTMLDOCUMENT5_ONFOCUSIN = DISPID_EVPROP_ONFOCUSIN;
        public const int DISPID_IHTMLDOCUMENT5_ONFOCUSOUT = DISPID_EVPROP_ONFOCUSOUT;
        public const int DISPID_IHTMLDOCUMENT5_ONACTIVATE = DISPID_EVPROP_ONACTIVATE;
        public const int DISPID_IHTMLDOCUMENT5_ONDEACTIVATE = DISPID_EVPROP_ONDEACTIVATE;
        public const int DISPID_IHTMLDOCUMENT5_ONBEFOREACTIVATE = DISPID_EVPROP_ONBEFOREACTIVATE;
        public const int DISPID_IHTMLDOCUMENT5_ONBEFOREDEACTIVATE = DISPID_EVPROP_ONBEFOREDEACTIVATE;
        public const int DISPID_IHTMLDOCUMENT5_COMPATMODE = DISPID_OMDOCUMENT + 102;

        //DISPIDS for interface IHTMLDocumentEvents2
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONHELP = DISPID_EVMETH_ONHELP;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONCLICK = DISPID_EVMETH_ONCLICK;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDBLCLICK = DISPID_EVMETH_ONDBLCLICK;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONKEYDOWN = DISPID_EVMETH_ONKEYDOWN;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONKEYUP = DISPID_EVMETH_ONKEYUP;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONKEYPRESS = DISPID_EVMETH_ONKEYPRESS;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEDOWN = DISPID_EVMETH_ONMOUSEDOWN;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEMOVE = DISPID_EVMETH_ONMOUSEMOVE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEUP = DISPID_EVMETH_ONMOUSEUP;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEOUT = DISPID_EVMETH_ONMOUSEOUT;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEOVER = DISPID_EVMETH_ONMOUSEOVER;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONREADYSTATECHANGE = DISPID_EVMETH_ONREADYSTATECHANGE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONBEFOREUPDATE = DISPID_EVMETH_ONBEFOREUPDATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONAFTERUPDATE = DISPID_EVMETH_ONAFTERUPDATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONROWEXIT = DISPID_EVMETH_ONROWEXIT;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONROWENTER = DISPID_EVMETH_ONROWENTER;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDRAGSTART = DISPID_EVMETH_ONDRAGSTART;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONSELECTSTART = DISPID_EVMETH_ONSELECTSTART;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONERRORUPDATE = DISPID_EVMETH_ONERRORUPDATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONCONTEXTMENU = DISPID_EVMETH_ONCONTEXTMENU;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONSTOP = DISPID_EVMETH_ONSTOP;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONROWSDELETE = DISPID_EVMETH_ONROWSDELETE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONROWSINSERTED = DISPID_EVMETH_ONROWSINSERTED;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONCELLCHANGE = DISPID_EVMETH_ONCELLCHANGE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONPROPERTYCHANGE = DISPID_EVMETH_ONPROPERTYCHANGE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDATASETCHANGED = DISPID_EVMETH_ONDATASETCHANGED;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDATAAVAILABLE = DISPID_EVMETH_ONDATAAVAILABLE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDATASETCOMPLETE = DISPID_EVMETH_ONDATASETCOMPLETE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONBEFOREEDITFOCUS = DISPID_EVMETH_ONBEFOREEDITFOCUS;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONSELECTIONCHANGE = DISPID_EVMETH_ONSELECTIONCHANGE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONCONTROLSELECT = DISPID_EVMETH_ONCONTROLSELECT;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONMOUSEWHEEL = DISPID_EVMETH_ONMOUSEWHEEL;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONFOCUSIN = DISPID_EVMETH_ONFOCUSIN;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONFOCUSOUT = DISPID_EVMETH_ONFOCUSOUT;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONACTIVATE = DISPID_EVMETH_ONACTIVATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONDEACTIVATE = DISPID_EVMETH_ONDEACTIVATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONBEFOREACTIVATE = DISPID_EVMETH_ONBEFOREACTIVATE;
        public const int DISPID_HTMLDOCUMENTEVENTS2_ONBEFOREDEACTIVATE = DISPID_EVMETH_ONBEFOREDEACTIVATE;

        //    DISPIDs for interface IHTMLDOMNode
        public const int DISPID_IHTMLDOMNODE_NODETYPE = DISPID_ELEMENT + 46;
        public const int DISPID_IHTMLDOMNODE_PARENTNODE = DISPID_ELEMENT + 47;
        public const int DISPID_IHTMLDOMNODE_HASCHILDNODES = DISPID_ELEMENT + 48;
        public const int DISPID_IHTMLDOMNODE_CHILDNODES = DISPID_ELEMENT + 49;
        public const int DISPID_IHTMLDOMNODE_ATTRIBUTES = DISPID_ELEMENT + 50;
        public const int DISPID_IHTMLDOMNODE_INSERTBEFORE = DISPID_ELEMENT + 51;
        public const int DISPID_IHTMLDOMNODE_REMOVECHILD = DISPID_ELEMENT + 52;
        public const int DISPID_IHTMLDOMNODE_REPLACECHILD = DISPID_ELEMENT + 53;
        public const int DISPID_IHTMLDOMNODE_CLONENODE = DISPID_ELEMENT + 61;
        public const int DISPID_IHTMLDOMNODE_REMOVENODE = DISPID_ELEMENT + 66;
        public const int DISPID_IHTMLDOMNODE_SWAPNODE = DISPID_ELEMENT + 68;
        public const int DISPID_IHTMLDOMNODE_REPLACENODE = DISPID_ELEMENT + 67;
        public const int DISPID_IHTMLDOMNODE_APPENDCHILD = DISPID_ELEMENT + 73;
        public const int DISPID_IHTMLDOMNODE_NODENAME = DISPID_ELEMENT + 74;
        public const int DISPID_IHTMLDOMNODE_NODEVALUE = DISPID_ELEMENT + 75;
        public const int DISPID_IHTMLDOMNODE_FIRSTCHILD = DISPID_ELEMENT + 76;
        public const int DISPID_IHTMLDOMNODE_LASTCHILD = DISPID_ELEMENT + 77;
        public const int DISPID_IHTMLDOMNODE_PREVIOUSSIBLING = DISPID_ELEMENT + 78;
        public const int DISPID_IHTMLDOMNODE_NEXTSIBLING = DISPID_ELEMENT + 79;

        public const int DISPID_COLLECTION_MIN = 1000000;
        public const int DISPID_COLLECTION_MAX = 2999999;
        public const int DISPID_COLLECTION = (DISPID_NORMAL_FIRST + 500);
        public const int DISPID_VALUE = 0;

        //The following DISPID is reserved to indicate the param
        // that is the right-hand-side (or "put" value) of a PropertyPut
        public const int DISPID_PROPERTYPUT = -3;

        // DISPID reserved for the standard "NewEnum" method
        public const int DISPID_NEWENUM = -4;

        //    DISPIDs for interface IHTMLDOMChildrenCollection
        public const int DISPID_IHTMLDOMCHILDRENCOLLECTION_LENGTH = DISPID_COLLECTION;
        public const int DISPID_IHTMLDOMCHILDRENCOLLECTION__NEWENUM = DISPID_NEWENUM;
        public const int DISPID_IHTMLDOMCHILDRENCOLLECTION_ITEM = DISPID_VALUE;

        //    DISPIDs for interface IHTMLFramesCollection2
        public const int DISPID_IHTMLFRAMESCOLLECTION2_ITEM = 0;
        public const int DISPID_IHTMLFRAMESCOLLECTION2_LENGTH = 1001;

        //    DISPIDs for interface IHTMLWindow2
        public const int DISPID_IHTMLWINDOW2_FRAMES = 1100;
        public const int DISPID_IHTMLWINDOW2_DEFAULTSTATUS = 1101;
        public const int DISPID_IHTMLWINDOW2_STATUS = 1102;
        public const int DISPID_IHTMLWINDOW2_SETTIMEOUT = 1172;
        public const int DISPID_IHTMLWINDOW2_CLEARTIMEOUT = 1104;
        public const int DISPID_IHTMLWINDOW2_ALERT = 1105;
        public const int DISPID_IHTMLWINDOW2_CONFIRM = 1110;
        public const int DISPID_IHTMLWINDOW2_PROMPT = 1111;
        public const int DISPID_IHTMLWINDOW2_IMAGE = 1125;
        public const int DISPID_IHTMLWINDOW2_LOCATION = 14;
        public const int DISPID_IHTMLWINDOW2_HISTORY = 2;
        public const int DISPID_IHTMLWINDOW2_CLOSE = 3;
        public const int DISPID_IHTMLWINDOW2_OPENER = 4;
        public const int DISPID_IHTMLWINDOW2_NAVIGATOR = 5;
        public const int DISPID_IHTMLWINDOW2_NAME = 11;
        public const int DISPID_IHTMLWINDOW2_PARENT = 12;
        public const int DISPID_IHTMLWINDOW2_OPEN = 13;
        public const int DISPID_IHTMLWINDOW2_SELF = 20;
        public const int DISPID_IHTMLWINDOW2_TOP = 21;
        public const int DISPID_IHTMLWINDOW2_WINDOW = 22;
        public const int DISPID_IHTMLWINDOW2_NAVIGATE = 25;
        public const int DISPID_IHTMLWINDOW2_ONFOCUS = DISPID_EVPROP_ONFOCUS;
        public const int DISPID_IHTMLWINDOW2_ONBLUR = DISPID_EVPROP_ONBLUR;
        public const int DISPID_IHTMLWINDOW2_ONLOAD = DISPID_EVPROP_ONLOAD;
        public const int DISPID_IHTMLWINDOW2_ONBEFOREUNLOAD = DISPID_EVPROP_ONBEFOREUNLOAD;
        public const int DISPID_IHTMLWINDOW2_ONUNLOAD = DISPID_EVPROP_ONUNLOAD;
        public const int DISPID_IHTMLWINDOW2_ONHELP = DISPID_EVPROP_ONHELP;
        public const int DISPID_IHTMLWINDOW2_ONERROR = DISPID_EVPROP_ONERROR;
        public const int DISPID_IHTMLWINDOW2_ONRESIZE = DISPID_EVPROP_ONRESIZE;
        public const int DISPID_IHTMLWINDOW2_ONSCROLL = DISPID_EVPROP_ONSCROLL;
        public const int DISPID_IHTMLWINDOW2_DOCUMENT = 1151;
        public const int DISPID_IHTMLWINDOW2_EVENT = 1152;
        public const int DISPID_IHTMLWINDOW2__NEWENUM = 1153;
        public const int DISPID_IHTMLWINDOW2_SHOWMODALDIALOG = 1154;
        public const int DISPID_IHTMLWINDOW2_SHOWHELP = 1155;
        public const int DISPID_IHTMLWINDOW2_SCREEN = 1156;
        public const int DISPID_IHTMLWINDOW2_OPTION = 1157;
        public const int DISPID_IHTMLWINDOW2_FOCUS = 1158;
        public const int DISPID_IHTMLWINDOW2_CLOSED = 23;
        public const int DISPID_IHTMLWINDOW2_BLUR = 1159;
        public const int DISPID_IHTMLWINDOW2_SCROLL = 1160;
        public const int DISPID_IHTMLWINDOW2_CLIENTINFORMATION = 1161;
        public const int DISPID_IHTMLWINDOW2_SETINTERVAL = 1173;
        public const int DISPID_IHTMLWINDOW2_CLEARINTERVAL = 1163;
        public const int DISPID_IHTMLWINDOW2_OFFSCREENBUFFERING = 1164;
        public const int DISPID_IHTMLWINDOW2_EXECSCRIPT = 1165;
        public const int DISPID_IHTMLWINDOW2_TOSTRING = 1166;
        public const int DISPID_IHTMLWINDOW2_SCROLLBY = 1167;
        public const int DISPID_IHTMLWINDOW2_SCROLLTO = 1168;
        public const int DISPID_IHTMLWINDOW2_MOVETO = 6;
        public const int DISPID_IHTMLWINDOW2_MOVEBY = 7;
        public const int DISPID_IHTMLWINDOW2_RESIZETO = 9;
        public const int DISPID_IHTMLWINDOW2_RESIZEBY = 8;
        public const int DISPID_IHTMLWINDOW2_EXTERNAL = 1169;

        //    DISPIDs for interface IHTMLImgElement
        public const int DISPID_IMGBASE = DISPID_NORMAL_FIRST;
        public const int DISPID_IMG = (DISPID_IMGBASE + 1000);
        public const int DISPID_A_READYSTATE = (DISPID_A_FIRST + 116); // ready state
        public const int STDPROPID_XOBJ_CONTROLALIGN = (DISPID_XOBJ_BASE + 0x49);
        public const int STDPROPID_XOBJ_NAME = (DISPID_XOBJ_BASE + 0x0);
        public const int STDPROPID_XOBJ_WIDTH = (DISPID_XOBJ_BASE + 0x5);
        public const int STDPROPID_XOBJ_HEIGHT = (DISPID_XOBJ_BASE + 0x6);

        public const int DISPID_IHTMLIMGELEMENT_ISMAP = DISPID_IMG + 2;
        public const int DISPID_IHTMLIMGELEMENT_USEMAP = DISPID_IMG + 8;
        public const int DISPID_IHTMLIMGELEMENT_MIMETYPE = DISPID_IMG + 10;
        public const int DISPID_IHTMLIMGELEMENT_FILESIZE = DISPID_IMG + 11;
        public const int DISPID_IHTMLIMGELEMENT_FILECREATEDDATE = DISPID_IMG + 12;
        public const int DISPID_IHTMLIMGELEMENT_FILEMODIFIEDDATE = DISPID_IMG + 13;
        public const int DISPID_IHTMLIMGELEMENT_FILEUPDATEDDATE = DISPID_IMG + 14;
        public const int DISPID_IHTMLIMGELEMENT_PROTOCOL = DISPID_IMG + 15;
        public const int DISPID_IHTMLIMGELEMENT_HREF = DISPID_IMG + 16;
        public const int DISPID_IHTMLIMGELEMENT_NAMEPROP = DISPID_IMG + 17;
        public const int DISPID_IHTMLIMGELEMENT_BORDER = DISPID_IMGBASE + 4;
        public const int DISPID_IHTMLIMGELEMENT_VSPACE = DISPID_IMGBASE + 5;
        public const int DISPID_IHTMLIMGELEMENT_HSPACE = DISPID_IMGBASE + 6;
        public const int DISPID_IHTMLIMGELEMENT_ALT = DISPID_IMGBASE + 2;
        public const int DISPID_IHTMLIMGELEMENT_SRC = DISPID_IMGBASE + 3;
        public const int DISPID_IHTMLIMGELEMENT_LOWSRC = DISPID_IMGBASE + 7;
        public const int DISPID_IHTMLIMGELEMENT_VRML = DISPID_IMGBASE + 8;
        public const int DISPID_IHTMLIMGELEMENT_DYNSRC = DISPID_IMGBASE + 9;
        public const int DISPID_IHTMLIMGELEMENT_READYSTATE = DISPID_A_READYSTATE;
        public const int DISPID_IHTMLIMGELEMENT_COMPLETE = DISPID_IMGBASE + 10;
        public const int DISPID_IHTMLIMGELEMENT_LOOP = DISPID_IMGBASE + 11;
        public const int DISPID_IHTMLIMGELEMENT_ALIGN = STDPROPID_XOBJ_CONTROLALIGN;
        public const int DISPID_IHTMLIMGELEMENT_ONLOAD = DISPID_EVPROP_ONLOAD;
        public const int DISPID_IHTMLIMGELEMENT_ONERROR = DISPID_EVPROP_ONERROR;
        public const int DISPID_IHTMLIMGELEMENT_ONABORT = DISPID_EVPROP_ONABORT;
        public const int DISPID_IHTMLIMGELEMENT_NAME = STDPROPID_XOBJ_NAME;
        public const int DISPID_IHTMLIMGELEMENT_WIDTH = STDPROPID_XOBJ_WIDTH;
        public const int DISPID_IHTMLIMGELEMENT_HEIGHT = STDPROPID_XOBJ_HEIGHT;
        public const int DISPID_IHTMLIMGELEMENT_START = DISPID_IMGBASE + 13;

        //    DISPIDs for interface IHTMLTxtRange
        public const int DISPID_RANGE = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLTXTRANGE_HTMLTEXT = DISPID_RANGE + 3;
        public const int DISPID_IHTMLTXTRANGE_TEXT = DISPID_RANGE + 4;
        public const int DISPID_IHTMLTXTRANGE_PARENTELEMENT = DISPID_RANGE + 6;
        public const int DISPID_IHTMLTXTRANGE_DUPLICATE = DISPID_RANGE + 8;
        public const int DISPID_IHTMLTXTRANGE_INRANGE = DISPID_RANGE + 10;
        public const int DISPID_IHTMLTXTRANGE_ISEQUAL = DISPID_RANGE + 11;
        public const int DISPID_IHTMLTXTRANGE_SCROLLINTOVIEW = DISPID_RANGE + 12;
        public const int DISPID_IHTMLTXTRANGE_COLLAPSE = DISPID_RANGE + 13;
        public const int DISPID_IHTMLTXTRANGE_EXPAND = DISPID_RANGE + 14;
        public const int DISPID_IHTMLTXTRANGE_MOVE = DISPID_RANGE + 15;
        public const int DISPID_IHTMLTXTRANGE_MOVESTART = DISPID_RANGE + 16;
        public const int DISPID_IHTMLTXTRANGE_MOVEEND = DISPID_RANGE + 17;
        public const int DISPID_IHTMLTXTRANGE_SELECT = DISPID_RANGE + 24;
        public const int DISPID_IHTMLTXTRANGE_PASTEHTML = DISPID_RANGE + 26;
        public const int DISPID_IHTMLTXTRANGE_MOVETOELEMENTTEXT = DISPID_RANGE + 1;
        public const int DISPID_IHTMLTXTRANGE_SETENDPOINT = DISPID_RANGE + 25;
        public const int DISPID_IHTMLTXTRANGE_COMPAREENDPOINTS = DISPID_RANGE + 18;
        public const int DISPID_IHTMLTXTRANGE_FINDTEXT = DISPID_RANGE + 19;
        public const int DISPID_IHTMLTXTRANGE_MOVETOPOINT = DISPID_RANGE + 20;
        public const int DISPID_IHTMLTXTRANGE_GETBOOKMARK = DISPID_RANGE + 21;
        public const int DISPID_IHTMLTXTRANGE_MOVETOBOOKMARK = DISPID_RANGE + 9;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDSUPPORTED = DISPID_RANGE + 27;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDENABLED = DISPID_RANGE + 28;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDSTATE = DISPID_RANGE + 29;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDINDETERM = DISPID_RANGE + 30;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDTEXT = DISPID_RANGE + 31;
        public const int DISPID_IHTMLTXTRANGE_QUERYCOMMANDVALUE = DISPID_RANGE + 32;
        public const int DISPID_IHTMLTXTRANGE_EXECCOMMAND = DISPID_RANGE + 33;
        public const int DISPID_IHTMLTXTRANGE_EXECCOMMANDSHOWHELP = DISPID_RANGE + 34;

        //    DISPIDs for interface IHTMLDOMAttribute
        public const int DISPID_DOMATTRIBUTE = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLDOMATTRIBUTE_NODENAME = DISPID_DOMATTRIBUTE;
        public const int DISPID_IHTMLDOMATTRIBUTE_NODEVALUE = DISPID_DOMATTRIBUTE + 2;
        public const int DISPID_IHTMLDOMATTRIBUTE_SPECIFIED = DISPID_DOMATTRIBUTE + 1;

        //    DISPIDs for interface IHTMLAttributeCollection
        public const int DISPID_IHTMLATTRIBUTECOLLECTION_LENGTH = DISPID_COLLECTION;
        public const int DISPID_IHTMLATTRIBUTECOLLECTION__NEWENUM = DISPID_NEWENUM;
        public const int DISPID_IHTMLATTRIBUTECOLLECTION_ITEM = DISPID_VALUE;

        //    DISPIDs for interface IHTMLStyleSheetsCollection
        public const int DISPID_STYLESHEETS_COL = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLSTYLESHEETSCOLLECTION_LENGTH = DISPID_STYLESHEETS_COL + 1;
        public const int DISPID_IHTMLSTYLESHEETSCOLLECTION__NEWENUM = DISPID_NEWENUM;
        public const int DISPID_IHTMLSTYLESHEETSCOLLECTION_ITEM = DISPID_VALUE;

        //    DISPIDs for interface IHTMLSelectionObject
        public const int DISPID_SELECTOBJ = DISPID_NORMAL_FIRST;
        public const int DISPID_IHTMLSELECTIONOBJECT_CREATERANGE = DISPID_SELECTOBJ + 1;
        public const int DISPID_IHTMLSELECTIONOBJECT_EMPTY = DISPID_SELECTOBJ + 2;
        public const int DISPID_IHTMLSELECTIONOBJECT_CLEAR = DISPID_SELECTOBJ + 3;
        public const int DISPID_IHTMLSELECTIONOBJECT_TYPE = DISPID_SELECTOBJ + 4;

        // DISPIDS for interface IHTMLBodyElement
        public const int DISPID_TEXTSITE = DISPID_NORMAL_FIRST;
        public const int DISPID_BODY = (DISPID_TEXTSITE + 1000);
        public const int DISPID_IHTMLBODYELEMENT_CREATETEXTRANGE = DISPID_BODY + 13;
    }
}