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
    /// A Variation is the fundamental logging construct, providing means to
    /// log files, object, messages, and a result.
    /// </summary>
    public class Variation : IDisposable
    {
        #region Private Data

        private ILogger logger;
        private bool useExistingVariation = false;

        #endregion

        #region Constructors

        /// <summary>
        /// A static ctor is called the first time the type if referred to.
        /// So, for example, if a test refers to Variation.Current, this ctor will
        /// get called first. In the ctor we query LogManager.Instance, which
        /// if non-null means the logging system is initalized. (Calling
        /// LogManager.Instance fires it up) LogManager is responsible for
        /// wiring up Log and Variation if they have already been started up in
        /// a different process. The result of which is that if a blind process
        /// refers to Variation.Current without first doing anything else, we will
        /// auto-initialize and Variation.Current will be non-null.
        /// </summary>
        static Variation()
        {
            if (LogManager.Instance == null)
            {
                throw new InvalidOperationException("Was not able to initialize LogManager.");
            }
        }

        internal Variation(string name, ILogger logger)
        {
            bool useExistingVariation = logger.GetCurrentVariationName() != null;

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", "name");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (Current != null)
            {
                throw new InvalidOperationException("Variation cannot be constructed when there is already a current variation.");
            }

            this.Name = name;
            this.logger = logger;
            this.useExistingVariation = useExistingVariation;
            Current = this;

            if (!useExistingVariation)
            {
                logger.BeginVariation(name);
            }
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Close down a Variation. A result should be logged beforehand.
        /// </summary>
        public void Close()
        {
            if (logger.GetCurrentVariationResult() == null)
            {
                throw new InvalidOperationException("A variation should not be closed without first logging a result.");
            }

            Current = null;

            if (!useExistingVariation)
            {
                logger.EndVariation(Name);
            }
            else
            {
                throw new InvalidOperationException("A variation should not be closed if it was created on a parent process");
            }

            if (VariationClosed != null)
            {
                VariationClosed(this, null);
            }
        }

        /// <summary>
        /// IDisposable implementation to support 'using' pattern. Calls Close().
        /// </summary>
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Logs a file for storage in the result directory.
        /// The file copy is executed after the test exits
        /// to avoid file locking problems.
        /// </summary>
        public void LogFile(FileInfo fileInfo)
        {            
            logger.LogFile(fileInfo.FullName);
        }

        /// <summary>
        /// Logs a string to the variation's log file
        /// Can be formatted
        /// </summary>
        /// <param name="message">The message to be logged</param>
        /// <param name="args">The objects to complete the format of the string (Optional)</param>
        public void LogMessage(string message, params object[] args)
        {
            if (args != null)
            {
                message = String.Format(CultureInfo.InvariantCulture, message, args);
            }
            logger.LogMessage(message);
        }

        /// <summary>
        /// Saves an object in the variation's log file
        /// </summary>
        public void LogObject(Object value)
        {
            logger.LogObject(value);
        }

        /// <summary>
        /// Logs the result of the current variation. Should call exactly once.
        /// </summary>
        public void LogResult(Result result)
        {
            if (logger.GetCurrentVariationResult() != null)
            {
                throw new InvalidOperationException("The result of a variation can only be logged once.");
            }
            else
            {
                logger.LogResult(result);
            }
        }

        /// <summary />
        public string Name { get; private set; }

        /// <summary />
        public Result? Result
        {
            get
            {
                return logger.GetCurrentVariationResult();
            }
        }

        #endregion

        #region Internal Members

        public delegate void VariationClosingEventHandler(object sender, EventArgs e);
        public event VariationClosingEventHandler VariationClosed;

        #endregion        

        #region Static Members

        /// <summary />
        public static Variation Current { get; internal set; }

        #endregion
    }
}
