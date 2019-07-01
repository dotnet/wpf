// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// Summary description for Logger.
    /// </summary>
    internal class Logger
    {
        static Logger singleinstance = null;
        static bool bconsolelog = true;

        public Logger(bool initializectr, bool usecurrentlogger)
        {
            if (initializectr)
            {
                CTRLogger ctr = new CTRLogger();
            }

            CheckConsole();

            singleinstance = this;
        }

        public static Logger LoggerInstance
        {
            get
            {
                return singleinstance ;                                
            }
        }

        public string Stage
        {
            set
            {
                // Not actually allowing the build class to do this... this now happens at the AMC level
                // Log it regardless.
                GlobalLog.LogEvidence(String.Format("Stage = {0}", value));
            }
        }

        public void PrintAsBlock(string message)
        {
            int windowwidth = 0;

            if (bconsolelog)
            {
                try
                {
                    windowwidth = (Console.WindowWidth / 2) - message.Length ;
                }
                catch
                {
                    windowwidth = 5;
                    bconsolelog = false;
                }
            }
            else
            {
                windowwidth = 5;
            }

            string box = null;
            for (int i = 0; i < windowwidth - 5; i++)
            {
                box += "-";
            }

            box += "\t";

            LogComment(box + message + box);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void CheckConsole()
        {
            if (bconsolelog == false)
            {
                return;
            }

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            catch
            {
                bconsolelog = false;
            }

            if (bconsolelog)
            {
                Console.ResetColor();
            }
        }

        public void LogComment(string comment)
        {
            GlobalLog.LogEvidence(comment);
        }

        public void Result(bool passed)
        {
            if (passed)
            {
                TestLog.Current.Result = TestResult.Pass;
            }
            else
            {
                TestLog.Current.Result = TestResult.Fail;
            }
        }

        public string Log
        {
            set
            {
                if (CTRLogger.Logger != null)
                {
                    CTRLogger.Logger.Log = value;
                }

                Microsoft.Test.MSBuildEngine.MSBuildEngineCommonHelper.Log = value;
            }
        }

        public string LogError
        {
            set
            {
                if (CTRLogger.Logger != null)
                {
                    CTRLogger.Logger.LogError = value;
                }
                
                MSBuildEngine.MSBuildEngineCommonHelper.LogError = value;                
            }
        }

        public void DisplayExceptionInformation(Exception ex)
        {
            this.LogError = "";
            this.LogError = "\t" + ex.GetType().Name + " Occured";
            this.LogError = "\t" + "Message - \n\t   " + ex.Message.ToString();

            this.LogError = "\t" + ex.Source.ToString();
            this.LogError = "\t" + ex.StackTrace.ToString();
            if (ex.InnerException != null)
            {
                this.LogError = "\t" + ex.InnerException.ToString();
            }
        }

        //public void Close()
        //{
        //    if (CTRLogger.Logger != null)
        //    {
        //        CTRLogger.Logger.Close();
        //    }
        //}

        public void Save(string filename)
        {
            if (CTRLogger.Logger != null)
            {
                CTRLogger.Logger.Save(filename);
            }
        }

        public string this[string propertyvalue]
        {
            set
            {
                if (CTRLogger.Logger != null)
                {
                    CTRLogger.Logger[propertyvalue] = value;
                }
            }
            get
            {
                if (CTRLogger.Logger != null)
                {
                    return CTRLogger.Logger[propertyvalue];
                }

                return null;
            }
        }
    }
}

namespace Microsoft.Test.Utilities.VariationEngine
{
	/// <summary>
	/// Summary description for Macros.
	/// </summary>
	public static class UtilsLogger
	{
		private static bool bDebug = false;

		static DebugMode debugmode;

		/// <summary>
		/// Method that formats exception information.
		/// </summary>
		/// <param name="ex"></param>
		public static void DisplayExceptionInformation(Exception ex)
		{
			//Console.ForegroundColor = ConsoleColor.Red;
			//Console.WriteLine();
		    LogError = ex.Message.ToString();
			if (debugmode != DebugMode.Quiet)
			{
                Console.WriteLine("{0} Occured", ex.GetType().Name);
                Console.WriteLine(ex.Source.ToString());
                Console.WriteLine(ex.StackTrace.ToString());
				if (ex.InnerException != null)
				{
					Console.WriteLine(ex.InnerException.ToString());
				}
			}
			//Console.ResetColor();
		}

		internal static string Log
		{
			set
			{
				if (bDebug)
				{
					Console.WriteLine("Log - {0}", value);
				}
			}
		}

		internal static string LogError
		{
			set
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(value);
				Console.ResetColor();
			}
		}

		internal static string LogDiagnostic
		{
			set
			{
				if (debugmode == DebugMode.Diagnoistic)
				{
					Log = "\t" + value;
				}
			}
		}

		/// <summary>
		/// Public property to set Debug options.
		/// </summary>
		/// <value></value>
		public static DebugMode Debug
		{
			set
			{
				debugmode = value;
			}
		}

	}
	
	/// <summary>
	/// Debug Mode options.
	/// </summary>
	public enum DebugMode
	{
		/// <summary>
		/// Minimal information displayed. Only errors
		/// </summary>
		Quiet,
		/// <summary>
		/// Detailed information displayed.
		/// </summary>
		Verbose,
		/// <summary>
		/// Stack like information displayed.
		/// </summary>
		Diagnoistic
	}
}


