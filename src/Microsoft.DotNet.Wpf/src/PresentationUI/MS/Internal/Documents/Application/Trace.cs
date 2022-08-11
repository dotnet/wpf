// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Utility class for Trace switches and methods for XpsViewer.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Utility class for Trace switches and methods for XpsViewer.
/// <summary>
internal static class Trace
{
    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Will permit only internet zone permissions for TraceListeners and is
    /// safe to use inside of asserts for partial trust code.
    /// </summary>
    internal static void SafeWrite(
        BooleanSwitch boolSwitch, string format, params object[] args)
    {
        if (AvTrace.IsWpfTracingEnabledInRegistry())
        {
            System.Diagnostics.Trace.WriteLineIf(
                boolSwitch.Enabled,
                string.Format(
                CultureInfo.CurrentCulture,
                format,
                args),
                boolSwitch.DisplayName);
        }
    }

    /// <summary>
    /// Will permit only internet zone permissions for TraceListeners and is
    /// safe to use inside of asserts for partial trust code.
    /// </summary>
    internal static void SafeWriteIf(
        bool condition,
        BooleanSwitch boolSwitch,
        string format,
        params object[] args)
    {
        if (AvTrace.IsWpfTracingEnabledInRegistry())
        {
            System.Diagnostics.Trace.WriteLineIf(
                boolSwitch.Enabled && condition,
                string.Format(
                CultureInfo.CurrentCulture,
                format,
                args),
                boolSwitch.DisplayName);
        }
    }

    #endregion Internal Methods

    #region Internal Fields
    //--------------------------------------------------------------------------
    // Internal Fields
    //--------------------------------------------------------------------------

    internal static BooleanSwitch File = new BooleanSwitch(
        FileSwitchName, FileSwitchName, "1");
    internal static BooleanSwitch Packaging = new BooleanSwitch(
        PackagingSwitchName, PackagingSwitchName, "1");
    internal static BooleanSwitch Presentation = new BooleanSwitch(
        PresentationSwitchName, PresentationSwitchName, "1");
    internal static BooleanSwitch Rights = new BooleanSwitch(
        RightsSwitchName, RightsSwitchName, "1");
    internal static BooleanSwitch Signatures = new BooleanSwitch(
        SignaturesSwitchName, SignaturesSwitchName, "1");
    #endregion Internal Fields

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    private const string FileSwitchName = "XpsViewerFile";
    private const string PackagingSwitchName = "XpsViewerPackaging";
    private const string PresentationSwitchName = "XpsViewerUI";
    private const string RightsSwitchName = "XpsViewerRights";
    private const string SignaturesSwitchName = "XpsViewerSignatures";
    #endregion Private Fields
}
}
