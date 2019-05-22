// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    ///     Provides data for the SourceChanged event.
    /// </summary>
    public sealed class SourceChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the SourceChangedEventArgs class.
        /// </summary>
        /// <param name="oldSource">
        ///     The old source that this handler is being notified about.
        /// </param>
        /// <param name="newSource">
        ///     The new source that this handler is being notified about.
        /// </param>
        /// <SecurityNote>
        ///     Critical:This handles critical in the form of PresentationSource but there are demands on the
        ///     data
        ///     PublicOK: As this code does not expose the data.
        /// </SecurityNote>
        [SecurityCritical]
        public SourceChangedEventArgs(PresentationSource oldSource,
                                      PresentationSource newSource)
        :this(oldSource, newSource, null, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the SourceChangedEventArgs class.
        /// </summary>
        /// <param name="oldSource">
        ///     The old source that this handler is being notified about.
        /// </param>
        /// <param name="newSource">
        ///     The new source that this handler is being notified about.
        /// </param>
        /// <param name="element">
        ///     The element whose parent changed causing the source to change.
        /// </param>
        /// <param name="oldParent">
        ///     The old parent of the element whose parent changed causing the
        ///     source to change.
        /// </param>
        /// <SecurityNote>
        ///     Critical:This handles critical data in the form of PresentationSource but there are demands on the
        ///     critical data.
        ///     PublicOK:As this code does not expose any critical data.
        /// </SecurityNote>
        [SecurityCritical]
        public SourceChangedEventArgs(PresentationSource oldSource,
                                      PresentationSource newSource,
                                      IInputElement element,
                                      IInputElement oldParent)
        {
            _oldSource = new SecurityCriticalData<PresentationSource>(oldSource);
            _newSource = new SecurityCriticalData<PresentationSource>(newSource);
            _element = element;
            _oldParent = oldParent;
        }
        
        /// <summary>
        ///     The old source that this handler is being notified about.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This handles critical data in the form of PresentationSource but there are demands on the
        ///     critical data
        ///     PublicOK: There exists a demand
        /// </SecurityNote>
        public PresentationSource OldSource
        {
            [SecurityCritical]
            get 
            {
                SecurityHelper.DemandUIWindowPermission();
                return _oldSource.Value;
            }
        }

        /// <summary>
        ///     The new source that this handler is being notified about.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical: This handles critical data in the form of PresentationSource but there are demands on the
        ///     critical data
        ///     PublicOK: There exists a demand
        /// </SecurityNote>
        public PresentationSource NewSource
        {
            [SecurityCritical]
            get 
            {
                SecurityHelper.DemandUIWindowPermission();
                return _newSource.Value;
            }
        }

        /// <summary>
        ///     The element whose parent changed causing the source to change.
        /// </summary>
        public IInputElement Element
        {
            get {return _element;}
        }

        /// <summary>
        ///     The old parent of the element whose parent changed causing the
        ///     source to change.
        /// </summary>
        public IInputElement OldParent
        {
            get {return _oldParent;}
        }

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            SourceChangedEventHandler handler = (SourceChangedEventHandler) genericHandler;
            handler(genericTarget, this);
        }
        /// <SecurityNote>
        ///     Critical: This holds reference to a presentation source not safe to give out
        /// </SecurityNote>
        private SecurityCriticalData<PresentationSource> _oldSource;

        /// <SecurityNote>
        ///     Critical: This holds reference to a presentation source not safe to give out
        /// </SecurityNote>
        private SecurityCriticalData<PresentationSource> _newSource;
        private IInputElement _element;
        private IInputElement _oldParent;
    }
}
