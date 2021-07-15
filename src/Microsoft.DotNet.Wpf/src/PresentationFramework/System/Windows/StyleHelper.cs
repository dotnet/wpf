// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Style and templating data structures and helper methods.
*
*
\***************************************************************************/
using MS.Internal;                      // Helper
using MS.Utility;                       // ItemStructList<ChildValueLookup>
using System.Collections;               // Hashtable
using System.Collections.Generic;       // List<T>
using System.Collections.Specialized;   // HybridDictionary
using System.ComponentModel;            // TypeConverter, TypeDescriptor
using System.Diagnostics;               // Debug.Assert
using System.Runtime.CompilerServices;  // ConditionalWeakTable
using System.Windows.Controls;          // Control, ContentPresenter
using System.Windows.Data;              // BindingExpression
using System.Windows.Documents;         // TableRowGroup,TableRow
using System.Windows.Media;             // VisualCollection
using System.Windows.Media.Animation;   // Storyboard
using System.Windows.Markup;            // MarkupExtension
using System.Windows.Threading;         // DispatcherObject
using System.Threading;                 // Interlocked
using MS.Internal.Data;                 // BindingValueChangedEventArgs
using System.Globalization;
using System.Reflection;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows
{
    #region StyleHelper

    internal static class StyleHelper
    {
        //  ===========================================================================
        //  These methods are invoked when a Style/Template cache needs to be updated
        //  ===========================================================================

        #region UpdateCache

        //
        //  This method
        //  1. Updates the style cache for the given fe/fce
        //
        internal static void UpdateStyleCache(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            Style                   oldStyle,
            Style                   newStyle,
            ref Style               styleCache)
        {
            Debug.Assert(fe != null || fce != null);

            if (newStyle != null)
            {
                // We have a new style.  Make sure it's targeting the right
                // type, and then seal it.

                DependencyObject d = fe;
                if (d == null)
                {
                    d = fce;
                }
                newStyle.CheckTargetType(d);
                newStyle.Seal();
            }

            styleCache = newStyle;

            // Do style property invalidations. Note that some of the invalidations may be callouts
            // that could turn around and query the style property on this node. Hence it is essential
            // to update the style cache before we do this operation.
            StyleHelper.DoStyleInvalidations(fe, fce, oldStyle, newStyle);

            // Now look for triggers that might want their EnterActions or ExitActions
            //  to run immediately.
            StyleHelper.ExecuteOnApplyEnterExitActions(fe, fce, newStyle, StyleDataField);
        }

        //
        //  This method
        //  1. Updates the theme style cache for the given fe/fce
        //
        internal static void UpdateThemeStyleCache(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            Style                   oldThemeStyle,
            Style                   newThemeStyle,
            ref Style               themeStyleCache)
        {
            Debug.Assert(fe != null || fce != null);

            if (newThemeStyle != null)
            {
                DependencyObject d = fe;
                if (d == null)
                {
                    d = fce;
                }
                newThemeStyle.CheckTargetType(d);
                newThemeStyle.Seal();

#pragma warning disable 6503
                // Check if the theme style has the OverridesDefaultStyle  property set on the target tag or any of its
                // visual triggers. It is an error to specify the OverridesDefaultStyle  in your own ThemeStyle.
                if (StyleHelper.IsSetOnContainer(FrameworkElement.OverridesDefaultStyleProperty, ref newThemeStyle.ContainerDependents, true))
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotHaveOverridesDefaultStyleInThemeStyle));
                }
                // Check if the theme style has EventHandlers set on the target tag or int its setter collection.
                // We do not support EventHandlers in a ThemeStyle
                if (newThemeStyle.HasEventSetters)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotHaveEventHandlersInThemeStyle));
                }
#pragma warning restore 6503
            }

            themeStyleCache = newThemeStyle;

            Style style = null;

            if (fe != null)
            {
                if(ShouldGetValueFromStyle ( FrameworkElement.DefaultStyleKeyProperty ) )
                {
                    style = fe.Style;
                }
            }
            else
            {
                if(ShouldGetValueFromStyle ( FrameworkContentElement.DefaultStyleKeyProperty ) )
                {
                    style = fce.Style;
                }
            }

            // Do theme style property invalidations. Note that some of the invalidations may be callouts
            // that could turn around and query the theme style property on this node. Hence it is essential
            // to update the theme style cache before we do this operation.
            StyleHelper.DoThemeStyleInvalidations(fe, fce, oldThemeStyle, newThemeStyle, style);

            // Now look for triggers that might want their EnterActions or ExitActions
            //  to run immediately.
            StyleHelper.ExecuteOnApplyEnterExitActions(fe, fce, newThemeStyle, ThemeStyleDataField);
        }

        //
        //  This method
        //  1. Find the ThemeStyle for the given Framework[Content]Element
        //  2. Checks for a self-reference, but does not check for loops.
        //     The caller (Seal() or anybody else) is expected to call
        //     CheckForCircularBasedOnReferences() to check for this condition.
        //
        internal static Style GetThemeStyle(FrameworkElement fe, FrameworkContentElement fce)
        {
            Debug.Assert((fe != null && fce == null)|| (fe == null && fce != null));

            // Fetch the DefaultStyleKey and the self Style for
            // the given Framework[Content]Element
            object themeStyleKey = null;
            Style oldThemeStyle = null;
            Style newThemeStyle = null;
            bool overridesDefaultStyle;
            if (fe != null)
            {
                // If this is the first time that the ThemeStyleProperty
                // is being fetched then mark it such
                fe.HasThemeStyleEverBeenFetched = true;

                themeStyleKey = fe.DefaultStyleKey;
                overridesDefaultStyle = fe.OverridesDefaultStyle;
              
                oldThemeStyle = fe.ThemeStyle;
            }
            else
            {
                // If this is the first time that the ThemeStyleProperty
                // is being fetched then mark it such
                fce.HasThemeStyleEverBeenFetched = true;

                themeStyleKey = fce.DefaultStyleKey;
                overridesDefaultStyle = fce.OverridesDefaultStyle;
               
                oldThemeStyle = fce.ThemeStyle;
            }

            // Don't lookup properties from the themes if user has specified OverridesDefaultStyle
            // or DefaultStyleKey = null
            if (themeStyleKey != null && !overridesDefaultStyle)
            {
                // Fetch the DependencyObjectType for the ThemeStyleKey
                DependencyObjectType dTypeKey;
                if (fe != null)
                {
                    dTypeKey = fe.DTypeThemeStyleKey;
                }
                else
                {
                    dTypeKey = fce.DTypeThemeStyleKey;
                }

                // First look for an applicable style in system resources
                object styleLookup;
                if (dTypeKey != null && dTypeKey.SystemType != null && dTypeKey.SystemType.Equals(themeStyleKey))
                {
                    // Optimized lookup based on the DependencyObjectType for the DefaultStyleKey
                    styleLookup = SystemResources.FindThemeStyle(dTypeKey);
                }
                else
                {
                    // Regular lookup based on the DefaultStyleKey. Involves locking and Hashtable lookup
                    styleLookup = SystemResources.FindResourceInternal(themeStyleKey);
                }

                if( styleLookup != null )
                {
                    if( styleLookup is Style )
                    {
                        // We have found an applicable Style in system resources
                        //  let's us use that as second stop to find property values.
                        newThemeStyle = (Style)styleLookup;
                    }
                    else
                    {
                        // We found something keyed to the ThemeStyleKey, but it's not
                        //  a style.  This is a problem, throw an exception here.
                        throw new InvalidOperationException(SR.Get(
                            SRID.SystemResourceForTypeIsNotStyle, themeStyleKey));
                    }
                }

                if (newThemeStyle == null)
                {
                    // No style in system resources, try to retrieve the default
                    //  style for the target type.
                    Type themeStyleTypeKey = themeStyleKey as Type;
                    if (themeStyleTypeKey != null)
                    {
                        PropertyMetadata styleMetadata =
                            FrameworkElement.StyleProperty.GetMetadata(themeStyleTypeKey);

                        if( styleMetadata != null )
                        {
                            // Have a metadata object, get the default style (if any)
                            newThemeStyle = styleMetadata.DefaultValue as Style;
                        }
                    }
                }

            }

            // Propagate change notification
            if (oldThemeStyle != newThemeStyle)
            {
                if (fe != null)
                {
                    FrameworkElement.OnThemeStyleChanged(fe, oldThemeStyle, newThemeStyle);
                }
                else
                {
                    FrameworkContentElement.OnThemeStyleChanged(fce, oldThemeStyle, newThemeStyle);
                }
            }

            return newThemeStyle;
        }



        //
        //  This method
        //  1. Updates the template cache for the given fe/fce
        //
        internal static void UpdateTemplateCache(
            FrameworkElement        fe,
            FrameworkTemplate       oldTemplate,
            FrameworkTemplate       newTemplate,
            DependencyProperty      templateProperty)
        {
            DependencyObject d = fe;

            if (newTemplate != null)
            {
                newTemplate.Seal();

#if DEBUG
                // Check if there is a cyclic reference between the condition properties on a
                // style trigger that triggers the TemplateProperty and the values set by that
                // Template's triggers on the container.  This check is done only when we're
                // not in the middle of updating Style or ThemeStyle.
                if( StyleHelper.ShouldGetValueFromStyle(templateProperty)
                    &&
                    StyleHelper.ShouldGetValueFromThemeStyle(templateProperty))
                {
                    Style style = fe.Style;
                    Style themeStyle = fe.ThemeStyle;
                    StyleHelper.CheckForCyclicReferencesInStyleAndTemplateTriggers(templateProperty, newTemplate, style, themeStyle);
                }
#endif
            }

            // Update the template cache
            fe.TemplateCache = newTemplate;

            // Do template property invalidations. Note that some of the invalidations may be callouts
            // that could turn around and query the template property on this node. Hence it is essential
            // to update the template cache before we do this operation.
            StyleHelper.DoTemplateInvalidations(fe, oldTemplate);

            // Now look for triggers that might want their EnterActions or ExitActions
            //  to run immediately.
            StyleHelper.ExecuteOnApplyEnterExitActions(fe, null, newTemplate);
        }

        #endregion UpdateCache

        //  ===========================================================================
        //  These methods are invoked when a Style/Template is Sealed
        //  ===========================================================================

        #region WriteMethods

        //
        //  This method
        //  1. Seals a template
        //
        internal static void SealTemplate(
            FrameworkTemplate                                           frameworkTemplate,
            ref bool                                                    isSealed,
            FrameworkElementFactory                                     templateRoot,
            TriggerCollection                                           triggers,
            ResourceDictionary                                          resources,
            HybridDictionary                                            childIndexFromChildID,
            ref FrugalStructList<ChildRecord>                           childRecordFromChildIndex,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>>    triggerSourceRecordFromChildIndex,
            ref FrugalStructList<ContainerDependent>                    containerDependents,
            ref FrugalStructList<ChildPropertyDependent>                resourceDependents,
            ref ItemStructList<ChildEventDependent>                     eventDependents,
            ref HybridDictionary                                        triggerActions,
            ref HybridDictionary                                        dataTriggerRecordFromBinding,
            ref bool                                                    hasInstanceValues,
            ref EventHandlersStore                                      eventHandlersStore)
        {
            Debug.Assert(frameworkTemplate != null );

            // This template has already been sealed.
            // There is no more to do.
            if (isSealed)
            {
                return;
            }

            // Seal template nodes (if exists)


            if (frameworkTemplate != null)
            {
                frameworkTemplate.ProcessTemplateBeforeSeal();
            }


            if (templateRoot != null)
            {
                Debug.Assert( !frameworkTemplate.HasXamlNodeContent );

                // Seal the template

                Debug.Assert(frameworkTemplate != null);
                //frameworkTemplate.ProcessTemplateBeforeSeal();
                templateRoot.Seal(frameworkTemplate);
            }

            // Seal triggers
            if (triggers != null)
            {
                triggers.Seal();
            }

            // Seal Resource Dictionary
            if (resources != null)
            {
                resources.IsReadOnly = true;
            }

            //  Build shared tables

            if (templateRoot != null)
            {
                // This is a FEF-style template.  Process the root node, and it will
                // recurse through the rest of the FEF tree.

                StyleHelper.ProcessTemplateContentFromFEF(
                                templateRoot,
                                ref childRecordFromChildIndex,
                                ref triggerSourceRecordFromChildIndex,
                                ref resourceDependents,
                                ref eventDependents,
                                ref dataTriggerRecordFromBinding,
                                childIndexFromChildID,
                                ref hasInstanceValues);
            }

            // Process Triggers. (Trigger PropertyValues are inserted
            // last into the Style/Template GetValue chain because they
            // are the highest priority)

            bool hasHandler = false;

            Debug.Assert( frameworkTemplate != null );

            StyleHelper.ProcessTemplateTriggers(
                triggers,
                frameworkTemplate,
                ref childRecordFromChildIndex,
                ref triggerSourceRecordFromChildIndex, ref containerDependents, ref resourceDependents, ref eventDependents,
                ref dataTriggerRecordFromBinding, childIndexFromChildID, ref hasInstanceValues,
                ref triggerActions, templateRoot, ref eventHandlersStore,
                ref frameworkTemplate.PropertyTriggersWithActions,
                ref frameworkTemplate.DataTriggersWithActions,
                ref hasHandler );

            frameworkTemplate.HasLoadedChangeHandler = hasHandler;

            frameworkTemplate.SetResourceReferenceState();

            // All done, seal self and call it a day.
            isSealed = true;

            // Remove thread affinity so it can be accessed across threads
            frameworkTemplate.DetachFromDispatcher();

            // Check if the template has the Template property set on the container via its visual triggers.
            // It is an error to specify the TemplateProperty in your own Template.
            if (StyleHelper.IsSetOnContainer(Control.TemplateProperty, ref containerDependents, true) ||
                StyleHelper.IsSetOnContainer(ContentPresenter.TemplateProperty, ref containerDependents, true))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHavePropertyInTemplate, Control.TemplateProperty.Name));
            }

            // Check if the template has the Style property set on the container via its visual triggers.
            // It is an error to specify the StyleProperty in your own Template.
            if (StyleHelper.IsSetOnContainer(FrameworkElement.StyleProperty, ref containerDependents, true))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHavePropertyInTemplate, FrameworkElement.StyleProperty.Name));
            }

            // Check if the template has the DefaultStyleKey property set on the container via its visual triggers.
            // It is an error to specify the DefaultStyleKeyProperty in your own Template.
            if (StyleHelper.IsSetOnContainer(FrameworkElement.DefaultStyleKeyProperty, ref containerDependents, true))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHavePropertyInTemplate, FrameworkElement.DefaultStyleKeyProperty.Name));
            }

            // Check if the template has the OverridesDefaultStyle property set on the container via its visual triggers.
            // It is an error to specify the OverridesDefaultStyleProperty in your own Template.
            if (StyleHelper.IsSetOnContainer(FrameworkElement.OverridesDefaultStyleProperty, ref containerDependents, true))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHavePropertyInTemplate, FrameworkElement.OverridesDefaultStyleProperty.Name));
            }

            // Check if the template has the Name property set on the container via its visual triggers.
            // It is an error to specify the Name in your own Template.
            if (StyleHelper.IsSetOnContainer(FrameworkElement.NameProperty, ref containerDependents, true))
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHavePropertyInTemplate, FrameworkElement.NameProperty.Name));
            }
        }



        //
        //  All table datastructures read-lock-free/write-lock
        //  UpdateTables writes the datastructures, locks set by callers
        //
        //  This method
        //  1. Adds a ChildValueLookup entry to the given ChildRecord.
        //     This is used in value computation.
        //  2. Optionally adds a ChildPropertyDependent entry to the given
        //     ContainerRecordFromProperty list. This is used to invalidate
        //     container dependents.
        //  3. Optionally adds a ChildPropertyDependent entry to the given
        //     ResourceDependents list. This is used when invalidating resource
        //     references
        //
        internal static void UpdateTables(
            ref PropertyValue                                           propertyValue,
            ref FrugalStructList<ChildRecord>                           childRecordFromChildIndex,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>>    triggerSourceRecordFromChildIndex,
            ref FrugalStructList<ChildPropertyDependent>                resourceDependents,
            ref HybridDictionary                                        dataTriggerRecordFromBinding,
            HybridDictionary                                            childIndexFromChildName,
            ref bool                                                    hasInstanceValues)
        {
            //
            //  Record instructions for Child/Self value computation
            //

            // Query for child index (may be 0 if "self")
            int childIndex = QueryChildIndexFromChildName(propertyValue.ChildName, childIndexFromChildName);
            if (childIndex == -1)
            {
                throw new InvalidOperationException(SR.Get(SRID.NameNotFound, propertyValue.ChildName));
            }

            object value = propertyValue.ValueInternal;
            bool requiresInstanceStorage = RequiresInstanceStorage(ref value);
            propertyValue.ValueInternal = value;

            childRecordFromChildIndex.EnsureIndex(childIndex);
            ChildRecord childRecord = childRecordFromChildIndex[childIndex];

            int mapIndex = childRecord.ValueLookupListFromProperty.EnsureEntry(propertyValue.Property.GlobalIndex);

            ChildValueLookup valueLookup = new ChildValueLookup();
            valueLookup.LookupType = (ValueLookupType)propertyValue.ValueType; // Maps directly to ValueLookupType for applicable values
            valueLookup.Conditions = propertyValue.Conditions;
            valueLookup.Property = propertyValue.Property;
            valueLookup.Value = propertyValue.ValueInternal;

            childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value.Add(ref valueLookup);

            // Put back modified struct
            childRecordFromChildIndex[childIndex] = childRecord;

            //
            //  Container property invalidation
            //

            switch ((ValueLookupType)propertyValue.ValueType)
            {
            case ValueLookupType.Simple:
                {
                    hasInstanceValues |= requiresInstanceStorage;
                }
                break;

            case ValueLookupType.Trigger:
            case ValueLookupType.PropertyTriggerResource:
                {
                    if( propertyValue.Conditions != null )
                    {
                        // Record the current property as a dependent to each on of the
                        // properties in the condition. This is to facilitate the invalidation
                        // of the current property in the event that any one of the properties
                        // in the condition change. This will allow the current property to get
                        // re-evaluated.
                        for (int i = 0; i < propertyValue.Conditions.Length; i++)
                        {
                            int sourceChildIndex = propertyValue.Conditions[i].SourceChildIndex;
                            triggerSourceRecordFromChildIndex.EnsureIndex(sourceChildIndex);
                            ItemStructMap<TriggerSourceRecord> triggerSourceRecordMap = triggerSourceRecordFromChildIndex[sourceChildIndex];

                            if (propertyValue.Conditions[i].Property == null)
                            {
                                throw new InvalidOperationException(SR.Get(SRID.MissingTriggerProperty));
                            }
                            int index = triggerSourceRecordMap.EnsureEntry(propertyValue.Conditions[i].Property.GlobalIndex);
                            AddPropertyDependent(childIndex, propertyValue.Property,
                                ref triggerSourceRecordMap.Entries[index].Value.ChildPropertyDependents);

                            // Store the triggerSourceRecordMap back into the list after it has been updated
                            triggerSourceRecordFromChildIndex[sourceChildIndex] = triggerSourceRecordMap;
                        }

                        // If value is a resource reference, add dependent on resource changes
                        if ((ValueLookupType)propertyValue.ValueType == ValueLookupType.PropertyTriggerResource)
                        {
                            AddResourceDependent(childIndex, propertyValue.Property, propertyValue.ValueInternal, ref resourceDependents);
                        }
                    }

                    // values in a Trigger may require per-instance storage
                    if ((ValueLookupType)propertyValue.ValueType != ValueLookupType.PropertyTriggerResource)
                    {
                        hasInstanceValues |= requiresInstanceStorage;
                    }
                }
                break;

            case ValueLookupType.DataTrigger:
            case ValueLookupType.DataTriggerResource:
                {
                    if( propertyValue.Conditions != null )
                    {
                        if (dataTriggerRecordFromBinding == null)
                        {
                            dataTriggerRecordFromBinding = new HybridDictionary();
                        }

                        // Record container conditional child property dependents
                        for (int i = 0; i < propertyValue.Conditions.Length; i++)
                        {
                            DataTriggerRecord record = (DataTriggerRecord)dataTriggerRecordFromBinding[propertyValue.Conditions[i].Binding];
                            if (record == null)
                            {
                                record = new DataTriggerRecord();
                                dataTriggerRecordFromBinding[propertyValue.Conditions[i].Binding] = record;
                            }

                            // Add dependent on trigger
                            AddPropertyDependent(childIndex, propertyValue.Property,
                                ref record.Dependents);
                        }

                        // If value is a resource reference, add dependent on resource changes
                        if ((ValueLookupType)propertyValue.ValueType == ValueLookupType.DataTriggerResource)
                        {
                            AddResourceDependent(childIndex, propertyValue.Property, propertyValue.ValueInternal, ref resourceDependents);
                        }
                    }

                    // values in a DataTrigger may require per-instance storage
                    if ((ValueLookupType)propertyValue.ValueType != ValueLookupType.DataTriggerResource)
                    {
                        hasInstanceValues |= requiresInstanceStorage;
                    }
                }
                break;

            case ValueLookupType.TemplateBinding:
                {
                    TemplateBindingExtension templateBinding = (TemplateBindingExtension)propertyValue.ValueInternal;
                    DependencyProperty destinationProperty = propertyValue.Property; // Child
                    DependencyProperty sourceProperty = templateBinding.Property; // Container

                    // Record the current property as a dependent to the aliased
                    // property on the container. This is to facilitate the
                    // invalidation of the current property in the event that the
                    // aliased container property changes. This will allow the current
                    // property to get re-evaluated.
                    int sourceChildIndex = 0; // TemplateBinding is always sourced off of the container
                    triggerSourceRecordFromChildIndex.EnsureIndex(sourceChildIndex);
                    ItemStructMap<TriggerSourceRecord> triggerSourceRecordMap = triggerSourceRecordFromChildIndex[sourceChildIndex];

                    int index = triggerSourceRecordMap.EnsureEntry(sourceProperty.GlobalIndex);
                    AddPropertyDependent(childIndex, destinationProperty, ref triggerSourceRecordMap.Entries[index].Value.ChildPropertyDependents);

                    // Store the triggerSourceRecordMap back into the list after it has been updated
                    triggerSourceRecordFromChildIndex[sourceChildIndex] = triggerSourceRecordMap;
                }
                break;

            case ValueLookupType.Resource:
                {
                    AddResourceDependent(childIndex, propertyValue.Property, propertyValue.ValueInternal, ref resourceDependents);
                }
                break;
            }
        }

        //
        //  1. Seal a property value before entering it into the ValueLookup table.
        //  2. Determine If the value requires per-instance storage.
        //
        private static bool RequiresInstanceStorage(ref object value)
        {
            DeferredReference deferredReference = null;
            MarkupExtension markupExtension     = null;
            Freezable freezable                 = null;

            if ((deferredReference = value as DeferredReference) != null)
            {
                Type valueType = deferredReference.GetValueType();
                if (valueType != null)
                {
                    if (typeof(MarkupExtension).IsAssignableFrom(valueType))
                    {
                        value = deferredReference.GetValue(BaseValueSourceInternal.Style);
                        if ((markupExtension = value as MarkupExtension) == null)
                        {
                            freezable = value as Freezable;
                        }
                    }
                    else if (typeof(Freezable).IsAssignableFrom(valueType))
                    {
                        freezable = (Freezable)deferredReference.GetValue(BaseValueSourceInternal.Style);
                    }
                }

            }
            else if ((markupExtension = value as MarkupExtension) == null)
            {
                freezable = value as Freezable;
            }

            bool requiresInstanceStorage = false;

            // MarkupExtensions use per-instance storage for the "provided" value
            if (markupExtension != null)
            {
                value = markupExtension;
                requiresInstanceStorage = true;
            }

            // Freezables should be frozen, if possible.  Otherwise, they use
            // per-instance storage for a clone.
            else if (freezable != null)
            {
                value = freezable;

                if (!freezable.CanFreeze)
                {
                    requiresInstanceStorage = true;
                }
                else
                {
                    Debug.Assert(freezable.IsFrozen, "Freezable within a Style or Template should have been frozen by now");
                }
            }

            return requiresInstanceStorage;
        }

        //
        //  All table datastructures read-lock-free/write-lock
        //  AddContainerDependent writes the datastructures, locks set by callers
        //
        //  This method
        //  1. Adds a ContainerDependent to the ContainerDependents list if not
        //     already present. This is used to invalidate container dependents.
        //
        internal static void AddContainerDependent(
            DependencyProperty                          dp,
            bool                                        fromVisualTrigger,
            ref FrugalStructList<ContainerDependent>    containerDependents)
        {
            ContainerDependent dependent;

            for (int i = 0; i < containerDependents.Count; i++)
            {
                dependent = containerDependents[i];

                if (dp == dependent.Property)
                {
                    // If the dp is set on targetType tag and can be set via TriggerBase it is recorded as coming
                    // from TriggerBase because that way we are pessimistic in invalidating and always invalidate.
                    dependent.FromVisualTrigger |= fromVisualTrigger;
                    return;
                }
            }

            dependent = new ContainerDependent();
            dependent.Property = dp;
            dependent.FromVisualTrigger = fromVisualTrigger;
            containerDependents.Add(dependent);
        }

        //
        //  All table datastructures read-lock-free/write-lock
        //  AddEventDependent writes the datastructures, locks set by callers
        //
        //  This method
        //  1. Adds an EventDependent to the EventDependents list. This is used
        //     to lookup events in styles during event routing.
        //
        internal static void AddEventDependent(
            int                                     childIndex,
            EventHandlersStore                      eventHandlersStore,
            ref ItemStructList<ChildEventDependent> eventDependents)
        {
            if (eventHandlersStore != null)
            {
                Debug.Assert(childIndex >= 0);

                ChildEventDependent dependent = new ChildEventDependent();
                dependent.ChildIndex = childIndex;
                dependent.EventHandlersStore = eventHandlersStore;

                eventDependents.Add(ref dependent);
            }
        }

        //
        //  This method
        //  1. Adds a ChildPropertyDependent entry to the given
        //     PropertyDependents list. This is used when invalidating
        //     properties dependent upon a certain property on the container.
        //     The dependent properties could have originated from a Trigger
        //     or from a property alias on a TemplateNode.
        //
        private static void AddPropertyDependent(
            int                                             childIndex,
            DependencyProperty                              dp,
            ref FrugalStructList<ChildPropertyDependent>    propertyDependents)
        {
            ChildPropertyDependent dependent = new ChildPropertyDependent();
            dependent.ChildIndex = childIndex;
            dependent.Property = dp;

            propertyDependents.Add(dependent);
        }

        //
        //  This method
        //  1. Adds a ChildPropertyDependent entry to the given
        //     ResourceDependents list. This is used when invalidating
        //     resource references
        //
        private static void AddResourceDependent(
            int                                             childIndex,
            DependencyProperty                              dp,
            object                                          name,
            ref FrugalStructList<ChildPropertyDependent>    resourceDependents)
        {
            bool add = true;

            for (int i = 0; i < resourceDependents.Count; i++)
            {
                // Check for duplicate entry
                ChildPropertyDependent resourceDependent = resourceDependents[i];
                if ((resourceDependent.ChildIndex == childIndex) &&
                    (resourceDependent.Property == dp) &&
                    (resourceDependent.Name == name))
                {
                    add = false;
                    break;
                }
            }

            if (add)
            {
                // Since there isn't a duplicate entry,
                // create and add a new one
                ChildPropertyDependent resourceDependent = new ChildPropertyDependent();
                resourceDependent.ChildIndex = childIndex;
                resourceDependent.Property = dp;
                resourceDependent.Name = name;

                resourceDependents.Add(resourceDependent);
            }
        }

        //+----------------------------------------------------------------------------------------------
        //
        //  ProcessTemplateContentFromFEF
        //
        //  This method walks the FEF tree and builds the shared tables from the property values
        //  in the FEF.
        //
        //  For the Baml templates (non-FEF), see the ProcessTemplateContent routine.
        //
        //+----------------------------------------------------------------------------------------------

        internal static void ProcessTemplateContentFromFEF(
            FrameworkElementFactory                                     factory,
            ref FrugalStructList<ChildRecord>                           childRecordFromChildIndex,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>>    triggerSourceRecordFromChildIndex,
            ref FrugalStructList<ChildPropertyDependent>                resourceDependents,
            ref ItemStructList<ChildEventDependent>                     eventDependents,
            ref HybridDictionary                                        dataTriggerRecordFromBinding,
            HybridDictionary                                            childIndexFromChildID,
            ref bool                                                    hasInstanceValues)
        {
            // Process the PropertyValues on the current node
            for (int i = 0; i < factory.PropertyValues.Count; i++)
            {
                PropertyValue propertyValue = factory.PropertyValues[i];
                StyleHelper.UpdateTables(ref propertyValue, ref childRecordFromChildIndex,
                    ref triggerSourceRecordFromChildIndex, ref resourceDependents, ref dataTriggerRecordFromBinding,
                    childIndexFromChildID, ref hasInstanceValues);
            }

            // Add an entry in the EventDependents list for
            // the current TemplateNode's EventHandlersStore.
            StyleHelper.AddEventDependent(factory._childIndex, factory.EventHandlersStore,
                ref eventDependents);

            // Traverse the children of this TemplateNode
            factory = factory.FirstChild;
            while (factory != null)
            {
                ProcessTemplateContentFromFEF(factory, ref childRecordFromChildIndex, ref triggerSourceRecordFromChildIndex, ref resourceDependents,
                    ref eventDependents, ref dataTriggerRecordFromBinding, childIndexFromChildID, ref hasInstanceValues);

                factory = factory.NextSibling;
            }
        }

        //
        //  This method
        //  1. Adds shared table entries for property values set via Triggers
        //
        private static void ProcessTemplateTriggers(
            TriggerCollection                                           triggers,
            FrameworkTemplate                                           frameworkTemplate,
            ref FrugalStructList<ChildRecord>                           childRecordFromChildIndex,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>>    triggerSourceRecordFromChildIndex,
            ref FrugalStructList<ContainerDependent>                    containerDependents,
            ref FrugalStructList<ChildPropertyDependent>                resourceDependents,
            ref ItemStructList<ChildEventDependent>                     eventDependents,
            ref HybridDictionary                                        dataTriggerRecordFromBinding,
            HybridDictionary                                            childIndexFromChildID,
            ref bool                                                    hasInstanceValues,
            ref HybridDictionary                                        triggerActions,
            FrameworkElementFactory                                     templateRoot,
            ref EventHandlersStore                                      eventHandlersStore,
            ref FrugalMap                                               propertyTriggersWithActions,
            ref HybridDictionary                                        dataTriggersWithActions,
            ref bool                                                    hasLoadedChangeHandler)
        {
            if (triggers != null)
            {
                int triggerCount = triggers.Count;
                for (int i = 0; i < triggerCount; i++)
                {
                    TriggerBase triggerBase = triggers[i];

                    Trigger trigger;
                    MultiTrigger multiTrigger;
                    DataTrigger dataTrigger;
                    MultiDataTrigger multiDataTrigger;
                    EventTrigger eventTrigger;

                    DetermineTriggerType( triggerBase, out trigger, out multiTrigger, out dataTrigger, out multiDataTrigger, out eventTrigger );

                    if ( trigger != null || multiTrigger != null||
                        dataTrigger != null || multiDataTrigger != null )
                    {
                        // Update the SourceChildIndex for each of the conditions for this trigger
                        TriggerCondition[] conditions = triggerBase.TriggerConditions;
                        for (int k=0; k<conditions.Length; k++)
                        {
                            conditions[k].SourceChildIndex = StyleHelper.QueryChildIndexFromChildName(conditions[k].SourceName, childIndexFromChildID);
                        }

                        // Set things up to handle Setter values
                        for (int j = 0; j < triggerBase.PropertyValues.Count; j++)
                        {
                            PropertyValue propertyValue = triggerBase.PropertyValues[j];

                            // Check for trigger rules that act on template children
                            if (propertyValue.ChildName == StyleHelper.SelfName)
                            {
                                // "Self" (container) trigger

                                // Track properties on the container that are being driven by
                                // the Template so that they can be invalidated during Template changes
                                StyleHelper.AddContainerDependent(propertyValue.Property, true /*fromVisualTrigger*/, ref containerDependents);
                            }

                            StyleHelper.UpdateTables(ref propertyValue, ref childRecordFromChildIndex,
                                ref triggerSourceRecordFromChildIndex, ref resourceDependents, ref dataTriggerRecordFromBinding,
                                childIndexFromChildID, ref hasInstanceValues);
                        }

                        // Set things up to handle TriggerActions
                        if( triggerBase.HasEnterActions || triggerBase.HasExitActions )
                        {
                            if( trigger != null )
                            {
                                StyleHelper.AddPropertyTriggerWithAction( triggerBase, trigger.Property, ref propertyTriggersWithActions );
                            }
                            else if( multiTrigger != null )
                            {
                                for( int k = 0; k < multiTrigger.Conditions.Count; k++ )
                                {
                                    Condition triggerCondition = multiTrigger.Conditions[k];

                                    StyleHelper.AddPropertyTriggerWithAction( triggerBase, triggerCondition.Property, ref propertyTriggersWithActions );
                                }
                            }
                            else if( dataTrigger != null )
                            {
                                StyleHelper.AddDataTriggerWithAction( triggerBase, dataTrigger.Binding, ref dataTriggersWithActions );
                            }
                            else if( multiDataTrigger != null )
                            {
                                for( int k = 0; k < multiDataTrigger.Conditions.Count; k++ )
                                {
                                    Condition dataCondition = multiDataTrigger.Conditions[k];

                                    StyleHelper.AddDataTriggerWithAction( triggerBase, dataCondition.Binding, ref dataTriggersWithActions );
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException(SR.Get(SRID.UnsupportedTriggerInTemplate, triggerBase.GetType().Name));
                            }
                        }
                    }
                    else if( eventTrigger != null )
                    {
                        StyleHelper.ProcessEventTrigger(eventTrigger,
                                                        childIndexFromChildID,
                                                        ref triggerActions,
                                                        ref eventDependents,
                                                        templateRoot,
                                                        frameworkTemplate,
                                                        ref eventHandlersStore,
                                                        ref hasLoadedChangeHandler);
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Get(SRID.UnsupportedTriggerInTemplate, triggerBase.GetType().Name));
                    }
                }
            }
        }

        // This block of code attempts to minimize the number of
        //  type operations and assignments involved in figuring out what
        //  trigger type we're dealing here.
        // Attempted casts are done in the order of decreasing expected frequency.
        //  rearrange as expectations change.
        private static void DetermineTriggerType( TriggerBase triggerBase,
            out Trigger trigger, out MultiTrigger multiTrigger,
            out DataTrigger dataTrigger, out MultiDataTrigger multiDataTrigger,
            out EventTrigger eventTrigger )
        {
            if( (trigger = (triggerBase as Trigger)) != null )
            {
                multiTrigger = null;
                dataTrigger = null;
                multiDataTrigger = null;
                eventTrigger = null;
            }
            else if( (multiTrigger = (triggerBase as MultiTrigger)) != null )
            {
                dataTrigger = null;
                multiDataTrigger = null;
                eventTrigger = null;
            }
            else if( (dataTrigger = (triggerBase as DataTrigger)) != null )
            {
                multiDataTrigger = null;
                eventTrigger = null;
            }
            else if( (multiDataTrigger = (triggerBase as MultiDataTrigger)) != null )
            {
                eventTrigger = null;
            }
            else if( (eventTrigger = (triggerBase as EventTrigger)) != null )
            {
                ; // Do nothing - eventTrigger is now non-null, and everything else has been set to null.
            }
            else
            {
                // None of the above - the caller is expected to throw an exception
                //  stating that the trigger type is not supported.
            }
        }

        //
        //  This method
        //  1. Adds the trigger information to the data structure that will be
        //     used when it's time to add the delegate to the event route.
        //
        internal static void ProcessEventTrigger (
            EventTrigger                            eventTrigger,
            HybridDictionary                        childIndexFromChildName,
            ref HybridDictionary                    triggerActions,
            ref ItemStructList<ChildEventDependent> eventDependents,
            FrameworkElementFactory                 templateRoot,
            FrameworkTemplate                       frameworkTemplate,
            ref EventHandlersStore                  eventHandlersStore,
            ref bool                                hasLoadedChangeHandler)
        {
            if( eventTrigger == null )
            {
                return;
            }

            // The list of actions associated with the event of this EventTrigger.
            List<TriggerAction> actionsList = null;
            bool                actionsListExisted = true;
            bool                actionsListChanged = false;
            TriggerAction       action = null;

            FrameworkElementFactory childFef = null;

            // Find a ChildID for the EventTrigger.
            if( eventTrigger.SourceName == null )
            {
                eventTrigger.TriggerChildIndex = 0;
            }
            else
            {
                int childIndex = StyleHelper.QueryChildIndexFromChildName(eventTrigger.SourceName, childIndexFromChildName);

                if( childIndex == -1 )
                {
                    throw new InvalidOperationException(SR.Get(SRID.EventTriggerTargetNameUnresolvable, eventTrigger.SourceName));
                }

                eventTrigger.TriggerChildIndex = childIndex;
            }

            // We have at least one EventTrigger - will need triggerActions
            // if it doesn't already exist
            if (triggerActions == null)
            {
                triggerActions = new HybridDictionary();
            }
            else
            {
                actionsList = triggerActions[eventTrigger.RoutedEvent] as List<TriggerAction>;
            }

            // Set up TriggerAction list if one doesn't already exist
            if (actionsList == null)
            {
                actionsListExisted = false;
                actionsList = new List<TriggerAction>();
            }

            for (int i = 0; i < eventTrigger.Actions.Count; i++)
            {
                action = eventTrigger.Actions[i];

                // Any reason we shouldn't use this TriggerAction?  Check here.
                if( false /* No reason not to use it right now */ )
                {
                    // continue;
                }

                // Looks good, add to list.
                Debug.Assert(action.IsSealed, "TriggerAction should have already been sealed by this point.");
                actionsList.Add(action);
                actionsListChanged = true;
            }

            if (actionsListChanged && !actionsListExisted)
            {
                triggerActions[eventTrigger.RoutedEvent] = actionsList;
            }


            // Add a special delegate to listen for this event and
            // fire the trigger.

            if( templateRoot != null || eventTrigger.TriggerChildIndex == 0 )
            {
                // This is a FEF-style template, or the trigger is keying off of
                // the templated parent.

                // Get the FEF that is referenced by this trigger.

                if (eventTrigger.TriggerChildIndex != 0)
                {
                    childFef = StyleHelper.FindFEF(templateRoot, eventTrigger.TriggerChildIndex);
                }

                // If this trigger needs the loaded/unloaded events, set the optimization
                // flag.

                if (  (eventTrigger.RoutedEvent == FrameworkElement.LoadedEvent)
                    ||(eventTrigger.RoutedEvent == FrameworkElement.UnloadedEvent))
                {
                    if (eventTrigger.TriggerChildIndex == 0)
                    {
                        // Mark the template to show it has a loaded or unloaded handler

                        hasLoadedChangeHandler  = true;
                    }
                    else
                    {
                        // Mark the FEF to show it has a loaded or unloaded handler

                        childFef.HasLoadedChangeHandler  = true;
                    }
                }


                // Add a delegate that'll come back and fire these actions.
                //  This information will be used by FrameworkElement.AddStyleHandlersToEventRoute

                StyleHelper.AddDelegateToFireTrigger(eventTrigger.RoutedEvent,
                                                     eventTrigger.TriggerChildIndex,
                                                     templateRoot,
                                                     childFef,
                                                     ref eventDependents,
                                                     ref eventHandlersStore);
            }
            else
            {
                // This is a baml-style template.

                // If this trigger needs the loaded/unloaded events, set the optimization
                // flag.

                if (eventTrigger.RoutedEvent == FrameworkElement.LoadedEvent
                    ||
                    eventTrigger.RoutedEvent == FrameworkElement.UnloadedEvent )
                {
                    FrameworkTemplate.TemplateChildLoadedFlags templateChildLoadedFlags
                        = frameworkTemplate._TemplateChildLoadedDictionary[ eventTrigger.TriggerChildIndex ] as FrameworkTemplate.TemplateChildLoadedFlags;

                    if( templateChildLoadedFlags == null )
                    {
                        templateChildLoadedFlags = new FrameworkTemplate.TemplateChildLoadedFlags();
                        frameworkTemplate._TemplateChildLoadedDictionary[ eventTrigger.TriggerChildIndex ] = templateChildLoadedFlags;
                    }

                    if( eventTrigger.RoutedEvent == FrameworkElement.LoadedEvent )
                    {
                        templateChildLoadedFlags.HasLoadedChangedHandler = true;
                    }
                    else
                    {
                        templateChildLoadedFlags.HasUnloadedChangedHandler = true;
                    }
                }


                // Add a delegate that'll come back and fire these actions.

                StyleHelper.AddDelegateToFireTrigger(eventTrigger.RoutedEvent,
                                                     eventTrigger.TriggerChildIndex,
                                                     ref eventDependents,
                                                     ref eventHandlersStore);
            }
        }

        //
        //  This method
        //  1. Adds a delegate that will get called during event routing and
        //     will allow us to invoke the TriggerActions
        //
        private static void AddDelegateToFireTrigger(
            RoutedEvent                             routedEvent,
            int                                     childIndex,
            FrameworkElementFactory                 templateRoot,
            FrameworkElementFactory                 childFef,
            ref ItemStructList<ChildEventDependent> eventDependents,
            ref EventHandlersStore                  eventHandlersStore)
        {
            if (childIndex == 0)
            {
                if (eventHandlersStore == null)
                {
                    eventHandlersStore = new EventHandlersStore();

                    // Add an entry in the EventDependents list for
                    // the TargetType's EventHandlersStore. Notice
                    // that the childIndex is 0.
                    StyleHelper.AddEventDependent(0, eventHandlersStore, ref eventDependents);
                }
                eventHandlersStore.AddRoutedEventHandler(routedEvent, StyleHelper.EventTriggerHandlerOnContainer, false/* HandledEventsToo */);
            }
            else
            {
                //FrameworkElementFactory fef = StyleHelper.FindFEF(templateRoot, childIndex);
                if (childFef.EventHandlersStore == null)
                {
                    childFef.EventHandlersStore = new EventHandlersStore();

                    // Add an entry in the EventDependents list for
                    // the current FEF's EventHandlersStore.
                    StyleHelper.AddEventDependent(childIndex, childFef.EventHandlersStore, ref eventDependents);
                }
                childFef.EventHandlersStore.AddRoutedEventHandler(routedEvent, StyleHelper.EventTriggerHandlerOnChild, false/* HandledEventsToo */);
            }
        }


        //+---------------------------------------------------------------------------------------
        //
        //  AddDelegateToFireTrigger
        //
        //  Add the EventTriggerHandlerOnChild to listen for an event, like the above overload
        //  except this is for baml-style templates, rather than FEF-style.
        //
        //+---------------------------------------------------------------------------------------

        private static void AddDelegateToFireTrigger(
            RoutedEvent                             routedEvent,
            int                                     childIndex,
            ref ItemStructList<ChildEventDependent> eventDependents,
            ref EventHandlersStore                  eventHandlersStore)
        {
            Debug.Assert( childIndex != 0 ); // This should go to the other AddDelegateToFireTrigger overload


            if (eventHandlersStore == null)
            {
                eventHandlersStore = new EventHandlersStore();
            }

            StyleHelper.AddEventDependent( childIndex,
                                           eventHandlersStore,
                                           ref eventDependents );
            eventHandlersStore.AddRoutedEventHandler(routedEvent, StyleHelper.EventTriggerHandlerOnChild, false/* HandledEventsToo */);

        }

        //
        //  This method
        //  1. If the value is an ISealable and it is not sealed
        //     and can be sealed, seal it now.
        //  2. Else it returns the value as is.
        //
        internal static void SealIfSealable(object value)
        {
            // If the value is an ISealable and it is not sealed
            // and can be sealed, seal it now.
            ISealable sealable = value as ISealable;
            if (sealable != null && !sealable.IsSealed && sealable.CanSeal)
            {
                sealable.Seal();
            }
        }

        #endregion WriteMethods

        //  ===========================================================================
        //  These methods are invoked when a visual tree is
        //  being created/destroyed via a Style/Template and
        //  when a Style/Template is being applied or
        //  unapplied to a FE/FCE
        //  ===========================================================================

        #region WriteInstanceData

        //
        //  This method
        //  1. Is called whenever a Style/Template is [un]applied to an FE/FCE
        //  2. It updates the per-instance StyleData/TemplateData
        //
        internal static void UpdateInstanceData(
            UncommonField<HybridDictionary[]> dataField,
            FrameworkElement           fe,
            FrameworkContentElement    fce,
            Style                      oldStyle,
            Style                      newStyle,
            FrameworkTemplate          oldFrameworkTemplate,
            FrameworkTemplate          newFrameworkTemplate,
            InternalFlags              hasGeneratedSubTreeFlag)
        {
            Debug.Assert((fe != null && fce == null) || (fe == null && fce != null));

            DependencyObject container = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

            if (oldStyle != null || oldFrameworkTemplate != null )
            {
                ReleaseInstanceData(dataField, container, fe, fce, oldStyle, oldFrameworkTemplate, hasGeneratedSubTreeFlag);
            }

            if (newStyle != null || newFrameworkTemplate != null )
            {
                CreateInstanceData(dataField, container, fe, fce, newStyle, newFrameworkTemplate );
            }
            else
            {
                dataField.ClearValue(container);
            }
        }

        //
        //  This method
        //  1. Is called whenever a new Style/Template is applied to an FE/FCE
        //  2. It adds per-instance StyleData/TemplateData for the new Style/Template
        //
        internal static void CreateInstanceData(
            UncommonField<HybridDictionary[]> dataField,
            DependencyObject            container,
            FrameworkElement            fe,
            FrameworkContentElement     fce,
            Style                       newStyle,
            FrameworkTemplate           newFrameworkTemplate )
        {
            Debug.Assert((fe != null && fce == null) || (fe == null && fce != null));
            Debug.Assert((fe != null && fe == container) || (fce != null && fce == container));
            Debug.Assert(newStyle != null || newFrameworkTemplate != null );

            if (newStyle != null)
            {
                if (newStyle.HasInstanceValues)
                {
                    HybridDictionary instanceValues = EnsureInstanceData(dataField, container, InstanceStyleData.InstanceValues);
                    StyleHelper.ProcessInstanceValuesForChild(
                        container, container, 0, instanceValues, true,
                        ref newStyle.ChildRecordFromChildIndex);
                }
            }
            else if (newFrameworkTemplate != null)
            {
                if (newFrameworkTemplate.HasInstanceValues)
                {
                    HybridDictionary instanceValues = EnsureInstanceData(dataField, container, InstanceStyleData.InstanceValues);
                    StyleHelper.ProcessInstanceValuesForChild(
                        container, container, 0, instanceValues, true,
                        ref newFrameworkTemplate.ChildRecordFromChildIndex);
                }
            }
        }

        //
        //  This method
        //  1. Adds the new TemplateNode's information to the container's per-instance
        //     StyleData/TemplateData. (This only makes sense for children created via
        //     FrameworkElementFactory. Children acquired via BuildVisualTree don't use
        //     any property-related funtionality of the Style/Template.)
        //
        internal static void CreateInstanceDataForChild(
            UncommonField<HybridDictionary[]>   dataField,
            DependencyObject                    container,
            DependencyObject                    child,
            int                                 childIndex,
            bool                                hasInstanceValues,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex)
        {
            if (hasInstanceValues)
            {
                HybridDictionary instanceValues = EnsureInstanceData(dataField, container, InstanceStyleData.InstanceValues);
                StyleHelper.ProcessInstanceValuesForChild(
                    container, child, childIndex, instanceValues, true,
                    ref childRecordFromChildIndex);
            }
        }

        //
        //  This method
        //  1. Is called whenever a new Style/Template is upapplied from an FE/FCE
        //  2. It removes per-instance StyleData/TemplateData for the old Style/Template
        //
        internal static void ReleaseInstanceData(
            UncommonField<HybridDictionary[]>  dataField,
            DependencyObject            container,
            FrameworkElement            fe,
            FrameworkContentElement     fce,
            Style                       oldStyle,
            FrameworkTemplate           oldFrameworkTemplate,
            InternalFlags               hasGeneratedSubTreeFlag)
        {
            Debug.Assert((fe != null && fce == null) || (fe == null && fce != null));
            Debug.Assert((fe != null && fe == container) || (fce != null && fce == container));
            Debug.Assert(oldStyle != null || oldFrameworkTemplate != null );

            // Fetch the per-instance data field value
            HybridDictionary[] styleData = dataField.GetValue(container);

            if (oldStyle != null)
            {
                HybridDictionary instanceValues = (styleData != null) ? styleData[(int)InstanceStyleData.InstanceValues] : null;
                ReleaseInstanceDataForDataTriggers(dataField, instanceValues, oldStyle, oldFrameworkTemplate );
                if (oldStyle.HasInstanceValues)
                {
                    StyleHelper.ProcessInstanceValuesForChild(
                        container, container, 0, instanceValues, false,
                        ref oldStyle.ChildRecordFromChildIndex);
                }
            }
            else if (oldFrameworkTemplate != null)
            {
                HybridDictionary instanceValues = (styleData != null) ? styleData[(int)InstanceStyleData.InstanceValues] : null;
                ReleaseInstanceDataForDataTriggers(dataField, instanceValues, oldStyle, oldFrameworkTemplate );
                if (oldFrameworkTemplate.HasInstanceValues)
                {
                    StyleHelper.ProcessInstanceValuesForChild(
                        container, container, 0, instanceValues, false,
                        ref oldFrameworkTemplate.ChildRecordFromChildIndex);
                }
            }
            else
            {
                HybridDictionary instanceValues = (styleData != null) ? styleData[(int)InstanceStyleData.InstanceValues] : null;
                ReleaseInstanceDataForDataTriggers(dataField, instanceValues, oldStyle, oldFrameworkTemplate );
            }
        }

        //
        //  This method
        //  1. Ensures that the desired per-instance storage
        //     for Style/Template exists
        //
        internal static HybridDictionary EnsureInstanceData(
            UncommonField<HybridDictionary[]>  dataField,
            DependencyObject            container,
            InstanceStyleData           dataType)
        {
            return EnsureInstanceData(dataField, container, dataType, -1);
        }

        //
        //  This method
        //  1. Ensures that the desired per-instance storage
        //     for Style/Template exists
        //  2. Also allows you to specify initial capacity
        //
        internal static HybridDictionary EnsureInstanceData(
            UncommonField<HybridDictionary[]>  dataField,
            DependencyObject            container,
            InstanceStyleData           dataType,
            int                         initialSize)
        {
            Debug.Assert((container is FrameworkElement) || (container is FrameworkContentElement), "Caller has queried with non-framework element.  Bad caller, bad!");
            Debug.Assert(dataType < InstanceStyleData.ArraySize, "Caller has queried using a value outside the range of the Enum.  Bad caller, bad!");

            HybridDictionary[] data = dataField.GetValue(container);

            if (data == null)
            {
                data = new HybridDictionary[(int)InstanceStyleData.ArraySize];
                dataField.SetValue(container, data);
            }

            if (data[(int)dataType] == null )
            {
                if( initialSize < 0 )
                {
                    data[(int)dataType] = new HybridDictionary();
                }
                else
                {
                    data[(int)dataType] = new HybridDictionary(initialSize);
                }
            }

            return (HybridDictionary)data[(int)dataType];
        }

        //
        //  This method
        //  1. Adds or removes per-instance state on the container/child (push model)
        //  2. Processes values that need per-instance storage
        //
        private static void ProcessInstanceValuesForChild(
            DependencyObject                    container,
            DependencyObject                    child,
            int                                 childIndex,
            HybridDictionary                    instanceValues,
            bool                                apply,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex)
        {
            // If childIndex has not been provided,
            // fetch it from the given child node
            if (childIndex == -1)
            {
                FrameworkElement feChild;
                FrameworkContentElement fceChild;
                Helper.DowncastToFEorFCE(child, out feChild, out fceChild, false);

                childIndex = (feChild != null) ? feChild.TemplateChildIndex
                            : (fceChild != null) ? fceChild.TemplateChildIndex : -1;
            }

            // Check if this Child Index/Property is represented in style
            if ((0 <= childIndex) && (childIndex < childRecordFromChildIndex.Count))
            {
                int n = childRecordFromChildIndex[childIndex].ValueLookupListFromProperty.Count;
                for (int i = 0; i < n; ++i)
                {
                    ProcessInstanceValuesHelper(
                        ref childRecordFromChildIndex[childIndex].ValueLookupListFromProperty.Entries[i].Value,
                        child, childIndex, instanceValues, apply);
                }
            }
        }

        //
        //  This method
        //  1. Adds or removes per-instance state on the container/child (push model)
        //  2. Processes values that need per-instance storage
        //
        private static void ProcessInstanceValuesHelper(
            ref ItemStructList<ChildValueLookup> valueLookupList,
            DependencyObject                     target,
            int                                  childIndex,
            HybridDictionary                     instanceValues,
            bool                                 apply)
        {
            // update all properties whose value needs per-instance storage
            for (int i = valueLookupList.Count - 1;  i >= 0;  --i)
            {
                switch (valueLookupList.List[i].LookupType)
                {
                case ValueLookupType.Simple:
                case ValueLookupType.Trigger:
                case ValueLookupType.DataTrigger:
                    Freezable freezable;

                    DependencyProperty dp = valueLookupList.List[i].Property;
                    object o = valueLookupList.List[i].Value;

                    if (o is MarkupExtension)
                    {
                        ProcessInstanceValue(target, childIndex, instanceValues, dp, i, apply);
                    }
                    else if ((freezable = o as Freezable) != null)
                    {
                        if (freezable.CheckAccess())
                        {
                            if (!freezable.IsFrozen)
                            {
                                ProcessInstanceValue(target, childIndex, instanceValues, dp, i, apply);
                            }
                        }
                        else
                        {
                            Debug.Assert(!freezable.CanFreeze, "If a freezable could have been frozen it would have been done by now.");
                            throw new InvalidOperationException(SR.Get(SRID.CrossThreadAccessOfUnshareableFreezable, freezable.GetType().FullName));
                        }
                    }

                    break;
                }
            }
        }

        //
        //  This method
        //  1. Adds or removes per-instance state on the container/child (push model)
        //  2. Processes a single value that needs per-instance storage
        //
        internal static void ProcessInstanceValue(
            DependencyObject    target,
            int                 childIndex,
            HybridDictionary    instanceValues,
            DependencyProperty  dp,
            int                 i,
            bool                apply)
        {
            // If we get this far, it's because there's a value
            // in the property value list of an active style that requires
            // per-instance storage.  The initialization (CreateInstaceData)
            // should have created the InstanceValues hashtable by now.
            Debug.Assert(instanceValues != null, "InstanceValues hashtable should have been created at initialization time.");

            InstanceValueKey key = new InstanceValueKey(childIndex, dp.GlobalIndex, i);

            if (apply)
            {
                // Store a sentinel value in per-instance StyleData.
                // The actual value is created only on demand.
                instanceValues[key] = NotYetApplied;
            }
            else
            {
                // Remove the instance value from the table
                object value = instanceValues[key];
                instanceValues.Remove(key);

                Expression expr;
                Freezable freezable;

                if ((expr = value as Expression)!= null)
                {
                    // if the instance value is an expression, detach it
                    expr.OnDetach(target, dp);
                }
                else if ((freezable = value as Freezable)!= null)
                {
                    // if the instance value is a Freezable, remove its
                    // inheritance context
                    target.RemoveSelfAsInheritanceContext(freezable, dp);
                }
            }
        }

        //
        //  This method
        //  1. Is called when a style/template is removed from a container.
        //  2. It is meant to release its data trigger information.
        //
        private static void ReleaseInstanceDataForDataTriggers(
            UncommonField<HybridDictionary[]> dataField,
            HybridDictionary            instanceValues,
            Style                       oldStyle,
            FrameworkTemplate           oldFrameworkTemplate)
        {
            Debug.Assert(oldStyle != null || oldFrameworkTemplate != null );

            if (instanceValues == null)
                return;

            // the event handler depends only on whether the instance data
            // applies to Style, Template, or ThemeStyle
            EventHandler<BindingValueChangedEventArgs> handler;
            if (dataField == StyleDataField)
            {
                handler = new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInStyleChanged);
            }
            else if (dataField == TemplateDataField)
            {
                handler = new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInTemplateChanged);
            }
            else
            {
                Debug.Assert(dataField == ThemeStyleDataField);
                handler = new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInThemeStyleChanged);
            }

            // clean up triggers with setters
            HybridDictionary dataTriggerRecordFromBinding = null;
            if (oldStyle != null)
            {
                dataTriggerRecordFromBinding = oldStyle._dataTriggerRecordFromBinding;
            }
            else if (oldFrameworkTemplate != null)
            {
                dataTriggerRecordFromBinding = oldFrameworkTemplate._dataTriggerRecordFromBinding;
            }

            if (dataTriggerRecordFromBinding != null)
            {
                foreach (object o in dataTriggerRecordFromBinding.Keys)
                {
                    BindingBase binding = (BindingBase)o;
                    ReleaseInstanceDataForTriggerBinding(binding, instanceValues, handler);
                }
            }

            // clean up triggers with actions
            HybridDictionary dataTriggersWithActions = null;
            if (oldStyle != null)
            {
                dataTriggersWithActions = oldStyle.DataTriggersWithActions;
            }
            else if (oldFrameworkTemplate != null)
            {
                dataTriggersWithActions = oldFrameworkTemplate.DataTriggersWithActions;
            }

            if (dataTriggersWithActions != null)
            {
                foreach (object o in dataTriggersWithActions.Keys)
                {
                    BindingBase binding = (BindingBase)o;
                    ReleaseInstanceDataForTriggerBinding(binding, instanceValues, handler);
                }
            }
        }

        private static void ReleaseInstanceDataForTriggerBinding(
            BindingBase                                 binding,
            HybridDictionary                            instanceValues,
            EventHandler<BindingValueChangedEventArgs>  handler)
        {
            BindingExpressionBase bindingExpr = (BindingExpressionBase)instanceValues[binding];

            if (bindingExpr != null)
            {
                bindingExpr.ValueChanged -= handler;
                bindingExpr.Detach();
                instanceValues.Remove(binding);
            }
        }

        #endregion WriteInstanceData


        //+----------------------------------------------------------------------------------
        //
        //  ApplyTemplateContent
        //
        //  Instantiate the content of the template (either from FEFs or from Baml).
        //  This is done for every element to which this template is attached.
        //
        //+----------------------------------------------------------------------------------
        #region InstantiateSubTree

        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal static bool ApplyTemplateContent(
            UncommonField<HybridDictionary[]>  dataField,
            DependencyObject            container,
            FrameworkElementFactory     templateRoot,
            int                         lastChildIndex,
            HybridDictionary            childIndexFromChildID,
            FrameworkTemplate           frameworkTemplate)
        {
            Debug.Assert(frameworkTemplate != null );

            bool visualsCreated = false;

            FrameworkElement feContainer = container as FrameworkElement;

            // Is this a FEF-style template?

            if (templateRoot != null)
            {
                // Yes, we'll instantiate from a FEF tree.

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseInstVisTreeBegin);

                CheckForCircularReferencesInTemplateTree(container, frameworkTemplate );

                // Container is considered ChildIndex '0' (Self), but,
                // Container.ChildIndex isn't set
                List<DependencyObject> affectedChildren = new List<DependencyObject>(lastChildIndex);

                // Assign affectedChildren to container
                TemplatedFeChildrenField.SetValue(container, affectedChildren);

                // When building the template children chain, we keep a chain of
                // nodes that don't need to be in the chain for property invalidation
                // or lookup purposes.  (And hence not assigned a TemplateChildIndex)
                // We only need them in order to clean up their _templatedParent
                // references (see FrameworkElement.ClearTemplateChain)
                List<DependencyObject> noChildIndexChildren = null;

                // Instantiate template
                // Setup container's reference to first child in chain
                // and add to tree
                DependencyObject treeRoot = templateRoot.InstantiateTree(
                    dataField,
                    container,
                    container,
                    affectedChildren,
                    ref noChildIndexChildren,
                    ref frameworkTemplate.ResourceDependents);

                Debug.Assert(treeRoot is FrameworkElement || treeRoot is FrameworkContentElement,
                    "Root node of tree must be a FrameworkElement or FrameworkContentElement.  This should have been caught by set_VisualTree" );

                // From childFirst to childLast is the chain of child nodes with
                //  childIndex.  Append that chain with the chain of child nodes
                //  with no childIndex assigned.
                if( noChildIndexChildren != null )
                {
                    affectedChildren.AddRange(noChildIndexChildren);
                }

                visualsCreated = true;

                if (feContainer != null && EventTrace.IsEnabled(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose))
                {
                    string label = feContainer.Name;
                    if (label == null || label.Length == 0)
                        label = container.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientParseInstVisTreeEnd, EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose,
                                                         String.Format(System.Globalization.CultureInfo.InvariantCulture, "Style.InstantiateSubTree for {0} {1}", container.GetType().Name, label));
                }
            }

            // It's not a FEF-style template, is it a non-empty optimized one?

            else if (frameworkTemplate != null && frameworkTemplate.HasXamlNodeContent)
            {
                // Yes, create from the optimized template content.
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseInstVisTreeBegin);

                CheckForCircularReferencesInTemplateTree(container, frameworkTemplate );

                // Container is considered ChildIndex '0' (Self), but,
                // Container.ChildIndex isn't set

                List<DependencyObject> affectedChildren = new List<DependencyObject>(lastChildIndex);

                // Assign affectedChildren to container

                TemplatedFeChildrenField.SetValue(container, affectedChildren);

                DependencyObject treeRoot;

                // Load the content

                treeRoot = frameworkTemplate.LoadContent( container, affectedChildren);

                Debug.Assert(treeRoot == null || treeRoot is FrameworkElement || treeRoot is FrameworkContentElement,
                    "Root node of tree must be a FrameworkElement or FrameworkContentElement.  This should have been caught by set_VisualTree" );

                visualsCreated = true;

                if (feContainer != null && EventTrace.IsEnabled(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose))
                {
                    string label = feContainer.Name;
                    if (label == null || label.Length == 0)
                        label = container.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientParseInstVisTreeEnd, EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose,
                                                         String.Format(System.Globalization.CultureInfo.InvariantCulture, "Style.InstantiateSubTree for {0} {1}", container.GetType().Name, label));
                }
            }

            // No template was supplied.  Allow subclasses to provide the template.
            // This is currently only implemented for FrameworkElement's.

            else
            {
                if (feContainer != null)
                {
                    // This template will not be driven by the Style in any way.
                    // Rather, it will be built and initialized by the callee

                    // CALLBACK
#if DEBUG
                    Debug.Assert( feContainer._buildVisualTreeVerification == VerificationState.WaitingForBuildVisualTree,
                        "The BuildVisualTree override has triggered another call to itself.  This is not good - something between the two Style.InstantiateSubTree calls on the stack is doing A Very Bad Thing.");
                    feContainer._buildVisualTreeVerification = VerificationState.WaitingForAddCustomTemplateRoot;
                    bool exceptionThrown = true;
                    try
                    {
#endif

                    Debug.Assert(frameworkTemplate != null, "Only FrameworkTemplate has the ability to build a VisualTree by this means");
                    visualsCreated = frameworkTemplate.BuildVisualTree(feContainer);

#if DEBUG
                    exceptionThrown = false;
                    }
                    finally
                    {
                        if (!exceptionThrown)   // results are unreliable if an exception was thrown
                        {
                            if( visualsCreated )
                            {
                                Debug.Assert( feContainer._buildVisualTreeVerification == VerificationState.WaitingForBuildVisualTreeCompletion,
                                    "A derived class overriding BuildVisualTree must call AddCustomTemplateRoot to attach its custom subtree before exiting.");
                            }
                            else
                            {
                                Debug.Assert( feContainer._buildVisualTreeVerification == VerificationState.WaitingForAddCustomTemplateRoot,
                                    "If a derived class overriding BuildVisualTree has called AddCustomTemplateRoot to attach its custom subtree, its BuildVisualTree must return true to indicate that it has done so.");
                            }
                        }

                        // All done building this visual tree, stand by for the next one.
                        feContainer._buildVisualTreeVerification = VerificationState.WaitingForBuildVisualTree;
                    }
#endif
                }
            }

            return visualsCreated;
        }

        // This logic used to be in Style.InstantiateSubTree, but now we're
        //  requiring derived classes to add the root of the tree they're
        //  creating in BuildVisualTree.  They need to call this as soon as
        //  they've created the root, before building the rest of the tree.
        // Building this tree "in place" means we won't have to do a tree
        //  invalidation after BuildVisualTree completes.
        internal static void AddCustomTemplateRoot( FrameworkElement container, UIElement child )
        {
            AddCustomTemplateRoot( container, child, true, false);
        }

        // The boolean parameter allows us to bypass a possibly expensive call
        //  to VisualTreeHelper.GetParent if we're sure it's unnecessary.
        internal static void AddCustomTemplateRoot(
            FrameworkElement container,
            UIElement child,
            bool checkVisualParent, //  Figure out if this is still worth it.
            bool mustCacheTreeStateOnChild)
        {
#if DEBUG
            Debug.Assert( container._buildVisualTreeVerification != VerificationState.WaitingForBuildVisualTreeCompletion,
                "AddCustomTemplateRoot should only be called once.  This assert may be removed if really necessary but be aware of the performance penalty you're imposing for unnecessary work");
            Debug.Assert( container._buildVisualTreeVerification == VerificationState.WaitingForAddCustomTemplateRoot,
                "This should only be called from the BuildVisualTree override of Style-derived subclasses");
#endif

            // child==null can happen if a ContentPresenter is presenting content
            // that claims to be type-convertible to UIElement, but the type converter
            // actually returns null.  Not a likely situation, but still have to check.
            if (child != null)
            {
                if( checkVisualParent )
                {
                    // Need to disconnect the template root from it's previous parent.
                    FrameworkElement parent = VisualTreeHelper.GetParent(child) as FrameworkElement;
                    if (parent != null)
                    {
                        parent.TemplateChild = null;
                        parent.InvalidateMeasure();
                    }
                }
                else
                {
                    Debug.Assert( null == VisualTreeHelper.GetParent(child),
                        "The caller was positive that 'child' doesn't have a visual parent, bad call.");
                }
            }

            container.TemplateChild = child;
/*
            AncestorChangedTreeState containerCache = AncestorChangedTreeState.From(container);
            FrameworkElement.InvalidateTreeDependentProperties(child as FrameworkElement, null, containerCache);
*/
#if DEBUG
            container._buildVisualTreeVerification = VerificationState.WaitingForBuildVisualTreeCompletion;
#endif
        }

        /// <summary>
        ///     Look for circumstances where one or more Style's template trees
        /// result in a circular reference chain.
        /// </summary>
        /// <remarks>
        ///     We are about to instantiate a template tree for a node.  Check
        /// the node to see if it is, in turn, created from another template tree.
        /// Keep following this chain until:
        ///     (1) We hit a node that wasn't a template-created node.  (GOOD)
        ///     (2) We hit a node that was created by "this" Style and is same
        ///             type as container. (BAD)
        ///
        ///     We care about the chain of template-created nodes because that
        /// is an automated process and once we have a cycle it'll go on forever.
        /// If we hit a non-template-created node (case 1 above) then we can
        /// hope that there's logic to break the chain.
        ///
        ///     We check for same Style *and type* because the Style object may
        /// be applied to a subclass.  In that case it's not necessarily a cycle
        /// just yet, since the template-created object won't be the same as
        /// what the Style is applied on.  If there's actually a cycle later on
        /// we will catch it then.
        ///
        /// Sample markup of things we will catch:
        ///
        ///     <Style>
        ///         <Button />
        ///         <Style.VisualTree>
        ///             <Button />
        ///         </Style.VisualTree>
        ///     </Style>
        ///
        ///     The reason we have to do this from InstantiateSubTree instead of
        /// a self-check in Seal() (Which might catch the above) is that the
        /// cycle may be spread across multiple Style objects.  For example, if
        /// both of the following exist:
        ///
        ///     <Style>
        ///         <Button />
        ///         <Style.VisualTree>
        ///             <TextBox />
        ///         </Style.VisualTree>
        ///     </Style>
        ///
        ///     <Style>
        ///         <TextBox />
        ///         <Style.VisualTree>
        ///             <Button />
        ///         </Style.VisualTree>
        ///     </Style>
        ///
        ///     We won't realize this if we're just looking at the individual
        /// Style objects.
        ///
        /// </remarks>
        private static void CheckForCircularReferencesInTemplateTree(
            DependencyObject    container,
            FrameworkTemplate   frameworkTemplate)
        {
            Debug.Assert(frameworkTemplate != null );

            // Get set up to handle the FE/FCE duality
            DependencyObject walkNode = container;
            DependencyObject nextParent = null;
            FrameworkElement feWalkNode;
            FrameworkContentElement fceWalkNode;
            bool walkNodeIsFE;

            while( walkNode != null )
            {
                // Figure out whether the node is a FE or FCE
                Helper.DowncastToFEorFCE(walkNode, out feWalkNode, out fceWalkNode, false);
                walkNodeIsFE = (feWalkNode != null);
                Debug.Assert( feWalkNode != null || fceWalkNode != null,
                    "Template tree node should be either FE or FCE - did you add support for more types and forgot to update this function?");
                if( walkNodeIsFE )
                {
                    nextParent = feWalkNode.TemplatedParent;
                }
                else
                {
                    nextParent = fceWalkNode.TemplatedParent;
                }

                // If we're beyond "this" container, check for identical Style & type
                //  because that indicates a cycle.  If so, stop the train.
                if( walkNode != container && nextParent != null ) // Only interested in nodes that are "Not me" and not auto-generated (== no TemplatedParent)
                {
                    // Do the cheaper comparison first - the Style reference should be cached
                    if ((frameworkTemplate != null && walkNodeIsFE == true && feWalkNode.TemplateInternal == frameworkTemplate) )
                    {
                        // Then the expensive one - pulling in reflection to check if they're also the same types.
                        if( walkNode.GetType() == container.GetType() )
                        {
                            string name = (walkNodeIsFE) ? feWalkNode.Name : fceWalkNode.Name;

                            // Same Style, Same type, on a chain of Style-created nodes.
                            //  This is bad news since this chain will continue indefinitely.
                            throw new InvalidOperationException(
                                SR.Get(SRID.TemplateCircularReferenceFound, name, walkNode.GetType()));
                        }
                    }
                }

                // If the container is, in turn, created from another Style,
                //  keep walking up that chain.  Exception:  do not walk up from a
                //  ContentPresenter;  this avoids false positives involving a
                //  ContentControl whose effective ContentTemplate contains another
                //  instance of the same type of ContentControl, for example:
                //          <Button Content="A">
                //              <Button.ContentTemplate>
                //                  <DataTemplate>
                //                      <Button Content="B"/>
                //                  </DataTemplate>
                //              </Button.ContentTemplate>
                //          </Button>
                //  Both Buttons have the same ControlTemplate, which would be flagged
                //  by this loop.  But they have different ContentTemplates, so there
                //  isn't a cycle.  Stopping the loop after checking a ContentPresenter
                //  allows cases like the one illustrated here, while catching real
                //  cycles involving ContentPresenter - they're caught when this method
                //  is called with container = ContentPresenter.
                walkNode = (walkNode is ContentPresenter) ? null: nextParent;
            }
        }

        #endregion InstantiateSubTree

        //  ===========================================================================
        //  These methods are invoked when a Template's
        //  VisualTree is destroyed
        //  ===========================================================================

        #region ClearGeneratedSubTree

        //
        //  This method
        //  1. Wipes out all child references to this Templated container.
        //  2. Invalidates all properties that came from FrameworkElementFactory.SetValue
        //
        internal static void ClearGeneratedSubTree(
            HybridDictionary[]          instanceData,
            FrameworkElement            feContainer,
            FrameworkContentElement     fceContainer,
            FrameworkTemplate           oldFrameworkTemplate)
        {
            Debug.Assert(feContainer != null || fceContainer != null,
                "Must supply a non-null container");

            // Forget about the templatedChildren chain
            List<DependencyObject> templatedChildren;
            if (feContainer != null)
            {
                templatedChildren = StyleHelper.TemplatedFeChildrenField.GetValue(feContainer);
                StyleHelper.TemplatedFeChildrenField.ClearValue(feContainer);
            }
            else
            {
                templatedChildren = StyleHelper.TemplatedFeChildrenField.GetValue(fceContainer);
                StyleHelper.TemplatedFeChildrenField.ClearValue(fceContainer);
            }

            DependencyObject rootNode = null;
            if (templatedChildren != null)
            {
                Debug.Assert(templatedChildren.Count > 0,
                    "Styled Children should have at least one element in it");

                // Fetch the rootNode of the template generated tree
                rootNode = templatedChildren[0];

                if (oldFrameworkTemplate != null )
                {
                    // Style/Template has built the tree via FrameworkElementFactories
                    ClearTemplateChain(instanceData, feContainer, fceContainer, templatedChildren, oldFrameworkTemplate );
                }
            }

            // Clear the NameMap property on the root of the generated subtree
            if (rootNode != null)
            {
                rootNode.ClearValue(NameScope.NameScopeProperty);
            }

            // Detach the generated tree from the conatiner
            DetachGeneratedSubTree(feContainer, fceContainer);
        }

        //
        //  This method
        //  1. Detaches the generated sub-tree from this container
        //  2. Invalidates all properties that came from FrameworkElementFactory.SetValue
        //
        private static void DetachGeneratedSubTree(
            FrameworkElement            feContainer,
            FrameworkContentElement     fceContainer)
        {
            Debug.Assert(feContainer != null || fceContainer != null);

            if (feContainer != null)
            {
                feContainer.TemplateChild = null;
                // VisualTree has been cleared
                // Style.FrameworkElementFactory or Style.BuildVisualTree
                // will recreate on next ApplyTemplate
                feContainer.HasTemplateGeneratedSubTree = false;
            }
            else
            {
                // GeneratedTree has been cleared
                // Style.FrameworkElementFactory or Style.BuildVisualTree
                // will recreate on next EnsureLogical
                fceContainer.HasTemplateGeneratedSubTree = false;

                // there is no corresponding method for a logical templated subtree;
                // each container must do this on its own, since logical trees are
                // attached in different ways for different containers.
            }
        }

        //
        //  This method
        //  1. Wipes out all Visual child references to this Templated container.
        //  2. Invalidates all properties that came from FrameworkElementFactory.SetValue
        //
        private static void ClearTemplateChain(
            HybridDictionary[]      instanceData,
            FrameworkElement        feContainer,
            FrameworkContentElement fceContainer,
            List<DependencyObject>  templateChain,
            FrameworkTemplate       oldFrameworkTemplate)
        {
            Debug.Assert(oldFrameworkTemplate != null );

            FrameworkObject container = new FrameworkObject(feContainer, fceContainer);

            HybridDictionary instanceValues = (instanceData != null) ? instanceData[(int)InstanceStyleData.InstanceValues] : null;
            int[] childIndices = new int[templateChain.Count];

            // Assumes that styleChain[0] is the root of the templated subtree
            // structure.  This comes from Template.Seal() where it sets
            // FrameworkElementFactory.IsTemplatedTreeRoot = true.
            // (HasGeneratedSubTree tells us if we have one, but it
            // doesn't tell us who it is.)
            for (int i=0; i< templateChain.Count; i++)
            {
                DependencyObject walk = templateChain[i];

                // Visual child longer longer refers to container
                FrameworkElement fe;
                FrameworkContentElement fce;
                SpecialDowncastToFEorFCE(walk, out fe, out fce, true); // Doesn't throw for Visual3D

                if (fe != null)
                {
                    childIndices[i] = fe.TemplateChildIndex;
                    fe._templatedParent = null;
                    fe.TemplateChildIndex = -1;
                }
                else if (fce != null) // walk is FrameworkContentElement
                {
                    childIndices[i] = fce.TemplateChildIndex;
                    fce._templatedParent = null;
                    fce.TemplateChildIndex = -1;
                }
            }

            // Invalidate all the properties on the visual tree node that
            // have been set on the corresponding FEF. NOTE: We do not do
            // the two operations of deleting members and invalidating properties
            // in the same loop because, any property invalidation involves a
            // call out, which may end up reaching a node that hasn't had its
            // TemplateChildIndex and TemplatedParent dropped. Hence we use two
            // separate loops to do these two operations so one does not
            // interfere with the other.
            for (int i=0; i< templateChain.Count; i++)
            {
                DependencyObject walk = templateChain[i];
                FrameworkObject foWalk = new FrameworkObject(walk);
                int childIndex = childIndices[i];

                Debug.Assert( oldFrameworkTemplate != null );

                // Unapply the style's InstanceValues on the subtree node.
                ProcessInstanceValuesForChild(
                    feContainer, walk, childIndices[i], instanceValues, false,
                    ref oldFrameworkTemplate.ChildRecordFromChildIndex);

                // And now we'll also need to invalidate any properties that
                // came from FrameworkElementFactory.SetValue or VisulaTrigger.SetValue
                // so that they can get cleared. When tearing down a template visual
                // tree we do not want to invalidate the inheritable properties along
                // with the other template properties. This is so because soon after
                // detaching the VisualTree from the container we will be firing an
                // InvalidateTree call and all inheritable properties will get invalidated
                // then. Note that we will also be skipping the Style property along with
                // all other inheritable properties because Style is really a psuedo
                // inheritable property and gets automagically invalidated during an
                // invalidation storm.
                InvalidatePropertiesOnTemplateNode(
                        container.DO,
                        foWalk,
                        childIndices[i],
                        ref oldFrameworkTemplate.ChildRecordFromChildIndex,
                        true /*isDetach*/,
                        oldFrameworkTemplate.VisualTree);

                // Unapply unshared instance values on the current node

                if (foWalk.StoresParentTemplateValues)
                {
                    HybridDictionary parentTemplateValues = StyleHelper.ParentTemplateValuesField.GetValue(walk);

                    StyleHelper.ParentTemplateValuesField.ClearValue(walk);
                    foWalk.StoresParentTemplateValues = false;

                    foreach (DictionaryEntry entry in parentTemplateValues)
                    {
                        DependencyProperty dp = (DependencyProperty)entry.Key;

                        if (entry.Value is MarkupExtension)
                        {
                            // Clear the entry for this unshared value in the per-instance store for MarkupExtensions

                            StyleHelper.ProcessInstanceValue(walk, childIndex, instanceValues, dp, -1, false /*apply*/);
                        }

                        // Invalidate this property so that we no longer use the template applied value

                        walk.InvalidateProperty(dp);
                    }
                }
            }
        }



        // This is a special version of DowncastToFEorFCE, for use by ClearTemplateChain,
        // to handle Visual3D (workaround for PDC)

        internal static void SpecialDowncastToFEorFCE(DependencyObject d,
                                    out FrameworkElement fe, out FrameworkContentElement fce,
                                    bool throwIfNeither)
        {
            if (FrameworkElement.DType.IsInstanceOfType(d))
            {
                fe = (FrameworkElement)d;
                fce = null;
            }
            else if (FrameworkContentElement.DType.IsInstanceOfType(d))
            {
                fe = null;
                fce = (FrameworkContentElement)d;
            }
            else if (throwIfNeither && !(d is System.Windows.Media.Media3D.Visual3D) )
            {
                throw new InvalidOperationException(SR.Get(SRID.MustBeFrameworkDerived, d.GetType()));
            }
            else
            {
                fe = null;
                fce = null;
            }
        }


        #endregion ClearGeneratedSubTree

        //  ===========================================================================
        //  These methods are invoked when an Event
        //  is routed through a tree
        //  ===========================================================================

        #region InvokeEventTriggers

        //
        //  This method
        //  1. Is invoked when Sealing a style/template
        //  2. It is used to add a handler to all those nodes that are targeted by EventTriggers
        //
        //  Note that this is a recursive routine
        //
        internal static FrameworkElementFactory FindFEF(FrameworkElementFactory root, int childIndex)
        {
            if (root._childIndex == childIndex)
            {
                return root;
            }

            FrameworkElementFactory child = root.FirstChild;
            FrameworkElementFactory match = null;
            while (child != null)
            {
                match = FindFEF(child, childIndex);
                if (match != null) return match;

                child = child.NextSibling;
            }

            return null;
        }

        //
        //  This method
        //  1. Invokes the TriggerActions as the corresponding event is being
        //     routed through the tree
        //
        private static void ExecuteEventTriggerActionsOnContainer (object sender, RoutedEventArgs e)
        {
            Debug.Assert(sender is FrameworkElement || sender is FrameworkContentElement,
                "Sender object was expected to be FE or FCE, because they are the ones that can have a Style.  Have Style support been added to some other type?  If so the EventTrigger mechanism needs to be updated too.");

            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE((DependencyObject)sender, out fe, out fce, false);

            Style selfStyle;
            Style selfThemeStyle;
            FrameworkTemplate selfFrameworkTemplate = null;

            if (fe != null)
            {
                selfStyle = fe.Style;
                selfThemeStyle = fe.ThemeStyle;

                // An FE might have a template
                selfFrameworkTemplate = fe.TemplateInternal;
            }
            else
            {
                selfStyle = fce.Style;
                selfThemeStyle = fce.ThemeStyle;

            }

            // Invoke trigger actions on selfStyle
            if (selfStyle != null && selfStyle.EventHandlersStore != null)
            {
                InvokeEventTriggerActions(fe, fce, selfStyle, null, 0, e.RoutedEvent);
            }

            // Invoke trigger actions on theme style
            if (selfThemeStyle != null && selfThemeStyle.EventHandlersStore != null)
            {
                InvokeEventTriggerActions(fe, fce, selfThemeStyle, null, 0, e.RoutedEvent);
            }


            // Invokte trigger actions on the template or table template.
            if (selfFrameworkTemplate != null && selfFrameworkTemplate.EventHandlersStore != null)
            {
                InvokeEventTriggerActions(fe, fce, null /*style*/, selfFrameworkTemplate, 0, e.RoutedEvent);
            }


        }

        //
        //  This method
        //  1. Invokes the TriggerActions as the corresponding event is being
        //     routed through the tree
        //
        private static void ExecuteEventTriggerActionsOnChild (object sender, RoutedEventArgs e)
        {
            Debug.Assert(sender is FrameworkElement || sender is FrameworkContentElement,
                "Sender object was expected to be FE or FCE, because they are the ones that can have a Style.  Have Style support been added to some other type?  If so the EventTrigger mechanism needs to be updated too.");

            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE((DependencyObject)sender, out fe, out fce, false);

            DependencyObject templatedParent;
            int templateChildIndex;

            if (fe != null)
            {
                templatedParent = fe.TemplatedParent;
                templateChildIndex = fe.TemplateChildIndex;
            }
            else
            {
                templatedParent = fce.TemplatedParent;
                templateChildIndex = fce.TemplateChildIndex;
            }

            if (templatedParent != null)
            {
                // This node is the result of a Style/Template's VisualTree expansion

                FrameworkElement feTemplatedParent;
                FrameworkContentElement fceTemplatedParent;
                Helper.DowncastToFEorFCE(templatedParent, out feTemplatedParent, out fceTemplatedParent, false);

                FrameworkTemplate templatedParentTemplate = null;

                Debug.Assert( feTemplatedParent != null );
                templatedParentTemplate = feTemplatedParent.TemplateInternal;

                Debug.Assert(templatedParentTemplate != null, "Must have a VisualTree owner");

                // Invoke the trigger action on the Template
                // for the templated parent.
                InvokeEventTriggerActions(feTemplatedParent, fceTemplatedParent, null /*templatedParentStyle*/, templatedParentTemplate, templateChildIndex, e.RoutedEvent);
            }

        }

        //
        //  This method
        //  1. Invokes the trigger actions on either the given ownerStyle or the ownerTemplate
        //
        private static void InvokeEventTriggerActions(
            FrameworkElement        fe,
            FrameworkContentElement fce,
            Style                   ownerStyle,
            FrameworkTemplate       frameworkTemplate,
            int                     childIndex,
            RoutedEvent           Event)
        {
            Debug.Assert(ownerStyle != null || frameworkTemplate != null );

            List<TriggerAction> actionsList;

            if (ownerStyle != null)
            {
                actionsList = (ownerStyle._triggerActions != null)
                                ? ownerStyle._triggerActions[Event] as List<TriggerAction>
                                : null;
            }
            else
            {
                Debug.Assert( frameworkTemplate != null );
                actionsList = (frameworkTemplate._triggerActions != null)
                                ? frameworkTemplate._triggerActions[Event] as List<TriggerAction>
                                : null;
            }

            if (actionsList != null)
            {
                for (int i = 0; i < actionsList.Count; i++)
                {
                    TriggerAction action = actionsList[i];

                    Debug.Assert(action.ContainingTrigger is EventTrigger,
                        "This trigger actions list used by this method should contain only those actions under event triggers.");

                    int triggerIndex = ((EventTrigger)action.ContainingTrigger).TriggerChildIndex;

                    if (childIndex == triggerIndex)
                    {
                        action.Invoke(fe, fce, ownerStyle, frameworkTemplate, Storyboard.Layers.StyleOrTemplateEventTrigger);
                    }
                }
            }
        }

        #endregion InvokeEventTriggers

        //  ===========================================================================
        //  These methods are invoked when a Property
        //  value is fetched from a Style/Template
        //  ===========================================================================

        #region GetValueMethods

        //
        //  This method
        //  1. Computes the value of a template child
        //     (Index is '0' when the styled container is asking)
        //
        internal static object GetChildValue(
            UncommonField<HybridDictionary[]>   dataField,
            DependencyObject                    container,
            int                                 childIndex,
            FrameworkObject                     child,
            DependencyProperty                  dp,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex,
            ref EffectiveValueEntry             entry,
            out ValueLookupType                 sourceType,
            FrameworkElementFactory             templateRoot)
        {
            object value = DependencyProperty.UnsetValue;
            sourceType = ValueLookupType.Simple;

            // Check if this Child Index is represented in given data-structure
            if ((0 <= childIndex) && (childIndex < childRecordFromChildIndex.Count))
            {
                // Fetch the childRecord for the given childIndex
                ChildRecord childRecord = childRecordFromChildIndex[childIndex];

                // Check if this Property is represented in the childRecord
                int mapIndex = childRecord.ValueLookupListFromProperty.Search(dp.GlobalIndex);
                if (mapIndex >= 0)
                {
                    if (childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value.Count > 0)
                    {
                        // Child Index/Property are both represented in this style/template,
                        // continue with value computation

                        // Pass into helper so ValueLookup struct can be accessed by ref
                        value = GetChildValueHelper(
                            dataField,
                            ref childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value,
                            dp,
                            container,
                            child,
                            childIndex,
                            true,
                            ref entry,
                            out sourceType,
                            templateRoot);
                    }
                }
            }

            return value;
        }

        //
        //  This method
        //  1. Computes the property value given the ChildLookupValue list for it
        //
        private static object GetChildValueHelper(
            UncommonField<HybridDictionary[]>       dataField,
            ref ItemStructList<ChildValueLookup>    valueLookupList,
            DependencyProperty                      dp,
            DependencyObject                        container,
            FrameworkObject                         child,
            int                                     childIndex,
            bool                                    styleLookup,
            ref EffectiveValueEntry                 entry,
            out ValueLookupType                     sourceType,
            FrameworkElementFactory                 templateRoot)
        {
            Debug.Assert(child.IsValid, "child should either be an FE or an FCE");

            object value = DependencyProperty.UnsetValue;
            sourceType = ValueLookupType.Simple;

            // Walk list backwards since highest priority lookup items are inserted last
            for (int i = valueLookupList.Count - 1; i >= 0; i--)
            {
                sourceType = valueLookupList.List[i].LookupType;

                // Lookup logic is determined by lookup type. "Trigger"
                // is misleading right now because today it's also being used
                // for Storyboard timeline lookups.
                switch (valueLookupList.List[i].LookupType)
                {
                case ValueLookupType.Simple:
                    {
                        // Simple value
                        value = valueLookupList.List[i].Value;
                    }
                    break;

                case ValueLookupType.Trigger:
                case ValueLookupType.PropertyTriggerResource:
                case ValueLookupType.DataTrigger:
                case ValueLookupType.DataTriggerResource:
                    {
                        // Conditional value based on Container state
                        bool triggerMatch = true;

                        if( valueLookupList.List[i].Conditions != null )
                        {
                            // Check whether the trigger applies.  All conditions must match,
                            // so the loop can terminate as soon as it finds a condition
                            // that doesn't match.
                            for (int j = 0; triggerMatch && j < valueLookupList.List[i].Conditions.Length; j++)
                            {
                                object state;

                                switch (valueLookupList.List[i].LookupType)
                                {
                                case ValueLookupType.Trigger:
                                case ValueLookupType.PropertyTriggerResource:
                                    // Find the source node
                                    DependencyObject sourceNode;
                                    int sourceChildIndex = valueLookupList.List[i].Conditions[j].SourceChildIndex;
                                    if (sourceChildIndex == 0)
                                    {
                                        sourceNode = container;
                                    }
                                    else
                                    {
                                        sourceNode = StyleHelper.GetChild(container, sourceChildIndex);
                                    }

                                    // Note that the sourceNode could be null when the source
                                    // property for this trigger is on a node that hasn't been
                                    // instantiated yet.
                                    DependencyProperty sourceProperty = valueLookupList.List[i].Conditions[j].Property;
                                    if (sourceNode != null)
                                    {
                                        state = sourceNode.GetValue(sourceProperty);
                                    }
                                    else
                                    {
                                        Type sourceNodeType;

                                        if( templateRoot != null )
                                        {
                                            sourceNodeType = FindFEF(templateRoot, sourceChildIndex).Type;
                                        }
                                        else
                                        {
                                            sourceNodeType = (container as FrameworkElement).TemplateInternal.ChildTypeFromChildIndex[sourceChildIndex];
                                        }

                                        state = sourceProperty.GetDefaultValue(sourceNodeType);
                                    }

                                    triggerMatch = valueLookupList.List[i].Conditions[j].Match(state);

                                    break;

                                case ValueLookupType.DataTrigger:
                                case ValueLookupType.DataTriggerResource:
                                default:    // this cannot happen - but make the compiler happy

                                    state = GetDataTriggerValue(dataField, container, valueLookupList.List[i].Conditions[j].Binding);
                                    triggerMatch = valueLookupList.List[i].Conditions[j].ConvertAndMatch(state);

                                    break;
                                }
                            }
                        }

                        if (triggerMatch)
                        {
                            // Conditionals matched, use the value

                            if (valueLookupList.List[i].LookupType == ValueLookupType.PropertyTriggerResource ||
                                valueLookupList.List[i].LookupType == ValueLookupType.DataTriggerResource)
                            {
                                // Resource lookup
                                object source;
                                value = FrameworkElement.FindResourceInternal(child.FE,
                                                                              child.FCE,
                                                                              dp,
                                                                              valueLookupList.List[i].Value,  // resourceKey
                                                                              null,  // unlinkedParent
                                                                              true,  // allowDeferredResourceReference
                                                                              false, // mustReturnDeferredResourceReference
                                                                              null,  // boundaryElement
                                                                              false, // disableThrowOnResourceNotFound
                                                                              out source);

                                // Try to freeze the value
                                SealIfSealable(value);
                            }
                            else
                            {
                                value = valueLookupList.List[i].Value;
                            }
                        }
                    }
                    break;

                case ValueLookupType.TemplateBinding:
                    {
                        TemplateBindingExtension templateBinding = (TemplateBindingExtension)valueLookupList.List[i].Value;
                        DependencyProperty sourceProperty = templateBinding.Property;

                        // Direct binding of Child property to Container
                        value = container.GetValue(sourceProperty);

                        // Apply the converter, if any
                        if (templateBinding.Converter != null)
                        {
                            DependencyProperty targetProperty = valueLookupList.List[i].Property;
                            System.Globalization.CultureInfo culture = child.Language.GetCompatibleCulture();

                            value = templateBinding.Converter.Convert(
                                                value,
                                                targetProperty.PropertyType,
                                                templateBinding.ConverterParameter,
                                                culture);
                        }

                        // if the binding returns an invalid value, fallback to default value
                        if ((value != DependencyProperty.UnsetValue) && !dp.IsValidValue(value))
                        {
                            value = DependencyProperty.UnsetValue;
                        }
                    }
                    break;

                case ValueLookupType.Resource:
                    {
                        // Resource lookup
                        object source;
                        value = FrameworkElement.FindResourceInternal(
                                        child.FE,
                                        child.FCE,
                                        dp,
                                        valueLookupList.List[i].Value,  // resourceKey
                                        null,  // unlinkedParent
                                        true,  // allowDeferredResourceReference
                                        false, // mustReturnDeferredResourceReference
                                        null,  // boundaryElement
                                        false, // disableThrowOnResourceNotFound
                                        out source);

                        // Try to freeze the value
                        SealIfSealable(value);
                    }
                    break;
                }

                // See if value needs per-instance storage
                if (value != DependencyProperty.UnsetValue)
                {
                    entry.Value = value;
                    // When the value requires per-instance storage (and comes from this style),
                    // get the real value from per-instance data.
                    switch (valueLookupList.List[i].LookupType)
                    {
                    case ValueLookupType.Simple:
                    case ValueLookupType.Trigger:
                    case ValueLookupType.DataTrigger:
                        {
                            MarkupExtension me;
                            Freezable freezable;

                            if ((me = value as MarkupExtension) != null)
                            {
                                value = GetInstanceValue(
                                                dataField,
                                                container,
                                                child.FE,
                                                child.FCE,
                                                childIndex,
                                                valueLookupList.List[i].Property,
                                                i,
                                                ref entry);
                            }
                            else if ((freezable = value as Freezable) != null && !freezable.IsFrozen)
                            {
                                value = GetInstanceValue(
                                                dataField,
                                                container,
                                                child.FE,
                                                child.FCE,
                                                childIndex,
                                                valueLookupList.List[i].Property,
                                                i,
                                                ref entry);
                            }
                        }
                        break;

                    default:
                        break;
                    }
                }

                if (value != DependencyProperty.UnsetValue)
                {
                    // Found a value, break out of the for() loop.
                    break;
                }
            }

            return value;
        }


        //
        //  This method
        //  1. Retrieves a value from a binding in the condition of a data trigger
        //
        internal static object GetDataTriggerValue(
            UncommonField<HybridDictionary[]>  dataField,
            DependencyObject            container,
            BindingBase                 binding)
        {
            // get the container's instance value list - the bindings are stored there
            HybridDictionary[] data = dataField.GetValue(container);
            HybridDictionary instanceValues = EnsureInstanceData(dataField, container, InstanceStyleData.InstanceValues);

            // get the binding, creating it if necessary
            BindingExpressionBase bindingExpr = (BindingExpressionBase)instanceValues[binding];
            if (bindingExpr == null)
            {
                bindingExpr = BindingExpression.CreateUntargetedBindingExpression(container, binding);
                instanceValues[binding] = bindingExpr;

                if (dataField == StyleDataField)
                {
                    bindingExpr.ValueChanged += new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInStyleChanged);
                }
                else if (dataField == TemplateDataField)
                {
                    bindingExpr.ResolveNamesInTemplate = true;
                    bindingExpr.ValueChanged += new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInTemplateChanged);
                }
                else
                {
                    Debug.Assert(dataField == ThemeStyleDataField);
                    bindingExpr.ValueChanged += new EventHandler<BindingValueChangedEventArgs>(OnBindingValueInThemeStyleChanged);
                }
                bindingExpr.Attach(container);
            }

            // get the value
            return bindingExpr.Value;
        }

        //
        //  This method
        //  1. Retrieves an instance value from per-instance StyleData.
        //  2. Creates the StyleData if this is the first request.
        //
        internal static object GetInstanceValue(
            UncommonField<HybridDictionary []>  dataField,
            DependencyObject            container,
            FrameworkElement            feChild,
            FrameworkContentElement     fceChild,
            int                         childIndex,
            DependencyProperty          dp,
            int                         i,
            ref EffectiveValueEntry     entry)
        {
            object rawValue = entry.Value;
            DependencyObject child = null;

            FrameworkElement feContainer;
            FrameworkContentElement fceContainer;
            Helper.DowncastToFEorFCE(container, out feContainer, out fceContainer, true);

            HybridDictionary[] styleData = (dataField != null) ? dataField.GetValue(container) : null;
            HybridDictionary instanceValues = (styleData != null) ? styleData[(int)InstanceStyleData.InstanceValues] : null;
            InstanceValueKey key = new InstanceValueKey(childIndex, dp.GlobalIndex, i);

            object value = (instanceValues != null)? instanceValues[key] : null;
            bool isRequestingExpression = (feChild != null) ? feChild.IsRequestingExpression : fceChild.IsRequestingExpression;

            if (value == null)
            {
                value = NotYetApplied;
            }

            // if the value is a detached expression, replace it with a new one
            Expression expr = value as Expression;
            if (expr != null && expr.HasBeenDetached)
            {
                value = NotYetApplied;
            }

            // if this is the first request, create the value
            if (value == NotYetApplied)
            {
                child = feChild;
                if (child == null)
                    child = fceChild;

                MarkupExtension me;
                Freezable freezable;

                if ((me = rawValue as MarkupExtension) != null)
                {
                    // exception:  if the child is not yet initialized and the request
                    // is for an expression, don't create the value.  This gives the parser
                    // a chance to set local values, to override the style-defined values.
                    if (isRequestingExpression)
                    {
                        bool isInitialized = (feChild != null) ? feChild.IsInitialized : fceChild.IsInitialized;
                        if (!isInitialized)
                        {
                            return DependencyProperty.UnsetValue;
                        }
                    }

                    ProvideValueServiceProvider provideValueServiceProvider = new ProvideValueServiceProvider();
                    provideValueServiceProvider.SetData( child, dp );
                    value = me.ProvideValue(provideValueServiceProvider);
                }
                else if ((freezable = rawValue as Freezable) != null)
                {
                    value = freezable.Clone();
                    child.ProvideSelfAsInheritanceContext(value, dp);
                }

                // store it in per-instance StyleData (even if it's DependencyProperty.UnsetValue)
                Debug.Assert(value != NotYetApplied, "attempt to retrieve instance value that was never set");
                instanceValues[key] = value;

                if (value != DependencyProperty.UnsetValue)
                {
                    expr = value as Expression;
                    // if the instance value is an expression, attach it
                    if (expr != null)
                    {
                        expr.OnAttach(child, dp);
                    }
                }
            }

            // if the value is an Expression (and we're being asked for the real value),
            // delegate to the expression.
            if (expr != null)
            {
                if (!isRequestingExpression)
                {
                    if (child == null)
                    {
                        child = feChild;
                        if (child == null)
                            child = fceChild;
                    }

                    entry.ResetValue(DependencyObject.ExpressionInAlternativeStore, true);
                    entry.SetExpressionValue(expr.GetValue(child, dp), DependencyObject.ExpressionInAlternativeStore);
                }
                else
                {
                    entry.Value = value;
                }
            }
            else
            {
                entry.Value = value;
            }

            return value;
        }

#if DEBUG
        //
        //  This method
        //  1. Verifies that the condition properties on a style trigger that caused this template
        //     it value aren't actually being set via the template itself. Because that would be
        //     a cyclic reference and would be bad.
        //
        internal static void CheckForCyclicReferencesInStyleAndTemplateTriggers(
            DependencyProperty  templateProperty,
            FrameworkTemplate   frameworkTemplate,
            Style               style,
            Style               themeStyle)
        {
            Debug.Assert(frameworkTemplate != null );

            CheckForCyclicReferencesInStyleAndTemplateTriggers(templateProperty, frameworkTemplate, style);
            if (themeStyle != style)
            {
                CheckForCyclicReferencesInStyleAndTemplateTriggers(templateProperty, frameworkTemplate, themeStyle);
            }
        }

        private static void CheckForCyclicReferencesInStyleAndTemplateTriggers(
            DependencyProperty  templateProperty,
            FrameworkTemplate   frameworkTemplate,
            Style               style)
        {
            Debug.Assert(frameworkTemplate != null );

            FrugalStructList<ContainerDependent> containerDependents = frameworkTemplate.ContainerDependents;

            if (style != null && style.ChildRecordFromChildIndex.Count > 0)
            {
                // Fetch the childRecord for the container
                ChildRecord childRecord = style.ChildRecordFromChildIndex[0];

                // Check if this Property is represented in the childRecord
                int mapIndex = childRecord.ValueLookupListFromProperty.Search(templateProperty.GlobalIndex);
                if (mapIndex >= 0)
                {
                    ItemStructList<ChildValueLookup> valueLookupList = childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value;
                    if (valueLookupList.Count > 0)
                    {
                        // Walk list backwards since highest priority lookup items are inserted last
                        for (int i = valueLookupList.Count - 1; i >= 0; i--)
                        {
                            // Lookup logic is determined by lookup type. "Trigger"
                            // is misleading right now because today it's also being used
                            // for Storyboard timeline lookups.
                            switch (valueLookupList.List[i].LookupType)
                            {
                            case ValueLookupType.Trigger:
                            case ValueLookupType.PropertyTriggerResource:
                            case ValueLookupType.DataTrigger:
                            case ValueLookupType.DataTriggerResource:
                                {
                                    TriggerCondition[] conditions = valueLookupList.List[i].Conditions;
                                    if (conditions != null)
                                    {
                                        // Check for property trigger presence & applicability
                                        for (int j = 0; j < conditions.Length; j++)
                                        {
                                            Debug.Assert(!StyleHelper.IsSetOnContainer(conditions[j].Property, ref containerDependents, true), "Style trigger condition property " + conditions[j].Property + " is set via a template trigger on the container. This is a cyclic reference and is illegal.");
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
#endif

        //
        //  This method
        //  1. Says if [FE/FCE].GetRawValue should look for a value on the self style.
        //  2. It establishes the rule that any dependency property other than the
        //     StyleProperty are stylable on the self style
        //
        internal static bool ShouldGetValueFromStyle (DependencyProperty dp)
        {
            return (dp != FrameworkElement.StyleProperty);
        }

        //
        //  This method
        //  1. Says if [FE/FCE].GetRawValue should look for a value on the self themestyle.
        //  2. It establishes the rule that any dependency property other than the
        //     StyleProperty, OverridesDefaultStyleProperty and DefaultStyleKeyProperty
        //     are stylable on the self themestyle
        //
        internal static bool ShouldGetValueFromThemeStyle (DependencyProperty dp)
        {
            return (dp != FrameworkElement.StyleProperty &&
                    dp != FrameworkElement.DefaultStyleKeyProperty &&
                    dp != FrameworkElement.OverridesDefaultStyleProperty);
        }

        //
        //  This method
        //  1. Says if [FE/FCE].GetRawValue should look for a value on the self template.
        //  2. It establishes the rule that any dependency property other than the
        //     StyleProperty, OverridesDefaulStyleProperty, DefaultStyleKeyProperty and
        //     TemplateProperty are stylable on the self template.
        //
        internal static bool ShouldGetValueFromTemplate(
            DependencyProperty dp)
        {
            return (dp != FrameworkElement.StyleProperty &&
                    dp != FrameworkElement.DefaultStyleKeyProperty &&
                    dp != FrameworkElement.OverridesDefaultStyleProperty &&
                    dp != Control.TemplateProperty &&
                    dp != ContentPresenter.TemplateProperty);
        }

        #endregion GetValueMethods

        //  ===========================================================================
        //  These methods are invoked when a Property is being
        //  invalidated via a Style/Template
        //  ===========================================================================

        #region InvalidateMethods

        //
        //  This method
        //  1. Is invoked when the StyleProperty is invalidated on a FrameworkElement or
        //     FrameworkContentElement or a sub-class thereof.
        //
        internal static void DoStyleInvalidations(
            FrameworkElement fe,
            FrameworkContentElement fce,
            Style oldStyle,
            Style newStyle)
        {
            Debug.Assert(fe != null || fce != null);

            if (oldStyle != newStyle)
            {
                //
                // Style is changing
                //

                DependencyObject container = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

                // If the style wants to watch for the Loaded and/or Unloaded events, set the
                // flag that says we want to receive it.  Otherwise, if it was set in the old style, clear it.
                StyleHelper.UpdateLoadedFlag( container, oldStyle, newStyle );

                // Set up any per-instance state relating to the new Style
                // We do this here instead of OnStyleInvalidated because
                //  this needs to happen for the *first* Style.
                StyleHelper.UpdateInstanceData(
                    StyleHelper.StyleDataField,
                    fe, fce,
                    oldStyle, newStyle,
                    null /* oldFrameworkTemplate */, null /* newFrameworkTemplate */,
                    (InternalFlags)0);

                // If this new style has resource references (either for the container
                // or for children in the visual tree), then, mark it so that it will
                // not be ignored during resource change invalidations
                if ((newStyle != null) && (newStyle.HasResourceReferences))
                {
                    if (fe != null)
                    {
                        fe.HasResourceReference = true;
                    }
                    else
                    {
                        fce.HasResourceReference = true;
                    }
                }

                FrugalStructList<ContainerDependent> oldContainerDependents =
                    oldStyle != null ? oldStyle.ContainerDependents : StyleHelper.EmptyContainerDependents;

                FrugalStructList<ContainerDependent> newContainerDependents =
                    newStyle != null ? newStyle.ContainerDependents : StyleHelper.EmptyContainerDependents;

                // Propagate invalidation for Style dependents
                FrugalStructList<ContainerDependent> exclusionContainerDependents =
                    new FrugalStructList<ContainerDependent>();
                StyleHelper.InvalidateContainerDependents(container,
                    ref exclusionContainerDependents,
                    ref oldContainerDependents,
                    ref newContainerDependents);

                // Propagate invalidation for resource references that may be
                // picking stuff from the style's ResourceDictionary
                DoStyleResourcesInvalidations(container, fe, fce, oldStyle, newStyle);

                // Notify Style has changed
                // CALLBACK
                if (fe != null)
                {
                    fe.OnStyleChanged(oldStyle, newStyle);
                }
                else
                {
                    fce.OnStyleChanged(oldStyle, newStyle);
                }
            }
        }

        //
        //  This method
        //  1. Is invoked when the ThemeStyleProperty is invalidated on a FrameworkElement or
        //     FrameworkContentElement or a sub-class thereof.
        //
        internal static void DoThemeStyleInvalidations(
            FrameworkElement fe,
            FrameworkContentElement fce,
            Style oldThemeStyle,
            Style newThemeStyle,
            Style style)
        {
            Debug.Assert(fe != null || fce != null);

            if (oldThemeStyle != newThemeStyle && newThemeStyle != style)
            {
                //
                // Style is changing
                //

                DependencyObject container = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

                // If the them style wants to watch for the Loaded and/or Unloaded events, set the
                // flag that says we want to receive it.  Otherwise, if it was set in the old style, clear it.
                StyleHelper.UpdateLoadedFlag( container, oldThemeStyle, newThemeStyle );

                // Set up any per-instance state relating to the new Style
                // We do this here instead of OnStyleInvalidated because
                // this needs to happen for the *first* Style.
                StyleHelper.UpdateInstanceData(
                    StyleHelper.ThemeStyleDataField,
                    fe, fce,
                    oldThemeStyle, newThemeStyle,
                    null /* oldFrameworkTemplate */, null /* newFrameworkTemplate */,
                    (InternalFlags)0);

                // If this new style has resource references (either for the container
                // or for children in the visual tree), then, mark it so that it will
                // not be ignored during resource change invalidations
                if ((newThemeStyle != null) && (newThemeStyle.HasResourceReferences))
                {
                    if (fe != null)
                    {
                        fe.HasResourceReference = true;
                    }
                    else
                    {
                        fce.HasResourceReference = true;
                    }
                }

                FrugalStructList<ContainerDependent> oldContainerDependents =
                    oldThemeStyle != null ? oldThemeStyle.ContainerDependents : StyleHelper.EmptyContainerDependents;

                FrugalStructList<ContainerDependent> newContainerDependents =
                    newThemeStyle != null ? newThemeStyle.ContainerDependents : StyleHelper.EmptyContainerDependents;

                // Propagate invalidation for ThemeStyle dependents
                // Note that we are using the properties in the style's ContainerDependents as the exclusion list.
                // This is so because GetValue logica gives precedence to a style value over a theme style value.
                // So there should be no need to invalidate those properties when the theme style changes.
                FrugalStructList<ContainerDependent> exclusionContainerDependents =
                    (style != null) ? style.ContainerDependents : new FrugalStructList<ContainerDependent>();
                StyleHelper.InvalidateContainerDependents(container,
                    ref exclusionContainerDependents,
                    ref oldContainerDependents,
                    ref newContainerDependents);


                // Propagate invalidation for resource references that may be
                // picking stuff from the style's ResourceDictionary
                DoStyleResourcesInvalidations(container, fe, fce, oldThemeStyle, newThemeStyle);
            }
        }

        //
        //  This method
        //  1. Is invoked when the TemplateProperty is invalidated on a Control,
        //     Page, PageFunctionBase, ContentPresenter, or a sub-class thereof.
        //
        internal static void DoTemplateInvalidations(
            FrameworkElement            feContainer,
            FrameworkTemplate           oldFrameworkTemplate)
        {
            Debug.Assert(feContainer != null);

            DependencyObject    container;
            HybridDictionary[]  oldTemplateData;
            FrameworkTemplate   newFrameworkTemplate = null;
            object              oldTemplate;
            object              newTemplate;
            bool                newTemplateHasResourceReferences;

            Debug.Assert(feContainer != null);

            // Fetch the per-instance data before it goes away (during Template_get)
            oldTemplateData = StyleHelper.TemplateDataField.GetValue(feContainer);

            // Do immediate pull of Template to refresh the value since the
            // new Template needs to be known at this time to do accurate
            // invalidations
            newFrameworkTemplate = feContainer.TemplateInternal;

            container = feContainer;
            oldTemplate = oldFrameworkTemplate;
            newTemplate = newFrameworkTemplate;
            newTemplateHasResourceReferences = (newFrameworkTemplate != null) ? newFrameworkTemplate.HasResourceReferences : false;

            // If the template wants to watch for the Loaded and/or Unloaded events, set the
            // flag that says we want to receive it.  Otherwise, if it was set in the old template, clear it.
            StyleHelper.UpdateLoadedFlag( container, oldFrameworkTemplate, newFrameworkTemplate );

            if (oldTemplate != newTemplate)
            {
                //
                // Template is changing
                //

                // Set up any per-instance state relating to the new Template
                // We do this here instead of OnTemplateInvalidated because
                // this needs to happen for the *first* Template.
                StyleHelper.UpdateInstanceData(
                    StyleHelper.TemplateDataField,
                    feContainer /* fe */, null /* fce */,
                    null /*oldStyle */, null /* newStyle */,
                    oldFrameworkTemplate, newFrameworkTemplate,
                    InternalFlags.HasTemplateGeneratedSubTree);

                // If this new template has resource references (either for the container
                // or for children in the visual tree), then, mark it so that it will
                // not be ignored during resource change invalidations
                if (newTemplate != null && newTemplateHasResourceReferences)
                {
                    Debug.Assert(feContainer != null);
                    feContainer.HasResourceReference = true;
                }

                // If the template wants to watch for the Loaded and/or Unloaded events, set the
                // flag that says we want to receive it.  Otherwise, if it was set in the old template, clear it.

                UpdateLoadedFlag( container, oldFrameworkTemplate, newFrameworkTemplate );


                // Wipe out VisualTree only if VisualTree factories
                // are changing
                //
                // If the factories are null for both new and old, then, the Template
                // has the opportunity to supply the VisualTree using the "BuildVisualTree"
                // virtual.
                FrameworkElementFactory              oldFactory;
                FrameworkElementFactory              newFactory;
                bool                                 canBuildVisualTree;
                bool                                 hasTemplateGeneratedSubTree;
                FrugalStructList<ContainerDependent> oldContainerDependents;
                FrugalStructList<ContainerDependent> newContainerDependents;

                Debug.Assert(feContainer != null);
                oldFactory = (oldFrameworkTemplate != null) ? oldFrameworkTemplate.VisualTree : null;
                newFactory = (newFrameworkTemplate != null) ? newFrameworkTemplate.VisualTree : null;

                canBuildVisualTree = (oldFrameworkTemplate != null) ? oldFrameworkTemplate.CanBuildVisualTree : false;
                hasTemplateGeneratedSubTree = feContainer.HasTemplateGeneratedSubTree;
                oldContainerDependents = (oldFrameworkTemplate != null) ? oldFrameworkTemplate.ContainerDependents : StyleHelper.EmptyContainerDependents;
                newContainerDependents = (newFrameworkTemplate != null) ? newFrameworkTemplate.ContainerDependents : StyleHelper.EmptyContainerDependents;

                if (hasTemplateGeneratedSubTree)
                {
                    StyleHelper.ClearGeneratedSubTree(oldTemplateData,
                        feContainer /* fe */, null /* fce */,
                        oldFrameworkTemplate );
                }

                // Propagate invalidation for template dependents
                FrugalStructList<ContainerDependent> exclusionContainerDependents =
                    new FrugalStructList<ContainerDependent>();
                StyleHelper.InvalidateContainerDependents(container,
                    ref exclusionContainerDependents,
                    ref oldContainerDependents,
                    ref newContainerDependents);

                // Propagate invalidation for resource references that may be
                // picking stuff from the style's ResourceDictionary
                DoTemplateResourcesInvalidations(container, feContainer, null /*fce*/, oldTemplate, newTemplate);

                Debug.Assert(feContainer != null);
                feContainer.OnTemplateChangedInternal(oldFrameworkTemplate, newFrameworkTemplate);
            }
            else
            {
                //
                // Template is not changing
                //

                // Template was invalidated but didn't change. If the Template created the
                // VisualTree via an override of BuildVisualTree, then, it is
                // wiped out now so that it may be conditionally rebuilt by the
                // custom Template
                if (newFrameworkTemplate != null)
                {
                    if (feContainer.HasTemplateGeneratedSubTree
                        && newFrameworkTemplate.VisualTree == null
                        && !newFrameworkTemplate.HasXamlNodeContent )
                    {
                        StyleHelper.ClearGeneratedSubTree(oldTemplateData, feContainer /* fe */, null /* fce */,
                            oldFrameworkTemplate);

                        // Nothing guarantees that ApplyTemplate actually gets
                        // called, so ask for it explicitly (bug 963163).
                        feContainer.InvalidateMeasure();
                    }
                }
            }
        }

        //
        // DoStyleResourcesInvalidation
        //
        // This method is called to propagate invalidations for a Style.Resources
        // change so all the ResourcesReferences in the sub-tree of the container
        // gets invalidated.
        //
        internal static void DoStyleResourcesInvalidations(
            DependencyObject        container,
            FrameworkElement        fe,
            FrameworkContentElement fce,
            Style                   oldStyle,
            Style                   newStyle)
        {
            Debug.Assert(fe != null || fce != null);
            Debug.Assert(container == fe || container == fce);

            // Propagate invalidation for resource references that may be
            // picking stuff from the style's ResourceDictionary. This
            // invalidation needs to happen only if the style change is
            // not the result of a tree change. If it is then the
            // ResourceReferences are already being wiped out by the
            // InvalidateTree called for the tree change operation and so
            // we do not need to repeat it here.
            bool isAncestorChangedInProgress = (fe != null) ? fe.AncestorChangeInProgress : fce.AncestorChangeInProgress;
            if (!isAncestorChangedInProgress)
            {
                List<ResourceDictionary> oldStyleTables = StyleHelper.GetResourceDictionariesFromStyle(oldStyle);
                List<ResourceDictionary> newStyleTables = StyleHelper.GetResourceDictionariesFromStyle(newStyle);

                if ((oldStyleTables != null && oldStyleTables.Count > 0) ||
                    (newStyleTables != null && newStyleTables.Count > 0))
                {
                    // Set the ShouldLookupImplicitStyles flag if the given style's Resources has implicit styles.
                    SetShouldLookupImplicitStyles(new FrameworkObject(fe, fce), newStyleTables);

                    TreeWalkHelper.InvalidateOnResourcesChange(fe, fce, new ResourcesChangeInfo(oldStyleTables, newStyleTables, true /*isStyleResourcesChange*/, false /*isTemplateResourcesChange*/, container));
                }
            }
        }

        //
        // DoTemplateResourcesInvalidation
        //
        // This method is called to propagate invalidations for a Template.Resources
        // change so all the ResourcesReferences in the sub-tree of the container
        // gets invalidated.
        //
        internal static void DoTemplateResourcesInvalidations(
            DependencyObject        container,
            FrameworkElement        fe,
            FrameworkContentElement fce,
            object                  oldTemplate,
            object                  newTemplate)
        {
            Debug.Assert(fe != null || fce != null);
            Debug.Assert(container == fe || container == fce);
            Debug.Assert( oldTemplate == null || oldTemplate is FrameworkTemplate,
                "Existing template is of unknown type." );
            Debug.Assert( newTemplate == null || newTemplate is FrameworkTemplate,
                "New template is of unknown type.");
            Debug.Assert( (oldTemplate == null || newTemplate == null ) ||
                (oldTemplate is FrameworkTemplate && newTemplate is FrameworkTemplate),
                "Old and new template types do not match." );

            // Propagate invalidation for resource references that may be
            // picking stuff from the template's ResourceDictionary. This
            // invalidation needs to happen only if the template change is
            // not the result of a tree change. If it is then the
            // ResourceReferences are already being wiped out by the
            // InvalidateTree called for the tree change operation and so
            // we do not need to repeat it here.
            bool isAncestorChangedInProgress = (fe != null) ? fe.AncestorChangeInProgress : fce.AncestorChangeInProgress;
            if (!isAncestorChangedInProgress)
            {
                List<ResourceDictionary> oldResourceTable = GetResourceDictionaryFromTemplate(oldTemplate);
                List<ResourceDictionary> newResourceTable = GetResourceDictionaryFromTemplate(newTemplate);

                if (oldResourceTable != newResourceTable)
                {
                    // Set the ShouldLookupImplicitStyles flag if the given template's Resources has implicit styles.
                    SetShouldLookupImplicitStyles(new FrameworkObject(fe, fce), newResourceTable);

                    TreeWalkHelper.InvalidateOnResourcesChange(fe, fce, new ResourcesChangeInfo(oldResourceTable, newResourceTable, false /*isStyleResourcesChange*/, true /*isTemplateResourcesChange*/, container));
                }
            }
        }

        // Sets the ShouldLookupImplicitStyles flag on the given element if the given style/template's
        // ResourceDictionary has implicit styles.
        private static void SetShouldLookupImplicitStyles(FrameworkObject fo, List<ResourceDictionary> dictionaries)
        {
            if (dictionaries != null && dictionaries.Count > 0 && !fo.ShouldLookupImplicitStyles)
            {
                for (int i=0; i<dictionaries.Count; i++)
                {
                    if (dictionaries[i].HasImplicitStyles)
                    {
                        fo.ShouldLookupImplicitStyles = true;
                        break;
                    }
                }
            }
        }

        // Given an object (that might be a Style or ThemeStyle)
        // get its and basedOn Style's ResourceDictionary.  Null
        // is returned if all the dictionaries turn out to be empty.
        private static List<ResourceDictionary> GetResourceDictionariesFromStyle(Style style)
        {
            List<ResourceDictionary> dictionaries = null;

            while (style != null)
            {
                if (style._resources != null)
                {
                    if (dictionaries == null)
                    {
                        dictionaries = new List<ResourceDictionary>(1);
                    }

                    dictionaries.Add(style._resources);
                }

                style = style.BasedOn;
            }

            return dictionaries;
        }

        // Given an object (that might be a FrameworkTemplate)
        //  get its ResourceDictionary.  Null is returned if the dictionary
        //  turns out to be empty.
        private static List<ResourceDictionary> GetResourceDictionaryFromTemplate( object template )
        {
            ResourceDictionary resources = null;

            if( template is FrameworkTemplate )
            {
                resources = ((FrameworkTemplate)template)._resources;
            }

            if (resources != null)
            {
                List<ResourceDictionary> table = new List<ResourceDictionary>(1);
                table.Add(resources);
                return table;
            }

            return null;
        }

        //
        // UpdateLoadedFlags
        //
        // These methods are called to update the Loaded/Unloaded
        // optimization flags on an FE or FCE, when a style or template
        // affecting that element has changed.
        // If HasLoadedChangeHandler was through an Interface we could Template this.

        internal static void UpdateLoadedFlag( DependencyObject d,
                Style oldStyle, Style newStyle)
        {
            Invariant.Assert(null != oldStyle || null != newStyle);

            if((oldStyle==null || !oldStyle.HasLoadedChangeHandler)
                && (newStyle != null && newStyle.HasLoadedChangeHandler))
            {
                BroadcastEventHelper.AddHasLoadedChangeHandlerFlagInAncestry(d);
            }
            else if((oldStyle != null && oldStyle.HasLoadedChangeHandler)
                && (newStyle==null || !newStyle.HasLoadedChangeHandler))
            {
                BroadcastEventHelper.RemoveHasLoadedChangeHandlerFlagInAncestry(d);
            }
        }

        internal static void UpdateLoadedFlag( DependencyObject d,
                FrameworkTemplate oldFrameworkTemplate, FrameworkTemplate newFrameworkTemplate)
        {
            // We've seen a case where the XAML designer in VS uses DeferredThemeResourceReference
            // for the Template, but the old value evaluates to null and the new value is null because the
            // element was being removed from the tree.  In such cases, just no-op, instead of the old
            // Invariant.Assert.
            // Invariant.Assert(null != oldFrameworkTemplate || null != newFrameworkTemplate);

            if((oldFrameworkTemplate==null || !oldFrameworkTemplate.HasLoadedChangeHandler)
                && (newFrameworkTemplate != null && newFrameworkTemplate.HasLoadedChangeHandler))
            {
                BroadcastEventHelper.AddHasLoadedChangeHandlerFlagInAncestry(d);
            }
            else if((oldFrameworkTemplate != null && oldFrameworkTemplate.HasLoadedChangeHandler)
                && (newFrameworkTemplate==null || !newFrameworkTemplate.HasLoadedChangeHandler))
            {
                BroadcastEventHelper.RemoveHasLoadedChangeHandlerFlagInAncestry(d);
            }
        }

         //
        //  This method
        //  1. Invalidates all the properties set on the container's style.
        //     The value could have been set directly on the Style or via Trigger.
        //
        internal static void InvalidateContainerDependents(
            DependencyObject                         container,
            ref FrugalStructList<ContainerDependent> exclusionContainerDependents,
            ref FrugalStructList<ContainerDependent> oldContainerDependents,
            ref FrugalStructList<ContainerDependent> newContainerDependents)
        {
            // Invalidate all properties on the container that were being driven via the oldStyle
            int count = oldContainerDependents.Count;
            for (int i = 0; i < count; i++)
            {
                DependencyProperty dp = oldContainerDependents[i].Property;

                // Invalidate the property only if it is not locally set
                if (!IsSetOnContainer(dp, ref exclusionContainerDependents, false /*alsoFromTriggers*/))
                {
                    // call GetValueCore to get value from Style/Template
                    container.InvalidateProperty(dp);
                }
            }

            // Invalidate all properties on the container that will be driven via the newStyle
            count = newContainerDependents.Count;
            if (count > 0)
            {
                FrameworkObject fo = new FrameworkObject(container);

                for (int i = 0; i < count; i++)
                {
                    DependencyProperty dp = newContainerDependents[i].Property;

                    // Invalidate the property only if it
                    // - is not a part of oldContainerDependents and
                    // - is not locally set
                    if (!IsSetOnContainer(dp, ref exclusionContainerDependents, false /*alsoFromTriggers*/) &&
                        !IsSetOnContainer(dp, ref oldContainerDependents, false /*alsoFromTriggers*/))
                    {
                        ApplyStyleOrTemplateValue(fo, dp);
                    }
                }
            }
        }

        internal static void ApplyTemplatedParentValue(
                DependencyObject                container,
                FrameworkObject                 child,
                int                             childIndex,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex,
                DependencyProperty              dp,
                FrameworkElementFactory         templateRoot)
        {
            EffectiveValueEntry newEntry = new EffectiveValueEntry(dp);
            newEntry.Value = DependencyProperty.UnsetValue;
            if (GetValueFromTemplatedParent(
                    container,
                    childIndex,
                    child,
                    dp,
                ref childRecordFromChildIndex,
                    templateRoot,
                ref newEntry))

            {
                DependencyObject target = child.DO;
                target.UpdateEffectiveValue(
                        target.LookupEntry(dp.GlobalIndex),
                        dp,
                        dp.GetMetadata(target.DependencyObjectType),
                        new EffectiveValueEntry() /* oldEntry */,
                    ref newEntry,
                        false /* coerceWithDeferredReference */,
                        false /* coerceWithCurrentValue */,
                        OperationType.Unknown);
            }
        }

        // determine whether the template declares the given property's value
        // via a dynamic construct - DynamicResource, TemplateBinding, Binding
        internal static bool IsValueDynamic(
                DependencyObject container,
                int childIndex,
                DependencyProperty dp)
        {
            bool isValueDynamic = false;

            FrameworkObject foContainer = new FrameworkObject(container);
            FrameworkTemplate frameworkTemplate = foContainer.TemplateInternal;
            if (frameworkTemplate != null)
            {
                var childRecordFromChildIndex = frameworkTemplate.ChildRecordFromChildIndex;

                // Check if this Child Index is represented in given data-structure
                if ((0 <= childIndex) && (childIndex < childRecordFromChildIndex.Count))
                {
                    // Fetch the childRecord for the given childIndex
                    ChildRecord childRecord = childRecordFromChildIndex[childIndex];

                    // Check if this Property is represented in the childRecord
                    int mapIndex = childRecord.ValueLookupListFromProperty.Search(dp.GlobalIndex);
                    if (mapIndex >= 0)
                    {
                        if (childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value.Count > 0)
                        {
                            ChildValueLookup childValue = childRecord.ValueLookupListFromProperty.Entries[mapIndex].Value.List[0];
                            isValueDynamic =
                                (childValue.LookupType == ValueLookupType.Resource)         // DynamicResource
                             || (childValue.LookupType == ValueLookupType.TemplateBinding)  // TemplateBinding
                             || (childValue.LookupType == ValueLookupType.Simple &&         // Binding et al.
                                    childValue.Value is BindingBase);
                        }
                    }
                }
            }

            return isValueDynamic;
        }

        internal static bool GetValueFromTemplatedParent(
                DependencyObject                container,
                int                             childIndex,
                FrameworkObject                 child,
                DependencyProperty              dp,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex,
                FrameworkElementFactory         templateRoot,
            ref EffectiveValueEntry             entry)
        {
            ValueLookupType sourceType = ValueLookupType.Simple;
            // entry will be updated to hold the value -- we only need to set the value source
            object value = StyleHelper.GetChildValue(
                StyleHelper.TemplateDataField,
                container,
                childIndex,
                child,
                dp,
                ref childRecordFromChildIndex,
                ref entry,
                out sourceType,
                templateRoot);

            if (value != DependencyProperty.UnsetValue)
            {
                if (sourceType == ValueLookupType.Trigger ||
                    sourceType == ValueLookupType.PropertyTriggerResource ||
                    sourceType == ValueLookupType.DataTrigger ||
                    sourceType == ValueLookupType.DataTriggerResource)
                {
                    entry.BaseValueSourceInternal = BaseValueSourceInternal.ParentTemplateTrigger;
                }
                else
                {
                    entry.BaseValueSourceInternal = BaseValueSourceInternal.ParentTemplate;
                }

                return true;
            }
            else
            {
                // If we didn't get a value from GetValueFromTemplatedParent, we know that
                // the template isn't offering a value from a trigger or from its shared
                // value table.  But we could still have a value from the template, stored
                // in per-instance storage (e.g. a Freezable with an embedded dynamic binding).

                if (child.StoresParentTemplateValues)
                {
                    HybridDictionary parentTemplateValues = StyleHelper.ParentTemplateValuesField.GetValue(child.DO);
                    if(parentTemplateValues.Contains(dp))
                    {
                        entry.BaseValueSourceInternal = BaseValueSourceInternal.ParentTemplate;
                        value = parentTemplateValues[dp];
                        entry.Value = value;

                        if (value is MarkupExtension)
                        {
                            // entry will be updated to hold the value
                            StyleHelper.GetInstanceValue(
                                        StyleHelper.TemplateDataField,
                                        container,
                                        child.FE,
                                        child.FCE,
                                        childIndex,
                                        dp,
                                        StyleHelper.UnsharedTemplateContentPropertyIndex,
                                        ref entry);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        internal static void ApplyStyleOrTemplateValue(
                FrameworkObject fo,
                DependencyProperty dp)
        {
            EffectiveValueEntry newEntry = new EffectiveValueEntry(dp);
            newEntry.Value = DependencyProperty.UnsetValue;
            if (GetValueFromStyleOrTemplate(fo, dp, ref newEntry))
            {
                DependencyObject target = fo.DO;
                target.UpdateEffectiveValue(
                      target.LookupEntry(dp.GlobalIndex),
                      dp,
                      dp.GetMetadata(target.DependencyObjectType),
                      new EffectiveValueEntry() /* oldEntry */,
                      ref newEntry,
                      false /* coerceWithDeferredReference */,
                      false /* coerceWithCurrentValue */,
                      OperationType.Unknown);
            }
        }

        internal static bool GetValueFromStyleOrTemplate(
                FrameworkObject fo,
                DependencyProperty dp,
            ref EffectiveValueEntry entry)
        {
            ValueLookupType sourceType = ValueLookupType.Simple;

            // setterValue & setterEntry are used to record the result of a style setter,
            // so that if a higher-priority template trigger does not apply, we can use
            // this value
            object setterValue = DependencyProperty.UnsetValue;
            EffectiveValueEntry setterEntry = entry;
            object value;

            // Regardless of metadata, the Style property is never stylable on "Self"
            Style style = fo.Style;
            if ((style != null) && StyleHelper.ShouldGetValueFromStyle(dp))
            {
                // Get value from Style Triggers/Storyboards

                // This is a styled container, check for style-driven value
                // Container's use child index '0' (meaning "self")

                // This is the 'container' and the 'child'
                // entry will be updated to hold the value -- we only need to set the value source
                value = StyleHelper.GetChildValue(
                    StyleHelper.StyleDataField,
                    fo.DO,
                    0,
                    fo,
                    dp,
                    ref style.ChildRecordFromChildIndex,
                    ref setterEntry,
                    out sourceType,
                    null);

                if (value != DependencyProperty.UnsetValue)
                {
                    if (sourceType == ValueLookupType.Trigger ||
                        sourceType == ValueLookupType.PropertyTriggerResource ||
                        sourceType == ValueLookupType.DataTrigger ||
                        sourceType == ValueLookupType.DataTriggerResource)
                    {
                        entry = setterEntry;
                        entry.BaseValueSourceInternal = BaseValueSourceInternal.StyleTrigger;
                        return true;
                    }
                    else
                    {
                        Debug.Assert(sourceType == ValueLookupType.Simple ||
                                     sourceType == ValueLookupType.Resource);
                        setterValue = value;
                    }
                }
            }

            // Get value from Template Triggers/Storyboards
            if (StyleHelper.ShouldGetValueFromTemplate(dp))
            {
                FrameworkTemplate template = fo.TemplateInternal;
                if (template != null)
                {
                    // This is a templated container, check for template-driven value
                    // Container's use child index '0' (meaning "self")

                    // This is the 'container' and the 'child'
                    // entry will be updated to hold the value -- we only need to set the value source
                    value = StyleHelper.GetChildValue(
                        StyleHelper.TemplateDataField,
                        fo.DO,
                        0,
                        fo,
                        dp,
                        ref template.ChildRecordFromChildIndex,
                        ref entry,
                        out sourceType,
                        template.VisualTree);
                    if (value != DependencyProperty.UnsetValue)
                    {
                        Debug.Assert(sourceType == ValueLookupType.Trigger ||
                                     sourceType == ValueLookupType.PropertyTriggerResource ||
                                     sourceType == ValueLookupType.DataTrigger ||
                                     sourceType == ValueLookupType.DataTriggerResource);

                        entry.BaseValueSourceInternal = BaseValueSourceInternal.TemplateTrigger;
                        return true;
                    }
                }
            }

            // Get value from Style Setters
            if (setterValue != DependencyProperty.UnsetValue)
            {
                entry = setterEntry;
                entry.BaseValueSourceInternal = BaseValueSourceInternal.Style;
                return true;
            }

            // Get value from ThemeStyle Triggers/Storyboards/Setters
            // Note that this condition assumes that DefaultStyleKey cannot be set on a ThemeStyle
            if (StyleHelper.ShouldGetValueFromThemeStyle(dp))
            {
                Style themeStyle = fo.ThemeStyle;

                if (themeStyle != null)
                {
                    // The desired property value could not be found in the self style,
                    // check if the theme style has a value for it
                    // Container's use child index '0' (meaning "self")

                    // This is the 'container' and the 'child'
                    // entry will be updated to hold the value -- we only need to set the value source
                    value = StyleHelper.GetChildValue(
                        StyleHelper.ThemeStyleDataField,
                        fo.DO,
                        0,
                        fo,
                        dp,
                        ref themeStyle.ChildRecordFromChildIndex,
                        ref entry,
                        out sourceType,
                        null);
                    if (value != DependencyProperty.UnsetValue)
                    {
                        if (sourceType == ValueLookupType.Trigger ||
                            sourceType == ValueLookupType.PropertyTriggerResource ||
                            sourceType == ValueLookupType.DataTrigger ||
                            sourceType == ValueLookupType.DataTriggerResource)
                        {
                            entry.BaseValueSourceInternal = BaseValueSourceInternal.ThemeStyleTrigger;
                        }
                        else
                        {
                            Debug.Assert(sourceType == ValueLookupType.Simple ||
                                         sourceType == ValueLookupType.Resource);
                            entry.BaseValueSourceInternal = BaseValueSourceInternal.ThemeStyle;
                        }

                        return true;
                    }
                }
            }
            return false;
        }



        //
        //  This method
        //  1. Sorts a resource dependent list by (childIndex, dp.GlobalIndex).
        //  This helps to avoid duplicate invalidation.
        //
        internal static void SortResourceDependents(
            ref FrugalStructList<ChildPropertyDependent> resourceDependents)
        {
            // Ideally this would be done by having the ChildPropertyDependent
            // struct implement IComparable<ChildPropertyDependent>, and just
            // calling resourceDependents.Sort().  Unfortunately, this causes
            // an unwelcome JIT of mscorlib, to load internal methods
            // GenericArraySortHelper<T>.Sort and GenericArraySortHelper<T>.QuickSort.
            //
            // Instead we implement sort directly.  The resource dependent lists
            // are short and nearly-sorted in practice, so insertion sort is good
            // enough.

            int n = resourceDependents.Count;
            for (int i=1; i<n; ++i)
            {
                ChildPropertyDependent current = resourceDependents[i];
                int childIndex = current.ChildIndex;
                int dpIndex = current.Property.GlobalIndex;

                int j;
                for (j=i-1;  j>=0;  --j)
                {
                    if (childIndex < resourceDependents[j].ChildIndex ||
                        (childIndex == resourceDependents[j].ChildIndex &&
                         dpIndex < resourceDependents[j].Property.GlobalIndex))
                    {
                        resourceDependents[j+1] = resourceDependents[j];
                    }
                    else
                    {
                        break;
                    }
                }

                if (j < i-1)
                {
                    resourceDependents[j+1] = current;
                }
            }
        }

        //
        //  This method
        //  1. Invalidates all the resource references set on a style or a template.
        //
        //  Note: In the case that the visualtree was not generated from the particular
        //  style in question we will skip past those resource references that haven't
        //  been set on the container. This condition is described by the
        //  invalidateVisualTreeToo flag being false.
        //
        internal static void InvalidateResourceDependents(
            DependencyObject                             container,
            ResourcesChangeInfo                          info,
            ref FrugalStructList<ChildPropertyDependent> resourceDependents,
            bool                                         invalidateVisualTreeToo)
        {
            List<DependencyObject> styledChildren = TemplatedFeChildrenField.GetValue(container);

            // Invalidate all properties on this container and its children that
            // are being driven via a resource reference in a style
            for (int i = 0; i < resourceDependents.Count; i++)
            {
                // Invalidate property
                //  1. If nothing is known about the data or
                //  2. If the data tells us the key in the dictionary that was modified and this property is refering to it or
                //  3. If it tells us info about the changed dictionaries and this property was refering to one of their entries
                //  4. If this a theme change
                if (info.Contains(resourceDependents[i].Name, false /*isImplicitStyleKey*/))
                {
                    DependencyObject child = null;
                    DependencyProperty invalidProperty = resourceDependents[i].Property;

                    int childIndex = resourceDependents[i].ChildIndex;
                    if (childIndex == 0)
                    {
                        // Index '0' means 'self' (container)
                        child = container;
                    }
                    else if (invalidateVisualTreeToo)
                    {
                        Debug.Assert(styledChildren != null, "Should reach here only if the template tree has already been created");

                        // Locate child to invalidate
                        child = GetChild(styledChildren, childIndex);

                        if (child == null)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.ChildTemplateInstanceDoesNotExist));
                        }
                    }

                    if (child != null)
                    {
                        // Invalidate property on child
                        child.InvalidateProperty(invalidProperty);

                        // skip remaining dependents for the same property - we only
                        // need to invalidate once.  The list is sorted, so we just need
                        // to skip until we find a new property.
                        int dpIndex = invalidProperty.GlobalIndex;
                        while (++i < resourceDependents.Count)
                        {
                            if (resourceDependents[i].ChildIndex != childIndex ||
                                resourceDependents[i].Property.GlobalIndex != dpIndex)
                            {
                                break;
                            }
                        }
                        --i;    // back up to let the for-loop do its normal increment
                    }
                }
            }
        }

        //
        //  This method
        //  1. Invalidates all the resource references set on a template for a given child.
        //  2. Returns true if any were found
        //
        internal static void InvalidateResourceDependentsForChild(
                DependencyObject                            container,
                DependencyObject                            child,
                int                                         childIndex,
                ResourcesChangeInfo                         info,
                FrameworkTemplate                           parentTemplate)
        {
            FrugalStructList<ChildPropertyDependent> resourceDependents = parentTemplate.ResourceDependents;
            int count = resourceDependents.Count;

            // Invalidate all properties on the given child that
            // are being driven via a resource reference in a template
            for (int i = 0; i < count; i++)
            {
                if (resourceDependents[i].ChildIndex == childIndex &&
                    info.Contains(resourceDependents[i].Name, false /*isImplicitStyleKey*/))
                {
                    DependencyProperty dp = resourceDependents[i].Property;
                    // Update property on child
                    child.InvalidateProperty(dp);

                    // skip remaining dependents for the same property - we only
                    // need to invalidate once.  The list is sorted, so we just need
                    // to skip until we find a new property.
                    int dpIndex = dp.GlobalIndex;
                    while (++i < resourceDependents.Count)
                    {
                        if (resourceDependents[i].ChildIndex != childIndex ||
                            resourceDependents[i].Property.GlobalIndex != dpIndex)
                        {
                            break;
                        }
                    }
                    --i;    // back up to let the for-loop do its normal increment
                }
            }
        }

        //
        //  This method
        //  1. Returns true if any resource references are set on a template for a given child.
        //
        internal static bool HasResourceDependentsForChild(
            int                                          childIndex,
            ref FrugalStructList<ChildPropertyDependent> resourceDependents)
        {
            // Look for properties on the given child that
            // are being driven via a resource reference in a template
            for (int i = 0; i < resourceDependents.Count; i++)
            {
                if (resourceDependents[i].ChildIndex == childIndex)
                {
                    return true;
                }
            }

            return false;
        }

        //
        //  This method
        //  1. Invalidates properties set on a TemplateNode directly or via a Trigger
        //
        internal static void InvalidatePropertiesOnTemplateNode(
                DependencyObject                container,
                FrameworkObject                 child,
                int                             childIndex,
            ref FrugalStructList<ChildRecord>   childRecordFromChildIndex,
                bool                            isDetach,
                FrameworkElementFactory         templateRoot)
        {
            Debug.Assert(child.FE != null || child.FCE != null);

            // Check if this Child Index is represented in given data-structure
            if ((0 <= childIndex) && (childIndex < childRecordFromChildIndex.Count))
            {
                // Fetch the childRecord for the given childIndex
                ChildRecord childRecord = childRecordFromChildIndex[childIndex];
                int count = childRecord.ValueLookupListFromProperty.Count;
                if (count > 0)
                {
                    // Iterate through all the properties set on the given childRecord
                    for (int i=0; i< count; i++)
                    {
                        // NOTE: Every entry in the ValueLookupListFromProperty corresponds to
                        // one DependencyProperty. All the items in Entries[i].Value.List
                        // represent values for the same DependencyProperty that might have
                        // originated from different sources such as a direct property set or a
                        // Trigger or a Storyboard value.
                        DependencyProperty dp = childRecord.ValueLookupListFromProperty.Entries[i].Value.List[0].Property;
                        Debug.Assert(dp != null, "dp must not be null");

                        if (!isDetach)
                        {
                            ApplyTemplatedParentValue(
                                    container,
                                    child,
                                    childIndex,
                                ref childRecordFromChildIndex,
                                    dp,
                                    templateRoot);
                        }
                        else
                        {
                            // for the detach case, we can skip inherited properties
                            // see comment in ClearTemplateChain

                            // Invalidate only the non-inherited, non-style properties.
                            // Note that I say non-style because StyleProperty is really
                            // a psuedo inherited property, which gets specially handled
                            // during an InvalidateTree call.
                            if (dp != FrameworkElement.StyleProperty)
                            {
                                bool invalidate = true;

                                if (dp.IsPotentiallyInherited)
                                {
                                    PropertyMetadata metadata = dp.GetMetadata(child.DO.DependencyObjectType);
                                    if ((metadata != null) && metadata.IsInherited)
                                    {
                                        invalidate = false;
                                    }
                                }

                                if (invalidate)
                                {
                                    child.DO.InvalidateProperty(dp);
                                }
                            }
                        }
                    }
                }
            }
        }

        //
        //  This method
        //  1. Says if the given DP a part of the given ContainerDependents
        //  2. Is used to skip properties while invalidating the inherited properties for an
        //     ancestor change. If this method returns true the invalidation will be skipped.
        //     If this DP has been set on the container this value will take precedence over the inherited
        //     value. Hence there is no need to invalidate this property as part of ancestor change processing.
        //
        //  Ancestor changed invalidation for this DP can be skipped if it
        //  - Is in the give ContainerDependents list but
        //  - Is not the result of visual trigger
        //  NOTE: If the style has changed all container dependents including the ones originating
        //  from visual triggers would have been invalidated. Hence they can all be skipped.
        //
        internal static bool IsSetOnContainer(
            DependencyProperty                       dp,
            ref FrugalStructList<ContainerDependent> containerDependents,
            bool                                     alsoFromTriggers)
        {
            for (int i = 0; i < containerDependents.Count; i++)
            {
                if (dp == containerDependents[i].Property)
                {
                    return alsoFromTriggers || !containerDependents[i].FromVisualTrigger;
                }
            }
            return false;
        }

        //
        //  This method
        //  1. Is invoked when Styled/Templated container property invalidation
        //     is propagated to its dependent properties from Style/Template.
        //
        internal static void OnTriggerSourcePropertyInvalidated(
            Style                                                       ownerStyle,
            FrameworkTemplate                                           frameworkTemplate,
            DependencyObject                                            container,
            DependencyProperty                                          dp,
            DependencyPropertyChangedEventArgs                          changedArgs,
            bool                                                        invalidateOnlyContainer,
            ref FrugalStructList<ItemStructMap<TriggerSourceRecord>>    triggerSourceRecordFromChildIndex,
            ref FrugalMap                                               propertyTriggersWithActions,
            int                                                         sourceChildIndex)
        {
            Debug.Assert(ownerStyle != null || frameworkTemplate != null );

            ///////////////////////////////////////////////////////////////////
            // Update all values affected by property trigger Setters

            // Check if this Child Index is represented in given data-structure
            if ((0 <= sourceChildIndex) && (sourceChildIndex < triggerSourceRecordFromChildIndex.Count))
            {
                // Fetch the triggerSourceRecordMap for the given childIndex
                ItemStructMap<TriggerSourceRecord> triggerSourceRecordMap = triggerSourceRecordFromChildIndex[sourceChildIndex];

                // Check if this Container property is represented in style
                int mapIndex = triggerSourceRecordMap.Search(dp.GlobalIndex);
                if (mapIndex >= 0)
                {
                    // Container's property is represented in style
                    TriggerSourceRecord record = triggerSourceRecordMap.Entries[mapIndex].Value;

                    // Invalidate all Self/Child-Index/Property dependents
                    InvalidateDependents(ownerStyle, frameworkTemplate, container, dp,
                        ref record.ChildPropertyDependents, invalidateOnlyContainer);
                }
            }

            ///////////////////////////////////////////////////////////////////
            // Find all TriggerActions that may need to execute in response to
            //  the property change.
            object candidateTrigger = propertyTriggersWithActions[dp.GlobalIndex];

            if( candidateTrigger != DependencyProperty.UnsetValue )
            {
                // One or more trigger objects need to be evaluated.  The candidateTrigger
                //  object may be a single trigger or a collection of them.

                TriggerBase triggerBase = candidateTrigger as TriggerBase;
                if( triggerBase != null )
                {
                    InvokePropertyTriggerActions( triggerBase, container, dp, changedArgs, sourceChildIndex,
                        ownerStyle, frameworkTemplate );
                }
                else
                {
                    Debug.Assert(candidateTrigger is List<TriggerBase>, "Internal data structure error: The FrugalMap [Style/Template].PropertyTriggersWithActions " +
                        "is expected to hold a single TriggerBase or a List<T> of them.  An object of type " +
                        candidateTrigger.GetType().ToString() + " is not expected.  Where did this object come from?");

                    List<TriggerBase> triggerList = (List<TriggerBase>)candidateTrigger;

                    for( int i = 0; i < triggerList.Count; i++ )
                    {
                        InvokePropertyTriggerActions( triggerList[i], container, dp, changedArgs, sourceChildIndex,
                            ownerStyle, frameworkTemplate );
                    }
                }
            }
        }


        //
        //  This method
        //  1. Is common code to invalidate a list of dependents of a property trigger
        //     or data trigger. Returns true if any of the dependents could not
        //     be invalidated because they don't exist yet.
        //
        private static void InvalidateDependents(
                Style                                    ownerStyle,
                FrameworkTemplate                        frameworkTemplate,
                DependencyObject                         container,
                DependencyProperty                       dp,
            ref FrugalStructList<ChildPropertyDependent> dependents,
                bool                                     invalidateOnlyContainer)
        {
            Debug.Assert(ownerStyle != null || frameworkTemplate != null );

            for (int i = 0; i < dependents.Count; i++)
            {
                DependencyObject child = null;

                int childIndex = dependents[i].ChildIndex;
                if (childIndex == 0)
                {
                    // Index '0' means 'self' (container)
                    child = container;
                }
                else if (!invalidateOnlyContainer)
                {
                    // Locate child to invalidate

                    // This assumes that at least one node in the
                    //  Style.VisualTree is in the child chain, to guarantee
                    //  this, the root node is always in the child chain.
                    List<DependencyObject> styledChildren = TemplatedFeChildrenField.GetValue(container);
                    if ((styledChildren != null) && (childIndex <= styledChildren.Count))
                    {
                        child = GetChild(styledChildren, childIndex);

                        // Notice that we allow GetChildValue to return null because it
                        // could so happen that the dependent properties for the current
                        // trigger source are on nodes that haven't been instantiated yet.
                        // We do not have to bother about deferring these invalidations
                        // because InvalidatePropertiesOnTemplateNode will take care of
                        // these invalidations when the node is instantiated via
                        // FrameworkElementFactory.InstantiateTree.
                    }
                }

                // Invalidate property on child
                DependencyProperty invalidProperty = dependents[i].Property;

                // Invalidate only if the property is not locally set because local value
                // has precedence over style acquired value.
                bool hasModifiers;
                if (child != null &&
                    child.GetValueSource(invalidProperty, null, out hasModifiers) != BaseValueSourceInternal.Local)
                {
                    child.InvalidateProperty(invalidProperty, preserveCurrentValue:true);
//                    ApplyStyleOrTemplateValue(new FrameworkObject(child), invalidProperty);
                }
            }
        }

        // This is expected to be called when a Binding for a DataTrigger has
        //   changed and we need to look for true/false transitions.  If found,
        //   invoke EnterAction (or ExitAction) as needed.
        private static void InvokeDataTriggerActions( TriggerBase triggerBase,
            DependencyObject triggerContainer, BindingBase binding, BindingValueChangedEventArgs bindingChangedArgs,
            Style style, FrameworkTemplate frameworkTemplate,
            UncommonField<HybridDictionary[]> dataField)
        {
            bool oldState;
            bool newState;

            DataTrigger dataTrigger = triggerBase as DataTrigger;

            if( dataTrigger != null )
            {
                EvaluateOldNewStates( dataTrigger, triggerContainer,
                    binding, bindingChangedArgs, dataField,
                    style, frameworkTemplate,
                    out oldState, out newState );
            }
            else
            {
                Debug.Assert( triggerBase is MultiDataTrigger,
                    "This method only handles DataTrigger and MultiDataTrigger.  Other types should have been handled elsewhere." );

                EvaluateOldNewStates( (MultiDataTrigger)triggerBase, triggerContainer,
                    binding, bindingChangedArgs, dataField,
                    style, frameworkTemplate,
                    out oldState, out newState );
            }

            InvokeEnterOrExitActions( triggerBase, oldState, newState, triggerContainer,
                style, frameworkTemplate  );
        }

        // This is expected to be called when a property value has changed and that
        //  property is specified as a condition in a trigger.
        // We're given the trigger here, the property changed, and the before/after state.
        // Evaluate the Trigger, and see if we need to invoke any of the TriggerAction
        //  objects associated with the given trigger.
        private static void InvokePropertyTriggerActions( TriggerBase triggerBase,
            DependencyObject triggerContainer, DependencyProperty changedProperty,
            DependencyPropertyChangedEventArgs changedArgs, int sourceChildIndex,
            Style style, FrameworkTemplate frameworkTemplate )
        {
            bool oldState;
            bool newState;

            Trigger trigger = triggerBase as Trigger;

            if( trigger != null )
            {
                EvaluateOldNewStates( trigger, triggerContainer, changedProperty, changedArgs,
                    sourceChildIndex, style, frameworkTemplate,
                    out oldState, out newState );
            }
            else
            {
                Debug.Assert( triggerBase is MultiTrigger,
                    "This method only handles Trigger and MultiTrigger.  Other types should have been handled elsewhere." );

                EvaluateOldNewStates( (MultiTrigger)triggerBase, triggerContainer, changedProperty, changedArgs,
                    sourceChildIndex, style, frameworkTemplate,
                    out oldState, out newState );
            }

            InvokeEnterOrExitActions( triggerBase, oldState, newState, triggerContainer,
                style, frameworkTemplate );
        }

        // Called from UpdateStyleCache - When an object's Style changes, some of
        //  the triggers contain EnterActions and they want to be run immediately
        //  if the trigger condition evaluates to true.
        // Usually these EnterActions are to be run when there's a False->True
        //  transition.  This code treats "true at time Style is applied" as
        //  a False->True transition even though it's possible no transition ever
        //  took place.
        private static void ExecuteOnApplyEnterExitActions( FrameworkElement fe,
            FrameworkContentElement fce, Style style, UncommonField<HybridDictionary[]> dataField )
        {
            // If the "Style Change" is the style being set to null - exit.
            if( style == null )
            {
                return;
            }
            // Note: PropertyTriggersWithActions is a FrugalMap, so its count is checked against zero.
            //  DataTriggersWithActions is a HybridDictionary allocated on demand, so it's checked against null.
            if( style.PropertyTriggersWithActions.Count == 0 && style.DataTriggersWithActions == null )
            {
                // Style has no trigger actions at all, exit.
                return;
            }

            TriggerCollection triggers = style.Triggers;
            DependencyObject triggerContainer = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

            ExecuteOnApplyEnterExitActionsLoop( triggerContainer, triggers, style, null, dataField );
        }

        // Called from UpdateStyleCache - When an object's Template changes, some of
        //  the triggers contain EnterActions and they want to be run immediately
        //  if the trigger condition evaluates to true.
        // Usually these EnterActions are to be run when there's a False->True
        //  transition.  This code treats "true at time Template is applied" as
        //  a False->True transition even though it's possible no transition ever
        //  took place.
        private static void ExecuteOnApplyEnterExitActions( FrameworkElement fe, FrameworkContentElement fce,
            FrameworkTemplate ft )
        {
            // If the "Template Change" is a template being set to null - exit.
            if( ft == null )
            {
                return;
            }
            // Note: PropertyTriggersWithActions is a FrugalMap, so its count is checked against zero.
            //  DataTriggersWithActions is a HybridDictionary allocated on demand, so it's checked against null.
            if( ft != null && ft.PropertyTriggersWithActions.Count == 0 && ft.DataTriggersWithActions == null )
            {
                // FrameworkTemplate has no trigger actions at all, exit.
                return;
            }

            Debug.Assert( ft != null );
            TriggerCollection triggers = ft.TriggersInternal;
            DependencyObject triggerContainer = (fe != null) ? (DependencyObject)fe : (DependencyObject)fce;

            ExecuteOnApplyEnterExitActionsLoop( triggerContainer, triggers, null, ft, TemplateDataField );
        }

        // Called from either the Style-specific ExecuteOnApplyEnterActions or the
        //  Template-specific version.  This section is the common code for both that
        //  walks through the trigger collection and execute applicable actions.
        private static void ExecuteOnApplyEnterExitActionsLoop( DependencyObject triggerContainer, TriggerCollection triggers,
            Style style, FrameworkTemplate ft, UncommonField<HybridDictionary[]> dataField )
        {
            TriggerBase triggerBase;
            bool triggerState;
            for( int i = 0; i < triggers.Count; i++ )
            {
                triggerBase = triggers[i];
                if( (!triggerBase.HasEnterActions) && (!triggerBase.HasExitActions) )
                {
                    ; // Trigger has neither enter nor exit actions.  There's nothing to run anyway, so skip.
                }
                else if( triggerBase.ExecuteEnterActionsOnApply ||
                    triggerBase.ExecuteExitActionsOnApply )
                {
                    // Look for any SourceName in the condition
                    if( NoSourceNameInTrigger( triggerBase ) )
                    {
                        // Evaluate the current state of the trigger.
                        triggerState = triggerBase.GetCurrentState( triggerContainer, dataField );

                        if( triggerState && triggerBase.ExecuteEnterActionsOnApply )
                        {
                            // Trigger is true, and Trigger wants EnterActions to be executed on Style/Template application.
                            InvokeActions( triggerBase.EnterActions, triggerBase, triggerContainer,
                                style, ft );
                        }
                        else if( !triggerState && triggerBase.ExecuteExitActionsOnApply )
                        {
                            // Trigger is false, and Trigger wants ExitActions to be executed on Style/Template application.
                            InvokeActions( triggerBase.ExitActions, triggerBase, triggerContainer,
                                style, ft );
                        }
                    }
                    else
                    {
                        // If one or more conditions are dependent on a template
                        //  child, then it can't possibly apply immediately.
                    }
                }
            }
        }

        // Used by ExecuteOnApplyEnterExitActionsLoop to determine whether the
        //  particular trigger is dependent on any child nodes, by checking for
        //  a SourceName string in the trigger.
        // We only check the two property trigger types here, data triggers
        //  do not support being dependent on child nodes.
        private static bool NoSourceNameInTrigger( TriggerBase triggerBase )
        {
            Trigger trigger = triggerBase as Trigger;
            if( trigger != null )
            {
                if( trigger.SourceName == null )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                MultiTrigger multiTrigger = triggerBase as MultiTrigger;
                if( multiTrigger != null )
                {
                    for( int i = 0; i < multiTrigger.Conditions.Count; i++ )
                    {
                        if( multiTrigger.Conditions[i].SourceName != null )
                        {
                            return false;
                        }
                    }

                    // Ran through all the conditions - not a single SourceName was found.
                    return true;
                }
            }

            // DataTrigger and MultiDataTrigger doesn't allow SourceName - so it's true that they have no SourceName.
            return true;
        }

        // Helper method shared between property trigger and data trigger.  After
        //  the trigger's old and new states are evaluated, look at them and see
        //  if we should invoke the associated EnterActions or ExitActions.
        private static void InvokeEnterOrExitActions( TriggerBase triggerBase,
            bool oldState, bool newState, DependencyObject triggerContainer,
            Style style, FrameworkTemplate frameworkTemplate)
        {
            TriggerActionCollection actions;

            if( !oldState && newState )
            {
                // False -> True transition.  Execute EnterActions.
                actions = triggerBase.EnterActions;
            }
            else if( oldState && !newState )
            {
                // True -> False transition.  Execute ExitActions.
                actions = triggerBase.ExitActions;
            }
            else
            {
                actions = null;
            }

            InvokeActions( actions, triggerBase, triggerContainer, style, frameworkTemplate );
        }

        // Called from InvokeEnterOrExitActions in response to a changed event, or
        //  from ExecuteOnApplyEnterExitActionsLoop when Style/Template is initially
        //  applied.
        // At this point we've decided that the given set of trigger action collections
        //  should be run now.  This method checks to see if that's actually possible
        //  and either invokes immediately or saves enough information to invoke later.
        private static void InvokeActions( TriggerActionCollection actions,
            TriggerBase triggerBase, DependencyObject triggerContainer,
            Style style, FrameworkTemplate frameworkTemplate )
        {
            if( actions != null )
            {
                // See CanInvokeActionsNow for all the (known) reasons why we might not be able to
                //  invoke immediately.
                if( CanInvokeActionsNow( triggerContainer, frameworkTemplate ) )
                {
                    InvokeActions( triggerBase, triggerContainer, actions,
                        style, frameworkTemplate );
                }
                else
                {
                    DeferActions( triggerBase, triggerContainer, actions,
                        style, frameworkTemplate );
                }
            }
        }

        // This function holds the knowledge about whether InvokeEnterOrExitActions
        //  should do an immediate invoke of the applicable actions.

        // We check against HasTemplatedGeneratedSubTree instead of TemplatedChildrenField
        //  because the TemplatedChildrenField is non-null when the child nodes have
        //  been generated - but their EffectiveValues cache haven't necessarily picked
        //  up all their templated values yet.  HasTemplatedGeneratedSubTree is set
        //  to true only after all the property values have been updated.
        private static bool CanInvokeActionsNow( DependencyObject container,
            FrameworkTemplate frameworkTemplate)
        {
            bool result;

            if( frameworkTemplate != null )
            {
                FrameworkElement fe = (FrameworkElement)container;
                if( fe.HasTemplateGeneratedSubTree )
                {
                    ContentPresenter cp = container as ContentPresenter;

                    if( cp != null && !cp.TemplateIsCurrent )
                    {
                        // The containing ContentPresenter does not have an
                        //  up-to-date template.  If we run now we'll run
                        //  against the wrong thing, so we need to hold off.
                        result = false;
                    }
                    else
                    {
                        // The containing element has the template sub tree ready to go.
                        //  we can launch the action now.
                        result = true;
                    }
                }
                else
                {
                    // The template generated sub tree isn't ready yet.  Hold off.
                    result = false;
                }
            }
            else
            {
                // Today there is no reason why we can't invoke actions
                //  immediately if we're not dealing with a template.
                result = true;
            }

            return result;
        }

        // In the event that we can't do an immediate invoke of the collection
        //  of actions, add this to the list of deferred actions that is stored
        //  on the template object.  Because each template can be applicable to
        //  multiple objects, the storage of deferred actions is keyed by the
        //  triggerContainer instance.
        private static void DeferActions( TriggerBase triggerBase,
            DependencyObject triggerContainer, TriggerActionCollection actions,
            Style style, FrameworkTemplate frameworkTemplate)

        {
            ConditionalWeakTable<DependencyObject, List<DeferredAction>> deferredActions;
            DeferredAction deferredAction; // struct
            deferredAction.TriggerBase = triggerBase;
            deferredAction.TriggerActionCollection = actions;

            if( frameworkTemplate != null )
            {
                deferredActions = frameworkTemplate.DeferredActions;

                if( deferredActions == null )
                {
                    deferredActions = new ConditionalWeakTable<DependencyObject, List<DeferredAction>>();
                    frameworkTemplate.DeferredActions = deferredActions;
                }
            }
            else
            {
                // Nothing - deferring actions only happen for FrameworkTemplate
                //  scenarios, so deferred actions is empty.
                deferredActions = null;
            }

            if( deferredActions != null )
            {
                List<DeferredAction> actionList;

                if( !deferredActions.TryGetValue(triggerContainer, out actionList) )
                {
                    actionList = new List<DeferredAction>();
                    deferredActions.Add(/* key */triggerContainer,/* value */actionList);
                }

                actionList.Add(deferredAction);
            }
        }

        // Execute any actions we stored away in the method DeferActions, and
        //  clear out the store for these actions.
        internal static void InvokeDeferredActions( DependencyObject triggerContainer,
            FrameworkTemplate frameworkTemplate )
        {
            // See if we have a list of deferred actions to execute.
            if (frameworkTemplate != null && frameworkTemplate.DeferredActions != null)
            {
                List<DeferredAction> actionList;
                if (frameworkTemplate.DeferredActions.TryGetValue(triggerContainer, out actionList))
                {
                    // Execute any actions found.
                    for (int i = 0; i < actionList.Count; i++)
                    {
                        InvokeActions(actionList[i].TriggerBase,
                            triggerContainer, actionList[i].TriggerActionCollection,
                            null, frameworkTemplate);
                    }

                    // Now that we've run them, remove the list of deferred actions.
                    frameworkTemplate.DeferredActions.Remove(triggerContainer);
                }
            }
        }

        // Given a list of action collection, invoke all individual TriggerAction
        //  in that collection using the rest of the information given.
        internal static void InvokeActions( TriggerBase triggerBase,
            DependencyObject triggerContainer, TriggerActionCollection actions,
            Style style, FrameworkTemplate frameworkTemplate)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                TriggerAction action = actions[i];

                Debug.Assert(!(action.ContainingTrigger is EventTrigger),
                    "This trigger actions list used by this method are expected to contain only those actions under non-event triggers.");

                action.Invoke(triggerContainer as FrameworkElement, triggerContainer as FrameworkContentElement,
                    style, frameworkTemplate, triggerBase.Layer);
            }
        }

        // Given a single property trigger and associated context information,
        //  evaluate the old and new states of the trigger.
        private static void EvaluateOldNewStates( Trigger trigger,
            DependencyObject triggerContainer, DependencyProperty changedProperty, DependencyPropertyChangedEventArgs changedArgs,
            int sourceChildIndex, Style style, FrameworkTemplate frameworkTemplate,
            out bool oldState, out bool newState )
        {

            Debug.Assert( trigger.Property == changedProperty,
                "We're trying to evaluate the state of a Trigger based on a property that doesn't affect that trigger.  This is indicative of an error upstream, when setting up the [Style/Template].TriggersWithActions data structure." );

            int triggerChildId = 0;

            if( trigger.SourceName != null )
            {
                Debug.Assert( frameworkTemplate != null,
                    "A trigger outside of a Template is not supposed to have a SourceName.  This should have been caught somewhere upstream, like in Style.Seal().");
                triggerChildId = QueryChildIndexFromChildName(trigger.SourceName, frameworkTemplate.ChildIndexFromChildName);
            }

            if( triggerChildId == sourceChildIndex )
            {
                TriggerCondition[] conditions = trigger.TriggerConditions;
                Debug.Assert( conditions != null && conditions.Length == 1,
                    "This method assumes there is exactly one TriggerCondition." );

                oldState = conditions[0].Match(changedArgs.OldValue);
                newState = conditions[0].Match(changedArgs.NewValue);
            }
            else
            {
                // Property change did not occur on an object we care about -
                //  skip evaluation of property values.  The state values here
                //  are bogus.  This is OK today since we just care about transition
                //  states but if later on it actually matters what the values are
                //  then we need to evaluate state on the actual triggerChildId.
                // (The old/new values would be the same, naturally.)
                oldState = false;
                newState = false;
            }

            return;
        }

        // Given a single data trigger and associated context information,
        //  evaluate the old and new states of the trigger.
        private static void EvaluateOldNewStates( DataTrigger dataTrigger,
            DependencyObject triggerContainer,
            BindingBase binding, BindingValueChangedEventArgs bindingChangedArgs, UncommonField<HybridDictionary[]> dataField,
            Style style, FrameworkTemplate frameworkTemplate,
            out bool oldState, out bool newState )
        {
            TriggerCondition[] conditions = dataTrigger.TriggerConditions;
            Debug.Assert( conditions != null && conditions.Length == 1,
                "This method assumes there is exactly one TriggerCondition." );

            oldState = conditions[0].ConvertAndMatch(bindingChangedArgs.OldValue);
            newState = conditions[0].ConvertAndMatch(bindingChangedArgs.NewValue);
        }

        // Given a multi property trigger and associated context information,
        //  evaluate the old and new states of the trigger.

        // Note that we can only have a transition only if every property other
        //  than the changed property is true.  If any of the other properties
        //  are false, then the state is false and we can't have a false->true
        //  transition.
        // Hence this evaluation short-circuits if any property evaluation
        //  (other than the one being compared) turns out false.
        private static void EvaluateOldNewStates( MultiTrigger multiTrigger,
            DependencyObject triggerContainer, DependencyProperty changedProperty, DependencyPropertyChangedEventArgs changedArgs,
            int sourceChildIndex, Style style, FrameworkTemplate frameworkTemplate,
            out bool oldState, out bool newState )
        {
            int triggerChildId = 0;
            DependencyObject evaluationNode = null;
            TriggerCondition[] conditions = multiTrigger.TriggerConditions;

            // Set up the default condition: A trigger with no conditions will never evaluate to true.
            oldState = false;
            newState = false;

            for( int i = 0; i < conditions.Length; i++ )
            {
                if( conditions[i].SourceChildIndex != 0 )
                {
                    Debug.Assert( frameworkTemplate != null,
                        "A trigger outside of a Template is not supposed to have a SourceName.  This should have been caught somewhere upstream, like in Style.Seal().");
                    triggerChildId = conditions[i].SourceChildIndex;
                    evaluationNode = GetChild(triggerContainer, triggerChildId);
                }
                else
                {
                    triggerChildId = 0;
                    evaluationNode = triggerContainer;
                }

                Debug.Assert(evaluationNode != null,
                    "Couldn't find the node corresponding to the ID and name given in the trigger.  This should have been caught somewhere upstream, like StyleHelper.SealTemplate()." );

                if( conditions[i].Property == changedProperty && triggerChildId == sourceChildIndex )
                {
                    // This is the property that changed, and on the object we
                    //  care about.  Evaluate states.- see if the condition
                    oldState = conditions[i].Match(changedArgs.OldValue);
                    newState = conditions[i].Match(changedArgs.NewValue);

                    if( oldState == newState )
                    {
                        // There couldn't possibly be a transition here, abort.  The
                        //  returned values here aren't necessarily the state of the
                        //  triggers, but we only care about a transition today.  If
                        //  we care about actual values, we'll need to continue evaluation.
                        return;
                    }
                }
                else
                {
                    object evaluationValue = evaluationNode.GetValue( conditions[i].Property );
                    if( !conditions[i].Match(evaluationValue) )
                    {
                        // A condition other than the one changed has evaluated to false.
                        // There couldn't possibly be a transition here, short-circuit and abort.
                        oldState = false;
                        newState = false;
                        return;
                    }
                }
            }

            // We should only get this far only if every property change causes
            //  a true->false (or vice versa) transition in one of the conditions,
            // AND that every other condition evaluated to true.
            return;
        }

        // Given a multi data trigger and associated context information,
        //  evaluate the old and new states of the trigger.

        // The short-circuit logic of multi property trigger applies here too.
        //  we bail if any of the "other" conditions evaluate to false.
        private static void EvaluateOldNewStates( MultiDataTrigger multiDataTrigger,
            DependencyObject triggerContainer,
            BindingBase binding, BindingValueChangedEventArgs changedArgs, UncommonField<HybridDictionary[]> dataField,
            Style style, FrameworkTemplate frameworkTemplate,
            out bool oldState, out bool newState )
        {
            BindingBase evaluationBinding = null;
            object  evaluationValue = null;
            TriggerCondition[] conditions = multiDataTrigger.TriggerConditions;

            // Set up the default condition: A trigger with no conditions will never evaluate to true.
            oldState = false;
            newState = false;

            for( int i = 0; i < multiDataTrigger.Conditions.Count; i++ )
            {
                evaluationBinding = conditions[i].Binding;

                Debug.Assert( evaluationBinding != null,
                    "A null binding was encountered in a MultiDataTrigger conditions collection - this is invalid input and should have been caught earlier.");

                if( evaluationBinding == binding )
                {
                    // The binding that changed belonged to the current condition.
                    oldState = conditions[i].ConvertAndMatch(changedArgs.OldValue);
                    newState = conditions[i].ConvertAndMatch(changedArgs.NewValue);

                    if( oldState == newState )
                    {
                        // There couldn't possibly be a transition here, abort.  The
                        //  returned values here aren't necessarily the state of the
                        //  triggers, but we only care about a transition today.  If
                        //  we care about actual values, we'll need to continue evaluation.
                        return;
                    }
                }
                else
                {
                    evaluationValue = GetDataTriggerValue(dataField, triggerContainer, evaluationBinding);
                    if( !conditions[i].ConvertAndMatch(evaluationValue) )
                    {
                        // A condition other than the one changed has evaluated to false.
                        // There couldn't possibly be a transition here, short-circuit and abort.
                        oldState = false;
                        newState = false;
                        return;
                    }
                }
            }

            // We should only get this far only if the binding change causes
            //  a true->false (or vice versa) transition in one of the conditions,
            // AND that every other condition evaluated to true.
            return;
        }


        // Called during Style/Template Seal when encountering a property trigger that
        //  has associated TriggerActions.
        internal static void AddPropertyTriggerWithAction(TriggerBase triggerBase,
            DependencyProperty property, ref FrugalMap triggersWithActions)
        {
            object existing = triggersWithActions[property.GlobalIndex];

            if( existing == DependencyProperty.UnsetValue )
            {
                // No existing trigger, we put given trigger as entry.
                triggersWithActions[property.GlobalIndex] = triggerBase;
            }
            else
            {
                TriggerBase existingTriggerBase = existing as TriggerBase;
                if( existingTriggerBase != null )
                {
                    List<TriggerBase> newList = new List<TriggerBase>();

                    newList.Add(existingTriggerBase);
                    newList.Add(triggerBase);

                    triggersWithActions[property.GlobalIndex] = newList;
                }
                else
                {
                    Debug.Assert( existing is List<TriggerBase>,
                        "FrugalMap for holding List<TriggerBase> is holding an instance of unexpected type " + existing.GetType() );

                    List<TriggerBase> existingList = (List<TriggerBase>)existing;

                    existingList.Add(triggerBase);
                }
            }

            // Note the order in which we processed this trigger.
            triggerBase.EstablishLayer();
        }

        // Called during Style/Template Seal when encountering a data trigger that
        //  has associated TriggerActions.
        internal static void AddDataTriggerWithAction(TriggerBase triggerBase,
            BindingBase binding, ref HybridDictionary dataTriggersWithActions )
        {
            if( dataTriggersWithActions == null )
            {
                dataTriggersWithActions = new HybridDictionary();
            }

            object existing = dataTriggersWithActions[binding];

            if( existing == null )
            {
                // No existing trigger, we put given trigger as entry.
                dataTriggersWithActions[binding] = triggerBase;
            }
            else
            {
                TriggerBase existingTriggerBase = existing as TriggerBase;
                if( existingTriggerBase != null )
                {
                    // Up-convert to list and replace.

                    List<TriggerBase> newList = new List<TriggerBase>();

                    newList.Add(existingTriggerBase);
                    newList.Add(triggerBase);

                    dataTriggersWithActions[binding] = newList;
                }
                else
                {
                    Debug.Assert( existing is List<TriggerBase>,
                        "HybridDictionary for holding List<TriggerBase> is holding an instance of unexpected type " + existing.GetType() );

                    List<TriggerBase> existingList = (List<TriggerBase>)existing;

                    existingList.Add(triggerBase);
                }
            }

            // Note the order in which we processed this trigger.
            triggerBase.EstablishLayer();
        }

        //
        //  This method
        //  1. Is Invoked when a binding in a condition of a data trigger changes its value.
        //  2. When this happens we must invalidate all its dependents
        //
        private static void OnBindingValueInStyleChanged(object sender, BindingValueChangedEventArgs  e)
        {
            BindingExpressionBase bindingExpr = (BindingExpressionBase)sender;
            BindingBase binding = bindingExpr.ParentBindingBase;
            DependencyObject container = bindingExpr.TargetElement;

            // if the target has been GC'd, nothing more to do
            if (container == null)
                return;

            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(container, out fe, out fce, false);
            Style style = (fe != null) ? fe.Style : fce.Style;

            // Look for data trigger Setter information - invalidate the associated
            //  properties if found.
            HybridDictionary dataTriggerRecordFromBinding = style._dataTriggerRecordFromBinding;
            if( dataTriggerRecordFromBinding != null &&
                !bindingExpr.IsAttaching ) // Don't invalidate in the middle of attaching - effective value will be updated elsewhere in Style application code.
            {
                DataTriggerRecord record = (DataTriggerRecord)dataTriggerRecordFromBinding[binding];
                if (record != null)         // triggers with no setters (only actions) don't appear in the table
                {
                    InvalidateDependents(style, null, container, null, ref record.Dependents, false);
                }
            }

            // Look for any applicable trigger EnterAction or ExitAction
            InvokeApplicableDataTriggerActions(style, null, container, binding, e, StyleDataField);
        }

        //
        //  This method
        //  1. Is Invoked when a binding in a condition of a data trigger changes its value.
        //  2. When this happens we must invalidate all its dependents
        //
        private static void OnBindingValueInTemplateChanged(object sender, BindingValueChangedEventArgs  e)
        {
            BindingExpressionBase bindingExpr = (BindingExpressionBase)sender;
            BindingBase binding = bindingExpr.ParentBindingBase;
            DependencyObject container = bindingExpr.TargetElement;

            // if the target has been GC'd, nothing more to do
            if (container == null)
                return;

            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(container, out fe, out fce, false);

            FrameworkTemplate ft = fe.TemplateInternal;

            // Look for data trigger Setter information - invalidate the associated
            //  properties if found.
            HybridDictionary dataTriggerRecordFromBinding = null;
            if (ft != null)
            {
                dataTriggerRecordFromBinding = ft._dataTriggerRecordFromBinding;
            }

            if( dataTriggerRecordFromBinding != null &&
                !bindingExpr.IsAttaching ) // Don't invalidate in the middle of attaching - effective value will be updated elsewhere in Template application code.
            {
                DataTriggerRecord record = (DataTriggerRecord)dataTriggerRecordFromBinding[binding];
                if (record != null)         // triggers with no setters (only actions) don't appear in the table
                {
                    InvalidateDependents(null, ft, container, null, ref record.Dependents, false);
                }
            }

            // Look for any applicable trigger EnterAction or ExitAction
            InvokeApplicableDataTriggerActions(null, ft, container, binding, e, TemplateDataField);
        }

        //
        //  This method
        //  1. Is Invoked when a binding in a condition of a data trigger changes its value.
        //  2. When this happens we must invalidate all its dependents
        //
        private static void OnBindingValueInThemeStyleChanged(object sender, BindingValueChangedEventArgs e)
        {
            BindingExpressionBase bindingExpr = (BindingExpressionBase)sender;
            BindingBase binding = bindingExpr.ParentBindingBase;
            DependencyObject container = bindingExpr.TargetElement;

            // if the target has been GC'd, nothing more to do
            if (container == null)
                return;

            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(container, out fe, out fce, false);
            Style style = (fe != null) ? fe.ThemeStyle : fce.ThemeStyle;

            // Look for data trigger Setter information - invalidate the associated
            //  properties if found.
            HybridDictionary dataTriggerRecordFromBinding = style._dataTriggerRecordFromBinding;

            if( dataTriggerRecordFromBinding != null &&
                !bindingExpr.IsAttaching ) // Don't invalidate in the middle of attaching - effective value will be updated elsewhere in Style application code.
            {
                DataTriggerRecord record = (DataTriggerRecord)dataTriggerRecordFromBinding[binding];
                if (record != null)         // triggers with no setters (only actions) don't appear in the table
                {
                    InvalidateDependents(style, null, container, null, ref record.Dependents, false);
                }
            }

            // Look for any applicable trigger EnterAction or ExitAction
            InvokeApplicableDataTriggerActions(style, null, container, binding, e, ThemeStyleDataField);
        }

        // Given a Style/Template and a Binding whose value has changed, look for
        //  any data triggers that have trigger actions (EnterAction/ExitAction)
        //  and see if any of those actions need to run as a response to this change.
        private static void InvokeApplicableDataTriggerActions(
            Style                               style,
            FrameworkTemplate                   frameworkTemplate,
            DependencyObject                    container,
            BindingBase                         binding,
            BindingValueChangedEventArgs        e,
            UncommonField<HybridDictionary[]>   dataField)
        {
            HybridDictionary dataTriggersWithActions;

            if( style != null )
            {
                dataTriggersWithActions = style.DataTriggersWithActions;
            }
            else if( frameworkTemplate != null )
            {
                dataTriggersWithActions = frameworkTemplate.DataTriggersWithActions;
            }
            else
            {
                dataTriggersWithActions = null;
            }

            if( dataTriggersWithActions != null )
            {
                object candidateTrigger = dataTriggersWithActions[binding];

                if( candidateTrigger != null )
                {
                    // One or more trigger objects need to be evaluated.  The candidateTrigger
                    //  object may be a single trigger or a collection of them.
                    TriggerBase triggerBase = candidateTrigger as TriggerBase;
                    if( triggerBase != null )
                    {
                        InvokeDataTriggerActions( triggerBase, container, binding, e,
                            style, frameworkTemplate, dataField);
                    }
                    else
                    {
                        Debug.Assert(candidateTrigger is List<TriggerBase>, "Internal data structure error: The HybridDictionary [Style/Template].DataTriggersWithActions " +
                            "is expected to hold a single TriggerBase or a List<T> of them.  An object of type " +
                            candidateTrigger.GetType().ToString() + " is not expected.  Where did this object come from?");

                        List<TriggerBase> triggerList = (List<TriggerBase>)candidateTrigger;

                        for( int i = 0; i < triggerList.Count; i++ )
                        {
                            InvokeDataTriggerActions( triggerList[i], container, binding, e,
                                style, frameworkTemplate, dataField);
                        }
                    }
                }
            }
        }

        #endregion InvalidateMethods

        //  ===========================================================================
        //  These methods read some of per-instane Style/Template data
        //  ===========================================================================

        #region ReadInstanceData

        //
        //  This method
        //  1. Creates the ChildIndex for the given ChildName. If there is an
        //     existing childIndex it throws an exception for a duplicate name
        //
        internal static int CreateChildIndexFromChildName(
            string              childName,
            FrameworkTemplate   frameworkTemplate)
        {
            Debug.Assert(frameworkTemplate != null );

            // Lock must be made by caller

            HybridDictionary childIndexFromChildName;
            int lastChildIndex;

            Debug.Assert(frameworkTemplate != null);

            childIndexFromChildName = frameworkTemplate.ChildIndexFromChildName;
            lastChildIndex = frameworkTemplate.LastChildIndex;

            if (childIndexFromChildName.Contains(childName))
            {
                throw new ArgumentException(SR.Get(SRID.NameScopeDuplicateNamesNotAllowed, childName));
            }

            // Normal templated child check
            // If we're about to give out an index that we can't support, throw.
            if (lastChildIndex >= 0xFFFF)
            {
                throw new InvalidOperationException(SR.Get(SRID.StyleHasTooManyElements));
            }

            // No index found, allocate
            object value = lastChildIndex;

            childIndexFromChildName[childName] = value;

            Interlocked.Increment(ref lastChildIndex);

            Debug.Assert(frameworkTemplate != null);
            frameworkTemplate.LastChildIndex = lastChildIndex;

            return (int)value;
        }

        //
        //  This method
        //  1. Returns the ChildIndex for the given ChildName. If there isn't an
        //     existing childIndex it return -1
        //
        internal static int QueryChildIndexFromChildName(
            string           childName,
            HybridDictionary childIndexFromChildName)
        {
            // "Self" check
            if (childName == StyleHelper.SelfName)
            {
                return 0;
            }

            // Normal templated child check
            object value = childIndexFromChildName[childName];
            if (value == null)
            {
                return -1;
            }

            return (int)value;
        }

        //+------------------------------------------------------------------------------------
        //
        //  FindNameInTemplateContent
        //
        //  Find the object in the template content with the specified name.  This looks
        //  both for children both that are optimized FE/FCE tyes, or any other type.
        //
        //+------------------------------------------------------------------------------------

        internal static Object FindNameInTemplateContent(
            DependencyObject    container,
            string              childName,
            FrameworkTemplate   frameworkTemplate)
        {
            Debug.Assert(frameworkTemplate != null );

            // First, look in the list of optimized FE/FCEs in this template for this name.

            int index;
            Debug.Assert(frameworkTemplate != null);
            index = StyleHelper.QueryChildIndexFromChildName(childName, frameworkTemplate.ChildIndexFromChildName);

            // Did we find it?
            if (index == -1)
            {
                // No, we didn't find it, look at the rest of the named elements in the template content.

                Hashtable hashtable = TemplatedNonFeChildrenField.GetValue(container);
                if( hashtable != null )
                {
                    return hashtable[childName];
                }

                return null;
            }
            else
            {
                // Yes, we found the FE/FCE, return it.

                return StyleHelper.GetChild(container, index);
            }
        }

        //
        //  This method
        //  1. Finds the child corresponding to the given childIndex
        //
        internal static DependencyObject GetChild(DependencyObject container, int childIndex)
        {
            return GetChild(TemplatedFeChildrenField.GetValue(container), childIndex);
        }

        //
        //  This method
        //  1. Finds the child corresponding to the given childIndex
        //
        internal static DependencyObject GetChild(List<DependencyObject> styledChildren, int childIndex)
        {
            // Notice that if we are requesting a childIndex that hasn't been
            // instantiated yet we return null. This could happen when we are
            // invalidating the dependents for a property on a TemplateNode and
            // the dependent properties are meant to be on template nodes that
            // haven't been instantiated yet.
            if (styledChildren == null || childIndex > styledChildren.Count)
            {
                return null;
            }

            if (childIndex < 0)
            {
                throw new ArgumentOutOfRangeException("childIndex");
            }

            DependencyObject child = styledChildren[childIndex - 1];

            Debug.Assert(
                child is FrameworkElement && ((FrameworkElement)child).TemplateChildIndex == childIndex ||
                child is FrameworkContentElement && ((FrameworkContentElement)child).TemplateChildIndex == childIndex);

            return child;
        }

        #endregion ReadInstanceData

        //  ===========================================================================
        //  These methods are used to manipulate alternative expression storage
        //  ===========================================================================

        #region AlternateExpressionStorage

        //
        //  This method
        //  1. Registers for the "alternative Expression storage" feature, since
        //     we store Expressions in per-instance StyleData.
        //
        internal static void RegisterAlternateExpressionStorage()
        {
            DependencyObject.RegisterForAlternativeExpressionStorage(
                                new AlternativeExpressionStorageCallback(GetExpressionCore),
                                out _getExpression);
        }

        private static Expression GetExpressionCore(
            DependencyObject d,
            DependencyProperty dp,
            PropertyMetadata metadata)
        {
            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(d, out fe, out fce, false);

            if (fe != null)
            {
                return fe.GetExpressionCore(dp, metadata);
            }

            if (fce != null)
            {
                return fce.GetExpressionCore(dp, metadata);
            }

            return null;
        }

        //
        //  This method
        //  1. Is a wrapper for property engine's GetExpression method
        //
        internal static Expression GetExpression(
            DependencyObject d,
            DependencyProperty dp)
        {
            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(d, out fe, out fce, false);

            // temporarily mark the element as "initialized", so that we always get
            // the desired expression (see GetInstanceValue).
            bool isInitialized = (fe != null) ? fe.IsInitialized : (fce != null) ? fce.IsInitialized : true;
            if (!isInitialized)
            {
                if (fe != null)
                    fe.WriteInternalFlag(InternalFlags.IsInitialized, true);
                else if (fce != null)
                    fce.WriteInternalFlag(InternalFlags.IsInitialized, true);
            }

            // get the desired expression
            Expression result = _getExpression(d, dp, dp.GetMetadata(d.DependencyObjectType));

            // restore the initialized flag
            if (!isInitialized)
            {
                if (fe != null)
                    fe.WriteInternalFlag(InternalFlags.IsInitialized, false);
                else if (fce != null)
                    fce.WriteInternalFlag(InternalFlags.IsInitialized, false);
            }

            return result;
        }

        #endregion AlternateExpressionStorage

        //  ===========================================================================
        //  These methods are used to query Style/Template
        //  eventhandlers during event routing
        //  ===========================================================================

        #region ReadEvents

        //
        //  This method
        //  Gets the handlers of a template child
        //  (Index is '0' when the styled container is asking)
        //
        internal static RoutedEventHandlerInfo[] GetChildRoutedEventHandlers(
            int                                     childIndex,
            RoutedEvent                           routedEvent,
            ref ItemStructList<ChildEventDependent> eventDependents)
        {
            Debug.Assert(routedEvent != null);

            if (childIndex > 0)
            {
                // Find the EventHandlersStore that matches the given childIndex
                EventHandlersStore eventHandlersStore = null;
                for (int i=0; i<eventDependents.Count; i++)
                {
                    if (eventDependents.List[i].ChildIndex == childIndex)
                    {
                        eventHandlersStore = eventDependents.List[i].EventHandlersStore;
                        break;
                    }
                }

                if (eventHandlersStore != null)
                {
                    return eventHandlersStore.GetRoutedEventHandlers(routedEvent);
                }
            }

            return null;
        }

        #endregion ReadEvents



        //  ===========================================================================
        //  These are some Style/Template helper methods
        //  ===========================================================================

        #region HelperMethods

        //
        //  This method
        //  1. Says if styling this property involve styling the logical tree
        //
        internal static bool IsStylingLogicalTree(DependencyProperty dp, object value)
        {
            // some properties are known not to affect the logical tree
            if (dp == ItemsControl.ItemsPanelProperty || dp == FrameworkElement.ContextMenuProperty || dp == FrameworkElement.ToolTipProperty)
                return false;

            return (value is Visual) || (value is ContentElement);
        }



        #endregion HelperMethods

        #region Properties
        #endregion Properties

        #region Fields

        //
        //  Stores instance state of a style
        //
        internal static readonly UncommonField<HybridDictionary[]> StyleDataField = new UncommonField<HybridDictionary[]>();

        //
        //  Stores instance state of a template
        //
        internal static readonly UncommonField<HybridDictionary[]> TemplateDataField = new UncommonField<HybridDictionary[]>();

        //
        //  Stores per-instance template property values.  E.g. this is used for a template child value
        //  if it's a Freezable within a DynamicResource inside (and thus can't be shared).
        //
        internal static readonly UncommonField<HybridDictionary> ParentTemplateValuesField = new UncommonField<HybridDictionary>();

        //
        //  Stores instance state of a theme style
        //
        internal static readonly UncommonField<HybridDictionary[]> ThemeStyleDataField = new UncommonField<HybridDictionary[]>();

        //
        //  A list of all children created from the Style/Template that is an FE/FCE. This list is built
        //  such that all elements with non-negative child indices come first.Once
        //  you see a child with TemplateChildIndex = -1 there are no more "interesting"
        //  children after that.
        //
        internal static readonly UncommonField<List<DependencyObject>> TemplatedFeChildrenField = new UncommonField<List<DependencyObject>>();

        //
        //  A list of all named objects created from the Template that is *not* an FE/FCE.
        //

        internal static readonly UncommonField<Hashtable> TemplatedNonFeChildrenField = new UncommonField<Hashtable>();

        //  "Self" ChildName, maps to ChildIndex 0 to represent
        //  the Container for a Style/Template
        //
        internal const string SelfName = "~Self";

        //
        // Used to pass an empty stucture reference when the parent style/template is null
        //
        internal static FrugalStructList<ContainerDependent>  EmptyContainerDependents;

        //
        // Certain instance values are "applied" only on demand.  This value indicates that
        // the demand hasn't happened yet.
        //
        internal static readonly object NotYetApplied = new NamedObject("NotYetApplied");

        //
        //  The property engine's API for the "alternative Expression storage"
        //  feature are set in the static ctor.
        private static AlternativeExpressionStorageCallback _getExpression;

        //
        //  Delegate used for performing the actions associated with
        //  an event trigger.  [Style/Template].ProcessEventTrigger will add this to the
        //  list of delegates to be consulted, and FrameworkElement.AddStyleHandlersToEventRoute
        //  is the one that takes the list and add to the actual event route.
        //
        internal static RoutedEventHandler EventTriggerHandlerOnContainer = new RoutedEventHandler(ExecuteEventTriggerActionsOnContainer);
        internal static RoutedEventHandler EventTriggerHandlerOnChild = new RoutedEventHandler(ExecuteEventTriggerActionsOnChild);

        //
        // This is used an index for an unshared template content property with a
        // MarkupExtension for its value within the instance value data-structures.
        //
        internal const int UnsharedTemplateContentPropertyIndex = -1;

        // This baml record reader is used during template application.  It's thread saftey is
        // protected because we only allow one template to be instaniated at a time
        // (FrameworkTemplate.Synchronized).

        #endregion Fields
    }

    #endregion StyleHelper

    #region DataStructures

    //
    //  Describes the value stored in the PropertyValue structure
    //
    internal enum PropertyValueType : int
    {
        Set                       = ValueLookupType.Simple,
        Trigger                   = ValueLookupType.Trigger,
        PropertyTriggerResource   = ValueLookupType.PropertyTriggerResource,
        DataTrigger               = ValueLookupType.DataTrigger,
        DataTriggerResource       = ValueLookupType.DataTriggerResource,
        TemplateBinding           = ValueLookupType.TemplateBinding,
        Resource                  = ValueLookupType.Resource,
    }

    //
    //  Property Values set on either Style or a TemplateNode or a
    //  Trigger are stored in structures of this type
    //
    internal struct PropertyValue
    {
        internal PropertyValueType  ValueType;
        internal TriggerCondition[] Conditions;
        internal string             ChildName;
        internal DependencyProperty Property;
        internal object             ValueInternal;

        /// <summary>
        /// Sparkle uses this to query values on a FEF
        /// </summary>
        internal object Value
        {
            get
            {
                // Inflate the deferred reference if the value is one of those.
                DeferredReference deferredReference = ValueInternal as DeferredReference;
                if (deferredReference != null)
                {
                    ValueInternal = deferredReference.GetValue(BaseValueSourceInternal.Unknown);
                }

                return ValueInternal;
            }
        }
    }

    //
    //  Describes the value stored in the ChildValueLookup structure
    //
    internal enum ValueLookupType : int
    {
        Simple,
        Trigger,
        PropertyTriggerResource,
        DataTrigger,
        DataTriggerResource,
        TemplateBinding,
        Resource
    }

    //
    //  PropertyValues set on either a Style or a TemplateNode or a
    //  Trigger are consolidated into a this data-structure. This
    //  happens either when the owner Style or the Template is sealed.
    //
    internal struct ChildValueLookup
    {
        internal ValueLookupType    LookupType;
        internal TriggerCondition[] Conditions;
        internal DependencyProperty Property;
        internal object             Value;

        // Implemented for #1038821, FxCop ConsiderOverridingEqualsAndOperatorEqualsOnValueTypes
        //  Trading off an object boxing cost in exchange for avoiding reflection cost.
        public override bool Equals( object value )
        {
            if( value is ChildValueLookup )
            {
                ChildValueLookup other = (ChildValueLookup)value;

                if( LookupType      == other.LookupType &&
                    Property        == other.Property &&
                    Value           == other.Value )
                {
                    if( Conditions == null &&
                        other.Conditions == null )
                    {
                        // Both condition arrays are null
                        return true;
                    }

                    if( Conditions == null ||
                        other.Conditions == null )
                    {
                        // One condition array is null, but not other
                        return false;
                    }

                    // Both condition array non-null, see if they're the same length..
                    if( Conditions.Length == other.Conditions.Length )
                    {
                        // Same length.  Walk the list and compare.
                        for( int i = 0; i < Conditions.Length; i++ )
                        {
                            if( !Conditions[i].TypeSpecificEquals(other.Conditions[i]) )
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        // GetHashCode, ==, and != are required when Equals is overridden, even though we don't expect to need them.
        public override int GetHashCode()
        {
            Debug.Assert(false, "GetHashCode for value types will use reflection to generate the hashcode.  Write a better hash code generation algorithm if this struct is to be used in a hashtable, or remove this assert if it's decided that reflection is OK.");

            return base.GetHashCode();
        }

        public static bool operator == ( ChildValueLookup value1, ChildValueLookup value2 )
        {
            return value1.Equals( value2 );
        }

        public static bool operator != ( ChildValueLookup value1, ChildValueLookup value2 )
        {
            return !value1.Equals( value2 );
        }
    }

    //
    //  Conditions set on [Multi]Trigger are stored
    //  in structures of this kind
    //
    internal struct TriggerCondition
    {
        #region Construction

        internal TriggerCondition(DependencyProperty dp, LogicalOp logicalOp, object value, string sourceName)
        {
            Property = dp;
            Binding = null;
            LogicalOp = logicalOp;
            Value = value;
            SourceName = sourceName;
            SourceChildIndex = 0;
            BindingValueCache = new BindingValueCache(null, null);
        }

        internal TriggerCondition(BindingBase binding, LogicalOp logicalOp, object value) :
            this(binding, logicalOp, value, StyleHelper.SelfName)
        {
            // Call Forwarded
        }

        internal TriggerCondition(BindingBase binding, LogicalOp logicalOp, object value, string sourceName)
        {
            Property = null;
            Binding = binding;
            LogicalOp = logicalOp;
            Value = value;
            SourceName = sourceName;
            SourceChildIndex = 0;
            BindingValueCache = new BindingValueCache(null, null);
        }

        // Check for match
        internal bool Match(object state)
        {
            return Match(state, Value);
        }

        private bool Match(object state, object referenceValue)
        {
            if (LogicalOp == LogicalOp.Equals)
            {
                return Object.Equals(state, referenceValue);
            }
            else
            {
                return !Object.Equals(state, referenceValue);
            }
        }

        // Check for match, after converting the reference value to the type
        // of the state value.  (Used by data triggers)
        internal bool ConvertAndMatch(object state)
        {
            // convert the reference value to the type of 'state',
            // provided the reference value is a string and the
            // state isn't null or a string.  (Otherwise, we can
            // compare the state and reference values directly.)
            object referenceValue = Value;
            string referenceString = referenceValue as String;
            Type stateType = (state != null) ? state.GetType() : null;

            if (referenceString != null && stateType != null &&
                stateType != typeof(String))
            {
                // the most recent type and value are cached in the
                // TriggerCondition, since it's very likely the
                // condition's Binding produces the same type of
                // value every time.  The cached values can be used
                // on any thread, so we must synchronize access.
                BindingValueCache bindingValueCache = BindingValueCache;
                Type cachedType = bindingValueCache.BindingValueType;
                object cachedValue = bindingValueCache.ValueAsBindingValueType;

                if (stateType != cachedType)
                {
                    // the cached type isn't the current type - refresh the cache

                    cachedValue = referenceValue; // in case of failure
                    TypeConverter typeConverter = DefaultValueConverter.GetConverter(stateType);
                    if (typeConverter != null && typeConverter.CanConvertFrom(typeof(String)))
                    {
                        // PreSharp uses message numbers that the C# compiler doesn't know about.
                        // Disable the C# complaints, per the PreSharp documentation.
                        #pragma warning disable 1634, 1691

                        // PreSharp complains about catching NullReference (and other) exceptions.
                        // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
                        #pragma warning disable 56500

                        try
                        {
                            cachedValue = typeConverter.ConvertFromString(null, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS, referenceString);
                        }
                        catch (Exception ex)
                        {
                            if (CriticalExceptions.IsCriticalApplicationException(ex))
                                throw;
                            // if the conversion failed, just use the unconverted value
                        }
                        catch // non CLS compliant exception
                        {
                            // if the conversion failed, just use the unconverted value
                        }

                        #pragma warning restore 56500
                        #pragma warning restore 1634, 1691
                    }

                    // cache the converted value
                    bindingValueCache = new BindingValueCache(stateType, cachedValue);
                    BindingValueCache = bindingValueCache;
                }

                referenceValue = cachedValue;
            }

            return Match(state, referenceValue);
        }

        // Implemented for #1038821, FxCop ConsiderOverridingEqualsAndOperatorEqualsOnValueTypes
        //  Called from ChildValueLookup.Equals, avoid boxing by not using the generic object-based Equals.
        internal bool TypeSpecificEquals( TriggerCondition value )
        {
            if( Property            == value.Property &&
                Binding             == value.Binding &&
                LogicalOp           == value.LogicalOp &&
                Value               == value.Value &&
                SourceName          == value.SourceName )
            {
                return true;
            }
            return false;
        }

        #endregion Construction

        internal readonly DependencyProperty        Property;
        internal readonly BindingBase               Binding;
        internal readonly LogicalOp                 LogicalOp;
        internal readonly object                    Value;
        internal readonly string                    SourceName;
        internal          int                       SourceChildIndex;
        internal          BindingValueCache         BindingValueCache;
    }

    //
    //  This is a data-structure used to prevent threading issues while setting
    //  BindingValueType and ValueAsBindingValueType as separate members.
    //
    internal class BindingValueCache
    {
        internal BindingValueCache(Type bindingValueType, object valueAsBindingValueType)
        {
            BindingValueType = bindingValueType;
            ValueAsBindingValueType = valueAsBindingValueType;
        }

        internal readonly Type   BindingValueType;
        internal readonly object ValueAsBindingValueType;
    }

    //
    //  Describes the logical operation to be used to test the
    //  condition of a [Multi]Trigger
    //
    internal enum LogicalOp
    {
        Equals,
        NotEquals
    }

    //
    //  Each item in the ContainerDependents list is of this type. Stores the DP on
    //  the container that is dependent upon this style and whether that dp was set
    //  via Style.SetValue or TriggerBase.SetValue.
    //
    internal struct ContainerDependent
    {
        internal DependencyProperty         Property;
        internal bool                       FromVisualTrigger;
    }

    //
    //  Each item in the ChildEventDependents list is of this type. Stores the the
    //  EventHandlersStore corresponding the a childIndex.
    //
    internal struct ChildEventDependent
    {
        internal int                ChildIndex;
        internal EventHandlersStore EventHandlersStore;
    }

    //
    //  Each item in the ChildPropertyDependents list and the ResourceDependents
    //  list is of this type.
    //
    //  PERF: Name is used only when storing a ResourceDependent.
    //       Listener is used only when a Trigger has an invalidation listener
    //  Both are relatively rare.  Is there an optimization here?
    internal struct ChildPropertyDependent
    {
        public int                          ChildIndex;
        public DependencyProperty           Property;
        public object                       Name; // When storing ResourceDependent, the name of the resource.
    }

    //
    // This struct represents a list of actions associated with a trigger whose
    //  conditions have fired but could not execute immediately.  The original
    //  motivation is to address the scenario where the actions want to manipulate
    //  the templated children but the template expansion hasn't happened yet.
    //
    internal struct DeferredAction
    {
        internal TriggerBase TriggerBase;
        internal TriggerActionCollection TriggerActionCollection;
    }

    //
    //  Each Binding that appears in a condition of a data trigger has
    //  supporting information that appears in this record.
    //
    internal class DataTriggerRecord
    {
        public FrugalStructList<ChildPropertyDependent> Dependents = new FrugalStructList<ChildPropertyDependent>();
    }

    #pragma warning disable 649

    //
    //  Disable warnings about fields never being assigned to. The structs in this
    //  section are designed to function with its fields starting at their default
    //  values and without the need of surfacing a constructor other than the default.
    //

    //
    //  Stores a list of all those properties that need to be invalidated whenever
    //  a trigger driver property is invalidated on the source node.
    //
    internal struct TriggerSourceRecord
    {
        public FrugalStructList<ChildPropertyDependent> ChildPropertyDependents;
    }

    //
    //  Stores a list of properties set via explicit SetValue or Triggers or
    //  Storyboards on a TemplateNode.
    //
    internal struct ChildRecord
    {
        public ItemStructMap<ItemStructList<ChildValueLookup>> ValueLookupListFromProperty;  // Indexed by DP.GlobalIndex
    }

    #pragma warning restore 649

    #if DEBUG

    //
    //  This data-structure is used only on debug builds.
    //

    internal enum VerificationState
    {
        WaitingForBuildVisualTree,
        WaitingForAddCustomTemplateRoot,
        WaitingForBuildVisualTreeCompletion
    }

    #endif

    //
    //  This enum is used to designate different types of per-instance
    //  style data that we use at run-time
    //
    internal enum InstanceStyleData
    {
        InstanceValues,
        ArraySize // Keep this one last - used to allocate size of array in dataField
    }

    //
    //  When an unshareable value appears in the property value list, we store the
    //  corresponding "instance value" in per-instance StyleData.  More precisely,
    //  the instance value is stored in a hash table, using the following class
    //  as the key (so we know where the value came from).
    //
    internal class InstanceValueKey
    {
        #region Construction

        internal InstanceValueKey(int childIndex, int dpIndex, int index)
        {
            _childIndex = childIndex;
            _dpIndex = dpIndex;
            _index = index;
        }

        #endregion Construction

        public override bool Equals(object o)
        {
            InstanceValueKey key = o as InstanceValueKey;
            if (key != null)
                return (_childIndex == key._childIndex) && (_dpIndex == key._dpIndex) && (_index == key._index);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (20000*_childIndex + 20*_dpIndex + _index);
        }

        // the origin of the instance value in the container's style:
        int _childIndex;    // the childIndex of the target element
        int _dpIndex;       // the global index of the target DP
        int _index;         // the index in the ItemStructList<ChildValueLookup>
    }

    #endregion DataStructures
}


