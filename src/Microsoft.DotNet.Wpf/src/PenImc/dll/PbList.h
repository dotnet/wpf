// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

/////////////////////////////////////////////////////////////////////////////

typedef DWORD_PTR PBLKEY; // PBList KEY

#define PBLKEY_NULL NULL

/////////////////////////////////////////////////////////////////////////////

template<class ENTRY_TYPE>
class CPbList
{
public:

    /////////////////////////////////////////////////////////////////////////

    CPbList()
    {
        m_pHead = NULL;
        m_pTail = NULL;
#ifdef DBG
        m_pfSyncCheckDbg = NULL;
#endif
    }

    /////////////////////////////////////////////////////////////////////////

    ~CPbList()
    {
        ClearList();
    }

    /////////////////////////////////////////////////////////////////////////

#ifdef DBG
    void SetSyncCheckDbg(BOOL * pfSyncCheckDbg)
    {
        m_pfSyncCheckDbg = pfSyncCheckDbg;
    }
#endif

    /////////////////////////////////////////////////////////////////////////

#ifdef DBG
    BOOL SyncCheckDbg()
    {
        if (!m_pfSyncCheckDbg)
            return TRUE;
        return SyncCheckCoreDbg(*m_pfSyncCheckDbg);
    }
#endif

    /////////////////////////////////////////////////////////////////////////

#ifdef DBG
    static BOOL SyncCheckCoreDbg(BOOL fSyncCheckDbg)
    {
        return fSyncCheckDbg;
    }
#endif

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToHead(ENTRY_TYPE entry)
    {
        DHR;
        PBLKEY key;
        CHR(AddToHead(&key));
        (*this)[key] = entry;
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToTail(ENTRY_TYPE entry)
    {
        DHR;
        PBLKEY key;
        CHR(AddToTail(&key));
        (*this)[key] = entry;
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToHead(__typefix(CListEntry *) __out PBLKEY * pkeyNew)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (pkeyNew);
        CListEntry * pNew = new CListEntry;
        CHR_MEMALLOC(pNew);
        CHR(AddToHeadCore(pNew))
        *pkeyNew = (PBLKEY)pNew;
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToTail(__typefix(CListEntry *) __out PBLKEY * pkeyNew)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (pkeyNew);
        CListEntry * pNew = new CListEntry;
        CHR_MEMALLOC(pNew);
        CHR(AddToTailCore(pNew))
        *pkeyNew = (PBLKEY)pNew;
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT InsertBefore(__typefix(CListEntry *) __inout PBLKEY keyBefore, __typefix(CListEntry *) __out PBLKEY * pkeyNew)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (pkeyNew);
        CListEntry * pNew = new CListEntry;
        CHR_MEMALLOC(pNew);
        CHR(InsertBeforeCore(keyBefore, pNew))
        *pkeyNew = (PBLKEY)pNew;
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT Remove(__typefix(CListEntry *) __in PBLKEY key, bool deleteEntry)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (!IsAtEnd(key));
        CListEntry * p = (CListEntry*)key;
        CHR(RemoveCore(p));

        if (deleteEntry)
        {
            delete p;
        }

    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT MoveToHead(__typefix(CListEntry *) __inout PBLKEY key)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (!IsAtEnd(key));
        CListEntry * p = (CListEntry*)key;
        CHR(RemoveCore(p));
        CHR(AddToHeadCore(p));
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT MoveToTail(__typefix(CListEntry *) __inout PBLKEY key)
    {
        DHR;
        ASSERT (SyncCheckDbg());
        ASSERT (!IsAtEnd(key));
        CListEntry * p = (CListEntry*)key;
        CHR(RemoveCore(p));
        CHR(AddToTailCore(p));
    CLEANUP:
        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    inline __typefix(CListEntry *) __out PBLKEY GetHead()
    {
        ASSERT (SyncCheckDbg());
        return (PBLKEY)m_pHead;
    }

    /////////////////////////////////////////////////////////////////////////

    inline __typefix(CListEntry *) __out PBLKEY GetTail()
    {
        ASSERT (SyncCheckDbg());
        return (PBLKEY)m_pTail;
    }

    /////////////////////////////////////////////////////////////////////////

    inline __typefix(CListEntry *) __out PBLKEY GetNext(__typefix(CListEntry *) __in PBLKEY key)
    {
        ASSERT (SyncCheckDbg());
        ASSERT (!IsAtEnd(key));
        CListEntry * p = (CListEntry*)key;
        return (PBLKEY)p->m_pNext;
    }

    /////////////////////////////////////////////////////////////////////////

    inline __typefix(CListEntry *) __out PBLKEY GetPrev(__typefix(CListEntry *) __in PBLKEY key)
    {
        ASSERT (SyncCheckDbg());
        ASSERT (!IsAtEnd(key));
        CListEntry * p = (CListEntry*)key;
        return (PBLKEY)p->m_pPrev;
    }

    /////////////////////////////////////////////////////////////////////////

    inline __out ENTRY_TYPE & operator[](__typefix(CListEntry *) __in PBLKEY key)
    {
        ASSERT (SyncCheckDbg());
        return Entry(key);
    }

    /////////////////////////////////////////////////////////////////////////

    inline static __out ENTRY_TYPE & Entry(
        __typefix(CListEntry *) __in PBLKEY key
#ifdef DBG
        , BOOL fSyncCheckDbg = TRUE
#endif
        )
    {
        ASSERT (SyncCheckCoreDbg(fSyncCheckDbg));
        CListEntry * p = (CListEntry*)key;
        return p->m_data;
    }

    /////////////////////////////////////////////////////////////////////////

    inline BOOL IsAtEnd(__typefix(CListEntry *) __in PBLKEY key)
    {
        ASSERT (SyncCheckDbg());
        return key == NULL;
    }

    /////////////////////////////////////////////////////////////////////////

    inline BOOL IsEmpty()
    {
        ASSERT (SyncCheckDbg());
        return m_pHead == NULL;
    }

    /////////////////////////////////////////////////////////////////////////

protected:

    /////////////////////////////////////////////////////////////////////////

    class CListEntry
    {
    public:
        ENTRY_TYPE      m_data;
        CListEntry *    m_pPrev;
        CListEntry *    m_pNext;
    };

    /////////////////////////////////////////////////////////////////////////

    void ClearList()
    {
        CListEntry * pCur = m_pHead;
        while (pCur)
        {
            CListEntry * pToDel = pCur;
            pCur = pCur->m_pNext;
            delete pToDel;
        }
        m_pHead = m_pTail = NULL;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToHeadCore(__inout CListEntry * pNew)
    {
        DHR;

        if (!m_pTail)
            m_pTail = pNew;

        pNew->m_pPrev = NULL;
        pNew->m_pNext = m_pHead;

        if (m_pHead)
            m_pHead->m_pPrev = pNew;

        m_pHead = pNew;

        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT AddToTailCore(__inout CListEntry * pNew)
    {
        DHR;

        if (!m_pHead)
            m_pHead = pNew;

        pNew->m_pPrev = m_pTail;
        pNew->m_pNext = NULL;

        if (m_pTail)
            m_pTail->m_pNext = pNew;

        m_pTail = pNew;

        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT InsertBeforeCore(__typefix(CListEntry *) __inout PBLKEY keyBefore, __inout CListEntry * pNew)
    {
        DHR;

        ASSERT(pNew);

        CListEntry * pPoint = (CListEntry*)keyBefore;
        CListEntry * pPrev = pPoint->m_pPrev;

        if (pPrev)
            pPrev->m_pNext = pNew;
        else
            m_pHead = pNew;

        pPoint->m_pPrev = pNew;

        pNew->m_pPrev = pPrev;
        pNew->m_pNext = pPoint;

        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    HRESULT RemoveCore (__in CListEntry * p)
    {
        DHR;

        ASSERT (p);

        CListEntry * pPrev = p->m_pPrev;
        CListEntry * pNext = p->m_pNext;
        if (pPrev)
            pPrev->m_pNext = pNext;
        if (pNext)
            pNext->m_pPrev = pPrev;

        if (m_pHead == p)
            m_pHead = pNext;
        if (m_pTail == p)
            m_pTail = pPrev;

        ASSERT (m_pHead != p);
        ASSERT (m_pTail != p);

        RHR;
    }

    /////////////////////////////////////////////////////////////////////////

    CListEntry *    m_pHead;
    CListEntry *    m_pTail;

#ifdef DBG
    BOOL *  m_pfSyncCheckDbg;
#endif
}; // class CPbList

/////////////////////////////////////////////////////////////////////////////

#ifdef DBG
void TestPbList();
#endif


