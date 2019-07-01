// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Stores information about a faulted method.
    /// </summary>
    public class RuntimeContext : IRuntimeContext
    {
        #region Private Data

        private int calledTimes = 0;
        StackTrace callStackTrace = null;
        private CallStack callStack = new CallStack(new StackTrace());

        #endregion

        #region Contructors
        /// <summary>
        /// Initializes a new instance of the RuntimeContext class.
        /// </summary>
        public RuntimeContext()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// The number of times the method has been called.
        /// </summary>
        public int CalledTimes
        {
            get
            {
                int times = calledTimes;
                return times;
            }
            set
            {
                calledTimes = value;
                
            }
        }

        /// <summary>
        /// The method's stack trace.
        /// </summary>
        public StackTrace CallStackTrace
        {
            get
            {
                return callStackTrace;
            }
            set
            {
                callStackTrace = value;          
            }
        }

        /// <summary>
        /// An array of C#-style method signatures for each method on the call stack.
        /// </summary>
        public CallStack CallStack
        {
            get
            {
                return callStack;
            }
            set
            {              
                callStack = value;              
            }
        }

        /// <summary>
        /// The C#-style method signature of the caller of the faulted method.
        /// </summary>
        public string Caller
        {
            get
            {
                if (callStack != null)
                {
                    return callStack[1];
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion    
    }
}
