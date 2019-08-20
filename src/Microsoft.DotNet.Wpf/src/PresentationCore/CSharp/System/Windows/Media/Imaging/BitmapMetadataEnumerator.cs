// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

// Allow suppression of certain presharp messages
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// An enumerator that iterates over the children of a timeline.
    /// </summary>
    internal struct BitmapMetadataEnumerator : IEnumerator<String>, IEnumerator
    {
        #region External interface

        #region IEnumerator interface

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <value>
        /// The current element in the collection.
        /// </value>
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next element;
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (_fStarted && _current == null)
            {
                return false;
            }

            _fStarted = true;
            
            IntPtr ppStr = IntPtr.Zero;
            Int32 celtFetched = 0;

            try
            {
                int hr = UnsafeNativeMethods.EnumString.Next(
                    _enumeratorHandle,
                    1,
                    ref ppStr,
                    ref celtFetched);

                if (HRESULT.IsWindowsCodecError(hr))
                {
                    _current = null;
                    return false;
                }

                HRESULT.Check(hr);

                if (celtFetched == 0)
                {
                    _current = null;
                    return false;
                }
                else
                {
                    _current = Marshal.PtrToStringUni(ppStr);
                }
            }
            finally
            {
                if (ppStr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(ppStr);
                    ppStr = IntPtr.Zero;
                }           
            }

            return true;
        }
        
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element
        /// in the collection.
        /// </summary>
        public void Reset()
        {
            HRESULT.Check(UnsafeNativeMethods.EnumString.Reset(_enumeratorHandle));

            _current = null;
            _fStarted = false;
        }

        #endregion // Methods

        #endregion // IEnumerator interface

        #region Properties
        
        /// <summary>
        /// The current timeline referenced by this enumerator.
        /// </summary>
        public String Current
        {
            get
            {
                if (_current == null)
                {
                    if (!_fStarted)
                    {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_NotStarted));
                    }
                    else
                    {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException(SR.Get(SRID.Enumerator_ReachedEnd));
                    }
                }

                return _current;
            }
        }

        /// <summary>
        ///
        /// </summary>
        void IDisposable.Dispose()
        {
            // Do nothing - Required by the IEnumerable contract.
        }

        #endregion // Properties

        #endregion // External interface

        #region Internal implementation

        #region Construction

        /// <summary>
        /// Creates an enumerator iterates over the children of the specified container.
        /// </summary>
        /// <param name="metadataHandle">
        /// Handle to a metadata query reader/writer
        /// </param>
        internal BitmapMetadataEnumerator(SafeMILHandle metadataHandle)
        {
            Debug.Assert(metadataHandle != null && !metadataHandle.IsInvalid);

            HRESULT.Check(UnsafeNativeMethods.WICMetadataQueryReader.GetEnumerator(
                metadataHandle,
                out _enumeratorHandle));

            _current = null;
            _fStarted = false;
        }

        #endregion // Construction

        #region Methods

        #endregion // Methods

        #region Data

        private SafeMILHandle _enumeratorHandle;

        private String _current;
        private bool _fStarted;

        #endregion // Data

        #endregion // Internal implementation
    }
}
