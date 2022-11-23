// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements some helper functions.
//

using MS.Internal.Utility;
using System;
using System.Collections;
using System.Collections.ObjectModel; // Collection<T>
using System.ComponentModel;

using System.Diagnostics;
using System.IO.Packaging;

using System.Reflection;
using System.Windows;
using System.Windows.Data; // BindingBase
using System.Windows.Markup; // IProvideValueTarget
using System.Windows.Media;
using System.Security;

using MS.Internal.AppModel;
using System.Windows.Threading;
using System.Collections.Generic;
using MS.Internal.Hashing.PresentationFramework;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

namespace MS.Internal
{
    // Miscellaneous (and internal) helper functions.
    internal static class Helper
    {
        internal static object ResourceFailureThrow(object key)
        {
            FindResourceHelper helper = new FindResourceHelper(key);
            return helper.TryCatchWhen();
        }

        private class FindResourceHelper
        {
            internal object TryCatchWhen()
            {
                Dispatcher.CurrentDispatcher.WrappedInvoke(new DispatcherOperationCallback(DoTryCatchWhen),
                                                                                            null,
                                                                                            1,
                                                                                            new DispatcherOperationCallback(CatchHandler));
                return _resource;
            }

            private object DoTryCatchWhen(object arg)
            {
                throw new ResourceReferenceKeyNotFoundException(SR.Get(SRID.MarkupExtensionResourceNotFound, _name), _name);
            }

            private object CatchHandler(object arg)
            {
                _resource = DependencyProperty.UnsetValue;
                return null;
            }

            public FindResourceHelper(object name)
            {
                _name = name;
                _resource = null;
            }

            private object _name;
            private object _resource;
        }


        // Find a data template (or table template) resource
        internal static object FindTemplateResourceFromAppOrSystem(DependencyObject target, ArrayList keys, int exactMatch, ref int bestMatch)
        {
            object resource = null;
            int k;

            // Comment out below three lines code.
            // For now, we will always get the resource from Application level
            // if the resource exists.
            //
            // But we do need to have a right design in the future that can make
            // sure the tree get the right resource updated while the Application
            // level resource is changed later dynamically.
            //
//            DependencyObject root = GetAbsoluteRoot(target);
//            bool isInWindowCollection = IsInWindowCollection (root);
//            if ( isInWindowCollection )

            Application app = Application.Current;
            if (app != null)
            {
                // If the element is rooted to a Window and App exists, defer to App.
                for (k = 0;  k < bestMatch;  ++k)
                {
                    object appResource = Application.Current.FindResourceInternal(keys[k]);
                    if (appResource != null)
                    {
                        bestMatch = k;
                        resource = appResource;

                        if (bestMatch < exactMatch)
                            return resource;
                    }
                }
            }

            // if best match is not found from the application level,
            // try it from system level.
            if (bestMatch >= exactMatch)
            {
                // Try the system resource collection.
                for (k = 0;  k < bestMatch;  ++k)
                {
                    object sysResource = SystemResources.FindResourceInternal(keys[k]);
                    if (sysResource != null)
                    {
                        bestMatch = k;
                        resource = sysResource;

                        if (bestMatch < exactMatch)
                            return resource;
                    }
                }
            }

            return resource;
        }

/*
        // Returns the absolute root of the tree by walking through frames.
        internal static DependencyObject GetAbsoluteRoot(DependencyObject iLogical)
        {
            DependencyObject currentRoot = iLogical;
            Visual visual;
            Visual parentVisual;
            bool bDone = false;

            if (currentRoot == null)
            {
                return null;
            }

            while (!bDone)
            {
                // Try logical parent.
                DependencyObject parent = LogicalTreeHelper.GetParent(currentRoot);

                if (parent != null)
                {
                    currentRoot = parent;
                }
                else
                {
                    // Try visual parent
                    Visual visual = currentRoot as Visual;
                    if (visual != null)
                    {
                        Visual parentVisual = VisualTreeHelper.GetParent(visual);
                        if (parentVisual != null)
                        {
                            currentRoot = parentVisual;
                            continue;
                        }
                    }

                    // No logical or visual parent, so we're done.
                    bDone = true;
                }
            }

            return currentRoot;
        }
*/

        /// <summary>
        ///     This method finds the mentor by looking up the InheritanceContext
        ///     links starting from the given node until it finds an FE/FCE. This
        ///     mentor will be used to do a FindResource call while evaluating this
        ///     expression.
        /// </summary>
        /// <remarks>
        ///     This method is invoked by the ResourceReferenceExpression
        ///     and BindingExpression
        /// </remarks>
        internal static DependencyObject FindMentor(DependencyObject d)
        {
            // Find the nearest FE/FCE InheritanceContext
            while (d != null)
            {
                FrameworkElement fe;
                FrameworkContentElement fce;
                Helper.DowncastToFEorFCE(d, out fe, out fce, false);

                if (fe != null)
                {
                    return fe;
                }
                else if (fce != null)
                {
                    return fce;
                }
                else
                {
                    d = d.InheritanceContext;
                }
            }

            return null;
        }

        /// <summary>
        /// Return true if the given property is not set locally or from a style
        /// </summary>
        internal static bool HasDefaultValue(DependencyObject d, DependencyProperty dp)
        {
            return HasDefaultOrInheritedValueImpl(d, dp, false, true);
        }

        /// <summary>
        /// Return true if the given property is not set locally or from a style or by inheritance
        /// </summary>
        internal static bool HasDefaultOrInheritedValue(DependencyObject d, DependencyProperty dp)
        {
            return HasDefaultOrInheritedValueImpl(d, dp, true, true);
        }

        /// <summary>
        /// Return true if the given property is not set locally or from a style
        /// </summary>
        internal static bool HasUnmodifiedDefaultValue(DependencyObject d, DependencyProperty dp)
        {
            return HasDefaultOrInheritedValueImpl(d, dp, false, false);
        }

        /// <summary>
        /// Return true if the given property is not set locally or from a style or by inheritance
        /// </summary>
        internal static bool HasUnmodifiedDefaultOrInheritedValue(DependencyObject d, DependencyProperty dp)
        {
            return HasDefaultOrInheritedValueImpl(d, dp, true, false);
        }

        /// <summary>
        /// Return true if the given property is not set locally or from a style
        /// </summary>
        private static bool HasDefaultOrInheritedValueImpl(DependencyObject d, DependencyProperty dp,
                                                                bool checkInherited,
                                                                bool ignoreModifiers)
        {
            PropertyMetadata metadata = dp.GetMetadata(d);
            bool hasModifiers;
            BaseValueSourceInternal source = d.GetValueSource(dp, metadata, out hasModifiers);

            if (source == BaseValueSourceInternal.Default ||
                (checkInherited && source == BaseValueSourceInternal.Inherited))
            {
                if (ignoreModifiers)
                {
                    // ignore modifiers on FE/FCE, for back-compat
                    if (d is FrameworkElement || d is FrameworkContentElement)
                    {
                        hasModifiers = false;
                    }
                }

                // a default or inherited value might be animated or coerced.  We should
                // return false in that case - the hasModifiers flag tests this.
                // (An expression modifier can't apply to a default or inherited value.)
                return !hasModifiers;
            }

            return false;
        }

        /// <summary>
        /// Downcast the given DependencyObject into FrameworkElement or
        /// FrameworkContentElement, as appropriate.
        /// </summary>
        internal static void DowncastToFEorFCE(DependencyObject d,
                                    out FrameworkElement fe, out FrameworkContentElement fce,
                                    bool throwIfNeither)
        {
            if (FrameworkElement.DType.IsInstanceOfType(d))
            {
                fe = (FrameworkElement)d;
                fce = null;
            }
            else if (FrameworkContentElement.DType.IsInstanceOfType(d))
            {
                fe = null;
                fce = (FrameworkContentElement)d;
            }
            else if (throwIfNeither)
            {
                throw new InvalidOperationException(SR.Get(SRID.MustBeFrameworkDerived, d.GetType()));
            }
            else
            {
                fe = null;
                fce = null;
            }
        }


        /// <summary>
        /// Issue a trace message if both the xxxStyle and xxxStyleSelector
        /// properties are set on the given element.
        /// </summary>
        internal static void CheckStyleAndStyleSelector(string name,
                                                        DependencyProperty styleProperty,
                                                        DependencyProperty styleSelectorProperty,
                                                        DependencyObject d)
        {
            // Issue a trace message if user defines both xxxStyle and xxxStyleSelector
            // (bugs 1007020, 1019240).  Only explicit local values or resource
            // references count;  data-bound or styled values don't count.
            // Do not throw here (bug 1434271), because it's very confusing if the
            // user tries to continue from this exception.
            if (TraceData.IsEnabled)
            {
                object styleSelector = d.ReadLocalValue(styleSelectorProperty);

                if (styleSelector != DependencyProperty.UnsetValue &&
                    (styleSelector is System.Windows.Controls.StyleSelector || styleSelector is ResourceReferenceExpression))
                {
                    object style = d.ReadLocalValue(styleProperty);

                    if (style != DependencyProperty.UnsetValue &&
                        (style is Style || style is ResourceReferenceExpression))
                    {
                        TraceData.TraceAndNotify(TraceEventType.Error, TraceData.StyleAndStyleSelectorDefined(name), null,
                            traceParameters: new object[] { d });
                    }
                }
            }
        }

        /// <summary>
        /// Issue a trace message if both the xxxTemplate and xxxTemplateSelector
        /// properties are set on the given element.
        /// </summary>
        internal static void CheckTemplateAndTemplateSelector(string name,
                                                        DependencyProperty templateProperty,
                                                        DependencyProperty templateSelectorProperty,
                                                        DependencyObject d)
        {
            // Issue a trace message if user defines both xxxTemplate and xxxTemplateSelector
            // (bugs 1007020, 1019240).  Only explicit local values or resource
            // references count;  data-bound or templated values don't count.
            // Do not throw here (bug 1434271), because it's very confusing if the
            // user tries to continue from this exception.
            if (TraceData.IsEnabled)
            {
                if (IsTemplateSelectorDefined(templateSelectorProperty, d))
                {
                    if (IsTemplateDefined(templateProperty, d))
                    {
                        TraceData.TraceAndNotify(TraceEventType.Error, TraceData.TemplateAndTemplateSelectorDefined(name), null,
                            traceParameters: new object[] { d });
                    }
                }
            }
        }

        /// <summary>
        /// Check whether xxxTemplateSelector property is set on the given element.
        /// Only explicit local values or resource references count;  data-bound or templated values don't count.
        /// </summary>
        internal static bool IsTemplateSelectorDefined(DependencyProperty templateSelectorProperty, DependencyObject d)
        {
            // Check whether xxxTemplateSelector property is set on the given element.
            object templateSelector = d.ReadLocalValue(templateSelectorProperty);
            // the checks for UnsetValue and null are for perf:
            // they're redundant to the type checks, but they're cheaper
            return (templateSelector != DependencyProperty.UnsetValue &&
                    templateSelector != null &&
                   (templateSelector is System.Windows.Controls.DataTemplateSelector ||
                    templateSelector is ResourceReferenceExpression));
        }

        /// <summary>
        /// Check whether xxxTemplate property is set on the given element.
        /// Only explicit local values or resource references count;  data-bound or templated values don't count.
        /// </summary>
        internal static bool IsTemplateDefined(DependencyProperty templateProperty, DependencyObject d)
        {
            // Check whether xxxTemplate property is set on the given element.
            object template = d.ReadLocalValue(templateProperty);
            // the checks for UnsetValue and null are for perf:
            // they're redundant to the type checks, but they're cheaper
            return (template != DependencyProperty.UnsetValue &&
                    template != null &&
                    (template is FrameworkTemplate ||
                    template is ResourceReferenceExpression));
        }

        ///<summary>
        ///     Helper method to find an object by name inside a template
        ///</summary>
        internal static object FindNameInTemplate(string name, DependencyObject templatedParent)
        {
            FrameworkElement fe = templatedParent as FrameworkElement;
            Debug.Assert( fe != null );

            return fe.TemplateInternal.FindName(name, fe);
        }

        /// <summary>
        /// Find the IGeneratorHost that is responsible (possibly indirectly)
        /// for the creation of the given DependencyObject.
        /// </summary>
        internal static MS.Internal.Controls.IGeneratorHost GeneratorHostForElement(DependencyObject element)
        {
            DependencyObject d = null;
            DependencyObject parent = null;

            // 1. Follow the TemplatedParent chain to the end.  This should be
            // the ItemContainer.
            while (element != null)
            {
                while (element != null)
                {
                    d = element;
                    element= GetTemplatedParent(element);

                    // Special case to display the selected item in a ComboBox, when
                    // the items are XmlNodes and the DisplayMemberPath is an XPath
                    // that uses namespace prefixes.  We need an
                    // XmlNamespaceManager to map prefixes to namespaces, and in this
                    // special case we should use the ComboBox itself, rather than any
                    // surrounding ItemsControl.  There's no elegant way to detect
                    // this situation;  the following code is a child of necessity.
                    // It relies on the fact that the "selection box" is implemented
                    // by a ContentPresenter in the ComboBox's control template, and
                    // any ContentPresenter whose TemplatedParent is a ComboBox is
                    // playing the role of "selection box".
                    if (d is System.Windows.Controls.ContentPresenter)
                    {
                        System.Windows.Controls.ComboBox cb = element as System.Windows.Controls.ComboBox;
                        if (cb != null)
                        {
                            return cb;
                        }
                    }
                }

                Visual v = d as Visual;
                if (v != null)
                {
                    parent = VisualTreeHelper.GetParent(v);

                    // In ListView, we should rise through a GridView*RowPresenter
                    // even though it is not the TemplatedParent (bug 1937470)
                    element = parent as System.Windows.Controls.Primitives.GridViewRowPresenterBase;
                }
                else
                {
                    parent = null;
                }
            }

            // 2. In an ItemsControl, the container's parent is the "ItemsHost"
            // panel, from which we get to the ItemsControl by public API.
            if (parent != null)
            {
                System.Windows.Controls.ItemsControl ic = System.Windows.Controls.ItemsControl.GetItemsOwner(parent);
                if (ic != null)
                    return ic;
            }

            return null;
        }

        internal static DependencyObject GetTemplatedParent(DependencyObject d)
        {
            FrameworkElement fe;
            FrameworkContentElement fce;
            DowncastToFEorFCE(d, out fe, out fce, false);
            if (fe != null)
                return fe.TemplatedParent;
            else if (fce != null)
                return fce.TemplatedParent;

            return null;
        }

        /// <summary>
        /// Find the XmlDataProvider (if any) that is associated with the
        /// given DependencyObject.
        /// This method only works when the DO is part of the generated content
        /// of an ItemsControl or TableRowGroup.
        /// </summary>
        internal static System.Windows.Data.XmlDataProvider XmlDataProviderForElement(DependencyObject d)
        {
            MS.Internal.Controls.IGeneratorHost host = Helper.GeneratorHostForElement(d);
            System.Windows.Controls.ItemCollection ic = (host != null) ? host.View : null;
            ICollectionView icv = (ic != null) ? ic.CollectionView : null;
            MS.Internal.Data.XmlDataCollection xdc = (icv != null) ? icv.SourceCollection as MS.Internal.Data.XmlDataCollection : null;

            return (xdc != null) ? xdc.ParentXmlDataProvider : null;
        }

#if CF_Envelope_Activation_Enabled
        /// <summary>
        /// Indicates whether our content is inside an old-style container
        /// </summary>
        /// <value></value>

        internal static bool IsContainer
        {
            get
            {
                return BindUriHelper.Container != null;
            }
        }
#endif

        /// <summary>
        /// Measure a simple element with a single child.
        /// </summary>
        internal static Size MeasureElementWithSingleChild(UIElement element, Size constraint)
        {
            UIElement child = (VisualTreeHelper.GetChildrenCount(element) > 0) ? VisualTreeHelper.GetChild(element, 0) as UIElement : null;

            if (child != null)
            {
                child.Measure(constraint);
                return child.DesiredSize;
            }

            return new Size();
        }


        /// <summary>
        /// Arrange a simple element with a single child.
        /// </summary>
        internal static Size ArrangeElementWithSingleChild(UIElement element, Size arrangeSize)
        {
            UIElement child = (VisualTreeHelper.GetChildrenCount(element) > 0) ? VisualTreeHelper.GetChild(element, 0) as UIElement : null;

            if (child != null)
            {
                child.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }

        /// <summary>
        /// Helper method used for double parameter validation.  Returns false
        /// if the value is either Infinity (positive or negative) or NaN.
        /// </summary>
        /// <param name="value">The double value to test</param>
        /// <returns>Whether the value is a valid double.</returns>
        internal static bool IsDoubleValid(double value)
        {
            return !(Double.IsInfinity(value) || Double.IsNaN(value));
        }

        /// <summary>
        /// Checks if the given IProvideValueTarget can receive
        /// a DynamicResource or Binding MarkupExtension.
        /// </summary>
        internal static void CheckCanReceiveMarkupExtension(
                MarkupExtension     markupExtension,
                IServiceProvider    serviceProvider,
            out DependencyObject    targetDependencyObject,
            out DependencyProperty  targetDependencyProperty)
        {
            targetDependencyObject = null;
            targetDependencyProperty = null;

            IProvideValueTarget provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (provideValueTarget == null)
            {
                return;
            }

            object targetObject = provideValueTarget.TargetObject;

            if (targetObject == null)
            {
                return;
            }

            Type targetType = targetObject.GetType();
            object targetProperty = provideValueTarget.TargetProperty;

            if (targetProperty != null)
            {
                targetDependencyProperty = targetProperty as DependencyProperty;
                if (targetDependencyProperty != null)
                {
                    // This is the DependencyProperty case

                    targetDependencyObject = targetObject as DependencyObject;
                    Debug.Assert(targetDependencyObject != null, "DependencyProperties can only be set on DependencyObjects");
                }
                else
                {
                    MemberInfo targetMember = targetProperty as MemberInfo;
                    if (targetMember != null)
                    {
                        // This is the Clr Property case
                        PropertyInfo propertyInfo = targetMember as PropertyInfo;

                        // Setters, Triggers, DataTriggers & Conditions are the special cases of
                        // Clr properties where DynamicResource & Bindings are allowed. Normally
                        // these cases are handled by the parser calling the appropriate
                        // ReceiveMarkupExtension method.  But a custom MarkupExtension
                        // that delegates ProvideValue will end up here.
                        // So we handle it similarly to how the parser does it.

                        EventHandler<System.Windows.Markup.XamlSetMarkupExtensionEventArgs> setMarkupExtension
                            = LookupSetMarkupExtensionHandler(targetType);

                        if (setMarkupExtension != null && propertyInfo != null)
                        {
                            System.Xaml.IXamlSchemaContextProvider scp = serviceProvider.GetService(typeof(System.Xaml.IXamlSchemaContextProvider)) as System.Xaml.IXamlSchemaContextProvider;
                            if (scp != null)
                            {
                                System.Xaml.XamlSchemaContext sc = scp.SchemaContext;
                                System.Xaml.XamlType xt = sc.GetXamlType(targetType);
                                if (xt != null)
                                {
                                    System.Xaml.XamlMember member = xt.GetMember(propertyInfo.Name);
                                    if (member != null)
                                    {
                                        var eventArgs = new System.Windows.Markup.XamlSetMarkupExtensionEventArgs(member, markupExtension, serviceProvider);

                                        // ask the target object whether it accepts MarkupExtension
                                        setMarkupExtension(targetObject, eventArgs);
                                        if (eventArgs.Handled)
                                            return;     // if so, all is well
                                    }
                                }
                            }
                        }


                        // Find the MemberType

                        Debug.Assert(targetMember is PropertyInfo || targetMember is MethodInfo,
                            "TargetMember is either a Clr property or an attached static settor method");

                        Type memberType;

                        if (propertyInfo != null)
                        {
                            memberType = propertyInfo.PropertyType;
                        }
                        else
                        {
                            MethodInfo methodInfo = (MethodInfo)targetMember;
                            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                            Debug.Assert(parameterInfos.Length == 2, "The signature of a static settor must contain two parameters");
                            memberType = parameterInfos[1].ParameterType;
                        }

                        // Check if the MarkupExtensionType is assignable to the given MemberType
                        // This check is to allow properties such as the following
                        // - DataTrigger.Binding
                        // - Condition.Binding
                        // - HierarchicalDataTemplate.ItemsSource
                        // - GridViewColumn.DisplayMemberBinding

                        if (!typeof(MarkupExtension).IsAssignableFrom(memberType) ||
                             !memberType.IsAssignableFrom(markupExtension.GetType()))
                        {
                            throw new XamlParseException(SR.Get(SRID.MarkupExtensionDynamicOrBindingOnClrProp,
                                                                markupExtension.GetType().Name,
                                                                targetMember.Name,
                                                                targetType.Name));
                        }
                    }
                    else
                    {
                        // This is the Collection ContentProperty case
                        // Example:
                        // <DockPanel>
                        //   <Button />
                        //   <DynamicResource ResourceKey="foo" />
                        // </DockPanel>

                        // Collection<BindingBase> used in MultiBinding is a special
                        // case of a Collection that can contain a Binding.

                        if (!typeof(BindingBase).IsAssignableFrom(markupExtension.GetType()) ||
                            !typeof(Collection<BindingBase>).IsAssignableFrom(targetProperty.GetType()))
                        {
                            throw new XamlParseException(SR.Get(SRID.MarkupExtensionDynamicOrBindingInCollection,
                                                                markupExtension.GetType().Name,
                                                                targetProperty.GetType().Name));
                        }
                    }
                }
            }
            else
            {
                // This is the explicit Collection Property case
                // Example:
                // <DockPanel>
                // <DockPanel.Children>
                //   <Button />
                //   <DynamicResource ResourceKey="foo" />
                // </DockPanel.Children>
                // </DockPanel>

                // Collection<BindingBase> used in MultiBinding is a special
                // case of a Collection that can contain a Binding.

                if (!typeof(BindingBase).IsAssignableFrom(markupExtension.GetType()) ||
                    !typeof(Collection<BindingBase>).IsAssignableFrom(targetType))
                {
                    throw new XamlParseException(SR.Get(SRID.MarkupExtensionDynamicOrBindingInCollection,
                                                        markupExtension.GetType().Name,
                                                        targetType.Name));
                }
            }
        }

        static EventHandler<System.Windows.Markup.XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandler(Type type)
        {
            if (typeof(Setter).IsAssignableFrom(type))
            {
                return Setter.ReceiveMarkupExtension;
            }
            else if (typeof(DataTrigger).IsAssignableFrom(type))
            {
                return DataTrigger.ReceiveMarkupExtension;
            }
            else if (typeof(Condition).IsAssignableFrom(type))
            {
                return Condition.ReceiveMarkupExtension;
            }
            return null;
        }

        // build a format string suitable for String.Format from the given argument,
        // by expanding the convenience form, if necessary
        internal static string GetEffectiveStringFormat(string stringFormat)
        {
            if (stringFormat.IndexOf('{') < 0)
            {
                // convenience syntax - build a composite format string with one parameter
                stringFormat = @"{0:" + stringFormat + @"}";
            }

            return stringFormat;
        }

        #region ItemValueStorage common methods

        internal static object ReadItemValue(DependencyObject owner, object item, int dpIndex)
        {
            if (item != null)
            {
                List<KeyValuePair<int, object>> itemValues = GetItemValues(owner, item);

                if (itemValues != null)
                {
                    for (int i = 0; i < itemValues.Count; i++)
                    {
                        if (itemValues[i].Key == dpIndex)
                        {
                            return itemValues[i].Value;
                        }
                    }
                }
            }

            return null;
        }


        internal static void StoreItemValue(DependencyObject owner, object item, int dpIndex, object value)
        {
            if (item != null)
            {
                List<KeyValuePair<int, object>> itemValues = EnsureItemValues(owner, item);

                //
                // Find the key, if it exists, and modify its value.  Since the number of DPs we want to store
                // is typically very small, using a List in this manner is faster than hashing
                //

                bool found = false;
                KeyValuePair<int, object> keyValue = new KeyValuePair<int, object>(dpIndex, value);

                for (int j = 0; j < itemValues.Count; j++)
                {
                    if (itemValues[j].Key == dpIndex)
                    {
                        itemValues[j] = keyValue;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    itemValues.Add(keyValue);
                }
            }
        }

        internal static void ClearItemValue(DependencyObject owner, object item, int dpIndex)
        {
            if (item != null)
            {
                List<KeyValuePair<int, object>> itemValues = GetItemValues(owner, item);

                if (itemValues != null)
                {
                    for (int i = 0; i < itemValues.Count; i++)
                    {
                        if (itemValues[i].Key == dpIndex)
                        {
                            itemValues.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the ItemValues list for a given item.  May return null if one hasn't been set yet.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static List<KeyValuePair<int, object>> GetItemValues(DependencyObject owner, object item)
        {
            return GetItemValues(owner, item, ItemValueStorageField.GetValue(owner));
        }

        internal static List<KeyValuePair<int, object>> GetItemValues(DependencyObject owner, object item,
                                                              WeakDictionary<object, List<KeyValuePair<int, object>>> itemValueStorage)
        {
            Debug.Assert(item != null);
            List<KeyValuePair<int, object>> itemValues = null;

            if (itemValueStorage != null)
            {
                itemValueStorage.TryGetValue(item, out itemValues);
            }

            return itemValues;
        }


        internal static List<KeyValuePair<int, object>> EnsureItemValues(DependencyObject owner, object item)
        {
            WeakDictionary<object, List<KeyValuePair<int, object>>> itemValueStorage = EnsureItemValueStorage(owner);
            List<KeyValuePair<int, object>> itemValues = GetItemValues(owner, item, itemValueStorage);

            if (itemValues == null && HashHelper.HasReliableHashCode(item))
            {
                itemValues = new List<KeyValuePair<int, object>>(3);    // So far the only use of this is to store three values.
                itemValueStorage[item] = itemValues;
            }

            return itemValues;
        }


        internal static WeakDictionary<object, List<KeyValuePair<int, object>>> EnsureItemValueStorage(DependencyObject owner)
        {
            WeakDictionary<object, List<KeyValuePair<int, object>>> itemValueStorage = ItemValueStorageField.GetValue(owner);

            if (itemValueStorage == null)
            {
                itemValueStorage = new WeakDictionary<object, List<KeyValuePair<int, object>>>();
                ItemValueStorageField.SetValue(owner, itemValueStorage);
            }

            return itemValueStorage;
        }

        /// <summary>
        /// Sets all values saved in ItemValueStorage for the given item onto the container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        internal static void SetItemValuesOnContainer(DependencyObject owner, DependencyObject container, object item)
        {
            int[] dpIndices = ItemValueStorageIndices;
            List<KeyValuePair<int, object>> itemValues = GetItemValues(owner, item) ?? new List<KeyValuePair<int, object>>();

            for (int j = 0; j < dpIndices.Length; j++)
            {
                int dpIndex = dpIndices[j];
                DependencyProperty dp = DependencyProperty.RegisteredPropertyList.List[dpIndex];
                object value = DependencyProperty.UnsetValue;

                for (int i = 0; i < itemValues.Count; i++)
                {
                    if (itemValues[i].Key == dpIndex)
                    {
                        value = itemValues[i].Value;
                        break;
                    }
                }

                if (dp != null)
                {
                    if (value != DependencyProperty.UnsetValue)
                    {
                        ModifiedItemValue modifiedItemValue = value as ModifiedItemValue;
                        if (modifiedItemValue == null)
                        {
                            // for real properties, call SetValue so that the property's
                            // change-callback is called
                            container.SetValue(dp, value);
                        }
                        else if (modifiedItemValue.IsCoercedWithCurrentValue)
                        {
                            // set as current-value
                            container.SetCurrentValue(dp, modifiedItemValue.Value);
                        }
                    }
                    else if (container != container.GetValue(ItemContainerGenerator.ItemForItemContainerProperty))
                    {
                        // at this point we have
                        //   a. a real property (dp != null)
                        //   b. with no saved value (value != Unset)
                        //   c. a generated container (container != item)
                        // If the container has a local or current value for the
                        // property, it came from a previous lifetime before the
                        // container was recycled and should be discarded
                        EntryIndex entryIndex = container.LookupEntry(dpIndex);
                        EffectiveValueEntry entry = new EffectiveValueEntry(dp);

                        // first discard the current value, if any.
                        if (entryIndex.Found)
                        {
                            entry = container.EffectiveValues[entryIndex.Index];

                            if (entry.IsCoercedWithCurrentValue)
                            {
                                // call ClearCurrentValue  (when it exists - for now, use the substitute - (see comment at DO.InvalidateProperty)
                                container.InvalidateProperty(dp, preserveCurrentValue:false);

                                // side-effects may move the entry - re-fetch it
                                entryIndex = container.LookupEntry(dpIndex);

                                if (entryIndex.Found)
                                {
                                    entry = container.EffectiveValues[entryIndex.Index];
                                }
                            }
                        }

                        // next discard values that were set in a previous lifetime
                        if (entryIndex.Found)
                        {
                            if ((entry.BaseValueSourceInternal == BaseValueSourceInternal.Local ||
                                 entry.BaseValueSourceInternal == BaseValueSourceInternal.ParentTemplate) &&
                                 !entry.HasModifiers)
                            {
                                // this entry denotes a value from a previous lifetime - discard it
                                container.ClearValue(dp);
                            }
                        }
                    }
                }
                else if (value != DependencyProperty.UnsetValue)
                {
                    // for "fake" properties (no corresponding DP - e.g. VSP's desired-size),
                    // set the property directly into the effective value table
                    EntryIndex entryIndex = container.LookupEntry(dpIndex);
                    container.SetEffectiveValue(entryIndex, null /*dp*/, dpIndex, null /*metadata*/, value, BaseValueSourceInternal.Local);
                }
            }
        }

        /// <summary>
        /// Stores the value of a container for the given item and set of dependency properties
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <param name="dpIndices"></param>
        internal static void StoreItemValues(IContainItemStorage owner, DependencyObject container, object item)
        {
            int[] dpIndices = ItemValueStorageIndices;

            DependencyObject ownerDO = (DependencyObject)owner;

            //
            // Loop through all DPs we care about storing.  If the container has a current-value or locally-set value we'll store it.
            //
            for (int i = 0; i < dpIndices.Length; i++)
            {
                int dpIndex = dpIndices[i];
                EntryIndex entryIndex = container.LookupEntry(dpIndex);

                if (entryIndex.Found)
                {
                    EffectiveValueEntry entry = container.EffectiveValues[entryIndex.Index];

                    if ((entry.BaseValueSourceInternal == BaseValueSourceInternal.Local ||
                         entry.BaseValueSourceInternal == BaseValueSourceInternal.ParentTemplate) &&
                         !entry.HasModifiers)
                    {
                        // store local values that aren't modified
                        StoreItemValue(ownerDO, item, dpIndex, entry.Value);
                    }
                    else if (entry.IsCoercedWithCurrentValue)
                    {
                        // store current-values
                        StoreItemValue(ownerDO, item,
                                        dpIndex,
                                        new ModifiedItemValue(entry.ModifiedValue.CoercedValue, FullValueSource.IsCoercedWithCurrentValue));
                    }
                    else
                    {
                        ClearItemValue(ownerDO, item, dpIndex);
                    }
                }
            }
        }

        internal static void ClearItemValueStorage(DependencyObject owner)
        {
            ItemValueStorageField.ClearValue(owner);
        }

        internal static void ClearItemValueStorage(DependencyObject owner, int[] dpIndices)
        {
            ClearItemValueStorageRecursive(ItemValueStorageField.GetValue(owner), dpIndices);
        }

        private static void ClearItemValueStorageRecursive(WeakDictionary<object, List<KeyValuePair<int, object>>> itemValueStorage, int[] dpIndices)
        {
            if (itemValueStorage != null)
            {
                foreach (List<KeyValuePair<int, object>> itemValuesList in itemValueStorage.Values)
                {
                    for (int i=0; i<itemValuesList.Count; i++)
                    {
                        KeyValuePair<int, object> itemValue = itemValuesList[i];
                        if (itemValue.Key == ItemValueStorageField.GlobalIndex)
                        {
                            ClearItemValueStorageRecursive((WeakDictionary<object, List<KeyValuePair<int, object>>>)itemValue.Value, dpIndices);
                        }

                        for (int j=0; j<dpIndices.Length; j++)
                        {
                            if (itemValue.Key == dpIndices[j])
                            {
                                itemValuesList.RemoveAt(i--);
                                break;
                            }
                        }
                    }
                }
            }
        }

        // *** DEAD CODE  Only used in VSP45 compat mode ***
        internal static void ApplyCorrectionFactorToPixelHeaderSize(
            ItemsControl scrollingItemsControl,
            FrameworkElement virtualizingElement,
            Panel itemsHost,
            ref Size headerSize)
        {
            if (!VirtualizingStackPanel.IsVSP45Compat)
                return;

            bool shouldApplyItemsCorrectionFactor = itemsHost != null && itemsHost.IsVisible;
            if (shouldApplyItemsCorrectionFactor)
            {
                Thickness itemsCorrectionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);
                headerSize.Height = Math.Max(itemsCorrectionFactor.Top, headerSize.Height);
            }
            else
            {
                headerSize.Height = Math.Max(virtualizingElement.DesiredSize.Height, headerSize.Height);
            }
            headerSize.Width = Math.Max(virtualizingElement.DesiredSize.Width, headerSize.Width);
        }
        // *** END DEAD CODE ***

        // *** DEAD CODE  Only used in VSP45 compat mode ***
        internal static HierarchicalVirtualizationItemDesiredSizes ApplyCorrectionFactorToItemDesiredSizes(
            FrameworkElement virtualizingElement,
            Panel itemsHost)
        {
            HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes =
                GroupItem.HierarchicalVirtualizationItemDesiredSizesField.GetValue(virtualizingElement);

            if (!VirtualizingStackPanel.IsVSP45Compat)
                return itemDesiredSizes;

            if (itemsHost != null && itemsHost.IsVisible)
            {
                Size itemPixelSize = itemDesiredSizes.PixelSize;
                Size itemPixelSizeInViewport = itemDesiredSizes.PixelSizeInViewport;
                Size itemPixelSizeBeforeViewport = itemDesiredSizes.PixelSizeBeforeViewport;
                Size itemPixelSizeAfterViewport = itemDesiredSizes.PixelSizeAfterViewport;
                bool correctionComputed = false;
                Thickness correctionFactor = new Thickness(0);
                Size desiredSize = virtualizingElement.DesiredSize;

                if (DoubleUtil.GreaterThan(itemPixelSize.Height, 0))
                {
                    correctionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);
                    itemPixelSize.Height += correctionFactor.Bottom;
                    correctionComputed = true;
                }
                itemPixelSize.Width = Math.Max(desiredSize.Width, itemPixelSize.Width);

                if (DoubleUtil.AreClose(itemDesiredSizes.PixelSizeAfterViewport.Height, 0) &&
                    DoubleUtil.AreClose(itemDesiredSizes.PixelSizeInViewport.Height, 0) &&
                    DoubleUtil.GreaterThan(itemDesiredSizes.PixelSizeBeforeViewport.Height, 0))
                {
                    if (!correctionComputed)
                    {
                        correctionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);
                    }
                    itemPixelSizeBeforeViewport.Height += correctionFactor.Bottom;
                    correctionComputed = true;
                }
                itemPixelSizeBeforeViewport.Width = Math.Max(desiredSize.Width, itemPixelSizeBeforeViewport.Width);

                if (DoubleUtil.AreClose(itemDesiredSizes.PixelSizeAfterViewport.Height, 0) &&
                    DoubleUtil.GreaterThan(itemDesiredSizes.PixelSizeInViewport.Height, 0))
                {
                    if (!correctionComputed)
                    {
                        correctionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);
                    }
                    itemPixelSizeInViewport.Height += correctionFactor.Bottom;
                    correctionComputed = true;
                }
                itemPixelSizeInViewport.Width = Math.Max(desiredSize.Width, itemPixelSizeInViewport.Width);

                if (DoubleUtil.GreaterThan(itemDesiredSizes.PixelSizeAfterViewport.Height, 0))
                {
                    if (!correctionComputed)
                    {
                        correctionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);
                    }
                    itemPixelSizeAfterViewport.Height += correctionFactor.Bottom;
                    correctionComputed = true;
                }
                itemPixelSizeAfterViewport.Width = Math.Max(desiredSize.Width, itemPixelSizeAfterViewport.Width);

                itemDesiredSizes = new HierarchicalVirtualizationItemDesiredSizes(itemDesiredSizes.LogicalSize,
                    itemDesiredSizes.LogicalSizeInViewport,
                    itemDesiredSizes.LogicalSizeBeforeViewport,
                    itemDesiredSizes.LogicalSizeAfterViewport,
                    itemPixelSize,
                    itemPixelSizeInViewport,
                    itemPixelSizeBeforeViewport,
                    itemPixelSizeAfterViewport);
            }
            return itemDesiredSizes;
        }
        // *** END DEAD CODE ***

        // *** DEAD CODE  Only used in VSP45 compat mode ***
        internal static void ComputeCorrectionFactor(
            ItemsControl scrollingItemsControl,
            FrameworkElement virtualizingElement,
            Panel itemsHost,
            FrameworkElement headerElement)
        {
            if (!VirtualizingStackPanel.IsVSP45Compat)
                return;

            Rect parentRect = new Rect(new Point(), virtualizingElement.DesiredSize);
            bool remeasure = false;

            if (itemsHost != null)
            {
                Thickness itemsCorrectionFactor = new Thickness();

                if (itemsHost.IsVisible)
                {
                    Rect itemsRect = itemsHost.TransformToAncestor(virtualizingElement).TransformBounds(new Rect(new Point(), itemsHost.DesiredSize));
                    itemsCorrectionFactor.Top = itemsRect.Top;
                    itemsCorrectionFactor.Bottom = parentRect.Bottom - itemsRect.Bottom;

                    // the correction is supposed to be non-negative, but there's some
                    // kind of race condition that occasionally results in a negative
                    // value that eventually crashes in ApplyCorrectionFactorToItemDesiredSizes
                    // by setting a rect.Height to a negative number.
                    // We haven't been able to repro the race,
                    // so to avoid the crash we'll artificially clamp the correction.
                    if (itemsCorrectionFactor.Bottom < 0)
                    {
                        #if DEBUG
                        Debugger.Break();
                        // Debug.Assert would be better, but we're in layout where
                        // Dispatcher events are disabled - can't pop up a dialog
                        #endif
                        itemsCorrectionFactor.Bottom = 0;
                    }
                }

                Thickness oldItemsCorrectionFactor = GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GetValue(virtualizingElement);

                if (!(DoubleUtil.AreClose(itemsCorrectionFactor.Top, oldItemsCorrectionFactor.Top) &&
                      DoubleUtil.AreClose(itemsCorrectionFactor.Bottom, oldItemsCorrectionFactor.Bottom)))
                {
                    remeasure = true;
                    GroupItem.DesiredPixelItemsSizeCorrectionFactorField.SetValue(virtualizingElement, itemsCorrectionFactor);
                }
            }

            if (remeasure)
            {
                if (scrollingItemsControl != null)
                {
                    itemsHost = scrollingItemsControl.ItemsHost;
                    if (itemsHost != null)
                    {
                        VirtualizingStackPanel vsp = itemsHost as VirtualizingStackPanel;
                        if (vsp != null)
                        {
                            vsp.AnchoredInvalidateMeasure();
                        }
                        else
                        {
                            itemsHost.InvalidateMeasure();
                        }
                    }
                }
            }
        }// *** END DEAD CODE ***

        // This class reprents an item value that arises from a non-local source (e.g. current-value)
        private class ModifiedItemValue
        {
            public ModifiedItemValue(object value, FullValueSource valueSource)
            {
                _value = value;
                _valueSource = valueSource;
            }

            public object Value { get { return _value; } }

            public bool IsCoercedWithCurrentValue
            {
                get { return (_valueSource & FullValueSource.IsCoercedWithCurrentValue) != 0; }
            }

            object _value;
            FullValueSource _valueSource;
        }

        #endregion

        internal static void ClearVirtualizingElement(IHierarchicalVirtualizationAndScrollInfo virtualizingElement)
        {
            Debug.Assert(virtualizingElement != null, "Must have a virtualizingElement to clear");

            virtualizingElement.ItemDesiredSizes = new HierarchicalVirtualizationItemDesiredSizes();
            virtualizingElement.MustDisableVirtualization = false;
        }

        /// <summary>
        /// Walk through the templated chilren tree of an element until a child of type T is found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchStart">element from where the tree walk starts</param>
        /// <param name="templatedParent">TemplatedParent of all elements</param>
        /// <returns></returns>
        internal static T FindTemplatedDescendant<T>(FrameworkElement searchStart, FrameworkElement templatedParent) where T : FrameworkElement
        {
            FrameworkElement descendant = null;
            T found = null;
            // Do a DFS among templated children
            int count = VisualTreeHelper.GetChildrenCount(searchStart);
            for (int i = 0; (i < count) && (found == null); i++)
            {
                descendant = VisualTreeHelper.GetChild(searchStart, i) as FrameworkElement;
                if (descendant != null && descendant.TemplatedParent == templatedParent)
                {
                    T returnTypeElement = descendant as T;
                    if (returnTypeElement != null)
                    {
                        found = returnTypeElement;
                    }
                    else
                    {
                        found = FindTemplatedDescendant<T>(descendant, templatedParent);
                    }
                }
            }

            return found;
        }

        /// <summary>
        ///     Walks up the visual parent tree looking for a parent type.
        /// </summary>
        internal static T FindVisualAncestor<T>(DependencyObject element, Func<DependencyObject, bool> shouldContinueFunc) where T : DependencyObject
        {
            while (element != null)
            {
                element = VisualTreeHelper.GetParent(element);
                T correctlyTyped = element as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                if (!shouldContinueFunc(element))
                {
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Invalidates measure on visual tree from pathStartElement to pathEndElement
        /// </summary>
        /// <param name="pathStartElement">descendant to start invalidation from</param>
        /// <param name="pathEndElement">ancestor to stop invalidation at</param>
        internal static void InvalidateMeasureOnPath(DependencyObject pathStartElement, DependencyObject pathEndElement, bool duringMeasure)
        {
            InvalidateMeasureOnPath(pathStartElement, pathEndElement, duringMeasure, false /*includePathEnd*/);
        }

        /// <summary>
        /// Invalidates measure on visual tree from pathStartElement to pathEndElement
        /// </summary>
        /// <param name="pathStartElement">descendant to start invalidation from</param>
        /// <param name="pathEndElement">ancestor to stop invalidation at</param>
        internal static void InvalidateMeasureOnPath(DependencyObject pathStartElement, DependencyObject pathEndElement, bool duringMeasure, bool includePathEnd)
        {
            Debug.Assert(VisualTreeHelper.IsAncestorOf(pathEndElement, pathStartElement), "pathEndElement should be an ancestor of pathStartElement");

            DependencyObject element = pathStartElement;

            // Includes pathStartElement
            // Includes pathEndElement conditionally
            while (element != null)
            {
                if (!includePathEnd &&
                    element == pathEndElement)
                {
                    break;
                }
                UIElement uiElement = element as UIElement;
                if (uiElement != null)
                {
                    //
                    //Please note that this method makes an internal call because
                    // it is expected to only be called when in a measure pass and
                    // hence doesnt require these items to be explicitly added to
                    // the layout queue.
                    //
                    if (duringMeasure)
                    {
                        uiElement.InvalidateMeasureInternal();
                    }
                    else
                    {
                        uiElement.InvalidateMeasure();
                    }
                }

                if (element == pathEndElement)
                {
                    break;
                }

                element = VisualTreeHelper.GetParent(element);
            }
        }

        internal static void InvalidateMeasureForSubtree(DependencyObject d)
        {
            UIElement element = d as UIElement;
            if (element != null)
            {
                if (element.MeasureDirty)
                {
                    return;
                }

                //
                //Please note that this method makes an internal call because
                // it is expected to only be called when in a measure pass and
                // hence doesnt require these items to be explicitly added to
                // the layout queue.
                //
                element.InvalidateMeasureInternal();
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(d);
            for (int i=0; i<childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(d, i);
                if (child != null)
                {
                    InvalidateMeasureForSubtree(child);
                }
            }
        }

        /// <summary>
        ///     Walks up the visual parent tree looking ancestor.
        ///     If we are out of visual parent it switches over to the logical parent.
        /// </summary>
        /// <param name="ancestor"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static bool IsAnyAncestorOf(DependencyObject ancestor, DependencyObject element)
        {
            if (ancestor == null || element == null)
            {
                return false;
            }
            return FindAnyAncestor(element, delegate(DependencyObject d) { return d == ancestor; }) != null;
        }

        /// <summary>
        ///     Walks up the visual parent tree matching the given predicate.
        ///     If we are out of visual parents it switches over to the logical parent.
        /// </summary>
        internal static DependencyObject FindAnyAncestor(DependencyObject element,
            Predicate<DependencyObject> predicate)
        {
            while (element != null)
            {
                element = GetAnyParent(element);
                if (element != null && predicate(element))
                {
                    return element;
                }
            }
            return null;
        }

        /// <summary>
        ///     Returns visual parent if possible else
        ///     logical parent of the element.
        /// </summary>
        internal static DependencyObject GetAnyParent(DependencyObject element)
        {
            DependencyObject parent = null;
            if (!(element is ContentElement))
            {
                parent = VisualTreeHelper.GetParent(element);
            }
            if (parent == null)
            {
                parent = LogicalTreeHelper.GetParent(element);
            }
            return parent;
        }

        /// <summary>
        ///     Returns if the value source of the given property
        ///     on the given element is Default or not.
        /// </summary>
        internal static bool IsDefaultValue(DependencyProperty dp, DependencyObject element)
        {
            bool hasModifiers;
            return element.GetValueSource(dp, null, out hasModifiers) == BaseValueSourceInternal.Default;
        }


        /// <summary>
        ///     Return true if a TSF composition is in progress on the given
        ///     property of the given element.
        /// </summary>
        internal static bool IsComposing(DependencyObject d, DependencyProperty dp)
        {
            if (dp != TextBox.TextProperty)
                return false;

            return IsComposing(d as TextBoxBase);
        }

        internal static bool IsComposing(TextBoxBase tbb)
        {
            if (tbb == null)
                return false;

            System.Windows.Documents.TextEditor te = tbb.TextEditor;
            if (te == null)
                return false;

            System.Windows.Documents.TextStore ts = te.TextStore;
            if (ts == null)
                return false;

            return ts.IsEffectivelyComposing;
        }


        // The following unused API has been removed (caught by FxCop).  If needed, recall from history.
        //
        // internal static Condition BuildCondition(DependencyProperty property, object value)
        //
        // internal static MultiTrigger BuildMultiTrigger(ArrayList triggerCollection,
        //                                  string target, DP targetProperty, object targetValue)
        //
        // internal static Trigger BuildTrigger( DependencyProperty triggerProperty, object triggerValue,
        //                              string target, DependencyProperty targetProperty, object targetValue )
        //
        // internal static bool CheckWriteableContainerStatus()
        //
        // internal static bool IsCallerOfType(Type type)
        //
        // public static bool IsInStaticConstructorOfType(Type t)
        //
        // private static bool IsInWindowCollection(object window)
        //
        // internal static bool IsMetroContainer
        //
        // internal static bool IsRootElement(DependencyObject node)

        //------------------------------------------------------
        //
        //  Private Enums, Structs, Constants
        //
        //------------------------------------------------------

        static readonly Type   NullableType = Type.GetType("System.Nullable`1");
        // ItemValueStorage.  For each data item it stores a list of (DP, value) pairs that we want to preserve on the container.
        private static readonly UncommonField<WeakDictionary<object, List<KeyValuePair<int, object>>>> ItemValueStorageField =
                            new UncommonField<WeakDictionary<object, List<KeyValuePair<int, object>>>>();

        // Since ItemValueStorage is private and only used for TreeView and Grouping virtualization we hardcode the DPs that we'll store in it.
        // If we make this available as a service to the rest of the platform we'd come up with some sort of DP registration mechanism.
        private static readonly int[] ItemValueStorageIndices = new int[] {
            ItemValueStorageField.GlobalIndex,
            TreeViewItem.IsExpandedProperty.GlobalIndex,
            Expander.IsExpandedProperty.GlobalIndex,
            GroupItem.DesiredPixelItemsSizeCorrectionFactorField.GlobalIndex,
            VirtualizingStackPanel.ItemsHostInsetProperty.GlobalIndex};
    }
}


