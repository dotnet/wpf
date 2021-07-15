// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Windows.Controls
{
#if RIBBON_IN_FRAMEWORK
    using MS.Internal;
#endif
    using System.Collections.Generic;
    using System.Windows;

    internal static class PropertyHelper
    {
        public static bool IsDefaultValue(DependencyObject d, DependencyProperty dp)
        {
            return DependencyPropertyHelper.GetValueSource(d, dp).BaseValueSource == BaseValueSource.Default;
        }

        public static object GetCoercedTransferPropertyValue(
            DependencyObject baseObject,
            object baseValue,
            DependencyProperty baseProperty,
            DependencyObject parentObject,
            DependencyProperty parentProperty)
        {
            return GetCoercedTransferPropertyValue(
                baseObject,
                baseValue,
                baseProperty,
                parentObject,
                parentProperty,
                null,
                null);
        }

        /// <summary>
        ///     Computes the value of a given property based on the property transfer rules.
        /// </summary>
        /// <remarks>
        ///     This is intended to be called from within the coercion of the baseProperty.
        /// </remarks>
        /// <param name="baseObject">The target object which recieves the transferred property</param>
        /// <param name="baseValue">The baseValue that was passed into the coercion delegate</param>
        /// <param name="baseProperty">The property that is being coerced</param>
        /// <param name="parentObject">The object that contains the parentProperty</param>
        /// <param name="parentProperty">A property who's value should be transfered (via coercion) to the baseObject if it has a higher precedence.</param>
        /// <param name="grandParentObject">Same as parentObject but evaluated at a lower presedece for a given BaseValueSource</param>
        /// <param name="grandParentProperty">Same as parentProperty but evaluated at a lower presedece for a given BaseValueSource</param>
        /// <returns></returns>
        public static object GetCoercedTransferPropertyValue(
            DependencyObject baseObject,
            object baseValue,
            DependencyProperty baseProperty,
            DependencyObject parentObject,
            DependencyProperty parentProperty,
            DependencyObject grandParentObject,
            DependencyProperty grandParentProperty)
        {
            // Transfer Property Coercion rules:
            //
            // Determine if this is a 'Transfer Property Coercion'.  If so:
            //   We can safely get the BaseValueSource because the property change originated from another
            //   property, and thus this BaseValueSource wont be stale.
            //   Pick a value to use based on who has the greatest BaseValueSource
            // If not a 'Transfer Property Coercion', simply return baseValue.  This will cause a property change if the value changes, which
            // will trigger a 'Transfer Property Coercion', and we will no longer have a stale BaseValueSource
            var coercedValue = baseValue;

            if (IsPropertyTransferEnabled(baseObject, baseProperty))
            {
                var propertySource = DependencyPropertyHelper.GetValueSource(baseObject, baseProperty);
                var maxBaseValueSource = propertySource.BaseValueSource;

                if (parentObject != null)
                {
                    var parentPropertySource = DependencyPropertyHelper.GetValueSource(parentObject, parentProperty);

                    if (parentPropertySource.BaseValueSource > maxBaseValueSource)
                    {
                        coercedValue = parentObject.GetValue(parentProperty);
                        maxBaseValueSource = parentPropertySource.BaseValueSource;
                    }
                }

                if (grandParentObject != null)
                {
                    var grandParentPropertySource = DependencyPropertyHelper.GetValueSource(grandParentObject, grandParentProperty);

                    if (grandParentPropertySource.BaseValueSource > maxBaseValueSource)
                    {
                        coercedValue = grandParentObject.GetValue(grandParentProperty);
                        maxBaseValueSource = grandParentPropertySource.BaseValueSource;
                    }
                }
            }

            return coercedValue;
        }

        /// <summary>
        ///     Causes the given DependencyProperty to be coerced in transfer mode.
        /// </summary>
        /// <remarks>
        ///     This should be called from within the target object's NotifyPropertyChanged.  It MUST be called in
        ///     response to a change in the target property.
        /// </remarks>
        /// <param name="d">The DependencyObject which contains the property that needs to be transfered.</param>
        /// <param name="p">The DependencyProperty that is the target of the property transfer.</param>
        public static void TransferProperty(DependencyObject d, DependencyProperty p)
        {
            var transferEnabledMap = GetPropertyTransferEnabledMapForObject(d);
            transferEnabledMap[p] = true;
            d.CoerceValue(p);
            transferEnabledMap[p] = false;
        }

        private static Dictionary<DependencyProperty, bool> GetPropertyTransferEnabledMapForObject(DependencyObject d)
        {
            var propertyTransferEnabledForObject = _propertyTransferEnabledMap[d] as Dictionary<DependencyProperty, bool>;

            if (propertyTransferEnabledForObject == null)
            {
                propertyTransferEnabledForObject = new Dictionary<DependencyProperty, bool>();
                _propertyTransferEnabledMap.SetWeak(d, propertyTransferEnabledForObject);
            }

            return propertyTransferEnabledForObject;
        }

        internal static bool IsPropertyTransferEnabled(DependencyObject d, DependencyProperty p)
        {
            var propertyTransferEnabledForObject = _propertyTransferEnabledMap[d] as Dictionary<DependencyProperty, bool>;

            if (propertyTransferEnabledForObject != null)
            {
                bool isPropertyTransferEnabled;
                if (propertyTransferEnabledForObject.TryGetValue(p, out isPropertyTransferEnabled))
                {
                    return isPropertyTransferEnabled;
                }
            }

            return false;
        }

        /// <summary>
        ///     Tracks which properties are currently being transfered.  This information is needed when GetPropertyTransferEnabledMapForObject
        ///     is called inside of Coercion.
        /// </summary>
        private static WeakHashtable _propertyTransferEnabledMap = new WeakHashtable();
    }
}
