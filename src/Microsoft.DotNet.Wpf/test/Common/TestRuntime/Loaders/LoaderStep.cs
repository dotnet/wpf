// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.Test.Loaders 
{

    /// <summary>
    /// Abstract base class for implementing LoaderSteps to customize execution of the AppMonitor Loader
    /// </summary>
    
    public abstract class LoaderStep 
    {

        #region Private Data

        // Whether a step should be run despite the return value of previous steps
        bool alwaysRun = false;
        // Reference to the parent step for accessing public properties.
        LoaderStep parentStep = null;
        // List of child steps that will be executed in order when step is executed.
        List<LoaderStep> childSteps = new List<LoaderStep>();

        #endregion
        
        #region Constructors

        /// <summary>
        /// Creates a new instance of the LoaderStep
        /// </summary>
        protected LoaderStep() 
        {
        }

        #endregion
        
        #region Public Members

        /// <summary>
        /// Gets or sets a value indicating if the Step should be run even if steps that execute before it fail
        /// </summary>
        /// <value>Boolean value indicating if the Step should be run even if steps that execute before it fail</value>
        public bool AlwaysRun 
        {
            get { return alwaysRun; }
            set { alwaysRun = value; }
        }

        /// <summary>
        /// Gets the Parent LoaderStep for this Step
        /// </summary>
        /// <value>The Parent LoaderStep (if one exists) otherwise null</value>
        public LoaderStep ParentStep 
        {
            get { return parentStep; }
        }

        /// <summary>
        /// Begins the Step for asynchronous execution
        /// </summary>
        /// <returns>Returns true if the child steps should be executed, otherwise, false</returns>
        /// <remarks>
        /// You should override this Method if you want to enable substeps that happen within the step.
        /// </remarks>
        protected virtual bool BeginStep() 
        {
            return true;
        }

        /// <summary>
        /// Ends the Step for asynchronous execution
        /// </summary>
        /// <returns>Returns true if the subsequent steps should be executed, otherwise, false</returns>
        /// <remarks>
        /// You should override this Method if you want to enable substeps that happen within the step.
        /// </remarks>
        protected virtual bool EndStep() 
        {
            return true;
        }

        /// <summary>
        /// Performs the step behavior
        /// </summary>
        /// <returns>returns true if the rest of the steps should be executed, otherwise, false</returns>
        public virtual bool DoStep() 
        {
            bool runChildSteps = BeginStep();
            if (runChildSteps) 
            {
                //if false then only run steps that are marked AlwaysRun
                bool runAnyStep = true;  
                foreach (LoaderStep step in childSteps) 
                {
                    if (runAnyStep || step.AlwaysRun)
                        runAnyStep &= step.DoStep();
                }
            }
            return EndStep();
        }

        /// <summary>
        /// Adds a Step as a Child to this Step.
        /// </summary>
        /// <param name="step">the step to add to as a child</param>
        /// <remarks>
        /// Child Steps are invoked when the Step in implemented asynchronously using BeginStep and EndStep
        /// </remarks>
        public void AddChildStep(LoaderStep step) 
        {
            step.parentStep = this;
            childSteps.Add(step);
        }

        #endregion
        
    }
}
