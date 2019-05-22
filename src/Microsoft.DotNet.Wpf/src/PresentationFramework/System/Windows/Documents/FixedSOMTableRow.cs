// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                            
    Description:
        This class reprsents a table row on the page. It would contain several table cells           
--*/

namespace System.Windows.Documents
{
    using System.Windows.Shapes;
    using System.Windows.Media;
    using System.Globalization;
    using System.Diagnostics;
    using System.Windows;

    internal sealed class FixedSOMTableRow : FixedSOMContainer
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMTableRow()
        {
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
            /*
            Pen pen = new Pen(Brushes.Red, 5);
            Rect rect = _boundingRect;
            dc.DrawRectangle(null, pen , rect);

            FormattedText ft = new FormattedText(String.Format("{0} columns", _semanticBoxes.Count),
                                        TypeConverterHelper.InvariantEnglishUS,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Courier New"),
                                        20,
                                        Brushes.Red);
            Point labelLocation = new Point(rect.Right + 10, (rect.Bottom + rect.Top) / 2 - 10);
            dc.DrawText(ft, labelLocation);
            */
            for (int i = 0; i < _semanticBoxes.Count; i++)
            {
                _semanticBoxes[i].Render(dc, label + ":" + i.ToString(), debugVisual);
            }

        }
#endif
        public void AddCell(FixedSOMTableCell cell)
        {
            base.Add(cell);
        }
        #endregion Internal Methods

        #region Internal Properties
        internal override FixedElement.ElementType[] ElementTypes
        {
            get
            {
                return new FixedElement.ElementType[1] { FixedElement.ElementType.TableRow };
            }
        }

        internal bool IsEmpty
        {
            get
            {
                foreach (FixedSOMTableCell cell in this.SemanticBoxes)
                {
                    if (!cell.IsEmpty)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields
        #endregion Private Fields
    }
}



