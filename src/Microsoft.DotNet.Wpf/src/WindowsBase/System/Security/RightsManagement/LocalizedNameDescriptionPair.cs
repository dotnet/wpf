// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This class represents an immutable pair of Strings (Name, Description)
// That are generally used to represent name and description of an unsigned publish license 
// (a.k.a. template). Unsigned Publish License has property called LocalizedNameDescriptionDictionary
// which holds a map of a local Id to a Name Description pair, in order to support scenarios of 
// building locale specific template browsing applications.
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

// Allow use of presharp warning numbers [6506] and [6518] unknown to the compiler
#pragma warning disable 1634, 1691

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// LocalizedNameDescriptionPair class represent an immutable (Name, Description) pair of strings. This is 
    /// a basic building block for structures that need to express locale specific information about 
    ///  Unsigned Publish Licenses. 
    /// </summary>
    /// <SecurityNote>
    ///     Critical:    This class exposes access to methods that eventually do one or more of the following
    ///             1. call into unmanaged code 
    ///             2. affects state/data that will eventually cross over unmanaged code boundary
    ///             3. Return some RM related information which is considered private 
    ///
    ///     TreatAsSafe: This attrbiute automatically applied to all public entry points. All the public entry points have
    ///     Demands for RightsManagementPermission at entry to counter the possible attacks that do 
    ///     not lead to the unamanged code directly(which is protected by another Demand there) but rather leave 
    ///     some status/data behind which eventually might cross the unamanaged boundary. 
    /// </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything)]    
    public class LocalizedNameDescriptionPair
    {
        /// <summary>
        /// Constructor for the read only LocalizedNameDescriptionPair class. It takes values for Name and Description as parameters. 
        /// </summary>
        public LocalizedNameDescriptionPair(string name, string description)
        {
            SecurityHelper.DemandRightsManagementPermission();
        
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (description == null)
            {
                throw new ArgumentNullException("description");
            }
            
            _name = name;
            _description = description;
        }

        /// <summary>
        /// Read only Name property.
        /// </summary>
        public string Name 
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();

                return _name;
            }
        }

        /// <summary>
        /// Read only Description property.
        /// </summary>
        public string Description 
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();
            
                return _description;
            }
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            SecurityHelper.DemandRightsManagementPermission();

            if ((obj == null) || (obj.GetType() != GetType()))
            {
                return false;
            }

            LocalizedNameDescriptionPair localizedNameDescr = obj as LocalizedNameDescriptionPair;
            
            //PRESHARP:Parameter to this public method must be validated:  A null-dereference can occur here. 
            //This is a false positive as the checks above can gurantee no null dereference will occur  
#pragma warning disable 6506
            return (String.CompareOrdinal(localizedNameDescr.Name, Name) == 0)
                        &&
                    (String.CompareOrdinal(localizedNameDescr.Description, Description) == 0);
#pragma warning restore 6506
        }        
            
        /// <summary>
        /// Compute hash code.
        /// </summary>
        public override int GetHashCode()
        {
            SecurityHelper.DemandRightsManagementPermission();
        
            return Name.GetHashCode()  ^  Description.GetHashCode();
        }

        private string _name;
        private string _description; 
    }
}
