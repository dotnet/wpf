// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines PropertyPathWorker object, workhorse for CLR bindings
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;                      // Validation
using System.Windows.Data;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Hashing.PresentationFramework;    // HashHelper

namespace MS.Internal.Data
{
    internal sealed class PropertyPathWorker : IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal PropertyPathWorker(PropertyPath path)
            : this(path, DataBindEngine.CurrentDataBindEngine)
        {
        }

        internal PropertyPathWorker(PropertyPath path, ClrBindingWorker host, bool isDynamic, DataBindEngine engine)
            : this(path, engine)
        {
            _host = host;
            _isDynamic = isDynamic;
        }

        private PropertyPathWorker(PropertyPath path, DataBindEngine engine)
        {
            _parent = path;
            _arySVS = new SourceValueState[path.Length];
            _engine = engine;

            // initialize each level to NullDataItem, so that the first real
            // item will force a change
            for (int i = _arySVS.Length - 1; i >= 0; --i)
            {
                _arySVS[i].item = BindingExpression.CreateReference(BindingExpression.NullDataItem);
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal int Length { get { return _parent.Length; } }
        internal PropertyPathStatus Status { get { return _status; } }

        internal DependencyObject TreeContext
        {
            get { return BindingExpression.GetReference(_treeContext) as DependencyObject; }
            set { _treeContext = BindingExpression.CreateReference(value); }
        }

        internal void SetTreeContext(WeakReference wr)
        {
            _treeContext = BindingExpression.CreateReference(wr);
        }

        internal bool IsDBNullValidForUpdate
        {
            get
            {
                if (!_isDBNullValidForUpdate.HasValue)
                {
                    DetermineWhetherDBNullIsValid();
                }

                return _isDBNullValidForUpdate.Value;
            }
        }

        internal object SourceItem
        {
            get
            {
                int level = Length - 1;
                object item = (level >= 0) ? GetItem(level) : null;
                if (item == BindingExpression.NullDataItem)
                {
                    item = null;
                }

                return item;
            }
        }

        internal string SourcePropertyName
        {
            get
            {
                int level = Length - 1;

                if (level < 0)
                    return null;

                switch (SVI[level].type)
                {
                    case SourceValueType.Property:
                        // return the real name of the property
                        DependencyProperty dp;
                        PropertyInfo pi;
                        PropertyDescriptor pd;
                        DynamicPropertyAccessor dpa;

                        SetPropertyInfo(GetAccessor(level), out pi, out pd, out dp, out dpa);
                        return (dp != null) ? dp.Name :
                                (pi != null) ? pi.Name :
                                (pd != null) ? pd.Name :
                                (dpa != null) ? dpa.PropertyName : null;

                    case SourceValueType.Indexer:
                        // return the indexer string, e.g. "[foo]"
                        string s = _parent.Path;
                        int lastBracketIndex = s.LastIndexOf('[');
                        return s.Substring(lastBracketIndex);
                }

                // in all other cases, no name is available
                return null;
            }
        }

        // true when we need to register for direct notification from the RawValue,
        // i.e. when it's a DO that we get to via a non-DP
        internal bool NeedsDirectNotification
        {
            get { return _needsDirectNotification; }
            private set
            {
                if (value)
                {
                    _dependencySourcesChanged = true;
                }
                _needsDirectNotification = value;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //-------  common methods ------

        internal object GetItem(int level)
        {
            return BindingExpression.GetReference(_arySVS[level].item);
        }

        internal object GetAccessor(int level)
        {
            return _arySVS[level].info;
        }

        internal object[] GetIndexerArguments(int level)
        {
            object[] args = _arySVS[level].args;

            // unwrap the IList wrapper, if any
            IListIndexerArg wrapper;
            if (args != null && args.Length == 1 &&
                (wrapper = args[0] as IListIndexerArg) != null)
            {
                return new object[] { wrapper.Value };
            }

            return args;
        }

        internal Type GetType(int level)
        {
            return _arySVS[level].type;
        }

        //-------  target mode ------

        // Set the context for the path.  Use this method in "target" mode
        // to connect the path to a rootItem for a short time:
        //      using (path.SetContext(myItem))
        //      {
        //          ... call target-mode convenience methods ...
        //      }
        internal IDisposable SetContext(object rootItem)
        {
            if (_contextHelper == null)
                _contextHelper = new ContextHelper(this);

            _contextHelper.SetContext(rootItem);
            return _contextHelper;
        }

        //-------  source mode (should only be called by ClrBindingWorker) ------

        internal void AttachToRootItem(object rootItem)
        {
            _rootItem = BindingExpression.CreateReference(rootItem);
            UpdateSourceValueState(-1, null);
        }

        internal void DetachFromRootItem()
        {
            _rootItem = BindingExpression.NullDataItem;
            UpdateSourceValueState(-1, null);
            _rootItem = null;
        }

        internal object GetValue(object item, int level)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetValue);
            DependencyProperty dp;
            PropertyInfo pi;
            PropertyDescriptor pd;
            DynamicPropertyAccessor dpa;
            object value = DependencyProperty.UnsetValue;
            SetPropertyInfo(_arySVS[level].info, out pi, out pd, out dp, out dpa);

            switch (SVI[level].type)
            {
                case SourceValueType.Property:
                    if (pi != null)
                    {
                        value = pi.GetValue(item, null);
                    }
                    else if (pd != null)
                    {
                        bool indexerIsNext = (level + 1 < SVI.Length && SVI[level + 1].type == SourceValueType.Indexer);
                        value = Engine.GetValue(item, pd, indexerIsNext);
                    }
                    else if (dp != null)
                    {
                        DependencyObject d = (DependencyObject)item;
                        if (level != Length - 1 || _host == null || _host.TransfersDefaultValue)
                            value = d.GetValue(dp);
                        else if (!Helper.HasDefaultValue(d, dp))
                            value = d.GetValue(dp);
                        else
                            value = BindingExpression.IgnoreDefaultValue;
                    }
                    else if (dpa != null)
                    {
                        value = dpa.GetValue(item);
                    }
                    break;

                case SourceValueType.Indexer:
                    DynamicIndexerAccessor dia;
                    // how do we get an indexed value from a PropertyDescriptor?
                    if (pi != null)
                    {
                        object[] args = _arySVS[level].args;

                        IListIndexerArg wrapper;
                        if (args != null && args.Length == 1 &&
                            (wrapper = args[0] as IListIndexerArg) != null)
                        {
                            // common special case: IList indexer.  Avoid
                            // out-of-range exceptions.
                            int index = wrapper.Value;
                            IList ilist = (IList)item;

                            if (0 <= index && index < ilist.Count)
                            {
                                value = ilist[index];
                            }
                            else
                            {
                                value = IListIndexOutOfRange;
                            }
                        }
                        else
                        {
                            // normal case
                            value = pi.GetValue(item,
                                            BindingFlags.GetProperty, null,
                                            args,
                                            CultureInfo.InvariantCulture);
                        }
                    }
                    else if ((dia = _arySVS[level].info as DynamicIndexerAccessor) != null)
                    {
                        value = dia.GetValue(item, _arySVS[level].args);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Get(SRID.IndexedPropDescNotImplemented));
                    }
                    break;

                case SourceValueType.Direct:
                    value = item;
                    break;
            }

            if (isExtendedTraceEnabled)
            {
                object accessor = _arySVS[level].info;
                if (accessor == DependencyProperty.UnsetValue)
                    accessor = null;

                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GetValue(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        level,
                                        TraceData.Identify(item),
                                        TraceData.IdentifyAccessor(accessor),
                                        TraceData.Identify(value)),
                                    _host.ParentBindingExpression);
            }

            return value;
        }

        internal void SetValue(object item, object value)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetValue);
            PropertyInfo pi;
            PropertyDescriptor pd;
            DependencyProperty dp;
            DynamicPropertyAccessor dpa;
            int level = _arySVS.Length - 1;
            SetPropertyInfo(_arySVS[level].info, out pi, out pd, out dp, out dpa);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.SetValue(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        level,
                                        TraceData.Identify(item),
                                        TraceData.IdentifyAccessor(_arySVS[level].info),
                                        TraceData.Identify(value)),
                                    _host.ParentBindingExpression);
            }

            switch (SVI[level].type)
            {
                case SourceValueType.Property:
                    if (pd != null)
                    {
                        pd.SetValue(item, value);
                    }
                    else if (pi != null)
                    {
                        pi.SetValue(item, value, null);
                    }
                    else if (dp != null)
                    {
                        ((DependencyObject)item).SetValue(dp, value);
                    }
                    else if (dpa != null)
                    {
                        dpa.SetValue(item, value);
                    }
                    break;

                case SourceValueType.Indexer:
                    DynamicIndexerAccessor dia;
                    //  How do we set an indexed value on a PropertyDescriptor?
                    if (pi != null)
                    {
                        pi.SetValue(item, value,
                                        BindingFlags.SetProperty, null,
                                        GetIndexerArguments(level),
                                        CultureInfo.InvariantCulture);
                    }
                    else if ((dia = _arySVS[level].info as DynamicIndexerAccessor) != null)
                    {
                        dia.SetValue(item, _arySVS[level].args, value);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Get(SRID.IndexedPropDescNotImplemented));
                    }
                    break;
            }
        }

        internal object RawValue()
        {
            object rawValue = RawValue(Length - 1);

            if (rawValue == AsyncRequestPending)
                rawValue = DependencyProperty.UnsetValue;     // the real value will arrive later

            return rawValue;
        }

        // Called by BE.UpdateTarget().  Re-fetch the value at each level.
        // If there's a difference, simulate a property-change at that level.
        internal void RefreshValue()
        {
            for (int k = 1; k < _arySVS.Length; ++k)
            {
                object oldValue = BindingExpression.GetReference(_arySVS[k].item);
                if (!ItemsControl.EqualsEx(oldValue, RawValue(k - 1)))
                {
                    UpdateSourceValueState(k - 1, null);
                    return;
                }
            }

            UpdateSourceValueState(Length - 1, null);
        }

        // return the source level where the change happened, or -1 if the
        // change is irrelevant.
        internal int LevelForPropertyChange(object item, string propertyName)
        {
            // This test must be thread-safe - it can get called on the "wrong" context.
            // It's read-only (good).  And if another thread changes the values it reads,
            // the worst that can happen is to schedule a transfer operation needlessly -
            // the operation itself won't do anything (since the test is repeated on the
            // right thread).

            bool isIndexer = propertyName == Binding.IndexerName;

            for (int k = 0; k < _arySVS.Length; ++k)
            {
                object o = BindingExpression.GetReference(_arySVS[k].item);
                if (o == BindingExpression.StaticSource)
                    o = null;

                if (o == item &&
                        (String.IsNullOrEmpty(propertyName) ||
                         (isIndexer && SVI[k].type == MS.Internal.Data.SourceValueType.Indexer) ||
                         String.Equals(SVI[k].propertyName, propertyName, StringComparison.OrdinalIgnoreCase)))
                {
                    return k;
                }
            }

            return -1;
        }

        internal void OnPropertyChangedAtLevel(int level)
        {
            UpdateSourceValueState(level, null);
        }

        internal void OnCurrentChanged(ICollectionView collectionView)
        {
            for (int k = 0; k < Length; ++k)
            {
                if (_arySVS[k].collectionView == collectionView)
                {
                    _host.CancelPendingTasks();

                    // update everything below that level
                    UpdateSourceValueState(k, collectionView);
                    break;
                }
            }
        }

        // determine if a source invalidation is relevant.
        // This method must be thread-safe - it can be called on any Dispatcher.
        internal bool UsesDependencyProperty(DependencyObject d, DependencyProperty dp)
        {
            if (dp == DependencyObject.DirectDependencyProperty)
            {
                // the only way we get notified about this property is when we
                // ask for it.
                return true;
            }

            // find the source level where the change happened
            for (int k = 0; k < _arySVS.Length; ++k)
            {
                if ((_arySVS[k].info == dp) && (BindingExpression.GetReference(_arySVS[k].item) == d))
                {
                    return true;
                }
            }

            return false;
        }

        internal void OnDependencyPropertyChanged(DependencyObject d, DependencyProperty dp, bool isASubPropertyChange)
        {
            if (dp == DependencyObject.DirectDependencyProperty)
            {
                // the only way we get notified about this property is when the raw
                // value reports a subProperty change.
                UpdateSourceValueState(_arySVS.Length, null, BindingExpression.NullDataItem, isASubPropertyChange);
                return;
            }

            // find the source level where the change happened
            int k;
            for (k = 0; k < _arySVS.Length; ++k)
            {
                if ((_arySVS[k].info == dp) && (BindingExpression.GetReference(_arySVS[k].item) == d))
                {
                    // update everything below that level
                    UpdateSourceValueState(k, null, BindingExpression.NullDataItem, isASubPropertyChange);
                    break;
                }
            }
        }

        internal void OnNewValue(int level, object value)
        {
            // optimistically assume the new value will fix previous path errors
            _status = PropertyPathStatus.Active;
            if (level < Length - 1)
                UpdateSourceValueState(level, null, value, false);
        }

        // for error reporting only
        internal SourceValueInfo GetSourceValueInfo(int level)
        {
            return SVI[level];
        }

        internal static bool IsIndexedProperty(PropertyInfo pi)
        {
            bool result = false;

            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
#pragma warning disable 56500
            try
            {
                result = (pi != null) && pi.GetIndexParameters().Length > 0;
            }
            catch (Exception ex)
            {
                // if the PropertyInfo throws an exception, treat it as non-indexed
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;
            }

#pragma warning restore 56500
#pragma warning restore 1634, 1691

            return result;
        }


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        bool IsDynamic { get { return _isDynamic; } }
        SourceValueInfo[] SVI { get { return _parent.SVI; } }
        DataBindEngine Engine { get { return _engine; } }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // fill in the SourceValueState with updated infomation, starting at level k+1.
        // If view isn't null, also update the current item at level k.
        private void UpdateSourceValueState(int k, ICollectionView collectionView)
        {
            UpdateSourceValueState(k, collectionView, BindingExpression.NullDataItem, false);
        }

        // fill in the SourceValueState with updated infomation, starting at level k+1.
        // If view isn't null, also update the current item at level k.
        private void UpdateSourceValueState(int k, ICollectionView collectionView, object newValue, bool isASubPropertyChange)
        {
            // give host a chance to shut down the binding if the target has
            // gone away
            DependencyObject target = null;
            if (_host != null)
            {
                target = _host.CheckTarget();
                if (_rootItem != BindingExpression.NullDataItem && target == null)
                    return;
            }

            int initialLevel = k;
            object rawValue = null;

            // don't do a data transfer if there's no one to do it, or if this is
            // a (re-)activation with a new source item.   In the latter case,
            // the host binding decides whether to do a transfer.
            bool suppressTransfer = (_host == null) || (k < 0);

            // optimistically assume the new value will fix previous path errors
            _status = PropertyPathStatus.Active;

            // prepare to collect changes to dependency sources
            _dependencySourcesChanged = false;

            // Update the current item at level k, if requested
            if (collectionView != null)
            {
                Debug.Assert(0 <= k && k < _arySVS.Length && _arySVS[k].collectionView == collectionView, "bad parameters to UpdateSourceValueState");
                ReplaceItem(k, collectionView.CurrentItem, NoParent);
            }

            // update the remaining levels
            for (++k; k < _arySVS.Length; ++k)
            {
                isASubPropertyChange = false;   // sub-property changes only matter at the last level

                ICollectionView oldCollectionView = _arySVS[k].collectionView;

                // replace the item at level k using parent from level k-1
                rawValue = (newValue == BindingExpression.NullDataItem) ? RawValue(k - 1) : newValue;
                newValue = BindingExpression.NullDataItem;
                if (rawValue == AsyncRequestPending)
                {
                    _status = PropertyPathStatus.AsyncRequestPending;
                    break;      // we'll resume the loop after the request completes
                }
                else if (!suppressTransfer &&
                         rawValue == BindingExpressionBase.DisconnectedItem &&
                         _arySVS[k - 1].info == FrameworkElement.DataContextProperty)
                {
                    // don't transfer if {DisconnectedItem} shows up on the path 
                    suppressTransfer = true;
                }

                ReplaceItem(k, BindingExpression.NullDataItem, rawValue);

                // replace view, if necessary
                ICollectionView newCollectionView = _arySVS[k].collectionView;
                if (oldCollectionView != newCollectionView && _host != null)
                {
                    _host.ReplaceCurrentItem(oldCollectionView, newCollectionView);
                }
            }

            // notify binding about what happened
            if (_host != null)
            {
                if (initialLevel < _arySVS.Length)
                {
                    // when something in the path changes, recompute whether we
                    // need direct notifications from the raw value
                    NeedsDirectNotification = _status == PropertyPathStatus.Active &&
                            _arySVS.Length > 0 &&
                            SVI[_arySVS.Length - 1].type != SourceValueType.Direct &&
                            !(_arySVS[_arySVS.Length - 1].info is DependencyProperty) &&
                            typeof(DependencyObject).IsAssignableFrom(_arySVS[_arySVS.Length - 1].type);
                }

                if (!suppressTransfer && _arySVS.Length > 0 &&
                    _arySVS[_arySVS.Length - 1].info == FrameworkElement.DataContextProperty &&
                    RawValue() == BindingExpressionBase.DisconnectedItem)
                {
                    // don't transfer if {DisconnectedItem} is the final value 
                    suppressTransfer = true;
                }

                _host.NewValueAvailable(_dependencySourcesChanged, suppressTransfer, isASubPropertyChange);
            }

            GC.KeepAlive(target);   // keep target alive during changes (bug 956831)
        }

        // replace the item at level k with the given item, or with an item obtained from the given parent
        private void ReplaceItem(int k, object newO, object parent)
        {
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.ReplaceItem);
            SourceValueState svs = new SourceValueState();

            object oldO = BindingExpression.GetReference(_arySVS[k].item);

            // stop listening to old item
            if (IsDynamic && SVI[k].type != SourceValueType.Direct)
            {
                INotifyPropertyChanged oldPC;
                DependencyProperty oldDP;
                PropertyInfo oldPI;
                PropertyDescriptor oldPD;
                DynamicObjectAccessor oldDOA;
                PropertyPath.DowncastAccessor(_arySVS[k].info, out oldDP, out oldPI, out oldPD, out oldDOA);

                if (oldO == BindingExpression.StaticSource)
                {
                    Type declaringType = (oldPI != null) ? oldPI.DeclaringType
                                        : (oldPD != null) ? oldPD.ComponentType
                                        : null;
                    if (declaringType != null)
                    {
                        StaticPropertyChangedEventManager.RemoveHandler(declaringType, OnStaticPropertyChanged, SVI[k].propertyName);
                    }
                }
                else if (oldDP != null)
                {
                    _dependencySourcesChanged = true;
                }
                else if ((oldPC = oldO as INotifyPropertyChanged) != null)
                {
                    PropertyChangedEventManager.RemoveHandler(oldPC, OnPropertyChanged, SVI[k].propertyName);
                }
                else if (oldPD != null && oldO != null)
                {
                    ValueChangedEventManager.RemoveHandler(oldO, OnValueChanged, oldPD);
                }
            }

            // extra work at the last level
            if (_host != null && k == Length - 1)
            {
                // handle INotifyDataErrorInfo
                if (IsDynamic && _host.ValidatesOnNotifyDataErrors)
                {
                    INotifyDataErrorInfo indei = oldO as INotifyDataErrorInfo;
                    if (indei != null)
                    {
                        ErrorsChangedEventManager.RemoveHandler(indei, OnErrorsChanged);
                    }
                }
            }

            // clear the IsDBNullValid cache
            _isDBNullValidForUpdate = null;

            if (newO == null ||
                parent == DependencyProperty.UnsetValue ||
                parent == BindingExpression.NullDataItem ||
                parent == BindingExpressionBase.DisconnectedItem)
            {
                _arySVS[k].item = BindingExpression.ReplaceReference(_arySVS[k].item, newO);

                if (parent == DependencyProperty.UnsetValue ||
                    parent == BindingExpression.NullDataItem ||
                    parent == BindingExpressionBase.DisconnectedItem)
                {
                    _arySVS[k].collectionView = null;
                }

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.ReplaceItemShort(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k,
                                            TraceData.Identify(newO)),
                                        _host.ParentBindingExpression);
                }

                return;
            }

            // obtain the new item and its access info
            if (newO != BindingExpression.NullDataItem)
            {
                parent = newO;              // used by error reporting
                GetInfo(k, newO, ref svs);
                svs.collectionView = _arySVS[k].collectionView;
            }
            else
            {
                // Note: if we want to support binding to HasValue and/or Value
                // properties of nullable types, we need a way to find out if
                // the rawvalue is Nullable and pass that information here.

                DrillIn drillIn = SVI[k].drillIn;
                ICollectionView view = null;

                // first look for info on the parent
                if (drillIn != DrillIn.Always)
                {
                    GetInfo(k, parent, ref svs);
                }

                // if that fails, look for information on the view itself
                if (svs.info == null)
                {
                    view = CollectionViewSource.GetDefaultCollectionView(parent, TreeContext,
                        (x) =>
                        {
                            return BindingExpression.GetReference((k == 0) ? _rootItem : _arySVS[k - 1].item);
                        });

                    if (view != null && drillIn != DrillIn.Always)
                    {
                        if (view != parent)             // don't duplicate work
                            GetInfo(k, view, ref svs);
                    }
                }

                // if that fails, drill in to the current item
                if (svs.info == null && drillIn != DrillIn.Never && view != null)
                {
                    newO = view.CurrentItem;
                    if (newO != null)
                    {
                        GetInfo(k, newO, ref svs);
                        svs.collectionView = view;
                    }
                    else
                    {
                        // no current item: use previous info (if known)
                        svs = _arySVS[k];
                        svs.collectionView = view;

                        // if there's no current item because parent is an empty
                        // XmlDataCollection, treat it as a path error (the XPath
                        // didn't return any nodes)
                        if (!SystemXmlHelper.IsEmptyXmlDataCollection(parent))
                        {
                            // otherwise it's not an error - currency is simply
                            // off the collection
                            svs.item = BindingExpression.ReplaceReference(svs.item, BindingExpression.NullDataItem);
                            if (svs.info == null)
                                svs.info = DependencyProperty.UnsetValue;
                        }
                    }
                }
            }

            // update info about new item
            if (svs.info == null)
            {
                svs.item = BindingExpression.ReplaceReference(svs.item, BindingExpression.NullDataItem);
                _arySVS[k] = svs;
                _status = PropertyPathStatus.PathError;
                ReportNoInfoError(k, parent);
                return;
            }

            _arySVS[k] = svs;
            newO = BindingExpression.GetReference(svs.item);

            if (isExtendedTraceEnabled)
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.ReplaceItemLong(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        k,
                                        TraceData.Identify(newO),
                                        TraceData.IdentifyAccessor(svs.info)),
                                    _host.ParentBindingExpression);
            }

            // start listening to new item
            if (IsDynamic && SVI[k].type != SourceValueType.Direct)
            {
                Engine.RegisterForCacheChanges(newO, svs.info);

                INotifyPropertyChanged newPC;
                DependencyProperty newDP;
                PropertyInfo newPI;
                PropertyDescriptor newPD;
                DynamicObjectAccessor newDOA;
                PropertyPath.DowncastAccessor(svs.info, out newDP, out newPI, out newPD, out newDOA);

                if (newO == BindingExpression.StaticSource)
                {
                    Type declaringType = (newPI != null) ? newPI.DeclaringType
                                        : (newPD != null) ? newPD.ComponentType
                                        : null;
                    if (declaringType != null)
                    {
                        StaticPropertyChangedEventManager.AddHandler(declaringType, OnStaticPropertyChanged, SVI[k].propertyName);
                    }
                }
                else if (newDP != null)
                {
                    _dependencySourcesChanged = true;
                }
                else if ((newPC = newO as INotifyPropertyChanged) != null)
                {
                    PropertyChangedEventManager.AddHandler(newPC, OnPropertyChanged, SVI[k].propertyName);
                }
                else if (newPD != null && newO != null)
                {
                    ValueChangedEventManager.AddHandler(newO, OnValueChanged, newPD);
                }
            }

            // extra work at the last level
            if (_host != null && k == Length - 1)
            {
                // set up the default transformer
                _host.SetupDefaultValueConverter(svs.type);

                // check for request to update a read-only property
                if (_host.IsReflective)
                {
                    CheckReadOnly(newO, svs.info);
                }

                // handle INotifyDataErrorInfo
                if (_host.ValidatesOnNotifyDataErrors)
                {
                    INotifyDataErrorInfo indei = newO as INotifyDataErrorInfo;
                    if (indei != null)
                    {
                        if (IsDynamic)
                        {
                            ErrorsChangedEventManager.AddHandler(indei, OnErrorsChanged);
                        }

                        _host.OnDataErrorsChanged(indei, SourcePropertyName);
                    }
                }
            }
        }

        void ReportNoInfoError(int k, object parent)
        {
            // report cannot find info.  Ignore when in priority bindings.
            if (TraceData.IsEnabled)
            {
                BindingExpression bindingExpression = (_host != null) ? _host.ParentBindingExpression : null;
                if (bindingExpression == null || !bindingExpression.IsInPriorityBindingExpression)
                {
                    if (!SystemXmlHelper.IsEmptyXmlDataCollection(parent))
                    {
                        SourceValueInfo svi = SVI[k];
                        bool inCollection = (svi.drillIn == DrillIn.Always);
                        string cs = (svi.type != SourceValueType.Indexer) ? svi.name : "[" + svi.name + "]";
                        string ps = TraceData.DescribeSourceObject(parent);
                        string os = inCollection ? "current item of collection" : "object";

                        // if the parent is null, the path error probably only means the
                        // data provider hasn't produced any data yet.  When it does,
                        // the binding will try again and probably succeed.  Give milder
                        // feedback for this special case, so as not to alarm users unduly.
                        if (parent == null)
                        {
                            TraceData.TraceAndNotify(TraceEventType.Information, TraceData.NullItem(cs, os), bindingExpression);
                        }
                        // Similarly, if the parent is the NewItemPlaceholder.
                        else if (parent == CollectionView.NewItemPlaceholder ||
                                parent == DataGrid.NewItemPlaceholder)
                        {
                            TraceData.TraceAndNotify(TraceEventType.Information, TraceData.PlaceholderItem(cs, os), bindingExpression);
                        }
                        else
                        {
                            TraceEventType traceType = (bindingExpression != null) ? bindingExpression.TraceLevel : TraceEventType.Error;
                            TraceData.TraceAndNotify(traceType, TraceData.ClrReplaceItem(cs, ps, os), bindingExpression,
                                traceParameters: new object[] { bindingExpression },
                                eventParameters: new object[] { cs, parent, inCollection });
                        }
                    }
                    else
                    {
                        TraceEventType traceType = (bindingExpression != null) ? bindingExpression.TraceLevel : TraceEventType.Error;
                        _host.ReportBadXPath(traceType);
                    }
                }
            }
        }

        // determine if the cached state of the path is still correct.  This is
        // used to deduce whether event leapfrogging has occurred along the path
        // (i.e. something changed, but we haven't yet received the notification)
        internal bool IsPathCurrent(object rootItem)
        {
            if (Status != PropertyPathStatus.Active)
                return false;

            object item = rootItem;
            for (int level = 0, n = Length; level < n; ++level)
            {
                ICollectionView view = _arySVS[level].collectionView;
                if (view != null)
                {
                    item = view.CurrentItem;
                }

                if (PropertyPath.IsStaticProperty(_arySVS[level].info))
                {
                    // for static properties, we set svs.item to StaticSource
                    // at discovery time.  Do the same here. 
                    item = BindingExpression.StaticSource;
                }

                if (!ItemsControl.EqualsEx(item, BindingExpression.GetReference(_arySVS[level].item))
                    && !IsNonIdempotentProperty(level - 1))
                {
                    return false;
                }

                if (level < n - 1)
                {
                    item = GetValue(item, level);
                }
            }

            return true;
        }

        // Certain properties are known to be non-idempotent, i.e. they return a
        // different value every time the getter is called.   For the purpose of
        // detecting event leapfrogging, the value produced by such a property
        // should be ignored.
        bool IsNonIdempotentProperty(int level)
        {
            PropertyDescriptor pd;
            if (level < 0 || (pd = _arySVS[level].info as PropertyDescriptor) == null)
                return false;

            return SystemXmlLinqHelper.IsXLinqNonIdempotentProperty(pd);
        }

        // look for property/indexer on the given item
        private void GetInfo(int k, object item, ref SourceValueState svs)
        {
#if DEBUG
            bool checkCacheResult = false;
#endif
            object oldItem = BindingExpression.GetReference(_arySVS[k].item);
            bool isExtendedTraceEnabled = IsExtendedTraceEnabled(TraceDataLevel.GetInfo);

            // optimization - only change info if the type changed
            // exception - if the info is a PropertyDescriptor, it might depend
            // on the item itself (not just the type), so we have to re-fetch
            Type oldType = ReflectionHelper.GetReflectionType(oldItem);
            Type newType = ReflectionHelper.GetReflectionType(item);
            Type sourceType = null;

            if (newType == oldType && oldItem != BindingExpression.NullDataItem &&
                !(_arySVS[k].info is PropertyDescriptor))
            {
                svs = _arySVS[k];
                svs.item = BindingExpression.ReplaceReference(svs.item, item);

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.GetInfo_Reuse(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k,
                                            TraceData.IdentifyAccessor(svs.info)),
                                        _host.ParentBindingExpression);
                }
                return;
            }

            // if the new item is null, we won't find a property/indexer on it
            if (newType == null && SVI[k].type != SourceValueType.Direct)
            {
                svs.info = null;
                svs.args = null;
                svs.type = null;
                svs.item = BindingExpression.ReplaceReference(svs.item, item);

                if (isExtendedTraceEnabled)
                {
                    TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                        TraceData.GetInfo_Null(
                                            TraceData.Identify(_host.ParentBindingExpression),
                                            k),
                                        _host.ParentBindingExpression);
                }
                return;
            }

            // optimization - see if we've cached the answer
            int index;
            bool cacheAccessor = !PropertyPath.IsParameterIndex(SVI[k].name, out index);
            if (cacheAccessor)
            {
                AccessorInfo accessorInfo = Engine.AccessorTable[SVI[k].type, newType, SVI[k].name];
                if (accessorInfo != null)
                {
                    svs.info = accessorInfo.Accessor;
                    svs.type = accessorInfo.PropertyType;
                    svs.args = accessorInfo.Args;

                    if (PropertyPath.IsStaticProperty(svs.info))
                        item = BindingExpression.StaticSource;

                    svs.item = BindingExpression.ReplaceReference(svs.item, item);

                    if (IsDynamic && SVI[k].type == SourceValueType.Property && svs.info is DependencyProperty)
                    {
                        _dependencySourcesChanged = true;
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Cache(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(svs.info)),
                                            _host.ParentBindingExpression);
                    }

#if DEBUG   // compute the answer the old-fashioned way, and compare
                    checkCacheResult = true;
#else
                    return;
#endif
                }
            }

            object info = null;
            object[] args = null;

            switch (SVI[k].type)
            {
                case SourceValueType.Property:
                    info = _parent.ResolvePropertyName(k, item, newType, TreeContext);

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Property(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(info)),
                                            _host.ParentBindingExpression);
                    }

                    DependencyProperty dp;
                    PropertyInfo pi1;
                    PropertyDescriptor pd;
                    DynamicObjectAccessor doa;
                    PropertyPath.DowncastAccessor(info, out dp, out pi1, out pd, out doa);

                    if (dp != null)
                    {
                        sourceType = dp.PropertyType;
                        if (IsDynamic)
                        {
#if DEBUG
                        if (checkCacheResult)
                            Debug.Assert(_dependencySourcesChanged, "Cached accessor didn't change sources");
#endif
                            _dependencySourcesChanged = true;
                        }
                        break;
                    }
                    else if (pi1 != null)
                    {
                        sourceType = pi1.PropertyType;
                    }
                    else if (pd != null)
                    {
                        sourceType = pd.PropertyType;
                    }
                    else if (doa != null)
                    {
                        sourceType = doa.PropertyType;
#if DEBUG
                    checkCacheResult = false;      // not relevant for dynamic objects
#endif
                    }
                    break;

                case SourceValueType.Indexer:
                    IndexerParameterInfo[] aryInfo = _parent.ResolveIndexerParams(k, TreeContext);

                    // Check if we should treat the indexer as a property instead.
                    // (See ShouldConvertIndexerToProperty for why we might do that.)
                    if (aryInfo.Length == 1 &&
                        (aryInfo[0].type == null || aryInfo[0].type == typeof(string)))
                    {
                        string name = (string)aryInfo[0].value;
                        if (ShouldConvertIndexerToProperty(item, ref name))
                        {
                            _parent.ReplaceIndexerByProperty(k, name);
                            goto case SourceValueType.Property;
                        }
                    }

                    args = new object[aryInfo.Length];

                    // find the matching indexer
                    MemberInfo[][] aryMembers = new MemberInfo[][] { GetIndexers(newType, k), null };
                    bool isIList = (item is IList);
                    if (isIList)
                        aryMembers[1] = typeof(IList).GetDefaultMembers();

                    for (int ii = 0; info == null && ii < aryMembers.Length; ++ii)
                    {
                        if (aryMembers[ii] == null)
                            continue;

                        MemberInfo[] defaultMembers = aryMembers[ii];

                        for (int jj = 0; jj < defaultMembers.Length; ++jj)
                        {
                            PropertyInfo pi = defaultMembers[jj] as PropertyInfo;
                            if (pi != null)
                            {
                                if (MatchIndexerParameters(pi, aryInfo, args, isIList))
                                {
                                    info = pi;
                                    sourceType = newType.GetElementType();
                                    if (sourceType == null)
                                        sourceType = pi.PropertyType;
                                    break;
                                }
                            }
                        }
                    }

                    if (info == null && SystemCoreHelper.IsIDynamicMetaObjectProvider(item))
                    {
                        if (MatchIndexerParameters(null, aryInfo, args, false))
                        {
                            info = SystemCoreHelper.GetIndexerAccessor(args.Length);
                            sourceType = typeof(Object);
                        }
                    }

                    if (isExtendedTraceEnabled)
                    {
                        TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                            TraceData.GetInfo_Indexer(
                                                TraceData.Identify(_host.ParentBindingExpression),
                                                k,
                                                newType.Name,
                                                SVI[k].name,
                                                TraceData.IdentifyAccessor(info)),
                                            _host.ParentBindingExpression);
                    }

                    break;

                case SourceValueType.Direct:
                    if (!(item is ICollectionView) || _host == null || _host.IsValidValue(item))
                    {
                        info = DependencyProperty.UnsetValue;
                        sourceType = newType;

                        if (Length == 1 &&
                                item is Freezable &&    // subproperty notifications only arise from Freezables
                                item != TreeContext)    // avoid self-loops
                        {
                            info = DependencyObject.DirectDependencyProperty;
                            _dependencySourcesChanged = true;
                        }
                    }
                    break;
            }

#if DEBUG
            if (checkCacheResult)
            {
                StringBuilder sb = new StringBuilder();

                if (!Object.Equals(info, svs.info))
                    sb.AppendLine(String.Format("  Info is wrong: expected '{0}' got '{1}'",
                                    info, svs.info));

                if (sourceType != svs.type)
                    sb.AppendLine(String.Format("  Type is wrong: expected '{0}' got '{1}'",
                                    sourceType, svs.type));

                if (item != BindingExpression.GetReference(svs.item))
                    sb.AppendLine(String.Format("  Item is wrong: expected '{0}' got '{1}'",
                                    item, BindingExpression.GetReference(svs.item)));

                int len1 = (args != null) ? args.Length : 0;
                int len2 = (svs.args != null) ? svs.args.Length : 0;
                if (len1 == len2)
                {
                    for (int i=0; i<len1; ++i)
                    {
                        if (!Object.Equals(args[i], svs.args[i]))
                        {
                            sb.AppendLine(String.Format("  args[{0}] is wrong:  expected '{1}' got '{2}'",
                                    i, args[i], svs.args[i]));
                        }
                    }
                }
                else
                    sb.AppendLine(String.Format("  Args are wrong: expected length '{0}' got length '{1}'",
                                    len1, len2));

                if (sb.Length > 0)
                {
                    Debug.Assert(false,
                        String.Format("Accessor cache returned incorrect result for ({0},{1},{2})\n{3}",
                            SVI[k].type, newType.Name, SVI[k].name, sb.ToString()));
                }

                return;
            }
#endif
            if (PropertyPath.IsStaticProperty(info))
                item = BindingExpression.StaticSource;

            svs.info = info;
            svs.args = args;
            svs.type = sourceType;
            svs.item = BindingExpression.ReplaceReference(svs.item, item);

            // cache the answer, to avoid doing all that reflection again
            // (but not if the answer is a PropertyDescriptor,
            // since then the answer potentially depends on the item itself)
            if (cacheAccessor && info != null && !(info is PropertyDescriptor))
            {
                Engine.AccessorTable[SVI[k].type, newType, SVI[k].name] =
                            new AccessorInfo(info, sourceType, args);
            }
        }

        // get indexers declared by the given type, for the path component at level k
        private MemberInfo[] GetIndexers(Type type, int k)
        {
            if (k > 0 && _arySVS[k - 1].info == (object)IndexerPropertyInfo.Instance)
            {
                // if the previous path component discovered a named indexed property,
                // return all the matches for the name
                List<MemberInfo> list = new List<MemberInfo>();
                string name = SVI[k - 1].name;
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                // we enumerate through all properties, rather than call GetProperty(name),
                // to avoid AmbiguousMatchExceptions when there are multiple overloads
                foreach (PropertyInfo pi in properties)
                {
                    if (pi.Name == name && IsIndexedProperty(pi))
                    {
                        list.Add(pi);
                    }
                }

                return list.ToArray();
            }
            else
            {
                // C#-style indexed property - GetDefaultMembers does what we need
                return type.GetDefaultMembers();
            }
        }

        // convert the (string) argument names to types appropriate for use with
        // the given property.  Put the results in the args[] array.  Return
        // true if everything works.
        private bool MatchIndexerParameters(PropertyInfo pi, IndexerParameterInfo[] aryInfo, object[] args, bool isIList)
        {
            ParameterInfo[] aryPI = pi?.GetIndexParameters();

            // must have the right number of parameters
            if (aryPI != null && aryPI.Length != aryInfo.Length)
                return false;

            // each parameter must be settable from user-specified type or from a string
            for (int i = 0; i < args.Length; ++i)
            {
                IndexerParameterInfo pInfo = aryInfo[i];
                Type paramType = (aryPI != null) ? aryPI[i].ParameterType : typeof(Object);
                if (pInfo.type != null)
                {
                    // Check that a user-specified type is compatible with the parameter type
                    if (paramType.IsAssignableFrom(pInfo.type))
                    {
                        args.SetValue(pInfo.value, i);
                        continue;
                    }
                    else
                        return false;
                }

                // PreSharp uses message numbers that the C# compiler doesn't know about.
                // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

                // PreSharp complains about catching NullReference (and other) exceptions.
                // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
#pragma warning disable 56500

                try
                {
                    object arg = null;

                    if (paramType == typeof(int))
                    {
                        // common case is paramType = Int32.  Use TryParse - this
                        // avoids expensive exceptions if it fails
                        int argInt;
                        if (Int32.TryParse((string)pInfo.value,
                                    NumberStyles.Integer,
                                    TypeConverterHelper.InvariantEnglishUS.NumberFormat,
                                    out argInt))
                        {
                            arg = argInt;
                        }
                    }
                    else
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(paramType);
                        if (tc != null && tc.CanConvertFrom(typeof(string)))
                        {
                            arg = tc.ConvertFromString(null, TypeConverterHelper.InvariantEnglishUS,
                                                            (string)pInfo.value);
                            // technically the converter can return null as a legitimate
                            // value.  In practice, this seems always to be a sign that
                            // the conversion didn't work (often because the converter
                            // reverts to the default behavior - returning null).  So
                            // we treat null as an "error", and keep trying for something
                            // better.  (See bug 861966)
                        }
                    }

                    if (arg == null && paramType.IsAssignableFrom(typeof(string)))
                    {
                        arg = pInfo.value;
                    }

                    if (arg != null)
                        args.SetValue(arg, i);
                    else
                        return false;
                }

                // catch all exceptions.  We simply want to move on to the next
                // candidate indexer.
                catch (Exception ex)
                {
                    if (CriticalExceptions.IsCriticalApplicationException(ex))
                        throw;
                    return false;
                }
                catch
                {
                    return false;
                }

#pragma warning restore 56500
#pragma warning restore 1634, 1691
            }

            // common case is IList - one arg of type Int32.  Wrap the arg so
            // that we can treat it specially in Get/SetValue.
            if (isIList && aryPI.Length == 1 && aryPI[0].ParameterType == typeof(int))
            {
                // only wrap when the property returns the same value as IList.Item[]
                bool shouldWrap = true;

                // .Net 4.0-4.7.2 always wrapped, but this hides more-derived
                // indexers that don't agree with IList, e.g. the indexer
                // provided by KeyedCollection<int,TItem>
                // The app-context switch maintains compat.
                if (!FrameworkAppContextSwitches.IListIndexerHidesCustomIndexer)
                {
                    // We can't recognize such properties in general, but we can
                    // recognize the most common cases - properties declared by .Net
                    // types on a whitelist.
                    Type type = pi.DeclaringType;
                    if (type.IsGenericType)
                    {
                        type = type.GetGenericTypeDefinition();
                    }
                    shouldWrap = IListIndexerWhitelist.Contains(type);
                }

                if (shouldWrap)
                {
                    args[0] = new IListIndexerArg((int)args[0]);
                }
            }

            return true;
        }

        private bool ShouldConvertIndexerToProperty(object item, ref string name)
        {
            // Special case for ADO.  If the path specifies an indexer on a DataRowView,
            // and if the DRV exposes a property with the same name as the indexer
            // argument, use the property instead.  (E.g. convert [foo] to .foo)
            // This works around a problem in ADO - they raise PropertyChanged for
            // property "foo", but they don't raise PropertyChanged for "Item[]".
            // See bug 1180454.
            // Likewise when the indexer arg is an integer - convert to the corresponding named property.
            if (SystemDataHelper.IsDataRowView(item))
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(item);
                if (properties[name] != null)
                    return true;

                int index;
                if (Int32.TryParse(name,
                                    NumberStyles.Integer,
                                    TypeConverterHelper.InvariantEnglishUS.NumberFormat,
                                    out index))
                {
                    if (0 <= index && index < properties.Count)
                    {
                        name = properties[index].Name;
                        return true;
                    }
                }
            }

            return false;
        }

        // return the raw value from level k
        private object RawValue(int k)
        {
            if (k < 0)
                return BindingExpression.GetReference(_rootItem);
            if (k >= _arySVS.Length)
                return DependencyProperty.UnsetValue;

            object item = BindingExpression.GetReference(_arySVS[k].item);
            object info = _arySVS[k].info;

            // try to get the value, unless (a) binding is being detached,
            // (b) no info - e.g. Nullable with no value, or (c) item expected
            // but not present - e.g. currency moved off the end.
            if (item != BindingExpression.NullDataItem && info != null && !(item == null && info != DependencyProperty.UnsetValue))
            {
                object o = DependencyProperty.UnsetValue;
                DependencyProperty dp = info as DependencyProperty;

                // if the binding is async, post a request to get the value
                if (!(dp != null || SVI[k].type == SourceValueType.Direct))
                {
                    if (_host != null && _host.AsyncGet(item, k))
                    {
                        _status = PropertyPathStatus.AsyncRequestPending;
                        return AsyncRequestPending;
                    }
                }

                // PreSharp uses message numbers that the C# compiler doesn't know about.
                // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

                // PreSharp complains about catching NullReference (and other) exceptions.
                // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
#pragma warning disable 56500

                try
                {
                    o = GetValue(item, k);
                }
                // Catch all exceptions.  There is no app code on the stack,
                // so the exception isn't actionable by the app.
                // Yet we don't want to crash the app.
                catch (Exception ex)    // if error getting value, we will use fallback/default instead
                {
                    if (CriticalExceptions.IsCriticalApplicationException(ex))
                        throw;
                    BindingOperations.LogException(ex);
                    if (_host != null)
                        _host.ReportGetValueError(k, item, ex);
                }
                catch // non CLS compliant exception
                {
                    if (_host != null)
                        _host.ReportGetValueError(k, item, new InvalidOperationException(SR.Get(SRID.NonCLSException, "GetValue")));
                }

                // catch the pseudo-exception as well
                if (o == IListIndexOutOfRange)
                {
                    o = DependencyProperty.UnsetValue;
                    if (_host != null)
                        _host.ReportGetValueError(k, item, new ArgumentOutOfRangeException("index"));
                }

#pragma warning restore 56500
#pragma warning restore 1634, 1691

                return o;
            }

            if (_host != null)
            {
                _host.ReportRawValueErrors(k, item, info);
            }

            return DependencyProperty.UnsetValue;
        }

        void SetPropertyInfo(object info, out PropertyInfo pi, out PropertyDescriptor pd, out DependencyProperty dp, out DynamicPropertyAccessor dpa)
        {
            pi = null;
            pd = null;
            dpa = null;
            dp = info as DependencyProperty;

            if (dp == null)
            {
                pi = info as PropertyInfo;
                if (pi == null)
                {
                    pd = info as PropertyDescriptor;

                    if (pd == null)
                        dpa = info as DynamicPropertyAccessor;
                }
            }
        }

        void CheckReadOnly(object item, object info)
        {
            PropertyInfo pi;
            PropertyDescriptor pd;
            DependencyProperty dp;
            DynamicPropertyAccessor dpa;
            SetPropertyInfo(info, out pi, out pd, out dp, out dpa);

            if (pi != null)
            {
                if (IsPropertyReadOnly(item, pi))
                    throw new InvalidOperationException(SR.Get(SRID.CannotWriteToReadOnly, item.GetType(), pi.Name));
            }
            else if (pd != null)
            {
                if (pd.IsReadOnly)
                    throw new InvalidOperationException(SR.Get(SRID.CannotWriteToReadOnly, item.GetType(), pd.Name));
            }
            else if (dp != null)
            {
                if (dp.ReadOnly)
                    throw new InvalidOperationException(SR.Get(SRID.CannotWriteToReadOnly, item.GetType(), dp.Name));
            }
            else if (dpa != null)
            {
                if (dpa.IsReadOnly)
                    throw new InvalidOperationException(SR.Get(SRID.CannotWriteToReadOnly, item.GetType(), dpa.PropertyName));
            }
        }

        bool IsPropertyReadOnly(object item, PropertyInfo pi)
        {
            // Custom properties obtained from ICustomTypeProvider often don't
            // implement all the methods we call below.  In those cases, we catch
            // the (arbitrary) exception they throw, and just use the result of
            // CanWrite.

            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
#pragma warning disable 1634, 1691

            // PreSharp complains about catching NullReference (and other) exceptions.
            // It doesn't recognize that IsCritical[Application]Exception() handles these correctly.
#pragma warning disable 56500

            // CanWrite says whether we're even allowed to call SetValue
            // (there's no try-block here - if a custom property doesn't even
            // implement CanWrite, we just fail).
            if (!pi.CanWrite)
                return true;

            // most properties implement SetValue by calling the setter.  Get it.
            MethodInfo setter = null;
            try
            {
                setter = pi.GetSetMethod(true);  // 'true' means get a non-public setter
            }
            catch (Exception ex2)
            {
                if (CriticalExceptions.IsCriticalApplicationException(ex2))
                    throw;
            }

            if (setter == null || setter.IsPublic)
            {
                // If there's no setter at all (including the case where GetSetMethod fails),
                // SetValue presumably works some other way.  For example,
                // custom properties from ICustomTypeProvider don't have
                // setter methods; instead SetValue modifies a property bag or
                // changes a table or the like.  In this case, there's no other
                // way to get information about this property, so we go with the
                // pi.CanWrite and treat the property as writeable.
                //
                // And if the setter is public, also treat the property as writeable.
                return false;
            }

            // if we get here, the property has CanWrite=true, and a non-public
            // setter. Returning true causes the caller to throw.
            return true;

#pragma warning restore 56500
#pragma warning restore 1634, 1691
        }

        // see whether DBNull is a valid value for update, and cache the answer
        void DetermineWhetherDBNullIsValid()
        {
            bool result = false;
            object item = GetItem(Length - 1);

            if (item != null && AssemblyHelper.IsLoaded(UncommonAssembly.System_Data_Common))
            {
                result = DetermineWhetherDBNullIsValid(item);
            }

            _isDBNullValidForUpdate = result;
        }

        bool DetermineWhetherDBNullIsValid(object item)
        {
            PropertyInfo pi;
            PropertyDescriptor pd;
            DependencyProperty dp;
            DynamicPropertyAccessor dpa;
            SetPropertyInfo(_arySVS[Length - 1].info, out pi, out pd, out dp, out dpa);

            string columnName = (pd != null) ? pd.Name :
                                (pi != null) ? pi.Name : null;

            object arg = (columnName == "Item" && pi != null) ? _arySVS[Length - 1].args[0] : null;

            return SystemDataHelper.DetermineWhetherDBNullIsValid(item, columnName, arg);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsExtendedTraceEnabled(TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        "PropertyChanged",
                                        TraceData.Identify(sender)),
                                    _host.ParentBindingExpression);
            }

            _host.OnSourcePropertyChanged(sender, e.PropertyName);
        }

        void OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (IsExtendedTraceEnabled(TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        "ValueChanged",
                                        TraceData.Identify(sender)),
                                    _host.ParentBindingExpression);
            }

            _host.OnSourcePropertyChanged(sender, e.PropertyDescriptor.Name);
        }

        void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            if (e.PropertyName == SourcePropertyName)
            {
                _host.OnDataErrorsChanged((INotifyDataErrorInfo)sender, e.PropertyName);
            }
        }

        void OnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsExtendedTraceEnabled(TraceDataLevel.Events))
            {
                TraceData.TraceAndNotifyWithNoParameters(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(_host.ParentBindingExpression),
                                        "PropertyChanged",
                                        "(static)"),
                                    _host.ParentBindingExpression);
            }

            _host.OnSourcePropertyChanged(sender, e.PropertyName);
        }

        bool IsExtendedTraceEnabled(TraceDataLevel level)
        {
            if (_host != null)
            {
                return TraceData.IsExtendedTraceEnabled(_host.ParentBindingExpression, level);
            }
            else
            {
                return false;
            }
        }

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        // helper for setting context via the "using" pattern
        class ContextHelper : IDisposable
        {
            PropertyPathWorker _owner;

            public ContextHelper(PropertyPathWorker owner)
            {
                _owner = owner;
            }

            public void SetContext(object rootItem)
            {
                _owner.TreeContext = rootItem as DependencyObject;
                _owner.AttachToRootItem(rootItem);
            }

            void IDisposable.Dispose()
            {
                _owner.DetachFromRootItem();
                _owner.TreeContext = null;
                GC.SuppressFinalize(this);
            }
        }

        // wrapper for arguments to IList indexer
        class IListIndexerArg
        {
            public IListIndexerArg(int arg)
            {
                _arg = arg;
            }

            public int Value { get { return _arg; } }

            int _arg;
        }

        //------------------------------------------------------
        //
        //  Private Enums, Structs, Constants
        //
        //------------------------------------------------------

        struct SourceValueState
        {
            public ICollectionView collectionView;
            public object item;
            public object info;             // PropertyInfo or PropertyDescriptor or DP
            public Type type;               // Type of the value (useful for Arrays)
            public object[] args;           // for indexers
        }

        static readonly Char[] s_comma = new Char[] { ',' };
        static readonly Char[] s_dot = new Char[] { '.' };

        static readonly object NoParent = new NamedObject("NoParent");
        static readonly object AsyncRequestPending = new NamedObject("AsyncRequestPending");
        internal static readonly object IListIndexOutOfRange = new NamedObject("IListIndexOutOfRange");

        // a list of types that declare indexers known to be consistent
        // with IList.Item[int index].  It is safe to replace these indexers
        // with the IList one.
        static readonly IList<Type> IListIndexerWhitelist = new Type[]
        {
            typeof(System.Collections.ArrayList),
            typeof(System.Collections.IList),
            typeof(System.Collections.Generic.List<>),
     // not typeof(System.Collections.Generic.SynchronizedCollection), safe but not worth adding a reference to System.ServiceModel
            typeof(System.Collections.ObjectModel.Collection<>),
     // not typeof(System.Collections.ObjectModel.KeyedCollection), indexer for KeyedCollection<int,TItem> is *not* consistent with IList
            typeof(System.Collections.ObjectModel.ReadOnlyCollection<>),
            typeof(System.Collections.Specialized.StringCollection),
            typeof(System.Windows.Documents.LinkTargetCollection),
     // not typeof(other type-safe derived class of CollectionBase),  safe but not worth adding references
        };

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        PropertyPath _parent;
        PropertyPathStatus _status;
        object _treeContext;
        object _rootItem;
        SourceValueState[] _arySVS;
        ContextHelper _contextHelper;

        ClrBindingWorker _host;
        DataBindEngine _engine;

        bool _dependencySourcesChanged;
        bool _isDynamic;
        bool _needsDirectNotification;
        bool? _isDBNullValidForUpdate;
    }
}

