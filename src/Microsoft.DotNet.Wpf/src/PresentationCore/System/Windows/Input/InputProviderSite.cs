// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
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
        internal InputProviderSite(InputManager inputManager, IInputProvider inputProvider)
        {
            _inputManager = new SecurityCriticalDataClass<InputManager>(inputManager);
            _inputProvider = new SecurityCriticalDataClass<IInputProvider>(inputProvider);
        }

        /// <summary>
        ///     Returns the input manager that this site is attached to.
        /// </summary>
        public InputManager InputManager
        {
            get
            {
                return CriticalInputManager;
            }
        }

        /// <summary>
        ///     Returns the input manager that this site is attached to.
        /// </summary>
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
        private SecurityCriticalDataClass<InputManager> _inputManager;
        private SecurityCriticalDataClass<IInputProvider> _inputProvider;
    }
}

