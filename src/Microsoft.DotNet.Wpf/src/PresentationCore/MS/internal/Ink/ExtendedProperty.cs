// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MS.Internal.Ink.InkSerializedFormat;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    /// <summary>
    /// Drawing Attribute Key/Value pair for specifying each attribute
    /// </summary>
    internal sealed class ExtendedProperty
    {
        /// <summary>
        /// Create a new drawing attribute with the specified key and value
        /// </summary>
        /// <param name="id">Identifier of attribute</param>
        /// <param name="value">Attribute value - not that the Type for value is tied to the id</param>
        /// <exception cref="System.ArgumentException">Value type must be compatible with attribute Id</exception>
        internal ExtendedProperty(Guid id, object value)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidGuid));
            }
            _id = id;
            Value = value;
        }

        /// <summary>Returns a value that can be used to store and lookup
        /// ExtendedProperty objects in a hash table</summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Value.GetHashCode();
        }

        /// <summary>Determine if two ExtendedProperty objects are equal</summary>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            ExtendedProperty that = (ExtendedProperty)obj;

            if (that.Id == this.Id)
            {
                Type type1 = this.Value.GetType();
                Type type2 = that.Value.GetType();

                if (type1.IsArray && type2.IsArray)
                {
                    Type elementType1 = type1.GetElementType();
                    Type elementType2 = type2.GetElementType();
                    if (elementType1 == elementType2 &&
                        elementType1.IsValueType && 
                        type1.GetArrayRank() == 1 &&
                        elementType2.IsValueType && 
                        type2.GetArrayRank() == 1)
                    {
                        Array array1 = (Array)this.Value;
                        Array array2 = (Array)that.Value;
                        if (array1.Length == array2.Length)
                        {
                            for (int i = 0; i < array1.Length; i++)
                            {
                                if (!array1.GetValue(i).Equals(array2.GetValue(i)))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                    }
                }
                else
                {
                    return that.Value.Equals(this.Value);
                }
            }
            return false;
        }

        /// <summary>Overload of the equality operator for comparing
        /// two ExtendedProperty objects</summary>
        public static bool operator ==(ExtendedProperty first, ExtendedProperty second)
        {
            if ((object)first == null && (object)second == null)
            {
                return true;
            }
            else if ((object)first == null || (object)second == null)
            {
                return false;
            }
            else
            {
                return first.Equals(second);
            }
        }

        /// <summary>Compare two custom attributes for Id and value inequality</summary>
        /// <remarks>Value comparison is performed based on Value.Equals</remarks>
        public static bool operator !=(ExtendedProperty first, ExtendedProperty second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Returns a debugger-friendly version of the ExtendedProperty
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string val;
            if (Value == null)
            {
                val = "<undefined value>";
            }
            else if (Value is string)
            {
                val = "\"" + Value.ToString() + "\"";
            }
            else
            {
                val = Value.ToString();
            }
            return KnownIds.ConvertToString(Id) + "," + val;
        }

        /// <summary>
        /// Retrieve the Identifier, or key, for Drawing Attribute key/value pair
        /// </summary>
        internal Guid Id
        {
            get
            {
                return _id;
            }
        }
        /// <summary>
        /// Set or retrieve the value for ExtendedProperty key/value pair
        /// </summary>
        /// <exception cref="System.ArgumentException">Value type must be compatible with attribute Id</exception>
        /// <remarks>Value can be null.</remarks>
        internal object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // validate the type information for the id against the id
                ExtendedPropertySerializer.Validate(_id, value);

                _value = value;
            }
        }

        /// <summary>
        /// Creates a copy of the Guid and Value
        /// </summary>
        /// <returns></returns>
        internal ExtendedProperty Clone()
        {
            //
            // the only properties we accept are value types or arrays of
            // value types with the exception of string.
            //
            Guid guid = _id; //Guid is a ValueType that copies on assignment
            Type type = _value.GetType();

            //
            // check for the very common, copy on assignment
            // types (ValueType or string)
            //
            if (type.IsValueType || type == typeof(string))
            {
                //
                // either ValueType or string is passed by value
                //
                return new ExtendedProperty(guid, _value);
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if (elementType.IsValueType && type.GetArrayRank() == 1)
                {
                    //
                    // copy the array memebers, which we know are copy
                    // on assignment value types
                    //
                    Array newArray = Array.CreateInstance(elementType, ((Array)_value).Length);
                    Array.Copy((Array)_value, newArray, ((Array)_value).Length);
                    return new ExtendedProperty(guid, newArray);
                }
            }
            //
            // we didn't find a type we expect, throw
            //
            throw new InvalidOperationException(SR.Get(SRID.InvalidDataTypeForExtendedProperty));
        }


        private Guid _id;                // id of attribute
        private object _value;             // data in attribute
    }
}
