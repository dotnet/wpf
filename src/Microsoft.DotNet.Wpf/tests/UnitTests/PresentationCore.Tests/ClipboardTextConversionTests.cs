// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace System.Windows.Tests;

// Clipboard synthesis requires the real, process-wide clipboard, not a mocked data object.
// Prevent other test collections from replacing clipboard contents while these assertions run.
[CollectionDefinition("ClipboardTextConversion", DisableParallelization = true)]
public class ClipboardTextConversionCollection
{
}

[Collection("ClipboardTextConversion")]
public class ClipboardTextConversionTests
{
    [WpfTheory]
    [InlineData(TextDataFormat.Text, false)]
    [InlineData(TextDataFormat.Text, true)]
    [InlineData(TextDataFormat.UnicodeText, false)]
    [InlineData(TextDataFormat.UnicodeText, true)]
    public void SetDataObject_SetText_ClipboardSynthesizesText(TextDataFormat format, bool copy)
    {
        // SetText stores only the requested format and disables managed conversion.
        // Verify Windows still supplies the other plain-text formats, both before and after flushing.
        DataObject source = new();
        source.SetText("test string", format);

        try
        {
            Clipboard.SetDataObject(source, copy);
            AssertClipboardText();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
    [InlineData("Text", false)]
    [InlineData("Text", true)]
    [InlineData("UnicodeText", false)]
    [InlineData("UnicodeText", true)]
    [InlineData("OEMText", false)]
    [InlineData("OEMText", true)]
    public void SetDataObject_TextWithoutAutoConvert_ClipboardSynthesizesText(string format, bool copy)
    {
        // Exercise Text, UnicodeText and OEMText directly with managed conversion disabled.
        // Clipboard reads must expose native synthesis without adding formats to the original source.
        DataObject source = new();
        source.SetData(format, "test string", autoConvert: false);

        try
        {
            Clipboard.SetDataObject(source, copy);
            AssertClipboardText();

            // Reading through the clipboard must not add converted formats to the managed source.
            source.GetFormats(autoConvert: false).Should().Equal(format);
            foreach (string target in new[] { DataFormats.Text, DataFormats.UnicodeText, DataFormats.OemText })
            {
                source.GetDataPresent(target, autoConvert: false).Should().Be(target == format);
            }
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
    [InlineData("Text")]
    [InlineData("UnicodeText")]
    [InlineData("OEMText")]
    public void ContainsText_TextAddedAfterPublishing_UsesPublishedClipboardFormats(string format)
    {
        // Guard against reporting text that exists only in the managed source after publication.
        // Availability, including case-insensitive format names, must reflect Windows' published formats.
        DataObject source = new();
        source.SetData("CustomPayload", "sentinel", autoConvert: false);

        try
        {
            Clipboard.SetDataObject(source, copy: false);
            source.SetData(format, "test string", autoConvert: false);

            // Changing the managed source does not publish new formats to the Windows clipboard.
            source.GetDataPresent(format, autoConvert: false).Should().BeTrue();
            Clipboard.ContainsData(format).Should().BeFalse();
            Clipboard.ContainsData(format.ToUpperInvariant()).Should().BeFalse();
            Clipboard.ContainsData(format.ToLowerInvariant()).Should().BeFalse();
            Clipboard.ContainsText().Should().BeFalse();
            Clipboard.ContainsText(TextDataFormat.Text).Should().BeFalse();
            Clipboard.ContainsText(TextDataFormat.UnicodeText).Should().BeFalse();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_Rtf_DoesNotSynthesizePlainText()
    {
        // Negative control: publishing rich text must not make plain text appear available.
        // The native fallback is for Text, UnicodeText and OEMText, not arbitrary text-like formats.
        DataObject source = new();
        source.SetData(DataFormats.Rtf, @"{\rtf1 test string}", autoConvert: false);

        try
        {
            Clipboard.SetDataObject(source);
            Clipboard.ContainsText().Should().BeFalse();
            Clipboard.GetText().Should().BeEmpty();
            Clipboard.GetDataObject()!.GetDataPresent(DataFormats.UnicodeText).Should().BeFalse();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetDataObject_CustomData_PreservesManagedPayload(bool includeText)
    {
        // A nonserializable payload must retain reference identity rather than round-tripping through OLE.
        // Adding text should enable native synthesis without losing that managed retrieval path.
        object payload = new();
        DataObject source = new();
        source.SetData("CustomPayload", payload, autoConvert: false);
        if (includeText)
        {
            source.SetText("test string", TextDataFormat.Text);
        }

        try
        {
            Clipboard.SetDataObject(source, copy: false);
            IDataObject result = Clipboard.GetDataObject()!;
            result.GetData("CustomPayload", autoConvert: false).Should().BeSameAs(payload);
            Clipboard.GetData("CustomPayload").Should().BeSameAs(payload);
            Clipboard.IsCurrent(source).Should().BeTrue();
            if (includeText)
            {
                AssertClipboardText();
            }
            else
            {
                result.Should().BeSameAs(source);
                Clipboard.ContainsText().Should().BeFalse();
            }
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_TextWithoutAutoConvert_TypedRetrievalUsesClipboardSynthesis()
    {
        // All typed retrieval overloads must read synthesized Unicode text from an ANSI-only source.
        // Neither disabling managed conversion nor supplying a resolver should bypass native synthesis.
        DataObject source = new();
        source.SetText("test string", TextDataFormat.Text);

        try
        {
            Clipboard.SetDataObject(source, copy: false);
            ITypedDataObject result = (ITypedDataObject)Clipboard.GetDataObject()!;
            result.TryGetData(DataFormats.UnicodeText, autoConvert: false, out string? text).Should().BeTrue();
            text.Should().Be("test string");
            result.TryGetData(DataFormats.UnicodeText, out text).Should().BeTrue();
            text.Should().Be("test string");
            result.TryGetData(DataFormats.UnicodeText, static _ => null, autoConvert: false, out text).Should().BeTrue();
            text.Should().Be("test string");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfTheory]
    [InlineData(TextDataFormat.Text)]
    [InlineData(TextDataFormat.UnicodeText)]
    public void SetDataObject_LegacyDataObject_ConvenienceGettersPreserveData(TextDataFormat format)
    {
        // A wrapper must not force legacy-only IDataObject sources onto the typed retrieval path.
        // Verify text synthesis and unrelated convenience getters still work for the same mixed source.
        DataObject source = new();
        source.SetText("test string", format);
        source.SetAudio(new byte[] { 1, 2, 3 });
        source.SetData(DataFormats.FileDrop, new[] { "filePath" }, autoConvert: true);
        source.SetImage(new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, palette: null));

        try
        {
            Clipboard.SetDataObject(new LegacyDataObject(source), copy: false);
            // A legacy-only source must also allow safe typed text reads through the composed clipboard view.
            ITypedDataObject result = (ITypedDataObject)Clipboard.GetDataObject()!;
            result.TryGetData(DataFormats.UnicodeText, autoConvert: false, out string? text).Should().BeTrue();
            text.Should().Be("test string");
            result.TryGetData(DataFormats.UnicodeText, static _ => null, autoConvert: false, out text).Should().BeTrue();
            text.Should().Be("test string");
            Clipboard.GetText().Should().Be("test string");
            Clipboard.GetText(TextDataFormat.Text).Should().Be("test string");
            Clipboard.GetAudioStream().Should().BeOfType<System.IO.MemoryStream>().Which.ToArray().Should().Equal(1, 2, 3);
            var files = Clipboard.GetFileDropList();
            files.Count.Should().Be(1);
            files[0].Should().Be("filePath");
            Clipboard.GetImage()!.PixelWidth.Should().Be(1);
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_LegacyDataObject_PreservesMixedCustomPayload()
    {
        // Falling back to legacy retrieval must keep the composed wrapper, not just its managed source.
        // This preserves custom payload identity while still allowing native text synthesis.
        object payload = new();
        DataObject source = new();
        source.SetText("test string", TextDataFormat.Text);
        source.SetData("CustomPayload", payload, autoConvert: false);

        try
        {
            Clipboard.SetDataObject(new LegacyDataObject(source), copy: false);
            Clipboard.GetDataObject()!.GetData("CustomPayload", autoConvert: false).Should().BeSameAs(payload);
            Clipboard.GetData("CustomPayload").Should().BeSameAs(payload);
            Clipboard.GetText().Should().Be("test string");
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    [WpfFact]
    public void SetDataObject_LegacyDataObject_UsesAutoConvertOverloads()
    {
        // As in .NET 9, clipboard wrappers forward single-argument getters to the autoConvert overloads,
        // even when an application's single-argument getters expose different data.
        object payload = new();
        DataObject source = new();
        source.SetText("test string", TextDataFormat.Text);

        try
        {
            Clipboard.SetDataObject(new LegacyDataObject(source, payload), copy: false);
            IDataObject result = Clipboard.GetDataObject()!;
            result.GetData("SingleArgumentPayload").Should().BeNull();
            result.GetDataPresent("SingleArgumentPayload").Should().BeFalse();
            result.GetData("SingleArgumentPayload", autoConvert: true).Should().BeNull();
            result.GetDataPresent("SingleArgumentPayload", autoConvert: true).Should().BeFalse();
        }
        finally
        {
            Clipboard.Clear();
        }
    }

    private static void AssertClipboardText()
    {
        // autoConvert:false suppresses managed mapping, but must not suppress Windows' synthesized formats.
        IDataObject result = Clipboard.GetDataObject()!;
        foreach (string format in new[] { DataFormats.Text, DataFormats.UnicodeText, DataFormats.OemText })
        {
            result.GetDataPresent(format).Should().BeTrue();
            result.GetDataPresent(format, autoConvert: false).Should().BeTrue();
            result.GetData(format, autoConvert: false).Should().Be("test string");
            Clipboard.ContainsData(format).Should().BeTrue();
            Clipboard.GetData(format).Should().Be("test string");
        }

        Clipboard.ContainsText().Should().BeTrue();
        Clipboard.ContainsText(TextDataFormat.Text).Should().BeTrue();
        Clipboard.ContainsText(TextDataFormat.UnicodeText).Should().BeTrue();
        Clipboard.GetText().Should().Be("test string");
        Clipboard.GetText(TextDataFormat.Text).Should().Be("test string");
        Clipboard.GetText(TextDataFormat.UnicodeText).Should().Be("test string");
    }

    // Expose only the legacy contract, regardless of other interfaces implemented by the delegated object.
    private sealed class LegacyDataObject(DataObject data, object? singleArgumentPayload = null) : IDataObject
    {
        public object? GetData(string format, bool autoConvert) => data.GetData(format, autoConvert);
        public object? GetData(string format) => format == "SingleArgumentPayload" ? singleArgumentPayload : data.GetData(format);
        public object? GetData(Type format) => data.GetData(format);
        public bool GetDataPresent(string format, bool autoConvert) => data.GetDataPresent(format, autoConvert);
        public bool GetDataPresent(string format) =>
            (format == "SingleArgumentPayload" && singleArgumentPayload is not null) || data.GetDataPresent(format);
        public bool GetDataPresent(Type format) => data.GetDataPresent(format);
        public string[] GetFormats(bool autoConvert) => data.GetFormats(autoConvert);
        public string[] GetFormats() => data.GetFormats();
        public void SetData(string format, object? value, bool autoConvert) => data.SetData(format, value, autoConvert);
        public void SetData(string format, object? value) => data.SetData(format, value);
        public void SetData(Type format, object? value) => data.SetData(format, value);
        public void SetData(object? value) => data.SetData(value);
    }
}
