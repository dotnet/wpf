// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.ComTypes;

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class DataStore
    {
        #region DataStoreEntry Class

        private class DataStoreEntry
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            public DataStoreEntry(Object data, bool autoConvert, DVASPECT aspect, int index)
            {
                this._data = data;
                this._autoConvert = autoConvert;
                this._aspect = aspect;
                this._index = index;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

            #region Public Properties

            // Data object property.
            public Object Data
            {
                get { return _data; }
                set { _data = value; }
            }

            // Auto convert proeprty.
            public bool AutoConvert
            {
                get { return _autoConvert; }
            }

            // Aspect flag property.
            public DVASPECT Aspect
            {
                get { return _aspect; }
            }

            // Index property.
            public int Index
            {
                get { return _index; }
            }

            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private Object _data;
            private bool _autoConvert;
            private DVASPECT _aspect;
            private int _index;

            #endregion Private Fields
        }

        #endregion DataStoreEntry Class
    }
}
