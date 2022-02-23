using System;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontCollectionLoader : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteFontCollectionLoader*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteFontCollectionLoader* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteFontCollectionLoader*, uint>)Vtbl[1];

            fixed (IDWriteFontCollectionLoader* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteFontCollectionLoader*, uint>)Vtbl[2];

            fixed (IDWriteFontCollectionLoader* handle = &this)
            {
                return function(handle);
            }
        }
    }
}
