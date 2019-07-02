// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using
    
    /// <summary>
    /// A Discret Wavelet Transform based low pass Filter (Currently only implement Daubechies4)
    /// </summary>
    public class WaveletTransformFilter: Filter
    {
        #region Constants
            private const string LEVEL = "Level";
            private const float c0 = 0.4829629131445341f;
            private const float c1 = 0.8365163037378079f;
            private const float c2 = 0.2241438680420134f;
            private const float c3 =-0.1294085225512604f;
        #endregion Constants

        #region Properties
            #region Private Properties
                private float[] mData = null;
                /// <summary>
                /// The Data representing the color image
                /// </summary>
                private float[,,] rgbPixels =null;
                /// <summary>
                /// indexer to the (float array) data
                /// </summary>
                private float this[int idx] 
                {
                    get { return mData[idx-1]; }
                    set { mData[idx-1] = value; }
                }
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Perform a Wavelet based (Daubechies 4) low pass fitlering of the image";
                    }
                }
                /// <summary>
                /// The level of details filtering.
                /// 0-nothing 1-some 2-more n-all
                /// </summary>
                /// <value></value>
                public int Level
                {
                    get 
                    {
                        return (int)this[LEVEL].Parameter;
                    }
                    set 
                    {
                        if (value < 0) { throw new ArgumentException("Level must be positive or null"); }
                        this[LEVEL].Parameter = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Discret Wavelet Transform Filter constructor
            /// </summary>
            public WaveletTransformFilter()
            {
                FilterParameter level = new FilterParameter(LEVEL, "The value of the Level to use for the details fitlering", (int)1);
                AddParameter(level);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Normalize the Image
                /// </summary>
                private void Normalize(IImageAdapter iiad) {
                                
                    int wi = rgbPixels.GetUpperBound(1);
                    int he = rgbPixels.GetUpperBound(2);
                    
                    //normalization
                    float max = float.MinValue;
                    
                    for(int k = 0; k < 3; k++)
                    {
                        for(int j = 0; j < he; j++)
                        {
                            for(int i = 0; i < wi; i++)
                            {
                                float lva=(float)(Math.Abs(rgbPixels[k,i,j]));
                                if(lva> max){max = lva;}
                            }
                        }
                    }

                    #if VERBOSE
                    Console.WriteLine("Wavelet maxabs " + max);
                    #endif
                    
                    if(max == 0.0f){max = 1.0f;}
                    for(int j = 0; j < he; j++)
                    {
                        for(int i = 0; i < wi; i++)
                        {
                            iiad[i,j].Red   = (float)Math.Abs(rgbPixels[0,i,j]/max);
                            iiad[i,j].Green = (float)Math.Abs(rgbPixels[1,i,j]/max);
                            iiad[i,j].Blue  = (float)Math.Abs(rgbPixels[2,i,j]/max);
                        }
                    }
                }
                /// <summary>
                /// Bilinearly Interpolate the filtered image to keep visual smoothness and avoid edgy effect
                /// </summary>
                private IImageAdapter Interp(IImageAdapter iiad) 
                {
                    ImageAdapter retVal = new ImageAdapter(iiad);
                    int llLength=(int)Math.Pow(2,Level);
                    double wexpFactor=(double)(iiad.Width-4*Level)/((iiad.Width)*llLength);
                    double hexpFactor=(double)(iiad.Height-4*Level)/((iiad.Height)*llLength);
                    
                    for(int j=0;j<iiad.Height;j++)
                    {
                        
                        for(int i=0;i<iiad.Width;i++)
                        {
                            double x = (double)i*wexpFactor;
                            double y = (double)j*hexpFactor;
                            retVal[i,j]= BilinearInterpolator.ProcessPoint(x,y,iiad,null);
                        }
                    }
                 
                    return retVal;
                }
                /// <summary>
                /// Process the image
                /// </summary>
                /// <param name="it">number of iteration</param>
                /// <param name="fwd"></param>
                private void Process(int it,bool fwd)
                {
                    int wi = rgbPixels.GetUpperBound(1);
                    int he = rgbPixels.GetUpperBound(2);
                
                    // horizontal processing
                    for(int k = 0; k < 3; k++)
                    {
                        for(int j = 0; j < he; j++)
                        {
                            float[] vx = new float[wi];
                            for(int i = 0; i < wi; i++)
                            {
                                vx[i] = rgbPixels[k,i,j];
                            }
                            LineProcess(vx, fwd, it);
                            for(int i = 0; i < wi; i++)
                            {
                                rgbPixels[k,i,j] = vx[i];
                            }
                        }
                    }
                    // vertical processing
                    for(int k = 0; k < 3; k++)
                    {
                        for(int i = 0; i < wi; i++)
                        {
                            float[] vx = new float[he];
                            for(int j = 0; j < he; j++)
                            {
                                vx[j] = rgbPixels[k,i,j];
                            }
                        
                            LineProcess(vx, fwd, it);
                            for(int j = 0; j < he; j++)
                            {
                                rgbPixels[k,i,j] = vx[j];
                            }
                        }
                    }
                }
                /// <summary>
                /// Process a 1D signal like a an image line
                /// </summary>
                /// <param name="data">data to be processes</param>
                /// <param name="forward">forward</param>
                /// <param name="nit">number of iteration</param>
                private void LineProcess(float []data,bool forward,int nit)
                {
                    mData = data;
                    
                    if(forward==true)
                        {
                            int nn=data.Length;
                            for(int j=0;j<nit;j++)
                            {
                                Daubechies4(forward, nn, data);
                                nn>>=1;
                            }
                        }
                        else 
                        {
                            int nn=data.Length/(int)Math.Pow(2,nit-1);
                            for(int j=0;j<nit;j++)
                            {
                                Daubechies4(forward, nn, data);
                                nn<<=1;
                            }
                    }
                }
                /// <summary>
                /// Process the bitmap
                /// </summary>
                /// <param name="forward">direction of operation</param>
                /// <param name="n">number of iterations</param>
                /// <param name="data">data to process</param>
                private void Daubechies4(bool forward,int n,float []data)
                    {
                     
                    try
                    {
                        float []wksp=null;
                        float []a = data;
                        int nh,nh1;
                        if(a.Length<4)
                        {
                            return;
                        }
                        
                        wksp=new float[a.Length+1];
                        nh1=(nh=n>>1)+1;

                        int isign=-1;
                        if(forward==true)
                        {
                            isign=1;
                        }
                        WaveletTransformFilter lw = this;

                        int i,j;
                        if(isign>=0)
                        {
                            for(i=1,j=1;j<=n-3;j+=2,i++)
                            {
                                wksp[i]   = c0*lw[j] +c1*lw[j+1] +c2*lw[j+2] +c3*lw[j+3];
                                wksp[i+nh]= c3*lw[j] -c2*lw[j+1] +c1*lw[j+2] -c0*lw[j+3];
                            }

                            wksp[i]   = c0*lw[n-1] +c1*lw[n] +c2*lw[1] +c3*lw[2];
                            wksp[i+nh]= c3*lw[n-1] -c2*lw[n] +c1*lw[1] -c0*lw[2];
                        }
                        else
                        {
                            wksp[1]= c2*lw[nh] +c1*lw[n] +c0*lw[1] +c3*lw[nh1];
                            wksp[2]= c3*lw[nh] -c0*lw[n] +c1*lw[1] -c2*lw[nh1];
                            for(i=1,j=3;i<nh;i++)
                            {
                                wksp[j++]= c2*lw[i] +c1*lw[i+nh] +c0*lw[i+1] +c3*lw[i+nh1];
                                wksp[j++]= c3*lw[i] -c0*lw[i+nh] +c1*lw[i+1] -c2*lw[i+nh1];
                            }
                        }

                        for(i=1;i<=n;i++)
                        {
                            lw[i]=wksp[i];
                        }
                    }
                    catch(Exception)
                    {
                    }
                    }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "The value passed in cannot be null");
                    }

                    int width = source.Width;
                    int height = source.Height;

                    rgbPixels = new float[3,source.Width,source.Height];
                    for (int j = 0; j < source.Height; j++)
                    {
                        for (int i = 0; i < source.Width; i++)
                        {
                            rgbPixels[0,i,j] = (float)source[i,j].Red;
                            rgbPixels[1,i,j] = (float)source[i,j].Green;
                            rgbPixels[2,i,j] = (float)source[i,j].Blue;
                        }
                    }
                    IImageAdapter retVal = new ImageAdapter(source);
                       Process(Level,true);
                    Normalize(retVal);
                    retVal = Interp(retVal);
                    
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
