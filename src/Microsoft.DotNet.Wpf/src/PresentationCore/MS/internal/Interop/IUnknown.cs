using System;

namespace MS.Internal.Interop
{
    internal unsafe interface IUnknown
    {
        int QueryInterface(Guid* guid, void** comObject);

        uint AddReference();

        uint Release();
    }
}
