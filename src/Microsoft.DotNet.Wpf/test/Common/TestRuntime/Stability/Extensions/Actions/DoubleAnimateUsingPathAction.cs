// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Use DoubleAnimationUsingPath and TranslateTransform to animate UIElement RenderTransform. 
    /// </summary>
    public class DoubleAnimateUsingPathAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Target { get; set; }

        public DoubleAnimationUsingPath DoubleAnimationUsingPath { get; set; }

        public TranslateTransform AnimatedTranslateTransform { get; set; }

        public bool IsXAxis { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Target.RenderTransform = AnimatedTranslateTransform;
            if (IsXAxis)
            {
                AnimatedTranslateTransform.BeginAnimation(TranslateTransform.XProperty, DoubleAnimationUsingPath);
            }
            else
            {
                AnimatedTranslateTransform.BeginAnimation(TranslateTransform.YProperty, DoubleAnimationUsingPath);
            }
        }

        public override bool CanPerform()
        {
            if (!base.CanPerform())
            {
                return false;
            }

            //Window can’t set RenderTransform.
            if (Target is Window)
            {
                return false;
            }

            PathGeometry animationPath = DoubleAnimationUsingPath.PathGeometry;
            if (animationPath != null && animationPath.Figures != null)
            {
                return animationPath.Figures.Count > 0;
            }

            return false;
        }

        #endregion
    }
}
