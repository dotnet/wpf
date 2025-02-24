// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using MS.Win32;
using System.Formats.Nrbf;
using System.IO;
using System.Private.Windows.Ole;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Text;
using MS.Internal;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using System.Runtime.CompilerServices;

using Windows.Win32.Graphics.Gdi;

namespace System.Windows;

public sealed partial class DataObject
{
    /// <summary>
    ///  This class handles converting from our managed types to standard OLE clipboard formats.
    /// </summary>
    private partial class OleConverter : IDataObject
    {
        private readonly IComDataObject _innerData;

        public OleConverter(IComDataObject data) => _innerData = data;

        public object? GetData(string format) => GetData(format, autoConvert: true);

        public object? GetData(Type format) => GetData(format.FullName ?? "");

        public object? GetData(string format, bool autoConvert) =>
            GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);

        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);

        public bool GetDataPresent(Type format) => GetDataPresent(format.FullName ?? "");

        public bool GetDataPresent(string format, bool autoConvert) =>
            GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);

        public string[] GetFormats() => GetFormats(autoConvert: true);

        public void SetData(object? data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data is ISerializable)
            {
                SetData(DataFormats.Serializable, data);
            }
            else
            {
                SetData(data.GetType(), data);
            }
        }

        public string[] GetFormats(bool autoConvert)
        {
            IEnumFORMATETC enumFORMATETC = EnumFormatEtcInner(DATADIR.DATADIR_GET);
            List<string> formats = [];

            if (enumFORMATETC is null)
            {
                return GetDistinctStrings(formats);
            }

            FORMATETC[] formatetc = [new FORMATETC()];
            int[] retrieved = [1];

            enumFORMATETC.Reset();

            while (retrieved[0] > 0)
            {
                retrieved[0] = 0;

                if (enumFORMATETC.Next(1, formatetc, retrieved) != NativeMethods.S_OK || retrieved[0] <= 0)
                {
                    continue;
                }

                string name = DataFormats.GetDataFormat(formatetc[0].cfFormat).Name;

                if (autoConvert)
                {
                    string[] mappedFormats = GetMappedFormats(name);

                    for (int i = 0; i < mappedFormats.Length; i++)
                    {
                        formats.Add(mappedFormats[i]);
                    }
                }
                else
                {
                    formats.Add(name);
                }

                // Release the allocated memory by IEnumFORMATETC::Next for DVTARGETDEVICE
                // pointer in the ptd member of the FORMATETC structure.
                // Otherwise, there will be the memory leak.
                for (int formatetcIndex = 0; formatetcIndex < formatetc.Length; formatetcIndex++)
                {
                    if (formatetc[formatetcIndex].ptd != 0)
                    {
                        Marshal.FreeCoTaskMem(formatetc[formatetcIndex].ptd);
                    }
                }
            }

            return GetDistinctStrings(formats);
        }

        public void SetData(string format, object? data) => SetData(format, data, autoConvert: true);

        public void SetData(Type format, object? data) => SetData(format.FullName!, data);

        public void SetData(string format, object? data, bool autoConvert)
        {
            throw new InvalidOperationException(SR.DataObject_CannotSetDataOnAFozenOLEDataDbject);
        }

        /// <summary>
        ///  Returns the data object we are wrapping
        /// </summary>
        public IComDataObject OleDataObject => _innerData;

        private object? GetData(string format, bool autoConvert, DVASPECT aspect, int index)
        {
            object? current;
            object? original;

            current = GetDataFromBoundOleDataObject(format, aspect, index);
            original = current;

            if (!autoConvert
                || (current is not null && current is not MemoryStream)
                || GetMappedFormats(format) is not { } mappedFormats)
            {
                return original;
            }

            for (int i = 0; i < mappedFormats.Length; i++)
            {
                if (format != mappedFormats[i])
                {
                    current = GetDataFromBoundOleDataObject(mappedFormats[i], aspect, index);

                    if (current is not null and not MemoryStream)
                    {
                        if (current is BitmapSource || SystemDrawingHelper.IsBitmap(current))
                        {
                            // Ensure Bitmap(BitmapSource or System.Drawing.Bitmap) data which
                            // match with the requested format.
                            current = EnsureBitmapDataFromFormat(format, autoConvert, current);
                        }

                        original = null;
                        break;
                    }
                }
            }

            return original ?? current;
        }

        private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
        {
            if (GetDataPresentInner(format, aspect, index))
            {
                return true;
            }

            if (autoConvert && GetMappedFormats(format) is { } mappedFormats)
            {
                for (int i = 0; i < mappedFormats.Length; i++)
                {
                    if (format != mappedFormats[i] && GetDataPresentInner(mappedFormats[i], aspect, index))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///  Uses IStream and retrieves the specified format from the bound IComDataObject.
        /// </summary>
        private object? GetDataFromOleIStream(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index,
                tymed = TYMED.TYMED_ISTREAM
            };

            if (NativeMethods.S_OK != QueryGetDataInner(ref formatetc))
            {
                return null;
            }

            GetDataInner(ref formatetc, out STGMEDIUM medium);

            try
            {
                // Check both handle and type of storage medium
                if (medium.unionmember == 0 || medium.tymed != TYMED.TYMED_ISTREAM)
                {
                    return null;
                }

                UnsafeNativeMethods.IStream pStream;

                pStream = (UnsafeNativeMethods.IStream)Marshal.GetObjectForIUnknown(medium.unionmember);

                NativeMethods.STATSTG sstg = new NativeMethods.STATSTG();
                pStream.Stat(sstg, NativeMethods.STATFLAG_DEFAULT);
                int size = (int)sstg.cbSize;

                nint hglobal = Win32GlobalAlloc(
                    NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_DDESHARE | NativeMethods.GMEM_ZEROINIT,
                    size);

                try
                {
                    nint ptr = Win32GlobalLock(new HandleRef(this, hglobal));

                    try
                    {
                        // Seek to the beginning of the stream before reading it.
                        pStream.Seek(0, 0 /* STREAM_SEEK_SET */);
                        pStream.Read(ptr, size);
                    }
                    finally
                    {
                        Win32GlobalUnlock(new HandleRef(this, hglobal));
                    }

                    return GetDataFromHGLOBAL(format, hglobal);
                }
                finally
                {
                    Win32GlobalFree(new HandleRef(this, hglobal));
                }
            }
            finally
            {
                UnsafeNativeMethods.ReleaseStgMedium(ref medium);
            }
        }


        /// <summary>
        ///  Retrieves the specified data type from the specified hglobal.
        /// </summary>
        private object? GetDataFromHGLOBAL(string format, nint hglobal)
        {
            if (hglobal == 0)
            {
                return null;
            }

            object? data;

            // Convert from OLE to IW objects

            // Add any new formats here.
            if (format == DataFormats.Html || format == DataFormats.Xaml)
            {
                // Read string from handle as UTF8 encoding.
                // ReadStringFromHandleUtf8 will return Unicode string from UTF8
                // encoded handle.
                data = ReadStringFromHandleUtf8(hglobal);
            }
            else if (format == DataFormats.Text
                || format == DataFormats.Rtf
                || format == DataFormats.OemText
                || format == DataFormats.CommaSeparatedValue)
            {
                data = ReadStringFromHandle(hglobal, unicode: false);
            }
            else if (format == DataFormats.UnicodeText)
            {
                data = ReadStringFromHandle(hglobal, unicode: true);
            }
            else if (format == DataFormats.FileDrop)
            {
                data = ReadFileListFromHandle(hglobal);
            }
            else if (format == DataFormatNames.FileNameAnsi)
            {
                data = new string[] { ReadStringFromHandle(hglobal, unicode: false) };
            }
            else if (format == DataFormatNames.FileNameUnicode)
            {
                data = new string[] { ReadStringFromHandle(hglobal, unicode: true) };
            }
            else if (format == typeof(BitmapSource).FullName)
            {
                data = ReadBitmapSourceFromHandle(hglobal);
            }
            // Limit deserialization to DataFormats that correspond to primitives, which are:
            //
            // DataFormats.CommaSeparatedValue
            // DataFormats.FileDrop
            // DataFormats.Html
            // DataFormats.OemText
            // DataFormats.PenData
            // DataFormats.Rtf
            // DataFormats.Serializable
            // DataFormats.Text
            // DataFormats.UnicodeText
            // DataFormats.WaveAudio
            // DataFormats.Xaml
            // DataFormats.XamlPackage 
            // DataFormats.StringFormat *
            // 
            // * Out of these, we will disallow deserialization of 
            // DataFormats.StringFormat to prevent potentially malicious objects from
            // being deserialized as part of a "text" copy-paste or drag-drop.
            // TypeRestrictingSerializationBinder will throw when it encounters 
            // anything other than strings and primitives - this ensures that we will
            // continue successfully deserializing basic strings while rejecting other 
            // data types that advertise themselves as DataFormats.StringFormat.
            // 
            // The rest of the following formats are pre-defined in the OS,
            // they are not managed objects - an so we will not attempt to deserialize them.
            else
            {
                bool restrictDeserialization = format == DataFormats.StringFormat
                    || format == DataFormats.Dib
                    || format == DataFormats.Bitmap
                    || format == DataFormats.EnhancedMetafile
                    || format == DataFormats.MetafilePicture
                    || format == DataFormats.SymbolicLink
                    || format == DataFormats.Dif
                    || format == DataFormats.Tiff
                    || format == DataFormats.Palette
                    || format == DataFormats.PenData
                    || format == DataFormats.Riff
                    || format == DataFormats.WaveAudio
                    || format == DataFormats.Locale;

                data = ReadObjectFromHandle(hglobal, restrictDeserialization);
            }

            return data;
        }

        /// <summary>
        ///  Uses HGLOBALs and retrieves the specified format from the bound IComDataObject.
        /// </summary>
        private object? GetDataFromOleHGLOBAL(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index,
                tymed = TYMED.TYMED_HGLOBAL
            };

            object? data = null;

            if (NativeMethods.S_OK == QueryGetDataInner(ref formatetc))
            {
                GetDataInner(ref formatetc, out STGMEDIUM medium);
                try
                {
                    // Check both handle and type of storage medium
                    if (medium.unionmember != 0 && medium.tymed == TYMED.TYMED_HGLOBAL)
                    {
                        data = GetDataFromHGLOBAL(format, medium.unionmember);
                    }
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseStgMedium(ref medium);
                }
            }

            return data;
        }

        /// <summary>
        ///  Retrieves the specified format data from the bound IComDataObject, from
        ///  other sources that IStream and HGLOBAL. This is really just a place
        ///  to put the "special" formats like BITMAP, ENHMF, etc.
        /// </summary>
        private object? GetDataFromOleOther(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc = new FORMATETC();
            TYMED tymed = 0;

            if (format == DataFormats.Bitmap)
            {
                tymed = TYMED.TYMED_GDI;
            }
            else if (format == DataFormats.EnhancedMetafile)
            {
                tymed = TYMED.TYMED_ENHMF;
            }

            if (tymed == (TYMED)0)
            {
                return null;
            }

            formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
            formatetc.dwAspect = aspect;
            formatetc.lindex = index;
            formatetc.tymed = tymed;

            object? data = null;

            if (NativeMethods.S_OK == QueryGetDataInner(ref formatetc))
            {
                GetDataInner(ref formatetc, out STGMEDIUM medium);
                try
                {
                    if (medium.unionmember != 0)
                    {
                        if (format == DataFormats.Bitmap)
                        //||IsFormatEqual(format, DataFormats.Dib)
                        {
                            // Get the bitmap from the handle of bitmap.
                            data = GetBitmapSourceFromHbitmap(medium.unionmember);
                        }
                        else if (format == DataFormats.EnhancedMetafile)
                        {
                            // Get the metafile object form the enhanced metafile handle.
                            data = SystemDrawingHelper.GetMetafileFromHemf((HENHMETAFILE)medium.unionmember);
                        }
                    }
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseStgMedium(ref medium);
                }
            }

            return data;
        }

        /// <summary>
        ///  Extracts a managed Object from the innerData of the specified format.
        ///  This is the base of the OLE to managed conversion.
        /// </summary>
        private object? GetDataFromBoundOleDataObject(string format, DVASPECT aspect, int index)
        {
            object? data = GetDataFromOleOther(format, aspect, index);
            data ??= GetDataFromOleHGLOBAL(format, aspect, index);
            data ??= GetDataFromOleIStream(format, aspect, index);

            return data;
        }

        /// <summary>
        ///  Creates an Stream from the data stored in handle.
        /// </summary>
        private MemoryStream ReadByteStreamFromHandle(nint handle, out bool isSerializedObject)
        {
            nint ptr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                int size = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));
                byte[] bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);

                int index = 0;

                // The object here can either be a stream or a serialized
                // object.  We identify a serialized object by writing the
                // bytes for the guid serializedObjectID at the front
                // of the stream.  Check for that here.
                if (size > s_serializedObjectID.Length)
                {
                    isSerializedObject = true;
                    for (int i = 0; i < s_serializedObjectID.Length; i++)
                    {
                        if (s_serializedObjectID[i] != bytes[i])
                        {
                            isSerializedObject = false;
                            break;
                        }
                    }

                    // Advance the byte pointer.
                    if (isSerializedObject)
                    {
                        index = s_serializedObjectID.Length;
                    }
                }
                else
                {
                    isSerializedObject = false;
                }

                return new MemoryStream(bytes, index, bytes.Length - index);
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }
        }

        /// <summary>
        ///  Creates a new instance of the Object that has been persisted into the handle.
        /// </summary>
        private object? ReadObjectFromHandle(nint handle, bool restrictDeserialization)
        {
            object? value;
            Stream stream = ReadByteStreamFromHandle(handle, out bool isSerializedObject);

            if (isSerializedObject)
            {
                long startPosition = stream.Position;
                try
                {
                    if (NrbfDecoder.Decode(stream, leaveOpen: true).TryGetFrameworkObject(out value))
                    {
                        return value;
                    }
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    // Couldn't parse for some reason, let the BinaryFormatter try to handle it.
                }

                // Using Binary formatter
                stream.Position = startPosition;
#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete 
#pragma warning disable CA2300 // Do not use insecure deserializer BinaryFormatter
                BinaryFormatter formatter = new BinaryFormatter();
                if (restrictDeserialization)
                {
                    formatter.Binder = new TypeRestrictingSerializationBinder();
                }
                try
                {
                    value = formatter.Deserialize(stream);
#pragma warning restore CA2300
#pragma warning restore SYSLIB0011
                }
                catch (RestrictedTypeDeserializationException)
                {
                    // Couldn't parse for some reason, then need to add a type converter that round trips with string or byte[]
                    value = null;
                }
            }
            else
            {
                value = stream;
            }

            return value;
        }

        /// <summary>
        ///  Creates a new instance of BitmapSource that has been saved to the
        ///  handle as the memory stream of BitmapSource.
        /// </summary>
        private BitmapFrame? ReadBitmapSourceFromHandle(nint handle)
        {
            // Read the bitmap stream from the handle
            if (ReadByteStreamFromHandle(handle, out _) is { } bitmapStream)
            {
                // Create BitmapSource instance from the bitmap stream
                return BitmapFrame.Create(bitmapStream);
            }

            return null;
        }

        /// <summary>
        ///  Parses the HDROP format and returns a list of strings using
        ///  the DragQueryFile function.
        /// </summary>
        private string[]? ReadFileListFromHandle(nint hdrop)
        {
            string[]? files = null;
            StringBuilder sb = new(NativeMethods.MAX_PATH);

            int count = UnsafeNativeMethods.DragQueryFile(new HandleRef(this, hdrop), unchecked((int)0xFFFFFFFF), null, 0);
            if (count > 0)
            {
                files = new string[count];

                for (int i = 0; i < count; i++)
                {
                    if (UnsafeNativeMethods.DragQueryFile(new HandleRef(this, hdrop), i, sb, sb.Capacity) != 0)
                    {
                        files[i] = sb.ToString();
                    }
                }
            }

            return files;
        }

        /// <summary>
        ///  Creates a string from the data stored in handle. If
        ///  unicode is set to true, then the string is assume to be unicode,
        ///  else DBCS (ASCI) is assumed.
        /// </summary>
        private unsafe string ReadStringFromHandle(nint handle, bool unicode)
        {
            nint ptr = Win32GlobalLock(new HandleRef(this, handle));
            try
            {
                return unicode ? new string((char*)ptr) : new string((sbyte*)ptr);
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }
        }

        /// <summary>
        ///  Creates a string from the data stored in handle as UTF8.
        /// </summary>
        private unsafe string? ReadStringFromHandleUtf8(nint handle)
        {
            string? stringData = null;

            int utf8ByteSize = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));

            nint pointerUtf8 = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                int utf8ByteCount;

                // GlobalSize can return the size of a memory block that may be
                // larger than the size requested when the memory was allocated.
                // So recount the utf8 byte from looking the null terminator.
                for (utf8ByteCount = 0; utf8ByteCount < utf8ByteSize; utf8ByteCount++)
                {
                    // Read the byte from utf8 encoded pointer until get the null terminator.
                    byte endByte = Marshal.ReadByte((nint)((long)pointerUtf8 + utf8ByteCount));

                    // Break if endByte is the null terminator.
                    if (endByte == '\0')
                    {
                        break;
                    }
                }

                if (utf8ByteCount > 0)
                {
                    byte[] bytes = new byte[utf8ByteCount];

                    // Copy the UTF8 encoded data from memory to the byte array.
                    Marshal.Copy(pointerUtf8, bytes, 0, utf8ByteCount);

                    // Create UTF8Encoding to decode the utf8encoded byte to the string(Unicode).
                    UTF8Encoding utf8Encoding = new UTF8Encoding();

                    // Get the string from the UTF8 encoding bytes.
                    stringData = utf8Encoding.GetString(bytes, 0, utf8ByteCount);
                }
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }

            return stringData;
        }

        private bool GetDataPresentInner(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index
            };

            for (int i = 0; i < s_allowedTymeds.Length; i++)
            {
                formatetc.tymed |= s_allowedTymeds[i];
            }

            return QueryGetDataInner(ref formatetc) == NativeMethods.S_OK;
        }

        private int QueryGetDataInner(ref FORMATETC formatetc) => _innerData.QueryGetData(ref formatetc);

        private void GetDataInner(ref FORMATETC formatetc, out STGMEDIUM medium) =>
            _innerData.GetData(ref formatetc, out medium);

        private IEnumFORMATETC EnumFormatEtcInner(DATADIR dwDirection) => _innerData.EnumFormatEtc(dwDirection);

        /// <summary>
        ///  Get the bitmap from the handle of bitmap(Hbitmap).
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   We need a separate method to avoid loading the System.Drawing assembly when unnecessary.
        ///  </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static BitmapSource GetBitmapSourceFromHbitmap(nint hbitmap) =>
            Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap,
                0,
                Int32Rect.Empty,
                null);
    }
}
