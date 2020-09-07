// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//      Proxy registration.

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

namespace UIAutomationClientsideProviders
{
    /// <summary>
    /// Class containing Client-Side Providers for various common Win32 controls
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class UIAutomationClientSideProviders
#else
    public static class UIAutomationClientSideProviders
#endif
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        /// <summary>
        /// Table of client-side providers for various common Win32 controls
        /// </summary>
        static public ClientSideProviderDescription[] ClientSideProviderDescriptionTable =
        {
            // Windows proxies
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsButton.Create), "Button"),
            // Create a dummy proxy for ComboboxEx32, which will be hidden from the LogicalTree, combo will always be presented by WindowsComboBox
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsNonControl.Create), "ComboBoxEx32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsComboBox.Create), "ComboBox"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsListBox.Create), "ComboLBox"), // List portion of combo
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsListBox.Create), "ListBox"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsHyperlink.Create), "SysLink"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsIPAddress.Create), "SysIPAddress32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsSysHeader.Create), "SysHeader32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsListView.Create), "SysListView32", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRichEdit.Create), "RichEdit"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRichEdit.Create), "RichEdit20A"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRichEdit.Create), "RichEdit20W"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRichEdit.Create), "RichEdit50W"),
            // The winforms control WindowForms10.RichEdit20W.app11c7a8c does not get the a match
            // for richedit so assume the follow patial match will get it
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRichEdit.Create), ".RichEdit20", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsEditBox.Create), "Edit"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsSpinner.Create), "msctls_updown32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsUpDown.Create), "msctls_updown32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsStatic.Create), "Static", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsStatusBar.Create), "msctls_statusbar32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsTab.Create), "SysTabControl32", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsToolbar.Create), "ToolbarWindow32", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsMenu.Create), "#32768"),
            // this proxy has a dependancy on Microsoft.mshtml.dll which is an interop dll created by tlbimp.exe.
            // A wrapper needs to be created to get rid of the use of this dll.  this must happen before this proxy
            // can be reenabled.
            //        new ClientSideProviderDescription( new ClientSideProviderFactoryCallback( MS.Internal.AutomationProxiesInternetExplorerProxy.Create ),     "Internet Explorer_Server" ),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsSlider.Create), "msctls_trackbar32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsProgressBar.Create), "msctls_progress32"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsTreeView.Create), "SysTreeView32", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsTooltip.Create), "tooltips_class32", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsTooltip.Create), "#32774"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsRebar.Create), "ReBarWindow32"),
            // Add entries for other window classes here...

            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsAltTab.Create), "#32771"),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsContainer.Create), "#32770"),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsContainer.Create), "AfxControlBar", null, ClientSideProviderMatchIndicator.AllowSubstringMatch),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsStartMenu.Create), "BaseBar"),

            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsNonControl.Create), "WorkerW"),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsNonControl.Create), "SHELLDLL_DefView"),

            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsFormsHelper.Create), "WindowsForms", null, ClientSideProviderMatchIndicator.AllowSubstringMatch), 

            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.NonClientArea.Create), "#nonclient"), 
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.NonClientArea.CreateMenuBarItem), "#nonclientmenubar"),
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.NonClientArea.CreateSystemMenu), "#nonclientsysmenu"),


            // this one handles determing the focused item when in menu mode...
            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.WindowsMenu.CreateFocusedMenuItem), "#user32focusedmenu"),

            new ClientSideProviderDescription(new ClientSideProviderFactoryCallback(MS.Internal.AutomationProxies.MsaaNativeProvider.Create), null)
        };
        #endregion
    }
}
