// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Xaml.MS.Impl
{
    [DebuggerDisplay("{ClrNamespace} {Assembly.FullName}")]
    internal readonly struct AssemblyNamespacePair
    {
        private readonly WeakReference _assembly;
        private readonly string _clrNamespace;

        public AssemblyNamespacePair(Assembly asm, string clrNamespace)
        {
            _assembly = new WeakReference(asm);
            _clrNamespace = clrNamespace;
        }

        public Assembly? Assembly
        {
            get { return (Assembly?)_assembly.Target; }
        }

        public string ClrNamespace
        {
            get { return _clrNamespace; }
        }
    }
}
