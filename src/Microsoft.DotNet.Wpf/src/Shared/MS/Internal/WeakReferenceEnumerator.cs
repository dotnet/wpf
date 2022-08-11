// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows;

#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif DRT
    using MS.Internal.Drt;
#else
#error Attempt to use FriendAccessAllowedAttribute from an unknown assembly.
using MS.Internal.YourAssemblyName;
#endif

// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691

namespace MS.Internal
{
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
    [FriendAccessAllowed]
    internal struct WeakReferenceListEnumerator : IEnumerator
    {
        public WeakReferenceListEnumerator( ArrayList List)
        {
            _i = 0;
            _List = List;
            _StrongReference = null;
        }

        object IEnumerator.Current
        {  get{ return Current; } }
        
        public object Current
        {
            get
            {
                if( null == _StrongReference )
                {
#pragma warning suppress 6503
                    throw new System.InvalidOperationException(SR.Enumerator_VerifyContext);
                }
                return _StrongReference;
            }
        }

        public bool MoveNext()
        {
            object obj=null;
            while( _i < _List.Count )
            {
                WeakReference weakRef = (WeakReference) _List[ _i++ ];
                obj = weakRef.Target;
                if(null != obj)
                    break;
            }
            _StrongReference = obj;
            return (null != obj);
        }

        public void Reset()
        {
            _i = 0;
            _StrongReference = null;
        }

        int _i;
        ArrayList _List;
        object _StrongReference;
    }
}

