// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontFileLoader : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteFontFileLoader*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteFontFileLoader* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteFontFileLoader*, uint>)Vtbl[1];

            fixed (IDWriteFontFileLoader* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteFontFileLoader*, uint>)Vtbl[2];

            fixed (IDWriteFontFileLoader* handle = &this)
            {
                return function(handle);
            }
        }
    }
}
