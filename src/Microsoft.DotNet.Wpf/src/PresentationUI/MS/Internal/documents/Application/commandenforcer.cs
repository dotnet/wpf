// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Manager for Enforcing RM Permissions on DocumentApplication-specific RoutedUICommands.

using MS.Internal.Documents;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// A PolicyBinding represents a binding between a Command and the RMPolicy required to
    /// Invoke said command.
    /// </summary>
    internal sealed class PolicyBinding
    {
        /// <summary>
        /// Constructor for a PolicyBinding.
        /// </summary>
        /// <param name="command">The Command to bind the permission to.</param>
        /// <param name="policy">The RMPolicy to be bound to the above Command.</param>
        public PolicyBinding(RoutedUICommand command, RightsManagementPolicy policy)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            _command = command;
            _policy = policy;
        }

        /// <summary>
        /// The Command associated with this binding.
        /// </summary>    
        public RoutedUICommand Command
        {
            get { return _command; }
        }

        /// <summary>
        /// The RMPolicy associated with this binding.
        /// </summary>
        public RightsManagementPolicy Policy
        {
            get { return _policy; }
        }

        private RoutedUICommand _command;
        private RightsManagementPolicy _policy;
    }

    /// <summary>
    /// The CommandEnforcer class provides a very simple central repository for RM enforced
    /// Commands used by Mongoose.
    /// </summary>
    internal sealed class CommandEnforcer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="docViewer">The DocumentApplicationDocumentViewer parent.</param>
        internal CommandEnforcer(DocumentApplicationDocumentViewer docViewer)
        {
            //Create our bindings List.
            _bindings = new List<PolicyBinding>(_initialBindCount);
            _docViewer = docViewer;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~CommandEnforcer()
        {
            // Ensure that PrintScreen is re-enabled in native code
            DisablePrintScreen(false);
        }

        /// <summary>
        /// Adds a new binding to the Enforcer.
        /// NOTE: CommandEnforcer does not check for duplicate entries, so be warned
        /// that if you have duplicate entries, the newest duplicate will be the one whose
        /// enforcement is honored.
        /// </summary>
        /// <param name="bind">The binding to add</param>
        internal void AddBinding(PolicyBinding bind)
        {
            if (bind == null)
            {
                throw new ArgumentNullException("bind");
            }

            _bindings.Add(bind);
        }

        /// <summary>
        /// Enforces the RMPolicy by walking through the PolicyBinding and setting the
        /// IsBlockedByRM property on the associated Commands as appropriate.    
        /// </summary>
        /// <param name="policyToEnforce"></param>
        internal void Enforce()
        {
            Invariant.Assert(
                _docViewer != null,
                "CommandEnforcer has no reference to the parent DocumentApplicationDocumentViewer.");

            // Get the current policy from the parent DocumentApplicationDocumentViewer
            RightsManagementPolicy policyToEnforce = _docViewer.RightsManagementPolicy;
            
            // Walk through the list of bindings
            foreach (PolicyBinding binding in _bindings)
            {
                // If the incoming policy allows this action, and it completely masks our binding policy, 
                // then we set the IsBlockedByRM property to false, as this Command is permitted.
                binding.Command.IsBlockedByRM = !((policyToEnforce & binding.Policy) == binding.Policy);
            }

            // Enforce PrintScreen
            //   This will disable PrintScreen if neither AllowCopy or AllowPrint
            //   permission is granted.
            bool disablePrintScreen = !(((policyToEnforce & RightsManagementPolicy.AllowCopy)
                                            == RightsManagementPolicy.AllowCopy) ||
                                        ((policyToEnforce & RightsManagementPolicy.AllowPrint)
                                            == RightsManagementPolicy.AllowPrint));
            DisablePrintScreen(disablePrintScreen);
        }

        /// <summary>
        /// Disable all PrintScreen related functions in the system.
        /// </summary>
        /// <param name="disable">True if PrintScreen should be disabled, false otherwise.</param>
        private void DisablePrintScreen(bool disable)
        {
            // To ensure that everytime PrintScreen is disabled it is also re-enabled we
            // only issue the system call if the value has changed.  
            if (disable != _printScreenDisabled)
            {
                // Check if currently running in 32bit and that RM is available.
                if ((!_isRMMissing) && (IntPtr.Size == 4))
                {
                    // Initialize hresult to a valid return value.
                    int hresult = UnsafeNativeMethods.FAIL;

                    try
                    {
                        // Make call to RM API here to change the count.
                        hresult = UnsafeNativeMethods.DRMRegisterContent(disable);
                    }
                    catch (System.DllNotFoundException)
                    {
                        // We could not find MSDRM.DLL to disable the print screen.
                        // Just silently catch this exception and fail.
                        // This is safe to catch, because it means that RM is not 
                        // installed on the machine so we don't need to worry about
                        // disabling the print screen.
                        _isRMMissing = true;
                    }

                    // Result should be S_OK for successful value assignment.
                    Trace.SafeWrite(
                        Trace.Presentation,
                        "CommandEnforcer: DisablePrintScreen change to: {0} hresult: {1}",
                        disable,
                        hresult);
                }

                // Update property with new value
                _printScreenDisabled = disable;

            }
        }

        /// <summary>
        /// The list of bindings to enforce.
        /// </summary>
        private List<PolicyBinding> _bindings;

        private DocumentApplicationDocumentViewer _docViewer;
        private const int _initialBindCount = 10; //We allocate space for 10 bindings initially.
        /// <summary>
        /// _printScreenDisabled is used to track the current PrintScreen state.  It begins with
        /// false since PrintScreen is initially enabled, and tracks changes from there.
        /// </summary>
        private bool _printScreenDisabled; // Trusted to be initialized to false
        /// <summary>
        /// _isRMMissing is used to track if a successful call has been made to native RM.
        /// This is used for performance, to limit the number of Exceptions being caught.
        /// </summary>
        private bool _isRMMissing; // Initially assumed that RM is installed and working.

        #region UnsafeNativeMethods (Private Class)
        /// <summary>
        /// This class is used to provide access to unmanaged code in order to disable
        /// the native PrintScreen support.
        /// </summary>
        private static class UnsafeNativeMethods
        {
            /// <summary>
            /// Called to toggle native PrintScreen support.  This will increment/decrement
            /// a reference counter, so it must be reset after use (for stability).
            /// </summary>
            /// <param name="register">A bool value to determine if PrintScreen should be
            /// disabled.  True means disabled, false re-enables.</param>
            /// <returns>S_OK on success, an error code otherwise.</returns>
            [DllImport("msdrm.dll", SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int DRMRegisterContent(bool register);

            /// <summary>
            /// Non-success value.
            /// </summary>
            //  This value can be anything other than S_OK
            internal const int FAIL = -1;
        }
        #endregion UnsafeNativeMethods
    }
}
