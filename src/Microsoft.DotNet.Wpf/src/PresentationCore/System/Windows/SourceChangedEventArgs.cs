// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
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
        public PresentationSource OldSource
        {
            get 
            {
                return _oldSource.Value;
            }
        }

        /// <summary>
        ///     The new source that this handler is being notified about.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public PresentationSource NewSource
        {
            get 
            {
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
        private SecurityCriticalData<PresentationSource> _oldSource;

        private SecurityCriticalData<PresentationSource> _newSource;
        private IInputElement _element;
        private IInputElement _oldParent;
    }
}
