// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helpers to handle unexpected situations.
//


using System;
using System.Globalization;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Helpers to handle unexpected situations.
    // ----------------------------------------------------------------------
    internal static class ErrorHandler
    {
        // ------------------------------------------------------------------
        // Assert.
        // ------------------------------------------------------------------
        internal static void Assert(bool condition, string message)
        {
#if DEBUG
            if (!condition)
            {
                System.Diagnostics.Debug.Assert(false, message);
            }
#endif
        }

        // ------------------------------------------------------------------
        // Assert.
        // ------------------------------------------------------------------
        internal static void Assert(bool condition, string format, params object[] args)
        {
#if DEBUG
            if (!condition)
            {
                string message = String.Format(CultureInfo.InvariantCulture, format, args);
                System.Diagnostics.Debug.Assert(false, message);
            }
#endif
        }

        // ------------------------------------------------------------------
        // Initialization errors
        // ------------------------------------------------------------------
        internal static string PtsCacheAlreadyCreated           = "PTS cache is already created.";
        internal static string PtsCacheAlreadyDestroyed         = "PTS cache is already destroyed.";
        internal static string NullPtsHost                      = "Valid PtsHost object is required.";
        internal static string CreateContextFailed              = "Failed to create PTS Context.";
        //internal static string ParagraphVisualDetached          = "Paragraph's visual has been already detached.";
        internal static string EnumIntegrationError             = "Some enum values has been changed. Need to update dependent code.";
        //internal static string FinitePageNotCreated             = "Finite page has not been created - need to handle it?";
        //internal static string SubpageFormatMismatch            = "Nested subpage does not match requested page format.";
        //internal static string PageSizeIsZero                   = "Requested page size is 0.";
        //internal static string CannotUpdateFromDirtyBreakRecord = "Cannot create page from dirty break record.";
        internal static string NoNeedToDestroyPtsPage = "PTS page is not created, there is no need to destroy it.";

        // ------------------------------------------------------------------
        // Not supported errors
        // ------------------------------------------------------------------
        //internal static string NotSupportedSimpleTreeForTFP     = "Simple tree is not supported by TextFlowPresenter.";
        //internal static string NotSupportedWritingDirections    = "Writing directions are not supported yet.";
        internal static string NotSupportedFiniteIncremental    = "Incremental update is not supported yet.";
        internal static string NotSupportedFootnotes            = "Footnotes are not supported yet.";
        //internal static string NotSupportedHeadersFooters       = "Headers/footers are not supported yet.";
        //internal static string NotSupportedVerticalJustify      = "Vertical justification is not supported yet.";
        internal static string NotSupportedCompositeColumns     = "Composite columns are not supported yet.";
        //internal static string NotSupportedAdvancedLine         = "Advanced line features are not supported yet.";
        //internal static string NotSupportedOnLayoutTask         = "OnLayoutTask is not supported yet.";
        internal static string NotSupportedDropCap              = "DropCap is not supported yet.";
        internal static string NotSupportedForcedLineBreaks     = "Forced vertical line break is not supported yet.";
        internal static string NotSupportedMultiSection         = "Multiply sections are not supported yet.";
        internal static string NotSupportedSubtrackShift        = "Column shifting is not supported yet.";

        // ------------------------------------------------------------------
        // Handle mapper
        // ------------------------------------------------------------------
        internal static string NullObjectInCreateHandle         = "Valid object is required to create handle.";
        internal static string InvalidHandle                    = "No object associated with the handle or type mismatch.";
        internal static string HandleOutOfRange                 = "Object handle has to be within handle array's range.";
        //internal static string HandleAlreadyDisposed            = "UnmanagedHandle has been already disposed.";
        //internal static string EmptyHandleArrray                = "Handle array cannot be empty.";
        //internal static string FreeIndexOutOfRange              = "Index of the next free entry in handle array is out of range.";
        //internal static string NoNeedToResizeHandleArray        = "There is no need to resize handle array. Free entries are still available.";

        // ------------------------------------------------------------------
        // Break record
        // ------------------------------------------------------------------
        //internal static string NullPresenterCache               = "Valid PresenterCache is required.";
        //internal static string NullElementOwner                 = "Valid Element is required.";
        internal static string BreakRecordDisposed              = "Break record already disposed.";
        //internal static string InconsistentElementOwner         = "Inconsistent element owner.";
        //internal static string TFPInvalidNotificationHandler    = "NotificationHandler is null, or it does not belong to TextFlowPresenter.";
        //internal static string NullNotificationSource           = "Notification source is not specified.";
        internal static string BreakRecordNotNeeded             = "There is no need to create break record.";
        internal static string BrokenParaHasMcs                 = "Broken paragraph cannot have margin collapsing state.";
        internal static string BrokenParaHasTopSpace            = "Top space should be always suppressed at the top of broken paragraph.";
        internal static string GoalReachedHasBreakRecord        = "Goal is reached, so there should be no break record.";
        internal static string BrokenContentRequiresBreakRecord = "Goal is not reached, break record is required to continue.";

        // ------------------------------------------------------------------
        // NameTable PTS structures errors
        // ------------------------------------------------------------------
        internal static string PTSAssert                        = "PTS Assert:\n\t{0}\n\t{1}\n\t{2}\n\t{3}";
        //internal static string NullPresenter                    = "Valid Presenter object is required.";
        //internal static string NullParagraph                    = "Valid Paragraph object is required.";
        //internal static string NullParaClient                   = "Valid ParaClient object is required.";
        //internal static string NullFragment                     = "Valid Fragment object is required.";
        //internal static string InvalidParaClientCache           = "ParaClient cache has not been updated yet. Forgot to call UpdateCache?";
        internal static string ParaHandleMismatch               = "Paragraph handle mismatch.";
        //internal static string FragmentNotDetached              = "Fragment object has not been detached.";
        //internal static string FragmentAlreadyDetached          = "Fragment has been already detached.";
        //internal static string BBoxNotDefined                   = "Bounding box is not defined.";
        //internal static string UseSimpleQueryPath               = "Simple query path should be used.";
        //internal static string UseComplexQueryPath              = "Complex query path should be used.";
        //internal static string UseComplexCompositeQueryPath     = "Complex composite query path should be used.";
        internal static string PTSObjectsCountMismatch          = "Actual number of PTS objects does not match number of requested PTS objects.";
        //internal static string PageAlreadyDestroyed             = "Page has been already destroyed.";
        //internal static string NullChildPresenter               = "ChildPresenter has not been created.";
        //internal static string InconsistentDataForBrokenPara    = "Inconsistent data for broken paragraph.";
        //internal static string IncorrectPositionOfTreePtr       = "TreePtr has not been properly positioned.";
        internal static string SubmitForEmptyRange              = "Submitting embedded objects for empty range.";
        internal static string SubmitInvalidList                = "Submitting invalid list of embedded objects.";
        //internal static string SubmitInvalidObjectType          = "Submitting invalid embedded object type.";
        //internal static string LineNotHit                       = "The current line should be hit.";
        //internal static string NoObjectAtDcp                    = "No object has been found for specified 'dcp'.";
        //internal static string InvalidArg                       = "Invalid argument.";
        //internal static string EmptyLine                        = "Created line is empty.";
        //internal static string ParaRectOutOfSync                = "Paragraph rect out of sync.";
        internal static string HandledInsideSegmentPara         = "Paragraph structure invalidation should be handled by Segments.";
        internal static string EmptyParagraph                   = "There are no lines in the paragraph.";
        //internal static string UnsuportedParagraphBreak         = "Returned paragraph break is not supported.";
        internal static string ParaStartsWithEOP                = "NameTable is out of sync with TextContainer. The next paragraph begins with EOP.";
        internal static string FetchParaAtTextRangePosition     = "Trying to fetch paragraph at not supported TextPointer - TextRange.";
        internal static string ParagraphCharacterCountMismatch  = "Paragraph's character count is out of sync.";
        internal static string ContainerNeedsTextElement        = "Container paragraph can be only created for TextElement.";
        internal static string CannotPositionInsideUIElement    = "Cannot position TextPointer inside a UIElement.";
        internal static string CannotFindUIElement              = "Cannot find specified UIElement in the TextContainer.";
        internal static string InvalidDocumentPage              = "DocumentPage is not created for IDocumentPaginatorSource object.";

        // ------------------------------------------------------------------
        // Incremental update errors
        // ------------------------------------------------------------------
        //internal static string InconsistentUpdateRecordData     = "Inconsistent UpdateRecord data.";
        //internal static string NullUpdateRecord                 = "Valid UpdateRecord object is required.";
        //internal static string UpdateRecordAlreadyCreated       = "UpdateRecord has been already created.";
        //internal static string EmptyDTRList                     = "DTR list is empty.";
        //internal static string NullNextUpdateRecord             = "Next UpdateRecord cannot be null.";
        //internal static string DTRListOutOfSync                 = "DTRList is out of sync.";
        //internal static string DtrOutOfScope                    = "DTR is exciding presenter scope.";
        internal static string NoVisualToTransfer               = "Old paragraph does not have a visual node. Cannot transfer data.";
        internal static string UpdateShiftedNotValid            = "Update shifted is not a valid update type for top level PTS objects.";
        internal static string ColumnVisualCountMismatch        = "Number of column visuals does not match number of columns.";
        internal static string VisualTypeMismatch               = "Visual does not match expected type.";

        // ------------------------------------------------------------------
        // Line formatting
        // ------------------------------------------------------------------
        internal static string EmbeddedObjectTypeMismatch       = "EmbeddedObject type missmatch.";
        internal static string EmbeddedObjectOwnerMismatch      = "Cannot transfer data from an embedded object representing another element.";
        //internal static string ElementOwnerMismatch             = "Element owner mismatch.";
        //internal static string UnsuportedPositionType           = "Unsupported position type.";
        //internal static string ObjectRunNotFound                = "There is no object run in the cache. Why FormatObject has been called?";
        //internal static string ObjectRunExpected                = "Only object run is expected in FormatObject.";
        //internal static string NullChildPresenterInRenderMode   = "ChildPresenter cannot be null in render mode.";
        //internal static string LLLineCreationFailed             = "Failed to create a line.";
        internal static string LineAlreadyDestroyed             = "Line has been already disposed.";
        internal static string OnlyOneRectIsExpected            = "Expecting only one rect for text object.";
        //internal static string RenderOnlyMode                   = "Supported only during render mode.";
        //internal static string NoFloatersInLine                 = "There are no floaters in the line.";
        //internal static string FloatersNumberMismatch           = "Number of floaters doesn't match size of PTS array.";
        //internal static string NoFiguresInLine                  = "There are no figures in the line.";
        //internal static string FiguresNumberMismatch            = "Number of figures doesn't match size of PTS array.";
        //internal static string NullLineLayoutHostContext        = "Context for LineLayoutHost has not been set.";
        internal static string NotInLineBoundary                = "Requesting data outside of line's range.";
        internal static string FetchRunAtTextArrayStart         = "Trying to fetch run at the beginning of TextContainer.";
        internal static string TextFormatterHostNotInitialized  = "TextFormatter host is not initialized.";
        internal static string NegativeCharacterIndex           = "Character index must be non-negative.";
        internal static string NoClientDataForObjectRun         = "ClientData should be always provided for object runs.";
        internal static string UnknownDOTypeInTextArray         = "Unknown DependencyObject type stored in TextContainer.";
        internal static string NegativeObjectWidth              = "Negative object's width within a text line.";
        internal static string NoUIElementForObjectPosition     = "TextContainer does not have a UIElement for position of Object type.";
        internal static string InlineObjectCacheCorrupted       = "Paragraph's inline object cache is corrupted.";
    }
}
