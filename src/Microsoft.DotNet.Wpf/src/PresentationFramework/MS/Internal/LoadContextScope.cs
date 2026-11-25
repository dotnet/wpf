// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Loader;
using System.Windows.Baml2006;
using System.Xaml;

namespace MS.Internal
{
    /// <summary>
    ///     Establishes the <see cref="AssemblyLoadContext"/> of the assembly a markup load is for,
    ///     so that the assemblies, types and pack URIs the markup references are resolved in the
    ///     same context as the code that owns it.
    /// </summary>
    /// <remarks>
    ///     The context is established as the current contextual reflection context, which is what
    ///     the assembly lookups in WindowsBase, PresentationCore and System.Xaml read, and what
    ///     <see cref="Assembly.Load(AssemblyName)"/> honors. The returned scope is <see langword="null"/>
    ///     when nothing would change, and is meant to be disposed with a <see langword="using"/>.
    /// </remarks>
    internal static class LoadContextScope
    {
        /// <summary>
        ///     Establishes the load context of the assembly a load is made for.
        /// </summary>
        /// <param name="owner">The assembly that owns the markup being loaded.</param>
        /// <returns>
        ///     The scope to dispose, or <see langword="null"/> when the owner is unknown, belongs to
        ///     the default context, or its context is already current.
        /// </returns>
        internal static IDisposable EnterFor(Assembly owner)
        {
            if (owner is null)
            {
                return null;
            }

            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(owner);

            if (context is null ||
                context == AssemblyLoadContext.Default ||
                context == AssemblyLoadContext.CurrentContextualReflectionContext)
            {
                return null;
            }

            return context.EnterContextualReflection();
        }

        /// <summary>
        ///     Establishes the load context of the assembly that owns markup realized after the load
        ///     that carried it returned, such as template content and deferred resources.
        /// </summary>
        /// <param name="schemaContext">The schema context the markup was recorded with.</param>
        /// <returns>
        ///     The scope to dispose, or <see langword="null"/> when the owner is unknown or nothing
        ///     would change.
        /// </returns>
        /// <remarks>
        ///     Deferred markup is realized during a layout pass, a resource lookup or a theme change,
        ///     under whatever context flowed with whoever queued that work: none, or another
        ///     component's. The owner's context is therefore entered even when another one is current.
        /// </remarks>
        internal static IDisposable EnterForDeferred(XamlSchemaContext schemaContext)
        {
            Assembly owner = (schemaContext as Baml2006SchemaContext)?.LocalAssembly;

            if (owner is null)
            {
                return null;
            }

            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(owner);
            AssemblyLoadContext current = AssemblyLoadContext.CurrentContextualReflectionContext;

            if (context is null ||
                context == current ||
                (context == AssemblyLoadContext.Default && current is null))
            {
                return null;
            }

            return context.EnterContextualReflection();
        }
    }
}
