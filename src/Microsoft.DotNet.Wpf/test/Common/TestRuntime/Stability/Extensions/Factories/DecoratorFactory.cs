// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    abstract class DecoratorFactory<T> : DiscoverableFactory<T> where T : Decorator
    {
        public UIElement UIElement { get; set; }

        protected void ApplyDecoratorProperties(Decorator decorator, DeterministicRandom random)
        {
            decorator.Child = UIElement;
        }
    }
}
