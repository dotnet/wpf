// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

///////////////////////////////////////////////////////////////////////////////
//
// Update application manifest for browser-hosted application.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Build.Tasks.Windows;
using System.Collections;

using MS.Utility;
using MS.Internal.Tasks;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691


namespace Microsoft.Build.Tasks.Windows
{
    #region Manifest Creator Task class

    /// <summary>
    /// Class of UpdateManifestForBrowserApplication
    /// </summary>
    public sealed class UpdateManifestForBrowserApplication : Task
    {

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Constructors
        /// </summary>
        public UpdateManifestForBrowserApplication()
            : base(SR.SharedResourceManager)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Add hostInBrowser element node to the application manifest file.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            bool successful = true;
            TaskHelper.DisplayLogo(Log, nameof(UpdateManifestForBrowserApplication));

            if (HostInBrowser != true)
            {
                // HostInBrowser is not true, don't modify the manifest.
                // Stop here.
                return successful;
            }

            try
            {
                string appManifestFile = ApplicationManifest[0].ItemSpec;
                XmlDocument manifestDocument;

                XmlTextReader manifestReader = null;
                XmlTextWriter manifestWriter = null;

                //Read the manifest
                try
                {
                    manifestReader = new XmlTextReader(appManifestFile);
                    manifestDocument = new XmlDocument();
                    manifestDocument.Load(manifestReader);
                }
                finally
                {
                    if (manifestReader != null)
                    {
                        // Close the manifest reader
                        manifestReader.Close();
                    }
                }

                // NOTE:
                //
                // manifestReader must be closed before the manfiestWriter can
                // update the document on the same manifest file.
                //

                //Get to entryPoint XML
                XmlNodeList entryPointList = manifestDocument.GetElementsByTagName(c_entryPoint);
                XmlNode entryPoint = entryPointList[0];

                //Create element node for browser entry point
                XmlElement hostInBrowser;
                hostInBrowser = manifestDocument.CreateElement(c_hostInBrowser, c_asmv3);

                // Append HostInBrowser node to the end of list of children of entryPoint.
                entryPoint.AppendChild(hostInBrowser);

                // Update the manifest file.
                try
                {
                    manifestWriter = new XmlTextWriter(appManifestFile, System.Text.Encoding.UTF8);
                    manifestWriter.Formatting = Formatting.Indented;
                    manifestWriter.Indentation = 4;
                    manifestDocument.WriteTo(manifestWriter);
                }
                finally
                {
                    if (manifestWriter != null)
                    {
                        // Close the manifest writer
                        manifestWriter.Close();
                    }
                }

            }
            catch (Exception e)
            {
                // PreSharp Complaint 6500 - do not handle null-ref or SEH exceptions.
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
                else
                {
                    Log.LogErrorFromException(e);
                    successful = false;
                }
            }
#pragma warning disable 6500
            catch   // Non-cls compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                successful = false;
            }
#pragma warning restore 6500


            return successful;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Host In Browser
        /// </summary>
        /// <value></value>
        [Required]
        public bool HostInBrowser
        {
            get { return _hostInBrowser; }
            set
            {
                _hostInBrowser = value;
            }
        }

        /// <summary>
        /// Application Manifest File
        /// </summary>
        /// <value></value>
        [Required]
        public ITaskItem[] ApplicationManifest
        {
            get { return _applicationmanifest; }
            set { _applicationmanifest = value; }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ITaskItem[] _applicationmanifest;
        private bool        _hostInBrowser = false;

        //
        // Put some predefined element or attribute name in below
        // const strings.
        //
        private const string c_entryPoint = "entryPoint";
        private const string c_hostInBrowser ="hostInBrowser";
        private const string c_asmv3= "urn:schemas-microsoft-com:asm.v3";

        #endregion Private Fields

    }

    #endregion Manifest Creator Task class

}
