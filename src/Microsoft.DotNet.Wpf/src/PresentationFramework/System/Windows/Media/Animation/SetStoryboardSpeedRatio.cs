// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This object includes a Storyboard reference.  When triggered, the Storyboard
*  speed ratio is set to the given parameter.
*
*
\***************************************************************************/
using System.ComponentModel;            // DefaultValueAttribute
using System.Diagnostics;               // Debug.Assert

namespace System.Windows.Media.Animation
{
/// <summary>
/// SetStoryboardSpeedRatio will set the speed for its Storyboard reference when
///  it is triggered.
/// </summary>
public sealed class SetStoryboardSpeedRatio : ControllableStoryboardAction
{
    /// <summary>
    ///     A speed ratio to use for this action.  If it is never explicitly
    /// specified, it is 1.0.
    /// </summary>
    [DefaultValue(1.0)]
    public double SpeedRatio
    {
        get
        {
            return _speedRatio;
        }
        set
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "SetStoryboardSpeedRatio"));
            }

            _speedRatio = value;
        }
    }

    /// <summary>
    ///     Called when it's time to execute this storyboard action
    /// </summary>
    internal override void Invoke( FrameworkElement containingFE, FrameworkContentElement containingFCE, Storyboard storyboard )
    {
        Debug.Assert( containingFE != null || containingFCE != null,
            "Caller of internal function failed to verify that we have a FE or FCE - we have neither." );

        if( containingFE != null )
        {
            storyboard.SetSpeedRatio(containingFE, SpeedRatio);
        }
        else
        {
            storyboard.SetSpeedRatio(containingFCE, SpeedRatio);
        }
    }

    double          _speedRatio = 1.0;
}
}
