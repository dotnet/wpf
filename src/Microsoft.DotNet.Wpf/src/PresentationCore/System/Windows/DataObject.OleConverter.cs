// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace System.Windows;

public sealed partial class DataObject
{
    /// <summary>
    /// OLE Converter.  This class embodies the nastiness required to convert from our
    /// managed types to standard OLE clipboard formats.
    /// </summary>
    private partial class OleConverter : IDataObject
    {
        internal IComDataObject _innerData;

        public OleConverter(IComDataObject data)
        {
            _innerData = data;
        }

        public Object GetData(string format)
        {
            return GetData(format, true);
        }

        public Object GetData(Type format)
        {
            return GetData(format.FullName);
        }

        public Object GetData(string format, bool autoConvert)
        {
            return GetData(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
        }

        public bool GetDataPresent(string format)
        {
            return GetDataPresent(format, true);
        }

        public bool GetDataPresent(Type format)
        {
            return GetDataPresent(format.FullName);
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return GetDataPresent(format, autoConvert, DVASPECT.DVASPECT_CONTENT, -1);
        }

        public string[] GetFormats()
        {
            return GetFormats(true);
        }

        public void SetData(Object data)
        {
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

            if (enumFORMATETC is not null)
            {
                FORMATETC[] formatetc = [new FORMATETC()];
                int[] retrieved = [1];

                enumFORMATETC.Reset();

                while (retrieved[0] > 0)
                {
                    retrieved[0] = 0;

                    if (enumFORMATETC.Next(1, formatetc, retrieved) == NativeMethods.S_OK && retrieved[0] > 0)
                    {
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
                            if (formatetc[formatetcIndex].ptd != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(formatetc[formatetcIndex].ptd);
                            }
                        }
                    }
                }
            }

            return GetDistinctStrings(formats);
        }

        public void SetData(string format, Object data)
        {
            SetData(format, data, true);
        }

        public void SetData(Type format, Object data)
        {
            SetData(format.FullName, data);
        }

        public void SetData(string format, Object data, bool autoConvert)
        {
            SetData(format, data, true, DVASPECT.DVASPECT_CONTENT, 0);
        }

        /// <summary>
        /// Returns the data Object we are wrapping
        /// </summary>
        public IComDataObject OleDataObject
        {
            get
            {
                return _innerData;
            }
        }

        private Object GetData(string format, bool autoConvert, DVASPECT aspect, int index)
        {

            Object baseVar;
            Object original;

            baseVar = GetDataFromBoundOleDataObject(format, aspect, index);
            original = baseVar;

            if (autoConvert && (baseVar == null || baseVar is MemoryStream))
            {
                string[] mappedFormats;

                mappedFormats = GetMappedFormats(format);
                if (mappedFormats != null)
                {
                    for (int i = 0; i < mappedFormats.Length; i++)
                    {
                        if (!IsFormatEqual(format, mappedFormats[i]))
                        {
                            baseVar = GetDataFromBoundOleDataObject(mappedFormats[i], aspect, index);

                            if (baseVar != null && !(baseVar is MemoryStream))
                            {
                                if (IsDataSystemBitmapSource(baseVar) || SystemDrawingHelper.IsBitmap(baseVar))
                                {
                                    // Ensure Bitmap(BitmapSource or System.Drawing.Bitmap) data which
                                    // match with the requested format.
                                    baseVar = EnsureBitmapDataFromFormat(format, autoConvert, baseVar);
                                }

                                original = null;
                                break;
                            }
                        }
                    }
                }
            }

            if (original != null)
            {
                return original;
            }
            else
            {
                return baseVar;
            }
        }

        private bool GetDataPresent(string format, bool autoConvert, DVASPECT aspect, int index)
        {

            bool baseVar;

            baseVar = GetDataPresentInner(format, aspect, index);

            if (!baseVar && autoConvert)
            {
                string[] mappedFormats;

                mappedFormats = GetMappedFormats(format);
                if (mappedFormats != null)
                {
                    for (int i = 0; i < mappedFormats.Length; i++)
                    {
                        if (!IsFormatEqual(format, mappedFormats[i]))
                        {
                            baseVar = GetDataPresentInner(mappedFormats[i], aspect, index);
                            if (baseVar)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return baseVar;
        }

        private void SetData(string format, object data, bool autoConvert, DVASPECT aspect, int index)
        {
            // If we want to support setting data into an OLE data Object,
            // the code should be here.
            throw new InvalidOperationException(SR.DataObject_CannotSetDataOnAFozenOLEDataDbject);
        }

        /// <summary>
        /// Uses IStream and retrieves the specified format from the bound IComDataObject.
        /// </summary>
        private Object GetDataFromOleIStream(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc;
            STGMEDIUM medium;

            formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index,
                tymed = TYMED.TYMED_ISTREAM
            };

            object outData = null;

            if (NativeMethods.S_OK == QueryGetDataInner(ref formatetc))
            {
                GetDataInner(ref formatetc, out medium);
                try
                {
                    // Check both handle and type of storage medium
                    if (medium.unionmember != IntPtr.Zero && medium.tymed == TYMED.TYMED_ISTREAM)
                    {
                        UnsafeNativeMethods.IStream pStream;

                        pStream = (UnsafeNativeMethods.IStream)Marshal.GetObjectForIUnknown(medium.unionmember);

                        NativeMethods.STATSTG sstg = new NativeMethods.STATSTG();
                        pStream.Stat(sstg, NativeMethods.STATFLAG_DEFAULT);
                        int size = (int)sstg.cbSize;

                        IntPtr hglobal = Win32GlobalAlloc(NativeMethods.GMEM_MOVEABLE
                                                           | NativeMethods.GMEM_DDESHARE
                                                           | NativeMethods.GMEM_ZEROINIT,
                                                          (IntPtr)(size));
                        try
                        {
                            IntPtr ptr = Win32GlobalLock(new HandleRef(this, hglobal));

                            try
                            {
                                // 
                                // Seek to the beginning of the stream before reading it.
                                pStream.Seek(0, 0 /* STREAM_SEEK_SET */);
                                pStream.Read(ptr, size);
                            }
                            finally
                            {
                                Win32GlobalUnlock(new HandleRef(this, hglobal));
                            }
                            outData = GetDataFromHGLOBAL(format, hglobal);
                        }
                        finally
                        {
                            Win32GlobalFree(new HandleRef(this, hglobal));
                        }
                    }
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseStgMedium(ref medium);
                }
            }

            return outData;
        }


        /// <summary>
        /// Retrieves the specified data type from the specified hglobal.
        /// </summary>
        private object GetDataFromHGLOBAL(string format, IntPtr hglobal)
        {
            object data;

            data = null;

            if (hglobal != IntPtr.Zero)
            {
                //=----------------------------------------------------------------=
                // Convert from OLE to IW objects
                //=----------------------------------------------------------------=
                // Add any new formats here...
                if (IsFormatEqual(format, DataFormats.Html)
                    || IsFormatEqual(format, DataFormats.Xaml))
                {
                    // Read string from handle as UTF8 encoding.
                    // ReadStringFromHandleUtf8 will return Unicode string from UTF8
                    // encoded handle.
                    data = ReadStringFromHandleUtf8(hglobal);
                }
                else if (IsFormatEqual(format, DataFormats.Text)
                    || IsFormatEqual(format, DataFormats.Rtf)
                    || IsFormatEqual(format, DataFormats.OemText)
                    || IsFormatEqual(format, DataFormats.CommaSeparatedValue))
                {
                    data = ReadStringFromHandle(hglobal, false);
                }
                else if (IsFormatEqual(format, DataFormats.UnicodeText))
                {
                    data = ReadStringFromHandle(hglobal, true);
                }
                else if (IsFormatEqual(format, DataFormats.FileDrop))
                {
                    data = (object)ReadFileListFromHandle(hglobal);
                }
                else if (IsFormatEqual(format, DataFormatNames.FileNameAnsi))
                {
                    data = new string[] { ReadStringFromHandle(hglobal, false) };
                }
                else if (IsFormatEqual(format, DataFormatNames.FileNameUnicode))
                {
                    data = new string[] { ReadStringFromHandle(hglobal, true) };
                }
                else if (IsFormatEqual(format, typeof(BitmapSource).FullName))
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
                    bool restrictDeserialization =
                      (IsFormatEqual(format, DataFormats.StringFormat) ||
                       IsFormatEqual(format, DataFormats.Dib) ||
                       IsFormatEqual(format, DataFormats.Bitmap) ||
                       IsFormatEqual(format, DataFormats.EnhancedMetafile) ||
                       IsFormatEqual(format, DataFormats.MetafilePicture) ||
                       IsFormatEqual(format, DataFormats.SymbolicLink) ||
                       IsFormatEqual(format, DataFormats.Dif) ||
                       IsFormatEqual(format, DataFormats.Tiff) ||
                       IsFormatEqual(format, DataFormats.Palette) ||
                       IsFormatEqual(format, DataFormats.PenData) ||
                       IsFormatEqual(format, DataFormats.Riff) ||
                       IsFormatEqual(format, DataFormats.WaveAudio) ||
                       IsFormatEqual(format, DataFormats.Locale));

                    data = ReadObjectFromHandle(hglobal, restrictDeserialization);
                }
            }

            return data;
        }

        /// <summary>
        /// Uses HGLOBALs and retrieves the specified format from the bound IComDataObject.
        /// </summary>
        private object GetDataFromOleHGLOBAL(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc;
            STGMEDIUM medium;
            Object data;

            formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index,
                tymed = TYMED.TYMED_HGLOBAL
            };

            data = null;

            if (NativeMethods.S_OK == QueryGetDataInner(ref formatetc))
            {
                GetDataInner(ref formatetc, out medium);
                try
                {
                    // Check both handle and type of storage medium
                    if (medium.unionmember != IntPtr.Zero && medium.tymed == TYMED.TYMED_HGLOBAL)
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
        /// Retrieves the specified format data from the bound IComDataObject, from
        /// other sources that IStream and HGLOBAL... this is really just a place
        /// to put the "special" formats like BITMAP, ENHMF, etc.
        /// </summary>
        private Object GetDataFromOleOther(string format, DVASPECT aspect, int index)
        {
            FORMATETC formatetc;
            STGMEDIUM medium;
            TYMED tymed;
            Object data;

            formatetc = new FORMATETC();

            tymed = (TYMED)0;

            if (IsFormatEqual(format, DataFormats.Bitmap))
            {
                tymed = TYMED.TYMED_GDI;
            }
            else if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
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

            data = null;

            if (NativeMethods.S_OK == QueryGetDataInner(ref formatetc))
            {
                GetDataInner(ref formatetc, out medium);
                try
                {
                    if (medium.unionmember != IntPtr.Zero)
                    {
                        if (IsFormatEqual(format, DataFormats.Bitmap))
                        //||IsFormatEqual(format, DataFormats.Dib)
                        {
                            // Get the bitmap from the handle of bitmap.
                            data = GetBitmapSourceFromHbitmap(medium.unionmember);
                        }
                        else if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
                        {
                            // Get the metafile object form the enhanced metafile handle.
                            data = SystemDrawingHelper.GetMetafileFromHemf(medium.unionmember);
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
        /// Extracts a managed Object from the innerData of the specified
        /// format. This is the base of the OLE to managed conversion.
        /// </summary>
        private Object GetDataFromBoundOleDataObject(string format, DVASPECT aspect, int index)
        {
            Object data;

            data = null;

            data = GetDataFromOleOther(format, aspect, index);
            if (data == null)
            {
                data = GetDataFromOleHGLOBAL(format, aspect, index);
            }
            if (data == null)
            {
                data = GetDataFromOleIStream(format, aspect, index);
            }

            return data;
        }

        /// <summary>
        /// Creates an Stream from the data stored in handle.
        /// </summary>
        private Stream ReadByteStreamFromHandle(IntPtr handle, out bool isSerializedObject)
        {
            IntPtr ptr;

            ptr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                Int32 size;
                byte[] bytes;
                int index;

                size = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));
                bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                index = 0;

                // The object here can either be a stream or a serialized
                // object.  We identify a serialized object by writing the
                // bytes for the guid serializedObjectID at the front
                // of the stream.  Check for that here.
                //
                if (size > _serializedObjectID.Length)
                {
                    isSerializedObject = true;
                    for(int i = 0; i < _serializedObjectID.Length; i++)
                    {
                        if (_serializedObjectID[i] != bytes[i])
                        {
                            isSerializedObject = false;
                            break;
                        }
                    }

                    // Advance the byte pointer.
                    //
                    if (isSerializedObject)
                    {
                        index = _serializedObjectID.Length;
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
        /// Creates a new instance of the Object that has been persisted into the
        /// handle.
        /// </summary>
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        private Object ReadObjectFromHandle(IntPtr handle, bool restrictDeserialization)
        {
            object value;
            bool isSerializedObject;
            Stream stream;

            value = null;

            stream = ReadByteStreamFromHandle(handle, out isSerializedObject);

            if (isSerializedObject)
            {

                long startPosition = stream.Position;
                try
                {
                    if (NrbfDecoder.Decode(stream, leaveOpen: true).TryGetFrameworkObject(out object val))
                    {
                        return val;
                    }
                }
                catch (Exception ex) when (!ex.IsCriticalException()) 
                {
                    // Couldn't parse for some reason, let the BinaryFormatter try to handle it.
                    
                }

                // Using Binary formatter
                stream.Position = startPosition;
                BinaryFormatter formatter;
                formatter = new BinaryFormatter();
                if (restrictDeserialization)
                {
                    formatter.Binder = new TypeRestrictingSerializationBinder();
                }
                try
                {
                    #pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete 
                    value = formatter.Deserialize(stream);
                    #pragma warning restore SYSLIB0011 // BinaryFormatter is obsolete 
                }
                catch (RestrictedTypeDeserializationException)
                {
                    value = null;
                    // Couldn't parse for some reason, then need to add a type converter that round trips with string or byte[]                     
                }
            }
            else
            {
                value = stream;
            }

            return value;
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        /// <summary>
        /// Creates a new instance of BitmapSource that has been saved to the
        /// handle as the memory stream of BitmapSource.
        /// </summary>
        private BitmapSource ReadBitmapSourceFromHandle(IntPtr handle)
        {
            Stream bitmapStream;
            BitmapSource bitmapSource;
            bool isSerializedObject;

            bitmapSource = null;

            // Read the bitmap stream from the handle
            bitmapStream = ReadByteStreamFromHandle(handle, out isSerializedObject);

            if (bitmapStream != null)
            {
                // Create BitmapSource instance from the bitmap stream
                bitmapSource = (BitmapSource)BitmapFrame.Create(bitmapStream);
            }

            return bitmapSource;
        }

        /// <summary>
        /// Parses the HDROP format and returns a list of strings using
        /// the DragQueryFile function.
        /// </summary>
        private string[] ReadFileListFromHandle(IntPtr hdrop)
        {
            string[] files;
            StringBuilder sb;
            int count;

            files = null;
            sb = new StringBuilder(NativeMethods.MAX_PATH);

            count = UnsafeNativeMethods.DragQueryFile(new HandleRef(this, hdrop), unchecked((int)0xFFFFFFFF), null, 0);
            if (count > 0)
            {
                files = new string[count];

                for (int i=0; i<count; i++)
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
        /// Creates a string from the data stored in handle. If
        /// unicode is set to true, then the string is assume to be unicode,
        /// else DBCS (ASCI) is assumed.
        /// </summary>
        private unsafe string ReadStringFromHandle(IntPtr handle, bool unicode)
        {
            string stringData;
            IntPtr ptr;

            stringData = null;

            ptr = Win32GlobalLock(new HandleRef(this, handle));
            try
            {
                if (unicode)
                {
                    stringData = new string((char*)ptr);
                }
                else
                {
                    stringData = new string((sbyte*)ptr);
                }
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }

            return stringData;
        }

        /// <summary>
        /// Creates a string from the data stored in handle as UTF8.
        /// </summary>
        private unsafe string ReadStringFromHandleUtf8(IntPtr handle)
        {
            string stringData = null;

            int utf8ByteSize = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));

            IntPtr pointerUtf8 = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                int utf8ByteCount;
                // GlobalSize can return the size of a memory block that may be
                // larger than the size requested when the memory was allocated.
                // So recount the utf8 byte from looking the null terminator.
                for (utf8ByteCount = 0; utf8ByteCount < utf8ByteSize; utf8ByteCount++)
                {
                    // Read the byte from utf8 encoded pointer until get the null terminator.
                    byte endByte = Marshal.ReadByte((IntPtr)((long)pointerUtf8 + utf8ByteCount));

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
            FORMATETC formatetc;
            int hr;

            formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = aspect,
                lindex = index
            };

            for (int i=0; i<ALLOWED_TYMEDS.Length; i++)
            {
                formatetc.tymed |= ALLOWED_TYMEDS[i];
            }

            hr = QueryGetDataInner(ref formatetc);

            return (hr == NativeMethods.S_OK);
        }

        private int QueryGetDataInner(ref FORMATETC formatetc)
        {
            return _innerData.QueryGetData(ref formatetc);
        }

        private void GetDataInner(ref FORMATETC formatetc, out STGMEDIUM medium)
        {
            _innerData.GetData(ref formatetc, out medium);
        }

        private IEnumFORMATETC EnumFormatEtcInner(DATADIR dwDirection)
        {
            return _innerData.EnumFormatEtc(dwDirection);
        }

        /// <summary>
        ///     Get the bitmap from the handle of bitmap(Hbitmap).
        ///
        ///     We need a separate method to avoid loading the System.Drawing assembly
        ///     when unnecessary.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private Object GetBitmapSourceFromHbitmap(IntPtr hbitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                null);
        }
    }
}
