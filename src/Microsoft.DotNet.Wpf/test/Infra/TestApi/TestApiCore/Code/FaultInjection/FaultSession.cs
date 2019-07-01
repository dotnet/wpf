// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Test.FaultInjection.Constants;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Maintains information needed for injecting faults into a test application. 
    /// For general information on fault injection see <a href="http://en.wikipedia.org/wiki/Fault_injection">this page</a>.
    /// </summary>
    /// <remarks>
    /// Users can launch the faulted application by calling GetProcessStartInfo(string) and calling Process.Start()
    /// with the returned ProcessStartInfo.
    /// </remarks>
    /// 
    /// <example>
    /// The following example creates a new FaultSession with a single FaultRule and launches the application under test.
    /// <code>
    /// string sampleAppPath = "SampleApp.exe";
    ///
    /// FaultRule rule = new FaultRule(
    ///     "SampleApp.TargetMethod(string, string)",
    ///     BuiltInConditions.TriggerOnEveryCall,
    ///     BuiltInFaults.ReturnValueFault(""));
    ///
    /// FaultSession session = new FaultSession(rule);
    /// ProcessStartInfo psi = session.GetProcessStartInfo(sampleAppPath);
    /// Process.Start(psi);
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// The following example creates a new FaultSession with multiple FaultRules and launches the application under test.
    /// <code>
    /// string sampleAppPath = "SampleApp.exe";
    ///
    /// FaultRule[] ruleArray = new FaultRule[]
    /// {
    ///     new FaultRule(
    ///         "SampleApp.TargetMethod(string, string)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ReturnValueFault("")),
    ///
    ///     new FaultRule(
    ///         "SampleApp.TargetMethod2(string, int)",
    ///         BuiltInConditions.TriggerOnEveryCall,
    ///         BuiltInFaults.ReturnValueFault(Int32.MaxValue)),
    ///
    ///     new FaultRule(
    ///         "static SampleApp.StaticTargetMethod()",
    ///         BuiltInConditions.TriggerOnEveryNthCall(2),
    ///         BuiltInFaults.ThrowExceptionFault(new InvalidOperationException()))
    /// };     
    ///
    /// FaultSession session = new FaultSession(ruleArray);
    /// ProcessStartInfo psi = session.GetProcessStartInfo(sampleAppPath);
    /// Process.Start(psi);
    /// </code>
    /// </example>
    /// 
    /// <example>The following example demonstrates how to modify a fault rule in an existing session.
    /// <code>
    /// ...
    /// string sampleAppPath = "SampleApp.exe";     
    /// FaultSession session = new FaultSession(rule);
    /// ...
    ///
    /// FaultRule foundRule = session.FindRule("SampleApp.TargetMethod(string, string)");
    /// if (foundRule != null)
    /// {
    ///     foundRule.Condition = BuiltInConditions.TriggerOnEveryNthCall(4);
    ///     session.NotifyRuleChanges();
    /// }
    /// ...
    /// </code>
    /// </example>
    public sealed class FaultSession
    {
        #region Private Data

        private Dictionary<string, FaultRule> ruleDict = new Dictionary<string, FaultRule>();
        // This cache stores binary representation of previously serialized rules
        // FaultSession use this cache to decide whether serialization version of an FaultRule
        // object should be updated while reserialized
        private Dictionary<string, byte[]> ruleCache = new Dictionary<string, byte[]>();
        private readonly string serializationFileName;
        private readonly Mutex serializationMutex;  // Shared with tester and testee process
        private readonly string methodFilterFileName;
        private string logDirectory = Directory.GetCurrentDirectory();

        #endregion  

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FaultSession class with the specified FaultRule objects.
        /// </summary>
        /// <param name="rules">FaultRule objects defining how to fault the test application.</param>
        /// <exception cref="FaultInjectionException">Two FaultRule objects corresponding to the same method.</exception>
        public FaultSession(params FaultRule[] rules)
        {            
            ComRegistrar.AutoRegister();
            
            AddRulesToDict(rules);

            serializationFileName = Path.Combine(this.logDirectory, DateTime.Now.ToString("yyyyMMddHHmmssff", CultureInfo.CurrentCulture) + ".rul"); 
            {
                string mutexName = Path.GetFileName(serializationFileName);

                // Serialization file will be shared between tester and testee process, they must be synchronized
                // by a named global Mutex
                bool newMutexCreated;
                serializationMutex = new Mutex(false, mutexName, out newMutexCreated);
                if (!newMutexCreated)
                {
                    throw new FaultInjectionException(
                        string.Format(CultureInfo.CurrentCulture, ApiErrorMessages.UnableToCreateFileReadWriteMutex, mutexName));
                }
            }

            methodFilterFileName = Path.Combine(this.logDirectory, DateTime.Now.ToString("yyyyMMddHHmmssff", CultureInfo.CurrentCulture) + ".mfi");
            MethodFilterHelper.WriteMethodFilter(methodFilterFileName, rules);
        }

        #endregion 

        #region Public Members

        /// <summary>
        /// Finds the rule corresponding to a specified method.
        /// </summary>
        /// <param name="method">The signature of the method.</param>
        /// <returns>
        /// The FaultRule instance corresponding to the method specified. Returns null if no such rule exists.
        /// </returns>
        public FaultRule FindRule(string method)
        {
            string key = Signature.ConvertSignature(method, SignatureStyle.Formal);
            if (ruleDict.ContainsKey(key))
            {
                return ruleDict[key];
            }

            return null;
        }

        /// <summary>
        /// Notifies all test applications that fault rules have changed.
        /// </summary>
        public void NotifyRuleChanges()
        {
            SerializeRules();
        }       

        /// <summary>
        /// Creates a ProcessStartInfo with the appropriate environment variables set
        /// for fault injection.
        /// </summary>
        /// <param name="file">The path to the executable to launch.</param>
        /// <returns>The ProcessStartInfo object for the executable.</returns>
        public ProcessStartInfo GetProcessStartInfo(string file)
        {
            ProcessStartInfo psi = new ProcessStartInfo(file);
            SerializeRules();
            SetProcessEnvironmentVariables(this, psi);
            psi.UseShellExecute = false;
            return psi;
        }

        /// <summary>
        /// Directory for all log files written by applications launched by this session.
        /// </summary>
        public string LogDirectory
        {
            get { return logDirectory; }
            set
            {
                if (value == null || value == string.Empty)
                {
                    throw new FaultInjectionException(ApiErrorMessages.LogDirectoryNullOrEmpty);
                }
                logDirectory = Path.GetFullPath(value);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
        }        

        #endregion        

        #region Private Members

        private void AddRulesToDict(FaultRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                throw new FaultInjectionException(ApiErrorMessages.FaultRulesNullOrEmpty);
            }
            foreach (FaultRule rule in rules)
            {
                if (ruleDict.ContainsKey(rule.FormalSignature))
                {
                    throw new FaultInjectionException(ApiErrorMessages.FaultRulesConflict);
                }
                ruleDict.Add(rule.FormalSignature, rule);
            }
        }

        private void SerializeRules()
        {
            List<FaultRule> rules = new List<FaultRule>();

            foreach (KeyValuePair<string, FaultRule> pair in ruleDict)
            {
                rules.Add(pair.Value);

                // If the rule doesn't changed since last serialization, the version need not be updated
                if (ruleCache.ContainsKey(pair.Key))
                {
                    byte[] current = Serializer.SerializeRuleToBuffer(pair.Value);
                    byte[] cached = ruleCache[pair.Key];
                    if (ArrayEquals(cached, current))
                    {
                        continue;
                    }
                }

                // Update serialization version, then save it to cache
                ++pair.Value.SerializationVersion;
                ruleCache[pair.Key] = Serializer.SerializeRuleToBuffer(pair.Value);
            }

            Serializer.SerializeRules(serializationFileName, rules.ToArray(), serializationMutex);
        }      

        #endregion 

        #region Static Members

        /// <summary>
        /// Sets a global fault session. This enables fault injection for all .NET
        /// processes launched after the creation of the global session. This functionality is often
        /// useful when testing server .NET  applications, where one application typically consists of
        /// and launches many processes.
        /// </summary>
        public static void SetGlobalFault(FaultSession session)
        {
            session.NotifyRuleChanges();

            SetEnvironmentVariable(session, EnvironmentVariableTarget.Process);
            SetEnvironmentVariable(session, EnvironmentVariableTarget.Machine);
        }

        /// <summary>
        /// Destroys the global fault session. 
        /// </summary>
        public static void ClearGlobalFault()
        {
            ClearEnvironmentVariable(EnvironmentVariableTarget.Machine);
            ClearEnvironmentVariable(EnvironmentVariableTarget.Process);
        }        

        private static void SetProcessEnvironmentVariables(FaultSession session, ProcessStartInfo processStartInfo)
        {
            // Enable Profiling Callback implemented by Engine
            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.EnableProfiling, "1");
            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.Proflier, ComRegistrar.Clsid);

            // Set method filter file name for Engine
            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.MethodFilter, session.methodFilterFileName);
            // Set serialization file name for Dispatcher
            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.RuleRepository, session.serializationFileName);
            // Set log directory for Engine and Dispatcher
            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.LogDirectory, session.LogDirectory);

            processStartInfo.EnvironmentVariables.Add(EnvironmentVariable.CLR4Compatibility, "EnableV2Profiler");
        }

        private static void SetEnvironmentVariable(FaultSession session, EnvironmentVariableTarget target)
        {
            // Enable Profiling Callback implemented by Engine
            Environment.SetEnvironmentVariable(EnvironmentVariable.EnableProfiling, "1", target);
            Environment.SetEnvironmentVariable(EnvironmentVariable.Proflier, ComRegistrar.Clsid, target);

            // Set method filter file name for Engine
            Environment.SetEnvironmentVariable(EnvironmentVariable.MethodFilter, session.methodFilterFileName, target);
            // Set serialization file name for Dispatcher
            Environment.SetEnvironmentVariable(EnvironmentVariable.RuleRepository, session.serializationFileName, target);
            // Set log directory for Engine and Dispatcher
            Environment.SetEnvironmentVariable(EnvironmentVariable.LogDirectory, session.LogDirectory, target);

            Environment.SetEnvironmentVariable(EnvironmentVariable.CLR4Compatibility, "EnableV2Profiler", target);
        }

        private static void ClearEnvironmentVariable(EnvironmentVariableTarget target)
        {
            // Disable Profiling Callback implemented by Engine
            Environment.SetEnvironmentVariable(EnvironmentVariable.EnableProfiling, "0", target);
            Environment.SetEnvironmentVariable(EnvironmentVariable.Proflier, string.Empty, target);
            // Clear method filter file name
            Environment.SetEnvironmentVariable(EnvironmentVariable.MethodFilter, string.Empty, target);
            // Clear serialization file name for Dispatcher
            Environment.SetEnvironmentVariable(EnvironmentVariable.RuleRepository, string.Empty, target);
            // Clear log directory for Engine and Dispatcher
            Environment.SetEnvironmentVariable(EnvironmentVariable.LogDirectory, string.Empty, target);

            Environment.SetEnvironmentVariable(EnvironmentVariable.CLR4Compatibility, string.Empty, target);
        }

        private static bool ArrayEquals(byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }
            for (int i = 0; i < lhs.Length; ++i)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
