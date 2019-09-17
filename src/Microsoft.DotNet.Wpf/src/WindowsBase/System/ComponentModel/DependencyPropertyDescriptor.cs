// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Security;
using System.Windows;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.ComponentModel
{
    /// <summary>
    ///     This is a wrapper property descriptor that offers a merged API of
    ///     both CLR and DependencyProperty features.  To use it, call its
    ///     static FromProperty method passing a PropertyDescriptor.  The
    ///     API degrades gracefully if the property descriptor passed does
    ///     not represent a dependency property.
    /// </summary>
    public sealed class DependencyPropertyDescriptor : PropertyDescriptor {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a new dependency property descriptor.  A note on perf:  We don't 
        ///     pass the property descriptor down as the default member descriptor here.  Doing
        ///     so gets the attributes off of the property descriptor, which can be costly if they
        ///     haven't been accessed yet.  Instead, we wait until someone needs to access our
        ///     Attributes property and demand create the attributes at that time.
        /// </summary>
        private DependencyPropertyDescriptor(PropertyDescriptor property, string name, Type componentType, DependencyProperty dp, bool isAttached) : base(name, null) 
        {
            Debug.Assert(property != null || !isAttached, "Demand-load of property descriptor is only supported for direct properties");

            _property = property;
            _componentType = componentType;
            _dp = dp;
            _isAttached = isAttached;
            _metadata = _dp.GetMetadata(componentType);
        }

        #endregion Constructors

        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Static method that returns a DependencyPropertyDescriptor from a PropertyDescriptor.
        /// </summary>
        public static DependencyPropertyDescriptor FromProperty(PropertyDescriptor property) 
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            DependencyPropertyDescriptor dpd;
            bool found;

            lock (_cache)
            {
                found = _cache.TryGetValue(property, out dpd);
            }

            if (found) 
            {
                return dpd;
            }

            // Locate the dependency property.  We do this a fast way
            // by searching for InternalPropertyDescriptor, and a slow
            // way, by looking for an attribute.  The fast way works unless
            // someone has added another layer of metadata overrides to
            // TypeDescriptor.

            DependencyProperty dp = null;
            bool isAttached = false;

            DependencyObjectPropertyDescriptor idpd = property as DependencyObjectPropertyDescriptor;
            if (idpd != null) 
            {
                dp = idpd.DependencyProperty;
                isAttached = idpd.IsAttached;
            }
            else 
            {
                #pragma warning suppress 6506 // Property is obviously not null.
                DependencyPropertyAttribute dpa = property.Attributes[typeof(DependencyPropertyAttribute)] as DependencyPropertyAttribute;
                if (dpa != null)
                {
                    dp = dpa.DependencyProperty;
                    isAttached = dpa.IsAttached;
                }
            }

            if (dp != null) 
            {
                dpd = new DependencyPropertyDescriptor(property, property.Name, property.ComponentType, dp, isAttached);

                lock(_cache)
                {
                    _cache[property] = dpd;
                }
            }

            return dpd;
        }


        /// <summary>
        ///     Static method that returns a DependencyPropertyDescriptor from a DependencyProperty.  The
        ///     DependencyProperty may refer to either a direct or attached property.  The targetType is the
        ///     type of object to associate with the property:  either the owner type for a direct property
        ///     or the type of object to attach to for an attached property.
        /// </summary>
        internal static DependencyPropertyDescriptor FromProperty(DependencyProperty dependencyProperty, Type ownerType, Type targetType, bool ignorePropertyType)
        {
            if (dependencyProperty == null) throw new ArgumentNullException(nameof(dependencyProperty));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            // We have a different codepath here for attached and direct
            // properties.  For direct properties, we route through Type
            // Descriptor because we need the underlying CLR property descriptor
            // to create our wrapped property.  For attached properties, all we
            // need is the dp and the object type and we can create an attached
            // property descriptor based on that.  We must special case attached
            // properties here because TypeDescriptor will only return attached
            // properties for instances, not types.

            DependencyPropertyDescriptor dpd = null;

            if (ownerType.GetProperty(dependencyProperty.Name) != null)
            {
                // For direct properties we don't want to get the property descriptor
                // yet because it is very expensive.  Delay it until needed.

                lock (_ignorePropertyTypeCache)
                {
                    _ignorePropertyTypeCache.TryGetValue(dependencyProperty, out dpd);
                }

                if (dpd == null)
                {
                    // Create a new DPD based on the type information we have.  It 
                    // will fill in the property descriptor by calling TypeDescriptor
                    // when needed.

                    dpd = new DependencyPropertyDescriptor(null, dependencyProperty.Name, targetType, dependencyProperty, false);

                    lock (_ignorePropertyTypeCache)
                    {
                        _ignorePropertyTypeCache[dependencyProperty] = dpd;
                    }
                }
            }
            else
            {
                if (ownerType.GetMethod("Get" + dependencyProperty.Name) == null &&
                    ownerType.GetMethod("Set" + dependencyProperty.Name) == null)
                {
                    return null;
                }

                // If it isn't a direct property, we treat it as attached unless it is internal.
                // We should never release internal properties to the user

                PropertyDescriptor prop = DependencyObjectProvider.GetAttachedPropertyDescriptor(dependencyProperty, targetType);
                if (prop != null)
                {
                    dpd = FromProperty(prop);
                }
            }

            return dpd;
        }

        /// <summary>
        ///     Static method that returns a DependencyPropertyDescriptor from a DependencyProperty.  The
        ///     DependencyProperty may refer to either a direct or attached property.  The targetType is the
        ///     type of object to associate with the property:  either the owner type for a direct property
        ///     or the type of object to attach to for an attached property.
        /// </summary>
        public static DependencyPropertyDescriptor FromProperty(DependencyProperty dependencyProperty, Type targetType)
        {
            if (dependencyProperty == null) throw new ArgumentNullException(nameof(dependencyProperty));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            // We have a different codepath here for attached and direct
            // properties.  For direct properties, we route through Type
            // Descriptor because we need the underlying CLR property descriptor
            // to create our wrapped property.  For attached properties, all we
            // need is the dp and the object type and we can create an attached
            // property descriptor based on that.  We must special case attached
            // properties here because TypeDescriptor will only return attached
            // properties for instances, not types.

            DependencyPropertyDescriptor dpd = null;
            DependencyPropertyKind dpKind = DependencyObjectProvider.GetDependencyPropertyKind(dependencyProperty, targetType);

            if (dpKind.IsDirect) 
            {
                // For direct properties we don't want to get the property descriptor
                // yet because it is very expensive.  Delay it until needed.

                lock (_cache)
                {
                    _cache.TryGetValue(dependencyProperty, out dpd);
                }

                if (dpd == null) 
                {
                    // Create a new DPD based on the type information we have.  It 
                    // will fill in the property descriptor by calling TypeDescriptor
                    // when needed.

                    dpd = new DependencyPropertyDescriptor(null, dependencyProperty.Name, targetType, dependencyProperty, false);

                    lock (_cache)
                    {
                        _cache[dependencyProperty] = dpd;
                    }
                }
            }
            else if (!dpKind.IsInternal) 
            {
                // If it isn't a direct property, we treat it as attached unless it is internal.
                // We should never release internal properties to the user

                PropertyDescriptor prop = DependencyObjectProvider.GetAttachedPropertyDescriptor(dependencyProperty, targetType);
                if (prop != null) 
                {
                    dpd = FromProperty(prop);
                }
            }

            return dpd;
        }

        /// <summary>
        ///     Static method that returns a DependencyPropertyDescriptor for a given property name.
        ///     The name may refer to a direct or attached property.  OwnerType is the type of 
        ///     object that owns the property definition.  TargetType is the type of object you wish
        ///     to set the property for.  For direct properties, they are the same type.  For attached
        ///     properties they usually differ.
        /// </summary>
        public static DependencyPropertyDescriptor FromName(string name, Type ownerType, Type targetType)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (ownerType == null) throw new ArgumentNullException(nameof(ownerType));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            DependencyProperty dp = DependencyProperty.FromName(name, ownerType);
            if (dp != null) 
            {
                return FromProperty(dp, targetType);
            }

            return null;
        }

        /// <summary>
        ///     Static method that returns a DependencyPropertyDescriptor for a given property name.
        ///     The name may refer to a direct or attached property.  OwnerType is the type of 
        ///     object that owns the property definition.  TargetType is the type of object you wish
        ///     to set the property for.  For direct properties, they are the same type.  For attached
        ///     properties they usually differ.
        /// </summary>
        public static DependencyPropertyDescriptor FromName(string name, Type ownerType, Type targetType,
            bool ignorePropertyType)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (ownerType == null) throw new ArgumentNullException(nameof(ownerType));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            DependencyProperty dp = DependencyProperty.FromName(name, ownerType);
            if (dp != null)
            {
                if (ignorePropertyType)
                {
                    try
                    {
                        return FromProperty(dp, ownerType, targetType, ignorePropertyType);
                    }
                    catch (AmbiguousMatchException)
                    {
                        return FromProperty(dp, targetType);
                    }
                }
                else
                {
                    return FromProperty(dp, targetType);
                }
            }

            return null;
        }

        /// <summary>
        ///     Object.Equals override
        /// </summary>
        public override bool Equals(object obj) 
        {
            DependencyPropertyDescriptor dp = obj as DependencyPropertyDescriptor;
            if (dp != null && dp._dp == _dp && dp._componentType == _componentType)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Object.GetHashCode override
        /// </summary>
        public override int GetHashCode() 
        {
            return _dp.GetHashCode() ^ _componentType.GetHashCode();
        }

        /// <summary>
        ///     Object.ToString override
        /// </summary>
        public override string ToString() 
        {
            return Name;
        }

        //
        // The following methods simply route to the underlying property descriptor.
        //

        /// <summary>
        ///     When overridden in a derived class, indicates whether
        ///     the property's value can be reset to a default state.
        /// </summary>
        public override bool CanResetValue(object component) { return Property.CanResetValue(component); }

        /// <summary>
        ///     When overridden in a derived class, gets the current
        ///     value of the property on a component.
        /// </summary>
        public override object GetValue(object component) { return Property.GetValue(component); }

        /// <summary>
        ///     When overridden in a derived class, resets the
        ///     value for this property of the component.
        /// </summary>
        public override void ResetValue(object component) { Property.ResetValue(component); }

        /// <summary>
        ///     When overridden in a derived class, sets the value of
        ///     the component to a different value.
        /// </summary>
        public override void SetValue(object component, object value) { Property.SetValue(component, value); }

        /// <summary>
        ///     When overridden in a derived class, indicates whether the
        ///     value of this property needs to be persisted.
        /// </summary>
        public override bool ShouldSerializeValue(object component) { return Property.ShouldSerializeValue(component); }
        
        /// <summary>
        ///     Allows interested objects to be notified when this property changes.
        /// </summary>
        public override void AddValueChanged(object component, EventHandler handler) { Property.AddValueChanged(component, handler); }

        /// <summary>
        ///     Allows interested objects to be notified when this property changes.
        /// </summary>
        public override void RemoveValueChanged(object component, EventHandler handler) { Property.RemoveValueChanged(component, handler); }
        
        /// <summary>
        ///    Retrieves the properties 
        /// </summary>
        public override PropertyDescriptorCollection GetChildProperties(object instance, Attribute[] filter) { return Property.GetChildProperties(instance, filter); }
        
        /// <summary>
        ///     Gets an editor of the specified type.
        /// </summary>
        public override object GetEditor(Type editorBaseType) { return Property.GetEditor(editorBaseType); }


        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        
        /// <summary>
        ///     Returns the raw dependency property, or null if the property
        ///     this wraps is not a dependency property.
        /// </summary>
        public DependencyProperty DependencyProperty 
        {
            get { return _dp; }
        }

        /// <summary>
        ///     True if the dependency property is being attached to the target type.
        /// </summary>
        public bool IsAttached 
        {
            get { return _isAttached; }
        }
        
        /// <summary>
        ///     The property metadata for the dependency property.  This can be null if there is 
        ///     no metadata or if there is no dependency property.  Values contained in property 
        ///     metadata that have matching concepts in CLR attributes are re-exposed as attributes 
        ///     in the property descriptor's Attributes collection.
        /// </summary>
        public PropertyMetadata Metadata
        {
            get { return _metadata; }
        }

        //
        // The following properties simply route to the underlying property descriptor.
        //

        /// <summary>
        ///     When overridden in a derived class, gets the type of the
        ///     component this property
        ///     is bound to.
        /// </summary>
        public override Type ComponentType { get { return _componentType; } }

        /// <summary>
        ///     When overridden in a derived class, gets a value
        ///     indicating whether this property is read-only.
        /// </summary>
        public override bool IsReadOnly { get { return Property.IsReadOnly; } }

        /// <summary>
        ///     When overridden in a derived class,
        ///     gets the type of the property.
        /// </summary>
        public override Type PropertyType { get { return _dp.PropertyType; } }

        /// <summary>
        ///     Gets the collection of attributes for this member.
        /// </summary>
        public override AttributeCollection Attributes { get { return Property.Attributes; } }

        /// <summary>
        ///     Gets the name of the category that the
        ///     member belongs to, as specified in the CategoryAttribute.
        /// </summary>
        public override string Category { get { return Property.Category; } }

        /// <summary>
        ///     Gets the description of
        ///     the member as specified in the DescriptionAttribute.
        /// </summary>
        public override string Description { get { return Property.Description; } }

        /// <summary>
        ///     Determines whether this member should be set only at
        ///     design time as specified in the DesignOnlyAttribute.
        /// </summary>
        public override bool DesignTimeOnly { get { return Property.DesignTimeOnly; } }

        /// <summary>
        ///     Gets the name that can be displayed in a window like a
        ///     properties window.
        /// </summary>
        public override string DisplayName { get { return Property.DisplayName; } }

        /// <summary>
        ///     Gets the type converter for this property.
        /// </summary>
        public override TypeConverter Converter
        {
            get
            {
                // We only support public type converters, in order to avoid asserts.
                TypeConverter typeConverter = Property.Converter;
                if (typeConverter.GetType().IsPublic)
                {
                    return typeConverter;
                }
                else
                {
                    return null;
                }
            }
        }
        

        /// <summary>
        ///     Gets a value indicating whether the member is browsable as specified in the
        ///     BrowsableAttribute.
        /// </summary>
        public override bool IsBrowsable { get { return Property.IsBrowsable; } }

        /// <summary>
        ///     Gets a value indicating whether this property should be localized, as
        ///     specified in the LocalizableAttribute.
        /// </summary>
        public override bool IsLocalizable { get { return Property.IsLocalizable; } }

        /// <summary>
        ///     Indicates whether value change notifications for this property may originate from outside the property
        ///     descriptor, such as from the component itself (value=true), or whether notifications will only originate
        ///     from direct calls made to PropertyDescriptor.SetValue (value=false). For example, the component may
        ///     implement the INotifyPropertyChanged interface, or may have an explicit '{name}Changed' event for this property.
        /// </summary>
        public override bool SupportsChangeEvents { get { return Property.SupportsChangeEvents; } }

        /// <summary>
        /// This is the callback designers use to participate in the computation of property 
        /// values at design time. Eg. Even if the author sets Visibility to Hidden, the designer 
        /// wants to coerce the value to Visible at design time so that the element doesn't 
        /// disappear from the design surface.
        /// </summary>
        public CoerceValueCallback DesignerCoerceValueCallback
        {
            get { return DependencyProperty.DesignerCoerceValueCallback; }
            set { DependencyProperty.DesignerCoerceValueCallback = value; }
        }
        
        #endregion Public Properties
        

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
            lock (_cache) 
            {
                _cache.Clear();
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties
        
        // Return the property descriptor we're wrapping.  We may have to get
        // this on demand if it wasn't passed into our constructor
        private PropertyDescriptor Property
        {
            get
            {
                if (_property == null) 
                {
                    _property = TypeDescriptor.GetProperties(_componentType)[Name];
                    if (_property == null) 
                    {
                        // This should not normally happen.  If it does, it means
                        // that someone has messed around with metadata and has
                        // removed this property from the type's metadata.  We know
                        // that there is really a CLR property, however, because
                        // we are dealing with a direct property (only direct
                        // properties can have their property descriptor delay
                        // loaded).  So, we can magically create one directly
                        // from the CLR property through TypeDescriptor.
                        _property = TypeDescriptor.CreateProperty(_componentType, Name, _dp.PropertyType);
                    }
                }

                return _property;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private PropertyDescriptor _property;
        private Type _componentType;
        private DependencyProperty _dp;
        private bool _isAttached;
        private PropertyMetadata _metadata;

        // Synchronized by "_cache"
        private static Dictionary<object, DependencyPropertyDescriptor> _cache = 
            new Dictionary<object, DependencyPropertyDescriptor>(
                new ReferenceEqualityComparer()
            );
        private static Dictionary<object, DependencyPropertyDescriptor> _ignorePropertyTypeCache =
            new Dictionary<object, DependencyPropertyDescriptor>(
                new ReferenceEqualityComparer()
            );

        #endregion Private Fields
    }
}

