// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Windows.Input;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    public static class RibbonCommands
    {
        public static RoutedUICommand AddToQuickAccessToolBarCommand { get; private set; }
        public static RoutedUICommand RemoveFromQuickAccessToolBarCommand { get; private set; }
        public static RoutedUICommand MinimizeRibbonCommand { get; private set; }
        public static RoutedUICommand MaximizeRibbonCommand { get; private set; }
        public static RoutedUICommand ShowQuickAccessToolBarAboveRibbonCommand { get; private set; }
        public static RoutedUICommand ShowQuickAccessToolBarBelowRibbonCommand { get; private set; }

        static RibbonCommands()
        {
            AddToQuickAccessToolBarCommand = new RoutedUICommand(RibbonContextMenu.AddToQATText, "AddToQuickAccessToolBar", typeof(RibbonCommands));
            RemoveFromQuickAccessToolBarCommand = new RoutedUICommand(RibbonContextMenu.RemoveFromQATText, "RemoveFromQuickAccessToolBar", typeof(RibbonCommands));
            MinimizeRibbonCommand = new RoutedUICommand(RibbonContextMenu.MinimizeTheRibbonText, "MinimizeRibbon", typeof(RibbonCommands));
            MaximizeRibbonCommand = new RoutedUICommand(RibbonContextMenu.MaximizeTheRibbonText, "MaximizeRibbon", typeof(RibbonCommands));
            ShowQuickAccessToolBarAboveRibbonCommand = new RoutedUICommand(RibbonContextMenu.ShowQATAboveText, "ShowQuickAccessToolBarAboveRibbon", typeof(RibbonCommands));
            ShowQuickAccessToolBarBelowRibbonCommand = new RoutedUICommand(RibbonContextMenu.ShowQATBelowText, "ShowQuickAccessToolBarBelowRibbon", typeof(RibbonCommands));
        }
    }
}
