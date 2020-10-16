// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines BindingExpression object, the run-time instance of data binding.
//
// See spec at Data Binding.mht
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Markup;
using MS.Utility;
using MS.Internal;
using MS.Internal.Controls; // Validation
using MS.Internal.Data;
using MS.Internal.KnownBoxes;
using MS.Internal.Utility;  // TraceLog

namespace System.Windows.Data
{
    /// <summary>
    /// called whenever any exception is encountered when trying to update
    /// the value to the source. The application author can provide its own
    /// handler for handling exceptions here. If the delegate returns
    ///     null - dont throw an error or provide a ValidationError.
    ///     Exception - returns the exception itself, we will fire the exception using Async exception model.
    ///     ValidationError - it will set itself as the BindingInError and add it to the elements Validation errors.
    /// </summary>
    public delegate object UpdateSourceExceptionFilterCallback(object bindExpression, Exception exception);

    /// <summary>
    ///  Describes a single run-time instance of data binding, binding a target
    ///  (element, DependencyProperty) to a source (object, property, XML node)
    /// </summary>
    public sealed class BindingExpression : BindingExpressionBase, IDataBindEngineClient, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Enums
        //
        //------------------------------------------------------

        internal enum SourceType { Unknown, CLR, XML }
        private enum AttachAttempt { First, Again, Last }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        private BindingExpression(Binding binding, BindingExpressionBase owner)
            : base(binding, owner)
        {
            UseDefaultValueConverter = (ParentBinding.Converter == null);

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.ShowPath))
            {
                PropertyPath pp = binding.Path;
                string path = (pp != null) ? pp.Path : String.Empty;

                if (String.IsNullOrEmpty(binding.XPath))
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.BindingPath(
                                            TraceData.Identify(path)),
                                        this);
                }
                else
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.BindingXPathAndPath(
                                            TraceData.Identify(binding.XPath),
                                            TraceData.Identify(path)),
                                        this);
                }
            }
        }

        //------------------------------------------------------
        //
        //  Interfaces
        //
        //------------------------------------------------------

        void IDataBindEngineClient.TransferValue()
        {
            TransferValue();
        }

        void IDataBindEngineClient.UpdateValue()
        {
            UpdateValue();
        }

        bool IDataBindEngineClient.AttachToContext(bool lastChance)
        {
            AttachToContext(lastChance ? AttachAttempt.Last : AttachAttempt.Again);
            return (StatusInternal != BindingStatusInternal.Unattached);
        }

        void IDataBindEngineClient.VerifySourceReference(bool lastChance)
        {
            DependencyObject target = TargetElement;
            if (target == null)
                return;     // binding was detached since scheduling this task

            ObjectRef sourceRef = ParentBinding.SourceReference;
            DependencyObject mentor = !UsingMentor ? target : Helper.FindMentor(target);
            ObjectRefArgs args = new ObjectRefArgs() {
                                    ResolveNamesInTemplate = ResolveNamesInTemplate,
                                    };
            object source = sourceRef.GetDataObject(mentor, args);

            if (source != DataItem)
            {
                // the source reference resolves differently from before, so
                // re-attach to the tree context
                AttachToContext(lastChance ? AttachAttempt.Last : AttachAttempt.Again);
            }
        }

        void IDataBindEngineClient.OnTargetUpdated()
        {
            OnTargetUpdated();
        }

        DependencyObject IDataBindEngineClient.TargetElement
        {
            get { return !UsingMentor ? TargetElement : Helper.FindMentor(TargetElement); }
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> Binding from which this expression was created </summary>
        public Binding ParentBinding { get { return (Binding)ParentBindingBase; } }

        /// <summary>The data item actually used by this BindingExpression</summary>
        public object DataItem { get { return GetReference(_dataItem); } }

        /// <summary> The object that gets changed during UpdateSource </summary>
        public object ResolvedSource { get { return SourceItem; } }

        /// <summary> The name of the property that gets changed during UpdateSource </summary>
        public string ResolvedSourcePropertyName { get { return SourcePropertyName; } }

        /// <summary>The data source actually used by this BindingExpression</summary>
        internal object DataSource
        {
            get
            {
                DependencyObject target = TargetElement;
                if (target == null)
                    return null;

                // if we're using DataContext, find the source for the DataContext
                if (_ctxElement != null)
                    return GetDataSourceForDataContext(ContextElement);

                // otherwise use the explicit source
                ObjectRef or = ParentBinding.SourceReference;
                return or.GetObject(target, new ObjectRefArgs());
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary> Send the current value back to the source </summary>
        /// <remarks> Does nothing when binding's Mode is not TwoWay or OneWayToSource </remarks>
        public override void UpdateSource()
        {
            if (IsDetached)
                throw new InvalidOperationException(SR.Get(SRID.BindingExpressionIsDetached));

            NeedsUpdate = true;     // force update
            Update();               // update synchronously
        }

        /// <summary> Force a data transfer from source to target </summary>
        public override void UpdateTarget()
        {
            if (IsDetached)
                throw new InvalidOperationException(SR.Get(SRID.BindingExpressionIsDetached));

            if (Worker != null)
            {
                Worker.RefreshValue();  // calls TransferValue
            }
        }

#region Expression overrides

        /// <summary>
        ///     Notification that a Dependent that this Expression established has
        ///     been invalidated as a result of a Source invalidation
        /// </summary>
        /// <param name="d">DependencyObject that was invalidated</param>
        /// <param name="args">Changed event args for the property that was invalidated</param>
        internal override void OnPropertyInvalidation(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            DependencyProperty dp = args.Property;
            if (dp == null)
                throw new InvalidOperationException(SR.Get(SRID.ArgumentPropertyMustNotBeNull, "Property", "args"));

            // ignore irrelevant notifications.  This test must happen before any marshalling.
            bool relevant = !IgnoreSourcePropertyChange;

            if (dp == FrameworkElement.DataContextProperty && d == ContextElement)
            {
                relevant = true;    // changes from context element are always relevant
            }
            else if (dp == CollectionViewSource.ViewProperty && d == CollectionViewSource)
            {
                relevant = true;    // changes from the CollectionViewSource are always relevant
            }
            else if (dp == FrameworkElement.LanguageProperty && UsesLanguage && d == TargetElement)
            {
                relevant = true;    // changes from target's Language are always relevant
            }
            else if (relevant)
            {
                relevant = (Worker != null) && (Worker.UsesDependencyProperty(d, dp));
            }

            if (!relevant)
                return;

            base.OnPropertyInvalidation(d, args);
        }

#endregion  Expression overrides

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Invalidate the given child expression.
        /// </summary>
        internal override void InvalidateChild(BindingExpressionBase bindingExpression)
        {
            // BindingExpression does not support child bindings
        }

        /// <summary>
        /// Change the dependency sources for the given child expression.
        /// </summary>
        internal override void ChangeSourcesForChild(BindingExpressionBase bindingExpression, WeakDependencySource[] newSources)
        {
            // BindingExpression does not support child bindings
        }

        /// <summary>
        /// Replace the given child expression with a new one.
        /// </summary>
        internal override void ReplaceChild(BindingExpressionBase bindingExpression)
        {
            // BindingExpression does not support child bindings
        }

        // register the leaf bindings with the binding group
        internal override void UpdateBindingGroup(BindingGroup bg)
        {
            bg.UpdateTable(this);
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        // The ContextElement is the DependencyObject (if any) whose DataContext
        // is used as the starting point for the evaluation of the BindingExpression Path.
        //      We should not store a strong reference to the context element,
        // for the same reasons as mentioned above for TargetElement.  Instead,
        // we store a weak reference.  Callers should be prepared for the case
        // ContextElement==null, which is different from _ctxElement==null.  The
        // former means the BindingExpression uses a context element, but that element has
        // been GC'd;  the latter means that the BindingExpression does not use a context
        // element.
        internal DependencyObject ContextElement
        {
            get
            {
                if (_ctxElement != null)
                    return _ctxElement.Target as DependencyObject;
                else
                    return null;
            }
        }

        // The CollectionViewSource is the source object, as a CollectionViewSource.
        internal CollectionViewSource CollectionViewSource
        {
            get
            {
                WeakReference wr = (WeakReference)GetValue(Feature.CollectionViewSource, null);
                return (wr == null) ? null : (CollectionViewSource)wr.Target;
            }
            set
            {
                if (value == null)
                    ClearValue(Feature.CollectionViewSource);
                else
                    SetValue(Feature.CollectionViewSource, new WeakReference(value));
            }
        }

        /// <summary> True if this binding expression should ignore changes from the source </summary>
        internal bool IgnoreSourcePropertyChange
        {
            get
            {
                if (IsTransferPending || IsInUpdate)
                    return true;

                return false;
            }
        }

        internal PropertyPath Path
        {
            get { return ParentBinding.Path; }
        }

        internal IValueConverter Converter
        {
            get { return (IValueConverter)GetValue(Feature.Converter, null); }
            set { SetValue(Feature.Converter, value, null); }
        }

        // MultiBinding looks at this to find out what type its MultiValueConverter should
        // convert back to, when this BindingExpression is not using a user-specified converter.
        internal Type ConverterSourceType
        {
            get { return _sourceType; }
        }

        // the item whose property changes when we UpdateSource
        internal object SourceItem
        {
            get { return (Worker != null) ? Worker.SourceItem : null; }
        }

        // the name of the property that changes when we UpdateSource
        internal string SourcePropertyName
        {
            get { return (Worker != null) ? Worker.SourcePropertyName : null; }
        }

        // the value of the source property
        internal object SourceValue
        {
            get { return (Worker != null) ? Worker.RawValue() : DependencyProperty.UnsetValue; }
        }

        internal override bool IsParentBindingUpdateTriggerDefault
        {
            get { return (ParentBinding.UpdateSourceTrigger == UpdateSourceTrigger.Default); }
        }

        internal override bool IsDisconnected
        {
            get { return GetReference(_dataItem) == DisconnectedItem; }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // Create a new BindingExpression from the given Bind description
        internal static BindingExpression CreateBindingExpression(DependencyObject d,
                                                DependencyProperty dp,
                                                Binding binding,
                                                BindingExpressionBase parent)
        {
            FrameworkPropertyMetadata fwMetaData = dp.GetMetadata(d.DependencyObjectType) as FrameworkPropertyMetadata;

            if ((fwMetaData != null && !fwMetaData.IsDataBindingAllowed) || dp.ReadOnly)
                throw new ArgumentException(SR.Get(SRID.PropertyNotBindable, dp.Name), "dp");

            // create the BindingExpression
            BindingExpression bindExpr = new BindingExpression(binding, parent);

            bindExpr.ResolvePropertyDefaultSettings(binding.Mode, binding.UpdateSourceTrigger, fwMetaData);

            // Two-way Binding with an empty path makes no sense
            if (bindExpr.IsReflective && binding.XPath == null &&
                    (binding.Path == null || String.IsNullOrEmpty(binding.Path.Path)))
                throw new InvalidOperationException(SR.Get(SRID.TwoWayBindingNeedsPath));

            return bindExpr;
        }


        // Note: For Nullable types, DefaultValueConverter is created for the inner type of the Nullable.
        //       Nullable "Drill-down" service is not provided for user provided Converters.
        internal void SetupDefaultValueConverter(Type type)
        {
            if (!UseDefaultValueConverter)
                return;

            if (IsInMultiBindingExpression)
            {
                Converter = null;
                _sourceType = type;
            }
            else if (type != null && type != _sourceType)
            {
                _sourceType = type;

                IValueConverter converter = Engine.GetDefaultValueConverter(type,
                                    TargetProperty.PropertyType, IsReflective);

                // null converter means failure to create one
                if (converter == null && TraceData.IsEnabled)
                {
                     TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Error,
                                    TraceData.CannotCreateDefaultValueConverter(
                                        type,
                                        TargetProperty.PropertyType,
                                        (IsReflective ? "two-way" : "one-way")),
                                    this );
                }

                if (converter == DefaultValueConverter.ValueConverterNotNeeded)
                {
                    converter = null;     // everyone else takes null for an answer.
                }

                Converter = converter;
            }
        }

        // return true if DataContext is set locally (not inherited) on DependencyObject
        internal static bool HasLocalDataContext(DependencyObject d)
        {
            bool hasModifiers;
            BaseValueSourceInternal valueSource = d.GetValueSource(FrameworkElement.DataContextProperty, null, out hasModifiers);
            return (valueSource != BaseValueSourceInternal.Inherited) &&
                    (valueSource != BaseValueSourceInternal.Default || hasModifiers);
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private bool CanActivate
        {
            get { return StatusInternal != BindingStatusInternal.Unattached; }
        }

        private BindingWorker Worker { get { return _worker; } }

        private DynamicValueConverter DynamicConverter
        {
            get
            {
                if (!HasValue(Feature.DynamicConverter))
                {
                    Invariant.Assert(Worker != null);
                    // pass along the static source and target types to find same DefaultValueConverter as SetupDefaultValueConverter
                    SetValue(Feature.DynamicConverter, new DynamicValueConverter(IsReflective, Worker.SourcePropertyType, Worker.TargetPropertyType), null);
                }
                return (DynamicValueConverter)GetValue(Feature.DynamicConverter, null);
            }
        }

        private DataSourceProvider DataProvider
        {
            get { return (DataSourceProvider)GetValue(Feature.DataProvider, null); }
            set { SetValue(Feature.DataProvider, value, null); }
        }


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

#region Attachment

        /// <summary>
        /// Attach the binding expression to the given target object and property.
        /// </summary>
        internal override bool AttachOverride(DependencyObject target, DependencyProperty dp)
        {
            if (!base.AttachOverride(target, dp))
                return false;

            // listen for InheritanceContext change (if target is mentored)
            if (ParentBinding.SourceReference == null || ParentBinding.SourceReference.UsesMentor)
            {
                DependencyObject mentor = Helper.FindMentor(target);
                if (mentor != target)
                {
                    InheritanceContextChangedEventManager.AddHandler(target, OnInheritanceContextChanged);
                    UsingMentor = true;

                    if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Attach))
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.UseMentor(
                                                TraceData.Identify(this),
                                                TraceData.Identify(mentor)),
                                            this);
                    }
                }
            }

            // listen for lost focus
            if (IsUpdateOnLostFocus)
            {
                Invariant.Assert(!IsInMultiBindingExpression, "Source BindingExpressions of a MultiBindingExpression should never be UpdateOnLostFocus.");
                LostFocusEventManager.AddHandler(target, OnLostFocus);
            }

            // attach to things that need tree context.  Do it synchronously
            // if possible, otherwise post a task.  This gives the parser et al.
            // a chance to assemble the tree before we start walking it.
            AttachToContext(AttachAttempt.First);
            if (StatusInternal == BindingStatusInternal.Unattached)
            {
                Engine.AddTask(this, TaskOps.AttachToContext);

                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.AttachToContext))
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.DeferAttachToContext(
                                            TraceData.Identify(this)),
                                        this);
                }
            }

            GC.KeepAlive(target);   // keep target alive during activation (bug 956831)
            return true;
        }


        /// <summary>
        /// Detach the binding expression from its target object and property.
        /// </summary>
        internal override void DetachOverride()
        {
            Deactivate();

            DetachFromContext();

            // detach from target element
            DependencyObject target = TargetElement;
            if (target != null && IsUpdateOnLostFocus)
            {
                LostFocusEventManager.RemoveHandler(target, OnLostFocus);
            }

            // detach from INDEI.ErrorsChanged event
            if (ValidatesOnNotifyDataErrors)
            {
                WeakReference dataErrorWR = (WeakReference)GetValue(Feature.DataErrorValue, null);
                INotifyDataErrorInfo dataErrorValue = (dataErrorWR == null) ? null : dataErrorWR.Target as INotifyDataErrorInfo;
                if (dataErrorValue != null)
                {
                    ErrorsChangedEventManager.RemoveHandler(dataErrorValue, OnErrorsChanged);
                    SetValue(Feature.DataErrorValue, null, null);
                }
            }

            ChangeValue(DependencyProperty.UnsetValue, false);

            base.DetachOverride();
        }

        // try to get information from the tree context (parent, root, etc.)
        // If everything succeeds, activate the binding.
        // If anything fails in a way that might succeed after further layout,
        // just return (with status == Unattached).  The binding engine will try
        // again later. For hard failures, set an error status;  no more chances.
        // During the "last chance" attempt, treat all failures as "hard".
        void AttachToContext(AttachAttempt attempt)
        {
            // if the target has been GC'd, just give up
            DependencyObject target = TargetElement;
            if (target == null)
                return;     // status will be Detached

            bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.AttachToContext);
            bool traceObjectRef = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.SourceLookup);

            // certain features should never be tried on the first attempt, as
            // they certainly require at least one layout pass
            if (attempt == AttachAttempt.First)
            {
                // relative source with ancestor lookup
                ObjectRef or = ParentBinding.SourceReference;
                if (or != null && or.TreeContextIsRequired(target))
                {
                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.SourceRequiresTreeContext(
                                                TraceData.Identify(this),
                                                or.Identify()),
                                            this);
                    }

                    return;
                }
            }

            bool lastChance = (attempt == AttachAttempt.Last);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.AttachToContext(
                                        TraceData.Identify(this),
                                        lastChance ? " (last chance)" : String.Empty),
                                    this);
            }

            // if the path has unresolved type names, the parser needs namesapce
            // information to resolve them.  See XmlTypeMapper.GetTypeFromName.
            // Ignore this requirement during the last chance, and just let
            // GetTypeFromName fail if it wants to.
            if (!lastChance && ParentBinding.TreeContextIsRequired)
            {
                if (target.GetValue(XmlAttributeProperties.XmlnsDictionaryProperty) == null ||
                    target.GetValue(XmlAttributeProperties.XmlNamespaceMapsProperty) == null)
                {
                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.PathRequiresTreeContext(
                                                TraceData.Identify(this),
                                                ParentBinding.Path.Path),
                                            this);
                    }

                    return;
                }
            }

            // if the binding uses a mentor, check that it exists
            DependencyObject mentor = !UsingMentor ? target :  Helper.FindMentor(target);
            if (mentor == null)
            {
                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                        TraceData.NoMentorExtended(
                            TraceData.Identify(this)),
                        this);
                }

                if (lastChance)
                {
                    SetStatus(BindingStatusInternal.PathError);
                    if (TraceData.IsEnabled)
                    {
                        TraceData.TraceAndNotify(TraceEventType.Error, TraceData.NoMentor, this);
                    }
                }
                return;
            }

            // determine the element whose DataContext governs this BindingExpression
            DependencyObject contextElement = null;     // no context element
            bool contextElementFound = true;
            if (ParentBinding.SourceReference == null)
            {
                contextElement = mentor;    // usually the mentor/target element

                // special cases:
                // 1. if target property is DataContext, use the target's parent.
                //      This enables <X DataContext="{Binding...}"/> and
                // 2. if the target is ContentPresenter and the target property
                //      is Content, use the parent.  This enables
                //          <ContentPresenter Content="{Binding...}"/>
                // 3. if target is CVS, and its inheritance context was set
                //      via DataContext, use the mentor's parent.  This enables
                //      <X.DataContext> <CollectionViewSource Source={Binding ...}/>
                CollectionViewSource cvs;
                if (TargetProperty == FrameworkElement.DataContextProperty ||
                    (TargetProperty == ContentPresenter.ContentProperty &&
                            target is ContentPresenter) ||
                    (UsingMentor &&
                            (cvs = target as CollectionViewSource) != null &&
                            cvs.PropertyForInheritanceContext == FrameworkElement.DataContextProperty)
                    )
                {
                    contextElement = FrameworkElement.GetFrameworkParent(contextElement);
                    contextElementFound = (contextElement != null);
                }
            }
            else
            {
                RelativeObjectRef ror = ParentBinding.SourceReference as RelativeObjectRef;
                if (ror != null && ror.ReturnsDataContext)
                {
                    object o = ror.GetObject(mentor, new ObjectRefArgs() { IsTracing = traceObjectRef});
                    contextElement = o as DependencyObject;    // ref to another element's DataContext
                    contextElementFound = (o != DependencyProperty.UnsetValue);
                }
            }

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.ContextElement(
                                        TraceData.Identify(this),
                                        TraceData.Identify(contextElement),
                                        contextElementFound ? "OK" : "error"),
                                    this);
            }

            // if we need a context element, check that we found it
            if (!contextElementFound)
            {
                if (lastChance)
                {
                    SetStatus(BindingStatusInternal.PathError);
                    if (TraceData.IsEnabled)
                    {
                        TraceData.TraceAndNotify(TraceEventType.Error, TraceData.NoDataContext, this);
                    }
                }

                return;
            }

            // determine the source object, from which the path evaluation starts
            object source;
            ObjectRef sourceRef;

            if (contextElement != null)
            {
                source = contextElement.GetValue(FrameworkElement.DataContextProperty);

                // if the data context is default null, try again later;  future
                // layout may change the inherited value.
                // Ignore this requirement during the last chance, and just let
                // the binding to null DataContext proceed.
                if (source == null && !lastChance && !HasLocalDataContext(contextElement))
                {
                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                            TraceData.NullDataContext(
                                TraceData.Identify(this)),
                            this);
                    }

                    return;
                }
            }
            else if ((sourceRef = ParentBinding.SourceReference) != null)
            {
                ObjectRefArgs args = new ObjectRefArgs() {
                                        IsTracing = traceObjectRef,
                                        ResolveNamesInTemplate = ResolveNamesInTemplate,
                                        };
                source = sourceRef.GetDataObject(mentor, args);

                // check that the source could be found
                if (source == DependencyProperty.UnsetValue)
                {
                    if (lastChance)
                    {
                        SetStatus(BindingStatusInternal.PathError);
                        if (TraceData.IsEnabled)
                        {
                            TraceData.TraceAndNotify(TraceLevel, TraceData.NoSource(sourceRef), this);
                        }
                    }

                    return;
                }
                else if (!lastChance && args.NameResolvedInOuterScope)
                {
                    // if ElementName resolved in an outer scope, it's possible
                    // that future work might add the name to an inner scope.
                    // Schedule a task to check this.
                    Engine.AddTask(this, TaskOps.VerifySourceReference);
                }
            }
            else
            {
                // we get here only if we need ambient data context, but there
                // is no context element.  E.g. binding the DataContext property
                // of an element with no parent.  Just use null.
                source = null;
            }

            // if we get this far, all the ingredients for a successful binding
            // are present.  Remember what we've found and activate the binding.
            if (contextElement != null)
                _ctxElement = new WeakReference(contextElement);

            // attach to context element
            ChangeWorkerSources(null, 0);

            if (!UseDefaultValueConverter)
            {
                Converter = ParentBinding.Converter;

                if (Converter == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.MissingValueConverter)); // report instead of throw?
                }
            }

            // join the right binding group (if any)
            JoinBindingGroup(IsReflective, contextElement);

            SetStatus(BindingStatusInternal.Inactive);

            // inner BindingExpressions of PriorityBindingExpressions may not need to be activated
            if (IsInPriorityBindingExpression)
                ParentPriorityBindingExpression.InvalidateChild(this);
            else    // singular BindingExpressions and those in MultiBindingExpressions should always activate
                Activate(source);

            GC.KeepAlive(target);   // keep target alive during activation (bug 956831)
        }

        // Detach from things that may require tree context
        private void DetachFromContext()
        {
            // detach from data source
            if (HasValue(Feature.DataProvider))
            {
                DataChangedEventManager.RemoveHandler(DataProvider, OnDataChanged);
            }

            if (!UseDefaultValueConverter)
            {
                Converter = null;
            }

            if (!IsInBindingExpressionCollection)
                ChangeSources(null);

            if (UsingMentor)
            {
                DependencyObject target = TargetElement;
                if (target != null)
                    InheritanceContextChangedEventManager.RemoveHandler(target, OnInheritanceContextChanged);
            }

            _ctxElement = null;
        }

#endregion Attachment

#region Activation

        // Activate the BindingExpression, if necessary and possible
        internal override void Activate()
        {
            if (!CanActivate)
                return;

            if (_ctxElement == null)
            {
                // only activate once if there's an explicit source
                if (StatusInternal == BindingStatusInternal.Inactive)
                {
                    DependencyObject target = TargetElement;
                    if (target != null)
                    {
                        if (UsingMentor)
                        {
                            target = Helper.FindMentor(target);
                            if (target == null)
                            {
                                // mentor is not available
                                SetStatus(BindingStatusInternal.PathError);
                                if (TraceData.IsEnabled)
                                {
                                    TraceData.TraceAndNotify(TraceEventType.Error, TraceData.NoMentor, this);
                                }
                                return;
                            }
                        }
                        Activate(ParentBinding.SourceReference.GetDataObject(target,
                                                                new ObjectRefArgs() { ResolveNamesInTemplate = ResolveNamesInTemplate }));
                    }
                }
            }
            else
            {
                DependencyObject contextElement = ContextElement;
                if (contextElement == null)
                {
                    // context element has been GC'd, or unavailable (e.g. no mentor)
                    SetStatus(BindingStatusInternal.PathError);
                    if (TraceData.IsEnabled)
                    {
                        TraceData.TraceAndNotify(TraceEventType.Error, TraceData.NoDataContext, this);
                    }
                    return;
                }

                object item = contextElement.GetValue(FrameworkElement.DataContextProperty);

                // if binding inactive or the data item has changed, (re-)activate
                if (StatusInternal == BindingStatusInternal.Inactive || !System.Windows.Controls.ItemsControl.EqualsEx(item, DataItem))
                {
                    Activate(item);
                }
            }
        }

        internal void Activate(object item)
        {
            DependencyObject target = TargetElement;
            if (target == null)
                return;

            if (item == DisconnectedItem)
            {
                // disconnect from the former item, with the least acceptable impact
                Disconnect();
                return;
            }

            bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Activate);

            Deactivate();

            // apply magic (for CVS, DSP, etc.), unless asked not to
            if (!ParentBinding.BindsDirectlyToSource)
            {
                CollectionViewSource cvs = item as CollectionViewSource;
                this.CollectionViewSource = cvs;
                if (cvs != null)
                {
                    item = cvs.CollectionView;

                    // the CVS is one of our implicit sources
                    ChangeWorkerSources(null, 0);

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.UseCVS(
                                                TraceData.Identify(this),
                                                TraceData.Identify(cvs)),
                                            this);
                    }
                }
                else
                {
                    // when the source is DataSourceProvider, use its data instead
                    item = DereferenceDataProvider(item);
                }
            }

            _dataItem = CreateReference(item);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.ActivateItem(
                                        TraceData.Identify(this),
                                        TraceData.Identify(item)),
                                    this);
            }

            if (Worker == null)
                CreateWorker();

            // mark the BindingExpression active
            SetStatus(BindingStatusInternal.Active);

            // attach to data item (may set error status)
            Worker.AttachDataItem();

            // BUG: allow transfers now; see remark inside BindOperations.SetBinding and BindingExpression.TransferValue
            //ClearFlag(BindingFlags.iInUpdate);

            // initial transfer
            bool initialTransferIsUpdate = IsOneWayToSource;
            object currentValue;
            if (ShouldUpdateWithCurrentValue(target, out currentValue))
            {
                initialTransferIsUpdate = true;
                ChangeValue(currentValue, /*notify*/false);
                NeedsUpdate = true;
            }

            if (!initialTransferIsUpdate)
            {
                ValidationError error;
                object value = GetInitialValue(target, out error);
                bool useValueFromBindingGroup = (value == NullDataItem);
                if (!useValueFromBindingGroup)
                {
                    TransferValue(value, false);
                }

                if (error != null)
                {
                    UpdateValidationError(error, useValueFromBindingGroup);
                }
            }
            else if (!IsInMultiBindingExpression)   // MultiBinding initiates its own update
            {
                UpdateValue();
            }

            GC.KeepAlive(target);   // keep target alive during activation (bug 956831)
        }

        // return the value for the initial data transfer.  Specifically:
        //      normal case - return UnsetValue (we'll get the value from the source object)
        //      valid proposed value - return the proposed value
        //      invalid proposed value - two subcases:
        //          one-way binding - return default/fallback value
        //          two-way binding - retrun NullDataItem (meaning "don't transfer")
        //
        //      In both subcases, adopt the validation errors discovered earlier.
        //      In the two-way subcase, instead of a source-to-target transfer, we set the
        //      the target property to the (saved) raw proposed value, as if the user
        //      had edited this property.
        object GetInitialValue(DependencyObject target, out ValidationError error)
        {
            object proposedValue;

            // find the binding group this binding would join if it were two-way (even if it isn't)
            BindingGroup bindingGroup = RootBindingExpression.FindBindingGroup(true, ContextElement);
            BindingGroup.ProposedValueEntry entry;

            // get the proposed value from the binding group
            if (bindingGroup == null ||
                (entry = bindingGroup.GetProposedValueEntry(SourceItem, SourcePropertyName)) == null)
            {
                // no proposed value
                error = null;
                proposedValue = DependencyProperty.UnsetValue;
            }
            else
            {
                // adopt the validation error (possibly null)
                error = entry.ValidationError;

                if (IsReflective && TargetProperty.IsValidValue(entry.RawValue))
                {
                    // two-way binding - set the target property directly
                    target.SetValue(TargetProperty, entry.RawValue);
                    proposedValue = NullDataItem;
                    bindingGroup.RemoveProposedValueEntry(entry);
                }
                else if (entry.ConvertedValue == DependencyProperty.UnsetValue)
                {
                    // invalid proposed value - use fallback/default
                    proposedValue = UseFallbackValue();
                }
                else
                {
                    // valid proposed value
                    proposedValue = entry.ConvertedValue;
                }

                // if this binding didn't take over responsibility for the proposed
                // value, add it to the list of bindings using the proposed value
                if (proposedValue != NullDataItem)
                {
                    bindingGroup.AddBindingForProposedValue(this, SourceItem, SourcePropertyName);
                }
            }

            return proposedValue;
        }

        internal override void Deactivate()
        {
            // inactive BindingExpressions don't need any more work
            if (StatusInternal == BindingStatusInternal.Inactive)
                return;

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Activate))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.Deactivate(
                                        TraceData.Identify(this)),
                                    this);
            }

            // stop transfers
            CancelPendingTasks();

            // detach from data item
            if (Worker != null)
                Worker.DetachDataItem();

            // restore default value, in case source/converter fail to provide a good value
            ChangeValue(DefaultValueObject, false);

            // don't keep a handle to old data item if the BindingExpression is inactive
            _dataItem = null;

            SetStatus(BindingStatusInternal.Inactive);
        }

        // disconnect from the former item, with the least acceptable impact.
        // This arises when a container is removed from an ItemsControl.
        internal override void Disconnect()
        {
            // disconnect this binding from its former item
            _dataItem = CreateReference(DisconnectedItem);

            if (Worker == null)
                return;

            Worker.AttachDataItem();

            base.Disconnect();
        }

        // if the root item is a DataSourceProvider, use its Data instead
        // and listen for DataChanged events.  (Unless overridden explicitly
        // by the BindsDirectlyToSource property).
        private object DereferenceDataProvider(object item)
        {
            DataSourceProvider newDataProvider = item as DataSourceProvider;
            DataSourceProvider oldDataProvider = DataProvider;
            if (newDataProvider != oldDataProvider)
            {
                // we have a new data provider - retarget the event handler
                if (oldDataProvider != null)
                {
                    DataChangedEventManager.RemoveHandler(oldDataProvider, OnDataChanged);
                }

                DataProvider = newDataProvider;
                oldDataProvider = newDataProvider;

                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Activate))
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.UseDataProvider(
                                            TraceData.Identify(this),
                                            TraceData.Identify(newDataProvider)),
                                        this);
                }

                if (newDataProvider != null)
                {
                    DataChangedEventManager.AddHandler(newDataProvider, OnDataChanged);
                    newDataProvider.InitialLoad();
                }
            }

            return (oldDataProvider != null) ? oldDataProvider.Data : item;
        }

        // Return the object from which the given value was obtained, if possible
        internal override object GetSourceItem(object newValue)
        {
            return SourceItem;
        }

#endregion Activation

#region Worker

        private void CreateWorker()
        {
            Invariant.Assert(Worker == null, "duplicate call to CreateWorker");

            _worker = new ClrBindingWorker(this, Engine);
        }

        // worker calls here if it changes its dependency sources
        // n is the number of real entries in newWorkerSources (which may be longer)
        internal void ChangeWorkerSources(WeakDependencySource[] newWorkerSources, int n)
        {
            int offset = 0;
            int size = n;

            // create the new sources array, and add the context and CollectionViewSource elements
            DependencyObject contextElement = ContextElement;
            CollectionViewSource cvs = CollectionViewSource;
            bool usesLanguage = UsesLanguage;

            if (contextElement != null)
                ++size;
            if (cvs != null)
                ++size;
            if (usesLanguage)
                ++size;

            WeakDependencySource[] newSources = (size > 0) ? new WeakDependencySource[size] : null;

            if (contextElement != null)
            {
                newSources[offset++] = new WeakDependencySource(_ctxElement, FrameworkElement.DataContextProperty);
            }

            if (cvs != null)
            {
                WeakReference wr = GetValue(Feature.CollectionViewSource, null) as WeakReference;
                newSources[offset++] =
                    (wr != null) ? new WeakDependencySource(wr, CollectionViewSource.ViewProperty)
                                 : new WeakDependencySource(cvs, CollectionViewSource.ViewProperty);
            }

            if (usesLanguage)
            {
                newSources[offset++] = new WeakDependencySource(TargetElementReference, FrameworkElement.LanguageProperty);
            }

            // add the worker's sources
            if (n > 0)
                Array.Copy(newWorkerSources, 0, newSources, offset, n);

            // tell the property engine
            ChangeSources(newSources);
        }

#endregion Worker

#region Value

        // transfer a value from the source to the target
        void TransferValue()
        {
            TransferValue(DependencyProperty.UnsetValue, false);
        }

        // transfer a value from the source to the target
        internal void TransferValue(object newValue, bool isASubPropertyChange)
        {
            // if the target element has been GC'd, do nothing
            DependencyObject target = TargetElement;
            if (target == null)
                return;

            // if the BindingExpression hasn't activated, do nothing
            if (Worker == null)
                return;

            Type targetType = GetEffectiveTargetType();
            IValueConverter implicitConverter = null;
            bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Transfer);

            // clear the Pending flag before actually doing the transfer.  That way if another
            // thread sets the flag, we'll schedule another transfer.  This might do more
            // transfers than absolutely necessary, but it guarantees that we'll eventually pick
            // up the value from the last change.
            IsTransferPending = false;
            IsInTransfer = true;

            UsingFallbackValue = false;

            // get the raw value from the source
            object value = (newValue == DependencyProperty.UnsetValue) ? Worker.RawValue() : newValue;

            UpdateNotifyDataErrors(value);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GetRawValue(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)),
                                    this);
            }

            // apply any necessary conversions
            if (value != DependencyProperty.UnsetValue)
            {
            #if !TargetNullValueBC  //BreakingChange
                bool doNotSetStatus = false;
            #endif

                if (!UseDefaultValueConverter)
                {
                    // if there's a user-defined converter, call it without catching
                    // exceptions (bug 992237).  It can return DependencyProperty.UnsetValue
                    // to indicate a failure to convert.
                    value = Converter.Convert(value, targetType, ParentBinding.ConverterParameter, GetCulture());

                    if (IsDetached)
                    {
                        // user code detached the binding.  Give up.
                        return;
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.UserConverter(
                                                TraceData.Identify(this),
                                                TraceData.Identify(value)),
                                            this);
                    }

                    // chain in a default value converter if the returned value's type is not compatible with the targetType
                    if (   ((value != null) && (value != Binding.DoNothing) && (value != DependencyProperty.UnsetValue))
                        && !targetType.IsAssignableFrom(value.GetType()))
                    {
                        // the dynamic converter is shared between Transfer and Update directions
                        // once instantiated, DefaultValueConverters are kept in a lookup table, making swapping
                        // default value converters in the DynamicValueConverter reasonably fast
                        implicitConverter = DynamicConverter;
                    }
                }
                else
                {
                    // if there's no user-defined converter, use the default converter (if any)
                    // we chose earlier (in SetupDefaultValueConverter)
                    implicitConverter = Converter;
                }

                // apply an implicit conversion, if needed.  This can be
                //  a) null conversion
                //  b) string formatting
                //  c) type conversion
                if ((value != Binding.DoNothing) && (value != DependencyProperty.UnsetValue))
                {
                    // ultimately, TargetNullValue should get assigned implicitly,
                    // even if the user doesn't declare it.  We can't do this yet because
                    // of back-compat.  I wrote it both ways, and #if'd out the breaking
                    // change.
                #if TargetNullValueBC   //BreakingChange
                    if (IsNullValue(value))
                #else
                    if (EffectiveTargetNullValue != DependencyProperty.UnsetValue &&
                        IsNullValue(value))
                #endif
                    {
                        value = EffectiveTargetNullValue;

                        if (isExtendedTraceEnabled)
                        {
                            TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                                TraceData.NullConverter(
                                                    TraceData.Identify(this),
                                                    TraceData.Identify(value)),
                                                this);
                        }
                    }
                #if !TargetNullValueBC   //BreakingChange
                    // For DBNull, unless there's a user converter, we handle it here
                    else if ((value == DBNull.Value) && (Converter == null || UseDefaultValueConverter))
                    {
                        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            value = null;
                        }
                        else
                        {
                            value = DependencyProperty.UnsetValue;
                            // The 3.5 code failed to set the status to UpdateTargetError in this
                            // case.  It's a bug, but we have to maintain the buggy behavior for
                            // back-compat.
                            doNotSetStatus = true;
                        }

                        if (isExtendedTraceEnabled)
                        {
                            TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                                TraceData.ConvertDBNull(
                                                    TraceData.Identify(this),
                                                    TraceData.Identify(value)),
                                                this);
                        }
                    }
                #endif
                    else if (implicitConverter != null || EffectiveStringFormat != null)
                    {
                        // call a DefaultValueConverter:
                        // NOTE:
                        // here we pass in the TargetElement that is expected by our default value converters;
                        // this does violate the general rule that value converters should be stateless
                        // and must not be aware of e.g. their target element.
                        // Our DefaultValueConverters are all internal and only use this target element
                        // to determine a BaseUri for their TypeConverters
                        // -> hence a reluctant exeption of above rule
                        value = ConvertHelper(implicitConverter, value, targetType, TargetElement, GetCulture());

                        if (isExtendedTraceEnabled)
                        {
                            TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                                TraceData.DefaultConverter(
                                                    TraceData.Identify(this),
                                                    TraceData.Identify(value)),
                                                this);
                        }
                    }
                }

                if (
            #if !TargetNullValueBC   //BreakingChange
                    !doNotSetStatus &&
            #endif
                    value == DependencyProperty.UnsetValue)
                {
                    SetStatus(BindingStatusInternal.UpdateTargetError);
                }
            }

            // the special value DoNothing means no error, but no data transfer
            if (value == Binding.DoNothing)
                goto Done;

            // if the value isn't acceptable to the target property, don't use it
            // (in MultiBinding, the value will go through the multi-converter, so
            // it's too early to make this judgment)
            if (!IsInMultiBindingExpression && value != IgnoreDefaultValue &&
                value != DependencyProperty.UnsetValue && !TargetProperty.IsValidValue(value))
            {
                if (TraceData.IsEnabled && !IsInBindingExpressionCollection)
                {
                    TraceData.TraceAndNotify(TraceLevel, TraceData.BadValueAtTransfer, this,
                        traceParameters: new object[] { value, this });
                }

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.BadValueAtTransferExtended(
                                            TraceData.Identify(this),
                                            TraceData.Identify(value)),
                                        this);
                }

                value = DependencyProperty.UnsetValue;

                if (StatusInternal == BindingStatusInternal.Active)
                    SetStatus(BindingStatusInternal.UpdateTargetError);
            }

            // If we haven't obtained a value yet,
            // use the fallback value.  This could happen when the currency
            // has moved off the end of the collection, e.g.
            if (value == DependencyProperty.UnsetValue)
            {
                value = UseFallbackValue();

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.UseFallback(
                                            TraceData.Identify(this),
                                            TraceData.Identify(value)),
                                        this);
                }
            }

            // Ignore a default source value by setting the value to NoValue;
            // this causes the property engine to obtain the value elsewhere
            if (value == IgnoreDefaultValue)
            {
                value = Expression.NoValue;
            }

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.TransferValue(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)),
                                    this);
            }

            // if this is a re-transfer after a source update and the value
            // hasn't changed, don't do any more work.
            bool realTransfer = !(IsInUpdate && System.Windows.Controls.ItemsControl.EqualsEx(value, Value));

            if (realTransfer)
            {
                // update the cached value
                ChangeValue(value, true);

                // push the new value through the property engine
                Invalidate(isASubPropertyChange);

                // evaluate "forward" validation rules
                ValidateOnTargetUpdated();
            }

            // after updating all the state (value, validation), mark the binding clean
            Clean();

            if (realTransfer)
            {
                OnTargetUpdated();
            }

        Done:
            IsInTransfer = false;

            GC.KeepAlive(target);   // keep target alive during transfer (bug 956831)
        }

        // run the validation rules marked as ValidateOnTargetUpdated
        private void ValidateOnTargetUpdated()
        {
            // update the validation state
            ValidationError validationError = null;

            Collection<ValidationRule> validationRules = ParentBinding.ValidationRulesInternal;
            CultureInfo culture = null;

            bool needDataErrorRule = ParentBinding.ValidatesOnDataErrors;

            if (validationRules != null)
            {
                // these may be needed by several rules, but only compute them once
                object rawValue = DependencyProperty.UnsetValue;
                object itemValue = DependencyProperty.UnsetValue;

                foreach (ValidationRule validationRule in validationRules)
                {
                    if (validationRule.ValidatesOnTargetUpdated)
                    {
                        if (validationRule is DataErrorValidationRule)
                        {
                            needDataErrorRule = false;
                        }

                        object value;

                        switch (validationRule.ValidationStep)
                        {
                            case ValidationStep.RawProposedValue:
                                if (rawValue == DependencyProperty.UnsetValue)
                                {
                                    rawValue = GetRawProposedValue();
                                }
                                value = rawValue;
                                break;
                            case ValidationStep.ConvertedProposedValue:
                            case ValidationStep.UpdatedValue:
                            case ValidationStep.CommittedValue:
                                if (itemValue == DependencyProperty.UnsetValue)
                                {
                                    itemValue = Worker.RawValue();
                                }
                                value = itemValue;
                                break;
                            default:
                                throw new InvalidOperationException(SR.Get(SRID.ValidationRule_UnknownStep, validationRule.ValidationStep, validationRule));
                        }

                        // lazy-fetch culture (avoids exception when target DP is Language)
                        if (culture == null)
                        {
                            culture = GetCulture();
                        }

                        validationError = RunValidationRule(validationRule, value, culture);
                        if (validationError != null)
                            break;
                    }
                }
            }

            if (needDataErrorRule && validationError == null)
            {
                // lazy-fetch culture (avoids exception when target DP is Language)
                if (culture == null)
                {
                    culture = GetCulture();
                }

                validationError = RunValidationRule(DataErrorValidationRule.Instance, this, culture);
            }

            UpdateValidationError(validationError);
        }

        ValidationError RunValidationRule(ValidationRule validationRule, object value, CultureInfo culture)
        {
            ValidationError error;

            ValidationResult validationResult = validationRule.Validate(value, culture, this);

            if (validationResult.IsValid)
            {
                error = null;
            }
            else
            {
                if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Update))
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.ValidationRuleFailed(
                                            TraceData.Identify(this),
                                            TraceData.Identify(validationRule)),
                                        this);
                }

                error = new ValidationError(validationRule, this, validationResult.ErrorContent, null);
            }

            return error;
        }

        private object ConvertHelper(IValueConverter converter, object value, Type targetType, object parameter, CultureInfo culture)
        {
            // use the StringFormat (if appropriate) in preference to the default converter
            string stringFormat = EffectiveStringFormat;
            Invariant.Assert(converter != null || stringFormat != null);

            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
            #pragma warning disable 56500

            object convertedValue = null;
            try
            {
                if (stringFormat != null)
                {
                    convertedValue = String.Format(culture, stringFormat, value);
                }
                else
                {
                    convertedValue = converter.Convert(value, targetType, parameter, culture);
                }
            }

            // Catch all exceptions.  There is no app code on the stack,
            // so the exception isn't actionable by the app.
            // Yet we don't want to crash the app.
            catch (Exception ex)
            {
                // the DefaultValueConverter can end up calling BaseUriHelper.GetBaseUri()
                // which can raise SecurityException if the app does not have the right FileIO privileges
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;

                if (TraceData.IsEnabled)
                {
                    string name = String.IsNullOrEmpty(stringFormat) ? converter.GetType().Name : "StringFormat";
                    TraceData.TraceAndNotify(TraceLevel,
                            TraceData.BadConverterForTransfer(
                                name,
                                AvTrace.ToStringHelper(value),
                                AvTrace.TypeName(value)),
                            this, ex);
                }
                convertedValue = DependencyProperty.UnsetValue;
            }
            catch // non CLS compliant exception
            {
                if (TraceData.IsEnabled)
                {
                    TraceData.TraceAndNotify(TraceLevel,
                            TraceData.BadConverterForTransfer(
                                converter.GetType().Name,
                                AvTrace.ToStringHelper(value),
                                AvTrace.TypeName(value)),
                            this);
                }
                convertedValue = DependencyProperty.UnsetValue;
            }

            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            return convertedValue;
        }

        private object ConvertBackHelper(IValueConverter converter,
                                         object value,
                                         Type sourceType,
                                         object parameter,
                                         CultureInfo culture)
        {
            Invariant.Assert(converter != null);

            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
            #pragma warning disable 56500

            object convertedValue = null;
            try
            {
                convertedValue = converter.ConvertBack(value, sourceType, parameter, culture);
            }

            // Catch all exceptions.  There is no app code on the stack,
            // so the exception isn't actionable by the app.
            // Yet we don't want to crash the app.
            catch (Exception ex)
            {
                // the DefaultValueConverter can end up calling BaseUriHelper.GetBaseUri()
                // which can raise SecurityException if the app does not have the right FileIO privileges
                ex = CriticalExceptions.Unwrap(ex);
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;

                if (TraceData.IsEnabled)
                {
                    TraceData.TraceAndNotify(TraceEventType.Error,
                        TraceData.BadConverterForUpdate(
                            AvTrace.ToStringHelper(Value),
                            AvTrace.TypeName(value)),
                        this, ex);
                }

                ProcessException(ex, ValidatesOnExceptions);
                convertedValue = DependencyProperty.UnsetValue;
            }
            catch // non CLS compliant exception
            {
                if (TraceData.IsEnabled)
                {
                    TraceData.TraceAndNotify(TraceEventType.Error,
                        TraceData.BadConverterForUpdate(
                            AvTrace.ToStringHelper(Value),
                            AvTrace.TypeName(value)),
                        this);
                }
                convertedValue = DependencyProperty.UnsetValue;
            }

            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            return convertedValue;
        }

        internal void ScheduleTransfer(bool isASubPropertyChange)
        {
            if (isASubPropertyChange && Converter != null)
            {
                // a converter doesn't care about sub-property changes
                isASubPropertyChange = false;
            }

            TransferValue(DependencyProperty.UnsetValue, isASubPropertyChange);
        }


        void OnTargetUpdated()
        {
            if (NotifyOnTargetUpdated)
            {
                DependencyObject target = TargetElement;
                if (target != null)
                {
                    if (    !IsInMultiBindingExpression           // not an inner BindingExpression
                        &&  (   !IsInPriorityBindingExpression
                            ||  this == ParentPriorityBindingExpression.ActiveBindingExpression)    // in ProrityBinding, and either active
                            ||  (IsAttaching && (StatusInternal==BindingStatusInternal.Active || UsingFallbackValue) // or about to become active
                            )
                        )
                    {
                        // while attaching a normal (not style-defined) BindingExpression,
                        // we must defer raising the event until after the
                        // property has been invalidated, so that the event handler
                        // gets the right value if it asks (bug 1036862)
                        if (IsAttaching && RootBindingExpression == target.ReadLocalValue(TargetProperty))
                        {
                            Engine.AddTask(this, TaskOps.RaiseTargetUpdatedEvent);
                        }
                        else
                        {
                            OnTargetUpdated(target, TargetProperty);
                        }
                    }
                }
            }
        }

        void OnSourceUpdated()
        {
            if (NotifyOnSourceUpdated)
            {
                DependencyObject target = TargetElement;
                if (target != null)
                {
                    if (    !IsInMultiBindingExpression           // not an inner BindingExpression
                        &&  (   !IsInPriorityBindingExpression
                            ||  this == ParentPriorityBindingExpression.ActiveBindingExpression))   // not an inactive BindingExpression
                    {
                        OnSourceUpdated(target, TargetProperty);
                    }
                }
            }
        }


        internal override bool ShouldReactToDirtyOverride()
        {
            // if the binding has disconnected, no need to react to Dirty()
            return (DataItem != DisconnectedItem);
        }

        // transfer a value from target to source
        internal override bool UpdateOverride()
        {
            // various reasons not to update:
            if (   !NeedsUpdate                     // nothing to do
                || !IsReflective                    // no update desired
                || IsInTransfer                     // in a transfer
                || Worker == null                   // not activated
                || !Worker.CanUpdate                // no source (currency moved off end)
                )
                return true;

            return UpdateValue();
        }

        internal override object ConvertProposedValue(object value)
        {
            object rawValue = value;
            bool isExtendedTraceEnabled = TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Update);
            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.UpdateRawValue(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)),
                                    this);
            }

            Type sourceType = Worker.SourcePropertyType;
            IValueConverter implicitConverter = null;
            CultureInfo culture = GetCulture();

            // apply user-defined converter
            if (Converter != null)
            {
                if (!UseDefaultValueConverter)
                {
                    // if there's a user-defined converter, call it without catching
                    // exceptions (bug 992237).  It can return DependencyProperty.UnsetValue
                    // to indicate a failure to convert.
                    value = Converter.ConvertBack(value, sourceType, ParentBinding.ConverterParameter, culture);

                    if (IsDetached)
                    {
                        // user code detached the binding.  Give up.
                        return Binding.DoNothing;
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.UserConvertBack(
                                                TraceData.Identify(this),
                                                TraceData.Identify(value)),
                                            this);
                    }

                    // chain in a default value converter if the returned value's type is not compatible with the sourceType
                    if (value != Binding.DoNothing && value != DependencyProperty.UnsetValue &&
                        !IsValidValueForUpdate(value, sourceType))
                    {
                        // the dynamic converter is shared between Transfer and Update directions
                        // once instantiated, DefaultValueConverters are kept in a lookup table, making swapping
                        // default value converters reasonably fast
                        implicitConverter = DynamicConverter;
                    }
                }
                else
                {
                    implicitConverter = Converter;
                }
            }

            // apply an implicit conversion, if needed.  This can be
            //  a) null conversion
            //  b) type conversion
            if (value != Binding.DoNothing && value != DependencyProperty.UnsetValue)
            {
                if (IsNullValue(value))
                {
                    if (value == null || !IsValidValueForUpdate(value, sourceType))
                    {
                        if (Worker.IsDBNullValidForUpdate)
                        {
                            value = DBNull.Value;
                        }
                        else
                        {
                            value = NullValueForType(sourceType);
                        }
                    }
                }
                else if (implicitConverter != null)
                {
                    // here we pass in the TargetElement, see NOTE of caution in TransferValue() why this is ok
                    value = ConvertBackHelper(implicitConverter, value, sourceType, this.TargetElement, culture);

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.DefaultConvertBack(
                                                TraceData.Identify(this),
                                                TraceData.Identify(value)),
                                            this);
                    }
                }
            }

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.Update(
                                        TraceData.Identify(this),
                                        TraceData.Identify(value)),
                                    this);
            }

            // if the conversion failed, signal a validation error
            if (value == DependencyProperty.UnsetValue)
            {
                if (ValidationError == null)
                {
                    ValidationError validationError = new ValidationError(ConversionValidationRule.Instance, this, SR.Get(SRID.Validation_ConversionFailed, rawValue), null);
                    UpdateValidationError(validationError);
                }
            }

            return value;
        }

        /// <summary>
        /// Get the converted proposed value and inform the binding group
        /// <summary>
        internal override bool ObtainConvertedProposedValue(BindingGroup bindingGroup)
        {
            bool result = true;
            object value;

            if (NeedsUpdate)
            {
                value = bindingGroup.GetValue(this);
                if (value != DependencyProperty.UnsetValue)
                {
                    value = ConvertProposedValue(value);
                    if (value == DependencyProperty.UnsetValue)
                    {
                        result = false;
                    }
                }
            }
            else
            {
                value = BindingGroup.DeferredSourceValue;
            }

            bindingGroup.SetValue(this, value);
            return result;
        }

        internal override object UpdateSource(object value)
        {
            // If there is a failure to convert, then Update failed.
            if (value == DependencyProperty.UnsetValue)
            {
                SetStatus(BindingStatusInternal.UpdateSourceError);
            }

            if (value == Binding.DoNothing || value == DependencyProperty.UnsetValue ||
                ShouldIgnoreUpdate())
            {
                return value;
            }


            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
            #pragma warning disable 56500

            try
            {
                BeginSourceUpdate();
                Worker.UpdateValue(value);
            }

            // Catch all exceptions.  There is no app code on the stack,
            // so the exception isn't actionable by the app.
            // Yet we don't want to crash the app.
            catch (Exception ex)
            {
                ex = CriticalExceptions.Unwrap(ex);
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;

                if (TraceData.IsEnabled)
                    TraceData.TraceAndNotify(TraceEventType.Error, TraceData.WorkerUpdateFailed, this, ex);

                ProcessException(ex, (ValidatesOnExceptions || BindingGroup != null));
                SetStatus(BindingStatusInternal.UpdateSourceError);
                value = DependencyProperty.UnsetValue;
            }
            catch // non CLS compliant exception
            {
                if (TraceData.IsEnabled)
                    TraceData.TraceAndNotify(TraceEventType.Error, TraceData.WorkerUpdateFailed, this);

                SetStatus(BindingStatusInternal.UpdateSourceError);
                value = DependencyProperty.UnsetValue;
            }
            finally
            {
                EndSourceUpdate();
            }

            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            OnSourceUpdated();

            return value;
        }

        /// <summary>
        /// Update the source value and inform the binding group
        /// <summary>
        internal override bool UpdateSource(BindingGroup bindingGroup)
        {
            bool result = true;
            if (NeedsUpdate)
            {
                object value = bindingGroup.GetValue(this);
                value = UpdateSource(value);
                bindingGroup.SetValue(this, value);
                if (value == DependencyProperty.UnsetValue)
                {
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Store the value in the binding group
        /// </summary>
        internal override void StoreValueInBindingGroup(object value, BindingGroup bindingGroup)
        {
            bindingGroup.SetValue(this, value);
        }

        /// <summary>
        /// Run validation rules for the given step
        /// <summary>
        internal override bool Validate(object value, ValidationStep validationStep)
        {
            // run rules attached to this binding
            bool result = base.Validate(value, validationStep);

            if (validationStep == ValidationStep.UpdatedValue)
            {
                if (result && ParentBinding.ValidatesOnDataErrors)
                {
                    // remember the old validation error, if it came from the implicit DataError rule
                    ValidationError oldValidationError = GetValidationErrors(validationStep);
                    if (oldValidationError != null &&
                        oldValidationError.RuleInError != DataErrorValidationRule.Instance)
                    {
                        oldValidationError = null;
                    }

                    // run the DataError rule, even though it doesn't appear in the
                    // ValidationRules collection
                    ValidationError error = RunValidationRule(DataErrorValidationRule.Instance, this, GetCulture());

                    if (error != null)
                    {
                        UpdateValidationError(error);
                        result = false;
                    }
                    else if (oldValidationError != null)
                    {
                        // the implicit rule is now valid - clear the old error
                        UpdateValidationError(null);
                    }
                }
            }
            else if (validationStep == ValidationStep.CommittedValue)
            {
                if (result)
                {
                    NeedsValidation = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Run validation rules for the given step, and inform the binding group
        /// <summary>
        internal override bool CheckValidationRules(BindingGroup bindingGroup, ValidationStep validationStep)
        {
            if (!NeedsValidation)
                return true;

            object value;
            switch (validationStep)
            {
                case ValidationStep.RawProposedValue:
                    value = GetRawProposedValue();
                    break;
                case ValidationStep.ConvertedProposedValue:
                    value = bindingGroup.GetValue(this);
                    break;
                case ValidationStep.UpdatedValue:
                case ValidationStep.CommittedValue:
                    value = this;
                    break;
                default:
                    throw new InvalidOperationException(SR.Get(SRID.ValidationRule_UnknownStep, validationStep, bindingGroup));
            }

            return Validate(value, validationStep);
        }

        /// <summary>
        /// Get the proposed value(s) that would be written to the source(s), applying
        /// conversion and checking UI-side validation rules.
        /// </summary>
        internal override bool ValidateAndConvertProposedValue(out Collection<ProposedValue> values)
        {
            Debug.Assert(NeedsValidation, "check NeedsValidation before calling this");
            values = null;

            // validate raw proposed value
            object rawValue = GetRawProposedValue();

            bool isValid = Validate(rawValue, ValidationStep.RawProposedValue);

            // apply conversion and validate it
            object convertedValue = isValid ? ConvertProposedValue(rawValue)
                                            : DependencyProperty.UnsetValue;

            if (convertedValue == Binding.DoNothing)
            {
                convertedValue = DependencyProperty.UnsetValue;
            }
            else if (convertedValue == DependencyProperty.UnsetValue)
            {
                isValid = false;
            }
            else
            {
                isValid = Validate(convertedValue, ValidationStep.ConvertedProposedValue);
            }

            // return the result
            values = new Collection<ProposedValue>();
            values.Add(new ProposedValue(this, rawValue, convertedValue));
            return isValid;
        }

        private bool IsValidValueForUpdate(object value, Type sourceType)
        {
            // null is always valid, even for value types.  The reflection layer
            // apparently converts null to default(T).
            if (value == null)
                return true;

            // if direct assignment is possible, the value is valid
            if (sourceType.IsAssignableFrom(value.GetType()))
                return true;

            // if the value is DBNull, ask the worker (answer depends on several factors)
            if (Convert.IsDBNull(value))
                return Worker.IsDBNullValidForUpdate;

            // otherwise the value is invalid
            return false;
        }

        private void ProcessException(Exception ex, bool validate)
        {
            object filteredException = null;
            ValidationError validationError = null;

            // If there is not ExceptionFilter, then Wrap the
            // exception in a ValidationError.
            if (ExceptionFilterExists())
            {
                filteredException = CallDoFilterException(ex);

                if (filteredException == null)
                    return;

                validationError = filteredException as ValidationError;
            }

            // See if an ExceptionValidationRule is in effect
            if (validationError == null && validate)
            {
                ValidationRule exceptionValidationRule = ExceptionValidationRule.Instance;

                if (filteredException == null)
                {
                    validationError = new ValidationError(exceptionValidationRule, this, ex.Message, ex);
                }
                else
                {
                    validationError = new ValidationError(exceptionValidationRule, this, filteredException, ex);
                }
            }

            if (validationError != null)
            {
                UpdateValidationError(validationError);
            }
        }

        // Sometimes a source update gets requested as a result of event leapfrogging,
        // where the binding has not yet been informed of a pending change to its
        // path. In this case, we should simply ignore the update.
        private bool ShouldIgnoreUpdate()
        {
            // The detection algorithm below is expensive, so only do when the target
            // property is especially susceptible to the problem
            if (TargetProperty.OwnerType != typeof(System.Windows.Controls.Primitives.Selector) &&  // SelectedItem, SelectedValue, SelectedIndex, etc.
                TargetProperty != ComboBox.TextProperty                                             // ComboBox.Text
                )
            {
                return false;
            }

            // Re-evaluate the entire path.  If it doesn't produce the same source
            // object as our cached value, there is presumably a change notification
            // somewhere along the path that hasn't been delivered yet, due to event
            // leapfrogging.

            // get initial (top-level) item
            object item;
            DependencyObject contextElement = ContextElement;
            if (contextElement == null)
            {
                DependencyObject target = TargetElement;
                if (target != null && UsingMentor)
                {
                    target = Helper.FindMentor(target);
                }
                if (target == null)
                {
                    return true;
                }
                item = ParentBinding.SourceReference.GetDataObject(target,
                                                    new ObjectRefArgs() { ResolveNamesInTemplate = ResolveNamesInTemplate });
            }
            else
            {
                item = contextElement.GetValue(FrameworkElement.DataContextProperty);
            }

            // Apply auto-magic rules for CVS, DSP, etc.
            // This logic should agree with Activate(item), except for event hookup.
            if (!ParentBinding.BindsDirectlyToSource)
            {
                CollectionViewSource cvs;
                DataSourceProvider dsp;

                if ((cvs = item as CollectionViewSource) != null)
                {
                    item = cvs.CollectionView;
                }
                else if ((dsp = item as DataSourceProvider) != null)
                {
                    item = dsp.Data;
                }
            }

            // If the top-level item is different, ignore the update
            if (!System.Windows.Controls.ItemsControl.EqualsEx(DataItem, item))
                return true;

            // check the rest of the path
            return !Worker.IsPathCurrent();
        }
#endregion Value

#region INotifyDataErrorInfo

        // fetch the current data errors from the source object (and its value),
        // and update the list of data-error ValidationResults accordingly
        internal void UpdateNotifyDataErrors(INotifyDataErrorInfo indei, string propertyName, object value)
        {
            if (!ValidatesOnNotifyDataErrors || IsDetached)
                return;

            // replace the value object, if it has changed
            WeakReference dataErrorWR = (WeakReference)GetValue(Feature.DataErrorValue, null);
            INotifyDataErrorInfo dataErrorValue = (dataErrorWR == null) ? null : dataErrorWR.Target as INotifyDataErrorInfo;
            if (value != DependencyProperty.UnsetValue && value != dataErrorValue && IsDynamic)
            {
                if (dataErrorValue != null)
                    ErrorsChangedEventManager.RemoveHandler(dataErrorValue, OnErrorsChanged);

                INotifyDataErrorInfo newDataErrorValue = value as INotifyDataErrorInfo;
                object newDataErrorWR = ReplaceReference(dataErrorWR, newDataErrorValue);
                SetValue(Feature.DataErrorValue, newDataErrorWR, null);
                dataErrorValue = newDataErrorValue;

                if (newDataErrorValue != null)
                    ErrorsChangedEventManager.AddHandler(newDataErrorValue, OnErrorsChanged);
            }

            // fetch the errors from the last item and from its value
            IsDataErrorsChangedPending = false;
            try
            {
                List<object> propertyErrors = GetDataErrors(indei, propertyName);
                List<object> valueErrors = GetDataErrors(dataErrorValue, String.Empty);
                List<object> errors = MergeErrors(propertyErrors, valueErrors);

                UpdateNotifyDataErrorValidationErrors(errors);
            }
            catch (Exception ex)
            {   // if GetErrors or the enumerators throw, leave the old errors in place
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;
            }
        }

        void UpdateNotifyDataErrors(object value)
        {
            if (!ValidatesOnNotifyDataErrors)
                return;

            UpdateNotifyDataErrors(SourceItem as INotifyDataErrorInfo, SourcePropertyName, value);
        }

        // fetch errors for the given property
        internal static List<object> GetDataErrors(INotifyDataErrorInfo indei, string propertyName)
        {
            const int RetryCount = 3;
            List<object> result = null;
            if (indei != null && indei.HasErrors)
            {
                // if a worker thread is updating the source's errors while we're trying to
                // read them, the enumerator will throw.   The interface doesn't provide
                // any way to work around this, so we'll just try it a few times hoping
                // for success.
                for (int i=RetryCount; i>=0; --i)
                {
                    try
                    {
                        result = new List<object>();
                        IEnumerable ie = indei.GetErrors(propertyName);
                        if (ie != null)
                        {
                            foreach (object o in ie)
                            {
                                result.Add(o);
                            }
                        }
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        // on the last try, let the exception bubble up
                        if (i == 0)
                            throw;
                    }
                }
            }

            if (result != null && result.Count == 0)
                result = null;

            return result;
        }

        List<object> MergeErrors(List<object> list1, List<object> list2)
        {
            if (list1 == null)
                return list2;
            if (list2 == null)
                return list1;

            foreach (object o in list2)
                list1.Add(o);
            return list1;
        }

#endregion INotifyDataErrorInfo

#region Event handlers

        private void OnDataContextChanged(DependencyObject contextElement)
        {
            // ADO BindingExpressions change the data context when a field changes.
            // If the field is the one we're updating, ignore the DC change.
            if (!IsInUpdate && CanActivate)
            {
                if (IsReflective && RootBindingExpression.ParentBindingBase.BindingGroupName == String.Empty)
                {
                    RejoinBindingGroup(IsReflective, contextElement);
                }

                object newItem = contextElement.GetValue(FrameworkElement.DataContextProperty);
                if (!System.Windows.Controls.ItemsControl.EqualsEx(DataItem, newItem))
                {
                    Activate(newItem);
                }
            }
        }

        internal void OnCurrentChanged(object sender, EventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(this),
                                        "CurrentChanged",
                                        TraceData.Identify(sender)),
                                    this);
            }

            Worker.OnCurrentChanged(sender as ICollectionView, e);
        }

        internal void OnCurrentChanging(object sender, CurrentChangingEventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(this),
                                        "CurrentChanging",
                                        TraceData.Identify(sender)),
                                    this);
            }

            Update();
            // Consider Cancel if anything goes wrong.
        }

        void OnDataChanged(object sender, EventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(this),
                                        "DataChanged",
                                        TraceData.Identify(sender)),
                                    this);
            }
            Activate(sender);
        }

        void OnInheritanceContextChanged(object sender, EventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(this),
                                        "InheritanceContextChanged",
                                        TraceData.Identify(sender)),
                                    this);
            }

            if (StatusInternal == BindingStatusInternal.Unattached)
            {
                // retry bindings immediately when InheritanceContext changes,
                // so that triggers, animations, and rendering see the bound
                // value when they initialize their own local cache (bug DD 139838).
                Engine.CancelTask(this, TaskOps.AttachToContext);   // cancel existing task
                AttachToContext(AttachAttempt.Again);

                if (StatusInternal == BindingStatusInternal.Unattached)
                {
                    // if that didn't work, run the task again
                    // By adding a new task, the engine will add a LayoutUpdated handler
                    Engine.AddTask(this, TaskOps.AttachToContext);
                }
            }
            else
            {
                AttachToContext(AttachAttempt.Last);
            }
        }

        internal override void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(this),
                                        "LostFocus",
                                        TraceData.Identify(sender)),
                                    this);
            }

            Update();
        }

        void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            // if notification was on the right thread, just do the work (normal case)
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                UpdateNotifyDataErrors(DependencyProperty.UnsetValue);
            }
            else if (!IsDataErrorsChangedPending)
            {
                // otherwise invoke an operation to do the work on the right context
                IsDataErrorsChangedPending = true;
                Engine.Marshal(
                    (arg) => {  UpdateNotifyDataErrors(DependencyProperty.UnsetValue);
                                 return null; }, null);
            }
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

#endregion Event handlers

#region Helper functions


        //
        // If this BindingExpression's ParentBinding has an ExceptionFilter set,
        // call it, otherwise give the MultiBinding (if there is one)
        // a chance.
        //
        private object CallDoFilterException(Exception ex)
        {
            if (ParentBinding.UpdateSourceExceptionFilter != null)
            {
                return ParentBinding.DoFilterException(this, ex);
            }
            else if (IsInMultiBindingExpression)
            {
                return ParentMultiBindingExpression.ParentMultiBinding.DoFilterException(this, ex);
            }

            return null;
        }

        private bool ExceptionFilterExists()
        {
            return ( (ParentBinding.UpdateSourceExceptionFilter != null) ||
                     (IsInMultiBindingExpression && ParentMultiBindingExpression.ParentMultiBinding.UpdateSourceExceptionFilter != null)
                   );
        }


        // surround any code that changes the value of a BindingExpression by
        //      using (bindExpr.ChangingValue())
        //      { ... }
        internal IDisposable ChangingValue()
        {
            return new ChangingValueHelper(this);
        }

        // cancel any pending work
        internal void CancelPendingTasks()
        {
            Engine.CancelTasks(this);
        }

        // replace this BindingExpression with a new one
        void Replace()
        {
            DependencyObject target = TargetElement;
            if (target != null)
            {
                if (IsInBindingExpressionCollection)
                    ParentBindingExpressionBase.ReplaceChild(this);
                else
                    BindingOperations.SetBinding(target, TargetProperty, ParentBinding);
            }
        }

        // raise the TargetUpdated event (explicit polymorphism)
        internal static void OnTargetUpdated(DependencyObject d, DependencyProperty dp)
        {
            DataTransferEventArgs args = new DataTransferEventArgs(d, dp);
            args.RoutedEvent = Binding.TargetUpdatedEvent;
            FrameworkObject fo = new FrameworkObject(d);

            if (!fo.IsValid && d != null)
            {
                fo.Reset(Helper.FindMentor(d));
            }

            fo.RaiseEvent(args);
        }

        // raise the SourceUpdatedEvent event (explicit polymorphism)
        internal static void OnSourceUpdated(DependencyObject d, DependencyProperty dp)
        {
            DataTransferEventArgs args = new DataTransferEventArgs(d, dp);
            args.RoutedEvent = Binding.SourceUpdatedEvent;
            FrameworkObject fo = new FrameworkObject(d);

            if (!fo.IsValid && d != null)
            {
                fo.Reset(Helper.FindMentor(d));
            }

            fo.RaiseEvent(args);
        }

        internal override void HandlePropertyInvalidation(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            DependencyProperty dp = args.Property;

            if (TraceData.IsExtendedTraceEnabled(this, TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotPropertyChanged(
                                        TraceData.Identify(this),
                                        TraceData.Identify(d),
                                        dp.Name),
                                    this);
            }

            if (dp == FrameworkElement.DataContextProperty)
            {
                DependencyObject contextElement = ContextElement;
                if (d == contextElement)
                {
                    IsTransferPending = false;  // clear this flag, even if data context isn't changing
                    OnDataContextChanged(contextElement);
                }
            }

            if (dp == CollectionViewSource.ViewProperty)
            {
                CollectionViewSource cvs = this.CollectionViewSource;
                if (d == cvs)
                {
                    Activate(cvs);
                }
            }

            if (dp == FrameworkElement.LanguageProperty && UsesLanguage && d == TargetElement)
            {
                InvalidateCulture();
                TransferValue();
            }

            if (Worker != null)
            {
                Worker.OnSourceInvalidation(d, dp, args.IsASubPropertyChange);
            }
        }


        private class ChangingValueHelper : IDisposable
        {
            internal ChangingValueHelper(BindingExpression b)
            {
                _bindingExpression = b;
                b.CancelPendingTasks();
            }

            public void Dispose()
            {
                _bindingExpression.TransferValue();
                GC.SuppressFinalize(this);
            }

            BindingExpression _bindingExpression;
        }

        void SetDataItem(object newItem)
        {
            _dataItem = CreateReference(newItem);
        }

        // find the DataSource object (if any) that produced the DataContext
        // for element d
        object GetDataSourceForDataContext(DependencyObject d)
        {
            // look for ancestor that contributed the inherited value
            DependencyObject ancestor;
            BindingExpression b = null;

            for (ancestor = d;
                 ancestor != null;
                 ancestor = FrameworkElement.GetFrameworkParent(ancestor))
            {
                if (HasLocalDataContext(ancestor))
                {
                    b = BindingOperations.GetBindingExpression(ancestor, FrameworkElement.DataContextProperty) as BindingExpression;
                    break;
                }
            }

            if (b != null)
                return b.DataSource;

            return null;
        }

#endregion Helper functions

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        WeakReference           _ctxElement;
        object                  _dataItem;
        BindingWorker           _worker;
        Type                    _sourceType;

        internal static readonly object NullDataItem = new NamedObject("NullDataItem");
        internal static readonly object IgnoreDefaultValue = new NamedObject("IgnoreDefaultValue");
        internal static readonly object StaticSource = new NamedObject("StaticSource");
    }
}

