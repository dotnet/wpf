using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationFramework.Win11.Controls
{
    // TODO: Use font icon if available
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class FontIconFallback : Control
    {
        static FontIconFallback()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontIconFallback), new FrameworkPropertyMetadata(typeof(FontIconFallback)));
            FocusableProperty.OverrideMetadata(typeof(FontIconFallback), new FrameworkPropertyMetadata(false));
        }

        #region Data

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(Geometry),
                typeof(FontIconFallback),
                null);

        public Geometry Data
        {
            get => (Geometry)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        #endregion
    }
}
