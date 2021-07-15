// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Button Proxy

using System;
using System.Collections;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Windows Button proxy
    class WindowsButton : ProxyHwnd, IInvokeProvider, IToggleProvider, ISelectionProvider, ISelectionItemProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Contructor for Button Proxy class.
        // param "hwnd", Windows handle
        // param "parent", Proxy Parent. Null if it is a root fragment
        // param "type", Button / Checkbox / Radio / Group
        // param "style", Button Style (BS_*) also used as the Proxy ID
        // Made internal so that WinFormsHelper.CreateButton can use.
        internal WindowsButton (IntPtr hwnd, ProxyFragment parent, ButtonType type, int style, Accessible acc)
            : base( hwnd, parent, 0)
        {
            _type = type;
            _fIsKeyboardFocusable = true;
            _style = style;
            _acc = acc;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);

            // Set ControlType based on type
            // Note: Do not call LocalizedName() within the constructor
            // since it is a virtual method.  Calling a virtual method
            // in a constructor would have unintended consequences for
            // derived classes.
            if(type == ButtonType.PushButton)
            {
                _cControlType = ControlType.Button;
                _fControlHasLabel = false;
            }
            else if(type == ButtonType.CheckBox)
            {
                _cControlType = ControlType.CheckBox;

                // If a check box has non-empty text, it has no associated label.
                _fControlHasLabel = string.IsNullOrEmpty(GetLocalizedName());
            }
            else if(type == ButtonType.RadioButton)
            {
                _cControlType = ControlType.RadioButton;

                // If a radio button has non-empty text, it has no associated label.
                _fControlHasLabel = string.IsNullOrEmpty(GetLocalizedName());
            }
            else if (type == ButtonType.GroupBox)
            {
                _cControlType = ControlType.Group;
                _fIsKeyboardFocusable = false;
                // If a group box has non-empty text, it has no associated label.
                _fControlHasLabel = string.IsNullOrEmpty(GetLocalizedName());
            }
            else
            {
                _cControlType = ControlType.Custom;
            }
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful 
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            ButtonType type;
            int style;
            
            try
            {
                if (WindowsFormsHelper.IsWindowsFormsControl(hwnd))
                {
                    return WindowsFormsHelper.CreateButton(hwnd);
                }

                style = Misc.GetWindowStyle(hwnd) & NativeMethods.BS_TYPEMASK;

                switch (style)
                {
                    case NativeMethods.BS_PUSHBUTTON:
                    case NativeMethods.BS_DEFPUSHBUTTON:
                    case NativeMethods.BS_OWNERDRAW:
                    case NativeMethods.BS_SPLITBUTTON: // explore back and forward buttons
                        type = ButtonType.PushButton;
                        break;

                    case NativeMethods.BS_CHECKBOX:
                    case NativeMethods.BS_AUTOCHECKBOX:
                    case NativeMethods.BS_3STATE:
                    case NativeMethods.BS_AUTO3STATE:
                        type = ButtonType.CheckBox;
                        break;

                    case NativeMethods.BS_RADIOBUTTON:
                    case NativeMethods.BS_AUTORADIOBUTTON:
                        type = ButtonType.RadioButton;
                        break;

                    case NativeMethods.BS_GROUPBOX:
                        type = ButtonType.GroupBox;
                        break;

                    default:
                        return null;
                }
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }

            return new WindowsButton(hwnd, null, type, style, null);
        }

        // Static create method called by the event tracker system.
        // WinEvents are thrown only when a notification has been set for a
        // specific item. Create the item first and check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                // Can not RaiseEvents on windows that are no longer available.
                if (!UnsafeNativeMethods.IsWindow(hwnd))
                {
                    throw new ElementNotAvailableException();
                }

                WindowsButton wtv = (WindowsButton)Create(hwnd, 0);

                // Create can return null if we don't know what kind of button this is
                if (wtv == null)
                {
                    return;
                }
                
                //Only one event is generated for the winforms button so no need to check the pressed state.
                if (wtv._acc != null)
                {
                    if (idProp == SelectionItemPattern.ElementSelectedEvent)
                    {
                        if (!wtv._acc.HasState(AccessibleState.Checked))
                        {
                            eventId = NativeMethods.EventObjectSelectionRemove;
                            idProp = SelectionItemPattern.ElementRemovedFromSelectionEvent;
                        }
                    }
                    wtv.DispatchEvents(eventId, idProp, idObject, idChild);
                }
                else
                {
                    if (idProp == InvokePattern.InvokedEvent)
                    {
                        // On XP, this event is triggered by the WinEvent
                        // EventObjectStateChange (fired when a button is
                        // pressed) since EventObjectInvoke is not available on XP.
                        // However, this event is not fired reliably since it
                        // is sensitive to timing issues during the button press.
                        // The new event EventObjectInvoke, available only
                        // on Vista, is fired reliably and corrects this.
                        if (Environment.OSVersion.Version.Major < 6)
                        {
                            int state = Misc.ProxySendMessageInt(hwnd, NativeMethods.BM_GETSTATE, IntPtr.Zero, IntPtr.Zero);
                            if (Misc.IsBitSet(state, NativeMethods.BST_PUSHED)
                                    && eventId == NativeMethods.EventObjectStateChange)
                            {
                                wtv.DispatchEvents(eventId, idProp, idObject, idChild);
                            }
                        }
                        else if(eventId == NativeMethods.EventObjectInvoke)
                        {
                            // Vista or greater.  To avoid duplicate firings of the InvokedEvent,
                            // only dispatch this when the eventId is EventObjectInvoke.
                            wtv.DispatchEvents(eventId, idProp, idObject, idChild);
                        }
                    }
                    else
                    {
                        wtv.DispatchEvents(eventId, idProp, idObject, idChild);
                    }
                }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            if (iid == InvokePattern.Pattern && _type == ButtonType.PushButton)
            {
                return this;
            }
            // Only WinForms RadioGroups should have this pattern.
            else if (iid == SelectionPattern.Pattern && _type == ButtonType.GroupBox)
            {
                return ContainsRadioButtons()? this : null;
            }
            else if (iid == SelectionItemPattern.Pattern && _type == ButtonType.RadioButton)
            {
                return this;
            }
            else if (iid == TogglePattern.Pattern && _type == ButtonType.CheckBox)
            {
                return this;
            }

            return null;
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.AccessKeyProperty)
            {
                // Special handling for forms
                if (!WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms) && IsStartButton())
                {
                    // Hard coded shortcut for the start button
                    return SR.Get(SRID.KeyCtrl) + " + " + SR.Get(SRID.KeyEsc);
                }
                return Misc.AccessKey(Misc.ProxyGetText(_hwnd));
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                if (InShellTray())
                {
                    return SafeNativeMethods.IsWindowVisible(_hwnd);
                }
            }

            return base.GetElementProperty (idProp);
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return GetLocalizedName();
            }
        }

        #endregion

        #region ProxyHwnd Overrides

        // Builds a list of Win32 WinEvents to process a UIAutomation Event.
        protected override WinEventTracker.EvtIdProperty[] EventToWinEvent(AutomationEvent idEvent, out int cEvent)
        {
            // For Vista, we only need register for EventObjectInvoke to handle InvokePattern.InvokedEvent.
            // For XP, we rely on state changes, handled in ProxyHwnd.EventToWinEvent().
            if (idEvent == InvokePattern.InvokedEvent && Environment.OSVersion.Version.Major >= 6)
            {
                cEvent = 1;
                return new WinEventTracker.EvtIdProperty[] { 
                    new WinEventTracker.EvtIdProperty (NativeMethods.EventObjectInvoke, idEvent)
                };
            }

            return base.EventToWinEvent(idEvent, out cEvent);
        }

        #endregion

        #region Invoke Pattern

        // Click the button
        void IInvokeProvider.Invoke ()
        {
            Invoke();
        }

        #endregion Invoke Pattern

        #region Selection Pattern

        // Returns an enumerator over the current selection.
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            IRawElementProviderSimple[] selection = null;
            Accessible accRadioButton = null;
            IntPtr hwndRadioButton = GetSelection();

            if (hwndRadioButton == IntPtr.Zero ||
                Accessible.AccessibleObjectFromWindow(hwndRadioButton, NativeMethods.OBJID_CLIENT, ref accRadioButton) != NativeMethods.S_OK ||
                accRadioButton == null)
            {
                // framework will handle this one correctly
                return null;
            }
            else
            {
                selection = new IRawElementProviderSimple[] 
                    {
                        new WindowsButton(hwndRadioButton, null, ButtonType.RadioButton, Misc.GetWindowStyle(hwndRadioButton) & NativeMethods.BS_TYPEMASK, accRadioButton)
                    };
            }

            return selection;
        }

        // Returns whether the control requires a minimum of one selected element at all times.
        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return true;
            }
        }

        // Returns whether the control supports multiple selection.
        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return false;
            }
        }

        #endregion Selection Pattern

        #region SelectionItem Pattern

        // Selects this element
        void ISelectionItemProvider.Select()
        {
            Invoke();
        }

        // Adds this element to the selection
        void ISelectionItemProvider.AddToSelection()
        {
            throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
        }

        // Removes this element from the selection
        void ISelectionItemProvider.RemoveFromSelection()
        {
            throw new InvalidOperationException(SR.Get(SRID.DoesNotSupportMultipleSelection));
        }

        // True if this element is part of the the selection
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                return ToggleState == ToggleState.On ? true : false;
            }
        }

        // Returns the container for this element
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                IntPtr hwndParent = Misc.GetParent(_hwnd);
                if (hwndParent != IntPtr.Zero && WindowsFormsHelper.IsWindowsFormsControl(hwndParent))
                {
                    Accessible accParent = null;
                    if (Accessible.AccessibleObjectFromWindow(hwndParent, NativeMethods.OBJID_CLIENT, ref accParent) != NativeMethods.S_OK || accParent == null)
                    {
                        return null;
                    }

                    if (accParent.Role == AccessibleRole.Grouping)
                    {
                        return new WindowsButton(hwndParent, null, ButtonType.GroupBox, Misc.GetWindowStyle(hwndParent) & NativeMethods.BS_TYPEMASK, accParent);
                    }
                }

                return null;
            }
        }

        #endregion SelectionItem Pattern

        #region IToggleProvider

        void IToggleProvider.Toggle()
        {
            // This pattern is only supported for checkboxes and radio buttons
            // so the invoke will never invoke a normal button.
            Invoke();
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                return ToggleState;
            }
        }

        #endregion IToggleProvider

        // ------------------------------------------------------
        //
        // Internal Types
        //
        // ------------------------------------------------------

        #region Internal Types

        // Button control types based on groupings of style constants
        // Made internal so that WinFormsHelper can use.
        internal enum ButtonType
        {
            PushButton,
            CheckBox,
            RadioButton,
            GroupBox
        };

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private void Invoke()
        {
            // Check that button can be clicked
            // This state could change anytime
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // Moved this outside the if block because it's needed for WinForms, which uses _acc.DoDefaultAction()
            if (!IsShowAllProgramsButton())
            {
                // SetFocus is needed here to workaround a bug
                Misc.SetFocus(_hwnd);
            }

            if (_acc == null)
            {
                switch (_style)
                {
                    case NativeMethods.BS_PUSHBUTTON:
                    case NativeMethods.BS_DEFPUSHBUTTON:
                    case NativeMethods.BS_PUSHBOX:
                    case NativeMethods.BS_OWNERDRAW:
                    case NativeMethods.BS_USERBUTTON:
                    case NativeMethods.BS_CHECKBOX:
                    case NativeMethods.BS_AUTOCHECKBOX:
                    case NativeMethods.BS_RADIOBUTTON:
                    case NativeMethods.BS_AUTORADIOBUTTON:
                    case NativeMethods.BS_3STATE:
                    case NativeMethods.BS_AUTO3STATE:
                    case NativeMethods.BS_SPLITBUTTON: // explore back and forward buttons

                        if (IsStartButton())
                        {
                            // You can't just click the start button; it won't do
                            // anything if the tray isn't active except take focus
                            Misc.PostMessage(_hwnd, NativeMethods.WM_SYSCOMMAND, new IntPtr(NativeMethods.SC_TASKLIST), IntPtr.Zero);
                            break;
                        }

                        if (_type == ButtonType.PushButton && !IsStartButton())
                        {
                            // For the Invoke event to work, there needs to be time between the OBJ_STATECHANGE 
                            // for pushing the button and the OBJ_STATECHANGE for releasing the button.
                            // For buttons the OBJ_STATECHANGE is caused by the BM_SETSTATE message.
                            // The BM_CLICK causes these BM_SETSTATE's to happen to fast, the OBJ_STATECHANGES
                            // are received simultaneous. This does not give enough time to check the button pushed
                            // state in the event handler, cause the state to be missed and the Invoke event not 
                            // being raised.  Send an extra BM_SETSTATE to allow the event handler to be able to
                            // see the state change and raise the Invoke event.
                            Misc.ProxySendMessage(_hwnd, NativeMethods.BM_SETSTATE, new IntPtr(1), IntPtr.Zero, true);
                            System.Threading.Thread.Sleep(1);
                        }

                        try
                        {
                            // Now cause the button click.
                            Misc.ProxySendMessage(_hwnd, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero, true);
                        }
                        catch (ElementNotAvailableException)
                        {
                            // There is a timing issue with the SendMessage and
                            // the Cancel button on the Log Off Dialog box.  The button with be invoked but sometimes
                            // the SendMessage will return a failure that will cause the ElementNotAvailableException
                            // to be thrown.
                            return;
                        }

                        break;
                }
            }
            else
            {
                _acc.DoDefaultAction();
            }
        }

        private bool InShellTray()
        {
            IntPtr hwndShell = Misc.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            if (hwndShell != IntPtr.Zero)
            {
                // if the outter most window/dialog box of the button is the shell window return true.
                return GetRootAncestor() == hwndShell;
            }

            return false;
        }

        private bool IsStartButton()
        {
            if (!Misc.GetClassName(_hwnd).Equals("Button"))
            {
                return false;
            }

            // Vista's start button is top-level - use this check for it (leveraged from oleacc)
            if (Environment.OSVersion.Version.Major >= 6)
            {
                return Misc.InTheShellProcess(_hwnd) && UnsafeNativeMethods.GetProp(_hwnd, "StartButtonTag") == new IntPtr(304);
            }
            else
            {
                IntPtr hwndParent = Misc.GetParent(_hwnd);
                if (hwndParent != IntPtr.Zero)
                {
                    if (Misc.GetClassName(hwndParent).Equals("Shell_TrayWnd"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool IsShowAllProgramsButton()
        {
            if (!Misc.GetClassName(_hwnd).Equals("Button"))
            {
                return false;
            }

            IntPtr hwndParent = Misc.GetParent(_hwnd);
            if (hwndParent != IntPtr.Zero)
            {
                if (Misc.GetClassName(hwndParent).Equals("Desktop More Programs Pane"))
                {
                    return true;
                }
            }

            return false;
        }

        // Find the outter most window/dialog box that contains the button.
        private IntPtr GetRootAncestor()
        {
            IntPtr hwndParent = _hwnd;
            IntPtr hwndRoot;
            do
            {
                // Have not found the outter most window/dialog box yet, so take one more step out.
                hwndRoot = hwndParent;
                if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_CHILD))
                {
                    hwndParent = Misc.GetParent(hwndRoot);
                }
                else
                {
                    hwndParent = Misc.GetWindow(hwndRoot, NativeMethods.GW_OWNER);
                }
            // is the parent of this root the desktop?  If so root is the outter most window/dialog box.
            } while (hwndParent != _hwndDesktop && hwndParent != IntPtr.Zero);

            return hwndRoot;
        }

        private ToggleState ToggleState
        {
            get
            {
                ToggleState icsState;

                // Special handling for forms
                if (_acc != null)
                {
                    AccessibleState state = _acc.State;
                    if (Accessible.HasState(state, AccessibleState.Checked))
                    {
                        icsState = ToggleState.On;
                    }
                    else if (Accessible.HasState(state, AccessibleState.Mixed))
                    {
                        icsState = ToggleState.Indeterminate;
                    }
                    else
                    {
                        icsState = ToggleState.Off;
                    }
                }
                else
                {
                    int state = Misc.ProxySendMessageInt(_hwnd, NativeMethods.BM_GETCHECK, IntPtr.Zero, IntPtr.Zero);
                    if (Misc.IsBitSet(state, NativeMethods.BST_CHECKED))
                    {
                        icsState = ToggleState.On;
                    }
                    else if (Misc.IsBitSet(state, NativeMethods.BST_INDETERMINATE))
                    {
                        icsState = ToggleState.Indeterminate;
                    }
                    else
                    {
                        icsState = ToggleState.Off;
                    }  
                }
                
                return icsState;
            }
        }
        
        unsafe private bool ContainsRadioButtons()
        {
            bool radiobuttonChildFound = false;
            // WinForm GroupBoxes have a parent/child relationship.  Win32 GroupBoxes do not.
            if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms))
            {
                Misc.EnumChildWindows(_hwnd, new NativeMethods.EnumChildrenCallbackVoid(FindRadioButtonChild), (void*)&radiobuttonChildFound);
            }
            return radiobuttonChildFound;
        }

        unsafe private bool FindRadioButtonChild(IntPtr hwnd, void* lParam)
        {
            // Only be concerned with Winforms child controls.
            if (!WindowsFormsHelper.IsWindowsFormsControl(hwnd))
            {
                return true;
            }

            Accessible acc = null;
            if (Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) == NativeMethods.S_OK &&
                acc != null && 
                acc.Role == AccessibleRole.RadioButton)
            {
                *(bool*)lParam = true;
                return false;
            }

            return true;
        }

        // Private method to encapsulate logic used by both the constructor
        // and the virtual LocalizedName property.
        private string GetLocalizedName()
        {
            return Misc.StripMnemonic(Misc.ProxyGetText(_hwnd));
        }

        private unsafe IntPtr GetSelection()
        {
            // WinForm GroupBoxes have a parent/child relationship.  Win32 GroupBoxes do not.
            if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms))
            {
                IntPtr selectedRadiobutton = new IntPtr(0);
                Misc.EnumChildWindows(_hwnd, new NativeMethods.EnumChildrenCallbackVoid(FindSelectedRadioButtonChild), (void*)&selectedRadiobutton);
                return selectedRadiobutton;
            }

            return IntPtr.Zero;
        }

        private unsafe bool FindSelectedRadioButtonChild(IntPtr hwnd, void* lParam)
        {
            // Only be concerned with Winforms child controls.
            if (!WindowsFormsHelper.IsWindowsFormsControl(hwnd))
            {
                return true;
            }

            Accessible acc = null;
            if (Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) == NativeMethods.S_OK &&
                acc != null &&
                acc.Role == AccessibleRole.RadioButton &&
                acc.HasState(AccessibleState.Checked))
            {
                *(IntPtr*)lParam = hwnd;
                return false;
            }

            return true;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private ButtonType _type;
        private int _style;
        private Accessible _acc;  // Accessible is used for WinForms Buttons.

        #endregion
    }
}
