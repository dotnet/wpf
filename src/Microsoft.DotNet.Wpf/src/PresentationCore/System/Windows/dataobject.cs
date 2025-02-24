// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Win32;
using System.Collections.Specialized;
using System.ComponentModel;
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

// Description: Top-level class for data transfer for drag-drop and clipboard.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm

namespace System.Windows;

#region DataObject Class
/// <summary>
/// Implements a basic data transfer mechanism.
/// </summary>
public sealed partial class DataObject : IDataObject, IComDataObject
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
        ArgumentNullException.ThrowIfNull(data);

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
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        ArgumentNullException.ThrowIfNull(data);

        _innerData = new DataStore();
        SetData(format, data);
    }

    /// <summary>
    /// Initializes a new instance of the class, containing the specified data and its
    /// associated format.
    /// </summary>
    public DataObject(Type format, object data)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(data);
        _innerData = new DataStore();
        SetData(format.FullName, data);
    }

    /// <summary>
    /// Initializes a new instance of the class, containing the specified data and its
    /// associated format.
    /// </summary>
    public DataObject(string format, object data, bool autoConvert)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        ArgumentNullException.ThrowIfNull(data);

        _innerData = new DataStore();
        SetData(format, data, autoConvert);
    }

    /// <summary>
    /// Initializes a new instance of the class, with the specified
    /// </summary>
    internal DataObject(System.Windows.IDataObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _innerData = data;
    }

    /// <summary>
    /// Initializes a new instance of the class, with the specified
    /// </summary>
    internal DataObject(IComDataObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

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
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        return _innerData.GetData(format, autoConvert);
    }

    /// <summary>
    /// Retrieves the data associated with the specified data
    /// format.
    /// </summary>
    public object GetData(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        return GetData(format, true);
    }

    /// <summary>
    /// Retrieves the data associated with the specified class
    /// type format.
    /// </summary>
    public object GetData(Type format)
    {
        ArgumentNullException.ThrowIfNull(format);
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
        ArgumentNullException.ThrowIfNull(format);
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

        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
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
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
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
        ArgumentNullException.ThrowIfNull(data);
        _innerData.SetData(data);
    }

    /// <summary>
    /// Stores the specified data and its associated format in this
    /// instance.
    /// </summary>
    public void SetData(string format, object data)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        ArgumentNullException.ThrowIfNull(data);

        _innerData.SetData(format, data);
    }

    /// <summary>
    /// Stores the specified data and
    /// its associated class type in this instance.
    /// </summary>
    public void SetData(Type format, object data)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(data);

        _innerData.SetData(format, data);
    }

    /// <summary>
    /// Stores the specified data and its associated format in
    /// this instance, using the automatic conversion parameter
    /// to specify whether the
    /// data can be converted to another format.
    /// </summary>
    public void SetData(string format, Object data, bool autoConvert)
    {
        ArgumentNullException.ThrowIfNull(format);

        if (format == string.Empty)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed);
        }

        ArgumentNullException.ThrowIfNull(data);

        _innerData.SetData(format, data, autoConvert);
    }


        /// <summary>
        /// Return true if DataObject contains the audio data. Otherwise, return false.
        /// </summary>
        public bool ContainsAudio()
        {
            return GetDataPresent(DataFormats.WaveAudio, autoConvert: false);
        }

        /// <summary>
        /// Return true if DataObject contains the file drop list data. Otherwise, return false.
        /// </summary>
        public bool ContainsFileDropList()
        {
            return GetDataPresent(DataFormats.FileDrop, autoConvert: false);
        }

        /// <summary>
        /// Return true if DataObject contains the image data. Otherwise, return false.
        /// </summary>
        public bool ContainsImage()
        {
            return GetDataPresent(DataFormats.Bitmap, autoConvert: false);
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

            return GetDataPresent(DataFormats.ConvertToDataFormats(format), autoConvert: false);
        }

        /// <summary>
        /// Get audio data as Stream.
        /// </summary>
        public Stream GetAudioStream()
        {
            return GetData(DataFormats.WaveAudio, autoConvert: false) as Stream;
        }

    /// <summary>
    /// Get file drop list data as Stream.
    /// </summary>
    public StringCollection GetFileDropList()
    {
        StringCollection fileDropListCollection;
        string[] fileDropList;

        fileDropListCollection = new StringCollection();

            fileDropList = GetData(DataFormats.FileDrop, autoConvert: true) as string[];
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
            return GetData(DataFormats.Bitmap, autoConvert: true) as BitmapSource;
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
        ArgumentNullException.ThrowIfNull(audioBytes);

        SetAudio(new MemoryStream(audioBytes));
    }

    /// <summary>
    /// Set the audio data with Stream.
    /// </summary>
    public void SetAudio(Stream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);

            SetData(DataFormats.WaveAudio, audioStream, autoConvert: false);
        }

    /// <summary>
    /// Set the file drop list data.
    /// </summary>
    public void SetFileDropList(StringCollection fileDropList)
    {
        ArgumentNullException.ThrowIfNull(fileDropList);

        if (fileDropList.Count == 0)
        {
            throw new ArgumentException(SR.Format(SR.DataObject_FileDropListIsEmpty, fileDropList));
        }

        foreach (string fileDrop in fileDropList)
        {
            try
            {
                string filePath = Path.GetFullPath(fileDrop);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(SR.Format(SR.DataObject_FileDropListHasInvalidFileDropPath, e));
            }
        }

        string[] fileDropListStrings;

        fileDropListStrings = new string[fileDropList.Count];
        fileDropList.CopyTo(fileDropListStrings, 0);

            SetData(DataFormats.FileDrop, fileDropListStrings, autoConvert: true);
        }

    /// <summary>
    /// Set the image data with BitmapSource.
    /// </summary>
    public void SetImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);

            SetData(DataFormats.Bitmap, image, autoConvert: true);
        }

    /// <summary>
    /// Set the text data.
    /// </summary>
    public void SetText(string textData)
    {
        ArgumentNullException.ThrowIfNull(textData);

        SetText(textData, TextDataFormat.UnicodeText);
    }

    /// <summary>
    /// Set the text data for the specified text data format.
    /// </summary>
    public void SetText(string textData, TextDataFormat format)
    {
        ArgumentNullException.ThrowIfNull(textData);

        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException("format", (int)format, typeof(TextDataFormat));
        }

            SetData(DataFormats.ConvertToDataFormats(format), textData, autoConvert: false);
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
            throw new ExternalException(SR.Format(SR.DataObject_NotImplementedEnumFormatEtc, dwDirection), NativeMethods.E_NOTIMPL);
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

                    hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: false);

                if (NativeMethods.Failed(hr))
                {
                    Win32GlobalFree(new HandleRef(this, medium.unionmember));
                }
            }
            else if ( ( formatetc.tymed & TYMED.TYMED_ISTREAM ) != 0 )
            {
                medium.tymed = TYMED.TYMED_ISTREAM;

                    IStream istream = null;
                    hr = Win32CreateStreamOnHGlobal(IntPtr.Zero, fDeleteOnRelease: true, ref istream);
                    if ( NativeMethods.Succeeded(hr) )
                    {
                        medium.unionmember = Marshal.GetComInterfaceForObject(istream, typeof(IStream));
                        Marshal.ReleaseComObject(istream);

                        hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: false);

                        if ( NativeMethods.Failed(hr) )
                        {
                            Marshal.Release(medium.unionmember);
                        }
                    }
                }
                else
                {
                    medium.tymed = formatetc.tymed;
                    hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: false);
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

            int hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: true);
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
            int win32Return = UnsafeNativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, flags: 0, wideString, wideChars, bytes, byteCount, IntPtr.Zero, IntPtr.Zero);
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
            || IsFormatEqual(format, DataFormatNames.FileNameAnsi)
            || IsFormatEqual(format, DataFormatNames.FileNameUnicode))
        {
            return new string[] {
                                    DataFormats.FileDrop,
                                    DataFormatNames.FileNameUnicode,
                                    DataFormatNames.FileNameAnsi,
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
    private static string[] GetDistinctStrings(List<string> formats)
    {
        List<string> distinct = new(formats.Count);

        for (int i = 0; i < formats.Count; i++)
        {
            string formatString = formats[i];

            if (!distinct.Contains(formatString))
            {
                distinct.Add(formatString);
            }
        }

        return distinct.ToArray();
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
                hr = SaveStringToHandle(medium.unionmember, data.ToString(), unicode: false, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.UnicodeText))
            {
                hr = SaveStringToHandle(medium.unionmember, data.ToString(), unicode: true, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormats.FileDrop))
            {
                hr = SaveFileListToHandle(medium.unionmember, (string[])data, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormatNames.FileNameAnsi))
            {
                string[] filelist;

                filelist = (string[])data;
                hr = SaveStringToHandle(medium.unionmember, filelist[0], unicode: false, doNotReallocate);
            }
            else if (IsFormatEqual(format, DataFormatNames.FileNameUnicode))
            {
                string[] filelist;

                filelist = (string[])data;
                hr = SaveStringToHandle(medium.unionmember, filelist[0], unicode: true, doNotReallocate);
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
#pragma warning disable SYSLIB0050
        else if (IsFormatEqual(format, DataFormats.Serializable)
            || data is ISerializable
            || (data != null && data.GetType().IsSerializable))
        {
            hr = SaveObjectToHandle(medium.unionmember, data, doNotReallocate);
        }
#pragma warning restore SYSLIB0050
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
                    inkStream.ReadExactly(buffer);

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
#pragma warning disable SYSLIB0011 // Type or member is obsolete
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
                bool success = false;
                try
                {
                    success = BinaryFormatWriter.TryWriteFrameworkObject(stream,data);
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    // Being extra cautious here, but the Try method above should never throw in normal circumstances.
                    Debug.Fail($"Unexpected exception writing binary formatted data. {ex.Message}");
                }

                if(!success)
                {
                    //Using Binary formatter
                    formatter = new BinaryFormatter();
                    #pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete 
                    formatter.Serialize(stream, data);
                    #pragma warning restore SYSLIB0011 // BinaryFormatter is obsolete
                }
                return SaveStreamToHandle(handle, stream, doNotReallocate);
            }
        }
    }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

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
            stream.ReadExactly(bytes);
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

            // Write out the strings...
            for (int i = 0; i < files.Length; i++)
            {
                // Write out the each of file as the unicode and increase the pointer.
                UnsafeNativeMethods.CopyMemoryW(currentPtr, files[i], files[i].Length * 2);
                currentPtr = (IntPtr)((long)currentPtr + (files[i].Length * 2));

                // Terminate the each of file string.
                unsafe
                {
                    *(char*)currentPtr = '\0';
                }

                // Increase the current pointer by 2 since it is a unicode.
                currentPtr = (IntPtr)((long)currentPtr + 2);
            }

            // Terminate the string and add 2bytes since it is a unicode.
            unsafe
            {
                *(char*)currentPtr = '\0';
            }
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

                UnsafeNativeMethods.CopyMemoryW(ptr, chars, chars.Length * 2);

                // Terminate the string becasue of GlobalReAlloc GMEM_ZEROINIT will zero
                // out only the bytes it adds to the memory object. It doesn't initialize
                // any of the memory that existed before the call.
                unsafe
                {
                    *(char*)(IntPtr)((ulong)ptr + (ulong)chars.Length * 2) = '\0';
                }
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
                UnsafeNativeMethods.CopyMemory(ptr, strBytes, pinvokeSize);

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
            // Copy UTF8 encoding bytes to the memory.
            UnsafeNativeMethods.CopyMemory(pointerUtf8, utf8Bytes, utf8ByteCount);

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
#pragma warning disable SYSLIB0050
    private static bool IsFormatAndDataSerializable(string format, object data)
    {
        return
             IsFormatEqual(format, DataFormats.Serializable)
              || data is ISerializable
              || (data != null && data.GetType().IsSerializable);
    }
#pragma warning restore SYSLIB0050

    /// <summary>
    /// Return true if the format string are equal(Case-senstive).
    /// </summary>
    private static bool IsFormatEqual(string format1, string format2)
    {
        return (string.Equals(format1, format2, StringComparison.Ordinal));
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
}

#endregion DataObject Class
