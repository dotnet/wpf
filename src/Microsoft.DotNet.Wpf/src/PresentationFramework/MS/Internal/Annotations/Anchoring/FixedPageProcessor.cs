// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     This processor looks for FixedPage elements.  When generating
//     locators it produces a locator part identifying the FixedPage's
//     index in the parent FixedDocument.  When resolving, it looks for
//     the FixedPage in the required index.  When processing it 
//     processes annotations for any FixedPage it finds.
//
//

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Utility;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     This processor looks for FixedPage elements.  When generating
    ///     locators it produces a locator part identifying the FixedPage's
    ///     index in the parent FixedDocument.  When resolving, it looks for
    ///     the FixedPage in the required index.  When processing it 
    ///     processes annotations for any FixedPage it finds.
    /// </summary>
    internal class FixedPageProcessor : SubTreeProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of FixedPageProcessor.
        /// </summary>
        /// <param name="manager">the manager that owns this processor</param>
        /// <exception cref="ArgumentNullException">manager is null</exception>
        public FixedPageProcessor(LocatorManager manager) : base(manager)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     If the node is a 'chunk' of fingerprintable text, 
        ///     LocatorManager.ProcessAnnotations is called.
        /// </summary>
        /// <param name="node">node to process</param>
        /// <param name="calledProcessAnnotations">indicates the callback was called by
        /// this processor</param>
        /// <returns>
        ///     a list of AttachedAnnotations loaded during the processing of
        ///     the node; can be null if the node is not a FixedPage, or empty
        ///     if the node is FixedPage, but annotations are not loaded
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public override IList<IAttachedAnnotation> PreProcessNode(DependencyObject node, out bool calledProcessAnnotations)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            DocumentPageView dpv = node as DocumentPageView;

            if (dpv != null && (dpv.DocumentPage is FixedDocumentPage || dpv.DocumentPage is FixedDocumentSequenceDocumentPage))
            {
                calledProcessAnnotations = true;
                return Manager.ProcessAnnotations(dpv);
            }

            calledProcessAnnotations = false;
            return null;
        }

        /// <summary>
        ///     Generates locators identifying 'chunks'.  If node is a chunk, 
        ///     generates a locator with a single locator part containing a 
        ///     fingerprint of the chunk.  Otherwise null is returned.
        /// </summary>
        /// <param name="node">the node to generate a locator for</param>
        /// <param name="continueGenerating">return flag indicating whether the search 
        /// should continue (presumably because the search was not exhaustive). 
        /// This processor will always return true because it is possible to locate 
        /// parts of the node (if it is FixedPage or FixedPageProxy)</param>
        /// <returns>if node is a FixedPage or FixedPageProxy, a ContentLocator 
        /// with  a single locator part containing the page number; null if node is not 
        /// FixedPage or FixedPageProxy </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        /// <exception cref="ArgumentException">node points to a Document Page View which
        /// doesn't contain a FixedDocumentPage</exception>
        public override ContentLocator GenerateLocator(PathNode node, out bool continueGenerating)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            // Initial value
            continueGenerating = true;

            ContentLocator locator = null;
            DocumentPageView dpv = node.Node as DocumentPageView;

            int pageNb = -1;

            if (dpv != null)
            {
                // Only produce locator parts for FixedDocumentPages
                if (dpv.DocumentPage is FixedDocumentPage || dpv.DocumentPage is FixedDocumentSequenceDocumentPage)
                {
                    pageNb = dpv.PageNumber;
                }
            }
            else
            {
                FixedTextSelectionProcessor.FixedPageProxy fPage = node.Node as FixedTextSelectionProcessor.FixedPageProxy;
                if (fPage != null)
                {
                    pageNb = fPage.Page;
                }
            }

            if (pageNb >= 0)
            {
                locator = new ContentLocator();
                ContentLocatorPart locatorPart = CreateLocatorPart(pageNb);
                locator.Parts.Add(locatorPart);
            }

            return locator;
        }

        /// <summary>
        ///     Searches the logical tree for a node matching the values of 
        ///     locatorPart.  A match must be a chunk which produces the same
        ///     fingerprint as in the locator part. 
        /// </summary>
        /// <param name="locatorPart">locator part to be matched, must be of the type 
        /// handled by this processor</param>
        /// <param name="startNode">logical tree node to start search at</param>
        /// <param name="continueResolving">return flag indicating whether the search 
        /// should continue (presumably because the search was not exhaustive). This
        /// processor will return false if the startNode is a FixedPage
        /// with a different page number than the locator part's page number.
        /// Otherwise the return value will be true.
        /// inside the FixedPage(like TextSelection) </param>
        /// <returns>returns a node that matches the locator part; null if no such 
        /// node is found</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect 
        /// type</exception>
        public override DependencyObject ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out bool continueResolving)
        {
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            if (startNode == null)
                throw new ArgumentNullException("startNode");

            if (PageNumberElementName != locatorPart.PartType)
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");

            // Initial value
            continueResolving = true;

            int pageNumber = 0;
            string pageNumberString = locatorPart.NameValuePairs[ValueAttributeName];

            if (pageNumberString != null)
                pageNumber = Int32.Parse(pageNumberString, NumberFormatInfo.InvariantInfo);
            else
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");


            // Get the actual FixedPage for the page number specified in the LocatorPart.  We need
            // the actual FixedPage cause its what exists in the visual tree and what we'll use to
            // anchor the annotations to.
            FixedDocumentPage page = null;
            IDocumentPaginatorSource document = null;
            DocumentPageView dpv = null;
            if (_useLogicalTree)
            {
                document = startNode as FixedDocument;
                if (document != null)
                {
                    page = document.DocumentPaginator.GetPage(pageNumber) as FixedDocumentPage;
                }
                else
                {
                    document = startNode as FixedDocumentSequence;
                    if (document != null)
                    {
                        FixedDocumentSequenceDocumentPage sequencePage = document.DocumentPaginator.GetPage(pageNumber) as FixedDocumentSequenceDocumentPage;
                        if (sequencePage != null)
                        {
                            page = sequencePage.ChildDocumentPage as FixedDocumentPage;
                        }
                    }
                }
            }
            else
            {
                dpv = startNode as DocumentPageView;
                if (dpv != null)
                {
                    page = dpv.DocumentPage as FixedDocumentPage;
                    if (page == null)
                    {
                        FixedDocumentSequenceDocumentPage sequencePage = dpv.DocumentPage as FixedDocumentSequenceDocumentPage;
                        if (sequencePage != null)
                        {
                            page = sequencePage.ChildDocumentPage as FixedDocumentPage;
                        }
                    }

                    // If this was the wrong fixed page we want to stop searching this subtree
                    if (page != null && dpv.PageNumber != pageNumber)
                    {
                        continueResolving = false;
                        page = null;
                    }
                }
            }

            if (page != null)
            {
                return page.FixedPage;
            }

            return null;
        }

        /// <summary>
        ///     Returns a list of XmlQualifiedNames representing the
        ///     the locator parts this processor can resolve/generate.
        /// </summary>
        public override XmlQualifiedName[] GetLocatorPartTypes()
        {
            return (XmlQualifiedName[])LocatorPartTypeNames.Clone();
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
        ///     Id used to register this processor with the LocatorManager.  Registration
        ///     is done by the framework and does not need to be repeated.  Use this
        ///     string in markup as a value for SubTreeProcessorIdProperty.
        /// </summary>
        public static readonly String Id = "FixedPage";

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal bool UseLogicalTree
        {
            set
            {
                _useLogicalTree = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Creates an instance of the locator part type handled by this
        ///     handler that represents node.  
        /// </summary>
        /// <param name="page">FixedPage for which a locator part will be created</param>
        /// <returns>
        ///     a locator part of the type handled by this handler representing
        ///     the passed in node; null is returned if the locator part cannot
        ///     be created for the node
        /// </returns>
        static internal ContentLocatorPart CreateLocatorPart(int page)
        {
            Debug.Assert(page >= 0, "page can not be negative");

            ContentLocatorPart part = new ContentLocatorPart(PageNumberElementName);

            part.NameValuePairs.Add(ValueAttributeName, page.ToString(NumberFormatInfo.InvariantInfo));
            return part;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Name of the value attribute containing the hash
        private static readonly String ValueAttributeName = "Value";

        // Name of the locator part produced by this processor.
        private static readonly XmlQualifiedName PageNumberElementName = new XmlQualifiedName("PageNumber", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        // ContentLocatorPart types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames = new XmlQualifiedName[] {
            PageNumberElementName
        };

        // Specifies whether the processor should use the logical tree to resolve locator parts.
        // If this is false (the default) only visible pages will be found.
        private bool _useLogicalTree = false;

        #endregion Private Fields
    }
}
