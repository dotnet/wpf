using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PresentationCore;

internal static class ThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    internal static void ThrowArgumentNullException(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }
}
