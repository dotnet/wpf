// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

using MS.Utility;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///		Collection of the stylus devices that are available on the tablet.
    /// </summary>
    public class StylusButtonCollection : ReadOnlyCollection<StylusButton>
    {
        /////////////////////////////////////////////////////////////////////

        internal StylusButtonCollection(StylusButton[] buttons)
            : base(new List<StylusButton>(buttons))
        {
        }

        internal StylusButtonCollection(List<StylusButton> buttons)
            : base(buttons)
        {
        }

        /// <summary>
        /// Returns the first StylusButton in the collection with a Guid property
        /// that matches the specified guid.  Returns null if no matching StylusButton is found
        /// </summary>
        /// <param name="guid">guid</param>
        public StylusButton GetStylusButtonByGuid(Guid guid)
        {
            for (int x = 0; x < this.Count; x++)
            {
                if (this[x].Guid == guid)
                {
                    return this[x];
                }
            }
            return null;
        }
    }
}
