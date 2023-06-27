// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomNamespacePrefixLookup : INamespacePrefixLookup
    {
        #nullable enable
        public string[]? ExpectedNamespaces { get; set; }
        #nullable disable
        public string[] Prefixes { get; set; }

        private int CurrentIndex { get; set; }

        public string LookupPrefix(string ns)
        {
            if (ExpectedNamespaces != null)
            {
                Assert.Equal(ExpectedNamespaces[CurrentIndex], ns);
            }

            return Prefixes[CurrentIndex++];
        }
    }
}
