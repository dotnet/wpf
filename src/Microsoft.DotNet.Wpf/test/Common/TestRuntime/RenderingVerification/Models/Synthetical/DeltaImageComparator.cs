// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Collections;
        using System.Windows.Forms;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
        using Microsoft.Test.RenderingVerification.Filters;
        using Microsoft.Test.RenderingVerification.Model.Analytical;
    #endregion using

      /// <summary>
    /// Summary description for DeltaComparatorResult.
    /// </summary>
    public sealed class Result
    {
        #region Constants
            /// <summary>
            /// TopLeft corner index
            /// </summary>
            public const int TopLeft = 0;
            /// <summary>
            /// TopRight corner index
            /// </summary>
            public const int TopRight = 1;
            /// <summary>
            /// BottomLeft corner index
            /// </summary>
            public const int BottomLeft = 2;
            /// <summary>
            /// BottomRight corner index
            /// </summary>
            public const int BottomRight = 3;
            /// <summary>
            /// X index
            /// </summary>
            public const int X = 0;
            /// <summary>
            /// Y index
            /// </summary>
            public const int Y = 1;
        #endregion Constants
        
        #region Properties
            #region Private Properties
                private bool _found = false;
                private double[,] _cornerLocations = null;
                private string _log = "";
            #endregion PrivateProperties
            #region public Properties
                /// <summary>
                /// Indicates if the Delta content found
                /// </summary>
                public bool Found
                {
                    get { return _found; }
                }
                /// <summary>
                /// Log info sent back to the caller
                /// mainly for debug purposes
                /// </summary>
                public double[,] CornerLocations
                {
                    get { return _cornerLocations; }
                }
                /// <summary>
                /// Log info sent back to the caller
                /// mainly for debug purposes
                /// </summary>
                public string Log
                {
                    get { return _log; }
                }
            #endregion public Properties
        #endregion Properties

        #region Constructors
            internal Result() : this(false,"<!Uninitialized Result>",null)
            {
            }
            internal Result(bool found, string log, double[,] corners)
            {
                _found = found;
                if (log != null)
                {
                    _log = log;
                }

                if (corners != null)
                {
                    if (corners.GetUpperBound(0)+1 != 4 || corners.GetUpperBound(1)+1 != 2)
                    {
                        throw new RenderingVerificationException("Corners should be a double[4,2] array");
                    }

                    _cornerLocations = (double[,])corners.Clone();
                }
            }
        #endregion Constructors
    }

    /// <summary>
    /// Summary description for DeltaImageComparator
    /// </summary>
    public class DeltaImageComparator
    {
        #region Properties
            #region Private Properties
                private SortedList _tolerance = null;
                private double[,] _contentLocation = new double[4, 2];
                private double _imageAdapterXmin = double.NaN;
                private double _imageAdapterYmin = double.NaN;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// set the tolerance used by the comparison 
                /// </summary>
                public SortedList Tolerance
                {
                    get { return _tolerance; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of the DeltaImageComparator class
            /// </summary>
            public DeltaImageComparator()
            {
                _tolerance = new SortedList();
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private ImageAdapter GetTransformedContent(IImageAdapter imageAdapter, double[] transformMatrix, int width, int height)
                {
                    // validate the variation
                    //extract the popup from the original
                    GlyphModel glmodel = new GlyphModel();

                    //todo work with the minsize bounding box - add the transfrom from XTC
                    glmodel.Size = new System.Drawing.Size(width, height);

                    GlyphImage glimage = new GlyphImage(glmodel);

                    glmodel.Glyphs.Add(glimage);
                    ImageUtility.ToImageFile(imageAdapter,"foo.png", ImageFormat.Png);
                    glimage.Path = "foo.png";
                    transformMatrix[1] = -transformMatrix[1];
                    transformMatrix[2] = -transformMatrix[2];
                    glimage.Panel.Transform.Matrix = transformMatrix;

                    double[] ltrans = glimage.Panel.Transform.Matrix;
                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    double maxX = double.MinValue;
                    double maxY = double.MinValue;
                    double[,] tp = new double[,] { { 0, 0 } , { imageAdapter.Width, 0 } , { 0, imageAdapter.Height } , { imageAdapter.Width, imageAdapter.Height } };

                    for (int j = 0; j < 4; j++)
                    {
                        double lxp = ltrans[0] * tp[j, 0] + ltrans[1] * tp[j, 1] + ltrans[4];
                        double lyp = ltrans[2] * tp[j, 0] + ltrans[3] * tp[j, 1] + ltrans[5];

                        _contentLocation[j, 0] = lxp;
                        _contentLocation[j, 1] = lyp;
                        Console.WriteLine(lxp + "," + lyp);
                        if (lxp > maxX) { maxX = lxp; }

                        if (lyp > maxY) { maxY = lyp; }

                        if (lxp < minX) { minX = lxp; }

                        if (lyp < minY) { minY = lyp; }
                    }

                    _imageAdapterXmin = minX;
                    _imageAdapterYmin = minY;
                    Console.WriteLine(minX + " " + minY + "    " + maxX + " " + maxY);
                    transformMatrix[1] = -transformMatrix[1];
                    transformMatrix[2] = -transformMatrix[2];
                    glimage.Panel.Transform.Matrix = transformMatrix;

                    ImageAdapter res = new ImageAdapter((int)(maxX - minX), (int)(maxY - minY));
                    ImageAdapter buffer = new ImageAdapter(glmodel.Size.Width, glmodel.Size.Height);
                    glimage.Render();
                    Point pt = new Point((int)(glimage.Position.X + .5), (int)(glimage.Position.Y + .5));
                    ImageUtility.CopyImageAdapter(buffer, glimage.GeneratedImage, pt, glimage.Size, true);

                    int mixx = int.MaxValue;
                    int mixy = int.MaxValue;

                    for (int j = 0; j < buffer.Width; j++)
                    {
                        for (int i = 0; i < buffer.Height; i++)
                        {
                            if (buffer[j, i].Alpha != 0)
                            {
                                if (j < mixx) { mixx = j; }

                                if (i < mixy) { mixy = i; }
                            }
                        }
                    }

                    Console.WriteLine("comp " + mixx + " " + mixy);
                    for (int j = 0; j < res.Width; j++)
                    {
                        for (int i = 0; i < res.Height; i++)
                        {
                            if (j + mixx >= 0 && j + mixx < buffer.Width && i + mixy >= 0 && i + mixy < buffer.Height)
                            {
                                res[j, i] = buffer[j + mixx, i + mixy];
                            }
                        }
                    }
                    string path = "TransPost.png";
#if DEBUG
                    ImageUtility.ToImageFile(res, path);
#endif
                    glimage.Path = path;
                    return res;
                }
                private static void DRT(string[] args)
                {
                    try
                    {
                        DeltaImageComparator DIComp = new DeltaImageComparator();
                        ImageAdapter cntb = new ImageAdapter(new Bitmap(args[0]));
                        ImageAdapter offcnt = new ImageAdapter(new Bitmap(args[1]));
                        ImageAdapter oncnt = new ImageAdapter(new Bitmap(args[2]));
                        double[] transform = new double[6];

                        for (int j = 0; j < 6; j++)
                        {
                            transform[j] = double.Parse(args[3 + j]);
                        }

                        //add tolerance to the comparison
                        DIComp.Tolerance.Add(0, 100);
                        DIComp.Tolerance.Add(10, 80);
                        DIComp.Tolerance.Add(50, 40);
                        DIComp.Tolerance.Add(100, 10);
                        DIComp.Tolerance.Add(200, 1);

                        //client to provide geometry and alpha map in content
                        // emulating a real scenario here
                        ImageAdapter cnt = new ImageAdapter(300, 200);

                        for (int j = 0; j < cnt.Width; j++)
                        {
                            for (int i = 0; i < cnt.Height; i++)
                            {
                                IColor vsc = (IColor)cntb[j, i].Clone();
                                vsc.A = 127;
                                cnt[j, i] = vsc;
                            }
                        }

                        Result decres = DIComp.Process(cnt, offcnt, oncnt, transform);

                        Console.WriteLine("Dynamic Content tracking result:");
                        Console.WriteLine("  Found: " + decres.Found + "   Log: " + decres.Log);
                        Console.WriteLine("\n");
                        if (decres.Found == true)
                        {
                            // const int are defined in DeltaImageComparator.Result to access each corner from Result.CornerLocation
                            Console.WriteLine("            TopLeft coordinates: " + decres.CornerLocations[Result.BottomLeft, 0] + " " + decres.CornerLocations[Result.BottomLeft, 1]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            #endregion PrivateMethods
            #region Public Methods
                /// <summary>
                /// The scanner attemps to locate content assuming that:
                ///   contentOff is the imagaeadapter of the scene before content is shown
                ///   contentOn  is the imagaeadapter of the scene with the content shown
                /// Note: content may have a variable alpha map 
                /// </summary>
                public Result Process(IImageAdapter content, IImageAdapter contentOff, IImageAdapter contentOn, double[] transformMatrix)
                {
                    string paramEx = string.Empty;
                    if (content == null) { paramEx = "<IImageAdapter content> "; }
                    if (contentOff == null) { paramEx += "<IImageAdapter contentOff>"; }
                    if (contentOn == null) { paramEx += "<IImageAdapter contentOn>"; }
                    if (content == null) { paramEx += "<IImageAdapter content>"; }
                    if (transformMatrix == null) { paramEx += "<double []transformMatrix>"; }
/*
                    if (transformMatrix.Length != Interpolator.TransformLength)
                    {
                        throw new Exception("Transform array length != " + Interpolator.TransformLength);
                    }
*/
                    //sentinel
                    bool validResult = true;
                    string log = string.Empty;
                    ImageAdapter res = GetTransformedContent(content, transformMatrix, contentOff.Width, contentOff.Height);

                    // locate rec 
                    //  1) compute the diff image
                    //  2) synthetise the transformed content
                    //  3) compare with the sourceOn
                    ImageComparator imageComparator = new ImageComparator();
                    imageComparator.Compare(contentOn, contentOff);
                    IImageAdapter imdiff = imageComparator.GetErrorDifference(ErrorDifferenceType.FilterEdge);

                    // make sure there is only one descriptor and the background.
                    //convert the diff image to a binary image (black-bg and white-fg)
                    ImageAdapter imbinmap = new ImageAdapter(imdiff.Width, imdiff.Height);
                    IColor black = new ColorByte(Color.Black);
                    IColor white = new ColorByte(Color.White);

                    for (int j = 0; j < imbinmap.Width; j++)
                    {
                        for (int i = 0; i < imbinmap.Height; i++)
                        {
                            if (imdiff[j, i].Red + imdiff[j, i].Green + imdiff[j, i].Blue > 1e-6)
                            {
                                imdiff[j, i] = white;
                            }
                            else
                            {
                                imdiff[j, i] = black;
                            }
                        }
                    }
#if DEBUG
                    ImageUtility.ToImageFile(imdiff, "bwmap.png", ImageFormat.Png);
#endif
                    //Analyze the bin-diff-image
                    VScan lvsc = new VScan(imdiff);
                    lvsc.OriginalData.Analyze();

                    //topological check
                    //root nodes: either a pair of white and black descriptor
                    //or a tree.
                    // all further descendant must be children of the white cell
/*
                    int[] descriptorCounter = new int[2];

                    //loop on the descriptors
                    foreach (IDescriptor desc in lvsc.OriginalData.Descriptors.ActiveDescriptors)
                    {
                        if (desc.Depth <= 1)
                        {
                            descriptorCounter[desc.Depth]++;
                        }

                        Console.WriteLine("Descr " + desc.BoundingBox + "   " + desc.Depth);
                    }

                    //check 
                    int summ = descriptorCounter[0] + descriptorCounter[1];
                    if (summ != 2)
                    {
                        validResult = false;
                        if (summ == 0)
                        {
                            log = "<Fail> No top level descriptors found";
                        }
                        else
                        {
                            log = "<Fail> Too many top level descriptors found (should be two) :" + summ;
                        }
                    }
*/
                    // topology is good to go, time to find the bounding box of the dynamic content
                    int minx = int.MaxValue;
                    int miny = int.MaxValue;
                    int maxx = int.MinValue;
                    int maxy = int.MinValue;
                    if (validResult == true)
                    {
                        for (int j = 0; j < imdiff.Width; j++)
                        {
                            for (int i = 0; i < imdiff.Height; i++)
                            {
                                double sum = imdiff[j, i].Blue + imdiff[j, i].Red + imdiff[j, i].Green;
                                if (sum > 1e-6)
                                {
                                    if (j < minx) { minx = j; }
                                    if (i < miny) { miny = i; }
                                    if (j > maxx) { maxx = j; }
                                    if (i > maxy) { maxy = i; }
                                }
                            }
                        }

                        // bounding box
                        maxx -= minx;
                        maxy -= miny;
                        Console.WriteLine("<Target> found at " + minx + " " + miny + "  " + maxx + " " + maxy);
                        ImageUtility.ToImageFile(imdiff, "Recpos.png");

                        // synthetize content into contentOff
                        IImageAdapter iafcomp = new ImageAdapter(contentOff.Width, contentOff.Height);
                        double dx = minx - _imageAdapterXmin;
                        double dy = miny - _imageAdapterYmin;

                        // translate results
                        for (int j = 0; j < 4; j++)
                        {
                            _contentLocation[j, 0] += dx;
                            _contentLocation[j, 1] += dy;
                        }

                        // copy the background
                        iafcomp = (IImageAdapter)contentOff.Clone();

                        // add the transformed content
                        for (int j = 0; j < res.Width; j++)
                        {
                            for (int i = 0; i < res.Height; i++)
                            {
                                if (j + minx < iafcomp.Width && i + miny < iafcomp.Height)
                                {
                                    if (res[j, i].Alpha > 0)
                                    {
                                        double lalp = res[j, i].Alpha;
                                        int jid = j + minx;
                                        int iid = i + miny;
                                        IColor lvc = iafcomp[jid, iid];
                                        lvc.Red = lalp * res[j, i].Red + (1 - lalp) * iafcomp[jid, iid].Red;
                                        lvc.Green = lalp * res[j, i].Green + (1 - lalp) * iafcomp[jid, iid].Green;
                                        lvc.Blue = lalp * res[j, i].Blue + (1 - lalp) * iafcomp[jid, iid].Blue;
                                        iafcomp[jid, iid] = lvc;
                                    }
                                }
                            }
                        }
#if DEBUG
                        ImageUtility.ToImageFile(iafcomp, "SynthGlyph.png",ImageFormat.Png);
#endif
/*
                        if (Tolerance != null)
                        {
                            imageComparator.Tolerance.Clear();
                            double x = double.NaN;
                            double y = double.NaN;
                            for(int t=0;t<Tolerance.Count;t++)
                            {
                                x = (double)Tolerance.GetKey(t);
                                y = (double)Tolerance[x];
                                imageComparator.Tolerance.Add(x,y);
                            }
                        }
*/
                        validResult = imageComparator.Compare(iafcomp, contentOn);
                        string toluse = "No Tolerance used - strict comparison";
                        if (_tolerance != null)
                        {
                            toluse = "with the given Tolerance";
                        }
                        if (validResult == false)
                        {
                            log = "<Fail> Computed Content does not match actual content -- " + toluse;
                        }
                        else
                        {
                            log = "<Pass>";
                        }

                        Console.WriteLine("Final comp pass " + validResult);
                        ImageUtility.ToImageFile(imageComparator.GetErrorDifference(ErrorDifferenceType.FilterEdge), "SynthError.png");
#if DEBUG
                        using (Bitmap fbmp = ImageUtility.ToBitmap(contentOn))
                        {
                            using (Graphics graphics = Graphics.FromImage(fbmp))
                            {
                                using (Brush brush = new SolidBrush(Color.FromArgb(40, 255, 0, 0)))
                                {
                                    graphics.FillRectangle(brush, minx, miny, maxx, maxy);
                                }
                                using (Pen pen = new Pen(Color.Red, 2))
                                {
                                    graphics.DrawRectangle(pen, minx, miny, maxx, maxy);
                                }
                                using (Font fnt = new Font("Arial", 10))
                                {
                                    SizeF sz = graphics.MeasureString("TL", fnt);
                                    graphics.FillRectangle(Brushes.Yellow, (float)_contentLocation[0, 0], (float)_contentLocation[0, 1], sz.Width, sz.Height);
                                    graphics.FillRectangle(Brushes.Yellow, (float)_contentLocation[1, 0], (float)_contentLocation[1, 1], sz.Width, sz.Height);
                                    graphics.FillRectangle(Brushes.Yellow, (float)_contentLocation[2, 0], (float)_contentLocation[2, 1], sz.Width, sz.Height);
                                    graphics.FillRectangle(Brushes.Yellow, (float)_contentLocation[3, 0], (float)_contentLocation[3, 1], sz.Width, sz.Height);
                                    graphics.DrawString("TL", fnt, Brushes.Red, (float)_contentLocation[0, 0], (float)_contentLocation[0, 1]);
                                    graphics.DrawString("TR", fnt, Brushes.Red, (float)_contentLocation[1, 0], (float)_contentLocation[1, 1]);
                                    graphics.DrawString("BL", fnt, Brushes.Red, (float)_contentLocation[2, 0], (float)_contentLocation[2, 1]);
                                    graphics.DrawString("BR", fnt, Brushes.Red, (float)_contentLocation[3, 0], (float)_contentLocation[3, 1]);
                                }
                                fbmp.Save("TrackMatch.png");
                            }
                        }
#endif
                    }
                    Result dcres = new Result(validResult, log, _contentLocation);

                    return dcres;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
