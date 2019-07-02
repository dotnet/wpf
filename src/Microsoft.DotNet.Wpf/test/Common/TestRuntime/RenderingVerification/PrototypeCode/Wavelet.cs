// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Drawing;
    using Microsoft.Test.RenderingVerification;
    
    /// <summary>
    /// Summary description for Wavelet.
    /// </summary>
    public class Wavelet
    {
        const float c0= 0.4829629131445341f;
        const float c1= 0.8365163037378079f;
        const float c2= 0.2241438680420134f;
        const float c3=-0.1294085225512604f;

        /// <summary>
        /// Instanciate a wavelet object
        /// </summary>
        /// <param name="fic">the name of the image to load</param>
        /// <param name="nbit">the numberof time to process the image</param>
        unsafe public Wavelet(string fic, string nbit)
        {
            try 
            {
                int iint = int.Parse(nbit);
                Init(fic);
                Process(iint,true);
                //LowPassFilter(iint);
                Process(iint,false);
                Normalize();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// ???
        /// </summary>
        /// <param name="fic">The image to load</param>
        /// <param name="nbit">the number of time to process the image</param>
        unsafe public void oWavelet(string fic, string nbit)
        {
            try 
            {
                int it =int.Parse(nbit);
                Bitmap bmp=new Bitmap(fic);
                rgbPixels = new float[3,bmp.Width,bmp.Height];
                bwPixels  = new float[bmp.Width,bmp.Height];
            
/*
                Counter c = new Counter();
                c.Start();
                for(int j=0;j<bmp.Height;j++)
                {
                    for(int i=0;i<bmp.Width;i++)
                    {
                        Color col=bmp.GetPixel(i,j);
                        rgbPixels[0,i,j] = col.R;
                        rgbPixels[1,i,j] = col.G;
                        rgbPixels[2,i,j] = col.B;
                        bwPixels[i,j]= IPUtil.RGB2Yint(col);
                    }
                }
                c.Stop();
                Console.WriteLine("slow mo "+c.Seconds.ToString());
*/                

//                c = new Counter();
//                c.Start();
                ImageUtility ImageUtility = new ImageUtility(bmp);
                ImageUtility.GetSetPixelUnsafeBegin();

                Point size = new Point(ImageUtility.Bitmap32Bits.Width, ImageUtility.Bitmap32Bits.Height);
                for (int j = 0; j < size.Y; j++)
                {
                    for (int i = 0; i < size.X; i++)
                    {
                        int argb = ImageUtility.PixelDirectAccessUnsafe(i, j);
                        rgbPixels[0,i,j] = ((argb) & 0x00FF0000) >> 16;
                        rgbPixels[1,i,j] = ((argb) & 0x0000FF00) >> 8;
                        rgbPixels[2,i,j] = ((argb) & 0x000000FF);
                    }
                }
                
                ImageUtility.GetSetPixelUnsafeRollBack();
//                c.Stop();
//                Console.WriteLine("fast mo "+c.Seconds.ToString());


                mVisData = rgbPixels;
                image = bmp;
                if(it>0)
                {
                    //ProcessBW(bmp,it);
                    ProcessRGB(bmp,it);
                }
            } 
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        /// <summary>
        /// Initialize the rgbPixels array, mVisData and the bitmap
        /// </summary>
        /// <param name="fic">The Image to load</param>
        unsafe public void Init(string fic)
        {
            try 
            {
                Bitmap bmp = new Bitmap(fic);
                rgbPixels = new float[3, bmp.Width, bmp.Height];
                bwPixels  = new float[bmp.Width, bmp.Height];
//                Counter c = new Counter();
//                c.Start();
                ImageUtility ImageUtility = new ImageUtility(bmp);
                ImageUtility.GetSetPixelUnsafeBegin();
                Point size = new Point(ImageUtility.Bitmap32Bits.Width, ImageUtility.Bitmap32Bits.Height);
                for (int j = 0; j < size.Y; j++)
                {
                    for (int i = 0; i < size.X; i++)
                    {
                        int argb = ImageUtility.PixelDirectAccessUnsafe(i, j);
                        rgbPixels[0,i,j] = ((argb) & 0x00FF0000) >> 16;
                        rgbPixels[1,i,j] = ((argb) & 0x0000FF00) >> 8;
                        rgbPixels[2,i,j] = ((argb) & 0x000000FF);
                    }
                }
                ImageUtility.GetSetPixelUnsafeRollBack();
//                c.Stop();
//                Console.WriteLine("fast mo " + c.Seconds.ToString());
                mVisData = rgbPixels;
                image = bmp;
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// Process the bitmap
        /// </summary>
        /// <param name="it">number of iteration</param>
        /// <param name="fwd"></param>
        public void Process(int it,bool fwd)
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
                    Process(vx, fwd, it);
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
                
                    Process(vx, fwd, it);
                    for(int j = 0; j < he; j++)
                    {
                        rgbPixels[k,i,j] = vx[j];
                    }
                }
            }
        }


        /// <summary>
        /// Normalize the Image
        /// </summary>
        public void Normalize() {

            int wi = rgbPixels.GetUpperBound(1);
            int he = rgbPixels.GetUpperBound(2);
            
            Bitmap bmp = new Bitmap(wi,he);
            //normalization
            float max = float.MinValue;
            float min = float.MaxValue;
            for(int k = 0; k < 3; k++)
            {
                for(int j = 0; j < bmp.Height; j++)
                {
                    for(int i = 0; i < bmp.Width; i++)
                    {
                        if(rgbPixels[k,i,j] > max){max = rgbPixels[k,i,j];}
                        if(rgbPixels[k,i,j] < min){min = rgbPixels[k,i,j];}
                    }
                }
            }

            Console.WriteLine("Wavelet " + min + " " + max);

            //display
            max -= min;
            if(max == 0.0f){max = 1.0f;}
            for(int j = 0; j < bmp.Height; j++)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    int vaR = (int)(NormalizedColor.NormalizeValue * (rgbPixels[0,i,j] - min) / max);
                    int vaG = (int)(NormalizedColor.NormalizeValue * (rgbPixels[1, i, j] - min) / max);
                    int vaB = (int)(NormalizedColor.NormalizeValue * (rgbPixels[2, i, j] - min) / max);
                    Color col = Color.FromArgb(vaR,vaG,vaB);
                    bmp.SetPixel(i, j, col);                  
                }
            }
            image = bmp;
        }

        

        /// <summary>
        /// Filter the array of data
        /// </summary>
        /// <param name="power">The strenght of the low pass filter (?)</param>
        public void LowPassFilter(int power)
        {
            int pow = (int)Math.Pow(2, power);
            float[,,] a = rgbPixels;
            int w = a.GetUpperBound(1);
            int h = a.GetUpperBound(2);
            int rw = w / pow;
            int rh = h / pow;
            for(int i = 0; i < w; i++)
                {
                    for(int j = 0; j < h; j++)
                    {
                        if(i > rw || j > rw)
                        {
                            a[0,i,j] = 0;
                            a[1,i,j] = 0;
                            a[2,i,j] = 0;
                        }
                    }
                }
            } 
        

        /// <summary>
        /// Get the energy for this array
        /// </summary>
        /// <returns>the energy value</returns>
        public float getEnergy()
        {
            float[] en = new float[4];
                for(int i = 0; i <= rgbPixels.GetUpperBound(1); i++)
                {
                    for(int j = 0; j < rgbPixels.GetUpperBound(2); j++)
                    {
                        float len = rgbPixels[0,i,j] * rgbPixels[0,i,j];
                        en[0] += len;  en[3] += len;
                        len = rgbPixels[1,i,j] * rgbPixels[1,i,j];
                        en[1] += len;  en[3] += len;
                        len = rgbPixels[2,i,j] * rgbPixels[2,i,j];
                        en[2] += len;  en[3] += len;                                  
                    }
                }
            for(int j=0;j<en.Length;j++)
            {
                en[j] /= rgbPixels.GetUpperBound(1) * rgbPixels.GetUpperBound(2);
            }

            Console.WriteLine("Energy " + en[3] + "    " + en[0] + " " + en[1] + " " + en[2]);
            
            return en[3];
        }


        /// <summary>
        /// Proces color bitmap
        /// </summary>
        /// <param name="bmp">Bitmap to process</param>
        /// <param name="it">iteration</param>
        private void ProcessRGB(Bitmap bmp, int it)
        {
            mVisData = rgbPixels;

            getEnergy();

            // horizontal processing
            for(int k = 0; k < 3; k++)
            {
                for(int j = 0; j < bmp.Height; j++)
                {
                    float[] vx = new float[bmp.Width];
                    for(int i = 0; i < bmp.Width; i++)
                    {
                        vx[i] = rgbPixels[k,i,j];
                    }
                    Process(vx, true, it);
                    for(int i = 0; i < bmp.Width; i++)
                    {
                        rgbPixels[k,i,j] = vx[i];
                    }
                }
            }
            // vertical processing
            for(int k = 0; k < 3; k++)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    float[] vx = new float[bmp.Height];
                    for(int j = 0; j < bmp.Height; j++)
                    {
                        vx[j] = rgbPixels[k,i,j];
                    }
                
                    Process(vx, true, it);
                    for(int j = 0; j < bmp.Height; j++)
                    {
                        rgbPixels[k,i,j] = vx[j];
                    }
                }
            }
            getEnergy();

            //normalization
            float max = float.MinValue;
            float min = float.MaxValue;
            for(int k = 0; k < 3; k++)
            {
                for(int j = 0; j < bmp.Height; j++)
                {
                    for(int i = 0; i < bmp.Width; i++)
                    {
                        if(rgbPixels[k,i,j] > max){max = rgbPixels[k,i,j];}
                        if(rgbPixels[k,i,j] < min){min = rgbPixels[k,i,j];}
                    }
                }
            }

            Console.WriteLine("Wavelet " + min + " "+max);

            //display
            max -= min;
            if(max == 0.0f){max = 1.0f;}
            for(int j = 0; j < bmp.Height; j++)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    int vaR = (int)(NormalizedColor.NormalizeValue * (rgbPixels[0, i, j] - min) / max);
                    int vaG = (int)(NormalizedColor.NormalizeValue * (rgbPixels[1, i, j] - min) / max);
                    int vaB = (int)(NormalizedColor.NormalizeValue * (rgbPixels[2, i, j] - min) / max);
                    Color col = Color.FromArgb(vaR, vaG, vaB);
                    bmp.SetPixel(i,j,col);                  
                }
            }
            image = bmp;
        } 
        


        /// <summary>
        /// Process Black and white bitmap
        /// </summary>
        /// <param name="bmp">Bitmap to process</param>
        /// <param name="it">iterator</param>
        private void ProcessBW(Bitmap bmp,int it)
        {
            mVisData = bwPixels;
            // horizontal processing
            for(int j = 0; j < bmp.Height; j++)
            {
                float[] vx = new float[bmp.Width];
                for(int i = 0; i < bmp.Width; i++)
                {
                    vx[i] = bwPixels[i,j];
                }
                Process(vx, true, it);
                for(int i = 0; i < bmp.Width; i++)
                {
                    bwPixels[i,j] = vx[i];
                }
            }
    
            // vertical processing
            for(int i = 0; i < bmp.Width; i++)
            {
                float[] vx = new float[bmp.Height];
                for(int j = 0; j < bmp.Height; j++)
                {
                    vx[j] = bwPixels[i,j];
                }
                
                Process(vx, true, it);
                for(int j = 0; j < bmp.Height; j++)
                {
                    bwPixels[i,j] = vx[j];
                }
            }

            //normalization
            float max = float.MinValue;
            float min = float.MaxValue;

            for(int j = 0; j < bmp.Height; j++)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    if(bwPixels[i,j] > max){max = bwPixels[i,j];}
                    if(bwPixels[i,j] < min){min = bwPixels[i,j];}
                }
            }
            
            Console.WriteLine("Wavelet " + min + " " + max);

            //display
            max -= min;
            if(max == 0.0f){ max = 1.0f; }
            
            for(int j = 0; j < bmp.Height; j++)
            {
                for(int i = 0; i < bmp.Width; i++)
                {
                    int va = (int)(NormalizedColor.NormalizeValue * (bwPixels[i, j] - min) / max);
                    Color col = Color.FromArgb(va, va, va);
                    if(va > 250)
                    {
                        col = Color.FromArgb(255,0,0);
                        Console.WriteLine(j + " " + i);
                    }
                    bmp.SetPixel(i, j, col);                  
                }
            }
            image = bmp;
        }



        /// <summary>
        /// Get the data
        /// </summary>
        public object data
        {
            get {return mVisData;}
        }
        object mVisData = null;

        private float []mData;

        /// <summary>
        /// indexer to the (float array) data
        /// </summary>
        public float this[int idx] 
        {
            get{return mData[idx-1];}
            set{mData[idx-1]=value;}
        }
            
        const int MAXIT=4;
    
        /// <summary>
        /// Process some data
        /// </summary>
        /// <param name="data">data to be processes</param>
        /// <param name="forward">forward</param>
        /// <param name="nit">number of iteration</param>
        public void Process(float []data,bool forward,int nit)
        {
            if(data!=null && nit>0)
            {
                mData=data;
                if(forward==true)
                {
                    int nn=data.Length;
                    for(int j=0;j<nit;j++)
                    {
                        daubechy4(forward,nn);
                        nn>>=1;
                    }
                }
                else 
                {
                    int nn=data.Length/(int)Math.Pow(2,nit-1);
                    for(int j=0;j<nit;j++)
                    {
                        daubechy4(forward,nn);
                        nn<<=1;
                    }
                }
            }
        }
    
        /// <summary>
        /// The bitmap image
        /// </summary>
        public Bitmap image = null;
        /// <summary>
        /// The Data representing the color image
        /// </summary>
        public float[,,] rgbPixels =null;
        /// <summary>
        /// Data representing the black and white image
        /// </summary>
        public float[,]  bwPixels  =null;

        const int MVECT=128;
        /// <summary>
        /// Process the data
        /// </summary>
        /// <param name="data">data to be processed</param>
        /// <param name="forward">???</param>
        public void Process(float []data,bool forward)
        {
            if(data!=null)
            {
                mData=data;
                int nn;
                int n=data.Length;
                if(n<MVECT){return;}
                if(forward==true)
                {
                    for(nn=n;nn>=MVECT;nn>>=1)
                    {
                        daubechy4(forward,nn);
                    }
                }
                else
                {
                    for(nn=MVECT;nn<=n;nn<<=1)
                    {
                        daubechy4(forward,nn);
                    }
                }       
            }
        }
    
        private void daubechy4(bool forward,int n)
        {
            if(mData !=null)
            {
                float []wksp=null;
                float []a = mData;
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

                Wavelet lw = this; // VS.net does not like cxx*this[yy]
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
        }
    }
}
