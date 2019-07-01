// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Globalization;
using Microsoft.Test.FaultInjection.Constants;

namespace Microsoft.Test.FaultInjection
{
    internal static class FaultRuleLoader
    {
        #region Private Data

        private static Mutex fileReadWriteMutex;  // Shared with tester and testee process
        private static FaultRule[] currentRules;
        private static object initializeLock = new object();
        private static object accessCurrentRuleLock = new object();

        #endregion  

        #region Public Members

        public static FaultRule[] Load()
        {
            string serializationFileName = Environment.GetEnvironmentVariable(EnvironmentVariable.RuleRepository);
            if (serializationFileName == null)
            {
                throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, ApiErrorMessages.UnableToFindEnvironmentVariable, EnvironmentVariable.RuleRepository));
            }

            lock (initializeLock)
            {
                if (fileReadWriteMutex == null)
                {
                    fileReadWriteMutex = new Mutex(false, Path.GetFileName(serializationFileName));
                    currentRules = Serializer.DeserializeRules(serializationFileName, fileReadWriteMutex);

                    return currentRules;
                }
            }

            FaultRule[] swapBuffer;
            lock (accessCurrentRuleLock)
            {
                swapBuffer = new FaultRule[currentRules.Length];
                currentRules.CopyTo(swapBuffer, 0);
            }

            FaultRule[] loadedRules = Serializer.DeserializeRules(serializationFileName, fileReadWriteMutex);
            MergeRuleArray(swapBuffer, loadedRules);
            lock (accessCurrentRuleLock)
            {
                currentRules = swapBuffer;
            }

            return currentRules;
        }        

        #endregion  

        #region Private Members

        private static void MergeRuleArray(FaultRule[] current, FaultRule[] loaded)
        {
            foreach (FaultRule loadedRule in loaded)
            {
                int i = Array.FindIndex(
                    current,
                    delegate(FaultRule ithRule) { return ithRule.FormalSignature == loadedRule.FormalSignature; }
                );
                if (i != -1 && current[i].SerializationVersion < loadedRule.SerializationVersion)
                {
                    current[i] = loadedRule;
                }
            }
        }

        #endregion
    }
}
