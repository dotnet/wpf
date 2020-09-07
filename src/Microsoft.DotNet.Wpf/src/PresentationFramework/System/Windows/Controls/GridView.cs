// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;            // DesignerSerializationVisibility
using System.Windows.Automation.Peers;
using System.Windows.Markup;
using System.Diagnostics;               // Debug

using MS.Internal;                      // Helper
using MS.Internal.KnownBoxes;


namespace System.Windows.Controls
{
    /// <summary>
    /// GridView is a built-in view of the ListView control.  It is unique
    /// from other built-in views because of its table-like layout.  Data in
    /// details view is shown in a table with each row corresponding to an
    /// entity in the data collection and each column being generated from a
    /// data-bound template, populated with values from the bound data entity.
    /// </summary>

    [StyleTypedProperty(Property = "ColumnHeaderContainerStyle", StyleTargetType = typeof(System.Windows.Controls.GridViewColumnHeader))]
    [ContentProperty("Columns")]
    public class GridView : ViewBase, IAddChild
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        void IAddChild.AddChild(object column)
        {
            AddChild(column);
        }

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        protected virtual void AddChild(object column)
        {
            GridViewColumn c = column as GridViewColumn;

            if (c != null)
            {
                Columns.Add(c);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.ListView_IllegalChildrenType));
            }
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        void IAddChild.AddText(string text)
        {
            AddText(text);
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        protected virtual void AddText(string text)
        {
            AddChild(text);
        }

        /// <summary>
        ///     Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SR.Get(SRID.ToStringFormatString_GridView, this.GetType(), Columns.Count);
        }

        /// <summary>
        /// called when ListView creates its Automation peer
        /// GridView override this method to return a GridViewAutomationPeer
        /// </summary>
        /// <param name="parent">listview reference</param>
        /// <returns>GridView automation peer</returns>
        internal protected override IViewAutomationPeer GetAutomationPeer(ListView parent)
        {
            return new GridViewAutomationPeer(this, parent);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        // For all the DPs on GridView, null is treated as unset,
        // because it's impossible to distinguish null and unset.
        // Change a property between null and unset, PropertyChangedCallback will not be called.

        // ----------------------------------------------------------------------------
        //  Defines the names of the resources to be consumed by the GridView style.
        //  Used to restyle several roles of GridView without having to restyle
        //  all of the control.
        // ----------------------------------------------------------------------------

        #region StyleKeys

        /// <summary>
        /// Key used to mark the template of ScrollViewer for use by GridView
        /// </summary>
        public static ResourceKey GridViewScrollViewerStyleKey
        {
            get
            {
                return SystemResourceKey.GridViewScrollViewerStyleKey;
            }
        }

        /// <summary>
        /// Key used to mark the Style of GridView
        /// </summary>
        public static ResourceKey GridViewStyleKey
        {
            get
            {
                return SystemResourceKey.GridViewStyleKey;
            }
        }

        /// <summary>
        /// Key used to mark the Style of ItemContainer
        /// </summary>
        public static ResourceKey GridViewItemContainerStyleKey
        {
            get
            {
                return SystemResourceKey.GridViewItemContainerStyleKey;
            }
        }

        #endregion StyleKeys

        #region GridViewColumnCollection Attached DP

        /// <summary>
        /// Reads the attached property GridViewColumnCollection from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the GridViewColumnCollection attached property.</param>
        /// <returns>The property's value.</returns>
        public static GridViewColumnCollection GetColumnCollection(DependencyObject element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (GridViewColumnCollection)element.GetValue(ColumnCollectionProperty);
        }

        /// <summary>
        /// Writes the attached property GridViewColumnCollection to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the GridViewColumnCollection attached property.</param>
        /// <param name="collection">The collection to set</param>
        public static void SetColumnCollection(DependencyObject element, GridViewColumnCollection collection)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(ColumnCollectionProperty, collection);
        }

        /// <summary>
        /// This is the dependency property registered for the GridView' ColumnCollection attached property.
        /// </summary>
        public static readonly DependencyProperty ColumnCollectionProperty
            = DependencyProperty.RegisterAttached(
                    "ColumnCollection",
                    typeof(GridViewColumnCollection),
                    typeof(GridView));

        /// <summary>
        /// Whether should serialize ColumnCollection attach DP
        /// </summary>
        /// <param name="obj">Object on which this DP is set</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ShouldSerializeColumnCollection(DependencyObject obj)
        {
            ListViewItem listViewItem = obj as ListViewItem;
            if (listViewItem != null)
            {
                ListView listView = listViewItem.ParentSelector as ListView;
                if (listView != null)
                {
                    GridView gridView = listView.View as GridView;
                    if (gridView != null)
                    {
                        // if GridViewColumnCollection attached on ListViewItem is Details.Columns, it should't be serialized.
                        GridViewColumnCollection localValue = listViewItem.ReadLocalValue(ColumnCollectionProperty) as GridViewColumnCollection;
                        return (localValue != gridView.Columns);
                    }
                }
            }

            return true;
        }

        #endregion

        /// <summary> GridViewColumn List</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public GridViewColumnCollection Columns
        {
            get
            {
                if (_columns == null)
                {
                    _columns = new GridViewColumnCollection();

                    // Give the collection a back-link, this is used for the inheritance context
                    _columns.Owner = this;
                    _columns.InViewMode = true;
                }

                return _columns;
            }
        }

        #region ColumnHeaderContainerStyle

        /// <summary>
        /// ColumnHeaderContainerStyleProperty DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderContainerStyleProperty =
                DependencyProperty.Register(
                    "ColumnHeaderContainerStyle",
                    typeof(Style),
                    typeof(GridView)
                );

        /// <summary>
        /// header container's style
        /// </summary>
        public Style ColumnHeaderContainerStyle
        {
            get { return (Style)GetValue(ColumnHeaderContainerStyleProperty); }
            set { SetValue(ColumnHeaderContainerStyleProperty, value); }
        }

        #endregion // ColumnHeaderContainerStyle

        #region ColumnHeaderTemplate

        /// <summary>
        /// ColumnHeaderTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderTemplateProperty =
            DependencyProperty.Register(
                "ColumnHeaderTemplate",
                typeof(DataTemplate),
                typeof(GridView),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnColumnHeaderTemplateChanged))
            );


        /// <summary>
        /// column header template
        /// </summary>
        public DataTemplate ColumnHeaderTemplate
        {
            get { return (DataTemplate)GetValue(ColumnHeaderTemplateProperty); }
            set { SetValue(ColumnHeaderTemplateProperty, value); }
        }

        private static void OnColumnHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridView dv = (GridView)d;

            // Check to prevent Template and TemplateSelector at the same time
            Helper.CheckTemplateAndTemplateSelector("GridViewColumnHeader", ColumnHeaderTemplateProperty, ColumnHeaderTemplateSelectorProperty, dv);
        }

        #endregion  ColumnHeaderTemplate

        #region ColumnHeaderTemplateSelector

        /// <summary>
        /// ColumnHeaderTemplateSelector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderTemplateSelectorProperty =
            DependencyProperty.Register(
                "ColumnHeaderTemplateSelector",
                typeof(DataTemplateSelector),
                typeof(GridView),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnColumnHeaderTemplateSelectorChanged))
            );


        /// <summary>
        /// header template selector
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="ColumnHeaderTemplate"/> is set.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTemplateSelector ColumnHeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ColumnHeaderTemplateSelectorProperty); }
            set { SetValue(ColumnHeaderTemplateSelectorProperty, value); }
        }

        private static void OnColumnHeaderTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridView dv = (GridView)d;

            // Check to prevent Template and TemplateSelector at the same time
            Helper.CheckTemplateAndTemplateSelector("GridViewColumnHeader", ColumnHeaderTemplateProperty, ColumnHeaderTemplateSelectorProperty, dv);
        }

        #endregion ColumnHeaderTemplateSelector

        #region ColumnHeaderStringFormat

        /// <summary>
        /// ColumnHeaderStringFormat DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderStringFormatProperty =
            DependencyProperty.Register(
                "ColumnHeaderStringFormat",
                typeof(String),
                typeof(GridView)
            );


        /// <summary>
        /// column header string format
        /// </summary>
        public String ColumnHeaderStringFormat
        {
            get { return (String)GetValue(ColumnHeaderStringFormatProperty); }
            set { SetValue(ColumnHeaderStringFormatProperty, value); }
        }

        #endregion  ColumnHeaderStringFormat

        #region AllowsColumnReorder

        /// <summary>
        /// AllowsColumnReorderProperty DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AllowsColumnReorderProperty =
                DependencyProperty.Register(
                    "AllowsColumnReorder",
                    typeof(bool),
                    typeof(GridView),
                    new FrameworkPropertyMetadata(BooleanBoxes.TrueBox  /* default value */
                        )
                );

        /// <summary>
        /// AllowsColumnReorder
        /// </summary>
        public bool AllowsColumnReorder
        {
            get { return (bool)GetValue(AllowsColumnReorderProperty); }
            set { SetValue(AllowsColumnReorderProperty, value); }
        }

        #endregion AllowsColumnReorder

        #region ColumnHeaderContextMenu

        /// <summary>
        /// ColumnHeaderContextMenuProperty DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderContextMenuProperty =
                DependencyProperty.Register(
                    "ColumnHeaderContextMenu",
                    typeof(ContextMenu),
                    typeof(GridView)
                );

        /// <summary>
        /// ColumnHeaderContextMenu
        /// </summary>
        public ContextMenu ColumnHeaderContextMenu
        {
            get { return (ContextMenu)GetValue(ColumnHeaderContextMenuProperty); }
            set { SetValue(ColumnHeaderContextMenuProperty, value); }
        }

        #endregion ColumnHeaderContextMenu

        #region ColumnHeaderToolTip

        /// <summary>
        /// ColumnHeaderToolTipProperty DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderToolTipProperty =
                DependencyProperty.Register(
                    "ColumnHeaderToolTip",
                    typeof(object),
                    typeof(GridView)
                );

        /// <summary>
        /// ColumnHeaderToolTip
        /// </summary>
        public object ColumnHeaderToolTip
        {
            get { return GetValue(ColumnHeaderToolTipProperty); }
            set { SetValue(ColumnHeaderToolTipProperty, value); }
        }

        #endregion ColumnHeaderToolTip

        #endregion // Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// called when ListView is prepare container for item.
        /// GridView override this method to attache the column collection
        /// </summary>
        /// <param name="item">the container</param>
        protected internal override void PrepareItem(ListViewItem item)
        {
            base.PrepareItem(item);

            // attach GridViewColumnCollection to ListViewItem.
            SetColumnCollection(item, _columns);
        }

        /// <summary>
        /// called when ListView is clear container for item.
        /// GridView override this method to clear the column collection
        /// </summary>
        /// <param name="item">the container</param>
        protected internal override void ClearItem(ListViewItem item)
        {
            item.ClearValue(ColumnCollectionProperty);

            base.ClearItem(item);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Properties
        //
        //-------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// override with style key of GridView.
        /// </summary>
        protected internal override object DefaultStyleKey
        {
            get { return GridViewStyleKey; }
        }

        /// <summary>
        /// override with style key using GridViewRowPresenter.
        /// </summary>
        protected internal override object ItemContainerDefaultStyleKey
        {
            get { return GridViewItemContainerStyleKey; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);

            if (_columns != null)
            {
                foreach (GridViewColumn column in _columns)
                {
                    column.OnInheritanceContextChanged(args);
                }
            }
        }

        // Propagate theme changes to contained headers
        internal override void OnThemeChanged()
        {
            if (_columns != null)
            {
                for (int i=0; i<_columns.Count; i++)
                {
                    _columns[i].OnThemeChanged();
                }
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private GridViewColumnCollection _columns;

        #endregion // Private Fields

        internal GridViewHeaderRowPresenter HeaderRowPresenter
        {
            get { return _gvheaderRP; }
            set { _gvheaderRP = value; }
        }

        private GridViewHeaderRowPresenter _gvheaderRP;
    }
}
