// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: BaseParagraph provides identity for displayable part of 
//              paragraph in PTS world.
//

using System;
using System.Collections.Generic; // ReadOnlyCollection
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Documents; // TextPointer
using System.Windows.Media;
using MS.Internal;
using MS.Internal.Documents; // ParagraphResult
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // BaseParagraph provides identity for displayable part of paragraph in 
    // PTS world.
    // ----------------------------------------------------------------------
    internal abstract class BaseParaClient : UnmanagedHandle
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph owner of the ParaClient.
        // ------------------------------------------------------------------
        protected BaseParaClient(BaseParagraph paragraph) : base(paragraph.PtsContext)
        {
            _paraHandle = new SecurityCriticalDataForSet<IntPtr>(IntPtr.Zero);
            _paragraph = paragraph;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        // ------------------------------------------------------------------
        // Update internal cache of ParaClient and arrange its content.
        //
        //      paraDesc - paragraph handle
        //      rcPara - rectangle of the paragraph
        //      dvrTopSpace - top space calculated as a result of margin 
        //                    collapsing
        //      fswdirParent - Flow direction of track
        // ------------------------------------------------------------------
        internal void Arrange(IntPtr pfspara, PTS.FSRECT rcPara, int dvrTopSpace, uint fswdirParent)
        {
            // Make sure that paragraph handle (PFSPARA) is set. It is required to query paragraph content.
            Debug.Assert(_paraHandle.Value == IntPtr.Zero || _paraHandle.Value == pfspara);
            _paraHandle.Value = pfspara;

            // Set paragraph rectangle (relative to the page)
            _rect = rcPara;

            // Cache dvrTopSpace
            // Note: currently used only by tight geometry bound calculation code
            _dvrTopSpace = dvrTopSpace;

            // Current page context (used for mirroring and offsets)
            _pageContext = Paragraph.StructuralCache.CurrentArrangeContext.PageContext;

            // Cache flow directions
            _flowDirectionParent = PTS.FswdirToFlowDirection(fswdirParent);
            _flowDirection = (FlowDirection)Paragraph.Element.GetValue(FrameworkElement.FlowDirectionProperty);

            // Do paragraph specifc arrange
            OnArrange();
        }

        // ------------------------------------------------------------------
        // Returns baseline for first text line
        // ------------------------------------------------------------------
        internal virtual int GetFirstTextLineBaseline()
        {
            return _rect.v + _rect.dv;
        }

        // ------------------------------------------------------------------
        // Transfer display related information from another ParaClient.
        //
        //      oldParaClient - another ParaClient
        // ------------------------------------------------------------------
        internal void TransferDisplayInfo(BaseParaClient oldParaClient)
        {
            Debug.Assert(oldParaClient._visual != null);

            // Transfer visual node ownership
            _visual = oldParaClient._visual;
            oldParaClient._visual = null;
        }
    
        // ------------------------------------------------------------------
        // Hit tests to the correct IInputElement within the paragraph
        // that the mouse is over.
        // ------------------------------------------------------------------
        internal virtual IInputElement InputHitTest(PTS.FSPOINT pt)
        {
            return null;
        }

        // ------------------------------------------------------------------
        // Returns ArrayList of rectangles for the ContentElement e. 
        // Returns empty list if the paraClient does not contain e.
        // start: int representing start offset of e
        // length: int representing number of characters occupied by e.
        // parentOffset: indicates offset of parent element. Used only by
        //               subpage para clients when calculating rectangles
        // ------------------------------------------------------------------
        internal virtual List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            // Return empty collection as default
            return new List<Rect>();
        }

        // ------------------------------------------------------------------
        // Returns rectangles for a the Paragraph element if we have found
        // that it matches the element for which rectangles are needed.
        // Converts the _rect member to the layout DPI and returns it
        // ------------------------------------------------------------------
        internal virtual void GetRectanglesForParagraphElement(out List<Rect> rectangles)
        {
            rectangles = new List<Rect>();
            // Convert rect from Text DPI values
            Rect rect = TextDpi.FromTextRect(_rect);

            rectangles.Add(rect);
        }

        // ------------------------------------------------------------------
        // Validate visual node associated with paragraph.
        //
        //      fskupdInherited - inherited update info
        // ------------------------------------------------------------------
        internal virtual void ValidateVisual(PTS.FSKUPDATE fskupdInherited) { }

        // ------------------------------------------------------------------
        // Updates the para content with current viewport
        //
        // ------------------------------------------------------------------
        internal virtual void UpdateViewport(ref PTS.FSRECT viewport) { }

        // ------------------------------------------------------------------
        // Create paragraph result representing this paragraph.
        // ------------------------------------------------------------------
        internal abstract ParagraphResult CreateParagraphResult();

        // ------------------------------------------------------------------
        // Return TextContentRange for the content of the paragraph.
        // ------------------------------------------------------------------
        internal abstract TextContentRange GetTextContentRange();

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        // ------------------------------------------------------------------
        // Visual associated with paragraph
        // ------------------------------------------------------------------
        internal virtual ParagraphVisual Visual
        {
            get
            {
                if (_visual == null)
                {
                    _visual = new ParagraphVisual();
                }
                return _visual;
            }
        }

        // ------------------------------------------------------------------
        // Is this the first chunk of paginated content.
        // ------------------------------------------------------------------
        internal virtual bool IsFirstChunk { get { return true; } }

        // ------------------------------------------------------------------
        // Is this the last chunk of paginated content.
        // ------------------------------------------------------------------
        internal virtual bool IsLastChunk { get { return true; } }

        // ------------------------------------------------------------------
        // Paragraph owner of the ParaClient.
        // ------------------------------------------------------------------
        internal BaseParagraph Paragraph { get { return _paragraph; } }

        // ------------------------------------------------------------------
        // Rect of para client
        // ------------------------------------------------------------------
        internal PTS.FSRECT Rect { get { return _rect; } } 

        internal FlowDirection ThisFlowDirection { get { return _flowDirection; } }
        internal FlowDirection ParentFlowDirection { get { return _flowDirectionParent; } }
        internal FlowDirection PageFlowDirection { get { return Paragraph.StructuralCache.PageFlowDirection; } }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        // ------------------------------------------------------------------
        // Arrange paragraph.
        // ------------------------------------------------------------------
        protected virtual void OnArrange() 
        { 
            Paragraph.UpdateLastFormatPositions();
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Fields
        //
        //-------------------------------------------------------------------

        #region Protected Fields

        // ------------------------------------------------------------------
        // Paragraph owner of the ParaClient.
        // ------------------------------------------------------------------
        protected readonly BaseParagraph _paragraph;

        // ------------------------------------------------------------------
        // PTS paragraph handle.
        // ------------------------------------------------------------------
        protected SecurityCriticalDataForSet<IntPtr> _paraHandle;

        // ------------------------------------------------------------------
        // Rectangle occupied by this portion of the paragraph (relative
        // to the page).
        // ------------------------------------------------------------------
        protected PTS.FSRECT _rect;

        // ------------------------------------------------------------------
        // TopSpace value for the paragraph (margin accumulated 
        // during margin collapsing process).
        // ------------------------------------------------------------------
        protected int _dvrTopSpace;

        // ------------------------------------------------------------------
        // Associated visual.
        // ------------------------------------------------------------------
        protected ParagraphVisual _visual;

        // ------------------------------------------------------------------
        // Page context
        // ------------------------------------------------------------------
        protected PageContext _pageContext;

        // ------------------------------------------------------------------
        // Cached flow directions
        // ------------------------------------------------------------------
        protected FlowDirection _flowDirectionParent;
        protected FlowDirection _flowDirection;

        #endregion Protected Fields
    }
}
