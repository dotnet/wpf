// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Analogous to DispatcherOperation - one unit of cross-thread work.
//

using System.Windows.Threading;

namespace MS.Internal.Data
{
    internal class DataBindOperation
    {
        public DataBindOperation(DispatcherOperationCallback method, object arg, int cost = 1)
        {
            _method = method;
            _arg = arg;
            _cost = cost;
        }

        public int Cost
        {
            get { return _cost; }
            set { _cost = value; }
        }

        public void Invoke()
        {
            _method(_arg);
        }

        private DispatcherOperationCallback _method;
        private object _arg;
        private int _cost;
    }
}
