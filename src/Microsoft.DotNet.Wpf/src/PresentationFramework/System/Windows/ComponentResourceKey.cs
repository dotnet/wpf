// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace System.Windows
{
    /// <summary>
    ///     Key class for custom components to define the names of their resources to be loaded by SystemResources.
    /// </summary>
    [TypeConverter(typeof(System.Windows.Markup.ComponentResourceKeyConverter))]
    public class ComponentResourceKey : ResourceKey
    {
        /// <summary>
        ///     Default constructor. Type and ID are null.
        /// </summary>
        public ComponentResourceKey()
        {
        }

        /// <summary>
        ///     Type and ID are initialized to the specified parameters.
        /// </summary>
        /// <param name="typeInTargetAssembly">The Type to which this key is associated.</param>
        /// <param name="resourceId">A unique ID to differentiate this key from others associated with this type.</param>
        public ComponentResourceKey(Type typeInTargetAssembly, object resourceId)
        {
            if (typeInTargetAssembly == null)
            {
                throw new ArgumentNullException("typeInTargetAssembly");
            }
            if (resourceId == null)
            {
                throw new ArgumentNullException("resourceId");
            }

            _typeInTargetAssembly = typeInTargetAssembly;
            _typeInTargetAssemblyInitialized = true;

            _resourceId = resourceId;
            _resourceIdInitialized = true;
        }

        /// <summary>
        ///     The Type associated with this resources. Must be in assembly where the resource is located.
        /// </summary>
        public Type TypeInTargetAssembly
        {
            get
            {
                return _typeInTargetAssembly;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (_typeInTargetAssemblyInitialized)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ChangingTypeNotAllowed));
                }
                _typeInTargetAssembly = value;
                _typeInTargetAssemblyInitialized = true;
            }
        }

        /// <summary>
        ///     Used to determine where to look for the resource dictionary that holds this resource.
        /// </summary>
        public override Assembly Assembly
        {
            get
            {
                return (_typeInTargetAssembly != null) ? _typeInTargetAssembly.Assembly : null;
            }
        }

        /// <summary>
        ///     A unique Id to differentiate this key from other keys associated with the same type.
        /// </summary>
        public object ResourceId
        {
            get
            {
                return _resourceId;
            }

            set
            {
                if (_resourceIdInitialized)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ChangingIdNotAllowed));
                }
                _resourceId = value;
                _resourceIdInitialized = true;
            }
        }

        /// <summary>
        ///     Determines if the passed in object is equal to this object.
        ///     Two keys will be equal if they both have equal Types and IDs.
        /// </summary>
        /// <param name="o">The object to compare with.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public override bool Equals(object o)
        {
            ComponentResourceKey key = o as ComponentResourceKey;

            if (key != null)
            {
                return ((key._typeInTargetAssembly != null) ? key._typeInTargetAssembly.Equals(this._typeInTargetAssembly) : (this._typeInTargetAssembly == null)) &&
                    ((key._resourceId != null) ? key._resourceId.Equals(this._resourceId) : (this._resourceId == null));
            }

            return false;
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return ((_typeInTargetAssembly != null) ? _typeInTargetAssembly.GetHashCode() : 0) ^ ((_resourceId != null) ? _resourceId.GetHashCode() : 0);
        }

        /// <summary>
        ///     return string representation of this key
        /// </summary>
        /// <returns>the string representation of the key</returns>
        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder(256);
            strBuilder.Append("TargetType=");
            strBuilder.Append((_typeInTargetAssembly != null) ? _typeInTargetAssembly.FullName : "null");
            strBuilder.Append(" ID=");
            strBuilder.Append((_resourceId != null) ? _resourceId.ToString() : "null");
            return strBuilder.ToString();
        }

        private Type _typeInTargetAssembly;
        private bool _typeInTargetAssemblyInitialized;

        private object _resourceId;
        private bool _resourceIdInitialized;
    }
}
