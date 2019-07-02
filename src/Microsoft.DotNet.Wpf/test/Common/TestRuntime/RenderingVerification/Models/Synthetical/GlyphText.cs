// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        


namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region usings
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Text;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
        using Microsoft.Test.RenderingVerification.Filters;
        using Microsoft.Test.RenderingVerification.Model.Synthetical.LayoutEngine;
    #endregion usings

    /// <summary>
    /// Summary description for GlyphText.
    /// </summary>
    [SerializableAttribute]
    public class GlyphText : GlyphContainer, ISerializable
    {
        #region Properties
            #region Private Properties
                private GlyphFont _font = new GlyphFont();
                private TextRenderingHint _textRenderingHint = TextRenderingHint.SystemDefault;
                private ArrayList _matches = new ArrayList();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The text to draw
                /// </summary>
                public string Text
                {
                    get
                    {
                        System.Text.StringBuilder retVal = new System.Text.StringBuilder();
                        foreach (GlyphChar glyphChar in Glyphs)
                        {
                            retVal.Append(glyphChar.Character);
                        }
                        return retVal.ToString();
                    }
                    set
                    {
                        if (value == null) { value = string.Empty; }
                        char[] charArray = value.ToCharArray();
                        Glyphs.Clear();
                        foreach (char character in charArray)
                        {
                            GlyphChar glyphChar = new GlyphChar(this);
                            glyphChar.Character = character;
                            glyphChar.CompareInfo.EdgesOnly = CompareInfo.EdgesOnly;
                            glyphChar.Font = _font;
//                            Glyphs.Add(glyphChar);
                        }

                    }
                }
                /// <summary>
                /// The font to use to draw the text
                /// </summary>
                /// <value></value>
                public GlyphFont Font
                {
                    get { return _font; }
                }
                /// <summary>
                /// Type of Text smoothing to use (ClearType / Antialiasing / ...)
                /// </summary>
                /// <value></value>
                public System.Drawing.Text.TextRenderingHint TextRenderingHint
                {
                    get 
                    {
                        return _textRenderingHint;
                    }
                    set 
                    {
                        _textRenderingHint = value;
                    }
                }
                /// <summary>
                /// The background color of the text
                /// </summary>
                /// <value></value>
                public override IColor BackgroundColor
                {
                    get { return _font.BackgroundColor; }
                    set { _font.BackgroundColor = value; }
                }
                /// <summary>
                /// The foreground color of the text (font color)
                /// </summary>
                /// <value></value>
                public override IColor ForegroundColor
                {
                    get { return _font.ForegroundColor; }
                    set { _font.ForegroundColor = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphText() : base()
            {
            }
            /// <summary>
            /// The constructor
            /// </summary>
            public GlyphText(GlyphContainer container) : base(container)
            {
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphText(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                _font = (GlyphFont)info.GetValue("Font", typeof(GlyphFont));
                _textRenderingHint = (System.Drawing.Text.TextRenderingHint)info.GetValue("TextRenderingHint", typeof(System.Drawing.Text.TextRenderingHint));
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Measure of the text
                /// </summary>
                private SizeF GetTypoMeasure(string text)
                {
                    ITypographer avtp = TypographerBroker.Instance;
                    avtp.Text = text;
                    avtp.FontSize = Font.Size;
                    avtp.FontName = Font.Name;
                    return avtp.Measure();
                }
                /// <summary>
                /// Image of the text
                /// </summary>
                private IImageAdapter GetTypoResult()
                {
                    int maxX = 0;
                    int maxY = 0;
                    ITypographer avtp = TypographerBroker.Instance;
                    foreach(GlyphChar glyphChar in Glyphs)
                    {
                        int x = (int)(glyphChar.ComputedLayout.SizeF.Width + glyphChar.ComputedLayout.PositionF.X + .5);
                        int y = (int)(glyphChar.ComputedLayout.SizeF.Height + glyphChar.ComputedLayout.PositionF.Y + .5);
                        if (x > maxX) { maxX = x; }
                        if (y > maxY) { maxY = y; }
                    }
                    IImageAdapter retVal = new ImageAdapter(maxX, maxY, BackgroundColor);
//                    IImageAdapter imageChar = null;
//                    for (int t = 0; t < _text.Count; t++)
                    foreach (GlyphChar glyphChar in Glyphs)
                    {
                        // HACK : Call Render Char one more time but ignore Background color
                        avtp.Text = glyphChar.Character.ToString();
                        avtp.FontSize = Font.Size;
                        avtp.FontName = Font.Name;
                        avtp.BackgroundColor = BackgroundColor;
                        avtp.ForegroundColor = ForegroundColor;
                        avtp.TextRenderingHint = TextRenderingHint;
                        glyphChar.GeneratedImage = avtp.Render();

                        Point offset = glyphChar.ComputedLayout.Position;
                        System.Drawing.Size size = new System.Drawing.Size(glyphChar.GeneratedImage.Width, glyphChar.GeneratedImage.Height);
                        retVal = ImageUtility.CopyImageAdapter(retVal, glyphChar.GeneratedImage, offset, size, true);
                    }

                    return retVal;
                }
            #endregion Private Methods
            #region Internal Methods
                /// <summary>
                /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    for (int t = 0; t < Glyphs.Count; t++)
                    {
                        GlyphChar glyph = (GlyphChar)Glyphs[t];
                        // Reposition the char in the string
                        SizeF szString = GetTypoMeasure(Text.Substring( 0, t + 1));
                        SizeF szChar = GetTypoMeasure(glyph.Character.ToString());
                        glyph.Panel.Position = new PointF(szString.Width - szChar.Width, 0);
                    }
                    // Perform Layout
                    LayoutEngine.ArrangeGlyphs();

                    // Render final result
                    GeneratedImage = GetTypoResult();

                    return GeneratedImage;
                }
            #endregion Intenal Methods
            #region Public Methods
                /// <summary>
                /// Measure the text
                /// </summary>
                public override SizeF Measure()
                {
                    if (Font == null) { throw new RenderingVerificationException("Cannot call Measure before setting the Font"); }

                    SizeF sz = new SizeF();
                    sz = GetTypoMeasure(Text);
                    Size = new Size((int)sz.Width, (int)sz.Height);
//                    //if (PanelSize == SizeF.Empty)
//                    {
//                        Panel.Size = new SizeF(Size.Width, Size.Height);
//                    }
                    return sz;
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("Font", Font);
                info.AddValue("TextRenderingHint", TextRenderingHint);
            }
        #endregion
    }
}
