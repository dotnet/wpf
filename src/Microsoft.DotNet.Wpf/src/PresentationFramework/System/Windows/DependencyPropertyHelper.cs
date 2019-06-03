// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Helper class for miscellaneous framework-level features related
// to DependencyProperties.
//
// See spec at GetValueSource.mht
//

using System;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// Source of a DependencyProperty value.
    /// </summary>
    public enum BaseValueSource
    {
        /// <summary> The source is not known by the Framework. </summary>
        Unknown                 = BaseValueSourceInternal.Unknown,
        /// <summary> Default value, as defined by property metadata. </summary>
        Default                 = BaseValueSourceInternal.Default,
        /// <summary> Inherited from an ancestor. </summary>
        Inherited               = BaseValueSourceInternal.Inherited,
        /// <summary> Default Style for the current theme. </summary>
        DefaultStyle            = BaseValueSourceInternal.ThemeStyle,
        /// <summary> Trigger in the default Style for the current theme. </summary>
        DefaultStyleTrigger     = BaseValueSourceInternal.ThemeStyleTrigger,
        /// <summary> Style setter. </summary>
        Style                   = BaseValueSourceInternal.Style,
        /// <summary> Trigger in the Template. </summary>
        TemplateTrigger         = BaseValueSourceInternal.TemplateTrigger,
        /// <summary> Trigger in the Style. </summary>
        StyleTrigger            = BaseValueSourceInternal.StyleTrigger,
        /// <summary> Implicit Style reference. </summary>
        ImplicitStyleReference  = BaseValueSourceInternal.ImplicitReference,
        /// <summary> Template that created the element. </summary>
        ParentTemplate          = BaseValueSourceInternal.ParentTemplate,
        /// <summary> Trigger in the Template that created the element. </summary>
        ParentTemplateTrigger   = BaseValueSourceInternal.ParentTemplateTrigger,
        /// <summary> Local value. </summary>
        Local                   = BaseValueSourceInternal.Local,
    }

    /// <summary>
    /// This struct contains the information returned from
    /// DependencyPropertyHelper.GetValueSource.
    /// </summary>
    public struct ValueSource
    {
        internal ValueSource(BaseValueSourceInternal source, bool isExpression, bool isAnimated, bool isCoerced, bool isCurrent)
        {
            // this cast is justified because the public BaseValueSource enum
            // values agree with the internal BaseValueSourceInternal enum values.
            _baseValueSource = (BaseValueSource)source;

            _isExpression = isExpression;
            _isAnimated = isAnimated;
            _isCoerced = isCoerced;
            _isCurrent = isCurrent;
        }

        /// <summary>
        /// The base value source.
        /// </summary>
        public BaseValueSource BaseValueSource
        {
            get { return _baseValueSource; }
        }

        /// <summary>
        /// True if the value came from an Expression.
        /// </summary>
        public bool IsExpression
        {
            get { return _isExpression; }
        }

        /// <summary>
        /// True if the value came from an animation.
        /// </summary>
        public bool IsAnimated
        {
            get { return _isAnimated; }
        }

        /// <summary>
        /// True if the value was coerced.
        /// </summary>
        public bool IsCoerced
        {
            get { return _isCoerced; }
        }

        /// <summary>
        /// True if the value was set by SetCurrentValue.
        /// </summary>
        public bool IsCurrent
        {
            get { return _isCurrent; }
        }

        #region Object overrides - required by FxCop

        /// <summary>
        /// Return the hash code for this ValueSource.
        /// </summary>
        public override int GetHashCode()
        {
            return _baseValueSource.GetHashCode();
        }

        /// <summary>
        /// True if this ValueSource equals the argument.
        /// </summary>
        public override bool Equals(object o)
        {
            if (o is ValueSource)
            {
                ValueSource that = (ValueSource)o;

                return  this._baseValueSource == that._baseValueSource &&
                        this._isExpression == that._isExpression &&
                        this._isAnimated == that._isAnimated &&
                        this._isCoerced == that._isCoerced;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// True if the two arguments are equal.
        /// </summary>
        public static bool operator==(ValueSource vs1, ValueSource vs2)
        {
            return vs1.Equals(vs2);
        }

        /// <summary>
        /// True if the two arguments are unequal.
        /// </summary>
        public static bool operator!=(ValueSource vs1, ValueSource vs2)
        {
            return !vs1.Equals(vs2);
        }

        #endregion Object overrides - required by FxCop

        BaseValueSource _baseValueSource;
        bool            _isExpression;
        bool            _isAnimated;
        bool            _isCoerced;
        bool            _isCurrent;
    }

    /// <summary>
    /// Helper class for miscellaneous framework-level features related
    /// to DependencyProperties.
    /// </summary>
    public static class DependencyPropertyHelper
    {
        /// <summary>
        /// Return the source of the value for the given property.
        /// </summary>
        public static ValueSource GetValueSource(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            if (dependencyObject == null)
                throw new ArgumentNullException(nameof(dependencyObject));
            if (dependencyProperty == null)
                throw new ArgumentNullException(nameof(dependencyProperty));

            dependencyObject.VerifyAccess();

            bool hasModifiers, isExpression, isAnimated, isCoerced, isCurrent;
            BaseValueSourceInternal source = dependencyObject.GetValueSource(dependencyProperty, null, out hasModifiers, out isExpression, out isAnimated, out isCoerced, out isCurrent);

            return new ValueSource(source, isExpression, isAnimated, isCoerced, isCurrent);
        }

        /// <summary>
        /// Returns true if the given element belongs to an instance of a template
        /// that defines a value for the given property that may change during runtime
        /// based on changes elsewhere.
        /// For example, values set by Binding, TemplateBinding, or DynamicResource.
        /// </summary>
        /// <param name="elementInTemplate">element belonging to a template instance</param>
        /// <param name="dependencyProperty">property</param>
        /// <remarks>
        /// This method provides more detailed information in cases where
        /// the BaseValueSource is ParentTemplate.  The information is primarily
        /// of use to diagnostic tools.
        /// </remarks>
        public static bool IsTemplatedValueDynamic(DependencyObject elementInTemplate, DependencyProperty dependencyProperty)
        {
            if (elementInTemplate == null)
                throw new ArgumentNullException(nameof(elementInTemplate));

            if (dependencyProperty == null)
                throw new ArgumentNullException(nameof(dependencyProperty));

            FrameworkObject child = new FrameworkObject(elementInTemplate);
            DependencyObject templatedParent = child.TemplatedParent;

            if (templatedParent == null)
            {
                throw new ArgumentException(SR.Get(SRID.ElementMustBelongToTemplate), nameof(elementInTemplate));
            }

            int templateChildIndex = child.TemplateChildIndex;
            return StyleHelper.IsValueDynamic(templatedParent, templateChildIndex, dependencyProperty);
        }
    }
}
