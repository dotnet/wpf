// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Grid implementation.
//
// Specs
//      Grid : Grid.mht
//      Size Sharing: Size Information Sharing.doc
//
// Misc
//      Grid Tutorial: Grid Tutorial.mht
//

using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.PresentationFramework;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Controls
{
    /// <summary>
    /// Grid
    /// </summary>
    public class Grid : Panel, IAddChild
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static Grid()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.Grid);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Grid()
        {
            SetFlags((bool) ShowGridLinesProperty.GetDefaultValue(DependencyObjectType), Flags.ShowGridLinesPropertyValue);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// <see cref="IAddChild.AddChild"/>
        /// </summary>
        void IAddChild.AddChild(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            UIElement cell = value as UIElement;
            if (cell != null)
            {
                Children.Add(cell);
                return;
            }

            throw (new ArgumentException(SR.Get(SRID.Grid_UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value"));
        }

        /// <summary>
        /// <see cref="IAddChild.AddText"/>
        /// </summary>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        /// <summary>
        /// <see cref="FrameworkElement.LogicalChildren"/>
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                // empty panel or a panel being used as the items
                // host has *no* logical children; give empty enumerator
                bool noChildren = (base.VisualChildrenCount == 0) || IsItemsHost;

                if (noChildren)
                {
                    ExtendedData extData = ExtData;

                    if (    extData == null
                        ||  (   (extData.ColumnDefinitions == null || extData.ColumnDefinitions.Count == 0)
                            &&  (extData.RowDefinitions    == null || extData.RowDefinitions.Count    == 0) )
                       )
                    {
                        //  grid is empty
                        return EmptyEnumerator.Instance;
                    }
                }

                return (new GridChildrenCollectionEnumeratorSimple(this, !noChildren));
            }
        }

        /// <summary>
        /// Helper for setting Column property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set Column property on.</param>
        /// <param name="value">Column property value.</param>
        public static void SetColumn(UIElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// Helper for reading Column property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read Column property from.</param>
        /// <returns>Column property value.</returns>
        [AttachedPropertyBrowsableForChildren()]
        public static int GetColumn(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((int)element.GetValue(ColumnProperty));
        }

        /// <summary>
        /// Helper for setting Row property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set Row property on.</param>
        /// <param name="value">Row property value.</param>
        public static void SetRow(UIElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(RowProperty, value);
        }

        /// <summary>
        /// Helper for reading Row property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read Row property from.</param>
        /// <returns>Row property value.</returns>
        [AttachedPropertyBrowsableForChildren()]
        public static int GetRow(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((int)element.GetValue(RowProperty));
        }

        /// <summary>
        /// Helper for setting ColumnSpan property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set ColumnSpan property on.</param>
        /// <param name="value">ColumnSpan property value.</param>
        public static void SetColumnSpan(UIElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ColumnSpanProperty, value);
        }

        /// <summary>
        /// Helper for reading ColumnSpan property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read ColumnSpan property from.</param>
        /// <returns>ColumnSpan property value.</returns>
        [AttachedPropertyBrowsableForChildren()]
        public static int GetColumnSpan(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((int)element.GetValue(ColumnSpanProperty));
        }

        /// <summary>
        /// Helper for setting RowSpan property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set RowSpan property on.</param>
        /// <param name="value">RowSpan property value.</param>
        public static void SetRowSpan(UIElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(RowSpanProperty, value);
        }

        /// <summary>
        /// Helper for reading RowSpan property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read RowSpan property from.</param>
        /// <returns>RowSpan property value.</returns>
        [AttachedPropertyBrowsableForChildren()]
        public static int GetRowSpan(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((int)element.GetValue(RowSpanProperty));
        }

        /// <summary>
        /// Helper for setting IsSharedSizeScope property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set IsSharedSizeScope property on.</param>
        /// <param name="value">IsSharedSizeScope property value.</param>
        public static void SetIsSharedSizeScope(UIElement element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsSharedSizeScopeProperty, value);
        }

        /// <summary>
        /// Helper for reading IsSharedSizeScope property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read IsSharedSizeScope property from.</param>
        /// <returns>IsSharedSizeScope property value.</returns>
        public static bool GetIsSharedSizeScope(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((bool)element.GetValue(IsSharedSizeScopeProperty));
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// ShowGridLines property.
        /// </summary>
        public bool ShowGridLines
        {
            get { return (CheckFlagsAnd(Flags.ShowGridLinesPropertyValue)); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        /// <summary>
        /// Returns a ColumnDefinitionCollection of column definitions.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ColumnDefinitionCollection ColumnDefinitions
        {
            get
            {
                if (_data == null) { _data = new ExtendedData(); }
                if (_data.ColumnDefinitions == null) { _data.ColumnDefinitions = new ColumnDefinitionCollection(this); }

                return (_data.ColumnDefinitions);
            }
        }

        /// <summary>
        /// Returns a RowDefinitionCollection of row definitions.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public RowDefinitionCollection RowDefinitions
        {
            get
            {
                if (_data == null) { _data = new ExtendedData(); }
                if (_data.RowDefinitions == null) { _data.RowDefinitions = new RowDefinitionCollection(this); }

                return (_data.RowDefinitions);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            // because "base.Count + 1" for GridLinesRenderer
            // argument checking done at the base class
            if(index == base.VisualChildrenCount)
            {
                if (_gridLinesRenderer == null)
                {
                    throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
                return _gridLinesRenderer;
            }
            else return base.GetVisualChild(index);
        }

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int VisualChildrenCount
        {
            //since GridLinesRenderer has not been added as a child, so we do not subtract
            get { return base.VisualChildrenCount + (_gridLinesRenderer != null ? 1 : 0); }
        }


        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size gridDesiredSize;
            ExtendedData extData = ExtData;

            try
            {
                EnterCounterScope(Counters.MeasureOverride);

                ListenToNotifications = true;
                MeasureOverrideInProgress = true;

                if (extData == null)
                {
                    gridDesiredSize = new Size();
                    UIElementCollection children = InternalChildren;

                    for (int i = 0, count = children.Count; i < count; ++i)
                    {
                        UIElement child = children[i];
                        if (child != null)
                        {
                            child.Measure(constraint);
                            gridDesiredSize.Width = Math.Max(gridDesiredSize.Width, child.DesiredSize.Width);
                            gridDesiredSize.Height = Math.Max(gridDesiredSize.Height, child.DesiredSize.Height);
                        }
                    }
                }
                else
                {
                    {
                        bool sizeToContentU = double.IsPositiveInfinity(constraint.Width);
                        bool sizeToContentV = double.IsPositiveInfinity(constraint.Height);

                        // Clear index information and rounding errors
                        if (RowDefinitionCollectionDirty || ColumnDefinitionCollectionDirty)
                        {
                            if (_definitionIndices != null)
                            {
                                Array.Clear(_definitionIndices, 0, _definitionIndices.Length);
                                _definitionIndices = null;
                            }

                            if (UseLayoutRounding)
                            {
                                if (_roundingErrors != null)
                                {
                                    Array.Clear(_roundingErrors, 0, _roundingErrors.Length);
                                    _roundingErrors = null;
                                }
                            }
                        }

                        ValidateDefinitionsUStructure();
                        ValidateDefinitionsLayout(DefinitionsU, sizeToContentU);

                        ValidateDefinitionsVStructure();
                        ValidateDefinitionsLayout(DefinitionsV, sizeToContentV);

                        CellsStructureDirty |= (SizeToContentU != sizeToContentU) || (SizeToContentV != sizeToContentV);

                        SizeToContentU = sizeToContentU;
                        SizeToContentV = sizeToContentV;
                    }

                    ValidateCells();

                    Debug.Assert(DefinitionsU.Length > 0 && DefinitionsV.Length > 0);

                    //  Grid classifies cells into four groups depending on
                    //  the column / row type a cell belongs to (number corresponds to
                    //  group number):
                    //
                    //                   Px      Auto     Star
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //            Px |    1   |    1   |    3   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //          Auto |    1   |    1   |    3   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //          Star |    4   |    2   |    4   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //
                    //  The group number indicates the order in which cells are measured.
                    //  Certain order is necessary to be able to dynamically resolve star
                    //  columns / rows sizes which are used as input for measuring of
                    //  the cells belonging to them.
                    //
                    //  However, there are cases when topology of a grid causes cyclical
                    //  size dependences. For example:
                    //
                    //
                    //                         column width="Auto"      column width="*"
                    //                      +----------------------+----------------------+
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //  row height="Auto"   |                      |      cell 1 2        |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      +----------------------+----------------------+
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //  row height="*"      |       cell 2 1       |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      +----------------------+----------------------+
                    //
                    //  In order to accurately calculate constraint width for "cell 1 2"
                    //  (which is the remaining of grid's available width and calculated
                    //  value of Auto column), "cell 2 1" needs to be calculated first,
                    //  as it contributes to the Auto column's calculated value.
                    //  At the same time in order to accurately calculate constraint
                    //  height for "cell 2 1", "cell 1 2" needs to be calcualted first,
                    //  as it contributes to Auto row height, which is used in the
                    //  computation of Star row resolved height.
                    //
                    //  to "break" this cyclical dependency we are making (arbitrary)
                    //  decision to treat cells like "cell 2 1" as if they appear in Auto
                    //  rows. And then recalculate them one more time when star row
                    //  heights are resolved.
                    //
                    //  (Or more strictly) the code below implement the following logic:
                    //
                    //                       +---------+
                    //                       |  enter  |
                    //                       +---------+
                    //                            |
                    //                            V
                    //                    +----------------+
                    //                    | Measure Group1 |
                    //                    +----------------+
                    //                            |
                    //                            V
                    //                          / - \
                    //                        /       \
                    //                  Y   /    Can    \    N
                    //            +--------|   Resolve   |-----------+
                    //            |         \  StarsV?  /            |
                    //            |           \       /              |
                    //            |             \ - /                |
                    //            V                                  V
                    //    +----------------+                       / - \
                    //    | Resolve StarsV |                     /       \
                    //    +----------------+               Y   /    Can    \    N
                    //            |                      +----|   Resolve   |------+
                    //            V                      |     \  StarsU?  /       |
                    //    +----------------+             |       \       /         |
                    //    | Measure Group2 |             |         \ - /           |
                    //    +----------------+             |                         V
                    //            |                      |                 +-----------------+
                    //            V                      |                 | Measure Group2' |
                    //    +----------------+             |                 +-----------------+
                    //    | Resolve StarsU |             |                         |
                    //    +----------------+             V                         V
                    //            |              +----------------+        +----------------+
                    //            V              | Resolve StarsU |        | Resolve StarsU |
                    //    +----------------+     +----------------+        +----------------+
                    //    | Measure Group3 |             |                         |
                    //    +----------------+             V                         V
                    //            |              +----------------+        +----------------+
                    //            |              | Measure Group3 |        | Measure Group3 |
                    //            |              +----------------+        +----------------+
                    //            |                      |                         |
                    //            |                      V                         V
                    //            |              +----------------+        +----------------+
                    //            |              | Resolve StarsV |        | Resolve StarsV |
                    //            |              +----------------+        +----------------+
                    //            |                      |                         |
                    //            |                      |                         V
                    //            |                      |                +------------------+
                    //            |                      |                | Measure Group2'' |
                    //            |                      |                +------------------+
                    //            |                      |                         |
                    //            +----------------------+-------------------------+
                    //                                   |
                    //                                   V
                    //                           +----------------+
                    //                           | Measure Group4 |
                    //                           +----------------+
                    //                                   |
                    //                                   V
                    //                               +--------+
                    //                               |  exit  |
                    //                               +--------+
                    //
                    //  where:
                    //  *   all [Measure GroupN] - regular children measure process -
                    //      each cell is measured given contraint size as an input
                    //      and each cell's desired size is accumulated on the
                    //      corresponding column / row;
                    //  *   [Measure Group2'] - is when each cell is measured with
                    //      infinit height as a constraint and a cell's desired
                    //      height is ignored;
                    //  *   [Measure Groups''] - is when each cell is measured (second
                    //      time during single Grid.MeasureOverride) regularly but its
                    //      returned width is ignored;
                    //
                    //  This algorithm is believed to be as close to ideal as possible.
                    //  It has the following drawbacks:
                    //  *   cells belonging to Group2 can be called to measure twice;
                    //  *   iff during second measure a cell belonging to Group2 returns
                    //      desired width greater than desired width returned the first
                    //      time, such a cell is going to be clipped, even though it
                    //      appears in Auto column.
                    //

                    MeasureCellsGroup(extData.CellGroup1, constraint, false, false);

                    {
                        //  after Group1 is measured,  only Group3 may have cells belonging to Auto rows.
                        bool canResolveStarsV = !HasGroup3CellsInAutoRows;

                        if (canResolveStarsV)
                        {
                            if (HasStarCellsV) { ResolveStar(DefinitionsV, constraint.Height); }
                            MeasureCellsGroup(extData.CellGroup2, constraint, false, false);
                            if (HasStarCellsU) { ResolveStar(DefinitionsU, constraint.Width); }
                            MeasureCellsGroup(extData.CellGroup3, constraint, false, false);
                        }
                        else
                        {
                            //  if at least one cell exists in Group2, it must be measured before
                            //  StarsU can be resolved.
                            bool canResolveStarsU = extData.CellGroup2 > PrivateCells.Length;
                            if (canResolveStarsU)
                            {
                                if (HasStarCellsU) { ResolveStar(DefinitionsU, constraint.Width); }
                                MeasureCellsGroup(extData.CellGroup3, constraint, false, false);
                                if (HasStarCellsV) { ResolveStar(DefinitionsV, constraint.Height); }
                            }
                            else
                            {
                                // This is a revision to the algorithm employed for the cyclic
                                // dependency case described above. We now repeatedly
                                // measure Group3 and Group2 until their sizes settle. We
                                // also use a count heuristic to break a loop in case of one.

                                bool hasDesiredSizeUChanged = false;
                                int cnt=0;

                                // Cache Group2MinWidths & Group3MinHeights
                                double[] group2MinSizes = CacheMinSizes(extData.CellGroup2, false);
                                double[] group3MinSizes = CacheMinSizes(extData.CellGroup3, true);

                                MeasureCellsGroup(extData.CellGroup2, constraint, false, true);

                                do
                                {
                                    if (hasDesiredSizeUChanged)
                                    {
                                        // Reset cached Group3Heights
                                        ApplyCachedMinSizes(group3MinSizes, true);
                                    }

                                    if (HasStarCellsU) { ResolveStar(DefinitionsU, constraint.Width); }
                                    MeasureCellsGroup(extData.CellGroup3, constraint, false, false);

                                    // Reset cached Group2Widths
                                    ApplyCachedMinSizes(group2MinSizes, false);

                                    if (HasStarCellsV) { ResolveStar(DefinitionsV, constraint.Height); }
                                    MeasureCellsGroup(extData.CellGroup2, constraint, cnt == c_layoutLoopMaxCount, false, out hasDesiredSizeUChanged);
                                }
                                while (hasDesiredSizeUChanged && ++cnt <= c_layoutLoopMaxCount);
                            }
                        }
                    }

                    MeasureCellsGroup(extData.CellGroup4, constraint, false, false);

                    EnterCounter(Counters._CalculateDesiredSize);
                    gridDesiredSize = new Size(
                            CalculateDesiredSize(DefinitionsU),
                            CalculateDesiredSize(DefinitionsV));
                    ExitCounter(Counters._CalculateDesiredSize);
                }
            }
            finally
            {
                MeasureOverrideInProgress = false;
                ExitCounterScope(Counters.MeasureOverride);
            }

            return (gridDesiredSize);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            try
            {
                EnterCounterScope(Counters.ArrangeOverride);

                ArrangeOverrideInProgress = true;

                if (_data == null)
                {
                    UIElementCollection children = InternalChildren;

                    for (int i = 0, count = children.Count; i < count; ++i)
                    {
                        UIElement child = children[i];
                        if (child != null)
                        {
                            child.Arrange(new Rect(arrangeSize));
                        }
                    }
                }
                else
                {
                    Debug.Assert(DefinitionsU.Length > 0 && DefinitionsV.Length > 0);

                    EnterCounter(Counters._SetFinalSize);

                    SetFinalSize(DefinitionsU, arrangeSize.Width, true);
                    SetFinalSize(DefinitionsV, arrangeSize.Height, false);

                    ExitCounter(Counters._SetFinalSize);

                    UIElementCollection children = InternalChildren;

                    for (int currentCell = 0; currentCell < PrivateCells.Length; ++currentCell)
                    {
                        UIElement cell = children[currentCell];
                        if (cell == null)
                        {
                            continue;
                        }

                        int columnIndex = PrivateCells[currentCell].ColumnIndex;
                        int rowIndex = PrivateCells[currentCell].RowIndex;
                        int columnSpan = PrivateCells[currentCell].ColumnSpan;
                        int rowSpan = PrivateCells[currentCell].RowSpan;

                        Rect cellRect = new Rect(
                            columnIndex == 0 ? 0.0 : DefinitionsU[columnIndex].FinalOffset,
                            rowIndex == 0 ? 0.0 : DefinitionsV[rowIndex].FinalOffset,
                            GetFinalSizeForRange(DefinitionsU, columnIndex, columnSpan),
                            GetFinalSizeForRange(DefinitionsV, rowIndex, rowSpan)   );

                        EnterCounter(Counters._ArrangeChildHelper2);
                        cell.Arrange(cellRect);
                        ExitCounter(Counters._ArrangeChildHelper2);
                    }

                    //  update render bound on grid lines renderer visual
                    GridLinesRenderer gridLinesRenderer = EnsureGridLinesRenderer();
                    if (gridLinesRenderer != null)
                    {
                        gridLinesRenderer.UpdateRenderBounds(arrangeSize);
                    }
                }
            }
            finally
            {
                SetValid();
                ArrangeOverrideInProgress = false;
                ExitCounterScope(Counters.ArrangeOverride);
            }
            return (arrangeSize);
        }

        /// <summary>
        /// <see cref="Visual.OnVisualChildrenChanged"/>
        /// </summary>
        protected internal override void OnVisualChildrenChanged(
            DependencyObject visualAdded,
            DependencyObject visualRemoved)
        {
            CellsStructureDirty = true;

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///     Invalidates grid caches and makes the grid dirty for measure.
        /// </summary>
        internal void Invalidate()
        {
            CellsStructureDirty = true;
            InvalidateMeasure();
        }

        /// <summary>
        ///     Returns final width for a column.
        /// </summary>
        /// <remarks>
        ///     Used from public ColumnDefinition ActualWidth. Calculates final width using offset data.
        /// </remarks>
        internal double GetFinalColumnDefinitionWidth(int columnIndex)
        {
            double value = 0.0;

            Invariant.Assert(_data != null);

            //  actual value calculations require structure to be up-to-date
            if (!ColumnDefinitionCollectionDirty)
            {
                DefinitionBase[] definitions = DefinitionsU;
                value = definitions[(columnIndex + 1) % definitions.Length].FinalOffset;
                if (columnIndex != 0) { value -= definitions[columnIndex].FinalOffset; }
            }
            return (value);
        }

        /// <summary>
        ///     Returns final height for a row.
        /// </summary>
        /// <remarks>
        ///     Used from public RowDefinition ActualHeight. Calculates final height using offset data.
        /// </remarks>
        internal double GetFinalRowDefinitionHeight(int rowIndex)
        {
            double value = 0.0;

            Invariant.Assert(_data != null);

            //  actual value calculations require structure to be up-to-date
            if (!RowDefinitionCollectionDirty)
            {
                DefinitionBase[] definitions = DefinitionsV;
                value = definitions[(rowIndex + 1) % definitions.Length].FinalOffset;
                if (rowIndex != 0) { value -= definitions[rowIndex].FinalOffset; }
            }
            return (value);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Convenience accessor to MeasureOverrideInProgress bit flag.
        /// </summary>
        internal bool MeasureOverrideInProgress
        {
            get { return (CheckFlagsAnd(Flags.MeasureOverrideInProgress)); }
            set { SetFlags(value, Flags.MeasureOverrideInProgress); }
        }

        /// <summary>
        /// Convenience accessor to ArrangeOverrideInProgress bit flag.
        /// </summary>
        internal bool ArrangeOverrideInProgress
        {
            get { return (CheckFlagsAnd(Flags.ArrangeOverrideInProgress)); }
            set { SetFlags(value, Flags.ArrangeOverrideInProgress); }
        }

        /// <summary>
        /// Convenience accessor to ValidDefinitionsUStructure bit flag.
        /// </summary>
        internal bool ColumnDefinitionCollectionDirty
        {
            get { return (!CheckFlagsAnd(Flags.ValidDefinitionsUStructure)); }
            set { SetFlags(!value, Flags.ValidDefinitionsUStructure); }
        }

        /// <summary>
        /// Convenience accessor to ValidDefinitionsVStructure bit flag.
        /// </summary>
        internal bool RowDefinitionCollectionDirty
        {
            get { return (!CheckFlagsAnd(Flags.ValidDefinitionsVStructure)); }
            set { SetFlags(!value, Flags.ValidDefinitionsVStructure); }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Lays out cells according to rows and columns, and creates lookup grids.
        /// </summary>
        private void ValidateCells()
        {
            EnterCounter(Counters._ValidateCells);

            if (CellsStructureDirty)
            {
                ValidateCellsCore();
                CellsStructureDirty = false;
            }

            ExitCounter(Counters._ValidateCells);
        }

        /// <summary>
        /// ValidateCellsCore
        /// </summary>
        private void ValidateCellsCore()
        {
            UIElementCollection children = InternalChildren;
            ExtendedData extData = ExtData;

            extData.CellCachesCollection = new CellCache[children.Count];
            extData.CellGroup1 = int.MaxValue;
            extData.CellGroup2 = int.MaxValue;
            extData.CellGroup3 = int.MaxValue;
            extData.CellGroup4 = int.MaxValue;

            bool hasStarCellsU = false;
            bool hasStarCellsV = false;
            bool hasGroup3CellsInAutoRows = false;

            for (int i = PrivateCells.Length - 1; i >= 0; --i)
            {
                UIElement child = children[i];
                if (child == null)
                {
                    continue;
                }

                CellCache cell = new CellCache();

                //
                //  read and cache child positioning properties
                //

                //  read indices from the corresponding properties
                //      clamp to value < number_of_columns
                //      column >= 0 is guaranteed by property value validation callback
                cell.ColumnIndex = Math.Min(GetColumn(child), DefinitionsU.Length - 1);
                //      clamp to value < number_of_rows
                //      row >= 0 is guaranteed by property value validation callback
                cell.RowIndex = Math.Min(GetRow(child), DefinitionsV.Length - 1);

                //  read span properties
                //      clamp to not exceed beyond right side of the grid
                //      column_span > 0 is guaranteed by property value validation callback
                cell.ColumnSpan = Math.Min(GetColumnSpan(child), DefinitionsU.Length - cell.ColumnIndex);

                //      clamp to not exceed beyond bottom side of the grid
                //      row_span > 0 is guaranteed by property value validation callback
                cell.RowSpan = Math.Min(GetRowSpan(child), DefinitionsV.Length - cell.RowIndex);

                Debug.Assert(0 <= cell.ColumnIndex && cell.ColumnIndex < DefinitionsU.Length);
                Debug.Assert(0 <= cell.RowIndex && cell.RowIndex < DefinitionsV.Length);

                //
                //  calculate and cache length types for the child
                //

                cell.SizeTypeU = GetLengthTypeForRange(DefinitionsU, cell.ColumnIndex, cell.ColumnSpan);
                cell.SizeTypeV = GetLengthTypeForRange(DefinitionsV, cell.RowIndex, cell.RowSpan);

                hasStarCellsU |= cell.IsStarU;
                hasStarCellsV |= cell.IsStarV;

                //
                //  distribute cells into four groups.
                //

                if (!cell.IsStarV)
                {
                    if (!cell.IsStarU)
                    {
                        cell.Next = extData.CellGroup1;
                        extData.CellGroup1 = i;
                    }
                    else
                    {
                        cell.Next = extData.CellGroup3;
                        extData.CellGroup3 = i;

                        //  remember if this cell belongs to auto row
                        hasGroup3CellsInAutoRows |= cell.IsAutoV;
                    }
                }
                else
                {
                    if (    cell.IsAutoU
                            //  note below: if spans through Star column it is NOT Auto
                        &&  !cell.IsStarU )
                    {
                        cell.Next = extData.CellGroup2;
                        extData.CellGroup2 = i;
                    }
                    else
                    {
                        cell.Next = extData.CellGroup4;
                        extData.CellGroup4 = i;
                    }
                }

                PrivateCells[i] = cell;
            }

            HasStarCellsU = hasStarCellsU;
            HasStarCellsV = hasStarCellsV;
            HasGroup3CellsInAutoRows = hasGroup3CellsInAutoRows;
        }

        /// <summary>
        /// Initializes DefinitionsU memeber either to user supplied ColumnDefinitions collection
        /// or to a default single element collection. DefinitionsU gets trimmed to size.
        /// </summary>
        /// <remarks>
        /// This is one of two methods, where ColumnDefinitions and DefinitionsU are directly accessed.
        /// All the rest measure / arrange / render code must use DefinitionsU.
        /// </remarks>
        private void ValidateDefinitionsUStructure()
        {
            EnterCounter(Counters._ValidateColsStructure);

            if (ColumnDefinitionCollectionDirty)
            {
                ExtendedData extData = ExtData;

                if (extData.ColumnDefinitions == null)
                {
                    if (extData.DefinitionsU == null)
                    {
                        extData.DefinitionsU = new DefinitionBase[] { new ColumnDefinition() };
                    }
                }
                else
                {
                    extData.ColumnDefinitions.InternalTrimToSize();

                    if (extData.ColumnDefinitions.InternalCount == 0)
                    {
                        //  if column definitions collection is empty
                        //  mockup array with one column
                        extData.DefinitionsU = new DefinitionBase[] { new ColumnDefinition() };
                    }
                    else
                    {
                        extData.DefinitionsU = extData.ColumnDefinitions.InternalItems;
                    }
                }

                ColumnDefinitionCollectionDirty = false;
            }

            Debug.Assert(ExtData.DefinitionsU != null && ExtData.DefinitionsU.Length > 0);

            ExitCounter(Counters._ValidateColsStructure);
        }

        /// <summary>
        /// Initializes DefinitionsV memeber either to user supplied RowDefinitions collection
        /// or to a default single element collection. DefinitionsV gets trimmed to size.
        /// </summary>
        /// <remarks>
        /// This is one of two methods, where RowDefinitions and DefinitionsV are directly accessed.
        /// All the rest measure / arrange / render code must use DefinitionsV.
        /// </remarks>
        private void ValidateDefinitionsVStructure()
        {
            EnterCounter(Counters._ValidateRowsStructure);

            if (RowDefinitionCollectionDirty)
            {
                ExtendedData extData = ExtData;

                if (extData.RowDefinitions == null)
                {
                    if (extData.DefinitionsV == null)
                    {
                        extData.DefinitionsV = new DefinitionBase[] { new RowDefinition() };
                    }
                }
                else
                {
                    extData.RowDefinitions.InternalTrimToSize();

                    if (extData.RowDefinitions.InternalCount == 0)
                    {
                        //  if row definitions collection is empty
                        //  mockup array with one row
                        extData.DefinitionsV = new DefinitionBase[] { new RowDefinition() };
                    }
                    else
                    {
                        extData.DefinitionsV = extData.RowDefinitions.InternalItems;
                    }
                }

                RowDefinitionCollectionDirty = false;
            }

            Debug.Assert(ExtData.DefinitionsV != null && ExtData.DefinitionsV.Length > 0);

            ExitCounter(Counters._ValidateRowsStructure);
        }

        /// <summary>
        /// Validates layout time size type information on given array of definitions.
        /// Sets MinSize and MeasureSizes.
        /// </summary>
        /// <param name="definitions">Array of definitions to update.</param>
        /// <param name="treatStarAsAuto">if "true" then star definitions are treated as Auto.</param>
        private void ValidateDefinitionsLayout(
            DefinitionBase[] definitions,
            bool treatStarAsAuto)
        {
            for (int i = 0; i < definitions.Length; ++i)
            {
                definitions[i].OnBeforeLayout(this);

                double userMinSize = definitions[i].UserMinSize;
                double userMaxSize = definitions[i].UserMaxSize;
                double userSize = 0;

                switch (definitions[i].UserSize.GridUnitType)
                {
                    case (GridUnitType.Pixel):
                        definitions[i].SizeType = LayoutTimeSizeType.Pixel;
                        userSize = definitions[i].UserSize.Value;
                        // this was brought with NewLayout and defeats squishy behavior
                        userMinSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
                        break;
                    case (GridUnitType.Auto):
                        definitions[i].SizeType = LayoutTimeSizeType.Auto;
                        userSize = double.PositiveInfinity;
                        break;
                    case (GridUnitType.Star):
                        if (treatStarAsAuto)
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Auto;
                            userSize = double.PositiveInfinity;
                        }
                        else
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Star;
                            userSize = double.PositiveInfinity;
                        }
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                definitions[i].UpdateMinSize(userMinSize);
                definitions[i].MeasureSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
            }
        }

        private double[] CacheMinSizes(int cellsHead, bool isRows)
        {
            double[] minSizes = isRows ? new double[DefinitionsV.Length] : new double[DefinitionsU.Length];

            for (int j=0; j<minSizes.Length; j++)
            {
                minSizes[j] = -1;
            }

            int i = cellsHead;
            do
            {
                if (isRows)
                {
                    minSizes[PrivateCells[i].RowIndex] = DefinitionsV[PrivateCells[i].RowIndex].MinSize;
                }
                else
                {
                    minSizes[PrivateCells[i].ColumnIndex] = DefinitionsU[PrivateCells[i].ColumnIndex].MinSize;
                }

                i = PrivateCells[i].Next;
            } while (i < PrivateCells.Length);

            return minSizes;
        }

        private void ApplyCachedMinSizes(double[] minSizes, bool isRows)
        {
            for (int i=0; i<minSizes.Length; i++)
            {
                if (DoubleUtil.GreaterThanOrClose(minSizes[i], 0))
                {
                    if (isRows)
                    {
                        DefinitionsV[i].SetMinSize(minSizes[i]);
                    }
                    else
                    {
                        DefinitionsU[i].SetMinSize(minSizes[i]);
                    }
                }
            }
        }

        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV)
        {
            bool unusedHasDesiredSizeUChanged;
            MeasureCellsGroup(cellsHead, referenceSize, ignoreDesiredSizeU, forceInfinityV, out unusedHasDesiredSizeUChanged);
        }

        /// <summary>
        /// Measures one group of cells.
        /// </summary>
        /// <param name="cellsHead">Head index of the cells chain.</param>
        /// <param name="referenceSize">Reference size for spanned cells
        /// calculations.</param>
        /// <param name="ignoreDesiredSizeU">When "true" cells' desired
        /// width is not registered in columns.</param>
        /// <param name="forceInfinityV">Passed through to MeasureCell.
        /// When "true" cells' desired height is not registered in rows.</param>
        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV,
            out bool hasDesiredSizeUChanged)
        {
            hasDesiredSizeUChanged = false;

            if (cellsHead >= PrivateCells.Length)
            {
                return;
            }

            UIElementCollection children = InternalChildren;
            Hashtable spanStore = null;
            bool ignoreDesiredSizeV = forceInfinityV;

            int i = cellsHead;
            do
            {
                double oldWidth = children[i].DesiredSize.Width;

                MeasureCell(i, forceInfinityV);

                hasDesiredSizeUChanged |= !DoubleUtil.AreClose(oldWidth, children[i].DesiredSize.Width);

                if (!ignoreDesiredSizeU)
                {
                    if (PrivateCells[i].ColumnSpan == 1)
                    {
                        DefinitionsU[PrivateCells[i].ColumnIndex].UpdateMinSize(Math.Min(children[i].DesiredSize.Width, DefinitionsU[PrivateCells[i].ColumnIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            PrivateCells[i].ColumnIndex,
                            PrivateCells[i].ColumnSpan,
                            true,
                            children[i].DesiredSize.Width);
                    }
                }

                if (!ignoreDesiredSizeV)
                {
                    if (PrivateCells[i].RowSpan == 1)
                    {
                        DefinitionsV[PrivateCells[i].RowIndex].UpdateMinSize(Math.Min(children[i].DesiredSize.Height, DefinitionsV[PrivateCells[i].RowIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            PrivateCells[i].RowIndex,
                            PrivateCells[i].RowSpan,
                            false,
                            children[i].DesiredSize.Height);
                    }
                }

                i = PrivateCells[i].Next;
            } while (i < PrivateCells.Length);

            if (spanStore != null)
            {
                foreach (DictionaryEntry e in spanStore)
                {
                    SpanKey key = (SpanKey)e.Key;
                    double requestedSize = (double)e.Value;

                    EnsureMinSizeInDefinitionRange(
                        key.U ? DefinitionsU : DefinitionsV,
                        key.Start,
                        key.Count,
                        requestedSize,
                        key.U ? referenceSize.Width : referenceSize.Height);
                }
            }
        }

        /// <summary>
        /// Helper method to register a span information for delayed processing.
        /// </summary>
        /// <param name="store">Reference to a hashtable object used as storage.</param>
        /// <param name="start">Span starting index.</param>
        /// <param name="count">Span count.</param>
        /// <param name="u"><c>true</c> if this is a column span. <c>false</c> if this is a row span.</param>
        /// <param name="value">Value to store. If an entry already exists the biggest value is stored.</param>
        private static void RegisterSpan(
            ref Hashtable store,
            int start,
            int count,
            bool u,
            double value)
        {
            if (store == null)
            {
                store = new Hashtable();
            }

            SpanKey key = new SpanKey(start, count, u);
            object o = store[key];

            if (    o == null
                ||  value > (double)o   )
            {
                store[key] = value;
            }
        }

        /// <summary>
        /// Takes care of measuring a single cell.
        /// </summary>
        /// <param name="cell">Index of the cell to measure.</param>
        /// <param name="forceInfinityV">If "true" then cell is always
        /// calculated to infinite height.</param>
        private void MeasureCell(
            int cell,
            bool forceInfinityV)
        {
            EnterCounter(Counters._MeasureCell);

            double cellMeasureWidth;
            double cellMeasureHeight;

            if (    PrivateCells[cell].IsAutoU
                &&  !PrivateCells[cell].IsStarU   )
            {
                //  if cell belongs to at least one Auto column and not a single Star column
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureWidth = double.PositiveInfinity;
            }
            else
            {
                //  otherwise...
                cellMeasureWidth = GetMeasureSizeForRange(
                                        DefinitionsU,
                                        PrivateCells[cell].ColumnIndex,
                                        PrivateCells[cell].ColumnSpan);
            }

            if (forceInfinityV)
            {
                cellMeasureHeight = double.PositiveInfinity;
            }
            else if (   PrivateCells[cell].IsAutoV
                    &&  !PrivateCells[cell].IsStarV   )
            {
                //  if cell belongs to at least one Auto row and not a single Star row
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureHeight = double.PositiveInfinity;
            }
            else
            {
                cellMeasureHeight = GetMeasureSizeForRange(
                                        DefinitionsV,
                                        PrivateCells[cell].RowIndex,
                                        PrivateCells[cell].RowSpan);
            }

            EnterCounter(Counters.__MeasureChild);
            UIElement child = InternalChildren[cell];
            if (child != null)
            {
                Size childConstraint = new Size(cellMeasureWidth, cellMeasureHeight);
                child.Measure(childConstraint);
            }
            ExitCounter(Counters.__MeasureChild);

            ExitCounter(Counters._MeasureCell);
        }


        /// <summary>
        /// Calculates one dimensional measure size for given definitions' range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Calculated measure size.</returns>
        /// <remarks>
        /// For "Auto" definitions MinWidth is used in place of PreferredSize.
        /// </remarks>
        private double GetMeasureSizeForRange(
            DefinitionBase[] definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Length);

            double measureSize = 0;
            int i = start + count - 1;

            do
            {
                measureSize += (definitions[i].SizeType == LayoutTimeSizeType.Auto)
                    ? definitions[i].MinSize
                    : definitions[i].MeasureSize;
            } while (--i >= start);

            return (measureSize);
        }

        /// <summary>
        /// Accumulates length type information for given definition's range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Length type for given range.</returns>
        private LayoutTimeSizeType GetLengthTypeForRange(
            DefinitionBase[] definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Length);

            LayoutTimeSizeType lengthType = LayoutTimeSizeType.None;
            int i = start + count - 1;

            do
            {
                lengthType |= definitions[i].SizeType;
            } while (--i >= start);

            return (lengthType);
        }

        /// <summary>
        /// Distributes min size back to definition array's range.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <param name="requestedSize">Minimum size that should "fit" into the definitions range.</param>
        /// <param name="definitions">Definition array receiving distribution.</param>
        /// <param name="percentReferenceSize">Size used to resolve percentages.</param>
        private void EnsureMinSizeInDefinitionRange(
            DefinitionBase[] definitions,
            int start,
            int count,
            double requestedSize,
            double percentReferenceSize)
        {
            Debug.Assert(1 < count && 0 <= start && (start + count) <= definitions.Length);

            //  avoid processing when asked to distribute "0"
            if (!_IsZero(requestedSize))
            {
                DefinitionBase[] tempDefinitions = TempDefinitions; //  temp array used to remember definitions for sorting
                int end = start + count;
                int autoDefinitionsCount = 0;
                double rangeMinSize = 0;
                double rangePreferredSize = 0;
                double rangeMaxSize = 0;
                double maxMaxSize = 0;                              //  maximum of maximum sizes

                //  first accumulate the necessary information:
                //  a) sum up the sizes in the range;
                //  b) count the number of auto definitions in the range;
                //  c) initialize temp array
                //  d) cache the maximum size into SizeCache
                //  e) accumulate max of max sizes
                for (int i = start; i < end; ++i)
                {
                    double minSize = definitions[i].MinSize;
                    double preferredSize = definitions[i].PreferredSize;
                    double maxSize = Math.Max(definitions[i].UserMaxSize, minSize);

                    rangeMinSize += minSize;
                    rangePreferredSize += preferredSize;
                    rangeMaxSize += maxSize;

                    definitions[i].SizeCache = maxSize;

                    //  sanity check: no matter what, but min size must always be the smaller;
                    //  max size must be the biggest; and preferred should be in between
                    Debug.Assert(   minSize <= preferredSize
                                &&  preferredSize <= maxSize
                                &&  rangeMinSize <= rangePreferredSize
                                &&  rangePreferredSize <= rangeMaxSize  );

                    if (maxMaxSize < maxSize)   maxMaxSize = maxSize;
                    if (definitions[i].UserSize.IsAuto) autoDefinitionsCount++;
                    tempDefinitions[i - start] = definitions[i];
                }

                //  avoid processing if the range already big enough
                if (requestedSize > rangeMinSize)
                {
                    if (requestedSize <= rangePreferredSize)
                    {
                        //
                        //  requestedSize fits into preferred size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to equi-size min sizes, without exceeding preferred size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all auto definitions
                        //  are first, then definitions follow ascending order with PreferredSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, s_spanPreferredDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize; i < autoDefinitionsCount; ++i)
                        {
                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinitions[i].UserSize.IsAuto);

                            //  adjust sizeToDistribute value by subtracting auto definition min size
                            sizeToDistribute -= (tempDefinitions[i].MinSize);
                        }

                        for (; i < count; ++i)
                        {
                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinitions[i].UserSize.IsAuto);

                            double newMinSize = Math.Min(sizeToDistribute / (count - i), tempDefinitions[i].PreferredSize);
                            if (newMinSize > tempDefinitions[i].MinSize) { tempDefinitions[i].UpdateMinSize(newMinSize); }
                            sizeToDistribute -= newMinSize;
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(_IsZero(sizeToDistribute));
                    }
                    else if (requestedSize <= rangeMaxSize)
                    {
                        //
                        //  requestedSize bigger than preferred size, but fit into max size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions, if possible - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to euqi-size min sizes, without exceeding max size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all non-auto definitions
                        //  are last, then definitions follow ascending order with MaxSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, s_spanMaxDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize - rangePreferredSize; i < count - autoDefinitionsCount; ++i)
                        {
                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinitions[i].UserSize.IsAuto);

                            double preferredSize = tempDefinitions[i].PreferredSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - autoDefinitionsCount - i);
                            tempDefinitions[i].UpdateMinSize(Math.Min(newMinSize, tempDefinitions[i].SizeCache));
                            sizeToDistribute -= (tempDefinitions[i].MinSize - preferredSize);
                        }

                        for (; i < count; ++i)
                        {
                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinitions[i].UserSize.IsAuto);

                            double preferredSize = tempDefinitions[i].MinSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - i);
                            tempDefinitions[i].UpdateMinSize(Math.Min(newMinSize, tempDefinitions[i].SizeCache));
                            sizeToDistribute -= (tempDefinitions[i].MinSize - preferredSize);
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(_IsZero(sizeToDistribute));
                    }
                    else
                    {
                        //
                        //  requestedSize bigger than max size of the range.
                        //  distribute according to the following logic:
                        //  * for all definitions distribute to equi-size min sizes.
                        //
                        double equalSize = requestedSize / count;

                        if (    equalSize < maxMaxSize
                            &&  !_AreClose(equalSize, maxMaxSize)   )
                        {
                            //  equi-size is less than maximum of maxSizes.
                            //  in this case distribute so that smaller definitions grow faster than
                            //  bigger ones.
                            double totalRemainingSize = maxMaxSize * count - rangeMaxSize;
                            double sizeToDistribute = requestedSize - rangeMaxSize;

                            //  sanity check: totalRemainingSize and sizeToDistribute must be real positive numbers
                            Debug.Assert(   !double.IsInfinity(totalRemainingSize)
                                        &&  !DoubleUtil.IsNaN(totalRemainingSize)
                                        &&  totalRemainingSize > 0
                                        &&  !double.IsInfinity(sizeToDistribute)
                                        &&  !DoubleUtil.IsNaN(sizeToDistribute)
                                        &&  sizeToDistribute > 0    );

                            for (int i = 0; i < count; ++i)
                            {
                                double deltaSize = (maxMaxSize - tempDefinitions[i].SizeCache) * sizeToDistribute / totalRemainingSize;
                                tempDefinitions[i].UpdateMinSize(tempDefinitions[i].SizeCache + deltaSize);
                            }
                        }
                        else
                        {
                            //
                            //  equi-size is greater or equal to maximum of max sizes.
                            //  all definitions receive equalSize as their mim sizes.
                            //
                            for (int i = 0; i < count; ++i)
                            {
                                tempDefinitions[i].UpdateMinSize(equalSize);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves Star's for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to resolve stars.</param>
        /// <param name="availableSize">All available size.</param>
        /// <remarks>
        /// Must initialize LayoutSize for all Star entries in given array of definitions.
        /// </remarks>
        private void ResolveStar(
            DefinitionBase[] definitions,
            double availableSize)
        {
            if (FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpace)
            {
                ResolveStarLegacy(definitions, availableSize);
            }
            else
            {
                ResolveStarMaxDiscrepancy(definitions, availableSize);
            }
        }

        // original implementation, used from 3.0 through 4.6.2
        private void ResolveStarLegacy(
            DefinitionBase[] definitions,
            double availableSize)
        {
            DefinitionBase[] tempDefinitions = TempDefinitions;
            int starDefinitionsCount = 0;
            double takenSize = 0;

            for (int i = 0; i < definitions.Length; ++i)
            {
                switch (definitions[i].SizeType)
                {
                    case (LayoutTimeSizeType.Auto):
                        takenSize += definitions[i].MinSize;
                        break;
                    case (LayoutTimeSizeType.Pixel):
                        takenSize += definitions[i].MeasureSize;
                        break;
                    case (LayoutTimeSizeType.Star):
                        {
                            tempDefinitions[starDefinitionsCount++] = definitions[i];

                            double starValue = definitions[i].UserSize.Value;

                            if (_IsZero(starValue))
                            {
                                definitions[i].MeasureSize = 0;
                                definitions[i].SizeCache = 0;
                            }
                            else
                            {
                                //  clipping by c_starClip guarantees that sum of even a very big number of max'ed out star values
                                //  can be summed up without overflow
                                starValue = Math.Min(starValue, c_starClip);

                                //  Note: normalized star value is temporary cached into MeasureSize
                                definitions[i].MeasureSize = starValue;
                                double maxSize             = Math.Max(definitions[i].MinSize, definitions[i].UserMaxSize);
                                maxSize                    = Math.Min(maxSize, c_starClip);
                                definitions[i].SizeCache   = maxSize / starValue;
                            }
                        }
                        break;
                }
            }

            if (starDefinitionsCount > 0)
            {
                Array.Sort(tempDefinitions, 0, starDefinitionsCount, s_starDistributionOrderComparer);

                //  the 'do {} while' loop below calculates sum of star weights in order to avoid fp overflow...
                //  partial sum value is stored in each definition's SizeCache member.
                //  this way the algorithm guarantees (starValue <= definition.SizeCache) and thus
                //  (starValue / definition.SizeCache) will never overflow due to sum of star weights becoming zero.
                //  this is an important change from previous implementation where the following was possible:
                //  ((BigValueStar + SmallValueStar) - BigValueStar) resulting in 0...
                double allStarWeights = 0;
                int i = starDefinitionsCount - 1;
                do
                {
                    allStarWeights += tempDefinitions[i].MeasureSize;
                    tempDefinitions[i].SizeCache = allStarWeights;
                } while (--i >= 0);

                i = 0;
                do
                {
                    double resolvedSize;
                    double starValue = tempDefinitions[i].MeasureSize;

                    if (_IsZero(starValue))
                    {
                        resolvedSize = tempDefinitions[i].MinSize;
                    }
                    else
                    {
                        double userSize = Math.Max(availableSize - takenSize, 0.0) * (starValue / tempDefinitions[i].SizeCache);
                        resolvedSize    = Math.Min(userSize, tempDefinitions[i].UserMaxSize);
                        resolvedSize    = Math.Max(tempDefinitions[i].MinSize, resolvedSize);
                    }

                    tempDefinitions[i].MeasureSize = resolvedSize;
                    takenSize                     += resolvedSize;
                } while (++i < starDefinitionsCount);
            }
        }

        // new implementation as of 4.7.  Several improvements:
        // 1. Allocate to *-defs hitting their min or max constraints, before allocating
        //      to other *-defs.  A def that hits its min uses more space than its
        //      proportional share, reducing the space available to everyone else.
        //      The legacy algorithm deducted this space only from defs processed
        //      after the min;  the new algorithm deducts it proportionally from all
        //      defs.   This avoids the "*-defs exceed available space" problem,
        //      and other related problems where *-defs don't receive proportional
        //      allocations even though no constraints are preventing it.
        // 2. When multiple defs hit min or max, resolve the one with maximum
        //      discrepancy (defined below).   This avoids discontinuities - small
        //      change in available space resulting in large change to one def's allocation.
        // 3. Correct handling of large *-values, including Infinity.
        private void ResolveStarMaxDiscrepancy(
            DefinitionBase[] definitions,
            double availableSize)
        {
            int defCount = definitions.Length;
            DefinitionBase[] tempDefinitions = TempDefinitions;
            int minCount = 0, maxCount = 0;
            double takenSize = 0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;     // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i=0; i<defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (Double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > Double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(Double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }

            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3=true; runPhase2and3; )
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of tempDefinitions,
                // the "max" list in the second half.  TempDefinitions has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i=0; i<defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    switch (def.SizeType)
                    {
                        case (LayoutTimeSizeType.Auto):
                            takenSize += definitions[i].MinSize;
                            break;
                        case (LayoutTimeSizeType.Pixel):
                            takenSize += def.MeasureSize;
                            break;
                        case (LayoutTimeSizeType.Star):
                            if (def.MeasureSize < 0.0)
                            {
                                takenSize += -def.MeasureSize;  // already resolved
                            }
                            else
                            {
                                double starWeight = StarWeight(def, scale);
                                totalStarWeight += starWeight;

                                if (def.MinSize > 0.0)
                                {
                                    // store ratio w/min in MeasureSize (for now)
                                    tempDefinitions[minCount++] = def;
                                    def.MeasureSize = starWeight / def.MinSize;
                                }

                                double effectiveMaxSize = Math.Max(def.MinSize, def.UserMaxSize);
                                if (!Double.IsPositiveInfinity(effectiveMaxSize))
                                {
                                    // store ratio w/max in SizeCache (for now)
                                    tempDefinitions[defCount + maxCount++] = def;
                                    def.SizeCache = starWeight / effectiveMaxSize;
                                }
                            }
                            break;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = availableSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;
                Array.Sort(tempDefinitions, 0, minCount, s_minRatioComparer);
                Array.Sort(tempDefinitions, defCount, maxCount, s_maxRatioComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.SizeType == LayoutTimeSizeType.Star && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? tempDefinitions[minCount - 1].MeasureSize : Double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? tempDefinitions[defCount + maxCount - 1].SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!(chooseMin.HasValue))
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedDef = tempDefinitions[minCount - 1];
                        resolvedSize = resolvedDef.MinSize;
                        --minCount;
                    }
                    else
                    {
                        resolvedDef = tempDefinitions[defCount + maxCount - 1];
                        resolvedSize = Math.Max(resolvedDef.MinSize, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = availableSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && tempDefinitions[minCount - 1].MeasureSize < 0.0)
                    {
                        --minCount;
                        tempDefinitions[minCount] = null;
                    }
                    while (maxCount > 0 && tempDefinitions[defCount + maxCount - 1].MeasureSize < 0.0)
                    {
                        --maxCount;
                        tempDefinitions[defCount + maxCount] = null;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < availableSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        DefinitionBase def = tempDefinitions[i];
                        if (def != null)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > availableSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        DefinitionBase def = tempDefinitions[defCount + i];
                        if (def != null)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i=0; i<defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its measure size
                        def.MeasureSize = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        tempDefinitions[starCount++] = def;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                Array.Sort(tempDefinitions, 0, starCount, s_starWeightComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = tempDefinitions[i];
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = tempDefinitions[i];
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(availableSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSize, resolvedSize);

                    def.MeasureSize = resolvedSize;
                    takenSize += resolvedSize;
                }
            }
        }

        /// <summary>
        /// Calculates desired size for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to use for calculations.</param>
        /// <returns>Desired size.</returns>
        private double CalculateDesiredSize(
            DefinitionBase[] definitions)
        {
            double desiredSize = 0;

            for (int i = 0; i < definitions.Length; ++i)
            {
                desiredSize += definitions[i].MinSize;
            }

            return (desiredSize);
        }

        /// <summary>
        /// Calculates and sets final size for all definitions in the given array.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="finalSize">Final size to lay out to.</param>
        /// <param name="rows">True if sizing row definitions, false for columns</param>
        private void SetFinalSize(
            DefinitionBase[] definitions,
            double finalSize,
            bool columns)
        {
            if (FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpace)
            {
                SetFinalSizeLegacy(definitions, finalSize, columns);
            }
            else
            {
                SetFinalSizeMaxDiscrepancy(definitions, finalSize, columns);
            }
        }

        // original implementation, used from 3.0 through 4.6.2
        private void SetFinalSizeLegacy(
            DefinitionBase[] definitions,
            double finalSize,
            bool columns)
        {
            int starDefinitionsCount = 0;                       //  traverses form the first entry up
            int nonStarIndex = definitions.Length;              //  traverses from the last entry down
            double allPreferredArrangeSize = 0;
            bool useLayoutRounding = this.UseLayoutRounding;
            int[] definitionIndices = DefinitionIndices;
            double[] roundingErrors = null;

            // If using layout rounding, check whether rounding needs to compensate for high DPI
            double dpi = 1.0;

            if (useLayoutRounding)
            {
                DpiScale dpiScale = GetDpi();
                dpi = columns ? dpiScale.DpiScaleX : dpiScale.DpiScaleY;
                roundingErrors = RoundingErrors;
            }

            for (int i = 0; i < definitions.Length; ++i)
            {
                //  if definition is shared then is cannot be star
                Debug.Assert(!definitions[i].IsShared || !definitions[i].UserSize.IsStar);

                if (definitions[i].UserSize.IsStar)
                {
                    double starValue = definitions[i].UserSize.Value;

                    if (_IsZero(starValue))
                    {
                        //  cach normilized star value temporary into MeasureSize
                        definitions[i].MeasureSize = 0;
                        definitions[i].SizeCache = 0;
                    }
                    else
                    {
                        //  clipping by c_starClip guarantees that sum of even a very big number of max'ed out star values
                        //  can be summed up without overflow
                        starValue = Math.Min(starValue, c_starClip);

                        //  Note: normalized star value is temporary cached into MeasureSize
                        definitions[i].MeasureSize = starValue;
                        double maxSize = Math.Max(definitions[i].MinSizeForArrange, definitions[i].UserMaxSize);
                        maxSize = Math.Min(maxSize, c_starClip);
                        definitions[i].SizeCache = maxSize / starValue;
                        if (useLayoutRounding)
                        {
                            roundingErrors[i] = definitions[i].SizeCache;
                            definitions[i].SizeCache = UIElement.RoundLayoutValue(definitions[i].SizeCache, dpi);
                        }
                    }
                    definitionIndices[starDefinitionsCount++] = i;
                }
                else
                {
                    double userSize = 0;

                    switch (definitions[i].UserSize.GridUnitType)
                    {
                        case (GridUnitType.Pixel):
                            userSize = definitions[i].UserSize.Value;
                            break;

                        case (GridUnitType.Auto):
                            userSize = definitions[i].MinSizeForArrange;
                            break;
                    }

                    double userMaxSize;

                    if (definitions[i].IsShared)
                    {
                        //  overriding userMaxSize effectively prevents squishy-ness.
                        //  this is a "solution" to avoid shared definitions from been sized to
                        //  different final size at arrange time, if / when different grids receive
                        //  different final sizes.
                        userMaxSize = userSize;
                    }
                    else
                    {
                        userMaxSize = definitions[i].UserMaxSize;
                    }

                    definitions[i].SizeCache = Math.Max(definitions[i].MinSizeForArrange, Math.Min(userSize, userMaxSize));
                    if (useLayoutRounding)
                    {
                        roundingErrors[i] = definitions[i].SizeCache;
                        definitions[i].SizeCache = UIElement.RoundLayoutValue(definitions[i].SizeCache, dpi);
                    }

                    allPreferredArrangeSize += definitions[i].SizeCache;
                    definitionIndices[--nonStarIndex] = i;
                }
            }

            //  indices should meet
            Debug.Assert(nonStarIndex == starDefinitionsCount);

            if (starDefinitionsCount > 0)
            {
                StarDistributionOrderIndexComparer starDistributionOrderIndexComparer = new StarDistributionOrderIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, starDefinitionsCount, starDistributionOrderIndexComparer);

                //  the 'do {} while' loop below calculates sum of star weights in order to avoid fp overflow...
                //  partial sum value is stored in each definition's SizeCache member.
                //  this way the algorithm guarantees (starValue <= definition.SizeCache) and thus
                //  (starValue / definition.SizeCache) will never overflow due to sum of star weights becoming zero.
                //  this is an important change from previous implementation where the following was possible:
                //  ((BigValueStar + SmallValueStar) - BigValueStar) resulting in 0...
                double allStarWeights = 0;
                int i = starDefinitionsCount - 1;
                do
                {
                    allStarWeights += definitions[definitionIndices[i]].MeasureSize;
                    definitions[definitionIndices[i]].SizeCache = allStarWeights;
                } while (--i >= 0);

                i = 0;
                do
                {
                    double resolvedSize;
                    double starValue = definitions[definitionIndices[i]].MeasureSize;

                    if (_IsZero(starValue))
                    {
                        resolvedSize = definitions[definitionIndices[i]].MinSizeForArrange;
                    }
                    else
                    {
                        double userSize = Math.Max(finalSize - allPreferredArrangeSize, 0.0) * (starValue / definitions[definitionIndices[i]].SizeCache);
                        resolvedSize = Math.Min(userSize, definitions[definitionIndices[i]].UserMaxSize);
                        resolvedSize = Math.Max(definitions[definitionIndices[i]].MinSizeForArrange, resolvedSize);
                    }

                    definitions[definitionIndices[i]].SizeCache = resolvedSize;
                    if (useLayoutRounding)
                    {
                        roundingErrors[definitionIndices[i]] = definitions[definitionIndices[i]].SizeCache;
                        definitions[definitionIndices[i]].SizeCache = UIElement.RoundLayoutValue(definitions[definitionIndices[i]].SizeCache, dpi);
                    }

                    allPreferredArrangeSize += definitions[definitionIndices[i]].SizeCache;
                } while (++i < starDefinitionsCount);
            }

            if (    allPreferredArrangeSize > finalSize
                &&  !_AreClose(allPreferredArrangeSize, finalSize)  )
            {
                DistributionOrderIndexComparer distributionOrderIndexComparer = new DistributionOrderIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, definitions.Length, distributionOrderIndexComparer);
                double sizeToDistribute = finalSize - allPreferredArrangeSize;

                for (int i = 0; i < definitions.Length; ++i)
                {
                    int definitionIndex = definitionIndices[i];
                    double final = definitions[definitionIndex].SizeCache + (sizeToDistribute / (definitions.Length - i));
                    double finalOld = final;
                    final = Math.Max(final, definitions[definitionIndex].MinSizeForArrange);
                    final = Math.Min(final, definitions[definitionIndex].SizeCache);

                    if (useLayoutRounding)
                    {
                        roundingErrors[definitionIndex] = final;
                        final = UIElement.RoundLayoutValue(finalOld, dpi);
                        final = Math.Max(final, definitions[definitionIndex].MinSizeForArrange);
                        final = Math.Min(final, definitions[definitionIndex].SizeCache);
                    }

                    sizeToDistribute -= (final - definitions[definitionIndex].SizeCache);
                    definitions[definitionIndex].SizeCache = final;
                }

                allPreferredArrangeSize = finalSize - sizeToDistribute;
            }

            if (useLayoutRounding)
            {
                if (!_AreClose(allPreferredArrangeSize, finalSize))
                {
                    // Compute deltas
                    for (int i = 0; i < definitions.Length; ++i)
                    {
                        roundingErrors[i] = roundingErrors[i] - definitions[i].SizeCache;
                        definitionIndices[i] = i;
                    }

                    // Sort rounding errors
                    RoundingErrorIndexComparer roundingErrorIndexComparer = new RoundingErrorIndexComparer(roundingErrors);
                    Array.Sort(definitionIndices, 0, definitions.Length, roundingErrorIndexComparer);
                    double adjustedSize = allPreferredArrangeSize;
                    double dpiIncrement = UIElement.RoundLayoutValue(1.0, dpi);

                    if (allPreferredArrangeSize > finalSize)
                    {
                        int i = definitions.Length - 1;
                        while ((adjustedSize > finalSize && !_AreClose(adjustedSize, finalSize)) && i >= 0)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache - dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final < definition.SizeCache)
                            {
                                adjustedSize -= dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i--;
                        }
                    }
                    else if (allPreferredArrangeSize < finalSize)
                    {
                        int i = 0;
                        while ((adjustedSize < finalSize && !_AreClose(adjustedSize, finalSize)) && i < definitions.Length)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache + dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final > definition.SizeCache)
                            {
                                adjustedSize += dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i++;
                        }
                    }
                }
            }

            definitions[0].FinalOffset = 0.0;
            for (int i = 0; i < definitions.Length; ++i)
            {
                definitions[(i + 1) % definitions.Length].FinalOffset = definitions[i].FinalOffset + definitions[i].SizeCache;
            }
        }

        // new implementation, as of 4.7.  This incorporates the same algorithm
        // as in ResolveStarMaxDiscrepancy.  It differs in the same way that SetFinalSizeLegacy
        // differs from ResolveStarLegacy, namely (a) leaves results in def.SizeCache
        // instead of def.MeasureSize, (b) implements LayoutRounding if requested,
        // (c) stores intermediate results differently.
        // The LayoutRounding logic is improved:
        // 1. Use pre-rounded values during proportional allocation.  This avoids the
        //      same kind of problems arising from interaction with min/max that
        //      motivated the new algorithm in the first place.
        // 2. Use correct "nudge" amount when distributing roundoff space.   This
        //      comes into play at high DPI - greater than 134.
        // 3. Applies rounding only to real pixel values (not to ratios)
        private void SetFinalSizeMaxDiscrepancy(
            DefinitionBase[] definitions,
            double finalSize,
            bool columns)
        {
            int defCount = definitions.Length;
            int[] definitionIndices = DefinitionIndices;
            int minCount = 0, maxCount = 0;
            double takenSize = 0.0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;   // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i=0; i<defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (Double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > Double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(Double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }


            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3=true; runPhase2and3; )
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of definitionIndices,
                // the "max" list in the second half.  DefinitionIndices has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i=0; i<defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    if (def.UserSize.IsStar)
                    {
                        Debug.Assert(!def.IsShared, "*-defs cannot be shared");

                        if (def.MeasureSize < 0.0)
                        {
                            takenSize += -def.MeasureSize;  // already resolved
                        }
                        else
                        {
                            double starWeight = StarWeight(def, scale);
                            totalStarWeight += starWeight;

                            if (def.MinSizeForArrange > 0.0)
                            {
                                // store ratio w/min in MeasureSize (for now)
                                definitionIndices[minCount++] = i;
                                def.MeasureSize = starWeight / def.MinSizeForArrange;
                            }

                            double effectiveMaxSize = Math.Max(def.MinSizeForArrange, def.UserMaxSize);
                            if (!Double.IsPositiveInfinity(effectiveMaxSize))
                            {
                                // store ratio w/max in SizeCache (for now)
                                definitionIndices[defCount + maxCount++] = i;
                                def.SizeCache = starWeight / effectiveMaxSize;
                            }
                        }
                    }
                    else
                    {
                        double userSize = 0;

                        switch (def.UserSize.GridUnitType)
                        {
                            case (GridUnitType.Pixel):
                                userSize = def.UserSize.Value;
                                break;

                            case (GridUnitType.Auto):
                                userSize = def.MinSizeForArrange;
                                break;
                        }

                        double userMaxSize;

                        if (def.IsShared)
                        {
                            //  overriding userMaxSize effectively prevents squishy-ness.
                            //  this is a "solution" to avoid shared definitions from been sized to
                            //  different final size at arrange time, if / when different grids receive
                            //  different final sizes.
                            userMaxSize = userSize;
                        }
                        else
                        {
                            userMaxSize = def.UserMaxSize;
                        }

                        def.SizeCache = Math.Max(def.MinSizeForArrange, Math.Min(userSize, userMaxSize));
                        takenSize += def.SizeCache;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = finalSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;

                MinRatioIndexComparer minRatioIndexComparer = new MinRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, minCount, minRatioIndexComparer);
                MaxRatioIndexComparer maxRatioIndexComparer = new MaxRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, defCount, maxCount, maxRatioIndexComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.UserSize.IsStar && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? definitions[definitionIndices[minCount - 1]].MeasureSize : Double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? definitions[definitionIndices[defCount + maxCount - 1]].SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!(chooseMin.HasValue))
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    int resolvedIndex;
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedIndex = definitionIndices[minCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = resolvedDef.MinSizeForArrange;
                        --minCount;
                    }
                    else
                    {
                        resolvedIndex = definitionIndices[defCount + maxCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = Math.Max(resolvedDef.MinSizeForArrange, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = finalSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && definitions[definitionIndices[minCount - 1]].MeasureSize < 0.0)
                    {
                        --minCount;
                        definitionIndices[minCount] = -1;
                    }
                    while (maxCount > 0 && definitions[definitionIndices[defCount + maxCount - 1]].MeasureSize < 0.0)
                    {
                        --maxCount;
                        definitionIndices[defCount + maxCount] = -1;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < finalSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        if (definitionIndices[i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > finalSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        if (definitionIndices[defCount + i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[defCount + i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i=0; i<defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its size
                        def.SizeCache = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        definitionIndices[starCount++] = i;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                StarWeightIndexComparer starWeightIndexComparer = new StarWeightIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, starCount, starWeightIndexComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight.
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(finalSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSizeForArrange, resolvedSize);

                    // Use the raw (unrounded) sizes to update takenSize, so that
                    // proportions are computed in the same terms as in phase 3;
                    // this avoids errors arising from min/max constraints.
                    takenSize += resolvedSize;
                    def.SizeCache = resolvedSize;
                }
            }

            // Phase 5.  Apply layout rounding.  We do this after fully allocating
            // unrounded sizes, to avoid breaking assumptions in the previous phases
            if (UseLayoutRounding)
            {
                DpiScale dpiScale = GetDpi();
                double dpi = columns ? dpiScale.DpiScaleX : dpiScale.DpiScaleY;
                double[] roundingErrors = RoundingErrors;
                double roundedTakenSize = 0.0;

                // round each of the allocated sizes, keeping track of the deltas
                for (int i = 0; i < definitions.Length; ++i)
                {
                    DefinitionBase def = definitions[i];
                    double roundedSize = UIElement.RoundLayoutValue(def.SizeCache, dpi);
                    roundingErrors[i] = (roundedSize - def.SizeCache);
                    def.SizeCache = roundedSize;
                    roundedTakenSize += roundedSize;
                }

                // The total allocation might differ from finalSize due to rounding
                // effects.  Tweak the allocations accordingly.

                // Theoretical and historical note.  The problem at hand - allocating
                // space to columns (or rows) with *-weights, min and max constraints,
                // and layout rounding - has a long history.  Especially the special
                // case of 50 columns with min=1 and available space=435 - allocating
                // seats in the U.S. House of Representatives to the 50 states in
                // proportion to their population.  There are numerous algorithms
                // and papers dating back to the 1700's, including the book:
                // Balinski, M. and H. Young, Fair Representation, Yale University Press, New Haven, 1982.
                //
                // One surprising result of all this research is that *any* algorithm
                // will suffer from one or more undesirable features such as the
                // "population paradox" or the "Alabama paradox", where (to use our terminology)
                // increasing the available space by one pixel might actually decrease
                // the space allocated to a given column, or increasing the weight of
                // a column might decrease its allocation.   This is worth knowing
                // in case someone complains about this behavior;  it's not a bug so
                // much as something inherent to the problem.  Cite the book mentioned
                // above or one of the 100s of references, and resolve as WontFix.
                //
                // Fortunately, our scenarios tend to have a small number of columns (~10 or fewer)
                // each being allocated a large number of pixels (~50 or greater), and
                // people don't even notice the kind of 1-pixel anomolies that are
                // theoretically inevitable, or don't care if they do.  At least they shouldn't
                // care - no one should be using the results WPF's grid layout to make
                // quantitative decisions; its job is to produce a reasonable display, not
                // to allocate seats in Congress.
                //
                // Our algorithm is more susceptible to paradox than the one currently
                // used for Congressional allocation ("Huntington-Hill" algorithm), but
                // it is faster to run:  O(N log N) vs. O(S * N), where N=number of
                // definitions, S = number of available pixels.  And it produces
                // adequate results in practice, as mentioned above.
                //
                // To reiterate one point:  all this only applies when layout rounding
                // is in effect.  When fractional sizes are allowed, the algorithm
                // behaves as well as possible, subject to the min/max constraints
                // and precision of floating-point computation.  (However, the resulting
                // display is subject to anti-aliasing problems.   TANSTAAFL.)

                if (!_AreClose(roundedTakenSize, finalSize))
                {
                    // Compute deltas
                    for (int i = 0; i < definitions.Length; ++i)
                    {
                        definitionIndices[i] = i;
                    }

                    // Sort rounding errors
                    RoundingErrorIndexComparer roundingErrorIndexComparer = new RoundingErrorIndexComparer(roundingErrors);
                    Array.Sort(definitionIndices, 0, definitions.Length, roundingErrorIndexComparer);
                    double adjustedSize = roundedTakenSize;
                    double dpiIncrement = 1.0/dpi;

                    if (roundedTakenSize > finalSize)
                    {
                        int i = definitions.Length - 1;
                        while ((adjustedSize > finalSize && !_AreClose(adjustedSize, finalSize)) && i >= 0)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache - dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final < definition.SizeCache)
                            {
                                adjustedSize -= dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i--;
                        }
                    }
                    else if (roundedTakenSize < finalSize)
                    {
                        int i = 0;
                        while ((adjustedSize < finalSize && !_AreClose(adjustedSize, finalSize)) && i < definitions.Length)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache + dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final > definition.SizeCache)
                            {
                                adjustedSize += dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i++;
                        }
                    }
                }
            }

            // Phase 6.  Compute final offsets
            definitions[0].FinalOffset = 0.0;
            for (int i = 0; i < definitions.Length; ++i)
            {
                definitions[(i + 1) % definitions.Length].FinalOffset = definitions[i].FinalOffset + definitions[i].SizeCache;
            }
        }

        /// <summary>
        /// Choose the ratio with maximum discrepancy from the current proportion.
        /// Returns:
        ///     true    if proportion fails a min constraint but not a max, or
        ///                 if the min constraint has higher discrepancy
        ///     false   if proportion fails a max constraint but not a min, or
        ///                 if the max constraint has higher discrepancy
        ///     null    if proportion doesn't fail a min or max constraint
        /// The discrepancy is the ratio of the proportion to the max- or min-ratio.
        /// When both ratios hit the constraint,  minRatio < proportion < maxRatio,
        /// and the minRatio has higher discrepancy if
        ///         (proportion / minRatio) > (maxRatio / proportion)
        /// </summary>
        private static bool? Choose(double minRatio, double maxRatio, double proportion)
        {
            if (minRatio < proportion)
            {
                if (maxRatio > proportion)
                {
                    // compare proportion/minRatio : maxRatio/proportion, but
                    // do it carefully to avoid floating-point overflow or underflow
                    // and divide-by-0.
                    double minPower = Math.Floor(Math.Log(minRatio, 2.0));
                    double maxPower = Math.Floor(Math.Log(maxRatio, 2.0));
                    double f = Math.Pow(2.0, Math.Floor((minPower + maxPower) / 2.0));
                    if ((proportion / f) * (proportion / f) > (minRatio / f) * (maxRatio / f))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (maxRatio > proportion)
            {
                return false;
            }

            return null;
        }

        /// <summary>
        /// Sorts row/column indices by rounding error if layout rounding is applied.
        /// </summary>
        /// <param name="x">Index, rounding error pair</param>
        /// <param name="y">Index, rounding error pair</param>
        /// <returns>1 if x.Value > y.Value, 0 if equal, -1 otherwise</returns>
        private static int CompareRoundingErrors(KeyValuePair<int, double> x, KeyValuePair<int, double> y)
        {
            if (x.Value < y.Value)
            {
                return -1;
            }
            else if (x.Value > y.Value)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Calculates final (aka arrange) size for given range.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <returns>Final size.</returns>
        private double GetFinalSizeForRange(
            DefinitionBase[] definitions,
            int start,
            int count)
        {
            double size = 0;
            int i = start + count - 1;

            do
            {
                size += definitions[i].SizeCache;
            } while (--i >= start);

            return (size);
        }

        /// <summary>
        /// Clears dirty state for the grid and its columns / rows
        /// </summary>
        private void SetValid()
        {
            ExtendedData extData = ExtData;
            if (extData != null)
            {
//                for (int i = 0; i < PrivateColumnCount; ++i) DefinitionsU[i].SetValid ();
//                for (int i = 0; i < PrivateRowCount; ++i) DefinitionsV[i].SetValid ();

                if (extData.TempDefinitions != null)
                {
                    //  TempDefinitions has to be cleared to avoid "memory leaks"
                    Array.Clear(extData.TempDefinitions, 0, Math.Max(DefinitionsU.Length, DefinitionsV.Length));
                    extData.TempDefinitions = null;
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if ColumnDefinitions collection is not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeColumnDefinitions()
        {
            ExtendedData extData = ExtData;
            return (    extData != null
                    &&  extData.ColumnDefinitions != null
                    &&  extData.ColumnDefinitions.Count > 0   );
        }

        /// <summary>
        /// Returns <c>true</c> if RowDefinitions collection is not empty
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeRowDefinitions()
        {
            ExtendedData extData = ExtData;
            return (    extData != null
                    &&  extData.RowDefinitions != null
                    &&  extData.RowDefinitions.Count > 0  );
        }

        /// <summary>
        /// Synchronized ShowGridLines property with the state of the grid's visual collection
        /// by adding / removing GridLinesRenderer visual.
        /// Returns a reference to GridLinesRenderer visual or null.
        /// </summary>
        private GridLinesRenderer EnsureGridLinesRenderer()
        {
            //
            //  synchronize the state
            //
            if (ShowGridLines && (_gridLinesRenderer == null))
            {
                _gridLinesRenderer = new GridLinesRenderer();
                this.AddVisualChild(_gridLinesRenderer);
            }

            if ((!ShowGridLines) && (_gridLinesRenderer != null))
            {
                this.RemoveVisualChild(_gridLinesRenderer);
                _gridLinesRenderer = null;
            }

            return (_gridLinesRenderer);
        }

        /// <summary>
        /// SetFlags is used to set or unset one or multiple
        /// flags on the object.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlagsAnd returns <c>true</c> if all the flags in the
        /// given bitmask are set on the object.
        /// </summary>
        private bool CheckFlagsAnd(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        /// <summary>
        /// CheckFlagsOr returns <c>true</c> if at least one flag in the
        /// given bitmask is set.
        /// </summary>
        /// <remarks>
        /// If no bits are set in the given bitmask, the method returns
        /// <c>true</c>.
        /// </remarks>
        private bool CheckFlagsOr(Flags flags)
        {
            return (flags == 0 || (_flags & flags) != 0);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnShowGridLinesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;

            if (    grid.ExtData != null    // trivial grid is 1 by 1. there is no grid lines anyway
                &&  grid.ListenToNotifications)
            {
                grid.InvalidateVisual();
            }

            grid.SetFlags((bool) e.NewValue, Flags.ShowGridLinesPropertyValue);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnCellAttachedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Visual child = d as Visual;

            if (child != null)
            {
                Grid grid = VisualTreeHelper.GetParent(child) as Grid;
                if (    grid != null
                    &&  grid.ExtData != null
                    &&  grid.ListenToNotifications  )
                {
                    grid.CellsStructureDirty = true;
                    grid.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool IsIntValueNotNegative(object value)
        {
            return ((int)value >= 0);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool IsIntValueGreaterThanZero(object value)
        {
            return ((int)value > 0);
        }

        /// <summary>
        /// Helper for Comparer methods.
        /// </summary>
        /// <returns>
        /// true iff one or both of x and y are null, in which case result holds
        /// the relative sort order.
        /// </returns>
        private static bool CompareNullRefs(object x, object y, out int result)
        {
            result = 2;

            if (x == null)
            {
                if (y == null)
                {
                    result = 0;
                }
                else
                {
                    result = -1;
                }
            }
            else
            {
                if (y == null)
                {
                    result = 1;
                }
            }

            return (result != 2);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Private version returning array of column definitions.
        /// </summary>
        private DefinitionBase[] DefinitionsU
        {
            get { return (ExtData.DefinitionsU); }
        }

        /// <summary>
        /// Private version returning array of row definitions.
        /// </summary>
        private DefinitionBase[] DefinitionsV
        {
            get { return (ExtData.DefinitionsV); }
        }

        /// <summary>
        /// Helper accessor to layout time array of definitions.
        /// </summary>
        private DefinitionBase[] TempDefinitions
        {
            get
            {
                ExtendedData extData = ExtData;
                int requiredLength = Math.Max(DefinitionsU.Length, DefinitionsV.Length) * 2;

                if (    extData.TempDefinitions == null
                    ||  extData.TempDefinitions.Length < requiredLength   )
                {
                    WeakReference tempDefinitionsWeakRef = (WeakReference)Thread.GetData(s_tempDefinitionsDataSlot);
                    if (tempDefinitionsWeakRef == null)
                    {
                        extData.TempDefinitions = new DefinitionBase[requiredLength];
                        Thread.SetData(s_tempDefinitionsDataSlot, new WeakReference(extData.TempDefinitions));
                    }
                    else
                    {
                        extData.TempDefinitions = (DefinitionBase[])tempDefinitionsWeakRef.Target;
                        if (    extData.TempDefinitions == null
                            ||  extData.TempDefinitions.Length < requiredLength   )
                        {
                            extData.TempDefinitions = new DefinitionBase[requiredLength];
                            tempDefinitionsWeakRef.Target = extData.TempDefinitions;
                        }
                    }
                }
                return (extData.TempDefinitions);
            }
        }

        /// <summary>
        /// Helper accessor to definition indices.
        /// </summary>
        private int[] DefinitionIndices
        {
            get
            {
                int requiredLength = Math.Max(Math.Max(DefinitionsU.Length, DefinitionsV.Length), 1) * 2;

                if (_definitionIndices == null || _definitionIndices.Length < requiredLength)
                {
                    _definitionIndices = new int[requiredLength];
                }

                return _definitionIndices;
            }
        }

        /// <summary>
        /// Helper accessor to rounding errors.
        /// </summary>
        private double[] RoundingErrors
        {
            get
            {
                int requiredLength = Math.Max(DefinitionsU.Length, DefinitionsV.Length);

                if (_roundingErrors == null && requiredLength == 0)
                {
                    _roundingErrors = new double[1];
                }
                else if (_roundingErrors == null || _roundingErrors.Length < requiredLength)
                {
                    _roundingErrors = new double[requiredLength];
                }
                return _roundingErrors;
            }
        }

        /// <summary>
        /// Private version returning array of cells.
        /// </summary>
        private CellCache[] PrivateCells
        {
            get { return (ExtData.CellCachesCollection); }
        }

        /// <summary>
        /// Convenience accessor to ValidCellsStructure bit flag.
        /// </summary>
        private bool CellsStructureDirty
        {
            get { return (!CheckFlagsAnd(Flags.ValidCellsStructure)); }
            set { SetFlags(!value, Flags.ValidCellsStructure); }
        }

        /// <summary>
        /// Convenience accessor to ListenToNotifications bit flag.
        /// </summary>
        private bool ListenToNotifications
        {
            get { return (CheckFlagsAnd(Flags.ListenToNotifications)); }
            set { SetFlags(value, Flags.ListenToNotifications); }
        }

        /// <summary>
        /// Convenience accessor to SizeToContentU bit flag.
        /// </summary>
        private bool SizeToContentU
        {
            get { return (CheckFlagsAnd(Flags.SizeToContentU)); }
            set { SetFlags(value, Flags.SizeToContentU); }
        }

        /// <summary>
        /// Convenience accessor to SizeToContentV bit flag.
        /// </summary>
        private bool SizeToContentV
        {
            get { return (CheckFlagsAnd(Flags.SizeToContentV)); }
            set { SetFlags(value, Flags.SizeToContentV); }
        }

        /// <summary>
        /// Convenience accessor to HasStarCellsU bit flag.
        /// </summary>
        private bool HasStarCellsU
        {
            get { return (CheckFlagsAnd(Flags.HasStarCellsU)); }
            set { SetFlags(value, Flags.HasStarCellsU); }
        }

        /// <summary>
        /// Convenience accessor to HasStarCellsV bit flag.
        /// </summary>
        private bool HasStarCellsV
        {
            get { return (CheckFlagsAnd(Flags.HasStarCellsV)); }
            set { SetFlags(value, Flags.HasStarCellsV); }
        }

        /// <summary>
        /// Convenience accessor to HasGroup3CellsInAutoRows bit flag.
        /// </summary>
        private bool HasGroup3CellsInAutoRows
        {
            get { return (CheckFlagsAnd(Flags.HasGroup3CellsInAutoRows)); }
            set { SetFlags(value, Flags.HasGroup3CellsInAutoRows); }
        }

        /// <summary>
        /// fp version of <c>d == 0</c>.
        /// </summary>
        /// <param name="d">Value to check.</param>
        /// <returns><c>true</c> if d == 0.</returns>
        private static bool _IsZero(double d)
        {
            return (Math.Abs(d) < c_epsilon);
        }

        /// <summary>
        /// fp version of <c>d1 == d2</c>
        /// </summary>
        /// <param name="d1">First value to compare</param>
        /// <param name="d2">Second value to compare</param>
        /// <returns><c>true</c> if d1 == d2</returns>
        private static bool _AreClose(double d1, double d2)
        {
            return (Math.Abs(d1 - d2) < c_epsilon);
        }

        /// <summary>
        /// Returns reference to extended data bag.
        /// </summary>
        private ExtendedData ExtData
        {
            get { return (_data); }
        }

        /// <summary>
        /// Returns *-weight, adjusted for scale computed during Phase 1
        /// </summary>
        static double StarWeight(DefinitionBase def, double scale)
        {
            if (scale < 0.0)
            {
                // if one of the *-weights is Infinity, adjust the weights by mapping
                // Infinty to 1.0 and everything else to 0.0:  the infinite items share the
                // available space equally, everyone else gets nothing.
                return (Double.IsPositiveInfinity(def.UserSize.Value)) ? 1.0 : 0.0;
            }
            else
            {
                return def.UserSize.Value * scale;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private ExtendedData _data;                             //  extended data instantiated on demand, for non-trivial case handling only
        private Flags _flags;                                   //  grid validity / property caches dirtiness flags
        private GridLinesRenderer _gridLinesRenderer;

        // Keeps track of definition indices.
        int[] _definitionIndices;

        // Stores unrounded values and rounding errors during layout rounding.
        double[] _roundingErrors;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Static Fields
        //
        //------------------------------------------------------

        #region Static Fields
        private const double c_epsilon = 1e-5;                  //  used in fp calculations
        private const double c_starClip = 1e298;                //  used as maximum for clipping star values during normalization
        private const int c_layoutLoopMaxCount = 5;             // 5 is an arbitrary constant chosen to end the measure loop
        private static readonly LocalDataStoreSlot s_tempDefinitionsDataSlot = Thread.AllocateDataSlot();
        private static readonly IComparer s_spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
        private static readonly IComparer s_spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();
        private static readonly IComparer s_starDistributionOrderComparer = new StarDistributionOrderComparer();
        private static readonly IComparer s_distributionOrderComparer = new DistributionOrderComparer();
        private static readonly IComparer s_minRatioComparer = new MinRatioComparer();
        private static readonly IComparer s_maxRatioComparer = new MaxRatioComparer();
        private static readonly IComparer s_starWeightComparer = new StarWeightComparer();

        #endregion Static Fields

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        /// <summary>
        /// Extended data instantiated on demand, when grid handles non-trivial case.
        /// </summary>
        private class ExtendedData
        {
            internal ColumnDefinitionCollection ColumnDefinitions;  //  collection of column definitions (logical tree support)
            internal RowDefinitionCollection RowDefinitions;        //  collection of row definitions (logical tree support)
            internal DefinitionBase[] DefinitionsU;                 //  collection of column definitions used during calc
            internal DefinitionBase[] DefinitionsV;                 //  collection of row definitions used during calc
            internal CellCache[] CellCachesCollection;              //  backing store for logical children
            internal int CellGroup1;                                //  index of the first cell in first cell group
            internal int CellGroup2;                                //  index of the first cell in second cell group
            internal int CellGroup3;                                //  index of the first cell in third cell group
            internal int CellGroup4;                                //  index of the first cell in forth cell group
            internal DefinitionBase[] TempDefinitions;              //  temporary array used during layout for various purposes
                                                                    //  TempDefinitions.Length == Max(definitionsU.Length, definitionsV.Length)
        }

        /// <summary>
        /// Grid validity / property caches dirtiness flags
        /// </summary>
        [System.Flags]
        private enum Flags
        {
            //
            //  the foolowing flags let grid tracking dirtiness in more granular manner:
            //  * Valid???Structure flags indicate that elements were added or removed.
            //  * Valid???Layout flags indicate that layout time portion of the information
            //    stored on the objects should be updated.
            //
            ValidDefinitionsUStructure              = 0x00000001,
            ValidDefinitionsVStructure              = 0x00000002,
            ValidCellsStructure                     = 0x00000004,

            //
            //  boolean properties state
            //
            ShowGridLinesPropertyValue              = 0x00000100,   //  show grid lines ?

            //
            //  boolean flags
            //
            ListenToNotifications                   = 0x00001000,   //  "0" when all notifications are ignored
            SizeToContentU                          = 0x00002000,   //  "1" if calculating to content in U direction
            SizeToContentV                          = 0x00004000,   //  "1" if calculating to content in V direction
            HasStarCellsU                           = 0x00008000,   //  "1" if at least one cell belongs to a Star column
            HasStarCellsV                           = 0x00010000,   //  "1" if at least one cell belongs to a Star row
            HasGroup3CellsInAutoRows                = 0x00020000,   //  "1" if at least one cell of group 3 belongs to an Auto row
            MeasureOverrideInProgress               = 0x00040000,   //  "1" while in the context of Grid.MeasureOverride
            ArrangeOverrideInProgress               = 0x00080000,   //  "1" while in the context of Grid.ArrangeOverride
        }

        #endregion Private Structures Classes

        //------------------------------------------------------
        //
        //  Properties
        //
        //------------------------------------------------------

        #region Properties

        /// <summary>
        /// ShowGridLines property. This property is used mostly
        /// for simplification of visual debuggig. When it is set
        /// to <c>true</c> grid lines are drawn to visualize location
        /// of grid lines.
        /// </summary>
        public static readonly DependencyProperty ShowGridLinesProperty =
                DependencyProperty.Register(
                      "ShowGridLines",
                      typeof(bool),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              false,
                              new PropertyChangedCallback(OnShowGridLinesPropertyChanged)));

        /// <summary>
        /// Column property. This is an attached property.
        /// Grid defines Column property, so that it can be set
        /// on any element treated as a cell. Column property
        /// specifies child's position with respect to columns.
        /// </summary>
        /// <remarks>
        /// <para> Columns are 0 - based. In order to appear in first column, element
        /// should have Column property set to <c>0</c>. </para>
        /// <para> Default value for the property is <c>0</c>. </para>
        /// </remarks>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ColumnProperty =
                DependencyProperty.RegisterAttached(
                      "Column",
                      typeof(int),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              0,
                              new PropertyChangedCallback(OnCellAttachedPropertyChanged)),
                      new ValidateValueCallback(IsIntValueNotNegative));

        /// <summary>
        /// Row property. This is an attached property.
        /// Grid defines Row, so that it can be set
        /// on any element treated as a cell. Row property
        /// specifies child's position with respect to rows.
        /// <remarks>
        /// <para> Rows are 0 - based. In order to appear in first row, element
        /// should have Row property set to <c>0</c>. </para>
        /// <para> Default value for the property is <c>0</c>. </para>
        /// </remarks>
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty RowProperty =
                DependencyProperty.RegisterAttached(
                      "Row",
                      typeof(int),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              0,
                              new PropertyChangedCallback(OnCellAttachedPropertyChanged)),
                      new ValidateValueCallback(IsIntValueNotNegative));

        /// <summary>
        /// ColumnSpan property. This is an attached property.
        /// Grid defines ColumnSpan, so that it can be set
        /// on any element treated as a cell. ColumnSpan property
        /// specifies child's width with respect to columns.
        /// Example, ColumnSpan == 2 means that child will span across two columns.
        /// </summary>
        /// <remarks>
        /// Default value for the property is <c>1</c>.
        /// </remarks>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ColumnSpanProperty =
                DependencyProperty.RegisterAttached(
                      "ColumnSpan",
                      typeof(int),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              1,
                              new PropertyChangedCallback(OnCellAttachedPropertyChanged)),
                      new ValidateValueCallback(IsIntValueGreaterThanZero));

        /// <summary>
        /// RowSpan property. This is an attached property.
        /// Grid defines RowSpan, so that it can be set
        /// on any element treated as a cell. RowSpan property
        /// specifies child's height with respect to row grid lines.
        /// Example, RowSpan == 3 means that child will span across three rows.
        /// </summary>
        /// <remarks>
        /// Default value for the property is <c>1</c>.
        /// </remarks>
        [CommonDependencyProperty]
        public static readonly DependencyProperty RowSpanProperty =
                DependencyProperty.RegisterAttached(
                      "RowSpan",
                      typeof(int),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              1,
                              new PropertyChangedCallback(OnCellAttachedPropertyChanged)),
                      new ValidateValueCallback(IsIntValueGreaterThanZero));


        /// <summary>
        /// IsSharedSizeScope property marks scoping element for shared size.
        /// </summary>
        public static readonly DependencyProperty IsSharedSizeScopeProperty  =
                DependencyProperty.RegisterAttached(
                      "IsSharedSizeScope",
                      typeof(bool),
                      typeof(Grid),
                      new FrameworkPropertyMetadata(
                              false,
                              new PropertyChangedCallback(DefinitionBase.OnIsSharedSizeScopePropertyChanged)));

        #endregion Properties

        //------------------------------------------------------
        //
        //  Internal Structures / Classes
        //
        //------------------------------------------------------

        #region Internal Structures Classes

        /// <summary>
        /// LayoutTimeSizeType is used internally and reflects layout-time size type.
        /// </summary>
        [System.Flags]
        internal enum LayoutTimeSizeType : byte
        {
            None        = 0x00,
            Pixel       = 0x01,
            Auto        = 0x02,
            Star        = 0x04,
        }

        #endregion Internal Structures Classes

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        /// <summary>
        /// CellCache stored calculated values of
        /// 1. attached cell positioning properties;
        /// 2. size type;
        /// 3. index of a next cell in the group;
        /// </summary>
        private struct CellCache
        {
            internal int ColumnIndex;
            internal int RowIndex;
            internal int ColumnSpan;
            internal int RowSpan;
            internal LayoutTimeSizeType SizeTypeU;
            internal LayoutTimeSizeType SizeTypeV;
            internal int Next;
            internal bool IsStarU { get { return ((SizeTypeU & LayoutTimeSizeType.Star) != 0); } }
            internal bool IsAutoU { get { return ((SizeTypeU & LayoutTimeSizeType.Auto) != 0); } }
            internal bool IsStarV { get { return ((SizeTypeV & LayoutTimeSizeType.Star) != 0); } }
            internal bool IsAutoV { get { return ((SizeTypeV & LayoutTimeSizeType.Auto) != 0); } }
        }

        /// <summary>
        /// Helper class for representing a key for a span in hashtable.
        /// </summary>
        private class SpanKey
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="start">Starting index of the span.</param>
            /// <param name="count">Span count.</param>
            /// <param name="u"><c>true</c> for columns; <c>false</c> for rows.</param>
            internal SpanKey(int start, int count, bool u)
            {
                _start = start;
                _count = count;
                _u = u;
            }

            /// <summary>
            /// <see cref="object.GetHashCode"/>
            /// </summary>
            public override int GetHashCode()
            {
                int hash = (_start ^ (_count << 2));

                if (_u) hash &= 0x7ffffff;
                else    hash |= 0x8000000;

                return (hash);
            }

            /// <summary>
            /// <see cref="object.Equals(object)"/>
            /// </summary>
            public override bool Equals(object obj)
            {
                SpanKey sk = obj as SpanKey;
                return (    sk != null
                        &&  sk._start == _start
                        &&  sk._count == _count
                        &&  sk._u == _u );
            }

            /// <summary>
            /// Returns start index of the span.
            /// </summary>
            internal int Start { get { return (_start); } }

            /// <summary>
            /// Returns span count.
            /// </summary>
            internal int Count { get { return (_count); } }

            /// <summary>
            /// Returns <c>true</c> if this is a column span.
            /// <c>false</c> if this is a row span.
            /// </summary>
            internal bool U { get { return (_u); } }

            private int _start;
            private int _count;
            private bool _u;
        }

        /// <summary>
        /// SpanPreferredDistributionOrderComparer.
        /// </summary>
        private class SpanPreferredDistributionOrderComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    if (definitionX.UserSize.IsAuto)
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = definitionX.MinSize.CompareTo(definitionY.MinSize);
                        }
                        else
                        {
                            result = -1;
                        }
                    }
                    else
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = +1;
                        }
                        else
                        {
                            result = definitionX.PreferredSize.CompareTo(definitionY.PreferredSize);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// SpanMaxDistributionOrderComparer.
        /// </summary>
        private class SpanMaxDistributionOrderComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    if (definitionX.UserSize.IsAuto)
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                        }
                        else
                        {
                            result = +1;
                        }
                    }
                    else
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = -1;
                        }
                        else
                        {
                            result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// StarDistributionOrderComparer.
        /// </summary>
        private class StarDistributionOrderComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// DistributionOrderComparer.
        /// </summary>
        private class DistributionOrderComparer: IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    double xprime = definitionX.SizeCache - definitionX.MinSizeForArrange;
                    double yprime = definitionY.SizeCache - definitionY.MinSizeForArrange;
                    result = xprime.CompareTo(yprime);
                }

                return result;
            }
        }


        /// <summary>
        /// StarDistributionOrderIndexComparer.
        /// </summary>
        private class StarDistributionOrderIndexComparer : IComparer
        {
            private readonly DefinitionBase[] definitions;

            internal StarDistributionOrderIndexComparer(DefinitionBase[] definitions)
            {
                Invariant.Assert(definitions != null);
                this.definitions = definitions;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase definitionX = null;
                DefinitionBase definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// DistributionOrderComparer.
        /// </summary>
        private class DistributionOrderIndexComparer : IComparer
        {
            private readonly DefinitionBase[] definitions;

            internal DistributionOrderIndexComparer(DefinitionBase[] definitions)
            {
                Invariant.Assert(definitions != null);
                this.definitions = definitions;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase definitionX = null;
                DefinitionBase definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    double xprime = definitionX.SizeCache - definitionX.MinSizeForArrange;
                    double yprime = definitionY.SizeCache - definitionY.MinSizeForArrange;
                    result = xprime.CompareTo(yprime);
                }

                return result;
            }
        }

        /// <summary>
        /// RoundingErrorIndexComparer.
        /// </summary>
        private class RoundingErrorIndexComparer : IComparer
        {
            private readonly double[] errors;

            internal RoundingErrorIndexComparer(double[] errors)
            {
                Invariant.Assert(errors != null);
                this.errors = errors;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                int result;

                if (!CompareNullRefs(indexX, indexY, out result))
                {
                    double errorX = errors[indexX.Value];
                    double errorY = errors[indexY.Value];
                    result = errorX.CompareTo(errorY);
                }

                return result;
            }
        }

        /// <summary>
        /// MinRatioComparer.
        /// Sort by w/min (stored in MeasureSize), descending.
        /// We query the list from the back, i.e. in ascending order of w/min.
        /// </summary>
        private class MinRatioComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionY, definitionX, out result))
                {
                    result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioComparer.
        /// Sort by w/max (stored in SizeCache), ascending.
        /// We query the list from the back, i.e. in descending order of w/max.
        /// </summary>
        private class MaxRatioComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// StarWeightComparer.
        /// Sort by *-weight (stored in MeasureSize), ascending.
        /// </summary>
        private class StarWeightComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                DefinitionBase definitionX = x as DefinitionBase;
                DefinitionBase definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.MeasureSize.CompareTo(definitionY.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MinRatioIndexComparer.
        /// </summary>
        private class MinRatioIndexComparer : IComparer
        {
            private readonly DefinitionBase[] definitions;

            internal MinRatioIndexComparer(DefinitionBase[] definitions)
            {
                Invariant.Assert(definitions != null);
                this.definitions = definitions;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase definitionX = null;
                DefinitionBase definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionY, definitionX, out result))
                {
                    result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioIndexComparer.
        /// </summary>
        private class MaxRatioIndexComparer : IComparer
        {
            private readonly DefinitionBase[] definitions;

            internal MaxRatioIndexComparer(DefinitionBase[] definitions)
            {
                Invariant.Assert(definitions != null);
                this.definitions = definitions;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase definitionX = null;
                DefinitionBase definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioIndexComparer.
        /// </summary>
        private class StarWeightIndexComparer : IComparer
        {
            private readonly DefinitionBase[] definitions;

            internal StarWeightIndexComparer(DefinitionBase[] definitions)
            {
                Invariant.Assert(definitions != null);
                this.definitions = definitions;
            }

            public int Compare(object x, object y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase definitionX = null;
                DefinitionBase definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.MeasureSize.CompareTo(definitionY.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// Implementation of a simple enumerator of grid's logical children
        /// </summary>
        private class GridChildrenCollectionEnumeratorSimple : IEnumerator
        {
            internal GridChildrenCollectionEnumeratorSimple(Grid grid, bool includeChildren)
            {
                Debug.Assert(grid != null);
                _currentEnumerator = -1;
                _enumerator0 = new ColumnDefinitionCollection.Enumerator(grid.ExtData != null ? grid.ExtData.ColumnDefinitions : null);
                _enumerator1 = new RowDefinitionCollection.Enumerator(grid.ExtData != null ? grid.ExtData.RowDefinitions : null);
                // GridLineRenderer is NOT included into this enumerator.
                _enumerator2Index = 0;
                if (includeChildren)
                {
                    _enumerator2Collection = grid.Children;
                    _enumerator2Count = _enumerator2Collection.Count;
                }
                else
                {
                    _enumerator2Collection = null;
                    _enumerator2Count = 0;
                }
            }

            public bool MoveNext()
            {
                while (_currentEnumerator < 3)
                {
                    if (_currentEnumerator >= 0)
                    {
                        switch (_currentEnumerator)
                        {
                            case (0): if (_enumerator0.MoveNext()) { _currentChild = _enumerator0.Current; return (true); } break;
                            case (1): if (_enumerator1.MoveNext()) { _currentChild = _enumerator1.Current; return (true); } break;
                            case (2): if (_enumerator2Index < _enumerator2Count)
                                      {
                                          _currentChild = _enumerator2Collection[_enumerator2Index];
                                          _enumerator2Index++;
                                          return (true);
                                      }
                                      break;
                        }
                    }
                    _currentEnumerator++;
                }
                return (false);
            }

            public Object Current
            {
                get
                {
                    if (_currentEnumerator == -1)
                    {
                        #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorNotStarted));
                    }
                    if (_currentEnumerator >= 3)
                    {
                        #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorReachedEnd));
                    }

                    //  assert below is not true anymore since UIElementCollection allowes for null children
                    //Debug.Assert(_currentChild != null);
                    return (_currentChild);
                }
            }

            public void Reset()
            {
                _currentEnumerator = -1;
                _currentChild = null;
                _enumerator0.Reset();
                _enumerator1.Reset();
                _enumerator2Index = 0;
            }

            private int _currentEnumerator;
            private Object _currentChild;
            private ColumnDefinitionCollection.Enumerator _enumerator0;
            private RowDefinitionCollection.Enumerator _enumerator1;
            private UIElementCollection _enumerator2Collection;
            private int _enumerator2Index;
            private int _enumerator2Count;
        }

        /// <summary>
        /// Helper to render grid lines.
        /// </summary>
        internal class GridLinesRenderer : DrawingVisual
        {
            /// <summary>
            /// Static initialization
            /// </summary>
            static GridLinesRenderer()
            {
                s_oddDashPen = new Pen(Brushes.Blue, c_penWidth);
                DoubleCollection oddDashArray = new DoubleCollection();
                oddDashArray.Add(c_dashLength);
                oddDashArray.Add(c_dashLength);
                s_oddDashPen.DashStyle = new DashStyle(oddDashArray, 0);
                s_oddDashPen.DashCap = PenLineCap.Flat;
                s_oddDashPen.Freeze();

                s_evenDashPen = new Pen(Brushes.Yellow, c_penWidth);
                DoubleCollection evenDashArray = new DoubleCollection();
                evenDashArray.Add(c_dashLength);
                evenDashArray.Add(c_dashLength);
                s_evenDashPen.DashStyle = new DashStyle(evenDashArray, c_dashLength);
                s_evenDashPen.DashCap = PenLineCap.Flat;
                s_evenDashPen.Freeze();
            }

            /// <summary>
            /// UpdateRenderBounds.
            /// </summary>
            /// <param name="boundsSize">Size of render bounds</param>
            internal void UpdateRenderBounds(Size boundsSize)
            {
                using (DrawingContext drawingContext = RenderOpen())
                {
                    Grid grid = VisualTreeHelper.GetParent(this) as Grid;
                    if (    grid == null
                        ||  grid.ShowGridLines == false )
                    {
                        return;
                    }

                    for (int i = 1; i < grid.DefinitionsU.Length; ++i)
                    {
                        DrawGridLine(
                            drawingContext,
                            grid.DefinitionsU[i].FinalOffset, 0.0,
                            grid.DefinitionsU[i].FinalOffset, boundsSize.Height);
                    }

                    for (int i = 1; i < grid.DefinitionsV.Length; ++i)
                    {
                        DrawGridLine(
                            drawingContext,
                            0.0, grid.DefinitionsV[i].FinalOffset,
                            boundsSize.Width, grid.DefinitionsV[i].FinalOffset);
                    }
                }
            }

            /// <summary>
            /// Draw single hi-contrast line.
            /// </summary>
            private static void DrawGridLine(
                DrawingContext drawingContext,
                double startX,
                double startY,
                double endX,
                double endY)
            {
                Point start = new Point(startX, startY);
                Point end = new Point(endX, endY);
                drawingContext.DrawLine(s_oddDashPen, start, end);
                drawingContext.DrawLine(s_evenDashPen, start, end);
            }

            private const double c_dashLength = 4.0;    //
            private const double c_penWidth = 1.0;      //
            private static readonly Pen s_oddDashPen;   //  first pen to draw dash
            private static readonly Pen s_evenDashPen;  //  second pen to draw dash
            private static readonly Point c_zeroPoint = new Point(0, 0);
        }

        #endregion Private Structures Classes

        //------------------------------------------------------
        //
        //  Extended debugging for grid
        //
        //------------------------------------------------------

#if GRIDPARANOIA
        private static double _performanceFrequency;
        private static readonly bool _performanceFrequencyInitialized = InitializePerformanceFrequency();

        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private static double CostInMilliseconds(long count)
        {
            return ((double)count / _performanceFrequency);
        }

        private static long Cost(long startCount, long endCount)
        {
            long l = endCount - startCount;
            if (l < 0)  { l += long.MaxValue;   }
            return (l);
        }

        private static bool InitializePerformanceFrequency()
        {
            long l;
            QueryPerformanceFrequency(out l);
            _performanceFrequency = (double)l * 0.001;
            return (true);
        }

        private struct Counter
        {
            internal long   Start;
            internal long   Total;
            internal int    Calls;
        }

        private Counter[] _counters;
        private bool _hasNewCounterInfo;
#endif // GRIDPARANOIA

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        [Conditional("GRIDPARANOIA")]
        internal void EnterCounterScope(Counters scopeCounter)
        {
            #if GRIDPARANOIA
            if (ID == "CountThis")
            {
                if (_counters == null)
                {
                    _counters = new Counter[(int)Counters.Count];
                }
                ExitCounterScope(Counters.Default);
                EnterCounter(scopeCounter);
            }
            else
            {
                _counters = null;
            }
            #endif // GRIDPARANOIA
        }

        [Conditional("GRIDPARANOIA")]
        internal void ExitCounterScope(Counters scopeCounter)
        {
            #if GRIDPARANOIA
            if (_counters != null)
            {
                if (scopeCounter != Counters.Default)
                {
                    ExitCounter(scopeCounter);
                }

                if (_hasNewCounterInfo)
                {
                    string NFormat = "F6";
                    Console.WriteLine(
                                "\ncounter name          | total t (ms)  | # of calls    | per call t (ms)"
                            +   "\n----------------------+---------------+---------------+----------------------" );

                    for (int i = 0; i < _counters.Length; ++i)
                    {
                        if (_counters[i].Calls > 0)
                        {
                            Counters counter = (Counters)i;
                            double total = CostInMilliseconds(_counters[i].Total);
                            double single = total / _counters[i].Calls;
                            string counterName = counter.ToString();
                            string separator;

                            if (counterName.Length < 8)         { separator = "\t\t\t";  }
                            else if (counterName.Length < 16)   { separator = "\t\t";    }
                            else                                { separator = "\t";      }

                            Console.WriteLine(
                                    counter.ToString() + separator
                                +   total.ToString(NFormat) + "\t"
                                +   _counters[i].Calls + "\t\t"
                                +   single.ToString(NFormat));

                            _counters[i] = new Counter();
                        }
                    }
                }
                _hasNewCounterInfo = false;
            }
            #endif // GRIDPARANOIA
        }

        [Conditional("GRIDPARANOIA")]
        internal void EnterCounter(Counters counter)
        {
            #if GRIDPARANOIA
            if (_counters != null)
            {
                Debug.Assert((int)counter < _counters.Length);

                int i = (int)counter;
                QueryPerformanceCounter(out _counters[i].Start);
            }
            #endif // GRIDPARANOIA
        }

        [Conditional("GRIDPARANOIA")]
        internal void ExitCounter(Counters counter)
        {
            #if GRIDPARANOIA
            if (_counters != null)
            {
                Debug.Assert((int)counter < _counters.Length);

                int i = (int)counter;
                long l;
                QueryPerformanceCounter(out l);
                l = Cost(_counters[i].Start, l);
                _counters[i].Total += l;
                _counters[i].Calls++;
                _hasNewCounterInfo = true;
            }
            #endif // GRIDPARANOIA
        }

        internal enum Counters : int
        {
            Default = -1,

            MeasureOverride,
            _ValidateColsStructure,
            _ValidateRowsStructure,
            _ValidateCells,
            _MeasureCell,
            __MeasureChild,
            _CalculateDesiredSize,

            ArrangeOverride,
            _SetFinalSize,
            _ArrangeChildHelper2,
            _PositionCell,

            Count,
        }
    }
}




