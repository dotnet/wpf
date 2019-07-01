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
    /// Factory for creating BitmapEffectInputs
    /// </summary>
    public class EffectInputFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffectInput MakeEffectInput(string input)
        {
            string[] parsedInput = input.Split(' ');

            switch (parsedInput[0])
            {
                case "Relative":
                    return Relative(StringConverter.ToRect(parsedInput[1]));

                case "Absolute":
                    return Absolute(StringConverter.ToRect(parsedInput[1]));

                default:
                    throw new ArgumentException("Specified effect input (" + input + ") cannot be created");
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffectInput Relative(Rect relativeArea)
        {
            BitmapEffectInput input = new BitmapEffectInput();
            input.AreaToApplyEffect = relativeArea;
            input.AreaToApplyEffectUnits = BrushMappingMode.RelativeToBoundingBox;

            return input;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static BitmapEffectInput Absolute(Rect absoluteArea)
        {
            BitmapEffectInput input = new BitmapEffectInput();
            input.AreaToApplyEffect = absoluteArea;
            input.AreaToApplyEffectUnits = BrushMappingMode.Absolute;

            return input;
        }
    }
}