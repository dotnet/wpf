// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.IO;
using System.Windows.Markup;

using MS.Utility;
using MS.Internal;

using System;
using System.ComponentModel;            // DesignerSerializationVisibilityAttribute & DefaultValue
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    ///     A visual trigger is the base class for specifying a conditional
    /// value within a Style object.
    /// </summary>
    /// <remarks>
    ///     The TriggerBase class deals with the values, and a derived class
    /// is responsible for handling the conditions that determine whether a
    /// value is to be used.
    ///
    ///     In other words, the TriggerBase class stores the "then" portion
    /// of an if/then.  The derived class handles the "if" part.
    ///
    ///     The most common derived type is the Trigger class.  A
    /// property trigger is conditional on property values, creating logic like:
    ///
    ///     if( MouseOver == true ) then (Background = Red )
    ///
    ///     The various Set methods in this class handles the (Background = Red) portion
    /// of this logic.
    /// </remarks>
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    public abstract class TriggerBase : DependencyObject
    {
        internal TriggerBase()
        {
        }

        /// <summary>
        ///     A collection of trigger actions to perform when this trigger
        /// object becomes active.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TriggerActionCollection EnterActions
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if( _enterActions == null )
                {
                    _enterActions = new TriggerActionCollection();
                    if( IsSealed )
                    {
                        // This collection might receive its first query after
                        //  the containing trigger had already been sealed
                        _enterActions.Seal(this);
                    }
                }
                return _enterActions;
            }
        }

        // Internal way to check without triggering a pointless allocation
        internal bool HasEnterActions { get { return _enterActions != null && _enterActions.Count > 0; } }

        /// <summary>
        ///     A collection of trigger actions to perform when this trigger
        /// object becomes inactive.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TriggerActionCollection ExitActions
        {
            get
            {
                // Verify Context Access
                VerifyAccess();

                if( _exitActions == null )
                {
                    _exitActions = new TriggerActionCollection();
                    if( IsSealed )
                    {
                        // This collection might receive its first query after
                        //  the containing trigger had already been sealed
                        _exitActions.Seal(this);
                    }
                }
                return _exitActions;
            }
        }

        // Internal way to check without triggering a pointless allocation
        internal bool HasExitActions { get { return _exitActions != null && _exitActions.Count > 0; } }

//  Here's the internal version that does what Robby thinks it should do.
        internal bool ExecuteEnterActionsOnApply
        {
            get
            {
                return true;
            }
        }

        internal bool ExecuteExitActionsOnApply
        {
            get
            {
                return false;
            }
        }

/*  Here's the version that looks like a public API.  (Needs Style/Template parser changes before these will actually parse.)

        // These fields determine what we do about any existing Enter/ExitActions
        //  when the Trigger is first applied.

        /// <summary>
        /// If set to 'true', EnterActions will be executed immediately if the
        ///  conditions are met, before any "enter" change has occurred.
        /// </summary>

        // This is on the assumption that the "enter" change has occurred already.
        //  For example: <CheckBox IsChecked="true" /> the IsChecked property
        //  change occurred before the CheckBox Style/Template is applied.
        [DefaultValue(false)]
        public bool ExecuteEnterActionsOnApply
        {
            get
            {
                return _executeEnterActionsOnApply;
            }
            set
            {
                if (IsSealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "trigger"));
                }

                _executeEnterActionsOnApply = value;
            }
        }

        /// <summary>
        /// If set to 'true', ExitActions will be executed immediately if the
        ///  conditions are not met, before any "exit" change has occurred.
        /// </summary>
        [DefaultValue(false)]
        public bool ExecuteExitActionsOnApply
        {
            get
            {
                return _executeExitActionsOnApply;
            }
            set
            {
                if (IsSealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "trigger"));
                }

                _executeExitActionsOnApply = value;
            }
        }

*/
        /// <summary>
        ///     Parameter validation work common to the SetXXXX methods that deal
        /// with the container node of the Style/Template.
        /// </summary>
        internal void ProcessParametersContainer(DependencyProperty dp)
        {
            // Not allowed to use Style to affect the StyleProperty.
            if (dp == FrameworkElement.StyleProperty)
            {
                throw new ArgumentException(SR.Get(SRID.StylePropertyInStyleNotAllowed));
            }
        }

        /// <summary>
        ///     Parameter validation work common to the SetXXXX methods that deal
        /// with visual tree child nodes.
        /// </summary>
        internal string ProcessParametersVisualTreeChild(DependencyProperty dp, string target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (target.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.ChildNameMustBeNonEmpty));
            }

            return String.Intern(target);
        }

        /// <summary>
        ///     After the parameters have been validated, store it in the
        /// PropertyValues collection.
        /// </summary>
        /// <remarks>
        ///     All these will be looked at again (and processed into runtime
        /// data structures) by Style.Seal(). We keep them around even after
        /// that point should we need to serialize this data back out.
        /// </remarks>
        internal void AddToPropertyValues(string childName, DependencyProperty dp, object value, PropertyValueType valueType)
        {
            // Store original data
            PropertyValue propertyValue = new PropertyValue();
            propertyValue.ValueType = valueType;
            propertyValue.Conditions = null;  // Delayed - derived class is responsible for this item.
            propertyValue.ChildName = childName;
            propertyValue.Property = dp;
            propertyValue.ValueInternal = value;

            PropertyValues.Add(propertyValue);
        }

        internal override void Seal()
        {
            // Verify Context Access
            VerifyAccess();

            base.Seal();

            // Super classes have added all delayed conditions

            // Track Dependent/Source relationship for prediction
            for (int i = 0; i < PropertyValues.Count; i++)
            {
                PropertyValue propertyValue = PropertyValues[i];

                DependencyProperty dependent = propertyValue.Property;

                for (int j = 0; j < propertyValue.Conditions.Length; j++)
                {
                    DependencyProperty source = propertyValue.Conditions[j].Property;

                    // Check for obvious cycles.  Don't test for cycles if we have
                    // something other than self as the target, since this means that
                    // the templatedParent is presumably not the target.  See windows bug
                    // 984916 for details.
                    if (source == dependent && propertyValue.ChildName == StyleHelper.SelfName)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.PropertyTriggerCycleDetected, source.Name));
                    }
                }
            }

            if( _enterActions != null )
            {
                _enterActions.Seal(this);
            }
            if( _exitActions != null )
            {
                _exitActions.Seal(this);
            }

            // Remove thread affinity so it can be accessed across threads
            DetachFromDispatcher();
        }

        // This will transfer information in the _setters collection to PropertyValues array.
        internal void ProcessSettersCollection(SetterBaseCollection setters)
        {
            // Add information in Setters collection to PropertyValues array.
            if( setters != null )
            {
                // Seal Setters
                setters.Seal();

                for (int i = 0; i < setters.Count; i++ )
                {
                    Setter setter = setters[i] as Setter;
                    if( setter != null )
                    {
                        DependencyProperty dp = setter.Property;
                        object value          = setter.ValueInternal;
                        string target         = setter.TargetName;

                        if( target == null )
                        {
                            ProcessParametersContainer(dp);

                            target = StyleHelper.SelfName;
                        }
                        else
                        {
                            target = ProcessParametersVisualTreeChild(dp, target); // name string will get interned
                        }

                        DynamicResourceExtension dynamicResource = value as DynamicResourceExtension;
                        if (dynamicResource == null)
                        {
                            AddToPropertyValues(target, dp, value, PropertyValueType.Trigger);
                        }
                        else
                        {
                            AddToPropertyValues(target, dp, dynamicResource.ResourceKey, PropertyValueType.PropertyTriggerResource);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Get(SRID.VisualTriggerSettersIncludeUnsupportedSetterType, setters[i].GetType().Name));
                    }
                }
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

        // Says if the current instance has multiple InheritanceContexts

        internal override bool HasMultipleInheritanceContexts
        {
            get { return _hasMultipleInheritanceContexts; }
        }

        // This ranking is used when trigger needs to be sorted relative to
        //  the ordering, as when determining precedence for enter/exit
        //  animation composition.  Otherwise, it stays at default value of zero.
        internal Int64 Layer
        {
            get { return _globalLayerRank; }
        }

        // Set self rank to current number, increment global static.
        internal void EstablishLayer()
        {
            if( _globalLayerRank == 0 )
            {
                lock(Synchronized)
                {
                    _globalLayerRank = _nextGlobalLayerRank++;
                }

                if( _nextGlobalLayerRank == Int64.MaxValue )
                {
                    throw new InvalidOperationException(SR.Get(SRID.PropertyTriggerLayerLimitExceeded));
                }
            }
        }

        // evaluate the current state of the trigger
        internal virtual bool GetCurrentState(DependencyObject container, UncommonField<HybridDictionary[]> dataField)
        {
            Debug.Assert( false,
                "This method was written to handle Trigger, MultiTrigger, DataTrigger, and MultiDataTrigger.  It looks like a new trigger type was added - please add support as appropriate.");

            return false;
        }

        // Collection of TriggerConditions
        internal TriggerCondition[] TriggerConditions
        {
            get { return _triggerConditions; }
            set { _triggerConditions = value; }
        }

        // Synchronized (write locks, lock-free reads): Covered by the TriggerBase instance
        /* property */ internal FrugalStructList<System.Windows.PropertyValue> PropertyValues = new FrugalStructList<System.Windows.PropertyValue>();

        // Global, cross-object synchronization
        private static object Synchronized = new object();

        // Conditions
        TriggerCondition[] _triggerConditions;

        // Fields to implement DO's inheritance context
        private DependencyObject _inheritanceContext = null;
        private bool _hasMultipleInheritanceContexts = false;

        // Fields to handle enter/exit actions.
        private TriggerActionCollection _enterActions = null;
        private TriggerActionCollection _exitActions = null;

//      On hold - is this a new public API we want to do?
//        private bool _executeEnterActionsOnApply = false;
//        private bool _executeExitActionsOnApply = false;

        private Int64 _globalLayerRank = 0;
        private static Int64 _nextGlobalLayerRank = System.Windows.Media.Animation.Storyboard.Layers.PropertyTriggerStartLayer;
    }
}
