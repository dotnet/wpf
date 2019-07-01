// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    public class DispatcherFactory : DiscoverableFactory<Dispatcher>
    {
        #region Public Members

        public int DispatcherIndex { get; set; }

        #endregion

        #region Override Members

        public override Dispatcher Create(DeterministicRandom random)
        {
            lock (Dispatchers)
            {
                if (!Dispatchers.Contains(Dispatcher.CurrentDispatcher))
                {
                    //Save all Dispatchers, then we can get Dispatchers of another threads. 
                    Dispatchers.Add(Dispatcher.CurrentDispatcher);
                }

                return Dispatchers[DispatcherIndex % Dispatchers.Count];
            }
        }

        #endregion

        #region Private Data

        private static List<Dispatcher> Dispatchers = new List<Dispatcher>();

        #endregion
    }
}
