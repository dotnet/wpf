// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.Test.Stability.Extensions.State;

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// Definition of a Stability test.
    /// </summary>
    [Serializable]
    public class StabilityTestDefinition 
    {

        #region Constructor

        public StabilityTestDefinition(ContentPropertyBag payload)
        {
            string schedulerTypename = payload["SchedulerType"];
            SchedulerType = Type.GetType(schedulerTypename);
            if (SchedulerType == null)
            {
                throw new ArgumentException(string.Format("Requested SchedulerType '{0}' does not exist in assembly '{1}'.", schedulerTypename, Assembly.GetExecutingAssembly().FullName));
            }

            RandomSeed = int.Parse(payload["Seed"]);
            NumWorkerThreads = int.Parse(payload["NumWorkerThreads"]);

            IsolateThreadsInAppdomains = bool.Parse(payload["IsolateThreadsInAppdomains"]);

            ExecutionContext = new ExecutionContextMetadata();
            ExecutionContext.StateType = InitType(payload["StateAssembly"], payload["StateType"]);
            ExecutionContext.ActionSequencerType = InitType(payload["ActionAssembly"], payload["SequenceType"]);
            ExecutionContext.StateArguments = PopulateStateArgs(ExecutionContext.StateType, payload);

            int timeInMinutes;
            if (!int.TryParse(payload["DurationInMinutes"], out timeInMinutes))
            {
                timeInMinutes = 900;
            }
            ExecutionTime= TimeSpan.FromMinutes(timeInMinutes);

            int idleDuration;
            if (!int.TryParse(payload["IdleDuration"], out idleDuration))
            {
                idleDuration = 200;
            }
            IdleDuration = TimeSpan.FromMilliseconds(idleDuration);
        }

        //HACK: This TestDefinition/ObjectSerializer/XmlDocument business is a nightmare. We need to move to a simple dictionary based model
        //This is quite a disapointing solution, but expedient. To fix this up would be expensive, with no functional/external gain.
        //I would like to switch the pipeline to be CLD/CLP based to streamline the design however.
        private IStatePayload PopulateStateArgs(Type stateType, ContentPropertyBag bag)
        {
            IStatePayload payload = null;
            if (stateType == typeof(ConvenienceStressState))
            {
                ConvenienceStressStatePayload csp = new ConvenienceStressStatePayload();
                csp.Initialize(bag);
                payload = csp;
            }
            return payload;
        }

        private Type InitType(string stateAssemblyName, string stateTypeName)
        {
            Assembly assembly = Assembly.LoadFrom(stateAssemblyName);

            Type stateType = assembly.GetType(stateTypeName);
            if (stateType == null)
            {
                throw new ArgumentException(string.Format("Requested Type '{0}' could not be located in Assembly '{1}'.", stateTypeName, stateAssemblyName));
            }
            return stateType;
        }

        #endregion

        #region Public Members

        public Type SchedulerType { get; private set; }
        public int RandomSeed { get; private set; }
        public TimeSpan ExecutionTime { get; internal set; }
        public TimeSpan IdleDuration { get; private set; }        
        public ExecutionContextMetadata ExecutionContext { get; private set; }
        public int NumWorkerThreads { get; private set; }
        public bool IsolateThreadsInAppdomains { get; private set; }

        #endregion
    }



    /// <summary>
    /// Metadata package for ExecutionContext objects...
    /// </summary>
    [Serializable]
    public class ExecutionContextMetadata
    {
        #region Public Properties

        /// <summary/>
        public Type ActionSequencerType { get; set; }

        /// <summary/>
        public Type StateType { get; set; }

        /// <summary/>
        public IStatePayload StateArguments { get; set; }

        #endregion
    }
}
