// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Description of UI for grouping.
//
// See spec at Grouping.mht
//

using System.ComponentModel;    // [DefaultValue]
using System.Windows.Data;      // CollectionViewGroup

namespace System.Windows.Controls
{
    /// <summary>
    /// The GroupStyle describes how to display the items in a GroupCollection,
    /// such as the collection obtained from CollectionViewGroup.Items.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string
    public class GroupStyle : INotifyPropertyChanged
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of GroupStyle.
        /// </summary>
        public GroupStyle()
        {
        }

        static GroupStyle()
        {
            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            template.Seal();
            DefaultGroupPanel = template;
            DefaultStackPanel = template;

            template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            template.Seal();
            DefaultVirtualizingStackPanel = template;

            s_DefaultGroupStyle = new GroupStyle();
        }

        #endregion Constructors

        #region INotifyPropertyChanged

        /// <summary>
        ///     This event is raised when a property of the group style has changed.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }
            remove
            {
                PropertyChanged -= value;
            }
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A subclass can call this method to raise the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        #endregion INotifyPropertyChanged

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// A template that creates the panel used to layout the items.
        /// </summary>
        public ItemsPanelTemplate Panel
        {
            get { return _panel; }
            set
            {
                _panel = value;
                OnPropertyChanged("Panel");
            }
        }

        /// <summary>
        ///     ContainerStyle is the style that is applied to the GroupItem generated
        ///     for each item.
        /// </summary>
        [DefaultValue(null)]
        public Style ContainerStyle
        {
            get { return _containerStyle; }
            set { _containerStyle = value;  OnPropertyChanged("ContainerStyle"); }
        }

        /// <summary>
        ///     ContainerStyleSelector allows the app writer to provide custom style selection logic
        ///     for a style to apply to each generated GroupItem.
        /// </summary>
        [DefaultValue(null)]
        public StyleSelector ContainerStyleSelector
        {
            get { return _containerStyleSelector; }
            set { _containerStyleSelector = value;  OnPropertyChanged("ContainerStyleSelector"); }
        }

        /// <summary>
        ///     HeaderTemplate is the template used to display the group header.
        /// </summary>
        [DefaultValue(null)]
        public DataTemplate HeaderTemplate
        {
            get { return _headerTemplate; }
            set { _headerTemplate = value;  OnPropertyChanged("HeaderTemplate"); }
        }

        /// <summary>
        ///     HeaderTemplateSelector allows the app writer to provide custom selection logic
        ///     for a template used to display the group header.
        /// </summary>
        [DefaultValue(null)]
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return _headerTemplateSelector; }
            set { _headerTemplateSelector = value;  OnPropertyChanged("HeaderTemplateSelector"); }
        }

        /// <summary>
        ///     HeaderStringFormat is the format used to display the header content as a string.
        ///     This arises only when no template is available.
        /// </summary>
        [DefaultValue(null)]
        public String HeaderStringFormat
        {
            get { return _headerStringFormat; }
            set { _headerStringFormat = value;  OnPropertyChanged("HeaderStringFormat"); }
        }

        /// <summary>
        /// HidesIfEmpty allows the app writer to indicate whether items corresponding
        /// to empty groups should be displayed.
        /// </summary>
        [DefaultValue(false)]
        public bool HidesIfEmpty
        {
            get { return _hidesIfEmpty; }
            set { _hidesIfEmpty = value;  OnPropertyChanged("HidesIfEmpty"); }
        }

        /// <summary>
        /// AlternationCount controls the range of values assigned to the
        /// ItemsControl.AlternationIndex property on containers generated
        /// for this level of grouping.
        [DefaultValue(0)]
        public int AlternationCount
        {
            get { return _alternationCount; }
            set
            {
                _alternationCount = value;
                _isAlternationCountSet = true;
                OnPropertyChanged("AlternationCount");
            }
        }

       /// <summary>The default panel template.</summary>
        public static readonly ItemsPanelTemplate DefaultGroupPanel;

        /// <summary>The default GroupStyle.</summary>
        public static GroupStyle Default
        {
            get { return s_DefaultGroupStyle; }
        }

        #endregion Public Properties

        #region Private Properties
        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        internal bool IsAlternationCountSet
        {
            get { return _isAlternationCountSet; }
        }

        #endregion Private Properties

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        ItemsPanelTemplate      _panel;
        Style                   _containerStyle;
        StyleSelector           _containerStyleSelector;
        DataTemplate            _headerTemplate;
        DataTemplateSelector    _headerTemplateSelector;
        string                  _headerStringFormat;
        bool                    _hidesIfEmpty;
        bool                    _isAlternationCountSet;
        int                     _alternationCount;

        static GroupStyle       s_DefaultGroupStyle;

        /// <summary>The default panel template.</summary>
        internal static ItemsPanelTemplate DefaultStackPanel;
        internal static ItemsPanelTemplate DefaultVirtualizingStackPanel;

        #endregion Private Fields
    }

    /// <summary>
    /// A delegate to select the group style as a function of the
    /// parent group and its level.
    /// </summary>
    public delegate GroupStyle GroupStyleSelector(CollectionViewGroup group, int level);
}
