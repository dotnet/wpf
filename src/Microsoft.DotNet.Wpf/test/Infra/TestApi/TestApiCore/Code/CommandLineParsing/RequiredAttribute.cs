// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.CommandLineParsing
{
    /// <summary>
    /// Defines whether a property value is required to be specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute
    {
        /// <summary />
        public RequiredAttribute()
        {
        }
    }
}