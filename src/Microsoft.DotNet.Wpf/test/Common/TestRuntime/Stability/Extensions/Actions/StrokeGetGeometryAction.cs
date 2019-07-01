// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets the Geometry of the current Stroke.
    /// </summary>
    public class StrokeGetGeometryAction : SimpleDiscoverableAction
    {
        public Stroke Target { get; set; }

        public bool IsDefault { get; set; }

        public DrawingAttributes DrawingAttributes { get; set; }

        public override void Perform()
        {
            if (IsDefault)
            {
                //Gets the Geometry of the current Stroke.
                Target.GetGeometry();
            }
            else
            {
                //Gets the Geometry of the current Stroke using the specified DrawingAttributes.
                Target.GetGeometry(DrawingAttributes);
            }
        }
    }
}
