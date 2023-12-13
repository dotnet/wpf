using System.Windows;
using System.Windows.Controls;
using  PresentationFramework.Win11.Controls.Primitives;

namespace PresentationFramework.Win11.Controls
{
    public class ContentControlEx : ContentControl
    {
        static ContentControlEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentControlEx), new FrameworkPropertyMetadata(typeof(ContentControlEx)));
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(ContentControlEx), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(ContentControlEx), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
        }

        #region CornerRadius

        public static readonly DependencyProperty CornerRadiusProperty =
            ControlHelper.CornerRadiusProperty.AddOwner(typeof(ContentControlEx));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region RecognizesAccessKey

        public static readonly DependencyProperty RecognizesAccessKeyProperty =
            DependencyProperty.Register(
                nameof(RecognizesAccessKey),
                typeof(bool),
                typeof(ContentControlEx),
                new FrameworkPropertyMetadata(false));

        public bool RecognizesAccessKey
        {
            get => (bool)GetValue(RecognizesAccessKeyProperty);
            set => SetValue(RecognizesAccessKeyProperty, value);
        }

        #endregion
    }
}
