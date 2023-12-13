// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



MtExtern(UniqueListNode);

template <typename Type>
class UniqueList
{
public:
    UniqueList(
        void
        );

    ~UniqueList(
        void
        );

    HRESULT
    AddHead(
        Type    instance
       );

    bool
    Remove(
        Type    instance
        );

    struct Node : ListNodeT<Node>
    {
        DECLARE_METERHEAP_ALLOC(ProcessHeap, Mt(UniqueListNode));

        Node(
            void
            );

        Type    instance;
    };

    bool
    IsEmpty(
        void
        );

    Node*
    GetHead(
        void
        );

private:
    List<Node>  m_list;
};

#include "UniqueList.inl"

