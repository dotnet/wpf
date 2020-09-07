// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationFramework;

namespace MS.Internal.Telemetry.PresentationFramework
{
    [Flags]
    internal enum TelemetryControls : long
    {
        None =              0x0000000000000000,
        Border =            0x0000000000000001,
        Button =            0x0000000000000002,
        Calendar =          0x0000000000000004,
        Canvas =            0x0000000000000008,
        CheckBox =          0x0000000000000010,
        ComboBox =          0x0000000000000020,
        ContentControl =    0x0000000000000040,
        DataGrid =          0x0000000000000080,
        DatePicker =        0x0000000000000100,
        DockPanel =         0x0000000000000200,
        DocumentViewer =    0x0000000000000400,
        Expander =          0x0000000000000800,
        Frame =             0x0000000000001000,
        Grid =              0x0000000000002000,
        GridSplitter =      0x0000000000004000,
        GroupBox =          0x0000000000008000,
        Image =             0x0000000000010000,
        Label =             0x0000000000020000,
        ListBox =           0x0000000000040000,
        ListView =          0x0000000000080000,
        MediaElement =      0x0000000000100000,
        Menu =              0x0000000000200000,
        PasswordBox =       0x0000000000400000,
        ProgressBar =       0x0000000000800000,
        RadioButton =       0x0000000001000000,
        RichTextBox =       0x0000000002000000,
        ScrollBar =         0x0000000004000000,
        ScrollViewer =      0x0000000008000000,
        Separator =         0x0000000010000000,
        Slider =            0x0000000020000000,
        StackPanel =        0x0000000040000000,
        StatusBar =         0x0000000080000000,
        TabControl =        0x0000000100000000,
        TextBlock =         0x0000000200000000,
        TextBox =           0x0000000400000000,
        ToolBar =           0x0000000800000000,
        ToolBarPanel =      0x0000001000000000,
        ToolBarTray =       0x0000002000000000,
        TreeView =          0x0000004000000000,
        ViewBox =           0x0000008000000000,
        WebBrowser =        0x0000010000000000,
        WrapPanel =         0x0000020000000000,
        FlowDocument =      0x0000040000000000
    }

    internal static class ControlsTraceLogger
    {
        internal static void LogUsedControlsDetails()
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(ControlsUsed, TelemetryEventSource.MeasuresOptions(), new
            {
                ControlsUsedInApp = _telemetryControls
            });
        }

        internal static void AddControl(TelemetryControls control)
        {
            _telemetryControls |= control;
        }

        private static readonly string ControlsUsed = "ControlsUsed";
        private static TelemetryControls _telemetryControls = TelemetryControls.None;
    }

}