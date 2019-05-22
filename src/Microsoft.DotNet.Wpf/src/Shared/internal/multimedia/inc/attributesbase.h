// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//*@@@+++@@@@******************************************************************
//
//
//
//
//*@@@---@@@@******************************************************************
//
#ifndef __ATTRIBUTESBASE_H
#define __ATTRIBUTESBASE_H

#include <mfapi.h>
#include <mferror.h>

#ifndef MAXULONG
#define MAXULONG    0xffffffff  // winnt
#endif

class CMFAttributes;

///////////////////////////////////////////////////////////////////////////////
// critical section based lock
// MF based components should use CMFLock instear
class CWin32AttributeLock
{
public:
    CWin32AttributeLock()
    {
        InitializeCriticalSection(&m_cs);
    }


    ~CWin32AttributeLock()
    {
        DeleteCriticalSection(&m_cs);
    }

    void Lock()
    {
        EnterCriticalSection(&m_cs);
    }

    void Unlock()
    {
        LeaveCriticalSection(&m_cs);
    }

private:
    CRITICAL_SECTION m_cs;
};

///////////////////////////////////////////////////////////////////////////////
//
template<class T, class TLock = CWin32AttributeLock>
class CMFAttributesImpl
    : public T
{
protected:

    TLock           m_Lock;

    enum
    {
        MEDIA_PROP_EXTEND_INCREMENT = 4,
    };

    class CPropEntry
    {
    public:
        GUID            m_guidKey;
        PROPVARIANT     m_Value;
    };

    CPropEntry*     m_pEntries;
    UINT32          m_cTotalEntries;
    UINT32          m_cUsedEntries;


    HRESULT
    ExtendStorage(
        __in    UINT32 cNewEntries
        )
    {
        HRESULT hr = S_OK;
        _LockStore();

        if (cNewEntries <= m_cTotalEntries) {
            //
            // we already have enough
            //
            goto out;
        }
        //
        // allocate more storage
        //
        CPropEntry* pTemp = new CPropEntry[cNewEntries];
        if (!pTemp) {
            hr = E_OUTOFMEMORY;
            goto out;
        }

        //
        // move the old elements
        //
        memcpy(pTemp, m_pEntries, m_cUsedEntries * sizeof(CPropEntry));
        delete [] m_pEntries;
        m_pEntries = pTemp;
        m_cTotalEntries = cNewEntries;

        //
        // initialize the new elements
        //
        memset(m_pEntries + m_cUsedEntries, 0, (m_cTotalEntries - m_cUsedEntries) * sizeof(CPropEntry));

    out:
        _UnlockStore();

        return hr;
    }

    PROPVARIANT*
    FindItem(
        __in    REFGUID guidKey
        )
    {
        PROPVARIANT* pRet = NULL;

        _LockStore();

        for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
            if (guidKey == m_pEntries[i].m_guidKey) {
                pRet = &(m_pEntries[i].m_Value);
                break;
            }
        }

        _UnlockStore();

        return pRet;
    }

    PROPVARIANT*
    CreateItem(
        __in    REFGUID guidKey
        )
    {
        PROPVARIANT* pRet = NULL;

        _LockStore();

        //
        // look for an old one and clear it out
        //
        pRet = FindItem(guidKey);
        if (pRet) {
            PropVariantClear(pRet);
            goto out;
        }

        //
        //  create a new one
        //
        if (m_cUsedEntries == m_cTotalEntries) {
            HRESULT hr = ExtendStorage(m_cTotalEntries + MEDIA_PROP_EXTEND_INCREMENT);
            if (FAILED(hr)) {
                pRet = NULL;
                goto out;
            }
        }

        //
        // fill in the guid and return the propvariant
        //
        m_pEntries[m_cUsedEntries].m_guidKey = guidKey;
        pRet = &(m_pEntries[m_cUsedEntries].m_Value);
        m_cUsedEntries += 1;

    out:
        _UnlockStore();

        return pRet;
    }

    HRESULT
    CloneAllAttributes(
        __in    IMFAttributes*  pDest
        )
    {
        HRESULT         hr = S_OK;

        _LockStore();

        hr = pDest->DeleteAllItems();
        if (FAILED(hr)) {
            goto out;
        }

        for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
            hr = pDest->SetItem(m_pEntries[i].m_guidKey, m_pEntries[i].m_Value);
            if (FAILED(hr)) {
                goto out;
            }
        }

    out:
        _UnlockStore();

        return hr;
    }

    HRESULT
    _DeleteAllItems()
    {
        for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
            PropVariantClear(&m_pEntries[i].m_Value);
        }

        m_cUsedEntries = 0;

        return S_OK;
    }

    void
    _LockStore()
    {
        m_Lock.Lock();
    }

    void
    _UnlockStore()
    {
        m_Lock.Unlock();
    }

    BOOL
    IsMFAttributeType(VARTYPE vt)
    {
        BOOL fRecognized = FALSE;

        switch(vt) {
            case MF_ATTRIBUTE_UINT32:
            case MF_ATTRIBUTE_UINT64:
            case MF_ATTRIBUTE_DOUBLE:
            case MF_ATTRIBUTE_GUID:
            case MF_ATTRIBUTE_STRING:
            case MF_ATTRIBUTE_BLOB:
            case MF_ATTRIBUTE_IUNKNOWN:
                fRecognized = TRUE;
                break;
        }

        return fRecognized;
    }

public:

    CMFAttributesImpl(UINT32 unInitialSize = 0)
        : m_cTotalEntries(0)
        , m_cUsedEntries(0)
        , m_pEntries(NULL)
    {
        ExtendStorage(unInitialSize);
    }


    virtual
    ~CMFAttributesImpl()
    {
        _DeleteAllItems();

        delete [] m_pEntries;

        m_pEntries = NULL;
        m_cTotalEntries = 0;
    }

#if 0
#ifndef ATTRIBUTES_BASE_NO_QI
    HRESULT
    STDMETHODCALLTYPE
    QueryInterface(
        __in    REFIID riid,
        __out   LPVOID *ppvObject
        )
    {
        if( NULL == ppvObject )
        {
            return( E_INVALIDARG );
        }

        HRESULT hr = S_OK;
        *ppvObject = NULL;

        if( ( riid == __uuidof(IMFAttributes) ) ||
            ( riid == __uuidof(IUnknown) ) )
        {
            *ppvObject = ( IMFAttributes *)this;
        }
        else
        {
            hr = E_NOINTERFACE;
        }

        if ( SUCCEEDED( hr ) )
        {
            ((LPUNKNOWN) *ppvObject)->AddRef( );
        }

        return( hr );
    }
#endif
#endif

    //
    // IMFAttributes interface
    //

    HRESULT
    STDMETHODCALLTYPE
    GetItem(
        __in        REFGUID       guidKey,
        __out_opt   PROPVARIANT*  pValue      // can be NULL to check for existence - must use PropVariantClear to free
        )
    {
        HRESULT         hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pValue) {
                hr = PropVariantCopy(pValue, pFound);
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetItemType(
        __in    REFGUID             guidKey,
        __out   MF_ATTRIBUTE_TYPE*  pType
        )
    {
        HRESULT         hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            *pType = (MF_ATTRIBUTE_TYPE)pFound->vt;
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    //
    // check if given PROPVARIANT is equal to one in store
    //
    // NOTE: only compares items of the 6 "standard" IMFAttributes types
    //
    HRESULT
    STDMETHODCALLTYPE
    CompareItem(
        __in    REFGUID         guidKey,
        __in    REFPROPVARIANT  Value,
        __out   BOOL*           pResult
        )
    {
        HRESULT         hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (Value.vt != pFound->vt) {
                *pResult = FALSE;
                goto out;
            }
            switch (pFound->vt) {
            case (MF_ATTRIBUTE_UINT32):
                if (pFound->ulVal != Value.ulVal) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_UINT64):
                if (pFound->uhVal.QuadPart != Value.uhVal.QuadPart) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_DOUBLE):
                if (pFound->dblVal != Value.dblVal) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_GUID):
                if (*(pFound->puuid) != *(Value.puuid)) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_STRING):
                if (wcscmp(pFound->pwszVal,Value.pwszVal) != 0) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_BLOB):
                if (pFound->caub.cElems != Value.caub.cElems) {
                    *pResult = FALSE;
                    goto out;
                }
                if (memcmp(pFound->caub.pElems, Value.caub.pElems, pFound->caub.cElems) != 0) {
                    *pResult = FALSE;
                    goto out;
                }
                break;

            case (MF_ATTRIBUTE_IUNKNOWN):
                {
                    //
                    // We don't compare IUnknowns, but the types match,
                    // which is good enough
                    //
                    *pResult = TRUE;
                    goto out;
                }

            default:
                {
                    //
                    // we don't compare any other type, but the types match,
                    // which is good enough
                    //
                    *pResult = TRUE;
                    goto out;
                }
                break;
            }
        } else {
            *pResult = FALSE;
            goto out;
        }

        //
        // everything matches
        //
        *pResult = TRUE;

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    Compare(
        __in    IMFAttributes*              pTheirs,
        __in    MF_ATTRIBUTES_MATCH_TYPE    MatchType,
        __out   BOOL*                       pbResult
        )
    {
        return _Compare(pTheirs, MatchType, NULL, 0, pbResult);
    }
    
    HRESULT _Compare(
        __in    IMFAttributes*              pTheirs,
        __in    MF_ATTRIBUTES_MATCH_TYPE    MatchType,
        __in    const GUID*                 pExcludeGuids,
        __in    UINT32                      cExcludeGuids,
        __out   BOOL*                       pbResult
        )
    {
        HRESULT hr = S_OK;
        (void)_LockStore();
        hr = pTheirs->LockStore();
        if (FAILED(hr)) {
            goto out2;
        }

        //
        // handle the MF_ATTRIBUTES_MATCH_SMALLER type by converting it to
        // a "match theirs" or "match ours" type.
        //
        if (MatchType == MF_ATTRIBUTES_MATCH_SMALLER) {
            UINT32 unTheirSize = 0;
            hr = pTheirs->GetCount(&unTheirSize);
            if (FAILED(hr)) {
                goto out;
            }

            if (unTheirSize < m_cUsedEntries) {
                MatchType = MF_ATTRIBUTES_MATCH_THEIR_ITEMS;
            } else {
                MatchType = MF_ATTRIBUTES_MATCH_OUR_ITEMS;
            }
        }

        switch (MatchType) {
        case MF_ATTRIBUTES_MATCH_INTERSECTION:
            {
                //
                // match our items that also exist in theirs
                //
                for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
                    BOOL bMatch;

                    if(_IsGuidExcluded(m_pEntries[i].m_guidKey, pExcludeGuids, cExcludeGuids))
                    {
                        continue;
                    }
                    
                    //
                    // check for existence in their store
                    //
                    if (SUCCEEDED(pTheirs->GetItem(m_pEntries[i].m_guidKey, NULL))) {

                        //
                        // we call their comparison function so we don't need to copy their value - we can safely
                        // send our value without a copy
                        //
                        hr = pTheirs->CompareItem(m_pEntries[i].m_guidKey, m_pEntries[i].m_Value, &bMatch);
                        if (FAILED(hr)) {
                            goto out;
                        }
                        if (!bMatch) {
                            *pbResult = FALSE;
                            goto out;
                        }
                    }
                }
            }
            break;

        case MF_ATTRIBUTES_MATCH_OUR_ITEMS:
        case MF_ATTRIBUTES_MATCH_ALL_ITEMS:
            {
                //
                // first match our items
                //
                for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
                    BOOL bMatch;

                    if(_IsGuidExcluded(m_pEntries[i].m_guidKey, pExcludeGuids, cExcludeGuids))
                    {
                        continue;
                    }
                    
                    //
                    // we call their comparison function so we don't need to copy their value - we can safely
                    // send our value without a copy
                    //
                    hr = pTheirs->CompareItem(m_pEntries[i].m_guidKey, m_pEntries[i].m_Value, &bMatch);
                    if (FAILED(hr)) {
                        goto out;
                    }
                    if (!bMatch) {
                        *pbResult = FALSE;
                        goto out;
                    }
                }

                //
                // now check the other way, if necessary
                //
                if (MatchType == MF_ATTRIBUTES_MATCH_ALL_ITEMS) {
                    UINT32 cItems = 0;

                    hr = pTheirs->GetCount(&cItems);
                    if (FAILED(hr)) {
                        goto out;
                    }

                    //
                    // if the two stores have different number of items, they clearly don't match.
                    //
                    // and if they do have the same number of items, then we've already checked them all above. Q.E.D.
                    //
                    if (cItems != m_cUsedEntries) {
                        *pbResult = FALSE;
                        goto out;
                    }
                }
            }
            break;

        case MF_ATTRIBUTES_MATCH_THEIR_ITEMS:
            {
                UINT32 cItems = 0;
                GUID guidKey;
                PROPVARIANT*    pFound = NULL;

                hr = pTheirs->GetCount(&cItems);
                if (FAILED(hr)) {
                    goto out;
                }

                for (UINT32 i = 0; i < cItems; ++i) {
                    BOOL bMatch;

                    hr = pTheirs->GetItemByIndex(i, &guidKey, NULL);
                    if (FAILED(hr)) {
                        goto out;
                    }

                    if(_IsGuidExcluded(guidKey, pExcludeGuids, cExcludeGuids))
                    {
                        continue;
                    }
                    
                    //
                    // look for it in our store to avoid a PROPVARIANT copy
                    //
                    pFound = FindItem(guidKey);
                    if (!pFound) {
                        *pbResult = FALSE;
                        goto out;
                    }

                    //
                    // we call their comparison function so we don't need to copy their value - we can safely
                    // send our value without a copy
                    //
                    hr = pTheirs->CompareItem(guidKey, *pFound, &bMatch);
                    if (FAILED(hr)) {
                        goto out;
                    }

                    if (!bMatch) {
                        *pbResult = FALSE;
                        goto out;
                    }
                }
            }
            break;
        case MF_ATTRIBUTES_MATCH_SMALLER: // fix compiler warning C4061
        default:
            {
                hr = E_INVALIDARG;
                goto out;
            }

        }

        //
        // everything matched!
        //
        *pbResult = TRUE;

    out:
        (void)(pTheirs->UnlockStore());

    out2:
        (void)_UnlockStore();
        return hr;
    }

    BOOL _IsGuidExcluded(
        __in    GUID                        guidCheck,
        __in    const GUID*                 pExcludeGuids,
        __in    UINT32                      cExcludeGuids
        )
    {
        for(UINT32 i = 0; i < cExcludeGuids; i++)
        {
            if(guidCheck == pExcludeGuids[i])
            {
                return TRUE;
            }
        }

        return FALSE;
    }
    
    //
    // friendly typed "get" functions - return the default if the guid isn't found or the type
    // cannot be converted.
    //

    HRESULT
    STDMETHODCALLTYPE
    GetUINT32(
        __in    REFGUID guidKey,
        __out   UINT32* punValue
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if ( pFound->vt == MF_ATTRIBUTE_UINT32) {
                *punValue = pFound->ulVal;
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetUINT64(
        __in    REFGUID guidKey,
        __out   UINT64* punValue
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_UINT64) {
                *punValue = pFound->uhVal.QuadPart;
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetDouble(
        __in    REFGUID guidKey,
        __out   double* pfValue
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_DOUBLE) {
                *pfValue = pFound->dblVal;
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetGUID(
        __in    REFGUID guidKey,
        __out   GUID*   pguidValue
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_GUID) {
                *pguidValue = *(pFound->puuid);
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetStringLength(
        __in    REFGUID guidKey,
        __out   UINT32* pcchLength
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_STRING) {
                size_t size = wcslen(pFound->pwszVal);

                if (size >= MAXULONG) {
                    hr = E_OUTOFMEMORY;
                } else {
                    *pcchLength = UINT32(size);
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetString(
        __in                        REFGUID guidKey,
        __out_ecount(cchBufSize)    LPWSTR  pwszValue,
        __in                        UINT32  cchBufSize,
        __out_opt                   UINT32* pcchLength
        )
    {
        HRESULT hr = S_OK;
        UINT32  unLen = 0;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_STRING) {
                size_t size = wcslen(pFound->pwszVal);

            if (size >= (MAXULONG / sizeof(WCHAR)) - 1) {
                    hr = E_OUTOFMEMORY;
                } else {
                    unLen = UINT32(size);
                    if (pcchLength) {
                        *pcchLength = unLen;
                    }
                    if (unLen + 1 > cchBufSize) {
                        hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
                    } else {
                        memcpy(pwszValue, pFound->pwszVal, (unLen + 1) * sizeof(WCHAR));
                    }
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetAllocatedString(
        __in        REFGUID guidKey,
        __out       LPWSTR* ppwszValue, // returned string must be deallocated with CoTaskMemFree
        __out_opt   UINT32* pcchLength
        )
    {
        HRESULT hr = S_OK;
        UINT32  unLen = 0;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);


        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_STRING) {
                size_t size = wcslen(pFound->pwszVal);

                if (size >= (MAXULONG / sizeof(WCHAR)) - 1) {
                    hr = E_OUTOFMEMORY;
                } else {
                    unLen = UINT32(size);
                    if (pcchLength) {
                        *pcchLength = unLen;
                    }
                    *ppwszValue = (LPWSTR)CoTaskMemAlloc((unLen + 1) * sizeof(WCHAR));
                    if (*ppwszValue == NULL) {
                        hr = E_OUTOFMEMORY;
                    } else {
                        memcpy(*ppwszValue, pFound->pwszVal, (unLen + 1) * sizeof(WCHAR));
                    }
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetBlobSize(
        __in    REFGUID guidKey,
        __out   UINT32* pcbBlobSize
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_BLOB) {
               *pcbBlobSize = pFound->caub.cElems;
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetBlob(
        __in                    REFGUID guidKey,
        __out_bcount(cbBufSize) UINT8*  pBuf,
        __in                    UINT32  cbBufSize,
        __out_opt               UINT32* pcbBlobSize
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_BLOB) {
                if (pcbBlobSize) {
                    *pcbBlobSize = pFound->caub.cElems;
                }
                if (pFound->caub.cElems > cbBufSize) {
                    hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
                } else {
                    memcpy(pBuf, pFound->caub.pElems, pFound->caub.cElems);
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetAllocatedBlob(
        __in        REFGUID guidKey,
        __out       UINT8** ppBuf,       // returned blob must be deallocated with CoTaskMemFree
        __out_opt   UINT32* pcbBlobSize
        )
    {
        HRESULT hr = S_OK;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_BLOB) {
                if (pcbBlobSize) {
                    *pcbBlobSize = pFound->caub.cElems;
                }
                *ppBuf = (UINT8*)CoTaskMemAlloc(pFound->caub.cElems);
                if (*ppBuf == NULL) {
                    hr = E_OUTOFMEMORY;
                } else {
                    memcpy(*ppBuf, pFound->caub.pElems, pFound->caub.cElems);
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetUnknown(
        __in    REFGUID guidKey,
        __in    REFIID  riid,
        __out   LPVOID* ppv
        )
    {
        HRESULT hr = S_OK;
        *ppv = NULL;
        _LockStore();
        PROPVARIANT*    pFound = FindItem(guidKey);

        if (pFound) {
            if (pFound->vt == MF_ATTRIBUTE_IUNKNOWN) {
                if (pFound->punkVal) {
                    hr = pFound->punkVal->QueryInterface( riid, ppv );
                } else {
#ifdef MFASSERT
                    MFASSERT( FALSE );
#else
#ifdef ASSERT
                    ASSERT( FALSE );
#endif
#endif
                    hr = MF_E_INVALIDTYPE;
                }
            } else {
                hr = MF_E_INVALIDTYPE;
            }
        } else {
            hr = MF_E_ATTRIBUTENOTFOUND;
        }

        _UnlockStore();
        return hr;
    }

    //
    // Generic set/delete functions
    //

    HRESULT
    STDMETHODCALLTYPE
    SetItem(
        __in    REFGUID         guidKey,
        __in    REFPROPVARIANT  Value
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (IsMFAttributeType(Value.vt)) {
            if (!pNew) {
                hr = E_OUTOFMEMORY;
                goto out;
            }
            hr = PropVariantCopy(pNew, &Value);
        } else {
#if DBG
            DebugBreak();
#endif
            hr = MF_E_INVALIDTYPE;
        }

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    DeleteItem(
        __in    REFGUID guidKey
        )
    {
        _LockStore();
        HRESULT hr = S_OK;

        for (UINT32 i = 0; i < m_cUsedEntries; ++i) {
            if (guidKey == m_pEntries[i].m_guidKey) {
                //
                // Ignore return value from PVClear, since item is still removed.
                // Assert in CHK code, since this might indicate a problem.
                //
                (void)PropVariantClear(&(m_pEntries[i].m_Value));
                //
                // whether the PropVariantClear succeeded or not, we're still going to remove the entry
                //
                memcpy(m_pEntries + i, m_pEntries + i + 1, (m_cUsedEntries - i - 1) * sizeof(CPropEntry));
                m_cUsedEntries -= 1;
                break;
            }
        }

        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    DeleteAllItems()
    {
        _LockStore();

        _DeleteAllItems();

        _UnlockStore();

        return S_OK;
    }

    //
    // Typed set functions
    //

    HRESULT
    STDMETHODCALLTYPE
    SetUINT32(
        __in    REFGUID guidKey,
        __in    UINT32  unValue
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }
        pNew->vt = MF_ATTRIBUTE_UINT32;
        pNew->ulVal = unValue;

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    SetUINT64(
        __in    REFGUID guidKey,
        __in    UINT64  unValue
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }
        pNew->vt = MF_ATTRIBUTE_UINT64;
        pNew->uhVal.QuadPart = unValue;

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    SetDouble(
        __in    REFGUID guidKey,
        __in    double  fValue
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }
        pNew->vt = MF_ATTRIBUTE_DOUBLE;
        pNew->dblVal = fValue;

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    SetGUID(
        __in    REFGUID guidKey,
        __in    REFGUID guidValue
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }
        pNew->puuid = (GUID*)CoTaskMemAlloc(sizeof(GUID));
        if (!pNew->puuid) {
            DeleteItem(guidKey);
            hr = E_OUTOFMEMORY;
            goto out;
        }

        pNew->vt = MF_ATTRIBUTE_GUID;
        *(pNew->puuid) = guidValue;

    out:
        _UnlockStore();
        return hr;
    }

    HRESULT
    STDMETHODCALLTYPE
    SetString(
        __in    REFGUID guidKey,
        __in    LPCWSTR wszValue
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;
        UINT32          cbBuf = 0;
        size_t          size = 0;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }

        size = (wcslen(wszValue) + 1) * sizeof(WCHAR);
        if (size >= MAXULONG) {
            hr = E_INVALIDARG;
            goto out;
        }
        cbBuf = UINT32(size);

        pNew->pwszVal = (LPWSTR)CoTaskMemAlloc(cbBuf);
        if (!pNew->pwszVal) {
            DeleteItem(guidKey);
            hr = E_OUTOFMEMORY;
            goto out;
        }

        memcpy(pNew->pwszVal, wszValue, cbBuf);

        pNew->vt = MF_ATTRIBUTE_STRING;

    out:
        _UnlockStore();
        return hr;
    }

    //
    // the blob is copied into a newly allocated buffer which is owned
    // by the underlying MediaPropStore object
    //
    HRESULT
    STDMETHODCALLTYPE
    SetBlob(
        __in                    REFGUID         guidKey,
        __in_bcount(cbBufSize)  const UINT8*    pBuf,
        __in                    UINT32          cbBufSize
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }
        pNew->caub.pElems = (UINT8*)CoTaskMemAlloc(cbBufSize);
        if (!pNew->caub.pElems) {
            DeleteItem(guidKey);
            hr = E_OUTOFMEMORY;
            goto out;
        }

        memcpy(pNew->caub.pElems, pBuf, cbBufSize);

        pNew->vt = MF_ATTRIBUTE_BLOB;
        pNew->caub.cElems = cbBufSize;

    out:
        _UnlockStore();
        return hr;
    }


    HRESULT
    STDMETHODCALLTYPE
    SetUnknown(
        __in    REFGUID     guidKey,
        __in    IUnknown*   pUnknown
        )
    {
        _LockStore();
        PROPVARIANT*    pNew = CreateItem(guidKey);
        HRESULT         hr = S_OK;

        if (!pNew) {
            hr = E_OUTOFMEMORY;
            goto out;
        }

        pNew->vt = MF_ATTRIBUTE_IUNKNOWN;
        pNew->punkVal = pUnknown;
        if (pNew->punkVal) {
            pNew->punkVal->AddRef();
        }


    out:
        _UnlockStore();
        return hr;
    }

    //
    // functions to enumerate all items in the store
    // - first lock the store unless you are sure no other threads will add/delete/change items.
    // - then get the count (optional)
    // - then get each guid/value pair until you have gotten (count) or GetItemN returns (error TBD)
    // - unlock the store if it was locked in step 1
    //

    //
    // lock the store so it will not allow any other thread to access it
    //
    HRESULT
    STDMETHODCALLTYPE
    LockStore()
    {
        _LockStore();

        return S_OK;
    }

    //
    // unlock the store, allowing multi-thread access again
    //
    HRESULT
    STDMETHODCALLTYPE
    UnlockStore()
    {
        _UnlockStore();

        return S_OK;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetCount(
        __out   UINT32* pcItems
        )
    {
        *pcItems = m_cUsedEntries;
        return S_OK;
    }

    HRESULT
    STDMETHODCALLTYPE
    GetItemByIndex(
        __in        UINT32          unIndex,
        __out       GUID*           pguidKey,
        __out_opt   PROPVARIANT*    pValue      // must use PropVariantClear() to free
        )
    {
        HRESULT hr = S_OK;

        _LockStore();
        if (unIndex >= m_cUsedEntries) {
            hr = E_INVALIDARG;
            goto out;

        }
        *pguidKey = m_pEntries[unIndex].m_guidKey;
        if (pValue) {
            hr = PropVariantCopy(pValue, &(m_pEntries[unIndex].m_Value));
        }

    out:
        _UnlockStore();

        return hr;
    }

    //
    // function to get a copy of the entire store
    //
    HRESULT
    STDMETHODCALLTYPE
    CopyAllItems(
        __in    IMFAttributes*  pDest
        )
    {
        HRESULT         hr = S_OK;

        _LockStore();

        hr = CloneAllAttributes(pDest);
        if (FAILED(hr)) {
            goto out;
        }

    out:

        _UnlockStore();

        return hr;
    }
};

#endif // #ifndef __ATTRIBUTESBASE_H

