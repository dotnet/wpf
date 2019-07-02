// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
    #endregion using

    /// <summary>
    /// Summary description for VMorph.
    /// </summary>
    /// 
    public class WVMorph
    {
        /// <summary>
        /// Instanciate a WVMorph instance
        /// </summary>
        /// <param name="nbx">?</param>
        /// <param name="nby">?</param>
        /// <param name="fsrc">?</param>
        /// <param name="ftgt">?</param>
        public WVMorph(int nbx, int nby, string fsrc, string ftgt)
        {
            nbCtrlPntX = nbx;
            nbCtrlPntY = nby;
            mDef = new PointF[nbCtrlPntX,nbCtrlPntY];
            for(int j=0;j<nbCtrlPntY;j++)
            {
                for(int i=0;i<nbCtrlPntX;i++)
                {
                    mDef[i,j]=new PointF((float)i/(nbCtrlPntX-1),(float)j/(nbCtrlPntY-1));
                }
            }
            
            src = new Bitmap(fsrc);
            tgt = new Bitmap(ftgt);
        
#if Synth
            for(int j=0;j<tgt.Height;j++)
            {
                for(int i=0;i<tgt.Width;i++)
                {
                    if((i+j)%2==0)src.SetPixel(i,j,Color.Black);
                    else src.SetPixel(i,j,Color.Red);
                }
            }

            float iy=0.0f;
            for(int j=0;j<tgt.Height;j++)
            {
                float ix=0.0f;
                for(int i=0;i<tgt.Width;i++)
                {
                    tgt.SetPixel(i,j,Interp(ix,iy,src));
                    ix+=0.1f;
                }
                iy+=0.1f;
            }
#endif
        }

        /// <summary>
        /// The source image
        /// </summary>
        public Bitmap src = null;
        /// <summary>
        /// the target image
        /// </summary>
        public Bitmap tgt = null;
        
        int nbCtrlPntX=0;
        int nbCtrlPntY=0;
        

        static float []tInterp = compInterpVect();
        const int SZAR = 1024;
        
        static float[] compInterpVect()
        {
            float []tval=new float[SZAR];
            for(int j=0;j<SZAR;j++)
            {
                tval[j]=(float)(SZAR-1-j)/(SZAR-1);
            }
            return tval;
        } 
        PointF[,] mDef=new PointF[0,0];
        
        /// <summary>
        /// Morph
        /// </summary>
        /// <returns>The morphed bitmap</returns>
        public Bitmap Morph()
        {
            

            mDef[2,2].X/=1.021f;
            mDef[2,2].Y/=1.021f;
            mDef[2,3].X/=1.021f;
            mDef[2,3].Y/=1.021f;
            mDef[3,2].X/=1.021f;
        
            
            Bitmap bmpret = new Bitmap(src.Width,src.Height);
            
            int wi=src.Width;
            int he=src.Height;

            float xinc=(float)mDef.GetUpperBound(1)/wi;
            float yinc=(float)mDef.GetUpperBound(0)/he;
            
            float y=0.0f;
            for(int j=0;j<src.Height;j++)
            {
                float x=0.0f;
                for(int i=0;i<src.Width;i++)
                {
                    PointF np = Interp(x,y,mDef);
                    Color lci = Interp(np.X*wi,np.Y*he,src);
                    bmpret.SetPixel(i,j,lci);
                    x+=xinc;
                }
                y+=yinc;
            }

#if muent
            float mut =IPUtil.Entropy(
                IPUtil.getFBiHistogram(src,src),
                IPUtil.getFHistogram(src),
                IPUtil.getFHistogram(src)
                );

            Console.WriteLine("MUENtr "+mut);

            mut =IPUtil.Entropy(
                IPUtil.getFBiHistogram(src,bmpret),
                IPUtil.getFHistogram(src),
                IPUtil.getFHistogram(bmpret)
                );

            Console.WriteLine("MUENtr "+mut);
#endif
            return bmpret;
        }


        private PointF Interp(float x,float y,PointF[,]tdef)
        {
            PointF vect=new PointF(0.0f,0.0f);
            if(tdef != null)
            {
                if(x>0.0f && y>0.0f && x<tdef.GetUpperBound(0) && y<tdef.GetUpperBound(1))
                {
                    int ix=(int)x;
                    int iy=(int)y;
                    
                    PointF c00 = tdef[ix,  iy];
                    PointF c01 = tdef[ix,  iy+1];
                    PointF c10 = tdef[ix+1,iy];
                    PointF c11 = tdef[ix+1,iy+1];

                    float dx = x-ix;int ddx=(int)(dx*SZAR); 
                    float dy = y-iy;int ddy=(int)(dy*SZAR);

                    float v0 =c00.X*tInterp[ddx]+c10.X*(1.0f-tInterp[ddx]);
                    float v1 =c01.X*tInterp[ddx]+c11.X*(1.0f-tInterp[ddx]);
                    float ivx= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    
                    v0 =c00.Y*tInterp[ddx]+c10.Y*(1.0f-tInterp[ddx]);
                    v1 =c01.Y*tInterp[ddx]+c11.Y*(1.0f-tInterp[ddx]);
                    float ivy= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    
                    vect = new PointF(ivx,ivy); 
                }
            }
            return vect; 
        }

        private Color Interp(float x,float y,Bitmap img)
        {
            Color clr = Color.FromArgb(0,0,0);
            if(img != null)
            {
                if(x>0.0f && y>0.0f && x<img.Width-1 && y<img.Height-1)
                {
                    int ix=(int)x;
                    int iy=(int)y;
                    
                    Color c00 = img.GetPixel(ix,  iy);
                    Color c01 = img.GetPixel(ix,  iy+1);
                    Color c10 = img.GetPixel(ix+1,iy);
                    Color c11 = img.GetPixel(ix+1,iy+1);

                    float dx = x-ix;int ddx=(int)(dx*SZAR); 
                    float dy = y-iy;int ddy=(int)(dy*SZAR);

                    float v0 =c00.A*tInterp[ddx]+c10.A*(1.0f-tInterp[ddx]);
                    float v1 =c01.A*tInterp[ddx]+c11.A*(1.0f-tInterp[ddx]);
                    float iva= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    int cA = (int)iva;

                    v0 =c00.R*tInterp[ddx]+c10.R*(1.0f-tInterp[ddx]);
                    v1 =c01.R*tInterp[ddx]+c11.R*(1.0f-tInterp[ddx]);
                    iva= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    int cR = (int)iva;
                    v0 =c00.G*tInterp[ddx]+c10.G*(1.0f-tInterp[ddx]);
                    v1 =c01.G*tInterp[ddx]+c11.G*(1.0f-tInterp[ddx]);
                    iva= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    int cG = (int)iva;
                    v0 =c00.B*tInterp[ddx]+c10.B*(1.0f-tInterp[ddx]);
                    v1 =c01.B*tInterp[ddx]+c11.B*(1.0f-tInterp[ddx]);
                    iva= (v0*tInterp[ddy]+v1*(1.0f-tInterp[ddy]));
                    int cB = (int)iva;

                    clr = Color.FromArgb(cA,cR,cG,cB);
                }
            }
            return clr; 
        }
    }
}
