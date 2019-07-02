// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using Microsoft.Test.EventTracing.FastSerialization;
using System.Security;

/* This file was generated with the command */
// traceParserGen /merge CLREtwAll.man CLRTraceEventParser.cs
/* And then modified by hand to add functionality (handle to name lookup, fixup of evenMethodLoadUnloadTraceDatats ...) */
// The version before any hand modifications is kept as KernelTraceEventParser.base.cs, and a 3
// way diff is done when traceParserGen is rerun.  This allows the 'by-hand' modifications to be
// applied again if the mof or the traceParserGen transformation changes. 
// 
// See traceParserGen /usersGuide for more on the /merge option 
namespace Microsoft.Test.EventTracing
{
    /* Parsers defined in this file */
    // code:ClrTraceEventParser, code:ClrRundownTraceEventParser, code:ClrStressTraceEventParser 
    /* code:ClrPrivateTraceEventParser  code:#ClrPrivateProvider */
    [SecuritySafeCritical, SecurityCritical]
    public class ClrTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "Microsoft-Windows-DotNETRuntime";
        public static Guid ProviderGuid = new Guid(unchecked((int) 0xe13c0d23), unchecked((short) 0xccbc), unchecked((short) 0x4e12), 0x93, 0x1b, 0xd9, 0xcc, 0x2e, 0xee, 0x27, 0xe4);
        /// <summary>
        ///  Keywords are passed to code:TraceEventSession.EnableProvider to enable particular sets of
        /// </summary>
        public enum Keywords : long
        {
            /// <summary>
            /// Logging when garbage collections and finalization happen. 
            /// </summary>
            GC = 0x1,
            Binder = 0x4,
            /// <summary>
            /// Logging when modules actually get loaded and unloaded. 
            /// </summary>
            Loader = 0x8,
            /// <summary>
            /// Logging when Just in time (JIT) compilation occurs. 
            /// </summary>
            Jit = 0x10,
            /// <summary>
            /// Logging when precompiled native (NGEN) images are loaded.
            /// </summary>
            NGen = 0x20,
            /// <summary>
            /// Indicates that on attach or module load , a rundown of all existing methods should be done
            /// </summary>
            StartEnumeration = 0x40,
            /// <summary>
            /// Indicates that on detach or process shutdown, a rundown of all existing methods should be done
            /// </summary>
            StopEnumeration = 0x80,
            /// <summary>
            /// Events associted with validating security restrictions.
            /// </summary>
            Security = 0x400,
            /// <summary>
            /// Events for logging resource consumption on an app-domain level granularity
            /// </summary>
            AppDomainResourceManagement = 0x800,
            /// <summary>
            /// Logging of the internal workings of the Just In Time compiler.  This is fairly verbose.  
            /// It details decidions about interesting optimization (like inlining and tail call) 
            /// </summary>
            JitTracing = 0x1000,
            /// <summary>
            /// Log information about code thunks that transition between managed and unmanaged code. 
            /// </summary>
            Interop = 0x2000,
            /// <summary>
            /// Log when lock conentions occurs.  (Monitor.Enters actually blocks)
            /// </summary>
            Contention = 0x4000,
            /// <summary>
            /// Log exception processing.  
            /// </summary>
            Exception = 0x8000,
            /// <summary>
            /// Log events associated with the threadpool, and other threading events.  
            /// </summary>
            Threading = 0x10000,
            /// <summary>
            /// Also log the stack trace of events for which this is valuable.
            /// </summary>
            Stack = 0x40000000,
            /// <summary>
            /// Recommend default flags (good compromise on verbosity).  
            /// </summary>
            Default = GC | Binder | Loader | Jit | NGen | StopEnumeration | Security | AppDomainResourceManagement | Exception | Threading | Contention,
        };
        [CLSCompliant(false)]
        public ClrTraceEventParser(TraceEventSource source) : base(source) { }

        public event Action<GCStartTraceData> GCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCStartTraceData(value, 1, 1, "GC", GCTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCEndTraceData> GCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCEndTraceData(value, 2, 1, "GC", GCTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCRestartEEStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 3, 1, "GC", GCTaskGuid, 132, "RestartEEStop", ProviderGuid, ProviderName));
                // Added for V2 Runtime compatibilty
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 3, 1, "GC", GCTaskGuid, 4, "RestartEEStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCHeapStatsTraceData> GCHeapStats
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCHeapStatsTraceData(value, 4, 1, "GC", GCTaskGuid, 133, "HeapStats", ProviderGuid, ProviderName));
                // Added for V2 Runtime compatibilty
                source.RegisterEventTemplate(new GCHeapStatsTraceData(value, 4, 1, "GC", GCTaskGuid, 5, "HeapStats", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCCreateSegmentTraceData> GCCreateSegment
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCCreateSegmentTraceData(value, 5, 1, "GC", GCTaskGuid, 134, "CreateSegment", ProviderGuid, ProviderName));
                // Added for V2 Runtime compatibilty
                source.RegisterEventTemplate(new GCCreateSegmentTraceData(value, 5, 1, "GC", GCTaskGuid, 6, "CreateSegment", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCFreeSegmentTraceData> GCFreeSegment
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCFreeSegmentTraceData(value, 6, 1, "GC", GCTaskGuid, 135, "FreeSegment", ProviderGuid, ProviderName));
                // Added for V2 Runtime compatibilty
                source.RegisterEventTemplate(new GCFreeSegmentTraceData(value, 6, 1, "GC", GCTaskGuid, 7, "FreeSegment", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCRestartEEStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 7, 1, "GC", GCTaskGuid, 136, "RestartEEStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCSuspendEEStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 8, 1, "GC", GCTaskGuid, 137, "SuspendEEStop", ProviderGuid, ProviderName));
                // Added for V2 Runtime compatibilty
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 8, 1, "GC", GCTaskGuid, 9, "SuspendEEStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCSuspendEETraceData> GCSuspendEEStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCSuspendEETraceData(value, 9, 1, "GC", GCTaskGuid, 10, "SuspendEEStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCAllocationTickTraceData> GCAllocationTick
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCAllocationTickTraceData(value, 10, 1, "GC", GCTaskGuid, 11, "AllocationTick", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCCreateConcurrentThreadTraceData> GCCreateConcurrentThread
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCCreateConcurrentThreadTraceData(value, 11, 1, "GC", GCTaskGuid, 12, "CreateConcurrentThread", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCTerminateConcurrentThreadTraceData> GCTerminateConcurrentThread
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCTerminateConcurrentThreadTraceData(value, 12, 1, "GC", GCTaskGuid, 13, "TerminateConcurrentThread", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCFinalizersEndTraceData> GCFinalizersStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCFinalizersEndTraceData(value, 13, 1, "GC", GCTaskGuid, 15, "FinalizersStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCFinalizersStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 14, 1, "GC", GCTaskGuid, 19, "FinalizersStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrWorkerThreadTraceData> WorkerThreadCreationV2Start
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrWorkerThreadTraceData(value, 40, 2, "WorkerThreadCreationV2", WorkerThreadCreationV2TaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrWorkerThreadTraceData> WorkerThreadCreationV2Stop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrWorkerThreadTraceData(value, 41, 2, "WorkerThreadCreationV2", WorkerThreadCreationV2TaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrWorkerThreadTraceData> WorkerThreadRetirementV2Start
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrWorkerThreadTraceData(value, 42, 4, "WorkerThreadRetirementV2", WorkerThreadRetirementV2TaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrWorkerThreadTraceData> WorkerThreadRetirementV2Stop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrWorkerThreadTraceData(value, 43, 4, "WorkerThreadRetirementV2", WorkerThreadRetirementV2TaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<IOThreadTraceData> IOThreadCreationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new IOThreadTraceData(value, 44, 3, "IOThreadCreation", IOThreadCreationTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<IOThreadTraceData> IOThreadCreationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new IOThreadTraceData(value, 45, 3, "IOThreadCreation", IOThreadCreationTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<IOThreadTraceData> IOThreadRetirementStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new IOThreadTraceData(value, 46, 5, "IOThreadRetirement", IOThreadRetirementTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<IOThreadTraceData> IOThreadRetirementStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new IOThreadTraceData(value, 47, 5, "IOThreadRetirement", IOThreadRetirementTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrThreadPoolSuspendTraceData> ThreadpoolSuspensionV2Start
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrThreadPoolSuspendTraceData(value, 48, 6, "ThreadpoolSuspensionV2", ThreadpoolSuspensionV2TaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        [Obsolete("Only used on V2 runtimes.")]
        public event Action<ClrThreadPoolSuspendTraceData> ThreadpoolSuspensionV2Stop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrThreadPoolSuspendTraceData(value, 49, 6, "ThreadpoolSuspensionV2", ThreadpoolSuspensionV2TaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadTraceData> ThreadPoolWorkerThreadStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadTraceData(value, 50, 16, "ThreadPoolWorkerThread", ThreadPoolWorkerThreadTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadTraceData> ThreadPoolWorkerThreadStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadTraceData(value, 51, 16, "ThreadPoolWorkerThread", ThreadPoolWorkerThreadTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadTraceData> ThreadPoolWorkerThreadRetirementStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadTraceData(value, 52, 17, "ThreadPoolWorkerThreadRetirement", ThreadPoolWorkerThreadRetirementTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadTraceData> ThreadPoolWorkerThreadRetirementStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadTraceData(value, 53, 17, "ThreadPoolWorkerThreadRetirement", ThreadPoolWorkerThreadRetirementTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadAdjustmentSampleTraceData> ThreadPoolWorkerThreadAdjustmentSample
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadAdjustmentSampleTraceData(value, 54, 18, "ThreadPoolWorkerThreadAdjustment", ThreadPoolWorkerThreadAdjustmentTaskGuid, 100, "Sample", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData> ThreadPoolWorkerThreadAdjustmentAdjustment
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData(value, 55, 18, "ThreadPoolWorkerThreadAdjustment", ThreadPoolWorkerThreadAdjustmentTaskGuid, 101, "Adjustment", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadPoolWorkerThreadAdjustmentStatsTraceData> ThreadPoolWorkerThreadAdjustmentStats
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadPoolWorkerThreadAdjustmentStatsTraceData(value, 56, 18, "ThreadPoolWorkerThreadAdjustment", ThreadPoolWorkerThreadAdjustmentTaskGuid, 102, "Stats", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ExceptionTraceData> ExceptionStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ExceptionTraceData(value, 80, 7, "Exception", ExceptionTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ContentionTraceData> ContentionStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ContentionTraceData(value, 81, 8, "Contention", ContentionTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ClrStackWalkTraceData> ClrStackWalk
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrStackWalkTraceData(value, 82, 11, "ClrStack", ClrStackTaskGuid, 82, "Walk", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainMemAllocatedTraceData> AppDomainResourceManagementMemAllocated
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainMemAllocatedTraceData(value, 83, 14, "AppDomainResourceManagement", AppDomainResourceManagementTaskGuid, 48, "MemAllocated", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainMemSurvivedTraceData> AppDomainResourceManagementMemSurvived
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainMemSurvivedTraceData(value, 84, 14, "AppDomainResourceManagement", AppDomainResourceManagementTaskGuid, 49, "MemSurvived", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadCreatedTraceData> AppDomainResourceManagementThreadCreated
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadCreatedTraceData(value, 85, 14, "AppDomainResourceManagement", AppDomainResourceManagementTaskGuid, 50, "ThreadCreated", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadTerminatedOrTransitionTraceData> AppDomainResourceManagementThreadTerminated
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTerminatedOrTransitionTraceData(value, 86, 14, "AppDomainResourceManagement", AppDomainResourceManagementTaskGuid, 51, "ThreadTerminated", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadTerminatedOrTransitionTraceData> AppDomainResourceManagementDomainEnter
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTerminatedOrTransitionTraceData(value, 87, 14, "AppDomainResourceManagement", AppDomainResourceManagementTaskGuid, 52, "DomainEnter", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ILStubGeneratedTraceData> ILStubStubGenerated
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ILStubGeneratedTraceData(value, 88, 15, "ILStub", ILStubTaskGuid, 88, "StubGenerated", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ILStubCacheHitTraceData> ILStubStubCacheHit
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ILStubCacheHitTraceData(value, 89, 15, "ILStub", ILStubTaskGuid, 89, "StubCacheHit", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ContentionTraceData> ContentionStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ContentionTraceData(value, 91, 8, "Contention", ContentionTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> MethodDCStartCompleteV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 135, 9, "Method", MethodTaskGuid, 14, "DCStartCompleteV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> MethodDCEndCompleteV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 136, 9, "Method", MethodTaskGuid, 15, "DCEndCompleteV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodDCStartV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 137, 9, "Method", MethodTaskGuid, 35, "DCStartV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodDCStopV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 138, 9, "Method", MethodTaskGuid, 36, "DCStopV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodDCStartVerboseV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 139, 9, "Method", MethodTaskGuid, 39, "DCStartVerboseV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodDCStopVerboseV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 140, 9, "Method", MethodTaskGuid, 40, "DCStopVerboseV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodLoad
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 141, 9, "Method", MethodTaskGuid, 33, "Load", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodUnload
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 142, 9, "Method", MethodTaskGuid, 34, "Unload", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodLoadVerbose
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 143, 9, "Method", MethodTaskGuid, 37, "LoadVerbose", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodUnloadVerbose
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 144, 9, "Method", MethodTaskGuid, 38, "UnloadVerbose", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodJittingStartedTraceData> MethodJittingStarted
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodJittingStartedTraceData(value, 145, 9, "Method", MethodTaskGuid, 42, "JittingStarted", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> LoaderModuleDCStartV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 149, 10, "Loader", LoaderTaskGuid, 35, "ModuleDCStartV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> LoaderModuleDCStopV2
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 150, 10, "Loader", LoaderTaskGuid, 36, "ModuleDCStopV2", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DomainModuleLoadUnloadTraceData> LoaderDomainModuleLoad
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DomainModuleLoadUnloadTraceData(value, 151, 10, "Loader", LoaderTaskGuid, 45, "DomainModuleLoad", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> LoaderModuleLoad
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 152, 10, "Loader", LoaderTaskGuid, 33, "ModuleLoad", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> LoaderModuleUnload
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 153, 10, "Loader", LoaderTaskGuid, 34, "ModuleUnload", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AssemblyLoadUnloadTraceData> LoaderAssemblyLoad
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AssemblyLoadUnloadTraceData(value, 154, 10, "Loader", LoaderTaskGuid, 37, "AssemblyLoad", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AssemblyLoadUnloadTraceData> LoaderAssemblyUnload
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AssemblyLoadUnloadTraceData(value, 155, 10, "Loader", LoaderTaskGuid, 38, "AssemblyUnload", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainLoadUnloadTraceData> LoaderAppDomainLoad
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainLoadUnloadTraceData(value, 156, 10, "Loader", LoaderTaskGuid, 41, "AppDomainLoad", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainLoadUnloadTraceData> LoaderAppDomainUnload
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainLoadUnloadTraceData(value, 157, 10, "Loader", LoaderTaskGuid, 42, "AppDomainUnload", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StrongNameVerificationTraceData> StrongNameVerificationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StrongNameVerificationTraceData(value, 181, 12, "StrongNameVerification", StrongNameVerificationTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StrongNameVerificationTraceData> StrongNameVerificationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StrongNameVerificationTraceData(value, 182, 12, "StrongNameVerification", StrongNameVerificationTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AuthenticodeVerificationTraceData> AuthenticodeVerificationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AuthenticodeVerificationTraceData(value, 183, 13, "AuthenticodeVerification", AuthenticodeVerificationTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AuthenticodeVerificationTraceData> AuthenticodeVerificationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AuthenticodeVerificationTraceData(value, 184, 13, "AuthenticodeVerification", AuthenticodeVerificationTaskGuid, 2, "Stop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodJitInliningSucceededTraceData> MethodInliningSucceeded
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodJitInliningSucceededTraceData(value, 185, 9, "Method", MethodTaskGuid, 83, "InliningSucceeded", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodJitInliningFailedTraceData> MethodInliningFailed
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodJitInliningFailedTraceData(value, 186, 9, "Method", MethodTaskGuid, 84, "InliningFailed", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RuntimeInformationTraceData> RuntimeStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RuntimeInformationTraceData(value, 187, 19, "Runtime", RuntimeTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodJitTailCallSucceededTraceData> MethodTailCallSucceeded
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodJitTailCallSucceededTraceData(value, 188, 9, "Method", MethodTaskGuid, 85, "TailCallSucceeded", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodJitTailCallFailedTraceData> MethodTailCallFailed
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodJitTailCallFailedTraceData(value, 189, 9, "Method", MethodTaskGuid, 86, "TailCallFailed", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

       #region Event ID Definitions
        public const TraceEventID GCStartEventID = (TraceEventID) 1;
        public const TraceEventID GCStopEventID = (TraceEventID) 2;
        public const TraceEventID GCRestartEEStopEventID = (TraceEventID) 3;
        public const TraceEventID GCHeapStatsEventID = (TraceEventID) 4;
        public const TraceEventID GCCreateSegmentEventID = (TraceEventID) 5;
        public const TraceEventID GCFreeSegmentEventID = (TraceEventID) 6;
        public const TraceEventID GCRestartEEStartEventID = (TraceEventID) 7;
        public const TraceEventID GCSuspendEEStopEventID = (TraceEventID) 8;
        public const TraceEventID GCSuspendEEStartEventID = (TraceEventID) 9;
        public const TraceEventID GCAllocationTickEventID = (TraceEventID) 10;
        public const TraceEventID GCCreateConcurrentThreadEventID = (TraceEventID) 11;
        public const TraceEventID GCTerminateConcurrentThreadEventID = (TraceEventID) 12;
        public const TraceEventID GCFinalizersStopEventID = (TraceEventID) 13;
        public const TraceEventID GCFinalizersStartEventID = (TraceEventID) 14;
        public const TraceEventID WorkerThreadCreationV2StartEventID = (TraceEventID) 40;
        public const TraceEventID WorkerThreadCreationV2StopEventID = (TraceEventID) 41;
        public const TraceEventID WorkerThreadRetirementV2StartEventID = (TraceEventID) 42;
        public const TraceEventID WorkerThreadRetirementV2StopEventID = (TraceEventID) 43;
        public const TraceEventID IOThreadCreationStartEventID = (TraceEventID) 44;
        public const TraceEventID IOThreadCreationStopEventID = (TraceEventID) 45;
        public const TraceEventID IOThreadRetirementStartEventID = (TraceEventID) 46;
        public const TraceEventID IOThreadRetirementStopEventID = (TraceEventID) 47;
        public const TraceEventID ThreadpoolSuspensionV2StartEventID = (TraceEventID) 48;
        public const TraceEventID ThreadpoolSuspensionV2StopEventID = (TraceEventID) 49;
        public const TraceEventID ThreadPoolWorkerThreadStartEventID = (TraceEventID) 50;
        public const TraceEventID ThreadPoolWorkerThreadStopEventID = (TraceEventID) 51;
        public const TraceEventID ThreadPoolWorkerThreadRetirementStartEventID = (TraceEventID) 52;
        public const TraceEventID ThreadPoolWorkerThreadRetirementStopEventID = (TraceEventID) 53;
        public const TraceEventID ThreadPoolWorkerThreadAdjustmentSampleEventID = (TraceEventID) 54;
        public const TraceEventID ThreadPoolWorkerThreadAdjustmentAdjustmentEventID = (TraceEventID) 55;
        public const TraceEventID ThreadPoolWorkerThreadAdjustmentStatsEventID = (TraceEventID) 56;
        public const TraceEventID ExceptionStartEventID = (TraceEventID) 80;
        public const TraceEventID ContentionStartEventID = (TraceEventID) 81;
        public const TraceEventID ClrStackWalkEventID = (TraceEventID) 82;
        public const TraceEventID AppDomainResourceManagementMemAllocatedEventID = (TraceEventID) 83;
        public const TraceEventID AppDomainResourceManagementMemSurvivedEventID = (TraceEventID) 84;
        public const TraceEventID AppDomainResourceManagementThreadCreatedEventID = (TraceEventID) 85;
        public const TraceEventID AppDomainResourceManagementThreadTerminatedEventID = (TraceEventID) 86;
        public const TraceEventID AppDomainResourceManagementDomainEnterEventID = (TraceEventID) 87;
        public const TraceEventID ILStubStubGeneratedEventID = (TraceEventID) 88;
        public const TraceEventID ILStubStubCacheHitEventID = (TraceEventID) 89;
        public const TraceEventID ContentionStopEventID = (TraceEventID) 91;
        public const TraceEventID MethodDCStartCompleteV2EventID = (TraceEventID) 135;
        public const TraceEventID MethodDCEndCompleteV2EventID = (TraceEventID) 136;
        public const TraceEventID MethodDCStartV2EventID = (TraceEventID) 137;
        public const TraceEventID MethodDCStopV2EventID = (TraceEventID) 138;
        public const TraceEventID MethodDCStartVerboseV2EventID = (TraceEventID) 139;
        public const TraceEventID MethodDCStopVerboseV2EventID = (TraceEventID) 140;
        public const TraceEventID MethodLoadEventID = (TraceEventID) 141;
        public const TraceEventID MethodUnloadEventID = (TraceEventID) 142;
        public const TraceEventID MethodLoadVerboseEventID = (TraceEventID) 143;
        public const TraceEventID MethodUnloadVerboseEventID = (TraceEventID) 144;
        public const TraceEventID MethodJittingStartedEventID = (TraceEventID) 145;
        public const TraceEventID LoaderModuleDCStartV2EventID = (TraceEventID) 149;
        public const TraceEventID LoaderModuleDCStopV2EventID = (TraceEventID) 150;
        public const TraceEventID LoaderDomainModuleLoadEventID = (TraceEventID) 151;
        public const TraceEventID LoaderModuleLoadEventID = (TraceEventID) 152;
        public const TraceEventID LoaderModuleUnloadEventID = (TraceEventID) 153;
        public const TraceEventID LoaderAssemblyLoadEventID = (TraceEventID) 154;
        public const TraceEventID LoaderAssemblyUnloadEventID = (TraceEventID) 155;
        public const TraceEventID LoaderAppDomainLoadEventID = (TraceEventID) 156;
        public const TraceEventID LoaderAppDomainUnloadEventID = (TraceEventID) 157;
        public const TraceEventID StrongNameVerificationStartEventID = (TraceEventID) 181;
        public const TraceEventID StrongNameVerificationStopEventID = (TraceEventID) 182;
        public const TraceEventID AuthenticodeVerificationStartEventID = (TraceEventID) 183;
        public const TraceEventID AuthenticodeVerificationStopEventID = (TraceEventID) 184;
        public const TraceEventID MethodInliningSucceededEventID = (TraceEventID) 185;
        public const TraceEventID MethodInliningFailedEventID = (TraceEventID) 186;
        public const TraceEventID RuntimeStartEventID = (TraceEventID) 187;
        public const TraceEventID MethodTailCallSucceededEventID = (TraceEventID) 188;
        public const TraceEventID MethodTailCallFailedEventID = (TraceEventID) 189;
       #endregion

    #region private
        private static Guid GCTaskGuid = new Guid(unchecked((int) 0x044973cd), unchecked((short) 0x251f), unchecked((short) 0x4dff), 0xa3, 0xe9, 0x9d, 0x63, 0x07, 0x28, 0x6b, 0x05);
        private static Guid WorkerThreadCreationV2TaskGuid = new Guid(unchecked((int) 0xcfc4ba53), unchecked((short) 0xfb42), unchecked((short) 0x4757), 0x8b, 0x70, 0x5f, 0x5d, 0x51, 0xfe, 0xe2, 0xf4);
        private static Guid IOThreadCreationTaskGuid = new Guid(unchecked((int) 0xc71408de), unchecked((short) 0x42cc), unchecked((short) 0x4f81), 0x9c, 0x93, 0xb8, 0x91, 0x2a, 0xbf, 0x2a, 0x0f);
        private static Guid WorkerThreadRetirementV2TaskGuid = new Guid(unchecked((int) 0xefdf1eac), unchecked((short) 0x1d5d), unchecked((short) 0x4e84), 0x89, 0x3a, 0x19, 0xb8, 0x0f, 0x69, 0x21, 0x76);
        private static Guid IOThreadRetirementTaskGuid = new Guid(unchecked((int) 0x840c8456), unchecked((short) 0x6457), unchecked((short) 0x4eb7), 0x9c, 0xd0, 0xd2, 0x8f, 0x01, 0xc6, 0x4f, 0x5e);
        private static Guid ThreadpoolSuspensionV2TaskGuid = new Guid(unchecked((int) 0xc424b3e3), unchecked((short) 0x2ae0), unchecked((short) 0x416e), 0xa0, 0x39, 0x41, 0x0c, 0x5d, 0x8e, 0x5f, 0x14);
        private static Guid ExceptionTaskGuid = new Guid(unchecked((int) 0x300ce105), unchecked((short) 0x86d1), unchecked((short) 0x41f8), 0xb9, 0xd2, 0x83, 0xfc, 0xbf, 0xf3, 0x2d, 0x99);
        private static Guid ContentionTaskGuid = new Guid(unchecked((int) 0x561410f5), unchecked((short) 0xa138), unchecked((short) 0x4ab3), 0x94, 0x5e, 0x51, 0x64, 0x83, 0xcd, 0xdf, 0xbc);
        private static Guid MethodTaskGuid = new Guid(unchecked((int) 0x3044f61a), unchecked((short) 0x99b0), unchecked((short) 0x4c21), 0xb2, 0x03, 0xd3, 0x94, 0x23, 0xc7, 0x3b, 0x00);
        private static Guid LoaderTaskGuid = new Guid(unchecked((int) 0xd00792da), unchecked((short) 0x07b7), unchecked((short) 0x40f5), 0x97, 0xeb, 0x5d, 0x97, 0x4e, 0x05, 0x47, 0x40);
        private static Guid ClrStackTaskGuid = new Guid(unchecked((int) 0xd3363dc0), unchecked((short) 0x243a), unchecked((short) 0x4620), 0xa4, 0xd0, 0x8a, 0x07, 0xd7, 0x72, 0xf5, 0x33);
        private static Guid StrongNameVerificationTaskGuid = new Guid(unchecked((int) 0x15447a14), unchecked((short) 0xb523), unchecked((short) 0x46ae), 0xb7, 0x5b, 0x02, 0x3f, 0x90, 0x0b, 0x43, 0x93);
        private static Guid AuthenticodeVerificationTaskGuid = new Guid(unchecked((int) 0xb17304d9), unchecked((short) 0x5afa), unchecked((short) 0x4da6), 0x9f, 0x7b, 0x5a, 0x4f, 0xa7, 0x31, 0x29, 0xb6);
        private static Guid AppDomainResourceManagementTaskGuid = new Guid(unchecked((int) 0x88e83959), unchecked((short) 0x6185), unchecked((short) 0x4e0b), 0x95, 0xb8, 0x0e, 0x4a, 0x35, 0xdf, 0x61, 0x22);
        private static Guid ILStubTaskGuid = new Guid(unchecked((int) 0xd00792da), unchecked((short) 0x07b7), unchecked((short) 0x40f5), 0x00, 0x00, 0x5d, 0x97, 0x4e, 0x05, 0x47, 0x40);
        private static Guid ThreadPoolWorkerThreadTaskGuid = new Guid(unchecked((int) 0x8a9a44ab), unchecked((short) 0xf681), unchecked((short) 0x4271), 0x88, 0x10, 0x83, 0x0d, 0xab, 0x9f, 0x56, 0x21);
        private static Guid ThreadPoolWorkerThreadRetirementTaskGuid = new Guid(unchecked((int) 0x402ee399), unchecked((short) 0xc137), unchecked((short) 0x4dc0), 0xa5, 0xab, 0x3c, 0x2d, 0xea, 0x64, 0xac, 0x9c);
        private static Guid ThreadPoolWorkerThreadAdjustmentTaskGuid = new Guid(unchecked((int) 0x94179831), unchecked((short) 0xe99a), unchecked((short) 0x4625), 0x88, 0x24, 0x23, 0xca, 0x5e, 0x00, 0xca, 0x7d);
        private static Guid RuntimeTaskGuid = new Guid(unchecked((int) 0xcd7d3e32), unchecked((short) 0x65fe), unchecked((short) 0x40cd), 0x92, 0x25, 0xa2, 0x57, 0x7d, 0x20, 0x3f, 0xc3);
    #endregion
    }

    public sealed class GCStartTraceData : TraceEvent
    {
        public int Count { get { return GetInt32At(0); } }
        public GCReason Reason { get { if (Version >= 1) return (GCReason)GetInt32At(8); return (GCReason)GetInt32At(4); } }
        public int Depth { get { if (Version >= 1) return GetInt32At(4); return 0; } }
        public GCType Type { get { if (Version >= 1) return (GCType)GetInt32At(12); return (GCType)0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(16); return 0; } }

        #region Private
        internal GCStartTraceData(Action<GCStartTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 18));
            Debug.Assert(!(Version > 1 && EventDataLength < 18));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Count", Count);
             sb.XmlAttrib("Reason", Reason);
             sb.XmlAttrib("Depth", Depth);
             sb.XmlAttrib("Type", Type);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Count", "Reason", "Depth", "Type", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Count;
                case 1:
                    return Reason;
                case 2:
                    return Depth;
                case 3:
                    return Type;
                case 4:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCStartTraceData> Action;
        #endregion
    }
    public sealed class GCEndTraceData : TraceEvent
    {
        public int Count { get { return GetInt32At(0); } }
        public int Depth { get { if (Version >= 1) return GetInt32At(4); return GetInt16At(4); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal GCEndTraceData(Action<GCEndTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 6));           // HAND_MODIFIED <
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Count", Count);
             sb.XmlAttrib("Depth", Depth);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Count", "Depth", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Count;
                case 1:
                    return Depth;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCEndTraceData> Action;
        #endregion
    }
    public sealed class GCNoUserDataTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal GCNoUserDataTraceData(Action<GCNoUserDataTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCNoUserDataTraceData> Action;
        #endregion
    }
    public sealed class GCHeapStatsTraceData : TraceEvent
    {
        public long GenerationSize0 { get { return GetInt64At(0); } }
        public long TotalPromotedSize0 { get { return GetInt64At(8); } }
        public long GenerationSize1 { get { return GetInt64At(16); } }
        public long TotalPromotedSize1 { get { return GetInt64At(24); } }
        public long GenerationSize2 { get { return GetInt64At(32); } }
        public long TotalPromotedSize2 { get { return GetInt64At(40); } }
        public long GenerationSize3 { get { return GetInt64At(48); } }
        public long TotalPromotedSize3 { get { return GetInt64At(56); } }
        public long FinalizationPromotedSize { get { return GetInt64At(64); } }
        public long FinalizationPromotedCount { get { return GetInt64At(72); } }
        public int PinnedObjectCount { get { return GetInt32At(80); } }
        public int SinkBlockCount { get { return GetInt32At(84); } }
        public int GCHandleCount { get { return GetInt32At(88); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(92); return 0; } }

        #region Private
        internal GCHeapStatsTraceData(Action<GCHeapStatsTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 96));          // HAND_MODIFIED C++ pads to 96
            Debug.Assert(!(Version == 1 && EventDataLength != 94));
            Debug.Assert(!(Version > 1 && EventDataLength < 94));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("GenerationSize0", GenerationSize0);
             sb.XmlAttribHex("TotalPromotedSize0", TotalPromotedSize0);
             sb.XmlAttribHex("GenerationSize1", GenerationSize1);
             sb.XmlAttribHex("TotalPromotedSize1", TotalPromotedSize1);
             sb.XmlAttribHex("GenerationSize2", GenerationSize2);
             sb.XmlAttribHex("TotalPromotedSize2", TotalPromotedSize2);
             sb.XmlAttribHex("GenerationSize3", GenerationSize3);
             sb.XmlAttribHex("TotalPromotedSize3", TotalPromotedSize3);
             sb.XmlAttribHex("FinalizationPromotedSize", FinalizationPromotedSize);
             sb.XmlAttrib("FinalizationPromotedCount", FinalizationPromotedCount);
             sb.XmlAttrib("PinnedObjectCount", PinnedObjectCount);
             sb.XmlAttrib("SinkBlockCount", SinkBlockCount);
             sb.XmlAttrib("GCHandleCount", GCHandleCount);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "GenerationSize0", "TotalPromotedSize0", "GenerationSize1", "TotalPromotedSize1", "GenerationSize2", "TotalPromotedSize2", "GenerationSize3", "TotalPromotedSize3", "FinalizationPromotedSize", "FinalizationPromotedCount", "PinnedObjectCount", "SinkBlockCount", "GCHandleCount", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return GenerationSize0;
                case 1:
                    return TotalPromotedSize0;
                case 2:
                    return GenerationSize1;
                case 3:
                    return TotalPromotedSize1;
                case 4:
                    return GenerationSize2;
                case 5:
                    return TotalPromotedSize2;
                case 6:
                    return GenerationSize3;
                case 7:
                    return TotalPromotedSize3;
                case 8:
                    return FinalizationPromotedSize;
                case 9:
                    return FinalizationPromotedCount;
                case 10:
                    return PinnedObjectCount;
                case 11:
                    return SinkBlockCount;
                case 12:
                    return GCHandleCount;
                case 13:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCHeapStatsTraceData> Action;
        #endregion
    }
    public sealed class GCCreateSegmentTraceData : TraceEvent
    {
        public long Address { get { return GetInt64At(0); } }
        public long Size { get { return GetInt64At(8); } }
        public GCSegmentType Type { get { return (GCSegmentType)GetInt32At(16); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(20); return 0; } }

        #region Private
        internal GCCreateSegmentTraceData(Action<GCCreateSegmentTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 20));      // HAND_MODIFIED V0 has 24  because of C++ rounding
            Debug.Assert(!(Version == 1 && EventDataLength != 22));
            Debug.Assert(!(Version > 1 && EventDataLength < 22));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("Address", Address);
             sb.XmlAttribHex("Size", Size);
             sb.XmlAttrib("Type", Type);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Address", "Size", "Type", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Address;
                case 1:
                    return Size;
                case 2:
                    return Type;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCCreateSegmentTraceData> Action;
        #endregion
    }
    public sealed class GCFreeSegmentTraceData : TraceEvent
    {
        public long Address { get { return GetInt64At(0); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal GCFreeSegmentTraceData(Action<GCFreeSegmentTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("Address", Address);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Address", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Address;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCFreeSegmentTraceData> Action;
        #endregion
    }
    public sealed class GCSuspendEETraceData : TraceEvent
    {
        public GCReason Reason { get { if (Version >= 1) return (GCReason)GetInt32At(0); return (GCReason)GetInt16At(0); } }
        public int Count { get { if (Version >= 1) return GetInt32At(4); return 0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal GCSuspendEETraceData(Action<GCSuspendEETraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 2));       // HAND_MODIFIED 
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Reason", Reason);
             sb.XmlAttrib("Count", Count);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reason", "Count", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reason;
                case 1:
                    return Count;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCSuspendEETraceData> Action;
        #endregion
    }
    public sealed class GCAllocationTickTraceData : TraceEvent
    {
        public int AllocationAmount { get { return GetInt32At(0); } }
        public GCAllocationKind AllocationKind { get { return (GCAllocationKind)GetInt32At(4); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal GCAllocationTickTraceData(Action<GCAllocationTickTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("AllocationAmount", AllocationAmount);
             sb.XmlAttrib("AllocationKind", AllocationKind);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AllocationAmount", "AllocationKind", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AllocationAmount;
                case 1:
                    return AllocationKind;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCAllocationTickTraceData> Action;
        #endregion
    }
    public sealed class GCCreateConcurrentThreadTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal GCCreateConcurrentThreadTraceData(Action<GCCreateConcurrentThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCCreateConcurrentThreadTraceData> Action;
        #endregion
    }
    public sealed class GCTerminateConcurrentThreadTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal GCTerminateConcurrentThreadTraceData(Action<GCTerminateConcurrentThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCTerminateConcurrentThreadTraceData> Action;
        #endregion
    }
    public sealed class GCFinalizersEndTraceData : TraceEvent
    {
        public int Count { get { return GetInt32At(0); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(4); return 0; } }

        #region Private
        internal GCFinalizersEndTraceData(Action<GCFinalizersEndTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version == 1 && EventDataLength != 6));
            Debug.Assert(!(Version > 1 && EventDataLength < 6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Count", Count);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Count", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Count;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCFinalizersEndTraceData> Action;
        #endregion
    }
    public sealed class ClrWorkerThreadTraceData : TraceEvent
    {
        public int WorkerThreadCount { get { return GetInt32At(0); } }
        public int RetiredWorkerThreads { get { return GetInt32At(4); } }

        #region Private
        internal ClrWorkerThreadTraceData(Action<ClrWorkerThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version > 0 && EventDataLength < 8));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("WorkerThreadCount", WorkerThreadCount);
             sb.XmlAttrib("RetiredWorkerThreads", RetiredWorkerThreads);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "WorkerThreadCount", "RetiredWorkerThreads"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return WorkerThreadCount;
                case 1:
                    return RetiredWorkerThreads;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ClrWorkerThreadTraceData> Action;
        #endregion
    }
    public sealed class IOThreadTraceData : TraceEvent
    {
        public int IOThreadCount { get { return GetInt32At(0); } }
        public int RetiredIOThreads { get { return GetInt32At(4); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal IOThreadTraceData(Action<IOThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("IOThreadCount", IOThreadCount);
             sb.XmlAttrib("RetiredIOThreads", RetiredIOThreads);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IOThreadCount", "RetiredIOThreads", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IOThreadCount;
                case 1:
                    return RetiredIOThreads;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<IOThreadTraceData> Action;
        #endregion
    }
    public sealed class ClrThreadPoolSuspendTraceData : TraceEvent
    {
        public int ClrThreadID { get { return GetInt32At(0); } }
        public int CpuUtilization { get { return GetInt32At(4); } }

        #region Private
        internal ClrThreadPoolSuspendTraceData(Action<ClrThreadPoolSuspendTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version > 0 && EventDataLength < 8));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrThreadID", ClrThreadID);
             sb.XmlAttrib("CpuUtilization", CpuUtilization);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrThreadID", "CpuUtilization"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrThreadID;
                case 1:
                    return CpuUtilization;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ClrThreadPoolSuspendTraceData> Action;
        #endregion
    }
    public sealed class ThreadPoolWorkerThreadTraceData : TraceEvent
    {
        public int ActiveWorkerThreadCount { get { return GetInt32At(0); } }
        public int RetiredWorkerThreadCount { get { return GetInt32At(4); } }
        public int ClrInstanceID { get { return GetInt16At(8); } }

        #region Private
        internal ThreadPoolWorkerThreadTraceData(Action<ThreadPoolWorkerThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 10));
            Debug.Assert(!(Version > 0 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ActiveWorkerThreadCount", ActiveWorkerThreadCount);
             sb.XmlAttrib("RetiredWorkerThreadCount", RetiredWorkerThreadCount);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ActiveWorkerThreadCount", "RetiredWorkerThreadCount", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ActiveWorkerThreadCount;
                case 1:
                    return RetiredWorkerThreadCount;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadPoolWorkerThreadTraceData> Action;
        #endregion
    }
    public sealed class ThreadPoolWorkerThreadAdjustmentSampleTraceData : TraceEvent
    {
        public double Throughput { get { return GetDoubleAt(0); } }
        public int ClrInstanceID { get { return GetInt16At(8); } }

        #region Private
        internal ThreadPoolWorkerThreadAdjustmentSampleTraceData(Action<ThreadPoolWorkerThreadAdjustmentSampleTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 10));
            Debug.Assert(!(Version > 0 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Throughput", Throughput);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Throughput", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Throughput;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadPoolWorkerThreadAdjustmentSampleTraceData> Action;
        #endregion
    }
    public sealed class ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData : TraceEvent
    {
        public double AverageThroughput { get { return GetDoubleAt(0); } }
        public int NewWorkerThreadCount { get { return GetInt32At(8); } }
        public ThreadAdjustmentReason Reason { get { return (ThreadAdjustmentReason)GetInt32At(12); } }
        public int ClrInstanceID { get { return GetInt16At(16); } }

        #region Private
        internal ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData(Action<ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 18));
            Debug.Assert(!(Version > 0 && EventDataLength < 18));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("AverageThroughput", AverageThroughput);
             sb.XmlAttrib("NewWorkerThreadCount", NewWorkerThreadCount);
             sb.XmlAttrib("Reason", Reason);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AverageThroughput", "NewWorkerThreadCount", "Reason", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AverageThroughput;
                case 1:
                    return NewWorkerThreadCount;
                case 2:
                    return Reason;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadPoolWorkerThreadAdjustmentAdjustmentTraceData> Action;
        #endregion
    }
    public sealed class ThreadPoolWorkerThreadAdjustmentStatsTraceData : TraceEvent
    {
        public double Duration { get { return GetDoubleAt(0); } }
        public double Throughput { get { return GetDoubleAt(8); } }
        public double ThreadWave { get { return GetDoubleAt(16); } }
        public double ThroughputWave { get { return GetDoubleAt(24); } }
        public double ThroughputErrorEstimate { get { return GetDoubleAt(32); } }
        public double AverageThroughputErrorEstimate { get { return GetDoubleAt(40); } }
        public double ThroughputRatio { get { return GetDoubleAt(48); } }
        public double Confidence { get { return GetDoubleAt(56); } }
        public double NewControlSetting { get { return GetDoubleAt(64); } }
        public int NewThreadWaveMagnitude { get { return GetInt16At(72); } }
        public int ClrInstanceID { get { return GetInt16At(74); } }

        #region Private
        internal ThreadPoolWorkerThreadAdjustmentStatsTraceData(Action<ThreadPoolWorkerThreadAdjustmentStatsTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 76));
            Debug.Assert(!(Version > 0 && EventDataLength < 76));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Duration", Duration);
             sb.XmlAttrib("Throughput", Throughput);
             sb.XmlAttrib("ThreadWave", ThreadWave);
             sb.XmlAttrib("ThroughputWave", ThroughputWave);
             sb.XmlAttrib("ThroughputErrorEstimate", ThroughputErrorEstimate);
             sb.XmlAttrib("AverageThroughputErrorEstimate", AverageThroughputErrorEstimate);
             sb.XmlAttrib("ThroughputRatio", ThroughputRatio);
             sb.XmlAttrib("Confidence", Confidence);
             sb.XmlAttrib("NewControlSetting", NewControlSetting);
             sb.XmlAttrib("NewThreadWaveMagnitude", NewThreadWaveMagnitude);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Duration", "Throughput", "ThreadWave", "ThroughputWave", "ThroughputErrorEstimate", "AverageThroughputErrorEstimate", "ThroughputRatio", "Confidence", "NewControlSetting", "NewThreadWaveMagnitude", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Duration;
                case 1:
                    return Throughput;
                case 2:
                    return ThreadWave;
                case 3:
                    return ThroughputWave;
                case 4:
                    return ThroughputErrorEstimate;
                case 5:
                    return AverageThroughputErrorEstimate;
                case 6:
                    return ThroughputRatio;
                case 7:
                    return Confidence;
                case 8:
                    return NewControlSetting;
                case 9:
                    return NewThreadWaveMagnitude;
                case 10:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadPoolWorkerThreadAdjustmentStatsTraceData> Action;
        #endregion
    }
    public sealed class ExceptionTraceData : TraceEvent
    {
        public string ExceptionType { get { if (Version >= 1) return GetUnicodeStringAt(0); return ""; } }
        public string ExceptionMessage { get { if (Version >= 1) return GetUnicodeStringAt(SkipUnicodeString(0)); return ""; } }
        public Address ExceptionEIP { get { if (Version >= 1) return GetHostPointer(SkipUnicodeString(SkipUnicodeString(0))); return 0; } }
        public int ExceptionHRESULT { get { if (Version >= 1) return GetInt32At(HostOffset(SkipUnicodeString(SkipUnicodeString(0))+4, 1)); return 0; } }
        public ExceptionThrownFlags ExceptionFlags { get { if (Version >= 1) return (ExceptionThrownFlags)GetInt16At(HostOffset(SkipUnicodeString(SkipUnicodeString(0))+8, 1)); return (ExceptionThrownFlags)0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(HostOffset(SkipUnicodeString(SkipUnicodeString(0))+10, 1)); return 0; } }

        #region Private
        internal ExceptionTraceData(Action<ExceptionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != HostOffset(SkipUnicodeString(SkipUnicodeString(0))+12, 1)));
            Debug.Assert(!(Version > 1 && EventDataLength < HostOffset(SkipUnicodeString(SkipUnicodeString(0))+12, 1)));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ExceptionType", ExceptionType);
             sb.XmlAttrib("ExceptionMessage", ExceptionMessage);
             sb.XmlAttribHex("ExceptionEIP", ExceptionEIP);
             sb.XmlAttribHex("ExceptionHRESULT", ExceptionHRESULT);
             sb.XmlAttrib("ExceptionFlags", ExceptionFlags);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ExceptionType", "ExceptionMessage", "ExceptionEIP", "ExceptionHRESULT", "ExceptionFlags", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ExceptionType;
                case 1:
                    return ExceptionMessage;
                case 2:
                    return ExceptionEIP;
                case 3:
                    return ExceptionHRESULT;
                case 4:
                    return ExceptionFlags;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ExceptionTraceData> Action;
        #endregion
    }
    public sealed class ContentionTraceData : TraceEvent
    {
        public ContentionFlags ContentionFlags { get { if (Version >= 1) return (ContentionFlags)GetByteAt(0); return (ContentionFlags)0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(1); return 0; } }

        #region Private
        internal ContentionTraceData(Action<ContentionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 3));
            Debug.Assert(!(Version > 1 && EventDataLength < 3));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ContentionFlags", ContentionFlags);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ContentionFlags", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ContentionFlags;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ContentionTraceData> Action;
        #endregion
    }
    public sealed class ClrStackWalkTraceData : TraceEvent
    {
        public int ClrInstanceID { get { return GetInt16At(0); } }
        // Skipping Reserved1
        // Skipping Reserved2
        public int FrameCount { get { return GetInt32At(4); } }
        /// <summary>
        /// Fetches the instruction pointer of a eventToStack frame 0 is the deepest frame, and the maximum should
        /// be a thread offset routine (if you get a complete eventToStack).  
        /// </summary>
        /// <param name="i">The index of the frame to fetch.  0 is the CPU EIP, 1 is the Caller of that
        /// routine ...</param>
        /// <returns>The instruction pointer of the specified frame.</returns>
        public Address InstructionPointer(int i)
        {
            Debug.Assert(0 <= i && i < FrameCount);
            return GetHostPointer(8 + i * PointerSize);
        }

        #region Private
        internal ClrStackWalkTraceData(Action<ClrStackWalkTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(EventDataLength < 6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb).XmlAttrib("ClrInstanceID", ClrInstanceID).XmlAttrib("FrameCount", FrameCount).AppendLine(">");
            for (int i = 0; i < FrameCount; i++)
            {
                sb.Append("  ");
                sb.Append("0x").Append(((ulong)InstructionPointer(i)).ToString("x"));
            }
            sb.AppendLine();
            sb.Append("</Event>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID", "FrameCount" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                case 1:
                    return FrameCount;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ClrStackWalkTraceData> Action;
        #endregion
    }
    public sealed class AppDomainMemAllocatedTraceData : TraceEvent
    {
        public long AppDomainID { get { return GetInt64At(0); } }
        public long Allocated { get { return GetInt64At(8); } }
        public int ClrInstanceID { get { return GetInt16At(16); } }

        #region Private
        internal AppDomainMemAllocatedTraceData(Action<AppDomainMemAllocatedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 18));
            Debug.Assert(!(Version > 0 && EventDataLength < 18));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttribHex("Allocated", Allocated);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AppDomainID", "Allocated", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AppDomainID;
                case 1:
                    return Allocated;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<AppDomainMemAllocatedTraceData> Action;
        #endregion
    }
    public sealed class AppDomainMemSurvivedTraceData : TraceEvent
    {
        public long AppDomainID { get { return GetInt64At(0); } }
        public long Survived { get { return GetInt64At(8); } }
        public long ProcessSurvived { get { return GetInt64At(16); } }
        public int ClrInstanceID { get { return GetInt16At(24); } }

        #region Private
        internal AppDomainMemSurvivedTraceData(Action<AppDomainMemSurvivedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 26));
            Debug.Assert(!(Version > 0 && EventDataLength < 26));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttribHex("Survived", Survived);
             sb.XmlAttribHex("ProcessSurvived", ProcessSurvived);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AppDomainID", "Survived", "ProcessSurvived", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AppDomainID;
                case 1:
                    return Survived;
                case 2:
                    return ProcessSurvived;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<AppDomainMemSurvivedTraceData> Action;
        #endregion
    }
    public sealed class ThreadCreatedTraceData : TraceEvent
    {
        public long ManagedThreadID { get { return GetInt64At(0); } }
        public long AppDomainID { get { return GetInt64At(8); } }
        public int Flags { get { return GetInt32At(16); } }
        public int ManagedThreadIndex { get { return GetInt32At(20); } }
        public int OSThreadID { get { return GetInt32At(24); } }
        public int ClrInstanceID { get { return GetInt16At(28); } }

        #region Private
        internal ThreadCreatedTraceData(Action<ThreadCreatedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 30));
            Debug.Assert(!(Version > 0 && EventDataLength < 30));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("ManagedThreadID", ManagedThreadID);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttribHex("Flags", Flags);
             sb.XmlAttrib("ManagedThreadIndex", ManagedThreadIndex);
             sb.XmlAttrib("OSThreadID", OSThreadID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ManagedThreadID", "AppDomainID", "Flags", "ManagedThreadIndex", "OSThreadID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ManagedThreadID;
                case 1:
                    return AppDomainID;
                case 2:
                    return Flags;
                case 3:
                    return ManagedThreadIndex;
                case 4:
                    return OSThreadID;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadCreatedTraceData> Action;
        #endregion
    }
    public sealed class ThreadTerminatedOrTransitionTraceData : TraceEvent
    {
        public long ManagedThreadID { get { return GetInt64At(0); } }
        public long AppDomainID { get { return GetInt64At(8); } }
        public int ClrInstanceID { get { return GetInt16At(16); } }

        #region Private
        internal ThreadTerminatedOrTransitionTraceData(Action<ThreadTerminatedOrTransitionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 18));
            Debug.Assert(!(Version > 0 && EventDataLength < 18));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("ManagedThreadID", ManagedThreadID);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ManagedThreadID", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ManagedThreadID;
                case 1:
                    return AppDomainID;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadTerminatedOrTransitionTraceData> Action;
        #endregion
    }
    public sealed class ILStubGeneratedTraceData : TraceEvent
    {
        public int ClrInstanceID { get { return GetInt16At(0); } }
        public long ModuleID { get { return GetInt64At(2); } }
        public long StubMethodID { get { return GetInt64At(10); } }
        public ILStubGeneratedFlags StubFlags { get { return (ILStubGeneratedFlags)GetInt32At(18); } }
        public int ManagedInteropMethodToken { get { return GetInt32At(22); } }
        public string ManagedInteropMethodNamespace { get { return GetUnicodeStringAt(26); } }
        public string ManagedInteropMethodName { get { return GetUnicodeStringAt(SkipUnicodeString(26)); } }
        public string ManagedInteropMethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(26))); } }
        public string NativeMethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(26)))); } }
        public string StubMethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(26))))); } }
        public string StubMethodILCode { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(26)))))); } }

        #region Private
        internal ILStubGeneratedTraceData(Action<ILStubGeneratedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(26))))))));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(26))))))));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("StubMethodID", StubMethodID);
             sb.XmlAttrib("StubFlags", StubFlags);
             sb.XmlAttribHex("ManagedInteropMethodToken", ManagedInteropMethodToken);
             sb.XmlAttrib("ManagedInteropMethodNamespace", ManagedInteropMethodNamespace);
             sb.XmlAttrib("ManagedInteropMethodName", ManagedInteropMethodName);
             sb.XmlAttrib("ManagedInteropMethodSignature", ManagedInteropMethodSignature);
             sb.XmlAttrib("NativeMethodSignature", NativeMethodSignature);
             sb.XmlAttrib("StubMethodSignature", StubMethodSignature);
             sb.XmlAttrib("StubMethodILCode", StubMethodILCode);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID", "ModuleID", "StubMethodID", "StubFlags", "ManagedInteropMethodToken", "ManagedInteropMethodNamespace", "ManagedInteropMethodName", "ManagedInteropMethodSignature", "NativeMethodSignature", "StubMethodSignature", "StubMethodILCode"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                case 1:
                    return ModuleID;
                case 2:
                    return StubMethodID;
                case 3:
                    return StubFlags;
                case 4:
                    return ManagedInteropMethodToken;
                case 5:
                    return ManagedInteropMethodNamespace;
                case 6:
                    return ManagedInteropMethodName;
                case 7:
                    return ManagedInteropMethodSignature;
                case 8:
                    return NativeMethodSignature;
                case 9:
                    return StubMethodSignature;
                case 10:
                    return StubMethodILCode;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ILStubGeneratedTraceData> Action;
        #endregion
    }
    public sealed class ILStubCacheHitTraceData : TraceEvent
    {
        public int ClrInstanceID { get { return GetInt16At(0); } }
        public long ModuleID { get { return GetInt64At(2); } }
        public long StubMethodID { get { return GetInt64At(10); } }
        public int ManagedInteropMethodToken { get { return GetInt32At(18); } }
        public string ManagedInteropMethodNamespace { get { return GetUnicodeStringAt(22); } }
        public string ManagedInteropMethodName { get { return GetUnicodeStringAt(SkipUnicodeString(22)); } }
        public string ManagedInteropMethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(22))); } }

        #region Private
        internal ILStubCacheHitTraceData(Action<ILStubCacheHitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(22)))));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(22)))));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("StubMethodID", StubMethodID);
             sb.XmlAttribHex("ManagedInteropMethodToken", ManagedInteropMethodToken);
             sb.XmlAttrib("ManagedInteropMethodNamespace", ManagedInteropMethodNamespace);
             sb.XmlAttrib("ManagedInteropMethodName", ManagedInteropMethodName);
             sb.XmlAttrib("ManagedInteropMethodSignature", ManagedInteropMethodSignature);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID", "ModuleID", "StubMethodID", "ManagedInteropMethodToken", "ManagedInteropMethodNamespace", "ManagedInteropMethodName", "ManagedInteropMethodSignature"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                case 1:
                    return ModuleID;
                case 2:
                    return StubMethodID;
                case 3:
                    return ManagedInteropMethodToken;
                case 4:
                    return ManagedInteropMethodNamespace;
                case 5:
                    return ManagedInteropMethodName;
                case 6:
                    return ManagedInteropMethodSignature;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ILStubCacheHitTraceData> Action;
        #endregion
    }

    public sealed class MethodLoadUnloadTraceData : TraceEvent
    {
        public long MethodID { get { return GetInt64At(0); } }
        public long ModuleID { get { return GetInt64At(8); } }
        public Address MethodStartAddress { get { return (Address)GetInt64At(16); } }
        public int MethodSize { get { return GetInt32At(24); } }
        public int MethodToken { get { return GetInt32At(28); } }
        public MethodFlags MethodFlags { get { return (MethodFlags)GetInt32At(32); } }
        public bool IsDynamic { get { return (MethodFlags & MethodFlags.Dynamic) != 0; } }
        public bool IsGeneric { get { return (MethodFlags & MethodFlags.Generic) != 0; } }
        public bool IsJitted { get { return (MethodFlags & MethodFlags.Jitted) != 0; } }
        public int MethodExtent { get { return GetInt32At(32) >> 28; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(36); return 0; } }

        #region Private
        internal MethodLoadUnloadTraceData(Action<MethodLoadUnloadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 36));
            Debug.Assert(!(Version == 1 && EventDataLength != 38));
            Debug.Assert(!(Version > 1 && EventDataLength < 38));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("MethodID", MethodID);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("MethodStartAddress", MethodStartAddress);
             sb.XmlAttribHex("MethodSize", MethodSize);
             sb.XmlAttribHex("MethodToken", MethodToken);
             sb.XmlAttrib("MethodFlags", MethodFlags);
            sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodID", "ModuleID", "MethodStartAddress", "MethodSize", "MethodToken", "MethodFlags", "ClrInstanceID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodID;
                case 1:
                    return ModuleID;
                case 2:
                    return MethodStartAddress;
                case 3:
                    return MethodSize;
                case 4:
                    return MethodToken;
                case 5:
                    return MethodFlags;
                case 6:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodLoadUnloadTraceData> Action;
        #endregion
    }
    public sealed class MethodLoadUnloadVerboseTraceData : TraceEvent
    {
        public long MethodID { get { return GetInt64At(0); } }
        public long ModuleID { get { return GetInt64At(8); } }
        public Address MethodStartAddress { get { return (Address)GetInt64At(16); } }
        public int MethodSize { get { return GetInt32At(24); } }
        public int MethodToken { get { return GetInt32At(28); } }
        public MethodFlags MethodFlags { get { return (MethodFlags)GetInt32At(32); } }
        public bool IsDynamic { get { return (MethodFlags & MethodFlags.Dynamic) != 0; } }
        public bool IsGeneric { get { return (MethodFlags & MethodFlags.Generic) != 0; } }
        public bool IsJitted { get { return (MethodFlags & MethodFlags.Jitted) != 0; } }
        public int MethodExtent { get { return GetInt32At(32) >> 28; } }
        public string MethodNamespace { get { return GetUnicodeStringAt(36); } }
        public string MethodName { get { return GetUnicodeStringAt(SkipUnicodeString(36)); } }
        public string MethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(36))); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(36)))); return 0; } }

        #region Private
        internal MethodLoadUnloadVerboseTraceData(Action<MethodLoadUnloadVerboseTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(36)))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(36))) + 2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(36))) + 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("MethodID", MethodID);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("MethodStartAddress", MethodStartAddress);
             sb.XmlAttribHex("MethodSize", MethodSize);
             sb.XmlAttribHex("MethodToken", MethodToken);
             sb.XmlAttrib("MethodFlags", MethodFlags);
             sb.XmlAttrib("MethodNamespace", MethodNamespace);
             sb.XmlAttrib("MethodName", MethodName);
             sb.XmlAttrib("MethodSignature", MethodSignature);
            sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodID", "ModuleID", "MethodStartAddress", "MethodSize", "MethodToken", "MethodFlags", "MethodNamespace", "MethodName", "MethodSignature", "ClrInstanceID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodID;
                case 1:
                    return ModuleID;
                case 2:
                    return MethodStartAddress;
                case 3:
                    return MethodSize;
                case 4:
                    return MethodToken;
                case 5:
                    return MethodFlags;
                case 6:
                    return MethodNamespace;
                case 7:
                    return MethodName;
                case 8:
                    return MethodSignature;
                case 9:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodLoadUnloadVerboseTraceData> Action;
        #endregion
    }

    public sealed class MethodJittingStartedTraceData : TraceEvent
    {
        public long MethodID { get { return GetInt64At(0); } }
        public long ModuleID { get { return GetInt64At(8); } }
        public int MethodToken { get { return GetInt32At(16); } }
        public int MethodILSize { get { return GetInt32At(20); } }
        public string MethodNamespace { get { return GetUnicodeStringAt(24); } }
        public string MethodName { get { return GetUnicodeStringAt(SkipUnicodeString(24)); } }
        public string MethodSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(24))); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(24)))); return 0; } }

        #region Private
        internal MethodJittingStartedTraceData(Action<MethodJittingStartedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(24)))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(24)))+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(24)))+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("MethodID", MethodID);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("MethodToken", MethodToken);
             sb.XmlAttribHex("MethodILSize", MethodILSize);
             sb.XmlAttrib("MethodNamespace", MethodNamespace);
             sb.XmlAttrib("MethodName", MethodName);
             sb.XmlAttrib("MethodSignature", MethodSignature);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodID", "ModuleID", "MethodToken", "MethodILSize", "MethodNamespace", "MethodName", "MethodSignature", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodID;
                case 1:
                    return ModuleID;
                case 2:
                    return MethodToken;
                case 3:
                    return MethodILSize;
                case 4:
                    return MethodNamespace;
                case 5:
                    return MethodName;
                case 6:
                    return MethodSignature;
                case 7:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodJittingStartedTraceData> Action;
        #endregion
    }
    public sealed class ModuleLoadUnloadTraceData : TraceEvent
    {
        public long ModuleID { get { return GetInt64At(0); } }
        public long AssemblyID { get { return GetInt64At(8); } }
        public ModuleFlags ModuleFlags { get { return (ModuleFlags)GetInt32At(16); } }
        // Skipping Reserved1
        public string ModuleILPath { get { return GetUnicodeStringAt(24); } }
        public string ModuleNativePath { get { return GetUnicodeStringAt(SkipUnicodeString(24)); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(SkipUnicodeString(24))); return 0; } }

        #region Private
        internal ModuleLoadUnloadTraceData(Action<ModuleLoadUnloadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(24))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(SkipUnicodeString(24) + 2)));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(SkipUnicodeString(24) + 2)));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("AssemblyID", AssemblyID);
             sb.XmlAttrib("ModuleFlags", ModuleFlags);
             sb.XmlAttrib("ModuleILPath", ModuleILPath);
             sb.XmlAttrib("ModuleNativePath", ModuleNativePath);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ModuleID", "AssemblyID", "ModuleFlags", "ModuleILPath", "ModuleNativePath"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ModuleID;
                case 1:
                    return AssemblyID;
                case 2:
                    return ModuleFlags;
                case 3:
                    return ModuleILPath;
                case 4:
                    return ModuleNativePath;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ModuleLoadUnloadTraceData> Action;
        #endregion
    }
    public sealed class DomainModuleLoadUnloadTraceData : TraceEvent
    {
        public long ModuleID { get { return GetInt64At(0); } }
        public long AssemblyID { get { return GetInt64At(8); } }
        public long AppDomainID { get { return GetInt64At(16); } }
        public ModuleFlags ModuleFlags { get { return (ModuleFlags)GetInt32At(24); } }
        // Skipping Reserved1
        public string ModuleILPath { get { return GetUnicodeStringAt(32); } }
        public string ModuleNativePath { get { return GetUnicodeStringAt(SkipUnicodeString(32)); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(SkipUnicodeString(32))); return 0; } }

        #region Private
        internal DomainModuleLoadUnloadTraceData(Action<DomainModuleLoadUnloadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(32))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(SkipUnicodeString(32))+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(SkipUnicodeString(32))+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("ModuleID", ModuleID);
             sb.XmlAttribHex("AssemblyID", AssemblyID);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttrib("ModuleFlags", ModuleFlags);
             sb.XmlAttrib("ModuleILPath", ModuleILPath);
             sb.XmlAttrib("ModuleNativePath", ModuleNativePath);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ModuleID", "AssemblyID", "AppDomainID", "ModuleFlags", "ModuleILPath", "ModuleNativePath", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ModuleID;
                case 1:
                    return AssemblyID;
                case 2:
                    return AppDomainID;
                case 3:
                    return ModuleFlags;
                case 4:
                    return ModuleILPath;
                case 5:
                    return ModuleNativePath;
                case 6:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DomainModuleLoadUnloadTraceData> Action;
        #endregion
    }
    public sealed class AssemblyLoadUnloadTraceData : TraceEvent
    {
        public long AssemblyID { get { return GetInt64At(0); } }
        public long AppDomainID { get { return GetInt64At(8); } }
        public AssemblyFlags AssemblyFlags { get { if (Version >= 1) return (AssemblyFlags)GetInt32At(24); return (AssemblyFlags)GetInt32At(16); } }
        public string FullyQualifiedAssemblyName { get { if (Version >= 1) return GetUnicodeStringAt(28); return GetUnicodeStringAt(20); } }
        public long BindingID { get { if (Version >= 1) return GetInt64At(16); return 0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(28)); return 0; } }

        #region Private
        internal AssemblyLoadUnloadTraceData(Action<AssemblyLoadUnloadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(20)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(28)+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(28)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("AssemblyID", AssemblyID);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttrib("AssemblyFlags", AssemblyFlags);
             sb.XmlAttrib("FullyQualifiedAssemblyName", FullyQualifiedAssemblyName);
             sb.XmlAttribHex("BindingID", BindingID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AssemblyID", "AppDomainID", "AssemblyFlags", "FullyQualifiedAssemblyName", "BindingID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AssemblyID;
                case 1:
                    return AppDomainID;
                case 2:
                    return AssemblyFlags;
                case 3:
                    return FullyQualifiedAssemblyName;
                case 4:
                    return BindingID;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<AssemblyLoadUnloadTraceData> Action;
        #endregion
    }
    public sealed class AppDomainLoadUnloadTraceData : TraceEvent
    {
        public long AppDomainID { get { return GetInt64At(0); } }
        public AppDomainFlags AppDomainFlags { get { return (AppDomainFlags)GetInt32At(8); } }
        public string AppDomainName { get { return GetUnicodeStringAt(12); } }
        public int AppDomainIndex { get { if (Version >= 1) return GetInt32At(SkipUnicodeString(12)); return 0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(12)+4); return 0; } }

        #region Private
        internal AppDomainLoadUnloadTraceData(Action<AppDomainLoadUnloadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(12)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(12)+6));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(12)+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("AppDomainID", AppDomainID);
             sb.XmlAttrib("AppDomainFlags", AppDomainFlags);
             sb.XmlAttrib("AppDomainName", AppDomainName);
             sb.XmlAttrib("AppDomainIndex", AppDomainIndex);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AppDomainID", "AppDomainFlags", "AppDomainName", "AppDomainIndex", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AppDomainID;
                case 1:
                    return AppDomainFlags;
                case 2:
                    return AppDomainName;
                case 3:
                    return AppDomainIndex;
                case 4:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<AppDomainLoadUnloadTraceData> Action;
        #endregion
    }
    public sealed class StrongNameVerificationTraceData : TraceEvent
    {
        public int VerificationFlags { get { return GetInt32At(0); } }
        public int ErrorCode { get { return GetInt32At(4); } }
        public string FullyQualifiedAssemblyName { get { return GetUnicodeStringAt(8); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(8)); return 0; } }

        #region Private
        internal StrongNameVerificationTraceData(Action<StrongNameVerificationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(8)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(8)+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(8)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("VerificationFlags", VerificationFlags);
             sb.XmlAttribHex("ErrorCode", ErrorCode);
             sb.XmlAttrib("FullyQualifiedAssemblyName", FullyQualifiedAssemblyName);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "VerificationFlags", "ErrorCode", "FullyQualifiedAssemblyName", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return VerificationFlags;
                case 1:
                    return ErrorCode;
                case 2:
                    return FullyQualifiedAssemblyName;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<StrongNameVerificationTraceData> Action;
        #endregion
    }
    public sealed class AuthenticodeVerificationTraceData : TraceEvent
    {
        public int VerificationFlags { get { return GetInt32At(0); } }
        public int ErrorCode { get { return GetInt32At(4); } }
        public string ModulePath { get { return GetUnicodeStringAt(8); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipUnicodeString(8)); return 0; } }

        #region Private
        internal AuthenticodeVerificationTraceData(Action<AuthenticodeVerificationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(8)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(8)+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipUnicodeString(8)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("VerificationFlags", VerificationFlags);
             sb.XmlAttribHex("ErrorCode", ErrorCode);
             sb.XmlAttrib("ModulePath", ModulePath);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "VerificationFlags", "ErrorCode", "ModulePath", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return VerificationFlags;
                case 1:
                    return ErrorCode;
                case 2:
                    return ModulePath;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<AuthenticodeVerificationTraceData> Action;
        #endregion
    }
    public sealed class MethodJitInliningSucceededTraceData : TraceEvent
    {
        public string MethodBeingCompiledNamespace { get { return GetUnicodeStringAt(0); } }
        public string MethodBeingCompiledName { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public string MethodBeingCompiledNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(0))); } }
        public string InlinerNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))); } }
        public string InlinerName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))); } }
        public string InlinerNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))); } }
        public string InlineeNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))); } }
        public string InlineeName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))); } }
        public string InlineeNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))))); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))); } }

        #region Private
        internal MethodJitInliningSucceededTraceData(Action<MethodJitInliningSucceededTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+2));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("MethodBeingCompiledNamespace", MethodBeingCompiledNamespace);
             sb.XmlAttrib("MethodBeingCompiledName", MethodBeingCompiledName);
             sb.XmlAttrib("MethodBeingCompiledNameSignature", MethodBeingCompiledNameSignature);
             sb.XmlAttrib("InlinerNamespace", InlinerNamespace);
             sb.XmlAttrib("InlinerName", InlinerName);
             sb.XmlAttrib("InlinerNameSignature", InlinerNameSignature);
             sb.XmlAttrib("InlineeNamespace", InlineeNamespace);
             sb.XmlAttrib("InlineeName", InlineeName);
             sb.XmlAttrib("InlineeNameSignature", InlineeNameSignature);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodBeingCompiledNamespace", "MethodBeingCompiledName", "MethodBeingCompiledNameSignature", "InlinerNamespace", "InlinerName", "InlinerNameSignature", "InlineeNamespace", "InlineeName", "InlineeNameSignature", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodBeingCompiledNamespace;
                case 1:
                    return MethodBeingCompiledName;
                case 2:
                    return MethodBeingCompiledNameSignature;
                case 3:
                    return InlinerNamespace;
                case 4:
                    return InlinerName;
                case 5:
                    return InlinerNameSignature;
                case 6:
                    return InlineeNamespace;
                case 7:
                    return InlineeName;
                case 8:
                    return InlineeNameSignature;
                case 9:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodJitInliningSucceededTraceData> Action;
        #endregion
    }
    public sealed class MethodJitInliningFailedTraceData : TraceEvent
    {
        public string MethodBeingCompiledNamespace { get { return GetUnicodeStringAt(0); } }
        public string MethodBeingCompiledName { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public string MethodBeingCompiledNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(0))); } }
        public string InlinerNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))); } }
        public string InlinerName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))); } }
        public string InlinerNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))); } }
        public string InlineeNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))); } }
        public string InlineeName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))); } }
        public string InlineeNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))))); } }
        public bool FailAlways { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))) != 0; } }
        public string FailReason { get { return GetAsciiStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4); } }
        public int ClrInstanceID { get { return GetInt16At(SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)); } }

        #region Private
        internal MethodJitInliningFailedTraceData(Action<MethodJitInliningFailedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)+2));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("MethodBeingCompiledNamespace", MethodBeingCompiledNamespace);
             sb.XmlAttrib("MethodBeingCompiledName", MethodBeingCompiledName);
             sb.XmlAttrib("MethodBeingCompiledNameSignature", MethodBeingCompiledNameSignature);
             sb.XmlAttrib("InlinerNamespace", InlinerNamespace);
             sb.XmlAttrib("InlinerName", InlinerName);
             sb.XmlAttrib("InlinerNameSignature", InlinerNameSignature);
             sb.XmlAttrib("InlineeNamespace", InlineeNamespace);
             sb.XmlAttrib("InlineeName", InlineeName);
             sb.XmlAttrib("InlineeNameSignature", InlineeNameSignature);
             sb.XmlAttrib("FailAlways", FailAlways);
             sb.XmlAttrib("FailReason", FailReason);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodBeingCompiledNamespace", "MethodBeingCompiledName", "MethodBeingCompiledNameSignature", "InlinerNamespace", "InlinerName", "InlinerNameSignature", "InlineeNamespace", "InlineeName", "InlineeNameSignature", "FailAlways", "FailReason", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodBeingCompiledNamespace;
                case 1:
                    return MethodBeingCompiledName;
                case 2:
                    return MethodBeingCompiledNameSignature;
                case 3:
                    return InlinerNamespace;
                case 4:
                    return InlinerName;
                case 5:
                    return InlinerNameSignature;
                case 6:
                    return InlineeNamespace;
                case 7:
                    return InlineeName;
                case 8:
                    return InlineeNameSignature;
                case 9:
                    return FailAlways;
                case 10:
                    return FailReason;
                case 11:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodJitInliningFailedTraceData> Action;
        #endregion
    }
    public sealed class RuntimeInformationTraceData : TraceEvent
    {
        public int ClrInstanceID { get { return GetInt16At(0); } }
        public RuntimeSku Sku { get { return (RuntimeSku)GetInt16At(2); } }
        public int BclMajorVersion { get { return GetInt16At(4); } }
        public int BclMinorVersion { get { return GetInt16At(6); } }
        public int BclBuildNumber { get { return GetInt16At(8); } }
        public int BclQfeNumber { get { return GetInt16At(10); } }
        public int VMMajorVersion { get { return GetInt16At(12); } }
        public int VMMinorVersion { get { return GetInt16At(14); } }
        public int VMBuildNumber { get { return GetInt16At(16); } }
        public int VMQfeNumber { get { return GetInt16At(18); } }
        public StartupFlags StartupFlags { get { return (StartupFlags)GetInt32At(20); } }
        public StartupMode StartupMode { get { return (StartupMode)GetByteAt(24); } }
        public string CommandLine { get { return GetUnicodeStringAt(25); } }
        public Guid ComObjectGuid { get { return GetGuidAt(SkipUnicodeString(25)); } }
        public string RuntimeDllPath { get { return GetUnicodeStringAt(SkipUnicodeString(25)+16); } }

        #region Private
        internal RuntimeInformationTraceData(Action<RuntimeInformationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(25)+16)));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(25)+16)));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.XmlAttrib("Sku", Sku);
             sb.XmlAttrib("BclMajorVersion", BclMajorVersion);
             sb.XmlAttrib("BclMinorVersion", BclMinorVersion);
             sb.XmlAttrib("BclBuildNumber", BclBuildNumber);
             sb.XmlAttrib("BclQfeNumber", BclQfeNumber);
             sb.XmlAttrib("VMMajorVersion", VMMajorVersion);
             sb.XmlAttrib("VMMinorVersion", VMMinorVersion);
             sb.XmlAttrib("VMBuildNumber", VMBuildNumber);
             sb.XmlAttrib("VMQfeNumber", VMQfeNumber);
             sb.XmlAttrib("StartupFlags", StartupFlags);
             sb.XmlAttrib("StartupMode", StartupMode);
             sb.XmlAttrib("CommandLine", CommandLine);
             sb.XmlAttrib("ComObjectGuid", ComObjectGuid);
             sb.XmlAttrib("RuntimeDllPath", RuntimeDllPath);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID", "Sku", "BclMajorVersion", "BclMinorVersion", "BclBuildNumber", "BclQfeNumber", "VMMajorVersion", "VMMinorVersion", "VMBuildNumber", "VMQfeNumber", "StartupFlags", "StartupMode", "CommandLine", "ComObjectGuid", "RuntimeDllPath"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                case 1:
                    return Sku;
                case 2:
                    return BclMajorVersion;
                case 3:
                    return BclMinorVersion;
                case 4:
                    return BclBuildNumber;
                case 5:
                    return BclQfeNumber;
                case 6:
                    return VMMajorVersion;
                case 7:
                    return VMMinorVersion;
                case 8:
                    return VMBuildNumber;
                case 9:
                    return VMQfeNumber;
                case 10:
                    return StartupFlags;
                case 11:
                    return StartupMode;
                case 12:
                    return CommandLine;
                case 13:
                    return ComObjectGuid;
                case 14:
                    return RuntimeDllPath;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<RuntimeInformationTraceData> Action;
        #endregion
    }
    public sealed class MethodJitTailCallSucceededTraceData : TraceEvent
    {
        public string MethodBeingCompiledNamespace { get { return GetUnicodeStringAt(0); } }
        public string MethodBeingCompiledName { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public string MethodBeingCompiledNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(0))); } }
        public string CallerNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))); } }
        public string CallerName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))); } }
        public string CallerNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))); } }
        public string CalleeNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))); } }
        public string CalleeName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))); } }
        public string CalleeNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))))); } }
        public bool TailPrefix { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))) != 0; } }
        public TailCallType TailCallType { get { return (TailCallType)GetInt32At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+8); } }

        #region Private
        internal MethodJitTailCallSucceededTraceData(Action<MethodJitTailCallSucceededTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+10));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("MethodBeingCompiledNamespace", MethodBeingCompiledNamespace);
             sb.XmlAttrib("MethodBeingCompiledName", MethodBeingCompiledName);
             sb.XmlAttrib("MethodBeingCompiledNameSignature", MethodBeingCompiledNameSignature);
             sb.XmlAttrib("CallerNamespace", CallerNamespace);
             sb.XmlAttrib("CallerName", CallerName);
             sb.XmlAttrib("CallerNameSignature", CallerNameSignature);
             sb.XmlAttrib("CalleeNamespace", CalleeNamespace);
             sb.XmlAttrib("CalleeName", CalleeName);
             sb.XmlAttrib("CalleeNameSignature", CalleeNameSignature);
             sb.XmlAttrib("TailPrefix", TailPrefix);
             sb.XmlAttrib("TailCallType", TailCallType);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodBeingCompiledNamespace", "MethodBeingCompiledName", "MethodBeingCompiledNameSignature", "CallerNamespace", "CallerName", "CallerNameSignature", "CalleeNamespace", "CalleeName", "CalleeNameSignature", "TailPrefix", "TailCallType", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodBeingCompiledNamespace;
                case 1:
                    return MethodBeingCompiledName;
                case 2:
                    return MethodBeingCompiledNameSignature;
                case 3:
                    return CallerNamespace;
                case 4:
                    return CallerName;
                case 5:
                    return CallerNameSignature;
                case 6:
                    return CalleeNamespace;
                case 7:
                    return CalleeName;
                case 8:
                    return CalleeNameSignature;
                case 9:
                    return TailPrefix;
                case 10:
                    return TailCallType;
                case 11:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodJitTailCallSucceededTraceData> Action;
        #endregion
    }
    public sealed class MethodJitTailCallFailedTraceData : TraceEvent
    {
        public string MethodBeingCompiledNamespace { get { return GetUnicodeStringAt(0); } }
        public string MethodBeingCompiledName { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public string MethodBeingCompiledNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(0))); } }
        public string CallerNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))); } }
        public string CallerName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))); } }
        public string CallerNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))); } }
        public string CalleeNamespace { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))); } }
        public string CalleeName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))); } }
        public string CalleeNameSignature { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0))))))))); } }
        public bool TailPrefix { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))) != 0; } }
        public string FailReason { get { return GetAsciiStringAt(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4); } }
        public int ClrInstanceID { get { return GetInt16At(SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)); } }

        #region Private
        internal MethodJitTailCallFailedTraceData(Action<MethodJitTailCallFailedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)+2));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipAsciiString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(0)))))))))+4)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("MethodBeingCompiledNamespace", MethodBeingCompiledNamespace);
             sb.XmlAttrib("MethodBeingCompiledName", MethodBeingCompiledName);
             sb.XmlAttrib("MethodBeingCompiledNameSignature", MethodBeingCompiledNameSignature);
             sb.XmlAttrib("CallerNamespace", CallerNamespace);
             sb.XmlAttrib("CallerName", CallerName);
             sb.XmlAttrib("CallerNameSignature", CallerNameSignature);
             sb.XmlAttrib("CalleeNamespace", CalleeNamespace);
             sb.XmlAttrib("CalleeName", CalleeName);
             sb.XmlAttrib("CalleeNameSignature", CalleeNameSignature);
             sb.XmlAttrib("TailPrefix", TailPrefix);
             sb.XmlAttrib("FailReason", FailReason);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MethodBeingCompiledNamespace", "MethodBeingCompiledName", "MethodBeingCompiledNameSignature", "CallerNamespace", "CallerName", "CallerNameSignature", "CalleeNamespace", "CalleeName", "CalleeNameSignature", "TailPrefix", "FailReason", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MethodBeingCompiledNamespace;
                case 1:
                    return MethodBeingCompiledName;
                case 2:
                    return MethodBeingCompiledNameSignature;
                case 3:
                    return CallerNamespace;
                case 4:
                    return CallerName;
                case 5:
                    return CallerNameSignature;
                case 6:
                    return CalleeNamespace;
                case 7:
                    return CalleeName;
                case 8:
                    return CalleeNameSignature;
                case 9:
                    return TailPrefix;
                case 10:
                    return FailReason;
                case 11:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodJitTailCallFailedTraceData> Action;
        #endregion
    }

        [Flags]
        public enum AppDomainFlags
        {
            None = 0,
            Default = 0x1,
            Executable = 0x2,
            Shared = 0x4,
        }
        [Flags]
        public enum AssemblyFlags
        {
            None = 0,
            DomainNeutral = 0x1,
            Dynamic = 0x2,
            Native = 0x4,
            Collectible = 0x8,
        }
        [Flags]
        public enum ModuleFlags
        {
            None = 0,
            DomainNeutral = 0x1,
            Native = 0x2,
            Dynamic = 0x4,
            Manifest = 0x8,
        }
        [Flags]
        public enum MethodFlags
        {
            None = 0,
            Dynamic = 0x1,
            Generic = 0x2,
            HasSharedGenericCode = 0x4,
            Jitted = 0x8,
        }
        [Flags]
        public enum StartupMode
        {
            None = 0,
            ManagedExe = 0x1,
            HostedClr = 0x2,
            IjwDll = 0x4,
            ComActivated = 0x8,
            Other = 0x10,
        }
        [Flags]
        public enum RuntimeSku
        {
            None = 0,
            DesktopClr = 0x1,
            CoreClr = 0x2,
        }
        [Flags]
        public enum ExceptionThrownFlags
        {
            None = 0,
            HasInnerException = 0x1,
            Nested = 0x2,
            ReThrown = 0x4,
            CorruptedState = 0x8,
            CLSCompliant = 0x10,
        }
        [Flags]
        public enum ILStubGeneratedFlags
        {
            None = 0,
            ReverseInterop = 0x1,
            ComInterop = 0x2,
            NGenedStub = 0x4,
            Delegate = 0x8,
            VarArg = 0x10,
            UnmanagedCallee = 0x20,
        }
        [Flags]
        public enum StartupFlags
        {
            None = 0,
            CONCURRENT_GC = 0x000001,
            LOADER_OPTIMIZATION_SINGLE_DOMAIN = 0x000002,
            LOADER_OPTIMIZATION_MULTI_DOMAIN = 0x000004,
            LOADER_SAFEMODE = 0x000010,
            LOADER_SETPREFERENCE = 0x000100,
            SERVER_GC = 0x001000,
            HOARD_GC_VM = 0x002000,
            SINGLE_VERSION_HOSTING_INTERFACE = 0x004000,
            LEGACY_IMPERSONATION = 0x010000,
            DISABLE_COMMITTHREADSTACK = 0x020000,
            ALWAYSFLOW_IMPERSONATION = 0x040000,
            TRIM_GC_COMMIT = 0x080000,
            ETW = 0x100000,
            SERVER_BUILD = 0x200000,
            ARM = 0x400000,
        }
        public enum GCSegmentType
        {
            SmallObjectHeap = 0x0,
            LargeObjectHeap = 0x1,
            ReadOnlyHeap = 0x2,
        }
        public enum GCAllocationKind
        {
            Small = 0x0,
            Large = 0x1,
        }
        public enum GCType
        {
            NonConcurrentGC = 0x0,
            BackgroundGC = 0x1,
            ForegroundGC = 0x2,
        }
        public enum GCReason
        {
            AllocSmall = 0x0,
            Induced = 0x1,
            LowMemory = 0x2,
            Empty = 0x3,
            AllocLarge = 0x4,
            OutOfSpaceSmallObjectHeap = 0x5,
            OutOfSpaceLargeObjectHeap = 0x6,
        }
        public enum ContentionFlags
        {
            Managed = 0x0,
            Native = 0x1,
        }
        public enum TailCallType
        {
            OptimizedTailCall = 0x0,
            RecursiveLoop = 0x1,
            HelperAssistedTailCall = 0x2,
        }
        public enum ThreadAdjustmentReason
        {
            Warmup = 0x0,
            Initializing = 0x1,
            RandomMove = 0x2,
            ClimbingMove = 0x3,
            ChangePoint = 0x4,
            Stabilizing = 0x5,
            Starvation = 0x6,
            ThreadTimedOut = 0x7,
        }

        [SecuritySafeCritical, SecurityCritical]
        [CLSCompliant(false)]
    public sealed class ClrRundownTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "Microsoft-Windows-DotNETRuntimeRundown";
        public static Guid ProviderGuid = new Guid(unchecked((int) 0xa669021c), unchecked((short) 0xc450), unchecked((short) 0x4609), 0xa0, 0x35, 0x5a, 0xf5, 0x9a, 0xf4, 0xdf, 0x18);
        public enum Keywords : long
        {
            Loader = 0x8,
            Jit = 0x10,
            NGen = 0x20,
            Start = 0x40,
            End = 0x100,
            AppDomainResourceManagement = 0x800,
            Stack = 0x40000000,
        };
                
        public ClrRundownTraceEventParser(TraceEventSource source) : base(source) { }

        public event Action<ClrStackWalkTraceData> ClrStackWalk
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrStackWalkTraceData(value, 0, 11, "ClrStack", ClrStackTaskGuid, 82, "Walk", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodDCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 141, 1, "Method", MethodTaskGuid, 35, "DCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadTraceData> MethodDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadTraceData(value, 142, 1, "Method", MethodTaskGuid, 36, "DCEnd", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodDCStartVerbose
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 143, 1, "Method", MethodTaskGuid, 39, "DCStartVerbose", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodLoadUnloadVerboseTraceData> MethodDCStopVerbose
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodLoadUnloadVerboseTraceData(value, 144, 1, "Method", MethodTaskGuid, 40, "DCStopVerbose", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DCStartEndTraceData> MethodDCStartComplete
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DCStartEndTraceData(value, 145, 1, "Method", MethodTaskGuid, 14, "DCStartComplete", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DCStartEndTraceData> MethodDCStopComplete
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DCStartEndTraceData(value, 146, 1, "Method", MethodTaskGuid, 15, "DCStopComplete", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DCStartEndTraceData> MethodDCStartInit
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DCStartEndTraceData(value, 147, 1, "Method", MethodTaskGuid, 16, "DCStartInit", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DCStartEndTraceData> MethodDCStopInit
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DCStartEndTraceData(value, 148, 1, "Method", MethodTaskGuid, 17, "DCStopInit", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DomainModuleLoadUnloadTraceData> LoaderDomainModuleDCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DomainModuleLoadUnloadTraceData(value, 151, 2, "Loader", LoaderTaskGuid, 46, "DomainModuleDCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DomainModuleLoadUnloadTraceData> LoaderDomainModuleDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DomainModuleLoadUnloadTraceData(value, 152, 2, "Loader", LoaderTaskGuid, 47, "DomainModuleDCStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> ModuleDCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 153, 2, "Loader", LoaderTaskGuid, 35, "ModuleDCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleLoadUnloadTraceData> LoaderModuleDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleLoadUnloadTraceData(value, 154, 2, "Loader", LoaderTaskGuid, 36, "ModuleDCStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AssemblyLoadUnloadTraceData> AssemblyDCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AssemblyLoadUnloadTraceData(value, 155, 2, "Loader", LoaderTaskGuid, 39, "AssemblyDCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AssemblyLoadUnloadTraceData> LoaderAssemblyDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AssemblyLoadUnloadTraceData(value, 156, 2, "Loader", LoaderTaskGuid, 40, "AssemblyDCStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainLoadUnloadTraceData> AppDomainDCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainLoadUnloadTraceData(value, 157, 2, "Loader", LoaderTaskGuid, 43, "AppDomainDCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<AppDomainLoadUnloadTraceData> LoaderAppDomainDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new AppDomainLoadUnloadTraceData(value, 158, 2, "Loader", LoaderTaskGuid, 44, "AppDomainDCStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadCreatedTraceData> LoaderThreadDCStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadCreatedTraceData(value, 159, 2, "Loader", LoaderTaskGuid, 48, "ThreadDCStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RuntimeInformationTraceData> RuntimeStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RuntimeInformationTraceData(value, 187, 19, "Runtime", RuntimeTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

       #region Event ID Definitions
        public const TraceEventID ClrStackWalkEventID = (TraceEventID) 0;
        public const TraceEventID MethodDCStartEventID = (TraceEventID) 141;
        public const TraceEventID MethodDCStopEventID = (TraceEventID) 142;
        public const TraceEventID MethodDCStartVerboseEventID = (TraceEventID) 143;
        public const TraceEventID MethodDCStopVerboseEventID = (TraceEventID) 144;
        public const TraceEventID MethodDCStartCompleteEventID = (TraceEventID) 145;
        public const TraceEventID MethodDCStopCompleteEventID = (TraceEventID) 146;
        public const TraceEventID MethodDCStartInitEventID = (TraceEventID) 147;
        public const TraceEventID MethodDCStopInitEventID = (TraceEventID) 148;
        public const TraceEventID LoaderDomainModuleDCStartEventID = (TraceEventID) 151;
        public const TraceEventID LoaderDomainModuleDCStopEventID = (TraceEventID) 152;
        public const TraceEventID LoaderModuleDCStartEventID = (TraceEventID) 153;
        public const TraceEventID LoaderModuleDCStopEventID = (TraceEventID) 154;
        public const TraceEventID LoaderAssemblyDCStartEventID = (TraceEventID) 155;
        public const TraceEventID LoaderAssemblyDCStopEventID = (TraceEventID) 156;
        public const TraceEventID LoaderAppDomainDCStartEventID = (TraceEventID) 157;
        public const TraceEventID LoaderAppDomainDCStopEventID = (TraceEventID) 158;
        public const TraceEventID LoaderThreadDCStopEventID = (TraceEventID) 159;
        public const TraceEventID RuntimeStartEventID = (TraceEventID) 187;
       #endregion

    public sealed class DCStartEndTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal DCStartEndTraceData(Action<DCStartEndTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DCStartEndTraceData> Action;
        #endregion
    }
        #region private
        private static Guid MethodTaskGuid = new Guid(unchecked((int)0x0bcd91db), unchecked((short)0xf943), unchecked((short)0x454a), 0xa6, 0x62, 0x6e, 0xdb, 0xcf, 0xbb, 0x76, 0xd2);
        private static Guid LoaderTaskGuid = new Guid(unchecked((int)0x5a54f4df), unchecked((short)0xd302), unchecked((short)0x4fee), 0xa2, 0x11, 0x6c, 0x2c, 0x0c, 0x1d, 0xcb, 0x1a);
        private static Guid ClrStackTaskGuid = new Guid(unchecked((int)0xd3363dc0), unchecked((short)0x243a), unchecked((short)0x4620), 0xa4, 0xd0, 0x8a, 0x07, 0xd7, 0x72, 0xf5, 0x33);
        private static Guid RuntimeTaskGuid = new Guid(unchecked((int)0xcd7d3e32), unchecked((short)0x65fe), unchecked((short)0x40cd), 0x92, 0x25, 0xa2, 0x57, 0x7d, 0x20, 0x3f, 0xc3);
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ClrStressTraceEventParser : TraceEventParser
        {
        public static string ProviderName = "Microsoft-Windows-DotNETRuntimeStress";
        public static Guid ProviderGuid = new Guid(unchecked((int) 0xcc2bcbba), unchecked((short) 0x16b6), unchecked((short) 0x4cf3), 0x89, 0x90, 0xd7, 0x4c, 0x2e, 0x8a, 0xf5, 0x00);
        public enum Keywords : long
        {
            Stack = 0x40000000,
        };

        public ClrStressTraceEventParser(TraceEventSource source) : base(source) { }

        public event Action<StressLogTraceData> StressLogStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StressLogTraceData(value, 0, 1, "StressLog", StressLogTaskGuid, 1, "Start", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ClrStackWalkTraceData> ClrStackWalk
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrStackWalkTraceData(value, 1, 11, "ClrStack", ClrStackTaskGuid, 82, "Walk", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

       #region Event ID Definitions
        public const TraceEventID StressLogStartEventID = (TraceEventID) 0;
        public const TraceEventID ClrStackWalkEventID = (TraceEventID) 1;
       #endregion

    #region private
        private static Guid StressLogTaskGuid = new Guid(unchecked((int) 0xea40c74d), unchecked((short) 0x4f65), unchecked((short) 0x4561), 0xbb, 0x26, 0x65, 0x62, 0x31, 0xc8, 0x96, 0x7f);
        private static Guid ClrStackTaskGuid = new Guid(unchecked((int) 0xd3363dc0), unchecked((short) 0x243a), unchecked((short) 0x4620), 0xa4, 0xd0, 0x8a, 0x07, 0xd7, 0x72, 0xf5, 0x33);
    #endregion
    }

    public sealed class StressLogTraceData : TraceEvent
    {
        public int Facility { get { return GetInt32At(0); } }
        public int LogLevel { get { return GetByteAt(4); } }
        public string Message { get { return GetAsciiStringAt(5); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(SkipAsciiString(5)); return 0; } }

        #region Private
        internal StressLogTraceData(Action<StressLogTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipAsciiString(5)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipAsciiString(5)+2));
            Debug.Assert(!(Version > 1 && EventDataLength < SkipAsciiString(5)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("Facility", Facility);
             sb.XmlAttrib("LogLevel", LogLevel);
             sb.XmlAttrib("Message", Message);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Facility", "LogLevel", "Message", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Facility;
                case 1:
                    return LogLevel;
                case 2:
                    return Message;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<StressLogTraceData> Action;
        #endregion
    }

    #region PRIVATE_CLR_PROVIDER
#if !ONLY_PUBLIC_CLR

    // #ClrPrivateProvider
    [SecuritySafeCritical, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class ClrPrivateTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "Microsoft-Windows-DotNETRuntimePrivate";
        public static Guid ProviderGuid = new Guid(unchecked((int) 0x763fd754), unchecked((short) 0x7086), unchecked((short) 0x4dfe), 0x95, 0xeb, 0xc0, 0x1a, 0x46, 0xfa, 0xf4, 0xca);
        public enum Keywords : long
        {
            GC = 0x00000001,
            Binding = 0x00000002,
            NGenForceRestore = 0x00000004,
            Fusion = 0x00000008,
            Security = 0x00000400,
            Stack = 0x40000000,
            Startup = 0x80000000,
        };

        public ClrPrivateTraceEventParser(TraceEventSource source) : base(source) { }

        public event Action<GCDecisionTraceData> GCDecision
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCDecisionTraceData(value, 1, 1, "GC", GCTaskGuid, 132, "Decision", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCSettingsTraceData> GCSettings
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCSettingsTraceData(value, 2, 1, "GC", GCTaskGuid, 14, "Settings", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCOptimizedTraceData> GCOptimized
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCOptimizedTraceData(value, 3, 1, "GC", GCTaskGuid, 16, "Optimized", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCPerHeapHistoryTraceData> GCPerHeapHistory
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCPerHeapHistoryTraceData(value, 4, 1, "GC", GCTaskGuid, 17, "PerHeapHistory", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCGlobalHeapTraceData> GCGlobalHeapHistory
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCGlobalHeapTraceData(value, 5, 1, "GC", GCTaskGuid, 18, "GlobalHeapHistory", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCJoinTraceData> GCJoin
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCJoinTraceData(value, 6, 1, "GC", GCTaskGuid, 20, "Join", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCMarkTraceData> GCMarkStackRoots
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCMarkTraceData(value, 7, 1, "GC", GCTaskGuid, 21, "MarkStackRoots", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCMarkTraceData> GCMarkFinalizeQueueRoots
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCMarkTraceData(value, 8, 1, "GC", GCTaskGuid, 22, "MarkFinalizeQueueRoots", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCMarkTraceData> GCMarkHandles
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCMarkTraceData(value, 9, 1, "GC", GCTaskGuid, 23, "MarkHandles", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCMarkTraceData> GCMarkCards
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCMarkTraceData(value, 10, 1, "GC", GCTaskGuid, 24, "MarkCards", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGCStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 11, 1, "GC", GCTaskGuid, 25, "BGCStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC1stNonCondStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 12, 1, "GC", GCTaskGuid, 26, "BGC1stNonCondStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC1stConStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 13, 1, "GC", GCTaskGuid, 27, "BGC1stConStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC2ndNonConStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 14, 1, "GC", GCTaskGuid, 28, "BGC2ndNonConStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC2ndNonConStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 15, 1, "GC", GCTaskGuid, 29, "BGC2ndNonConStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC2ndConStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 16, 1, "GC", GCTaskGuid, 30, "BGC2ndConStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGC2ndConStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 17, 1, "GC", GCTaskGuid, 31, "BGC2ndConStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGCPlanStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 18, 1, "GC", GCTaskGuid, 32, "BGCPlanStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCNoUserDataTraceData> GCBGCSweepStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCNoUserDataTraceData(value, 19, 1, "GC", GCTaskGuid, 33, "BGCSweepStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BGCDrainMarkTraceData> GCBGCDrainMark
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BGCDrainMarkTraceData(value, 20, 1, "GC", GCTaskGuid, 34, "BGCDrainMark", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BGCRevisitTraceData> GCBGCRevisit
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BGCRevisitTraceData(value, 21, 1, "GC", GCTaskGuid, 35, "BGCRevisit", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BGCOverflowTraceData> GCBGCOverflow
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BGCOverflowTraceData(value, 22, 1, "GC", GCTaskGuid, 36, "BGCOverflow", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BGCAllocWaitTraceData> GCBGCAllocWaitStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BGCAllocWaitTraceData(value, 23, 1, "GC", GCTaskGuid, 37, "BGCAllocWaitStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BGCAllocWaitTraceData> GCBGCAllocWaitStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BGCAllocWaitTraceData(value, 24, 1, "GC", GCTaskGuid, 38, "BGCAllocWaitStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<GCFullNotifyTraceData> GCFullNotify
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new GCFullNotifyTraceData(value, 25, 1, "GC", GCTaskGuid, 19, "FullNotify", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEStartupStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 80, 9, "Startup", StartupTaskGuid, 128, "EEStartupStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEStartupStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 81, 9, "Startup", StartupTaskGuid, 129, "EEStartupStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEConfigSetupStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 82, 9, "Startup", StartupTaskGuid, 130, "EEConfigSetupStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEConfigSetupStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 83, 9, "Startup", StartupTaskGuid, 131, "EEConfigSetupStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLoadSystemBasesStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 84, 9, "Startup", StartupTaskGuid, 132, "LoadSystemBasesStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLoadSystemBasesStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 85, 9, "Startup", StartupTaskGuid, 133, "LoadSystemBasesStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupExecExeStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 86, 9, "Startup", StartupTaskGuid, 134, "ExecExeStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupExecExeStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 87, 9, "Startup", StartupTaskGuid, 135, "ExecExeStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupMainStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 88, 9, "Startup", StartupTaskGuid, 136, "MainStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupMainStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 89, 9, "Startup", StartupTaskGuid, 137, "MainStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupApplyPolicyStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 90, 9, "Startup", StartupTaskGuid, 10, "ApplyPolicyStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupApplyPolicyStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 91, 9, "Startup", StartupTaskGuid, 11, "ApplyPolicyStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLdLibShFolderStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 92, 9, "Startup", StartupTaskGuid, 12, "LdLibShFolderStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLdLibShFolderStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 93, 9, "Startup", StartupTaskGuid, 13, "LdLibShFolderStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupPrestubWorkerStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 94, 9, "Startup", StartupTaskGuid, 14, "PrestubWorkerStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupPrestubWorkerStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 95, 9, "Startup", StartupTaskGuid, 15, "PrestubWorkerStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupGetInstallationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 96, 9, "Startup", StartupTaskGuid, 16, "GetInstallationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupGetInstallationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 97, 9, "Startup", StartupTaskGuid, 17, "GetInstallationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupOpenHModuleStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 98, 9, "Startup", StartupTaskGuid, 18, "OpenHModuleStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupOpenHModuleStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 99, 9, "Startup", StartupTaskGuid, 19, "OpenHModuleStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupExplicitBindStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 100, 9, "Startup", StartupTaskGuid, 20, "ExplicitBindStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupExplicitBindStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 101, 9, "Startup", StartupTaskGuid, 21, "ExplicitBindStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupParseXmlStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 102, 9, "Startup", StartupTaskGuid, 22, "ParseXmlStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupParseXmlStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 103, 9, "Startup", StartupTaskGuid, 23, "ParseXmlStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupInitDefaultDomainStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 104, 9, "Startup", StartupTaskGuid, 24, "InitDefaultDomainStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupInitDefaultDomainStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 105, 9, "Startup", StartupTaskGuid, 25, "InitDefaultDomainStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupInitSecurityStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 106, 9, "Startup", StartupTaskGuid, 26, "InitSecurityStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupInitSecurityStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 107, 9, "Startup", StartupTaskGuid, 27, "InitSecurityStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupAllowBindingRedirsStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 108, 9, "Startup", StartupTaskGuid, 28, "AllowBindingRedirsStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupAllowBindingRedirsStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 109, 9, "Startup", StartupTaskGuid, 29, "AllowBindingRedirsStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEConfigSyncStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 110, 9, "Startup", StartupTaskGuid, 30, "EEConfigSyncStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupEEConfigSyncStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 111, 9, "Startup", StartupTaskGuid, 31, "EEConfigSyncStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupBindingStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 112, 9, "Startup", StartupTaskGuid, 32, "BindingStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupBindingStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 113, 9, "Startup", StartupTaskGuid, 33, "BindingStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLoaderCatchCallStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 114, 9, "Startup", StartupTaskGuid, 34, "LoaderCatchCallStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupLoaderCatchCallStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 115, 9, "Startup", StartupTaskGuid, 35, "LoaderCatchCallStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusionInitStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 116, 9, "Startup", StartupTaskGuid, 36, "FusionInitStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusionInitStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 117, 9, "Startup", StartupTaskGuid, 37, "FusionInitStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusionAppCtxStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 118, 9, "Startup", StartupTaskGuid, 38, "FusionAppCtxStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusionAppCtxStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 119, 9, "Startup", StartupTaskGuid, 39, "FusionAppCtxStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusion2EEStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 120, 9, "Startup", StartupTaskGuid, 40, "Fusion2EEStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupFusion2EEStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 121, 9, "Startup", StartupTaskGuid, 41, "Fusion2EEStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupSecurityCatchCallStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 122, 9, "Startup", StartupTaskGuid, 42, "SecurityCatchCallStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StartupTraceData> StartupSecurityCatchCallStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StartupTraceData(value, 123, 9, "Startup", StartupTaskGuid, 43, "SecurityCatchCallStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ClrStackWalkTraceData> ClrStackWalk
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ClrStackWalkTraceData(value, 151, 11, "ClrStack", ClrStackTaskGuid, 82, "Walk", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingPolicyPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 159, 10, "Binding", BindingTaskGuid, 51, "PolicyPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingPolicyPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 160, 10, "Binding", BindingTaskGuid, 52, "PolicyPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingNgenPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 161, 10, "Binding", BindingTaskGuid, 53, "NgenPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingNgenPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 162, 10, "Binding", BindingTaskGuid, 54, "NgenPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoopupAndProbingPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 163, 10, "Binding", BindingTaskGuid, 55, "LoopupAndProbingPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLookupAndProbingPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 164, 10, "Binding", BindingTaskGuid, 56, "LookupAndProbingPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 165, 10, "Binding", BindingTaskGuid, 57, "LoaderPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 166, 10, "Binding", BindingTaskGuid, 58, "LoaderPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 167, 10, "Binding", BindingTaskGuid, 59, "PhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 168, 10, "Binding", BindingTaskGuid, 60, "PhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingDownloadPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 169, 10, "Binding", BindingTaskGuid, 61, "DownloadPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingDownloadPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 170, 10, "Binding", BindingTaskGuid, 62, "DownloadPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderAssemblyInitPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 171, 10, "Binding", BindingTaskGuid, 63, "LoaderAssemblyInitPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderAssemblyInitPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 172, 10, "Binding", BindingTaskGuid, 64, "LoaderAssemblyInitPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderMappingPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 173, 10, "Binding", BindingTaskGuid, 65, "LoaderMappingPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderMappingPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 174, 10, "Binding", BindingTaskGuid, 66, "LoaderMappingPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderDeliverEventPhaseStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 175, 10, "Binding", BindingTaskGuid, 67, "LoaderDeliverEventPhaseStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BindingTraceData> BindingLoaderDeliverEventsPhaseStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BindingTraceData(value, 176, 10, "Binding", BindingTaskGuid, 68, "LoaderDeliverEventsPhaseStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EvidenceGeneratedTraceData> EvidenceGenerationEvidenceGenerated
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EvidenceGeneratedTraceData(value, 177, 12, "EvidenceGeneration", EvidenceGenerationTaskGuid, 10, "EvidenceGenerated", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleTransparencyCalculationTraceData> TransparencyModuleTransparencyComputationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleTransparencyCalculationTraceData(value, 178, 14, "Transparency", TransparencyTaskGuid, 83, "ModuleTransparencyComputationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ModuleTransparencyCalculationResultTraceData> TransparencyModuleTransparencyComputationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ModuleTransparencyCalculationResultTraceData(value, 179, 14, "Transparency", TransparencyTaskGuid, 84, "ModuleTransparencyComputationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TypeTransparencyCalculationTraceData> TransparencyTypeTransparencyComputationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TypeTransparencyCalculationTraceData(value, 180, 14, "Transparency", TransparencyTaskGuid, 85, "TypeTransparencyComputationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TypeTransparencyCalculationResultTraceData> TransparencyTypeTransparencyComputationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TypeTransparencyCalculationResultTraceData(value, 181, 14, "Transparency", TransparencyTaskGuid, 86, "TypeTransparencyComputationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodTransparencyCalculationTraceData> TransparencyMethodTransparencyComputationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodTransparencyCalculationTraceData(value, 182, 14, "Transparency", TransparencyTaskGuid, 87, "MethodTransparencyComputationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<MethodTransparencyCalculationResultTraceData> TransparencyMethodTransparencyComputationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new MethodTransparencyCalculationResultTraceData(value, 183, 14, "Transparency", TransparencyTaskGuid, 88, "MethodTransparencyComputationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FieldTransparencyCalculationTraceData> TransparencyFieldTransparencyComputationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FieldTransparencyCalculationTraceData(value, 184, 14, "Transparency", TransparencyTaskGuid, 89, "FieldTransparencyComputationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FieldTransparencyCalculationResultTraceData> TransparencyFieldTransparencyComputationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FieldTransparencyCalculationResultTraceData(value, 185, 14, "Transparency", TransparencyTaskGuid, 90, "FieldTransparencyComputationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TokenTransparencyCalculationTraceData> TransparencyTokenTransparencyComputationStart
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TokenTransparencyCalculationTraceData(value, 186, 14, "Transparency", TransparencyTaskGuid, 91, "TokenTransparencyComputationStart", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TokenTransparencyCalculationResultTraceData> TransparencyTokenTransparencyComputationStop
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TokenTransparencyCalculationResultTraceData(value, 187, 14, "Transparency", TransparencyTaskGuid, 92, "TokenTransparencyComputationStop", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<NgenBindEventTraceData> NgenBinderNgenBind
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new NgenBindEventTraceData(value, 188, 13, "NgenBinder", NgenBinderTaskGuid, 69, "NgenBind", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FailFastTraceData> FailFastFailFast
        {
            add
            {
                                                         // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FailFastTraceData(value, 191, 2, "FailFast", FailFastTaskGuid, 52, "FailFast", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

       #region Event ID Definitions
        public const TraceEventID GCDecisionEventID = (TraceEventID) 1;
        public const TraceEventID GCSettingsEventID = (TraceEventID) 2;
        public const TraceEventID GCOptimizedEventID = (TraceEventID) 3;
        public const TraceEventID GCPerHeapHistoryEventID = (TraceEventID) 4;
        public const TraceEventID GCGlobalHeapHistoryEventID = (TraceEventID) 5;
        public const TraceEventID GCJoinEventID = (TraceEventID) 6;
        public const TraceEventID GCMarkStackRootsEventID = (TraceEventID) 7;
        public const TraceEventID GCMarkFinalizeQueueRootsEventID = (TraceEventID) 8;
        public const TraceEventID GCMarkHandlesEventID = (TraceEventID) 9;
        public const TraceEventID GCMarkCardsEventID = (TraceEventID) 10;
        public const TraceEventID GCBGCStartEventID = (TraceEventID) 11;
        public const TraceEventID GCBGC1stNonCondStopEventID = (TraceEventID) 12;
        public const TraceEventID GCBGC1stConStopEventID = (TraceEventID) 13;
        public const TraceEventID GCBGC2ndNonConStartEventID = (TraceEventID) 14;
        public const TraceEventID GCBGC2ndNonConStopEventID = (TraceEventID) 15;
        public const TraceEventID GCBGC2ndConStartEventID = (TraceEventID) 16;
        public const TraceEventID GCBGC2ndConStopEventID = (TraceEventID) 17;
        public const TraceEventID GCBGCPlanStopEventID = (TraceEventID) 18;
        public const TraceEventID GCBGCSweepStopEventID = (TraceEventID) 19;
        public const TraceEventID GCBGCDrainMarkEventID = (TraceEventID) 20;
        public const TraceEventID GCBGCRevisitEventID = (TraceEventID) 21;
        public const TraceEventID GCBGCOverflowEventID = (TraceEventID) 22;
        public const TraceEventID GCBGCAllocWaitStartEventID = (TraceEventID) 23;
        public const TraceEventID GCBGCAllocWaitStopEventID = (TraceEventID) 24;
        public const TraceEventID GCFullNotifyEventID = (TraceEventID) 25;
        public const TraceEventID StartupEEStartupStartEventID = (TraceEventID) 80;
        public const TraceEventID StartupEEStartupStopEventID = (TraceEventID) 81;
        public const TraceEventID StartupEEConfigSetupStartEventID = (TraceEventID) 82;
        public const TraceEventID StartupEEConfigSetupStopEventID = (TraceEventID) 83;
        public const TraceEventID StartupLoadSystemBasesStartEventID = (TraceEventID) 84;
        public const TraceEventID StartupLoadSystemBasesStopEventID = (TraceEventID) 85;
        public const TraceEventID StartupExecExeStartEventID = (TraceEventID) 86;
        public const TraceEventID StartupExecExeStopEventID = (TraceEventID) 87;
        public const TraceEventID StartupMainStartEventID = (TraceEventID) 88;
        public const TraceEventID StartupMainStopEventID = (TraceEventID) 89;
        public const TraceEventID StartupApplyPolicyStartEventID = (TraceEventID) 90;
        public const TraceEventID StartupApplyPolicyStopEventID = (TraceEventID) 91;
        public const TraceEventID StartupLdLibShFolderStartEventID = (TraceEventID) 92;
        public const TraceEventID StartupLdLibShFolderStopEventID = (TraceEventID) 93;
        public const TraceEventID StartupPrestubWorkerStartEventID = (TraceEventID) 94;
        public const TraceEventID StartupPrestubWorkerStopEventID = (TraceEventID) 95;
        public const TraceEventID StartupGetInstallationStartEventID = (TraceEventID) 96;
        public const TraceEventID StartupGetInstallationStopEventID = (TraceEventID) 97;
        public const TraceEventID StartupOpenHModuleStartEventID = (TraceEventID) 98;
        public const TraceEventID StartupOpenHModuleStopEventID = (TraceEventID) 99;
        public const TraceEventID StartupExplicitBindStartEventID = (TraceEventID) 100;
        public const TraceEventID StartupExplicitBindStopEventID = (TraceEventID) 101;
        public const TraceEventID StartupParseXmlStartEventID = (TraceEventID) 102;
        public const TraceEventID StartupParseXmlStopEventID = (TraceEventID) 103;
        public const TraceEventID StartupInitDefaultDomainStartEventID = (TraceEventID) 104;
        public const TraceEventID StartupInitDefaultDomainStopEventID = (TraceEventID) 105;
        public const TraceEventID StartupInitSecurityStartEventID = (TraceEventID) 106;
        public const TraceEventID StartupInitSecurityStopEventID = (TraceEventID) 107;
        public const TraceEventID StartupAllowBindingRedirsStartEventID = (TraceEventID) 108;
        public const TraceEventID StartupAllowBindingRedirsStopEventID = (TraceEventID) 109;
        public const TraceEventID StartupEEConfigSyncStartEventID = (TraceEventID) 110;
        public const TraceEventID StartupEEConfigSyncStopEventID = (TraceEventID) 111;
        public const TraceEventID StartupBindingStartEventID = (TraceEventID) 112;
        public const TraceEventID StartupBindingStopEventID = (TraceEventID) 113;
        public const TraceEventID StartupLoaderCatchCallStartEventID = (TraceEventID) 114;
        public const TraceEventID StartupLoaderCatchCallStopEventID = (TraceEventID) 115;
        public const TraceEventID StartupFusionInitStartEventID = (TraceEventID) 116;
        public const TraceEventID StartupFusionInitStopEventID = (TraceEventID) 117;
        public const TraceEventID StartupFusionAppCtxStartEventID = (TraceEventID) 118;
        public const TraceEventID StartupFusionAppCtxStopEventID = (TraceEventID) 119;
        public const TraceEventID StartupFusion2EEStartEventID = (TraceEventID) 120;
        public const TraceEventID StartupFusion2EEStopEventID = (TraceEventID) 121;
        public const TraceEventID StartupSecurityCatchCallStartEventID = (TraceEventID) 122;
        public const TraceEventID StartupSecurityCatchCallStopEventID = (TraceEventID) 123;
        public const TraceEventID ClrStackWalkEventID = (TraceEventID) 151;
        public const TraceEventID BindingPolicyPhaseStartEventID = (TraceEventID) 159;
        public const TraceEventID BindingPolicyPhaseStopEventID = (TraceEventID) 160;
        public const TraceEventID BindingNgenPhaseStartEventID = (TraceEventID) 161;
        public const TraceEventID BindingNgenPhaseStopEventID = (TraceEventID) 162;
        public const TraceEventID BindingLoopupAndProbingPhaseStartEventID = (TraceEventID) 163;
        public const TraceEventID BindingLookupAndProbingPhaseStopEventID = (TraceEventID) 164;
        public const TraceEventID BindingLoaderPhaseStartEventID = (TraceEventID) 165;
        public const TraceEventID BindingLoaderPhaseStopEventID = (TraceEventID) 166;
        public const TraceEventID BindingPhaseStartEventID = (TraceEventID) 167;
        public const TraceEventID BindingPhaseStopEventID = (TraceEventID) 168;
        public const TraceEventID BindingDownloadPhaseStartEventID = (TraceEventID) 169;
        public const TraceEventID BindingDownloadPhaseStopEventID = (TraceEventID) 170;
        public const TraceEventID BindingLoaderAssemblyInitPhaseStartEventID = (TraceEventID) 171;
        public const TraceEventID BindingLoaderAssemblyInitPhaseStopEventID = (TraceEventID) 172;
        public const TraceEventID BindingLoaderMappingPhaseStartEventID = (TraceEventID) 173;
        public const TraceEventID BindingLoaderMappingPhaseStopEventID = (TraceEventID) 174;
        public const TraceEventID BindingLoaderDeliverEventPhaseStartEventID = (TraceEventID) 175;
        public const TraceEventID BindingLoaderDeliverEventsPhaseStopEventID = (TraceEventID) 176;
        public const TraceEventID EvidenceGenerationEvidenceGeneratedEventID = (TraceEventID) 177;
        public const TraceEventID TransparencyModuleTransparencyComputationStartEventID = (TraceEventID) 178;
        public const TraceEventID TransparencyModuleTransparencyComputationStopEventID = (TraceEventID) 179;
        public const TraceEventID TransparencyTypeTransparencyComputationStartEventID = (TraceEventID) 180;
        public const TraceEventID TransparencyTypeTransparencyComputationStopEventID = (TraceEventID) 181;
        public const TraceEventID TransparencyMethodTransparencyComputationStartEventID = (TraceEventID) 182;
        public const TraceEventID TransparencyMethodTransparencyComputationStopEventID = (TraceEventID) 183;
        public const TraceEventID TransparencyFieldTransparencyComputationStartEventID = (TraceEventID) 184;
        public const TraceEventID TransparencyFieldTransparencyComputationStopEventID = (TraceEventID) 185;
        public const TraceEventID TransparencyTokenTransparencyComputationStartEventID = (TraceEventID) 186;
        public const TraceEventID TransparencyTokenTransparencyComputationStopEventID = (TraceEventID) 187;
        public const TraceEventID NgenBinderNgenBindEventID = (TraceEventID) 188;
        public const TraceEventID FailFastFailFastEventID = (TraceEventID) 191;
       #endregion

    #region private
        private static Guid GCTaskGuid = new Guid(unchecked((int) 0x2f1b6bf6), unchecked((short) 0x18ff), unchecked((short) 0x4645), 0x95, 0x01, 0x15, 0xdf, 0x6c, 0x64, 0xc2, 0xcf);
        private static Guid FailFastTaskGuid = new Guid(unchecked((int) 0xee9ede12), unchecked((short) 0xc5f5), unchecked((short) 0x4995), 0x81, 0xa2, 0xdc, 0xfb, 0x5f, 0x6b, 0x80, 0xc8);
        private static Guid StartupTaskGuid = new Guid(unchecked((int) 0x02d08a4f), unchecked((short) 0xfd01), unchecked((short) 0x4538), 0x98, 0x9b, 0x03, 0xe4, 0x37, 0xb9, 0x50, 0xf4);
        private static Guid BindingTaskGuid = new Guid(unchecked((int) 0xe90e32ba), unchecked((short) 0xe396), unchecked((short) 0x4e6a), 0xa7, 0x90, 0x0a, 0x08, 0xc6, 0xc9, 0x25, 0xdc);
        private static Guid ClrStackTaskGuid = new Guid(unchecked((int) 0xd3363dc0), unchecked((short) 0x243a), unchecked((short) 0x4620), 0xa4, 0xd0, 0x8a, 0x07, 0xd7, 0x72, 0xf5, 0x33);
        private static Guid EvidenceGenerationTaskGuid = new Guid(unchecked((int) 0x24333617), unchecked((short) 0x5ae4), unchecked((short) 0x4f9e), 0xa5, 0xc5, 0x5e, 0xde, 0x1b, 0xc5, 0x92, 0x07);
        private static Guid NgenBinderTaskGuid = new Guid(unchecked((int) 0x861f5339), unchecked((short) 0x19d6), unchecked((short) 0x4873), 0xb3, 0x50, 0x7b, 0x03, 0x22, 0x8b, 0xda, 0x7c);
        private static Guid TransparencyTaskGuid = new Guid(unchecked((int) 0xe2444377), unchecked((short) 0xddf9), unchecked((short) 0x4589), 0xa8, 0x85, 0x08, 0xd6, 0x09, 0x25, 0x21, 0xdf);
    #endregion
    }

    public sealed class GCDecisionTraceData : TraceEvent
    {
        public bool DoCompact { get { return GetInt32At(0) != 0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(4); return 0; } }

        #region Private
        internal GCDecisionTraceData(Action<GCDecisionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version == 1 && EventDataLength != 6));
            Debug.Assert(!(Version > 1 && EventDataLength < 6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("DoCompact", DoCompact);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "DoCompact", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return DoCompact;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCDecisionTraceData> Action;
        #endregion
    }
    public sealed class GCSettingsTraceData : TraceEvent
    {
        public long SegmentSize { get { return GetInt64At(0); } }
        public long LargeObjectSegmentSize { get { return GetInt64At(8); } }
        public bool ServerGC { get { return GetInt32At(16) != 0; } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(20); return 0; } }

        #region Private
        internal GCSettingsTraceData(Action<GCSettingsTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 20));
            Debug.Assert(!(Version == 1 && EventDataLength != 22));
            Debug.Assert(!(Version > 1 && EventDataLength < 22));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("SegmentSize", SegmentSize);
             sb.XmlAttrib("LargeObjectSegmentSize", LargeObjectSegmentSize);
             sb.XmlAttrib("ServerGC", ServerGC);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "SegmentSize", "LargeObjectSegmentSize", "ServerGC", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return SegmentSize;
                case 1:
                    return LargeObjectSegmentSize;
                case 2:
                    return ServerGC;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCSettingsTraceData> Action;
        #endregion
    }
    public sealed class GCOptimizedTraceData : TraceEvent
    {
        public long DesiredAllocation { get { return GetInt64At(0); } }
        public long NewAllocation { get { return GetInt64At(8); } }
        public int GenerationNumber { get { return GetInt32At(16); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(20); return 0; } }

        #region Private
        internal GCOptimizedTraceData(Action<GCOptimizedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 20));
            Debug.Assert(!(Version == 1 && EventDataLength != 22));
            Debug.Assert(!(Version > 1 && EventDataLength < 22));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("DesiredAllocation", DesiredAllocation);
             sb.XmlAttribHex("NewAllocation", NewAllocation);
             sb.XmlAttrib("GenerationNumber", GenerationNumber);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "DesiredAllocation", "NewAllocation", "GenerationNumber", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return DesiredAllocation;
                case 1:
                    return NewAllocation;
                case 2:
                    return GenerationNumber;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCOptimizedTraceData> Action;
        #endregion
    }
    public sealed class GCPerHeapHistoryTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal GCPerHeapHistoryTraceData(Action<GCPerHeapHistoryTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCPerHeapHistoryTraceData> Action;
        #endregion
    }
    public sealed class GCGlobalHeapTraceData : TraceEvent
    {
        public long FinalYoungestDesired { get { return GetInt64At(0); } }
        public int NumHeaps { get { return GetInt32At(8); } }
        public int CondemnedGeneration { get { return GetInt32At(12); } }
        public int Gen0ReductionCount { get { return GetInt32At(16); } }
        public int Reason { get { return GetInt32At(20); } }
        public int GlobalMechanisms { get { return GetInt32At(24); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(28); return 0; } }

        #region Private
        internal GCGlobalHeapTraceData(Action<GCGlobalHeapTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 28));
            Debug.Assert(!(Version == 1 && EventDataLength != 30));
            Debug.Assert(!(Version > 1 && EventDataLength < 30));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttribHex("FinalYoungestDesired", FinalYoungestDesired);
             sb.XmlAttrib("NumHeaps", NumHeaps);
             sb.XmlAttrib("CondemnedGeneration", CondemnedGeneration);
             sb.XmlAttrib("Gen0ReductionCount", Gen0ReductionCount);
             sb.XmlAttrib("Reason", Reason);
             sb.XmlAttrib("GlobalMechanisms", GlobalMechanisms);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "FinalYoungestDesired", "NumHeaps", "CondemnedGeneration", "Gen0ReductionCount", "Reason", "GlobalMechanisms", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FinalYoungestDesired;
                case 1:
                    return NumHeaps;
                case 2:
                    return CondemnedGeneration;
                case 3:
                    return Gen0ReductionCount;
                case 4:
                    return Reason;
                case 5:
                    return GlobalMechanisms;
                case 6:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCGlobalHeapTraceData> Action;
        #endregion
    }
    public sealed class GCJoinTraceData : TraceEvent
    {
        public int Heap { get { return GetInt32At(0); } }
        public int JoinTime { get { return GetInt32At(4); } }
        public int JoinType { get { return GetInt32At(8); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(12); return 0; } }

        #region Private
        internal GCJoinTraceData(Action<GCJoinTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 12));
            Debug.Assert(!(Version == 1 && EventDataLength != 14));
            Debug.Assert(!(Version > 1 && EventDataLength < 14));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Heap", Heap);
             sb.XmlAttrib("JoinTime", JoinTime);
             sb.XmlAttrib("JoinType", JoinType);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Heap", "JoinTime", "JoinType", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Heap;
                case 1:
                    return JoinTime;
                case 2:
                    return JoinType;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCJoinTraceData> Action;
        #endregion
    }
    public sealed class GCMarkTraceData : TraceEvent
    {
        public int HeapNum { get { return GetInt32At(0); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(4); return 0; } }

        #region Private
        internal GCMarkTraceData(Action<GCMarkTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version == 1 && EventDataLength != 6));
            Debug.Assert(!(Version > 1 && EventDataLength < 6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("HeapNum", HeapNum);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapNum", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapNum;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCMarkTraceData> Action;
        #endregion
    }
    public sealed class BGCDrainMarkTraceData : TraceEvent
    {
        public long Objects { get { return GetInt64At(0); } }
        public int ClrInstanceID { get { return GetInt16At(8); } }

        #region Private
        internal BGCDrainMarkTraceData(Action<BGCDrainMarkTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 10));
            Debug.Assert(!(Version > 0 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Objects", Objects);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Objects", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Objects;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<BGCDrainMarkTraceData> Action;
        #endregion
    }
    public sealed class BGCRevisitTraceData : TraceEvent
    {
        public long Pages { get { return GetInt64At(0); } }
        public long Objects { get { return GetInt64At(8); } }
        public int IsLarge { get { return GetInt32At(16); } }
        public int ClrInstanceID { get { return GetInt16At(20); } }

        #region Private
        internal BGCRevisitTraceData(Action<BGCRevisitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 22));
            Debug.Assert(!(Version > 0 && EventDataLength < 22));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Pages", Pages);
             sb.XmlAttrib("Objects", Objects);
             sb.XmlAttrib("IsLarge", IsLarge);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Pages", "Objects", "IsLarge", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Pages;
                case 1:
                    return Objects;
                case 2:
                    return IsLarge;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<BGCRevisitTraceData> Action;
        #endregion
    }
    public sealed class BGCOverflowTraceData : TraceEvent
    {
        public long Min { get { return GetInt64At(0); } }
        public long Max { get { return GetInt64At(8); } }
        public long Objects { get { return GetInt64At(16); } }
        public int IsLarge { get { return GetInt32At(24); } }
        public int ClrInstanceID { get { return GetInt16At(28); } }

        #region Private
        internal BGCOverflowTraceData(Action<BGCOverflowTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 30));
            Debug.Assert(!(Version > 0 && EventDataLength < 30));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Min", Min);
             sb.XmlAttrib("Max", Max);
             sb.XmlAttrib("Objects", Objects);
             sb.XmlAttrib("IsLarge", IsLarge);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Min", "Max", "Objects", "IsLarge", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Min;
                case 1:
                    return Max;
                case 2:
                    return Objects;
                case 3:
                    return IsLarge;
                case 4:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<BGCOverflowTraceData> Action;
        #endregion
    }
    public sealed class BGCAllocWaitTraceData : TraceEvent
    {
        public int Reason { get { return GetInt32At(0); } }
        public int ClrInstanceID { get { return GetInt16At(4); } }

        #region Private
        internal BGCAllocWaitTraceData(Action<BGCAllocWaitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 6));
            Debug.Assert(!(Version > 0 && EventDataLength < 6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Reason", Reason);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reason", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reason;
                case 1:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<BGCAllocWaitTraceData> Action;
        #endregion
    }
    public sealed class GCFullNotifyTraceData : TraceEvent
    {
        public int GenNumber { get { return GetInt32At(0); } }
        public int IsAlloc { get { return GetInt32At(4); } }
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(8); return 0; } }

        #region Private
        internal GCFullNotifyTraceData(Action<GCFullNotifyTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 10));
            Debug.Assert(!(Version > 1 && EventDataLength < 10));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("GenNumber", GenNumber);
             sb.XmlAttrib("IsAlloc", IsAlloc);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "GenNumber", "IsAlloc", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return GenNumber;
                case 1:
                    return IsAlloc;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<GCFullNotifyTraceData> Action;
        #endregion
    }
    public sealed class StartupTraceData : TraceEvent
    {
        public int ClrInstanceID { get { if (Version >= 1) return GetInt16At(0); return 0; } }

        #region Private
        internal StartupTraceData(Action<StartupTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 2));
            Debug.Assert(!(Version > 1 && EventDataLength < 2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<StartupTraceData> Action;
        #endregion
    }
    public sealed class BindingTraceData : TraceEvent
    {
        public int AppDomainID { get { return GetInt32At(0); } }
        public int LoadContextID { get { return GetInt32At(4); } }
        public int FromLoaderCache { get { return GetInt32At(8); } }
        public int DynamicLoad { get { return GetInt32At(12); } }
        public string AssemblyCodebase { get { return GetUnicodeStringAt(16); } }
        public string AssemblyName { get { return GetUnicodeStringAt(SkipUnicodeString(16)); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(16))); } }

        #region Private
        internal BindingTraceData(Action<BindingTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(16))+2));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(16))+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("LoadContextID", LoadContextID);
             sb.XmlAttrib("FromLoaderCache", FromLoaderCache);
             sb.XmlAttrib("DynamicLoad", DynamicLoad);
             sb.XmlAttrib("AssemblyCodebase", AssemblyCodebase);
             sb.XmlAttrib("AssemblyName", AssemblyName);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AppDomainID", "LoadContextID", "FromLoaderCache", "DynamicLoad", "AssemblyCodebase", "AssemblyName", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AppDomainID;
                case 1:
                    return LoadContextID;
                case 2:
                    return FromLoaderCache;
                case 3:
                    return DynamicLoad;
                case 4:
                    return AssemblyCodebase;
                case 5:
                    return AssemblyName;
                case 6:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<BindingTraceData> Action;
        #endregion
    }
    public sealed class EvidenceGeneratedTraceData : TraceEvent
    {
        public int Type { get { return GetInt32At(0); } }
        public int AppDomain { get { return GetInt32At(4); } }
        public string ILImage { get { return GetUnicodeStringAt(8); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(8)); } }

        #region Private
        internal EvidenceGeneratedTraceData(Action<EvidenceGeneratedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(8)+2));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(8)+2));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Type", Type);
             sb.XmlAttrib("AppDomain", AppDomain);
             sb.XmlAttrib("ILImage", ILImage);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Type", "AppDomain", "ILImage", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Type;
                case 1:
                    return AppDomain;
                case 2:
                    return ILImage;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<EvidenceGeneratedTraceData> Action;
        #endregion
    }
    public sealed class ModuleTransparencyCalculationTraceData : TraceEvent
    {
        public string Module { get { return GetUnicodeStringAt(0); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(0)); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(0)+4); } }

        #region Private
        internal ModuleTransparencyCalculationTraceData(Action<ModuleTransparencyCalculationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(0)+6));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(0)+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Module", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Module;
                case 1:
                    return AppDomainID;
                case 2:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ModuleTransparencyCalculationTraceData> Action;
        #endregion
    }
    public sealed class ModuleTransparencyCalculationResultTraceData : TraceEvent
    {
        public string Module { get { return GetUnicodeStringAt(0); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(0)); } }
        public bool IsAllCritical { get { return GetInt32At(SkipUnicodeString(0)+4) != 0; } }
        public bool IsAllTransparent { get { return GetInt32At(SkipUnicodeString(0)+8) != 0; } }
        public bool IsTreatAsSafe { get { return GetInt32At(SkipUnicodeString(0)+12) != 0; } }
        public bool IsOpportunisticallyCritical { get { return GetInt32At(SkipUnicodeString(0)+16) != 0; } }
        public int SecurityRuleSet { get { return GetInt32At(SkipUnicodeString(0)+20); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(0)+24); } }

        #region Private
        internal ModuleTransparencyCalculationResultTraceData(Action<ModuleTransparencyCalculationResultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(0)+26));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(0)+26));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("IsAllCritical", IsAllCritical);
             sb.XmlAttrib("IsAllTransparent", IsAllTransparent);
             sb.XmlAttrib("IsTreatAsSafe", IsTreatAsSafe);
             sb.XmlAttrib("IsOpportunisticallyCritical", IsOpportunisticallyCritical);
             sb.XmlAttrib("SecurityRuleSet", SecurityRuleSet);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Module", "AppDomainID", "IsAllCritical", "IsAllTransparent", "IsTreatAsSafe", "IsOpportunisticallyCritical", "SecurityRuleSet", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Module;
                case 1:
                    return AppDomainID;
                case 2:
                    return IsAllCritical;
                case 3:
                    return IsAllTransparent;
                case 4:
                    return IsTreatAsSafe;
                case 5:
                    return IsOpportunisticallyCritical;
                case 6:
                    return SecurityRuleSet;
                case 7:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ModuleTransparencyCalculationResultTraceData> Action;
        #endregion
    }
    public sealed class TypeTransparencyCalculationTraceData : TraceEvent
    {
        public string Type { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+4); } }

        #region Private
        internal TypeTransparencyCalculationTraceData(Action<TypeTransparencyCalculationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+6));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Type", Type);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Type", "Module", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Type;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TypeTransparencyCalculationTraceData> Action;
        #endregion
    }
    public sealed class TypeTransparencyCalculationResultTraceData : TraceEvent
    {
        public string Type { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public bool IsAllCritical { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+4) != 0; } }
        public bool IsAllTransparent { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+8) != 0; } }
        public bool IsCritical { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+12) != 0; } }
        public bool IsTreatAsSafe { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+16) != 0; } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+20); } }

        #region Private
        internal TypeTransparencyCalculationResultTraceData(Action<TypeTransparencyCalculationResultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+22));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+22));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Type", Type);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("IsAllCritical", IsAllCritical);
             sb.XmlAttrib("IsAllTransparent", IsAllTransparent);
             sb.XmlAttrib("IsCritical", IsCritical);
             sb.XmlAttrib("IsTreatAsSafe", IsTreatAsSafe);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Type", "Module", "AppDomainID", "IsAllCritical", "IsAllTransparent", "IsCritical", "IsTreatAsSafe", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Type;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return IsAllCritical;
                case 4:
                    return IsAllTransparent;
                case 5:
                    return IsCritical;
                case 6:
                    return IsTreatAsSafe;
                case 7:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TypeTransparencyCalculationResultTraceData> Action;
        #endregion
    }
    public sealed class MethodTransparencyCalculationTraceData : TraceEvent
    {
        public string Method { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+4); } }

        #region Private
        internal MethodTransparencyCalculationTraceData(Action<MethodTransparencyCalculationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+6));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Method", Method);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Method", "Module", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Method;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodTransparencyCalculationTraceData> Action;
        #endregion
    }
    public sealed class MethodTransparencyCalculationResultTraceData : TraceEvent
    {
        public string Method { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public bool IsCritical { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+4) != 0; } }
        public bool IsTreatAsSafe { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+8) != 0; } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+12); } }

        #region Private
        internal MethodTransparencyCalculationResultTraceData(Action<MethodTransparencyCalculationResultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+14));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+14));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Method", Method);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("IsCritical", IsCritical);
             sb.XmlAttrib("IsTreatAsSafe", IsTreatAsSafe);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Method", "Module", "AppDomainID", "IsCritical", "IsTreatAsSafe", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Method;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return IsCritical;
                case 4:
                    return IsTreatAsSafe;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<MethodTransparencyCalculationResultTraceData> Action;
        #endregion
    }
    public sealed class FieldTransparencyCalculationTraceData : TraceEvent
    {
        public string Field { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+4); } }

        #region Private
        internal FieldTransparencyCalculationTraceData(Action<FieldTransparencyCalculationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+6));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Field", Field);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Field", "Module", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Field;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FieldTransparencyCalculationTraceData> Action;
        #endregion
    }
    public sealed class FieldTransparencyCalculationResultTraceData : TraceEvent
    {
        public string Field { get { return GetUnicodeStringAt(0); } }
        public string Module { get { return GetUnicodeStringAt(SkipUnicodeString(0)); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))); } }
        public bool IsCritical { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+4) != 0; } }
        public bool IsTreatAsSafe { get { return GetInt32At(SkipUnicodeString(SkipUnicodeString(0))+8) != 0; } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(SkipUnicodeString(0))+12); } }

        #region Private
        internal FieldTransparencyCalculationResultTraceData(Action<FieldTransparencyCalculationResultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(0))+14));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(0))+14));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Field", Field);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("IsCritical", IsCritical);
             sb.XmlAttrib("IsTreatAsSafe", IsTreatAsSafe);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Field", "Module", "AppDomainID", "IsCritical", "IsTreatAsSafe", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Field;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return IsCritical;
                case 4:
                    return IsTreatAsSafe;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FieldTransparencyCalculationResultTraceData> Action;
        #endregion
    }
    public sealed class TokenTransparencyCalculationTraceData : TraceEvent
    {
        public int Token { get { return GetInt32At(0); } }
        public string Module { get { return GetUnicodeStringAt(4); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(4)); } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(4)+4); } }

        #region Private
        internal TokenTransparencyCalculationTraceData(Action<TokenTransparencyCalculationTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(4)+6));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(4)+6));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Token", Token);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Token", "Module", "AppDomainID", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Token;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TokenTransparencyCalculationTraceData> Action;
        #endregion
    }
    public sealed class TokenTransparencyCalculationResultTraceData : TraceEvent
    {
        public int Token { get { return GetInt32At(0); } }
        public string Module { get { return GetUnicodeStringAt(4); } }
        public int AppDomainID { get { return GetInt32At(SkipUnicodeString(4)); } }
        public bool IsCritical { get { return GetInt32At(SkipUnicodeString(4)+4) != 0; } }
        public bool IsTreatAsSafe { get { return GetInt32At(SkipUnicodeString(4)+8) != 0; } }
        public int ClrInstanceID { get { return GetInt16At(SkipUnicodeString(4)+12); } }

        #region Private
        internal TokenTransparencyCalculationResultTraceData(Action<TokenTransparencyCalculationResultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(4)+14));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(4)+14));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("Token", Token);
             sb.XmlAttrib("Module", Module);
             sb.XmlAttrib("AppDomainID", AppDomainID);
             sb.XmlAttrib("IsCritical", IsCritical);
             sb.XmlAttrib("IsTreatAsSafe", IsTreatAsSafe);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Token", "Module", "AppDomainID", "IsCritical", "IsTreatAsSafe", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Token;
                case 1:
                    return Module;
                case 2:
                    return AppDomainID;
                case 3:
                    return IsCritical;
                case 4:
                    return IsTreatAsSafe;
                case 5:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TokenTransparencyCalculationResultTraceData> Action;
        #endregion
    }
    public sealed class NgenBindEventTraceData : TraceEvent
    {
        public int ClrInstanceID { get { return GetInt16At(0); } }
        public long BindingID { get { return GetInt64At(2); } }
        public int ReasonCode { get { return GetInt32At(10); } }
        public string AssemblyName { get { return GetUnicodeStringAt(14); } }

        #region Private
        internal NgenBindEventTraceData(Action<NgenBindEventTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(14)));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(14)));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.XmlAttrib("BindingID", BindingID);
             sb.XmlAttrib("ReasonCode", ReasonCode);
             sb.XmlAttrib("AssemblyName", AssemblyName);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ClrInstanceID", "BindingID", "ReasonCode", "AssemblyName"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ClrInstanceID;
                case 1:
                    return BindingID;
                case 2:
                    return ReasonCode;
                case 3:
                    return AssemblyName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<NgenBindEventTraceData> Action;
        #endregion
    }
    public sealed class FailFastTraceData : TraceEvent
    {
        public string FailFastUserMessage { get { return GetUnicodeStringAt(0); } }
        public Address FailedEIP { get { return GetHostPointer(SkipUnicodeString(0)); } }
        public int OSExitCode { get { return GetInt32At(HostOffset(SkipUnicodeString(0)+4, 1)); } }
        public int ClrExitCode { get { return GetInt32At(HostOffset(SkipUnicodeString(0)+8, 1)); } }
        public int ClrInstanceID { get { return GetInt16At(HostOffset(SkipUnicodeString(0)+12, 1)); } }

        #region Private
        internal FailFastTraceData(Action<FailFastTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        protected internal override void Dispatch()
        {
            Action(this);
        }
        protected internal override void Validate()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(SkipUnicodeString(0)+14, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(SkipUnicodeString(0)+14, 1)));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
             Prefix(sb);
             sb.XmlAttrib("FailFastUserMessage", FailFastUserMessage);
             sb.XmlAttribHex("FailedEIP", FailedEIP);
             sb.XmlAttrib("OSExitCode", OSExitCode);
             sb.XmlAttrib("ClrExitCode", ClrExitCode);
             sb.XmlAttrib("ClrInstanceID", ClrInstanceID);
             sb.Append("/>");
             return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "FailFastUserMessage", "FailedEIP", "OSExitCode", "ClrExitCode", "ClrInstanceID"};
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FailFastUserMessage;
                case 1:
                    return FailedEIP;
                case 2:
                    return OSExitCode;
                case 3:
                    return ClrExitCode;
                case 4:
                    return ClrInstanceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FailFastTraceData> Action;
        #endregion
    }
#endif
    #endregion // PRIVATE_CLR_PROVIDER
}
