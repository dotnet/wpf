// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    /// <summary>
    /// This action performs FontSize Animation on Button object including Easing Functions.  
    /// </summary>
    [TargetTypeAttribute(typeof(AnimateFontSizeAction))]
    public class AnimateFontSizeAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window window { get; set; }        
        public Button button { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double animateToFontSize { get; set; }

        public DoubleAnimation FontSizeAnimation { get; set; }
        public EasingFunctionBase easingFunction { get; set; }

        public override void Perform()
        {
            FontSizeAnimation.From = 1.0;
            FontSizeAnimation.To = animateToFontSize;            
            FontSizeAnimation.EasingFunction = easingFunction;
            window.Content = button;
            button.BeginAnimation(Button.FontSizeProperty, FontSizeAnimation);
        }
    }
#endif
}


