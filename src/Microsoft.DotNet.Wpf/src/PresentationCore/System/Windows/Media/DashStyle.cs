// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of the class DashStyle
//
//

using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media
{
    #region DashStyle
    /// <summary>
    /// This class captures the array of dashe and gap lengths and the dash offset.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public partial class DashStyle : Animatable, DUCE.IResource
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        public DashStyle()
        {
        }

        /// <summary>
        ///     Constructor from an array and offset
        /// <param name="dashes">
        ///     The array of lengths of dashes and gaps, measured in Thickness units.
        ///     If the value of dashes is null then the style will be solid
        /// </param>
        /// <param name="offset">
        ///     Determines where in the dash sequence the stroke will start
        /// </param>
        /// </summary>
        public DashStyle(IEnumerable<double> dashes, Double offset)
        {
            Offset = offset;

            if (dashes != null)
            {
                Dashes = new DoubleCollection(dashes);
            }
        }


        #endregion Constructors


        #region Internal Methods
        /// <summary>
        /// Returns the dashes information.
        /// </summary>
        internal unsafe void GetDashData(MIL_PEN_DATA* pData, out double[] dashArray)
        {
            DoubleCollection vDashes = Dashes;
            int count = 0;

            if (vDashes != null)
            {
                count = vDashes.Count;
            }

            unsafe
            {
                pData->DashArraySize = (UInt32)count * sizeof(double);
                pData->DashOffset = Offset;
            }

            if (count > 0)
            {
                dashArray = vDashes._collection.ToArray();
            }
            else
            {
                dashArray = null;
            }
        }
        #endregion Internal Methods
    }
    #endregion
}
