// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* A TriggerAction is stored within a Trigger object as one of the actions to
*  be performed by that trigger.  A Trigger object is similar to an if/then
*  statement, and by that analogy a TriggerActionCollection is the entire
*  "then" block of the Trigger, and a TriggerAction is a single line within
*  the "then" block.
*
*
\***************************************************************************/
using System;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    ///   A class that describes an action to perform for a trigger
    /// </summary>
    public abstract class TriggerAction : DependencyObject
    {
        /// <summary>
        ///     Internal constructor - nobody is supposed to ever create an instance
        /// of this class.  Use a derived class instead.
        /// </summary>
        internal TriggerAction()
        {
        }

        /// <summary>
        ///     Called when all conditions have been satisfied for this action to be
        /// invoked.  (Conditions are not described on this TriggerAction object,
        /// but on the Trigger object holding it.)
        /// </summary>
        /// <remarks>
        ///     This variant is called when the Trigger lives in a Style, and
        /// hence given a reference to its corresponding Style object.
        /// </remarks>
        internal abstract void Invoke( FrameworkElement fe,
                                      FrameworkContentElement fce,
                                      Style targetStyle,
                                      FrameworkTemplate targetTemplate,
                                      Int64 layer);

        /// <summary>
        ///     Called when all conditions have been satisfied for this action to be
        /// invoked.  (Conditions are not described on this TriggerAction object,
        /// but on the Trigger object holding it.)
        /// </summary>
        /// <remarks>
        ///     This variant is called when the Trigger lives on an element, as
        /// opposed to Style, so it is given only the reference to the element.
        /// </remarks>
        internal abstract void Invoke( FrameworkElement fe );

        /// <summary>
        ///     The EventTrigger object that contains this action.
        /// </summary>
        /// <remarks>
        ///     A TriggerAction may need to get back to the Trigger that
        /// holds it, this is the back-link to allow that.  Also, this allows
        /// us to verify that each TriggerAction is associated with one and
        /// only one Trigger.
        /// </remarks>
        internal TriggerBase ContainingTrigger
        {
            get
            {
                return _containingTrigger;
            }
        }

        /// <summary>
        ///     Seal this TriggerAction to prevent further updates
        /// </summary>
        /// <remarks>
        ///     TriggerActionCollection will call this method to seal individual
        /// TriggerAction objects.  We do some check here then call the
        /// parameter-less Seal() so subclasses can also do what they need to do.
        /// </remarks>
        internal void Seal( TriggerBase containingTrigger )
        {
            if( IsSealed && containingTrigger != _containingTrigger )
            {
                throw new InvalidOperationException(SR.Get(SRID.TriggerActionMustBelongToASingleTrigger));
            }
            _containingTrigger = containingTrigger;
            Seal();
        }

        /// <summary>
        ///     A derived class overrideing Seal() should set object state such
        /// that further changes are not allowed.  This is also a time to make
        /// validation checks to see if all parameters make sense.
        /// </summary>
        internal override void Seal()
        {
            if( IsSealed )
            {
                throw new InvalidOperationException(SR.Get(SRID.TriggerActionAlreadySealed));
            }
            base.Seal();
        }

        /// <summary>
        ///     Checks sealed status and throws exception if object is sealed
        /// </summary>
        internal void CheckSealed()
        {
            if( IsSealed )
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "TriggerAction"));
            }
        }

        // Define the DO's inheritance context

        internal override DependencyObject InheritanceContext
        {
            get { return _inheritanceContext; }
        }

        // Receive a new inheritance context (this will be a FE/FCE)
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.AddInheritanceContext(context,
                                                              this,
                                                              ref _hasMultipleInheritanceContexts,
                                                              ref _inheritanceContext);
        }

        // Remove an inheritance context (this will be a FE/FCE)
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.RemoveInheritanceContext(context,
                                                                  this,
                                                                  ref _hasMultipleInheritanceContexts,
                                                                  ref _inheritanceContext);
        }

        /// <summary>
        ///     Says if the current instance has multiple InheritanceContexts
        /// </summary>
        internal override bool HasMultipleInheritanceContexts
        {
            get { return _hasMultipleInheritanceContexts; }
        }


        private TriggerBase _containingTrigger = null;

        // Fields to implement DO's inheritance context
        private DependencyObject _inheritanceContext = null;
        private bool _hasMultipleInheritanceContexts = false;
    }
}
