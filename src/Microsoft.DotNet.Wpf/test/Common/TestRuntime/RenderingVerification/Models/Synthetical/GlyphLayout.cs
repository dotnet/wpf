// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
   #endregion using

    /// <summary>
    /// Summary description for GlyphBase.
    /// </summary>
    public class GlyphLayout
    {
        #region Properties
            #region Private Properties
                private GlyphBase _glyphBase = null;
                private PointF _position = PointF.Empty;
                private SizeF _size = SizeF.Empty;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the position after layout has been performed
                /// </summary>
                /// <value></value>
                public PointF PositionF
                {
                    get { return _position; }
                    set { _position = value; }
                }
                /// <summary>
                /// Get the Size after layout has been performed
                /// </summary>
                /// <value></value>
                public SizeF SizeF
                {
                    get { return _size; }
                    set { _size= value; }
                }
                /// <summary>
                /// Get the position (rounded to closest integer) after layout has been performed
                /// </summary>
                /// <value></value>
                public Point Position
                {
                    get 
                    {
                        if (_position == PointF.Empty) { return Point.Empty; }
                        return new Point((int)(_position.X + .5), (int)(_position.Y + .5));
                    }
                    set
                    {
                        if (value == Point.Empty) { _position = PointF.Empty; }
                        _position = new PointF(value.X, value.Y);
                    }
                }
                /// <summary>
                /// Get the size (rounded to closest integer) after layout has been performed
                /// </summary>
                /// <value></value>
                public Size Size
                {
                    get 
                    {
                        if (_size == SizeF.Empty) { return Size.Empty; }
                        return new Size((int)(_size.Width + .5), (int)(_size.Height + .5));
                    }
                    set 
                    {
                        if (value == Size.Empty) { _size = SizeF.Empty; }
                        _size = new SizeF(value.Width, value.Height);
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor
            /// </summary>
            private GlyphLayout() 
            {
                _position = new PointF();
                _size = new SizeF();
            }
            /// <summary>
            /// The constructor
            /// </summary>
            public GlyphLayout(GlyphBase glyphBase) : this()
            {
                _glyphBase = glyphBase;
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Internal Abstract Methods
            #endregion Internal Abstract Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods
    }
}
