// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    
    /// <summary>
    /// Summary description for FFT.
    /// </summary>
    
    public sealed class FFT
    {
        /// <summary>
        /// Private Constructor
        /// </summary>
        private FFT()
        {
        }

        /// <summary>
        /// internal accessor to the data
        /// </summary>
        private float this[ulong idx]
        {
            get
            {
                return mData[idx-1];
            }
            set
            {
                mData[idx-1]=value;
            }
        }
        private float []mData=null;
        
        /// <summary>
        /// Performs the FFT of real data.
        /// </summary>
        public static void Real(float []data,bool forward)
        {
            if((data != null) && ( (data.Length&(data.Length-1))==0))
            {
                FFT lfft =new FFT();
                lfft.mData=data;

                ulong i,i1,i2,i3,i4,np3;
                double wr,wi,wpr,wpi,wtemp,theta;
                float c1=0.5f,c2,h1r,h1i,h2r,h2i;
                int isign=-1;
                if(forward==true){isign=1;}
                ulong n=(ulong)data.Length;
                
                theta=3.141592653589793/(double)(n>>1);
                if(isign==1)
                {
                    c2=-0.5f;
                    Complex(lfft.mData,true);
                } 
                else
                {
                    c2=0.5f;
                    theta=-theta;
                }
                wtemp=Math.Sin(0.5*theta);
                wpr=-2.0*wtemp*wtemp;
                wpi=Math.Sin(theta);
                wr=1.0+wpr;
                wi=wpi;
                np3=n+3;
                for(i=2;i<=(n>>2);i++)
                {
                    i4=1+(i3=np3-(i2=1+(i1=i+i-1)));
                    h1r=c1*(lfft[i1]+lfft[i3]);
                    h1i=c1*(lfft[i2]-lfft[i4]);
                    h2r=-c2*(lfft[i2]+lfft[i4]);
                    h2i=c2*(lfft[i1]-lfft[i3]);
                    lfft[i1]=(float)( h1r +wr*h2r -wi*h2i);
                    lfft[i2]=(float)( h1i +wr*h2i +wi*h2r);
                    lfft[i3]=(float)( h1r -wr*h2r +wi*h2i);
                    lfft[i4]=(float)(-h1i +wr*h2i +wi*h2r);
                    wr=(wtemp=wr)*wpr-wi*wpi+wr;
                    wi=wi*wpr+wtemp*wpi+wi;
                }
                if(isign==1)
                {
                    lfft[1]=(h1r=lfft[1])+lfft[2];
                    lfft[2]=h1r-lfft[2];
                }
                else
                {
                    lfft[1]=c1*((h1r=lfft[1])+lfft[2]);
                    lfft[2]=c1*(h1r-lfft[2]);
                    Complex(lfft.mData,false);
                }
            }

            else
            {
                if(data ==null)
                {
                    throw new NullReferenceException("data is null");
                } 
                else 
                {
                    throw new Exception("array length is not a power of 2");
                }
            }
        }

        /// <summary>
        /// Performs the FFT of complex data.
        /// </summary>
        public static void Complex(float []data,bool forward)
        {
            if((data != null) && ( (data.Length&(data.Length-1))==0))
            {
                FFT lfft =new FFT();
                lfft.mData=data;

                ulong n,nn,mmax,m,j,i,istep;
                double wtemp,wr,wpr,wpi,wi,theta;
                float tempr,tempi;
                int isign=-1;
                if(forward==true){isign=1;}
                
                nn=(ulong)data.Length/2;
                n=nn<<1;
                j=1;
                for(i=1;i<n;i+=2)
                {
                    if(j>i)
                    {
                        tempr= lfft[j];
                        lfft[j]=lfft[i];
                        lfft[i]=tempr;
                        tempr= lfft[j+1];
                        lfft[j+1]=lfft[i+1];
                        lfft[i+1]=tempr;
                    }
                    m=n>>1;
                    while(m>=2 && j>m)
                    {
                        j-=m;
                        m>>=1;
                    }
                    j+=m;
                }
            
                mmax=2;
                while(n>mmax)
                {
                    istep=mmax<<1;
                    theta=isign*(6.283118530717959/mmax);
                    wtemp=Math.Sin(0.5*theta);
                    wpr= -2.0*wtemp*wtemp;
                    wpi=Math.Sin(theta);
                    wr=1.0;
                    wi=0.0;
                    for(m=1;m<mmax;m+=2)
                    {
                        for(i=m;i<=n;i+=istep)
                        {
                            j=i+mmax;
                            tempr=(float)(wr*lfft[j]-wi*lfft[j+1]);
                            tempi=(float)(wr*lfft[j+1]+wi*lfft[j]);
                            lfft[j]=lfft[i]-tempr;
                            lfft[j+1]=lfft[i+1]-tempi;
                            lfft[i]+=tempr;
                            lfft[i+1]+=tempi;
                        }
                        wtemp=wr;
                        wr=wtemp*wpr-wi*wpi+wr;
                        wi=wi*wpr+wtemp*wpi+wi;
                    }
                    mmax=istep;
                }
            }
            else
            {
                if(data ==null)
                {
                    throw new NullReferenceException("data is null");
                } 
                else 
                {
                    throw new Exception("array length is not a power of 2");
                }
            }
        }
    }
}
