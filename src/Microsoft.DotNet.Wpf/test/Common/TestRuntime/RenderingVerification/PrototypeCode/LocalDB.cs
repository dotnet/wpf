// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Collections;

    /// <summary>
    /// Provides a descriptor repository for the VScan engine.
    /// </summary>
    public class LocalDB
    {
        #region Private data.

        private Hashtable _htSymb = new Hashtable();

        #endregion Private data.

        /// <summary>
        /// Adds the specified Descriptor instance to the repository. 
        /// </summary>
        /// <param name="descriptor">Instance to add.</param>
        public void Insert(Descriptor descriptor)
        {
            if (descriptor != null)
            {
/*
                MomentTransform transform = new MomentTransform(descriptor);
                transform.ComputeDescriptors(ref _bmp, ref _root);
*/
                if (_htSymb.Contains(descriptor))
                {
                    _htSymb[descriptor] = descriptor.TagName;
                }
                else
                {
                    _htSymb.Add(descriptor, descriptor.TagName);
                }
            }
        }

        /// <summary>
        /// Retrieves the tag associated with the specified Descriptor.
        /// </summary>
        /// <param name="descriptor">Descriptor sought.</param>
        /// <returns>
        /// The tag associated with the specified Descriptor.
        /// If the descriptor is null or not found, an empty string
        /// is returned.
        /// </returns>
        public string Search(Descriptor descriptor)
        {
            string str = "";
                
            if (descriptor != null)
            {
                ITransform transform = descriptor.ExtendedDescriptor;
                {
                    float dist = float.MaxValue;
                    Descriptor tgt = null;
                    string lstr = "";
                            
                    foreach (Descriptor descr in _htSymb.Keys)
                    {
                        float ldist = transform.DistMatch(descriptor.ExtendedDescriptor,DistMode.Image);
                                                
                        if (ldist < dist)
                        {
                            dist = ldist;
                            lstr = (string)_htSymb[descr] + "  (" + dist + ")";
                            tgt = descr;
                        }
                    }

                    if (tgt != null)
                    {
                        float sh = descriptor.BoundingBoxArea / tgt.BoundingBoxArea;
                        if (sh > 1.0)
                        {
                            sh = 1.0f / sh;
                        }
                        lstr += " r(" + sh + ")";
                        str += lstr + "\n";
                    }
                }
            }
            return str;
        }
    }
}
