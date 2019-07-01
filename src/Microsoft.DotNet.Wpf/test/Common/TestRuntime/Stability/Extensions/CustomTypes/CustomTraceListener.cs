// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Diagnostics;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    /// <summary>
    /// Custom TraceListener class that throws away Trace messages. 
    /// </summary>
    public class CustomTraceListener : TraceListener
    {

        public override void Write(string message) { }

        public override void WriteLine(string message) { }

        public override void Write(string message, string category) { }

        ~CustomTraceListener()
        {
            base.Dispose();
        }
    }
}
