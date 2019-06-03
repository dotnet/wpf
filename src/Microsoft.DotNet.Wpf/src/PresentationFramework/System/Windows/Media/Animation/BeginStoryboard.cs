// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This object includes a Storyboard reference.  When triggered, the Storyboard
*  is started.
*
*
\***************************************************************************/
using System.ComponentModel;            // DefaultValueAttribute
using System.Diagnostics;               // Debug.Assert
using System.Windows;                   // SR.Get
using System.Windows.Documents;         // TableTemplate
using System.Windows.Markup;     // IAddChild

namespace System.Windows.Media.Animation
{
/// <summary>
/// BeginStoryboard will call begin on its Storyboard reference when
///  it is triggered.
/// </summary>
[RuntimeNameProperty("Name")] // Enables INameScope.FindName to find BeginStoryboard objects.
[ContentProperty("Storyboard")] // Enables <Storyboard> child without explicit <BeginStoryboard.Storyboard> tag.
public sealed class BeginStoryboard : TriggerAction
{
    /// <summary>
    ///     Creates an instance of the BeginStoryboard object.
    /// </summary>
    public BeginStoryboard()
        : base()
    {
    }

    /// <summary>
    ///     DependencyProperty to back the Storyboard property
    /// </summary>
    public static readonly DependencyProperty StoryboardProperty =
                DependencyProperty.Register( "Storyboard", typeof(Storyboard), typeof(BeginStoryboard) );


    /// <summary>
    ///     The Storyboard object that this action is associated with.  This 
    /// must be specified before Invoke is called.
    /// </summary>
    [DefaultValue(null)]
    public Storyboard Storyboard
    {
        get
        {
            return GetValue(StoryboardProperty) as Storyboard;
        }
        set
        {
            ThrowIfSealed();

            SetValue( StoryboardProperty, value );
        }
    }

    /// <summary>
    ///     Specify the hand-off behavior to use when starting the animation
    /// clocks in this storyboard
    /// </summary>
    [DefaultValue(HandoffBehavior.SnapshotAndReplace)]
    public HandoffBehavior HandoffBehavior 
    {
        get
        {
            return _handoffBehavior;
        }
        set
        {
            ThrowIfSealed();

            if(HandoffBehaviorEnum.IsDefined(value)) 
            {
                _handoffBehavior = value;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.Storyboard_UnrecognizedHandoffBehavior));
            }
        }
    }

    /// <summary>
    ///     The name to use for referencing this Storyboard.  This named is used 
    /// by a control action such as pause and resume.  Defaults to null, which
    /// means this storyboard is not going to be interactively controlled.
    /// </summary>
    // Null == no interactive control == "Fire and Forget"
    [DefaultValue(null)]
    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            ThrowIfSealed();

            if(value != null && !System.Windows.Markup.NameValidationHelper.IsValidIdentifierName(value))
            {
                // Duplicate the error string thrown from DependencyObject.SetValueValidateParams
                throw new ArgumentException(SR.Get(SRID.InvalidPropertyValue, value, "Name"));
            }
            
            // Null is OK - it's to remove whatever name was previously set.
            _name = value;
        }
    }

    private void ThrowIfSealed()
    {
        if (IsSealed)
        {
            throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "BeginStoryboard"));
        }
    }
    
    // Bug #1329664 workaround to make beta 2
    // Remove thread affinity when sealed, but before doing that, snapshot the
    //  current Storyboard value and remove *its* thread affinity too.
    internal override void Seal()
    {
        if( !IsSealed )
        {
            // Gets our Storyboard value.  This value may have come from a
            //  ResourceReferenceExpression or might have been a deferred
            //  reference that has since been realized.
            Storyboard snapshot = GetValue(StoryboardProperty) as Storyboard;

            if( snapshot == null )
            {
                // This is the same error thrown by Begin if the Storyboard
                //  property couldn't be resolved at Begin time.  Since we're
                //  not allowing changes after this point, lack of resolution
                //  here means the same thing.
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_StoryboardReferenceRequired));
            }
            
            // We're planning to break our thread affinity - we also need to
            //  make sure the Storyboard can also be used accross threads.
            if(!snapshot.CanFreeze)
            {
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_UnableToFreeze));
            }
            if(!snapshot.IsFrozen)
            {
                snapshot.Freeze();
            }

            // Promote that snapshot into a local value.  This is a no-op if it
            //  was a deferred reference or local Storyboard, but if it came from a 
            //  ResourceReferenceExpression it will replace the Expression object
            //  with a snapshot of its current value.
            Storyboard = snapshot;
        }
        else
        {
            ; // base.Seal() will throw exception for us if already sealed.
        }

        base.Seal();        

        // Now we can break our thread affinity
        DetachFromDispatcher();
    }
    
    /// <summary>
    ///     Called when it's time to execute this storyboard action
    /// </summary>

    internal sealed override void Invoke( FrameworkElement fe, FrameworkContentElement fce, Style targetStyle, FrameworkTemplate frameworkTemplate, Int64 layer )
    {
        Debug.Assert( fe != null || fce != null, "Caller of internal function failed to verify that we have a FE or FCE - we have neither." );

        INameScope nameScope = null;
        if( targetStyle != null )
        {
            nameScope = targetStyle;
        }
        else
        {
            Debug.Assert( frameworkTemplate != null );
            nameScope = frameworkTemplate;
        }


        Begin( (fe != null ) ? (DependencyObject)fe : (DependencyObject)fce, nameScope, layer );
    }

    internal sealed override void Invoke( FrameworkElement fe )
    {
        Debug.Assert( fe != null, "Invoke needs an object as starting point");

        Begin( fe, null, Storyboard.Layers.ElementEventTrigger );
    }

    private void Begin( DependencyObject targetObject, INameScope nameScope, Int64 layer )
    {
        if( Storyboard == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_StoryboardReferenceRequired));
        }

        if( Name != null )
        {
            Storyboard.BeginCommon(targetObject, nameScope, _handoffBehavior, true /* == is controllable */, layer );
        }
        else
        {
            Storyboard.BeginCommon(targetObject, nameScope, _handoffBehavior, false /* == not controllable */, layer );
        }
    }

    private HandoffBehavior _handoffBehavior = HandoffBehavior.SnapshotAndReplace;
    private string _name = null;
}
}
