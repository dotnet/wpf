// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Factory for creating BitmapEffects
    /// </summary>
    public class EffectFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffect MakeEffect(string effect)
        {
            string[] parsedEffect = effect.Split(' ');

            switch (parsedEffect[0])
            {
                case "Blur":
                    return Blur(StringConverter.ToDouble(parsedEffect[1]));

                case "DropShadow":
                    return DropShadow(
                                StringConverter.ToColor(parsedEffect[1]),
                                StringConverter.ToDouble(parsedEffect[2]),
                                StringConverter.ToDouble(parsedEffect[3]));

                // add Bevel, Emboss, OuterGlow, Group (maybe)

                default:
                    throw new ArgumentException("Specified effect (" + effect + ") cannot be created");
            }
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffect Blur(double radius)
        {
            BlurBitmapEffect effect = new BlurBitmapEffect();
            effect.Radius = radius;

            return effect;
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffect DropShadow(Color color, double direction, double depth)
        {
            DropShadowBitmapEffect effect = new DropShadowBitmapEffect();
            effect.Color = color;
            effect.Direction = direction;
            effect.ShadowDepth = depth;
            effect.Softness = 0.0;

            return effect;
        }
    }
}