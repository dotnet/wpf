// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;

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

        internal static void SafeWrite(BooleanSwitch boolSwitch, string format, params ReadOnlySpan<object> args)
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

        internal static void SafeWriteIf(
            bool condition,
            BooleanSwitch boolSwitch,
            string format,
            params ReadOnlySpan<object> args)
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

        internal static readonly BooleanSwitch File = new(FileSwitchName, FileSwitchName, "1");
        internal static readonly BooleanSwitch Packaging = new(PackagingSwitchName, PackagingSwitchName, "1");
        internal static readonly BooleanSwitch Presentation = new(PresentationSwitchName, PresentationSwitchName, "1");
        internal static readonly BooleanSwitch Rights = new(RightsSwitchName, RightsSwitchName, "1");
        internal static readonly BooleanSwitch Signatures = new(SignaturesSwitchName, SignaturesSwitchName, "1");
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
