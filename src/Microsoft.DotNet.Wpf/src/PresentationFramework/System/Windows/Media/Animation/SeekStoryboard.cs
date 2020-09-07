// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This object includes a Storyboard reference.  When triggered, the Storyboard
*  seeks to the given offset.
*
*
\***************************************************************************/
using System.ComponentModel;            // DefaultValueAttribute
using System.Diagnostics;               // Debug.Assert

namespace System.Windows.Media.Animation
{
/// <summary>
/// SeekStoryboard will call seek on its Storyboard reference when
///  it is triggered.
/// </summary>
public sealed class SeekStoryboard : ControllableStoryboardAction
{
    /// <summary>
    ///     A time offset to use for this action.  If it is never explicitly
    /// specified, it will be zero.
    /// </summary>
    // [DefaultValue(TimeSpan.Zero)] - not usable because TimeSpan.Zero is not a constant expression.
    public TimeSpan Offset
    {
        get
        {
            return _offset;
        }
        set
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "SeekStoryboard"));
            }
            // TimeSpan is a struct and can't be null - hence no ArgumentNullException check.
            _offset = value;
        }
    }

    /// <summary>
    /// This method is used by TypeDescriptor to determine if this property should
    /// be serialized.
    /// </summary>
    // Because we can't use [DefaultValue(TimeSpan.Zero)] - TimeSpan.Zero is not a constant expression.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeOffset()
    {
        return !(TimeSpan.Zero.Equals(_offset));
    }
    

    /// <summary>
    ///     A time offset origin from which to evaluate the Offset value.
    /// If it is never explicitly specified, it will be relative to the
    /// beginning.  ("Begin")
    /// </summary>
    [DefaultValue(TimeSeekOrigin.BeginTime)]
    public TimeSeekOrigin Origin
    {
        get
        {
            return _origin;
        }
        set
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "SeekStoryboard"));
            }

            if( value == TimeSeekOrigin.BeginTime || value == TimeSeekOrigin.Duration ) // FxCop doesn't like Enum.IsDefined, probably need some central validation mechanism.
            {
                _origin = value;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.Storyboard_UnrecognizedTimeSeekOrigin));
            }
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
            storyboard.Seek(containingFE, Offset, Origin);
        }
        else
        {
            storyboard.Seek(containingFCE, Offset, Origin);
        }
    }

    TimeSpan       _offset = TimeSpan.Zero;
    TimeSeekOrigin _origin = TimeSeekOrigin.BeginTime;
}
}
