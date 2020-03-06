// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#include <FontInfo.hpp>

#undef GetEnvironmentVariable

using namespace System::Globalization;
using namespace Microsoft::Win32;
using namespace System::Collections::Generic;
using namespace System::Security;

bool IsLeftToRight(GlyphRun ^pGlyphRun)
{
	return ((pGlyphRun->BidiLevel & 1) == 0);
}

/// <remarks>
/// Does the dictionary correctly map the keys to values
/// </remarks>
bool IsMappingConsistent(
	System::Collections::Generic::IDictionary<Int32, UInt16>  ^map,
	System::Collections::Generic::IList<Char> ^keys,
	System::Collections::Generic::IList<UInt16> ^expectedValues)
{
	if(map == nullptr || keys == nullptr ||  expectedValues == nullptr)
	{
		return false;
	}

	if(keys->Count != expectedValues->Count)
	{
		return false;
	}

	for(int i = 0; i < keys->Count; i++)
	{
		unsigned short int value;
		if(map->TryGetValue(keys[i], value))
		{
			if(value != expectedValues[i])
			{
				return false;
			}
		}
		else
        {
            return false;
        }
    }

    return true;
}

bool RenderUnicode(GlyphRun ^pGlyphRun)
{
    bool renderCodepoints =
	(pGlyphRun->Characters != nullptr && pGlyphRun->Characters->Count > 0) &&
	// If there are no characters to render try passing GDI glyph indices

	// When IsSideways is true, we need to access WPF's topsideBearings, which is glyph index based. So we need to pass GDI glyph indices
	(!pGlyphRun->IsSideways) &&

	// GDI's shaping does not always agree with WPF's given RTL text. So we need to pass GDI glyph indices
	IsLeftToRight(pGlyphRun) &&

	// For symbol font, Unicode string is off by 0xFF00. . So we need to pass GDI glyph indices
        (!pGlyphRun->GlyphTypeface->Symbol) &&

	// GDI's uses the cmap table of a given typeface for mapping codepoints to glyph indices
	// If the mapping from glyphRun->Characters to glyphRun->GlyphIndices does not agree with the typefaces cmap table
	// GDI will display different glyphs from WPF so pass GDI use glyph indices instead
        IsMappingConsistent(
		pGlyphRun->GlyphTypeface->CharacterToGlyphMap, // **DO NOT USE IN WPF 4.0+** the performance of dictionary lookups on GlyphTypeface->CharacterToGlyphMap has significantly regressed
		pGlyphRun->Characters,
		pGlyphRun->GlyphIndices
	);

    // In case we are rendering code points we should have one to one mapping between the code points
    // and glyphs. otherwise we render glyphs.
    Debug::Assert(!renderCodepoints ||
        (  pGlyphRun->Characters != nullptr
        && pGlyphRun->GlyphIndices->Count == pGlyphRun->Characters->Count
        )
      );

    //
    // Fix bug 1505836: PasswordBox with PasswordChar="" results in box without box characters.
    //
    // A PasswordChar of "" results in GlyphRun where characters and glyph indices are all 0.
    // ExtTextOut displays nothing when rendering characters, but displays boxes when rendering
    // glyph indices. Force glyph index rendering if any indices are zero.
    //
    if (pGlyphRun->GlyphIndices != nullptr)
    {
        for (int index = 0; index < pGlyphRun->GlyphIndices->Count; index++)
        {
            if (pGlyphRun->GlyphIndices[index] == 0)
            {
                renderCodepoints = false;
                break;
            }
        }
    }

    return renderCodepoints;
}


// ROBERTAN
HRESULT CGDIRenderTarget::RenderGlyphRun(
    GlyphRun ^pGlyphRun,
    Point translate,
    Point scale,
    Boolean isPrivateFont
    )
{
    HRESULT hr = S_OK;

    bool renderCodepoints = RenderUnicode(pGlyphRun);

    int direction = 1;

    if ((pGlyphRun->BidiLevel & 1) == 0)
    {
        SetTextAlign(TA_BASELINE | TA_LEFT); // Left alignment
    }
    else
    {
        SetTextAlign(TA_BASELINE | TA_RIGHT); // Right alignment
        direction = -1;
    }

    // baseline in device units
    double baselineX = translate.X + (pGlyphRun->BaselineOrigin.X * scale.X);
    double baselineY = translate.Y + (pGlyphRun->BaselineOrigin.Y * scale.Y);

    if (pGlyphRun->IsSideways)
    {
        TEXTMETRIC metric;

        int errCode = CNativeMethods::GetTextMetrics(m_hDC, & metric);  // ROBERTAN
        Debug::Assert(errCode != 0, "GetTextMetrics failed");

        // Trying to map Avalon with GDI, still not perfect
        baselineX -= metric.tmExternalLeading;
    }

    int glyphCount = 0;
    UINT etoOptions = renderCodepoints ? 0 : ETO_GLYPH_INDEX;

    interior_ptr<unsigned short> text = nullptr;

    if (renderCodepoints)
    {
        // prevent the string from moving around. we end up duplicating the string through
        // the array conversion, but it's needed to call win32 api
        glyphCount = pGlyphRun->Characters->Count;
        array<Char> ^ characters = gcnew array<Char>(glyphCount);
        pGlyphRun->Characters->CopyTo(characters, 0);

        text = (interior_ptr<unsigned short>)& characters[0];
    }
    else
    {
        glyphCount = pGlyphRun->GlyphIndices->Count;
        array<unsigned short> ^ glyphIndices = gcnew array<unsigned short>(glyphCount);
        pGlyphRun->GlyphIndices->CopyTo(glyphIndices, 0);

        text = & glyphIndices[0];
    }

    array<int> ^ dx = gcnew array<int>(glyphCount);

    System::Collections::Generic::IList<Point> ^ glyphOffsets = pGlyphRun->GlyphOffsets;    // Avalon units

    // Keep track of glyph position (sum of advance widths of prior glyphs).
    System::Collections::Generic::IList<double> ^ advanceWidths = pGlyphRun->AdvanceWidths;  // Avalon units
    double glyphPositionX = 0;  // Avalon units

    System::Collections::Generic::IDictionary<unsigned short, double>^ topsideBearings    = nullptr;

    if (pGlyphRun->IsSideways)
    {
        topsideBearings    = pGlyphRun->GlyphTypeface->TopSideBearings;
    }

    for (int i = 0; i < glyphCount; )
    {
        int j = i + 1;

        Point offset;

        if (glyphOffsets != nullptr)
        {
            offset = glyphOffsets[i];
        }

        if (pGlyphRun->IsSideways)
        {
            offset.X += topsideBearings[text[i]] * pGlyphRun->FontRenderingEmSize;
        }

        // origin in device units
        int originX = (int) Math::Round(baselineX + (offset.X * direction + glyphPositionX) * scale.X);
        int originY = (int) Math::Round(baselineY - offset.Y * scale.Y);

        int oldX = originX;

        // Add width of glyph i to get position of glyph i+1=j in Avalon units
        glyphPositionX += advanceWidths[i] * direction;

        if (direction == 1) // Print multiple character in a single call only for left to right
        {
            // As long as Y glyph offsets are the same, we can work with runs of multiple glyphs
            for (; j < glyphCount; j ++)
            {
                Point offsetJ;

                if (glyphOffsets != nullptr)
                {
                    offsetJ = glyphOffsets[j];
                }

                if (!AreCloseReal(offsetJ.Y, offset.Y))
                {
                    break;
                }

                if (pGlyphRun->IsSideways)
                {
                    offsetJ.X = topsideBearings[text[j]] * pGlyphRun->FontRenderingEmSize;
                }

                int curX = (int) Math::Round(baselineX + (offsetJ.X * direction + glyphPositionX) * scale.X);

                dx[j - 1] = curX - oldX;

                oldX = curX;

                // add width of glyph j to get position of glyph j+1 Avalon units
                glyphPositionX += advanceWidths[j] * direction;
            }
        }

        // we are not using DC current position, so the last value
        // in lpDx are of no matter. Just for clarity - let's put zero here
        dx[j - 1] = 0;

        pin_ptr<unsigned short> textPin = &text[i];
        pin_ptr<int> pDxPin = &dx[i];
        int* pDxArg = pDxPin;

        // Skipping Dx array if there is just one character
        if ((j - i) == 1)
        {
            pDxArg = NULL;
        }

        // Workaround for GDI bug where ExtTextOut no-ops when
        //    A private (memory) font is selected into the DC
        //    The DC is obtained from a text only printer (CAP_CharacterStream)(e.g. the "Generic\Text only" printer)
        //    and the fuOptions flags does not include ETO_GLYPH_INDEX
        // We workaround this by selecting a stock font.
        bool workAroundMemFontPrintingBug =
            isPrivateFont
            && (CAP_CharacterStream == (GetCaps() & CAP_CharacterStream))
            && (0x0000 == (etoOptions & ETO_GLYPH_INDEX))
        ;

        if(workAroundMemFontPrintingBug)
        {
            GdiSafeHandle^ stockFont = CNativeMethods::GetStockObject(DEVICE_DEFAULT_FONT);
            SelectObject(stockFont, OBJ_FONT);

            // Disable the Dx array
            // Character stream devices usually resolve overlapping glyphs by not rendering or
            // pushing one of the glyphs down by a line.
            // using the Dx array leads to a lot of bounding rect overlapps and ultimately unreadable text.
            pDxArg = NULL;
        }

        // GetLastError after ExtTextOut is not so reliable (GDI bug 1764877)
        if (CNativeMethods::ExtTextOutW(m_hDC, originX, originY, etoOptions, NULL, textPin, j - i, pDxArg))
        {
            hr = S_OK;
        }
        else
        {
            // N.B. bug 1504904: ExtTextOut may fail but GetLastError==ERROR_SUCCESS if nothing rendered,
            // such as text too small due to transformation).

            hr = E_NOTIMPL; // allow converting to vector drawing using outline

            break;
        }

        i = j;
    }

    return hr;
}


HRESULT CGDIRenderTarget::RenderTextThroughGDI(
    GlyphRun ^pGlyphRun,
    Brush ^pBrush
    )
{
    // zero-length glyphrun
    if (pGlyphRun->GlyphIndices->Count == 0)
    {
        return S_OK;
    }

    // GDI only supports solid color for text.
    SolidColorBrush^ pForeground = dynamic_cast<SolidColorBrush ^>(pBrush);

    if (pForeground == nullptr)
    {
        return E_NOTIMPL;
    }

    HRESULT hr = S_OK;

    Point translate, scale;
    BOOL isScaleTranslateOnly;

    XFORM originalTransform;

    // Push simple transform (translate/scale) to glyph rendering code. In the case of
    // complex transformation, let GDI handle via SetWorldTransform.
    isScaleTranslateOnly = IsTranslateOrScale(m_transform) &&                   // no rotation/shearing
                           AreCloseReal(m_transform.M11, m_transform.M22)  &&    // 1:1 scaling
                           m_transform.M11 > 0 && m_transform.M22 > 0;          // no mirroring
    bool transformPushed = false;

    if (isScaleTranslateOnly)
    {
        // Manually transform glyphs in RenderGlyphRun.
        translate.X = m_transform.OffsetX;
        translate.Y = m_transform.OffsetY;
        scale.X     = m_transform.M11;
        scale.Y     = m_transform.M22;
    }
    else
    {
        // Otherwise let GDI transform via SetWorldTransform. Still need to transform
        // glyphs to device units.
        translate.X = 0.0f;
        translate.Y = 0.0f;
        scale.X     = m_nDpiX / 96.0;
        scale.Y     = m_nDpiY / 96.0;

        //
        // Get scaling components of m_transform. If it's > 0, push to glyph rendering, otherwise
        // integer rounding errors occur at small glyph sizes that when scaled up via m_transform
        // become very noticeable.
        //
        // S = [ scale | translate ]
        // M = m_transform
        // P = scaling transform pulled from M
        //
        // transform glyph to device = S * M = S * P * P^-1 * M = (S * P) * (P^-1 * M)
        //
        if (m_transform.M11 > 1 && m_transform.M22 > 1)
        {
            double transformScale = Math::Min(m_transform.M11, m_transform.M22);

            scale.X *= transformScale;
            scale.Y *= transformScale;

            Matrix transformScaleInvert;    // P^-1
            transformScaleInvert.Scale(1.0 / transformScale, 1.0 / transformScale);
            PushTransform(transformScaleInvert);
            transformPushed = true;
        }

        // TODO: If printers don't support arbitrary transformations very well, you can
        // disable the gdi codepath here by returning E_NOTIMPL. The text will then be rasterized
        // using Avalon.
    }

    // create font and use it
    Boolean isPrivateFont = false;
    GdiSafeHandle^ font = CreateFont(pGlyphRun, pGlyphRun->FontRenderingEmSize, scale.Y, isPrivateFont);

    if (font == nullptr)
    {
        hr = E_NOTIMPL;
    }

    if (SUCCEEDED(hr))
    {
        SelectObject(font, OBJ_FONT);

        // set state. if failure, still set all states but don't attempt to render text.
        // clipping. must do prior to text world transformation!
        // gdi transformation
        if (!isScaleTranslateOnly || pGlyphRun->IsSideways)
        {
            if (!isScaleTranslateOnly)
            {
                // originalTransform valid even if failed
                if (!SetTextWorldTransform(originalTransform))
                {
                    hr = E_NOTIMPL;
                }
            }
        }

        // text color
        SetTextColor(ToCOLORREF(pForeground));

        // render
        if (SUCCEEDED(hr))
        {
            hr = RenderGlyphRun(pGlyphRun, translate, scale, isPrivateFont);
        }

        if (!isScaleTranslateOnly || pGlyphRun->IsSideways)
        {
            if (!isScaleTranslateOnly)
            {
                int errCode = CNativeMethods::SetWorldTransform(m_hDC, &originalTransform);
                Debug::Assert(errCode != 0, "SetWorldTransform failed");
            }
        }
    }

    if (transformPushed)
    {
        PopTransform();
    }

    return hr;
}


void CGDIDevice::UninstallFonts()
{
    if (s_InstalledFonts != nullptr)
    {
        // Loop through all font names and uninstall any private fonts that were installed
        // for that name.
        for each(FontInfo^ info in s_InstalledFonts->Values)
        {
            info->UninstallPrivate();
        }
    }
}


/// <Returns>Returns nullptr if unable to retrieve directory name.</Returns>
String^ GetFontDir()
{
    StringBuilder^ fontDirectory = gcnew StringBuilder(MAX_PATH);

    //
    // Ideally we'd use Environment.GetFolderPath, but the Environment.SpecialFolder enumeration doesn't
    // have a Fonts entry as of .NET 2.0. We therefore use SHGetSpecialFolderPathW. Note that the BCL
    // already takes a dependency on shell32.dll (through shfolder.dll) by way of Environment.GetFolderPath.
    //
    if (CNativeMethods::SHGetSpecialFolderPathW(IntPtr::Zero, fontDirectory, CNativeMethods::CSIDL_FONTS, false))
    {
        return fontDirectory->ToString()->ToUpperInvariant();
    }
    else
    {
        return nullptr;
    }
}


Hashtable^ BuildFontList(String ^ fontdir)
{
    Hashtable^ installedFonts = gcnew Hashtable();

    RegistryKey^ key = Registry::LocalMachine->OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts");

    if (key != nullptr)
    {
        array<String^>^ valueNames = key->GetValueNames();

        for (int i = 0; i < valueNames->Length; i ++)
        {
            String ^name = valueNames[i];

            String ^value = key->GetValue(name)->ToString();

            Uri ^ font = gcnew Uri(value, UriKind::RelativeOrAbsolute);

            if (!font->IsAbsoluteUri)
            {
                font = gcnew Uri(Path::Combine(fontdir, value));
            }

            int pos = name->IndexOf('(');

            if (pos > 0)
            {
                String ^ post = name->Substring(pos);

                // Skip bitmap/vector fonts
                if ((post == "(All res)") || (post == "(VGA res)"))
                {
                    continue;
                }

                name = name->Substring(0, pos - 1);  // Remove " (TrueType)"
            }

            pos = name->IndexOf(" & ");

            // Split TrueType font collection: "MS Mincho & MS PMincho"

            while (pos > 0)
            {
                String ^ pre = name->Substring(0, pos);

                FontInfo^ fi = gcnew FontInfo(font);
                installedFonts[pre] = fi;

                name = name->Substring(pos + 3);

                pos = name->IndexOf(" & ");
            }

            // add system font to installed fonts table
            FontInfo^ fi = gcnew FontInfo(font);
            installedFonts[name] = fi;
        }
    }

    return installedFonts;
}


String ^ CGDIDevice::CheckFont(GlyphTypeface^ typeface, String ^ fontname, [Out] Boolean% isPrivateFont)
{
    isPrivateFont = false;

    System::Threading::Monitor::Enter(s_lockObject);
    {
        __try
        {
            if (s_InstalledFonts == nullptr)
            {
                // Build a list of installed Windows font

                String ^ fontdir = GetFontDir();

                if (fontdir == nullptr)
                {
                    // can't enumerate existing fonts, so can't perform font installation.
                    // fallback to filling text geometry.
                    return nullptr;
                }

                s_InstalledFonts = BuildFontList(fontdir);
            }

            // Get FontInfo entry for this name. Entry may not exist if there's
            // no system-installed font with this name.
            FontInfo^ info = (FontInfo^) s_InstalledFonts->default[fontname];

            if (info == nullptr)
            {
                info = gcnew FontInfo();
                s_InstalledFonts[fontname] = info;
            }

            // Install private font to override any system font with same name, if any.
            if (info->UsePrivate(typeface))
            {
                if (info->NewFamilyName != nullptr)
                {
                    isPrivateFont = true;
                    return info->NewFamilyName;
                }
                else
                {
                    return fontname;
                }
            }

            return nullptr;
        }
        __finally
        {
            System::Threading::Monitor::Exit(s_lockObject);
        }
    }
}

void CopyTo(interior_ptr<WCHAR> buffer, int len, String ^s)
{
    len = Math::Min(len, s->Length);

    while (len > 0)
    {
        len --;

	// Requires pointer math
        buffer[len] = s[len];
    }
}


/**************************************************************************\
*
* Function Description:
*
*   Port of unmanaged CGDIRenderTarget::CreateAndSelectFont to managed code.
*
* Arguments:
*
*   pGlyphRun - Run of glyphs we're rendering.
*
*   fontSize - Font em size in MIL units, which is defined as 1/96 inch.
*
*   scaleY - Height scaling value from MIL unit resolution to device resolution.
*               When device is 96 dpi, scaleY is 1.
*
* Return Value:
*
*   Created GDI HFONT handle, NULL on failure.
*
* History:
*   8/4/2004 t-bnguy    It now only creates font and does not select it.
*   7/1/2004 t-bnguy    Ported it.
*
\**************************************************************************/

GdiSafeHandle^ CGDIRenderTarget::CreateFont(
    GlyphRun ^pGlyphRun,
    double fontSize,
    double scaleY,
    [Out] Boolean% isPrivateFont
    )
{
    GlyphTypeface^ typeface = pGlyphRun->GlyphTypeface;

    CultureInfo^ engCulture = CultureInfo::GetCultureInfo("en-US");
    CultureInfo^ sysCulture = CultureInfo::InstalledUICulture;

    String ^familyName  = typeface->Win32FamilyNames->default[sysCulture];

    if (familyName == nullptr)
    {
        familyName  = typeface->Win32FamilyNames->default[engCulture];
    }

    String ^faceName = typeface->Win32FaceNames->default[sysCulture];

    if (faceName == nullptr)
    {
        faceName  = typeface->Win32FaceNames->default[engCulture];
    }

    String ^fullName    = nullptr;

    if (typeface->Win32FaceNames->default[engCulture] != "Regular")
    {
        fullName = String::Concat(String::Concat(familyName, " "), faceName);
    }
    else
    {
        fullName = familyName;
    }

    String ^ newName = CheckFont(typeface, fullName, isPrivateFont);

    if (newName == nullptr)
    {
        // font installation failed; fallback to filling text geometry
        return nullptr;
    }

    if (newName != fullName) // private font is installed
    {
        familyName = newName;
    }

    ENUMLOGFONTEXDV logfontdv;

    // Clear ENUMLOGFONTEXDV
    memset(&logfontdv, 0, sizeof logfontdv);

    LOGFONT & logfont = logfontdv.elfEnumLogfontEx.elfLogFont;

    logfont.lfQuality           = PROOF_QUALITY;
    logfont.lfPitchAndFamily    = FF_DONTCARE | DEFAULT_PITCH;

    if (typeface->Symbol)
    {
        logfont.lfCharSet = SYMBOL_CHARSET;
        logfont.lfOutPrecision  = OUT_OUTLINE_PRECIS;
    }
    else
    {
        logfont.lfCharSet = DEFAULT_CHARSET;
        logfont.lfOutPrecision  = OUT_TT_PRECIS;
    }

    if (typeface->Style != FontStyles::Normal)
    {
        logfont.lfItalic = true;
    }

    if (pGlyphRun->IsSideways)
    {
        logfont.lfOrientation = 900;  //90 degrees
    }

    logfont.lfWeight = typeface->Weight.ToOpenTypeWeight();

    // the scaleY is already multiplied by (device dpi / 96.0) then to select correct font size
    // by multiplying scaleY by fontSize where fontSize is 96.0 dpi.
    logfont.lfHeight = - (int) Math::Round(fontSize * scaleY);

    if ((typeface->StyleSimulations & StyleSimulations::ItalicSimulation) == StyleSimulations::ItalicSimulation)
    {
        // simulating italic on a font that is already italic is not supported by GDI
        if (logfont.lfItalic)
        {
            return nullptr;
        }

        logfont.lfItalic = true;
    }

    // bold simulation increases font weight by (FW_BOLD - FW_NORMAL)
    if ((typeface->StyleSimulations & StyleSimulations::BoldSimulation) == StyleSimulations::BoldSimulation)
    {
        logfont.lfWeight += (FW_BOLD - FW_NORMAL);

        // going higher than FW_HEAVY is not supported by GDI
        if (logfont.lfWeight > FW_HEAVY)
        {
            return nullptr;
        }
    }

    // This is needed for GDI to pick the right font, according to dbrown
    //  CopyTo(logfontdv.elfEnumLogfontEx.elfFullName, LF_FULLFACESIZE - 1, familyName);

    CopyTo(&logfont.lfFaceName[0], LF_FACESIZE - 1, familyName);

    //
    // We now have logfont info setup, perform actual font creation.
    //
    GdiSafeHandle^ fontResult;

    if ((int)typeface->StyleSimulations == 0)
    {
        // no style simulations, gdi's font should be correct
        fontResult = CreateFontCached(&logfontdv);
    }
    else
    {
        // path to handle fix for bug 985195, see CreateSimulatedStyleFont for more info
        fontResult = CreateSimulatedStyleFont(&logfontdv, typeface->StyleSimulations);
    }

    return fontResult;
}

// Creates or retrieves a cached font, and caches it if needed.
GdiSafeHandle^ CGDIRenderTarget::CreateFontCached(interior_ptr<ENUMLOGFONTEXDV> logfontdv)
{
    GdiSafeHandle^ result = CacheMatch((interior_ptr<Byte>) logfontdv, sizeof(ENUMLOGFONTEXDV));
    GdiSafeHandle^ firstAttempt = nullptr;

    if (result != nullptr)
    {
        return result;
    }

    // cached font not found, create it

    // Fix: Windows OS Bugs 1925144:
    // LOGFONT is underspecified and GDI could end up not selecting the correct font.
    // Cycle through a series of LOGFONTS until the created font has the same face name as the LOGFONT struct.
    // This is fairly reliable for xps embedded fonts because they have autogenerated names that are not visible across processes

    String^ desiredFaceName = nullptr;
    {
        pin_ptr<TCHAR> text = logfontdv->elfEnumLogfontEx.elfLogFont.lfFaceName;
        desiredFaceName = gcnew String(text);
    }

    // the loop below modifies logfontdv.  Save a copy of the original to use
    // as the key when calling CacheObject
    ENUMLOGFONTEXDV originalLogfontDv;
    ::memcpy(&originalLogfontDv, logfontdv, sizeof(ENUMLOGFONTEXDV));

    int i = 0;
    do
    {
        GdiSafeHandle^ tempResult = CNativeMethods::CreateFontIndirectEx(logfontdv);

        Debug::Assert(tempResult != nullptr, "CreateFontIndirectEx failed");

        if(tempResult != nullptr)
        {
            if(firstAttempt == nullptr)
            {
                // Keep the first attempt as a potential fallback
                // in the worst case scenario where we never convince GDI to load our embedded font
                // The theory here is that the first attempt is the least constrained and
                // gives GDI the best chance of finding a 'close enough' matching font
                // It also makes the font matching behavior more predicatable because the guarantee is
                // we either load the embedded font or the best matching font given logfont defaults
                firstAttempt = tempResult;
            }

            if(desiredFaceName != nullptr)
            {
                String^ actualFaceName = GetFontFace(tempResult);
                if(actualFaceName != nullptr && 0 == String::Compare(desiredFaceName, actualFaceName, StringComparison::OrdinalIgnoreCase))
                {
                    result = tempResult;
                }
            }

            if (result == nullptr && tempResult != firstAttempt)
            {
                // attempt failed and it was not the first attempt
                tempResult->Close();
            }
        }
    }
    while(result == nullptr && SetLOGFONT(logfontdv, i++));

    if(result == nullptr)
    {
        // No result found fallback to first attempt
        result = firstAttempt;
    }
    else if (result != firstAttempt && firstAttempt != nullptr)
    {
        // A result found that is not the same as the first attempt
        // dispose of the first attempt
        firstAttempt->Close();
        firstAttempt = nullptr;
    }

    if(result != nullptr)
    {
        Debug::Assert(!result->IsClosed, "CreateFontCached must never return a closed handle");
        Debug::Assert(!result->IsInvalid, "CreateFontCached must never return an invalide handle");
        CacheObject((interior_ptr<Byte>)&originalLogfontDv, sizeof(ENUMLOGFONTEXDV), result);
    }

    Debug::Assert(result != nullptr, "CreateFontCached must never return null");

    return result;
}

//
// Fix for bug 985195: Text with style simulation differs from Avalon rendering.
//
// Cause: Avalon will always simulate the style stimulation, while GDI font creation may create
// a styled font and not simulate. Most obvious case is simulating italicized bold Arial:
// Avalon uses arial.ttf, while GDI uses arialbi.ttf.
//
// Fix: Force different charset to force GDI to select unstyled font and perform style simulation,
// otherwise GDI will select the non-simulated styled font.
//
// Reason this works: Styled fonts typically missing some characters in non-ANSI charsets that
// are present in unstyled font. Therefore, selecting different charset may force GDI
// to use unstyled font and perform style simulations.
//
// Risk: Possible excessive font creation if style simulation requested due to looping through
// charsets and being unable to force GDI creation of style-simulated font. However, based on
// discussion this seems to be the best method of creating style-simulated font.
//
GdiSafeHandle^ CGDIRenderTarget::CreateSimulatedStyleFont(interior_ptr<ENUMLOGFONTEXDV> logfontdv, StyleSimulations styleSimulations)
{
    GdiSafeHandle^ fontResult = nullptr;

    interior_ptr<LOGFONT> logfont = &logfontdv->elfEnumLogfontEx.elfLogFont;

    if (logfont->lfWeight == FW_BOLD && ((int)styleSimulations & (int)StyleSimulations::BoldSimulation) != 0)
    {
        // bold simulation desired; reduce GDI bold since
        // avalon simulated bold is not quite FW_BOLD.
        logfont->lfWeight = FW_SEMIBOLD;
    }

    String^ regularFaceName = nullptr;
    {
        pin_ptr<TCHAR> text = logfont->lfFaceName;
        regularFaceName = gcnew String(text);
    }

    //
    // Check if we have cached a good charset for this particular facename, weight, italic
    // combination.
    //
    FontSimulatedStyleKey^ cacheKey = gcnew FontSimulatedStyleKey(
        regularFaceName,
        logfont->lfWeight,
        logfont->lfItalic
        );

    if (m_cachedUnstyledFontCharsets->Contains(cacheKey))
    {
        BYTE cacheCharset = (BYTE)m_cachedUnstyledFontCharsets->default[cacheKey];

        // we have cached charset for this combo, create the font
        logfont->lfCharSet = cacheCharset;
        fontResult = CreateFontCached(logfontdv);
    }

    //
    // Try all charsets if cache did not contain requested font.
    //
    if (fontResult == nullptr)
    {
        // Get the style name of the unstyled font, since style names aren't standardized.
        // We use this name to check if we've succeeded in creating unstyled font.
        String^ regularStyleName = nullptr;
        {
            GdiSafeHandle^ regularFont = CreateUnstyledFont(logfontdv);

            if (regularFont == nullptr)
            {
                // we have no way of telling if we've succeeded in creating unstyled font,
                // fail.
                Debug::Assert(false, "CreateUnstyledFont failed");
            }
            else
            {
                regularStyleName = GetFontStyle(regularFont);
            }
        }

        if (regularStyleName != nullptr)
        {
            // try all charsets in effort to get plain font
            const BYTE charsets[] =
            {
                ARABIC_CHARSET,
                HEBREW_CHARSET,
                THAI_CHARSET,

                BALTIC_CHARSET,
                CHINESEBIG5_CHARSET,
                EASTEUROPE_CHARSET,
                GB2312_CHARSET,
                GREEK_CHARSET,
                HANGUL_CHARSET,
                MAC_CHARSET,
                OEM_CHARSET,
                RUSSIAN_CHARSET,
                SHIFTJIS_CHARSET,
                SYMBOL_CHARSET,
                TURKISH_CHARSET,
                VIETNAMESE_CHARSET,
                JOHAB_CHARSET,
            };

            const int charsetCount = sizeof(charsets)/sizeof(BYTE);

            for (int charsetIndex = 0; charsetIndex < charsetCount && fontResult == nullptr; charsetIndex++)
            {
                logfont->lfCharSet = charsets[charsetIndex];

                // create font using this charset
                GdiSafeHandle^ charsetFont = CreateFontCached(logfontdv);

                if (charsetFont == nullptr)
                {
                    Debug::Assert(false, "CreateFontCached failed");
                }
                else if (CheckFontFaceAndStyle(charsetFont, regularFaceName, regularStyleName))
                {
                    // we've succeeded in picking unstyled font with GDI style simulation
                    fontResult = charsetFont;

                    // cache the successful charset
                    m_cachedUnstyledFontCharsets->default[cacheKey] = logfont->lfCharSet;
                }
            }
        } // regularStyleName != nullptr
    } // fontResult == nullptr

    return fontResult;
}

GdiSafeHandle^ CGDIRenderTarget::CreateUnstyledFont(interior_ptr<ENUMLOGFONTEXDV> logfontdv)
{
    // remove styling
    ENUMLOGFONTEXDV unstylizedLogfontDv;
    ::memcpy(&unstylizedLogfontDv, logfontdv, sizeof(ENUMLOGFONTEXDV));

    LOGFONT& unstylizedLogfont = unstylizedLogfontDv.elfEnumLogfontEx.elfLogFont;

    unstylizedLogfont.lfWeight = FW_NORMAL;
    unstylizedLogfont.lfItalic = false;
    unstylizedLogfont.lfUnderline = false;
    unstylizedLogfont.lfStrikeOut = false;

    // create font using unstyled logfont info
    return CreateFontCached(&unstylizedLogfontDv);
}

String^ CGDIRenderTarget::GetFontFace(GdiSafeHandle^ font)
{
    SelectObject(font, OBJ_FONT);

    String^ face = nullptr;

    int bufferSize = CNativeMethods::GetTextFace(m_hDC, 0, nullptr);

    if (bufferSize > 0)
    {
        System::Text::StringBuilder^ faceBuilder = gcnew System::Text::StringBuilder(bufferSize);

        if (CNativeMethods::GetTextFace(m_hDC, bufferSize, faceBuilder) > 0)
        {
            face = faceBuilder->ToString();
        }
    }

    return face;
}

String^ CGDIRenderTarget::GetFontStyle(GdiSafeHandle^ font)
{
    String^ styleName = nullptr;

    //
    // Get text metrics buffer size.
    //
    // Bug 1323116: GetOutlineTextMetrics can fail with ERROR_INVALID_DATA on Simplified
    // Chinese OS with font Georgia. Currently we return nullptr on failure, which will
    // be handled gracefully, but should investigate why GDI is failing.
    //
    SelectObject(font, OBJ_FONT);
    UINT metricSize = CNativeMethods::GetOutlineTextMetrics(m_hDC, 0, nullptr);

    if (metricSize > 0)
    {
        // create buffer and retrieve text metrics
        array<Byte>^ buffer = gcnew array<Byte>(metricSize);
        interior_ptr<OUTLINETEXTMETRIC> metric = (interior_ptr<OUTLINETEXTMETRIC>)&buffer[0];

        if (CNativeMethods::GetOutlineTextMetrics(m_hDC, metricSize, buffer) > 0)
        {
            // pin and perform pointer manipulation to retrieve style name
            pin_ptr<OUTLINETEXTMETRIC> pinnedMetric(metric);

            styleName = gcnew String(
                (WCHAR*)((BYTE*)pinnedMetric + (INT_PTR)metric->otmpStyleName)
                );
        }
    }

    return styleName;
}

bool CGDIRenderTarget::CheckFontFaceAndStyle(GdiSafeHandle^ font, String^ fontFace, String^ fontStyle)
{
    bool result = true;

    {
        // Check if charset font's style is the regular font's style, which is what we want.
        String^ actualFontStyle = GetFontStyle(font);

        if (actualFontStyle == nullptr ||
            String::Compare(fontStyle, actualFontStyle, StringComparison::OrdinalIgnoreCase) != 0)
        {
            result = false;
        }
    }

    if (result)
    {
        // Make sure facename is same. Sometimes GDI will return completely different face
        // if requested font can't match requested styling.
        String^ actualFontFace = GetFontFace(font);

        if (actualFontFace == nullptr ||
            String::Compare(fontFace, actualFontFace, StringComparison::OrdinalIgnoreCase) != 0)
        {
            result = false;
        }
    }

    return result;
}

int SelectIndex(int index, int itemCount, int% nextIndex)
{
    int result = index % itemCount;
    nextIndex = index / itemCount;
    return result;
}

// used to generate a range of LOGFONTS based on index.
// this is done by setting flags on logfontdv.elfEnumLogfontEx.elfLogFont
//returns false if index falls outside the range of valid LOGFONTS
bool CGDIRenderTarget::SetLOGFONT(interior_ptr<ENUMLOGFONTEXDV> logfontdv, int index)
{
#define ARRAY_COUNT(x) (sizeof(x)/sizeof((x)[0]))
    BYTE lfCharSet[] = {
        ANSI_CHARSET,
        SYMBOL_CHARSET,
        OEM_CHARSET,

        DEFAULT_CHARSET,

        MAC_CHARSET,
        BALTIC_CHARSET,
        CHINESEBIG5_CHARSET,
        EASTEUROPE_CHARSET,
        GB2312_CHARSET,
        GREEK_CHARSET,
        HANGUL_CHARSET,
        RUSSIAN_CHARSET,
        SHIFTJIS_CHARSET,
        TURKISH_CHARSET,
        JOHAB_CHARSET,
        HEBREW_CHARSET,
        ARABIC_CHARSET,
        THAI_CHARSET
    };

    BYTE lfOutPrecision[] = { OUT_TT_PRECIS, OUT_OUTLINE_PRECIS, OUT_DEFAULT_PRECIS};

    BYTE lfPitch[] = { DEFAULT_PITCH, FIXED_PITCH, VARIABLE_PITCH };

    BYTE lfFamily[] = { FF_DONTCARE, FF_MODERN, FF_ROMAN, FF_SWISS, FF_SCRIPT, FF_DECORATIVE };

    const int maxIndex =
          ARRAY_COUNT(lfCharSet)
        * ARRAY_COUNT(lfOutPrecision)
        * ARRAY_COUNT(lfPitch)
        * ARRAY_COUNT(lfFamily);

    if(index >= 0 && index < maxIndex)
    {
        int lfFamilyIndex       = SelectIndex(index, ARRAY_COUNT(lfFamily), index);
        int lfPitchIndex        = SelectIndex(index, ARRAY_COUNT(lfPitch), index);
        int lfOutPrecisionIndex = SelectIndex(index, ARRAY_COUNT(lfOutPrecision), index);
        int lfCharSetIndex      = SelectIndex(index, ARRAY_COUNT(lfCharSet), index);

        logfontdv->elfEnumLogfontEx.elfLogFont.lfPitchAndFamily  = lfPitch[lfPitchIndex] | lfFamily[lfFamilyIndex];
        logfontdv->elfEnumLogfontEx.elfLogFont.lfOutPrecision    = lfOutPrecision[lfOutPrecisionIndex];
        logfontdv->elfEnumLogfontEx.elfLogFont.lfCharSet         = lfCharSet[lfCharSetIndex];
        return true;
    }

    return false;
#undef ARRAY_COUNT
}

/**************************************************************************
*
* Method Description:
*
*   SetTextWorldTransform set the GDI world transform. because we create
*     the font scaled to device dpi then we need to reset the scaling unit
*     in the transform into the 96.0 dpi (conventional MIL dpi unit).
*     But we still need to keep the translation in the device dpi.
*
* Return Value:
*     BOOL
*
*   Even on failure this is sure to retrieve original transform.
*
**************************************************************************/

BOOL CGDIRenderTarget::SetTextWorldTransform(XFORM% OriginalTransform)
{
    XFORM renderTransform;
    int errCode = CNativeMethods::GetWorldTransform(m_hDC, &OriginalTransform);
    Debug::Assert(errCode != 0, "GetWorldTransform failed");

    Matrix transform;
    transform.Scale(96.0 / m_nDpiX, 96.0 / m_nDpiY);
    transform.Append(m_transform);

    renderTransform.eM11 = (float) transform.M11;
    renderTransform.eM12 = (float) transform.M12;
    renderTransform.eM21 = (float) transform.M21;
    renderTransform.eM22 = (float) transform.M22;
    renderTransform.eDx  = (float) transform.OffsetX;
    renderTransform.eDy  = (float) transform.OffsetY;

    return CNativeMethods::SetWorldTransform(m_hDC, &renderTransform);
}

