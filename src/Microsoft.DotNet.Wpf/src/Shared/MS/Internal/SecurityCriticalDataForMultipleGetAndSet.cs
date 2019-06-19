// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
// 
// Description:
//              This is a helper class to facilate the storage of data that's Critical for set and get.
//              This file is used as a cannister to hold values for Dynamic properties that are
//              not safe to expose and are built to be used only privately.
//              The other caveat is that these dynamic properties should not be needed in the
//              animation or databinding scenarios example: PresenationSource                
//
//
//
//
//---------------------------------------------------------------------------
using System ; 
using System.Security ; 

using MS.Internal.PresentationCore;

namespace MS.Internal 
{
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal class SecurityCriticalDataForMultipleGetAndSet<T>
    {
        internal SecurityCriticalDataForMultipleGetAndSet(T value)
        { 
            _value = value; 
        }

        internal T Value 
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }

        private T _value;
    }
}
