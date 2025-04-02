// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media.Effects
{
    internal struct BitmapEffectInputData
    {
        public BitmapEffect BitmapEffect;
        public BitmapEffectInput BitmapEffectInput;

        public BitmapEffectInputData(BitmapEffect bitmapEffect,
                                    BitmapEffectInput bitmapEffectInput)
        {
            BitmapEffect = bitmapEffect;
            BitmapEffectInput = bitmapEffectInput;
        }
    }
}
