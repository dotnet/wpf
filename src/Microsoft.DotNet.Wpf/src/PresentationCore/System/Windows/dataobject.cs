// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Private.Windows.Ole;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media.Imaging;
using System.Windows.Ole;
using HRESULT = Windows.Win32.Foundation.HRESULT;
using BOOL = Windows.Win32.Foundation.BOOL;
using Com = Windows.Win32.System.Com;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Windows;

public sealed unsafe partial class DataObject :
    ITypedDataObject,
    IDataObjectInternal<DataObject, IDataObject>,
    // Built-in COM interop chooses the first interface that implements an IID,
    // we want the CsWin32 to be chosen over System.Runtime.InteropServices.ComTypes
    // so it must come first.
    Com.IDataObject.Interface,
    ComTypes.IDataObject,
    Com.IManagedWrapper<Com.IDataObject>,
    IComVisibleDataObject
{
    private readonly Composition _innerData;
    private readonly bool _doNotUnwrap;

    static DataObject IDataObjectInternal<DataObject, IDataObject>.Create() => new();
    static DataObject IDataObjectInternal<DataObject, IDataObject>.Create(Com.IDataObject* dataObject) => new(dataObject);
    static DataObject IDataObjectInternal<DataObject, IDataObject>.Create(object data) => new(data);

    static IDataObjectInternal IDataObjectInternal<DataObject, IDataObject>.Wrap(IDataObject data) =>
        new DataObjectAdapter(data);

    /// <summary>
    ///  Initializes a new instance of the <see cref="DataObject"/> class, which can store arbitrary data.
    /// </summary>
    public DataObject() => _innerData = Composition.Create();

    /// <summary>
    ///  Initializes a new instance of the <see cref="DataObject"/> class, containing the specified data.
    /// </summary>
    public DataObject(object data) => _innerData = Composition.Create<DataObject, IDataObject>(data);

    /// <summary>
    ///  Initializes a new instance of the class, containing the specified data and its
    ///  associated format.
    /// </summary>
    public DataObject(string format, object data) : this() =>
        SetData(format, data.OrThrowIfNull());

    /// <summary>
    ///  Initializes a new instance of the class, containing the specified data and its associated format.
    /// </summary>
    public DataObject(Type format, object data) : this() =>
        SetData(format.FullName.OrThrowIfNull(), data.OrThrowIfNull());

    /// <summary>
    ///  Initializes a new instance of the class, containing the specified data and its associated format.
    /// </summary>
    public DataObject(string format, object data, bool autoConvert) : this() =>
        SetData(format, data.OrThrowIfNull(), autoConvert);

    /// <summary>
    ///  Initializes a new instance of the <see cref="DataObject"/> class, with the raw <see cref="Com.IDataObject"/>
    ///  and the managed data object the raw pointer is associated with.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This method will add a reference to the <paramref name="data"/> pointer.
    ///  </para>
    /// </remarks>
    /// <inheritdoc cref="DataObject(object)"/>
    internal DataObject(Com.IDataObject* data) => _innerData = Composition.Create(data);

    /// <summary>
    ///  Special factory for the <see cref="Clipboard"/> to use.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   When constructing from the clipboard we only want to unwrap the nested data if it is an
    ///   <see cref="IDataObject"/> on retrieving the data back from the clipboard. WinForms deals
    ///   with this via a derived DataObject class, as it isn't sealed in WinForms.
    ///  </para>
    /// </remarks>
    internal static DataObject CreateFromClipboard(object data) =>
        new DataObject(data, doNotUnwrap: data is not IDataObject);

    /// <inheritdoc cref="DataObject(object)"/>
    /// <param name="doNotUnwrap">Do not allow unwrapping of nested data.</param>
    private DataObject(object data, bool doNotUnwrap) : this(data)
        => _doNotUnwrap = doNotUnwrap;

    bool IDataObjectInternal<DataObject, IDataObject>.TryUnwrapUserDataObject([NotNullWhen(true)] out IDataObject? dataObject) =>
        TryUnwrapUserDataObject(out dataObject);

    internal bool TryUnwrapUserDataObject([NotNullWhen(true)] out IDataObject? dataObject)
    {
        if (_doNotUnwrap)
        {
            // We dont want to unwrap internally constructed DataObjects unless they were constructed from
            // a user provided IDataObject.
            dataObject = null;
            return false;
        }

        dataObject = _innerData.ManagedDataObject switch
        {
            DataObject data => data,
            DataObjectAdapter adapter => adapter.DataObject,
            DataStore<WpfOleServices> => this,
            _ => null
        };

        return dataObject is not null;
    }

    /// <summary>
    ///  Retrieves the data associated with the specified data format, using an automated conversion parameter to
    ///  determine whether to convert the data to the format.
    /// </summary>
    public object? GetData(string format, bool autoConvert) => _innerData.GetData(format, autoConvert);

    /// <summary>
    ///  Retrieves the data associated with the specified data format.
    /// </summary>
    public object? GetData(string format) => GetData(format, autoConvert: true);

    /// <summary>
    ///  Retrieves the data associated with the specified class type format.
    /// </summary>
    public object? GetData(Type format) => GetData(format.OrThrowIfNull().FullName.OrThrowIfNull());

    /// <inheritdoc cref="Clipboard.TryGetData{T}(string, Func{TypeName, Type}, out T)"/>
    [CLSCompliant(false)]
    public bool TryGetData<T>(
        string format,
        Func<TypeName, Type?> resolver,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data)
    {
        data = default;
        resolver.OrThrowIfNull();

        return TryGetDataInternal(format, resolver, autoConvert, out data);
    }

    public bool TryGetData<T>(
        string format,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            TryGetDataInternal(format, resolver: null, autoConvert, out data);

    public bool TryGetData<T>(
        string format,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            TryGetDataInternal(format, resolver: null, autoConvert: true, out data);

    public bool TryGetData<T>(
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            TryGetDataInternal(typeof(T).FullName!, resolver: null, autoConvert: true, out data);

    private bool TryGetDataInternal<T>(
        string format,
        Func<TypeName, Type?>? resolver,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data)
    {
        data = default;

        if (!ClipboardCore.IsValidTypeForFormat(typeof(T), format))
        {
            // Resolver implementation is specific to the overridden TryGetDataCore method,
            // can't validate if a non-null resolver is required for unbounded types.
            return false;
        }

        // Invoke the appropriate overload so we don't fail a null check on a nested object if the resolver is null.
        return resolver is null
            ? _innerData.TryGetData(format, autoConvert, out data)
            : _innerData.TryGetData(format, resolver, autoConvert, out data);
    }

    /// <summary>
    ///  Determines whether data stored in this instance is associated with, or can be converted to, the specified format.
    /// </summary>
    public bool GetDataPresent(Type format) => GetDataPresent(format.OrThrowIfNull().FullName.OrThrowIfNull());

    /// <summary>
    ///  Determines whether data stored in this instance is associated with the specified format, using an automatic
    ///  conversion parameter to determine whether to convert the data to the format.
    /// </summary>
    public bool GetDataPresent(string format, bool autoConvert) => _innerData.GetDataPresent(format, autoConvert);

    /// <summary>
    ///  Determines whether data stored in this instance is associated with, or can be converted to, the specified format.
    /// </summary>
    public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);

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
    public void SetData(object? data) => _innerData.SetData(data);

    /// <summary>
    ///  Stores the specified data and its associated format in this instance.
    /// </summary>
    public void SetData(string format, object? data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _innerData.SetData(format, data);
    }

    /// <summary>
    ///  Stores the specified data and its associated class type in this instance.
    /// </summary>
    public void SetData(Type format, object? data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _innerData.SetData(format, data);
    }

    /// <summary>
    ///  Stores the specified data and its associated format in this instance, using the automatic conversion parameter
    ///  to specify whether the data can be converted to another format.
    /// </summary>
    public void SetData(string format, object? data, bool autoConvert)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        _innerData.SetData(format, autoConvert, data);
    }

    // WinForms and WPF have these defined in a different order.
    void IDataObjectInternal.SetData(string format, bool autoConvert, object? data) => SetData(format, data, autoConvert);


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
        StringCollection dropList = [];
        if (GetData(DataFormatNames.FileDrop, autoConvert: true) is string[] strings)
        {
            dropList.AddRange(strings);
        }

        return dropList;
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
        string[] strings = new string[fileDropList.OrThrowIfNull().Count];
        fileDropList.CopyTo(strings, 0);
        SetData(DataFormatNames.FileDrop, strings, autoConvert: true);
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

    #region ComTypes.IDataObject
    int ComTypes.IDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink pAdvSink, out int pdwConnection) =>
        _innerData.DAdvise(ref pFormatetc, advf, pAdvSink, out pdwConnection);

    void ComTypes.IDataObject.DUnadvise(int dwConnection) => _innerData.DUnadvise(dwConnection);

    int ComTypes.IDataObject.EnumDAdvise(out IEnumSTATDATA? enumAdvise) =>
        _innerData.EnumDAdvise(out enumAdvise);

    IEnumFORMATETC ComTypes.IDataObject.EnumFormatEtc(DATADIR dwDirection) =>
        _innerData.EnumFormatEtc(dwDirection);

    int ComTypes.IDataObject.GetCanonicalFormatEtc(ref FORMATETC pformatetcIn, out FORMATETC pformatetcOut) =>
        _innerData.GetCanonicalFormatEtc(ref pformatetcIn, out pformatetcOut);

    void ComTypes.IDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium) =>
        _innerData.GetData(ref formatetc, out medium);

    void ComTypes.IDataObject.GetDataHere(ref FORMATETC formatetc, ref STGMEDIUM medium) =>
        _innerData.GetDataHere(ref formatetc, ref medium);

    int ComTypes.IDataObject.QueryGetData(ref FORMATETC formatetc) =>
        _innerData.QueryGetData(ref formatetc);

    void ComTypes.IDataObject.SetData(ref FORMATETC pFormatetcIn, ref STGMEDIUM pmedium, bool fRelease) =>
        _innerData.SetData(ref pFormatetcIn, ref pmedium, fRelease);

    #endregion

    #region Com.IDataObject.Interface

    HRESULT Com.IDataObject.Interface.DAdvise(Com.FORMATETC* pformatetc, uint advf, Com.IAdviseSink* pAdvSink, uint* pdwConnection) =>
        _innerData.DAdvise(pformatetc, advf, pAdvSink, pdwConnection);

    HRESULT Com.IDataObject.Interface.DUnadvise(uint dwConnection) =>
        _innerData.DUnadvise(dwConnection);

    HRESULT Com.IDataObject.Interface.EnumDAdvise(Com.IEnumSTATDATA** ppenumAdvise) =>
        _innerData.EnumDAdvise(ppenumAdvise);

    HRESULT Com.IDataObject.Interface.EnumFormatEtc(uint dwDirection, Com.IEnumFORMATETC** ppenumFormatEtc) =>
        _innerData.EnumFormatEtc(dwDirection, ppenumFormatEtc);

    HRESULT Com.IDataObject.Interface.GetData(Com.FORMATETC* pformatetcIn, Com.STGMEDIUM* pmedium) =>
        _innerData.GetData(pformatetcIn, pmedium);

    HRESULT Com.IDataObject.Interface.GetDataHere(Com.FORMATETC* pformatetc, Com.STGMEDIUM* pmedium) =>
        _innerData.GetDataHere(pformatetc, pmedium);

    HRESULT Com.IDataObject.Interface.QueryGetData(Com.FORMATETC* pformatetc) =>
        _innerData.QueryGetData(pformatetc);

    HRESULT Com.IDataObject.Interface.GetCanonicalFormatEtc(Com.FORMATETC* pformatectIn, Com.FORMATETC* pformatetcOut) =>
        _innerData.GetCanonicalFormatEtc(pformatectIn, pformatetcOut);

    HRESULT Com.IDataObject.Interface.SetData(Com.FORMATETC* pformatetc, Com.STGMEDIUM* pmedium, BOOL fRelease) =>
        _innerData.SetData(pformatetc, pmedium, fRelease);
    #endregion

    /// <inheritdoc cref="SetDataAsJson{T}(string, T)"/>
    public void SetDataAsJson<T>(T data) =>
        _innerData.SetDataAsJson<T, DataObject>(data);

    /// <summary>
    ///  Stores the data in the specified format using the <see cref="JsonSerializer"/>.
    /// </summary>
    /// <param name="format">The format associated with the data. See <see cref="DataFormats"/> for predefined formats.</param>
    /// <param name="data">The data to store.</param>
    /// <remarks>
    ///  <para>
    ///   The default behavior of <see cref="JsonSerializer"/> is used to serialize the data.
    ///  </para>
    ///  <para>
    ///   See
    ///   <see href="https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/how-to#serialization-behavior"/>
    ///   and <see href="https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/reflection-vs-source-generation#metadata-collection"/>
    ///   for more details on default <see cref="JsonSerializer"/> behavior.
    ///  </para>
    ///  <para>
    ///   If custom JSON serialization behavior is needed, manually JSON serialize the data and then use SetData,
    ///   or create a custom <see cref="Text.Json.Serialization.JsonConverter"/>, attach the
    ///   <see cref="Text.Json.Serialization.JsonConverterAttribute"/>, and then recall this method.
    ///   See <see href="https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to"/> for more details
    ///   on custom converters for JSON serialization.
    ///  </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="format"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///  <paramref name="format"/> is empty, whitespace, or a predefined format -or- <paramref name="data"/> isa a DataObject.
    /// </exception>
    public void SetDataAsJson<T>(string format, T data) =>
        _innerData.SetDataAsJson<T, DataObject>(data, format);
}
