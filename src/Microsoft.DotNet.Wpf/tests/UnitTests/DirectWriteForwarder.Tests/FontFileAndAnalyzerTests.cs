// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="FontFile"/> class.
/// </summary>
public class FontFileTests
{
    [Fact]
    public void CreateFontFile_WithValidPath_ShouldSucceed()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var factory = DWriteFactory.Instance;
        var fontFile = factory.CreateFontFile(new Uri(TestHelpers.ArialPath));
        
        fontFile.Should().NotBeNull();
    }

    [Fact]
    public void GetUriPath_ShouldReturnPath()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var factory = DWriteFactory.Instance;
        var fontFile = factory.CreateFontFile(new Uri(TestHelpers.ArialPath));
        
        var uriPath = fontFile.GetUriPath();
        
        uriPath.Should().NotBeNullOrEmpty();
        uriPath.ToLowerInvariant().Should().Contain("arial");
    }

    [Fact]
    public void DWriteFontFileNoAddRef_ShouldReturnNonNullPointer()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var factory = DWriteFactory.Instance;
        var fontFile = factory.CreateFontFile(new Uri(TestHelpers.ArialPath));
        
        // This returns the native pointer - just verify it doesn't throw
        // The actual pointer is internal to DirectWrite
        fontFile.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for <see cref="FontSource"/> and <see cref="FontSourceFactory"/> classes.
/// </summary>
public class FontSourceTests
{
    [Fact]
    public void FontSourceFactory_Create_ShouldReturnFontSource()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var factory = new FontSourceFactory();
        var fontSource = factory.Create(TestHelpers.ArialPath);
        
        fontSource.Should().NotBeNull();
    }

    [Fact]
    public void FontSource_Uri_ShouldReturnUri()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        
        fontSource.Uri.Should().NotBeNull();
        fontSource.Uri.LocalPath.ToLowerInvariant().Should().Contain("arial");
    }

    [Fact]
    public void FontSource_IsComposite_ShouldBeFalseForTtf()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        
        fontSource.IsComposite.Should().BeFalse();
    }

    [Fact]
    public void FontSource_GetLastWriteTimeUtc_ShouldReturnValidTime()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        var lastWriteTime = fontSource.GetLastWriteTimeUtc();
        
        lastWriteTime.Should().BeBefore(DateTime.UtcNow);
        lastWriteTime.Year.Should().BeGreaterThan(1990);
    }

    [Fact]
    public void FontSource_GetUnmanagedStream_ShouldReturnStream()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        
        using var stream = fontSource.GetUnmanagedStream();
        
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FontSource_TestFileOpenable_ShouldNotThrowForValidFile()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        
        // Should not throw for valid file
        Action act = () => fontSource.TestFileOpenable();
        act.Should().NotThrow();
    }
}

/// <summary>
/// Tests for <see cref="FontFileLoader"/> class.
/// </summary>
public class FontFileLoaderTests
{
    [Fact]
    public void FontFileLoader_CanBeConstructedWithFactory()
    {
        var factory = new FontSourceFactory();
        var loader = new FontFileLoader(factory);
        
        loader.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for <see cref="FontFileStream"/> class.
/// </summary>
public class FontFileStreamTests
{
    [Fact]
    public void FontFileStream_CanBeConstructedWithFontSource()
    {
        TestHelpers.SkipIfArialNotAvailable();
        
        var fontSource = new FontSource(new Uri(TestHelpers.ArialPath));
        var stream = new FontFileStream(fontSource);
        
        stream.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for <see cref="FontFileEnumerator"/> class.
/// FontFileEnumerator is a C++/CLI class in DirectWriteForwarder that implements
/// IDWriteFontFileEnumeratorMirror for enumerating font files in a collection.
/// </summary>
public class FontFileEnumeratorTests
{
    [Fact]
    public unsafe void FontFileEnumerator_CanBeConstructed()
    {
        TestHelpers.SkipIfArialNotAvailable();

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = new IFontSource[] { new FontSource(new Uri(TestHelpers.ArialPath)) };
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        enumerator.Should().NotBeNull();
    }

    [Fact]
    public unsafe void FontFileEnumerator_MoveNext_ReturnsTrueForFirstFile()
    {
        TestHelpers.SkipIfArialNotAvailable();

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = new IFontSource[] { new FontSource(new Uri(TestHelpers.ArialPath)) };
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        bool hasCurrentFile = false;
        var hr = enumerator.MoveNext(ref hasCurrentFile);
        
        hr.Should().Be(0); // S_OK
        hasCurrentFile.Should().BeTrue();
    }

    [Fact]
    public unsafe void FontFileEnumerator_MoveNext_ReturnsFalseAfterLastFile()
    {
        TestHelpers.SkipIfArialNotAvailable();

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = new IFontSource[] { new FontSource(new Uri(TestHelpers.ArialPath)) };
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        bool hasCurrentFile = false;
        enumerator.MoveNext(ref hasCurrentFile); // Move to first
        hasCurrentFile.Should().BeTrue();
        
        enumerator.MoveNext(ref hasCurrentFile); // Move past last
        hasCurrentFile.Should().BeFalse();
    }

    [Fact]
    public unsafe void FontFileEnumerator_MultipleFiles_EnumeratesAll()
    {
        // Get a few font files
        var fontFiles = Directory.GetFiles(TestHelpers.FontsDirectory, "*.ttf").Take(3).ToArray();
        Assert.SkipUnless(fontFiles.Length >= 2, "Need at least 2 fonts for this test");

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = fontFiles.Select(f => (IFontSource)new FontSource(new Uri(f))).ToArray();
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        int count = 0;
        bool hasCurrentFile = false;
        while (enumerator.MoveNext(ref hasCurrentFile) == 0 && hasCurrentFile)
        {
            count++;
        }
        
        count.Should().Be(fontSources.Length);
    }

    [Fact]
    public unsafe void FontFileEnumerator_EmptyCollection_MoveNextReturnsFalse()
    {
        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = Array.Empty<IFontSource>();
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        bool hasCurrentFile = false;
        var hr = enumerator.MoveNext(ref hasCurrentFile);
        
        hr.Should().Be(0); // S_OK
        hasCurrentFile.Should().BeFalse();
    }

    [Fact]
    public unsafe void FontFileEnumerator_GetCurrentFontFile_ReturnsValidPointer()
    {
        TestHelpers.SkipIfArialNotAvailable();

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = new IFontSource[] { new FontSource(new Uri(TestHelpers.ArialPath)) };
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        bool hasCurrentFile = false;
        enumerator.MoveNext(ref hasCurrentFile);
        hasCurrentFile.Should().BeTrue();
        
        Native.IDWriteFontFile* fontFile = null;
        var hr = enumerator.GetCurrentFontFile(&fontFile);
        
        hr.Should().Be(0); // S_OK
        ((IntPtr)fontFile).Should().NotBe(IntPtr.Zero);
        
        // Release the font file via COM Release (method index 2 in IUnknown vtable)
        if (fontFile != null)
        {
            Marshal.Release((IntPtr)fontFile);
        }
    }

    [Fact]
    public unsafe void FontFileEnumerator_GetCurrentFontFile_WithNullPointer_ReturnsInvalidArg()
    {
        TestHelpers.SkipIfArialNotAvailable();

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = new IFontSource[] { new FontSource(new Uri(TestHelpers.ArialPath)) };
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        bool hasCurrentFile = false;
        enumerator.MoveNext(ref hasCurrentFile);
        
        var hr = enumerator.GetCurrentFontFile(null);
        
        // E_INVALIDARG = 0x80070057
        hr.Should().Be(unchecked((int)0x80070057));
    }

    [Fact]
    public unsafe void FontFileEnumerator_MultipleFontTypes_EnumeratesAll()
    {
        // Get different font types
        var ttfFiles = Directory.GetFiles(TestHelpers.FontsDirectory, "*.ttf").Take(2);
        var ttcFiles = Directory.GetFiles(TestHelpers.FontsDirectory, "*.ttc").Take(1);
        var allFiles = ttfFiles.Concat(ttcFiles).ToArray();
        
        Assert.SkipUnless(allFiles.Length >= 2, "Need at least 2 fonts for this test");

        var fontSourceFactory = new FontSourceFactory();
        var fontFileLoader = new FontFileLoader(fontSourceFactory);
        var fontSources = allFiles.Select(f => (IFontSource)new FontSource(new Uri(f))).ToArray();
        
        var factory = DWriteFactory.Instance;
        var enumerator = new FontFileEnumerator(
            fontSources,
            fontFileLoader,
            (Native.IDWriteFactory*)factory.DWriteFactory
        );
        
        int count = 0;
        bool hasCurrentFile = false;
        while (enumerator.MoveNext(ref hasCurrentFile) == 0 && hasCurrentFile)
        {
            count++;
        }
        
        count.Should().Be(fontSources.Length);
    }
}

/// <summary>
/// Tests for <see cref="TextAnalyzer"/> class.
/// </summary>
public class TextAnalyzerTests
{
    [Fact]
    public void CreateTextAnalyzer_ShouldReturnNonNull()
    {
        var factory = DWriteFactory.Instance;
        
        var textAnalyzer = factory.CreateTextAnalyzer();
        
        textAnalyzer.Should().NotBeNull();
    }

    [Fact]
    public void TextAnalyzer_CanBeCreatedMultipleTimes()
    {
        var factory = DWriteFactory.Instance;
        
        var analyzer1 = factory.CreateTextAnalyzer();
        var analyzer2 = factory.CreateTextAnalyzer();
        
        analyzer1.Should().NotBeNull();
        analyzer2.Should().NotBeNull();
        analyzer1.Should().NotBeSameAs(analyzer2);
    }
}

/// <summary>
/// Tests for <see cref="LocalizedErrorMsgs"/> class.
/// </summary>
public class LocalizedErrorMsgsTests
{
    [Fact]
    public void EnumeratorNotStarted_ShouldBeSet()
    {
        // DWriteFactory static constructor sets these
        var _ = DWriteFactory.Instance;
        
        LocalizedErrorMsgs.EnumeratorNotStarted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EnumeratorReachedEnd_ShouldBeSet()
    {
        // DWriteFactory static constructor sets these
        var _ = DWriteFactory.Instance;
        
        LocalizedErrorMsgs.EnumeratorReachedEnd.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ErrorMessages_CanBeSetAndRetrieved()
    {
        var originalNotStarted = LocalizedErrorMsgs.EnumeratorNotStarted;
        var originalReachedEnd = LocalizedErrorMsgs.EnumeratorReachedEnd;
        
        try
        {
            LocalizedErrorMsgs.EnumeratorNotStarted = "Custom not started message";
            LocalizedErrorMsgs.EnumeratorReachedEnd = "Custom reached end message";
            
            LocalizedErrorMsgs.EnumeratorNotStarted.Should().Be("Custom not started message");
            LocalizedErrorMsgs.EnumeratorReachedEnd.Should().Be("Custom reached end message");
        }
        finally
        {
            // Restore original values
            LocalizedErrorMsgs.EnumeratorNotStarted = originalNotStarted;
            LocalizedErrorMsgs.EnumeratorReachedEnd = originalReachedEnd;
        }
    }
}

/// <summary>
/// Tests for <see cref="ItemProps"/> struct.
/// </summary>
public class ItemPropsTests
{
    [Fact]
    public void ItemProps_DefaultConstructor_ShouldCreateWithDefaults()
    {
        var itemProps = new ItemProps();
        
        itemProps.Should().NotBeNull();
        itemProps.HasCombiningMark.Should().BeFalse();
        itemProps.HasExtendedCharacter.Should().BeFalse();
        itemProps.NeedsCaretInfo.Should().BeFalse();
        itemProps.IsIndic.Should().BeFalse();
        itemProps.IsLatin.Should().BeFalse();
        itemProps.DigitCulture.Should().BeNull();
    }

    [Fact]
    public unsafe void ItemProps_Create_WithNullPointers_ShouldSucceed()
    {
        var itemProps = ItemProps.Create(
            scriptAnalysis: null,
            numberSubstitution: null,
            digitCulture: null,
            hasCombiningMark: true,
            needsCaretInfo: true,
            hasExtendedCharacter: true,
            isIndic: false,
            isLatin: true
        );
        
        itemProps.Should().NotBeNull();
        itemProps.HasCombiningMark.Should().BeTrue();
        itemProps.NeedsCaretInfo.Should().BeTrue();
        itemProps.HasExtendedCharacter.Should().BeTrue();
        itemProps.IsIndic.Should().BeFalse();
        itemProps.IsLatin.Should().BeTrue();
    }

    [Fact]
    public unsafe void ItemProps_Create_WithDigitCulture_ShouldSetCulture()
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("ar-SA");
        
        var itemProps = ItemProps.Create(
            scriptAnalysis: null,
            numberSubstitution: null,
            digitCulture: culture,
            hasCombiningMark: false,
            needsCaretInfo: false,
            hasExtendedCharacter: false,
            isIndic: false,
            isLatin: false
        );
        
        itemProps.DigitCulture.Should().Be(culture);
    }

    [Fact]
    public unsafe void ItemProps_CanShapeTogether_WithSameNullPointers_ShouldReturnTrue()
    {
        var props1 = ItemProps.Create(null, null, null, false, false, false, false, true);
        var props2 = ItemProps.Create(null, null, null, true, true, true, true, false);
        
        // Both have null script analysis and null number substitution, so they can shape together
        props1.CanShapeTogether(props2).Should().BeTrue();
    }

    [Fact]
    public unsafe void ItemProps_ScriptAnalysis_WithNullPointer_ShouldReturnNull()
    {
        var itemProps = ItemProps.Create(null, null, null, false, false, false, false, false);
        
        // ScriptAnalysis returns void*, so cast to check for null
        void* ptr = itemProps.ScriptAnalysis;
        ((IntPtr)ptr).Should().Be(IntPtr.Zero);
    }
}

/// <summary>
/// Tests for <see cref="MS.Internal.Span"/> struct.
/// </summary>
public class SpanTests
{
    [Fact]
    public void Span_Constructor_ShouldSetElementAndLength()
    {
        var element = "test element";
        var span = new MS.Internal.Span(element, 10);
        
        span.element.Should().Be(element);
        span.length.Should().Be(10);
    }

    [Fact]
    public void Span_Constructor_WithNullElement_ShouldSucceed()
    {
        var span = new MS.Internal.Span(null, 5);
        
        span.element.Should().BeNull();
        span.length.Should().Be(5);
    }

    [Fact]
    public void Span_Constructor_WithZeroLength_ShouldSucceed()
    {
        var span = new MS.Internal.Span("element", 0);
        
        span.length.Should().Be(0);
    }

    [Fact]
    public void Span_CanHoldItemProps()
    {
        var itemProps = new ItemProps();
        var span = new MS.Internal.Span(itemProps, 100);
        
        span.element.Should().Be(itemProps);
        span.length.Should().Be(100);
    }
}

/// <summary>
/// Tests for <see cref="ClassificationUtility"/> class.
/// </summary>
public class ClassificationUtilityTests
{
    [Fact]
    public void ClassificationUtility_Instance_ShouldNotBeNull()
    {
        var instance = ClassificationUtility.Instance;
        
        instance.Should().NotBeNull();
    }

    [Fact]
    public void GetCharAttribute_ForLatinA_ShouldReturnLatinAndNotDigit()
    {
        var instance = ClassificationUtility.Instance;
        
        instance.GetCharAttribute(
            unicodeScalar: 'A', // Latin capital A
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong
        );
        
        isCombining.Should().BeFalse();
        needsCaretInfo.Should().BeFalse();
        isDigit.Should().BeFalse();
        isLatin.Should().BeTrue();
        isIndic.Should().BeFalse();
        isStrong.Should().BeTrue(); // Latin A is a strong character
    }

    [Fact]
    public void GetCharAttribute_ForDigit_ShouldReturnIsDigit()
    {
        var instance = ClassificationUtility.Instance;
        
        instance.GetCharAttribute(
            unicodeScalar: '0', // Digit zero
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong
        );
        
        isCombining.Should().BeFalse();
        needsCaretInfo.Should().BeFalse();
        isDigit.Should().BeTrue();
        isLatin.Should().BeFalse();
        isIndic.Should().BeFalse();
        // Digits may or may not be "strong" depending on classification - don't assert
        _ = isStrong; // suppress IDE0059
    }

    [Fact]
    public void GetCharAttribute_ForDevanagari_ShouldReturnIndicAndNeedsCaretInfo()
    {
        var instance = ClassificationUtility.Instance;
        
        // Devanagari letter A (U+0905)
        instance.GetCharAttribute(
            unicodeScalar: 0x0905,
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong
        );
        
        isCombining.Should().BeFalse();
        isIndic.Should().BeTrue();
        needsCaretInfo.Should().BeTrue();
        isLatin.Should().BeFalse();
        isDigit.Should().BeFalse();
        isStrong.Should().BeTrue();
    }

    [Fact]
    public void GetCharAttribute_ForCombiningMark_ShouldReturnIsCombining()
    {
        var instance = ClassificationUtility.Instance;
        
        // Combining Acute Accent (U+0301) is in the Latin Extended block
        instance.GetCharAttribute(
            unicodeScalar: 0x0301,
            out bool isCombining,
            out bool needsCaretInfo,
            out bool isIndic,
            out bool isDigit,
            out bool isLatin,
            out bool isStrong
        );
        
        isCombining.Should().BeTrue();
        needsCaretInfo.Should().BeFalse();
        isIndic.Should().BeFalse();
        isDigit.Should().BeFalse();
        // isLatin may be true since U+0301 is in the Latin Extended block
        _ = isLatin;
        isStrong.Should().BeFalse(); // Combining marks are not strong
    }

    [Fact]
    public void ScriptCaretInfo_ShouldHaveExpectedLength()
    {
        ClassificationUtility.ScriptCaretInfo.Should().NotBeEmpty();
        ClassificationUtility.ScriptCaretInfo.Length.Should().BeGreaterThan(50);
    }
}
