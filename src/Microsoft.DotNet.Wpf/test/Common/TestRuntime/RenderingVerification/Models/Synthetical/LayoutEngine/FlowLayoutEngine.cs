// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical.LayoutEngine
{
    #region usings
        using System;   
        using System.Drawing; 
        using Microsoft.Test.RenderingVerification.Model.Synthetical;
    #endregion

    /// <summary>
    /// Summary description for FlowLayoutEngine.
    /// </summary>
    public class FlowLayoutEngine: CanvasLayoutEngine
    {
        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of the FlowLayoutEngine object
            /// </summary>
            public FlowLayoutEngine() : base()
            {
            }
            /// <summary>
            /// Create a new instance of the FlowLayoutEngine object and set the container to use
            /// </summary>
            /// <param name="container">The container on which to apply the Layout</param>
            public FlowLayoutEngine(GlyphContainer container) : base(container)
            {
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Arrange all the glyphs
                /// </summary>
                public override void ArrangeGlyphs()
                {
                    // Try to Arrange using Canvas first
                    base.ArrangeGlyphs();

                    float maxY = 0f;
                    float xPos = 0f;
                    float yPos = 0f;
                    for (int t = 0; t < Container.Glyphs.Count; t++)
                    {
                        GlyphBase glyph = (GlyphBase)Container.Glyphs[t];
                        if (glyph.ComputedLayout.PositionF.X - xPos + glyph.ComputedLayout.Size.Width > Container.Panel.Size.Width)
                        {
                            xPos = glyph.ComputedLayout.PositionF.X;
                            yPos += glyph.Measure().Height;
                            maxY = yPos + glyph.ComputedLayout.Size.Height;
                        }
                        glyph.ComputedLayout.PositionF = new PointF(glyph.ComputedLayout.PositionF.X - xPos, yPos);
                    }
                    if (maxY != 0f && Container.Panel.Size == Container.Size)
                    {
                        Container.Panel.Size = new SizeF(Container.Panel.Size.Width, maxY);
                    }
                }
            #endregion Public Methods
        #endregion Methods
    }
}
