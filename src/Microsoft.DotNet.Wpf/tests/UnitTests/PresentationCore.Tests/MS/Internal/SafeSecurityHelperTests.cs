// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using SafeSecurityHelper = MS.Internal.PresentationCore.SafeSecurityHelper;

namespace PresentationCore.Tests.MS.Internal;

/// <summary>
/// Tests for <see cref="SafeSecurityHelper.GetLoadedAssembly(AssemblyName)"/> when the same
/// assembly is loaded into more than one <see cref="AssemblyLoadContext"/>.
/// </summary>
public class SafeSecurityHelperTests
{
    private static readonly Assembly s_testAssembly = typeof(SafeSecurityHelperTests).Assembly;

    [Fact]
    public void GetLoadedAssembly_CopiesInSeveralContexts_ReturnsTheCopyOfTheCurrentContext()
    {
        var first = new AssemblyLoadContext(nameof(GetLoadedAssembly_CopiesInSeveralContexts_ReturnsTheCopyOfTheCurrentContext) + "1", isCollectible: true);
        var second = new AssemblyLoadContext(nameof(GetLoadedAssembly_CopiesInSeveralContexts_ReturnsTheCopyOfTheCurrentContext) + "2", isCollectible: true);

        try
        {
            Assembly firstCopy = first.LoadFromAssemblyPath(s_testAssembly.Location);
            Assembly secondCopy = second.LoadFromAssemblyPath(s_testAssembly.Location);

            using (first.EnterContextualReflection())
            {
                Assert.Same(firstCopy, SafeSecurityHelper.GetLoadedAssembly(s_testAssembly.GetName()));
            }

            using (second.EnterContextualReflection())
            {
                Assert.Same(secondCopy, SafeSecurityHelper.GetLoadedAssembly(s_testAssembly.GetName()));
            }
        }
        finally
        {
            first.Unload();
            second.Unload();
        }
    }

    [Fact]
    public void GetLoadedAssembly_ContextShipsTheNameButHasNotLoadedIt_ReturnsTheContextsOwnCopy()
    {
        var other = new AssemblyLoadContext(nameof(GetLoadedAssembly_ContextShipsTheNameButHasNotLoadedIt_ReturnsTheContextsOwnCopy) + "Other", isCollectible: true);
        var shipping = new ShippingLoadContext(s_testAssembly);

        try
        {
            // Another context already holds a copy; the process-wide search would return it.
            Assembly otherCopy = other.LoadFromAssemblyPath(s_testAssembly.Location);

            using (shipping.EnterContextualReflection())
            {
                Assembly result = SafeSecurityHelper.GetLoadedAssembly(s_testAssembly.GetName());

                Assert.NotSame(otherCopy, result);
                Assert.Same(shipping, AssemblyLoadContext.GetLoadContext(result));
            }
        }
        finally
        {
            other.Unload();
            shipping.Unload();
        }
    }

    [Fact]
    public void GetLoadedAssembly_OnlyAnotherContextHoldsTheName_ReturnsThatCopy()
    {
        var holder = new AssemblyLoadContext(nameof(GetLoadedAssembly_OnlyAnotherContextHoldsTheName_ReturnsThatCopy), isCollectible: true);

        try
        {
            AssemblyName name = new($"{nameof(SafeSecurityHelperTests)}.{Guid.NewGuid():N}");
            Assembly held;

            using (holder.EnterContextualReflection())
            {
                held = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
            }

            Assembly? result = SafeSecurityHelper.GetLoadedAssembly(name);

            // The context enumerates the runtime assembly behind the builder, a separate instance.
            Assert.NotNull(result);
            Assert.Same(holder, AssemblyLoadContext.GetLoadContext(held));
            Assert.Same(holder, AssemblyLoadContext.GetLoadContext(result));
            Assert.Equal(held.FullName, result.FullName);
        }
        finally
        {
            holder.Unload();
        }
    }

    [Fact]
    public void GetLoadedAssembly_ResolverThrows_FallsBackInsteadOfFailingTheLookup()
    {
        var failing = new FailingLoadContext();

        try
        {
            using (failing.EnterContextualReflection())
            {
                AssemblyName name = new($"{nameof(SafeSecurityHelperTests)}.{Guid.NewGuid():N}");

                // The runtime reports the resolver's exception as a FileLoadException, which is
                // treated as "cannot resolve": the lookup falls back, and nothing holds the name.
                Assert.Null(SafeSecurityHelper.GetLoadedAssembly(name));
            }
        }
        finally
        {
            failing.Unload();
        }
    }

    [Fact]
    public void GetLoadedAssembly_NoContextEstablished_ReturnsTheDefaultContextsCopy()
    {
        Assembly coreLibrary = typeof(object).Assembly;

        Assert.Same(coreLibrary, SafeSecurityHelper.GetLoadedAssembly(coreLibrary.GetName()));
    }

    /// <summary>A context that resolves one assembly from its own copy, without having loaded it yet.</summary>
    private sealed class ShippingLoadContext : AssemblyLoadContext
    {
        private readonly Assembly _shipped;

        public ShippingLoadContext(Assembly shipped)
            : base(nameof(ShippingLoadContext), isCollectible: true)
        {
            _shipped = shipped;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return AssemblyName.ReferenceMatchesDefinition(assemblyName, _shipped.GetName())
                ? LoadFromAssemblyPath(_shipped.Location)
                : null;
        }
    }

    /// <summary>A context whose resolver throws for the names this test asks for.</summary>
    private sealed class FailingLoadContext : AssemblyLoadContext
    {
        public FailingLoadContext()
            : base(nameof(FailingLoadContext), isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return assemblyName.Name is { } name && name.StartsWith(nameof(SafeSecurityHelperTests), StringComparison.Ordinal)
                ? throw new InvalidOperationException(name)
                : null;
        }
    }
}
