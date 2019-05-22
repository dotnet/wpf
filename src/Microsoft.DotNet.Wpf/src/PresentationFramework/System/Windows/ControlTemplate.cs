// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   A generic class that allow instantiation of a tree of Framework[Content]Elements. 
//   This is used to specify an overall master template for a Control. The control 
//   author defines the default ControlTemplate and the app author can override this.
//
//

using System.Diagnostics;               // Debug
using System.Windows.Controls;          // Control
using System.Windows.Media.Animation;   // Timeline
using System.Windows.Navigation;        // PageFunctionBase
using System.ComponentModel;            // DesignerSerializationVisibilityAttribute & DefaultValue
using System.Windows.Markup;     // DependsOnAttribute

namespace System.Windows.Controls
{
    /// <summary>
    ///     A generic class that allow instantiation of a tree of 
    ///     Framework[Content]Elements. This is used to specify an 
    ///     overall master template for a Control. The control author 
    ///     defines the default ControlTemplate and the app author 
    ///     can override this.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    [DictionaryKeyProperty("TargetType")]
    public class ControlTemplate : FrameworkTemplate
    {
        #region Construction

        /// <summary>
        ///     ControlTemplate Constructor
        /// </summary>
        public ControlTemplate()
        {
        }

        /// <summary>
        ///     ControlTemplate Constructor
        /// </summary>
        public ControlTemplate(Type targetType)
        {
            ValidateTargetType(targetType, "targetType");
            _targetType = targetType;
        }
        
        #endregion Construction

        #region PublicMethods

        /// <summary>
        ///     Validate against the following rules
        ///     1. One cannot use a ControlTemplate to template a FrameworkContentElement
        ///     2. One cannot use a ControlTemplate to template a FrameworkElement other than a Control
        ///     3. One cannot use a ControlTemplate to template a Control that isn't associated with it
        /// </summary>
        protected override void ValidateTemplatedParent(FrameworkElement templatedParent)
        {
            // Must have a non-null feTemplatedParent
            if (templatedParent == null)
            {
                throw new ArgumentNullException("templatedParent");
            }

            // The target type of a ControlTemplate must match the 
            // type of the Control that it is being applied to
            if (_targetType != null && !_targetType.IsInstanceOfType(templatedParent))
            {
                throw new ArgumentException(SR.Get(SRID.TemplateTargetTypeMismatch, _targetType.Name, templatedParent.GetType().Name));
            }

            // One cannot use a ControlTemplate to template a Control that isn't associated with it
            if (templatedParent.TemplateInternal != this)
            {
                throw new ArgumentException(SR.Get(SRID.MustNotTemplateUnassociatedControl));
            }
        }
        #endregion PublicMethods

        #region PublicProperties

        /// <summary>
        ///     TargetType for this ControlTemplate
        /// </summary>
        [Ambient]
        [DefaultValue(null)]
        public Type TargetType
        {
            get {  return _targetType; }
            set
            {
                ValidateTargetType(value, "value");
                CheckSealed();
                _targetType = value;
            }
        }
        
        /// <summary>
        ///     Collection of Triggers
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [DependsOn("VisualTree")]
        [DependsOn("Template")]
        public TriggerCollection Triggers
        {
            get 
            { 
                if (_triggers == null)
                {
                    _triggers = new TriggerCollection();

                    // If the template has been sealed prior to this the newly 
                    // created TriggerCollection also needs to be sealed
                    if (IsSealed)
                    {
                        _triggers.Seal();
                    }
                }
                return _triggers; 
            }
        }

        #endregion PublicProperties

        #region NonPublicMethods

        // Validate against two rules
        //  1. targetType must not null
        //  2. targetType must be a Control or a subclass of it
        private void ValidateTargetType(Type targetType, string argName)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(argName);
            }
            if (!typeof(Control).IsAssignableFrom(targetType) &&
                !typeof(Page).IsAssignableFrom(targetType) &&
                !typeof(PageFunctionBase).IsAssignableFrom(targetType))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidControlTemplateTargetType, targetType.Name));
            }
        }
        
        #endregion NonPublicMethods

        #region NonPublicProperties
        
        //
        //  TargetType for ControlTemplate. This is override is 
        //  so FrameworkTemplate can see this property.
        //
        internal override Type TargetTypeInternal
        {
            get 
            {  
                if (TargetType != null)
                {
                    return TargetType; 
                }

                return DefaultTargetType;
            }
        }

        // Subclasses must provide a way for the parser to directly set the
        // target type.
        internal override void SetTargetTypeInternal(Type targetType)
        {
            TargetType = targetType;
        }

        //
        //  Collection of Triggers for a ControlTemplate. This is 
        //  override is so FrameworkTemplate can see this property.
        //
        internal override TriggerCollection TriggersInternal
        {
            get { return Triggers; }
        }

        #endregion NonPublicProperties

        #region Data

        private Type                    _targetType;
        private TriggerCollection       _triggers;
        
        // Target type is FrameworkElement by default
        internal static readonly Type DefaultTargetType = typeof(Control);

        #endregion Data
    }
}

