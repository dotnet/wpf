// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft Windows Presentation Foundation
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Markup;
using System.Windows.Media.Composition;
using System.Windows.Media.Media3D;
using System.Security;
using System.Runtime.InteropServices;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Effects
{
    public abstract partial class ShaderEffect : Effect
    {
        /// <summary>
        /// Takes in content bounds, and returns the bounds of the rendered
        /// output of that content after the Effect is applied.
        /// </summary>
        internal override Rect GetRenderBounds(Rect contentBounds)
        {
            Point topLeft = new Point();
            Point bottomRight = new Point();

            topLeft.X = contentBounds.TopLeft.X - PaddingLeft;
            topLeft.Y = contentBounds.TopLeft.Y - PaddingTop;
            bottomRight.X = contentBounds.BottomRight.X + PaddingRight;
            bottomRight.Y = contentBounds.BottomRight.Y + PaddingBottom;

            return new Rect(topLeft, bottomRight);
        }

        /// <summary>
        /// Padding is used to specify that an effect's output texture is larger than its input
        /// texture in a specific direction, e.g. for a drop shadow effect.
        /// </summary>
        protected double PaddingTop
        {
            get
            {
                ReadPreamble();
                return _topPadding;
            }
            set
            {
                WritePreamble();
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.Get(SRID.Effect_ShaderEffectPadding));
                }
                else
                {
                    _topPadding = value;
                    RegisterForAsyncUpdateResource();
                }
                WritePostscript();
            }
        }

        /// <summary>
        /// Padding is used to specify that an effect's output texture is larger than its input
        /// texture in a specific direction, e.g. for a drop shadow effect.
        /// </summary>
        protected double PaddingBottom
        {
            get
            {
                ReadPreamble();
                return _bottomPadding;
            }
            set
            {
                WritePreamble();
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.Get(SRID.Effect_ShaderEffectPadding));
                }
                else
                {
                    _bottomPadding = value;
                    RegisterForAsyncUpdateResource();
                }
                WritePostscript();
            }
        }

        /// <summary>
        /// Padding is used to specify that an effect's output texture is larger than its input
        /// texture in a specific direction, e.g. for a drop shadow effect.
        /// </summary>
        protected double PaddingLeft
        {
            get
            {
                ReadPreamble();
                return _leftPadding;
            }
            set
            {
                WritePreamble();
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.Get(SRID.Effect_ShaderEffectPadding));
                }
                else
                {
                    _leftPadding = value;
                    RegisterForAsyncUpdateResource();
                }
                WritePostscript();
            }
        }

        /// <summary>
        /// Padding is used to specify that an effect's output texture is larger than its input
        /// texture in a specific direction, e.g. for a drop shadow effect.
        /// </summary>
        protected double PaddingRight
        {
            get
            {
                ReadPreamble();
                return _rightPadding;
            }
            set
            {
                WritePreamble();
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.Get(SRID.Effect_ShaderEffectPadding));
                }
                else
                {
                    _rightPadding = value;
                    RegisterForAsyncUpdateResource();
                }
                WritePostscript();
            }
        }

        /// <summary>
        /// To specify a shader constant register to set to the size of the
        /// destination.  Default is -1, which means to not send any.  Only
        /// intended to be set once, in the constructor, and will fail if set
        /// after the effect is initially processed.
        /// </summary>
        protected int DdxUvDdyUvRegisterIndex

        {
            get
            {
                ReadPreamble();
                return _ddxUvDdyUvRegisterIndex;
            }

            set
            {
                WritePreamble();
                _ddxUvDdyUvRegisterIndex = value;
                WritePostscript();
            }
        }

        /// <summary>
        /// This method is invoked whenever the PixelShader property changes.
        /// </summary>
        private void PixelShaderPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            PixelShader oldShader = (PixelShader)e.OldValue;
            if (oldShader != null)
            {
                oldShader._shaderBytecodeChanged -= OnPixelShaderBytecodeChanged;
            }

            PixelShader newShader = (PixelShader)e.NewValue;
            if (newShader != null)
            {
                newShader._shaderBytecodeChanged += OnPixelShaderBytecodeChanged;
            }

            OnPixelShaderBytecodeChanged(PixelShader, null);
        }

        /// <summary>
        /// When the PixelShader's bytecode changes to a ps_2_0 shader, verify that registers only
        /// available in ps_3_0 are not being used.
        /// </summary>
        private void OnPixelShaderBytecodeChanged(object sender, EventArgs e)
        {
            PixelShader pixelShader = (PixelShader)sender;

            if (pixelShader != null &&
                pixelShader.ShaderMajorVersion == 2 &&
                pixelShader.ShaderMinorVersion == 0 &&
                UsesPS30OnlyRegisters())
            {
                throw new InvalidOperationException(SR.Get(SRID.Effect_20ShaderUsing30Registers));
            }
        }

        private bool UsesPS30OnlyRegisters()
        {
            // int and bool registers are ps_3_0 only
            if (_intCount > 0 || _intRegisters != null ||
                _boolCount > 0 || _boolRegisters != null)
            {
                return true;
            }

            // float registers 32 or above are ps_3_0 only
            if (_floatRegisters != null)
            {
                for (int i = PS_2_0_FLOAT_REGISTER_LIMIT; i < _floatRegisters.Count; i++)
                {
                    if (_floatRegisters[i] != null)
                    {
                        return true;
                    }
                }
            }

            // sampler registers 4 or above are ps_3_0 only
            // Note: it's really 16, but we use 4 because some cards have trouble with 16 samplers
            // being set.
            if (_samplerData != null)
            {
                for (int i = PS_2_0_SAMPLER_LIMIT; i < _samplerData.Count; i++)
                {
                    if (_samplerData[i] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tells the Effect that the shader constant or sampler corresponding 
        /// to the specified DependencyProperty needs to be updated. 
        /// </summary>
        protected void UpdateShaderValue(DependencyProperty dp)
        {
            if (dp != null)
            {
                WritePreamble();
                object val = this.GetValue(dp);
                var metadata = dp.GetMetadata(this);
                if (metadata != null)
                {
                    var callback = metadata.PropertyChangedCallback;
                    if (callback != null)
                    {
                        callback(this, new DependencyPropertyChangedEventArgs(dp, val, val));
                    }
                }
                WritePostscript();
            }
        }

        /// <summary>
        /// Construct a PropertyChangedCallback which, when invoked, will result in the DP being 
        /// associated with the specified shader constant register index.
        /// </summary>
        protected static PropertyChangedCallback PixelShaderConstantCallback(int floatRegisterIndex)
        {
            return
                (obj, args) =>
                {
                    ShaderEffect eff = obj as ShaderEffect;
                    if (eff != null)
                    {
                        eff.UpdateShaderConstant(args.Property, args.NewValue, floatRegisterIndex);
                    }
                };
        }

        /// <summary>
        /// Construct a PropertyChangedCallback which, when invoked, will result
        /// in the DP being associated with the specified shader sampler
        /// register index. Expected to be called on a Brush-valued
        /// DependencyProperty.  
        /// </summary>
        protected static PropertyChangedCallback PixelShaderSamplerCallback(int samplerRegisterIndex)
        {
            return PixelShaderSamplerCallback(samplerRegisterIndex, _defaultSamplingMode);
        }

        /// <summary>
        /// Construct a PropertyChangedCallback which, when invoked, will result
        /// in the DP being associated with the specified shader sampler
        /// register index. Expected to be called on a Brush-valued
        /// DependencyProperty.  
        /// </summary>
        protected static PropertyChangedCallback PixelShaderSamplerCallback(int samplerRegisterIndex, SamplingMode samplingMode)
        {
            return
                (obj, args) =>
                {
                    ShaderEffect eff = obj as ShaderEffect;
                    if (eff != null)
                    {
                        if (args.IsAValueChange)
                        {
                            eff.UpdateShaderSampler(args.Property, args.NewValue, samplerRegisterIndex, samplingMode);
                        }
                    }
                };
        }

        /// <summary>
        /// Helper for defining Brush-valued DependencyProperties to associate with a
        /// sampler register in the PixelShader.
        /// </summary>
        protected static DependencyProperty RegisterPixelShaderSamplerProperty(string dpName,
                                                                               Type ownerType,
                                                                               int samplerRegisterIndex)
        {
            return RegisterPixelShaderSamplerProperty(dpName, ownerType, samplerRegisterIndex, _defaultSamplingMode);
        }

        /// <summary>
        /// Helper for defining Brush-valued DependencyProperties to associate with a
        /// sampler register in the PixelShader.
        /// </summary>
        protected static DependencyProperty RegisterPixelShaderSamplerProperty(string dpName,
                                                                               Type ownerType,
                                                                               int samplerRegisterIndex,
                                                                               SamplingMode samplingMode)
        {
            return
                DependencyProperty.Register(dpName, typeof(Brush), ownerType,
                                            new UIPropertyMetadata(Effect.ImplicitInput,
                                                                   PixelShaderSamplerCallback(samplerRegisterIndex,
                                                                                              samplingMode)));
        }


        // Updates the shader constant referred to by the DP.  Converts to the
        // form that the HLSL shaders want, and stores that value, since it will
        // be sent on every update.
        // We WritePreamble/Postscript here since this method is called by the user with the callback 
        // created in PixelShaderConstantCallback.
        private void UpdateShaderConstant(DependencyProperty dp, object newValue, int registerIndex)
        {
            WritePreamble();
            Type t = DetermineShaderConstantType(dp.PropertyType, PixelShader);

            if (t == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Effect_ShaderConstantType, dp.PropertyType.Name));
            }
            else
            {
                //
                // Treat as a float constant in ps_2_0 by default
                //
                int registerMax = PS_2_0_FLOAT_REGISTER_LIMIT;
                string srid = SRID.Effect_Shader20ConstantRegisterLimit;

                if (PixelShader != null && PixelShader.ShaderMajorVersion >= 3)
                {
                    //
                    // If there's a ps_3_0 shader, the limit depends on the type
                    //
                    if (t == typeof(float))
                    {
                        registerMax = PS_3_0_FLOAT_REGISTER_LIMIT;
                        srid = SRID.Effect_Shader30FloatConstantRegisterLimit;
                    }
                    else if (t == typeof(int))
                    {
                        registerMax = PS_3_0_INT_REGISTER_LIMIT;
                        srid = SRID.Effect_Shader30IntConstantRegisterLimit;
                    }
                    else if (t == typeof(bool))
                    {
                        registerMax = PS_3_0_BOOL_REGISTER_LIMIT;
                        srid = SRID.Effect_Shader30BoolConstantRegisterLimit;
                    }
                }

                if (registerIndex >= registerMax || registerIndex < 0)
                {
                    throw new ArgumentException(SR.Get(srid), "dp");
                }

                if (t == typeof(float))
                {
                    MilColorF fourTuple;
                    ConvertValueToMilColorF(newValue, out fourTuple);
                    StashInPosition(ref _floatRegisters, registerIndex, fourTuple, registerMax, ref _floatCount);
                }
                else if (t == typeof(int))
                {
                    MilColorI fourTuple;
                    ConvertValueToMilColorI(newValue, out fourTuple);
                    StashInPosition(ref _intRegisters, registerIndex, fourTuple, registerMax, ref _intCount);
                }
                else if (t == typeof(bool))
                {
                    StashInPosition(ref _boolRegisters, registerIndex, (bool)newValue, registerMax, ref _boolCount);
                }
                else
                {
                    // We should have converted all acceptable types.
                    Debug.Assert(false);
                }
            }

            // Propagate dirty
            this.PropertyChanged(dp);
            WritePostscript();
        }


        // Updates the shader sampler referred to by the DP.  Converts to the
        // form that the HLSL shaders want, and stores that value, since it will
        // be sent on every update.
        // We WritePreamble/Postscript here since this method is called by the user with the callback 
        // created in PixelShaderSamplerCallback.
        private void UpdateShaderSampler(DependencyProperty dp, object newValue, int registerIndex, SamplingMode samplingMode)
        {
            WritePreamble();

            if (newValue != null)
            {
                if (!(typeof(VisualBrush).IsInstanceOfType(newValue) ||
                      typeof(BitmapCacheBrush).IsInstanceOfType(newValue) ||
                      typeof(ImplicitInputBrush).IsInstanceOfType(newValue) ||
                      typeof(ImageBrush).IsInstanceOfType(newValue))
                      )
                {
                    // Note that if the type of the brush is ImplicitInputBrush and the value is non null, the value is actually
                    // Effect.ImplicitInput. This is because ImplicitInputBrush is internal and the user can only get to the singleton
                    // Effect.ImplicitInput.
                    throw new ArgumentException(SR.Get(SRID.Effect_ShaderSamplerType), "dp");
                }
            }

            //
            // Treat as ps_2_0 by default
            //
            int registerMax = PS_2_0_SAMPLER_LIMIT;
            string srid = SRID.Effect_Shader20SamplerRegisterLimit;

            if (PixelShader != null && PixelShader.ShaderMajorVersion >= 3)
            {
                registerMax = PS_3_0_SAMPLER_LIMIT;
                srid = SRID.Effect_Shader30SamplerRegisterLimit;
            }

            if (registerIndex >= registerMax || registerIndex < 0)
            {
                throw new ArgumentException(SR.Get(srid));
            }

            SamplerData sd = new SamplerData()
            {
                _brush = (Brush)newValue,
                _samplingMode = samplingMode
            };

            StashSamplerDataInPosition(registerIndex, sd, registerMax);

            // Propagate dirty
            this.PropertyChanged(dp);
            WritePostscript();
        }


        // Ensures that list is extended to 'position', and that
        // the specified value is inserted there.  For lists of value types.
        private static void StashInPosition<T>(ref List<T?> list, int position, T value, int maxIndex, ref uint count) where T : struct
        {
            if (list == null)
            {
                list = new List<T?>(maxIndex);
            }

            if (list.Count <= position)
            {
                int numToAdd = position - list.Count + 1;
                for (int i = 0; i < numToAdd; i++)
                {
                    list.Add((T?)null);
                }
            }

            if (!list[position].HasValue)
            {
                // Going from null to having a value, so increment count
                count++;
            }

            list[position] = value;
        }

        // Ensures that _samplerData is extended to 'position', and that
        // the specified value is inserted there.
        private void StashSamplerDataInPosition(int position, SamplerData newSampler, int maxIndex)
        {
            if (_samplerData == null)
            {
                _samplerData = new List<SamplerData?>(maxIndex);
            }

            if (_samplerData.Count <= position)
            {
                int numToAdd = position - _samplerData.Count + 1;
                for (int i = 0; i < numToAdd; i++)
                {
                    _samplerData.Add((SamplerData?)null);
                }
            }

            if (!_samplerData[position].HasValue)
            {
                // Going from null to having a value, so increment count
                _samplerCount++;
            }

            System.Windows.Threading.Dispatcher dispatcher = this.Dispatcher;

            // Release the old value if it is a resource on channel.  AddRef the
            // new value.
            if (dispatcher != null)
            {
                SamplerData? oldSampler = _samplerData[position];
                Brush oldBrush = null;
                if (oldSampler.HasValue)
                {
                    SamplerData ss = oldSampler.Value;

                    oldBrush = ss._brush;
                }

                Brush newBrush = newSampler._brush;

                DUCE.IResource targetResource = (DUCE.IResource)this;
                using (CompositionEngineLock.Acquire())
                {
                    int channelCount = targetResource.GetChannelCount();

                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        DUCE.Channel channel = targetResource.GetChannel(channelIndex);
                        Debug.Assert(!channel.IsOutOfBandChannel);
                        Debug.Assert(!targetResource.GetHandle(channel).IsNull);
                        ReleaseResource(oldBrush, channel);
                        AddRefResource(newBrush, channel);
                    }
                }
            }

            _samplerData[position] = newSampler;
        }

        private void ManualUpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                if (PixelShader == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Effect_ShaderPixelShaderSet));
                }

                checked
                {
                    DUCE.MILCMD_SHADEREFFECT data;
                    data.Type = MILCMD.MilCmdShaderEffect;
                    data.Handle = _duceResource.GetHandle(channel);

                    data.TopPadding = _topPadding;
                    data.BottomPadding = _bottomPadding;
                    data.LeftPadding = _leftPadding;
                    data.RightPadding = _rightPadding;

                    data.DdxUvDdyUvRegisterIndex = this.DdxUvDdyUvRegisterIndex;
                    data.hPixelShader = ((DUCE.IResource)PixelShader).GetHandle(channel);

                    unsafe
                    {
                        data.ShaderConstantFloatRegistersSize = (uint)(sizeof(Int16) * _floatCount);
                        data.DependencyPropertyFloatValuesSize = (uint)(4 * sizeof(Single) * _floatCount);
                        data.ShaderConstantIntRegistersSize = (uint)(sizeof(Int16) * _intCount);
                        data.DependencyPropertyIntValuesSize = (uint)(4 * sizeof(Int32) * _intCount);
                        data.ShaderConstantBoolRegistersSize = (uint)(sizeof(Int16) * _boolCount);
                        //
                        // Note: the multiply by 4 is not because the boolean register holds 4
                        // values, but to compensate for the difference between sizeof(bool)
                        // in managed code (1) and sizeof(BOOL) in native code (4).
                        //
                        data.DependencyPropertyBoolValuesSize = (uint)(4 * sizeof(bool) * _boolCount);
                        data.ShaderSamplerRegistrationInfoSize = (uint)(2 * sizeof(uint) * _samplerCount); // 2 pieces of data per sampler.
                        data.DependencyPropertySamplerValuesSize = (uint)(1 * sizeof(DUCE.ResourceHandle) * _samplerCount);

                        channel.BeginCommand(
                            (byte*)&data,
                            sizeof(DUCE.MILCMD_SHADEREFFECT),
                            (int)(data.ShaderConstantFloatRegistersSize +
                                  data.DependencyPropertyFloatValuesSize +
                                  data.ShaderConstantIntRegistersSize +
                                  data.DependencyPropertyIntValuesSize +
                                  data.ShaderConstantBoolRegistersSize +
                                  data.DependencyPropertyBoolValuesSize +
                                  data.ShaderSamplerRegistrationInfoSize +
                                  data.DependencyPropertySamplerValuesSize)
                            );

                        // Arrays appear in this order:
                        // 1) float register indices
                        // 2) float dp values
                        // 3) int register indices
                        // 4) int dp values
                        // 5) bool register indices
                        // 6) bool dp values
                        // 7) sampler registration info
                        // 8) sampler dp values

                        // 1) float register indices
                        AppendRegisters(channel, _floatRegisters);

                        // 2) float dp values
                        if (_floatRegisters != null)
                        {
                            for (int i = 0; i < _floatRegisters.Count; i++)
                            {
                                MilColorF? v = _floatRegisters[i];
                                if (v.HasValue)
                                {
                                    MilColorF valueToPush = v.Value;
                                    channel.AppendCommandData((byte*)&valueToPush, sizeof(MilColorF));
                                }
                            }
                        }

                        // 3) int register indices
                        AppendRegisters(channel, _intRegisters);

                        // 4) int dp values
                        if (_intRegisters != null)
                        {
                            for (int i = 0; i < _intRegisters.Count; i++)
                            {
                                MilColorI? v = _intRegisters[i];
                                if (v.HasValue)
                                {
                                    MilColorI valueToPush = v.Value;
                                    channel.AppendCommandData((byte*)&valueToPush, sizeof(MilColorI));
                                }
                            }
                        }

                        // 5) bool register indices
                        AppendRegisters(channel, _boolRegisters);

                        // 6) bool dp values
                        if (_boolRegisters != null)
                        {
                            for (int i = 0; i < _boolRegisters.Count; i++)
                            {
                                bool? v = _boolRegisters[i];
                                if (v.HasValue)
                                {
                                    //
                                    // Note: need 4 bytes for the bool, because the render thread
                                    // unmarshals it into a 4-byte BOOL. See the comment above for
                                    // DependencyPropertyBoolValuesSize for more details.
                                    //

                                    Int32 valueToPush = v.Value ? 1 : 0;
                                    channel.AppendCommandData((byte*)&valueToPush, sizeof(Int32));
                                }
                            }
                        }

                        // 7) sampler registration info
                        if (_samplerCount > 0)
                        {
                            int count = _samplerData.Count;
                            for (int i = 0; i < count; i++)
                            {
                                SamplerData? ssn = _samplerData[i];
                                if (ssn.HasValue)
                                {
                                    SamplerData ss = ssn.Value;

                                    // add as a 2-tuple (SamplerRegisterIndex,
                                    // SamplingMode)  

                                    channel.AppendCommandData((byte*)&i, sizeof(int));

                                    int value = (int)(ss._samplingMode);
                                    channel.AppendCommandData((byte*)&value, sizeof(int));
                                }
                            }
                        }


                        // 8) sampler dp values
                        if (_samplerCount > 0)
                        {
                            for (int i = 0; i < _samplerData.Count; i++)
                            {
                                SamplerData? ssn = _samplerData[i];
                                if (ssn.HasValue)
                                {
                                    SamplerData ss = ssn.Value;

                                    // Making this assumption by storing a collection of
                                    // handles as an Int32Collection
                                    Debug.Assert(sizeof(DUCE.ResourceHandle) == sizeof(Int32));

                                    DUCE.ResourceHandle hBrush = ss._brush != null
                                        ? ((DUCE.IResource)ss._brush).GetHandle(channel)
                                        : DUCE.ResourceHandle.Null;

                                    Debug.Assert(!hBrush.IsNull || ss._brush == null, "If brush isn't null, hBrush better not be");

                                    channel.AppendCommandData((byte*)&hBrush, sizeof(DUCE.ResourceHandle));
                                }
                            }
                        }

                        // That's it...
                        channel.EndCommand();
                    }
                }
            }
        }

        // write the non-null values of the list of nullables to the command data.
        private void AppendRegisters<T>(DUCE.Channel channel, List<T?> list) where T : struct
        {
            if (list != null)
            {
                unsafe
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        T? v = list[i];
                        if (v.HasValue)
                        {
                            Int16 regIndex = (Int16)i;  // put onto stack so next &-operator compiles
                            channel.AppendCommandData((byte*)&regIndex, sizeof(Int16));
                        }
                    }
                }
            }
        }


        // Written by hand to include management of input Brushes (which aren't DPs).
        internal override DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            if (_duceResource.CreateOrAddRefOnChannel(this, channel, System.Windows.Media.Composition.DUCE.ResourceType.TYPE_SHADEREFFECT))
            {
                // Ensures brushes are property instantiated into Duce resources.
                if (_samplerCount > 0)
                {
                    int numSamplers = _samplerData.Count;
                    for (int i = 0; i < numSamplers; i++)
                    {
                        SamplerData? ssn = _samplerData[i];
                        if (ssn.HasValue)
                        {
                            SamplerData ss = ssn.Value;

                            DUCE.IResource brush = ss._brush as DUCE.IResource;
                            if (brush != null)
                            {
                                brush.AddRefOnChannel(channel);
                            }
                        }
                    }
                }

                PixelShader vPixelShader = PixelShader;
                if (vPixelShader != null) ((DUCE.IResource)vPixelShader).AddRefOnChannel(channel);

                AddRefOnChannelAnimations(channel);


                UpdateResource(channel, true /* skip "on channel" check - we already know that we're on channel */ );
            }

            return _duceResource.GetHandle(channel);
        }


        // Written by hand to include management of input Brushes (which aren't DPs).
        internal override void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));

            if (_duceResource.ReleaseOnChannel(channel))
            {
                // Ensure that brushes are released.
                if (_samplerCount > 0)
                {
                    int numSamplers = _samplerData.Count;
                    for (int i = 0; i < numSamplers; i++)
                    {
                        SamplerData? ssn = _samplerData[i];
                        if (ssn.HasValue)
                        {
                            SamplerData ss = ssn.Value;

                            DUCE.IResource brush = ss._brush as DUCE.IResource;
                            if (brush != null)
                            {
                                brush.ReleaseOnChannel(channel);
                            }
                        }
                    }
                }

                PixelShader vPixelShader = PixelShader;
                if (vPixelShader != null) ((DUCE.IResource)vPixelShader).ReleaseOnChannel(channel);

                ReleaseOnChannelAnimations(channel);
            }
        }


        // Shader constants can be coerced into 4-tuples of floats in ps_2_0. Ints and bools are
        // also supported in ps_3_0.
        internal static Type DetermineShaderConstantType(Type type, PixelShader pixelShader)
        {
            Type result = null;
            if (type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(Color) ||
                type == typeof(Point) ||
                type == typeof(Size) ||
                type == typeof(Vector) ||
                type == typeof(Point3D) ||
                type == typeof(Vector3D) ||
                type == typeof(Point4D))
            {
                result = typeof(float);
            }
            else if (pixelShader != null && pixelShader.ShaderMajorVersion >= 3)
            {
                //
                // int and bool are also supported by ps_3_0.
                //
                if (type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(char))
                {
                    result = typeof(int);
                }
                else if (type == typeof(bool))
                {
                    result = typeof(bool);
                }
            }

            return result;
        }


        // Convert to float four tuple
        internal static void ConvertValueToMilColorF(object value, out MilColorF newVal)
        {
            Type t = value.GetType();

            // Fill in four-tuples.  Always fill in 1.0's for where there are
            // empty slots, to avoid division by zero on vector operations that
            // these values are subjected to.

            // Should order these in terms of most likely to be hit first.
            if (t == typeof(double) || t == typeof(float))
            {
                float fVal = (t == typeof(double)) ? (float)(double)value : (float)value;

                // Scalars extend out to fill entire vector.
                newVal.r = newVal.g = newVal.b = newVal.a = fVal;
            }
            else if (t == typeof(Color))
            {
                Color col = (Color)value;
                newVal.r = (float)col.R / 255f;
                newVal.g = (float)col.G / 255f;
                newVal.b = (float)col.B / 255f;
                newVal.a = (float)col.A / 255f;
            }
            else if (t == typeof(Point))
            {
                Point p = (Point)value;
                newVal.r = (float)p.X;
                newVal.g = (float)p.Y;
                newVal.b = 1f;
                newVal.a = 1f;
            }
            else if (t == typeof(Size))
            {
                Size s = (Size)value;
                newVal.r = (float)s.Width;
                newVal.g = (float)s.Height;
                newVal.b = 1f;
                newVal.a = 1f;
            }
            else if (t == typeof(Vector))
            {
                Vector v = (Vector)value;
                newVal.r = (float)v.X;
                newVal.g = (float)v.Y;
                newVal.b = 1f;
                newVal.a = 1f;
            }
            else if (t == typeof(Point3D))
            {
                Point3D p = (Point3D)value;
                newVal.r = (float)p.X;
                newVal.g = (float)p.Y;
                newVal.b = (float)p.Z;
                newVal.a = 1f;
            }
            else if (t == typeof(Vector3D))
            {
                Vector3D v = (Vector3D)value;
                newVal.r = (float)v.X;
                newVal.g = (float)v.Y;
                newVal.b = (float)v.Z;
                newVal.a = 1f;
            }
            else if (t == typeof(Point4D))
            {
                Point4D p = (Point4D)value;
                newVal.r = (float)p.X;
                newVal.g = (float)p.Y;
                newVal.b = (float)p.Z;
                newVal.a = (float)p.W;
            }
            else
            {
                // We should never hit this case, since we check the type using DetermineShaderConstantType
                // before we call this method.
                Debug.Assert(false);
                newVal.r = newVal.b = newVal.g = newVal.a = 1f;
            }
        }

        // Convert to int four tuple
        internal static void ConvertValueToMilColorI(object value, out MilColorI newVal)
        {
            Type t = value.GetType();

            // Fill in four-tuples.  Always fill in 1's for where there are
            // empty slots, to avoid division by zero on vector operations that
            // these values are subjected to.
            int iVal = 1;

            //
            // Note: conversions from long/ulong/uint can change the sign of the number
            //
            if (t == typeof(long))
            {
                iVal = (int)(long)value;
            }
            else if (t == typeof(ulong))
            {
                iVal = (int)(ulong)value;
            }
            else if (t == typeof(uint))
            {
                iVal = (int)(uint)value;
            }
            else if (t == typeof(short))
            {
                iVal = (int)(short)value;
            }
            else if (t == typeof(ushort))
            {
                iVal = (int)(ushort)value;
            }
            else if (t == typeof(byte))
            {
                iVal = (int)(byte)value;
            }
            else if (t == typeof(sbyte))
            {
                iVal = (int)(sbyte)value;
            }
            else if (t == typeof(char))
            {
                iVal = (int)(char)value;
            }
            else
            {
                iVal = (int)value;
            }

            // Scalars extend out to fill entire vector.
            newVal.r = newVal.g = newVal.b = newVal.a = iVal;
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            ShaderEffect effect = (ShaderEffect)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(effect);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            ShaderEffect effect = (ShaderEffect)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(effect);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            ShaderEffect effect = (ShaderEffect)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(effect);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            ShaderEffect effect = (ShaderEffect)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(effect);
        }

        /// <summary>
        /// Clones values that do not have corresponding DPs.
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(ShaderEffect effect)
        {
            _topPadding = effect._topPadding;
            _bottomPadding = effect._bottomPadding;
            _leftPadding = effect._leftPadding;
            _rightPadding = effect._rightPadding;
            if (_floatRegisters != null)
            {
                _floatRegisters = new List<MilColorF?>(effect._floatRegisters);
            }
            if (_samplerData != null)
            {
                _samplerData = new List<SamplerData?>(effect._samplerData);
            }
            _floatCount = effect._floatCount;
            _samplerCount = effect._samplerCount;
            _ddxUvDdyUvRegisterIndex = effect._ddxUvDdyUvRegisterIndex;
        }


        private struct SamplerData
        {
            public Brush _brush;
            public SamplingMode _samplingMode;
        }

        private const SamplingMode _defaultSamplingMode = SamplingMode.Auto;

        // Instance data
        private double _topPadding = 0.0;
        private double _bottomPadding = 0.0;
        private double _leftPadding = 0.0;
        private double _rightPadding = 0.0;
        private List<MilColorF?> _floatRegisters = null;
        private List<MilColorI?> _intRegisters = null;
        private List<bool?> _boolRegisters = null;
        private List<SamplerData?> _samplerData = null;
        private uint _floatCount = 0;
        private uint _intCount = 0;
        private uint _boolCount = 0;
        private uint _samplerCount = 0;

        private int _ddxUvDdyUvRegisterIndex = -1;

        private const int PS_2_0_FLOAT_REGISTER_LIMIT = 32;
        private const int PS_3_0_FLOAT_REGISTER_LIMIT = 224;
        private const int PS_3_0_INT_REGISTER_LIMIT = 16;
        private const int PS_3_0_BOOL_REGISTER_LIMIT = 16;

        // ps_2_0 allows max 16, but some cards seem to have trouble with 16 samplers being set. 
        // Restricting to 4 for now. 
        private const int PS_2_0_SAMPLER_LIMIT = 4;
        private const int PS_3_0_SAMPLER_LIMIT = 8;
    }
}
