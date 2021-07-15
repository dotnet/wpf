// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/////////////////////////////////////////////////////////////////////////////
// CGDIRenderTarget: Interface with AlphaFlattener in ReachFramework.Dll
/////////////////////////////////////////////////////////////////////////////
typedef struct tagMXDWEscape
{
    ULONG cbInput;
    ULONG cbOutput;
    ULONG OpCode;
} MXDWEscapeData;

enum MxdwEscapes
{
    MxdwEscape              = 4122,
    MxdwGetFileNameEscape   = 14,
    MxdwPassThruEscape      = 32
};

int CGDIRenderTarget::StartDocument(String ^ printerName, String^ jobName, String^ filename, array<Byte>^ devmode)
{
    Debug::Assert(!HasDC, "HDC already created.");
    if (printerName == nullptr)
    {
        throw gcnew ArgumentNullException("printerName");
    }
    // filename can be NULL, which corresponds to DOCINFO.lpszOutput being NULL

    if (s_oldPrivateFonts->Count != 0)
    {
        System::Threading::Monitor::Enter(s_lockObject);
        {
            __try
            {

                // The best fix would be waiting for those jobs to be completed.
                // Heuristics: Release fonts which are more than 10 minutes old.
                
                DateTime cutoffTime = DateTime::Now - TimeSpan::FromMinutes(10);

                int i = 0;

                while (i < s_oldPrivateFonts->Count)
                {
                    GdiFontResourceSafeHandle^ handle = dynamic_cast<GdiFontResourceSafeHandle^>(s_oldPrivateFonts[i]);

                    if ((handle != nullptr) && (handle->TimeStamp < cutoffTime))
                    {
                        handle->Close();
                        s_oldPrivateFonts->RemoveAt(i);
                        continue;
                    }

                    i ++;
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(s_lockObject);
            }
        }
    }

    HRESULT hr = S_OK;
    int     jobIdentifier = 0;

    m_lastDevmode = devmode;

    m_hDC = CNativeMethods::CreateDC(nullptr, printerName, nullptr, devmode);

    hr = ErrorCode(HasDC);

    if (SUCCEEDED(hr))
    {
        hr = Initialize();
    }
    
    if (SUCCEEDED(hr))
    {
        CNativeMethods::GdiDocInfoW^ DocInfo = gcnew CNativeMethods::GdiDocInfoW;

        DocInfo->cbSize  = Marshal::SizeOf(DocInfo);
        DocInfo->DocName = (jobName != nullptr) ? jobName : String::Empty;
        DocInfo->Output  = filename;
        DocInfo->DataType= nullptr;
        DocInfo->Types   = 0;

        hr = ErrorCode((jobIdentifier = CNativeMethods::StartDocW(m_hDC, DocInfo)) > 0);
    }

    ThrowOnFailure(hr);

    return jobIdentifier;
}

void CGDIRenderTarget::StartDocumentWithoutCreatingDC(String^ printerName, String^ jobName, String^ filename)
{
    HRESULT hr = S_OK;
    
    CNativeMethods::GdiDocInfoW^ DocInfo = gcnew CNativeMethods::GdiDocInfoW;

    DocInfo->cbSize  = Marshal::SizeOf(DocInfo);
    DocInfo->DocName = (jobName != nullptr) ? jobName : String::Empty;
    DocInfo->Output  = filename;
    DocInfo->DataType= nullptr;
    DocInfo->Types   = 0;

    hr = ErrorCode(CNativeMethods::StartDocW(m_hDC, DocInfo) > 0);

    ThrowOnFailure(hr);
}


void CGDIRenderTarget::EndDocument()
{
    if (!HasDC)
    {
        return;
    }

    HRESULT hr = HrEndDoc();

    if (HasDC)
    {
        m_hDC->Close();
        m_hDC = nullptr;
    }

    ThrowOnFailure(hr);
}

void CGDIRenderTarget::CreateDeviceContext(String ^ printerName, String^ jobName, array<Byte>^ devmode)
{
    Debug::Assert(!HasDC, "HDC already created.");
    if (printerName == nullptr)
    {
        throw gcnew ArgumentNullException("printerName");
    }
    // filename can be NULL, which corresponds to DOCINFO.lpszOutput being NULL
    
    HRESULT hr = S_OK;

    m_lastDevmode = devmode;

    m_hDC = CNativeMethods::CreateDC(nullptr, printerName, nullptr, devmode);
    
    if (!HasDC)
    {
        hr = Marshal::GetHRForLastWin32Error();
    }

    if (SUCCEEDED(hr))
    {
        hr = Initialize();
    }
    
    ThrowOnFailure(hr);
}

void CGDIRenderTarget::DeleteDeviceContext()
{
    if (HasDC)
    {
        m_hDC->Close();
        m_hDC = nullptr;
    }

    m_hDC = nullptr;
}

String^ CGDIRenderTarget::ExtEscGetName()
{
    HRESULT                         hr             = S_OK;
    int                             win32ErrorCode = 0;
    DWORD                           fileNameSize   = 0;
    MXDWEscapeData                  getFileName    = {0};
    String^                         mxdwFileName   = nullptr;

    getFileName.cbInput  = sizeof(getFileName.cbInput) + sizeof(getFileName.cbOutput) + sizeof(getFileName.OpCode);
    getFileName.cbOutput = sizeof(fileNameSize);
    getFileName.OpCode   = MxdwGetFileNameEscape;

    win32ErrorCode = CNativeMethods::ExtEscape(m_hDC,
                                                    MxdwEscape,
                                                    getFileName.cbInput,
                                                    (void*)&getFileName,
                                                    getFileName.cbOutput,
                                                    (void*)&fileNameSize);

    // Testing shows that if cbInput specifies too small a size the call may fail with -1 
    // but will still set fileNameSize to the number of *characters* necessary to make the call succeed
    if((win32ErrorCode == -1) || (win32ErrorCode > 0))
    {
        if(fileNameSize > 0)
        {
            fileNameSize += 2*sizeof(WCHAR);

            array<Byte>^ fileName = gcnew array<Byte>(fileNameSize);

            if(fileName != nullptr)
            {
                pin_ptr<Byte> rawFileName = &fileName[0];

                getFileName.cbInput  = sizeof(getFileName.cbInput) + sizeof(getFileName.cbOutput) + sizeof(getFileName.OpCode);
                getFileName.cbOutput = fileNameSize;
                getFileName.OpCode   = MxdwGetFileNameEscape;

                win32ErrorCode = CNativeMethods::ExtEscape(m_hDC,
                                                               MxdwEscape,
                                                               getFileName.cbInput,
                                                               (void*)&getFileName,
                                                               getFileName.cbOutput,
                                                               (void*)rawFileName);
                if(win32ErrorCode > 0)
                {
                    int sizeOfTag = sizeof(ULONG);
                    //
                    // remove the size of tag and null termination
                    //
                    fileNameSize = fileNameSize-sizeOfTag-2;
                    array<Byte>^ shortFileName = gcnew array<Byte>(fileNameSize);
                    Array::Copy( fileName, sizeOfTag, shortFileName, 0, fileNameSize);
                    System::Text::Encoding^ encoding = System::Text::Encoding::Unicode;
                    mxdwFileName = encoding->GetString(shortFileName);
                }
                else
                {
                    hr = Marshal::GetHRForLastWin32Error();
                }
            }
            else
            {
                hr = E_OUTOFMEMORY;
            }
        }
    }
    else
    {
        hr = Marshal::GetHRForLastWin32Error();
    }

    ThrowOnFailure(hr);

    return mxdwFileName;
}

bool CGDIRenderTarget::ExtEscMXDWPassThru()
{
    HRESULT                         hr             = S_OK;
    int                             win32ErrorCode = 0;
    MXDWEscapeData                  passThru       = {0};
    bool                            result         = true;

    if (SUCCEEDED(hr))
    {
        passThru.cbInput  = sizeof(passThru.cbInput) + sizeof(passThru.cbOutput) + sizeof(passThru.OpCode);
        passThru.cbOutput = 0;
        passThru.OpCode   = MxdwPassThruEscape;

        if((win32ErrorCode = CNativeMethods::ExtEscape(m_hDC,
                                                       MxdwEscape,
                                                       passThru.cbInput,
                                                       (void*)&passThru,
                                                       passThru.cbOutput,
                                                       (void*)nullptr)) > 0)
        {
            hr = S_OK;
        }
        else
        {
            hr = Marshal::GetHRForLastWin32Error();
        }

    }

    if(FAILED(hr))
    {
        result = false;
    }

    return result;
}


void CGDIRenderTarget::StartPage(array<Byte> ^devmode, int rasterizationDPI)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(!m_startPage, "EndPage already called.");
    
    HRESULT hr = HrStartPage(devmode);

    ThrowOnFailure(hr);

    m_nWidth           = CNativeMethods::GetDeviceCaps(m_hDC, HORZRES);
    m_nHeight          = CNativeMethods::GetDeviceCaps(m_hDC, VERTRES);
    m_RasterizationDPI = rasterizationDPI;

    m_startPage = true;

    PushTransform(m_DeviceTransform);
}


void CGDIRenderTarget::EndPage()
{
    if (!HasDC)
    {
        return;
    }        

    Debug::Assert(m_startPage, "StartPage has not been called yet (EndPage).");

    PopTransform();

    m_startPage = false;
    
    HRESULT hr = HrEndPage();

    ThrowOnFailure(hr);
}


void CGDIRenderTarget::PopTransform()
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (Pop).");

    if (m_state->Count <= 0)
    {
        throw gcnew InvalidOperationException();
    }

    Object^ next = m_state->Pop();  // next state
    
    m_transform = (System::Windows::Media::Matrix) next;
}


void CGDIRenderTarget::PushClip(Geometry^ clipGeometry)
{
    GeometryProxy geometry(clipGeometry);
    PushClipProxy(geometry);
}


void CGDIRenderTarget::PopClip()
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (Pop).");

    if (m_state->Count <= 0)
    {
        throw gcnew InvalidOperationException();
    }

    Object^ tos = m_state->Pop();  // next state
    
    int flag = (int) tos;

    if (flag == 1)
    {
        m_clipLevel --;

        if (m_clipLevel != 0)
        {
            int errCode = CNativeMethods::RestoreDC(m_hDC, -1);     // ROBERTAN
            Debug::Assert(errCode != 0, "RestoreDC failed.");

            ResetStates();    // Values in DC will be returned to before SaveDC states
                              // Reset them to unset value until we have a stack to track them exactly
        }
        else
        {
            int errCode = CNativeMethods::SelectClipRgn(m_hDC, NULL);   // ROBERTAN
            
            Debug::Assert(errCode != ERROR, "SelectClipRgn failed.");
        } 
    }
}


void CGDIRenderTarget::PushTransform(System::Windows::Media::Matrix transform)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (PushTransform).");

    m_state->Push(m_transform);

    m_transform = transform * m_transform;
}


void CGDIRenderTarget::DrawGeometry(Brush^ fillBrush, Pen^ pen, Brush^ strokeBrush, Geometry^ geometry)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (DrawGeometry).");

    if ((geometry == nullptr) || ((fillBrush == nullptr) && (pen == nullptr)))
    {
        return;
    }
    
    HRESULT hr = S_OK;

    GeometryProxy geometryProxy(geometry);

    Rect bounds = geometryProxy.GetBounds(pen);

    if (!IsRenderVisible(bounds))
    {
        //
        // This will also cover cases where PathGeometry contains NaN, since that'll result
        // in bounds with zero area.
        //
        // Since transformations may result in NaN coordinates, we perform the test here after
        // all geometry transformations have been carried out.
        //
        return;
    }

    if (fillBrush != nullptr)
    {
        hr = E_NOTIMPL;

        ImageBrush^ brush = dynamic_cast<ImageBrush^>(fillBrush);

        if (brush != nullptr)
        {
            hr = FillImage(geometryProxy, brush);
        }

        if (hr == E_NOTIMPL)
        {
            hr = FillPath(geometryProxy, fillBrush);
        }
    }

    if (SUCCEEDED(hr) && (pen!= nullptr) && (strokeBrush!= nullptr))
    {
        hr = StrokePath(geometryProxy, pen, strokeBrush);
    }

    ThrowOnFailure(hr);
}


void CGDIRenderTarget::DrawGlyphRun(Brush ^pBrush, GlyphRun^ glyphRun)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (DrawGlyphRun).");
    
    if ((glyphRun == nullptr) || (pBrush == nullptr))
    {
        return;
    }

    HRESULT hr = RenderTextThroughGDI(glyphRun, pBrush);

    if (hr == E_NOTIMPL)
    {
        Geometry ^ outline = glyphRun->BuildGeometry();

        if (outline != nullptr)
        {
            GeometryProxy outlineProxy(outline);
            hr = FillPath(outlineProxy, pBrush);
        }
    }

    ThrowOnFailure(hr);
}


void CGDIRenderTarget::DrawImage(BitmapSource^ source, array<Byte>^ buffer, Rect rect)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (DrawImage).");

    if ((source == nullptr) || !IsRenderVisible(rect))
    {
        return;
    }

    HRESULT hr = DrawBitmap(source, buffer, rect);

    ThrowOnFailure(hr);
}


void CGDIRenderTarget::Comment(String ^ comment)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (Comment).");

    if (comment != nullptr)
    {
        IntPtr pComment = Marshal::StringToCoTaskMemAnsi(comment);

        try
        {
            int errCode = CNativeMethods::GdiComment(m_hDC, comment->Length, (BYTE *) (pComment.ToPointer()));
            Debug::Assert(errCode != 0, "GdiComment failed");
        }
        finally
        {
            Marshal::FreeCoTaskMem(pComment);
        }
    }
}
