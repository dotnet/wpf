// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add a Stroke to the StrokeCollection.
    /// </summary>
    public class StrokeCollectionAddStrokeAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public Stroke Stroke { get; set; }

        public override void Perform()
        {
            InkCanvas.Strokes.Add(Stroke);
        }
    }
}
