// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System.Reflection;
using System.Windows.Resources;
using System.Windows.Navigation;

namespace MS.Internal.Resources
{
    // <summary>
    //  ContentFileHelper class provides helper method to get assembly 
    //  associated content files.
    // </summary>
    internal static class ContentFileHelper
    {
        internal static bool IsContentFile(ReadOnlySpan<char> partName)
        {
            s_contentFiles ??= GetContentFiles(BaseUriHelper.ResourceAssembly);
            if (s_contentFiles is not null && s_contentFiles.Count > 0)
            {
                if (s_contentFiles.GetAlternateLookup<ReadOnlySpan<char>>().Contains(partName))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        //
        // Get a list of Content Files for a given Assembly.
        //
        static internal HashSet<string> GetContentFiles(Assembly asm)
        {
            HashSet<string> contentFiles = null;

            Attribute[] assemblyAttributes;

            if (asm == null)
            {
                asm = BaseUriHelper.ResourceAssembly;
                if (asm == null)
                {
                    // If we have no entry assembly return an empty list because
                    // we can't have any content files.
                    return new HashSet<string>();
                }
            }

            assemblyAttributes = Attribute.GetCustomAttributes(
                                   asm,
                                   typeof(AssemblyAssociatedContentFileAttribute));

            if (assemblyAttributes != null && assemblyAttributes.Length > 0)
            {
                contentFiles = new HashSet<string>(assemblyAttributes.Length, StringComparer.OrdinalIgnoreCase);

                for (int i=0; i<assemblyAttributes.Length; i++)
                {
                    AssemblyAssociatedContentFileAttribute aacf;

                    aacf = (AssemblyAssociatedContentFileAttribute) assemblyAttributes[i];
                    contentFiles.Add(aacf.RelativeContentFilePath);
                }
            }

            return contentFiles;
        }

        private static HashSet<string> s_contentFiles;
    }
}
