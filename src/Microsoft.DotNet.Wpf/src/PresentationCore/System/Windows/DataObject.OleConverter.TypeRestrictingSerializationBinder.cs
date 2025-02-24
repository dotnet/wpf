// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class OleConverter
    {
        /// <summary>
        ///  This class is meant to restrict deserialization of managed objects during Ole conversion to only strings
        ///  and arrays of primitives.  A RestrictedTypeDeserializationException is thrown upon calling
        ///  BinaryFormatter.Deserialized if a binder of this type is provided to the BinaryFormatter.
        /// </summary>
        private class TypeRestrictingSerializationBinder : SerializationBinder
        {
            public TypeRestrictingSerializationBinder()
            {
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                throw new RestrictedTypeDeserializationException();
            }
        }
    }
}
