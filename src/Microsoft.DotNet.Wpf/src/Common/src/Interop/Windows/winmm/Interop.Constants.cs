// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class WinMM
    {
        public const int WAVE_FORMAT_PCM = 0x0001;
        public const int WAVE_FORMAT_ADPCM = 0x0002;
        public const int WAVE_FORMAT_IEEE_FLOAT = 0x0003;

        public const int MMIO_READ = 0x00000000;
        public const int MMIO_ALLOCBUF = 0x00010000;
        public const int MMIO_FINDRIFF = 0x00000020;

        public const int SND_SYNC = 0000;
        public const int SND_ASYNC = 0x0001;
        public const int SND_NODEFAULT = 0x0002;
        public const int SND_MEMORY = 0x0004;
        public const int SND_LOOP = 0x0008;
        public const int SND_PURGE = 0x0040;
        public const int SND_FILENAME = 0x00020000;
        public const int SND_NOSTOP = 0x0010;
    }
}
