// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Media;

using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Data;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The base class for all controls that contain single content and have a header.
    /// </summary>
    /// <remarks>
    ///     HeaderedContentControl adds Header, HasHeader, HeaderTemplate, and HeaderTemplateSelector features to a ContentControl.
    /// </remarks>
    [Localizability(LocalizationCategory.Text)] // can contain localizable text
    public class HeaderedContentControl : ContentControl
    {
        #region Constructors

        static HeaderedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedContentControl), new FrameworkPropertyMetadata(typeof(HeaderedContentControl)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(HeaderedContentControl));
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        public HeaderedContentControl() : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the Header property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderProperty =
                DependencyProperty.Register(
                        "Header",
                        typeof(object),
                        typeof(HeaderedContentControl),
                        new FrameworkPropertyMetadata(
                                (object) null,
                                new PropertyChangedCallback(OnHeaderChanged)));


        /// <summary>
        ///     Header is the data used to for the header of each item in the control.
        /// </summary>
        [Bindable(true), Category("Content")]
        [Localizability(LocalizationCategory.Label)]
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedContentControl ctrl = (HeaderedContentControl) d;

            ctrl.SetValue(HasHeaderPropertyKey, (e.NewValue != null) ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox);
            ctrl.OnHeaderChanged(e.OldValue, e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Header property changes.
        /// </summary>
        /// <param name="oldHeader">The old value of the Header property.</param>
        /// <param name="newHeader">The new value of the Header property.</param>
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {
            RemoveLogicalChild(oldHeader);
            AddLogicalChild(newHeader);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey HasHeaderPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "HasHeader",
                        typeof(bool),
                        typeof(HeaderedContentControl),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the HasHeader property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HasHeaderProperty =
                HasHeaderPropertyKey.DependencyProperty;

        /// <summary>
        ///     True if Header is non-null, false otherwise.
        /// </summary>
        [Bindable(false), Browsable(false)]
        public bool HasHeader
        {
            get { return (bool) GetValue(HasHeaderProperty); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplate property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderTemplateProperty =
                DependencyProperty.Register(
                        "HeaderTemplate",
                        typeof(DataTemplate),
                        typeof(HeaderedContentControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplate) null,
                                new PropertyChangedCallback(OnHeaderTemplateChanged)));

        /// <summary>
        ///     HeaderTemplate is the template used to display the <seealso cref="Header"/>.
        /// </summary>
        [Bindable(true), Category("Content")]
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate) GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderTemplateProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedContentControl ctrl = (HeaderedContentControl)d;
            ctrl.OnHeaderTemplateChanged((DataTemplate) e.OldValue, (DataTemplate) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderTemplate property changes.
        /// </summary>
        /// <param name="oldHeaderTemplate">The old value of the HeaderTemplate property.</param>
        /// <param name="newHeaderTemplate">The new value of the HeaderTemplate property.</param>
        protected virtual void OnHeaderTemplateChanged(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate)
        {
            Helper.CheckTemplateAndTemplateSelector("Header", HeaderTemplateProperty, HeaderTemplateSelectorProperty, this);
        }


        /// <summary>
        ///     The DependencyProperty for the HeaderTemplateSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
                DependencyProperty.Register(
                        "HeaderTemplateSelector",
                        typeof(DataTemplateSelector),
                        typeof(HeaderedContentControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector) null,
                                new PropertyChangedCallback(OnHeaderTemplateSelectorChanged)));

        /// <summary>
        ///     HeaderTemplateSelector allows the application writer to provide custom logic
        ///     for choosing the template used to display the <seealso cref="Header"/>.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="HeaderTemplate"/> is set.
        /// </remarks>
        [Bindable(true), Category("Content")]
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderTemplateSelectorProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedContentControl ctrl = (HeaderedContentControl) d;

            ctrl.OnHeaderTemplateSelectorChanged((DataTemplateSelector) e.OldValue, (DataTemplateSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderTemplateSelector property changes.
        /// </summary>
        /// <param name="oldHeaderTemplateSelector">The old value of the HeaderTemplateSelector property.</param>
        /// <param name="newHeaderTemplateSelector">The new value of the HeaderTemplateSelector property.</param>
        protected virtual void OnHeaderTemplateSelectorChanged(DataTemplateSelector oldHeaderTemplateSelector, DataTemplateSelector newHeaderTemplateSelector)
        {
            Helper.CheckTemplateAndTemplateSelector("Header", HeaderTemplateProperty, HeaderTemplateSelectorProperty, this);
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderStringFormatProperty =
                DependencyProperty.Register(
                        "HeaderStringFormat",
                        typeof(String),
                        typeof(HeaderedContentControl),
                        new FrameworkPropertyMetadata(
                                (String) null,
                              new PropertyChangedCallback(OnHeaderStringFormatChanged)));


        /// <summary>
        ///     HeaderStringFormat is the format used to display the header content as a string.
        ///     This arises only when no template is available.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public String HeaderStringFormat
        {
            get { return (String) GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderStringFormatProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedContentControl ctrl = (HeaderedContentControl)d;
            ctrl.OnHeaderStringFormatChanged((String) e.OldValue, (String) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderStringFormat property changes.
        /// </summary>
        /// <param name="oldHeaderStringFormat">The old value of the HeaderStringFormat property.</param>
        /// <param name="newHeaderStringFormat">The new value of the HeaderStringFormat property.</param>
        protected virtual void OnHeaderStringFormatChanged(String oldHeaderStringFormat, String newHeaderStringFormat)
        {
        }

        #endregion

        #region LogicalTree

        /// <summary>
        ///     Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                object header = Header;

                if (HeaderIsNotLogical || header == null)
                {
                    return base.LogicalChildren;
                }

                return new HeaderedContentModelTreeEnumerator(this, ContentIsNotLogical ? null : Content, header);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        /// <returns></returns>
        internal override string GetPlainText()
        {
            return ContentControl.ContentObjectToString(Header);
        }

        /// <summary>
        ///    Indicates whether Header should be a logical child or not.
        /// </summary>
        internal bool HeaderIsNotLogical
        {
            get { return ReadControlFlag(ControlBoolFlags.HeaderIsNotLogical); }
            set { WriteControlFlag(ControlBoolFlags.HeaderIsNotLogical, value); }
        }

        /// <summary>
        ///    Indicates whether Header is a data item
        /// </summary>
        internal bool HeaderIsItem
        {
            get { return ReadControlFlag(ControlBoolFlags.HeaderIsItem); }
            set { WriteControlFlag(ControlBoolFlags.HeaderIsItem, value); }
        }


        /// <summary>
        /// Prepare to display the item.
        /// </summary>
        internal void PrepareHeaderedContentControl(object item,
                                        DataTemplate itemTemplate,
                                        DataTemplateSelector itemTemplateSelector,
                                        string stringFormat)
        {
            if (item != this)
            {
                // don't treat Content as a logical child
                ContentIsNotLogical = true;
                HeaderIsNotLogical = true;

                if (ContentIsItem || !HasNonDefaultValue(ContentProperty))
                {
                    Content = item;
                    ContentIsItem = true;
                }

                // Visuals can't be placed in both Header and Content, but data can
                if (!(item is Visual) && (HeaderIsItem || !HasNonDefaultValue(HeaderProperty)))
                {
                    Header = item;
                    HeaderIsItem = true;
                }

                if (itemTemplate != null)
                    SetValue(HeaderTemplateProperty, itemTemplate);
                if (itemTemplateSelector != null)
                    SetValue(HeaderTemplateSelectorProperty, itemTemplateSelector);
                if (stringFormat != null)
                    SetValue(HeaderStringFormatProperty, stringFormat);
            }
            else
            {
                ContentIsNotLogical = false;
            }
        }

        /// <summary>
        /// Undo the effect of PrepareHeaderedContentControl.
        /// </summary>
        internal void ClearHeaderedContentControl(object item)
        {
            if (item != this)
            {
                if (ContentIsItem)
                {
                    Content = BindingExpressionBase.DisconnectedItem;
                }

                if (HeaderIsItem)
                {
                    Header = BindingExpressionBase.DisconnectedItem;
                }
            }
        }

        #endregion

        #region Method Overrides


        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            string typeText = this.GetType().ToString();
            string headerText = String.Empty;
            string contentText = String.Empty;
            bool valuesDefined = false;

            // Accessing Header's content may be thread sensitive
            if (CheckAccess())
            {
                headerText = ContentControl.ContentObjectToString(Header);
                contentText = ContentControl.ContentObjectToString(Content);
                valuesDefined = true;
            }
            else
            {
                //Not on dispatcher, try posting to the dispatcher with 20ms timeout
                Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 0, 0, 20), new DispatcherOperationCallback(delegate(object o)
                {
                    headerText = ContentControl.ContentObjectToString(Header);
                    contentText = ContentControl.ContentObjectToString(Content);
                    valuesDefined = true;
                    return null;
                }), null);
            }

            // If header and content text are defined
            if (valuesDefined)
            {
                return SR.Get(SRID.ToStringFormatString_HeaderedContentControl, typeText, headerText, contentText);
            }

            // Not able to access the dispatcher
            return typeText;
        }

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered DefaultStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}
