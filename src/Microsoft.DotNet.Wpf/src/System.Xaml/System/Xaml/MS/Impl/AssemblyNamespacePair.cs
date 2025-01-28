// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Xaml.MS.Impl
{
    /// <summary>
    /// Holds a <see cref="WeakReference"/> to an <see cref="Reflection.Assembly"/> associated with the current <see cref="ClrNamespace"/>.
    /// </summary>
    [DebuggerDisplay("{ClrNamespace} {Assembly.FullName}")]
    internal readonly struct AssemblyNamespacePair
    {
        private readonly WeakReference<Assembly> _assembly;
        private readonly string _clrNamespace;

        public AssemblyNamespacePair(Assembly asm, string clrNamespace)
        {
            _assembly = new WeakReference<Assembly>(asm);
            _clrNamespace = clrNamespace;
        }

        public Assembly? Assembly
        {
            get => _assembly.TryGetTarget(out Assembly? assembly) ? assembly : null;
        }

        public string ClrNamespace
        {
            get => _clrNamespace;
        }
    }
}
