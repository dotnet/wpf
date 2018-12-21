// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class NameScopePropertyAttribute : Attribute
    {
        public NameScopePropertyAttribute (string name)
        {
            Name = name;
        }

        public NameScopePropertyAttribute (string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }
    }
}