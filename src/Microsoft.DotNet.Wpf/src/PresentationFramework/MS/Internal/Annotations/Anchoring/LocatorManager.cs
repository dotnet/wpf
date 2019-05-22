// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691
//
//
// Description:
//     The main entry-point to the Anchoring namespace.  LocatorManager is the
//     controller for the Anchoring algorithms.  Most of the work is delegated
//     to processors.  LocatorManager maintains a registry of processors.
//     Spec: Anchoring Namespace Spec.doc
//
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using MS.Internal;
using MS.Utility;


namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     The main entry-point to the Anchoring namespace.  LocatorManager is the
    ///     controller for the Anchoring algorithms.  Most of the work is delegated
    ///     to processors.  LocatorManager maintains a registry of processors.
    /// </summary>
    sealed internal class LocatorManager : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Create an instance of LocatorManager.  It manages the
        ///     processors that are used to create and resolve locators.
        /// </summary>
        public LocatorManager() : this(null)
        {
        }

        /// <summary>
        ///     Create an instance of LocatorManager with an optional specific store.
        ///     It manages the processors that are used to create and resolve locators.
        ///     If a store is passed in, its used to query for annotations.  Otherwise
        ///     the service is looked for and the service's store is used.
        /// </summary>
        /// <param name="store">optional, store to use for query of annotations</param>
        public LocatorManager(AnnotationStore store)
        {
            _locatorPartHandlers = new Hashtable();
            _subtreeProcessors = new Hashtable();
            _selectionProcessors = new Hashtable();

            RegisterSubTreeProcessor(new DataIdProcessor(this), DataIdProcessor.Id);
            RegisterSubTreeProcessor(new FixedPageProcessor(this), FixedPageProcessor.Id);
            TreeNodeSelectionProcessor nodeProcessor = new TreeNodeSelectionProcessor();
            RegisterSelectionProcessor(nodeProcessor, typeof(FrameworkElement));
            RegisterSelectionProcessor(nodeProcessor, typeof(FrameworkContentElement));
            TextSelectionProcessor textProcessor = new TextSelectionProcessor();
            RegisterSelectionProcessor(textProcessor, typeof(TextRange));
            RegisterSelectionProcessor(textProcessor, typeof(TextAnchor));

            _internalStore = store;
        }
        #endregion Constructors     

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #region Processor Caches

        /// <summary>
        ///     Registers a subtree processor.  The processorId is the string to be
        ///     used as the value of the SubTreeProcessorIdProperty for this processor.
        ///     This call overrides any previous registrations for processorId or for
        ///     the locator part types recognized by processor.
        /// </summary>
        /// <param name="processor">instance to be registered</param>
        /// <param name="processorId">string id used to specify this processor as 
        /// the SubTreeProcessorIdProperty value</param>
        /// <exception cref="ArgumentNullException">if the processor or processorId are null</exception>
        public void RegisterSubTreeProcessor(SubTreeProcessor processor, String processorId)
        {
            VerifyAccess();
            if (processor == null)
                throw new ArgumentNullException("processor");

            if (processorId == null)
                throw new ArgumentNullException("processorId");

            XmlQualifiedName[] locatorPartTypes = processor.GetLocatorPartTypes();

            _subtreeProcessors[processorId] = processor;

            if (locatorPartTypes != null)
            {
                foreach (XmlQualifiedName typeName in locatorPartTypes)
                {
                    _locatorPartHandlers[typeName] = processor;
                }
            }
        }

        /// <summary>
        ///     Returns the subtree processor set on this tree node to be used to
        ///     process the subtree rooted at this node.  The subtree processor
        ///     returned is not the one used to process this node - only its 
        ///     subtree.  If no subtree processor is specified on this node
        ///     or any of its ancestors in the tree, an instance of the DataIdProcessor
        ///     is returned.  
        /// </summary>
        /// <param name="node">tree node we are retrieving subtree processor for</param>
        /// <returns>a subtree processor specified by the SubTreeProcessorIdProperty
        /// on this node, or an instance of the DataIdProcessor if none is specified</returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public SubTreeProcessor GetSubTreeProcessor(DependencyObject node)
        {
            VerifyAccess();

            if (node == null)
                throw new ArgumentNullException("node");

            // This property should contain one or more (comma-delimited) 
            // registered string IDs for subtree processors.
            string processorString = node.GetValue(SubTreeProcessorIdProperty) as string;
            if (!String.IsNullOrEmpty(processorString))
            {
                SubTreeProcessor processor = (SubTreeProcessor)_subtreeProcessors[processorString];

                if (processor != null)
                    return processor;
                else
                    throw new ArgumentException(SR.Get(SRID.InvalidSubTreeProcessor, processorString));
            }
            else
            {
                return _subtreeProcessors[DataIdProcessor.Id] as SubTreeProcessor;
            }
        }

        /// <summary>
        ///     Returns the subtree processor registered to handle the locator 
        ///     part's type.  If none is registered, null is returned.
        /// </summary>
        /// <param name="locatorPart">locator part for which a subtree processor
        /// is being retreived</param>
        /// <returns>a subtree processor registered to handle the locator part's
        /// type; null if no such processor is registred</returns>
        /// <exception cref="ArgumentNullException">locatorPart is null</exception>
        public SubTreeProcessor GetSubTreeProcessorForLocatorPart(ContentLocatorPart locatorPart)
        {
            VerifyAccess();
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            return _locatorPartHandlers[locatorPart.PartType] as SubTreeProcessor;
        }

        /// <summary>
        ///     Registers a selection processor for a selection Type.
        ///     This call overrides any previous registrations for the Type.
        ///     The processor also provides an array of locator part types it knows how
        ///     to handle.  If a processor provides a locator part type that has been
        ///     provided by a previously registered processor, the last processor to be
        ///     registered overrides all others.
        /// </summary>
        /// <remark>
        ///     GetSelectionProcessor(Type) looks for a processor registered for
        ///     the Type or any of its base Types.  Interfaces are not taken into
        ///     account.  Do not register a selection processor for an interface
        ///     Type.
        /// </remark>
        /// <param name="processor">instance to be registered</param>
        /// <param name="selectionType">Type of selection processed by this processor</param>
        /// <exception cref="ArgumentNullException">processor or selectionType is null</exception>
        public void RegisterSelectionProcessor(SelectionProcessor processor, Type selectionType)
        {
            VerifyAccess();
            if (processor == null)
                throw new ArgumentNullException("processor");

            if (selectionType == null)
                throw new ArgumentNullException("selectionType");

            XmlQualifiedName[] locatorPartTypes = processor.GetLocatorPartTypes();
            _selectionProcessors[selectionType] = processor;

            if (locatorPartTypes != null)
            {
                foreach (XmlQualifiedName type in locatorPartTypes)
                {
                    _locatorPartHandlers[type] = processor;
                }
            }
        }

        /// <summary>
        ///     Returns the selection processor for selections of the specified
        ///     Type.  If no processor is registered for the specified Type, the
        ///     Type's base Type (as defined by Type.BaseType) is checked, and so 
        ///     on.  Interfaces implemented by the Type or any of its base Types 
        ///     are not taken into account. If no processor is registered for Type 
        ///     or any of its base Types, null is returned.
        /// </summary>
        /// <param name="selectionType">the Type of selection for which a handler 
        /// is requested</param>
        /// <returns>the selection processor for the specified Type or one of its
        /// base Types; null if no processor has been registered for this Type or
        /// any of its base Types</returns>
        /// <exception cref="ArgumentNullException">selectionType is null</exception>
        public SelectionProcessor GetSelectionProcessor(Type selectionType)
        {
            VerifyAccess();
            if (selectionType == null)
                throw new ArgumentNullException("selectionType");

            SelectionProcessor processor = null;

            // We keep looking until we find a processor or
            // there are no more base types to check for
            do
            {
                processor = _selectionProcessors[selectionType] as SelectionProcessor;
                selectionType = selectionType.BaseType;
            }
            while (processor == null && selectionType != null);

            return processor;
        }

        /// <summary>
        ///     Returns the selection processor registered to handle the locator 
        ///     part's type.  If none is registered, null is returned.
        /// </summary>
        /// <param name="locatorPart">locator part for which a selection processor is
        /// being retreived</param>
        /// <returns>the selection processor for the locatorPart's type;  null if no
        /// processor has been registerd for that type</returns>
        /// <exception cref="ArgumentNullException">locatorPart is null</exception>
        public SelectionProcessor GetSelectionProcessorForLocatorPart(ContentLocatorPart locatorPart)
        {
            VerifyAccess();
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            return _locatorPartHandlers[locatorPart.PartType] as SelectionProcessor;
        }

        #endregion Processor Caches        

        /// <summary>
        ///     Called by processors when they've encountered content in the tree
        ///     that should have annotations processed for it.  This is a low-level
        ///     method that will get called several times during the algorithm for 
        ///     loading annotations.  
        /// </summary>
        /// <param name="node">the tree node that needs to be processed</param>
        /// <returns>list of IAttachedAnnotations that were loaded for 'node';
        /// the list will never be null but may be empty</returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        /// <exception cref="SystemException">no AnnotationStore is available from
        /// the element tree</exception>
        public IList<IAttachedAnnotation> ProcessAnnotations(DependencyObject node)
        {
            VerifyAccess();
            if (node == null)
                throw new ArgumentNullException("node");

            IList<IAttachedAnnotation> attachedAnnotations = new List<IAttachedAnnotation>();
            IList<ContentLocatorBase> locators = GenerateLocators(node);
            if (locators.Count > 0)
            {
                AnnotationStore store = null;
                if (_internalStore != null)
                {
                    store = _internalStore;
                }
                else
                {
                    AnnotationService service = AnnotationService.GetService(node);
                    if (service == null || !service.IsEnabled)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.AnnotationServiceNotEnabled));
                    }
                    store = service.Store;
                }

                // LocatorBases for a single node should always be Locators
                ContentLocator[] lists = new ContentLocator[locators.Count];
                locators.CopyTo(lists, 0);

                IList<Annotation> annotations = store.GetAnnotations(lists[0]);

                foreach (ContentLocator locator in locators)
                {
                    if (locator.Parts[locator.Parts.Count - 1].NameValuePairs.ContainsKey(TextSelectionProcessor.IncludeOverlaps))
                    {
                        locator.Parts.RemoveAt(locator.Parts.Count - 1);
                    }
                }

                foreach (Annotation annotation in annotations)
                {
                    foreach (AnnotationResource anchor in annotation.Anchors)
                    {
                        foreach (ContentLocatorBase locator in anchor.ContentLocators)
                        {
                            AttachmentLevel attachmentLevel;
                            object attachedAnchor = FindAttachedAnchor(node, lists, locator, out attachmentLevel);

                            if (attachmentLevel != AttachmentLevel.Unresolved)
                            {
                                Debug.Assert(attachedAnchor != null, "AttachedAnchor cannot be null if attachmentLevel is not Unresolved.");
                                attachedAnnotations.Add(new AttachedAnnotation(this, annotation, anchor, attachedAnchor, attachmentLevel));

                                // Only process one locator per resource
                                break;
                            }
                        }
                    }
                }
            }

            return attachedAnnotations;
        }


        /// <summary>
        ///     Generates zero or more Locators that map to the passed in 
        ///     anchor.  The Locators are generated using the 
        ///     SubTreeProcessorIdProperty values set on the tree containing 
        ///     the anchor and the processors registered.
        /// </summary>
        /// <param name="selection">the anchor to generate Locators for</param>
        /// <returns>an array of Locators;  will never return null but may return
        /// an empty list</returns>
        /// <exception cref="ArgumentNullException">if selection is null</exception>
        /// <exception cref="ArgumentException">if no processor is registered for
        /// selection's Type</exception>
        public IList<ContentLocatorBase> GenerateLocators(Object selection)
        {
            VerifyAccess();
            if (selection == null)
                throw new ArgumentNullException("selection");

            ICollection nodes = null;
            SelectionProcessor selProcessor = GetSelectionProcessor(selection.GetType());

            if (selProcessor != null)
            {
                nodes = (ICollection)selProcessor.GetSelectedNodes(selection);
            }
            else
            {
                throw new ArgumentException("Unsupported Selection", "selection");
            }

            IList<ContentLocatorBase> returnLocators = null;
            PathNode pathRoot = PathNode.BuildPathForElements(nodes);

            if (pathRoot != null)
            {
                SubTreeProcessor processor = GetSubTreeProcessor(pathRoot.Node);
                Debug.Assert(processor != null, "SubtreeProcessor can not be null");

                returnLocators = GenerateLocators(processor, pathRoot, selection);
            }

            // We never return null.  A misbehaved processor might return null so we fix it up.
            if (returnLocators == null)
                returnLocators = new List<ContentLocatorBase>(0);

            return returnLocators;
        }


        /// <summary>
        ///     Produces an anchor spanning the content specified by 'locator'.
        ///     This method traverses the tree and, using the registered 
        ///     processors, resolves the locator in the current element tree.
        /// </summary>
        /// <param name="locator">the locator to be resolved</param>
        /// <param name="offset">the index of the locator part to begin resolution with, ignored for 
        /// LocatorGroups which always start with the first locator part</param>
        /// <param name="startNode">the tree node to start the resolution from</param>
        /// <param name="attachmentLevel">type of the returned anchor</param>
        /// <returns>an anchor that spans the content specified by locator; may return
        /// null if the locator could not be resolved (in which case type is set to
        /// AttachmentLevel.Unresolved</returns>
        /// <exception cref="ArgumentNullException">locator or startNode are null</exception>
        /// <exception cref="ArgumentException">offset is negative or greater than
        /// locator.Count - 1</exception>
        public Object ResolveLocator(ContentLocatorBase locator, int offset, DependencyObject startNode, out AttachmentLevel attachmentLevel)
        {
            VerifyAccess();

            if (locator == null)
                throw new ArgumentNullException("locator");

            if (startNode == null)
                throw new ArgumentNullException("startNode");

            // Offset need only be checked for Locators
            ContentLocator realLocator = locator as ContentLocator;
            if (realLocator != null)
            {
                if (offset < 0 || offset >= realLocator.Parts.Count)
                    throw new ArgumentOutOfRangeException("offset");
            }

            return InternalResolveLocator(locator, offset, startNode, false /*skipStartNode*/, out attachmentLevel);
        }


        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties     

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
        public static readonly DependencyProperty SubTreeProcessorIdProperty = DependencyProperty.RegisterAttached(
                "SubTreeProcessorId",
                typeof(string),
                typeof(LocatorManager),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior));

        /// <summary>
        ///    Sets the value of the SubTreeProcessorId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">element to which to write the attached property</param>
        /// <param name="id">the value to set</param>
        /// <exception cref="ArgumentNullException">d is null</exception>
        public static void SetSubTreeProcessorId(DependencyObject d, String id)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            //d will check the  context
            d.SetValue(SubTreeProcessorIdProperty, id);
        }

        /// <summary>
        ///    Gets the value of the SubTreeProcessorId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">the object from which to read the attached property</param>
        /// <returns>the value of the SubTreeProcessorId attached property</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static String GetSubTreeProcessorId(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            //d will check the  context
            return d.GetValue(SubTreeProcessorIdProperty) as String;
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods


        /// <summary>
        ///     Traverse the element tree starting at subtree and load
        ///     annotations for that subtree.
        /// </summary>
        /// <param name="subTree">root of the subtree for which to load annotations</param>
        /// <returns>list of IAttachedAnnotations that were loaded for 'node';
        /// the list will never be null but may be empty</returns>
        /// <exception cref="ArgumentNullException">subtree is null</exception>
        internal IList<IAttachedAnnotation> ProcessSubTree(DependencyObject subTree)
        {
            if (subTree == null)
                throw new ArgumentNullException("subTree");

            ProcessingTreeState data = new ProcessingTreeState();
            PrePostDescendentsWalker<ProcessingTreeState> walker = new PrePostDescendentsWalker<ProcessingTreeState>(TreeWalkPriority.VisualTree, PreVisit, PostVisit, data);
            walker.StartWalk(subTree);

            return data.AttachedAnnotations;
        }



        /// <summary>
        ///     Searches the element subtree for a node that maps to the 
        ///     passed in ContentLocatorBase.  
        ///     Note: For LocatorGroups the startNode, offset, and prefixes
        ///     are ignored.  Due to the nature of LocatorGroups, we always
        ///     start searching at the node with the service enabled, with
        ///     the first locator part, ignoring all prefixes.
        /// </summary>
        /// <param name="startNode">the root of the subtree to search</param>
        /// <param name="prefixes">locators for the root of the subtree</param>
        /// <param name="locator">the ContentLocatorBase we are resolving to an anchor</param>
        /// <param name="attachmentLevel">type of the anchor returned</param>
        /// <returns>the anchor for the passed in ContentLocatorBase; will return null if no match can be found</returns>
        /// <exception cref="ArgumentNullException">startNode or locator is null</exception>
        internal Object FindAttachedAnchor(DependencyObject startNode, ContentLocator[] prefixes, ContentLocatorBase locator, out AttachmentLevel attachmentLevel)
        {
            if (startNode == null)
                throw new ArgumentNullException("startNode");

            if (locator == null)
                throw new ArgumentNullException("locator");

            // Set it to unresolved initially
            attachmentLevel = AttachmentLevel.Unresolved;

            Object anchor = null;
            bool matched = true;
            int locatorPartIdx = FindMatchingPrefix(prefixes, locator, out matched);

            // The annotation's locator starts with at least 
            // one of the locators for the local root.
            if (matched)
            {
                ContentLocator realLocator = locator as ContentLocator;
                if (realLocator == null || locatorPartIdx < realLocator.Parts.Count)
                {
                    // Now we try to resolve.  If any locator parts were matched to the startNode we want to 
                    // start resolving with its children, skipping a revisit to the startNode.
                    anchor = InternalResolveLocator(locator, locatorPartIdx, startNode, locatorPartIdx != 0 /*skipStartNode*/, out attachmentLevel);
                }

                // If nothing was returned, we base our return values on the results
                // of matching against the local root.
                if (attachmentLevel == AttachmentLevel.Unresolved && locatorPartIdx > 0)
                {
                    if (locatorPartIdx == 0)
                    {
                        attachmentLevel = AttachmentLevel.Unresolved;
                    }
                    // If there was anything left to resolve then its incomplete
                    // what if its an incompletely resolved locator set? Can that happen?
                    else if (realLocator != null && locatorPartIdx < realLocator.Parts.Count)
                    {
                        attachmentLevel = AttachmentLevel.Incomplete;
                        anchor = startNode;
                    }
                    // otherwise its fully resolved 
                    else
                    {
                        attachmentLevel = AttachmentLevel.Full;
                        anchor = startNode;
                    }
                }
            }

            return anchor;
        }

        /// <summary>
        ///    Determines if the locator matches any of the prefixes, and if so, returns
        ///    the length of the prefix that was matched.  Resolving of the locator can
        ///    begin with the first locator part after those that were matched by the prefix.
        /// </summary>
        /// <param name="prefixes">locators representing prefixes to match</param>
        /// <param name="locator">locator to find a match for</param>
        /// <param name="matched">whether or not a match was found</param>
        /// <returns>index of the next locator part to resolve</returns>
        private int FindMatchingPrefix(ContentLocator[] prefixes, ContentLocatorBase locator, out bool matched)
        {
            matched = true;
            int locatorPartIdx = 0;
            ContentLocator realLocator = locator as ContentLocator;

            // If we have a locator set or there are no prefixes then we
            // are 'matched' implicitly.
            if (realLocator != null && prefixes != null && prefixes.Length > 0)
            {
                matched = false;
                foreach (ContentLocator prefix in prefixes)
                {
                    if (realLocator.StartsWith(prefix))
                    {
                        locatorPartIdx = prefix.Parts.Count;
                        matched = true;
                        break;
                    }
                }
            }

            return locatorPartIdx;
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #region Generating Locators

        /// <summary>
        ///     Walks the path through the element tree specified by 'root' and
        ///     produces a set of locators identifying the path based on
        ///     DependencyProperty settings on the tree.
        /// </summary>
        /// <param name="processor">SubTreeProcessor to use on node</param>
        /// <param name="startNode">the PathNode identifying the root of the subtree to process</param>
        /// <param name="selection">selection for which Locators are being generated</param>
        /// <returns>
        ///     a set of locators identifying the path through the element tree;
        ///     can return null if no locators can be generated for this root
        /// </returns>
        /// <exception cref="ArgumentNullException">root is null</exception>
        /// <exception cref="SystemException">no AnnotationStore is available from
        /// the element tree</exception>
        private IList<ContentLocatorBase> GenerateLocators(SubTreeProcessor processor, PathNode startNode, Object selection)
        {
            Debug.Assert(startNode != null, "startNode can not be null");

            List<ContentLocatorBase> locatorsToReturn = new List<ContentLocatorBase>();
            bool continueProcessing = true;

            // Process the current PathNode, with the possibilty of doing the whole
            // subtree (in which case continueProcessing comes back false).
            ContentLocator list = processor.GenerateLocator(startNode, out continueProcessing);
            bool processSelection = list != null;
            IList<ContentLocatorBase> newLocators = null;

            // If we should continue processing, we look at the children of the 
            // root.  Depending on the number of children we do  different things.
            if (continueProcessing)
            {
                switch (startNode.Children.Count)
                {
                    case 0:
                        // No children - we just return what we have so far
                        if (list != null)
                        {
                            locatorsToReturn.Add(list);
                        }

                        break;

                    case 1:
                        // One child - we ask the root of the subtree for the processor
                        // to use and then ask the processor to handle the subtree.
                        SubTreeProcessor newProcessor = GetSubTreeProcessor(startNode.Node);
                        newLocators = GenerateLocators(newProcessor, (PathNode)startNode.Children[0], selection);

                        if (newLocators != null && newLocators.Count > 0)
                            processSelection = false;

                        if (list != null)
                            locatorsToReturn.AddRange(Merge(list, newLocators));
                        else
                            locatorsToReturn.AddRange(newLocators);

                        break;

                    default:
                        // Multiple children - we must process all the children as a
                        // locator set.  This returns one or more locators that all start
                        // start with a ContentLocatorGroup which can have one locator for each
                        // child.
                        ContentLocatorBase newLocator = GenerateLocatorGroup(startNode, selection);

                        if (newLocator != null)
                            processSelection = false;

                        if (list != null)
                            locatorsToReturn.Add(list.Merge(newLocator));
                        else if (newLocator != null)
                            locatorsToReturn.Add(newLocator);
                        break;
                }
            }
            else
            {
                // If we shouldn't continue processing we package up the
                // locator we got from the first GenerateLocator call
                if (list != null)
                {
                    locatorsToReturn.Add(list);
                }
            }

            // If we produced a locator for root and no one below us did as well,
            // we need to process the selection, if any
            if (processSelection && selection != null)
            {
                SelectionProcessor selProcessor = GetSelectionProcessor(selection.GetType());

                if (selProcessor != null)
                {
                    IList<ContentLocatorPart> locatorParts = selProcessor.GenerateLocatorParts(selection, startNode.Node);
                    // Possible bug - AddLocatorPartsToLocator was only written to handle normal locators, not
                    // locator groups. ToDo
                    if (locatorParts != null && locatorParts.Count > 0)
                    {
                        List<ContentLocatorBase> tempLocators = new List<ContentLocatorBase>(locatorsToReturn.Count * locatorParts.Count);

                        foreach (ContentLocatorBase loc in locatorsToReturn)
                        {
                            tempLocators.AddRange(((ContentLocator)loc).DotProduct(locatorParts));  // TODO
                        }

                        locatorsToReturn = tempLocators;
                    }
                }
            }

            return locatorsToReturn;
        }

        /// <summary>
        ///     Produces a set of locators that uses locator groups to
        ///     span multiple branches in the tree.
        /// </summary>
        /// <param name="node">the PathNode identifying the root of the subtree to process</param>
        /// <param name="selection">the selection we are currently processing</param>
        /// <returns>
        ///     a locator set containing locators identifying the path 
        ///     through the element tree
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        /// <exception cref="SystemException">no AnnotationStore is available from
        /// the element tree</exception>
        private ContentLocatorBase GenerateLocatorGroup(PathNode node, Object selection)
        {
            Debug.Assert(node != null, "node can not be null");

            SubTreeProcessor processor = GetSubTreeProcessor(node.Node);
            IList<ContentLocatorBase> tempLocators = null;

            ContentLocatorGroup ContentLocatorGroup = new ContentLocatorGroup();

            // Produce a locator representing each child and add it
            // to the locator set.  NOTE - We currently only support one
            // locator per branch.
            foreach (PathNode child in node.Children)
            {
                tempLocators = GenerateLocators(processor, child, selection);
                if (tempLocators != null && tempLocators.Count > 0)
                {
                    // Don't add empty Locators to the ContentLocatorGroup
                    if (tempLocators[0] != null)
                    {
                        ContentLocatorGroup locatorGroup = null;
                        ContentLocator locator = tempLocators[0] as ContentLocator;
                        if (locator != null && locator.Parts.Count != 0)
                        {
                            // NOTE - We currently only support producing one locator
                            // per branch of the locator set.
                            ContentLocatorGroup.Locators.Add(locator);
                        }
                        else if ((locatorGroup = tempLocators[0] as ContentLocatorGroup) != null)
                        {
                            // TODO - need to merge two locator groups
                            // Not supported in V1
                        }
                    }
                }
            }

            // If the locators set is empty, we return null
            // If only one locator was generated for the children, we return that
            // locator directly.  No need for a ContentLocatorGroup
            // Otherwise return the ContentLocatorGroup
            if (ContentLocatorGroup.Locators.Count == 0)
            {
                return null;
            }
            else if (ContentLocatorGroup.Locators.Count == 1)
            {
                ContentLocator list = ContentLocatorGroup.Locators[0];
                ContentLocatorGroup.Locators.Remove(list);
                return list;
            }
            else
            {
                return ContentLocatorGroup;
            }
        }

        #endregion Generating Locators

        #region Processing Tree

        /// <summary>
        ///     Callback from DescendentsWalker before the nodes children are visited.
        /// </summary>
        /// <param name="dependencyObject">node to visit</param>
        /// <param name="data">data used for the current tree walk</param>
        /// <returns>returns whether or not the children should be visited</returns>
        private bool PreVisit(DependencyObject dependencyObject, ProcessingTreeState data, bool visitedViaVisualTree)
        {
            Debug.Assert(data != null, "dataBlob is either null or not of ProcessingTreeState type");

            bool calledProcessAnnotations = false;

            SubTreeProcessor processor = GetSubTreeProcessor(dependencyObject);
            Debug.Assert(processor != null, "SubtreeProcessor can not be null"); // There is always a default processor

            IList<IAttachedAnnotation> attachedAnnotations = processor.PreProcessNode(dependencyObject, out calledProcessAnnotations);
            if (attachedAnnotations != null)
                data.AttachedAnnotations.AddRange(attachedAnnotations);

            // Combine whether this processor called ProcessAnnotations with the info of its siblings
            data.CalledProcessAnnotations = data.CalledProcessAnnotations || calledProcessAnnotations;
            data.Push();

            // Returning true here prevents children from being called
            return !calledProcessAnnotations;
        }

        /// <summary>
        ///     Callback from DescendentsWalker after the nodes children are visited.
        /// </summary>
        /// <param name="dependencyObject">node to visit</param>
        /// <param name="data">data used for the current tree walk</param>
        /// <returns>return value is ignored</returns>
        private bool PostVisit(DependencyObject dependencyObject, ProcessingTreeState data, bool visitedViaVisualTree)
        {
            Debug.Assert(data != null, "dataBlob is either null or not of ProcessingTreeState type");

            bool childrenCalledProcessAnnotations = data.Pop();
            SubTreeProcessor processor = GetSubTreeProcessor(dependencyObject);
            Debug.Assert(processor != null, "SubtreeProcessor can not be null");

            bool calledProcessAnnotations = false;
            IList<IAttachedAnnotation> attachedAnnotations = processor.PostProcessNode(dependencyObject, childrenCalledProcessAnnotations, out calledProcessAnnotations);
            if (attachedAnnotations != null)
                data.AttachedAnnotations.AddRange(attachedAnnotations);

            // This flag is information available to PostVisit calls
            data.CalledProcessAnnotations = data.CalledProcessAnnotations || calledProcessAnnotations || childrenCalledProcessAnnotations;

            // This return value is not used by PrePostDescendentsWalker
            return true;
        }



        #endregion Processing Tree

        #region Resolving Locators

        /// <summary>
        ///     Resolves a locator starting on a specified locator part with a specified tree node.
        ///     The tree node can optionally be skipped (if some previous locator part has already
        ///     matched it) in which case the resolution starts with its children.
        /// </summary>
        /// <param name="locator">the locator to resolve</param>
        /// <param name="offset">the index of the first locator part to resolve</param>
        /// <param name="startNode">the node to start the resolution at</param>
        /// <param name="skipStartNode">specifies whether to start with the startNode or its children</param>
        /// <param name="attachmentLevel">return value specifying how successful the resolution was</param>
        /// <returns>the attached anchor the locator was resolved to</returns>
        /// <exception cref="InvalidOperationException">if a locator set is resolved and the individual selections
        /// can't be merged</exception>
        private Object InternalResolveLocator(ContentLocatorBase locator, int offset, DependencyObject startNode, bool skipStartNode, out AttachmentLevel attachmentLevel)
        {
            Debug.Assert(locator != null, "locator can not be null");
            Debug.Assert(startNode != null, "startNode can not be null");

            // Set it to unresolved initially
            attachmentLevel = AttachmentLevel.Full;
            Object selection = null;
            ContentLocatorGroup locatorGroup = locator as ContentLocatorGroup;
            ContentLocator realLocator = locator as ContentLocator;
            AttachmentLevel individualAttachmentLevel = AttachmentLevel.Unresolved;

            // If only one locator part left, it might represent a selection so we take
            // care of that case before trying to resolve the locator part
            if (realLocator != null && offset == realLocator.Parts.Count - 1)
            {
                ContentLocatorPart locatorPart = realLocator.Parts[offset];
                SelectionProcessor selProcessor = GetSelectionProcessorForLocatorPart(locatorPart);

                if (selProcessor != null)
                {
                    selection = selProcessor.ResolveLocatorPart(locatorPart, startNode, out individualAttachmentLevel);
                    attachmentLevel = individualAttachmentLevel;
                    // No node has actually been matched in this case so we 
                    // return the default Unresolved (set at top of method).  
                    // Its up to the caller to know if the node and 
                    // index passed in represented an incomplete resolution.

                    return selection;
                }
            }

            IList<ContentLocator> locators = null;

            // Setup the locators and other inputs before the loop.  Normal locators
            // are put in an array of locators (with one element).  LocatorGroups have
            // their Locators collection used.
            if (locatorGroup == null)
            {
                Debug.Assert(offset >= 0 && offset < realLocator.Parts.Count, "offset out of range");

                locators = new List<ContentLocator>(1);
                locators.Add(realLocator);
            }
            else
            {
                // If there is a service, start at its root.
                AnnotationService svc = AnnotationService.GetService(startNode);
                if (svc != null)
                {
                    startNode = svc.Root;
                }

                locators = locatorGroup.Locators;

                // Always start resolving locator groups from the beginning
                // and use the node the service is enabled on to start
                offset = 0;
                skipStartNode = false;
            }

            bool middlePortionExists = true;

            if (locators.Count > 0)
            {
                // Otherwise we need to resolve each of the locators in the locator set
                // and then try to merge the anchors that are returned
                ResolvingLocatorState data = ResolveSingleLocator(ref selection, ref attachmentLevel, AttachmentLevel.StartPortion, locators[0], offset, startNode, skipStartNode);

                // Special case - when there is only one locator we simply use the anchor and level
                // returned from resolving that single locator
                if (locators.Count == 1)
                {
                    selection = data.AttachedAnchor;
                    attachmentLevel = data.AttachmentLevel;
                }
                else
                {
                    // Resolve all locators after the first and before the last
                    if (locators.Count > 2)
                    {
                        AttachmentLevel tempLevel = AttachmentLevel.Unresolved;
                        AttachmentLevel savedLevel = attachmentLevel;
                        for (int i = 1; i < locators.Count - 1; i++)
                        {
                            data = ResolveSingleLocator(ref selection, ref attachmentLevel, AttachmentLevel.MiddlePortion, locators[i], offset, startNode, skipStartNode);

                            //if there are >1 middle locators some of them might be resolved, some other - not
                            //if even one middle locator is resolved we should save its attachmenLevel
                            if ((tempLevel == AttachmentLevel.Unresolved) || ((attachmentLevel & AttachmentLevel.MiddlePortion) != 0))
                                tempLevel = attachmentLevel;

                            attachmentLevel = savedLevel;
                        }
                        attachmentLevel = tempLevel;
                    }
                    else
                    {
                        // We make note that there were no middle portion locators
                        middlePortionExists = false;
                    }

                    // Process the last locator
                    data = ResolveSingleLocator(ref selection, ref attachmentLevel, AttachmentLevel.EndPortion, locators[locators.Count - 1], offset, startNode, skipStartNode);

                    // If no locators exists for the middle portion we need to make
                    // sure its not the only portion left on.
                    if (!middlePortionExists && attachmentLevel == AttachmentLevel.MiddlePortion)
                    {
                        attachmentLevel &= ~AttachmentLevel.MiddlePortion;
                    }

                    //if start and end is resolved we consider this as fully resolved
                    //this will handle the case of empty middle page in fixed
                    if (attachmentLevel == (AttachmentLevel.StartPortion | AttachmentLevel.EndPortion))
                        attachmentLevel = AttachmentLevel.Full;
                }
            }
            else
            {
                // There are no locators to resolve
                attachmentLevel = AttachmentLevel.Unresolved;
            }

            return selection;
        }

        /// <summary>
        ///     Resolves a single locator starting at the given startNode.  
        /// Sets the selection and attachmentLevel if necessary.
        /// </summary>
        /// <param name="selection">object representing the content that has been resolved 
        /// so far; updated if the locator passed in is resolved</param>
        /// <param name="attachmentLevel">attachmentLevel of content that has been resolved 
        /// so far; updated based on the resolution of the passed in locator</param>
        /// <param name="attemptedLevel">the level that is represented by this locator - 
        /// start, middle or end</param>
        /// <param name="locator">the locator to resolve</param>
        /// <param name="offset">the offset into the locator to start the resolution at</param>
        /// <param name="startNode">the node to start the resolution at</param>
        /// <param name="skipStartNode">whether or not the start node should be looked at</param> 
        /// <returns>the data representing the resolution of the single locator; used for 
        /// special cases by calling code to override results from this method</returns>
        private ResolvingLocatorState ResolveSingleLocator(ref object selection, ref AttachmentLevel attachmentLevel, AttachmentLevel attemptedLevel, ContentLocator locator, int offset, DependencyObject startNode, bool skipStartNode)
        {
            ResolvingLocatorState data = new ResolvingLocatorState();
            data.LocatorPartIndex = offset;
            data.ContentLocatorBase = locator;

            PrePostDescendentsWalker<ResolvingLocatorState> walker = new PrePostDescendentsWalker<ResolvingLocatorState>(TreeWalkPriority.VisualTree, ResolveLocatorPart, TerminateResolve, data);
            walker.StartWalk(startNode, skipStartNode);

            if (data.AttachmentLevel == AttachmentLevel.Full && data.AttachedAnchor != null)
            {
                // Merge the results with pre-existing selection
                if (selection != null)
                {
                    SelectionProcessor selProcessor = GetSelectionProcessor(selection.GetType());
                    object newSelection;
                    if (selProcessor != null)
                    {
                        if (selProcessor.MergeSelections(selection, data.AttachedAnchor, out newSelection))
                        {
                            selection = newSelection;
                        }
                        else
                        {
                            // If we can't merge, them this locator isn't included in final results so we
                            // we turn off the level that we are attempting to resolve
                            attachmentLevel &= ~attemptedLevel;
                        }
                    }
                    else
                    {
                        // If not selection processor, the locator can't be resolved so 
                        // we turn off the level that we were attempting to resolve
                        attachmentLevel &= ~attemptedLevel;
                    }
                }
                else
                {
                    selection = data.AttachedAnchor;
                }
            }
            else
            {
                // ContentLocator didn't fully resolve so we turn off the level
                // that we were attempting to resolve
                attachmentLevel &= ~attemptedLevel;
            }

            return data;
        }

        /// <summary>
        ///     Resolves a locator starting from 'startFrom' and the locator part at
        ///     position 'offset' in the locator.  This method is called from the 
        ///     DescendentsWalker.  It maintains the state of resolution as each
        ///     node is visited and individual locator parts are resolved.
        /// </summary>
        /// <param name="dependencyObject">the current node to visit</param>
        /// <param name="data">data containing the state of the current resolution</param>
        /// <returns>whether or not the children of this node should be visited</returns>
        private bool ResolveLocatorPart(DependencyObject dependencyObject, ResolvingLocatorState data, bool visitedViaVisualTree)
        {
            if (data.Finished)
                return false;

            ContentLocator locator = data.ContentLocatorBase;

            Debug.Assert(locator != null, "locator can not be null");
            Debug.Assert(data.LocatorPartIndex >= 0 && data.LocatorPartIndex < locator.Parts.Count,
                         "LocatorPartIndex out of range");

            bool keepResolving = true;
            DependencyObject node = null;
            SubTreeProcessor processor = null;
            ContentLocatorPart locatorPart = locator.Parts[data.LocatorPartIndex];
            if (locatorPart == null)
            {
                // Can't resolve a null ContentLocatorPart
                keepResolving = false;
            }

            processor = this.GetSubTreeProcessorForLocatorPart(locatorPart);
            if (processor == null)
            {
                // Can't keep resolving if there is no processor for this ContentLocatorBase Part                
                keepResolving = false;
            }

            if (locatorPart != null && processor != null)
            {
                node = processor.ResolveLocatorPart(locatorPart, dependencyObject, out keepResolving);
                if (node != null)
                {
                    // At a minimum we are incompletely resolved
                    data.AttachmentLevel = AttachmentLevel.Incomplete;
                    data.AttachedAnchor = node;
                    keepResolving = true;

                    data.LastNodeMatched = node;
                    data.LocatorPartIndex++;

                    // We might already be finished here - if there are no more locator parts
                    // we are fully resolved and there's no need to keep resolving
                    if (data.LocatorPartIndex == locator.Parts.Count)
                    {
                        data.AttachmentLevel = AttachmentLevel.Full;
                        data.AttachedAnchor = node;
                        keepResolving = false;
                    }

                    // If all we have left is the last locatorPart, lets try to resolve it as a
                    // selection. If there is no selection processor, it will fall through and
                    // be handled by resolving on one of the children
                    else if (data.LocatorPartIndex == locator.Parts.Count - 1)
                    {
                        locatorPart = locator.Parts[data.LocatorPartIndex];

                        SelectionProcessor selProcessor = GetSelectionProcessorForLocatorPart(locatorPart);
                        if (selProcessor != null)
                        {
                            AttachmentLevel attachmentLevel;
                            Object selection = selProcessor.ResolveLocatorPart(locatorPart, node, out attachmentLevel);
                            if (selection != null)
                            {
                                data.AttachmentLevel = attachmentLevel;
                                data.AttachedAnchor = selection;
                                keepResolving = false;
                            }
                            else
                            {
                                // In this case the processor couldn't resolve the selection
                                // locator part.  There's no use in continuing.
                                keepResolving = false;
                            }
                        }
                    }
                }
            }

            return keepResolving;
        }

        /// <summary>
        ///     This gets called after each node's subtree has been called to resolve
        ///     the current locator part.
        ///     If the node the call is made for was the last node that anything was
        ///     matched with, we want to stop looking at the rest of the tree.  This is
        ///     because matches should be unique (so no sibling should be able to match)
        ///     and if they aren't the first match wins.
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool TerminateResolve(DependencyObject dependencyObject, ResolvingLocatorState data, bool visitedViaVisualTree)
        {
            // If we are finished with the subtree for the last node matched, we've
            // resolved as much as we can and we should not bother looking at the
            // rest of the tree.  Finished is a property we use to short-circuit
            // vising the rest of the tree.  Note: Returning false only prevents 
            // the children from being visited, we need to not visit siblings either.
            if (!data.Finished && data.LastNodeMatched == dependencyObject)
            {
                data.Finished = true;
            }

            return false;
        }

        #endregion Resolving Locators


        #region ContentLocatorBase Operations


        /// <summary>
        ///     Adds the additionalLocators to the end of initialLocator.  If
        ///     there are more than one additional locators, clones of
        ///     initialLocator are created.  This method may be destructive -
        ///     the locators passed in may be modified.
        /// </summary>
        /// <param name="initialLocator">the locator to append the additional locators to</param>
        /// <param name="additionalLocators">array of locators that need to be appended</param>
        /// <returns>list of merged locators</returns>
        private IList<ContentLocatorBase> Merge(ContentLocatorBase initialLocator, IList<ContentLocatorBase> additionalLocators)
        {
            if (additionalLocators == null || additionalLocators.Count == 0)
            {
                List<ContentLocatorBase> res = new List<ContentLocatorBase>(1);

                res.Add(initialLocator);
                return res;
            }

            for (int i = 1; i < additionalLocators.Count; i++)
            {
                additionalLocators[i] = ((ContentLocatorBase)initialLocator.Clone()).Merge(additionalLocators[i]);
            }

            // Avoid making one too many clones...
            additionalLocators[0] = initialLocator.Merge(additionalLocators[0]);
            return additionalLocators;
        }

        #endregion ContentLocatorBase Operations

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        ///     Private class used to store state while processing the tree.
        ///     It keeps a list of annotations loaded as well as the state of
        ///     the calledProcessAnnotations flags returned by processors.
        /// </summary>
        private class ProcessingTreeState
        {
            public ProcessingTreeState()
            {
                _calledProcessAnnotations.Push(false);
            }

            /// <summary>
            ///     Returns list of attached annotations loaded so far.
            /// </summary>
            public List<IAttachedAnnotation> AttachedAnnotations
            {
                get
                {
                    return _attachedAnnotations;
                }
            }

            /// <summary>
            ///     Returns whether any node in the current node's subtree (including the
            ///     current node itself) has returned true for calledProcessAnnotations.
            /// </summary>
            public bool CalledProcessAnnotations
            {
                get
                {
                    return _calledProcessAnnotations.Peek();
                }
                set
                {
                    if (_calledProcessAnnotations.Peek() != value)
                    {
                        _calledProcessAnnotations.Pop();
                        _calledProcessAnnotations.Push(value);
                    }
                }
            }

            /// <summary>
            ///   Pushes another boolean on to the stack of return values 
            ///   we are maintaining for each level of children.
            /// </summary>
            public void Push()
            {
                // Push on a fresh bool value
                _calledProcessAnnotations.Push(false);
            }

            /// <summary>
            ///   Pops one return value off the stack and returns it.
            /// </summary>            
            public bool Pop()
            {
                return _calledProcessAnnotations.Pop();
            }

            private List<IAttachedAnnotation> _attachedAnnotations = new List<IAttachedAnnotation>();

            private Stack<bool> _calledProcessAnnotations = new Stack<bool>();
        }

        /// <summary>
        ///     Data structure that maintains the state during an operation
        ///     to resolve a locator.  We keep the locator and locator part
        ///     index as the inputs.  We keep the attachment level and
        ///     attached anchor as the outputs.
        ///     LastNodeMatched and Finished are used to short-circuit the
        ///     resolution process once we've made at least an incomplete 
        ///     match (in which case we only want to visit the children of
        ///     the last matched node, not its siblings).
        /// 
        ///     This class is not a struct because we don't want it to be 
        ///     a value type.
        /// </summary>
        private class ResolvingLocatorState
        {
            public ContentLocator ContentLocatorBase;

            public int LocatorPartIndex;

            public AttachmentLevel AttachmentLevel = AttachmentLevel.Unresolved;

            public Object AttachedAnchor;

            public bool Finished;

            public Object LastNodeMatched;
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Hashtable of locator part handlers - keyed by XmlQualifiedName
        private Hashtable _locatorPartHandlers;

        // Hashtable of subtree processors - keyed by processor id
        private Hashtable _subtreeProcessors;

        // Hashtable of selection processors - keyed by selection Type
        private Hashtable _selectionProcessors;

        // Potential separators for subtree processor class names
        private static readonly Char[] Separators = new Char[] { ',', ' ', ';' };

        // Optional store, used if passed in, otherwise we grab the service's store
        private AnnotationStore _internalStore = null;
        #endregion Private Fields
    }
}

