// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: property/pattern/event information

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Automation.Provider;
using System.Collections;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics;
using MS.Internal.Automation;

namespace MS.Internal.Automation
{
    // Disable warning for obsolete types.  These are scheduled to be removed in M8.2 so 
    // only need the warning to come out for components outside of APT.
    #pragma warning disable 0618

    // Information about automation properties and patterns
    internal sealed class Schema
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // static singleton class, private ctor prevents creation
        private Schema()
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // look up information on the specified property, returns true if found, else false
        internal static bool GetPropertyInfo( AutomationProperty id, out AutomationPropertyInfo info )
        {
            foreach( AutomationPropertyInfo pi in _propertyInfoTable )
            {
                if( pi.ID == id )
                {
                    info = pi;
                    return true;
                }
            }
            info = null;
            Debug.Assert( false, "GetPropertyInfo failed " + id );
            return false;
        }


        // get default value for a property
        internal static object GetDefaultValue(AutomationProperty property)
        {
            AutomationPropertyInfo pi;

            if (!Schema.GetPropertyInfo(property, out pi))
            {
                Debug.Assert(false, "GetDefaultValue was passed an unknown property");
                return null;
            }

            return pi.DefaultValue;
        }


        // look up information on the specified pattern, returns true if found, else false
        internal static bool GetPatternInfo( AutomationPattern id, out AutomationPatternInfo info )
        {
            foreach( AutomationPatternInfo pi in _patternInfoTable )
            {
                if( pi.ID == id )
                {
                    info = pi;
                    return true;
                }
            }
            info = null;
            return false;
        }


        // look up information on the specified property, returns true if found, else false
        internal static bool GetAttributeInfo( AutomationTextAttribute id, out AutomationAttributeInfo info )
        {
            foreach( AutomationAttributeInfo ai in _attributeInfoTable )
            {
                if( ai.ID == id )
                {
                    info = ai;
                    return true;
                }
            }
            info = null;
            Debug.Assert( false, "GetAttributeInfo failed " + id );
            return false;
        }


        // Used by AutomationElement to get the basic property list
        internal static AutomationProperty [ ] GetBasicProperties()
        {
            return _basicProperties;
        }

        // Used by AutomationElement to get list of patterns to try querying providers for to see what they support
        internal static AutomationPatternInfo [ ] GetPatternInfoTable()
        {
            return _patternInfoTable;
        }
        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private static object ConvertToBool(object value)                   { return value; } // Leave as-is, we use VT_BOOL
        private static object ConvertToRowOrColumnMajor(object value)       { return (RowOrColumnMajor)value; }
        private static object ConvertToToggleState(object value)            { return (ToggleState)value; }
        private static object ConvertToWindowInteractionState(object value) { return (WindowInteractionState)value; }
        private static object ConvertToWindowVisualState(object value)      { return (WindowVisualState)value; }
        private static object ConvertToExpandCollapseState(object value)    { return (ExpandCollapseState)value; }
        private static object ConvertToOrientationType(object value)        { return (OrientationType)value; }
        private static object ConvertToDockPosition(object value)           { return (DockPosition)value; }

        private static object ConvertToRect(object value)
        {
            // Convert array of doubles to rect...
            double [ ] doubles = (double [ ]) value;
            double left = doubles[0];
            double top = doubles[1];
            double width = doubles[2];
            double height = doubles[3];
            return new Rect(left, top, width, height);
        }

        private static object ConvertToPoint(object value)
        {
            // Convert array of doubles to point...
            double[] doubles = (double[])value;
            return new Point(doubles[0], doubles[1]);
        }

        private static object ConvertToControlType(object value)
        {
            // This currently passes through as-is for proxies - but need to
            // convert to ID so that UiaCore can serialize properly.
            if (value is ControlType)
                return value;
            return ControlType.LookupById((int)value);
        }

        private static object ConvertToCultureInfo(object value)
        {
            if(value is int)
            {
                return new CultureInfo((int)value);
            }
            return null;
        }

        private static object ConvertToElement(object value)
        {
            SafeNodeHandle hnode = UiaCoreApi.UiaHUiaNodeFromVariant(value);
            return AutomationElement.Wrap(hnode);
        }

        internal static object ConvertToElementArray(object value)
        {
            // Convert each item to an AutomationElement...
            object[] objArr = (object[])value;
            AutomationElement[] els = new AutomationElement[objArr.Length];
            for (int i = 0; i < objArr.Length; i++)
            {
                if (objArr[i] == null)
                {
                    els[i] = null;
                }
                else
                {
                    SafeNodeHandle hnode = UiaCoreApi.UiaHUiaNodeFromVariant(objArr[i]);
                    els[i] = AutomationElement.Wrap(hnode);
                }
            }
            return els;
        }

        // Delegate versions of the above...
        private static AutomationPropertyConverter convertToBool = new AutomationPropertyConverter(ConvertToBool);
        private static AutomationPropertyConverter convertToRowOrColumnMajor        = new AutomationPropertyConverter(ConvertToRowOrColumnMajor);
        private static AutomationPropertyConverter convertToToggleState             = new AutomationPropertyConverter(ConvertToToggleState);
        private static AutomationPropertyConverter convertToWindowInteractionState  = new AutomationPropertyConverter(ConvertToWindowInteractionState);
        private static AutomationPropertyConverter convertToWindowVisualState       = new AutomationPropertyConverter(ConvertToWindowVisualState);
        private static AutomationPropertyConverter convertToExpandCollapseState     = new AutomationPropertyConverter(ConvertToExpandCollapseState);
        private static AutomationPropertyConverter convertToRect                    = new AutomationPropertyConverter(ConvertToRect);
        private static AutomationPropertyConverter convertToPoint                   = new AutomationPropertyConverter(ConvertToPoint);
        private static AutomationPropertyConverter convertToOrientationType         = new AutomationPropertyConverter(ConvertToOrientationType);
        private static AutomationPropertyConverter convertToDockPosition            = new AutomationPropertyConverter(ConvertToDockPosition);
        private static AutomationPropertyConverter convertToElement                 = new AutomationPropertyConverter(ConvertToElement);
        private static AutomationPropertyConverter convertToElementArray            = new AutomationPropertyConverter(ConvertToElementArray);
        private static AutomationPropertyConverter convertToControlType             = new AutomationPropertyConverter(ConvertToControlType);
        private static AutomationPropertyConverter convertToCultureInfo             = new AutomationPropertyConverter(ConvertToCultureInfo);

        #endregion Private Methods



        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private static readonly AutomationPropertyInfo[] _propertyInfoTable =
        {
            // Fundamental properties, that every element must support...
            //                          Converter                        PropertyID                                             Type                           Default value                                                                                                                                                                                                                                                                                            
            new AutomationPropertyInfo( null,                            AutomationElement.RuntimeIdProperty,                   typeof(int[]),                 null                           ),
            new AutomationPropertyInfo( convertToRect,                   AutomationElement.BoundingRectangleProperty,           typeof(Rect),                  Rect.Empty                     ),

            new AutomationPropertyInfo( null,                            AutomationElement.ProcessIdProperty,                   typeof(int),                   0                              ),
                                                                                                                                                                                                                                                              
            // General properties that can apply to any element                                                                                                                                                                                               
            //                          Converter                        PropertyID                                              Type                           Default value
            new AutomationPropertyInfo( convertToControlType,            AutomationElement.ControlTypeProperty,                  typeof(ControlType),           ControlType.Custom             ),
            new AutomationPropertyInfo( null,                            AutomationElement.LocalizedControlTypeProperty,         typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.NameProperty,                         typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.AcceleratorKeyProperty,               typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.AccessKeyProperty,                    typeof(string),                ""                             ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.HasKeyboardFocusProperty,             typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsKeyboardFocusableProperty,          typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsEnabledProperty,                    typeof(bool),                  false                          ),
            new AutomationPropertyInfo( null,                            AutomationElement.AutomationIdProperty,                 typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.ClassNameProperty,                    typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.HelpTextProperty,                     typeof(string),                ""                             ),
            new AutomationPropertyInfo( convertToPoint,                  AutomationElement.ClickablePointProperty,               typeof(Point),                 null                           ),
            new AutomationPropertyInfo( convertToCultureInfo,            AutomationElement.CultureProperty,                      typeof(CultureInfo),           null                           ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsControlElementProperty,             typeof(bool),                  true                           ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsContentElementProperty,             typeof(bool),                  true                           ),
            new AutomationPropertyInfo( convertToElement,                AutomationElement.LabeledByProperty,                    typeof(AutomationElement),     null                           ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsPasswordProperty,                   typeof(bool),                  false                          ),
            new AutomationPropertyInfo( null,                            AutomationElement.NativeWindowHandleProperty,           typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            AutomationElement.ItemTypeProperty,                     typeof(string),                ""                             ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsOffscreenProperty,                  typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToOrientationType,        AutomationElement.OrientationProperty,                  typeof(OrientationType),       OrientationType.None           ),
            new AutomationPropertyInfo( null,                            AutomationElement.FrameworkIdProperty,                  typeof(string),                ""                             ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsRequiredForFormProperty,            typeof(bool),                  false                          ),
            new AutomationPropertyInfo( null,                            AutomationElement.ItemStatusProperty,                   typeof(string),                ""                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.SizeOfSetProperty,                    typeof(int),                   -1                             ),
            new AutomationPropertyInfo( null,                            AutomationElement.PositionInSetProperty,                typeof(int),                   -1                             ),

            // Pattern Available properties            
            //                                                           PropertyID                                                  Type           Default value
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsInvokePatternAvailableProperty,         typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsDockPatternAvailableProperty,           typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsExpandCollapsePatternAvailableProperty, typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsGridItemPatternAvailableProperty,       typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsGridPatternAvailableProperty,           typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsMultipleViewPatternAvailableProperty,   typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsRangeValuePatternAvailableProperty,     typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsScrollPatternAvailableProperty,         typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsVirtualizedItemPatternAvailableProperty,typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsItemContainerPatternAvailableProperty,  typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsScrollItemPatternAvailableProperty,     typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsSelectionItemPatternAvailableProperty,  typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsSelectionPatternAvailableProperty,      typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsSynchronizedInputPatternAvailableProperty,      typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsTablePatternAvailableProperty,          typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsTableItemPatternAvailableProperty,      typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsTextPatternAvailableProperty,           typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsTogglePatternAvailableProperty,         typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsTransformPatternAvailableProperty,      typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsValuePatternAvailableProperty,          typeof(bool),  false  ),
            new AutomationPropertyInfo( convertToBool,                   AutomationElement.IsWindowPatternAvailableProperty,         typeof(bool),  false  ),
                                                                       
            // Properties that are pattern-specific                    
            //                                                           PropertyID                                              Type                           Default value
            new AutomationPropertyInfo( null,                            ValuePattern.ValueProperty,                             typeof(string),                ""                             ),
            new AutomationPropertyInfo( convertToBool,                   ValuePattern.IsReadOnlyProperty,                        typeof(bool),                  true                           ),
            new AutomationPropertyInfo( null,                            RangeValuePattern.ValueProperty,                        typeof(double),                0.0                            ),
            new AutomationPropertyInfo( convertToBool,                   RangeValuePattern.IsReadOnlyProperty,                   typeof(bool),                  true                           ),
            new AutomationPropertyInfo( null,                            RangeValuePattern.MinimumProperty,                      typeof(object),                0.0                            ),
            new AutomationPropertyInfo( null,                            RangeValuePattern.MaximumProperty,                      typeof(object),                0.0                            ),
            new AutomationPropertyInfo( null,                            RangeValuePattern.LargeChangeProperty,                  typeof(double),                0.0                            ),
            new AutomationPropertyInfo( null,                            RangeValuePattern.SmallChangeProperty,                  typeof(double),                0.0                            ),
            new AutomationPropertyInfo( null,                            ScrollPattern.HorizontalScrollPercentProperty,          typeof(double),                (double)0                      ),
            new AutomationPropertyInfo( null,                            ScrollPattern.HorizontalViewSizeProperty,               typeof(double),                (double)100                    ),
            new AutomationPropertyInfo( null,                            ScrollPattern.VerticalScrollPercentProperty,            typeof(double),                (double)0                      ),
            new AutomationPropertyInfo( null,                            ScrollPattern.VerticalViewSizeProperty,                 typeof(double),                (double)100                    ),
            new AutomationPropertyInfo( convertToBool,                   ScrollPattern.HorizontallyScrollableProperty,           typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   ScrollPattern.VerticallyScrollableProperty,             typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToElementArray,           SelectionPattern.SelectionProperty,                     typeof(AutomationElement[]),   new AutomationElement[]{}      ),
            new AutomationPropertyInfo( convertToBool,                   SelectionPattern.CanSelectMultipleProperty,             typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   SelectionPattern.IsSelectionRequiredProperty,           typeof(bool),                  false                          ),
            new AutomationPropertyInfo( null,                            GridPattern.RowCountProperty,                           typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            GridPattern.ColumnCountProperty,                        typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            GridItemPattern.RowProperty,                            typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            GridItemPattern.ColumnProperty,                         typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            GridItemPattern.RowSpanProperty,                        typeof(int),                   1                              ),
            new AutomationPropertyInfo( null,                            GridItemPattern.ColumnSpanProperty,                     typeof(int),                   1                              ),
            new AutomationPropertyInfo( convertToElement,                GridItemPattern.ContainingGridProperty,                 typeof(AutomationElement),     null                           ),
            new AutomationPropertyInfo( convertToDockPosition,           DockPattern.DockPositionProperty,                       typeof(DockPosition),          DockPosition.None              ),
            new AutomationPropertyInfo( convertToExpandCollapseState,    ExpandCollapsePattern.ExpandCollapseStateProperty,      typeof(ExpandCollapseState),   ExpandCollapseState.LeafNode   ),
            new AutomationPropertyInfo( null,                            MultipleViewPattern.CurrentViewProperty,                typeof(int),                   0                              ),
            new AutomationPropertyInfo( null,                            MultipleViewPattern.SupportedViewsProperty,             typeof(int []),                new int [0]                    ),
            new AutomationPropertyInfo( convertToBool,                   WindowPattern.CanMaximizeProperty,                      typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   WindowPattern.CanMinimizeProperty,                      typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToWindowVisualState,      WindowPattern.WindowVisualStateProperty,                typeof(WindowVisualState),     WindowVisualState.Normal       ),
            new AutomationPropertyInfo( convertToWindowInteractionState, WindowPattern.WindowInteractionStateProperty,           typeof(WindowInteractionState),WindowInteractionState.Running ),
            new AutomationPropertyInfo( convertToBool,                   WindowPattern.IsModalProperty,                          typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   WindowPattern.IsTopmostProperty,                        typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   SelectionItemPattern.IsSelectedProperty,                typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToElement,                SelectionItemPattern.SelectionContainerProperty,        typeof(AutomationElement),     null                           ),
            new AutomationPropertyInfo( convertToElementArray,           TablePattern.RowHeadersProperty,                        typeof(AutomationElement []),  new AutomationElement [0]      ),
            new AutomationPropertyInfo( convertToElementArray,           TablePattern.ColumnHeadersProperty,                     typeof(AutomationElement []),  new AutomationElement [0]      ),
            new AutomationPropertyInfo( convertToRowOrColumnMajor,       TablePattern.RowOrColumnMajorProperty,                  typeof(RowOrColumnMajor),      RowOrColumnMajor.Indeterminate ),
            new AutomationPropertyInfo( convertToElementArray,           TableItemPattern.RowHeaderItemsProperty,                typeof(AutomationElement []),  new AutomationElement [0]      ),
            new AutomationPropertyInfo( convertToElementArray,           TableItemPattern.ColumnHeaderItemsProperty,             typeof(AutomationElement []),  new AutomationElement [0]      ),
            new AutomationPropertyInfo( convertToToggleState,            TogglePattern.ToggleStateProperty,                      typeof(ToggleState),           ToggleState.Indeterminate      ),
            new AutomationPropertyInfo( convertToBool,                   TransformPattern.CanMoveProperty,                       typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   TransformPattern.CanResizeProperty,                     typeof(bool),                  false                          ),
            new AutomationPropertyInfo( convertToBool,                   TransformPattern.CanRotateProperty,                     typeof(bool),                  false                          ),
        };

        // Basic properties assumed to be always supported
        private static AutomationProperty [ ] _basicProperties = 
        {
            AutomationElement.RuntimeIdProperty,
            AutomationElement.BoundingRectangleProperty,
            AutomationElement.ProcessIdProperty,
            AutomationElement.IsControlElementProperty,
            AutomationElement.IsContentElementProperty,
            
            AutomationElement.ControlTypeProperty,
            AutomationElement.LocalizedControlTypeProperty,
            AutomationElement.NameProperty,
            AutomationElement.AcceleratorKeyProperty,
            AutomationElement.AccessKeyProperty,
            AutomationElement.HasKeyboardFocusProperty,
            AutomationElement.IsKeyboardFocusableProperty,
            AutomationElement.IsEnabledProperty,
            AutomationElement.AutomationIdProperty,
            AutomationElement.ClassNameProperty,
            AutomationElement.HelpTextProperty,
            AutomationElement.LabeledByProperty,
            AutomationElement.IsPasswordProperty,
            AutomationElement.ItemTypeProperty,
            AutomationElement.IsOffscreenProperty,
            AutomationElement.OrientationProperty,
            AutomationElement.FrameworkIdProperty,
            AutomationElement.IsRequiredForFormProperty,
            AutomationElement.ItemStatusProperty
        };

        // Note - these need to be declared before the patternInfoTable array that uses them,
        // if they're after it, they'll still be null when used to init the array.
        private static readonly AutomationProperty [ ] ValueProperties = { ValuePattern.ValueProperty,
                                                                           ValuePattern.IsReadOnlyProperty };

        private static readonly AutomationProperty [ ] RangeValueProperties = { RangeValuePattern.ValueProperty,
                                                                                RangeValuePattern.IsReadOnlyProperty,
                                                                                RangeValuePattern.MinimumProperty,
                                                                                RangeValuePattern.MaximumProperty,
                                                                                RangeValuePattern.LargeChangeProperty,
                                                                                RangeValuePattern.SmallChangeProperty,};

        private static readonly AutomationProperty [ ] ScrollProperties = { ScrollPattern.HorizontalScrollPercentProperty,
                                                                            ScrollPattern.HorizontalViewSizeProperty,
                                                                            ScrollPattern.HorizontallyScrollableProperty,
                                                                            ScrollPattern.VerticallyScrollableProperty,
                                                                            ScrollPattern.VerticalScrollPercentProperty,
                                                                            ScrollPattern.VerticalViewSizeProperty };

        private static readonly AutomationProperty [ ] SelectionProperties = { SelectionPattern.SelectionProperty,
                                                                               SelectionPattern.CanSelectMultipleProperty,
                                                                               SelectionPattern.IsSelectionRequiredProperty };

        private static readonly AutomationProperty [ ] ExpandCollapseProperties = { ExpandCollapsePattern.ExpandCollapseStateProperty};

        private static readonly AutomationProperty [ ] DockProperties = { DockPattern.DockPositionProperty };

        private static readonly AutomationProperty [ ] GridProperties = { GridPattern.RowCountProperty,
                                                                          GridPattern.ColumnCountProperty };

        private static readonly AutomationProperty [ ] GridItemProperties = { GridItemPattern.RowProperty,
                                                                              GridItemPattern.ColumnProperty,
                                                                              GridItemPattern.RowSpanProperty,
                                                                              GridItemPattern.ColumnSpanProperty,
                                                                              GridItemPattern.ContainingGridProperty };
        
        private static readonly AutomationProperty [ ] MultipleViewProperties = { MultipleViewPattern.CurrentViewProperty,
                                                                                  MultipleViewPattern.SupportedViewsProperty };
        
        private static readonly AutomationProperty [ ] WindowProperties = { WindowPattern.CanMaximizeProperty,
                                                                            WindowPattern.CanMinimizeProperty,
                                                                            WindowPattern.IsModalProperty,
                                                                            WindowPattern.WindowVisualStateProperty,
                                                                            WindowPattern.WindowInteractionStateProperty,
                                                                            WindowPattern.IsTopmostProperty };
          
        private static readonly AutomationProperty [ ] SelectionItemProperties = { SelectionItemPattern.IsSelectedProperty,
                                                                                   SelectionItemPattern.SelectionContainerProperty};

        private static readonly AutomationProperty [ ] TableProperties = { TablePattern.RowHeadersProperty,
                                                                           TablePattern.ColumnHeadersProperty,
                                                                           TablePattern.RowOrColumnMajorProperty};
        
        private static readonly AutomationProperty [ ] TableItemProperties = { TableItemPattern.RowHeaderItemsProperty,
                                                                               TableItemPattern.ColumnHeaderItemsProperty};

        private static readonly AutomationProperty [ ] ToggleProperties = { TogglePattern.ToggleStateProperty};


        private static readonly AutomationProperty [ ] TransformProperties = { TransformPattern.CanMoveProperty,
                                                                               TransformPattern.CanResizeProperty,
                                                                               TransformPattern.CanRotateProperty};
        private static readonly AutomationPatternInfo [ ] _patternInfoTable =
        {
            new AutomationPatternInfo( InvokePattern.Pattern,                null,                            new WrapObjectClientSide(InvokePattern.Wrap)         ),
            new AutomationPatternInfo( SelectionPattern.Pattern,             SelectionProperties,             new WrapObjectClientSide(SelectionPattern.Wrap)      ),
            new AutomationPatternInfo( ValuePattern.Pattern,                 ValueProperties,                 new WrapObjectClientSide(ValuePattern.Wrap)          ),
            new AutomationPatternInfo( RangeValuePattern.Pattern,            RangeValueProperties,            new WrapObjectClientSide(RangeValuePattern.Wrap)     ),
            new AutomationPatternInfo( ScrollPattern.Pattern,                ScrollProperties,                new WrapObjectClientSide(ScrollPattern.Wrap)         ),
            new AutomationPatternInfo( ExpandCollapsePattern.Pattern,        ExpandCollapseProperties,        new WrapObjectClientSide(ExpandCollapsePattern.Wrap) ),
            new AutomationPatternInfo( GridPattern.Pattern,                  GridProperties,                  new WrapObjectClientSide(GridPattern.Wrap)           ),
            new AutomationPatternInfo( GridItemPattern.Pattern,              GridItemProperties,              new WrapObjectClientSide(GridItemPattern.Wrap)       ),
            new AutomationPatternInfo( MultipleViewPattern.Pattern,          MultipleViewProperties,          new WrapObjectClientSide(MultipleViewPattern.Wrap)   ),
            new AutomationPatternInfo( WindowPattern.Pattern,                WindowProperties,                new WrapObjectClientSide(WindowPattern.Wrap)         ), 
            new AutomationPatternInfo( SelectionItemPattern.Pattern,         SelectionItemProperties,         new WrapObjectClientSide(SelectionItemPattern.Wrap)  ), 
            new AutomationPatternInfo( DockPattern.Pattern,                  null,                            new WrapObjectClientSide(DockPattern.Wrap)           ), 
            new AutomationPatternInfo( TablePattern.Pattern,                 TableProperties,                 new WrapObjectClientSide(TablePattern.Wrap)          ), 
            new AutomationPatternInfo( TableItemPattern.Pattern,             TableItemProperties,             new WrapObjectClientSide(TableItemPattern.Wrap)      ), 
            new AutomationPatternInfo( TextPattern.Pattern,                  null,                            new WrapObjectClientSide(TextPattern.Wrap)           ), 
            new AutomationPatternInfo( TogglePattern.Pattern,                ToggleProperties,                new WrapObjectClientSide(TogglePattern.Wrap)         ),
            new AutomationPatternInfo( TransformPattern.Pattern,             TransformProperties,             new WrapObjectClientSide(TransformPattern.Wrap)      ), 
            new AutomationPatternInfo( ScrollItemPattern.Pattern,            null,                            new WrapObjectClientSide(ScrollItemPattern.Wrap)     ),
            new AutomationPatternInfo( SynchronizedInputPattern.Pattern,     null,                            new WrapObjectClientSide(SynchronizedInputPattern.Wrap)     ),
            new AutomationPatternInfo( VirtualizedItemPattern.Pattern,       null,                            new WrapObjectClientSide(VirtualizedItemPattern.Wrap)),
            new AutomationPatternInfo( ItemContainerPattern.Pattern,         null,                            new WrapObjectClientSide(ItemContainerPattern.Wrap)  ),
            
        };

        private static readonly AutomationAttributeInfo[] _attributeInfoTable =
        {
            new AutomationAttributeInfo( null,                  TextPattern.AnimationStyleAttribute,             typeof(AnimationStyle)          ),
            new AutomationAttributeInfo( null,                  TextPattern.BackgroundColorAttribute,            typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.BulletStyleAttribute,                typeof(BulletStyle)             ),
            new AutomationAttributeInfo( null,                  TextPattern.CapStyleAttribute,                   typeof(CapStyle)                ),
            new AutomationAttributeInfo( convertToCultureInfo,  TextPattern.CultureAttribute,                    typeof(CultureInfo)             ),
            new AutomationAttributeInfo( null,                  TextPattern.FontNameAttribute,                   typeof(string)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.FontSizeAttribute,                   typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.FontWeightAttribute,                 typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.ForegroundColorAttribute,            typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.HorizontalTextAlignmentAttribute,    typeof(HorizontalTextAlignment) ),
            new AutomationAttributeInfo( null,                  TextPattern.IndentationFirstLineAttribute,       typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.IndentationLeadingAttribute,         typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.IndentationTrailingAttribute,        typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.IsHiddenAttribute,                   typeof(bool)                    ),
            new AutomationAttributeInfo( null,                  TextPattern.IsItalicAttribute,                   typeof(bool)                    ),
            new AutomationAttributeInfo( null,                  TextPattern.IsReadOnlyAttribute,                 typeof(bool)                    ),
            new AutomationAttributeInfo( null,                  TextPattern.IsSubscriptAttribute,                typeof(bool)                    ),
            new AutomationAttributeInfo( null,                  TextPattern.IsSuperscriptAttribute,              typeof(bool)                    ),
            new AutomationAttributeInfo( null,                  TextPattern.MarginBottomAttribute,               typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.MarginLeadingAttribute,              typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.MarginTopAttribute,                  typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.MarginTrailingAttribute,             typeof(double)                  ),
            new AutomationAttributeInfo( null,                  TextPattern.OutlineStylesAttribute,              typeof(OutlineStyles)           ),
            new AutomationAttributeInfo( null,                  TextPattern.OverlineColorAttribute,              typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.OverlineStyleAttribute,              typeof(TextDecorationLineStyle) ),
            new AutomationAttributeInfo( null,                  TextPattern.StrikethroughColorAttribute,         typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.StrikethroughStyleAttribute,         typeof(TextDecorationLineStyle) ),
            new AutomationAttributeInfo( null,                  TextPattern.TabsAttribute,                       typeof(double[])                ),
            new AutomationAttributeInfo( null,                  TextPattern.TextFlowDirectionsAttribute,         typeof(FlowDirections)          ),
            new AutomationAttributeInfo( null,                  TextPattern.UnderlineColorAttribute,             typeof(int)                     ),
            new AutomationAttributeInfo( null,                  TextPattern.UnderlineStyleAttribute,             typeof(TextDecorationLineStyle) ),
        };

        #endregion Private Fields
    }
}
