// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Base class for all the Win32 and office Controls.
//
//
//              Only true ROOT object should derive from this class
//              NOTE: In the case when ProxyHwnd.ElementProviderFromPoint 
//                    returns provider of type ProxyFragment we MUST do the further drilling ourselves
//                    since UIAutomation will not (and correctly!!!) do it. (see ProxyFragment's
//                    comments on how to implement this and WindowsListView.cs for example)
//
//              Class ProxyHwnd: ProxyFragment, IRawElementProviderAdviseEvents
//                  AdviseEventAdded
//                  AdviseEventRemoved

using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows;
using MS.Win32;
using System;

namespace MS.Internal.AutomationProxies
{
    #region ProxyHwnd

    // Base Class for all the Windows Control that handle context.
    // Implements the default behavior
    internal class ProxyHwnd : ProxyFragment, IRawElementProviderAdviseEvents
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        internal ProxyHwnd (IntPtr hwnd, ProxyFragment parent, int item) 
            : base (hwnd, parent, item)
        {
        }
        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxyHwnd Methods

        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        // Advises proxy that an event has been added.
        // Maps the Automation Events into WinEvents and add those to the list of WinEvents notification hooks
        internal virtual void AdviseEventAdded(AutomationEvent eventId, AutomationProperty[] aidProps)
        {
            // No RawElementBase creation callback, exit from here
            if (_createOnEvent == null)
            {
                return;
            }

            ReadOnlySpan<WinEventTracker.EvtIdProperty> aEvents;

            // Gets an Array of WinEvents to trap on a per window handle basis
            if (eventId == AutomationElement.AutomationPropertyChangedEvent)
            {
                aEvents = PropertyToWinEvent(aidProps);
            }
            else
            {
                aEvents = EventToWinEvent(eventId);
            }

            // If we have WinEvents to trap, add those to the list of WinEvent
            // notification list
            if (aEvents.Length > 0)
            {
                WinEventTracker.AddToNotificationList(_hwnd, _createOnEvent, aEvents);
            }
        }

        // Advises proxy that an event has been removed.
        internal virtual void AdviseEventRemoved(AutomationEvent eventId, AutomationProperty [] aidProps)
        {
            // No RawElementBase creation callback, exit from here
            if (_createOnEvent == null)
            {
                return;
            }

            ReadOnlySpan<WinEventTracker.EvtIdProperty> aEvents;

            // Gets an Array of WinEvents to trap on a per window handle basis
            if (eventId == AutomationElement.AutomationPropertyChangedEvent)
            {
                aEvents = PropertyToWinEvent(aidProps);
            }
            else
            {
                aEvents = EventToWinEvent(eventId);
            }

            // If we have WinEvents to remove, remive those to the list of WinEvent
            // notification list
            if (aEvents.Length > 0)
            {
                WinEventTracker.RemoveToNotificationList(_hwnd, aEvents, null);
            }
        }

        // Returns a proxy element corresponding to the specified screen coordinates.
        // For an hwnd element, the default behavior is to let UIAutomation do the work. 
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            // Optimisation. If the point is within the client area return this, otherwise it could the the 
            // non client area. It would be better to return null all the time but this will lead to 
            // object to be created twice.
            return PtInClientRect (_hwnd, x, y) ? this : null;
        }

        internal override string GetAccessKey()
        {
            string accessKey = base.GetAccessKey();
            if ((bool)GetElementProperty(AutomationElement.IsKeyboardFocusableProperty))
            {
                if (string.IsNullOrEmpty(accessKey))
                {
                    accessKey = GetLabelAccessKey(_hwnd);
                }
            }
            return accessKey;
        }

        // Process all the Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            // if the hwnd is a winform, then return the Winform id otherwise let
            // UIAutomation do the job
            if (idProp == AutomationElement.AutomationIdProperty)
            {
                // Winforms have a special way to obtain the id
                if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms))
                {
                    string sPersistentID = WindowsFormsHelper.WindowsFormsID (_hwnd);
                    return string.IsNullOrEmpty(sPersistentID) ? null : sPersistentID;
                }
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                string name;
                // If this is a winforms control and the AccessibleName is set, use it.
                if (WindowsFormsHelper.IsWindowsFormsControl(_hwnd, ref _windowsForms))
                {
                    name = GetAccessibleName(NativeMethods.CHILD_SELF);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }

                // Only hwnd's can be labeled.
                name = LocalizedName;

                // It is valid to set LocalizedName to an empty string.  LocalizedName being an
                // empty string will prevent the SendMessage(WM_GETTEXT) call.
                if (name == null && GetParent() == null)
                {
                    if (_fControlHasLabel)
                    {
                        IntPtr label = Misc.GetLabelhwnd(_hwnd);
                        name = Misc.GetControlName(label, true);
                        if (!string.IsNullOrEmpty(name))
                        {
                            _controlLabel = label;
                        }
                    }
                    else
                    {
                        name = Misc.ProxyGetText(_hwnd);
                    }
                }


                // If name is still null, and we have an IAccessible, try it:
                // this picks up names on HWNDs set through Dynamic Annotation
                // (eg. on the richedits in Windows Mail), and holds us over till
                // we add DA support to UIA properly.
                if (String.IsNullOrEmpty(name))
                {
                    name = GetAccessibleName(NativeMethods.CHILD_SELF);
                }
                return name;
            }
            // Only hwnd's can be labeled.
            else if (idProp == AutomationElement.LabeledByProperty && _fControlHasLabel)
            {
                // This is called to make sure that _controlLabel gets set.
                object name = GetElementProperty(AutomationElement.NameProperty);

                // If a control has a LocalizedName, the _controlLabel will not get set.
                // So look for it now.
                if (_controlLabel == IntPtr.Zero && name != null && GetParent() == null)
                {
                    _controlLabel = Misc.GetLabelhwnd(_hwnd);
                }

                // If we have a cached _controlLabel that means that the name property we just got 
                // was retreived from the label of the control and not its text or something else.  If this 
                // is the case expose it as the label.
                if (_controlLabel != IntPtr.Zero)
                {
                    return AutomationInteropProvider.HostProviderFromHandle(_controlLabel);
                }
            }
            else if (idProp == AutomationElement.IsOffscreenProperty)
            {
                if (!SafeNativeMethods.IsWindowVisible(_hwnd))
                {
                    return true;
                }

                IntPtr hwndParent = Misc.GetParent(_hwnd);
                // Check if rect is within rect of parent. Don't do this for top-level windows,
                // however, since the win32 desktop hwnd claims to have a rect only as large as the
                // primary monitor, making hwnds on other monitors seem clipped.
                if (hwndParent != IntPtr.Zero && hwndParent != UnsafeNativeMethods.GetDesktopWindow())
                {
                    NativeMethods.Win32Rect parentRect = NativeMethods.Win32Rect.Empty;
                    if (Misc.GetClientRectInScreenCoordinates(hwndParent, ref parentRect) && !parentRect.IsEmpty)
                    {
                        Rect itemRect = BoundingRectangle;

                        if (!itemRect.IsEmpty && !Misc.IsItemVisible(ref parentRect, ref itemRect))
                        {
                            return true;
                        }
                    }
                }
            }

            return base.GetElementProperty(idProp);
        }

        // Gets the controls help text
        internal override string HelpText
        {
            get
            {
                int idChild = Misc.GetWindowId(_hwnd);
                string text = Misc.GetItemToolTipText(_hwnd, IntPtr.Zero, idChild);
                if (string.IsNullOrEmpty(text))
                {
                    text = Misc.GetItemToolTipText(_hwnd, IntPtr.Zero, 0);
                }

                if (string.IsNullOrEmpty(text))
                {
                    Accessible acc = Accessible.Wrap(AccessibleObject);
                    if (acc != null)
                    {
                        text = acc.Description;
                    }
                }

                return text;
            }
        }

        #endregion

        #region IRawElementProviderAdviseEvents Interface

        // ------------------------------------------------------
        //
        // IRawElementProviderAdviseEvents interface implementation
        //
        // ------------------------------------------------------
        // Advises event sinks that an event has been added.
        void IRawElementProviderAdviseEvents.AdviseEventAdded(int eventIdAsInt, int[] propertiesAsInts)
        {
            AutomationEvent eventId = AutomationEvent.LookupById(eventIdAsInt);
            AutomationProperty [] properties = null;
            if (propertiesAsInts != null)
            {
                properties = new AutomationProperty[propertiesAsInts.Length];
                for (int i = 0; i < propertiesAsInts.Length; i++)
                {
                    properties[i] = AutomationProperty.LookupById(propertiesAsInts[i]);
                }
            }

            //ProxyHwnd.AdviseEventAdded
            AdviseEventAdded (eventId, properties);
        }

        // Advises event sinks that an event has been removed.
        void IRawElementProviderAdviseEvents.AdviseEventRemoved(int eventIdAsInt, int[] propertiesAsInts)
        {
            AutomationEvent eventId = AutomationEvent.LookupById(eventIdAsInt);
            AutomationProperty [] properties = null;
            if (propertiesAsInts != null)
            {
                properties = new AutomationProperty[propertiesAsInts.Length];
                for (int i = 0; i < propertiesAsInts.Length; i++)
                {
                    properties[i] = AutomationProperty.LookupById(propertiesAsInts[i]);
                }
            }

            //ProxyHwnd.AdviseEventRemoved
            AdviseEventRemoved (eventId, properties);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        #region Internal Fields

        // Callback into the Proxy code to create a raw element based on a WinEvent callback parameters
        internal WinEventTracker.ProxyRaiseEvents _createOnEvent = null;

        #endregion

        // ------------------------------------------------------
        //
        // Protected Methods
        //
        // ------------------------------------------------------

        #region Protected Methods

        // Picks a WinEvent to track for a UIA property
        protected virtual int [] PropertyToWinEvent (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                return new int [] { NativeMethods.EventObjectFocus };
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                return new int[] { NativeMethods.EventObjectNameChange };
            }
            else if (idProp == ValuePattern.ValueProperty || idProp == RangeValuePattern.ValueProperty)
            {
                return new int[] { NativeMethods.EventObjectValueChange };
            }
            else if (idProp == AutomationElement.IsOffscreenProperty)
            {
                return new int[] { NativeMethods.EventObjectLocationChange };
            }
            else if (idProp == ExpandCollapsePattern.ExpandCollapseStateProperty)
            {
                return new int [] { NativeMethods.EventObjectStateChange,
                                    NativeMethods.EventObjectShow,
                                    NativeMethods.EventObjectHide};
            }

            // Windows sent OBJECT_VALUECHANGE for changes in the scroll bar with the idObject set to the scroll bar id of the originator
            else if ((idProp == ScrollPattern.HorizontalScrollPercentProperty || 
                idProp == ScrollPattern.VerticalScrollPercentProperty) || 
                idProp == ScrollPattern.HorizontalViewSizeProperty ||
                idProp == ScrollPattern.VerticalViewSizeProperty )
            {
                return new int [] { NativeMethods.EventObjectValueChange };
            }
            else if (idProp == SelectionItemPattern.IsSelectedProperty)
            {
                return new int [] { NativeMethods.EventObjectSelectionAdd, 
                                    NativeMethods.EventObjectSelectionRemove, 
                                    NativeMethods.EventObjectSelection};
            }
            else if (idProp == TogglePattern.ToggleStateProperty)
            {
                return new int[] { NativeMethods.EventSystemCaptureEnd,
                                   NativeMethods.EventObjectStateChange };
            }

            return null;
        }

        /// <summary> Builds a list of Win32 WinEvents to process a UIAutomation Event. </summary>
        /// <param name="idEvent"> An UIAutomation event. </param>
        /// <returns> Returns an array of Events to Set. </returns>
        protected virtual ReadOnlySpan<WinEventTracker.EvtIdProperty> EventToWinEvent(AutomationEvent idEvent)
        {
            // Fill this variable with a WinEvent id if found
            int idWinEvent;

            if (idEvent == SelectionItemPattern.ElementSelectedEvent)
            {
                return new WinEventTracker.EvtIdProperty[2]
                {
                    new(NativeMethods.EventObjectSelection, idEvent), 
                    new(NativeMethods.EventObjectStateChange, idEvent)
                };
            }
            else if (idEvent == SelectionItemPattern.ElementAddedToSelectionEvent)
            {
                // For some control, the Event Selection is sent instead of SelectionAdd
                // Trap both.
                return new WinEventTracker.EvtIdProperty [2]
                {
                    new(NativeMethods.EventObjectSelectionAdd, idEvent), 
                    new(NativeMethods.EventObjectSelection, idEvent)
                };
            }
            else if (idEvent == SelectionItemPattern.ElementRemovedFromSelectionEvent)
            {
                idWinEvent = NativeMethods.EventObjectSelectionRemove;
            }
            else if (idEvent == SelectionPattern.InvalidatedEvent)
            {
                idWinEvent = NativeMethods.EventObjectSelectionWithin;
            }
            else if (idEvent == InvokePattern.InvokedEvent)
            {
                return new WinEventTracker.EvtIdProperty[4] { 
                    new(NativeMethods.EventSystemCaptureEnd, idEvent), // For SysHeaders
                    new(NativeMethods.EventObjectStateChange, idEvent),
                    new(NativeMethods.EventObjectValueChange, idEvent), // For WindowsScrollBarBits
                    new(NativeMethods.EventObjectInvoke, idEvent)
                };
            }
            else if (idEvent == AutomationElement.StructureChangedEvent)
            {
                return new WinEventTracker.EvtIdProperty[3] { 
                    new(NativeMethods.EventObjectCreate, idEvent), 
                    new(NativeMethods.EventObjectDestroy, idEvent), 
                    new(NativeMethods.EventObjectReorder, idEvent) 
                };
            }
            else if (idEvent == TextPattern.TextSelectionChangedEvent)
            {
                return new WinEventTracker.EvtIdProperty[2] {
                    new(NativeMethods.EventObjectLocationChange, idEvent),
                    new(NativeMethods.EventObjectTextSelectionChanged, idEvent)
                };
            }
            else
            {
                return ReadOnlySpan<WinEventTracker.EvtIdProperty>.Empty;
            }

            // found one and only one
            return new WinEventTracker.EvtIdProperty [1] { new(idWinEvent, idEvent) };
        }
        
        // Check if a point is within the client Rect of a window
        static protected bool PtInClientRect (IntPtr hwnd, int x, int y)
        {
            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect ();
            if (!Misc.GetClientRect(hwnd, ref rc))
            {
                return false;
            }

            if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rc, 2))
            {
                return false;
            }
            return x >= rc.left && x < rc.right && y >= rc.top && y < rc.bottom;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        // Get the access key of the associated label (if there is
        // an associated label).
        static protected string GetLabelAccessKey(IntPtr hwnd)
        {
            string accessKey = string.Empty;
            IntPtr label = Misc.GetLabelhwnd(hwnd);
            if (label != IntPtr.Zero)
            {
                string labelText = Misc.ProxyGetText(label);
                if (!string.IsNullOrEmpty(labelText))
                {
                    accessKey = Misc.AccessKey(labelText);
                }
            }
            return accessKey;
        }

        // Builds a list of Win32 WinEvents to process changes in properties changes values.
        // Returns an array of Events to Set. The number of valid entries in this array is pass back in cEvents
        private ReadOnlySpan<WinEventTracker.EvtIdProperty> PropertyToWinEvent(AutomationProperty[] aProps)
        {
            List<WinEventTracker.EvtIdProperty> automationEvents = new(16);

            foreach (AutomationProperty idProp in aProps)
            {
                int[] evtId = PropertyToWinEvent(idProp);

                for (int i = 0; evtId != null && i < evtId.Length; i++)
                {
                    automationEvents.Add(new WinEventTracker.EvtIdProperty(evtId[i], idProp));
                }

            }

            return CollectionsMarshal.AsSpan(automationEvents);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // True if the control has an associated label.
        protected bool _fControlHasLabel = true;

        // The hwnd of static text that is functioning as a label for a control.
        // This is initialized in GetElementProperty for the Name property and
        // used by the LabeledBy property.
        // If !_fControlHasLabel, _controlLabel will be IntPtr.Zero.
        private IntPtr _controlLabel;

        #endregion
    }
    #endregion
}
