// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using MTI = Microsoft.Test.Input;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// UI Automation helper methods
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class Utils
    {
        /// <summary>
        /// Returns full IE title bar text, including the " - Microsoft Internet Explorer" part.
        /// returns blank string if something fails along the way.
        /// </summary>
        /// <returns></returns>
        internal static string GetIETitleBarText()
        {
            try
            {
                AutomationElement ae = FindElementByTitleBarText(AutomationElement.RootElement, new Regex(".* - .* Internet Explorer"));
                if (null == ae)
                {
                    return String.Empty;
                }
                // get its NameProperty value
                string name = (string)ae.GetCurrentPropertyValue(AutomationElement.NameProperty);
                return (name);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Returns the first element whose title bar has the given text
        /// </summary>
        /// <param name="context">the element to use</param>
        /// <param name="text">the text to match</param>
        /// <returns>the found automation element</returns>
        internal static AutomationElement FindElementByTitleBarText(AutomationElement context, string text)
        {
            const string titleBar = "TitleBar";
            return (FindElementByAutomationIDAndText(context, titleBar, text));
        }

        /// <summary>
        /// Returns the first element whose title bar text matches the given reg exp
        /// </summary>
        /// <param name="context">the element to use</param>
        /// <param name="r">the reg exp to match</param>
        /// <returns>the found automation element</returns>
        internal static AutomationElement FindElementByTitleBarText(AutomationElement context, Regex r)
        {
            const string titleBar = "TitleBar";
            return (FindElementByAutomationIDAndText(context, titleBar, r));
        }

        /// <summary>
        /// Finds an element given its AutID (i.e. 'TitleBar') and Text (i.e. 'Dummy - Microsoft Development Environment')
        /// </summary>
        /// <param name="context">the element to use</param>
        /// <param name="autId">aut ID description</param>
        /// <param name="text">element text</param>
        /// <returns>the found automation element</returns>
        internal static AutomationElement FindElementByAutomationIDAndText(AutomationElement context, string autId, string text)
        {
            // check params
            if (autId == null)
            {
                throw (new ArgumentNullException("autId"));
            }
            if (text == null)
            {
                throw (new ArgumentNullException("text"));
            }

            // find the element
            CacheRequest creq = new CacheRequest();
            creq.TreeFilter = Automation.RawViewCondition;
            using (creq.Activate())
            {
                AndCondition c = new AndCondition(
                    new PropertyCondition(AutomationElement.AutomationIdProperty, autId),
                    new PropertyCondition(AutomationElement.NameProperty, text)
                    );

                AutomationElement e = AutomationContext(context);
                AutomationElement target = e.FindFirst(TreeScope.Element | TreeScope.Descendants, c);
                return (target);
            }
        }

        /// <summary>
        /// Finds an element given its AutID (i.e. 'TitleBar') and reg exp (i.e. '.* - Microsoft Development Environment')
        /// </summary>
        /// <param name="context">the element to use</param>
        /// <param name="autId">aut ID description</param>
        /// <param name="r">reg exp</param>
        /// <returns>the found target</returns>
        internal static AutomationElement FindElementByAutomationIDAndText(AutomationElement context, string autId, Regex r)
        {
            // check params
            if (autId == null)
            {
                throw (new ArgumentNullException("autId"));
            }
            if (r == null)
            {
                throw (new ArgumentNullException("r"));
            }

            // find the element
            CacheRequest creq = new CacheRequest();
            creq.TreeFilter = Automation.RawViewCondition;
            using (creq.Activate())
            {
                // get all the elements
                PropertyCondition c = new PropertyCondition(AutomationElement.AutomationIdProperty, autId);
                AutomationElement e = AutomationContext(context);
                AutomationElementCollection candidates = e.FindAll(TreeScope.Element | TreeScope.Descendants, c);

                // find the first one that matches the pattern
                foreach (AutomationElement target in candidates)
                {
                    // get its NameProperty value
                    string name = (string)target.GetCurrentPropertyValue(AutomationElement.NameProperty);

                    // verify if it maches with the provided string
                    if (r.Match(name).Success)
                    {
                        return (target);
                    }
                }

                // target not found
                return (null);
            }
        }

        /// <summary>
        /// Finds (control and non-control) elements with timeout
        /// </summary>
        /// <param name="elementID"></param>
        /// <param name="timeoutMS"></param>
        /// <returns></returns>
        internal static AutomationElement FindElementFromIDWithTimeout(string elementID, int timeoutMS)
        {
            DateTime methodCalledTime = DateTime.Now;
            while (true)
            {
                // try to get the element
                AutomationElement target = FindElementFromID(AutomationElement.RootElement, elementID);
                if (target != null)
                {
                    return (target);
                }

                // timeout?
                TimeSpan ellapsed = DateTime.Now - methodCalledTime;
                if (ellapsed.TotalMilliseconds > timeoutMS)
                {
                    // timeout
                    return (null);
                }
            }
        }

        /// <summary>
        /// Finds (control and non-control) elements
        /// </summary>
        /// <param name="elementID"></param>
        /// <returns></returns>
        internal static AutomationElement FindElementFromID(string elementID)
        {
            return (FindElementFromID(AutomationElement.RootElement, elementID));
        }

        /// <summary>
        /// Finds (control and non-control) elements in a given context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="elementID"></param>
        /// <returns>the found target</returns>
        internal static AutomationElement FindElementFromID(AutomationElement context, string elementID)
        {
            CacheRequest creq = new CacheRequest();
            creq.TreeFilter = Automation.RawViewCondition;
            using (creq.Activate())
            {
                System.Windows.Automation.PropertyCondition cond = new PropertyCondition(AutomationElement.AutomationIdProperty, elementID);
                AutomationElement e = AutomationContext(context);
                AutomationElement target = e.FindFirst(TreeScope.Element | TreeScope.Descendants, cond);
                return (target);
            }
        }

        /// <summary>
        /// Returns the context in which to find an element
        /// </summary>
        /// <param name="e">the given automation element</param>
        /// <returns>the context</returns>
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

        internal static void MaximizeWindow(AutomationElement target)
        {
            object patternObject;
            target.TryGetCurrentPattern(WindowPattern.Pattern, out patternObject);
            WindowPattern wp = patternObject as WindowPattern;
            wp.SetWindowVisualState(WindowVisualState.Maximized);
            Thread.Sleep(100);
        }

        internal static void MinimizeWindow(AutomationElement target)
        {
            object patternObject;
            target.TryGetCurrentPattern(WindowPattern.Pattern, out patternObject);
            WindowPattern wp = patternObject as WindowPattern;
            wp.SetWindowVisualState(WindowVisualState.Minimized);
            Thread.Sleep(100);
        }

        internal static void NormalizeWindow(AutomationElement target)
        {
            object patternObject;
            target.TryGetCurrentPattern(WindowPattern.Pattern, out patternObject);
            WindowPattern wp = patternObject as WindowPattern;
            wp.SetWindowVisualState(WindowVisualState.Normal);
            Thread.Sleep(100);
        }

        internal static void ResizeWindow(AutomationElement target, double factor)
        {
            // get the bottom-right corner
            Rect r = target.Current.BoundingRectangle;

            MTI.Input.SendMouseInput(r.Right - 1, r.Bottom - 1, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move | MTI.SendMouseInputFlags.LeftDown);
            Thread.Sleep(100);

            MTI.Input.SendMouseInput(r.Right * factor, r.Bottom * factor, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move | MTI.SendMouseInputFlags.LeftUp);
            Thread.Sleep(100);
        }

        internal static void MoveToAndClick(AutomationElement target)
        {
            MTI.Input.MoveToAndClick(target);
        }

        internal static void MoveTo(AutomationElement target)
        {
            // get target bounding rectangle
            Rect r = target.Current.BoundingRectangle;

            // move the mouse there
            MTI.Input.SendMouseInput((r.Left + r.Right) / 2, (r.Bottom + r.Top) / 2, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.Move);
        }
    }

    /// <summary>
    /// Rect utils
    /// </summary>
    internal class RectUtils
    {
        internal static Point Center(Rect r)
        {
            return (new Point(r.X + r.Width / 2, r.Y + r.Height / 2));
        }
    }

    /// <summary>
    /// EventQueueItem
    /// </summary>
    internal class EventQueueItem
    {
        internal EventQueue.Command Command;
        internal object Parameter = null;

        /// <summary>
        /// .ctor
        /// </summary>
        internal EventQueueItem(EventQueue.Command command, object parameter)
        {
            Command = command;
            Parameter = parameter;
        }
    }

    /// <summary>
    /// EventQueue
    /// </summary>
    internal class EventQueue : Queue
    {
        /// <summary>
        /// Available actions to perform
        /// </summary>
        internal enum Command
        {
            MouseRightDown,
            MouseRightUp,
            MouseLeftDown,
            MouseLeftUp,
            MouseMove,
        }

        /// <summary>
        /// Execute posted items
        /// </summary>
        internal void Flush()
        {
            Post(null);
        }

        /// <summary>
        /// Methods that gets called by the dispatcher to 1) execute an item and then 2) post next item in the dispatcher's queue
        /// </summary>
        private object Post(object o)
        {
            // if called with a non-null object, it means that an item must be execute
            if (o != null)
            {
                DoRequestedAction(o);
            }

            // take next item
            if (this.Count > 0)
            {
                // get next item
                EventQueueItem item = (EventQueueItem)Dequeue();

                // post it
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(Post), item);
            }

            return (null);
        }

        /// <summary>
        /// Executes requested action
        /// </summary>
        private void DoRequestedAction(object o)
        {
            EventQueueItem item = (EventQueueItem)o;
            switch (item.Command)
            {
                case Command.MouseRightDown:
                    MTI.Input.SendMouseInput(0, 0, 0, MTI.SendMouseInputFlags.RightDown);
                    break;
                case Command.MouseRightUp:
                    MTI.Input.SendMouseInput(0, 0, 0, MTI.SendMouseInputFlags.RightUp);
                    break;
                case Command.MouseLeftDown:
                    MTI.Input.SendMouseInput(0, 0, 0, MTI.SendMouseInputFlags.LeftDown);
                    break;
                case Command.MouseLeftUp:
                    MTI.Input.SendMouseInput(0, 0, 0, MTI.SendMouseInputFlags.LeftUp);
                    break;
                case Command.MouseMove:
                    Rect r = AvalonHelper.GetBoundingRectangle(item.Parameter as UIElement);
                    Point center = RectUtils.Center(r);
                    MTI.Input.MoveTo(center);
                    break;
                default:
                    break;
            }
        }
    }
}

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// script tags
    /// </summary>
    enum Tags
    {
        Actions,
        Action,
        Description,
        ID,
        Target
    }

    /// <summary>
    /// abstract class for UI actions
    /// </summary>
    abstract class Action
    {
        /// <summary>
        /// ID of the target to direct the action. we link the target ID with the actual Target when executing the script, not when reading it
        /// </summary>
        internal string TargetID;

        /// <summary>
        /// Execute the action
        /// </summary>
        internal abstract void Execute(ILog log, IntPtr top);

        /// <summary>
        /// log action execution
        /// </summary>
        /// <param name="log">log</param>
        /// <param name="ID">action ID</param>
        /// <param name="target">target ID</param>
        protected void LogExecution(ILog log, string ID, string target)
        {
            string message = null;
            if (target == null)
            {
                message = "Executed action '" + ID + "'";
            }
            else
            {
                message = "Executed action '" + ID + "' on target '" + target + "'";
            }
            log.StatusMessage = message;
        }
    }

    /// <summary>
    /// UIScript represents a list of actions and useful methods to manipulate it
    /// </summary>
    internal class UIScript
    {
        private ArrayList actions = null;
        private ILog log = null;
        private IntPtr topLevelHwnd = new IntPtr(0);

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="topLevelHwnd"></param>
        /// <param name="log"></param>
        internal UIScript(Stream stream, IntPtr topLevelHwnd, ILog log)
        {
            // store the log object
            this.log = log;
            this.topLevelHwnd = topLevelHwnd;

            // read the actions from the config file
            Read(stream);
        }

        /// <summary>
        /// read actions from a stream
        /// </summary>
        /// <param name="stream">stream containing the actions</param>
        internal void Read(Stream stream)
        {
            // create objects needed to read the file
            XmlTextReader reader = new XmlTextReader(stream);
            reader.WhitespaceHandling = WhitespaceHandling.None;

            // get the lists ready
            if (actions != null)
            {
                // clear the list
                actions.Clear();
            }
            else
            {
                // create the list
                actions = new ArrayList();
            }

            // read the header of the xml stream
            reader.Read();
            if (reader.Name == Tags.Actions.ToString())
            {
                // log the description of the tests set
                string description = reader.GetAttribute(Tags.Description.ToString());
                if (description == null)
                {
                    throw (new System.Xml.XmlException("Tests description not provided"));
                }
                log.StatusMessage = "Test collection description = '" + description + "'";
            }

            // read actions
            while (reader.Read())
            {
                if (reader.Name == Tags.Action.ToString())
                {
                    Action a = ActionBuilder.Build(reader);
                    actions.Add(a);
                }
            }
        }

        /// <summary>
        /// Execute the actions
        /// </summary>
        internal void Execute()
        {
            // execute the stored actions
            foreach (Action a in actions)
            {
                // execute the action
                a.Execute(log, topLevelHwnd);
            }
        }
    }

    /// <summary>
    /// ActionBuilder takes in a stream in the position where an Action tag begins and creates the appropiate action object
    /// </summary>
    class ActionBuilder
    {
        internal static Action Build(XmlTextReader r)
        {
            // get the ID
            string ID = r.GetAttribute(Tags.ID.ToString());
            if (ID == null)
            {
                // action with no ID
                const string message = "ID not specified";
                throw (new ArgumentNullException(message));
            }

            // get the Target's ID
            string targetID = r.GetAttribute(Tags.Target.ToString());

            // depending the ID, create the appropriate target
            switch (ID)
            {
                case WindowMaximizeAction.ID:
                    return (new WindowMaximizeAction(r, targetID));

                case WindowMinimizeAction.ID:
                    return (new WindowMinimizeAction(r, targetID));

                case ResizeAction.ID:
                    return (new ResizeAction(r, targetID));

                case WindowNormalAction.ID:
                    return (new WindowNormalAction(r, targetID));

                case MouseRightClickAction.ID:
                    return (new MouseRightClickAction(r, targetID));

                case MouseLeftClickAction.ID:
                    return (new MouseLeftClickAction(r, targetID));

                case MouseMoveAction.ID:
                    return (new MouseMoveAction(r, targetID));

                case KeyboardWriteTextAction.ID:
                    return (new KeyboardWriteTextAction(r, targetID));

                case KeyboardWriteKeyAction.ID:
                    return (new KeyboardWriteKeyAction(r, targetID));

                case SleepAction.ID:
                    return (new SleepAction(r, targetID));

                case VerifyIETitleAction.ID:
                    return (new VerifyIETitleAction(r, targetID));

                default:
                    string message = "Action ID ='" + ID + "' unrecognized";
                    throw (new ArgumentException(message));
            }
        }
    }

    /// <summary>
    /// TargetBuilder takes in a stream in the position where a Target tag begins and creates the appropiate target object
    /// </summary>
    class TargetResolver
    {
        internal static AutomationElement Resolve(ILog log, IntPtr topLevelHwnd, string targetID)
        {
            int timeoutMS = 60000;//60 seconds timeout
            if (targetID == null)
            {
                // target with no ID
                const string message = "Target ID not specified";
                throw (new ArgumentNullException(message));
            }
            log.StatusMessage = "Resolving target '" + targetID + "'";

            AutomationElement target = null;

            // depending the ID, create the appropriate target
            switch (targetID)
            {
                case "%ROOT%":
                    target = ResolveWithTimeout(ResolveRootTarget, topLevelHwnd, timeoutMS);
                    break;

                case "%IEADDRESSBAR%":
                    target = ResolveWithTimeout(ResolveIEAddressBarTarget, topLevelHwnd, timeoutMS);
                    break;

                case "%IEBACKBUTTON%":
                    target = ResolveWithTimeout(ResolveIEBackButtonTarget, topLevelHwnd, timeoutMS);
                    break;

                default:
                    // we have the ID of a target
                    target = ResolveIDTarget(topLevelHwnd, targetID);
                    break;
            }

            // return the target
            return (target);
        }

        static AutomationElement ResolveRootTarget(IntPtr top)
        {
            // create the root target
            return (AutomationElement.FromHandle(top));
        }

        static AutomationElement ResolveIEAddressBarTarget(IntPtr top)
        {
            // create the condition to find the address bar
            const string IEAddressBarClassNameProperty = "Edit";
            AndCondition isAddrBar = new AndCondition(
                new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true),
                new PropertyCondition(AutomationElement.ClassNameProperty, IEAddressBarClassNameProperty)
                );

            // get the address bar
            AutomationElement topWnd = AutomationElement.FromHandle(top);
            return (topWnd.FindFirst(TreeScope.Descendants, isAddrBar));
        }

        static AutomationElement ResolveIEBackButtonTarget(IntPtr top)
        {
            // create the condition to find the back button
            const string IEBackButtonNameProperty = "Back";
            PropertyCondition isBackButton = new PropertyCondition(AutomationElement.NameProperty, IEBackButtonNameProperty);

            // get the address bar
            AutomationElement topWnd = AutomationElement.FromHandle(top);
            return (topWnd.FindFirst(TreeScope.Descendants, isBackButton));
        }

        delegate AutomationElement ResolverDelegate(IntPtr ptr);

        static AutomationElement ResolveWithTimeout(ResolverDelegate resolver, IntPtr context, int timeoutMS)
        {
            // resolve with timeout
            DateTime methodCalledTime = DateTime.Now;
            while (true)
            {
                // try to get the element
                AutomationElement target = resolver(context);
                if (target != null)
                {
                    return (target);
                }

                // timeout?
                TimeSpan ellapsed = DateTime.Now - methodCalledTime;
                if (ellapsed.TotalMilliseconds > timeoutMS)
                {
                    // timeout
                    return (null);
                }
            }
        }

        static AutomationElement ResolveIDTarget(IntPtr top, string ID)
        {
            const int timeoutMS = 60000;//60 seconds timeout to get an element
            return (Utils.FindElementFromIDWithTimeout(ID, timeoutMS));
        }
    }

    /// <summary>
    /// SleepAction 
    /// </summary>
    class SleepAction : Action
    {
        internal const string ID = "Sleep";
        private string milliseconds = null;

        internal enum Attributes
        {
            Milliseconds
        }

        internal SleepAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;

            // read the miliseconds
            milliseconds = r.GetAttribute(Attributes.Milliseconds.ToString());
            if (milliseconds == null)
            {
                const string message = "Milliseconds attribute must be specified for a Sleep action";
                throw (new ArgumentException(message));
            }
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            Thread.Sleep(Int32.Parse(milliseconds));
            LogExecution(log, ID, null);// no target for this action
        }
    }

    /// <summary>
    /// MaximizeAction 
    /// </summary>
    class WindowMaximizeAction : Action
    {
        internal const string ID = "WindowMaximize";

        internal WindowMaximizeAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.MaximizeWindow(target);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// MinimizeAction 
    /// </summary>
    class WindowMinimizeAction : Action
    {
        internal const string ID = "WindowMinimize";

        internal WindowMinimizeAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.MinimizeWindow(target);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// NormalAction 
    /// </summary>
    class WindowNormalAction : Action
    {
        internal const string ID = "WindowNormal";

        internal WindowNormalAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.NormalizeWindow(target);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// VerifyIETitleAction 
    /// </summary>
    class VerifyIETitleAction : Action
    {
        internal const string ID = "VerifyIETitle";
        internal string expected = null;

        internal enum Attributes
        {
            Expected
        }

        internal VerifyIETitleAction(XmlTextReader r, string targetID)
        {
            // read action's attributes
            expected = r.GetAttribute(Attributes.Expected.ToString());
            if (expected == null)
            {
                const string message = "'Expected' must be specified for a VerifyIETitleAction action";
                throw (new ArgumentException(message));
            }
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            // get IE title
            string currentTitle = Utils.GetIETitleBarText();

            // check that the expected title is contained in the current one
            log.StatusMessage = "Phrase expected to be in the title: '" + expected + "'";
            log.StatusMessage = "Current: '" + currentTitle + "'";
            if (!StringUtils.Matches(expected, currentTitle))
            {
                log.FailMessage = "Title is not the expected one";
            }

            // log action
            LogExecution(log, ID, null);
        }
    }

    /// <summary>
    /// Right click action
    /// </summary>
    class MouseRightClickAction : Action
    {
        internal const string ID = "MouseRightClick";

        internal MouseRightClickAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.MoveToAndClick(target);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// Left click action
    /// </summary>
    class MouseLeftClickAction : Action
    {
        internal const string ID = "MouseLeftClick";

        internal MouseLeftClickAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Point p = target.GetClickablePoint();
            MTI.Input.SendMouseInput(p.X, p.Y, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.LeftDown | MTI.SendMouseInputFlags.Move);
            MTI.Input.SendMouseInput(p.X, p.Y, 0, MTI.SendMouseInputFlags.Absolute | MTI.SendMouseInputFlags.LeftUp | MTI.SendMouseInputFlags.Move);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// Move action
    /// </summary>
    class MouseMoveAction : Action
    {
        internal const string ID = "MouseMove";

        internal MouseMoveAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.MoveTo(target);
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// Keyboard write text action
    /// </summary>
    class KeyboardWriteTextAction : Action
    {
        internal const string ID = "KeyboardWriteText";
        private string text = null;

        internal enum Attributes
        {
            Text
        }

        internal KeyboardWriteTextAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;

            // read action's attributes
            text = r.GetAttribute(Attributes.Text.ToString());
            if (text == null)
            {
                const string message = "Text must be specified for a KeyboardWriteText action";
                throw (new ArgumentException(message));
            }
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);

            // set the focus in the element
            target.SetFocus();

            // wait until the element is focused
            Thread.Sleep(100);

            // write text (was 300ms but that's just too slow)
            MTI.Input.SendUnicodeString(text, 1, 200);

            // log the action
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// Keyboard write key action
    /// </summary>
    class KeyboardWriteKeyAction : Action
    {
        internal const string ID = "KeyboardWriteKey";
        private string keyDescription = null;

        internal enum Attributes
        {
            Key
        }

        internal KeyboardWriteKeyAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;

            // read action's attributes
            keyDescription = r.GetAttribute(Attributes.Key.ToString());
            if (keyDescription == null)
            {
                const string message = "Text must be specified for a KeyboardWriteText action";
                throw (new ArgumentException(message));
            }
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            if (TargetID != null)
            {
                // get the target's automation element
                AutomationElement target = TargetResolver.Resolve(log, top, TargetID);

                // set the focus in the element
                target.SetFocus();
            }

            // wait until the element is focused
            Thread.Sleep(100);

            // press key
            Key key = (Key)Enum.Parse(typeof(Key), keyDescription);
            MTI.Input.SendKeyboardInput(key, true);

            // log the execution
            LogExecution(log, ID, TargetID);
        }
    }

    /// <summary>
    /// Resize action
    /// </summary>
    class ResizeAction : Action
    {
        internal const string ID = "Resize";
        internal double factor = 0.0;

        internal enum Attributes
        {
            Factor
        }

        internal ResizeAction(XmlTextReader r, string targetID)
        {
            TargetID = targetID;

            // get the factor
            string f = r.GetAttribute(Attributes.Factor.ToString());
            if (f == null)
            {
                const string message = "Factor must be specified for a Resize action";
                throw (new ArgumentException(message));
            }
            factor = Double.Parse(f, NumberFormatInfo.InvariantInfo);
        }

        override internal void Execute(ILog log, IntPtr top)
        {
            AutomationElement target = TargetResolver.Resolve(log, top, TargetID);
            Utils.ResizeWindow(target, factor);
            LogExecution(log, ID, TargetID);
        }
    }
}
