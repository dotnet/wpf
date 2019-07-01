// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.Conditions;


namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Contains all built-in fault injection conditions.
    /// </summary>
    /// <remarks>
    /// For more information on how to use the BuiltInConditions class, see the <see cref="FaultSession"/> class. 
    /// All fault injection conditions implement the <see cref="ICondition"/> interface.
    /// </remarks> 
    public static class BuiltInConditions
    {
        #region Public Members

        /// <summary>
        /// A built-in condition which triggers a fault every time the faulted method is called.
        /// </summary>
        public static ICondition TriggerOnEveryCall
        {
            get { return new TriggerOnEveryCall(); }
        }        
        
        /// <summary>
        /// A built-in condition which triggers a fault if the faulted method is called by a specified method.</summary>
        /// <param name="caller">A string in the format:
        /// System.Console.WriteLine(string),
        /// Namespace&lt;T&gt;.OuterClass&lt;E&gt;.InnerClass&lt;F,G&gt;.MethodName&lt;H&gt;(T, E, F, H, List&lt;H&gt;).
        /// </param> 
        public static ICondition TriggerIfCalledBy(string caller)
        {
            return new TriggerIfCalledBy(caller);
        }
        
        
        /// <summary>
        /// A built-in condition which triggers a fault if the current call stack contains a specified method.
        /// </summary>
        /// <param name="method">A string in the format:
        /// System.Console.WriteLine(string),
        /// Namespace&lt;T&gt;.OuterClass&lt;E&gt;.InnerClass&lt;F,G&gt;.MethodName&lt;H&gt;(T, E, F, H, List&lt;H&gt;).
        /// </param>
        public static ICondition TriggerIfStackContains(string method)
        {
            return new TriggerIfStackContains(method);
        }
        
        
        /// <summary>
        /// A built-in condition which triggers a fault after every n times the faulted method is called.
        /// </summary>
        /// <param name="n">A positive number.</param>      
        /// <remarks>
        /// A System.Argument exception is thrown if n is not positive.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static ICondition TriggerOnEveryNthCall(int n)
        {
            return new TriggerOnEveryNthCall(n);
        }
        
        
        /// <summary>
        /// A built-in condition which triggers a fault after the first n times the faulted method is called.
        /// </summary>
        /// <param name="n">A positive number.</param>      
        /// <remarks>
        /// A System.Argument exception is thrown if n is not positive.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static ICondition TriggerOnNthCall(int n)
        {
            return new TriggerOnNthCall(n);
        }
        
        
        /// <summary>
        /// A built-in condition which triggers a fault the first time the faulted method is called.
        /// </summary>
        public static ICondition TriggerOnFirstCall
        {
            get { return new TriggerOnFirstCall(); }
        }
       
        
        /// <summary>
        /// A built-in condition which never triggers a fault. This condition can be used to turn off a fault rule.
        /// </summary>
        public static ICondition NeverTrigger
        {
            get { return new NeverTrigger(); }
        }

        /// <summary>
        /// A built-in condition which triggers a fault after the faulted method is called n times by the specified caller.
        /// </summary>
        /// <param name="n">A positive number.</param>
        /// <param name="caller">A string in the format:
        /// System.Console.WriteLine(string),
        /// Namespace&lt;T&gt;.OuterClass&lt;E&gt;.InnerClass&lt;F,G&gt;.MethodName&lt;H&gt;(T, E, F, H, List&lt;H&gt;).
        /// </param>   
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static ICondition TriggerOnNthCallBy(int n, string caller)
        {
            return new TriggerOnNthCallBy(n, caller);
        }

        #endregion
    }
}
