// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691
//
//
// Description:
//     DataIdProcessor walks the tree and loads annotations based on unique ids
//     identified by the DataIdProperty.  It loads annotations when it
//     reaches a leaf in the tree or when it runs into a node with the
//     FetchAnnotationsAsBatch property set to true.
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;

using MS.Utility;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     DataIdProcessor walks the tree and loads annotations based on unique ids
    ///     identified by the DataIdProperty.  It loads annotations when it
    ///     reaches a leaf in the tree or when it runs into a node with the
    ///     FetchAnnotationsAsBatch property set to true.
    /// </summary>
    internal sealed class DataIdProcessor : SubTreeProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of DataIdProcessor.
        /// </summary>
        /// <param name="manager">the manager that owns this processor</param>
        /// <exception cref="ArgumentNullException">manager is null</exception>
        public DataIdProcessor(LocatorManager manager) : base(manager)
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
        ///     If and only if the current node has a DataId set and has FetchAnnotationsAsBatch 
        ///     set to true, then all annotations for the subtree rooted at this node are loaded
        ///     at once. 
        /// </summary>
        /// <param name="node">node to process</param>
        /// <param name="calledProcessAnnotations">indicates the callback was called by
        /// this processor</param>
        /// <returns>
        ///     a list of AttachedAnnotations loaded during the processing of
        ///     the node; can be null if no annotations were loaded
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public override IList<IAttachedAnnotation> PreProcessNode(DependencyObject node, out bool calledProcessAnnotations)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            // We get the local value so we can distinguish between the property
            // being set or not.  We don't want to rely on null or String.Empty because
            // those might have been the values set.
            object dataId = node.ReadLocalValue(DataIdProcessor.DataIdProperty);
            bool fetchAsBatch = (bool)node.GetValue(FetchAnnotationsAsBatchProperty);

            // If the current node has an ID set on it and FetchAnnotationsAsBatch is
            // set to true, we process this node immediately and return.  All its children
            // will be processed indirectly.
            if (fetchAsBatch && dataId != DependencyProperty.UnsetValue)
            {
                calledProcessAnnotations = true;
                return Manager.ProcessAnnotations(node);
            }

            calledProcessAnnotations = false;
            return null;
        }

        /// <summary>
        ///     This method is called after PreProcessNode and after all the children
        ///     in the subtree have been processed (or skipped if PreProcessNode returns
        ///     true for calledProcessAnnotations).
        ///     If no calls to ProcessAnnotations were made for any portion of the subtree below
        ///     this node, then annotations for this node will be loaded
        /// </summary>
        /// <param name="node">the node to process</param>
        /// <param name="childrenCalledProcessAnnotations">indicates whether calledProcessAnnotations
        /// was returned as true by any node underneath this node</param>
        /// <param name="calledProcessAnnotations">indicates whether ProcessAnnotations was called
        /// by this method</param>
        /// <returns>        
        ///     a list of AttachedAnnotations loaded during the processing of
        ///     the node; can be null if no annotations were loaded
        /// </returns>
        public override IList<IAttachedAnnotation> PostProcessNode(DependencyObject node, bool childrenCalledProcessAnnotations, out bool calledProcessAnnotations)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            // We get the local value so we can distinguish between the property
            // being set or not.  We don't want to rely on null or String.Empty because
            // those might have been the values set.
            object dataId = node.ReadLocalValue(DataIdProcessor.DataIdProperty);
            bool fetchAsBatch = (bool)node.GetValue(FetchAnnotationsAsBatchProperty);

            // If no children were processed, we try and process this node
            if (!fetchAsBatch && !childrenCalledProcessAnnotations && dataId != DependencyProperty.UnsetValue)
            {
                FrameworkElement nodeParent = null;
                FrameworkElement feNode = node as FrameworkElement;
                if (feNode != null)
                {
                    nodeParent = feNode.Parent as FrameworkElement;
                }
                AnnotationService service = AnnotationService.GetService(node);
                if (service != null &&
                    (service.Root == node ||
                    (nodeParent != null && service.Root == nodeParent.TemplatedParent)))
                {
                    calledProcessAnnotations = true;
                    return Manager.ProcessAnnotations(node);
                }
            }

            calledProcessAnnotations = false;
            return null;
        }

        /// <summary>
        ///     Generates a locator part list identifying node.  If node has a
        ///     value for DataIdProperty, a locator with a single locator part
        ///     containing the id value is returned.  Otherwise null is returned.
        /// </summary>
        /// <param name="node">the node to generate a locator for</param>
        /// <param name="continueGenerating">specifies whether or not generating should 
        /// continue for the rest of the path; always set to true</param>
        /// <returns>if node has a value for DataIdProperty, a locator with a
        /// single locator part containing the id value; null otherwise
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public override ContentLocator GenerateLocator(PathNode node, out bool continueGenerating)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            continueGenerating = true;

            ContentLocator locator = null;
            ContentLocatorPart newLocatorPart = CreateLocatorPart(node.Node);
            if (newLocatorPart != null)
            {
                locator = new ContentLocator();
                locator.Parts.Add(newLocatorPart);
            }

            return locator;
        }

        /// <summary>
        ///     Searches the logical tree for a node matching the values of 
        ///     locatorPart.  The search begins with startNode.  
        /// </summary>
        /// <param name="locatorPart">locator part to be matched, must be of the type 
        /// handled by this processor</param>
        /// <param name="startNode">logical tree node to start search at</param>
        /// <param name="continueResolving">return flag indicating whether the search 
        /// should continue (presumably because the search was not exhaustive)</param>
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

            if (DataIdElementName != locatorPart.PartType)
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");

            // Initial value
            continueResolving = true;

            // Get the values from the locator part...
            string id = locatorPart.NameValuePairs[ValueAttributeName];
            if (id == null)
            {
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");
            }

            // and from the node to examine.
            string nodeId = GetNodeId(startNode);

            if (nodeId != null)
            {
                if (nodeId.Equals(id))
                {
                    return startNode;
                }
                else
                {
                    // If there was a value and it didn't match,
                    // we shouldn't bother checking the subtree
                    continueResolving = false;
                }
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
        ///     string in markup as the value for SubTreeProcessorIdProperty.
        /// </summary>
        public const String Id = "Id";

        /// <summary>
        ///     Used to specify a unique id for the data represented by a 
        ///     logical tree node.  Attach this property to the element with a
        ///     unique value.
        /// </summary>
#pragma warning suppress 7009
        public static readonly DependencyProperty DataIdProperty =
                DependencyProperty.RegisterAttached(
                        "DataId",
                        typeof(String),
                        typeof(DataIdProcessor),
                        new PropertyMetadata(
                                (string)null,
                                new PropertyChangedCallback(OnDataIdPropertyChanged),
                                new CoerceValueCallback(CoerceDataId)));

        /// <summary>
        ///    Sets the value of the DataId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">element to which to write the attached property</param>
        /// <param name="id">the value to set</param>
        /// <exception cref="ArgumentNullException">d is null</exception>
        public static void SetDataId(DependencyObject d, String id)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            d.SetValue(DataIdProperty, id);
        }

        /// <summary>
        ///    Gets the value of the DataId attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">the object from which to read the attached property</param>
        /// <returns>the value of the DataId attached property</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static String GetDataId(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            return d.GetValue(DataIdProperty) as String;
        }

        /// <summary>
        ///     Property that specifies, when set to true on an element, this
        ///     processor should load all annotations for the subtree rooted
        ///     a the element as a batch.
        /// </summary>
#pragma warning suppress 7009
        public static readonly DependencyProperty FetchAnnotationsAsBatchProperty = DependencyProperty.RegisterAttached(
                "FetchAnnotationsAsBatch",
                typeof(bool),
                typeof(DataIdProcessor),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///    Sets the value of the FetchAnnotationsAsBatch attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">element to which to write the attached property</param>
        /// <param name="id">the value to set</param>
        /// <exception cref="ArgumentNullException">d is null</exception>
        public static void SetFetchAnnotationsAsBatch(DependencyObject d, bool id)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            d.SetValue(FetchAnnotationsAsBatchProperty, id);
        }


        /// <summary>
        ///    Gets the value of the FetchAnnotationsAsBatch attached property
        ///    of the LocatorManager class.
        /// </summary>
        /// <param name="d">the object from which to read the attached property</param>
        /// <returns>the value of the FetchAnnotationsAsBatch attached property</returns>
        /// <exception cref="ArgumentNullException">d is null</exception>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static bool GetFetchAnnotationsAsBatch(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            return (bool)d.GetValue(FetchAnnotationsAsBatchProperty);
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///    Callback triggered when a DataIdProperty value changes.
        ///    If the values are really different we unload and reload annotations
        ///    using the new value.
        /// </summary>
        private static void OnDataIdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            String oldValue = (string)e.OldValue;
            String newValue = (string)e.NewValue;

            if (!String.Equals(oldValue, newValue))
            {
                // If we get here the value has changed so we reload annotations
                AnnotationService service = AnnotationService.GetService(d);
                if (service != null && service.IsEnabled)
                {
                    service.UnloadAnnotations(d);
                    service.LoadAnnotations(d);
                }
            }
        }

        private static object CoerceDataId(DependencyObject d, object value)
        {
            string newValue = (string)value;

            return (newValue != null && newValue.Length == 0) ? null : value;
        }


        /// <summary>
        ///     Creates a DataId locator part for node.
        /// </summary>
        /// <param name="node">logical tree node for which a locator part will be created</param>
        /// <returns>a DataId locator part for node, or null if node has no
        /// value for the DataIdProperty</returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        private ContentLocatorPart CreateLocatorPart(DependencyObject node)
        {
            Debug.Assert(node != null, "DependencyObject can not be null");

            // Get values from the node
            string nodeId = GetNodeId(node);
            if ((nodeId == null) || (nodeId.Length == 0))
                return null;

            ContentLocatorPart part = new ContentLocatorPart(DataIdElementName);

            part.NameValuePairs.Add(ValueAttributeName, nodeId);
            return part;
        }

        /// <summary>
        ///     Get the value of the DataId dependency property for a
        ///     DependencyObject.
        /// </summary>       
        /// <param name="d">the object whose DataId value is to be retrieved</param>
        /// <returns>the object's DataId, if it is set, null otherwise</returns>
        internal String GetNodeId(DependencyObject d)
        {
            Debug.Assert(d != null, "DependencyObject can not be null");

            String id = d.GetValue(DataIdProperty) as string;

            // Return null if the string is empty
            if (String.IsNullOrEmpty(id))
            {
                id = null;
            }

            return id;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     The type name of locator parts handled by this  handler.  
        ///     This is internal and available to the processor that
        ///     is closely aligned with this handler.
        /// </summary>
        private static readonly XmlQualifiedName DataIdElementName = new XmlQualifiedName("DataId", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        //the name of the value attribute
        private const String ValueAttributeName = "Value";

        // ContentLocatorPart types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames =
                new XmlQualifiedName[]
                {
                    DataIdElementName
                };

        #endregion Private Fields
    }
}
