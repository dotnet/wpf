// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BackgroundSetterFactory : DiscoverableFactory<Setter>
    {
        public Brush Background { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.BackgroundProperty;
            setter.Value = Background;
            return setter;
        }
    }

    class BorderBrushSetterFactory : DiscoverableFactory<Setter>
    {
        public Brush BorderBrush { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.BorderBrushProperty;
            setter.Value = BorderBrush;
            return setter;
        }
    }

    class BorderThicknessSetterFactory : DiscoverableFactory<Setter>
    {
        public Thickness BorderThickness { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.BorderThicknessProperty;
            setter.Value = BorderThickness;
            return setter;
        }
    }

// ContextMenuFactory is currently commented out. This factory cannot be used until ContextMenuFactory is fixed.
// Commenting it out to prevent futile attempts to use it in future.
// TODO: Reenable after fixing the issues with ContextMenu
/*
    class ContextMenuSetterFactory : DiscoverableFactory<Setter>
    {
        public ContextMenu ContextMenu { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.ContextMenuProperty;
            setter.Value = ContextMenu;
            return setter;
        }
    }
*/

    class FontFamilySetterFactory : DiscoverableFactory<Setter>
    {
        public FontFamily FontFamily { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.FontFamilyProperty;
            setter.Value = FontFamily;
            return setter;
        }
    }

    [TargetTypeAttribute(typeof(Setter))]
    class FontSizeSetterFactory : DiscoverableFactory<Setter>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double FontSize { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = Control.FontSizeProperty;
            setter.Value = FontSize;
            return setter;
        }
    }

    [TargetTypeAttribute(typeof(Setter))]
    class OpacitySetterFactory : DiscoverableFactory<Setter>
    {
        public double Opacity { get; set; }

        public override Setter Create(DeterministicRandom random)
        {
            Setter setter = new Setter();
            setter.Property = FrameworkElement.OpacityProperty;
            setter.Value = Opacity;
            return setter;
        }
    }
}
