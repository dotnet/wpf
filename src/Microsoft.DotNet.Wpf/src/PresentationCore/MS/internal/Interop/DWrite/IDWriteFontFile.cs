using System;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontFile : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteFontFile*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteFontFile* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteFontFile*, uint>)Vtbl[1];

            fixed (IDWriteFontFile* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteFontFile*, uint>)Vtbl[2];

            fixed (IDWriteFontFile* handle = &this)
            {
                return function(handle);
            }
        }
    }
}
