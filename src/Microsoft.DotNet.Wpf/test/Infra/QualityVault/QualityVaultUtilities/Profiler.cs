// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.IO;

namespace Microsoft.Test
{
    /// <summary>
    /// Profiler is a simple explicit instrumentation profiler to record time spent in key infra components.
    /// </summary>
    static public class Profiler
    {
        /// <summary>
        /// Starts profiling this method. Must be complemented with EndMethod call. Can be nested.
        /// </summary>
        public static void StartMethod()
        {
            StackFrame frame = new StackTrace().GetFrame(1);
            string name = frame.GetMethod().Name + "." + frame.GetMethod().ReflectedType;
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}Start: {1}", Indent(), name));
            sb.AppendLine("");
            startTimeStack.Push(new NameDate(name, DateTime.Now));
        }

        /// <summary>
        /// Stops profiling of this method.
        /// </summary>
        public static void EndMethod()
        {
            NameDate nd = startTimeStack.Pop();
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}End: {1}  Duration: {2}s", Indent(), nd.Name, (DateTime.Now - nd.DateTime).TotalSeconds));
            sb.AppendLine("");
        }

        /// <summary>
        /// Creates a report indicating where time was spent in the program.
        /// </summary>
        public static void GenerateReport()
        {
            if (startTimeStack.Count != 0)
            {
                throw new InvalidOperationException("Why is the Profiler Stack not at 0? Please Investigate:" + startTimeStack);
            }
            string reportPath="ProfilingReport.txt";
            using (StreamWriter sw = new StreamWriter(reportPath))
            {
                // Most people could care less about the infra profiling data, so just log it to local path.
                // We should formalize the scope of profiling scheme. Chances are we only care about test execution block, as non execution parts:
                // discovery, filtering, reporting, scatter/gather, save/load can be easily profiled in VS, and are more uniform in behavior.
                sw.Write("Infra Profiling Report:\n" + sb.ToString()); 
            }
        }

        private static string Indent()
        {
            return "".PadLeft((startTimeStack.Count + 1) * 2);
        }

        private class NameDate
        {
            public NameDate(string name, DateTime date)
            {
                Name = name;
                DateTime = date;
            }
            public DateTime DateTime;
            public string Name;
        }

        private static Stack<NameDate> startTimeStack = new Stack<NameDate>();
        private static StringBuilder sb = new StringBuilder();
    }
}