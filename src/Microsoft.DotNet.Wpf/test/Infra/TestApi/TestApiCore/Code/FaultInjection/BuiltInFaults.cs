// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.Faults;

namespace Microsoft.Test.FaultInjection
{
    #region Public Members

    /// <summary>
    /// Contains all built-in faults. 
    /// </summary>
    /// <remarks>
    /// For more information on how to use the BuiltInFaults class, see the <see cref="FaultSession"/> class. 
    /// All fault injection faults implement the <see cref="IFault"/> interface.
    /// </remarks>
    public static class BuiltInFaults
    {   
        /// <summary>
        /// A built-in fault which returns when triggered.
        /// </summary>
        /// <remarks>
        /// This method can be called when the faulted method has a void return type;
        /// it will return null if triggered in a non-void method.
        /// </remarks>
        public static IFault ReturnFault()
        {
            return new ReturnFault();
        }

        /// <summary>
        /// A built-in fault which returns the specified object when triggered.
        /// </summary>
        /// <param name="returnValue">The object to return. The faulted method will return this object when the fault condition is triggered.</param>
        public static IFault ReturnValueFault(object returnValue)
        {
            return new ReturnValueFault(returnValue);
        }

        /// <summary>
        /// A built-in fault which returns an object constructed according to the specified expression when triggered.
        /// </summary>
        /// <param name="returnValueExpression">A string in the format:
        /// (int)3, (double)6.6, (bool)true, �Hello World� which means "Hello World",
        /// System.Exception(�This is a fault�).
        /// </param>
        
        public static IFault ReturnValueRuntimeFault(string returnValueExpression)
        {
            return new ReturnValueRuntimeFault(returnValueExpression);
        }

        /// <summary>
        /// A built-in fault which throws the specified exception object when triggered.
        /// </summary>
        /// <param name="exceptionValue">
        /// An Exception object constructed by the process that injects the fault.
        /// </param>
        /// <remarks>
        /// The exception object must be serializable.
        /// </remarks>
        public static IFault ThrowExceptionFault(Exception exceptionValue)
        {
            return new ThrowExceptionFault(exceptionValue);
        }

        /// <summary>
        /// A built-in fault which throws an exception object constructed according to the specified expression when triggered.
        /// </summary>
        /// <param name="exceptionExpression">A string in the format:
        /// System.Exception(�This is a fault�),
        /// CustomizedNameSpace.CustomizedException(�Error Message�, (int)3, System.Exception(�innerException�)).
        /// </param>
        public static IFault ThrowExceptionRuntimeFault(string exceptionExpression)
        {
            return new ThrowExceptionRuntimeFault(exceptionExpression);
        }

    #endregion
    }
}
