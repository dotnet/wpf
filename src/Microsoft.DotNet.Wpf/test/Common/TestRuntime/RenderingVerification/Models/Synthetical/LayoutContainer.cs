// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    using System;
        using System.Collections;
        using System.Drawing;
    using System.Xml.Serialization;

    /// <summary>
    /// Layout modes
    /// </summary>
    public enum LayoutMode
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Flow
        /// </summary>
        Flow,
        /// <summary>
        /// Flow Iso
        /// </summary>
        FlowIso
    }


    /// <summary>
    /// Summary description for LayoutContainer.
    /// </summary>
    ///
    [XmlRoot("LayoutContainer")]
    public class LayoutContainer: GlyphContainer
    {
        #region Properties
            #region Private Properties
                private LayoutMode _mode = LayoutMode.None;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The Layout Mode 
                /// </summary>
                public LayoutMode Mode
                {
                    set
                    {
                        _mode = value;
                    }
                    get
                    {
                        return _mode;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// default constructor - XML serialzation only
            /// </summary>
            public LayoutContainer() 
            {
            }
            /// <summary>
            /// Constructor
            /// </summary>
            public LayoutContainer(GlyphModel model) : base(model) 
            {
            }
        #endregion Constructors

        #region Methods
            #region Internal Methods
                /// <summary>
                /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    throw new NotImplementedException();
/*
                    ArrayList bboxes = new ArrayList();
                    float maxHeight = 0f;

                    for (int k = 0; k < Glyphs.Length; k++)
                    {
                        if (Mode == LayoutMode.Flow || Mode == LayoutMode.FlowIso)
                        {
                            GlyphText glt = Glyphs[k] as GlyphText;
                            if (glt != null)
                            {
                                foreach (string token in glt.Tokens)
                                {
                                    SizeF sz = graphics.MeasureString(token, glt.Font);
                                    if (sz.Height > maxHeight)
                                    {
                                        maxHeight = sz.Height;
                                    }

                                    Console.WriteLine(sz);
                                    bboxes.Add(sz);
                                }
                            }
                            else
                            {
                                Glyphs[k].Measure();
                                Console.WriteLine(Glyphs[k].BoundingBox);
                                SizeF sz = new SizeF(Glyphs[k].BoundingBox.Width, Glyphs[k].BoundingBox.Height);
                                bboxes.Add(sz);
                                if (Glyphs[k].BoundingBox.Height > maxHeight)
                                {
                                    maxHeight = Glyphs[k].BoundingBox.Height;
                                }
                            }
                        }
                    }

                    double idx = 0;
                    double idy = 0;

                    for (int k = 0; k < Glyphs.Length; k++)
                    {
                        GlyphText glt = Glyphs[k] as GlyphText;
                        if (glt != null)
                        {
                            foreach (string token in glt.Tokens)
                            {
                                SizeF sz = graphics.MeasureString(token, glt.Font);
                                if (idx + glt.MeasureToken(token).Width > Width)
                                {
                                    idx = 0;
                                    idy += maxHeight;
                                }

                                glt.RenderSub(graphics, token, (float)idx, (float)idy);
                                idx += glt.MeasureToken(token).Width;
                            }
                        }
                        else
                        {

                            if (idx + Glyphs[k].Width > Width)
                            {
                                idx = 0;
                                idy += maxHeight;
                            }

                            Glyphs[k].OffsetX = (float)idx;
                            Glyphs[k].OffsetY = (float)idy;
                            Glyphs[k]._Render(graphics);

                            idx += Glyphs[k].Width;
                        }
                    }
*/
                }
            #endregion Internal Methods
            #region Public Methods
                /// <summary>
                /// Measure the content
                /// </summary>
                public override SizeF Measure()
                {
                    return SizeF.Empty;
                }
/*
                /// <summary>
                /// Renders into an ImageAdapter
                /// </summary>
                public override ImageAdapter RenderObject(ImageAdapter image)
                {
                    ArrayList bboxes = new ArrayList();

                    float maxHeight = 0f;
                    for (int k = 0; k < Glyphs.Length; k++)
                    {
                        if (Mode == LayoutMode.Flow || Mode == LayoutMode.FlowIso)
                        {
                            GlyphText glt = Glyphs[k] as GlyphText;

                            if (glt != null)
                            {
                                foreach (string token in glt.Tokens)
                                {
                                    SizeF sz = glt.MeasureToken(token);
                                    RectangleF recf = new RectangleF(0f, 0f, sz.Width, sz.Height);
                                    if (recf.Height > maxHeight)
                                    {
                                        maxHeight = recf.Height;
                                    }
                                    Console.WriteLine(recf);
                                    bboxes.Add(recf);
                                }
                            }
                            else
                            {
                                Glyphs[k].Measure();
                                Console.WriteLine(Glyphs[k].BoundingBox);
                                bboxes.Add(Glyphs[k].BoundingBox);
                                if (Glyphs[k].BoundingBox.Height > maxHeight)
                                {
                                    maxHeight = Glyphs[k].BoundingBox.Height;
                                }
                            }
                        }
                    }

                    double idx = 0;
                    double idy = 0;
                    for (int k = 0; k < Glyphs.Length; k++)
                    {

                        GlyphText glt = Glyphs[k] as GlyphText;

                        if (glt != null)
                        {
                            foreach (string token in glt.Tokens)
                            {
                                ImageAdapter glyphRes = glt.RenderToken(token);

                                if (idx + glt.MeasureToken(token).Width > image.Width)
                                {
                                    idx = 0;
                                    idy += maxHeight;
                                }

                                for (int i = 0; i < glyphRes.Width && i + idx < image.Width; i++)
                                {
                                    for (int j = 0; j < glyphRes.Height && j + idy < image.Height; j++)
                                    {
                                        if (glyphRes[i, j].IsEmpty == true)
                                        {
                                            image[i + (int)idx, j + (int)idy] = glyphRes[i, j];
                                        }
                                    }
                                }
                                idx += glt.MeasureToken(token).Width;
                            }
                        }
                        else
                        {
                            ImageAdapter iap = Glyphs[k].Render(image.Width, image.Height);

                            if (idx + Glyphs[k].Width > image.Width)
                            {
                                idx = 0;
                                idy += maxHeight;
                            }

                            for (int i = 0; i < iap.Width && i + idx < image.Width; i++)
                            {
                                for (int j = 0; j < iap.Height && j + idy < image.Height; j++)
                                {
                                    if (iap[i, j].IsEmpty == true)
                                    {
                                        if (image[i + (int)idx, j + (int)idy].IsEmpty == false)
                                        {
                                            image[i + (int)idx, j + (int)idy] = iap[i, j];
                                        }
                                        else
                                        {
                                            IColor lc = (IColor)image[i + (int)idx, j + (int)idy].Clone();
                                            IColor lbl = iap[i, j];

                                            lc.Red = lbl.Alpha * lbl.Red + (1.0 - lbl.Alpha) * lc.Red;
                                            lc.Green = lbl.Alpha * lbl.Green + (1.0 - lbl.Alpha) * lc.Green;
                                            lc.Blue = lbl.Alpha * lbl.Blue + (1.0 - lbl.Alpha) * lc.Blue;
                                            image[i + (int)idx, j + (int)idy] = lc;
                                        }
                                    }

                                }
                            }
                            idx += Glyphs[k].Width;
                        }
                    }

                    return image;
                }
*/
            #endregion Public Methods
        #endregion Methods
    }
}

