// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Forms.Integration;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

using SWC = System.Windows.Controls;
using SD = System.Drawing;
using SW = System.Windows;
using SWM = System.Windows.Media;
using SWF = System.Windows.Forms;
using SWI = System.Windows.Input;

namespace System.Windows.Forms.Integration
{
    internal sealed class ElementHostPropertyMap : PropertyMap
    {
        //Since the host controls our lifetime, we shouldn't be disposing it.
#pragma warning disable 1634, 1691
#pragma warning disable 56524
        private ElementHost _host;
#pragma warning restore 1634, 1691, 56524
        public ElementHostPropertyMap(ElementHost host)
            : base(host)
        {
            _host = host;
            InitializeDefaultTranslators();
            ResetAll();
        }

        /// <summary>
        ///     Initialize the list of things we translate by default, like 
        ///     BackColor.
        /// </summary>
        private void InitializeDefaultTranslators()
        {
            DefaultTranslators.Add("BackColor", BackgroundPropertyTranslator);
            DefaultTranslators.Add("BackgroundImage", BackgroundPropertyTranslator);
            DefaultTranslators.Add("BackgroundImageLayout", BackgroundPropertyTranslator);
            DefaultTranslators.Add("Cursor", CursorPropertyTranslator);
            DefaultTranslators.Add("Enabled", EnabledPropertyTranslator);
            DefaultTranslators.Add("Font", FontPropertyTranslator);
            DefaultTranslators.Add("RightToLeft", RightToLeftPropertyTranslator);
            DefaultTranslators.Add("Visible", VisiblePropertyTranslator);
            DefaultTranslators.Add("ImeMode", ImeModePropertyTranslator);
        }

        /// <summary>
        ///     Translator for BackColor, BackgroundImage, and BackgroundImageLayout
        /// </summary>
        private static void BackgroundPropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null)
            {
                UpdateBackgroundImage(elementHost);
            }
        }

        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: HostUtils.GetCoveredPortionOfBitmap and HostUtils.GetBitmapOfControl
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private static void UpdateBackgroundImage(ElementHost host)
        {
            if (host != null && host.HostContainerInternal != null)
            {
                if (host.BackColorTransparent)
                {
                    Control parent = host.Parent;
                    if (parent != null && parent.Visible)
                    {
                        using (SD.Bitmap parentBitmap = HostUtils.GetCoveredPortionOfBitmap(parent, host))
                        {
                            host.HostContainerInternal.Background = new SWM.ImageBrush(Convert.ToSystemWindowsMediaImagingBitmapImage(parentBitmap));
                        }
                    }
                }
                else
                {
                    using (SD.Bitmap elementHostBitmap = HostUtils.GetBitmapOfControl(host, host))
                    {
                        host.HostContainerInternal.Background = new SWM.ImageBrush(Convert.ToSystemWindowsMediaImagingBitmapImage(elementHostBitmap));
                    }
                }
            }
        }

        /// <summary>
        ///     Translator for Cursor
        /// </summary>
        private static void CursorPropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null)
            {
                AvalonAdapter adapter = elementHost.HostContainerInternal;
                if (adapter != null)
                {
                    //Note: Allow nulls to propagate
                    SWF.Cursor fromCursor = value as SWF.Cursor;
                    SWI.Cursor toCursor = Convert.ToSystemWindowsInputCursor(fromCursor);
                    adapter.Cursor = toCursor;
                }
            }
        }

        /// <summary>
        ///     Translator for Enabled
        /// </summary>
        private static void EnabledPropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null)
            {
                AvalonAdapter adapter = elementHost.HostContainerInternal;
                if (adapter != null && value is bool)
                {
                    adapter.IsEnabled = (bool)value;
                }
            }
        }

        /// <summary>
        ///     Translator for Font
        /// </summary>
        private static void FontPropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            SD.Font wfFont = value as SD.Font;

            if (elementHost != null && wfFont != null)
            {
                AvalonAdapter adapter = elementHost.HostContainerInternal;
                if (adapter != null)
                {
                    adapter.SetValue(SWC.Control.FontSizeProperty, Convert.SystemDrawingFontToSystemWindowsFontSize(wfFont));
                    adapter.SetValue(SWC.Control.FontFamilyProperty, Convert.ToSystemWindowsFontFamily(wfFont.FontFamily));
                    adapter.SetValue(SWC.Control.FontWeightProperty, Convert.ToSystemWindowsFontWeight(wfFont));
                    adapter.SetValue(SWC.Control.FontStyleProperty, Convert.ToSystemWindowsFontStyle(wfFont));

                    SWC.TextBlock childTextBlock = elementHost.Child as SWC.TextBlock;
                    if (childTextBlock != null)
                    {
                        TextDecorationCollection decorations = new TextDecorationCollection();
                        if (wfFont.Underline) { decorations.Add(TextDecorations.Underline); };
                        if (wfFont.Strikeout) { decorations.Add(TextDecorations.Strikethrough); }
                        childTextBlock.TextDecorations = decorations;
                    }
                }
            }
        }

        /// <summary>
        ///     Translator for ImeMode
        /// </summary>
        private static void ImeModePropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null && elementHost.HwndSource != null)
            {
                elementHost.SyncHwndSrcImeStatus();
            }
        }

        /// <summary>
        ///     Translator for RightToLeft
        /// </summary>
        private static void RightToLeftPropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null)
            {
                AvalonAdapter adapter = elementHost.HostContainerInternal;
                if (adapter != null && value is SWF.RightToLeft)
                {
                    SWF.RightToLeft fromRTL = (SWF.RightToLeft)value;
                    SW.FlowDirection toFlowDirection = ((fromRTL == SWF.RightToLeft.Yes) ? SW.FlowDirection.RightToLeft : SW.FlowDirection.LeftToRight);
                    adapter.FlowDirection = toFlowDirection;
                }
            }
        }

        /// <summary>
        ///     Translator for Visible
        /// </summary>
        private static void VisiblePropertyTranslator(object host, string propertyName, object value)
        {
            ElementHost elementHost = host as ElementHost;
            if (elementHost != null)
            {
                AvalonAdapter adapter = elementHost.HostContainerInternal;
                if (value is bool && adapter != null)
                {
                    bool fromVisible = (bool)value;
                    SW.Visibility toVisibility = ((fromVisible) ? SW.Visibility.Visible : SW.Visibility.Hidden);
                    adapter.Visibility = toVisibility;
                }
            }
        }
    }
}
