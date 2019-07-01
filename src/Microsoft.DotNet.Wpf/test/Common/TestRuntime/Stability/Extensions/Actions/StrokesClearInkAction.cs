// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Removes all elements from the StokeCollection.
    /// </summary>
    public class StrokesClearInkAction : SimpleDiscoverableAction
    {
        public InkCanvas InkCanvas { get; set; }
        
        public override void Perform()
        {
            InkCanvas.Strokes.Clear();
        }
    }
}
