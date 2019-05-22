// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* A Storyboard coordinates a set of actions in a time-dependent manner.  An
*  example usage is to coordinate animation events such as start/stop/pause.
*
*
\***************************************************************************/
using System.Collections;               // DictionaryEntry
using System.Collections.Generic;       // List<T>
using System.Collections.Specialized;   // HybridDictionary
using System.ComponentModel;            // PropertyDescriptor
using System.Diagnostics;               // Debug.Assert
using System.Reflection;                // PropertyInfo

using System.Windows.Controls;          // MediaElement
using System.Windows.Documents;         // TableTemplate
using System.Windows.Markup;            // INameScope
using MS.Internal;                      // Helper
using MS.Utility;                       // FrugalMap

namespace System.Windows.Media.Animation
{
/// <summary>
/// A Storyboard coordinates a set of actions in a time-dependent manner.
/// </summary>
public class Storyboard : ParallelTimeline
{
    static Storyboard()
    {
        PropertyMetadata targetPropertyMetadata = new PropertyMetadata();
        targetPropertyMetadata.FreezeValueCallback = TargetFreezeValueCallback;

        TargetProperty = DependencyProperty.RegisterAttached("Target", typeof(DependencyObject), typeof(Storyboard), targetPropertyMetadata);
    }

    /// <summary>
    ///     Creates an instance of the Storyboard object.
    /// </summary>
    public Storyboard()
        : base()
    {
    }

#region Freezable Requirements

    /// <summary>
    ///     Override method required of Freezable-derived types
    /// </summary>
    protected override Freezable CreateInstanceCore()
    {
        return new Storyboard();
    }

    // We don't need to override CopyCore since it doesn't do anything, the
    // base class will handle what is necessary.

    /// <summary>
    ///     Override method required of Freezable-derived types
    /// </summary>
    public new Storyboard Clone()
    {
        return (Storyboard)base.Clone();
    }

#endregion


#region Attached Properties

    /// <summary>
    ///     The Target property is designed to be attached to animation
    ///     timelines to indicate the object they should target.
    /// </summary>
    public static readonly DependencyProperty TargetProperty;

    /// <summary>
    ///     Sets value of the Target property on the specified object.
    /// </summary>
    public static void SetTarget(DependencyObject element, DependencyObject value)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        element.SetValue(TargetProperty, value);
    }

    /// <summary>
    ///     Gets the value of the Target property from the specified object.
    /// </summary>
    /// <remarks>
    ///     The target property is not serializable, since it can be set to
    ///     any DependencyObject, and it is not guaranteed that this object
    ///     can be correctly referenced from XAML.
    /// </remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static DependencyObject GetTarget(DependencyObject element)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        return (DependencyObject)element.GetValue(TargetProperty);
    }

    private static bool TargetFreezeValueCallback(
        DependencyObject d,
        DependencyProperty dp,
        EntryIndex entryIndex,
        PropertyMetadata metadata,
        bool isChecking)
    {
        // We allow the object to which the Target property is attached to be
        // frozen, even though the value of the Target property is not usable
        // from other threads.  Clocks clone & freeze copies of their original
        // timelines because the clocks will not respond to changes to those
        // timelines.
        return true;
    }

    /// <summary>
    /// The TargetName property is designed to be attached to animation objects,
    ///  giving a string that will be matched against an element with the given name.
    /// </summary>
    public static readonly DependencyProperty TargetNameProperty =
        DependencyProperty.RegisterAttached("TargetName", typeof(string), typeof(Storyboard));

    // The static setter/getter methods for the TargetName property is required
    //  for parser support.

    /// <summary>
    ///     Attaches the TargetName value on the given object.
    /// </summary>
    public static void SetTargetName(DependencyObject element, String name)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        if (name == null) { throw new ArgumentNullException("name"); }
        element.SetValue(TargetNameProperty, name);
    }

    /// <summary>
    ///     Retrieves the attached TargetName value of the given object.
    /// </summary>
    public static string GetTargetName(DependencyObject element)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        return (string)element.GetValue(TargetNameProperty);
    }

    /// <summary>
    /// The TargetProperty property is designed to be attached to animation objects,
    ///  giving the string representation of the DependencyProperty that the
    ///  animation object will be manipulating.
    /// </summary>
    public static readonly DependencyProperty TargetPropertyProperty =
        DependencyProperty.RegisterAttached("TargetProperty", typeof(PropertyPath), typeof(Storyboard));

    // The static setter/getter methods for the TargetProperty property is required
    //  for parser support.

    /// <summary>
    ///     Attaches the TargetProperty value on the given object.
    /// </summary>
    public static void SetTargetProperty(DependencyObject element, PropertyPath path)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        if (path == null) { throw new ArgumentNullException("path"); }
        element.SetValue(TargetPropertyProperty, path);
    }

    /// <summary>
    ///     Retrieves the attached TargetProperty value of the given object.
    /// </summary>
    public static PropertyPath GetTargetProperty(DependencyObject element)
    {
        if (element == null) { throw new ArgumentNullException("element"); }
        return (PropertyPath)element.GetValue(TargetPropertyProperty);
    }

#endregion

    /// <summary>
    ///     An object that represents a DependencyObject+DependencyProperty
    /// pairing, designed to be used as a key into a Hashtable or similar data
    /// structure.
    /// </summary>
    private class ObjectPropertyPair
    {
        public ObjectPropertyPair(DependencyObject o, DependencyProperty p)
        {
            _object = o;
            _property = p;
        }

        public override int GetHashCode()
        {
            return _object.GetHashCode() ^ _property.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if ((o != null) && (o is ObjectPropertyPair))
            {
                return Equals((ObjectPropertyPair)o);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(ObjectPropertyPair key)
        {
            return (_object.Equals(key._object) && (_property == key._property));
        }

        public DependencyObject DependencyObject { get { return _object; } }
        public DependencyProperty DependencyProperty { get { return _property; } }

        private DependencyObject _object;
        private DependencyProperty _property;
    }

    /// <summary>
    ///     Finds the target element of a Storyboard.TargetName property.
    /// </summary>
    /// <remarks>
    ///     This is using a different set of FindName rules than that used
    /// by ResolveBeginStoryboardName for finding a BeginStoryboard object due
    /// to the different FindName behavior in templated objects.
    ///
    ///     The templated object name is the name attached to the
    /// FrameworkElementFactory that created the object.  There are many of them
    /// created, one per templated object.  So we need to use Template.FindName()
    /// to find the templated child using the context of the templated parent.
    ///
    ///     Note that this FindName() function on the template class is
    /// completely different from the INameScope.FindName() function on the
    /// same class
    /// </remarks>
    internal static DependencyObject ResolveTargetName(
        string targetName,
        INameScope nameScope,
        DependencyObject element )
    {
        object           nameScopeUsed = null;
        object           namedObject = null;
        DependencyObject targetObject = null;
        FrameworkElement fe = element as FrameworkElement;
        FrameworkContentElement fce = element as FrameworkContentElement;

        if( fe != null )
        {
            if( nameScope != null )
            {
                namedObject = ((FrameworkTemplate)nameScope).FindName(targetName, fe);
                nameScopeUsed = nameScope;
            }
            else
            {
                namedObject = fe.FindName(targetName);
                nameScopeUsed = fe;
            }
        }
        else if( fce != null )
        {
            Debug.Assert( nameScope == null );
            namedObject = fce.FindName(targetName);
            nameScopeUsed = fce;
        }
        else
        {
            throw new InvalidOperationException(
                SR.Get(SRID.Storyboard_NoNameScope, targetName));
        }

        if( namedObject == null )
        {
            throw new InvalidOperationException(
                SR.Get(SRID.Storyboard_NameNotFound, targetName, nameScopeUsed.GetType().ToString()));
        }

        targetObject = namedObject as DependencyObject;
        if( targetObject == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_TargetNameNotDependencyObject, targetName ));
        }

        return targetObject;
    }

    /// <summary>
    ///     Finds a BeginStoryboard with the given name, following the rules
    /// governing Storyboard.  Returns null if not found.
    /// </summary>
    /// <remarks>
    ///     If a name scope is given, look there and nowhere else.  In the
    /// absense of name scope, use Framework(Content)Element.FindName which
    /// has its own complex set of rules for looking up name scopes.
    ///
    ///     This is a different set of rules than from that used to look up
    /// the TargetName.  BeginStoryboard name is registered with the template
    /// INameScope on a per-template basis.  So we look it up using
    /// INameScope.FindName().  This is a function completely different from
    /// Template.FindName().
    /// </remarks>
    internal static BeginStoryboard ResolveBeginStoryboardName(
        string targetName,
        INameScope nameScope,
        FrameworkElement fe,
        FrameworkContentElement fce)
    {
        object          namedObject = null;
        BeginStoryboard beginStoryboard = null;

        if( nameScope != null )
        {
            namedObject = nameScope.FindName(targetName);
            if( namedObject == null )
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.Storyboard_NameNotFound, targetName, nameScope.GetType().ToString()));
            }
        }
        else if( fe != null )
        {
            namedObject = fe.FindName(targetName);
            if( namedObject == null )
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.Storyboard_NameNotFound, targetName, fe.GetType().ToString()));
            }
        }
        else if( fce != null )
        {
            namedObject = fce.FindName(targetName);
            if( namedObject == null )
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.Storyboard_NameNotFound, targetName, fce.GetType().ToString()));
            }
        }
        else
        {
            throw new InvalidOperationException(
                SR.Get(SRID.Storyboard_NoNameScope, targetName));
        }

        beginStoryboard = namedObject as BeginStoryboard;

        if( beginStoryboard == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_BeginStoryboardNameNotFound, targetName));
        }

        return beginStoryboard;
    }

    /// <summary>
    ///     Recursively walks the clock tree and determine the target object
    /// and property for each clock in the tree.
    /// </summary>
    /// <remarks>
    ///     The currently active object and property path are passed in as parameters,
    /// they will be used unless a target/property specification exists on
    /// the Timeline object corresponding to the current clock.  (So that the
    /// leaf-most reference wins.)
    ///
    ///     The active object and property parameters may be null if they have
    /// never been specified.  If we reach a leaf node clock and a needed attribute
    /// is still null, it is an error condition.  Otherwise we keep hoping they'll be found.
    /// </remarks>
    private void ClockTreeWalkRecursive(
        Clock currentClock,                /* No two calls will have the same currentClock     */
        DependencyObject containingObject, /* Remains the same through all the recursive calls */
        INameScope nameScope,              /* Remains the same through all the recursive calls */
        DependencyObject parentObject,
        string parentObjectName,
        PropertyPath parentPropertyPath,
        HandoffBehavior handoffBehavior,   /* Remains the same through all the recursive calls */
        HybridDictionary clockMappings,
        Int64 layer                        /* Remains the same through all the recursive calls */)
    {
        Timeline currentTimeline = currentClock.Timeline;

        DependencyObject targetObject = parentObject;
        string currentObjectName = parentObjectName;
        PropertyPath currentPropertyPath = parentPropertyPath;

        // If we have target object/property information, use it instead of the
        //  parent's information.
        string nameString = (string)currentTimeline.GetValue(TargetNameProperty);
        if( nameString != null )
        {
            if( nameScope is Style )
            {
                // We are inside a Style - we don't let people target anything.
                //  They're only allowed to modify the Styled object, which is
                //  already the implicit target.
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_TargetNameNotAllowedInStyle, nameString));
            }
            currentObjectName = nameString;
        }

        // The TargetProperty trumps the TargetName property.
        DependencyObject localTargetObject = (DependencyObject) currentTimeline.GetValue(TargetProperty);
        if( localTargetObject != null )
        {
            targetObject = localTargetObject;
            currentObjectName = null;
        }

        PropertyPath propertyPath = (PropertyPath)currentTimeline.GetValue(TargetPropertyProperty);
        if( propertyPath != null )
        {
            currentPropertyPath = propertyPath;
        }

        // Now see if the current clock is an animation clock
        if( currentClock is AnimationClock )
        {
            DependencyProperty targetProperty = null;
            AnimationClock animationClock = (AnimationClock)currentClock;

            if( targetObject == null )
            {
                // Resolve the target object name.  If no name specified, use the
                //  containing object.
                if( currentObjectName != null )
                {
                    DependencyObject mentor = Helper.FindMentor(containingObject);

                    targetObject = ResolveTargetName(currentObjectName, nameScope, mentor);
                }
                else
                {
                    // The containing object must be either an FE or FCE.
                    // (Not a Storyboard, as used for "shared clocks" mode.)
                    targetObject = containingObject as FrameworkElement;
                    if(targetObject == null)
                    {
                        targetObject = containingObject as FrameworkContentElement;
                    }

                    if( targetObject == null )
                    {
                        // The containing object is not an FE or FCE.
                        throw new InvalidOperationException(SR.Get(SRID.Storyboard_NoTarget, currentTimeline.GetType().ToString() ));
                    }
                }
            }

            // See if we have a property name to use.
            if( currentPropertyPath == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_TargetPropertyRequired, currentTimeline.GetType().ToString() ));
            }

            // A property name can be a straightforward property name (like "Angle")
            // but may be a more complex multi-step property path.  The two cases
            // are handled differently.
            using(currentPropertyPath.SetContext(targetObject))
            {
                if( currentPropertyPath.Length < 1 )
                {
                    throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathEmpty));
                }

                VerifyPathIsAnimatable(currentPropertyPath);

                if( currentPropertyPath.Length == 1 )
                {
                    // We have a simple single-step property.
                    targetProperty = currentPropertyPath.GetAccessor(0) as DependencyProperty;

                    if( targetProperty == null )
                    {
                        // Unfortunately it's not a DependencyProperty.
                        throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathMustPointToDependencyProperty, currentPropertyPath.Path ));
                    }

                    VerifyAnimationIsValid(targetProperty, animationClock);

                    ObjectPropertyPair animatedTarget = new ObjectPropertyPair(targetObject, targetProperty);
                    UpdateMappings(clockMappings, animatedTarget, animationClock);
                }
                else // path.Length > 1
                {
                    // This is a multi-step property path that requires more extensive
                    //  setup.
                    ProcessComplexPath(clockMappings, targetObject, currentPropertyPath, animationClock, handoffBehavior, layer);
                }
            }
        }
        else if ( currentClock is MediaClock ) // Not an animation clock - maybe a media clock?
        {
            // Yes it's a media clock.  Try to find the corresponding object and
            //  apply the clock to that object.
            ApplyMediaClock(nameScope, containingObject, targetObject, currentObjectName, (MediaClock) currentClock);
        }
        else
        {
            // None of the types we recognize as leaf node clock types -
            //  recursively process child clocks.
            ClockGroup currentClockGroup = currentClock as ClockGroup;

            if (currentClockGroup != null)
            {
                ClockCollection childrenClocks = currentClockGroup.Children;

                for( int i = 0; i < childrenClocks.Count; i++ )
                {
                    ClockTreeWalkRecursive(
                        childrenClocks[i],
                        containingObject,
                        nameScope,
                        targetObject,
                        currentObjectName,
                        currentPropertyPath,
                        handoffBehavior,
                        clockMappings,
                        layer);
                }
            }
        }
    }

    /// <summary>
    ///     When we've found a media clock, try to find a corresponding media
    /// element and attach the media clock to that element.
    /// </summary>
    private static void ApplyMediaClock( INameScope nameScope, DependencyObject containingObject,
        DependencyObject currentObject, string currentObjectName, MediaClock mediaClock )
    {
        MediaElement targetMediaElement = null;

        if( currentObjectName != null )
        {
            // Find the object named as the current target name.
            DependencyObject mentor = Helper.FindMentor(containingObject);
            targetMediaElement = ResolveTargetName(currentObjectName, nameScope, mentor ) as MediaElement;

            if( targetMediaElement == null )
            {
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_MediaElementNotFound, currentObjectName ));
            }
        }
        else if( currentObject != null )
        {
            targetMediaElement = currentObject as MediaElement;
        }
        else
        {
            targetMediaElement = containingObject as MediaElement;
        }

        if( targetMediaElement == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_MediaElementRequired));
        }

        targetMediaElement.Clock = mediaClock;
    }


    /// <summary>
    ///     Given an animation clock, add it to the data structure which tracks
    /// all the clocks along with their associated target object and property.
    /// </summary>
    private static void UpdateMappings(
        HybridDictionary clockMappings,
        ObjectPropertyPair mappingKey,
        AnimationClock animationClock)
    {
        object mappedObject = clockMappings[mappingKey];

        Debug.Assert( mappedObject == null || mappedObject is AnimationClock || mappedObject is List<AnimationClock>,
            "Internal error - clockMappings table contains an unexpected object " + ((mappedObject == null ) ? "" : mappedObject.GetType().ToString()) );

        if( mappedObject == null )
        {
            // No clock currently in storage, put this clock in that slot.
            clockMappings[mappingKey] = animationClock;
        }
        else if( mappedObject is AnimationClock )
        {
            // One clock currently in storage, up-convert to list and replace in slot.
            List<AnimationClock> clockList = new List<AnimationClock>();

            clockList.Add((AnimationClock)mappedObject);
            clockList.Add(animationClock);

            clockMappings[mappingKey] = clockList;
        }
        else // mappedObject is List<AnimationClock>
        {
            // Add to existing list in storage.
            List<AnimationClock> clockList = (List<AnimationClock>)mappedObject;

            clockList.Add(animationClock);
        }

        return;
    }

    /// <summary>
    ///     Takes the built up mapping table for animation clocks and applies
    /// them to the specified property on the specified object.
    /// </summary>
    private static void ApplyAnimationClocks( HybridDictionary clockMappings, HandoffBehavior handoffBehavior, Int64 layer )
    {
        foreach( DictionaryEntry entry in clockMappings )
        {
            ObjectPropertyPair key = (ObjectPropertyPair)entry.Key;
            object value = entry.Value;
            List<AnimationClock> clockList;

            Debug.Assert( value is AnimationClock || value is List<AnimationClock> ,
                "Internal error - clockMappings table contains unexpected object of type" + value.GetType() );

            if( value is AnimationClock )
            {
                clockList = new List<AnimationClock>(1);
                clockList.Add((AnimationClock)value);
            }
            else // if( value is List<AnimationClock> )
            {
                clockList = (List<AnimationClock>)value;
            }

            AnimationStorage.ApplyAnimationClocksToLayer(
                key.DependencyObject,
                key.DependencyProperty,
                clockList,
                handoffBehavior,
                layer);
        }
    }

    /// <summary>
    ///     Function that checks to see if a given PropertyPath (already given
    /// the context object) can be used.
    /// </summary>

    // The rules currently in effect:
    // * The last object in the path must be a DependencyObject
    // * The last property (on that last object) must be a DependencyProperty
    // * Any of these objects may be Freezable objects.  There are two cases for
    //   this.
    //   1) The value of the first property is Frozen.  We might be able to
    //      handle this via the cloning mechanism, so we don't check Frozen-ness
    //      if we see the first property is Frozen.  Whether the cloning
    //      mechanism can be used is verified elsewhere.
    //   2) The value of the first property is not Frozen, or is not a Freezable
    //      at all.  In this case, the cloning code path does not apply, and
    //      thus we must not have any immutable Freezable objects any further
    //      down the line.

    // Another rule not enforced here:
    // * If cloning is required, the first property value must be a Freezable,
    //   which knows how to clone itself.  However, this is only needed in
    //   cases of complex property paths and is checked elsewhere.

    // Things we don't care about:
    // * Whether or not any of the intermediate objects are DependencyObject or
    //   not - this is supposed to work no matter the object type.
    // * Whether or not any of the intermediate properties are DP or not - this
    //   is supposed to work whether it's a CLR property or DependencyProperty.
    // * Whether or not any of the intermediate properties are animatable or not.
    //   Even though they are changing, we're not attaching an animation to clock
    //   to those properties specifically.
    // * By the same token, we don't care if any of them are marked Read-Only.

    // Note that this means: If the intention is to make something fixed, it is
    //  not sufficient to mark an intermediate property read-only and
    //  not-animatable.  In fact, in the current design, it is impossible to
    //  be 100% sure that something will stay put.
    internal static void VerifyPathIsAnimatable(PropertyPath path)
    {
        object    intermediateObject = null;
        object    intermediateProperty = null; // Might be DependencyProperty, PropertyInfo, or PropertyDescriptor
        bool      checkingFrozenState = true;
        Freezable intermediateFreezable = null;

        for( int i=0; i < path.Length; i++ )
        {
            intermediateObject = path.GetItem(i);
            intermediateProperty = path.GetAccessor(i);

            if( intermediateObject == null )
            {
                Debug.Assert( i > 0, "The caller should not have set the PropertyPath context to a null object." );
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathObjectNotFound, AccessorName(path, i-1), path.Path ));
            }

            if( intermediateProperty == null )
            {
                // Would love to throw error with the name of the property we couldn't find,
                //  but that information is not exposed from the PropertyPath class.
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathPropertyNotFound, path.Path ));
            }

            // If the first property value is an immutable Freezable, then turn
            //  off the Frozen state checking - let's hope we can use the cloning
            //  mechanism for that case.
            // Index of zero is the path context object itself, one (that we're
            //  checking here) is the value of the first property.
            // Example: Property path "Background.Opacity" as applied to Button.
            //  Object 0 is the Button, object 1 is the brush.
            if( i == 1 )
            {
                intermediateFreezable = intermediateObject as Freezable;
                if( intermediateFreezable != null && intermediateFreezable.IsFrozen )
                {
                    checkingFrozenState = false;
                }
            }
            // Freezable objects (other than the one returned as the value of
            //  the first property) must not be frozen if the first one isn't.
            else if( checkingFrozenState )
            {
                intermediateFreezable = intermediateObject as Freezable;
                if( intermediateFreezable != null && intermediateFreezable.IsFrozen )
                {
                    if( i > 0 )
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathFrozenCheckFailed, AccessorName(path, i-1), path.Path, intermediateFreezable.GetType().ToString() ));
                    }
                    else
                    {
                        // i == 0 means the targeted object itself is a frozen Freezable.
                        //  This need a different error message.
                        throw new InvalidOperationException(SR.Get(SRID.Storyboard_ImmutableTargetNotSupported, path.Path));
                    }
                }
            }

            // The last object + property pairing (the one we're actually going
            //  to stick the clock on) has further requirements.
            if( i == path.Length-1 )
            {
                DependencyObject intermediateDO = intermediateObject as DependencyObject;
                DependencyProperty intermediateDP = intermediateProperty as DependencyProperty;

                if( intermediateDO == null )
                {
                    Debug.Assert( i > 0, "The caller should not have set the PropertyPath context to a non DependencyObject." );
                    throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathMustPointToDependencyObject, AccessorName(path, i-1), path.Path));
                }

                if( intermediateDP == null )
                {
                    throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathMustPointToDependencyProperty, path.Path ));
                }

                if( checkingFrozenState && intermediateDO.IsSealed )
                {
                    throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathSealedCheckFailed, intermediateDP.Name, path.Path, intermediateDO));
                }

                if(!AnimationStorage.IsPropertyAnimatable(intermediateDO, intermediateDP) )
                {
                    throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathIncludesNonAnimatableProperty, path.Path, intermediateDP.Name));
                }
            }
        }
    }

    private static string AccessorName( PropertyPath path, int index )
    {
        object propertyAccessor = path.GetAccessor(index);

        if( propertyAccessor is DependencyProperty )
        {
            return ((DependencyProperty)propertyAccessor).Name;
        }
        else if( propertyAccessor is PropertyInfo )
        {
            return ((PropertyInfo)propertyAccessor).Name;
        }
        else if( propertyAccessor is PropertyDescriptor )
        {
            return ((PropertyDescriptor)propertyAccessor).Name;
        }
        else
        {
            return "[Unknown]";
        }
    }

    /// <summary>
    ///     Makes sure that the given clock can animate the given property -
    /// throw an exception otherwise.
    /// </summary>
    private static void VerifyAnimationIsValid( DependencyProperty targetProperty, AnimationClock animationClock )
    {
        if( !AnimationStorage.IsAnimationClockValid(targetProperty, animationClock) )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_AnimationMismatch, animationClock.Timeline.GetType(), targetProperty.Name, targetProperty.PropertyType));
        }
    }

    /// <summary>
    ///     For complex property paths, we need to dig our way down to the
    /// property and attach the animation clock there.  We will not be able to
    /// actually attach the clocks if the targetProperty points to a frozen
    /// Freezable.  More extensive handling will be required for that case.
    /// </summary>
    private void ProcessComplexPath( HybridDictionary clockMappings, DependencyObject targetObject,
        PropertyPath path, AnimationClock animationClock, HandoffBehavior handoffBehavior, Int64 layer )
    {
        Debug.Assert(path.Length > 1, "This method shouldn't even be called for a simple property path.");

        // For complex paths, the target object/property differs from the actual
        //  animated object/property.
        //
        // Example:
        //  TargetName="Rect1" TargetProperty="(Rectangle.LayoutTransform).(RotateTransform.Angle)"
        //
        // The target object is a Rectangle.
        // The target property is LayoutTransform.
        // The animated object is a RotateTransform
        // The animated property is Angle.

        // Currently unsolved problem: If the LayoutTransform is not a RotateTransform,
        //  we have no way of knowing.  We'll merrily set up to animate the Angle
        //  property as an attached property, not knowing that the value will be
        //  completely ignored.

        DependencyProperty targetProperty   = path.GetAccessor(0) as DependencyProperty;

        // Two different ways to deal with property paths.  If the target is
        //  on a frozen Freezable, we'll have to make a clone of the value and
        //  attach the animation on the clone instead.
        // For all other objects, we attach the animation clock directly on the
        //  specified animating object and property.
        object targetPropertyValue = targetObject.GetValue(targetProperty);

        DependencyObject   animatedObject   = path.LastItem as DependencyObject;
        DependencyProperty animatedProperty = path.LastAccessor as DependencyProperty;

        if( animatedObject == null ||
            animatedProperty == null ||
            targetProperty == null )
        {
            throw new InvalidOperationException(SR.Get(SRID.Storyboard_PropertyPathUnresolved, path.Path));
        }

        VerifyAnimationIsValid(animatedProperty, animationClock);

        if( PropertyCloningRequired( targetPropertyValue ) )
        {
            // Verify that property paths are supported for the specified
            //  object and property.  If the property value query (usually in
            //  GetValueCore) doesn't call into Storyboard code, then none of this
            //  will have any effect.  (Silently do nothing.)
            // Throwing here is for user's sake to alert that nothing will happen.
            VerifyComplexPathSupport( targetObject );

            // We need to clone the value of the target, and from here onwards
            //  try to pretend that it is the actual value.
            Debug.Assert(targetPropertyValue is Freezable, "We shouldn't be trying to clone a type we don't understand.  PropertyCloningRequired() has improperly flagged the current value as 'need to clone'.");

            // To enable animations on frozen Freezable objects, complex
            //  path processing is done on a clone of the value.
            Freezable clone = ((Freezable)targetPropertyValue).Clone();
            SetComplexPathClone( targetObject, targetProperty, targetPropertyValue, clone );

            // Promote the clone to the EffectiveValues cache
            targetObject.InvalidateProperty(targetProperty);

            // We're supposed to have the animatable clone in place by now.  But if
            //  things went sour for whatever reason, halt the app instead of corrupting
            //  the frozen object.
            if( targetObject.GetValue(targetProperty) != clone )
            {
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_ImmutableTargetNotSupported, path.Path));
            }

            // Now that we have a clone, update the animatedObject and animatedProperty
            //  with references to those on the clone.
            using(path.SetContext(targetObject))
            {
                animatedObject = path.LastItem as DependencyObject;
                animatedProperty = path.LastAccessor as DependencyProperty;
            }

            // And set up to listen to changes on this clone.
            ChangeListener.ListenToChangesOnFreezable(
                targetObject, clone, targetProperty, (Freezable)targetPropertyValue );
        }

        // Apply animation clock on the animated object/animated property.
        ObjectPropertyPair directApplyTarget = new ObjectPropertyPair( animatedObject, animatedProperty );
        UpdateMappings( clockMappings, directApplyTarget, animationClock );
    }

    private bool PropertyCloningRequired( object targetPropertyValue )
    {
        bool cloningRequired;

        if( targetPropertyValue is Freezable )
        {
            if( ((Freezable)targetPropertyValue).IsFrozen )
            {
                // The target property value is a Freezable, and is frozen.
                //  we will need to clone in order to use a complex property path.
                cloningRequired = true;
            }
            else
            {
                // The target property value is a Freezable, and is not frozen.
                //  We can apply the animation clocks directly.
                cloningRequired = false;
            }
        }
        else
        {
            // We have no idea what this might be and can't tell if we need to
            //  clone it.  But even if we do, we don't know how, so we won't.
            cloningRequired = false;
        }

        return cloningRequired;
    }

    /// <summary>
    ///     Check to see if the given object and property combination will be
    /// able to resolve complex paths.
    /// </summary>
    private void VerifyComplexPathSupport( DependencyObject targetObject )
    {
        if( FrameworkElement.DType.IsInstanceOfType(targetObject) )
        {
            // FrameworkElement and derived types are supported.
            return;
        }

        if( FrameworkContentElement.DType.IsInstanceOfType(targetObject) )
        {
            // FrameworkContentElement and derived types are supported.
            return;
        }

        // ... and anything else that knows to call into Storyboard.GetComplexPathValue.

        // Otherwise - throw.
        throw new InvalidOperationException(SR.Get(SRID.Storyboard_ComplexPathNotSupported, targetObject.GetType().ToString()));
    }

    /// <summary>
    ///     Check to see if there is a complex path that started with the
    /// given target object and property.  If so, process the complex path
    /// information and return the results.
    /// </summary>
    internal static void GetComplexPathValue(
            DependencyObject targetObject,
            DependencyProperty targetProperty,
        ref EffectiveValueEntry entry, 
            PropertyMetadata metadata)
    {
        CloneCacheEntry cacheEntry = GetComplexPathClone(targetObject, targetProperty);

        if (cacheEntry != null)
        {
            object baseValue = entry.Value;
            if (baseValue == DependencyProperty.UnsetValue)
            {
                // If the incoming baseValue is DependencyProperty.UnsetValue, that
                // means the current property value is the default value.  Either
                // the cacheEntry.Clone was a clone of a default value (and should be
                // returned to the caller) or someone called ClearValue() (and
                // cacheEntry.Clone should be cleared accordingly).
                // To distinguish these cases we must check the cached source
                // against the default value.
                //
                // We don't have to handle the ClearValue case in this clause;
                // the comparison with the cached source to the base value
                // will fail in that case (since the cached source won't be UnsetValue)
                // and we'll clear out the cache.

                Debug.Assert(cacheEntry.Source != DependencyProperty.UnsetValue,
                    "Storyboard complex path\u2019s clone cache should never contain DependencyProperty.UnsetValue.  Either something went wrong in Storyboard.ProcessComplexPath() or somebody else is messing with the Storyboard clone cache.");

                if (cacheEntry.Source == metadata.GetDefaultValue(targetObject, targetProperty))
                {
                    //  The cacheEntry.Clone is the clone of the default value.  In normal
                    //  non-Storyboard code paths, BaseValueSourceInternal is Unknown for default
                    //  values at this time, so we need to switch it over explicitly.
                    //
                    //  And to prevent DependencyObject.UpdateEffectiveValue from misconstruing this
                    //  as an unaltered default value (which would result in UEV thinking no change
                    //  in value occurred and discarding this new value), we will go ahead and set the 
                    //  animated value modifier on this value entry. (jeffbog:  B#1616678  5/19/2006)
                    //
                    //  In all other cases, valueSource should have the correct
                    //  valueSource corresponding to the object we cloned from,
                    //  so we don't need to do anything.

                    entry.BaseValueSourceInternal = BaseValueSourceInternal.Default;
                    entry.SetAnimatedValue(cacheEntry.Clone, DependencyProperty.UnsetValue);
                    return;
                }
            }

            // If the incoming baseValue is a deferred object, we need to get the
            //  real value to make a valid comparison against the cache entry source.
            DeferredReference deferredBaseValue = baseValue as DeferredReference;
            if (deferredBaseValue != null)
            {
                baseValue = deferredBaseValue.GetValue(entry.BaseValueSourceInternal);
                entry.Value = baseValue;
            }

            // If the incoming baseValue is different from the original source object that
            // we cloned and cached then we need to invalidate this cache. Otherwise we use
            // the value in the cache as is.
            if (cacheEntry.Source == baseValue)
            {
                CloneEffectiveValue(ref entry, cacheEntry);
                return;
            }
            else
            {
                // Setting to DependencyProperty.UnsetValue is how FrugalMap does delete.
                SetComplexPathClone(
                        targetObject, 
                        targetProperty, 
                        DependencyProperty.UnsetValue, 
                        DependencyProperty.UnsetValue);
            }
        }
    }

    private static void CloneEffectiveValue(ref EffectiveValueEntry entry, CloneCacheEntry cacheEntry)
    {
        object clonedValue = cacheEntry.Clone;
/*
        if (!entry.IsExpression)
        {
            if (entry.LocalValue != clonedValue)
            {
                entry.Value = clonedValue;
            }
        }
        else
        {
            ModifiedValue modifiedValue = entry.ModifiedValue;
            if (modifiedValue.ExpressionValue != clonedValue)
            {
                modifiedValue.ExpressionValue = clonedValue;
            }
        }
*/                
        if (!entry.IsExpression)
        {
            entry.Value = clonedValue;
        }
        else
        {
            entry.ModifiedValue.ExpressionValue = clonedValue;
        }
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkElement containingObject )
    {
        Begin( containingObject, HandoffBehavior.SnapshotAndReplace, false );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkElement containingObject, HandoffBehavior handoffBehavior )
    {
        Begin( containingObject, handoffBehavior, false );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkElement containingObject, bool isControllable )
    {
        Begin(containingObject, HandoffBehavior.SnapshotAndReplace, isControllable );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkElement containingObject, HandoffBehavior handoffBehavior, bool isControllable )
    {
        BeginCommon(containingObject, null, handoffBehavior, isControllable, Storyboard.Layers.Code );
    }

    /// <summary>
    ///     Begins all ControlTemplate animations underneath this storyboard, clock tree starts at the given Control.
    /// </summary>
    public void Begin( FrameworkElement containingObject, FrameworkTemplate frameworkTemplate )
    {
        Begin( containingObject, frameworkTemplate, HandoffBehavior.SnapshotAndReplace, false );
    }

    /// <summary>
    ///     Begins all ControlTemplate animations underneath this storyboard, clock tree starts at the given Control.
    /// </summary>
    public void Begin( FrameworkElement containingObject, FrameworkTemplate frameworkTemplate, HandoffBehavior handoffBehavior )
    {
        Begin( containingObject, frameworkTemplate, handoffBehavior, false );
    }

    /// <summary>
    ///     Begins all ControlTemplate animations underneath this storyboard, clock tree starts at the given Control.
    /// </summary>
    public void Begin( FrameworkElement containingObject, FrameworkTemplate frameworkTemplate, bool isControllable )
    {
        Begin(containingObject, frameworkTemplate, HandoffBehavior.SnapshotAndReplace, isControllable );
    }

    /// <summary>
    ///     Begins all ControlTemplate animations underneath this storyboard, clock tree starts at the given Control.
    /// </summary>
    public void Begin( FrameworkElement containingObject, FrameworkTemplate frameworkTemplate, HandoffBehavior handoffBehavior, bool isControllable )
    {
        BeginCommon(containingObject, frameworkTemplate, handoffBehavior, isControllable, Storyboard.Layers.Code );
    }

/*  This is the ContentControl+DataTemplate counterpert to Control+ControlTemplate above, need test signoff before enabling.
    /// <summary>
    ///     Begins all DataTemplate animations underneath this storyboard, clock tree starts at the given ContentControl.
    /// </summary>
    public void Begin( ContentControl contentControl, DataTemplate dataTemplate )
    {
        Begin( contentControl, dataTemplate, HandoffBehavior.SnapshotAndReplace, false );
    }

    /// <summary>
    ///     Begins all DataTemplate animations underneath this storyboard, clock tree starts at the given ContentControl.
    /// </summary>
    public void Begin( ContentControl contentControl, DataTemplate dataTemplate, HandoffBehavior handoffBehavior )
    {
        Begin( contentControl, dataTemplate, handoffBehavior, false );
    }

    /// <summary>
    ///     Begins all DataTemplate animations underneath this storyboard, clock tree starts at the given ContentControl.
    /// </summary>
    public void Begin( ContentControl contentControl, DataTemplate dataTemplate, bool isControllable )
    {
        Begin( contentControl, dataTemplate, HandoffBehavior.SnapshotAndReplace, isControllable );
    }

    /// <summary>
    ///     Begins all DataTemplate animations underneath this storyboard, clock tree starts at the given ContentControl.
    /// </summary>
    public void Begin( ContentControl contentControl, DataTemplate dataTemplate, HandoffBehavior handoffBehavior, bool isControllable )
    {
        BeginCommon( contentControl, dataTemplate, handoffBehavior, isControllable, Storyboard.Layers.Code );
    }
*/
    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkContentElement containingObject )
    {
        Begin( containingObject, HandoffBehavior.SnapshotAndReplace, false );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkContentElement containingObject, HandoffBehavior handoffBehavior )
    {
        Begin( containingObject, handoffBehavior, false );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkContentElement containingObject, bool isControllable )
    {
        Begin(containingObject, HandoffBehavior.SnapshotAndReplace, isControllable );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    public void Begin( FrameworkContentElement containingObject, HandoffBehavior handoffBehavior, bool isControllable )
    {
        BeginCommon(containingObject, null, handoffBehavior, isControllable, Storyboard.Layers.Code );
    }

    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts in "shared clocks" mode.
    /// </summary>
    public void Begin()
    {
        DependencyObject containingObject = this; // Helper.FindMentor(this);
        INameScope nameScope = null;
        HandoffBehavior handoffBehavior = HandoffBehavior.SnapshotAndReplace;
        bool isControllable =  true;
        Int64 layer = Storyboard.Layers.Code;

        BeginCommon(containingObject, nameScope, handoffBehavior, isControllable, layer);
    }
    
    /// <summary>
    ///     Begins all animations underneath this storyboard, clock tree starts at the given containing object.
    /// </summary>
    internal void BeginCommon( DependencyObject containingObject, INameScope nameScope,
        HandoffBehavior handoffBehavior, bool isControllable, Int64 layer)
    {
        if (containingObject == null)
        {
            throw new ArgumentNullException("containingObject");
        }

        if (!HandoffBehaviorEnum.IsDefined(handoffBehavior))
        {
            throw new ArgumentException(SR.Get(SRID.Storyboard_UnrecognizedHandoffBehavior));
        }

        if (BeginTime == null)
        {
            // a null BeginTime means to not allocate or start the clock
            return;
        }

        // It's not possible to begin when there is no TimeManager.  This condition
        //  is known to occur during app shutdown.  Since an app being shut down
        //  won't care about its Storyboards, we silently exit.
        // If we don't exit here, we'll need to catch and handle the "no time
        //  manager" exception implemented for bug #1247862
        if( MediaContext.CurrentMediaContext.TimeManager == null )
        {
            return;
        }


        if( TraceAnimation.IsEnabled )
        {
            TraceAnimation.TraceActivityItem(
                TraceAnimation.StoryboardBegin,
                this,
                Name,
                containingObject,
                nameScope );
        }


        // This table maps an [object,property] key pair to one or more clocks.
        // If we have knowledge of whether this Storyboard was changed between multiple
        //  Begin(), we can cache this.  But we don't know, so we don't cache.
        HybridDictionary simplePathClockMappings = new HybridDictionary();

        // Create (and Begin) a clock tree corresponding to this Storyboard timeline tree
        Clock storyboardClockTree = CreateClock(isControllable);

        // We now have one or more clocks that are created from this storyboard.
        //  However, the individual clocks are not necessarily intended for
        //  the containing object so we need to do a tree walk and sort out
        //  which clocks go on which objects and their properties.
        ClockTreeWalkRecursive(
            storyboardClockTree,
            containingObject,
            nameScope,
            null, /* target object */
            null, /* target object name */
            null, /* target property path */
            handoffBehavior,
            simplePathClockMappings,
            layer);

        // Apply the storyboard's animation clocks we found in the tree walk
        ApplyAnimationClocks( simplePathClockMappings, handoffBehavior, layer );

        if (isControllable)
        {
            // Save a reference to this clock tree on the containingObject.  We
            //  need it there in order to control this clock tree later.
            SetStoryboardClock(containingObject, storyboardClockTree);
        }

        return;
    }


    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current global speed.
    /// </summary>
    public Nullable<Double> GetCurrentGlobalSpeed( FrameworkElement containingObject )
    {
        return GetCurrentGlobalSpeedImpl(containingObject);
    }


    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current global speed.
    /// </summary>
    public Nullable<Double> GetCurrentGlobalSpeed( FrameworkContentElement containingObject )
    {
        return GetCurrentGlobalSpeedImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, return the current global speed.
    /// </summary>
    public Double GetCurrentGlobalSpeed()
    {
        Nullable<Double> currentGlobalSpeed = GetCurrentGlobalSpeedImpl(this);

        if(currentGlobalSpeed.HasValue)
        {
            return currentGlobalSpeed.Value;
        }
        else
        {
            return default(Double);
        }
    }

    private Nullable<Double> GetCurrentGlobalSpeedImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.CurrentGlobalSpeed;
        }

        return null;
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current iteration.
    /// </summary>
    public Nullable<Int32> GetCurrentIteration( FrameworkElement containingObject )
    {
        return GetCurrentIterationImpl(containingObject);
    }


    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current iteration.
    /// </summary>
    public Nullable<Int32> GetCurrentIteration( FrameworkContentElement containingObject )
    {
        return GetCurrentIterationImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, return the current iteration.
    /// </summary>
    public Int32 GetCurrentIteration()
    {
        Nullable<Int32> currentIteration = GetCurrentIterationImpl(this);

        if(currentIteration.HasValue)
        {
            return currentIteration.Value;
        }
        else
        {
            return default(Int32);
        }
    }

    private Nullable<Int32> GetCurrentIterationImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.CurrentIteration;
        }

        return null;
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current progress.
    /// </summary>
    public Nullable<Double> GetCurrentProgress( FrameworkElement containingObject )
    {
        return GetCurrentProgressImpl(containingObject);
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current progress.
    /// </summary>
    public Nullable<Double> GetCurrentProgress( FrameworkContentElement containingObject )
    {
        return GetCurrentProgressImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, return the current progress.
    /// </summary>
    public Double GetCurrentProgress()
    {
        Nullable<Double> currentProgress = GetCurrentProgressImpl(this);

        if(currentProgress.HasValue)
        {
            return currentProgress.Value;
        }
        else
        {
            return default(Double);
        }
    }

    private Nullable<Double> GetCurrentProgressImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.CurrentProgress;
        }

        return null;
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current state.
    /// </summary>
    public ClockState GetCurrentState( FrameworkElement containingObject )
    {
        return GetCurrentStateImpl(containingObject);
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current state.
    /// </summary>
    public ClockState GetCurrentState( FrameworkContentElement containingObject )
    {
        return GetCurrentStateImpl(containingObject);
    }

    /// <summary>
    ///     Return the current state of this storyboard.  This storyboard
    ///     must have been created in "shared clocks" mode.
    /// </summary>
    public ClockState GetCurrentState()
    {
        return GetCurrentStateImpl(this);
    }

    private ClockState GetCurrentStateImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.CurrentState;
        }

        return ClockState.Stopped; // Not default(ClockState)...
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current time.
    /// </summary>
    public Nullable<TimeSpan> GetCurrentTime( FrameworkElement containingObject )
    {
        return GetCurrentTimeImpl(containingObject);
    }


    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return the current time.
    /// </summary>
    public Nullable<TimeSpan> GetCurrentTime( FrameworkContentElement containingObject )
    {
        return GetCurrentTimeImpl(containingObject);
    }

    /// <summary>
    ///     Return the current time of this storyboard.  This storyboard
    ///     must have been created in "shared clocks" mode.
    /// </summary>
    public TimeSpan GetCurrentTime()
    {
        Nullable<TimeSpan> currentTime = GetCurrentTimeImpl(this);

        if(currentTime.HasValue)
        {
            return currentTime.Value;
        }
        else
        {
            return default(TimeSpan);
        }
    }

    private Nullable<TimeSpan> GetCurrentTimeImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.CurrentTime;
        }

        return null;
    }

    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return whether the clock is paused.
    /// </summary>
    public bool GetIsPaused( FrameworkElement containingObject )
    {
        return GetIsPausedImpl(containingObject);
    }


    /// <summary>
    ///  Given an object, look on the clock store for a clock that was
    ///  generated from 'this' storyboard.  If found, return whether the clock is paused.
    /// </summary>
    public bool GetIsPaused( FrameworkContentElement containingObject )
    {
        return GetIsPausedImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, return whether the clock is paused.
    /// </summary>
    public bool GetIsPaused()
    {
        return GetIsPausedImpl(this);
    }

    private bool GetIsPausedImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject);

        if (clock != null)
        {
            return clock.IsPaused;
        }

        // A clock that has been disposed is not in a paused state.
        return false;
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call pause on the clock.
    /// </summary>
    public void Pause( FrameworkElement containingObject )
    {
        PauseImpl(containingObject);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call pause on the clock.
    /// </summary>
    public void Pause( FrameworkContentElement containingObject )
    {
        PauseImpl(containingObject);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call pause on the clock.
    /// </summary>
    public void Pause()
    {
        PauseImpl(this);
    }

    private void PauseImpl(DependencyObject containingObject)
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.Pause);

        if (clock != null)
        {
            clock.Controller.Pause();
        }
        
        if( TraceAnimation.IsEnabled )
        {
            TraceAnimation.TraceActivityItem(
                TraceAnimation.StoryboardPause,
                this,
                Name,
                this );
        }
    }

    /// <summary>
    /// If a clock was generated from 'this' storyboard for the given object, remove it.
    /// </summary>
    public void Remove(FrameworkElement containingObject)
    {
        RemoveImpl(containingObject);
    }

    /// <summary>
    /// If a clock was generated from 'this' storyboard for the given object, remove it.
    /// </summary>
    public void Remove(FrameworkContentElement containingObject)
    {
        RemoveImpl(containingObject);
    }

    /// <summary>
    ///     If a clock was generated from this storyboard, remove it.
    /// </summary>
    public void Remove()
    {
        RemoveImpl(this);
    }

    private void RemoveImpl(DependencyObject containingObject)
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.Remove);

        if (clock != null)
        {
            clock.Controller.Remove();
            HybridDictionary clocks = StoryboardClockTreesField.GetValue(containingObject);
            if (clocks != null)
            {
                clocks.Remove(this);
            }
        }

        if( TraceAnimation.IsEnabled )
        {
            TraceAnimation.TraceActivityItem(
                TraceAnimation.StoryboardRemove,
                this,
                Name,
                containingObject );
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call resume on the clock.
    /// </summary>
    public void Resume( FrameworkElement containingObject )
    {
        ResumeImpl(containingObject);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call resume on the clock.
    /// </summary>
    public void Resume( FrameworkContentElement containingObject )
    {
        ResumeImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call resume on the clock.
    /// </summary>
    public void Resume()
    {
        ResumeImpl(this);
    }

    private void ResumeImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.Resume);

        if (clock != null)
        {
            clock.Controller.Resume();
        }

        if( TraceAnimation.IsEnabled )
        {
            TraceAnimation.TraceActivityItem(
                TraceAnimation.StoryboardResume,
                this,
                Name,
                containingObject );
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call seek on the clock
    /// with the given parameters.
    /// </summary>
    public void Seek( FrameworkElement containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekImpl(containingObject, offset, origin);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call seek on the clock
    /// with the given parameters.
    /// </summary>
    public void Seek( FrameworkContentElement containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekImpl(containingObject, offset, origin);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call seek on the clock with the given
    ///     parameters.
    /// </summary>
    public void Seek( TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekImpl(this, offset, origin);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call seek on the clock with the given
    ///     parameters.
    /// </summary>
    public void Seek( TimeSpan offset )
    {
        SeekImpl(this, offset, TimeSeekOrigin.BeginTime);
    }

    private void SeekImpl( DependencyObject containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.Seek);

        if (clock != null)
        {
            // Seek is a public API as well, so its parameters should get validated there.
            clock.Controller.Seek(offset, origin);
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call SeekAlignedToLastTick
    /// on the clock with the given parameters.
    /// </summary>
    public void SeekAlignedToLastTick( FrameworkElement containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekAlignedToLastTickImpl(containingObject, offset, origin);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call SeekAlignedToLastTick
    /// on the clock with the given parameters.
    /// </summary>
    public void SeekAlignedToLastTick( FrameworkContentElement containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekAlignedToLastTickImpl(containingObject, offset, origin);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call SeekAlignedToLastTick on the clock
    ///     with the given parameters.
    /// </summary>
    public void SeekAlignedToLastTick( TimeSpan offset, TimeSeekOrigin origin )
    {
        SeekAlignedToLastTickImpl(this, offset, origin);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call SeekAlignedToLastTick on the clock
    ///     with the given parameters.
    /// </summary>
    public void SeekAlignedToLastTick( TimeSpan offset )
    {
        SeekAlignedToLastTickImpl(this, offset, TimeSeekOrigin.BeginTime);
    }

    private void SeekAlignedToLastTickImpl( DependencyObject containingObject, TimeSpan offset, TimeSeekOrigin origin )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.SeekAlignedToLastTick);

        if (clock != null)
        {
            // SeekAlignedToLastTick is a public API as well, so its parameters should get validated there.
            clock.Controller.SeekAlignedToLastTick(offset, origin);
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, set the speed ratio on
    /// the clock to the given ratio.
    /// </summary>
    public void SetSpeedRatio( FrameworkElement containingObject, double speedRatio )
    {
        SetSpeedRatioImpl(containingObject, speedRatio);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, set the speed ratio on the clock
    /// with the given parameters.
    /// </summary>
    public void SetSpeedRatio( FrameworkContentElement containingObject, double speedRatio )
    {
        SetSpeedRatioImpl(containingObject, speedRatio);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, set the speed ratio on the clock with the
    ///     given parameters.
    /// </summary>
    public void SetSpeedRatio( double speedRatio )
    {
        SetSpeedRatioImpl(this, speedRatio);
    }

    private void SetSpeedRatioImpl( DependencyObject containingObject, double speedRatio )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.SetSpeedRatio);

        if (clock != null)
        {
            clock.Controller.SpeedRatio = speedRatio;
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call skip-to-fill on the clock.
    /// </summary>
    public void SkipToFill( FrameworkElement containingObject )
    {
        SkipToFillImpl(containingObject);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call skip-to-fill on the clock.
    /// </summary>
    public void SkipToFill( FrameworkContentElement containingObject )
    {
        SkipToFillImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call skip-to-fill on the clock.
    /// </summary>
    public void SkipToFill()
    {
        SkipToFillImpl(this);
    }

    private void SkipToFillImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.SkipToFill);

        if (clock != null)
        {
            clock.Controller.SkipToFill();
        }
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call stop on the clock.
    /// </summary>
    public void Stop( FrameworkElement containingObject )
    {
        StopImpl(containingObject);
    }

    /// <summary>
    ///     Given an object, look on the clock store for a clock that was
    /// generated from 'this' storyboard.  If found, call stop on the clock.
    /// </summary>
    public void Stop( FrameworkContentElement containingObject )
    {
        StopImpl(containingObject);
    }

    /// <summary>
    ///     Look on the clock store for a clock that was generated from this
    ///     storyboard.  If found, call stop on the clock.
    /// </summary>
    public void Stop()
    {
        StopImpl(this);
    }

    private void StopImpl( DependencyObject containingObject )
    {
        Clock clock = GetStoryboardClock(containingObject, false, InteractiveOperation.Stop);

        if (clock != null)
        {
            clock.Controller.Stop();
        }

        if( TraceAnimation.IsEnabled )
        {
            TraceAnimation.TraceActivityItem(
                TraceAnimation.StoryboardStop,
                this,
                Name,
                containingObject );
        }
    }

    /// <summary>
    ///   HybridDictionary for storing the root clock tree for each storyboard.
    ///     Key: An instance of the Storyboard object.
    ///     Value: The root of the clock tree that was created from the key object.
    /// </summary>
    /// <remarks>
    ///     Another way to describe the key-value relation is that the value
    /// clock object's Timeline property points to the Storyboard.
    /// </remarks>
    private static readonly UncommonField<HybridDictionary> StoryboardClockTreesField = new UncommonField<HybridDictionary>();

    /// <summary>
    ///     Given an object, look in the attached storage for storyboard clocks
    /// and retrieve the one that is associated with 'this' Storyboard instance.
    /// throw if not found.
    /// </summary>
    private Clock GetStoryboardClock(DependencyObject o)
    {
        return GetStoryboardClock(o, true, InteractiveOperation.Unknown);
    }

    /// <summary>
    ///     Given an object, look in the attached storage for storyboard clocks
    /// and retrieve the one that is associated with 'this' Storyboard instance.
    /// If the clock is null we'll either throw an exception or emit a trace, depending
    /// on the value of the throwIfNull parameter.  The InteractiveOperation
    /// parameter is used to give more detailed info in the trace.
    /// 
    /// </summary>
    private Clock GetStoryboardClock(DependencyObject o, bool throwIfNull, InteractiveOperation operation)
    {
        Clock clock = null;
        WeakReference clockReference = null;

        HybridDictionary clocks = StoryboardClockTreesField.GetValue(o);

        if (clocks != null)
        {
            clockReference = clocks[this] as WeakReference;
        }

        if (clockReference == null)
        {
            if (throwIfNull)
            {
                // This exception indicates that the storyboard has never been applied.
                // We check the weak reference because the only way it can be null
                // is if it had never been put in the dictionary.
                throw new InvalidOperationException(SR.Get(SRID.Storyboard_NeverApplied));
            }
            else  if (TraceAnimation.IsEnabledOverride )
            {
                TraceAnimation.Trace(
                    TraceEventType.Warning,
                    TraceAnimation.StoryboardNotApplied,
                    operation,
                    this,
                    o);
            }
        }



        if (clockReference != null)
        {
            clock = clockReference.Target as Clock;

            // At this point the clock may have been garbage collected.
            // We may have a null clock even though this Storyboard had
            // been applied to the given DependencyObject. One way this
            // can happen is if another Storyboard Begins an animation
            // on that same DO / DP pair with SnapshotAndReplace semantics.
            // In that case AnimationStorage will toss out the old clock.
        }

        return clock;
    }


    /// <summary>
    ///     Given an object, and a clock to associate with 'this' Storyboard
    /// instance, save a reference to the clock on the object's attached storage
    /// for storyboard clocks.  We are storing a weak reference so that the
    /// clock is not kept alive.  Currently we don't have a way of removing
    /// clocks from the list when it is no longer required.
    /// </summary>
    /// <remarks>
    ///     We don't care if there's already a clock there - if there is one,
    /// the reference is overridden in the HybridDictionary, and the old clock
    /// is abandoned.
    /// </remarks>
    private void SetStoryboardClock(DependencyObject o, Clock clock)
    {
        HybridDictionary clocks = StoryboardClockTreesField.GetValue(o);

        if (clocks == null)
        {
            clocks = new HybridDictionary();
            StoryboardClockTreesField.SetValue(o, clocks);
        }

        clocks[this] = new WeakReference(clock);

        return;
    }

    /// <summary>
    ///     The complex path clone storage field stores the clone that we're using
    /// in place of the original value.
    /// </summary>
    /// <remarks>
    ///     This field is attached to the target object from which the path
    /// starts.  The field is a map indexed by the property affected.  For the
    /// example
    ///
    ///  TargetName="Rect1" TargetProperty="(Rectangle.LayoutTransform).(RotateTransform.Angle)"
    ///
    ///     The FrugalMap will be attached to whatever "Rect1" is.  The data
    /// will then be stored in the FrugalMap at the index for the property
    /// (in this case the LayoutTransformProperty.GlobalIndex)
    /// </remarks>
    private static readonly UncommonField<FrugalMap> ComplexPathCloneField = new UncommonField<FrugalMap>();

    private static CloneCacheEntry GetComplexPathClone(DependencyObject o, DependencyProperty dp)
    {
        FrugalMap clonesMap = ComplexPathCloneField.GetValue(o);

        // FrugalMap is a struct, so no need to check against null.
        // when there is no clones field on this object we will get a FrugalMap with no elements.
        object value = clonesMap[dp.GlobalIndex];
        if (value != DependencyProperty.UnsetValue)
        {
            return (CloneCacheEntry)clonesMap[dp.GlobalIndex];
        }
        else
        {
            return null;
        }
    }

    private static void SetComplexPathClone(
        DependencyObject    o,
        DependencyProperty  dp,
        object              source,
        object              clone)
    {
        FrugalMap clonesMap = ComplexPathCloneField.GetValue(o);

        if (clone != DependencyProperty.UnsetValue)
        {
            clonesMap[dp.GlobalIndex] = new CloneCacheEntry(source, clone);
        }
        else
        {
            clonesMap[dp.GlobalIndex] = DependencyProperty.UnsetValue;
        }

        // FrugalMap is a struct - after a change it needs to be set back on the object.
        ComplexPathCloneField.SetValue(o, clonesMap);
    }

    // This is the entry in the ComplexPathClone cache
    private class CloneCacheEntry
    {
        internal CloneCacheEntry(object source, object clone)
        {
            Source = source;
            Clone = clone;
        }

        internal object Source;
        internal object Clone;
    }

    // Small object used to send a property invalidation when the InvalidatePropertyOnChange
    //  delegate is called in response to an event.
    // The ChangeListener class supports Storyboard animation scenarios with
    //  multi-step property paths.  In these cases, a clone of the original
    //  value is made and the storyboard animation is attached to the clone.
    // This class listens to the changes on both the original object and the
    //  clone.
    // If the original object has changed, this class signals the need to
    //  re-clone in order to pick up the state of the original object.
    // If the cloned object has changed, this class signals an animation-
    //  driven sub-property invalidation.
    internal class ChangeListener
    {
        // Constructor of the object, the parameters include the property to
        //  invalidate and the object to invalidate it on.  As well as the
        //  two Freezable objects (original and clone) that are associated
        //  with the property on the target object.
        internal ChangeListener( DependencyObject target, Freezable clone, DependencyProperty property, Freezable original )
        {
            Debug.Assert( target != null && clone != null && property != null && original != null,
                "Internal utility class requires non-null arguments.  Check the caller of this method for an error.");
            _target = target;
            _property = property;
            _clone = clone;
            _original = original;
        }

        // Called when the clone has changed.  We check the clone cache on
        //  the target object to see if we were the most recent clone.  If so,
        //  signal a sub-property invalidation.  If not, we are no longer
        //  relevant and we should clean up.
        internal void InvalidatePropertyOnCloneChange( object source, EventArgs e )
        {
            CloneCacheEntry cacheEntry = GetComplexPathClone( _target, _property );

            // If the changed freezable is the currently outstanding instance
            //  then we need to trigger a sub-property invalidation.
            if( cacheEntry != null && cacheEntry.Clone == _clone )
            {
                _target.InvalidateSubProperty(_property);
            }
            // Otherwise, we are no longer relevant and need to clean up.
            else
            {
                Cleanup();
            }
        }

        // This is the event handler on the original.  When the original
        //  changes, the clone is no longer valid.  This method triggers a
        //  re-clone by calling InvalidateProperty, then clean up.  Now that
        //  the associated clone is no longer valid, there's nothing useful
        //  for us to listen on.
        internal void InvalidatePropertyOnOriginalChange( object source, EventArgs e )
        {
            // recompute animated value
            _target.InvalidateProperty(_property);
            Cleanup();
        }

        // This is the internal method called to set up the listeners on both
        //  the original and the clone.
        internal static void ListenToChangesOnFreezable(
            DependencyObject target,
            Freezable clone,
            DependencyProperty dp,
            Freezable original)
        {
            ChangeListener listener = new ChangeListener( target, clone, dp, original );

            listener.Setup();
        }

        private void Setup()
        {
            EventHandler changeEventHandler = new EventHandler(InvalidatePropertyOnCloneChange);

            // Listen to changes on clone.
            _clone.Changed += changeEventHandler;

            if( _original.IsFrozen )
            {
                // We skip setting up for the original object when it is Frozen,
                //  because it won't change so we don't need to worry about listening.
                _original = null;
            }
            else
            {
                // If the original is not Frozen, we do need to listen and
                //  signal a re-clone if the original changes.
                changeEventHandler = new EventHandler(InvalidatePropertyOnOriginalChange);

                _original.Changed += changeEventHandler;
            }
        }

        // Stop listening to the Changed event on the given Freezable objects
        //  and clean up.
        private void Cleanup()
        {
            // Remove ourself from the clone
            EventHandler changeEventHandler = new EventHandler(InvalidatePropertyOnCloneChange);

            _clone.Changed -= changeEventHandler;

            // If we're listening on the original, remove ourselves from there too.
            //  (In Setup() _original was nulled out if we aren't listening.)
            if( _original != null )
            {
                changeEventHandler = new EventHandler(InvalidatePropertyOnOriginalChange);

                _original.Changed -= changeEventHandler;
            }

            // Clear all object references.
            _target = null;
            _property = null;
            _clone = null;
            _original = null;
        }

        DependencyObject _target;     // The object to invalidate
        DependencyProperty _property; // The property to invalidate on the above object.
        Freezable _clone;             // The cloned Freezable whose Changed event we were listening to.
        Freezable _original;          // The original Freezable whose Changed event we're also listening to.
    }

    internal static class Layers
    {
        internal static Int64 ElementEventTrigger = 1;
        internal static Int64 StyleOrTemplateEventTrigger = 1;
        internal static Int64 Code = 1;
        internal static Int64 PropertyTriggerStartLayer = 2; // First PropertyTrigger takes this layer number.
    }

    // Describes the various interactive operations we can do to a controllable
    // storyboard.  Used by GetStoryboardClock for debug tracing.
    private enum InteractiveOperation : ushort
    {
        Unknown = 0,
        Pause, 
        Remove, 
        Resume,
        Seek,
        SeekAlignedToLastTick,
        SetSpeedRatio,
        SkipToFill,
        Stop
    }
}
}
