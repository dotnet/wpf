// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.LeakDetection;
using System;
using System.Diagnostics;
using Microsoft.Test.Logging;
using Microsoft.Test.CommandLineParsing;
using System.IO;

namespace Microsoft.Test.Leak
{
    public abstract class LeakTest
    {
        public void Run(int detectionCycles, int actionsPerCycle)
        {   
            //TODO1: Launch memorysnapshot monitor as a seperate process so as not to incur extra load.

            Type type = this.GetType();
            LeakTest leakTest = (LeakTest)Activator.CreateInstance(type);

            // Create new memory snapshot collection and get a handle to the process.
            MemorySnapshotCollection collection = new MemorySnapshotCollection();
            Process process = Process.GetCurrentProcess();

            //TODO2: Add GC cleanup / get ready logic.      

            leakTest.Initialize();
            collection.Add(MemorySnapshot.FromProcess(process.Id));

            // Rinse and repeat the following as requested by the user.
            for (int i = 0; i < detectionCycles; i++)
            {
                for (int j = 0; j < actionsPerCycle; j++)
                {
                    // Perform and undo action followed by a snapshot.
                    leakTest.PerformAction();
                    leakTest.UndoAction();
                }
                collection.Add(MemorySnapshot.FromProcess(process.Id));
            }

            // Log collection to file.
            string filePath = Path.Combine(Environment.CurrentDirectory, @"snapshots.xml");
            collection.ToFile(filePath);
            TestLog log = new TestLog("LeakLog");
            log.LogFile(filePath);
            log.Close();
        }

        public abstract void Initialize();

        public abstract void PerformAction();

        public abstract void UndoAction();        
    }
}