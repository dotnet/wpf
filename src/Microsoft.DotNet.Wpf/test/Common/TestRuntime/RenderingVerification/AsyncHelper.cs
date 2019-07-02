// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    internal static class AsyncHelper
    {
        private static List<AsyncData> _data = new List<AsyncData>();
        internal static List<AsyncData> Data
        {
            get { return _data; }
        }

        public static WaitHandle[] WaitHandles
        {
            get
            {
                WaitHandle[] waitHandles = new WaitHandle[_data.Count];
                for (int t = 0; t < _data.Count; t++)
                {
                    waitHandles[t] = ((IAsyncResult)_data[t].Result).AsyncWaitHandle;
                }
                return waitHandles;
            }
        }
        public static ImageComparisonResult[] Results
        {
            get 
            {
                ImageComparisonResult[] retVal = new ImageComparisonResult[_data.Count];
                for (int t = 0; t < _data.Count; t++)
                {
                    retVal[t] = _data[t].Result;
                }

                return retVal; 
            }
        }
        public static void ClearAll()
        {
            for (int t = 0; t < _data.Count; t++) 
            { 
                ((IAsyncResult)_data[t].Result).AsyncWaitHandle.Close();
                _data[t].Dispose();
            }
            _data.Clear();
        }
    }
}
