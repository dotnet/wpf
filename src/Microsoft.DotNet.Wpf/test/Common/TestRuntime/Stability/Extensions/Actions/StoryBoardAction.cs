// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Abstract action, inherit it to perfom particular animation by using Storyboard 
    /// </summary>
    public abstract class StoryBoardAction : SimpleDiscoverableAction
    {
        public void BeginAnimation(Timeline animation, FrameworkElement frameworkElement, object property)
        {
            Storyboard storyboard = new Storyboard();
            Storyboard.SetTargetProperty(animation, new PropertyPath(property));
            storyboard.Children.Add(animation);
            storyboard.Begin(frameworkElement);
        }
    }
}
