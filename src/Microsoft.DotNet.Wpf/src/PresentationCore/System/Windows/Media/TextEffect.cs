// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
//  Contents:  TextEffect class
//
//


using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for TextEffect
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    public partial class TextEffect : Animatable
    {
        //----------------------------------------
        // constructor
        //----------------------------------------

        
        /// <summary>
        /// Constructor to TextEffect
        /// </summary>
        /// <param name="transform">transform of the text effect</param>
        /// <param name="foreground">foreground of the text effect</param>
        /// <param name="clip">clip of the text effect</param>
        /// <param name="positionStart">starting character index of the text effect</param>
        /// <param name="positionCount">number of code points</param>
        public TextEffect(
            Transform transform, 
            Brush foreground,
            Geometry clip,
            int positionStart,
            int positionCount
            )            
        {
            ArgumentOutOfRangeException.ThrowIfNegative(positionCount);

            Transform       = transform;
            Foreground      = foreground;
            Clip            = clip;
            PositionStart   = positionStart;
            PositionCount   = positionCount;
        }

        /// <summary>
        /// constructor
        /// </summary>
        public TextEffect()
        {
        }        

        //-------------------------------
        // Private method
        //-------------------------------
        private static bool OnPositionStartChanging(int value)
        {
            return (value >= 0);
        }

        private static bool OnPositionCountChanging(int value)
        {
            return (value >= 0);
        }
    }       
}

