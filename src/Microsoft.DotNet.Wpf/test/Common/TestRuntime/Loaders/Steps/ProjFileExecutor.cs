// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#region Using directives
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using Microsoft.Win32;
using Microsoft.Test.Utilities.StepsEngine;
using Microsoft.Test.MSBuildEngine;
using Microsoft.Test.Security.Wrappers;
using Microsoft.Test.Utilities.VariationEngine;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
#endregion

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// MSBuild Step class for instantiating, correcting, and building .*proj files.
    /// </summary>
    public class ProjectFileExecutor : IDisposable
    {
        #region Private Member Variables

        static bool ctrfound = false;
        
        bool blocaltimestamp = false;
        bool _busectr = false;

        string[] _variation = { "all" };
        string _scenario;

        string _filename = null;
        string _errorsandwarningstoignore = null;
        string _errorstoignore = null;
        string _warningstoignore = null;
        string _projcommandlineoptions = null;
        string _variationstepsfile = null;
        string _logfile = null;
        string _generatedassembly = null;
        string _generatedassemblytype = null;
        string _errorfile = null;

        string[] commandlineargs = null;

        bool _bgenonly = false;
        bool _build = false;
        bool _blogtofile = true;
        bool _bloggingappend = false;
        bool _bexpectedresult = true;
        bool _usecurrenttestlog = true;
        bool _bgentimestamp = false;

        int iteration;

        MSBuildProjExecutor msbuild = null;
        List<VariationStep> stepslist = null;
        List<string> _stepstorunlist = null;
        List<string> _generatedfileslist = null;
        Hashtable projectsperfresultslist = null;

        XmlElement _msbuilderrorselement = null;

        Queue runqueue = null;

        string _compilerpath = null;
        string _templatefilepath = null;

        #endregion

        #region Public Members

        /// <summary>
        /// Here to implement IDisposable.  Not actually needed now that we dont close the log.
        /// </summary>
        public void Dispose()
        {
            // Do nothing.  Don't need to close the log any more since as a LoaderStep, this is handled for us.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProjectFileExecutor()
        {
            iteration = 1;
            stepslist = new List<VariationStep>();

            _compilerpath = System.Windows.Forms.Application.StartupPath;

            string ctrpath = PathSW.GetDirectoryName(Application.ExecutablePath) + PathSW.DirectorySeparatorChar + "TestRuntime.dll";
            if (FileSW.Exists(ctrpath))
            {
                ctrfound = true;
                // Should now use this log path for ALL logging.  Not using it is the exception.
                _busectr = true;
            }

            if (ctrfound == false)
            {
                ctrpath = DirectorySW.GetCurrentDirectory() + PathSW.DirectorySeparatorChar + "TestRuntime.dll";
                if (FileSW.Exists(ctrpath))
                {
                    ctrfound = true;
                    // Should now use this log path for ALL logging.  Not using it is the exception.
                    _busectr = true;
                }
            }
        }

        /// <summary>
        /// Parse Commandline options.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool ParseCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                GlobalLog.LogDebug("No commandline arguments specified");
                return false;
            }

            ArrayList tempargs = new ArrayList(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                tempargs.Add(args[i]);
            }

            char[] separator = { '/', '-' };

            for (int i = 0; i < args.Length; i++)
            {
                string argument = (string)args[i];

                if (argument == null)
                {
                    tempargs.Remove(args[i]);
                    continue;
                }

                argument = argument.Trim();

                // Trying to get the value out of the property bag.
                // Piper no longer magically does this for us
                if (argument.StartsWith("&") && argument.EndsWith("&"))
                {
                    argument = Harness.Current[argument.Substring(1, argument.Length - 2)];
                    if (argument == null)
                    {
                        throw new InvalidOperationException(argument + " is not defined in the property bag; cannot be used as an argument");
                    }
                }

                int index = -1;

                if (argument.StartsWith("/") == false && argument.StartsWith("-") == false)
                {
                    // For File name processing.
                    if (IsProjectFile(argument))
                    {
                        _filename = argument;
                        tempargs.Remove(args[i]);
                        continue;
                    }
                    else if (IsVariationFile(argument))
                    {
                        _filename = argument;
                        tempargs.Remove(args[i]);
                        continue;
                    }
                    else if (IsStepsFile(argument))
                    {
                        _filename = argument;
                        _variationstepsfile = _filename;
                        tempargs.Remove(args[i]);
                        continue;
                    }
                    else
                    {
                        GlobalLog.LogDebug(String.Format("{0} is not a file that is known by MSBuildStep", _filename));
                        return false;
                    }
                }

                index = argument.IndexOfAny(separator);
                argument = argument.Substring(index + 1);

                char[] split = { ':' };
                char[] delimiter = { ',' };
                string[] splitstring = new string[2];

                index = argument.IndexOf(":", index);
                if (index < 0)
                {
                    splitstring[0] = argument;
                    // Error out, Print Help
                }
                else
                {
                    splitstring = argument.Split(split, 2);
                }

                switch (splitstring[0].ToLower())
                {
                    // Enable logging to file.
                    case "l":
                        _blogtofile = false;
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            _logfile = splitstring[1];
                        }

                        tempargs.Remove(args[i]);
                        break;

                    // Enable Debugging.
                    case "d":

                        if (String.IsNullOrEmpty(splitstring[1]))
                        {
                            MSBuildEngineCommonHelper.Debug = Microsoft.Test.MSBuildEngine.DebugMode.Quiet;
                            continue;
                        }

                        switch (splitstring[1].ToLowerInvariant())
                        {
                            case "q":
                            case "quiet":
                                MSBuildEngineCommonHelper.Debug = Microsoft.Test.MSBuildEngine.DebugMode.Quiet;
                                break;

                            case "diag":
                            case "diagnostic":
                                MSBuildEngineCommonHelper.Debug = Microsoft.Test.MSBuildEngine.DebugMode.Diagnoistic;
                                break;

                            case "verbose":
                                MSBuildEngineCommonHelper.Debug = Microsoft.Test.MSBuildEngine.DebugMode.Verbose;
                                break;
                        }

                        tempargs.Remove(args[i]);
                        break;

                    // Error file for list of errors and warnings.
                    case "e":
                        _errorfile = splitstring[1];
                        tempargs.Remove(args[i]);
                        break;

                    // List of Errors to be ignored at compilation.
                    case "err":
                        // List of errors and warnings to ignore.
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            _errorstoignore = splitstring[1];
                            _bexpectedresult = false;
                        }

                        tempargs.Remove(args[i]);
                        break;

                    // List of Warnings to be ignored at compilation.
                    case "wrn":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            _warningstoignore = splitstring[1];
                            _bexpectedresult = false;
                        }

                        tempargs.Remove(args[i]);
                        break;

                    // For file generation.
                    // Apply scenario in the Variation template.
                    case "s":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            _scenario = splitstring[1];
                        }
                        tempargs.Remove(args[i]);
                        break;

                    // For variation generation.
                    // Apply variation(s) in the Variation template.
                    case "v":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            _variation = splitstring[1].Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        tempargs.Remove(args[i]);
                        break;

                    // For variation generation.
                    // Steps file that outlines the steps that need to be taken.
                    case "st":
                    case "steps":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            if (IsStepsFile(splitstring[1]))
                            {
                                _variationstepsfile = splitstring[1];
                            }
                            else
                            {
                                throw new NotSupportedException(splitstring[0] + " is not a supported steps file.");
                            }
                        }
                        tempargs.Remove(args[i]);
                        break;

                    case "rst":
                    case "runstep":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            string[] stepstorunlist = splitstring[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (stepstorunlist.Length == 0)
                            {
                                throw new NotSupportedException("If using RunStep flag please give an approriate step ID.");
                            }

                            _stepstorunlist = new List<string>(stepstorunlist.Length);
                            for (int listcount = 0; listcount < stepstorunlist.Length; listcount++)
                            {
                                _stepstorunlist.Add(stepstorunlist[listcount]);
                            }

                            stepstorunlist = null;
                        }
                        tempargs.Remove(args[i]);
                        break;


                    // For variation generation
                    // Number of times to run compilation.
                    case "it":
                    case "iterate":
                        if (String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            iteration = Convert.ToInt32(splitstring[1]);
                        }
                        tempargs.Remove(args[i]);
                        break;

                    // Compiler target
                    case "t":
                        if (String.IsNullOrEmpty(splitstring[1]))
                        {
                            continue;
                        }

                        //if (splitstring[1].ToLowerInvariant().Contains("clean"))
                        //{
                        //    _bloggingappend = true;
                        //}
                        break;

                    // For variation generation.
                    case "g":
                    case "gen":
                        _bgenonly = true;
                        break;

                    // Expected results from compilation.
                    case "er":
                        _bexpectedresult = false;

                        //if (String.IsNullOrEmpty(splitstring[1]) == false)
                        //{
                        //    _bexpectedresult = Convert.ToBoolean(splitstring[1]);
                        //}

                        tempargs.Remove(args[i]);
                        break;

                    case "b":
                    case "build":
                        _build = true;
                        break;

                    case "time":
                        if (splitstring.Length >= 2 && String.IsNullOrEmpty(splitstring[1]) == false)
                        {
                            bool result ;
                            if (Boolean.TryParse(splitstring[1], out result))
                            {
                                _bgentimestamp = result;
                            }
                        }
                        else
                        {
                            _bgentimestamp = true;
                        }
                        break;

                    default:
                        break;
                }
            }

            if (tempargs.Count > 0)
            {
                commandlineargs = new string[tempargs.Count];
                tempargs.CopyTo(commandlineargs);
            }

            return true;
        }

        /// <summary>
        /// Initialize the logger class.
        /// </summary>
        public void InitializeLogger()
        {
            Logger log = new Logger(true, _usecurrenttestlog);
        }

        /// <summary>
        /// Reads a steps file if specified in the commandline line or 
        /// use the commandline options specified to create a Variation Step.
        /// </summary>
        /// <returns></returns>
        public bool ReadSteps()
        {
            try
            {
                if (String.IsNullOrEmpty(_variationstepsfile))
                {
                    // There is no steps file to read from.
                    // Using commandline options to do the right thing.
                    VariationStep variationstep = new VariationStep();
                    // If this is a project file then check if genonly flag is set and set for building the project file.
                    if (IsProjectFile(_filename))
                    {
                        variationstep.FileName = _filename;
                        if (_bgenonly)
                        {
                            variationstep.Build = false;
                        }

                        stepslist.Add(variationstep);
                    }
                    // If this variation file check commandline options 
                    else if (IsVariationFile(_filename))
                    {
                        variationstep.FileName = _filename;
                        variationstep.Scenario = _scenario;
                        variationstep.Variation = _variation;
                        variationstep.Build = _build;

                        stepslist.Add(variationstep);

                        _stepstorunlist = new List<string>();
                        _stepstorunlist.Add(variationstep.ID);
                    }
                }
                else
                {
                    _templatefilepath = CommonHelper.VerifyFileExists(_variationstepsfile);
                    if (String.IsNullOrEmpty(_templatefilepath) == false)
                    {
                        _templatefilepath = PathSW.GetDirectoryName(_templatefilepath) + PathSW.DirectorySeparatorChar;
                    }

                    VariationSteps steps = new VariationSteps();
                    if (steps.Read(_variationstepsfile) == false)
                    {
                        // Failed to read variation step file.
                        return false;
                    }

                    stepslist = steps.VariationStepsList;

                    if (_stepstorunlist != null)
                    {
                        runqueue = new Queue();
                        for (int i = 0; i < _stepstorunlist.Count; i++)
                        {
                            try
                            {
                                if (ResolveDependsOnRunList(GetStepFromID(_stepstorunlist[i])))
                                {
                                    runqueue.Enqueue(GetStepFromID(_stepstorunlist[i]));
                                    //runqueue.Enqueue(_stepstorunlist[i]);
                                }
                                else
                                {
                                    throw new ApplicationException("Step '" + _stepstorunlist[i] + "' could not be found ");
                                }
                            }
                            catch (ApplicationException aex)
                            {
                                GlobalLog.LogDebug(aex.Message.ToString() + " in " + _variationstepsfile);
                                return false;
                            }
                        }

                        if (runqueue.Count > 0)
                        {
                            stepslist = new List<VariationStep>();
                            while (runqueue.Count > 0)
                            {
                                if (stepslist.Contains((VariationStep)runqueue.Peek()) == false)
                                {
                                    stepslist.Add((VariationStep)runqueue.Dequeue());
                                }
                                else
                                {
                                    runqueue.Dequeue();
                                }
                            }
                        }
                        else
                        {
                            runqueue = null; 
                            return false;
                        }

                        runqueue = null;
                    }


                    _msbuilderrorselement = steps.MSBuildErrors;
                }
            }
            catch (Exception ex)
            {
                Logger.LoggerInstance.LogError = "Error reading file - " + _variationstepsfile;
                Logger.LoggerInstance.DisplayExceptionInformation(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Execute the steps read in ReadSteps.
        /// </summary>
        /// <returns></returns>
        public bool Execute()
        {
            bool returnvalue = true;
            string currentfilename = null;

            CommonHelper.AdditionalSearchPaths = _compilerpath + "," + _templatefilepath;

            if (this._bgentimestamp)
            {
                if (FileSW.Exists(Constants.MSBuildPerfDataFile))
                {
                    ReadPerfDatafromLog();
                    FileStreamSW fs = new FileStreamSW(Constants.MSBuildPerfDataFile, FileMode.Open);
                    fs.Flush();
                    fs.Close();
                }
                else
                {
                    FileStreamSW fs = new FileStreamSW(Constants.MSBuildPerfDataFile, FileMode.CreateNew);
                    fs.Close();
                }
            }

            try
            {
                //bool brunsubsteps = false;
                //short compilationcount = 0;

                // Execute all variation steps.
                for (int i = 0; i < stepslist.Count; i++)
                {
                    VariationStep currentstep = stepslist[i];

                    string generatedfilesarray = null;
                    if (_busectr && String.IsNullOrEmpty(Harness.Current["GeneratedFiles"]) == false)
                    {
                        generatedfilesarray = Harness.Current["GeneratedFiles"];
                    }
                    else if (_generatedfileslist != null && _generatedfileslist.Count > 0)
                    {
                        string[] temparray = new string[_generatedfileslist.Count + 1];
                        _generatedfileslist.CopyTo(temparray);
                        generatedfilesarray = Helper.ConvertArrayToString(temparray);

                        temparray = null;
                    }

                    if (String.IsNullOrEmpty(generatedfilesarray) == false)
                    {
                        string currentdirectory = DirectorySW.GetCurrentDirectory() + PathSW.DirectorySeparatorChar;
                        string[] temparray = generatedfilesarray.Split(',');
                        for (int j = 0; j < temparray.Length; j++)
                        {
                            if (temparray[j].StartsWith(currentdirectory) == false)
                            {
                                continue;
                            }

                            temparray[j] = temparray[j].Substring(currentdirectory.Length);
                        }

                        GeneratedFilesHelper.GeneratedFiles = Helper.ConvertArrayToString(temparray);
                        temparray = null;
                    }

                    Logger.LoggerInstance.PrintAsBlock("Begin Generation and Compilation");

                    VariationFileInfo tempfileinfo = new VariationFileInfo();
                    currentfilename = currentstep.FileName;

                    // Execute variations, files that end with xvar.
                    if (IsVariationFile(currentstep.FileName))
                    {
                        // Generate variations.
                        tempfileinfo = GenerateVariations(currentstep);
                        if (String.IsNullOrEmpty(tempfileinfo.FileName))
                        {
                            Logger.LoggerInstance.Result(false);
                            CleanGeneratedFiles();
                            return false;
                        }

                        if (_generatedfileslist == null)
                        {
                            _generatedfileslist = new List<string>();
                        }

                        tempfileinfo.CommandlineOptions += " " + Helper.ConvertArrayToString(this.commandlineargs);

                        // Save generated file info in piper for deletion at clean up time.
                        if (_busectr)
                        {
                            if (String.IsNullOrEmpty(Harness.Current["GeneratedFiles"]))
                            {
                                Harness.Current["GeneratedFiles"] = tempfileinfo.FileName;
                            }
                            else
                            {
                                if (String.IsNullOrEmpty(tempfileinfo.FileName) == false)
                                {
                                    string[] temparray = new string[_generatedfileslist.Count + 1];
                                    Harness.Current["GeneratedFiles"] = "";

                                    _generatedfileslist.CopyTo(temparray);

                                    temparray[_generatedfileslist.Count] = tempfileinfo.FileName;

                                    Harness.Current["GeneratedFiles"] = Helper.ConvertArrayToString(temparray);
                                    temparray = null;
                                }
                            }
                        }

                        _generatedfileslist.Add(tempfileinfo.FileName);
                        Logger.LoggerInstance.LogComment("\t\tGenerated file - " + tempfileinfo.FileName);
                    }
                    else if (IsProjectFile(currentstep.FileName))
                    {
                        VariationFileInfo projfileinfo = new VariationFileInfo();
                        projfileinfo.FileName = currentstep.FileName;
                        if (currentstep.Build || projfileinfo.GenerateOnly == false)
                        {
                            projfileinfo.CommandlineOptions = Helper.ConvertCommanlineArrayToString(this.commandlineargs);

                            if (this._errorstoignore != null)
                            {
                                projfileinfo.ErrorCodes = this._errorstoignore.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            }

                            if (this._warningstoignore != null)
                            {
                                projfileinfo.WarningCodes = this._warningstoignore.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }

                        projfileinfo.IsProjectFile = true;

                        tempfileinfo = projfileinfo;
                    }
                    else
                    {
                        // Anything else don't know what to do.
                        GlobalLog.LogDebug(String.Format("Unrecognized file type - {0}", currentstep.FileName));
                        msbuild.TargetCleanup = true;
                        CleanGeneratedFiles();
                        Logger.LoggerInstance.Result(false);
                        return false;
                    }

                    if (tempfileinfo.IsProjectFile)
                    {
                        // NoBuild is not specified or is set to true.
                        if (tempfileinfo.GenerateOnly == false && _bgenonly == false)
                        {
                            if (currentstep.RunMultipleTimes <= 0)
                            {
                                currentstep.RunMultipleTimes = 1;
                            }
                            else
                            {
                                Logger.LoggerInstance.Log = "Multiple Compilation enabled";
                            }

                            for (int buildcount = 0; buildcount < currentstep.RunMultipleTimes; buildcount++)
                            {
                                if (currentstep.RunMultipleTimes > 1)
                                {
                                    Logger.LoggerInstance.Log = "Compilation Run - " + buildcount;
                                }

                                if (InitializeandBuild(tempfileinfo, currentstep) == false)
                                {
                                    Logger.LoggerInstance.Log = "Initialize and Build failed";
                                    Logger.LoggerInstance.Result(false);
                                    msbuild.TargetCleanup = true;
                                    CleanGeneratedFiles();
                                    returnvalue = false;
                                    break;
                                    //CleanGeneratedFiles();
                                }
                                else
                                {
                                    if (currentstep.RunMultipleTimes > 1)
                                    {
                                        GlobalLog.LogDebug("*************************************************");
                                        GlobalLog.LogDebug("Calling dispose");
                                        msbuild.Dispose();
                                        msbuild = null;
                                        GlobalLog.LogDebug("*************************************************");

                                        TimeSpan ts = new TimeSpan(0, 0, 8);
                                        System.Threading.Thread.Sleep(ts);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tempfileinfo.ErrorCodes != null && tempfileinfo.ErrorCodes.Length > 0)
                            {
                                if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                                {
                                    if (_errorsandwarningstoignore.EndsWith(",") == false)
                                    {
                                        _errorsandwarningstoignore += ",";
                                    }
                                }

                                _errorsandwarningstoignore += Helper.ConvertArrayToString(tempfileinfo.ErrorCodes);
                            }

                            //GlobalLog.LogDebug("Expected Error Codes options - {0}", _errorsandwarningstoignore);

                            if (tempfileinfo.WarningCodes != null && tempfileinfo.WarningCodes.Length > 0)
                            {
                                if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                                {
                                    _errorsandwarningstoignore += ",";
                                }

                                _errorsandwarningstoignore = Helper.ConvertArrayToString(tempfileinfo.WarningCodes);
                            }
                        }
                    }

                    Logger.LoggerInstance.PrintAsBlock("End Generation and Compilation");
                }

                if (returnvalue)
                {
                    GlobalLog.LogEvidence("MSBuild Step passed!");
                    TestLog.Current.Result = TestResult.Pass;
                }

            }
            catch (XmlException ex)
            {
                Logger.LoggerInstance.LogError = "Error reading file - " + currentfilename;
                Logger.LoggerInstance.DisplayExceptionInformation(ex);
            }
            catch (Exception ex)
            {
                Logger.LoggerInstance.LogError = "Unexpected Exception thrown.";
                Logger.LoggerInstance.DisplayExceptionInformation(ex);
                Logger.LoggerInstance.Result(false);
                if (msbuild != null)
                {
                    msbuild.TargetCleanup = true;
                }
                CleanGeneratedFiles();
                return false;
            }
            finally
            {
                _stepstorunlist = null;
                this._scenario = null;
            }

            OutputPerfData();

            return returnvalue;
        }

        /// <summary>
        /// Delete files generated by the build process
        /// </summary>
        public void CleanGeneratedFiles()
        {
            if (_busectr && (msbuild != null && msbuild.TargetCleanup))
            {
                GlobalLog.LogDebug("Cleaning Generated Files");
                string generatedfiles = Harness.Current["GeneratedFiles"];
                if (String.IsNullOrEmpty(generatedfiles) == false)
                {
                    string[] _generatedfileslistfromctr = generatedfiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    _generatedfileslist = new List<string>(_generatedfileslistfromctr);
                }
            }
            else
            {
                _generatedfileslist = null;
            }

            if (_generatedfileslist != null)
            {
                while (_generatedfileslist.Count != 0)
                {
                    if (FileSW.Exists(_generatedfileslist[0]))
                    {
                        GlobalLog.LogDebug(String.Format("File to be deleted - {0}", _generatedfileslist[0]));
                        FileSW.Delete(_generatedfileslist[0]);
                    }

                    _generatedfileslist.RemoveAt(0);
                }
            }

            _generatedfileslist = null;
            this.msbuild = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Output the perf data that has been stored.
        /// </summary>
        private void OutputPerfData()
        {
            if (_bgenonly == false && _bgentimestamp && projectsperfresultslist != null && projectsperfresultslist.Count > 0)
            {
                XmlDocumentSW docfrag = new XmlDocumentSW();
                XmlNode resultnode = docfrag.CreateElement("RESULT");
                docfrag.AppendChild(resultnode);

                IDictionaryEnumerator ide = projectsperfresultslist.GetEnumerator();
                while (ide.MoveNext())
                {
                    PerfResult pr = (PerfResult)ide.Value;
                    resultnode.InnerXml = pr.OutputPerfResults() + resultnode.InnerXml;
                }

                XmlNodeReaderSW xnr = new XmlNodeReaderSW((XmlNode)docfrag.InnerObject);
                XmlTextWriterSW xtr = null;

                Console.Write("Generating {0} file .", Constants.MSBuildPerfDataFile);
                System.Text.Encoding currentencoding = System.Text.Encoding.ASCII;

                xtr = new XmlTextWriterSW(_templatefilepath + Constants.MSBuildPerfDataFile, currentencoding);

                // Generate file
                if (xtr != null)
                {
                    Console.Write(".");
                    xtr.Flush();
                    xtr.Formatting = Formatting.Indented;
                    xtr.Indentation = 4;
                    xtr.WriteNode((XmlNodeReader)xnr.InnerObject, true);
                    xnr.Close();
                    //				vardoc.GetElementsByTagName(Macros.TemplateDataElement)[0].WriteContentTo(xtr);
                    xtr.Close();
                    GlobalLog.LogDebug(". Done");
                }
            }
        }

        /// <summary>
        /// Sets up and builds given project given variation info.
        /// </summary>
        /// <param name="tempfileinfo">Generated Variation File</param>
        /// <param name="currentstep">Current Variation Step</param>
        /// <returns></returns>
        private bool InitializeandBuild(VariationFileInfo tempfileinfo, VariationStep currentstep)
        {
            bool breturnvalue = true;

            if (IsProjectFile(tempfileinfo.FileName))
            {
                GlobalLog.LogDebug(String.Format("Project file name - {0}", tempfileinfo.FileName));

                if (String.IsNullOrEmpty(currentstep.StepExpectedErrorCodes))
                {
                    if (tempfileinfo.ErrorCodes != null && tempfileinfo.ErrorCodes.Length > 0)
                    {
                        if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                        {
                            if (_errorsandwarningstoignore.EndsWith(",") == false)
                            {
                                _errorsandwarningstoignore += ",";
                            }
                        }

                        _errorsandwarningstoignore = Helper.ConvertArrayToString(tempfileinfo.ErrorCodes);
                    }

                    if (tempfileinfo.WarningCodes != null && tempfileinfo.WarningCodes.Length > 0)
                    {
                        if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                        {
                            _errorsandwarningstoignore += ",";
                        }

                        _errorsandwarningstoignore = Helper.ConvertArrayToString(tempfileinfo.WarningCodes);
                    }
                }
                else
                {
                    _errorsandwarningstoignore = currentstep.StepExpectedErrorCodes;
                }

                if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                {
                    _bexpectedresult = false;
                }

                _filename = tempfileinfo.FileName;

                if (String.IsNullOrEmpty(currentstep.StepCommandLineOptions))
                {
                    _projcommandlineoptions = tempfileinfo.CommandlineOptions;
                }
                else
                {
                    _projcommandlineoptions = currentstep.StepCommandLineOptions;
                }

                blocaltimestamp = false;
                // Add code to build with timestamp attribute set.
                if (_bgentimestamp == true)
                {
                    if (currentstep.OutputPerfData)
                    {
                        blocaltimestamp = true;
                        _bgentimestamp = false;
                    }
                }

                if (Build() == false)
                {
                    if (_bexpectedresult == false && msbuild.UnhandledErrorsandWarningsList.Count == 0)
                    {
                        //Logger.LoggerInstance.Result(true);
                        breturnvalue = true;
                        Logger.LoggerInstance.LogComment("Compilation Failed as expected");
                    }
                    else
                    {
                        //Logger.LoggerInstance.Result(false);
                        Logger.LoggerInstance.Stage = "Cleanup";
                        breturnvalue = false;
                        Logger.LoggerInstance.LogComment("Compilation Failed unexpectedly with one or more errors/warnings unhandled.");
                    }

                    Logger.LoggerInstance.LogComment("Cleaning up");
                }
                else
                {
                    if (_bexpectedresult == false && msbuild.BuiltwithWarnings == true)
                    {
                        //Logger.LoggerInstance.Result(false);
                        Logger.LoggerInstance.LogComment("Compilation Failed");
                        breturnvalue = false;
                    }
                    else
                    {
                        //Logger.LoggerInstance.Result(true);
                        //Logger.LoggerInstance.LogComment("Compilation Succeeded.");
                        breturnvalue = true;
                    }
                }

                if (blocaltimestamp)
                {
                    _bgentimestamp = true;
                }

                // Keep a track of libraries being generated which may be used in VariationGeneration
                // for special value types.
                if (msbuild.GeneratedAssemblyType != null && msbuild.GeneratedAssemblyType.ToLowerInvariant() == "library")
                {
                    GeneratedFilesHelper.AddGeneratedAssembly(_filename, _generatedassembly);
                }

                if (msbuild.TargetCleanup == false)
                {
                    Logger.LoggerInstance.LogComment("____________________________________");
                    Logger.LoggerInstance.LogComment("Project File name = " + _filename);
                    Harness.Current["ProjectFile"] = _filename;

                    if (String.IsNullOrEmpty(msbuild.GeneratedAssembly) == false)
                    {
                        string assemblypath = msbuild.GeneratedAssembly;
                        if (FileSW.Exists(assemblypath) == false)
                        {
                            string outputdir = "";

                            if (Directory.Exists(PathSW.GetDirectoryName(currentstep.FileName)) == false)
                            {
                                outputdir = DirectorySW.GetCurrentDirectory() + PathSW.DirectorySeparatorChar;
                            }

                            outputdir += PathSW.GetDirectoryName(currentstep.FileName) + PathSW.DirectorySeparatorChar;

                            if (String.IsNullOrEmpty(PathSW.GetDirectoryName(msbuild.GeneratedAssembly)) == false)
                            {
                                outputdir += PathSW.GetDirectoryName(assemblypath) + PathSW.DirectorySeparatorChar;
                            }

                            assemblypath = outputdir + PathSW.GetFileName(assemblypath);
                            outputdir = null;
                        }

                        string appextension = null;
                        Logger.LoggerInstance.LogComment("Generated Assembly Name = " + assemblypath);
                        Harness.Current["ExecutableFullPath"] = assemblypath;

                        if (msbuild.HostinBrowser)
                        {
                            appextension = GetApplicationExtension(true);
                        }
                        else
                        {
                            appextension = GetApplicationExtension(false);
                        }

                        appextension = appextension.ToLowerInvariant();

                        if (String.IsNullOrEmpty(appextension) == false)
                        {
                            if (FileSW.Exists(assemblypath))
                            {
                                Harness.Current["DeployFullPath"] = PathSW.GetFullPath(PathSW.GetDirectoryName(assemblypath)) + PathSW.DirectorySeparatorChar + PathSW.GetFileNameWithoutExtension(assemblypath) + appextension;
                                Logger.LoggerInstance.LogComment("Deploy path = " + Harness.Current["DeployFullPath"]);
                            }

                            appextension = null;
                        }
                    }

                    Logger.LoggerInstance.LogComment("____________________________________");
                }

                if (_bgentimestamp == false)
                {
                    SaveLogData();
                }

                _errorsandwarningstoignore = null;
                _filename = null;
                _projcommandlineoptions = null;
            }

            return breturnvalue;
        }

        /// <summary>
        /// Build the current project file.
        /// </summary>
        /// <returns></returns>
        private bool Build()
        {
            Logger.LoggerInstance.LogComment("MSBuild Compiler Step : Executing Build");

            if (msbuild != null)
            {
                msbuild = null;
            }

            if (msbuild == null)
            {
                if (String.IsNullOrEmpty(PathSW.GetDirectoryName(Application.ExecutablePath)))
                {
                    GlobalLog.LogDebug("Application executable path incorrect");
                    return false;
                }

                string _defaulterrorfile = PathSW.GetDirectoryName(Application.ExecutablePath) + PathSW.DirectorySeparatorChar + "ErrorCodes.xml";
                
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ErrorCodes.xml");
                //if (stream == null)
                //    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Test.Loaders.clickoncetest.pfx");
                byte[] certBuffer = new byte[stream.Length];
                stream.Read(certBuffer, 0, (int)stream.Length);
                stream.Close();
                File.WriteAllBytes(_defaulterrorfile, certBuffer);

                if (FileSW.Exists(_defaulterrorfile) == false)
                {
                    GlobalLog.LogDebug(String.Format("Error file does not exist {0}", _defaulterrorfile));
                    if (_bgentimestamp)
                    {
                        msbuild = new MSBuildProjExecutor(_bgentimestamp);
                    }
                    else
                    {
                        msbuild = new MSBuildProjExecutor();
                    }
                }
                else
                {
                    if (_bgentimestamp)
                    {
                        msbuild = new MSBuildProjExecutor(_defaulterrorfile, _bgentimestamp);
                    }
                    else
                    {
                        msbuild = new MSBuildProjExecutor(_defaulterrorfile);
                    }
                }

                if (_bgentimestamp == false)
                {
                    // Making errorhandling as not the default and so require this flag for LHCompiler.
                    msbuild.IgnoreErrorWarningHandling = true;
                    if (String.IsNullOrEmpty(_errorfile) == false)
                    {
                        msbuild.AdditionalExpectedErrorsandWarnings(_errorfile);
                    }

                    if (_msbuilderrorselement != null)
                    {
                        msbuild.ExpectedMSBuildErrors = _msbuilderrorselement;
                    }
                }
            }

            if (_bgentimestamp == false)
            {
                if (_blogtofile)
                {
                    string logdirectory = null;
                    if (String.IsNullOrEmpty(_logfile))
                    {
                        _logfile = "msbuild";
                    }

                    logdirectory = PathSW.GetDirectoryName(PathSW.GetFullPath(_logfile));
                    if (String.IsNullOrEmpty(logdirectory))
                    {
                        logdirectory = PathSW.GetDirectoryName(PathSW.GetFullPath(_filename));
                        if (String.IsNullOrEmpty(logdirectory))
                        {
                            logdirectory = DirectorySW.GetCurrentDirectory();
                        }
                    }

                    string msbuildlogfile = null;

                    msbuildlogfile = logdirectory + PathSW.DirectorySeparatorChar + _logfile;

                    msbuild.BuildLogFileName = msbuildlogfile + ".log";
                    msbuild.BuildLogErrorFileName = msbuildlogfile + ".err";
                    msbuild.BuildLogWarningFileName = msbuildlogfile + ".wrn";

                    if (_bloggingappend || msbuild.TargetCleanup)
                    {
                        if (_bgentimestamp)
                        {
                            //msbuild.LHCompilerLog("Overwrite");
                            msbuild.LHCompilerLog("Append");
                        }
                        else
                        {
                            msbuild.LHCompilerLog("Append");
                        }
                    }
                    else
                    {
                        msbuild.LHCompilerLog("Overwrite");
                    }

                }

                if (String.IsNullOrEmpty(_errorsandwarningstoignore) == false)
                {
                    msbuild.ErrorWarningsToIgnore(_errorsandwarningstoignore);
                }
            }

            if (String.IsNullOrEmpty(_projcommandlineoptions) == false)
            {
                //msbuild.CommandLineArguements = projcommandlineoptions; //
                msbuild.CommandlineArguements = _projcommandlineoptions.Split(new char[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            }


            bool returnvalue = true;

            // Make sure we fully qualify the path to the proj file here
            // Some codepaths will come here with the path though, so check first.
            if (!_filename.StartsWith(PathSW.GetDirectoryName(Application.ExecutablePath)))
            {
                _filename = PathSW.GetDirectoryName(Application.ExecutablePath) + PathSW.DirectorySeparatorChar + _filename;
            }

            if (msbuild.Build(_filename) == false)
            {
                if (_bgentimestamp == false)
                {
                    // Compilation failed. Cannot get any assembly information.
                    if (msbuild.UnhandledErrorsandWarningsList != null && msbuild.UnhandledErrorsandWarningsList.Count > 0)
                    {
                        Logger.LoggerInstance.LogError = "Unhandled Errors/Warnings - ";
                        for (int i = 0; i < msbuild.UnhandledErrorsandWarningsList.Count; i++)
                        {
                            Logger.LoggerInstance.LogError = "\t" + msbuild.UnhandledErrorsandWarningsList[i].FullDescription;
                        }
                    }
                }
                return false;
            }
            else
            {
                if (_bgentimestamp == false)
                {
                    // This is specific to Warnings where MSBuild return value is true even if warnings are encountered.
                    if (msbuild.BuiltwithWarnings)
                    {
                        if (msbuild.UnhandledErrorsandWarningsList != null && msbuild.UnhandledErrorsandWarningsList.Count > 0)
                        {
                            Logger.LoggerInstance.LogError = "Unhandled Errors/Warnings - ";
                            for (int i = 0; i < msbuild.UnhandledErrorsandWarningsList.Count; i++)
                            {
                                Logger.LoggerInstance.LogError = "\t" + msbuild.UnhandledErrorsandWarningsList[i].FullDescription;
                            }
                        }

                        returnvalue = false;
                    }
                }
            }

            if (_bgentimestamp == false)
            {
                _generatedassembly = msbuild.GeneratedAssembly;
                _generatedassemblytype = msbuild.GeneratedAssemblyType;

                //Logger.LoggerInstance.LogComment("Poseidon: Generated Assembly type = " + msbuild.GeneratedAssemblyType);
                //Logger.LoggerInstance.LogComment("Poseidon: Generated Assembly = " + _generatedassembly);

                Logger.LoggerInstance.LogComment("Compilation result = true");
                _errorsandwarningstoignore = null;
            }
            else 
            {
                ReadandUpdatePerfData();
            }

            return returnvalue;
        }

        /// <summary>
        /// 
        /// </summary>
        private void ReadPerfDatafromLog()
        {
            PerfResult pr = new PerfResult();
            // TODO: Add try catch block.
            if (pr.ReadPerfResultsFromFile(_templatefilepath + Constants.MSBuildPerfDataFile))
            {
                projectsperfresultslist = pr.ProjectsPerfResultsTable;

                // Once the results are successfully read we need to construct the right set of PerfResults for 
                // the right set of projects.
            }

            //projectsperfresultslist.Add(pr.ProjectName, pr);
        }

        /// <summary>
        /// Provide perf data by reading the MSBuild Log file.
        /// </summary>
        private void ReadandUpdatePerfData()
        {
            if (msbuild.ProjectFilesPerfList == null)
            {
                return;
            }

            //Hashtable projectsperfresultslist = new Hashtable();
            if (projectsperfresultslist == null)
            {
                projectsperfresultslist = new Hashtable();
            }

            for (int projectscount = 0; projectscount < msbuild.ProjectFilesPerfList.Count; projectscount++)
            {
                MSBuildProjectPerf projectperf = msbuild.ProjectFilesPerfList[projectscount];
                TimeSpan timespan = projectperf.EndTime - projectperf.StartTime;
                GlobalLog.LogDebug(String.Format("Project \"{0}\" \t\t - {1} (ms)", projectperf.ProjectName, timespan.TotalMilliseconds));

                PerfResult pr = null;
                if (projectsperfresultslist.Count > 0 && projectsperfresultslist[projectperf.ProjectName] != null)
                {
                    pr = (PerfResult)projectsperfresultslist[projectperf.ProjectName];
                }
                else
                {
                    pr = new PerfResult();
                    pr.ProjectName = projectperf.ProjectName;
                    projectsperfresultslist.Add(projectperf.ProjectName, pr);
                }

                string projfilename = PathSW.GetFileName(projectperf.ProjectName);
                //pr.SetSubResult(projfilename + "(Total_Elapsed_Time)", (int)timespan.TotalMilliseconds);
                if (projectscount == msbuild.ProjectFilesPerfList.Count - 1)
                {
                    pr.IsMainProjectFile = true;
                }
                    
                pr.SetSubResult(projectperf.ProjectName + "(Total_Elapsed_Time)", (int)timespan.TotalMilliseconds);

                string perflog = _templatefilepath + "msbuilddetails.log";
                MemoryStream perflogstream = null;

                MSBuildEngineCommonHelper.SaveToMemory("=================================================================", ref perflogstream, perflog);

                for (int targetscount = 0; targetscount < projectperf.TargetsPerfDataList.Count; targetscount++)
                {
                    MSBuildTargetPerf targetperf = projectperf.TargetsPerfDataList[targetscount];
                    timespan = targetperf.EndTime - targetperf.StartTime;

                    StringBuilder outputstring = new StringBuilder();
                    outputstring.AppendFormat("Target \"{0}\"        - {1} (ms)", targetperf.TargetName, timespan.TotalMilliseconds);

                    if (PerfResult.TargetsToList != null && PerfResult.TargetsToList.Count > 0
                        && PerfResult.TargetsToList.Contains(targetperf.TargetName.ToLowerInvariant()) == true)
                    {
                        pr.SetSubResult(projfilename + "(" + targetperf.TargetName + ")", (int)timespan.TotalMilliseconds);
                    }

                    for (int taskscount = 0; taskscount < targetperf.TasksPerfDataList.Count; taskscount++)
                    {
                        MSBuildTaskPerf taskperf = targetperf.TasksPerfDataList[taskscount];
                        timespan = taskperf.EndTime - taskperf.StartTime;
                        outputstring.AppendFormat("\n    Task \"{0}\"", taskperf.TaskName);
                        //for (int tabcount = 0; tabcount < maxvalue - taskperf.TaskName.Length; tabcount++)
                        //{
                        //    Console.Write(" ");
                        //}

                        outputstring.AppendFormat("     - {0} (ms)", timespan.TotalMilliseconds);
                    }

                    //GlobalLog.LogDebug(outputstring);

                    MSBuildEngineCommonHelper.SaveToMemory(outputstring.ToString(), ref perflogstream, perflog);
                    MSBuildEngineCommonHelper.SaveToMemory("---------------------------------------------------", ref perflogstream, perflog);
                }

                MSBuildEngineCommonHelper.SaveToMemory("=================================================================", ref perflogstream, perflog);
                MSBuildEngineCommonHelper.WritetoFilefromMemory(ref perflogstream, perflog);
            }
        }

        /// <summary>
        /// Recursive function to get the list of dependent steps.
        /// </summary>
        /// <param name="currentstep"></param>
        /// <returns></returns>
        private bool ResolveDependsOnRunList(VariationStep currentstep)
        {
            if (currentstep == null)
            {
                return false;
            }

            if (currentstep.StepDependson == null)
            {
                return true;
            }

            if (currentstep.StepDependson.Length == 0)
            {
                return true;
            }

            bool breturnvalue = false;
            for (int i = 0; i < currentstep.StepDependson.Length; i++)
            {
                VariationStep curr = GetStepFromID(currentstep.StepDependson[i]);
                if (curr != null)
                {
                    if (ResolveDependsOnRunList(curr))
                    {
                        // Add to queue
                        //runqueue.Enqueue(curr.ID);
                        runqueue.Enqueue(curr);
                        breturnvalue = true;
                    }
                    else
                    {
                        breturnvalue = false;
                    }
                }
                else
                {
                    throw new ApplicationException("Step " + currentstep.StepDependson[i] + " could not be found");
                }
            }

            return breturnvalue;
        }

        /// <summary>
        /// Helper Function to get a variation step from ID.
        /// </summary>
        /// <param name="stepid"></param>
        /// <returns></returns>
        private VariationStep GetStepFromID(string stepid)
        {
            for (int i = 0; i < stepslist.Count; i++)
            {
                if (stepslist[i].ID == stepid)
                {
                    return stepslist[i];
                }
            }

            return null;
        }

        /// <summary>
        /// For Test run with TestRuntime dll save the log files.
        /// </summary>
        private void SaveLogData()
        {
            if (msbuild != null)
            {
                Logger.LoggerInstance.Log = "MSbuild test cleanup = " + msbuild.TargetCleanup;

                if (msbuild.TargetCleanup == false)
                {
                    Logger.LoggerInstance.LogComment("Saving Log data to temp");

                    Logger.LoggerInstance.Log = "File length = " + FileSW.ReadAllLines(msbuild.BuildLogFileName).Length;

                    if (FileSW.ReadAllLines(msbuild.BuildLogFileName).Length > 0)
                    {
                        Logger.LoggerInstance.Save(msbuild.BuildLogFileName);
                    }

                    if (FileSW.ReadAllLines(msbuild.BuildLogErrorFileName).Length > 0)
                    {
                        Logger.LoggerInstance.Save(msbuild.BuildLogErrorFileName);
                    }

                    if (FileSW.ReadAllLines(msbuild.BuildLogWarningFileName).Length > 0)
                    {
                        Logger.LoggerInstance.Save(msbuild.BuildLogWarningFileName);
                    }

                    Logger.LoggerInstance.Save(@"MSBuildCompilerStep.log");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        private string[] CheckForAll(string[] argument)
        {
            if (argument == null || argument.Length == 0)
            {
                argument = new string[1];
                argument[0] = "all";
                return argument;
            }

            string[] temp = new string[1];

            if (argument[0].ToLowerInvariant() == "all")
            {
                return argument;
            }

            for (int i = 0; i < argument.Length; i++)
            {
                if (argument[i].ToLowerInvariant() == "all")
                {
                    temp[0] = "all";
                    return temp;
                }
            }

            return argument;
        }

        /// <summary>
        /// Generation Variations not used right now.
        /// </summary>
        /// <param name="currentstep"></param>
        /// <returns></returns>
        private VariationFileInfo GenerateVariations(VariationStep currentstep)
        {
            VariationFileInfo returninfo = new VariationFileInfo();
            if (currentstep == null)
            {
                return returninfo;
            }

            // Setup version info for Presentation assemblies.
            // Enables running side by side scenario.
            // Todo: Need to identify scenario where need to compile to a lower version than current version. (Maybe)
            VersionHelper.PresentationAssemblyFullName = MSBuildEngineCommonHelper.PresentationFrameworkFullName;

            ProjVariationGenerator projfilecreator = new ProjVariationGenerator();

            currentstep.FileName = CommonHelper.VerifyFileExists(currentstep.FileName);

            if (projfilecreator.Read(currentstep.FileName) == false)
            {
                Logger.LoggerInstance.LogError = currentstep.FileName + " is not a recognized variation file.";
                return returninfo;
            }

            if (currentstep.Scenario == null)
            {
                Logger.LoggerInstance.LogError = "There were no scenarios specified to be applied for variation generation";
                return returninfo;
            }

            //             Not going to use all in Scenario case.
            if (currentstep.Scenario.Length == 0)
            {
                GlobalLog.LogDebug("No Scenarios to apply");
                return new VariationFileInfo();
            }

            Logger.LoggerInstance.Log = "Running Step - " + currentstep.ID + " & scenario " + currentstep.Scenario;

            if (String.IsNullOrEmpty(currentstep.OutputDirectory) == false)
            {
                projfilecreator.OutputDirectory = currentstep.OutputDirectory;
            }
            else
            {
                projfilecreator.OutputDirectory = PathSW.GetDirectoryName(currentstep.FileName);
            }

            if (currentstep.Variation[0] != "all")
            {
                if (projfilecreator.GenerateVariationFile(currentstep.Scenario, currentstep.Variation) == false)
                {
                    Logger.LoggerInstance.LogComment("Generating Variation file failed");
                    return returninfo;
                }
            }
            else
            {
                if (projfilecreator.GenerateVariationFile(currentstep.Scenario) == false)
                {
                    Logger.LoggerInstance.LogComment("Generating Variation file failed");
                    return returninfo;
                }
            }

            returninfo = projfilecreator.GeneratedFileInfo;

            projfilecreator.Dispose();

            projfilecreator = null;

            return returninfo;
        }

        /// <summary>
        /// Determine if file specified is a project file.
        /// </summary>
        /// <param name="file">File name</param>
        /// <returns>True if file is a project file</returns>
        bool IsProjectFile(string file)
        {
            if (String.IsNullOrEmpty(file))
            {
                return false;
            }

            string fileextension = PathSW.GetExtension(file);
            if (fileextension.ToLowerInvariant().EndsWith("proj") == false)
            {
                MSBuildEngine.MSBuildEngineCommonHelper.Log = "Not a recognized project file extension " + file;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if file specified is a Variation File.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool IsVariationFile(string file)
        {
            if (String.IsNullOrEmpty(file))
            {
                return false;
            }

            string fileextension = PathSW.GetExtension(file);
            if (String.Compare(fileextension.ToLowerInvariant(), ".xvar") != 0)
            {
                MSBuildEngine.MSBuildEngineCommonHelper.Log = "Not a Variation file " + file + ".";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Function to detemine if current file is a Step file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool IsStepsFile(string file)
        {
            if (String.IsNullOrEmpty(file))
            {
                return false;
            }

            string fileextension = PathSW.GetExtension(file);
            if (String.Compare(fileextension.ToLowerInvariant(), ".csxml") != 0)
            {
                MSBuildEngine.MSBuildEngineCommonHelper.Log = "Not a Variation file " + file + ".";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Helper function
        /// </summary>
        /// <param name="hostinbrowserflag"></param>
        /// <returns></returns>
        string GetApplicationExtension(bool hostinbrowserflag)
        {
            if (hostinbrowserflag)
            {
                return ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION;
            }
            else
            {
                return ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION;
            }
        }

        #endregion
    }
}
