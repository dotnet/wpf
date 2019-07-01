// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;
using System.Collections.Generic;
using System.Text;

using Microsoft.Test.Security.Wrappers;
using Microsoft.Test.Utilities.VariationEngine;
// using Microsoft.Test.Utilities.VariationEngine;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// Stores all Perf related results and outputs in Perf desired Xml format.
    /// </summary>
    class PerfResult
    {
        Hashtable subresults;
        string projectname;
        public static ArrayList TargetsToList;
        Hashtable perfresultslist = null;
        static string ReportNode = null;
        const string PerfDatumElement = "DATUM";
        bool bmainprojectfile = false;

        /// <summary>
        /// Static Constructor
        /// Read the file that contains the names of the Targets that needs to be reported 
        /// for perf analysis.
        /// </summary>
        static PerfResult()
        {
            TargetsToList = new ArrayList();

            // Get full path to filepath.
            string targetsfilepath = CommonHelper.VerifyFileExists(Constants.TargetsListFile);
            if (String.IsNullOrEmpty(targetsfilepath) == false)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(targetsfilepath);

                // Get the Targets element from the xmldocument and read the list. 
                // Only the first element is considered.
                XmlNodeList nodelist = doc.GetElementsByTagName(Constants.PerfTargetsElement);
                if (nodelist.Count > 0)
                {
                    for (int i = 0; i < nodelist[0].ChildNodes.Count; i++)
                    {
                        XmlNode targetnode = nodelist[0].ChildNodes[i];
                        if (TargetsToList.Contains(targetnode.Name.ToLowerInvariant()) == false)
                        {
                            if (targetnode.NodeType == XmlNodeType.Element)
                            {
                                if (targetnode.Attributes.Count > 0)
                                {
                                    if (targetnode.Attributes["Report"] != null && 
                                        String.IsNullOrEmpty(targetnode.Attributes["Report"].Value) == false)
                                    {
                                        if (Convert.ToBoolean(targetnode.Attributes["Report"].Value) == true)
                                        {
                                            if (String.IsNullOrEmpty(ReportNode))
                                            {
                                                ReportNode = targetnode.Name;
                                            }
                                        }
                                    }
                                }
                                TargetsToList.Add(targetnode.Name.ToLowerInvariant());
                                targetnode = null;
                            }
                        }
                    }
                }

                nodelist = null;
                doc = null;                
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmldatafile"></param>
        public bool ReadPerfResultsFromFile(string xmldatafile)
        {
            if (String.IsNullOrEmpty(xmldatafile))
            {
                return false;
            }

            FileStreamSW fs = new FileStreamSW(xmldatafile, System.IO.FileMode.Open);
            if (fs.Length == 0)
            {
                fs.Close();
                return false;
            }
            fs.Close();

            XmlDocumentSW xmldoc = new XmlDocumentSW();
            xmldoc.Load(xmldatafile);

            XmlNode subresultnode = xmldoc.CreateElement(Constants.PerfSubResultElement);

            XmlAttribute attrib = xmldoc.CreateAttribute("NAME");

            string mainprojectfilename = null;
            for (int i = 0; i < xmldoc.DocumentElement.ChildNodes.Count; i++)
            {
                if (xmldoc.DocumentElement.ChildNodes[i].Name == PerfDatumElement)
                {
                    subresultnode.InnerXml += xmldoc.DocumentElement.ChildNodes[i].OuterXml;
                }

                if (xmldoc.DocumentElement.ChildNodes[i].NodeType == XmlNodeType.Comment)
                {
                    mainprojectfilename = xmldoc.DocumentElement.ChildNodes[i].Value;
                    attrib.Value = "(" + ReportNode + ")";                    
                }
            }

            subresultnode.Attributes.Append(attrib);

            PerfSubResult psr = new PerfSubResult();
            psr.ReadSubResult(subresultnode);

            subresults = new Hashtable();
            subresults.Add(mainprojectfilename + psr.SubResultName, psr);

            XmlNodeList subresultslist = xmldoc.DocumentElement.GetElementsByTagName(Constants.PerfSubResultElement);
            if (subresultslist.Count == 0)
            {
                return false;
            }

            List<string> projectslist = new List<string>();

            for (int i = 0; i < subresultslist.Count; i++)
            {
                psr = new PerfSubResult();
                if (psr.ReadSubResult(subresultslist[i]))
                {
                    subresults.Add(psr.SubResultName, psr);
                    if (projectslist.Contains(psr.ProjectFileName) == false)
                    {
                        projectslist.Add(psr.ProjectFileName);
                    }
                }
                psr = null;
            }

            perfresultslist = new Hashtable(projectslist.Count);

            for (int i = 0; i < projectslist.Count; i++)
            {
                PerfResult pr = new PerfResult();
                pr.projectname = projectslist[i];
                if (projectslist[i] == mainprojectfilename)
                {
                    bmainprojectfile = true;
                }

                IDictionaryEnumerator ide = subresults.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().StartsWith(pr.projectname))
                    {
                        if (pr.subresults == null)
                        {
                            pr.subresults = new Hashtable();
                        }

                        pr.subresults.Add(ide.Key, ide.Value);
                    }
                }

                perfresultslist.Add(pr.projectname,pr);
            }

            return true;
        }

        /// <summary>
        /// Project name associated with current Perf result.
        /// </summary>
        public string ProjectName
        {
            set
            {
                projectname = value.Trim();
            }
        }

        /// <summary>
        /// List of project files read from Perf data file.
        /// </summary>
        public Hashtable ProjectsPerfResultsTable
        {
            get
            {
                return this.perfresultslist;
            }
        }

        /// <summary>
        /// Indicates if current perfresult is for the main project file.
        /// </summary>
        public bool IsMainProjectFile
        {
            get
            {
                return bmainprojectfile;
            }
            set
            {
                bmainprojectfile = value;
            }
        }

        /// <summary>
        /// Sets subresult node in Perf schema.
        /// </summary>
        /// <param name="subresultname"></param>
        /// <param name="subresultelapsedtime"></param>
        internal void SetSubResult(string subresultname, int subresultelapsedtime)
        {
            if (subresults == null)
            {
                subresults = new Hashtable();
            }

            PerfSubResult subresult;
            if (subresults.Count > 0 && subresults[subresultname] != null)
            {
                subresult = (PerfSubResult)subresults[subresultname];
                subresults.Remove(subresultname);
                subresult.SubResultTime = subresultelapsedtime;
                subresults.Add(subresultname, subresult);
            }
            else
            {
                subresult = new PerfSubResult();
                subresult.Initialize();
                subresult.SubResultName = subresultname;
                subresult.SubResultTime = subresultelapsedtime;
                subresults.Add(subresultname, subresult);
            }
        }

        /// <summary>
        /// Construct each SubResult node and return Xml string.
        /// </summary>
        /// <returns></returns>
        public string OutputPerfResults()
        {
            string innerxml = null;

            // Based on Targets order listed in ErrorCodes.xml construct the Xml
            for (int i = 0; i < TargetsToList.Count; i++)
            {
                string targetname = TargetsToList[i].ToString().ToLowerInvariant();
                IDictionaryEnumerator ide = subresults.GetEnumerator();
                while (ide.MoveNext())
                {
                    PerfSubResult perfsubresult = (PerfSubResult)ide.Value;
                    // Check targetname specified in ErrorCodes.xml with current targetname.
                    if (perfsubresult.SubResultName.ToLowerInvariant().Contains(targetname))
                    {
                        if (targetname.Contains(ReportNode.ToLowerInvariant()))
                        {
                            perfsubresult.ProjectFileName = this.projectname;
                            innerxml += perfsubresult.GetSubResultXml(bmainprojectfile);
                        }
                        else
                        {
                            innerxml += perfsubresult.GetSubResultXml(false);
                        }
                        break;
                    }
                }
            }

            return innerxml;
        }

    }

    /// <summary>
    /// Stores all Perf related subresults and outputs in Perf desired Xml format.
    /// </summary>
    class PerfSubResult
    {
        #region Member variables
        #region Constants
        const string PerfNameAttribute = "NAME";
        const string PerfValueAttribute = "VALUE";
        const string PerfComparatorAttribute = "COMPARATOR";
        const string PerfDatumElement = "DATUM";
        const string PerfMaximumAttribute = "MAXIMUM";
        const string PerfMinimumAttribute = "MINIMUM";
        const string PerfAverageAttribute = "AVERAGE";
        const string PerfNumberofIterations = "NUMBER OF ITERATIONS";
        const string PerfElapsedTime = "ELAPSED TIME";
        const string PerfComparatorYes = "yes";
        const string PerfComparatorNo = "no";
        #endregion Constants

        string subresultname;
        Hashtable perfdatumlist = null;
        string projectfilename;

        #endregion Member variables

        #region Public Methods
        /// <summary>
        /// Constructor, initialize Perf sub-result Maximum, Minimum, Average, Total Elapsed Time, Number of Iterations.
        /// </summary>
        public PerfSubResult()
        {
            perfdatumlist = new Hashtable();
        }

        /// <summary>
        /// Initialize all Datum values.
        /// </summary>
        public void Initialize()
        {
            PerfDatum perfdatum = new PerfDatum();
            perfdatum.Name = PerfMaximumAttribute;
            perfdatum.Unit = Constants.PerfSubResultUnit;
            perfdatumlist.Add(PerfMaximumAttribute, perfdatum);

            perfdatum = new PerfDatum();
            perfdatum.Name = PerfMinimumAttribute;
            perfdatum.Unit = Constants.PerfSubResultUnit;
            perfdatumlist.Add(PerfMinimumAttribute, perfdatum);

            perfdatum = new PerfDatum();
            perfdatum.Name = PerfAverageAttribute;
            perfdatum.Unit = Constants.PerfSubResultUnit;
            perfdatum.Comparator = true;
            perfdatumlist.Add(PerfAverageAttribute, perfdatum);

            perfdatum = new PerfDatum();
            perfdatum.Name = PerfNumberofIterations;
            perfdatum.Unit = null;
            perfdatumlist.Add(PerfNumberofIterations, perfdatum);

            perfdatum = new PerfDatum();
            perfdatum.Name = PerfElapsedTime;
            perfdatum.Unit = Constants.PerfSubResultUnit;
            perfdatumlist.Add(PerfElapsedTime, perfdatum);
        }

        /// <summary>
        /// Number of times a particular Subresult has been excited.
        /// </summary>
        public int NumberofIterations
        {
            get
            {
                if (perfdatumlist[PerfNumberofIterations] != null)
                {
                    PerfDatum pd = (PerfDatum)perfdatumlist[PerfNumberofIterations];
                    return Convert.ToInt32(pd.Value);
                }

                return 0;
            }
            set
            {
                if (perfdatumlist[PerfNumberofIterations] != null)
                {
                    object o = perfdatumlist[PerfNumberofIterations];
                    PerfDatum pd = (PerfDatum)o;
                    perfdatumlist.Remove(PerfNumberofIterations);
                    pd.Value = value;
                    perfdatumlist.Add(PerfNumberofIterations, pd);
                }
            }
        }

        /// <summary>
        /// Set the elapsed time and calculate all other perf data.
        /// (Maximum, Number of Iterations, Minimum, Average and Total Elapsed time.
        /// </summary>
        public decimal SubResultTime
        {
            set
            {
                this.NumberofIterations++;
                int numberofiterations = this.NumberofIterations;

                //decimal time = value;
                decimal totalelapsedtime = 0M;
                if (perfdatumlist[PerfElapsedTime] != null)
                {
                    PerfDatum pd = (PerfDatum)perfdatumlist[PerfElapsedTime];
                    totalelapsedtime = pd.Value;
                    totalelapsedtime += value;
                    pd.Value = totalelapsedtime;
                    perfdatumlist.Remove(PerfElapsedTime);
                    perfdatumlist.Add(PerfElapsedTime, pd);
                }

                if (perfdatumlist[PerfMaximumAttribute] != null)
                {
                    PerfDatum pd = (PerfDatum)perfdatumlist[PerfMaximumAttribute];
                    decimal maximumvalue = pd.Value;
                    if (value > maximumvalue)
                    {
                        perfdatumlist.Remove(PerfMaximumAttribute);
                        pd.Value = value;
                        perfdatumlist.Add(PerfMaximumAttribute, pd);
                    }
                }

                if (perfdatumlist[PerfMinimumAttribute] != null)
                {
                    PerfDatum pd = (PerfDatum)perfdatumlist[PerfMinimumAttribute];
                    decimal minimumvalue = pd.Value;
                    if (value < minimumvalue)
                    {
                        perfdatumlist.Remove(PerfMinimumAttribute);
                        pd.Value = value;
                        perfdatumlist.Add(PerfMinimumAttribute, pd);
                    }

                    if (numberofiterations == 1)
                    {
                        perfdatumlist.Remove(PerfMinimumAttribute);
                        pd.Value = value;
                        perfdatumlist.Add(PerfMinimumAttribute, pd);
                    }
                }

                if (perfdatumlist[PerfAverageAttribute] != null)
                {
                    PerfDatum pdaverage = (PerfDatum)perfdatumlist[PerfAverageAttribute];
                    perfdatumlist.Remove(PerfAverageAttribute);
                    if (perfdatumlist[PerfNumberofIterations] != null)
                    {
                        PerfDatum pd = (PerfDatum)perfdatumlist[PerfNumberofIterations];
                        decimal averagetime = totalelapsedtime / Convert.ToInt32(pd.Value);
                        pdaverage.Value = averagetime;
                        perfdatumlist.Add(PerfAverageAttribute, pdaverage);
                    }                    
                }
            }
        }

        /// <summary>
        /// Perf SubResult name.
        /// </summary>
        public string SubResultName
        {
            set
            {
                subresultname = value;

                // It's also good to get the ProjectFileName.

                string[] projectstring = subresultname.Split(new char[] { '(' , ')' }, StringSplitOptions.RemoveEmptyEntries);
                if ( projectstring.Length > 0 && projectstring.Length == 2 )
                {
                    projectfilename = projectstring[0];
                }
                projectstring = null;
            }
            get
            {
                return subresultname;
            }
        }

        /// <summary>
        /// Returns SubResults node outerxml.
        /// </summary>
        /// <returns></returns>
        public string GetSubResultXml(bool comparenode)
        {
            XmlDocument xmldoc = new XmlDocument();
            XmlDocumentFragment docfragment = xmldoc.CreateDocumentFragment();

            XmlNode subresultnode = xmldoc.CreateElement(Constants.PerfSubResultElement);
            docfragment.AppendChild(subresultnode);

            XmlAttribute attrib = xmldoc.CreateAttribute(PerfNameAttribute);
            attrib.Value = subresultname;
            subresultnode.Attributes.Append(attrib);

            string[] perfdataorder = new string[] {
                "Maximum",
                "Minimum",
                "Average",
                "Elapsed Time",
                "Number of Iterations"
            };

            for (int i = 0; i < perfdataorder.Length; i++)
            {
                string currentperfdata = perfdataorder[i].ToLowerInvariant();

                if ( perfdatumlist[PerfAverageAttribute] != null ) 
                {
                    PerfDatum pd = (PerfDatum)perfdatumlist[PerfAverageAttribute];
                    if ((int)pd.Value == 0)
                    {
                        return null;
                    }
                }

                IDictionaryEnumerator ide = perfdatumlist.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (currentperfdata == ide.Key.ToString().ToLowerInvariant())
                    {
                        XmlNode node = xmldoc.CreateElement(PerfDatumElement);

                        attrib = xmldoc.CreateAttribute(PerfNameAttribute);
                        attrib.Value = ide.Key.ToString();
                        node.Attributes.Append(attrib);

                        PerfDatum pd = (PerfDatum)ide.Value;
                        //if (perfdataorder[i].ToLowerInvariant() == "average" &&
                        //    (int)pd.Value == 0)
                        //{
                        //    node = null;
                        //    attrib = null;
                        //    continue;
                        //}

                        attrib = xmldoc.CreateAttribute(PerfValueAttribute);
                        if (String.IsNullOrEmpty(pd.Unit))
                        {
                            attrib.Value = ((int)pd.Value).ToString();
                        }
                        else
                        {
                            attrib.Value = ((int)pd.Value).ToString() + " " + pd.Unit;
                        }
                        node.Attributes.Append(attrib);

                        if (pd.Comparator)
                        {
                            attrib = xmldoc.CreateAttribute(PerfComparatorAttribute);
                            attrib.Value = PerfComparatorYes;
                            node.Attributes.Append(attrib);
                        }

                        subresultnode.AppendChild(node);
                        node = null;
                        attrib = null;
                        break;
                    }
                    
                }
            }

            string outerxml = null;
            if (comparenode)
            {
                subresultnode.AppendChild(xmldoc.CreateComment(projectfilename));
                outerxml = subresultnode.InnerXml;                
            }
            else
            {
                outerxml = subresultnode.OuterXml;
            }
            subresultnode = null;

            xmldoc = null;
            docfragment = null;

            return outerxml;
        }

        /// <summary>
        /// Read subresult values from a XmlNode.
        /// </summary>
        /// <param name="subresultnode"></param>
        /// <returns></returns>
        public bool ReadSubResult(XmlNode subresultnode)
        {
            if (subresultnode == null)
            {
                return false;
            }

            if (subresultnode.Attributes[PerfNameAttribute] == null)
            {
                return false;
            }

            subresultname = subresultnode.Attributes[PerfNameAttribute].Value;

            // Derive the project file from subresult.
            int index = subresultname.IndexOf('(');
            if (index < 0)
            {
                return false;
            }

            projectfilename = subresultname.Substring(0, index);

            for (int i = 0; i < subresultnode.ChildNodes.Count; i++)
            {
                XmlNode datumnode = subresultnode.ChildNodes[i];
                if (datumnode.Name != PerfDatumElement)
                {
                    continue;
                }

                for (int j = 0; j < datumnode.Attributes.Count; j++)
                {
                    XmlAttribute datumattribute = datumnode.Attributes[j];
                    PerfDatum datum;
                    string millisecondvalue = null;
                    switch (datumattribute.Value)
                    {
                        case PerfMaximumAttribute:
                        case PerfMinimumAttribute:
                        case PerfAverageAttribute:
                        case PerfElapsedTime:
                            datum = new PerfDatum();
                            datum.Name = datumattribute.Value;
                            if (datumnode.Attributes[PerfValueAttribute] == null)
                            {
                                continue;
                            }

                            millisecondvalue = datumnode.Attributes[PerfValueAttribute].Value;
                            if (String.IsNullOrEmpty(millisecondvalue))
                            {
                                continue;
                            }

                            millisecondvalue = millisecondvalue.Trim();

                            index = millisecondvalue.IndexOf(Constants.PerfSubResultUnit);
                            if ( String.IsNullOrEmpty(Constants.PerfSubResultUnit) == false && index >= 0)
                            {
                                datum.Unit = Constants.PerfSubResultUnit;
                                millisecondvalue = millisecondvalue.Substring(0, index);
                            }

                            if (datumnode.Attributes[PerfComparatorAttribute] != null 
                                && String.IsNullOrEmpty(datumnode.Attributes[PerfComparatorAttribute].Value) == false )
                            {
                                if (datumnode.Attributes[PerfComparatorAttribute].Value.ToLowerInvariant() == PerfComparatorYes)
                                {
                                    datum.Comparator = true;
                                }
                            }

                            if (String.IsNullOrEmpty(millisecondvalue) == false)
                            {
                                millisecondvalue = millisecondvalue.Trim();
                                datum.Value = Convert.ToDecimal(millisecondvalue);
                                if (perfdatumlist.Contains(datum.Name) == false)
                                {
                                    this.perfdatumlist.Add(datum.Name, datum);
                                }
                            }
                            break;

                        case PerfNumberofIterations:
                            datum = new PerfDatum();
                            datum.Name = datumattribute.Value;
                            if (datumnode.Attributes[PerfValueAttribute] == null)
                            {
                                continue;
                            }

                            millisecondvalue = datumnode.Attributes[PerfValueAttribute].Value;
                            if (String.IsNullOrEmpty(millisecondvalue) == false)
                            {
                                millisecondvalue = millisecondvalue.Trim();
                                datum.Value = Convert.ToInt16(millisecondvalue);
                                if (perfdatumlist.Contains(datum.Name) == false)
                                {
                                    this.perfdatumlist.Add(datum.Name, datum);
                                }
                            }
                            break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        internal string ProjectFileName
        {
            get
            {
                return projectfilename.Trim();
            }
            set
            {
                projectfilename = value;
            }
        }
        #endregion Public Methods
    }

    /// <summary>
    /// PerfData structure.
    /// </summary>
    struct PerfDatum
    {
        public decimal Value;
        public string Name;
        public string Unit;
        public bool Comparator;
    }
}
