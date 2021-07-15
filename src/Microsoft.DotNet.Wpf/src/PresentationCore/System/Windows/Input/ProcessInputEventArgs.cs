// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security; 
using MS.Internal;
using MS.Internal.PresentationCore;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input 
{
    /// <summary>
    ///     Provides access to the input manager's staging area. 
    /// </summary>
    /// <remarks>
    ///     An instance of this class, or a derived class, is passed to the
    ///     handlers of the following events:
    ///     <list>
    ///         <item>
    ///             <see cref="InputManager.PreProcessInput"/>
    ///         </item>
    ///         <item>
    ///             <see cref="InputManager.PostProcessInput"/>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class ProcessInputEventArgs : NotifyInputEventArgs
    {
        // Only we can make these.  Note that we cache and resuse instances.
        internal ProcessInputEventArgs() {}
        
        internal override void Reset(StagingAreaInputItem input, InputManager inputManager)
        {
            _allowAccessToStagingArea = true;
            base.Reset(input, inputManager);
        }

        /// <summary>
        ///     Pushes an input event onto the top of the staging area.
        /// </summary>
        /// <param name="input">
        ///     The input event to place on the staging area.  This may not
        ///     be null, and may not already exist in the staging area.
        /// </param>
        /// <param name="promote">
        ///     An existing staging area item to promote the state from.
        /// </param>
        /// <returns>
        ///     The staging area input item that wraps the specified input.
        /// </returns>
        public StagingAreaInputItem PushInput(InputEventArgs input, 
                                              StagingAreaInputItem promote) // Note: this should be a bool, and always use the InputItem available on these args.
        {
            if(!_allowAccessToStagingArea)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotAllowedToAccessStagingArea));
            }
            
            return this.UnsecureInputManager.PushInput(input, promote);
        }

        /// <summary>
        ///     Pushes an input event onto the top of the staging area.
        /// </summary>
        /// <param name="input">
        ///     The input event to place on the staging area.  This may not
        ///     be null, and may not already exist in the staging area.
        /// </param>
        /// <returns>
        ///     The specified staging area input item.
        /// </returns>      
        public StagingAreaInputItem PushInput(StagingAreaInputItem input)
        {
            if(!_allowAccessToStagingArea)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotAllowedToAccessStagingArea));
            }
            
            return this.UnsecureInputManager.PushInput(input);
        }

        /// <summary>
        ///     Pops off the input event on the top of the staging area.
        /// </summary>
        /// <returns>
        ///     The input event that was on the top of the staging area.
        ///     This can be null, if the staging area was empty.
        /// </returns>    
        public StagingAreaInputItem PopInput()
        {
            
            if(!_allowAccessToStagingArea)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotAllowedToAccessStagingArea));
            }
            
            return this.UnsecureInputManager.PopInput();
        }

        /// <summary>
        ///     Returns the input event on the top of the staging area.
        /// </summary>
        /// <returns>
        ///     The input event that is on the top of the staging area.
        ///     This can be null, if the staging area is empty.
        /// </returns>
        public StagingAreaInputItem PeekInput()
        {

            if(!_allowAccessToStagingArea)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotAllowedToAccessStagingArea));
            }
            
            return this.UnsecureInputManager.PeekInput();
        }

        private bool _allowAccessToStagingArea;
}

    /// <summary>
    ///     Delegate type for handles of events that use
    ///     <see cref="ProcessInputEventArgs"/>.
    /// </summary>
    public delegate void ProcessInputEventHandler(object sender, ProcessInputEventArgs e);
}


