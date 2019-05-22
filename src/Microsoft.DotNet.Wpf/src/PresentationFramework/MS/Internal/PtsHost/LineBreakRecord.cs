// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: LineBreakRecord is unmanaged resouce handle to TextLineBreak
//


using System;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Text;
using System.Windows.Media.TextFormatting;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Break record for line - holds decoration information
    // ----------------------------------------------------------------------
    internal sealed class LineBreakRecord : UnmanagedHandle
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      PtsContext - Context
        //      TextLineBreak - Contained line break
        // ------------------------------------------------------------------
        internal LineBreakRecord(PtsContext ptsContext, TextLineBreak textLineBreak) : base(ptsContext)
        {
            _textLineBreak = textLineBreak;
        }

        /// <summary>
        /// Dispose the line break
        /// </summary>
        public override void Dispose()
        {
            if(_textLineBreak != null)
            {
                _textLineBreak.Dispose();
            }

            base.Dispose();
        }

        #region Internal Methods

        /// <summary>
        /// Clones the underlying TextLineBreak
        /// </summary>
        internal LineBreakRecord Clone()
        {
            return new LineBreakRecord(PtsContext, _textLineBreak.Clone());
        }

        internal TextLineBreak TextLineBreak { get { return _textLineBreak; } }


        #endregion Internal Methods


        #region Private Fields
        
        private TextLineBreak _textLineBreak;

        #endregion Private Fields
    }
}

