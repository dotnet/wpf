// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Log of recent actions.  Use this to debug those nasty problems
// that don't repro on demand and don't have enough information in a crash
// dump.
//
//  In the class(es) of interest, add a TraceLog object.  At points of
//  interest, call TraceLog.Add to record a string in the log.  After the
//  crash, call TraceLog.WriteLog (or simply examine the log directly in
//  the debugger).  Log entries are timestamped.
//

using System;
using System.Collections;
using System.Globalization;

namespace MS.Internal.Utility
{
    internal class TraceLog
    {
        // create an unbounded trace log
        internal TraceLog() : this(Int32.MaxValue) {}

        // create a trace log that remembers the last 'size' actions
        internal TraceLog(int size)
        {
            _size = size;
            _log = new ArrayList();
        }

        // add an entry to the log.  Args are just like String.Format
        internal void Add(string message, params object[] args)
        {
            // create timestamped message string
            string s = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)
                        + " "
                        + String.Format(CultureInfo.InvariantCulture, message, args);

            // if log is full, discard the oldest message
            if (_log.Count == _size)
                _log.RemoveAt(0);

            // add the new message
            _log.Add(s);
        }

        // write the log to the console
        internal void WriteLog()
        {
            for (int k=0; k<_log.Count; ++k)
                Console.WriteLine(_log[k]);
        }

        // return a printable id for the object
        internal static string IdFor(object o)
        {
            if (o == null)
                return "NULL";
            else
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                        o.GetType().Name, o.GetHashCode());
        }

        ArrayList _log;
        int _size;
    }
}
