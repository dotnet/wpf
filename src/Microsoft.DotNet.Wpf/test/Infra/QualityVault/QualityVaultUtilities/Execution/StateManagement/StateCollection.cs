// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using System.Xml;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary/>
    public class StateCollection : Collection<StateModule>
    {
        internal static StateCollection LoadStateCollection(string deployment, DirectoryInfo testBinariesDirectory)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(testBinariesDirectory.FullName, deployment));
            StateCollection stateCollection = null;
            using (XmlTextReader textReader = new XmlTextReader(fileInfo.OpenRead()))
            {
                stateCollection = (StateCollection)ObjectSerializer.Deserialize(textReader, typeof(StateCollection), null);
            }
            foreach (StateModule module in stateCollection)
            {
                module.TestBinariesDirectory = testBinariesDirectory;
            }
            return stateCollection;
        }

        internal void Push(StatePool statePool)
        {
            foreach (StateModule stateModule in this)
            {
                StateManagementEngine.PushStateOnPool(statePool, stateModule);                               
            }
        }

        internal static void ApplyDeployments(Collection<string> deployments, DirectoryInfo testBinariesDirectory)
        {
            if (deployments != null)
            {
                foreach (string deployment in deployments)
                {
                    ExecutionEventLog.RecordStatus("Applying Deployment: " + deployment); 
                    StateCollection stateCollection = LoadStateCollection(deployment, testBinariesDirectory);
                    stateCollection.Push(StatePool.Execution);
                }
            }
        }
    }    
}