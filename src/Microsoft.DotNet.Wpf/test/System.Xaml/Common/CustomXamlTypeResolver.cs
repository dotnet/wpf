// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomXamlTypeResolver : IXamlTypeResolver
    {
        #nullable enable
        public string? ExpectedQualifiedTypeName { get; set; }
        #nullable disable
        public Type ResolveResult { get; set; }
        
        public Type Resolve(string qualifiedTypeName)
        {
            if (ExpectedQualifiedTypeName != null)
            {
                Assert.Equal(ExpectedQualifiedTypeName, qualifiedTypeName);
            }

            return ResolveResult;
        }
    }
}
