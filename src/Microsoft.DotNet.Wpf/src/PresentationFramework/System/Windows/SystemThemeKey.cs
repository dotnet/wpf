// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    ///     Implements ResourceKey to create unique keys for our system resources.
    ///     Keys will be exposed publicly only with the ResourceKey API.
    /// </summary>
    [TypeConverter(typeof(System.Windows.Markup.SystemKeyConverter))]
    internal class SystemThemeKey : ResourceKey
    {
        /// <summary>
        ///     Constructs a new instance of the key with the given ID.
        /// </summary>
        /// <param name="id">The internal, unique ID of the system resource.</param>
        internal SystemThemeKey(SystemResourceKeyID id)
        {
            _id = id;
            Debug.Assert(id > SystemResourceKeyID.InternalSystemThemeStylesStart &&
                         id < SystemResourceKeyID.InternalSystemThemeStylesEnd);
        }

        /// <summary>
        ///     Used to determine where to look for the resource dictionary that holds this resource.
        /// </summary>
        public override Assembly Assembly
        {
            get
            {
                if (_presentationFrameworkAssembly == null)
                {
                    _presentationFrameworkAssembly = typeof(FrameworkElement).Assembly;
                }

                return _presentationFrameworkAssembly;
            }
        }

        /// <summary>
        ///     Determines if the passed in object is equal to this object.
        ///     Two keys will be equal if they both have the same ID.
        /// </summary>
        /// <param name="o">The object to compare with.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public override bool Equals(object o)
        {
            SystemThemeKey key = o as SystemThemeKey;

            if (key != null)
            {
                return key._id == this._id;
            }

            return false;
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)_id;
        }

        /// <summary>
        ///     get string representation of this key
        /// </summary>
        /// <returns>the string representation of the key</returns>
        public override string ToString()
        {
            return _id.ToString();
        }


        internal SystemResourceKeyID InternalKey
        {
            get
            {
                return _id;
            }
        }


        private SystemResourceKeyID _id;
        private static Assembly _presentationFrameworkAssembly;
    }
}
