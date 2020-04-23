// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Reflection;
using System.Runtime.Versioning;

using SWF = System.Windows.Forms;
using SD = System.Drawing;
using SW = System.Windows;
using SWM = System.Windows.Media;
using SWI = System.Windows.Input;
using SWC = System.Windows.Controls;

namespace System.Windows.Forms.Integration
{
    internal sealed class WindowsFormsHostPropertyMap : PropertyMap
    {
        public WindowsFormsHostPropertyMap(WindowsFormsHost host)
            : base(host)
        {
            InitializeDefaultTranslators();
            ResetAll();
        }

        /// <summary>
        ///     Initialize the list of things we translate by default, like 
        ///     Background.
        /// </summary>
        private void InitializeDefaultTranslators()
        {
            DefaultTranslators.Add("Background", BackgroundPropertyTranslator);
            DefaultTranslators.Add("FlowDirection", FlowDirectionPropertyTranslator);
            DefaultTranslators.Add("FontStyle", FontStylePropertyTranslator);
            DefaultTranslators.Add("FontWeight", FontWeightPropertyTranslator);
            DefaultTranslators.Add("FontFamily", FontFamilyPropertyTranslator);
            DefaultTranslators.Add("FontSize", FontSizePropertyTranslator);
            DefaultTranslators.Add("Foreground", ForegroundPropertyTranslator);
            DefaultTranslators.Add("IsEnabled", IsEnabledPropertyTranslator);
            DefaultTranslators.Add("Padding", PaddingPropertyTranslator);
            DefaultTranslators.Add("Visibility", VisibilityPropertyTranslator);

            //Note: there's no notification when the ambient cursor changes, so we
            //can't do a normal mapping for this and have it work.  See the note in
            //WinFormsAdapter.Cursor.
            DefaultTranslators.Add("Cursor", EmptyPropertyTranslator);
            DefaultTranslators.Add("ForceCursor", EmptyPropertyTranslator);
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: HostUtils.GetBitmapForWindowsFormsHost
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private void BackgroundPropertyTranslator(object host, string propertyName, object value)
        {
            SWM.Brush brush = value as SWM.Brush;

            if (value == null)
            {
                //If they passed null (not to be confused with "passed us a non-null non-Brush"),
                //we should look up our parent chain.
                DependencyObject parent = host as WindowsFormsHost;
                do
                {
                    brush = (Brush)parent.GetValue(SWC.Control.BackgroundProperty);
                    parent = VisualTreeHelper.GetParent(parent);
                } while (parent != null && brush == null);
            }

            WinFormsAdapter adapter = GetAdapter(host);

            if (adapter != null && brush != null)
            {
                WindowsFormsHost windowsFormsHost = host as WindowsFormsHost;
                if (windowsFormsHost != null)
                {
                    SWF.Control child = windowsFormsHost.Child;

                    if (HostUtils.BrushIsSolidOpaque(brush))
                    {
                        bool ignore;
                        SD.Color wfColor = WindowsFormsHostPropertyMap.TranslateSolidOrGradientBrush(brush, out ignore);
                        adapter.BackColor = wfColor;
                    }
                    else
                    {
                        SD.Bitmap backgroundImage = HostUtils.GetBitmapForWindowsFormsHost(windowsFormsHost, brush);
                        HostUtils.SetBackgroundImage(adapter, child, backgroundImage);
                    }
                }
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        private void FlowDirectionPropertyTranslator(object host, string propertyName, object value)
        {
            SWF.Control childControl = GetChildControl(host, FlowDirectionPropertyTranslator, value);
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null && childControl != null)
            {
                if (value is SW.FlowDirection)
                {
                    const BindingFlags memberAccess = BindingFlags.Public | BindingFlags.Instance;

                    //use reflection to see if this control supports the RightToLeftLayout property
                    PropertyInfo propertyInfo = childControl.GetType().GetProperty("RightToLeftLayout", memberAccess);
                    switch ((SW.FlowDirection)value)
                    {
                        case SW.FlowDirection.RightToLeft:
                            adapter.RightToLeft = SWF.RightToLeft.Yes;
                            if (propertyInfo != null) { propertyInfo.SetValue(childControl, true, null); }
                            break;
                        case SW.FlowDirection.LeftToRight:
                            adapter.RightToLeft = SWF.RightToLeft.No;
                            if (propertyInfo != null) { propertyInfo.SetValue(childControl, false, null); }
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: new Font
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void FontFamilyPropertyTranslator(object host, string propertyName, object value)
        {
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null)
            {
                SWM.FontFamily family = value as SWM.FontFamily;
                if (family != null)
                {
                    string familySource = family.Source;
                    adapter.Font = new SD.Font(familySource, adapter.Font.Size, adapter.Font.Style);
                }
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: new Font
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void FontStylePropertyTranslator(object host, string propertyName, object value)
        {
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null)
            {
                if (value is SW.FontStyle)
                {
                    SD.FontStyle style;
                    if ((SW.FontStyle)value == SW.FontStyles.Normal)
                    {
                        style = SD.FontStyle.Regular;
                    }
                    else
                    {
                        style = SD.FontStyle.Italic;
                    }
                    if (HostUtils.FontWeightIsBold(Host.FontWeight))
                    {
                        style |= SD.FontStyle.Bold;
                    }
                    adapter.Font = new SD.Font(CurrentFontFamily, CurrentFontSize, style);
                }
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: new Font
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void FontWeightPropertyTranslator(object host, string propertyName, object value)
        {
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null && value is SW.FontWeight)
            {
                SD.FontStyle style = CurrentFontStyle;
                if (HostUtils.FontWeightIsBold((SW.FontWeight)value))
                {
                    style |= SD.FontStyle.Bold;
                }
                adapter.Font = new SD.Font(CurrentFontFamily, CurrentFontSize, style);
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: new Font
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void FontSizePropertyTranslator(object host, string propertyName, object value)
        {
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null && value is double)
            {
                double pointSize = Convert.FontSizeToSystemDrawing((double)value);
                adapter.Font = new SD.Font(CurrentFontFamily, (float)pointSize, CurrentFontStyle);
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        private void ForegroundPropertyTranslator(object host, string propertyName, object value)
        {
            SWM.Brush brush = value as SWM.Brush;
            if (brush != null)
            {
                bool defined;
                SD.Color wfColor = WindowsFormsHostPropertyMap.TranslateSolidOrGradientBrush(brush, out defined);
                if (defined)
                {
                    WinFormsAdapter adapter = GetAdapter(host);
                    if (adapter != null)
                    {
                        adapter.ForeColor = wfColor;
                    }
                }
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        private void IsEnabledPropertyTranslator(object host, string propertyName, object value)
        {
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null && value is bool)
            {
                adapter.Enabled = (bool)value;
            }
        }

        /// <summary>
        ///     Translator for individual property
        /// </summary>
        private void PaddingPropertyTranslator(object host, string propertyName, object value)
        {
            SWF.Control childControl = GetChildControl(host, PaddingPropertyTranslator, value);
            WinFormsAdapter adapter = GetAdapter(host);
            if (adapter != null && childControl != null)
            {
                if (value is SW.Thickness)
                {
                    childControl.Padding = Convert.ToSystemWindowsFormsPadding((SW.Thickness)value);
                }
            }
        }

        /// <summary>
        ///     Translator for Visibility property
        /// </summary>
        private void VisibilityPropertyTranslator(object host, string propertyName, object value)
        {
            if (value is SW.Visibility)
            {
                WindowsFormsHost windowsFormsHost = host as WindowsFormsHost;
                if (windowsFormsHost != null && windowsFormsHost.Child != null)
                {
                    //Visible for Visible, not Visible for Hidden/Collapsed
                    windowsFormsHost.Child.Visible = ((SW.Visibility)value == SW.Visibility.Visible);
                }
            }
        }

        /// <summary>
        ///     The WindowsFormsHost we're mapping
        /// </summary>
        private WindowsFormsHost Host
        {
            get
            {
                return (WindowsFormsHost)SourceObject;
            }
        }

        /// <summary>
        ///     Gets the child control if we have one.  If we don't, cache the translator
        ///     and property value so that we can restore them when the control is set.
        /// <param name="host">WindowsFormsHost</param>
        /// <param name="translator">The delegate to be called if this property is to be set later</param>
        /// <param name="value">The value the property will have if it needs to be set later</param>
        /// </summary>
        private SWF.Control GetChildControl(object host, PropertyTranslator translator, object value)
        {
            WindowsFormsHost windowsFormsHost = host as WindowsFormsHost;
            if (windowsFormsHost == null || windowsFormsHost.Child == null)
            {
                return null;
            }
            return windowsFormsHost.Child;
        }

        /// <summary>
        ///     The HostContainer, if any.
        /// </summary>
        /// <param name="host">WindowsFormsHost</param>
        internal static WinFormsAdapter GetAdapter(object host)
        {
            WindowsFormsHost windowsFormsHost = host as WindowsFormsHost;
            if (windowsFormsHost == null)
            {
                return null;
            }
            return windowsFormsHost.HostContainerInternal;
        }

        /// <summary>
        ///     Translate the fontsize of this WFH to a font size equivalent to one
        ///     that a SD.Font would use.
        /// </summary>
        private float CurrentFontSize
        {
            get
            {
                return (float)(Convert.FontSizeToSystemDrawing(Host.FontSize));
            }
        }

        /// <summary>
        ///     Translate the FontStyle and FontWeight of this WFH to a SD.Font
        ///     equivalent.
        /// </summary>
        private SD.FontStyle CurrentFontStyle
        {
            get
            {
                SD.FontStyle style;

                if (Host.FontStyle == SW.FontStyles.Normal)
                {
                    style = SD.FontStyle.Regular;
                }
                else
                {
                    style = SD.FontStyle.Italic;
                }

                if (HostUtils.FontWeightIsBold(Host.FontWeight))
                {
                    style |= SD.FontStyle.Bold;
                }

                return style;
            }
        }
        /// <summary>
        ///     The current FontFamily of this WFH
        /// </summary>
        private string CurrentFontFamily
        {
            get
            {
                return Host.FontFamily.Source;
            }
        }

        /// <summary>
        ///     This method translates Avalon SD.Brushes into
        ///     WindowsForms color objects
        /// </summary>
        private static SD.Color TranslateSolidOrGradientBrush(SWM.Brush brush, out bool defined)
        {
            SWM.Color brushColor;
            SD.Color wfColor = SD.Color.Empty;
            defined = false;
            SWM.SolidColorBrush solidColorBrush = brush as SWM.SolidColorBrush;
            if (solidColorBrush != null)
            {
                brushColor = solidColorBrush.Color;
                defined = true;
                wfColor = Convert.ToSystemDrawingColor(brushColor);
            }
            else
            {
                SWM.GradientBrush gradientBrush = brush as SWM.GradientBrush;
                if (gradientBrush != null)
                {
                    SWM.GradientStopCollection grads = gradientBrush.GradientStops;
                    if (grads != null)
                    {
                        SWM.GradientStop firstStop = grads[0];
                        if (firstStop != null)
                        {
                            brushColor = firstStop.Color;
                            defined = true;
                            wfColor = Convert.ToSystemDrawingColor(brushColor);
                        }
                    }
                }
            }
            return wfColor;
        }
    }
}
