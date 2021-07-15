// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class WinMM
    {
        [StructLayout(LayoutKind.Sequential)]
        public class WAVEFORMATEX
        {
            internal short wFormatTag;
            internal short nChannels;
            internal int nSamplesPerSec;
            internal int nAvgBytesPerSec;
            internal short nBlockAlign;
            internal short wBitsPerSample;
            internal short cbSize;
        }
    }
}
