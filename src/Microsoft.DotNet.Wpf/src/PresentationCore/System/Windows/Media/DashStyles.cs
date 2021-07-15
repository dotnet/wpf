// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This file contains the static class DashStyles.
//              DashStyles contains well known DashStyles implementations.
//
//

using System;
using System.Windows;

namespace System.Windows.Media 
{
    /// <summary>
    /// DashStyles - The DashStyles class is static, and contains properties for well known
    /// dash styles.
    /// </summary> 
    public static class DashStyles
    {
        #region Public Static Properties
        
        /// <summary>
        /// Solid - A solid DashArray (no dashes).
        /// </summary>
        public static DashStyle Solid 
        {
            get
            {
                if (_solid == null)
                {
                    DashStyle solid = new DashStyle();
                    solid.Freeze();
                    _solid = solid;
                }

                return _solid;
            }
        }

        /// <summary>
        /// Dash - A DashArray which is 2 on, 2 off
        /// </summary>
        public static DashStyle Dash
        {
            get
            {
                if (_dash == null)
                {
                    DashStyle style = new DashStyle(new double[] {2, 2}, 1);
                    style.Freeze();
                    _dash = style;
                }

                return _dash;
            }
        }

        /// <summary>
        /// Dot - A DashArray which is 0 on, 2 off
        /// </summary>
        public static DashStyle Dot
        {
            get
            {
                if (_dot == null)
                {
                    DashStyle style = new DashStyle(new double[] {0, 2}, 0);
                    style.Freeze();
                    _dot = style;
                }

                return _dot;
            }
        }

        /// <summary>
        /// DashDot - A DashArray which is 2 on, 2 off, 0 on, 2 off
        /// </summary>
        public static DashStyle DashDot
        {
            get
            {
                if (_dashDot == null)
                {
                    DashStyle style = new DashStyle(new double[] {2, 2, 0, 2}, 1);
                    style.Freeze();
                    _dashDot = style;
                }

                return _dashDot;
            }
        }

        /// <summary>
        /// DashDot - A DashArray which is 2 on, 2 off, 0 on, 2 off, 0 on, 2 off
        /// </summary>
        public static DashStyle DashDotDot
        {
            get
            {
                if (_dashDotDot == null)
                {
                    DashStyle style = new DashStyle(new double[] {2, 2, 0, 2, 0, 2}, 1);
                    style.Freeze();
                    _dashDotDot = style;
                }

                return _dashDotDot;
            }
        }

        #endregion Public Static Properties

        #region Private Static Fields
        
        private static DashStyle _solid;
        private static DashStyle _dash; 
        private static DashStyle _dot;
        private static DashStyle _dashDot;
        private static DashStyle _dashDotDot;

        #endregion Private Static Fields
    }
}

