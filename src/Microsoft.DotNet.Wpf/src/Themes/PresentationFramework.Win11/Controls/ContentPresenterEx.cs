using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PresentationFramework.Win11.Controls
{
    /// <summary>
    /// ContentPresenter is used within the template of a content control to denote the
    /// place in the control's visual tree (control template) where the content
    /// is to be added.
    /// </summary>
    public class ContentPresenterEx : ContentPresenter
    {
        #region Public Properties

        /// <summary>
        /// DependencyProperty for <see cref="FontFamily" /> property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The FontFamily property specifies the name of font family.
        /// </summary>
        [Localizability(LocalizationCategory.Font)]
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStyle" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The FontStyle property requests normal, italic, and oblique faces within a font family.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontWeight" /> property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontStretch" /> property.
        /// </summary>
        public static readonly DependencyProperty FontStretchProperty =
                TextElement.FontStretchProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The FontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FontSize" /> property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The FontSize property specifies the size of the font.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Foreground" /> property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The Foreground property specifies the foreground brush of an element's text content.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty =
                Block.LineHeightProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The LineHeight property specifies the height of each generated line box.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                Block.LineStackingStrategyProperty.AddOwner(typeof(ContentPresenterEx));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextWrapping" /> property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty =
                TextBlock.TextWrappingProperty.AddOwner(
                        typeof(ContentPresenterEx),
                        new FrameworkPropertyMetadata(
                                TextWrapping.NoWrap,
                                new PropertyChangedCallback(OnTextWrappingChanged)));

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps 
        /// when it reaches the flow edge of its containing block box.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (ContentPresenterEx)d;
            if (ctrl.TextBlock != null)
            {
                ctrl.TextBlock.TextWrapping = (TextWrapping)e.NewValue;
            }
            else if (ctrl.AccessText != null)
            {
                ctrl.AccessText.TextWrapping = (TextWrapping)e.NewValue;
            }
        }

        #endregion

        #region Private Properties

        private bool IsUsingDefaultTemplate { get; set; }

        private TextBlock _textBlock;
        private TextBlock TextBlock
        {
            get => _textBlock;
            set
            {
                if (_textBlock != null)
                {
                    _textBlock.ClearValue(TextBlock.TextWrappingProperty);
                }

                _textBlock = value;

                if (_textBlock != null)
                {
                    _textBlock.TextWrapping = TextWrapping;
                }
            }
        }

        private AccessText _accessText;
        private AccessText AccessText
        {
            get => _accessText;
            set
            {
                if (_accessText != null)
                {
                    _accessText.ClearValue(AccessText.TextWrappingProperty);
                }

                _accessText = value;

                if (_accessText != null)
                {
                    _accessText.TextWrapping = TextWrapping;
                }
            }
        }

        #endregion

        #region Protected Methods

        protected override DataTemplate ChooseTemplate()
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
                template = base.ChooseTemplate();
                IsUsingDefaultTemplate = true;
            }
            else
            {
                IsUsingDefaultTemplate = false;
            }

            return template;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded != null && IsUsingDefaultTemplate)
            {
                if (visualAdded is TextBlock textBlock)
                {
                    TextBlock = textBlock;
                }
                else if (visualAdded is AccessText accessText)
                {
                    AccessText = accessText;
                }
            }
            else if (visualRemoved != null)
            {
                if (visualRemoved == TextBlock)
                {
                    TextBlock = null;
                }
                else if (visualRemoved == AccessText)
                {
                    AccessText = null;
                }
            }
        }

        #endregion
    }
}
