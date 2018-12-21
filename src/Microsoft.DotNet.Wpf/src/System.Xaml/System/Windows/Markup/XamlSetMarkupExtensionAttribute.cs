// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Markup
{
    [AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=false)]
    public sealed class XamlSetMarkupExtensionAttribute : Attribute
    {
        public XamlSetMarkupExtensionAttribute(string xamlSetMarkupExtensionHandler)
        {
            XamlSetMarkupExtensionHandler = xamlSetMarkupExtensionHandler;
        }

        public String XamlSetMarkupExtensionHandler { get; private set; }
    }
}