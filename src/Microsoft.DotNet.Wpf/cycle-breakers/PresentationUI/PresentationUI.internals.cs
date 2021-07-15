// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using MS.Internal.PresentationCore;

[assembly:InternalsVisibleTo(BuildInfo.PresentationFramework)]

// This is the minimum set of surface area required to enable PresentationFramework to build.

namespace MS.Internal.Documents.Application
{
    internal sealed partial class DocumentStream
    {
        internal static readonly string XpsFileExtension;
    }

    internal partial struct DocumentApplicationState
    {
        private int _dummyPrimitive;
        public DocumentApplicationState(double zoom, double horizontalOffset, double verticalOffset, int maxPagesAcross) { throw null; }
        public double HorizontalOffset { get { throw null; } }
        public int MaxPagesAcross { get { throw null; } }
        public double VerticalOffset { get { throw null; } }
        public double Zoom { get { throw null; } }
    }
}

namespace MS.Internal.Documents
{
    internal sealed partial class DocumentApplicationDocumentViewer : System.Windows.Controls.DocumentViewer
    {
        public static System.Windows.Input.RoutedUICommand RequestSigners { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ShowRMCredentialManager { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ShowRMPermissions { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ShowRMPublishingUI { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand ShowSignatureSummary { get { throw null; } }
        public static System.Windows.Input.RoutedUICommand Sign { get { throw null; } }
        public MS.Internal.Documents.Application.DocumentApplicationState StoredDocumentApplicationState { get { throw null; } set { } }
        public void SetUIToStoredState() { }
    }
    
    internal partial class FindToolBar : System.Windows.Controls.ToolBar //, System.Windows.Markup.IComponentConnector
    {
        public FindToolBar() { }
        public bool DocumentLoaded { set { } }
        public bool FindEnabled { get { throw null; } }
        public bool MatchAlefHamza { get { throw null; } }
        public bool MatchCase { get { throw null; } }
        public bool MatchDiacritic { get { throw null; } }
        public bool MatchKashida { get { throw null; } }
        public bool MatchWholeWord { get { throw null; } }
        public string SearchText { get { throw null; } }
        public bool SearchUp { get { throw null; } set { } }
        public event System.EventHandler FindClicked { add { } remove { } }
        public void GoToTextBox() { }
    }
}
