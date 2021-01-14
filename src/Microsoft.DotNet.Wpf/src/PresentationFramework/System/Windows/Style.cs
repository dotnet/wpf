// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Style and templating.
*
*
\***************************************************************************/
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;  // For Debug.Assert
using System.Windows.Threading;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation; // For Storyboard support
using System.Windows.Markup;
using System.IO;
using MS.Utility;
using MS.Internal;
using System;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows
{
    /// <summary>
    ///     Styling and Templating
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    [DictionaryKeyProperty("TargetType")]
    [ContentProperty("Setters")]
    public class Style : DispatcherObject, INameScope, IAddChild, ISealable, IHaveResources, IQueryAmbient
    {
        static Style()
        {
            // Register for the "alternative Expression storage" feature, since
            // we store Expressions in per-instance StyleData.
            StyleHelper.RegisterAlternateExpressionStorage();
        }

        /// <summary>
        ///     Style construction
        /// </summary>
        public Style()
        {
            GetUniqueGlobalIndex();
        }

        /// <summary>
        ///     Style construction
        /// </summary>
        /// <param name="targetType">Type in which Style will be applied</param>
        public Style(Type targetType)
        {
            TargetType = targetType;

            GetUniqueGlobalIndex();
        }

        /// <summary>
        ///     Style construction
        /// </summary>
        /// <param name="targetType">Type in which Style will be applied</param>
        /// <param name="basedOn">Style to base this Style on</param>
        public Style(Type targetType, Style basedOn)
        {
            TargetType = targetType;
            BasedOn = basedOn;

            GetUniqueGlobalIndex();
        }

        #region INameScope
        /// <summary>
        /// Registers the name - Context combination
        /// </summary>
        /// <param name="name">Name to register</param>
        /// <param name="scopedElement">Element where name is defined</param>
        public void RegisterName(string name, object scopedElement)
        {
            // Verify Context Access
            VerifyAccess();

            _nameScope.RegisterName(name, scopedElement);
        }

        /// <summary>
        /// Unregisters the name - element combination
        /// </summary>
        /// <param name="name">Name of the element</param>
        public void UnregisterName(string name)
        {
            // Verify Context Access
            VerifyAccess();

            _nameScope.UnregisterName(name);
        }

        /// <summary>
        /// Find the element given name
        /// </summary>
        /// <param name="name">Name of the element</param>
        object INameScope.FindName(string name)
        {
            // Verify Context Access
            VerifyAccess();

            return _nameScope.FindName(name);
        }

        private NameScope _nameScope = new NameScope();
        #endregion IIdScope

        /// <summary>
        /// Each Style gets its own unique index used for Style.GetHashCode
        /// </summary>
        private void GetUniqueGlobalIndex()
        {
            lock (Synchronized)
            {
                // Setup unqiue global index
                StyleInstanceCount++;
                GlobalIndex = StyleInstanceCount;
            }
        }

        /// <summary>
        ///     Style mutability state
        /// </summary>
        /// <remarks>
        ///     A style is sealed when another style is basing on it, or,
        ///     when it's applied
        /// </remarks>
        public bool IsSealed
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return _sealed;
            }
        }


        /// <summary>
        ///     Type that this style is intended
        /// </summary>
        /// <remarks>
        ///     By default, the target type is FrameworkElement
        /// </remarks>
        [Ambient]
        [Localizability(LocalizationCategory.NeverLocalize)]
        public Type TargetType
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return _targetType;
            }

            set
            {
                // Verify Context Access
                VerifyAccess();

                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Style"));
                }

                if( value == null )
                {
                    throw new ArgumentNullException("value");
                }

                if (!typeof(FrameworkElement).IsAssignableFrom(value) &&
                    !typeof(FrameworkContentElement).IsAssignableFrom(value) &&
                    !(DefaultTargetType == value))
                {
                    #pragma warning suppress 6506 // value is obviously not null
                    throw new ArgumentException(SR.Get(SRID.MustBeFrameworkDerived, value.Name));
                }

                _targetType = value;

                SetModified(TargetTypeID);
            }
        }

        /// <summary>
        ///     Style to base on
        /// </summary>
        [DefaultValue(null)]
        [Ambient]
        public Style BasedOn
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                return _basedOn;
            }
            set
            {
                // Verify Context Access
                VerifyAccess();

                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Style"));
                }

                if( value == this )
                {
                    // Basing on self is not allowed.  This is a degenerate case
                    //  of circular reference chain, the full check for circular
                    //  reference is done in Seal().
                    throw new ArgumentException(SR.Get(SRID.StyleCannotBeBasedOnSelf));
                }

                _basedOn = value;

                SetModified(BasedOnID);
            }
        }


        /// <summary>
        ///     Visual triggers
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TriggerCollection Triggers
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if (_visualTriggers == null)
                {
                    _visualTriggers = new TriggerCollection();

                    // If the style has been sealed prior to this the newly
                    // created TriggerCollection also needs to be sealed
                    if (_sealed)
                    {
                        _visualTriggers.Seal();
                    }
                }
                return _visualTriggers;
            }
        }

        /// <summary>
        ///     The collection of property setters for the target type
        /// </summary>

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public SetterBaseCollection Setters
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if( _setters == null )
                {
                    _setters = new SetterBaseCollection();

                    // If the style has been sealed prior to this the newly
                    // created SetterBaseCollection also needs to be sealed
                    if (_sealed)
                    {
                        _setters.Seal();
                    }
                }
                return _setters;
            }
        }

        /// <summary>
        ///     The collection of resources that can be
        ///     consumed by the container and its sub-tree.
        /// </summary>
        [Ambient]
        public ResourceDictionary Resources
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if( _resources == null )
                {
                    _resources = new ResourceDictionary();

                    // A Style ResourceDictionary can be accessed across threads
                    _resources.CanBeAccessedAcrossThreads = true;

                    // If the style has been sealed prior to this the newly
                    // created ResourceDictionary also needs to be sealed
                    if (_sealed)
                    {
                        _resources.IsReadOnly = true;
                    }
                }
                return _resources;
            }
            set
            {
                // Verify Context Access
                VerifyAccess();

                if( _sealed )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "Style"));
                }

                _resources = value;

                if (_resources != null)
                {
                    // A Style ResourceDictionary can be accessed across threads
                    _resources.CanBeAccessedAcrossThreads = true;
                }
            }
        }

        ResourceDictionary IHaveResources.Resources
        {
            get { return Resources; }
            set { Resources = value; }
        }

        /// <summary>
        ///     Tries to find a Resource for the given resourceKey in the current
        ///     style's ResourceDictionary or the basedOn style's ResourceDictionary
        ///     in that order.
        /// </summary>
        internal object FindResource(object resourceKey, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference)
        {
            if ((_resources != null) && _resources.Contains(resourceKey))
            {
                bool canCache;
                return _resources.FetchResource(resourceKey, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
            if (_basedOn != null)
            {
                return _basedOn.FindResource(resourceKey, allowDeferredResourceReference, mustReturnDeferredResourceReference);
            }
            return DependencyProperty.UnsetValue;
        }

        internal ResourceDictionary FindResourceDictionary(object resourceKey)
        {
            Debug.Assert(resourceKey != null, "Argument cannot be null");
            if (_resources != null && _resources.Contains(resourceKey))
            {
                return _resources;
            }
            if (_basedOn != null)
            {
                return _basedOn.FindResourceDictionary(resourceKey);
            }
            return null;
        }

        bool IQueryAmbient.IsAmbientPropertyAvailable(string propertyName)
        {
            // We want to make sure that StaticResource resolution checks the .Resources
            // Ie.  The Ambient search should look at Resources if it is set.
            // Even if it wasn't set from XAML (eg. the Ctor (or derived Ctor) added stuff)
            switch (propertyName)
            {
                case "Resources":
                    if (_resources == null)
                    {
                        return false;
                    }
                    break;
                case "BasedOn":
                    if (_basedOn == null)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        ///<summary>
        /// This method is called to Add a Setter object as a child of the Style.
        /// This method is used primarily by the parser to set style properties and events.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a SetterBase subclass.
        ///</param>
        void IAddChild.AddChild (Object value)
        {
            // Verify Context Access
            VerifyAccess();

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            SetterBase sb = value as SetterBase;

            if (sb == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(SetterBase)), "value");
            }

            Setters.Add(sb);
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As default Styles do not support text, calling this method has no effect.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param>
        void IAddChild.AddText (string text)
        {
            // Verify Context Access
            VerifyAccess();

            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        /// <summary>
        ///     Given a set of values for the PropertyValue struct, put that in
        /// to the PropertyValueList, overwriting any existing entry.
        /// </summary>
        private void UpdatePropertyValueList(
            DependencyProperty dp,
            PropertyValueType valueType,
            object value)
        {
            // Check for existing value on dp
            int existingIndex = -1;
            for( int i = 0; i < PropertyValues.Count; i++ )
            {
                if( PropertyValues[i].Property == dp )
                {
                    existingIndex = i;
                    break;
                }
            }

            if( existingIndex >= 0 )
            {
                // Overwrite existing value for dp
                PropertyValue propertyValue = PropertyValues[existingIndex];
                propertyValue.ValueType = valueType;
                propertyValue.ValueInternal = value;
                // Put back modified struct
                PropertyValues[existingIndex] = propertyValue;
            }
            else
            {
                // Store original data
                PropertyValue propertyValue = new PropertyValue();
                propertyValue.ValueType = valueType;
                propertyValue.ChildName = StyleHelper.SelfName;
                propertyValue.Property = dp;
                propertyValue.ValueInternal = value;

                PropertyValues.Add(propertyValue);
            }
        }

        internal void CheckTargetType(object element)
        {
            // In the most common case TargetType is Default
            // and we can avoid a call to IsAssignableFrom() who's performance is unknown.
            if(DefaultTargetType == TargetType)
                return;

            Type elementType = element.GetType();
            if(!TargetType.IsAssignableFrom(elementType))
            {
                throw new InvalidOperationException(SR.Get(SRID.StyleTargetTypeMismatchWithElement,
                                                    this.TargetType.Name,
                                                    elementType.Name));
            }
        }

        /// <summary>
        /// This Style and all factories/triggers are now immutable
        /// </summary>
        public void Seal()
        {
            // Verify Context Access
            VerifyAccess();

            // 99% case - Style is already sealed.
            if (_sealed)
            {
                return;
            }

            // Most parameter checking is done as "upstream" as possible, but some
            //  can't be checked until Style is sealed.
            if (_targetType == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.NullPropertyIllegal, "TargetType"));
            }

            if (_basedOn != null)
            {
                if(DefaultTargetType != _basedOn.TargetType &&
                    !_basedOn.TargetType.IsAssignableFrom(_targetType))
                {
                    throw new InvalidOperationException(SR.Get(SRID.MustBaseOnStyleOfABaseType, _targetType.Name));
                }
            }

            // Seal setters
            if (_setters != null)
            {
                _setters.Seal();
            }

            // Seal triggers
            if (_visualTriggers != null)
            {
                _visualTriggers.Seal();
            }

            // Will throw InvalidOperationException if we find a loop of
            //  BasedOn references.  (A.BasedOn = B, B.BasedOn = C, C.BasedOn = A)
            CheckForCircularBasedOnReferences();

            // Seal BasedOn Style chain
            if (_basedOn != null)
            {
                _basedOn.Seal();
            }

            // Seal the ResourceDictionary
            if (_resources != null)
            {
                _resources.IsReadOnly = true;
            }

            //
            // Build shared tables
            //

            // Process all Setters set on the selfStyle. This stores all the property
            // setters on the current styles into PropertyValues list, so it can be used
            // by ProcessSelfStyle in the next step. The EventSetters for the current
            // and all the basedOn styles are merged into the EventHandlersStore on the
            // current style.
            ProcessSetters(this);

            // Add an entry in the EventDependents list for
            // the TargetType's EventHandlersStore. Notice
            // that the childIndex is 0.
            StyleHelper.AddEventDependent(0, this.EventHandlersStore, ref EventDependents);

            // Process all PropertyValues (all are "Self") in the Style
            // chain (base added first)
            ProcessSelfStyles(this);

            // Process all TriggerBase PropertyValues ("Self" triggers
            // and child triggers) in the Style chain last (highest priority)
            ProcessVisualTriggers(this);

            // Sort the ResourceDependents, to help avoid duplicate invalidations
            StyleHelper.SortResourceDependents(ref ResourceDependents);

            // All done, seal self and call it a day.
            _sealed = true;

            // Remove thread affinity so it can be accessed across threads
            DetachFromDispatcher();
        }

        /// <summary>
        ///     This method checks to see if the BasedOn hierarchy contains
        /// a loop in the chain of references.
        /// </summary>
        /// <remarks>
        /// Classic "when did we enter the cycle" problem where we don't know
        ///  what to start remembering and what to check against.  Brute-
        ///  force approach here is to remember everything with a stack
        ///  and do a linear comparison through everything.  Since the Style
        ///  BasedOn hierarchy is not expected to be large, this should be OK.
        /// </remarks>
        private void CheckForCircularBasedOnReferences()
        {
            Stack basedOnHierarchy = new Stack(10);  // 10 because that's the default value (see MSDN) and the perf team wants us to specify something.
            Style latestBasedOn = this;

            while( latestBasedOn != null )
            {
                if( basedOnHierarchy.Contains( latestBasedOn ) )
                {
                    // Uh-oh.  We've seen this Style before.  This means
                    //  the BasedOn hierarchy contains a loop.
                    throw new InvalidOperationException(SR.Get(
                        SRID.StyleBasedOnHasLoop));

                    // Debugging note: If we stop here, the basedOnHierarchy
                    //  object is still alive and we can browse through it to
                    //  see what we've explored.  (This does not apply if
                    //  somebody catches this exception and re-throws.)
                }

                // Haven't seen it, push on stack and go to next level.
                basedOnHierarchy.Push( latestBasedOn );
                latestBasedOn = latestBasedOn.BasedOn;
            }

            return;
        }

        // Iterates through the setters collection and adds the EventSetter information into
        // an EventHandlersStore for easy and fast retrieval during event routing. Also adds
        // an entry in the EventDependents list for EventhandlersStore holding the TargetType's
        // events.
        private void ProcessSetters(Style style)
        {
            // Walk down to bottom of based-on chain
            if (style == null)
            {
                return;
            }

            style.Setters.Seal(); // Does not mark individual setters as sealed, that's up to the loop below.


            // On-demand create the PropertyValues list, so that we can specify the right size.

            if(PropertyValues.Count == 0)
            {
                PropertyValues = new FrugalStructList<System.Windows.PropertyValue>(style.Setters.Count);
            }

            // Add EventSetters to local EventHandlersStore
            for (int i = 0; i < style.Setters.Count; i++)
            {
                SetterBase setterBase = style.Setters[i];
                Debug.Assert(setterBase != null, "Setter collection must contain non-null instances of SetterBase");

                // Setters are folded into the PropertyValues table only for the current style. The
                // processing of BasedOn Style properties will occur in subsequent call to ProcessSelfStyle
                Setter setter = setterBase as Setter;
                if (setter != null)
                {
                    // Style Setters are not allowed to have a child target name - since there are no child nodes in a Style.
                    if( setter.TargetName != null )
                    {
                        throw new InvalidOperationException(SR.Get(SRID.SetterOnStyleNotAllowedToHaveTarget, setter.TargetName));
                    }

                    if (style == this)
                    {
                        DynamicResourceExtension dynamicResource = setter.ValueInternal as DynamicResourceExtension;
                        if (dynamicResource == null)
                        {
                            UpdatePropertyValueList( setter.Property, PropertyValueType.Set, setter.ValueInternal );
                        }
                        else
                        {
                            UpdatePropertyValueList( setter.Property, PropertyValueType.Resource, dynamicResource.ResourceKey );
                        }
                    }
                }
                else
                {

                    Debug.Assert(setterBase is EventSetter,
                                 "Unsupported SetterBase subclass in style triggers ({0})", setterBase.GetType().ToString());

                    // Add this to the _eventHandlersStore

                    EventSetter eventSetter = (EventSetter)setterBase;
                    if (_eventHandlersStore == null)
                    {
                        _eventHandlersStore = new EventHandlersStore();
                    }
                    _eventHandlersStore.AddRoutedEventHandler(eventSetter.Event, eventSetter.Handler, eventSetter.HandledEventsToo);

                    SetModified(HasEventSetter);

                    // If this event setter watches the loaded/unloaded events, set the optimization
                    // flag.

                    if (eventSetter.Event == FrameworkElement.LoadedEvent || eventSetter.Event == FrameworkElement.UnloadedEvent)
                    {
                        _hasLoadedChangeHandler = true;
                    }


                }
            }

            // Process EventSetters on based on style so they get merged
            // into the EventHandlersStore for the current style.
            ProcessSetters(style._basedOn);
        }

        private void ProcessSelfStyles(Style style)
        {
            // Walk down to bottom of based-on chain
            if (style == null)
            {
                return;
            }

            ProcessSelfStyles(style._basedOn);

            // Merge in "self" PropertyValues while walking back up the tree
            // "Based-on" style "self" rules are always added first (lower priority)
            for (int i = 0; i < style.PropertyValues.Count; i++)
            {
                PropertyValue propertyValue = style.PropertyValues[i];

                StyleHelper.UpdateTables(ref propertyValue, ref ChildRecordFromChildIndex,
                    ref TriggerSourceRecordFromChildIndex, ref ResourceDependents, ref _dataTriggerRecordFromBinding,
                    null /*_childIndexFromChildID*/, ref _hasInstanceValues);

                // Track properties on the container that are being driven by
                // the Style so that they can be invalidated during style changes
                StyleHelper.AddContainerDependent(propertyValue.Property, false /*fromVisualTrigger*/, ref ContainerDependents);
            }
        }

        private void ProcessVisualTriggers(Style style)
        {
            // Walk down to bottom of based-on chain
            if (style == null)
            {
                return;
            }

            ProcessVisualTriggers(style._basedOn);

            if (style._visualTriggers != null)
            {
                // Merge in "self" and child TriggerBase PropertyValues while walking
                // back up the tree. "Based-on" style rules are always added first
                // (lower priority)
                int triggerCount = style._visualTriggers.Count;
                for (int i = 0; i < triggerCount; i++)
                {
                    TriggerBase trigger = style._visualTriggers[i];

                    // Set things up to handle Setter values
                    for (int j = 0; j < trigger.PropertyValues.Count; j++)
                    {
                        PropertyValue propertyValue = trigger.PropertyValues[j];

                        // Check for trigger rules that act on container
                        if (propertyValue.ChildName != StyleHelper.SelfName)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.StyleTriggersCannotTargetTheTemplate));
                        }

                        TriggerCondition[] conditions = propertyValue.Conditions;
                        for (int k=0; k<conditions.Length; k++)
                        {
                            if( conditions[k].SourceName != StyleHelper.SelfName )
                            {
                                throw new InvalidOperationException(SR.Get(SRID.TriggerOnStyleNotAllowedToHaveSource, conditions[k].SourceName));
                            }
                        }

                        // Track properties on the container that are being driven by
                        // the Style so that they can be invalidated during style changes
                        StyleHelper.AddContainerDependent(propertyValue.Property, true /*fromVisualTrigger*/, ref this.ContainerDependents);

                        StyleHelper.UpdateTables(ref propertyValue, ref ChildRecordFromChildIndex,
                            ref TriggerSourceRecordFromChildIndex, ref ResourceDependents, ref _dataTriggerRecordFromBinding,
                            null /*_childIndexFromChildID*/, ref _hasInstanceValues);
                    }

                    // Set things up to handle TriggerActions
                    if( trigger.HasEnterActions || trigger.HasExitActions )
                    {
                        if( trigger is Trigger )
                        {
                            StyleHelper.AddPropertyTriggerWithAction( trigger, ((Trigger)trigger).Property, ref this.PropertyTriggersWithActions );
                        }
                        else if( trigger is MultiTrigger )
                        {
                            MultiTrigger multiTrigger = (MultiTrigger)trigger;
                            for( int k = 0; k < multiTrigger.Conditions.Count; k++ )
                            {
                                Condition triggerCondition = multiTrigger.Conditions[k];

                                StyleHelper.AddPropertyTriggerWithAction( trigger, triggerCondition.Property, ref this.PropertyTriggersWithActions );
                            }
                        }
                        else if( trigger is DataTrigger )
                        {
                            StyleHelper.AddDataTriggerWithAction( trigger, ((DataTrigger)trigger).Binding, ref this.DataTriggersWithActions );
                        }
                        else if( trigger is MultiDataTrigger )
                        {
                            MultiDataTrigger multiDataTrigger = (MultiDataTrigger)trigger;
                            for( int k = 0; k < multiDataTrigger.Conditions.Count; k++ )
                            {
                                Condition dataCondition = multiDataTrigger.Conditions[k];

                                StyleHelper.AddDataTriggerWithAction( trigger, dataCondition.Binding, ref this.DataTriggersWithActions );
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Get(SRID.UnsupportedTriggerInStyle, trigger.GetType().Name));
                        }
                    }

                    // Set things up to handle EventTrigger
                    EventTrigger eventTrigger = trigger as EventTrigger;
                    if( eventTrigger != null )
                    {
                        if( eventTrigger.SourceName != null && eventTrigger.SourceName.Length > 0 )
                        {
                            throw new InvalidOperationException(SR.Get(SRID.EventTriggerOnStyleNotAllowedToHaveTarget, eventTrigger.SourceName));
                        }

                        StyleHelper.ProcessEventTrigger(eventTrigger,
                                                        null /*_childIndexFromChildID*/,
                                                        ref _triggerActions,
                                                        ref EventDependents,
                                                        null /*_templateFactoryRoot*/,
                                                        null,
                                                        ref _eventHandlersStore,
                                                        ref _hasLoadedChangeHandler);
                    }
                }
            }
        }

        /// <summary>
        ///     Serves as a hash function for a particular type, suitable for use in
        ///     hashing algorithms and data structures like a hash table
        /// </summary>
        /// <returns>The Style's GlobalIndex</returns>
        public override int GetHashCode()
        {
            // Verify Context Access
            VerifyAccess();

            return GlobalIndex;
        }

        #region ISealable

        /// <summary>
        /// Can this style be sealed
        /// </summary>
        bool ISealable.CanSeal
        {
            get { return true; }
        }

        /// <summary>
        /// Is this style sealed
        /// </summary>
        bool ISealable.IsSealed
        {
            get { return IsSealed; }
        }

        /// <summary>
        /// Seal this style
        /// </summary>
        void ISealable.Seal()
        {
            Seal();
        }

        #endregion ISealable

        internal bool HasResourceReferences
        {
            get
            {
                return ResourceDependents.Count > 0;
            }
        }

        /// <summary>
        ///     Store all the event handlers for this Style TargetType
        /// </summary>
        internal EventHandlersStore EventHandlersStore
        {
            get { return _eventHandlersStore; }
        }

        /// <summary>
        ///     Does the current style or any of its template children
        ///     have any event setters OR event triggers
        /// </summary>
        internal bool HasEventDependents
        {
            get
            {
                return (EventDependents.Count > 0);
            }
        }

        /// <summary>
        ///     Does the current style or any of its template children
        ///     have event setters, ignoring event triggers.
        /// </summary>
        internal bool HasEventSetters
        {
            get
            {
                return IsModified(HasEventSetter);
            }
        }

        //
        //  Says if this style contains any per-instance values
        //
        internal bool HasInstanceValues
        {
            get { return _hasInstanceValues; }
        }

        //
        // Says if we have anything listening for the Loaded or Unloaded
        // event (used for an optimization in FrameworkElement).
        //

        internal bool HasLoadedChangeHandler
        {
            get { return _hasLoadedChangeHandler; }
            set { _hasLoadedChangeHandler = value; }
        }

        // Special equality check that takes into account 'null'
        private static bool IsEqual(object a, object b)
        {
            return (a != null) ? a.Equals(b) : (b == null);
        }

        internal bool IsBasedOnModified { get { return IsModified(BasedOnID); } }

        private EventHandlersStore _eventHandlersStore = null;

        private bool _sealed;
        private bool _hasInstanceValues;

        internal static readonly Type DefaultTargetType = typeof(IFrameworkInputElement);

        private bool _hasLoadedChangeHandler;

        private Type _targetType = DefaultTargetType;
        private Style _basedOn;

        private TriggerCollection _visualTriggers = null;

        private SetterBaseCollection _setters = null;

        // Holds resources that are applicable to the container
        // of this style and its sub-tree.
        internal ResourceDictionary _resources = null;

        /* property */ internal int GlobalIndex;

        // Style tables
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock
        /* property */ internal FrugalStructList<ChildRecord> ChildRecordFromChildIndex = new FrugalStructList<ChildRecord>(); // Indexed by Child.ChildIndex
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock

        //
        // Shared tables used during OnTriggerSourcePropertyInvalidated
        //
        internal FrugalStructList<ItemStructMap<TriggerSourceRecord>> TriggerSourceRecordFromChildIndex = new FrugalStructList<ItemStructMap<TriggerSourceRecord>>();
        // Dictionary of property triggers that have TriggerActions, keyed via DP.GlobalIndex affecting those triggers.
        //  Each trigger can be listed multiple times, if they are dependent on multiple properties.
        internal FrugalMap PropertyTriggersWithActions;

        // Original Style data (not including based-on data)
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock
        /* property */ internal FrugalStructList<System.Windows.PropertyValue> PropertyValues = new FrugalStructList<System.Windows.PropertyValue>();

        // Properties driven on the container (by the Style) that should be
        // invalidated when the style gets applied/unapplied. These properties
        // could have been set via Style.SetValue or TriggerBase.SetValue
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock
        /* property */ internal FrugalStructList<ContainerDependent> ContainerDependents = new FrugalStructList<ContainerDependent>();

        // Properties driven by a resource that should be invalidated
        // when a resource dictionary changes
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock
        /* property */ internal FrugalStructList<ChildPropertyDependent> ResourceDependents = new FrugalStructList<ChildPropertyDependent>();

        // Events driven by a this style. An entry for every childIndex that has associated events.
        // childIndex '0' is used to represent events set on the style's TargetType. This data-structure
        // will be frequently looked up during event routing.
        // Synchronized (write locks, lock-free reads): Covered by Style instance lock
        /* property */ internal ItemStructList<ChildEventDependent> EventDependents = new ItemStructList<ChildEventDependent>(1);

        // Used by EventTrigger: Maps a RoutedEventID to a set of TriggerAction objects
        //  to be performed.
        internal HybridDictionary _triggerActions = null;

        // Data trigger information.  An entry for each Binding that appears in a
        // condition of a data trigger.
        // Synchronized: Covered by Style instance
        internal HybridDictionary _dataTriggerRecordFromBinding;

        // An entry for each Binding that appears in a DataTrigger with EnterAction or ExitAction
        //  This overlaps but should not be the same as _dataTriggerRecordFromBinding above:
        //   A DataTrigger can have Setters but no EnterAction/ExitAction.  (The reverse can also be true.)
        internal HybridDictionary DataTriggersWithActions = null;

        // Unique index for every instance of Style
        // Synchronized: Covered by Style.Synchronized
        private static int StyleInstanceCount = 0;

        // Global, cross-object synchronization
        internal static object Synchronized = new object();

        private const int TargetTypeID = 0x01;
        internal const int BasedOnID    = 0x02;

        // Using the modified flags to note whether we have an EventSetter.
        private const int HasEventSetter = 0x10;

        private int _modified = 0;

        private void SetModified(int id) { _modified |= id; }
        internal bool IsModified(int id) { return (id & _modified) != 0; }
    }
}

