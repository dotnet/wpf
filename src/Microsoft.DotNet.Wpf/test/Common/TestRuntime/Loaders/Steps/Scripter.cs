// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Xml;
// TODO-Miguep: uncomment for clickonce
//using Microsoft.Test.Deployment;
using Microsoft.Test.Logging;
using Microsoft.Test.Globalization;
using MTI = Microsoft.Test.Input;
using Microsoft.Win32;
using Microsoft.Test.Win32;
using MTUR = Microsoft.Test.Utilities.Reflection;
using Microsoft.Test.Utilities;

namespace Microsoft.Test.Utilities.Scripter
{
    /// <summary>
    /// Finder helps to find nodes through UIAutomation
    /// </summary>
    internal class UIFinder
    {
        /// <summary>
        /// FindElement
        /// </summary>
        /// <param name="log"></param>
        /// <param name="context"></param>
        /// <param name="conditions"></param>
        /// <returns></returns>
        internal static AutomationElement FindElement(ILog log, AutomationElement context, PropertyCondition[] conditions)
        {
            // if no conditions, there's no search to do: just return the context, will be used as target
            if (conditions == null)
            {
                return (context);
            }

            // create the condition to find
            System.Windows.Automation.Condition condition = null;
            if (conditions.Length <= 0)
            {
                throw (new ArgumentException("No conditions specified"));
            }
            else if (conditions.Length == 1)
            {
                condition = conditions[0];
            }
            else
            {
                AndCondition ac = new AndCondition(conditions);
                condition = ac;
            }

            // find the element
            CacheRequest creq = new CacheRequest();
            creq.TreeFilter = Automation.ControlViewCondition;
            using (creq.Activate())
            {
                AutomationElement e = AutomationContext(context);
                AutomationElement target = e.FindFirst(TreeScope.Subtree, condition);
                return (target);
            }
        }

        /// <summary>
        /// FindElement
        /// </summary>
        /// <param name="log"></param>
        /// <param name="className"></param>
        /// <param name="windowText"></param>
        /// <returns></returns>
        internal static AutomationElement FindElement(ILog log, string className, string windowText)
        {
            while (true)
            {
                // find the element
                CacheRequest creq = new CacheRequest();
                creq.TreeFilter = Automation.ControlViewCondition;
                using (creq.Activate())
                {
                    WindowFinder wf = new WindowFinder();
                    int timeoutSec = 3;
                    IntPtr handle = wf.FindWindow(log, timeoutSec, className, windowText);
                    if (handle != null && handle != IntPtr.Zero)
                    {
                        AutomationElement target = AutomationElement.FromHandle(handle);
                        return (target);
                    }
                    else
                    {
                        log.StatusMessage = "Attempt failed...";
                    }
                }
            }
        }

        /// <summary>
        /// AutomationContext
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static AutomationElement AutomationContext(AutomationElement e)
        {
            if (e == null)
            {
                return (AutomationElement.RootElement);
            }
            else
            {
                return (e);
            }
        }
    }

    internal class LocalizedStrings
    {
        private Hashtable resourceTable = new Hashtable(50);
        char[] separator = { ',' };

        /// <summary>
        /// LocalizedStrings
        /// </summary>
        internal LocalizedStrings()
        {
            // fill table
            resourceTable.Add("IE_SECURITY_ALERT", @"$(WINDIR)\system32\shdoclc.dll,1206");
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal string Get(string key)
        {
            string value = (string)resourceTable[key];
            if (value == null)
            {
                return (null);
            }

            // get the value for that key: (library, id)
            string[] split = value.Split(separator);
            if (split.Length != 2)
            {
                // ignore badly formed values
                return (null);
            }

            // extract requested string from resource
            string library = split[0];
            string id = split[1];

            // parse library, it may contain references to env variables
            const string pattern = "(\\$\\(\\w+\\))";
            StringUtils.MatchProcessor m = new StringUtils.MatchProcessor(ParseLibrary);
            library = StringUtils.ProcessMatches(library, pattern, m);

            // get string resource
            UnmanagedResourceHelper urh = new UnmanagedResourceHelper(library);
            string s = urh.GetResource(id, UnmanagedResourceHelper.ResourceType.String);
            return (s);
        }

        /// <summary>
        /// ParseLibrary
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string ParseLibrary(string text)
        {
            // extract the define name
            const string pattern = "\\$\\((\\w+)\\)";
            string[] matches = StringUtils.GetMatches(text, pattern);
            if (matches.Length != 1)
            {
                throw (new System.Xml.XmlException("invalid define: " + text));
            }
            string name = matches[0];

            // replace with env variable
            return (Environment.GetEnvironmentVariable(name));
        }
    }

    /// <summary>
    /// Window finder class
    /// </summary>
    internal class WindowFinder
    {
        /// <summary>
        /// needed for thread synchronization
        /// </summary>
        AutoResetEvent signal = new AutoResetEvent(false);

        /// <summary>
        /// log object
        /// </summary>
        ILog log = null;

        /// <summary>
        /// class name to find
        /// </summary>
        string targetClassName = null;
        Regex targetClassNameRegEx = null;

        /// <summary>
        /// windows name to find
        /// </summary>
        string targetWindowName = null;
        Regex targetWindowNameRegEx = null;

        /// <summary>
        /// found hwnd
        /// </summary>
        IntPtr foundHwnd = IntPtr.Zero;

        /// <summary>
        /// FindWindow
        /// </summary>
        /// <param name="log"></param>
        /// <param name="timeoutSeconds"></param>
        /// <param name="className"></param>
        /// <param name="windowName"></param>
        /// <returns></returns>
        internal IntPtr FindWindow(ILog log, int timeoutSeconds, string className, string windowName)
        {
            // window not found yet
            foundHwnd = IntPtr.Zero;

            // either the target class or window must be provided
            if (className == null && windowName == null)
            {
                throw (new ArgumentNullException("className or windowName must be NOT null"));
            }

            // set log
            this.log = log;

            // analyze whether to use regex or not for class name and window name
            const string regExSyntaxPattern = "RegEx:.*";

            if (className != null)
            {
                if (StringUtils.Matches(regExSyntaxPattern, className))
                {
                    targetClassNameRegEx = ExtractRegEx(className);
                }
                else
                {
                    targetClassName = className;
                }
            }

            if (windowName != null)
            {
                if (StringUtils.Matches(regExSyntaxPattern, windowName))
                {
                    targetWindowNameRegEx = ExtractRegEx(windowName);
                }
                else
                {
                    targetWindowName = windowName;
                }
            }

            // create the worker thread
            ThreadPool.QueueUserWorkItem(new WaitCallback(EnumChildWindows), signal);

            // wait for work to end
            signal.WaitOne(new TimeSpan(0, 0, timeoutSeconds), false);

            // return the found window
            return (foundHwnd);
        }

        /// <summary>
        /// ExtractRegEx takes a syntax like 'RegEx:regular expression' and returns a Regex based on it
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private Regex ExtractRegEx(string s)
        {
            // find the first ':'
            int splitPos = s.IndexOf(':');
            string pattern = s.Substring(splitPos + 1);
            return (new Regex(pattern));
        }

        /// <summary>
        /// EnumChildWindows
        /// </summary>
        /// <param name="o"></param>
        private void EnumChildWindows(object o)
        {
            EnumChildWindows(IntPtr.Zero);
        }

        /// <summary>
        /// EnumChildWindows
        /// </summary>
        /// <param name="hwnd"></param>
        private void EnumChildWindows(IntPtr hwnd)
        {
            // set the callback
            Win32Helper.EnumChildrenCallback callback = new Win32Helper.EnumChildrenCallback(EnumChildWindowsCallback);

            // enumerate windows; start from the desktop
            Win32Helper.EnumChildWindows(hwnd, callback, IntPtr.Zero);
        }

        /// <summary>
        /// IsWindowNameMatchingRequired
        /// </summary>
        private bool IsWindowNameMatchingRequired
        {
            get
            {
                return ((targetWindowName != null && targetWindowName != String.Empty) || targetWindowNameRegEx != null);
            }
        }

        /// <summary>
        /// IsClassNameMatchingRequired
        /// </summary>
        private bool IsClassNameMatchingRequired
        {
            get
            {
                return ((targetClassName != null && targetClassName != String.Empty) || targetClassNameRegEx != null);
            }
        }

        /// <summary>
        /// EnumChildWindowsCallback
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumChildWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            // window was already found
            if (foundHwnd != IntPtr.Zero)
            {
                log.StatusMessage = "Window already found";
                return (false);
            }

            bool matchesWindowName = false;
            bool matchesClassName = false;

            // get the text of the window
            if (IsWindowNameMatchingRequired)
            {
                const int count = 256;//maxpath
                StringBuilder text = new StringBuilder(count);
                int result = Win32Helper.GetWindowText(hwnd, text, count);
                if (result != 0)
                {
                    if (NameMatches(targetWindowName, targetWindowNameRegEx, text.ToString()))
                    {
                        matchesWindowName = true;
                    }
                }
            }

            // get the class name of the window
            if (IsClassNameMatchingRequired)
            {
                const int count = 256;//maxpath
                StringBuilder text = new StringBuilder(count);
                int result = Win32Helper.GetClassName(hwnd, text, count);
                if (result != 0)
                {
                    if (NameMatches(targetClassName, targetClassNameRegEx, text.ToString()))
                    {
                        matchesClassName = true;
                    }
                }
            }

            if ((matchesWindowName || !IsWindowNameMatchingRequired) && (matchesClassName || !IsClassNameMatchingRequired))
            {
                // it was either found or no search was made. either way, search ends
                string foundClassName = String.Empty;
                if (targetClassName != null)
                {
                    foundClassName = targetClassName;
                }
                else if (targetClassNameRegEx != null)
                {
                    foundClassName = targetClassNameRegEx.ToString();
                }

                string foundWindowName = String.Empty;
                if (targetWindowName != null)
                {
                    foundWindowName = targetWindowName;
                }
                else if (targetWindowNameRegEx != null)
                {
                    foundWindowName = targetWindowNameRegEx.ToString();
                }

                log.StatusMessage = "Window Found (hwnd " + hwnd.ToInt32().ToString() + "), ClassName = '" + foundClassName + "', WindowText = '" + foundWindowName + "'";
                foundHwnd = hwnd;
                ((AutoResetEvent)signal).Set();
                return (false);
            }
            else
            {
                EnumChildWindows(hwnd);
                return (true);
            }
        }

        /// <summary>
        /// Helper function to isolate logic for determining if name matches or not
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="targetRegex"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool NameMatches(string targetName, Regex targetRegex, string name)
        {
            // expect either one to be not null
            if (targetName == null && targetRegex == null)
            {
                throw (new ArgumentNullException("No name or regular expression was specified"));
            }

            // analyze direct name match
            if (targetName != null)
            {
                return (targetName == name);
            }

            // analyze regex match
            if (targetRegex != null)
            {
                return (targetRegex.Match(name).Success);
            }

            // return
            return (false);
        }
    }

    /// <summary>
    /// AutomationPropertyFactory
    /// </summary>
    internal class AutomationPropertyFactory
    {
        /// <summary>
        /// AvailableAutomationProperty
        /// </summary>
        private struct AvailableAutomationProperty
        {
            internal const string ControlType = "ControlType";
            internal const string LocalizedControlType = "LocalizedControlType";
            internal const string Name = "Name";
            internal const string AutomationId = "AutomationId";
        }

        /// <summary>
        /// FromString
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static AutomationProperty FromString(string s)
        {
            switch (s)
            {
                case AvailableAutomationProperty.ControlType:
                    return (AutomationElement.ControlTypeProperty);
                case AvailableAutomationProperty.LocalizedControlType:
                    return (AutomationElement.LocalizedControlTypeProperty);
                case AvailableAutomationProperty.Name:
                    return (AutomationElement.NameProperty);
                case AvailableAutomationProperty.AutomationId:
                    return (AutomationElement.AutomationIdProperty);
                default:
                    return (null);
            }
        }
    }

    /// <summary>
    /// Command
    /// </summary>
    internal class Command
    {
        /// <summary>
        /// MaximizeWindow
        /// </summary>
        /// <param name="target"></param>
        internal static void MaximizeWindow(AutomationElement target)
        {
            WindowPattern wp = target.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
            if (wp.Current.CanMaximize)
            {
                wp.SetWindowVisualState(WindowVisualState.Maximized);
            }
            Thread.Sleep(100);
        }

        /// <summary>
        /// MinimizeWindow
        /// </summary>
        /// <param name="target"></param>
        internal static void MinimizeWindow(AutomationElement target)
        {
            WindowPattern wp = target.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
            if (wp.Current.CanMinimize)
            {
                wp.SetWindowVisualState(WindowVisualState.Minimized);
            }
            Thread.Sleep(100);
        }

        /// <summary>
        /// NormalizeWindow
        /// </summary>
        /// <param name="target"></param>
        internal static void NormalizeWindow(AutomationElement target)
        {
            WindowPattern wp = target.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
            wp.SetWindowVisualState(WindowVisualState.Normal);
            Thread.Sleep(100);
        }

        /// <summary>
        /// ResizeWindow
        /// </summary>
        /// <param name="target"></param>
        /// <param name="factor"></param>
        internal static void ResizeWindow(AutomationElement target, double factor)
        {
            // get the bottom-right corner
            Rect r = target.Current.BoundingRectangle;

            MTI.Input.SendMouseInput(r.Right - 1, r.Bottom - 1, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move | MTI.SendMouseInputFlags.LeftDown);
            Thread.Sleep(100);

            MTI.Input.SendMouseInput(r.Right * factor, r.Bottom * factor, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move | MTI.SendMouseInputFlags.LeftUp);
            Thread.Sleep(100);
        }

        /// <summary>
        /// MoveTo
        /// </summary>
        /// <param name="target"></param>
        internal static void MoveTo(AutomationElement target)
        {
            // get target bounding rectangle
            Rect r = target.Current.BoundingRectangle;

            // move the mouse there
            MTI.Input.SendMouseInput((r.Left + r.Right) / 2, (r.Bottom + r.Top) / 2, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move);
        }

        /// <summary>
        /// MoveToAndClick
        /// </summary>
        /// <param name="target"></param>
        internal static void MoveToAndClick(AutomationElement target)
        {
            MTI.Input.MoveToAndClick(target);
        }
    }

    /// <summary>
    /// ReaderProvider
    /// </summary>
    internal class ReaderProvider
    {
        /// <summary>
        /// keep the available readers
        /// </summary>
        private Stack readers = new Stack();

        /// <summary>
        /// Returns the current reader
        /// </summary>
        internal XmlTextReader Current
        {
            get
            {
                if (readers.Count == 0)
                {
                    return (null);
                }
                return (readers.Peek() as XmlTextReader);
            }
        }

        /// <summary>
        /// Adds a reader to the collection
        /// </summary>
        /// <param name="r"></param>
        internal void Add(XmlTextReader r)
        {
            readers.Push(r);
        }

        /// <summary>
        /// Removes current reader
        /// </summary>
        internal void Remove()
        {
            XmlTextReader r = (XmlTextReader)readers.Pop();
            r.Close();
        }
    }

    /// <summary>
    /// DefinesTable - a singleton that keeps the defines made on a script
    /// </summary>
    sealed class DefinesTable
    {
        /// <summary>
        /// .ctor
        /// </summary>
        private DefinesTable()
        {
        }

        /// <summary>
        /// keeps the global instance of this class
        /// </summary>
        static DefinesTable instance = null;

        /// <summary>
        /// gets the global instance of this class
        /// </summary>
        internal static DefinesTable Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DefinesTable();
                }
                return instance;
            }
        }

        /// <summary>
        /// returns the attribute value after parsing defines
        /// </summary>
        /// <param name="r"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private string GetAttributeImpl(XmlTextReader r, string attr)
        {
            // get the attribute value
            string text = r.GetAttribute(attr);
            if (text == null)
            {
                // if attribute is not defined, return
                return (null);
            }

            // replace the defines with the appropriate values
            StringUtils.MatchProcessor m = new StringUtils.MatchProcessor(ExtractDefineStructure);
            const string pattern = "(\\$\\([a-zA-Z0-9 .:=,_]+\\))";
            string result = StringUtils.ProcessMatches(text, pattern, m);

            // return it
            return (result);
        }

        /// <summary>
        /// processes a define structure of the form '$(defineName)' and returns the replacement
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string ExtractDefineStructure(string text)
        {
            Singleton.Instance.Log.StatusMessage = "Searching definition for '" + text + "'...";

            // extract the define name
            const string pattern = "\\$\\(([a-zA-Z0-9 .:=,_]+)\\)";
            string[] matches = StringUtils.GetMatches(text, pattern);
            if (matches.Length != 1)
            {
                throw (new System.Xml.XmlException("invalid define: " + text));
            }
            string name = matches[0];

            // (1) look for it in the repository
            string value = Table[name] as string;
            if (value != null)
            {
                Singleton.Instance.Log.StatusMessage = "Definition found: '" + value + "'";
                return (value);
            }

            // (2) process special cases
            switch(name)
            {
                case "LOCAL_MORE_INFORMATION":
                    return (UnhandledExceptionPageResourcesHelper.ExtractFromUnhandledExceptionPage(UnhandledExceptionPageResourcesHelper.Resource.MoreInfoButtonText));
                case "LOCAL_LESS_INFORMATION":
                    return (UnhandledExceptionPageResourcesHelper.ExtractFromUnhandledExceptionPage(UnhandledExceptionPageResourcesHelper.Resource.LessInfoButtonText));
                case "WPF_UNHANDLED_EXCEPTION_PAGE_TITLE":
                    return (UnhandledExceptionPageResourcesHelper.ExtractFromUnhandledExceptionPage(UnhandledExceptionPageResourcesHelper.Resource.PageTitle));
                default:
                    break;
            }

            // (3) look for it in the localized strings table
            string localizedString = localizedStrings.Get(name);
            if (localizedString != null)
            {
                Singleton.Instance.Log.StatusMessage = "Definition found: '" + localizedString + "'";
                return (localizedString);
            }

            // (4) look for it in the registry variables repository
            string reg = RegistryVar.Get(name);
            if (reg != null)
            {
                Singleton.Instance.Log.StatusMessage = "Definition found: '" + reg + "'";
                return (reg);
            }

            // (5) look for it as an environment variable
            string env = Environment.GetEnvironmentVariable(name);
            if (env != null)
            {
                Singleton.Instance.Log.StatusMessage = "Definition found: '" + env + "'";
                return (env);
            }

            // (6) call a static method that returns a string
            // name should come in this form: "assembly::type::method"
            string[] sep = { "::" };
            string[] callData = name.Split(sep, StringSplitOptions.None);
            if (callData.Length == 3)
            {
                string call = GetStringFromStaticCall(callData[0], callData[1], callData[2]);
                if (call != null)
                {
                    Singleton.Instance.Log.StatusMessage = "Definition found: '" + call + "'";
                    return (call);
                }
            }

            // (7) get a string from an unmanaged resource
            // name should come in this form: "dll full path::string ID"
            // example: "C:\WINDOWS\system32\shdocvw.dll::858"
            string[] uresData = name.Split(sep, StringSplitOptions.None);
            if (uresData.Length == 2)
            {
                string s = GetStringFromUnmanagedResource(uresData[0], uresData[1]);
                if (s != null)
                {
                    Singleton.Instance.Log.StatusMessage = "Definition found: '" + s + "'";
                    return (s);
                }
            }

            // (8) get a string from a managed resource
            // name should come in this form: "assembly::resource::string name"
            // example: "DummyLibrary::DummyLibrary.Resource1::Sample"
            string[] mresData = name.Split(sep, StringSplitOptions.None);
            if (mresData.Length == 3)
            {
                string s = GetStringFromManagedResource(mresData[0], mresData[1], mresData[2]);
                if (s != null)
                {
                    Singleton.Instance.Log.StatusMessage = "Definition found: '" + s + "'";
                    return (s);
                }
            }

            // not found
            Singleton.Instance.Log.StatusMessage = "Warning: Definition not found";
            return (null);
        }

        /// <summary>
        /// GetStringFromStaticCall
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private string GetStringFromStaticCall(string assembly, string type, string method)
        {
            string s = null;
            try
            {
                Assembly a = MTUR.Utils.Load(assembly);
                Type t = MTUR.Utils.GetType(a, type);
                MethodInfo mi = t.GetMethod(method);
                s = MTUR.Utils.Invoke(mi, null, null) as string;
            }
            catch (Exception)
            {
            }
            return (s);
        }

        /// <summary>
        /// GetStringFromUnmanagedResource
        /// </summary>
        /// <param name="dll"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private string GetStringFromUnmanagedResource(string dll, string id)
        {
            string s = null;
            try
            {
                s = new UnmanagedResourceHelper(dll).GetString(Int32.Parse(id));
            }
            catch (Exception)
            {
            }
            return (s);
        }

        /// <summary>
        /// GetStringFromManagedResource
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetStringFromManagedResource(string assembly, string resource, string name)
        {
            string s = null;
            try
            {
                s = new ManagedResourceHelper(assembly, resource).GetString(name);
            }
            catch (Exception)
            {
            }
            return (s);
        }

        /// <summary>
        /// returns the attribute value after parsing defines
        /// </summary>
        /// <param name="r"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        internal static string GetAttribute(XmlTextReader r, string attr)
        {
            return (DefinesTable.Instance.GetAttributeImpl(r, attr));
        }

        /// <summary>
        /// table in which defines will be stored
        /// </summary>
        internal Hashtable Table = new Hashtable();

        /// <summary>
        /// table in which localized strings are stored
        /// </summary>
        private LocalizedStrings localizedStrings = new LocalizedStrings();
    }

    /// <summary>
    /// Condition
    /// </summary>
    internal class Condition
    {
        private string _condition = null;

        /// <summary>
        /// Operand
        /// </summary>
        internal struct Operand
        {
            internal const string IE = "IE";
            internal const string OSVersion = "OSVersion";
            internal const string OSName = "OSName";
            internal const string WOW = "WOW";
            internal const string Arch = "Arch";
        }

        /// <summary>
        /// Operator
        /// </summary>
        internal enum Operator
        {
            Equals,
            NotEquals
        }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="condition"></param>
        internal Condition(string condition)
        {
            _condition = condition;
        }

        /// <summary>
        /// Matches
        /// </summary>
        /// <returns></returns>
        internal bool Matches()
        {
            bool isOrCondition = _condition.Contains(Token.OR);
            bool isAndCondition = _condition.Contains(Token.AND);
            if (isAndCondition && isOrCondition)
            {
                throw (new ArgumentException("Conditions including AND and OR are not supported: '" + _condition + "'"));
            }
            if (isOrCondition)
            {
                return (EvaluateOrCondition(_condition));
            }
            else
            {
                return (EvaluateAndCondition(_condition));
            }
        }

        /// <summary>
        /// Tokens
        /// </summary>
        private struct Token
        {
            internal const string AND = " AND ";
            internal const string OR = " OR ";
        }

        /// <summary>
        /// EvaluateAndCondition
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private bool EvaluateAndCondition(string condition)
        {
            // get conditions
            string[] andSep = new string[1];
            andSep[0] = Token.AND;
            string[] andConditions = _condition.Split(andSep, StringSplitOptions.None);

            // process AND conditions
            foreach (string c in andConditions)
            {
                // check operator
                bool conditionMet = true;
                if (c.Contains("=="))
                {
                    conditionMet = EvaluateCondition(c, Operator.Equals);
                }
                else if (c.Contains("!="))
                {
                    conditionMet = EvaluateCondition(c, Operator.NotEquals);
                }
                else
                {
                    throw (new ArgumentException("Cannot parse condition: '" + c + "'"));
                }

                // if a condition is not met, no match
                if (!conditionMet)
                {
                    return (false);
                }
            }

            // unmet condition not found
            return (true);
        }

        /// <summary>
        /// EvaluateOrCondition
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private bool EvaluateOrCondition(string condition)
        {
            // get conditions
            string[] orSep = new string[1];
            orSep[0] = Token.OR;
            string[] orConditions = _condition.Split(orSep, StringSplitOptions.None);

            // process OR conditions
            foreach (string c in orConditions)
            {
                // check operator
                bool conditionMet = true;
                if (c.Contains("=="))
                {
                    conditionMet = EvaluateCondition(c, Operator.Equals);
                }
                else if (c.Contains("!="))
                {
                    conditionMet = EvaluateCondition(c, Operator.NotEquals);
                }
                else
                {
                    throw (new ArgumentException("Cannot parse condition: '" + c + "'"));
                }

                // if a condition is met, no match
                if (conditionMet)
                {
                    return (true);
                }
            }

            // met condition not found
            return (false);
        }

        /// <summary>
        /// EvaluateEqualsCondition
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool EvaluateCondition(string condition, Operator t)
        {
            // get conditions
            string[] sep = null;
            string[] sepEq = { "==" };
            string[] sepNEq = { "!=" };
            switch (t)
            {
                case Operator.Equals:
                    sep = sepEq;
                    break;
                case Operator.NotEquals:
                    sep = sepNEq;
                    break;
                default:
                    throw (new ArgumentException("Unknown operator: '" + condition + "'"));
            }
            string[] operands = condition.Split(sep, StringSplitOptions.None);

            // get first operand value
            string op1 = GetOperandValue(operands[0], operands[1], operands[0]);

            // get second operand value
            string op2 = GetOperandValue(operands[0], operands[1], operands[1]);

            // evaluate
            switch (t)
            {
                case Operator.Equals:
                    return (op1 == op2);
                case Operator.NotEquals:
                    return (op1 != op2);
                default:
                    throw (new ArgumentException("Unknown operator: '" + condition + "'"));
            }
        }

        /// <summary>
        /// GetOperandValue
        /// </summary>
        /// <param name="op1"></param>
        /// <param name="op2"></param>
        /// <param name="targetOp"></param>
        private string GetOperandValue(string op1, string op2, string targetOp)
        {
            op1 = op1.Trim();
            op2 = op2.Trim();
            targetOp = targetOp.Trim();
            switch (targetOp)
            {
                case Operand.Arch:
                    return (EnvironmentVariable.Get("PROCESSOR_ARCHITECTURE"));
                case Operand.IE:
                    return (IEHelper.GetIEVersion().ToString());
                case Operand.OSVersion:
                    return (Environment.OSVersion.Version.Major.ToString());
                case Operand.OSName:
                    return (OSVersion.Name);
                case Operand.WOW:
                    if (Win32Helper.IsWow64Process(op2))
                    {
                        return (op2);
                    }
                    else
                    {
                        return (targetOp);
                    }
                default:
                    return (targetOp);
            }
        }
    }

    /// <summary>
    /// Scripter
    /// </summary>
    internal class Scripter
    {
        ILog log = null;
        ReaderProvider readers = new ReaderProvider();

        /// <summary>
        /// .ctor
        /// </summary>
        internal Scripter()
        {
        }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="s">steps - xml</param>
        /// <param name="log"></param>
        internal Scripter(Stream s, ILog log)
        {
            // set the log
            this.log = log;

            // read steps
            Read(s);
        }

        /// <summary>
        /// ScripterTags
        /// </summary>
        private struct ScripterTags
        {
            internal const string Actions = "Actions";
            internal const string Action = "Action";
            internal const string If = "If";
            internal const string Condition = "Condition";
            internal const string Context = "Context";
            internal const string Include = "Include";
            internal const string Define = "Define";
            internal const string File = "File";
            internal const string Name = "Name";
            internal const string Value = "Value";
            internal const string WindowText = "WindowText";
            internal const string ClassName = "ClassName";
            internal const string ContinueIfNotFound = "ContinueIfNotFound";
        }

        /// <summary>
        /// ActionAttributes
        /// </summary>
        private struct ActionAttributes
        {
            internal const string Name = "Name";
        }

        /// <summary>
        /// ActionsAttributes
        /// </summary>
        private struct ActionsAttributes
        {
            internal const string Description = "Description";
        }

        /// <summary>
        /// Read steps from a stream
        /// </summary>
        /// <param name="s">steps - xml</param>
        private void Read(Stream s)
        {
            bool exit = false;
            Condition condition = null;
            bool conditionActive = false;

            // create an xml reader
            XmlTextReader rootReader = new XmlTextReader(s);
            rootReader.WhitespaceHandling = WhitespaceHandling.None;

            // put the reader in the stack
            readers.Add(rootReader);

            // first reader is the current one
            XmlTextReader reader = readers.Current;

            // read script: it can be either a context switch, an include, a header or an action
            reader.Read();
            AutomationElement context = null;
            while (true)
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == ScripterTags.If)
                    {
                        conditionActive = false;
                    }
                    reader.Read();
                    continue;
                }
                else if (conditionActive)
                {
                    // there's a condition not met that's active
                    reader.Read();
                    continue;
                }
                else if (reader.Name == ScripterTags.If)
                {
                    condition = new Condition(DefinesTable.GetAttribute(reader, ScripterTags.Condition));
                    conditionActive = !condition.Matches();
                }
                else if (reader.Name == ScripterTags.Actions)
                {
                    // log the description of the tests set
                    string description = DefinesTable.GetAttribute(reader, ActionsAttributes.Description);
                    if (description == null)
                    {
                        throw (new System.Xml.XmlException("Tests description not provided"));
                    }
                    log.StatusMessage = "Test collection description = '" + description + "'";
                }
                else if (reader.Name == ScripterTags.Action)
                {
                    ProcessAction(reader, context, ref exit);

                    // check if an action orders to exit
                    if (exit)
                    {
                        break;
                    }
                }
                else if (reader.Name == ScripterTags.Include)
                {
                    // get the file
                    string file = DefinesTable.GetAttribute(reader, ScripterTags.File);

                    // open the stream
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    XmlTextReader includedReader = new XmlTextReader(fs);
                    includedReader.WhitespaceHandling = WhitespaceHandling.None;

                    // add the current reader
                    readers.Add(includedReader);

                    // set current stream as the new stream
                    reader = readers.Current;
                }
                else if (reader.Name == ScripterTags.Context)
                {
                    log.StatusMessage = "Context => (" + reader.LineNumber + "," + reader.LinePosition + ")";
                    string windowText = DefinesTable.GetAttribute(reader, ScripterTags.WindowText);
                    string className = DefinesTable.GetAttribute(reader, ScripterTags.ClassName);
                    bool continueIfNotFound;
                    if (!Boolean.TryParse(DefinesTable.GetAttribute(reader, ScripterTags.ContinueIfNotFound), out continueIfNotFound))
                    {
                        continueIfNotFound = false;
                    }

                    // find the context window - be agressive
                    AutomationElement newContext = null;
                    do
                    {
                        newContext = UIFinder.FindElement(log, className, windowText);
                        if (continueIfNotFound)
                        {
                            break;
                        }
                    }
                    while (newContext == null);

                    // set new context
                    if (newContext != null)
                    {
                        context = newContext;
                        log.StatusMessage = "Context changed";
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        // not found
                        string notFoundMessage = "Context not found: ClassName = '" + className + "', WindowText = '" + windowText + "'";
                        if (!continueIfNotFound)
                        {
                            throw (new ArgumentException(notFoundMessage));
                        }
                        log.StatusMessage = "Warning: " + notFoundMessage;
                    }
                }
                else if (reader.Name == ScripterTags.Define)
                {
                    // get variable name and value
                    string name = DefinesTable.GetAttribute(reader, ScripterTags.Name);
                    if (name == null)
                    {
                        throw (new System.Xml.XmlException("Define tag does not contain a " + ScripterTags.Name + " attribute"));
                    }
                    string value = DefinesTable.GetAttribute(reader, ScripterTags.Value);
                    if (value == null)
                    {
                        throw (new System.Xml.XmlException("Define tag does not contain a " + ScripterTags.Value + " attribute"));
                    }

                    // delete the pair if already exists
                    if (DefinesTable.Instance.Table[name] != null)
                    {
                        DefinesTable.Instance.Table.Remove(name);
                    }

                    // add the pair to the table
                    DefinesTable.Instance.Table.Add(name, value);
                }

                // if done with reader, get the next available
                if (!reader.Read())
                {
                    while (true)
                    {
                        readers.Remove();
                        reader = readers.Current;

                        // if run out of readers, return
                        if (reader == null)
                        {
                            return;
                        }

                        // if can read from current reader, continue script execution
                        if (reader.Read())
                        {
                            break;
                        }
                    }
                }

                // When a new context is set, UIA does not return the title of the new context unless we sleep here
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// ProcessAction
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="exit"></param>
        private void ProcessAction(XmlTextReader reader, AutomationElement context, ref bool exit)
        {
            log.StatusMessage = "Action => (" + reader.LineNumber + "," + reader.LinePosition + ")";

            // get action's name
            string actionName = DefinesTable.GetAttribute(reader, ActionAttributes.Name);
            if (actionName == null)
            {
                throw (new System.Xml.XmlException(" (" + reader.LineNumber + "," + reader.LinePosition + ") " + "Action's Name attribute not specified"));
            }

            // create the specified action
            Action a = Action.CreateAction(actionName, log, reader);

            // look for any target condition
            PropertyCondition[] conditions = ReadTargetProperties(reader);

            // set initial time
            DateTime start = DateTime.Now;
            int maxSeconds = 10;
            TimeSpan maxDuration = new TimeSpan(0, 0, maxSeconds);

            // find the context element
            AutomationElement target = context;
            if (conditions != null)
            {
                do
                {
                    target = UIFinder.FindElement(log, context, conditions);

                    // break if this is taking much time...
                    TimeSpan duration = DateTime.Now - start;
                    if (duration > maxDuration)
                    {
                        log.StatusMessage = "Warning: unresolved target";
                        break;
                    }
                }
                while (target == null);
            }

            //HACK: Vista and newer file dialogs have Open/Save buttons that are sometimes ControlType.Button, and other times
            //ControlType.Pane.  We can't tell which.  If we tried to click a Button with ID=1 and it failed, try to click
            //a Pane with ID=1.
            if ((target == null) && (a is LeftClickAction) && (conditions != null) && (conditions.Length == 2) &&
                ((int)conditions[0].Value == ControlType.Button.Id) && (conditions[1].Value.ToString() == "1"))
            {
                log.StatusMessage = "Special case for Open button in Vista file dialog - retry with Pane instead of Button";
                conditions[0] = new PropertyCondition(conditions[0].Property, ControlType.Pane);
                target = UIFinder.FindElement(log, context, conditions);
            }

            // If we failed to find the target, and it's LeftClick, we're probably trying to click More Info on the IE error page 
            // In this case, we do an ugly hack, getting the HTMLElement for the More Info and click that
            if ((target == null) && (a is LeftClickAction))
            {
                log.StatusMessage = "Performing special case for LeftClickAction on More/Less Info on WinXP";
                a.Execute2(Win32Helper.GetErrorPageHwnd(), ref exit);
            }
            else
            {
                // execute the action
                a.Execute(target, ref exit);
            }
        }

        /// <summary>
        /// TargetPropertyAttributes
        /// </summary>
        private struct TargetPropertyAttributes
        {
            internal const string TargetProperty = "TargetProperty";
            internal const string Name = "Name";
            internal const string Value = "Value";
        }

        /// <summary>
        /// ReadTargetProperties
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private static PropertyCondition[] ReadTargetProperties(XmlTextReader r)
        {
            // list of property conditions
            ArrayList props = new ArrayList();

            // no conditions in this fragment
            if (r.IsEmptyElement)
            {
                return (null);
            }

            // get whether element is empty or not (if it's, end element will be read)
            bool isEmptyElement = r.IsEmptyElement;

            // read conditions
            while (r.Read())
            {
                if (r.Name != TargetPropertyAttributes.TargetProperty)
                {
                    break;
                }

                // get name
                string name = DefinesTable.GetAttribute(r, TargetPropertyAttributes.Name);
                if (name == null)
                {
                    throw (new XmlException(TargetPropertyAttributes.Name + " not specified for a TargetProperty"));
                }

                // get value
                string value = DefinesTable.GetAttribute(r, TargetPropertyAttributes.Value);
                if (value == null)
                {
                    throw (new XmlException(TargetPropertyAttributes.Value + " not specified for a TargetProperty"));
                }

                // add the property condition
                AutomationProperty p = AutomationPropertyFactory.FromString(name);
                if (p == null)
                {
                    throw new XmlException("The Property '" + name + "' used in test script is unknown to Scripter.");
                }

                // choose object to pass to construct the condition
                object conditionValue = null;
                const string controlTypeTarget = "ControlType";
                if (name == controlTypeTarget)
                {
                    conditionValue = GetControlType(value);
                }
                else
                {
                    conditionValue = value;
                }

                // add the condition
                PropertyCondition pc = new PropertyCondition(p, conditionValue);
                props.Add(pc);
            }

            // if no conditions, return null: context should be used as target
            if (props.Count == 0)
            {
                return (null);
            }

            // collect the conditions
            PropertyCondition[] conditions = new PropertyCondition[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                conditions[i] = (PropertyCondition)props[i];
            }

            // return it
            return (conditions);
        }

        /// <summary>
        /// GetControlType
        /// </summary>
        /// <param name="ctrlType"></param>
        /// <returns></returns>
        internal static ControlType GetControlType(string ctrlType)
        {
            const string controlTypePrefix = "ControlType.";
            if (ctrlType.StartsWith(controlTypePrefix))
            {
                FieldInfo fi = typeof(ControlType).GetField(ctrlType.Substring(ctrlType.IndexOf(".") + 1));
                return fi.GetValue(null) as ControlType;
            }
            else
            {
                return(null);
            }
        }
    }

    /// <summary>
    /// Interface to be implemented by actions
    /// </summary>
    internal abstract class Action
    {
        /// <summary>
        /// Action name
        /// </summary>
        protected string Name = null;

        /// <summary>
        /// Log
        /// </summary>
        protected ILog Log = null;

        /// <summary>
        /// AvailableActions
        /// </summary>
        internal struct AvailableActions
        {
            internal const string Sleep = "Sleep";
            internal const string Maximize = "Maximize";
            internal const string Minimize = "Minimize";
            internal const string Normalize = "Normalize";
            internal const string Resize = "Resize";
            internal const string RightClick = "RightClick";
            internal const string LeftClick = "LeftClick";
            internal const string Move = "Move";
            internal const string WriteText = "WriteText";
            internal const string WriteKey = "WriteKey";
            internal const string WriteRegistry = "WriteRegistry";
            internal const string DeleteRegistryKey = "DeleteRegistryKey";
            internal const string DeleteRegistryVariable = "DeleteRegistryVariable";
            internal const string Execute = "Execute";
            internal const string PassMessage = "PassMessage";
            internal const string VerifyWindowText = "VerifyWindowText";
            internal const string IsProcessRunning = "IsProcessRunning";
            internal const string KillProcess = "KillProcess";
            internal const string TestShutdown = "TestShutdown";
            internal const string SetEnvironment = "SetEnvironment";
            internal const string WriteRegistryVariable = "WriteRegistryVariable";
            internal const string CreateDirectory = "CreateDirectory";
            internal const string CopyFile = "CopyFile";
            internal const string DeleteFile = "DeleteFile";
            internal const string VerifyFileContent = "VerifyFileContent";
            internal const string SetLocale = "SetLocale";
            internal const string Invoke = "Invoke";
            internal const string CleanClickOnceCache = "CleanClickOnceCache";
        }

        /// <summary>
        /// CreateAction
        /// </summary>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static Action CreateAction(string name, ILog log, XmlTextReader reader)
        {
            // read next action
            Action a = null;

            switch (name)
            {
                case AvailableActions.Sleep:
                    a = new SleepAction(reader);
                    break;

                case AvailableActions.Maximize:
                    a = new MaximizeAction(reader);
                    break;

                case AvailableActions.Minimize:
                    a = new MinimizeAction(reader);
                    break;

                case AvailableActions.Normalize:
                    a = new NormalizeAction(reader);
                    break;

                case AvailableActions.Resize:
                    a = new ResizeAction(reader);
                    break;

                case AvailableActions.RightClick:
                    a = new RightClickAction(reader);
                    break;

                case AvailableActions.LeftClick:
                    a = new LeftClickAction(reader);
                    break;

                case AvailableActions.Move:
                    a = new MoveAction(reader);
                    break;

                case AvailableActions.WriteText:
                    a = new WriteTextAction(reader);
                    break;

                case AvailableActions.WriteKey:
                    a = new WriteKeyAction(reader);
                    break;

                case AvailableActions.WriteRegistry:
                    a = new WriteRegistryAction(reader);
                    break;

                case AvailableActions.DeleteRegistryKey:
                    a = new DeleteRegistryKeyAction(reader);
                    break;

                case AvailableActions.DeleteRegistryVariable:
                    a = new DeleteRegistryVariableAction(reader);
                    break;

                case AvailableActions.Execute:
                    a = new ExecuteAction(reader);
                    break;

                case AvailableActions.PassMessage:
                    a = new PassMessageAction(reader);
                    break;

                case AvailableActions.VerifyWindowText:
                    a = new VerifyWindowTextAction(reader);
                    break;

                case AvailableActions.IsProcessRunning:
                    a = new IsProcessRunningAction(reader);
                    break;

                case AvailableActions.KillProcess:
                    a = new KillProcessAction(reader);
                    break;

                case AvailableActions.TestShutdown:
                    a = new TestShutdownAction(reader);
                    break;

                case AvailableActions.SetEnvironment:
                    a = new SetEnvironmentAction(reader);
                    break;

                case AvailableActions.WriteRegistryVariable:
                    a = new WriteRegistryVariableAction(reader);
                    break;

                case AvailableActions.CreateDirectory:
                    a = new CreateDirectoryAction(reader);
                    break;

                case AvailableActions.CopyFile:
                    a = new CopyFileAction(reader);
                    break;

                case AvailableActions.DeleteFile:
                    a = new DeleteFileAction(reader);
                    break;

                case AvailableActions.VerifyFileContent:
                    a = new VerifyFileContentAction(reader);
                    break;

                case AvailableActions.SetLocale:
                    a = new SetLocaleAction(reader);
                    break;

                case AvailableActions.Invoke:
                    a = new InvokeAction(reader);
                    break;

                case AvailableActions.CleanClickOnceCache:
                    a = new CleanClickOnceCacheAction(reader);
                    break;

                default:
                    throw (new System.ArgumentException("Unknown action specified: " + name));
            }

            // set action fields
            a.Log = log;
            a.Name = name;

            // return
            return (a);
        }

        /// <summary>
        /// Trace
        /// </summary>
        /// <param name="p"></param>
        protected void Trace(params object[] p)
        {
            // show action name
            Log.StatusMessage = "Action: " + Name;

            // if no pairs to show, return
            if( p == null || p.Length == 0 )
            {
                return;
            }

            // show pairs
            if (p.Length % 2 != 0)
            {
                throw (new ArgumentException("Parameter/Value pairs must be logged"));
            }
            for(int i = 0; i < p.Length - 1; i+=2)
            {
                object param = p[i] != null ? p[i] : "(undefined)";
                object value = p[i + 1] != null ? p[i + 1] : "(undefined)";
                Log.StatusMessage = "\tParameter: '" + param + "', Value: '" + value + "'";
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="exit"></param>
        internal abstract void Execute(AutomationElement target, ref bool exit);

        /// <summary>
        /// Execute2, used if UIAutomation doesn't work (like clicking "More Information" in IE)
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="exit"></param>
        internal abstract void Execute2(IntPtr hwnd, ref bool exit);

    }

    /// <summary>
    /// SleepAction
    /// </summary>
    internal class SleepAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct SleepActionAttributes
        {
            internal const string Milliseconds = "Milliseconds";
        }

        /// <summary>
        /// milliseconds to sleep
        /// </summary>
        string milliseconds = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal SleepAction(XmlTextReader r)
        {
            // read the miliseconds
            milliseconds = DefinesTable.GetAttribute(r, SleepActionAttributes.Milliseconds);
            if (milliseconds == null)
            {
                const string message = "Milliseconds attribute must be specified for a Sleep action";
                throw (new ArgumentException(message));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Thread.Sleep(Int32.Parse(milliseconds));
            Trace(SleepActionAttributes.Milliseconds, milliseconds);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// MaximizeAction 
    /// </summary>
    internal class MaximizeAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal MaximizeAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.MaximizeWindow(target);
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// MinimizeAction 
    /// </summary>
    internal class MinimizeAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal MinimizeAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.MinimizeWindow(target);
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// NormalizeAction 
    /// </summary>
    internal class NormalizeAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal NormalizeAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.NormalizeWindow(target);
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// ResizeAction
    /// </summary>
    internal class ResizeAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct ResizeActionAttributes
        {
            internal const string Factor = "Factor";
        }

        /// <summary>
        /// factor for resizing
        /// </summary>
        private string factor = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal ResizeAction(XmlTextReader r)
        {
            // read the miliseconds
            factor = DefinesTable.GetAttribute(r, ResizeActionAttributes.Factor);
            if (factor == null)
            {
                const string message = "Factor attribute must be specified for a Resize action";
                throw (new ArgumentException(message));
            }
        }

        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.ResizeWindow(target, Double.Parse(factor, NumberFormatInfo.InvariantInfo));
            Trace(ResizeActionAttributes.Factor, factor);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// RightClickAction
    /// </summary>
    internal class RightClickAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal RightClickAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.MoveToAndClick(target);
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// LeftClickAction
    /// </summary>
    internal class LeftClickAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal LeftClickAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// ElementIsEnabled
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        bool ElementIsEnabled(params object[] p)
        {
            // expected params are: AutomationElement
            if (p == null || p[0] == null)
            {
                throw (new NullReferenceException("params"));
            }
            AutomationElement e = p[0] as AutomationElement;
            if (e == null)
            {
                throw(new ArgumentException("Expected AutomationElement type"));
            }

            return((bool)e.GetCurrentPropertyValue(AutomationElement.IsEnabledProperty));
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            // wait for element to get enabled
            bool enabled = Runner.InvokeUntilExpectedValue(new Runner.InvokeUntilExpectedValueDelegate(ElementIsEnabled), true, 30, target);
            if (!enabled)
            {
                throw (new Exception("Element didn't get enabled in the given timeline"));
            }

            // click
            Rect r = (Rect)target.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            double x = r.Left + ((r.Right - r.Left) / 2.0);
            double y = r.Top + ((r.Bottom - r.Top) / 2.0);
            Point center = new Point(x, y);
            MTI.Input.SendMouseInput(center.X, center.Y, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.LeftDown | MTI.SendMouseInputFlags.Move);
            MTI.Input.SendMouseInput(center.X, center.Y, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.LeftUp | MTI.SendMouseInputFlags.Move);
            Log.StatusMessage = "Left clicked";
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
            // TODO-Miguep: only on XP
            //Guid IID_IHTMLDocument = new Guid("626FC520-A41E-11CF-A731-00A0C9082637");
            //int ieVer = IEHelper.GetIEVersion();

            //HTMLDocument IEDomFromhWnd;

            //int ret1 = Win32Helper.RegisterWindowMessage("WM_HTML_GETOBJECT");
            //if (ret1 == 0)
            //{
            //    Log.FailMessage = "Failed to RegisterWindowMessage for WM_HTML_GETOBJECT";
            //    return;
            //}
            //int lRes = 0;
            //int ret2 = Win32Helper.SendMessageTimeout(hwnd, ret1, 0, 0, 2 /*abort if hung*/, 1000, out lRes);
            //if (ret2 == 0)
            //{
            //    Log.FailMessage = "Failed to SendMessage to IE";
            //    return;
            //}
     
            //int hr = Win32Helper.ObjectFromLresult(lRes, ref IID_IHTMLDocument, 0, out IEDomFromhWnd);
            //if (hr != 0)
            //{
            //    Log.FailMessage = "ObjectFromLresult Failed with hr: " + hr;
            //    return;
            //}

            //IHTMLDocument3 doc3 = (IHTMLDocument3)IEDomFromhWnd;
            //IHTMLElementCollection kids = doc3.getElementsByName((ieVer == 6)?"toggleButton" : "infoSwitch");
            //IHTMLElement infoSw = (IHTMLElement)kids.Item((ieVer == 6)?"toggleButton" : "infoSwitch", 0);

            //infoSw.Click();
            //Log.StatusMessage = "Left clicked";
            //Trace();
        }
    }

    /// <summary>
    /// MoveAction
    /// </summary>
    internal class MoveAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal MoveAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Command.MoveTo(target);
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// WriteTextAction
    /// </summary>
    internal class WriteTextAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct WriteTextActionAttributes
        {
            internal const string Text = "Text";
            internal const string SetFocus = "SetFocus";
        }

        /// <summary>
        /// text to write
        /// </summary>
        private string text = null;

        /// <summary>
        /// whether to focus a specific element before writing the text or just write wherever you are currently focused
        /// </summary>
        private bool setFocus = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal WriteTextAction(XmlTextReader r)
        {
            // read the miliseconds
            text = DefinesTable.GetAttribute(r, WriteTextActionAttributes.Text);
            if (text == null)
            {
                throw (new XmlException(WriteTextActionAttributes.Text + " attribute not specified for a WriteText action"));
            }

            // read whether set focus or not over the current target element
            string setFocusStr = DefinesTable.GetAttribute(r, WriteTextActionAttributes.SetFocus);
            if (setFocusStr != null)
            {
                setFocus = Boolean.Parse(setFocusStr);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            // set the focus in the element
            if (setFocus)
            {
                target.SetFocus();
            }

            // wait until the element is focused
            Thread.Sleep(100);

            // write text
            MTI.Input.SendUnicodeString(text, 1, 10);

            // log
            Trace(WriteTextActionAttributes.Text, text, WriteTextActionAttributes.SetFocus, setFocus);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// WriteKeyAction
    /// </summary>
    internal class WriteKeyAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct WriteKeyActionAttributes
        {
            internal const string Key = "Key";
            internal const string Times = "Times";
            internal const string SetFocus = "SetFocus";
            internal const string Release = "Release";
        }

        /// <summary>
        /// key to write
        /// </summary>
        private string keyName = null;

        /// <summary>
        /// whether to focus a specific element before pressing the key or just do it wherever you are currently focused
        /// </summary>
        private bool setFocus = false;

        /// <summary>
        /// whether to release the key or not
        /// </summary>
        private bool release = false;

        /// <summary>
        /// number of times to press the key
        /// </summary>
        uint times = 1;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal WriteKeyAction(XmlTextReader r)
        {
            // read the key to press
            keyName = DefinesTable.GetAttribute(r, WriteKeyActionAttributes.Key);
            if (keyName == null)
            {
                throw (new XmlException(WriteKeyActionAttributes.Key + " attribute not specified for a WriteKey action"));
            }

            // read whether set focus or not over the current target element
            string setFocusStr = DefinesTable.GetAttribute(r, WriteKeyActionAttributes.SetFocus);
            if (setFocusStr != null)
            {
                setFocus = Boolean.Parse(setFocusStr);
            }

            // read whether to release the key or not (needed for control keys)
            string strRelease = DefinesTable.GetAttribute(r, WriteKeyActionAttributes.Release);
            if (strRelease != null)
            {
                release = Boolean.Parse(strRelease);
            }

            // read # of repetitions - optional, defaults to 1 repetition
            string timesStr = DefinesTable.GetAttribute(r, WriteKeyActionAttributes.Times);
            if (timesStr != null)
            {
                times = uint.Parse(timesStr);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            // set the focus in the element
            if (setFocus)
            {
                target.SetFocus();
            }

            // parse key
            if ((keyName.StartsWith("$(")) && (keyName.EndsWith(")")))
            {
                // We need to look up a resource string.  We may also need to handle shift and ctrl keys.
                bool useCtrl = false;
                bool useShift = false;
                int firstPos = keyName.IndexOf("(") + 1;
                int lastPos = keyName.IndexOf(")");
                string lookupName = keyName.Substring(firstPos, lastPos - firstPos);

                // get proper resource string
                string keyString = "";
                try
                {
#if TESTBUILD_CLR40
                    Assembly presentationFramework = Assembly.Load("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
#endif
#if TESTBUILD_CLR20
                    Assembly presentationFramework = Assembly.Load("PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
#endif
                    keyString = Extract.GetExceptionString(lookupName, presentationFramework);
                }
                catch (Exception)
                {
                    throw (new ArgumentException("Resource " + keyName + " not found."));
                }
                Singleton.Instance.Log.StatusMessage = "Retrieved resource string: " + keyString;

                // string will look like "Shift+Ctrl+F" or "Ctrl+B".  Search for shift and ctrl, and use last letter as the key
                useCtrl = keyString.IndexOf("Ctrl", StringComparison.OrdinalIgnoreCase) != -1;
                useShift = keyString.IndexOf("Shift", StringComparison.OrdinalIgnoreCase) != -1;
                string keyToPress = keyString.Substring(keyString.Length - 1);

                if (useCtrl)
                {
                    Thread.Sleep(100);
                    MTI.Input.SendKeyboardInput(Key.LeftCtrl, true);
                }
                if (useShift)
                {
                    Thread.Sleep(100);
                    MTI.Input.SendKeyboardInput(Key.LeftShift, true);
                }

                Thread.Sleep(100);
                Key key = (Key)Enum.Parse(typeof(Key), keyToPress);
                MTI.Input.SendKeyboardInput(key, true);

                if (useShift)
                {
                    Thread.Sleep(100);
                    MTI.Input.SendKeyboardInput(Key.LeftShift, false);
                }
                if (useCtrl)
                {
                    Thread.Sleep(100);
                    MTI.Input.SendKeyboardInput(Key.LeftCtrl, false);
                }                               
            }
            else
            {
                Key key = (Key)Enum.Parse(typeof(Key), keyName);
                for (uint i = 0; i < times; i++)
                {
                    // wait until the element is focused
                    Thread.Sleep(100);
                    MTI.Input.SendKeyboardInput(key, !release);
                }
            }

            // log
            Trace(WriteKeyActionAttributes.Key, keyName, WriteKeyActionAttributes.SetFocus, setFocus, WriteKeyActionAttributes.Release, release, WriteKeyActionAttributes.Times, times);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// WriteRegistryAction
    /// </summary>
    internal class WriteRegistryAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct WriteRegistryActionAttributes
        {
            internal const string Key = "Key";
            internal const string Variable = "Variable";
            internal const string Value = "Value";
            internal const string Type = "Type";
            internal const string RedirectToWOW6432Node = "RedirectToWOW6432Node";
        }

        /// <summary>
        /// needed parameters to write the registry
        /// </summary>
        private string key = null;
        private string variable = null;
        private string value = null;
        private string type = null;
        private bool redirect = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal WriteRegistryAction(XmlTextReader r)
        {
            // read key
            key = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Key);
            if (key == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Key + " attribute not specified for a WriteRegistry action"));
            }

            // read variable
            variable = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Variable);
            if (variable == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Variable + " attribute not specified for a WriteRegistry action"));
            }

            // read value
            value = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Value);
            if (value == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Value + " attribute not specified for a WriteRegistry action"));
            }

            // read type
            type = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Type);
            if (type == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Type + " attribute not specified for a WriteRegistry action"));
            }

            // read redirect
            string redirectSetting = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.RedirectToWOW6432Node);
            if (redirectSetting != null)
            {
                redirect = Boolean.Parse(redirectSetting);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            RegistryHelper.Write(key, variable, value, type, redirect);
            Trace(WriteRegistryActionAttributes.Key, key, WriteRegistryActionAttributes.Variable, variable, WriteRegistryActionAttributes.Value, value, WriteRegistryActionAttributes.Type, type, WriteRegistryActionAttributes.RedirectToWOW6432Node, redirect);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// DeleteRegistryKeyAction
    /// </summary>
    internal class DeleteRegistryKeyAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct DeleteRegistryKeyActionAttributes
        {
            internal const string Key = "Key";
            internal const string RedirectToWOW6432Node = "RedirectToWOW6432Node";
        }

        /// <summary>
        /// needed parameters to write the registry
        /// </summary>
        private string key = null;
        private bool redirect = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal DeleteRegistryKeyAction(XmlTextReader r)
        {
            // read key
            key = DefinesTable.GetAttribute(r, DeleteRegistryKeyActionAttributes.Key);
            if (key == null)
            {
                throw (new XmlException(DeleteRegistryKeyActionAttributes.Key + " attribute not specified for a DeleteRegistryKey action"));
            }

            // read redirect
            string redirectSetting = DefinesTable.GetAttribute(r, DeleteRegistryKeyActionAttributes.RedirectToWOW6432Node);
            if (redirectSetting != null)
            {
                redirect = Boolean.Parse(redirectSetting);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            RegistryHelper.DeleteKeyTree(key, redirect);
            Trace(DeleteRegistryKeyActionAttributes.Key, key, DeleteRegistryKeyActionAttributes.RedirectToWOW6432Node, redirect);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// DeleteRegistryVariableAction
    /// </summary>
    internal class DeleteRegistryVariableAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct DeleteRegistryVariableActionAttributes
        {
            internal const string Key = "Key";
            internal const string Variable = "Variable";
            internal const string RedirectToWOW6432Node = "RedirectToWOW6432Node";
        }

        /// <summary>
        /// needed parameters to write the registry
        /// </summary>
        private string key = null;
        private string variable = null;
        private bool redirect = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal DeleteRegistryVariableAction(XmlTextReader r)
        {
            // read key
            key = DefinesTable.GetAttribute(r, DeleteRegistryVariableActionAttributes.Key);
            if (key == null)
            {
                throw (new XmlException(DeleteRegistryVariableActionAttributes.Key + " attribute not specified for a DeleteRegistryVariable action"));
            }

            // read variable
            variable = DefinesTable.GetAttribute(r, DeleteRegistryVariableActionAttributes.Variable);
            if (variable == null)
            {
                throw (new XmlException(DeleteRegistryVariableActionAttributes.Variable + " attribute not specified for a DeleteRegistryVariable action"));
            }

            // read redirect
            string redirectSetting = DefinesTable.GetAttribute(r, DeleteRegistryVariableActionAttributes.RedirectToWOW6432Node);
            if (redirectSetting != null)
            {
                redirect = Boolean.Parse(redirectSetting);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            RegistryHelper.DeleteVariable(key, variable, redirect);
            Trace(DeleteRegistryVariableActionAttributes.Key, key, DeleteRegistryVariableActionAttributes.Variable, variable, DeleteRegistryVariableActionAttributes.RedirectToWOW6432Node, redirect);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// WriteRegistryAction
    /// </summary>
    internal class WriteRegistryVariableAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct WriteRegistryActionAttributes
        {
            internal const string Variable = "Variable";
            internal const string Value = "Value";
        }

        /// <summary>
        /// needed parameters to write the registry
        /// </summary>
        private string variable = null;
        private string value = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal WriteRegistryVariableAction(XmlTextReader r)
        {
            // read variable
            variable = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Variable);
            if (variable == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Variable + " attribute not specified for a WriteRegistry action"));
            }

            // read value
            value = DefinesTable.GetAttribute(r, WriteRegistryActionAttributes.Value);
            if (value == null)
            {
                throw (new XmlException(WriteRegistryActionAttributes.Value + " attribute not specified for a WriteRegistry action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            RegistryVar.Set(variable, value);
            Trace(WriteRegistryActionAttributes.Variable, variable, WriteRegistryActionAttributes.Value, value);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// DeleteRegistryKeyAction
    /// </summary>
    internal class ExecuteAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        internal struct ExecuteActionAttributes
        {
            internal const string Command = "Command";
            internal const string Arguments = "Arguments";
            internal const string WaitForExit = "WaitForExit";
            internal const string UseShellExecute = "UseShellExecute";
        }

        /// <summary>
        /// needed parameters to execute the process
        /// </summary>
        private string command = null;
        private string arguments = null;
        private bool waitForExit = false;
        private bool useShellExecute = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal ExecuteAction(XmlTextReader r)
        {
            // read command
            command = DefinesTable.GetAttribute(r, ExecuteActionAttributes.Command);
            if (command == null)
            {
                throw (new XmlException(ExecuteActionAttributes.Command + " attribute not specified for a " + Name + " action"));
            }

            // read arguments
            arguments = DefinesTable.GetAttribute(r, ExecuteActionAttributes.Arguments);
            if (command == null)
            {
                throw (new XmlException(ExecuteActionAttributes.Arguments + " attribute not specified for a " + Name + " action"));
            }

            // read wait for exit
            string waitForExitStr = DefinesTable.GetAttribute(r, ExecuteActionAttributes.WaitForExit);
            if (waitForExitStr == null)
            {
                throw (new XmlException(ExecuteActionAttributes.WaitForExit + " attribute not specified for a " + Name + " action"));
            }
            waitForExit = Boolean.Parse(waitForExitStr);

            // read use ShellExecute
            string useShellExecuteStr = DefinesTable.GetAttribute(r, ExecuteActionAttributes.UseShellExecute);
            if (useShellExecuteStr == null)
            {
                throw (new XmlException(ExecuteActionAttributes.WaitForExit + " attribute not specified for a " + Name + " action"));
            }
            useShellExecute = Boolean.Parse(useShellExecuteStr);
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            int exit, procid;
            string stdout;
            ProcessUtils.ExecuteProcess(command, arguments, useShellExecute, waitForExit, out exit, out stdout, out procid);
            Trace(ExecuteActionAttributes.Command, command, ExecuteActionAttributes.Arguments, arguments, ExecuteActionAttributes.WaitForExit, waitForExit, ExecuteActionAttributes.UseShellExecute, useShellExecute);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// PassMessageAction
    /// </summary>
    internal class PassMessageAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct PassMessageActionAttributes
        {
            internal const string Message = "Message";
            internal const string IsNotIEVersion = "IsNotIEVersion";
            internal const string Exit = "Exit";
        }

        /// <summary>
        /// milliseconds to sleep
        /// </summary>
        string message = null;

        /// <summary>
        /// isNotIEVersion
        /// </summary>
        uint isNotIEVersion = 0;

        /// <summary>
        /// Exit
        /// </summary>
        bool exit = false;

        /// <summary>
        /// true if there's a condition to pass
        /// </summary>
        bool conditional = false;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal PassMessageAction(XmlTextReader r)
        {
            // read message
            message = DefinesTable.GetAttribute(r, PassMessageActionAttributes.Message);
            if (message == null)
            {
                throw (new XmlException(PassMessageActionAttributes.Message + " attribute not specified for a " + Name + " action"));
            }

            // read exit
            string exitStr = DefinesTable.GetAttribute(r, PassMessageActionAttributes.Exit);
            if (exitStr != null)
            {
                exit = Boolean.Parse(exitStr);
            }

            // read IEVersion
            string ieCondition = DefinesTable.GetAttribute(r, PassMessageActionAttributes.IsNotIEVersion);
            if (ieCondition != null)
            {
                conditional = true;
                isNotIEVersion = uint.Parse(ieCondition);
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            if (conditional)
            {
                if (isNotIEVersion != IEHelper.GetIEVersion())
                {
                    Log.PassMessage = message;
                    if(exit)
                    {
                        doExit = true;
                    }
                }
            }
            else
            {
                Log.PassMessage = message;
                if(exit)
                {
                    doExit = true;
                }
            }
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// VerifyWindowTextAction
    /// </summary>
    internal class VerifyWindowTextAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct VerifyWindowTextActionAttributes
        {
            internal const string ExpectedText = "ExpectedText";
        }

        /// <summary>
        /// milliseconds to sleep
        /// </summary>
        string expectedText = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal VerifyWindowTextAction(XmlTextReader r)
        {
            // read the miliseconds
            expectedText = DefinesTable.GetAttribute(r, VerifyWindowTextActionAttributes.ExpectedText);
            if (expectedText == null)
            {
                throw (new XmlException(VerifyWindowTextActionAttributes.ExpectedText + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            Log.StatusMessage = "Expected = '" + expectedText + "'";
            TimeSpan timeout = new TimeSpan(0, 0, 0, 20);
            DateTime start = DateTime.Now;
            while (true)
            {
                // check match
                string currentText = (string)target.GetCurrentPropertyValue(AutomationElement.NameProperty);
                Log.StatusMessage = "Current = '" + currentText + "'";
                if (StringUtils.Matches(expectedText, currentText))
                {
                    Log.PassMessage = "current matches expected";
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                    DateTime now = DateTime.Now;
                    Log.StatusMessage = "Verification attempt failed at " + now.ToLongTimeString();
                    if ( now > start + timeout )
                    {
                        Log.FailMessage = "current doesn't match expected";
                        break;
                    }
                }
            }

            Trace(VerifyWindowTextActionAttributes.ExpectedText, expectedText);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// KillProcessAction
    /// </summary>
    internal class KillProcessAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct KillProcessActionAttributes
        {
            internal const string MainModuleName = "MainModuleName";
        }

        /// <summary>
        /// mainModuleName
        /// </summary>
        string mainModuleName = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal KillProcessAction(XmlTextReader r)
        {
            // read the main module
            mainModuleName = DefinesTable.GetAttribute(r, KillProcessActionAttributes.MainModuleName);
            if (mainModuleName == null)
            {
                throw (new XmlException(KillProcessActionAttributes.MainModuleName + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            ProcessUtils.KillProcess(mainModuleName);
            Trace(KillProcessActionAttributes.MainModuleName, mainModuleName);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// IsProcessRunningAction
    /// </summary>
    internal class IsProcessRunningAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct IsProcessRunningActionAttributes
        {
            internal const string MainModuleName = "MainModuleName";
            internal const string ExpectedValue = "ExpectedValue";
        }

        /// <summary>
        /// mainModuleName
        /// </summary>
        string mainModuleName = null;

        /// <summary>
        /// expectedValue is 'true' or 'false'
        /// </summary>
        bool expectedValue = true;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal IsProcessRunningAction(XmlTextReader r)
        {
            // read the main module
            mainModuleName = DefinesTable.GetAttribute(r, IsProcessRunningActionAttributes.MainModuleName);
            if (mainModuleName == null)
            {
                throw (new XmlException(IsProcessRunningActionAttributes.MainModuleName + " attribute not specified for a " + Name + " action"));
            }

            // read the expected value
            string expectedValueStr = DefinesTable.GetAttribute(r, IsProcessRunningActionAttributes.ExpectedValue);
            if (expectedValueStr == null)
            {
                throw (new XmlException(IsProcessRunningActionAttributes.ExpectedValue + " attribute not specified for a " + Name + " action"));
            }
            expectedValue = Boolean.Parse(expectedValueStr);
        }

        /// <summary>
        /// ProcessIsRunning
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        bool IsProcessRunning(params object[] p)
        {
            // expected params are: AutomationElement
            if (p == null || p[0] == null)
            {
                throw (new NullReferenceException("params"));
            }
            string mainModuleName = p[0] as string;
            if (mainModuleName == null)
            {
                throw (new ArgumentException("Expected string type"));
            }

            return (ProcessUtils.IsProcessRunning(mainModuleName));
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            bool isRunning = Runner.InvokeUntilExpectedValue(new Runner.InvokeUntilExpectedValueDelegate(IsProcessRunning), expectedValue, 10, mainModuleName);
            Log.StatusMessage = "Process is running? " + isRunning.ToString();
            if (isRunning == expectedValue)
            {
                Log.PassMessage = String.Empty;
            }
            else
            {
                Log.FailMessage = String.Empty;
            }
            Trace(IsProcessRunningActionAttributes.MainModuleName, mainModuleName, IsProcessRunningActionAttributes.ExpectedValue, expectedValue);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// TestShutdownAction
    /// </summary>
    internal class TestShutdownAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal TestShutdownAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            TestEnvironmentProxy.Shutdown();
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// SetEnvironmentAction
    /// </summary>
    internal class SetEnvironmentAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct SetEnvironmentActionAttributes
        {
            internal const string Name = "Name";
            internal const string Value = "Value";
        }

        /// <summary>
        /// mainModuleName
        /// </summary>
        string name = null;

        /// <summary>
        /// expectedValue is 'true' or 'false'
        /// </summary>
        string value = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal SetEnvironmentAction(XmlTextReader r)
        {
            // read the variable name
            name = DefinesTable.GetAttribute(r, SetEnvironmentActionAttributes.Name);
            if (name == null)
            {
                throw (new XmlException(SetEnvironmentActionAttributes.Name + " attribute not specified for a " + Name + " action"));
            }

            // read the variable value
            value = DefinesTable.GetAttribute(r, SetEnvironmentActionAttributes.Value);
            if (value == null)
            {
                throw (new XmlException(SetEnvironmentActionAttributes.Value + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            EnvironmentVariable.Set(name, value);
            Trace(SetEnvironmentActionAttributes.Name, name, SetEnvironmentActionAttributes.Value, value);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// CreateDirectoryAction
    /// </summary>
    internal class CreateDirectoryAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct CreateDirectoryActionAttributes
        {
            internal const string Path = "Path";
        }

        /// <summary>
        /// directory path
        /// </summary>
        string path = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal CreateDirectoryAction(XmlTextReader r)
        {
            // read the variable name
            path = DefinesTable.GetAttribute(r, CreateDirectoryActionAttributes.Path);
            if (path == null)
            {
                throw (new XmlException(CreateDirectoryActionAttributes.Path + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            DirectoryHelper.Create(path);
            Trace(CreateDirectoryActionAttributes.Path, path);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// CopyFileAction
    /// </summary>
    internal class CopyFileAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct CopyFileActionAttributes
        {
            internal const string Source = "Source";
            internal const string Destination = "Destination";
            internal const string Overwrite = "Overwrite";
        }

        /// <summary>
        /// source path
        /// </summary>
        string source = null;

        /// <summary>
        /// destination path
        /// </summary>
        string destination = null;

        /// <summary>
        /// overwrite
        /// </summary>
        bool overwrite = true;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal CopyFileAction(XmlTextReader r)
        {
            // read source
            source = DefinesTable.GetAttribute(r, CopyFileActionAttributes.Source);
            if (source == null)
            {
                throw (new XmlException(CopyFileActionAttributes.Source + " attribute not specified for a " + Name + " action"));
            }

            // read destination
            destination = DefinesTable.GetAttribute(r, CopyFileActionAttributes.Destination);
            if (destination == null)
            {
                throw (new XmlException(CopyFileActionAttributes.Destination + " attribute not specified for a " + Name + " action"));
            }

            // read overwrite
            overwrite = Boolean.Parse(DefinesTable.GetAttribute(r, CopyFileActionAttributes.Overwrite));
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            FileHelper.Copy(source, destination, overwrite);
            Trace(CopyFileActionAttributes.Source, source, CopyFileActionAttributes.Destination, destination, CopyFileActionAttributes.Overwrite, overwrite);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// DeleteFileAction
    /// </summary>
    internal class DeleteFileAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct DeleteFileActionAttributes
        {
            internal const string Path = "Path";
        }

        /// <summary>
        /// path
        /// </summary>
        string path = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal DeleteFileAction(XmlTextReader r)
        {
            // read path
            path = DefinesTable.GetAttribute(r, DeleteFileActionAttributes.Path);
            if (path == null)
            {
                throw (new XmlException(DeleteFileActionAttributes.Path + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            FileHelper.Delete(path);
            Trace(DeleteFileActionAttributes.Path, path);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// VerifyFileContentAction
    /// </summary>
    internal class VerifyFileContentAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct VerifyFileContentActionAttributes
        {
            internal const string Path = "Path";
            internal const string Content = "Content";
        }

        /// <summary>
        /// path
        /// </summary>
        string path = null;

        /// <summary>
        /// content
        /// </summary>
        string expectedContent = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal VerifyFileContentAction(XmlTextReader r)
        {
            // read path
            path = DefinesTable.GetAttribute(r, VerifyFileContentActionAttributes.Path);
            if (path == null)
            {
                throw (new XmlException(VerifyFileContentActionAttributes.Path + " attribute not specified for a " + Name + " action"));
            }

            // read content
            expectedContent = DefinesTable.GetAttribute(r, VerifyFileContentActionAttributes.Content);
            if (expectedContent == null)
            {
                throw (new XmlException(VerifyFileContentActionAttributes.Content + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            // get content of the file
            FileHandler f = new FileHandler(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string actualContent = f.StreamReader.ReadToEnd();
            
            // pass if matches, fail otherwise
            if (expectedContent == actualContent)
            {
                Log.PassMessage = "File content matches expected: " + expectedContent;
            }
            else
            {
                Log.FailMessage = "File content does not match expected.";
                Log.StatusMessage = "Expected: " + expectedContent;
                Log.StatusMessage = "Current: " + actualContent;
            }

            Trace(VerifyFileContentActionAttributes.Path, path, VerifyFileContentActionAttributes.Content, expectedContent);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// SetLocaleAction
    /// </summary>
    internal class SetLocaleAction : Action
    {
        /// <summary>
        /// action's attributes
        /// </summary>
        private struct SetLocaleActionAttributes
        {
            internal const string Locale = "Locale";
        }

        /// <summary>
        /// path
        /// </summary>
        string locale = null;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal SetLocaleAction(XmlTextReader r)
        {
            // read locale
            locale = DefinesTable.GetAttribute(r, SetLocaleActionAttributes.Locale);
            if (locale == null)
            {
                throw (new XmlException(SetLocaleActionAttributes.Locale + " attribute not specified for a " + Name + " action"));
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            // set locale
            LocalizationHelper.SetActiveInputLocale(locale);
            Trace(SetLocaleActionAttributes.Locale, locale);
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// InvokeAction
    /// </summary>
    internal class InvokeAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal InvokeAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            IInvokeProvider invokeProvider = target.GetCurrentPattern(InvokePattern.Pattern) as IInvokeProvider;
            if (invokeProvider == null)
            {
                throw (new ArgumentNullException("Target does not implement IInvokeProvider"));
            }
            invokeProvider.Invoke();
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }

    /// <summary>
    /// CleanClickOnceCacheAction
    /// </summary>
    internal class CleanClickOnceCacheAction : Action
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="r"></param>
        internal CleanClickOnceCacheAction(XmlTextReader r)
        {
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="doExit"></param>
        internal override void Execute(AutomationElement target, ref bool doExit)
        {
            //TODO-Miguep:uncomment
            //ClickOnceHelper.CleanClickOnceCache();
            Trace();
        }

        /// <summary>
        /// Execute2, only used to left click "More Information" or "Less Information" on XP
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="doExit"></param>
        internal override void Execute2(IntPtr hwnd, ref bool doExit)
        {
        }
    }
}