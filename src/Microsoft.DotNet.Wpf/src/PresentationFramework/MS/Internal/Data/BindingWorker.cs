// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines BindingWorker base class.
//

using System;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;


namespace MS.Internal.Data
{
    // Base class for binding workers.
    // Derived classes implement binding functionality depending on the
    // type of source, e.g.  ClrBindingWorker, XmlBindingWorker
    internal abstract class BindingWorker
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        static BindingWorker()
        {
            Debug.Assert((int)Feature.LastFeatureId <= 32, "UncommonValueTable supports only 32 Ids");
        }

        protected BindingWorker(BindingExpression b)
        {
            _bindingExpression = b;
        }

        //------------------------------------------------------
        //
        //  Internal properties - used by parent BindingExpression
        //
        //------------------------------------------------------

        internal virtual Type SourcePropertyType { get { return null; } }
        internal virtual bool CanUpdate { get { return false; } }
        internal BindingExpression ParentBindingExpression { get { return _bindingExpression; } }
        internal Type TargetPropertyType { get { return TargetProperty.PropertyType; } }
        internal virtual bool IsDBNullValidForUpdate { get { return false; } }
        internal virtual object SourceItem { get { return null; } }
        internal virtual string SourcePropertyName { get { return null; } }

        //------------------------------------------------------
        //
        //  Internal methods - used by parent BindingExpression
        //
        //------------------------------------------------------

        internal virtual void AttachDataItem() { }
        internal virtual void DetachDataItem() { }
        internal virtual void OnCurrentChanged(ICollectionView collectionView, EventArgs args) { }
        internal virtual object RawValue() { return null; }
        internal virtual void UpdateValue(object value) { }
        internal virtual void RefreshValue() { }
        internal virtual bool UsesDependencyProperty(DependencyObject d, DependencyProperty dp) { return false; }
        internal virtual void OnSourceInvalidation(DependencyObject d, DependencyProperty dp, bool isASubPropertyChange) { }
        internal virtual bool IsPathCurrent() { return true; }

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        protected Binding ParentBinding { get { return ParentBindingExpression.ParentBinding; } }

        protected bool IsDynamic { get { return ParentBindingExpression.IsDynamic; } }
        internal bool IsReflective { get { return ParentBindingExpression.IsReflective; } }
        protected bool IgnoreSourcePropertyChange { get { return ParentBindingExpression.IgnoreSourcePropertyChange; } }
        protected object DataItem { get { return ParentBindingExpression.DataItem; } }
        protected DependencyObject TargetElement { get { return ParentBindingExpression.TargetElement; } }
        protected DependencyProperty TargetProperty { get { return ParentBindingExpression.TargetProperty; } }
        protected DataBindEngine Engine { get { return ParentBindingExpression.Engine; } }
        protected Dispatcher Dispatcher { get { return ParentBindingExpression.Dispatcher; } }

        protected BindingStatusInternal Status
        {
            get { return ParentBindingExpression.StatusInternal; }
            set { ParentBindingExpression.SetStatus(value); }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        protected void SetTransferIsPending(bool value)
        {
            ParentBindingExpression.IsTransferPending = value;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        BindingExpression _bindingExpression;

        #region Uncommon Values

        internal enum Feature
        {
            // ClrBindingWorker
            XmlWorker,
            PendingGetValueRequest,
            PendingSetValueRequest,

            // XmlBindingWorker

            // Sentinel, for error checking.   Must be last.
            LastFeatureId
        }

        internal bool HasValue(Feature id) { return _values.HasValue((int)id); }
        internal object GetValue(Feature id, object defaultValue) { return _values.GetValue((int)id, defaultValue); }
        internal void SetValue(Feature id, object value) { _values.SetValue((int)id, value); }
        internal void SetValue(Feature id, object value, object defaultValue) { if (Object.Equals(value, defaultValue)) _values.ClearValue((int)id); else _values.SetValue((int)id, value); }
        internal void ClearValue(Feature id) { _values.ClearValue((int)id); }
        UncommonValueTable _values;

        #endregion Uncommon Values
    }
}
