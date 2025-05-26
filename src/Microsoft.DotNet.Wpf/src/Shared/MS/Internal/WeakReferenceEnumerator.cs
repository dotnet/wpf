// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;

#nullable enable

namespace MS.Internal;

/// <summary>
///    This allows callers to "foreach" through a WeakReferenceList.
///    Each weakreference is checked for liveness and "current"
///    actually returns a strong reference to the current element.
/// </summary>
/// <remarks>
///    Due to the way enumerators function, this enumerator often
///    holds a cached strong reference to the "Current" element.
///    This should not be a problem unless the caller stops enumerating
///    before the end of the list AND holds the enumerator alive forever.
/// </remarks>
internal struct WeakReferenceListEnumerator : IEnumerator
{
    private readonly ReadOnlyCollection<object> _backingList;

    private object? _strongReference;
    private int _index;

    public WeakReferenceListEnumerator(ReadOnlyCollection<object> backingList)
    {
        _index = 0;
        _backingList = backingList;
        _strongReference = null;
    }

    readonly object IEnumerator.Current
    {
        get => Current;
    }

    public readonly object Current
    {
        get => _strongReference ?? throw new InvalidOperationException(SR.Enumerator_VerifyContext);
    }

    public bool MoveNext()
    {
        object? element = null;

        while (_index < _backingList.Count)
        {
            WeakReference weakRef = (WeakReference)_backingList[_index++];
            element = weakRef.Target;
            if (element is not null)
                break;
        }

        _strongReference = element;

        return element is not null;
    }

    public void Reset()
    {
        _index = 0;
        _strongReference = null;
    }
}

