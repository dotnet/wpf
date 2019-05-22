// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The definition of ApplicationGesture enum type
//

using System;

namespace System.Windows.Ink
{
    /// <summary>
    /// ApplicationGesture
    /// </summary>
    public enum ApplicationGesture
    {
        /// <summary>
        /// AllGestures
        /// </summary>
        AllGestures = 0,
        /// <summary>
        /// ArrowDown
        /// </summary>
        ArrowDown = 61497,
        /// <summary>
        /// ArrowLeft
        /// </summary>
        ArrowLeft = 61498,
        /// <summary>
        /// ArrowRight
        /// </summary>
        ArrowRight = 61499,
        /// <summary>
        /// ArrowUp
        /// </summary>
        ArrowUp = 61496,
        /// <summary>
        /// Check
        /// </summary>
        Check = 61445,
        /// <summary>
        /// ChevronDown
        /// </summary>
        ChevronDown = 61489,
        /// <summary>
        /// ChevronLeft
        /// </summary>
        ChevronLeft = 61490,
        /// <summary>
        /// ChevronRight
        /// </summary>
        ChevronRight = 61491,
        /// <summary>
        /// ChevronUp
        /// </summary>
        ChevronUp = 61488,
        /// <summary>
        /// Circle
        /// </summary>
        Circle = 61472,
        /// <summary>
        /// Curlicue
        /// </summary>
        Curlicue = 61456,
        /// <summary>
        /// DoubleCircle
        /// </summary>
        DoubleCircle = 61473,
        /// <summary>
        /// DoubleCurlicue
        /// </summary>
        DoubleCurlicue = 61457,
        /// <summary>
        /// DoubleTap
        /// </summary>
        DoubleTap = 61681,
        /// <summary>
        /// Down
        /// </summary>
        Down = 61529,
        /// <summary>
        /// DownLeft
        /// </summary>
        DownLeft = 61546,
        /// <summary>
        /// DownLeftLong
        /// </summary>
        DownLeftLong = 61542,
        /// <summary>
        /// DownRight
        /// </summary>
        DownRight = 61547,
        /// <summary>
        /// DownRightLong
        /// </summary>
        DownRightLong = 61543,
        /// <summary>
        /// DownUp
        /// </summary>
        DownUp = 61537,
        /// <summary>
        /// Exclamation
        /// </summary>
        Exclamation = 61604,
        /// <summary>
        /// Left
        /// </summary>
        Left = 61530,
        /// <summary>
        /// LeftDown
        /// </summary>
        LeftDown = 61549,
        /// <summary>
        /// LeftRight
        /// </summary>
        LeftRight = 61538,
        /// <summary>
        /// LeftUp
        /// </summary>
        LeftUp = 61548,
        /// <summary>
        /// NoGesture
        /// </summary>
        NoGesture = 61440,
        /// <summary>
        /// Right
        /// </summary>
        Right = 61531,
        /// <summary>
        /// RightDown
        /// </summary>
        RightDown = 61551,
        /// <summary>
        /// RightLeft
        /// </summary>
        RightLeft = 61539,
        /// <summary>
        /// RightUp
        /// </summary>
        RightUp = 61550,
        /// <summary>
        /// ScratchOut
        /// </summary>
        ScratchOut = 61441,
        /// <summary>
        /// SemicircleLeft
        /// </summary>
        SemicircleLeft = 61480,
        /// <summary>
        /// SemicircleRight
        /// </summary>
        SemicircleRight = 61481,
        /// <summary>
        /// Square
        /// </summary>
        Square = 61443,
        /// <summary>
        /// Star
        /// </summary>
        Star = 61444,
        /// <summary>
        /// Tap
        /// </summary>
        Tap = 61680,
        /// <summary>
        /// Triangle
        /// </summary>
        Triangle = 61442,
        /// <summary>
        /// Up
        /// </summary>
        Up = 61528,
        /// <summary>
        /// UpDown
        /// </summary>
        UpDown = 61536,
        /// <summary>
        /// UpLeft
        /// </summary>
        UpLeft = 61544,
        /// <summary>
        /// UpLeftLong
        /// </summary>
        UpLeftLong = 61540,
        /// <summary>
        /// UpRight
        /// </summary>
        UpRight = 61545,
        /// <summary>
        /// UpRightLong
        /// </summary>
        UpRightLong = 61541
    }

        // Whenever the ApplicationGesture is modified, please update this ApplicationGestureHelper.IsDefined.
    internal static class ApplicationGestureHelper
    {
        // the number of enums defined, used by NativeRecognizer
        // to limit input
        internal static readonly int CountOfValues = 44;

        // Helper like Enum.IsDefined,  for ApplicationGesture.  It is an fxcop violation
        // to use Enum.IsDefined (for perf reasons)
        internal static bool IsDefined(ApplicationGesture applicationGesture)
        {
            //note that we can't just check the upper and lower bounds since the app gesture
            //values are not contiguous
            switch(applicationGesture)
            {
                case ApplicationGesture.AllGestures:
                case ApplicationGesture.ArrowDown:
                case ApplicationGesture.ArrowLeft:
                case ApplicationGesture.ArrowRight:
                case ApplicationGesture.ArrowUp:
                case ApplicationGesture.Check:
                case ApplicationGesture.ChevronDown:
                case ApplicationGesture.ChevronLeft:
                case ApplicationGesture.ChevronRight:
                case ApplicationGesture.ChevronUp:
                case ApplicationGesture.Circle:
                case ApplicationGesture.Curlicue:
                case ApplicationGesture.DoubleCircle:
                case ApplicationGesture.DoubleCurlicue:
                case ApplicationGesture.DoubleTap:
                case ApplicationGesture.Down:
                case ApplicationGesture.DownLeft:
                case ApplicationGesture.DownLeftLong:
                case ApplicationGesture.DownRight:
                case ApplicationGesture.DownRightLong:
                case ApplicationGesture.DownUp:
                case ApplicationGesture.Exclamation:
                case ApplicationGesture.Left:
                case ApplicationGesture.LeftDown:
                case ApplicationGesture.LeftRight:
                case ApplicationGesture.LeftUp:
                case ApplicationGesture.NoGesture:
                case ApplicationGesture.Right:
                case ApplicationGesture.RightDown:
                case ApplicationGesture.RightLeft:
                case ApplicationGesture.RightUp:
                case ApplicationGesture.ScratchOut:
                case ApplicationGesture.SemicircleLeft:
                case ApplicationGesture.SemicircleRight:
                case ApplicationGesture.Square:
                case ApplicationGesture.Star:
                case ApplicationGesture.Tap:
                case ApplicationGesture.Triangle:
                case ApplicationGesture.Up:
                case ApplicationGesture.UpDown:
                case ApplicationGesture.UpLeft:
                case ApplicationGesture.UpLeftLong:
                case ApplicationGesture.UpRight:
                case ApplicationGesture.UpRightLong:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}
