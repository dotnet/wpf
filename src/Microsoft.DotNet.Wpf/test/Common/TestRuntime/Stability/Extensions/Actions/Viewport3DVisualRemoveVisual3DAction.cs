// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Viewport3DVisual remove a Visual3D.
    /// </summary>
    public class Viewport3DVisualRemoveVisual3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromVisualTree)]
        public Viewport3DVisual Viewport3DVisual { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            int visual3DCount = Viewport3DVisual.Children.Count;
            if (visual3DCount > 0)
            {
                Viewport3DVisual.Children.RemoveAt(RemoveIndex % visual3DCount);
            }
        }

        #endregion
    }
}
