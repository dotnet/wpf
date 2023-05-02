// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomNamespacePrefixLookup : INamespacePrefixLookup
    {
        public Optional<string[]> ExpectedNamespaces { get; set; }
        public string[] Prefixes { get; set; }

        private int CurrentIndex { get; set; }

        public string LookupPrefix(string ns)
        {
            if (ExpectedNamespaces.HasValue)
            {
                Assert.Equal(ExpectedNamespaces.Value[CurrentIndex], ns);
            }

            return Prefixes[CurrentIndex++];
        }
    }
}
