// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class implements the UnsignedPublishLicense class 
//   this class is the first step in the RightsManagement publishing process
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;           // for IEqualityComparer<T> generic interface.
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;

using MS.Internal;                          // for Invariant
using MS.Internal.Security.RightsManagement;
using SecurityHelper = MS.Internal.WindowsBase.SecurityHelper;

namespace System.Security.RightsManagement
{
    /// <summary>
    ///  This class represents a User for purposes of granting rights to that user, initializing secure environment for the user, 
    /// or enumerating rights granted to various users. 
    /// </summary>
    /// <SecurityNote>
    ///     Critical:    This class expose access to methods that eventually do one or more of the the following
    ///             1. call into unmanaged code 
    ///             2. affects state/data that will eventually cross over unmanaged code boundary
    ///             3. Return some RM related information which is considered private 
    ///
    ///     TreatAsSafe: This attrbiute automatically applied to all public entry points. All the public entry points have
    ///     Demands for RightsManagementPermission at entry to counter the possible attacks that do 
    ///     not lead to the unamanged code directly(which is protected by another Demand there) but rather leave 
    ///     some status/data behind which eventually might cross the unamanaged boundary. 
    /// </SecurityNote>
    public class ContentUser
    {
        /// <summary>
        ///  This constructor creates a user that will be granted a right. Or used in other related scenarios like
        /// initializing secure environment for the user, or enumerating rights granted to various users.         
        /// </summary>
        public ContentUser(string name, AuthenticationType authenticationType)
        {
            SecurityHelper.DemandRightsManagementPermission();

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Trim().Length == 0)
            {
                throw new ArgumentOutOfRangeException("name");
            }

            if ((authenticationType != AuthenticationType.Windows) &&
                (authenticationType != AuthenticationType.Passport) &&
                (authenticationType != AuthenticationType.WindowsPassport) &&
                (authenticationType != AuthenticationType.Internal))
            {
                throw new ArgumentOutOfRangeException("authenticationType");
            }

            // We only support Anyone for the internal mode at the moment
            if (authenticationType == AuthenticationType.Internal)
            {
                if (!CompareToAnyone(name) && !CompareToOwner(name))
                {
                    // we only support Anyone as internal user 
                    throw new ArgumentOutOfRangeException("name");
                }
            }

            _name = name;
            _authenticationType = authenticationType;
        }

        /// <summary>
        /// Currently only 2 Authentication types supported Windows and Passport
        /// </summary>
        public AuthenticationType AuthenticationType
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();

                return _authenticationType;
            }
        }

        /// <summary>
        /// Fully qualified SMTP address.
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
        /// Return true if the current User currently authenticated, which means that initialization 
        /// process will likely not require a prompt.
        /// We check if the machine and the user are activated.
        /// We don't check the existence of a client licensor certificate since it is only
        ///     required for publishing only
        /// </summary>
        public bool IsAuthenticated()
        {
            SecurityHelper.DemandRightsManagementPermission();

            // we can only have activated Windows or Passport users 
            // undefined authentication type can only be used for building a UnsignedPublishLicense  
            if ((_authenticationType != AuthenticationType.Windows) &&
                 (_authenticationType != AuthenticationType.Passport))
            {
                return false;
            }

            // User specific client session to check it's status 
            using (ClientSession userClientSession = new ClientSession(this))
            {
                return (userClientSession.IsMachineActivated() &&
                                userClientSession.IsUserActivated());
            }
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            SecurityHelper.DemandRightsManagementPermission();

            if (obj == null)
                return false;   // Standard behavior.

            if (GetType() != obj.GetType())
                return false;   // Different type.

            ContentUser userObj = (ContentUser)obj;

            return (String.CompareOrdinal(_name.ToUpperInvariant(), userObj._name.ToUpperInvariant()) == 0)
                        &&
                            _authenticationType.Equals(userObj._authenticationType);
        }

        /// <summary>
        /// Returns an instance of the User class that identifyes "Anyone" persona.
        /// This user has authentication type "Internal" and Name "Anyone".
        /// If this such user was granted rights dutring publishing; server will issue Use License 
        /// to anyone who requests one, but it will be attached to the requesting user.  
        /// </summary>
        public static ContentUser AnyoneUser
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();

                if (_anyoneUser == null)
                {
                    _anyoneUser = new ContentUser(AnyoneUserName, AuthenticationType.Internal);
                }
                return _anyoneUser;
            }
        }

        /// <summary>
        /// Returns an instance of the User class that identifies "Owner" persona.
        /// This user has authentication type Internal and Name "Owner".
        /// This is mostly used by the server side templates to give special rights to the 
        /// Publisher/author who would be building a protected document using those templates
        /// </summary>
        public static ContentUser OwnerUser
        {
            get
            {
                SecurityHelper.DemandRightsManagementPermission();

                if (_ownerUser == null)
                {
                    _ownerUser = new ContentUser(OwnerUserName, AuthenticationType.Internal);
                }
                return _ownerUser;
            }
        }

        /// <summary>
        /// Compute hash code.
        /// </summary>
        public override int GetHashCode()
        {
            SecurityHelper.DemandRightsManagementPermission();

            if (!hashCalcIsDone)
            {
                StringBuilder hashString = new StringBuilder(_name.ToUpperInvariant());
                hashString.Append(_authenticationType.ToString());

                hashValue = (hashString.ToString()).GetHashCode();
                hashCalcIsDone = true;
            }

            return hashValue;
        }

        /// <summary>
        /// Converts  AuthenticationType to AuthenticationProviderType string require for a client session 
        /// </summary>
        internal string AuthenticationProviderType
        {
            get
            {
                if (_authenticationType == AuthenticationType.Windows)
                {
                    return WindowsAuthProvider;
                }
                else if (_authenticationType == AuthenticationType.Passport)
                {
                    return PassportAuthProvider;
                }
                else
                {
                    Invariant.Assert(false, "AuthenticationProviderType can only be queried for Windows or Passport authentication");
                    return null;
                }
            }
        }

        /// <summary>
        /// Generic test for equality. This method allows any types based on ContentUser
        /// to be comparable.
        /// </summary>
        /// <SecurityNote>
        ///     TreatAsSafe: This method may be called indirectly from a Partial Trust environment. 
        ///         It only performs a safe string comparison.
        /// </SecurityNote>
        internal bool GenericEquals(ContentUser userObj)
        {
            // this checks for null argument
            if (userObj == null)
            {
                return false;
            }
            else
            {
                return (String.CompareOrdinal(_name.ToUpperInvariant(), userObj._name.ToUpperInvariant()) == 0)
                            &&
                                _authenticationType.Equals(userObj._authenticationType);
            }
        }

        /// <summary>
        /// Implements the IEqualityComparer generic interface to be
        /// used in a Dictionary with ContentUser as key type. 
        /// This interface allows any types based on ContentUser to be
        /// comparable.
        /// </summary>
        /// <SecurityNote>
        ///     TreatAsSafe: The methods of this class may be called indirectly from 
        ///                  a Partial Trust environment. 
        ///         Both methods only validate their parameters, then call other safe methods.
        /// </SecurityNote>
        internal sealed class ContentUserComparer : IEqualityComparer<ContentUser>
        {
            bool IEqualityComparer<ContentUser>.Equals(ContentUser user1, ContentUser user2)
            {
                Invariant.Assert(user1 != null, "user1 should not be null");
                return user1.GenericEquals(user2);
            }

            int IEqualityComparer<ContentUser>.GetHashCode(ContentUser user)
            {
                Invariant.Assert(user != null, "user should not be null");
                return user.GetHashCode();
            }
        }

        /// <summary>
        /// A comparer that can be passed to a Dictionary to allow
        /// generic match for different types based on ContentUser.
        /// </summary>
        /// <SecurityNote>
        ///     TreatAsSafe: This field will be accessed by SecurityTransparent code. It's static, readonly.
        ///             It's safe to allow access from SecurityTransparent code.
        /// </SecurityNote>
        internal static readonly ContentUserComparer _contentUserComparer = new ContentUserComparer();

        internal static bool CompareToAnyone(string name)
        {
            return (0 == String.CompareOrdinal(AnyoneUserName.ToUpperInvariant(), name.ToUpperInvariant()));
        }

        internal static bool CompareToOwner(string name)
        {
            return (0 == String.CompareOrdinal(OwnerUserName.ToUpperInvariant(), name.ToUpperInvariant()));
        }

        private const string WindowsAuthProvider = "WindowsAuthProvider";
        private const string PassportAuthProvider = "PassportAuthProvider";

        private const string OwnerUserName = "Owner";
        private static ContentUser _ownerUser;

        private const string AnyoneUserName = "Anyone";
        private static ContentUser _anyoneUser;

        private string _name;
        private AuthenticationType _authenticationType;
        private int hashValue;
        private bool hashCalcIsDone;    // flag that indicates the value in hasValue is already calculated and usable
    }
}
