// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft Windows Presentation Foundation
//

using System;
using System.IO;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Windows.Navigation;
using System.IO.Packaging;
using MS.Internal.PresentationCore; 

using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows.Media.Effects
{
    public sealed partial class PixelShader : Animatable, DUCE.IResource
    {
        // Method and not property so we don't need to hang onto the stream.
        public void SetStreamSource(Stream source)
        {
            WritePreamble();
            LoadPixelShaderFromStreamIntoMemory(source);
            WritePostscript();
        }

        /// <summary>
        /// The major version of the pixel shader
        /// </summary>
        internal short ShaderMajorVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// The minor version of the pixel shader
        /// </summary>
        internal short ShaderMinorVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// If any PixelShader cannot be processed by the render thread, this
        /// event will be raised.  
        /// </summary>
        public static event EventHandler InvalidPixelShaderEncountered
        {
            add
            {
                // Just forward to the internal event on MediaContext
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                mediaContext.InvalidPixelShaderEncountered += value;
            }
            remove
            {
                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                mediaContext.InvalidPixelShaderEncountered -= value;
            }
        }

        /// <summary>
        /// This method is invoked whenever the source property changes. 
        /// </summary>
        private void UriSourcePropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            // Decided against comparing the URI because the user might want to change the shader on the filesystem
            // and reload it. 

            // We do not support async loading of shaders here. If that is desired the user needs to use the SetStreamSource
            // API.
            
            Uri newUri = (Uri)e.NewValue;
            Stream stream = null;

            try {                    
                if (newUri != null)
                {
                    if (!newUri.IsAbsoluteUri)
                    {
                         newUri = BaseUriHelper.GetResolvedUri(BaseUriHelper.BaseUri, newUri);
                    }

                    Debug.Assert(newUri.IsAbsoluteUri);

                    // Now the URI is an absolute URI.

                    //
                    // Only allow file and pack URIs.
                    if (!newUri.IsFile && 
                        !MS.Internal.IO.Packaging.PackUriHelper.IsPackUri(newUri))
                    {
                        throw new ArgumentException(SR.Get(SRID.Effect_SourceUriMustBeFileOrPack));
                    }

                    stream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(newUri);
                }

                LoadPixelShaderFromStreamIntoMemory(stream);            
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads the byte code for the pixel shader into a local byte array. If the stream is null, the byte array
        /// will be empty (length 0). The compositor will use an identity shader.
        /// </summary>
        private void LoadPixelShaderFromStreamIntoMemory(Stream source) 
        {
            
            _shaderBytecode = new SecurityCriticalData<byte[]>(null);
            
            if (source != null)
            {
                if (!source.CanSeek)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Effect_ShaderSeekableStream));
                }

                int len = (int)source.Length;  // only works on seekable streams.

                if (len % sizeof(int) != 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Effect_ShaderBytecodeSize));
                }
                
                BinaryReader br = new BinaryReader(source);
                _shaderBytecode = new SecurityCriticalData<byte[]>(new byte[len]);
                int lengthRead = br.Read(_shaderBytecode.Value, 0, (int)len);

                //
                // The first 4 bytes contain version info in the form of
                // [Minor][Major][xx][xx]
                //
                if (_shaderBytecode.Value != null && _shaderBytecode.Value.Length > 3)
                {
                    ShaderMajorVersion = _shaderBytecode.Value[1];
                    ShaderMinorVersion = _shaderBytecode.Value[0];
                }
                else
                {
                    ShaderMajorVersion = ShaderMinorVersion = 0;
                }

                Debug.Assert(len == lengthRead); 
            }

            // We received new stream data. Need to register for a async update to update the composition
            // engine. 
            RegisterForAsyncUpdateResource();

            //
            // Notify any ShaderEffects listening that the bytecode changed.
            // The bytecode may have changed from a ps_3_0 shader to a ps_2_0 shader, and any
            // ShaderEffects using this PixelShader need to check that they are using only
            // registers that are valid in ps_2_0.
            //
            if (_shaderBytecodeChanged != null)
            {
                _shaderBytecodeChanged(this, null);
            }
        }


        private void ManualUpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                checked
                {
                    DUCE.MILCMD_PIXELSHADER data;
                    data.Type = MILCMD.MilCmdPixelShader;
                    data.Handle = _duceResource.GetHandle(channel);
                    data.PixelShaderBytecodeSize = (_shaderBytecode.Value == null) ? 0 : (uint)(_shaderBytecode.Value).Length;
                    data.ShaderRenderMode = ShaderRenderMode;
                    data.CompileSoftwareShader = CompositionResourceManager.BooleanToUInt32(!(ShaderMajorVersion == 3 && ShaderMinorVersion == 0));

                    unsafe
                    {
                        channel.BeginCommand(
                            (byte*)&data,                      
                            sizeof(DUCE.MILCMD_PIXELSHADER),
                            (int)data.PixelShaderBytecodeSize);   // extra data

                            if (data.PixelShaderBytecodeSize > 0)
                            {
                                fixed (byte *pPixelShaderBytecode = _shaderBytecode.Value)
                                {
                                    channel.AppendCommandData(pPixelShaderBytecode, (int)data.PixelShaderBytecodeSize);
                                }
                            }
                    }
                }

                channel.EndCommand();
            }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            PixelShader shader = (PixelShader)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(shader);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            PixelShader shader = (PixelShader)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(shader);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            PixelShader shader = (PixelShader)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(shader);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            PixelShader shader = (PixelShader)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(shader);
        }

        /// <summary>
        /// Clones values that do not have corresponding DPs.
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(PixelShader shader)
        {
            byte[] sourceBytecode = shader._shaderBytecode.Value;
            byte[] destinationBytecode = null;

            if (sourceBytecode != null)
            {
                destinationBytecode = new byte[sourceBytecode.Length];
                sourceBytecode.CopyTo(destinationBytecode, 0);
            }                          

            _shaderBytecode = new SecurityCriticalData<byte[]>(destinationBytecode);
        }

        private SecurityCriticalData<byte[]> _shaderBytecode;

        //
        // Introduced with ps_3_0 for ShaderEffect's use
        //
        // The scenario is setting up a ShaderEffect with a PixelShader containing a ps_3_0 shader,
        // setting a ps_3_0 only register (int, bool, or float registers 32 and above), then using
        // ShaderEffect.PixelShader.UriSource (or SetStreamSource) to swap the ps_3_0 shader out
        // for a ps_2_0 one. Now there's a value in a register that doesn't exist in ps_2_0.
        //
        // The fix is to have ShaderEffect listen for this event on its PixelShader, and verifying
        // that no invalid registers are used when switching a ps_3_0 register to a ps_2_0 one.
        //
        internal event EventHandler _shaderBytecodeChanged;
    }
}

