// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __GDIEXPORTER_RT_H__
#define __GDIEXPORTER_RT_H__

COLORREF ToCOLORREF(SolidColorBrush ^ pBrush);

bool     PenSupported(Pen ^ pPen, Matrix * pMatrix, unsigned dpi);

/*
*
* CGDIRenderTarget
*
* Internal class that serves as bridge between Avalon drawing primitive and
* legacy GDI32. Intended for printing to legacy drivers.
*
* Usage:
*   Construct with a PrintQueue object. Call StartDoc/StartPage, etc. functions.
*   Call DrawGeometry, etc. to draw.
*
* Notes:
*   Implements the ILegacyDevice interface, which is a lightweight interface
*   similar to DrawingContext, but with less functions.
*
*/

ref class CGDIRenderTarget : public CGDIDevice, public ILegacyDevice
{
// Memeber variables
private:
    bool m_startPage;       // StartPage called

public:
    virtual int StartDocument(String^ printerName, String ^ jobName, String^ filename, array<Byte>^ devmode);

    virtual void StartDocumentWithoutCreatingDC(String^ priterName, String ^ jobName, String^ filename);

    virtual void EndDocument();

    virtual void CreateDeviceContext(String ^ printerName, String^ jobName, array<Byte>^ devmode);

    virtual void DeleteDeviceContext();

    virtual String^ ExtEscGetName();

    virtual bool    ExtEscMXDWPassThru();

    virtual void StartPage(array<Byte>^ devmode, int rasterizationDPI);

    virtual void EndPage();

    virtual void PopClip();

    virtual void PopTransform();

    virtual void PushClip(Geometry^ clipGeometry);

    virtual void PushTransform(System::Windows::Media::Matrix transform);

    virtual void DrawGeometry(Brush^ brush, Pen^ pen, Brush^ strokeBrush, Geometry^ geometry);

    virtual void DrawImage(BitmapSource^ source, array<Byte>^ buffer, Rect rect);

    virtual void DrawGlyphRun(Brush ^ pBrush, GlyphRun^ glyphRun);

    virtual void Comment(String ^ comment);

protected:
    // current state data
    System::Collections::Stack^ m_state;

    int                         m_clipLevel;
    Matrix                      m_transform;
    Matrix                      m_DeviceTransform;
    int                         m_nWidth;
    int                         m_nHeight;

    // Fix for bug 985195: We try each charset when creating style-simulated font
    // in effort to force GDI to create unstyled font with style simulations. Here
    // we cache the charsets that work in creating unstyled, style-simulated font.
    //
    // Hash from FontSimulatedStyleKey^ -> BYTE charset
    ref class FontSimulatedStyleKey sealed
    {
    public:
        FontSimulatedStyleKey(String^ faceName, LONG lfWeight, BYTE lfItalic)
        {
            Debug::Assert(faceName != nullptr);

            m_faceName = faceName;
            m_lfWeight = lfWeight;
            m_lfItalic = lfItalic;
        }

    private:
        String^ m_faceName;
        LONG m_lfWeight;
        BYTE m_lfItalic;

    public:
        virtual int GetHashCode() override sealed
        {
            return m_faceName->GetHashCode() ^ m_lfWeight.GetHashCode() ^ m_lfItalic.GetHashCode();
        }

        virtual bool Equals(Object^ other) override sealed
        {
            FontSimulatedStyleKey^ o = dynamic_cast<FontSimulatedStyleKey^>(other);

            if (o == nullptr)
            {
                return false;
            }
            else
            {
                return m_faceName == o->m_faceName &&
                    m_lfWeight == o->m_lfWeight &&
                    m_lfItalic == o->m_lfItalic;
            }
        }
    };

    System::Collections::Hashtable^ m_cachedUnstyledFontCharsets;

    // Throws an exception for an HRESULT if it's a failure.
    // Special case: Throws PrintingCanceledException for ERROR_CANCELLED/ERROR_PRINT_CANCELLED.

    void ThrowOnFailure(HRESULT hr);
    
    HRESULT Initialize();

    HRESULT DrawBitmap(
        BitmapSource ^pImage, 
        array<Byte>^ buffer,
        Rect rectDest
        );

    HRESULT GetBrushScale(
        Brush ^ pFillBrush,
        double % ScaleX,
        double % ScaleY
        );

    void PushClipProxy(GeometryProxy% geometry);

    HRESULT StrokePath(
        IN GeometryProxy% geometry,
        IN Pen ^pPen,
        IN Brush ^pStrokeBrush
        );

    HRESULT FillPath(
        IN GeometryProxy% geometry,
        IN Brush ^pFillBrush
        );

    // Fills geometry with ImageBrush if possible.
    HRESULT FillImage(
        IN GeometryProxy% geometry,
        IN ImageBrush^ brush
        );

    HRESULT RasterizeBrush(
        CGDIBitmap    % bmpdata,
        Int32Rect       renderBounds,     // render bounds in device space, rounded
        Int32Rect       bounds,           // geometry bounds in device space, rounded
        Rect            geometryBounds,   // geometry bounds in local space
        Brush         ^ pFillBrush,
        bool            vertical,
        bool            horizontal,
        double          ScaleX,
        double          ScaleY
        );

    HRESULT RasterizeShape(
        GeometryProxy% geometry,
        Int32Rect % pBounds,
        Brush ^ pFillBrush
        );

    BOOL SetTextWorldTransform(
        XFORM % OriginalTransform
        );

    GdiSafeHandle^ CreateFont(
        GlyphRun ^pGlyphRun,
        double fontSize,        // in MIL units (96.0 dpi)
        double scaleY,
        [Out] Boolean% isPrivateFont
        );
    
    HRESULT RenderGlyphRun(
        GlyphRun ^pGlyphRun,
        Point translate,
        Point scale,
        Boolean isPrivateFont
        );

    HRESULT RenderTextThroughGDI(
        GlyphRun ^pGlyphRun,
        Brush ^pBrush
        );

    /// <summary>
    /// Creates a font and caches it, or retrieves an existing cached font.
    /// Returns nullptr on failure.
    /// </summary>
    GdiSafeHandle^ CreateFontCached(interior_ptr<ENUMLOGFONTEXDV> logfontdv);

    /// <summary>
    /// Attempts to create a font with simulated styles. It will loop through
    /// available charsets to try to force GDI to create simulated style font.
    /// See definition for details.
    /// </summary>
    GdiSafeHandle^ CreateSimulatedStyleFont(interior_ptr<ENUMLOGFONTEXDV> logfontdv, StyleSimulations styleSimulations);

    /// <summary>
    /// Creates an unstyled (normal weight, non italics) version of a font.
    /// Returns nullptr on failure.
    /// </summary>
    GdiSafeHandle^ CreateUnstyledFont(interior_ptr<ENUMLOGFONTEXDV> logfontdv);

    /// <summary>
    /// Gets the face name for a font, ex: "Arial", "Times New Roman".
    /// Returns nullptr on failure.
    /// </summary>
    String^ GetFontFace(GdiSafeHandle^ font);

    /// <summary>
    /// Gets the font style, ex: "Regular", "Bold".
    /// Returns nullptr on failure.
    /// </summary>
    String^ GetFontStyle(GdiSafeHandle^ font);

    /// <summary>
    /// Checks if a font has particular face and style names.
    /// </summary>
    bool CheckFontFaceAndStyle(GdiSafeHandle^ font, String^ fontFace, String^ fontStyle);

    HRESULT DrawBitmap_PassThrough(
        BitmapSource     ^ pIBitmap,
        Int32Rect        % rcDstBounds,    // pixels
        INT                nImageWidth,     // pixels
        INT                nImageHeight     // pixels
        );

    HRESULT FillLinearGradient(
        IN GeometryProxy% geometry,
        IN Brush^ brush
        );
        
private:
    /// sets some members of the ENUMLOGFONTEXDV structure to vaues computed from index
    /// Returns false if no members could be set based on index
    /// Used to generate a series of ENUMLOGFONTEXDV structures
    bool SetLOGFONT(
        interior_ptr<ENUMLOGFONTEXDV> logfontdv,
        int index
        );
};

#endif
