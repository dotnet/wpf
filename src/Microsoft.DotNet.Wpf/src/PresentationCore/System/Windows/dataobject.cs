// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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
using System.Diagnostics.CodeAnalysis;

// Description: Top-level class for data transfer for drag-drop and clipboard.
//
// See spec at http://avalon/uis/Data%20Transfer%20clipboard%20dragdrop/Avalon%20Data%20Transfer%20Object.htm

namespace System.Windows;

public sealed partial class DataObject : IDataObject, IComDataObject
{
    private const string SystemDrawingBitmapFormat = "System.Drawing.Bitmap";
    private const string SystemBitmapSourceFormat = "System.Windows.Media.Imaging.BitmapSource";
    private const string SystemDrawingImagingMetafileFormat = "System.Drawing.Imaging.Metafile";

    private const int DV_E_FORMATETC = unchecked((int)0x80040064);
    private const int DV_E_LINDEX = unchecked((int)0x80040068);
    private const int DV_E_TYMED = unchecked((int)0x80040069);
    private const int DV_E_DVASPECT = unchecked((int)0x8004006B);
    private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
    private const int DATA_S_SAMEFORMATETC = unchecked((int)0x00040130);
    private const int STG_E_MEDIUMFULL = unchecked((int)0x80030070);

    // Const integer base size of the file drop list: "4 + 8 + 4 + 4"
    private const int FILEDROPBASESIZE = 20;

    // Allowed type medium.
    private static readonly TYMED[] s_allowedTymeds =
    [
        TYMED.TYMED_HGLOBAL,
        TYMED.TYMED_ISTREAM,
        TYMED.TYMED_ENHMF,
        TYMED.TYMED_MFPICT,
        TYMED.TYMED_GDI
    ];


    // Inner data object of IDataObject.
    private readonly IDataObject _innerData;

    // We use this to identify that a stream is actually a serialized object.  On read,
    // we don't know if the contents of a stream were saved "raw" or if the stream is really
    // pointing to a serialized object.  If we saved an object, we prefix it with this
    // guid.
    //
    // FD9EA796-3B13-4370-A679-56106BB288FB
    private static readonly byte[] s_serializedObjectID = new Guid(0xFD9EA796, 0x3B13, 0x4370, 0xA6, 0x79, 0x56, 0x10, 0x6B, 0xB2, 0x88, 0xFB).ToByteArray();

    /// <summary>
    ///  Initializes a new instance of the <see cref="DataObject"/> class, which can store arbitrary data.
    /// </summary>
    public DataObject() => _innerData = new DataStore();

    /// <summary>
    ///  Initializes a new instance of the <see cref="DataObject"/> class, containing the specified data.
    /// </summary>
    public DataObject(object data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data is IDataObject dataObject)
        {
            _innerData = dataObject;
        }
        else
        {
            if (data is IComDataObject oleDataObject)
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
    ///  Initializes a new instance of the class, containing the specified data and its
    ///  associated format.
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
    ///  Initializes a new instance of the class, containing the specified data and its associated format.
    /// </summary>
    public DataObject(Type format, object data)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(data);
        _innerData = new DataStore();
        SetData(format.FullName!, data);
    }

    /// <summary>
    ///  Initializes a new instance of the class, containing the specified data and its associated format.
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
    ///  Initializes a new instance of the class, with the specified
    /// </summary>
    internal DataObject(IDataObject data)
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

    /// <summary>
    ///  Retrieves the data associated with the specified data format, using an automated conversion parameter to
    ///  determine whether to convert the data to the format.
    /// </summary>
    public object? GetData(string format, bool autoConvert)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return _innerData.GetData(format, autoConvert);
    }

    /// <summary>
    ///  Retrieves the data associated with the specified data format.
    /// </summary>
    public object? GetData(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return GetData(format, autoConvert: true);
    }

    /// <summary>
    ///  Retrieves the data associated with the specified class type format.
    /// </summary>
    public object? GetData(Type format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return GetData(format.FullName!);
    }

    /// <summary>
    ///  Determines whether data stored in this instance is associated with, or can be converted to, the specified format.
    /// </summary>
    public bool GetDataPresent(Type format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return GetDataPresent(format.FullName!);
    }

    /// <summary>
    ///  Determines whether data stored in this instance is associated with the specified format, using an automatic
    ///  conversion parameter to determine whether to convert the data to the format.
    /// </summary>
    public bool GetDataPresent(string format, bool autoConvert)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return _innerData.GetDataPresent(format, autoConvert);
    }

    /// <summary>
    ///  Determines whether data stored in this instance is associated with, or can be converted to, the specified format.
    /// </summary>
    public bool GetDataPresent(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return GetDataPresent(format, autoConvert: true);
    }

    /// <summary>
    ///  Gets a list of all formats that data stored in this instance is associated with or can be converted to, using
    ///  an automatic conversion parameter <paramref name="autoConvert"/> to determine whether to retrieve all formats
    ///  that the data can be converted to or only native data formats.
    /// </summary>
    public string[] GetFormats(bool autoConvert) => _innerData.GetFormats(autoConvert);

    /// <summary>
    ///  Gets a list of all formats that data stored in this instance is associated with or can be converted to.
    /// </summary>
    public string[] GetFormats() => GetFormats(autoConvert: true);

    /// <summary>
    ///  Stores the specified data in this instance, using the class of the data for the format.
    /// </summary>
    public void SetData(object? data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _innerData.SetData(data);
    }

    /// <summary>
    ///  Stores the specified data and its associated format in this instance.
    /// </summary>
    public void SetData(string format, object? data)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentNullException.ThrowIfNull(data);
        _innerData.SetData(format, data);
    }

    /// <summary>
    ///  Stores the specified data and its associated class type in this instance.
    /// </summary>
    public void SetData(Type format, object? data)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(data);
        _innerData.SetData(format, data);
    }

    /// <summary>
    ///  Stores the specified data and its associated format in this instance, using the automatic conversion parameter
    ///  to specify whether the data can be converted to another format.
    /// </summary>
    public void SetData(string format, object? data, bool autoConvert)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentNullException.ThrowIfNull(data);
        _innerData.SetData(format, data, autoConvert);
    }


    /// <summary>
    ///  Return <see langword="true"/> if DataObject contains the audio data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public bool ContainsAudio() => GetDataPresent(DataFormats.WaveAudio, autoConvert: false);

    /// <summary>
    ///  Return <see langword="true"/> if DataObject contains the file drop list data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public bool ContainsFileDropList() => GetDataPresent(DataFormats.FileDrop, autoConvert: false);

    /// <summary>
    ///  Return <see langword="true"/> if DataObject contains the image data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public bool ContainsImage() => GetDataPresent(DataFormats.Bitmap, autoConvert: false);

    /// <summary>
    ///  Return <see langword="true"/> if DataObject contains the text data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public bool ContainsText() => ContainsText(TextDataFormat.UnicodeText);

    /// <summary>
    ///  Return <see langword="true"/> if DataObject contains the specified text data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public bool ContainsText(TextDataFormat format)
    {
        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        return GetDataPresent(DataFormats.ConvertToDataFormats(format), autoConvert: false);
    }

    /// <summary>
    ///  Get audio data as Stream.
    /// </summary>
    public Stream? GetAudioStream() => GetData(DataFormats.WaveAudio, autoConvert: false) as Stream;

    /// <summary>
    ///  Get file drop list data as Stream.
    /// </summary>
    public StringCollection GetFileDropList()
    {
        StringCollection fileDropListCollection = [];

        if (GetData(DataFormats.FileDrop, autoConvert: true) is string[] dropList)
        {
            fileDropListCollection.AddRange(dropList);
        }

        return fileDropListCollection;
    }

    /// <summary>
    ///  Get image data as <see cref="BitmapSource"/>.
    /// </summary>
    public BitmapSource? GetImage() => GetData(DataFormats.Bitmap, autoConvert: true) as BitmapSource;

    /// <summary>
    ///  Get text data which is the unicode text.
    /// </summary>
    public string GetText() => GetText(TextDataFormat.UnicodeText);

    /// <summary>
    ///  Get text data for the specified data format.
    /// </summary>
    public string GetText(TextDataFormat format)
    {
        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        return (string?)GetData(DataFormats.ConvertToDataFormats(format), autoConvert: false)
            ?? string.Empty;
    }

    /// <summary>
    ///  Set the audio data with bytes.
    /// </summary>
    public void SetAudio(byte[] audioBytes)
    {
        ArgumentNullException.ThrowIfNull(audioBytes);
        SetAudio(new MemoryStream(audioBytes));
    }

    /// <summary>
    ///  Set the audio data with Stream.
    /// </summary>
    public void SetAudio(Stream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);
        SetData(DataFormats.WaveAudio, audioStream, autoConvert: false);
    }

    /// <summary>
    ///  Set the file drop list data.
    /// </summary>
    public void SetFileDropList(StringCollection fileDropList)
    {
        ArgumentNullException.ThrowIfNull(fileDropList);

        if (fileDropList.Count == 0)
        {
            throw new ArgumentException(SR.Format(SR.DataObject_FileDropListIsEmpty, fileDropList));
        }

        foreach (string? fileDrop in fileDropList)
        {
            try
            {
                string filePath = Path.GetFullPath(fileDrop!);
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
    ///  Set the image data with <see cref="BitmapSource"/>.
    /// </summary>
    public void SetImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);
        SetData(DataFormats.Bitmap, image, autoConvert: true);
    }

    /// <summary>
    ///  Set the text data.
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
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        SetData(DataFormats.ConvertToDataFormats(format), textData, autoConvert: false);
    }

    int IComDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink pAdvSink, out int pdwConnection)
    {
        if (_innerData is OleConverter converter)
        {
            return converter.OleDataObject.DAdvise(ref pFormatetc, advf, pAdvSink, out pdwConnection);
        }

        pdwConnection = 0;
        return (NativeMethods.E_NOTIMPL);
    }

    void IComDataObject.DUnadvise(int dwConnection)
    {
        if (_innerData is OleConverter converter)
        {
            converter.OleDataObject.DUnadvise(dwConnection);
            return;
        }

        // Throw the exception NativeMethods.E_NOTIMPL.
        Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
    }

    int IComDataObject.EnumDAdvise(out IEnumSTATDATA? enumAdvise)
    {
        if (_innerData is OleConverter converter)
        {
            return converter.OleDataObject.EnumDAdvise(out enumAdvise);
        }

        enumAdvise = null;
        return OLE_E_ADVISENOTSUPPORTED;
    }

    IEnumFORMATETC IComDataObject.EnumFormatEtc(DATADIR dwDirection)
    {
        if (_innerData is OleConverter converter)
        {
            return converter.OleDataObject.EnumFormatEtc(dwDirection);
        }

        return dwDirection == DATADIR.DATADIR_GET
            ? (IEnumFORMATETC)new FormatEnumerator(this)
            : throw new ExternalException(SR.Format(SR.DataObject_NotImplementedEnumFormatEtc, dwDirection), NativeMethods.E_NOTIMPL);
    }

    int IComDataObject.GetCanonicalFormatEtc(ref FORMATETC pformatetcIn, out FORMATETC pformatetcOut)
    {
        pformatetcOut = pformatetcIn;
        pformatetcOut.ptd = 0;

        if (pformatetcIn.lindex != -1)
        {
            return DV_E_LINDEX;
        }

        if (_innerData is OleConverter converter)
        {
            return converter.OleDataObject.GetCanonicalFormatEtc(ref pformatetcIn, out pformatetcOut);
        }

        return DATA_S_SAMEFORMATETC;
    }

    void IComDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
    {
        if (_innerData is OleConverter converter)
        {
            converter.OleDataObject.GetData(ref formatetc, out medium);
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

                medium.unionmember = Win32GlobalAlloc(
                    NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_DDESHARE | NativeMethods.GMEM_ZEROINIT,
                    1);

                hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: false);

                if (NativeMethods.Failed(hr))
                {
                    Win32GlobalFree(new HandleRef(this, medium.unionmember));
                }
            }
            else if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != 0)
            {
                medium.tymed = TYMED.TYMED_ISTREAM;

                IStream? istream = null;
                hr = Win32CreateStreamOnHGlobal(0, fDeleteOnRelease: true, ref istream);
                if (NativeMethods.Succeeded(hr))
                {
                    medium.unionmember = Marshal.GetComInterfaceForObject(istream, typeof(IStream));
                    Marshal.ReleaseComObject(istream);

                    hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: false);

                    if (NativeMethods.Failed(hr))
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
            medium.unionmember = 0;
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    void IComDataObject.GetDataHere(ref FORMATETC formatetc, ref STGMEDIUM medium)
    {
        // This method is spec'd to accepted only limited number of tymed
        // values, and it does not support multiple OR'd values.
        if (medium.tymed is not TYMED.TYMED_ISTORAGE
            and not TYMED.TYMED_ISTREAM
            and not TYMED.TYMED_HGLOBAL
            and not TYMED.TYMED_FILE)
        {
            Marshal.ThrowExceptionForHR(DV_E_TYMED);
        }

        int hr = OleGetDataUnrestricted(ref formatetc, ref medium, doNotReallocate: true);
        if (NativeMethods.Failed(hr))
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    int IComDataObject.QueryGetData(ref FORMATETC formatetc)
    {
        if (_innerData is OleConverter converter)
        {
            return converter.OleDataObject.QueryGetData(ref formatetc);
        }

        if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
        {
            return DV_E_DVASPECT;
        }

        if (!GetTymedUseable(formatetc.tymed))
        {
            return (DV_E_TYMED);
        }

        if (formatetc.cfFormat == 0)
        {
            return NativeMethods.S_FALSE;
        }

        return GetDataPresent(DataFormats.GetDataFormat(formatetc.cfFormat).Name)
            ? NativeMethods.S_OK
            : DV_E_FORMATETC;
    }

    void IComDataObject.SetData(ref FORMATETC pFormatetcIn, ref STGMEDIUM pmedium, bool fRelease)
    {
        if (_innerData is OleConverter converter)
        {
            converter.OleDataObject.SetData(ref pFormatetcIn, ref pmedium, fRelease);
            return;
        }

        Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
    }

    /// <summary>
    ///  Adds a handler for the Copying attached event.
    /// </summary>
    /// <param name="element"><see cref="UIElement"/> or <see cref="ContentElement"/> that listens to this event.</param>
    /// <param name="handler">
    ///  A handler for DataObject.Copying event.
    ///  The handler is expected to inspect the content of a data object
    ///  passed via event arguments (DataObjectCopyingEventArgs.DataObject)
    ///  and add additional (custom) data format to it.
    ///  It's also possible for the handler to change
    ///  the contents of other data formats already put on DataObject
    ///  or even remove some of those formats.
    ///  All this happens before DataObject is put on
    ///  the Clipboard (in copy operation) or before DragDrop
    ///  process starts.
    ///  The handler can cancel the whole copying event
    ///  by calling DataObjectCopyingEventArgs.CancelCommand method.
    ///  For the case of Copy a command will be cancelled,
    ///  for the case of DragDrop a dragdrop process will be
    ///  terminated in the beginning.
    /// </param>
    public static void AddCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
    {
        UIElement.AddHandler(element, CopyingEvent, handler);
    }

    /// <summary>
    ///  Removes a handler for the Copying attached event
    /// </summary>
    /// <param name="element">UIElement or ContentElement that listens to this event</param>
    /// <param name="handler">Event Handler to be removed</param>
    public static void RemoveCopyingHandler(DependencyObject element, DataObjectCopyingEventHandler handler)
    {
        UIElement.RemoveHandler(element, CopyingEvent, handler);
    }

    /// <summary>
    ///  Adds a handler for the Pasting attached event.
    /// </summary>
    /// <param name="element"><see cref="UIElement"/> or <see cref="ContentElement"/> that listens to this event.</param>
    /// <param name="handler">
    ///  An event handler for a DataObject.Pasting event.
    ///  It is called when ah editor already made a decision
    ///  what format (from available on the Clipboard)
    ///  to apply to selection. With this handler an application
    ///  has a chance to inspect a content of DataObject extracted
    ///  from the Clipboard and decide what format to use instead.
    ///  There are three options for the handler here:
    ///  a) to cancel the whole Paste/Drop event by calling
    ///  DataObjectPastingEventArgs.CancelCommand method,
    ///  b) change an editor's choice of format by setting
    ///  new value for DataObjectPastingEventArgs.FormatToApply
    ///  property (the new value is supposed to be understandable
    ///  by an editor - it's application's code responsibility
    ///  to act consistently with an editor; example is to
    ///  replace "rich text" (xaml) format to "plain text" format -
    ///  both understandable by the TextEditor).
    ///  c) choose it's own custom format, apply it to a selection
    ///  and cancel a command for the following execution in an
    ///  editor by calling DataObjectPastingEventArgs.CancelCommand
    ///  method. This is how custom data formats are expected
    ///  to be pasted.
    ///  Note that by changing a content of data formats on DataObject
    ///  an application code does not affect the global Clipboard.
    ///  It only affects how an editor pastes this format.
    ///  For instance, by parsing xaml data format and making
    ///  some changes in it, the handler does not change this xaml
    ///  for the following acts of pasting into the same or another
    ///  application.
    /// </param>
    public static void AddPastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
    {
        UIElement.AddHandler(element, PastingEvent, handler);
    }

    /// <summary>
    ///  Removes a handler for the Pasting attached event
    /// </summary>
    /// <param name="element"><see cref="UIElement"/> or <see cref="ContentElement"/> that listens to this event.</param>
    /// <param name="handler">Event Handler to be removed</param>
    public static void RemovePastingHandler(DependencyObject element, DataObjectPastingEventHandler handler)
    {
        UIElement.RemoveHandler(element, PastingEvent, handler);
    }

    /// <summary>
    ///  Adds a handler for the <see cref="SettingDataEvent"/> attached event.
    /// </summary>
    /// <param name="element">UIElement or ContentElement that listens to this event</param>
    /// <param name="handler">
    ///  A handler for a <see cref="SettingDataEvent"> event. The event is fired as part of Copy (or Drag) command
    ///  once for each of data formats added to a <see cref="DataObject"/>. The purpose of this handler is mostly
    ///  copy command optimization. With the help of it application can filter some formats from being added to
    ///  <see cref="DataObject"/>. The other opportunity of doing that exists in <see cref="CopyingEvent"/> event,
    ///  which could set all undesirable formats to null, but in this case the work for data conversion is already
    ///  done, which may be too expensive. By handling <see cref="SettingDataEvent"> event an application
    ///  can prevent from each particular data format conversion. By calling the
    ///  <see cref="DataObjectSettingDataEventArgs.CancelCommand"/> method the handler tells an editor to skip one
    ///  particular data format (identified by <see cref="DataObjectSettingDataEventArgs.Format"/> property). Note
    ///  that calling CancelCommand method for this event does not cancel the whole Copy or Drag command.
    /// </param>
    public static void AddSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
    {
        UIElement.AddHandler(element, SettingDataEvent, handler);
    }

    /// <summary>
    ///  Removes a handler for the <see cref="SettingDataEvent"/> attached event.
    /// </summary>
    /// <param name="element"><see cref="UIElement"/> or <see cref="ContentElement"/> that listens to this event.</param>
    /// <param name="handler">Event handler to be removed.</param>
    public static void RemoveSettingDataHandler(DependencyObject element, DataObjectSettingDataEventHandler handler)
    {
        UIElement.RemoveHandler(element, SettingDataEvent, handler);
    }

    /// <summary>
    ///  The <see cref="CopyingEvent"/> event is raised when an editor has converted a content of selection into
    ///  all appropriate clipboard data formats, collected them all in <see cref="DataObject"/> and is ready to put
    ///  the objet onto the <see cref="Clipboard"/> or ready to start drag operation.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Application code can inspect <see cref="DataObject"/>, change, remove or add some data formats into it and
    ///   decide whether to proceed with the copying or cancel it.
    ///  </para>
    /// </remarks>
    public static readonly RoutedEvent CopyingEvent =
        EventManager.RegisterRoutedEvent(
            "Copying",
            RoutingStrategy.Bubble,
            typeof(DataObjectCopyingEventHandler),
            typeof(DataObject));

    /// <summary>
    ///  The <see cref="PastingEvent"/> event is raised when texteditor is ready to paste one of data format to the
    ///  content during paste operation.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Application can inspect a DataObject, change, remove or add data formats and also can decide whether to
    ///   proceed pasting or cancel it.
    ///  </para>
    /// </remarks>
    public static readonly RoutedEvent PastingEvent =
        EventManager.RegisterRoutedEvent(
            "Pasting",
            RoutingStrategy.Bubble,
            typeof(DataObjectPastingEventHandler),
            typeof(DataObject));

    /// <summary>
    ///  The <see cref="SettingData"/> event is raised when an editor is intended to add one more data format to a
    ///  <see cref="DataObject"/> during a copy operation.
    /// </summary>
    /// <remarks>
    ///  Handling this event allows for a user to prevent from adding undesirable formats, thus improving
    ///  performance of copy operations.
    /// </remarks>
    public static readonly RoutedEvent SettingDataEvent =
        EventManager.RegisterRoutedEvent(
            "SettingData",
            RoutingStrategy.Bubble,
            typeof(DataObjectSettingDataEventHandler),
            typeof(DataObject));

    /// <summary>
    /// Call Win32 UnsafeNativeMethods.GlobalAlloc() with Win32 error checking.
    /// </summary>
    internal static nint Win32GlobalAlloc(int flags, nint bytes)
    {
        nint win32Pointer = UnsafeNativeMethods.GlobalAlloc(flags, bytes);
        int win32Error = Marshal.GetLastWin32Error();
        if (win32Pointer == 0)
        {
            throw new Win32Exception(win32Error);
        }

        return win32Pointer;
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.CreateStreamOnHGlobal() with Win32 error checking.
    /// </summary>
    private static int Win32CreateStreamOnHGlobal(nint hGlobal, bool fDeleteOnRelease, [NotNull] ref IStream? istream)
    {
        int hr = UnsafeNativeMethods.CreateStreamOnHGlobal(hGlobal, fDeleteOnRelease, ref istream);
        if (NativeMethods.Failed(hr))
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return hr;
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.GlobalFree() with Win32 error checking.
    /// </summary>
    internal static void Win32GlobalFree(HandleRef handle)
    {
        nint win32Pointer = UnsafeNativeMethods.GlobalFree(handle);
        int win32Error = Marshal.GetLastWin32Error();
        if (win32Pointer != 0)
        {
            throw new Win32Exception(win32Error);
        }
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.GlobalReAlloc() with Win32 error checking.
    /// </summary>
    internal static nint Win32GlobalReAlloc(HandleRef handle, nint bytes, int flags)
    {
        nint win32Pointer = UnsafeNativeMethods.GlobalReAlloc(handle, bytes, flags);
        int win32Error = Marshal.GetLastWin32Error();
        if (win32Pointer == 0)
        {
            throw new Win32Exception(win32Error);
        }

        return win32Pointer;
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.GlobalLock() with Win32 error checking.
    /// </summary>
    internal static nint Win32GlobalLock(HandleRef handle)
    {
        nint win32Pointer = UnsafeNativeMethods.GlobalLock(handle);
        int win32Error = Marshal.GetLastWin32Error();
        if (win32Pointer == 0)
        {
            throw new Win32Exception(win32Error);
        }

        return win32Pointer;
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.GlobalUnlock() with Win32 error checking.
    /// </summary>
    internal static void Win32GlobalUnlock(HandleRef handle)
    {
        bool win32Return = UnsafeNativeMethods.GlobalUnlock(handle);
        int win32Error = Marshal.GetLastWin32Error();
        if (!win32Return && win32Error != 0)
        {
            throw new Win32Exception(win32Error);
        }
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.GlobalSize() with Win32 error checking.
    /// </summary>
    internal static nint Win32GlobalSize(HandleRef handle)
    {
        nint win32Pointer = UnsafeNativeMethods.GlobalSize(handle);

        int win32Error = Marshal.GetLastWin32Error();
        if (win32Pointer == 0)
        {
            throw new Win32Exception(win32Error);
        }

        return win32Pointer;
    }

    /// <summary>
    ///  Call Win32 SafeNativeMethods.SelectObject() with Win32 error checking.
    /// </summary>
    internal static nint Win32SelectObject(HandleRef handleDC, nint handleObject)
    {
        nint handleOldObject = UnsafeNativeMethods.SelectObject(handleDC, handleObject);
        if (handleOldObject == 0)
        {
            throw new Win32Exception();
        }

        return handleOldObject;
    }

    /// <summary>
    /// Call Win32 UnsafeNativeMethods.BitBlt() with Win32 error checking.
    /// </summary>
    internal static void Win32BitBlt(HandleRef handledestination, int width, int height, HandleRef handleSource, int operationCode)
    {
        bool win32Return = UnsafeNativeMethods.BitBlt(handledestination, 0, 0, width, height, handleSource, 0, 0, operationCode);
        if (!win32Return)
        {
            throw new Win32Exception();
        }
    }

    /// <summary>
    ///  Call Win32 UnsafeNativeMethods.WideCharToMultiByte() with Win32 error checking.
    /// </summary>
    internal static int Win32WideCharToMultiByte(string wideString, int wideChars, byte[]? bytes, int byteCount)
    {
        int win32Return = UnsafeNativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0 /*flags*/, wideString, wideChars, bytes, byteCount, 0, 0);
        int win32Error = Marshal.GetLastWin32Error();

        if (win32Return == 0)
        {
            throw new Win32Exception(win32Error);
        }

        return win32Return;
    }

    /// <summary>
    ///  Returns all the "synonyms" for the specified format.
    /// </summary>
    internal static string[] GetMappedFormats(string format)
    {
        if (format == DataFormats.Text
            || format == DataFormats.UnicodeText
            || format == DataFormats.StringFormat)
        {
            string[] arrayFormats =
            [
                DataFormats.Text,
                DataFormats.UnicodeText,
                DataFormats.StringFormat,
            ];

            return arrayFormats;
        }

        if (format == DataFormats.FileDrop
            || format == DataFormatNames.FileNameAnsi
            || format == DataFormatNames.FileNameUnicode)
        {
            return
            [
                DataFormats.FileDrop,
                DataFormatNames.FileNameUnicode,
                DataFormatNames.FileNameAnsi,
            ];
        }

        // Get the System.Drawing.Bitmap string instead of getting it from typeof.
        // So we won't load System.Drawing.dll module here.
        if (format == DataFormats.Bitmap
            || format == SystemBitmapSourceFormat
            || format == SystemDrawingBitmapFormat)
        {
            return
            [
                DataFormats.Bitmap,
                SystemDrawingBitmapFormat,
                SystemBitmapSourceFormat
            ];
        }

        return format == DataFormats.EnhancedMetafile || format == SystemDrawingImagingMetafileFormat
            ? [DataFormats.EnhancedMetafile, SystemDrawingImagingMetafileFormat]
            : [format];
    }

    /// <summary>
    ///  Behaves like IComDataObject.GetData and IComDataObject.GetDataHere,
    ///  except we make no restrictions TYMED values.
    /// </summary>
    private int OleGetDataUnrestricted(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
    {
        if (_innerData is OleConverter converter)
        {
            converter.OleDataObject.GetDataHere(ref formatetc, ref medium);

            return NativeMethods.S_OK;
        }

        return GetDataIntoOleStructs(ref formatetc, ref medium, doNotReallocate);
    }

    /// <summary>
    ///  Retrieves a list of distinct strings from the array.
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

        return [.. distinct];
    }

    /// <summary>
    ///  Returns <see langword="true"/> if the tymed is useable.
    /// </summary>
    private static bool GetTymedUseable(TYMED tymed)
    {
        for (int i = 0; i < s_allowedTymeds.Length; i++)
        {
            if ((tymed & s_allowedTymeds[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private nint GetCompatibleBitmap(object data)
    {
        nint hBitmap = SystemDrawingHelper.GetHBitmap(data, out int width, out int height);

        if (hBitmap == 0)
        {
            return 0;
        }

        nint hBitmapNew;

        try
        {
            // Get the screen DC.
            nint hDC = UnsafeNativeMethods.GetDC(new HandleRef(this, 0));

            // Create a compatible DC to render the source bitmap.
            nint sourceDC = UnsafeNativeMethods.CreateCompatibleDC(new HandleRef(this, hDC));

            // Select the original object from the current DC.
            nint sourceObject = Win32SelectObject(new HandleRef(this, sourceDC), hBitmap);

            // Create a compatible DC and a new compatible bitmap.
            nint destinationDC = UnsafeNativeMethods.CreateCompatibleDC(new HandleRef(this, hDC));

            // creates a bitmap with the device that is associated with the specified DC.
            hBitmapNew = UnsafeNativeMethods.CreateCompatibleBitmap(new HandleRef(this, hDC), width, height);

            // Select the new bitmap into a compatible DC and render the blt the original bitmap.
            nint destinationObject = Win32SelectObject(new HandleRef(this, destinationDC), hBitmapNew);

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
                UnsafeNativeMethods.DeleteDC(new HandleRef(this, sourceDC));
                UnsafeNativeMethods.DeleteDC(new HandleRef(this, destinationDC));

                // release the screen DC
                UnsafeNativeMethods.ReleaseDC(new HandleRef(this, 0), new HandleRef(this, hDC));
            }
        }
        finally
        {
            // Delete the bitmap object.
            UnsafeNativeMethods.DeleteObject(new HandleRef(this, hBitmap));
        }

        return hBitmapNew;
    }

    /// <summary>
    ///  Get the enhanced metafile handle from the metafile data object.
    /// </summary>
    private static nint GetEnhancedMetafileHandle(string format, object data)
    {
        if (format != DataFormats.EnhancedMetafile)
        {
            return 0;
        }

        // Get the metafile handle from metafile data object.
        if (SystemDrawingHelper.IsMetafile(data))
        {
            return SystemDrawingHelper.GetHandleFromMetafile(data);
        }

        if (data is MemoryStream memoryStream && memoryStream.GetBuffer() is { } buffer && buffer.Length != 0)
        {
            nint hEnhancedMetafile = NativeMethods.SetEnhMetaFileBits((uint)buffer.Length, buffer);
            int win32Error = Marshal.GetLastWin32Error();

            if (hEnhancedMetafile == 0)
            {
                // Throw the Win32 exception with GetLastWin32Error.
                throw new Win32Exception(win32Error);
            }
        }

        return 0;
    }

    /// <summary>
    ///  Populates Ole datastructes from a WinForms dataObject. This is the core
    ///  of WinForms to OLE conversion.
    /// </summary>
    private int GetDataIntoOleStructs(ref FORMATETC formatetc, ref STGMEDIUM medium, bool doNotReallocate)
    {
        int hr = DV_E_TYMED;

        if (!GetTymedUseable(formatetc.tymed) || !GetTymedUseable(medium.tymed))
        {
            return hr;
        }

        string format = DataFormats.GetDataFormat(formatetc.cfFormat).Name;

        // Set the default result with DV_E_FORMATETC.
        hr = DV_E_FORMATETC;

        if (!GetDataPresent(format))
        {
            return hr;
        }

        if (GetData(format) is not object data)
        {
            Debug.Fail("DataObject.GetDataPresent returned true for a format, but then returned null for the data.");
            return hr;
        }

        // Set the default result with DV_E_TYMED.
        hr = DV_E_TYMED;

        if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != 0)
        {
            hr = GetDataIntoOleStructsByTypeMedimHGlobal(format, data, ref medium, doNotReallocate);
        }
        else if ((formatetc.tymed & TYMED.TYMED_GDI) != 0)
        {
            hr = GetDataIntoOleStructsByTypeMediumGDI(format, data, ref medium);
        }
        else if ((formatetc.tymed & TYMED.TYMED_ENHMF) != 0)
        {
            hr = GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(format, data, ref medium);
        }
        else if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != 0)
        {
            hr = GetDataIntoOleStructsByTypeMedimIStream(format, data, ref medium);
        }

        return hr;
    }

    /// <summary>
    ///  Populates Ole data structes from a dataObject that is TYMED_HGLOBAL.
    /// </summary>
    private int GetDataIntoOleStructsByTypeMedimHGlobal(string format, object data, ref STGMEDIUM medium, bool doNotReallocate)
    {
        int hr;

        if (data is Stream stream)
        {
            hr = SaveStreamToHandle(medium.unionmember, stream, doNotReallocate);
        }
        else if (format == DataFormats.Html || format == DataFormats.Xaml)
        {
            // Save Html and Xaml data string as UTF8 encoding.
            hr = SaveStringToHandleAsUtf8(medium.unionmember, data.ToString() ?? "", doNotReallocate);
        }
        else if (format == DataFormats.Text
            || format == DataFormats.Rtf
            || format == DataFormats.OemText
            || format == DataFormats.CommaSeparatedValue)
        {
            hr = SaveStringToHandle(medium.unionmember, data.ToString() ?? "", unicode: false, doNotReallocate);
        }
        else if (format == DataFormats.UnicodeText)
        {
            hr = SaveStringToHandle(medium.unionmember, data.ToString() ?? "", unicode: true, doNotReallocate);
        }
        else if (format == DataFormats.FileDrop)
        {
            hr = SaveFileListToHandle(medium.unionmember, (string[])data, doNotReallocate);
        }
        else if (format == DataFormatNames.FileNameAnsi)
        {
            string[] filelist = (string[])data;
            hr = SaveStringToHandle(medium.unionmember, filelist[0], unicode: false, doNotReallocate);
        }
        else if (format == DataFormatNames.FileNameUnicode)
        {
            string[] filelist = (string[])data;
            hr = SaveStringToHandle(medium.unionmember, filelist[0], unicode: true, doNotReallocate);
        }
        else if (format == DataFormats.Dib && SystemDrawingHelper.IsImage(data))
        {
            // GDI+ does not properly handle saving to DIB images.  Since the
            // clipboard will take an HBITMAP and publish a Dib, we don't need
            // to support this.
            hr = DV_E_TYMED;
        }
        else if (format == typeof(BitmapSource).FullName)
        {
            // Save the System.Drawing.Bitmap or BitmapSource data to handle as BitmapSource
            hr = SaveSystemBitmapSourceToHandle(medium.unionmember, data, doNotReallocate);
        }
        else if (format == SystemDrawingBitmapFormat)
        {
            // Save the System.Drawing.Bitmap or BitmapSource data to handle as System.Drawing.Bitmap
            hr = SaveSystemDrawingBitmapToHandle(medium.unionmember, data, doNotReallocate);
        }
        else if (format == DataFormats.EnhancedMetafile || SystemDrawingHelper.IsMetafile(data))
        {
            // We don't need to support the enhanced metafile for TYMED.TYMED_HGLOBAL,
            // since we directly support TYMED.TYMED_ENHMF.
            hr = DV_E_TYMED;
        }
#pragma warning disable SYSLIB0050
        else if (format == DataFormats.Serializable
            || data is ISerializable
            || (data is not null && data.GetType().IsSerializable))
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
    ///  Populates Ole data structes from a dataObject that is TYMED_ISTREAM.
    /// </summary>
    private static int GetDataIntoOleStructsByTypeMedimIStream(string format, object data, ref STGMEDIUM medium)
    {
        IStream istream = (IStream)(Marshal.GetObjectForIUnknown(medium.unionmember));
        if (istream is null)
        {
            return NativeMethods.E_INVALIDARG;
        }

        int hr = NativeMethods.E_FAIL;

        try
        {
            // If the format is ISF, we should copy the data from the managed stream to the COM IStream object.
            if (format == Ink.StrokeCollection.InkSerializedFormat)
            {
                if (data is Stream inkStream)
                {
                    nint size = (nint)inkStream.Length;

                    byte[] buffer = new byte[NativeMethods.IntPtrToInt32(size)];
                    inkStream.Position = 0;
                    inkStream.ReadExactly(buffer);

                    istream.Write(buffer, NativeMethods.IntPtrToInt32(size), 0);
                    hr = NativeMethods.S_OK;
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(istream);
        }

        if (NativeMethods.Succeeded(hr))
        {
            medium.tymed = TYMED.TYMED_ISTREAM;
        }

        return hr;
    }

    /// <summary>
    ///  Populates Ole data structes from a dataObject that is TYMED_GDI.
    /// </summary>
    private int GetDataIntoOleStructsByTypeMediumGDI(string format, object data, ref STGMEDIUM medium)
    {
        int hr = NativeMethods.E_FAIL;

        if (format == DataFormats.Bitmap && (SystemDrawingHelper.IsBitmap(data) || data is BitmapSource))
        {
            // Get the bitmap and save it.
            nint hBitmap = GetCompatibleBitmap(data);

            if (hBitmap != 0)
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
    ///  Populates Ole data structes from a dataObject that is TYMED_ENHMF.
    /// </summary>
    private static int GetDataIntoOleStructsByTypeMediumEnhancedMetaFile(string format, object data, ref STGMEDIUM medium)
    {
        int hr = NativeMethods.E_FAIL;

        if (format == DataFormats.EnhancedMetafile)
        {
            // Get the enhanced metafile handle from the metafile data
            // and save the metafile handle.
            nint hMetafile = GetEnhancedMetafileHandle(format, data);

            if (hMetafile != 0)
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

    private int SaveObjectToHandle(nint handle, object data, bool doNotReallocate)
    {
        using MemoryStream stream = new();
        using BinaryWriter binaryWriter = new(stream);

        binaryWriter.Write(s_serializedObjectID);
        bool success = false;

        try
        {
            success = BinaryFormatWriter.TryWriteFrameworkObject(stream, data);
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            // Being extra cautious here, but the Try method above should never throw in normal circumstances.
            Debug.Fail($"Unexpected exception writing binary formatted data. {ex.Message}");
        }

        if (!success)
        {
#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
            // Using Binary formatter
            BinaryFormatter formatter = new();
            formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011 // BinaryFormatter is obsolete
        }

        return SaveStreamToHandle(handle, stream, doNotReallocate);
    }

    /// <summary>
    ///  Saves stream out to handle.
    /// </summary>
    private int SaveStreamToHandle(nint handle, Stream stream, bool doNotReallocate)
    {
        if (handle == 0)
        {
            return (NativeMethods.E_INVALIDARG);
        }

        nint size = (nint)stream.Length;
        int hr = EnsureMemoryCapacity(ref handle, NativeMethods.IntPtrToInt32(size), doNotReallocate);

        if (NativeMethods.Failed(hr))
        {
            return hr;
        }

        nint ptr = Win32GlobalLock(new HandleRef(this, handle));

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
    ///  Save the System.Drawing.Bitmap or BitmapSource data to handle as BitmapSource.
    /// </summary>
    private int SaveSystemBitmapSourceToHandle(nint handle, object data, bool doNotReallocate)
    {
        BitmapSource? bitmapSource = data as BitmapSource;

        if (bitmapSource is null && SystemDrawingHelper.IsBitmap(data))
        {
            // Create BitmapSource instance from System.Drawing.Bitmap
            nint hbitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
            bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap,
                0,
                Int32Rect.Empty,
                null);

            UnsafeNativeMethods.DeleteObject(new HandleRef(this, hbitmap));
        }

        Invariant.Assert(bitmapSource is not null);

        // Get BitmapSource stream to save it as the handle
        new BmpBitmapEncoder().Frames.Add(BitmapFrame.Create(bitmapSource));
        Stream bitmapStream = new MemoryStream();
        new BmpBitmapEncoder().Save(bitmapStream);

        return SaveStreamToHandle(handle, bitmapStream, doNotReallocate);
    }

    /// <summary>
    ///  Save the System.Drawing.Bitmap or BitmapSource data to handle as System.Drawing.Bitmap.
    /// </summary>
    private int SaveSystemDrawingBitmapToHandle(nint handle, object data, bool doNotReallocate)
    {
        object? systemDrawingBitmap = SystemDrawingHelper.GetBitmap(data);
        Invariant.AssertNotNull(systemDrawingBitmap);
        return SaveObjectToHandle(handle, systemDrawingBitmap, doNotReallocate);
    }

    /// <summary>
    ///  Saves a list of files out to the handle in HDROP format.
    /// </summary>
    private int SaveFileListToHandle(nint handle, string[] files, bool doNotReallocate)
    {
        if (files is null || files.Length < 1)
        {
            return NativeMethods.S_OK;
        }

        if (handle == 0)
        {
            return (NativeMethods.E_INVALIDARG);
        }

        if (Marshal.SystemDefaultCharSize == 1)
        {
            Invariant.Assert(false, "Expected the system default char size to be 2 for Unicode systems.");
            return (NativeMethods.E_INVALIDARG);
        }

        nint currentPtr = 0;
        int baseStructSize = FILEDROPBASESIZE;
        int sizeInBytes = baseStructSize;

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

        nint basePtr = Win32GlobalLock(new HandleRef(this, handle));

        try
        {
            currentPtr = basePtr;

            // Write out the struct...
            int[] structData = [baseStructSize, 0, 0, 0, 0];

            structData[4] = unchecked((int)0xFFFFFFFF);

            Marshal.Copy(structData, 0, currentPtr, structData.Length);

            currentPtr = (nint)((long)currentPtr + baseStructSize);

            // Write out the strings.
            for (int i = 0; i < files.Length; i++)
            {
                // Write out the each of file as the unicode and increase the pointer.
                UnsafeNativeMethods.CopyMemoryW(currentPtr, files[i], files[i].Length * 2);
                currentPtr = (nint)((long)currentPtr + (files[i].Length * 2));

                // Terminate the each of file string.
                unsafe
                {
                    *(char*)currentPtr = '\0';
                }

                // Increase the current pointer by 2 since it is a unicode.
                currentPtr = (nint)((long)currentPtr + 2);
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
    ///  Save string to handle. If unicode is set to true then the string is saved as unicode, else it is saves as DBCS.
    /// </summary>
    private int SaveStringToHandle(nint handle, string str, bool unicode, bool doNotReallocate)
    {
        if (handle == 0)
        {
            return (NativeMethods.E_INVALIDARG);
        }

        if (unicode)
        {
            int byteSize = (str.Length * 2 + 2);

            int hr = EnsureMemoryCapacity(ref handle, byteSize, doNotReallocate);
            if (NativeMethods.Failed(hr))
            {
                return hr;
            }

            nint ptr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                char[] chars = str.ToCharArray(0, str.Length);

                UnsafeNativeMethods.CopyMemoryW(ptr, chars, chars.Length * 2);

                // Terminate the string becasue of GlobalReAlloc GMEM_ZEROINIT will zero
                // out only the bytes it adds to the memory object. It doesn't initialize
                // any of the memory that existed before the call.
                unsafe
                {
                    *(char*)(nint)((ulong)ptr + (ulong)chars.Length * 2) = '\0';
                }
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }
        }
        else
        {
            // Convert the unicode text to the ansi multi byte in case of the source unicode is available.
            // WideCharToMultiByte will throw exception in case of passing 0 size of unicode.

            int pinvokeSize = str.Length > 0 ? Win32WideCharToMultiByte(str, str.Length, null, 0) : 0;

            byte[] strBytes = new byte[pinvokeSize];

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

            nint ptr = Win32GlobalLock(new HandleRef(this, handle));

            try
            {
                UnsafeNativeMethods.CopyMemory(ptr, strBytes, pinvokeSize);

                Marshal.Copy(new byte[] { 0 }, 0, (nint)((long)ptr + pinvokeSize), 1);
            }
            finally
            {
                Win32GlobalUnlock(new HandleRef(this, handle));
            }
        }

        return NativeMethods.S_OK;
    }

    /// <summary>
    ///  Save string to handle as UTF8 encoding.
    ///  Html and Xaml data format will be save as UTF8 encoding.
    /// </summary>
    private int SaveStringToHandleAsUtf8(nint handle, string str, bool doNotReallocate)
    {
        if (handle == 0)
        {
            return (NativeMethods.E_INVALIDARG);
        }

        // Create UTF8Encoding instance to convert the string to UFT8 from GetBytes.
        UTF8Encoding utf8Encoding = new UTF8Encoding();

        // Get the byte count to be UTF8 encoding.
        int utf8ByteCount = utf8Encoding.GetByteCount(str);

        // Create byte array and assign UTF8 encoding.
        byte[] utf8Bytes = utf8Encoding.GetBytes(str);

        int hr = EnsureMemoryCapacity(ref handle, utf8ByteCount + 1, doNotReallocate);
        if (NativeMethods.Failed(hr))
        {
            return hr;
        }

        nint pointerUtf8 = Win32GlobalLock(new HandleRef(this, handle));

        try
        {
            // Copy UTF8 encoding bytes to the memory.
            UnsafeNativeMethods.CopyMemory(pointerUtf8, utf8Bytes, utf8ByteCount);

            // Copy the null into the last of memory.
            Marshal.Copy(new byte[] { 0 }, 0, (nint)((long)pointerUtf8 + utf8ByteCount), 1);
        }
        finally
        {
            Win32GlobalUnlock(new HandleRef(this, handle));
        }

        return NativeMethods.S_OK;
    }

    /// <summary>
    ///  Return true if data is BitmapSource.
    /// </summary>
    private static bool IsDataSystemBitmapSource(object data) => data is BitmapSource;

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
    ///  <see langword="true"/> if the data is likely to be serializaed through CLR serialization
    /// </returns>
#pragma warning disable SYSLIB0050
    private static bool IsFormatAndDataSerializable(string format, object? data) =>
        format == DataFormats.Serializable
            || data is ISerializable
            || (data is not null && data.GetType().IsSerializable);
#pragma warning restore SYSLIB0050

    /// <summary>
    ///  Ensures that a memory block is sized to match a specified byte count.
    /// </summary>
    /// <remarks>
    /// Returns a pointer to the original memory block, a re-sized memory block,
    /// or null if the original block has insufficient capacity and doNotReallocate
    /// is true.
    ///
    /// Returns an HRESULT
    ///  S_OK: success.
    ///  STG_E_MEDIUMFULL: the original handle lacks capacity and doNotReallocate == true.  handle is null on exit.
    ///  E_OUTOFMEMORY: could not re-size the handle.  handle is null on exit.
    ///
    /// If doNotReallocate is false, this method will always realloc the original
    /// handle to fit minimumByteCount tightly.
    /// </remarks>
    private int EnsureMemoryCapacity(ref nint handle, int minimumByteCount, bool doNotReallocate)
    {
        int hr = NativeMethods.S_OK;

        if (doNotReallocate)
        {
            int byteCount = NativeMethods.IntPtrToInt32(Win32GlobalSize(new HandleRef(this, handle)));
            if (byteCount < minimumByteCount)
            {
                handle = 0;
                hr = STG_E_MEDIUMFULL;
            }
        }
        else
        {
            handle = Win32GlobalReAlloc(
                new HandleRef(this, handle),
                minimumByteCount,
                NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_DDESHARE | NativeMethods.GMEM_ZEROINIT);

            if (handle == 0)
            {
                hr = NativeMethods.E_OUTOFMEMORY;
            }
        }

        return hr;
    }

    /// <summary>
    ///  Ensure returning Bitmap(BitmapSource or System.Drawing.Bitmap) data that base
    ///  on the passed Bitmap format parameter.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Bitmap data will be converted if the data mismatch with the format in case of
    ///   autoConvert is "true", but return null if autoConvert is "false".
    ///  </para>
    /// </remarks>
    private static object? EnsureBitmapDataFromFormat(string format, bool autoConvert, object? data)
    {
        object? bitmapData = data;

        if (data is BitmapSource && format == SystemDrawingBitmapFormat)
        {
            // Data is BitmapSource, but have the mismatched System.Drawing.Bitmap format
            if (!autoConvert)
            {
                return null;
            }

            // Convert data from BitmapSource to SystemDrawingBitmap
            bitmapData = SystemDrawingHelper.GetBitmap(data);
        }
        else if (SystemDrawingHelper.IsBitmap(data) && (format == DataFormats.Bitmap || format == SystemBitmapSourceFormat))
        {
            // Data is System.Drawing.Bitmap, but have the mismatched BitmapSource format
            if (!autoConvert)
            {
                return null;
            }

            // Create BitmapSource instance from System.Drawing.Bitmap
            nint hbitmap = SystemDrawingHelper.GetHBitmapFromBitmap(data);
            bitmapData = Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap,
                0,
                Int32Rect.Empty,
                null);

            UnsafeNativeMethods.DeleteObject(new HandleRef(null, hbitmap));
        }

        return bitmapData;
    }
}
