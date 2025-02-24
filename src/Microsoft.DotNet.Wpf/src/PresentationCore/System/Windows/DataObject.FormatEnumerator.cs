// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Win32;
using System.Runtime.InteropServices.ComTypes;

namespace System.Windows;

public sealed partial class DataObject
{
    #region FormatEnumerator Class

    /// <summary>
    /// IEnumFORMATETC implementation for DataObject.
    /// </summary>
    private class FormatEnumerator : IEnumFORMATETC
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new enumerator instance.
        internal FormatEnumerator(DataObject dataObject)
        {
            FORMATETC temp;
            string[] formats;

            formats = dataObject.GetFormats();
            _formats = new FORMATETC[formats == null ? 0 : formats.Length];

            if (formats != null)
            {
                for (int i = 0; i < formats.Length; i++)
                {
                    string format;

                    format = formats[i];
                    temp = new FORMATETC
                    {
                        cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                        dwAspect = DVASPECT.DVASPECT_CONTENT,
                        ptd = IntPtr.Zero,
                        lindex = -1
                    };

                    if (IsFormatEqual(format, DataFormats.Bitmap))
                    {
                        temp.tymed = TYMED.TYMED_GDI;
                    }
                    else if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
                    {
                        temp.tymed = TYMED.TYMED_ENHMF;
                    }
                    else
                    {
                        temp.tymed = TYMED.TYMED_HGLOBAL;
                    }

                    _formats[i] = temp;
                }
            }
        }

        // Copy constructor.  Used by the Clone method.
        private FormatEnumerator(FormatEnumerator formatEnumerator)
        {
            _formats = formatEnumerator._formats;
            _current = formatEnumerator._current;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // IEnumFORMATETC.Next implementation.
        public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
        {
            int fetched = 0;

            if (rgelt == null)
            {
                return NativeMethods.E_INVALIDARG;
            }

            for (int i = 0; i < celt && _current < _formats.Length; i++)
            {
                rgelt[i] = _formats[this._current];
                _current++;
                fetched++;
            }

            if (pceltFetched != null)
            {
                pceltFetched[0] = fetched;
            }

            return (fetched == celt) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
        }

        // IEnumFORMATETC.Skip implementation.
        public int Skip(int celt)
        {
            // Make sure we don't overflow on the skip.
            _current = _current + (int)Math.Min(celt, Int32.MaxValue - _current);

            return (_current < _formats.Length) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
        }

        // IEnumFORMATETC.Reset implementation.
        public int Reset()
        {
            _current = 0;
            return NativeMethods.S_OK;
        }

        // IEnumFORMATETC.Clone implementation.
        public void Clone(out IEnumFORMATETC ppenum)
        {
            ppenum = new FormatEnumerator(this);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // List of FORMATETC to enumerate.
        private readonly FORMATETC[] _formats;

        // Current offset of the enumerator.
        private int _current;

        #endregion Private Fields
    }

    #endregion FormatEnuerator Class
}
