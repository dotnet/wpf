// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// State pool Enum allows user to select the appropriate state pool for manipulation.     
    /// </summary>
    public enum StatePool
    {
        /// <summary>
        /// Lab State - This slot is reserved for lab specific state which goes beyond run lifetime to enable setup tests.
        /// Not currently implemented, but defined to establish the 0th element.
        /// </summary>
        Lab = 0,
        
        /// <summary>
        /// Run Execution state stack manages state on per a Infrastructure execution basis
        /// </summary>
        Run = 1,  

        // Reserved 2 - We may add an execution group which allows sharing of some, but not all settings
        
        /// <summary>
        /// Execution State Stack is used to manage state on per driver invocation basis
        /// </summary>
        Execution = 3,

        /// <summary>
        /// Last element 
        /// </summary>
        Last = 3
        

    }
}