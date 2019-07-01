// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Helper Struct that stores Project file specific variation data.
    /// </summary>
    public struct VariationFileInfo
    {
        #region Member variables
        internal string filename;
        internal string commandlineoptions;

        internal string[] errorcode;
        internal string[] warningcode;

        internal bool bprojfile;
        internal bool _bgenerateonly;
        #endregion

        #region Public memember
        /// <summary>
        /// Project File name.
        /// </summary>
        /// <value></value>
        public string FileName
        {
            set
            {
                if (String.IsNullOrEmpty(value) == false)
                {
                    filename = value;
                }
            }
            get
            {
                return filename;
            }
        }

        /// <summary>
        /// Project file commandline options.
        /// </summary>
        /// <value></value>
        public string CommandlineOptions
        {
            set
            {
                if (String.IsNullOrEmpty(value) == false)
                {
                    commandlineoptions = value;
                }
            }
            get
            {
                return commandlineoptions;
            }
        }

        /// <summary>
        /// Error codes that the file has to ignore.
        /// </summary>
        /// <value></value>
        public string[] ErrorCodes
        {
            get
            {
                if (errorcode == null)
                {
                    errorcode = new string[] {};
                }
                return errorcode;
            }
            set
            {
                errorcode = value;
            }
        }

        /// <summary>
        /// Warning codes that the file has to ignore.
        /// </summary>
        /// <value></value>
        public string[] WarningCodes
        {
            get
            {
                if (warningcode == null)
                {
                    warningcode = new string[] {};
                }
                return warningcode;
            }
            set
            {
                warningcode = value;
            }
        }

        /// <summary>
        /// Is current file a project file.
        /// </summary>
        /// <value></value>
        public bool IsProjectFile
        {
            get
            {
                return bprojfile;
            }
            set
            {
                bprojfile = value;
            }
        }

        /// <summary>
        /// Generate the file and do not compile if Project file.
        /// </summary>
        public bool GenerateOnly
        {
            get
            {
                return _bgenerateonly;
            }
        }
        #endregion
    }
}
