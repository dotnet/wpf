// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace System.Windows;

// Note: the OS Clipboard is a system wide resource and all access should be done sequentially to avoid
// collisions with other tests. We also retry as we cannot control other processes that may be using the clipboard.
[Collection("Sequential")]
[UISettings(MaxAttempts = 3)]
public class ClipboardTests
{
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
    // These three fail in WinForms, should probably fail in WPF as well.
    // [InlineData("")]
    // [InlineData(" ")]
    // [InlineData("\t")]
    [InlineData(null)]
    public void SetData_EmptyOrWhitespaceFormat_ThrowsArgumentException(string? format)
    {
        Action action = () => Clipboard.SetData(format!, "data");
        action.Should().Throw<ArgumentException>().WithParameterName("format");
    }

    [WpfFact]
    public void SetData_Null_Throws()
        {
            Action action = () => Clipboard.SetData("MyData", data: null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfFact]
    public void SetData_NullData_ThrowsArgumentNullException()
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
}
