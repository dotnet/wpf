// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Collections.Generic;

using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Summary description for Class1.
    /// Distinct in terms of -
    /// Knows what commandline arguements element means.
    /// Checks if a Project element is there
    /// <remarks>Should I enforce that Import element is required?</remarks>
    /// </summary>
    public class ProjVariationGenerator : XmlFileVariationGenerator
    {
        #region Member variables

        private string commandlineargs = null;
        private string warningcode = null;
        private string errorcode = null;
        private bool bisprojectfile = false;
        private bool _bgenonly = false;

        const string GenerateOnlyElement = "GenerateOnly";

        #endregion Member variables

        #region Public API's

        /// <summary>
        /// Constructor, Initialize variations list.
        /// </summary>
        public ProjVariationGenerator()
            : base()
        {
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Reads Project file specific element information including Warning and Error codes and if an 
        /// element has CommandlineArgs set to true.
        /// </summary>
        /// <param name="actualnode"></param>
        /// <param name="modifiednode"></param>
        /// <param name="attributestable"></param>
        protected override void NodeVariationApplied(XmlNode actualnode, XmlNode modifiednode, Hashtable attributestable)
        {
            // Now it becomes much more easier to do what i need to do.
            if (actualnode == null || modifiednode == null)
            {
                return;
            }

            if (attributestable.Count == 0)
            {
                return;
            }

            // First read Hashtable for commandline, warning and error attributes.
            IDictionaryEnumerator enumerator = attributestable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Key == null)
                {
                    continue;
                }

                switch (enumerator.Key.ToString())
                {
                    case Constants.CommandLineArgAttribute:
                        if (String.IsNullOrEmpty(enumerator.Value.ToString()))
                        {
                            continue;
                        }

                        if (Convert.ToBoolean(enumerator.Value.ToString()))
                        {
                            // Check if this is a element is part of a property group.
                            XmlNode traversenode = actualnode;

                            do
                            {
                                if (traversenode.Name.ToLowerInvariant() == "propertygroup")
                                {
                                    RecurseSubTree(modifiednode);
                                    break;
                                }

                                traversenode = traversenode.ParentNode;

                            } while (traversenode.ParentNode != null);
                        }

                        break;

                    case Constants.WarningCodeAttribute:
                        if (String.IsNullOrEmpty(warningcode))
                        {
                            warningcode += enumerator.Value.ToString();
                        }
                        else
                        {
                            warningcode += "," + enumerator.Value.ToString();
                        }
                        break;

                    case Constants.ErrorCodeAttribute:
                        if (String.IsNullOrEmpty(errorcode))
                        {
                            errorcode += enumerator.Value.ToString();
                        }
                        else
                        {
                            errorcode += "," + enumerator.Value.ToString();
                        }
                        break;
                }

                enumerator.MoveNext();
            }


            base.NodeVariationApplied(actualnode, modifiednode, attributestable);

        }

        /// <summary>
        /// Read Default values in the Scenarios section for project specific elements.
        /// </summary>
        /// <param name="node"></param>
        protected override void ReadDefaults(XmlNode node)
        {
            switch (node.Name)
            {
                case Constants.CommandLineArgsElement:
                    this.commandlineargs = node.InnerText;
                    break;

                case GenerateOnlyElement:
                    if (String.IsNullOrEmpty(node.InnerText) == false)
                    {
                        _bgenonly = Convert.ToBoolean(node.InnerText);
                    }
                    break;
            }

            base.ReadDefaults(node);
        }

        /// <summary>
        /// If a Commandline Args, ErrorCode or ErrorWarning element is found in the Scenario element
        /// process them.
        /// </summary>
        /// <param name="unrecognizednode"></param>
        /// <returns></returns>
        protected override bool ProcessUnRecognizedElements(XmlNode unrecognizednode)
        {
            if (unrecognizednode == null)
            {
                return false;
            }

            switch (unrecognizednode.Name)
            {
                case Constants.CommandLineArgsElement:
                    this.commandlineargs = unrecognizednode.InnerText;
                    return true;

                case GenerateOnlyElement:
                    if (String.IsNullOrEmpty(unrecognizednode.InnerText) == false)
                    {
                        _bgenonly = Convert.ToBoolean(unrecognizednode.InnerText);
                    }
                    return true;
            }

            return base.ProcessUnRecognizedElements(unrecognizednode);
        }


        /// <summary>
        /// Updating file with Project specific data.
        /// </summary>
        /// <param name="finalxmldocument"></param>
        [CLSCompliant(false)]
        protected override void GenerateFile(ref XmlDocumentSW finalxmldocument)
        {
            if (finalxmldocument == null)
            {
                return;
            }

            if (finalxmldocument.DocumentElement.Name.ToLowerInvariant() == "project")
            {
                string commentdata = " Generated by XVariation. \n";
                commentdata += "Project File Commandline arguments = " + commandlineargs + "\n";
                commentdata += "Project File Expected Warnings Codes = " + warningcode + "\n";
                commentdata += "Project File Expected Errors Codes = " + errorcode + "\n";

                XmlComment commentelement = finalxmldocument.CreateComment(commentdata);
                finalxmldocument.AppendChild(commentelement);

                bisprojectfile = true;
            }

            base.GenerateFile(ref finalxmldocument);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Hmm need to review why this was created.
        /// </summary>
        /// <param name="element"></param>
        private void RecurseSubTree(XmlNode element)
        {
            if (element == null)
            {
                return;
            }

            switch (element.Name)
            {
                case "Property":
                    // Dang, i have to check which variation this is.
                    // This is for old Property handling.
                    if (element.Attributes.Count == 1)
                    {
                        commandlineargs += element.Attributes[0].Name + "=" + element.Attributes[0].Value + ";";
                    }
                    break;

                default:
                    if (element.HasChildNodes)
                    {
                        for (int i = 0; i < element.ChildNodes.Count; i++)
                        {
                            RecurseSubTree(element.ChildNodes[i]);
                        }
                    }
                    break;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Generated Project file info which includes, error, warnings and commandline options.
        /// </summary>
        /// <value></value>
        public VariationFileInfo GeneratedFileInfo
        {
            get
            {
                VariationFileInfo temp = new VariationFileInfo();
                if (FileSW.Exists(this.generatedFile))
                {
                    temp.commandlineoptions = this.commandlineargs;
                    char[] separator = new char[] { ',' };

                    if (String.IsNullOrEmpty(this.errorcode) == false)
                    {
                        temp.errorcode = this.errorcode.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (String.IsNullOrEmpty(this.warningcode) == false)
                    {
                        temp.warningcode = this.warningcode.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    }

                    temp.filename = PathSW.GetFullPath(this.generatedFile);
                    temp.bprojfile = bisprojectfile;
                    temp._bgenerateonly = _bgenonly;

                    return temp;
                }

                return temp;
            }
        }

        #endregion

    }
}