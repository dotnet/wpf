// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

/**************************************************************************
*
*
* Abstract:
*
*   GDI font installation information and management.
*
*
**************************************************************************/

// FontStreamContext
FontStreamContext::FontStreamContext(GlyphTypeface^ source)
{
    Debug::Assert(source != nullptr);

    _sourceTypeface = source;
}

FontStreamContext::FontStreamContext(Uri^ source, int streamLength)
{
    Debug::Assert(source != nullptr);

    _sourceUri = source;
    _streamLength = streamLength;
}

void FontStreamContext::Close()
{
    if (_stream != nullptr)
    {
        _stream->Close();
    }
}


const int c_FontBufferSize = 32 * 1024;


Stream^ CopyToMemoryStream(Stream ^ source)
{
    MemoryStream^ dest = gcnew MemoryStream();

    array<Byte>^ buffer= gcnew array<Byte>(c_FontBufferSize);

    int bytesRead = source->Read(buffer, 0, c_FontBufferSize);

    while (bytesRead > 0)
    {
        dest->Write(buffer, 0, bytesRead);

        bytesRead = source->Read(buffer, 0, c_FontBufferSize);
    }

    return dest;
}


Stream^ FontStreamContext::GetStream()
{
    if (_stream == nullptr)
    {
        if (_sourceUri != nullptr && _sourceUri->IsFile)
        {
            _stream = File::OpenRead(_sourceUri->LocalPath);
        }
        else if (_sourceTypeface != nullptr)
        {
            // avalon returns new stream on every GetFontStream() call
            _stream = _sourceTypeface->GetFontStream();

            if (! _stream->CanSeek)
            {
                Stream^ newstream = CopyToMemoryStream(_stream);

                _stream->Close();
                _stream = newstream;
            }
        }
    }
    else
    {
        // ensure stream is at zero
        _stream->Position = 0;
    }

    return _stream;
}

void FontStreamContext::UpdateStreamLength()
{
    if (_streamLength == 0)
    {
        Stream^ stream = GetStream();

        if (stream == nullptr || stream->Length >= MaximumStreamLength)
        {
            _streamLength = MaximumStreamLength;
        }
        else
        {
            _streamLength = (int)stream->Length;
        }
    }
}

bool FontStreamContext::Equals(FontStreamContext% otherContext)
{
    // make sure stream lengths are valid for comparison
    UpdateStreamLength();
    otherContext.UpdateStreamLength();

    if (_streamLength != otherContext._streamLength)
    {
        // streams have different lengths; definitely not the same font
        return false;
    }

    // otherwise compare first CompareLength bytes of both streams
    Stream^ thisStream = GetStream();

    if (thisStream != nullptr)
    {
        Stream^ otherStream = otherContext.GetStream();

        if (otherStream != nullptr)
        {
            Debug::Assert(thisStream->Length == otherStream->Length);

            //
            // Compare both streams CompareLength bytes at a time.
            //
            array<Byte>^ thisData = gcnew array<Byte>(CompareLength);
            array<Byte>^ otherData = gcnew array<Byte>(CompareLength);

            int eof = 0;    // number of streams reaching eof

            while (eof == 0)
            {
                // Read CompareLength amount of data from both streams, or less only if eof.
                int thisRead = 0, otherRead = 0;

                while (thisRead < CompareLength)
                {
                    int read = thisStream->Read(thisData, thisRead, CompareLength - thisRead);

                    if (read == 0)
                    {
                        eof++;
                        break;
                    }

                    thisRead += read;
                }

                while (otherRead < CompareLength)
                {
                    int read = otherStream->Read(otherData, otherRead, CompareLength - otherRead);

                    if (read == 0)
                    {
                        eof++;
                        break;
                    }

                    otherRead += read;
                }

                if (thisRead != otherRead || eof == 1)
                {
                    // One of the streams EOF'd early despite being same length. Assume both fonts aren't equal.
                    return false;
                }

                // Compare data byte-by-byte.
                for (int index = 0; index < thisRead; index++)
                {
                    if (thisData[index] != otherData[index])
                    {
                        // byte mismatch; not the same font
                        return false;
                    }
                }
            }

            Debug::Assert(eof == 2);
        }
    }

    return true;
}

// FontInstallInfo
FontInstallInfo::FontInstallInfo(Uri^ uri)
{
    Debug::Assert(uri != nullptr);

    _uri = uri;
}

bool FontInstallInfo::Equals(FontStreamContext% context, FontInstallInfo^ otherFont)
{
    Debug::Assert(otherFont != nullptr);

    if (_uri->Equals(otherFont->_uri))
    {
        // Fonts come from same location, therefore same.
        return true;
    }

    // Construct stream context with other font's URI as source,
    // and compare the two contexts for stream sameness.
    FontStreamContext otherContext(otherFont->_uri, otherFont->_streamLength);

    try
    {
        return context.Equals(otherContext);
    }
    finally
    {
        // The comparison process may've updated stream information for otherFont.
        // Cache it to otherFont.
        otherFont->UpdateFromContext(otherContext);
        otherContext.Close();
    }
}


// Class for handling TrueType font name change
// fyuan, 07/28/2006
//
// BUG 1772833: When installing fonts with the same name as existing fonts, GDI would not pick them.
// So we need to modify TrueType font to make the names 'unique'.
//
// Truetype name table: http://www.microsoft.com/typography/otspec/name.htm

value class TrueTypeFont
{
//constants for fields in Truetype name table
#define    NAME_FAMILY              1
#define    NAME_FULLNAME            4

#define    MS_PLATFORM              3
#define    MS_SYMBOL_ENCODING       0
#define    MS_UNICODEBMP_ENCODING   1
#define    MS_LANG_EN_US            0x409

#define    MAC_PLATFORM             1
#define    MAC_ROMAN_ENCODING       0
#define    MAC_LANG_ENGLISH         0

    array<Byte>^   m_fontdata;       // Complete font data
    unsigned       m_faceIndex;     // Truetype font collection index
    unsigned       m_size;           // font data size

    static Random  ^ s_random = gcnew Random();
    static int       s_order  = 0;

public:

    TrueTypeFont(array<Byte>^ fontdata, unsigned faceIndex)
    {
        m_fontdata  = fontdata;
        m_faceIndex = faceIndex;
        m_size      = (unsigned)fontdata->Length;
    }

    // Replace font family name with new randomly generated 'unique' name
    // Return new family name
    String ^ ReplaceFontName()
    {
        unsigned base = 0;

        if (read32(0) == 0x74746366) // ttcf: TrueType font collection
        {
            unsigned nfonts = read32(8);

            if (m_faceIndex >= nfonts)
            {
                return nullptr;
            }

            base = read32(12 + m_faceIndex * 4);
        }

        unsigned version = read32(base);       // TableDirectory
        (void)version;

        int ntables = read16(base + 4);

        for (int i = 0; i < ntables; i ++)
        {
            unsigned pos = base + 12 + i * 16;     // TableEntry

            unsigned tag = read32(pos);

            if (tag == 0x6E616D65) // 'name'
            {
                return ProcessNameTable(pos);
            }
        }

        return nullptr;
    }

private:
    // Replace font family name in name table
    String ^ ProcessNameTable(unsigned pos)
    {
        // TableEntry
        //      ULONG tag
        //      ULONG checksum
        //      ULONG offset
        //      ULONG length

        unsigned crc = read32(pos + 4);          // check sum
        unsigned len = read32(pos + 12);

        unsigned nametablepos = read32(pos + 8); // offset to name table

        unsigned sum = CheckSum(nametablepos, len);

        if (sum != crc)
        {
            return nullptr;
        }

        array<Char>^ familyName;
        array<Char>^ newFamilyName;

        GenerateFamilyNameFromNametable(nametablepos, familyName, newFamilyName);

        if(newFamilyName == nullptr)
        {
            return nullptr;
        }

        int count = ReplaceAll(nametablepos, familyName, newFamilyName);

        if (count == 0)
        {
            return nullptr;
        }

        sum = CheckSum(nametablepos, len);

        write32(pos + 4, sum);  // update checksum

        String^ newName = gcnew String(newFamilyName);

#ifdef Testing
        if(newName != nullptr)
        {
            FileStream^ dest = gcnew FileStream(String::Concat("c:\\", String::Concat(newName, ".ttf")), FileMode::Create, FileAccess::Write);

            dest->Write(m_fontdata, 0, m_size);

            dest->Close();
        }
#endif

        return newName;
    }


    // Fix: Windows OS Bugs 1925144:
    // Extending font family name lookup to use MS <OSLANG> Unicode, MS <OSLANG> Symbol and Mac English Roman family names
    // (where <OSLANG> denotes the OS language).
    // The previous implementation was unable to rename some embedded fonts because it only checked for MS English Unicode family names

    // Search for the Microsoft <OSLANG> or the Macintosh English family names and generate a random alternate
    // When the function returns
    //    familyName will be set to the MS <OSLANG> Unicode family name if one was found
    //    newFamilyName will be set to the generated name (which can still happen even if an MS <OSLANG> Unicode family name was not found)
    void GenerateFamilyNameFromNametable(unsigned nametablepos, array<Char>^ % familyName, array<Char>^ % newFamilyName)
    {
        // NameHeader
        //      USHORT formatSelector;
        //      USHORT numNameRecords;
        //      USHORT offsetToStringStorage;   // from start of table

        array<Char>^ fallbackFamilyName = nullptr;
        familyName = nullptr;
        newFamilyName = nullptr;

        unsigned formatSelector = read16(nametablepos);
        (void)formatSelector;
        unsigned numNames       = read16(nametablepos + 2);
        unsigned stringOffset   = read16(nametablepos + 4);

        unsigned osLanguageID = (unsigned)CultureInfo::InstalledUICulture->LCID;

        for (unsigned i = 0; i < numNames; i++)
        {
            unsigned p = nametablepos + 6 + i * 12;
            unsigned nameID     = read16(p + 6);

            if(nameID == NAME_FAMILY)
            {
                unsigned platformID = read16(p);
                unsigned encodingID = read16(p + 2);
                unsigned languageID = read16(p + 4);

                if(platformID == MS_PLATFORM && (encodingID == MS_UNICODEBMP_ENCODING || encodingID == MS_SYMBOL_ENCODING) && languageID == osLanguageID)
                {
                    unsigned length  = read16(p + 8);
                    unsigned offset  = read16(p + 10);
                    unsigned namepos = nametablepos + stringOffset + offset;

                    if(encodingID == MS_UNICODEBMP_ENCODING)
                    {
                        //The MS Unicode family name is GDI's prefered family name, dont look for any alternate names
                        readString(namepos, length, familyName, System::Text::Encoding::BigEndianUnicode);
                        break;
                    }
                    else
                    {
                        //Use the MS Symbol family name as a fallback in the absence of a MS Unicode name
                        readString(namepos, length, fallbackFamilyName, System::Text::Encoding::BigEndianUnicode);
                    }
                }
                else if(platformID == MAC_PLATFORM && encodingID == MAC_ROMAN_ENCODING && languageID == MAC_LANG_ENGLISH)
                {
                    unsigned length  = read16(p + 8);
                    unsigned offset  = read16(p + 10);
                    unsigned namepos = nametablepos + stringOffset + offset;

                    //Use the MS Symbol family name as a fallback in the absence of a MS Unicode name
                    readString(namepos, length, fallbackFamilyName, System::Text::Encoding::ASCII);
                }
            }
        }

        if(familyName != nullptr)
        {
            newFamilyName = GenerateRandomName(familyName->Length);
        }
        else if(fallbackFamilyName != nullptr)
        {
            newFamilyName = GenerateRandomName(fallbackFamilyName->Length);
        }
    }

    // Replace all matches of font family name in TrueType font name table
    int ReplaceAll(unsigned nametablepos, array<Char>^ baseEnglishFamilyName, array<Char>^ newFamilyName)
    {
        /*
            Fix: Windows OS Bugs 1925144: Ported font rename logic from TTEmbed code.
            The previous implementation did not follow some subtle font rename conventions and created fonts that could not
            be located using newFamilyname

            Replace all Family Names, Full Family Names and Unique Names with newFamilyName given the following constraints
            Only replace the prefix of an MS Full Family Name that matches
                An existing Family Name of the same platform and language
                or The MS <OSLANG> Unicode Family Name
            Only replace the prefix of a MAC Full Family Name that matches
                An existing Family Name of the same platform

            using the following algorithm

            Given an EnglishBaseFamilyName
            //Obtained by scanning the name table for the first <OSLANG> MS Unicode Family Name
            //note it is possible for such a record to not exist
            //also note that it's not necessarily English (despite the variable's name)

            While scanning the name table a second time
                For any Family Name (MS Unicode, MS Symbol or Mac Roman)
                    Let CurrentBaseFamily=the entry (its value, platform and language)
                    Replace the entry's value with newFamilyName

                For any Full Family Name (MS Unicode, MS Symbol)
                    If there is a CurrentBaseFamily and the entry has the same platform and language as the CurrentBaseFamily
                        let familyNamePrefix = CurrentBaseFamily's value
                    Else
                        let familyNamePrefix = EnglishBaseFamilyName
                    If a familyNamePrefix was set
                        If the entry's value starts with familyNamePrefix
                            Replace familyNamePrefix in the entry with newFamilyName

                For any Full Family Name (Mac Roman)
                    If there is a CurrentBaseFamily and the entry has the same platform as the CurrentBaseFamily
                        let familyNamePrefix = CurrentBaseFamily's value
                        If the entry's value starts with familyNamePrefix
                            Replace familyNamePrefix in the entry with newFamilyName
        */

        int count = 0;
        array<Char>^ baseFamilyName = nullptr;

        unsigned numNames     = read16(nametablepos + 2);
        unsigned stringOffset = read16(nametablepos + 4);

        unsigned basePlatformID = 0;
        unsigned baseEncodingID = 0;
        unsigned baseLanguageID = 0;

        for (unsigned i = 0; i < numNames; i ++)
        {
            unsigned p = nametablepos + 6 + i * 12;
            unsigned platformID = read16(p);
            unsigned encodingID = read16(p + 2);
            unsigned languageID = read16(p + 4);
            unsigned nameID     = read16(p + 6);
            unsigned length     = read16(p + 8);
            unsigned offset     = read16(p + 10);
            unsigned namepos    = nametablepos + stringOffset + offset;

            switch(nameID)
            {
                case NAME_FAMILY:
                {
                    if((platformID == MS_PLATFORM) && (encodingID == MS_UNICODEBMP_ENCODING || encodingID == MS_SYMBOL_ENCODING))
                    {
                        readString(namepos, length, baseFamilyName, System::Text::Encoding::BigEndianUnicode);
                        basePlatformID = platformID;
                        baseEncodingID = encodingID;
                        baseLanguageID = languageID;

                        if(ReplaceFamilyName(namepos, length, newFamilyName, System::Text::Encoding::BigEndianUnicode))
                        {
                            count++;
                        }
                    }
                    else if((platformID == MAC_PLATFORM) && (encodingID == MAC_ROMAN_ENCODING))
                    {
                        readString(namepos, length, baseFamilyName, System::Text::Encoding::ASCII);
                        basePlatformID = platformID;
                        baseEncodingID = encodingID;
                        baseLanguageID = languageID;

                        if(ReplaceFamilyName(namepos, length, newFamilyName, System::Text::Encoding::ASCII))
                        {
                            count++;
                        }
                    }

                    break;
                }

                case NAME_FULLNAME:
                {
                    if((platformID == MS_PLATFORM) && (encodingID == MS_UNICODEBMP_ENCODING || encodingID == MS_SYMBOL_ENCODING))
                    {
                        array<Char>^ familyName = nullptr;

                        if(baseFamilyName != nullptr && basePlatformID == platformID && baseLanguageID == languageID)
                        {
                            familyName = baseFamilyName;
                        }
                        else
                        {
                            familyName = baseEnglishFamilyName;
                        }

                        if(familyName != nullptr)
                        {
                            if(ReplaceFullFamilyName(namepos, length, familyName, newFamilyName, System::Text::Encoding::BigEndianUnicode))
                            {
                                count++;
                            }
                        }
                    }
                    else if((platformID == MAC_PLATFORM) && (encodingID == MAC_ROMAN_ENCODING))
                    {
                        if(baseFamilyName != nullptr && basePlatformID == platformID)
                        {
                            if(ReplaceFullFamilyName(namepos, length, baseFamilyName, newFamilyName, System::Text::Encoding::ASCII))
                            {
                                count++;
                            }
                        }
                    }

                    break;
                }
            } //end switch
        } //end for

        return count;
    }

    //Replaces a Family Name entry
    //newFamilyName must be the same byte length as length when encoded
    bool ReplaceFamilyName(unsigned namepos, unsigned length, array<Char>^ newFamilyName, System::Text::Encoding^ encoding)
    {
        if(length == (unsigned)encoding->GetByteCount(newFamilyName))
        {
            writeString(namepos, length, newFamilyName, encoding);
            return true;
        }

        return false;
    }

    //Replaces the Family Name prefix of a Full Family Name
    //if the entry starts with familyName then familyName is replaced with newFamilyName
    //familyName and newFamilyName must have the same length
    bool ReplaceFullFamilyName(unsigned namepos, unsigned length, array<Char>^ familyName, array<Char>^ newFamilyName, System::Text::Encoding^ encoding)
    {
        array<Char>^ fullName = nullptr;

        readString(namepos, length, fullName, encoding);
        if(newFamilyName->Length <= familyName->Length)
        {
            if(AreCharsEqual(familyName, fullName, newFamilyName->Length))
            {
                writeString(namepos, encoding->GetByteCount(newFamilyName), newFamilyName, encoding);
                return true;
            }
        }

        return false;
    }

    array<Char>^ GenerateRandomName(unsigned length)
    {
        array<Char>^ newName =  gcnew array<Char>(length);

        unsigned start = 2;
        if(newName->Length < 2)
        {
            start = 0;
        }
        else
        {
            newName[0] = (Char)('0' + ((s_order / 10) % 10));
            newName[1] = (Char)('0' + (s_order % 10));
        }

        for (unsigned i = start; i < (unsigned)newName->Length; i++)
        {
            newName[i] = (Char)('a' + s_random->Next(26));    // random low-case character
        }

        s_order ++;

        return newName;
    }

    //Returns true if
    //  a and b have up to length characters
    //  and
    //  a and b's first length characters are identical
    bool AreCharsEqual(array<Char>^ a, array<Char>^ b, unsigned length)
    {
        unsigned i = 0;

        if(((unsigned)a->Length < length || ((unsigned)b->Length) < length))
        {
            return false;
        }

        for(i = 0; i < length; i++)
        {
            if(a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    // Truetype font table checksum
    unsigned CheckSum(unsigned pos, unsigned len)
    {
        len = (len + 3) / 4; // Always DWORD aligned

        unsigned sum = 0;

        for (unsigned i = 0; i < len; i ++)
        {
            sum += read32(pos);
            pos += 4;
        }

        return sum;
    }

    // Read two bytes and reverse byte order
    unsigned short read16(unsigned offset)
    {
        return (m_fontdata[offset] << 8) | m_fontdata[offset + 1];
    }

    // Read four bytes and reverse byte order
    unsigned read32(unsigned offset)
    {
        return (m_fontdata[offset] << 24) | (m_fontdata[offset + 1] << 16) | (m_fontdata[offset + 2] << 8) + m_fontdata[offset + 3];
    }

    // Write two bytes in reverse byte order
    void write16(unsigned offset, unsigned short value)
    {
        m_fontdata[offset + 1] = (Byte) (value); value >>= 8;
        m_fontdata[offset    ] = (Byte) (value);
    }

    // Write four bytes in reverse byte order
    void write32(unsigned offset, unsigned value)
    {
        m_fontdata[offset + 3] = (Byte) (value); value >>= 8;
        m_fontdata[offset + 2] = (Byte) (value); value >>= 8;
        m_fontdata[offset + 1] = (Byte) (value); value >>= 8;
        m_fontdata[offset    ] = (Byte) (value);
    }

    //write a string with a given encoding
    //only System::Text::Encoding::ASCII and System::Text::Encoding::BigEndianUnicode are safe to use)
    //returns false if the bytes written exceeds byteLength
    bool writeString(unsigned offset, unsigned byteLength, String^ value, System::Text::Encoding^ encoding)
    {
        unsigned charCount = (encoding->IsSingleByte) ? byteLength : (byteLength / 2);
        return byteLength >= (unsigned)encoding->GetBytes(value, 0, charCount, m_fontdata, offset);
    }

    //write a string with a given encoding
    //only System::Text::Encoding::ASCII and System::Text::Encoding::BigEndianUnicode are safe to use)
    void writeString(unsigned offset, unsigned byteLength, array<Char>^ value, System::Text::Encoding^ encoding)
    {
        unsigned charCount = (encoding->IsSingleByte) ? byteLength : (byteLength / 2);
        encoding->GetBytes(value, 0, charCount, m_fontdata, offset);
    }

    //reads a string with a given encoding
    //value is resize to be exactly big enough to accept the string
    //only System::Text::Encoding::ASCII and System::Text::Encoding::BigEndianUnicode are safe to use)
    void readString(unsigned offset, unsigned byteLength, array<Char>^ % value, System::Text::Encoding^ encoding)
    {
        unsigned charCount = (encoding->IsSingleByte) ? byteLength : (byteLength / 2);
        if(value == nullptr || (unsigned)value->Length != charCount)
        {
            value = gcnew array<Char>(charCount);
        }

        encoding->GetChars(m_fontdata, offset, byteLength, value, 0);
    }
};


Object^ FontInstallInfo::Install(FontStreamContext% context, String^ % newFamilyName, unsigned faceIndex)
{
    // cache font stream length and hash if provided in context
    UpdateFromContext(context);

    Object^ installHandle = nullptr;

    // Comment out AddFontResourceEx path. We need to modify font name table before installation.
    // So we can't use original file content.

/*  if (_uri->IsFile)
    {
        // install font from file
        int numberAdded = CNativeMethods::AddFontResourceEx(_uri->LocalPath, FR_PRIVATE | FR_NOT_ENUM, nullptr);

        if (numberAdded > 0)
        {
            installHandle = _uri->LocalPath;
        }
    }
*/
    if (installHandle == nullptr)
    {
        // read stream and install from memory
        context.UpdateStreamLength();
        int size = context.StreamLength;

        if (size > 0 && size < FontStreamContext::MaximumStreamLength)
        {
            Stream^ stream = context.GetStream();

            if (stream != nullptr)
            {
                array<Byte>^ data = gcnew array<Byte>(size);

                // ensure we read entire font file for GDI install
                if (stream->Read(data, 0, size) == size)
                {
                    TrueTypeFont font(data, faceIndex);

                    newFamilyName = font.ReplaceFontName();

                    DWORD nFonts = 0;

                    installHandle = CNativeMethods::AddFontMemResourceEx(data, size, NULL, &nFonts);
                }
            }
        }
    }

    return installHandle;
}


void FontInstallInfo::Uninstall(Object^ installHandle)
{
    Debug::Assert(installHandle != nullptr);

    String^ filename = dynamic_cast<String^>(installHandle);

    if (filename != nullptr)
    {
        // uninstall local file
        int errCode = CNativeMethods::RemoveFontResourceEx(filename, FR_PRIVATE | FR_NOT_ENUM, NULL);
        Debug::Assert(errCode != 0, "RemoveFontResourceEx failed");
    }
    else
    {
        GdiFontResourceSafeHandle^ hfont = dynamic_cast<GdiFontResourceSafeHandle^>(installHandle);

        if (hfont != nullptr)
        {
            // We can't unstall the font from memory now, because it could be still needed by printer driver
            // in local EMF spooling print job. Move such handles into OldPrivateFonts
            CGDIDevice::OldPrivateFonts->Add(hfont);
            // hfont->Close(); uninstall from memory
        }
    }
}

void FontInstallInfo::UpdateFromContext(FontStreamContext% context)
{
    // save font stream length to avoid reopening the stream in the future
    // when comparing lengths
    if (_streamLength == 0)
    {
        _streamLength = context.StreamLength;
    }
}

// FontInfo
FontInfo::FontInfo()
{
}

FontInfo::FontInfo(Uri^ systemUri)
{
    Debug::Assert(systemUri != nullptr, "System font URI can't be null");

    _systemInstall = gcnew FontInstallInfo(systemUri);
}

bool FontInfo::UsePrivate(GlyphTypeface^ typeface)
{
    //
    // Prepare GDI to render text using GlyphTypeface. First see if GlyphTypeface is already
    // installed as private or system font, in which case we simply use one of those.
    // Otherwise install the GlyphTypeface font into GDI.
    //
    Debug::Assert(typeface != nullptr);

    FontStreamContext installContext(typeface);

    try
    {
        FontInstallInfo^ install = gcnew FontInstallInfo(Microsoft::Internal::AlphaFlattener::Utility::GetFontUri(typeface));

        if (_privateInstall != nullptr)
        {
            // We have a private font installed with this name. If requested typeface
            // matches this private font, use it, otherwise uninstall it.
            if (install->Equals(installContext, _privateInstall))
            {
                return true;
            }
            else
            {
                UninstallPrivate();
            }
        }

        Debug::Assert(_privateInstall == nullptr, "Private font should not be installed at this point");

        if (_systemInstall != nullptr && install->Equals(installContext, _systemInstall))
        {
            // Requested typeface matches the system-installed font; use that one.
            return true;
        }

        // Otherwise we need to install a new private font.
        _privateInstallHandle = install->Install(installContext, _newFamilyName, typeface->FaceIndex);

        if (_privateInstallHandle == nullptr)
        {
            return false;
        }
        else
        {
            _privateInstall = install;
            return true;
        }
    }
    finally
    {
        installContext.Close();
    }
}

void FontInfo::UninstallPrivate()
{
    if (_privateInstall != nullptr)
    {
        Debug::Assert(_privateInstallHandle != nullptr);

        _privateInstall->Uninstall(_privateInstallHandle);

        _privateInstall = nullptr;
        _privateInstallHandle = nullptr;
        _newFamilyName = nullptr;
    }
}
