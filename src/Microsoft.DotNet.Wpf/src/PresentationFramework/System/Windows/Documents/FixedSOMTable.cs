// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                            
    Description:
        This class reprsents a table on the page
--*/

namespace System.Windows.Documents
{
    using System.Windows.Shapes;
    using System.Windows.Media;
    using System.Windows.Markup;
    using System.Diagnostics;
    using System.Windows;
    using System.Globalization;

    internal sealed class FixedSOMTable : FixedSOMPageElement
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMTable(FixedSOMPage page) : base(page)
        {
            _numCols = 0;
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods
#if DEBUG        
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisual)
        {
            Pen pen = new Pen(Brushes.Green, 5);
            Rect rect = _boundingRect;
            dc.DrawRectangle(null, pen , rect);

            FormattedText ft = new FormattedText(label, 
                                        System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Courier New"), 
                                        20,
                                        Brushes.Blue,
                                        MS.Internal.FontCache.Util.PixelsPerDip);
            Point labelLocation = new Point(rect.Left-25, (rect.Bottom + rect.Top)/2 - 10);
          //  dc.DrawText(ft, labelLocation);            
            
            for (int i = 0; i < _semanticBoxes.Count; i++)
            {
                _semanticBoxes[i].Render(dc, label + " " + i.ToString(),debugVisual);
            }
        }
#endif
        public void AddRow(FixedSOMTableRow row)
        {
            base.Add(row);
            int colCount = row.SemanticBoxes.Count;
            if (colCount > _numCols)
            {
                _numCols = colCount;
            }
        }

        public bool AddContainer(FixedSOMContainer container)
        {
            Rect bounds = container.BoundingRect;

            
            //Allow couple pixels margin
            double verticalOverlap = bounds.Height * 0.2;
            double horizontalOverlap = bounds.Width * 0.2;
            bounds.Inflate(-horizontalOverlap,-verticalOverlap);

            if (this.BoundingRect.Contains(bounds))
            {
                foreach (FixedSOMTableRow row in this.SemanticBoxes)
                {
                    if (row.BoundingRect.Contains(bounds))
                    {
                        foreach (FixedSOMTableCell cell in row.SemanticBoxes)
                        {
                            if (cell.BoundingRect.Contains(bounds))
                            {
                                cell.AddContainer(container);
                                FixedSOMFixedBlock block = container as FixedSOMFixedBlock;
                                if (block != null)
                                {
                                    if (block.IsRTL)
                                    {
                                        _RTLCount++;
                                    }
                                    else
                                    {                                        _LTRCount++;
                                    }
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }


        public override void SetRTFProperties(FixedElement element)
        {
            if (element.Type == typeof(Table))
            {
                element.SetValue(Table.CellSpacingProperty, 0.0);
            }
        }

        
        #endregion Public Methods

        #region Public Properties
        public override bool IsRTL
        {
            get
            {
                return _RTLCount > _LTRCount;
            }
        }
        #endregion Public Properties

        #region Internal Properties
        internal override FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return new FixedElement.ElementType[2] { FixedElement.ElementType.Table, FixedElement.ElementType.TableRowGroup };
            }
        }

        internal bool IsEmpty
        {
            get
            {
                foreach (FixedSOMTableRow row in this.SemanticBoxes)
                {
                    if (!row.IsEmpty)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        internal bool IsSingleCelled
        {
            get
            {
                if (this.SemanticBoxes.Count == 1)
                {
                    FixedSOMTableRow row = this.SemanticBoxes[0] as FixedSOMTableRow;
                    Debug.Assert(row != null);
                    return (row.SemanticBoxes.Count == 1);
                }
                return false;
            }
        }


        #endregion Internal Properties

        #region Internal methods
        internal void DeleteEmptyRows()
        {
            for (int i=0; i<this.SemanticBoxes.Count;)
            {
                FixedSOMTableRow row = this.SemanticBoxes[i] as FixedSOMTableRow;
                Debug.Assert(row != null);
                if (row != null && row.IsEmpty && row.BoundingRect.Height < _minRowHeight)
                {
                    this.SemanticBoxes.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        //Deletes an entire column if its empty
        //Sets column spans

        internal void DeleteEmptyColumns()
        {
            int nRows = this.SemanticBoxes.Count;
            int[] indexInRow = new int[nRows];

            while (true)
            {
                double nextCol = double.MaxValue;
                bool deleteCol = true;
                int r;
                for (r = 0; r < nRows; r++)
                {
                    FixedSOMTableRow row = (FixedSOMTableRow)(this.SemanticBoxes[r]);
                    int idx = indexInRow[r];
                    FixedSOMTableCell cell;

                    deleteCol = deleteCol && idx < row.SemanticBoxes.Count;
                    if (deleteCol)
                    {
                        cell = (FixedSOMTableCell)row.SemanticBoxes[idx];
                        // is empty?
                        deleteCol = cell.IsEmpty && cell.BoundingRect.Width < _minColumnWidth;
                    }

                    // where does next column start?
                    if (idx + 1 < row.SemanticBoxes.Count)
                    {
                        cell = (FixedSOMTableCell)row.SemanticBoxes[idx+1];
                        double cellStart = cell.BoundingRect.Left;

                        if (cellStart < nextCol)
                        {
                            if (nextCol != double.MaxValue)
                            {
                                // can't delete colums where not all have same width
                                deleteCol = false;
                            }
                            nextCol = cellStart;
                        }
                        else if (cellStart > nextCol)
                        {
                            deleteCol = false;
                        }
                    }
                }

                if (deleteCol)
                {
                    for (r = 0; r < nRows; r++)
                    {
                        FixedSOMTableRow row = (FixedSOMTableRow)(this.SemanticBoxes[r]);
                        row.SemanticBoxes.RemoveAt(indexInRow[r]);
                    }

                    if (nextCol == double.MaxValue)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                // are we done?
                if (nextCol == double.MaxValue)
                {
                    break;
                }
                // increment ColumnSpans, indexInRow
                for (r = 0; r < nRows; r++)
                {
                    FixedSOMTableRow row = (FixedSOMTableRow)(this.SemanticBoxes[r]);
                    int idx = indexInRow[r];
                    if (idx + 1 < row.SemanticBoxes.Count && row.SemanticBoxes[idx + 1].BoundingRect.Left == nextCol)
                    {
                        indexInRow[r] = idx + 1;
                    }
                    else
                    {
                        ((FixedSOMTableCell)row.SemanticBoxes[idx]).ColumnSpan++;
                    }
                }
            }
        }

          

        #endregion Internal methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields
        const double _minColumnWidth = 5; // empty columns narrower than this will be deleted
        const double _minRowHeight = 10; //empty rows smaller than this will be deleted

        private int _RTLCount;
        private int _LTRCount;

        int _numCols;

        #endregion Private Fields
    }
}



