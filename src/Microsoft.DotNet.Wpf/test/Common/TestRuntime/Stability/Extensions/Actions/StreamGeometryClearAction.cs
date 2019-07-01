// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Apply StreamGeometry Clear Method.
    /// </summary>
    public class StreamGeometryClearAction : SimpleDiscoverableAction
    {
        #region Public Members

        // Get PathGeometry from UIElement, since get from ObjectTree directly takes too much time
        [InputAttribute(ContentInputSource.GetFromVisualTree, IsEssentialContent = true)]
        public UIElement UIElement { get; set; }

        #endregion

        #region Override Members
        public override bool CanPerform()
        {
            return ((UIElement.Clip != null) && (UIElement.Clip.GetType() == typeof(StreamGeometry)) && (!UIElement.Clip.IsFrozen));
        }

        /// <summary/>
        public override void Perform()
        {
            Trace.WriteLine("Perform StreamGeometryClearAction...");
            ((StreamGeometry)(UIElement.Clip)).Clear();
        }

        #endregion
    }
}
