// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691
//
// Description:
//      AnnotationService provides DynamicProperties and API for configuring
//      and invoking the annotation framework.
//

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
using MS.Internal;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Anchoring;
using MS.Internal.Annotations.Component;
using MS.Internal.Documents;
using MS.Utility;
using System.Reflection;        // for BindingFlags
using System.Globalization;     // for CultureInfo
using System.Threading;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     AnnotationService represents a single configuration of the annotation sub-system.
    ///     AnnotationService is scoped to the DependencyObject it was created on.
    /// </summary>
    public sealed class AnnotationService : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///    Static constructor registers command bindings on DocumentViewerBase for all annotation commands.
        /// </summary>
        static AnnotationService()
        {
            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(CreateHighlightCommand, AnnotationHelper.OnCreateHighlightCommand, AnnotationHelper.OnQueryCreateHighlightCommand));

            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(CreateTextStickyNoteCommand, AnnotationHelper.OnCreateTextStickyNoteCommand, AnnotationHelper.OnQueryCreateTextStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(CreateInkStickyNoteCommand, AnnotationHelper.OnCreateInkStickyNoteCommand, AnnotationHelper.OnQueryCreateInkStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(ClearHighlightsCommand, AnnotationHelper.OnClearHighlightsCommand, AnnotationHelper.OnQueryClearHighlightsCommand));

            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(DeleteStickyNotesCommand, AnnotationHelper.OnDeleteStickyNotesCommand, AnnotationHelper.OnQueryDeleteStickyNotesCommand));

            CommandManager.RegisterClassCommandBinding(typeof(DocumentViewerBase), new CommandBinding(DeleteAnnotationsCommand, AnnotationHelper.OnDeleteAnnotationsCommand, AnnotationHelper.OnQueryDeleteAnnotationsCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(CreateHighlightCommand, AnnotationHelper.OnCreateHighlightCommand, AnnotationHelper.OnQueryCreateHighlightCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(CreateTextStickyNoteCommand, AnnotationHelper.OnCreateTextStickyNoteCommand, AnnotationHelper.OnQueryCreateTextStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(CreateInkStickyNoteCommand, AnnotationHelper.OnCreateInkStickyNoteCommand, AnnotationHelper.OnQueryCreateInkStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(ClearHighlightsCommand, AnnotationHelper.OnClearHighlightsCommand, AnnotationHelper.OnQueryClearHighlightsCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(DeleteStickyNotesCommand, AnnotationHelper.OnDeleteStickyNotesCommand, AnnotationHelper.OnQueryDeleteStickyNotesCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentScrollViewer), new CommandBinding(DeleteAnnotationsCommand, AnnotationHelper.OnDeleteAnnotationsCommand, AnnotationHelper.OnQueryDeleteAnnotationsCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(CreateHighlightCommand, AnnotationHelper.OnCreateHighlightCommand, AnnotationHelper.OnQueryCreateHighlightCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(CreateTextStickyNoteCommand, AnnotationHelper.OnCreateTextStickyNoteCommand, AnnotationHelper.OnQueryCreateTextStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(CreateInkStickyNoteCommand, AnnotationHelper.OnCreateInkStickyNoteCommand, AnnotationHelper.OnQueryCreateInkStickyNoteCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(ClearHighlightsCommand, AnnotationHelper.OnClearHighlightsCommand, AnnotationHelper.OnQueryClearHighlightsCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(DeleteStickyNotesCommand, AnnotationHelper.OnDeleteStickyNotesCommand, AnnotationHelper.OnQueryDeleteStickyNotesCommand));

            CommandManager.RegisterClassCommandBinding(typeof(FlowDocumentReader), new CommandBinding(DeleteAnnotationsCommand, AnnotationHelper.OnDeleteAnnotationsCommand, AnnotationHelper.OnQueryDeleteAnnotationsCommand));
        }

        /// <summary>
        ///     Creates an instance of the AnnotationService focused on a particular
        ///     DocumentViewerBase.
        /// </summary>
        /// <param name="viewer">the viewer this service will operate on</param>
        /// <exception cref="ArgumentNullException">viewer is null</exception>
        public AnnotationService(DocumentViewerBase viewer)
        {
            if (viewer == null)
                throw new ArgumentNullException("viewer");

            Initialize(viewer);
        }

        /// <summary>
        ///     Creates an instance of the AnnotationService focused on a particular
        ///     FlowDocumentScrollViewer.
        /// </summary>
        /// <param name="viewer">the viewer this service will operate on</param>
        /// <exception cref="ArgumentNullException">viewer is null</exception>
        public AnnotationService(FlowDocumentScrollViewer viewer)
        {
            if (viewer == null)
                throw new ArgumentNullException("viewer");

            Initialize(viewer);
        }

        /// <summary>
        ///     Creates an instance of the AnnotationService focused on a particular
        ///     FlowDocumentReader.
        /// </summary>
        /// <param name="viewer">the viewer this service will operate on</param>
        /// <exception cref="ArgumentNullException">viewer is null</exception>
        public AnnotationService(FlowDocumentReader viewer)
        {
            if (viewer == null)
                throw new ArgumentNullException("viewer");

            Initialize(viewer);
        }


        /// <summary>
        ///     Creates an instance of the AnnotationService focused on a particular
        ///     tree node.
        /// </summary>
        /// <param name="root">the tree node this service will operate on</param>
        /// <exception cref="ArgumentNullException">root is null</exception>
        /// <exception cref="ArgumentException">element is not a FrameworkElement or FrameworkContentElement</exception>
        internal AnnotationService(DependencyObject root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            if (!(root is FrameworkElement || root is FrameworkContentElement))
                throw new ArgumentException(SR.Get(SRID.ParameterMustBeLogicalNode), "root");

            Initialize(root);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Enables the service with the given store.
        /// </summary>
        /// <param name="annotationStore">store to use for retreiving and persisting annotations</param>
        /// <exception cref="ArgumentNullException">store is null</exception>
        /// <exception cref="InvalidOperationException">DocumentViewerBase has content which is neither
        /// FlowDocument or FixedDocument; only those two are supported</exception>
        /// <exception cref="InvalidOperationException">this service or another service is already
        /// enabled for the DocumentViewerBase</exception>
        public void Enable(AnnotationStore annotationStore)
        {
            if (annotationStore == null)
                throw new ArgumentNullException("annotationStore");

            VerifyAccess();

            if (_isEnabled)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceIsAlreadyEnabled));

            // Make sure there isn't a service above or below us
            VerifyServiceConfiguration(_root);

            // Post a background work item to load existing annotations.  Do this early in the
            // Enable method in case any later code causes an indirect call to LoadAnnotations,
            // this cached operation will make that LoadAnnotations call a no-op.
            _asyncLoadOperation = _root.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(LoadAnnotationsAsync), this);

            // Enable the store and set it on the tree
            _isEnabled = true;
            _root.SetValue(AnnotationService.ServiceProperty, this);
            _store = annotationStore;

            // If our root is a DocumentViewerBase we need to setup some special processors.
            DocumentViewerBase viewer = _root as DocumentViewerBase;
            if (viewer != null)
            {
                bool isFixed = viewer.Document is FixedDocument || viewer.Document is FixedDocumentSequence;
                bool isFlow = !isFixed && viewer.Document is FlowDocument;

                if (isFixed || isFlow || viewer.Document == null)
                {
                    // Take care of some special processors here
                    if (isFixed)
                    {
                        _locatorManager.RegisterSelectionProcessor(new FixedTextSelectionProcessor(), typeof(TextRange));
                        _locatorManager.RegisterSelectionProcessor(new FixedTextSelectionProcessor(), typeof(TextAnchor));
                    }
                    else if (isFlow)
                    {
                        _locatorManager.RegisterSelectionProcessor(new TextSelectionProcessor(), typeof(TextRange));
                        _locatorManager.RegisterSelectionProcessor(new TextSelectionProcessor(), typeof(TextAnchor));
                        _locatorManager.RegisterSelectionProcessor(new TextViewSelectionProcessor(), typeof(DocumentViewerBase));
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.OnlyFlowFixedSupported));
                }
            }

            // Don't register on the store until the service is ready to accept events.
            // Register for content change and anchor change events - we'll need to attach/detach
            // annotations as they are added/removed from the store or their anchors change
            annotationStore.StoreContentChanged += new StoreContentChangedEventHandler(OnStoreContentChanged);
            annotationStore.AnchorChanged += new AnnotationResourceChangedEventHandler(OnAnchorChanged);
        }

        /// <summary>
        ///     Disables annotations on the DocumentViewerBase.  Any visible annotations will
        ///     disappear.  Service can be enabled again by calling Enable.
        /// </summary>
        /// <exception cref="InvalidOperationException">service isn't enabled</exception>
        public void Disable()
        {
            VerifyAccess();

            if (!_isEnabled)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceNotEnabled));

            // If it hasn't been run yet, abort the pending operation.  Still need to
            // unregister and unload annotations.  They may have been loaded due to a
            // store event.
            if (_asyncLoadOperation != null)
            {
                _asyncLoadOperation.Abort();
                _asyncLoadOperation = null;
            }

            // Unregister for changes to the store - add/deletes/anchor changes - before
            // unloading annotations.  We don't want any events between unloading and unregistering.
            try
            {
                _store.StoreContentChanged -= new StoreContentChangedEventHandler(OnStoreContentChanged);
                _store.AnchorChanged -= new AnnotationResourceChangedEventHandler(OnAnchorChanged);
            }
            finally
            {
                IDocumentPaginatorSource document;
                DocumentViewerBase viewer;

                GetViewerAndDocument(_root, out viewer, out document);
                if (viewer != null)
                {
                    // If our root is a DocumentViewerBase or Reader in pageView mode -
                    // we need to unregister for changes
                    // to its collection of DocumentPageViews.
                    UnregisterOnDocumentViewer(viewer);
                }
                else if (document != null)
                {
                    // If working with a FlowDocumentScrollViewer or a FlowDocumentReader
                    // in Scroll mode, we need to unregister for TextViewUpdated events
                    ITextView textView = GetTextView(document);
                    // ITextView may have gone away already
                    if (textView != null)
                    {
                        textView.Updated -= OnContentChanged;
                    }
                }

                // Unload annotations
                this.UnloadAnnotations();

                // This must be cleared no matter what else fails
                _isEnabled = false;
                _root.ClearValue(AnnotationService.ServiceProperty);
            }
        }

        /// <summary>
        ///     Returns the AnnotationService enabled for the viewer.  If no service is
        ///     enabled for the viewer, returns null.
        /// </summary>
        /// <param name="viewer">viewer to check for an enabled AnnotationService</param>
        /// <returns>the AnnotationService instance for this viewer, null if a service
        /// is not enabled for this viewer</returns>
        /// <exception cref="ArgumentNullException">viewer is null</exception>
        public static AnnotationService GetService(DocumentViewerBase viewer)
        {
            if (viewer == null)
                throw new ArgumentNullException("viewer");

            return viewer.GetValue(AnnotationService.ServiceProperty) as AnnotationService;
        }

        /// <summary>
        ///     Returns the AnnotationService enabled for the reader.  If no service is
        ///     enabled for the reader, returns null.
        /// </summary>
        /// <param name="reader">reader to check for an enabled AnnotationService</param>
        /// <returns>the AnnotationService instance for this reader, null if a service
        /// is not enabled for this reader</returns>
        /// <exception cref="ArgumentNullException">reader is null</exception>
        public static AnnotationService GetService(FlowDocumentReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            return reader.GetValue(AnnotationService.ServiceProperty) as AnnotationService;
        }

        /// <summary>
        ///     Returns the AnnotationService enabled for the viewer.  If no service is
        ///     enabled for the viewer, returns null.
        /// </summary>
        /// <param name="viewer">viewer to check for an enabled AnnotationService</param>
        /// <returns>the AnnotationService instance for this viewer, null if a service
        /// is not enabled for this viewer</returns>
        /// <exception cref="ArgumentNullException">viewer is null</exception>
        public static AnnotationService GetService(FlowDocumentScrollViewer viewer)
        {
            if (viewer == null)
                throw new ArgumentNullException("viewer");

            return viewer.GetValue(AnnotationService.ServiceProperty) as AnnotationService;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Loads all annotations that are found for the subtree rooted at element.
        /// For each annotation found and resolved in the subtree, the annotation service
        /// will fire an AttachedAnnotationChanged event with action AttachedAnnotation.Added.
        /// A call to GetAttachedAnnotations() after LoadAnnotations(element) will include the newly
        /// added AttachedAnnotations.
        /// </summary>
        /// <param name="element">root of the subtree where annotations are to be loaded</param>
        /// <exception cref="ArgumentNullException">element is null</exception>
        /// <exception cref="InvalidOperationException">service is not enabled</exception>
        /// <exception cref="ArgumentException">element is not a FrameworkElement or FrameworkContentElement</exception>
        internal void LoadAnnotations(DependencyObject element)
        {
            // If a LoadAnnotations call has happened before our initial asynchronous
            // load has happened, we should just ignore this call
            if (_asyncLoadOperation != null)
                return;

            if (element == null)
                throw new ArgumentNullException("element");

            if (!(element is FrameworkElement || element is FrameworkContentElement))
                throw new ArgumentException(SR.Get(SRID.ParameterMustBeLogicalNode), "element");

            VerifyAccess();

            if (!_isEnabled)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceNotEnabled));

            //fire start trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.LoadAnnotationsBegin);

            IList<IAttachedAnnotation> attachedAnnotations = LocatorManager.ProcessSubTree(element);

            LoadAnnotationsFromList(attachedAnnotations);

            //fire end trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.LoadAnnotationsEnd);
        }

        /// <summary>
        /// Unloads all the annotations for the subtree rooted at element.
        /// For each attached annotation unloaded, the annotation service will fire an
        /// AttachedAnnotationChanged event with AttachedAnnotationAction.Deleted.
        /// A call to GetAttachedAnnotations() will return a list that excludes the just
        /// removed AttachedAnnotations.
        /// Note: Multiple calls to UnloadAnnotations() at the same element are idempotent
        /// </summary>
        /// <param name="element">root of the subtree where annotations are to be unloaded</param>
        /// <exception cref="ArgumentNullException">element is null</exception>
        /// <exception cref="InvalidOperationException">service is not enabled</exception>
        /// <exception cref="ArgumentException">element is not a FrameworkElement or FrameworkContentElement</exception>
        internal void UnloadAnnotations(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (!(element is FrameworkElement || element is FrameworkContentElement))
                throw new ArgumentException(SR.Get(SRID.ParameterMustBeLogicalNode), "element");

            VerifyAccess();

            if (!_isEnabled)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceNotEnabled));

            // Short circuit search for annotations if the service has no attached annotations
            if (_annotationMap.IsEmpty)
                return;

            // We have to clear all the attached annotations underneath this DependencyObject
            IList attachedAnnotations = GetAllAttachedAnnotationsFor(element);
            UnloadAnnotationsFromList(attachedAnnotations);
        }

        /// <summary>
        /// Unloads all attached annotations at the moment. Does not walk the tree - this is
        /// invoked when there is no garantee that all annotation parents are still attached
        /// to the visual tree
        /// </summary>
        private void UnloadAnnotations()
        {
            IList attachedAnnotations = _annotationMap.GetAllAttachedAnnotations();
            UnloadAnnotationsFromList(attachedAnnotations);
        }

        /// <summary>
        ///     Returns all attached annotations that the service knows about in no particular order.
        ///     Note: The order of the attached annotations in the list is not significant.  Do not
        ///     write code that relies on the order.
        /// </summary>
        /// <returns>a list of IAttachedAnnotations</returns>
        /// <exception cref="InvalidOperationException">service is not enabled</exception>
        internal IList<IAttachedAnnotation> GetAttachedAnnotations()
        {
            VerifyAccess();

            if (!_isEnabled)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceNotEnabled));

            return _annotationMap.GetAllAttachedAnnotations();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///   Command to create Highlight annotation for the current selection.  (selection > 0)
        ///  User can pass the color as command parameter. Default yellow color is used otherwise.
        /// </summary>
        public static readonly RoutedUICommand CreateHighlightCommand = new RoutedUICommand(SR.Get(SRID.CreateHighlight), "CreateHighlight", typeof(AnnotationService), null);

        /// <summary>
        ///  Command to create Text StickyNote annotation for the current selection.  (selection > 0)
        /// </summary>
        public static readonly RoutedUICommand CreateTextStickyNoteCommand = new RoutedUICommand(SR.Get(SRID.CreateTextNote), "CreateTextStickyNote", typeof(AnnotationService), null);

        /// <summary>
        ///  Command to create Ink StickyNote annotation for the current selection.  (selection > 0)
        /// </summary>
        public static readonly RoutedUICommand CreateInkStickyNoteCommand = new RoutedUICommand(SR.Get(SRID.CreateInkNote), "CreateInkStickyNote", typeof(AnnotationService), null);

        /// <summary>
        ///   Command to clear highlight(s) for the current selection.
        /// </summary>
        public static readonly RoutedUICommand ClearHighlightsCommand = new RoutedUICommand(SR.Get(SRID.ClearHighlight), "ClearHighlights", typeof(AnnotationService), null);

        /// <summary>
        ///   Command to delete all Ink  and Text StickyNote annotation(s) that are included in a selection.
        /// </summary>
        public static readonly RoutedUICommand DeleteStickyNotesCommand = new RoutedUICommand(SR.Get(SRID.DeleteNotes), "DeleteStickyNotes", typeof(AnnotationService), null);

        /// <summary>
        ///   Command to delete all Ink+Text StickyNote and highlight annotation(s) that are included in a selection.
        /// </summary>
        public static readonly RoutedUICommand DeleteAnnotationsCommand = new RoutedUICommand(SR.Get(SRID.DeleteAnnotations), "DeleteAnnotations", typeof(AnnotationService), null);

        /// <summary>
        ///     Returns whether or not the annotation service is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
        }

        /// <summary>
        ///     Soon to be public.  Returns the AnnotationStore this service uses.
        /// </summary>
        public AnnotationStore Store
        {
            get
            {
                return _store;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Dependency Properties
        //
        //------------------------------------------------------

        #region Public Dependency Properties

        /// <summary>
        ///     Returns the AnnotationService instance for this object.  If the service is not
        ///     enabled for this object, returns null.
        /// </summary>
        /// <param name="d">object from which to read the attached property</param>
        /// <returns>the AnnotationService instance for this object, null if the service
        /// is not enabled for this object</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        internal static AnnotationService GetService(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            return d.GetValue(AnnotationService.ServiceProperty) as AnnotationService;
        }

        /// <summary>
        /// The chooser property holds a AnnotationComponentChooser which determines what annotation components are used
        /// for a given attached annotation for the subtree the DP is on.  If this DP is not set, a default chooser will return the appropriate
        /// out of box annotation component.  If the DP is set to AnnotationComponentChooser.None, no annotation components will be choosen
        /// for this subtree, in essence disabling the mechanism for the subtree.
        /// </summary>
#pragma warning suppress 7009
        internal static readonly DependencyProperty ChooserProperty = DependencyProperty.RegisterAttached("Chooser", typeof(AnnotationComponentChooser), typeof(AnnotationService), new FrameworkPropertyMetadata(new AnnotationComponentChooser(), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior));

        /// <summary>
        /// Return the AnnotationComponentChooser set in the Chooser DP.
        /// </summary>
        /// <param name="d">Object from which to get the DP.</param>
        /// <returns>AnnotationComponentChooser to use for this subtree </returns>
        internal static AnnotationComponentChooser GetChooser(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            return (AnnotationComponentChooser)d.GetValue(ChooserProperty);
        }

        /// <summary>
        ///     DependencyProperty used to specify the processor to use for a
        ///     given subtree.  When set on a node, all nodes below it will be
        ///     processed with the specified processor, unless overriden.  Setting
        ///     this property after annotations have been loaded will have no effect
        ///     on existing annotations.  If you want to change how the tree is
        ///     processed, you should set this property and call LoadAnnotations/
        ///     UnloadAnnotations on the service.
        /// </summary>
#pragma warning suppress 7009
        internal static readonly DependencyProperty SubTreeProcessorIdProperty = LocatorManager.SubTreeProcessorIdProperty.AddOwner(typeof(AnnotationService));

        /// <summary>
        ///    Sets the value of the SubTreeProcessorId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">object to which to write the attached property</param>
        /// <param name="id">the DP value to set</param>
        /// <exception cref="ArgumentNullException">d is null</exception>
        internal static void SetSubTreeProcessorId(DependencyObject d, String id)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            if (id == null)
                throw new ArgumentNullException("id");

            //d will check the context if needed
            d.SetValue(SubTreeProcessorIdProperty, id);
        }

        /// <summary>
        ///    Gets the value of the SubTreeProcessorId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">the object from which to read the attached property</param>
        /// <returns>the value of the SubTreeProcessorId attached property</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        internal static String GetSubTreeProcessorId(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            //d will check the context if needed
            return d.GetValue(SubTreeProcessorIdProperty) as String;
        }

        /// <summary>
        ///     Used to specify a unique id for the data represented by a
        ///     logical tree node.  Attach this property to the element with a
        ///     unique value.
        /// </summary>
#pragma warning suppress 7009
        internal static readonly DependencyProperty DataIdProperty = DataIdProcessor.DataIdProperty.AddOwner(typeof(AnnotationService));

        /// <summary>
        ///    Sets the value of the DataId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">element to which to write the attached property</param>
        /// <param name="id">the value to set</param>
        /// <exception cref="ArgumentNullException">d is null</exception>
        internal static void SetDataId(DependencyObject d, String id)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            if (id == null)
                throw new ArgumentNullException("id");

            //d will check the context if needed
            d.SetValue(DataIdProperty, id);
        }

        /// <summary>
        ///    Gets the value of the DataId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">the object from which to read the attached property</param>
        /// <returns>the value of the DataId attached property</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        internal static String GetDataId(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            //d will check the context if needed
            return d.GetValue(DataIdProperty) as String;
        }

        #endregion Public Dependency Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// event that notifies the registered objects, typically the annotation panel, with the Add/Delete/Modify
        /// changes to the AttachedAnnotations
        /// </summary>
        internal event AttachedAnnotationChangedEventHandler AttachedAnnotationChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        ///     Gets the LocatorManager instance.
        /// </summary>
        internal LocatorManager LocatorManager
        {
            get
            {
                return _locatorManager;
            }
        }

        /// <summary>
        ///    The node the service is enabled on.  This used by the LocatorManager
        ///    when resolving a locator that must be resolved from the 'top' of the
        ///    annotatable tree.
        /// </summary>
        internal DependencyObject Root
        {
            get
            {
                return _root;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        /// <summary>
        ///     ServiceProperty for the AnnotationService.  Used to retrieve the service object
        ///     in order to make direct calls such as CreateAnnotation.  Kept internal because
        ///     PathNode uses it to short-circuit the path building when there is a service set.
        /// </summary>
#pragma warning suppress 7009
        internal static readonly DependencyProperty ServiceProperty = DependencyProperty.RegisterAttached("Service", typeof(AnnotationService), typeof(AnnotationService), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior));

        #endregion Internal Fields


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        /// <summary>
        ///     Initializes the service on this tree node.
        /// </summary>
        /// <param name="root">the tree node the service is being created on</param>
        private void Initialize(DependencyObject root)
        {
            Invariant.Assert(root != null, "Parameter 'root' is null.");

            // Root of the subtree this service will operate on
            _root = root;

            // Object that resolves and generates locators
            _locatorManager = new LocatorManager();

            // sets up dependency for AnnotationComponentManager
            _annotationComponentManager = new AnnotationComponentManager(this);

            //set known component types priorities
            AdornerPresentationContext.SetTypeZLevel(typeof(StickyNoteControl), 0);
            AdornerPresentationContext.SetTypeZLevel(typeof(MarkedHighlightComponent), 1);
            AdornerPresentationContext.SetTypeZLevel(typeof(HighlightComponent), 1);
            //set ZRanges for the ZLevels
            AdornerPresentationContext.SetZLevelRange(0, Int32.MaxValue / 2 + 1, Int32.MaxValue);
            AdornerPresentationContext.SetZLevelRange(1, 0, 0);
        }

        /// <summary>
        ///     Queue work item - put on the queue when the service is first enabled.
        ///     This method causes the service's annotations to be loaded.
        ///     If the service is disabled before this work item is run, this work item
        ///     does nothing.
        /// </summary>
        /// <param name="obj">the service being enabled</param>
        /// <returns>Always returns null</returns>
        private object LoadAnnotationsAsync(object obj)
        {
            Invariant.Assert(_isEnabled, "Service was disabled before attach operation executed.");

            // Operation is now being executed so we can clear the cached operation
            _asyncLoadOperation = null;

            IDocumentPaginatorSource document = null;
            DocumentViewerBase documentViewerBase;

            GetViewerAndDocument(Root, out documentViewerBase, out document);

            if (documentViewerBase != null)
            {
                // If our root is a DocumentViewerBase - we need to register for changes
                // to its collection of DocumentPageViews.  This allows us to unload/load
                // annotations as the DPVs change.  Prevents us from loading an annotation
                // because of a store event and not unloading it becuase we havent' registered
                // on the DPV yet.
                RegisterOnDocumentViewer(documentViewerBase);
            }
            else if (document != null)
            {
                // If working with a FlowDocumentScrollViewer or FlowDocumentReader
                // in Scroll mode, we must register on ITextView to
                // get notified about relayout changes. For the ScrollViewer OnTextViewUpdated
                // happens synchronously which is not true for the FDPV - we register OnPageConnected
                // for each page there.
                ITextView textView = GetTextView(document);
                if (textView != null)
                {
                    textView.Updated += OnContentChanged;
                }
            }

            //if there are too many visible annotations the application will freeze if we load them
            // synchronously - so do it in batches
            IList<IAttachedAnnotation> attachedAnnotations = LocatorManager.ProcessSubTree(_root);
            LoadAnnotationsFromListAsync(attachedAnnotations);

            return null;
        }

        /// <summary>
        ///     Queue work item - put on the queue when the service is first enabled.
        ///     This method causes the service's annotations to be loaded.
        ///     If the service is disabled before this work item is run, this work item
        ///     does nothing.
        /// </summary>
        /// <param name="obj">list of annotations to be loaded</param>
        /// <returns>Always returns null</returns>
        private object LoadAnnotationsFromListAsync(object obj)
        {
            //if we are executed from the Dispather queue _asyncLoadFromListOperation
            // will point to us. Set it to null, to avoid Abort
            _asyncLoadFromListOperation = null;

            List<IAttachedAnnotation> annotations = obj as List<IAttachedAnnotation>;
            if (annotations == null)
                return null;

            if (annotations.Count > _maxAnnotationsBatch)
            {
                //put the overhead in a new array
                List<IAttachedAnnotation> leftover = new List<IAttachedAnnotation>(annotations.Count);
                leftover = annotations.GetRange(_maxAnnotationsBatch, annotations.Count - _maxAnnotationsBatch);
                //put another item in the queue for the rest
                _asyncLoadFromListOperation = _root.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(LoadAnnotationsFromListAsync), leftover);

                //trim the annotations to process list
                annotations.RemoveRange(_maxAnnotationsBatch, annotations.Count - _maxAnnotationsBatch);
            }

            LoadAnnotationsFromList(annotations);

            return null;
        }

        /// <summary>
        /// Check if the attachment level of the two attached annotations is equal, if the
        /// two anchoes are TextAnchors and are equal
        /// </summary>
        /// <param name="firstAttachedAnnotation">first attached annotation</param>
        /// <param name="secondAttachedAnnotation">second attached annotation</param>
        /// <returns>true if the attachment level is equal and the two anchors are equal TextAnchors</returns>
        private bool AttachedAnchorsEqual(IAttachedAnnotation firstAttachedAnnotation, IAttachedAnnotation secondAttachedAnnotation)
        {
            //the annotation exists, but we do not know if the attached anchor has
            //changed, so we we always modify it
            object oldAttachedAnchor = firstAttachedAnnotation.AttachedAnchor;

            // WorkItem 41404 - Until we have a design that lets us get around
            // anchor specifics in the service we need this here.
            // If the attachment levels are the same, we want to test to see if the
            // anchors are the same as well - if they are we do nothing
            if (firstAttachedAnnotation.AttachmentLevel == secondAttachedAnnotation.AttachmentLevel)
            {
                // If new anchor is text anchor - compare it to old anchor to
                // detect if any changes actually happened.
                TextAnchor newAnchor = secondAttachedAnnotation.AttachedAnchor as TextAnchor;
                if (newAnchor != null)
                {
                    if (newAnchor.Equals(oldAttachedAnchor))
                       return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads all annotations that are found for the subtree rooted at element.
        /// For each annotation found and resolved in the subtree, the annotation service
        /// will fire an AttachedAnnotationChanged event with action AttachedAnnotation.Added.
        /// A call to GetAttachedAnnotations() after LoadAnnotations(element) will include the newly
        /// added AttachedAnnotations.
        /// </summary>
        /// <param name="attachedAnnotations">list of attachedAnnotations to be processed</param>
        /// <exception cref="ArgumentNullException">element is null</exception>
        /// <exception cref="InvalidOperationException">service is not enabled</exception>
        /// <exception cref="ArgumentException">element is not a FrameworkElement or FrameworkContentElement</exception>
        private void LoadAnnotationsFromList(IList<IAttachedAnnotation> attachedAnnotations)
        {
            Invariant.Assert(attachedAnnotations != null, "null attachedAnnotations list");

            List<AttachedAnnotationChangedEventArgs> eventsToFire = new List<AttachedAnnotationChangedEventArgs>(attachedAnnotations.Count);

            IAttachedAnnotation matchingAnnotation = null;
            foreach (IAttachedAnnotation attachedAnnotation in attachedAnnotations)
            {
                Invariant.Assert((attachedAnnotation != null) && (attachedAnnotation.Annotation != null), "invalid attached annotation");
                matchingAnnotation = FindAnnotationInList(attachedAnnotation, _annotationMap.GetAttachedAnnotations(attachedAnnotation.Annotation.Id));

                if (matchingAnnotation != null)
                {
                    //the annotation exists, but we do not know if the attached anchor has
                    //changed, so we we always modify it
                    object oldAttachedAnchor = matchingAnnotation.AttachedAnchor;
                    AttachmentLevel oldAttachmentLevel = matchingAnnotation.AttachmentLevel;
                    if (attachedAnnotation.AttachmentLevel != AttachmentLevel.Unresolved && attachedAnnotation.AttachmentLevel != AttachmentLevel.Incomplete)
                    {
                        if (AttachedAnchorsEqual(matchingAnnotation, attachedAnnotation))
                            continue;

                        ((AttachedAnnotation)matchingAnnotation).Update(attachedAnnotation.AttachedAnchor, attachedAnnotation.AttachmentLevel, null);
                        FullyResolveAnchor(matchingAnnotation);

                        eventsToFire.Add(AttachedAnnotationChangedEventArgs.Modified(matchingAnnotation,
                                            oldAttachedAnchor, oldAttachmentLevel));
                    }
                    // It went from being attached to being partially attached.  Since we don't
                    // support attaching partially resolved anchors we remove it here
                    else
                    {
                        DoRemoveAttachedAnnotation(attachedAnnotation);
                        eventsToFire.Add(AttachedAnnotationChangedEventArgs.Unloaded(attachedAnnotation));
                    }
                }
                else
                {
                    if (attachedAnnotation.AttachmentLevel != AttachmentLevel.Unresolved && attachedAnnotation.AttachmentLevel != AttachmentLevel.Incomplete)
                    {
                        DoAddAttachedAnnotation(attachedAnnotation);
                        eventsToFire.Add(AttachedAnnotationChangedEventArgs.Loaded(attachedAnnotation));
                    }
                }
            }

            FireEvents(eventsToFire);
        }

        /// <summary>
        /// Unloads all the annotations from the list
        /// </summary>
        /// <param name="attachedAnnotations">list of annotations</param>
        private void UnloadAnnotationsFromList(IList attachedAnnotations)
        {
            Invariant.Assert(attachedAnnotations != null, "null attachedAnnotations list");

            List<AttachedAnnotationChangedEventArgs> eventsToFire = new List<AttachedAnnotationChangedEventArgs>(attachedAnnotations.Count);

            foreach (IAttachedAnnotation attachedAnnotation in attachedAnnotations)
            {
                DoRemoveAttachedAnnotation(attachedAnnotation);
                eventsToFire.Add(AttachedAnnotationChangedEventArgs.Unloaded(attachedAnnotation));
            }

            FireEvents(eventsToFire);
        }


        /// <summary>
        /// LayoutUpdate event handler
        /// </summary>
        /// <param name="sender">event sender (not used)</param>
        /// <param name="args">event arguments (not used)</param>
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            // Unregister for the event
            UIElement root = _root as UIElement;
            if (root != null)
                root.LayoutUpdated -= OnLayoutUpdated;

            UpdateAnnotations();
        }

        /// <summary>
        /// Updates the set of attached annotations. Unneeded annotations are unloaded
        /// the annotations that were there and must stay are invalidated, the annotations that
        /// must be added are added in batches asinchronously to avoid freezing of the application
        /// </summary>
        private void UpdateAnnotations()
        {
            // If a UpdateAnnotations call has happened before our initial asynchronous
            // load has finished, we should just ignore this call
            if (_asyncLoadOperation != null)
                return;

            //the service must be Enabled
            if (!_isEnabled)
                return;

            IList<IAttachedAnnotation> attachedAnnotations = null;
            IList<IAttachedAnnotation> dirtyAnnotations = new List<IAttachedAnnotation>();
            //first get the list of annotations to be
            attachedAnnotations = LocatorManager.ProcessSubTree(_root);

            //now make list of annotations to remove
            List<IAttachedAnnotation> existingAnnotations = _annotationMap.GetAllAttachedAnnotations();
            for (int i = existingAnnotations.Count - 1; i >= 0; i--)
            {
                IAttachedAnnotation match = FindAnnotationInList(existingAnnotations[i], attachedAnnotations);
                if ((match != null) && AttachedAnchorsEqual(match, existingAnnotations[i]))
                {
                    //we do not need to load the matching one
                    attachedAnnotations.Remove(match);

                    dirtyAnnotations.Add(existingAnnotations[i]);

                    //we do not need to unload the existing one too
                    existingAnnotations.RemoveAt(i);
                }
            }

            //now every thing that is left in existingAnnotations must be removed
            if ((existingAnnotations != null) && (existingAnnotations.Count > 0))
            {
                UnloadAnnotationsFromList(existingAnnotations);
            }

            //invalidate the annotations that are left
            IList<UIElement> processedElements = new List<UIElement>();
            foreach (IAttachedAnnotation annotation in dirtyAnnotations)
            {
                UIElement parent = annotation.Parent as UIElement;
                if ((parent != null) && !processedElements.Contains(parent))
                {
                    processedElements.Add(parent);
                    InvalidateAdorners(parent);
                }
            }

            if (_asyncLoadFromListOperation != null)
            {
                //stop this one - we  will set a new one in the queue
                _asyncLoadFromListOperation.Abort();
                _asyncLoadFromListOperation = null;
            }

            if ((attachedAnnotations != null) && (attachedAnnotations.Count > 0))
                LoadAnnotationsFromListAsync(attachedAnnotations);
		}

        /// <summary>
        /// Mark all AnnotationAdorners that Annotate this element as Dirty
        /// <param name="element">annotated element</param>
        /// </summary>
        private static void InvalidateAdorners(UIElement element)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(element);
            if (layer != null)
            {
                //mark the components that adorn the same element as dirty
                Adorner[] adorners = layer.GetAdorners(element);
                if (adorners != null)
                {
                    for (int i = 0; i < adorners.Length; i++)
                    {
                        AnnotationAdorner adorner = adorners[i] as AnnotationAdorner;
                        if (adorner != null)
                        {
                            Invariant.Assert(adorner.AnnotationComponent != null, "AnnotationAdorner component undefined");
                            adorner.AnnotationComponent.IsDirty = true;
                        }
                    }
                    layer.Update(element);
                }
            }
        }

        /// <summary>
        /// Verify that no other annotation services are attached above, on or below root
        /// </summary>
        /// <param name="root">the proposed root for a new AnnotationService being enabled</param>
        /// <exception cref="InvalidOperationException">Other Instance of AnnotationService Is Already Set</exception>
        static private void VerifyServiceConfiguration(DependencyObject root)
        {
            Invariant.Assert(root != null, "Parameter 'root' is null.");

            // First make sure there isn't a service above us
            if (AnnotationService.GetService(root) != null)
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceAlreadyExists));

            // Now check the tree below us for an existing service
            DescendentsWalker<Object> walker = new DescendentsWalker<Object>(TreeWalkPriority.VisualTree, VerifyNoServiceOnNode, null);
            walker.StartWalk(root);
        }

        /// <summary>
        /// Finds the DocumentViewerBase object if root is DocumentViewerBase or FlowDocumentReader
        /// in paginated mode. If the root is FDSV or FDR in scroll mode - sets only the document;
        /// </summary>
        /// <param name="root">root the service is enabled on</param>
        /// <param name="documentViewerBase">DocumentViewerBase used by the viewer</param>
        /// <param name="document">document for the viewer</param>
        static private void GetViewerAndDocument(DependencyObject root, out DocumentViewerBase documentViewerBase, out IDocumentPaginatorSource document)
        {
            documentViewerBase = root as DocumentViewerBase;

            // Initialize out parameters
            document = null;

            if (documentViewerBase != null)
            {
                document = documentViewerBase.Document;
            }
            else
            {
                FlowDocumentReader reader = root as FlowDocumentReader;
                if (reader != null)
                {
                    documentViewerBase = AnnotationHelper.GetFdrHost(reader) as DocumentViewerBase;
                    document = reader.Document;
                }
                else
                {
                    FlowDocumentScrollViewer docScrollViewer = root as FlowDocumentScrollViewer;
                    // For backwards compatibility with internal APIs - you can enable
                    // a service on any element with internal APIs - so the root might
                    // not be neither an FDR or a FDSV
                    if (docScrollViewer != null)
                    {
                        document = docScrollViewer.Document;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the ITextView  of an IDocumentPaginator if any
        /// </summary>
        /// <param name="document">the document</param>
        /// <returns>the ITextView</returns>
        static private ITextView GetTextView(IDocumentPaginatorSource document)
        {
            ITextView textView = null;
            IServiceProvider provider = document as IServiceProvider;
            if (provider != null)
            {
                ITextContainer textContainer = provider.GetService(typeof(ITextContainer)) as ITextContainer;
                if (textContainer != null)
                    textView = textContainer.TextView;
            }
            return textView;
        }

        /// <summary>
        ///     Checks to see if the node has an annotation service set on it.
        ///     This is a callback for DescendentsWalker and will be called for each logical
        ///     and visual node at most once.
        ///     We throw instead of returning a value in 'data' because we want to short-circuit
        ///     the visiting of the rest of the tree and this method is currently only used
        ///     for error-checking.
        /// </summary>
        /// <param name="node">the node to check for a service</param>
        /// <param name="data">this parameter is ignored</param>
        /// <returns>always returns true, to continue the traversal</returns>
        static private bool VerifyNoServiceOnNode(DependencyObject node, object data, bool visitedViaVisualTree)
        {
            Invariant.Assert(node != null, "Parameter 'node' is null.");

            // Check that there is no existing service for this node - we use the local value
            // because we don't want to get a false positive based on a service set further
            // up the tree.
            AnnotationService existingService = node.ReadLocalValue(AnnotationService.ServiceProperty) as AnnotationService;
            if (existingService != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceAlreadyExists));
            }

            return true;
        }

        /// <summary>
        ///  searches match to an attachedAnnotation in a list of attachedAnnotations
        /// </summary>
        /// <param name="attachedAnnotation">the attached annotation</param>
        /// <param name="list">the list</param>
        /// <returns>the annotation form the list if mathing is found</returns>
        private IAttachedAnnotation FindAnnotationInList(IAttachedAnnotation attachedAnnotation, IList<IAttachedAnnotation> list)
        {
            foreach (IAttachedAnnotation aa in list)
            {
                if (aa.Annotation == attachedAnnotation.Annotation &&
                    aa.Anchor == attachedAnnotation.Anchor &&
                    aa.Parent == attachedAnnotation.Parent)
                {
                    return aa;
                }
            }

            return null;
        }

        /// <summary>
        /// get the AttachedAnnotations that are attached on or beneath subtree rooted at element
        /// this never returns null
        /// </summary>
        /// <param name="element">root of subtree</param>
        /// <returns>list of all attached annotations found. it never returns null.</returns>
        private IList GetAllAttachedAnnotationsFor(DependencyObject element)
        {
            Invariant.Assert(element != null, "Parameter 'element' is null.");

            List<IAttachedAnnotation> result = new List<IAttachedAnnotation>();
            DescendentsWalker<List<IAttachedAnnotation>> walker = new DescendentsWalker<List<IAttachedAnnotation>>(TreeWalkPriority.VisualTree, GetAttachedAnnotationsFor, result);
            walker.StartWalk(element);

            return result;
        }

        /// <summary>
        ///     Add the annotations loaded for this node to the array list passed in.
        ///     This method is a callback for DescendentsWalker - it will be called
        ///     on every logical and visual node at most one time.
        /// </summary>
        /// <param name="node">node whose annotations we are collecting</param>
        /// <param name="result">list of results to add annotations to</param>
        /// <returns>always returns true, to continue the traversal</returns>
        private bool GetAttachedAnnotationsFor(DependencyObject node, List<IAttachedAnnotation> result, bool visitedViaVisualTree)
        {
            Invariant.Assert(node != null, "Parameter 'node' is null.");
            Invariant.Assert(result != null, "Incorrect data passed - should be a List<IAttachedAnnotation>.");

            List<IAttachedAnnotation> annotationsOnNode = node.GetValue(AnnotationService.AttachedAnnotationsProperty) as List<IAttachedAnnotation>;
            if (annotationsOnNode != null)
            {
                result.AddRange(annotationsOnNode);
            }

            return true;
        }

        /// <summary>
        ///     Called to handle updates to the annotation store.  We handle additions, deletions,
        ///     and certain kinds of modifications(e.g., if an annotation gets a new Context we must
        ///     calculate the anchor for that context).  Updates to content will be handled by
        ///     individual components.
        /// </summary>
        /// <param name="node">the store that was updated</param>
        /// <param name="args">args describing the update to the store</param>
        private void OnStoreContentChanged(object node, StoreContentChangedEventArgs args)
        {
            VerifyAccess();

            switch (args.Action)
            {
                case StoreContentAction.Added:
                    AnnotationAdded(args.Annotation);
                    break;

                case StoreContentAction.Deleted:
                    AnnotationDeleted(args.Annotation.Id);
                    break;

                default:
                    Invariant.Assert(false, "Unknown StoreContentAction.");
                    break;
            }
        }

        /// <summary>
        ///     Called when an anchor on any attached annotation changes.
        ///     We need to handle these changes by updating the set of
        ///     attached annotations and firing any necessary events.
        /// </summary>
        /// <param name="sender">the annotation whose anchor changed</param>
        /// <param name="args">args describing the action and which anchor was changed</param>
        private void OnAnchorChanged(object sender, AnnotationResourceChangedEventArgs args)
        {
            VerifyAccess();

            // Ignore null resources added to an annotation
            if (args.Resource == null)
                return;

            AttachedAnnotationChangedEventArgs newArgs = null;

            switch (args.Action)
            {
                case AnnotationAction.Added:
                    newArgs = AnchorAdded(args.Annotation, args.Resource);
                    break;

                case AnnotationAction.Removed:
                    newArgs = AnchorRemoved(args.Annotation, args.Resource);
                    break;

                case AnnotationAction.Modified:
                    newArgs = AnchorModified(args.Annotation, args.Resource);
                    break;

                default:
                    Invariant.Assert(false, "Unknown AnnotationAction.");
                    break;
            }

            if (newArgs != null)
            {
                AttachedAnnotationChanged(this, newArgs);
            }
        }


        /// <summary>
        /// a handler for the storeupdate annotation added action
        /// </summary>
        /// <param name="annotation">the annotation added to the store</param>
        private void AnnotationAdded(Annotation annotation)
        {
            Invariant.Assert(annotation != null, "Parameter 'annotation' is null.");

            // if we already have an annotation in our map - then something is messed up (store has bug)
            // we are getting an add event on something that already exists - throw an exception
            if (_annotationMap.GetAttachedAnnotations(annotation.Id).Count > 0)
                throw new Exception(SR.Get(SRID.AnnotationAlreadyExistInService));

            List<AttachedAnnotationChangedEventArgs> eventsToFire = new List<AttachedAnnotationChangedEventArgs>(annotation.Anchors.Count);

            foreach (AnnotationResource anchor in annotation.Anchors)
            {
                if (anchor.ContentLocators.Count == 0)
                    continue; // attachedAnchor without locator, keep looping

                AttachedAnnotationChangedEventArgs args = AnchorAdded(annotation, anchor);
                if (args != null)
                {
                    eventsToFire.Add(args);
                }
            }

            FireEvents(eventsToFire);
        }

        /// <summary>
        /// a handler for the storeupdate annotation deleted action
        /// </summary>
        /// <param name="annotationId">the id of the deleted annotation</param>
        private void AnnotationDeleted(Guid annotationId)
        {
            IList<IAttachedAnnotation> annotations = _annotationMap.GetAttachedAnnotations(annotationId);

            // Do nothing if this annotation isn't already loaded
            if (annotations.Count > 0)
            {
                // Since we will be modifying this collection, we make a copy of it to iterate on
                IAttachedAnnotation[] list = new IAttachedAnnotation[annotations.Count];
                annotations.CopyTo(list, 0);

                List<AttachedAnnotationChangedEventArgs> eventsToFire = new List<AttachedAnnotationChangedEventArgs>(list.Length);

                foreach (IAttachedAnnotation attachedAnnotation in list)
                {
                    DoRemoveAttachedAnnotation(attachedAnnotation);
                    eventsToFire.Add(AttachedAnnotationChangedEventArgs.Deleted(attachedAnnotation));
                }

                FireEvents(eventsToFire);
            }
        }

        /// <summary>
        /// handle the case when a new anchor is added to the annotation
        /// </summary>
        /// <param name="annotation">the annotation which anchor was affected</param>
        /// <param name="anchor">the deleted anchor</param>
        /// <returns></returns>
        private AttachedAnnotationChangedEventArgs AnchorAdded(Annotation annotation, AnnotationResource anchor)
        {
            Invariant.Assert(annotation != null && anchor != null, "Parameter 'annotation' or 'anchor' is null.");

            AttachedAnnotationChangedEventArgs args = null;
            AttachmentLevel attachmentLevel;

            object attachedAnchor = FindAttachedAnchor(anchor, out attachmentLevel);

            if (attachmentLevel != AttachmentLevel.Unresolved && attachmentLevel != AttachmentLevel.Incomplete)
            {
                Invariant.Assert(attachedAnchor != null, "Must have a valid attached anchor.");
                AttachedAnnotation attachedAnnotation = new AttachedAnnotation(
                    this.LocatorManager,
                    annotation,
                    anchor,
                    attachedAnchor,
                    attachmentLevel);
                DoAddAttachedAnnotation(attachedAnnotation);
                args = AttachedAnnotationChangedEventArgs.Added(attachedAnnotation);
            }

            return args;
        }

        /// <summary>
        /// handle the case when an anchor is removed from the annotation
        /// </summary>
        /// <param name="annotation">the annotation which anchor was affected</param>
        /// <param name="anchor">the removed anchor</param>
        /// <returns>EventArgs to use when firing an AttachedAnnotationChanged event</returns>
        private AttachedAnnotationChangedEventArgs AnchorRemoved(Annotation annotation, AnnotationResource anchor)
        {
            Invariant.Assert(annotation != null && anchor != null, "Parameter 'annotation' or 'anchor' is null.");

            AttachedAnnotationChangedEventArgs args = null;

            IList<IAttachedAnnotation> annotations = _annotationMap.GetAttachedAnnotations(annotation.Id);

            if (annotations.Count > 0)
            {
                // Since we will be modifying this collection, we make a copy of it to iterate on
                IAttachedAnnotation[] list = new IAttachedAnnotation[annotations.Count];
                annotations.CopyTo(list, 0);

                foreach (IAttachedAnnotation attachedAnnotation in list)
                {
                    if (attachedAnnotation.Anchor == anchor)
                    {
                        DoRemoveAttachedAnnotation(attachedAnnotation);
                        args = AttachedAnnotationChangedEventArgs.Deleted(attachedAnnotation);
                        break;
                    }
                }
            }

            return args;
        }


        /// <summary>
        /// handle the case when an anchor is modified
        /// </summary>
        /// <param name="annotation">the annotation which anchor was affected</param>
        /// <param name="anchor">the modified anchor</param>
        /// <returns></returns>
        private AttachedAnnotationChangedEventArgs AnchorModified(Annotation annotation, AnnotationResource anchor)
        {
            Invariant.Assert(annotation != null && anchor != null, "Parameter 'annotation' or 'anchor' is null.");

            AttachedAnnotationChangedEventArgs args = null;
            AttachmentLevel newAttachmentLevel;
            bool previouslyAttached = false;

            // anchor has changed, need to find new attached anchor
            object newAttachedAnchor = FindAttachedAnchor(anchor, out newAttachmentLevel);

            // Since we will be modifying this collection, we make a copy of it to iterate on
            IList<IAttachedAnnotation> annotations = _annotationMap.GetAttachedAnnotations(annotation.Id);
            IAttachedAnnotation[] list = new IAttachedAnnotation[annotations.Count];
            annotations.CopyTo(list, 0);

            foreach (IAttachedAnnotation attachedAnnotation in list)
            {
                if (attachedAnnotation.Anchor == anchor)
                {
                    previouslyAttached = true;

                    if (newAttachmentLevel != AttachmentLevel.Unresolved)
                    {
                        Invariant.Assert(newAttachedAnchor != null, "AttachedAnnotation with AttachmentLevel != Unresolved should have non-null AttachedAnchor.");

                        object oldAttachedAnchor = attachedAnnotation.AttachedAnchor;
                        AttachmentLevel oldAttachmentLevel = attachedAnnotation.AttachmentLevel;

                        ((AttachedAnnotation)attachedAnnotation).Update(newAttachedAnchor, newAttachmentLevel, null);

                        // Update the full anchor
                        FullyResolveAnchor(attachedAnnotation);

                        // No need to update map - we just changed the AttachedAnnotation in-place
                        args = AttachedAnnotationChangedEventArgs.Modified(attachedAnnotation, oldAttachedAnchor, oldAttachmentLevel);
                    }
                    else
                    {
                        // the new modified anchor doesn't resolve
                        // we need to delete the original attached annotation
                        DoRemoveAttachedAnnotation(attachedAnnotation);
                        args = AttachedAnnotationChangedEventArgs.Deleted(attachedAnnotation);
                    }
                    break;
                }
            }

            // If it wasn't previously attached, but can be resolved now we create an AttachedAnnotation
            if (!previouslyAttached && newAttachmentLevel != AttachmentLevel.Unresolved && newAttachmentLevel != AttachmentLevel.Incomplete)
            {
                Invariant.Assert(newAttachedAnchor != null, "AttachedAnnotation with AttachmentLevel != Unresolved should have non-null AttachedAnchor.");
                AttachedAnnotation attachedAnnotation = new AttachedAnnotation(
                    this.LocatorManager,
                    annotation,
                    anchor,
                    newAttachedAnchor,
                    newAttachmentLevel);

                DoAddAttachedAnnotation(attachedAnnotation);
                args = AttachedAnnotationChangedEventArgs.Added(attachedAnnotation);
            }

            return args;
        }

        /// <summary>
        /// add the passed in attachedAnnotation to the map and to the internal DP to the element tree
        /// </summary>
        /// <param name="attachedAnnotation">the attachedannotation to be added to the map and the element tree</param>
        private void DoAddAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null, "Parameter 'attachedAnnotation' is null.");

            DependencyObject element = attachedAnnotation.Parent;
            Invariant.Assert(element != null, "AttachedAnnotation being added should have non-null Parent.");

            List<IAttachedAnnotation> list = element.GetValue(AnnotationService.AttachedAnnotationsProperty) as List<IAttachedAnnotation>;

            if (list == null)
            {
                list = new List<IAttachedAnnotation>(1);
                element.SetValue(AnnotationService.AttachedAnnotationsProperty, list);
            }
            list.Add(attachedAnnotation);

            _annotationMap.AddAttachedAnnotation(attachedAnnotation);

            // Now we do a full resolve of the anchor - fully anchors are used for activation of sticky notes
            FullyResolveAnchor(attachedAnnotation);
        }

        /// <summary>
        /// remove the passed in attachedAnnotation from the map and the internal DP
        /// </summary>
        /// <param name="attachedAnnotation"></param>
        private void DoRemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null, "Parameter 'attachedAnnotation' is null.");

            DependencyObject element = attachedAnnotation.Parent;
            Invariant.Assert(element != null, "AttachedAnnotation being added should have non-null Parent.");

            _annotationMap.RemoveAttachedAnnotation(attachedAnnotation);

            List<IAttachedAnnotation> list = element.GetValue(AnnotationService.AttachedAnnotationsProperty) as List<IAttachedAnnotation>;

            if (list != null)
            {
                list.Remove(attachedAnnotation);
                // note, we do not guarantee DP change event firing, this is not used in our framework and the DP is internal

                if (list.Count == 0)
                {
                    element.ClearValue(AnnotationService.AttachedAnnotationsProperty);
                }
            }
        }

        /// <summary>
        /// For the given annotation and anchor, attempt to fully resolve the anchor on the tree.
        /// This method turns any trimming to visible content off in order to get an anchor that
        /// includes non-visible content.  This lets us process anchors on virtualized content
        /// as if they were present.
        /// </summary>
        private void FullyResolveAnchor(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null, "Attached annotation cannot be null.");

            // Do nothing if attachment level is already full.
            if (attachedAnnotation.AttachmentLevel == AttachmentLevel.Full)
                return;

            FixedPageProcessor fixedProcessor = null;
            TextSelectionProcessor flowRangeProcessor = null;
            TextSelectionProcessor flowAnchorProcessor = null;

            bool isFlow = false;
            FrameworkElement viewer = this.Root as FrameworkElement;

            if (viewer is DocumentViewerBase)
                isFlow = ((DocumentViewerBase)viewer).Document is FlowDocument;
            else if ((viewer is FlowDocumentScrollViewer) || (viewer is FlowDocumentReader))
                isFlow = true;
            else
                viewer = null;

            if (viewer != null)
            {
                try
                {
                    if (isFlow)
                    {
                        flowRangeProcessor = this.LocatorManager.GetSelectionProcessor(typeof(TextRange)) as TextSelectionProcessor;
                        Invariant.Assert(flowRangeProcessor != null, "TextSelectionProcessor should be available if we are processing flow content.");
                        flowRangeProcessor.Clamping = false;
                        flowAnchorProcessor = this.LocatorManager.GetSelectionProcessor(typeof(TextAnchor)) as TextSelectionProcessor;
                        Invariant.Assert(flowAnchorProcessor != null, "TextSelectionProcessor should be available if we are processing flow content.");
                        flowAnchorProcessor.Clamping = false;
                    }
                    else
                    {
                        fixedProcessor = this.LocatorManager.GetSubTreeProcessorForLocatorPart(FixedPageProcessor.CreateLocatorPart(0)) as FixedPageProcessor;
                        Invariant.Assert(fixedProcessor != null, "FixedPageProcessor should be available if we are processing fixed content.");
                        fixedProcessor.UseLogicalTree = true;
                    }

                    AttachmentLevel attachmentLevel;
                    object fullAnchor = FindAttachedAnchor(attachedAnnotation.Anchor, out attachmentLevel);
                    if (attachmentLevel == AttachmentLevel.Full)
                    {
                        ((AttachedAnnotation)attachedAnnotation).SetFullyAttachedAnchor(fullAnchor);
                    }
                }
                finally
                {
                    if (isFlow)
                    {
                        flowRangeProcessor.Clamping = true;
                        flowAnchorProcessor.Clamping = true;
                    }
                    else
                    {
                        fixedProcessor.UseLogicalTree = false;
                    }
                }
            }
        }


        /// <summary>
        /// Asks the LocatorManager to find the attached anchor for a certain anchor
        /// </summary>
        /// <param name="anchor">the anchor to be resolved</param>
        /// <param name="attachmentLevel">the attachment level of the resulting attached anchor</param>
        /// <returns></returns>
        private object FindAttachedAnchor(AnnotationResource anchor, out AttachmentLevel attachmentLevel)
        {
            Invariant.Assert(anchor != null, "Parameter 'anchor' is null.");

            attachmentLevel = AttachmentLevel.Unresolved;
            Object attachedAnchor = null;

            foreach (ContentLocatorBase locator in anchor.ContentLocators)
            {
                attachedAnchor = LocatorManager.FindAttachedAnchor(_root,
                                                                    null,
                                                                    locator,
                                                                    out attachmentLevel);
                if (attachedAnchor != null)
                    break;
            }

            return attachedAnchor;
        }

        /// <summary>
        ///     Fires AttachedAnnotationChanged events for each args in the list.
        /// </summary>
        /// <param name="eventsToFire">list of AttachedAnnotationChangedEventArgs to fire events for</param>
        private void FireEvents(List<AttachedAnnotationChangedEventArgs> eventsToFire)
        {
            Invariant.Assert(eventsToFire != null, "Parameter 'eventsToFire' is null.");

            if (AttachedAnnotationChanged != null)
            {
                foreach (AttachedAnnotationChangedEventArgs args in eventsToFire)
                {
                    AttachedAnnotationChanged(this, args);
                }
            }
        }

        #region DocumentViewerBase handling

        /// <summary>
        ///    Register for notifications from the viewer and its DPVs for
        ///    any changes that may occur.  As DPV content goes away we
        ///    need to unload annotations.
        /// </summary>
        private void RegisterOnDocumentViewer(DocumentViewerBase viewer)
        {
            Invariant.Assert(viewer != null, "Parameter 'viewer' is null.");
            Invariant.Assert(_views.Count == 0, "Failed to unregister on a viewer before registering on new viewer.");
            foreach (DocumentPageView view in viewer.PageViews)
            {
                view.PageConnected += new EventHandler(OnContentChanged);
                _views.Add(view);
            }
            viewer.PageViewsChanged += new EventHandler(OnPageViewsChanged);
        }

        /// <summary>
        ///    Unregister for notifications from the viewer and its DPVs for
        ///    any changes that may occur.
        /// </summary>
        private void UnregisterOnDocumentViewer(DocumentViewerBase viewer)
        {
            Invariant.Assert(viewer != null, "Parameter 'viewer' is null.");
            foreach (DocumentPageView view in _views)
            {
                view.PageConnected -= new EventHandler(OnContentChanged);
            }
            _views.Clear();
            viewer.PageViewsChanged -= new EventHandler(OnPageViewsChanged);
        }

        /// <summary>
        ///     If the set of page views changes we need to register on new
        ///     ones and unregister on removed ones.  The event only tells us
        ///     the set changed.  This is a relatively small collection (almost
        ///     always below 10 in rare cases maybe up to 50 or 100).  We simply
        ///     unregister from all the ones we previously registered on and
        ///     register on the entire set again.
        /// </summary>
        private void OnPageViewsChanged(object sender, EventArgs e)
        {
            DocumentViewerBase viewer = sender as DocumentViewerBase;
            Invariant.Assert(viewer != null, "Sender for PageViewsChanged event should be a DocumentViewerBase.");

            // First unregister from all page views
            UnregisterOnDocumentViewer(viewer);

            try
            {
                UpdateAnnotations();
            }
            finally
            {
                // Now register on all present page views
                RegisterOnDocumentViewer(viewer);
            }
        }

        /// <summary>
        /// Gets called when the page is connected
        /// to the visual tree during the ArrangeOverride or TextViewUpdated in scroll mode.
        /// We do not want to invalidate more stuff in the middle
        /// of arrange, so all we do is to register for
        /// LayoutUpdated event
        /// </summary>
        private void OnContentChanged(object sender, EventArgs e)
        {
            UIElement root = _root as UIElement;
            if (root != null)
                root.LayoutUpdated += OnLayoutUpdated;
        }

        #endregion DocumentViewerBase handling


        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     Private DynamicProperty used to attach lists of AttachedAnnotations to
        ///     Elements in the tree.
        /// </summary>
#pragma warning suppress 7009
        private static readonly DependencyProperty AttachedAnnotationsProperty = DependencyProperty.RegisterAttached("AttachedAnnotations", typeof(IList<IAttachedAnnotation>), typeof(AnnotationService));

        // The root of the subtree for which this AnnotationService is managing annotations.
        private DependencyObject _root;

        // The annotation/component/anchor map used by the system to
        // keep track of relationships between the different instances
        private AnnotationMap _annotationMap = new AnnotationMap();

        // the annotation component manager creates annotation components and adds them to the appropriate adorner layer
        private AnnotationComponentManager _annotationComponentManager;

        // Provides locator resolving and generating functionality
        private LocatorManager _locatorManager;

        // Is the service currently attached to its root - this value allows for
        // step by step and asynchronous initialization
        private bool _isEnabled = false;

        // the annotation store instance associated with this service
        private AnnotationStore _store;

        // List of DPVs that we have registered on.  These are the ones
        // we need to unregister on once we detach from the viewer.
        private Collection<DocumentPageView> _views = new Collection<DocumentPageView>();

        // Operation for asynchronously loading annotations; can be
        // aborted before the operation is triggered by calling Disable
        private DispatcherOperation _asyncLoadOperation;

        /// <summary>
        /// If there are too many annotations to load - load them asynchroneously
        /// </summary>
        private DispatcherOperation _asyncLoadFromListOperation = null;
        //maximum number of annotations to be loaded at once
        private const int _maxAnnotationsBatch = 10;

        #endregion Private Fields
    }
}
