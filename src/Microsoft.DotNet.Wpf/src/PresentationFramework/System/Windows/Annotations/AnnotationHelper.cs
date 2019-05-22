// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Xml;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Anchoring;
using MS.Internal.Annotations.Component;
using MS.Internal.Documents;
using MS.Utility;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     AnnotationHelper
    /// </summary>
    public static class AnnotationHelper
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Creates a highlight annotation for the service.  The anchor used is the
        ///     current selection in the DocumentViewerBase associated with the service.
        ///     If the selection length is 0 no highlight is created.
        /// </summary>
        /// <param name="service">service used to create annotation</param>
        /// <param name="author">annotation author added to new annotation; if null, no author added</param>
        /// <param name="highlightBrush">the brush to use when drawing the highlight; if null, uses default highlight brush</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled or highlightBrush is not a SolidColorBrush</exception>
        /// <exception cref="InvalidOperationException">selection is of zero length</exception>
        /// <returns>the Annotation created</returns>
        public static Annotation CreateHighlightForSelection(AnnotationService service, string author, Brush highlightBrush)
        {
            Annotation highlight = null;
            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.CreateHighlightBegin);

            try
            {
                highlight = Highlight(service, author, highlightBrush, true);
                Invariant.Assert(highlight != null, "Highlight not returned from create call.");
            }
            finally
            {
                //fire end trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.CreateHighlightEnd);
            }
            return highlight;
        }

        /// <summary>
        ///     Creates a text sticky note annotation for the service.  The anchor used is
        ///     the current selection in the DocumentViewerBase associated with the service.
        ///     If the selection length is 0 no sticky note is created.
        /// </summary>
        /// <param name="service">service used to create annotation</param>
        /// <param name="author">annotation author added to new annotation; if null, no author added</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        /// <exception cref="InvalidOperationException">selection is of zero length</exception>
        /// <returns>the Annotation created</returns>
        public static Annotation CreateTextStickyNoteForSelection(AnnotationService service, string author)
        {
            return CreateStickyNoteForSelection(service, StickyNoteControl.TextSchemaName, author);
        }

        /// <summary>
        ///     Creates an ink sticky note annotation for the service.  The anchor used is
        ///     the current selection in the DocumentViewerBase associated with the service.
        ///     If the selection length is 0 no sticky note is created.
        /// </summary>
        /// <param name="service">service used to create annotation</param>
        /// <param name="author">annotation author added to new annotation; if null, no author added</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        /// <exception cref="InvalidOperationException">selection is of zero length</exception>
        /// <returns>the Annotation created</returns>
        public static Annotation CreateInkStickyNoteForSelection(AnnotationService service, string author)
        {
            return CreateStickyNoteForSelection(service, StickyNoteControl.InkSchemaName, author);
        }

        /// <summary>
        ///     Clears all highlights that overlap with the service's viewer's selection.
        ///     If a highlight overlaps the selection only partially, the portion
        ///     that is overlapping is cleared.  The rest of it remains.
        ///     If no highlights overlap the selection, this method is a no-op.
        ///     If the selection is of zero length, this method is a no-op.
        /// </summary>
        /// <param name="service">service from which to delete annotations</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        public static void ClearHighlightsForSelection(AnnotationService service)
        {
            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.ClearHighlightBegin);
            try
            {
                Highlight(service, null, null, false);
            }
            finally
            {
                //fire end trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.ClearHighlightEnd);
            }
        }

        /// <summary>
        ///     Deletes ink sticky notes whose anchors are wholly contained by the
        ///     service's viewer's selection.  If no anchors are wholly contained
        ///     by the selection, then this method is a no-op.
        /// </summary>
        /// <param name="service">service from which to delete annotations</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        public static void DeleteTextStickyNotesForSelection(AnnotationService service)
        {
            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteTextNoteBegin);
            try
            {
                DeleteSpannedAnnotations(service, StickyNoteControl.TextSchemaName);
            }
            finally
            {
                //fire end trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteTextNoteEnd);
            }
        }

        /// <summary>
        ///     Deletes ink sticky notes whose anchors are wholly contained by the
        ///     service's viewer's selection.  If no anchors are wholly contained
        ///     by the selection, then this method is a no-op.
        /// </summary>
        /// <param name="service">service from which to delete annotations</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        public static void DeleteInkStickyNotesForSelection(AnnotationService service)
        {
            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteInkNoteBegin);
            try
            {
                DeleteSpannedAnnotations(service, StickyNoteControl.InkSchemaName);
            }
            finally
            {
                //fire end trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeleteInkNoteEnd);
            }
        }

        /// <summary>
        ///     Gets the AttachedAnnotation for any annotation, even if its not visible.
        /// </summary>
        /// <param name="service">service from which to resolve annotations</param>
        /// <param name="annotation">annotation to get anchor info for</param>
        /// <exception cref="ArgumentNullException">service or annotation is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        public static IAnchorInfo GetAnchorInfo(AnnotationService service, Annotation annotation)
        {
            CheckInputs(service);

            if (annotation == null)
                throw new ArgumentNullException("annotation");

            bool isFlow = true;

            // Determine if we are using a viewer that supports pagination
            DocumentViewerBase viewer = service.Root as DocumentViewerBase;
            if (viewer == null)
            {
                FlowDocumentReader fdr = service.Root as FlowDocumentReader;
                if (fdr != null)
                {
                    viewer = GetFdrHost(fdr) as DocumentViewerBase;
                }
            }
            else
            {
                // Only paginated viewers support non-FlowDocuments, so
                // if we have one, check its content type
                isFlow = viewer.Document is FlowDocument;
            }

            IList<IAttachedAnnotation> attachedAnnotations = null;


            // Use the method specific to the kind of content we are displaying
            if (isFlow)
            {
                TextSelectionProcessor rangeProcessor = service.LocatorManager.GetSelectionProcessor(typeof(TextRange)) as TextSelectionProcessor;
                TextSelectionProcessor anchorProcessor = service.LocatorManager.GetSelectionProcessor(typeof(TextAnchor)) as TextSelectionProcessor;
                Invariant.Assert(rangeProcessor != null, "TextSelectionProcessor should be available for TextRange if we are processing flow content.");
                Invariant.Assert(anchorProcessor != null, "TextSelectionProcessor should be available for TextAnchor if we are processing flow content.");

                try
                {
                    // Turn resolving for non-visible content on
                    rangeProcessor.Clamping = false;
                    anchorProcessor.Clamping = false;

                    attachedAnnotations = ResolveAnnotations(service, new Annotation[] { annotation });
                }
                finally
                {
                    // Turn resolving of non-visible content off again
                    rangeProcessor.Clamping = true;
                    anchorProcessor.Clamping = true;
                }
            }
            else
            {
                FixedPageProcessor processor = service.LocatorManager.GetSubTreeProcessorForLocatorPart(FixedPageProcessor.CreateLocatorPart(0)) as FixedPageProcessor;
                Invariant.Assert(processor != null, "FixedPageProcessor should be available if we are processing fixed content.");

                try
                {
                    // Turn resolving of non-visible anchors on
                    processor.UseLogicalTree = true;

                    attachedAnnotations = ResolveAnnotations(service, new Annotation[] { annotation });
                }
                finally
                {
                    // Turn resolving of non-visible anchors off again
                    processor.UseLogicalTree = false;
                }
            }


            Invariant.Assert(attachedAnnotations != null);
            if (attachedAnnotations.Count > 0)
                return attachedAnnotations[0];

            return null;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Handle the CreateHighlightCommand by calling helper method.
        /// Pass in the command parameter if there was one.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnCreateHighlightCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DependencyObject viewer = sender as DependencyObject;
            if (viewer != null)
            {
                CreateHighlightForSelection(AnnotationService.GetService(viewer), null, e.Parameter != null ? e.Parameter as Brush : null);
            }
        }

        /// <summary>
        /// Handle the CreateTextStickyNoteCommand by calling helper method.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnCreateTextStickyNoteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DependencyObject viewer = sender as DependencyObject;
            if (viewer != null)
            {
                CreateTextStickyNoteForSelection(AnnotationService.GetService(viewer), e.Parameter as String);
            }
        }

        /// <summary>
        /// Handle the CreateInkStickyNoteCommand by calling helper method.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnCreateInkStickyNoteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DependencyObject viewer = sender as DependencyObject;
            if (viewer != null)
            {
                CreateInkStickyNoteForSelection(AnnotationService.GetService(viewer), e.Parameter as String);
            }
        }

        /// <summary>
        /// Handle the ClearHighlightsCommand by calling helper method.
        /// Pass in the command parameter if there was one.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnClearHighlightsCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DependencyObject viewer = sender as DependencyObject;
            if (viewer != null)
            {
                ClearHighlightsForSelection(AnnotationService.GetService(viewer));
            }
        }

        /// <summary>
        /// Handle the DeleteStickyNotesCommand by calling helper method.
        /// Pass in the command parameter if there was one.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnDeleteStickyNotesCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DependencyObject viewer = sender as DependencyObject;
            if (viewer != null)
            {
                DeleteTextStickyNotesForSelection(AnnotationService.GetService(viewer));
                DeleteInkStickyNotesForSelection(AnnotationService.GetService(viewer));
            }
        }

        /// <summary>
        /// Handle the DeleteAnnotationsCommand by calling helper method.
        /// Pass in the command parameter if there was one.
        /// </summary>
        /// <param name="sender">viewer the command is being executed on</param>
        /// <param name="e">args for the command</param>
        internal static void OnDeleteAnnotationsCommand(object sender, ExecutedRoutedEventArgs e)
        {
            FrameworkElement viewer = sender as FrameworkElement;
            if (viewer != null)
            {
                // Only clear highlights if the selection is not empty.
                ITextSelection selection = GetTextSelection(viewer);
                if (selection != null)
                {
                    AnnotationService service = AnnotationService.GetService(viewer);
                    DeleteTextStickyNotesForSelection(service);
                    DeleteInkStickyNotesForSelection(service);
                    //ClearHighlightsForSelection will clear the selection, so it should be called last
                    if (!selection.IsEmpty)
                    {
                        ClearHighlightsForSelection(service);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the CreateHighlightCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryCreateHighlightCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, true);
            e.Handled = true;
        }

        /// <summary>
        /// Determines if the CreateTextStickyNoteCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryCreateTextStickyNoteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, true);
            e.Handled = true;
        }

        /// <summary>
        /// Determines if the CreateInkStickyNoteCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryCreateInkStickyNoteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, true);
            e.Handled = true;
        }

        /// <summary>
        /// Determines if the ClearHighlightsCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryClearHighlightsCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, true);
            e.Handled = true;
        }

        /// <summary>
        /// Determines if the DeleteStickyNotesCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryDeleteStickyNotesCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, false);
            e.Handled = true;
        }

        /// <summary>
        /// Determines if the DeleteAnnotationsCommand should be enabled on the
        /// specified DocumentViewerBase.
        /// </summary>
        /// <param name="sender">the viewer the command would be enabled on</param>
        /// <param name="e">parameters to the command</param>
        internal static void OnQueryDeleteAnnotationsCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsCommandEnabled(sender, false);
            e.Handled = true;
        }

        /// <summary>
        /// Gets the DocumentPageView for a particular page if any
        /// </summary>
        /// <param name="viewer">DocumentViewer</param>
        /// <param name="pageNb">pageNb</param>
        /// <returns>DocumentPageView; null if page is not loaded</returns>
        internal static DocumentPageView FindView(DocumentViewerBase viewer, int pageNb)
        {
            Invariant.Assert(viewer != null, "viewer is null");
            Invariant.Assert(pageNb >= 0, "negative pageNb");

            foreach (DocumentPageView view in viewer.PageViews)
            {
                if (view.PageNumber == pageNb)
                    return view;
            }

            return null;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Creates a sticky note annotation of the specified type for the service.  The anchor
        ///     used is the current selection in the DocumentViewerBase associated with the service.
        ///     If the selection length is 0 no sticky note is created.
        /// </summary>
        /// <param name="service">service in which to create annotation</param>
        /// <param name="noteType">type of StickyNote to create</param>
        /// <param name="author">optional author of new annotation</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        private static Annotation CreateStickyNoteForSelection(AnnotationService service, XmlQualifiedName noteType, string author)
        {
            CheckInputs(service);

            // Get the selection for the viewer
            ITextSelection selection = GetTextSelection((FrameworkElement)service.Root);
            Invariant.Assert(selection != null, "TextSelection is null");

            // Cannot create an annotation with zero length text anchor
            if (selection.IsEmpty)
            {
                throw new InvalidOperationException(SR.Get(SRID.EmptySelectionNotSupported));
            }

            Annotation annotation = null;

            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.CreateStickyNoteBegin);
            try
            {
                // Create the annotation
                annotation = CreateAnnotationForSelection(service, selection, noteType, author);
                Invariant.Assert(annotation != null, "CreateAnnotationForSelection returned null.");

                // Add annotation to the store - causing it to be resolved and displayed, if possible
                service.Store.AddAnnotation(annotation);

                // Clear the selection
                selection.SetCaretToPosition(selection.MovingPosition, selection.MovingPosition.LogicalDirection, true, true);
            }
            finally
            {
                //fire end trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.CreateStickyNoteEnd);
            }
            return annotation;
        }

        /// <summary>
        /// Checks if all of the pages in a given range are currently visible,
        /// and if yes - returns true
        /// </summary>
        /// <param name="viewer">the viewer</param>
        /// <param name="startPage">first page in range to check for visibility</param>
        /// <param name="endPage">last page in range to check for visibility</param>
        /// <returns>true all pages in the range are visible, false otherwise</returns>

        private static bool AreAllPagesVisible(DocumentViewerBase viewer, int startPage, int endPage)
        {
            Invariant.Assert(viewer != null, "viewer is null.");
            Invariant.Assert(endPage >= startPage, "EndPage is less than StartPage");

            bool pageVisibility = true;

            //if visible pages are less than selected then we have invisible pages
            // for sure
            if (viewer.PageViews.Count <= endPage - startPage)
                return false;

            for (int page = startPage; page <= endPage; page++)
            {
                if (FindView(viewer, page) == null)
                {
                    //there is at least one invisible page
                    pageVisibility = false;
                    break;
                }
            }

            return pageVisibility;
        }


        private static IList<IAttachedAnnotation> GetSpannedAnnotations(AnnotationService service)
        {
            CheckInputs(service);

            bool isFlow = true;

            // Determine if we are using a viewer that supports pagination
            DocumentViewerBase viewer = service.Root as DocumentViewerBase;
            if (viewer == null)
            {
                FlowDocumentReader fdr = service.Root as FlowDocumentReader;
                if (fdr != null)
                {
                    viewer = GetFdrHost(fdr) as DocumentViewerBase;
                }
            }
            else
            {
                // Only paginated viewers support non-FlowDocuments, so
                // if we have one, check its content type
                isFlow = viewer.Document is FlowDocument;
            }

            bool allPagesVisible = true;

            ITextSelection selection = GetTextSelection((FrameworkElement)service.Root);
            Invariant.Assert(selection != null, "TextSelection is null");
            int selStartPage = 0, selEndPage = 0;

            if(viewer != null)
            {
                //if this is a DocumentViewerBase check the selection pages
                TextSelectionHelper.GetPointerPage(selection.Start, out selStartPage);
                TextSelectionHelper.GetPointerPage(selection.End, out selEndPage);

                // If either page cannot be found, the selection we are trying to anchor to
                // is invalid.  This can happen if the selection was created programmatically
                // for TextPointers that don't have pages because pagination failed.
                if (selStartPage == -1 || selEndPage == -1)
                    throw new ArgumentException(SR.Get(SRID.InvalidSelectionPages));

                allPagesVisible = AreAllPagesVisible(viewer, selStartPage, selEndPage);
            }

            IList<IAttachedAnnotation> attachedAnnotations = null;

            if (allPagesVisible)
            {
                // If viewer is not a DocumentViewerBase or the selection has
                // no parts on non-visible pages, just use the attached annotations
                attachedAnnotations = service.GetAttachedAnnotations();
            }
            else
            {
                // Use the method specific to the kind of content we are displaying
                if (isFlow)
                {
                    attachedAnnotations = GetSpannedAnnotationsForFlow(service, selection);
                }
                else
                {
                    attachedAnnotations = GetSpannedAnnotationsForFixed(service, selStartPage, selEndPage);
                }
            }

            IList<TextSegment> textSegments = selection.TextSegments;
            Debug.Assert((textSegments != null) && (textSegments.Count > 0), "Invalid selection TextSegments");

            if ((attachedAnnotations != null) && (attachedAnnotations.Count > 0))
            {
                if (allPagesVisible || !isFlow)
                {
                    //if the list contains all currently attached annotations or the Annotations
                    // from all visible fixed pages we must remove the annotations that are not
                    // overlapped by the selection. This is not needed for Flow because
                    // GetSpannedAnnotationsForFlow will retrieve only the annotations covered by the selection
                    for (int i = attachedAnnotations.Count - 1; i >= 0; i--)
                    {
                        TextAnchor ta = attachedAnnotations[i].AttachedAnchor as TextAnchor;
                        if ((ta == null) || !ta.IsOverlapping(textSegments))
                        {
                            //remove this attached annotation - it is out of selection scope
                            attachedAnnotations.RemoveAt(i);
                        }
                    }
                }
            }
            return attachedAnnotations;
        }

        /// <summary>
        /// Gets the current viewer of FlowDocumentReader
        /// </summary>
        /// <param name="fdr">FlowDocumentReader</param>
        /// <returns>CurrentViewer - can be any of IFlowDocumentViewer implementations</returns>
        internal static object GetFdrHost(FlowDocumentReader fdr)
        {
            Invariant.Assert(fdr != null, "Null FDR");

            Decorator host = null;
            if (fdr.TemplateInternal != null)
            {
                host = StyleHelper.FindNameInTemplateContent(fdr, "PART_ContentHost", fdr.TemplateInternal) as Decorator;
            }
            return host != null ? host.Child : null;
        }

        private static IList<IAttachedAnnotation> GetSpannedAnnotationsForFlow(AnnotationService service, ITextSelection selection)
        {
            Invariant.Assert(service != null);

            // Expand the range to get annotations that sit just outside the selection - important for merging
            ITextPointer start = selection.Start.CreatePointer();
            ITextPointer end = selection.End.CreatePointer();
            start.MoveToNextInsertionPosition(LogicalDirection.Backward);
            end.MoveToNextInsertionPosition(LogicalDirection.Forward);
            ITextRange textRange = new TextRange(start, end);

            // Create locator that reflects all spanned annotations for the current selection
            IList<ContentLocatorBase> locators = service.LocatorManager.GenerateLocators(textRange);
            Invariant.Assert(locators != null && locators.Count > 0);

            TextSelectionProcessor rangeProcessor = service.LocatorManager.GetSelectionProcessor(typeof(TextRange)) as TextSelectionProcessor;
            TextSelectionProcessor anchorProcessor = service.LocatorManager.GetSelectionProcessor(typeof(TextAnchor)) as TextSelectionProcessor;
            Invariant.Assert(rangeProcessor != null, "TextSelectionProcessor should be available for TextRange if we are processing flow content.");
            Invariant.Assert(anchorProcessor != null, "TextSelectionProcessor should be available for TextAnchor if we are processing flow content.");

            IList<IAttachedAnnotation> attachedAnnotations = null;
            IList<Annotation> annotations = null;
            try
            {
                // Turn resolving for non-visible content on
                rangeProcessor.Clamping = false;
                anchorProcessor.Clamping = false;

                ContentLocator locator = locators[0] as ContentLocator;
                Invariant.Assert(locator != null, "Locators for selection in Flow should always be ContentLocators.  ContentLocatorSets not supported.");

                // Make sure we get all annotations that overlap with the selection
                locator.Parts[locator.Parts.Count - 1].NameValuePairs.Add(TextSelectionProcessor.IncludeOverlaps, Boolean.TrueString);

                // Query for the annotations
                annotations = service.Store.GetAnnotations(locator);

                // Impact to Perf here - we could avoid resolving those already attached annotations
                attachedAnnotations = ResolveAnnotations(service, annotations);
            }
            finally
            {
                // Turn resolving of non-visible content off again
                rangeProcessor.Clamping = true;
                anchorProcessor.Clamping = true;
            }

            return attachedAnnotations;
        }

        private static IList<IAttachedAnnotation> GetSpannedAnnotationsForFixed(AnnotationService service, int startPage, int endPage)
        {
            Invariant.Assert(service != null, "Need non-null service to get spanned annotations for fixed content.");

            FixedPageProcessor processor = service.LocatorManager.GetSubTreeProcessorForLocatorPart(FixedPageProcessor.CreateLocatorPart(0)) as FixedPageProcessor;
            Invariant.Assert(processor != null, "FixedPageProcessor should be available if we are processing fixed content.");

            List<IAttachedAnnotation> attachedAnnotations = null;
            List<Annotation> annotations = new List<Annotation>();
            try
            {
                // Turn resolving of non-visible anchors on
                processor.UseLogicalTree = true;

                // For each non-visible page, query the store for annotations on that page
                for (int pageNumber = startPage; pageNumber <= endPage; pageNumber++)
                {
                    ContentLocator locator = new ContentLocator();
                    locator.Parts.Add(FixedPageProcessor.CreateLocatorPart(pageNumber));
                    AddRange(annotations, service.Store.GetAnnotations(locator));
                }

                attachedAnnotations = ResolveAnnotations(service, annotations);
            }
            finally
            {
                // Turn resolving of non-visible anchors off again
                processor.UseLogicalTree = false;
            }

            return attachedAnnotations;
        }

        /// <summary>
        /// Adds to a list of Annotations only the non duplicate annotations from another list
        /// </summary>
        /// <param name="annotations">first list</param>
        /// <param name="newAnnotations">the new annotations to be added</param>
        private static void AddRange(List<Annotation> annotations, IList<Annotation> newAnnotations)
        {
            Invariant.Assert((annotations != null) && (newAnnotations != null), "annotations or newAnnotations array is null");
            foreach (Annotation newAnnotation in newAnnotations)
            {
                bool insert = true;
                foreach (Annotation annotation in annotations)
                {
                    if (annotation.Id.Equals(newAnnotation.Id))
                    {
                        insert = false;
                        break;
                    }
                }
                if (insert)
                    annotations.Add(newAnnotation);
            }
        }


        private static List<IAttachedAnnotation> ResolveAnnotations(AnnotationService service, IList<Annotation> annotations)
        {
            Invariant.Assert(annotations != null);
            List<IAttachedAnnotation> attachedAnnotations = new List<IAttachedAnnotation>(annotations.Count);

            // Now resolve any that we queried and add them to list of attached annotations
            foreach (Annotation annot in annotations)
            {
                AttachmentLevel level;
                object anchor = service.LocatorManager.ResolveLocator(annot.Anchors[0].ContentLocators[0], 0, service.Root, out level);
                if (level != AttachmentLevel.Incomplete && level != AttachmentLevel.Unresolved && anchor != null)
                {
                    attachedAnnotations.Add(new AttachedAnnotation(service.LocatorManager, annot, annot.Anchors[0], anchor, level));
                }
            }

            return attachedAnnotations;
        }


        /// <summary>
        /// Finds and removes all annotations of the specified type that have part or the entire
        /// anchor covered by the start-end range. If the delete range is adjacent to the anchor it
        /// it will not be deleted
        /// </summary>
        /// <param name="service">service to use for this operation</param>
        /// <param name="annotationType">type of the annotations to be removed</param>
        private static void DeleteSpannedAnnotations(AnnotationService service, XmlQualifiedName annotationType)
        {
            CheckInputs(service);

            // Limited set of annotation types supported in V1
            Invariant.Assert(annotationType != null &&
                (annotationType == HighlightComponent.TypeName ||
                annotationType == StickyNoteControl.TextSchemaName ||
                annotationType == StickyNoteControl.InkSchemaName), "Invalid Annotation Type");

            // Get the selection from the viewer
            ITextSelection selection = GetTextSelection((FrameworkElement)service.Root);
            Invariant.Assert(selection != null, "TextSelection is null");

            // Get annotations spanned by current selection
            IList<IAttachedAnnotation> attachedAnnotations = GetSpannedAnnotations(service);

            // Find the annotations that overlap with the selection
            foreach (IAttachedAnnotation attachedAnnot in attachedAnnotations)
            {
                if (annotationType.Equals(attachedAnnot.Annotation.AnnotationType))
                {
                    // Only annotations with TextRange anchors can be compared to
                    // the text selection, we ignore others

                    TextAnchor anchor = attachedAnnot.AttachedAnchor as TextAnchor;
                    if (anchor == null)
                        continue;

                    // Remove any annotations that overlap in anyway
                    if (((selection.Start.CompareTo(anchor.Start) > 0) && (selection.Start.CompareTo(anchor.End) < 0)) ||
                        ((selection.End.CompareTo(anchor.Start) > 0) && (selection.End.CompareTo(anchor.End) < 0)) ||
                        ((selection.Start.CompareTo(anchor.Start) <= 0) && (selection.End.CompareTo(anchor.End) >= 0)) ||
                        CheckCaret(selection, anchor, annotationType))
                    {
                        service.Store.DeleteAnnotation(attachedAnnot.Annotation.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Process the case of Empty selection for the different annotation types
        /// </summary>
        /// <param name="selection">the selection</param>
        /// <param name="anchor">annotation anchor</param>
        /// <param name="type">annotation type</param>
        /// <returns></returns>
        private static bool CheckCaret(ITextSelection selection, TextAnchor anchor, XmlQualifiedName type)
        {
            //selection must be empty
            if (!selection.IsEmpty)
                return false;

            if (((anchor.Start.CompareTo(selection.Start) == 0) &&
                 (selection.Start.LogicalDirection == LogicalDirection.Forward)) ||
               ((anchor.End.CompareTo(selection.End) == 0) &&
                (selection.End.LogicalDirection == LogicalDirection.Backward)))
                return true;

            return false;
        }


        /// <summary>
        ///     Creates an annotation of the specified type in the service.  The current
        ///     selection of the DocumentViewerBase is used as the anchor of the new
        ///     annotation.
        ///     If the selection is of length 0 no annotation is created.
        ///     If the no locators can be generated for the textAnchor, no annotation is created.
        /// </summary>
        /// <param name="service">the AnnotationService</param>
        /// <param name="textSelection">text selection for new annotation</param>
        /// <param name="annotationType">the type of the annotation to create</param>
        /// <param name="author">optional author for new annotation</param>
        /// <returns>the annotation created</returns>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        /// <exception cref="InvalidOperationException">selection is of zero length</exception>
        private static Annotation CreateAnnotationForSelection(AnnotationService service, ITextRange textSelection, XmlQualifiedName annotationType, string author)
        {
            Invariant.Assert(service != null && textSelection != null, "Parameter 'service' or 'textSelection' is null.");

            // Limited set of annotation types supported in V1
            Invariant.Assert(annotationType != null &&
                (annotationType == HighlightComponent.TypeName ||
                annotationType == StickyNoteControl.TextSchemaName ||
                annotationType == StickyNoteControl.InkSchemaName), "Invalid Annotation Type");

            Annotation annotation = new Annotation(annotationType);

            SetAnchor(service, annotation, textSelection);

            // Add the author to the annotation
            if (author != null)
            {
                annotation.Authors.Add(author);
            }

            return annotation;
        }

        /// <summary>
        /// Highlights/Unhighlights selection
        /// </summary>
        /// <param name="service">annotation servise</param>
        /// <param name="author">annotation author</param>
        /// <param name="highlightBrush">highlight color</param>
        /// <param name="create">true - highlight, false - clear highlight</param>
        /// <returns>the annotation created, if create flag was true; null otherwise</returns>
        private static Annotation Highlight(AnnotationService service, string author, Brush highlightBrush, bool create)
        {
            CheckInputs(service);

            // Get the selection for the viewer and wrap it in a TextRange
            ITextSelection selection = GetTextSelection((FrameworkElement)service.Root);
            Invariant.Assert(selection != null, "TextSelection is null");

            // Cannot create an annotation with zero length text anchor
            if (selection.IsEmpty)
            {
                throw new InvalidOperationException(SR.Get(SRID.EmptySelectionNotSupported));
            }

            Nullable<Color> color = null;
            if (highlightBrush != null)
            {
                SolidColorBrush brush = highlightBrush as SolidColorBrush;
                if (brush == null)
                    throw new ArgumentException(SR.Get(SRID.InvalidHighlightColor), "highlightBrush");

                // Opacity less than 0 is treated as 0; greater than 1 is treated a 1.
                byte alpha;
                if (brush.Opacity <= 0)
                    alpha = 0;
                else if (brush.Opacity >= 1)
                    alpha = brush.Color.A;
                else
                    alpha = (byte) (brush.Opacity * brush.Color.A);

                color = Color.FromArgb(alpha, brush.Color.R, brush.Color.G, brush.Color.B);
            }

            // Create a range so we can move its ends without changing the selection
            ITextRange anchor = new TextRange(selection.Start, selection.End);

            Annotation highlight = ProcessHighlights(service, anchor, author, color, create);

            // Clear the selection
            selection.SetCaretToPosition(selection.MovingPosition, selection.MovingPosition.LogicalDirection, true, true);

            return highlight;
        }

        /// <summary>
        /// Merges highlights with the same color. Splits highlights with different colors
        /// </summary>
        /// <param name="service">the AnnotationService</param>
        /// <param name="textRange">TextRange of the new highlight</param>
        /// <param name="color">highlight color</param>
        /// <param name="author">highlight author</param>
        /// <param name="create">if true, this is create highlight operation, otherwise it is Clear</param>
        /// <returns>the annotation created, if create flag was true; null otherwise</returns>
        private static Annotation ProcessHighlights(AnnotationService service, ITextRange textRange, string author, Nullable<Color> color, bool create)
        {
            Invariant.Assert(textRange != null, "Parameter 'textRange' is null.");

            IList<IAttachedAnnotation> spannedAnnots = GetSpannedAnnotations(service);

            // Step one: trim all the spanned annotations so there is no overlap with the new annotation
            foreach (IAttachedAnnotation attachedAnnotation in spannedAnnots)
            {
                if (HighlightComponent.TypeName.Equals(attachedAnnotation.Annotation.AnnotationType))
                {
                    TextAnchor textAnchor = attachedAnnotation.FullyAttachedAnchor as TextAnchor;
                    Invariant.Assert(textAnchor != null, "FullyAttachedAnchor must not be null.");
                    TextAnchor copy = new TextAnchor(textAnchor);

                    // This modifies the current fully resolved anchor
                    copy = TextAnchor.TrimToRelativeComplement(copy, textRange.TextSegments);

                    // If the trimming resulting in no more content being in the anchor,
                    // delete the annotation
                    if (copy == null || copy.IsEmpty)
                    {
                        service.Store.DeleteAnnotation(attachedAnnotation.Annotation.Id);
                        continue;
                    }

                    // If there was some portion of content still in the anchor,
                    // generate new locators representing the modified anchor
                    SetAnchor(service, attachedAnnotation.Annotation, copy);
                }
            }

            // Step two: create new annotation
            if (create)
            {
                //create one annotation and return
                Annotation highlight = CreateHighlight(service, textRange, author, color);
                service.Store.AddAnnotation(highlight);
                return highlight;
            }

            return null;
        }

        /// <summary>
        /// Creates a highlight annotation with the specified color and author
        /// </summary>
        /// <param name="service">the AnnotationService</param>
        /// <param name="textRange">highlight anchor</param>
        /// <param name="author">highlight author</param>
        /// <param name="color">highlight brush</param>
        /// <returns>created annotation</returns>
        private static Annotation CreateHighlight(AnnotationService service, ITextRange textRange, string author, Nullable<Color> color)
        {
            Invariant.Assert(textRange != null, "textRange is null");

            Annotation annotation = CreateAnnotationForSelection(service, textRange, HighlightComponent.TypeName, author);

            // Set the cargo with highlight color
            if (color != null)
            {
                ColorConverter converter = new ColorConverter();
                XmlDocument doc = new XmlDocument();
                XmlElement colorsElement = doc.CreateElement(HighlightComponent.ColorsContentName, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
                colorsElement.SetAttribute(HighlightComponent.BackgroundAttributeName, converter.ConvertToInvariantString(color.Value));

                AnnotationResource cargo = new AnnotationResource(HighlightComponent.HighlightResourceName);
                cargo.Contents.Add(colorsElement);

                annotation.Cargos.Add(cargo);
            }

            return annotation;
        }

        private static ITextSelection GetTextSelection(FrameworkElement viewer)
        {
            // Special case for FDR - get the nested viewer which has the editor attached
            FlowDocumentReader reader = viewer as FlowDocumentReader;
            if (reader != null)
            {
                viewer = GetFdrHost(reader) as FrameworkElement;
            }

            // Get the selection for the viewer and wrap it in a TextRange
            return viewer == null ? null : TextEditor.GetTextSelection(viewer);
        }


        private static void SetAnchor(AnnotationService service, Annotation annot, object selection)
        {
            Invariant.Assert(annot != null && selection != null, "null input parameter");

            // Generate locators for the selection - add them to the anchor
            IList<ContentLocatorBase> locators = service.LocatorManager.GenerateLocators(selection);
            Invariant.Assert(locators != null && locators.Count > 0, "No locators generated for selection.");

            // Create an annotation with a single anchor
            AnnotationResource anchor = new AnnotationResource();

            // Add the locators to the anchor
            foreach (ContentLocatorBase locator in locators)
            {
                anchor.ContentLocators.Add(locator);
            }

            annot.Anchors.Clear();
            annot.Anchors.Add(anchor);
        }

        /// <summary>
        ///     Common input checks.  Service must be non-null and enabled.
        /// </summary>
        /// <param name="service">service to check</param>
        /// <exception cref="ArgumentNullException">service is null</exception>
        /// <exception cref="ArgumentException">service is not enabled</exception>
        private static void CheckInputs(AnnotationService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            if (!service.IsEnabled)
            {
                throw new ArgumentException(SR.Get(SRID.AnnotationServiceNotEnabled), "service");
            }

            DocumentViewerBase viewer = service.Root as DocumentViewerBase;
            if (viewer == null)
            {
                FlowDocumentScrollViewer scrollViewer = service.Root as FlowDocumentScrollViewer;
                FlowDocumentReader reader = service.Root as FlowDocumentReader;
                Invariant.Assert((scrollViewer != null) || (reader != null), "Service's Root must be either a FlowDocumentReader, DocumentViewerBase or a FlowDocumentScrollViewer.");
            }
            else
            {
                if (viewer.Document == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.OnlyFlowFixedSupported));
                }
            }
        }

        /// <summary>
        ///     Determines if a command should be enabled based on four things:
        ///      1.  Existing of a service
        ///      2.  Service is enabled
        ///      3.  Selection available
        ///      4.  Selection is not empty (optional)
        /// </summary>
        /// <param name="sender">DocumentViewerBase the command will operate on</param>
        /// <param name="checkForEmpty">whether to check for empty selection or not</param>
        /// <returns>true if the command should be enabled; false otherwise</returns>
        private static bool IsCommandEnabled(object sender, bool checkForEmpty)
        {
            Invariant.Assert(sender != null, "Parameter 'sender' is null.");

            FrameworkElement viewer = sender as FrameworkElement;
            if (viewer != null)
            {
                FrameworkElement parent = viewer.Parent as FrameworkElement;

                AnnotationService service = AnnotationService.GetService(viewer);
                if (service != null && service.IsEnabled &&
                    // service is on the viewer or
                    (service.Root == viewer ||
                    // service is on a viewer that's part of another element's Template (such as Reader)
                    (parent != null && service.Root == parent.TemplatedParent)))
                {
                    ITextSelection selection = GetTextSelection(viewer);
                    if (selection != null)
                    {
                        if (checkForEmpty)
                        {
                            return !selection.IsEmpty;
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion Private Methods
    }
}
