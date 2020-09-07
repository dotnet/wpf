// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Para client for cell
//

using MS.Internal.Text;
using MS.Internal.Documents;
using MS.Internal.PtsTable;
using System.Security;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Para client for cell
    /// </summary>
    internal sealed class CellParaClient : SubpageParaClient
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cellParagraph">Cell paragraph.</param>
        /// <param name="tableParaClient">Table paraclient.</param>
        internal CellParaClient(CellParagraph cellParagraph, TableParaClient tableParaClient) : base(cellParagraph)
        {
            _tableParaClient = tableParaClient;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Arrange.
        /// </summary>
        /// <param name="du">U offset component of the cell's visual.</param>
        /// <param name="dv">V offset component of the cell's visual.</param>
        /// <param name="rcTable">Table's rectangle.. (in page flow dir)</param>
        /// <param name="tableFlowDirection">Table's flow direction</param>
        /// <param name="pageContext">Page context</param>
        internal void Arrange(int du, int dv, PTS.FSRECT rcTable, FlowDirection tableFlowDirection, PageContext pageContext)
        {
            //
            // Determine cell width based on column widths.
            //
            CalculatedColumn[] calculatedColumns = _tableParaClient.CalculatedColumns;
            Debug.Assert(calculatedColumns != null
                        && (Cell.ColumnIndex + Cell.ColumnSpan) <= calculatedColumns.Length);

            double durCellSpacing = Table.InternalCellSpacing;
            double durCellWidth = -durCellSpacing;

            // find the width sum of all columns the cell spans
            int i = Cell.ColumnIndex + Cell.ColumnSpan - 1;

            do
            {
                durCellWidth += calculatedColumns[i].DurWidth + durCellSpacing;
            } while (--i >= ColumnIndex);

            if(tableFlowDirection != PageFlowDirection)
            {
                PTS.FSRECT pageRect = pageContext.PageRect;

                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(PageFlowDirection), ref pageRect, ref rcTable, PTS.FlowDirectionToFswdir(tableFlowDirection), out rcTable));
            }

            _rect.u = du + rcTable.u;
            _rect.v = dv + rcTable.v;
            _rect.du = TextDpi.ToTextDpi(durCellWidth);
            _rect.dv = TextDpi.ToTextDpi(_arrangeHeight);

            if(tableFlowDirection != PageFlowDirection)
            {
                PTS.FSRECT pageRect = pageContext.PageRect;

                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(tableFlowDirection), ref pageRect, ref _rect, PTS.FlowDirectionToFswdir(PageFlowDirection), out _rect));
            }

            _flowDirectionParent = tableFlowDirection;
            _flowDirection = (FlowDirection)Paragraph.Element.GetValue(FrameworkElement.FlowDirectionProperty);
            _pageContext = pageContext;

            OnArrange();

            if(_paraHandle.Value != IntPtr.Zero)
            {
                PTS.Validate(PTS.FsClearUpdateInfoInSubpage(PtsContext.Context, _paraHandle.Value), PtsContext);
            }
        }

        /// <summary>
        /// ValidateVisual.
        /// </summary>
        internal void ValidateVisual()
        {
            ValidateVisual(PTS.FSKUPDATE.fskupdNew);
        }


        /// <summary>
        /// FormatCellFinite - Calls same apis that PTS does for formatting
        /// </summary>
        /// <param name="subpageSize">Subpage size.</param>
        /// <param name="breakRecordIn">Subpage break record.</param>
        /// <param name="isEmptyOk">Is an empty cell ok?</param>
        /// <param name="fswdir">Text Direction</param>
        /// <param name="fsksuppresshardbreakbeforefirstparaIn">Suppress hard break before first para</param>
        /// <param name="fsfmtr">Format result</param>
        /// <param name="dvrUsed">dvr Used</param>
        /// <param name="breakRecordOut">Resultant break record for end of page</param>
        internal void FormatCellFinite(Size subpageSize, IntPtr breakRecordIn, bool isEmptyOk, uint fswdir, 
                                       PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                       out PTS.FSFMTR fsfmtr, out int dvrUsed, out IntPtr breakRecordOut)
        {
            IntPtr pfspara;
            PTS.FSBBOX fsbbox;
            IntPtr pmcsclientOut;
            PTS.FSKCLEAR fskclearOut;
            int dvrTopSpace;
            PTS.FSPAP fspap;

            if(CellParagraph.StructuralCache.DtrList != null && breakRecordIn != null)
            {
                CellParagraph.InvalidateStructure(TextContainerHelper.GetCPFromElement(CellParagraph.StructuralCache.TextContainer, CellParagraph.Element, ElementEdge.BeforeStart));
            }

            // Ensures segment is created for paragraph
            fspap = new PTS.FSPAP();
            CellParagraph.GetParaProperties(ref fspap);

            PTS.FSRECT rectCell;

            rectCell = new PTS.FSRECT();

            rectCell.u = rectCell.v = 0;

            rectCell.du = TextDpi.ToTextDpi(subpageSize.Width);
            rectCell.dv = TextDpi.ToTextDpi(subpageSize.Height);

            // Suppress top space if cell is broken, but not otherwise
            bool suppressTopSpace = (breakRecordIn != IntPtr.Zero) ? true : false;

            CellParagraph.FormatParaFinite(this, breakRecordIn,
                                           PTS.FromBoolean(true),
                                           IntPtr.Zero,
                                           PTS.FromBoolean(isEmptyOk),
                                           PTS.FromBoolean(suppressTopSpace),
                                           fswdir,
                                           ref rectCell,
                                           null,
                                           PTS.FSKCLEAR.fskclearNone,
                                           fsksuppresshardbreakbeforefirstparaIn,
                                           out fsfmtr,
                                           out pfspara,
                                           out breakRecordOut,
                                           out dvrUsed,
                                           out fsbbox,
                                           out pmcsclientOut,
                                           out fskclearOut,
                                           out dvrTopSpace);

            if (pmcsclientOut != IntPtr.Zero)
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                dvrUsed += mcs.Margin;
                mcs.Dispose();
                pmcsclientOut = IntPtr.Zero;
            }
            _paraHandle.Value = pfspara;
        }

        /// <summary>
        /// FormatCellBottomless
        /// </summary>
        /// <param name="fswdir">Text Direction</param>
        /// <param name="width">Width of cell (height is specified by row props)</param>
        /// <param name="fmtrbl">bottomless format result</param>
        /// <param name="dvrUsed">dvr Used</param>
        internal void FormatCellBottomless(uint fswdir, double width, out PTS.FSFMTRBL fmtrbl, out int dvrUsed)
        {
            IntPtr pfspara;
            PTS.FSBBOX fsbbox;
            IntPtr pmcsclientOut;
            PTS.FSKCLEAR fskclearOut;
            int dvrTopSpace;
            int fPageBecomesUninterruptable;
            PTS.FSPAP fspap;


            if(CellParagraph.StructuralCache.DtrList != null)
            {
                CellParagraph.InvalidateStructure(TextContainerHelper.GetCPFromElement(CellParagraph.StructuralCache.TextContainer, CellParagraph.Element, ElementEdge.BeforeStart));
            }

            fspap = new PTS.FSPAP();
            CellParagraph.GetParaProperties(ref fspap);

            CellParagraph.FormatParaBottomless(this, PTS.FromBoolean(false),
                                                fswdir, 0, TextDpi.ToTextDpi(width),
                                                0, null, PTS.FSKCLEAR.fskclearNone,
                                                PTS.FromBoolean(true),
                                                out fmtrbl,
                                                out pfspara,
                                                out dvrUsed,
                                                out fsbbox,
                                                out pmcsclientOut,
                                                out fskclearOut,
                                                out dvrTopSpace,
                                                out fPageBecomesUninterruptable);

            if (pmcsclientOut != IntPtr.Zero)
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                dvrUsed += mcs.Margin;
                mcs.Dispose();
                pmcsclientOut = IntPtr.Zero;
            }

            _paraHandle.Value = pfspara;
        }


        /// <summary>
        /// UpdateBottomlessCell
        /// </summary>
        /// <param name="fswdir">Text Direction</param>
        /// <param name="width">Width of cell (height is specified by row props)</param>
        /// <param name="fmtrbl">bottomless format result</param>
        /// <param name="dvrUsed">dvr Used</param>
        internal void UpdateBottomlessCell(uint fswdir, double width, out PTS.FSFMTRBL fmtrbl, out int dvrUsed)
        {
            IntPtr pmcsclientOut;
            PTS.FSKCLEAR fskclearOut;
            PTS.FSBBOX fsbbox;
            int dvrTopSpace;
            int fPageBecomesUninterruptable;

            PTS.FSPAP fspap;

            fspap = new PTS.FSPAP();
            CellParagraph.GetParaProperties(ref fspap);

            CellParagraph.UpdateBottomlessPara(_paraHandle.Value, this,
                                               PTS.FromBoolean(false),
                                               fswdir, 0,
                                               TextDpi.ToTextDpi(width),
                                               0, null,
                                               PTS.FSKCLEAR.fskclearNone,
                                               PTS.FromBoolean(true),
                                               out fmtrbl,
                                               out dvrUsed,
                                               out fsbbox,
                                               out pmcsclientOut,
                                               out fskclearOut,
                                               out dvrTopSpace,
                                               out fPageBecomesUninterruptable);
            if (pmcsclientOut != IntPtr.Zero)
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientOut) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                dvrUsed += mcs.Margin;
                mcs.Dispose();
                pmcsclientOut = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="visibleRect"></param>
        /// <returns></returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            Geometry geometry = null;

            // Find out if cell is selected. We consider cell selected if its end tag is crossed by selection.
            // The asymmetry is important here - it allows to use only normalized positions
            // and still be able to select individual cells.
            // Note that this logic is an assumption in textselection unit expansion mechanism
            // (TexSelection.ExtendSelectionToStructuralUnit method).
            if (endPosition.CompareTo(Cell.StaticElementEnd) >= 0)
            {
                geometry = new RectangleGeometry(_rect.FromTextDpi());
            }
            else
            {
                SubpageParagraphResult paragraphResult = (SubpageParagraphResult)(CreateParagraphResult());
                ReadOnlyCollection<ColumnResult> colResults = paragraphResult.Columns;
                Transform transform;

                transform = new TranslateTransform(-TextDpi.FromTextDpi(ContentRect.u), -TextDpi.FromTextDpi(ContentRect.v));
                visibleRect = transform.TransformBounds(visibleRect);
                transform = null;

                geometry = TextDocumentView.GetTightBoundingGeometryFromTextPositionsHelper(colResults[0].Paragraphs, paragraphResult.FloatingElements, startPosition, endPosition, 0.0, visibleRect);

                if (geometry != null)
                {
                    //  restrict geometry to the cell's content rect boundary.
                    //  because of end-of-line / end-of-para simulation calculated geometry could be larger. 
                    Rect viewport = new Rect(0, 0, TextDpi.FromTextDpi(ContentRect.du), TextDpi.FromTextDpi(ContentRect.dv));
                    CaretElement.ClipGeometryByViewport(ref geometry, viewport);

                    transform = new TranslateTransform(TextDpi.FromTextDpi(ContentRect.u), TextDpi.FromTextDpi(ContentRect.v));
                    CaretElement.AddTransformToGeometry(geometry, transform);
                }
            }

            return (geometry);
        }

        /// <summary>
        /// Calculates width of cell
        /// </summary>
        /// <param name="tableParaClient">Table owner</param>
        /// <returns>Cell's width</returns>
        internal double CalculateCellWidth(TableParaClient tableParaClient)
        {
            Debug.Assert(tableParaClient != null);

            CalculatedColumn[] calculatedColumns = tableParaClient.CalculatedColumns;
            Debug.Assert(   calculatedColumns != null 
                        && (Cell.ColumnIndex + Cell.ColumnSpan) <= calculatedColumns.Length);

            double durCellSpacing = Table.InternalCellSpacing;
            double durCellWidth = -durCellSpacing;

            // find the width sum of all columns the cell spans
            int i = Cell.ColumnIndex + Cell.ColumnSpan - 1;

            do
            {
                durCellWidth += calculatedColumns[i].DurWidth + durCellSpacing;
            } while (--i >= Cell.ColumnIndex);

            Debug.Assert(0 <= durCellWidth);

            return durCellWidth;
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Cell.
        /// </summary>
        internal TableCell Cell { get { return (CellParagraph.Cell); } }

        /// <summary>
        /// Table.
        /// </summary>
        internal Table Table { get { return (Cell.Table); } }

        /// <summary>
        /// Cell paragraph.
        /// </summary>
        internal CellParagraph CellParagraph { get { return (CellParagraph)_paragraph; } }

        /// <summary>
        /// Returns column index.
        /// </summary>
        internal int ColumnIndex { get { return (Cell.ColumnIndex); } }

        /// <summary>
        /// Sets height for arrange.
        /// </summary>
        internal double ArrangeHeight { set { _arrangeHeight = value; } }

        /// <summary>
        /// Table para client
        /// </summary>
        internal TableParaClient TableParaClient { get { return (_tableParaClient); } }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private double _arrangeHeight;              //  height for arrange
        private TableParaClient _tableParaClient;

        #endregion Private Fields
    }


    internal class CellInfo
    {
        /// <summary>
        /// C'tor - Just needs the table and cell para clients.
        /// </summary>
        /// <param name="tpc">Table para client.</param>
        /// <param name="cpc">Cell Para client.</param>
        internal CellInfo(TableParaClient tpc, CellParaClient cpc)
        {
            _rectTable = new Rect(TextDpi.FromTextDpi(tpc.Rect.u),
                                 TextDpi.FromTextDpi(tpc.Rect.v),
                                 TextDpi.FromTextDpi(tpc.Rect.du),
                                 TextDpi.FromTextDpi(tpc.Rect.dv));

            _rectCell = new Rect(TextDpi.FromTextDpi(cpc.Rect.u),
                                 TextDpi.FromTextDpi(cpc.Rect.v),
                                 TextDpi.FromTextDpi(cpc.Rect.du),
                                 TextDpi.FromTextDpi(cpc.Rect.dv));

            _autofitWidth = tpc.AutofitWidth;

            _columnWidths = new double[tpc.CalculatedColumns.Length];

            for(int index = 0; index < tpc.CalculatedColumns.Length; index++)
            {
                _columnWidths[index] = tpc.CalculatedColumns[index].DurWidth;
            }

            _cell = cpc.Cell;
        }

        /// <summary>
        /// Adjusts cells rectangles by a given amount
        /// </summary>
        /// <param name="ptAdjust">Table para client.</param>
        internal void Adjust(Point ptAdjust)
        {
            _rectTable.X += ptAdjust.X;
            _rectTable.Y += ptAdjust.Y;

            _rectCell.X += ptAdjust.X;
            _rectCell.Y += ptAdjust.Y;
        }

        /// <summary>
        /// Cell info is for
        /// </summary>
        internal TableCell Cell { get { return (_cell); } }

        /// <summary>
        /// Widths of columns in table
        /// </summary>
        internal double[] TableColumnWidths { get { return (_columnWidths); } }

        /// <summary>
        /// Autofit Width of table
        /// </summary>
        internal double TableAutofitWidth { get { return (_autofitWidth); } }

        /// <summary>
        /// Area for table
        /// </summary>
        internal Rect TableArea { get { return (_rectTable); } }

        /// <summary>
        /// Area for cell
        /// </summary>
        internal Rect CellArea { get { return (_rectCell); } }

        private Rect _rectCell;
        private Rect _rectTable;
        private TableCell _cell;
        private double[] _columnWidths;
        private double _autofitWidth;
    }
}
