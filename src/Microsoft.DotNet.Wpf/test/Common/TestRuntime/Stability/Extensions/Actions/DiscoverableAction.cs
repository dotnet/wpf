// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Discoverable Actions are actions which have a parameterless constructor and consume data via attribute
    /// Getter/setters
    /// </summary>
    public abstract class DiscoverableAction : IDiscoverableObject, IAction
    {
        /// <summary/>
        public DiscoverableAction()
        {
        }

        #region IAction Members

        /// <summary/>
        public abstract bool CanPerform();

        /// <summary/>
        public virtual void Perform(DeterministicRandom random)
        {
            Perform();
        }

        public abstract void Perform();

        #endregion
    }

    public abstract class SimpleDiscoverableAction : DiscoverableAction
    {
        /// <summary/>
        public SimpleDiscoverableAction()
        {
        }

        #region IAction Members

        /// <summary/>
        public override bool CanPerform()
        {
            return true;
        }

        #endregion
    }
}
