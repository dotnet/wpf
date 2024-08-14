// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Runtime.Serialization;

namespace System.Windows
{
    internal static class SerializationExtensions
    {
        /// <summary>
        ///  Get a typed value. Hard casts.
        /// </summary>
        public static T? GetValue<T>(this SerializationInfo info, string name) => (T?)info.GetValue(name, typeof(T));
    }
}
