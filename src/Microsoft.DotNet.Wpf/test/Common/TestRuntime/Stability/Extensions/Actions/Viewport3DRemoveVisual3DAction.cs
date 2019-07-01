// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Viewport3D remove a Visual3D.
    /// </summary>
    public class Viewport3DRemoveVisual3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            int visual3DCount = Viewport3D.Children.Count;
            if (visual3DCount > 0)
            {
                Viewport3D.Children.RemoveAt(RemoveIndex % visual3DCount);
            }
        }

        #endregion
    }
}
