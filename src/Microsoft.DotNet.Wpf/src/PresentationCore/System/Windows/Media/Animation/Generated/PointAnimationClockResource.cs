// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// PointAnimationClockResource class.
    /// AnimationClockResource classes refer to an AnimationClock and a base
    /// value.  They implement DUCE.IResource, and thus can be used to produce
    /// a render-side resource which represents the current value of this
    /// AnimationClock.
    /// They subscribe to the Changed event on the AnimationClock and ensure
    /// that the resource's current value is up to date.
    /// </summary>
    internal class PointAnimationClockResource: AnimationClockResource, DUCE.IResource
    {
        /// <summary>
        /// Constructor for public PointAnimationClockResource.
        /// This constructor accepts the base value and AnimationClock.
        /// Note that since there is no current requirement that we be able to set or replace either the
        /// base value or the AnimationClock, this is the only way to initialize an instance of
        /// PointAnimationClockResource.
        /// Also, we currently Assert that the resource is non-null, since without mutability
        /// such a resource isn't needed.
        /// We can easily extend this class if/when new requirements arise.
        /// </summary>
        /// <param name="baseValue"> Point - The base value. </param>
        /// <param name="animationClock"> AnimationClock - cannot be null. </param>
        public PointAnimationClockResource(
            Point baseValue,
            AnimationClock animationClock
            ): base( animationClock )
        {
            _baseValue = baseValue;
        }

        #region Public Properties

        /// <summary>
        /// BaseValue Property - typed accessor for BaseValue.
        /// </summary>
        public Point BaseValue
        {
            get
            {
                return _baseValue;
            }
        }

        /// <summary>
        /// CurrentValue Property - typed accessor for CurrentValue
        /// </summary>
        public Point CurrentValue
        {
            get
            {
                if (_animationClock != null)
                {
                    // No handoff for DrawingContext animations so we use the
                    // BaseValue as the defaultOriginValue and the
                    // defaultDestinationValue.  We call the Timeline's GetCurrentValue
                    // directly to avoid boxing
                    return ((PointAnimationBase)(_animationClock.Timeline)).GetCurrentValue(
                        _baseValue,  // defaultOriginValue
                        _baseValue,  // defaultDesinationValue
                        _animationClock); // clock
                }
                else
                {
                    return _baseValue;
                }
            }
        }

        #endregion Public Properties

        #region DUCE

        //
        // Method which returns the DUCE type of this class.
        // The base class needs this type when calling CreateOrAddRefOnChannel.
        // By providing this via a virtual, we avoid a per-instance storage cost.
        //
        protected override DUCE.ResourceType ResourceType
        {
            get
            {
                 return DUCE.ResourceType.TYPE_POINTRESOURCE;
            }
        }

        /// <summary>
        /// UpdateResource - This method is called to update the render-thread
        /// resource on a given channel.
        /// </summary>
        /// <param name="handle"> The DUCE.ResourceHandle for this resource on this channel. </param>
        /// <param name="channel"> The channel on which to update the render-thread resource. </param>
        protected override void UpdateResource(
            DUCE.ResourceHandle handle,
            DUCE.Channel channel)
        {
            DUCE.MILCMD_POINTRESOURCE cmd = new DUCE.MILCMD_POINTRESOURCE();

            cmd.Type = MILCMD.MilCmdPointResource;
            cmd.Handle = handle;
            cmd.Value = CurrentValue;

            unsafe
            {
                channel.SendCommand(
                    (byte*)&cmd,
                    sizeof(DUCE.MILCMD_POINTRESOURCE));
            }

            // Validate this resource
            IsResourceInvalid = false;
        }

        #endregion DUCE

        private Point _baseValue;
    }
}
