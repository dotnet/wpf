// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.Drawing;
    #endregion usings

    /// <summary>
    /// Pixel : Provide a Point + Color
    /// </summary>
    public struct Pixel
    {
        #region Properties
            #region Private Properties
                private int _x;
                private int _y;
                private IColor _color;
                private bool _defined;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The value of the x coordinate
                /// </summary>
                /// <value></value>
                public int X
                {
                    get
                    {
                        return _x;
                    }
                    set
                    {
                        _defined = true;
                        _x = value;
                    }
                }
                /// <summary>
                /// The value of the y coordinate
                /// </summary>
                /// <value></value>
                public int Y
                {
                    get
                    {
                        return _y;
                    }
                    set
                    {
                        _defined = true;
                        _y = value;
                    }
                }
                /// <summary>
                /// The value of the Color
                /// </summary>
                /// <value></value>
                public IColor Color
                {
                    get 
                    {
                        return _color;
                    }
                    set 
                    {
                        _color = value;
                    }
                }
            #endregion Public Properties
            #region Static Properties
                /// <summary>
                /// A definition for an empty Pixel structure
                /// </summary>
                public static readonly Pixel Empty;
            #endregion Static Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of the pixel object with specified coordinate and color
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="color"></param>
            public Pixel(int x, int y, IColor color)
            {
                _x = x;
                _y = y;
                _color = color;
                _defined = true;
            }
            /// <summary>
            /// Create a new instance of the pixel object with specified coordinate and color
            /// </summary>
            /// <param name="xHighYLow">The integer contains the x value in its high bits and y in its low bits</param>
            /// <param name="color">The color to assign to the pixel</param>
            public Pixel(int xHighYLow, IColor color)
            {
                _x = xHighYLow >> 16;
                _y = xHighYLow & 0xFFFF;
                _color = color;
                _defined = true;
            }
            static Pixel()
            {
                Empty = new Pixel();
            }
        #endregion Constructors

        #region Methods
            #region Private Methods (mimic System.Drawing.Point public methods)
                /// <summary>
                /// Translate the point by the specified amount
                /// </summary>
                /// <param name="dx">The amount to offset the x coordinate</param>
                /// <param name="dy">The amount to offset the y coordinate</param>
                private void Offset(int dx, int dy)
                {
                    if (_defined == false)
                    {
                        return;
                    }
                    _x += dx;
                    _y += dy;
                }
                /// <summary>
                /// Retireve if a Pixel is unassigned / Empty
                /// </summary>
                /// <value></value>
                private bool IsEmpty
                {
                    get 
                    {
                        return _defined;
                    }
                }
            #endregion Private Methods (mimic System.Drawing.Point public methods)
            #region Overridden Methods
                /// <summary>
                /// Compare two Pixel for Equality
                /// </summary>
                /// <param name="obj">The Pixel to compare against</param>
                /// <returns></returns>
                public override bool Equals(object obj)
                {
                    // BUGBUG : Won't work for class derived from Pixel
                    if (obj.GetType() != this.GetType())
                    {
                        throw new InvalidCastException("Don't know how to cast '" + obj.GetType().ToString() + "' into '"+ this.GetType().ToString() + "'");
                    }

                    return this == (Pixel)obj;
                }
                /// <summary>
                /// Retrieve the HashCode for this object
                /// </summary>
                /// <returns></returns>
                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }
                /// <summary>
                /// Format the Pixel for user friendly output
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    if (_defined == false)
                    {
                        return "{ "+ this.GetType().ToString() + ".Empty }";
                    }

                    return "{ x:" + _x.ToString() + ", y:" + _y.ToString() + " }";
                }
            #endregion Overridden Methods
            #region Operators
                /// <summary>
                /// Compare two Pixel for Equality
                /// </summary>
                /// <param name="first">The pixel use to compare</param>
                /// <param name="second">The pixel to compare against</param>
                /// <returns></returns>
                public static bool operator ==(Pixel first, Pixel second)
                {
                    return (first._defined == second._defined && first._x == second._x && first._y == second._y) ? true : false;
                }
                /// <summary>
                /// Compare two Pixel for Inequality
                /// </summary>
                /// <param name="first">The pixel use to compare</param>
                /// <param name="second">The pixel to compare against</param>
                /// <returns></returns>
                public static bool operator !=(Pixel first, Pixel second)
                {
                    return !(first == second);
                }
            #endregion Operators
        #endregion Methods
    }
}
