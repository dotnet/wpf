// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Windows;
        using System.Threading; 
        using System.Windows.Media;
        using System.Globalization;
        using System.Windows.Navigation;
        using System.Windows.Media.Imaging;
    #endregion using

    /// <summary>
    /// Summary description for AvalonTypographer.
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public class AvalonTypographer: ITypographer
    {
        #region Properties
            #region Private Properties 
                private string _text = string.Empty;
                private string _fontName = "Arial";
                private float _size = 12;
                private FormattedText mFormattedText = null;
                private System.Windows.Media.Color _foreGround = new System.Windows.Media.Color();
                private System.Windows.Media.Color _backGround = new System.Windows.Media.Color();
                private System.Drawing.FontStyle _style = System.Drawing.FontStyle.Regular;
                private static AvalonTypographer _typograph = new AvalonTypographer();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Return the singleton.
                /// </summary>
                public static AvalonTypographer Instance
                {
                    get { return _typograph; }
                }
                /// <summary>
                /// Set the Text to be rendered
                /// </summary>
                public string Text
                {
                    set { if (value == null) { value = ""; } _text = value; }
                    get { return _text; }
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
                public System.Drawing.FontStyle FontStyle
                {
                    set { _style = value; }
                    get { return _style; }
                }
                /// <summary>
                /// the foreground color
                /// </summary>
                public IColor ForegroundColor 
                {
//                    get { return new ColorByte(_foreGround.A, _foreGround.R, _foreGround.G, _foreGround.B); }
//                    set { _foreGround = System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B); }
                    get { return (ColorByte)_foreGround;  }
                    set { _foreGround = (System.Windows.Media.Color)(ColorByte)value; }
                }
                /// <summary>
                /// the background color
                /// </summary>
                public IColor BackgroundColor
                {
//                    get { return new ColorByte(_backGround.A, _backGround.R, _backGround.G, _backGround.B); }
//                    set { _backGround = System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B); }
                    get { return (ColorByte)_backGround; }
                    set { _backGround = (System.Windows.Media.Color)(ColorByte)value; }
                }
                /// <summary>
                /// Get/set the text Rendering (ClearType / Anti-Aliasing / ...)
                /// </summary>
                /// <value></value>
                public System.Drawing.Text.TextRenderingHint TextRenderingHint 
                {
                    get { return System.Drawing.Text.TextRenderingHint.SystemDefault; }
                    set 
                    {
                        if (value == TextRenderingHint) { return; }
                        throw new NotSupportedException("Cannot currently change the TestRenderingHint in Avalon"); 
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private AvalonTypographer()
            {
                // BUGBUG : Release / Dispose / Clean up Avalon object created here

                // another Dev Hack to avoid throwing by missing Dispatcher 
//                Dispatcher uct = new Dispatcher();
//                uct.Enter();
//                Window win = new Window(uct);
                Window win = new Window();
                ForegroundColor = new ColorByte(255, 0, 0, 0);
                BackgroundColor = new ColorByte(255, 255, 255, 255);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods 
                private Typeface getTypeface()
                {
                    return new Typeface(FontName);
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Renders the text with the font at the specified size
                /// </summary>
                public IImageAdapter Render(string s, string f, float sz)
                {
                    
                    Text = s;
                    FontName = f;
                    FontSize = sz;
                    return Render();
                }
                /// <summary>
                /// return the size of the drawn text 
                /// </summary>
                public SizeF Measure()
                {
                    FormattedText ft = new FormattedText(Text,CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, getTypeface(), FontSize, new System.Windows.Media.SolidColorBrush(_foreGround));
                    return new SizeF((float)ft.Width, (float)ft.Height);
                }
                /// <summary>
                /// renders the text based on the properties set
                /// </summary>
                public IImageAdapter Render()
                {
                    IImageAdapter retVal = null;
                    mFormattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, getTypeface(), FontSize, new System.Windows.Media.SolidColorBrush(_foreGround));
                    int textWidth = (int)mFormattedText.Width;
                    int textHeight = (int)mFormattedText.Height;
                    using (ImageUtility imageUtil = new ImageUtility(textWidth, textHeight))
                    {
                        imageUtil.GetSetPixelUnsafeBegin();
                        int stride = textWidth * 4;
                        byte[] milBuffer = new byte[stride * textHeight];
                        RenderTargetBitmap renderTargeBitmap = (RenderTargetBitmap)RenderTargetBitmap.Create(textWidth, textHeight, 96, 96, PixelFormats.Bgra32, null, milBuffer, stride);
                        {
                            Visual visual = new TypoDrawingVisual(mFormattedText, _backGround);
                            renderTargeBitmap.Render(visual);
                            renderTargeBitmap.CopyPixels(milBuffer, stride, 0);

                            int index = 0;
                            byte[] vscanBuffer = imageUtil.GetStreamBufferBGRA();
                            for (int y = 0; y < textHeight; y++)
                            {
                                for (int x = 0; x < textWidth; x++)
                                {
                                    index = 4 * (y * textWidth + x);

                                    vscanBuffer[index] = milBuffer[index];
                                    vscanBuffer[index + 1] = milBuffer[index + 1];
                                    vscanBuffer[index + 2] = milBuffer[index + 2];
                                    vscanBuffer[index + 3] = milBuffer[index + 3];
                                }
                            }
                        }
                        imageUtil.GetSetPixelUnsafeCommit();
                        retVal = new ImageAdapter(imageUtil.Bitmap32Bits);
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }

    /// <summary>
    /// The Typograph Visual that will do the rendering. _WARNING_ Suboptimal dev impl will go away in M8
    /// </summary>
    public class TypoDrawingVisual : DrawingVisual
    {
        #region Properties
            private FormattedText _ftxt = null;
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The Constructor
            /// </summary>
            public TypoDrawingVisual(FormattedText ftxt, System.Windows.Media.Color bg) : base()
            {
                using (DrawingContext ctx = RenderOpen())
                {
                    _ftxt = ftxt;
                    Render(ctx, bg);
                }
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Renders the Formatted text with the bg color
            /// </summary>
            private void Render(DrawingContext ctx, System.Windows.Media.Color bg)
            {
                if (ctx == null || _ftxt == null) { return; }

                System.Windows.Media.SolidColorBrush scb = new System.Windows.Media.SolidColorBrush(bg);
                ctx.DrawRectangle(scb, new System.Windows.Media.Pen(scb, 1), new System.Windows.Rect(0, 0, _ftxt.Width + 2, _ftxt.Height + 2));
                ctx.DrawText(_ftxt, new System.Windows.Point(0, 0));
            }
        #endregion Methods
    }
}
