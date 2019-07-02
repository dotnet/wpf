// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
 
namespace Microsoft.Test.RenderingVerification.Model.Synthetical.LayoutEngine
{
    #region usings
        using System;
    #endregion usings

    /// <summary>
    /// Summary description for CanvasLayoutEngine.
    /// </summary>
    public class CanvasLayoutEngine : ILayoutEngine
    {
        #region Properties
            #region Private Properties
                GlyphContainer _glyphContainer = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get/set the GlyphContainer holding the Glyph to perform Lyyout on
                /// </summary>
                /// <value></value>
                public GlyphContainer Container
                {
                    get 
                    {
                        return _glyphContainer;
                    }
                    set 
                    {
                        _glyphContainer = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of the CanvasLayoutEngine object
            /// </summary>
            public CanvasLayoutEngine()
            {
            }
            /// <summary>
            /// Create a new instance of the CanvasLayoutEngine object and set the container to use
            /// </summary>
            /// <param name="container">The container on which to apply the Layout</param>
            public CanvasLayoutEngine(GlyphContainer container) : this() 
            {
                _glyphContainer = container;
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Arrange all the glyphs
                /// </summary>
                public virtual void ArrangeGlyphs()
                {
                    if (_glyphContainer == null) { throw new RenderingVerificationException("FlowLayoutEngine::Container must be set before calling ArrangeGlyphs (currently set to null)"); }

                    // Canvas : Absolute position, do not rearrange any glyph.
                    foreach (GlyphBase glyph in _glyphContainer.Glyphs)
                    {
                        glyph.ComputedLayout.PositionF = new System.Drawing.PointF(glyph.Panel.Position.X + glyph.Position.X, glyph.Panel.Position.Y + glyph.Position.Y);
                        glyph.ComputedLayout.SizeF = new System.Drawing.SizeF(glyph.Panel.Size.Width, glyph.Panel.Size.Height);
                    }
                }
            #endregion Public Methods
        #endregion Methods
    }
}
