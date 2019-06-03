// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;
using System.Windows.Automation.Peers;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Separator control is a simple Control subclass that is used in different styles
    /// depend on container control. Common usage is inside ListBox, ComboBox, MenuItem and ToolBar.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string    
    public class Separator : Control
    {
        static Separator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Separator), new FrameworkPropertyMetadata(typeof(Separator)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Separator));

            IsEnabledProperty.OverrideMetadata(typeof(Separator), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            ControlsTraceLogger.AddControl(TelemetryControls.Separator);
        }

        internal static void PrepareContainer(Control container)
        {
            if (container != null)
            {
                // Disable the control and set the alignment to stretch
                container.IsEnabled = false;
                container.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            }
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SeparatorAutomationPeer(this);
        }

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}
