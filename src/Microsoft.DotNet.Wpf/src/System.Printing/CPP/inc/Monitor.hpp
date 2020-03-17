// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __MONITOR_HPP__
#define __MONITOR_HPP__

namespace System
{
namespace Printing
{
    __gc public class Monitor :
    public PrintSystemObject
    {
        private public: 

        Monitor(
            String*    monitorName
            );
       
        ~Monitor(
            void
            );
    };
}
}

#endif
