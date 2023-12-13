// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

ref class FontInfo;

//
// Wraps a font stream source (file Uri or GlyphTypeface) and allows comparing
// font streams to determine if two fonts are the same.
//
// Stream length is cached to avoid reopening the stream, and is only updated
// via explicit UpdateStreamLength() call.
//
// Close() must be called when context becomes unused to close the underlying stream
// if it was opened.
//
ref struct FontStreamContext
{
// Constants
public:
    // Maximum font size we'll process.
    static const int MaximumStreamLength = Int32::MaxValue;

private:
    // We compare font files CompareLength bytes at a time.
    static const int CompareLength = 65535;

// Constructors
public:
    FontStreamContext(GlyphTypeface^ source);

    FontStreamContext(Uri^ source, int streamLength);

// Public Properties
public:
    // Length of font stream.
    property int StreamLength
    {
        int get() { return _streamLength; }
    }

// Public Methods
public:
    /// <Remarks>
    /// Closes the underlying stream if it's open.
    /// </Remarks>
    void Close();

    /// <Remarks>
    /// Gets font stream, opening it if necessary.
    /// May return nullptr if unable to open stream.
    /// Stream position guaranteed to be at zero.
    /// </Remarks>
    Stream^ GetStream();

    /// <Remarks>
    /// Updates length information, opening the font stream if necessary.
    /// </Remarks>
    void UpdateStreamLength();

    /// <Remarks>
    /// Determines if two font streams are the same, comparing length and first CompareLength
    /// bytes if necessary.
    /// </Remarks>
    bool Equals(FontStreamContext %otherContext);

// Private Fields
private:
    GlyphTypeface^ _sourceTypeface;

    Uri^ _sourceUri;

    Stream^ _stream;

    int _streamLength;
};

//
// Describes a font installation instance, either an existing system installation of the font or
// a private install during the course of printing glyphs.
//
// Allows comparing two font installations to determine if the fonts are the same.
// Can install/uninstall the font from GDI.
//
ref class FontInstallInfo
{
public:
    FontInstallInfo(Uri^ uri);

// Public Methods
public:
    /// <Remarks>
    /// Determines if two font installations refer to the same font, given stream context of this installation.
    /// </Remarks>
    bool Equals(FontStreamContext% context, FontInstallInfo^ otherFont);

    /// <Remarks>
    /// Installs GDI font via AddFont*ResourceEx.
    ///
    /// Returns either null (installation failed), string (font filename; installed from file),
    /// or GDI install handle (installed from memory). Returns new font family name via newFamilyName
    /// </Remarks>
    Object^ Install(FontStreamContext% context, String^ % newFamilyName, unsigned faceIndex);

    /// <Remarks>
    /// Uninstalls GDI font via RemoveFont*ResourceEx.
    ///
    /// installHandle is either a string (file to uninstall) or GdiFontResourceSafeHandle (handle to
    /// uninstall font from memory).
    /// </Remarks>
    void Uninstall(Object^ installHandle);

// Private Fields
private:
    Uri^ _uri;

    int _streamLength;

// Private Methods
private:
    /// <Remarks>
    /// Caches font stream information to speed up future font stream comparisons.
    /// </Remarks>
    void UpdateFromContext(FontStreamContext% context);
};

//
// Stores information to track the status of a font used to print a document. Each FontInfo
// corresponds to a font with a particular name.
//
// The system may have a font installed with this name, in which case _systemInstall != nullptr.
// The font used can be overridden by installing a private font; _privateInstall != nullptr in
// that case.
//
ref class FontInfo
{
public:
    // Constructs FontInfo that describes no installed font with this name.
    FontInfo();

    /// <Remarks>Constructs FontInfo that describes a system-installed font.</Remarks>
    FontInfo(Uri^ systemUri);

// Public Methods
public:
    /// <Remarks>
    /// Prepares GDI to render glyphs using GlyphTypeface by installing GDI fonts or
    /// verifying that the currently installed GDI font matches GlyphTypeface.
    ///
    /// Returns false if GDI could not be prepared. In such an event, should fallback
    /// to filling glyph geometry.
    /// </Remarks>
    bool UsePrivate(GlyphTypeface^ typeface);

    /// <Remarks>Uninstalls the private font if one was installed.</Remarks>
    void UninstallPrivate();

    property String^ NewFamilyName
    {
        String^ get()
        {
            return _newFamilyName;
        }
    }

// Private Fields
private:
    FontInstallInfo^ _systemInstall;
    FontInstallInfo^ _privateInstall;
    String^          _newFamilyName;    // New 'unique' font family name to avoid name conflict. 
                                        // Should be valid when _privateInstall has value

    Object^ _privateInstallHandle;
};
