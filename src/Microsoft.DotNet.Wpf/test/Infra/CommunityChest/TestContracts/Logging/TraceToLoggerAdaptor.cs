// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Globalization;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// The Trace to Logger Adaptor converts traces into calls to the Logging Client.
    /// </summary>
    public class TraceToLoggerAdaptor : TraceListener
    {       
        #region Constructor

        /// <summary/>        
        public TraceToLoggerAdaptor()
            : base()
        {            
        }

        #endregion

        /// <summary>
        /// Logger to recieve converted trace messages
        /// </summary>
        public ILogger Logger { get; set; }

        
        #region Public Methods (TraceListener members)


        /// <summary/>        
        public override void Fail(string message)
        {  
            //Treat assertions as exceptions to be handled by the driver.
            throw new AssertionException(message);
        }
        /// <summary/>
        public override void Fail(string message, string detailMessage)
        {
            //Treat assertions as exceptions to be handled by the driver.
            throw new AssertionException(message + "\n" + detailMessage);
        }

        /// <summary/>
        public override void Close()
        {
            TraceOut("Trace.Close", TraceEventType.Information, 0, null);
        }

        /// <summary/>
        public override void Flush()
        {
            TraceOut("Trace.Flush", TraceEventType.Information, 0, null);
        }

        /// <summary/>
        void TraceOut(string source, TraceEventType eventType, int id, params object[] data)
        {
            string datastr = VariableNumberofParamsToString(data);
            Logger.LogMessage(String.Format(CultureInfo.InvariantCulture, "TestContract: Source: {0} | EventType: {1} | ID: {2} | Data: {3} |", source, eventType, id, datastr));
        }

        /// <summary>
        /// Takes any array of data and returns a string of values. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string VariableNumberofParamsToString(object[] data)
        {
            string datastr = string.Empty;
            if (data == null)
            {
                datastr = "null";
            }
            else if (data.Length == 0)
            {
                datastr = "No Data";
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    object o = data[i];

                    // special case: If we have an empty string the output leaves the reader confused 
                    // the reader is not able to tell why there is an empty field.
                    if (typeof(string) == o.GetType())
                    {
                        if (o.Equals(string.Empty))
                        {
                            datastr = "String.Empty" + " ";
                        }
                        else
                        {
                            datastr += o;
                        }
                    }
                    else
                    {
                        datastr += o;
                    }

                    // added so the last element of the array is not followed by a comma - aesthetics :)!
                    if (i < data.Length - 1)
                    {
                        datastr += ", ";
                    }
                }
            }
            return datastr;
        }

        /// <summary/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            TraceOut(source, eventType, id, data);
        }

        /// <summary/>
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {            
            TraceOut(source, eventType, id, data);         
        }

        /// <summary/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            TraceOut(source, eventType, id, "Parameterless TraceEvent Recorded.");
        }

        /// <summary/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceOut(source, eventType, id, message);
        }

        /// <summary/>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            TraceOut(source, eventType, id, String.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary/>
        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            TraceOut(source, TraceEventType.Transfer, id, String.Format(CultureInfo.InvariantCulture, "Transfer Message:{0}\n ActivityID:{1}", message, relatedActivityId.ToString()));
        }

        /// <summary/>
        public override void Write(string message)
        {
            TraceOut("Trace.Write", TraceEventType.Information, 0, message);
        }

        /// <summary/>
        public override void WriteLine(string message)
        {
            TraceOut("Trace.WriteLine", TraceEventType.Information, 0, message);
        }

        #endregion


        #region Private class
        /// <summary>
        /// Represents errors arising from Trace Assertions
        /// </summary>
        [Serializable]
        private class AssertionException : Exception
        {
            public AssertionException(string message)
                : base(message)
            {

            }
            protected AssertionException(SerializationInfo info, StreamingContext context)
                : base(info,context)
            {                
            }

        }
        #endregion
    }
}
