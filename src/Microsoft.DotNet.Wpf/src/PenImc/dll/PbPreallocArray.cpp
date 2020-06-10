// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

/////////////////////////////////////////////////////////////////////////////

template<class ENTRY_TYPE, const int INITIAL_COUNT>
class CPbPreallocArray
{
public:

	/////////////////////////////////////////////////////////////////////////

    CPbPreallocArray()
    {
		m_cCurrent   = 0;
        m_cAllocated = INITIAL_COUNT;
        m_pa = m_aInitial;
    };

	/////////////////////////////////////////////////////////////////////////

    ~CPbPreallocArray()
    {
        if (m_pa != NULL &&
            m_pa != m_aInitial)
        {
            delete m_pa;
        }
    };

	/////////////////////////////////////////////////////////////////////////

    inline INT GetSize()
	{
		return m_cCurrent;
	};

	/////////////////////////////////////////////////////////////////////////

	HRESULT SetSize(INT cNew, BOOL fGrowFast = TRUE)
	{
		DHR;

		CHR(EnsureSize(cNew, fGrowFast));

		m_cCurrent = cNew;

	CLEANUP:
		RHR;
	}

	/////////////////////////////////////////////////////////////////////////

    HRESULT Add(INT * pidxNew, BOOL fGrowFast = TRUE)
    {
        DHR;
        ASSERT (pidxNew);
        CHR(SetSize(GetSize() + 1, fGrowFast));
        *pidxNew = GetSize() - 1;
    CLEANUP:
        RHR;
    }

	/////////////////////////////////////////////////////////////////////////

    HRESULT Add(ENTRY_TYPE entry, BOOL fGrowFast = TRUE)
    {
        DHR;
        INT idxNew;
        CHR(Add(&idxNew));
        (*this)[idxNew] = entry;
    CLEANUP:
        RHR;
    }

	/////////////////////////////////////////////////////////////////////////

	ENTRY_TYPE & operator[](UINT idx)
	{
		ASSERT (idx < m_cCurrent);
		return m_pa[idx];
	}

	/////////////////////////////////////////////////////////////////////////

	ENTRY_TYPE * GetData()
	{
		ASSERT (m_pa);
		return m_pa;
	}

	/////////////////////////////////////////////////////////////////////////

    HRESULT Remove(UINT idx)
    {
        DHR;
        ASSERT (0 <= idx && idx < m_cCurrent);
        if (idx < m_cCurrent - 1)
        {
            UINT cbToCopy = sizeof(m_pa[0]) * (m_cCurrent - idx - 1);
            ASSERT (sizeof(m_pa[0]) * 1 <= cbToCopy && cbToCopy <= sizeof(m_pa[0]) * (m_cCurrent - 1));
            CopyMemory(&(m_pa[idx]), &(m_pa[idx + 1]), cbToCopy);
        }
        m_cCurrent--;
        RHR;
    }

	/////////////////////////////////////////////////////////////////////////

protected:

	/////////////////////////////////////////////////////////////////////////

    HRESULT EnsureSize(UINT cRequested, BOOL fGrowFast)
    {
        DHR;

        if (m_cAllocated < cRequested)
        {
            if (fGrowFast)
                cRequested = max(m_cAllocated * 2, cRequested);

			ASSERT (m_pa != NULL);
			if (m_pa == m_aInitial)
			{
				CHR_MEMALLOC(m_pa = new ENTRY_TYPE[cRequested]);
				CopyMemory(m_pa, m_aInitial, sizeof(m_pa[0]) * m_cAllocated);
			}
			else
			{
                ENTRY_TYPE * paNew = (ENTRY_TYPE*)realloc(m_pa, sizeof(m_pa[0]) * cRequested);
				CHR_MEMALLOC(paNew);
                m_pa = paNew;
			}
			m_cAllocated = cRequested;
        }

	CLEANUP:
        RHR;
    }

	/////////////////////////////////////////////////////////////////////////

protected:

    ENTRY_TYPE	    m_aInitial[INITIAL_COUNT];
    ENTRY_TYPE*	    m_pa;
    UINT			m_cAllocated;
    UINT			m_cCurrent;
};

/////////////////////////////////////////////////////////////////////////////

#ifdef DBG
void TestPbPreallocArray();
#endif


