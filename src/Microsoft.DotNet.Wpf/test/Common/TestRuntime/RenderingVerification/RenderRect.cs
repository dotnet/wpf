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
    /// RenderRect : Provide a Rectangle struct without having to use System.Drawing (for MIL)
    /// </summary>
    public struct RenderRect
    {
        #region Properties
            #region Private properties
                private bool _defined;
            #endregion Private properties
            #region Public properties
                /// <summary>
                /// The x coordinate of the upper-left corner defined by type
                /// </summary>
                public int Left;
                /// <summary>
                /// The y coordinate of the upper-left corner defined by type
                /// </summary>
                public int Top;
                /// <summary>
                /// The x coordinate of the lower-right corner defined by type
                /// </summary>
                public int Right;
                /// <summary>
                /// The y coordinate of the lower-right corner defined by type
                /// </summary>
                public int Bottom;

                /// <summary>
                /// Get the width of the rectangular region defined by this type
                /// </summary>
                /// <value></value>
                public int Width
                {
                    get { return Right - Left + 1; }
                }
                /// <summary>
                /// Get the height of the rectangular region defined by this type
                /// </summary>
                /// <value></value>
                public int Height
                {
                    get { return Bottom - Top + 1; }
                }
                /// <summary>
                /// Get the area of the rectangular region defined by this type
                /// </summary>
                /// <value></value>
                public int Area
                {
                    get { return Width * Height; }
                }
            #endregion Public properties
            #region Static properties
                /// <summary>
                /// A definition for an empty Pixel structure
                /// </summary>
                public static readonly RenderRect Empty;
            #endregion Static properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an new instance of this type
            /// </summary>
            /// <param name="left">The x coordinate of the upper-left corner defined by type</param>
            /// <param name="top">The y coordinate of the upper-left corner defined by type</param>
            /// <param name="right">The x coordinate of the lower.right corner defined by type</param>
            /// <param name="bottom">The y coordinate of the lower.right corner defined by type</param>
            public RenderRect(int left, int top, int right, int bottom)
            {
                Top = top;
                Left = left;
                Bottom = bottom;
                Right = right;
                _defined = true;
            }
            static RenderRect()
            {
                Empty = new RenderRect();
            }
        #endregion Constructors

        #region Methods
            #region overriden methods
                /// <summary>
                /// Compare two Rect for equality
                /// </summary>
                /// <param name="obj">The rect to compare against</param>
                /// <returns></returns>
                public override bool Equals(object obj)
                {
                    if (obj == null)
                    {
                        throw new ArgumentNullException("obj", "parameter passed in must be an instance of type '" + this.GetType().ToString() +"' (null was passed in)");
                    }
                    if ((obj is RenderRect) == false)
                    {
                        throw new InvalidCastException("Parameter passed in must be of type '" + this.GetType().ToString() + "' (type '" + obj.GetType().ToString() + "' passed in)");
                    }

                    return (this == (RenderRect)obj);
                }
                /// <summary>
                /// Return the hashcode for this object
                /// </summary>
                /// <returns></returns>
                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }
                /// <summary>
                /// Format the Rect for user friendly output
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    if (_defined == false) { return "{Rect.Empty}"; }
                    return "{Left:" + Left + ", Top:" + Top + ", Right:" + Right + ", Bottom:" + Bottom + "}";
                }
            #endregion overriden methods
            #region Static and operator overload
                /// <summary>
                /// Create a rect from the coordinate passed in
                /// </summary>
                /// <param name="left">The x coordinate of the upper-left corner defined by type</param>
                /// <param name="top">The y coordinate of the upper-left corner defined by type</param>
                /// <param name="right">The x coordinate of the lower.right corner defined by type</param>
                /// <param name="bottom">The y coordinate of the lower.right corner defined by type</param>
                /// <returns></returns>
                public static RenderRect FromLTRB(int left, int top, int right, int bottom)
                {
                    return new RenderRect(left, top, right, bottom);
                }
                /// <summary>
                /// Cast this type into a System.Drawing.Rectangle type
                /// </summary>
                /// <param name="rectangle">The object to convert</param>
                /// <returns></returns>
                public static implicit operator Rectangle(RenderRect rectangle)
                {
                    if (rectangle == RenderRect.Empty) { return Rectangle.Empty; }
                    return Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
                }
                /// <summary>
                /// Cast a System.Drawing.Rectangle into this type
                /// </summary>
                /// <param name="rectangle">The System.Drawing.rectangle object to convert</param>
                /// <returns></returns>
                public static implicit operator RenderRect(Rectangle rectangle)
                {
                    if (rectangle == Rectangle.Empty) { return RenderRect.Empty; }
                    return RenderRect.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
                }
                /// <summary>
                /// Compare two Rect for equality
                /// </summary>
                /// <param name="rectangle1">The rect to be compared</param>
                /// <param name="rectangle2">The rect to compare against</param>
                /// <returns></returns>
                public static bool operator == (RenderRect rectangle1, RenderRect rectangle2)
                {
                    if (rectangle1._defined == rectangle2._defined &&
                        rectangle1.Top == rectangle2.Top &&
                        rectangle1.Left == rectangle2.Left &&
                        rectangle1.Bottom == rectangle2.Bottom &&
                        rectangle1.Right == rectangle2.Right)
                    {
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Compare two Rect for inequality
                /// </summary>
                /// <param name="rectangle1">The rect to be compared</param>
                /// <param name="rectangle2">The rect to compare against</param>
                /// <returns></returns>
                public static bool operator !=(RenderRect rectangle1, RenderRect rectangle2)
                {
                    return !(rectangle1 == rectangle2);
                }
            #endregion Static and operator overload
        #endregion Methods
    }
}
