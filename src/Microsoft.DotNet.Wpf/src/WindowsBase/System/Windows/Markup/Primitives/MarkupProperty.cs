// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  API for iterating a tree of objects for serialization
//
//

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Text;

using System.Windows;
using MS.Internal.WindowsBase;

namespace System.Windows.Markup.Primitives
{
    /// <summary>
    /// A property description used by serialiation to encapsulate access to properties and their values. A property is 
    /// either representable as a string or a list of items. If the property can be respresented as a string 
    /// IsComposite is false, otherwise, if IsComposite is true, the property is a list of items.
    /// </summary>
    public abstract class MarkupProperty
    {
        /// <summary>
        /// Prevent external specialization
        /// </summary>
        [FriendAccessAllowed] // Used by MarkupPropertyWrapper
        internal MarkupProperty() { }

        /// <summary>
        /// A name sutible for diagnostics and error reporting. A serializer should not use this value. It should use 
        /// the PropertyDescriptor and/or DependencyProperty instead.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        public abstract Type PropertyType { get; }

        /// <summary>
        /// whether the property type is a collection or not
        /// </summary>
        internal bool IsCollectionProperty
        {
            get
            {
                Type propertyType = PropertyType;
                
                return (typeof(IList).IsAssignableFrom(propertyType) || 
                        typeof(IDictionary).IsAssignableFrom(propertyType) || 
                        propertyType.IsArray);
            }
        }

        /// <summary>
        /// The property descriptor for the markup property if this property is associated with a CLR property.
        /// </summary>
        public virtual PropertyDescriptor PropertyDescriptor { get { return null; } }

        /// <summary>
        /// The dependency property for the markup property if this property is backed by a DependencyProperty.
        /// </summary>
        public virtual DependencyProperty DependencyProperty { get { return null; } }

        /// <summary>
        /// Returns true when this propety is an attached dependency property. When true, PropertyDescriptor
        /// can be null (but is not required to be) and DependencyProperty is not null.
        /// </summary>
        public virtual bool IsAttached { get { return false; } }

        /// <summary>
        /// Returns true when this property represents a constructor argument. When true, PropertyDescriptor and 
        /// DependencyProperty are both null. XAML only uses this for represnting the constructor parameters of 
        /// MarkupExtension instances.
        /// </summary>
        public virtual bool IsConstructorArgument { get { return false; } }

        /// <summary>
        /// Returns true when this property represents the text to pass to the type's ValueConverter to create an 
        /// instance instead of using a constructor. When true, PropertyDescriptor and DependencyProperty are both 
        /// null. If a property is provided through MarkupItem.Properties that has this value set to true it is the 
        /// only property the type will provide.
        /// </summary>
        public virtual bool IsValueAsString { get { return false; } }

        /// <summary>
        /// Returns true when this property represents direct content of a collection (or dictionary) class. When true,
        /// PropertyDescriptor and DependencyProperty are both null.
        /// </summary>
        public virtual bool IsContent { get { return false; } }

        /// <summary>
        /// Returns true when the property represents the key used by the MarkupItem's container to store the item in a 
        /// dictionary. When true, the PropertyDescriptor and DependencyProperty will return null. XAML will emit this 
        /// as an x:Key attribute.
        /// </summary>
        public virtual bool IsKey { get { return false; } }

        /// <summary>
        /// If IsComposite is false Value and StringValue are valid to call, the property can be represented as a 
        /// string. If is true, Items is valid to use and the property is one or more items. If the property is 
        /// composite but has not items, the property isn't returned by MarkupItem.GetProperties.
        /// </summary>
        public virtual bool IsComposite { get { return false; } }

        /// <summary>
        /// The current value of the property.
        /// </summary>
        public abstract object Value { get; }

        /// <summary>
        /// The string value of the property using ValueSerializer classes when appropriate to convert the value to a 
        /// string. Which ValueSerializer to invoke is determined by the IValueSerializerContext of the MarkupItem that 
        /// returned this MarkupProperty.
        /// </summary>
        public abstract string StringValue { get; }

        /// <summary>
        /// The list of types that this property will reference when it serializes its value as a string. This allows
        /// a serializer to ensure that the de-serializer has enough information to convert references to these type
        /// from the string representations. For example, ensuring there is an xmlns declaration for the XML namespace
        /// that defines the type.
        /// </summary>
        public abstract IEnumerable<Type> TypeReferences { get; }

        /// <summary>
        /// Enumerate the items that make up the value of this property. If the property is not an enumeration, this 
        /// will only be one item in the enumeration. If the property is an enumeration (or enumerable) then all the 
        /// items will be returned. At least one will always be returned since it MarkupItem will not create a 
        /// MarkupProperty for properties with no items.
        /// </summary>
        public abstract IEnumerable<MarkupObject> Items { get; }

        /// <summary>
        /// The attributes associated with the markup property.
        /// </summary>
        public abstract AttributeCollection Attributes { get; }

        /// <summary>
        /// Checks to see that each markup object is of a public type.  Used in serialization.
        /// </summary>
        internal virtual void VerifyOnlySerializableTypes() { }
    }
}
