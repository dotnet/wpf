// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



template <typename Type>
UniqueList<Type>::
UniqueList(
    void
    )
{}

template <typename Type>
UniqueList<Type>::
~UniqueList(
    void
    )
{
    Node    *pCurrent   = m_list.GetHead();
    Node    *pNext      = NULL;

    while (pCurrent != NULL)
    {
        pNext = static_cast<UniqueList<Type>::Node*>(pCurrent->pNext);
        m_list.Unlink(pCurrent);
        delete pCurrent;
        pCurrent = pNext;
    }
}

template <typename Type>
HRESULT
UniqueList<Type>::
AddHead(
    Type    instance
    )
{
    HRESULT hr = S_OK;
    Node    *pCurrent   = m_list.GetHead();
    Node    *pNext      = NULL;

    while (pCurrent != NULL)
    {
        pNext = static_cast<UniqueList<Type>::Node*>(pCurrent->pNext);

        if (pCurrent->instance == instance)
        {
            break;
        }

        pCurrent = pNext;
    }

    if (pCurrent == NULL)
    {

        Node    *pNewNode = new Node();

        IFCOOM(pNewNode);

        pNewNode->instance = instance;

        m_list.AddHead(pNewNode);
    }
    else
    {
        hr = S_FALSE;
    }

Cleanup:
    RRETURN1(hr, S_FALSE);
}

template <typename Type>
bool
UniqueList<Type>::
Remove(
    Type    instance
    )
{
    Node    *pCurrent   = m_list.GetHead();
    Node    *pNext      = NULL;

    while (pCurrent != NULL)
    {
        pNext = static_cast<UniqueList<Type>::Node*>(pCurrent->pNext);

        if (pCurrent->instance == instance)
        {
            break;
        }

        pCurrent = pNext;
    }

    if (pCurrent != NULL)
    {
        m_list.Unlink(pCurrent);
        delete pCurrent;
        pCurrent = NULL;

        return true;
    }
    else
    {
        return false;
    }
}

template <typename Type>
UniqueList<Type>::Node::
Node(
    void
    )
{
    pPrev = NULL;
    pNext = NULL;
}

template <typename Type>
bool
UniqueList<Type>::
IsEmpty(
    void
    )
{
    return !!m_list.IsEmpty();
}

template <typename Type>
typename UniqueList<Type>::Node*
UniqueList<Type>::
GetHead(
    void
    )
{
    return m_list.GetHead();
}


