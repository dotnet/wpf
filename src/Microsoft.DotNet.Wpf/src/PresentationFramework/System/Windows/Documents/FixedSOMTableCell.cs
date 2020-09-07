// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                              
    Description:
        This class reprsents a table cell on the page. Objects of this class would contain 
        several FixedBlocks, Images etc. that fit within the cell boundaries        
--*/

namespace System.Windows.Documents
{
    using System.Windows.Shapes;
    using System.Windows.Media;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    internal sealed class FixedSOMTableCell : FixedSOMContainer
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMTableCell(double left, double top, double right, double bottom)
        {
            _boundingRect = new Rect(new Point(left, top), new Point(right, bottom));
            _containsTable = false;
            _columnSpan = 1;
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
            Pen pen = new Pen(Brushes.Red, 2);
            Rect rect = _boundingRect;
            dc.DrawRectangle(null, pen , rect);
            /*
            for (int i = 0; i < _semanticBoxes.Count; i++)
            {
                _semanticBoxes[i].Render(dc, null,debugVisual);
            }
            */
        }
#endif
        public void AddContainer(FixedSOMContainer container)
        {
            //Check nested tables first
            if (!(_containsTable &&
                _AddToInnerTable(container)))
            {
                base.Add(container);
            }

            if (container is FixedSOMTable)
            {
                _containsTable = true;
            }
        }

        public override void SetRTFProperties(FixedElement element)
        {
            element.SetValue(Block.BorderThicknessProperty, new Thickness(1));
            element.SetValue(Block.BorderBrushProperty, Brushes.Black);
            element.SetValue(TableCell.ColumnSpanProperty, _columnSpan);
        }

        #endregion Public Methods

        #region Private Methods

        private bool _AddToInnerTable(FixedSOMContainer container)
        {
            foreach (FixedSOMSemanticBox box in _semanticBoxes)
            {
                FixedSOMTable table = box as FixedSOMTable;
                if (table != null &&
                    table.AddContainer(container))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Private Methods


        #region Internal Properties
        internal override FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return new FixedElement.ElementType[1] { FixedElement.ElementType.TableCell };
            }
        }

        internal bool IsEmpty
        {
            get
            {
                foreach (FixedSOMContainer container in this.SemanticBoxes)
                {
                    FixedSOMTable table = container as FixedSOMTable;
                    if (table != null && !table.IsEmpty)
                    {
                        return false;
                    }
                    FixedSOMFixedBlock block = container as FixedSOMFixedBlock;
                    if (block != null && !block.IsWhiteSpace)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        internal int ColumnSpan
        {
            get
            {
                return _columnSpan;
            }
            set
            {
                _columnSpan = value;
            }
        }
        #endregion Internal Properties
        
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields
        private bool _containsTable;
        private int _columnSpan;
        #endregion Private Fields
        
    }
}



