// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region usings
        using System;
        using System.Threading; 
        using System.Reflection;
        using System.Drawing;
        using System.Collections;
        using System.IO;
    #endregion usings

    /// <summary>
    /// Summary description for TypographerBroker.
    /// </summary>
    public sealed class TypographerBroker
    {
        #region Properties
            #region Private Properties
                private static ITypographer _typograph = StandardTypographer.Instance;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// returns the Typograph singleton
                /// </summary>
                public static ITypographer Instance
                {
                    get
                    {
                        return _typograph;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private TypographerBroker() { } // block instantiation
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Reflection based loader for custom typographers supplied by the end user
                /// </summary>
                public static void LoadTypographer(string assemblyToLoad, string type)
                {
                    Assembly asm = Assembly.Load(assemblyToLoad);
                    Type tpgr = asm.GetType(type);
                    if (tpgr == null) { throw new ApplicationException("Specified type '" + type + "' not found in assembly '" + assemblyToLoad + "'"); }

                    ITypographer ltypo = (ITypographer)tpgr.InvokeMember("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty, null, null, null);
                    if (ltypo == null) { throw new ApplicationException("Unable to create the specified typographer ('" + type + "." + assemblyToLoad + "')"); }

                    _typograph = ltypo;
                }
                /// <summary>
                /// Return the cleartype version of the source (WIP)
                /// </summary>
                public static IImageAdapter ClearType(IImageAdapter imageAdapter)
                {
                    ImageAdapter iaret = null;

                    if (imageAdapter != null)
                    {
                        iaret = new ImageAdapter(imageAdapter.Width, imageAdapter.Height);
                        for (int j = 0; j < imageAdapter.Height; j++)
                        {
                            for (int i = 1; i < imageAdapter.Width - 1; i++)
                            {
                                IColor lcm = imageAdapter[i - 1, j];
                                IColor lc = imageAdapter[i, j];
                                IColor lcp = imageAdapter[i + 1, j];
                                int r = lcm.R + (int)Math.Abs(lc.R - 255);
                                iaret[i - 1, j].ARGB = unchecked((int)(0xFF000000 + (r << 16) + (lcm.G) << 8 + lcm.B));
                                int b = lcp.B + (int)Math.Abs(lc.B - 255);
                                iaret[i + 1, j].ARGB = unchecked((int)(0xFF000000 + (lcp.R << 16) + (lcp.G << 8) + b));
                            }
                        }
                    }
                    return iaret;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
