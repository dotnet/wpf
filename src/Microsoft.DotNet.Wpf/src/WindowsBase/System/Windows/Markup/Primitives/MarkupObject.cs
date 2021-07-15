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
using System.Reflection;
using System.Text;
using System.Windows.Markup;
using MS.Internal.WindowsBase;

namespace System.Windows.Markup.Primitives
{
    /// <summary>
    /// An enumeration class used by serialization to walk the a tree of objects. Markupobject's represent objects 
    /// and using XML based serialization  they would be written as elements.
    /// </summary>
    public abstract class MarkupObject
    {
        /// <summary>
        /// Prevent external specialization
        /// </summary>
        [FriendAccessAllowed] // Used by ElementMarkupObject and others in Framework
        internal MarkupObject() { }

        /// <summary>
        /// The CLR type for the object. For example, an item representing a Button would return the equivilient of 
        /// typeof(System.Windows.Button).
        /// </summary>
        public abstract Type ObjectType { get; }

        /// <summary>
        /// The instance of the object represented by the this MarkupObject. The type of the object might not be
        /// ObjectType if the instance represents a factory creating instances of type ObjectType.
        /// </summary>
        public abstract object Instance { get; }

        /// <summary>
        /// An enumeration the items properties. Only properties that have significant (serializable) properties are 
        /// returned. Properties that have their default value, or are otherwise not visible to serialization, are not 
        /// returned.
        /// 
        /// The first 0 to N properties returned by the Properties enumeration might have the IsConstructorArgument 
        /// set. These properties should be used as the parameters to the ItemType's constructor with N parameters. If 
        /// the first property doesn't have the IsConstructorArgument set, the default constructor of the ItemType 
        /// should be used.
        /// 
        /// If the MarkupItem is in a dictionary, one of the properties of the item will have an IsKey set to true. 
        /// This is the value for the dictionary key.
        /// </summary>
        public virtual IEnumerable<MarkupProperty> Properties { get { return GetProperties(true /*mapToConstructorArgs*/); } }
        internal abstract IEnumerable<MarkupProperty> GetProperties(bool mapToConstructorArgs);

        /// <summary>
        /// Assigns a root context to use for ValueSerializer's that are used to return, for example, the string value 
        /// of a property or key. All value serializaers will be looked up via this context if present.
        /// </summary>
        /// <param name="context">
        /// The context that will be passed to ValueSerializer's and TypeConverter's when converting text to a string
        /// </param>
        public abstract void AssignRootContext(IValueSerializerContext context);

        /// <summary>
        /// The attributes associated with the markup item.
        /// </summary>
        public abstract AttributeCollection Attributes { get; }
    }
}
