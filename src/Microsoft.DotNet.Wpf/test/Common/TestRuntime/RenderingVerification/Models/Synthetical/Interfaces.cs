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
    /// ITypographer Interface.
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public interface ITypographer
    {
        /// <summary>
        /// The text to be rendered
        /// </summary>
        string Text { get;set;}
        /// <summary>
        /// the name of the font
        /// </summary>
        string FontName { get;set;}
        /// <summary>
        /// the size of the font
        /// </summary>
        float FontSize { get;set;}
        /// <summary>
        /// the style of the font
        /// </summary>
        FontStyle FontStyle { get;set;}
        /// <summary>
        /// returns the rendered Image
        /// </summary>
        IImageAdapter Render();
        /// <summary>
        /// returns the computed size of the text with the given font
        /// </summary>
        SizeF Measure();
        /// <summary>
        /// Get/Set the Foreground color
        /// </summary>
        IColor ForegroundColor{get;set;}
        /// <summary>
        /// Get/set the Background color
        /// </summary>
        IColor BackgroundColor{get;set;}
        /// <summary>
        /// Get/set the text Rendering (ClearType / Anti-Aliasing / ...)
        /// </summary>
        /// <value></value>
        System.Drawing.Text.TextRenderingHint TextRenderingHint{get;set;}
    }

    /// <summary>
    /// ILayoutEngine Interface.
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public interface ILayoutEngine
    {
        /// <summary>
        /// get /set the object (deriving from GlyphContainer) containing the collection of glyphs to be be arranged
        /// </summary>
        /// <value></value>
        GlyphContainer Container { get;set; }
        /// <summary>
        /// Arrange all the glyphs according to this layout engine type.
        /// </summary>
        void ArrangeGlyphs();
    }
}
