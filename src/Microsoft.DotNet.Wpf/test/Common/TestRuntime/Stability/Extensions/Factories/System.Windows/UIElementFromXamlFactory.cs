// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create an UIElement object from xaml files under UIElementFactoryXamlFiles.
    /// </summary>
    internal class UIElementFromXamlFactory : LoadFromXamlFactory<UIElement> 
    {
        protected override string GetXamlDirectoryPath()
        {
            return "UIElementFactoryXamlFiles";
        }
    }
}
