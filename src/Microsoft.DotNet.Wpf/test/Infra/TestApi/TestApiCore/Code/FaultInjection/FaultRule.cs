// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.Conditions;
using Microsoft.Test.FaultInjection.Faults;
using Microsoft.Test.FaultInjection.Constants;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Defines which method to fault, under what conditions the fault will occur, 
    /// and how the method will fail. See also the <see cref="FaultSession"/> class.
    /// </summary>
    /// <remarks>
    /// There can only be one fault rule per target method.
    /// </remarks> 
    /// 
    /// <example>
    /// The following example creates a new FaultSession with several different 
    /// FaultRules and launches the application.
    /// <code>
    /// string sampleAppPath = "SampleApp.exe";
    ///
    /// FaultRule[] ruleArray = new FaultRule[]
    /// {     
    ///     // Instance method
    ///     new FaultRule(
    ///         "SampleApp.TargetType.TargetMethod(string, string)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ReturnValueFault("")),
    ///
    ///     // Constructor
    ///     new FaultRule(
    ///         "SampleApp.TargetType.TargetType(string, int)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ThrowExceptionFault(new InvalidOperationException())),
    ///
    ///     // Static method
    ///     new FaultRule(
    ///         "static SampleApp.TargetType.StaticTargetMethod()",
    ///         BuiltInConditions.TriggerOnEveryNthCall(2),
    ///         BuiltInFaults.ThrowExceptionFault(new InvalidOperationException())),
    ///
    ///     // Generic method
    ///     new FaultRule(
    ///         "SampleApp.TargetType.GenericTargetMethod&lt;T&gt;(string)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ReturnFault()),
    ///
    ///     // Property 
    ///     new FaultRule(
    ///         "SampleApp.TargetType.get_TargetProperty()",
    ///         BuiltInConditions.TriggerOnEveryNthCall(3),
    ///         BuiltInFaults.ReturnFault()),
    ///
    ///     // Operator overload
    ///     new FaultRule(
    ///         "SampleApp.TargetType.op_Increment(int, int)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ThrowExceptionFault(new InvalidOperationException()))
    /// };     
    ///
    /// FaultSession session = new FaultSession(ruleArray);
    /// ProcessStartInfo psi = session.GetProcessStartInfo(sampleAppPath);
    /// Process.Start(psi);
    /// </code>
    /// </example>
    [Serializable()]
    public sealed class FaultRule
    {
        #region Private Data

        private string method = null;
        private string formalSignature = null;
        private ICondition condition = new NeverTrigger();
        private IFault fault = new ReturnFault();
        private int serializationVersion = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of FaultRule class with the specified method.
        /// </summary>
        /// <param name="method">The signature of the method where the fault should be injected.</param>
        public FaultRule(string method)
        {
            this.formalSignature = Signature.ConvertSignature(method, SignatureStyle.Formal);
            this.method = method;
        }

        /// <summary>
        /// Initializes a new instance of FaultRule class with the specified method, condition and fault.
        /// </summary>
        /// <param name="method">The signature of the method where the fault should be injected.</param>
        /// <param name="condition">The condition that defines when the fault should occur.</param>
        /// <param name="fault">The fault to be injected.</param>
        public FaultRule(string method, ICondition condition, IFault fault)
            : this(method)
        {
            this.Condition = condition;
            this.Fault = fault;
        }

        #endregion  // Constructors

        #region Public Members

        /// <summary>
        /// The signature of the method where the fault should be injected.
        /// </summary>
        public string MethodSignature
        {
            get { return method; }
        }        

        /// <summary>
        /// The condition that defines when the fault should occur.
        /// </summary>
        public ICondition Condition
        {
            get { return condition; }
            set
            {
                if (value == null)
                {
                    throw new FaultInjectionException(ApiErrorMessages.ConditionNull);
                }
                condition = value;
            }
        }

        /// <summary>
        /// The fault to be injected.
        /// </summary>
        public IFault Fault
        {
            get { return fault; }
            set
            {
                if (value == null)
                {
                    throw new FaultInjectionException(ApiErrorMessages.FaultNull);
                }
                fault = value;
            }
        }        

        #endregion

        #region Internal Members

        // This is the formal signature which is more strict. Users won't see this but internally, we use
        // this to specify method .
        internal string FormalSignature
        {
            get { return formalSignature; }
        }

        // This property records "version" of this rule from viewpoint of serialization.
        // Each time the rule is serialized after some changes, the version number increased.
        // We need this when we load rule by deserialization.
        internal int SerializationVersion
        {
            get { return serializationVersion; }
            set { serializationVersion = value; }
        }

        #endregion
    }
}
