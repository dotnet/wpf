// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Moq;

namespace PresentationCore.Tests;

[Collection(ClipboardCollection.Name)]
public sealed class ClipboardTests
{
    private static readonly Lazy<nint> s_wpfGraphics = new(
        () => NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "wpfgfx_cor3.dll")));

    // Ensures every string-format API rejects a null format before attempting clipboard access.
    [Fact]
    public void FormatTakingApis_NullFormat_ThrowArgumentNullException()
    {
        AssertArgumentNull("format", () => Clipboard.ContainsData(null!));
        AssertArgumentNull("format", () => Clipboard.GetData(null!));
        AssertArgumentNull("format", () => Clipboard.SetData(null!, "value"));
    }

    // Ensures every string-format API rejects the unsupported empty format consistently.
    [Fact]
    public void FormatTakingApis_EmptyFormat_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Clipboard.ContainsData(string.Empty));
        Assert.Throws<ArgumentException>(() => Clipboard.GetData(string.Empty));
        Assert.Throws<ArgumentException>(() => Clipboard.SetData(string.Empty, "value"));
    }

    // Verifies SetData validates its payload independently of format validation.
    [Fact]
    public void SetData_NullData_ThrowsArgumentNullException()
    {
        AssertArgumentNull("data", () => Clipboard.SetData("format", null!));
    }

    // Verifies each setter overload reports the documented parameter name for a null payload.
    [Fact]
    public void SetApis_NullData_ThrowArgumentNullException()
    {
        AssertArgumentNull("audioBytes", () => Clipboard.SetAudio((byte[])null!));
        AssertArgumentNull("audioStream", () => Clipboard.SetAudio((Stream)null!));
        AssertArgumentNull("fileDropList", () => Clipboard.SetFileDropList(null!));
        AssertArgumentNull("image", () => Clipboard.SetImage(null!));
        AssertArgumentNull("text", () => Clipboard.SetText(null!));
        AssertArgumentNull("text", () => Clipboard.SetText(null!, TextDataFormat.Text));
        AssertArgumentNull("data", () => Clipboard.SetDataObject(null!));
        AssertArgumentNull("data", () => Clipboard.SetDataObject(null!, false));
    }

    // Confirms all text APIs reject values outside the defined TextDataFormat range.
    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void TextFormatApis_InvalidFormat_ThrowInvalidEnumArgumentException(int value)
    {
        TextDataFormat format = (TextDataFormat)value;

        Assert.Equal("format", Assert.Throws<InvalidEnumArgumentException>(() => Clipboard.ContainsText(format)).ParamName);
        Assert.Equal("format", Assert.Throws<InvalidEnumArgumentException>(() => Clipboard.GetText(format)).ParamName);
        Assert.Equal("format", Assert.Throws<InvalidEnumArgumentException>(() => Clipboard.SetText("value", format)).ParamName);
    }

    // Verifies IsCurrent rejects a null IDataObject before performing an OLE query.
    [Fact]
    public void IsCurrent_NullData_ThrowsArgumentNullException()
    {
        AssertArgumentNull("data", () => Clipboard.IsCurrent(null!));
    }

    // Confirms IsCurrent short-circuits for managed IDataObject implementations that are not COM data objects.
    [Fact]
    public void IsCurrent_ManagedIDataObject_ReturnsFalseWithoutOleCall()
    {
        IDataObject dataObject = new Mock<IDataObject>(MockBehavior.Strict).Object;

        Assert.False(Clipboard.IsCurrent(dataObject));
    }

    // Verifies an empty file list is not accepted as clipboard file-drop data.
    [Fact]
    public void SetFileDropList_EmptyList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Clipboard.SetFileDropList(new StringCollection()));
    }

    // Verifies every file-drop entry must contain a non-null, non-empty path.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetFileDropList_NullOrEmptyPath_ThrowsArgumentException(string? path)
    {
        StringCollection paths = new() { path! };

        Assert.Throws<ArgumentException>(() => Clipboard.SetFileDropList(paths));
    }

    // Verifies malformed paths are rejected before file-drop data is placed on the clipboard.
    [Fact]
    public void SetFileDropList_InvalidPath_ThrowsArgumentException()
    {
        StringCollection paths = new() { "\0" };

        Assert.Throws<ArgumentException>(() => Clipboard.SetFileDropList(paths));
    }

    // Documents that every OLE-backed Clipboard API requires an STA thread, including all typed overloads.
    [Fact]
    public void OleBackedApis_MtaThread_ThrowThreadStateException()
    {
        EnsureWpfGraphicsLoaded();
        MemoryStream audioStream = new([1, 2, 3]);
        DataObject dataObject = new("format", "value");
        StringCollection fileDropList = new() { Path.Combine(Path.GetTempPath(), "clipboard.txt") };
        WriteableBitmap image = new(1, 1, 96, 96, PixelFormats.Bgra32, palette: null);
        Action[] actions =
        [
            Clipboard.Clear,
            Clipboard.Flush,
            () => Clipboard.GetAudioStream(),
            () => Clipboard.GetDataObject(),
            () => Clipboard.GetData("format"),
            () => Clipboard.GetFileDropList(),
            () => Clipboard.GetImage(),
            () => Clipboard.GetText(),
            () => Clipboard.GetText(TextDataFormat.Text),
            () => Clipboard.SetAudio([1, 2, 3]),
            () => Clipboard.SetAudio(audioStream),
            () => Clipboard.SetData("format", "value"),
            () => Clipboard.SetDataObject(dataObject),
            () => Clipboard.SetDataObject(dataObject, true),
            () => Clipboard.SetFileDropList(fileDropList),
            () => Clipboard.SetImage(image),
            () => Clipboard.SetText("value"),
            () => Clipboard.SetText("value", TextDataFormat.Text),
            () => Clipboard.IsCurrent(dataObject),
        ];

        foreach (Action action in actions)
        {
            Exception? exception = null;
            Thread thread = new(() => exception = Xunit.Record.Exception(action));
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();

            Assert.IsType<ThreadStateException>(exception);
        }
    }

    // Documents that Contains APIs use Win32 format probing and therefore remain valid on an MTA thread.
    [Fact]
    public void ContainsApis_MtaThread_DoNotRequireSta()
    {
        Action[] actions =
        [
            () => Clipboard.ContainsAudio(),
            () => Clipboard.ContainsData("format"),
            () => Clipboard.ContainsFileDropList(),
            () => Clipboard.ContainsImage(),
            () => Clipboard.ContainsText(),
            () => Clipboard.ContainsText(TextDataFormat.Text),
        ];

        foreach (Action action in actions)
        {
            Exception? exception = null;
            Thread thread = new(() => exception = Xunit.Record.Exception(action));
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();

            Assert.Null(exception);
        }
    }

    // Verifies Clear removes every typed view and leaves only an empty clipboard data object.
    [StaFact]
    public void Clear_PopulatedClipboard_ResetsAllTypedViews()
    {
        Clipboard.SetText("value");

        Clipboard.Clear();

        IDataObject dataObject = Assert.IsAssignableFrom<IDataObject>(Clipboard.GetDataObject());
        Assert.Empty(RetryClipboardAccess(dataObject.GetFormats));
        Assert.False(Clipboard.ContainsAudio());
        Assert.False(Clipboard.ContainsData(DataFormats.UnicodeText));
        Assert.False(Clipboard.ContainsFileDropList());
        Assert.False(Clipboard.ContainsImage());
        Assert.False(Clipboard.ContainsText());
        Assert.False(Clipboard.ContainsText(TextDataFormat.UnicodeText));
        Assert.Null(Clipboard.GetAudioStream());
        Assert.Null(Clipboard.GetData(DataFormats.UnicodeText));
        Assert.Empty(Clipboard.GetFileDropList().Cast<string>());
        Assert.Null(Clipboard.GetImage());
        Assert.Equal(string.Empty, Clipboard.GetText());
        Assert.Equal(string.Empty, Clipboard.GetText(TextDataFormat.UnicodeText));
    }

    // Verifies custom formats register case-insensitively and round-trip through Clipboard and IDataObject APIs.
    [StaFact]
    public void SetData_CustomFormat_RoundTripsThroughIDataObject()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        const string Value = "custom clipboard value";

        try
        {
            Clipboard.SetData(format, Value);
            DataFormat registeredFormat = DataFormats.GetDataFormat(format);

            Assert.True(Clipboard.ContainsData(format));
            Assert.True(Clipboard.ContainsData(format.ToUpperInvariant()));
            Assert.Equal(Value, Clipboard.GetData(format));
            Assert.Equal(Value, Clipboard.GetData(format.ToUpperInvariant()));
            Assert.Equal(format, registeredFormat.Name);
            Assert.Same(registeredFormat, DataFormats.GetDataFormat(registeredFormat.Id));
            Assert.Same(registeredFormat, DataFormats.GetDataFormat(format.ToUpperInvariant()));

            IDataObject dataObject = Assert.IsAssignableFrom<IDataObject>(Clipboard.GetDataObject());
            Assert.True(RetryClipboardAccess(() => dataObject.GetDataPresent(format)));
            Assert.True(RetryClipboardAccess(() => dataObject.GetDataPresent(format, false)));
            Assert.Equal(Value, RetryClipboardAccess(() => dataObject.GetData(format)));
            Assert.Equal(Value, RetryClipboardAccess(() => dataObject.GetData(format, false)));
            Assert.Contains(format, RetryClipboardAccess(dataObject.GetFormats));
            Assert.Contains(format, RetryClipboardAccess(() => dataObject.GetFormats(false)));

            Assert.False(Clipboard.ContainsAudio());
            Assert.False(Clipboard.ContainsFileDropList());
            Assert.False(Clipboard.ContainsImage());
            Assert.False(Clipboard.ContainsText());
            Assert.Null(Clipboard.GetAudioStream());
            Assert.Empty(Clipboard.GetFileDropList().Cast<string>());
            Assert.Null(Clipboard.GetImage());
            Assert.Equal(string.Empty, Clipboard.GetText());
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Confirms the default and copy:false overloads retain a live data source whose later mutations are observable.
    [StaFact]
    public void SetDataObject_DefaultAndFalseOverloads_PreserveLiveDataObject()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "first");

        try
        {
            Clipboard.SetDataObject(dataObject);

            Assert.True(Clipboard.IsCurrent(dataObject));
            dataObject.SetData(format, "first changed");
            Assert.Equal("first changed", Clipboard.GetData(format));

            Clipboard.Clear();
            dataObject.SetData(format, "second");
            Clipboard.SetDataObject(dataObject, copy: false);

            Assert.True(Clipboard.IsCurrent(dataObject));
            dataObject.SetData(format, "second changed");
            Assert.Equal("second changed", Clipboard.GetData(format));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies replacing the clipboard owner updates IsCurrent for both the previous and new data objects.
    [StaFact]
    public void SetDataObject_ReplacingCurrentObject_UpdatesIsCurrent()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject first = new(format, "first");
        DataObject second = new(format, "second");

        try
        {
            Clipboard.SetDataObject(first, copy: false);
            Assert.True(Clipboard.IsCurrent(first));

            Clipboard.SetDataObject(second, copy: false);

            Assert.False(Clipboard.IsCurrent(first));
            Assert.True(Clipboard.IsCurrent(second));
            Assert.Equal("second", Clipboard.GetData(format));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies a live DataObject preserves the type-, format-, and auto-conversion IDataObject contracts.
    [StaFact]
    public void SetDataObject_LiveDataObject_PreservesIDataObjectOverloads()
    {
        string nativeFormat = $"WPF ClipboardTests {Guid.NewGuid():N}";
        Uri uri = new("https://example.test/clipboard");
        DataObject source = new();
        source.SetData("typed string");
        source.SetData(typeof(Uri), uri);
        source.SetData(nativeFormat, "native", autoConvert: false);

        try
        {
            Clipboard.SetDataObject(source, copy: false);

            IDataObject result = Assert.IsAssignableFrom<IDataObject>(Clipboard.GetDataObject());
            Assert.True(result.GetDataPresent(typeof(string)));
            Assert.True(result.GetDataPresent(typeof(Uri)));
            Assert.True(result.GetDataPresent(nativeFormat));
            Assert.True(result.GetDataPresent(nativeFormat, autoConvert: false));
            Assert.Equal("typed string", result.GetData(typeof(string)));
            Assert.Equal(uri, result.GetData(typeof(Uri)));
            Assert.Equal("native", result.GetData(nativeFormat));
            Assert.Equal("native", result.GetData(nativeFormat, autoConvert: false));
            Assert.Contains(nativeFormat, result.GetFormats());
            Assert.Contains(nativeFormat, result.GetFormats(autoConvert: false));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Captures how the system clipboard synthesizes UnicodeText even when the source DataObject disables text auto-conversion.
    [StaFact]
    public void SetDataObject_AnsiText_ExposesSystemGeneratedUnicodeTextToClipboardApis()
    {
        const string Value = "ahoj";
        DataObject source = new();
        source.SetText(Value, TextDataFormat.Text);

        try
        {
            Assert.False(source.GetDataPresent(DataFormats.UnicodeText));
            Assert.False(source.GetDataPresent(DataFormats.UnicodeText, autoConvert: false));

            Clipboard.SetDataObject(source);

            IDataObject result = Assert.IsAssignableFrom<IDataObject>(Clipboard.GetDataObject());
            Assert.True(RetryClipboardAccess(() => result.GetDataPresent(DataFormats.UnicodeText)));
            Assert.True(RetryClipboardAccess(() => result.GetDataPresent(DataFormats.UnicodeText, autoConvert: false)));
            Assert.Equal(Value, RetryClipboardAccess(() => result.GetData(DataFormats.UnicodeText)));
            Assert.Equal(Value, RetryClipboardAccess(() => result.GetData(DataFormats.UnicodeText, autoConvert: false)));

            Assert.True(Clipboard.ContainsText());
            Assert.True(Clipboard.ContainsText(TextDataFormat.Text));
            Assert.True(Clipboard.ContainsText(TextDataFormat.UnicodeText));
            Assert.Equal(Value, Clipboard.GetText());
            Assert.Equal(Value, Clipboard.GetText(TextDataFormat.Text));
            Assert.Equal(Value, Clipboard.GetText(TextDataFormat.UnicodeText));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies autoConvert controls text format advertisement while default GetData still searches the mapped format group.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataObject_TextAutoConvert_ControlsMappedFormatAdvertisement(bool autoConvert)
    {
        const string Value = "text conversion verification";
        string[] mappedFormats =
        [
            DataFormats.Text,
            DataFormats.UnicodeText,
            DataFormats.StringFormat,
        ];
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.Text, Value, autoConvert);

        foreach (string format in mappedFormats)
        {
            bool expected = autoConvert || format == DataFormats.Text;

            Assert.Equal(expected, dataObject.GetDataPresent(format));
            Assert.Equal(format == DataFormats.Text, dataObject.GetDataPresent(format, autoConvert: false));
            Assert.Equal(Value, dataObject.GetData(format));
            Assert.Equal(format == DataFormats.Text ? Value : null, dataObject.GetData(format, autoConvert: false));
        }

        string[] expectedFormats = autoConvert ? mappedFormats : [DataFormats.Text];
        Assert.Equal(expectedFormats.Order(), dataObject.GetFormats().Order());
        Assert.Equal(new[] { DataFormats.Text }, dataObject.GetFormats(autoConvert: false));
    }

    // Verifies every built-in DataObject format mapping advertises all synonyms only when autoConvert is enabled.
    [Fact]
    public void DataObject_AutoConvert_ControlsAllBuiltInFormatMappings()
    {
        string[][] formatGroups =
        [
            [DataFormats.Text, DataFormats.UnicodeText, DataFormats.StringFormat],
            [DataFormats.FileDrop, DataFormats.FileNameW, DataFormats.FileName],
            [DataFormats.Bitmap, typeof(System.Drawing.Bitmap).FullName!, typeof(BitmapSource).FullName!],
            [DataFormats.EnhancedMetafile, typeof(System.Drawing.Imaging.Metafile).FullName!],
        ];

        foreach (string[] formatGroup in formatGroups)
        {
            foreach (string sourceFormat in formatGroup)
            {
                foreach (bool autoConvert in new[] { false, true })
                {
                    DataObject dataObject = new();
                    dataObject.SetData(sourceFormat, "value", autoConvert);
                    string[] expectedFormats = autoConvert ? formatGroup : [sourceFormat];

                    Assert.Equal(expectedFormats.Order(), dataObject.GetFormats().Order());
                    Assert.Equal(new[] { sourceFormat }, dataObject.GetFormats(autoConvert: false));

                    foreach (string targetFormat in formatGroup)
                    {
                        Assert.Equal(
                            autoConvert || targetFormat == sourceFormat,
                            dataObject.GetDataPresent(targetFormat));
                        Assert.Equal(
                            targetFormat == sourceFormat,
                            dataObject.GetDataPresent(targetFormat, autoConvert: false));
                    }
                }
            }
        }
    }

    // Confirms SetDataObject wraps ordinary CLR values in a DataObject with their expected native formats.
    [StaFact]
    public void SetDataObject_RawObject_WrapsItInADataObject()
    {
        const string Value = "raw string";

        try
        {
            Clipboard.SetDataObject(Value);

            Assert.True(Clipboard.ContainsText());
            Assert.Equal(Value, Clipboard.GetText());
            IDataObject result = Assert.IsAssignableFrom<IDataObject>(Clipboard.GetDataObject());
            Assert.True(result.GetDataPresent(typeof(string)));
            Assert.Equal(Value, result.GetData(typeof(string)));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies Flush materializes delayed data, releases ownership, and preserves the rendered value.
    [StaFact]
    public void Flush_NonPersistentDataObject_RendersDataAndReleasesSource()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "persisted");

        try
        {
            Clipboard.SetDataObject(dataObject, copy: false);
            Assert.True(Clipboard.IsCurrent(dataObject));

            Clipboard.Flush();

            Assert.False(Clipboard.IsCurrent(dataObject));
            Assert.Equal("persisted", Clipboard.GetData(format));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies copy:true snapshots the source immediately instead of retaining a live data object.
    [StaFact]
    public void SetDataObject_CopyTrue_RendersDataAndReleasesSource()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "persisted");

        try
        {
            Clipboard.SetDataObject(dataObject, copy: true);
            dataObject.SetData(format, "changed after copy");

            Assert.False(Clipboard.IsCurrent(dataObject));
            Assert.Equal("persisted", Clipboard.GetData(format));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Confirms the default text overload uses UnicodeText and preserves non-ASCII and surrogate-pair content.
    [StaFact]
    public void SetText_DefaultOverload_RoundTripsUnicodeText()
    {
        const string Value = "Clipboard \u03A9 \U0001F600";

        try
        {
            Clipboard.SetText(Value);

            Assert.True(Clipboard.ContainsText());
            Assert.True(Clipboard.ContainsText(TextDataFormat.UnicodeText));
            Assert.True(Clipboard.ContainsData(DataFormats.UnicodeText));
            Assert.Equal(Value, Clipboard.GetText());
            Assert.Equal(Value, Clipboard.GetText(TextDataFormat.UnicodeText));
            Assert.Equal(Value, Clipboard.GetData(DataFormats.UnicodeText));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies each supported TextDataFormat maps to its corresponding clipboard format and round-trips unchanged.
    [StaTheory]
    [InlineData(TextDataFormat.Text)]
    [InlineData(TextDataFormat.UnicodeText)]
    [InlineData(TextDataFormat.Rtf)]
    [InlineData(TextDataFormat.Html)]
    [InlineData(TextDataFormat.CommaSeparatedValue)]
    [InlineData(TextDataFormat.Xaml)]
    public void SetText_AllSupportedFormats_RoundTrip(TextDataFormat format)
    {
        string value = $"text for {format}";
        string dataFormat = GetDataFormat(format);

        try
        {
            Clipboard.SetText(value, format);

            Assert.True(Clipboard.ContainsText(format));
            Assert.True(Clipboard.ContainsData(dataFormat));
            Assert.Equal(value, Clipboard.GetText(format));
            Assert.Equal(value, Clipboard.GetData(dataFormat));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Confirms an empty string is valid clipboard text and remains distinguishable from an absent format.
    [StaFact]
    public void SetText_EmptyString_RoundTrips()
    {
        try
        {
            Clipboard.SetText(string.Empty);

            Assert.True(Clipboard.ContainsText());
            Assert.Equal(string.Empty, Clipboard.GetText());
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies SetAudio(byte[]) snapshots the input buffer and exposes identical WaveAudio data.
    [StaFact]
    public void SetAudio_ByteArray_CopiesAndRoundTripsData()
    {
        byte[] source = [1, 2, 3, 4, 5];

        try
        {
            Clipboard.SetAudio(source);
            source[0] = 42;

            Assert.True(Clipboard.ContainsAudio());
            Assert.True(Clipboard.ContainsData(DataFormats.WaveAudio));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, ReadAllBytes(Clipboard.GetAudioStream()));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, ReadAllBytes(Assert.IsAssignableFrom<Stream>(Clipboard.GetData(DataFormats.WaveAudio))));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies SetAudio(Stream) copies the complete stream, regardless of position, and outlives its source.
    [StaFact]
    public void SetAudio_Stream_CopiesWholeStreamAndSurvivesSourceDisposal()
    {
        MemoryStream source = new([6, 7, 8, 9]);
        source.Position = 2;

        try
        {
            Clipboard.SetAudio(source);
            source.Dispose();

            Assert.True(Clipboard.ContainsAudio());
            Assert.Equal(new byte[] { 6, 7, 8, 9 }, ReadAllBytes(Clipboard.GetAudioStream()));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies file-drop paths and returned collections are copied rather than sharing mutable state.
    [StaFact]
    public void SetFileDropList_CopiesAndRoundTripsPaths()
    {
        string first = Path.Combine(Path.GetTempPath(), $"clipboard-{Guid.NewGuid():N}-1.txt");
        string second = Path.Combine(Path.GetTempPath(), $"clipboard-{Guid.NewGuid():N}-2.txt");
        StringCollection source = new() { first, second };

        try
        {
            Clipboard.SetFileDropList(source);
            source.Clear();

            Assert.True(Clipboard.ContainsFileDropList());
            Assert.True(Clipboard.ContainsData(DataFormats.FileDrop));
            StringCollection returned = Clipboard.GetFileDropList();
            Assert.Equal(new[] { first, second }, returned.Cast<string>());
            Assert.Equal(new[] { first, second }, Assert.IsType<string[]>(Clipboard.GetData(DataFormats.FileDrop)));

            returned[0] = "changed";
            returned.RemoveAt(1);

            Assert.Equal(new[] { first, second }, Clipboard.GetFileDropList().Cast<string>());
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies SetImage snapshots source pixels and publishes a retrievable native bitmap representation.
    [StaFact]
    public void SetImage_CopiesAndRoundTripsPixels()
    {
        EnsureWpfGraphicsLoaded();
        byte[] originalPixels =
        [
            0x10, 0x20, 0x30, 0xFF,
            0x40, 0x50, 0x60, 0xFF,
        ];
        WriteableBitmap source = new(2, 1, 96, 96, PixelFormats.Bgra32, palette: null);
        source.WritePixels(new Int32Rect(0, 0, 2, 1), originalPixels, stride: 8, offset: 0);

        try
        {
            Clipboard.SetImage(source);
            source.WritePixels(
                new Int32Rect(0, 0, 2, 1),
                new byte[]
                {
                    0x70, 0x80, 0x90, 0xFF,
                    0xA0, 0xB0, 0xC0, 0xFF,
                },
                stride: 8,
                offset: 0);

            Assert.True(Clipboard.ContainsImage());
            Assert.True(Clipboard.ContainsData(DataFormats.Bitmap));
            BitmapSource image = Assert.IsAssignableFrom<BitmapSource>(Clipboard.GetImage());
            Assert.Equal(2, image.PixelWidth);
            Assert.Equal(1, image.PixelHeight);
            Assert.NotSame(source, image);
            BitmapSource normalized = new FormatConvertedBitmap(image, PixelFormats.Bgr24, null, 0);
            byte[] actualPixels = new byte[6];
            normalized.CopyPixels(actualPixels, stride: 6, offset: 0);
            Assert.Equal(
                new byte[]
                {
                    0x10, 0x20, 0x30,
                    0x40, 0x50, 0x60,
                },
                actualPixels);
            Assert.IsAssignableFrom<BitmapSource>(Clipboard.GetData(DataFormats.Bitmap));
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        Assert.NotNull(stream);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static void EnsureWpfGraphicsLoaded() =>
        _ = s_wpfGraphics.Value;

    private static T RetryClipboardAccess<T>(Func<T> operation)
    {
        const int ClipboardCannotOpen = unchecked((int)0x800401D0);

        for (int attemptsRemaining = 10; ; attemptsRemaining--)
        {
            try
            {
                return operation();
            }
            catch (COMException exception) when (exception.HResult == ClipboardCannotOpen && attemptsRemaining > 1)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static void AssertArgumentNull(string paramName, Action action) =>
        Assert.Equal(paramName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static string GetDataFormat(TextDataFormat format) =>
        format switch
        {
            TextDataFormat.Text => DataFormats.Text,
            TextDataFormat.UnicodeText => DataFormats.UnicodeText,
            TextDataFormat.Rtf => DataFormats.Rtf,
            TextDataFormat.Html => DataFormats.Html,
            TextDataFormat.CommaSeparatedValue => DataFormats.CommaSeparatedValue,
            TextDataFormat.Xaml => DataFormats.Xaml,
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat)),
        };
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ClipboardCollection
{
    public const string Name = "System clipboard serial";
}
