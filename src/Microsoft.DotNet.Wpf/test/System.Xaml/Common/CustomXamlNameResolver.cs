// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomXamlNameResolver : IXamlNameResolver
    {
        public string ExpectedName { get; set; } = "name";
        public bool ExpectedCanAssignDirectly { get; set; } = true;

        public object ResolveResult { get; set; }
        public object GetFixupTokenResult { get; set; }

        public bool IsFixupTokenAvailable { get; }

        public object Resolve(string name)
        {
            Assert.Equal(ExpectedName, name);
            return ResolveResult;
        }

        public object Resolve(string name, out bool isFullyInitialized)
        {
            throw new NotImplementedException();
        }

        public object GetFixupToken(IEnumerable<string> names)
        {
            throw new NotImplementedException();
        }
        
        public object GetFixupToken(IEnumerable<string> names, bool canAssignDirectly)
        {
            Assert.Equal(new string[] { ExpectedName }, names);
            Assert.Equal(ExpectedCanAssignDirectly, canAssignDirectly);
            return GetFixupTokenResult;
        }

        public IEnumerable<KeyValuePair<string, object>> GetAllNamesAndValuesInScope()
        {
            throw new NotImplementedException();
        }

        public event EventHandler OnNameScopeInitializationComplete
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
    }
}
