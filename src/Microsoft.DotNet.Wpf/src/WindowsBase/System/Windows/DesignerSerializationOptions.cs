// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Markup
{
    /// <summary>
    ///     Specifies the serialization flags per property
    /// </summary>
    [Flags]
    public enum DesignerSerializationOptions : int
    {
        /// <summary>
        ///     Serialize the property as an attibute
        /// </summary>
        SerializeAsAttribute = 0x001
    }
}

