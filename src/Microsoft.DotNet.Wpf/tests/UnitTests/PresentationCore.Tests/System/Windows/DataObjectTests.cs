// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;
using System.Formats.Nrbf;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Windows;

// Note: the OS Clipboard is a system wide resource and all access should be done sequentially to avoid
// collisions with other tests. We also retry as we cannot control other processes that may be using the clipboard.
[Collection("Sequential")]
[UISettings(MaxAttempts = 3)]
public class DataObjectTests
{
    private const int SOk = 0;
    private const int SFalse = 1;
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int DV_E_TYMED = unchecked((int)0x80040069);

    [WpfFact]
    public void GetData_NonExistentFormat_ReturnsNull()
    {
        DataObject data = new();
        data.GetData("non-existent format").Should().BeNull();
    }

    [WpfFact]
    public void GetData_NonExistentKey_ReturnsNull()
    {
        Type key = typeof(string);

        DataObject data = new();

        data.GetData(key.FullName!).Should().BeNull();
    }

    [WpfFact]
    public void ContainsData_ContainsText_ReturnsFalse()
    {
        string testData = "test data";
        DataObject data = new();
        data.SetData(testData);
        data.ContainsText().Should().BeFalse();
    }

    #region SetData Tests

    [WpfFact]
    public void SetData_Invoke_GetReturnsExpected()
    {
        string testData = "test data";
        DataObject data = new();
        data.SetData(testData);
        data.GetData(testData.GetType().FullName!).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_Null_ThrowsArgumentNullException()
    {
        DataObject data = new();
        Action action = () => data.SetData(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [WpfFact]
    public void SetData_WithTextFormat_CanRetrieve()
    {
        DataObject data = new();
        data.SetData(DataFormats.Text, "Hello World");
        data.GetData(DataFormats.Text).Should().Be("Hello World");
    }

    [WpfFact]
    public void SetData_WithCustomFormat_CanRetrieve()
    {
        DataObject data = new();
        const string customFormat = "CustomFormat";
        int testValue = 42;
        data.SetData(customFormat, testValue);
        data.GetData(customFormat).Should().Be(testValue);
    }

    [WpfFact]
    public void SetData_NullFormat_ThrowsArgumentNullException()
    {
        DataObject data = new();
        Action action = () => data.SetData((string)null!, "Some Data");
        action.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "format");
    }

    [WpfFact]
    public void SetData_MultipleTypes_GetReturnsExpected()
    {
        int intData = 20;
        string stringData = "test string";

        DataObject data = new();

        data.SetData(intData);
        data.SetData(stringData);
        data.GetData(intData.GetType().FullName!).Should().Be(intData);
        data.GetData(stringData.GetType().FullName!).Should().Be(stringData);
    }

    [WpfFact]
    public void SetData_SameTypeTwice_OverwritesData()
    {
        string data1 = "data 1";
        string data2 = "data 2";

        DataObject data = new();

        data.SetData(data1);
        data.SetData(data2);
        data.GetData(data1.GetType().FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObject_Invoke_GetReturnsExpected()
    {
        string key = "key";
        string testData = "test data";

        DataObject data = new();

        data.SetData(key, testData);
        data.GetData(key).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_StringObject_NullData_ThrowsArgumentNullException()
    {
        string key = "key";
        string? testData = null;

        DataObject data = new();

        Action act = () => data.SetData(key, testData);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObject_EmptyStringKey_ThrowsArgumentException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(string.Empty, testData);
        act.Should().Throw<ArgumentException>();
    }

    [WpfFact]
    public void SetData_StringObject_SameKeyTwice_OverwritesData()
    {
        string key = "key";
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key, data1);
        data.SetData(key, data2);

        data.GetData(key).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObject_DifferentKeys_DataIsStoredSeparately()
    {
        string key1 = "key1";
        string key2 = "key2";
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key1, data1);
        data.SetData(key2, data2);
        data.GetData(key1).Should().Be(data1);
        data.GetData(key2).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_TypeObject_Invoke_GetReturnsExpected()
    {
        Type stringKey = typeof(string);
        Type intKey = typeof(int);
        Type boolKey = typeof(bool);
        string stringTestData = "string test data";
        string intTestData = "int test data";
        string boolTestData = "bool test data";

        DataObject data = new();

        data.SetData(stringKey, stringTestData);
        data.SetData(intKey, intTestData);
        data.SetData(boolKey, boolTestData);

        data.GetData(stringKey.FullName!).Should().Be(stringTestData);
        data.GetData(intKey.FullName!).Should().Be(intTestData);
        data.GetData(boolKey.FullName!).Should().Be(boolTestData);
    }

    [WpfFact]
    public void SetData_TypeObject_NullTypeKey_ThrowsArgumentNullException()
    {
        Type? key = null;
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(key!, testData);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_TypeObject_NullData_ThrowsArgumentNullException()
    {
        Type key = typeof(string);

        DataObject data = new();

        Action act = () => data.SetData(key, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_TypeObject_SameKeyTwice_OverwritesData()
    {
        Type key = typeof(string);
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key, data1);
        data.SetData(key, data2);

        data.GetData(key.FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_TypeObject_DifferentKeys_DataIsStoredSeparately()
    {
        Type key1 = typeof(string);
        Type key2 = typeof(int);
        string data1 = "data1";
        string data2 = "data2";

        DataObject data = new();

        data.SetData(key1, data1);
        data.SetData(key2, data2);
        data.GetData(key1.FullName!).Should().Be(data1);
        data.GetData(key2.FullName!).Should().Be(data2);
    }

    [WpfFact]
    public void SetData_StringObjectBool_Invoke_GetReturnsExpected()
    {
        string key = "key";
        string testData = "test data";

        DataObject data = new();

        data.SetData(key, testData, true);
        data.GetData(key).Should().Be(testData);
    }

    [WpfFact]
    public void SetData_StringObjectBool_NullData_ThrowsArgumentNullException()
    {
        string key = "key";

        DataObject data = new();

        Action act = () => data.SetData(key, null, true);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObjectBool_NullKey_ThrowsArgumentNullException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData((string)null!, testData, true);
        act.Should().Throw<ArgumentNullException>();
    }

    [WpfFact]
    public void SetData_StringObjectBool_EmptyKey_ThrowsArgumentException()
    {
        string testData = "test data";

        DataObject data = new();

        Action act = () => data.SetData(string.Empty, testData, true);
        act.Should().Throw<ArgumentException>();
    }

    // Verifies managed EMF mappings honor autoConvert while preserving the original Metafile instance.
    [WpfTheory]
    [InlineData("EnhancedMetafile", "System.Drawing.Imaging.Metafile")]
    [InlineData("System.Drawing.Imaging.Metafile", "EnhancedMetafile")]
    public void TryGetData_MetafileMappedFormat_RespectsAutoConvert(
        string sourceFormat,
        string mappedFormat)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(sourceFormat, source, autoConvert: true);

        dataObject.GetDataPresent(sourceFormat, autoConvert: false).Should().BeTrue();
        dataObject.GetDataPresent(mappedFormat, autoConvert: false).Should().BeFalse();
        dataObject.GetDataPresent(mappedFormat, autoConvert: true).Should().BeTrue();

        string[] nativeFormats = dataObject.GetFormats(autoConvert: false);
        nativeFormats.Should().Contain(sourceFormat);
        nativeFormats.Should().NotContain(mappedFormat);

        string[] convertedFormats = dataObject.GetFormats(autoConvert: true);
        convertedFormats.Should().Contain(sourceFormat);
        convertedFormats.Should().Contain(mappedFormat);

        dataObject.TryGetData(
            sourceFormat,
            autoConvert: false,
            out Metafile? exactResult).Should().BeTrue();
        exactResult.Should().BeSameAs(source);
        EmfTestData.AssertValid(exactResult!);

        dataObject.TryGetData(
            mappedFormat,
            autoConvert: false,
            out Metafile? nonConvertedResult).Should().BeFalse();
        nonConvertedResult.Should().BeNull();

        dataObject.TryGetData(
            mappedFormat,
            autoConvert: true,
            out Metafile? convertedResult).Should().BeTrue();
        convertedResult.Should().BeSameAs(source);
        EmfTestData.AssertValid(convertedResult!);

        dataObject.GetData(sourceFormat, autoConvert: false).Should().BeSameAs(source);
        dataObject.GetData(mappedFormat, autoConvert: false).Should().BeNull();
        dataObject.GetData(mappedFormat, autoConvert: true).Should().BeSameAs(source);
        EmfTestData.AssertValid(source);
    }

    // Verifies typed requests reject an incompatible result type without corrupting the stored Metafile.
    [WpfFact]
    public void TryGetData_Metafile_WrongTypedRequestThrowsNotSupportedException()
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.EnhancedMetafile, source);

        Action action = () => dataObject.TryGetData(
            DataFormats.EnhancedMetafile,
            autoConvert: false,
            out string? _);

        action.Should().Throw<NotSupportedException>();
        EmfTestData.AssertValid(source);
    }

    // WPF's native-medium callback recognizes only the exact canonical name, as in .NET 9.
    [WpfTheory]
    [InlineData("EnhancedMetafile", false, true, false, ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("EnhancedMetafile", true, true, true, ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("enhancedmetafile", false, true, false, ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("ENHANCEDMETAFILE", false, true, false, ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("System.Drawing.Imaging.Metafile", false, false, true, ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("System.Drawing.Imaging.Metafile", true, true, true, ComTypes.TYMED.TYMED_ENHMF)]
    public void EnumFormatEtc_Metafile_SeparatesNativeAndAliasMedia(
        string sourceFormat,
        bool autoConvert,
        bool nativeAdvertised,
        bool aliasAdvertised,
        ComTypes.TYMED nativeMedium)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(sourceFormat, source, autoConvert);

        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        List<ComTypes.FORMATETC> formats = EnumerateComFormats(comDataObject);
        formats.Should().HaveCount((nativeAdvertised ? 1 : 0) + (aliasAdvertised ? 1 : 0));

        AssertFormat(DataFormats.EnhancedMetafile, nativeAdvertised, nativeMedium);
        AssertFormat(typeof(Metafile).FullName!, aliasAdvertised, ComTypes.TYMED.TYMED_HGLOBAL);
        EmfTestData.AssertValid(source);

        void AssertFormat(string format, bool advertised, ComTypes.TYMED medium)
        {
            short id = unchecked((short)DataFormats.GetDataFormat(format).Id);
            ComTypes.FORMATETC[] matches = formats.Where(entry => entry.cfFormat == id).ToArray();
            if (!advertised)
            {
                matches.Should().BeEmpty();
                return;
            }

            matches.Should().ContainSingle();
            ComTypes.FORMATETC entry = matches[0];
            entry.tymed.Should().Be(medium);
            entry.dwAspect.Should().Be(ComTypes.DVASPECT.DVASPECT_CONTENT);
            entry.lindex.Should().Be(-1);
            entry.ptd.Should().Be(0);
            // QueryGetData confirms the advertised medium, not whether its payload can be rendered.
            comDataObject.QueryGetData(ref entry).Should().Be(SOk);
        }
    }

    [WpfTheory]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("enhancedmetafile", ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("ENHANCEDMETAFILE", ComTypes.TYMED.TYMED_ENHMF)]
    public void GetData_EnumeratedEnhancedMetafile_ReturnsOwnedNativeMedium(
        string requestedFormat,
        ComTypes.TYMED requestedMedium)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        // COM requests use case-insensitive numeric format IDs. Store the canonical key: rendering resolves
        // that ID back to EnhancedMetafile, and case-insensitive managed-source lookup is not promised.
        dataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC advertised = EnumerateComFormats(comDataObject).Single();
        advertised.tymed.Should().Be(ComTypes.TYMED.TYMED_ENHMF);
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, requestedMedium);
        format.cfFormat.Should().Be(advertised.cfFormat);
        comDataObject.QueryGetData(ref format).Should().Be(SOk);

        ComTypes.STGMEDIUM medium = default;
        try
        {
            comDataObject.GetData(ref format, out medium);
            medium.tymed.Should().Be(ComTypes.TYMED.TYMED_ENHMF);
            medium.unionmember.Should().NotBe(0);
            using Metafile result = new(medium.unionmember, deleteEmf: false);
            EmfTestData.AssertValid(result);
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        // Releasing the transferred handle must not consume the caller's source.
        EmfTestData.AssertValid(source);
    }

    [WpfTheory]
    [InlineData("System.Drawing.Imaging.Metafile", ComTypes.TYMED.TYMED_ENHMF)]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_MFPICT)]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_ISTORAGE)]
    public void GetData_MetafileUnsupportedMedium_Fails(
        string requestedFormat,
        ComTypes.TYMED requestedMedium)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: true);
        AssertRuntimeGetDataFailure(
            (ComTypes.IDataObject)dataObject,
            requestedFormat,
            requestedMedium,
            DV_E_TYMED,
            queryResult: DV_E_TYMED);
        EmfTestData.AssertValid(source);
    }

    [WpfFact]
    public void GetData_EnhancedMetafile_GdiTakesPrecedenceOverEnhMf()
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: false);

        AssertRuntimeGetDataFailure(
            (ComTypes.IDataObject)dataObject,
            DataFormats.EnhancedMetafile,
            ComTypes.TYMED.TYMED_GDI | ComTypes.TYMED.TYMED_ENHMF,
            DV_E_TYMED,
            queryResult: SOk);
        EmfTestData.AssertValid(source);
    }

    // Request exactly the enumerated ENHMF medium: failure must not return a serialized HGLOBAL fallback.
    [WpfTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetData_EnumeratedEnhancedMetafile_InvalidPayloadFailsWithoutSerializedFallback(int payloadKind)
    {
        using MemoryStream stream = new(payloadKind == 1 ? new byte[] { 1, 2, 3, 4 } : Array.Empty<byte>());
        object payload = payloadKind == 2 ? "not a metafile" : stream;
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.EnhancedMetafile, payload, autoConvert: false);
        ComTypes.IDataObject runtimeDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC advertised = EnumerateComFormats(runtimeDataObject).Single();
        advertised.tymed.Should().Be(ComTypes.TYMED.TYMED_ENHMF);
        runtimeDataObject.QueryGetData(ref advertised).Should().Be(SOk);

        int expected = payloadKind == 2 ? DV_E_TYMED : E_FAIL;

        AssertRuntimeGetDataFailure(
            runtimeDataObject,
            DataFormats.EnhancedMetafile,
            ComTypes.TYMED.TYMED_ENHMF,
            expected,
            queryResult: SOk);
        stream.CanRead.Should().BeTrue();
    }

    [WpfTheory]
    [InlineData("EnhancedMetafile")]
    [InlineData("enhancedmetafile")]
    [InlineData("System.Drawing.Imaging.Metafile")]
    public void GetDataHere_Metafile_DoesNotAcceptCallerProvidedEnhMfMedium(string requestedFormat)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(DataFormats.EnhancedMetafile, source, autoConvert: true);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, ComTypes.TYMED.TYMED_ENHMF);
        ComTypes.STGMEDIUM medium = new() { tymed = ComTypes.TYMED.TYMED_ENHMF };
        try
        {
            Action getDataHere = () => comDataObject.GetDataHere(ref format, ref medium);
            getDataHere.Should().Throw<COMException>().Which.HResult.Should().Be(DV_E_TYMED);
            medium.unionmember.Should().Be(0);
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        EmfTestData.AssertValid(source);
    }

    [WpfTheory]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_ENHMF | ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("System.Drawing.Imaging.Metafile", ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("Wpf.Metafile.Custom", ComTypes.TYMED.TYMED_HGLOBAL)]
    public void GetData_MetafileHGlobal_RejectsWithoutNativeOrSerializedFallback(
        string requestedFormat,
        ComTypes.TYMED requestedMedium)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(requestedFormat, source, autoConvert: false);

        // QueryGetData remains a capability check; it does not promise serialization will succeed.
        AssertRuntimeGetDataFailure(
            (ComTypes.IDataObject)dataObject,
            requestedFormat,
            requestedMedium,
            DV_E_TYMED,
            queryResult: SOk);
        EmfTestData.AssertValid(source);
    }

    [WpfTheory]
    [InlineData(false, ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData(true, ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData(false, ComTypes.TYMED.TYMED_ENHMF | ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData(true, ComTypes.TYMED.TYMED_ENHMF | ComTypes.TYMED.TYMED_HGLOBAL)]
    public void GetData_EnhancedMetafileNonStreamPayload_RejectsHGlobal(
        bool useSerializableObject,
        ComTypes.TYMED requestedMedium)
    {
        DataObject dataObject = new();
        object payload = useSerializableObject ? 42 : "not a metafile";
        dataObject.SetData(DataFormats.EnhancedMetafile, payload, autoConvert: false);

        AssertRuntimeGetDataFailure(
            (ComTypes.IDataObject)dataObject,
            DataFormats.EnhancedMetafile,
            requestedMedium,
            DV_E_TYMED,
            queryResult: SOk);
        dataObject.GetData(DataFormats.EnhancedMetafile, autoConvert: false).Should().BeSameAs(payload);
    }

    [WpfTheory]
    [InlineData("EnhancedMetafile")]
    [InlineData("System.Drawing.Imaging.Metafile")]
    [InlineData("Wpf.Metafile.Custom")]
    public void GetDataHere_MetafileHGlobal_RejectionPreservesCallerStorage(string requestedFormat)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(requestedFormat, source, autoConvert: false);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, ComTypes.TYMED.TYMED_HGLOBAL);
        byte[] sentinel = Enumerable.Repeat((byte)0xA5, 256).ToArray();
        nint callerStorage = GlobalAlloc(0x42, (nuint)sentinel.Length);
        callerStorage.Should().NotBe(0);
        ComTypes.STGMEDIUM medium = new()
        {
            tymed = ComTypes.TYMED.TYMED_HGLOBAL,
            unionmember = callerStorage
        };
        try
        {
            nint pointer = GlobalLock(callerStorage);
            pointer.Should().NotBe(0);
            try
            {
                Marshal.Copy(sentinel, 0, pointer, sentinel.Length);
            }
            finally
            {
                GlobalUnlock(callerStorage);
            }

            Action getDataHere = () => comDataObject.GetDataHere(ref format, ref medium);
            getDataHere.Should().Throw<COMException>().Which.HResult.Should().Be(DV_E_TYMED);
            medium.tymed.Should().Be(ComTypes.TYMED.TYMED_HGLOBAL);
            medium.unionmember.Should().Be(callerStorage);
            ReadHGlobal(callerStorage, sentinel.Length).Should().Equal(sentinel);
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        EmfTestData.AssertValid(source);
    }

    [WpfTheory]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("EnhancedMetafile", ComTypes.TYMED.TYMED_ENHMF | ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("System.Drawing.Imaging.Metafile", ComTypes.TYMED.TYMED_HGLOBAL)]
    [InlineData("Wpf.Metafile.Custom", ComTypes.TYMED.TYMED_HGLOBAL)]
    public void GetData_StreamHGlobal_BypassesRejectionAndCopiesWholeStream(
        string requestedFormat,
        ComTypes.TYMED requestedMedium)
    {
        byte[] bytes = [1, 2, 3, 4, 5, 6, 7];
        using MemoryStream source = EmfTestData.CreateNonExposableStream(bytes);
        source.Position = 3;
        DataObject dataObject = new();
        dataObject.SetData(requestedFormat, source, autoConvert: false);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, requestedMedium);
        ComTypes.STGMEDIUM medium = default;
        try
        {
            comDataObject.QueryGetData(ref format).Should().Be(SOk);
            comDataObject.GetData(ref format, out medium);
            medium.tymed.Should().Be(ComTypes.TYMED.TYMED_HGLOBAL);
            ReadHGlobal(medium.unionmember, bytes.Length).Should().Equal(bytes);
            // The legacy raw stream path rewinds and consumes the stream, rather than copying its suffix.
            source.Position.Should().Be(source.Length);
            source.CanRead.Should().BeTrue();
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }
    }

    [WpfTheory]
    [InlineData("Text", false)]
    [InlineData("UnicodeText", true)]
    public void GetData_MetafileTextHGlobal_UsesToStringBeforeRejection(string requestedFormat, bool unicode)
    {
        using Metafile source = EmfTestData.CreateMetafile();
        DataObject dataObject = new();
        dataObject.SetData(requestedFormat, source, autoConvert: false);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, ComTypes.TYMED.TYMED_HGLOBAL);
        ComTypes.STGMEDIUM medium = default;
        try
        {
            comDataObject.GetData(ref format, out medium);
            medium.tymed.Should().Be(ComTypes.TYMED.TYMED_HGLOBAL);
            nint pointer = GlobalLock(medium.unionmember);
            pointer.Should().NotBe(0);
            try
            {
                string? text = unicode ? Marshal.PtrToStringUni(pointer) : Marshal.PtrToStringAnsi(pointer);
                text.Should().Be(source.ToString());
            }
            finally
            {
                GlobalUnlock(medium.unionmember);
            }
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }

        EmfTestData.AssertValid(source);
    }

    [WpfTheory]
    [InlineData("Wpf.HGlobal.Integer")]
    [InlineData("System.Int32")]
    public void GetData_OrdinaryObjectHGlobal_StillSerializes(string requestedFormat)
    {
        DataObject dataObject = new();
        dataObject.SetData(requestedFormat, 42, autoConvert: false);
        ComTypes.IDataObject comDataObject = (ComTypes.IDataObject)dataObject;
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, ComTypes.TYMED.TYMED_HGLOBAL);
        ComTypes.STGMEDIUM medium = default;
        try
        {
            comDataObject.GetData(ref format, out medium);
            medium.tymed.Should().Be(ComTypes.TYMED.TYMED_HGLOBAL);
            byte[] bytes = ReadHGlobal(medium.unionmember, checked((int)GlobalSize(medium.unionmember)));
            byte[] prefix = new Guid("FD9EA796-3B13-4370-A679-56106BB288FB").ToByteArray();
            bytes.Take(prefix.Length).Should().Equal(prefix);
            using MemoryStream serialized = new(bytes, prefix.Length, bytes.Length - prefix.Length, writable: false);
            NrbfDecoder.Decode(serialized).Should().BeAssignableTo<PrimitiveTypeRecord<int>>().Which.Value.Should().Be(42);
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }
    }

    private static byte[] ReadHGlobal(nint handle, int count)
    {
        nint pointer = GlobalLock(handle);
        pointer.Should().NotBe(0);
        try
        {
            byte[] bytes = new byte[count];
            Marshal.Copy(pointer, bytes, 0, count);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static List<ComTypes.FORMATETC> EnumerateComFormats(ComTypes.IDataObject dataObject)
    {
        ComTypes.IEnumFORMATETC enumerator = dataObject.EnumFormatEtc(ComTypes.DATADIR.DATADIR_GET);
        List<ComTypes.FORMATETC> formats = [];
        ComTypes.FORMATETC[] entry = new ComTypes.FORMATETC[1];
        int[] fetched = new int[1];
        int result;
        while ((result = enumerator.Next(1, entry, fetched)) == SOk)
        {
            fetched[0].Should().Be(1);
            formats.Add(entry[0]);
        }

        result.Should().Be(SFalse);
        fetched[0].Should().Be(0);
        return formats;
    }

    private static ComTypes.FORMATETC CreateComFormat(string format, ComTypes.TYMED medium) => new()
    {
        cfFormat = unchecked((short)DataFormats.GetDataFormat(format).Id),
        dwAspect = ComTypes.DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = medium
    };

    private static void AssertRuntimeGetDataFailure(
        ComTypes.IDataObject dataObject,
        string requestedFormat,
        ComTypes.TYMED requestedMedium,
        int expected,
        int queryResult)
    {
        ComTypes.FORMATETC format = CreateComFormat(requestedFormat, requestedMedium);
        dataObject.QueryGetData(ref format).Should().Be(queryResult);
        ComTypes.STGMEDIUM medium = default;
        try
        {
            Action getData = () => dataObject.GetData(ref format, out medium);
            getData.Should().Throw<COMException>().Which.HResult.Should().Be(expected);
            medium.unionmember.Should().Be(0);
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }
    }

    [DllImport("ole32.dll")]
    private static extern void ReleaseStgMedium(ref ComTypes.STGMEDIUM medium);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalLock(nint handle);

    [DllImport("kernel32.dll")]
    private static extern nuint GlobalSize(nint handle);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(nint handle);
    #endregion

    #region ContainsText Tests

    [WpfFact]
    public void ContainsText_WithEmptyDataObject_ShouldReturnFalse()
    {
        var dataObject = new DataObject();

        dataObject.ContainsText().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsText_WithTextData_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample text");

        dataObject.ContainsText().Should().BeTrue();
    }

    [WpfFact]
    public void ContainsText_WithEmptyTextData_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        dataObject.SetText(string.Empty);

        dataObject.ContainsText().Should().BeTrue();
    }

    [WpfFact]
    public void ContainsText_WithNonTextData_ShouldReturnFalse()
    {
        var dataObject = new DataObject();
        dataObject.SetData("CustomFormat", new byte[] { 1, 2, 3 });

        dataObject.ContainsText().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsText_WithTextAndNonTextData_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample text");
        dataObject.SetData("CustomFormat", new byte[] { 1, 2, 3 });

        dataObject.ContainsText().Should().BeTrue();
    }

    [WpfFact]
    public void ContainsText_WithTextFormat_WhenUsingTextFormatParam_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample text", TextDataFormat.Text);

        dataObject.ContainsText(TextDataFormat.Text).Should().BeTrue();

        // Additional case verifying any other data format should return false
        dataObject.ContainsText(TextDataFormat.UnicodeText).Should().BeFalse();
    }

    [WpfFact]
    public void ContainsText_WithUnicodeTextFormat_WhenUsingTextFormatParam_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample text", TextDataFormat.UnicodeText);

        dataObject.ContainsText(TextDataFormat.UnicodeText).Should().BeTrue();
        dataObject.ContainsText(TextDataFormat.Text).Should().BeFalse();
    }

    [WpfFact]
    public void ContainsText_WithMultipleTextFormats_WhenUsingTextFormatParam_ShouldReturnTrueForEach()
    {
        var dataObject = new DataObject();
        var sampleText = "Sample text";
        dataObject.SetText(sampleText, TextDataFormat.Text);
        dataObject.SetText(sampleText, TextDataFormat.UnicodeText);
        dataObject.SetText(sampleText, TextDataFormat.Rtf);

        dataObject.ContainsText(TextDataFormat.Text).Should().BeTrue();
        dataObject.ContainsText(TextDataFormat.UnicodeText).Should().BeTrue();
        dataObject.ContainsText(TextDataFormat.Rtf).Should().BeTrue();

        // Additional check
        dataObject.ContainsText(TextDataFormat.Html).Should().BeFalse();
    }
    #endregion

    #region ContainsAudio Tests

    [WpfFact]
    public void ContainsAudio_WithEmptyDataObject_ShouldReturnFalse()
    {
        var dataObject = new DataObject();

        dataObject.ContainsAudio().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsAudio_WithWaveFormat_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        using MemoryStream audioStream = new([0x52, 0x49, 0x46, 0x46]); // RIFF header 
        dataObject.SetData(DataFormats.WaveAudio, audioStream);

        dataObject.ContainsAudio().Should().BeTrue();
    }

    [WpfFact]
    public void ContainsAudio_WithTextData_ShouldReturnFalse()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample Text Data");

        dataObject.ContainsAudio().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsAudio_WithMultipleFormatsMixedWithAudio_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        using MemoryStream audioStream = new([0x52, 0x49, 0x46, 0x46]); // RIFF header

        dataObject.SetText("Sample text");
        dataObject.SetData(DataFormats.WaveAudio, audioStream);
        dataObject.SetData("CustomFormat", new byte[] { 1, 2, 3 });

        dataObject.ContainsAudio().Should().BeTrue();
    }
    #endregion

    #region ContainsImage Tests

    [WpfFact]
    public void ContainsImage_WithEmptyDataObject_ShouldReturnFalse()
    {
        var dataObject = new DataObject();

        dataObject.ContainsImage().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsImage_WithBitmapData_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        var bitmap = new BitmapImage();

        dataObject.SetData(DataFormats.Bitmap, bitmap);

        dataObject.ContainsImage().Should().BeTrue();
    }

    [WpfFact]
    public void ContainsImage_WithTextData_ShouldReturnFalse()
    {
        var dataObject = new DataObject();
        dataObject.SetText("Sample Text Data");

        dataObject.ContainsImage().Should().BeFalse();
    }

    [WpfFact]
    public void ContainsImage_WithMultipleFormatsMixedWithImage_ShouldReturnTrue()
    {
        var dataObject = new DataObject();
        var bitmap = new BitmapImage();

        dataObject.SetText("Sample text");
        dataObject.SetData(DataFormats.Bitmap, bitmap);
        dataObject.SetData("CustomFormat", new byte[] { 1, 2, 3 });

        dataObject.ContainsImage().Should().BeTrue();
    }
    #endregion
}
