// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Main class used by Automation clients, represents a UI element

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using MS.Win32;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

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
    internal sealed class AutomationElement  //: IDisposable 
#else
    public sealed class AutomationElement
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Private ctor, used mostly by CacheHelper when reconstructing AutomationElements from
        // a CacheResponse.
        internal AutomationElement(SafeNodeHandle hnode, object[,] cachedValues, int cachedValuesIndex, UiaCoreApi.UiaCacheRequest request)
        {
            _hnode = hnode; // Can be IntPtr.Zero for a lightweight object
            _cachedValues = cachedValues; // Can be null if there are no cached properties for this node
            _cachedValuesIndex = cachedValuesIndex;
            _request = request;

            // Set RuntimeId (if available; 'as int[]' filters out AutomationElement.NotAvailable)
            _runtimeId = LookupCachedValue(AutomationElement.RuntimeIdProperty, false, true) as int[];

            // Anytime an element is packaged up it should always go through a CacheRequest and that always grabs 
            // RuntimeId so don't check here if _runtimeId is null.  If we get here and there is no RuntimeId the implication 
            // is that pre-fetching isn't working.  Possible edge case: During capturing the properties we get partial
            // properties but don't abandon the pre-fetch.  Should re-visit this scenario in a special Drt.

            // One scenario that allows for null runtimeID - doing UpdatedCache() on a node and asking only
            // for children - gives us back a placeholder node that only has valid .CachedChildren,
            // the node itself doesn't have any cached properties or a node.

            // Since null is a valid value for these, we need another value to
            // indicate that they were not requested - it's a bit obscure, but
            // 'this' works well here, since these can never have it as legal value.
            _cachedParent = this;
            _cachedFirstChild = this;
            _cachedNextSibling = this;
        }

        /// <summary>
        /// Overrides Object.Finalize
        /// </summary>
        ~AutomationElement()
        {
        }
        
        // Used by methods that return non-cached AutomationElements - examples currently include
        // AutomationElements returned as properties (SelecitonContainer, RowHeaders).
        internal static AutomationElement Wrap(SafeNodeHandle hnode)
        {
            if (hnode == null || hnode.IsInvalid)
            {
                return null;
            }

            return new AutomationElement(hnode, null, 0, null);
        }

       #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>
        /// Indicates that a element does not support the requested value
        /// </summary>
        public static readonly object NotSupported = AutomationElementIdentifiers.NotSupported;

        /// <summary>Property ID: Indicates that this element should be included in the Control view of the tree</summary>
        public static readonly AutomationProperty IsControlElementProperty = AutomationElementIdentifiers.IsControlElementProperty;

        /// <summary>Property ID: The ControlType of this Element</summary>
        public static readonly AutomationProperty ControlTypeProperty = AutomationElementIdentifiers.ControlTypeProperty;

        /// <summary>Property ID: NativeWindowHandle - Window Handle, if the underlying control is a Window</summary>
        public static readonly AutomationProperty IsContentElementProperty = AutomationElementIdentifiers.IsContentElementProperty;

        /// <summary>Property ID: The AutomationElement that labels this element</summary>
        public static readonly AutomationProperty LabeledByProperty = AutomationElementIdentifiers.LabeledByProperty;

        /// <summary>Property ID: NativeWindowHandle - Window Handle, if the underlying control is a Window</summary>
        public static readonly AutomationProperty NativeWindowHandleProperty = AutomationElementIdentifiers.NativeWindowHandleProperty;

        /// <summary>Property ID: AutomationId - An identifier for an element that is unique within its containing element.</summary>
        public static readonly AutomationProperty AutomationIdProperty = AutomationElementIdentifiers.AutomationIdProperty;

        /// <summary>Property ID: ItemType - An application-level property used to indicate what the items in a list represent.</summary> 
        public static readonly AutomationProperty ItemTypeProperty = AutomationElementIdentifiers.ItemTypeProperty;

        /// <summary>Property ID: True if the control is a password protected field. </summary>
        public static readonly AutomationProperty IsPasswordProperty = AutomationElementIdentifiers.IsPasswordProperty;

        /// <summary>Property ID: Localized control type description (eg. "Button")</summary>
        public static readonly AutomationProperty LocalizedControlTypeProperty = AutomationElementIdentifiers.LocalizedControlTypeProperty;

        /// <summary>Property ID: name of this instance of control</summary>
        public static readonly AutomationProperty NameProperty = AutomationElementIdentifiers.NameProperty;

        /// <summary>Property ID: Hot-key equivalent for this command item. (eg. Ctrl-P for Print)</summary>
        public static readonly AutomationProperty AcceleratorKeyProperty = AutomationElementIdentifiers.AcceleratorKeyProperty;

        /// <summary>Property ID: Keys used to move focus to this control</summary>
        public static readonly AutomationProperty AccessKeyProperty = AutomationElementIdentifiers.AccessKeyProperty;

        /// <summary>Property ID: HasKeyboardFocus</summary>
        public static readonly AutomationProperty HasKeyboardFocusProperty = AutomationElementIdentifiers.HasKeyboardFocusProperty;

        /// <summary>Property ID: IsKeyboardFocusable</summary>
        public static readonly AutomationProperty IsKeyboardFocusableProperty = AutomationElementIdentifiers.IsKeyboardFocusableProperty;

        /// <summary>Property ID: Enabled</summary>
        public static readonly AutomationProperty IsEnabledProperty = AutomationElementIdentifiers.IsEnabledProperty;

        /// <summary>Property ID: BoundingRectangle - bounding rectangle</summary>
        public static readonly AutomationProperty BoundingRectangleProperty = AutomationElementIdentifiers.BoundingRectangleProperty;

        /// <summary>Property ID: id of process that this element lives in</summary>
        public static readonly AutomationProperty ProcessIdProperty = AutomationElementIdentifiers.ProcessIdProperty;

        /// <summary>Property ID: RuntimeId - runtime unique ID</summary>
        public static readonly AutomationProperty RuntimeIdProperty = AutomationElementIdentifiers.RuntimeIdProperty;

        /// <summary>Property ID: ClassName - name of underlying class - implementation dependant, but useful for test</summary>
        public static readonly AutomationProperty ClassNameProperty = AutomationElementIdentifiers.ClassNameProperty;

        /// <summary>Property ID: HelpText - brief description of what this control does</summary>
        public static readonly AutomationProperty HelpTextProperty = AutomationElementIdentifiers.HelpTextProperty;

        /// <summary>Property ID: ClickablePoint - Set by provider, used internally for GetClickablePoint</summary>
        public static readonly AutomationProperty ClickablePointProperty = AutomationElementIdentifiers.ClickablePointProperty;

        /// <summary>Property ID: Culture - Returns the culture that provides information about the control's content.</summary>
        public static readonly AutomationProperty CultureProperty = AutomationElementIdentifiers.CultureProperty;

        /// <summary>Property ID: Offscreen - Determined to be not-visible to the sighted user</summary>
        public static readonly AutomationProperty IsOffscreenProperty = AutomationElementIdentifiers.IsOffscreenProperty;

        /// <summary>Property ID: Orientation - Identifies whether a control is positioned in a specfic direction</summary>
        public static readonly AutomationProperty OrientationProperty = AutomationElementIdentifiers.OrientationProperty;

        /// <summary>Property ID: FrameworkId - Identifies the underlying UI framework's name for the element being accessed</summary>
        public static readonly AutomationProperty FrameworkIdProperty = AutomationElementIdentifiers.FrameworkIdProperty;

        /// <summary>Property ID: IsRequiredForForm - Identifies weather an edit field is required to be filled out on a form</summary>
        public static readonly AutomationProperty IsRequiredForFormProperty = AutomationElementIdentifiers.IsRequiredForFormProperty;

        /// <summary>Property ID: ItemStatus - Identifies the status of the visual representation of a complex item</summary>
        public static readonly AutomationProperty ItemStatusProperty = AutomationElementIdentifiers.ItemStatusProperty;

        /// <summary>
        /// Property ID: SizeOfSet - Describes the count of automation elements in a group or set that are considered to be siblings.
        /// Works in coordination with the PositionInSet property to describe the count of items in the set.
        /// </summary>
        public static readonly AutomationProperty SizeOfSetProperty = AutomationElementIdentifiers.SizeOfSetProperty;

        /// <summary>
        /// Property ID: PositionInSet - Describes the ordinal location of an automation element within a set of elements which are considered to be siblings.
        /// Works in coordination with the SizeOfSet property to describe the ordinal location in the set.
        /// </summary>
        public static readonly AutomationProperty PositionInSetProperty = AutomationElementIdentifiers.PositionInSetProperty;

        #region IsNnnnPatternAvailable properties
        /// <summary>Property that indicates whether the DockPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsDockPatternAvailableProperty = AutomationElementIdentifiers.IsDockPatternAvailableProperty;
        /// <summary>Property that indicates whether the ExpandCollapsePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsExpandCollapsePatternAvailableProperty = AutomationElementIdentifiers.IsExpandCollapsePatternAvailableProperty;
        /// <summary>Property that indicates whether the GridItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsGridItemPatternAvailableProperty = AutomationElementIdentifiers.IsGridItemPatternAvailableProperty;
        /// <summary>Property that indicates whether the GridPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsGridPatternAvailableProperty = AutomationElementIdentifiers.IsGridPatternAvailableProperty;
        /// <summary>Property that indicates whether the InvokePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsInvokePatternAvailableProperty = AutomationElementIdentifiers.IsInvokePatternAvailableProperty;
        /// <summary>Property that indicates whether the MultipleViewPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsMultipleViewPatternAvailableProperty = AutomationElementIdentifiers.IsMultipleViewPatternAvailableProperty;
        /// <summary>Property that indicates whether the RangeValuePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsRangeValuePatternAvailableProperty = AutomationElementIdentifiers.IsRangeValuePatternAvailableProperty;
        /// <summary>Property that indicates whether the SelectionItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSelectionItemPatternAvailableProperty = AutomationElementIdentifiers.IsSelectionItemPatternAvailableProperty;
        /// <summary>Property that indicates whether the SelectionPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSelectionPatternAvailableProperty = AutomationElementIdentifiers.IsSelectionPatternAvailableProperty;
        /// <summary>Property that indicates whether the ScrollPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsScrollPatternAvailableProperty = AutomationElementIdentifiers.IsScrollPatternAvailableProperty;
        /// <summary>Property that indicates whether the SynchronizedInputPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsSynchronizedInputPatternAvailableProperty = AutomationElementIdentifiers.IsSynchronizedInputPatternAvailableProperty;
        /// <summary>Property that indicates whether the ScrollItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsScrollItemPatternAvailableProperty = AutomationElementIdentifiers.IsScrollItemPatternAvailableProperty;
        /// <summary>Property that indicates whether the VirtualizedItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsVirtualizedItemPatternAvailableProperty = AutomationElementIdentifiers.IsVirtualizedItemPatternAvailableProperty;
        /// <summary>Property that indicates whether the ItemContainerPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsItemContainerPatternAvailableProperty = AutomationElementIdentifiers.IsItemContainerPatternAvailableProperty;
        /// <summary>Property that indicates whether the TablePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTablePatternAvailableProperty = AutomationElementIdentifiers.IsTablePatternAvailableProperty;
        /// <summary>Property that indicates whether the TableItemPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTableItemPatternAvailableProperty = AutomationElementIdentifiers.IsTableItemPatternAvailableProperty;
        /// <summary>Property that indicates whether the TextPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTextPatternAvailableProperty = AutomationElementIdentifiers.IsTextPatternAvailableProperty;
        /// <summary>Property that indicates whether the TogglePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTogglePatternAvailableProperty = AutomationElementIdentifiers.IsTogglePatternAvailableProperty;
        /// <summary>Property that indicates whether the TransformPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsTransformPatternAvailableProperty = AutomationElementIdentifiers.IsTransformPatternAvailableProperty;
        /// <summary>Property that indicates whether the ValuePattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsValuePatternAvailableProperty = AutomationElementIdentifiers.IsValuePatternAvailableProperty;
        /// <summary>Property that indicates whether the WindowPattern is available for this AutomationElement</summary>
        public static readonly AutomationProperty IsWindowPatternAvailableProperty = AutomationElementIdentifiers.IsWindowPatternAvailableProperty;
        #endregion IsNnnnPatternAvailable properties

        #region Events

        /// <summary>Event ID: ToolTipOpenedEvent - indicates a tooltip has appeared</summary>
        public static readonly AutomationEvent ToolTipOpenedEvent = AutomationElementIdentifiers.ToolTipOpenedEvent;

        /// <summary>Event ID: ToolTipClosedEvent - indicates a tooltip has closed.</summary>
        public static readonly AutomationEvent ToolTipClosedEvent = AutomationElementIdentifiers.ToolTipClosedEvent;

        /// <summary>Event ID: StructureChangedEvent - used mainly by servers to notify of structure changed events.  Clients use AddStructureChangedHandler.</summary>
        public static readonly AutomationEvent StructureChangedEvent = AutomationElementIdentifiers.StructureChangedEvent;

        /// <summary>Event ID: MenuOpened - Indicates an a menu has opened.</summary>
        public static readonly AutomationEvent MenuOpenedEvent = AutomationElementIdentifiers.MenuOpenedEvent;

        /// <summary>Event ID: AutomationPropertyChangedEvent - used mainly by servers to notify of property changes. Clients use AddPropertyChangedListener.</summary>
        public static readonly AutomationEvent AutomationPropertyChangedEvent = AutomationElementIdentifiers.AutomationPropertyChangedEvent;

        /// <summary>Event ID: AutomationFocusChangedEvent - used mainly by servers to notify of focus changed events.  Clients use AddAutomationFocusChangedListener.</summary>
        public static readonly AutomationEvent AutomationFocusChangedEvent = AutomationElementIdentifiers.AutomationFocusChangedEvent;

        /// <summary>Event ID: AsyncContentLoadedEvent - indicates an async content loaded event.</summary>
        public static readonly AutomationEvent AsyncContentLoadedEvent = AutomationElementIdentifiers.AsyncContentLoadedEvent;

        /// <summary>Event ID: MenuClosed - Indicates an a menu has closed.</summary>
        public static readonly AutomationEvent MenuClosedEvent = AutomationElementIdentifiers.MenuClosedEvent;

        /// <summary>Event ID: LayoutInvalidated - Indicates that many element locations/extents/offscreenedness have changed.</summary>
        public static readonly AutomationEvent LayoutInvalidatedEvent = AutomationElementIdentifiers.LayoutInvalidatedEvent;

        #endregion Events
        
        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        #region Equality
        /// <summary>
        /// Overrides Object.Equals
        /// </summary>
        /// <param name="obj">The Object to compare with the current object</param>
        /// <returns>true if the AutomationElements refer to the same UI; otherwise, false</returns>
        /// <remarks>Note that two AutomationElements that compare as equal may contain
        /// different cached information from different points in time; the equality check only
        /// tests that the AutomationElements refer to the same underlying UI.
        /// </remarks>
        public override bool Equals(object obj)
        {
            AutomationElement el = obj as AutomationElement;
            if (obj == null || el == null)
                return false;

            return Misc.Compare(this, el);
        }

        /// <summary>
        /// Overrides Object.GetHashCode
        /// </summary>
        /// <returns>An integer that represents the hashcode for this AutomationElement</returns>
        public override int GetHashCode()
        {
            int[] id = GetRuntimeId();
            int hash = 0;

            if (id == null)
            {
                // Hash codes need to be unique if the runtime ids are null we will end up 
                // handing out duplicates so throw an exception.
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            for (int i = 0; i < id.Length; i++)
            {
                hash = (hash * 2) ^ id[i];
            }

            return hash;
        }

        /// <summary>
        /// Tests whether two AutomationElement objects are equivalent
        /// </summary>
        /// <param name="left">The AutomationElement that is to the left of the equality operator</param>
        /// <param name="right">The AutomationElement that is to the right of the equality operator</param>
        /// <returns>This operator returns true if two AutomationElements refer to the same UI; otherwise false</returns>
        /// <remarks>Note that two AutomationElements that compare as equal may contain
        /// different cached information from different points in time; the equality check only
        /// tests that the AutomationElements refer to the same underlying UI.
        /// </remarks>
        public static bool operator ==(AutomationElement left, AutomationElement right)
        {
            if ((object)left == null)
                return (object)right == null;

            if ((object)right == null)
                return (object)left == null;

            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two AutomationElement objects are not equivalent
        /// </summary>
        /// <param name="left">The AutomationElement that is to the left of the inequality operator</param>
        /// <param name="right">The AutomationElement that is to the right of the inequality operator</param>
        /// <returns>This operator returns true if two AutomationElements refer to different UI; otherwise false</returns>
        public static bool operator !=(AutomationElement left, AutomationElement right)
        {
            return !(left == right);
        }
        #endregion Equality


        /// <summary>
        /// Returns an array of ints that uniquely identifies the UI element that this object represents.
        /// Caller should treat the array as an opaque value; do not attempt to analyze it or pick it apart,
        /// the format may change in future.
        /// 
        /// These identifies are only guaranteed to be unique on a given desktop.
        /// Identifiers may be recycled over time.
        /// </summary>
        public int[] GetRuntimeId()
        {
            if (_runtimeId != null)
                return _runtimeId;

            //Not true - some AE's from properties and event args (eg. SelectionItem.SelectionContainer,
            //and FocuEventArgs's previousFocus) don't currently come through CacheReqest
            //Debug.Assert(false, "Should always have runtimeID from cache at ctor.");

            // false -> return null (instead of throwing) if not available; true->wrap
            int [] val = LookupCachedValue(AutomationElement.RuntimeIdProperty, false, true) as int[];
            if (val != null)
            {
                _runtimeId = val;
                return _runtimeId;
            }

            // Possible that we got this element from a path that doesn't have prefetch
            // enabled - fall back to getting RuntimeId the slow cross-proc way for now
            // Also possible that this was called on an empty element - eg. where someone
            // use TreeScope.Children to get just the children, but not any info for this
            // element. CheckElement() will throw an exception in that case...
            CheckElement();

            _runtimeId = UiaCoreApi.UiaGetRuntimeId(_hnode);
            return _runtimeId;
        }

        /// <summary>
        /// Get element at specified point on current desktop
        /// </summary>
        /// <param name="pt">point in screen coordinates</param>
        /// <returns>element at specified point</returns>
        public static AutomationElement FromPoint(Point pt)
        {
            return DrillForPointOrFocus(true, pt, CacheRequest.CurrentUiaCacheRequest);
        }

        /// <summary>
        /// Get element from specified HWND
        /// </summary>
        /// <param name="hwnd">Handle of window to get element for</param>
        /// <returns>element representing root node of specified window</returns>
        public static AutomationElement FromHandle(IntPtr hwnd)
        {
            Misc.ValidateArgument(hwnd != IntPtr.Zero, SRID.HwndMustBeNonNULL);

            SafeNodeHandle hnode = UiaCoreApi.UiaNodeFromHandle(hwnd);
            if (hnode.IsInvalid)
            {
                return null;
            }

            UiaCoreApi.UiaCacheRequest cacheRequest = CacheRequest.CurrentUiaCacheRequest;
            // Don't do any normalization when getting updated cache...
            UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaGetUpdatedCache(hnode, cacheRequest, UiaCoreApi.NormalizeState.None, null);
            // should we release hnode?
            return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
        }

        /// <summary>
        /// Converts a local IRawElementProvider implementation to an AutomationElement.
        /// </summary>
        /// <param name="localImpl">Local object implementing IRawElementProvider</param>
        /// <returns>A corresponding AutomationElement for the impl parameter</returns>
        /// <remarks>This would be used by a client helper library that wanted
        /// to allow its callers to access its own native element type via PAW.
        /// For example, the Windows Client Platform uses its own Element type, but
        /// uses this iternally so that it can had back an AutomationElement to clients
        /// that want to get an AutomationElement directly from an Element.
        /// </remarks>
        public static AutomationElement FromLocalProvider(IRawElementProviderSimple localImpl)
        {
            Misc.ValidateArgumentNonNull(localImpl, "localImpl");

            return AutomationElement.Wrap(UiaCoreApi.UiaNodeFromProvider(localImpl));
        }



        /// <summary>
        /// Get current value of specified property from an element.
        /// </summary>
        /// <param name="property">AutomationProperty that identifies the property</param>
        /// <returns>Returns value of specified property</returns>
        /// <remarks>
        /// If the specified property is not explicitly supported by the target UI,
        /// a default value will be returned. For example, if the target UI doesn't
        /// support the AutomationElement.NameProperty, calling GetCurrentPropertyValue
        /// for that property will return an empty string.
        /// 
        /// This API gets the current value of the property at this point in time,
        /// without checking the cache. For some types of UI, this API will incur
        /// a cross-process performance hit. To access values in this AutomationElement's
        /// cache, use GetCachedPropertyValue instead.
        /// </remarks>
        public object GetCurrentPropertyValue(AutomationProperty property)
        {
            return GetCurrentPropertyValue(property, false);
        }

        /// <summary>
        /// Get the current value of specified property from an element.
        /// </summary>
        /// <param name="property">AutomationProperty that identifies the property</param>
        /// <param name="ignoreDefaultValue">Specifies whether a default value should be
        /// ignored if the specified property is supported by the target UI</param>
        /// <returns>Returns value of specified property, or AutomationElement.NotSupported</returns>
        /// <remarks>
        /// This API gets the current value of the property at this point in time,
        /// without checking the cache. For some types of UI, this API will incur
        /// a cross-process performance hit. To access values in this AutomationElement's
        /// cache, use GetCachedPropertyValue instead.
        /// </remarks>
        public object GetCurrentPropertyValue(AutomationProperty property, bool ignoreDefaultValue)
        {
            Misc.ValidateArgumentNonNull(property, "property");
            CheckElement();

            AutomationPropertyInfo pi;
            if (!Schema.GetPropertyInfo(property, out pi))
            {
                return new ArgumentException(SR.Get(SRID.UnsupportedProperty));
            }

            object value;
            // PRESHARP will flag this as warning 56506/6506:Parameter 'property' to this public method must be validated: A null-dereference can occur here.
            // False positive, property is checked, see above
#pragma warning suppress 6506
             UiaCoreApi.UiaGetPropertyValue(_hnode, property.Id, out value);
            if (value != AutomationElement.NotSupported)
            {
                // we need to verify that we've got the expected basic variant type before casting/returning?
                // We've got a variant - but that collapses all enums to ints, for example.
                // Convert back to a more appropriate managed type if necessary...
                if (value != null && pi.ObjectConverter != null)
                {
                    value = pi.ObjectConverter(value);
                }
            }
            else
            {
                if (ignoreDefaultValue)
                {
                    value = AutomationElement.NotSupported;
                }
                else
                {
                    value = Schema.GetDefaultValue(property);
                }
            }


            return value;
        }

        /// <summary>
        /// Get a pattern class from this object
        /// </summary>
        /// <param name="pattern">AutomationPattern indicating the pattern to return</param>
        /// <returns>Returns the pattern as an object, if currently supported</returns>
        /// <remarks>
        /// Throws InvalidOperationException if the pattern is not supported.
        /// 
        /// This API gets the pattern based on availability at this point in time,
        /// without checking the cache. For some types of UI, this API will incur
        /// a cross-process performance hit. To access patterns in this AutomationElement's
        /// cache, use GetCachedPattern instead.
        /// </remarks>
        public object GetCurrentPattern(AutomationPattern pattern)
        {
            object retObject;
            if (!TryGetCurrentPattern(pattern, out retObject))
            {
                throw new InvalidOperationException(SR.Get(SRID.UnsupportedPattern));
            }

            return retObject;
        }


        /// <summary>
        /// Get a pattern class from this object
        /// </summary>
        /// <param name="pattern">an object repressenting the AutomationPattern indicating
        /// the pattern to return</param>
        /// <param name="patternObject">the returned pattern object will be an object 
        /// implementing the control pattern interface if the pattern is supported else 
        /// the object will be null.</param>
        /// <returns>Returns true, if currently supported</returns>
        /// <remarks>
        /// This API gets the pattern based on availability at this point in time,
        /// without checking the cache. For some types of UI, this API will incur
        /// a cross-process performance hit. To access patterns in this AutomationElement's
        /// cache, use GetCachedPattern instead.
        /// </remarks>
        public bool TryGetCurrentPattern(AutomationPattern pattern, out object patternObject)
        {
            patternObject = null;
            Misc.ValidateArgumentNonNull(pattern, "pattern");
            CheckElement();
            // Need to catch non-critical exceptions. The WinFormsSpinner will raise an
            // InvalidOperationException if it is a domain spinner and the SelectionPattern is asked for.
            SafePatternHandle hpatternobj = null;
            try
            {
                hpatternobj = UiaCoreApi.UiaGetPatternProvider(_hnode, pattern.Id);
            }
            catch (Exception e)
            {
                if (Misc.IsCriticalException(e))
                {
                    throw;
                }
                return false;
            }
            if (hpatternobj.IsInvalid)
            {
                return false;
            }

            patternObject = Misc.WrapInterfaceOnClientSide(this, hpatternobj, pattern);
            return patternObject != null;
        }


        /// <summary>
        /// Get cached value of specified property from an element.
        /// </summary>
        /// <param name="property">AutomationProperty that identifies the property</param>
        /// <returns>Returns value of specified property</returns>
        /// <remarks>
        /// Throws InvalidOperationException if the requested property was not
        /// previously specified to be pre-fetched using a CacheRequest.
        /// 
        /// If the specified property is not explicitly supported by the target UI,
        /// a default value will be returned. For example, if the target UI doesn't
        /// support the AutomationElement.NameProperty, calling GetCachedPropertyValue
        /// for that property will return an empty string.
        /// </remarks>
        public object GetCachedPropertyValue(AutomationProperty property)
        {
            return GetCachedPropertyValue(property, false);
        }

        /// <summary>
        /// Get the cached value of specified property from an element.
        /// </summary>
        /// <param name="property">AutomationProperty that identifies the property</param>
        /// <param name="ignoreDefaultValue">Specifies whether a default value should be
        /// ignored if the specified property is not supported by the target UI</param>
        /// <returns>Returns value of specified property, or AutomationElement.NotSupported</returns>
        /// <remarks>
        /// Throws InvalidOperationException if the requested property was not
        /// previously specified to be pre-fetched using a CacheRequest.
        /// 
        /// If ignoreDefaultValue is true, then when the specified property is not
        /// explicitly supported by the target UI, a default value will not be returned.
        /// For example, if the target UI doesn't
        /// support the AutomationElement.NameProperty, calling GetCachedPropertyValue
        /// for that property will return an empty string.
        /// When ignoreDefaultValue is true, the value AutomationElement.NotSupported will
        /// be returned instead.
        /// </remarks>
        public object GetCachedPropertyValue(AutomationProperty property, bool ignoreDefaultValue)
        {
            Misc.ValidateArgumentNonNull(property, "property");

            // true -> throw if not available, true -> wrap
            object val = LookupCachedValue(property, true, true);

            UiaCoreApi.IsErrorMarker(val, true/*throwException*/);

            if (val == AutomationElement.NotSupported && !ignoreDefaultValue)
            {
                val = Schema.GetDefaultValue(property);
            }

            return val;
        }

        /// <summary>
        /// Get a pattern class from this object
        /// </summary>
        /// <param name="pattern">AutomationPattern indicating the pattern to return</param>
        /// <returns>Returns the pattern as an object, if currently supported; otherwise returns null/</returns>
        /// <remarks>
        /// Throws InvalidOperationException if the requested pattern was not
        /// previously specified to be pre-fetched using a CacheRequest.
        /// 
        /// This API gets the pattern from the cache. 
        /// </remarks>
        public object GetCachedPattern(AutomationPattern pattern)
        {
            object patternObject;
            if (!TryGetCachedPattern(pattern, out patternObject))
            {
                throw new InvalidOperationException(SR.Get(SRID.UnsupportedPattern));
            }
            return patternObject;
        }

        /// <summary>
        /// Get a pattern class from this object
        /// </summary>
        /// <param name="pattern">AutomationPattern indicating the pattern to return</param>
        /// <param name="patternObject">Is the pattern as an object, if currently in the cache; otherwise is null</param>
        /// <returns>Returns true, if currently in the cache; otherwise returns false</returns>
        /// <remarks>
        /// This API gets the pattern from the cache. 
        /// </remarks>
        public bool TryGetCachedPattern(AutomationPattern pattern, out object patternObject)
        {
            patternObject = null;

            // Lookup a cached remote reference - but even if we get
            // back null, still go ahead an create a pattern wrapper
            // to provide access to cached properties
            Misc.ValidateArgumentNonNull(pattern, "pattern");

            // false -> don't throw, false -> don't wrap
            object obj = LookupCachedValue(pattern, false, false);
            if (obj == null)
            {
                return false;
            }
            SafePatternHandle hPattern = (SafePatternHandle)obj;

            AutomationPatternInfo pi;
            if (!Schema.GetPatternInfo(pattern, out pi))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedPattern));
            }

            patternObject = pi.ClientSideWrapper(this, hPattern, true);

            return patternObject != null;
        }

        /// <summary>
        /// Get an AutomationElement with updated cached values
        /// </summary>
        /// <param name="request">CacheRequest object describing the properties and other information to fetch</param>
        /// <returns>Returns a new AutomationElement, which refers to the same UI as this element, but which is
        /// populated with properties specified in the CacheRequest.</returns>
        /// <remarks>
        /// Unlike other methods, such as FromHandle, FromPoint, this method takes
        /// an explicit CacheRequest as a parameter, and ignores the currently
        /// active CacheRequest.
        /// </remarks>
        public AutomationElement GetUpdatedCache(CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(request, "request");
            CheckElement();

            UiaCoreApi.UiaCacheRequest cacheRequest = request.GetUiaCacheRequest();

            // Don't normalize when getting updated cache...
            UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaGetUpdatedCache(_hnode, cacheRequest, UiaCoreApi.NormalizeState.None, null);
            return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
        }

        /// <summary>
        /// Find first child or descendant element that matches specified condition
        /// </summary>
        /// <param name="scope">Indicates whether to include this element, children
        /// or descendants in the search</param>
        /// <param name="condition">Condition to search for</param>
        /// <returns>Returns first element that satisfies condition,
        /// or null if no match is found.</returns>
        public AutomationElement FindFirst(TreeScope scope, Condition condition)
        {
            Misc.ValidateArgumentNonNull(condition, "condition");
            UiaCoreApi.UiaCacheResponse[] responses = Find(scope, condition, CacheRequest.CurrentUiaCacheRequest, true, null);
            if (responses.Length < 1)
            {
                return null;
            }

            Debug.Assert(responses.Length == 1);

            return CacheHelper.BuildAutomationElementsFromResponse(CacheRequest.CurrentUiaCacheRequest, responses[0]);
        }

        /// <summary>
        /// Find all child or descendant elements that match specified condition
        /// </summary>
        /// <param name="scope">Indicates whether to include this element, children
        /// or descendants in the search</param>
        /// <param name="condition">Condition to search for</param>
        /// <returns>Returns collection of all AutomationElements that
        /// match specified condition. Collection will be empty if
        /// no matches found.</returns>
        public AutomationElementCollection FindAll(TreeScope scope, Condition condition)
        {
            Misc.ValidateArgumentNonNull(condition, "condition");
            UiaCoreApi.UiaCacheRequest request = CacheRequest.CurrentUiaCacheRequest;
            UiaCoreApi.UiaCacheResponse[] responses = Find(scope, condition, request, false, null);

            AutomationElement[] els = new AutomationElement[responses.Length];

            for( int i = 0 ; i < responses.Length ; i ++ )
            {
                els[i] = CacheHelper.BuildAutomationElementsFromResponse(request, responses[i]);
            }

            return new AutomationElementCollection( els );
        }

        /// <summary>
        /// Get array of supported property identifiers
        /// </summary>
        /// <remarks>
        /// The returned array contains at least all the properties supported by this element;
        /// however it may also contain duplicate entries or properties that the element does not
        /// currently support or which have null or empty values. Use GetPropertyValue to determine
        /// whether a property is currently supported and to determine what its current value is.
        /// </remarks>
        public AutomationProperty [ ] GetSupportedProperties()
        {
            CheckElement();

            ArrayList propArrays = new ArrayList(4);
            propArrays.Add(Schema.GetBasicProperties());

            AutomationPattern[] patterns = GetSupportedPatterns();
            if (patterns != null && patterns.Length > 0)
            {
                foreach (AutomationPattern pattern in patterns)
                {
                    AutomationPatternInfo pi;
                    if (Schema.GetPatternInfo(pattern, out pi))
                    {
                        if (pi.Properties != null)
                        {
                            propArrays.Add(pi.Properties);
                        }
                    }
                }
            }

            return (AutomationProperty[])Misc.RemoveDuplicates(Misc.CombineArrays(propArrays, typeof(AutomationProperty)), typeof(AutomationProperty));
        }

        /// <summary>
        /// Get the interfaces that this object supports
        /// </summary>
        /// <returns>An array of AutomationPatterns that represent the supported interfaces</returns>
        public AutomationPattern [ ] GetSupportedPatterns()
        {
            CheckElement();

            ArrayList interfaces = new ArrayList(4);
            object patternObject;
            foreach (AutomationPatternInfo pi in Schema.GetPatternInfoTable())
            {
                if (pi.ID != null && TryGetCurrentPattern(pi.ID, out patternObject))
                {
                    interfaces.Add(pi.ID);
                }
            }

            return (AutomationPattern[])interfaces.ToArray(typeof(AutomationPattern));
        }

        /// <summary>
        /// Request to set focus to this element
        /// </summary>
        public void SetFocus()
        {
            CheckElement();

            object canReceiveFocus = GetCurrentPropertyValue(AutomationElement.IsKeyboardFocusableProperty);

            if (canReceiveFocus is bool && (bool)canReceiveFocus)
            {
                UiaCoreApi.UiaSetFocus(_hnode);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
            }
        }

        /// <summary>
        /// Get a point that can be clicked on.  If there is no ClickablePoint return false
        /// </summary>
        /// <param name="pt">A point that can be used ba a client to click on this LogicalElement</param>
        /// <returns>true if there is point that is clickable</returns>
        public bool TryGetClickablePoint( out Point pt )
        {
            // initialize point here so if we return false its initialized
            pt = new Point (0, 0);

            // Request the provider for a clickable point. 
            object ptClickable = GetCurrentPropertyValue(AutomationElement.ClickablePointProperty);

            if (ptClickable == NotSupported)
            {
                return false;
            }

            // if got one
            if (ptClickable is Point)
            {
                //If the ClickablePointProperty from the provider is NaN that means no point.
                if (double.IsNaN (((Point) ptClickable).X) || double.IsNaN (((Point) ptClickable).Y))
                {
                    return false;
                }

                // Allow the object if it is the element or a descentant...
                AutomationElement scan = AutomationElement.FromPoint((Point)ptClickable);
                while (scan != null)
                {
                    if (scan == this)
                    {
                        pt = (Point)ptClickable;
                        return true;
                    }

                    scan = TreeWalker.RawViewWalker.GetParent(scan, CacheRequest.DefaultCacheRequest);
                }
            }
            
            // the providers point is either no good or they did not have one so poke around 
            // trying to find one.
            if (ClickablePoint.HitTestForClickablePoint( (AutomationElement)this, out pt) )
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Get a point that can be clicked on.  This throws the NoClickablePointException if there is no clickable point
        /// </summary>
        /// <returns>A point that can be used by a client to click on this LogicalElement</returns>
        /// <exception cref="NoClickablePointException">If there is not clickable point for this element</exception>
        public Point GetClickablePoint()
        {
            Point pt;
            if ( !TryGetClickablePoint( out pt ) )
                throw new NoClickablePointException(SR.Get(SRID.LogicalElementNoClickablePoint));

            return pt;
        }
        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Get root element for current desktop
        /// </summary>
        /// <returns>root element for current desktop</returns>
        public static AutomationElement RootElement
        {
            get
            {
                SafeNodeHandle hnode = UiaCoreApi.UiaGetRootNode();

                UiaCoreApi.UiaCacheRequest cacheRequest = CacheRequest.CurrentUiaCacheRequest;

                // Don't normalize...
                UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaGetUpdatedCache(hnode, cacheRequest, UiaCoreApi.NormalizeState.None, null);
                // do we need to release hnode from above?

                return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
            }
        }

        /// <summary>
        /// Return the currently focused element
        /// </summary>
        public static AutomationElement FocusedElement
        {
            get
            {
                return DrillForPointOrFocus(false, new Point(0, 0), CacheRequest.CurrentUiaCacheRequest);
            }
        }

        /// <summary>
        /// This member allows access to previously requested
        /// cached properties for this element. The returned object
        /// has accessors for AutomationElement properties.
        /// </summary>
        /// <remarks>
        /// Cached property values must have been previously requested
        /// using a CacheRequest. If you try to access a cached
        /// property that was not previously requested, an InvalidOperation
        /// Exception will be thrown.
        /// 
        /// To get the value of a property at the current point in time,
        /// access the property via the Current accessor instead of
        /// Cached.
        /// </remarks>
        public AutomationElementInformation Cached
        {
            get
            {
                return new AutomationElementInformation(this, true);
            }
        }

        /// <summary>
        /// This member allows access to current property values
        /// for this element. The returned object has accessors for
        /// AutomationElement properties.
        /// </summary>
        /// <remarks>
        /// This AutomationElement must have a
        /// Full reference in order to get current values. If the
        /// AutomationElement was obtained using AutomationElementMode.None,
        /// then it contains only cached data, and attempting to get
        /// the current value of any property will throw an InvalidOperationException.
        /// 
        /// To get the cached value of a property that was previously
        /// specified using a CacheRequest, access the property via the
        /// Cached accessor instead of Current.
        /// </remarks>
        public AutomationElementInformation Current
        {
            get
            {
                return new AutomationElementInformation(this, false);
            }
        }

        /// <summary>
        /// Returns the cached parent of this AutomationElement
        /// </summary>
        /// <remarks>
        /// Returns the parent of this element, with respect to the TreeFilter
        /// condition of the CacheRequest that was active when this AutomationElement
        /// was obtained.
        /// 
        /// Throws InvalidOperationException if the parent was not previously requested
        /// in a CacheRequest.
        /// 
        /// Can return null if the specified element has no parent - eg. is the root node.
        /// </remarks>
        public AutomationElement CachedParent
        {
            get
            {
                // this is used as a marker to indicate 'not requested'
                // - used since null is a valid value for parent, but this can never be.
                // Use (object) case to ensure we just do a ref check here, not call .Equals
                if ((object)_cachedParent == (object)this)
                {
                    // PRESHARP will flag this as a warning 56503/6503: Property get methods should not throw exceptions
                    // We've spec'd as throwing an Exception, and that's what we do PreSharp shouldn't complain
#pragma warning suppress 6503
                    throw new InvalidOperationException(SR.Get(SRID.CachedPropertyNotRequested));
                }

                return _cachedParent;
            }
        }

        /// <summary>
        /// Returns the cached children of this AutomationElement
        /// </summary>
        /// <remarks>
        /// Returns a collection of children of this element, with respect to the TreeFilter
        /// condition of the CacheRequest that was active when this AutomationElement
        /// was obtained.
        /// 
        /// Throws InvalidOperationException if children or descendants were not previously requested
        /// in a CacheRequest.
        /// 
        /// Can return an empty collection if this AutomationElement has no children.
        /// </remarks>
        public AutomationElementCollection CachedChildren
        {
            get
            {
                // this is used as a marker to indicate 'not requested'
                // - used since null is a valid value for parent, but this can never be.
                // Use (object) case to ensure we just do a ref check here, not call .Equals
                if ((object)_cachedFirstChild == (object)this)
                {
                    // PRESHARP will flag this as a warning 56503/6503: Property get methods should not throw exceptions
                    // We've spec'd as throwing an Exception, and that's what we do PreSharp shouldn't complain
#pragma warning suppress 6503
                    throw new InvalidOperationException(SR.Get(SRID.CachedPropertyNotRequested));
                }

                // Build up an array to return - first count the children,
                // then build an array and populate it...
                int childCount = 0;
                AutomationElement scan = _cachedFirstChild;

                for (; scan != null; scan = scan._cachedNextSibling)
                {
                    childCount++;
                }

                AutomationElement[] children = new AutomationElement[childCount];

                scan = _cachedFirstChild;
                for (int i = 0; i < childCount; i++)
                {
                    children[i] = scan;
                    scan = scan._cachedNextSibling;
                }

                return new AutomationElementCollection(children);
            }
        }


        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal void CheckElement()
        {
            if (_hnode == null || _hnode.IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.CacheRequestNeedElementReference));
            }
        }

        // Called by the treewalker classes to navigate - we call through to the
        // provider wrapper, which gets the navigator code to do its stuff
        internal AutomationElement Navigate(NavigateDirection direction, Condition condition, CacheRequest request)
        {
            CheckElement();

            UiaCoreApi.UiaCacheRequest cacheRequest;
            if (request == null)
                cacheRequest = CacheRequest.DefaultUiaCacheRequest;
            else
                cacheRequest = request.GetUiaCacheRequest();

            UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaNavigate(_hnode, direction, condition, cacheRequest);
            return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
        }

        internal AutomationElement Normalize(Condition condition, CacheRequest request )
        {
            CheckElement();

            UiaCoreApi.UiaCacheRequest cacheRequest;
            if (request == null)
                cacheRequest = CacheRequest.DefaultUiaCacheRequest;
            else
                cacheRequest = request.GetUiaCacheRequest();

            // Normalize against the treeview condition, not the one in the cache request...
            UiaCoreApi.UiaCacheResponse response = UiaCoreApi.UiaGetUpdatedCache(_hnode, cacheRequest, UiaCoreApi.NormalizeState.Custom, condition);
            return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
        }


        // Used by the pattern wrappers to get property values
        internal object GetPatternPropertyValue(AutomationProperty property, bool useCache)
        {
            if (useCache)
                return GetCachedPropertyValue(property);
            else
                return GetCurrentPropertyValue(property);
        }


        // The following are used by CacheUtil when building up a cached AutomationElemen tree

        internal void SetCachedParent(AutomationElement cachedParent)
        {
            _cachedParent = cachedParent;
            // If we're setting the parent, it means this is one of potentially
            // many siblings - so set _cachedNextSibling to null, instead of
            // the 'not requested' marker value 'this'
            _cachedNextSibling = null;
        }

        internal void SetCachedFirstChild(AutomationElement cachedFirstChild)
        {
            _cachedFirstChild = cachedFirstChild;
        }

        internal void SetCachedNextSibling(AutomationElement cachedNextSibling)
        {
            _cachedNextSibling = cachedNextSibling;
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
 
        #region Internal Properties

        internal SafeNodeHandle RawNode
        {
            get
            {
                return _hnode;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // Lookup a cached AutomationPattern or AutomationProperty
        object LookupCachedValue(AutomationIdentifier id, bool throwIfNotRequested, bool wrap)
        {
            if (_cachedValues == null)
            {
                if (throwIfNotRequested)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CachedPropertyNotRequested));
                }
                else
                {
                    return null;
                }
            }

            AutomationProperty automationProperty = id as AutomationProperty;

            bool isProperty = automationProperty != null;
            AutomationIdentifier[] refTable = isProperty ? (AutomationIdentifier[])_request.Properties
                                                           : (AutomationIdentifier[])_request.Patterns;
            bool found = false;
            object val = null;

            int dataOffset = isProperty ? 1 : 1 + _request.Properties.Length;
            for (int i = 0; i < refTable.Length; i++)
            {
                if (refTable[i] == id)
                {
                    found = true;
                    val = _cachedValues[_cachedValuesIndex, i + dataOffset];
                    break;
                }
            }

            if (!found)
            {
                if (throwIfNotRequested)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CachedPropertyNotRequested));
                }
                else
                {
                    return null;
                }
            }

            // Bail now if no wrapping required; also, even with wrapping, null remains null
            // for both properties and patterns..
            if (!wrap || val == null)
            {
                return val;
            }

            AutomationPattern automationPattern = id as AutomationPattern;

            // Cached values are internally stored as unwrapped, direct-from-provider values, so
            // need to be wrapped as appropriate before handing back to client...
            if (automationPattern != null)
            {
                SafePatternHandle hpatternobj = (SafePatternHandle)val;
                val = Misc.WrapInterfaceOnClientSide(this, hpatternobj, automationPattern);
            }

            // No wrapping necessary here for properties - the objects in the array are fully wrapped/converted as soon as they are
            // received from the unmanaged API, so they're ready-to-use without any further processing.
            return val;
        }

        // drill for either focused raw element, or element at specified point
        private static AutomationElement DrillForPointOrFocus(bool atPoint, Point pt, UiaCoreApi.UiaCacheRequest cacheRequest)
        {
            UiaCoreApi.UiaCacheResponse response;
            if (atPoint)
                response = UiaCoreApi.UiaNodeFromPoint(pt.X, pt.Y, cacheRequest);
            else
                response = UiaCoreApi.UiaNodeFromFocus(cacheRequest);

            return CacheHelper.BuildAutomationElementsFromResponse(cacheRequest, response);
        }


        // called by FindFirst and FindAll
        private UiaCoreApi.UiaCacheResponse[] Find(TreeScope scope, Condition condition, UiaCoreApi.UiaCacheRequest request, bool findFirst, BackgroundWorker worker)
        {
            Misc.ValidateArgumentNonNull(condition, "condition");
            if (scope == 0)
            {
                throw new ArgumentException(SR.Get(SRID.TreeScopeNeedAtLeastOne));
            }
            if ((scope & ~(TreeScope.Element | TreeScope.Children | TreeScope.Descendants)) != 0)
            {
                throw new ArgumentException(SR.Get(SRID.TreeScopeElementChildrenDescendantsOnly));
            }

            // Set up a find struct...
            UiaCoreApi.UiaFindParams findParams = new UiaCoreApi.UiaFindParams();
            findParams.FindFirst = findFirst;

            if ((scope & TreeScope.Descendants) != 0)
                findParams.MaxDepth = -1;
            else if ((scope & TreeScope.Children) != 0)
                findParams.MaxDepth = 1;
            else
                findParams.MaxDepth = 0;

            if ((scope & TreeScope.Element) != 0)
                findParams.ExcludeRoot = false;
            else
                findParams.ExcludeRoot = true;

            UiaCoreApi.UiaCacheResponse[] retVal = UiaCoreApi.UiaFind(_hnode, findParams, condition, request);
            return retVal;
        }
        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafeNodeHandle _hnode;
        private int[] _runtimeId;

        // Cached object values - use in conjunction with the Properties/Pattern arrays in
        // _request to figure out which properties/patterns they are.
        // Note that these use NotSupported, so need to substitute default values
        // when returning to user.
        private object[,] _cachedValues;
        private int _cachedValuesIndex; // index of row in cachedValues that corresponds to this element

        // Reference to the cache request information that was active when this
        // element was created
        private UiaCoreApi.UiaCacheRequest _request;

        // Cached structure links - these set to 'this' to indicate that they
        // were not requested - since null is a valid value.
        private AutomationElement _cachedParent;
        private AutomationElement _cachedFirstChild;
        private AutomationElement _cachedNextSibling;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Nested Classes
        //
        //------------------------------------------------------

        #region Nested Classes

        /// <summary>
        /// This class provides access to either Cached or Current
        /// properties on an AutomationElement via the .Cached or
        /// .Current accessors.
        /// </summary>
        public struct AutomationElementInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal AutomationElementInformation(AutomationElement el, bool useCache)
            {
                _el = el;
                _useCache = useCache;
            }

            #endregion Constructors


            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------
 
            #region Public Properties

            /// <summary>The ControlType of this Element</summary>
            public ControlType  ControlType           { get { return (ControlType) _el.GetPatternPropertyValue(ControlTypeProperty,          _useCache); } }

            /// <summary>Localized control type description (eg. "Button")</summary>
            public string       LocalizedControlType  { get { return (string)      _el.GetPatternPropertyValue(LocalizedControlTypeProperty, _useCache); } }
            
            /// <summary>Name of this instance of control</summary>
            public string       Name                  { get { return (string)      _el.GetPatternPropertyValue(NameProperty,                 _useCache); } }
            
            /// <summary>Hot-key equivalent for this command item. (eg. Ctrl-P for Print)</summary>
            public string       AcceleratorKey        { get { return (string)      _el.GetPatternPropertyValue(AcceleratorKeyProperty,       _useCache); } }
            
            /// <summary>Keys used to move focus to this control</summary>
            public string       AccessKey             { get { return (string)      _el.GetPatternPropertyValue(AccessKeyProperty,            _useCache); } }
            
            /// <summary>Indicates whether this control has keyboard focus</summary>
            public bool         HasKeyboardFocus      { get { return (bool)        _el.GetPatternPropertyValue(HasKeyboardFocusProperty,     _useCache); } }
            
            /// <summary>True if this control can take keyboard focus</summary>
            public bool         IsKeyboardFocusable   { get { return (bool)        _el.GetPatternPropertyValue(IsKeyboardFocusableProperty,  _useCache); } }
            
            /// <summary>True if this control is enabled</summary>
            public bool         IsEnabled             { get { return (bool)        _el.GetPatternPropertyValue(IsEnabledProperty,            _useCache); } }
            
            /// <summary>Bounding rectangle, in screen coordinates</summary>
            public Rect         BoundingRectangle     { get { return (Rect)        _el.GetPatternPropertyValue(BoundingRectangleProperty,    _useCache); } }
            
            /// <summary>HelpText - brief description of what this control does</summary>
            public string       HelpText              { get { return (string)      _el.GetPatternPropertyValue(HelpTextProperty,             _useCache); } }
            
            /// <summary>Indicates that this element should be included in the Control view of the tree</summary>
            public bool         IsControlElement      { get { return (bool)        _el.GetPatternPropertyValue(IsControlElementProperty,     _useCache); } }

            /// <summary>Indicates that this element should be included in the Content view of the tree</summary>
            public bool         IsContentElement      { get { return (bool)        _el.GetPatternPropertyValue(IsContentElementProperty,     _useCache); } }

            /// <summary>The AutomationElement that labels this element</summary>
            public AutomationElement LabeledBy        { get { return (AutomationElement) _el.GetPatternPropertyValue(LabeledByProperty,      _useCache); } }

            /// <summary>The identifier for an element that is unique within its containing element</summary>
            public string       AutomationId          { get { return (string)      _el.GetPatternPropertyValue(AutomationIdProperty,         _useCache); } }

            /// <summary>Localized string that indicates what the items in a list represent</summary>
            public string       ItemType              { get { return (string)      _el.GetPatternPropertyValue(ItemTypeProperty,             _useCache ); } }

            /// <summary>True if the control is a password protected field.</summary>
            public bool         IsPassword            { get { return (bool)        _el.GetPatternPropertyValue(IsPasswordProperty,           _useCache); } }

            /// <summary>Name of underlying class - implementation dependant, but useful for test</summary>
            public string       ClassName             { get { return (string)      _el.GetPatternPropertyValue(ClassNameProperty,            _useCache); } }

            /// <summary>Window Handle, if the underlying control is a Window</summary>
            public int          NativeWindowHandle    { get { return (int)         _el.GetPatternPropertyValue(NativeWindowHandleProperty,   _useCache); } }

            /// <summary>Id of process that this element lives in</summary>
            public int          ProcessId             { get { return (int)         _el.GetPatternPropertyValue(ProcessIdProperty,            _useCache); } }
            
            /// <summary>True if this control is not visible to the sighted user</summary>
            public bool         IsOffscreen           { get { return (bool)        _el.GetPatternPropertyValue(IsOffscreenProperty,          _useCache); } }

            /// <summary>The controls specfied direction</summary>
            public OrientationType Orientation        { get { return (OrientationType) _el.GetPatternPropertyValue(OrientationProperty,      _useCache); } }

            /// <summary>The controls specfied direction</summary>
            public string       FrameworkId           { get { return (string)      _el.GetPatternPropertyValue(FrameworkIdProperty,          _useCache); } }

            /// <summary>True if this element is required to be filled out on a form</summary>
            public bool         IsRequiredForForm     { get { return (bool)        _el.GetPatternPropertyValue(IsRequiredForFormProperty,    _useCache); } }

            /// <summary>The visual status of a complex item as a string</summary>
            public string       ItemStatus            { get { return (string)      _el.GetPatternPropertyValue(ItemStatusProperty,           _useCache); } }

            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private AutomationElement _el; // AutomationElement that contains the cache or live reference

            private bool _useCache; // true to use cache, false to use live reference to get current values

            #endregion Private Fields
        }
        #endregion Nested Classes
    }
}
