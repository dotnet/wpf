// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// Pipes logger output to the console in human centric text form.
    /// </summary>
    public class ConsoleLogger : LoggerBase
    {
        #region ILogger Members
        /// <summary/>        
        public override void BeginTest(string name)
        {
            indentLevel = 1;
            PrintMessage("********* Starting Test: " + name + " *********", testColor);
        }

        /// <summary/>
        public override void EndTest(string name)
        {
            indentLevel = 1;
            PrintMessage("********* Ending Test: " + name + " *********", testColor);
        }

        /// <summary/>
        public override void BeginVariation(string name)
        {
            indentLevel = 2;
            PrintMessage("********* Begin Variation: " + name + " *********", variationColor);
            indentLevel = 3;
        }

        /// <summary/>
        public override void EndVariation(string name)
        {
            indentLevel = 3;
            PrintMessage("********* End Variation  : " + name + " *********", variationColor);
            indentLevel = 2;
        }

        /// <summary/>
        public override void LogFile(string filename)
        {
            PrintMessage("File logged: " + filename, fileColor);
        }

        /// <summary/>
        public override void LogMessage(string message)
        {
            PrintMessage(message, ConsoleColor.White);
        }

        /// <summary/>
        public override void LogObject(object payload)
        {
            PrintMessage(payload.ToString(), messageColor);
        }

        /// <summary/>
        public override void LogResult(Result result)
        {
            PrintResult(result);
        }

        /// <summary/>
        public override void LogProcessCrash(int processId)
        {
            indentLevel = 1;
            PrintMessage("********* Test Crashed *********", testColor);
        }

        /// <summary/>
        public override void LogProcess(int ProcessId)
        {
            //NOP
        }

        #endregion

        #region Private Methods

        private void PrintMessage(string message, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message.PadLeft(indentLevel * 2));
            Console.ForegroundColor = originalColor;
        }

        private void PrintResult(Result result)
        {
            string message = string.Format(CultureInfo.InvariantCulture, "Variation Result: {0}",  result.ToString());
            ConsoleColor color;
            switch (result)
            {
                case Result.Pass:
                    color = passColor;
                    break;
                case Result.Fail:
                    color = failColor;
                    break;
                case Result.Ignore:
                    color = ignoreColor;
                    break;
                default:
                    throw new NotImplementedException();
            }
            PrintMessage("***************************************", color);
            PrintMessage(message, color);
            PrintMessage("***************************************", color);
        }
        #endregion

        #region Private Fields

        private int indentLevel;

        //The fields below are private, so safely consted (no downside here). These fields can be modified.
        //If the team adopts a standard color coding, these should follow it. 
        //Note that the Color scheme can be down mapped from an RGB representation color:
        // bit fields: 1==blue, 2==green, 4== red, 8==high brightness
        private const ConsoleColor passColor = ConsoleColor.Green;
        private const ConsoleColor failColor = ConsoleColor.Red;
        private const ConsoleColor ignoreColor = ConsoleColor.DarkGreen;
        private const ConsoleColor messageColor = ConsoleColor.White;
        private const ConsoleColor fileColor = ConsoleColor.Cyan;
        private const ConsoleColor variationColor = ConsoleColor.Yellow;
        private const ConsoleColor testColor = ConsoleColor.DarkCyan;

        #endregion
    }
}
