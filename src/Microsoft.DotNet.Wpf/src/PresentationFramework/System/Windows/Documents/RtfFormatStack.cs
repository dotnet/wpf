// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf format stack.
//

using System.Collections;
using MS.Internal; // Invariant

namespace System.Windows.Documents
{
    /// <summary>
    /// RtfFormatStack
    /// </summary>
    internal class RtfFormatStack : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal RtfFormatStack() : base(20)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void Push()
        {
            FormatState previousFormatState = Top();
            FormatState formatState;

            formatState = previousFormatState != null ? new FormatState(previousFormatState) : new FormatState();

            Add(formatState);
        }

        internal void Pop()
        {
            Invariant.Assert(Count != 0);

            if (Count > 0)
            {
                RemoveAt(Count - 1);
            }
        }

        internal FormatState Top()
        {
            return Count > 0 ? EntryAt(Count - 1) : null;
        }

        internal FormatState PrevTop(int fromTop)
        {
            int index = Count - 1 - fromTop;

            if (index < 0 || index >= Count)
            {
                return null;
            }

            return EntryAt(index);
        }

        internal FormatState EntryAt(int index)
        {
            return (FormatState)this[index];
        }

        #endregion Internal Methods
    }
}
