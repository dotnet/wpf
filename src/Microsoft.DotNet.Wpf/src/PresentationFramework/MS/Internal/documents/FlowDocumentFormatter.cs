// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Bottomless content formatter associated with FlowDocument.
//

using System;                       // Object
using System.Windows;               // Size
using System.Windows.Documents;     // FlowDocument
using System.Windows.Media;         // Visual
using System.Windows.Threading;     // DispatcherOperationCallback
using MS.Internal.PtsHost;          // FlowDocumentPage
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.Documents
{
    /// <summary>
    /// Bottomless content formatter associated with FlowDocument.
    /// </summary>
    internal class FlowDocumentFormatter : IFlowDocumentFormatter
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal FlowDocumentFormatter(FlowDocument document)
        {
            _document = document;
            _documentPage = new FlowDocumentPage(_document.StructuralCache);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Formatts content.
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        internal void Format(Size constraint)
        {
            Thickness pageMargin;
            Size pageSize;

            // Reentrancy check.
            if (_document.StructuralCache.IsFormattingInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentFormattingReentrancy));
            }
            if (_document.StructuralCache.IsContentChangeInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }

            // Check if we can continue with formatting without nuking incremental udpate info.
            if (_document.StructuralCache.IsFormattedOnce)
            {
                if (!_lastFormatSuccessful)
                {
                    // We cannot resolve update info if last formatting was unsuccessful.
                    _document.StructuralCache.InvalidateFormatCache(true);
                }
                if (!_arrangedAfterFormat && (!_document.StructuralCache.ForceReformat || !_document.StructuralCache.DestroyStructure))
                {
                    // Need to clear update info by running arrange process.
                    // This is necessary, because Format may be called more than once
                    // before Arrange is called. But PTS is not able to merge update info.
                    // To protect against loosing incremental changes delta, need
                    // to arrange the page and create all necessary visuals.
                    _documentPage.Arrange(_documentPage.ContentSize);
                    _documentPage.EnsureValidVisuals();
                }
            }
            _arrangedAfterFormat = false;
            _lastFormatSuccessful = false;
            _isContentFormatValid = false;

            pageSize = ComputePageSize(constraint);
            pageMargin = ComputePageMargin();

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
            using (_document.Dispatcher.DisableProcessing())
            {
                _document.StructuralCache.IsFormattingInProgress = true; // Set reentrancy flag.
                try
                {
                    _document.StructuralCache.BackgroundFormatInfo.ViewportHeight = constraint.Height;
                    _documentPage.FormatBottomless(pageSize, pageMargin);
                }
                finally
                {
                    _document.StructuralCache.IsFormattingInProgress = false; // Clear reentrancy flag.
                }
            }
            _lastFormatSuccessful = true;
        }

        /// <summary>
        /// Arranges content.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        /// <param name="viewport">Viewport for visible content.</param>
        internal void Arrange(Size arrangeSize, Rect viewport)
        {
            Invariant.Assert(_document.StructuralCache.DtrList == null || _document.StructuralCache.DtrList.Length == 0 ||
                             (_document.StructuralCache.DtrList.Length == 1 && _document.StructuralCache.BackgroundFormatInfo.DoesFinalDTRCoverRestOfText));

            // Arrange the content and create visual tree.
            _documentPage.Arrange(arrangeSize);
            _documentPage.EnsureValidVisuals();
            _arrangedAfterFormat = true;

            // Render content only for the current viewport.
            if (viewport.IsEmpty)
            {
                viewport = new Rect(0, 0, arrangeSize.Width, _document.StructuralCache.BackgroundFormatInfo.ViewportHeight);
            }
            PTS.FSRECT fsrectViewport = new PTS.FSRECT(viewport);
            _documentPage.UpdateViewport(ref fsrectViewport, true);

            _isContentFormatValid = true;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// DocumentPage representing formatted content.
        /// </summary>
        internal FlowDocumentPage DocumentPage
        {
            get
            {
                return _documentPage;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Events
        //
        //-------------------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Fired when content has been invalidated.
        /// </summary>
        internal event EventHandler ContentInvalidated;

        /// <summary>
        /// Fired when formatter has been suspended.
        /// </summary>
        internal event EventHandler Suspended;

        #endregion Internal Events

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Compute size for the page.
        /// </summary>
        private Size ComputePageSize(Size constraint)
        {
            double max, min;
            Size pageSize = new Size(_document.PageWidth, double.PositiveInfinity);
            if (DoubleUtil.IsNaN(pageSize.Width))
            {
                pageSize.Width = constraint.Width;
                max = _document.MaxPageWidth;
                if (pageSize.Width > max)
                {
                    pageSize.Width = max;
                }
                min = _document.MinPageWidth;
                if (pageSize.Width < min)
                {
                    pageSize.Width = min;
                }
            }
            // If the width is Double.PositiveInfinity, crop it to predefined value.
            if (double.IsPositiveInfinity(pageSize.Width))
            {
                pageSize.Width = _defaultWidth;
            }
            return pageSize;
        }

        /// <summary>
        /// Compute margin for the page.
        /// </summary>
        private Thickness ComputePageMargin()
        {
            double lineHeight = MS.Internal.Text.DynamicPropertyReader.GetLineHeightValue(_document);
            Thickness pageMargin = _document.PagePadding;

            // If Padding value is 'Auto', treat it as 1*LineHeight.
            if (DoubleUtil.IsNaN(pageMargin.Left))
            {
                pageMargin.Left = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Top))
            {
                pageMargin.Top = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Right))
            {
                pageMargin.Right = lineHeight;
            }
            if (DoubleUtil.IsNaN(pageMargin.Bottom))
            {
                pageMargin.Bottom = lineHeight;
            }
            return pageMargin;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// FlowDocument associated with the paginator.
        /// </summary>
        private readonly FlowDocument _document;

        /// <summary>
        /// DocumentPage representing formatted content.
        /// </summary>
        private FlowDocumentPage _documentPage;

        /// <summary>
        /// Whether Arrange was called after formatting.
        /// </summary>
        private bool _arrangedAfterFormat;

        /// <summary>
        /// Whether last formatting was succesful.
        /// </summary>
        private bool _lastFormatSuccessful;

        /// <summary>
        /// Width used when no width is specified.
        /// </summary>
        private const double _defaultWidth = 500.0;

        /// <summary>
        /// Whether the current format for the content is valid
        /// </summary>
        private bool _isContentFormatValid = false;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IFlowDocumentFormatter Members
        //
        //-------------------------------------------------------------------

        #region IFlowDocumentFormatter Members

        /// <summary>
        /// Responds to change affecting entire content of associated FlowDocument.
        /// </summary>
        /// <param name="affectsLayout">Whether change affects layout.</param>
        void IFlowDocumentFormatter.OnContentInvalidated(bool affectsLayout)
        {
            // If change happens before we've been arranged, we need to do a full reformat
            if (affectsLayout)
            {
                if (!_arrangedAfterFormat)
                {
                    _document.StructuralCache.InvalidateFormatCache(true);
                }
                _isContentFormatValid = false;
            }

            if (ContentInvalidated != null)
            {
                ContentInvalidated(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Responds to change affecting entire content of associated FlowDocument.
        /// </summary>
        /// <param name="affectsLayout">Whether change affects layout.</param>
        /// <param name="start">Start of the affected content range.</param>
        /// <param name="end">End of the affected content range.</param>
        void IFlowDocumentFormatter.OnContentInvalidated(bool affectsLayout, ITextPointer start, ITextPointer end)
        {
            ((IFlowDocumentFormatter)this).OnContentInvalidated(affectsLayout);
        }

        /// <summary>
        /// Suspend formatting.
        /// </summary>
        void IFlowDocumentFormatter.Suspend()
        {
            if (Suspended != null)
            {
                Suspended(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Is layout data in a valid state.
        /// </summary>
        bool IFlowDocumentFormatter.IsLayoutDataValid
        {
            get
            {
                // Layout is clean only when the page is calculated and it
                // is in the clean state - there are no pending changes that affect layout.
                //
                // Hittest can be called with invalid arrange. This happens in
                // following situation:
                //   Something is causing to call InvalidateTree and eventually 
                //   InvalidateAllProperties will be called in such case. In responce
                //   to that following properties are invalidated:
                //   * ClipToBounds - invalidates arrange
                //   * IsEnabled - calls MouseDevice.Synchronize and it will eventually
                //     do hittesting.
                //
                // OR
                //   TextContainer sends Changing event, which invalidates measure,
                //   but we have not yet received a matching Changed event.
                //
                // So, it is possible to receive hittesting request on dirty layout.
                bool layoutValid = _documentPage != null &&
                    _document.StructuralCache.IsFormattedOnce &&
                    !_document.StructuralCache.ForceReformat &&
                    _isContentFormatValid &&
                    !_document.StructuralCache.IsContentChangeInProgress &&
                    !_document.StructuralCache.IsFormattingInProgress;

                return layoutValid;
            }
        }

        #endregion IFlowDocumentFormatter Members
    }
}
