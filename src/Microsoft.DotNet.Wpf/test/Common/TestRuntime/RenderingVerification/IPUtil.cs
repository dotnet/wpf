// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Drawing;
    using System.Collections;

    /// <summary>
    /// color manipulation helper class
    /// </summary>
    internal class IPUtil
    {
        /// <summary>
        /// Convert RGB to Int (luminance ?)
        /// </summary>
        /// <param name="col">the color to be converted</param>
        /// <returns>the converted color as int</returns>
        internal static int RGB2Yint(Color col)
        {
            int v=(col.R*19595+col.G*38469+col.B*7471)>>16;
            if(v>255){v=255;}
            return v;
        }
        
        /// <summary>
        /// Convert RGB to a Float (luminance ?) [0-1]
        /// </summary>
        /// <param name="col">the color to be converted</param>
        /// <returns>the converted color as float</returns>
        internal static float RGB2Yfloat(Color col)
        {
            float v=(float)(col.R*0.299f+col.G*0.587f+col.B*0.114f);
            return v;
        }
        /// <summary>
        /// Convert R, G and B channel represneted as float [0-1] as a float luminance
        /// </summary>
        /// <param name="R">The red channel</param>
        /// <param name="G">The green channel</param>
        /// <param name="B">The blue channel</param>
        /// <returns>a float representing the luminance</returns>
        internal static float RGBfloat2Yfloat(float R, float G, float B)
        {
            float v=(R*0.299f+G*0.587f+0.114f);
            return v;
        }

        /// <summary>
        /// Convert an color from the RGB color model (Red Green Blue)to the CMY model (Cyan Magenta Yellow)
        /// </summary>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="M"></param>
        /// <param name="Y"></param>
        internal static void RGB2CMY(double R, double G, double B, out double C, out double M, out double Y)
        {
            C = 1.0 - R;
            M = 1.0 - G;
            Y = 1.0 - B;
        }

        /// <summary>
        /// Convert an color from the CMY model (Cyan Magenta Yellow) to the RGB color model (Red Green Blue)
        /// </summary>
        /// <param name="C">The Cyan channel</param>
        /// <param name="M">The Magenta channel</param>
        /// <param name="Y">The Yellow channel</param>
        /// <param name="R">The Red channel</param>
        /// <param name="G">The Green channel</param>
        /// <param name="B">The Blue channel</param>
        internal static void CMY2RGB2(double C, double M, double Y, out double R, out double G, out double B)
        {
            R = 1.0 - C;
            G = 1.0 - M;
            B = 1.0 - Y;
        }

        /// <summary>
        /// Convert an color from the RGB color model (Red Green Blue)to the HSI model (Hue Saturation Intensity)
        /// </summary>
        /// <param name="R">The Red channel</param>
        /// <param name="G">The Green channel</param>
        /// <param name="B">The Blue channel</param>
        /// <param name="H">The Hue channel</param>
        /// <param name="S">The Saturation channel</param>
        /// <param name="I">The Intensity channel</param>
        internal static void RGB2HSI(double R, double G, double B, out double H, out double S, out double I)
        { 
            double minRGB = Math.Min(Math.Min(R, G), B);
            double theta = Math.Acos( ( 2 * R - (G + B) ) / ( 2 * Math.Sqrt( Math.Pow(R - G, 2) + (R - G) * (G - B) ) ) );
            H = (B > G) ? (360 - theta) : theta;
            S = 1 - 3 * minRGB / (R + G + B);
            I = (R + G + B) / 3;
        }

        /// <summary>
        /// Convert an color from the HSI color model (Hue Saturation Intensity)to the RGB model (Red Green Blue)
        /// </summary>
        /// <param name="H">The Hue channel</param>
        /// <param name="S">The Saturation channel</param>
        /// <param name="I">The Intensity channel</param>
        /// <param name="R">The Red channel</param>
        /// <param name="G">The Green channel</param>
        /// <param name="B">The Blue channel</param>
        internal static void HSI2RGB(double H, double S, double I, out double R, out double G, out double B)
        {
            // Check args
            if (S < 0.0 || S > 1.0)
            { 
                throw new ArgumentOutOfRangeException();
            }
            if (I < 0.0 || I > 1.0)
            { 
                throw new ArgumentOutOfRangeException();
            }
            if (H < 0 || H > 360)
            { 
                throw new ArgumentOutOfRangeException();
            }

            if (H >= 240)
            {
                H = H - 240;
                G = I * (1 - S);
                B = I * (1 + S * Math.Cos(H) / Math.Cos(60 - H));
                R = 3 * I + (G + B);
                return;
            }
            if(H >=120)
            {
                H = H - 120;
                R = I * (1 - S);
                G = I * (1 + S * Math.Cos(H) / Math.Cos(60 - H));
                B = 3 * I + (R + G);
                return;
            }
            B = I * (1 - S);
            R = I * (1 + S * Math.Cos(H) / Math.Cos(60 - H));
            G = 3 * I - (R + B);
        }

        /// <summary>
        /// Convert an color from the RGB color model (Red Green Blue)to the YUV model (Luminance RedChrominance BlueChrominance)
        /// </summary>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <param name="Y"></param>
        /// <param name="U"></param>
        /// <param name="V"></param>
        internal static void RGB2YUV(double R, double G, double B, out double Y, out double U, out double V)
        {
            Y = 0.299* R + 0.587 * G + 0.114 * B;
//          U = -0.146 * R - 0.288 * G + 0.434 * B;
//          V = 0.617 * R - 0.517 * G - 0.100 * B;
            U = 0.492 * (B - Y);
            V = 0.877 * (R - Y);

        }

        /// <summary>
        /// Convert an color from the YUV model (Luminance RedChrominance BlueChrominance) to the RGB color model (Red Green Blue)
        /// </summary>
        /// <param name="Y"></param>
        /// <param name="U"></param>
        /// <param name="V"></param>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        internal static void YUV2RGB(double Y, double U, double V, out double R, out double G, out double B)
        {
            // warning : Unchecked code.
            // Conversion grabbed from the web
            R = 1 * Y - 0.0009267 * (U - 128) + 1.4016868 * (V - 128);
            G = 1 * Y - 0.3436954 * (U - 128) - 0.7141690 * (V - 128);
            B = 1 * Y + 1.7721604 * (U - 128) + 0.0009902 * (V - 128);
        }


        private static ArrayList mHist = new ArrayList();
        /// <summary>
        /// Build an histogram based on a multidimentinal array (2 or 3)
        /// </summary>
        /// <param name="tval">the multidimentional array containing the data</param>
        /// <returns>an array of float representing the histogram</returns>
 
        internal static float[] getFHistogram(object tval)
        {
            float[] hist=new float[256];
            
            float [,] data = tval as float[,];
                if(data!=null)
                {
                    int w = data.GetUpperBound(0);
                    int h = data.GetUpperBound(1);
                    float []bd=IPUtil.GetBounds(data);
                    
                    Console.WriteLine("miinimaxx "+bd[0]+" "+bd[1]);
                    bd[1]=2000;
                    bd[1]-=bd[0];
                                        
                    for(int i=0;i<w;i++)
                    {
                        for(int j=0;j<h;j++)
                        {
                            int idx=(int)(255.0*(data[i,j]-bd[0])/bd[1]); 
                            if(idx>255){idx=255;}
                            if(idx<0){idx=9;}
                            hist[idx]+=1.0f;
                        }
                    }
                    float nbp = w*h;
                    for(int j=0;j<hist.Length;j++)
                    {
                        hist[j]/=nbp;
                    }
                }

            float [,,] ddata = tval as float[,,];
                if(ddata!=null)
                {
                    int w = ddata.GetUpperBound(1);
                    int h = ddata.GetUpperBound(2);
                    float []bd=IPUtil.GetBounds(ddata);
                
                    for(int i=0;i<w;i++)
                    {
                        for(int j=0;j<h;j++)
                        {
                            float val = RGBfloat2Yfloat(ddata[0,i,j],ddata[1,i,j],ddata[2,i,j]);
                            int idx=(int)(255.0*(val-bd[0])/bd[1]); 
                            if(idx>255){idx=255;}
                            if(idx<0){idx=9;}
                            hist[idx]+=1.0f;
                        }
                    }
                    float nbp = w*h;
                    for(int j=0;j<hist.Length;j++)
                    {
                        hist[j]/=nbp;
                    }
                }
            mHist.Add(hist);            

            return hist;
        }

        /// <summary>
        /// Buid an Histogram for a bitmap
        /// </summary>
        /// <param name="bmp">The bitmap to be analyzed</param>
        /// <returns>an array of float containing the frequency found for each luminance</returns>
        internal static float[] getFHistogram(Bitmap bmp)
        {
            float[] hist = new float[256];
            if(bmp != null && bmp.Width * bmp.Height != 0)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    for(int j = 0; j < bmp.Height; j++)
                    {
                        int lum = IPUtil.RGB2Yint(bmp.GetPixel(i,j));
                        hist[lum] += 1.0f;
                    }
                }
                float nbp = bmp.Width * bmp.Height;
                for(int j = 0; j < hist.Length; j++)
                {
                    hist[j] /= nbp;
                }
            }
            return hist;
        }

        /// <summary>
        /// Retrieve the histogram of 2 Bitmap
        /// </summary>
        /// <param name="bmp1">the first bitmap</param>
        /// <param name="bmp2">the second bitmap</param>
        /// <returns>the bihistogram of the bitmap</returns>
        internal static float[,] getFBiHistogram(Bitmap bmp1, Bitmap bmp2)
        {
            float[,] hist = new float[256,256];
            if(bmp1 != null && bmp1.Width * bmp1.Height != 0 )
            {
                if(bmp2 != null && bmp2.Width * bmp2.Height != 0 )
                {
                    if(bmp1.Width == bmp2.Width && bmp1.Height == bmp2.Height)
                    {
                        int wi = bmp1.Width;
                        int he = bmp1.Height;
                        for(int i = 0; i < wi; i++)
                        {
                            for(int j = 0; j < he; j++)
                            {
                                int lum1 = IPUtil.RGB2Yint(bmp1.GetPixel(i,j));
                                int lum2 = IPUtil.RGB2Yint(bmp2.GetPixel(i,j));
                                hist[lum1,lum2] += 1.0f;
                            }
                        }
                        float nbp = wi * he;
                        nbp *= nbp;
                        int w = hist.GetUpperBound(0);
                        int h = hist.GetUpperBound(1);
                        for(int i = 0; i < w; i++)
                        {
                            for(int j = 0; j < h; j++)
                            {
                                hist[i,j] /= nbp;
                            }
                        }
                    }
                }
            }
            return hist;
        }

        /// <summary>
        /// Debug purpose - dump the histogram array to console
        /// </summary>
        internal static void dumpHistArray()
        {
            object[] tobj = mHist.ToArray();

            Console.WriteLine("\n");
            for (int t = 0; t < 256; t++)
            {
                for (int j = 0; j < tobj.Length; j++)
                {
                    float[] lhi = (float[])tobj[j];

                    Console.Write(lhi[t] + ";");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n");
        }



        internal const int ALPHA = unchecked((int)0xFF000000);
        internal const int RED = 0x00FF0000;
        internal const int GREEN = 0x0000FF00;
        internal const int BLUE = 0x000000FF;
        internal static readonly int[] intColor = new int[] { ALPHA, RED, GREEN, BLUE };

        /// <summary>
        /// Build the histogram for this image
        /// </summary>
        /// <param name="imageAdapter">The image to analyze</param>
        /// <returns></returns>
        internal static Hashtable BuildHistogram(IImageAdapter imageAdapter)
        { 
            // Initialize the variables
            Hashtable hashChannel = new Hashtable();
            for (int channel = 0; channel < intColor.Length; channel++)
            {
                ArrayList[] list1 = new ArrayList[256];
                for (int index = 0; index < 256; index++)
                {
                    list1[index] = new ArrayList();
                }
                hashChannel.Add(intColor[channel], list1);
            }


            // Build the histogram
            IColor color = null;

            for (int height = 0; height < imageAdapter.Height; height++)
            {
                for (int width = 0; width < imageAdapter.Width; width++)
                {
                    color = (IColor)imageAdapter[width, height].Clone();
                    Point point = new Point(width, height);
                    ((ArrayList[])hashChannel[ALPHA])[color.A].Add(point);
                    ((ArrayList[])hashChannel[RED])[color.R].Add(point);
                    ((ArrayList[])hashChannel[GREEN])[color.G].Add(point);
                    ((ArrayList[])hashChannel[BLUE])[color.B].Add(point);
                }
            }

            return hashChannel;
        }


        /// <summary>
        /// Create a IImageAdapter based on a histogram
        /// </summary>
        /// <param name="dictio">The histogram containg the points</param>
        /// <param name="source">The Histogram to convert</param>
        internal static IImageAdapter HistogramToIImageAdapter(Hashtable dictio, IImageAdapter source)
        {
            // check params
            if (dictio == null || source == null)
            { 
                throw new ArgumentNullException( (dictio == null) ? "dictio" : "source", "Argument cannot be null");
            }

            IImageAdapter imageAdapter = (IImageAdapter)source.Clone();

            // Build IImageAdapter based on histogram.
            IColor color = null;
            for (int channel = 0; channel < intColor.Length; channel++)
            {
                for (int index = 0; index < 256; index++)
                {
                    foreach (Point point in ((ArrayList[])dictio[intColor[channel]])[index])
                    {
                        color = imageAdapter[point.X, point.Y];
                        switch (intColor[channel])
                        {
                            case ALPHA :
                                color.A = (byte)index;
                                break;
                            case RED :
                                color.R = (byte)index;
                                break;
                            case GREEN :
                                color.G = (byte)index;
                                break;
                            case BLUE :
                                color.B = (byte)index;
                                break;
                        }
//                        imageAdapter[point.X, point.Y] = color;
                    }
                }
            }

            return imageAdapter;
        }


        /// <summary>
        /// Contrast an image by Streching the Histogram associated with this image
        /// </summary>
        /// <param name="imageAdapter">the image to contrast</param>
        /// <returns>The contrasted image</returns>
        internal static IImageAdapter ContrastStretch(IImageAdapter imageAdapter)
        {
            // Check Params
            if (imageAdapter == null)
            { 
                throw new ArgumentNullException("imageAdapter", "Argument cannot be null");
            }
            IImageAdapter retVal = new ImageAdapter(imageAdapter.Width, imageAdapter.Height);
            IColor color = imageAdapter[0, 0];
            int[] min = new int[] { (int)color.MinChannelValue, (int)color.MinChannelValue, (int)color.MinChannelValue, (int)color.MinChannelValue };
            int[] max = new int[] { (int)color.MaxChannelValue, (int)color.MaxChannelValue, (int)color.MaxChannelValue, (int)color.MaxChannelValue };
            Hashtable dico = BuildHistogram(imageAdapter);
            Hashtable stretched = new Hashtable();
            for (int channel = 1; channel < intColor.Length; channel++)  // channel = 1 to Ignore Alpha Channel
                {
                    ArrayList[] list = new ArrayList[256];
                    for (int index = 0; index < 256; index++)
                    {
                        list[index] = new ArrayList();
                    }
                    stretched.Add(intColor[channel], list);
                }
                stretched[ALPHA] = ((ArrayList[])dico[ALPHA]).Clone();  // Clone Alpha channel from original Histogram


            // Find the min and max value of the histogram
            for (int channel = 1; channel < intColor.Length; channel++) // ignore the alpha channel
            {
                bool minFound = false;
                ArrayList[] array = (ArrayList[])dico[intColor[channel]];

                for (int index = 0; index < 256; index++)
                {
                    if (array[index].Count != 0)
                    {
//                      sum[channel] += array[index].Count;
                        max[channel] = index;
                        minFound = true;
                    }
                    else
                    {
                        if (!minFound)
                        {
                            min[channel] = index;
                        }
                    }
                }
            }

            // Stretch the histogram
            // Transformation used : 
            //    (min <= i <= max)
            //    Stretch   :   (i - min) * 255 / ( max - min) + 0.5
            for (int channel = 1; channel < intColor.Length; channel++) // Ignore the Alpha Channel
            {
                ArrayList[] channelOriginal = (ArrayList[])dico[intColor[channel]];
                ArrayList[] channelStretched = (ArrayList[])stretched[intColor[channel]];
                int minChannel = min[channel];
                int maxChannel = max[channel];
                float stretchConstant = 255.0f / (float)(maxChannel - minChannel);
                float stretch = 0f;

                for (int index = minChannel; index <= maxChannel; index++)
                {
                    stretch = (index - minChannel) * stretchConstant + 0.5f;
                    channelStretched[(int)stretch].AddRange(channelOriginal[index]);
                }
            }

            retVal = HistogramToIImageAdapter(stretched, imageAdapter);

            dico.Clear();
            stretched.Clear();

            return retVal;
        }


        /// <summary>
        /// Equalize the image on all channels
        /// </summary>
        /// <param name="imageAdapter">The image to equalize</param>
        /// <returns>The Equalized image</returns>
        internal static IImageAdapter EqualizeImage(IImageAdapter imageAdapter)
        {
            // Check params
            if (imageAdapter == null)
            { 
                throw new ArgumentNullException("imageAdapter", "Argument cannot be null");
            }

            IImageAdapter retVal = new ImageAdapter(imageAdapter.Width, imageAdapter.Height);
            int[] sum = new int[] { 0, 0, 0, 0 };
            Hashtable dico = BuildHistogram(imageAdapter);
            Hashtable equalized = new Hashtable();

            for (int channel = 1; channel < intColor.Length; channel++)  // Ignore Alpha Channel
            {
                ArrayList[] list = new ArrayList[256];
                for (int index = 0; index < 256; index++)
                {
                    list[index] = new ArrayList();
                }

                equalized.Add(intColor[channel], list);
            }
            equalized[ALPHA] = ((ArrayList[])dico[ALPHA]).Clone();  // Clone Alpha channel from original Histogram

            // compute the sum per channel
            for (int channel = 0; channel < intColor.Length; channel++)
            { 
                ArrayList[] array = (ArrayList[])dico[intColor[channel]];
                for (int index = 0; index < 256; index++)
                { 
                    sum[channel] += array[index].Count;
                }
            }

            // Stretch and Normalize the histogram
            // Transformation used : 
            //    (min <= i <= max)
            //    Normalize :   0.5 + ( Sum(0,i-1) + ( Sum(i-1,i) / 2 ) ) * 255 / Sum(0,255) 
            for (int channel = 1; channel < intColor.Length; channel++) // Ignore Alpha channel
            {               
                ArrayList[] channelOriginal = (ArrayList[])dico[intColor[channel]];
                ArrayList[] channelEqualized = (ArrayList[])equalized[intColor[channel]];
                float equalizeConstant = 255.0f / ((sum[channel] != 0) ? sum[channel] : 1); 
                int currentSum = 0;
                float equalize = 0f;
                for (int index = 0; index < 256; index++)
                { 
                    equalize = 0.5f + (currentSum + channelOriginal[index].Count / 2) * equalizeConstant;
                    currentSum += channelOriginal[index].Count;
                    channelEqualized[(int)equalize].AddRange(channelOriginal[index]);
                }
            }


            retVal = HistogramToIImageAdapter(equalized, imageAdapter);

            equalized.Clear();
            dico.Clear();

            return retVal;
        }


        /// <summary>
        /// Equalize the histogram on all channels
        /// </summary>
        /// <param name="histogram">The histogram to be used</param>
        /// <param name="colorThreshold">The threshold per channel</param>
        /// <returns>The expanded histogram </returns>
        internal static Array EqualizeHistogram(Array histogram, IColor colorThreshold)
        { 
            Array retVal = (Array)histogram.Clone();
            Array.Clear(retVal, 0, retVal.Length);

            int[] min = new int[histogram.Rank];
            int[] max = new int[histogram.Rank];
            Point[] bounds = new Point[histogram.Rank];
            double[] sums = new double[histogram.Rank];
//          int maxChannel = int.MinValue;
//          int minChannel = int.MaxValue;

            // Find the min and max on each channels
            for (int i = 0; i < histogram.Rank; i++)    // Repeat for each channels
            {
                sums[i] = 0.0;
                bool findMin = true;
                min[i] = histogram.GetLowerBound(i);
                max[i] = histogram.GetUpperBound(i);
                bounds[i] = new Point(min[i], max[i]);

                for (int t = bounds[i].X; t < bounds[i].Y; t++) // find min and max across all channels
                {
                    float entry = 0f;
                    double threshold = 0f;
                    if (histogram.Rank == 1)
                    {
                        entry = (float)histogram.GetValue(t);
                        threshold = colorThreshold.Red; // in this case A = R = G = B
                    }
                    else
                    {
                        entry = (float)histogram.GetValue(i, t);
                        if (histogram.Rank == 3)
                        {
                            switch (i) 
                            {
                                case 0 :
                                    threshold = colorThreshold.Red;
                                    break;
                                case 1 : 
                                    threshold = colorThreshold.Green;
                                    break;
                                case 2 : 
                                    threshold = colorThreshold.Blue;
                                    break;
                            }
                        }
                        else
                        {
                            if (histogram.Rank != 4)
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                            switch (i)
                            {
                                case 0 :
                                    threshold = colorThreshold.Alpha;
                                    break;
                                case 1 :
                                    threshold = colorThreshold.Red;
                                    break;

                                case 2 :
                                    threshold = colorThreshold.Green;
                                    break;
                                case 3 :
                                    threshold = colorThreshold.Blue;
                                    break;
                            }
                        }
                    }

                    if (entry > (float)threshold)
                    {
                        sums[i] += entry;
                        max[i] = t;
                    }
                    if (findMin)
                    {
                        min[i] = t;
                        if (entry > (float)threshold)
                        {
                            findMin = false;
                        }
                    }
                }

                // Stretch ans Equalize histogram
                double stretch = 0;
                int equalize = 0;
                double currentSum = 0.0;
                for (int rank = 0; rank < histogram.Rank; rank++)
                {
                    for (int index = min[rank]; index <= max[rank]; index++)
                    {
                        if (histogram.Rank == 1)
                        {
                            currentSum += (float)histogram.GetValue(index);
                        }
                        else 
                        {
                            currentSum += (float)histogram.GetValue(rank, index);
                        }

                        stretch = /*.5+ */ (index - min[rank]) * colorThreshold.NormalizedValue / (max[rank] - min[rank]);
                        equalize = (int)(stretch * currentSum / sums[rank]);

                        if (histogram.Rank == 1)
                        {
                            retVal.SetValue((float)retVal.GetValue(equalize) + (float)histogram.GetValue(index), equalize);
                        }
                        else
                        {
                            retVal.SetValue((float)retVal.GetValue(rank,equalize) + (float)histogram.GetValue(rank, index), rank, equalize);
                        }
                    }
                }

                // Equalize histogram
            }


            return retVal;
/*
            int min = histogram.GetLowerBound(0);
            int max = histogram.GetUpperBound(0);
            Point bounds = new Point(min, max);

            bool findMin = true;
            for (int t = bounds[0]; t < bounds[1]; t++)
            { 
                float entry = histogram[t];
                if (entry != 0 && entry <= Threshold)
                { 
                    max = t;
                }
                if (findMin)
                {
                    if (entry > Threshold)
                    {
                        findMin = false;
                    }
                    else
                    {
                        min = t;
                    }
                }
            }
*/
            


        }

        /// <summary>
        /// Check the value of an argument
        /// </summary>
        /// <param name="obj">the argument passed in</param>
        internal static void ChkNull(object obj)
        {
            ChkNull(obj,"");
        }

        /// <summary>
        /// Check the value of an argument
        /// </summary>
        /// <param name="obj">the argument passed in</param>
        /// <param name="err">The error string to be passed in the exception</param>
        internal static void ChkNull(object obj, string err)
        {
            if(obj ==null)
            {
                throw new ArgumentNullException(err);
            }
        }


        private const  int TCOLMAX = 255;
        /// <summary>
        /// Bound a value (output value will be between 0 and 255)
        /// </summary>
        /// <param name="v">the value to be mappped</param>
        /// <returns>The value mapped</returns>
        internal static Color getColorMapping(int v)
        {
            if(v<0){v=0;}
            if(v>=TCOLMAX){v=TCOLMAX-1;}
            return mColor[v];
        }
        private static Color[] mColor = initCol();


        /// <summary>
        /// convert a string to a ChannelCompareMode
        /// </summary>
        /// <param name="str">the compare mode as string</param>
        /// <returns>the compare mode as Enum value</returns>
        internal static ChannelCompareMode ParseChannelString(string str)
        {
            ChannelCompareMode mode = ChannelCompareMode.ARGB;
            switch (str.Trim().ToUpper())
            {
                case "ARGB":
                    mode = ChannelCompareMode.ARGB;
                    break;
                case "RGB":
                    mode = ChannelCompareMode.RGB;
                    break;
                case "A":
                    mode = ChannelCompareMode.A;
                    break;
                case "R":
                    mode = ChannelCompareMode.R;
                    break;
                case "G":
                    mode = ChannelCompareMode.G;
                    break;
                case "B":
                    mode = ChannelCompareMode.B;
                    break;
                default:
                    throw new RenderingVerificationException("ChannelCompareMode mode unknown: "+str);
                }
            return mode;
        
        }

        private static Color[] initCol()
        {
            Color[] tcol=new Color[TCOLMAX];
            int idx=0;
            int bin = TCOLMAX/4;
            for(int j=0;j<TCOLMAX;j++){tcol[j]=Color.Black;}
            for(int j=0;j<bin;j++)
            {
                if(idx<TCOLMAX){tcol[idx]=Color.FromArgb(0,255*j/bin,255);}
                idx++;
            }
            for(int j=0;j<bin;j++)
            {
                if(idx<TCOLMAX){tcol[idx]=Color.FromArgb(0,255,255-255*j/bin);}
                idx++;
            }
            for(int j=0;j<bin;j++)
            {
                if(idx<TCOLMAX){tcol[idx]=Color.FromArgb(255*j/bin,255,0);}
                idx++;
            }
            for(int j=0;j<bin;j++)
            {
                if(idx<TCOLMAX){tcol[idx]=Color.FromArgb(255,255-255*j/bin,0);}
                idx++;
            }
            tcol[0]=Color.Black;
            return tcol;
        }

        /// <summary>
        /// Build a bimap from data (array of float)
        /// </summary>
        /// <param name="data">The values for the bitmap</param>
        /// <returns></returns>
        internal static Bitmap getBmpFromData(float[,] data)
        {
            Bitmap bmp = new Bitmap(1,1);
            if(data!=null)
            {
                int wi = data.GetUpperBound(0);
                int he = data.GetUpperBound(1);
                bmp = new Bitmap(wi,he);

                float[] minmax = GetBounds(data);
                
                for(int i=0;i<wi;i++)
                {
                    for(int j=0;j<he;j++)
                    {
                        int v = (int)(TCOLMAX*data[i,j]/minmax[1]);
                        bmp.SetPixel(i,j,getColorMapping(v));
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Build a bitmap from an error historgram
        /// </summary>
        /// <param name="data">the error histogram</param>
        /// <param name="height">the heigh of the bitmap to be created</param>
        /// <returns></returns>
        internal static Bitmap getBmpFromErrHist(float[] data, int height)
        {
            Bitmap bmp = new Bitmap(1,1);
            if(data!=null)
            {
                bmp = new Bitmap(data.Length,height);
                Graphics grp = Graphics.FromImage(bmp);
                
                grp.FillRectangle(Brushes.Black,0,0,bmp.Width,bmp.Height);
                
                data[0]=0; // reset 0 as it does not count for the error
                float []minmax = IPUtil.GetBounds(data);
                Pen dpen = new Pen(Color.FromArgb(200,100,200,0));
                for(int j=0;j<data.Length;j++)
                {
                    int nbc = (int)(height*data[j]/minmax[1]);
                    if(nbc==0&&data[j]!=0){nbc=1;}
                    grp.DrawLine(dpen,j,height,j,height-nbc);
                    
                }
                // draw % max of pixels in error
                grp.DrawString((100*minmax[1]).ToString(),new Font("arial",8.0f),new SolidBrush(Color.FromArgb(200,255,255,0)),10,10);
                grp.Dispose();
            }
            return bmp;
        }
    
        /// <summary>
        /// Get the bound of a (multidimensional) array
        /// </summary>
        /// <param name="data">the array (might be multidimentional or not)</param>
        /// <returns>the bitmap generated using the data in the error histogram</returns>
        internal static float[] GetBounds(object data)
        {
            float max=float.MinValue;
            float min=float.MaxValue;
            
            float[] sdata = data as float[];
                if(sdata!=null)
                {
                    int w = sdata.Length;
                    for(int j=0;j<w;j++)
                    {
                        if(sdata[j]>max){max=sdata[j];}
                        if(sdata[j]<min){min=sdata[j];}
                    }
                }
            
            float[,] bwdata = data as float[,];
            
                if(bwdata!=null)
                {
                    int w = bwdata.GetUpperBound(0);
                    int h = bwdata.GetUpperBound(1);
                    for(int j=0;j<w;j++)
                    {
                        for(int i=0;i<h;i++)
                        {
                            if(bwdata[j,i]>max){max=bwdata[j,i];}
                            if(bwdata[j,i]<min){min=bwdata[j,i];}
                        }
                    }
                }

            float[,,] rgbdata = data as float[,,];
            
                if(rgbdata != null)
                {
                    int w = rgbdata.GetUpperBound(1);
                    int h = rgbdata.GetUpperBound(2);
                    for(int k=0;k<3;k++)
                    {
                        for(int j=0;j<w;j++)
                        {
                            for(int i=0;i<h;i++)
                            {
                                if(rgbdata[k,j,i]>max){max=rgbdata[k,j,i];}
                                if(rgbdata[k,j,i]<min){min=rgbdata[k,j,i];}
                            }
                        }
                    }
                }
        
            float[] bounds={min,max};
            return bounds;
        } 

        /// <summary>
        /// Normalize the data
        /// </summary>
        /// <param name="data">the data array</param>
        /// <param name="val">the coefficient to use for the normalization</param>
        internal static void Normalize(float[,] data, float val)
        {
            float max = float.MinValue;
            float min = float.MaxValue;

            int w = data.GetUpperBound(0);
            int h = data.GetUpperBound(1);
            for(int j = 0; j < w; j++)
            {
                for(int i = 0; i < h; i++)
                {
                    if(data[j,i] > max){max = data[j,i];}
                    if(data[j,i] < min){min = data[j,i];}
                }
            }
            max -= min;
            if(max == 0.0f){ max = 1.0f; }
            for(int j = 0; j < w; j++)
            {
                for(int i = 0; i < h; i++)
                {
                    data[j,i] = val * (data[j,i] - min) / max;
                }
            }
        }


        /// <summary>
        /// Set a max an min on the value in the array  -- Should be Crop ? Mistyped ?
        /// </summary>
        /// <param name="data">The (multidimentional) array</param>
        /// <param name="min">the min value acceptable in the array</param>
        /// <param name="max">the max value acceptable in the array</param>
        internal static void Crope(object data, float min, float max)
        {
            float[,] bwdata = data as float[,];
                if(bwdata != null)
                {
                    int w = bwdata.GetUpperBound(0);
                    int h = bwdata.GetUpperBound(1);
                    for(int j = 0; j < w; j++)
                    {
                        for(int i = 0; i < h; i++)
                        {
                            if(bwdata[j,i] < min){bwdata[j,i] = min;}
                            if(bwdata[j,i] > max){bwdata[j,i] = max;}
                        }
                    }
                }
                float [,]rgbdata = data as float[,];
                if(rgbdata != null)
                {
                    int w = rgbdata.GetUpperBound(1);
                    int h = rgbdata.GetUpperBound(2);
                    for(int k=0;k<3;k++)
                    {
                        for(int j=0;j<w;j++)
                        {
                            for(int i=0;i<h;i++)
                            {
                                if(rgbdata[j,i]<min){rgbdata[j,i]=min;}
                                if(rgbdata[j,i]>max){rgbdata[j,i]=max;}
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// Compute the entropy for this array
        /// </summary>
        /// <param name="tv">the data to be passed in </param>
        /// <returns>The entropy</returns>
        internal static float Entropy(float[] tv)
        {
            float ent = 0.0f;
            for(int j = 0; j < tv.Length; j++)
            {
                if(tv[j] != 0.0f)
                {
                    ent += (float)(-tv[j] * Math.Log(tv[j], 2.0));
                }
            }
            return ent;
        }
        
        /// <summary>
        /// Compute the Entropy
        /// </summary>
        /// <param name="tv">first array</param>
        /// <param name="ts">second array</param>
        /// <param name="tt">thrird array</param>
        /// <returns></returns>
        internal static float Entropy(float[,] tv, float[] ts, float[] tt)
        {
            float ent = 0.0f;
            int w = tv.GetUpperBound(0);
            int h = tv.GetUpperBound(1);
            for(int i = 0; i < w; i++)
            {
                for(int j = 0; j < h; j++)
                {
                    if(tv[i,j] != 0.0f && ts[i] !=0.0f && ts[j] != 0.0f)
                    {
                        ent += (float)(-tv[i,j] * Math.Log(tv[i,j], 2.0 / (ts[i] * ts[j])));
                    }
                }
            }
            return ent;
        }

    

        private const int HMAX=256;

        /// <summary>
        /// Compute the difference between entry data in 2 arrays (always >= 0)
        /// </summary>
        /// <param name="a">the first array </param>
        /// <param name="b">the second array</param>
        /// <returns>an array containg the difference between the arrays</returns>
        internal static float[,] diffFArray(float[,] a, float[,] b)
        {
            float[,] diff = null;
            if(a != null && b != null)
            {
                int w = a.GetUpperBound(0);
                int h = a.GetUpperBound(1);
                if(b.GetUpperBound(0) == w && b.GetUpperBound(1) == h)
                {
                    diff = new float[w,h];
                    for(int i = 0; i < w; i++)
                    {
                        for(int j = 0; j < h; j++)
                        {
                            Math.Abs(diff[i,j] = a[i,j] - b[i,j]);
                        }
                    }
                }
            }
            return diff;
        }

        /// <summary>
        /// Compute the difference between entry data in 2 arrays (always >= 0)
        /// </summary>
        /// <param name="a">the first array </param>
        /// <param name="b">the second array</param>
        /// <returns>an array containg the difference between the arrays</returns>
        internal static float[,] diffFArray (float[, ,] a, float[, ,] b)
        {
            float [,]diff=null;
            if(a!=null && b!=null)
            {
                int w=a.GetUpperBound(1);
                int h=a.GetUpperBound(2);
                if(b.GetUpperBound(1)==w&&b.GetUpperBound(2)==h)
                {
                    diff=new float[w,h];            
                    for(int i=0;i<w;i++)
                    {
                        for(int j=0;j<h;j++)
                        {
                            float dR = a[0,i,j]-b[0,i,j];
                            float dG = a[1,i,j]-b[1,i,j];
                            float dB = a[2,i,j]-b[2,i,j];
                            diff[i,j]= (float)Math.Sqrt(dR*dR+dG*dG+dB*dB);
                        }
                    }
                } 
            }
            return diff;
        }

        /// <summary>
        /// Compute the distance between entry data in 2 arrays (always >= 0)
        /// </summary>
        /// <param name="a">the first array </param>
        /// <param name="b">the second array</param>
        /// <param name="pow">Why divide h and w by this? </param>
        /// <returns>an array containg the difference between the arrays</returns>
        internal static float[,] diffFArray(float[, ,] a, float[, ,] b, int pow)
        {
            float[,] diff = null;
            if(a != null && b != null && pow > 0)
            {
                int w = a.GetUpperBound(1);
                int h = a.GetUpperBound(2);
                if(b.GetUpperBound(1) == w && b.GetUpperBound(2) == h)
                {
                    diff = new float[w,h];
                    w /= pow;
                    h /= pow;
                    for(int i = 0; i < w; i++)
                    {
                        for(int j = 0; j < h; j++)
                        {
                            float dR = a[0,i,j] - b[0,i,j];
                            float dG = a[1,i,j] - b[1,i,j];
                            float dB = a[2,i,j] - b[2,i,j];
                            diff[i,j] = (float)Math.Sqrt(dR * dR + dG * dG + dB * dB);
                        }
                    }
                } 
            }
            return diff;
        }
    }
}
