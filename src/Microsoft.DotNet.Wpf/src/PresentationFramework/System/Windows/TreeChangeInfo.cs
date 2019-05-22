// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This data-structure is used
//   1. As the data that is passed around by the DescendentsWalker
//      during an AncestorChanged tree-walk.
//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using MS.Internal;
using MS.Utility;

namespace System.Windows
{
    /// <summary>
    ///     This is the data that is passed through the DescendentsWalker 
    ///     during an AncestorChange tree-walk.
    /// </summary>
    internal struct TreeChangeInfo
    {
        #region Properties
        public TreeChangeInfo(DependencyObject root, DependencyObject parent, bool isAddOperation)
        {
            _rootOfChange = root;
            _isAddOperation = isAddOperation;
            _topmostCollapsedParentNode = null;
            _rootInheritableValues = null;
            _inheritablePropertiesStack = null;
            _valueIndexer = 0;
            
            // Create the InheritableProperties cache for the parent
            // and push it to start the stack ... we don't need to
            // pop this when we're done because we'll be throing the
            // stack away at that point.
            InheritablePropertiesStack.Push(CreateParentInheritableProperties(root, parent, isAddOperation));            
        }

        //
        //  This method
        //  1. Is called from AncestorChange InvalidateTree.
        //  2. It is used to create the InheritableProperties on the given node.
        //  3. It also accumulates oldValues for the inheritable properties that are about to be invalidated
        //
        internal FrugalObjectList<DependencyProperty> CreateParentInheritableProperties(
            DependencyObject                         d,
            DependencyObject                         parent,
            bool                                     isAddOperation)
        {
            Debug.Assert(d != null, "Must have non-null current node");
            
            if (parent == null)
            {
                return new FrugalObjectList<DependencyProperty>(0);
            }

            DependencyObjectType treeObjDOT = d.DependencyObjectType;

            // See if we have a cached value.
            EffectiveValueEntry[] parentEffectiveValues = null;
            uint parentEffectiveValuesCount = 0;
            uint inheritablePropertiesCount = 0;

            // If inheritable properties aren't cached on you then use the effective
            // values cache on the parent to discover those inherited properties that
            // may need to be invalidated on the children nodes.
            if (!parent.IsSelfInheritanceParent)
            {
                DependencyObject inheritanceParent = parent.InheritanceParent;
                if (inheritanceParent != null)
                {
                    parentEffectiveValues = inheritanceParent.EffectiveValues;
                    parentEffectiveValuesCount = inheritanceParent.EffectiveValuesCount;
                    inheritablePropertiesCount = inheritanceParent.InheritableEffectiveValuesCount;
                }
            }
            else
            {
                parentEffectiveValues = parent.EffectiveValues;
                parentEffectiveValuesCount = parent.EffectiveValuesCount;
                inheritablePropertiesCount = parent.InheritableEffectiveValuesCount;
            }

            FrugalObjectList<DependencyProperty> inheritableProperties = new FrugalObjectList<DependencyProperty>((int) inheritablePropertiesCount);

            if (inheritablePropertiesCount == 0)
            {
                return inheritableProperties;
            }

            _rootInheritableValues = new InheritablePropertyChangeInfo[(int) inheritablePropertiesCount];
            int inheritableIndex = 0;

            FrameworkObject foParent = new FrameworkObject(parent);
            
            for (uint i=0; i<parentEffectiveValuesCount; i++)
            {
                // Add all the inheritable properties from the effectiveValues
                // cache to the TreeStateCache on the parent
                EffectiveValueEntry entry = parentEffectiveValues[i];
                DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[entry.PropertyIndex];

                // There are UncommonFields also stored in the EffectiveValues cache. We need to exclude those.
                if ((dp != null) && dp.IsPotentiallyInherited)
                {
                    PropertyMetadata metadata = dp.GetMetadata(parent.DependencyObjectType);
                    if (metadata != null && metadata.IsInherited)
                    {
                        Debug.Assert(!inheritableProperties.Contains(dp), "EffectiveValues cache must not contains duplicate entries for the same DP");

                        FrameworkPropertyMetadata fMetadata = (FrameworkPropertyMetadata)metadata;

                        // Children do not need to inherit properties across a tree boundary 
                        // unless the property is set to override this behavior.

                        if (!TreeWalkHelper.SkipNow(foParent.InheritanceBehavior) || fMetadata.OverridesInheritanceBehavior)
                        {
                            inheritableProperties.Add(dp);

                            EffectiveValueEntry oldEntry;
                            EffectiveValueEntry newEntry;

                            oldEntry = d.GetValueEntry(
                                        d.LookupEntry(dp.GlobalIndex),
                                        dp,
                                        dp.GetMetadata(treeObjDOT),
                                        RequestFlags.DeferredReferences);

                            if (isAddOperation)
                            {
                                // set up the new value
                                newEntry = entry;

                                if ((newEntry.BaseValueSourceInternal != BaseValueSourceInternal.Default) || newEntry.HasModifiers)
                                {                        
                                    newEntry = newEntry.GetFlattenedEntry(RequestFlags.FullyResolved);
                                    newEntry.BaseValueSourceInternal = BaseValueSourceInternal.Inherited;
                                }                        
                            }
                            else
                            {
                                newEntry = new EffectiveValueEntry();
                            }

                            
                            _rootInheritableValues[inheritableIndex++] = 
                                        new InheritablePropertyChangeInfo(d, dp, oldEntry, newEntry);

                            if (inheritablePropertiesCount == inheritableIndex)
                            {
                                // no more inheritable properties, bail early
                                break;
                            }
                        }
                    }
                }
            }

            return inheritableProperties;
        }

        // This is called by TreeWalker.InvalidateTreeDependentProperties before looping through
        // all of the items in the InheritablesPropertiesStack
        internal void ResetInheritableValueIndexer()
        {
            _valueIndexer = 0;
        }
        
        // This is called by TreeWalker.InvalidateTreeDependentProperty.
        // _valueIndexer is an optimization because we know (a) that the last DP list pushed on the
        // InheritablePropertiesStack is a subset of the first DP list pushed on this stack; 
        // (b) the first DP list pushed on the stack has the same # (and order) of entries as the
        // RootInheritableValues list; and (c) the last DP list pushed on the stack will have its
        // DPs in the same order as those DPs appear in the first DP list pushed on the stack.  This
        // allows us to simply increment _valueIndexer until we find a match; and on subsequent
        // calls to GetRootInheritableValue just continue our incrementing from where we left off
        // the last time.
        internal InheritablePropertyChangeInfo GetRootInheritableValue(DependencyProperty dp)
        {
            InheritablePropertyChangeInfo info;
            do
            {
                info = _rootInheritableValues[_valueIndexer++];
            }
            while (info.Property != dp);
            return info;                
        }

        /// <summary>
        ///     This is a stack that is used during an AncestorChange operation. 
        ///     I holds the InheritableProperties cache on the parent nodes in 
        ///     the tree in the order of traversal. The top of the stack holds 
        ///     the cache for the immediate parent node.
        /// </summary>
        internal Stack<FrugalObjectList<DependencyProperty>> InheritablePropertiesStack
        {
            get 
            { 
                if (_inheritablePropertiesStack == null)
                {
                    _inheritablePropertiesStack = new Stack<FrugalObjectList<DependencyProperty>>(1);
                }
                
                return _inheritablePropertiesStack; 
            }
        }

        /// <summary>
        ///     When we enter a Visibility=Collapsed subtree, we remember that node
        ///     using this property. As we process the children of this collapsed
        ///     node, we see this property as non-null and know we're collapsed.
        ///     As we exit the subtree, this reference is nulled out which means
        ///     we don't know whether we're in a collapsed tree and the optimizations
        ///     based on Visibility=Collapsed are not valid and should not be used.
        /// </summary>
        internal object TopmostCollapsedParentNode
        {
            get { return _topmostCollapsedParentNode; }
            set { _topmostCollapsedParentNode = value; }
        }

        // Indicates if this is a add child tree operation
        internal bool IsAddOperation
        {
            get { return _isAddOperation; }
        }

        // This is the element at the root of the sub-tree that had a parent change.
        internal DependencyObject Root
        {
            get { return _rootOfChange; }
        }

        #endregion Properties

        #region Data
        
        private Stack<FrugalObjectList<DependencyProperty>> _inheritablePropertiesStack;
        private object                                      _topmostCollapsedParentNode;
        private bool                                        _isAddOperation;
        private DependencyObject                            _rootOfChange;
        private InheritablePropertyChangeInfo[]             _rootInheritableValues;
        private int                                         _valueIndexer;

        #endregion Data
    }
}

