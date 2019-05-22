// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class to manage UIAutomation events and how they relate to winevents

using System;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections;
using Accessibility;
using System.Windows;
using System.Windows.Input;
using System.Globalization;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // Class to manage UIAutomation events and how they relate to winevents
    static class EventManager
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        
        #endregion
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static void DispatchEvent(ProxySimple el, IntPtr hwnd, int eventId, object idProp, int idObject)
        {
            // This logic uses a hastables in order to get to a delegate that will raise the correct Automation event
            // that may be a property change event a Automation event or a structure changed event. 
            // There are three hashtables one for each idObject we support.  Depending on the idObject that gets
            // passed in we access one of these hashtables with a key of an automation identifier and then retrieve 
            // the data which ia a delegate of type RasieEvent.  This delegate is called to raise the correct type of event.
            RaiseEvent raiseEvent = null;

            switch (idObject)
            {
                case NativeMethods.OBJID_WINDOW:
                    lock (_classLock)
                    {
                        if (_objectIdWindow == null)
                            InitObjectIdWindow();
                    }
                    
                    raiseEvent = (RaiseEvent)_objectIdWindow[idProp];
                    break;
                    
                case NativeMethods.OBJID_CLIENT:
                    lock (_classLock)
                    {
                        if (_objectIdClient == null)
                            InitObjectIdClient();
                    }

                    raiseEvent = (RaiseEvent)_objectIdClient[idProp];
                    break;
                    
                case NativeMethods.OBJID_VSCROLL:
                case NativeMethods.OBJID_HSCROLL:
                    lock (_classLock)
                    {
                        if (_objectIdScroll == null)
                            InitObjectIdScroll();
                    }

                    raiseEvent = (RaiseEvent)_objectIdScroll[idProp];
                    break;

                case NativeMethods.OBJID_CARET:
                    lock (_classLock)
                    {
                        if (_objectIdCaret == null)
                            InitObjectIdCaret();
                    }

                    raiseEvent = (RaiseEvent)_objectIdCaret[idProp];
                    break;

                case NativeMethods.OBJID_SYSMENU:
                case NativeMethods.OBJID_MENU:
                    lock (_classLock)
                    {
                        if (_objectIdMenu == null)
                            InitObjectIdMenu();
                    }

                    raiseEvent = (RaiseEvent)_objectIdMenu[idProp];
                    break;

                default:
                    // Commented out to remove annoying asserts temporarily.
                    // (See work item PS1254940.)
                    //System.Diagnostics.Debug.Assert(false, "Unexpected idObject " + idObject);
                    return;
            }
            
            
            if (raiseEvent != null)
            {
                raiseEvent(el, hwnd, eventId);
            }
            else
            {
                // If there is no delegate then we need to handle this property genericly by just getting the property value
                // and raising the a property changed event.
                AutomationProperty property = idProp as AutomationProperty;
                if (property == null)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Unexpected idProp {0} for idOject 0x{1:x8} on element {2} for event {3}", idProp, idObject, el, eventId));
                    return;
                }

                object propertyValue = ((IRawElementProviderSimple)el).GetPropertyValue(property.Id);

                RaisePropertyChangedEvent(el, property,  propertyValue);
            }
        }

        #endregion
        
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static void HandleIsReadOnlyProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            // Special call for the ReadOnly Proxperty. If a Windows, becomes
            // enabled/disabled, all of its non hwnd children should become
            // change the read only state.
            if (eventId == NativeMethods.EventObjectStateChange)
            {
                bool fIsReadOnly = SafeNativeMethods.IsWindowEnabled(hwnd);

                el.RecursiveRaiseEvents(ValuePattern.IsReadOnlyProperty, new AutomationPropertyChangedEventArgs(ValuePattern.IsReadOnlyProperty, null, fIsReadOnly));
            }

        }

        private static void HandleStructureChangedEventWindow(ProxySimple el, IntPtr hwnd, int eventId)
        {
            if (eventId == NativeMethods.EventObjectReorder)
            {
                AutomationInteropProvider.RaiseStructureChangedEvent( el, new StructureChangedEventArgs( StructureChangeType.ChildrenReordered, el.MakeRuntimeId() ) );
            }
        }
        
        private static void HandleCanMinimizeProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            if (eventId == NativeMethods.EventObjectLocationChange)
            {
                WindowVisualState wvs = GetWindowVisualState(hwnd);
                
                bool canMinimize = wvs != WindowVisualState.Minimized;
                RaisePropertyChangedEvent(el, WindowPattern.CanMinimizeProperty, canMinimize);
            }
        }
        
        private static void HandleCanMaximizeProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            if (eventId == NativeMethods.EventObjectLocationChange)
            {
                WindowVisualState wvs = GetWindowVisualState(hwnd);
                
                bool canMaximize = wvs != WindowVisualState.Maximized;
                RaisePropertyChangedEvent(el, WindowPattern.CanMaximizeProperty, canMaximize);
            }
        }
        
        private static void HandleValueProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IValueProvider value = el.GetPatternProvider(ValuePattern.Pattern) as IValueProvider;
            if (value == null)
                return;
            
            RaisePropertyChangedEvent(el, ValuePattern.ValueProperty, value.Value);
        }
        
        private static void HandleRangeValueProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IRangeValueProvider rangeValue = el.GetPatternProvider(RangeValuePattern.Pattern) as IRangeValueProvider;
            if (rangeValue == null)
                return;

            RaisePropertyChangedEvent(el, RangeValuePattern.ValueProperty, rangeValue.Value);
        }

        private static void HandleIsSelectedProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ISelectionItemProvider selectionItem = el.GetPatternProvider(SelectionItemPattern.Pattern) as ISelectionItemProvider;
            if (selectionItem == null)
                return;

            RaisePropertyChangedEvent(el, SelectionItemPattern.IsSelectedProperty, selectionItem.IsSelected);
        }

        private static void HandleExpandCollapseStateProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IExpandCollapseProvider expandCollapse = el.GetPatternProvider(ExpandCollapsePattern.Pattern) as IExpandCollapseProvider;
            if (expandCollapse == null)
                return;
            
            RaisePropertyChangedEvent(el, ExpandCollapsePattern.ExpandCollapseStateProperty, expandCollapse.ExpandCollapseState);
        }

        private static void HandleColumnCountProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IGridProvider grid = el.GetPatternProvider(GridPattern.Pattern) as IGridProvider;
            if (grid == null)
                return;

            RaisePropertyChangedEvent(el, GridPattern.ColumnCountProperty, grid.ColumnCount);
        }

        private static void HandleRowCountProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IGridProvider grid = el.GetPatternProvider(GridPattern.Pattern) as IGridProvider;
            if (grid == null)
                return;

            RaisePropertyChangedEvent(el, GridPattern.RowCountProperty,  grid.RowCount);
        }

        private static void HandleColumnProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IGridItemProvider gridItem = el.GetPatternProvider(GridItemPattern.Pattern) as IGridItemProvider;
            if (gridItem == null)
                return;

            RaisePropertyChangedEvent(el, GridItemPattern.ColumnProperty, gridItem.Column);
        }
        
        private static void HandleRowProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IGridItemProvider gridItem = el.GetPatternProvider(GridItemPattern.Pattern) as IGridItemProvider;
            if (gridItem == null)
                return;

            RaisePropertyChangedEvent(el, GridItemPattern.RowProperty, gridItem.Row);
        }

        private static void HandleColumnHeadersProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ITableProvider table = el.GetPatternProvider(TablePattern.Pattern) as ITableProvider;
            if (table == null)
                return;

            RaisePropertyChangedEvent(el, TablePattern.ColumnHeadersProperty, table.GetColumnHeaders());
        }

        private static void HandleRowHeadersProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ITableProvider table = el.GetPatternProvider(TablePattern.Pattern) as ITableProvider;
            if (table == null)
                return;

            RaisePropertyChangedEvent(el, TablePattern.RowHeadersProperty, table.GetRowHeaders());
        }

        private static void HandleIsSelectionRequiredProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ISelectionProvider selection = el.GetPatternProvider(SelectionPattern.Pattern) as ISelectionProvider;
            if (selection == null)
                return;

            RaisePropertyChangedEvent(el, SelectionPattern.IsSelectionRequiredProperty, selection.IsSelectionRequired);
        }

        private static void HandleVerticalViewSizeProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IScrollProvider scroll = el.GetPatternProvider(ScrollPattern.Pattern) as IScrollProvider;
            if (scroll == null)
                return;

            RaisePropertyChangedEvent(el, ScrollPattern.VerticalViewSizeProperty, scroll.VerticalViewSize);
        }

        private static void HandleHorizontalViewSizeProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IScrollProvider scroll = el.GetPatternProvider(ScrollPattern.Pattern) as IScrollProvider;
            if (scroll == null)
                return;

            RaisePropertyChangedEvent(el, ScrollPattern.HorizontalViewSizeProperty, scroll.HorizontalViewSize);
        }

        private static void HandleToggleStateProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IToggleProvider toggle = el.GetPatternProvider(TogglePattern.Pattern) as IToggleProvider;
            if (toggle == null)
                return;

            RaisePropertyChangedEvent(el, TogglePattern.ToggleStateProperty, toggle.ToggleState);
        }

        private static void HandleInvokedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IInvokeProvider invoke = el.GetPatternProvider(InvokePattern.Pattern) as IInvokeProvider;
            if (invoke == null)
                return;

            if (eventId == NativeMethods.EventObjectInvoke ||
                eventId == NativeMethods.EventObjectStateChange ||
                eventId == NativeMethods.EventObjectSelection && el is ListViewItem)
            {
                AutomationInteropProvider.RaiseAutomationEvent(InvokePattern.InvokedEvent, el, new AutomationEventArgs(InvokePattern.InvokedEvent));
            }
        }

        private static void HandleScrollInvokedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IInvokeProvider invoke = el.GetPatternProvider(InvokePattern.Pattern) as IInvokeProvider;
            if (invoke == null)
                return;

            if (eventId == NativeMethods.EventObjectStateChange)
            {
                AutomationInteropProvider.RaiseAutomationEvent(InvokePattern.InvokedEvent, el, new AutomationEventArgs(InvokePattern.InvokedEvent));
            }
        }

        private static void HandleWindowInvokedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IInvokeProvider invoke = el.GetPatternProvider(InvokePattern.Pattern) as IInvokeProvider;
            if (invoke == null)
                return;

            if (eventId == NativeMethods.EventSystemCaptureEnd )
            {
                AutomationInteropProvider.RaiseAutomationEvent(InvokePattern.InvokedEvent, el, new AutomationEventArgs(InvokePattern.InvokedEvent));
            }
        }

        private static void HandleMenuItemInvokedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            // Skip the check for InvokePattern because el is just a wrapper on a dead element and
            // GetPatternProvider will fail to return the pattern.  Later, if the caller tries to
            // use this element most properties and methods will throw ElementNotAvailable.
            if (eventId == NativeMethods.EventObjectInvoke)
            {
                AutomationInteropProvider.RaiseAutomationEvent(InvokePattern.InvokedEvent, el, new AutomationEventArgs(InvokePattern.InvokedEvent));
            }
        }

        private static void HandleElementSelectedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ISelectionItemProvider selProvider = el.GetPatternProvider(SelectionItemPattern.Pattern) as ISelectionItemProvider;
            if (selProvider == null)
                return;

            if (eventId == NativeMethods.EventObjectSelection ||
                eventId == NativeMethods.EventObjectStateChange)
            {
                AutomationInteropProvider.RaiseAutomationEvent(SelectionItemPattern.ElementSelectedEvent, el, new AutomationEventArgs(SelectionItemPattern.ElementSelectedEvent));
            }
        }

        private static void HandleElementAddedToSelectionEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ISelectionItemProvider selProvider = el.GetPatternProvider(SelectionItemPattern.Pattern) as ISelectionItemProvider;
            if (selProvider == null)
                return;

            if (eventId == NativeMethods.EventObjectSelectionAdd)
            {
                AutomationInteropProvider.RaiseAutomationEvent(SelectionItemPattern.ElementAddedToSelectionEvent, el, new AutomationEventArgs(SelectionItemPattern.ElementAddedToSelectionEvent));
            }
        }
        
        private static void HandleElementRemovedFromSelectionEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ISelectionItemProvider selProvider = el.GetPatternProvider(SelectionItemPattern.Pattern) as ISelectionItemProvider;
            if (selProvider == null)
                return;

            if (eventId == NativeMethods.EventObjectSelectionRemove)
            {
                AutomationInteropProvider.RaiseAutomationEvent(SelectionItemPattern.ElementRemovedFromSelectionEvent, el, new AutomationEventArgs(SelectionItemPattern.ElementRemovedFromSelectionEvent));
            }
        }

        private static void HandleStructureChangedEventClient(ProxySimple el, IntPtr hwnd, int eventId)
        {
            if (eventId == NativeMethods.EventObjectCreate)
            {
                AutomationInteropProvider.RaiseStructureChangedEvent (el, new StructureChangedEventArgs (StructureChangeType.ChildAdded, el.MakeRuntimeId()));
            }
            else if (eventId == NativeMethods.EventObjectDestroy)
            {
                AutomationInteropProvider.RaiseStructureChangedEvent( el, new StructureChangedEventArgs( StructureChangeType.ChildRemoved, el.MakeRuntimeId() ) );
            }
            else if ( eventId == NativeMethods.EventObjectReorder )
            {
                IGridProvider grid = el.GetPatternProvider(GridPattern.Pattern) as IGridProvider;
                if ( grid == null )
                    return;
                AutomationInteropProvider.RaiseStructureChangedEvent( el, new StructureChangedEventArgs( StructureChangeType.ChildrenInvalidated, el.MakeRuntimeId() ) );
            }
        }

        private static void HandleVerticalScrollPercentProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IScrollProvider scroll = el.GetPatternProvider (ScrollPattern.Pattern) as IScrollProvider;
            if (scroll == null || scroll.VerticalScrollPercent == ScrollPattern.NoScroll)
                return;
            
            RaisePropertyChangedEvent(el, ScrollPattern.VerticalScrollPercentProperty, scroll.VerticalScrollPercent);
        }

        private static void HandleHorizontalScrollPercentProperty(ProxySimple el, IntPtr hwnd, int eventId)
        {
            IScrollProvider scroll = el.GetPatternProvider (ScrollPattern.Pattern) as IScrollProvider;
            if (scroll == null || scroll.HorizontalScrollPercent == ScrollPattern.NoScroll)
                return;
            
            RaisePropertyChangedEvent(el, ScrollPattern.HorizontalScrollPercentProperty, scroll.HorizontalScrollPercent);
        }


        private static void HandleInvalidatedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            AutomationInteropProvider.RaiseAutomationEvent(SelectionPattern.InvalidatedEvent, el, new AutomationEventArgs(SelectionPattern.InvalidatedEvent));
        }
        
        private static void RaisePropertyChangedEvent(ProxySimple el, AutomationProperty property, object propertyValue)
        {
            if (propertyValue != null && propertyValue != AutomationElement.NotSupported)
            {
                AutomationInteropProvider.RaiseAutomationPropertyChangedEvent(el, new AutomationPropertyChangedEventArgs(property, null, propertyValue));
            }
        }

        private static WindowVisualState GetWindowVisualState(IntPtr hwnd)
        {
            int style = Misc.GetWindowStyle(hwnd);

            // How do you return Invisable?
            if (Misc.IsBitSet(style, NativeMethods.WS_MAXIMIZE))
            {
                return WindowVisualState.Maximized;
            }
            else if (Misc.IsBitSet(style, NativeMethods.WS_MINIMIZE))
            {
                return WindowVisualState.Minimized;
            }
            else
            {
                return WindowVisualState.Normal;
            }
        }

        private static void HandleTextSelectionChangedEvent(ProxySimple el, IntPtr hwnd, int eventId)
        {
            ITextProvider textProvider = el.GetPatternProvider(TextPattern.Pattern) as ITextProvider;
            if (textProvider == null)
                return;

            if (eventId == NativeMethods.EventObjectLocationChange)
            {
                // We do not want to raise the EventObjectLocationChange when it is caused by a scroll.  To do this
                // store the previous range and compare it to the current range.  The range will not change when scrolling.
                ITextRangeProvider[] currentRanges = textProvider.GetSelection();
                ITextRangeProvider currentRange = null;
                if (currentRanges != null && currentRanges.Length > 0)
                    currentRange = currentRanges[0];

                if (hwnd == _hwndLast && currentRange != null)
                {
                    if (_lastSelection != null && !currentRange.Compare(_lastSelection))
                    {
                        AutomationInteropProvider.RaiseAutomationEvent(TextPattern.TextSelectionChangedEvent, el, new AutomationEventArgs(TextPattern.TextSelectionChangedEvent));
                    }
                }
                else
                {
                    AutomationInteropProvider.RaiseAutomationEvent(TextPattern.TextSelectionChangedEvent, el, new AutomationEventArgs(TextPattern.TextSelectionChangedEvent));
                }

                //store the current range and window handle.
                _hwndLast = hwnd;
                _lastSelection = currentRange;
            }
            else if (eventId == NativeMethods.EventObjectTextSelectionChanged)
            {
                AutomationInteropProvider.RaiseAutomationEvent(
                    TextPattern.TextSelectionChangedEvent, el,
                    new AutomationEventArgs(TextPattern.TextSelectionChangedEvent));
            }
        }

        private static void InitObjectIdWindow()
        {
            _objectIdWindow = new Hashtable(7, .1f);
            _objectIdWindow.Add(ValuePattern.IsReadOnlyProperty,                   new RaiseEvent(HandleIsReadOnlyProperty));
            _objectIdWindow.Add(AutomationElement.StructureChangedEvent,           new RaiseEvent(HandleStructureChangedEventWindow));
            _objectIdWindow.Add(WindowPattern.CanMaximizeProperty,                 new RaiseEvent(HandleCanMaximizeProperty));
            _objectIdWindow.Add(WindowPattern.CanMinimizeProperty,                 new RaiseEvent(HandleCanMinimizeProperty));
            _objectIdWindow.Add(TogglePattern.ToggleStateProperty,                 new RaiseEvent(HandleToggleStateProperty));
            _objectIdWindow.Add(InvokePattern.InvokedEvent,                        new RaiseEvent(HandleWindowInvokedEvent));
        }

        private static void InitObjectIdClient()
        {
            _objectIdClient = new Hashtable(20, .1f);
            _objectIdClient.Add(ValuePattern.ValueProperty,                        new RaiseEvent(HandleValueProperty));
            _objectIdClient.Add(RangeValuePattern.ValueProperty,                   new RaiseEvent(HandleRangeValueProperty));
            _objectIdClient.Add(SelectionItemPattern.IsSelectedProperty,           new RaiseEvent(HandleIsSelectedProperty));
            _objectIdClient.Add(ExpandCollapsePattern.ExpandCollapseStateProperty, new RaiseEvent(HandleExpandCollapseStateProperty));
            _objectIdClient.Add(GridPattern.ColumnCountProperty,                   new RaiseEvent(HandleColumnCountProperty));
            _objectIdClient.Add(GridPattern.RowCountProperty,                      new RaiseEvent(HandleRowCountProperty));
            _objectIdClient.Add(GridItemPattern.ColumnProperty,                    new RaiseEvent(HandleColumnProperty));
            _objectIdClient.Add(GridItemPattern.RowProperty,                       new RaiseEvent(HandleRowProperty));
            _objectIdClient.Add(TablePattern.ColumnHeadersProperty,                new RaiseEvent(HandleColumnHeadersProperty));
            _objectIdClient.Add(TablePattern.RowHeadersProperty,                   new RaiseEvent(HandleRowHeadersProperty));
            _objectIdClient.Add(SelectionPattern.IsSelectionRequiredProperty,      new RaiseEvent(HandleIsSelectionRequiredProperty));
            _objectIdClient.Add(ScrollPattern.VerticalViewSizeProperty,            new RaiseEvent(HandleVerticalViewSizeProperty));
            _objectIdClient.Add(ScrollPattern.HorizontalViewSizeProperty,          new RaiseEvent(HandleHorizontalViewSizeProperty));
            _objectIdClient.Add(InvokePattern.InvokedEvent,                        new RaiseEvent(HandleInvokedEvent));
            _objectIdClient.Add(SelectionItemPattern.ElementSelectedEvent,         new RaiseEvent(HandleElementSelectedEvent));
            _objectIdClient.Add(SelectionItemPattern.ElementAddedToSelectionEvent, new RaiseEvent(HandleElementAddedToSelectionEvent));
            _objectIdClient.Add(SelectionItemPattern.ElementRemovedFromSelectionEvent, new RaiseEvent(HandleElementRemovedFromSelectionEvent));
            _objectIdClient.Add(AutomationElement.StructureChangedEvent,           new RaiseEvent(HandleStructureChangedEventClient));
            _objectIdClient.Add(SelectionPattern.InvalidatedEvent,                 new RaiseEvent(HandleInvalidatedEvent));
            _objectIdClient.Add(TogglePattern.ToggleStateProperty,                 new RaiseEvent(HandleToggleStateProperty));
            _objectIdClient.Add(TextPattern.TextSelectionChangedEvent,             new RaiseEvent(HandleTextSelectionChangedEvent));
        }
        
        private static void InitObjectIdScroll()
        {
            _objectIdScroll = new Hashtable(3, .1f);
            _objectIdScroll.Add(ScrollPattern.VerticalScrollPercentProperty,       new RaiseEvent(HandleVerticalScrollPercentProperty));
            _objectIdScroll.Add(ScrollPattern.HorizontalScrollPercentProperty,     new RaiseEvent(HandleHorizontalScrollPercentProperty));
            _objectIdScroll.Add(InvokePattern.InvokedEvent,                        new RaiseEvent(HandleScrollInvokedEvent));
        }

        private static void InitObjectIdCaret()
        {
            _objectIdCaret = new Hashtable(1, .1f);
            _objectIdCaret.Add(TextPattern.TextSelectionChangedEvent,              new RaiseEvent(HandleTextSelectionChangedEvent));
        }

        private static void InitObjectIdMenu()
        {
            _objectIdMenu = new Hashtable(1, .1f);
            _objectIdMenu.Add(InvokePattern.InvokedEvent, new RaiseEvent(HandleMenuItemInvokedEvent));
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private delegate void RaiseEvent (ProxySimple el, IntPtr hwnd, int eventId);

        private static Hashtable _objectIdWindow;
        private static Hashtable _objectIdClient;
        private static Hashtable _objectIdScroll;
        private static Hashtable _objectIdCaret;
        private static Hashtable _objectIdMenu;
        private static object _classLock = new object();   // use lock object vs typeof(class) for perf 

        // The hwndLast and objLast is to allow limited filtering of events.
        private static IntPtr _hwndLast = IntPtr.Zero;
        private static ITextRangeProvider _lastSelection = null;

        #endregion
    }
}
