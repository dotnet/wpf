// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Build.Framework
{
    // MSBuild recognizes this attribute by full type name, allowing the task assembly
    // to opt into multithreaded execution while targeting an older Microsoft.Build.Framework.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class MSBuildMultiThreadableTaskAttribute : Attribute
    {
    }
}
