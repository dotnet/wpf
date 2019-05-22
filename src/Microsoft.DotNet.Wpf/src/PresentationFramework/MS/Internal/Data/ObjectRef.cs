// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: ObjectRef is a general way to name objects used in data binding
//
// See spec at Data Binding.mht
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Utility;

namespace MS.Internal.Data
{
    #region ObjectRefArgs

    // args to GetObject and GetDataObject
    internal class ObjectRefArgs
    {
        internal bool IsTracing { get; set; }
        internal bool ResolveNamesInTemplate { get; set; }
        internal bool NameResolvedInOuterScope { get; set; }
    }

    #endregion ObjectRefArgs

    #region ObjectRef
    /// <summary> Abstract object reference. </summary>
    internal abstract class ObjectRef
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor is protected - you can only create subclasses. </summary>
        protected ObjectRef() { }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary> Returns the referenced object. </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        internal virtual object GetObject(DependencyObject d, ObjectRefArgs args)
        {
            return null;
        }

        /// <summary> Returns the data object associated with the referenced object.
        /// Often this is the same as the referenced object.
        /// </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        internal virtual object GetDataObject(DependencyObject d, ObjectRefArgs args)
        {
            return GetObject(d, args);
        }

        /// <summary> true if the ObjectRef really needs the tree context </summary>
        internal bool TreeContextIsRequired(DependencyObject target)
        {
            return ProtectedTreeContextIsRequired(target);
        }

        /// <summary> true if the ObjectRef really needs the tree context </summary>
        protected virtual bool ProtectedTreeContextIsRequired(DependencyObject target)
        {
            return false;
        }

        /// <summary>
        /// true if the ObjectRef uses the mentor of the target element,
        /// rather than the target element itself.
        /// </summary>
        internal bool UsesMentor
        {
            get { return ProtectedUsesMentor; }
        }

        /// <summary>
        /// true if the ObjectRef uses the mentor of the target element,
        /// rather than the target element itself.
        /// </summary>
        protected virtual bool ProtectedUsesMentor
        {
            get { return true; }
        }

        /// <summary>
        /// identify this ObjectRef to the user - used by extended tracing
        /// </summary>
        internal abstract string Identify();
    }

    #endregion ObjectRef

    #region ElementObjectRef
    /// <summary> Object reference to a DependencyObject via its Name. </summary>
    internal sealed class ElementObjectRef : ObjectRef
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor. </summary>
        /// <param name="name">Name of the referenced Element.</param>
        /// <exception cref="ArgumentNullException"> name is a null reference </exception>
        internal ElementObjectRef(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            _name = name.Trim();
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary> Returns the referenced object. </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        internal override object GetObject(DependencyObject d, ObjectRefArgs args)
        {
            if (d == null)
                throw new ArgumentNullException("d");

            object o = null;
            if (args.ResolveNamesInTemplate)
            {
                // look in container's template (if any) first
                FrameworkElement fe = d as FrameworkElement;
                if (fe != null && fe.TemplateInternal != null)
                {
                    o = Helper.FindNameInTemplate(_name, d);

                    if (args.IsTracing)
                    {
                        TraceData.Trace(TraceEventType.Warning,
                                            TraceData.ElementNameQueryTemplate(
                                                _name,
                                                TraceData.Identify(d)));
                    }
                }

                if (o == null)
                {
                    args.NameResolvedInOuterScope = true;
                }
            }

            FrameworkObject fo = new FrameworkObject(d);
            while (o == null && fo.DO != null)
            {
                DependencyObject scopeOwner;
                o = fo.FindName(_name, out scopeOwner);

                // if the original element is a scope owner, supports IComponentConnector,
                // and has a parent, don't use the result of FindName.  The
                // element is probably an instance of a Xaml-subclassed control;
                // we want to resolve the name starting in the next outer scope.
                // (bug 1669408)
                // Also, if the element's NavigationService property is locally
                // set, the element is the root of a navigation and should use the
                // inner scope (bug 1765041)
                if (d == scopeOwner && d is IComponentConnector &&
                    d.ReadLocalValue(System.Windows.Navigation.NavigationService.NavigationServiceProperty) == DependencyProperty.UnsetValue)
                {
                    DependencyObject parent = LogicalTreeHelper.GetParent(d);
                    if (parent == null)
                    {
                        parent = Helper.FindMentor(d.InheritanceContext);
                    }

                    if (parent != null)
                    {
                        o = null;
                        fo.Reset(parent);
                        continue;
                    }
                }

                if (args.IsTracing)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.ElementNameQuery(
                                            _name,
                                            TraceData.Identify(fo.DO)));
                }

                if (o == null)
                {
                    args.NameResolvedInOuterScope = true;

                    // move to the next outer namescope.
                    // First try TemplatedParent of the scope owner.
                    FrameworkObject foScopeOwner = new FrameworkObject(scopeOwner);
                    DependencyObject dd = foScopeOwner.TemplatedParent;

                    // if that doesn't work, we could be at the top of
                    // generated content for an ItemsControl.  If so, use
                    // the (visual) parent - a panel.
                    if (dd == null)
                    {
                        Panel panel = fo.FrameworkParent.DO as Panel;
                        if (panel != null && panel.IsItemsHost)
                        {
                            dd = panel;
                        }
                    }

                    // if the logical parent is a ContentControl whose content
                    // points right back, move to the ContentControl.   This is the
                    // moral equivalent of having the ContentControl as the TemplatedParent.
                    // (The InheritanceBehavior clause prevents this for cases where the
                    // parent ContentControl imposes a barrier, e.g. Frame)
                    if (dd == null && scopeOwner == null)
                    {
                        ContentControl cc = LogicalTreeHelper.GetParent(fo.DO) as ContentControl;
                        if (cc != null && cc.Content == fo.DO && cc.InheritanceBehavior == InheritanceBehavior.Default)
                        {
                            dd = cc;
                        }
                    }

                    // next, see if we're in a logical tree attached directly
                    // to a ContentPresenter.  This is the moral equivalent of
                    // having the ContentPresenter as the TemplatedParent.
                    if (dd == null && scopeOwner == null)
                    {
                        // go to the top of the logical subtree
                        DependencyObject parent;
                        for (dd = fo.DO; ;)
                        {
                            parent = LogicalTreeHelper.GetParent(dd);
                            if (parent == null)
                            {
                                parent = Helper.FindMentor(dd.InheritanceContext);
                            }

                            if (parent == null)
                                break;

                            dd = parent;
                        }

                        // if it's attached to a ContentPresenter, move to the CP
                        ContentPresenter cp = VisualTreeHelper.IsVisualType(dd) ? VisualTreeHelper.GetParent(dd) as ContentPresenter : null;
                        dd = (cp != null && cp.TemplateInternal.CanBuildVisualTree) ? cp : null;
                    }

                    fo.Reset(dd);
                }
            }

            if (o == null)
            {
                o = DependencyProperty.UnsetValue;
                args.NameResolvedInOuterScope = false;
            }

            return o;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,
                    "ElementName={0}", _name);
        }

        internal override string Identify()
        {
            return "ElementName";
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        string _name;
    }

    #endregion ElementObjectRef

    #region RelativeObjectRef
    /// <summary> Object reference relative to the target element.
    /// </summary>
    internal sealed class RelativeObjectRef : ObjectRef
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor. </summary>
        /// <param name="relativeSource">RelativeSource. </param>
        /// <exception cref="ArgumentNullException"> relativeSource is a null reference </exception>
        internal RelativeObjectRef(RelativeSource relativeSource)
        {
            if (relativeSource == null)
                throw new ArgumentNullException("relativeSource");

            _relativeSource = relativeSource;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        public override string ToString()
        {
            string s;
            switch (_relativeSource.Mode)
            {
                case RelativeSourceMode.FindAncestor:
                    s = String.Format(CultureInfo.InvariantCulture,
                        "RelativeSource {0}, AncestorType='{1}', AncestorLevel='{2}'",
                        _relativeSource.Mode,
                        _relativeSource.AncestorType,
                        _relativeSource.AncestorLevel);
                    break;
                default:
                    s = String.Format(CultureInfo.InvariantCulture,
                        "RelativeSource {0}", _relativeSource.Mode);
                    break;
            }

            return s;
        }

        /// <summary> Returns the referenced object. </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        /// <exception cref="ArgumentNullException"> d is a null reference </exception>
        internal override object GetObject(DependencyObject d, ObjectRefArgs args)
        {
            return GetDataObjectImpl(d, args);
        }

        /// <summary> Returns the data object associated with the referenced object.
        /// Often this is the same as the referenced object.
        /// </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        /// <exception cref="ArgumentNullException"> d is a null reference </exception>
        internal override object GetDataObject(DependencyObject d, ObjectRefArgs args)
        {
            object o = GetDataObjectImpl(d, args);
            DependencyObject el = o as DependencyObject;

            if (el != null && ReturnsDataContext)
            {
                // for generated wrappers, use the ItemForContainer property instead
                // of DataContext, since it's always set by the generator
                o = el.GetValue(ItemContainerGenerator.ItemForItemContainerProperty);
                if (o == null)
                    o = el.GetValue(FrameworkElement.DataContextProperty);
            }

            return o;
        }

        private object GetDataObjectImpl(DependencyObject d, ObjectRefArgs args)
        {
            if (d == null)
                return null;

            switch (_relativeSource.Mode)
            {
                case RelativeSourceMode.Self:
                    break;              // nothing to do

                case RelativeSourceMode.TemplatedParent:
                    d = Helper.GetTemplatedParent(d);
                    break;

                case RelativeSourceMode.PreviousData:
                    return GetPreviousData(d);

                case RelativeSourceMode.FindAncestor:
                    d = FindAncestorOfType(_relativeSource.AncestorType, _relativeSource.AncestorLevel, d, args.IsTracing);
                    if (d == null)
                    {
                        return DependencyProperty.UnsetValue;   // we fell off the tree
                    }
                    break;

                default:
                    return null;
            }

            if (args.IsTracing)
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.RelativeSource(
                                        _relativeSource.Mode,
                                        TraceData.Identify(d)));
            }

            return d;
        }

        internal bool ReturnsDataContext
        {
            get { return (_relativeSource.Mode == RelativeSourceMode.PreviousData); }
        }

        /// <summary> true if the ObjectRef really needs the tree context </summary>
        protected override bool ProtectedTreeContextIsRequired(DependencyObject target)
        {
            return ((_relativeSource.Mode == RelativeSourceMode.FindAncestor
                    || (_relativeSource.Mode == RelativeSourceMode.PreviousData)));
        }

        protected override bool ProtectedUsesMentor
        {
            get
            {
                switch (_relativeSource.Mode)
                {
                    case RelativeSourceMode.TemplatedParent:
                    case RelativeSourceMode.PreviousData:
                        return true;

                    default:
                        return false;
                }
            }
        }

        internal override string Identify()
        {
            return String.Format(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                "RelativeSource ({0})", _relativeSource.Mode);
        }

        //------------------------------------------------------
        //
        //  Private Method
        //
        //------------------------------------------------------

        private object GetPreviousData(DependencyObject d)
        {
            // move up to the next containing DataContext scope
            for (; d != null; d = FrameworkElement.GetFrameworkParent(d))
            {
                if (BindingExpression.HasLocalDataContext(d))
                {
                    // special case:  if the element is a ContentPresenter
                    // whose templated parent is a ContentControl or
                    // HeaderedItemsControl, and both have the same
                    // DataContext, we'll use the parent instead of the
                    // ContentPresenter.  In this case, the DataContext
                    // of the CP is set by various forwarding rules, and
                    // shouldn't count as a new scope.
                    // Similarly, do the same for a FE whose parent
                    // is a GridViewRowPresenter;  this enables Previous bindings
                    // inside ListView.
                    FrameworkElement parent, child;
                    ContentPresenter cp;

                    if ((cp = d as ContentPresenter) != null)
                    {
                        child = cp;
                        parent = cp.TemplatedParent as FrameworkElement;
                        if (!(parent is ContentControl || parent is HeaderedItemsControl))
                        {
                            parent = cp.Parent as System.Windows.Controls.Primitives.GridViewRowPresenterBase;
                        }
                    }
                    else
                    {
                        child = d as FrameworkElement;
                        parent = ((child != null) ? child.Parent : null) as System.Windows.Controls.Primitives.GridViewRowPresenterBase;
                    }

                    if (child != null && parent != null &&
                        ItemsControl.EqualsEx(child.DataContext, parent.DataContext))
                    {
                        d = parent;
                        if (!BindingExpression.HasLocalDataContext(parent))
                        {
                            continue;
                        }
                    }

                    break;
                }
            }

            if (d == null)
                return DependencyProperty.UnsetValue;   // we fell off the tree

            // this only makes sense within generated content.  If this
            // is the case, then d is now the wrapper element, its visual
            // parent is the layout element, and the layout's ItemsOwner
            // is the govening ItemsControl.
            Visual v = d as Visual;
            DependencyObject layout = (v != null) ? VisualTreeHelper.GetParent(v) : null;
            ItemsControl ic = ItemsControl.GetItemsOwner(layout);
            if (ic == null)
            {
                if (TraceData.IsEnabled)
                    TraceData.Trace(TraceEventType.Error, TraceData.RefPreviousNotInContext);
                return null;
            }

            // now look up the wrapper's previous sibling within the
            // layout's children collection
            Visual v2 = layout as Visual;
            int count = (v2 != null) ? v2.InternalVisualChildrenCount : 0;
            int j = -1;
            Visual prevChild = null;   //child at j-1th index
            if (count != 0)
            {
                j = IndexOf(v2, v, out prevChild);
            }
            if (j > 0)
            {
                d = prevChild;
            }
            else
            {
                d = null;
                if ((j < 0) && TraceData.IsEnabled)
                    TraceData.Trace(TraceEventType.Error, TraceData.RefNoWrapperInChildren);
            }
            return d;
        }

        private DependencyObject FindAncestorOfType(Type type, int level, DependencyObject d, bool isTracing)
        {
            if (type == null)
            {
                if (TraceData.IsEnabled)
                    TraceData.Trace(TraceEventType.Error, TraceData.RefAncestorTypeNotSpecified);
                return null;
            }
            if (level < 1)
            {
                if (TraceData.IsEnabled)
                    TraceData.Trace(TraceEventType.Error, TraceData.RefAncestorLevelInvalid);
                return null;
            }

            // initialize search to start at the parent of the given DO
            FrameworkObject fo = new FrameworkObject(d);
            fo.Reset(fo.GetPreferVisualParent(true).DO);

            while (fo.DO != null)
            {
                if (isTracing)
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.AncestorLookup(
                                            type.Name,
                                            TraceData.Identify(fo.DO)));
                }

                if (type.IsInstanceOfType(fo.DO))   // found it!
                {
                    if (--level <= 0)
                        break;
                }

                fo.Reset(fo.PreferVisualParent.DO);
            }

            return fo.DO;
        }

        private int IndexOf(Visual parent, Visual child, out Visual prevChild)
        {
            Visual temp;
            bool foundIndex = false;
            prevChild = null;
            int count = parent.InternalVisualChildrenCount;
            int i;
            for (i = 0; i < count; i++)
            {
                temp = parent.InternalGetVisualChild(i);
                if (child == temp)
                {
                    foundIndex = true;
                    break;
                }
                prevChild = temp;
            }
            if (foundIndex) return i;
            else return -1;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        RelativeSource _relativeSource;
    }

    #endregion RelativeObjectRef

    #region ExplicitObjectRef
    /// <summary> Explicit object reference. </summary>
    internal sealed class ExplicitObjectRef : ObjectRef
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor. </summary>
        internal ExplicitObjectRef(object o)
        {
            if (o is DependencyObject)
                _element = new WeakReference(o);
            else
                _object = o;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary> Returns the referenced object. </summary>
        /// <param name="d">Element defining context for the reference. </param>
        /// <param name="args">See ObjectRefArgs </param>
        internal override object GetObject(DependencyObject d, ObjectRefArgs args)
        {
            return (_element != null) ? _element.Target : _object;
        }

        /// <summary>
        /// true if the ObjectRef uses the mentor of the target element,
        /// rather than the target element itself.
        /// </summary>
        protected override bool ProtectedUsesMentor
        {
            get { return false; }
        }

        internal override string Identify()
        {
            return "Source";
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        object _object;
        WeakReference _element; // to DependencyObject (bug 986435)
    }

    #endregion ExplicitObjectRef
}


