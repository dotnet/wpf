// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text properties and state at the point where text is broken 
//             by the line breaking process, which may need to be carried over 
//             when formatting the next line.
//
//  Spec:      Text Formatting API.doc
//
//

using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.TextFormatting;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Text properties and state at the point where text is broken
    /// by the line breaking process. 
    /// </summary>
    public sealed class TextLineBreak : IDisposable
    {
        private TextModifierScope                   _currentScope;
        private SecurityCriticalDataForSet<IntPtr>  _breakRecord;

        #region Constructors

        /// <summary>
        /// Internallly construct the line break
        /// </summary>
        internal TextLineBreak(
            TextModifierScope                   currentScope,
            SecurityCriticalDataForSet<IntPtr>  breakRecord
            )
        {
            _currentScope = currentScope;
            _breakRecord = breakRecord;

            if (breakRecord.Value == IntPtr.Zero)
            {
                // this object does not hold unmanaged resource,
                // remove it from the finalizer queue.
                GC.SuppressFinalize(this);
            }
        }

        #endregion


        /// <summary>
        /// Finalize the line break
        /// </summary>
        ~TextLineBreak()
        {
            DisposeInternal(true);
        }


        /// <summary>
        /// Dispose the line break
        /// </summary>
        public void Dispose()
        {
            DisposeInternal(false);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Clone a new instance of TextLineBreak
        /// </summary>
        public TextLineBreak Clone()
        {
            IntPtr pbreakrec = IntPtr.Zero;

            if (_breakRecord.Value != IntPtr.Zero)
            {
                LsErr lserr = UnsafeNativeMethods.LoCloneBreakRecord(_breakRecord.Value, out pbreakrec);

                if (lserr != LsErr.None)
                {
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.CloneBreakRecordFailure, lserr), lserr);
                }
            }

            return new TextLineBreak(_currentScope, new SecurityCriticalDataForSet<IntPtr>(pbreakrec));
        }


        /// <summary>
        /// Destroy LS unmanaged break records object inside the line break 
        /// managed object. The parameter flag indicates whether the call is 
        /// from finalizer thread or the main UI thread.
        /// </summary>
        private void DisposeInternal(bool finalizing)
        {
            if (_breakRecord.Value != IntPtr.Zero)
            {
                UnsafeNativeMethods.LoDisposeBreakRecord(_breakRecord.Value, finalizing);

                _breakRecord.Value = IntPtr.Zero;
                GC.KeepAlive(this);
            }
        }


        /// <summary>
        /// Current text modifier scope, which can be null.
        /// </summary>
        internal TextModifierScope TextModifierScope
        {
            get { return _currentScope; }
        }

        
        /// <summary>
        /// Unmanaged pointer to LS break records structure
        /// </summary>
        internal SecurityCriticalDataForSet<IntPtr> BreakRecord
        {
            get { return _breakRecord; }
        }
    }
}
