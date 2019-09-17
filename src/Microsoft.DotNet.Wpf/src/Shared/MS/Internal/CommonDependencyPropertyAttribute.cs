// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;

#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.PresentationFramework
#else
#error Class is being used from an unknown assembly.
#endif
{
    /// <summary>
    ///     An attribute that indicates that a DependencyProperty declaration is common
    ///     enough to be included in KnownTypes.cs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("COMMONDPS")]
    internal sealed class CommonDependencyPropertyAttribute : Attribute
    {
    }
}
