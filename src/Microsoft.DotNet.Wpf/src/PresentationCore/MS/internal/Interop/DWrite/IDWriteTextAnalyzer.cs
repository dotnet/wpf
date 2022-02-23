using System;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteTextAnalyzer : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteTextAnalyzer*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteTextAnalyzer* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteTextAnalyzer*, uint>)Vtbl[1];

            fixed (IDWriteTextAnalyzer* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteTextAnalyzer*, uint>)Vtbl[2];

            fixed (IDWriteTextAnalyzer* handle = &this)
            {
                return function(handle);
            }
        }
    }
}
