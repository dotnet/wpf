// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Identifier for Automation ControlTypes


using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifier for Automation ControlType
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class ControlType : AutomationIdentifier
#else
    public class ControlType : AutomationIdentifier
#endif
    {
        //------------------------------------------------------
        //
        // Constructors
        //
        //------------------------------------------------------
        #region Constructors
        internal ControlType(int id, string programmaticName)
            : base(UiaCoreTypesApi.AutomationIdType.ControlType, id, programmaticName)
        {
        }

        /// <summary>
        /// </summary>
        internal static ControlType Register(AutomationIdentifierConstants.ControlTypes id, string programmaticName)
        {
            return (ControlType)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.ControlType, (int)id, programmaticName);
        }


        /// <summary>
        /// </summary>
        public static ControlType LookupById(int id)
        {
            return (ControlType)AutomationIdentifier.LookupById(UiaCoreTypesApi.AutomationIdType.ControlType, id);
        }

        //Registers a control type.
        internal static ControlType Register(AutomationIdentifierConstants.ControlTypes id, string programmaticName, string stId,
                                                AutomationProperty[] requiredProperties, 
                                                AutomationPattern[] neverSupportedPatterns, 
                                                AutomationPattern[][] requiredPatternsSets)
        {
            ControlType controlType = (ControlType)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.ControlType, (int)id, programmaticName);
            
            controlType._stId = stId;
            controlType._requiredPatternsSets = requiredPatternsSets;
            controlType._neverSupportedPatterns = neverSupportedPatterns;
            controlType._requiredProperties = requiredProperties;
            return controlType;
        }

        //Never supported patterns and required properties are set to an empty array
        internal static ControlType Register(AutomationIdentifierConstants.ControlTypes id, string programmaticName, string stId,
                                AutomationPattern[][] requiredPatternsSets)
        {
            return ControlType.Register(id, programmaticName, stId, Array.Empty<AutomationProperty>(), Array.Empty<AutomationPattern>(), requiredPatternsSets);
        }

        //Never supported patterns and required patterns are set to an empty array
        internal static ControlType Register(AutomationIdentifierConstants.ControlTypes id, string programmaticName, string stId,
                                AutomationProperty[] requiredProperties)
        {
            return ControlType.Register(id, programmaticName, stId, requiredProperties, Array.Empty<AutomationPattern>(), new AutomationPattern[0][]);
        }

        //Required patterns, never supported patterns and required properties are set to an empty array
        internal static ControlType Register(AutomationIdentifierConstants.ControlTypes id, string programmaticName, string stId)
        {
            return ControlType.Register(id, programmaticName, stId, Array.Empty<AutomationProperty>(), Array.Empty<AutomationPattern>(), new AutomationPattern[0][]);
        }
        #endregion
        
        //------------------------------------------------------
        //
        // Public Methods
        //
        //------------------------------------------------------
        #region Public Methods
        /// <summary>
        /// Each row of this array contains a set of AutomationPatterns in which
        /// atleast one set should be supported by the element.
        /// </summary>
        /// <returns>Array of sets of required patterns.</returns>
        public AutomationPattern[][] GetRequiredPatternSets()
        {
            int totalRows = this._requiredPatternsSets.Length;
            AutomationPattern[][] clone = new AutomationPattern[totalRows][];

            for (int i = 0; i < totalRows; i++)
            {
                clone[i] = (AutomationPattern[])this._requiredPatternsSets[i].Clone();
            }

            return clone;
        }

        /// <summary>
        /// Gets the array of never supported patterns.
        /// </summary>
        /// <returns>Array of never supported patterns.</returns>
        public AutomationPattern[] GetNeverSupportedPatterns()
        {
            return (AutomationPattern[])_neverSupportedPatterns.Clone();
        }

        /// <summary>
        /// Gets the array of required properties.
        /// </summary>
        /// <returns>Array of required properties.</returns>
        public AutomationProperty[] GetRequiredProperties()
        {
            return (AutomationProperty[])_requiredProperties.Clone();
        }
        #endregion

        //------------------------------------------------------
        //
        // Public Fields
        //
        //------------------------------------------------------
        #region Public Fields
        /// <summary>ControlType ID: Button - a clickable button</summary>
        public static readonly ControlType Button = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Button, "ControlType.Button", nameof(SR.LocalizedControlTypeButton), new AutomationPattern[][] {
                                                                                                                                            new AutomationPattern[] { InvokePatternIdentifiers.Pattern }
                                                                                                                                            });
        /// <summary>ControlType ID: Calendar - A date choosing mechanism</summary>
        public static readonly ControlType Calendar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Calendar, "ControlType.Calendar", nameof(SR.LocalizedControlTypeCalendar), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { GridPatternIdentifiers.Pattern, ValuePatternIdentifiers.Pattern, SelectionPatternIdentifiers.Pattern }
                                                                                                                                                    });
        /// <summary>ControlType ID: CheckBox - A toggleable checkbox</summary>
        public static readonly ControlType CheckBox = ControlType.Register(AutomationIdentifierConstants.ControlTypes.CheckBox, "ControlType.CheckBox", nameof(SR.LocalizedControlTypeCheckBox), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { TogglePatternIdentifiers.Pattern }
                                                                                                                                                    });
        /// <summary>ControlType ID: ComboBox - An editable dropdown box</summary>
        public static readonly ControlType ComboBox = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ComboBox, "ControlType.ComboBox", nameof(SR.LocalizedControlTypeComboBox), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { SelectionPatternIdentifiers.Pattern, ExpandCollapsePatternIdentifiers.Pattern }
                                                                                                                                                    });
        /// <summary>ControlType ID: Edit - A simple area with a user changeable value</summary>
        public static readonly ControlType Edit = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Edit, "ControlType.Edit", nameof(SR.LocalizedControlTypeEdit), new AutomationPattern[][] {
                                                                                                                                    new AutomationPattern[] { ValuePatternIdentifiers.Pattern }
                                                                                                                                    });
        /// <summary>ControlType ID: Hyperlink - a clickable hyperlink</summary>
        public static readonly ControlType Hyperlink = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Hyperlink, "ControlType.Hyperlink", nameof(SR.LocalizedControlTypeHyperlink), new AutomationPattern[][] {
                                                                                                                                                        new AutomationPattern[] { InvokePatternIdentifiers.Pattern }
                                                                                                                                                        });
        /// <summary>ControlType ID: Image - a non-interactive image</summary>
        public static readonly ControlType Image = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Image, "ControlType.Image", nameof(SR.LocalizedControlTypeImage));

        /// <summary>ControlType ID: ListItem - An Item in a ListView</summary>
        public static readonly ControlType ListItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ListItem, "ControlType.ListItem", nameof(SR.LocalizedControlTypeListItem), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { SelectionItemPatternIdentifiers.Pattern }
                                                                                                                                                    });
        /// <summary>ControlType ID: List - a Listview for making a selection</summary>
        public static readonly ControlType List = ControlType.Register(AutomationIdentifierConstants.ControlTypes.List, "ControlType.List", nameof(SR.LocalizedControlTypeListView), new AutomationPattern[][] {
                                                                                                                                           new AutomationPattern[] { SelectionPatternIdentifiers.Pattern, TablePatternIdentifiers.Pattern, GridPatternIdentifiers.Pattern, MultipleViewPatternIdentifiers.Pattern }
                                                                                                                                           });
        /// <summary>ControlType ID: Menu - A menu, usually has menuitems in it</summary>
        public static readonly ControlType Menu = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Menu, "ControlType.Menu", nameof(SR.LocalizedControlTypeMenu));
        /// <summary>ControlType ID: MenuBar - A menu-bar, usually populated with menubuttons</summary>
        public static readonly ControlType MenuBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.MenuBar, "ControlType.MenuBar", nameof(SR.LocalizedControlTypeMenuBar));
        /// <summary>ControlType ID: MenuItem - An item in a menu, usually clickable</summary>
        public static readonly ControlType MenuItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.MenuItem, "ControlType.MenuItem", nameof(SR.LocalizedControlTypeMenuItem), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { InvokePatternIdentifiers.Pattern }, 
                                                                                                                                                    new AutomationPattern[] { ExpandCollapsePatternIdentifiers.Pattern }, 
                                                                                                                                                    new AutomationPattern[] { TogglePatternIdentifiers.Pattern },
                                                                                                                                                    });
        /// <summary>ControlType ID: ProgressBar - Visually indicates the progress of a lengthy operation.</summary>
        public static readonly ControlType ProgressBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ProgressBar, "ControlType.ProgressBar", nameof(SR.LocalizedControlTypeProgressBar), new AutomationPattern[][] {
                                                                                                                                                                new AutomationPattern[] { ValuePatternIdentifiers.Pattern }
                                                                                                                                                                });
        /// <summary>ControlType ID: RadioButton - A selection mechanism allowing exactly 1 selected item in a group</summary>
        public static readonly ControlType RadioButton = ControlType.Register(AutomationIdentifierConstants.ControlTypes.RadioButton, "ControlType.RadioButton", nameof(SR.LocalizedControlTypeRadioButton));
        /// <summary>ControlType ID: ScrollBar - A Scrollbar, the value is usually used to control scrolling of a window</summary>
        public static readonly ControlType ScrollBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ScrollBar, "ControlType.ScrollBar", nameof(SR.LocalizedControlTypeScrollBar));
        /// <summary>ControlType ID: Slider - A Slider, usually used to set a value</summary>
        public static readonly ControlType Slider = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Slider, "ControlType.Slider", nameof(SR.LocalizedControlTypeSlider), new AutomationPattern[][] {
                                                                                                                                            new AutomationPattern[] { RangeValuePatternIdentifiers.Pattern }, 
                                                                                                                                            new AutomationPattern[] { SelectionPatternIdentifiers.Pattern }
                                                                                                                                            });
        /// <summary>ControlType ID: Spinner - A Control that allows you to either enter a numeric value or scroll it up and down</summary>
        public static readonly ControlType Spinner = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Spinner, "ControlType.Spinner", nameof(SR.LocalizedControlTypeSpinner), new AutomationPattern[][] {
                                                                                                                                                new AutomationPattern[] { RangeValuePatternIdentifiers.Pattern }, 
                                                                                                                                                new AutomationPattern[] { SelectionPatternIdentifiers.Pattern }
                                                                                                                                                });
        /// <summary>ControlType ID: StatusBar - A bar used to report status of an application</summary>
        public static readonly ControlType StatusBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.StatusBar, "ControlType.StatusBar", nameof(SR.LocalizedControlTypeStatusBar));
        /// <summary>ControlType ID: Tab - A Tabbed window</summary>
        public static readonly ControlType Tab = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Tab, "ControlType.Tab", nameof(SR.LocalizedControlTypeTab));
        /// <summary>ControlType ID: TabItem - An individual tab of a tabbed window</summary>
        public static readonly ControlType TabItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.TabItem, "ControlType.TabItem", nameof(SR.LocalizedControlTypeTabItem));
        /// <summary>ControlType ID: Image - a non-interactive text</summary>
        public static readonly ControlType Text = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Text, "ControlType.Text", nameof(SR.LocalizedControlTypeText));
        /// <summary>ControlType ID: ToolBar - A Bar, usually full of buttons or other controls</summary>
        public static readonly ControlType ToolBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ToolBar, "ControlType.ToolBar", nameof(SR.LocalizedControlTypeToolBar));
        /// <summary>ControlType ID: ToolTip - a small window that pops up containing helpful tips for using a control</summary>
        public static readonly ControlType ToolTip = ControlType.Register(AutomationIdentifierConstants.ControlTypes.ToolTip, "ControlType.ToolTip", nameof(SR.LocalizedControlTypeToolTip));

        /// <summary>ControlType ID: Tree - A display showing a hierarchy of specific items</summary>
        public static readonly ControlType Tree = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Tree, "ControlType.Tree", nameof(SR.LocalizedControlTypeTreeView));
        /// <summary>ControlType ID: TreeItem - An individual item in a tree, usually able to be expanded to show its children</summary>
        public static readonly ControlType TreeItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.TreeItem, "ControlType.TreeItem", nameof(SR.LocalizedControlTypeTreeViewItem));
        
        /// <summary>ControlType ID: Custom - Generic ControlType used for anything not covered by another ControlType</summary>
        public static readonly ControlType Custom = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Custom, "ControlType.Custom", nameof(SR.LocalizedControlTypeCustom));

        /// <summary>ControlType ID: Group - Is a separation in the automation tree so that items that are grouped together have a logical division with the structure.</summary>
        public static readonly ControlType Group = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Group, "ControlType.Group", nameof(SR.LocalizedControlTypeGroup));

        /// <summary>ControlType ID: Thumb - It provides basic semantic input for dragging/resizing-like input behavior from the user.</summary>
        public static readonly ControlType Thumb = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Thumb, "ControlType.Thumb", nameof(SR.LocalizedControlTypeThumb));
        
        /// <summary>ControlType ID: DataGrid - Lets a user easily work with items that contains metadata represented in columns. </summary>
        public static readonly ControlType DataGrid = ControlType.Register(AutomationIdentifierConstants.ControlTypes.DataGrid, "ControlType.DataGrid", nameof(SR.LocalizedControlTypeDataGrid), new AutomationPattern[][] {
                                                                                                                                           new AutomationPattern[] { GridPatternIdentifiers.Pattern },
                                                                                                                                           new AutomationPattern[] { SelectionPatternIdentifiers.Pattern },
                                                                                                                                           new AutomationPattern[] { TablePatternIdentifiers.Pattern },
                                                                                                                                           });

        /// <summary>ControlType ID: DataItem - Is more complicated than the simple list item because it has a large amount of information stored within it.</summary>
        public static readonly ControlType DataItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.DataItem, "ControlType.DataItem", nameof(SR.LocalizedControlTypeDataItem), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { SelectionItemPatternIdentifiers.Pattern }
                                                                                                                                                    });

        /// <summary>ControlType ID: Document - Lets a user view/manipulate multiple pages of text.</summary>
        public static readonly ControlType Document = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Document, "ControlType.Document", nameof(SR.LocalizedControlTypeDocument), new AutomationProperty[0],
                                                                                                        new AutomationPattern[] { ValuePatternIdentifiers.Pattern },
                                                                                                        new AutomationPattern[][] {
                                                                                                                                    new AutomationPattern[] { ScrollPatternIdentifiers.Pattern } ,
                                                                                                                                    new AutomationPattern[] { TextPatternIdentifiers.Pattern } ,
                                                                                                                                    });

        /// <summary>ControlType ID: SplitButton - Enables the ability to both perform an action on the toolbar button and expand the control to a list of other possible actions that can be performed.</summary>
        public static readonly ControlType SplitButton = ControlType.Register(AutomationIdentifierConstants.ControlTypes.SplitButton, "ControlType.SplitButton", nameof(SR.LocalizedControlTypeSplitButton), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { InvokePatternIdentifiers.Pattern }, 
                                                                                                                                                    new AutomationPattern[] { ExpandCollapsePatternIdentifiers.Pattern },
                                                                                                                                                    });

        /// <summary>ControlType ID: Window - The object representing the window frame, which contains child objects such as a title bar, client, and other objects contained in a window.</summary>
        public static readonly ControlType Window = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Window, "ControlType.Window", nameof(SR.LocalizedControlTypeWindow), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { TransformPatternIdentifiers.Pattern }, 
                                                                                                                                                    new AutomationPattern[] { WindowPatternIdentifiers.Pattern },
                                                                                                                                                    });

        /// <summary>ControlType ID: Pane - Simular to Window but does not support WindowPattern.</summary>
        public static readonly ControlType Pane = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Pane, "ControlType.Pane", nameof(SR.LocalizedControlTypePane), new AutomationPattern[][] {
                                                                                                                                                    new AutomationPattern[] { TransformPatternIdentifiers.Pattern }
                                                                                                                                                    });

        /// <summary>ControlType ID: Header - Provides a visual container for the labels for rows and/or columns of information.</summary>
        public static readonly ControlType Header = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Header, "ControlType.Header", nameof(SR.LocalizedControlTypeHeader));

        /// <summary>ControlType ID: HeaderItem - Provides a visual label for a row and/or column of information.</summary>
        public static readonly ControlType HeaderItem = ControlType.Register(AutomationIdentifierConstants.ControlTypes.HeaderItem, "ControlType.HeaderItem", nameof(SR.LocalizedControlTypeHeaderItem));

        /// <summary>ControlType ID: Table - Simular to DataGrid but only contains text elements.</summary>
        public static readonly ControlType Table = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Table, "ControlType.Table", nameof(SR.LocalizedControlTypeTable), new AutomationPattern[][] {
                                                                                                                                           new AutomationPattern[] { GridPatternIdentifiers.Pattern },
                                                                                                                                           new AutomationPattern[] { SelectionPatternIdentifiers.Pattern },
                                                                                                                                           new AutomationPattern[] { TablePatternIdentifiers.Pattern },
                                                                                                                                           });

        /// <summary>ControlType ID: TitleBar - The portion of a window that identifies the window.</summary>
        public static readonly ControlType TitleBar = ControlType.Register(AutomationIdentifierConstants.ControlTypes.TitleBar, "ControlType.TitleBar", nameof(SR.LocalizedControlTypeTitleBar));

        /// <summary>ControlType ID: Separator - An object that creates visual space in controls like menus and toolbars.</summary>
        public static readonly ControlType Separator = ControlType.Register(AutomationIdentifierConstants.ControlTypes.Separator, "ControlType.Separator", nameof(SR.LocalizedControlTypeSeparator));

        #endregion

        //------------------------------------------------------
        //
        // Public Properties
        //
        //------------------------------------------------------
        #region Public Properties
        /// <summary>Returns the Localized control type</summary>
        public string LocalizedControlType
        {
            get { return SR.GetResourceString(_stId, null); }
        }
        #endregion
        
        //------------------------------------------------------
        //
        // Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
        private AutomationPattern[][] _requiredPatternsSets;
        private AutomationPattern[] _neverSupportedPatterns;
        private AutomationProperty[] _requiredProperties;
        private string _stId;
        #endregion
    }
}
