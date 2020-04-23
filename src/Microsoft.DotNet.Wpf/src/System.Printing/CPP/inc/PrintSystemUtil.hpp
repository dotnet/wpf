// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMUTIL_HPP__
#define __PRINTSYSTEMUTIL_HPP__

namespace System
{
namespace Printing
{
    private ref class InternalExceptionResourceManager : System::Resources::ResourceManager
    {
        public:
        
        InternalExceptionResourceManager(
            void
            );
    };
}
}

#endif
