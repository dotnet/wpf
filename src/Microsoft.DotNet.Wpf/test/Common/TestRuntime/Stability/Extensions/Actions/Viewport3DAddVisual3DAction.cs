// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Viewport3D add a Visual3D.
    /// </summary>
    public class Viewport3DAddVisual3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent=true)]
        public Visual3D Visual3D { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Viewport3D.Children.Add(Visual3D);
        }

        #endregion
    }
}
