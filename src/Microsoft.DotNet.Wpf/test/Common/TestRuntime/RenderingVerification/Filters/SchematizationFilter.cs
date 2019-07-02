// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
    #endregion using 

    /// <summary>
    /// Summary description for TFilter.
    /// </summary>
    public class SchematizationFilter
    {
        #region Constants
            private const int OFFSET = 4;
        #endregion Constants

        #region Public Property
            /// <summary>
            /// The cost between to unmatching color
            /// </summary>
            public int Cost = 10;
        #endregion Public Property

        #region Private Properties
            private int _Idx = 0;
            private Bitmap _originalBitmap = null;
            private Bitmap _filteredBitmap = null;
            private ArrayList _colors = new ArrayList();
            private int _width = 0;
            private int _height = 0;
            private Random _rnd =new Random();
        #endregion Private Properties

        #region Constructors
            /// <summary>
            /// Load the image from a file to instantiate a new SchematizationFilter
            /// </summary>
            /// <param name="fileName">The file name of the image</param>
            /// <param name="cost">Specify the cost between unmatching pixel. Can be zero</param>
            public SchematizationFilter(string fileName, int cost) : this( ( (fileName == null) ? null : new Bitmap(fileName) ), cost)
            {
            }
            /// <summary>
            /// Instantiate a new SchematizationFilter from the Image
            /// </summary>
            /// <param name="bmp">The file name of the image</param>
            /// <param name="cost">Specify the cost between unmatching pixel. Can be zero</param>
            public SchematizationFilter(Bitmap bmp, int cost)
            {
                if (bmp != null)
                {
                    _originalBitmap = new Bitmap(bmp);
                    _filteredBitmap = new Bitmap(_originalBitmap);
                    _width = _originalBitmap.Width;
                    _height = _originalBitmap.Height;
                }
                if (cost >= 0)
                {
                    Cost = cost;
                }
            }
            
        #endregion Constructors
  
        #region Methods
            #region Private Methods
                private long ModeCost(Color co,Color cot)
                {
                    long dist = (long)(Math.Abs(co.R - cot.R) + Math.Abs(co.G - cot.G) + Math.Abs(co.B - cot.B));
                    return dist;
                }

                private long GetCost(int i,int j)
                {
                    return GetCost(i, j, _filteredBitmap.GetPixel(i, j));
                }

                private long GetCost(int i, int j, Color col)
                {
                    long cost = ModeCost(_originalBitmap.GetPixel(i,j), col);
                
                    Color co = _filteredBitmap.GetPixel(i-1, j-1);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i-1, j);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i-1, j+1);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i,j-1);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i, j+1);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i+1, j-1);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i+1, j);
                    if( co != col ) { cost += Cost; }
                    co = _filteredBitmap.GetPixel(i+1, j+1);
                    if( co != col ) { cost += Cost; }

                    return cost;
                }

                private Color[] GetColors()
                {
                    Color []tcol = new Color[_colors.Count];
                    for(int j = 0; j < _colors.Count; j++)
                    {
                        tcol[j] = (Color)_colors[j];
                    }
                    return tcol;
                }

            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Reset the Image to the original one
                /// </summary>
                public void Reset()
                {
                    _filteredBitmap = new Bitmap(_originalBitmap);
                }

                /// <summary>
                /// Modify the Bitmap based on the nearby pixels
                /// </summary>
                /// <returns></returns>        
                public float Iterate()
                {
                    float cost = 0;
                    
                    Color []tcol = GetColors();    

                    int locIdx = _Idx % (OFFSET * OFFSET);
                    int xoff = _Idx / OFFSET;
                    int yoff = _Idx % OFFSET;

                    if(_originalBitmap != null && _filteredBitmap != null)
                    {
                        for(int i = xoff + 1; i < _width - 1; i += OFFSET)
                        {
                            for(int j = yoff + 1; j < _height - 1; j += OFFSET)
                            {
                                long dist = GetCost(i,j);
                                int win =- 1;
                                for(int k = 0; k < tcol.Length; k++)
                                {
                                    long ldist = GetCost(i, j, tcol[k]);
                                    if( ldist < dist ) { win = k; dist = ldist; }
                                }
                                if(win >= 0)
                                {
                                    _filteredBitmap.SetPixel(i, j, tcol[win]);
                                    cost += dist;
                                }
                            }
                        }
                    }
                    _Idx++;
                    return cost;
                }

                /// <summary>
                /// Retrieve the bitmap (original or modified)
                /// </summary>
                /// <param name="filteredImage">true will return the filtered image, fasle will return the original one</param>
                /// <returns>The distance to the original</returns>
                public Bitmap GetBitmap(bool filteredImage)
                {
                    Bitmap ret = _originalBitmap;
                    if(filteredImage == true)
                    {
                        ret = _filteredBitmap;
                    }
                    return ret;
                }

                /// <summary>
                /// Binarize the image with the colors picked by the used
                /// </summary>
                public void DoDistribution()
                {
                    Color []tcol = GetColors();
                    int nbcol = tcol.Length;
                    if(_originalBitmap != null && _filteredBitmap != null)
                    {
                        for(int i = 0; i < _width; i++)
                        {
                            for(int j = 0; j < _height; j++)
                            {
                                long dist = long.MaxValue;
                                int win =- 1;
                                for(int k = 0; k < nbcol; k++)
                                {
                                    long ldist = ModeCost(_originalBitmap.GetPixel(i,j), tcol[k]);
                                    if( ldist < dist ) { win = k; dist = ldist; }
                                }
                                _filteredBitmap.SetPixel(i, j, tcol[win]);
                            }
                        }
                    }
                }
                
                /// <summary>
                /// Populate the filtered image with random values.
                /// Better accuracy but slower than DoDistribution
                /// </summary>
                public void DoRandomDistribution()
                {
                    Color[] tcol= GetColors();
                    int nbcol = tcol.Length;
                    if(_originalBitmap != null && _filteredBitmap != null)
                    {
                        for(int i = 0; i < _width; i++)
                        {
                            for(int j = 0; j < _height; j++)
                            {
                                int idx = _rnd.Next(nbcol);
                                _filteredBitmap.SetPixel(i, j, tcol[idx]);
                            }
                        }
                    }
                }

                /// <summary>
                /// Add a color to the list of color to use for the binarization
                /// </summary>
                /// <param name="col">The color to be added</param>
                public void AddColor(Color col)
                {
                    if(!_colors.Contains(col))
                    {
                        _colors.Add(col);
                        
                    }
                }
                
                /// <summary>
                /// Remove a color from the list of color to use for the binarization
                /// </summary>
                /// <param name="col">The color to be removed</param>
                public void RemoveColor(Color col)
                {
                    if(_colors.Contains(col))
                    {
                        _colors.Remove(col);
                    }
                }
                
            #endregion Public Methods
        #endregion Methods
    }
}
