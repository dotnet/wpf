// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows.Tests;

// Preserve the .NET 9 public EMF export contract for both streams and System.Drawing metafiles.
[Collection(nameof(EnhancedMetafileCollection))]
public class DataObjectEnhancedMetafileTests
{
    // Clipboard contents are global; these cases must not overlap other test collections.
    [CollectionDefinition(nameof(EnhancedMetafileCollection), DisableParallelization = true)]
    public class EnhancedMetafileCollection
    {
    }

    private const short CfEnhMetafile = 14;

    [StaTheory]
    [InlineData(false, TYMED.TYMED_ENHMF)]
    [InlineData(true, TYMED.TYMED_ENHMF)]
    [InlineData(false, TYMED.TYMED_ENHMF | TYMED.TYMED_ISTREAM)]
    [InlineData(true, TYMED.TYMED_ENHMF | TYMED.TYMED_ISTREAM)]
    public void ComDataObject_EnhancedMetafile_ReturnsValidHandle(bool useMetafile, TYMED requestedMedium)
    {
        // Supply identical EMF content as either a raw stream or a System.Drawing metafile.
        byte[] expected = CreateMetafileBytes();
        using MemoryStream stream = new();
        stream.Write(expected);
        stream.Position = 0;
        using Metafile? metafile = useMetafile ? new Metafile(stream) : null;
        ComDataObject dataObject = new DataObject(DataFormats.EnhancedMetafile, (object?)metafile ?? stream);
        FORMATETC format = CreateFormat();
        // Allowing IStream as well must still select exactly ENHMF for this format.
        format.tymed = requestedMedium;

        // Advertising ENHMF must agree with both QueryGetData and the actual native GetData result.
        FORMATETC[] formats = new FORMATETC[1];
        IEnumFORMATETC enumerator = dataObject.EnumFormatEtc(DATADIR.DATADIR_GET);
        int[] fetched = new int[1];
        Assert.Equal(0, enumerator.Next(1, formats, fetched));
        Assert.Equal(1, fetched[0]);
        Assert.Equal(CfEnhMetafile, formats[0].cfFormat);
        Assert.Equal(TYMED.TYMED_ENHMF, formats[0].tymed);
        Assert.Equal(0, dataObject.QueryGetData(ref format));

        STGMEDIUM medium = new() { tymed = TYMED.TYMED_ENHMF };
        try
        {
            dataObject.GetData(ref format, out medium);
            Assert.Equal(TYMED.TYMED_ENHMF, medium.tymed);
            Assert.Null(medium.pUnkForRelease);
            Assert.Equal(expected, ReadMetafileBytes(medium.unionmember));
        }
        finally
        {
            // A regression may mislabel a valid EMF; normalize only cleanup, never the asserted result.
            if (medium.unionmember != IntPtr.Zero && GetEnhMetaFileBits(medium.unionmember, 0, null) != 0)
            {
                medium.tymed = TYMED.TYMED_ENHMF;
            }

            // GetData transfers ownership of the returned handle to the consumer.
            ReleaseStgMedium(ref medium);
            GC.KeepAlive(dataObject);
        }
    }

    [StaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComDataObject_EnhancedMetafile_InvalidPayload_ThrowsCOMException(bool useEmptyStream)
    {
        // Neither an empty expandable stream nor an unsupported object can supply an EMF handle.
        using MemoryStream stream = new();
        ComDataObject dataObject = new DataObject(DataFormats.EnhancedMetafile, useEmptyStream ? stream : new object());
        FORMATETC format = CreateFormat();

        // Native consumers must receive E_FAIL, not success with a null handle.
        COMException exception = Assert.Throws<COMException>(() => dataObject.GetData(ref format, out _));
        Assert.Equal(unchecked((int)0x80004005), exception.HResult);
    }

    [StaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Clipboard_SetDataObject_EnhancedMetafile_NativeConsumerCanRead(bool useMetafile, bool copy)
    {
        // Cover both payload representations with both clipboard ownership/rendering modes.
        byte[] expected = CreateMetafileBytes();
        using MemoryStream stream = new();
        stream.Write(expected);
        stream.Position = 0;
        using Metafile? metafile = useMetafile ? new Metafile(stream) : null;
        DataObject dataObject = new(DataFormats.EnhancedMetafile, (object?)metafile ?? stream);
        dataObject.SetText("Enhanced metafile control");

        try
        {
            // Exercise delayed rendering (copy=false) and OLE clipboard flushing (copy=true).
            Clipboard.SetDataObject(dataObject, copy);
            Assert.True(Clipboard.ContainsData(DataFormats.EnhancedMetafile));
            Assert.Equal("Enhanced metafile control", Clipboard.GetText());
            // Other processes may briefly hold the clipboard open.
            Assert.True(SpinWait.SpinUntil(() => OpenClipboard(IntPtr.Zero), TimeSpan.FromSeconds(5)));
            try
            {
                // Presence alone is insufficient: read the EMF bytes from the clipboard-owned handle.
                Assert.Equal(expected, ReadMetafileBytes(GetClipboardData((uint)CfEnhMetafile)));
            }
            finally
            {
                Assert.True(CloseClipboard());
            }
        }
        finally
        {
            Clipboard.Clear();
            GC.KeepAlive(dataObject);
        }
    }

    // Request clipboard contents as an enhanced metafile for the full content aspect.
    private static FORMATETC CreateFormat() => new()
    {
        cfFormat = CfEnhMetafile,
        dwAspect = DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED.TYMED_ENHMF
    };

    private static byte[] CreateMetafileBytes()
    {
        // Generate a real in-memory EMF so both payload representations contain the same drawing records.
        IntPtr dc = CreateEnhMetaFileW(IntPtr.Zero, null, IntPtr.Zero, null);
        Assert.NotEqual(IntPtr.Zero, dc);
        IntPtr handle;
        bool drawn;
        try
        {
            drawn = Rectangle(dc, 0, 0, 200, 100);
        }
        finally
        {
            handle = CloseEnhMetaFile(dc);
        }

        Assert.NotEqual(IntPtr.Zero, handle);
        try
        {
            Assert.True(drawn);
            return ReadMetafileBytes(handle);
        }
        finally
        {
            Assert.True(DeleteEnhMetaFile(handle));
        }
    }

    private static byte[] ReadMetafileBytes(IntPtr handle)
    {
        Assert.NotEqual(IntPtr.Zero, handle);
        // GDI returns the required buffer size when called with a zero-length destination.
        uint size = GetEnhMetaFileBits(handle, 0, null);
        Assert.NotEqual(0u, size);
        byte[] bytes = new byte[size];
        Assert.Equal(size, GetEnhMetaFileBits(handle, size, bytes));
        return bytes;
    }

    // Keep the public-API tests identical across branches without depending on internal interop projections.
    [DllImport("gdi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEnhMetaFileW(IntPtr dc, string? fileName, IntPtr bounds, string? description);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Rectangle(IntPtr dc, int left, int top, int right, int bottom);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr CloseEnhMetaFile(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern uint GetEnhMetaFileBits(IntPtr handle, uint size, [Out] byte[]? bytes);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteEnhMetaFile(IntPtr handle);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void ReleaseStgMedium(ref STGMEDIUM medium);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetClipboardData(uint format);
}
