// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Runtime.InteropServices;
using Microsoft.Test.Logging;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Utilities;
using MTI = Microsoft.Test.Input;
using System.Reflection;
using System.Resources;
using Microsoft.Test.CrossProcess;

/*****************************************************
 * The logic in this file is maintained by the AppModel team
 * contact: mattgal
 *****************************************************/

namespace Microsoft.Test.Loaders.UIHandlers
{

    /// <summary>
    /// Logs error text from Browser-app exception page (but does not fail the test)
    /// </summary>
    public class BrowserAppExceptionLogger : UIHandler
    {

        #region Step Implementation
        /// <summary>
        /// Handles Browser Application error messages.  Currently just logs the text, so if other UIHandlers are there they can still execute.
        /// This will cause exception tests to time out instead of logging failure... we can change that easily later.
        /// </summary>
        /// <param name="topLevelhWnd"></param>
        /// <param name="hwnd"></param>
        /// <param name="process"></param>
        /// <param name="title"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, System.Diagnostics.Process process, string title, UIHandlerNotification notification)
        {
            return UIHandlerAction.Unhandled;
        }
        #endregion

        public static AutomationElement GetErrorDetails(AutomationElement moreInfoButton)
        {
            // Getting to the error details text box: It's a child of the More Info button's parent.
            // (IEWindow has many text boxes, so the search must be limited.)
            AutomationElement elem = TreeWalker.RawViewWalker.GetParent(moreInfoButton);
            AutomationElement errorDetailsElem = null;

            // In IE8, the UIA element for the error page switched from Edit-> Text
            // Unfortunately there are lots of ControlType.Texts here, so find the 2nd one with a value pattern.
            if (ApplicationDeploymentHelper.GetIEVersion() >= 8)
            {
                AutomationElementCollection collection = elem.FindAll(TreeScope.Descendants,
                    new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                    new PropertyCondition(AutomationElement.IsValuePatternAvailableProperty, true)));

                if (collection.Count >= 2)
                {
                    errorDetailsElem = collection[1];
                }
            }
            else
            {
                errorDetailsElem = elem.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
            }

            return errorDetailsElem;
        }
    }

    //Dismiss the Security Dialog
    internal class DismissSecurityDialog : UIHandler
    {
        #region Win32 Interop Imports

        // Interop is back :( for WOW64 cases.
        [DllImport("user32")]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool altTab);

        #endregion

        #region Step Implementation

        public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, System.Diagnostics.Process process, string title, UIHandlerNotification notification)
        {
            //Sleep a second to make sure that the dialog is ready
            Thread.Sleep(2000);

            //Make sure that the Security Dialog has focus
            GlobalLog.LogDebug("Switching to TrustManager dialog");
            AutomationElement trustManagerWindow;
            try
            {
                trustManagerWindow = AutomationElement.FromHandle(topLevelhWnd);

                // If we're running in a WOW64 Process, we need to do this without any automation.
                if (SystemInformation.Current.IsWow64Process)
                {
                    GlobalLog.LogDebug("Running in a WOW64 process... UI automation unavailable.  Using interop to dismiss dialog...");
                    //Make sure that the Security Dialog has focus
                    SwitchToThisWindow(topLevelhWnd, false);

                    // Switch from "Cancel" to "Install"...
                    MTI.Input.SendKeyboardInput(Key.LeftShift, true);
                    MTI.Input.SendKeyboardInput(Key.Tab, true);
                    MTI.Input.SendKeyboardInput(Key.Tab, false);
                    MTI.Input.SendKeyboardInput(Key.LeftShift, false);
                    // ... And hit enter
                    MTI.Input.SendKeyboardInput(Key.Enter, true);
                    MTI.Input.SendKeyboardInput(Key.Enter, false);
                }
                else
                {
                    //get the localized text of the run button
                    ResourceManager resMan = new ResourceManager("System.Windows.Forms", typeof(System.Windows.Forms.Form).Assembly);
                    string runBtnName = resMan.GetString("TrustManagerPromptUI_Run").Replace("&", "");

                    //try to click the run button with two identities then error out
                    if (tryInvokeByAutomationId(trustManagerWindow, "btnInstall") != true)
                        if (tryInvokeByName(trustManagerWindow, "Run Anyway") != true)
                        {
                            GlobalLog.LogEvidence("Failed to click any of the expected buttons on the Trust Dialog.  Aborting...");
                            return UIHandlerAction.Abort;
                        }
                }



                if (DictionaryStore.Current["HandledTrustManagerDialog"] == null)
                {
                    DictionaryStore.Current["HandledTrustManagerDialog"] = "once";
                }
                // If we're handling a WOW .application, there is an expected second trust prompt.
                // This will not be fixed until the Whidbey SP1, which is expected post Vista shipping...
                // The workaround is simply to pretend we only saw the dialog once if it's WOW64.
                // The 2nd prompt will have the same process name as the app, NOT dfsvc... so use this to trigger.
                else
                {
                    if (process.ProcessName == "dfsvc")
                    {
                        DictionaryStore.Current["HandledTrustManagerDialog"] = "multiple";
                    }
                    else
                    {
                        GlobalLog.LogEvidence("Ignoring second trust dialog as it appears to be a WOW64 application... contact mattgal if this is not the case");
                    }
                }

                return UIHandlerAction.Handled;
            }
            // This happens when the dialog is already being dismissed for other reasons.
            catch (System.Windows.Automation.ElementNotAvailableException)
            {
                return UIHandlerAction.Handled;
            }
        }

        #endregion

        #region Private Methods

        internal bool tryInvokeByAutomationId(AutomationElement window, string name)
        {
            PropertyCondition itemIdCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, name);
            return tryInvokeByPropertyCondition(itemIdCondition, window);
        }

        internal bool tryInvokeByName(AutomationElement window, string name)
        {
            PropertyCondition itemNameCondition = new PropertyCondition(AutomationElement.NameProperty, name);
            return tryInvokeByPropertyCondition(itemNameCondition, window);
        }

        internal bool tryInvokeByPropertyCondition(PropertyCondition cond, AutomationElement window)
        {
            AutomationElement theItem = window.FindFirst(TreeScope.Descendants, cond);
            if (theItem == null)
            {
                return false;
            }

            OperationCompletedObject result = new OperationCompletedObject();
            try
            {
                object patternObject;
                theItem.TryGetCurrentPattern(InvokePattern.Pattern, out patternObject);
                InvokePattern ip = patternObject as InvokePattern;

                // Eduardot: Just to wake up the queues...
                // This is a terrible hack but I want to validate
                // some suspicious about a bug on 64 bits and the install dialog.

                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tag = result;
                timer.Tick += delegate(object oTimer, EventArgs args)
                    {
                        System.Windows.Threading.DispatcherTimer currentTimer = (System.Windows.Threading.DispatcherTimer)oTimer;
                        OperationCompletedObject r = (OperationCompletedObject)(currentTimer).Tag;
                        if (r.Completed)
                        {
                            currentTimer.Stop();
                        }
                    };
                timer.Start();
                ip.Invoke();
            }
            catch (System.Windows.Automation.NoClickablePointException)
            {
                // If it throws the exception again, something is wrong (workaround not working around problem),
                // so this one should just let itself get thrown.
                // We sleep here so that other Trust dialogs that are popping up in the mean time get a chance to move
                // themselves to a different quadrant.  If the exception isnt thrown, there is no delay.
                Thread.Sleep(3000);
                object patternObject;
                theItem.TryGetCurrentPattern(InvokePattern.Pattern, out patternObject);
                InvokePattern ip = patternObject as InvokePattern;
                ip.Invoke();
            }
            finally
            {
                result.Completed = true;
            }
            return true;
        }

        #endregion

        // Used to pump dispatcher ... added by EduardoT?
        class OperationCompletedObject
        {
            public bool Completed = false;
        }
    }

    // Navigate to a given address when "about: NavigateIE" is in the address bar
    // Used by ActivationStep
    internal class NavigateIE : UIHandler
    {

        #region Private Members

        private Uri uriToNavigate;

        #endregion

        #region Constructors
        public NavigateIE(string uri)
        {
            uriToNavigate = new Uri(uri);
        }
        #endregion

        #region Step Implementation
        public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, System.Diagnostics.Process process, string title, UIHandlerNotification notification)
        {
            // Sleep for a bit.  Takes a bit longer for window to be ready in Longhorn.
            Thread.Sleep(800);

            // Get a reference to the IE window...
            GlobalLog.LogDebug("Finding IE window");
            AutomationElement ieWindow = AutomationElement.FromHandle(topLevelhWnd);
            if (ApplicationDeploymentHelper.GetIEVersion() >= 8)
            {
                GlobalLog.LogDebug("IE8+ present, using IWebBrowser2 Navigate() for reliability");
                Thread.Sleep(3000);
                // Other approaches are fine until they meet LCIE.
                // IF this proves to be 100% reliable, this could be the only implementation.
                IENavigationHelper.NavigateInternetExplorer(uriToNavigate.ToString(), process.MainWindowHandle);
            }
            else
            {
                NavigateIEWindow(ieWindow, uriToNavigate);
            }

            return UIHandlerAction.Unhandled;
        }
        #endregion

        #region Public Members

        public static void NavigateIEWindow(AutomationElement ieWindow, Uri UriToNavigate)
        {
            // Get a reference the address bar...
            GlobalLog.LogDebug("Finding the IE address bar");

            AndCondition isAddrBar = new AndCondition(new PropertyCondition(ValuePattern.ValueProperty, "about:NavigateIE"),
                                                       new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
            AutomationElement addressBar = null;
            try
            {
                addressBar = ieWindow.FindFirst(TreeScope.Descendants, isAddrBar);
            }
            catch // Any exception type... can be one of several
            // Retry this a few times in a loop before we go the "old" route.
            {
                for (int countdown = 15; countdown > 0; countdown--)
                {
                    try
                    {
                        Thread.Sleep(150);
                        addressBar = ieWindow.FindFirst(TreeScope.Descendants, isAddrBar);
                        countdown--;
                    }
                    catch { } // Still failing :(

                    // Success!
                    if (addressBar != null)
                    {
                        break;
                    }
                }
            }

            // Type the URI and press return
            GlobalLog.LogDebug("Typing " + UriToNavigate.ToString() + " into the address bar");

            if (addressBar != null)
            {
                // Don't focus the element because some OS'es will throw an InvalidOperationException
                // and this is unnecessary for ValuePattern.
                ValuePattern vp = addressBar.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                if (vp == null)
                {
                    throw new System.Exception("Couldn't get the valuePattern for the IE address bar! Please contact MattGal to fix this.");
                }
                vp.SetValue(UriToNavigate.ToString());
            }
            else
            {
                GlobalLog.LogEvidence("Could not set focus to address bar.  Attempting to continue...");
                //Sleep a few seconds to make sure that the address bar has focus
                Thread.Sleep(3500);
                // This section should only work as a backup, since this is unreliable in Japanese (possibly other) languages.
                // But if we fail to get the address bar or its valuepattern, it's better to try to continue.
                MTI.Input.SendUnicodeString(UriToNavigate.ToString(), 1, 15);
            }

            // Wait a second then send a 2nd Enter to the address bar if we're Japanese, as IME is on by default and requires this.
            // If this UIHandler fails for other languages, add their LCID's to the if statement.

            int tryCount = 10;
            Thread.Sleep(1500);

            AndCondition didNavigate = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                                                        new PropertyCondition(AutomationElement.NameProperty, "NavigateIE"));
            AutomationElement navigationSuccessElement = null;
            do
            {
                tryCount--;
                // Can't trust setfocus to work, so instead click on the address bar...
                try
                {
                    MTI.Input.MoveToAndClick(addressBar);
                }
                catch
                {
                    // Do nothing... we'll try again in a little bit hoping this has fixed itself.
                }
                // Should disable IME.  For most OS'es this is a no-op.  For east asian, this will preclude the need to hit enter twice
                // (this is unreliable to do by culture)
                // This is broken by DD bugs 201197 - Invariant Assert in TextServicesLoader.Load() when setting IME State
                // If IME is enabled and it matters, we're out of luck since this Asserts.
                // Renable (InputMethod.Current.ImeState = InputMethodState.Off) if this is ever fixed

                MTI.Input.SendKeyboardInput(Key.Enter, true);
                MTI.Input.SendKeyboardInput(Key.Enter, false);

                Thread.Sleep(2000);
                try
                {
                    navigationSuccessElement = ieWindow.FindFirst(TreeScope.Descendants, didNavigate);
                }
                catch // Any exception type... can be one of several
                {
                    // Do nothing.  Sometimes this happens when this handler is running while IE is shutting down.
                }
            }
            // Don't loop this in Vista... we'll take our chances.
            // LUA causes this to fail since some navigations launch new IE windows.
            while ((navigationSuccessElement != null) && tryCount > 0 && Environment.OSVersion.Version.Major != 6
                 && !UriToNavigate.ToString().EndsWith(ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION));
        }
        #endregion
    }


    // Used by ActivationStep for Method="Navigate" activations.
    internal class NavigateFF : UIHandler
    {
        #region Private Members
        // Don't use a URI here as NavigateIE does, since some paths (such as UNC) that System.Uri creates are "bad" for FireFox.
        private string uriToNavigate;

        #endregion

        #region Constructors
        public NavigateFF(string uri)
        {
            uriToNavigate = uri;
        }
        #endregion

        #region Step Implementation
        public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, System.Diagnostics.Process process, string title, UIHandlerNotification notification)
        {
            // Sleep for a bit.  Need to give the FF window a chance to render the address bar.
            Thread.Sleep(1000);

            // Get a reference to the IE window...
            GlobalLog.LogDebug("Navigating FireFox window to " + uriToNavigate);
            FireFoxAutomationHelper.NavigateFireFox(topLevelhWnd, uriToNavigate);

            return UIHandlerAction.Unhandled;
        }
        #endregion
    }
}
