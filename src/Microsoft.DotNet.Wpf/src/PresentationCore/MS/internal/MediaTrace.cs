// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System;
using MS.Internal; 
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace MS.Internal
{
#if DEBUG

    internal class MediaTrace : IDisposable
    {
        public static MediaTrace NodeFlag = new MediaTrace("Node Flags"); public static MediaTrace NodeOp = new MediaTrace("Node Operations");
        public static MediaTrace NodeCreation = new MediaTrace("Node creation");
        public static MediaTrace DrawingContextOp = new MediaTrace("Drawing Context Op");
        public static MediaTrace ContainerOp = new MediaTrace("Drawing Visual Operations Operations");
        public static MediaTrace Precompute = new MediaTrace("Precompute");
        public static MediaTrace Render = new MediaTrace("Render");
        public static MediaTrace RenderClipRectangle = new MediaTrace("Render red clip rectangle around Visual");
        public static MediaTrace GraphWalker = new MediaTrace("GraphWalker");
        public static MediaTrace DirtyRegion = new MediaTrace("DirtyRegion");
        public static MediaTrace DirtyRegionStacks = new MediaTrace("DirtyRegionStacks");
        public static MediaTrace RenderPass = new MediaTrace("RenderPass");
        public static MediaTrace AnimationThread = new MediaTrace("AnimationThread");
        public static MediaTrace ShowDirtyRegion = new MediaTrace("ShowDirtyRegion");
        public static MediaTrace HwndTarget = new MediaTrace("HwndTarget");
        public static MediaTrace QueueItems = new MediaTrace("MILQueueItems");
        public static MediaTrace Statistics = new MediaTrace("Statistics");
        
        public class ChangeQueue
        {
            public static MediaTrace ApplyChange = new MediaTrace("Change queue: Apply Change");
            public static MediaTrace Enqueue = new MediaTrace("Change queue: Enqueue");
        }

        public static MediaTrace ValidationQueue = new MediaTrace("Validate queue");

        // Note:
        // If you want to enable trace tags without recompiling. This is a good place to put a break point
        // during start-up.

        ///<SecurityNote> 
        ///     Critical - Elevates to register debug listeners. 
        ///                Getting the listeners property demands unmanaged code permission. 
        ///
        ///     TreatAsSafe ( in debug) - in debug code - it is ok for code to call this. 
        ///                               if this function does get enabled in retail code - we will demand. ( and this won't be TAS). 
        ///</SecurityNote> 
        [SecurityCritical
#if DEBUG        
        ,SecurityTreatAsSafe
#endif        
        ]
        static MediaTrace()
        {
            // NodeFlag.Enable();
            // NodeOp.Enable();
            // NodeCreation.Enable();
            // DrawingContextOp.Enable();
            // ContainerOp.Enable();
            // ChangeQueue.ApplyChange.Enable();
            // ValidationQueue.Enable();
            // RenderClipRectangle.Enable();
            // GraphWalker.Enable();
            // DirtyRegion.Enable();
            // Render.Enable();
            // RenderPass.Enable();
            // AnimationThread.Enable();
            // Precompute.Enable();
            // HwndTarget.Enable();
            // QueueItems.Enable();
            Statistics.Enable();

#if !DEBUG
            // if somehow this code gets enabled in retail. Do a demand. 
            //
            SecurityHelper.DemandUnmanagedCode(); 
#endif

#if DEBUG
            //
            // We are asserting on startup path. Very bad from a perf perspective.  
            // However considered ok for now as this is just done in debug code. 
            // 
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert(); // BlessedAssert: 
            try
            {
                System.Diagnostics.Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            }
            finally
            {
                CodeAccessPermission.RevertAssert(); 
            }
#endif            
        }
    
        public MediaTrace(string switchName)
        {
            _switch = new BooleanSwitch(switchName, "[" + SafeSecurityHelper.GetAssemblyPartialName(Assembly.GetCallingAssembly()) + "]");
        }

        ///<SecurityNote>
        ///     Critical: sets BooleanSwitch.Enabled which LinkDemands
        ///     TreatAsSafe: ok to enable tracing
        ///</SecurityNote> 
        public MediaTrace(string switchName, bool initialState) : this(switchName)
        {
            _switch.Enabled = initialState;
        }

        public void Trace(string message)
        {
            if (_switch.Enabled)
            {
                System.Diagnostics.Trace.WriteLine(_switch.Description + " " + _switch.DisplayName + " : " + message);
            }
        }

        public void TraceCallers(int Depth)
        {
            if (_switch.Enabled)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1);
                if (Depth < 0)
                    Depth = st.FrameCount;

                Indent();

                for (int i=0; i < st.FrameCount && i < Depth; i++)
                {
                    System.Diagnostics.StackFrame sf = st.GetFrame(i);
                    Trace(sf.GetMethod()+"+"+sf.GetILOffset().ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                Unindent();
            }

        }

        public void Indent()
        {
            System.Diagnostics.Trace.Indent();
        }

        public void Unindent()
        {
            System.Diagnostics.Trace.Unindent();
        }

        public IDisposable Indented()
        {
            System.Diagnostics.Trace.Indent();
            return (this);
        }

        public IDisposable Indented(string message)
        {
            System.Diagnostics.Trace.WriteLine(_switch.Description + " " + _switch.DisplayName + " : " + message);
            System.Diagnostics.Trace.Indent();
            return (this);
        }

        void IDisposable.Dispose()
        {
            System.Diagnostics.Trace.Unindent();
            GC.SuppressFinalize(this);
        }
        
        /// <SecurityNote>
        ///   Critical: This code calls into Enabled which is link demand protected
        ///   TreatAsSafe: This code is ok to call since it enables trace which
        ///                 is safe.
        /// </SecurityNote>
        public void Enable()
        {
            _switch.Enabled = true;
        }
    
        /// <SecurityNote>
        ///   Critical: This code calls into Enabled which is link demand protected
        ///   TreatAsSafe: This code is ok to call since it  disables trace which
        ///                 is safe.
        /// </SecurityNote>
        public void Disable()
        {
            _switch.Enabled = false;
        }

        public bool IsEnabled
        {
            get { return _switch.Enabled; }
        }


        private System.Diagnostics.BooleanSwitch _switch;
    } 

#endif
}
