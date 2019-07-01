// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create an WebBrowser object from xaml files under WebBrowserFactoryXamlFiles.
    /// </summary>
    internal class WebBrowserFromXamlFactory : LoadFromXamlFactory<WebBrowser> 
    {
        protected override string GetXamlDirectoryPath()
        {
            return "WebBrowserFactoryXamlFiles";
        }
    }
}
