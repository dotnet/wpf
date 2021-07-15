// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This object includes a Storyboard reference.  When triggered, the Storyboard
*  SkipToFills.
*
*
\***************************************************************************/
using System.Diagnostics;               // Debug.Assert

namespace System.Windows.Media.Animation
{
/// <summary>
/// SkipStoryboardToFill will call SkipToFill on its Storyboard reference when
///  it is triggered.
/// </summary>
public sealed class SkipStoryboardToFill : ControllableStoryboardAction
{
    /// <summary>
    ///     Called when it's time to execute this storyboard action
    /// </summary>
    internal override void Invoke( FrameworkElement containingFE, FrameworkContentElement containingFCE, Storyboard storyboard )
    {
        Debug.Assert( containingFE != null || containingFCE != null,
            "Caller of internal function failed to verify that we have a FE or FCE - we have neither." );

        if( containingFE != null )
        {
            storyboard.SkipToFill(containingFE);
        }
        else
        {
            storyboard.SkipToFill(containingFCE);
        }
    }
}
}
