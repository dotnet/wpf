// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
#if !RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   Implements Ribbon's special style of ToolTip.
    /// </summary>
    public class RibbonToolTip : ToolTip
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonToolTip class.  This overrides
        ///   the default style.
        /// </summary>
        static RibbonToolTip()
        {
            Type ownerType = typeof(RibbonToolTip);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsOpenProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsOpenChanged), new CoerceValueCallback(CoerceIsOpen)));
            PlacementTargetProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnPlacementTargetPropertyChanged));
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        public RibbonToolTip()
        {
            Loaded += new RoutedEventHandler(OnLoaded);
            CustomPopupPlacementCallback = new CustomPopupPlacementCallback(PlaceRibbonToolTip);
        }

        #endregion

        #region VisualStates

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonToolTip));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
            private set { RibbonControlService.SetRibbon(this, value); }
        }

        /// <summary>
        ///     Property changed callback for tooltip PlacementTarget property.
        /// </summary>
        private static void OnPlacementTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonToolTip ribbonToolTip = (RibbonToolTip)d;
            UIElement target = e.NewValue as UIElement;
            if (target == null)
            {
                ribbonToolTip.Ribbon = null;
            }
            else
            {
                ribbonToolTip.Ribbon = RibbonControlService.GetRibbon(target);
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // This method is needed only as a placeholder for the call to IsOpenProperty.OverrideMetadata
            // in the cctor, so that it can override the CoerceValueCallback.
        }

        #endregion VisualStates

        #region Public Properties

        /// <summary>
        ///   Gets or sets the Title property.
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for TitleProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(RibbonToolTip), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnToolTipHeaderPropertyChanged)));

        /// <summary>
        ///   Gets or sets the Description property.
        /// </summary>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for DescriptionProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(RibbonToolTip), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnToolTipHeaderPropertyChanged)));

        /// <summary>
        ///   Gets or sets the ImageSource property.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for ImageSourceProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(RibbonToolTip), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnToolTipHeaderPropertyChanged)));

        /// <summary>
        ///   Gets a value indicating whether the RibbonToolTip has a Header.
        /// </summary>
        public bool HasHeader
        {
            get { return (bool)GetValue(HasHeaderProperty); }
            internal set { SetValue(HasHeaderPropertyKey, value); }
        }

        /// <summary>
        ///     DependencyPropertyKey for HasHeaderProperty.
        /// </summary>
        private static readonly DependencyPropertyKey HasHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly("HasHeader", typeof(bool), typeof(RibbonToolTip), new UIPropertyMetadata(false));

        /// <summary>
        ///   Using a DependencyProperty as the backing store for HasHeaderProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HasHeaderProperty = HasHeaderPropertyKey.DependencyProperty;

        /// <summary>
        ///     Property changed callback for tooltip Header properties.
        ///     Sets the value of HasHeader property accordingly.
        /// </summary>
        private static void OnToolTipHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonToolTip ribbonToolTip = (RibbonToolTip)d;
            ribbonToolTip.HasHeader =
                (!string.IsNullOrEmpty(ribbonToolTip.Title) ||
                !string.IsNullOrEmpty(ribbonToolTip.Description) ||
                ribbonToolTip.ImageSource != null);
        }

        /// <summary>
        ///   Gets or sets the FooterTitle property.
        /// </summary>
        public string FooterTitle
        {
            get { return (string)GetValue(FooterTitleProperty); }
            set { SetValue(FooterTitleProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterTitleProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty FooterTitleProperty =
            DependencyProperty.Register("FooterTitle", typeof(string), typeof(RibbonToolTip), new UIPropertyMetadata(new PropertyChangedCallback(OnToolTipFooterPropertyChanged)));

        /// <summary>
        ///   Gets or sets the FooterDescription property.
        /// </summary>
        public string FooterDescription
        {
            get { return (string)GetValue(FooterDescriptionProperty); }
            set { SetValue(FooterDescriptionProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterDescriptionProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty FooterDescriptionProperty =
            DependencyProperty.Register("FooterDescription", typeof(string), typeof(RibbonToolTip), new UIPropertyMetadata(new PropertyChangedCallback(OnToolTipFooterPropertyChanged)));

        /// <summary>
        ///   Gets or sets the FooterImageSource property.
        /// </summary>
        public ImageSource FooterImageSource
        {
            get { return (ImageSource)GetValue(FooterImageSourceProperty); }
            set { SetValue(FooterImageSourceProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterImageSourceProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty FooterImageSourceProperty =
            DependencyProperty.Register("FooterImageSource", typeof(ImageSource), typeof(RibbonToolTip), new UIPropertyMetadata(new PropertyChangedCallback(OnToolTipFooterPropertyChanged)));

        /// <summary>
        ///   Gets a value indicating whether the RibbonToolTip has a footer.
        /// </summary>
        public bool HasFooter
        {
            get { return (bool)GetValue(HasFooterProperty); }
            internal set { SetValue(HasFooterPropertyKey, value); }
        }

        /// <summary>
        ///     DependencyPropertyKey for HasFooterProperty.
        /// </summary>
        private static readonly DependencyPropertyKey HasFooterPropertyKey =
            DependencyProperty.RegisterReadOnly("HasFooter", typeof(bool), typeof(RibbonToolTip), new UIPropertyMetadata(false));

        /// <summary>
        ///   Using a DependencyProperty as the backing store for HasFooterProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HasFooterProperty = HasFooterPropertyKey.DependencyProperty;

        /// <summary>
        ///     Property changed callback for tooltip footer properties.
        ///     Sets the value of HasFooter property accordingly.
        /// </summary>
        private static void OnToolTipFooterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonToolTip ribbonToolTip = (RibbonToolTip)d;
            ribbonToolTip.HasFooter =
                (!string.IsNullOrEmpty(ribbonToolTip.FooterTitle) ||
                !string.IsNullOrEmpty(ribbonToolTip.FooterDescription) ||
                ribbonToolTip.FooterImageSource != null);
        }

        /// <summary>
        ///    This DependencyProperty is used to determine whether the PlacementTarget is within a RibbonGroup or not.
        /// </summary>
        private static readonly DependencyPropertyKey IsPlacementTargetInRibbonGroupPropertyKey =
                DependencyProperty.RegisterReadOnly("IsPlacementTargetInRibbonGroup", typeof(bool), typeof(RibbonToolTip), new UIPropertyMetadata(false));

        public static readonly DependencyProperty IsPlacementTargetInRibbonGroupProperty = 
                IsPlacementTargetInRibbonGroupPropertyKey.DependencyProperty;

        public bool IsPlacementTargetInRibbonGroup 
        {
            get { return (bool)GetValue(IsPlacementTargetInRibbonGroupProperty); }
            internal set { SetValue(IsPlacementTargetInRibbonGroupPropertyKey, value); }
        }

        #endregion

        #region Private Methods


        /// <summary>
        ///   This CoerceValueCallback hack is used to determine whether the current PlacementTarget is within a RibbonGroup.
        /// </summary> 
        private static object CoerceIsOpen(DependencyObject d, object value)
        {
            if ((bool)value)
            {
                RibbonToolTip toolTip = (RibbonToolTip)d;
                RibbonGroup ribbonGroup = null;
                UIElement placementTarget = toolTip.PlacementTarget;

                // Walk up the visual tree from the PlacementTarget to see if 
                // it belongs to a RibbonGroup.
                DependencyObject element = placementTarget;
                while (element != null)
                {
                    ribbonGroup = element as RibbonGroup;
                    if (ribbonGroup != null)
                    {
                        break;
                    }

                    DependencyObject visualParent = VisualTreeHelper.GetParent(element);
                    if (visualParent == null)
                    {
                        // This special check is for the case that the PlacementTarget is 
                        // within the Popup of a Collapsed RibbonGroup
                        Popup popupParent = LogicalTreeHelper.GetParent(element) as Popup;
                        if (popupParent != null)
                        {
                            ribbonGroup = popupParent.TemplatedParent as RibbonGroup;
                        }
                        break;
                    }

                    element = visualParent;
                }

                // A RibbonGroup is in the QAT is special. Its tooltip should show relative 
                // to the mouse instead of under the Ribbon. All other control in the QAT 
                // are automatically taken care of because they will not be recognized as 
                // belonging to a RibbonGroup.

                bool isToolTipForRibbonGroup = ribbonGroup != null && 
                    (toolTip.PlacementTarget == ribbonGroup || 
                    (VisualTreeHelper.GetChildrenCount(ribbonGroup) > 0 && toolTip.PlacementTarget == VisualTreeHelper.GetChild(ribbonGroup, 0)));

                toolTip.IsPlacementTargetInRibbonGroup = (ribbonGroup != null && (!isToolTipForRibbonGroup || !ribbonGroup.IsInQuickAccessToolBar));
            }

            return value;
        }

        /// <summary>
        ///     RibbonToolTip custom placement logic
        /// </summary>
        /// <param name="popupSize">The size of the popup.</param>
        /// <param name="targetSize">The size of the placement target.</param>
        /// <param name="offset">The Point computed from the HorizontalOffset and VerticalOffset property values.</param>
        /// <returns>An array of possible tooltip placements.</returns>

        private CustomPopupPlacement[] PlaceRibbonToolTip(Size popupSize, Size targetSize, Point offset)
        {
            UIElement placementTarget = this.PlacementTarget;
            double belowOffsetY = 0.0;
            double aboveOffsetY = 0.0;
            double offsetX = FlowDirection == FlowDirection.LeftToRight ? 0.0 : -popupSize.Width;

            if (IsPlacementTargetInRibbonGroup)
            {
                // If the PlacementTarget is within a RibbonGroup we proceed 
                // with the custom placement policy.

                // Walk up the visual tree from PlacementTarget to find the Ribbon 
                // if exists or the root element which is likely a PopupRoot.
                Ribbon ribbon = null;
                DependencyObject rootElement = null;
                DependencyObject element = placementTarget;
                while (element != null)
                {
                    ribbon = element as Ribbon;
                    if (ribbon != null)
                    {
                        break;
                    }

                    rootElement = element;
                    element = VisualTreeHelper.GetParent(element);
                }

                double additionalOffset = 1.0;
                FrameworkElement referenceFE = null;

                if (ribbon != null)
                {
                    additionalOffset = 0.0;
                    referenceFE = ribbon;
                }
                else
                {
                    if (rootElement != null)
                    {
                        referenceFE = rootElement as FrameworkElement;
                    }
                }

                if (referenceFE != null)
                {
                    // When RibbonControl (PlacementTarget) is within a collapsed group RibbonToolTip is 
                    // placed just below the Popup or just above the Popup (in case there is not enough 
                    // screen space left below the Popup).

                    MatrixTransform transform = referenceFE.TransformToDescendant(placementTarget) as MatrixTransform;
                    if (transform != null)
                    {
                        MatrixTransform deviceTransform = new MatrixTransform(RibbonHelper.GetTransformToDevice(referenceFE));
                        GeneralTransformGroup transformGroup = new GeneralTransformGroup();
                        transformGroup.Children.Add(transform);
                        transformGroup.Children.Add(deviceTransform);

                        Point leftTop, rightBottom;
                        transformGroup.TryTransform(new Point(0, 0), out leftTop);
                        transformGroup.TryTransform(new Point(referenceFE.ActualWidth, referenceFE.ActualHeight), out rightBottom);

                        belowOffsetY = rightBottom.Y + additionalOffset;
                        aboveOffsetY = leftTop.Y - popupSize.Height - additionalOffset;
                    }
                }
            }
            else
            {
                // If PlacementTarget isn't within a RibbonGroup we shouldn't have 
                // gotten here in the first place. But now that we are we will make 
                // the best attempt at emulating PlacementMode.Bottom.
                FrameworkElement placementTargetAsFE = placementTarget as FrameworkElement;
                if (placementTargetAsFE != null)
                {
                    belowOffsetY = targetSize.Height;
                    aboveOffsetY = -popupSize.Height;
                }
            }

            // This is the prefered placement, below the ribbon for controls within Ribbon or below the Popup for controls within Popup.
            CustomPopupPlacement placementPreffered = new CustomPopupPlacement(new Point(offsetX, belowOffsetY), PopupPrimaryAxis.Horizontal);

            // This is a fallback placement, if the tooltip will not fit below the ribbon or Popup, place it above the ribbon or Popup.
            CustomPopupPlacement placementFallback = new CustomPopupPlacement(new Point(offsetX, aboveOffsetY), PopupPrimaryAxis.Horizontal);

            return new CustomPopupPlacement[] { placementPreffered, placementFallback };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RibbonHelper.FindAndHookPopup(this, ref _popup);
        }

        private Popup _popup;

        #endregion

        #region UI Automation

        /// <summary>
        ///     Get AutomationPeer for RibbonToolTip
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonToolTipAutomationPeer(this);
        }

        #endregion
    }
}
