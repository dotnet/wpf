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
    using System.Globalization;
    using System.Reflection;
    using System.Windows;
    using System.Security;
    using MS.Internal.WindowsBase;


    /// <summary>
    ///     An inplementation of a property descriptor for DependencyProperties.
    ///     This supports both normal and attached properties.
    /// </summary>
    internal sealed class DependencyObjectPropertyDescriptor : PropertyDescriptor {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a new dependency property descriptor.  A note on perf:  We don't 
        ///     pass the property descriptor down as the default member descriptor here.  Doing
        ///     so takes the attributes off of the property descriptor, which can be costly if they
        ///     haven't been accessed yet.  Instead, we wait until someone needs to access our
        ///     Attributes property and demand create the attributes at that time.
        /// </summary>
        internal DependencyObjectPropertyDescriptor(PropertyDescriptor property, DependencyProperty dp, Type objectType)
            : base(dp.Name, null) 
        {
            _property = property;
            _dp = dp;

            Debug.Assert(property != null && dp != null);
            Debug.Assert(!(property is DependencyObjectPropertyDescriptor), "Wrapping a DP in a DP");

            _componentType = property.ComponentType;
            _metadata = _dp.GetMetadata(objectType);
        }

        /// <summary>
        ///     Creates a new dependency property descriptor.  A note on perf:  We don't 
        ///     pass the property descriptor down as the default member descriptor here.  Doing
        ///     so takes the attributes off of the property descriptor, which can be costly if they
        ///     haven't been accessed yet.  Instead, we wait until someone needs to access our
        ///     Attributes property and demand create the attributes at that time.
        /// </summary>
        internal DependencyObjectPropertyDescriptor(DependencyProperty dp, Type ownerType)
            : base(string.Concat(dp.OwnerType.Name, ".", dp.Name), null) 
        {
            _dp = dp;
            _componentType = ownerType;
            _metadata = _dp.GetMetadata(ownerType);
        }

        #endregion Constructors
        

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Indicates if this property's value can be reset.
        ///     We map this to true if there is a local value
        ///     on the DP.
        /// </summary>
        public override bool CanResetValue(object component) 
        {
            // All DPs that have values set can be reset
            return ShouldSerializeValue(component);
        }

        /// <summary>
        ///     Returns the value for this property.
        /// </summary>
        public override object GetValue(object component) 
        {
            DependencyObject DO = FromObj(component);
            return DO.GetValue(_dp);
        }

        /// <summary>
        ///     Attempts to reset (or clear) the value in the
        ///     DP.
        /// </summary>
        public override void ResetValue(object component) 
        {
            if (!_queriedResetMethod) 
            {
                _resetMethod = GetSpecialMethod("Reset");
                _queriedResetMethod = true;
            }

            DependencyObject DO = FromObj(component);

            if (_resetMethod != null) 
            {
                // See if we need to pass parameters to this method.  When
                // _property == null, this is an attached property and
                // the method is static.  When _property != null, this
                // is a direct property and the method is instanced.

                if (_property == null) 
                {
                    _resetMethod.Invoke(null, new object[] {DO});
                }
                else
                {
                    _resetMethod.Invoke(DO, null);
                }
            }
            else
            {
                DO.ClearValue(_dp);
            }
        }

        /// <summary>
        ///     Sets the property value on the given object.
        /// </summary>
        public override void SetValue(object component, object value) 
        {
            DependencyObject DO = FromObj(component);
            DO.SetValue(_dp, value);
        }

        /// <summary>
        ///     Returns true if the property contains
        ///     a local value that should be serialized.
        /// </summary>
        public override bool ShouldSerializeValue(object component) 
        {
            DependencyObject DO = FromObj(component);
            bool shouldSerialize = DO.ShouldSerializeProperty(_dp);

            // The of precedence is that a property should be serialized if the ShouldSerializeProperty
            // method returns true and either ShouldSerializeXXX does not exist or it exists and returns true.

            if (shouldSerialize)
            {
                // If we have a ShouldSerialize method, use it

                if (!_queriedShouldSerializeMethod) 
                {
                    MethodInfo method = GetSpecialMethod("ShouldSerialize");
                    
                    if (method != null && method.ReturnType == BoolType) 
                    {
                        _shouldSerializeMethod = method;
                    }
                    _queriedShouldSerializeMethod = true;
                }

                if (_shouldSerializeMethod != null)
                {
                    // See if we need to pass parameters to this method.  When
                    // _property == null, this is an attached property and
                    // the method is static.  When _property != null, this
                    // is a direct property and the method is instanced.

                    if (_property == null) 
                    {
                        shouldSerialize = (bool)_shouldSerializeMethod.Invoke(null, new object[] {DO});
                    }
                    else
                    {
                        shouldSerialize = (bool)_shouldSerializeMethod.Invoke(DO, null);
                    }
                }
            }

            return shouldSerialize;
        }

        /// <summary>
        ///     Adds a change event handler to this descriptor.
        /// </summary>
        public override void AddValueChanged(object component, EventHandler handler) 
        {
            // Potentially optimize this if the individual cost of a change tracker is too high.

            DependencyObject DO = FromObj(component);
            if (_trackers == null) 
            {
                _trackers = new Dictionary<DependencyObject, PropertyChangeTracker>();
            }

            PropertyChangeTracker tracker;
            if (!_trackers.TryGetValue(DO, out tracker)) 
            {
                tracker = new PropertyChangeTracker(DO, _dp);
                _trackers.Add(DO, tracker);
            }

            tracker.Changed += handler;
        }

        /// <summary>
        ///     Removes a previously added change handler.
        /// </summary>
        public override void RemoveValueChanged(object component, EventHandler handler) {
            if (_trackers == null) return;
            PropertyChangeTracker tracker;
            DependencyObject DO = FromObj(component);

            if (_trackers.TryGetValue(DO, out tracker)) 
            {
                tracker.Changed -= handler;
                if (tracker.CanClose) 
                {
                    tracker.Close();
                    _trackers.Remove(DO);
                }
            }
        }


        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        
        /// <summary>
        ///     Returns the collection of attributes associated with this property.
        /// </summary>
        public override AttributeCollection Attributes 
        {
            get 
            {
                // The Attributes
                // property on PropertyDescriptor is not as thread
                // safe as it should be.  There are instances when
                // it can return null during contention.  Our fix is
                // to detect this case and lock.  Note that this isn't
                // 100% because not all property descriptors are
                // DependencyObjectPropertyDescriptors.  

                AttributeCollection attrs = base.Attributes;
                if (attrs == null) 
                {
                    lock(_attributeSyncLock) 
                    {
                        attrs = base.Attributes;
                        Debug.Assert(attrs != null);
                    }
                }

                return attrs;
            }
        }

        /// <summary>
        ///     The type of object this property is describing.
        /// </summary>
        public override Type ComponentType 
        {
            get { return _componentType; }
        }

        /// <summary>
        ///     Returns true if the DP is read only.
        /// </summary>
        public override bool IsReadOnly 
        {
            get { 
                // It is a lot cheaper to get DP metadata
                // than it is to calculate attributes.  While
                // the attributes do factor in DP metadata, short
                // circuit for this common case.
                bool readOnly = _dp.ReadOnly;
                if (!readOnly) {
                    readOnly = Attributes.Contains(ReadOnlyAttribute.Yes);
                }
                return readOnly;
            }
        }

        /// <summary>
        ///     The type of the property.
        /// </summary>
        public override Type PropertyType 
        {
            get { return _dp.PropertyType; }
        }

        /// <summary>
        ///     Returns true if this property descriptor supports change
        ///     notifications.  All dependency property descriptors do.
        /// </summary>
        public override bool SupportsChangeEvents 
        {
            get { return true; }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        /// <summary>
        ///     Returns the dependency property we're wrapping.
        /// </summary>
        internal DependencyProperty DependencyProperty 
        {
            get { return _dp; }
        }


        internal bool IsAttached 
        {
             get { return (_property == null); } 
        }

        internal PropertyMetadata Metadata
        {
            get { return _metadata; }
        }

        #endregion Internal Properties
        
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
            lock (_getMethodCache) 
            {
                _getMethodCache.Clear();
            }

            lock(_setMethodCache)
            {
                _setMethodCache.Clear();
            }

            _dpType = null;
            _boolType = null;
            _attributeType = null;
            _attachedPropertyBrowsableType = null;
        }

        /// <summary>
        ///     A helper method that returns the static "Get" method for an attached
        ///     property.  The dpType parameter is the data type of the object you
        ///     want to attach the property to.
        /// </summary>
        internal static MethodInfo GetAttachedPropertyMethod(DependencyProperty dp)
        {
            // Check the cache.  This property descriptor is cached by the
            // dependency object provider, but there is a unique property descriptor
            // for each type an attached property can be attached to.  Therefore,
            // caching this method lookup for a DP makes sense.

            MethodInfo method;

            // TypeDescriptor offers a feature called a "reflection type", which 
            // is an indirection to another type we should reflect on.  Anywhere
            // we rely on raw reflection we should be using GetReflectionType.
            // Also the returning type may change if someone added or removed
            // a provider, so we need to detect this in our cache invalidation 
            // logic.

            Type reflectionType = TypeDescriptor.GetReflectionType(dp.OwnerType);

            object methodObj = _getMethodCache[dp];
            method = methodObj as MethodInfo;

            if (methodObj == null || (method != null && !object.ReferenceEquals(method.DeclaringType, reflectionType))) {
                BindingFlags f = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
                string methodName = string.Concat("Get", dp.Name);
                method = reflectionType.GetMethod(methodName, f, _dpBinder, DpType, null);

                lock(_getMethodCache) {
                    _getMethodCache[dp] = (method == null ? _nullMethodSentinel : method);
                }
            }

            return method;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///     Overridden to lazily create our attributes.
        /// </summary>
        protected override AttributeCollection CreateAttributeCollection() 
        {
            MergeAttributes();
            return base.CreateAttributeCollection();
        }

        #endregion Protected Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        /// <summary>
        ///     Helper method that recovers a dependency object from a value.
        /// </summary>
        private static DependencyObject FromObj(object value) 
        {
            // This indirection is necessary to support
            // the "association" feature of type descriptor.  This feature
            // alows one object to mimic the API of another.
            return (DependencyObject)TypeDescriptor.GetAssociation(typeof(DependencyObject), value);
        }

        /// <summary>
        ///     Additional metadata attributes for attached properties
        ///     are taken from the "Get" method.
        /// </summary>
        private AttributeCollection GetAttachedPropertyAttributes() 
        {
            MethodInfo mi = GetAttachedPropertyMethod(_dp);

            if (mi != null) 
            {
                Type attrType = AttributeType;
                Attribute[] attrArray = (Attribute[])mi.GetCustomAttributes(attrType, true);

                Type propertyReflectionType = TypeDescriptor.GetReflectionType(_dp.PropertyType);
                Attribute[] typeAttrArray = (Attribute[])propertyReflectionType.GetCustomAttributes(attrType, true);
                if (typeAttrArray != null && typeAttrArray.Length > 0)
                {
                    // Merge attrArry and typeAttrArray
                    Attribute[] mergedAttrArray = new Attribute[attrArray.Length + typeAttrArray.Length];
                    Array.Copy(attrArray, mergedAttrArray, attrArray.Length);
                    Array.Copy(typeAttrArray, 0, mergedAttrArray, attrArray.Length, typeAttrArray.Length);
                    attrArray = mergedAttrArray;
                }

                // Look for and expand AttributeProvider attributes.  These are attributes
                // that allow a method to adopt attributes from another location.  This
                // allows generic properties, such as "public object DataSource {get; set;}",
                // to share a common set of attributes.

                Attribute[] addAttrs = null;

                foreach(Attribute attr in attrArray) 
                {
                    AttributeProviderAttribute pa = attr as AttributeProviderAttribute;
                    if (pa != null) 
                    {
                        Type providerType = Type.GetType(pa.TypeName);
                        if (providerType != null) 
                        {
                            Attribute[] paAttrs = null;
                            if (!string.IsNullOrEmpty(pa.PropertyName)) 
                            {
                                MemberInfo[] milist = providerType.GetMember(pa.PropertyName);
                                if (milist.Length > 0 && milist[0] != null) 
                                {
                                    paAttrs = (Attribute[])milist[0].GetCustomAttributes(typeof(Attribute), true);
                                }
                            }
                            else {
                                paAttrs = (Attribute[])providerType.GetCustomAttributes(typeof(Attribute), true);
                            }

                            if (paAttrs != null)
                            {
                                if (addAttrs == null) 
                                {
                                    addAttrs = paAttrs;
                                }
                                else
                                {
                                    Attribute[] newArray = new Attribute[addAttrs.Length + paAttrs.Length];
                                    addAttrs.CopyTo(newArray, 0);
                                    paAttrs.CopyTo(newArray, addAttrs.Length);
                                    addAttrs = newArray;
}
                            }
                        }
                    }
                }

                // See if we gathered additional attributes.  These are always lower priority
                // and therefore get tacked onto the end of the list
                if (addAttrs != null) 
                {
                    Attribute[] newArray = new Attribute[addAttrs.Length + attrArray.Length];
                    attrArray.CopyTo(newArray, 0);
                    addAttrs.CopyTo(newArray, attrArray.Length);
                    attrArray = newArray;
                }

                return new AttributeCollection(attrArray);
            }

            return AttributeCollection.Empty;
        }

        /// <summary>
        ///     A helper method that returns the static "Get" method for an attached
        ///     property.  The dpType parameter is the data type of the object you
        ///     want to attach the property to.
        /// </summary>
        private static MethodInfo GetAttachedPropertySetMethod(DependencyProperty dp)
        {
            // Check the cache.  This property descriptor is cached by the
            // dependency object provider, but there is a unique property descriptor
            // for each type an attached property can be attached to.  Therefore,
            // caching this method lookup for a DP makes sense.

            MethodInfo method;

            // TypeDescriptor offers a feature called a "reflection type", which 
            // is an indirection to another type we should reflect on.  Anywhere
            // we rely on raw reflection we should be using GetReflectionType.
            // Also the returning type may change if someone added or removed
            // a provider, so we need to detect this in our cache invalidation 
            // logic.

            Type reflectionType = TypeDescriptor.GetReflectionType(dp.OwnerType);

            object methodObj = _setMethodCache[dp];
            method = methodObj as MethodInfo;

            if (methodObj == null || (method != null && !object.ReferenceEquals(method.DeclaringType, reflectionType))) {
                BindingFlags f = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
                string methodName = string.Concat("Set", dp.Name);
    
                Type[] paramTypes = new Type[] {
                    DpType[0],
                    TypeDescriptor.GetReflectionType(dp.PropertyType)
                };
    
                method = reflectionType.GetMethod(methodName, f, _dpBinder, paramTypes, null);

                lock(_setMethodCache) {
                    _setMethodCache[dp] = (method == null ? _nullMethodSentinel : method);
                }
            }

            return method;
        }

        /// <summary>
        ///     Returns one of the "special" property methods for a property descriptor.
        ///     The property name will be appended to the method prefix.  This method
        ///     is used to return the MethodInfo for ShouldSerialize(property) and
        ///     Reset(property).
        /// </summary>
        private MethodInfo GetSpecialMethod(string methodPrefix)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
            Type[] types;
            Type reflectionType;

            if (_property == null) 
            {
                // Attached property
                types = DpType;
                flags |= BindingFlags.Static;
                
                // TypeDescriptor offers a feature called a "reflection type", which 
                // is an indirection to another type we should reflect on.  Anywyere
                // we rely on raw reflection we should be using GetReflectionType.

                reflectionType = TypeDescriptor.GetReflectionType(_dp.OwnerType);
            }
            else
            {
                // Direct property
                types = Type.EmptyTypes;
                flags |= BindingFlags.Instance;
                
                // TypeDescriptor offers a feature called a "reflection type", which 
                // is an indirection to another type we should reflect on.  Anywyere
                // we rely on raw reflection we should be using GetReflectionType.

                reflectionType = TypeDescriptor.GetReflectionType(_property.ComponentType);
            }

            string methodName = string.Concat(methodPrefix, _dp.Name);

            // According to spec, ShouldSerialize and Reset can be non-public.  So we should
            // assert ReflectionPermission here, like TypeDescriptor does.  But since every
            // assert is a security risk, we'll take the compatibility hit, and leave it out.

            MethodInfo methodInfo = reflectionType.GetMethod(methodName, flags, _dpBinder, types, null);

            if (methodInfo != null) 
            {
                // We don't support non-public ShouldSerialize/ClearValue methods.  We could just look
                // for public methods in the first place, but then authors might get confused as
                // to why their non-public method didn't get found, especially because the CLR
                // TypeDescriptor does find and use non-public methods.
                if( !methodInfo.IsPublic )
                {
                    throw new InvalidOperationException(SR.Get(SRID.SpecialMethodMustBePublic, methodInfo.Name));
                }
            }

            return methodInfo;
}

        /// <summary>
        ///     This method is called on demand when we need to get at one or
        ///     more attributes for this property.  Because obtaining attributes
        ///     can be costly, we wait until now to do the job.
        /// </summary>
        private void MergeAttributes() 
        {
            AttributeCollection baseAttributes;

            if (_property != null) 
            {
                baseAttributes = _property.Attributes;
            }
            else 
            {
                baseAttributes = GetAttachedPropertyAttributes();
            }

            List<Attribute> newAttributes = new List<Attribute>(baseAttributes.Count + 1);

            bool readOnly = false;

            foreach (Attribute a in baseAttributes) 
            {
                Attribute attrToAdd = a;
                DefaultValueAttribute defAttr = a as DefaultValueAttribute;

                if (defAttr != null) 
                {
                    // DP metadata always overrides CLR metadata for
                    // default value.
                    attrToAdd = null;
                }
                else 
                {
                    ReadOnlyAttribute roAttr = a as ReadOnlyAttribute;
                    if (roAttr != null)
                    {
                        // DP metata is the merge of CLR metadata for
                        // read only
                        readOnly = roAttr.IsReadOnly;
                        attrToAdd = null;
                    }
                }

                if (attrToAdd != null) newAttributes.Add(attrToAdd);
            }

            // Always include the metadata choice
            readOnly |= _dp.ReadOnly;

            // If we are an attached property and non-read only, the lack of a
            // set method will make us read only.
            if (_property == null && !readOnly && GetAttachedPropertySetMethod(_dp) == null) {
                readOnly = true;
            }

            // Add our own DependencyPropertyAttribute
            DependencyPropertyAttribute dpa = new DependencyPropertyAttribute(_dp, (_property == null));
            newAttributes.Add(dpa);

            // Add DefaultValueAttribute if the DP has a default
            // value
            if (_metadata.DefaultValue != DependencyProperty.UnsetValue) 
            {
                newAttributes.Add(new DefaultValueAttribute(_metadata.DefaultValue));
            }

            // And add a read only attribute if needed
            if (readOnly) 
            {
                newAttributes.Add(new ReadOnlyAttribute(true));
            }

            // Inject these attributes into our attribute array.  There
            // is a quirk to the way this works.  Attributes as they
            // are returned by the CLR and by AttributeCollection are in
            // priority order with the attributes at the front of the list
            // taking precidence over those at the end.  Attributes
            // handed to MemberDescriptor's AttributeArray, however, are
            // in reverse priority order so the "last one in wins".  Therefore
            // we need to reverse the array.

            Attribute[] attrArray = newAttributes.ToArray();
            for (int idx = 0; idx < attrArray.Length / 2; idx++) 
            {
                int swap = attrArray.Length - idx - 1;
                Attribute t = attrArray[idx];
                attrArray[idx] = attrArray[swap];
                attrArray[swap] = t;
            }

            AttributeArray = attrArray;
        }


        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Helper to get the reflection version of typeof(AttachedPropertyBrowsableAttribute).  This is used by 
        /// the AttachInfo class.
        /// </summary>
        internal static Type AttachedPropertyBrowsableAttributeType {
            get {
                Type attachedPropertyBrowsableType = _attachedPropertyBrowsableType;
                if (attachedPropertyBrowsableType == null) {
                    attachedPropertyBrowsableType = TypeDescriptor.GetReflectionType(typeof(AttachedPropertyBrowsableAttribute));
                    _attachedPropertyBrowsableType = attachedPropertyBrowsableType;
                }
                return attachedPropertyBrowsableType;
            }
        }

        /// <summary>
        /// Helper to get the reflection version of typeof(Attribute)
        /// </summary>
        private static Type AttributeType {
            get {
                Type attributeType = _attributeType;
                if (attributeType == null) {
                    attributeType = TypeDescriptor.GetReflectionType(typeof(Attribute));
                    _attributeType = attributeType;
                }
                return attributeType;
            }
        }

        /// <summary>
        /// Helper to get the reflection version of typeof(bool)
        /// </summary>
        private static Type BoolType {
            get {
                Type boolType = _boolType;
                if (boolType == null) {
                    boolType = TypeDescriptor.GetReflectionType(typeof(bool));
                    _boolType = boolType;
                }
                return boolType;
            }
        }

        /// <summary>
        /// Helper to get and cache the reflection version of a dependency object type array
        /// </summary>
        private static Type[] DpType {
            get {
                Type[] dpType = _dpType;
                if (dpType == null) {
                    dpType = new Type[] { TypeDescriptor.GetReflectionType(typeof(DependencyObject)) };
                    _dpType = dpType;
                }

                return dpType;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private static Binder _dpBinder = new AttachedPropertyMethodSelector();

        private static object _nullMethodSentinel = new object();

        // Synchronized by "_getMethodCache".  Note these are "reflection"
        // member infos and should only be passed types returned from
        // GetReflectionType.
        private static Hashtable _getMethodCache = new Hashtable();

        // Synchronized by "_setMethodCache".  Note these are "reflection"
        // member infos and should only be passed types returned from
        // GetReflectionType.
        private static Hashtable _setMethodCache = new Hashtable();

        // Synchronization object for Attributes property.  This would be better to be a
        // member than a static value, but the need for a lock on Attributes is very
        // rare and isn't worth the additional space this would take up as a member
        private static object _attributeSyncLock = new object();

        private PropertyDescriptor _property;
        private DependencyProperty _dp;
        private Type _componentType;
        private PropertyMetadata _metadata;
        private bool _queriedShouldSerializeMethod;
        private bool _queriedResetMethod;
        private Dictionary<DependencyObject, PropertyChangeTracker> _trackers;

        // These are reflection method infos, and should only be passed types
        // returned from GetReflectionType.
        private MethodInfo _shouldSerializeMethod;
        private MethodInfo _resetMethod;

        // These are constructed on demand and cleared when our cache is
        // cleared.  They are all reflection types.
        private static Type[] _dpType;
        private static Type _boolType;
        private static Type _attributeType;
        private static Type _attachedPropertyBrowsableType;

        #endregion Private Fields
    }
}

