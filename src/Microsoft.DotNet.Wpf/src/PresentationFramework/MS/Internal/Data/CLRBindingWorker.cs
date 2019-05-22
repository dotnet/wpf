// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines ClrBindingWorker object, workhorse for CLR bindings
//

using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Windows.Threading;
using System.Threading;

using System.ComponentModel;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls; // Validation
using System.Windows.Data;
using System.Windows.Markup;     // for GetTypeFromName
using MS.Internal.Controls; // Validation
using MS.Internal.Utility;              // for GetTypeFromName
using MS.Utility;

namespace MS.Internal.Data
{
    internal class ClrBindingWorker : BindingWorker
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal ClrBindingWorker(BindingExpression b, DataBindEngine engine) : base(b)
        {
            PropertyPath path = ParentBinding.Path;

            if (ParentBinding.XPath != null)
            {
                path = PrepareXmlBinding(path);
            }

            if (path == null)
            {
                path = new PropertyPath(String.Empty);
            }

            if (ParentBinding.Path == null)
            {
                ParentBinding.UsePath(path);
            }

            _pathWorker = new PropertyPathWorker(path, this, IsDynamic, engine);
            _pathWorker.SetTreeContext(ParentBindingExpression.TargetElementReference);
        }

        // separate method to avoid loading System.Xml if not needed
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        PropertyPath PrepareXmlBinding(PropertyPath path)
        {
            if (path == null)
            {
                DependencyProperty targetDP = TargetProperty;
                Type targetType = targetDP.PropertyType;
                string pathString;

                if (targetType == typeof(Object))
                {
                    if (targetDP == System.Windows.Data.BindingExpression.NoTargetProperty ||
                        targetDP == System.Windows.Controls.Primitives.Selector.SelectedValueProperty ||
                        targetDP.OwnerType == typeof(LiveShapingList)
                        )
                    {
                        // these properties want the "value" - i.e. the text of
                        // the first (and usually only) XmlNode
                        pathString = "/InnerText";
                    }
                    else if (targetDP == FrameworkElement.DataContextProperty ||
                              targetDP == CollectionViewSource.SourceProperty)
                    {
                        // these properties want the entire collection
                        pathString = String.Empty;
                    }
                    else
                    {
                        // most object-valued properties want the (current) XmlNode itself
                        pathString = "/";
                    }
                }
                else if (targetType.IsAssignableFrom(typeof(XmlDataCollection)))
                {
                    // these properties want the entire collection
                    pathString = String.Empty;
                }
                else
                {
                    // most other properties want the "value"
                    pathString = "/InnerText";
                }

                path = new PropertyPath(pathString);
            }

            // don't bother to create XmlWorker if we don't even have a valid path
            if (path.SVI.Length > 0)
            {
                // tell Xml Worker if desired result is collection, in order to get optimization
                SetValue(Feature.XmlWorker, new XmlBindingWorker(this, path.SVI[0].drillIn == DrillIn.Never));
            }
            return path;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal override Type SourcePropertyType
        {
            get
            {
                return PW.GetType(PW.Length - 1);
            }
        }

        internal override bool IsDBNullValidForUpdate
        {
            get
            {
                return PW.IsDBNullValidForUpdate;
            }
        }

        internal override object SourceItem
        {
            get
            {
                return PW.SourceItem;
            }
        }

        internal override string SourcePropertyName
        {
            get
            {
                return PW.SourcePropertyName;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal override bool CanUpdate
        {
            get
            {
                PropertyPathWorker ppw = PW;
                int k = PW.Length - 1;

                if (k < 0)
                    return false;

                object item = ppw.GetItem(k);
                if (item == null || item == BindingExpression.NullDataItem)
                    return false;

                object accessor = ppw.GetAccessor(k);
                if (accessor == null ||
                    (accessor == DependencyProperty.UnsetValue && XmlWorker == null))
                    return false;

                return true;
            }
        }

        internal override void AttachDataItem()
        {
            object item;

            if (XmlWorker == null)
            {
                item = DataItem;
            }
            else
            {
                XmlWorker.AttachDataItem();
                item = XmlWorker.RawValue();
            }

            PW.AttachToRootItem(item);

            if (PW.Length == 0)
            {
                ParentBindingExpression.SetupDefaultValueConverter(item.GetType());
            }
        }

        internal override void DetachDataItem()
        {
            PW.DetachFromRootItem();
            if (XmlWorker != null)
            {
                XmlWorker.DetachDataItem();
            }

            // cancel any pending async requests.  If it has already completed,
            // but is now waiting in the dispatcher queue, it will be ignored because
            // we set _pending*Request to null.
            AsyncGetValueRequest pendingGetValueRequest = (AsyncGetValueRequest)GetValue(Feature.PendingGetValueRequest, null);
            if (pendingGetValueRequest != null)
            {
                pendingGetValueRequest.Cancel();
                ClearValue(Feature.PendingGetValueRequest);
            }

            AsyncSetValueRequest pendingSetValueRequest = (AsyncSetValueRequest)GetValue(Feature.PendingSetValueRequest, null);
            if (pendingSetValueRequest != null)
            {
                pendingSetValueRequest.Cancel();
                ClearValue(Feature.PendingSetValueRequest);
            }
        }

        internal override object RawValue()
        {
            object rawValue = PW.RawValue();
            SetStatus(PW.Status);

            return rawValue;
        }

        internal override void RefreshValue()
        {
            PW.RefreshValue();
        }

        internal override void UpdateValue(object value)
        {
            int k = PW.Length - 1;
            object item = PW.GetItem(k);
            if (item == null || item == BindingExpression.NullDataItem)
                return;

            // if the binding is async, post a request to set the value
            if (ParentBinding.IsAsync && !(PW.GetAccessor(k) is DependencyProperty))
            {
                RequestAsyncSetValue(item, value);
                return;
            }

            PW.SetValue(item, value);
        }

        internal override void OnCurrentChanged(ICollectionView collectionView, EventArgs args)
        {
            if (XmlWorker != null)
                XmlWorker.OnCurrentChanged(collectionView, args);
            PW.OnCurrentChanged(collectionView);
        }

        internal override bool UsesDependencyProperty(DependencyObject d, DependencyProperty dp)
        {
            return PW.UsesDependencyProperty(d, dp);
        }

        internal override void OnSourceInvalidation(DependencyObject d, DependencyProperty dp, bool isASubPropertyChange)
        {
            PW.OnDependencyPropertyChanged(d, dp, isASubPropertyChange);
        }

        internal override bool IsPathCurrent()
        {
            object item = (XmlWorker == null) ? DataItem : XmlWorker.RawValue();
            return PW.IsPathCurrent(item);
        }

        //------------------------------------------------------
        //
        //  Internal Properties - callbacks from PropertyPathWorker
        //
        //------------------------------------------------------

        internal bool TransfersDefaultValue
        {
            get { return ParentBinding.TransfersDefaultValue; }
        }

        internal bool ValidatesOnNotifyDataErrors
        {
            get { return ParentBindingExpression.ValidatesOnNotifyDataErrors; }
        }

        //------------------------------------------------------
        //
        //  Internal Methods - callbacks from PropertyPathWorker
        //
        //------------------------------------------------------

        internal void CancelPendingTasks()
        {
            ParentBindingExpression.CancelPendingTasks();
        }

        internal bool AsyncGet(object item, int level)
        {
            if (ParentBinding.IsAsync)
            {
                RequestAsyncGetValue(item, level);
                return true;
            }
            else
                return false;
        }

        internal void ReplaceCurrentItem(ICollectionView oldCollectionView, ICollectionView newCollectionView)
        {
            // detach from old view
            if (oldCollectionView != null)
            {
                CurrentChangedEventManager.RemoveHandler(oldCollectionView, ParentBindingExpression.OnCurrentChanged);
                if (IsReflective)
                {
                    CurrentChangingEventManager.RemoveHandler(oldCollectionView, ParentBindingExpression.OnCurrentChanging);
                }
            }

            // attach to new view
            if (newCollectionView != null)
            {
                CurrentChangedEventManager.AddHandler(newCollectionView, ParentBindingExpression.OnCurrentChanged);
                if (IsReflective)
                {
                    CurrentChangingEventManager.AddHandler(newCollectionView, ParentBindingExpression.OnCurrentChanging);
                }
            }
        }

        internal void NewValueAvailable(bool dependencySourcesChanged, bool initialValue, bool isASubPropertyChange)
        {
            SetStatus(PW.Status);

            BindingExpression parent = ParentBindingExpression;

            // this method is called when the last item in the path is replaced.
            // BindingGroup also wants to know about this.
            BindingGroup bindingGroup = parent.BindingGroup;
            if (bindingGroup != null)
            {
                bindingGroup.UpdateTable(parent);
            }

            if (dependencySourcesChanged)
            {
                ReplaceDependencySources();
            }

            if (Status != BindingStatusInternal.AsyncRequestPending)
            {
                // if there's a revised value (i.e. not during initialization
                // and shutdown), transfer it.
                if (!initialValue)
                {
                    parent.ScheduleTransfer(isASubPropertyChange);
                }
                else
                {
                    // "initialValue" should really be called "suppressTransfer".
                    // It's true when we don't want to transfer the new value:
                    //  a) initial activation
                    //  b) shutdown
                    //  c) DataContext={DisconnectedItem}
                    // Even in these cases, at least clear the pending flag,
                    // so that future source changes aren't ignored 
                    SetTransferIsPending(false);
                }
            }
        }

        internal void SetupDefaultValueConverter(Type type)
        {
            ParentBindingExpression.SetupDefaultValueConverter(type);
        }

        internal bool IsValidValue(object value)
        {
            return TargetProperty.IsValidValue(value);
        }

        internal void OnSourcePropertyChanged(object o, string propName)
        {
            int level;

            // ignore changes that don't affect this binding.
            // This test must come before any marshalling to the right context (bug 892484)
            if (!IgnoreSourcePropertyChange && (level = PW.LevelForPropertyChange(o, propName)) >= 0)
            {
                // if notification was on the right thread, just do the work (normal case)
                if (Dispatcher.Thread == Thread.CurrentThread)
                {
                    PW.OnPropertyChangedAtLevel(level);
                }
                else
                {
                    // otherwise invoke an operation to do the work on the right context
                    SetTransferIsPending(true);

                    if (ParentBindingExpression.TargetWantsCrossThreadNotifications)
                    {
                        LiveShapingItem lsi = TargetElement as LiveShapingItem;
                        if (lsi != null)
                        {
                            lsi.OnCrossThreadPropertyChange(TargetProperty);
                        }
                    }

                    Engine.Marshal(
                        new DispatcherOperationCallback(ScheduleTransferOperation),
                        null);
                }
            }
        }

        internal void OnDataErrorsChanged(INotifyDataErrorInfo indei, string propName)
        {
            // if notification was on the right thread, just do the work (normal case)
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                ParentBindingExpression.UpdateNotifyDataErrors(indei, propName, DependencyProperty.UnsetValue);
            }
            else if (!ParentBindingExpression.IsDataErrorsChangedPending)
            {
                // otherwise invoke an operation to do the work on the right context
                ParentBindingExpression.IsDataErrorsChangedPending = true;
                Engine.Marshal(
                    (arg) =>
                    {
                        object[] args = (object[])arg;
                        ParentBindingExpression.UpdateNotifyDataErrors((INotifyDataErrorInfo)args[0], (string)args[1], DependencyProperty.UnsetValue);
                        return null;
                    },
                    new object[] { indei, propName });
            }
        }

        // called by the child XmlBindingWorker when an xml change is detected
        // but the identity of raw value has not changed.
        internal void OnXmlValueChanged()
        {
            // treat this as a property change at the top level
            object item = PW.GetItem(0);
            OnSourcePropertyChanged(item, null);
        }

        // called by the child XmlBindingWorker when there's a new raw value
        internal void UseNewXmlItem(object item)
        {
            PW.DetachFromRootItem();
            PW.AttachToRootItem(item);
            if (Status != BindingStatusInternal.AsyncRequestPending)
            {
                ParentBindingExpression.ScheduleTransfer(false);
            }
        }

        // called by the child XmlBindingWorker to get the current "result node"
        internal object GetResultNode()
        {
            return PW.GetItem(0);
        }

        internal DependencyObject CheckTarget()
        {
            // if the target has been GC'd, this will shut down the binding
            return TargetElement;
        }

        internal void ReportGetValueError(int k, object item, Exception ex)
        {
            if (TraceData.IsEnabled)
            {
                SourceValueInfo svi = PW.GetSourceValueInfo(k);
                Type type = PW.GetType(k);
                string parentName = (k > 0) ? PW.GetSourceValueInfo(k - 1).name : String.Empty;
                TraceData.Trace(ParentBindingExpression.TraceLevel,
                        TraceData.CannotGetClrRawValue(
                            svi.propertyName, type.Name,
                            parentName, AvTrace.TypeName(item)),
                        ParentBindingExpression, ex);
            }
        }

        internal void ReportSetValueError(int k, object item, object value, Exception ex)
        {
            if (TraceData.IsEnabled)
            {
                SourceValueInfo svi = PW.GetSourceValueInfo(k);
                Type type = PW.GetType(k);
                TraceData.Trace(TraceEventType.Error,
                        TraceData.CannotSetClrRawValue(
                            svi.propertyName, type.Name,
                            AvTrace.TypeName(item),
                            AvTrace.ToStringHelper(value),
                            AvTrace.TypeName(value)),
                        ParentBindingExpression, ex);
            }
        }

        internal void ReportRawValueErrors(int k, object item, object info)
        {
            if (TraceData.IsEnabled)
            {
                if (item == null)
                {
                    // There is probably no data item; e.g. we've moved currency off of a list.
                    // the type of the missing item is supposed to be _arySVS[k].info.DeclaringType
                    // the property we're looking for is named _arySVS[k].name
                    TraceData.Trace(TraceEventType.Information, TraceData.MissingDataItem, ParentBindingExpression);
                }

                if (info == null)
                {
                    // this no info problem should have been error reported at ReplaceItem already.

                    // this can happen when parent is Nullable with no value
                    // check _arySVS[k-1].info.ComponentType
                    //if (!IsNullableType(_arySVS[k-1].info.ComponentType))
                    TraceData.Trace(TraceEventType.Information, TraceData.MissingInfo, ParentBindingExpression);
                }

                if (item == BindingExpression.NullDataItem)
                {
                    // this is OK, not an error.
                    // this can happen when detaching bindings.
                    // this can happen when binding has a Nullable data item with no value
                    TraceData.Trace(TraceEventType.Information, TraceData.NullDataItem, ParentBindingExpression);
                }
            }
        }

        internal void ReportBadXPath(TraceEventType traceType)
        {
            XmlBindingWorker xmlWorker = XmlWorker;
            if (xmlWorker != null)
            {
                xmlWorker.ReportBadXPath(traceType);
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        PropertyPathWorker PW { get { return _pathWorker; } }
        XmlBindingWorker XmlWorker { get { return (XmlBindingWorker)GetValue(Feature.XmlWorker, null); } }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        void SetStatus(PropertyPathStatus status)
        {
            switch (status)
            {
                case PropertyPathStatus.Inactive:
                    Status = BindingStatusInternal.Inactive;
                    break;
                case PropertyPathStatus.Active:
                    Status = BindingStatusInternal.Active;
                    break;
                case PropertyPathStatus.PathError:
                    Status = BindingStatusInternal.PathError;
                    break;
                case PropertyPathStatus.AsyncRequestPending:
                    Status = BindingStatusInternal.AsyncRequestPending;
                    break;
            }
        }

        void ReplaceDependencySources()
        {
            if (!ParentBindingExpression.IsDetaching)
            {
                int size = PW.Length;
                if (PW.NeedsDirectNotification)
                    ++size;

                WeakDependencySource[] newSources = new WeakDependencySource[size];
                int n = 0;

                if (IsDynamic)
                {
                    for (int k = 0; k < PW.Length; ++k)
                    {
                        DependencyProperty dp = PW.GetAccessor(k) as DependencyProperty;
                        if (dp != null)
                        {
                            DependencyObject d = PW.GetItem(k) as DependencyObject;
                            if (d != null)
                                newSources[n++] = new WeakDependencySource(d, dp);
                        }
                    }

                    if (PW.NeedsDirectNotification)
                    {
                        // subproperty notifications can only arise from Freezables
                        // (as of today - 11/14/08), so we only need to propagate
                        // them when the raw value is a Freezable.
                        DependencyObject d = PW.RawValue() as Freezable;
                        if (d != null)
                            newSources[n++] = new WeakDependencySource(d, DependencyObject.DirectDependencyProperty);
                    }
                }

                ParentBindingExpression.ChangeWorkerSources(newSources, n);
            }
        }

        #region Async

        void RequestAsyncGetValue(object item, int level)
        {
            // get information about the property whose value we want
            string name = GetNameFromInfo(PW.GetAccessor(level));
            Invariant.Assert(name != null, "Async GetValue expects a name");

            // abandon any previous request
            AsyncGetValueRequest pendingGetValueRequest = (AsyncGetValueRequest)GetValue(Feature.PendingGetValueRequest, null);
            if (pendingGetValueRequest != null)
            {
                pendingGetValueRequest.Cancel();
            }

            // issue the new request
            pendingGetValueRequest =
                new AsyncGetValueRequest(item, name, ParentBinding.AsyncState,
                                DoGetValueCallback, CompleteGetValueCallback,
                                this, level);
            SetValue(Feature.PendingGetValueRequest, pendingGetValueRequest);
            Engine.AddAsyncRequest(TargetElement, pendingGetValueRequest);
        }

        static object OnGetValueCallback(AsyncDataRequest adr)
        {
            AsyncGetValueRequest request = (AsyncGetValueRequest)adr;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];
            object value = worker.PW.GetValue(request.SourceItem, (int)request.Args[1]);
            if (value == PropertyPathWorker.IListIndexOutOfRange)
                throw new ArgumentOutOfRangeException("index");
            return value;
        }

        static object OnCompleteGetValueCallback(AsyncDataRequest adr)
        {
            AsyncGetValueRequest request = (AsyncGetValueRequest)adr;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];

            DataBindEngine engine = worker.Engine;
            if (engine != null) // could be null if binding has been detached
            {
                engine.Marshal(CompleteGetValueLocalCallback, request);
            }

            return null;
        }

        static object OnCompleteGetValueOperation(object arg)
        {
            AsyncGetValueRequest request = (AsyncGetValueRequest)arg;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];
            worker.CompleteGetValue(request);
            return null;
        }

        void CompleteGetValue(AsyncGetValueRequest request)
        {
            AsyncGetValueRequest pendingGetValueRequest = (AsyncGetValueRequest)GetValue(Feature.PendingGetValueRequest, null);
            if (pendingGetValueRequest == request)
            {
                ClearValue(Feature.PendingGetValueRequest);
                int k = (int)request.Args[1];

                // if the target has gone away, ignore the request
                if (CheckTarget() == null)
                    return;

                switch (request.Status)
                {
                    case AsyncRequestStatus.Completed:
                        PW.OnNewValue(k, request.Result);
                        SetStatus(PW.Status);
                        if (k == PW.Length - 1)
                            ParentBindingExpression.TransferValue(request.Result, false);
                        break;

                    case AsyncRequestStatus.Failed:
                        ReportGetValueError(k, request.SourceItem, request.Exception);
                        PW.OnNewValue(k, DependencyProperty.UnsetValue);
                        break;
                }
            }
        }


        void RequestAsyncSetValue(object item, object value)
        {
            // get information about the property whose value we want
            string name = GetNameFromInfo(PW.GetAccessor(PW.Length - 1));
            Invariant.Assert(name != null, "Async SetValue expects a name");

            // abandon any previous request
            AsyncSetValueRequest pendingSetValueRequest = (AsyncSetValueRequest)GetValue(Feature.PendingSetValueRequest, null);
            if (pendingSetValueRequest != null)
            {
                pendingSetValueRequest.Cancel();
            }

            // issue the new request
            pendingSetValueRequest =
                new AsyncSetValueRequest(item, name, value, ParentBinding.AsyncState,
                                DoSetValueCallback, CompleteSetValueCallback,
                                this);
            SetValue(Feature.PendingSetValueRequest, pendingSetValueRequest);
            Engine.AddAsyncRequest(TargetElement, pendingSetValueRequest);
        }

        static object OnSetValueCallback(AsyncDataRequest adr)
        {
            AsyncSetValueRequest request = (AsyncSetValueRequest)adr;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];
            worker.PW.SetValue(request.TargetItem, request.Value);
            return null;
        }

        static object OnCompleteSetValueCallback(AsyncDataRequest adr)
        {
            AsyncSetValueRequest request = (AsyncSetValueRequest)adr;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];

            DataBindEngine engine = worker.Engine;
            if (engine != null) // could be null if binding has been detached
            {
                engine.Marshal(CompleteSetValueLocalCallback, request);
            }

            return null;
        }

        static object OnCompleteSetValueOperation(object arg)
        {
            AsyncSetValueRequest request = (AsyncSetValueRequest)arg;
            ClrBindingWorker worker = (ClrBindingWorker)request.Args[0];
            worker.CompleteSetValue(request);
            return null;
        }

        void CompleteSetValue(AsyncSetValueRequest request)
        {
            AsyncSetValueRequest pendingSetValueRequest = (AsyncSetValueRequest)GetValue(Feature.PendingSetValueRequest, null);
            if (pendingSetValueRequest == request)
            {
                ClearValue(Feature.PendingSetValueRequest);

                // if the target has gone away, ignore the request
                if (CheckTarget() == null)
                    return;

                switch (request.Status)
                {
                    case AsyncRequestStatus.Completed:
                        break;
                    case AsyncRequestStatus.Failed:
                        object filteredException = ParentBinding.DoFilterException(ParentBindingExpression, request.Exception);
                        Exception exception = filteredException as Exception;
                        ValidationError validationError;

                        if (exception != null)
                        {
                            if (TraceData.IsEnabled)
                            {
                                int k = PW.Length - 1;
                                object value = request.Value;
                                ReportSetValueError(k, request.TargetItem, request.Value, exception);
                            }
                        }
                        else if ((validationError = filteredException as ValidationError) != null)
                        {
                            Validation.MarkInvalid(ParentBindingExpression, validationError);
                        }

                        break;
                }
            }
        }

        string GetNameFromInfo(object info)
        {
            MemberInfo mi;
            PropertyDescriptor pd;
            DynamicObjectAccessor doa;

            if ((mi = info as MemberInfo) != null)
                return mi.Name;

            if ((pd = info as PropertyDescriptor) != null)
                return pd.Name;

            if ((doa = info as DynamicObjectAccessor) != null)
                return doa.PropertyName;

            return null;
        }
        #endregion Async

        #region Callbacks

        private object ScheduleTransferOperation(object arg)
        {
            PW.RefreshValue();
            return null;
        }

        #endregion Callbacks

        //------------------------------------------------------
        //
        //  Private Enums, Structs, Constants
        //
        //------------------------------------------------------

        static readonly AsyncRequestCallback DoGetValueCallback = new AsyncRequestCallback(OnGetValueCallback);
        static readonly AsyncRequestCallback CompleteGetValueCallback = new AsyncRequestCallback(OnCompleteGetValueCallback);
        static readonly DispatcherOperationCallback CompleteGetValueLocalCallback = new DispatcherOperationCallback(OnCompleteGetValueOperation);
        static readonly AsyncRequestCallback DoSetValueCallback = new AsyncRequestCallback(OnSetValueCallback);
        static readonly AsyncRequestCallback CompleteSetValueCallback = new AsyncRequestCallback(OnCompleteSetValueCallback);
        static readonly DispatcherOperationCallback CompleteSetValueLocalCallback = new DispatcherOperationCallback(OnCompleteSetValueOperation);

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        PropertyPathWorker _pathWorker;
    }

    internal class WeakDependencySource
    {
        internal WeakDependencySource(DependencyObject item, DependencyProperty dp)
        {
            _item = BindingExpressionBase.CreateReference(item);
            _dp = dp;
        }

        internal WeakDependencySource(WeakReference wr, DependencyProperty dp)
        {
            _item = wr;
            _dp = dp;
        }

        internal DependencyObject DependencyObject { get { return (DependencyObject)BindingExpressionBase.GetReference(_item); } }
        internal DependencyProperty DependencyProperty { get { return _dp; } }

        object _item;
        DependencyProperty _dp;
    }
}
