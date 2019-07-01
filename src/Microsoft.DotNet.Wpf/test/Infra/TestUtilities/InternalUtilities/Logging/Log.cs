// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// The API that handles logging from a test point of view.
    /// Each log corresponds to a single test that may contain many
    /// Variations.
    /// </summary>
    public class Log : IDisposable
    {
        #region Private Data

        private ILogger logger;
        // In the case of when there is an existing log from the parent process,
        // we can note this fact and do not produce BeginTest/EndTest upon Log
        // creation/close in this child process.
        private bool useExistingTest = false;

        #endregion

        #region Constructors

        /// <summary>
        /// A static ctor is called the first time the type if referred to.
        /// So, for example, if a test refers to Log.Current, this ctor will
        /// get called first. In the ctor we query LogManager.Instance, which
        /// if non-null means the logging system is initalized. (Calling
        /// LogManager.Instance fires it up) LogManager is responsible for
        /// wiring up Log and Variation if they have already been started up in
        /// a different process. The result of which is that if a blind process
        /// refers to Log.Current without first doing anything else, we will
        /// auto-initialize and Log.Current will be non-null.
        /// </summary>
        static Log()
        {
            if (LogManager.Instance == null)
            {
                throw new InvalidOperationException("Was not able to initialize LogManager.");
            }
        }

        internal Log(string name, ILogger logger)
        {
            bool useExistingTest = logger.GetCurrentTestName() != null;

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
                throw new InvalidOperationException("Log cannot be constructed when there is already a current log.");
            }

            this.Name = name;
            this.logger = logger;
            this.useExistingTest = useExistingTest;
            Current = this;

            if (!this.useExistingTest)
            {
                logger.BeginTest(name);
            }
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Close down a Log. The current variation should be closed beforehand.
        /// </summary>
        public void Close()
        {
            if (Variation.Current != null)
            {
                throw new InvalidOperationException("A log should not be closed when a variation is still open.");
            }

            Current = null;
            if (!useExistingTest)
            {
                logger.EndTest(Name);
            }
        }

        /// <summary>
        /// Create a variation. The current variation should be closed beforehand.
        /// </summary>
        /// <param name="name">Name for the variation, preferably unique.</param>
        /// <returns>Constructed variation.</returns>
        public Variation CreateVariation(string name)
        {
            if (CurrentVariation != null)
            {
                throw new InvalidOperationException("You must end the previous variation before beginning a new one.");
            }
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", "name");
            }

            return new Variation(name, logger);
        }

        /// The current active variation.  This will be null if there is no active
        /// variation. This is a wrapper around Variation.Current, which is how you should
        /// access the current variation - this getter will be removed in the future.
        public Variation CurrentVariation { get { return Variation.Current; } }

        /// <summary />
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        /// <summary />
        public string Name { get; private set; }

        #endregion


        #region Static Members

        /// <summary />
        public static Log Current { get; internal set; }

        #endregion
    }
}
