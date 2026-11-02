// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace MS.Internal
{
    internal static class ReachAppContextSwitches
    {
        // Opt-out of enforcing LocalName+NamespaceURI checks for XPS PageContent/DocumentReference
        // signature coverage. When true, reverts to the legacy reader.Name comparison.
        internal const string OptOutOfXpsPageSignatureCoverageEnforcementSwitchName = "Switch.System.Windows.Xps.OptOutOfPageSignatureCoverageEnforcement";
        private static int _optOutOfXpsPageSignatureCoverageEnforcement;
        public static bool OptOutOfXpsPageSignatureCoverageEnforcement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue(OptOutOfXpsPageSignatureCoverageEnforcementSwitchName, ref _optOutOfXpsPageSignatureCoverageEnforcement);
        }

        // Opt-out of requiring cryptographic binding verification for XPS Signature@Id.
        // When true, reverts to the legacy XmlConvert.DecodeName(sig.Id) path.
        internal const string OptOutOfXpsSignatureIdBindingVerificationSwitchName = "Switch.System.Windows.Xps.OptOutOfSignatureIdBindingVerification";
        private static int _optOutOfXpsSignatureIdBindingVerification;
        public static bool OptOutOfXpsSignatureIdBindingVerification
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue(OptOutOfXpsSignatureIdBindingVerificationSwitchName, ref _optOutOfXpsSignatureIdBindingVerification);
        }

        private static bool GetCachedSwitchValue(string switchName, ref int cachedValue)
        {
            if (cachedValue < 0)
            {
                return false;
            }

            if (cachedValue > 0)
            {
                return true;
            }

            bool isEnabled = AppContext.TryGetSwitch(switchName, out bool value) && value;
            cachedValue = isEnabled ? 1 : -1;
            return isEnabled;
        }
    }
}
