// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Xaml.MS.Impl
{
    /// <summary>
    /// Holds a <see cref="WeakReference"/> to an <see cref="Reflection.Assembly"/> associated with the current <see cref="ClrNamespace"/>.
    /// </summary>
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
