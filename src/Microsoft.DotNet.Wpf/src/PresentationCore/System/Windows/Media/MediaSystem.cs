// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+----------------------------------------------------------------------------
//
//
//  Abstract:
//     Media system holds the relation between an application 
//     domain and the underlying transport system. 
// 

using System;
using System.Windows.Threading;

using System.Collections;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using Microsoft.Win32;
using MS.Internal;
using MS.Internal.FontCache;
using MS.Win32;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods.MilCoreApi;
using SafeNativeMethods=MS.Win32.PresentationCore.SafeNativeMethods;

namespace System.Windows.Media
{
    /// <summary>
    /// The MediaSystem class controls the media layer.
    /// </summary>
    /// <remarks>
    /// Use <see cref="MediaSystem.Startup"/> to start up the media system and <see cref="MediaSystem.Shutdown"/> to
    /// shut down the mediasystem.
    /// </remarks>
    internal static class MediaSystem
    {
        /// <summary>
        /// This function initializes the MediaSystem. It must be called before any functions in the Media namespace
        /// can be used.
        /// </summary>
        /// <seealso cref="Shutdown"/>
        public static bool Startup(MediaContext mc)
        {
            //
            // Note to stress triagers:
            //
            // This call will fail if PresentationCore.dll and milcore.dll have mismatched
            // versions -- please make sure that both binaries have been properly built
            // and deployed. 
            //
            // *** Failure here does NOT indicate a bug in MediaContext.Startup! ***
            //

            HRESULT.Check(UnsafeNativeMethods.MilVersionCheck(MS.Internal.Composition.Version.MilSdkVersion));
            
            using (CompositionEngineLock.Acquire())
            {
                _mediaContexts.Add(mc);
                
                //Is this the first startup?
                if (0 == s_refCount)
                {
                    HRESULT.Check(SafeNativeMethods.MilCompositionEngine_InitializePartitionManager(
                                  0 // THREAD_PRIORITY_NORMAL
                                  )); 

                    s_forceSoftareForGraphicsStreamMagnifier =
                        UnsafeNativeMethods.WgxConnection_ShouldForceSoftwareForGraphicsStreamClient();

                    ConnectTransport();

                    // Read a flag from the registry to determine whether we should run
                    // animation smoothing code.
                    ReadAnimationSmoothingSetting();
                }
                s_refCount++;
            }
            // Consider making MediaSystem.ConnectTransport return the state of transport connectedness so 
            // that we can initialize the media system to a disconnected state.

            return true;
        }

        internal static bool ConnectChannels(MediaContext mc)
        {
            bool fCreated = false;

            using (CompositionEngineLock.Acquire())
            {
                if (IsTransportConnected)
                {
                    mc.CreateChannels();
                    fCreated = true;
                }
            }

            return fCreated;
        }

        /// <summary>
        /// Reads a value from the registry to decide whether to disable the animation
        /// smoothing algorithm.
        /// </summary>
        /// <remarks>
        /// The code is only present in internal builds
        /// </remarks>
        private static void ReadAnimationSmoothingSetting()
        {
#if PRERELEASE
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Avalon.Graphics");
            if (key != null)
            {
                object keyValue = key.GetValue("AnimationSmoothing");

                // The Regkey now turns off AnimationSmoothing
                s_animationSmoothing = !(keyValue is int && ((int)keyValue) == 0);
            }
#endif
        }

        /// <summary>
        /// This deinitializes the MediaSystem and frees any resources that it maintains.
        /// </summary>
        internal static void Shutdown(MediaContext mc)
        {
            using (CompositionEngineLock.Acquire())
            {
                Debug.Assert(s_refCount > 0);
                _mediaContexts.Remove(mc);

                s_refCount--;
                if (0 == s_refCount)
                {
                    // We can shut-down.
                    // Debug.WriteLine("MediSystem::NotifyDisconnect Stop Transport\n");
                   
                    if (IsTransportConnected)
                    {
                        DisconnectTransport();
                    }

                    HRESULT.Check(SafeNativeMethods.MilCompositionEngine_DeinitializePartitionManager());
                }
            }
        }

        /// <summary>
        /// Handle DWM messages that indicate that the state of the connection needs to change.
        /// </summary>
        internal static void NotifyRedirectionEnvironmentChanged()
        {
            using (CompositionEngineLock.Acquire())
            {
                // Check to see if we need to force software for the Vista Magnifier
                s_forceSoftareForGraphicsStreamMagnifier = 
                    UnsafeNativeMethods.WgxConnection_ShouldForceSoftwareForGraphicsStreamClient();

                foreach (MediaContext mc in _mediaContexts)
                {
                    mc.PostInvalidateRenderMode();
                }
            }
        }
        
        /// <summary>
        /// Connect the transport.
        /// </summary>
        private static void ConnectTransport()
        {
            if (IsTransportConnected)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.MediaSystem_OutOfOrderConnectOrDisconnect));
            }

            //
            // Create a default transport to be used by this media system. 
            // If creation fails, fall back to a local transport.
            //

            HRESULT.Check(UnsafeNativeMethods.WgxConnection_Create(
                false, // false means asynchronous transport
                out s_pConnection));

            // Create service channel used by global glyph cache. This channel is
            // the first channel created for the app, and by creating it with
            // a null channel reference it creates a new partition.
            // All subsequent channel creates will pass in a reference to this
            // channel thus using its partition.
            s_serviceChannel = new DUCE.Channel(
                null,
                false,       // not out of band
                s_pConnection,
                false);

            IsTransportConnected = true;
        }    

        /// <summary>
        /// Disconnect the transport. If we are calling this function from a disconnect
        /// request we want to keep the service channel around. So that media contexts that
        /// have not yet received disconnect event do not crash.
        /// </summary>
        private static void DisconnectTransport()
        {
            if (!IsTransportConnected)
            {
                return;
            }

            // Close global glyph cache channel.
            s_serviceChannel.Close();

            HRESULT.Check(UnsafeNativeMethods.WgxConnection_Disconnect(s_pConnection));

            // Release references to global glyph cache and service channel.
            s_serviceChannel = null;
            s_pConnection = IntPtr.Zero;

            IsTransportConnected = false;
        }

        /// <summary>
        /// Checks if to CAO have the same context affinity. This is for example important for
        /// ContainerVisual.Children.Add to ensure that the scene graph is homogenously build out
        /// of object that have the same context affinity.
        /// </summary>
        /// <param name="reference">Reference to which to compare to. This argument is usually the this
        /// pointer and can not be null.</param>
        /// <param name="other">Object for which the check is performed.</param>
        /// <remarks>
        /// Example:
        ///
        /// class Visual
        /// {
        ///     ...
        ///     void Add(Visual child)
        ///     {
        ///         VerifyContext(this);
        ///         AssertSameContext(this, child);
        ///         ...
        ///     }
        /// }
        ///
        /// Note that VerifyContext(A) AND AssertSameContext(A, B) implies that VerifyContext(B) holds. Hence you
        /// don't need to check the context for each argument if you assert the same context.
        /// </remarks>
        internal static void AssertSameContext(
            DispatcherObject reference,
            DispatcherObject other)
        {
            Debug.Assert(reference != null, "The reference object can not be null.");

            // DispatcherObjects may be created with the option of becoming unbound.
            // An unbound DO may be created without a Dispatcher or may detach from
            // its Dispatcher (e.g., a Freezable).  Unbound DOs return null for their
            // Dispatcher and should be accepted anywhere.
            if (other != null &&
                reference.Dispatcher != null &&
                other.Dispatcher != null &&
                reference.Dispatcher != other.Dispatcher)
            {
                throw new ArgumentException(SR.Get(SRID.MediaSystem_ApiInvalidContext));
            }
        }

        /// <summary>
        /// This flag indicates if the transport has been disabled by a session disconnect.
        /// If the transport has been disabled we need to defer all media system startup requests
        /// until we get the next connect message
        /// </summary>
        internal static bool IsTransportConnected
        {
            get { return s_isConnected; }
            set { s_isConnected = value; }
        }

        /// <summary>
        /// This flag indicates if all rendering should be in software.
        /// </summary>
        internal static bool ForceSoftwareRendering
        {
            get 
            {
                using (CompositionEngineLock.Acquire())
                {
                    return s_forceSoftareForGraphicsStreamMagnifier;
                }
            }
        }

        /// <summary>
        /// Returns the service channel for the current media system. This channel 
        /// is used by the glyph cache infrastructure.
        /// </summary>
        internal static DUCE.Channel ServiceChannel
        {
            get { return s_serviceChannel; }
        }

        /// <summary>
        /// Returns the pointer to the unmanaged transport object.
        /// </summary>
        internal static IntPtr Connection
        {
            get { return s_pConnection; }
        }

        internal static bool AnimationSmoothing
        {
            get { return s_animationSmoothing; }
        }

        /// <summary>
        /// Keeps track of how often MediaSystem.Startup is called. So that the MediaSystem can be shut down at the right
        /// point in time.
        /// </summary>
        private static int s_refCount = 0;

        private static ArrayList _mediaContexts = new ArrayList();

        private static bool s_isConnected = false;

        /// <summary>
        /// Service channel to serve global glyph cache.
        /// </summary>
        private static DUCE.Channel s_serviceChannel;

        private static bool s_animationSmoothing = true;

        /// <summary>
        /// Pointer to the unmanaged transport object.
        /// </summary>
        private static IntPtr s_pConnection;

        /// <summary>
        /// Indicates if a graphics stream client is present. If a graphics stream client is present,
        /// we drop back to sw rendering to enable the Vista magnifier. 
        /// </summary>
        private static bool s_forceSoftareForGraphicsStreamMagnifier;
     }
}

