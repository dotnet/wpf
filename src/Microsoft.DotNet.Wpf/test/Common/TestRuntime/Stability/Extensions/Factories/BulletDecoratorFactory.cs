// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BulletDecoratorFactory : DiscoverableFactory<BulletDecorator>
    {
        public UIElement Child { get; set; }
        public UIElement Bullet { get; set; }
        public Brush Brush { get; set; }

        public override BulletDecorator Create(DeterministicRandom random)
        {
            BulletDecorator bulletDecorator = new BulletDecorator();
            bulletDecorator.Child = Child;
            bulletDecorator.Bullet = Bullet;
            bulletDecorator.Background = Brush;
            return bulletDecorator;
        }
    }
}
