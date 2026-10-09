// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Private.Windows.Ole;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MS.Internal;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Com = Windows.Win32.System.Com;
using HRESULT = Windows.Win32.Foundation.HRESULT;

namespace System.Windows.Ole;

internal sealed unsafe class WpfOleServices : IOleServices
{
    private const string BitmapSourceFormat = "System.Windows.Media.Imaging.BitmapSource";

    // Prevent instantiation
    private WpfOleServices() { }

    public static void EnsureThreadState() => OleServicesContext.EnsureThreadState();

    public static HRESULT GetDataHere(string format, object data, FORMATETC* pformatetc, STGMEDIUM* pmedium)
    {
        TYMED mediumType = (TYMED)pformatetc->tymed;

        // Handle bitmaps.
        if (mediumType.HasFlag(TYMED.TYMED_GDI)
            && format.Equals(DataFormatNames.Bitmap)
            && (SystemDrawingHelper.IsBitmap(data) || data is BitmapSource))
        {
            pmedium->u.hBitmap = GetCompatibleBitmap(data);
            return HRESULT.S_OK;
        }

        // Handle enhanced metafiles.
        if (mediumType.HasFlag(TYMED.TYMED_ENHMF)
            && pmedium->tymed == TYMED.TYMED_ENHMF
            && IsEnhancedMetafileFormat(format))
        {
            pmedium->u.hEnhMetaFile = HENHMETAFILE.Null;

            if (SystemDrawingHelper.IsMetafile(data))
            {
                pmedium->u.hEnhMetaFile = SystemDrawingHelper.GetHandleFromMetafile(data);
                return pmedium->u.hEnhMetaFile.IsNull ? HRESULT.E_FAIL : HRESULT.S_OK;
            }

            if (data is MemoryStream memoryStream)
            {
                byte[] buffer = memoryStream.TryGetBuffer(out ArraySegment<byte> segment)
                    ? segment.AsSpan().ToArray()
                    : memoryStream.ToArray();

                if (buffer.Length == 0)
                {
                    return HRESULT.E_FAIL;
                }

                HENHMETAFILE hemf;
                fixed (byte* bufferPointer = buffer)
                {
                    hemf = PInvokeCore.SetEnhMetaFileBits(checked((uint)buffer.Length), bufferPointer);
                }

                if (hemf.IsNull)
                {
                    return HRESULT.E_FAIL;
                }

                pmedium->u.hEnhMetaFile = hemf;
                return HRESULT.S_OK;
            }

            return HRESULT.DV_E_TYMED;
        }

        return HRESULT.DV_E_TYMED;

        static HBITMAP GetCompatibleBitmap(object data)
        {
            HBITMAP hbitmap = SystemDrawingHelper.GetHBitmap(data, out int width, out int height);

            return hbitmap.IsNull ? HBITMAP.Null : hbitmap.CreateCompatibleBitmap(width, height);
        }
    }

    public static bool IsNativeTymedSupported(string format, TYMED tymed) =>
        IsEnhancedMetafileFormat(format)
        && tymed.HasFlag(TYMED.TYMED_ENHMF);

    public static bool TryGetObjectFromDataObject<T>(
        Com.IDataObject* dataObject,
        string format,
        [NotNullWhen(true)] out T data)
    {
        data = default!;

        TYMED mediumType;
        ushort formatId;

        if (format == DataFormatNames.Bitmap)
        {
            mediumType = TYMED.TYMED_GDI;
            formatId = (ushort)CLIPBOARD_FORMAT.CF_BITMAP;
        }
        else if (format.Equals(DataFormatNames.Emf, StringComparison.OrdinalIgnoreCase))
        {
            mediumType = TYMED.TYMED_ENHMF;
            formatId = (ushort)CLIPBOARD_FORMAT.CF_ENHMETAFILE;
        }
        else if (format.Equals(DataFormatNames.BinaryFormatMetafile, StringComparison.OrdinalIgnoreCase))
        {
            mediumType = TYMED.TYMED_ENHMF;
            formatId = checked((ushort)DataFormats.GetDataFormat(format).Id);
        }
        else
        {
            return false;
        }

        FORMATETC formatEtc = new()
        {
            cfFormat = formatId,
            dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = (uint)mediumType
        };

        HRESULT result = ClipboardRetry.QueryGetData(dataObject, formatEtc);

        if (result.Failed)
        {
            return false;
        }

        result = ClipboardRetry.GetData(dataObject, formatEtc, out STGMEDIUM medium);

        if (result.Failed)
        {
            return false;
        }

        try
        {
            if (mediumType == TYMED.TYMED_GDI)
            {
                // Get the bitmap from the handle of bitmap.
                object bitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    (HBITMAP)(nint)medium.hGlobal,
                    0,
                    Int32Rect.Empty,
                    sizeOptions: null);

                if (bitmap is T t)
                {
                    data = t;
                    return true;
                }
            }
            else
            {
                if (medium.tymed != TYMED.TYMED_ENHMF || medium.u.hEnhMetaFile.IsNull)
                {
                    return false;
                }

                // Get the metafile object form the enhanced metafile handle.
                object metafile = SystemDrawingHelper.GetMetafileFromHemf((HENHMETAFILE)(nint)medium.hGlobal);
                if (metafile is T t)
                {
                    data = t;
                    return true;
                }

                (metafile as IDisposable)?.Dispose();
            }
        }
        finally
        {
            PInvokeCore.ReleaseStgMedium(ref medium);
        }

        return false;
    }

    private static bool IsEnhancedMetafileFormat(string format) =>
        format.Equals(DataFormatNames.Emf, StringComparison.OrdinalIgnoreCase)
        || format.Equals(DataFormatNames.BinaryFormatMetafile, StringComparison.OrdinalIgnoreCase);

    public static bool AllowTypeWithoutResolver<T>()
    {
        // Image is a special case because we are reading bitmaps directly from the SerializationRecord.
        return typeof(T).FullName.Equals("System.Drawing.Image");
    }

    public static void AddMappedFormats(string format, ICollection<string> formats)
    {
        // BitmapSource is a WPF-only synonym that was part of the .NET 9 DataObject mapping. Keep it in the WPF
        // platform hook so the shared WinForms mapping does not advertise a type it cannot reference.
        if (format is DataFormatNames.Bitmap or DataFormatNames.BinaryFormatBitmap)
        {
            formats.Add(BitmapSourceFormat);
        }
        else if (format == BitmapSourceFormat)
        {
            formats.Add(DataFormatNames.Bitmap);
            formats.Add(DataFormatNames.BinaryFormatBitmap);
        }
    }

    public static bool IsValidTypeForFormat(Type type, string format) => format switch
    {
        DataFormatNames.Bitmap or DataFormatNames.BinaryFormatBitmap =>
            type == typeof(BitmapSource) || type.FullName is "System.Drawing.Bitmap" or "System.Drawing.Image",
        DataFormatNames.Emf or DataFormatNames.BinaryFormatMetafile =>
            type.FullName is "System.Drawing.Imaging.Metafile" or "System.Drawing.Image",

        // All else should fall through as valid.
        _ => true
    };

    public static void ValidateDataStoreData(ref string format, bool autoConvert, object data)
    {
        // We do not have proper support for Dibs, so if the user explicitly asked
        // for Dib and provided a Bitmap object we can't convert.  Instead, publish as an HBITMAP
        // and let the system provide the conversion for us.
        if (format == DataFormats.Dib && autoConvert && (SystemDrawingHelper.IsBitmap(data) || data is BitmapSource))
        {
            format = DataFormats.Bitmap;
        }
    }

    public static IComVisibleDataObject CreateDataObject() => new DataObject();

    static HRESULT IOleServices.OleGetClipboard(Com.IDataObject** dataObject) =>
        PInvokeCore.OleGetClipboard(dataObject);

    static HRESULT IOleServices.OleSetClipboard(Com.IDataObject* dataObject) =>
        PInvokeCore.OleSetClipboard(dataObject);

    static HRESULT IOleServices.OleFlushClipboard() =>
        PInvokeCore.OleFlushClipboard();
}
