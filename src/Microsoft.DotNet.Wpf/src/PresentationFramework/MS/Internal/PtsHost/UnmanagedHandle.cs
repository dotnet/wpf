// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Definition for Unmanaged Handle. Provides identity (handle), 
//              which can be used in unmanaged world. 
//


using System;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Provides identity (handle), which can  be used in unmanaged world.
    /// If object is passed into unmanaged world, and there is a need to identify 
    /// that object later on, its class should inherit from UnmanagedHandle.
    /// </summary>
    internal class UnmanagedHandle : IDisposable
    {
        /// <summary>
        /// Constructor. Used when object derives from UnmanagedHandle.
        /// </summary>
        /// <param name="ptsContext">
        /// PTS context
        /// </param>
        protected UnmanagedHandle(PtsContext ptsContext)
        {
            _ptsContext = ptsContext;
            _handle = ptsContext.CreateHandle(this);
        }

        /// <summary>
        /// Dispose the object and release handle. 
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                _ptsContext.ReleaseHandle(_handle);
            }
            finally
            {
                _handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handle of an object. 
        /// </summary>
        internal IntPtr Handle 
        { 
            get 
            { 
                return _handle; 
            } 
        }
        private IntPtr _handle;

        /// <summary>
        /// PtsContext that is the owner of the handle.
        /// It is required to store it here for Dispose. When Dispose is called
        /// it is not always possible to get instance of PtsContext that
        /// has been used to create this handle. 
        /// </summary>
        internal PtsContext PtsContext { get { return _ptsContext; } }
        private readonly PtsContext _ptsContext;
    }
}
