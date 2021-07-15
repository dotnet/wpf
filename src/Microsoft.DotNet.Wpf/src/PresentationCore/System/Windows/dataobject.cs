// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Top-level class for data transfer for drag-drop and clipboard.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm
//
//


namespace System.Windows
{
    using System;
    using MS.Win32;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;
    using System.Text;
    using MS.Internal;
    using MS.Internal.PresentationCore;                        // SecurityHelper

    using SR=MS.Internal.PresentationCore.SR;
    using SRID=MS.Internal.PresentationCore.SRID;
    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

// PreSharp uses message numbers that the C# compiler doesn't know about.
// Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

    #region DataObject Class
    /// <summary>
    /// Implements a basic data transfer mechanism.
    /// </summary>
    public sealed class DataObject : IDataObject, IComDataObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the dataobject
        /// class, which can store arbitrary data.
        /// </summary>
        public DataObject()
        {
            _innerData = new DataStore();
        }

        /// <summary>
        /// Initializes a new instance of the  class, containing the specified data.
        /// </summary>
        public DataObject(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            IDataObject dataObject = data as IDataObject;

            if (dataObject != null)
            {
                _innerData = dataObject;
            }
            else
            {
                IComDataObject oleDataObject= data as IComDataObject;

                if (oleDataObject != null)
                {
                    _innerData = new OleConverter(oleDataObject);
                }
                else
                {
                    _innerData = new DataStore();
                    SetData(data);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the class, containing the specified data and its
        /// associated format.
        /// </summary>
        public DataObject(string format, object data)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData = new DataStore();
            SetData(format, data);
        }

        /// <summary>
        /// Initializes a new instance of the class, containing the specified data and its
        /// associated format.
        /// </summary>
        public DataObject(Type format, object data)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            _innerData = new DataStore();
            SetData(format.FullName, data);
        }

        /// <summary>
        /// Initializes a new instance of the class, containing the specified data and its
        /// associated format.
        /// </summary>
        public DataObject(string format, object data, bool autoConvert)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData = new DataStore();
            SetData(format, data, autoConvert);
        }

        /// <summary>
        /// Initializes a new instance of the class, with the specified
        /// </summary>
        internal DataObject(System.Windows.IDataObject data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData = data;
        }

        /// <summary>
        /// Initializes a new instance of the class, with the specified
        /// </summary>
        internal DataObject(IComDataObject data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData = new OleConverter(data);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Retrieves the data associated with the specified data
        /// format, using an automated conversion parameter to determine whether to convert
        /// the data to the format.
        /// </summary>
        public object GetData(string format, bool autoConvert)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            return _innerData.GetData(format, autoConvert);
        }

        /// <summary>
        /// Retrieves the data associated with the specified data
        /// format.
        /// </summary>
        public object GetData(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            return GetData(format, true);
        }

        /// <summary>
        /// Retrieves the data associated with the specified class
        /// type format.
        /// </summary>
        public object GetData(Type format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            return GetData(format.FullName);
        }

        /// <summary>
        /// Determines whether data stored in this instance is
        /// associated with, or can be converted to, the specified
        /// format.
        /// </summary>
        public bool GetDataPresent(Type format)
        {
            bool dataPresent;
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            dataPresent = GetDataPresent(format.FullName);
            return dataPresent;
        }

        /// <summary>
        /// Determines whether data stored in this instance is
        /// associated with the specified format, using an automatic conversion
        /// parameter to determine whether to convert the data to the format.
        /// </summary>
        public bool GetDataPresent(string format, bool autoConvert)
        {
            bool dataPresent;

            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            dataPresent = _innerData.GetDataPresent(format, autoConvert);
            return dataPresent;
        }

        /// <summary>
        /// Determines whether data stored in this instance is
        /// associated with, or can be converted to, the specified
        /// format.
        /// </summary>
        public bool GetDataPresent(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            return GetDataPresent(format, true);
        }

        /// <summary>
        /// Gets a list of all formats that data stored in this
        /// instance is associated with or can be converted to, using an automatic
        /// conversion parameter <paramref name="autoConvert"/> to
        /// determine whether to retrieve all formats that the data can be converted to or
        /// only native data formats.
        /// </summary>
        public string[] GetFormats(bool autoConvert)
        {
            return _innerData.GetFormats(autoConvert);
        }

        /// <summary>
        /// Gets a list of all formats that data stored in this instance is associated
        /// with or can be converted to.
        /// </summary>
        public string[] GetFormats()
        {
            return GetFormats(true);
        }

        /// <summary>
        /// Stores the specified data in
        /// this instance, using the class of the data for the format.
        /// </summary>
        public void SetData(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            _innerData.SetData(data);
        }

        /// <summary>
        /// Stores the specified data and its associated format in this
        /// instance.
        /// </summary>
        public void SetData(string format, object data)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData.SetData(format, data);
        }

        /// <summary>
        /// Stores the specified data and
        /// its associated class type in this instance.
        /// </summary>
        public void SetData(Type format, object data)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _innerData.SetData(format, data);
        }

        /// <summary>
        /// Stores the specified data and its associated format in
        /// this instance, using the automatic conversion parameter
        /// to specify whether the
        /// data can be converted to another format.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionClipboard.AllClipboard) to call this API.
        /// </remarks>
        [FriendAccessAllowed]
        public void SetData(string format, Object data, bool autoConvert)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (format == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            _innerData.SetData(format, data, autoConvert);
        }


        /// <summary>
        /// Return true if DataObject contains the audio data. Otherwise, return false.
        /// </summary>
        public bool ContainsAudio()
        {
            return GetDataPresent(DataFormats.WaveAudio, /*autoConvert*/false);
        }

        /// <summary>
        /// Return true if DataObject contains the file drop list data. Otherwise, return false.
        /// </summary>
        public bool ContainsFileDropList()
        {
            return GetDataPresent(DataFormats.FileDrop, /*autoConvert*/false);
        }

        /// <summary>
        /// Return true if DataObject contains the image data. Otherwise, return false.
        /// </summary>
        public bool ContainsImage()
        {
            return GetDataPresent(DataFormats.Bitmap, /*autoConvert*/false);
        }

        /// <summary>
        /// Return true if DataObject contains the text data. Otherwise, return false.
        /// </summary>
        public bool ContainsText()
        {
            return ContainsText(TextDataFormat.UnicodeText);
        }

        /// <summary>
        /// Return true if DataObject contains the specified text data. Otherwise, return false.
        /// </summary>
        public bool ContainsText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
            }

            return GetDataPresent(DataFormats.ConvertToDataFormats(format), /*autoConvert*/false);
        }

        /// <summary>
        /// Get audio data as Stream.
        /// </summary>
        public Stream GetAudioStream()
        {
            return GetData(DataFormats.WaveAudio, /*autoConvert*/false) as Stream;
        }

        /// <summary>
        /// Get file drop list data as Stream.
        /// </summary>
        public StringCollection GetFileDropList()
        {
            StringCollection fileDropListCollection;
            string[] fileDropList;

            fileDropListCollection = new StringCollection();

            fileDropList = GetData(DataFormats.FileDrop, /*autoConvert*/true) as string[];
            if (fileDropList != null)
            {
                fileDropListCollection.AddRange(fileDropList);
            }

            return fileDropListCollection;
        }

        /// <summary>
        /// Get image data as BitmapSource.
        /// </summary>
        public BitmapSource GetImage()
        {
            return GetData(DataFormats.Bitmap, /*autoConvert*/true) as BitmapSource;
        }

        /// <summary>
        /// Get text data which is the unicode text.
        /// </summary>
        public string GetText()
        {
            return GetText(TextDataFormat.UnicodeText);
        }

        /// <summary>
        /// Get text data for the specified data format.
        /// </summary>
        public string GetText(TextDataFormat format)
        {
            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
            }

            string text;

            text = (string)GetData(DataFormats.ConvertToDataFormats(format), false);

            if (text != null)
            {
                return text;
            }

            return string.Empty;
        }

        /// <summary>
        /// Set the audio data with bytes.
        /// </summary>
        public void SetAudio(byte[] audioBytes)
        {
            if (audioBytes == null)
            {
                throw new ArgumentNullException("audioBytes");
            }

            SetAudio(new MemoryStream(audioBytes));
        }

        /// <summary>
        /// Set the audio data with Stream.
        /// </summary>
        public void SetAudio(Stream audioStream)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException("audioStream");
            }

            SetData(DataFormats.WaveAudio, audioStream, /*autoConvert*/false);
        }

        /// <summary>
        /// Set the file drop list data.
        /// </summary>
        public void SetFileDropList(StringCollection fileDropList)
        {
            if (fileDropList == null)
            {
                throw new ArgumentNullException("fileDropList");
            }

            if (fileDropList.Count == 0)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_FileDropListIsEmpty, fileDropList));
            }

            foreach (string fileDrop in fileDropList)
            {
                try
                {
                    string filePath = Path.GetFullPath(fileDrop);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(SR.Get(SRID.DataObject_FileDropListHasInvalidFileDropPath, e));
                }
            }

            string[] fileDropListStrings;

            fileDropListStrings = new string[fileDropList.Count];
            fileDropList.CopyTo(fileDropListStrings, 0);

            SetData(DataFormats.FileDrop, fileDropListStrings, /*audoConvert*/true);
        }

        /// <summary>
        /// Set the image data with BitmapSource.
        /// </summary>
        public void SetImage(BitmapSource image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            SetData(DataFormats.Bitmap, image, /*audoConvert*/true);
        }

        /// <summary>
        /// Set the text data.
        /// </summary>
        public void SetText(string textData)
        {
            if (textData == null)
            {
                throw new ArgumentNullException("textData");
            }

            SetText(textData, TextDataFormat.UnicodeText);
        }

        /// <summary>
        /// Set the text data for the specified text data format.
        /// </summary>
        public void SetText(string textData, TextDataFormat format)
        {
            if (textData == null)
            {
                throw new ArgumentNullException("textData");
            }

            if (!DataFormats.IsValidTextDataFormat(format))
            {
                throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
            }

            SetData(DataFormats.ConvertToDataFormats(format), textData, /*audoConvert*/false);
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        /// <param name="pFormatetc"></param>
        /// <param name="advf"></param>
        /// <param name="pAdvSink"></param>
        /// <param name="pdwConnection"></param>
        /// <returns></returns>
        int IComDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink pAdvSink, out int pdwConnection)
        {
            if (_innerData is OleConverter)
            {
                return ((OleConverter)_innerData).OleDataObject.DAdvise(ref pFormatetc, advf, pAdvSink, out pdwConnection);
            }
            pdwConnection = 0;
            return (NativeMethods.E_NOTIMPL);
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        void IComDataObject.DUnadvise(int dwConnection)
        {
            if (_innerData is OleConverter)
            {
                ((OleConverter)_innerData).OleDataObject.DUnadvise(dwConnection);
                return;
            }

            // Throw the exception NativeMethods.E_NOTIMPL.
            Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        int IComDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            if (_innerData is OleConverter)
            {
                return ((OleConverter)_innerData).OleDataObject.EnumDAdvise(out enumAdvise);
            }
            enumAdvise = null;
            return (OLE_E_ADVISENOTSUPPORTED);
        }

        // <summary>
        // Part of IComDataObject, used to interop with OLE.
        // </summary>
        IEnumFORMATETC IComDataObject.EnumFormatEtc(DATADIR dwDirection)
        {
            if (_innerData is OleConverter)
            {
                return ((OleConverter)_innerData).OleDataObject.EnumFormatEtc(dwDirection);
            }
            if (dwDirection == DATADIR.DATADIR_GET)
            {
                return new FormatEnumerator(this);
            }
            else
            {
                throw new ExternalException(SR.Get(SRID.DataObject_NotImplementedEnumFormatEtc, dwDirection), NativeMethods.E_NOTIMPL);
            }
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        int IComDataObject.GetCanonicalFormatEtc(ref FORMATETC pformatetcIn, out FORMATETC pformatetcOut)
        {
            pformatetcOut = new FORMATETC();
            pformatetcOut = pformatetcIn;
            pformatetcOut.ptd = IntPtr.Zero;

            if (pformatetcIn.lindex != -1)
            {
                return DV_E_LINDEX;
            }

            if (_innerData is OleConverter)
            {
                return ((OleConverter)_innerData).OleDataObject.GetCanonicalFormatEtc(ref pformatetcIn, out pformatetcOut);
            }

            return DATA_S_SAMEFORMATETC;
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        void IComDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
        {
            if (_innerData is OleConverter)
            {
                ((OleConverter)_innerData).OleDataObject.GetData(ref formatetc, out medium);
                return;
            }

            int hr;

            hr = DV_E_TYMED;

            medium = new STGMEDIUM();

            if (GetTymedUseable(formatetc.tymed))
            {
                if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != 0)
                {
                    medium.tymed = TYMED.TYMED_HGLOBAL;

                    medium.unionmember = Win32GlobalAlloc(NativeMethods.GMEM_MOVEABLE
                                                           | NativeMethods.GMEM_DDESHARE
                                                           | NativeMethods.GMEM_ZEROINIT,
                                                          (IntPtr)1);

                    hr = OleGetDataUnrestricted(ref formatetc, ref medium, false /* doNotReallocate */);

                    if (NativeMethods.Failed(hr))
                    {
                        Win32GlobalFree(new HandleRef(this, medium.unionmember));
                    }
                }
                else if ( ( formatetc.tymed & TYMED.TYMED_ISTREAM ) != 0 )
                {
                    medium.tymed = TYMED.TYMED_ISTREAM;

                    IStream istream = null;
                    hr = Win32CreateStreamOnHGlobal(IntPtr.Zero, true /*deleteOnRelease*/, ref istream);
                    if ( NativeMethods.Succeeded(hr) )
                    {
                        medium.unionmember = Marshal.GetComInterfaceForObject(istream, typeof(IStream));
                        Marshal.ReleaseComObject(istream);

                        hr = OleGetDataUnrestricted(ref formatetc, ref medium, false /* doNotReallocate */);

                        if ( NativeMethods.Failed(hr) )
                        {
                            Marshal.Release(medium.unionmember);
                        }
                    }
                }
                else
                {
                    medium.tymed = formatetc.tymed;
                    hr = OleGetDataUnrestricted(ref formatetc, ref medium, false /* doNotReallocate */);
                }
            }

            // Make sure we zero out that pointer if we don't support the format.
            if (NativeMethods.Failed(hr))
            {
                medium.unionmember = IntPtr.Zero;

                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        void IComDataObject.GetDataHere(ref FORMATETC formatetc, ref STGMEDIUM medium)
        {
            // This method is spec'd to accepted only limited number of tymed
            // values, and it does not support multiple OR'd values.
            if (medium.tymed != TYMED.TYMED_ISTORAGE &&
                medium.tymed != TYMED.TYMED_ISTREAM &&
                medium.tymed != TYMED.TYMED_HGLOBAL &&
                medium.tymed != TYMED.TYMED_FILE)
            {
                Marshal.ThrowExceptionForHR(DV_E_TYMED);
            }

            int hr = OleGetDataUnrestricted(ref formatetc, ref medium, true /* doNotReallocate */);
            if (NativeMethods.Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        int IComDataObject.QueryGetData(ref FORMATETC formatetc)
        {
            if (_innerData is OleConverter)
            {
                return ((OleConverter)_innerData).OleDataObject.QueryGetData(ref formatetc);
            }
            if (formatetc.dwAspect == DVASPECT.DVASPECT_CONTENT)
            {
                if (GetTymedUseable(formatetc.tymed))
                {
                    if (formatetc.cfFormat == 0)
                    {
                        return NativeMethods.S_FALSE;
                    }
                    else
                    {
                        if (!GetDataPresent(DataFormats.GetDataFormat(formatetc.cfFormat).Name))
                        {
                            return (DV_E_FORMATETC);
                        }
                    }
                }
                else
                {
                    return (DV_E_TYMED);
                }
            }
            else
            {
                return (DV_E_DVASPECT);
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Part of IComDataObject, used to interop with OLE.
        /// </summary>
        void IComDataObject.SetData(ref FORMATETC pFormatetcIn, ref STGMEDIUM pmedium, bool fRelease)
        {
            if (_innerData is OleConverter)
            {
                ((OleConverter)_innerData).OleDataObject.SetData(ref pFormatetcIn, ref pmedium, fRelease);
                return;
            }

            Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
        }

        //......................................................
        //
        //  Events for Clipboard Extensibility
        //
        //......................................................

        #region Events for Clipboard Extensibility

        /// <summary>
        ///     Adds a handler for the Copying attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">
        /// A handler for DataObject.Copying event.
        /// The handler is expected to inspect the content of a data object
        /// passed via event arguments (DataObjectCopyingEventArgs.DataObject)
        /// and add additional (custom) data format to it.
        /// It's also possible for the handler to change
        /// the contents of other data formats already put on DataObject
        /// or even remove some of those formats.
        /// All this happens before DataObject is put on
        /// the Clipboard (in copy operation) or before DragDrop
        /// process starts.
        /// The handler can cancel the whole copying event
        /// by calling DataObjectCopyingEventArgs.CancelCommand method.
        /// For the case of Copy a command will be cancelled,
        /// for the case of DragDrop a dragdrop process will be
        /// terminated in the beginning.
        /// </param>

        public static void AddCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
        {
            UIElement.AddHandler(element, CopyingEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the Copying attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
        {
            UIElement.RemoveHandler(element, CopyingEvent, handler);
        }

        /// <summary>
        ///     Adds a handler for the Pasting attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">
        /// An event handler for a DataObject.Pasting event.
        /// It is called when ah editor already made a decision
        /// what format (from available on the Clipboard)
        /// to apply to selection. With this handler an application
        /// has a chance to inspect a content of DataObject extracted
        /// from the Clipboard and decide what format to use instead.
        /// There are three options for the handler here:
        /// a) to cancel the whole Paste/Drop event by calling
        /// DataObjectPastingEventArgs.CancelCommand method,
        /// b) change an editor's choice of format by setting
        /// new value for DataObjectPastingEventArgs.FormatToApply
        /// property (the new value is supposed to be understandable
        /// by an editor - it's application's code responsibility
        /// to act consistently with an editor; example is to
        /// replace "rich text" (xaml) format to "plain text" format -
        /// both understandable by the TextEditor).
        /// c) choose it's own custom format, apply it to a selection
        /// and cancel a command for the following execution in an
        /// editor by calling DataObjectPastingEventArgs.CancelCommand
        /// method. This is how custom data formats are expected
        /// to be pasted.
        /// Note that by changing a content of data formats on DataObject
        /// an application code does not affect the global Clipboard.
        /// It only affects how an editor pastes this format.
        /// For instance, by parsing xaml data format and making
        /// some changes in it, the handler does not change this xaml
        /// for the following acts of pasting into the same or another
        /// application.
        /// </param>
        public static void AddPastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
        {
            UIElement.AddHandler(element, PastingEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the Pasting attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
        {
            UIElement.RemoveHandler(element, PastingEvent, handler);
        }

        /// <summary>
        ///     Adds a handler for the SettingData attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">
        /// A handler for a DataObject.SettingData event.
        /// Te event is fired as part of Copy (or Drag) command
        /// once for each of data formats added to a DataObject.
        /// The purpose of this handler is mostly copy command
        /// optimization. With the help of it application
        /// can filter some formats from being added to DataObject.
        /// The other opportunity of doing that exists in
        /// DataObject.Copying event, which could set all undesirable
        /// formats to null, but in this case the work for data
        /// conversion is already done, which may be too expensive.
        /// By handling DataObject.SettingData event an application
        /// can prevent from each particular data format conversion.
        /// By calling DataObjectSettingDataEventArgs.CancelCommand
        /// method the handler tells an editor to skip one particular
        /// data format (identified by DataObjectSettingDataEventArgs.Format
        /// property). Note that calling CancelCommand method
        /// for this event does not cancel the whole Copy or Drag
        /// command.
        /// </param>
        public static void AddSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
        {
            UIElement.AddHandler(element, SettingDataEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the SettingData attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
        {
            UIElement.RemoveHandler(element, SettingDataEvent, handler);
        }

        #endregion Events for Clipboard Extensibility

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The DataObject.Copying event is raised when an editor
        /// has converted a content of selection into all appropriate
        /// clipboard data formats, collected them all in DataObject
        /// and is ready to put the objet onto the Clipboard
        /// or ready to start drag operation.
        /// Application code can inspect DataObject, change, remove or
        /// add some data formats into it and decide whether to proceed
        /// with the copying or cancel it.
        /// </summary>
        public static readonly RoutedEvent CopyingEvent = //
            EventManager.RegisterRoutedEvent("Copying", //
                                               RoutingStrategy.Bubble, //
                                               typeof(DataObjectCopyingEventHandler), //
                                               typeof(DataObject)); //

        /// <summary>
        /// The DataObject.Pasting event is raised when texteditor
        /// is ready to paste one of data format to the content
        /// during paste operation.
        /// Application can inspect a DataObject, change, remove or add
        /// data formats and also can decide whether to proceed pasting
        /// or cancel it.
        /// </summary>
        public static readonly RoutedEvent PastingEvent = //
            EventManager.RegisterRoutedEvent("Pasting", //
                                               RoutingStrategy.Bubble, //
                                               typeof(DataObjectPastingEventHandler), //
                                               typeof(DataObject)); //

        /// <summary>
        /// The DataObject.SettingData event is raised when an editor
        /// is intended to add one more data format to a DataObject during
        /// copy operation.
        /// Handlign this event allows for a user to prevent from
        /// adding undesirable formats, thus improving performance of
        /// copy operations.
        /// </summary>
        public static readonly RoutedEvent SettingDataEvent = //
            EventManager.RegisterRoutedEvent("SettingData", //
                                               RoutingStrategy.Bubble, //
                                               typeof(DataObjectSettingDataEventHandler), //
                                               typeof(DataObject)); //

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalAlloc() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32GlobalAlloc(int flags, IntPtr bytes)
        {
            IntPtr win32Pointer = UnsafeNativeMethods.GlobalAlloc(flags, bytes);
            int win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }

            return win32Pointer;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.CreateStreamOnHGlobal() with Win32 error checking.
        /// </summary>
        private static int Win32CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, ref IStream istream)
        {
            int hr = UnsafeNativeMethods.CreateStreamOnHGlobal(hGlobal, fDeleteOnRelease, ref istream);
            if ( NativeMethods.Failed(hr) )
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return hr;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalFree() with Win32 error checking.
        /// </summary>
        internal static void Win32GlobalFree(HandleRef handle)
        {
            IntPtr win32Pointer = UnsafeNativeMethods.GlobalFree(handle);
            int win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer != IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalReAlloc() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32GlobalReAlloc(HandleRef handle, IntPtr bytes, int flags)
        {
            IntPtr win32Pointer = UnsafeNativeMethods.GlobalReAlloc(handle, bytes, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }

            return win32Pointer;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalLock() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32GlobalLock(HandleRef handle)
        {
            IntPtr win32Pointer = UnsafeNativeMethods.GlobalLock(handle);
            int win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }

            return win32Pointer;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalUnlock() with Win32 error checking.
        /// </summary>
        internal static void Win32GlobalUnlock(HandleRef handle)
        {
            bool win32Return = UnsafeNativeMethods.GlobalUnlock(handle);
            int win32Error = Marshal.GetLastWin32Error();
            if (!win32Return && win32Error != 0)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GlobalSize() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32GlobalSize(HandleRef handle)
        {
            IntPtr win32Pointer = UnsafeNativeMethods.GlobalSize(handle);

            int win32Error = Marshal.GetLastWin32Error();
            if (win32Pointer == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }

            return win32Pointer;
        }

        /// <summary>
        /// Call Win32 SafeNativeMethods.SelectObject() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32SelectObject(HandleRef handleDC, IntPtr handleObject)
        {
            IntPtr handleOldObject = UnsafeNativeMethods.SelectObject(handleDC, handleObject);
            if (handleOldObject == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception();
            }

            return handleOldObject;
        }

        /// <summary>
        /// Call Win32 SafeNativeMethods.DeleteObject() with Win32 error checking.
        /// </summary>
        internal static void Win32DeleteObject(HandleRef handleDC)
        {
            UnsafeNativeMethods.DeleteObject(handleDC);
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.GetDC() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32GetDC(HandleRef handleDC)
        {
            IntPtr newDC = UnsafeNativeMethods.GetDC(handleDC);
            return newDC;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.CreateCompatibleDC() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32CreateCompatibleDC(HandleRef handleDC)
        {
            IntPtr newDC = UnsafeNativeMethods.CreateCompatibleDC(handleDC);
            return newDC;
        }


        /// <summary>
        /// Call Win32 SafeNativeMethods.CreateCompatibleBitmap() with Win32 error checking.
        /// </summary>
        internal static IntPtr Win32CreateCompatibleBitmap(HandleRef handleDC, int width, int height)
        {
            IntPtr bitmap = UnsafeNativeMethods.CreateCompatibleBitmap(handleDC, width, height);
            return bitmap;
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.DeleteDC() with Win32 error checking.
        /// </summary>
        internal static void Win32DeleteDC(HandleRef handleDC)
        {
            UnsafeNativeMethods.DeleteDC(handleDC);
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.ReleaseDC() with Win32 error checking.
        /// </summary>
        private static void Win32ReleaseDC(HandleRef handleHWND, HandleRef handleDC)
        {
            UnsafeNativeMethods.ReleaseDC(handleHWND, handleDC);
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.BitBlt() with Win32 error checking.
        /// </summary>
        internal static void Win32BitBlt(HandleRef handledestination, int width, int height, HandleRef handleSource, int operationCode)
        {
            bool win32Return = UnsafeNativeMethods.BitBlt(handledestination, 0, 0, width, height, handleSource, 0, 0, operationCode);
            if (!win32Return)
            {
                throw new System.ComponentModel.Win32Exception();
            }
        }

        /// <summary>
        /// Call Win32 UnsafeNativeMethods.WideCharToMultiByte() with Win32 error checking.
        /// </summary>
        internal static int Win32WideCharToMultiByte(string wideString, int wideChars, byte[] bytes, int byteCount)
        {
            int win32Return = UnsafeNativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0 /*flags*/, wideString, wideChars, bytes, byteCount, IntPtr.Zero, IntPtr.Zero);
            int win32Error = Marshal.GetLastWin32Error();
            if (win32Return == 0)
            {
                throw new System.ComponentModel.Win32Exception(win32Error);
            }

            return win32Return;
        }

        /// <summary>
        /// Returns all the "synonyms" for the specified format.
        /// </summary>
        internal static string[] GetMappedFormats(string format)
        {
            // 
            if (format == null)
            {
                return null;
            }

            if (IsFormatEqual(format, DataFormats.Text)
                || IsFormatEqual(format, DataFormats.UnicodeText)
                || IsFormatEqual(format, DataFormats.StringFormat))
            {
                string[] arrayFormats;

                arrayFormats = new string[] {
                                    DataFormats.Text,
                                    DataFormats.UnicodeText,
                                    DataFormats.StringFormat,
                                    };

                return arrayFormats;
            }

            if (IsFormatEqual(format, DataFormats.FileDrop)
                || IsFormatEqual(format, DataFormats.FileName)
                || IsFormatEqual(format, DataFormats.FileNameW))
            {
                return new string[] {
                                        DataFormats.FileDrop,
                                        DataFormats.FileNameW,
                                        DataFormats.FileName,
                };
            }

            // Get the System.Drawing.Bitmap string instead of getting it from typeof.
            // So we won't load System.Drawing.dll module here.
            if (IsFormatEqual(format, DataFormats.Bitmap)
                //|| IsFormat.Equals(format, DataFormats.Dib)
                || IsFormatEqual(format, SystemBitmapSourceFormat)
                || IsFormatEqual(format, SystemDrawingBitmapFormat))
            {
                return new string[] {
                                        DataFormats.Bitmap,
                                        SystemDrawingBitmapFormat,
                                        SystemBitmapSourceFormat
                                        //DataFormats.Dib,
                };
            }

            if (IsFormatEqual(format, DataFormats.EnhancedMetafile)
                || IsFormatEqual(format, SystemDrawingImagingMetafileFormat))
            {
                return new string[] {
                                        DataFormats.EnhancedMetafile,
                                        SystemDrawingImagingMetafileFormat
                };
            }

            return new String[] { format };
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods



        /// <summary>
        /// Behaves like IComDataObject.GetData and IComDataObject.GetDataHere,
        /// except we make no restrictions TYMED values.
        /// </summary>
        private int OleGetDataUnrestricted(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
        {
            if (_innerData is OleConverter)
            {
                ((OleConverter)_innerData).OleDataObject.GetDataHere(ref formatetc, ref medium);

                return NativeMethods.S_OK;
            }

            return GetDataIntoOleStructs(ref formatetc, ref medium, doNotReallocate);
        }

        /// <summary>
        /// Retrieves a list of distinct strings from the array.
        /// </summary>
        private static string[] GetDistinctStrings(string[] formats)
        {
            ArrayList distinct;
            string[] distinctStrings;

            distinct = new ArrayList();
            for (int i=0; i<formats.Length; i++)
            {
                string formatString;

                formatString = formats[i];
                if (!distinct.Contains(formatString))
                {
                    distinct.Add(formatString);
                }
            }

            distinctStrings = new string[distinct.Count];
            distinct.CopyTo(distinctStrings, 0);

            return distinctStrings;
        }

        /// <summary>
        /// Returns true if the tymed is useable.
        /// </summary>
        private bool GetTymedUseable(TYMED tymed)
        {
            for (int i=0; i<ALLOWED_TYMEDS.Length; i++)
            {
                if ((tymed & ALLOWED_TYMEDS[i]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private IntPtr GetCompatibleBitmap(object data)
        {

            IntPtr hBitmap;
            IntPtr hBitmapNew;
            IntPtr hDC;
            IntPtr sourceDC;
            IntPtr sourceObject;
            IntPtr destinationDC;
            IntPtr destinationObject;
            int width, height;

            hBitmap = SystemDrawingHelper.GetHBitmap(data, out width, out height);

            if (hBitmap == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                // Get the screen DC.
                hDC = Win32GetDC(new HandleRef(this, IntPtr.Zero));

                // Create a compatible DC to render the source bitmap.
                sourceDC = Win32CreateCompatibleDC(new HandleRef(this, hDC));

                // Select the original object from the current DC.
                sourceObject = Win32SelectObject(new HandleRef(this, sourceDC), hBitmap);

                // Create a compatible DC and a new compatible bitmap.
                destinationDC = Win32CreateCompatibleDC(new HandleRef(this, hDC));

                // creates a bitmap with the device that is associated with the specified DC.
                hBitmapNew = Win32CreateCompatibleBitmap(new HandleRef(this, hDC), width, height);

                // Select the new bitmap into a compatible DC and render the blt the original bitmap.
                destinationObject = Win32SelectObject(new HandleRef(this, destinationDC), hBitmapNew);

                try
                {
                    // Bit-block transfer of the data onto the rectangle of pixels
                    // from the specified source device context into a destination device context.
                    Win32BitBlt(new HandleRef(this, destinationDC), width, height, new HandleRef(null, sourceDC), /* SRCCOPY */0x00CC0020);
                }
                finally
                {
                    // Select the old device context.
                    Win32SelectObject(new HandleRef(this, sourceDC), sourceObject);
                    Win32SelectObject(new HandleRef(this, destinationDC), destinationObject);

                    // Clear the source and destination compatible DCs.
                    Win32DeleteDC(new HandleRef(this, sourceDC));
                    Win32DeleteDC(new HandleRef(this, destinationDC));

                    // release the screen DC
                    Win32ReleaseDC(new HandleRef(this, IntPtr.Zero), new HandleRef(this, hDC));
                }
            }
            finally
            {
                // Delete the bitmap object.
                Win32DeleteObject(new HandleRef(this, hBitmap));
            }

            return hBitmapNew;
        }

        /// <summary>
        /// Get the enhanced metafile handle from the metafile data object.
        /// </summary>
        private IntPtr GetEnhancedMetafileHandle(String format, object data)
        {
            IntPtr hEnhancedMetafile;

            hEnhancedMetafile = IntPtr.Zero;

            if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
            {
                // Get the metafile handle from metafile data object.
                if (SystemDrawingHelper.IsMetafile(data))
                {
                    hEnhancedMetafile = SystemDrawingHelper.GetHandleFromMetafile(data);
                }
                else if (data is MemoryStream)
                {
                    MemoryStream memoryStream;

                    memoryStream = data as MemoryStream;
                    if (memoryStream != null)
                    {
                        byte[] buffer;

                        buffer = memoryStream.GetBuffer();
                        if (buffer != null && buffer.Length != 0)
                        {
                            hEnhancedMetafile = NativeMethods.SetEnhMetaFileBits((uint)buffer.Length, buffer);
                            int win32Error = Marshal.GetLastWin32Error(); // Dance around FxCop

                            if (hEnhancedMetafile == IntPtr.Zero)
                            {
                                // Throw the Win32 exception with GetLastWin32Error.
                                throw new System.ComponentModel.Win32Exception(win32Error);
                            }
                        }
                    }
                }
            }

            return hEnhancedMetafile;
        }

        /// <summary>
        /// Populates Ole datastructes from a WinForms dataObject. This is the core
        /// of WinForms to OLE conversion.
        /// </summary>
        private int GetDataIntoOleStructs(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
        {
            int hr;

            hr = DV_E_TYMED;

            if (GetTymedUseable(formatetc.tymed) && GetTymedUseable(medium.tymed))
            {
                string format;

                format = DataFormats.GetDataFormat(formatetc.cfFormat).Name;

                // set the default result with DV_E_FORMATETC.
                hr = DV_E_FORMATETC;

                if (GetDataPresent(format))
                {
                    object data;

                    data = GetData(format);

                    // set the default result with DV_E_TYMED.
                    hr = DV_E_TYMED;

                    if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != 0)
                    {
                        hr = GetDataIntoOleStructsByTypeMedimHGlobal(format, data, ref medium, doNotReallocate);
                    }
                    else if ( ( formatetc.tymed & TYMED.TYMED_GDI ) != 0 )
                    {
                        hr = GetDataIntoOleStructsByTypeMediumGDI(format, data, ref medium);
                    }
                    else if ( ( formatetc.tymed & TYMED.TYMED_ENHMF ) != 0 )
                    {
                        hr = GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(format, data, ref medium);
                    }
                    else if ( ( formatetc.tymed & TYMED.TYMED_ISTREAM ) != 0 )
                    {
                        hr = GetDataIntoOleStructsByTypeMedimIStream(format, data, ref medium);
                    }
                }
            }

            return hr;
        }

        /// <summary>
        /// Populates Ole data structes from a dataObject that is TYMED_HGLOBAL.
        /// </summary>
        private int GetDataIntoOleStructsByTypeMedimHGlobal(string format, object data, ref STGMEDIUM medium, bool doNotReallocate)
        {
            int hr;

            hr = NativeMethods.E_FAIL;

            if (data is Stream)
            {
                hr = SaveStreamToHandle(medium.unionmember, (Stream)data, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.Html)
                || IsFormatEqual(format, DataFormats.Xaml))
            {
                // Save Html and Xaml data string as UTF8 encoding.
                hr = SaveStringToHandleAsUtf8(medium.unionmember, data.ToString(), doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.Text)
                || IsFormatEqual(format, DataFormats.Rtf)
                || IsFormatEqual(format, DataFormats.OemText)
                || IsFormatEqual(format, DataFormats.CommaSeparatedValue))
            {
                hr = SaveStringToHandle(medium.unionmember, data.ToString(), false /* unicode */, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.UnicodeText)||
                     IsFormatEqual(format, DataFormats.ApplicationTrust))
            {
                hr = SaveStringToHandle(medium.unionmember, data.ToString(), true /* unicode */, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.FileDrop))
            {
                hr = SaveFileListToHandle(medium.unionmember, (string[])data, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.FileName))
            {
                string[] filelist;

                filelist = (string[])data;
                hr = SaveStringToHandle(medium.unionmember, filelist[0], false /* unicode */, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.FileNameW))
            {
                string[] filelist;

                filelist = (string[])data;
                hr = SaveStringToHandle(medium.unionmember, filelist[0], true /* unicode */, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.Dib)
                && SystemDrawingHelper.IsImage(data))
            {
                // GDI+ does not properly handle saving to DIB images.  Since the
                // clipboard will take an HBITMAP and publish a Dib, we don't need
                // to support this.
                //
                hr = DV_E_TYMED;
            }
            else if (IsFormatEqual(format, typeof(BitmapSource).FullName))
            {
                // Save the System.Drawing.Bitmap or BitmapSource data to handle as BitmapSource
                hr = SaveSystemBitmapSourceToHandle(medium.unionmember, data, doNotReallocate);
            }
            else if (IsFormatEqual(format, SystemDrawingBitmapFormat))
            {
                // Save the System.Drawing.Bitmap or BitmapSource data to handle as System.Drawing.Bitmap
                hr = SaveSystemDrawingBitmapToHandle(medium.unionmember, data, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.EnhancedMetafile)
                || SystemDrawingHelper.IsMetafile(data))
            {
                // We don't need to support the enhanced metafile for TYMED.TYMED_HGLOBAL,
                // since we directly support TYMED.TYMED_ENHMF.
                hr = DV_E_TYMED;
            }
            else if (IsFormatEqual(format, DataFormats.Serializable)
                || data is ISerializable
                || (data != null && data.GetType().IsSerializable))
            {
                hr = SaveObjectToHandle(medium.unionmember, data, doNotReallocate);
            }
            else
            {
                // Couldn't find the proper data for the current TYMED_HGLOBAL
                hr = DV_E_TYMED;
            }

            if (hr == NativeMethods.S_OK)
            {
                medium.tymed = TYMED.TYMED_HGLOBAL;
            }

            return hr;
        }

        /// <summary>
        /// Populates Ole data structes from a dataObject that is TYMED_ISTREAM.
        /// </summary>
        private int GetDataIntoOleStructsByTypeMedimIStream(string format, object data, ref STGMEDIUM medium)
        {
            IStream istream = (IStream)( Marshal.GetObjectForIUnknown(medium.unionmember) );
            if ( istream == null )
            {
                return NativeMethods.E_INVALIDARG;
            }

            int hr = NativeMethods.E_FAIL;

            try
            {
                // If the format is ISF, we should copy the data from the managed stream to the COM IStream object.
                if ( format == System.Windows.Ink.StrokeCollection.InkSerializedFormat )
                {
                    Stream inkStream = data as Stream;

                    if ( inkStream != null )
                    {
                        IntPtr size = (IntPtr)inkStream.Length;

                        byte[] buffer = new byte[NativeMethods.IntPtrToInt32(size)];
                        inkStream.Position = 0;
                        inkStream.Read(buffer, 0, NativeMethods.IntPtrToInt32(size));

                        istream.Write(buffer, NativeMethods.IntPtrToInt32(size), IntPtr.Zero);
                        hr = NativeMethods.S_OK;
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(istream);
            }

            if ( NativeMethods.Succeeded(hr) )
            {
                medium.tymed = TYMED.TYMED_ISTREAM;
            }

            return hr;
        }

        /// <summary>
        /// Populates Ole data structes from a dataObject that is TYMED_GDI.
        /// </summary>
        private int GetDataIntoOleStructsByTypeMediumGDI(string format, object data, ref STGMEDIUM medium)
        {
            int hr;

            hr = NativeMethods.E_FAIL;

            if (IsFormatEqual(format, DataFormats.Bitmap)
                && (SystemDrawingHelper.IsBitmap(data) || IsDataSystemBitmapSource(data)))
            {
                // Get the bitmap and save it.
                IntPtr hBitmap;

                hBitmap = GetCompatibleBitmap(data);

                if (hBitmap != IntPtr.Zero)
                {
                    medium.tymed = TYMED.TYMED_GDI;
                    medium.unionmember = hBitmap;

                    hr = NativeMethods.S_OK;
                }
            }
            else
            {
                // Couldn't find the proper data for the current TYMED_GDI
                hr = DV_E_TYMED;
            }

            return hr;
        }

        /// <summary>
        /// Populates Ole data structes from a dataObject that is TYMED_ENHMF.
        /// </summary>
        private int GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(string format, object data, ref STGMEDIUM medium)
        {
            IntPtr hMetafile;
            int hr;

            hr = NativeMethods.E_FAIL;

            if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
            {
                // Get the enhanced metafile handle from the metafile data
                // and save the metafile handle.
                hMetafile = GetEnhancedMetafileHandle(format, data);

                if (hMetafile != IntPtr.Zero)
                {
                    medium.tymed = TYMED.TYMED_ENHMF;
                    medium.unionmember = hMetafile;

                    hr = NativeMethods.S_OK;
                }
            }
            else
            {
                // Couldn't find the proper data for the current TYMED_ENHMF
                hr = DV_E_TYMED;
            }

            return hr;
        }

        private int SaveObjectToHandle(IntPtr handle, Object data, bool doNotReallocate)
        {
            Stream stream;
            BinaryWriter binaryWriter;
            BinaryFormatter formatter;

            using (stream = new MemoryStream())
            {
                using (binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.Write(_serializedObjectID);

                    formatter = new BinaryFormatter();

                    #pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete 
                    formatter.Serialize(stream, data);
                    #pragma warning restore SYSLIB0011 // BinaryFormatter is obsolete
                    return SaveStreamToHandle(handle, stream, doNotReallocate);
                }
            }
        }

        /// <summary>
        /// Saves stream out to handle.
        /// </summary>
        private int SaveStreamToHandle(IntPtr handle, Stream stream, bool doNotReallocate)
        {
            IntPtr size;
            IntPtr ptr;

            if (handle == IntPtr.Zero)
            {
                return (NativeMethods.E_INVALIDARG);
            }

            size = (IntPtr)stream.Length;

            int hr = EnsureMemoryCapacity(ref handle, NativeMethods.IntPtrToInt32(size), doNotReallocate);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            ptr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                byte[] bytes;

                bytes = new byte[NativeMethods.IntPtrToInt32(size)];
                stream.Position = 0;
                stream.Read(bytes, 0, NativeMethods.IntPtrToInt32(size));
                Marshal.Copy(bytes, 0, ptr, NativeMethods.IntPtrToInt32(size));
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Save the System.Drawing.Bitmap or BitmapSource data to handle as BitmapSource.
        /// </summary>
        private int SaveSystemBitmapSourceToHandle(IntPtr handle, Object data, bool doNotReallocate)
        {
            BitmapSource bitmapSource;
            Stream bitmapStream;
            BitmapEncoder bitmapEncoder;

            bitmapSource = null;

            if (IsDataSystemBitmapSource(data))
            {
                bitmapSource = (BitmapSource)data;
            }
            else if (SystemDrawingHelper.IsBitmap(data))
            {
                // Create BitmapSource instance from System.Drawing.Bitmap
                IntPtr hbitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hbitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    null);
                Win32DeleteObject(new HandleRef(this, hbitmap));
            }

            Invariant.Assert(bitmapSource != null);

            // Get BitmapSource stream to save it as the handle
            bitmapEncoder = new BmpBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            bitmapStream = new MemoryStream();
            bitmapEncoder.Save(bitmapStream);

            return SaveStreamToHandle(handle, bitmapStream, doNotReallocate);
        }

        /// <summary>
        /// Save the System.Drawing.Bitmap or BitmapSource data to handle as System.Drawing.Bitmap.
        /// </summary>
        private int SaveSystemDrawingBitmapToHandle(IntPtr handle, Object data, bool doNotReallocate)
        {
            object systemDrawingBitmap = SystemDrawingHelper.GetBitmap(data);

            Invariant.Assert(systemDrawingBitmap != null);

            return SaveObjectToHandle(handle, systemDrawingBitmap, doNotReallocate);
        }

        /// <summary>
        /// Saves a list of files out to the handle in HDROP format.
        /// </summary>
        private int SaveFileListToHandle(IntPtr handle, string[] files, bool doNotReallocate)
        {
            IntPtr currentPtr;
            Int32 baseStructSize;
            Int32 sizeInBytes;
            IntPtr basePtr;
            int[] structData;

            if (files == null || files.Length < 1)
            {
                return NativeMethods.S_OK;
            }

            if (handle == IntPtr.Zero)
            {
                return (NativeMethods.E_INVALIDARG);
            }

            if (Marshal.SystemDefaultCharSize == 1)
            {
                Invariant.Assert(false, "Expected the system default char size to be 2 for Unicode systems.");
                return (NativeMethods.E_INVALIDARG);
            }

            currentPtr = IntPtr.Zero;
            baseStructSize = FILEDROPBASESIZE;
            sizeInBytes = baseStructSize;

            // First determine the size of the array
            for (int i = 0; i < files.Length; i++)
            {
                sizeInBytes += (files[i].Length + 1) * 2;
            }

            // Add the extra 2bytes since it is unicode.
            sizeInBytes += 2;

            int hr = EnsureMemoryCapacity(ref handle, sizeInBytes, doNotReallocate);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            basePtr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                currentPtr = basePtr;

                // Write out the struct...
                structData = new int[] { baseStructSize, 0, 0, 0, 0 };

                structData[4] = unchecked((int)0xFFFFFFFF);

                Marshal.Copy(structData, 0, currentPtr, structData.Length);

                currentPtr = (IntPtr)((long)currentPtr + baseStructSize);

                // Win32 CopyMemory return void, so we should disable PreSharp 6523 that
                // expects the Win32 exception with the last error.
#pragma warning disable 6523

                // Write out the strings...
                for (int i = 0; i < files.Length; i++)
                {
                    // Write out the each of file as the unicode and increase the pointer.
                    UnsafeNativeMethods.CopyMemoryW(currentPtr, files[i], files[i].Length * 2);
                    currentPtr = (IntPtr)((long)currentPtr + (files[i].Length * 2));

                    // Terminate the each of file string.
                    Marshal.Copy(new char[] { '\0' }, 0, currentPtr, 1);

                    // Increase the current pointer by 2 since it is a unicode.
                    currentPtr = (IntPtr)((long)currentPtr + 2);
                }

#pragma warning restore 6523

                // Terminate the string and add 2bytes since it is a unicode.
                Marshal.Copy(new char[] { '\0' }, 0, currentPtr, 1);
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Save string to handle. If unicode is set to true
        /// then the string is saved as unicode, else it is saves as DBCS.
        /// </summary>
        private int SaveStringToHandle(IntPtr handle, string str, bool unicode, bool doNotReallocate)
        {
            if (handle == IntPtr.Zero)
            {
                return (NativeMethods.E_INVALIDARG);
            }

            if (unicode)
            {
                Int32 byteSize;
                IntPtr ptr;
                char[] chars;

                byteSize = (str.Length*2 + 2);

                int hr = EnsureMemoryCapacity(ref handle, byteSize, doNotReallocate);
                if (NativeMethods.Failed(hr))
                {
                    return hr;
                }

                ptr = Win32GlobalLock(new HandleRef(this, handle));

                try
                {
                    chars = str.ToCharArray(0, str.Length);

                    // Win32 CopyMemory return void, so we should disable PreSharp 6523 that
                    // expects the Win32 exception with the last error.
#pragma warning disable 6523

                    UnsafeNativeMethods.CopyMemoryW(ptr, chars, chars.Length * 2);

#pragma warning restore 6523

                    // Terminate the string becasue of GlobalReAlloc GMEM_ZEROINIT will zero
                    // out only the bytes it adds to the memory object. It doesn't initialize
                    // any of the memory that existed before the call.
                    Marshal.Copy(new char[] { '\0' }, 0, (IntPtr)((ulong)ptr + (ulong)chars.Length * 2), 1);
                }
                finally
                {
                    Win32GlobalUnlock(new HandleRef(this, handle));
                }
            }
            else
            {
                Int32 pinvokeSize;
                byte[] strBytes;
                IntPtr ptr;

                // Convert the unicode text to the ansi multi byte in case of the source unicode is available.
                // WideCharToMultiByte will throw exception in case of passing 0 size of unicode.
                if (str.Length > 0)
                {
                    pinvokeSize = Win32WideCharToMultiByte(str, str.Length, null, 0);
                }
                else
                {
                    pinvokeSize = 0;
                }

                strBytes = new byte[pinvokeSize];

                if (pinvokeSize > 0)
                {
                    Win32WideCharToMultiByte(str, str.Length, strBytes, strBytes.Length);
                }

                // Ensure memory allocation and copy multi byte data with the null terminate
                int hr = EnsureMemoryCapacity(ref handle, pinvokeSize + 1, doNotReallocate);
                if (NativeMethods.Failed(hr))
                {
                    return hr;
                }

                ptr = Win32GlobalLock(new HandleRef(this, handle));

                try
                {
                    // Win32 CopyMemory return void, so we should disable PreSharp 6523 that
                    // expects the Win32 exception with the last error.
#pragma warning disable 6523

                    UnsafeNativeMethods.CopyMemory(ptr, strBytes, pinvokeSize);

#pragma warning restore 6523

                    Marshal.Copy(new byte[] { 0 }, 0, (IntPtr)((long)ptr + pinvokeSize), 1);
                }
                finally
                {
                    Win32GlobalUnlock(new HandleRef(this, handle));
                }
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        /// Save string to handle as UTF8 encoding.
        /// Html and Xaml data format will be save as UTF8 encoding.
        /// </summary>
        private int SaveStringToHandleAsUtf8(IntPtr handle, string str, bool doNotReallocate)
        {
            IntPtr pointerUtf8;
            byte[] utf8Bytes;
            int utf8ByteCount;
            UTF8Encoding utf8Encoding;

            if (handle == IntPtr.Zero)
            {
                return (NativeMethods.E_INVALIDARG);
            }

            // Create UTF8Encoding instance to convert the string to UFT8 from GetBytes.
            utf8Encoding = new UTF8Encoding();

            // Get the byte count to be UTF8 encoding.
            utf8ByteCount = utf8Encoding.GetByteCount(str);

            // Create byte array and assign UTF8 encoding.
            utf8Bytes = utf8Encoding.GetBytes(str);

            int hr = EnsureMemoryCapacity(ref handle, utf8ByteCount + 1, doNotReallocate);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            pointerUtf8 = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                // Win32 CopyMemory return void, so we should disable PreSharp 6523 that
                // expects the Win32 exception with the last error.
#pragma warning disable 6523

                // Copy UTF8 encoding bytes to the memory.
                UnsafeNativeMethods.CopyMemory(pointerUtf8, utf8Bytes, utf8ByteCount);

#pragma warning restore 6523

                // Copy the null into the last of memory.
                Marshal.Copy(new byte[] { 0 }, 0, (IntPtr)((long)pointerUtf8 + utf8ByteCount), 1);
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }

            return NativeMethods.S_OK;
        }

        /// <summary>
        ///     Return true if data is BitmapSource.
        /// </summary>
        private static bool IsDataSystemBitmapSource(object data)
        {
            if (data is System.Windows.Media.Imaging.BitmapSource)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the format/data combination is likely to
        /// be serialized. This is only likely, because this isn't a
        /// clone of the logic to determine the serialization code
        /// path. The idea is to determine if the format is likely
        /// to be serialized and do an early security check. If that
        /// check fails, then we will omit the format from the list
        /// of available clipboard formats.
        ///
        /// Note: at the time of serialization the correct security
        /// check will be performed, guaranteeing the security of
        /// the data, however that will cause the clipboard to report
        /// that there are no valid data items.
        /// </summary>
        /// <param name="format">Clipboard format to check</param>
        /// <param name="data">Clipboard data to check</param>
        /// <returns>
        /// true if the data is likely to be serializated
        /// through CLR serialization
        /// </returns>
        private static bool IsFormatAndDataSerializable(string format, object data)
        {
            return
                 IsFormatEqual(format, DataFormats.Serializable)
                  || data is ISerializable
                  || (data != null && data.GetType().IsSerializable);
        }


        /// <summary>
        /// Return true if the format string are equal(Case-senstive).
        /// </summary>
        private static bool IsFormatEqual(string format1, string format2)
        {
            return (String.CompareOrdinal(format1, format2) == 0);
        }

        /// <summary>
        /// Ensures that a memory block is sized to match a specified byte count.
        /// </summary>
        /// <remarks>
        /// Returns a pointer to the original memory block, a re-sized memory block,
        /// or null if the original block has insufficient capacity and doNotReallocate
        /// is true.
        ///
        /// Returns an HRESULT
        ///  S_OK: success.
        ///  STG_E_MEDIUMFULL: the original handle lacks capacity and doNotReallocate == true.  handle == null on exit.
        ///  E_OUTOFMEMORY: could not re-size the handle.  handle == null on exit.
        ///
        /// If doNotReallocate is false, this method will always realloc the original
        /// handle to fit minimumByteCount tightly.
        /// </remarks>
        private int EnsureMemoryCapacity(ref IntPtr handle, Int32 minimumByteCount, bool doNotReallocate)
        {
            int hr = NativeMethods.S_OK;

            if (doNotReallocate)
            {
                int byteCount = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));
                if (byteCount < minimumByteCount)
                {
                    handle = IntPtr.Zero;
                    hr = STG_E_MEDIUMFULL;
                }
            }
            else
            {
                handle = Win32GlobalReAlloc(new HandleRef(this, handle),
                                               (IntPtr)minimumByteCount,
                                               NativeMethods.GMEM_MOVEABLE
                                                | NativeMethods.GMEM_DDESHARE
                                                | NativeMethods.GMEM_ZEROINIT);

                if (handle == IntPtr.Zero)
                {
                    hr = NativeMethods.E_OUTOFMEMORY;
                }
            }

            return hr;
        }

        /// <summary>
        /// Ensure returning Bitmap(BitmapSource or System.Drawing.Bitmap) data that base
        /// on the passed Bitmap format parameter.
        /// Bitmap data will be converted if the data mismatch with the format in case of
        /// autoConvert is "true", but return null if autoConvert is "false".
        /// </summary>
        private static object EnsureBitmapDataFromFormat(string format, bool autoConvert, object data)
        {
            object bitmapData = data;

            if (IsDataSystemBitmapSource(data) && IsFormatEqual(format, SystemDrawingBitmapFormat))
            {
                // Data is BitmapSource, but have the mismatched System.Drawing.Bitmap format
                if (autoConvert)
                {

                    // Convert data from BitmapSource to SystemDrawingBitmap
                    bitmapData = SystemDrawingHelper.GetBitmap(data);
                }
                else
                {
                    bitmapData = null;
                }
            }
            else if (SystemDrawingHelper.IsBitmap(data) &&
                     (IsFormatEqual(format, DataFormats.Bitmap) || IsFormatEqual(format, SystemBitmapSourceFormat)))
            {
                // Data is System.Drawing.Bitmap, but have the mismatched BitmapSource format
                if (autoConvert)
                {
                    // Create BitmapSource instance from System.Drawing.Bitmap
                    IntPtr hbitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
                    bitmapData = Imaging.CreateBitmapSourceFromHBitmap(
                        hbitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        null);
                    Win32DeleteObject(new HandleRef(null, hbitmap));
                }
                else
                {
                    bitmapData = null;
                }
            }

            return bitmapData;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const string SystemDrawingBitmapFormat = "System.Drawing.Bitmap";
        private const string SystemBitmapSourceFormat = "System.Windows.Media.Imaging.BitmapSource";
        private const string SystemDrawingImagingMetafileFormat = "System.Drawing.Imaging.Metafile";

        private const int DV_E_FORMATETC     =       unchecked((int)0x80040064);
        private const int DV_E_LINDEX        =       unchecked((int)0x80040068);
        private const int DV_E_TYMED         =       unchecked((int)0x80040069);
        private const int DV_E_DVASPECT      =       unchecked((int)0x8004006B);
        private const int OLE_E_NOTRUNNING   =       unchecked((int)0x80040005);
        private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
        private const int DATA_S_SAMEFORMATETC =     unchecked((int)0x00040130);
        private const int STG_E_MEDIUMFULL   =       unchecked((int)0x80030070);

        // Const integer base size of the file drop list: "4 + 8 + 4 + 4"
        private const int FILEDROPBASESIZE   = 20;

        // Allowed type medium.
        private static readonly TYMED[] ALLOWED_TYMEDS = new TYMED[]
        {
            TYMED.TYMED_HGLOBAL,
            TYMED.TYMED_ISTREAM,
            TYMED.TYMED_ENHMF,
            TYMED.TYMED_MFPICT,
            TYMED.TYMED_GDI
        };


        // Inner data object of IDataObject.
        private System.Windows.IDataObject _innerData;

        // We use this to identify that a stream is actually a serialized object.  On read,
        // we don't know if the contents of a stream were saved "raw" or if the stream is really
        // pointing to a serialized object.  If we saved an object, we prefix it with this
        // guid.
        //
        // FD9EA796-3B13-4370-A679-56106BB288FB
        //
        private static readonly byte[] _serializedObjectID = new Guid(0xFD9EA796, 0x3B13, 0x4370, 0xA6, 0x79, 0x56, 0x10, 0x6B, 0xB2, 0x88, 0xFB).ToByteArray();

        #endregion Private Fields


        #region FormatEnumerator Class

        /// <summary>
        /// IEnumFORMATETC implementation for DataObject.
        /// </summary>
        private class FormatEnumerator : IEnumFORMATETC
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            // Creates a new enumerator instance.
            internal FormatEnumerator(DataObject dataObject)
            {
                FORMATETC temp;
                string[] formats;

                formats = dataObject.GetFormats();
                _formats = new FORMATETC[formats == null ? 0 : formats.Length];

                if (formats != null)
                {
                    for (int i = 0; i < formats.Length; i++)
                    {
                        string format;

                        format = formats[i];
                        temp = new FORMATETC();
                        temp.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
                        temp.dwAspect = DVASPECT.DVASPECT_CONTENT;
                        temp.ptd = IntPtr.Zero;
                        temp.lindex = -1;

                        if (IsFormatEqual(format, DataFormats.Bitmap))
                        {
                            temp.tymed = TYMED.TYMED_GDI;
                        }
                        else if (IsFormatEqual(format, DataFormats.EnhancedMetafile))
                        {
                            temp.tymed = TYMED.TYMED_ENHMF;
                        }
                        else
                        {
                            temp.tymed = TYMED.TYMED_HGLOBAL;
                        }

                        _formats[i] = temp;
                    }
                }
            }

            // Copy constructor.  Used by the Clone method.
            private FormatEnumerator(FormatEnumerator formatEnumerator)
            {
                _formats = formatEnumerator._formats;
                _current = formatEnumerator._current;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            // IEnumFORMATETC.Next implementation.
            public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
            {
                int fetched = 0;

                if (rgelt == null)
                {
                    return NativeMethods.E_INVALIDARG;
                }

                for (int i = 0; i < celt && _current < _formats.Length; i++)
                {
                    rgelt[i] = _formats[this._current];
                    _current++;
                    fetched++;
                }

                if (pceltFetched != null)
                {
                    pceltFetched[0] = fetched;
                }

                return (fetched == celt) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
            }

            // IEnumFORMATETC.Skip implementation.
            public int Skip(int celt)
            {
                // Make sure we don't overflow on the skip.
                _current = _current + (int)Math.Min(celt, Int32.MaxValue - _current);

                return (_current < _formats.Length) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
            }

            // IEnumFORMATETC.Reset implementation.
            public int Reset()
            {
                _current = 0;
                return NativeMethods.S_OK;
            }

            // IEnumFORMATETC.Clone implementation.
            public void Clone(out IEnumFORMATETC ppenum)
            {
                ppenum = new FormatEnumerator(this);
            }

            #endregion Public Methods

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // List of FORMATETC to enumerate.
            private readonly FORMATETC[] _formats;

            // Current offset of the enumerator.
            private int _current;

            #endregion Private Fields
        }

        #endregion FormatEnuerator Class

        #region OleConverter Class

        /// <summary>
        /// OLE Converter.  This class embodies the nastiness required to convert from our
        /// managed types to standard OLE clipboard formats.
        /// </summary>
        private class OleConverter : IDataObject
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            public OleConverter(IComDataObject data)
            {
                _innerData = data;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            //=------------------------------------------------------------------------=
            // System.Windows.IDataObject
            //=------------------------------------------------------------------------=
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

                IEnumFORMATETC enumFORMATETC;
                ArrayList formats;
                string[] temp;

                enumFORMATETC = null;
                formats = new ArrayList();

                enumFORMATETC = EnumFormatEtcInner(DATADIR.DATADIR_GET);

                if (enumFORMATETC != null)
                {
                    FORMATETC []formatetc;
                    int[] retrieved;

                    enumFORMATETC.Reset();

                    formatetc = new FORMATETC[] { new FORMATETC() };
                    retrieved = new int[] {1};

                    while (retrieved[0] > 0)
                    {
                        retrieved[0] = 0;

                        if (enumFORMATETC.Next(1, formatetc, retrieved) == NativeMethods.S_OK && retrieved[0] > 0)
                        {
                            string name;

                            name = DataFormats.GetDataFormat(formatetc[0].cfFormat).Name;
                            if (autoConvert)
                            {
                                string[] mappedFormats;

                                mappedFormats = GetMappedFormats(name);
                                for (int i=0; i<mappedFormats.Length; i++)
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

                temp = new string[formats.Count];
                formats.CopyTo(temp, 0);
                return GetDistinctStrings(temp);
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

            #endregion Public Methods

            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

            #region Public Properties

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

            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Internal Fields
            //
            //------------------------------------------------------

            #region Internal Fields

            internal IComDataObject _innerData;

            #endregion Internal Fields

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

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
                throw new InvalidOperationException(SR.Get(SRID.DataObject_CannotSetDataOnAFozenOLEDataDbject));
            }

            /// <summary>
            /// Uses IStream and retrieves the specified format from the bound IComDataObject.
            /// </summary>
            private Object GetDataFromOleIStream(string format, DVASPECT aspect, int index)
            {
                FORMATETC formatetc;
                STGMEDIUM medium;

                formatetc = new FORMATETC();

                formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
                formatetc.dwAspect = aspect;
                formatetc.lindex = index;
                formatetc.tymed = TYMED.TYMED_ISTREAM;

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
                    else if (IsFormatEqual(format, DataFormats.UnicodeText) ||
                             IsFormatEqual(format, DataFormats.ApplicationTrust))
                    {
                        data = ReadStringFromHandle(hglobal, true);
                    }
                    else if (IsFormatEqual(format, DataFormats.FileDrop))
                    {
                        data = (object)ReadFileListFromHandle(hglobal);
                    }
                    else if (IsFormatEqual(format, DataFormats.FileName))
                    {
                        data = new string[] { ReadStringFromHandle(hglobal, false) };
                    }
                    else if (IsFormatEqual(format, DataFormats.FileNameW))
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

                formatetc = new FORMATETC();

                formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
                formatetc.dwAspect = aspect;
                formatetc.lindex = index;
                formatetc.tymed = TYMED.TYMED_HGLOBAL;

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
            private Object ReadObjectFromHandle(IntPtr handle, bool restrictDeserialization)
            {
                object value;
                bool isSerializedObject;
                Stream stream;

                value = null;

                stream = ReadByteStreamFromHandle(handle, out isSerializedObject);

                if (isSerializedObject)
                {
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
                    }
                }
                else
                {
                    value = stream;
                }

                return value;
            }

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

                formatetc = new FORMATETC();
                formatetc.cfFormat = (short)DataFormats.GetDataFormat(format).Id;
                formatetc.dwAspect = aspect;
                formatetc.lindex = index;

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

            #endregion Private Methods
            
            /// <summary>
            /// This class is meant to restrict deserialization of managed objects during Ole conversion to only strings and arrays of primitives. 
            /// A RestrictedTypeDeserializationException is thrown upon calling BinaryFormatter.Deserialized if a binder of this type is provided to the BinaryFormatter.
            /// </summary>
            private class TypeRestrictingSerializationBinder : SerializationBinder
            {
                public TypeRestrictingSerializationBinder()
                {
                }

                public override Type BindToType(string assemblyName, string typeName)
                {
                    throw new RestrictedTypeDeserializationException();
                }
            }

            /// <summary>
            /// Private exception to signal when a restricted type was encountered during deserialization.
            /// </summary>
            private class RestrictedTypeDeserializationException : Exception
            {
            }
        }

        #endregion OleConverter Class


        #region DataStore Class

        /// <summary>
        /// DataStore
        /// </summary>
        private class DataStore : IDataObject
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            public DataStore()
            {
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

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

            public string[] GetFormats(bool autoConvert)
            {
                string[] baseVar;

                //************************************************************
                // important! this is cached security information. It will
                //            remain valid for this function, but do not
                //            let this value leak outside of this function.
                //
                bool serializationCheckFailedForThisFunction = false;

                baseVar = new string[_data.Keys.Count];

                _data.Keys.CopyTo(baseVar, 0);

                if (autoConvert)
                {
                    ArrayList formats;
                    string[] temp;

                    formats = new ArrayList();

                    for (int baseFormatIndex = 0; baseFormatIndex < baseVar.Length; baseFormatIndex++)
                    {
                        DataStoreEntry[] entries;
                        bool canAutoConvert;

                        entries = (DataStoreEntry[])_data[baseVar[baseFormatIndex]];
                        canAutoConvert = true;

                        for (int dataStoreIndex = 0; dataStoreIndex < entries.Length; dataStoreIndex++)
                        {
                            if (!entries[dataStoreIndex].AutoConvert)
                            {
                                canAutoConvert = false;
                                break;
                            }
                        }

                        if (canAutoConvert)
                        {
                            string[] cur;

                            cur = GetMappedFormats(baseVar[baseFormatIndex]);
                            for (int mappedFormatIndex = 0; mappedFormatIndex < cur.Length; mappedFormatIndex++)
                            {
                                bool anySerializationFailure = false;
                                for (int dataStoreIndex = 0;
                                    !anySerializationFailure
                                      &&
                                    dataStoreIndex < entries.Length;
                                    dataStoreIndex++)
                                {
                                    if (DataObject.IsFormatAndDataSerializable(cur[mappedFormatIndex], entries[dataStoreIndex].Data))
                                    {
                                        if (serializationCheckFailedForThisFunction)
                                        {
                                            serializationCheckFailedForThisFunction = true;
                                            anySerializationFailure = true;
                                        }
                                    }
                                }
                                if (!anySerializationFailure)
                                {
                                    formats.Add(cur[mappedFormatIndex]);
                                }
}
                        }
                        else
                        {
                             if (!serializationCheckFailedForThisFunction)
                            {
                                formats.Add(baseVar[baseFormatIndex]);
                            }
                        }
                    }

                    temp = new string[formats.Count];
                    formats.CopyTo(temp, 0);
                    baseVar = GetDistinctStrings(temp);
                }

                return baseVar;
            }

            public void SetData(Object data)
            {
                if (data is ISerializable
                    && !this._data.ContainsKey(DataFormats.Serializable))
                {
                    SetData(DataFormats.Serializable, data);
                }

                SetData(data.GetType(), data);
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
                // We do not have proper support for Dibs, so if the user explicitly asked
                // for Dib and provided a Bitmap object we can't convert.  Instead, publish as an HBITMAP
                // and let the system provide the conversion for us.
                //
                if (IsFormatEqual(format, DataFormats.Dib))
                {
                    if (autoConvert && (SystemDrawingHelper.IsBitmap(data) || IsDataSystemBitmapSource(data)))
                    {
                        format = DataFormats.Bitmap;
                    }
                }

                SetData(format, data, autoConvert, DVASPECT.DVASPECT_CONTENT, 0);
            }

            #endregion Public Methods

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private Object GetData(string format, bool autoConvert, DVASPECT aspect, int index)
            {
                Object baseVar;
                Object original;
                DataStoreEntry dataStoreEntry;

                dataStoreEntry = FindDataStoreEntry(format, aspect, index);

                baseVar = GetDataFromDataStoreEntry(dataStoreEntry, format);

                original = baseVar;

                if (autoConvert
                    && (dataStoreEntry == null || dataStoreEntry.AutoConvert)
                    && (baseVar == null || baseVar is MemoryStream))
                {
                    string[] mappedFormats;

                    mappedFormats = GetMappedFormats(format);
                    if (mappedFormats != null)
                    {
                        for (int i = 0; i < mappedFormats.Length; i++)
                        {
                            if (!IsFormatEqual(format, mappedFormats[i]))
                            {
                                DataStoreEntry foundDataStoreEntry;

                                foundDataStoreEntry = FindDataStoreEntry(mappedFormats[i], aspect, index);

                                baseVar = GetDataFromDataStoreEntry(foundDataStoreEntry, mappedFormats[i]);

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
                if (!autoConvert)
                {
                    DataStoreEntry[] entries;
                    DataStoreEntry dse;
                    DataStoreEntry naturalDse;

                    if (!_data.ContainsKey(format))
                    {
                        return false;
                    }

                    entries = (DataStoreEntry[])_data[format];
                    dse = null;
                    naturalDse = null;

                    // Find the entry with the given aspect and index
                    for (int i = 0; i < entries.Length; i++)
                    {
                        DataStoreEntry entry;

                        entry = entries[i];
                        if (entry.Aspect == aspect)
                        {
                            if (index == -1 || entry.Index == index)
                            {
                                dse = entry;
                                break;
                            }
                        }
                        if (entry.Aspect == DVASPECT.DVASPECT_CONTENT && entry.Index == 0)
                        {
                            naturalDse = entry;
                        }
                    }

                    // If we couldn't find a specific entry, we'll use
                    // aspect == Content and index == 0.
                    if (dse == null && naturalDse != null)
                    {
                        dse = naturalDse;
                    }

                    // If we still didn't find data, return false.
                    return (dse != null);
                }
                else
                {
                    string[] formats;

                    formats = GetFormats(autoConvert);
                    for (int i = 0; i < formats.Length; i++)
                    {
                        if (IsFormatEqual(format, formats[i]))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            private void SetData(string format, Object data, bool autoConvert, DVASPECT aspect, int index)
            {
                DataStoreEntry dse;
                DataStoreEntry[] datalist;

                dse = new DataStoreEntry(data, autoConvert, aspect, index);
                datalist = (DataStoreEntry[])this._data[format];

                if (datalist == null)
                {
                    datalist = (DataStoreEntry[])Array.CreateInstance(typeof(DataStoreEntry), 1);
                }
                else
                {
                    DataStoreEntry[] newlist;

                    newlist = (DataStoreEntry[])Array.CreateInstance(typeof(DataStoreEntry), datalist.Length + 1);
                    datalist.CopyTo(newlist, 1);
                    datalist = newlist;
                }

                datalist[0] = dse;
                this._data[format] = datalist;
            }

            private DataStoreEntry FindDataStoreEntry(string format, DVASPECT aspect, int index)
            {
                DataStoreEntry[] dataStoreEntries;
                DataStoreEntry dataStoreEntry;
                DataStoreEntry naturalDataStoreEntry;

                dataStoreEntries = (DataStoreEntry[])_data[format];
                dataStoreEntry = null;
                naturalDataStoreEntry = null;

                // Find the entry with the given aspect and index
                if (dataStoreEntries != null)
                {
                    for (int i = 0; i < dataStoreEntries.Length; i++)
                    {
                        DataStoreEntry entry;

                        entry = dataStoreEntries[i];
                        if (entry.Aspect == aspect)
                        {
                            if (index == -1 || entry.Index == index)
                            {
                                dataStoreEntry = entry;
                                break;
                            }
                        }
                        if (entry.Aspect == DVASPECT.DVASPECT_CONTENT && entry.Index == 0)
                        {
                            naturalDataStoreEntry = entry;
                        }
                    }
                }

                // If we couldn't find a specific entry, we'll use
                // aspect == Content and index == 0.
                if (dataStoreEntry == null && naturalDataStoreEntry != null)
                {
                    dataStoreEntry = naturalDataStoreEntry;
                }

                return dataStoreEntry;
            }

            private Object GetDataFromDataStoreEntry(DataStoreEntry dataStoreEntry, string format)
            {
                Object data;

                data = null;
                if (dataStoreEntry != null)
                {
                    data = dataStoreEntry.Data;
                }

                return data;
            }

            #endregion Private Methods

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // Data hash table.
            private Hashtable _data = new Hashtable();

            #endregion Private Fields


            #region DataStoreEntry Class

            private class DataStoreEntry
            {
                //------------------------------------------------------
                //
                //  Constructors
                //
                //------------------------------------------------------

                #region Constructors

                public DataStoreEntry(Object data, bool autoConvert, DVASPECT aspect, int index)
                {
                    this._data = data;
                    this._autoConvert = autoConvert;
                    this._aspect = aspect;
                    this._index = index;
                }

                #endregion Constructors

                //------------------------------------------------------
                //
                //  Public Properties
                //
                //------------------------------------------------------

                #region Public Properties

                // Data object property.
                public Object Data
                {
                    get { return _data; }
                    set { _data = value; }
                }

                // Auto convert proeprty.
                public bool AutoConvert
                {
                    get { return _autoConvert; }
                }

                // Aspect flag property.
                public DVASPECT Aspect
                {
                    get { return _aspect; }
                }

                // Index property.
                public int Index
                {
                    get { return _index; }
                }

                #endregion Public Properties

                //------------------------------------------------------
                //
                //  Private Fields
                //
                //------------------------------------------------------

                #region Private Fields

                private Object _data;
                private bool _autoConvert;
                private DVASPECT _aspect;
                private int _index;

                #endregion Private Fields
            }

            #endregion DataStoreEntry Class
        }
        #endregion DataStore Class

    }

    #endregion DataObject Class
}
