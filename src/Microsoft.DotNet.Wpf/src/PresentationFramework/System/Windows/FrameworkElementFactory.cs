// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Baml2006;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using System.Collections;               // For ArrayList
using System.Collections.Generic;
using System.Collections.Specialized;   // HybridDictionary
using System.Diagnostics;               // For Debug.Assert
using System.Globalization;
using System.Windows.Media.Media3D;
using MS.Utility;
using MS.Internal;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows
{
    /// <summary>
    ///     Templating instance representation
    /// </summary>
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class FrameworkElementFactory
    {
        /// <summary>
        ///     Construction
        /// </summary>
        public FrameworkElementFactory() : this(null, null)
        {
            // Forwarding
        }

        /// <summary>
        ///     Construction
        /// </summary>
        /// <param name="type">Type of instance to create</param>
        public FrameworkElementFactory(Type type) : this(type, null)
        {
            // Forwarding
        }

        /// <summary>
        ///     Construction
        /// </summary>
        /// <param name="text">Text to add</param>
        public FrameworkElementFactory(string text) : this(null, null)
        {
            Text = text;
        }

        /// <summary>
        ///     Construction
        /// </summary>
        /// <param name="type">Type of instance to create</param>
        /// <param name="name">Style identifier</param>
        public FrameworkElementFactory(Type type, string name)
        {
            Type = type;
            Name = name;
        }


        /// <summary>
        ///     Type of object that the factory will produce
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
                }

                if (_text != null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.FrameworkElementFactoryCannotAddText));
                }

                if ( value != null ) // We allow null up until Seal
                {
                    // If non-null, must be derived from one of the supported types
                    if (!typeof(FrameworkElement).IsAssignableFrom(value) &&
                        !typeof(FrameworkContentElement).IsAssignableFrom(value) &&
                        !typeof(Visual3D).IsAssignableFrom(value))
                    {
                        #pragma warning suppress 6506 // value is obviously not null
                        throw new ArgumentException(SR.Get(SRID.MustBeFrameworkOr3DDerived, value.Name));
                    }
                }

                // It is possible that _type is null when a FEF is created for text content within a tag
                _type = value;

                // If this is a KnownType in the BamlSchemaContext, then there is a faster way to create
                // an instance of that type than using Activator.CreateInstance.  So in that case
                // save the delegate for later creation.
                WpfKnownType knownType = null;
                if (_type != null)
                {
                    knownType = XamlReader.BamlSharedSchemaContext.GetKnownXamlType(_type) as WpfKnownType;
                }
                _knownTypeFactory = (knownType != null) ? knownType.DefaultConstructor : null;
            }
        }

        /// <summary>
        ///     Text string that the factory will produce
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
                }

                if (_firstChild != null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.FrameworkElementFactoryCannotAddText));
                }

                if ( value == null )
                {
                    throw new ArgumentNullException("value");
                }

                _text = value;
            }
        }

        /// <summary>
        ///     Style identifier
        /// </summary>
        public string Name
        {
            get { return _childName; }
            set
            {
                if (_sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
                }
                if (value == string.Empty)
                {
                    throw new ArgumentException(SR.Get(SRID.NameNotEmptyString));
                }

                _childName = value;
            }
        }


        /// <summary>
        ///     Add a factory child to this factory
        /// </summary>
        /// <param name="child">Child to add</param>
        public void AppendChild(FrameworkElementFactory child)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
            }

            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            if (child._parent != null)
            {
                throw new ArgumentException(SR.Get(SRID.FrameworkElementFactoryAlreadyParented));
            }

            if (_text != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.FrameworkElementFactoryCannotAddText));
            }

            // Build tree of factories
            if (_firstChild == null)
            {
                _firstChild = child;
                _lastChild = child;
            }
            else
            {
                _lastChild._nextSibling = child;
                _lastChild = child;
            }

            child._parent = this;
        }

        /// <summary>
        ///     Simple value set on template child
        /// </summary>
        /// <param name="dp">Dependent property</param>
        /// <param name="value">Value to set</param>
        public void SetValue(DependencyProperty dp, object value)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
            }

            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            // Value needs to be valid for the DP, or Binding/MultiBinding/PriorityBinding.
            //  (They all have MarkupExtension, which we don't actually support, see above check.)

            if (!dp.IsValidValue(value) && !(value is MarkupExtension) && !(value is DeferredReference))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPropertyValue, value, dp.Name));
            }

            // Styling the logical tree is not supported
            if (StyleHelper.IsStylingLogicalTree(dp, value))
            {
                throw new NotSupportedException(SR.Get(SRID.ModifyingLogicalTreeViaStylesNotImplemented, value, "FrameworkElementFactory.SetValue"));
            }

            #pragma warning suppress 6506 // dp.DefaultMetadata is never null
            if (dp.ReadOnly)
            {
                // Read-only properties will not be consulting FrameworkElementFactory for value.
                //  Rather than silently do nothing, throw error.
                throw new ArgumentException(SR.Get(SRID.ReadOnlyPropertyNotAllowed, dp.Name, GetType().Name));
            }

            ResourceReferenceExpression resourceExpression = value as ResourceReferenceExpression;
            DynamicResourceExtension dynamicResourceExtension = value as DynamicResourceExtension;
            object resourceKey = null;

            if( resourceExpression != null )
            {
                resourceKey = resourceExpression.ResourceKey;
            }
            else if( dynamicResourceExtension != null )
            {
                resourceKey = dynamicResourceExtension.ResourceKey;
            }

            if (resourceKey == null)
            {
                TemplateBindingExtension templateBinding = value as TemplateBindingExtension;
                if (templateBinding == null)
                {
                    UpdatePropertyValueList( dp, PropertyValueType.Set, value );
                }
                else
                {
                    UpdatePropertyValueList( dp, PropertyValueType.TemplateBinding, templateBinding );
                }
            }
            else
            {
                UpdatePropertyValueList(dp, PropertyValueType.Resource, resourceKey);
            }
        }

        /// <summary>
        ///     Set up data binding on template child
        /// </summary>
        /// <param name="dp">Dependent property</param>
        /// <param name="binding">Description of binding</param>
        public void SetBinding(DependencyProperty dp, BindingBase binding)
        {
            // store Binding in the style - this will get converted to Binding
            // on demand (see Style.ProcessApplyValuesHelper)
            SetValue(dp, binding);
        }

        /// <summary>
        ///     Resource binding on template child
        /// </summary>
        /// <param name="dp">Dependent property</param>
        /// <param name="name">Resource identifier</param>
        public void SetResourceReference(DependencyProperty dp, object name)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
            }

            if (dp == null)
            {
                throw new ArgumentNullException("dp");
            }

            UpdatePropertyValueList( dp, PropertyValueType.Resource, name );
        }

        /// <summary>
        ///     Add an event handler for the given routed event. This action applies to the instances created by this factory
        /// </summary>
        public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        {
            // HandledEventToo defaults to false
            // Call forwarded
            AddHandler(routedEvent, handler, false);
        }

        /// <summary>
        ///     Add an event handler for the given routed event. This action applies to the instances created by this factory
        /// </summary>
        public void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
            }

            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (handler.GetType() != routedEvent.HandlerType)
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }

            if (_eventHandlersStore == null)
            {
                _eventHandlersStore = new EventHandlersStore();
            }

            _eventHandlersStore.AddRoutedEventHandler(routedEvent, handler, handledEventsToo);

            // Keep track of whether we're listening to the loaded or unloaded events;
            // if so, we have to trigger a listener in the FE/FCE (as a performance
            // optimization).

            if (  (routedEvent == FrameworkElement.LoadedEvent)
                ||(routedEvent == FrameworkElement.UnloadedEvent))
            {
                HasLoadedChangeHandler = true;
            }
        }

        /// <summary>
        ///     Remove an event handler for the given routed event. This action applies to the instances created by this factory
        /// </summary>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "FrameworkElementFactory"));
            }

            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (handler.GetType() != routedEvent.HandlerType)
            {
                throw new ArgumentException(SR.Get(SRID.HandlerTypeIllegal));
            }

            if (_eventHandlersStore != null)
            {
                _eventHandlersStore.RemoveRoutedEventHandler(routedEvent, handler);

                // Update the loaded/unloaded optimization flags if necessary

                if (  (routedEvent == FrameworkElement.LoadedEvent)
                    ||(routedEvent == FrameworkElement.UnloadedEvent))
                {
                    if (  !_eventHandlersStore.Contains(FrameworkElement.LoadedEvent)
                        &&!_eventHandlersStore.Contains(FrameworkElement.UnloadedEvent))
                    {
                        HasLoadedChangeHandler = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Store all the event handlers for this FEF
        /// </summary>
        internal EventHandlersStore EventHandlersStore
        {
            get
            {
                return _eventHandlersStore;
            }
            set
            {
                _eventHandlersStore = value;
            }
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
                lock (_synchronized)
                {
                    PropertyValue propertyValue = PropertyValues[existingIndex];
                    propertyValue.ValueType = valueType;
                    propertyValue.ValueInternal = value;
                    // Put back modified struct
                    PropertyValues[existingIndex] = propertyValue;
                }
            }
            else
            {
                // Store original data
                PropertyValue propertyValue = new PropertyValue();
                propertyValue.ValueType = valueType;
                propertyValue.ChildName = null;  // Delayed
                propertyValue.Property = dp;
                propertyValue.ValueInternal = value;

                lock (_synchronized)
                {
                    PropertyValues.Add(propertyValue);
                }
            }
        }

        /// <summary>
        ///     Create a DependencyObject instance of the specified type
        /// </summary>
        /// <remarks>
        ///     By default, reflection is used to create the instance. For
        ///     best perf, override this and do specific construction
        /// </remarks>
        /// <returns>New instance</returns>
        private DependencyObject CreateDependencyObject()
        {
            // Not expecting InvalidCastException - Type.set should have
            //  verified that it is a FrameworkElement or FrameworkContentElement.

            if (_knownTypeFactory != null)
            {
                return _knownTypeFactory.Invoke() as DependencyObject;
            }

            return (DependencyObject)Activator.CreateInstance(_type);
        }

        /// <summary>
        ///     FrameworkElementFactory mutability state
        /// </summary>
        public bool IsSealed
        {
            get { return _sealed; }
        }


        /// <summary>
        ///     Parent factory
        /// </summary>
        public FrameworkElementFactory Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///     First child factory
        /// </summary>
        public FrameworkElementFactory FirstChild
        {
            get { return _firstChild; }
        }

        /// <summary>
        ///     Next sibling factory
        /// </summary>
        public FrameworkElementFactory NextSibling
        {
            get { return _nextSibling; }
        }

        internal FrameworkTemplate FrameworkTemplate
        {
            get { return _frameworkTemplate; }
        }


        internal object GetValue(DependencyProperty dp)
        {
            // Retrieve a value previously set

            // Scan for record
            for (int i = 0; i < PropertyValues.Count; i++)
            {
                if (PropertyValues[i].ValueType == PropertyValueType.Set &&
                    PropertyValues[i].Property == dp)
                {
                    // Found a Set record, return the value
                    return PropertyValues[i].ValueInternal;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        // Seal this FEF
        internal void Seal(FrameworkTemplate ownerTemplate)
        {
            if (_sealed)
            {
                return;
            }

            // Store owner Template
            _frameworkTemplate = ownerTemplate;

            // Perform actual Seal operation
            Seal();
        }

        private void Seal()
        {
            if (_type == null && _text == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.NullTypeIllegal));
            }

            if (_firstChild != null)
            {
                // This factory has children, it must implement IAddChild so that these
                // children can be added to the logical tree
                if (!typeof(IAddChild).IsAssignableFrom(_type))
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMustImplementIAddChild, _type.Name));
                }
            }

            ApplyAutoAliasRules();

            if ((_childName != null) && (_childName != String.Empty))
            {
                // ChildName provided
                if (!IsChildNameValid(_childName))
                {
                    throw new InvalidOperationException(SR.Get(SRID.ChildNameNamePatternReserved, _childName));
                }

                _childName = String.Intern(_childName);
            }
            else
            {
                // ChildName not provided

                _childName = GenerateChildName();
            }


            lock (_synchronized)
            {
                // Set delayed ChildID for all property triggers
                for (int i = 0; i < PropertyValues.Count; i++)
                {
                    PropertyValue propertyValue = PropertyValues[i];
                    propertyValue.ChildName = _childName;

                    // Freeze the FEF property value
                    StyleHelper.SealIfSealable(propertyValue.ValueInternal);

                    // Put back modified struct
                    PropertyValues[i] = propertyValue;
                }
            }


            _sealed = true;

            // Convert ChildName to Template-specific ChildIndex, if applicable
            if ((_childName != null) && (_childName != String.Empty) &&
                _frameworkTemplate != null )
            {
                _childIndex = StyleHelper.CreateChildIndexFromChildName(_childName, _frameworkTemplate);
            }

            // Seal all children
            FrameworkElementFactory child = _firstChild;
            while (child != null)
            {
                if (_frameworkTemplate != null)
                {
                    child.Seal(_frameworkTemplate);
                }

                child = child._nextSibling;
            }
        }

        // Instantiate a tree.  This is a recursive routine that will build the
        //  subtree via calls to itself.  The root node being instantiated will
        //  have identical references for the "container" and "parent" parameters.
        // The "affectedChildren" and "noChildIndexChildren" parameters refer to the children
        //  chain for the "container" object.  This chain will have all the
        //  children - not just the immediate children.  The node being
        //  instantiated here will be added to this chain.
        // The tree is instantiated in a depth-first traversal, so children nodes
        //  are added to the chain in depth-first order as well.
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal DependencyObject InstantiateTree(
                UncommonField<HybridDictionary[]>           dataField,
                DependencyObject                            container,
                DependencyObject                            parent,
                List<DependencyObject>                      affectedChildren,
            ref List<DependencyObject>                      noChildIndexChildren,
            ref FrugalStructList<ChildPropertyDependent>    resourceDependents)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseFefCrInstBegin);

            FrameworkElement containerAsFE = container as FrameworkElement;
            bool isContainerAnFE = containerAsFE != null;

            DependencyObject treeNode = null;
            // If we have text, just add it to the parent.  Otherwise create the child
            // subtree
            if (_text != null)
            {
                // of FrameworkContentElement parent.  This is the logical equivalent
                // to what happens when adding a child to a visual collection.
                IAddChild addChildParent = parent as IAddChild;

                if (addChildParent == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMustImplementIAddChild,
                                                         parent.GetType().Name));
                }
                else
                {
                    addChildParent.AddText(_text);
                }
            }
            else
            {
                // Factory create instance
                treeNode = CreateDependencyObject();

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseFefCrInstEnd);

                // The tree node is either a FrameworkElement or a FrameworkContentElement.
                //  we'll deal with one or the other...
                FrameworkObject treeNodeFO = new FrameworkObject(treeNode);

                Visual3D treeNodeVisual3D = null;
                bool treeNodeIsVisual3D = false;

                if (!treeNodeFO.IsValid)
                {
                    // If it's neither of those, we have special support for Visual3D

                    treeNodeVisual3D = treeNode as Visual3D;
                    if (treeNodeVisual3D != null)
                        treeNodeIsVisual3D = true;
                }

                Debug.Assert( treeNodeFO.IsValid || (treeNodeVisual3D != null),
                    "We should not be trying to instantiate a node that is neither FrameworkElement nor FrameworkContentElement.  A type check should have been done when Type is set");

                // And here's the bool we'll use to make the decision.
                bool treeNodeIsFE = treeNodeFO.IsFE;

                // Handle FE/FCE-specific optimizations

                if (!treeNodeIsVisual3D)
                {
                    // Postpone "Initialized" event
                    NewNodeBeginInit( treeNodeIsFE, treeNodeFO.FE, treeNodeFO.FCE );

                    // Set the resource reference flags
                    if (StyleHelper.HasResourceDependentsForChild(_childIndex, ref resourceDependents))
                    {
                        treeNodeFO.HasResourceReference = true;
                    }

                    // Update the two chains that tracks all the nodes created
                    //  from all the FrameworkElementFactory of this Style.
                    UpdateChildChains( _childName, _childIndex,
                        treeNodeIsFE, treeNodeFO.FE, treeNodeFO.FCE,
                        affectedChildren, ref noChildIndexChildren );

                    // All FrameworkElementFactory-created elements point to the object
                    //  whose Style.VisualTree definition caused all this to occur
                    NewNodeStyledParentProperty( container, isContainerAnFE, treeNodeIsFE, treeNodeFO.FE, treeNodeFO.FCE );

                    // Initialize the per-instance data for the new element.  This
                    // needs to be done before any properties are invalidated.
                    if (_childIndex != -1)
                    {
                        Debug.Assert( _frameworkTemplate != null );

                        StyleHelper.CreateInstanceDataForChild(dataField, container, treeNode, _childIndex,
                            _frameworkTemplate.HasInstanceValues, ref _frameworkTemplate.ChildRecordFromChildIndex);
                    }

                    // If this element needs to know about the Loaded or Unloaded events, set the optimization
                    // bit in the element

                    if (HasLoadedChangeHandler)
                    {
                        BroadcastEventHelper.AddHasLoadedChangeHandlerFlagInAncestry(treeNode);
                    }
                }
                else
                {
                    if (_childName != null)
                    {
                        // Add this instance to the child index chain so that it may
                        // be tracked by the style

                        affectedChildren.Add(treeNode);
                    }
                    else
                    {
                        // Child nodes with no _childID (hence no _childIndex) are
                        //  tracked on a separate chain that will be appended to the
                        //  main chain for cleanup purposes.
                        if (noChildIndexChildren == null)
                        {
                            noChildIndexChildren = new List<DependencyObject>(4);
                        }

                        noChildIndexChildren.Add(treeNode);
                    }
                }


                // New node is initialized, build tree top down
                // (Node added before children of node)
                if (container == parent)
                {
                    // Set the NameScope on the root of the Template generated tree
                    TemplateNameScope templateNameScope = new TemplateNameScope(container);
                    NameScope.SetNameScope(treeNode, templateNameScope);

                    // This is the root of the tree
                    if (isContainerAnFE)
                    {
                        // The root is added to the Visual tree (not logical) for the
                        // case of FrameworkElement parents
                        containerAsFE.TemplateChild = treeNodeFO.FE;
                    }
                    else
                    {
                        // The root is added to the logical tree for the case
                        // of FrameworkContentElement parent.  This is the logical equivalent
                        // to what happens when adding a child to a visual collection.
                        AddNodeToLogicalTree( (FrameworkContentElement)parent, _type,
                            treeNodeIsFE, treeNodeFO.FE, treeNodeFO.FCE );
                    }
                }
                else
                {
                    // Call parent IAddChild to add treeNodeFO
                    AddNodeToParent( parent, treeNodeFO );
                }

                // Either set properties or invalidate them, depending on the type

                if (!treeNodeIsVisual3D)
                {
                    // For non-3D content, we need to invalidate any properties that
                    // came from FrameworkElementFactory.SetValue or VisulaTrigger.SetValue
                    // so that they can get picked up.

                    Debug.Assert( _frameworkTemplate != null );
                    StyleHelper.InvalidatePropertiesOnTemplateNode(
                                container,
                                treeNodeFO,
                                _childIndex,
                                ref _frameworkTemplate.ChildRecordFromChildIndex,
                                false /*isDetach*/,
                                this);

                }
                else
                {
                    // For 3D, which doesn't understand templates, we set the properties directly
                    // onto the newly-instantiated element.

                    for (int i = 0; i < PropertyValues.Count; i++)
                    {
                        if (PropertyValues[i].ValueType == PropertyValueType.Set)
                        {
                            // Get the value out of the table.
                            object o = PropertyValues[i].ValueInternal;


                            // If it's a freezable that can't be frozen, it's probably not sharable,
                            // so we make a copy of it.
                            Freezable freezableValue = o as Freezable;
                            if (freezableValue != null && !freezableValue.CanFreeze)
                            {
                                o = freezableValue.Clone();
                            }

                            // Or, if it's a markup extension, get the value
                            // to set on this property from the MarkupExtension itself.
                            MarkupExtension me = o as MarkupExtension;
                            if (me != null)
                            {
                                ProvideValueServiceProvider serviceProvider = new ProvideValueServiceProvider();
                                serviceProvider.SetData( treeNodeVisual3D, PropertyValues[i].Property );
                                o = me.ProvideValue( serviceProvider );
                            }

                            // Finally, set the value onto the object.
                            treeNodeVisual3D.SetValue(PropertyValues[i].Property, o);

                        }

                        else
                        {
                            // We don't support resource references, triggers, etc within the 3D content
                            throw new NotSupportedException(SR.Get(SRID.Template3DValueOnly, PropertyValues[i].Property) );

                        }

                    }
                }


                // Build child tree from factories
                FrameworkElementFactory childFactory = _firstChild;
                while (childFactory != null)
                {
                    childFactory.InstantiateTree(
                        dataField,
                        container,
                        treeNode,
                        affectedChildren,
                        ref noChildIndexChildren,
                        ref resourceDependents);

                    childFactory = childFactory._nextSibling;
                }

                if (!treeNodeIsVisual3D)
                {
                    // Fire "Initialized" event
                    NewNodeEndInit( treeNodeIsFE, treeNodeFO.FE, treeNodeFO.FCE );
                }
            }
            return treeNode;
        }


        //
        //  Add a child to a parent, using IAddChild.  This has special support for Grid,
        //  to allow backward compatibility with FEF-based templates that have Column/RowDefinition
        //  children directly  under the Grid.
        //

        private void AddNodeToParent( DependencyObject parent, FrameworkObject childFrameworkObject )
        {
            Grid parentGrid;
            ColumnDefinition childNodeColumnDefinition;
            RowDefinition childNodeRowDefinition = null;

            if (    childFrameworkObject.IsFCE
                &&  (parentGrid = parent as Grid) != null
                &&  (   (childNodeColumnDefinition = childFrameworkObject.FCE as ColumnDefinition) != null
                    ||  (childNodeRowDefinition = childFrameworkObject.FCE as RowDefinition) != null  )
                )
            {
                if (childNodeColumnDefinition != null)
                {
                    parentGrid.ColumnDefinitions.Add(childNodeColumnDefinition);
                }
                else if (childNodeRowDefinition != null)
                {
                    parentGrid.RowDefinitions.Add(childNodeRowDefinition);
                }
            }
            else
            {
                // CALLBACK
                // Inheritable property invalidations will occur due to
                //   OnParentChanged resulting from AddChild

                if (!(parent is IAddChild))
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMustImplementIAddChild,
                                                         parent.GetType().Name));
                }

                ((IAddChild)parent).AddChild(childFrameworkObject.DO);
            }
        }


        // This tree is used to instantiate a tree, represented by this factory.
        // It is instantiated as a normal tree without any template optimizations.
        // This is used by designers to inspect a template.

        internal FrameworkObject InstantiateUnoptimizedTree()
        {

            if (!_sealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.FrameworkElementFactoryMustBeSealed));
            }

            // Create the object.

            FrameworkObject frameworkObject = new FrameworkObject(CreateDependencyObject());

            // Mark the beginning of initialization

            frameworkObject.BeginInit();

            // Set values for this object, taking them from the shared values table.

            ProvideValueServiceProvider provideValueServiceProvider = null;
            FrameworkTemplate.SetTemplateParentValues( Name, frameworkObject.DO, _frameworkTemplate, ref provideValueServiceProvider );

            // Get the first child

            FrameworkElementFactory childFactory = _firstChild;

            // If we have children, get this object's IAddChild, because it's going to be a parent.

            IAddChild iAddChild = null;
            if( childFactory != null )
            {
                iAddChild = frameworkObject.DO as IAddChild;
                if (iAddChild == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMustImplementIAddChild,
                                                         frameworkObject.DO.GetType().Name));
                }
            }

            // Build the children.

            while (childFactory != null)
            {

                if (childFactory._text != null)
                {
                    iAddChild.AddText(childFactory._text);

                }
                else
                {
                    // Use frameworkObject's IAddChild to add this node.
                    FrameworkObject childFrameworkObject = childFactory.InstantiateUnoptimizedTree();
                    AddNodeToParent(frameworkObject.DO, childFrameworkObject );
                }

                childFactory = childFactory._nextSibling;
            }

            // Mark the end of the initialization phase

            frameworkObject.EndInit();

            return frameworkObject;

        }


        /// <summary>
        ///     Update the chain of FrameworkElementFactory-created nodes.
        /// </summary>
        /// <remarks>
        ///     We have two collections of child nodes created from all the
        /// FrameworkElementFactory in a single Style.  Some we "care about"
        /// because of property values, triggers, etc.  Others just needed
        /// to be created and put in a tree, and we can stop worrying about
        /// them.  The former is 'affectedChildren', the latter is
        /// 'noChildIndexChildren' so called because the nodes we don't
        /// care about were not assigned a child index.
        /// </remarks>
        private static void UpdateChildChains( string childID, int childIndex,
            bool treeNodeIsFE, FrameworkElement treeNodeFE, FrameworkContentElement treeNodeFCE,
            List<DependencyObject> affectedChildren, ref List<DependencyObject> noChildIndexChildren )
        {
            if (childID != null)
            {
                // If a child ID exists, then, a valid child index exists as well
                if( treeNodeIsFE )
                {
                    treeNodeFE.TemplateChildIndex = childIndex;
                }
                else
                {
                    treeNodeFCE.TemplateChildIndex = childIndex;
                }

                // Add this instance to the child index chain so that it may
                // be tracked by the style

                affectedChildren.Add(treeNodeIsFE ? (DependencyObject)treeNodeFE : (DependencyObject)treeNodeFCE);
            }
            else
            {
                // Child nodes with no _childID (hence no _childIndex) are
                //  tracked on a separate chain that will be appended to the
                //  main chain for cleanup purposes.
                if (noChildIndexChildren == null)
                {
                    noChildIndexChildren = new List<DependencyObject>(4);
                }

                noChildIndexChildren.Add(treeNodeIsFE ? (DependencyObject)treeNodeFE : (DependencyObject)treeNodeFCE);
            }
        }

        /// <summary>
        ///     Call BeginInit on the newly-created node to postpone the
        /// "Initialized" event.
        /// </summary>
        internal static void NewNodeBeginInit( bool treeNodeIsFE,
            FrameworkElement treeNodeFE, FrameworkContentElement treeNodeFCE )
        {
            if( treeNodeIsFE )
            {
                // Mark the beginning of the initialization phase
                treeNodeFE.BeginInit();
            }
            else
            {
                // Mark the beginning of the initialization phase
                treeNodeFCE.BeginInit();
            }
        }

        /// <summary>
        ///     Call EndInit on the newly-created node to fire the
        /// "Initialized" event.
        /// </summary>
        private static void NewNodeEndInit( bool treeNodeIsFE,
            FrameworkElement treeNodeFE, FrameworkContentElement treeNodeFCE )
        {
            if( treeNodeIsFE )
            {
                // Mark the beginning of the initialization phase
                treeNodeFE.EndInit();
            }
            else
            {
                // Mark the beginning of the initialization phase
                treeNodeFCE.EndInit();
            }
        }

        /// <summary>
        ///     Setup the pointers on this FrameworkElementFactory-created
        /// node so we can find our way back to the object whose Style
        /// included this FrameworkElementFactory.
        /// </summary>
        private static void NewNodeStyledParentProperty(
            DependencyObject container, bool isContainerAnFE,
            bool treeNodeIsFE, FrameworkElement treeNodeFE, FrameworkContentElement treeNodeFCE)
        {
            if( treeNodeIsFE )
            {
                treeNodeFE._templatedParent = container ;
                treeNodeFE.IsTemplatedParentAnFE = isContainerAnFE;
            }
            else
            {
                treeNodeFCE._templatedParent = container ;
                treeNodeFCE.IsTemplatedParentAnFE = isContainerAnFE;
            }
        }

        /// <summary>
        ///     When Style.VisualTree is applied to a FrameworkContentElement,
        /// it is actually added to the logical tree because FrameworkContentElement
        /// has no visual tree.
        /// </summary>
        /// <remarks>
        ///     A prime example of trying to shove a square peg into a round hole.
        /// </remarks>
        internal static void AddNodeToLogicalTree( DependencyObject parent, Type type,
            bool treeNodeIsFE, FrameworkElement treeNodeFE, FrameworkContentElement treeNodeFCE)
        {
            // If the logical parent already has children, then we can't add
            // a logical subtree from the style, since there would be a conflict.
            // Throw an exception in this case.
            FrameworkContentElement logicalParent = parent as FrameworkContentElement;
            if (logicalParent != null)
            {
                IEnumerator childEnumerator = logicalParent.LogicalChildren;
                if (childEnumerator != null && childEnumerator.MoveNext())
                {
                    throw new InvalidOperationException(SR.Get(SRID.AlreadyHasLogicalChildren,
                                                          parent.GetType().Name));
                }
            }

            IAddChild  addChildParent = parent as IAddChild;
            if (addChildParent == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotHookupFCERoot,
                                                          type.Name));
            }
            else
            {
                if (treeNodeFE != null)
                {
                    addChildParent.AddChild(treeNodeFE);
                }
                else
                {
                    addChildParent.AddChild(treeNodeFCE);
                }
            }
        }

        // This method is also used by XamlStyleSerializer to decide whether
        // or not to emit the Name attribute for a VisualTree node.
        internal bool IsChildNameValid(string childName)
        {
            return !childName.StartsWith(AutoGenChildNamePrefix, StringComparison.Ordinal);
        }

        private string GenerateChildName()
        {
            string childName = AutoGenChildNamePrefix + AutoGenChildNamePostfix.ToString(CultureInfo.InvariantCulture);

            Interlocked.Increment(ref AutoGenChildNamePostfix);

            return childName;
        }


        private void ApplyAutoAliasRules()
        {
            // See also StyleHelper.ApplyAutoAliasRules, which performs this auto-aliasing
            // for non-FEF (Baml) templates.

            if (typeof(ContentPresenter).IsAssignableFrom(_type))
            {
                // ContentPresenter auto-aliases Content, ContentTemplate, and
                // ContentTemplateSelector to respective properties on the
                // styled parent.

                // The prefix is obtained from the ContentSource property.
                // If this is null, user is explicitly asking for no auto-aliasing.
                object o = GetValue(ContentPresenter.ContentSourceProperty);
                string prefix = (o == DependencyProperty.UnsetValue) ? "Content" : (string)o;

                // if Content is previously set, do nothing
                if (!String.IsNullOrEmpty(prefix) && !IsValueDefined(ContentPresenter.ContentProperty))
                {
                    // find source properties, using prefix
                    Debug.Assert(_frameworkTemplate != null, "ContentPresenter is an FE and can only have a FrameworkTemplate");
                    Type targetType = _frameworkTemplate.TargetTypeInternal;

                    DependencyProperty dpContent = DependencyProperty.FromName(prefix, targetType);
                    DependencyProperty dpContentTemplate = DependencyProperty.FromName(prefix + "Template", targetType);
                    DependencyProperty dpContentTemplateSelector = DependencyProperty.FromName(prefix + "TemplateSelector", targetType);
                    DependencyProperty dpContentStringFormat = DependencyProperty.FromName(prefix + "StringFormat", targetType);

                    // if desired source for Content doesn't exist, report an error
                    if (dpContent == null && o != DependencyProperty.UnsetValue)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.MissingContentSource, prefix, targetType));
                    }

                    // auto-alias the Content property
                    if (dpContent != null)
                    {
                        SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(dpContent));
                    }

                    // auto-alias the remaining properties if none of them are previously set
                    if (!IsValueDefined(ContentPresenter.ContentTemplateProperty) &&
                        !IsValueDefined(ContentPresenter.ContentTemplateSelectorProperty) &&
                        !IsValueDefined(ContentPresenter.ContentStringFormatProperty))
                    {
                        if (dpContentTemplate != null)
                            SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(dpContentTemplate));
                        if (dpContentTemplateSelector != null)
                            SetValue(ContentPresenter.ContentTemplateSelectorProperty, new TemplateBindingExtension(dpContentTemplateSelector));
                        if (dpContentStringFormat != null)
                            SetValue(ContentPresenter.ContentStringFormatProperty, new TemplateBindingExtension(dpContentStringFormat));
                    }
                }
            }
            else if (typeof(GridViewRowPresenter).IsAssignableFrom(_type))
            {
                // GridViewRowPresenter auto-aliases Content and Columns to Content
                // property GridView.ColumnCollection property on the templated parent.

                // if Content is previously set, do nothing
                if (!IsValueDefined(GridViewRowPresenter.ContentProperty))
                {
                    // find source property
                    Debug.Assert(_frameworkTemplate != null, "GridViewRowPresenter is an FE and can only have a FrameworkTemplate");
                    Type targetType = _frameworkTemplate.TargetTypeInternal;

                    DependencyProperty dpContent = DependencyProperty.FromName("Content", targetType);

                    // auto-alias the Content property
                    if (dpContent != null)
                    {
                        SetValue(GridViewRowPresenter.ContentProperty, new TemplateBindingExtension(dpContent));
                    }
                }

                // if Columns is previously set, do nothing
                if (!IsValueDefined(GridViewRowPresenter.ColumnsProperty))
                {
                    // auto-alias the Columns property
                    SetValue(GridViewRowPresenter.ColumnsProperty, new TemplateBindingExtension(GridView.ColumnCollectionProperty));
                }
            }
        }


        private bool IsValueDefined(DependencyProperty dp)
        {
            for (int i = 0; i < PropertyValues.Count; i++)
            {
                if (PropertyValues[i].Property == dp &&
                    (PropertyValues[i].ValueType == PropertyValueType.Set ||
                     PropertyValues[i].ValueType == PropertyValueType.Resource ||
                     PropertyValues[i].ValueType == PropertyValueType.TemplateBinding))
                {
                    return true;
                }
            }

            return false;
        }


        private bool _sealed;

        // Synchronized (write locks, lock-free reads): Covered by FrameworkElementFactory instance lock
        /* property */ internal FrugalStructList<System.Windows.PropertyValue> PropertyValues = new FrugalStructList<System.Windows.PropertyValue>();

        // Store all the event handlers for this FEF
        // NOTE: We cannot use UnCommonField<T> because that uses property engine
        // storage that can be set only on a DependencyObject
        private EventHandlersStore _eventHandlersStore;

        internal bool                   _hasLoadedChangeHandler;

        private Type _type;
        private string _text;
        private Func<object> _knownTypeFactory;
        private string _childName;
        internal int _childIndex = -1; // Being used in Style.ProcessTemplateStyles

        private FrameworkTemplate _frameworkTemplate;

        // Auto-generated ChildID uniqueness
        // Synchronized: Covered by Interlocked.Increment
        private static int AutoGenChildNamePostfix = 1;
        private static string AutoGenChildNamePrefix = "~ChildID";

        private FrameworkElementFactory _parent;
        private FrameworkElementFactory _firstChild;
        private FrameworkElementFactory _lastChild;
        private FrameworkElementFactory _nextSibling;

        // Instance-based synchronization
        private object _synchronized = new object();
    }
}

