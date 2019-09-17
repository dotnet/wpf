// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  GlyphTypeface with shaping capability
//
//

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using MS.Utility;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.TextFormatting;

using FontFace = MS.Internal.FontFace;


namespace MS.Internal.Shaping
{
    /// <summary>
    /// Typeface that is capable of shaping character string. Shaping is done
    /// thru shaping engines.
    /// </summary>
    internal class ShapeTypeface
    {
        private GlyphTypeface  _glyphTypeface;
        private IDeviceFont    _deviceFont;


        internal ShapeTypeface(
            GlyphTypeface        glyphTypeface,
            IDeviceFont          deviceFont
            )
        {
            Invariant.Assert(glyphTypeface != null);
            _glyphTypeface = glyphTypeface;
            _deviceFont = deviceFont;
        }

        public override int GetHashCode()
        {
            return HashFn.HashMultiply(_glyphTypeface.GetHashCode())
                + (_deviceFont == null ? 0 : _deviceFont.Name.GetHashCode());
        }

        public override bool Equals(object o)
        {
            ShapeTypeface t = o as ShapeTypeface;
            if(t == null)
                return false;

            if (_deviceFont == null)
            {
                if (t._deviceFont != null)
                    return false;
            }
            else
            {
                if (t._deviceFont == null || t._deviceFont.Name != _deviceFont.Name)
                    return false;
            }

            return _glyphTypeface.Equals(t._glyphTypeface);
        }

        internal IDeviceFont DeviceFont
        {
            get { return _deviceFont; }
        }
        /// <summary>
        /// Get physical font face
        /// </summary>
        internal GlyphTypeface GlyphTypeface
        {
            get
            {
                return _glyphTypeface;
            }
        } 
    }


    /// <summary>
    /// Scaled shape typeface
    /// </summary>
    internal class ScaledShapeTypeface
    {
        private ShapeTypeface       _shapeTypeface;
        private double              _scaleInEm;
        private bool                _nullShape;


        internal ScaledShapeTypeface(
            GlyphTypeface           glyphTypeface,
            IDeviceFont             deviceFont,
            double                  scaleInEm,
            bool                    nullShape
            )
        {
            _shapeTypeface = new ShapeTypeface(glyphTypeface, deviceFont);
            _scaleInEm = scaleInEm;
            _nullShape = nullShape;
        }

        internal ShapeTypeface ShapeTypeface
        {
            get { return _shapeTypeface; }
        }

        internal double ScaleInEm
        {
            get { return _scaleInEm; }
        }

        internal bool NullShape
        {
            get { return _nullShape; }
        }

        public override int GetHashCode()
        {
            int hash = _shapeTypeface.GetHashCode();

            unsafe
            {
                hash = HashFn.HashMultiply(hash) + (int)(_nullShape ? 1 : 0);
                hash = HashFn.HashMultiply(hash) + _scaleInEm.GetHashCode();
                return HashFn.HashScramble(hash);
            }
        }

        public override bool Equals(object o)
        {
            ScaledShapeTypeface t = o as ScaledShapeTypeface;
            if (t == null)
                return false;

            return
                    _shapeTypeface.Equals(t._shapeTypeface)
                &&  _scaleInEm == t._scaleInEm
                &&  _nullShape == t._nullShape;
        }
}
}
