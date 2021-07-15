// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
//---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Runtime.InteropServices;
using MS.Internal;
using MS.Internal.FontCache;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    /// <summary>
    /// Master Glyph Cache class
    /// </summary>
    internal class GlyphCache
    {
        /// <summary>
        /// Callback delegate.
        /// </summary>
        public delegate int CreateGlyphsCallbackDelegate(
            IntPtr /*CMilSlaveGlyphCache | CMilSlaveGlyphRun* */ nativeObject,
            IntPtr /*GLYPH_BITMAP_CREATE_REQUEST | GLYPH_GEOMETRY_CREATE_REQUEST* */ request,
            ushort       isGeometryRequest);
        
        private DUCE.Resource _duceResource = new DUCE.Resource();
        private SafeReversePInvokeWrapper _reversePInvokeWrapper;

        internal DUCE.ResourceHandle Handle
        {
            get
            {
                return _duceResource.Handle;
            }
        }


        // Service channel that serves for both
        // pre-commit and post-commit actions
        internal DUCE.Channel _channel;

        internal void RemoveFromChannel()
        {
            if (_channel != null)
            {
                _duceResource.ReleaseOnChannel(_channel);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        internal GlyphCache(DUCE.Channel channel)
        {
            _channel = channel;
            
            Debug.Assert(_channel != null);
            _duceResource.CreateOrAddRefOnChannel(this, _channel, DUCE.ResourceType.TYPE_GLYPHCACHE);
            SendCallbackEntryPoint();
        }

        /// <summary>
        /// Sends a callback pointer to this glyphcache for glyph generation requests.
        /// </summary>
        private unsafe void SendCallbackEntryPoint()
        {
            _createGlyphBitmapsCallbackDelegate = new CreateGlyphsCallbackDelegate(FontCacheAccessor.CreateGlyphsCallback);

            IntPtr fcn = Marshal.GetFunctionPointerForDelegate(_createGlyphBitmapsCallbackDelegate);
            _reversePInvokeWrapper = new SafeReversePInvokeWrapper(fcn);
           
            DUCE.MILCMD_GLYPHCACHE_SETCALLBACK cmd;
            cmd.Type = MILCMD.MilCmdGlyphCacheSetCallback;
            cmd.Handle = Handle;

            // AddRef the reverse p-invoke wrapper while it is being transferred across the channel. There is a
            // small chance we would leak the wrapper. More specifically, if the app domain is shut down before
            // the wrapper is picked up by the composition engine.
            UnsafeNativeMethods.MILUnknown.AddRef(_reversePInvokeWrapper);
            cmd.CallbackPointer = (UInt64)_reversePInvokeWrapper.DangerousGetHandle();

            _channel.SendCommand((byte*)&cmd, sizeof(DUCE.MILCMD_GLYPHCACHE_SETCALLBACK), false /* sendInSeparateBatch */);
        }

        private CreateGlyphsCallbackDelegate _createGlyphBitmapsCallbackDelegate;                
    }
}
