// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;


    /// <summary>
    /// Fast-Fourrier-Transform based descriptor
    /// </summary>
    public class FFTExtDescriptor : ITransform
    {
        #region Private data.

        private Descriptor _descriptor = null;
        
        #endregion Private data.

        #region Constructor

        /// <summary>
        /// Creates a new MomentTransform instance for the specified 
        /// descriptor.
        /// </summary>
        /// <param name="descriptor">Descriptor for the transform. </param>
        public FFTExtDescriptor (Descriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }
            _descriptor = descriptor;
        }

        #endregion Constructor

        #region ITransform Implementation

        /// <summary>
        /// compute the descriptor 
        /// </summary>
        /// <param name="bmp">Bitmap to use</param>
        /// <param name="root">The Lookup table</param>
        public void ComputeDescriptors(Bitmap bmp, ref int[,] root)
        {
            try
            {
                _descriptor.muIdent = new float[Descriptor.MMAX,Descriptor.MMAX];
                _descriptor.muFunct = new float[Descriptor.MMAX,Descriptor.MMAX];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// Compute the distance between descriptors
        /// </summary>
        /// <param name="iTrf">The Descriptor to be compared against</param>
        /// <param name="mode">The type of criteria to use (Shape / Image)</param>
        /// <returns></returns>
        public float DistMatch(ITransform iTrf, DistMode mode)
        {
            FFTExtDescriptor lMtrf = iTrf as FFTExtDescriptor;
            if (iTrf == null)
            {
                throw new ArgumentNullException("iTrf", "transform passed in cannot be null or wrong type "+iTrf.GetType());
            }

            float dist = 0f;
            for (int l = 0; l < Descriptor.MMAX; l++)
            {
                for (int m = 0; m < Descriptor.MMAX; m++)
                {
                    dist += (float)Math.Abs(_descriptor.muFunct[l,m] - _descriptor.muFunct[l,m]);
                }
            }
            return dist;
        }

        #endregion ITransform Implementation
    }
}

