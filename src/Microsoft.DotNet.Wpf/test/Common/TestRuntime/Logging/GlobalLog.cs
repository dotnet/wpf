// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Permissions;

namespace Microsoft.Test.Logging {

    /// <summary>
    /// Global Log that logs to the Current TestLog or the Global TestLog if one is not avalible
    /// </summary>
    /// <remarks>
    /// You should use the GlobalLog if you are writing a loader, common library of utility
    /// functionality and want to do logging.  Or if you want to do logging that is not directly related
    /// to a test result.
    /// </remarks>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static class GlobalLog {
        
        /// <summary>
        /// Logs an existing file with the result of the test
        /// </summary>
        /// <param name="filename">name of the file to log</param>
        /// <remarks>
        /// You should call this API if you want to add files to the logs for later analysis.
        /// </remarks>
        public static void LogFileDeferred(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (!File.Exists(filename))
                throw new FileNotFoundException("The specified file could not be found", filename);

            LogManager.LogFileDangerously(new FileInfo(filename));
        }

        /// <summary>
        /// Creates a new file to log with the result of the test using the specified filename
        /// </summary>
        /// <param name="filename">filename of the file to log</param>
        /// <remarks>
        /// You should use this API if you have data you want to log in a file
        /// but do not want to write it to disk.  This can be useful when running
        /// in a secure environment.
        /// </remarks>
        public static void LogFile(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (!File.Exists(filename))
                throw new FileNotFoundException("The specified file could not be found", filename);

            LogManager.LogFileDangerously(new FileInfo(filename));
        }

        /// <summary>
        /// Creates a new file to log with the result of the test using the specified stream and filename
        /// </summary>
        /// <param name="filename">filename of the file to log</param>
        /// <param name="stream">stream containing the contents of the file</param>
        /// <remarks>
        /// You should use this API if you have data you want to log in a file
        /// but do not want to write it to disk.  This can be useful when running
        /// in a secure environment.
        /// </remarks>
        public static void LogFile(string filename, Stream stream)
        {
            FileInfo info = new FileInfo(filename);
            FileStream file = info.Open(FileMode.Create, FileAccess.Write);

            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            file.Write(buffer, 0, (int)stream.Length);
            file.Close();

            LogManager.LogFileDangerously(info);
        }

        /// <summary>
        /// Logs a Status trace to the Current or Global TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <remarks>
        /// Status are traces that indicate what the test is currently doing.
        /// You should regularly log status traces when you are doing something that may potentially be volitile.
        /// If something unexpected happens the last status logged is avalible by reading the LastStatus property.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogStatus(string message) {
            LogManager.LogMessageDangerously(message);
        }

        /// <summary>
        /// Logs a Status trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>
        /// <remarks>
        /// Status are traces that indicate what the test is currently doing.
        /// You should regularly log status traces when you are doing something that may potentially be volitile.
        /// If something unexpected happens the last status logged is avalible by reading the LastStatus property.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogStatus(string message, params object[] args)
        {
            LogManager.LogMessageDangerously(string.Format(message, args));
        }

        /// <summary>
        /// Logs a Debug trace to the Current or Global TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <remarks>
        /// Debug are traces that you only want to see if running in a debug mode.
        /// You can enable debug logging by setting the Harness["Debug"] property.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogDebug(string message) {
            LogManager.LogMessageDangerously(message);
        }

        /// <summary>
        /// Logs a Debug trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>
        /// <remarks>
        /// Debug are traces that you only want to see if running in a debug mode.
        /// You can enable debug logging by setting the Harness["Debug"] property.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogDebug(string message, params object[] args)
        {
            LogManager.LogMessageDangerously(string.Format(message, args));
        }

        /// <summary>
        /// Logs a Comment Trace to the Current or Global TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <remarks>
        /// Comments are the summary result of what the test did.
        /// You should log actions your test has done and results of validation as comments.
        /// This comments are agregated together and availible via the Comments property.
        /// It is not nessesary to log a commont indicating that the Test Passed as this is done for you.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogEvidence(string message)
        {
            LogManager.LogMessageDangerously(message);
        }

        /// <summary>
        /// Logs a Comment Trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>
        /// <remarks>
        /// Comments are the summary result of what the test did.
        /// You should log actions your test has done and results of validation as comments.
        /// This comments are agregated together and availible via the Comments property.
        /// It is not nessesary to log a commont indicating that the Test Passed as this is done for you.
        /// </remarks>
        [LoggingSupportFunction]
        public static void LogEvidence(string message, params object[] args)
        {
            LogManager.LogMessageDangerously(string.Format(message, args));
        }

        /// <summary>
        /// Logs an exception trace to the Current or Global TestLog
        /// </summary>
        /// <param name="exception">excpetion to log</param>        
        [LoggingSupportFunction]
        public static void LogEvidence(Exception exception)
        {
            LogManager.LogMessageDangerously(exception.ToString());
        }

        /// <summary>
        /// Logs an exception trace along with a message to the Current or Global TestLog
        /// </summary>
        /// <param name="exception">exception to log</param>
        /// <param name="message">exception to log</param>
        [LoggingSupportFunction]
        public static void LogEvidence(Exception exception, string message)
        {
            LogManager.LogMessageDangerously(message);
            LogManager.LogMessageDangerously(exception.ToString());
        }

    }

}

