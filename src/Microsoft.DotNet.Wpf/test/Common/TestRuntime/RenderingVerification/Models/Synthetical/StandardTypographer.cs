// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.IO;
    #endregion using

    /// <summary>
    /// Summary description for AvalonTypographer.
    /// </summary>
    public class StandardTypographer : ITypographer
    {
        #region Properties
            #region Private Properties
                private FontStyle _style = FontStyle.Regular;
                private string _text = string.Empty;
                private string _fontName = string.Empty;
                private float _size = 0f;
                private IColor _foregroundColor = ColorByte.Empty;
                private IColor _backgroundColor = ColorByte.Empty;
                private static StandardTypographer _typograph = null;
                private System.Drawing.Text.TextRenderingHint _textRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Set the Text to be rendered
                /// </summary>
                public string Text
                {
                    set { if (value == null) { value = string.Empty; } _text = value; }
                    get { return _text; }
                }
                /// <summary>
                /// Specify how to render the text (ClearType / AntiAliasing / SingleBitPerPixel / ...)
                /// </summary>
                /// <value></value>
                public System.Drawing.Text.TextRenderingHint TextRenderingHint
                {
                    get { return _textRenderingHint; }
                    set { _textRenderingHint = value; }
                }
                /// <summary>
                /// Set the font name.
                /// </summary>
                public string FontName
                {
                    set { if (value == null) { value = "arial"; } _fontName = value; }
                    get { return _fontName; }
                }
                /// <summary>
                /// Set the size of the font
                /// </summary>
                public float FontSize
                {
                    set { _size = value; }
                    get { return _size; }
                }
                /// <summary>
                /// Font styles
                /// </summary>
                public FontStyle FontStyle
                {
                    get { return _style; }
                    set { _style = value; }
                }
                /// <summary>
                /// the foreground color
                /// </summary>
                public IColor ForegroundColor 
                { 
                    get { return _foregroundColor; }
                    set { _foregroundColor = value; }
                }
                /// <summary>
                /// the background color
                /// </summary>
                public IColor BackgroundColor 
                { 
                    get { return _backgroundColor; }
                    set { _backgroundColor = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private StandardTypographer()
            {
                _text = "<Undef!>";
                _fontName = "Arial";
                _size = 12f;
                _foregroundColor = (ColorByte)Color.Black;
                _backgroundColor = (ColorByte)Color.Empty;
            }
            static StandardTypographer()
            {
                _typograph = new StandardTypographer();
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private Font getFont()
                {
                    return new Font(FontName, FontSize, FontStyle);
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Renders the text with the font at the specified size
                /// </summary>
                public IImageAdapter Render(string textValue, string fontName, float size)
                {
                    Text = textValue;
                    FontName = fontName;
                    FontSize = size;
                    return Render();
                }
                /// <summary>
                /// return the size of the drawn text 
                /// </summary>
                public SizeF Measure()
                {
                    SizeF retVal = SizeF.Empty;
                    using (Bitmap bmp = new Bitmap(10, 10))
                    {
                        using (Graphics gpr = Graphics.FromImage(bmp))
                        {
                            retVal = gpr.MeasureString(Text, getFont());
                        }
                    }
                    return retVal;
                }
                /// <summary>
                /// renders the text based on the properties set
                /// </summary>
                public IImageAdapter Render()
                {
                    IImageAdapter retVal = null;

                    SizeF fsize = Measure();
                    using (Bitmap bmp = new Bitmap((int)(fsize.Width+.5), (int)(fsize.Height+.5)))
                    {
                        using (Graphics gpr = Graphics.FromImage(bmp))
                        {
                            gpr.TextRenderingHint = TextRenderingHint;
                            gpr.Clear(BackgroundColor.ToColor());
                            using (Brush brush = new SolidBrush(ForegroundColor.ToColor()))
                            {
                                gpr.DrawString(Text, getFont(), brush, 0, 0);
                            }
                        }
                        retVal = new ImageAdapter(bmp);
                    }
                    return retVal;
                }
                /// <summary>
                /// Return the singleton.
                /// </summary>
                public static StandardTypographer Instance
                {
                    get { return _typograph; }
                }
            #endregion Public Methods
        #endregion Methods
    }
}
