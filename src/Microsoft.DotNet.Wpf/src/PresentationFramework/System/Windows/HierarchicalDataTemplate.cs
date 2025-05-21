// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: HierarchicalDataTemplate adds hierarchy support to DataTemplate.
//
// Specs:       http://avalon/coreui/Specs%20%20Property%20Engine/Styling%20Revisited.doc
//

using System.Windows.Controls;
using System.Windows.Data;

namespace System.Windows
{
    /// <summary>
    ///     HierarchicalDataTemplate adds hierarchy support to DataTemplate.
    /// </summary>
    public class HierarchicalDataTemplate : DataTemplate
    {
        #region Constructors

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     HierarchicalDataTemplate Constructor
        /// </summary>
        public HierarchicalDataTemplate()
        {
        }

        /// <summary>
        ///     HierarchicalDataTemplate Constructor
        /// </summary>
        public HierarchicalDataTemplate(object dataType) : base(dataType)
        {
        }

        #endregion Constructors

        #region Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     ItemsSource binding for this DataTemplate.  This is applied
        ///     to the ItemsSource property on a generated HeaderedItemsControl,
        ///     to indicate where to find the collection that represents the
        ///     next level in the data hierarchy.
        /// </summary>
        public BindingBase ItemsSource
        {
            get {  return _itemsSourceBinding; }
            set
            {
                CheckSealed();
                _itemsSourceBinding = value;
            }
        }

        /// <summary>
        ///     ItemTemplate for this DataTemplate.  This is applied
        ///     to the ItemTemplate property on a generated HeaderedItemsControl,
        ///     to indicate how to display items from the next level in the
        ///     data hierarchy.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get {  return _itemTemplate; }
            set
            {
                CheckSealed();
                _itemTemplate = value;
                _itemTemplateSet = true;
            }
        }

        /// <summary>
        ///     ItemTemplateSelector for this DataTemplate.  This is applied
        ///     to the ItemTemplateSelector property on a generated HeaderedItemsControl,
        ///     to indicate how to select a template to display items from the
        ///     next level in the data hierarchy.
        /// </summary>
        public DataTemplateSelector ItemTemplateSelector
        {
            get {  return _itemTemplateSelector; }
            set
            {
                CheckSealed();
                _itemTemplateSelector = value;
                _itemTemplateSelectorSet = true;
            }
        }

        /// <summary>
        ///     ItemContainerStyle for this DataTemplate.  This is applied
        ///     to the ItemContainerStyle property on a generated HeaderedItemsControl,
        ///     to indicate a style for the containers it generates.
        /// </summary>
        public Style ItemContainerStyle
        {
            get {  return _itemContainerStyle; }
            set
            {
                CheckSealed();
                _itemContainerStyle = value;
                _itemContainerStyleSet = true;
            }
        }

        /// <summary>
        ///     ItemContainerStyleSelector for this DataTemplate.  This is applied
        ///     to the ItemContainerStyleSelector property on a generated HeaderedItemsControl,
        ///     to indicate how to select a style for the containers it generates.
        /// </summary>
        public StyleSelector ItemContainerStyleSelector
        {
            get {  return _itemContainerStyleSelector; }
            set
            {
                CheckSealed();
                _itemContainerStyleSelector = value;
                _itemContainerStyleSelectorSet = true;
            }
        }

        /// <summary>
        ///     ItemStringFormat for this DataTemplate.  This is applied
        ///     to the ItemStringFormat property on a generated HeaderedItemsControl,
        ///     to indicate how to format items from the
        ///     next level in the data hierarchy.
        /// </summary>
        public string ItemStringFormat
        {
            get {  return _itemStringFormat; }
            set
            {
                CheckSealed();
                _itemStringFormat = value;
                _itemStringFormatSet = true;
            }
        }

        /// <summary>
        ///     AlternationCount for this DataTemplate.  This is applied
        ///     to the AlternationCount property on a generated HeaderedItemsControl,
        ///     to control the setting of AlternationIndex at the
        ///     next level in the data hierarchy.
        /// </summary>
        public int AlternationCount
        {
            get {  return _alternationCount; }
            set
            {
                CheckSealed();
                _alternationCount = value;
                _alternationCountSet = true;
            }
        }

        /// <summary>
        ///     ItemBindingGroup for this DataTemplate.  This is applied
        ///     to the ItemBindingGroup property on a generated HeaderedItemsControl,
        ///     to define the binding group used at the
        ///     next level in the data hierarchy.
        /// </summary>
        public BindingGroup ItemBindingGroup
        {
            get {  return _itemBindingGroup; }
            set
            {
                CheckSealed();
                _itemBindingGroup = value;
                _itemBindingGroupSet = true;
            }
        }

        #endregion Public Properties

        #region Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        internal bool IsItemTemplateSet
        {
            get { return _itemTemplateSet; }
        }

        internal bool IsItemTemplateSelectorSet
        {
            get { return _itemTemplateSelectorSet; }
        }

        internal bool IsItemContainerStyleSet
        {
            get { return _itemContainerStyleSet; }
        }

        internal bool IsItemContainerStyleSelectorSet
        {
            get { return _itemContainerStyleSelectorSet; }
        }

        internal bool IsItemStringFormatSet
        {
            get { return _itemStringFormatSet; }
        }

        internal bool IsAlternationCountSet
        {
            get { return _alternationCountSet; }
        }

        internal bool IsItemBindingGroupSet
        {
            get { return _itemBindingGroupSet; }
        }

        #endregion Internal Properties

        #region Data

        private BindingBase          _itemsSourceBinding;
        private DataTemplate         _itemTemplate;
        private DataTemplateSelector _itemTemplateSelector;
        private Style                _itemContainerStyle;
        private StyleSelector        _itemContainerStyleSelector;
        private string               _itemStringFormat;
        private int                  _alternationCount;
        private BindingGroup         _itemBindingGroup;

        private bool                 _itemTemplateSet;
        private bool                 _itemTemplateSelectorSet;
        private bool                 _itemContainerStyleSet;
        private bool                 _itemContainerStyleSelectorSet;
        private bool                 _itemStringFormatSet;
        private bool                 _alternationCountSet;
        private bool                 _itemBindingGroupSet;

        #endregion Data
    }
}

