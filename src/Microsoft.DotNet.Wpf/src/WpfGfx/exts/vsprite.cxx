// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------------
//

//
// File: vsprite.cxx
//---------------------------------------------------------------------------------

#include "precomp.hxx"

#define DBG_VSPRITE 0

/**************************************************************************\
* SaveBitmap
*
* Write a bitmap to file, reading the pixels from a pixel buffer in virtual
* address space.
*
\**************************************************************************/
HRESULT
SaveBitmap(
    PDEBUG_CLIENT Client,
    __in PCSTR pszFileName,
    __in const BITMAPINFOHEADER& bmih,
    ULONG64 ulpvPixels
    )
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);
    PDEBUG_DATA_SPACES Data = NULL;

    BITMAPFILEHEADER bmfh = { 'MB' };
    HANDLE hFile = NULL;
    VOID* pvScanLine = NULL;
    DWORD dwBytesWritten;
   
    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data))) goto Cleanup;    

    // 
    // First make sure we can open the file.
    //
    hFile = CreateFileA(pszFileName, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        OutCtl.Output("Couldn't open file %s, hr = %p\n", pszFileName, hr);
        goto Cleanup;
    }

    UINT uiStride = ((bmih.biWidth * bmih.biBitCount) >> 3);
    UINT uiHeight = (bmih.biHeight < 0) ? -bmih.biHeight : bmih.biHeight;

    //
    // Write the header to the file
    //
    bmfh.bfOffBits = sizeof(bmfh) + sizeof(bmih);
    bmfh.bfSize = bmfh.bfOffBits + uiHeight * uiStride;

    if (!WriteFile(hFile, &bmfh, sizeof(bmfh), &dwBytesWritten, NULL)
        || !WriteFile(hFile, &bmih, sizeof(bmih), &dwBytesWritten, NULL))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        OutCtl.Output("Couldn't write file header, hr = %p\n", hr);
        goto Cleanup;
    }
    else
    {
        //
        // Try to read each line from memory and then write it to disk.
        //
        pvScanLine = HeapAlloc(GetProcessHeap(), 0, uiStride);
        if (!pvScanLine)
        {
            OutCtl.Output("Couldn't allocate sprite memory\n");
            goto Cleanup;
        }

        ULONG64 ulpvInput = ulpvPixels;

        for (UINT i = 0; i < uiHeight; i++)
        {
            const UINT c_uNumColumns = 50;
            if (i != 0 && (i % c_uNumColumns) == 0)
            {
                OutCtl.Output(" [%ld%%]\n", 100*i/uiHeight);
            }

            if (FAILED(hr = OutCtl.GetInterrupt()))
            {
                OutCtl.Output("\n\nStop on user interrupt.\n\n");
                goto Cleanup;
            }
            
            ZeroMemory(pvScanLine, uiStride);
            ULONG ulBytesRead = 0;
            if (FAILED(Data->ReadVirtual(ulpvInput, pvScanLine, uiStride, &ulBytesRead))
                || ulBytesRead < uiStride)
            {
                OutCtl.Output("ReadVirtual failed @ 0x%p (scan %ld).\n", ulpvInput, i);
            }

            //
            // Write the scan line to disk
            //
            if (!WriteFile(hFile, pvScanLine, uiStride, &dwBytesWritten, NULL))
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                OutCtl.Output("Couldn't write scan line %d, hr = %p\n", i, hr);
            }

            OutCtl.Output(".");

            if (FAILED(hr = ULong64Add(ulpvInput, uiStride, &ulpvInput)))
            {
                goto Cleanup;
            }
        }
        OutCtl.Output("\n");
    }

Cleanup:
    if (hFile)
    {
        CloseHandle(hFile);
    }
    if (pvScanLine)
    {
        HeapFree(GetProcessHeap(), 0, pvScanLine);
    }
    ReleaseInterface(Data);

    return hr;
}

/******************************Public*Routine******************************\
* vsprite
*
* Write a the sprite of a specified window to a bitmap.
*
* Wrote it.
\**************************************************************************/
CPPMOD HRESULT CALLBACK vsprite(PDEBUG_CLIENT Client, PCSTR args)
{    
    HRESULT hr = S_OK;
    OutputControl   OutCtl(Client);

    CommandLine* pCommandLine = NULL;
    bool fShowHelp = false;
    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));
    IFC(CommandLine::CreateFromString(OutCtl, args, &pCommandLine));

    if (pCommandLine->GetCount() == 0 || (pCommandLine->GetCount() == 1 && (*pCommandLine)[0].fIsOption))
    {
        fShowHelp = true;
    }
    else
    {
        CommandLine& commandLine = *pCommandLine;

        PCSTR pszFileName = NULL;
        DEBUG_VALUE dvHwnd = { 0 };

        for (UINT i = 0; i < commandLine.GetCount(); i++)
        {
            if (commandLine[i].fIsOption)
            {
                if (commandLine[i].cchLength > 0 && commandLine[i].string[0] == 'o')
                {
                    i++;
                    pszFileName = commandLine[i].string;
                }
            }
            else
            {
                if (FAILED(hr = OutCtl.Evaluate(commandLine[i].string, DEBUG_VALUE_INT64, &dvHwnd, NULL)))
                {
                    OutCtl.Output("Could not evaluate argument %s\n", commandLine[i].string);
                    goto Cleanup;
                }                
            }
        }

        if (dvHwnd.I64 == 0)
        {
            OutCtl.Output("Must specify hwnd\n");
            fShowHelp = true;
            goto Cleanup;
        }

        if (pszFileName == NULL)
        {
            OutCtl.Output("Must specify output file\n");
            fShowHelp = true;
            goto Cleanup;
        }

        ULONG64 ulpCMilWindowContext = 0;
        ULONG64 ulhVisual = 0;
        ULONG64 ulpHANDLE_ENTRY = 0;
        ULONG64 ulpMilSlaveResource = 0;
        IFC(LookupCMilWindowContext(Client, dvHwnd.I64, &ulpCMilWindowContext));

#if DBG_VSPRITE
        OutCtl.Output("vsprite: ulpCMilWindowContext: %p\n", ulpCMilWindowContext);
#endif

        if (FAILED(ReadPointerField(Client, ulpCMilWindowContext, "dwmredir!CMilWindowContext", "m_hGdiSprite", &ulhVisual)))
        {
            OutCtl.Output("vsprite: failed to find field m_hGdiSprite, trying m_hVisual instead\n");
            IFC(ReadPointerField(Client, ulpCMilWindowContext, "dwmredir!CMilWindowContext", "m_hVisual", &ulhVisual));
        }

#if DBG_VSPRITE
        OutCtl.Output("vsprite: ulhVisual: %p\n", ulhVisual);
#endif

        IFC(ResolveHMilResource(Client, ulhVisual, 0, &ulpHANDLE_ENTRY));

#if DBG_VSPRITE
        OutCtl.Output("vsprite: ulpHANDLE_ENTRY: %p\n", ulpHANDLE_ENTRY);
#endif

        IFC(ReadPointerField(Client, ulpHANDLE_ENTRY, "milcore!CMilSlaveHandleTable::HANDLE_ENTRY", "pResource", &ulpMilSlaveResource));

#if DBG_VSPRITE
        OutCtl.Output("vsprite: ulpMilSlaveResource: %p\n", ulpMilSlaveResource);
#endif

        // NOTE: we assume the type is TYPE_GDISPRITEBITMAP, but we should probably check

        ULONG uiWidth = 0;
        ULONG uiHeight = 0;
        ULONG uiStride = 0;
        ULONG uiOffset = 0;
        ULONG milPixelFormat = 0;
        ULONG64 ulpvPixels = 0;

        IFC(ReadTypedField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_uiWidth", &uiWidth));
        IFC(ReadTypedField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_uiHeight", &uiHeight));
        IFC(ReadTypedField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_uiStride", &uiStride));
        IFC(ReadTypedField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_uiOffset", &uiOffset));
        IFC(ReadTypedField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_ePixelFormat", &milPixelFormat));
        IFC(ReadPointerField(Client, ulpMilSlaveResource, "milcore!CMilGdiSpriteBitmap", "m_pvPixels", &ulpvPixels));

#if DBG_VSPRITE
        OutCtl.Output("vsprite: %d*%d, stride: %d, pixels: %p\n", uiWidth, uiHeight, uiStride, ulpvPixels);
#endif

        BITMAPINFOHEADER bmih = { sizeof(bmih) };
        bmih.biWidth = uiWidth;
        bmih.biHeight = -static_cast<LONG>(uiHeight);
        bmih.biPlanes = 1;
        bmih.biBitCount = 32;
        bmih.biCompression = BI_RGB;
        bmih.biSizeImage = 0;
        bmih.biClrImportant = 0;
        IFC(SaveBitmap(Client, pszFileName, bmih, ulpvPixels));
    }

Cleanup:
    ReleaseInterface(Symbols);
    ReleaseInterface(Data);
    if (fShowHelp)
    {
        OutCtl.Output(
            "\n!vsprite hwnd -o file\n"
            );
    }
    return hr;
}




