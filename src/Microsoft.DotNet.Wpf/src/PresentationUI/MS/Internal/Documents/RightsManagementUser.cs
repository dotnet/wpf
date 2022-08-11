// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.RightsManagement;
using System.Windows.TrustUI;

namespace MS.Internal.Documents 
{

    /// <summary>
    /// This class represents a user in the Rights Management system.
    /// </summary>
    /// <remarks>
    /// Class responsibilities:
    ///  1) This class suppresses the RightsManagementPermission by asserting
    ///     for it and marking the respective methods SecurityCritical.
    ///  2) This class has factory methods to construct itself.
    /// 
    /// ContentUser is used pervasively.  The design was chosen to consolidate
    /// the asserts needed by RightsManagementProvider and simply require
    /// callers to be audited for not leaking the information.
    /// </remarks>
    internal class RightsManagementUser : ContentUser
    {
        #region Constructors
        //------------------------------------------------------
        // Constructors
        //------------------------------------------------------

        /// <summary>
        /// Creates a RightsManagementUser object.
        /// </summary>
        /// <param name="name">The name of the user</param>
        /// <param name="authenticationType">The authentication type of the
        /// user</param>
        private RightsManagementUser(string name, AuthenticationType authenticationType)
            : base(name, authenticationType)
        {
        }

        #endregion Constructors

        #region Public Methods
        //--------------------------------------------------------------------------
        // Public Methods
        //--------------------------------------------------------------------------

        /// <summary>
        /// Compute hash code.
        /// </summary>
        /// <remarks>We are breaking encapsulation by caching the hash code.
        /// This is OK as long as no properties on the object can change, which
        /// is the case. We did this for performance reasons, as an assert is
        /// expensive and GetHashCode() is called somewhat frequently.
        /// </remarks>
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                 _hashCode = base.GetHashCode();
            }

            return _hashCode;
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion Public Methods

        #region Internal Methods
        //--------------------------------------------------------------------------
        // Internal Methods
        //--------------------------------------------------------------------------

        /// <summary>
        /// Creates a RightsManagementUser object with the given name and
        /// authentication type.
        /// </summary>
        /// <param name="name">The user name</param>
        /// <param name="authenticationType">The user authentication type
        /// </param>
        /// <returns>A RightsManagementUser with the specified properties
        /// </returns>
        internal static RightsManagementUser CreateUser(
            string name,
            AuthenticationType authenticationType)
        {
            return new RightsManagementUser(name, authenticationType);
        }

        /// <summary>
        /// Creates a RightsManagementUser object from the given ContentUser
        /// object.
        /// </summary>
        /// <param name="user">The ContentUser to copy</param>
        /// <returns>A RightsManagementUser that has the same properties as the
        /// user passed in as an argument</returns>
        internal static RightsManagementUser CreateUser(ContentUser user)
        {
            return new RightsManagementUser(
                user.Name,
                user.AuthenticationType);
        }

        #endregion Internal Methods

        #region Internal Properties
        //--------------------------------------------------------------------------
        // Internal Properties
        //--------------------------------------------------------------------------

        /// <summary>
        /// Returns the authentication type of the user.
        /// </summary>
        internal new AuthenticationType AuthenticationType
        { 
            get
            {
                return base.AuthenticationType;
            }
        }

        /// <summary>
        /// Fully qualified e-mail address of the user.
        /// </summary>
        internal new string Name
        {
            get
            {
                string name = string.Empty;

                // Determine if the current RightsManagementUser represents the AnyoneUser.
                if (AnyoneRightsManagementUser.Equals(this))
                {
                    // Since this is the AnyoneUser return the localized representation for the name.
                    name = SR.Get(SRID.RMPublishingAnyoneUserDisplay);
                }
                else
                {
                    // Since this is not the AnyoneUser, use name from the RightsManagementUser.
                    name = base.Name;
                }

                return name;
            }
        }

        /// <summary>
        /// Returns an instance of the User class that identifyes "Anyone" persona.
        /// This user has authentication type "Internal" and Name "Anyone".
        /// If this such user was granted rights dutring publishing; server will issue Use License 
        /// to anyone who requests one, but it will be attached to the requesting user.  
        /// </summary>
        internal new static ContentUser AnyoneUser
        {
            get
            {
                 return ContentUser.AnyoneUser;
            }
        }

        /// <summary>
        /// Returns an instance of the RightsManagementUser class corresponding
        /// to ContentUser.AnyoneUser.
        /// </summary>
        internal static RightsManagementUser AnyoneRightsManagementUser
        {
            get
            {
                if (_anyoneUserInstance.Value == null)
                {
                    _anyoneUserInstance.Value = CreateUser(AnyoneUser);
                }

                return _anyoneUserInstance.Value;
            }
        }

        #endregion Internal Properties

        #region Private Fields
        //--------------------------------------------------------------------------
        // Private Fields
        //--------------------------------------------------------------------------

        /// <summary>
        /// The Anyone user as a RightsManagementUser.
        /// </summary>
        private static SecurityCriticalDataForSet<RightsManagementUser> _anyoneUserInstance;

        private int _hashCode;

        #endregion Private Fields
    }
}
