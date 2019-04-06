// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//
//     Adds, updates or checks validity of Uids (Unique identifiers)
//     on all XAML elements in XAML files.
//
//---------------------------------------------------------------------------


using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using MS.Internal.Markup;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Utility;                   // For SR
using MS.Internal.Tasks;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{
    /// <summary>
    /// An MSBuild task that checks or corrects unique identifiers in
    /// XAML markup.
    /// </summary>
    public sealed class UidManager : Task
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Create a UidManager object.
        /// </summary>
        public UidManager() : base(SR.SharedResourceManager)
        {
            _backupPath = Directory.GetCurrentDirectory();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// The method invoked by MSBuild to check or correct Uids.
        /// </summary>
        public override bool Execute()
        {
            TaskHelper.DisplayLogo(Log, SR.Get(SRID.UidManagerTask));

            if (MarkupFiles == null || MarkupFiles.Length == 0)
            {
                Log.LogErrorWithCodeFromResources(SRID.SourceFileNameNeeded);
                return false;
            }

            try
            {
                _task = (UidTask)Enum.Parse(typeof(UidTask), _taskAsString);
            }
            catch (ArgumentException)
            {
                Log.LogErrorWithCodeFromResources(SRID.BadUidTask, _taskAsString);
                return false;
            }


            bool allFilesOk;
            try
            {
                allFilesOk = ManageUids();
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
                    string message;
                    string errorId;

                    errorId = Log.ExtractMessageCode(e.Message, out message);

                    if (String.IsNullOrEmpty(errorId))
                    {
                        errorId = UnknownErrorID;
                        message = SR.Get(SRID.UnknownBuildError, message);
                    }

                    Log.LogError(null, errorId, null, null, 0, 0, 0, 0, message, null);

                    allFilesOk = false;
                }
            }
#pragma warning disable 6500
            catch // Non-CLS compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                allFilesOk = false;
            }
#pragma warning restore 6500

            return allFilesOk;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        ///<summary>
        /// The markup file(s) to be checked or updated.
        ///</summary>
        [Required]
        public ITaskItem[] MarkupFiles
        {
            get { return _markupFiles; }
            set { _markupFiles = value; }
        }

        /// <summary>
        /// The directory for intermedia files
        /// </summary>
        /// <remarks>
        /// </remarks>
        public string IntermediateDirectory
        {
            get { return _backupPath; }
            set
            {
                string sourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
                _backupPath = TaskHelper.CreateFullFilePath(value, sourceDir);
            }
        }


        ///<summary>
        /// Enum to determine which Uid management task to undertake
        ///</summary>
        private enum UidTask
        {

            ///<summary>
            /// Uid managment task to check validity of Uids
            ///</summary>
            Check = 0,

            ///<summary>
            /// Uid managment task to Update Uids to a valid state
            ///</summary>
            Update = 1,

            ///<summary>
            /// Uid managment task to remove all Uids
            ///</summary>
            Remove = 2,
        }

        ///<summary>
        /// Uid management task required
        ///</summary>
        [Required]
        public string Task
        {
            get { return _taskAsString; }
            set { _taskAsString = value; }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private bool ManageUids()
        {
            int countGoodFiles = 0;
            // enumerate through each file
            foreach (ITaskItem inputFile in _markupFiles)
            {
                Log.LogMessageFromResources(SRID.CheckingUids, inputFile.ItemSpec);
                switch (_task)
                {
                    case UidTask.Check:
                    {
                        UidCollector collector = ParseFile(inputFile.ItemSpec);

                        bool success = VerifyUid(
                            collector,          // uid collector
                            true                // log error
                            );

                        if (success) countGoodFiles++;
                        break;
                    }
                    case UidTask.Update:
                    {
                        UidCollector collector = ParseFile(inputFile.ItemSpec);

                        bool success = VerifyUid(
                            collector,          // uid collector
                            false               // log error
                            );

                        if (!success)
                        {
                            if (SetupBackupDirectory())
                            {
                                // resolve errors
                                collector.ResolveUidErrors();

                                // temp file to write to
                                string tempFile   = GetTempFileName(inputFile.ItemSpec);

                                // backup file of the source file before it is overwritten.
                                string backupFile = GetBackupFileName(inputFile.ItemSpec);

                                using (Stream uidStream = new FileStream(tempFile, FileMode.Create))
                                {
                                    using (Stream source = File.OpenRead(inputFile.ItemSpec))
                                    {
                                        UidWriter writer = new UidWriter(collector, source, uidStream);
                                        writer.UpdateUidWrite();
                                    }
                                }

                                // backup source file by renaming it. Expect to be (close to) atomic op.
                                RenameFile(inputFile.ItemSpec, backupFile);

                                // rename the uid output onto the source file. Expect to be (close to) atomic op.
                                RenameFile(tempFile, inputFile.ItemSpec);

                                // remove the temp files
                                RemoveFile(tempFile);
                                RemoveFile(backupFile);

                                countGoodFiles++;
                            }
                        }
                        else
                        {
                            // all uids are good. No-op
                            countGoodFiles++;
                        }

                        break;
                    }
                    case UidTask.Remove:
                    {
                        UidCollector collector = ParseFile(inputFile.ItemSpec);

                        bool hasUid = false;
                        for (int i = 0; i < collector.Count; i++)
                        {
                            if (collector[i].Status != UidStatus.Absent)
                            {
                                hasUid = true;
                                break;
                            }
                        }

                        if (hasUid)
                        {
                            if (SetupBackupDirectory())
                            {
                                // temp file to write to
                                string tempFile   = GetTempFileName(inputFile.ItemSpec);

                                // backup file of the source file before it is overwritten.
                                string backupFile = GetBackupFileName(inputFile.ItemSpec);

                                using (Stream uidStream = new FileStream(tempFile, FileMode.Create))
                                {
                                    using (Stream source = File.OpenRead(inputFile.ItemSpec))
                                    {
                                        UidWriter writer = new UidWriter(collector, source, uidStream);
                                        writer.RemoveUidWrite();
                                    }
                                }

                                // rename the source file to the backup file name. Expect to be (close to) atomic op.
                                RenameFile(inputFile.ItemSpec, backupFile);

                                // rename the output file over to the source file. Expect to be (close to) atomic op.
                                RenameFile(tempFile, inputFile.ItemSpec);

                                // remove the temp files
                                RemoveFile(tempFile);
                                RemoveFile(backupFile);

                                countGoodFiles++;
                            }
                        }
                        else
                        {
                            // There is no Uid in the file. No need to do remove.
                            countGoodFiles++;
                        }

                        break;
                    }
                }
            }

            // spew out the overral log info for the task
            switch (_task)
            {
                case UidTask.Remove:
                    Log.LogMessageFromResources(SRID.FilesRemovedUid, countGoodFiles);
                    break;

                case UidTask.Update:
                    Log.LogMessageFromResources(SRID.FilesUpdatedUid, countGoodFiles);
                    break;

                case UidTask.Check:
                    Log.LogMessageFromResources(SRID.FilesPassedUidCheck, countGoodFiles);

                    if (_markupFiles.Length > countGoodFiles)
                    {
                        Log.LogErrorWithCodeFromResources(SRID.FilesFailedUidCheck, _markupFiles.Length - countGoodFiles);
                    }
                    break;
            }

            return _markupFiles.Length == countGoodFiles;
        }


       private string GetTempFileName(string fileName)
        {
            return Path.Combine(_backupPath, Path.ChangeExtension(Path.GetFileName(fileName), "uidtemp"));
        }

        private string GetBackupFileName (string fileName)
        {
            return Path.Combine(_backupPath, Path.ChangeExtension(Path.GetFileName(fileName), "uidbackup"));
        }

        private void RenameFile(string src, string dest)
        {
            RemoveFile(dest);
            File.Move(src, dest);
        }

        private void RemoveFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        private bool SetupBackupDirectory()
        {
            try
            {
                if (!Directory.Exists(_backupPath))
                {
                    Directory.CreateDirectory(_backupPath);
                }

                return true;
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
                    Log.LogErrorWithCodeFromResources(SRID.IntermediateDirectoryError, _backupPath);
                    return false;
                }
            }
#pragma warning disable 6500
            catch   // Non-cls compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.IntermediateDirectoryError, _backupPath);
                return false;
            }
#pragma warning restore 6500
        }




        /// <summary>
        /// Verify the Uids in the file
        /// </summary>
        /// <param name="collector">UidCollector containing all Uid instances</param>
        /// <param name="logError">true to log errors while verifying</param>
        /// <returns>true indicates no errors</returns>
        private bool VerifyUid(
            UidCollector collector,
            bool         logError
            )
        {
            bool errorFound = false;

            for (int i = 0; i < collector.Count; i++)
            {
                Uid currentUid = collector[i];
                if (currentUid.Status == UidStatus.Absent)
                {
                    // Uid missing
                    if (logError)
                    {
                        Log.LogErrorWithCodeFromResources(
                             null,
                             collector.FileName,
                             currentUid.LineNumber,
                             currentUid.LinePosition,
                             0, 0,
                             SRID.UidMissing, currentUid.ElementName
                         );
                    }

                    errorFound = true;
                }
                else if (currentUid.Status == UidStatus.Duplicate)
                {
                    // Uid duplicates
                    if (logError)
                    {
                        Log.LogErrorWithCodeFromResources(
                              null,
                              collector.FileName,
                              currentUid.LineNumber,
                              currentUid.LinePosition,
                              0, 0,
                              SRID.MultipleUidUse, currentUid.Value, currentUid.ElementName
                         );

                    }

                    errorFound = true;
                }

            }

            return !errorFound;
        }

        /// <summary>
        /// Parse the input file and get all the information of Uids
        /// </summary>
        /// <param name="fileName">input file</param>
        /// <returns>UidCollector containing all the information for the Uids in the file</returns>
        private UidCollector ParseFile(string fileName)
        {
            UidCollector collector = new UidCollector(fileName  );

            using (Stream xamlStream = File.OpenRead(fileName))
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                XmlParserContext context  = new XmlParserContext(
                    null,                 // nametable
                    nsmgr,                // namespace manager
                    null,                 // xml:Lang scope
                    XmlSpace.Default      // XmlSpace
                    );

                XmlTextReader reader = new XmlTextReader(
                    xamlStream,           // xml stream
                    XmlNodeType.Document, // parsing document
                    context               // parser context
                    );

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element :
                        {
                            if (collector.RootElementLineNumber < 0)
                            {
                                collector.RootElementLineNumber   = reader.LineNumber;
                                collector.RootElementLinePosition = reader.LinePosition;
                            }

                            if (reader.Name.IndexOf('.') >= 0)
                            {
                                // the name has a dot, which suggests it is a property tag.
                                // we will ignore adding uid
                                continue;
                            }

                            Uid currentUid = new Uid(
                                    reader.LineNumber,
                                    reader.LinePosition + reader.Name.Length,
                                    reader.Name,
                                    SpaceInsertion.BeforeUid  // insert space before the Uid
                                    );                                   ;

                            if (reader.HasAttributes)
                            {
                                reader.MoveToNextAttribute();

                                // As a heuristic to better preserve the source file, add uid to the place of the
                                // first attribute
                                currentUid.LineNumber   = reader.LineNumber;
                                currentUid.LinePosition = reader.LinePosition;
                                currentUid.Space        = SpaceInsertion.AfterUid;

                                do
                                {
                                    string namespaceUri = nsmgr.LookupNamespace(reader.Prefix);

                                    if (reader.LocalName == XamlReaderHelper.DefinitionUid
                                     && namespaceUri == XamlReaderHelper.DefinitionNamespaceURI)
                                    {
                                        // found x:Uid attribute, store the actual value and position
                                        currentUid.Value        = reader.Value;
                                        currentUid.LineNumber   = reader.LineNumber;
                                        currentUid.LinePosition = reader.LinePosition;
                                    }
                                    else if (reader.LocalName == "Name"
                                          && namespaceUri == XamlReaderHelper.DefaultNamespaceURI)
                                    {
                                        // found Name attribute, store the Name value
                                        currentUid.FrameworkElementName = reader.Value;
                                    }
                                    else if (reader.LocalName == "Name"
                                          && namespaceUri == XamlReaderHelper.DefinitionNamespaceURI)
                                    {
                                        // found x:Name attribute, store the Name value
                                        currentUid.FrameworkElementName = reader.Value;
                                    }
                                    else if (reader.Prefix == "xmlns")
                                    {
                                        // found a namespace declaration, store the namespace prefix
                                        // so that when we need to add a new namespace declaration later
                                        // we won't reuse the namespace prefix.
                                        collector.AddNamespacePrefix(reader.LocalName);
                                    }
                                }
                                while (reader.MoveToNextAttribute());

                            }

                            if (currentUid.Value == null)
                            {
                                // there is no x:Uid found on this element, we need to resolve the
                                // namespace prefix in order to add the Uid
                                string prefix = nsmgr.LookupPrefix(XamlReaderHelper.DefinitionNamespaceURI);
                                if (prefix != string.Empty)
                                    currentUid.NamespacePrefix = prefix;
                            }

                            collector.AddUid(currentUid);
                            break;
                        }
                    }
                }
            }

            return collector;
        }

        //-----------------------------------
        // Private members
        //-----------------------------------
        private UidTask       _task;            // task
        private ITaskItem[]   _markupFiles;     // input Xaml files
        private string        _taskAsString;    // task string
        private string        _backupPath;      // path to store to backup source Xaml files
        private const string UnknownErrorID = "UM1000";
    }

    // represent all the information about a Uid
    // The uid may be valid, absent, or duplicate
    internal sealed class Uid
    {
        internal Uid(
            int     lineNumber,
            int     linePosition,
            string  elementName,
            SpaceInsertion    spaceInsertion
            )
        {
            LineNumber          = lineNumber;
            LinePosition        = linePosition;
            ElementName         = elementName;
            Value               = null;
            NamespacePrefix     = null;
            FrameworkElementName  = null;
            Status              = UidStatus.Valid;
            Space               = spaceInsertion;

        }

        internal int        LineNumber;         // Referenced line number of the original document
        internal int        LinePosition;       // Reference line position of the original document
        internal string     ElementName;        // name of the element that needs this uid
        internal SpaceInsertion    Space;       // Insert a space before/after the Uid

        internal string     Value;              // value of the uid
        internal string     NamespacePrefix;    // namespace prefix for the uid
        internal string     FrameworkElementName; // the FrameworkElement.Name of element
        internal UidStatus  Status;             // the status of the this uid

    }

    internal enum UidStatus : byte
    {
        Valid       = 0,    // uid is valid
        Absent      = 1,    // uid is absent
        Duplicate   = 2,    // uid is duplicated
    }

    internal enum SpaceInsertion : byte
    {
        BeforeUid,          // Insert a space before the Uid
        AfterUid            // Insert a space after the Uid
    }

    // a class collects all the information about Uids per file
    internal sealed class UidCollector
    {
        public UidCollector(string fileName)
        {
            _uids               = new List<Uid>(32);
            _namespacePrefixes  = new List<string>(2);
            _uidTable           = new Hashtable();
            _fileName           = fileName;
            _sequenceMaxIds     = new Hashtable();
        }

        // remembering all the namespace prefixes in the file
        // in case we need to add a new definition namespace declaration
        public void AddNamespacePrefix(string prefix)
        {
            _namespacePrefixes.Add(prefix);
        }

        // add the uid to the collector
        public void AddUid(Uid uid)
        {
            _uids.Add(uid);

            // set the uid status according to the raw data
            if (uid.Value == null)
            {
                uid.Status = UidStatus.Absent;
            }
            else  if (_uidTable.Contains(uid.Value))
            {
                uid.Status = UidStatus.Duplicate;
            }
            else
            {
                // valid uid, store it
                StoreUid(uid.Value);
            }
        }

        public void ResolveUidErrors()
        {
            for (int i = 0; i < _uids.Count; i++)
            {
                Uid currentUid = _uids[i];

                if ( currentUid.Status == UidStatus.Absent
                  && currentUid.NamespacePrefix == null
                  && _namespacePrefixForMissingUid == null)
                {
                    // there is Uid not in scope of any definition namespace
                    // we will need to generate a new namespace prefix for them
                    _namespacePrefixForMissingUid = GeneratePrefix();
                }

                if (currentUid.Status != UidStatus.Valid)
                {
                    // resolve invalid uids
                    currentUid.Value = GetAvailableUid(currentUid);
                }
            }
        }

        public int RootElementLineNumber
        {
            get { return _rootElementLineNumber; }
            set { _rootElementLineNumber = value; }
        }

        public int RootElementLinePosition
        {
            get { return _rootElementLinePosition; }
            set { _rootElementLinePosition = value; }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public Uid this[int index]
        {
            get
            {
                return _uids[index];
            }
        }

        public int Count
        {
            get { return _uids.Count; }
        }

        public string NamespaceAddedForMissingUid
        {
            get { return _namespacePrefixForMissingUid; }
        }


        //-------------------------------------
        // Private methods
        //-------------------------------------
        private void StoreUid(string value)
        {
            // we just want to check for existence, so storing a null
            _uidTable[value] = null;

            string uidSequence;
            Int64 index;

            ParseUid(value, out uidSequence, out index);
            if (uidSequence != null)
            {
                if (_sequenceMaxIds.Contains(uidSequence))
                {
                    Int64 maxIndex = (Int64)_sequenceMaxIds[uidSequence];
                    if (maxIndex < index)
                    {
                        _sequenceMaxIds[uidSequence] = index;
                    }
                }
                else
                {
                    _sequenceMaxIds[uidSequence] = index;
                }
            }
        }

        private string GetAvailableUid(Uid uid)
        {
            string availableUid;

            // copy the ID if available
            if (uid.FrameworkElementName != null
             && (!_uidTable.Contains(uid.FrameworkElementName))
             )
            {
                availableUid = uid.FrameworkElementName;
            }
            else
            {
                // generate a new id
                string sequence = GetElementLocalName(uid.ElementName);
                Int64 index;

                if (_sequenceMaxIds.Contains(sequence))
                {
                    index = (Int64) _sequenceMaxIds[sequence];

                    if (index == Int64.MaxValue)
                    {
                        // this sequence reaches the max
                        // we fallback to create a new sequence
                        index = -1;
                        while (index < 0)
                        {
                            sequence = (_uidSequenceFallbackCount == 0) ?
                                UidFallbackSequence
                              : UidFallbackSequence + _uidSequenceFallbackCount;

                            if (_sequenceMaxIds.Contains(sequence))
                            {
                                index = (Int64) _sequenceMaxIds[sequence];
                                if (index < Int64.MaxValue)
                                {
                                    // found the fallback sequence with valid index
                                    index ++;
                                    break;
                                }
                            }
                            else
                            {
                                // create a new sequence from 1
                                index = 1;
                                break;
                            }

                            _uidSequenceFallbackCount ++;
                        }
                    }
                    else
                    {
                        index ++;
                    }
                }
                else
                {
                    // a new sequence
                    index = 1;
                }

                availableUid = sequence + UidSeparator + index;
            }

            // store the uid so that it won't be used again
            StoreUid(availableUid);
            return availableUid;
        }

        private void ParseUid(string uid, out string prefix, out Int64 index)
        {
            // set prefix and index to invalid values
            prefix = null;
            index  = -1;

            if (uid == null) return;

            int separatorIndex = uid.LastIndexOf(UidSeparator);
            if (separatorIndex > 0)
            {
                string suffix = uid.Substring(separatorIndex + 1);

                // Disable Presharp warning 6502 : catch block shouldn't have empty body
                #pragma warning disable 6502
                try {
                    index  = Int64.Parse(suffix, TypeConverterHelper.InvariantEnglishUS);
                    prefix = uid.Substring(0, separatorIndex);
                }
                catch (FormatException)
                {
                    // wrong format
                }
                catch (OverflowException)
                {
                    // not acceptable uid
                }
                #pragma warning restore 6502
            }
        }

        private string GetElementLocalName(string typeFullName)
        {
            int index = typeFullName.LastIndexOf('.');
            if (index > 0)
            {
                return typeFullName.Substring(index + 1);
            }
            else
            {
                return typeFullName;
            }
        }

        private string GeneratePrefix()
        {
            Int64 ext = 1;
            string prefix = UidNamespaceAbbreviation.ToString(TypeConverterHelper.InvariantEnglishUS);

            // Disable Presharp warning 6502 : catch block shouldn't have empty body
            #pragma warning disable 6502
            try
            {
                // find a prefix that is not used in the Xaml
                // from x1, x2, ... x[n]
                while (_namespacePrefixes.Contains(prefix))
                {
                    prefix = UidNamespaceAbbreviation + ext.ToString(TypeConverterHelper.InvariantEnglishUS);
                    ext++;
                }
                return prefix;
            }
            catch (OverflowException)
            {
            }
            #pragma warning restore 6502

            // if overflows, (extreamly imposible), we will return a guid as the prefix
            return Guid.NewGuid().ToString();
        }

        private List<Uid>             _uids;
        private Hashtable             _uidTable;
        private string                _fileName;
        private Hashtable             _sequenceMaxIds;
        private List<string>          _namespacePrefixes;
        private int                   _rootElementLineNumber        = -1;
        private int                   _rootElementLinePosition      = -1;
        private string                _namespacePrefixForMissingUid = null;
        private int                   _uidSequenceFallbackCount     = 0;

        private const char UidNamespaceAbbreviation   = 'x';
        private const char UidSeparator               = '_';
        private const string UidFallbackSequence      = "_Uid";
    }


    // writing to a file, removing or updating uid
    internal sealed class UidWriter
    {
        internal UidWriter(UidCollector collector, Stream source, Stream target)
        {
            _collector     = collector;
            _sourceReader  = new StreamReader(source);

             UTF8Encoding encoding = new UTF8Encoding(true);
            _targetWriter  = new StreamWriter(target, encoding);
            _lineBuffer    = new LineBuffer(_sourceReader.ReadLine());
        }

        // write to target stream and update uids
        internal bool UpdateUidWrite()
        {
            try {
                // we need to add a new namespace
                if (_collector.NamespaceAddedForMissingUid != null)
                {
                    // write to the beginning of the root element
                    WriteTillSourcePosition(
                        _collector.RootElementLineNumber,
                        _collector.RootElementLinePosition
                        );

                    WriteElementTag();
                    WriteSpace();
                    WriteNewNamespace();
                }

                for (int i = 0; i < _collector.Count; i++)
                {
                    Uid currentUid = _collector[i];
                    WriteTillSourcePosition(currentUid.LineNumber, currentUid.LinePosition);

                    if (currentUid.Status == UidStatus.Absent)
                    {

                        if (currentUid.Space == SpaceInsertion.BeforeUid)
                        {
                            WriteSpace();
                        }

                        WriteNewUid(currentUid);

                        if (currentUid.Space == SpaceInsertion.AfterUid)
                        {
                            WriteSpace();
                        }
                    }
                    else if (currentUid.Status == UidStatus.Duplicate)
                    {
                        ProcessAttributeStart(WriterAction.Write);
                        SkipSourceAttributeValue();
                        WriteNewAttributeValue(currentUid.Value);
                    }
                }
                WriteTillEof();
                return true;
            }
            catch (Exception e)
            {
                // PreSharp Complaint 6500 - do not handle null-ref or SEH exceptions.
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }

                return false;
            }
#pragma warning disable 6500
            catch
            {
                return false;
            }
#pragma warning restore 6500
        }

        // writing to the target stream removing uids
        internal bool RemoveUidWrite()
        {
            try {
                for (int i = 0; i < _collector.Count; i++)
                {
                    Uid currentUid = _collector[i];

                    // skipping valid and duplicate uids.
                    if ( currentUid.Status == UidStatus.Duplicate
                      || currentUid.Status == UidStatus.Valid)
                    {
                        // write till the space in front of the Uid
                        WriteTillSourcePosition(currentUid.LineNumber, currentUid.LinePosition - 1);

                        // skip the uid
                        ProcessAttributeStart(WriterAction.Skip);
                        SkipSourceAttributeValue();
                    }
                }

                WriteTillEof();
                return true;
            }
            catch (Exception e)
            {
                // PreSharp Complaint 6500 - do not handle null-ref or SEH exceptions.
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }

                return false;
            }
#pragma warning disable 6500
            catch
            {
                return false;
            }
#pragma warning restore 6500
        }

        private void WriteTillSourcePosition(int lineNumber, int linePosition)
        {
            // write to the correct line
            while (_currentLineNumber < lineNumber)
            {
                // write out the line buffer
                _targetWriter.WriteLine(_lineBuffer.ReadToEnd());
                _currentLineNumber++;
                _currentLinePosition = 1;

                // read one more line
                _lineBuffer.SetLine(_sourceReader.ReadLine());
            }

            // write to the correct line position
            while (_currentLinePosition < linePosition)
            {
                _targetWriter.Write(_lineBuffer.Read());
                _currentLinePosition++;
            }

        }

        private void WriteElementTag()
        {
            if (_lineBuffer.EOL)
            {
                // advance to the non-empty line
                AdvanceTillNextNonEmptyLine(WriterAction.Write);
            }

            char ch = _lineBuffer.Peek();

            // stop when we see space, "/" or ">". That is the end of the
            // element name
            while (!Char.IsWhiteSpace(ch)
                   && ch != '/'
                   && ch != '>'
                  )
            {
                _targetWriter.Write(ch);

                _currentLinePosition++;
                _lineBuffer.Read();
                if (_lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Write);
                }

                ch = _lineBuffer.Peek();
            }
        }

        private void WriteNewUid(Uid uid)
        {
            // construct the attribute name, e.g. x:Uid
            // "x" will be the resolved namespace prefix for the definition namespace
            string attributeName =
                (uid.NamespacePrefix == null) ?
                 _collector.NamespaceAddedForMissingUid + ":" + XamlReaderHelper.DefinitionUid
               : uid.NamespacePrefix + ":" + XamlReaderHelper.DefinitionUid;

            // escape all the Xml entities in the value
            string attributeValue = EscapedXmlEntities.Replace(
                uid.Value,
                EscapeMatchEvaluator
                );

            string clause = string.Format(
                TypeConverterHelper.InvariantEnglishUS,
                "{0}=\"{1}\"",
                attributeName,
                attributeValue
                );

            _targetWriter.Write(clause);
        }

        private void WriteNewNamespace()
        {
            string clause = string.Format(
                TypeConverterHelper.InvariantEnglishUS,
                "xmlns:{0}=\"{1}\"",
                _collector.NamespaceAddedForMissingUid,
                XamlReaderHelper.DefinitionNamespaceURI
                );

            _targetWriter.Write(clause);
        }

        private void WriteNewAttributeValue(string value)
        {
            string attributeValue = EscapedXmlEntities.Replace(
                value,
                EscapeMatchEvaluator
                );

            _targetWriter.Write(
                string.Format(
                    TypeConverterHelper.InvariantEnglishUS,
                    "\"{0}\"",
                    value
                    )
                );
        }

        private void WriteSpace()
        {
             // insert a space
            _targetWriter.Write(" ");
        }

        private void WriteTillEof()
        {
            _targetWriter.WriteLine(_lineBuffer.ReadToEnd());
            _targetWriter.Write(_sourceReader.ReadToEnd());
            _targetWriter.Flush();
        }

        private void SkipSourceAttributeValue()
        {
            char ch = (char) 0;

            // read to the start quote of the attribute value
            while (ch != '\"' && ch != '\'')
            {
                if (_lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Skip);
                }

                ch = _lineBuffer.Read();
                _currentLinePosition ++;
            }

            char attributeValueStart = ch;
            // read to the end quote of the attribute value
            ch = (char) 0;
            while (ch != attributeValueStart)
            {
                if (_lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Skip);
                }

                ch = _lineBuffer.Read();
                _currentLinePosition ++;
            }
        }

        private void AdvanceTillNextNonEmptyLine(WriterAction action)
        {
            do
            {
                if (action == WriterAction.Write)
                {
                    _targetWriter.WriteLine();
                }

                _lineBuffer.SetLine(_sourceReader.ReadLine());
                _currentLineNumber++;
                _currentLinePosition = 1;

            } while (_lineBuffer.EOL);
        }


        private void ProcessAttributeStart(WriterAction action)
        {
            if (_lineBuffer.EOL)
            {
                AdvanceTillNextNonEmptyLine(action);
            }

            char ch;
            do
            {
                ch = _lineBuffer.Read();

                if (action == WriterAction.Write)
                {
                    _targetWriter.Write(ch);
                }

                _currentLinePosition++;

                if (_lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(action);
                }

            } while (ch != '=');
        }

        //
        // source position in a file starts from (1,1)
        //

        private int             _currentLineNumber   = 1;   // current line number in the source stream
        private int             _currentLinePosition = 1;   // current line position in the source stream
        private LineBuffer      _lineBuffer;                // buffer for one line's content

        private UidCollector    _collector;
        private StreamReader    _sourceReader;
        private StreamWriter    _targetWriter;

        //
        // buffer for the content of a line
        // The UidWriter always reads one line at a time from the source
        // and store the line in this buffer.
        //
        private sealed class LineBuffer
        {
            private int    Index;
            private string Content;

            public LineBuffer(string line)
            {
                SetLine(line);
            }

            public void SetLine(string line)
            {
                Content = (line == null) ? string.Empty : line;
                Index   = 0;
            }

            public bool EOL
            {
                get { return (Index == Content.Length); }
            }

            public char Read()
            {
                if (!EOL)
                {
                    return Content[Index++];
                }

                throw new InvalidOperationException();
            }

            public char Peek()
            {
                if (!EOL)
                {
                    return Content[Index];
                }

                throw new InvalidOperationException();
            }

            public string ReadToEnd()
            {
                if (!EOL)
                {
                    int temp = Index;
                    Index    = Content.Length;

                    return Content.Substring(temp);
                }

                return string.Empty;
            }
        }

        private enum WriterAction
        {
            Write = 0,  // write the content
            Skip  = 1,  // skip the content
        }

        private static Regex          EscapedXmlEntities   = new Regex("(<|>|\"|'|&)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static MatchEvaluator EscapeMatchEvaluator = new MatchEvaluator(EscapeMatch);

        /// <summary>
        /// the delegate to escape the matched pattern
        /// </summary>
        private static string EscapeMatch(Match match)
        {
            switch (match.Value)
            {
                case "<":
                    return "&lt;";
                case ">":
                    return "&gt;";
                case "&":
                    return "&amp;";
                case "\"":
                    return "&quot;";
                case "'":
                    return "&apos;";
                default:
                   return match.Value;
            }
        }
    }
}
