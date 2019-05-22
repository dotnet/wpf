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
    ///     This class is a custom type descriptor for dependency properties.  We could simply
    ///     derive from the CustomTypeDescriptor class, but because these are allocated a lot
    ///     we make them a struct so they are not on the heap.
    /// </summary>
    internal struct DPCustomTypeDescriptor : ICustomTypeDescriptor {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a new DPCustomTypeDescriptor.  We pass in the custom type descriptor of
        ///     our base provider, which provides is with a default implementation of everything
        ///     we don't override.  for us, we want to override only the property mechanism.
        /// </summary>
        internal DPCustomTypeDescriptor(ICustomTypeDescriptor parent, Type objectType, object instance) 
        {
            _parent = parent;
            _objectType = objectType;
            _instance = instance;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Returns the component name.  To do this, we try to find the
        ///     RuntimeNamePropertyAttribute on the type.  If we find
        ///     the attribute, we will try to invoke the property to retrieve
        ///     the component name.  If any of these fail, we defer to the
        ///     parent implementation.
        /// </summary>
        public string GetComponentName()
        {
            if (_instance != null) 
            {
                RuntimeNamePropertyAttribute nameAttr = GetAttributes()[typeof(RuntimeNamePropertyAttribute)] as RuntimeNamePropertyAttribute;
                if (nameAttr != null && nameAttr.Name != null) 
                {
                    PropertyDescriptor nameProp = GetProperties()[nameAttr.Name];
                    if (nameProp != null) 
                    {
                        return nameProp.GetValue(_instance) as string;
                    }
                }
            }

            return _parent.GetComponentName();
        }

        /// <summary>
        ///     Returns a collection of properties for our object.
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

            // If no filter is set, or if the only filter is for "invalid" properties, 
            // there's no work to do.

            if (filter == PropertyFilterOptions.None || filter == PropertyFilterOptions.Invalid) 
            {
                return PropertyDescriptorCollection.Empty;
            }

            // Value used during filtering.  Because direct properties are always
            // returned for .Valid and .All, the only case we're directly interested
            // in is when filter exactly equals SetValues.
            DependencyObject filterValue;
            if (filter == PropertyFilterOptions.SetValues) 
            {
                if (_instance == null) return PropertyDescriptorCollection.Empty;
                filterValue = (DependencyObject)TypeDescriptor.GetAssociation(_objectType, _instance);
            }
            else 
            {
                filterValue = null;
            }

            // Note:  For a property filter of "SetValues" it would be ideal if we could use
            // DependencyObject's GetLocalValueEnumerator.  Unfortunately, we can't:
            //
            // * We still need to scan properties to get the property descriptor that
            //   matches the DP.
            //
            // * The enumerator would skip CLR properties that have no backing DP.
            //
            // We can still do some optimizations.

            // First, have we already discovered properties for this type?

            PropertyDescriptorCollection properties = (PropertyDescriptorCollection)_typeProperties[_objectType];

            if (properties == null) 
            {
                properties = CreateProperties();
                
                lock (_typeProperties) 
                {
                    _typeProperties[_objectType] = properties;
                }
            }

            // Check bit combinations that would yield true in 
            // any case.  For non-attached properties, they're all valid, so if
            // the valid bit is set, we're done.


            if ((filter & _anySet) == _anySet || (filter & _anyValid) == _anyValid) 
            {
                return properties;
            }

            // The filter specifies either set or unset values.  

            Debug.Assert((filter & _anySet) == filter, "There is a filtering case we did not account for");

            List<PropertyDescriptor> newDescriptors = null;

            int cnt = properties.Count;
            for(int idx = 0; idx < cnt; idx++)
            {
                PropertyDescriptor prop = properties[idx];
                bool shouldSerialize = prop.ShouldSerializeValue(filterValue);
                bool addProp = shouldSerialize ^ ((filter & _anySet) == PropertyFilterOptions.UnsetValues);

                if (!addProp) 
                {
                    // Property should be removed.  Make sure our newDescriptors array is
                    // up to date for where we need to be
                    if (newDescriptors == null) 
                    {
                        newDescriptors = new List<PropertyDescriptor>(cnt);
                        for (int i = 0; i < idx; i++) 
                        {
                            newDescriptors.Add(properties[i]);
                        }
                    }
                }
                else if (newDescriptors != null) 
                {
                    newDescriptors.Add(prop);
                }
            }

            if (newDescriptors != null) 
            {
                properties = new PropertyDescriptorCollection(newDescriptors.ToArray(), true);
            }

            return properties;
        }

        // 
        // All methods below simply forward to the parent descriptor.
        //

        public AttributeCollection GetAttributes() { return _parent.GetAttributes(); }
        public string GetClassName() { return _parent.GetClassName(); }

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
                return new TypeConverter();
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
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        
        /// <summary>
        ///     This method is called when we should clear our cached state.  The cache
        ///     may become invalid if someone adds additional type description providers.
        /// </summary>
        internal static void ClearCache()
        {
            lock (_propertyMap) 
            {
                _propertyMap.Clear();
            }

            lock(_typeProperties)
            {
                _typeProperties.Clear();
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        //
        // Creates the property descriptor collection for this type.  The return
        // value is all properties that are exposed on this type.
        //
        private PropertyDescriptorCollection CreateProperties()
        {
            PropertyDescriptorCollection baseProps = _parent.GetProperties();
            List<PropertyDescriptor> newDescriptors = new List<PropertyDescriptor>(baseProps.Count);

            for (int idx = 0; idx < baseProps.Count; idx++) 
            {
                PropertyDescriptor prop = baseProps[idx];

                DependencyObjectPropertyDescriptor dpProp;
                DependencyProperty dp = null;

                bool inMap;
                 
                lock(_propertyMap)
                {
                    inMap = _propertyMap.TryGetValue(prop, out dpProp);
                }

                if (inMap && dpProp != null)
                {
                    // We need to verify that this property descriptor contains the correct DP.
                    // We can get the wrong one if a descendant of the type introducing the
                    // CLR property associates a different DP to itself with the same name.

                    dp = DependencyProperty.FromName(prop.Name, _objectType);
                    if (dp != dpProp.DependencyProperty)
                    {
                        dpProp = null;
                    }
                    else
                    {
                        // We also need to verify that the property metadata for dpProp matches
                        // our object type's metadata

                        if (dpProp.Metadata != dp.GetMetadata(_objectType)) 
                        {
                            dpProp = null;
                        }
                    }
                }

                if (dpProp == null) 
                {
                    // Either the property wasn't in the map or the one that was in there
                    // can't work for this type.  Make a new property if this property is
                    // backed by a DP.  Since we only care about direct dependency properties
                    // we can short circuit FromName for all properties on types that do
                    // not derive from DependencyObject.  Also, if we already got a DP out of
                    // the map we can skip the dependency object check on the property, since
                    // the fact that we got a dp means that there used to be something in the map
                    // so the component type is already a DependencyObject.

                    if (dp != null || typeof(DependencyObject).IsAssignableFrom(prop.ComponentType))
                    {
                        if (dp == null) 
                        {
                            dp = DependencyProperty.FromName(prop.Name, _objectType);
                        }

                        if (dp != null) 
                        {
                            dpProp = new DependencyObjectPropertyDescriptor(prop, dp, _objectType);
                        }
                    }

                    // Now insert the new property in our map.  Note that we will
                    // insert a null value into the map if this property descriptor
                    // had no backing DP so we don't go through this work twice.

                    if (!inMap) 
                    {
                        lock(_propertyMap)
                        {
                            _propertyMap[prop] = dpProp;
                        }
                    }
                }


                // If we found a dependency property desecriptor for this property,
                // use it as our new property.

                if (dpProp != null) 
                {
                    prop = dpProp;
                }

                newDescriptors.Add(prop);
            }

            return new PropertyDescriptorCollection(newDescriptors.ToArray(), true);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private ICustomTypeDescriptor _parent;
        private Type _objectType;
        private object _instance;

        // Synchronized by "_propertyMap"
        private static Dictionary<PropertyDescriptor, DependencyObjectPropertyDescriptor> _propertyMap = 
            new Dictionary<PropertyDescriptor, DependencyObjectPropertyDescriptor>(new PropertyDescriptorComparer());

        // Synchronized by "_typeProperties"
        private static Hashtable _typeProperties = new Hashtable();

        private const PropertyFilterOptions _anySet = PropertyFilterOptions.SetValues | PropertyFilterOptions.UnsetValues;
        private const PropertyFilterOptions _anyValid = PropertyFilterOptions.Valid;

        #endregion Private Fields
    }
}

