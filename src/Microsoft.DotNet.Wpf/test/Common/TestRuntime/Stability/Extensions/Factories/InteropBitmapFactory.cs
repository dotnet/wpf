// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(InteropBitmap))]
    class InteropBitmapFactory : DiscoverableFactory<InteropBitmap>
    {
        #region Private Data

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat Format { get; set; }

        public static readonly DependencyProperty HandleProperty =
            DependencyProperty.RegisterAttached("Handle", typeof(SafeFileHandle), typeof(InteropBitmap), new PropertyMetadata());

        #endregion

        #region Override Members

        public override InteropBitmap Create(DeterministicRandom random)
        {
            int width = random.Next(400) + 1;
            int height = random.Next(400) + 1;
            int bytesPerPixel = (int)((Format.BitsPerPixel + 7.0) / 8.0);
            uint bufferSizeInBytes = (uint)(width * height * bytesPerPixel);
            int stride = (int)(width * bytesPerPixel);

            IntPtr fileHandle = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, PAGE_READWRITE, 0, bufferSizeInBytes, null);

            if (fileHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not create a file mapping for the Interop Bitmap");
            }
            
            unsafe
            {
                IntPtr viewHandle = MapViewOfFile(fileHandle, FILE_MAP_ALL_ACCESS, 0, 0, bufferSizeInBytes);
                byte* pixels = (byte*)viewHandle.ToPointer();

                DrawToBitmap(pixels, bufferSizeInBytes, random);

                UnmapViewOfFile(viewHandle);
            }

            InteropBitmap bitmap = (InteropBitmap)System.Windows.Interop.Imaging.CreateBitmapSourceFromMemorySection(fileHandle, width, height, Format, stride, 0);

            // store the file handle as a dependency property, so that the InteropBitmap action has access to it
            // and so that when the InteropBitmap is garbage collected, the handle will still be closed
            SafeFileHandle section = new SafeFileHandle(fileHandle, true);
            bitmap.SetValue(HandleProperty, section);

            return bitmap;
        }

        #endregion

        #region Private Members

        private unsafe void DrawToBitmap(byte* buffer, uint size, DeterministicRandom random)
        {
            for (uint i = 0; i < size; i++)
            {
                buffer[i] = unchecked((byte)random.Next());
            }
        }

        #endregion

        #region Static Members

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile,
                                                       IntPtr lpFileMappingAttributes,
                                                       uint flProtect,
                                                       uint dwMaximumSizeHigh,
                                                       uint dwMaximumSizeLow,
                                                       string lpName);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
                                                   uint dwDesiredAccess,
                                                   uint dwFileOffsetHigh,
                                                   uint dwFileOffsetLow,
                                                   uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        // Windows constants
        public const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        public const uint PAGE_READWRITE = 0x04;
        public readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        #endregion
    }
}
