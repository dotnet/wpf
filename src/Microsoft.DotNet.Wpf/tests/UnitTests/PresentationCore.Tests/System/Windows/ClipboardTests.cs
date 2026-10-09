// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Moq;
using Color = System.Windows.Media.Color;

namespace System.Windows;

// Note: the OS Clipboard is a system wide resource and all access should be done sequentially to avoid
// collisions with other tests. We also retry as we cannot control other processes that may be using the clipboard.
[Collection("Sequential")]
[UISettings(MaxAttempts = 3)]
public class ClipboardTests : IDisposable
{
    private static readonly Lazy<nint> s_wpfGraphics = new(
        () => NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "wpfgfx_cor3.dll")));

    // Isolate each STA test from clipboard ownership and delayed-rendering state left by earlier tests.
    public ClipboardTests() => ClearClipboardIfSta();

    // Release the managed clipboard owner before the UI test runner tears down this test's STA.
    public void Dispose() => ClearClipboardIfSta();

    [WpfFact]
    public void SetText_InvokeString_GetReturnsExpected()
    {
        Clipboard.SetText("text");
        Clipboard.GetText().Should().Be("text");
        Clipboard.ContainsText().Should().BeTrue();
    }

    [WpfFact]
    public void SetAudio_InvokeByteArray_GetReturnsExpected()
    {
        byte[] audioBytes = [1, 2, 3];
        Clipboard.SetAudio(audioBytes);

        Clipboard.GetAudioStream().Should().BeOfType<MemoryStream>().Which.ToArray().Should().Equal(audioBytes);
        Clipboard.GetData(DataFormats.WaveAudio).Should().BeOfType<MemoryStream>().Which.ToArray().Should().Equal(audioBytes);
        Clipboard.ContainsAudio().Should().BeTrue();
        Clipboard.ContainsData(DataFormats.WaveAudio).Should().BeTrue();
    }

    [WpfFact(Skip = "WinForms difference")]
    public void SetAudio_InvokeEmptyByteArray_GetReturnsExpected()
    {
        byte[] audioBytes = Array.Empty<byte>();
        Clipboard.SetAudio(audioBytes);

        // Currently fails with CLIPBRD_E_BAD_DATA
        Clipboard.GetAudioStream().Should().BeNull();
        Clipboard.GetData(DataFormats.WaveAudio).Should().BeNull();
        Clipboard.ContainsAudio().Should().BeTrue();
        Clipboard.ContainsData(DataFormats.WaveAudio).Should().BeTrue();
    }

    [WpfFact]
    public void SetAudio_NullAudioBytes_ThrowsArgumentNullException()
    {
        Action action = () => Clipboard.SetAudio((byte[])null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("audioBytes");
    }

    [WpfFact]
    public void Clipboard_SetAudio_InvokeStream_GetReturnsExpected()
    {
        byte[] audioBytes = [1, 2, 3];
        using MemoryStream audioStream = new(audioBytes);
        Clipboard.SetAudio(audioStream);

        Clipboard.GetAudioStream().Should().BeOfType<MemoryStream>().Which.ToArray().Should().Equal(audioBytes);
        Clipboard.GetData(DataFormats.WaveAudio).Should().BeOfType<MemoryStream>().Which.ToArray().Should().Equal(audioBytes);
        Clipboard.ContainsAudio().Should().BeTrue();
        Clipboard.ContainsData(DataFormats.WaveAudio).Should().BeTrue();
    }

    [WpfFact(Skip = "WinForms difference")]
    public void SetAudio_InvokeEmptyStream_GetReturnsExpected()
    {
        using MemoryStream audioStream = new();
        Clipboard.SetAudio(audioStream);

        // Currently fails with CLIPBRD_E_BAD_DATA
        Clipboard.GetAudioStream().Should().BeNull();
        Clipboard.GetData(DataFormats.WaveAudio).Should().BeNull();
        Clipboard.ContainsAudio().Should().BeTrue();
        Clipboard.ContainsData(DataFormats.WaveAudio).Should().BeTrue();
    }

    [WpfFact]
    public void SetAudio_NullAudioStream_ThrowsArgumentNullException()
    {
        Action action = () => Clipboard.SetAudio((Stream)null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("audioStream");
    }

    [WpfTheory(Skip = "Setting null in WinForms is allowed")]
    [InlineData("format", null)]
    [InlineData("format", 1)]
    public void SetData_Invoke_GetReturnsExpected(string format, object? data)
    {
        // Setting null in WinForms is allowed, but really should be blocked.
        // WinForms does allow setting "1" as data, WPF does, but gives back null currently.
        Clipboard.SetData(format, data!);
        Clipboard.GetData(format).Should().Be(data);
        Clipboard.ContainsData(format).Should().BeTrue();
    }

    [WpfTheory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void SetData_EmptyOrWhitespaceFormat_ThrowsArgumentException(string format)
    {
        Action action = () => Clipboard.SetData(format, "data");
        action.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    [WpfFact]
    public void SetData_Null_Throws()
    {
        Action action = () => Clipboard.SetData("MyData", data: null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfFact]
    public void SetData_Int_GetReturnsExpected()
    {
        Clipboard.SetData("format", 1);
        // WinForms allows setting "1" as data, WPF does, but gives back null currently.
        Clipboard.GetData("format").Should().Be(1);
        Clipboard.ContainsData("format").Should().BeTrue();
    }

    [WpfFact]
    public void SetFileDropList_Invoke_GetReturnsExpected()
    {
        StringCollection filePaths =
        [
            "filePath",
            "filePath2"
        ];

        Clipboard.SetFileDropList(filePaths);

        Clipboard.GetFileDropList().Should().BeEquivalentTo(filePaths);
        Clipboard.ContainsFileDropList().Should().BeTrue();
    }

    [WpfFact]
    public void SetFileDropList_NullFilePaths_ThrowsArgumentNullException()
    {
        Action action = () => Clipboard.SetFileDropList(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("filePaths");
    }

    [WpfFact]
    public void SetFileDropList_EmptyFilePaths_ThrowsArgumentException()
    {
        Action action = static () => Clipboard.SetFileDropList([]);
        action.Should().Throw<ArgumentException>();
    }

    [WpfTheory]
    [InlineData("")]
    [InlineData("\0")]
    public void SetFileDropList_InvalidFileInPaths_ThrowsArgumentException(string filePath)
    {
        StringCollection filePaths =
        [
            filePath
        ];

        Action action = () => Clipboard.SetFileDropList(filePaths);
        action.Should().Throw<ArgumentException>();
    }

    [WpfFact]
    public unsafe void SetImage_InvokeBitmap_VerifyPixelColor()
    {
        WriteableBitmap bitmap = new(10, 10, 96, 96, PixelFormats.Bgra32, palette: null);

        // Set a specific pixel to a given color (e.g., set pixel at (1, 2) to red)
        Color color = Colors.Red;
        byte[] colorData = [color.B, color.G, color.R, color.A];
        bitmap.WritePixels(new Int32Rect(1, 2, 1, 1), colorData, 4, 0);

        Clipboard.SetImage(bitmap);

        Clipboard.ContainsImage().Should().BeTrue();
        InteropBitmap result = Clipboard.GetImage().Should().BeOfType<InteropBitmap>().Subject;

        // Verify the pixel color
        byte[] resultColorData = new byte[4];
        result.CopyPixels(new Int32Rect(1, 2, 1, 1), resultColorData, 4, 0);
        resultColorData.Should().Equal(colorData);

        // Set back the image we just got from the clipboard
        Clipboard.SetImage(result);
        Clipboard.ContainsImage().Should().BeTrue();
        result = Clipboard.GetImage().Should().BeOfType<InteropBitmap>().Subject;

        // Verify the pixel color
        result.CopyPixels(new Int32Rect(1, 2, 1, 1), resultColorData, 4, 0);
        resultColorData.Should().Equal(colorData);
    }

    [WpfTheory]
    [BoolData]
    public void SetDataObject_WithMultipleData(bool copy)
    {
        string testData1 = "test data one";
        int testData2 = 42;
        DataObject data = new();
        data.SetData("testData1", testData1);
        data.SetData("testData2", testData2);
        Clipboard.SetDataObject(data, copy);

        object? result1 = Clipboard.GetData("testData1");
        result1.Should().Be(testData1);
        object? result2 = Clipboard.GetData("testData2");
        result2.Should().Be(testData2);
    }

    // Clipboard getters should observe the Windows conversions exposed by the OLE proxy, matching .NET 9.
    [WpfTheory]
    [InlineData(TextDataFormat.Text, TextDataFormat.UnicodeText)]
    [InlineData(TextDataFormat.UnicodeText, TextDataFormat.Text)]
    public void GetText_AutoConvertibleFormat_UsesOleFormatConversion(
        TextDataFormat sourceFormat,
        TextDataFormat requestedFormat)
    {
        const string text = "Hello, World!";
        string sourceDataFormat = DataFormats.ConvertToDataFormats(sourceFormat);
        string requestedDataFormat = DataFormats.ConvertToDataFormats(requestedFormat);
        DataObject dataObject = new();
        dataObject.SetData(sourceDataFormat, text, autoConvert: true);

        dataObject.GetData(requestedDataFormat, autoConvert: true).Should().Be(text);
        dataObject.GetData(requestedDataFormat, autoConvert: false).Should().BeNull();

        Clipboard.SetDataObject(dataObject);

        Clipboard.GetText(requestedFormat).Should().Be(text);
    }

    [WpfFact]
    public void SetData_Text_Format_AllUpper()
    {
        Clipboard.SetData("TEXT", "Hello, World!");
        Clipboard.ContainsText().Should().BeTrue();
        Clipboard.ContainsData("TEXT").Should().BeTrue();
        Clipboard.ContainsData(DataFormats.Text).Should().BeTrue();
        Clipboard.ContainsData(DataFormats.UnicodeText).Should().BeTrue();

        IDataObject dataObject = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
        string[] formats = dataObject.GetFormats();
        formats.Should().BeEquivalentTo(["System.String", "UnicodeText", "Text"]);

        formats = dataObject.GetFormats(autoConvert: false);
        formats.Should().BeEquivalentTo(["Text"]);

        // CLIPBRD_E_BAD_DATA returned when trying to get clipboard data.
        Clipboard.GetText().Should().BeEmpty();
        Clipboard.GetText(TextDataFormat.Text).Should().BeEmpty();
        Clipboard.GetText(TextDataFormat.UnicodeText).Should().BeEmpty();

        Clipboard.GetData("System.String").Should().BeNull();
        Clipboard.GetData("TEXT").Should().BeNull();
    }

    [WpfFact]
    public void FormatTakingApis_NullFormat_ThrowArgumentNullException()
    {
        Action containsData = () => Clipboard.ContainsData(null!);
        Action getData = () => Clipboard.GetData(null!);
        Action setData = () => Clipboard.SetData(null!, "value");

        containsData.Should().Throw<ArgumentNullException>().WithParameterName("format");
        getData.Should().Throw<ArgumentNullException>().WithParameterName("format");
        setData.Should().Throw<ArgumentNullException>().WithParameterName("format");
    }

    // SetData empty-format validation is covered above.
    [WpfFact]
    public void ContainsDataAndGetData_EmptyFormat_ThrowArgumentException()
    {
        Action containsData = () => Clipboard.ContainsData(string.Empty);
        Action getData = () => Clipboard.GetData(string.Empty);

        containsData.Should().Throw<ArgumentException>().WithParameterName("format");
        getData.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    [WpfFact]
    public void SetApis_NullData_ThrowArgumentNullException()
    {
        Action setImage = () => Clipboard.SetImage(null!);
        Action setText = () => Clipboard.SetText(null!);
        Action setFormattedText = () => Clipboard.SetText(null!, TextDataFormat.Text);
        Action setDataObject = () => Clipboard.SetDataObject(null!);
        Action setDataObjectWithoutCopy = () => Clipboard.SetDataObject(null!, copy: false);

        setImage.Should().Throw<ArgumentNullException>().WithParameterName("image");
        setText.Should().Throw<ArgumentNullException>().WithParameterName("text");
        setFormattedText.Should().Throw<ArgumentNullException>().WithParameterName("text");
        setDataObject.Should().Throw<ArgumentNullException>().WithParameterName("data");
        setDataObjectWithoutCopy.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfTheory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void TextFormatApis_InvalidFormat_ThrowInvalidEnumArgumentException(int value)
    {
        TextDataFormat format = (TextDataFormat)value;
        Action containsText = () => Clipboard.ContainsText(format);
        Action getText = () => Clipboard.GetText(format);
        Action setText = () => Clipboard.SetText("value", format);

        containsText.Should().Throw<InvalidEnumArgumentException>().WithParameterName("format");
        getText.Should().Throw<InvalidEnumArgumentException>().WithParameterName("format");
        setText.Should().Throw<InvalidEnumArgumentException>().WithParameterName("format");
    }

    [WpfFact]
    public void IsCurrent_NullData_ThrowsArgumentNullException()
    {
        Action action = () => Clipboard.IsCurrent(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfFact]
    public void IsCurrent_ManagedIDataObject_ReturnsFalseWithoutOleCall()
    {
        IDataObject dataObject = new Mock<IDataObject>(MockBehavior.Strict).Object;

        Clipboard.IsCurrent(dataObject).Should().BeFalse();
    }

    [WpfFact]
    public void SetFileDropList_NullPath_ThrowsArgumentException()
    {
        StringCollection paths = new() { null! };
        Action action = () => Clipboard.SetFileDropList(paths);

        action.Should().Throw<ArgumentException>();
    }

    // OLE-backed Clipboard APIs require STA, so exercise MTA explicitly without changing the WpfFact convention.
    [WpfFact]
    public void OleBackedClipboardApis_MtaThread_ThrowThreadStateException()
    {
        EnsureWpfGraphicsLoaded();
        using MemoryStream audioStream = new([1, 2, 3]);
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
            () => Clipboard.SetDataObject(dataObject, copy: true),
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

            exception.Should().BeOfType<ThreadStateException>();
        }
    }

    // Contains APIs use native format availability and do not require an OLE apartment.
    [WpfFact]
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

            exception.Should().BeNull();
        }
    }

    [WpfFact]
    public void Clear_PopulatedClipboard_ResetsAllTypedViews()
    {
        Clipboard.SetText("value");

        Clipboard.Clear();

        IDataObject dataObject = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
        RetryClipboardAccess(dataObject.GetFormats).Should().BeEmpty();
        Clipboard.ContainsAudio().Should().BeFalse();
        Clipboard.ContainsData(DataFormats.UnicodeText).Should().BeFalse();
        Clipboard.ContainsFileDropList().Should().BeFalse();
        Clipboard.ContainsImage().Should().BeFalse();
        Clipboard.ContainsText().Should().BeFalse();
        Clipboard.ContainsText(TextDataFormat.UnicodeText).Should().BeFalse();
        Clipboard.GetAudioStream().Should().BeNull();
        Clipboard.GetData(DataFormats.UnicodeText).Should().BeNull();
        Clipboard.GetFileDropList().Cast<string>().Should().BeEmpty();
        Clipboard.GetImage().Should().BeNull();
        Clipboard.GetText().Should().BeEmpty();
        Clipboard.GetText(TextDataFormat.UnicodeText).Should().BeEmpty();
    }

    // Verifies the production Clipboard retry loop survives temporary native clipboard ownership by another thread.
    [WpfFact]
    public void Clear_ClipboardTemporarilyLocked_RetriesUntilAvailable()
    {
        using ManualResetEventSlim clipboardOpened = new();
        using ManualResetEventSlim releaseClipboard = new();
        Exception? lockerException = null;

        Thread locker = new(() =>
        {
            bool opened = false;
            try
            {
                opened = OpenClipboardWithRetry();
                clipboardOpened.Set();
                releaseClipboard.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception exception)
            {
                lockerException = exception;
                clipboardOpened.Set();
            }
            finally
            {
                if (opened && !CloseClipboard())
                {
                    lockerException ??= new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        })
        {
            IsBackground = true,
            Name = $"{nameof(ClipboardTests)} native clipboard owner"
        };
        locker.SetApartmentState(ApartmentState.MTA);
        locker.Start();

        clipboardOpened.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        lockerException.Should().BeNull();

        Thread releaser = new(() =>
        {
            Thread.Sleep(250);
            releaseClipboard.Set();
        })
        {
            IsBackground = true,
            Name = $"{nameof(ClipboardTests)} native clipboard releaser"
        };
        releaser.Start();

        try
        {
            // This call must initially receive CLIPBRD_E_CANT_OPEN and then succeed after the locker releases.
            Clipboard.Clear();
        }
        finally
        {
            releaseClipboard.Set();
            releaser.Join();
            locker.Join();
        }

        lockerException.Should().BeNull();
    }

    [WpfFact]
    public void SetData_CustomFormat_RoundTripsThroughIDataObject()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        const string value = "custom clipboard value";

        try
        {
            Clipboard.SetData(format, value);
            DataFormat registeredFormat = DataFormats.GetDataFormat(format);

            Clipboard.ContainsData(format).Should().BeTrue();
            Clipboard.ContainsData(format.ToUpperInvariant()).Should().BeTrue();
            Clipboard.GetData(format).Should().Be(value);
            Clipboard.GetData(format.ToUpperInvariant()).Should().Be(value);
            registeredFormat.Name.Should().Be(format);
            DataFormats.GetDataFormat(registeredFormat.Id).Should().BeSameAs(registeredFormat);
            DataFormats.GetDataFormat(format.ToUpperInvariant()).Should().BeSameAs(registeredFormat);

            IDataObject dataObject = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
            RetryClipboardAccess(() => dataObject.GetDataPresent(format)).Should().BeTrue();
            RetryClipboardAccess(() => dataObject.GetDataPresent(format, autoConvert: false)).Should().BeTrue();
            RetryClipboardAccess(() => dataObject.GetData(format)).Should().Be(value);
            RetryClipboardAccess(() => dataObject.GetData(format, autoConvert: false)).Should().Be(value);
            RetryClipboardAccess(dataObject.GetFormats).Should().Contain(format);
            RetryClipboardAccess(() => dataObject.GetFormats(autoConvert: false)).Should().Contain(format);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_DefaultAndFalseOverloads_PreserveLiveDataObject()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "first");

        try
        {
            Clipboard.SetDataObject(dataObject);

            Clipboard.IsCurrent(dataObject).Should().BeTrue();
            dataObject.SetData(format, "first changed");
            Clipboard.GetData(format).Should().Be("first changed");

            Clipboard.Clear();
            dataObject.SetData(format, "second");
            Clipboard.SetDataObject(dataObject, copy: false);

            Clipboard.IsCurrent(dataObject).Should().BeTrue();
            dataObject.SetData(format, "second changed");
            Clipboard.GetData(format).Should().Be("second changed");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_ReplacingCurrentObject_UpdatesIsCurrent()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject first = new(format, "first");
        DataObject second = new(format, "second");

        try
        {
            Clipboard.SetDataObject(first, copy: false);
            Clipboard.IsCurrent(first).Should().BeTrue();

            Clipboard.SetDataObject(second, copy: false);

            Clipboard.IsCurrent(first).Should().BeFalse();
            Clipboard.IsCurrent(second).Should().BeTrue();
            Clipboard.GetData(format).Should().Be("second");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_LiveDataObject_PreservesIDataObjectOverloads()
    {
        string nativeFormat = $"WPF ClipboardTests {Guid.NewGuid():N}";
        const int typedValue = 42;
        DataObject source = new();
        source.SetData("typed string");
        source.SetData(typeof(int), typedValue);
        source.SetData(nativeFormat, "native", autoConvert: false);

        try
        {
            Clipboard.SetDataObject(source, copy: false);

            IDataObject result = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
            result.GetDataPresent(typeof(string)).Should().BeTrue();
            result.GetDataPresent(typeof(int)).Should().BeTrue();
            result.GetDataPresent(nativeFormat).Should().BeTrue();
            result.GetDataPresent(nativeFormat, autoConvert: false).Should().BeTrue();
            result.GetData(typeof(string)).Should().Be("typed string");
            result.GetData(typeof(int)).Should().Be(typedValue);
            result.GetData(nativeFormat).Should().Be("native");
            result.GetData(nativeFormat, autoConvert: false).Should().Be("native");
            result.GetFormats().Should().Contain(nativeFormat);
            result.GetFormats(autoConvert: false).Should().Contain(nativeFormat);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // The system synthesizes UnicodeText from ANSI Text even when source auto-conversion is disabled.
    [WpfFact]
    public void SetDataObject_AnsiText_ExposesSystemGeneratedUnicodeTextToClipboardApis()
    {
        const string value = "ahoj";
        DataObject source = new();
        source.SetText(value, TextDataFormat.Text);

        try
        {
            source.GetDataPresent(DataFormats.UnicodeText).Should().BeFalse();
            source.GetDataPresent(DataFormats.UnicodeText, autoConvert: false).Should().BeFalse();

            Clipboard.SetDataObject(source);

            IDataObject result = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
            result.Should().NotBeSameAs(source);
            RetryClipboardAccess(() => result.GetDataPresent(DataFormats.UnicodeText)).Should().BeTrue();
            RetryClipboardAccess(() => result.GetDataPresent(DataFormats.UnicodeText, autoConvert: false)).Should().BeTrue();
            RetryClipboardAccess(() => result.GetData(DataFormats.UnicodeText)).Should().Be(value);
            RetryClipboardAccess(() => result.GetData(DataFormats.UnicodeText, autoConvert: false)).Should().Be(value);
            Clipboard.ContainsText().Should().BeTrue();
            Clipboard.ContainsText(TextDataFormat.Text).Should().BeTrue();
            Clipboard.ContainsText(TextDataFormat.UnicodeText).Should().BeTrue();
            Clipboard.GetText().Should().Be(value);
            Clipboard.GetText(TextDataFormat.Text).Should().Be(value);
            Clipboard.GetText(TextDataFormat.UnicodeText).Should().Be(value);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataObject_TextAutoConvert_ControlsMappedFormatAdvertisement(bool autoConvert)
    {
        const string value = "text conversion verification";
        string[] mappedFormats =
        [
            DataFormats.Text,
            DataFormats.UnicodeText,
            DataFormats.StringFormat,
        ];
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.Text, value, autoConvert);

        foreach (string format in mappedFormats)
        {
            bool expected = autoConvert || format == DataFormats.Text;

            dataObject.GetDataPresent(format).Should().Be(expected);
            dataObject.GetDataPresent(format, autoConvert: false).Should().Be(format == DataFormats.Text);
            dataObject.GetData(format).Should().Be(value);
            dataObject.GetData(format, autoConvert: false).Should().Be(format == DataFormats.Text ? value : null);
        }

        string[] expectedFormats = autoConvert ? mappedFormats : [DataFormats.Text];
        dataObject.GetFormats().Should().BeEquivalentTo(expectedFormats);
        dataObject.GetFormats(autoConvert: false).Should().Equal(DataFormats.Text);
    }

    [WpfFact]
    public void DataObject_AutoConvert_ControlsAllBuiltInFormatMappings()
    {
        string[][] formatGroups =
        [
            [DataFormats.Text, DataFormats.UnicodeText, DataFormats.StringFormat],
            [DataFormats.FileDrop, "FileNameW", "FileName"],
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

                    dataObject.GetFormats().Should().BeEquivalentTo(expectedFormats);
                    dataObject.GetFormats(autoConvert: false).Should().Equal(sourceFormat);

                    foreach (string targetFormat in formatGroup)
                    {
                        dataObject.GetDataPresent(targetFormat).Should().Be(autoConvert || targetFormat == sourceFormat);
                        dataObject.GetDataPresent(targetFormat, autoConvert: false).Should().Be(targetFormat == sourceFormat);
                    }
                }
            }
        }
    }

    // Verifies WPF's platform mapping restores BitmapSource as a Bitmap synonym without changing shared mappings.
    [WpfFact]
    public void SetDataObject_BitmapSourceFormat_UsesLegacyBitmapAutoConversion()
    {
        EnsureWpfGraphicsLoaded();
        byte[] pixels = [0x10, 0x20, 0x30, 0xFF];
        WriteableBitmap sourceImage = new(1, 1, 96, 96, PixelFormats.Bgra32, palette: null);
        sourceImage.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, stride: 4, offset: 0);
        DataObject source = new();
        source.SetData(typeof(BitmapSource), sourceImage);

        try
        {
            Clipboard.SetDataObject(source);

            Clipboard.ContainsImage().Should().BeTrue();
            Clipboard.GetData(DataFormats.Bitmap).Should().BeAssignableTo<BitmapSource>();
            BitmapSource result = Clipboard.GetImage().Should().BeAssignableTo<BitmapSource>().Subject;
            byte[] resultPixels = new byte[4];
            new FormatConvertedBitmap(result, PixelFormats.Bgra32, null, 0)
                .CopyPixels(resultPixels, stride: 4, offset: 0);
            resultPixels.Should().Equal(pixels);

            IDataObject dataObject = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
            dataObject.GetFormats().Should().Contain(
                DataFormats.Bitmap,
                typeof(System.Drawing.Bitmap).FullName!,
                typeof(BitmapSource).FullName!);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_RawObject_WrapsItInADataObject()
    {
        const string value = "raw string";

        try
        {
            Clipboard.SetDataObject(value);

            Clipboard.ContainsText().Should().BeTrue();
            Clipboard.GetText().Should().Be(value);
            IDataObject result = Clipboard.GetDataObject().Should().BeAssignableTo<IDataObject>().Subject;
            result.GetDataPresent(typeof(string)).Should().BeTrue();
            result.GetData(typeof(string)).Should().Be(value);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void Flush_NonPersistentDataObject_RendersDataAndReleasesSource()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "persisted");

        try
        {
            Clipboard.SetDataObject(dataObject, copy: false);
            Clipboard.IsCurrent(dataObject).Should().BeTrue();

            Clipboard.Flush();

            Clipboard.IsCurrent(dataObject).Should().BeFalse();
            Clipboard.GetData(format).Should().Be("persisted");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_CopyTrue_RendersDataAndReleasesSource()
    {
        string format = $"WPF ClipboardTests {Guid.NewGuid():N}";
        DataObject dataObject = new(format, "persisted");

        try
        {
            Clipboard.SetDataObject(dataObject, copy: true);
            dataObject.SetData(format, "changed after copy");

            Clipboard.IsCurrent(dataObject).Should().BeFalse();
            Clipboard.GetData(format).Should().Be("persisted");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetText_DefaultOverload_RoundTripsUnicodeText()
    {
        const string value = "Clipboard \u03A9 \U0001F600";

        try
        {
            Clipboard.SetText(value);

            Clipboard.ContainsText().Should().BeTrue();
            Clipboard.ContainsText(TextDataFormat.UnicodeText).Should().BeTrue();
            Clipboard.ContainsData(DataFormats.UnicodeText).Should().BeTrue();
            Clipboard.GetText().Should().Be(value);
            Clipboard.GetText(TextDataFormat.UnicodeText).Should().Be(value);
            Clipboard.GetData(DataFormats.UnicodeText).Should().Be(value);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
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

            Clipboard.ContainsText(format).Should().BeTrue();
            Clipboard.ContainsData(dataFormat).Should().BeTrue();
            Clipboard.GetText(format).Should().Be(value);
            Clipboard.GetData(dataFormat).Should().Be(value);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetText_EmptyString_RoundTrips()
    {
        try
        {
            Clipboard.SetText(string.Empty);

            Clipboard.ContainsText().Should().BeTrue();
            Clipboard.GetText().Should().BeEmpty();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetAudio_ByteArray_CopiesAndRoundTripsData()
    {
        byte[] source = [1, 2, 3, 4, 5];

        try
        {
            Clipboard.SetAudio(source);
            source[0] = 42;

            Clipboard.ContainsAudio().Should().BeTrue();
            Clipboard.ContainsData(DataFormats.WaveAudio).Should().BeTrue();
            ReadAllBytes(Clipboard.GetAudioStream()).Should().Equal(1, 2, 3, 4, 5);
            Stream waveData = Clipboard.GetData(DataFormats.WaveAudio).Should().BeAssignableTo<Stream>().Subject;
            ReadAllBytes(waveData).Should().Equal(1, 2, 3, 4, 5);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetAudio_Stream_CopiesWholeStreamAndSurvivesSourceDisposal()
    {
        MemoryStream source = new([6, 7, 8, 9])
        {
            Position = 2
        };

        try
        {
            Clipboard.SetAudio(source);
            source.Dispose();

            Clipboard.ContainsAudio().Should().BeTrue();
            ReadAllBytes(Clipboard.GetAudioStream()).Should().Equal(6, 7, 8, 9);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetFileDropList_CopiesAndRoundTripsPaths()
    {
        string first = Path.Combine(Path.GetTempPath(), $"clipboard-{Guid.NewGuid():N}-1.txt");
        string second = Path.Combine(Path.GetTempPath(), $"clipboard-{Guid.NewGuid():N}-2.txt");
        StringCollection source = new() { first, second };

        try
        {
            Clipboard.SetFileDropList(source);
            source.Clear();

            Clipboard.ContainsFileDropList().Should().BeTrue();
            Clipboard.ContainsData(DataFormats.FileDrop).Should().BeTrue();
            StringCollection returned = Clipboard.GetFileDropList();
            returned.Cast<string>().Should().Equal(first, second);
            Clipboard.GetData(DataFormats.FileDrop).Should().BeOfType<string[]>().Which.Should().Equal(first, second);

            returned[0] = "changed";
            returned.RemoveAt(1);

            Clipboard.GetFileDropList().Cast<string>().Should().Equal(first, second);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
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

            Clipboard.ContainsImage().Should().BeTrue();
            Clipboard.ContainsData(DataFormats.Bitmap).Should().BeTrue();
            BitmapSource image = Clipboard.GetImage().Should().BeAssignableTo<BitmapSource>().Subject;
            image.PixelWidth.Should().Be(2);
            image.PixelHeight.Should().Be(1);
            image.Should().NotBeSameAs(source);
            BitmapSource normalized = new FormatConvertedBitmap(image, PixelFormats.Bgr24, null, 0);
            byte[] actualPixels = new byte[6];
            normalized.CopyPixels(actualPixels, stride: 6, offset: 0);
            actualPixels.Should().Equal(0x10, 0x20, 0x30, 0x40, 0x50, 0x60);
            Clipboard.GetData(DataFormats.Bitmap).Should().BeAssignableTo<BitmapSource>();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies delayed native rendering returns an independent, renderable EMF without invalidating the source.
    [WpfFact]
    public void SetDataObject_Metafile_CopyFalse_RoundTripsWhileSourceIsAlive()
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject sourceDataObject = new();
        sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

        try
        {
            Clipboard.SetDataObject(sourceDataObject, copy: false);

            Clipboard.IsCurrent(sourceDataObject).Should().BeTrue();
            Clipboard.ContainsData(DataFormats.EnhancedMetafile).Should().BeTrue();

            DataObject clipboardDataObject = RetryClipboardAccess(Clipboard.GetDataObject)
                .Should().BeOfType<DataObject>().Subject;
            RetryClipboardAccess(
                () => clipboardDataObject.GetDataPresent(
                    DataFormats.EnhancedMetafile,
                    autoConvert: false)).Should().BeTrue();
            RetryClipboardAccess(
                () => clipboardDataObject.GetDataPresent(
                    typeof(Metafile).FullName!,
                    autoConvert: true)).Should().BeTrue();

            using (Metafile legacyResult = RetryClipboardAccess(
                () => Clipboard.GetData(DataFormats.EnhancedMetafile))
                .Should().BeOfType<Metafile>().Subject)
            {
                legacyResult.Should().NotBeSameAs(source);
                EmfTestData.AssertValid(legacyResult);
            }

            EmfTestData.AssertValid(source);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    // Verifies Flush persists renderable EMF data after both clipboard ownership and the source object are released.
    [WpfFact]
    public void SetDataObject_Metafile_CopyFalseFlush_PersistsAfterSourceDisposal()
    {
        Metafile? source = EmfTestData.CreateMetafile();

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: false);
                Clipboard.IsCurrent(sourceDataObject).Should().BeTrue();

                RetryClipboardAccess(
                    () =>
                    {
                        Clipboard.Flush();
                        return true;
                    }).Should().BeTrue();

                Clipboard.IsCurrent(sourceDataObject).Should().BeFalse();
                source.Dispose();
                source = null;

                using Metafile legacyResult = RetryClipboardAccess(
                    () => Clipboard.GetData(DataFormats.EnhancedMetafile))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(legacyResult);

                using Metafile typedResult = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                EmfTestData.AssertValid(typedResult);
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            source?.Dispose();
        }
    }

    // Verifies copy:true eagerly persists a renderable EMF that no longer depends on the source Metafile.
    [WpfFact]
    public void SetDataObject_Metafile_CopyTrue_PersistsAfterSourceDisposal()
    {
        Metafile? source = EmfTestData.CreateMetafile();

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: true);
                Clipboard.IsCurrent(sourceDataObject).Should().BeFalse();

                source.Dispose();
                source = null;

                Clipboard.ContainsData(DataFormats.EnhancedMetafile).Should().BeTrue();
                using Metafile legacyResult = RetryClipboardAccess(
                    () => Clipboard.GetData(DataFormats.EnhancedMetafile))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(legacyResult);

                using Metafile typedResult = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                EmfTestData.AssertValid(typedResult);
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            source?.Dispose();
        }
    }

    // Verifies the CLR Metafile alias is available only through auto-conversion when the source disables mappings.
    [WpfFact]
    public void SetDataObject_Metafile_MappedFormatRequiresAutoConvert()
    {
        Metafile? source = EmfTestData.CreateMetafile();

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: true);
                source.Dispose();
                source = null;

                DataObject clipboardDataObject = RetryClipboardAccess(Clipboard.GetDataObject)
                    .Should().BeOfType<DataObject>().Subject;
                string mappedFormat = typeof(Metafile).FullName!;

                Clipboard.ContainsData(DataFormats.EnhancedMetafile).Should().BeTrue();
                Clipboard.ContainsData(mappedFormat).Should().BeFalse();

                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        DataFormats.EnhancedMetafile,
                        autoConvert: false)).Should().BeTrue();
                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        mappedFormat,
                        autoConvert: false)).Should().BeFalse();
                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        mappedFormat,
                        autoConvert: true)).Should().BeTrue();

                using Metafile exactResult = RetryClipboardAccess(
                    () => clipboardDataObject.GetData(
                        DataFormats.EnhancedMetafile,
                        autoConvert: false))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(exactResult);

                using Metafile mappedResult = RetryClipboardAccess(
                    () => clipboardDataObject.GetData(
                        mappedFormat,
                        autoConvert: true))
                    .Should().BeOfType<Metafile>().Subject;
                mappedResult.Should().NotBeSameAs(exactResult);
                EmfTestData.AssertValid(mappedResult);

                using Metafile convertedResult = GetTypedDataObjectMetafile(
                    clipboardDataObject,
                    mappedFormat,
                    autoConvert: true);
                EmfTestData.AssertValid(convertedResult);
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            source?.Dispose();
        }
    }

    // Verifies an auto-convertible EMF advertises and retrieves both native and CLR mapped formats.
    [WpfFact]
    public void SetDataObject_Metafile_AutoConvertTrue_ExposesMappedIDataObjectFormat()
    {
        Metafile? source = EmfTestData.CreateMetafile();

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: true);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: false);
                Clipboard.IsCurrent(sourceDataObject).Should().BeTrue();

                string mappedFormat = typeof(Metafile).FullName!;
                Clipboard.ContainsData(DataFormats.EnhancedMetafile).Should().BeTrue();
                Clipboard.ContainsData(mappedFormat).Should().BeTrue();

                DataObject clipboardDataObject = RetryClipboardAccess(Clipboard.GetDataObject)
                    .Should().BeOfType<DataObject>().Subject;

                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        DataFormats.EnhancedMetafile,
                        autoConvert: false)).Should().BeTrue();
                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        mappedFormat,
                        autoConvert: false)).Should().BeTrue();
                RetryClipboardAccess(
                    () => clipboardDataObject.GetDataPresent(
                        mappedFormat,
                        autoConvert: true)).Should().BeTrue();

                using Metafile nativeResult = RetryClipboardAccess(
                    () => clipboardDataObject.GetData(
                        DataFormats.EnhancedMetafile,
                        autoConvert: false))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(nativeResult);

                using Metafile mappedResult = RetryClipboardAccess(
                    () => clipboardDataObject.GetData(
                        mappedFormat,
                        autoConvert: false))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(mappedResult);

                using Metafile clipboardResult = RetryClipboardAccess(
                    () => Clipboard.GetData(DataFormats.EnhancedMetafile))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(clipboardResult);

                using Metafile mappedClipboardResult = RetryClipboardAccess(
                    () => Clipboard.GetData(mappedFormat))
                    .Should().BeOfType<Metafile>().Subject;
                EmfTestData.AssertValid(mappedClipboardResult);

                using Metafile typedResult = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                EmfTestData.AssertValid(typedResult);
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            source?.Dispose();
        }
    }

    // Verifies repeated EMF reads return independent native copies whose disposal does not affect each other.
    [WpfFact]
    public void SetDataObject_Metafile_RepeatedGetsReturnIndependentlyDisposableResults()
    {
        Metafile? source = EmfTestData.CreateMetafile();

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: true);
                source.Dispose();
                source = null;

                Metafile? first = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                Metafile? second = null;
                try
                {
                    second = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                    second.Should().NotBeSameAs(first);
                    EmfTestData.AssertValid(first);

                    first.Dispose();
                    first = null;
                    EmfTestData.AssertValid(second);
                }
                finally
                {
                    first?.Dispose();
                    second?.Dispose();
                }
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            source?.Dispose();
        }
    }

    // Verifies a non-exposable EMF stream is fully rendered despite its current position and survives source disposal.
    [WpfFact]
    public void SetDataObject_ValidMetafileStream_CopyTrue_RoundTripsAsMetafile()
    {
        byte[] bytes;
        using (Metafile metafile = EmfTestData.CreateMetafile())
        {
            bytes = EmfTestData.GetBytes(metafile);
        }

        MemoryStream? sourceStream = EmfTestData.CreateNonExposableStream(bytes);
        sourceStream.Position = Math.Min(7, sourceStream.Length);

        try
        {
            DataObject sourceDataObject = new();
            sourceDataObject.SetData(DataFormats.EnhancedMetafile, sourceStream, autoConvert: false);

            try
            {
                Clipboard.SetDataObject(sourceDataObject, copy: true);
                sourceStream.Dispose();
                sourceStream = null;

                Clipboard.ContainsData(DataFormats.EnhancedMetafile).Should().BeTrue();
                using Metafile result = GetTypedClipboardMetafile(DataFormats.EnhancedMetafile);
                EmfTestData.AssertValid(result);
            }
            finally
            {
                Clipboard.Clear();
            }
        }
        finally
        {
            sourceStream?.Dispose();
        }
    }

    // Verifies WMF data mislabeled as EnhancedMetafile is rejected without invalidating the caller's WMF object.
    [WpfFact]
    public void SetDataObject_WmfAsEnhancedMetafile_CopyTrue_DoesNotReturnEmf()
    {
        using MemoryStream stream = new(EmfTestData.CreateWmfBytes(), writable: false);
        using Metafile source = new(stream);
        source.GetMetafileHeader().IsWmf().Should().BeTrue();
        DataObject sourceDataObject = new();
        sourceDataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

        try
        {
            Clipboard.SetDataObject(sourceDataObject, copy: true);

            Clipboard.GetData(DataFormats.EnhancedMetafile).Should().BeNull();
            source.GetMetafileHeader().IsWmf().Should().BeTrue();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    private static byte[] ReadAllBytes(Stream? stream)
    {
        stream.Should().NotBeNull();
        Stream nonNullStream = stream!;
        if (nonNullStream.CanSeek)
        {
            nonNullStream.Position = 0;
        }

        using MemoryStream copy = new();
        nonNullStream.CopyTo(copy);
        return copy.ToArray();
    }

    private static void EnsureWpfGraphicsLoaded() =>
        _ = s_wpfGraphics.Value;

    private static void ClearClipboardIfSta()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            RetryClipboardAccess(
                () =>
                {
                    Clipboard.Clear();
                    return true;
                });
        }
    }

    private static Metafile GetTypedClipboardMetafile(string format)
    {
        Metafile? result = null;
        try
        {
            RetryClipboardAccess(
                () => Clipboard.TryGetData(format, out result)).Should().BeTrue();
            result.Should().NotBeNull();
            return result!;
        }
        catch
        {
            result?.Dispose();
            throw;
        }
    }

    private static Metafile GetTypedDataObjectMetafile(
        DataObject dataObject,
        string format,
        bool autoConvert)
    {
        Metafile? result = null;
        try
        {
            RetryClipboardAccess(
                () => dataObject.TryGetData(
                    format,
                    autoConvert,
                    out result)).Should().BeTrue();
            result.Should().NotBeNull();
            return result!;
        }
        catch
        {
            result?.Dispose();
            throw;
        }
    }

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

    private static bool OpenClipboardWithRetry()
    {
        for (int attemptsRemaining = 10; attemptsRemaining > 0; attemptsRemaining--)
        {
            if (OpenClipboard(0))
            {
                return true;
            }

            Thread.Sleep(100);
        }

        throw new Win32Exception(Marshal.GetLastWin32Error());
    }

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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(nint newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();
}
