// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Structure that keeps Right name as a string not as enum, so it can be used to carry 
//  names that are not part of the ContentRights enum. 
//
//
//
//

using System;
using System.Diagnostics;
using System.Security;

namespace MS.Internal.Security.RightsManagement
{
    /// <SecurityNote>
    ///     Critical:    This class exposes access to methods that eventually do one or more of the the following
    ///             1. call into unmanaged code 
    ///             2. affects state/data that will eventually cross over unmanaged code boundary
    ///             3. Return some RM related information which is considered private 
    /// </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything)]  
    internal class RightNameExpirationInfoPair
    {
        internal RightNameExpirationInfoPair (string rightName, DateTime validFrom, DateTime validUntil)
        {
            Debug.Assert(rightName != null);
            
            _rightName = rightName;
            _validFrom = validFrom;
            _validUntil = validUntil;
        }
            
        /// <summary>
        /// We keep Right as a string for forward compatibility in case new
        /// rights get invented we would like be able to encrypt decrypt using them,
        /// although without ability to enumerate them 
        /// </summary>
        internal  string  RightName
        {
            get
            {
                return _rightName;
            }
        }

        /// <summary>
        /// The starting validity time, in UTC time 
        /// </summary>
        internal  DateTime  ValidFrom
        {
            get 
            {
                return _validFrom; 
            }
        }

        /// <summary>
        /// The ending validity time, in UTC time 
        /// </summary>
        internal  DateTime  ValidUntil 
        {
            get 
            {
                 return _validUntil; 
            }
        }

        private string _rightName;
        private DateTime _validFrom;
        private DateTime _validUntil;
    }
}





