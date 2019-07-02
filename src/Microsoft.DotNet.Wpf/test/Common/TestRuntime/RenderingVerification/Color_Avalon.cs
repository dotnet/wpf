// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.Windows.Media;
    #endregion usings

//    [SerializableAttribute()]
    /// <summary>
    /// ColorDouble : precise and support scRGB but slow and consumes lots of memory
    /// </summary>
    public partial struct ColorDouble: IColor
    {
        #region Constructors
            /// <summary>
            /// Build a new instance of the ColorDouble object using Avalon Color
            /// </summary>
            /// <param name="avalonColor"></param>
            public ColorDouble(System.Windows.Media.Color avalonColor) : this(avalonColor.A, avalonColor.R,avalonColor.G,avalonColor.B)
            {
                if (ARGB == 0)
                {
                    IsEmpty = true;
                }
            }
        #endregion Constructors

        #region Static Methods (Implicit Cast)

            // BUGBUG : CHECK IF WE DO NOT NEED A CONVERSION BETWEEN MY IMPLEMENTATION OF Extended color and Avalon

            /// <summary>
            /// Cast a Avalon color (System.Windows.Media.Color) into this type
            /// </summary>
            /// <param name="avalonColor">The Avalon color to cast</param>
            /// <returns>The color translated in this type</returns>
            public static implicit operator ColorDouble(System.Windows.Media.Color avalonColor)
            {
                ColorDouble colorDouble = new ColorDouble(avalonColor.ScA, avalonColor.ScR, avalonColor.ScG, avalonColor.ScB, true);
                float sum = 0f;
                foreach (float val in avalonColor.GetNativeColorValues())
                {
                    sum += Math.Abs(val);
                }
                if (sum == 0f) { colorDouble.IsEmpty = true; }

                return colorDouble;
            }
            /// <summary>
            /// Cast this type into an Avalon color (System.Windows.Media.Color)
            /// </summary>
            /// <param name="colorDouble">This ColorDouble type to cast</param>
            /// <returns>The color translated in the standard Avalon type</returns>
            public static explicit operator System.Windows.Media.Color(ColorDouble colorDouble)
            {
                return System.Windows.Media.Color.FromScRgb((float)colorDouble.ExtendedAlpha, (float)colorDouble.ExtendedRed, (float)colorDouble.ExtendedGreen, (float)colorDouble.ExtendedBlue);
            }
        #endregion Static Methods (Implicit Cast)

    }

//    [SerializableAttribute()]
        /// <summary>
        /// ColorDouble: Fast and small memory usage but no scRGB support (information migh be lost during filtering / type conversion)
    /// </summary>
    public partial struct ColorByte: IColor
    {
        #region Constructors
            /// <summary>
            /// Build a new instance of the ColorByte object using Avalon Color
            /// </summary>
            /// <param name="avalonColor">The avalon color to use</param>
            public ColorByte(System.Windows.Media.Color avalonColor) : this(avalonColor.A, avalonColor.R,avalonColor.G,avalonColor.B)
            {
                if (ARGB == 0) { IsEmpty = true;}
            }
        #endregion Constructors

        #region Static Methods (Implicit Cast)
            /// <summary>
            /// Cast an Avalon color (System.Windows.Media.Color) into this type
            /// </summary>
            /// <param name="avalonColor">The Avalon color to cast</param>
            /// <returns>The color translated in this type</returns>
            public static explicit operator ColorByte(System.Windows.Media.Color avalonColor)
            {
                ColorByte colorByte = new ColorByte();
                colorByte.A = avalonColor.A;
                colorByte.R = avalonColor.R;
                colorByte.G = avalonColor.G;
                colorByte.B = avalonColor.B;
                if (colorByte.ARGB == 0) { colorByte.IsEmpty = true; }
                return colorByte;
            }
            /// <summary>
            /// Cast this type into an avalon color (System.Windows.Media.Color)
            /// </summary>
            /// <param name="colorByte">This ColorByte type to cast</param>
            /// <returns>The color translated in the standard GDI type</returns>
            public static explicit operator System.Windows.Media.Color(ColorByte colorByte)
            {
                return System.Windows.Media.Color.FromArgb(colorByte.A, colorByte.R, colorByte.G, colorByte.B);
            }
        #endregion Static Methods (Implicit Cast)
    }
}
