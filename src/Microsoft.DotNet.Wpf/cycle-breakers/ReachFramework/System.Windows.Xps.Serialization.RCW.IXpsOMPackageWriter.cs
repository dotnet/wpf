// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
[assembly:InternalsVisibleTo(MS.Internal.ReachFramework.BuildInfo.SystemPrinting)]

namespace System.Windows.Xps.Serialization.RCW
{
    internal partial interface IXpsOMPackageWriter
    {
        // INTENTIONALLY empty to avoid exposing the entire type closure to reference assemblies
    }
}