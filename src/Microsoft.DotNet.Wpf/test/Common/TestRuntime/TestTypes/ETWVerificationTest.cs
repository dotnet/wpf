// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.EventTracing;
using Microsoft.Test.Logging;
using Microsoft.Test.Threading;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Validates that whatever occurs during the Run part of the StepsTest emitted (or didn't) the ETW events specified in the provided XML
    /// See src\wpf\Test\Common\Code\Microsoft\Test\TestTypes\ETWVerification_AllWPFEvents.xml for every possible Crimson/Classic event,
    /// then copy it to your own XML file
    /// Note that if you don't include a Run step, nothing happens.
    /// </summary>
    public class EtwEventExistenceTest : StepsTest
    {
        #region Private Members

        private List<ETWEventIdentifier> expectedEvents;
        private ETWEventEvaluator evaluator;
        private string expectedEventsXmlPath;

        #endregion

        #region Public Methods

        public EtwEventExistenceTest(string expectedEventsXml)
        {
            this.expectedEventsXmlPath = expectedEventsXml;
            this.InitializeSteps += new TestStep(etwEventExistenceTest_Initialize);
            this.CleanUpSteps += new TestStep(etwEventExistenceTest_CleanUpSteps);
        }

        public TestResult VerifyExpectedEvents()
        {
            DispatcherHelper.DoEvents();
            return evaluator.EndMonitoring();
        }

        #endregion

        #region Private Methods

        TestResult etwEventExistenceTest_CleanUpSteps()
        {
            if (evaluator != null)
            {
                evaluator.Stop();
            }
            return TestResult.Pass;
        }

        TestResult etwEventExistenceTest_Initialize()
        {
            expectedEvents = ReadIdentifiersFromXML(expectedEventsXmlPath);

            if (expectedEvents.Count == 0)
            {
                throw new Exception("Did not find any events to check for in " + expectedEventsXmlPath);
            }
            GlobalLog.LogEvidence("Found " + expectedEvents.Count + " events to check: ");
            foreach (ETWEventIdentifier identifier in expectedEvents)
            {
                GlobalLog.LogEvidence("--> " + identifier.ToString());
            }
            evaluator = new ETWEventEvaluator();
            evaluator.ExpectedEvents = expectedEvents;
            evaluator.BeginMonitoring();

            return TestResult.Pass;
        }



        private List<ETWEventIdentifier> ReadIdentifiersFromXML(string path)
        {
            List<ETWEventIdentifier> eventsToCheck = new List<ETWEventIdentifier>();
            XmlReader reader = XmlReader.Create(new FileStream(expectedEventsXmlPath, FileMode.Open), null);
            while (reader.Read())
            {
                if (reader.Name.ToLowerInvariant() == "etweventidentifier")
                {
                    string guid = reader.GetAttribute("TaskGuid");
                    string friendlyName = reader.GetAttribute("FriendlyName");
                    int opCode = int.Parse(reader.GetAttribute("OpCode"));
                    int eventId = int.Parse(reader.GetAttribute("EventId"));
                    eventsToCheck.Add(new ETWEventIdentifier(guid, opCode, eventId, friendlyName));
                }
            }
            return eventsToCheck;
        }

        #endregion

        // Private support classes
        private class ETWEventIdentifier
        {
            public Guid TaskGuid = Guid.Empty;
            public int OpCode = -1;
            public int EventId = -1;
            public string FriendlyName;

            public ETWEventIdentifier(string guid, int opCode, int eventId) : this(new Guid(guid), opCode, eventId, "unspecified") { }
            public ETWEventIdentifier(Guid guid, int opCode, int eventId) : this(guid, opCode, eventId, "unspecified") { }
            public ETWEventIdentifier(string guid, int opCode, int eventId, string friendlyName) : this(new Guid(guid), opCode, eventId, friendlyName) { }
            public ETWEventIdentifier(Guid guid, int opCode, int eventId, string friendlyName)
            {
                this.TaskGuid = guid;
                this.OpCode = opCode;
                this.EventId = eventId;
                this.FriendlyName = friendlyName;
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() != typeof(ETWEventIdentifier))
                {
                    return false;
                }

                return (((this.TaskGuid == ((ETWEventIdentifier)obj).TaskGuid) && (this.OpCode == ((ETWEventIdentifier)obj).OpCode)) // Pre-Crimson match... based on GUID + OpCode
                    || ((this.EventId == ((ETWEventIdentifier)obj).EventId) && (this.OpCode == ((ETWEventIdentifier)obj).OpCode)));   // Crimson match : Event IDs are unique.  Opcode included gratuitously.
            }

            public override string ToString()
            {
                if (this.FriendlyName != "unspecified")
                {
                    return ("[" + this.FriendlyName + "] Crimson Event: " + this.EventId + ".  Classic Event = " + this.TaskGuid + " Opcode: " + this.OpCode);
                }
                else
                {
                    return ("Crimson Event: " + this.EventId + ".  Classic Event = " + this.TaskGuid + " Opcode: " + this.OpCode);
                }
            }

            public override int GetHashCode()
            {
                return (this.EventId.GetHashCode() + this.OpCode.GetHashCode());
            }
        }

        private class ETWEventEvaluator
        {
            TraceEventSession session = null;
            ETWTraceEventSource eventSource = null;
            string sessionFriendlyName = "EventExistenceTraceSession";

            #region Public Members
            public List<ETWEventIdentifier> ExpectedEvents;
            public List<ETWEventIdentifier> DetectedEvents;

            public ETWEventEvaluator()
            {
                this.ExpectedEvents = new List<ETWEventIdentifier>();
                this.DetectedEvents = new List<ETWEventIdentifier>();
            }

            public void BeginMonitoring()
            {
                // Setting this to 0 means we'll see Crimson Events on Vista + and "Classic" on pre-Vista.  
                // Testing both ensures the events consistently fire and the tests can pass.
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\avalon.graphics", "ClassicETW", 0);

                try
                {
                    if (TraceEventSession.GetActiveSessionNames().Contains(sessionFriendlyName))
                    {
                        TraceEventSession.StopUserAndKernelSession(sessionFriendlyName);
                    }
                    session = new TraceEventSession(sessionFriendlyName, null);

                    // Always enable Crimson events for both 3.X and 4.0 runs, 
                    // since shared components (such as PresentationHost.exe) use Crimson on 3.5 SP1 w/ GDR patches
                    GlobalLog.LogDebug("Using WPF Crimson Trace Provider GUID: " + WPFTraceEventParser.WPFCrimsonProviderGuid);
                    session.EnableProvider(WPFTraceEventParser.WPFCrimsonProviderGuid, TraceEventLevel.Verbose, (ulong)0xFFFF);
#if TESTBUILD_CLR20
                    GlobalLog.LogDebug("Using WPF Classic Trace Provider GUID: " + WPFTraceEventParser.WPFClassicProviderGuid);
                    session.EnableProvider(WPFTraceEventParser.WPFClassicProviderGuid, TraceEventLevel.Verbose, (ulong)0xFFFF);
#endif
                    eventSource = new ETWTraceEventSource(sessionFriendlyName, TraceEventSourceType.Session);
                    eventSource.EveryEvent += new Action<TraceEvent>(checkForEventMatch);

                    // Start processing the events
                    ThreadPool.QueueUserWorkItem(new WaitCallback(o => eventSource.Process()));
                }
                catch { }
            }

            public TestResult EndMonitoring()
            {
                eventSource.StopProcessing();
                session.Stop();
                return verifyExpectedCounts();
            }

            public void Stop()
            {
                if (session.IsActive)
                {
                    eventSource.StopProcessing();
                    session.Stop();
                }
            }

            #endregion

            #region Private Members

            void checkForEventMatch(TraceEvent obj)
            {
                ETWEventIdentifier compareEvent = new ETWEventIdentifier(obj.TaskGuid, (int)obj.Opcode, (int)obj.ID);

                if (this.ExpectedEvents.Contains(compareEvent))
                {
                    GlobalLog.LogDebug("Detected Event: " + this.ExpectedEvents[this.ExpectedEvents.IndexOf(compareEvent)].ToString());
                    DetectedEvents.Add(this.ExpectedEvents[this.ExpectedEvents.IndexOf(compareEvent)]);
                    this.ExpectedEvents.Remove(compareEvent);
                }
            }

            private TestResult verifyExpectedCounts()
            {
                if (this.ExpectedEvents.Count > 0)
                {
                    GlobalLog.LogEvidence("Failure: Following events were not seen:");
                    foreach (ETWEventIdentifier evnt in ExpectedEvents)
                    {
                        GlobalLog.LogEvidence(evnt.ToString());
                    }
                    return TestResult.Fail;
                }
                else
                {
                    GlobalLog.LogEvidence("Success!  All expected events observed!");
                    return TestResult.Pass;
                }
            }
            #endregion
        }
    }
}
