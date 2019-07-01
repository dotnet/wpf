// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create an FlowDocument object from xaml files under FlowDocumentFactoryXamlFiles.
    /// </summary>
    internal class FlowDocumentFromXamlFactory : LoadFromXamlFactory<FlowDocument> 
    {
        protected override string GetXamlDirectoryPath()
        {
            return "FlowDocumentFactoryXamlFiles";
        }
    }
}
