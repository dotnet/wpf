// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines BindingGroup object, manages a collection of bindings.
//

using System;
using System.Collections;               // IList
using System.Collections.Generic;       // IList<T>
using System.Collections.ObjectModel;   // Collection<T>
using System.Collections.Specialized;   // INotifyCollectionChanged
using System.ComponentModel;            // IEditableObject
using System.Diagnostics;               // Debug
using System.Globalization;             // CultureInfo
using System.Threading;                 // Thread

using System.Windows;
using System.Windows.Controls;          // ValidationRule
using MS.Internal.Controls;             // ValidationRuleCollection
using MS.Internal;                      // InheritanceContextHelper
using MS.Internal.Data;                 // DataBindEngine

namespace System.Windows.Data
{
    /// <summary>
    /// A BindingGroup manages a collection of bindings, and provides services for
    /// item-level and cross-binding validation.
    /// </summary>
    public class BindingGroup : DependencyObject
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        ///     Initializes a new instance of the BindingGroup class.
        /// </summary>
        public BindingGroup()
        {
            _validationRules = new ValidationRuleCollection();
            Initialize();
        }

        // clone the binding group.  Called when setting a binding group on a
        // container, from the ItemControl's ItemBindingGroup.
        internal BindingGroup(BindingGroup master)
        {
            _validationRules = master._validationRules;
            _name = master._name;
            _notifyOnValidationError = master._notifyOnValidationError;
            _sharesProposedValues = master._sharesProposedValues;
            _validatesOnNotifyDataError = master._validatesOnNotifyDataError;
            Initialize();
        }

        void Initialize()
        {
            _engine = DataBindEngine.CurrentDataBindEngine;
            _bindingExpressions = new BindingExpressionCollection();
            ((INotifyCollectionChanged)_bindingExpressions).CollectionChanged += new NotifyCollectionChangedEventHandler(OnBindingsChanged);

            _itemsRW = new Collection<WeakReference>();
            _items = new WeakReadOnlyCollection<object>(_itemsRW);
        }


        #endregion Constructors

        #region Public properties

        //------------------------------------------------------
        //
        //  Public properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The DependencyObject that owns this BindingGroup
        /// </summary>
        public DependencyObject Owner
        {
            get { return InheritanceContext; }
        }

        /// <summary>
        /// The validation rules belonging to a BindingGroup are run during the
        /// process of updating the source values of the bindings.  Each rule
        /// indicates where in that process it should run.
        /// </summary>
        public Collection<ValidationRule> ValidationRules
        {
            get { return _validationRules; }
        }

        /// <summary>
        /// The collection of binding expressions belonging to this BindingGroup.
        /// </summary>
        public Collection<BindingExpressionBase> BindingExpressions
        {
            get { return _bindingExpressions; }
        }

        /// <summary>
        /// The name of this BindingGroup.  A binding can elect to join this group
        /// by declaring its BindingGroupName to match the name of the group.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// When NotifyOnValidationError is set to True, the binding group will
        /// raise a Validation.ValidationError event when its validation state changes.
        /// </summary>
        public bool NotifyOnValidationError
        {
            get { return _notifyOnValidationError; }
            set { _notifyOnValidationError = value; }
        }

        /// <summary>
        /// When ValidatesOnNotifyDataError is true, the binding group will listen
        /// to the ErrorsChanged event on each item that implements INotifyDataErrorInfo
        /// and report entity-level validation errors.
        /// </summary>
        public bool ValidatesOnNotifyDataError
        {
            get { return _validatesOnNotifyDataError; }
            set { _validatesOnNotifyDataError = value; }
        }

        /// <summary>
        /// Enables (or disables) the sharing of proposed values.
        /// </summary>
        /// <remarks>
        /// Some UI designs edit multiple properties of a given data item using two
        /// templates for each property - a "display template" that shows the current
        /// proposed value in non-editable controls, and an "editing template" that
        /// holds the proposed value in editable controls and allows the user to edit
        /// the value.  As the user moves from one property to another, the templates
        /// are swapped so that the first property uses its display template while the
        /// second uses its editing template.  The proposed value in the first property's
        /// departing editing template should be preserved ("shared") so that (a) it
        /// can be displayed in the arriving display template, and (b) it can be
        /// eventually written out to the data item at CommitEdit time.  The BindingGroup
        /// will implement this when SharesProposedValues is true.
        /// </remarks>
        public bool SharesProposedValues
        {
            get { return _sharesProposedValues; }
            set
            {
                if (_sharesProposedValues != value)
                {
                    _proposedValueTable.Clear();
                    _sharesProposedValues = value;
                }
            }
        }

        /// <summary>
        /// CanRestoreValues returns True if the binding group can restore
        /// each of its sources (during <see cref="CancelEdit"/>) to the state
        /// they had at the time of the most recent <see cref="BeginEdit"/>.
        /// This depends on whether the current sources provide a suitable
        /// mechanism to implement the rollback, such as <seealso cref="IEditableObject"/>.
        /// </summary>
        public bool CanRestoreValues
        {
            get
            {
                IList items = Items;
                for (int i=items.Count-1; i>=0; --i)
                {
                    if (!(items[i] is IEditableObject))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// The collection of items used as sources in the bindings owned by
        /// this BindingGroup.  Each item appears only once, even if it is used
        /// by several bindings.
        /// </summary>
        /// <remarks>
        /// The Items property returns a snapshot collection, reflecting the state
        /// of the BindingGroup at the time of the call.  As bindings in the group
        /// change to use different source items, the changes are not immediately
        /// visible in the collection.  They become visible only when the property is
        /// queried again.
        /// </remarks>
        public IList Items
        {
            get
            {
                EnsureItems();
                return _items;
            }
        }

        /// <summary>
        /// Return true if the BindingGroup has proposed values that have not yet
        /// been written to their respective source properties.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_proposedValueTable.Count > 0)
                    return true;

                foreach (BindingExpressionBase bb in _bindingExpressions)
                {
                    if (bb.IsDirty)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Return true if the BindingGroup has any validation errors.
        /// </summary>
        public bool HasValidationError
        {
            get
            {
                ValidationErrorCollection superset;
                bool isPure;
                return GetValidationErrors(out superset, out isPure);
            }
        }

        /// <summary>
        ///     ValidationErrors returns the validation errors currently
        ///     arising from this binding group, or null if there are no errors.
        /// </summary>
        public ReadOnlyCollection<ValidationError> ValidationErrors
        {
            get
            {
                ValidationErrorCollection superset;
                bool isPure;
                if (GetValidationErrors(out superset, out isPure))
                {
                    if (isPure)
                        return new ReadOnlyCollection<ValidationError>(superset);

                    // 1% case - the validation errors attached to the mentor include
                    // some that don't belong to this binding group.   Filter them out.
                    List<ValidationError> errors = new List<ValidationError>();
                    foreach (ValidationError error in superset)
                    {
                        if (Belongs(error))
                        {
                            errors.Add(error);
                        }
                    }

                    return new ReadOnlyCollection<ValidationError>(errors);
                }

                return null;
            }
        }

        // get the validation errors associated with this BindingGroup.
        // If there are none, return false.    Otherwise return a superset of the
        // errors, and set isPure to true if the superset contains no errors from
        // any other source.  (This avoids allocations in the 90% case.)
        bool GetValidationErrors(out ValidationErrorCollection superset, out bool isPure)
        {
            superset = null;
            isPure = true;

            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor == null)
                return false;

            superset = Validation.GetErrorsInternal(mentor);
            if (superset == null || superset.Count == 0)
                return false;

            for (int i=superset.Count-1; i>=0; --i)
            {
                ValidationError validationError = superset[i];
                if (!Belongs(validationError))
                {
                    isPure = false;
                    break;
                }
            }

            return true;
        }

        bool Belongs(ValidationError error)
        {
            BindingExpressionBase bb;
            return (error.BindingInError == this ||
                    _proposedValueTable.HasValidationError(error) ||
                    (  (bb = error.BindingInError as BindingExpressionBase) != null &&
                        bb.BindingGroup == this)
                    );
        }

        DataBindEngine Engine { get { return _engine; } }

        #endregion Public properties

        #region Public Methods

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Begin an editing transaction.  For each source that supports it,
        /// the binding group asks the source to save its state, for possible
        /// restoration during <see cref="CancelEdit"/>.
        /// </summary>
        public void BeginEdit()
        {
            if (!IsEditing)
            {
                IList items = Items;
                for (int i=items.Count-1; i>=0; --i)
                {
                    IEditableObject ieo = items[i] as IEditableObject;
                    if (ieo != null)
                    {
                        ieo.BeginEdit();
                    }
                }

                IsEditing = true;
            }
        }

        /// <summary>
        /// End an editing transaction.  The binding group attempts to update all
        /// its sources with the proposed new values held in the target UI elements.
        /// All validation rules are run, at the times requested by the rules.
        /// </summary>
        /// <returns>
        /// True, if all validation rules succeed and no errors arise.
        /// False, otherwise.
        /// </returns>
        public bool CommitEdit()
        {
            bool result = UpdateAndValidate(ValidationStep.CommittedValue);
            IsEditing = IsEditing && !result;
            return result;
        }

        /// <summary>
        /// Cancel an editing transaction.  For each source that supports it,
        /// the binding group asks the source to restore itself to the state saved
        /// at the most recent <see cref="BeginEdit"/>.  Then the binding group
        /// updates all targets with values from their respective sources, discarding
        /// any "dirty" values held in the targets.
        /// </summary>
        public void CancelEdit()
        {
            // remove validation errors affiliated with the group (errors for
            // individual bindings will be removed during UpdateTarget)
            ClearValidationErrors();

            // restore values
            IList items = Items;
            for (int i=items.Count-1; i>=0; --i)
            {
                IEditableObject ieo = items[i] as IEditableObject;
                if (ieo != null)
                {
                    ieo.CancelEdit();
                }
            }

            // update targets
            for (int i=_bindingExpressions.Count - 1; i>=0; --i)
            {
                _bindingExpressions[i].UpdateTarget();
            }

            // also update dependent targets.  These are one-way bindings that
            // were initialized with a proposed value, and now need to re-fetch
            // the data from their sources
            _proposedValueTable.UpdateDependents();

            // remove proposed values
            _proposedValueTable.Clear();

            IsEditing = false;
        }

        /// <summary>
        /// Run the validation process up to the ConvertedProposedValue step.
        /// This runs all validation rules marked as RawProposedValue or
        /// ConvertedProposedValue, but does not update any sources with new values.
        /// </summary>
        /// <returns>
        /// True, if all validation rules succeed and no errors arise.
        /// False, otherwise.
        /// </returns>
        public bool ValidateWithoutUpdate()
        {
            return UpdateAndValidate(ValidationStep.ConvertedProposedValue);
        }

        /// <summary>
        /// Run the validation process up to the UpdatedValue step.
        /// This runs all validation rules marked as RawProposedValue or
        /// ConvertedProposedValue, updates the sources with new values, and
        /// runs rules marked as Updatedvalue.
        /// </summary>
        /// <returns>
        /// True, if all validation rules succeed and no errors arise.
        /// False, otherwise.
        /// </returns>
        public bool UpdateSources()
        {
            return UpdateAndValidate(ValidationStep.UpdatedValue);
        }

        /// <summary>
        /// Find the binding that uses the given item and property, and return
        /// the value appropriate to the current validation step.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// the binding group does not contain a binding corresponding to the
        /// given item and property.
        /// </exception>
        /// <exception cref="ValueUnavailableException">
        /// the value is not available.  This could be because an earlier validation
        /// rule deemed the value invalid, or because the value could not be produced
        /// for some reason, such as conversion failure.
        /// </exception>
        /// <Remarks>
        /// This method is intended to be called from a validation rule, during
        /// its Validate method.
        /// </Remarks>
        public object GetValue(object item, string propertyName)
        {
            object value;

            if (TryGetValueImpl(item, propertyName, out value))
            {
                return value;
            }

            if (value == Binding.DoNothing)
                throw new ValueUnavailableException(SR.Get(SRID.BindingGroup_NoEntry, item, propertyName));
            else
                throw new ValueUnavailableException(SR.Get(SRID.BindingGroup_ValueUnavailable, item, propertyName));
        }

        /// <summary>
        /// Find the binding that uses the given item and property, and return
        /// the value appropriate to the current validation step.
        /// </summary>
        /// <returns>
        /// The method normally returns true and sets 'value' to the requested value.
        /// If the value is not available, the method returns false and sets 'value'
        /// to DependencyProperty.UnsetValue.
        /// </returns>
        /// <Remarks>
        /// This method is intended to be called from a validation rule, during
        /// its Validate method.
        /// </Remarks>
        public bool TryGetValue(object item, string propertyName, out object value)
        {
            bool result = TryGetValueImpl(item, propertyName, out value);

            // TryGetValueImpl sets value to DoNothing to signal "no entry".
            // TryGetValue should treat this as just another unavailable value.
            if (value == Binding.DoNothing)
            {
                value = DependencyProperty.UnsetValue;
            }

            return result;
        }

        bool TryGetValueImpl(object item, string propertyName, out object value)
        {
            GetValueTableEntry entry = _getValueTable[item, propertyName];
            if (entry == null)
            {
                ProposedValueEntry proposedValueEntry = _proposedValueTable[item, propertyName];
                if (proposedValueEntry != null)
                {
                    // return the proposed value (raw or converted, depending on step)
                    switch (_validationStep)
                    {
                        case ValidationStep.RawProposedValue:
                            value = proposedValueEntry.RawValue;
                            return true;
                        case ValidationStep.ConvertedProposedValue:
                        case ValidationStep.UpdatedValue:
                        case ValidationStep.CommittedValue:
                            value = proposedValueEntry.ConvertedValue;
                            return (value != DependencyProperty.UnsetValue);
                    }
                }

                value = Binding.DoNothing;   // signal "no entry"
                return false;
            }

            switch (_validationStep)
            {
                case ValidationStep.RawProposedValue:
                case ValidationStep.ConvertedProposedValue:
                case ValidationStep.UpdatedValue:
                case ValidationStep.CommittedValue:
                    value = entry.Value;
                    break;

                // outside of validation process, use the raw value
                default:
                    value = entry.BindingExpressionBase.RootBindingExpression.GetRawProposedValue();
                    break;
            }

            if (value == Binding.DoNothing)
            {
                // a converter has indicated that no value should be written to the source object.
                // Therefore the source's value is the one to return to the validation rule.
                BindingExpression bindingExpression = (BindingExpression)entry.BindingExpressionBase;
                value = bindingExpression.SourceValue;
            }

            return (value != DependencyProperty.UnsetValue);
        }

        #endregion Public Methods

        #region Internal properties

        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        // Define the DO's inheritance context
        internal override DependencyObject InheritanceContext
        {
            get
            {
                DependencyObject inheritanceContext;
                if (!_inheritanceContext.TryGetTarget(out inheritanceContext))
                {
                    CheckDetach(inheritanceContext);
                }
                return inheritanceContext;
            }
        }

        // Receive a new inheritance context (this will be a FE/FCE)
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            if (property != null && property.PropertyType != typeof(BindingGroup) &&
                TraceData.IsEnabled)
            {
                string name = (property != null) ? property.Name : "(null)";
                TraceData.TraceAndNotify(TraceEventType.Warning,
                        TraceData.BindingGroupWrongProperty(name, context.GetType().FullName));
            }

            DependencyObject inheritanceContext;
            _inheritanceContext.TryGetTarget(out inheritanceContext);
            InheritanceContextHelper.AddInheritanceContext(context,
                                                              this,
                                                              ref _hasMultipleInheritanceContexts,
                                                              ref inheritanceContext );
            CheckDetach(inheritanceContext);
            _inheritanceContext = (inheritanceContext == null) ? NullInheritanceContext : new WeakReference<DependencyObject>(inheritanceContext);

            // if there's a validation rule that should run on data transfer, schedule it to run
            if (property == FrameworkElement.BindingGroupProperty &&
                !_hasMultipleInheritanceContexts &&
                (ValidatesOnDataTransfer || ValidatesOnNotifyDataError))
            {
                UIElement layoutElement = Helper.FindMentor(this) as UIElement;
                if (layoutElement != null)
                {
                    // do the validation at the end of the current layout pass, to allow
                    // bindings to join the group
                    layoutElement.LayoutUpdated += new EventHandler(OnLayoutUpdated);
                }
            }

            // sharing a BindingGroup among multiple hosts is bad - we wouldn't know which host
            // to send the errors to (just for starters).  But sharing an ItemBindingGroup is
            // expected - this is what happens normally in a hierarchical control like TreeView.
            // The following code tries to detect the bad case and warn the user that something
            // is amiss.
            if (_hasMultipleInheritanceContexts && property != ItemsControl.ItemBindingGroupProperty && TraceData.IsEnabled)
            {
                TraceData.TraceAndNotify(TraceEventType.Warning,
                        TraceData.BindingGroupMultipleInheritance);
            }
        }

        // Remove an inheritance context (this will be a FE/FCE)
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            DependencyObject inheritanceContext;
            _inheritanceContext.TryGetTarget(out inheritanceContext);
            InheritanceContextHelper.RemoveInheritanceContext(context,
                                                                  this,
                                                                  ref _hasMultipleInheritanceContexts,
                                                                  ref inheritanceContext);
            CheckDetach(inheritanceContext);
            _inheritanceContext = (inheritanceContext == null) ? NullInheritanceContext : new WeakReference<DependencyObject>(inheritanceContext);
        }

        // Says if the current instance has multiple InheritanceContexts
        internal override bool HasMultipleInheritanceContexts
        {
            get { return _hasMultipleInheritanceContexts; }
        }

        // check whether we've been detached from the owner
        void CheckDetach(DependencyObject newOwner)
        {
            if (newOwner != null || _inheritanceContext == NullInheritanceContext)
                return;

            // if so, remove references to this binding group from global tables
            Engine.CommitManager.RemoveBindingGroup(this);
        }

        bool IsEditing { get; set; }

        bool IsItemsValid
        {
            get { return _isItemsValid; }
            set
            {
                _isItemsValid = value;
                if (!value && (IsEditing || ValidatesOnNotifyDataError))
                {
                    // re-evaluate items, in case new items need BeginEdit or INDEI listener
                    EnsureItems();
                }
            }
        }

        #endregion Internal properties

        #region Internal methods

        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        // called when a leaf binding changes its source item
        internal void UpdateTable(BindingExpression bindingExpression)
        {
            bool newEntry = _getValueTable.Update(bindingExpression);

            if (newEntry)
            {
                // once we get an active binding, we no longer need a proposed
                // value for its source property.
                _proposedValueTable.Remove(bindingExpression);
            }

            IsItemsValid = false;
        }

        // add an entry to the value table for the given binding
        internal void AddToValueTable(BindingExpressionBase bindingExpressionBase)
        {
            _getValueTable.EnsureEntry(bindingExpressionBase);
        }

        // get the value for the given binding
        internal object GetValue(BindingExpressionBase bindingExpressionBase)
        {
            return _getValueTable.GetValue(bindingExpressionBase);
        }

        // set the value for the given binding
        internal void SetValue(BindingExpressionBase bindingExpressionBase, object value)
        {
            _getValueTable.SetValue(bindingExpressionBase, value);
        }

        // set values to "source" for all bindings under the given root
        internal void UseSourceValue(BindingExpressionBase bindingExpressionBase)
        {
            _getValueTable.UseSourceValue(bindingExpressionBase);
        }

        // get the proposed value for the given <item, propertyName>
        internal ProposedValueEntry GetProposedValueEntry(object item, string propertyName)
        {
            return _proposedValueTable[item, propertyName];
        }

        // remove a proposed value entry
        internal void RemoveProposedValueEntry(ProposedValueEntry entry)
        {
            _proposedValueTable.Remove(entry);
        }

        // add a dependent on a proposed value
        internal void AddBindingForProposedValue(BindingExpressionBase dependent, object item, string propertyName)
        {
            ProposedValueEntry entry = _proposedValueTable[item, propertyName];
            if (entry != null)
            {
                entry.AddDependent(dependent);
            }
        }

        // add a validation error to the mentor's list
        internal void AddValidationError(ValidationError validationError)
        {
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor == null)
                return;

            Validation.AddValidationError(validationError, mentor, NotifyOnValidationError);
        }

        // remove a validation error from the mentor's list
        internal void RemoveValidationError(ValidationError validationError)
        {
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor == null)
                return;

            Validation.RemoveValidationError(validationError, mentor, NotifyOnValidationError);
        }

        // remove all errors raised at the given step, in preparation for running
        // the rules at that step
        void ClearValidationErrors(ValidationStep validationStep)
        {
            ClearValidationErrorsImpl(validationStep, false);
        }

        // remove all errors affiliated with the BindingGroup
        void ClearValidationErrors()
        {
            ClearValidationErrorsImpl(ValidationStep.RawProposedValue, true);
        }

        // remove validation errors - the real work
        void ClearValidationErrorsImpl(ValidationStep validationStep, bool allSteps)
        {
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor == null)
                return;

            ValidationErrorCollection validationErrors = Validation.GetErrorsInternal(mentor);
            if (validationErrors == null)
                return;

            for (int i=validationErrors.Count-1; i>=0; --i)
            {
                ValidationError validationError = validationErrors[i];
                if (allSteps || validationError.RuleInError.ValidationStep == validationStep)
                {
                    if (validationError.BindingInError == this ||
                        _proposedValueTable.HasValidationError(validationError))
                    {
                        RemoveValidationError(validationError);
                    }
                }
            }
        }

        #endregion Internal methods

        #region Private methods

        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------

        // rebuild the Items collection, if necessary
        void EnsureItems()
        {
            if (IsItemsValid)
                return;

            // find the new set of items
            IList<WeakReference> newItems = new Collection<WeakReference>();

            // always include the DataContext item.  This is necessary for
            // scenarios when there is an item-level validation rule (e.g. DataError)
            // but no edits pending on the item and no two-way bindings.  This
            // arises in DataGrid.
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor != null)
            {
                object dataContextItem = mentor.GetValue(FrameworkElement.DataContextProperty);
                if (dataContextItem != null &&
                    dataContextItem != CollectionView.NewItemPlaceholder &&
                    dataContextItem != BindingExpressionBase.DisconnectedItem)
                {
                    WeakReference itemReference = _itemsRW.Count > 0 ? _itemsRW[0] : null;
                    // 90% case:  the first entry in _itemsRW already points to the item,
                    // so just re-use it.  Otherwise create a new reference.
                    if (itemReference == null ||
                        !ItemsControl.EqualsEx(dataContextItem, itemReference.Target))
                    {
                        itemReference = new WeakReference(dataContextItem);
                    }

                    newItems.Add(itemReference);
                }
            }

            // include items from active two-way bindings and from proposed values
            _getValueTable.AddUniqueItems(newItems);
            _proposedValueTable.AddUniqueItems(newItems);

            // modify the Items collection to match the new set
            // First, remove items that no longer appear
            INotifyDataErrorInfo indei;
            for (int i=_itemsRW.Count-1;  i >= 0;  --i)
            {
                int index = FindIndexOf(_itemsRW[i], newItems);
                if (index >= 0)
                {   // common item, don't add it later
                    newItems.RemoveAt(index);
                }
                else
                {   // item no longer appears, remove it now
                    if (ValidatesOnNotifyDataError)
                    {
                        indei = _itemsRW[i].Target as INotifyDataErrorInfo;
                        if (indei != null)
                            ErrorsChangedEventManager.RemoveHandler(indei, OnErrorsChanged);
                    }

                    _itemsRW.RemoveAt(i);
                }
            }

            // then add items that are really new
            for (int i=newItems.Count-1;  i>=0;  --i)
            {
                _itemsRW.Add(newItems[i]);

                // the new item may need BeginEdit
                if (IsEditing)
                {
                    IEditableObject ieo = newItems[i].Target as IEditableObject;
                    if (ieo != null)
                    {
                        ieo.BeginEdit();
                    }
                }

                // the item may implement INotifyDataErrorInfo
                if (ValidatesOnNotifyDataError)
                {
                    indei = newItems[i].Target as INotifyDataErrorInfo;
                    if (indei != null)
                    {
                        ErrorsChangedEventManager.AddHandler(indei, OnErrorsChanged);
                        UpdateNotifyDataErrors(indei, newItems[i]);
                    }
                }
            }

            IsItemsValid = true;
        }

        // true if there is a validation rule that runs on data transfer
        bool ValidatesOnDataTransfer
        {
            get
            {
                if (ValidationRules != null)
                {
                    for (int i=ValidationRules.Count-1; i>=0; --i)
                    {
                        if (ValidationRules[i].ValidatesOnTargetUpdated)
                            return true;
                    }
                }

                return false;
            }
        }

        // at the first LayoutUpdated event, set up the data-transfer validation process
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            DependencyObject mentor = Helper.FindMentor(this);

            // only do this once
            UIElement layoutElement = mentor as UIElement;
            if (layoutElement != null)
            {
                layoutElement.LayoutUpdated -= new EventHandler(OnLayoutUpdated);
            }

            // do the validation every time the DataContext changes
            FrameworkElement fe;
            FrameworkContentElement fce;
            Helper.DowncastToFEorFCE(mentor, out fe, out fce, false);
            if (fe != null)
            {
                fe.DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
            }
            else if (fce != null)
            {
                fce.DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
            }

            // do the initial validation
            ValidateOnDataTransfer();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == BindingExpressionBase.DisconnectedItem)
                return;

            IsItemsValid = false;
            ValidateOnDataTransfer();
        }

        // run the data-transfer validation rules
        void ValidateOnDataTransfer()
        {
            if (!ValidatesOnDataTransfer)
                return;
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor == null || ValidationRules.Count == 0)
                return;

            // get the current validation errors associated with the rules to be run.
            // Eventually we will remove these.
            Collection<ValidationError> oldErrors;
            if (!Validation.GetHasError(mentor))
            {
                // usually there aren't any errors at all
                oldErrors = null;
            }
            else
            {
                // pick out the errors that come from data-transfer rules associated with this BindingGroup
                oldErrors = new Collection<ValidationError>();
                ReadOnlyCollection<ValidationError> errors = Validation.GetErrors(mentor);
                for (int i=0, n=errors.Count; i<n; ++i)
                {
                    ValidationError error = errors[i];
                    if (error.RuleInError.ValidatesOnTargetUpdated && error.BindingInError == this)
                    {
                        oldErrors.Add(error);
                    }
                }
            }

            // run the data-transfer rules, accumulate new errors
            CultureInfo culture = GetCulture();
            for (int i=0, n=ValidationRules.Count; i<n; ++i)
            {
                ValidationRule rule = ValidationRules[i];
                if (rule.ValidatesOnTargetUpdated)
                {
                    try
                    {
                        ValidationResult validationResult = rule.Validate(DependencyProperty.UnsetValue, culture, this);
                        if (!validationResult.IsValid)
                        {
                            AddValidationError(new ValidationError(rule, this, validationResult.ErrorContent, null));
                        }
                    }
                    catch (ValueUnavailableException vue)
                    {
                        AddValidationError(new ValidationError(rule, this, vue.Message, vue));
                    }
                }
            }

            // remove the old errors (do this last to avoid passing through a transient
            // "no error" state)
            if (oldErrors != null)
            {
                for (int i=0, n=oldErrors.Count; i<n; ++i)
                {
                    RemoveValidationError(oldErrors[i]);
                }
            }
        }

        // run the validation process up to the indicated step
        bool UpdateAndValidate(ValidationStep validationStep)
        {
            // if the group is attached to a container tied to the
            // NewItemPlaceholder, don't do anything.  Bindings and validation
            // rules aren't usually relevant in this case.
            DependencyObject mentor = Helper.FindMentor(this);
            if (mentor != null &&
                mentor.GetValue(FrameworkElement.DataContextProperty) == CollectionView.NewItemPlaceholder)
            {
                return true;
            }

            PrepareProposedValuesForUpdate(mentor, (validationStep >= ValidationStep.UpdatedValue));

            bool result = true;

            for (_validationStep = ValidationStep.RawProposedValue;
                    _validationStep <= validationStep && result;
                    ++ _validationStep)
            {
                switch (_validationStep)
                {
                    case ValidationStep.RawProposedValue:
                        _getValueTable.ResetValues();
                        break;
                    case ValidationStep.ConvertedProposedValue:
                        result = ObtainConvertedProposedValues();
                        break;
                    case ValidationStep.UpdatedValue:
                        result = UpdateValues();
                        break;
                    case ValidationStep.CommittedValue:
                        result = CommitValues();
                        break;
                }

                if (!CheckValidationRules())
                {
                    result = false;
                }
            }

            ResetProposedValuesAfterUpdate(mentor, result && validationStep == ValidationStep.CommittedValue);

            _validationStep = (ValidationStep)(-1);
            _getValueTable.ResetValues();

            NotifyCommitManager();

            return result;
        }

        // update the item-level validation errors arising from INotifyDataErrorInfo items
        void UpdateNotifyDataErrors(INotifyDataErrorInfo indei, WeakReference itemWR)
        {
            // get the key for the item (its WeakReference from _itemsRW)
            if (itemWR == null)
            {
                int index = FindIndexOf(indei, _itemsRW);
                if (index < 0)
                    return;     // ignore events from items we no longer own
                itemWR = _itemsRW[index];
            }

            // fetch the errors from the item
            List<object> errors;
            try
            {
                errors = BindingExpression.GetDataErrors(indei, String.Empty);
            }
            catch (Exception ex)
            {
                // if there are non-critical exceptions, leave the previous errors intact
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;
                return;
            }

            UpdateNotifyDataErrorValidationErrors(itemWR, errors);
        }

        // replace the validation errors for the given item with a set that matches the errors list
        void UpdateNotifyDataErrorValidationErrors(WeakReference itemWR, List<object> errors)
        {
            // get the previous errors for this item
            List<ValidationError> itemErrors;
            if (!_notifyDataErrors.TryGetValue(itemWR, out itemErrors))
                itemErrors = null;

            List<object> toAdd;
            List<ValidationError> toRemove;

            BindingExpressionBase.GetValidationDelta(itemErrors, errors, out toAdd, out toRemove);

            // add the new errors, then remove the old ones - this avoid a transient
            // "no error" state
            if (toAdd != null && toAdd.Count > 0)
            {
                ValidationRule rule = NotifyDataErrorValidationRule.Instance;

                if (itemErrors == null)
                    itemErrors = new List<ValidationError>();

                foreach (object o in toAdd)
                {
                    ValidationError veAdd = new ValidationError(rule, this, o, null);
                    itemErrors.Add(veAdd);
                    AddValidationError(veAdd);
                }
            }

            if (toRemove != null && toRemove.Count > 0)
            {
                foreach (ValidationError veRemove in toRemove)
                {
                    itemErrors.Remove(veRemove);
                    RemoveValidationError(veRemove);
                }

                if (itemErrors.Count == 0)
                    itemErrors = null;
            }

            // update the cached list of errors for this item
            if (itemErrors == null)
            {
                _notifyDataErrors.Remove(itemWR);
            }
            else
            {
                _notifyDataErrors[itemWR] = itemErrors;
            }
        }

        // apply conversions to each binding in the group
        bool ObtainConvertedProposedValues()
        {
            bool result = true;
            for (int i=_bindingExpressions.Count-1; i>=0; --i)
            {
                result = _bindingExpressions[i].ObtainConvertedProposedValue(this) && result;
            }

            return result;
        }

        // update the source value of each binding in the group
        bool UpdateValues()
        {
            bool result = true;

            for (int i=_bindingExpressions.Count-1; i>=0; --i)
            {
                result = _bindingExpressions[i].UpdateSource(this) && result;
            }

            if (_proposedValueBindingExpressions != null)
            {
                for (int i=_proposedValueBindingExpressions.Length-1; i>=0; --i)
                {
                    BindingExpression bindExpr = _proposedValueBindingExpressions[i];
                    ProposedValueEntry proposedValueEntry = _proposedValueTable[bindExpr];
                    result = (bindExpr.UpdateSource(proposedValueEntry.ConvertedValue) != DependencyProperty.UnsetValue)
                                && result;
                }
            }

            return result;
        }

        // check the validation rules for the current step
        bool CheckValidationRules()
        {
            bool result = true;

            // clear old errors arising from this step
            ClearValidationErrors(_validationStep);

            // check rules attached to the bindings
            for (int i=_bindingExpressions.Count-1; i>=0; --i)
            {
                if (!_bindingExpressions[i].CheckValidationRules(this, _validationStep))
                {
                    result = false;
                }
            }

            // include the bindings for proposed values, for the last two steps
            if (_validationStep >= ValidationStep.UpdatedValue &&
                _proposedValueBindingExpressions != null)
            {
                for (int i=_proposedValueBindingExpressions.Length-1; i>=0; --i)
                {
                    if (!_proposedValueBindingExpressions[i].CheckValidationRules(this, _validationStep))
                    {
                        result = false;
                    }
                }
            }

            // check rules attached to the binding group
            CultureInfo culture = GetCulture();
            for (int i=0, n=_validationRules.Count; i<n; ++i)
            {
                ValidationRule rule = _validationRules[i];
                if (rule.ValidationStep == _validationStep)
                {
                    try
                    {
                        ValidationResult validationResult = rule.Validate(DependencyProperty.UnsetValue, culture, this);
                        if (!validationResult.IsValid)
                        {
                            AddValidationError(new ValidationError(rule, this, validationResult.ErrorContent, null));
                            result = false;
                        }
                    }
                    catch (ValueUnavailableException vue)
                    {
                        AddValidationError(new ValidationError(rule, this, vue.Message, vue));
                        result = false;
                    }
                }
            }

            return result;
        }

        // commit all the source values
        bool CommitValues()
        {
            bool result = true;
            IList items = Items;
            for (int i=items.Count-1; i>=0; --i)
            {
                IEditableObject ieo = items[i] as IEditableObject;
                if (ieo != null)
                {
                    // PreSharp uses message numbers that the C# compiler doesn't know about.
                    // Disable the C# complaints, per the PreSharp documentation.
                    #pragma warning disable 1634, 1691

                    // PreSharp complains about catching NullReference (and other) exceptions.
                    // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
                    #pragma warning disable 56500

                    try
                    {
                        ieo.EndEdit();
                    }
                    catch (Exception ex)
                    {
                        if (CriticalExceptions.IsCriticalApplicationException(ex))
                            throw;

                        ValidationError error = new ValidationError(ExceptionValidationRule.Instance, this, ex.Message, ex);
                        AddValidationError(error);
                        result = false;
                    }

                    #pragma warning restore 56500
                    #pragma warning restore 1634, 1691
                }
            }
            return result;
        }

        // find the index of an item in a list, where both the item and
        // the list use WeakReferences
        static int FindIndexOf(WeakReference wr, IList<WeakReference> list)
        {
            object item = wr.Target;
            if (item == null)
                return -1;
            return FindIndexOf(item, list);
        }

        static int FindIndexOf(object item, IList<WeakReference> list)
        {
            for (int i=0, n=list.Count; i<n; ++i)
            {
                if (ItemsControl.EqualsEx(item, list[i].Target))
                {
                    return i;
                }
            }

            return -1;
        }

        // get the culture of the binding group's mentor
        CultureInfo GetCulture()
        {
            if (_culture == null)
            {
                DependencyObject mentor = Helper.FindMentor(this);
                if (mentor != null)
                {
                    _culture = ((System.Windows.Markup.XmlLanguage) mentor.GetValue(FrameworkElement.LanguageProperty)).GetSpecificCulture();
                }
            }

            return _culture;
        }

        // handle changes to the collection of binding expressions
        void OnBindingsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            BindingExpressionBase bindingExpr;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    bindingExpr = e.NewItems[0] as BindingExpressionBase;
                    bindingExpr.JoinBindingGroup(this, /*explicit*/ true);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    bindingExpr = e.OldItems[0] as BindingExpressionBase;
                    RemoveBindingExpression(bindingExpr);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;  // nothing to do - order within the collection doesn't matter
                case NotifyCollectionChangedAction.Replace:
                    bindingExpr = e.OldItems[0] as BindingExpressionBase;
                    RemoveBindingExpression(bindingExpr);
                    bindingExpr = e.NewItems[0] as BindingExpressionBase;
                    bindingExpr.JoinBindingGroup(this, /*explicit*/ true);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    // the only way this collection can raise Reset is due to Clear()
                    Debug.Assert(_bindingExpressions.Count == 0, "Unexpected Reset event");
                    RemoveAllBindingExpressions();
                    break;  // nothing to do - order within the collection doesn't matter
                default:
                    Debug.Assert(false, "Unexpected change event");
                    break;
            }

            IsItemsValid = false;      // all changes potentially affect Items
        }

        // explicitly remove a binding expression from the group
        void RemoveBindingExpression(BindingExpressionBase exprBase)
        {
            // we actually remove all expressions belonging to the same root
            BindingExpressionBase root = exprBase.RootBindingExpression;

            // preserve proposed values
            if (SharesProposedValues && root.NeedsValidation)
            {
                Collection<BindingExpressionBase.ProposedValue> proposedValues;
                root.ValidateAndConvertProposedValue(out proposedValues);
                PreserveProposedValues(proposedValues);
            }

            // remove the binding expressions from the value table
            List<BindingExpressionBase> list = _getValueTable.RemoveRootBinding(root);

            // tell each expression it is leaving the group
            foreach (BindingExpressionBase expr in list)
            {
                expr.OnBindingGroupChanged(/*joining*/ false);

                // also remove the expression from our collection.  Normally this is
                // a no-op, as we only get here after the expression has been removed,
                // and implicit membership only adds root expressions to the collection.
                // But an app (through confusion or malice) could explicitly add two
                // or more expressions with the same root.  We handle that case here.
                _bindingExpressions.Remove(expr);
            }

            // cut the root's link to the group
            root.LeaveBindingGroup();
        }

        // remove all binding expressions from the group
        void RemoveAllBindingExpressions()
        {
            // we can't use the BindingExpressions collection - it has already
            // been cleared.  Instead, find the expressions that need work by
            // looking in the GetValue table.
            GetValueTableEntry entry;
            while ((entry = _getValueTable.GetFirstEntry()) != null)
            {
                RemoveBindingExpression(entry.BindingExpressionBase);
            }
        }

        // preserve proposed values
        void PreserveProposedValues(Collection<BindingExpressionBase.ProposedValue> proposedValues)
        {
            if (proposedValues == null)
                return;

            for (int i=0, n=proposedValues.Count; i<n; ++i)
            {
                _proposedValueTable.Add(proposedValues[i]);
            }
        }

        // before beginning a validate/update pass, enable the proposed values
        // to participate
        void PrepareProposedValuesForUpdate(DependencyObject mentor, bool isUpdating)
        {
            int count = _proposedValueTable.Count;
            if (count == 0)
                return;

            if (isUpdating)
            {
                // create a shadow binding for each proposed value
                _proposedValueBindingExpressions = new BindingExpression[count];
                for (int i=0; i<count; ++i)
                {
                    ProposedValueEntry entry = _proposedValueTable[i];
                    Binding originalBinding = entry.Binding;

                    Binding binding = new Binding();
                    binding.Source = entry.Item;
                    binding.Mode = BindingMode.TwoWay;
                    binding.Path = new PropertyPath(entry.PropertyName, originalBinding.Path.PathParameters);

                    binding.ValidatesOnDataErrors = originalBinding.ValidatesOnDataErrors;
                    binding.ValidatesOnNotifyDataErrors = originalBinding.ValidatesOnNotifyDataErrors;
                    binding.ValidatesOnExceptions = originalBinding.ValidatesOnExceptions;

                    Collection<ValidationRule> rules = originalBinding.ValidationRulesInternal;
                    if (rules != null)
                    {
                        for (int j=0, n=rules.Count; j<n; ++j)
                        {
                            binding.ValidationRules.Add(rules[j]);
                        }
                    }

                    BindingExpression bindExpr = (BindingExpression)BindingExpression.CreateUntargetedBindingExpression(mentor, binding);
                    bindExpr.Attach(mentor);
                    bindExpr.NeedsUpdate = true;

                    _proposedValueBindingExpressions[i] = bindExpr;
                }
            }
        }

        // after a validate/update pass, reset the proposed values and related state
        void ResetProposedValuesAfterUpdate(DependencyObject mentor, bool isFullUpdate)
        {
            if (_proposedValueBindingExpressions != null)
            {
                for (int i=0, n=_proposedValueBindingExpressions.Length; i<n; ++i)
                {
                    BindingExpression bindExpr = _proposedValueBindingExpressions[i];
                    ValidationError validationError = bindExpr.ValidationError;

                    bindExpr.Detach();

                    // reattach the validation error (Detach removes it)
                    if (validationError != null)
                    {
                        // reassign error's owner to this BindingGroup
                        ValidationError newError = new ValidationError(
                                        validationError.RuleInError,
                                        this,   /* bindingInError */
                                        validationError.ErrorContent,
                                        validationError.Exception);
                        AddValidationError(newError);
                    }
                }

                _proposedValueBindingExpressions = null;
            }

            if (isFullUpdate)
            {
                // one-way bindings initialized from proposed values should now
                // re-fetch values from the source.  This handles cases where
                // the source normalizes the proposed value. 
                _proposedValueTable.UpdateDependents();
                _proposedValueTable.Clear();
            }
        }

        void NotifyCommitManager()
        {
            if (Engine.IsShutDown)
                return;

            bool shouldStore = Owner != null && (IsDirty || HasValidationError);

            if (shouldStore)
            {
                Engine.CommitManager.AddBindingGroup(this);
            }
            else
            {
                Engine.CommitManager.RemoveBindingGroup(this);
            }
        }


        #endregion Private methods

        #region Event handlers

        void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            // if notification was on the right thread, just do the work (normal case)
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                UpdateNotifyDataErrors((INotifyDataErrorInfo)sender, null);
            }
            else
            {
                // otherwise invoke an operation to do the work on the right context
                Engine.Marshal(
                    (arg) => {  UpdateNotifyDataErrors((INotifyDataErrorInfo)arg, null);
                                 return null; }, sender);
            }
        }

        #endregion Event handlers

        #region Private data

        //------------------------------------------------------
        //
        //  Private data
        //
        //------------------------------------------------------

        ValidationRuleCollection    _validationRules;
        string                      _name;
        bool                        _notifyOnValidationError;
        bool                        _sharesProposedValues;
        bool                        _validatesOnNotifyDataError = true;

        DataBindEngine              _engine;
        BindingExpressionCollection _bindingExpressions;
        bool                        _isItemsValid;
        ValidationStep              _validationStep = (ValidationStep)(-1);
        GetValueTable               _getValueTable = new GetValueTable();
        ProposedValueTable          _proposedValueTable = new ProposedValueTable();
        BindingExpression[]         _proposedValueBindingExpressions;
        Collection<WeakReference>   _itemsRW;
        WeakReadOnlyCollection<object> _items;
        CultureInfo                 _culture;
        Dictionary<WeakReference, List<ValidationError>> _notifyDataErrors = new Dictionary<WeakReference, List<ValidationError>>();

        internal static readonly object DeferredTargetValue = new NamedObject("DeferredTargetValue");
        internal static readonly object DeferredSourceValue = new NamedObject("DeferredSourceValue");

        // Fields to implement DO's inheritance context
        static WeakReference<DependencyObject> NullInheritanceContext = new WeakReference<DependencyObject>(null);
        WeakReference<DependencyObject> _inheritanceContext = NullInheritanceContext;
        bool                            _hasMultipleInheritanceContexts;

        #endregion Private data

        #region Private types

        //------------------------------------------------------
        //
        //  Private types
        //
        //------------------------------------------------------

        // to support GetValue, we maintain an associative array of all the bindings,
        // items, and property names that affect a binding group.
        private class GetValueTable
        {
            // lookup by item and propertyName
            public GetValueTableEntry this[object item, string propertyName]
            {
                get
                {
                    for (int i=_table.Count-1; i >= 0; --i)
                    {
                        GetValueTableEntry entry = _table[i];
                        if (propertyName == entry.PropertyName &&
                            ItemsControl.EqualsEx(item, entry.Item))
                        {
                            return entry;
                        }
                    }

                    return null;
                }
            }

            // lookup by binding
            public GetValueTableEntry this[BindingExpressionBase bindingExpressionBase]
            {
                get
                {
                    for (int i=_table.Count-1; i >= 0; --i)
                    {
                        GetValueTableEntry entry = _table[i];
                        if (bindingExpressionBase == entry.BindingExpressionBase)
                        {
                            return entry;
                        }
                    }

                    return null;
                }
            }

            // ensure an entry for the given binding
            public void EnsureEntry(BindingExpressionBase bindingExpressionBase)
            {
                GetValueTableEntry entry = this[bindingExpressionBase];
                if (entry == null)
                {
                    _table.Add(new GetValueTableEntry(bindingExpressionBase));
                }
            }

            // update (or add) the entry for the given leaf binding
            public bool Update(BindingExpression bindingExpression)
            {
                GetValueTableEntry entry = this[bindingExpression];
                bool newEntry = (entry == null);

                if (newEntry)
                {
                    _table.Add(new GetValueTableEntry(bindingExpression));
                }
                else
                {
                    entry.Update(bindingExpression);
                }

                return newEntry;
            }

            // remove all the entries for the given root binding.  Return the list of expressions.
            public List<BindingExpressionBase> RemoveRootBinding(BindingExpressionBase rootBindingExpression)
            {
                List<BindingExpressionBase> result = new List<BindingExpressionBase>();

                for (int i=_table.Count-1; i >= 0; --i)
                {
                    BindingExpressionBase expr = _table[i].BindingExpressionBase;
                    if (expr.RootBindingExpression == rootBindingExpression)
                    {
                        result.Add(expr);
                        _table.RemoveAt(i);
                    }
                }

                return result;
            }

            // append to a list of the unique items (wrapped in WeakReferences)
            public void AddUniqueItems(IList<WeakReference> list)
            {
                for (int i=_table.Count-1; i >= 0; --i)
                {
                    // don't include bindings that couldn't resolve
                    if (_table[i].BindingExpressionBase.StatusInternal == BindingStatusInternal.PathError)
                        continue;

                    WeakReference itemWR = _table[i].ItemReference;
                    if (itemWR != null && BindingGroup.FindIndexOf(itemWR, list) < 0)
                    {
                        list.Add(itemWR);
                    }
                }
            }

            // get the value for a binding expression
            public object GetValue(BindingExpressionBase bindingExpressionBase)
            {
                GetValueTableEntry entry = this[bindingExpressionBase];
                return (entry != null) ? entry.Value : DependencyProperty.UnsetValue;
            }

            // set the value for a binding expression
            public void SetValue(BindingExpressionBase bindingExpressionBase, object value)
            {
                GetValueTableEntry entry = this[bindingExpressionBase];
                if (entry != null)
                {
                    entry.Value = value;
                }
            }

            // reset values to "raw"
            public void ResetValues()
            {
                for (int i=_table.Count-1; i>=0; --i)
                {
                    _table[i].Value = BindingGroup.DeferredTargetValue;
                }
            }

            // set values to "source" for all bindings under the given root
            public void UseSourceValue(BindingExpressionBase rootBindingExpression)
            {
                for (int i=_table.Count-1; i>=0; --i)
                {
                    if (_table[i].BindingExpressionBase.RootBindingExpression == rootBindingExpression)
                    {
                        _table[i].Value = BindingGroup.DeferredSourceValue;
                    }
                }
            }

            // return the first entry in the table (or null)
            public GetValueTableEntry GetFirstEntry()
            {
                return (_table.Count > 0) ? _table[0] : null;
            }

            Collection<GetValueTableEntry> _table = new Collection<GetValueTableEntry>();
        }

        // a single entry in the GetValueTable
        private class GetValueTableEntry
        {
            public GetValueTableEntry(BindingExpressionBase bindingExpressionBase)
            {
                _bindingExpressionBase = bindingExpressionBase;
            }

            public void Update(BindingExpression bindingExpression)
            {
                object item = bindingExpression.SourceItem;
                if (item == null)
                {
                    _itemWR = null;
                }
                else if (_itemWR == null)
                {
                    _itemWR = new WeakReference(item);  // WR to avoid leaks
                }
                else
                {
                    _itemWR.Target = bindingExpression.SourceItem;
                }

                _propertyName = bindingExpression.SourcePropertyName;
            }

            public object Item
            {
                get { return _itemWR.Target; }
            }

            public WeakReference ItemReference
            {
                get { return _itemWR; }
            }

            public string PropertyName
            {
                get { return _propertyName; }
            }

            public BindingExpressionBase BindingExpressionBase
            {
                get { return _bindingExpressionBase; }
            }

            public object Value
            {
                get
                {
                    if (_value == BindingGroup.DeferredTargetValue)
                    {
                        _value = _bindingExpressionBase.RootBindingExpression.GetRawProposedValue();
                    }
                    else if (_value == BindingGroup.DeferredSourceValue)
                    {
                        BindingExpression bindingExpression = _bindingExpressionBase as BindingExpression;
                        Debug.Assert(bindingExpression != null, "do not ask for source value from a [Multi,Priority]Binding");
                        _value = (bindingExpression != null) ? bindingExpression.SourceValue : DependencyProperty.UnsetValue;
                    }

                    return _value;
                }
                set { _value = value; }
            }

            BindingExpressionBase   _bindingExpressionBase;
            WeakReference   _itemWR;
            string          _propertyName;
            object          _value = BindingGroup.DeferredTargetValue;
        }


        // to support sharing of proposed values, we maintain an associative array
        // of <item, propertyName, rawProposedValue, convertedProposedValue, validation rules>
        private class ProposedValueTable
        {
            // add an entry, based on the ProposedValue structure returned by validation
            public void Add(BindingExpressionBase.ProposedValue proposedValue)
            {
                BindingExpression bindExpr = proposedValue.BindingExpression;
                object item = bindExpr.SourceItem;
                string propertyName = bindExpr.SourcePropertyName;
                object rawValue = proposedValue.RawValue;
                object convertedValue = proposedValue.ConvertedValue;

                // at most one proposed value per <item, propertyName>
                Remove(item, propertyName);

                // add the new entry
                _table.Add(new ProposedValueEntry(item, propertyName, rawValue, convertedValue, bindExpr));
            }

            // remove an entry
            public void Remove(object item, string propertyName)
            {
                int index = IndexOf(item, propertyName);
                if (index >= 0)
                {
                    _table.RemoveAt(index);
                }
            }

            // remove an entry corresponding to a binding
            public void Remove(BindingExpression bindExpr)
            {
                if (_table.Count > 0)
                {
                    Remove(bindExpr.SourceItem, bindExpr.SourcePropertyName);
                }
            }

            // remove an entry
            public void Remove(ProposedValueEntry entry)
            {
                _table.Remove(entry);
            }

            // remove all entries
            public void Clear()
            {
                _table.Clear();
            }

            public int Count { get { return _table.Count; } }

            // lookup by item and propertyName
            public ProposedValueEntry this[object item, string propertyName]
            {
                get
                {
                    int index = IndexOf(item, propertyName);
                    return (index < 0) ? null : _table[index];
                }
            }

            // lookup by index
            public ProposedValueEntry this[int index]
            {
                get { return _table[index]; }
            }

            // lookup by BindingExpression
            public ProposedValueEntry this[BindingExpression bindExpr]
            {
                get { return this[bindExpr.SourceItem, bindExpr.SourcePropertyName]; }
            }

            // append to a list of unique items
            public void AddUniqueItems(IList<WeakReference> list)
            {
                for (int i=_table.Count-1; i >= 0; --i)
                {
                    WeakReference itemWR = _table[i].ItemReference;
                    if (itemWR != null && BindingGroup.FindIndexOf(itemWR, list) < 0)
                    {
                        list.Add(itemWR);
                    }
                }
            }

            // call UpdateTarget on all dependents
            public void UpdateDependents()
            {
                for (int i=_table.Count-1; i>=0; --i)
                {
                    Collection<BindingExpressionBase> dependents = _table[i].Dependents;
                    if (dependents != null)
                    {
                        for (int j=dependents.Count-1; j>=0; --j)
                        {
                            BindingExpressionBase beb = dependents[j];
                            if (!beb.IsDetached)
                            {
                                dependents[j].UpdateTarget();
                            }
                        }
                    }
                }
            }

            public bool HasValidationError(ValidationError validationError)
            {
                for (int i=_table.Count-1; i>=0; --i)
                {
                    if (validationError == _table[i].ValidationError)
                        return true;
                }
                return false;
            }

            // return the index of the entry with given key (or -1)
            private int IndexOf(object item, string propertyName)
            {
                for (int i=_table.Count-1; i >= 0; --i)
                {
                    ProposedValueEntry entry = _table[i];
                    if (propertyName == entry.PropertyName &&
                        ItemsControl.EqualsEx(item, entry.Item))
                    {
                        return i;
                    }
                }

                return -1;
            }

            Collection<ProposedValueEntry> _table = new Collection<ProposedValueEntry>();
        }

        // a single entry in the ProposedValueTable
        internal class ProposedValueEntry
        {
            public ProposedValueEntry(object item,
                                    string propertyName,
                                    object rawValue,
                                    object convertedValue,
                                    BindingExpression bindExpr)
            {
                _itemReference = new WeakReference(item);
                _propertyName = propertyName;
                _rawValue = rawValue;
                _convertedValue = convertedValue;
                _error = bindExpr.ValidationError;
                _binding = bindExpr.ParentBinding;
            }

            public object Item                      { get { return _itemReference.Target; } }
            public string PropertyName              { get { return _propertyName; } }
            public object RawValue                  { get { return _rawValue; } }
            public object ConvertedValue            { get { return _convertedValue; } }
            public ValidationError ValidationError  { get { return _error; } }
            public Binding Binding                  { get { return _binding; } }
            public WeakReference ItemReference      { get { return _itemReference; } }
            public Collection<BindingExpressionBase> Dependents { get { return _dependents; } }

            public void AddDependent(BindingExpressionBase dependent)
            {
                if (_dependents == null)
                {
                    _dependents = new Collection<BindingExpressionBase>();
                }
                _dependents.Add(dependent);
            }

            WeakReference _itemReference;
            string _propertyName;
            object _rawValue;
            object _convertedValue;
            ValidationError _error;
            Binding _binding;
            Collection<BindingExpressionBase> _dependents;
        }

        // add some error-checking to ObservableCollection
        class BindingExpressionCollection : ObservableCollection<BindingExpressionBase>
        {
            /// <summary>
            /// Called by base class Collection&lt;T&gt; when an item is added to list;
            /// raises a CollectionChanged event to any listeners.
            /// </summary>
            protected override void InsertItem(int index, BindingExpressionBase item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException("item");
                }

                base.InsertItem(index, item);
            }

            /// <summary>
            /// Called by base class Collection&lt;T&gt; when an item is set in list;
            /// raises a CollectionChanged event to any listeners.
            /// </summary>
            protected override void SetItem(int index, BindingExpressionBase item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException("item");
                }

                base.SetItem(index, item);
            }
        }

        #endregion Private types
    }
}
