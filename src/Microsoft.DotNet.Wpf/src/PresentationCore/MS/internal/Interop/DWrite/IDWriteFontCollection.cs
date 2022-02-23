using System;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontCollection : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteFontCollection*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteFontCollection* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteFontCollection*, uint>)Vtbl[1];

            fixed (IDWriteFontCollection* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteFontCollection*, uint>)Vtbl[2];

            fixed (IDWriteFontCollection* handle = &this)
            {
                return function(handle);
            }
        }
    }
}
