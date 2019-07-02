// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region using
        using System;
        using System.Collections;
    #endregion using

    /// <summary>
    /// Holder for all points (Mismatching or not) resulting of a comparison
    /// </summary>
    public class MismatchingPoints
    {
        #region Constants
            private const int ChannelMax = 256;
        #endregion Constants

        #region Properties
            #region Private Properties
                ArrayList[] _channels = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Return the list of Points associated with this error channel
                /// </summary>
                /// <param name="index"></param>
                /// <returns></returns>
                public ArrayList this[int index]
                {
                    get
                    {
                        if (index < 0 || index >= ChannelMax)
                        {
                            throw new ArgumentOutOfRangeException("index", index.ToString(), "value passed in must be between 0 and " + (ChannelMax - 1));
                        }
                        return _channels[index];
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Contructors
            /// <summary>
            /// Create a instance of the MismatchingPoints object.
            /// </summary>
            public MismatchingPoints()
            {
                _channels = new ArrayList[ChannelMax];
                for (int t = 0; t < ChannelMax; t++)
                {
                    _channels[t] = new ArrayList();
                }
            }
        #endregion Contructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Given a difference level (0 to 255), this method computes the number of pixels that
                /// have difference levels greater than or equal to the input difference level.
                /// </summary>
                /// <param name="level">The input difference level.</param>
                /// <returns>Number of pixels having difference levels greater than or equal to the input difference level.</returns>
                public int NumMismatchesAboveLevel(int level)
                {
                    if (level < 0 || level >= ChannelMax)
                    {
                        throw new ArgumentOutOfRangeException("index", level.ToString(), "value passed in must be between 0 and " + (ChannelMax - 1));
                    }

                    int count = 0;
                    for (int i = level; i < ChannelMax; i++)
                    {
                        count += _channels[i].Count;
                    }

                    return count;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
