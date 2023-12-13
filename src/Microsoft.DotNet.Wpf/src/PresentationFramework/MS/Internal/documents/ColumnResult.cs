// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The ColumnResult class provides access to layout-calculated 
//              information for a column.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.PtsHost;
using MS.Internal.PtsHost.UnsafeNativeMethods;
using MS.Internal.Text;



namespace MS.Internal.Documents
{
    /// <summary>
    /// The ColumnResult class provides access to layout-calculated 
    /// information for a column.
    /// </summary>
    internal sealed class ColumnResult
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="page">Document page that owns the column.</param>
        /// <param name="trackDesc">PTS track description.</param>
        /// <param name="contentOffset">Content's offset.</param>
        internal ColumnResult(FlowDocumentPage page, ref PTS.FSTRACKDESCRIPTION trackDesc, Vector contentOffset)
        {
            _page = page;
            _columnHandle = trackDesc.pfstrack;
            _layoutBox = new Rect(
                TextDpi.FromTextDpi(trackDesc.fsrc.u), TextDpi.FromTextDpi(trackDesc.fsrc.v),
                TextDpi.FromTextDpi(trackDesc.fsrc.du), TextDpi.FromTextDpi(trackDesc.fsrc.dv));
            _layoutBox.X += contentOffset.X;
            _layoutBox.Y += contentOffset.Y;
            _columnOffset = new Vector(TextDpi.FromTextDpi(trackDesc.fsrc.u), TextDpi.FromTextDpi(trackDesc.fsrc.v));
            _hasTextContent = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="subpage">Subpage that owns the column.</param>
        /// <param name="trackDesc">PTS track description.</param>
        /// <param name="contentOffset">Content's offset.</param>
        internal ColumnResult(BaseParaClient subpage, ref PTS.FSTRACKDESCRIPTION trackDesc, Vector contentOffset)
        {
            // Subpage must be figure, floater or subpage paraclient
            Invariant.Assert(subpage is SubpageParaClient || subpage is FigureParaClient || subpage is FloaterParaClient);
            _subpage = subpage;
            _columnHandle = trackDesc.pfstrack;
            _layoutBox = new Rect(
                TextDpi.FromTextDpi(trackDesc.fsrc.u), TextDpi.FromTextDpi(trackDesc.fsrc.v),
                TextDpi.FromTextDpi(trackDesc.fsrc.du), TextDpi.FromTextDpi(trackDesc.fsrc.dv));
            _layoutBox.X += contentOffset.X;
            _layoutBox.Y += contentOffset.Y;
            _columnOffset = new Vector(TextDpi.FromTextDpi(trackDesc.fsrc.u), TextDpi.FromTextDpi(trackDesc.fsrc.v));
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns whether the position is contained in this column.
        /// </summary>
        /// <param name="position">A position to test.</param>
        /// <param name="strict">Apply strict validation rules.</param>
        /// <returns>
        /// True if column contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        internal bool Contains(ITextPointer position, bool strict)
        {
            EnsureTextContentRange();
            return _contentRange.Contains(position, strict);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Represents the beginning of the column’s contents.
        /// </summary>
        internal ITextPointer StartPosition
        {
            get
            {
                EnsureTextContentRange();
                return _contentRange.StartPosition;
            }
        }

        /// <summary>
        /// Represents the end of the column’s contents.
        /// </summary>
        internal ITextPointer EndPosition
        {
            get
            {
                EnsureTextContentRange();
                return _contentRange.EndPosition;
            }
        }

        /// <summary>
        /// The bounding rectangle of the column; this is relative to the 
        /// parent bounding box.
        /// </summary>
        internal Rect LayoutBox { get { return _layoutBox; } }

        /// <summary>
        /// Collection of ParagraphResults for the column’s contents.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> Paragraphs
        {
            get
            {
                if (_paragraphs == null)
                {
                    // Set _hasTextContent to true when getting paragraph collections if any paragraph has text content.
                    _hasTextContent = false;
                    if (_page != null)
                    {
                        _paragraphs = _page.GetParagraphResultsFromColumn(_columnHandle, _columnOffset, out _hasTextContent);
                    }
                    else
                    {
                        if (_subpage is FigureParaClient)
                        {
                            _paragraphs = ((FigureParaClient)_subpage).GetParagraphResultsFromColumn(_columnHandle, _columnOffset, out _hasTextContent);
                        }
                        else if (_subpage is FloaterParaClient)
                        {
                            _paragraphs = ((FloaterParaClient)_subpage).GetParagraphResultsFromColumn(_columnHandle, _columnOffset, out _hasTextContent);
                        }
                        else if (_subpage is SubpageParaClient)
                        {
                            _paragraphs = ((SubpageParaClient)_subpage).GetParagraphResultsFromColumn(_columnHandle, _columnOffset, out _hasTextContent);
                        }
                        else
                        {
                            Invariant.Assert(false, "Expecting Subpage, Figure or Floater ParaClient");
                        }
                    }
                    Debug.Assert(_paragraphs != null && _paragraphs.Count > 0);
                }
                return _paragraphs;
            }
        }

        /// <summary>
        /// Returns true if the column has any text content. This is determined by checking if any paragraph in the paragraphs collection 
        /// has text content. A paragraph has text content if it includes some text characters besides figures and floaters. An EOP character is
        /// considered text content if it is the only character in the paragraph, but a paragraph that has only
        /// figures, floaters, EOP and no text has no text content.
        /// </summary>
        internal bool HasTextContent
        {
            get
            {
                if (_paragraphs == null)
                {
                    // Creating paragraph results will query the page/subpage about text content in the paragrph collection and
                    // set _hasTextContent appropriately
                    ReadOnlyCollection<ParagraphResult> paragraphs = Paragraphs;
                }
                return _hasTextContent;
            }
        }
        /// <summary>
        /// Represents the column’s contents.
        /// </summary>
        internal TextContentRange TextContentRange
        {
            get
            {
                EnsureTextContentRange();
                return _contentRange;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Retrive TextContentRange if necessary.
        /// </summary>
        private void EnsureTextContentRange()
        {
            if (_contentRange == null)
            {
                if (_page != null)
                {
                    _contentRange = _page.GetTextContentRangeFromColumn(_columnHandle);
                }
                else
                {
                    if (_subpage is FigureParaClient)
                    {
                        _contentRange = ((FigureParaClient)_subpage).GetTextContentRangeFromColumn(_columnHandle);
                    }
                    else if (_subpage is FloaterParaClient)
                    {
                        _contentRange = ((FloaterParaClient)_subpage).GetTextContentRangeFromColumn(_columnHandle);
                    }
                    else if (_subpage is SubpageParaClient)
                    {
                        _contentRange = ((SubpageParaClient)_subpage).GetTextContentRangeFromColumn(_columnHandle);
                    }
                    else
                    {
                        Invariant.Assert(false, "Expecting Subpage, Figure or Floater ParaClient");
                    }
                }
                Invariant.Assert(_contentRange != null);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Document page that owns the column.
        /// </summary>
        private readonly FlowDocumentPage _page;

        /// <summary>
        /// Subpage that owns the column.
        /// </summary>
        private readonly BaseParaClient _subpage;

        /// <summary>
        /// Column handle (PTS track handle).
        /// </summary>
        private readonly IntPtr _columnHandle;

        /// <summary>
        /// Layout rectangle of the column.
        /// </summary>
        private readonly Rect _layoutBox;

        /// <summary>
        /// Offset of the column from the top of PTS page.
        /// </summary>
        private readonly Vector _columnOffset;

        /// <summary>
        /// TextContentRanges representing the column's contents.
        /// </summary>
        private TextContentRange _contentRange;

        /// <summary>
        /// The collection of ParagraphResults for the column's paragraphs.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _paragraphs;

        /// <summary>
        /// True if any of the column's paragraphs results has text content
        /// </summary>
        private bool _hasTextContent;

        #endregion Private Fields
    }
}
