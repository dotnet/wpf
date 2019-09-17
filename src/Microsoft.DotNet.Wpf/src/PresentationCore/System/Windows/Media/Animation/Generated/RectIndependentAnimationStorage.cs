// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
// 
// This file was generated from the codegen template located at:
//     wpf\src\Graphics\codegen\mcg\generators\AnimationResourceTemplate.cs
//
// Please see MilCodeGen.html for more information.
//

using System;
using MS.Internal;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Media.Media3D;
using System.Security;

namespace System.Windows.Media.Animation
{
    internal class RectIndependentAnimationStorage : IndependentAnimationStorage
    {
        //
        // Method which returns the DUCE type of this class.
        // The base class needs this type when calling CreateOrAddRefOnChannel.
        // By providing this via a virtual, we avoid a per-instance storage cost.
        //
        protected override DUCE.ResourceType ResourceType
        {
            get
            {
                return DUCE.ResourceType.TYPE_RECTRESOURCE;
            }
        }

        protected override void UpdateResourceCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource.IsOnChannel(channel));
            DependencyObject dobj = ((DependencyObject) _dependencyObject.Target);

            // The dependency object was GCed, nothing to do here
            if (dobj == null)
            {
                return;
            }

            Rect tempValue = (Rect)dobj.GetValue(_dependencyProperty);

            DUCE.MILCMD_RECTRESOURCE data;
            data.Type = MILCMD.MilCmdRectResource;
            data.Handle = _duceResource.GetHandle(channel);
            data.Value = tempValue;

            unsafe
            {
                channel.SendCommand(
                    (byte*)&data,
                    sizeof(DUCE.MILCMD_RECTRESOURCE));
            }
        }
    }
}
