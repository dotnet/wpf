// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal
{
    // WPF's builds are seeing warnings as a result of using LocalAppContext in mutliple assemblies.
    // that have internalsVisibleTo attribute set between them - which results in the warning.
    // We don't have a way of suppressing this warning effectively until the shared copies of LocalAppContext and
    // AppContextDefaultValues have pragmas added to suppress warning 436
#pragma warning disable 436
    internal static class BuildTasksAppContextSwitches
    {
        #region DoNotUseSha256ForMarkupCompilerChecksumAlgorithm

        internal const string DoNotUseSha256ForMarkupCompilerChecksumAlgorithmSwitchName = "Switch.System.Windows.Markup.DoNotUseSha256ForMarkupCompilerChecksumAlgorithm";
        private static int _doNotUseSha256ForMarkupCompilerChecksumAlgorithm;

        public static bool DoNotUseSha256ForMarkupCompilerChecksumAlgorithm
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(DoNotUseSha256ForMarkupCompilerChecksumAlgorithmSwitchName, ref _doNotUseSha256ForMarkupCompilerChecksumAlgorithm);
            }
        }

        #endregion
    }
#pragma warning restore 436
}
