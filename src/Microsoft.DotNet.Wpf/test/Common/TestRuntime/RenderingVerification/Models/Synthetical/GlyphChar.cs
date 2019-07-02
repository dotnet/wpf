// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        


namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region usings
        using System;
        using System.Drawing;
        using System.Drawing.Text;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
        using Microsoft.Test.RenderingVerification.Filters;
    #endregion usings

    /// <summary>
    /// Summary description for GlyphChar.
    /// </summary>
    [SerializableAttribute]
    internal class GlyphChar : GlyphBase, ISerializable
    {
        #region constants
            private const TextRenderingHint TEXTRENDERINGHINT_INVALID = (TextRenderingHint)(-1);
        #endregion constants

        #region Properties
            #region Private Properties
                private char _character;
                private GlyphFont _font = null;
                private TextRenderingHint _textRenderingHint = TEXTRENDERINGHINT_INVALID;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The text itself
                /// </summary>
                public char Character
                {
                    get
                    {
                        return _character;
                    }
                    set
                    {
                        _character = value;
                        Measure();
                    }
                }
                /// <summary>
                /// Get/set The font associated with this char
                /// </summary>
                /// <value></value>
                public GlyphFont Font
                {
                    get { return _font; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("Font", "Must be set to a valid instance of an object(null passed in)"); }
                        _font = value; 
                    }
                }
                /// <summary>
                /// Get/set the background color associated with this char
                /// </summary>
                /// <value></value>
                public override IColor BackgroundColor
                {
                    get { return _font.BackgroundColor; }
                    set { _font.BackgroundColor = value; }
                }
                /// <summary>
                /// Get/set the foreground color associated with this char
                /// </summary>
                /// <value></value>
                public override IColor ForegroundColor
                {
                    get { return _font.ForegroundColor; }
                    set { _font.ForegroundColor = value; }
                }
                /// <summary>
                /// Type of Text smoothing to use (ClearType / Antialiasing / ...)
                /// </summary>
                /// <value></value>
                public System.Drawing.Text.TextRenderingHint TextRenderingHint
                {
                    get
                    {
                        if (_textRenderingHint == TEXTRENDERINGHINT_INVALID)
                        {
                            if (Owner != null && Owner is GlyphText)
                            {
                                return ((GlyphText)Owner).TextRenderingHint;
                            }
                            else 
                            {
                                return TextRenderingHint.SystemDefault;
                            }

                        }
                        return _textRenderingHint;
                    }
                    set
                    {
                        _textRenderingHint = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphChar() : base()
            {
                Font = new GlyphFont();
            }
            /// <summary>
            /// Create a GlyphChar and set the character
            /// </summary>
            public GlyphChar(GlyphContainer container) : base(container)
            {
                Owner = container;
                Font = new GlyphFont();
                GlyphText text = container as GlyphText;
                if (text != null)
                {
                    Font.Name= text.Font.Name;
                    Font.Size= text.Font.Size;
                    Font.Style = text.Font.Style;
                }
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public GlyphChar(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                _character = (char)info.GetValue("Character", typeof(char));
                _font = (GlyphFont)info.GetValue("Font", typeof(GlyphFont));
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Measure of the char
                /// </summary>
                private SizeF GetTypoMeasure()
                {
                    ITypographer avtp = TypographerBroker.Instance;
                    avtp.Text = Character.ToString();
                    avtp.FontSize = Font.Size;
                    avtp.FontName = Font.Name;
                    avtp.FontStyle = Font.Style;
                    return avtp.Measure();
                }
                /// <summary>
                /// Image of the text
                /// </summary>
                private IImageAdapter GetTypoResult()
                {
                     ITypographer avtp = TypographerBroker.Instance;
                    avtp.Text = Character.ToString();
                    avtp.FontSize = Font.Size;
                    avtp.FontName = Font.Name;
                    avtp.FontStyle = Font.Style;
                    avtp.TextRenderingHint = TextRenderingHint;
                    avtp.BackgroundColor = BackgroundColor;
                    avtp.ForegroundColor = ForegroundColor;

                    return avtp.Render();
                }
            #endregion Private Methods
            #region Internal Methods
                /// <summary>
                /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    return GetTypoResult();
                }
            #endregion Intenal Methods
            #region Public Methods
                /// <summary>
                /// Measure the text
                /// </summary>
                public override SizeF Measure()
                {
                    if (Font == null) { throw new RenderingVerificationException("Font must be set before calling Mesure"); }

                    SizeF sz = new SizeF();
                    sz = GetTypoMeasure();
                    Size = new Size((int)(sz.Width+.5), (int)(sz.Height+.5));
                    if (Panel.Size == SizeF.Empty)
                    {
                        Panel.Size = new SizeF (Size.Width, Size.Height);
                    }
                    return sz;
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("Character", Character);
                info.AddValue("Font", Font);
            }
        #endregion ISerializable Members
    }
}
