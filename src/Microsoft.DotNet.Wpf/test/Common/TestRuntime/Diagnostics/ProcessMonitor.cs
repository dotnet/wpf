// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading; 
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Test.Logging;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Diagnostics {

    //There may be uses for making this class public
    internal sealed class ProcessMonitor{
        
        #region Private Data
        static int MAX_PATH = 255; 
	static uint VISIBLE_WINDOW_FOUND = 0x0001;
	static uint VISIBLE_WINDOW_TITLE_CHANGED = 0x0010;

        int frequency = 10;
        Timer timer = null;
        List<int> ProcessesToIgnore = new List<int>();
        List<Process> ProcessesToMonitor = new List<Process>();
        List<string> ProcessesToFind = new List<string>();
        Dictionary<IntPtr, string> VisibleWindows = new Dictionary<IntPtr, string>();

        #endregion
        

        #region Constructors

        public ProcessMonitor() {
        }
        
        #endregion


        #region public Members

        public event ProcessFoundHandler ProcessFound;
        public event ProcessExitedHandler ProcessExited;
        public event VisibleWindowHandler VisibleWindowFound;
        public event VisibleWindowHandler VisibleWindowTitleChanged;

        public int Frequency {
            get { return frequency; }
            set {
                if (value <= 0)
                    throw new ArgumentException("Frequency must be a positive number greater then 0");
                frequency = value;
                if (timer != null)
                    timer.Change(1000 / frequency, 1000 / frequency);
            }
        }

        public void AddProcess(Process process) {
            lock (ProcessesToMonitor) {
                if (process != null && !ContainsProcess(ProcessesToMonitor, process)) {
                    ProcessesToMonitor.Add(process);
                    OnProcessFound(process);
                }
            }
        }

        public void AddProcess(params string[] processNames) {
            lock (ProcessesToFind) {
                foreach (string processName in processNames) {
                    ProcessesToFind.Add(processName);
                }
            }
        }

        public void IgnoreRunningProcesses(){
            lock (ProcessesToIgnore) {
                foreach (Process process in Process.GetProcesses()) {
                    if (!ProcessesToIgnore.Contains(process.Id)) {
                        string procName;
                        try
                        {
                            procName = process.ProcessName;
                        }
                        catch (InvalidOperationException)
                        {
                            //the process has exited
                            continue;
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            //strange whidbey bug.. the process has most likely already exited
                            continue;
                        }

                        //If this process is on that we want to monitor for UI then don't ignore it
                        foreach (string processNameToFind in ProcessesToFind)
                        {
                            if (procName.ToLowerInvariant() == processNameToFind.ToLowerInvariant())
                            {
                                GlobalLog.LogDebug(string.Format("Ignoring {0} : {1}", procName, process.Id));
                                break;
                            }
                        }

                        ProcessesToIgnore.Add(process.Id);
                    }
                }
            }
        }

        
        public void Start() {
            if (timer == null) {
                //we want to ignore all processes except for the ones that we want to monitor for UI
                if (ProcessesToIgnore.Count == 0) {
                    //take a snapshot of the processes right now
                    foreach (Process process in Process.GetProcesses()) {
                        if (!ProcessesToIgnore.Contains(process.Id)) {
                            //Get the name and ensure that the process is still running
                            string procName;
                            try {
                                procName = process.ProcessName;
                            }
                            catch (InvalidOperationException) {
                                //the process has exited
                                continue;
                            }
                            catch (System.ComponentModel.Win32Exception) {
                                //strange whidbey bug.. the process has most likely already exited
                                continue;
                            }
                            
                            bool knownProcess = false;
                            //If this process is on that we want to monitor for UI then don't ignore it
                            foreach (string processNameToFind in ProcessesToFind) {
                                if (procName.ToLowerInvariant() == processNameToFind.ToLowerInvariant()) {
                                    knownProcess = true;
                                    break;
                                }
                            }

                            //Ignore the process if we dont want to monitor it for UI
                            if (!knownProcess)
                                ProcessesToIgnore.Add(process.Id);
                        }
                    }
                }
                
                //start the process monitor timmer
                timer = new Timer(new TimerCallback(Monitor), null, 0, 1000 / frequency);
            }
        }

        public void Stop() {
            if (timer != null) {
                ManualResetEvent notifyObject = new ManualResetEvent(false);
                timer.Dispose(notifyObject);
                //notifyObject.WaitOne();
                timer = null;
            }
        }

        public void KillProcesses() {
            lock (ProcessesToMonitor) {
                foreach (Process process in ProcessesToMonitor){
                    if (process.HasExited)
                        continue;
                    if (process.CloseMainWindow()) {
                        GlobalLog.LogDebug("Closing Main window of process " + process.Id + ": " + GetProcessName(process));
                        if (!process.WaitForExit(5000)) {
                            try {
                                GlobalLog.LogDebug("Killing process " + process.Id + ": " + GetProcessName(process));
                                process.Kill();
                                process.WaitForExit();
                            }
                            catch (InvalidOperationException) {
                                //The process has already exited
                            }
                            catch (Win32Exception) {
                                //The process has already exited or access is denied (this is probobly a whidbey bug)
                            }
                        }
                    }
                    else {
                        try {
                            GlobalLog.LogDebug("Killing process " + process.Id + ": " + GetProcessName(process));
                            process.Kill();
                            process.WaitForExit();
                        }
                        catch (InvalidOperationException) {
                            //The process has already exited
                        }
                        catch (Win32Exception) {
                            //The process has already exited or access is denied (this is probobly a whidbey bug)
                        }
                    }
                }
                ProcessesToMonitor.Clear();
            }
        }
        
        #endregion


        #region Private Implementation
        
        void Monitor(object state) {
            //Find new processes that have started
            FindProcessesToMonitor();

            //TODO: find a better way to raise the events outside of the lock
            Process eventProcess = null;
            IntPtr eventTopLevelhWnd = IntPtr.Zero;
            IntPtr eventhWnd = IntPtr.Zero;
            string title = null;
	    uint eventsToRaise = 0x0000;

            //Monitor Each Process for exit, or visible windows
            lock (ProcessesToMonitor) {
                foreach (Process process in ProcessesToMonitor) {
                    eventProcess = process;

                    process.Refresh();
                    if (process.HasExited) {
                        ProcessesToMonitor.Remove(process);
                        OnProcessExited(process);
                        break;
                    }

                    //Monitor Visible Windows
                    foreach (HwndInfo info in GetTopLevelVisibleWindows(process)) {
                        eventTopLevelhWnd = info.hWnd;
                        title = GetWindowTitle(info.hWnd);
                        eventhWnd = eventTopLevelhWnd;
                        //Is the Window a new window we havent seen before?
                        if (!VisibleWindows.ContainsKey(info.hWnd)) {
                            VisibleWindows[info.hWnd] = title;
                            eventsToRaise |= VISIBLE_WINDOW_FOUND;
			    break;
                        }
                        //has the title of the window changed?
                        else if (VisibleWindows[info.hWnd] != title) {
                            VisibleWindows[info.hWnd] = title;
			    eventsToRaise |= VISIBLE_WINDOW_TITLE_CHANGED;
                            break;
                        }
                        
                        //Is the Window a new window we havent seen before?
                        HwndInfo[] infoChilds = GetVisibleChildWindowsNotInProcess(info.hWnd, info.ProcessId);
                        if (infoChilds.Length > 0) {
                            HwndInfo infoChild = infoChilds[infoChilds.Length - 1];
                            title = GetWindowTitle(infoChild.hWnd);
                            eventhWnd = infoChild.hWnd;
                            if (!VisibleWindows.ContainsKey(infoChild.hWnd)) {
                                VisibleWindows[infoChild.hWnd] = title;
                                eventsToRaise |= VISIBLE_WINDOW_FOUND;
                                eventProcess = Process.GetProcessById(infoChild.ProcessId);
                                break;
                            }
                        }
                    }
                    if (eventsToRaise != 0)
                        break;  //need to raise an event
                }
            }

            if ((eventsToRaise & VISIBLE_WINDOW_FOUND) != 0)
                OnVisibleWindowFound(eventTopLevelhWnd, eventhWnd, eventProcess, title);
            if ((eventsToRaise & VISIBLE_WINDOW_TITLE_CHANGED) != 0)
                OnVisibleWindowTitleChanged(eventTopLevelhWnd, eventhWnd, eventProcess, title);

        }

        void FindProcessesToMonitor(params string[] processNames) {
            lock (ProcessesToFind) {
                foreach (string processName in ProcessesToFind) {
                    Process[] currentProcesses = new Process[0];
                    try {  //Workaround whidbey bug that randomly throws Win32Exception
                        currentProcesses = Process.GetProcesses();
                    }
                    catch (Win32Exception)
                    {
                        //Unable to enumerate Process Moduals (this is probobly a whidbey bug)
                    }

                    //Look for any processes that we have not seen before
                    foreach (Process process in currentProcesses) {
                        if (!ProcessesToIgnore.Contains(process.Id))
                        {
                            //Get the name and ensure that the process is still running
                            string procName;
                            try {
                                
                                procName = process.ProcessName;
                                //Query to process to ensure that we have access to it and it has not exited
                                if (process.HasExited)
                                    continue;
                            }
                            catch (InvalidOperationException) {
                                //the process has exited
                                continue;
                            }
                            catch (System.ComponentModel.Win32Exception) {
                                //strange whidbey bug.. the process has most likely already exited
                                //Running as restricted user or under LUA on Longhorn and the process is not a user process
                                continue;
                            }
                            finally {
                               //remember that we have seen this process before
                               ProcessesToIgnore.Add(process.Id);
                            }

                            //tell the infrastructure to Monitor it as well                            
                            LogManager.LogProcessDangerously(process.Id);

                            
                            //If this process is on that we want to monitor for UI then add the process
                            foreach (string processNameToFind in ProcessesToFind)
                                if (procName.ToLowerInvariant() == processNameToFind.ToLowerInvariant())
                                    AddProcess(process);
                        }
                    }
                }
            }
        }

        void OnVisibleWindowFound(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title) {
            if (VisibleWindowFound != null)
                VisibleWindowFound(topLevelhWnd, hWnd, process, title);
        }

        void OnVisibleWindowTitleChanged(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title) {
            if (VisibleWindowTitleChanged != null)
                VisibleWindowTitleChanged(topLevelhWnd, hWnd, process, title);
        }

        void OnProcessFound(Process process) {
            if (process.HasExited)
                return;

            if (ProcessFound != null)
                ProcessFound(process);
        }

        void OnProcessExited(Process process) {
            if (ProcessExited != null)
                ProcessExited(process);
        }

        #endregion

    
        #region Static API (helpers and unmanaged imports)

        internal static string GetProcessName(Process process) {
            try
            {
                return process.ProcessName;
            }
            catch (InvalidOperationException)
            {
                //The process has already exited
                return null;
            }
            catch (Win32Exception)
            {
                //The process has already exited or access is denied (this is probobly a whidbey bug)
                return null;
            }
        }

        public static bool ContainsProcess(IEnumerable collection, Process process) {
            foreach (Process processMon in collection) {
                if (processMon.Id == process.Id)
                    return true;
            }

            return false;
        }


        [DllImport("user32", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("User32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        static int GetWindowProcessId(IntPtr hWnd) {
            int processId;
            GetWindowThreadProcessId(hWnd, out processId);
            return processId;
        }

        static string GetWindowTitle(IntPtr hWnd) {
            StringBuilder buf = new StringBuilder(MAX_PATH);

            if (GetWindowText(hWnd, buf, buf.Capacity) != 0)
                return buf.ToString();

            return null;
        }

        static HwndInfo[] GetTopLevelVisibleWindows(Process process) {
            return WindowEnumerator.GetTopLevelVisibleWindows(process);
        }

        static HwndInfo[] GetVisibleChildWindowsNotInProcess(IntPtr hWnd, int pid) {
            return WindowEnumerator.GetOutOfProcessVisibleChildWindows(hWnd);
        }

        #endregion
    }

    internal delegate void ProcessFoundHandler(Process process);
    internal delegate void ProcessExitedHandler(Process process);
    internal delegate void VisibleWindowHandler(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title);
}
