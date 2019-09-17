// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Tree walk helper methods.
*
*
\***************************************************************************/
using MS.Internal;
using MS.Utility;
using System;
using System.Diagnostics;
using System.Security;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows
{
    #region TreeWalkHelper

    /// <summary>
    ///     This is a static helper class that has methods
    ///     that use the DescendentsWalker to do tree walks.
    /// </summary>
    internal static class TreeWalkHelper
    {
        #region TreeChange

        /// <summary>
        ///     Invalidate inheritable properties and resource
        ///     references during a tree change operation.
        /// </summary>
        internal static void InvalidateOnTreeChange(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            DependencyObject        parent,
            bool                    isAddOperation)
        {
            Debug.Assert(fe != null || fce != null, "Node with the tree change notification must be an FE or an FCE.");
            Debug.Assert(parent != null, "Must have a parent that the current node is connected to or disconnected from.");

            // If the tree change is for a non-FE/FCE parent then we need to find
            // the nearest FE/FCE parent inorder to propagate inheritance correctly.
            FrameworkObject parentFO = new FrameworkObject(parent);
            if (!parentFO.IsValid)
            {
                parent = parentFO.FrameworkParent.DO;
            }

            // We're interested in changes to the Template property that occur during
            // the walk - if the template has changed we don't need to invalidate
            // template-driven properties a second time.  The HasTemplateChanged property
            // is cleared on the first visit to each node, so that it means "template
            // changed during the walk".  But one relevant node isn't visited during
            // the walk - the templated parent of the initial node.  So we handle that now.
            FrameworkObject fo = new FrameworkObject(fe, fce);

            // Synchronize the ShouldLookupImplicitStyles flag with respect to the parent here
            // because for the root node of a tree change UpdateStyleProperty happens right here
            // in this method. And we need to have synchrnozed the ShouldLookupImplicitStyles
            // before we re-query the Style property.

            if (isAddOperation)
            {
                fo.SetShouldLookupImplicitStyles();
            }

            fo.Reset(fo.TemplatedParent);
            fo.HasTemplateChanged = false;

            DependencyObject d = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

            // during a tree walk to invalidate inherited properties, we typically
            // call UpdateStyle from FE/FCE.InvalidateTreeDependentProperties.  But
            // for the root element of the tree change, we need to record old values
            // for inherited properties before we've updated the inheritance parent;
            // so do the updatestyle here before we record old values so that we
            // capture any updates provided by styles.
            if (fe != null)
            {
                if (fe.IsInitialized && !fe.HasLocalStyle)
                {
                    // Clear the HasStyleChanged flag
                    fe.HasStyleChanged = false;
                    fe.HasStyleInvalidated = false;
                    fe.HasTemplateChanged = false;

                    fe.AncestorChangeInProgress = true;
                    fe.UpdateStyleProperty();
                    fe.AncestorChangeInProgress = false;
                }
            }
            else
            {
                if (!fce.HasLocalStyle)
                {
                    // Clear the HasStyleChanged flag
                    fce.HasStyleChanged = false;
                    fce.HasStyleInvalidated = false;

                    fce.AncestorChangeInProgress = true;
                    fce.UpdateStyleProperty();
                    fce.AncestorChangeInProgress = false;
                }
            }

            if (HasChildren(fe, fce))
            {
                // Spin up a DescendentsWalker only when
                // the current node has children to walk

                // If there is another tree walk that has already visited the
                // current node then we do not need to re-walk its sub-tree.
                FrameworkContextData fcdata = FrameworkContextData.From(d.Dispatcher);
                if (!fcdata.WasNodeVisited(d, TreeChangeDelegate))
                {
                    // The TreeChangeInfo object is used here to track
                    // information that we have because we're doing a tree walk.
                    TreeChangeInfo parentInfo = new TreeChangeInfo(d, parent, isAddOperation);

                    // PrePostDescendentsWalker is used instead of the standard
                    // DescendentsWalker because we need a "post" callback to know when
                    // to pop the parent's InheritableProperties cache from the stack.
                    PrePostDescendentsWalker<TreeChangeInfo> walker = new PrePostDescendentsWalker<TreeChangeInfo>(
                        TreeWalkPriority.LogicalTree, TreeChangeDelegate, TreeChangePostDelegate, parentInfo);

                    fcdata.AddWalker(TreeChangeDelegate, walker);

                    try
                    {
                        walker.StartWalk(d);
                    }
                    finally
                    {
                        fcdata.RemoveWalker(TreeChangeDelegate, walker);
                    }
                }
            }
            else
            {
                // Degenerate case when the current node is a leaf node and has no children.

                TreeChangeInfo parentInfo = new TreeChangeInfo(d, parent, isAddOperation);

                // Degenerate case of OnAncestorChanged for a single node
                OnAncestorChanged(fe, fce, parentInfo);

                // Degenerate case of OnPostAncestorChanged for a single node
                bool visitedViaVisualTree = false;
                OnPostAncestorChanged(d, parentInfo, visitedViaVisualTree);
            }
        }

        /// <summary>
        ///     Callback on visiting each node in the descendency during a tree change
        ///     Note that this is only used in an entire sub-tree undergoes a change.
        ///     If the tree change is happening on a single node with no children, this
        ///     invalidation happens inside InvalidateOnTreeChange and this method doesn't
        ///     get involved.
        /// </summary>
        private static bool OnAncestorChanged(
            DependencyObject d,
            TreeChangeInfo   info,
            bool             visitedViaVisualTree)
        {
            // Invalidate properties on current instance
            FrameworkObject fo = new FrameworkObject(d, true);

            OnAncestorChanged(fo.FE, fo.FCE, info);

            // Continue walk down subtree
            return true;
        }

        /// <summary>
        ///     OnAncestorChanged variant when we know what type (FE/FCE) the
        ///     tree node is.
        /// </summary>
        private static void OnAncestorChanged(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            TreeChangeInfo          info)
        {
            if (fe!= null)
            {
                fe.OnAncestorChangedInternal(info);
            }
            else
            {
                fce.OnAncestorChangedInternal(info);
            }
        }

        /// <summary>
        ///     This is called when the PrePostDescendentsWalker is done with the given
        ///     node and its subtree.
        /// </summary>
        private static bool OnPostAncestorChanged(
            DependencyObject d,
            TreeChangeInfo   info,
            bool             visitedViaVisualTree)
        {
            // If the given node is the root of a collapsed subtree, we're
            // done with that subtree now and from this point forward, the given
            // node is not the "topmost collapsed parent node".
            // InvalidateTreeDependentProperties sets this reference when
            // appropriate, and OnPropertyChanged uses it to bypass
            // layout invalidation when in a collapsed subtree.
            if (info.TopmostCollapsedParentNode == d)
            {
                info.TopmostCollapsedParentNode = null;
            }

            // a return of true indicates that there was a cache set for this
            // node (meaning this node wasn't a leaf);
            // so pop the InheritableProperties cache for this node
            info.InheritablePropertiesStack.Pop();

            return true;
        }

        /// <summary>
        ///     Invalidate all the properties in the given
        ///     collection of inheritable properties
        /// </summary>
        /// <remarks>
        ///     This method is called during an [FE/FCE].OnAncestorChange
        /// </remarks>
        internal static FrugalObjectList<DependencyProperty> InvalidateTreeDependentProperties(
            TreeChangeInfo                       info,
            FrameworkElement                     fe,
            FrameworkContentElement              fce,
            Style                                selfStyle,
            Style                                selfThemeStyle,
            ref ChildRecord                      childRecord,
            bool                                 isChildRecordValid,
            bool                                 hasStyleChanged,
            bool                                 isSelfInheritanceParent,
            bool                                 wasSelfInheritanceParent)
        {
            Debug.Assert(fe != null || fce != null, "Must have non-null current node");
            DependencyObject d = fe != null ? (DependencyObject)fe : (DependencyObject)fce;
            FrameworkObject fo = new FrameworkObject(fe, fce);

            // Pull up the parent's InheritableProperties cache
            FrugalObjectList<DependencyProperty> parentInheritableProperties = info.InheritablePropertiesStack.Peek();

            // Loop through all cached inheritable
            // to see if they should be invalidated.
            int inheritablePropertiesCount = parentInheritableProperties  != null ? parentInheritableProperties.Count : 0;

            FrugalObjectList<DependencyProperty> currentInheritableProperties = null;
            if (HasChildren(fe, fce))
            {
                currentInheritableProperties = new FrugalObjectList<DependencyProperty>(inheritablePropertiesCount);
            }

            info.ResetInheritableValueIndexer();

            for (int i = 0; i < inheritablePropertiesCount; i++)
            {
                DependencyProperty inheritableProperty = parentInheritableProperties[i];

                Debug.Assert(inheritableProperty.IsPotentiallyInherited, "if we got here, it means that this property is inheritable by someone");

                PropertyMetadata metadata = inheritableProperty.GetMetadata(d);

                // Invalidate only properties that are marked as inheritable.
                // These are the ones that will be affected by an ancestor changes.
                if (metadata.IsInherited)
                {
                    FrameworkPropertyMetadata fMetadata = (FrameworkPropertyMetadata)metadata;

                    bool changed = InvalidateTreeDependentProperty(info, d, ref fo, inheritableProperty, fMetadata,
                        selfStyle, selfThemeStyle, ref childRecord, isChildRecordValid, hasStyleChanged, isSelfInheritanceParent, wasSelfInheritanceParent);

                    // If a change is detected then add the inheritable property to
                    // the current list so that it can be used to invalidate further children
                    if (changed && currentInheritableProperties != null)
                    {
                        Debug.Assert(!currentInheritableProperties.Contains(inheritableProperty), "InheritableProperties list should not have duplicates");

                        // Children do not need to inherit properties across a tree boundary
                        // unless the property is set to override this behavior.

                        if (!SkipNow(fo.InheritanceBehavior) || fMetadata.OverridesInheritanceBehavior)
                        {
                            currentInheritableProperties.Add(inheritableProperty);
                        }
                    }
                }
            }

            return currentInheritableProperties;
        }

        /// <summary>
        ///     Invalidate this property if
        ///     - It is not locally set and
        ///     - It is not acquired from a style/template
        /// </summary>
        private static bool InvalidateTreeDependentProperty(
            TreeChangeInfo              info,
            DependencyObject            d,
        ref FrameworkObject             fo,
            DependencyProperty          dp,
            FrameworkPropertyMetadata   fMetadata,
            Style                       selfStyle,
            Style                       selfThemeStyle,
            ref ChildRecord             childRecord,
            bool                        isChildRecordValid,
            bool                        hasStyleChanged,
            bool                        isSelfInheritanceParent,
            bool                        wasSelfInheritanceParent)
        {
            Debug.Assert(d != null, "Must have non-null current node");

            // This must be an inherited dependency property
            Debug.Assert(fMetadata.IsInherited == true, "This must be an inherited dependency property");

            // IsSelfInheritanceParent can only change from false to true
            Debug.Assert(!wasSelfInheritanceParent || isSelfInheritanceParent, "IsSelfInheritanceParent changed from true to false");

            // Children do not need to inherit properties across a tree boundary
            // unless the property is set to override this behavior.

            if (!SkipNext(fo.InheritanceBehavior) || fMetadata.OverridesInheritanceBehavior)
            {
                InheritablePropertyChangeInfo rootInfo = info.GetRootInheritableValue(dp);

                EffectiveValueEntry oldEntry = rootInfo.OldEntry;
                EffectiveValueEntry newEntry = info.IsAddOperation ? rootInfo.NewEntry : new EffectiveValueEntry(dp, BaseValueSourceInternal.Inherited);

                bool isForceInheritedProperty = IsForceInheritedProperty(dp);

                if (d != info.Root)
                {
                    if (wasSelfInheritanceParent)
                    {
                        oldEntry = d.GetValueEntry(
                                d.LookupEntry(dp.GlobalIndex),
                                dp,
                                fMetadata,
                                RequestFlags.DeferredReferences);
                    }
                    else if (isSelfInheritanceParent)
                    {
                        // IsSelfInheritanceParent can change from false to true during
                        // a visual tree change if the change induces a style change
                        // (from a DynamicResource or implicit style that now resolves)
                        // where the new style sets an inheritable property.  In this
                        // case, we should not invalidate the current property if it
                        // was set by the new style, but allow other
                        // properties to propagate as if the style change and
                        // IsSelfInheritanceParent change hadn't happened 
                        EffectiveValueEntry currentEntry = d.GetValueEntry(
                                d.LookupEntry(dp.GlobalIndex),
                                dp,
                                fMetadata,
                                RequestFlags.DeferredReferences);
                        if (currentEntry.BaseValueSourceInternal <= BaseValueSourceInternal.Inherited)
                        {
                            oldEntry = oldEntry.GetFlattenedEntry(RequestFlags.FullyResolved);
                            oldEntry.BaseValueSourceInternal = BaseValueSourceInternal.Inherited;
                        }
                        else
                        {
                            oldEntry = currentEntry;
                        }
                    }
                    else
                    {
                        oldEntry = oldEntry.GetFlattenedEntry(RequestFlags.FullyResolved);
                        oldEntry.BaseValueSourceInternal = BaseValueSourceInternal.Inherited;
                    }
                }
                else if (info.IsAddOperation &&
                    (isForceInheritedProperty || oldEntry.BaseValueSourceInternal <= BaseValueSourceInternal.Inherited))
                {
                    // before calling UpdateEffectiveValue on the root of the changed subtree,
                    // see if the property now has a higher-precedence value because
                    // a resource reference now resolves.  If so, don't invalidate 
                    EffectiveValueEntry currentEntry = d.GetValueEntry(
                        d.LookupEntry(dp.GlobalIndex),
                        dp,
                        fMetadata,
                        RequestFlags.DeferredReferences);
                    if (currentEntry.BaseValueSourceInternal > BaseValueSourceInternal.Inherited)
                    {
                        oldEntry = currentEntry;
                    }
                }

                OperationType operationType = info.IsAddOperation ? OperationType.AddChild : OperationType.RemoveChild;
                if (BaseValueSourceInternal.Inherited >= oldEntry.BaseValueSourceInternal)
                {
                    // If the oldValueSource is of lower precedence than Inheritance
                    // only then do we need to Invalidate the property. Examples of
                    // values with higher precedence are those that are locally set
                    // or set via a style/template.
                    return (d.UpdateEffectiveValue(
                                d.LookupEntry(dp.GlobalIndex),
                                dp,
                                fMetadata,
                                oldEntry,
                                ref newEntry,
                                false /* coerceWithDeferredReference */,
                                false /* coerceWithCurrentValue */,
                                operationType)
                            & (UpdateResult.ValueChanged | UpdateResult.InheritedValueOverridden))
                            == UpdateResult.ValueChanged;
                        // return false if either the value didn't change or
                        // it changed because the inherited value was overridden by coercion or animation.
                }
                else if (isForceInheritedProperty)
                {
                    // IsCoerced == true && value == UnsetValue indicates that we need to re-coerce this value
                    newEntry = new EffectiveValueEntry(dp, FullValueSource.IsCoerced);

                    // Re-coerce a force inherited property because it's coersion depends on treeness
                    return (d.UpdateEffectiveValue(
                                d.LookupEntry(dp.GlobalIndex),
                                dp,
                                fMetadata,
                                oldEntry,
                                ref newEntry,
                                false /* coerceWithDeferredReference */,
                                false /* coerceWithCurrentValue */,
                                operationType)
                            & (UpdateResult.ValueChanged | UpdateResult.InheritedValueOverridden))
                            == UpdateResult.ValueChanged;
                        // return false if either the value didn't change or
                        // it changed because the inherited value was overridden by coercion or animation.
                }
            }

            return false;
        }

        #endregion TreeChange

        #region ResourcesChange

        /// <summary>
        ///     Invalidates all the properties on the nodes in the given sub-tree
        ///     that are referring to the resource[s] that are changing.
        /// </summary>
        internal static void InvalidateOnResourcesChange(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            ResourcesChangeInfo     info)
        {
            Debug.Assert(fe != null || fce != null, "Node with the resources change notification must be an FE or an FCE.");

            // We're interested in changes to the Template property that occur during
            // the walk - if the template has changed we don't need to invalidate
            // template-driven properties a second time.  The HasTemplateChanged property
            // is cleared on the first visit to each node, so that it means "template
            // changed during the walk".  But one relevant node isn't visited during
            // the walk - the templated parent of the initial node.  So we handle that now.
            FrameworkObject fo = new FrameworkObject(fe, fce);

            fo.Reset(fo.TemplatedParent);
            fo.HasTemplateChanged = false;

            DependencyObject d = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

            if (HasChildren(fe, fce))
            {
                // Spin up a DescendentsWalker only when
                // the current node has children to walk

                DescendentsWalker<ResourcesChangeInfo> walker = new DescendentsWalker<ResourcesChangeInfo>(
                    TreeWalkPriority.LogicalTree, ResourcesChangeDelegate, info);

                walker.StartWalk(d);
            }
            else
            {
                // Degenerate case when the current node is a leaf node and has no children.

                OnResourcesChanged(d, info, true);
            }
        }

        /// <summary>
        ///     Callback on visiting each node in the descendency
        ///     during a resources change.
        /// </summary>
        private static bool OnResourcesChangedCallback(
            DependencyObject    d,
            ResourcesChangeInfo info,
            bool                visitedViaVisualTree)
        {
            OnResourcesChanged(d, info, true);

            // Continue walk down subtree
            return true;
        }


        /// <summary>
        ///     Process a resource change for the given DependencyObject.
        ///     Return true if the DO has resource references.
        /// </summary>
        internal static void OnResourcesChanged(
            DependencyObject    d,
            ResourcesChangeInfo info,
            bool raiseResourceChangedEvent)
        {
            Debug.Assert(d != null, "Must have non-null current node");

            bool containsTypeOfKey = info.Contains(d.DependencyObjectType.SystemType, true /*isImplicitStyleKey*/);
            bool isSystemResourcesChange = info.IsThemeChange;
            bool isStyleResourcesChange = info.IsStyleResourcesChange;
            bool isTemplateResourcesChange = info.IsTemplateResourcesChange;
            bool isContainer = (info.Container == d);

            FrameworkObject fo = new FrameworkObject(d);

            // If a resource dictionary changed above this node then we need to
            // synchronize the ShouldLookupImplicitStyles flag with respect to
            // our parent here.

            if (info.IsResourceAddOperation || info.IsCatastrophicDictionaryChange)
            {
                fo.SetShouldLookupImplicitStyles();
            }

            // Invalidate implicit and explicit resource
            // references on current instance
            if (fo.IsFE)
            {
                // If this is a FrameworkElement
                FrameworkElement fe = fo.FE;
                fe.HasStyleChanged = false; // detect style changes that arise from work done here
                fe.HasStyleInvalidated = false;
                fe.HasTemplateChanged = false; // detect template changes that arise from work done here

                // if the change affects implicit data templates, notify ContentPresenters
                if (info.IsImplicitDataTemplateChange)
                {
                    ContentPresenter contentPresenter = fe as ContentPresenter;
                    if (contentPresenter != null)
                    {
                        contentPresenter.ReevaluateTemplate();
                    }
                }

                if (fe.HasResourceReference)
                {
                    // Invalidate explicit ResourceReference properties on the current instance.
                    // If the Style property comes from an implicit resource reference that
                    // will be invalidated too.
                    InvalidateResourceReferences(fe, info);

                    // There is no need to invalidate the resources references on the
                    // container object if this call is a result of a style/template
                    // change. This is because the style/template change would have
                    // already invalidated all the container dependents and all the
                    // resources references on the container would have been a part of it.
                    if ((!isStyleResourcesChange && !isTemplateResourcesChange ) || !isContainer)
                    {
                        InvalidateStyleAndReferences(d, info, containsTypeOfKey);
                    }
                }
                else if (containsTypeOfKey &&
                        (fe.HasImplicitStyleFromResources || fe.Style == FrameworkElement.StyleProperty.GetMetadata(fe.DependencyObjectType).DefaultValue))
                {
                    // If The Style property on the given instance has been
                    // fetched by an implicit resource lookup then
                    // it needs to be invalidated. Also we need to do this
                    // invalidation only if the dictionary/resources that is
                    // changing matches the implicit key used for the resource lookup.

                    // The StyleProperty does not need to be invalidated if this
                    // call is the result of a style change
                    if (!isStyleResourcesChange || !isContainer)
                    {
                        fe.UpdateStyleProperty();
                    }
                }

                // If there has been a Theme change then
                // invalidate the ThemeStyleProperty
                if (isSystemResourcesChange)
                {
                    fe.UpdateThemeStyleProperty();
                }

                // Raise the ResourcesChanged Event so that ResourceReferenceExpressions
                // on non-[FE/FCE] (example Freezables) listening for this can then update
                // their values
                if (raiseResourceChangedEvent && fe.PotentiallyHasMentees)
                {
                    fe.RaiseClrEvent(FrameworkElement.ResourcesChangedKey, new ResourcesChangedEventArgs(info));
                }
            }
            else
            {
                // If this is a FrameworkContentElement
                FrameworkContentElement fce = fo.FCE;
                fce.HasStyleChanged = false; // detect style changes that arise from work done here
                fce.HasStyleInvalidated = false;

                if (fce.HasResourceReference)
                {
                    // Invalidate explicit ResourceReference properties on the current instance.
                    // If the Style property comes from an implicit resource reference that
                    // will be invalidated too.
                    InvalidateResourceReferences(fce, info);

                    // There is no need to invalidate the resources references on the
                    // container object if this call is a result of a style/template
                    // change. This is because the style/template change would have
                    // already invalidated all the container dependents and all the
                    // resources references on the container would have been a part of it.
                    if ((!isStyleResourcesChange && !isTemplateResourcesChange ) || !isContainer)
                    {
                        InvalidateStyleAndReferences(d, info, containsTypeOfKey);
                    }
                }
                else if (containsTypeOfKey &&
                        (fce.HasImplicitStyleFromResources || fce.Style == FrameworkContentElement.StyleProperty.GetMetadata(fce.DependencyObjectType).DefaultValue))
                {
                    // If The Style property on the given instance has been
                    // fetched by an implicit resource lookup then
                    // it needs to be invalidated. Also we need to do this
                    // invalidation only if the dictionary/resources that is
                    // changing matches the implicit key used for the resource lookup.

                    // The StyleProperty does not need to be invalidated if this
                    // call is the result of a style change
                    if (!isStyleResourcesChange || !isContainer)
                    {
                        fce.UpdateStyleProperty();
                    }
                }

                // If there has been a Theme change then
                // invalidate the ThemeStyleProperty
                if (isSystemResourcesChange)
                {
                    fce.UpdateThemeStyleProperty();
                }

                // Raise the ResourcesChanged Event so that ResourceReferenceExpressions
                // on non-[FE/FCE] (example Freezables) listening for this can then update
                // their values
                if (raiseResourceChangedEvent && fce.PotentiallyHasMentees)
                {
                    fce.RaiseClrEvent(FrameworkElement.ResourcesChangedKey, new ResourcesChangedEventArgs(info));
                }
            }
        }

        /// <summary>
        ///     Invalidates all properties that reference a resource.
        ///     NOTE: The return value for this method indicates whether or not a ResourceReference
        ///     property was found on the given object. This is to take care of the special case when
        ///     programmatically changing a ResourceReference property value does not reflect on the
        ///     bit stored on FrameworkElement or FrameworkContentElement that indicates whether
        ///     the current instance has ResourceReference values set on it. This current operation
        ///     is a point of synchronization for this flag.
        /// </summary>
        /// <remarks>
        ///     This methods is called when one of the following operations occurred.
        ///     1) A tree change
        ///     2) A resource dictionary change
        ///     3) A modification to a single entry in a dictionary
        /// </remarks>
        private static void InvalidateResourceReferences(
            DependencyObject    d,
            ResourcesChangeInfo info)
        {
            Debug.Assert(d != null, "Must have non-null current node");

            // Find properties that have resource reference value
            LocalValueEnumerator localValues = d.GetLocalValueEnumerator();
            int localValuesCount = localValues.Count;

            if (localValuesCount > 0)
            {
                // Resource reference invalidation involves two passes - first to
                // pick out what we need to invalidate, and the second to do the
                // actual invalidation.  This is needed because LocalValueEnumerator
                // will halt if any local values have changed, which can happen
                // depending on what people are doing in their OnPropertyChanged
                // callback.

                // The following array is used to track the ResourceReferenceExpressions that we find
                ResourceReferenceExpression[] resources = new ResourceReferenceExpression[localValuesCount];
                int invalidationCount = 0;

                // Pass #1 - find what needs invalidation
                while (localValues.MoveNext())
                {
                    // Is this a resource reference?
                    ResourceReferenceExpression resource = localValues.Current.Value as ResourceReferenceExpression;
                    if (resource != null)
                    {
                        // Record this property if it is referring
                        // to a resource that is being changed
                        if (info.Contains(resource.ResourceKey, false /*isImplicitStyleKey*/))
                        {
                            resources[invalidationCount]  = resource;
                            invalidationCount++;
                        }
                    }
                }

                ResourcesChangedEventArgs args = new ResourcesChangedEventArgs(info);

                // Pass #2 - actually make the invalidation calls, now that we're
                // outside the LocalValueEnumerator.
                for (int i = 0; i < invalidationCount; i++)
                {
                    // Let the resource reference throw away its cache
                    // and invalidate the property in which it's held
                    // re-evaluate expression
                    resources[i].InvalidateExpressionValue(d, args);
                }
            }
        }

        /// <summary>
        ///     Invalidates all properties that reference a resource and are set via a style/template.
        /// </summary>
        /// <remarks>
        ///     This methods is called when one of the following operations occurred.
        ///     1) A resource dictionary change
        ///     2) A modification to a single entry in a dictionary
        /// </remarks>
        private static void InvalidateStyleAndReferences(
            DependencyObject    d,
            ResourcesChangeInfo info,
            bool                containsTypeOfKey)
        {
            Debug.Assert(d != null, "Must have non-null current node");

            FrameworkObject fo = new FrameworkObject(d);

            if (fo.IsFE)
            {
                FrameworkElement fe = fo.FE;

                if (containsTypeOfKey &&
                    !info.IsThemeChange &&
                    (fe.HasImplicitStyleFromResources || fe.Style == FrameworkElement.StyleProperty.GetMetadata(fe.DependencyObjectType).DefaultValue))
                {
                    // If The Style property on the given instance has been
                    // fetched by an implicit resource lookup then
                    // it needs to be invalidated. Also we need to do this
                    // invalidation only if the dictionary/resources that is
                    // changing matches the implicit key used for the resource lookup.
                    // If we invalidate the style then we do not need to
                    // InvalidateResourceDependents because applying an
                    // all new style will have the same effect
                    fe.UpdateStyleProperty();
                }

                if (fe.Style != null && fe.Style.HasResourceReferences)
                {
                    // Check for resource references contained within associated Style.
                    // If found, invalidate all properties that are being driven by a resource.
                    // If the style has changed recently, that change would have already
                    // invalidated these properties.
                    if (!fe.HasStyleChanged)
                    {
                        StyleHelper.InvalidateResourceDependents(d, info, ref fe.Style.ResourceDependents,
                            false /* invalidateVisualTreeToo */);
                    }
                }

                if (fe.TemplateInternal != null && fe.TemplateInternal.HasContainerResourceReferences)
                {
                    // Check for resource references contained within associated Template.
                    // If found, invalidate all properties that are being driven by a resource
                    StyleHelper.InvalidateResourceDependents(d, info, ref fe.TemplateInternal.ResourceDependents,
                        false /* invalidateVisualTreeToo */);
                }

                if (fe.TemplateChildIndex > 0)
                {
                    // Check for resource references contained within parent's Template.
                    // If found, invalidate all properties that are being driven by a resource
                    FrameworkElement templatedParent = (FrameworkElement)fe.TemplatedParent;
                    FrameworkTemplate parentTemplate = templatedParent.TemplateInternal;

                    if (!templatedParent.HasTemplateChanged && parentTemplate.HasChildResourceReferences)
                    {
                        StyleHelper.InvalidateResourceDependentsForChild(
                                            templatedParent,
                                            fe,
                                            fe.TemplateChildIndex,
                                            info,
                                            parentTemplate);
                    }
                }


                if (!info.IsThemeChange)
                {
                    // Invalidate ResourceReferences on ThemeStyle only if this insn't a Theme change.
                    // It it is then ThemeStyle would already have been invalidated and hence there isn't
                    // a need to duplicate it here.
                    Style themeStyle = fe.ThemeStyle;
                    if (themeStyle != null && themeStyle.HasResourceReferences)
                    {
                        if (themeStyle != fe.Style)
                        {
                            StyleHelper.InvalidateResourceDependents(d, info, ref themeStyle.ResourceDependents,
                                false /* invalidateVisualTreeToo */);
                        }
                    }
                }
            }
            else if (fo.IsFCE)
            {
                FrameworkContentElement fce = fo.FCE;

                if (containsTypeOfKey &&
                    !info.IsThemeChange &&
                    (fce.HasImplicitStyleFromResources || fce.Style == FrameworkContentElement.StyleProperty.GetMetadata(fce.DependencyObjectType).DefaultValue))
                {
                    // If The Style property on the given instance has been
                    // fetched by an implicit resource lookup then
                    // it needs to be invalidated. Also we need to do this
                    // invalidation only if the dictionary/resources that is
                    // changing matches the implicit key used for the resource lookup.
                    // If we invalidate the style then we do not need to
                    // InvalidateResourceDependents because applying an
                    // all new style will have the same effect
                    fce.UpdateStyleProperty();
                }

                if (fce.Style != null && fce.Style.HasResourceReferences)
                {
                    // Check for resource references contained within associated Style.
                    // If found, invalidate all properties that are being driven by a resource
                    // If the style has changed recently, that change would have already
                    // invalidated these properties.
                    if (!fce.HasStyleChanged)
                    {
                        StyleHelper.InvalidateResourceDependents(d, info, ref fce.Style.ResourceDependents, !
                            false /*invalidateVisualTreeToo */);
                    }
                }


                if (fce.TemplateChildIndex > 0)
                {
                    // Check for resource references contained within parent's Template.
                    // If found, invalidate all properties that are being driven by a resource
                    FrameworkElement templatedParent = (FrameworkElement)fce.TemplatedParent;
                    FrameworkTemplate parentTemplate = templatedParent.TemplateInternal;

                    if (!templatedParent.HasTemplateChanged && parentTemplate.HasChildResourceReferences)
                    {
                        StyleHelper.InvalidateResourceDependentsForChild(
                                            templatedParent,
                                            fce,
                                            fce.TemplateChildIndex,
                                            info,
                                            parentTemplate);
                    }
                }

                if (!info.IsThemeChange)
                {
                    // Invalidate ResourceReferences on ThemeStyle only if this insn't a Theme change.
                    // It it is then ThemeStyle would already have been invalidated and hence there isn't
                    // a need to duplicate it here.
                    Style themeStyle = fce.ThemeStyle;
                    if (themeStyle != null && themeStyle.HasResourceReferences)
                    {
                        if (themeStyle != fce.Style)
                        {
                            StyleHelper.InvalidateResourceDependents(d, info, ref themeStyle.ResourceDependents,
                                false /*invalidateVisualTreeToo */);
                        }
                    }
                }
            }
        }

        #endregion ResourcesChange

        #region InheritablePropertyChange

        /// <summary>
        /// </summary>
        internal static void InvalidateOnInheritablePropertyChange(
            FrameworkElement              fe,
            FrameworkContentElement       fce,
            InheritablePropertyChangeInfo info,
            bool                          skipStartNode)
        {
            DependencyProperty dp = info.Property;
            FrameworkObject fo = new FrameworkObject(fe, fce);
            Debug.Assert(fo.IsValid, "Node with the resources change notification must be an FE or an FCE.");

            if (HasChildren(fe, fce))
            {
                // Spin up a DescendentsWalker only when
                // the current node has children to walk

                DependencyObject d = fo.DO;

                DescendentsWalker<InheritablePropertyChangeInfo> walker = new DescendentsWalker<InheritablePropertyChangeInfo>(
                    TreeWalkPriority.LogicalTree, InheritablePropertyChangeDelegate, info);

                walker.StartWalk(d, skipStartNode);
            }
            else if (!skipStartNode)
            {
                // Degenerate case when the current node is a leaf node and has no children.
                // If the current node needs a notification, do so now.
                bool visitedViaVisualTree = false;
                OnInheritablePropertyChanged(fo.DO, info, visitedViaVisualTree);
            }
        }

        /// <summary>
        ///     Callback on visiting each node in the descendency
        ///     during an inheritable property change
        /// </summary>
        private static bool OnInheritablePropertyChanged(
            DependencyObject              d,
            InheritablePropertyChangeInfo info,
            bool                          visitedViaVisualTree)
        {
            Debug.Assert(d != null, "Must have non-null current node");

            DependencyProperty dp = info.Property;
            EffectiveValueEntry oldEntry = info.OldEntry;
            EffectiveValueEntry newEntry = info.NewEntry;

            InheritanceBehavior inheritanceBehavior;
            bool inheritanceNode = IsInheritanceNode(d, dp, out inheritanceBehavior);
            bool isForceInheritedProperty = IsForceInheritedProperty(dp);

            // Note that if a node is marked SkipNext means it hasn't acquired any values from its parent and
            // hence we do not need to invalidate this node or any of its descendents. However if a node is
            // marked SkipNow then this node might have acquired values from its parent but none of its
            // descendents would. Hence in this case we process the current node but omit all of its descendents.
            if (inheritanceNode && (!SkipNext(inheritanceBehavior) || isForceInheritedProperty))
            {
                PropertyMetadata metadata = dp.GetMetadata(d);
                EntryIndex entryIndex = d.LookupEntry(dp.GlobalIndex);

                // Found an inheritance node
                if (!d.IsSelfInheritanceParent)
                {
                    DependencyObject parent = FrameworkElement.GetFrameworkParent(d);
                    InheritanceBehavior parentInheritanceBehavior = InheritanceBehavior.Default;

                    if (parent != null)
                    {
                        FrameworkObject parentFO = new FrameworkObject(parent, true);
                        parentInheritanceBehavior = parentFO.InheritanceBehavior;
                    }

                    if (!SkipNext(inheritanceBehavior) && !SkipNow(parentInheritanceBehavior))
                    {
                        // Synchronize InheritanceParent
                        d.SynchronizeInheritanceParent(parent);
                    }

                    // What should the oldValueSource on the child be?
                    // When the oldValue on the parent was default it
                    // means that the child also used its own default
                    // and did not inherit from the parent. However
                    // when the value on the parent was non-default
                    // it means that the child inherited it.
                    // Note that the oldValueSource on inheritablePropertyChangedData
                    // is actually the parent's oldValueSource

                    if (oldEntry.BaseValueSourceInternal == BaseValueSourceInternal.Unknown)
                    {
                        // we use an empty EffectiveValueEntry as a signal that the old entry was the default value
                        oldEntry = EffectiveValueEntry.CreateDefaultValueEntry(dp, metadata.GetDefaultValue(d, dp));
                    }
                }
                else
                {
                    oldEntry = d.GetValueEntry(
                                        entryIndex,
                                        dp,
                                        metadata,
                                        RequestFlags.RawEntry);
                 }

                // If the oldValueSource is of lower precedence than Inheritance
                // only then do we need to Invalidate the property
                if (BaseValueSourceInternal.Inherited >= oldEntry.BaseValueSourceInternal)
                {
                    if (visitedViaVisualTree && FrameworkElement.DType.IsInstanceOfType(d))
                    {
                        DependencyObject logicalParent = LogicalTreeHelper.GetParent(d);
                        if (logicalParent != null)
                        {
                            DependencyObject visualParent = VisualTreeHelper.GetParent(d);
                            if (visualParent != null && visualParent != logicalParent)
                            {
                                // Consider the following logical tree configuration. In this case we want
                                // to RibbonToggleButton to pick up the new DataContext flowing in from
                                // the Window.
                                //
                                // Window (info.RootElement)
                                //   ...
                                //   RibbonGroup (IsCollapsed)
                                //      RibbonControl (only in visual tree)
                                //          RibbonToggleButton
                                //
                                // Consider the following logical tree configuration. In this case we do not
                                // want to RibbonToggleButton to change its DataContext because the changes
                                // are only within the visual tree.
                                //
                                // Window
                                //   ...
                                //   RibbonGroup (IsCollapsed)
                                //      RibbonControl (only in visual tree) (info.RootElement)
                                //          RibbonToggleButton
                                //
                                // Saying it another way, the RibbonToggleButton in the above case belongs in a
                                // different logical tree than the one that the current invalidation storm begun.
                                //
                                // Any change in an inheritable property begins an invalidation storm using the
                                // DescendentsWalker and configures it to first traverse the logical children
                                // and then visual children. Also nodes that have previously been visited via the
                                // logical tree do not get visited again through the visual tree. I use this very
                                // behavior as the basis for detecting nodes such as RibbonToggleButton. If the
                                // RibbonToggleButton is being visisted for the first time via the visual tree then
                                // the invalidation storm did not include its logical parent. And therefore the
                                // RibbonToggleButton can early out of this storm.
                                return false;
                            }
                        }
                    }

                    // Since we do not hold a cache of the oldValue we need to supply one
                    // in order to correctly fire the change notification
                    return (d.UpdateEffectiveValue(
                            entryIndex,
                            dp,
                            metadata,
                            oldEntry,
                            ref newEntry,
                            false /* coerceWithDeferredReference */,
                            false /* coerceWithCurrentValue */,
                            OperationType.Inherit)
                        & (UpdateResult.ValueChanged | UpdateResult.InheritedValueOverridden))
                        == UpdateResult.ValueChanged;
                    // return false if either the value didn't change or
                    // it changed because the inherited value was overridden by coercion or animation.
                }
                else if (isForceInheritedProperty)
                {
                    // IsCoerced == true && value == UnsetValue indicates that we need to re-coerce this value
                    newEntry = new EffectiveValueEntry(dp, FullValueSource.IsCoerced);

                    // Re-coerce a force inherited property because it's coersion depends on treeness
                    return (d.UpdateEffectiveValue(
                            d.LookupEntry(dp.GlobalIndex),
                            dp,
                            metadata,
                            oldEntry,
                            ref newEntry,
                            false /* coerceWithDeferredReference */,
                            false /* coerceWithCurrentValue */,
                            OperationType.Inherit)
                        & (UpdateResult.ValueChanged | UpdateResult.InheritedValueOverridden))
                        == UpdateResult.ValueChanged;
                    // return false if either the value didn't change or
                    // it changed because the inherited value was overridden by coercion or animation.
                }
                else
                {
                    return false;
                }
            }

            // Do not continue walk down subtree if the walk was forced to stop
            // (due to separated trees)
            return (inheritanceBehavior == InheritanceBehavior.Default || isForceInheritedProperty);
        }

        // raise the InheritedPropertyChanged event to mentees.  Called from FE/FCE
        // OnPropertyChanged
        internal static void OnInheritedPropertyChanged(DependencyObject d,
                                            ref InheritablePropertyChangeInfo info,
                                            InheritanceBehavior inheritanceBehavior)
        {
            if (inheritanceBehavior == InheritanceBehavior.Default || IsForceInheritedProperty(info.Property))
            {
                FrameworkObject fo = new FrameworkObject(d);
                fo.OnInheritedPropertyChanged(ref info);
            }
        }

        /// <summary>
        ///     Determine if the current DependencyObject is a candidate for
        ///     producing inheritable values
        /// </summary>
        /// <remarks>
        ///     This is called by both InvalidateTree and GetValueCore
        /// </remarks>
        internal static bool IsInheritanceNode(
            DependencyObject    d,
            DependencyProperty  dp,
        out InheritanceBehavior inheritanceBehavior)
        {
            // Assume can continue search
            inheritanceBehavior = InheritanceBehavior.Default;

            // Get Framework metadata (if exists)
            FrameworkPropertyMetadata metadata = dp.GetMetadata(d.DependencyObjectType) as FrameworkPropertyMetadata;

            // Check for correct type of metadata
            if (metadata != null)
            {
                FrameworkObject fo = new FrameworkObject(d);

                if (fo.IsValid)
                {
                    // If parent is a Framework type, then check if it is at a
                    // tree separation boundary. Stop inheritance at the boundary unless
                    // overridden by the medata.OverridesInheritanceBehavior flag.

                    // GetValue from Parent only if instance is not a TreeSeparator
                    // or fmetadata.OverridesInheritanceBehavior is set to override separated tree behavior
                    if (fo.InheritanceBehavior != InheritanceBehavior.Default && !metadata.OverridesInheritanceBehavior)
                    {
                        // Hit a tree boundary
                        inheritanceBehavior = fo.InheritanceBehavior;
                    }
                }
                else
                {
                    // If not a Framework type, then, this isn't an inheritance node.
                    // Only Framework types know how to inherit

                    return false;
                }

                // Check if metadata is marked as inheritable
                if (metadata.Inherits)
                {
                    return true;
                }
            }

            // Not a framework type with inheritable metadata
            return false;
        }

        /// <summary>
        ///     FrameworkElement variant of IsInheritanceNode
        /// </summary>
        internal static bool IsInheritanceNode(
            FrameworkElement        fe,
            DependencyProperty      dp,
            out InheritanceBehavior inheritanceBehavior)
        {
            // Assume can continue search
            inheritanceBehavior = InheritanceBehavior.Default;

            // Get Framework metadata (if exists)
            FrameworkPropertyMetadata metadata = dp.GetMetadata(fe.DependencyObjectType) as FrameworkPropertyMetadata;

            // Check for correct type of metadata
            if (metadata != null)
            {
                if (fe.InheritanceBehavior != InheritanceBehavior.Default && !metadata.OverridesInheritanceBehavior)
                {
                    // Hit a tree boundary
                    inheritanceBehavior = fe.InheritanceBehavior;
                }

                // Return true if metadata is marked as inheritable; false otherwise
                return metadata.Inherits;
            }

            // Not framework type metadata
            return false;
        }

        /// <summary>
        ///     FrameworkContentElement variant of IsInheritanceNode
        /// </summary>
        internal static bool IsInheritanceNode(
            FrameworkContentElement fce,
            DependencyProperty      dp,
            out InheritanceBehavior inheritanceBehavior)
        {
            // Assume can continue search
            inheritanceBehavior = InheritanceBehavior.Default;

            // Get Framework metadata (if exists)
            FrameworkPropertyMetadata metadata = dp.GetMetadata(fce.DependencyObjectType) as FrameworkPropertyMetadata;

            // Check for correct type of metadata
            if (metadata != null)
            {
                if (fce.InheritanceBehavior != InheritanceBehavior.Default && !metadata.OverridesInheritanceBehavior)
                {
                    // Hit a tree boundary
                    inheritanceBehavior = fce.InheritanceBehavior;
                }

                // Return true if metadata is marked as inheritable; false otherwise
                return metadata.Inherits;
            }

            // Not framework type metadata
            return false;
        }

        /// <summary>
        ///     Says if the given value has SkipNow behavior
        /// </summary>
        internal static bool SkipNow(InheritanceBehavior inheritanceBehavior)
        {
            if (inheritanceBehavior == InheritanceBehavior.SkipToAppNow ||
                inheritanceBehavior == InheritanceBehavior.SkipToThemeNow ||
                inheritanceBehavior == InheritanceBehavior.SkipAllNow)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Says if the given value has SkipNext behavior
        /// </summary>
        internal static bool SkipNext(InheritanceBehavior inheritanceBehavior)
        {
            if (inheritanceBehavior == InheritanceBehavior.SkipToAppNext ||
                inheritanceBehavior == InheritanceBehavior.SkipToThemeNext ||
                inheritanceBehavior == InheritanceBehavior.SkipAllNext)
            {
                return true;
            }

            return false;
        }

        #endregion InheritablePropertyChange

        #region PrivateMethods

        /// <summary>
        ///     Says if the current FE or FCE has visual or logical children
        /// </summary>
        internal static bool HasChildren(FrameworkElement fe, FrameworkContentElement fce)
        {
            // See if we have logical or visual children, in which case this is a real tree invalidation.
            return ( (fe != null && (fe.HasLogicalChildren ||
                                               fe.HasVisualChildren ||
                                               (Popup.RegisteredPopupsField.GetValue(fe) != null)
                                              )
                         ) ||
                        (fce != null && fce.HasLogicalChildren)
                      );
        }

        /// <summary>
        ///     Says if the given property is a force inherited property
        /// </summary>
        private static bool IsForceInheritedProperty(DependencyProperty dp)
        {
            // NOTE: this is not really a force-inherited property, rather a property
            // that does not want to stop invalidations when a no-change state is
            // encountered.
            //
            // As of 2/5/2005, ForceInherited properties moved to Core, but
            // FlowDircectionProperty remains here to avoid a breaking change.
            return (dp == FrameworkElement.FlowDirectionProperty);
        }

        #endregion PrivateMethods

        #region StaticData

        // Delegates used for the DescendentsWalker TreeWalk.

        private static VisitedCallback<TreeChangeInfo> TreeChangeDelegate
            = new VisitedCallback<TreeChangeInfo>(OnAncestorChanged);

        private static VisitedCallback<TreeChangeInfo> TreeChangePostDelegate
            = new VisitedCallback<TreeChangeInfo>(OnPostAncestorChanged);

        private static VisitedCallback<ResourcesChangeInfo> ResourcesChangeDelegate
            = new VisitedCallback<ResourcesChangeInfo>(OnResourcesChangedCallback);

        private static VisitedCallback<InheritablePropertyChangeInfo> InheritablePropertyChangeDelegate
            = new VisitedCallback<InheritablePropertyChangeInfo>(OnInheritablePropertyChanged);

        #endregion StaticData
    }

    #endregion TreeWalkHelper
}


