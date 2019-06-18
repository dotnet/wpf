// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The ChannelManager is a helper structure internal to MediaContext
//      that abstracts out channel creation, storage and destruction.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Security;

using MS.Internal;
using MS.Utility;
using MS.Win32;

using Microsoft.Win32.SafeHandles;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods.MilCoreApi;

namespace System.Windows.Media
{
    partial class MediaContext
    {
        /// <summary>
        /// A helper structure that abstracts channel management.
        /// </summary>
        /// <remarks>
        /// The common agreement (avcomptm, avpttrev) is that channel creation 
        /// should be considered unsafe and therefore subject to control via 
        /// the SecurityCritical/SecurityTreatAsSafe mechanism.
        /// 
        /// On the other hand, using a channel that has been created by the 
        /// MediaContext is considered safe. 
        ///
        /// This class expresses the above policy by hiding channel creation
        /// and storage within the MediaContext while allowing to access 
        /// the channels at will.
        /// </remarks>
        private struct ChannelManager
        {
            /// <summary>
            /// Opens an asynchronous channel.
            /// </summary>
            internal void CreateChannels()
            {
                Invariant.Assert(_asyncChannel == null);
                Invariant.Assert(_asyncOutOfBandChannel == null);

                // Create a channel into the async composition device.
                // Pass in a reference to the global mediasystem channel so that it uses
                // the same partition.
                _asyncChannel = new DUCE.Channel(
                    System.Windows.Media.MediaSystem.ServiceChannel,
                    false,      // not out of band
                    System.Windows.Media.MediaSystem.Connection,
                    false // sync transport
                    );

                _asyncOutOfBandChannel = new DUCE.Channel(
                    System.Windows.Media.MediaSystem.ServiceChannel,
                    true,       // out of band
                    System.Windows.Media.MediaSystem.Connection,
                    false // sync transport
                    );
            }

            /// <summary>
            /// Closes all opened channels.
            /// </summary>
            internal void RemoveSyncChannels()
            {
                //
                // Close the synchronous channels.
                //
                
                if (_freeSyncChannels != null)
                {
                    while (_freeSyncChannels.Count > 0) 
                    {
                        _freeSyncChannels.Dequeue().Close();
                    }
                    _freeSyncChannels = null;
                }

                if (_syncServiceChannel != null)
                {
                    _syncServiceChannel.Close();

                    _syncServiceChannel = null;
                }
            }

            /// <summary>
            /// Closes all opened channels.
            /// </summary>
            internal void RemoveChannels()
            {
                if (_asyncChannel != null)
                {
                    _asyncChannel.Close();
                    _asyncChannel = null;
                }

                if (_asyncOutOfBandChannel != null)
                {
                    _asyncOutOfBandChannel.Close();
                    _asyncOutOfBandChannel = null;
                }
               
                RemoveSyncChannels();

                if (_pSyncConnection != IntPtr.Zero)
                {
                    HRESULT.Check(UnsafeNativeMethods.WgxConnection_Disconnect(_pSyncConnection));
                    _pSyncConnection = IntPtr.Zero;
                }
}

            /// <summary>
            /// Create a fresh or fetch one from the pool synchronous channel.
            /// </summary>
            internal DUCE.Channel AllocateSyncChannel()
            {
                DUCE.Channel syncChannel;

                if (_pSyncConnection == IntPtr.Zero)
                {
                    HRESULT.Check(UnsafeNativeMethods.WgxConnection_Create(
                        true, // true means synchronous transport
                        out _pSyncConnection));
                }

                if (_freeSyncChannels == null)
                {
                    //
                    // Ensure the free sync channels queue...
                    //

                    _freeSyncChannels = new Queue<DUCE.Channel>(3);
                }

                if (_freeSyncChannels.Count > 0) 
                {
                    //
                    // If there is a free sync channel in the queue, we're done:
                    //
                    
                    return _freeSyncChannels.Dequeue();
                }
                else
                {
                    //
                    // Otherwise, create a new channel. We will try to cache it
                    // when the ReleaseSyncChannel call is made. Also ensure the
                    // synchronous service channel and glyph cache.
                    //
                    
                    if (_syncServiceChannel == null) 
                    {
                        _syncServiceChannel  = new DUCE.Channel(
                            null,
                            false,      // not out of band
                            _pSyncConnection,
                            true        // synchronous
                            );
                    }
                    
                    syncChannel = new DUCE.Channel(
                        _syncServiceChannel,
                        false,      // not out of band
                        _pSyncConnection,
                        true        // synchronous
                        );

                    return syncChannel;
                }
            }

            /// <summary>
            /// Returns a sync channel back to the pool.
            /// </summary>
            internal void ReleaseSyncChannel(DUCE.Channel channel)
            {
                Invariant.Assert(_freeSyncChannels != null);

                //
                // A decision needs to be made whether or not we're interested
                // in re-using this channel later. For now, store up to three
                // synchronous channel in the queue.
                //
                
                if (_freeSyncChannels.Count <= 3) 
                {
                    _freeSyncChannels.Enqueue(channel);
                }
                else
                {
                    channel.Close();
                }
}

            /// <summary>
            /// Returns the asynchronous channel.
            /// </summary>
            internal DUCE.Channel Channel
            {
                get
                {
                    return _asyncChannel;
                }
            }

            /// <summary>
            /// Returns the asynchronous out-of-band channel.
            /// </summary>
            internal DUCE.Channel OutOfBandChannel
            {
                get
                {
                    return _asyncOutOfBandChannel;
                }
            }
            
            /// <summary>
            /// The asynchronous channel.
            /// </summary>
            /// <remarks>
            /// This field will be replaced with an asynchronous channel table as per task #26681.
            /// </remarks>
            private DUCE.Channel _asyncChannel;


            /// <summary>
            /// The asynchronous out-of-band channel.
            /// </summary>
            /// <remarks>
            /// This is needed by CompositionTarget for handling WM_PAINT, etc.
            /// </remarks>
            private DUCE.Channel _asyncOutOfBandChannel;

            /// <summary>
            /// We store the free synchronous channels for later re-use in this queue.
            /// </summary>
            private Queue<DUCE.Channel> _freeSyncChannels;

            /// <summary>
            /// The service channel to the synchronous partition.
            /// </summary>
            private DUCE.Channel _syncServiceChannel;

            /// <summary>
            /// Pointer to the unmanaged connection object.
            /// </summary>
            private IntPtr _pSyncConnection;
        }
    }
}
