// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Helper to determine files or assemblies "names" that were created with the FileVariation Engine.
    /// </summary>
    public class GeneratedFilesHelper
    {
        #region Private Member variables

        static string[] _generatedfiles = null;
        static Hashtable _generatedassembliestable = null;

        #endregion

        #region Public Methods
        /// <summary>
        /// List of assemblies that were generated in current execution.
        /// </summary>
        /// <param name="projectfilename"></param>
        /// <param name="assemblynamewithpath"></param>
        public static void AddGeneratedAssembly(string projectfilename, string assemblynamewithpath)
        {
            if (String.IsNullOrEmpty(projectfilename) || String.IsNullOrEmpty(assemblynamewithpath))
            {
                return;
            }

            if (_generatedassembliestable == null)
            {
                _generatedassembliestable = new Hashtable();
            }

            GeneratedAssemblyInfo _generatedassemblyinfo = new GeneratedAssemblyInfo();
            _generatedassemblyinfo.GeneratedAssembly = assemblynamewithpath;
            _generatedassemblyinfo.ProjectFile = projectfilename;

            if (_generatedassembliestable.ContainsValue(_generatedassemblyinfo))
            {
                _generatedassembliestable.Remove(projectfilename);
            }

            _generatedassembliestable.Add(_generatedassembliestable.Count + 1, _generatedassemblyinfo);
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GeneratedFiles
        {
            set
            {
                _generatedfiles = value.Split(',');
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Helper to replace {GeneratedFile[Relative|Absolute]:#} with actual file name.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        internal string GetGeneratedFileName(string text, XmlNode node)
        {
            if (_generatedfiles == null)
            {
                return null;
            }

            int index = text.IndexOf(':');
            if (index < 0)
            {
                return null;
            }

            text = text.Substring(index + 1);
            index = Convert.ToInt16(text);

            if (index == 0)
            {
                return _generatedfiles[index];
            }

            if (index - 1 >= _generatedfiles.Length)
            {
                return null;
            }

            return _generatedfiles[index - 1];
        }

        /// <summary>
        /// Return the Generated Assembly from the list.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        internal string GetGeneratedAssembly(string text, XmlNode node)
        {
            int index = -1;
            string filetype = "withoutextension";

            if (text.Contains("[") || text.Contains("]"))
            {
                index = text.IndexOf("[");
                if (index < 0)
                {
                    index = text.IndexOf("]");
                    filetype = text.Substring(0, index);
                }
                else
                {
                    if (text.Contains("]"))
                    {
                        filetype = text.Substring(index + 1, text.IndexOf("]") - index - 1);
                    }
                }
            }

            index = text.IndexOf(':');
            if (index < 0)
            {
                return null;
            }

            text = text.Substring(index + 1, text.Length - index - 1);
            index = Convert.ToInt16(text);

            if (_generatedassembliestable.Contains(index))
            {
                GeneratedAssemblyInfo asseminfo = (GeneratedAssemblyInfo)_generatedassembliestable[index];
                text = asseminfo.GeneratedAssembly;
            }

            if (filetype.ToLowerInvariant() == "withoutextension")
            {
                text = PathSW.GetFileNameWithoutExtension(text);
            }

            return text;
        }

        #endregion
    }
    
    struct GeneratedAssemblyInfo
    {
        public string GeneratedAssembly;
        public string ProjectFile;
    }

}
