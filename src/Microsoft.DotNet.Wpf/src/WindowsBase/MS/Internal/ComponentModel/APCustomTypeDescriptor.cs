// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Markup;
    using System.Text;


    /// <summary>
    ///     This class is a custom type descriptor for attached dependency properties.  We
    ///     could just inherit from the CustomTypeDescriptor class, which does most of the forwarding
    ///     work for us, but these are allocated a lot so we want them to be structs.
    /// </summary>
    struct APCustomTypeDescriptor : ICustomTypeDescriptor {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a new APCustomTypeDescriptor.  We pass in the custom type descriptor of
        ///     our base provider, which provides is with a default implementation of everything
        ///     we don't override.  for us, we want to override only the property mechanism.
        /// </summary>
        internal APCustomTypeDescriptor(ICustomTypeDescriptor parent, object instance) 
        {
            _parent = parent;
            _instance = FromObj(instance);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        
        /// <summary>
        ///     Returns a collection of properties for our object.  We first rely on base
        ///     CLR properties and then we attempt to match these with dependency properties.
        /// </summary>
        public PropertyDescriptorCollection GetProperties() 
        {
            return GetProperties(null);
        }

        /// <summary>
        ///     Returns a collection of properties for our object.  We first rely on base
        ///     CLR properties and then we attempt to match these with dependency properties.
        /// </summary>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) 
        {
            // Because attached properties can come and go at any time,
            // the set of properties we have here always needs to be rebuilt.

            // We have two code paths here based on filtered attributes.  An attribute
            // filter is just a notificaiton of a filter, it doesn't actually perform
            // the filter.  Because the default PropertyFilterAttribute is PropertyFilter.All,
            // it acts as a nice "don't care" in later filtering stages that TypeDescriptor
            // may apply.  That means that regardless of the filter value, we don't have
            // to fiddle with adding the attribute to the property descriptor.

            PropertyFilterOptions filter = PropertyFilterOptions.Valid | PropertyFilterOptions.SetValues;

            if (attributes != null) 
            {
                foreach (Attribute attr in attributes) 
                {
                    PropertyFilterAttribute filterAttr = attr as PropertyFilterAttribute;
                    if (filterAttr != null) 
                    {
                        filter = filterAttr.Filter;
                        break;
                    }
                }
            }

            if (filter == PropertyFilterOptions.None) 
            {
                return PropertyDescriptorCollection.Empty;
            }

            // First, get the set of all known registered properties in the
            // app domain.  GetRegisteredProperties caches its results and
            // will automatically re-fetch if new properties have been 
            // registered
            DependencyProperty[] registeredProperties = GetRegisteredProperties();
            Type instanceType = _instance.GetType();

            // Next, walk through them and see which ones can be attached to this
            // object.  If our filter is specifically SetValues, we can 
            // greatly shortcut the entire process by using the local value
            // enumerator.

            List<PropertyDescriptor> filteredProps;
             
            if (filter == PropertyFilterOptions.SetValues) 
            {
                LocalValueEnumerator localEnum = _instance.GetLocalValueEnumerator();
                filteredProps = new List<PropertyDescriptor>(localEnum.Count);

                while(localEnum.MoveNext())
                {
                    DependencyProperty dp = localEnum.Current.Property;
                    DependencyPropertyKind kind = DependencyObjectProvider.GetDependencyPropertyKind(dp, instanceType);

                    // For locally set values, we just want to exclude direct and internal properties.
                    if (!kind.IsDirect && !kind.IsInternal)
                    {
                        DependencyObjectPropertyDescriptor dpProp = DependencyObjectProvider.GetAttachedPropertyDescriptor(dp, instanceType);
                        filteredProps.Add(dpProp);
                    }
                }
            }
            else
            {
                filteredProps = new List<PropertyDescriptor>(registeredProperties.Length);

                foreach (DependencyProperty dp in registeredProperties) 
                {
                    bool addProp = false;
                    DependencyPropertyKind kind = DependencyObjectProvider.GetDependencyPropertyKind(dp, instanceType);

                    if (kind.IsAttached)
                    {
                        // Check bit combinations that would yield true in 
                        // any case.  For non-attached properties, they're all valid, so if
                        // the valid bit is set, we're done.

                        PropertyFilterOptions anySet = PropertyFilterOptions.SetValues | PropertyFilterOptions.UnsetValues;
                        PropertyFilterOptions anyValid = PropertyFilterOptions.Valid | PropertyFilterOptions.Invalid;

                        if ((filter & anySet) == anySet || (filter & anyValid) == anyValid) 
                        {
                            addProp = true;
                        }

                        if (!addProp && (filter & anyValid) != 0) 
                        {
                            bool canAttach = CanAttachProperty(dp, _instance);
                            addProp = canAttach ^ ((filter & anyValid) == PropertyFilterOptions.Invalid);
                        }


                        if (!addProp && (filter & anySet) != 0) 
                        {
                            bool shouldSerialize = _instance.ContainsValue(dp);
                            addProp = shouldSerialize ^ ((filter & anySet) == PropertyFilterOptions.UnsetValues);
                        }
                    }
                    else if ((filter & PropertyFilterOptions.SetValues) != 0 && _instance.ContainsValue(dp) && !kind.IsDirect && !kind.IsInternal)
                    {
                        // The property is not attached.  However, it isn't an internal DP and the user
                        // has requested set values.  See if the property is set on the object and include
                        // it if it is.
                        addProp = true;
                    }

                    if (addProp) 
                    {
                        DependencyObjectPropertyDescriptor dpProp = DependencyObjectProvider.GetAttachedPropertyDescriptor(dp, instanceType);
                        filteredProps.Add(dpProp);
                    }
                }
            }

            PropertyDescriptorCollection properties;
            properties = new PropertyDescriptorCollection(filteredProps.ToArray(), true);
            return properties;
        }

        // 
        // All methods below simply forward to the parent descriptor.
        //

        public AttributeCollection GetAttributes() { return _parent.GetAttributes(); }
        public string GetClassName() { return _parent.GetClassName(); }
        public string GetComponentName() { return _parent.GetComponentName(); }

        public TypeConverter GetConverter()
        {
            // We only support public type converters, in order to avoid asserts.
            TypeConverter typeConverter = _parent.GetConverter();
            if( typeConverter.GetType().IsPublic )
            {
                return typeConverter;
            }
            else
            {
                return null;
            }
        }
        public EventDescriptor GetDefaultEvent() { return _parent.GetDefaultEvent(); }
        public PropertyDescriptor GetDefaultProperty() { return _parent.GetDefaultProperty(); }
        public object GetEditor(Type editorBaseType) { return _parent.GetEditor(editorBaseType); }
        public EventDescriptorCollection GetEvents() { return _parent.GetEvents(); }
        public EventDescriptorCollection GetEvents(Attribute[] attributes) { return _parent.GetEvents(attributes); }
        public object GetPropertyOwner(PropertyDescriptor property) { return _parent.GetPropertyOwner(property); }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     This method determines if the given property can be attached
        ///     to the given instance.
        /// </summary>
        private bool CanAttachProperty(DependencyProperty dp, DependencyObject instance)
        {
            AttachInfo info = DependencyObjectProvider.GetAttachInfo(dp);
            return info.CanAttach(instance);
        }

        /// <summary>
        ///     Returns a dependency object for the given value.  
        /// </summary>
        private static DependencyObject FromObj(object value) 
        {
            // This indirection is necessary to support
            // the "association" feature of type descriptor.  This feature
            // alows one object to mimic the API of another.
            return (DependencyObject)TypeDescriptor.GetAssociation(typeof(DependencyObject), value);
        }

        /// <summary>
        ///     Returns an array of all registered properties declared in the
        ///     system.  
        /// </summary>
        private DependencyProperty[] GetRegisteredProperties() 
        {
            DependencyProperty[] registeredProperties;

            // We keep track of the global dependency property count.
            // Because DPs are never removed, we use this value to 
            // verify if our cache of registered properties is up to date.
            // If the count doesn't match our cached count, we re-fetch
            // all registered properties.

            lock(_syncLock) 
            {
                int cacheCnt = _dpCacheCount;
                int currentCnt = DependencyProperty.RegisteredPropertyCount;

                if (_dpCacheArray == null || cacheCnt != currentCnt) 
                {
                    List<DependencyProperty> dpList = new List<DependencyProperty>(currentCnt);
                    lock(DependencyProperty.Synchronized) 
                    {
                        foreach(DependencyProperty dp in DependencyProperty.RegisteredProperties) 
                        {
                            dpList.Add(dp);
                        }

                        _dpCacheCount = DependencyProperty.RegisteredPropertyCount;
                        _dpCacheArray = dpList.ToArray();
                    }
                }

                registeredProperties = _dpCacheArray;
            }

            return registeredProperties;
        }

        #endregion Private Methods
    
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        

        private ICustomTypeDescriptor _parent;
        private DependencyObject _instance;

        private static object _syncLock = new object();

        // Synchronized by "_syncLock"
        private static int _dpCacheCount = 0;
        
        // Synchronized by "_syncLock"
        private static DependencyProperty[] _dpCacheArray;

        #endregion Private Fields
    }
}

