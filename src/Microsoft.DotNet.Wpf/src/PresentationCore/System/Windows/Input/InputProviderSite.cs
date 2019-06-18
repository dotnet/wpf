// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;
using System.Windows.Threading;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     The object which input providers use to report input to the input
    ///     manager.
    /// </summary>
    internal class InputProviderSite : IDisposable
    {
        /// <SecurityNote>
        ///     Critical: This code creates critical data in the form of InputManager and InputProvider
        /// </SecurityNote>
        internal InputProviderSite(InputManager inputManager, IInputProvider inputProvider)
        {
            _inputManager = new SecurityCriticalDataClass<InputManager>(inputManager);
            _inputProvider = new SecurityCriticalDataClass<IInputProvider>(inputProvider);
        }

        /// <summary>
        ///     Returns the input manager that this site is attached to.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: We do not want to expose the Input manager in the SEE
        ///     TreatAsSafe: This code has a demand in it
        /// </SecurityNote>
        public InputManager InputManager
        {
            get
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                return CriticalInputManager;
            }
        }

        /// <summary>
        ///     Returns the input manager that this site is attached to.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: We do not want to expose the Input manager in the SEE
        /// </SecurityNote>
        internal InputManager CriticalInputManager
        {
            get
            {
                return _inputManager.Value;
            }
        }

        /// <summary>
        ///     Unregisters this input provider.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code accesses critical data (InputManager and InputProvider).
        ///     TreatAsSafe: The critical data is not exposed outside this call
        /// </SecurityNote>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_inputManager != null && _inputProvider != null)
                {
                    _inputManager.Value.UnregisterInputProvider(_inputProvider.Value);
                }
                _inputManager = null;
                _inputProvider = null;
            }
        }

        /// <summary>
        /// Returns true if the CompositionTarget is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        /// <summary>
        ///     Reports input to the input manager.
        /// </summary>
        /// <returns>
        ///     Whether or not any event generated as a consequence of this
        ///     event was handled.
        /// </returns>
        /// <remarks>
        ///  Do we really need this?  Make the "providers" call InputManager.ProcessInput themselves.
        ///  we currently need to map back to providers for other reasons.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical:This code is critical and can be used in event spoofing. It also accesses
        ///     InputManager and calls into ProcessInput which is critical.
        /// </SecurityNote>
        public bool ReportInput(InputReport inputReport)
        {
            if(IsDisposed)
            {
                throw new ObjectDisposedException(SR.Get(SRID.InputProviderSiteDisposed));
            }

            bool handled = false;

            InputReportEventArgs input = new InputReportEventArgs(null, inputReport);
            input.RoutedEvent=InputManager.PreviewInputReportEvent;

            if(_inputManager != null)
            {
                handled = _inputManager.Value.ProcessInput(input);
            }

            return handled;
        }

        private bool _isDisposed;
        /// <SecurityNote>
        ///     Critical: This object should not be exposed in the SEE as it can be
        ///     used for input spoofing
        /// </SecurityNote>
        private SecurityCriticalDataClass<InputManager> _inputManager;
        /// <SecurityNote>
        ///     Critical: This object should not be exposed in the SEE as it can be
        ///     used for input spoofing
        /// </SecurityNote>
        private SecurityCriticalDataClass<IInputProvider> _inputProvider;
    }
}

