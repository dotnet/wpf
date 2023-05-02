// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomXamlTypeResolver : IXamlTypeResolver
    {
        public Optional<string> ExpectedQualifiedTypeName { get; set; }
        public Type ResolveResult { get; set; }
        
        public Type Resolve(string qualifiedTypeName)
        {
            if (ExpectedQualifiedTypeName.HasValue)
            {
                Assert.Equal(ExpectedQualifiedTypeName.Value, qualifiedTypeName);
            }

            return ResolveResult;
        }
    }
}
