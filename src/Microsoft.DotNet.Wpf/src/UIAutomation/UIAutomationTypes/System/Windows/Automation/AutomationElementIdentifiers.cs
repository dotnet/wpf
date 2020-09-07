// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: All the Constants that used to be on AutomationElement

using System.Windows.Automation;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using MS.Internal.Automation;

#if EVENT_TRACING_PROPERTY
using Microsoft.Win32.Diagnostics;
#endif

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents an element in the UIAutomation tree.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class AutomationElementIdentifiers
#else
    public static class AutomationElementIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>
        /// Indicates that a element does not support the requested value
        /// </summary>
        public static readonly object NotSupported = UiaCoreTypesApi.UiaGetReservedNotSupportedValue();

        /// <summary>Property ID: Indicates that this element should be included in the Control view of the tree</summary>
        public static readonly AutomationProperty IsControlElementProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsControlElement, "AutomationElementIdentifiers.IsControlElementProperty");

        /// <summary>Property ID: The ControlType of this Element</summary>
        public static readonly AutomationProperty ControlTypeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ControlType, "AutomationElementIdentifiers.ControlTypeProperty");

        /// <summary>Property ID: NativeWindowHandle - Window Handle, if the underlying control is a Window</summary>
        public static readonly AutomationProperty IsContentElementProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsContentElement, "AutomationElementIdentifiers.IsContentElementProperty");

        /// <summary>Property ID: The AutomationElement that labels this element</summary>
        public static readonly AutomationProperty LabeledByProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.LabeledBy, "AutomationElementIdentifiers.LabeledByProperty");

        /// <summary>Property ID: NativeWindowHandle - Window Handle, if the underlying control is a Window</summary>
        public static readonly AutomationProperty NativeWindowHandleProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.NativeWindowHandle, "AutomationElementIdentifiers.NativeWindowHandleProperty");

        /// <summary>Property ID: AutomationId - An identifier for an element that is unique within its containing element.</summary>
        public static readonly AutomationProperty AutomationIdProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.AutomationId, "AutomationElementIdentifiers.AutomationIdProperty");

        /// <summary>Property ID: ItemType - An application-level property used to indicate what the items in a list represent.</summary> 
        public static readonly AutomationProperty ItemTypeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ItemType, "AutomationElementIdentifiers.ItemTypeProperty");

        /// <summary>Property ID: True if the control is a password protected field. </summary>
        public static readonly AutomationProperty IsPasswordProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsPassword, "AutomationElementIdentifiers.IsPasswordProperty");

        /// <summary>Property ID: Localized control type description (eg. "Button")</summary>
        public static readonly AutomationProperty LocalizedControlTypeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.LocalizedControlType, "AutomationElementIdentifiers.LocalizedControlTypeProperty");

        /// <summary>Property ID: name of this instance of control</summary>
        public static readonly AutomationProperty NameProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Name, "AutomationElementIdentifiers.NameProperty");

        /// <summary>Property ID: Hot-key equivalent for this command item. (eg. Ctrl-P for Print)</summary>
        public static readonly AutomationProperty AcceleratorKeyProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.AcceleratorKey, "AutomationElementIdentifiers.AcceleratorKeyProperty");

        /// <summary>Property ID: Keys used to move focus to this control</summary>
        public static readonly AutomationProperty AccessKeyProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.AccessKey, "AutomationElementIdentifiers.AccessKeyProperty");

        /// <summary>Property ID: HasKeyboardFocus</summary>
        public static readonly AutomationProperty HasKeyboardFocusProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.HasKeyboardFocus, "AutomationElementIdentifiers.HasKeyboardFocusProperty");

        /// <summary>Property ID: IsKeyboardFocusable</summary>
        public static readonly AutomationProperty IsKeyboardFocusableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsKeyboardFocusable, "AutomationElementIdentifiers.IsKeyboardFocusableProperty");

        /// <summary>Property ID: Enabled</summary>
        public static readonly AutomationProperty IsEnabledProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsEnabled, "AutomationElementIdentifiers.IsEnabledProperty");

        /// <summary>Property ID: BoundingRectangle - bounding rectangle</summary>
        public static readonly AutomationProperty BoundingRectangleProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.BoundingRectangle, "AutomationElementIdentifiers.BoundingRectangleProperty");

        /// <summary>Property ID: id of process that this element lives in</summary>
        public static readonly AutomationProperty ProcessIdProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ProcessId, "AutomationElementIdentifiers.ProcessIdProperty");

        /// <summary>Property ID: RuntimeId - runtime unique ID</summary>
        public static readonly AutomationProperty RuntimeIdProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RuntimeId, "AutomationElementIdentifiers.RuntimeIdProperty");

        /// <summary>Property ID: ClassName - name of underlying class - implementation dependant, but useful for test</summary>
        public static readonly AutomationProperty ClassNameProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ClassName, "AutomationElementIdentifiers.ClassNameProperty");

        /// <summary>Property ID: HelpText - brief description of what this control does</summary>
        public static readonly AutomationProperty HelpTextProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.HelpText, "AutomationElementIdentifiers.HelpTextProperty");

        /// <summary>Property ID: ClickablePoint - Set by provider, used internally for GetClickablePoint</summary>
        public static readonly AutomationProperty ClickablePointProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ClickablePoint, "AutomationElementIdentifiers.ClickablePointProperty");

        /// <summary>Property ID: Culture - Returns the culture that provides information about the control's content.</summary>
        public static readonly AutomationProperty CultureProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Culture, "AutomationElementIdentifiers.CultureProperty");

        /// <summary>Property ID: Offscreen - Determined to be not-visible to the sighted user</summary>
        public static readonly AutomationProperty IsOffscreenProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsOffscreen, "AutomationElementIdentifiers.IsOffscreenProperty");

        /// <summary>Property ID: Orientation - Identifies whether a control is positioned in a specfic direction</summary>
        public static readonly AutomationProperty OrientationProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.Orientation, "AutomationElementIdentifiers.OrientationProperty");

        /// <summary>Property ID: FrameworkId - Identifies the underlying UI framework's name for the element being accessed</summary>
        public static readonly AutomationProperty FrameworkIdProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.FrameworkId, "AutomationElementIdentifiers.FrameworkIdProperty");

        /// <summary>Property ID: IsRequiredForForm - Identifies weather an edit field is required to be filled out on a form</summary>
        public static readonly AutomationProperty IsRequiredForFormProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsRequiredForForm, "AutomationElementIdentifiers.IsRequiredForFormProperty");

        /// <summary>Property ID: ItemStatus - Identifies the status of the visual representation of a complex item</summary>
        public static readonly AutomationProperty ItemStatusProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ItemStatus, "AutomationElementIdentifiers.ItemStatusProperty");

        /// <summary>Property ID: LiveSetting - Indicates the "politeness" level that a client should use to notify the user of changes to the live region. Supported starting with Windows 8. </summary>
        public static readonly AutomationProperty LiveSettingProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.LiveSetting, "AutomationElementIdentifiers.LiveSettingProperty");

        /// <summary>Property ID: ControllerFor - Identifies the ControllerFor property, which is an array of automation elements that are manipulated by the automation element that supports this property. </summary>
        public static readonly AutomationProperty ControllerForProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ControllerFor, "AutomationElementIdentifiers.ControllerForProperty");

        /// <summary>
        /// Property ID: SizeOfSet - Describes the count of automation elements in a group or set that are considered to be siblings.
        /// Works in coordination with the PositionInSet property to describe the count of items in the set. Supported starting with Windows 10.
        /// </summary>
        public static readonly AutomationProperty SizeOfSetProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SizeOfSet, "AutomationElementIdentifiers.SizeOfSetProperty");

        /// <summary>
        /// Property ID: PositionInSet - Describes the ordinal location of an automation element within a set of elements which are considered to be siblings.
        /// Works in coordination with the SizeOfSet property to describe the ordinal location in the set. Supported starting with Windows 10.
        /// </summary>
        public static readonly AutomationProperty PositionInSetProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.PositionInSet, "AutomationElementIdentifiers.PositionInSetProperty");

        #region IsNnnnPatternAvailable properties
        /// <summary>Property that indicates whether the DockPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsDockPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsDockPatternAvailable, "AutomationElementIdentifiers.IsDockPatternAvailableProperty");
        /// <summary>Property that indicates whether the ExpandCollapsePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsExpandCollapsePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsExpandCollapsePatternAvailable, "AutomationElementIdentifiers.IsExpandCollapsePatternAvailableProperty");
        /// <summary>Property that indicates whether the GridItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsGridItemPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsGridItemPatternAvailable, "AutomationElementIdentifiers.IsGridItemPatternAvailableProperty");
        /// <summary>Property that indicates whether the GridPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsGridPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsGridPatternAvailable, "AutomationElementIdentifiers.IsGridPatternAvailableProperty");
        /// <summary>Property that indicates whether the InvokePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsInvokePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsInvokePatternAvailable, "AutomationElementIdentifiers.IsInvokePatternAvailableProperty");
        /// <summary>Property that indicates whether the MultipleViewPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsMultipleViewPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsMultipleViewPatternAvailable, "AutomationElementIdentifiers.IsMultipleViewPatternAvailableProperty");
        /// <summary>Property that indicates whether the RangeValuePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsRangeValuePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsRangeValuePatternAvailable, "AutomationElementIdentifiers.IsRangeValuePatternAvailableProperty");
        /// <summary>Property that indicates whether the SelectionItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSelectionItemPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsSelectionItemPatternAvailable, "AutomationElementIdentifiers.IsSelectionItemPatternAvailableProperty");
        /// <summary>Property that indicates whether the SelectionPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSelectionPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsSelectionPatternAvailable, "AutomationElementIdentifiers.IsSelectionPatternAvailableProperty");
        /// <summary>Property that indicates whether the ScrollPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsScrollPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsScrollPatternAvailable, "AutomationElementIdentifiers.IsScrollPatternAvailableProperty");
        /// <summary>Property that indicates whether the ScrollItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsScrollItemPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsScrollItemPatternAvailable, "AutomationElementIdentifiers.IsScrollItemPatternAvailableProperty");
        /// <summary>Property that indicates whether the SynchronizeInputPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSynchronizedInputPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsSynchronizedInputPatternAvailable, "AutomationElementIdentifiers.IsSynchronizedInputPatternAvailableProperty");
        /// <summary>Property that indicates whether the VirtualizedItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsVirtualizedItemPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsVirtualizedItemPatternAvailable, "AutomationElementIdentifiers.IsVirtualizedItemPatternAvailableProperty");
        /// <summary>Property that indicates whether the ItemContainerPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsItemContainerPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsItemContainerPatternAvailable, "AutomationElementIdentifiers.IsItemContainerPatternAvailableProperty");
        /// <summary>Property that indicates whether the TablePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTablePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsTablePatternAvailable, "AutomationElementIdentifiers.IsTablePatternAvailableProperty");
        /// <summary>Property that indicates whether the TableItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTableItemPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsTableItemPatternAvailable, "AutomationElementIdentifiers.IsTableItemPatternAvailableProperty");
        /// <summary>Property that indicates whether the TextPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTextPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsTextPatternAvailable, "AutomationElementIdentifiers.IsTextPatternAvailableProperty");
        /// <summary>Property that indicates whether the TogglePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTogglePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsTogglePatternAvailable, "AutomationElementIdentifiers.IsTogglePatternAvailableProperty");
        /// <summary>Property that indicates whether the TransformPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTransformPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsTransformPatternAvailable, "AutomationElementIdentifiers.IsTransformPatternAvailableProperty");
        /// <summary>Property that indicates whether the ValuePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsValuePatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsValuePatternAvailable, "AutomationElementIdentifiers.IsValuePatternAvailableProperty");
        /// <summary>Property that indicates whether the WindowPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsWindowPatternAvailableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.IsWindowPatternAvailable, "AutomationElementIdentifiers.IsWindowPatternAvailableProperty");
        #endregion IsNnnnPatternAvailable properties

        #region Events

        /// <summary>Event ID: ToolTipOpenedEvent - indicates a tooltip has appeared</summary>
        public static readonly AutomationEvent ToolTipOpenedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.ToolTipOpened, "AutomationElementIdentifiers.ToolTipOpenedEvent");

        /// <summary>Event ID: ToolTipClosedEvent - indicates a tooltip has closed.</summary>
        public static readonly AutomationEvent ToolTipClosedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.ToolTipClosed, "AutomationElementIdentifiers.ToolTipClosedEvent");

        /// <summary>Event ID: StructureChangedEvent - used mainly by servers to notify of structure changed events.  Clients use AddStructureChangedHandler.</summary>
        public static readonly AutomationEvent StructureChangedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.StructureChanged, "AutomationElementIdentifiers.StructureChangedEvent");

        /// <summary>Event ID: MenuOpened - Indicates an a menu has opened.</summary>
        public static readonly AutomationEvent MenuOpenedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.MenuOpened, "AutomationElementIdentifiers.MenuOpenedEvent");

        /// <summary>Event ID: AutomationPropertyChangedEvent - used mainly by servers to notify of property changes. Clients use AddPropertyChangedListener.</summary>
        public static readonly AutomationEvent AutomationPropertyChangedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.AutomationPropertyChanged, "AutomationElementIdentifiers.AutomationPropertyChangedEvent");

        /// <summary>Event ID: AutomationFocusChangedEvent - used mainly by servers to notify of focus changed events.  Clients use AddAutomationFocusChangedListener.</summary>
        public static readonly AutomationEvent AutomationFocusChangedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.AutomationFocusChanged, "AutomationElementIdentifiers.AutomationFocusChangedEvent");

        /// <summary>Event ID: AsyncContentLoadedEvent - indicates an async content loaded event.</summary>
        public static readonly AutomationEvent AsyncContentLoadedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.AsyncContentLoaded, "AutomationElementIdentifiers.AsyncContentLoadedEvent");

        /// <summary>Event ID: MenuClosed - Indicates an a menu has closed.</summary>
        public static readonly AutomationEvent MenuClosedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.MenuClosed, "AutomationElementIdentifiers.MenuClosedEvent");

        /// <summary>Event ID: LayoutInvalidated - Indicates that many element locations/extents/offscreenedness have changed.</summary>
        public static readonly AutomationEvent LayoutInvalidatedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.LayoutInvalidated, "AutomationElementIdentifiers.LayoutInvalidatedEvent");

        /// <summary>Event ID: Raised when the content of a live region has changed. Supported starting with Windows 8.</summary>
        public static readonly AutomationEvent LiveRegionChangedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.LiveRegionChanged, "AutomationElementIdentifiers.LiveRegionChangedEvent");


        #endregion Events

        
        #endregion Public Constants and Readonly Fields
    }
}
