// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Provides an internal way to get the assembly from which the stream was created.
//   This is used by the various streams that the Navigation engine creates internally
//   and consumed by the parser\Baml Loader.
//

using System;
using System.Reflection;

namespace System.Windows.Markup
{
    internal interface IStreamInfo
    {
        Assembly Assembly { get; }
    }
}
