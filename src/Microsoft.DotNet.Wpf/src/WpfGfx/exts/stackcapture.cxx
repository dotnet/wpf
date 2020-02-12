// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:
//      Implementation for interpreting and outputting stack capture
//      instrumentation
//
//      Includes !DumpCaptures and !ListCaptures extensions
//
//  Work To Be Done:
//      * Output return address instead of unknown
//      * Mark questionable trimming with warning message
//      * Display name of failed HRESULT
//      * Skip "redundant" RRETURN captures
//      * Don't display frame address for !DumpLastCapture or see about adding
//        to DoStackCapture
//      * Filter DoStackCapture based on a range
//      * Avoid constant length buffer - especially MAX_STACK_FRAMES
//      * Use more robust conversion mechanism than struct StackCaptureFrame
//          - should be able to handle variable length frame entries.
//

#include "precomp.hxx"


const UINT MAX_SYMBOL_NAME_LENGTH = 256;
const UINT MAX_STACK_FRAMES = 256;


#define STACKCAPTUREFRAME_OFFSET_AND_SIZE(f) {              \
        static_cast<ULONG>(offsetof(StackCaptureFrame,f)),  \
        static_cast<ULONG>(sizeof(reinterpret_cast<StackCaptureFrame *>(NULL)->f))     \
    }

static const struct {
    ULONG Offset;
    ULONG Size;
} s_rgFieldLocalType[] = {
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(hrFailure),
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(dwThreadId),
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(uLineNumber),
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(rgCapturedFrame[0]),
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(rgCapturedFrame[1]),
    STACKCAPTUREFRAME_OFFSET_AND_SIZE(rgCapturedFrame[2])
};

static const size_t s_cFirstAddressField = 3;

C_ASSERT(sizeof(StackCaptureFrame) == offsetof(StackCaptureFrame,rgCapturedFrame) + 3 * sizeof(ULONG64));


//+----------------------------------------------------------------------------
//
//  Class:
//      CStackCaptureFrameConverter
//
//  Synopsis:
//      Helper class to load stack capture frames from the target's virtual
//      address space and convert them to local format. 
//
//-----------------------------------------------------------------------------

class CStackCaptureFrameConverter
{
protected:
    CStackCaptureFrameConverter(
        __in ULONG64 u64DoStackCaptureOffset
        )
    {
        m_u64DoStackCaptureOffset = u64DoStackCaptureOffset;
        m_cDoStackCaptureShift = 0;
        m_fDoStackCaptureChecked = false;

        m_pbTargetStackCapture = NULL;
        m_cbTargetStackCapture = 0;
        m_cbTargetStackCaptureElement = 0;
        m_cIndices = 0;

        m_cAddresses = 0;

        RtlZeroMemory(m_rgTargetField, sizeof(m_rgTargetField));
    }

public:
    /// <summary>
    /// Creates a new instance of the stack capture frame converter.
    /// </summary>
    static HRESULT Create(
        __inout_ecount(1) IDebugSymbols3 *pISymbols,
        __in_ecount(1) DEBUG_TYPE_ENTRY const * pStackCaptureFrameType,
        bool fTempIsPointer64Bit,
        __in ULONG64 u64DoStackCaptureOffset,
        __deref_out_ecount(1) CStackCaptureFrameConverter **ppConverter,
        __inout_ecount_opt(1) OutputControl *pOutCtl
        )
    {
        MILX_TRACE_ENTRY;
        
        HRESULT hr = S_OK;

        // Instantiate the converter
        CStackCaptureFrameConverter *pConverter = 
            new CStackCaptureFrameConverter(
                u64DoStackCaptureOffset
                );

        if (pConverter == NULL)
        {
            pOutCtl->OutErr("Failed to allocate stack capture frame converter.\n");
            IFC(E_OUTOFMEMORY);
        }

        // Extract the offsets of the four fields
        #define GetFrameFieldEntry(szFieldName, idxOffset) \
            GetFieldEntry(pISymbols, pStackCaptureFrameType, szFieldName, &pConverter->m_rgTargetField[idxOffset], pOutCtl)
        IFC(GetFrameFieldEntry("hrFailure", 0));
        IFC(GetFrameFieldEntry("dwThreadId", 1));
        IFC(GetFrameFieldEntry("uLineNumber", 2));
        IFC(GetFrameFieldEntry("rgCapturedFrame", 3));

        #if 0  // DbgEng doesn't yet accept fields with array index - reported
               // Also work around in GetFieldEntry doesn't see to work for
               //  arrays of void* -- GetTypeId "void*" fails.
        ULONG uArraySize = pConverter->m_rgTargetField[3].Size;

        IFC(GetFrameFieldEntry("rgCapturedFrame[0]", 3));
        #undef GetFrameFieldEntry

        pConverter->m_cAddresses = uArraySize / pConverter->m_rgTargetField[3].Size;

        #else
        #undef GetFrameFieldEntry

        // This code does not support WOW64.
        ULONG uTargetPointerSize = (fTempIsPointer64Bit ? sizeof(ULONG64) : sizeof(ULONG32));

        if (pConverter->m_rgTargetField[3].Size % uTargetPointerSize != 0)
        {
            if (pOutCtl)
            {
                pOutCtl->OutWarn("Warning: Calculated frame address array size (%u bytes) is not divisible by native pointer size of %u bytes -- assuming WOW64 with 32bit pointers\n",
                                 pConverter->m_rgTargetField[3].Size,
                                 uTargetPointerSize
                                 );
            }

            // Assume size is really 32bit pointer - WOW64
            uTargetPointerSize = sizeof(ULONG32);
        }

        pConverter->m_cAddresses = pConverter->m_rgTargetField[3].Size / uTargetPointerSize;

        if (pConverter->m_cAddresses < 2)
        {
            if (pOutCtl)
            {
                pOutCtl->OutWarn("Warning: Array length (%u) is calculated at less than 2.\n",
                                 pConverter->m_cAddresses
                                 );
            }
        }

        // Convert entry 3 to index 0 member (of unknown type)
        pConverter->m_rgTargetField[3].TypeId = 0;
        pConverter->m_rgTargetField[3].Size /= pConverter->m_cAddresses;

        #endif

        C_ASSERT(ARRAYSIZE(reinterpret_cast<StackCaptureFrame*>(NULL)->rgCapturedFrame) == ARRAYSIZE(pConverter->m_rgTargetField)-3);

        if (pConverter->m_cAddresses > ARRAYSIZE(reinterpret_cast<StackCaptureFrame*>(NULL)->rgCapturedFrame))
        {
            if (pOutCtl)
            {
                pOutCtl->OutWarn("extension only using %u of %u available frames\n",
                                 ARRAYSIZE(reinterpret_cast<StackCaptureFrame*>(NULL)->rgCapturedFrame),
                                 pConverter->m_cAddresses
                                 );
            }

            pConverter->m_cAddresses = ARRAYSIZE(reinterpret_cast<StackCaptureFrame*>(NULL)->rgCapturedFrame);
        }

        // Calculate offsets into the rgCapturedFrame array. 
        for (UINT i = s_cFirstAddressField + 1; i < s_cFirstAddressField + pConverter->m_cAddresses; i++)
        {
            pConverter->m_rgTargetField[i] = pConverter->m_rgTargetField[i-1];
            pConverter->m_rgTargetField[i].Offset += pConverter->m_rgTargetField[i-1].Size;
        }

        *ppConverter = pConverter;
        pConverter = NULL;

    Cleanup:
        if (FAILED(hr))
        {
            delete pConverter;
        }

        RRETURN(hr);
    }


    /// <summary>
    /// Initialize the iterator with a stack capture array obtained from the target.
    /// </summary>
    HRESULT Load(
        __in_bcount(cbTargetStackCapture) const BYTE *pbTargetStackCapture,
        size_t cbTargetStackCapture,
        size_t cbTargetStackCaptureElement
        )
    {
        m_pbTargetStackCapture = pbTargetStackCapture;
        m_cbTargetStackCapture = cbTargetStackCapture;
        m_cbTargetStackCaptureElement = cbTargetStackCaptureElement;

        m_cIndices = cbTargetStackCapture / cbTargetStackCaptureElement;

        return S_OK;
    }


    /// <summary>
    /// Loads the n-th stack capture frame from the target's virtual address space
    /// and converts it to the local format.
    /// </summary>
    HRESULT Convert(
        __in_ecount(1) OutputControl* pOutCtl,
        __in_ecount(1) IDebugSymbols3* pISymbols,
        size_t index,
        __out_ecount(1) StackCaptureFrame *pStackCaptureFrame
        )
    {
        HRESULT hr = S_OK;

        if (index >= m_cIndices)
        {
            IFC(E_INVALIDARG);
        }
        
        const BYTE *pcbTarget = 
            m_pbTargetStackCapture + index * m_cbTargetStackCaptureElement;

        BYTE* pcbLocal = 
            reinterpret_cast<BYTE*>(pStackCaptureFrame);

        ULONG64 u64FirstCapturedFrameOffset = 0;

        // Ensure the local frame is cleared -- otherwise
        //  1) conversion from 32-bit targets will leave the high dword of the
        //     pointers uninitialized and
        //  2) unfilled frames would be uninitialized.
        RtlZeroMemory(pcbLocal, sizeof(StackCaptureFrame));

        for (UINT i = 0; i < s_cFirstAddressField; i++)
        {
            RtlCopyMemory(
                pcbLocal + s_rgFieldLocalType[i].Offset,
                pcbTarget + m_rgTargetField[i].Offset,
                min(s_rgFieldLocalType[i].Size, m_rgTargetField[i].Size)
                );  
        }

        RtlCopyMemory(
            &u64FirstCapturedFrameOffset,
            pcbTarget + m_rgTargetField[s_cFirstAddressField].Offset,
            min(sizeof(u64FirstCapturedFrameOffset), m_rgTargetField[s_cFirstAddressField].Size)
            );

        // Get rid of DoStackCapture symbols.
        if (!m_fDoStackCaptureChecked               // check only once per conversion
            && u64FirstCapturedFrameOffset != NULL) // check only meaningful captures
        {
            bool fIsDoStackCapture = false;

            if (SUCCEEDED(IsDoStackCapture(
                              pOutCtl,
                              pISymbols,
                              u64FirstCapturedFrameOffset,
                              &fIsDoStackCapture
                              )))
            {
                if (fIsDoStackCapture)
                {
                    m_cDoStackCaptureShift = 1;
                    m_cAddresses--;
                }

                m_fDoStackCaptureChecked = true;
            }
        }

        for (UINT i = s_cFirstAddressField; i < s_cFirstAddressField + m_cAddresses; i++)
        {
            RtlCopyMemory(
                pcbLocal + s_rgFieldLocalType[i].Offset,
                pcbTarget + m_rgTargetField[i + m_cDoStackCaptureShift].Offset,
                min(s_rgFieldLocalType[i].Size, m_rgTargetField[i + m_cDoStackCaptureShift].Size)
                );          
        }

    Cleanup:    
        RRETURN(hr);
    }

private:
    HRESULT IsDoStackCapture(
        __in_ecount(1) OutputControl* pOutCtl,
        __in_ecount(1) IDebugSymbols3* pISymbols,
        __in ULONG64 u64CapturedFrameSymbol,
        __out_ecount(1) bool* pfIsDoStackCapture
        ) const
    {
        HRESULT hr = S_OK;

        char szName[MAX_SYMBOL_NAME_LENGTH];
        ULONG64 u64NameDisplacement = 0;
        bool fIsDoStackCapture = false;

        // Obtain name of first frame
        IFC(GetNameByOffset(
                pISymbols,
                u64CapturedFrameSymbol,
                sizeof(szName),
                szName,
                &u64NameDisplacement,
                pOutCtl
                ));
        
        // If the first frame is 'DoStackCapture', skip and dump the next frame
        if ((u64CapturedFrameSymbol - u64NameDisplacement) == m_u64DoStackCaptureOffset)
        {
            fIsDoStackCapture = true;
        }
        else if (strstr(szName, "!DoStackCapture") != NULL)
        {
            pOutCtl->OutVerb("IsDoStackCapture assuming the offset is DoStackCapture basing on symbol name");
            fIsDoStackCapture = true;
        }

        *pfIsDoStackCapture = fIsDoStackCapture;

    Cleanup:
        RRETURN(hr);
    }

private:
    ULONG64 m_u64DoStackCaptureOffset;
    size_t m_cDoStackCaptureShift;
    bool m_fDoStackCaptureChecked;

    const BYTE *m_pbTargetStackCapture;
    size_t m_cbTargetStackCapture;
    size_t m_cbTargetStackCaptureElement;
    size_t m_cIndices;

    bool m_fIsPointer64Bit;

    UINT m_cAddresses;

    DEBUG_FIELD_ENTRY m_rgTargetField[ARRAYSIZE(s_rgFieldLocalType)];
};


class CStackCaptureData
{
protected:
    CStackCaptureData(
        )
    : m_uCurrentStackCaptureIndex(0),
      m_pbTargetStackCapture(NULL),
      m_cbTargetStackCapture(0),
      m_uNumberOfEntries(0),
      m_pCaptureConverter(NULL)
    {
    }

public:
    static HRESULT Create(
        __in_ecount(1) IDebugDataSpaces* pIData,
        __inout_ecount(1) IDebugSymbols3 *pISymbols,
        __in PCSTR szModuleName,
        __inout_ecount(1) OutputControl *pOutCtl,
        __deref_out_ecount(1) CStackCaptureData **ppCaptureData
        )
    {
        HRESULT hr = S_OK;

        // Instantiate the data set
        CStackCaptureData *pCaptureData = new CStackCaptureData();
        if (pCaptureData == NULL)
        {
            IFC(E_OUTOFMEMORY);
        }

        IFC(pCaptureData->Init(
            pIData,
            pISymbols,
            szModuleName,
            pOutCtl
            ));

        *ppCaptureData = pCaptureData;
        pCaptureData = NULL;

    Cleanup:
        delete pCaptureData;

        RRETURN(hr);
    }

    ~CStackCaptureData()
    {
        delete [] m_pbTargetStackCapture;
        delete m_pCaptureConverter;
    }

    UINT CurrentStackCaptureIndex() const { return m_uCurrentStackCaptureIndex; }
    ULONG NumberOfEntries() const { return m_uNumberOfEntries; }

    __out_ecount(1) CStackCaptureFrameConverter * Converter() const
    {
        return m_pCaptureConverter;
    }

protected:

    HRESULT Init(
        __in_ecount(1) IDebugDataSpaces* pIData,
        __inout_ecount(1) IDebugSymbols3 *pISymbols,
        __in PCSTR szModuleName,
        __inout_ecount(1) OutputControl *pOutCtl
        );

protected:

    UINT m_uCurrentStackCaptureIndex;

    __field_bcount(m_cbTargetStackCapture) BYTE *m_pbTargetStackCapture;
    ULONG m_cbTargetStackCapture;

    ULONG m_uNumberOfEntries;

    CStackCaptureFrameConverter *m_pCaptureConverter;
};



//+----------------------------------------------------------------------------
//
//  Structure:
//      CaptureCollectionData
//
//  Synopsis:
//      Basic information idetifying a capture collection and some status about
//      how much of it has been processed.  Use of processing status is caller
//      defined.
//
//-----------------------------------------------------------------------------

struct CaptureCollectionData
{
    DWORD   ThreadId;
    HRESULT hrFailure;
    UINT    UnprocessedIndex;
    UINT    ProcessedIndex;
};


//+----------------------------------------------------------------------------
//
//  Class:
//      CStackCaptureCollectionList
//
//  Synopsis:
//      List of unique Stack Capture Collections.
//
//-----------------------------------------------------------------------------

class CStackCaptureCollectionList
{
private:

    struct CollectionListItem :
        public LIST_ENTRY,
        public CaptureCollectionData
    {
    };

public:
    CStackCaptureCollectionList()
    {
        InitializeListHead(&m_Head);
    }

    ~CStackCaptureCollectionList()
    {
        PLIST_ENTRY pEntry = m_Head.Flink;

        while (pEntry != &m_Head)
        {
            PLIST_ENTRY pToDelete = pEntry;
            pEntry = pEntry->Flink;
            delete pToDelete;
        }
    }

    BOOL IsEmpty()
    {
        return IsListEmpty(&m_Head);
    }

    HRESULT Append(
        __in_ecount(1) StackCaptureFrame const *pFrame,
        UINT uIndex
        )
    {
        HRESULT hr = S_OK;

        if (!Find(pFrame, NULL))
        {
            CollectionListItem *pCollection = new CollectionListItem;
            if (pCollection == NULL)
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                pCollection->ThreadId = pFrame->dwThreadId;
                pCollection->hrFailure = pFrame->hrFailure;
                pCollection->UnprocessedIndex = uIndex;
                pCollection->ProcessedIndex = 0;
                InsertTailList(&m_Head, pCollection);
            }
        }

        RRETURN(hr);
    }

    bool SetProccessedIndex(
        __in_ecount(1) StackCaptureFrame const *pFrame,
        UINT uIndex
        )
    {
        CaptureCollectionData *pCollection;
        bool fSuccess = Find(pFrame, &pCollection);

        if (fSuccess)
        {
            pCollection->ProcessedIndex = uIndex;
        }

        return fSuccess;
    }

    bool Pop(
        __out_ecount(1) CaptureCollectionData * const pCollection
        )
    {
        PLIST_ENTRY pEntry = RemoveHeadList(&m_Head);

        bool fFoundEntry = (pEntry != &m_Head);

        if (fFoundEntry)
        {
            *pCollection = *static_cast<CollectionListItem *>(pEntry);
            delete pEntry;
        }

        return fFoundEntry;
    }

private:

    bool Find(
        __in_ecount(1) StackCaptureFrame const *pFrame,
        __deref_out_ecount_opt(1) CaptureCollectionData ** const ppCollection
        )
    {
        bool fFound = false;

        for (PLIST_ENTRY pEntry = m_Head.Blink;
              pEntry != &m_Head;
              pEntry = pEntry->Blink
              )
        {
            CaptureCollectionData *pCollection =
                static_cast<CollectionListItem *>(pEntry);

            if (pCollection->ThreadId == pFrame->dwThreadId)
            {
                if (pCollection->hrFailure == pFrame->hrFailure)
                {
                    if (ppCollection)
                    {
                        *ppCollection = pCollection;
                    }
                    fFound = true;
                }

                break;
            }
        }

        return fFound;
    }


protected:
    LIST_ENTRY m_Head;
};


//+----------------------------------------------------------------------------
//
//  Class:      CStackCaptureIterator
//
//  Synopsis:   The stack capture circular buffer iterator.
//
//-----------------------------------------------------------------------------

class CStackCaptureIterator
{
private:

protected:
    CStackCaptureIterator(
        __in_ecount(1) CStackCaptureData const * pData, 
        UINT uStartIndex,
        __in_ecount(1) DEBUG_VALUE const &ThreadIdFilter,
        __in_ecount(1) DEBUG_VALUE const &SkipUntilHRESULTFilter
        )
      : m_pConverter(pData->Converter()),
        m_uCircularListLength(pData->NumberOfEntries()),
        m_uCircularListHeadIndex(pData->CurrentStackCaptureIndex()),
        m_ThreadIdFilter(ThreadIdFilter),
        m_SkipUntilHRESULTFilter(SkipUntilHRESULTFilter),
        m_fHRESULTMatchFound(SkipUntilHRESULTFilter.Type != DEBUG_VALUE_INT32)
    {
        if (m_uCircularListHeadIndex >= m_uCircularListLength)
        {
            // Head index is beyond list length.  Treat as an empty list.
            m_uCurrentIndex = m_uCircularListLength;
        }
        else
        {
            // Initialize current index ensuring it is with in list length.
            m_uCurrentIndex = uStartIndex % m_uCircularListLength;
        }
    }

public:
    // Creates a new stack capture iterator
    static HRESULT Create(
        __in_ecount(1) CStackCaptureData const * pData,
        UINT uStartIndex,
        __in_ecount(1) DEBUG_VALUE const &ThreadIdFilter,
        __in_ecount(1) DEBUG_VALUE const &SkipUntilHRESULTFilter,
        __deref_out_ecount(1) CStackCaptureIterator **ppIterator
        )
    {
        MILX_TRACE_ENTRY;

        HRESULT hr = S_OK;

        // Create the stack capture iterator
        CStackCaptureIterator* pIterator = 
            new CStackCaptureIterator(
                pData,
                uStartIndex,
                ThreadIdFilter,
                SkipUntilHRESULTFilter
                );
        
        if (pIterator == NULL)
        {
            IFC(E_OUTOFMEMORY);
        }

        *ppIterator = pIterator;
        pIterator = NULL;       

    Cleanup:
        if (FAILED(hr))
        {
            delete pIterator;
        }

        RRETURN(hr);
    }

    // Walks the stack capture frames circular buffer
    __success(return == S_OK) HRESULT GetNextFrame(
        __in_ecount(1) OutputControl* pOutCtl,
        __in_ecount(1) IDebugSymbols3* pISymbols,
        __out_ecount(1) StackCaptureFrame* pNextFrame,
        __out_ecount(1) UINT *puIndex
        )
    {
        HRESULT hr = S_OK;

        for (;;)
        {
            if (m_uCurrentIndex >= m_uCircularListLength)
            {
                hr = S_FALSE;
                break;
            }


            // Equivalent to
            //  (m_uCapturesHeadIndex - m_uCurrentIndex) >= 0 ?
            //      (m_uCapturesHeadIndex - m_uCurrentIndex) :
            //      (m_uCapturesHeadIndex - m_uCurrentIndex + m_uCircularListLength)

            UINT idx = (m_uCircularListHeadIndex - m_uCurrentIndex + m_uCircularListLength) % m_uCircularListLength;

            IFC(m_pConverter->Convert(
                    pOutCtl,
                    pISymbols,
                    idx, 
                    pNextFrame
                    ));

            if (pNextFrame->hrFailure == S_OK)
            {
                // We've reached the end of the saved captures     
                hr = S_FALSE;
                break;
            }


            // Set index of capture.
            *puIndex = m_uCurrentIndex;

            m_uCurrentIndex++;

            //
            // If thread filter is enabled AND
            //    thread filter is NOT looking for first thread AND
            //    thread filter matches,
            // then continue searching.
            //
            if (   (m_ThreadIdFilter.Type == DEBUG_VALUE_INT32)
                && (m_ThreadIdFilter.I32 != 0)
                && (pNextFrame->dwThreadId != m_ThreadIdFilter.I32))
            {
                continue;
            }

            //
            // If "Skip until first HRESULT match" filter is enabled
            // and there is not a match then continue searching.
            //
            if (   (!m_fHRESULTMatchFound)
                && (pNextFrame->hrFailure != static_cast<HRESULT>(m_SkipUntilHRESULTFilter.I32)))
            {
                continue;
            }

            //
            // A match has been found.
            //

            //
            // If "Skip until first HRESULT match" filter is enabled and
            // now first match has been found, then disable the filter.
            //
            if (!m_fHRESULTMatchFound)
            {
                m_fHRESULTMatchFound = true;
            }

            //
            // If thread filter is looking for first thread, then
            // remember this thread id.
            //
            if (   (m_ThreadIdFilter.Type == DEBUG_VALUE_INT32)
                && (m_ThreadIdFilter.I32 == 0))
            {
                m_ThreadIdFilter.I32 = pNextFrame->dwThreadId;
            }

            hr = S_OK;
            break;
        }

    Cleanup:
        RRETURN(hr);
    }

    void RollbackOneFrame()
    {
        if (m_uCurrentIndex > 0)
        {
            m_uCurrentIndex--;
        }
    }

private:
    // Declare, but don't define copy operator. Suppress warning C4512
    CStackCaptureIterator & operator=( CStackCaptureIterator const &);

private: 
    // Stack frame converter
    CStackCaptureFrameConverter * const m_pConverter;

    UINT const m_uCircularListLength;
    UINT const m_uCircularListHeadIndex;
    __field_range(m_uCircularListLength,<=) UINT m_uCurrentIndex;

    // When enabled (Type == DEBUG_VALUE_INT32), captures will be skipped
    // unless Thread Id matches.  A filter Thread Id of 0 will match the first
    // capture's Thread Id when all other filterss are satisfied.
    DEBUG_VALUE m_ThreadIdFilter;

    // When enabled (Type == DEBUG_VALUE_INT32), captures will be skipped until
    // first instance is found which has a matching HRESULT.  Subsequent calls
    // to GetNextFrame will not filter based on HRESULT so that caller can make
    // more advanced decisions about when to stop iterating.
    DEBUG_VALUE const m_SkipUntilHRESULTFilter;
    bool m_fHRESULTMatchFound;
};


//+----------------------------------------------------------------------------
//
//  Function:
//      DumpStackCaptureFrame
//
//  Synopsis:
//      Prints a line of the stack capture dump.
//
//-----------------------------------------------------------------------------

HRESULT DumpStackCaptureFrame(
    __in_ecount(1) OutputControl* pOutCtl,
    __in_ecount(1) IDebugSymbols3* pISymbols,
    DWORD Flags,
    ULONG64 u64CaptureSymbol,
    ULONG uCaptureLine
    )
{
    MILX_TRACE_ENTRY;

    HRESULT hr = S_OK;

    //
    // Obtain symbolic name of address
    //

    char szName[256] = "<unknown>!<unknown>";
    ULONG64 u64NameDisplacement = 0;

    IGNORE_HR(GetNameByOffset(
            pISymbols,
            u64CaptureSymbol,
            sizeof(szName),
            szName,
            &u64NameDisplacement,
            pOutCtl
            ));

    //
    // Obtain line information
    //

    char szFile[MAX_PATH] = "<unknown file>";
    char *pszFile = szFile;

    IGNORE_HR(pISymbols->GetLineByOffset(
        u64CaptureSymbol,
        // Get line unless valid one is provided
        (uCaptureLine != 0) ? NULL : &uCaptureLine,
        szFile,
        sizeof(szFile),
        NULL,
        NULL
        ));

    //
    // Line information is crucial to capture information.  If line information
    // is not desired then trim source path to just the filename.
    //
    // Default value, <unknown file>, is not affected.
    //

    if ( !(Flags & DEBUG_STACK_SOURCE_LINE) )
    {
        // Search for last path delimiter
        PSTR pszDelim = strrchr(pszFile, '\\');

        if (pszDelim)
        {
            // Remove prior path
            pszFile = pszDelim + 1;

            // Check for NULL file -- restore path if no file
            if (*pszFile == 0)
            {
                pszFile = szFile;
            }
        }
    }


    //
    // Print frame address, return address, and capture address
    //
    // Future Consideration:   We often know return address - print it
    //

    static const char szUnknownAddress[] = "????????`????????";
    int AddressPrintSize = (pOutCtl->IsPointer64Bit() == S_OK) ? 17 : 8;

    int FrameAddressPrintSize = AddressPrintSize;
    int ReturnAddressPrintSize = AddressPrintSize;

    if (   !(Flags & DEBUG_STACK_FRAME_ADDRESSES)
        || (Flags & DEBUG_STACK_FRAME_ADDRESSES_RA_ONLY))
    {
        FrameAddressPrintSize = 0;
    }

    if ( !(Flags & (DEBUG_STACK_FRAME_ADDRESSES | DEBUG_STACK_FRAME_ADDRESSES_RA_ONLY)))
    {
        ReturnAddressPrintSize = 0;
    }

    IFC(pOutCtl->Output(
            "%.*s %.*s %s+0x%I64x [%s @ %u]\n",
            FrameAddressPrintSize, szUnknownAddress,
            ReturnAddressPrintSize, szUnknownAddress,
            szName,
            u64NameDisplacement,
            pszFile,
            uCaptureLine
            ));

Cleanup:
    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Class:      CCapturedStack
//
//  Synopsis:   Represents the stack capture for the last failure.
//
//-----------------------------------------------------------------------------

class CCapturedStack
{
protected:
    CCapturedStack()
    {
        Clear();
    }

public:
    static HRESULT Create(
        __deref_out_ecount(1) CCapturedStack **ppCapturedStack
        )
    {
        MILX_TRACE_ENTRY;

        HRESULT hr = S_OK;

        *ppCapturedStack = new CCapturedStack();

        if (*ppCapturedStack == NULL)
        {
            IFC(E_OUTOFMEMORY);
        }

    Cleanup:
        RRETURN(hr);
    }


    bool IsEmpty()
    {
        return (m_uStackTop == 0);
    }

    UINT StartIndexOfCollection()
    {
        return m_uIndexOfCaptureStart;
    }

    HRESULT Populate(
        __in_ecount(1) OutputControl* pOutCtl, 
        __in_ecount(1) IDebugSymbols3* pISymbols,
        __inout_ecount(1) CStackCaptureIterator* pIterator
        )
    {
        MILX_TRACE_ENTRY;

        HRESULT hr = S_OK;

        StackCaptureFrame firstFrame = { 0 };
        bool fFirstFrameSet = false;

        Clear();

        for (;;)
        {
            StackCaptureFrame currentFrame;
            UINT uIndexOfFrame;

            IFC(pIterator->GetNextFrame(
                    pOutCtl,
                    pISymbols,
                    &currentFrame,
                    &uIndexOfFrame
                    ));        

            if (hr == S_FALSE)
            {
                break;
            }

            if (!fFirstFrameSet)
            {
                firstFrame = currentFrame;
                Push(&firstFrame, uIndexOfFrame);
                fFirstFrameSet = true;
                continue;
            }

            // Restrict to matching threads.
            if (currentFrame.dwThreadId == firstFrame.dwThreadId)
            {

                if (   // Restrict to matching failure codes.
                       (currentFrame.hrFailure == firstFrame.hrFailure)
                       // Treat identical captures as a new instance.
                    && !RtlEqualMemory(&currentFrame, &firstFrame, sizeof(firstFrame)))
                {
                    Push(&currentFrame, uIndexOfFrame);
                }
                else
                {
                    // We've found the end of this failure.

                    // Rollback iterator one frame since we are not capturing
                    // the frame.
                    pIterator->RollbackOneFrame();

                    // Don't capture any more
                    break;
                }                  
            }
        }

    Cleanup:            
        RRETURN(hr);
    }    


    HRESULT Dump(
        __inout_ecount(1) OutputControl *pOutCtl, 
        __inout_ecount(1) IDebugSymbols3 *pISymbols,
        DWORD Flags,
        __out_ecount_opt(1) StackCaptureFrame *pLastCapturedFrame
        )
    {
        MILX_TRACE_ENTRY;

        HRESULT hr = S_OK;

        if (m_uStackTop == 0)
        {
            IFC(pOutCtl->Output(
                    "Captured stack associated with the selected filters is empty.\n\n"
                    ));
        }
        else
        {
            StackCaptureFrame const *pFrame = NULL;

            IFC(pOutCtl->Output(
                    "Captured stack.  HRESULT: %x.  ThreadID: %x.  Captured frame count: %d.\n\n",
                    m_rgFrames[0].hrFailure,
                    m_rgFrames[0].dwThreadId,
                    m_uStackTop
                    ));
            
            for (UINT i = m_uStackTop; i > 0; i--)
            {
                pFrame = &m_rgFrames[i-1];

                IFC(DumpStackCaptureFrame(
                        pOutCtl,
                        pISymbols,
                        Flags,
                        pFrame->rgCapturedFrame[0],
                        pFrame->uLineNumber
                        ));

                if ((i == 1)
                    ? (pLastCapturedFrame == NULL)
                    : (   (pFrame->rgCapturedFrame[1] != m_rgFrames[i-2].rgCapturedFrame[0])
                       && (pFrame->rgCapturedFrame[1] != m_rgFrames[i-2].rgCapturedFrame[1])
                          // frame 2 is valid and doesn't match frame 1 of "next" capture
                       && (   (pFrame->rgCapturedFrame[2] != 0)
                           && (pFrame->rgCapturedFrame[2] != m_rgFrames[i-2].rgCapturedFrame[1]))
                      )
                   )
                {
                    IFC(DumpStackCaptureFrame(
                            pOutCtl,
                            pISymbols,
                            Flags,
                            pFrame->rgCapturedFrame[1],
                            0
                            ));

                    if (pFrame->rgCapturedFrame[2])
                    {
                        IFC(DumpStackCaptureFrame(
                                pOutCtl,
                                pISymbols,
                                Flags,
                                pFrame->rgCapturedFrame[2],
                                0
                                ));
                    }
                }
            }
            
            if (pFrame != NULL && pLastCapturedFrame != NULL)
            {
                *pLastCapturedFrame = *pFrame;
            }
        }

    Cleanup:
        RRETURN(hr);
    }


private:

    void Clear()
    {
        m_uStackTop = 0;
        m_uIndexOfCaptureStart = 0;
    }

    HRESULT Push(
        __in_ecount(1) StackCaptureFrame* pFrame,
        UINT uIndexOfFrame
        )
    {
        HRESULT hr = S_OK;
   
        m_uIndexOfCaptureStart = uIndexOfFrame;

        if (m_uStackTop < ARRAYSIZE(m_rgFrames))
        {
            RtlCopyMemory(&m_rgFrames[m_uStackTop++], pFrame, sizeof(StackCaptureFrame));
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;            
    }
    

private:

    UINT m_uStackTop;    
    UINT m_uIndexOfCaptureStart;

    StackCaptureFrame m_rgFrames[MAX_STACK_FRAMES];
};


//+----------------------------------------------------------------------------
//
//  Function:  GetStackCaptureSymbols
//
//  Synopsis:  Retrieves the symbols for the stack capture globals and function.
//
//-----------------------------------------------------------------------------

HRESULT GetStackCaptureSymbols(
    __inout_ecount(1) IDebugSymbols3 *pISymbols,
    __in PCSTR szModuleName,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pStackCaptureFramesSymbolEntry,
    __out_ecount(1) PDEBUG_TYPE_ENTRY   pStackCaptureFrameTypeEntry,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pCurrentStackCaptureIndexSymbolEntry,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pDoStackCaptureSymbolEntry,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    )
{
    MILX_TRACE_ENTRY;

    HRESULT hr = S_OK;

    char szStackCaptureFrames[64];
    char szCurrentStackCaptureIndex[64];
    char szDoStackCapture[64];

    // Concatenate names of stack capture variables
    IFC(StringCchPrintfA(ARRAY_COMMA_ELEM_COUNT(szStackCaptureFrames), "%s!g_StackCaptureFrames", szModuleName));
    IFC(StringCchPrintfA(ARRAY_COMMA_ELEM_COUNT(szCurrentStackCaptureIndex), "%s!g_nCurrentStackCaptureIndex", szModuleName));
    IFC(StringCchPrintfA(ARRAY_COMMA_ELEM_COUNT(szDoStackCapture), "%s!DoStackCapture", szModuleName));

    // Lookup info of g_StackCaptureFrames
    IFC(GetFirstSymbolEntry(
            pISymbols,
            szStackCaptureFrames, 
            pStackCaptureFramesSymbolEntry,
            pOutCtl
            ));

    #if 0  // Bug in dbgeng currently prevents this from working - no fix

    // Lookup info of g_StackCaptureFrames[0] which has the type info for a
    // single capture element
    pStackCaptureFrameTypeEntry->ModuleBase = pStackCaptureFramesSymbolEntry->ModuleBase;
    IFC(GetTypeId(
            pISymbols,
            pStackCaptureFrameTypeEntry->ModuleBase,
            "g_StackCaptureFrames[0]", 
            &pStackCaptureFrameTypeEntry->TypeId,
            pOutCtl
            ));

    #elif 0  // Bug in dbgeng currently prevents this from working - fix made, but has yet to propagate.

    // Lookup info of g_StackCaptureFrames[0] which has the type info for a
    // single capture element
    IFC(StringCchCatA(ARRAY_COMMA_ELEM_COUNT(szStackCaptureFrames), "[0]"));
    IFC(pISymbols->GetSymbolTypeId(
            szStackCaptureFrames,
            &pStackCaptureFrameTypeEntry->TypeId,
            &pStackCaptureFrameTypeEntry->ModuleBase
            ));
    #else

    char szStackCaptureFrameType[128];

    // Lookup type name of g_StackCaptureFrames which has the type name of
    // single capture element, but with [] appended.
    IFC(pISymbols->GetTypeName(
        pStackCaptureFramesSymbolEntry->ModuleBase,
        pStackCaptureFramesSymbolEntry->TypeId,
        ARRAY_COMMA_ELEM_COUNT(szStackCaptureFrameType),
        NULL
        ));

    //
    // Remove [] from szStackCaptureFrameType
    //
    {
        PSTR pszArrayLen = strchr(szStackCaptureFrameType, '[');
        if (!pszArrayLen)
        {
            if (pOutCtl)
            {
                pOutCtl->OutErr("Array dimension not found in %s.  (Perhaps name buffer is too small.)\n",
                                szStackCaptureFrameType);
            }
            IFC(E_FAIL);
        }

        pszArrayLen[0] = 0;
    }

    // Lookup info of single g_StackCaptureFrames element
    pStackCaptureFrameTypeEntry->ModuleBase = pStackCaptureFramesSymbolEntry->ModuleBase;
    IFC(pISymbols->GetTypeId(
        pStackCaptureFrameTypeEntry->ModuleBase,
        szStackCaptureFrameType,
        &pStackCaptureFrameTypeEntry->TypeId
        ));

    #endif

    pStackCaptureFrameTypeEntry->Flags = 0;

    IFC(pISymbols->GetTypeSize(
        pStackCaptureFrameTypeEntry->ModuleBase,
        pStackCaptureFrameTypeEntry->TypeId,
        &pStackCaptureFrameTypeEntry->Size
        ));


    // Lookup info of g_nCurrentStackCaptureIndex
    IFC(GetFirstSymbolEntry(
            pISymbols,        
            szCurrentStackCaptureIndex, 
            pCurrentStackCaptureIndexSymbolEntry,
            pOutCtl
            ));    

    // Lookup info of DoStackCapture
    IFC(GetFirstSymbolEntry(
            pISymbols,        
            szDoStackCapture,
            pDoStackCaptureSymbolEntry,
            pOutCtl
            ));    

Cleanup:
    RRETURN(hr);
}

//+----------------------------------------------------------------------------
//
//  Function:  GetStackCaptureValuesAndSymbols
//
//  Synopsis:  Retrieves the symbols and values for the stack capture globals
//             and function.
//
//-----------------------------------------------------------------------------

HRESULT GetStackCaptureValuesAndSymbols(
    __in_ecount(1) IDebugDataSpaces* pIData,
    __inout_ecount(1) IDebugSymbols3 *pISymbols,
    __in PCSTR szModuleName,
    __out_ecount(1) PDEBUG_TYPE_ENTRY   pStackCaptureFrameTypeEntry,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pDoStackCaptureSymbolEntry,
    __out_ecount(1) PUINT puCurrentStackCaptureIndex,
    __deref_out_ecount_full(*pcbTargetStackCapture) PBYTE * ppbTargetStackCapture,
    __out_ecount(1) PULONG pcbTargetStackCapture,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    )
{
    HRESULT hr;

    DEBUG_SYMBOL_ENTRY StackCaptureFramesSymbolEntry;
    DEBUG_SYMBOL_ENTRY CurrentStackCaptureIndexSymbolEntry;

    BYTE *pbTargetStackCapture = NULL;
    *ppbTargetStackCapture = NULL;

    // Obtain the symbol entries for the stack capture globals and function
    IFC(GetStackCaptureSymbols(
            pISymbols,
            szModuleName,
            &StackCaptureFramesSymbolEntry,
            pStackCaptureFrameTypeEntry,
            &CurrentStackCaptureIndexSymbolEntry,
            pDoStackCaptureSymbolEntry,
            pOutCtl
            ));

    //
    // Read the last capture index
    //

    if (sizeof(*puCurrentStackCaptureIndex) != CurrentStackCaptureIndexSymbolEntry.Size)
    {
        if (pOutCtl)
        {
            pOutCtl->OutErr("Capture index has unexpected size of %u bytes instead of %u.\n",
                            CurrentStackCaptureIndexSymbolEntry.Size,
                            sizeof(*puCurrentStackCaptureIndex));
        }
        IFC(E_FAIL);
    }

    IFC(pIData->ReadVirtual(
        CurrentStackCaptureIndexSymbolEntry.Offset,
        puCurrentStackCaptureIndex,
        sizeof(*puCurrentStackCaptureIndex),
        NULL
        ));     


    //
    // Allocate a buffer to hold the stack capture frames.
    //

    pbTargetStackCapture = new BYTE[StackCaptureFramesSymbolEntry.Size];

    if (pbTargetStackCapture == NULL)
    {
        if (pOutCtl) { pOutCtl->OutErr("Failed to allocate stack capture buffer.\n"); }
        IFC(E_OUTOFMEMORY);
    }    

    //
    // Read the complete stack capture array into local buffer
    //

    IFC(pIData->ReadVirtual(
            StackCaptureFramesSymbolEntry.Offset, 
            pbTargetStackCapture, 
            StackCaptureFramesSymbolEntry.Size, 
            NULL
            ));         


    *ppbTargetStackCapture = pbTargetStackCapture;
    pbTargetStackCapture = NULL;
    *pcbTargetStackCapture = StackCaptureFramesSymbolEntry.Size;

Cleanup:
    delete [] pbTargetStackCapture;

    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Member:
//      CStackCaptureData::Init
//
//  Synopsis:
//      Initialize stack capture values and instantiate converter
//
//-----------------------------------------------------------------------------

HRESULT
CStackCaptureData::Init(
    __in_ecount(1) IDebugDataSpaces* pIData,
    __inout_ecount(1) IDebugSymbols3 *pISymbols,
    __in PCSTR szModuleName,
    __inout_ecount(1) OutputControl *pOutCtl
    )
{
    HRESULT hr;

    DEBUG_TYPE_ENTRY   StackCaptureFrameTypeEntry;
    DEBUG_SYMBOL_ENTRY DoStackCaptureSymbolEntry;

    // Obtain the symbol entries and values for the stack capture globals and
    // functions
    IFC(GetStackCaptureValuesAndSymbols(
        pIData,
        pISymbols,
        szModuleName,
        &StackCaptureFrameTypeEntry,
        &DoStackCaptureSymbolEntry,
        &m_uCurrentStackCaptureIndex,
        &m_pbTargetStackCapture,
        &m_cbTargetStackCapture,
        pOutCtl
        ));


    pOutCtl->OutVerb("Frame entry size is %u bytes.\n\n", StackCaptureFrameTypeEntry.Size);

    bool fTempIsPointer64Bit;

    // Check the size of the pointer on the target machine
    IFC(pOutCtl->IsPointer64Bit());

    fTempIsPointer64Bit = (hr == S_OK);

    // Create and initialize the stack capture frame converter
    IFC(CStackCaptureFrameConverter::Create(
            pISymbols,
            &StackCaptureFrameTypeEntry,
            fTempIsPointer64Bit,
            DoStackCaptureSymbolEntry.Offset,
            &m_pCaptureConverter,
            pOutCtl
            ));

    // Load the stack capture frames into the converter
    IFC(m_pCaptureConverter->Load(
            m_pbTargetStackCapture,
            m_cbTargetStackCapture,
            StackCaptureFrameTypeEntry.Size
            ));

    pOutCtl->OutVerb("Frame entry size is %u bytes.\n\n", StackCaptureFrameTypeEntry.Size);

    m_uNumberOfEntries = m_cbTargetStackCapture / StackCaptureFrameTypeEntry.Size;

Cleanup:
    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Function:  DumpCaptureImpl
//
//  Synopsis:  Prints out the last stack capture.
//
//-----------------------------------------------------------------------------

HRESULT DumpCaptureImpl(
    __in_ecount(1) OutputControl* pOutCtl,
    __in_ecount(1) IDebugDataSpaces* pIData,
    __in_ecount(1) IDebugSymbols3* pISymbols,
    __in_ecount(1) IDebugSystemObjects4 *pISystemObjects,
    DWORD StackOutputFlags,
    __in_ecount(1) DEBUG_VALUE const &ThreadIdFilterArg,
    __in_ecount(1) DEBUG_VALUE const &HRESULTFilter,
    __in PCSTR szModuleName,
    ULONG uNumberOfCaptureCollections,
    __out_ecount_opt(1) StackCaptureFrame *pLastCapturedFrame
    )
{
    MILX_TRACE_ENTRY;

    HRESULT hr = S_OK;

    DEBUG_VALUE ThreadIdFilter = ThreadIdFilterArg;

    CStackCaptureData *pCaptureData = NULL;
    CStackCaptureIterator* pCaptureIterator = NULL;
    CCapturedStack* pCapturedStack = NULL;

    // Check if a thread id filter has been specified
    if (ThreadIdFilter.Type == DEBUG_VALUE_INT32)
    {
        if (ThreadIdFilter.I32 == -1)
        {
            // Get the last event's thread id. We'll only consider stacks 
            // captured on this thread.
            if (FAILED(pISystemObjects->GetCurrentThreadSystemId(&ThreadIdFilter.I32)))
            {
                // We're running in kernel mode and can't obtain the system thread id.

                // investigate what can be done for kernel mode 
                //  to identify the stack capture that occurred on the last event's thread. 
                IGNORE_HR(pOutCtl->OutWarn(
                    "Warning: Couldn't identify current thread id.  Just picking\n"
                    "         whatever thread is found.\n"));
                ThreadIdFilter.I32 = 0;
            }
        }

        if (ThreadIdFilter.I32 != 0)
        {
            IGNORE_HR(pOutCtl->Output("Filtering captures by Thread Id 0x%08x.\n",
                                      ThreadIdFilter.I32));
        }
    }
    else
    {
        pOutCtl->OutErr("Internal Error: DumpCaptureImpl does not support dumping all threads.\n");
        IFC(E_NOTIMPL);
    }

    if (HRESULTFilter.Type == DEBUG_VALUE_INT32)
    {
        if (   (ThreadIdFilter.Type == DEBUG_VALUE_INT32)
            && (ThreadIdFilter.I32 == 0))
        {
            IGNORE_HR(pOutCtl->Output("Filtering captures for first thread with HRESULT of 0x%08x.\n",
                                      HRESULTFilter.I32
                                      ));
        }
        else
        {
            IGNORE_HR(pOutCtl->Output("Filtering captures for %sHRESULT of 0x%08x.\n",
                                      uNumberOfCaptureCollections > 1 ? "first " : "",
                                      HRESULTFilter.I32
                                      ));
        }
    }

    // Get stack capture data
    IFC(CStackCaptureData::Create(
        pIData,
        pISymbols,
        szModuleName,
        pOutCtl,
        &pCaptureData
        ));

    // Create the stack capture iterator
    IFC(CStackCaptureIterator::Create(
            pCaptureData,
            0,
            ThreadIdFilter,
            HRESULTFilter,
            &pCaptureIterator
            ));

    IFC(CCapturedStack::Create(
            &pCapturedStack
            ));

    bool fFoundCapture = false;

    while (uNumberOfCaptureCollections--)
    {
        // Read stack capture
        IFC(pCapturedStack->Populate(
                pOutCtl,
                pISymbols,
                pCaptureIterator
                ));

        IGNORE_HR(pOutCtl->Output("\n"));

        if (fFoundCapture && pCapturedStack->IsEmpty())
        {
            IGNORE_HR(pOutCtl->Output("No more matching captures found.\n"));
            break;
        }

        if (!pCapturedStack->IsEmpty())
        {
            IGNORE_HR(pOutCtl->OutVerb("Capture Collection Starting at index %u:\n",
                                       pCapturedStack->StartIndexOfCollection()));
        }

        // Dump the stack capture
        IFC(pCapturedStack->Dump(
                pOutCtl,
                pISymbols,
                StackOutputFlags,
                pLastCapturedFrame
                ));

        fFoundCapture = true;
    }

Cleanup:
    //
    // Cleanup the dynamically allocated resources
    //

    delete pCapturedStack;
    delete pCaptureIterator;
    delete pCaptureData;

    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Extension:
//      DumpLastCapture
//
//  Synopsis:
//      Debugger extension that dumps the last stack capture for a given module
//
//-----------------------------------------------------------------------------
          
DECLARE_API(dumplastcapture)
{
    HRESULT hr = S_OK;
    
    BEGIN_API(DumpLastCapture);

    OutputControl OutCtl(Client);           
    OutputControl* pOutCtl = &OutCtl;           

    MILX_TRACE_ENTRY;

    IDebugDataSpaces* pIData = NULL;
    IDebugSymbols3 *pISymbols = NULL;
    IDebugSystemObjects4 *pISystemObjects = NULL;

    // Obtain debug library interfaces for looking up symbols, etc.    
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));
    IFC(Client->QueryInterface(__uuidof(IDebugSymbols3), (void **)&pISymbols));    
    IFC(Client->QueryInterface(__uuidof(IDebugSystemObjects4), (void **)&pISystemObjects));

    DWORD StackOutputFlags =
        DEBUG_STACK_FRAME_ADDRESSES_RA_ONLY | DEBUG_STACK_SOURCE_LINE;

    PCSTR pszModule = NULL;

    //
    // Process options
    //

    DEBUG_VALUE ThreadIdFilter;
    ThreadIdFilter.I32 = 0;  // Filter based on last capture's thread (whatever it may be.)
    ThreadIdFilter.Type = DEBUG_VALUE_INT32;

    DEBUG_VALUE HRESULTFilter;
    HRESULTFilter.Type = DEBUG_VALUE_INVALID;

    bool BadSwitch = false;
    bool ShowUsage = false;

    while (!BadSwitch)
    {
        while (isspace(*args)) args++;

        if (*args != '-') break;

        args++;
        BadSwitch = (*args == '\0' || isspace(*args));

        while (*args != '\0' && !isspace(*args))
        {
            switch (*args)
            {
            case 'L': StackOutputFlags &= ~DEBUG_STACK_SOURCE_LINE; break;
            case 't': ThreadIdFilter.I32 = (ULONG)-1 /* current thread */; break;
            case '?': ShowUsage = true; break;
            default:
                IGNORE_HR(OutCtl.OutErr("Error: Unknown option at '%s'\n", args));
                BadSwitch = true;
                break;
            }

            if (BadSwitch) break;
            args++;
        }
    }

    if (!BadSwitch && !ShowUsage)
    {
        // Make sure remaining argument could be a module when base module is
        // not properly initialized/set.
        pszModule = args;

        if (!*pszModule)
        {
            if (!Type_Module.Name[0])
            {
                IGNORE_HR(OutCtl.OutErr("Error: Missing module name (base module not set)\n"));
                BadSwitch = true;
            }
            else
            {
                // Use base module as default
                pszModule = Type_Module.Name;
            }
        }
    }

    IGNORE_HR(OutCtl.OutWarn(" ** Warning - obsolete - use dumpcaptures **\n"));

    if (BadSwitch || ShowUsage)
    {
        IGNORE_HR(OutCtl.Output(
            "Usage:  !dumplastcapture [-?Lt] [module name]\n"
            "\n"
            "  L - Don't show full source lines\n"
            "  t - Consider only frames captured on the current thread\n"
            "\n"
            "  module name - module to look up last capture information from.\n"
            "                when not set defaults to current base module.\n"
            "\n"
            "Example: !dumplastcapture milcore\n"
            ));
    }
    else
    {
        // Dump the last capture
        IFC(DumpCaptureImpl(
                pOutCtl,
                pIData,
                pISymbols,
                pISystemObjects,
                StackOutputFlags,
                ThreadIdFilter,
                HRESULTFilter,
                pszModule,
                /* uNumberOfCaptureCollections */ 1,
                /* pLastCapturedFrame */ NULL
                ));
    }

Cleanup:
    IGNORE_HR(OutCtl.Output("\n"));

    if (FAILED(hr))
    {
        IGNORE_HR(OutCtl.Output("DumpLastCapture failed because of HR: %x\n\n",
                                hr));

        if (IsOutOfMemory(hr))
        {
            IGNORE_HR(OutCtl.Output("Memory is low: try unloading unnecessary modules and re-run the extension.\n"));
        }
    }

    ReleaseInterface(pIData);
    ReleaseInterface(pISymbols);
    ReleaseInterface(pISystemObjects);

    RRETURN(hr);
}

//+----------------------------------------------------------------------------
//
//  Extension:
//      DumpCaptures
//
//  Synopsis:
//      Debugger extension that dumps the last N stack captures for a given module
//
//-----------------------------------------------------------------------------
          
DECLARE_API(dumpcaptures)
{
    HRESULT hr = S_OK;
    
    BEGIN_API(DumpCaptures);

    OutputControl OutCtl(Client);           
    OutputControl* pOutCtl = &OutCtl;           

    MILX_TRACE_ENTRY;

    IDebugDataSpaces* pIData = NULL;
    IDebugSymbols3 *pISymbols = NULL;
    IDebugSystemObjects4 *pISystemObjects = NULL;

    CStackCaptureData *pCaptureData = NULL;
    CStackCaptureIterator* pCaptureIterator = NULL;
    DWORD *pThreads = NULL;

    // Obtain debug library interfaces for looking up symbols, etc.    
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));
    IFC(Client->QueryInterface(__uuidof(IDebugSymbols3), (void **)&pISymbols));    
    IFC(Client->QueryInterface(__uuidof(IDebugSystemObjects4), (void **)&pISystemObjects));

    DWORD StackOutputFlags =
        DEBUG_STACK_FRAME_ADDRESSES_RA_ONLY | DEBUG_STACK_SOURCE_LINE;

    ULONG Rem;
    ULONG EvalStartIndex;
    CHAR szModule[MAX_PATH] = "";
    bool allThreads = false;

    //
    // Process options
    //

    DEBUG_VALUE HRESULTFilter;
    // Default to no HRESULT filtering
    HRESULTFilter.Type = DEBUG_VALUE_INVALID;

    PCSTR pszModule = NULL;

    DEBUG_VALUE NumberOfCaptureCollections;
    // Default to 1 collection
    NumberOfCaptureCollections.I32 = 1;
    NumberOfCaptureCollections.Type = DEBUG_VALUE_INT32;

    DEBUG_VALUE ThreadIdFilter;
    // Default to filter based on most recent capture's thread
    ThreadIdFilter.I32 = 0;
    ThreadIdFilter.Type = DEBUG_VALUE_INT32;

    bool BadSwitch = false;
    bool ShowUsage = false;

    while (!BadSwitch)
    {
        while (isspace(*args)) { args++; }

        if (*args != '-') break;

        args++;
        BadSwitch = (*args == '\0' || isspace(*args));

        while (!BadSwitch && *args != '\0' && !isspace(*args))
        {
            bool SimpleOption = false;
            // Read option character and advance argument pointer
            CHAR Option = *args++;

            switch (Option)
            {
            case 'h':
                // Read next characters as a number value for an error code.
                if (Evaluate(Client, args,
                             DEBUG_VALUE_INT32, EVALUATE_DEFAULT_RADIX, &HRESULTFilter,
                             &Rem, &EvalStartIndex,
                             EVALUATE_COMPACT_EXPR) == S_OK)
                {
                    if (SUCCEEDED(HRESULTFilter.I32))
                    {
                        IGNORE_HR(OutCtl.OutWarn("Warning: Error filter '%*s' evaluated as success code.\n",
                                                 Rem-EvalStartIndex, args+EvalStartIndex
                                                 ));
                    }
                    args += Rem;
                }
                else
                {
                    IGNORE_HR(OutCtl.OutErr("Error: Unrecognized value at '%s'\n", args));
                    BadSwitch = true;
                }
                break;
            case 'L':
                SimpleOption = true;
                StackOutputFlags &= ~DEBUG_STACK_SOURCE_LINE;
                break;
            case 'm':
                // Read next characters as a module name containing no spaces.
                while (isspace(*args)) { args++; }
                pszModule = args;
                while (*args != '\0' && !isspace(*args)) { args++; }
                // Ignore result since truncation is acceptable.
                IGNORE_HR(StringCbCopyNA(szModule, sizeof(szModule),
                                         pszModule, args-pszModule));
                pszModule = szModule;
                if (szModule[0] == '\0' || szModule[0] == '-')
                {
                    IGNORE_HR(OutCtl.OutErr("Error: Missing module name after -m\n"));
                    BadSwitch = true;
                }
                break;
            case 'n':
                // Read next characters as a number.                
                if (   (Evaluate(Client, args,
                                 DEBUG_VALUE_INT32, EVALUATE_DEFAULT_RADIX, &NumberOfCaptureCollections,
                                 &Rem, NULL,
                                 EVALUATE_COMPACT_EXPR) == S_OK)
                    && (NumberOfCaptureCollections.I32 > 0))
                {
                    args += Rem;
                }
                else
                {
                    IGNORE_HR(OutCtl.OutErr("Error: Unrecognized number or 0 at '%s'\n", args));
                    BadSwitch = true;
                }
                break;
            case 't':
                // Examine next characters as a thread Id.
                while (isspace(*args)) { args++; }
                if (*args == '-')
                {
                    ThreadIdFilter.I32 = (ULONG)-1;
                    ThreadIdFilter.Type = DEBUG_VALUE_INT32;
                }
                else if (*args == '*')
                {
                    ThreadIdFilter.Type = DEBUG_VALUE_INVALID;
                }
                else
                {
                    if (Evaluate(
                            Client, args,
                            DEBUG_VALUE_INT32, EVALUATE_DEFAULT_RADIX, &ThreadIdFilter,
                            &Rem, &EvalStartIndex,
                            EVALUATE_COMPACT_EXPR) == S_OK)
                    {
                        if (ThreadIdFilter.I32 > WORD_MAX)
                        {
                            IGNORE_HR(OutCtl.OutWarn("Warning: ThreadId '%*s' evaluated as greater than 0xffff.\n",
                                                     Rem-EvalStartIndex, args+EvalStartIndex
                                                     ));
                        }
                        args += Rem;
                    }
                    else
                    {
                        IGNORE_HR(OutCtl.OutErr("Error: Unrecognized thread id at '%s'\n", args));
                        BadSwitch = true;
                    }
                }
                break;
            case 'a': allThreads = true; break;
            case '?': ShowUsage = true; break;
            default:
                IGNORE_HR(OutCtl.OutErr("Error: Unknown option at '%c%s'\n", Option, args));
                BadSwitch = true;
                break;
            }
        }
    }

    if (!BadSwitch)
    {
        // No other arguments are expected.
        if (*args != '\0')
        {
            IGNORE_HR(OutCtl.OutErr("Error: Unknown option at '%s'\n", args));
            BadSwitch = true;
        }
    }

    // Check that module option was used or try to use default.
    if (!BadSwitch && !ShowUsage && !pszModule)
    {
        // Use base module when module argument not specified.
        if (!Type_Module.Name[0])
        {
            IGNORE_HR(OutCtl.OutErr("Error: Missing module name (base module not set)\n"));
            BadSwitch = true;
        }
        else
        {
            pszModule = Type_Module.Name;
        }
    }

    if (BadSwitch || ShowUsage)
    {
        IGNORE_HR(OutCtl.Output(
            "Usage:  !dumpcaptures [-?Lmnt]\n"
            "\n"
            "  -h <HRESULT>  - Only show capture collections with HRESULT.\n"
            "                  Default is to show all collections.\n"
            "\n"
            "  -L            - Don't show full source lines.\n"
            "\n"
            "  -m <module name>  - Module to look up capture information from.\n"
            "                      Default is current base module.\n"
            "\n"
            "  -n <num>  - Show NUM capture collections.  Default is 1.\n"
            "\n"
            "  -t [tid]  - Set thread filter for output.  If TID is not specified\n"
            "              output is limited to current thread.  Default is to\n"
            "              output captures from last captured thread.\n"
            "\n"
            "Example: !dumpcaptures -n 4 -m milcore\n"
            ));
    }
    else
    {
        if (allThreads)
        {
            DEBUG_VALUE listThreadIdFilter;
            listThreadIdFilter.Type = DEBUG_VALUE_INVALID;

            DEBUG_VALUE listHRESULTFilter;
            listHRESULTFilter.Type = DEBUG_VALUE_INVALID;

            // Get stack capture data
            IFC(CStackCaptureData::Create(
                pIData,
                pISymbols,
                pszModule,
                pOutCtl,
                &pCaptureData
                ));

            // Create the stack capture iterator
            IFC(CStackCaptureIterator::Create(
                    pCaptureData,
                    0,
                    listThreadIdFilter,
                    listHRESULTFilter,
                    &pCaptureIterator
                    ));

            if (pCaptureData->CurrentStackCaptureIndex() == UINT_MAX)
            {
                IGNORE_HR(OutCtl.Output("\nNo captures in %s.\n", pszModule));
            }
            else
            {
                CStackCaptureCollectionList CollectionList;

                StackCaptureFrame StackCapture;
                UINT uIndex;
                int capacity = 0;
                int count = 0;

                while (pCaptureIterator->GetNextFrame(
                    pOutCtl,
                    pISymbols,
                    &StackCapture,
                    &uIndex
                    ) == S_OK)
                {
                    IFC(CollectionList.Append(&StackCapture, uIndex));
                    ++capacity;
                }
                if (capacity > 0)
                {
                    pThreads = new DWORD[capacity];
                    if (!pThreads)
                    {
                        IFC(E_OUTOFMEMORY);
                    }
                }

                if (CollectionList.IsEmpty())
                {
                    IGNORE_HR(OutCtl.Output("\n !! No captures found, though current capture index is %u !!\n",
                                        pCaptureData->CurrentStackCaptureIndex()
                                        ));
                }
                else
                {
                    CaptureCollectionData CaptureCollection;
    
                    IGNORE_HR(OutCtl.Output("\n"));

                    while (CollectionList.Pop(&CaptureCollection))
                    {
                        bool foundThread = false;
                        for(int i = 0; i<count; i++)
                        {
                            if (pThreads[i] == CaptureCollection.ThreadId)
                            {
                                foundThread = true;
                                break;
                            }
                        }
                        if (!foundThread)
                        {
                            pThreads[count] = CaptureCollection.ThreadId;
                            ++count;
                        }
                    }
                    for (int i=0; i<count; i++)
                    {
                        ThreadIdFilter.Type = DEBUG_VALUE_INT32;
                        ThreadIdFilter.I32 = pThreads[i];
                        IFC(DumpCaptureImpl(
                                pOutCtl,
                                pIData,
                                pISymbols,
                                pISystemObjects,
                                StackOutputFlags,
                                ThreadIdFilter,
                                HRESULTFilter,
                                pszModule,
                                NumberOfCaptureCollections.I32,
                                /* pLastCapturedFrame */ NULL
                                ));
                    }
                }
            }
        }
        else
        {
            // Dump the captures
            IFC(DumpCaptureImpl(
                    pOutCtl,
                    pIData,
                    pISymbols,
                    pISystemObjects,
                    StackOutputFlags,
                    ThreadIdFilter,
                    HRESULTFilter,
                    pszModule,
                    NumberOfCaptureCollections.I32,
                    /* pLastCapturedFrame */ NULL
                    ));
        }
    }

Cleanup:
    IGNORE_HR(OutCtl.Output("\n"));

    if (FAILED(hr))
    {
        IGNORE_HR(OutCtl.Output("DumpCapture failed because of HR: %x\n\n",
                                hr));

        if (IsOutOfMemory(hr))
        {
            IGNORE_HR(OutCtl.Output("Memory is low: try unloading unnecessary modules and re-run the extension.\n"));
        }
    }

    if (pCaptureIterator)
        delete pCaptureIterator;
    if (pCaptureData)
        delete pCaptureData;
    if (pThreads)
        delete pThreads;

    ReleaseInterface(pIData);
    ReleaseInterface(pISymbols);
    ReleaseInterface(pISystemObjects);

    RRETURN(hr);
}

//+----------------------------------------------------------------------------
//
//  Extension:
//      Listaptures
//
//  Synopsis:
//      Debugger extension that summarizes stack captures for a given module
//
//-----------------------------------------------------------------------------
          
DECLARE_API(listcaptures)
{
    HRESULT hr = S_OK;
    
    BEGIN_API(ListCaptures);

    OutputControl OutCtl(Client);           
    OutputControl* pOutCtl = &OutCtl;           

    MILX_TRACE_ENTRY;

    IDebugDataSpaces* pIData = NULL;
    IDebugSymbols3 *pISymbols = NULL;
    IDebugSystemObjects4 *pISystemObjects = NULL;

    CStackCaptureData *pCaptureData = NULL;
    CStackCaptureIterator* pCaptureIterator = NULL;

    // Obtain debug library interfaces for looking up symbols, etc.    
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));
    IFC(Client->QueryInterface(__uuidof(IDebugSymbols3), (void **)&pISymbols));    
    IFC(Client->QueryInterface(__uuidof(IDebugSystemObjects4), (void **)&pISystemObjects));

    PCSTR pszModule = NULL;

    //
    // Process options
    //

    bool BadSwitch = false;
    bool ShowUsage = false;

    while (!BadSwitch)
    {
        while (isspace(*args)) args++;

        if (*args != '-') break;

        args++;
        BadSwitch = (*args == '\0' || isspace(*args));

        while (*args != '\0' && !isspace(*args))
        {
            switch (*args)
            {
            case '?': ShowUsage = true; break;
            default:
                IGNORE_HR(OutCtl.OutErr("Error: Unknown option at '%s'\n", args));
                BadSwitch = true;
                break;
            }

            if (BadSwitch) break;
            args++;
        }
    }

    if (!BadSwitch && !ShowUsage)
    {
        // Make sure remaining argument could be a module when base module is
        // not properly initialized/set.
        pszModule = args;

        if (!*pszModule)
        {
            if (!Type_Module.Name[0])
            {
                IGNORE_HR(OutCtl.OutErr("Error: Missing module name (base module not set)\n"));
                BadSwitch = true;
            }
            else
            {
                // Use base module as default
                pszModule = Type_Module.Name;
            }
        }
    }

    if (BadSwitch || ShowUsage)
    {
        IGNORE_HR(OutCtl.Output(
            "Usage:  !listcaptures [-?] [module name]\n"
            "\n"
            "  [module name]  - Module to look up capture information from.\n"
            "                   Default is current base module.\n"
            "\n"
            "Example: !listcaptures milcore\n"
            ));
    }
    else
    {
        DEBUG_VALUE ThreadIdFilter;
        ThreadIdFilter.Type = DEBUG_VALUE_INVALID;

        DEBUG_VALUE HRESULTFilter;
        HRESULTFilter.Type = DEBUG_VALUE_INVALID;

        // Get stack capture data
        IFC(CStackCaptureData::Create(
            pIData,
            pISymbols,
            pszModule,
            pOutCtl,
            &pCaptureData
            ));

        // Create the stack capture iterator
        IFC(CStackCaptureIterator::Create(
                pCaptureData,
                0,
                ThreadIdFilter,
                HRESULTFilter,
                &pCaptureIterator
                ));

        if (pCaptureData->CurrentStackCaptureIndex() == UINT_MAX)
        {
            IGNORE_HR(OutCtl.Output("\nNo captures in %s.\n", pszModule));
        }
        else
        {
            CStackCaptureCollectionList CollectionList;

            StackCaptureFrame StackCapture;
            UINT uIndex;

            while (pCaptureIterator->GetNextFrame(
                pOutCtl,
                pISymbols,
                &StackCapture,
                &uIndex
                ) == S_OK)
            {
                IFC(CollectionList.Append(&StackCapture, uIndex));
            }

            if (CollectionList.IsEmpty())
            {
                IGNORE_HR(OutCtl.Output("\n !! No captures found, though current capture index is %u !!\n",
                                        pCaptureData->CurrentStackCaptureIndex()
                                        ));
            }
            else
            {
                CaptureCollectionData CaptureCollection;

                IGNORE_HR(OutCtl.Output("\n"));

                while (CollectionList.Pop(&CaptureCollection))
                {
                    IGNORE_HR(OutCtl.Output(
                        "Thread Id: %08x  HRESULT: 0x%08x\n",
                        CaptureCollection.ThreadId,
                        CaptureCollection.hrFailure
                        ));
                }
            }
        }
    }

Cleanup:
    IGNORE_HR(OutCtl.Output("\n"));

    if (FAILED(hr))
    {
        IGNORE_HR(OutCtl.Output("DumpCapture failed because of HR: %x\n\n",
                                hr));

        if (IsOutOfMemory(hr))
        {
            IGNORE_HR(OutCtl.Output("Memory is low: try unloading unnecessary modules and re-run the extension.\n"));
        }
    }

    delete pCaptureIterator;
    delete pCaptureData;

    ReleaseInterface(pIData);
    ReleaseInterface(pISymbols);
    ReleaseInterface(pISystemObjects);

    RRETURN(hr);
}




