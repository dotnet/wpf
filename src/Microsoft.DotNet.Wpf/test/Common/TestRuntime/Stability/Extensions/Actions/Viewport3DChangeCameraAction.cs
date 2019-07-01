// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Viewport3D Change a Camera.
    /// </summary>
    public class Viewport3DChangeCameraAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Camera Camera { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        #endregion

        public override void Perform()
        {
            Viewport3D.Camera = Camera;
        }
    }
}
