// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.ObjectModel;

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
