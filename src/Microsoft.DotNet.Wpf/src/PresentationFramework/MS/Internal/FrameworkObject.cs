// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Explicit polymorphism between FrameworkElement and FrameworkContentElement
//

/*
    This struct is intended for use as a local variable (i.e. on the stack, not
    on the heap) in situations where a DependencyObject must be cast to
    FrameworkElement or FrameworkContentElement.  It is a replacement for the
    Helper.DowncastToFEorFCE pattern.  It generates less code and uses the
    same amount of memory on the stack, but is more readable.

    Instead of

        DependencyObject d;
        FrameworkElement fe;
        FrameworkContentElement fce;

        Helper.DowncastToFEorFCE(d, out fe, out fce, false);

        if (fe != null)
        {
            fe.SomeMethod(arg);
        }
        else if (fce != null)
        {
            fce.SomeMethod(arg);
        }

    you would write

        DependencyObject d;
        FrameworkObject fo = new FrameworkObject(d);

        fo.SomeMethod(arg);
*/

using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;             // Visual, etc.
using System.Windows.Media.Media3D;     // Visual3D, etc.

using System.IO;

namespace MS.Internal
{
    internal struct FrameworkObject
    {
        #region Constructors

        internal FrameworkObject(DependencyObject d)
        {
            // [code should be identical to Reset(d)]
            _do = d;

            if (FrameworkElement.DType.IsInstanceOfType(d))
            {
                _fe = (FrameworkElement)d;
                _fce = null;
            }
            else if (FrameworkContentElement.DType.IsInstanceOfType(d))
            {
                _fe = null;
                _fce = (FrameworkContentElement)d;
            }
            else
            {
                _fe = null;
                _fce = null;
            }
        }

        internal FrameworkObject(DependencyObject d, bool throwIfNeither)
            : this(d)
        {
            if (throwIfNeither && _fe == null && _fce == null)
            {
                object arg = (d != null) ? (object)d.GetType() : (object)"NULL";
                throw new InvalidOperationException(SR.Get(SRID.MustBeFrameworkDerived, arg));
            }
        }

        internal FrameworkObject(FrameworkElement fe, FrameworkContentElement fce)
        {
            _fe = fe;
            _fce = fce;

            if (fe != null)
                _do = fe;
            else
                _do = fce;
        }

        internal void Reset(DependencyObject d)
        {
            _do = d;

            if (FrameworkElement.DType.IsInstanceOfType(d))
            {
                _fe = (FrameworkElement)d;
                _fce = null;
            }
            else if (FrameworkContentElement.DType.IsInstanceOfType(d))
            {
                _fe = null;
                _fce = (FrameworkContentElement)d;
            }
            else
            {
                _fe = null;
                _fce = null;
            }
        }

        #endregion Constructors

        #region Conversion to DO/FE/FCE

        internal FrameworkElement           FE  { get { return _fe; } }
        internal FrameworkContentElement    FCE { get { return _fce; } }
        internal DependencyObject           DO  { get { return _do; } }

        internal bool   IsFE    { get { return (_fe != null); } }
        internal bool   IsFCE   { get { return (_fce != null); } }
        internal bool   IsValid { get { return (_fe != null || _fce != null); } }

        #endregion Conversion to DO/FE/FCE

        #region Polymorphic Properties

        // logical parent
        internal DependencyObject Parent
        {
            get
            {
                if (IsFE)
                {
                    return _fe.Parent;
                }
                else if (IsFCE)
                {
                    return _fce.Parent;
                }
                else
                {
                    return null;
                }
            }
        }

        internal int TemplateChildIndex
        {
            get
            {
                if (IsFE)
                {
                    return _fe.TemplateChildIndex;
                }
                else if (IsFCE)
                {
                    return _fce.TemplateChildIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        internal DependencyObject TemplatedParent
        {
            get
            {
                if (IsFE)
                {
                    return _fe.TemplatedParent;
                }
                else if (IsFCE)
                {
                    return _fce.TemplatedParent;
                }
                else
                {
                    return null;
                }
            }
        }

        internal Style ThemeStyle
        {
            get
            {
                if (IsFE)
                {
                    return _fe.ThemeStyle;
                }
                else if (IsFCE)
                {
                    return _fce.ThemeStyle;
                }
                else
                {
                    return null;
                }
            }
        }

        internal XmlLanguage Language
        {
            get
            {
                if (IsFE)
                {
                    return _fe.Language;
                }
                else if (IsFCE)
                {
                    return _fce.Language;
                }
                else
                {
                    return null;
                }
            }
        }

        internal FrameworkTemplate TemplateInternal
        {
            get
            {
                if (IsFE)
                {
                    return _fe.TemplateInternal;
                }
                else
                {
                    return null;
                }
            }
        }
        
        internal FrameworkObject FrameworkParent
        {
            get
            {
                if (IsFE)
                {
                    DependencyObject parent = _fe.ContextVerifiedGetParent();

                    // NOTE: Logical parent can only be an FE, FCE
                    if (parent != null)
                    {
                        Invariant.Assert(parent is FrameworkElement || parent is FrameworkContentElement);

                        if (_fe.IsParentAnFE)
                        {
                            return new FrameworkObject((FrameworkElement)parent, null);
                        }
                        else
                        {
                            return new FrameworkObject(null, (FrameworkContentElement)parent);
                        }
                    }

                    // This is when current does not have a logical parent that is an fe or fce
                    FrameworkObject foParent = GetContainingFrameworkElement(_fe.InternalVisualParent);
                    if (foParent.IsValid)
                    {
                        return foParent;
                    }

                    // allow subclasses to override (e.g. Popup)
                    foParent.Reset(_fe.GetUIParentCore());
                    if (foParent.IsValid)
                    {
                        return foParent;
                    }

                    // try InheritanceContext
                    foParent.Reset(Helper.FindMentor(_fe.InheritanceContext));
                    return foParent;
                }
                else if (IsFCE)
                {
                    DependencyObject parent = _fce.Parent;

                    // NOTE: Logical parent can only be an FE, FCE
                    if (parent != null)
                    {
                        Invariant.Assert(parent is FrameworkElement || parent is FrameworkContentElement);

                        if (_fce.IsParentAnFE)
                        {
                            return new FrameworkObject((FrameworkElement)parent, null);
                        }
                        else
                        {
                            return new FrameworkObject(null, (FrameworkContentElement)parent);
                        }
                    }

                    // This is when current does not have a logical parent that is an fe or fce
                    parent = ContentOperations.GetParent((ContentElement)_fce);
                    FrameworkObject foParent = GetContainingFrameworkElement(parent);
                    if (foParent.IsValid)
                    {
                        return foParent;
                    }

                    // try InheritanceContext
                    foParent.Reset(Helper.FindMentor(_fce.InheritanceContext));
                    return foParent;
                }
                else
                {
                    return GetContainingFrameworkElement(_do);
                }
            }
        }

        internal static FrameworkObject GetContainingFrameworkElement(DependencyObject current)
        {
            FrameworkObject fo = new FrameworkObject(current);

            while (!fo.IsValid && fo.DO != null)
            {
                // The current object is neither a FrameworkElement nor a
                // FrameworkContentElement.  We will now walk the "core"
                // tree looking for one.
                Visual visual;
                Visual3D visual3D;
                ContentElement ce;

                if ((visual = fo.DO as Visual) != null)
                {
                    fo.Reset(VisualTreeHelper.GetParent(visual));
                }
                else if ((ce = fo.DO as ContentElement) != null)
                {
                    fo.Reset(ContentOperations.GetParent(ce));
                }
                else if ((visual3D = fo.DO as Visual3D) != null)
                {
                    fo.Reset(VisualTreeHelper.GetParent(visual3D));
                }
                else
                {
                    // The parent could be an application.
                    fo.Reset(null);
                }
            }

            return fo;
        }

        // Style property
        internal Style Style
        {
            get
            {
                if (IsFE)
                {
                    return _fe.Style;
                }
                else if (IsFCE)
                {
                    return _fce.Style;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.Style = value;
                }
                else if (IsFCE)
                {
                    _fce.Style = value;
                }
            }
        }

        // IsStyleSetFromGenerator property
        internal bool IsStyleSetFromGenerator
        {
            get
            {
                if (IsFE)
                {
                    return _fe.IsStyleSetFromGenerator;
                }
                else if (IsFCE)
                {
                    return _fce.IsStyleSetFromGenerator;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.IsStyleSetFromGenerator = value;
                }
                else if (IsFCE)
                {
                    _fce.IsStyleSetFromGenerator = value;
                }
            }
        }


        // returns the effective parent, whether visual, logical,
        // inheritance context, etc.
        internal DependencyObject EffectiveParent
        {
            get
            {
                DependencyObject parent;

                if (IsFE)
                {
                    parent = VisualTreeHelper.GetParent(_fe);
                }
                else if (IsFCE)
                {
                    parent = _fce.Parent;
                }
                else
                {
                    Visual visual;
                    Visual3D visual3D;
                    ContentElement ce;

                    if ((visual = _do as Visual) != null)
                    {
                        parent = VisualTreeHelper.GetParent(visual);
                    }
                    else if ((ce = _do as ContentElement) != null)
                    {
                        parent = ContentOperations.GetParent(ce);
                    }
                    else if ((visual3D = _do as Visual3D) != null)
                    {
                        parent = VisualTreeHelper.GetParent(visual3D);
                    }
                    else
                    {
                        parent = null;
                    }
                }

                if (parent == null && _do != null)
                {
                    parent = _do.InheritanceContext;
                }

                return parent;
            }
        }

        internal FrameworkObject PreferVisualParent
        {
            get { return GetPreferVisualParent(false); }
        }

        internal bool IsLoaded
        {
            get
            {
                if (IsFE)
                {
                    return _fe.IsLoaded;
                }
                else if (IsFCE)
                {
                    return _fce.IsLoaded;
                }
                else
                {
                    return BroadcastEventHelper.IsParentLoaded(_do);
                }
            }
        }

        internal bool IsInitialized
        {
            get
            {
                if (IsFE)
                {
                    return _fe.IsInitialized;
                }
                else if (IsFCE)
                {
                    return _fce.IsInitialized;
                }
                else
                {
                    return true;
                }
            }
        }

        internal bool ThisHasLoadedChangeEventHandler
        {
            get
            {
                if (IsFE)
                {
                    return _fe.ThisHasLoadedChangeEventHandler;
                }
                else if (IsFCE)
                {
                    return _fce.ThisHasLoadedChangeEventHandler;
                }
                else
                {
                    return false;
                }
            }
        }

        internal bool SubtreeHasLoadedChangeHandler
        {
            get
            {
                if (IsFE)
                {
                    return _fe.SubtreeHasLoadedChangeHandler;
                }
                else if (IsFCE)
                {
                    return _fce.SubtreeHasLoadedChangeHandler;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.SubtreeHasLoadedChangeHandler = value;
                }
                else if (IsFCE)
                {
                    _fce.SubtreeHasLoadedChangeHandler = value;
                }
            }
        }

        internal InheritanceBehavior InheritanceBehavior
        {
            get
            {
                if (IsFE)
                {
                    return _fe.InheritanceBehavior;
                }
                else if (IsFCE)
                {
                    return _fce.InheritanceBehavior;
                }
                else
                {
                    return InheritanceBehavior.Default;
                }
            }
        }

        internal bool StoresParentTemplateValues
        {
            get
            {
                if (IsFE)
                {
                    return _fe.StoresParentTemplateValues;
                }
                else if (IsFCE)
                {
                    return _fce.StoresParentTemplateValues;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.StoresParentTemplateValues = value;
                }
                else if (IsFCE)
                {
                    _fce.StoresParentTemplateValues = value;
                }
            }
        }


        internal bool HasResourceReference
        {
            /* not used (yet)
            get
            {
                if (IsFE)
                {
                    return _fe.HasResourceReference;
                }
                else if (IsFCE)
                {
                    return _fce.HasResourceReference;
                }
                else
                {
                    return false;
                }
            }
            */
            set
            {
                if (IsFE)
                {
                    _fe.HasResourceReference = value;
                }
                else if (IsFCE)
                {
                    _fce.HasResourceReference = value;
                }
            }
        }

        /* not used (yet)
        internal bool HasStyleChanged
        {
            get
            {
                if (IsFE)
                {
                    return _fe.HasStyleChanged;
                }
                else if (IsFCE)
                {
                    return _fce.HasStyleChanged;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.HasStyleChanged = value;
                }
                else if (IsFCE)
                {
                    _fce.HasStyleChanged = value;
                }
            }
        }
        */

        internal bool HasTemplateChanged
        {
            /* not used (yet)
            get
            {
                if (IsFE)
                {
                    return _fe.HasTemplateChanged;
                }
                else
                {
                    return false;
                }
            }
            */
            set
            {
                if (IsFE)
                {
                    _fe.HasTemplateChanged = value;
                }
            }
        }

        // Says if there are any implicit styles in the ancestry
        internal bool ShouldLookupImplicitStyles
        {
            get
            {
                if (IsFE)
                {
                    return _fe.ShouldLookupImplicitStyles;
                }
                else if (IsFCE)
                {
                    return _fce.ShouldLookupImplicitStyles;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (IsFE)
                {
                    _fe.ShouldLookupImplicitStyles = value;
                }
                else if (IsFCE)
                {
                    _fce.ShouldLookupImplicitStyles = value;
                }
            }
        }

        #endregion Polymorphic Properties

        #region Polymorphic Methods

        internal static bool IsEffectiveAncestor(DependencyObject d1, DependencyObject d2
#if TRACE_INHERITANCE_CONTEXT
                                , DependencyProperty dp
#endif
                                )
        {
#if TRACE_INHERITANCE_CONTEXT
            int depth = 0;
#endif
            for (   FrameworkObject fo = new FrameworkObject(d2);
                    fo.DO != null;
                    fo.Reset(fo.EffectiveParent))
            {
                if (fo.DO == d1)
                {
#if TRACE_INHERITANCE_CONTEXT
                    Log("{0} creates cycle at depth {1}", LogIC(d2, dp, d1), depth);
#endif
                    return true;
                }
#if TRACE_INHERITANCE_CONTEXT
                ++depth;
#endif
            }

            return false;
        }

#if TRACE_INHERITANCE_CONTEXT
        static StreamWriter logFile;
        internal static void Log(string format, params object[] args)
        {
            if (logFile == null)
            {
                logFile = File.AppendText("IClog.txt");
                logFile.WriteLine();
                logFile.WriteLine("Log for {0}", Environment.CommandLine);
            }

            logFile.WriteLine(format, args);
            logFile.Flush();
        }

        internal static string LogIC(DependencyObject d1, DependencyProperty dp, DependencyObject d2)
        {
            string name = (dp == null) ? "[null]" : dp.Name;
            return String.Format("{0}.{1} = {2}", d1.GetType().Name, name, d2.GetType().Name);
        }
#endif

        internal void ChangeLogicalParent(DependencyObject newParent)
        {
            if (IsFE)
            {
                _fe.ChangeLogicalParent(newParent);
            }
            else if (IsFCE)
            {
                _fce.ChangeLogicalParent(newParent);
            }
        }

        internal void BeginInit()
        {
            if( IsFE )
            {
                _fe.BeginInit();
            }
            else if( IsFCE )
            {
                _fce.BeginInit();
            }
            else
            {
                UnexpectedCall();
            }
        }
        
        internal void EndInit()
        {
            if( IsFE )
            {
                _fe.EndInit();
            }
            else if( IsFCE )
            {
                _fce.EndInit();
            }
            else
            {
                UnexpectedCall();
            }
        }
        
        internal object FindName(string name, out DependencyObject scopeOwner)
        {
            if (IsFE)
            {
                return _fe.FindName(name, out scopeOwner);
            }
            else if (IsFCE)
            {
                return _fce.FindName(name, out scopeOwner);
            }
            else
            {
                scopeOwner = null;
                return null;
            }
        }

        // returns the parent in the "prefer-visual" sense.
        // That is, this method
        //  1. prefers visual to logical parent (with InheritanceContext last)
        //  2. does not see parents whose InheritanceBehavior forbids it
        // Call with force=true to get the parent even if the current object doesn't
        // allow it via rule 2.
        internal FrameworkObject GetPreferVisualParent(bool force)
        {
            // If we're not allowed to move up from here, return null
            InheritanceBehavior inheritanceBehavior = force ? InheritanceBehavior.Default : this.InheritanceBehavior;
            if (inheritanceBehavior != InheritanceBehavior.Default)
            {
                return new FrameworkObject(null);
            }

            FrameworkObject parent = GetRawPreferVisualParent();

            // make sure the parent allows itself to be found
            switch (parent.InheritanceBehavior)
            {
                case InheritanceBehavior.SkipToAppNow:
                case InheritanceBehavior.SkipToThemeNow:
                case InheritanceBehavior.SkipAllNow:
                    parent.Reset(null);
                    break;

                default:
                    break;
            }

            return parent;
        }

        // helper used by GetPreferVisualParent - doesn't check InheritanceBehavior
        private FrameworkObject GetRawPreferVisualParent()
        {
            // the null object has no parent
            if (_do == null)
            {
                return new FrameworkObject(null);
            }

            // get visual parent
            DependencyObject visualParent;
            if (IsFE)
            {
                visualParent = VisualTreeHelper.GetParent(_fe);
            }
            else if (IsFCE)
            {
                visualParent = null;
            }
            else if (_do != null)
            {
                Visual visual = _do as Visual;
                visualParent = (visual != null) ? VisualTreeHelper.GetParent(visual) : null;
            }
            else
            {
                visualParent = null;
            }

            if (visualParent != null)
            {
                return new FrameworkObject(visualParent);
            }

            // if no visual parent, get logical parent
            DependencyObject logicalParent;
            if (IsFE)
            {
                logicalParent = _fe.Parent;
            }
            else if (IsFCE)
            {
                logicalParent = _fce.Parent;
            }
            else if (_do != null)
            {
                ContentElement ce = _do as ContentElement;
                logicalParent = (ce != null) ? ContentOperations.GetParent(ce) : null;
            }
            else
            {
                logicalParent = null;
            }

            if (logicalParent != null)
            {
                return new FrameworkObject(logicalParent);
            }

            // if no logical or visual parent, get "uiCore" parent
            UIElement uiElement;
            ContentElement contentElement;
            DependencyObject uiCoreParent;
            if ((uiElement = _do as UIElement) != null)
            {
                uiCoreParent = uiElement.GetUIParentCore();
            }
            else if ((contentElement = _do as ContentElement) != null)
            {
                uiCoreParent = contentElement.GetUIParentCore();
            }
            else
            {
                uiCoreParent = null;
            }

            if (uiCoreParent != null)
            {
                return new FrameworkObject(uiCoreParent);
            }

            // if all else fails, use InheritanceContext
            return new FrameworkObject(_do.InheritanceContext);
        }

        internal void RaiseEvent(RoutedEventArgs args)
        {
            if (IsFE)
            {
                _fe.RaiseEvent(args);
            }
            else if (IsFCE)
            {
                _fce.RaiseEvent(args);
            }
        }

        internal void OnLoaded(RoutedEventArgs args)
        {
            if (IsFE)
            {
                _fe.OnLoaded(args);
            }
            else if (IsFCE)
            {
                _fce.OnLoaded(args);
            }
        }

        internal void OnUnloaded(RoutedEventArgs args)
        {
            if (IsFE)
            {
                _fe.OnUnloaded(args);
            }
            else if (IsFCE)
            {
                _fce.OnUnloaded(args);
            }
        }

        internal void ChangeSubtreeHasLoadedChangedHandler(DependencyObject mentor)
        {
            if (IsFE)
            {
                _fe.ChangeSubtreeHasLoadedChangedHandler(mentor);
            }
            else if (IsFCE)
            {
                _fce.ChangeSubtreeHasLoadedChangedHandler(mentor);
            }
        }

        internal void OnInheritedPropertyChanged(ref InheritablePropertyChangeInfo info)
        {
            if (IsFE)
            {
                _fe.RaiseInheritedPropertyChangedEvent(ref info);
            }
            else if (IsFCE)
            {
                _fce.RaiseInheritedPropertyChangedEvent(ref info);
            }
        }

        // Set the ShouldLookupImplicitStyles flag on the current
        // node if the parent has it set to true.
        internal void SetShouldLookupImplicitStyles()
        {
            if (!ShouldLookupImplicitStyles)
            {
                FrameworkObject parent = FrameworkParent;
                if (parent.IsValid && parent.ShouldLookupImplicitStyles)
                {
                    ShouldLookupImplicitStyles = true;
                }
            }
        }

        #endregion Polymorphic Methods

        #region Polymorphic Events

        internal event RoutedEventHandler Loaded
        {
            add
            {
                if (IsFE)
                {
                    _fe.Loaded += value;
                }
                else if (IsFCE)
                {
                    _fce.Loaded += value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
            remove
            {
                if (IsFE)
                {
                    _fe.Loaded -= value;
                }
                else if (IsFCE)
                {
                    _fce.Loaded -= value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
        }

        internal event RoutedEventHandler Unloaded
        {
            add
            {
                if (IsFE)
                {
                    _fe.Unloaded += value;
                }
                else if (IsFCE)
                {
                    _fce.Unloaded += value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
            remove
            {
                if (IsFE)
                {
                    _fe.Unloaded -= value;
                }
                else if (IsFCE)
                {
                    _fce.Unloaded -= value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
        }

        internal event InheritedPropertyChangedEventHandler InheritedPropertyChanged
        {
            add
            {
                if (IsFE)
                {
                    _fe.InheritedPropertyChanged += value;
                }
                else if (IsFCE)
                {
                    _fce.InheritedPropertyChanged += value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
            remove
            {
                if (IsFE)
                {
                    _fe.InheritedPropertyChanged -= value;
                }
                else if (IsFCE)
                {
                    _fce.InheritedPropertyChanged -= value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
        }

        internal event EventHandler ResourcesChanged
        {
            add
            {
                if (IsFE)
                {
                    _fe.ResourcesChanged += value;
                }
                else if (IsFCE)
                {
                    _fce.ResourcesChanged += value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
            remove
            {
                if (IsFE)
                {
                    _fe.ResourcesChanged -= value;
                }
                else if (IsFCE)
                {
                    _fce.ResourcesChanged -= value;
                }
                else
                {
                    UnexpectedCall();
                }
            }
        }

        #endregion Polymorphic Events

        #region Helper methods

        void UnexpectedCall()
        {
            Invariant.Assert(false, "Call to FrameworkObject expects either FE or FCE");
        }

        public override string ToString()
        {
            if (IsFE)
            {
                return _fe.ToString();
            }
            else if (IsFCE)
            {
                return _fce.ToString();
            }

            return "Null";
        }

        #endregion Helper methods

        #region Private fields

        private FrameworkElement        _fe;
        private FrameworkContentElement _fce;
        private DependencyObject        _do;

        #endregion Private fields
    }
}
