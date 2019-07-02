// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using 
        using System;
    #endregion using

    /// <summary>
    /// Perform a bilinear interpolation of an image
    /// </summary>
    internal class BilinearInterpolator
    {
        #region Constants
            private const int SZAR = 1024;
            private const int SZAR_SQR = SZAR * SZAR;
        #endregion Constants

        #region Properties
            #region Static Properties (private)
                private static double [] _bilinearArray = null;
            #endregion Static Properties
        #endregion Properties

        #region Constructor
            private BilinearInterpolator() {}   // block instantiation
            static BilinearInterpolator()
            {
                _bilinearArray = CreateBilinearArray();
            }
        #endregion Constructor
 
        #region Methods
            #region Private Methods
                private static double[] CreateBilinearArray()
                {
                    double[] retVal = new double[SZAR];
                    for (int index = 0; index < SZAR; index++)
                    {
                        retVal[index] = 1.0 - (double)index / SZAR;
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Perform a Bilinear interpolation
                /// </summary>
                /// <param name="x">position on the x Axis</param>
                /// <param name="y">position on the y Axis</param>
                /// <param name="imageSource">The image to perform interpolation on</param>
                /// <param name="unassignedColor">The color to use for pixel that are unassigned\nCan be set to null (Empty color will be used)</param>
                /// <returns>The interpolated color</returns>
                public static IColor ProcessPoint(double x, double y, IImageAdapter imageSource, IColor unassignedColor)
                {
                    IColor retVal = null;
                    if (imageSource == null)
                    {
                        throw new ArgumentNullException("imageSource", "The IImageAdapter passed in cannot be null");
                    }

                    if (x >= 0.0 && y >= 0.0 && x <= imageSource.Width - 1 && y <= imageSource.Height - 1)
                    {
                        int ix = (int)x;
                        int iy = (int)y;
                        int incx = (ix >= imageSource.Width - 1) ? 0 : 1;
                        int incy = (iy >= imageSource.Height - 1) ? 0 : 1;

                        retVal = (IColor)imageSource[ix, iy].Clone();
                        retVal.IsEmpty = true;

                        IColor c00 = imageSource[ix, iy];
                        IColor c01 = imageSource[ix, iy + incy];
                        IColor c10 = imageSource[ix + incx, iy];
                        IColor c11 = imageSource[ix + incx, iy + incy];

                        if (c00.IsEmpty) { return (IColor)unassignedColor.Clone(); }


                        if (c01.IsEmpty || c10.IsEmpty || c11.IsEmpty)
                        {
                            // do this only if one of the color is "Empty" because this is time consumming (lots of computation)
                            ColorByte tempColor = new ColorByte();
                            int indexC00 = (int)(Math.Sqrt(((x - ix) * (x - ix) + (y - iy) * (y - iy)) * SZAR_SQR) + 0.5);
                            int indexC01 = (int)(Math.Sqrt(((x - ix) * (x - ix) + (y - (iy + incy)) * (y - (iy + incy))) * SZAR_SQR) + 0.5);
                            int indexC10 = (int)(Math.Sqrt(((x - (ix + incx)) * (x - (ix + incx)) + (y - iy) * (y - iy)) * SZAR_SQR) + 0.5);
                            int indexC11 = (int)(Math.Sqrt(((x -(ix + incx)) * (x -(ix + incx)) + (y - (iy + incy)) * (y - (iy + incy))) * SZAR_SQR) + 0.5);
                            double weigth = 0;
                            if (c00.IsEmpty == false && indexC00 < 1024)
                            {
                                weigth = _bilinearArray[indexC00];
                                tempColor.ExtendedAlpha = c00.ExtendedAlpha * _bilinearArray[indexC00];
                                tempColor.ExtendedRed = c00.ExtendedRed * _bilinearArray[indexC00];
                                tempColor.ExtendedGreen = c00.ExtendedGreen * _bilinearArray[indexC00];
                                tempColor.ExtendedBlue = c00.ExtendedBlue * _bilinearArray[indexC00];
                            }
                            if (c01.IsEmpty == false && indexC01 < 1024)
                            {
                                weigth = _bilinearArray[indexC01];
                                tempColor.ExtendedAlpha += c01.ExtendedAlpha * _bilinearArray[indexC01];
                                tempColor.ExtendedRed += c01.ExtendedRed * _bilinearArray[indexC01];
                                tempColor.ExtendedGreen += c01.ExtendedGreen * _bilinearArray[indexC01];
                                tempColor.ExtendedBlue += c01.ExtendedBlue * _bilinearArray[indexC01];
                            }
                            if (c10.IsEmpty == false && indexC10 < 1024)
                            {
                                weigth = _bilinearArray[indexC10];
                                tempColor.ExtendedAlpha += c10.ExtendedAlpha * _bilinearArray[indexC10];
                                tempColor.ExtendedRed += c10.ExtendedRed * _bilinearArray[indexC10];
                                tempColor.ExtendedGreen += c10.ExtendedGreen * _bilinearArray[indexC10];
                                tempColor.ExtendedBlue += c10.ExtendedBlue * _bilinearArray[indexC10];
                            }
                            if (c11.IsEmpty == false && indexC11 < 1024)
                            {
                                weigth = _bilinearArray[indexC11];
                                tempColor.ExtendedAlpha += c11.ExtendedAlpha * _bilinearArray[indexC11];
                                tempColor.ExtendedRed += c11.ExtendedRed * _bilinearArray[indexC11];
                                tempColor.ExtendedGreen += c11.ExtendedGreen * _bilinearArray[indexC11];
                                tempColor.ExtendedBlue += c11.ExtendedBlue * _bilinearArray[indexC11];
                            }

                            if (tempColor.IsEmpty == false)
                            {
                                tempColor.ExtendedAlpha /= weigth;
                                tempColor.ExtendedRed /= weigth;
                                tempColor.ExtendedGreen /= weigth;
                                tempColor.ExtendedBlue /= weigth;
                                retVal = tempColor;
                            }

                        }
                        else
                        {
                            int indexX = (int)((x - ix) * SZAR);
                            int indexY = (int)((y - iy) * SZAR);

                            double v0 = c00.ExtendedAlpha * _bilinearArray[indexX] + c10.ExtendedAlpha * (1.0 - _bilinearArray[indexX]);
                            double v1 = c01.ExtendedAlpha * _bilinearArray[indexX] + c11.ExtendedAlpha * (1.0 - _bilinearArray[indexX]);
                            retVal.ExtendedAlpha = (v0 * _bilinearArray[indexY] + v1 * (1.0 - _bilinearArray[indexY]));

                            v0 = c00.ExtendedRed * _bilinearArray[indexX] + c10.ExtendedRed * (1.0 - _bilinearArray[indexX]);
                            v1 = c01.ExtendedRed * _bilinearArray[indexX] + c11.ExtendedRed * (1.0 - _bilinearArray[indexX]);
                            retVal.ExtendedRed = (v0 * _bilinearArray[indexY] + v1 * (1.0 - _bilinearArray[indexY]));

                            v0 = c00.ExtendedGreen * _bilinearArray[indexX] + c10.ExtendedGreen * (1.0 - _bilinearArray[indexX]);
                            v1 = c01.ExtendedGreen * _bilinearArray[indexX] + c11.ExtendedGreen * (1.0 - _bilinearArray[indexX]);
                            retVal.ExtendedGreen = (v0 * _bilinearArray[indexY] + v1 * (1.0 - _bilinearArray[indexY]));

                            v0 = c00.ExtendedBlue * _bilinearArray[indexX] + c10.ExtendedBlue * (1.0 - _bilinearArray[indexX]);
                            v1 = c01.ExtendedBlue * _bilinearArray[indexX] + c11.ExtendedBlue * (1.0 - _bilinearArray[indexX]);
                            retVal.ExtendedBlue = (v0 * _bilinearArray[indexY] + v1 * (1.0 - _bilinearArray[indexY]));
                        }
                    }
                    else
                    {
                        // Out of bound of the original image, fill with IColor.Empty or the unassignedColor passed in
                        if (unassignedColor == null)
                        {
                            retVal = (IColor)imageSource[0, 0].Clone();
                            retVal.IsEmpty = true;
                        }
                        else
                        {
                            retVal = (IColor)unassignedColor.Clone();
                        }
                    }

                    return retVal;
                }
                public static IImageAdapter ScaleDown(double horizontalScaling, double verticalScaling,  IImageAdapter imageSource)
                {
                    if (horizontalScaling <= 0.0 || verticalScaling <= 0.0) { throw new ArgumentOutOfRangeException("horizontalScaling and verticalScaling must be strictly positive"); }

                    IImageAdapter retVal = (IImageAdapter)imageSource.Clone();
                    double deltaX = Math.Max(1.0, 1 / horizontalScaling);
                    double deltaY = Math.Max(1.0, 1 / verticalScaling);

                    if (deltaX != 1.0)
                    {
                        // Pass 1 : average on the x axis
                        for (int y = 0; y < retVal.Height; y++)
                        {
                            for (int x = 0; x < retVal.Width; x += (int)deltaX)
                            {
                                int weight = 0;
                                ColorDouble color = new ColorDouble();
                                for (int t = 0; t < (int)deltaX && x+t <retVal.Width; t++)
                                {
                                    weight++;
                                    color.ExtendedAlpha += imageSource[x + t, y].ExtendedAlpha;
                                    color.ExtendedRed += imageSource[x + t, y].ExtendedRed;
                                    color.ExtendedGreen += imageSource[x + t, y].ExtendedGreen;
                                    color.ExtendedBlue += imageSource[x + t, y].ExtendedBlue;
                                }
                                color.ExtendedAlpha /= weight;
                                color.ExtendedRed /= weight;
                                color.ExtendedGreen /= weight;
                                color.ExtendedBlue /= weight;
                                for (int t = 0; t < (int)deltaX && x+t < retVal.Width; t++)
                                {
                                    retVal[x + t, y].ExtendedAlpha = color.ExtendedAlpha;
                                    retVal[x + t, y].ExtendedRed = color.ExtendedRed;
                                    retVal[x + t, y].ExtendedGreen = color.ExtendedGreen;
                                    retVal[x + t, y].ExtendedBlue = color.ExtendedBlue;
                                }
                            }
                        }
                    }

                    if (deltaY != 1.0)
                    {
                        // Pass 2 : average on the y axis
                        for (int y = 0; y < (int)deltaY; y += (int)deltaY)
                        {
                            for (int x = 0; x < (int)deltaX; x += (int)deltaX)
                            {
                                int weight = 0;
                                ColorDouble color = new ColorDouble();
                                for (int t = 0; t < (int)deltaY && y+t < retVal.Height; t++)
                                {
                                    weight++;
                                    color.ExtendedAlpha += imageSource[x, y + t].ExtendedAlpha;
                                    color.ExtendedRed += imageSource[x, y + t].ExtendedRed;
                                    color.ExtendedGreen += imageSource[x, y + t].ExtendedGreen;
                                    color.ExtendedBlue += imageSource[x, y + t].ExtendedBlue;
                                }
                                color.ExtendedAlpha /= weight;
                                color.ExtendedRed /= weight;
                                color.ExtendedGreen /= weight;
                                color.ExtendedBlue /= weight;
                                for (int t = 0; t < (int)deltaY && y+t < retVal.Height; t++)
                                {
                                    retVal[x, y + t].ExtendedAlpha = color.ExtendedAlpha;
                                    retVal[x, y + t].ExtendedRed = color.ExtendedRed;
                                    retVal[x, y + t].ExtendedGreen = color.ExtendedGreen;
                                    retVal[x, y + t].ExtendedBlue = color.ExtendedBlue;
                                }
                            }
                        }
                    }

                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
            }

    /// <summary>
    /// Perform a bicubic interpolation of an image
    /// </summary>
    internal class BicubicInterpolator
    {
        #region Constants
            private const int SZAR = 1024;
        #endregion Constants

        #region Properties
            #region Static Properties (private)
                private static double [] _bicubicArray = null;
                private static double [] _srqt = null;
            #endregion Static Properties
        #endregion Properties

        #region Constructor
            private BicubicInterpolator() {} // Block instantiation
            static BicubicInterpolator()
            {
                _bicubicArray = CreateBicubicArray();
            }
        #endregion Constructor
 
        #region Methods
            #region Private Methods
                private static double[] CreateBicubicArray()
                {
                    double[] retVal = new double[2 * SZAR];

                    for (int j = 0; j < SZAR; j++)
                    {
                        double arg = (double)j / SZAR;
                        retVal[j] = 1 - 2.0 * arg * arg + arg * arg * arg;
                        arg += 1.0;
                        retVal[SZAR + j] = 4.0 - 8.0 * arg + 5.0 * arg * arg - arg * arg * arg;
                    }

                    _srqt = new double[SZAR];
                    for (int j = 0; j < SZAR; j++)
                    {
                        double arg = 4.0 * (double)j / SZAR;
                        _srqt[j] = Math.Sqrt(arg);
                    }

                    for (int j = 0; j < retVal.Length; j++)
                    {
                        retVal[j] = Math.Exp(-10.0 * ((j * j) / (retVal.Length * retVal.Length)));
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Perform a Bicubic interpolation
                /// </summary>
                /// <param name="x">position on the x Axis</param>
                /// <param name="y">position on the y Axis</param>
                /// <param name="imageSource">The image to perform interpolation to</param>
                /// <param name="unassignedColor">The color to use for pixel that are unassigned\nCan be set to null (Empty color will be used)</param>
                /// <returns>The interpolated color</returns>
                public static IColor ProcessPoint(double x, double y, IImageAdapter imageSource, IColor unassignedColor)
                {
                    IColor retVal = null;
                    retVal.RGB = 0; // Initialize everything but the Alpha channel

                    if (imageSource == null)
                    {
                        throw new ArgumentNullException("imageSource", "The image passed in cannot be null");
                    }
                    if (x >= 0 && x <= imageSource.Width - 1 && y >= 0 && y <= imageSource.Height - 1)
                    {
                        int xs = (int)x;
                        int ys = (int)y;
                        retVal = (IColor)imageSource[xs, ys].Clone();
                        retVal.IsEmpty = true;

                        double wght = 0;
                        IColor neighborColor = null;
                        for (int j = ys - 1; j < ys + 3; j++)
                        {
                            for (int i = xs - 1; i < xs + 3; i++)
                            {
                                if (j >= 0 && j < imageSource.Height && i >= 0 && i < imageSource.Width)
                                {
                                    neighborColor = imageSource[i, j];
                                    double ldx = x - i;
                                    double ldy = y - j;
                                    double distance = Math.Sqrt(ldx * ldx + ldy * ldy);

                                    int arg = (int)(distance * SZAR);
                                    if (arg < _bicubicArray.Length)
                                    {
                                        double coef = _bicubicArray[arg];
                                        retVal.ExtendedRed += coef * neighborColor.ExtendedRed;
                                        retVal.ExtendedGreen += coef * neighborColor.ExtendedGreen;
                                        retVal.ExtendedBlue += coef * neighborColor.ExtendedBlue;
                                        wght += coef;
                                    }
                                }
                            }
                        }

                        retVal.ExtendedRed /= wght;
                        retVal.ExtendedGreen /= wght;
                        retVal.ExtendedBlue /= wght;
                    }
                    else 
                    {
                        // Out of bound of the original image, fill with IColor.Empty or the unassignedColor passed in
                        if (unassignedColor == null)
                        {
                            retVal = (IColor)imageSource[0, 0].Clone();
                            retVal.IsEmpty = true;
                        }
                        else
                        {
                            retVal = unassignedColor;
                        }
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}

