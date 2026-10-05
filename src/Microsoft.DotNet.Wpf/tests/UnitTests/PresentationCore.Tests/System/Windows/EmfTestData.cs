// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace System.Windows;

// Creates deterministic EMF/WMF payloads and validates native records without relying on external test assets.
internal static class EmfTestData
{
    internal static Rectangle Frame { get; } = new(0, 0, 120, 80);

    // Records a rectangle into an in-memory EMF, then transfers ownership of an independent HENHMETAFILE.
    public static Metafile CreateMetafile()
    {
        using MemoryStream stream = new();
        using Bitmap referenceBitmap = new(1, 1);
        using Graphics referenceGraphics = Graphics.FromImage(referenceBitmap);

        Metafile recording;
        nint hdc = referenceGraphics.GetHdc();
        try
        {
            recording = new(
                stream,
                hdc,
                Frame,
                MetafileFrameUnit.Pixel,
                EmfType.EmfOnly);
        }
        finally
        {
            referenceGraphics.ReleaseHdc(hdc);
        }

        using (recording)
        {
            using Graphics graphics = Graphics.FromImage(recording);
            nint recordingHdc = graphics.GetHdc();
            try
            {
                Assert.NotEqual(0, GdiRectangle(recordingHdc, 10, 10, 110, 62));
            }
            finally
            {
                graphics.ReleaseHdc(recordingHdc);
            }
        }

        using MemoryStream source = new(stream.ToArray(), writable: false);
        using Metafile reader = new(source);
        nint handle = reader.GetHenhmetafile();
        if (handle == 0)
        {
            throw new Win32Exception();
        }

        try
        {
            return new Metafile(handle, deleteEmf: true);
        }
        catch
        {
            DeleteEnhMetaFile(handle);
            throw;
        }
    }

    // Copies the native EMF bits. GetHenhmetafile transfers a handle that this method must release.
    public static byte[] GetBytes(Metafile metafile)
    {
        nint handle = metafile.GetHenhmetafile();
        if (handle == 0)
        {
            throw new Win32Exception();
        }

        bool completed = false;
        try
        {
            uint size = GetEnhMetaFileBits(handle, 0, null);
            if (size == 0)
            {
                throw new Win32Exception();
            }

            byte[] bytes = new byte[size];
            if (GetEnhMetaFileBits(handle, size, bytes) != size)
            {
                throw new Win32Exception();
            }

            completed = true;
            return bytes;
        }
        finally
        {
            bool deleted = DeleteEnhMetaFile(handle);
            if (completed)
            {
                Assert.True(deleted);
            }
        }
    }

    // The production .NET 9 path calls MemoryStream.GetBuffer, so public buffer visibility changes behavior.
    public static MemoryStream CreateNonExposableStream(byte[] bytes) =>
        new(bytes, 0, bytes.Length, writable: false, publiclyVisible: false);

    public static MemoryStream CreateExposableStream(byte[] bytes) =>
        new(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);

    // GetBuffer returns the full capacity, not Length. This shape exposes trailing capacity bytes to production.
    public static MemoryStream CreateOverallocatedExposableStream(byte[] bytes)
    {
        MemoryStream stream = new(bytes.Length + 32);
        stream.Write(bytes);
        stream.Position = 0;
        return stream;
    }

    // Converts the deterministic EMF to WMF bits and adds the placeable header required by GDI+ stream loading.
    public static byte[] CreateWmfBytes()
    {
        using Metafile emf = CreateMetafile();
        nint emfHandle = emf.GetHenhmetafile();
        if (emfHandle == 0)
        {
            throw new Win32Exception();
        }

        using Bitmap referenceBitmap = new(1, 1);
        using Graphics referenceGraphics = Graphics.FromImage(referenceBitmap);
        nint referenceHdc = 0;
        bool completed = false;
        try
        {
            referenceHdc = referenceGraphics.GetHdc();
            const int MmAnisotropic = 8;
            uint size = GetWinMetaFileBits(emfHandle, 0, null, MmAnisotropic, referenceHdc);
            if (size == 0)
            {
                throw new Win32Exception();
            }

            byte[] wmfBits = new byte[size];
            if (GetWinMetaFileBits(emfHandle, size, wmfBits, MmAnisotropic, referenceHdc) != size)
            {
                throw new Win32Exception();
            }

            byte[] result = AddPlaceableWmfHeader(wmfBits);
            completed = true;
            return result;
        }
        finally
        {
            try
            {
                if (referenceHdc != 0)
                {
                    referenceGraphics.ReleaseHdc(referenceHdc);
                }
            }
            finally
            {
                bool deleted = DeleteEnhMetaFile(emfHandle);
                if (completed)
                {
                    Assert.True(deleted);
                }
            }
        }
    }

    // Verifies structure and meaningful drawing records rather than accepting any non-null Metafile.
    public static void AssertValid(Metafile metafile)
    {
        MetafileHeader header = metafile.GetMetafileHeader();
        Assert.True(header.IsEmfOrEmfPlus());
        Assert.True(header.Bounds.Width > 0);
        Assert.True(header.Bounds.Height > 0);

        List<EmfPlusRecordType> records = [];
        using Bitmap target = new(Frame.Width, Frame.Height);
        using Graphics graphics = Graphics.FromImage(target);
        graphics.EnumerateMetafile(
            metafile,
            System.Drawing.Point.Empty,
            (recordType, _, _, _, _) =>
            {
                records.Add(recordType);
                return true;
            });

        Assert.Contains(EmfPlusRecordType.EmfHeader, records);
        Assert.Contains(EmfPlusRecordType.EmfRectangle, records);
        Assert.Contains(EmfPlusRecordType.EmfEof, records);
    }

    // Prepends an Aldus placeable header and computes its XOR checksum over the first ten WORDs.
    private static byte[] AddPlaceableWmfHeader(byte[] wmfBits)
    {
        const int HeaderSize = 22;
        byte[] result = new byte[HeaderSize + wmfBits.Length];
        Span<byte> header = result.AsSpan(0, HeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x9AC6CDD7);
        BinaryPrimitives.WriteInt16LittleEndian(header[6..], (short)Frame.Left);
        BinaryPrimitives.WriteInt16LittleEndian(header[8..], (short)Frame.Top);
        BinaryPrimitives.WriteInt16LittleEndian(header[10..], (short)Frame.Right);
        BinaryPrimitives.WriteInt16LittleEndian(header[12..], (short)Frame.Bottom);
        BinaryPrimitives.WriteUInt16LittleEndian(header[14..], 1440);

        ushort checksum = 0;
        for (int offset = 0; offset < 20; offset += sizeof(ushort))
        {
            checksum ^= BinaryPrimitives.ReadUInt16LittleEndian(header[offset..]);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(header[20..], checksum);
        wmfBits.CopyTo(result, HeaderSize);
        return result;
    }

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteEnhMetaFile(nint hEnhMetaFile);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern uint GetEnhMetaFileBits(nint hEnhMetaFile, uint bufferSize, [Out] byte[]? buffer);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern uint GetWinMetaFileBits(
        nint hEnhMetaFile,
        uint bufferSize,
        [Out] byte[]? buffer,
        int mapMode,
        nint referenceHdc);

    [DllImport("gdi32.dll", EntryPoint = "Rectangle", SetLastError = true)]
    private static extern int GdiRectangle(nint hdc, int left, int top, int right, int bottom);
}
