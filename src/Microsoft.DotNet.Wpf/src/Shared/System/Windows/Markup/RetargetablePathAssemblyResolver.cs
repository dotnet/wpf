// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MS.Internal.Markup
{
    public class RetargetablePathAssemblyResolver : MetadataAssemblyResolver
    {
        private PathAssemblyResolver _pathAssemblyResolver; 

        public RetargetablePathAssemblyResolver(IEnumerable<string> assemblyPaths)
        {
            _pathAssemblyResolver = new PathAssemblyResolver(assemblyPaths);
        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            // PathAssemblyResolver will resolve the target assembly to the highest 
            // version of the target assembly available in the 'assemblyPaths' assembly
            // list, only if the public key token for the target assembly is not set.
            // Remove the public key token from 'mscorlib' to allow PathAssemblyResolver
            // to resolve the most recent version of 'mscorlib'. 
            if (assemblyName.Name.Equals(ReflectionHelper.MscorlibReflectionAssemblyName))
            {
                assemblyName.SetPublicKeyToken(null);
            }

            return _pathAssemblyResolver.Resolve(context, assemblyName);
        }
    }
}
