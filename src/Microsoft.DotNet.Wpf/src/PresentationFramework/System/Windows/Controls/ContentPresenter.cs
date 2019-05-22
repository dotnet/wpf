// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ContentPresenter class
//
// Specs:      Data Styling.mht
//

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

using System.Windows.Threading;

using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Data;
using MS.Internal.KnownBoxes;
using System.Windows.Documents;

using MS.Utility;
using MS.Internal.PresentationFramework;
using System.Collections.Specialized;

namespace System.Windows.Controls
{
    /// <summary>
    /// ContentPresenter is used within the template of a content control to denote the
    /// place in the control's visual tree (control template) where the content
    /// is to be added.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public class ContentPresenter : FrameworkElement
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static ContentPresenter()
        {
            DataTemplate template;
            FrameworkElementFactory text;
            Binding binding;

            // Default template for strings when hosted in ContentPresener with RecognizesAccessKey=true
            template = new DataTemplate();
            text = CreateAccessTextFactory();
            text.SetValue(AccessText.TextProperty, new TemplateBindingExtension(ContentProperty));
            template.VisualTree = text;
            template.Seal();
            s_AccessTextTemplate = template;

            // Default template for strings
            template = new DataTemplate();
            text = CreateTextBlockFactory();
            text.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentProperty));
            template.VisualTree = text;
            template.Seal();
            s_StringTemplate = template;

            // Default template for XmlNodes
            template = new DataTemplate();
            text = CreateTextBlockFactory();
            binding = new Binding();
            binding.XPath = ".";
            text.SetBinding(TextBlock.TextProperty, binding);
            template.VisualTree = text;
            template.Seal();
            s_XmlNodeTemplate = template;

            // Default template for UIElements
            template = new UseContentTemplate();
            template.Seal();
            s_UIElementTemplate = template;

            // Default template for everything else
            template = new DefaultTemplate();
            template.Seal();
            s_DefaultTemplate = template;

            // Default template selector
            s_DefaultTemplateSelector = new DefaultSelector();
        }


        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ContentPresenter() : base()
        {
            Initialize();
        }

        void Initialize()
        {
            // Initialize the _templateCache to the default value for TemplateProperty.
            // If the default value is non-null then wire it to the current instance.
            PropertyMetadata metadata = TemplateProperty.GetMetadata(DependencyObjectType);
            DataTemplate defaultValue = (DataTemplate) metadata.DefaultValue;
            if (defaultValue != null)
            {
                OnTemplateChanged(this, new DependencyPropertyChangedEventArgs(TemplateProperty, metadata, null, defaultValue));
            }

            DataContext = null; // this presents a uniform view:  CP always has local DC
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        /// <summary>
        ///     The DependencyProperty for the RecognizesAccessKey property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty RecognizesAccessKeyProperty =
                DependencyProperty.Register(
                        "RecognizesAccessKey",
                        typeof(bool),
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Determine if ContentPresenter should use AccessText in its style
        /// </summary>
        public bool RecognizesAccessKey
        {
            get { return (bool) GetValue(RecognizesAccessKeyProperty); }
            set { SetValue(RecognizesAccessKeyProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     The DependencyProperty for the Content property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        // Any change in Content properties affectes layout measurement since
        // a new template may be used. On measurement,
        // ApplyTemplate will be invoked leading to possible application
        // of a new template.
        [CommonDependencyProperty]
        public static readonly DependencyProperty ContentProperty =
                ContentControl.ContentProperty.AddOwner(
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(
                            (object)null,
                            FrameworkPropertyMetadataOptions.AffectsMeasure,
                            new PropertyChangedCallback(OnContentChanged)));

        /// <summary>
        ///     Content is the data used to generate the child elements of this control.
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentControl.ContentProperty); }
            set { SetValue(ContentControl.ContentProperty, value); }
        }

        /// <summary>
        ///     Called when ContentProperty is invalidated on "d."
        /// </summary>
        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter ctrl = (ContentPresenter)d;

            // if we're already marked to reselect the template, there's nothing more to do
            if (!ctrl._templateIsCurrent)
                return;

            bool mismatch;

            if (e.NewValue == BindingExpressionBase.DisconnectedItem)
            {
                mismatch = false;       // do not change templates when disconnecting
            }
            else if (ctrl.ContentTemplate != null)
            {
                mismatch = false;       // explicit template - matches by fiat
            }
            else if (ctrl.ContentTemplateSelector != null)
            {
                mismatch = true;        // template selector - always re-select
            }
            else if (ctrl.Template == UIElementContentTemplate)
            {
                mismatch = true;        // direct template - always re-apply
                ctrl.Template = null;   // and release the old content so it can be re-used elsewhere
            }
            else if (ctrl.Template == DefaultContentTemplate)
            {
                mismatch = true;        // default template - always re-apply
            }
            else
            {
                // implicit template - matches if data types agree
                Type type;  // unused
                object oldDataType = DataTypeForItem(e.OldValue, ctrl, out type);
                object newDataType = DataTypeForItem(e.NewValue, ctrl, out type);
                mismatch = (oldDataType != newDataType);

                // but mismatch if we're displaying strings via a default template
                // and the presence of an AccessKey changes
                if (!mismatch &&
                    ctrl.RecognizesAccessKey &&
                    Object.ReferenceEquals(typeof(String), newDataType) &&
                    ctrl.IsUsingDefaultStringTemplate)
                {
                    String oldString = (String)e.OldValue;
                    String newString = (String)e.NewValue;
                    bool oldHasAccessKey = (oldString.IndexOf(AccessText.AccessKeyMarker) > -1);
                    bool newHasAccessKey = (newString.IndexOf(AccessText.AccessKeyMarker) > -1);

                    if (oldHasAccessKey != newHasAccessKey)
                    {
                        mismatch = true;
                    }
                }
            }

            // if the content and (old) template don't match, reselect the template
            if (mismatch)
            {
                ctrl._templateIsCurrent = false;
            }

            // keep the DataContext in sync with Content
            if (ctrl._templateIsCurrent && ctrl.Template != UIElementContentTemplate)
            {
                ctrl.DataContext = e.NewValue;
            }
        }


        /// <summary>
        ///     The DependencyProperty for the ContentTemplate property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ContentTemplateProperty =
                ContentControl.ContentTemplateProperty.AddOwner(
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(
                                (DataTemplate)null,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnContentTemplateChanged)));

        /// <summary>
        ///     ContentTemplate is the template used to display the content of the control.
        /// </summary>
        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate) GetValue(ContentControl.ContentTemplateProperty); }
            set { SetValue(ContentControl.ContentTemplateProperty, value); }
        }

        /// <summary>
        ///     Called when ContentTemplateProperty is invalidated on "d."
        /// </summary>
        private static void OnContentTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter ctrl = (ContentPresenter)d;
            ctrl._templateIsCurrent = false;
            ctrl.OnContentTemplateChanged((DataTemplate) e.OldValue, (DataTemplate) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ContentTemplate property changes.
        /// </summary>
        /// <param name="oldContentTemplate">The old value of the ContentTemplate property.</param>
        /// <param name="newContentTemplate">The new value of the ContentTemplate property.</param>
        protected virtual void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            Helper.CheckTemplateAndTemplateSelector("Content", ContentTemplateProperty, ContentTemplateSelectorProperty, this);

            // if ContentTemplate is really changing, remove the old template
            this.Template = null;
        }


        /// <summary>
        ///     The DependencyProperty for the ContentTemplateSelector property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ContentTemplateSelectorProperty =
                ContentControl.ContentTemplateSelectorProperty.AddOwner(
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector)null,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnContentTemplateSelectorChanged)));

        /// <summary>
        ///     ContentTemplateSelector allows the application writer to provide custom logic
        ///     for choosing the template used to display the content of the control.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="ContentTemplate"/> is set.
        /// </remarks>
        public DataTemplateSelector ContentTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(ContentControl.ContentTemplateSelectorProperty); }
            set { SetValue(ContentControl.ContentTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeContentTemplateSelector()
        {
            return false;
        }

        /// <summary>
        ///     Called when ContentTemplateSelectorProperty is invalidated on "d."
        /// </summary>
        private static void OnContentTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter ctrl = (ContentPresenter) d;
            ctrl._templateIsCurrent = false;
            ctrl.OnContentTemplateSelectorChanged((DataTemplateSelector) e.OldValue, (DataTemplateSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ContentTemplateSelector property changes.
        /// </summary>
        /// <param name="oldContentTemplateSelector">The old value of the ContentTemplateSelector property.</param>
        /// <param name="newContentTemplateSelector">The new value of the ContentTemplateSelector property.</param>
        protected virtual void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            Helper.CheckTemplateAndTemplateSelector("Content", ContentTemplateProperty, ContentTemplateSelectorProperty, this);

            // if ContentTemplateSelector is really changing (and in use), remove the old template
            this.Template = null;
        }

        /// <summary>
        ///     The DependencyProperty for the ContentStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ContentStringFormatProperty =
                DependencyProperty.Register(
                        "ContentStringFormat",
                        typeof(String),
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(
                                (String) null,
                              new PropertyChangedCallback(OnContentStringFormatChanged)));


        /// <summary>
        ///     ContentStringFormat is the format used to display the content of
        ///     the control as a string.  This arises only when no template is
        ///     available.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public String ContentStringFormat
        {
            get { return (String) GetValue(ContentStringFormatProperty); }
            set { SetValue(ContentStringFormatProperty, value); }
        }

        /// <summary>
        ///     Called when ContentStringFormatProperty is invalidated on "d."
        /// </summary>
        private static void OnContentStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter ctrl = (ContentPresenter)d;
            ctrl.OnContentStringFormatChanged((String) e.OldValue, (String) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ContentStringFormat property changes.
        /// </summary>
        /// <param name="oldContentStringFormat">The old value of the ContentStringFormat property.</param>
        /// <param name="newContentStringFormat">The new value of the ContentStringFormat property.</param>
        protected virtual void OnContentStringFormatChanged(String oldContentStringFormat, String newContentStringFormat)
        {
            // force on-demand regeneration of the formatting templates for XML and String content
            XMLFormattingTemplateField.ClearValue(this);
            StringFormattingTemplateField.ClearValue(this);
            AccessTextFormattingTemplateField.ClearValue(this);
        }

        /// <summary>
        ///     The DependencyProperty for the ContentSource property.
        ///     Flags:              None
        ///     Default Value:      Content
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ContentSourceProperty =
                DependencyProperty.Register(
                        "ContentSource",
                        typeof(string),
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata("Content"));

        /// <summary>
        ///     ContentSource is the base name to use during automatic aliasing.
        ///     When a template contains a ContentPresenter with ContentSource="Abc",
        ///     its Content, ContentTemplate, ContentTemplateSelector, and ContentStringFormat
        ///     properties are automatically aliased to Abc, AbcTemplate, AbcTemplateSelector,
        ///     and AbcStringFormat respectively.  The two most useful values for
        ///     ContentSource are "Content" and "Header";  the default is "Content".
        /// </summary>
        /// <remarks>
        ///     This property only makes sense in a template.  It should not be set on
        ///     an actual ContentPresenter;  there will be no effect.
        /// </remarks>
        public string ContentSource
        {
            get { return GetValue(ContentSourceProperty) as string; }
            set { SetValue(ContentSourceProperty, value); }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Called when the Template's tree is about to be generated
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            // If we're inflating our visual tree but our TemplatedParent is null,
            // we might have been removed from the visual tree but not have had
            // our ContentProperty invalidated.  This would mean that when we go
            // to reparent our content, we'll be looking at a stale cache.  Make
            // sure to invalidate the Content property in this case.
            if (TemplatedParent == null)
            {
                // call GetValueCore to get this value from its TemplatedParent
                InvalidateProperty(ContentProperty);
            }

            // If the ContentPresenter is using "default expansion", the result
            // depends on the Language property.  There is no notification when it
            // changes (i.e. no virtual OnLanguageChanged method), but it is marked
            // as AffectsMeasure, so the CP will be re-measured and will call into
            // OnPreApplyTemplate.  At this point, if Language has changed (and if
            // we're actually using it), invalidate the template.  This will cause
            // DoDefaultExpansion to run again with the new language.
            if (_language != null && _language != this.Language)
            {
                _templateIsCurrent = false;
            }

            if (!_templateIsCurrent)
            {
                EnsureTemplate();
                _templateIsCurrent = true;
            }
        }


        /// <summary>
        /// Updates DesiredSize of the ContentPresenter.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// ContentPresenter determines a desired size it needs from the child's sizing properties, margin, and requested size.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ContentPresenter's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            return Helper.MeasureElementWithSingleChild(this, constraint);
        }


        /// <summary>
        /// ContentPresenter computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <param name="arrangeSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            return Helper.ArrangeElementWithSingleChild(this, arrangeSize);
        }


        /// <summary>
        /// Return the template to use.  This may depend on the Content, or
        /// other properties.
        /// </summary>
        /// <remarks>
        /// The base class implements the following rules:
        ///   (a) If ContentTemplate is set, use it.
        ///   (b) If ContentTemplateSelector is set, call its
        ///         SelectTemplate method.  If the result is not null, use it.
        ///   (c) Look for a DataTemplate whose DataType matches the
        ///         Content among the resources known to the ContentPresenter
        ///         (including application, theme, and system resources).
        ///         If one is found, use it.
        ///   (d) If the type of Content is "common", use a standard template.
        ///         The common types are String, XmlNode, UIElement.
        ///   (e) Otherwise, use a default template that essentially converts
        ///         Content to a string and displays it in a TextBlock.
        /// Derived classes can override these rules and implement their own.
        /// </remarks>
        protected virtual DataTemplate ChooseTemplate()
        {
            DataTemplate template = null;
            object content = Content;

            // ContentTemplate has first stab
            template = ContentTemplate;

            // no ContentTemplate set, try ContentTemplateSelector
            if (template == null)
            {
                if (ContentTemplateSelector != null)
                {
                    template = ContentTemplateSelector.SelectTemplate(content, this);
                }
            }

            // if that failed, try the default TemplateSelector
            if (template == null)
            {
                template = DefaultTemplateSelector.SelectTemplate(content, this);
            }

            return template;
        }

        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        internal static DataTemplate AccessTextContentTemplate
        {
            get { return s_AccessTextTemplate; }
        }

        internal static DataTemplate StringContentTemplate
        {
            get { return s_StringTemplate; }
        }

        // Internal Helper so the FrameworkElement could see this property
        internal override FrameworkTemplate TemplateInternal
        {
            get { return Template; }
        }

        // Internal Helper so the FrameworkElement could see the template cache
        internal override FrameworkTemplate TemplateCache
        {
            get { return _templateCache; }
            set { _templateCache = (DataTemplate)value; }
        }

        internal bool TemplateIsCurrent
        {
            get { return _templateIsCurrent; }
        }

        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Prepare to display the item.
        /// </summary>
        internal void PrepareContentPresenter(object item,
                                DataTemplate itemTemplate,
                                DataTemplateSelector itemTemplateSelector,
                                string stringFormat)
        {
            if (item != this)
            {
                // copy templates from parent ItemsControl
                if (_contentIsItem || !HasNonDefaultValue(ContentProperty))
                {
                    Content = item;
                    _contentIsItem = true;
                }
                if (itemTemplate != null)
                    SetValue(ContentTemplateProperty, itemTemplate);
                if (itemTemplateSelector != null)
                    SetValue(ContentTemplateSelectorProperty, itemTemplateSelector);
                if (stringFormat != null)
                    SetValue(ContentStringFormatProperty, stringFormat);
            }
        }

        /// <summary>
        /// Undo the effect of PrepareContentPresenter.
        /// </summary>
        internal void ClearContentPresenter(object item)
        {
            if (item != this)
            {
                if (_contentIsItem)
                {
                    Content = BindingExpressionBase.DisconnectedItem;
                }
            }
        }

        internal static object DataTypeForItem(object item, DependencyObject target, out Type type)
        {
            if (item == null)
            {
                type = null;
                return null;
            }

            object dataType;
            type = ReflectionHelper.GetReflectionType(item);

            if (SystemXmlLinqHelper.IsXElement(item))
            {
                dataType = SystemXmlLinqHelper.GetXElementTagName(item);
                type = null;
            }
            else if (SystemXmlHelper.IsXmlNode(item))
            {
                dataType = SystemXmlHelper.GetXmlTagName(item, target);
                type = null;
            }
            else if (type == typeof(Object))
            {
                dataType = null;     // don't search for Object - perf
            }
            else
            {
                dataType = type;
            }

            return dataType;
        }

        // called when a resource change affects implicit data templates
        internal void ReevaluateTemplate()
        {
            // run the template algorithm again
            if (Template != ChooseTemplate())
            {
                // if it chooses a different template, mark the current template
                // as no longer current, and ask for re-measure
                _templateIsCurrent = false;
                InvalidateMeasure();
            }
        }

        //------------------------------------------------------
        //
        //  Private properties
        //
        //------------------------------------------------------

        static DataTemplate XmlNodeContentTemplate
        {
            get { return s_XmlNodeTemplate; }
        }

        static DataTemplate UIElementContentTemplate
        {
            get { return s_UIElementTemplate; }
        }

        static DataTemplate DefaultContentTemplate
        {
            get { return s_DefaultTemplate; }
        }

        static DefaultSelector DefaultTemplateSelector
        {
            get { return s_DefaultTemplateSelector; }
        }

        DataTemplate FormattingAccessTextContentTemplate
        {
            get
            {
                DataTemplate template = AccessTextFormattingTemplateField.GetValue(this);
                if (template == null)
                {
                    Binding binding = new Binding();
                    binding.StringFormat = ContentStringFormat;

                    FrameworkElementFactory text = CreateAccessTextFactory();
                    text.SetBinding(AccessText.TextProperty, binding);

                    template = new DataTemplate();
                    template.VisualTree = text;
                    template.Seal();

                    AccessTextFormattingTemplateField.SetValue(this, template);
                }
                return template;
            }
        }

        DataTemplate FormattingStringContentTemplate
        {
            get
            {
                DataTemplate template = StringFormattingTemplateField.GetValue(this);
                if (template == null)
                {
                    Binding binding = new Binding();
                    binding.StringFormat = ContentStringFormat;

                    FrameworkElementFactory text = CreateTextBlockFactory();
                    text.SetBinding(TextBlock.TextProperty, binding);

                    template = new DataTemplate();
                    template.VisualTree = text;
                    template.Seal();

                    StringFormattingTemplateField.SetValue(this, template);
                }
                return template;
            }
        }

        DataTemplate FormattingXmlNodeContentTemplate
        {
            get
            {
                DataTemplate template = XMLFormattingTemplateField.GetValue(this);
                if (template == null)
                {
                    Binding binding = new Binding();
                    binding.XPath = ".";
                    binding.StringFormat = ContentStringFormat;

                    FrameworkElementFactory text = CreateTextBlockFactory();
                    text.SetBinding(TextBlock.TextProperty, binding);

                    template = new DataTemplate();
                    template.VisualTree = text;
                    template.Seal();

                    XMLFormattingTemplateField.SetValue(this, template);
                }
                return template;
            }
        }


        /// <summary>
        /// TemplateProperty
        /// </summary>
        internal static readonly DependencyProperty TemplateProperty =
                DependencyProperty.Register(
                        "Template",
                        typeof(DataTemplate),
                        typeof(ContentPresenter),
                        new FrameworkPropertyMetadata(
                                (DataTemplate) null,  // default value
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnTemplateChanged)));


        /// <summary>
        /// Template Property
        /// </summary>
        private DataTemplate Template
        {
            get {  return _templateCache; }
            set { SetValue(TemplateProperty, value); }
        }

        // Internal helper so FrameworkElement could see call the template changed virtual
        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate, FrameworkTemplate newTemplate)
        {
            OnTemplateChanged((DataTemplate)oldTemplate, (DataTemplate)newTemplate);
        }

        // Property invalidation callback invoked when TemplateProperty is invalidated
        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter c = (ContentPresenter) d;
            StyleHelper.UpdateTemplateCache(c, (FrameworkTemplate) e.OldValue, (FrameworkTemplate) e.NewValue, TemplateProperty);
        }

        /// <summary>
        ///     Template has changed
        /// </summary>
        /// <remarks>
        ///     When a Template changes, the VisualTree is removed. The new Template's
        ///     VisualTree will be created when ApplyTemplate is called
        /// </remarks>
        /// <param name="oldTemplate">The old Template</param>
        /// <param name="newTemplate">The new Template</param>
        protected virtual void OnTemplateChanged(DataTemplate oldTemplate, DataTemplate newTemplate)
        {
        }


        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------

        private void EnsureTemplate()
        {
            DataTemplate oldTemplate = Template;
            DataTemplate newTemplate = null;

            for (_templateIsCurrent = false; !_templateIsCurrent; )
            {
                // normally this loop will execute exactly once.  The only exception
                // is when setting the DataContext causes the ContentTemplate or
                // ContentTemplateSelector to change, presumably because they are
                // themselves data-bound (see bug 128119).  In that case, we need
                // to call ChooseTemplate again, to pick up the new template.
                // We detect this case because _templateIsCurrent is reset to false
                // in OnContentTemplate[Selector]Changed, causing a second iteration
                // of the loop.
                _templateIsCurrent = true;
                newTemplate = ChooseTemplate();

                // if the template is changing, it's important that the code that cleans
                // up the old template runs while the CP's DataContext is still set to
                // the old Content.  The way to get this effect is:
                //      a. change the template to null
                //      b. change the data context
                //      c. change the template to the new value

                if (oldTemplate != newTemplate)
                {
                    Template = null;
                }

                if (newTemplate != UIElementContentTemplate)
                {
                    // set data context to the content, so that the template can bind to
                    // properties of the content.
                    this.DataContext = Content;
                }
                else
                {
                    // If we're using the content directly, clear the data context.
                    // The content expects to inherit.
                    this.ClearValue(DataContextProperty);
                }
            }

            Template = newTemplate;

            // if the template didn't change, we still need to force the content for the template to be regenerated;
            // so call StyleHelper's DoTemplateInvalidations directly
            if (oldTemplate == newTemplate)
            {
                StyleHelper.DoTemplateInvalidations(this, oldTemplate);
            }
        }

        // Select a template for string content
        DataTemplate SelectTemplateForString(string s)
        {
            DataTemplate template;
            string format = ContentStringFormat;

            if (this.RecognizesAccessKey && s.IndexOf(AccessText.AccessKeyMarker) > -1)
            {
                template = (String.IsNullOrEmpty(format)) ? AccessTextContentTemplate : FormattingAccessTextContentTemplate;
            }
            else
            {
                template = (String.IsNullOrEmpty(format)) ? StringContentTemplate : FormattingStringContentTemplate;
            }

            return template;
        }

        // return true if the template was chosen by SelectTemplateForString
        bool IsUsingDefaultStringTemplate
        {
            get
            {
                if (Template == StringContentTemplate ||
                    Template == AccessTextContentTemplate)
                {
                    return true;
                }

                DataTemplate template;

                template = StringFormattingTemplateField.GetValue(this);
                if (template != null && template == Template)
                {
                    return true;
                }

                template = AccessTextFormattingTemplateField.GetValue(this);
                if (template != null && template == Template)
                {
                    return true;
                }

                return false;
            }
        }


        // Select a template for XML content
        DataTemplate SelectTemplateForXML()
        {
            return (String.IsNullOrEmpty(ContentStringFormat)) ? XmlNodeContentTemplate : FormattingXmlNodeContentTemplate;
        }

        // ContentPresenter often has occasion to display text.  The TextBlock it uses
        // should get the values for various text-related properties (foreground, fonts,
        // decoration, trimming) from the governing ContentControl.  The following
        // two methods accomplish this - first for the case where the TextBlock appears
        // in a true template, then for the case where the TextBlock is created on
        // demand via BuildVisualTree.

        // Create a FEF for a AccessText, to be used in a default template
        internal static FrameworkElementFactory CreateAccessTextFactory()
        {
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(AccessText));

            return text;
        }

        // Create a FEF for a TextBlock, to be used in a default template
        internal static FrameworkElementFactory CreateTextBlockFactory()
        {
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));

            return text;
        }

        // Create a TextBlock, to be used in a default "template" (via BuildVisualTree)
        static TextBlock CreateTextBlock(ContentPresenter container)
        {
            TextBlock text = new TextBlock();

            return text;
        }

        // Cache the Language property when it's used by DoDefaultExpansion, so
        // that we can detect changes.  (This could also be done by a virtual
        // OnLanguageChanged method, if FrameworkElement ever defines one.)
        private void CacheLanguage(XmlLanguage language)
        {
            _language = language;
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }

        //------------------------------------------------------
        //
        //  Private nested classes
        //
        //------------------------------------------------------

        // Template for displaying UIElements - use the UIElement itself
        private class UseContentTemplate : DataTemplate
        {
            public UseContentTemplate()
            {
                // We need to preserve the treeState cache on a container node
                // even after all its logical children have been added. This is so the
                // construction of the template visual tree nodes can consume the cache.
                // This member helps us know whether we should retain the cache for
                // special scenarios when the visual tree is being built via BuildVisualTree
                CanBuildVisualTree = true;
            }

            internal override bool BuildVisualTree(FrameworkElement container)
            {
                object content = ((ContentPresenter)container).Content;
                UIElement e = content as UIElement;
                if (e == null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(ReflectionHelper.GetReflectionType(content));
                    Debug.Assert(tc.CanConvertTo(typeof(UIElement)));
                    e = (UIElement) tc.ConvertTo(content, typeof(UIElement));
                }

                StyleHelper.AddCustomTemplateRoot( container, e );

                return true;
            }
        }


        // template for displaying content when all else fails
        private class DefaultTemplate : DataTemplate
        {
            public DefaultTemplate()
            {
                // We need to preserve the treeState cache on a container node
                // even after all its logical children have been added. This is so the
                // construction of the template visual tree nodes can consume the cache.
                // This member helps us know whether we should retain the cache for
                // special scenarios when the visual tree is being built via BuildVisualTree
                CanBuildVisualTree = true;
            }

            //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
            internal override bool BuildVisualTree(FrameworkElement container)
            {
                bool tracingEnabled = EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
                if (tracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "ContentPresenter.BuildVisualTree");
                }
                try
                {
                    ContentPresenter cp = (ContentPresenter)container;
                    Visual result = DefaultExpansion(cp.Content, cp);
                    return (result != null);
                }
                finally
                {
                    if (tracingEnabled)
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, String.Format(System.Globalization.CultureInfo.InvariantCulture, "ContentPresenter.BuildVisualTree for CP {0}", container.GetHashCode()));
                    }
                }
            }

            private UIElement DefaultExpansion(object content, ContentPresenter container)
            {
                if (content == null)
                    return null;

                TextBlock textBlock = CreateTextBlock(container);
                textBlock.IsContentPresenterContainer = true; // this is done so that the TextBlock does not steal away the logical child
                if( container != null )
                {
                    StyleHelper.AddCustomTemplateRoot(
                        container,
                        textBlock,
                        false, // Do not need to check for existing visual parent since we just created it
                        true); // set treeState cache on the Text instance created
                }

                DoDefaultExpansion(textBlock, content, container);

                return textBlock;
            }

            private void DoDefaultExpansion(TextBlock textBlock, object content, ContentPresenter container)
            {
                Debug.Assert(!(content is String) && !(content is UIElement));  // these are handled by different templates

                Inline inline;

                if ((inline = content as Inline) != null)
                {
                    textBlock.Inlines.Add(inline);
                }
                else
                {
                    bool succeeded = false;
                    string stringFormat;
                    XmlLanguage language = container.Language;
                    System.Globalization.CultureInfo culture = language.GetSpecificCulture();
                    container.CacheLanguage(language);

                    if ((stringFormat = container.ContentStringFormat) != null)
                    {
                        try
                        {
                            stringFormat = Helper.GetEffectiveStringFormat(stringFormat);
                            textBlock.Text = String.Format(culture, stringFormat, content);
                            succeeded = true;
                        }
                        catch (FormatException)
                        {
                        }
                    }

                    if (!succeeded)
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(ReflectionHelper.GetReflectionType(content));
                        TypeContext context = new TypeContext(content);
                        if (tc != null && (tc.CanConvertTo(context, typeof(String))))
                        {
                            textBlock.Text = (string)tc.ConvertTo(context, culture, content, typeof(string));
                        }
                        else
                        {
                            Debug.Assert(!(tc != null && tc.CanConvertTo(typeof(UIElement))));  // this is handled by a different template
                            textBlock.Text = String.Format(culture, "{0}", content);
                        }
                    }
                }
            }

            // Some type converters need the actual object that will be converted
            // in order to answer the CanConvertTo question.  (WPF's own CommandConverter
            // is an example)   We make this object available
            // via ITypeDescriptorContext.Instance.
            private class TypeContext : ITypeDescriptorContext
            {
                object _instance;
                public TypeContext(object instance)
                {
                    _instance = instance;
                }

                IContainer ITypeDescriptorContext.Container { get { return null; } }
                object ITypeDescriptorContext.Instance { get { return _instance; } }
                PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor { get { return null; } }
                void ITypeDescriptorContext.OnComponentChanged() {}
                bool ITypeDescriptorContext.OnComponentChanging() { return false; }
                object IServiceProvider.GetService(Type serviceType) { return null; }
            }
        }

        private class DefaultSelector : DataTemplateSelector
        {
            /// <summary>
            /// Override this method to return an app specific <seealso cref="Template"/>.
            /// </summary>
            /// <param name="item">The data content</param>
            /// <param name="container">The container in which the content is to be displayed</param>
            /// <returns>a app specific template to apply.</returns>
            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                DataTemplate template = null;

                // Lookup template for typeof(Content) in resource dictionaries.
                if (item != null)
                {
                    template = (DataTemplate)FrameworkElement.FindTemplateResourceInternal(container, item, typeof(DataTemplate));
                }

                // default templates for well known types:
                if (template == null)
                {
                    TypeConverter tc = null;
                    string s;

                    if ((s = item as string) != null)
                        template = ((ContentPresenter)container).SelectTemplateForString(s);
                    else if (item is UIElement)
                        template = UIElementContentTemplate;
                    else if (SystemXmlHelper.IsXmlNode(item))
                        template = ((ContentPresenter)container).SelectTemplateForXML();
                    else if (item is Inline)
                        template = DefaultContentTemplate;
                    else if (item != null &&
                                (tc = TypeDescriptor.GetConverter(ReflectionHelper.GetReflectionType(item))) != null &&
                                tc.CanConvertTo(typeof(UIElement)))
                        template = UIElementContentTemplate;
                    else
                        template = DefaultContentTemplate;
                }

                return template;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private DataTemplate _templateCache;

        private bool _templateIsCurrent;
        private bool _contentIsItem;
        private XmlLanguage _language;

        private static DataTemplate s_AccessTextTemplate;
        private static DataTemplate s_StringTemplate;
        private static DataTemplate s_XmlNodeTemplate;
        private static DataTemplate s_UIElementTemplate;
        private static DataTemplate s_DefaultTemplate;
        private static DefaultSelector s_DefaultTemplateSelector;
        private static readonly UncommonField<DataTemplate> XMLFormattingTemplateField = new UncommonField<DataTemplate>();
        private static readonly UncommonField<DataTemplate> StringFormattingTemplateField = new UncommonField<DataTemplate>();
        private static readonly UncommonField<DataTemplate> AccessTextFormattingTemplateField = new UncommonField<DataTemplate>();
    }
}

