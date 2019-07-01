// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class SetDebugTracingAction : SimpleDiscoverableAction
    {
        #region Public Members

        public SourceLevels SourceLevel { get; set; }

        public int TraceSourceIndex { get; set; }

        public bool IsRefresh { get; set; }

        #endregion

        #region Private Members

        static SetDebugTracingAction()
        {
            string keyName = "Software\\Microsoft\\Tracing\\WPF";
            //Creates a new sub key or opens an existing subkey
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(keyName);
            regKey.SetValue("ManagedTracing", 1);
            //
            // Load the config document and SourceSwitch attribute.
            //
            configDocument = new XmlDocument();
            configDocument.Load(configFileName);
            sourceSwitchAttrib = (XmlAttribute)configDocument.SelectSingleNode("//*[@name='SourceSwitch']/@value");
            //
            // Populate _traceSources.
            // Generate <sources> section of config document.
            //
            XmlElement sourcesElement = (XmlElement)configDocument.SelectSingleNode("//sources");
            XmlElement sourceElement = (XmlElement)sourcesElement.SelectSingleNode("source");
            sourcesElement.RemoveAll();

            Type type = typeof(PresentationTraceSources);
            PropertyInfo[] props = type.GetProperties();
            traceSources = new TraceSource[props.Length];

            for (int i = 0; i < props.Length; i++)
            {
                TraceSource traceSource = (TraceSource)props[i].GetValue(null, null);
                traceSources[i] = traceSource;

                // Clone source xml element.
                // Change 'name' attribute.
                // Insert in xml document.
                XmlElement newSource = (XmlElement)sourceElement.CloneNode(true);
                newSource.SetAttribute("name", traceSource.Name);

                sourcesElement.AppendChild(newSource);
            }
            //
            // Initialize timer.
            //
            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Tick += new EventHandler(OnTimerTick);
            timer.Interval = minTraceTime;
            // Ensure tracing is off at start.
            EndTraceAll();
            isTracingOn = false;
        }

        // The timer starts when we start tracing.  When the timer ticks,
        // we stop tracing.
        private static void OnTimerTick(object sender, EventArgs args)
        {
            // Stop the timer now.
            timer.Stop();

            lock (DebugTracingLock)
            {
                EndTraceAll();
                isTracingOn = false;
            }
        }

        // To start tracing, set SourceSwitch to one of the defined levels, and refresh.
        private void StartTraceAll()
        {
            if (IsRefresh)
            {
                sourceSwitchAttrib.Value = Enum.GetName(typeof(SourceLevels), SourceLevel);
                configDocument.Save(configFileName);
                Trace.Refresh();
            }
            else
            {
                TraceSourceIndex %= traceSources.Length;
                currentSource = traceSources[TraceSourceIndex];
                currentSource.Switch.Level = SourceLevel;
            }
        }

        // To stop tracing, set SourceSwitch to "Off", and refresh.
        private static void EndTraceAll()
        {
            if (currentSource == null)
            {
                sourceSwitchAttrib.Value = "Off";
                configDocument.Save(configFileName);
                Trace.Refresh();
            }
            else
            {
                currentSource.Switch.Level = SourceLevels.Off;
                currentSource = null;
            }
        }

        #endregion

        #region Override Members

        public override void Perform()
        {
            lock (DebugTracingLock)
            {
                // Start tracing if it's not on already, and
                // start the timer so we automatically stop
                // tracing after 10 seconds.
                if (!isTracingOn)
                {
                    isTracingOn = true;
                    StartTraceAll();
                    timer.Start();
                }
            }
        }

        #endregion

        #region Private Data

        private static object DebugTracingLock = new object();
        private static bool isTracingOn = false;
        private static DispatcherTimer timer = null;
        private static TimeSpan minTraceTime = new TimeSpan(0, 0, 10); // 10 seconds
        private static string configFileName = "SetDebugTracingAction.config";
        private static XmlDocument configDocument = null;
        private static XmlAttribute sourceSwitchAttrib = null;
        private static TraceSource[] traceSources = null;
        private static TraceSource currentSource = null;

        #endregion
    }
}
