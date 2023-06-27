// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomXamlNamespaceResolver : IXamlNamespaceResolver
    {
        #nullable enable
        public string? ExpectedPrefix { get; set; }
        #nullable disable
        public string GetNamespaceResult { get; set; }
        private int CurrentIndex { get; set; }

        public string GetNamespace(string prefix)
        {
            if (ExpectedPrefix != null)
            {
                Assert.Equal(ExpectedPrefix, prefix);
            }

            return GetNamespaceResult;
        }

        public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes()
        {
            throw new NotImplementedException();
        }
    }
}
