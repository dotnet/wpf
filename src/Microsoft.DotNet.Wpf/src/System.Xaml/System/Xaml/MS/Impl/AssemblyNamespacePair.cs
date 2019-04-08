// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Xaml.MS.Impl
{
    [DebuggerDisplay("{ClrNamespace} {Assembly.FullName}")]
    internal class AssemblyNamespacePair
    {
        WeakReference _assembly;

        public AssemblyNamespacePair(Assembly asm, String clrNamespace)
        {
            _assembly = new WeakReference(asm);
            ClrNamespace = clrNamespace;
        }

        public Assembly Assembly => (Assembly)_assembly.Target;

        public string ClrNamespace { get; }
    }
}
