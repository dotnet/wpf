// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This class represents a (ContentUser , ContentRight) pair.
//
//
//
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using MS.Internal.Security.RightsManagement;
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper; 

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// ContentGrant class represent a (ContentUser , ContentRight) pair this is 
    /// a basic building block for structures that need to express information about rights granted to a document.
    /// </summary>
    /// <SecurityNote>
    ///     Critical:    This class expose access to methods that eventually do one or more of the the following
    ///             1. call into unmanaged code 
    ///             2. affects state/data that will eventually cross over unmanaged code boundary
    ///             3. Return some RM related information which is considered private 
    ///
    ///     TreatAsSafe: This attribute automatically applied to all public entry points. All the public entry points have
    ///     Demands for RightsManagementPermission at entry to counter the possible attacks that do 
    ///     not lead to the unmanaged code directly(which is protected by another Demand there) but rather leave 
    ///     some status/data behind which eventually might cross the unamanaged boundary. 
    /// </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything)]    
    public class ContentGrant
    {
        /// <summary>
        /// Constructor for the read only ContentGrant class. It takes values for user and right as parameters. 
        /// </summary>
        public ContentGrant(ContentUser user, ContentRight right)
                    : this(user, right, DateTime.MinValue, DateTime.MaxValue)
        {
        }

        /// <summary>
        /// Constructor for the read only ContentGrant class. It takes values for
        ///     user, right, validFrom, and validUntil as parameters. 
        /// </summary>
        public ContentGrant(ContentUser user, ContentRight right, DateTime validFrom, DateTime validUntil)
        {
            SecurityHelper.DemandRightsManagementPermission();
            // Add validation here 

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if ((right != ContentRight.View) && 
                (right != ContentRight.Edit) && 
                (right != ContentRight.Print) && 
                (right != ContentRight.Extract) && 
                (right != ContentRight.ObjectModel) && 
                (right != ContentRight.Owner) && 
                (right != ContentRight.ViewRightsData) && 
                (right != ContentRight.Forward) && 
                (right != ContentRight.Reply) &&
                (right != ContentRight.ReplyAll) &&
                (right != ContentRight.Sign) &&
                (right != ContentRight.DocumentEdit)  &&
                (right != ContentRight.Export))
            {
                throw new ArgumentOutOfRangeException("right");                
            }

            if (validFrom > validUntil)
            {
                throw new ArgumentOutOfRangeException("validFrom");                
            }

            _user = user;
            _right = right;

            _validFrom = validFrom;
            _validUntil = validUntil;
        }



        /// <summary>
        /// Read only User propery.
        /// </summary>
        public ContentUser User
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();            
                return _user;
            }
        }

        /// <summary>
        /// Read only Right propery.
        /// </summary>
        public ContentRight Right
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();            
                return _right;
            }
        }

        /// <summary>
        /// The starting validity time, in UTC time, for the grant.
        /// </summary>
        public DateTime  ValidFrom
        {
            get 
            {
                SecurityHelper.DemandRightsManagementPermission();
            
                return _validFrom; 
            }
        }

        /// <summary>
        /// The ending validity time, in UTC time, for the grant.
        /// </summary>
        public DateTime  ValidUntil 
        {
            get 
            {
                SecurityHelper.DemandRightsManagementPermission();
            
                return _validUntil; 
            }
        }
        
        private ContentUser _user;
        private ContentRight _right;
        private DateTime _validFrom;
        private DateTime _validUntil;
    }
}
