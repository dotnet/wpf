// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Windows.Themes
{
    public sealed partial class BulletChrome : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty BackgroundProperty;
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty BorderThicknessProperty;
        public static readonly System.Windows.DependencyProperty IsCheckedProperty;
        public static readonly System.Windows.DependencyProperty IsRoundProperty;
        public static readonly System.Windows.DependencyProperty RenderMouseOverProperty;
        public static readonly System.Windows.DependencyProperty RenderPressedProperty;
        public BulletChrome() { }
        public System.Windows.Media.Brush Background { get { throw null; } set { } }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Thickness BorderThickness { get { throw null; } set { } }
        public bool? IsChecked { get { throw null; } set { } }
        public bool IsRound { get { throw null; } set { } }
        public bool RenderMouseOver { get { throw null; } set { } }
        public bool RenderPressed { get { throw null; } set { } }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
    public sealed partial class ButtonChrome : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty BorderBrushProperty;
        public static readonly System.Windows.DependencyProperty FillProperty;
        public static readonly System.Windows.DependencyProperty RenderDefaultedProperty;
        public static readonly System.Windows.DependencyProperty RenderMouseOverProperty;
        public static readonly System.Windows.DependencyProperty RenderPressedProperty;
        public ButtonChrome() { }
        public System.Windows.Media.Brush BorderBrush { get { throw null; } set { } }
        public System.Windows.Media.Brush Fill { get { throw null; } set { } }
        public bool RenderDefaulted { get { throw null; } set { } }
        public bool RenderMouseOver { get { throw null; } set { } }
        public bool RenderPressed { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
    public sealed partial class DataGridHeaderBorder : System.Windows.Controls.Border
    {
        public static readonly System.Windows.DependencyProperty IsClickableProperty;
        public static readonly System.Windows.DependencyProperty IsHoveredProperty;
        public static readonly System.Windows.DependencyProperty IsPressedProperty;
        public static readonly System.Windows.DependencyProperty IsSelectedProperty;
        public static readonly System.Windows.DependencyProperty OrientationProperty;
        public static readonly System.Windows.DependencyProperty SeparatorBrushProperty;
        public static readonly System.Windows.DependencyProperty SeparatorVisibilityProperty;
        public static readonly System.Windows.DependencyProperty SortDirectionProperty;
        public DataGridHeaderBorder() { }
        public bool IsClickable { get { throw null; } set { } }
        public bool IsHovered { get { throw null; } set { } }
        public bool IsPressed { get { throw null; } set { } }
        public bool IsSelected { get { throw null; } set { } }
        public System.Windows.Controls.Orientation Orientation { get { throw null; } set { } }
        public System.Windows.Media.Brush SeparatorBrush { get { throw null; } set { } }
        public System.Windows.Visibility SeparatorVisibility { get { throw null; } set { } }
        public System.ComponentModel.ListSortDirection? SortDirection { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext dc) { }
    }
    public static partial class PlatformCulture
    {
        public static System.Windows.FlowDirection FlowDirection { get { throw null; } }
    }
    public partial class ProgressBarBrushConverter : System.Windows.Data.IMultiValueConverter
    {
        public ProgressBarBrushConverter() { }
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw null; }
        public object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) { throw null; }
    }
    public sealed partial class ScrollChrome : System.Windows.FrameworkElement
    {
        public static readonly System.Windows.DependencyProperty HasOuterBorderProperty;
        public static readonly System.Windows.DependencyProperty PaddingProperty;
        public static readonly System.Windows.DependencyProperty RenderMouseOverProperty;
        public static readonly System.Windows.DependencyProperty RenderPressedProperty;
        public static readonly System.Windows.DependencyProperty ScrollGlyphProperty;
        public ScrollChrome() { }
        public bool HasOuterBorder { get { throw null; } set { } }
        public System.Windows.Thickness Padding { get { throw null; } set { } }
        public bool RenderMouseOver { get { throw null; } set { } }
        public bool RenderPressed { get { throw null; } set { } }
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize) { throw null; }
        public static Microsoft.Windows.Themes.ScrollGlyph GetScrollGlyph(System.Windows.DependencyObject element) { throw null; }
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize) { throw null; }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
        public static void SetScrollGlyph(System.Windows.DependencyObject element, Microsoft.Windows.Themes.ScrollGlyph value) { }
    }
    public enum ScrollGlyph
    {
        None = 0,
        LeftArrow = 1,
        RightArrow = 2,
        UpArrow = 3,
        DownArrow = 4,
        VerticalGripper = 5,
        HorizontalGripper = 6,
    }
    public sealed partial class SystemDropShadowChrome : System.Windows.Controls.Decorator
    {
        public static readonly System.Windows.DependencyProperty ColorProperty;
        public static readonly System.Windows.DependencyProperty CornerRadiusProperty;
        public SystemDropShadowChrome() { }
        public System.Windows.Media.Color Color { get { throw null; } set { } }
        public System.Windows.CornerRadius CornerRadius { get { throw null; } set { } }
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) { }
    }
}
