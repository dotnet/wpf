// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="Factory"/> class.
/// </summary>
public class FactoryTests
{
    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        var factory = DWriteFactory.Instance;
        factory.Should().NotBeNull();
    }

    [Fact]
    public void GetSystemFontCollection_ShouldReturnNonEmptyCollection()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        fontCollection.Should().NotBeNull();
        fontCollection.FamilyCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSystemFontCollection_CalledMultipleTimes_ReturnsSameInstance()
    {
        var collection1 = DWriteFactory.SystemFontCollection;
        var collection2 = DWriteFactory.SystemFontCollection;

        // Should be cached
        collection1.Should().BeSameAs(collection2);
    }

    [Fact]
    public void IsLocalUri_WithFileUri_ReturnsTrue()
    {
        var localUri = new Uri("file:///C:/Windows/Fonts/arial.ttf");
        
        Factory.IsLocalUri(localUri).Should().BeTrue();
    }

    [Fact]
    public void IsLocalUri_WithHttpUri_ReturnsFalse()
    {
        var httpUri = new Uri("http://example.com/font.ttf");
        
        Factory.IsLocalUri(httpUri).Should().BeFalse();
    }

    [Fact]
    public void IsLocalUri_WithUncPath_ReturnsFalse()
    {
        var uncUri = new Uri("file://server/share/font.ttf");
        
        Factory.IsLocalUri(uncUri).Should().BeFalse();
    }

    [Fact]
    public void CreateTextAnalyzer_ShouldReturnNonNull()
    {
        var factory = DWriteFactory.Instance;
        
        var textAnalyzer = factory.CreateTextAnalyzer();
        
        textAnalyzer.Should().NotBeNull();
    }

    [Fact]
    public void CreateFontFile_WithSystemFont_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        
        if (File.Exists(arialPath))
        {
            var fontFile = factory.CreateFontFile(new Uri(arialPath));
            fontFile.Should().NotBeNull();
        }
    }

    [Fact]
    public void CreateFontFace_WithSystemFont_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        
        if (File.Exists(arialPath))
        {
            var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
            fontFace.Should().NotBeNull();
        }
    }

    [Fact]
    public void CreateFontFace_WithSimulations_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        
        if (File.Exists(arialPath))
        {
            var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, FontSimulations.Bold);
            fontFace.Should().NotBeNull();
            fontFace.SimulationFlags.Should().Be(FontSimulations.Bold);
        }
    }

    [Fact]
    public void CreateFontFile_WithNonExistentFile_ShouldThrow()
    {
        var factory = DWriteFactory.Instance;
        var nonExistentPath = new Uri("file:///C:/NonExistent/fake_font.ttf");

        Action act = () => factory.CreateFontFile(nonExistentPath);
        
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateFontFace_WithInvalidFaceIndex_ShouldThrow()
    {
        var factory = DWriteFactory.Instance;
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        
        if (File.Exists(arialPath))
        {
            // Arial.ttf typically has only one face (index 0), so index 999 should fail
            Action act = () => factory.CreateFontFace(new Uri(arialPath), 999);
            
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
