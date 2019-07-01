// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Extracts frame information from a StackTrace object.
    /// </summary>
    /// <remarks>
    /// Calling CallStack[n] (where n is zero-indexed) will return a C#-style method signature for the nth frame.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711")]
    public class CallStack
    {
        #region Private Data

        private StackTrace stackTrace;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the CallStack class.
        /// </summary>
        /// <param name="stackTrace"> A stack trace from which to create the CallStack.</param>
        public CallStack(StackTrace stackTrace)
        {
            this.stackTrace = stackTrace;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Number of Frames in the CallStack.
        /// </summary>
        public int FrameCount
        {
            get
            {
                return stackTrace.FrameCount;
            }
        }        

        /// <summary>
        /// C#-style method signature for the specified frame.
        /// </summary>
        /// <param name="index">frame to evaluate</param>
        public String this[int index]
        {
            get
            {
                return GetCallStackFunction(index);
            }
        }

        #endregion

        #region Private Members

        private String GetCallStackFunction(int index)
        {
            StackFrame stackFrame = stackTrace.GetFrame(index);
            
            if (stackFrame == null)
            {
                return null;
            }
            MethodBase mb = stackFrame.GetMethod();
            ParameterInfo[] paras = mb.GetParameters();
            System.String callingFunc = stackFrame.GetMethod().ToString();
            
            string signatureBeforeFunName = MethodSignatureTranslator.GetTypeString(mb.DeclaringType);

            System.String[] temps = callingFunc.Split('(');
            if (temps.Length < 2)
            {
                //error handling
            }
            temps = temps[0].Split(' ');
            if (temps.Length < 2)
            {
                //error handling
            }
            temps[0] = temps[1];
            
            temps[0] = temps[0].Replace('[', '<');
            temps[0] = temps[0].Replace(']', '>');
            temps[0] = temps[0].Insert(0, signatureBeforeFunName + ".");
            temps[0] = temps[0].Insert(temps[0].Length, "(");

            foreach (ParameterInfo p in paras)
            {
                String typeString = MethodSignatureTranslator.GetTypeString(p.ParameterType);
                
                temps[0] = temps[0].Insert(temps[0].Length, typeString);
                temps[0] = temps[0].Insert(temps[0].Length, ",");
            }
            if (paras != null && paras.Length > 0)
            {
                temps[0] = temps[0].Remove(temps[0].Length - 1);
            }
            temps[0] = temps[0].Insert(temps[0].Length, ")");
            callingFunc = temps[0];
            return callingFunc;
        }

        #endregion
    }
}