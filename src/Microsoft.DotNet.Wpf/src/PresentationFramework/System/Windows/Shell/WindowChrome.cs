// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;
using System.Windows.Media;
using Standard;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Shell
#else
namespace Microsoft.Windows.Shell
#endif
{
    public enum ResizeGripDirection
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
    }

    [Flags]
    public enum NonClientFrameEdges
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
    }

    /// <summary>
    /// Describes how a <see cref="WindowChrome"/> takes ownership of the non-client area of its window.
    /// </summary>
    public enum WindowChromeFrameMode
    {
        /// <summary>
        /// WPF owns the whole window surface.  <see cref="WindowChrome.CaptionHeight"/>,
        /// <see cref="WindowChrome.ResizeBorderThickness"/>, <see cref="WindowChrome.GlassFrameThickness"/> and
        /// <see cref="WindowChrome.NonClientFrameEdges"/> drive the frame calculation.  This is the default and the
        /// historical behavior.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// The system frame is kept: the system keeps drawing and hit-testing the caption buttons while the client
        /// area is extended over the visible frame, whose thickness is measured from the actual window frame rather
        /// than from system metrics.  WPF content must not paint over <see cref="WindowChrome.CaptionButtonsBoundsProperty"/>.
        /// The window must use <see cref="WindowStyle.SingleBorderWindow"/> or <see cref="WindowStyle.ThreeDBorderWindow"/>.
        /// </summary>
        ExtendedClientArea = 1,

        /// <summary>
        /// The standard system frame is left untouched.  The chrome only manages the caption appearance (colors,
        /// corners, caption theme) and the backdrop.  This is the mode used by the chrome that the framework attaches
        /// automatically to Fluent-themed windows.
        /// </summary>
        SystemFrame = 2,
    }

    /// <summary>Rounded corner preference for a top-level window.  Requires Windows 11.</summary>
    public enum WindowCornerPreference
    {
        /// <summary>Let the system decide.</summary>
        Default = 0,
        /// <summary>Never round the corners.</summary>
        DoNotRound = 1,
        /// <summary>Round the corners if appropriate.</summary>
        Round = 2,
        /// <summary>Round the corners with a small radius.</summary>
        RoundSmall = 3,
    }

    /// <summary>Selects the light or dark rendering of the system caption.  Requires Windows 10 version 1809.</summary>
    public enum WindowCaptionTheme
    {
        /// <summary>Follow the theme decided by the framework (Fluent theme mode); leave the system default otherwise.</summary>
        Auto = 0,
        /// <summary>Force a light caption.</summary>
        Light = 1,
        /// <summary>Force a dark caption.</summary>
        Dark = 2,
    }

    /// <summary>System backdrop material for a top-level window.  Requires Windows 11 version 22H2.</summary>
    // The numeric values match DWM_SYSTEMBACKDROP_TYPE (DWMSBT_*) so they can be cast directly.
    public enum WindowBackdropKind
    {
        /// <summary>Follow the framework theme: Mica for Fluent-themed windows, none otherwise.</summary>
        Auto = 0,
        /// <summary>No backdrop.</summary>
        None = 1,
        /// <summary>Mica.</summary>
        Mica = 2,
        /// <summary>Desktop acrylic.</summary>
        Acrylic = 3,
        /// <summary>Mica Alt, the variant used for tabbed windows.</summary>
        Tabbed = 4,
    }

    /// <summary>Flags identifying which DWM-related properties of a <see cref="WindowChrome"/> changed.</summary>
    [Flags]
    internal enum WindowChromeDwmAttributes
    {
        None = 0,
        CaptionColor = 0x01,
        CaptionTextColor = 0x02,
        BorderColor = 0x04,
        CornerPreference = 0x08,
        CaptionTheme = 0x10,
        Backdrop = 0x20,
        All = 0x3F,
    }

    internal sealed class WindowChromeDwmAttributesChangedEventArgs : EventArgs
    {
        public WindowChromeDwmAttributesChangedEventArgs(WindowChromeDwmAttributes attributes)
        {
            Attributes = attributes;
        }

        public WindowChromeDwmAttributes Attributes { get; }
    }

    public class WindowChrome : Freezable
    {
        private struct _SystemParameterBoundProperty
        {
            public string SystemParameterPropertyName { get; set; }
            public DependencyProperty DependencyProperty { get; set; }
        }

        // Named property available for fully extending the glass frame.
        public static Thickness GlassFrameCompleteThickness { get { return new Thickness(-1); } }

        #region Attached Properties

        public static readonly DependencyProperty WindowChromeProperty = DependencyProperty.RegisterAttached(
            "WindowChrome",
            typeof(WindowChrome),
            typeof(WindowChrome),
            new PropertyMetadata(null, _OnChromeChanged));

        private static void _OnChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The different design tools handle drawing outside their custom window objects differently.
            // Rather than try to support this concept in the design surface let the designer draw its own
            // chrome anyways.
            // There's certainly room for improvement here.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(d))
            {
                return;
            }

            var window = (Window)d;
            var newChrome = (WindowChrome)e.NewValue;

            Assert.IsNotNull(window);

            // Update the ChromeWorker with this new object.

            // If there isn't currently a worker associated with the Window then assign a new one.
            // There can be a many:1 relationship of to Window to WindowChrome objects, but a 1:1 for a Window and a WindowChromeWorker.
            WindowChromeWorker chromeWorker = WindowChromeWorker.GetWindowChromeWorker(window);
            if (chromeWorker == null)
            {
                chromeWorker = new WindowChromeWorker();
                WindowChromeWorker.SetWindowChromeWorker(window, chromeWorker);
            }

            chromeWorker.SetWindowChrome(newChrome);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static WindowChrome GetWindowChrome(Window window)
        {
            Verify.IsNotNull(window, "window");
            return (WindowChrome)window.GetValue(WindowChromeProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetWindowChrome(Window window, WindowChrome chrome)
        {
            Verify.IsNotNull(window, "window");
            window.SetValue(WindowChromeProperty, chrome);
        }

        public static readonly DependencyProperty IsHitTestVisibleInChromeProperty = DependencyProperty.RegisterAttached(
            "IsHitTestVisibleInChrome",
            typeof(bool),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool GetIsHitTestVisibleInChrome(IInputElement inputElement)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }
            return (bool)dobj.GetValue(IsHitTestVisibleInChromeProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetIsHitTestVisibleInChrome(IInputElement inputElement, bool hitTestVisible)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }
            dobj.SetValue(IsHitTestVisibleInChromeProperty, hitTestVisible);
        }

        public static readonly DependencyProperty ResizeGripDirectionProperty = DependencyProperty.RegisterAttached(
            "ResizeGripDirection",
            typeof(ResizeGripDirection),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(ResizeGripDirection.None, FrameworkPropertyMetadataOptions.Inherits));

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static ResizeGripDirection GetResizeGripDirection(IInputElement inputElement)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }
            return (ResizeGripDirection)dobj.GetValue(ResizeGripDirectionProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetResizeGripDirection(IInputElement inputElement, ResizeGripDirection direction)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }
            dobj.SetValue(ResizeGripDirectionProperty, direction);
        }

        // The following attached properties hold per-window state that is computed by the WindowChromeWorker.
        // They cannot live on the WindowChrome instance because a WindowChrome is a Freezable that may be shared
        // (and frozen) across several windows.  They are inherited so that any element of the window template
        // can bind to them.

        internal static readonly DependencyPropertyKey CaptionButtonsBoundsPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "CaptionButtonsBounds",
            typeof(Rect),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(default(Rect), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Read-only attached property, set on the <see cref="Window"/>, describing the area (in device independent
        /// pixels, relative to the window's client origin) occupied by the caption buttons drawn by the system.
        /// WPF content should leave that area transparent.  The value is <c>default(Rect)</c> (all zeros) when
        /// the system is not drawing the caption buttons over the client area.
        /// </summary>
        public static readonly DependencyProperty CaptionButtonsBoundsProperty = CaptionButtonsBoundsPropertyKey.DependencyProperty;

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static Rect GetCaptionButtonsBounds(DependencyObject element)
        {
            Verify.IsNotNull(element, "element");
            return (Rect)element.GetValue(CaptionButtonsBoundsProperty);
        }

        internal static readonly DependencyPropertyKey CaptionButtonsClipPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "CaptionButtonsClip",
            typeof(Geometry),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Read-only attached property, set on the <see cref="Window"/>: a frozen geometry covering the client
        /// area except <see cref="CaptionButtonsBoundsProperty"/>, ready to be bound to the <c>Clip</c> of an
        /// element that fills the whole client area (typically the root of the window content) so that it never
        /// paints over the caption buttons drawn by the system.  It is expressed in the coordinates of the client
        /// origin, so it is not suitable for nested elements with an offset.  <see langword="null"/> when the
        /// system is not drawing the caption buttons over the client area, which leaves the element unclipped.
        /// </summary>
        public static readonly DependencyProperty CaptionButtonsClipProperty = CaptionButtonsClipPropertyKey.DependencyProperty;

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static Geometry GetCaptionButtonsClip(DependencyObject element)
        {
            Verify.IsNotNull(element, "element");
            return (Geometry)element.GetValue(CaptionButtonsClipProperty);
        }

        internal static readonly DependencyPropertyKey TitleBarBoundsPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "TitleBarBounds",
            typeof(Rect),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(default(Rect), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Read-only attached property, set on the <see cref="Window"/>, describing the part of the caption band
        /// that is free for application content (in device independent pixels, relative to the window's client
        /// origin): the whole band, as tall as the caption buttons drawn by the system, minus the area described
        /// by <see cref="CaptionButtonsBoundsProperty"/>.  Title bar content placed inside it lines up with the
        /// system caption.  The value is <c>default(Rect)</c> (all zeros) when the system is not drawing the caption
        /// buttons over the client area.
        /// </summary>
        public static readonly DependencyProperty TitleBarBoundsProperty = TitleBarBoundsPropertyKey.DependencyProperty;

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static Rect GetTitleBarBounds(DependencyObject element)
        {
            Verify.IsNotNull(element, "element");
            return (Rect)element.GetValue(TitleBarBoundsProperty);
        }

        internal static readonly DependencyPropertyKey CaptionForegroundPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "CaptionForeground",
            typeof(Brush),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(SystemColors.ActiveCaptionTextBrush, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Read-only attached property, set on the <see cref="Window"/>, holding the brush that matches the color
        /// the system would use for the caption text of this window (active/inactive state, accent color, caption
        /// theme and <see cref="CaptionColor"/>/<see cref="CaptionTextColor"/> are taken into account).
        /// </summary>
        public static readonly DependencyProperty CaptionForegroundProperty = CaptionForegroundPropertyKey.DependencyProperty;

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static Brush GetCaptionForeground(DependencyObject element)
        {
            Verify.IsNotNull(element, "element");
            return (Brush)element.GetValue(CaptionForegroundProperty);
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.Register(
            "CaptionHeight",
            typeof(double),
            typeof(WindowChrome),
            new PropertyMetadata(
                0d,
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()),
            value => (double)value >= 0d);

        /// <summary>
        /// The extent of the top of the window to treat as the caption.  Left unset it follows the system caption
        /// height.  In <see cref="WindowChromeFrameMode.ExtendedClientArea"/> a value set by the application also
        /// sizes the band extended over the frame and <see cref="TitleBarBoundsProperty"/>, instead of the height
        /// of the system caption buttons; that includes a backdrop, which then no longer covers the whole window
        /// unless a negative <see cref="GlassFrameThickness"/> asks for the whole window explicitly.
        /// </summary>
        public double CaptionHeight
        {
            get { return (double)GetValue(CaptionHeightProperty); }
            set { SetValue(CaptionHeightProperty, value); }
        }

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(
            "ResizeBorderThickness",
            typeof(Thickness),
            typeof(WindowChrome),
            new PropertyMetadata(default(Thickness)),
            (value) => Utility.IsThicknessNonNegative((Thickness)value));

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)GetValue(ResizeBorderThicknessProperty); }
            set { SetValue(ResizeBorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty GlassFrameThicknessProperty = DependencyProperty.Register(
            "GlassFrameThickness",
            typeof(Thickness),
            typeof(WindowChrome),
            new PropertyMetadata(
                default(Thickness),
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint(),
                (d, o) => _CoerceGlassFrameThickness((Thickness)o)));

        private static object _CoerceGlassFrameThickness(Thickness thickness)
        {
            // If it's explicitly set, but set to a thickness with at least one negative side then 
            // coerce the value to the stock GlassFrameCompleteThickness.
            if (!Utility.IsThicknessNonNegative(thickness))
            {
                return GlassFrameCompleteThickness;
            }

            return thickness;
        }

        /// <summary>
        /// Extent of the system frame extended into the client area.  Left unset it follows the system frame.
        /// In <see cref="WindowChromeFrameMode.ExtendedClientArea"/> and <see cref="WindowChromeFrameMode.SystemFrame"/>
        /// a non-negative value set by the application is used as the extension margins in every theme, with or
        /// without a backdrop, instead of the whole window a backdrop would otherwise cover; the top is the larger
        /// of its own top and <see cref="CaptionHeight"/> when that is set as well.  A negative value extends the
        /// whole window.
        /// </summary>
        public Thickness GlassFrameThickness
        {
            get { return (Thickness)GetValue(GlassFrameThicknessProperty); }
            set { SetValue(GlassFrameThicknessProperty, value); }
        }

        public static readonly DependencyProperty UseAeroCaptionButtonsProperty = DependencyProperty.Register(
            "UseAeroCaptionButtons",
            typeof(bool),
            typeof(WindowChrome),
            new FrameworkPropertyMetadata(true));

        public bool UseAeroCaptionButtons
        {
            get { return (bool)GetValue(UseAeroCaptionButtonsProperty); }
            set { SetValue(UseAeroCaptionButtonsProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(WindowChrome),
            new PropertyMetadata(
                default(CornerRadius),
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()),
            (value) => Utility.IsCornerRadiusValid((CornerRadius)value));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty NonClientFrameEdgesProperty = DependencyProperty.Register(
            "NonClientFrameEdges",
            typeof(NonClientFrameEdges),
            typeof(WindowChrome),
            new PropertyMetadata(
                NonClientFrameEdges.None,
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()),
            _NonClientFrameEdgesAreValid);

        private static readonly NonClientFrameEdges NonClientFrameEdges_All = NonClientFrameEdges.Left | NonClientFrameEdges.Top | NonClientFrameEdges.Right | NonClientFrameEdges.Bottom;

        private static bool _NonClientFrameEdgesAreValid(object value)
        {
            NonClientFrameEdges ncEdges = NonClientFrameEdges.None;
            try
            {
                ncEdges = (NonClientFrameEdges)value;
            }
            catch (InvalidCastException)
            {
                return false;
            }

            if (ncEdges == NonClientFrameEdges.None)
            {
                return true;
            }

            // Does this only contain valid bits?
            if ((ncEdges | NonClientFrameEdges_All) != NonClientFrameEdges_All)
            {
                return false;
            }

            // It can't sacrifice all 4 edges.  Weird things happen.
            if (ncEdges == NonClientFrameEdges_All)
            {
                return false;
            }

            return true; 
        }

        public NonClientFrameEdges NonClientFrameEdges
        {
            get { return (NonClientFrameEdges)GetValue(NonClientFrameEdgesProperty); }
            set { SetValue(NonClientFrameEdgesProperty, value); }
        }

        public static readonly DependencyProperty FrameModeProperty = DependencyProperty.Register(
            "FrameMode",
            typeof(WindowChromeFrameMode),
            typeof(WindowChrome),
            new PropertyMetadata(
                WindowChromeFrameMode.Custom,
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()),
            value => Enum.IsDefined(typeof(WindowChromeFrameMode), value));

        /// <summary>How the chrome takes ownership of the non-client area.  See <see cref="WindowChromeFrameMode"/>.</summary>
        public WindowChromeFrameMode FrameMode
        {
            get { return (WindowChromeFrameMode)GetValue(FrameModeProperty); }
            set { SetValue(FrameModeProperty, value); }
        }

        public static readonly DependencyProperty ShowSystemIconProperty = DependencyProperty.Register(
            "ShowSystemIcon",
            typeof(bool?),
            typeof(WindowChrome),
            new PropertyMetadata(
                (bool?)null,
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()));

        /// <summary>
        /// Whether the window icon is shown in the caption.  <c>null</c> (the default) means shown, except in
        /// <see cref="WindowChromeFrameMode.ExtendedClientArea"/> mode where the application usually provides its
        /// own title bar.  In <see cref="WindowChromeFrameMode.Custom"/> and <see cref="WindowChromeFrameMode.SystemFrame"/>
        /// modes the system stops drawing the icon when false; in ExtendedClientArea mode the icon is drawn by WPF
        /// when true.
        /// </summary>
        // Custom / SystemFrame: WTNCA_NODRAWICON through SetWindowThemeAttribute.
        public bool? ShowSystemIcon
        {
            get { return (bool?)GetValue(ShowSystemIconProperty); }
            set { SetValue(ShowSystemIconProperty, value); }
        }

        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
            "ShowTitle",
            typeof(bool?),
            typeof(WindowChrome),
            new PropertyMetadata(
                (bool?)null,
                (d, e) => ((WindowChrome)d)._OnPropertyChangedThatRequiresRepaint()));

        /// <summary>
        /// Whether the window title is shown in the caption.  <c>null</c> (the default) means shown, except in
        /// <see cref="WindowChromeFrameMode.ExtendedClientArea"/> mode where the application usually provides its
        /// own title bar.  In <see cref="WindowChromeFrameMode.Custom"/> and <see cref="WindowChromeFrameMode.SystemFrame"/>
        /// modes the system stops drawing the title when false; in ExtendedClientArea mode the title is drawn by WPF
        /// using <see cref="CaptionForegroundProperty"/> when true.
        /// </summary>
        // Custom / SystemFrame: WTNCA_NODRAWCAPTION through SetWindowThemeAttribute.
        public bool? ShowTitle
        {
            get { return (bool?)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }

        /// <summary>The effective value of <see cref="ShowSystemIcon"/>.</summary>
        internal bool EffectiveShowSystemIcon
        {
            get { return ShowSystemIcon ?? (FrameMode != WindowChromeFrameMode.ExtendedClientArea); }
        }

        /// <summary>The effective value of <see cref="ShowTitle"/>.</summary>
        internal bool EffectiveShowTitle
        {
            get { return ShowTitle ?? (FrameMode != WindowChromeFrameMode.ExtendedClientArea); }
        }

        public static readonly DependencyProperty CaptionColorProperty = DependencyProperty.Register(
            "CaptionColor",
            typeof(Color?),
            typeof(WindowChrome),
            new PropertyMetadata(
                (Color?)null,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.CaptionColor)));

        /// <summary>
        /// Color of the system caption.  <c>null</c> restores the system default.  A fully transparent color
        /// asks the system not to paint the caption at all while a backdrop (see <see cref="BackdropType"/>) is
        /// applied, so that the backdrop shows through the whole title bar even when the system is set to
        /// color title bars; without a backdrop a transparent color is treated like <c>null</c>.  Any other
        /// alpha value is ignored.  Requires Windows 11; ignored on earlier versions.
        /// </summary>
        // DWMWA_CAPTION_COLOR; transparent over a backdrop maps to DWMWA_COLOR_NONE.
        public Color? CaptionColor
        {
            get { return (Color?)GetValue(CaptionColorProperty); }
            set { SetValue(CaptionColorProperty, value); }
        }

        public static readonly DependencyProperty CaptionTextColorProperty = DependencyProperty.Register(
            "CaptionTextColor",
            typeof(Color?),
            typeof(WindowChrome),
            new PropertyMetadata(
                (Color?)null,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.CaptionTextColor)));

        /// <summary>
        /// Color of the system caption text.  <c>null</c> restores the system default.  The alpha channel is ignored.
        /// Requires Windows 11; ignored on earlier versions.
        /// </summary>
        // DWMWA_TEXT_COLOR.
        public Color? CaptionTextColor
        {
            get { return (Color?)GetValue(CaptionTextColorProperty); }
            set { SetValue(CaptionTextColorProperty, value); }
        }

        public static readonly DependencyProperty BorderColorProperty = DependencyProperty.Register(
            "BorderColor",
            typeof(Color?),
            typeof(WindowChrome),
            new PropertyMetadata(
                (Color?)null,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.BorderColor)));

        /// <summary>
        /// Color of the window border.  <c>null</c> restores the system default; a fully transparent color
        /// (alpha 0) removes the border.  Requires Windows 11; ignored on earlier versions.
        /// </summary>
        // DWMWA_BORDER_COLOR; alpha 0 maps to DWMWA_COLOR_NONE.
        public Color? BorderColor
        {
            get { return (Color?)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        public static readonly DependencyProperty CornerPreferenceProperty = DependencyProperty.Register(
            "CornerPreference",
            typeof(WindowCornerPreference),
            typeof(WindowChrome),
            new PropertyMetadata(
                WindowCornerPreference.Default,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.CornerPreference)),
            value => Enum.IsDefined(typeof(WindowCornerPreference), value));

        /// <summary>Rounded corner preference of the window.  Requires Windows 11; ignored on earlier versions.</summary>
        // DWMWA_WINDOW_CORNER_PREFERENCE.
        public WindowCornerPreference CornerPreference
        {
            get { return (WindowCornerPreference)GetValue(CornerPreferenceProperty); }
            set { SetValue(CornerPreferenceProperty, value); }
        }

        public static readonly DependencyProperty CaptionThemeProperty = DependencyProperty.Register(
            "CaptionTheme",
            typeof(WindowCaptionTheme),
            typeof(WindowChrome),
            new PropertyMetadata(
                WindowCaptionTheme.Auto,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.CaptionTheme)),
            value => Enum.IsDefined(typeof(WindowCaptionTheme), value));

        /// <summary>Light or dark rendering of the system caption.  Requires Windows 10 version 1809; ignored on earlier versions.</summary>
        // DWMWA_USE_IMMERSIVE_DARK_MODE.
        public WindowCaptionTheme CaptionTheme
        {
            get { return (WindowCaptionTheme)GetValue(CaptionThemeProperty); }
            set { SetValue(CaptionThemeProperty, value); }
        }

        public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.Register(
            "BackdropType",
            typeof(WindowBackdropKind),
            typeof(WindowChrome),
            new PropertyMetadata(
                WindowBackdropKind.Auto,
                (d, e) => ((WindowChrome)d)._OnDwmAttributeChanged(WindowChromeDwmAttributes.Backdrop)),
            value => Enum.IsDefined(typeof(WindowBackdropKind), value));

        /// <summary>System backdrop material of the window.  Requires Windows 11 version 22H2; ignored on earlier versions.</summary>
        // DWMWA_SYSTEMBACKDROP_TYPE.
        public WindowBackdropKind BackdropType
        {
            get { return (WindowBackdropKind)GetValue(BackdropTypeProperty); }
            set { SetValue(BackdropTypeProperty, value); }
        }

        /// <summary>Whether the current OS supports caption, text and border colors and corner preferences (Windows 11).</summary>
        public static bool IsCaptionCustomizationSupported
        {
            get { return Utility.IsOSWindows11OrNewer; }
        }

        /// <summary>Whether the current OS supports system backdrops (Windows 11 22H2).</summary>
        public static bool IsBackdropSupported
        {
            get { return Utility.IsWindows11_22H2OrNewer; }
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new WindowChrome();
        }

        private static readonly List<_SystemParameterBoundProperty> _BoundProperties = new List<_SystemParameterBoundProperty>
        {
            new _SystemParameterBoundProperty { DependencyProperty = CornerRadiusProperty, SystemParameterPropertyName = "WindowCornerRadius" },
            new _SystemParameterBoundProperty { DependencyProperty = CaptionHeightProperty, SystemParameterPropertyName = "WindowCaptionHeight" },
            new _SystemParameterBoundProperty { DependencyProperty = ResizeBorderThicknessProperty, SystemParameterPropertyName = "WindowResizeBorderThickness" },
            new _SystemParameterBoundProperty { DependencyProperty = GlassFrameThicknessProperty, SystemParameterPropertyName = "WindowNonClientFrameThickness" },
        };

        public WindowChrome()
        {
            // Effective default values for some of these properties are set to be bindings
            // that set them to system defaults.
            // A more correct way to do this would be to Coerce the value iff the source of the DP was the default value.
            // Unfortunately with the current property system we can't detect whether the value being applied at the time
            // of the coersion is the default.
            foreach (var bp in _BoundProperties)
            {
                // This list must be declared after the DP's are assigned.
                Assert.IsNotNull(bp.DependencyProperty);
                var defaultBinding = new Binding
                {
#if RIBBON_IN_FRAMEWORK
                    Path = new PropertyPath($"(SystemParameters.{bp.SystemParameterPropertyName})"),
#else
                    Source = SystemParameters2.Current,
                    Path = new PropertyPath(bp.SystemParameterPropertyName),
#endif
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };
                _defaultBindings[bp.DependencyProperty] = defaultBinding;
                BindingOperations.SetBinding(this, bp.DependencyProperty, defaultBinding);
            }
        }

        // The bindings to the system defaults set by the constructor, to tell them apart from application values.
        private readonly Dictionary<DependencyProperty, Binding> _defaultBindings = new Dictionary<DependencyProperty, Binding>();

        /// <summary>
        /// Whether the application gave the property a value of its own (local value, style, template or a
        /// binding of its own) instead of leaving the system default bound by the constructor.
        /// </summary>
        internal bool IsExplicitlySet(DependencyProperty dp)
        {
            BindingExpression expression = BindingOperations.GetBindingExpression(this, dp);
            if (expression != null)
            {
                Binding defaultBinding;
                return !_defaultBindings.TryGetValue(dp, out defaultBinding) || !ReferenceEquals(expression.ParentBinding, defaultBinding);
            }

            return DependencyPropertyHelper.GetValueSource(this, dp).BaseValueSource != BaseValueSource.Default;
        }

        /// <summary>Whether <see cref="CaptionHeight"/> was set by the application rather than left to the system default.</summary>
        internal bool IsCaptionHeightSet
        {
            get { return IsExplicitlySet(CaptionHeightProperty); }
        }

        /// <summary>Whether <see cref="GlassFrameThickness"/> was set by the application rather than left to the system default.</summary>
        internal bool IsGlassFrameThicknessSet
        {
            get { return IsExplicitlySet(GlassFrameThicknessProperty); }
        }

        /// <summary>
        /// Creates the chrome that the framework attaches to windows that need DWM attribute or backdrop
        /// management but do not define their own chrome.  It leaves the standard frame untouched.
        /// </summary>
        internal static WindowChrome CreateSystemFrameChrome()
        {
            return new WindowChrome { FrameMode = WindowChromeFrameMode.SystemFrame };
        }

        private void _OnPropertyChangedThatRequiresRepaint()
        {
            var handler = PropertyChangedThatRequiresRepaint;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void _OnDwmAttributeChanged(WindowChromeDwmAttributes attribute)
        {
            var handler = PropertyChangedThatRequiresDwmUpdate;
            if (handler != null)
            {
                handler(this, new WindowChromeDwmAttributesChangedEventArgs(attribute));
            }
        }

        internal event EventHandler PropertyChangedThatRequiresRepaint;

        /// <summary>
        /// Raised when a property that only affects DWM window attributes changed.  Unlike
        /// <see cref="PropertyChangedThatRequiresRepaint"/> this does not require the frame to be recomputed.
        /// </summary>
        internal event EventHandler<WindowChromeDwmAttributesChangedEventArgs> PropertyChangedThatRequiresDwmUpdate;
    }
}
