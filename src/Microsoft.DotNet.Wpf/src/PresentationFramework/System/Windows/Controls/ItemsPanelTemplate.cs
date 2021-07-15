// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ItemsPanelTemplate describes how ItemsPresenter creates the panel
//     that manages layout of containers for an ItemsControl.
//

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Diagnostics;

namespace System.Windows.Controls
{
    /// <summary>
    ///     ItemsPanelTemplate describes how ItemsPresenter creates the panel
    ///     that manages layout of containers for an ItemsControl.
    /// </summary>
    public class ItemsPanelTemplate : FrameworkTemplate
    {
        #region Constructors

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     ItemsPanelTemplate Constructor
        /// </summary>
        public ItemsPanelTemplate()
        {
        }

        /// <summary>
        ///     ItemsPanelTemplate Constructor
        /// </summary>
        public ItemsPanelTemplate(FrameworkElementFactory root)
        {
            VisualTree = root;
        }

        #endregion Constructors

        #region Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #endregion PublicProperties

        #region Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        //
        //  TargetType for ItemsPanelTemplate. This is override is
        //  so FrameworkTemplate can see this property.
        //
        internal override Type TargetTypeInternal
        {
            get {  return DefaultTargetType; }
        }

        // Subclasses must provide a way for the parser to directly set the
        // target type.  For ItemsPanelTemplate, this is not allowed.
        internal override void SetTargetTypeInternal(Type targetType)
        {
            throw new InvalidOperationException(SR.Get(SRID.TemplateNotTargetType));
        }

        // Target type of ItemsPanelTemplate is ItemsPresenter
        static internal Type DefaultTargetType
        {
            get { return typeof(ItemsPresenter); }
        }

        #endregion Internal Properties

        #region Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        //
        // ProcessTemplateBeforeSeal
        //
        // This is used in the case of templates defined with FEFs.  For templates
        // in Baml (the typical case), see the OnApply override.
        //
        // 1. Verify that
        //      a. root element is a Panel
        // 2. Set IsItemsHost = true
        //

        internal override void ProcessTemplateBeforeSeal()
        {
            FrameworkElementFactory root;

            if( HasContent )
            {
                // This is a Baml-style template

                // Validate the root type (it must be a Panel)

                TemplateContent templateHolder = Template as TemplateContent;
                System.Xaml.XamlType panelType = templateHolder.SchemaContext.GetXamlType(typeof(Panel));
                if (templateHolder.RootType == null || !templateHolder.RootType.CanAssignTo(panelType))
                {
                    throw new InvalidOperationException(SR.Get(SRID.ItemsPanelNotAPanel, templateHolder.RootType));
                }
            }

            else if ((root = this.VisualTree) != null)
            {
                // This is a FEF-style template
                if (!typeof(Panel).IsAssignableFrom(root.Type))
                    throw new InvalidOperationException(SR.Get(SRID.ItemsPanelNotAPanel, root.Type));

                root.SetValue(Panel.IsItemsHostProperty, true);
            }
        }


        #endregion Internal Methods

        #region Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     Validate against the following rules
        ///     1. Must have a non-null feTemplatedParent
        ///     2. A ItemsPanelTemplate must be applied to a ContentPresenter
        /// </summary>
        protected override void ValidateTemplatedParent(FrameworkElement templatedParent)
        {
            // Must have a non-null feTemplatedParent
            if (templatedParent == null)
            {
                throw new ArgumentNullException("templatedParent");
            }

            // A ItemsPanelTemplate must be applied to an ItemsPresenter
            if (!(templatedParent is ItemsPresenter))
            {
                throw new ArgumentException(SR.Get(SRID.TemplateTargetTypeMismatch, "ItemsPresenter", templatedParent.GetType().Name));
            }
        }

        #endregion Protected Methods
    }
}


