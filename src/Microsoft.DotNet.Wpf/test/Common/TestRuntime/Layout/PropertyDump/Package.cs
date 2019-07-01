// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace Microsoft.Test.Layout.PropertyDump
{
    // struct to minimize overhead (if ever porting to class use "sealed" to make it faster)
    internal struct Package
    {
        internal string     queryInterface;
        internal string     name;
        internal ArrayList  children;

        internal Package(string _queryInterface, string _name)
        {
            queryInterface = _queryInterface;
            name = _name;
            children = new ArrayList();
        }
        
        public override string ToString()
        {
            string _children = string.Empty;

            foreach (Package childPackage in children)
            {
                _children += childPackage.ToString();
            }

            return name + queryInterface + _children;
        }
     
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
