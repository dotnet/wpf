// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The RenderCapability class allows clients to query for the current
//      render tier associated with their Dispatcher and to register for 
//      notification on change.
//

using System;
using System.Diagnostics;

namespace System.Windows.Media
{
    /// <summary>
    /// RenderCapability - 
    ///   The RenderCapability class allows clients to query for the current
    ///   render tier associated with their Dispatcher and to register for 
    ///   notification on change.
    /// </summary>
    public static class RenderCapability
    {
        /// <summary>
        /// Tier Property - returns the current render tier for the Dispatcher associated
        /// with the current thread.
        /// </summary>
        public static int Tier
        {
            get
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;

                // The Dispatcher auto-creates if there is no Dispatcher associated with this
                // thread, and the MediaContext does the same.  Thus, mediaContext should never
                // be null.
                Debug.Assert(mediaContext != null);

                return mediaContext.Tier;
            }
        }

        /// <summary>
        /// Returns whether the specified PixelShader major/minor version is
        /// supported by this version of WPF, and whether Effects using the
        /// specified major/minor version can run on the GPU.
        /// </summary>
        public static bool IsPixelShaderVersionSupported(short majorVersionRequested, short minorVersionRequested)
        {
            bool isSupported = false;

            //
            // For now, we only support PS 2.0 and 3.0.  Can only return true if this is
            // the version asked for.
            //
            if (majorVersionRequested == 2 && minorVersionRequested == 0 ||
                majorVersionRequested == 3 && minorVersionRequested == 0)
            {
                // Now actually check.
                
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                byte majorVersion = (byte)((mediaContext.PixelShaderVersion >> 8) & 0xFF);
                byte minorVersion = (byte)((mediaContext.PixelShaderVersion >> 0) & 0xFF);

                // We assume here that a higher version does in fact support the
                // version we're requiring.
                if (majorVersion >= majorVersionRequested)
                {
                    isSupported = true;
                }
                else if (majorVersion == majorVersionRequested && minorVersion >= minorVersionRequested)
                {
                    isSupported = true;
                }
            }

            return isSupported;
        }

        /// <summary>
        /// Returns whether Effects can be rendered in software on this machine.
        /// </summary>
        public static bool IsPixelShaderVersionSupportedInSoftware(short majorVersionRequested, short minorVersionRequested)
        {
            bool isSupported = false;

            //
            // Software rendering is only supported for PS 2.0.
            //
            if (majorVersionRequested == 2 && minorVersionRequested == 0)
            {
                // Now actually check.
                
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                isSupported = mediaContext.HasSSE2Support;
            }

            return isSupported;
        }

        /// <summary>
        /// Returns whether Effects can be rendered in software on this machine.
        /// </summary>
        [Obsolete(IsShaderEffectSoftwareRenderingSupported_Deprecated)]
        public static bool IsShaderEffectSoftwareRenderingSupported
        {
            get
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                return mediaContext.HasSSE2Support;
            }
        }

        /// <summary>
        /// Returns the maximum number of instruction slots supported.
        /// The number of instruction slots supported by PS 3.0 varies, but will be at least 512.
        /// </summary>
        public static int MaxPixelShaderInstructionSlots(short majorVersionRequested, short minorVersionRequested)
        {
            if (majorVersionRequested == 2 && minorVersionRequested == 0)
            {
                // ps_2_0 supports 32 texture + 64 arithmetic = 96 instruction slots.
                return 96;
            }
            else if (majorVersionRequested == 3 && minorVersionRequested == 0)
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                return (int)mediaContext.MaxPixelShader30InstructionSlots;
            }
            else
            {
                // anything other than ps_2_0 and ps_3_0 are not supported.
                return 0;
            }
        }

        /// <summary>
        /// Returns the maximum width and height for texture creation of the underlying
        /// hardware device.  If there are multiple devices, this returns the minumum size
        /// among them.
        /// </summary>
        public static Size MaxHardwareTextureSize 
        {
            get
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                return mediaContext.MaxTextureSize;
            }
        }
        
        /// <summary>
        /// TierChanged event - 
        /// This event is raised when the Tier for a given Dispatcher changes.
        /// </summary>
        public static event EventHandler TierChanged
        {
            add
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;

                // The Dispatcher auto-creates if there is no Dispatcher associated with this
                // thread, and the MediaContext does the same.  Thus, mediaContext should never
                // be null.
                Debug.Assert(mediaContext != null);

                mediaContext.TierChanged += value;
            }
            remove
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;

                // The Dispatcher auto-creates if there is no Dispatcher associated with this
                // thread, and the MediaContext does the same.  Thus, mediaContext should never
                // be null.
                Debug.Assert(mediaContext != null);

                mediaContext.TierChanged -= value;
            }
        }

        private const string IsShaderEffectSoftwareRenderingSupported_Deprecated = "IsShaderEffectSoftwareRenderingSupported property is deprecated.  Use IsPixelShaderVersionSupportedInSoftware static method instead.";
    }
}
