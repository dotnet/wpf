// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region Usings
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Windows.Forms;
        using System.Drawing.Imaging;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        using Microsoft.Test.RenderingVerification;
        using Microsoft.Test.RenderingVerification.Filters;
        using Microsoft.Test.RenderingVerification.UnmanagedProxies;
#if (!STRESS_RUNTIME)
        using Microsoft.Test.Logging;
#endif
    using System.Diagnostics;
    #endregion Usings

    #region ErrorDifferenceType Enum
        /// <summary>
        /// The type of error difference
        /// </summary>
        public enum ErrorDifferenceType
        { 
            /// <summary>
            /// No post process
            /// </summary>
            Regular = 1,
            /// <summary>
            /// Force the alpha channel to 255 (full opacity)
            /// </summary>
            IgnoreAlpha = 2,
            /// <summary>
            /// Saturate color
            /// </summary>
            Saturate = 3,
            /// <summary>
            /// Filter edge
            /// </summary>
            FilterEdge = 4
        }
    #endregion ErrorDifferenceType Enum

    /// <summary>
    /// Image comparison based on double precision colors
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name="FullTrust")]
    public class ImageComparator : IImageComparator, IImageComparatorUnmanaged
    {
        #region Delegates
            /// <summary>
            /// the delegate to compare two colors based on channels
            /// </summary>
            internal delegate double CompareChannelDiff (IColor c1, IColor c2, out IColor diffColor);
        #endregion Delegates

        #region Properties
            #region Private Properties
                private IColor _saturatedColor = new ColorByte(Color.Lime);
                private IImageAdapter _source = null;
                private IImageAdapter _target = null;
                private IImageAdapter _errImage = null;
                private IImageAdapter _transformedImage = null;
                private Matrix2D _transformMatrix = null;
                //private SortedList _tolerance = null;
                //private SortedList _unscaledTolerance = null; 
                private Curve _unscaledCurve = null;
                private Curve _curve = null;
                private int _filterLevel = 0;
                private ChannelCompareMode _channels = ChannelCompareMode.Unknown;
                private double[] _errorDistance = null;
                private float _errDiffSum = 0;
                private MismatchingPoints _mismatchingPoints = null;
                private double _RGBEdgeCoefficient = 0;
                private double _RedEdgeCoefficient = 0;
                private double _GreenEdgeCoefficient = 0;
                private double _BlueEdgeCoefficient = 0;
                private static XmlDocument _xmlDoc = null;
                private bool verboseOutput = false;
            #endregion Privates Properties
            #region Public Properties
                /// <summary>
                /// Get/set the debugging mode (debug mode will output info to console).
                /// </summary>
                public bool Debug = false;
                /// <summary>
                /// Get/set the comparison mode {ARGB,RGB,A,R,G,B}
                /// </summary>
                public ChannelCompareMode ChannelsInUse
                {
                    set
                    {
                        DebugLog("Using ChannelCompareMode = " + value.ToString());
                        if (value == ChannelCompareMode.Unknown)
                        {
                            throw new RenderingVerificationException("Cannot set the comparison type to this value");
                        }
                        _channels = value;
                    }
                    get
                    {
                        return _channels;
                    }
                }
                /// <summary>
                /// The color to use for saturation
                /// </summary>
                /// <value></value>
                public IColor SaturatedColor
                {
                    get 
                    {
                        return _saturatedColor;
                    }
                    set 
                    {
                        _saturatedColor = value;
                    }
                }
                /// <summary>
                /// Get the image after the transform took place
                /// </summary>
                /// <value></value>
                public IImageAdapter TransformedImage
                {
                    get 
                    {
                        return _transformedImage;
                    }
                }
                /// <summary>
                /// the 2d affine transorm used - it has to be inversible 
                /// </summary>
                public Matrix2D TransformMatrix
                {
                    set
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("TransformMatrix","The value passed in must be a valid instance of a Matrix2D object (null passed in)");
                        }
                        if (value.IsInvertible == false)
                        {
                            throw new Matrix2DException("The Matrix provided is not inversible !");
                        }
                        DebugLog("Using a matrix transform");

                        _transformMatrix = value;
                    }
                    get
                    {
                        return _transformMatrix;
                    }
                }
                /// <summary>
                /// When a transform leave a part of the image empty, fill with this color
                /// </summary>
                public IColor BackgroundColor = ColorByte.Empty;
                /// <summary>
                /// Return the Curve object associated with this imagecompartor
                /// </summary>
                public Curve Curve
                {
                    get { return (_curve != null) ? _curve : _unscaledCurve; }
                }
                /// <summary>
                /// Filter the image using a Pixelize filter with SquareSize=2^FilterLevel, ExtendedSize=SquareSize*2 and UseMean=false.
                /// This low pass will smooth down the sharp transistion and lower the image quality, making it more likely to pass a comparison where small difference are present.
                /// Note : if FilterLevel is set to 0, no filtering take place (won't average 2 pixels although is should since ExtendedSize becomes 2)
                /// </summary>
                public int FilterLevel
                {
                    set
                    {
                        DebugLog("FilterLevel set to " + value.ToString());
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException("FilterLevel", "The value to be set must be positive (zero included)");
                        }
                        _filterLevel = value;
                    }
                    get
                    {
                        return _filterLevel;
                    }
                }
                /// <summary>
                /// Get an array of distance from the original
                /// </summary>
                public double[] ErrorDistance
                {
                    get
                    {
                        return _errorDistance;
                    }
                }
                /// <summary>
                /// Get the Mismatching pixel based on distance from original
                /// </summary>
                public MismatchingPoints MismatchingPoints
                {
                    get
                    {
                        return _mismatchingPoints;
                    }
                }
                /// <summary>
                /// Return the sum of difference in the image
                /// </summary>
                /// <value>A number representing the difference between the images</value>
                public float ErrorDiffSum
                {
                    get
                    {
                        return _errDiffSum;
                    }
                }
                /// <summary>
                /// The EdgeCoefficient coefficient for RGB Edges 
                /// </summary>
                /// <value>A number representing the difference between the images</value>
                public double RGBEdgeCoefficient
                {
                    set
                    {
                        _RGBEdgeCoefficient = UnitNormalizeCoefficient(value);

                        //copy the value on the actual coefficients
                        _RedEdgeCoefficient = _RGBEdgeCoefficient;
                        _GreenEdgeCoefficient = _RGBEdgeCoefficient;
                        _BlueEdgeCoefficient = _RGBEdgeCoefficient;
                    }
                    get
                    {
                        return _RGBEdgeCoefficient;
                    }
                }
                /// <summary>
                /// The EdgeCoefficient coefficient for Red Edges 
                /// </summary>summary>
                /// <value>A number representing the difference between the images</value>
                public double RedEdgeCoefficient
                {
                    set
                    {
                        _RedEdgeCoefficient = UnitNormalizeCoefficient(value);
                    }
                    get
                    {
                        return _RedEdgeCoefficient;
                    }
                }
                /// <summary>
                /// The EdgeCoefficient coefficient for Red Edges 
                /// </summary>
                /// <value>A number representing the difference between the images</value>
                public double GreenEdgeCoefficient
                {
                    set
                    {
                        _GreenEdgeCoefficient = UnitNormalizeCoefficient(value);
                    }
                    get
                    {
                        return _GreenEdgeCoefficient;
                    }
                }
                /// <summary>
                /// The EdgeCoefficient coefficient for Blue Edges 
                /// </summary>
                /// <value>A number representing the difference between the images</value>
                public double BlueEdgeCoefficient
                {
                    set
                    {
                        _BlueEdgeCoefficient = UnitNormalizeCoefficient(value);
                    }
                    get
                    {
                        return _RedEdgeCoefficient;
                    }
                }

                /// <summary>
                /// This property determines if detailed compare information is logged to the console
                /// This value should be set before any call to "Compare"
                /// </summary>
                public bool VerboseOutput
                {
                    set
                    {
                        verboseOutput = value;
                    }
                    get
                    {
                        return verboseOutput;
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
                static ImageComparator()
                {
                    _xmlDoc = new XmlDocument();
#if VSBUILD
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Code.Microsoft.Test.RenderingVerification.DefaultTolerance.xml");
#else
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Test.RenderingVerification.RenderingVerification.DefaultTolerance.xml");
#endif
                    _xmlDoc.Load(stream);
                }
                /// <summary>
            /// Instantiate an ImageComparator Object
            /// </summary>
            public ImageComparator()
            {
                RGBEdgeCoefficient = 3.0;
                RedEdgeCoefficient = 1.0;
                GreenEdgeCoefficient = 1.0;
                BlueEdgeCoefficient = 1.0;
                _channels = ChannelCompareMode.ARGB;
                // Set the default tolerance
                _unscaledCurve = new Curve();
                _unscaledCurve.CurveTolerance.LoadTolerance(_xmlDoc.DocumentElement);
            }
            /// <summary>
            /// Instantiate an ImageComparator Object and set the ChannelCompareMode to use
            /// </summary>
            /// <param name="channelsInUse">ChannelCompareMode to use for the comparison</param>
            public ImageComparator(ChannelCompareMode channelsInUse) : this()
            {
                ChannelsInUse = channelsInUse;
            }
            /// <summary>
            /// Instantiate an ImageComparator Object and set a tolerance
            /// </summary>
            /// <param name="curveTolerance">The list of point delimiting the acceptable values</param>
            public ImageComparator(CurveTolerance curveTolerance) : this()
            {
                _unscaledCurve.CurveTolerance = curveTolerance;
            }
            /// <summary>
            /// Instantiate an ImageComparator Object and set the color to be used as background color (when a transform occurs and set the pixel to empty)
            /// </summary>
            /// <param name="backGroundColor"></param>
            public ImageComparator(IColor backGroundColor) : this()
            {
                BackgroundColor = (IColor)backGroundColor.Clone();
            }
            /// <summary>
            /// Instantiate an ImageComparator Object, set the tolerance and the FilterLevel
            /// </summary>
            /// <param name="curveTolerance">The list of point delimiting the acceptable area</param>
            /// <param name="filterLevel">The filtering level (will perform a pixelize filter with an averaging : smooth out image and loose pixel precision)</param>
            public ImageComparator(CurveTolerance curveTolerance, int filterLevel) : this(curveTolerance)
            {
                FilterLevel = filterLevel;
            }
            /// <summary>
            /// Instantiate an ImageComparator Object, set the transform to be using and the background color (for empty entries caused by the  transform)
            /// </summary>
            /// <param name="transformMatrix">The transformation matrix</param>
            /// <param name="backGroundColor">The background color to use</param>
            public ImageComparator(Matrix2D transformMatrix, IColor backGroundColor) : this(backGroundColor)
            {
                TransformMatrix = (Matrix2D)transformMatrix.Clone();
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private double UnitNormalizeCoefficient(double val)
                {
                    if (val > 1) { val = 1; }
                    if (val < 0) { val = 0; }
                    return val;
                }
                private void DebugLog(string output)
                {
                    if (Debug) 
                    {
                        Console.WriteLine(output); 
                    }
                }
                /// <summary>
                /// Matches the error against the tolerance and returns a pass/fail result
                /// <param name="errorDistance">the error histogram in %</param>
                /// <returns>true if all success (all point of error histogram below the curve tolerance set), false otherwise</returns>
                /// </summary>
                private bool ComputeResult (double[] errorDistance)
                {
                    if (errorDistance == null)
                    {
                        throw new ArgumentException ("Error Array can not be null");
                    }

                    bool passResult = true;

                    if (Curve == null)
                    {
                        DebugLog("No tolerance set");
                        passResult = (errorDistance[0] == 1.0) ? true : false;
                    }
                    else
                    {
                        DebugLog("Using tolerance provided");
                        passResult = Curve.TestValues(errorDistance, Debug);
                    }

                    return passResult;
                }
                /// <summary>
                /// returns the filtered version of an image
                /// <param name="adapter">the image to process</param>
                /// <param name="filteringLevel">the filter level</param>
                /// </summary>
                internal IImageAdapter Process(IImageAdapter adapter, int filteringLevel)
                {
                    if (adapter == null)
                    {
                        throw new ArgumentNullException ("adapter is null");
                    }
                    IImageAdapter vret = null;

                    if (filteringLevel < 0)
                    {
                        throw new ArgumentOutOfRangeException ("filteringLevel", "Value must be positive (or zero)");
                    }
                    else if (filteringLevel == 0)
                    {
                        DebugLog ("Process with no Low pass filtering");
                        vret = (IImageAdapter)adapter.Clone ();
                    }
                    else
                    {
                        DebugLog ("Process with Low pass filtering set to " + filteringLevel.ToString ());
                        WaveletTransformFilter filter = new WaveletTransformFilter();
                        filter.Level = filteringLevel;
                        vret = (IImageAdapter)filter.Process (adapter);

                    }
                    return vret;
                }
                /// <summary>
                /// returns the filtered version of an image
                /// <param name="adapter">the image to process</param>
                /// <param name="filteringLevel">the filter level</param>
                /// </summary>
                internal IImageAdapter BlockProcess (IImageAdapter adapter, int filteringLevel)
                {
                    if (adapter == null)
                    {
                        throw new ArgumentNullException ("adapter is null");
                    }

                    if (filteringLevel < 0)
                    {
                        throw new ArgumentOutOfRangeException("filteringLevel", "Value must be positive (or zero)");
                    }

                    if (filteringLevel == 0)
                    {
                        DebugLog("Process with no Low pass filtering");
                    }
                    else 
                    {
                        DebugLog("Process with Low pass filtering set to " + filteringLevel.ToString());
                    }

                    PixelizeFilter filter = new PixelizeFilter();
                    filter.SquareSize = (int)Math.Pow(2, filteringLevel);
                    filter.ExtendedSize = filter.SquareSize * 2;
                    filter.UseMean = false;
                    return filter.Process(adapter);
                }
                /// <summary>
                /// returns the energy of the error image
                /// </summary>
                private double GetEnergy ()
                {
                    if (_errImage == null)
                    {
                        throw new RenderingVerificationException ("ErrorImage is null");
                    }

                    double[] en = new double[5];

                    for (int i = 0; i < _errImage.Width; i++)
                    {
                        for (int j = 0; j < _errImage.Height; j++)
                        {
                            double len = _errImage[i, j].Alpha * _errImage[i, j].Alpha;

                            en[0] += len;
                            en[4] += len;
                            len = _errImage[i, j].Red * _errImage[i, j].Red;
                            en[1] += len;
                            en[4] += len;
                            len = _errImage[i, j].Green * _errImage[i, j].Green;
                            en[2] += len;
                            en[4] += len;
                            len = _errImage[i, j].Blue * _errImage[i, j].Blue;
                            en[3] += len;
                            en[4] += len;
                        }
                    }

                    for (int j = 0; j < en.Length; j++)
                    {
                        en[j] /= _errImage.Width * _errImage.Height;
                    }

                    DebugLog("Energy " + en[4] + "    " + en[0] + " " + en[1] + " " + en[2] + " " + en[3]);
                    return en[4];
                }
                private void ScaleColorDepth(ref IImageAdapter master, ref IImageAdapter compare, PixelFormat masterPixelFormat, PixelFormat comparePixelFormat)
                {
                    if (masterPixelFormat == comparePixelFormat) { return; }

                    // BUGBUG : Assuming only type 32ARGB and 16RGB565 exist....
                    if (Image.GetPixelFormatSize(masterPixelFormat) > Image.GetPixelFormatSize(comparePixelFormat) )
                    {
                        using (Bitmap temp = ImageUtility.ToBitmap(master))
                        {
                            using (Bitmap masterBmp = ImageUtility.ConvertPixelFormat(temp, comparePixelFormat))
                            {
                                master = new ImageAdapter(masterBmp);
                            }
                        }
                    }
                    else
                    {
                        using (Bitmap temp = ImageUtility.ToBitmap(compare))
                        {
                            using (Bitmap compareBmp = ImageUtility.ConvertPixelFormat(temp, masterPixelFormat))
                            {
                                compare = new ImageAdapter(compareBmp);
                            }
                        }
                    }
                }
                private void ScaleDownImageAdapter(ref IImageAdapter master, ref IImageAdapter compare, Point masterDpi, Point compareDpi)
                {
                    if (masterDpi == compareDpi) { return; }

                    float ratioX = masterDpi.X / (float)compareDpi.X;
                    float ratioY = masterDpi.Y / (float)compareDpi.Y;


                    Microsoft.Test.Logging.GlobalLog.LogStatus(
                        String.Format(
                            "Warning: The Master or Comparison image DPI differs. Will scale the image with the larger DPI to the same size as the smaller before the image comparison. MasterImage DPI={0}; CompareImage DPI={1}.",
                            masterDpi,
                            compareDpi)
                        );

                    if (ratioY > 1) { ratioY = 1 / ratioY; }
                    if (ratioX > 1)
                    {
                        Microsoft.Test.Logging.GlobalLog.LogStatus(String.Format("Scaling down the MasterImage to the size of the CompareImage dimensions of ({0}x{1})", compare.Width, compare.Height));
                        ratioX = 1 / ratioX;
                        ImageAdapter limadpt = new ImageAdapter(compare.Width, compare.Height);
                        for (int i = 0; i < limadpt.Width; i++)
                        {
                            for (int j = 0; j < limadpt.Height; j++)
                            {
                                limadpt[i, j] = BilinearInterpolator.ProcessPoint((float)(i / ratioX), (float)(j / ratioY), master, ColorByte.Empty);
                            }
                        }
                        limadpt.SetMetadata(master.Metadata);
                        master = limadpt;
                    }
                    else
                    {
                        Microsoft.Test.Logging.GlobalLog.LogStatus(String.Format("Scaling down the CompareImage to the size of the MasterImage dimensions of ({0}x{1})", master.Width, master.Height));
                        ImageAdapter limadpt = new ImageAdapter(master.Width, master.Height);
                        for (int i = 0; i < limadpt.Width; i++)
                        {
                            for (int j = 0; j < limadpt.Height; j++)
                            {
                                limadpt[i, j] = BilinearInterpolator.ProcessPoint((float)(i / ratioX), (float)(j / ratioY), compare, ColorByte.Empty/*null*/);
                            }
                        }
                        limadpt.SetMetadata(compare.Metadata);
                        compare = limadpt;
                    }
                }
                private SortedDictionary<byte, double> StretchToleranceGraph(CurveTolerance curveTolerance, PixelFormat masterPixelFormat, PixelFormat comparePixelFormat)
                {
                    SortedDictionary<byte, double> retVal = new SortedDictionary<byte, double>();

                    int offset = 0;
                    // BUGBUG : only 32, 24 and 16 bits supported
                    if ( (Image.GetPixelFormatSize(masterPixelFormat) <= 16 || Image.GetPixelFormatSize(comparePixelFormat) <= 16 )  &&
                        (Image.GetPixelFormatSize(masterPixelFormat) > 16 || Image.GetPixelFormatSize(comparePixelFormat) > 16) ) 
                    { 
                        offset = 6; 
                    }

                    double previous = double.NaN;
                    double localMax = double.NaN;
                    for (int t = 0; t < 255; t++)
                    {
                        localMax = 0.0;
                        for (int range = -offset; range <= offset; range++)
                        {
                            if (t + range >= 0 && t + range <= 255)
                            {
                                double val = curveTolerance.InterpolatedValue((byte)(t + range));
                                if (val > localMax) { localMax = val; }
                            }
                        }
                        if (localMax != previous)
                        {
                            previous = localMax;
                            retVal.Add((byte)t, localMax);
                        }
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Compares to images with the predefined options (filter,transform,Tolerance) 
                /// if they are set. Uses defaults otherwise
                /// </summary>
                /// <param name="master">The master image adapter master</param>
                /// <param name="compare">The image adapter to compare</param>
                /// <param name="dpiScaling">Stretch image based on dpi of both images to get the same width and height. \nNote : Interpolation may lead to slight !=, take this into account when specifying the tolerance thresholparam></param>
                /// <param name="colorDepthScaling">Adjust image (AttractorFilter) and tolerance threshold (histogram stretch) to account for difference in color Depth.</param>
                /// <param name="populateMismatchingPoints"></param>
                /// <returns>true if compare image match the master image (within tolerance and using dpi / colordepth if specified)</returns>
                public bool Compare(IImageAdapter master, IImageAdapter compare, bool dpiScaling, bool colorDepthScaling, bool populateMismatchingPoints)
                {
                    if (master == null) { throw new ArgumentNullException("master", "IImageAdapter source can not be null"); }
                    if (compare == null) { throw new ArgumentNullException("compare", "IImageAdapter target can not be null"); }

                    PixelFormat masterPixelFormat = PixelFormat.Format32bppArgb;
                    PixelFormat comparePixelFormat = PixelFormat.Format32bppArgb;
                    Point masterDpi = new Point(96, 96);
                    Point compareDpi = new Point(96, 96);

                    if (dpiScaling)
                    {
                        masterDpi = new Point((int)Math.Round(master.DpiX),(int)Math.Round(master.DpiY));
                        compareDpi = new Point((int)Math.Round(compare.DpiX), (int)Math.Round(compare.DpiY)); ;
                    }
                    //TODO: Determine the correct thing to do here in the event that metaData is null
                    if (colorDepthScaling && master.Metadata != null && compare.Metadata != null)
                    {
                        masterPixelFormat = MetadataInfoHelper.GetPixelFormat(master.Metadata);
                        comparePixelFormat = MetadataInfoHelper.GetPixelFormat(compare.Metadata);
                    }

                    return Compare(master, masterDpi, masterPixelFormat, compare, compareDpi, comparePixelFormat, populateMismatchingPoints);
                }
                /// <summary>
                /// Compares to images with the predefined options (filter,transform,Tolerance) 
                /// if they are set. Uses defaults otherwise
                /// </summary>
                /// <param name="master">The master image adapter master</param>
                /// <param name="masterDpi">The master Dpi</param>
                /// <param name="masterPixelFormat">The PixelFormat value of the master</param>
                /// <param name="compare">The image adapter to compare</param>
                /// <param name="compareDpi">The compare Dpi</param>
                /// <param name="comparePixelFormat">The PixelFormat value of the compare</param>
                /// <param name="populateMismatchingPoints"></param>
                /// <returns></returns>
                public bool Compare (IImageAdapter master, Point masterDpi, PixelFormat masterPixelFormat, IImageAdapter compare, Point compareDpi, PixelFormat comparePixelFormat, bool populateMismatchingPoints)
                {
                    if (master == null) { throw new ArgumentNullException("master", "IImageAdapter source can not be null"); }
                    if (compare == null) { throw new ArgumentNullException("compare", "IImageAdapter target can not be null"); }
                    if (masterDpi.X <= 0 || double.IsNaN(masterDpi.X) || double.IsInfinity(masterDpi.X)) { throw new ArgumentException("masterDpi", "DPI must be a defined, finite and strictly positive value"); }
                    if (compareDpi.X <= 0 || double.IsNaN(compareDpi.X) || double.IsInfinity(compareDpi.X)) { throw new ArgumentException("compareDpi", "DPI must be a defined, finite and strictly positive value"); }
                    if (masterDpi.Y <= 0 || double.IsNaN(masterDpi.Y) || double.IsInfinity(masterDpi.Y)) { throw new ArgumentException("masterDpi", "DPI must be a defined, finite and strictly positive value"); }
                    if (compareDpi.Y <= 0 || double.IsNaN(compareDpi.Y) || double.IsInfinity(compareDpi.Y)) { throw new ArgumentException("compareDpi", "DPI must be a defined, finite and strictly positive value"); }

                    if (masterDpi != compareDpi)
                    {
                        // ImageAdapter is reset
                        ScaleDownImageAdapter(ref master, ref compare, masterDpi, compareDpi);
                    }

                    if (masterPixelFormat != comparePixelFormat)
                    {
                        if (_curve == null)
                        {
                            _curve = (Curve)_unscaledCurve.Clone();
                        }
                        ScaleColorDepth(ref master, ref compare, masterPixelFormat, comparePixelFormat);
                        _curve.CurveTolerance.Entries.Clear();
                        SortedDictionary<byte, double> temp = StretchToleranceGraph(_unscaledCurve.CurveTolerance, masterPixelFormat, comparePixelFormat);
                        foreach (byte x in temp.Keys)
                        {
                            _curve.CurveTolerance.Entries[x] = temp[x];
                        }
                    }
                    return Compare(master, compare, populateMismatchingPoints);
                }
                /// <summary>
                /// Compares to images with the predefined options (filter,transform,Tolerance) 
                /// if they are set. Uses defaults otherwise
                /// </summary>
                /// <param name="source">The image adapter source</param>
                /// <param name="target">The image adapter target</param>
                /// <param name="sourceDPI">The image adapter source DPI</param>
                /// <param name="targetDPI">The image adapter target DPI</param>
                /// <param name="populateMismatchingPoints"></param>
                /// <returns></returns>
                public bool Compare (IImageAdapter source, Point sourceDPI, IImageAdapter target, Point targetDPI, bool populateMismatchingPoints)
                {
                    if (sourceDPI.X <= 0 || sourceDPI.Y <= 0) { throw new ArgumentException("sourceDPI", "DPIs for source must be strictly positive"); }
                    if (targetDPI.X <= 0 || targetDPI.Y <= 0) { throw new ArgumentException("targetDPI", "DPIs for target must be strictly positive"); }
                    if (source == null) { throw new ArgumentNullException("source", "IImageAdapter source can not be null"); }
                    if (target == null) { throw new ArgumentNullException ("target", "IImageAdapter target can not be null"); }

                    ScaleDownImageAdapter(ref source, ref target, sourceDPI, targetDPI);

                    return Compare (source, target, populateMismatchingPoints);
                }
                /// <summary>
                /// Compares to images with the predefined options (filter,transform,Tolerance) if they are set. Uses defaults otherwise.
                /// NOTE : this overload will populate the MismatchingPoint Property (Kept for backward compability)
                /// </summary>
                /// <param name="source">The image adapter source</param>
                /// <param name="target">The image adapter target</param>
                /// <returns></returns>
                public bool Compare(IImageAdapter source, IImageAdapter target)
                {
                    return Compare(source, target, true);
                }
                /// <summary>
                /// Compares to images with the predefined options (filter,transform,Tolerance) 
                /// if they are set. Uses defaults otherwise
                /// </summary>
                /// <param name="source">The image adapter source</param>
                /// <param name="target">The image adapter target</param>
                /// <param name="populateMismatchingPoints"></param>
                /// <returns></returns>
                public bool Compare (IImageAdapter source, IImageAdapter target, bool populateMismatchingPoints)
                {
                    bool passResult = false;

                    if (source == null) { throw new ArgumentNullException("source", "IImageAdapter source can not be null"); }
                    if (target == null) { throw new ArgumentNullException("target", "IImageAdapter target can not be null"); }

                    _mismatchingPoints = new MismatchingPoints();
                    CompareChannelDiff colorDifference = GetColorComparator(ChannelsInUse);
                    IImageAdapter lSource = source;
                    IImageAdapter lTarget = target;


                    //Calulate dpiRatio 
                    Point masterDpi = new Point((int)Math.Round(lSource.DpiX), (int)Math.Round(lSource.DpiY));
                    Point compareDpi = new Point((int)Math.Round(lTarget.DpiX), (int)Math.Round(lTarget.DpiY));
                    float distMaster = (float)Math.Sqrt(Math.Pow(masterDpi.X, 2) + Math.Pow(masterDpi.Y, 2));
                    float distCompare = (float)Math.Sqrt(Math.Pow(compareDpi.X, 2) + Math.Pow(compareDpi.Y, 2));
                    float dpiRatio = Math.Max(distMaster, distCompare) / Math.Min(distMaster, distCompare);
                    //Only retain precision of 2 decimal place
                    dpiRatio = (float)Math.Round(dpiRatio, 2);

                    Microsoft.Test.Logging.GlobalLog.LogStatus(
                        String.Format(
                            "Comparing [MasterImage: Width={0}, Height={1}, DPI={2}] against [ActualImage: Width={3}, Height={4}, DPI={5}]",
                            source.Width,
                            source.Height,
                            masterDpi,
                            target.Width,
                            target.Height,
                            compareDpi)
                        );
          
                    if (_curve == null)
                    {
                        _curve = (Curve)_unscaledCurve.Clone();
                    }
                    _curve.CurveTolerance.DpiRatio = dpiRatio;

                    //fallback to the "default" 1-to-1 ratio if we can't
                    //find the tolerance for the specified dpi ratio.
                    if (_curve.CurveTolerance.Entries == null || _curve.CurveTolerance.Entries.Count == 0)
                    {
                        _curve.CurveTolerance.DpiRatio = 1.0f;
                    }

                    if (TransformMatrix != null)
                    {
                        DebugLog("Transforming image using Specified Matrix");
                        Image2DTransforms transform = new Image2DTransforms(source);
                        transform.ResizeToFitOutputImage = true;
                        transform.Transform(TransformMatrix);
                        _transformedImage = transform.ImageTransformed;
                        if (BackgroundColor.IsEmpty == false)
                        {
                            for (int y = 0; y < _transformedImage.Height; y++)
                            {
                                for (int x = 0; x < _transformedImage.Width; x++)
                                {
                                    if (_transformedImage[x, y].IsEmpty)
                                    {
                                        _transformedImage[x, y] = BackgroundColor;
                                    }
                                }
                            }
                        }
                        lSource = _transformedImage;
                    }


                    if (FilterLevel > 0)
                    {
                        DebugLog("Filtering image (Low pass), level = " + FilterLevel.ToString());
                        lSource = Process (lSource, FilterLevel);
                        lTarget = Process (lTarget, FilterLevel);
                    }

                    int height = Math.Min(lSource.Height, lTarget.Height);
                    int width = Math.Min(lSource.Width, lTarget.Width);

                    _errImage = new ImageAdapter (width, height);

                    _errDiffSum = 0;
                    _errorDistance = new double[256];

                    IColor diffColor = null;
                    int diff = 0;
                    int errorCounter = 0;
                    string errorMessage = String.Empty;
                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            diff = (int)(colorDifference(lSource[i, j], lTarget[i, j], out diffColor));
                            _errDiffSum += diff;
                            //if (diff < 0) {diff = 0;} // should never occur since the diff returned is an absolute value
                            if (diff > 255 * 4) { diff = 255 * 4; }
                            _errImage[i, j] = diffColor;
                            if (populateMismatchingPoints) { _mismatchingPoints[diff / 4].Add(new Point(i, j)); }
                            _errorDistance[diff / 4]++;
                            //Print out per pixel color information
                            if (diff != 0 && verboseOutput == true && errorCounter < 10)
                            {
                                errorMessage += "Difference at pixel = [" + i + "," + j + " ]\n";
                                errorMessage += "Source color ARGB = " + lSource[i, j].ExtendedAlpha + "," + lSource[i, j].ExtendedRed + "," + lSource[i, j].ExtendedGreen + "," + lSource[i, j].ExtendedBlue + "\n";
                                errorMessage += "Target color ARGB = " + lTarget[i, j].ExtendedAlpha + "," + lTarget[i, j].ExtendedRed + "," + lTarget[i, j].ExtendedGreen + "," + lTarget[i, j].ExtendedBlue + "\n";
                                errorMessage += "Color diff   ARGB = " + diffColor.ExtendedAlpha + "," + diffColor.ExtendedRed + "," + diffColor.ExtendedGreen + "," + diffColor.ExtendedBlue + "\n";
                                errorCounter++;
                                if (errorCounter == 10)
                                    errorMessage += "Further errors have been supressed because error threshold of 10 has been reached.\n";
                            }

                        }
                    }

                    double nbpix = width * height;
                    double integrate = 0.0f;

                    DebugLog("Compute error distance sum");
                    // Normalize per channel values
                    for (int j = 0; j < _errorDistance.Length; j++)
                    {
                        integrate += _errorDistance[j];
                        _errorDistance[j] /= nbpix;
                    }

                    passResult = ComputeResult(_errorDistance);

                    //Because a user may have a tolerance set we only want to output data if the compare fails
                    if (passResult == false && !String.IsNullOrEmpty(errorMessage))
                    {
#if (!STRESS_RUNTIME)
                        GlobalLog.LogEvidence(errorMessage);
#endif
                    }

                    if (lSource.Width == lTarget.Width && lSource.Height == lTarget.Height)
                    {
                        DebugLog("Checking Integrity");
                        integrate /= nbpix;
                        if (integrate != 1.0f)
                        {
                            throw new RenderingVerificationException("error histogram does not integrate to 1.0f " + integrate);
                        }
                    }
                    else 
                    {
                        DebugLog("Width and/or Height different, defaulting comparison to false (was : '" + passResult.ToString() + "' )");
                        passResult = false;
                    }

                    _source = (IImageAdapter)lSource.Clone();
                    _target = (IImageAdapter)lTarget.Clone();

                    DebugLog("Comparison result " + passResult);

                    return passResult;
                }
                /// <summary>
                /// Return the generated Error (after Compare being called)
                /// </summary>
                /// <param name="errorDiffType">The type of Error image returned</param>
                /// <returns></returns>
                public IImageAdapter GetErrorDifference(ErrorDifferenceType errorDiffType)
                {
                    if (_errImage == null) { return null; }

                    IImageAdapter retVal = null;
                    IColor errColor = null;

                    switch (errorDiffType)
                    {
                        case ErrorDifferenceType.Regular :
                            retVal = (IImageAdapter)_errImage.Clone();
                            break;
                        case ErrorDifferenceType.IgnoreAlpha :
                            retVal = (IImageAdapter)_errImage.Clone();
                            for (int y = 0; y < retVal.Height; y++)
                            {
                                for (int x = 0; x < retVal.Width; x++)
                                {
                                    errColor = _errImage[x, y];
                                    if (errColor.ExtendedAlpha != 0 || errColor.ExtendedRed != 0 || errColor.ExtendedGreen != 0 || errColor.ExtendedBlue != 0)
                                    {
                                        retVal[x, y].A = 255;
                                    }
                                }
                            }
                            break;
                        case ErrorDifferenceType.Saturate :
                            retVal = new ImageAdapter(_errImage.Width, _errImage.Height);
                            for (int y = 0; y < retVal.Height; y++)
                            {
                                for (int x = 0; x < retVal.Width; x++)
                                {
                                    errColor = _errImage[x,y];
                                    retVal[x, y] = (IColor)SaturatedColor.Clone();
                                    if (errColor.Alpha == 0 && errColor.Red == 0 && errColor.Green == 0 && errColor.Blue == 0)
                                    {
                                        retVal[x, y].IsEmpty = true;
                                    }
                                }
                            }
                            break;
                        case ErrorDifferenceType.FilterEdge :
                            retVal = new ImageAdapter(_errImage.Width, _errImage.Height);

                            ConvolutionFilter cvf = new ConvolutionFilter();
                            cvf.Laplacian = true;
                            IImageAdapter sourceLaplacian = cvf.Process(_source);
                            double diffColor = 0.0;

                            for (int y = 0; y < retVal.Height; y++)
                            {
                                for (int x = 0; x < retVal.Width; x++)
                                {
                                    errColor = sourceLaplacian[x, y];
                                    diffColor = 0.0;
                                    if (errColor.Red + errColor.Green + errColor.Blue > 1e-6)
                                    {
                                        diffColor += _errImage[x, y].Red * RedEdgeCoefficient;
                                        diffColor += _errImage[x,y].Green * GreenEdgeCoefficient;
                                        diffColor += _errImage[x,y].Blue * BlueEdgeCoefficient;
                                        diffColor *= _errImage[x, y].NormalizedValue;
                                    }

                                    retVal[x, y] = ColorByte.GetColorFromLUT((double)diffColor / (4 * _errImage[x, y].NormalizedValue));
                                }
                            }
                            break;
                        default :
                            throw new RenderingVerificationException("Unknow enum value passed in");
                    }
                    return retVal;
                }
            #endregion Public Methods
            #region Static Methods
                /// <summary>
                /// ARGB comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceARGB (IColor p1, IColor p2, out IColor diffColor)
                {
/*
                    // HACK : Speed up for ColorByte.
                    if ( ( p1.GetType() == typeof(ColorByte) ) && ( p2.GetType() ==  typeof(ColorByte) ) )
                    { 
                        int argb1, argb2;
                        byte a1, r1, g1, b1, a2, r2, g2, b2, a, r, g, b;
                        argb1 = p1.ARGB;
                        a1 = (byte)((argb1 & 0xff000000) >> 24);
                        r1 = (byte)((argb1 & 0x00ff0000) >> 16);
                        g1 = (byte)((argb1 & 0x0000ff00) >> 8);
                        b1 = (byte)(argb1 & 0x000000ff);
                        argb2 = p2.ARGB;
                        a2 = (byte)((argb1 & 0xff000000) >> 24);
                        r2 = (byte)((argb1 & 0x00ff0000) >> 16);
                        g2 = (byte)((argb1 & 0x0000ff00) >> 8);
                        b2 = (byte)(argb1 & 0x000000ff);

                        a = (byte)((a1 > a2) ? (a1 - a2) : (a2 - a1));
                        r = (byte)((r1 > r2) ? (r1 - r2) : (r2 - r1));
                        g = (byte)((g1 > g2) ? (g1 - g2) : (g2 - g1));
                        b = (byte)((b1 > b2) ? (b1 - b2) : (b2 - b1));
                        diffColor = new ColorByte(a, r, g, b);
                        return a + r + g + b;
                    }
*/
                    double retVal = double.NaN;
                    diffColor = new ColorDouble();
                    diffColor.ExtendedAlpha = p1.ExtendedAlpha - p2.ExtendedAlpha > 0 ? (p1.ExtendedAlpha - p2.ExtendedAlpha) : (p2.ExtendedAlpha - p1.ExtendedAlpha);
                    diffColor.ExtendedRed = p1.ExtendedRed - p2.ExtendedRed > 0 ? (p1.ExtendedRed - p2.ExtendedRed) : (p2.ExtendedRed - p1.ExtendedRed);
                    diffColor.ExtendedGreen = p1.ExtendedGreen - p2.ExtendedGreen > 0 ? (p1.ExtendedGreen - p2.ExtendedGreen) : (p2.ExtendedGreen - p1.ExtendedGreen);
                    diffColor.ExtendedBlue = p1.ExtendedBlue - p2.ExtendedBlue > 0 ? (p1.ExtendedBlue - p2.ExtendedBlue) : (p2.ExtendedBlue - p1.ExtendedBlue);
                    retVal = diffColor.ExtendedAlpha + diffColor.ExtendedRed + diffColor.ExtendedGreen + diffColor.ExtendedBlue;
                    if (retVal == 0) { diffColor.IsEmpty = true; }
                    return retVal * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// RGB comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceRGB (IColor p1, IColor p2, out IColor diffColor)
                {
                    double red = p1.ExtendedRed - p2.ExtendedRed > 0 ? (p1.ExtendedRed - p2.ExtendedRed) : (p2.ExtendedRed - p1.ExtendedRed);
                    double green = p1.ExtendedGreen - p2.ExtendedGreen > 0 ? (p1.ExtendedGreen - p2.ExtendedGreen) : (p2.ExtendedGreen - p1.ExtendedGreen);
                    double blue = p1.ExtendedBlue - p2.ExtendedBlue > 0 ? (p1.ExtendedBlue - p2.ExtendedBlue) : (p2.ExtendedBlue - p1.ExtendedBlue);
                    diffColor = new ColorDouble(0, red, green, blue);
                    return (red + green + blue) * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// A comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceA (IColor p1, IColor p2, out IColor diffColor)
                {
                    double alpha = p1.ExtendedAlpha - p2.ExtendedAlpha > 0 ? (p1.ExtendedAlpha - p2.ExtendedAlpha) : (p2.ExtendedAlpha - p1.ExtendedAlpha);
                    diffColor = new ColorDouble(alpha, 0, 0, 0);
                    return alpha * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// R comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceR (IColor p1, IColor p2, out IColor diffColor)
                {
                    double red = p1.ExtendedRed - p2.ExtendedRed > 0 ? (p1.ExtendedRed - p2.ExtendedRed) : (p2.ExtendedRed - p1.ExtendedRed);
                    diffColor = new ColorDouble(0, red, 0, 0);
                    return red * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// G comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceG (IColor p1, IColor p2, out IColor diffColor)
                {
                    double green = p1.ExtendedGreen - p2.ExtendedGreen > 0 ? (p1.ExtendedGreen - p2.ExtendedGreen) : (p2.ExtendedGreen - p1.ExtendedGreen);
                    diffColor = new ColorDouble(0, 0, green, 0);
                    return green * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// B comparison of two double precision colors
                /// <param name="p1">The first color</param>
                /// <param name="p2">The second color</param>
                /// <param name="diffColor">[out] The color diff between the two imageAdapter</param>
                /// </summary>
                public static double ColorDifferenceB (IColor p1, IColor p2, out IColor diffColor)
                {
                    double blue = p1.ExtendedBlue - p2.ExtendedBlue > 0 ? (p1.ExtendedBlue - p2.ExtendedBlue) : (p2.ExtendedBlue - p1.ExtendedBlue);
                    diffColor = new ColorDouble(0, 0, 0, blue);
                    return blue * ColorDouble._normalizedValue;
                }
                /// <summary>
                /// return the comparison method for two double precision colors
                /// <param name="mode">The mode of comparison</param>
                /// </summary>
                internal static CompareChannelDiff GetColorComparator (ChannelCompareMode mode)
                {
                    CompareChannelDiff comp = null;
                    switch (mode)
                    {
                        case ChannelCompareMode.ARGB:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceARGB);
                                break;
                            }
                        case ChannelCompareMode.RGB:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceRGB);
                                break;
                            }
                        case ChannelCompareMode.A:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceA);
                                break;
                            }
                        case ChannelCompareMode.R:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceR);
                                break;
                            }
                        case ChannelCompareMode.G:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceG);
                                break;
                            }
                        case ChannelCompareMode.B:
                            {
                                comp = new CompareChannelDiff (ColorDifferenceB);
                                break;
                            }
                        default:
                            {
                                throw new RenderingVerificationException("compChannelMode unknown " + mode);
                            }
                    }
                    return comp;
                }
            #endregion Static Methods
        #endregion Methods

        #region IImageComparatorUnmanaged extra stuff
            /// <summary>
            /// Compares the image histogram with the given Tolerance. 
            /// Returns true if the histogram fits the Tolerance.
            /// <param name="histogram">[in] image histogram (the error in % by energy level 0-255)</param>
            /// <param name="toleranceFileName">[out] the xml file name which contains Tolerance</param>
            /// </summary>
            bool IImageComparatorUnmanaged.CompTolerance(double[] histogram, string toleranceFileName)
            {
                System.Diagnostics.Debug.WriteLine ("CompTolerance(): started. histogram.Length=" + histogram.Length.ToString());
                // Build curve for tolerance (Tolerance)
                Curve curve = null;
                System.Diagnostics.Debug.WriteLine ("CompTolerance(): after Curve curve = new Curve();");
                if (File.Exists(toleranceFileName))
                {
                    System.Diagnostics.Debug.WriteLine ("CompTolerance(): The file " + toleranceFileName + " exists");
                    curve = new Curve();
                    curve.CurveTolerance.LoadTolerance(toleranceFileName);
                    System.Diagnostics.Debug.WriteLine("CompTolerance(): LoadTolerance completed");
                }
                System.Diagnostics.Debug.WriteLine ("CompTolerance(): addData completed");
                // check if image are OK (within tolerance if Tolerance defined / strictly identical otherwise)
                return curve.TestValues(histogram, false);
            }
            /// <summary>
            /// create XML file with histogram values
            /// </summary>
            /// <param name="fileName">[out] .xml file name which contains a histogram</param>
            /// <param name="histogram">[in] histogram (the error in % by energy level 0-255)</param>
            void IImageComparatorUnmanaged.SaveHistogram(string fileName, double[] histogram)
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlNode rootNode = (XmlNode)xmlDoc.CreateElement("Root");
                for (int i = 0; i < histogram.Length; i++)
                {
                    XmlNode childNode = (XmlNode)xmlDoc.CreateElement("Value_" + i);
                    childNode.InnerText = histogram[i].ToString();
                    rootNode.AppendChild(childNode);
                }
                xmlDoc.AppendChild(rootNode);
                xmlDoc.Save(fileName);
            }
            /// <summary>
            /// Compares two images in ARGB mode and 
            /// returns the result of the comparison (true if images are the same). 
            /// <param name="imageMaster">[in] master image</param>
            /// <param name="imageRendered">[in] rendered image</param>
            /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
            /// <param name="normializedErr">[out] the overall error / # of pixels</param>
            /// <param name="histoFileName">[out] .xml file name which contains a histogram (the error in % by energy level 0-255). null if not used</param>
            /// <param name="diffImgFileName">[out] .bmp file name which contains bitmap of the differences. null if not used</param>
            /// </summary>
            bool IImageComparatorUnmanaged.CompStrict(
                IImageAdapterUnmanaged imageMaster,
                IImageAdapterUnmanaged imageRendered,
                string vscanFileName, 
                ref float normializedErr,
                string histoFileName, 
                string diffImgFileName)
            {
                bool comparisonResult = false;
                this.Compare((IImageAdapter)imageMaster, (IImageAdapter)imageRendered, false);

                double[] histogram = this.ErrorDistance;
                float errSum = this.ErrorDiffSum;
                System.Diagnostics.Debug.WriteLine ("Compare(): errSum=" + errSum.ToString() + " histogram.Length=" + histogram.Length.ToString());
                IImageAdapter imageDiff = this.GetErrorDifference(ErrorDifferenceType.FilterEdge);

                if (errSum == 0f) // images are the same
                {
                    comparisonResult = true;
                    normializedErr = 0f;
                }
                else
                {
                    if (diffImgFileName != null && diffImgFileName != string.Empty)
                    {
                        ImageUtility.ToImageFile(imageDiff, diffImgFileName, ImageFormat.Png);
                    }
                    if (histoFileName != null && histoFileName != string.Empty)
                    {
                        ((IImageComparatorUnmanaged)this).SaveHistogram (histoFileName, histogram);
                    }
                    normializedErr = errSum / (imageDiff.Height * imageDiff.Width);
                }
                return comparisonResult;
            }
            /// <summary>
            /// Compares two images in ARGB mode and 
            /// returns the result of the comparison (true if images are the same). 
            /// <param name="master">[in] master image</param>
            /// <param name="rendered">[in] rendered image</param>
            /// <param name="toleranceFileName">[in] the xml file name which contains Tolerance. null if not used</param>
            /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
            /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// </summary>
            bool IImageComparatorUnmanaged.CompLenient (IImageAdapterUnmanaged master,
                                                        IImageAdapterUnmanaged rendered,
                                                        string toleranceFileName, 
                                                        string vscanFileName,            
                                                        int rcToCompareLeft, 
                                                        int rcToCompareTop, 
                                                        int rcToCompareRight, 
                                                        int rcToCompareBottom)

            {
                System.Diagnostics.Debug.WriteLine ("CompLenient() started");

                if (master == null || rendered == null) { throw new ArgumentNullException("CompLenient(): master or render are NULL"); }

                IImageAdapter imageMaster = (IImageAdapter)master;
                IImageAdapter imageRendered = (IImageAdapter)rendered;

                if(rcToCompareLeft != 0 || rcToCompareTop != 0 || rcToCompareRight != 0 || rcToCompareBottom != 0)
                {
                    Rectangle rcToCompare = new Rectangle (rcToCompareLeft, rcToCompareTop, rcToCompareRight - rcToCompareLeft, rcToCompareBottom - rcToCompareTop);
                    imageMaster = ImageUtility.ClipImageAdapter(imageMaster, rcToCompare);
                    imageRendered = ImageUtility.ClipImageAdapter(imageRendered, rcToCompare);
#if DEBUG
                    ImageUtility.ToImageFile (imageMaster, System.IO.Path.Combine (System.IO.Path.GetTempPath (), "master_Clipped.png"));
                    ImageUtility.ToImageFile (imageRendered, System.IO.Path.Combine (System.IO.Path.GetTempPath (), "rendered_Clipped.png"));
#endif // debug
                }

                if(toleranceFileName != null && toleranceFileName.Trim() != string.Empty && File.Exists(toleranceFileName))
                {
                    _unscaledCurve.CurveTolerance.LoadTolerance(toleranceFileName);
                }
                bool comparisonResult = this.Compare(imageMaster, imageRendered, false);
                if (comparisonResult == false && vscanFileName != null && vscanFileName.Trim() != string.Empty)
                {
                    Package package = Package.Create (vscanFileName, true);

                    package.MasterBitmap = ImageUtility.ToBitmap(imageMaster);
                    package.CapturedBitmap = ImageUtility.ToBitmap(imageRendered);
                    package.ChannelCompare = ChannelCompareMode.ARGB;
                    package.PackageCompare = PackageCompareTypes.ImageCompare;
                    if (toleranceFileName != null && toleranceFileName.Trim() != string.Empty && File.Exists(toleranceFileName) && this.Curve.CurveTolerance.Entries.Count != 0)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(toleranceFileName);
                        package.Tolerance = xmlDoc.DocumentElement;
                    }
                    package.Save ();
                }

                // Clean up
                System.Diagnostics.Debug.WriteLine ("CompLenient():  returned " + comparisonResult.ToString ());
                return comparisonResult;
            }

            /// <summary>
            /// Compares two images in ARGB mode with Tolerance and 
            /// returns the result of the comparison (true if images are the same). 
            /// Saves .vscan package in case of failure
            /// <param name="master">[in] master image</param>
            /// <param name="rendered">[in] rendered image</param>
            /// <param name="toleranceFileName">[in] the xml file name which contains Tolerance. null if not used</param>
            /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
            /// <param name="normializedErr">[out] the overall error / # of pixels</param>
            /// <param name="histoFileName">[out] .xml file name which contains a histogram (the error in % by energy level 0-255). null if not used</param>
            /// <param name="diffImgFileName">[out] .bmp file name which contains bitmap of the differences. null if not used</param>
            /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
            /// </summary>
            bool IImageComparatorUnmanaged.CompStrictOrToleranceSavePackage(
                IImageAdapterUnmanaged master, 
                IImageAdapterUnmanaged rendered, 
                string toleranceFileName, 
                string vscanFileName, 
                ref float normializedErr,
                string histoFileName, 
                string diffImgFileName,
                int rcToCompareLeft, 
                int rcToCompareTop, 
                int rcToCompareRight, 
                int rcToCompareBottom)
            {
                System.Diagnostics.Debug.WriteLine ("\n\nCompStrictOrToleranceSavePackage():  started");

                if (master == null || rendered == null) { throw new ArgumentNullException ("CompLenient(): master or render are NULL"); }

                ImageAdapter imageMaster = (ImageAdapter)master;
                ImageAdapter imageRendered = (ImageAdapter)rendered;

                if (rcToCompareLeft != 0 || rcToCompareTop != 0 || rcToCompareRight != 0 || rcToCompareBottom != 0)
                {
                    Rectangle rcToCompare = new Rectangle (rcToCompareLeft, rcToCompareTop, rcToCompareRight-rcToCompareLeft, rcToCompareBottom-rcToCompareTop);
                    master = (IImageAdapterUnmanaged)ImageUtility.ClipImageAdapter(imageMaster, rcToCompare);
                    rendered = (IImageAdapterUnmanaged)ImageUtility.ClipImageAdapter(imageRendered, rcToCompare);
                }

                bool comparisonResult = ((IImageComparatorUnmanaged)this).CompStrict(master,
                                                                                    rendered, 
                                                                                    vscanFileName, 
                                                                                    ref normializedErr,
                                                                                    histoFileName, 
                                                                                    diffImgFileName);

                System.Diagnostics.Debug.WriteLine("CompStrictOrToleranceSavePackage():  CompStrict() returned " + comparisonResult.ToString());

                if (toleranceFileName != null && toleranceFileName != string.Empty)
                {
                    double[] histogram = this.ErrorDistance;
                    comparisonResult = ((IImageComparatorUnmanaged)this).CompTolerance(histogram, toleranceFileName);
                }

                if (comparisonResult == false  && vscanFileName != null && vscanFileName != string.Empty)
                {
                    Package package = Package.Create(vscanFileName, true);
                    package.MasterBitmap = ImageUtility.ToBitmap(imageMaster);
                    package.CapturedBitmap = ImageUtility.ToBitmap(imageRendered);
                    package.ChannelCompare = ChannelCompareMode.ARGB;
                    package.PackageCompare = PackageCompareTypes.ImageCompare;
                    //HACK: BUG: This is not yet implemented and ExtraFiles throws an exception
                    //package.ExtraFiles.Add(histoFileName);
                    //package.ExtraFiles.Add(diffImgFileName);
                    if (toleranceFileName != null && toleranceFileName.Trim() != string.Empty && File.Exists(toleranceFileName) && this.Curve.CurveTolerance.Entries.Count != 0)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(toleranceFileName);
                        package.Tolerance = xmlDoc.DocumentElement;
                    }
                    package.Save();
                }

                return comparisonResult;
            }
        #endregion IImageComparatorUnmanaged extra stuff
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
