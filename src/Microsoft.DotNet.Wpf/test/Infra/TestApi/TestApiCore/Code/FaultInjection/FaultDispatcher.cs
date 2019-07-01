// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.IO;
using Microsoft.Test.FaultInjection.Constants;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Calls the appropriate fault for a given faulted method.
    /// </summary>
    /// <remarks>
    /// The FaultDispatcher class contains a static method <see cref="Trap"/>, which is called by the MSIL code, 
    /// injected in the faulted method, in order to dispatch to the specific fault.
    /// </remarks>
    public static class FaultDispatcher
    {
        #region Private Data

        static RuntimeContext[] contexts = null;
        static object initialized = new Object();

        #endregion

        #region Public Members

        /// <summary>
        /// Injected into the prologue of the target method.
        /// </summary>
        /// <param name="exceptionValue">Exception thrown by fault</param>
        /// <param name="returnValue">Value to return from fault</param>
        /// <returns></returns>
        /// <remarks>
        /// Trap creates a RuntimeContext for the current call and evaluates
        /// the fault condition's Trigger method.  If it evaluates to true
        /// the fault's Retrieve method is called.
        /// </remarks>
        public static bool Trap(out Exception exceptionValue, out Object returnValue)
        {
            StackTrace stackTrace = new StackTrace(1);
            CallStack callStack = new CallStack(stackTrace);
            //stackFrame does not include Trap()
            StackFrame stackFrame = stackTrace.GetFrame(0);
            String currentFunction = callStack[0];

            exceptionValue = null;
            returnValue = null;
            FaultRule[] newRules;


            if (GetNewRulesAndPrepareContext(out newRules, currentFunction) == false)
            {
                return false;
            }

            RuntimeContext currentContext = null;
            FaultRule rule = null;


            try
            {
                //Get Fault Rule and Update current RuntimeContext
                int len = contexts.Length;
                for (int i = 0; i < len; ++i)
                {
                    if (newRules[i].FormalSignature != null &&
                        currentFunction.Equals(newRules[i].FormalSignature) == true)
                    {
                        rule = newRules[i];
                        currentContext = new RuntimeContext();
                        lock (contexts)
                        {
                            contexts[i].CalledTimes++;
                            currentContext.CalledTimes = contexts[i].CalledTimes;
                        }
                        currentContext.CallStack = callStack;
                        currentContext.CallStackTrace = stackTrace;

                        break;
                    }
                }

                //Using ICondition and IFault
                if (rule == null)
                {
                    return false;
                }
                bool triggered = false;
                try
                {
                    lock (contexts)
                    {
                        triggered = rule.Condition.Trigger(currentContext);
                        if (triggered == true)
                        {
                            rule.Fault.Retrieve(currentContext, out (exceptionValue), out returnValue);
                        }
                    }

                }
                catch (System.Exception e)
                {
                    throw new FaultInjectionException(FaultDispatcherMessages.NoExceptionAllowedInTriggerAndRetrieve, e);
                }
                if (!triggered)
                {
                    return false;
                }
            }
            catch (System.Exception e)
            {
                throw new FaultInjectionException(FaultDispatcherMessages.UnknownExceptionInTrap, e);
            }


            // check return-value's type if not to throw exception
            if (null == exceptionValue)
            {
                if (stackFrame.GetMethod() is ConstructorInfo)
                {
                    return true;
                }
                Type returnTypeOfTrappedMethod = ((MethodInfo)stackFrame.GetMethod()).ReturnType;
                return CheckReturnType(returnTypeOfTrappedMethod, returnValue, currentFunction);
            }
            return true;

        }

        #endregion

        #region Private Members

        private static bool GetNewRulesAndPrepareContext(out FaultRule[] newRules, string currentFunction)
        {
            try
            {
                newRules = FaultRuleLoader.Load();
            }
            catch (FaultInjectionException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new FaultInjectionException(FaultDispatcherMessages.LoadFaultRuleError, e);
            }
            if (newRules == null || newRules.Length == 0)
            {
                return false;
            }

            lock(initialized)
            {       
                if (contexts == null)
                {
                    int length = newRules.Length;
                    contexts = new RuntimeContext[length];
                    for (int i = 0; i < length; ++i)
                    {
                        contexts[i] = new RuntimeContext();
                    }
                }
            }                     

            return true;
        }

        private static bool CheckReturnType(Type returnTypeOfTrappedMethod, Object returnValue, String currentFunction)
        {
            //
            // If the return type is <T> or List<T>, we simply omit it.
            // 
            if (returnTypeOfTrappedMethod.IsGenericParameter || returnTypeOfTrappedMethod.ContainsGenericParameters)
            {
                return true;
            }

            if (returnTypeOfTrappedMethod != typeof(void))
            {
                if (returnValue == null && returnTypeOfTrappedMethod.IsValueType == true)
                {                    
                    throw new FaultInjectionException(string.Format(CultureInfo.InvariantCulture.NumberFormat, FaultDispatcherMessages.ReturnValueTypeNullError, MethodSignatureTranslator.GetTypeString(returnTypeOfTrappedMethod)));
                }
                else if (returnValue != null &&
                    returnTypeOfTrappedMethod.IsInstanceOfType(returnValue) == false)
                {                    
                    throw new FaultInjectionException(string.Format(CultureInfo.InvariantCulture.NumberFormat, FaultDispatcherMessages.ReturnTypeMismatchError, MethodSignatureTranslator.GetTypeString(returnTypeOfTrappedMethod), MethodSignatureTranslator.GetTypeString(returnValue.GetType())));
                }
            }
            return true;
        }

        #endregion
    }
}
