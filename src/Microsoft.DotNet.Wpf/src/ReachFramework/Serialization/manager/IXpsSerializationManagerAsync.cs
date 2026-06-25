// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Windows.Xps.Serialization
{
    internal interface IXpsSerializationManagerAsync : IXpsSerializationManager
    {
        Stack
        OperationStack
        {
            get;
        }
    }
}
