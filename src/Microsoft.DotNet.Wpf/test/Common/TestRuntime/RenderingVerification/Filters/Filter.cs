// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.ComponentModel;
        using System.Drawing.Imaging;
        using System.Text.RegularExpressions;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// Base Filter definition
    /// </summary>
    public abstract class Filter
    {
        #region Properties
            #region Private Properties
                private Rectangle[] _rects = null;
                private Hashtable _params = new Hashtable();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The clipping rectangle used to expose filtered areas of the image
                /// if the collection is null or empty no clipping is computed (no perf degradation)
                /// intersection are computed with the filtered image so that clipping rectangles
                /// do not have to reside within the image boundaries.
                /// </summary>
                [BrowsableAttribute(false)]
                [DescriptionAttribute("A collection of area on which to apply the filter")]
                public Rectangle[] ClippingRectangles
                {
                    get { return _rects; }
                    set { _rects = value; }
                }

                /// <summary>
                /// indexer of the parameters to the filter  
                /// </summary>
                [BrowsableAttribute(false)]
                [DescriptionAttribute("Indexer to retrieve a Parameter")]
//                [TypeConverterAttribute(typeof(FilterParameterConverter))]
                public FilterParameter this[string name]
                {
                    get { return (FilterParameter)_params[name]; }
                }

                /// <summary>
                /// The parameters to the filter  
                /// </summary>
                [BrowsableAttribute(false)]
                [DescriptionAttribute("Retrieve all parameter applying to this filter")]
                public ICollection Parameters
                {
                    get
                    {
                        return _params.Values;
                    }
                }
                
                /// <summary>
                /// Return the description of this filter
                /// </summary>
                /// <value></value>
                [BrowsableAttribute(false)]
                [DescriptionAttribute("Retrieve all parameter applying to this filter")]
                public abstract string FilterDescription { get;}
                
            #endregion Public Properties
        #endregion Properties
        
        #region Constructors
            /// <summary>
            /// Base Filter constructor
            /// </summary>
            protected Filter()
            {
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods

            #region Public/Protected Methods
                /// <summary>
                /// Filter implementation
                /// The default implementation is identity
                /// </summary>
                /// 
                public IImageAdapter Process(IImageAdapter source)
                {
                    return Process(source, ClippingRectangles);
                }
                /// <summary>
                /// Filter implementation with rectangle clipping effect.
                /// if a rectangle does not cover a pixel in the processed image, 
                /// the pixel keeps its original value;
                /// </summary>
                public IImageAdapter Process(IImageAdapter source, Rectangle[] clippingRectangles)
                {
                    if (source == null)
                    {
                        throw new ArgumentNullException("source","Parameter must be set to a valid instance of the object (null passed in)");
                    }

                    IImageAdapter result = ProcessFilter(source);

                    if (result != null && clippingRectangles != null && clippingRectangles.Length > 0)
                    {
                        int width = source.Width;
                        int height = source.Height;
                        bool[,] tmap = new bool[width, height];
                        Rectangle iadpt = new Rectangle(0, 0, width, height);

                        foreach (Rectangle r in clippingRectangles)
                        {
                            Rectangle intersect = r;

                            r.Intersect(iadpt);
                            for (int j = intersect.Y; j < intersect.Y + intersect.Height; j++)
                            {
                                for (int i = intersect.X; i < intersect.X + intersect.Width; i++)
                                {
                                    tmap[i, j] = true;
                                }
                            }
                        }

                        result = Process(source, tmap);
                    }

                    return result;
                }
                /// <summary>
                /// Filter implementation with rectangle clipping effect.
                /// if a rectangle does not cover a pixel in the processed image, 
                /// the pixel keeps its original value;
                /// </summary>
                public IImageAdapter Process(IImageAdapter source, bool[,] clippingMap)
                {
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "Parameter must be set to a valid instance of the object (null passed in)");
                    }

                    IImageAdapter result = ProcessFilter(source);


                    // filter effect mask to be applied
                    if (result != null && clippingMap != null)
                    {
                        int minWidth = (int)Math.Min(source.Width, clippingMap.GetLength(0));
                        int minHeight = (int)Math.Min(source.Height, clippingMap.GetLength(1));

                        for (int j = 0; j < minHeight; j++)
                        {
                            for (int i = 0; i < minWidth; i++)
                            {
                                if (clippingMap[i, j] == false)
                                {
                                    result[i, j] = source[i, j];
                                }
                            }
                        }
                    }

                    return result;
                }
                /// <summary>
                /// Filter implementation with rectangle clipping effect.
                /// if a rectangle does not cover a pixel in the processed image, 
                /// the pixel keeps its original value;
                /// </summary>
                protected void AddParameter(FilterParameter parameter)
                {
                    if (parameter == null)
                    {
                        throw new ArgumentNullException("parameter", "Parameter must be a valid instance of FilterParameter (null passed in)");
                    }

                    if ( ! _params.Contains(parameter.Name))
                    {
                        _params.Add(parameter.Name, parameter);
                    }
                    else
                    {
                        _params[parameter.Name] = parameter;
                    }
                }
            #endregion Public/Protected Methods

            #region Override Methods
                /// <summary>
                /// Return the Filter name
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return Regex.Replace(this.GetType().ToString(), @".*\.(.*)", "($1)");
                }
            #endregion Override Methods

            #region Protected Virtual Methods
                /// <summary>
                /// Filter implementation
                /// The default implementation is identity
                /// </summary>
                protected virtual IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    return (IImageAdapter)source.Clone();
                }
            #endregion Protected Methods

            #region Internal Methods
            #endregion Internal Methods
        #endregion Methods
    
    }
}
