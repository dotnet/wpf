// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;

namespace MS.Internal.Utility
{
    #region SponsorHelper Class
    /// <summary>
    /// We either mark the Sponsor as MarshalByRef or make it serializable.
    /// If we make it MarshalByRef, then this sponsor which is used to control
    /// the lifetime of a MBR object in turn needs to have another sponsor OR
    /// the sponsor can mark itself to remain alive for the life of the AppDomain
    /// by overriding InitializeLifetimeService and returning null OR the object 
    /// can be marked as Serializeable instead of MBR in which case it is marshaled
    /// by value to the client appdomain and will not have the state of the host
    /// appdomain to make renewal decisions. In our case we don't have any state so
    /// its easier and better perf-wise to mark it Serializable.
    /// </summary>
    [Serializable]
    internal class SponsorHelper : ISponsor
    {
        #region Private Data
        private ILease _lease;
        private TimeSpan _timespan;
        #endregion Private Data

        #region Constructor
        internal SponsorHelper(ILease lease, TimeSpan timespan)
        {
            Debug.Assert(lease != null && timespan != null, "Lease and TimeSpan arguments cannot be null");
            _lease = lease;
            _timespan = timespan;
        }
        #endregion Constructor

        #region ISponsor Interface
        TimeSpan ISponsor.Renewal(ILease lease)
        {
            if (lease == null)
            {
                throw new ArgumentNullException("lease");
            }

            return _timespan;
        }
        #endregion ISponsor Interface

        #region Internal Methods
        /// <SecurityNote>
        /// Critical - asserts permission for RemotingConfiguration
        /// TreatAsSafe - The constructor for this object is internal and this function does not take 
        /// random parameters and hence can’t be used to keep random objects alive or access any other object
        /// in the application. 
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        [SecurityPermissionAttribute(SecurityAction.Assert, RemotingConfiguration = true)]
        internal void Register()
        {
            _lease.Register((ISponsor)this);
        }

        /// <SecurityNote>
        /// Critical - asserts permission for RemotingConfiguration
        /// TreatAsSafe - The constructor for this object is internal and this function does not take 
        /// random parameters and hence can’t be used to keep random objects alive or access any other object
        /// in the application. 
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        [SecurityPermissionAttribute(SecurityAction.Assert, RemotingConfiguration = true)]
        internal void Unregister()
        {
            _lease.Unregister((ISponsor)this);
        }
        #endregion Internal Methods
    }
    #endregion SponsorHelper Class
}
