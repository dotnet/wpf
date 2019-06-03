// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This object includes a named Storyboard references.  When triggered, the
*  name is resolved and passed to a derived class method to perform the 
*  actual action.
*
*
\***************************************************************************/
using System.ComponentModel;            // DefaultValueAttribute
using System.Diagnostics;               // Debug.Assert

using System.Windows.Documents;         // TableTemplate
using System.Windows.Markup;            // INameScope

namespace System.Windows.Media.Animation
{
/// <summary>
///     A controllable storyboard action associates a trigger action with a 
/// Storyboard.  The association from this object is a string that is the name
/// of the Storyboard in a resource dictionary.
/// </summary>
public abstract class ControllableStoryboardAction : TriggerAction
{
    /// <summary>
    ///     Internal constructor - nobody is supposed to ever create an instance
    /// of this class.  Use a derived class instead.
    /// </summary>
    internal ControllableStoryboardAction()
    {
    }
    
    /// <summary>
    ///     Name to use for resolving the storyboard reference needed.  This
    /// points to a BeginStoryboard instance, and we're controlling that one.
    /// </summary>
    [DefaultValue(null)]
    public string BeginStoryboardName
    {
        get
        {
            return _beginStoryboardName;
        }
        set
        {
            if (IsSealed)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "ControllableStoryboardAction"));
            }

            // Null is allowed to wipe out existing name - as long as another
            //  valid name is set before Invoke time.
            _beginStoryboardName = value;
        }
    }

    internal sealed override void Invoke( FrameworkElement fe, FrameworkContentElement fce, Style targetStyle, FrameworkTemplate frameworkTemplate, Int64 layer )
    {
        Debug.Assert( fe != null || fce != null, "Caller of internal function failed to verify that we have a FE or FCE - we have neither." );
        Debug.Assert( targetStyle != null || frameworkTemplate != null,
            "This function expects to be called when the associated action is inside a Style/Template.  But it was not given a reference to anything." );

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

        Invoke( fe, fce, GetStoryboard( fe, fce, nameScope ) );
    }
    
    internal sealed override void Invoke( FrameworkElement fe )
    {
        Debug.Assert( fe != null, "Invoke needs an object as starting point");

        Invoke( fe, null, GetStoryboard( fe, null, null ) );
    }

    internal virtual void Invoke( FrameworkElement containingFE, FrameworkContentElement containingFCE,  Storyboard storyboard )
    {
    }

    // Find a Storyboard object for this StoryboardAction to act on, using the
    //  given BeginStoryboardName to find a BeginStoryboard instance and use
    //  its Storyboard object reference.
    private Storyboard GetStoryboard( FrameworkElement fe, FrameworkContentElement fce, INameScope nameScope )
    {
        if( BeginStoryboardName == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_BeginStoryboardNameRequired));
        }

        BeginStoryboard keyedBeginStoryboard = Storyboard.ResolveBeginStoryboardName( BeginStoryboardName, nameScope, fe, fce );
        
        Storyboard storyboard = keyedBeginStoryboard.Storyboard;

        if( storyboard == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_BeginStoryboardNoStoryboard, BeginStoryboardName));
        }
        
        return storyboard;
    }

    private string     _beginStoryboardName = null;
}
}
