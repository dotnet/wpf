// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security;

namespace System.Xaml.Permissions
{
    [Serializable]
    public class XamlAccessLevel
    {
        private XamlAccessLevel(string assemblyName, string typeName) { }
        public static XamlAccessLevel AssemblyAccessTo(Assembly assembly) { return default(XamlAccessLevel); }
        public static XamlAccessLevel AssemblyAccessTo(AssemblyName assemblyName) { return default(XamlAccessLevel); }
        public static XamlAccessLevel PrivateAccessTo(Type type) { return default(XamlAccessLevel); }
        public static XamlAccessLevel PrivateAccessTo(string assemblyQualifiedTypeName) { return default(XamlAccessLevel); }
        public AssemblyName AssemblyAccessToAssemblyName { get { return new AssemblyName(); } }
        public string PrivateAccessToTypeName { get; private set; }
    }
}
