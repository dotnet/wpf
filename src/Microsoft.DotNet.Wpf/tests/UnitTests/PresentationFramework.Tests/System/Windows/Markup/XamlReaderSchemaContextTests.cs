// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Loader;
using System.Xaml;

namespace System.Windows.Markup.Tests;

/// <summary>
/// Tests for <see cref="XamlReader.GetWpfSchemaContext"/> across <see cref="AssemblyLoadContext"/> instances.
/// </summary>
public class XamlReaderSchemaContextTests
{
    [Fact]
    public void GetWpfSchemaContext_NoContextEstablished_ReturnsTheSharedInstance()
    {
        Assert.Same(XamlReader.GetWpfSchemaContext(), XamlReader.GetWpfSchemaContext());
    }

    [Fact]
    public void GetWpfSchemaContext_DefaultContextEstablished_ReturnsTheSharedInstance()
    {
        XamlSchemaContext shared = XamlReader.GetWpfSchemaContext();

        using (AssemblyLoadContext.Default.EnterContextualReflection())
        {
            Assert.Same(shared, XamlReader.GetWpfSchemaContext());
        }
    }

    [Fact]
    public void GetWpfSchemaContext_AnotherContextEstablished_ReturnsThatContextsOwnInstance()
    {
        XamlSchemaContext shared = XamlReader.GetWpfSchemaContext();
        var first = new AssemblyLoadContext(nameof(GetWpfSchemaContext_AnotherContextEstablished_ReturnsThatContextsOwnInstance) + "1", isCollectible: true);
        var second = new AssemblyLoadContext(nameof(GetWpfSchemaContext_AnotherContextEstablished_ReturnsThatContextsOwnInstance) + "2", isCollectible: true);

        try
        {
            XamlSchemaContext firstContext;
            XamlSchemaContext secondContext;

            using (first.EnterContextualReflection())
            {
                firstContext = XamlReader.GetWpfSchemaContext();

                Assert.Same(firstContext, XamlReader.GetWpfSchemaContext());
            }

            using (second.EnterContextualReflection())
            {
                secondContext = XamlReader.GetWpfSchemaContext();
            }

            Assert.NotSame(shared, firstContext);
            Assert.NotSame(shared, secondContext);
            Assert.NotSame(firstContext, secondContext);
            Assert.Same(shared, XamlReader.GetWpfSchemaContext());
        }
        finally
        {
            first.Unload();
            second.Unload();
        }
    }
}
