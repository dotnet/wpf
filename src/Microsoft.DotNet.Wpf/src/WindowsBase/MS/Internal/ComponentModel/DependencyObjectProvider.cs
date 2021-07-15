// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using MS.Internal.ComponentModel;
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
    ///     A type description provider provides metadata for types.  It allows a type
    ///     to define its own semantic layer for properties, events and attributes.
    ///     
    ///     Note: This class can stay internal.  To utilize it, the following 
    ///     metadata attribute should be added to DependencyObject:
    ///     
    ///     [TypeDescriptionProvider(typeof(DependencyObjectProvider))]
    ///     public class DependencyObject {}
    /// </summary>
    internal sealed class DependencyObjectProvider : TypeDescriptionProvider {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     The ctor for this class needs to be public because it is created
        ///     by TypeDescriptor using reflection.
        /// </summary>
        public DependencyObjectProvider() : base(TypeDescriptor.GetProvider(typeof(DependencyObject))) 
        {
            // We keep a lot of caches around.  When TypeDescriptor gets a refresh
            // we clear our caches.  We only need to do this if the refresh
            // contains type information, because we only keep static per-type
            // caches.

            TypeDescriptor.Refreshed += delegate(RefreshEventArgs args)
            {
                if (args.TypeChanged != null && typeof(DependencyObject).IsAssignableFrom(args.TypeChanged)) 
                {
                    ClearCache();
                    DependencyObjectPropertyDescriptor.ClearCache();
                    DPCustomTypeDescriptor.ClearCache();
                    DependencyPropertyDescriptor.ClearCache();
}
            };
        }

        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Returns a custom type descriptor suitable for querying about the
        ///     given object type and instance.
        /// </summary>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) 
        {
            return new DPCustomTypeDescriptor(base.GetTypeDescriptor(objectType, instance),
                objectType, instance);
        }

        /// <summary>
        ///     Returns a custom type descriptor suitable for querying about "extended"
        ///     properties.  Extended properties are are attached properties in our world.
        /// </summary>
        public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance) 
        {
            ICustomTypeDescriptor descriptor = base.GetExtendedTypeDescriptor(instance);

            // It is possible that a Type object worked its way in here as an instance.
            // If it did, we don't need our own descriptor because we don't support
            // attached properties on type instances.

            if (instance != null && !(instance is Type)) 
            {
                descriptor = new APCustomTypeDescriptor(descriptor, instance);
            }

            return descriptor;
        }

        /// <summary>
        ///     Returns a caching layer type descriptor will use to store
        ///     computed metadata.
        /// </summary>
        public override IDictionary GetCache(object instance) 
        {
            DependencyObject d = instance as DependencyObject;

            // This should never happen because we are bound only
            // to dependency object types.  However, in case it
            // does, simply invoke the base and get out.

            if (d == null) 
            {
                return base.GetCache(instance);
            }

            // The cache we return is used by TypeDescriptor to
            // store cached metadata information.  We demand create
            // it here.
            // If the DependencyObject is Sealed we cannot store
            // the cache on it - if no cache exists already we
            // will return null.

            IDictionary cache = _cacheSlot.GetValue(d);
            if (cache == null && !d.IsSealed) 
            {
                cache = new Hashtable();
                _cacheSlot.SetValue(d, cache);
            }

            return cache;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     This method is called when we should clear our cached state.  The cache
        ///     may become invalid if someone adds additional type description providers.
        /// </summary>
        private static void ClearCache()
        {
            lock (_propertyMap) 
            {
                _propertyMap.Clear();
            }

            lock(_propertyKindMap)
            {
                _propertyKindMap.Clear();
            }

            lock(_attachInfoMap)
            {
                _attachInfoMap.Clear();
            }
        }

        /// <summary>
        ///     This method calculates the attach rules defined on 
        ///     this dependency property.  It always returns a valid
        ///     AttachInfo, but the fields in AttachInfo may be null.
        /// </summary>
        internal static AttachInfo GetAttachInfo(DependencyProperty dp)
        {
            // Have we already seen this DP?
            AttachInfo info = (AttachInfo)_attachInfoMap[dp];

            if (info == null) 
            {
                info = new AttachInfo(dp);
    
                lock(_attachInfoMap)
                {
                    _attachInfoMap[dp] = info;
                }
            }

            return info;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        
        /// <summary>
        ///     This method returns an attached property descriptor for the given DP and target type.
        /// </summary>
        internal static DependencyObjectPropertyDescriptor GetAttachedPropertyDescriptor(DependencyProperty dp, Type targetType)
        {
            DependencyObjectPropertyDescriptor dpProp;
            PropertyKey key = new PropertyKey(targetType, dp);

            lock(_propertyMap) 
            {
                if (!_propertyMap.TryGetValue(key, out dpProp)) 
                {
                    dpProp = new DependencyObjectPropertyDescriptor(dp, targetType);
                    _propertyMap[key] = dpProp;
                }
            }

            return dpProp;
        }

        /// <summary>
        ///     This method returns a DependencyPropertyKind object which can
        ///     be used to tell if a given DP / target type combination represents
        ///     an attached or direct property.
        /// </summary>
        internal static DependencyPropertyKind GetDependencyPropertyKind(DependencyProperty dp, Type targetType)
        {
            DependencyPropertyKind kind;
            PropertyKey key = new PropertyKey(targetType, dp);

            lock(_propertyKindMap)
            {
                if (!_propertyKindMap.TryGetValue(key, out kind)) 
                {
                    kind = new DependencyPropertyKind(dp, targetType);
                    _propertyKindMap[key] = kind;
                }
            }

            return kind;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private static readonly UncommonField<IDictionary> _cacheSlot = new UncommonField<IDictionary>(null);

        // Synchronized by "_propertyMap"
        private static Dictionary<PropertyKey, DependencyObjectPropertyDescriptor> _propertyMap = new Dictionary<PropertyKey, DependencyObjectPropertyDescriptor>();

        // Synchronized by "_propertyKindMap"
        private static Dictionary<PropertyKey, DependencyPropertyKind> _propertyKindMap = new Dictionary<PropertyKey, DependencyPropertyKind>();

        // Synchronized by "_attachInfoMap"
        private static Hashtable _attachInfoMap = new Hashtable();

        #endregion Private Fields
    }
}

