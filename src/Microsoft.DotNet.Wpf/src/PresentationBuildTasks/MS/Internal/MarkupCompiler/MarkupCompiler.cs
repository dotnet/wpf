// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//   Markup Compiler class that compiles the markup in a Xaml file into a
//   binary stream (Baml) and\or code (IL) in an assembly.
//
//---------------------------------------------------------------------------

#pragma warning disable 1634, 1691

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using System.Security.Cryptography;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using System.Threading;
using MS.Internal.Markup;
using MS.Internal.Tasks;
using MS.Utility;   // for SR
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks.Windows;
using System.Runtime.CompilerServices;

namespace MS.Internal
{
    internal sealed class MarkupCompiler
    {
#region ExternalAPI

#region Public Properties

        ///<summary>String that specifies the directory to place the generated files into</summary>
        public string TargetPath
        {
            get { return _targetPath; }
            set { _targetPath = value;}
        }

        /// <summary>
        /// The version of the assembly
        /// </summary>
        public string AssemblyVersion
        {
            get { return _assemblyVersion; }
            set { _assemblyVersion = value; }
        }

        /// <summary>
        /// The public key token of the assembly
        /// </summary>
        public string AssemblyPublicKeyToken
        {
            get { return _assemblyPublicKeyToken; }
            set { _assemblyPublicKeyToken = value; }
        }

        ///<summary>Array of loose files associated with this assembly</summary>
        public string [] ContentList
        {
            get { return _contentList; }
            set { _contentList = value; }
        }

        /// <summary>
        /// Splash screen image to be displayed before application init
        /// </summary>
        public string SplashImage
        {
            set { _splashImage = value; }
        }

        ///<summary> Array of local xaml page files to be compiled at pass2 </summary>
        public string[] LocalXamlPages
        {
            get { return _localXamlPages; }
        }

        ///<summary> Local Application xaml file to be compiled at pass2 </summary>
        public string LocalXamlApplication
        {
            get { return _localXamlApplication; }
        }

        /// <summary>
        /// ReferenceAssemblyList. every item is an instance of ReferenceAssembly.
        /// </summary>
        /// <value></value>
        public ArrayList ReferenceAssemblyList
        {
            get { return _referenceAssemblyList; }
            set { _referenceAssemblyList = value; }
        }

        ///<summary>The language source file extension set in a project or the registered default.</summary>
        public string LanguageSourceExtension
        {
            get { return _languageSourceExtension; }
            set { _languageSourceExtension = value; }
        }

        ///<summary>Allows to hook a custom parser during compilation.</summary>
        public ParserHooks ParserHooks
        {
            get { return _parserHooks; }
            set { _parserHooks = value; }
        }

        ///<summary>If true code for supporting hosting in Browser is generated</summary>
        public bool HostInBrowser
        {
            get { return _hostInBrowser; }
            set { _hostInBrowser = value; }
        }

        ///<summary>Generate Debug information in the BAML file.</summary>
        public bool XamlDebuggingInformation
        {
            get { return _xamlDebuggingInformation; }
            set { _xamlDebuggingInformation = value; }
        }

        /// <summary>
        /// Get/Sets the TaskFileService which is used for abstracting simple
        /// files services provided by CLR and the HostObject (IVsMsBuildTaskFileManager)
        /// </summary>
        internal ITaskFileService TaskFileService
        {
            get { return _taskFileService; }
            set { _taskFileService = value; }
        }

        /// <summary>
        /// The CompilerWrapper's TaskLoggingHelper for reporting compiler warnings.
        /// </summary>
        internal TaskLoggingHelper TaskLogger
        {
            set { _taskLogger = value; }
        }

        /// <summary>
        /// Support custom IntermediateOutputPath and BaseIntermediateOutputPath outside the project path
        /// </summary>
        internal bool SupportCustomOutputPaths { get; set; } = false;

        // If the xaml has local references, then it could have internal element & properties
        // but there is no way to determine this until MCPass2. Yet, GeneratedInternalTypeHelper,
        // which is the class that allows access to legitimate internals, needs to be generated
        // in MCPass1. So, to determine if GeneratedInternalTypeHelper.cs needs to be kept or not,
        // MCPass1 & MCPass2 task will use the HasInternals property as indicated below:

        // In pass1, if this property returns true, it will be due to friend internals & so
        // MCPass1 will just decide to keep the file & this property will be ignored in MCPass2
        // if that task is executed as well.

        // In pass1, if this property returns false, MCPass2 will look at this property again after
        // the Xaml Compiler has been called to compile all the local markup files. Now if this
        // property still returns false, the file will be removed, else it will be kept, because
        // this property was true as the xaml compiler encountered local or friend internals
        // during MCPass2.

        // The above will apply only for a clean build, not incremental build.
        public static bool HasInternals
        {
            get { return XamlTypeMapper.HasInternals; }
        }

#endregion Public Properties

#region Public Events

        /// <summary>
        /// The Error event is fired when an error is encountered while compiling a xaml file.
        /// </summary>
        public event MarkupErrorEventHandler Error;

        /// <summary>
        /// The SourceFileResolve event is fired when it starts to compile one xaml file or handle
        /// resource file. The event handler will resolve the original filepath to a new
        /// SourcePath and RelativeSourceFilePath.
        /// </summary>
        public event SourceFileResolveEventHandler SourceFileResolve;

#endregion Public Events

#region Public Methods

        ///<summary>Complies list of file items comprising an Application.</summary>
        public void Compile(CompilationUnit cu)
        {
            // KnownTypes, XamlTypeMapper, and ReflectionHelper all hold on to data statically that 
            // must not be reused between compilations as different compilations can target different
            //
            // Defensively clear static data even though the prior compilation should have done it.
            // This is done to mitigate against a failed cleanup of a prior compilation from influencing
            // the current compilation.
            XamlTypeMapper.Clear();
            KnownTypes.Clear();
            ReflectionHelper.Dispose();

            try
            {
                CompileCore(cu);
            }
            finally
            {
                // Don't rely on next compilation to reset as that would unnecessarily delay
                // garbage collection of this static data that cannot be reused by the next compilation.
                // Also, the ReflectionHelper must be disposed now to release file locks on assemblies.
                XamlTypeMapper.Clear();
                KnownTypes.Clear();
                ReflectionHelper.Dispose();
            }
        }

        private void CompileCore(CompilationUnit cu)
        {
            try
            {
                AssemblyName = cu.AssemblyName;
                InitCompilerState();

                DefaultNamespace = cu.DefaultNamespace;
                _compilationUnitSourcePath = cu.SourcePath;

                if (!IsLanguageSupported(cu.Language))
                {
                    OnError(new Exception(SR.Get(SRID.UnknownLanguage, cu.Language)));
                    return;
                }

                if (!cu.Pass2)
                {
                    EnsureLanguageSourceExtension();
                }

                if (cu.ApplicationFile.Path != null && cu.ApplicationFile.Path.Length > 0)
                {
                    Initialize(cu.ApplicationFile);
                    ApplicationFile = SourceFileInfo.RelativeSourceFilePath;

                    if (ApplicationFile.Length > 0)
                    {
                        IsCompilingEntryPointClass = true;
                        _Compile(cu.ApplicationFile.Path, cu.Pass2);
                        IsCompilingEntryPointClass = false;

                        if (_pendingLocalFiles != null && _pendingLocalFiles.Count == 1)
                        {
                            Debug.Assert(!cu.Pass2);
                            _localXamlApplication = (string)_pendingLocalFiles[0];
                            _pendingLocalFiles.Clear();
                        }
                    }
                }

                if (cu.FileList != null)
                {
                    for (int i = 0; i < cu.FileList.Length; i++)
                    {
                        FileUnit sourceFile = cu.FileList[i];

                        Initialize(sourceFile);
                        if (SourceFileInfo.RelativeSourceFilePath.Length > 0)
                        {
                            _Compile(sourceFile.Path, cu.Pass2);
                        }
                    }

                    if (_pendingLocalFiles != null && _pendingLocalFiles.Count > 0)
                    {
                        Debug.Assert(!cu.Pass2);
                        _localXamlPages = (string[])_pendingLocalFiles.ToArray(typeof(string));
                        _pendingLocalFiles.Clear();
                    }
                }

                if (!cu.Pass2 && ContentList != null && ContentList.Length > 0)
                {
                    GenerateLooseContentAttributes();
                }

                Debug.Assert(!cu.Pass2 || _pendingLocalFiles == null);
                Debug.Assert(_pendingLocalFiles == null || _pendingLocalFiles.Count == 0);
                _pendingLocalFiles = null;

                if (cu.Pass2)
                {
                    _localAssembly = null;
                    _localXamlApplication = null;
                    _localXamlPages = null;
                }
            }
            finally
            {

                if (s_hashAlgorithm != null)
                {
                    s_hashAlgorithm.Clear();
                    s_hashAlgorithm = null;
                }
            }
        }

#endregion Public Methods

#endregion ExternalAPI

#region Implementation

#region Properties

        private CompilerInfo CompilerInfo
        {
            get { return _ci; }
            set
            {
                _ci = value;
                if (value == null)
                {
                    _codeProvider = null;
                }
            }
        }

        private string ApplicationFile
        {
            get { return _applicationFile; }
            set { _applicationFile = value; }
        }

        private string DefaultNamespace
        {
            get { return _defaultNamespace; }
            set
            {
                IsValidCLRNamespace(value, true);
                _defaultNamespace = value;
            }
        }

        private bool IsCodeNeeded
        {
            get { return _isCodeNeeded; }
            set { _isCodeNeeded = value; }
        }

        internal bool IsBamlNeeded
        {
            get { return IsCompilingEntryPointClass ? _isBamlNeeded : true; }
            set { _isBamlNeeded = value; }
        }

        internal bool IsRootPublic
        {
            get { return _ccRoot != null && _ccRoot.CodeClass.TypeAttributes == TypeAttributes.Public; }
        }

        internal bool ProcessingRootContext
        {
            get { return _ccRoot == null; }
        }

        internal bool IsRootNameScope
        {
            get
            {
                CodeContext cc = (CodeContext)_codeContexts.Peek();
                return cc.IsAllowedNameScope;
            }
        }

        internal bool HasLocalEvent
        {
            get { return _hasLocalEvent; }
            set { _hasLocalEvent = value; }
        }

        internal static bool HasLocalReference
        {
            get { return XamlTypeMapper.HasLocalReference; }
        }

        internal Assembly LocalAssembly
        {
            get
            {
                if (_localAssembly == null)
                {
                    if (LocalAssemblyFile != null)
                    {
                        _localAssembly = ReflectionHelper.LoadAssembly(LocalAssemblyFile.AssemblyName, LocalAssemblyFile.Path);
                    }
                }

                return _localAssembly;
            }
        }

        internal ReferenceAssembly LocalAssemblyFile
        {
            get { return _localAssemblyFile;  }
            set { _localAssemblyFile = value; }
        }

        internal string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        internal SourceFileInfo SourceFileInfo
        {
            get { return _sourceFileInfo; }
            set { _sourceFileInfo = value; }
        }

        internal bool IsCompilingEntryPointClass
        {
            get { return _isCompilingEntryPointClass; }
            set { _isCompilingEntryPointClass = value; _isBamlNeeded = !value; }
        }

        internal static string DefinitionNSPrefix
        {
            get { return _definitionNSPrefix; }
            set { _definitionNSPrefix = value; }
        }

        internal string Language
        {
            get { return _language; }
        }

#endregion Properties

#region CompileUnit

        private void InitCompilerState()
        {
            _hasGeneratedInternalTypeHelper = false;
            CompilerInfo = null;
            InitializeReflectionHelper();
            InitializeTypeMapper();
        }

        //
        // Generate the SourceFileInfo for the source file.
        // Do the appropriate initiallization work and file checking.
        //
        private void Initialize(FileUnit sourceFile)
        {
            try
            {
                // Keep the SourceFileInfo for the passed source file.
                SourceFileInfo = OnSourceFileResolve(sourceFile);

                // Process the input file
                if (sourceFile.Path == null || !SourceFileInfo.IsXamlFile)
                {
                    ThrowCompilerException(SRID.InvalidMarkupFile);
                }

                if (!TaskFileService.Exists(sourceFile.Path))
                {
                    ThrowCompilerException(SRID.FileNotFound, sourceFile.Path);
                }

                // Prime the output directory
                if (TargetPath.Length > 0)
                {
                    // check for ending Path.DirectorySeparatorChar
                    if (!TargetPath.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    {
                        TargetPath += Path.DirectorySeparatorChar;
                    }
                }

                int pathEndIndex = SourceFileInfo.RelativeSourceFilePath.LastIndexOf(Path.DirectorySeparatorChar);
                string targetPath = TargetPath + SourceFileInfo.RelativeSourceFilePath.Substring(0, pathEndIndex + 1);

                // Create if not already exists
                if (targetPath.Length > 0 && !Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
            }
            // All exceptions including NullRef & SEH need to be caught by the markupcompiler
            // since it is an app and not a component.
            #pragma warning suppress 6500
            catch (Exception e)
            {
                OnError(e);
            }
        }

        private void _Compile(string relativeSourceFile, bool pass2)
        {
            bool error = false;
            Stream bamlStream = null;
            XamlParser xamlParser = null;

            try
            {
                DefinitionNSPrefix = DEFINITION_PREFIX;
                IsCodeNeeded = false;

                _ccRoot = null;
                _hasLocalEvent = false;
                _codeContexts = new Stack();
                _parserContext = new ParserContext();
                _parserContext.XamlTypeMapper = _typeMapper;
                _hasEmittedEventSetterDeclaration = false;

                bamlStream = new MemoryStream();
                BamlRecordWriter bamlWriter = new BamlRecordWriter(bamlStream, _parserContext, true);
                bamlWriter.DebugBamlStream = XamlDebuggingInformation;

                xamlParser = new ParserExtension(this, _parserContext, bamlWriter, SourceFileInfo.Stream, pass2);

                xamlParser.ParserHooks = ParserHooks;

                try
                {
                    xamlParser.Parse();
                }
                finally
                {
                    _typeMapper.ResetMapper();
                }
            }
            catch (XamlParseException e)
            {
                OnError(e);
                error = true;
            }
            // All exceptions including NullRef & SEH need to be caught by the markupcompiler
            // since it is an app and not a component.
#pragma warning suppress 6500
            catch (Exception e)
            {
                OnError(e);
                error = true;
            }
            finally
            {
                if (!error &&
                    xamlParser.BamlRecordWriter == null &&
                    IsBamlNeeded)
                {
                    if (_pendingLocalFiles == null)
                    {
                        _pendingLocalFiles = new ArrayList(10);
                    }

                    _pendingLocalFiles.Add(relativeSourceFile);
                }

                if (_codeContexts != null)
                {
                    _codeContexts.Clear();
                    _codeContexts = null;
                }


                if (SourceFileInfo != null)
                {
                    SourceFileInfo.CloseStream();
                }

                if (bamlStream != null)
                {
                    bamlStream.Close();
                    bamlStream = null;
                }
            }
        }

        private void GenerateSource()
        {
            Debug.Assert(_codeContexts.Count == 0);

            CodeNamespace cnsImports = IsLanguageCSharp ? new CodeNamespace() : _ccRoot.CodeNS;

            if (IsCodeNeeded)
            {
                cnsImports.Imports.Add(new CodeNamespaceImport("System"));

                if (_usingNS != null)
                {
                    foreach (string u in _usingNS)
                    {
                        cnsImports.Imports.Add(new CodeNamespaceImport(u));
                    }
                }

                //  } end SubClass
                _ccRoot.CodeNS.Types.Add(_ccRoot.CodeClass);
            }

            if (_usingNS != null)
            {
                _usingNS.Clear();
                _usingNS = null;
            }

            if (IsCompilingEntryPointClass)
            {
                GenerateAppEntryPoint();
            }

            if (IsCodeNeeded)
            {
                MemoryStream codeMemStream = new MemoryStream();

                // using Disposes the StreamWriter when it ends.  Disposing the StreamWriter
                // also closes the underlying MemoryStream.  Furthermore, don't add BOM here since
                // TaskFileService.WriteGeneratedCodeFileFile adds it.
                using (StreamWriter codeStreamWriter = new StreamWriter(codeMemStream, new UTF8Encoding(false)))
                {
                    CodeGeneratorOptions o = new CodeGeneratorOptions();

                    // } end namespace
                    CodeCompileUnit ccu = new CodeCompileUnit();

                    // Use SHA1 if compat flag is set
                    // generate pragma checksum data
                    if (s_hashAlgorithm == null)
                    {
                        s_hashAlgorithm = SHA1.Create();
                        s_hashGuid = s_hashSHA1Guid;
                    }

                    CodeChecksumPragma csPragma = new CodeChecksumPragma();
                    csPragma.FileName = ParentFolderPrefix + SourceFileInfo.RelativeSourceFilePath + XAML;
                    csPragma.ChecksumAlgorithmId = s_hashGuid;
                    csPragma.ChecksumData = TaskFileService.GetChecksum(SourceFileInfo.OriginalFilePath, s_hashGuid);
                    ccu.StartDirectives.Add(csPragma);

                    if (cnsImports != _ccRoot.CodeNS)
                    {
                        ccu.Namespaces.Add(cnsImports);
                    }

                    ccu.Namespaces.Add(_ccRoot.CodeNS);

                    CodeDomProvider codeProvider = EnsureCodeProvider();

                    if (codeProvider.Supports(GeneratorSupport.PartialTypes) && _ccRoot.SubClass.Length == 0)
                    {
                        _ccRoot.CodeClass.IsPartial = true;
                    }

                    codeProvider.GenerateCodeFromCompileUnit(ccu, codeStreamWriter, o);

                    codeStreamWriter.Flush();
                    TaskFileService.WriteGeneratedCodeFile(codeMemStream.ToArray(),
                        TargetPath + SourceFileInfo.RelativeSourceFilePath,
                        SharedStrings.GeneratedExtension, SharedStrings.IntellisenseGeneratedExtension,
                        LanguageSourceExtension);
                }
            }

            // Generate the InternalTypeHelper class in a separate code file only once and on an as
            // needed basis for the current assembly being built. This class provides support for
            // accessing legitimate internal types and properties that are present in the same (local)
            // or a friend assembly and it is generated only when any such internals are actually
            // encountered in any of the xaml files in the project.
            GenerateInternalTypeHelperImplementation();
        }

        //
        // Return FileInfo for the given source file.
        //
        private SourceFileInfo OnSourceFileResolve(FileUnit file)
        {
            SourceFileInfo sourceFileInfo;

            if (SourceFileResolve != null)
            {
                //
                // If SourceFileResolve event handler is registered,  the handler
                // is responsible for generating the SourceFileInfo.
                // This is for MSBUILD tasks.
                //
                SourceFileResolveEventArgs scea = new SourceFileResolveEventArgs(file);

                SourceFileResolve(this, scea);

                sourceFileInfo = scea.SourceFileInfo;
            }
            else
            {
                // If SourceFileResolve event handler is not registered,  generate
                // the default SourceFileInfo for this file.
                //
                sourceFileInfo = new SourceFileInfo(file);

                sourceFileInfo.SourcePath = _compilationUnitSourcePath;

                if (sourceFileInfo.IsXamlFile)
                {
                    int fileExtIndex = file.Path.LastIndexOf(DOTCHAR);
                    
                    sourceFileInfo.RelativeSourceFilePath = file.Path.Substring(0, fileExtIndex);
                }
            }

            return sourceFileInfo;
        }

#endregion CompileUnit

#region ErrorHandling

        static void ThrowCompilerException(string id)
        {
            string message = SR.Get(id);
            ThrowCompilerExceptionImpl(message);
        }

        internal static void ThrowCompilerException(string id, string value)
        {
            string message = SR.Get(id, value);
            ThrowCompilerExceptionImpl(message);
        }

        internal static void ThrowCompilerException(string id, string value1, string value2)
        {
            string message = SR.Get(id, value1, value2);
            ThrowCompilerExceptionImpl(message);
        }

        internal static void ThrowCompilerException(string id, string value1, string value2, string value3)
        {
            string message = SR.Get(id, value1, value2, value3);
            ThrowCompilerExceptionImpl(message);
        }

        static void ThrowCompilerException(string id, string value1, string value2, string value3, string value4)
        {
            string message = SR.Get(id, value1, value2, value3, value4);
            ThrowCompilerExceptionImpl(message);
        }

        static void ThrowCompilerExceptionImpl(string message)
        {
            Exception compilerException = new Exception(message);
            throw compilerException;
        }

        internal void OnError(Exception e)
        {
            // Don't treat an AssemblyVersion parsing error as a XamlParseException.
            // Throw it back to the task execution.
            if(e is AssemblyVersionParseException)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw();
            }

            if (Error != null)
            {
                XamlParseException xe = e as XamlParseException;
                int lineNum = xe != null ? xe.LineNumber : 0;
                int linePos = xe != null ? xe.LinePosition : 0;
                string filename = SourceFileInfo.OriginalFilePath;

                MarkupErrorEventArgs eea = new MarkupErrorEventArgs(e, lineNum, linePos, filename);
                Error(this, eea);
            }
        }

#endregion ErrorHandling

#region Definition Namespace processing

        internal void ProcessDefinitionNamespace(XamlDefTagNode xamlDefTagNode)
        {
            bool exitLoop = false;
            XmlReader xmlReader = xamlDefTagNode.XmlReader;
            string LocalName = xmlReader.LocalName;
            bool isEmptyElement = xamlDefTagNode.IsEmptyElement;
            bool isProcessingCodeTag = false;

            do
            {
                XmlNodeType currNodeType = xmlReader.NodeType;
                switch (currNodeType)
                {
                    case XmlNodeType.Element:
                    {
                        if (isProcessingCodeTag)
                        {
                            ThrowCompilerException(SRID.DefnTagsCannotBeNested, DefinitionNSPrefix, LocalName, xmlReader.LocalName);
                        }

                        switch (LocalName)
                        {
                            case CODETAG:
                                isProcessingCodeTag = true;
                                if (!IsCodeNeeded)
                                {
                                    ThrowCompilerException(SRID.MissingClassDefinitionForCodeTag,
                                                           _ccRoot.ElementName,
                                                           DefinitionNSPrefix,
                                                           SourceFileInfo.RelativeSourceFilePath + XAML);
                                }

                                bool moreAttributes = xmlReader.MoveToFirstAttribute();
                                while (moreAttributes)
                                {
                                    string attributeNamespaceUri = xmlReader.LookupNamespace(xmlReader.Prefix);
                                    if (!attributeNamespaceUri.Equals(XamlReaderHelper.DefinitionNamespaceURI) ||
                                        !xmlReader.LocalName.Equals(XamlReaderHelper.DefinitionUid))
                                    {
                                        ThrowCompilerException(SRID.AttributeNotAllowedOnCodeTag,
                                                               xmlReader.Name,
                                                               DefinitionNSPrefix,
                                                               CODETAG);
                                    }

                                    moreAttributes = xmlReader.MoveToNextAttribute();
                                }

                                break;

                            default:
                                ThrowCompilerException(SRID.UnknownDefinitionTag, DefinitionNSPrefix, LocalName);
                                break;
                        }

                        // if an empty element do a Reader to
                        // get to the next node and then exit
                        if (isEmptyElement)
                        {
                            xmlReader.Read();
                            exitLoop = true;
                        }

                        break;
                    }

                    case XmlNodeType.EndElement:
                    {
                        xmlReader.Read();
                        exitLoop = true;
                        break;
                    }

                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    {
                        IXmlLineInfo xmlLineInfo = xmlReader as IXmlLineInfo;
                        int lineNumber = 0;

                        if (null != xmlLineInfo)
                        {
                            lineNumber = xmlLineInfo.LineNumber;
                        }

                        if (LocalName.Equals(CODETAG))
                        {
                            AddCodeSnippet(xmlReader.Value, lineNumber);
                        }
                        else
                        {
                            ThrowCompilerException(SRID.IllegalCDataTextScoping, DefinitionNSPrefix, LocalName, (currNodeType == XmlNodeType.CDATA ? "a CDATA section" : "text content"));
                        }

                        break;
                    }
                }
            }
            while (!exitLoop && xmlReader.Read());
        }

#endregion Definition Namespace processing

#region Baml Hookup Functions

        private CodeMemberMethod EnsureStyleConnector()
        {
            if (_ccRoot.StyleConnectorFn == null)
            {
                _ccRoot.StyleConnectorFn = new CodeMemberMethod();
                _ccRoot.StyleConnectorFn.Name = CONNECT;
                _ccRoot.StyleConnectorFn.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                _ccRoot.StyleConnectorFn.PrivateImplementationType = new CodeTypeReference(KnownTypes.Types[(int)KnownElements.IStyleConnector]);

                // void IStyleConnector.Connect(int connectionId, object target) {
                //
                CodeParameterDeclarationExpression param1 = new CodeParameterDeclarationExpression(typeof(int), CONNECTIONID);
                CodeParameterDeclarationExpression param2 = new CodeParameterDeclarationExpression(typeof(object), TARGET);
                _ccRoot.StyleConnectorFn.Parameters.Add(param1);
                _ccRoot.StyleConnectorFn.Parameters.Add(param2);

                AddDebuggerNonUserCodeAttribute(_ccRoot.StyleConnectorFn);
                AddGeneratedCodeAttribute(_ccRoot.StyleConnectorFn);
                AddEditorBrowsableAttribute(_ccRoot.StyleConnectorFn);
                AddSuppressMessageAttribute(_ccRoot.StyleConnectorFn, "Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes");
                AddSuppressMessageAttribute(_ccRoot.StyleConnectorFn, "Microsoft.Performance", "CA1800:DoNotCastUnnecessarily");
                AddSuppressMessageAttribute(_ccRoot.StyleConnectorFn, "Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity");

                if (SwitchStatementSupported())
                {
                    // switch (connectionId) -- Start Switch
                    // {
                    CodeSnippetStatement css = new CodeSnippetStatement(SWITCH_STATEMENT);
                    _ccRoot.StyleConnectorFn.Statements.Add(css);
                }
            }

            return _ccRoot.StyleConnectorFn;
        }

        internal void ConnectStyleEvent(XamlClrEventNode xamlClrEventNode)
        {
            CodeConditionStatement ccsConnector = null;

            // validate the event handler name per C# grammar for identifiers
            ValidateEventHandlerName(xamlClrEventNode.EventName, xamlClrEventNode.Value);

            EnsureStyleConnector();

            if (!xamlClrEventNode.IsSameScope)
            {
                int connectionId = xamlClrEventNode.ConnectionId;
                if (SwitchStatementSupported())
                {
                    // break any previous case staements as we are starting a new connection scope.
                    if (_ccRoot.StyleConnectorFn.Statements.Count > 1)
                    {
                        CodeSnippetStatement cssBreak = new CodeSnippetStatement(BREAK_STATEMENT);
                        _ccRoot.StyleConnectorFn.Statements.Add(cssBreak);
                    }

                    // case 1:
                    //
                    CodeSnippetStatement cssCase = new CodeSnippetStatement(CASE_STATEMENT + connectionId + COLON);
                    _ccRoot.StyleConnectorFn.Statements.Add(cssCase);
                }
                else
                {
                    // if (connectionId == 1)
                    //
                    ccsConnector = new CodeConditionStatement();
                    ccsConnector.Condition = new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression(CONNECTIONID),
                                                                              CodeBinaryOperatorType.ValueEquality,
                                                                              new CodePrimitiveExpression(connectionId));
                }
            }
            else if (!SwitchStatementSupported())
            {
                // if in the same scope then use the if statement that was last generated
                // at the start of the scope
                Debug.Assert(_ccRoot.StyleConnectorFn.Statements.Count > 0);
                ccsConnector = _ccRoot.StyleConnectorFn.Statements[_ccRoot.StyleConnectorFn.Statements.Count - 1] as CodeConditionStatement;
                Debug.Assert(ccsConnector != null);
            }

            CodeArgumentReferenceExpression careTarget = new CodeArgumentReferenceExpression(TARGET);

            if (xamlClrEventNode.IsStyleSetterEvent)
            {
                // EventSetter declaration only once to avoid warning!
                if (!_hasEmittedEventSetterDeclaration)
                {
                    _hasEmittedEventSetterDeclaration = true;

                    // EventSetter eventSetter;
                    //
                    CodeVariableDeclarationStatement cvdsES = new CodeVariableDeclarationStatement(KnownTypes.Types[(int)KnownElements.EventSetter], EVENTSETTER);
                    _ccRoot.StyleConnectorFn.Statements.Insert(0, cvdsES);
                }


                // eventSetter = new EventSetter();
                //
                CodeExpression[] esParams = {};
                CodeVariableReferenceExpression cvreES = new CodeVariableReferenceExpression(EVENTSETTER);
                CodeAssignStatement casES = new CodeAssignStatement(cvreES,
                                                                    new CodeObjectCreateExpression(KnownTypes.Types[(int)KnownElements.EventSetter],
                                                                                                   esParams));

                // eventSetter.Event = Button.ClickEvent;
                //
                CodePropertyReferenceExpression cpreEvent = new CodePropertyReferenceExpression(cvreES, EVENT);
                CodeAssignStatement casEvent = new CodeAssignStatement(cpreEvent,
                                                                       GetEvent(xamlClrEventNode.EventMember,
                                                                                xamlClrEventNode.EventName,
                                                                                xamlClrEventNode.Value));

                // eventSetter.Handler = new RoutedEventHandler(OnClick);
                //
                CodePropertyReferenceExpression cpreHandler = new CodePropertyReferenceExpression(cvreES, HANDLER);
                CodeAssignStatement casHandler = new CodeAssignStatement(cpreHandler,
                                                                         GetEventDelegate(null,
                                                                                          xamlClrEventNode.EventMember,
                                                                                          xamlClrEventNode.EventName,
                                                                                          xamlClrEventNode.Value));

                AddLinePragma(casHandler, xamlClrEventNode.LineNumber);

                // ((Style)target).Setters.Add(eventSetter);
                //
                CodeCastExpression cceTarget = new CodeCastExpression(KnownTypes.Types[(int)KnownElements.Style], careTarget);
                CodePropertyReferenceExpression cpreSetters = new CodePropertyReferenceExpression(cceTarget, SETTERS);
                CodeMethodInvokeExpression cmieAdd = new CodeMethodInvokeExpression(cpreSetters, ADD, cvreES);

                if (SwitchStatementSupported())
                {
                    _ccRoot.StyleConnectorFn.Statements.Add(casES);
                    _ccRoot.StyleConnectorFn.Statements.Add(casEvent);
                    _ccRoot.StyleConnectorFn.Statements.Add(casHandler);
                    _ccRoot.StyleConnectorFn.Statements.Add(new CodeExpressionStatement(cmieAdd));
                }
                else
                {
                    ccsConnector.TrueStatements.Add(casES);
                    ccsConnector.TrueStatements.Add(casEvent);
                    ccsConnector.TrueStatements.Add(casHandler);
                    ccsConnector.TrueStatements.Add(new CodeExpressionStatement(cmieAdd));
                    // Only add if statement at start of new scope
                    if (!xamlClrEventNode.IsSameScope)
                    {
                        _ccRoot.StyleConnectorFn.Statements.Add(ccsConnector);
                    }
                }
            }
            else
            {

                //
                // ((Foo)target).Bar += new BarEventHandler(OnBar);
                //
                // *or*
                //
                // ((Foo)target).AddHandler( Baz.BarEvent, new BarEventHandler(OnBar));
                //

                CodeCastExpression cceTarget;
                Type eventTarget;


                // Create the markup event information

                MarkupEventInfo mei = new MarkupEventInfo( xamlClrEventNode.Value,          // Event handler string
                                                           xamlClrEventNode.EventName,      // Event name string
                                                           xamlClrEventNode.EventMember,    // MemberInfo
                                                           xamlClrEventNode.LineNumber);    // LineNumber


                // Get the type that defines the event (e.g. typeof(Button) for Button.Clicked or typeof(Mouse) for Mouse.MouseMove)

                eventTarget = xamlClrEventNode.ListenerType;


                // Create the type cast expression "(Foo)target"

                cceTarget = new CodeCastExpression( eventTarget, careTarget);


                // Create the whole code statement (either in += form or in AddHandler form)

                CodeStatement csAddCLREvent = AddCLREvent( eventTarget, null, cceTarget, mei );

                if (SwitchStatementSupported())
                {
                    _ccRoot.StyleConnectorFn.Statements.Add( csAddCLREvent );
                }
                else
                {
                    ccsConnector.TrueStatements.Add( csAddCLREvent );

                    // Only add if statement at start of new scope
                    if (!xamlClrEventNode.IsSameScope)
                    {
                        _ccRoot.StyleConnectorFn.Statements.Add(ccsConnector);
                    }
                }
            }
        }

        private void EndStyleEventConnection()
        {
            if (_ccRoot.StyleConnectorFn != null)
            {
                _ccRoot.CodeClass.BaseTypes.Add(KnownTypes.Types[(int)KnownElements.IStyleConnector].FullName);

                if (SwitchStatementSupported())
                {
                    // break any last case staement as we are done with style event connections.
                    if (_ccRoot.StyleConnectorFn.Statements.Count > 1)
                    {
                        CodeSnippetStatement cssBreak = new CodeSnippetStatement(BREAK_STATEMENT);
                        _ccRoot.StyleConnectorFn.Statements.Add(cssBreak);
                    }

                    // switch (connectionId)
                    // {
                    // } -- End Switch
                    CodeSnippetStatement css = new CodeSnippetStatement(INDENT12 + ENDCURLY);
                    _ccRoot.StyleConnectorFn.Statements.Add(css);
                }

                _ccRoot.CodeClass.Members.Add(_ccRoot.StyleConnectorFn);
                _ccRoot.StyleConnectorFn = null;
            }
        }

        private void EnsureHookupFn()
        {
            // void IComponentConnector.Connect
            //
            if (_ccRoot.HookupFn == null)
            {
                _ccRoot.HookupFn = new CodeMemberMethod();
                _ccRoot.HookupFn.Name = CONNECT;
                _ccRoot.HookupFn.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                _ccRoot.HookupFn.PrivateImplementationType = new CodeTypeReference(KnownTypes.Types[(int)KnownElements.IComponentConnector]);

                // void IComponentConnector.Connect(int connectionId, object target) {
                //
                CodeParameterDeclarationExpression param1 = new CodeParameterDeclarationExpression(typeof(int), CONNECTIONID);
                CodeParameterDeclarationExpression param2 = new CodeParameterDeclarationExpression(typeof(object), TARGET);
                _ccRoot.HookupFn.Parameters.Add(param1);
                _ccRoot.HookupFn.Parameters.Add(param2);

                AddDebuggerNonUserCodeAttribute(_ccRoot.HookupFn);
                AddGeneratedCodeAttribute(_ccRoot.HookupFn);
                AddEditorBrowsableAttribute(_ccRoot.HookupFn);
                AddSuppressMessageAttribute(_ccRoot.HookupFn, "Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes");
                AddSuppressMessageAttribute(_ccRoot.HookupFn, "Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity");
                AddSuppressMessageAttribute(_ccRoot.HookupFn, "Microsoft.Performance", "CA1800:DoNotCastUnnecessarily");

                if (SwitchStatementSupported())
                {
                    // switch (connectionId) -- Start Switch
                    // {
                    CodeSnippetStatement css = new CodeSnippetStatement(SWITCH_STATEMENT);
                    _ccRoot.HookupFn.Statements.Add(css);
                }
            }
        }

        internal void ConnectNameAndEvents(string elementName, ArrayList events, int connectionId)
        {
            CodeContext cc = (CodeContext)_codeContexts.Peek();
            bool isAllowedNameScope = cc.IsAllowedNameScope;

            if (_codeContexts.Count > 1 && KnownTypes.Types[(int)KnownElements.INameScope].IsAssignableFrom(cc.ElementType))
            {
                cc.IsAllowedNameScope = false;
            }

            if ((elementName == null || !isAllowedNameScope) && (events == null || events.Count == 0))
            {
                return;
            }

            EnsureHookupFn();

            CodeConditionStatement ccsConnector = null;

            if (SwitchStatementSupported())
            {
                // case 1:
                //
                CodeSnippetStatement cssCase = new CodeSnippetStatement(CASE_STATEMENT + connectionId + COLON);
                _ccRoot.HookupFn.Statements.Add(cssCase);
            }
            else
            {
                // if (connectionId == 1)
                //
                ccsConnector = new CodeConditionStatement();
                ccsConnector.Condition = new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression(CONNECTIONID),
                                                                          CodeBinaryOperatorType.ValueEquality,
                                                                          new CodePrimitiveExpression(connectionId));
            }


            // (System.Windows.Controls.Footype)target;
            CodeArgumentReferenceExpression careTarget = new CodeArgumentReferenceExpression(TARGET);
            CodeCastExpression cceTarget = new CodeCastExpression(cc.ElementTypeReference, careTarget);
            CodeExpression ceEvent = cceTarget;

            // Names in nested Name scopes not be hooked up via ICC.Connect() as no fields are generated in this case.
            if (elementName != null && isAllowedNameScope)
            {
                // this.fooId = (System.Windows.Controls.Footype)target;
                //
                ceEvent = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), elementName);
                CodeAssignStatement casName = new CodeAssignStatement(ceEvent, cceTarget);
                if (SwitchStatementSupported())
                {
                    _ccRoot.HookupFn.Statements.Add(casName);
                }
                else
                {
                    ccsConnector.TrueStatements.Add(casName);
                }
            }

            if (events != null)
            {
                foreach (MarkupEventInfo mei in events)
                {
                    CodeStatement csEvent = AddCLREvent(cc, ceEvent, mei);

                    if (SwitchStatementSupported())
                    {
                        _ccRoot.HookupFn.Statements.Add(csEvent);
                    }
                    else
                    {
                        ccsConnector.TrueStatements.Add(csEvent);
                    }
                }
            }

            // return;
            //
            if (SwitchStatementSupported())
            {
                _ccRoot.HookupFn.Statements.Add(new CodeMethodReturnStatement());
            }
            else
            {
                ccsConnector.TrueStatements.Add(new CodeMethodReturnStatement());
                _ccRoot.HookupFn.Statements.Add(ccsConnector);
            }
        }

        // called from ParserExtension.WriteEndAttributes at the end of an element
        // that has x:TypeArguments, to clear the state used to support them.
        internal void ClearGenericTypeArgs()
        {
            _typeArgsList = null;
        }

        private void EndHookups()
        {
            if (_ccRoot.HookupFn != null)
            {
                var iComponentConnector = new CodeTypeReference(KnownTypes.Types[(int)KnownElements.IComponentConnector]);
                _ccRoot.CodeClass.BaseTypes.Add(iComponentConnector);

                // Visual Basic requires InitializeComponent to explicitly implement IComponentConnector.InitializeComponent
                // if the class implements the interface. GenerateInitializeComponent handles the cases where the class is not
                // the entry point for any programming language.
                if (IsLanguageVB && IsCompilingEntryPointClass)
                {
                    _ccRoot.InitializeComponentFn.ImplementationTypes.Add(iComponentConnector);
                }

                if (SwitchStatementSupported())
                {
                    // Don't generate an empty Switch block!
                    if (_ccRoot.HookupFn.Statements.Count == 1 &&
                        _ccRoot.HookupFn.Statements[0] is CodeSnippetStatement)
                    {
                        _ccRoot.HookupFn.Statements.Clear();
                    }
                    else
                    {
                        // switch (connectionId)
                        // {
                        // } -- End Switch
                        CodeSnippetStatement css = new CodeSnippetStatement(INDENT12 + ENDCURLY);
                        _ccRoot.HookupFn.Statements.Add(css);
                    }
                }

                // _contentLoaded = true;
                //
                CodeFieldReferenceExpression cfreContentLoaded = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), CONTENT_LOADED);
                CodeAssignStatement casContentLoaded = new CodeAssignStatement(cfreContentLoaded, new CodePrimitiveExpression(true));
                _ccRoot.HookupFn.Statements.Add(casContentLoaded);

                _ccRoot.CodeClass.Members.Add(_ccRoot.HookupFn);
                _ccRoot.HookupFn = null;
            }
        }

        internal void GenerateBamlFile(MemoryStream bamlMemStream)
        {
            // write baml file only if we're not doing intellisense build
            if ((IsBamlNeeded) && (TaskFileService.IsRealBuild))
            {
                string filepath = TargetPath + SourceFileInfo.RelativeSourceFilePath + BAML;
                using (FileStream bamlFileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    bamlMemStream.WriteTo(bamlFileStream);
                }
            }
        }

#endregion Baml Hookup Functions

#region Helpers

        private void InitializeReflectionHelper()
        {
            var paths = new List<string>(ReferenceAssemblyList?.Count ?? 0);
            if (ReferenceAssemblyList != null && ReferenceAssemblyList.Count > 0)
            {
                for (int i = 0; i < ReferenceAssemblyList.Count; i++)
                {
                    ReferenceAssembly refasm = ReferenceAssemblyList[i] as ReferenceAssembly;
                    if (refasm != null && refasm.Path.Length > 0)
                    {
                        paths.Add(refasm.Path);
                    }
                }
            }
            ReflectionHelper.Initialize(paths);
        }
        private void InitializeTypeMapper()
        {
            _typeMapper = XamlTypeMapper.DefaultMapper;
            ReflectionHelper.LocalAssemblyName = AssemblyName;

            if (ReferenceAssemblyList != null && ReferenceAssemblyList.Count > 0)
            {
                for (int i = 0; i < ReferenceAssemblyList.Count; i++)
                {
                    ReferenceAssembly refasm = ReferenceAssemblyList[i] as ReferenceAssembly;

                    if (refasm != null && refasm.Path.Length > 0)
                    {
                        _typeMapper.SetAssemblyPath(refasm.AssemblyName, refasm.Path);
                    }
                }
            }

            string asmMissing = string.Empty;
            if (XamlTypeMapper.AssemblyWB == null)
            {
                asmMissing = "WindowsBase";
            }
            if (XamlTypeMapper.AssemblyPC == null)
            {
                asmMissing += (asmMissing.Length > 0 ? ", " : string.Empty) + "PresentationCore";
            }
            if (XamlTypeMapper.AssemblyPF == null)
            {
                asmMissing += (asmMissing.Length > 0 ? ", " : string.Empty) + "PresentationFramework";
            }

            if (asmMissing.Length > 0)
            {
                string message = SR.Get(SRID.WinFXAssemblyMissing, asmMissing);
                ApplicationException aeAssemblyMissing = new ApplicationException(message);
                throw aeAssemblyMissing;
            }

            KnownTypes.InitializeKnownTypes(XamlTypeMapper.AssemblyPF, XamlTypeMapper.AssemblyPC, XamlTypeMapper.AssemblyWB);
            _typeMapper.InitializeReferenceXmlnsCache();
        }

        private bool SwitchStatementSupported()
        {
            return (IsLanguageCSharp || (CompilerInfo != null && (string.Compare(CompilerInfo.GetLanguages()[0], JSCRIPT, StringComparison.OrdinalIgnoreCase) == 0)));
        }

        private bool IsInternalAccessSupported
        {
            get
            {
                return (CompilerInfo == null || (string.Compare(CompilerInfo.GetLanguages()[0], JSHARP, StringComparison.OrdinalIgnoreCase) != 0));
            }
        }

        private bool IsLanguageCaseSensitive()
        {
            CodeDomProvider cdp = EnsureCodeProvider();
            return cdp.LanguageOptions != LanguageOptions.CaseInsensitive;
        }

        private bool IsLanguageCSharp
        {
            get { return _isLangCSharp; }
        }

        private bool IsLanguageVB
        {
            get { return _isLangVB; }
        }

        // Combine namespace and className
        private string GetFullClassName(string ns, string className)
        {
            string fullClass = className;

            if (ns != null && ns.Length > 0)
            {
                fullClass = ns + DOT + className;
            }

            return fullClass;
        }

        internal void ValidateFullSubClassName(ref string subClassFullName)
        {
            bool isValid = false;
            int index = subClassFullName.LastIndexOf(DOTCHAR);

            if (index > 0)
            {
                string subClassName = subClassFullName.Substring(index + 1);
                isValid = IsValidCLRNamespace(subClassFullName.Substring(0, index), false) &&
                          IsValidClassName(subClassName);
            }
            else
            {
                isValid = IsValidClassName(subClassFullName);
            }

            if (!isValid)
            {
                // flag error. Can't throw here as we are pre-scanning and parser context doesn't
                // have customized linenum\linepos yet.
                subClassFullName = DOT;
            }
        }

        private bool CrackClassName(ref string className, out string ns)
        {
            bool isValid = true;
            ns = string.Empty;

            if (className.Length > 0)
            {
                // Split the Namespace
                int index = className.LastIndexOf(DOTCHAR);

                if (index > 0)
                {
                    ns = className.Substring(0, index);
                    className = className.Substring(index + 1);
                    isValid = IsValidCLRNamespace(ns, false);
                }

                isValid = isValid && IsValidClassName(className);
            }

            return isValid;
        }

        internal string GetGenericTypeName(string typeName, string typeArgs)
        {
            if (typeArgs.Length == 0)
            {
                ThrowCompilerException(SRID.UnknownGenericType,
                                       DefinitionNSPrefix,
                                       typeArgs,
                                       typeName);
            }

            StringBuilder sb = new StringBuilder(typeName, 20);
            sb.Append(GENERIC_DELIMITER);

            _typeArgsList = typeArgs.Split(new Char[] { COMMA });

            sb.Append(_typeArgsList.Length);

            return sb.ToString();
        }

        private static void AddEditorBrowsableAttribute(CodeTypeMember ctmTarget)
        {
            CodeFieldReferenceExpression cfre = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EditorBrowsableState)), "Never");
            CodeAttributeArgument caa = new CodeAttributeArgument(cfre);
            ctmTarget.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(EditorBrowsableAttribute).FullName, caa));
        }

        private static void AddSuppressMessageAttribute(CodeTypeMember ctmTarget, string category, string rule)
        {
            CodeAttributeDeclaration cad = new CodeAttributeDeclaration(
                         new CodeTypeReference(typeof(SuppressMessageAttribute)),
                         new CodeAttributeArgument(new CodePrimitiveExpression(category)),
                         new CodeAttributeArgument(new CodePrimitiveExpression(rule)));

            ctmTarget.CustomAttributes.Add(cad);
        }

        private static void AddDebuggerNonUserCodeAttribute(CodeTypeMember ctmTarget)
        {
            CodeAttributeDeclaration cad = new CodeAttributeDeclaration(
                         new CodeTypeReference(typeof(DebuggerNonUserCodeAttribute)));

            ctmTarget.CustomAttributes.Add(cad);
        }

        internal static void GenerateXmlComments(CodeTypeMember ctm, string comment)
        {
            // generate xml comments

            // /// <summary>
            // /// </summary>
            CodeCommentStatement ccs = new CodeCommentStatement(SummaryStartTag, true);
            ctm.Comments.Add(ccs);
            ccs = new CodeCommentStatement(comment, true);
            ctm.Comments.Add(ccs);
            ccs = new CodeCommentStatement(SummaryEndTag, true);
            ctm.Comments.Add(ccs);
        }

        private bool IsValidClassName(string className)
        {
            if (className.Length == 0 ||!NameValidationHelper.IsValidIdentifierName(className))
            {
                return false;
            }

            return true;
        }

        private bool IsValidCLRNamespace(string ns, bool shouldThrow)
        {
            if (ns.Length > 0)
            {
                string[] nsParts = ns.Split(new char[] { DOTCHAR });

                foreach (string nsPart in nsParts)
                {
                    if (!NameValidationHelper.IsValidIdentifierName(nsPart.Trim()))
                    {
                        if (shouldThrow)
                        {
                            ThrowCompilerException(SRID.InvalidDefaultCLRNamespace, nsPart, ns);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal void ValidateEventHandlerName(string eventName, string handlerName)
        {
            if (!IsCodeNeeded)
            {
                ThrowCompilerException(SRID.MissingClassDefinitionForEvent, _ccRoot.ElementName, DefinitionNSPrefix, eventName);
            }

            string handler = handlerName.Trim();
            if (handler.Length == 0)
            {
                ThrowCompilerException(SRID.EmptyEventStringNotAllowed, eventName, handlerName);
            }
            else if (!NameValidationHelper.IsValidIdentifierName(handler))
            {
                ThrowCompilerException(SRID.InvalidEventHandlerName, eventName, handlerName);
            }
        }

        private string ParentFolderPrefix
        {
            get
            {
                if (SupportCustomOutputPaths)
                {
                    //  During code generation, ParentFolderPrefix returns the relative path from a .g.cs file to its markup file.
                    //
                    //      One example is generated #pragmas: #pragma checksum "..\..\..\..\Views\ExportNotificationView.xaml"  
                    //
                    //  The path information for a markup file is represented in SourceFileInfo: 
                    //
                    //      SourceFileInfo.OriginalFilePath: "c:\\greenshot\\src\\Greenshot.Addons\\Views\\ExportNotificationView.xaml"
                    //      SourceFileInfo.TargetPath: "c:\\greenshot\\src\\Greenshot.Addons\\obj\\Debug\\net6.0-windows\\"
                    //      SourceFileInfo.RelativeFilePath: "Views\\ExportNotificationView"
                    //      SourceFileInfo.SourcePath = "c:\\greenshot\\src\\Greenshot.Addons\\"
                    //
                    //  The path of the generated code file associated with this markup file is:
                    //
                    //      "c:\greenshot\src\Greenshot.Addons\obj\Debug\net6.0-windows\Views\ExportNotificationView.g.cs"
                    //
                    //  The markup file path is in SourceFileInfo.OriginalFilePath:
                    //
                    //      "c:\\greenshot\\src\\Greenshot.Addons\\Views\\ExportNotificationView.xaml"
                    //
                    //  The relative path calculation must take in to account both the TargetPath and the RelativeFilePath:
                    //
                    //      "c:\\greenshot\\src\\Greenshot.Addons\\obj\\Debug\\net6.0-windows\\" [SourceFileInfo.TargetPath]      
                    //      "Views\\ExportNotificationView" [SourceFileInfo.RelativeTargetPath]
                    //
                    //   TargetPath concatenated with the directory portion of the RelativeTargetPath is the location to the .g.cs file:
                    //
                    //      "c:\\greenshot\\src\\Greenshot.Addons\\obj\\Debug\\net6.0-windows\\Views"
                    //      
                    string pathOfRelativeSourceFilePath = System.IO.Path.GetDirectoryName(SourceFileInfo.RelativeSourceFilePath);

                    // Return the parent folder of the target file with a trailing DirectorySeparatorChar.  
                    // Return a relative path if possible.  Else, return an absolute path.
                    #if NETFX 
                    string path = PathInternal.GetRelativePath(TargetPath + pathOfRelativeSourceFilePath, SourceFileInfo.SourcePath, StringComparison.OrdinalIgnoreCase);
#else
                    string path = Path.GetRelativePath(TargetPath + pathOfRelativeSourceFilePath, SourceFileInfo.SourcePath);
#endif
                    // Always return a path with a trailing DirectorySeparatorChar.  
                    return path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                }
                else
                {
                    string parentFolderPrefix = string.Empty;
                    if (TargetPath.StartsWith(SourceFileInfo.SourcePath, StringComparison.OrdinalIgnoreCase))
                    {
                        string relPath = TargetPath.Substring(SourceFileInfo.SourcePath.Length);
                        relPath += SourceFileInfo.RelativeSourceFilePath;
                        string[] dirs = relPath.Split(new Char[] { Path.DirectorySeparatorChar });
                        for (int i = 1; i < dirs.Length; i++)
                        {
                            parentFolderPrefix += PARENTFOLDER;
                        }
                    }

                    return parentFolderPrefix;
                } 
            }
        }

        private void AddLinePragma(CodeTypeMember ctm, int lineNumber)
        {
            CodeLinePragma clp = new CodeLinePragma(ParentFolderPrefix + SourceFileInfo.RelativeSourceFilePath + XAML, lineNumber);
            ctm.LinePragma = clp;
        }

        private void AddLinePragma(CodeStatement cs, int lineNumber)
        {
            CodeLinePragma clp = new CodeLinePragma(ParentFolderPrefix + SourceFileInfo.RelativeSourceFilePath + XAML, lineNumber);
            cs.LinePragma = clp;
        }

        internal MemberAttributes GetMemberAttributes(string modifier)
        {
            if (!IsCodeNeeded)
            {
                ThrowCompilerException(SRID.MissingClassWithFieldModifier, DefinitionNSPrefix);
            }

            if (_private.Length == 0)
            {
                bool converted = false;
                CodeDomProvider codeProvider = EnsureCodeProvider();
                TypeConverter converter = codeProvider.GetConverter(typeof(MemberAttributes));
                if (converter != null)
                {
                    if (converter.CanConvertTo(typeof(string)))
                    {
                        try
                        {
                            _private = converter.ConvertToInvariantString(MemberAttributes.Private).ToLowerInvariant();
                            _public = converter.ConvertToInvariantString(MemberAttributes.Public).ToLowerInvariant();
                            _protected = converter.ConvertToInvariantString(MemberAttributes.Family).ToLowerInvariant();
                            _internal = converter.ConvertToInvariantString(MemberAttributes.Assembly).ToLowerInvariant();
                            _protectedInternal = converter.ConvertToInvariantString(MemberAttributes.FamilyOrAssembly).ToLowerInvariant();
                            converted = true;
                        }
                        catch (NotSupportedException)
                        {
                        }
                    }
                }

                if (!converted)
                {
                    ThrowCompilerException(SRID.UnknownFieldModifier, MarkupCompiler.DefinitionNSPrefix, modifier, _language);
                }
            }

            string normalizedModifier = modifier;
            if (!IsLanguageCaseSensitive())
            {
                normalizedModifier = modifier.ToLowerInvariant();
            }

            if (normalizedModifier.Equals(_private))
            {
                return MemberAttributes.Private;
            }
            else if (normalizedModifier.Equals(_public))
            {
                return MemberAttributes.Public;
            }
            else if (normalizedModifier.Equals(_protected))
            {
                return MemberAttributes.Family;
            }
            else if (normalizedModifier.Equals(_internal))
            {
                return MemberAttributes.Assembly;
            }
            else if (normalizedModifier.Equals(_protectedInternal))
            {
                return MemberAttributes.FamilyOrAssembly;
            }
            else
            {
                ThrowCompilerException(SRID.UnknownFieldModifier, MarkupCompiler.DefinitionNSPrefix, modifier, _language);
            }

            return MemberAttributes.Assembly;
        }

        private TypeAttributes GetTypeAttributes(ref string modifier)
        {
            if (modifier.Length > 0)
            {
                if (_privateClass.Length == 0)
                {
                    bool converted = false;
                    CodeDomProvider codeProvider = EnsureCodeProvider();
                    TypeConverter converter = codeProvider.GetConverter(typeof(TypeAttributes));
                    if (converter != null)
                    {
                        if (converter.CanConvertTo(typeof(string)))
                        {
                            try
                            {
                                _privateClass = converter.ConvertToInvariantString(TypeAttributes.NotPublic).ToLowerInvariant();
                                _publicClass = converter.ConvertToInvariantString(TypeAttributes.Public).ToLowerInvariant();
                                converted = true;
                            }
                            catch (NotSupportedException)
                            {
                            }
                        }
                    }

                    if (!converted)
                    {
                        ThrowCompilerException(SRID.UnknownClassModifier, MarkupCompiler.DefinitionNSPrefix, modifier, _language);
                    }
                }

                string normalizedModifier = modifier;
                if (!IsLanguageCaseSensitive())
                {
                    normalizedModifier = modifier.ToLowerInvariant();
                }

                if (normalizedModifier.Equals(_privateClass))
                {
                    return TypeAttributes.NotPublic;
                }
                else if (normalizedModifier.Equals(_publicClass))
                {
                    return TypeAttributes.Public;
                }
                else
                {
                    // flag error. Can't throw here as we are pre-scanning and parser context doesn't
                    // have customized linenum\linepos yet.
                    modifier = DOT;
                }
            }

            return TypeAttributes.Public;
        }

        internal void CheckForNestedNameScope()
        {
            CodeContext cc = (CodeContext)_codeContexts.Peek();
            if (_codeContexts.Count > 1 && KnownTypes.Types[(int)KnownElements.INameScope].IsAssignableFrom(cc.ElementType))
            {
                cc.IsAllowedNameScope = false;
            }
        }

#endregion Helpers

#region Property

        private CodeExpression GetPropertyValueExpression(ITypeDescriptorContext ctx, Type typeToConvertTo, Object value, string attributeValue)
        {
            CodeExpression ce = null;
            InstanceDescriptor desc = null;
            TypeConverter converter = null;

            if (value != null && (typeToConvertTo == typeof(String) || typeToConvertTo.IsPrimitive))
            {
                ce = new CodePrimitiveExpression(value);
            }
            else if (typeToConvertTo == typeof(Uri))
            {
                converter = new UriTypeConverter();
                if (!UriParser.IsKnownScheme(URISCHEME_PACK))
                {
                    UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), URISCHEME_PACK, -1);
                }
            }
            else if (typeToConvertTo.IsEnum)
            {
                converter = new EnumConverter(typeToConvertTo);
            }

            if (converter != null)
            {
                if (value == null)
                {
                    if (attributeValue != null)
                    {
                        value = converter.ConvertFromString(ctx, TypeConverterHelper.InvariantEnglishUS, attributeValue);

                        if (value == null)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        ce = new CodePrimitiveExpression(null);
                        return ce;
                    }
                }

                if (converter.CanConvertTo(ctx, typeof(InstanceDescriptor)))
                {
                    desc = (InstanceDescriptor)converter.ConvertTo(ctx, TypeConverterHelper.InvariantEnglishUS, value, typeof(InstanceDescriptor));

                    Debug.Assert(desc != null);

                    // static field ref...
                    if (desc.MemberInfo is FieldInfo || desc.MemberInfo is PropertyInfo)
                    {
                        CodeFieldReferenceExpression cfre = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(desc.MemberInfo.DeclaringType.FullName), desc.MemberInfo.Name);
                        ce = cfre;
                    }
                    else  // static method invoke
                    {
                        object[] args = new object[desc.Arguments.Count];
                        desc.Arguments.CopyTo(args, 0);
                        CodeExpression[] expressions = new CodeExpression[args.Length];

                        if (desc.MemberInfo is MethodInfo)
                        {
                            MethodInfo mi = (MethodInfo)desc.MemberInfo;
                            ParameterInfo[] parameters = mi.GetParameters();

                            for (int i = 0; i < args.Length; i++)
                            {
                                expressions[i] = GetPropertyValueExpression(ctx, parameters[i].ParameterType, args[i], null);
                            }

                            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(desc.MemberInfo.DeclaringType.FullName), desc.MemberInfo.Name);
                            foreach (CodeExpression e in expressions)
                            {
                                cmie.Parameters.Add(e);
                            }

                            ce = cmie;
                        }
                        else if (desc.MemberInfo is ConstructorInfo)  // instance ctor invoke
                        {
                            ConstructorInfo ci = (ConstructorInfo)desc.MemberInfo;
                            ParameterInfo[] parameters = ci.GetParameters();

                            for (int i = 0; i < args.Length; i++)
                            {
                                expressions[i] = GetPropertyValueExpression(ctx, parameters[i].ParameterType, args[i], null);
                            }

                            CodeObjectCreateExpression coce = new CodeObjectCreateExpression(desc.MemberInfo.DeclaringType.FullName);
                            foreach (CodeExpression e in expressions)
                            {
                                coce.Parameters.Add(e);
                            }

                            ce = coce;
                        }
                    }
                }
            }

            return ce;
        }

#endregion Property

#region Event

        // The given MemberInfo could either be an EventInfo for a Clr event or a
        // MethodInfo for a static Add{EventName}Handler helper for an attached event
        private Type GetEventHandlerType(MemberInfo memberInfo)
        {
            Type eventHandlerType = null;
            if (memberInfo is EventInfo)
            {
                EventInfo ei = (EventInfo)memberInfo;
                eventHandlerType = ei.EventHandlerType;
            }
            else
            {
                MethodInfo mi = (MethodInfo)memberInfo;
                ParameterInfo[] pis = mi.GetParameters();
                Debug.Assert(pis != null && pis.Length == 2 && KnownTypes.Types[(int)KnownElements.DependencyObject].IsAssignableFrom(pis[0].ParameterType));
                eventHandlerType = pis[1].ParameterType;
            }

            return eventHandlerType;
        }

        private CodeFieldReferenceExpression GetEvent(MemberInfo miEvent, string eventName, string eventHandler)
        {
            FieldInfo fiEvent = miEvent.DeclaringType.GetField(eventName + EVENT, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fiEvent == null || fiEvent.FieldType != KnownTypes.Types[(int)KnownElements.RoutedEvent])
            {
                ThrowCompilerException(SRID.RoutedEventNotRegistered, miEvent.DeclaringType.FullName, eventName, eventHandler);
            }

            CodeTypeReferenceExpression ctreEvent = new CodeTypeReferenceExpression(miEvent.DeclaringType.FullName);
            CodeFieldReferenceExpression cfreEvent = new CodeFieldReferenceExpression(ctreEvent, fiEvent.Name);
            return cfreEvent;
        }


        private CodeExpression GetEventDelegate(CodeContext cc, MemberInfo miEvent, string eventName, string eventHandler)
        {
            Type eventTarget = cc != null ? cc.ElementType : miEvent.DeclaringType;
            string eventTargetName = eventTarget != null ? eventTarget.FullName : cc.LocalElementFullName;

            bool subClassed = _ccRoot.SubClass.Length > 0;
            CodeDelegateCreateExpression cdce = new CodeDelegateCreateExpression();

            // Fetch the EventHandlerType from either the EventInfo or the MethodInfo
            // for the Add{Propertyname}Handler method's MethodInfo
            Type eventHandlerType = GetEventHandlerType(miEvent);
            string [] typeArgsList = cc != null ? cc.GenericTypeArgs : null;

            cdce.DelegateType = GenerateConstructedTypeReference(eventHandlerType, typeArgsList, eventTarget, eventTargetName, eventName);
            cdce.MethodName = eventHandler.Trim() + (subClassed ? HELPER : string.Empty);
            cdce.TargetObject = new CodeThisReferenceExpression();
            CodeExpression cDelExp = cdce;

            // NOTE: workaround for VB CodeDom bug which does not produce correct Delegate expression code
            if (IsLanguageVB)
            {
                CodeExpression[] delParams = { cdce };
                CodeObjectCreateExpression coce = new CodeObjectCreateExpression(eventHandlerType, delParams);
                cDelExp = coce;
            }

            		
//            The bug that this chunk of code works around was fixed but
//            exposes a different bug. To work around the second bug, we
//            remove the workaround for the first one.  
//            Note that the initial bug was not fixed for VB, so the code block above remains.
//            else if (Language == CompilerLanguage.JScript)
//            {
//                CodeCastExpression cce = new CodeCastExpression(mei.ei.EventHandlerType, cdce);
//                cDelExp = cce;
//            }

            return cDelExp;
        }


        private CodeStatement AddCLREvent(CodeContext cc, CodeExpression ce, MarkupEventInfo mei)
        {
            // Infer the event target's (aka the listener) type from the current code context
            return AddCLREvent( cc.ElementType, cc, ce, mei );
        }

        private CodeStatement AddCLREvent(Type eventTarget, CodeContext cc, CodeExpression ce, MarkupEventInfo mei)
        {

            bool subClassed = _ccRoot.SubClass.Length > 0;
            CodeStatement csEvent = null;
            // param2: <FooEventHandler>
            CodeExpression cDelExp = GetEventDelegate(cc, mei.mi, mei.eventName, mei.eventHandler);

            if (mei.mi.DeclaringType.IsAssignableFrom(eventTarget))
            {
                // _element.FooEvent += new FooEventHandlerDelegate(OnFoo);
                csEvent = new CodeAttachEventStatement(ce, mei.eventName, cDelExp);
            }
            else if (eventTarget == null || // for known attached events on unknown local tags
                     KnownTypes.Types[(int)KnownElements.UIElement].IsAssignableFrom(eventTarget) ||
                     KnownTypes.Types[(int)KnownElements.ContentElement].IsAssignableFrom(eventTarget))
            {
                // _element.AddHandler(FooEvent, new FooEventHandlerDelegate(OnFoo));
                CodeFieldReferenceExpression cfreEvent = GetEvent(mei.mi, mei.eventName, mei.eventHandler);
                CodeMethodInvokeExpression cmieAddHandler = new CodeMethodInvokeExpression(ce, ADDHANDLER, cfreEvent, cDelExp);
                csEvent = new CodeExpressionStatement(cmieAddHandler);
            }
            else
            {
                string eventTargetName = eventTarget != null ? eventTarget.FullName : cc.LocalElementFullName;
                ThrowCompilerException(SRID.UnknownEventAttribute, mei.eventName, mei.eventHandler, eventTargetName);
            }

            // When x:SubClass is used, event handlers can be specified in a code-behind file, under this sub class.
            // But these handler methods need to be accessible from the intermediary generated sub class. So an empty
            // internal virtual method with the same signature as the handler method is generated in this intermediary
            // sub class (in the generated file):
            //
            //      internal virtual void OnFooEvent(object sender, FooEventArgs ea)
            //      {
            //      }
            //
            // Since a delegate cannot take the address of a virtual function, a non-virtual helper function
            // with the same signature as the above function & which calls the above function is also generated:
            //
            //      private void OnFooEventHelper(object sender, FooEventArgs ea)
            //      {
            //          OnFooEvent(sender, ea);
            //      }
            //
            // All this is done only if x:Subclass is specified, since this means that this sub class would need to be
            // defined in a code-behind file. This also means that inline events (in <x:Code>) will not be supported.

            if (subClassed)
            {
                GenerateProtectedEventHandlerMethod(mei);
            }

            AddLinePragma(csEvent, mei.lineNumber);
            return csEvent;
        }

        private void GenerateProtectedEventHandlerMethod(MarkupEventInfo mei)
        {
            Debug.Assert(_ccRoot != null && _ccRoot.SubClass.Length > 0);

            // Fetch the EventHandlerType from either the EventInfo or the MethodInfo
            // for the Add{Propertyname}Handler method's MethodInfo
            Type eventHandlerType = GetEventHandlerType(mei.mi);

            MethodInfo methodInvoke = eventHandlerType.GetMethod("Invoke");
            ParameterInfo[] pars = methodInvoke.GetParameters();

            CodeMemberMethod cmmEventHandler = new CodeMemberMethod();
            CodeMemberMethod cmmEventHandlerHelper = new CodeMemberMethod();

            AddDebuggerNonUserCodeAttribute(cmmEventHandlerHelper);
            AddGeneratedCodeAttribute(cmmEventHandlerHelper);

            cmmEventHandler.Attributes = MemberAttributes.Assembly | MemberAttributes.Overloaded;
            cmmEventHandler.ReturnType = new CodeTypeReference(typeof(void));
            cmmEventHandler.Name = mei.eventHandler.Trim();

            CodeMethodInvokeExpression cmieOnEvent = new CodeMethodInvokeExpression(null, cmmEventHandler.Name);

            for (int i = 0; i < pars.Length; i++)
            {
                CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(pars[i].ParameterType, pars[i].Name);
                cmmEventHandler.Parameters.Add(param);
                cmmEventHandlerHelper.Parameters.Add(param);
                cmieOnEvent.Parameters.Add(new CodeArgumentReferenceExpression(pars[i].Name));
            }

            //
            // internal virtual void OnFooEvent(object sender, FooEventArgs ea)
            // {
            // }
            //
            _ccRoot.CodeClass.Members.Add(cmmEventHandler);

            cmmEventHandlerHelper.Name = cmmEventHandler.Name + HELPER;
            cmmEventHandlerHelper.ReturnType = new CodeTypeReference(typeof(void));
            cmmEventHandlerHelper.Statements.Add(new CodeExpressionStatement(cmieOnEvent));

            //
            // private void OnFooEventHelper(object sender, FooEventArgs ea)
            // {
            //     OnFooEvent(sender, ea);
            // }
            //
            _ccRoot.CodeClass.Members.Add(cmmEventHandlerHelper);
        }

        internal struct MarkupEventInfo
        {
            internal MarkupEventInfo(string eh, string en, MemberInfo mi, int ln)
            {
                eventHandler = eh;
                eventName = en;
                this.mi = mi;
                lineNumber = ln;
            }

            internal string eventHandler;
            internal string eventName;
            internal MemberInfo mi;
            internal int lineNumber;
        }

#endregion Event

#region Language

        private CodeDomProvider EnsureCodeProvider()
        {
            if (_codeProvider == null)
            {
                Debug.Assert(CompilerInfo != null && CompilerInfo.IsCodeDomProviderTypeValid);
                _codeProvider = CompilerInfo.CreateProvider();
            }

            return _codeProvider;
        }

        private bool IsLanguageSupported(string language)
        {
            _language = language;
            _isLangCSharp = string.Compare(language, CSHARP, StringComparison.OrdinalIgnoreCase) == 0;

            if (IsLanguageCSharp)
            {
                _codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
                return true;
            }
            else
            {
                _isLangVB = string.Compare(language, VB, StringComparison.OrdinalIgnoreCase) == 0;
                if (IsLanguageVB)
                {
                    _codeProvider = new Microsoft.VisualBasic.VBCodeProvider();
                    return true;
                }
            }

            if (CodeDomProvider.IsDefinedLanguage(language))
            {
                CompilerInfo = CodeDomProvider.GetCompilerInfo(language);
                return (CompilerInfo != null);
            }

            return false;
        }

        private void EnsureLanguageSourceExtension()
        {
            // If empty string is passed, use the default language source extension.
            if (String.IsNullOrEmpty(LanguageSourceExtension))
            {
                if (CompilerInfo != null)
                {
                    string[] listExtensions = CompilerInfo.GetExtensions();
                    LanguageSourceExtension = listExtensions[0];
                }
                else if (IsLanguageCSharp)
                {
                    LanguageSourceExtension = ".cs";
                }
                else if (IsLanguageVB)
                {
                    LanguageSourceExtension = ".vb";
                }
            }
        }

#endregion Language

#region CorePageGen

        internal CodeMemberField AddNameField(string name, int lineNumber, int linePosition)
        {
            CodeMemberField cmField = NameField(name, lineNumber, linePosition);
            if (cmField != null)
            {
                AddLinePragma(cmField, lineNumber);
            }
            return cmField;
        }

        internal CodeMemberField NameField(string name, int lineNumber, int linePosition)
        {
            // Warn for named ResourceDictionary items.
            if (_codeContexts.Count >= 2)
            {
                Type resourceDictionary = KnownTypes.Types[(int)KnownElements.ResourceDictionary];
                Type iNameScope = KnownTypes.Types[(int)KnownElements.INameScope];
                object[] contexts = _codeContexts.ToArray();
                for (int i = 1; i < contexts.Length; ++i)
                {
                    Type t = ((CodeContext)contexts[i]).ElementType;
                    if (iNameScope.IsAssignableFrom(t))
                    {
                        break;
                    }
                    if (resourceDictionary.IsAssignableFrom(t))
                    {
                        _taskLogger.LogWarningFromResources(
                            null,
                            null,
                            null,
                            SourceFileInfo.OriginalFilePath,
                            lineNumber,
                            linePosition,
                            0,
                            0,
                            SRID.NamedResDictItemWarning,
                            ((CodeContext)_codeContexts.Peek()).ElementType.FullName,
                            name
                        );
                        break;
                    }
                }
            }
            // Names in nested Name scopes should not have Name fields
            CodeContext cc = (CodeContext)_codeContexts.Peek();
            if (!cc.IsAllowedNameScope)
            {
                return null;
            }

            CodeMemberField field = new CodeMemberField();
            field.Name = name;
            field.Attributes = MemberAttributes.Assembly;
            field.Type = cc.ElementTypeReference;
            field.CustomAttributes.Add(
                new CodeAttributeDeclaration(
                         new CodeTypeReference("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"),
                         new CodeAttributeArgument(new CodePrimitiveExpression("Microsoft.Performance")),
                         new CodeAttributeArgument(new CodePrimitiveExpression("CA1823:AvoidUnusedPrivateFields"))));


            // Generate WithEvents ID fields in VB for objects supporting events
            field.UserData["WithEvents"] = true;

            _ccRoot.CodeClass.Members.Add(field);
            return field;
        }

        private void AddCodeSnippet(string codeText, int lineNum)
        {
            if (codeText == null || codeText.Trim().Length == 0)
                return;

            CodeSnippetTypeMember snippet = new CodeSnippetTypeMember();
            AddLinePragma(snippet, lineNum);
            snippet.Text = codeText;
            _ccRoot.CodeClass.Members.Add(snippet);
        }

        internal void AddGenericArguments(ParserContext parserContext, string typeArgs)
        {
            if (_typeArgsList != null)
            {
                string localTypeArgNamespace = string.Empty;
                string localTypeArgClassName = string.Empty;

                // for each generic param in this Type ...
                for (int i = 0; i < _typeArgsList.Length; i++)
                {
                    Type currTypeArg = parserContext.XamlTypeMapper.GetTypeArgsType(_typeArgsList[i].Trim(),
                                                                                    parserContext,
                                                                                    out localTypeArgClassName,
                                                                                    out localTypeArgNamespace);

                    if (currTypeArg == null)
                    {
                        bool error = false;
                        if (localTypeArgNamespace.Length == 0 && localTypeArgClassName.Length == 0)
                        {
                            error = true;
                        }
                        else
                        {
                            error = !IsValidClassName(localTypeArgClassName) ||
                                    !IsValidCLRNamespace(localTypeArgNamespace, false);
                        }

                        if (error)
                        {
                            ThrowCompilerException(SRID.InvalidTypeName,
                                                   MarkupCompiler.DefinitionNSPrefix,
                                                   typeArgs,
                                                   _typeArgsList[i].Trim(),
                                                   (i + 1).ToString(CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            _typeArgsList[i] = GetFullClassName(localTypeArgNamespace, localTypeArgClassName);
                        }
                    }
                    else
                    {
                        _typeArgsList[i] = currTypeArg.FullName;
                    }

                    // construct the type args list for the base class type to be generated
                    _ccRoot.CodeClass.BaseTypes[0].TypeArguments.Add(new CodeTypeReference(_typeArgsList[i]));
                }
            }
        }

        private static CodeTypeReference GenerateConstructedTypeReference(Type t, string [] typeArgsList, string genericName)
        {
            CodeTypeReference ctrConstructedType = null;

            // If the type has generic parameters, then need to add TypeArguments to the CodeTypeReference of this Type
            if (genericName.Length > 0 || t.IsGenericType)
            {
                Debug.Assert(genericName.Length > 0 || t.IsGenericTypeDefinition);

                if (t != null)
                {
                    Debug.Assert(genericName.Length == 0 && typeArgsList != null);

                    // NOTE: Remove when CodeDom is fixed to understand mangled generic names.
                    genericName = t.FullName;
                    int bang = genericName.IndexOf(GENERIC_DELIMITER);
                    if (bang > 0)
                    {
                        genericName = genericName.Substring(0, bang);
                    }
#if DBG
                    Type[] typeParams = t.GetGenericArguments();

                    // TypeArgument count must match TypeParameter count on generic type.
                    Debug.Assert(typeArgsList != null && typeArgsList.Length == typeParams.Length);

                    // for each generic param in this Type ...
                    for (int i = 0; i < typeArgsList.Length; i++)
                    {
                        // Type params should always be unbound
                        Debug.Assert(typeParams[i].IsGenericParameter);
                    }
#endif
                }

                ctrConstructedType = new CodeTypeReference(genericName);
            }
            else
            {
                ctrConstructedType = new CodeTypeReference(t.FullName);
            }

            return ctrConstructedType;
        }

        private static CodeTypeReference GenerateConstructedTypeReference(Type t, string [] typeArgsList, Type refType, string refTypeFullName, string eventName)
        {
            CodeTypeReference ctrConstructedType = null;

            // If the type has generic parameters, then need to add TypeArguments to the CodeTypeReference of this Type
            if (t.IsGenericType)
            {
                Type[] refTypeParams = null;
                CodeTypeReference ctrTypeArg = null;
                Type[] typeParams = t.GetGenericArguments();

                // NOTE: Remove when CodeDom is fixed to understand mangled generic names.
                string genericName = t.Namespace + DOT + t.Name;
                int bang = genericName.IndexOf(GENERIC_DELIMITER);
                if (bang > 0)
                {
                    genericName = genericName.Substring(0, bang);
                }

                ctrConstructedType = new CodeTypeReference(genericName);

                // NOTE: For certain types like EventHandler delegate types, CodeDom seems
                // to add bogus CodeTypeReferences as TypeArguments, so it needs to be cleared explicitly.
                ctrConstructedType.TypeArguments.Clear();

                // for each generic param in this Type ...
                foreach (Type typeParam in typeParams)
                {
                    // if the param is unbound
                    if (typeParam.IsGenericParameter)
                    {
                        // get the generic params of the containing\reference Type, only once
                        if (refTypeParams == null)
                        {
                            if (refType == null || !refType.IsGenericType || !refType.IsGenericTypeDefinition || typeArgsList == null)
                            {
                                ThrowCompilerException(SRID.ContainingTagNotGeneric, eventName, ctrConstructedType.BaseType, refTypeFullName);
                            }

                            refTypeParams = refType.GetGenericArguments();
                        }

                        ctrTypeArg = null;

                        // for each reference generic param
                        for (int i = 0; i < refTypeParams.Length; i++)
                        {
                            // if it matches the current generic param of this Type
                            if (refTypeParams[i] == typeParam)
                            {
                                // The TypeArgumentList must have already been populated with full Type names &
                                // the TypeArgument count must match TypeParameter count on generic reference type.
                                Debug.Assert(typeArgsList != null && typeArgsList.Length == refTypeParams.Length);

                                // Find the Type argument from the list that is in the same position as generic Type param
                                string currTypeArg = typeArgsList[i];

                                // and create a CodeTypeReference from it
                                ctrTypeArg = new CodeTypeReference(currTypeArg);
                                break;
                            }
                        }

                        // no match!
                        if (ctrTypeArg == null)
                        {
                            ThrowCompilerException(SRID.MatchingTypeArgsNotFoundInRefType,
                                                   eventName,
                                                   ctrConstructedType.BaseType,
                                                   typeParam.FullName,
                                                   refTypeFullName + "<" + string.Join(",", typeArgsList) + ">");
                        }
                    }
                    else
                    {
                        ctrTypeArg = new CodeTypeReference(typeParam);
                    }

                    // construct the type args list for the base class type to be generated
                    ctrConstructedType.TypeArguments.Add(ctrTypeArg);
                }
            }
            else
            {
                ctrConstructedType = new CodeTypeReference(t.FullName);
            }

            return ctrConstructedType;
        }

        private static void AddGeneratedCodeAttribute(CodeTypeMember ctmTarget)
        {
            if (s_generatedCode_ToolName == null || s_generatedCode_ToolVersion == null)
            {
                AssemblyName assemblyName = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                s_generatedCode_ToolName = assemblyName.Name;
                s_generatedCode_ToolVersion = assemblyName.Version.ToString();
            }

            CodeAttributeDeclaration cad = new CodeAttributeDeclaration(
                         new CodeTypeReference(typeof(GeneratedCodeAttribute)),
                         new CodeAttributeArgument(new CodePrimitiveExpression(s_generatedCode_ToolName)),
                         new CodeAttributeArgument(new CodePrimitiveExpression(s_generatedCode_ToolVersion)));

            ctmTarget.CustomAttributes.Add(cad);
        }

        private CodeTypeDeclaration GenerateClass(string className, ref string modifier, Type baseClass, string baseClassFullName)
        {
            // public class MyClass : BaseClass {
            //
            CodeTypeReference ctrBaseClass = null;
            CodeTypeDeclaration ctdClass = new CodeTypeDeclaration();
            ctdClass.Name = className;
            if (baseClass != null)
            {
                // At this point, we should only have fully open generic types if there is a typeargs list.
                Debug.Assert(_typeArgsList == null || (baseClass.IsGenericType && baseClass.IsGenericTypeDefinition));
                Debug.Assert(_typeArgsList != null || !baseClass.IsGenericType);

                ctrBaseClass = GenerateConstructedTypeReference(baseClass, _typeArgsList, string.Empty);

                // Add the type reference for the normal or fully constructed generic base class
                ctdClass.BaseTypes.Add(ctrBaseClass);
            }
            else if (baseClassFullName.Length > 0)
            {
                ctrBaseClass = GenerateConstructedTypeReference(null, _typeArgsList, baseClassFullName);

                // Add the type reference for the local base class
                ctdClass.BaseTypes.Add(ctrBaseClass);
            }

            ctdClass.TypeAttributes = GetTypeAttributes(ref modifier);
            ctdClass.Members.Clear();

            if (TypeAttributes.Public == ctdClass.TypeAttributes)
            {
                GenerateXmlComments(ctdClass, className);
            }

            // VBNOTE: The VB compiler will generate a ctor with an InitializeComponent call if not explicitly specified
            // by the user if this Attribute is set on the class.
            if (IsLanguageVB && !IsCompilingEntryPointClass)
            {
                ctdClass.CustomAttributes.Add(new CodeAttributeDeclaration("Microsoft.VisualBasic.CompilerServices.DesignerGenerated"));
            }

            return ctdClass;
        }

        private CodeContext GenerateSubClass(ref string className, ref string modifier, Type baseClass, string baseClassFullName)
        {
            string ns = string.Empty;
            string baseClassName = string.Empty;
            bool isValidClassName = CrackClassName(ref className, out ns);

            if (!string.IsNullOrEmpty(baseClassFullName))
            {
                int dotIndex = baseClassFullName.LastIndexOf(DOTCHAR);
                if (dotIndex != -1)
                {
                    baseClassName = baseClassFullName.Substring(dotIndex + 1);
                    if (!IsValidClassName(baseClassName))
                    {
                        ThrowCompilerException(SRID.InvalidBaseClassName, baseClassName);
                    }
                    string bns = baseClassFullName.Substring(0, dotIndex);
                    if (!IsValidCLRNamespace(bns, false))
                    {
                        ThrowCompilerException(SRID.InvalidBaseClassNamespace, bns, baseClassName);
                    }
                }
            }

            if (!isValidClassName)
            {
                // flag error. Can't throw here as we are pre-scanning and parser context doesn't
                // have customized linenum\linepos yet.
                className = DOT;
            }
            else if (IsCompilingEntryPointClass && className.Length == 0)
            {
                string baseName = baseClass != null ? baseClass.Name : baseClassName;
                className = ANONYMOUS_ENTRYCLASS_PREFIX + baseName;
                Debug.Assert(!string.IsNullOrEmpty(baseName));
                ns = XamlTypeMapper.GeneratedNamespace;
            }

            // namespace MyNamespace
            // {
            Debug.Assert(_ccRoot == null);
            Debug.Assert(_codeContexts == null || _codeContexts.Count == 0, "mismatched CodeContexts");
            CodeNamespace cns = new CodeNamespace();
            cns.Name = ns;
            cns.Types.Clear();

            CodeTypeDeclaration ctdClass = GenerateClass(className, ref modifier, baseClass, baseClassFullName);
            CodeContext cc = new CodeContextRoot(ctdClass, cns, baseClass, _typeArgsList, baseClassFullName);
            cc.ElementTypeReference = new CodeTypeReference(GetFullClassName(ns, className));

            return cc;
        }

        private void GenerateCreateDelegateHelper()
        {
            if (!IsInternalAccessSupported || !HasLocalEvent)
            {
                return;
            }

            Debug.Assert(HasLocalReference, "if we have a local event, there should be a local mapping PI for it");

            // internal Delegate _CreateDelegate(Type delegateType, string handler)
            // {
            //     return Delegate.CreateDelegate(delegateType, this, handler);
            // }
            //
            CodeMemberMethod cmmCD = new CodeMemberMethod();
            cmmCD.Name = CREATEDELEGATEHELPER;
            cmmCD.ReturnType = new CodeTypeReference(typeof(Delegate));
            cmmCD.Attributes = MemberAttributes.Assembly | MemberAttributes.Final;
            AddDebuggerNonUserCodeAttribute(cmmCD);
            AddGeneratedCodeAttribute(cmmCD);
            AddSuppressMessageAttribute(cmmCD, "Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode");

            CodeParameterDeclarationExpression param1 = new CodeParameterDeclarationExpression(typeof(Type), DELEGATETYPE);
            CodeParameterDeclarationExpression param2 = new CodeParameterDeclarationExpression(typeof(string), HANDLERARG);
            cmmCD.Parameters.Add(param1);
            cmmCD.Parameters.Add(param2);

            CodeMethodReferenceExpression cmreCD = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Delegate)), "CreateDelegate");
            CodeMethodInvokeExpression cmieCD = new CodeMethodInvokeExpression();
            cmieCD.Method = cmreCD;
            cmieCD.Parameters.Add(new CodeArgumentReferenceExpression(DELEGATETYPE));
            cmieCD.Parameters.Add(new CodeThisReferenceExpression());
            cmieCD.Parameters.Add(new CodeArgumentReferenceExpression(HANDLERARG));

            cmmCD.Statements.Add(new CodeMethodReturnStatement(cmieCD));
            _ccRoot.CodeClass.Members.Add(cmmCD);
        }

        private void GenerateInitializeComponent(bool isApp)
        {
            // public void InitializeComponent()
            // {
            //
            CodeMemberMethod cmmLC = _ccRoot.InitializeComponentFn;

            if (cmmLC == null)
            {
                cmmLC = _ccRoot.EnsureInitializeComponentFn;
                if (!isApp)
                {
                    cmmLC.ImplementationTypes.Add(new CodeTypeReference(KnownTypes.Types[(int)KnownElements.IComponentConnector]));
                }
            }

            //     if (_contentLoaded)
            //     {
            //         return;
            //     }
            //
            CodeConditionStatement ccsCL = new CodeConditionStatement();
            ccsCL.Condition = new CodeFieldReferenceExpression(null, CONTENT_LOADED);
            ccsCL.TrueStatements.Add(new CodeMethodReturnStatement());
            if (!isApp)
            {
                cmmLC.Statements.Add(ccsCL);
            }
            else
            {
                cmmLC.Statements.Insert(0, ccsCL);
            }

            //     _contentLoaded = true;
            //
            CodeAssignStatement casCL = new CodeAssignStatement(new CodeFieldReferenceExpression(null, CONTENT_LOADED),
                                                                new CodePrimitiveExpression(true));
            if (!isApp)
            {
                cmmLC.Statements.Add(casCL);
            }
            else
            {
                cmmLC.Statements.Insert(1, casCL);
            }

            // Generate canonicalized string as resource id.
            bool requestExtensionChange = false;
            string resourceID = ResourcesGenerator.GetResourceIdForResourceFile(
                    SourceFileInfo.RelativeSourceFilePath + XAML,
                    SourceFileInfo.OriginalFileLinkAlias,
                    SourceFileInfo.OriginalFileLogicalName,
                    TargetPath,
                    Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar,
                    requestExtensionChange);

            string uriPart = string.Empty;

            // Attempt to parse out the AssemblyVersion if it exists.  This validates that we can either use an empty version string (wildcards exist)
            // or we can utilize the passed in string (valid parse).
            if (!VersionHelper.TryParseAssemblyVersion(AssemblyVersion, allowWildcard: true, version: out _, out bool hasWildcard)
                && !string.IsNullOrWhiteSpace(AssemblyVersion))
            {
                throw new AssemblyVersionParseException(SR.Get(SRID.InvalidAssemblyVersion, AssemblyVersion));
            }

            // In .NET Framework (non-SDK-style projects), the process to use a wildcard AssemblyVersion is to do the following:
            //   - Modify the AssemblyVersionAttribute to a wildcard string (e.g. "1.2.*")
            //   - Set Deterministic to false in the build
            // During MarkupCompilation, the AssemblyVersion property would not be set and WPF would correctly generate a resource URI without a version.
            // In .NET Core/5 (or .NET Framework SDK-style projects), the same process can be used if GenerateAssemblyVersionAttribute is set to false in 
            // the build.  However, this isn't really the idiomatic way to set the version for an assembly.  Instead, developers are more likely to use the 
            // AssemblyVersion build property.  If a developer explicitly sets the AssemblyVersion build property to a wildcard version string, we would use 
            // that as part of the URI here.  This results in an error in Version.Parse during InitializeComponent's call tree.  Instead, do as we would have 
            // when the developer sets a wildcard version string via AssemblyVersionAttribute and use an empty string.
            string version = hasWildcard || String.IsNullOrEmpty(AssemblyVersion)
                ? String.Empty 
                : COMPONENT_DELIMITER + VER + AssemblyVersion;

            string token = String.IsNullOrEmpty(AssemblyPublicKeyToken) 
                ? String.Empty 
                : COMPONENT_DELIMITER + AssemblyPublicKeyToken;
            
            uriPart = FORWARDSLASH + AssemblyName + version + token + COMPONENT_DELIMITER + COMPONENT + FORWARDSLASH + resourceID;

            //
            //  Uri resourceLocator = new Uri(uriPart, UriKind.Relative);
            //
            string resVarname = RESOURCE_LOCATER;

            CodeFieldReferenceExpression cfreRelUri = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(System.UriKind)), "Relative");

            CodeExpression[] uriParams = { new CodePrimitiveExpression(uriPart), cfreRelUri };
            CodeObjectCreateExpression coceResourceLocator = new CodeObjectCreateExpression(typeof(System.Uri), uriParams);
            CodeVariableDeclarationStatement cvdsresLocator = new CodeVariableDeclarationStatement(typeof(System.Uri), resVarname, coceResourceLocator);

            cmmLC.Statements.Add(cvdsresLocator);

            //
            //  System.Windows.Application.LoadComponent(this, resourceLocator);
            //
            CodeMethodReferenceExpression cmreLoadContent = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(KnownTypes.Types[(int)KnownElements.Application]), LOADCOMPONENT);
            CodeMethodInvokeExpression cmieLoadContent = new CodeMethodInvokeExpression();

            cmieLoadContent.Method = cmreLoadContent;

            CodeVariableReferenceExpression cvreMemStm = new CodeVariableReferenceExpression(resVarname);

            cmieLoadContent.Parameters.Add(new CodeThisReferenceExpression());
            cmieLoadContent.Parameters.Add(cvreMemStm);

            CodeExpressionStatement cesLC = new CodeExpressionStatement(cmieLoadContent);
            AddLinePragma(cesLC, 1);
            cmmLC.Statements.Add(cesLC);

            // private bool _contentLoaded;
            //
            CodeMemberField cmfCL = new CodeMemberField();
            cmfCL.Name = CONTENT_LOADED;
            cmfCL.Attributes = MemberAttributes.Private;
            cmfCL.Type = new CodeTypeReference(typeof(bool));
            _ccRoot.CodeClass.Members.Add(cmfCL);

            if (!isApp)
            {
                // Make sure that ICC.Connect is generated to avoid compilation errors
                EnsureHookupFn();
            }
        }

        private void GenerateInternalTypeHelperImplementation()
        {
            if (!IsInternalAccessSupported ||
                !(HasInternals || HasLocalReference) ||
                _hasGeneratedInternalTypeHelper)
            {
                return;
            }

            _hasGeneratedInternalTypeHelper = true;

            // namespace XamlGeneratedNamespace
            // {
            //
            CodeNamespace cns = new CodeNamespace();
            cns.Name = XamlTypeMapper.GeneratedNamespace;

            //     [EditorBrowsable(EditorBrowsableState.Never)]
            //     public sealed class GeneratedInternalTypeHelper : InternalTypeHelper
            //     {
            //
            CodeTypeDeclaration ctdClass = new CodeTypeDeclaration();
            ctdClass.Name = XamlTypeMapper.GeneratedInternalTypeHelperClassName;
            ctdClass.BaseTypes.Add(new CodeTypeReference("System.Windows.Markup.InternalTypeHelper"));
            ctdClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            AddDebuggerNonUserCodeAttribute(ctdClass);
            AddGeneratedCodeAttribute(ctdClass);
            AddEditorBrowsableAttribute(ctdClass);
            GenerateXmlComments(ctdClass, ctdClass.Name);

            //         protected override object CreateInstance(Type type, CultureInfo culture)
            //         {
            //             return Activator.CreateInstance(type,
            //                                             BindingFlags.Public |
            //                                             BindingFlags.NonPublic |
            //                                             BindingFlags.Instance |
            //                                             BindingFlags.CreateInstance,
            //                                             null,
            //                                             null,
            //                                             culture);
            //         }
            //
            CodeMemberMethod cmmCI = new CodeMemberMethod();
            cmmCI.Name = "CreateInstance";
            cmmCI.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            cmmCI.ReturnType = new CodeTypeReference(typeof(Object));

            CodeParameterDeclarationExpression param1 = new CodeParameterDeclarationExpression(typeof(Type), TYPE);
            CodeParameterDeclarationExpression param4 = new CodeParameterDeclarationExpression(typeof(CultureInfo), CULTURE);
            cmmCI.Parameters.Add(param1);
            cmmCI.Parameters.Add(param4);

            CodeMethodReferenceExpression cmreCI = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Activator)), "CreateInstance");
            CodeMethodInvokeExpression cmieCI = new CodeMethodInvokeExpression();
            cmieCI.Method = cmreCI;
            cmieCI.Parameters.Add(new CodeArgumentReferenceExpression(TYPE));
            CodeFieldReferenceExpression cfre1 = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "Public");
            CodeFieldReferenceExpression cfre2 = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "NonPublic");
            CodeFieldReferenceExpression cfre3 = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "Instance");
            CodeFieldReferenceExpression cfre4 = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "CreateInstance");
            CodeBinaryOperatorExpression cboe1 = new CodeBinaryOperatorExpression(cfre1, CodeBinaryOperatorType.BitwiseOr, cfre2);
            CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression(cfre3, CodeBinaryOperatorType.BitwiseOr, cfre4);
            CodeBinaryOperatorExpression cboeCI = new CodeBinaryOperatorExpression(cboe1, CodeBinaryOperatorType.BitwiseOr, cboe2);
            cmieCI.Parameters.Add(cboeCI);
            cmieCI.Parameters.Add(new CodePrimitiveExpression(null));
            cmieCI.Parameters.Add(new CodePrimitiveExpression(null));
            cmieCI.Parameters.Add(new CodeArgumentReferenceExpression(CULTURE));

            cmmCI.Statements.Add(new CodeMethodReturnStatement(cmieCI));
            GenerateXmlComments(cmmCI, cmmCI.Name);
            ctdClass.Members.Add(cmmCI);

            //         protected override object GetPropertyValue(PropertyInfo propertyInfo, object target, CultureInfo culture)
            //         {
            //             return propertyInfo.GetValue(target, BindingFlags.Default, null, null, culture);
            //         }
            //
            CodeMemberMethod cmmGPV = new CodeMemberMethod();
            cmmGPV.Name = "GetPropertyValue";
            cmmGPV.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            cmmGPV.ReturnType = new CodeTypeReference(typeof(Object));

            param1 = new CodeParameterDeclarationExpression(typeof(PropertyInfo), PROPINFO);
            CodeParameterDeclarationExpression param2 = new CodeParameterDeclarationExpression(typeof(object), TARGET);
            cmmGPV.Parameters.Add(param1);
            cmmGPV.Parameters.Add(param2);
            cmmGPV.Parameters.Add(param4);

            CodeMethodReferenceExpression cmreGPV = new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PROPINFO), "GetValue");
            CodeMethodInvokeExpression cmieGPV = new CodeMethodInvokeExpression();
            cmieGPV.Method = cmreGPV;
            cmieGPV.Parameters.Add(new CodeArgumentReferenceExpression(TARGET));
            cmieGPV.Parameters.Add(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), DEFAULT));
            cmieGPV.Parameters.Add(new CodePrimitiveExpression(null));
            cmieGPV.Parameters.Add(new CodePrimitiveExpression(null));
            cmieGPV.Parameters.Add(new CodeArgumentReferenceExpression(CULTURE));

            cmmGPV.Statements.Add(new CodeMethodReturnStatement(cmieGPV));
            GenerateXmlComments(cmmGPV, cmmGPV.Name);
            ctdClass.Members.Add(cmmGPV);

            //         protected override void SetPropertyValue(PropertyInfo propertyInfo, object target, object value, CultureInfo culture)
            //         {
            //             propertyInfo.SetValue(target, value, BindingFlags.Default, null, null, culture);
            //         }
            //
            CodeMemberMethod cmmSPV = new CodeMemberMethod();
            cmmSPV.Name = "SetPropertyValue";
            cmmSPV.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            CodeParameterDeclarationExpression param3 = new CodeParameterDeclarationExpression(typeof(object), VALUE);
            cmmSPV.Parameters.Add(param1);
            cmmSPV.Parameters.Add(param2);
            cmmSPV.Parameters.Add(param3);
            cmmSPV.Parameters.Add(param4);

            CodeMethodReferenceExpression cmreSPV = new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PROPINFO), "SetValue");
            CodeMethodInvokeExpression cmieSPV = new CodeMethodInvokeExpression();
            cmieSPV.Method = cmreSPV;
            cmieSPV.Parameters.Add(new CodeArgumentReferenceExpression(TARGET));
            cmieSPV.Parameters.Add(new CodeArgumentReferenceExpression(VALUE));
            cmieSPV.Parameters.Add(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), DEFAULT));
            cmieSPV.Parameters.Add(new CodePrimitiveExpression(null));
            cmieSPV.Parameters.Add(new CodePrimitiveExpression(null));
            cmieSPV.Parameters.Add(new CodeArgumentReferenceExpression(CULTURE));

            cmmSPV.Statements.Add(new CodeExpressionStatement(cmieSPV));
            GenerateXmlComments(cmmSPV, cmmSPV.Name);
            ctdClass.Members.Add(cmmSPV);

            //         protected override Delegate CreateDelegate(Type delegateType, object target, string handler)
            //         {
            //             return (Delegate)target.GetType().InvokeMember("_CreateDelegate",
            //                                                            BindingFlags.Instance |
            //                                                            BindingFlags.NonPublic |
            //                                                            BindingFlags.InvokeMethod,
            //                                                            null,
            //                                                            target,
            //                                                            new object[] { delegateType, handler });
            //         }
            //
            CodeMemberMethod cmmCD = new CodeMemberMethod();
            cmmCD.Name = "CreateDelegate";
            cmmCD.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            cmmCD.ReturnType = new CodeTypeReference(typeof(Delegate));

            param1 = new CodeParameterDeclarationExpression(typeof(Type), DELEGATETYPE);
            param3 = new CodeParameterDeclarationExpression(typeof(string), HANDLERARG);
            cmmCD.Parameters.Add(param1);
            cmmCD.Parameters.Add(param2);
            cmmCD.Parameters.Add(param3);

            CodeArgumentReferenceExpression careTarget = new CodeArgumentReferenceExpression(TARGET);
            CodeMethodReferenceExpression cmreGetType = new CodeMethodReferenceExpression(careTarget, "GetType");
            CodeMethodInvokeExpression cmieGetType = new CodeMethodInvokeExpression();
            cmieGetType.Method = cmreGetType;

            CodeMethodReferenceExpression cmreCD = new CodeMethodReferenceExpression(cmieGetType, "InvokeMember");
            CodeMethodInvokeExpression cmieCD = new CodeMethodInvokeExpression();
            cmieCD.Method = cmreCD;
            cmieCD.Parameters.Add(new CodePrimitiveExpression(CREATEDELEGATEHELPER));

            CodeFieldReferenceExpression cfre5 = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BindingFlags)), "InvokeMethod");
            CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression(cfre2, CodeBinaryOperatorType.BitwiseOr, cfre3);
            CodeBinaryOperatorExpression cboeCD = new CodeBinaryOperatorExpression(cfre5, CodeBinaryOperatorType.BitwiseOr, cboe);
            cmieCD.Parameters.Add(cboeCD);

            cmieCD.Parameters.Add(new CodePrimitiveExpression(null));
            cmieCD.Parameters.Add(careTarget);

            CodeArrayCreateExpression caceCD = new CodeArrayCreateExpression(typeof(object));
            CodeArgumentReferenceExpression careDelType = new CodeArgumentReferenceExpression(DELEGATETYPE);
            CodeArgumentReferenceExpression careHandler = new CodeArgumentReferenceExpression(HANDLERARG);
            caceCD.Initializers.Add(careDelType);
            caceCD.Initializers.Add(careHandler);
            cmieCD.Parameters.Add(caceCD);
            cmieCD.Parameters.Add(new CodePrimitiveExpression(null));

            CodeCastExpression cceCD = new CodeCastExpression(typeof(Delegate), cmieCD);
            cmmCD.Statements.Add(new CodeMethodReturnStatement(cceCD));
            GenerateXmlComments(cmmCD, cmmCD.Name);
            ctdClass.Members.Add(cmmCD);

            //         protected override void AddEventHandler(EventInfo eventInfo, object target, Delegate handler);
            //         {
            //             eventInfo.AddEventHandler(target, handler);
            //         }
            //
            CodeMemberMethod cmmAEH = new CodeMemberMethod();
            cmmAEH.Name = "AddEventHandler";
            cmmAEH.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            param1 = new CodeParameterDeclarationExpression(typeof(EventInfo), EVENTINFO);
            param3 = new CodeParameterDeclarationExpression(typeof(Delegate), HANDLERARG);
            cmmAEH.Parameters.Add(param1);
            cmmAEH.Parameters.Add(param2);
            cmmAEH.Parameters.Add(param3);

            CodeMethodReferenceExpression cmreAEH = new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(EVENTINFO), "AddEventHandler");
            CodeMethodInvokeExpression cmieAEH = new CodeMethodInvokeExpression();
            cmieAEH.Method = cmreAEH;
            cmieAEH.Parameters.Add(new CodeArgumentReferenceExpression(TARGET));
            cmieAEH.Parameters.Add(new CodeArgumentReferenceExpression(HANDLERARG));

            cmmAEH.Statements.Add(new CodeExpressionStatement(cmieAEH));
            GenerateXmlComments(cmmAEH, cmmAEH.Name);
            ctdClass.Members.Add(cmmAEH);

            //     }
            //
            cns.Types.Add(ctdClass);

            // }
            //
            CodeCompileUnit ccu = new CodeCompileUnit();
            ccu.Namespaces.Add(cns);

            // For VB only we need to let the parser know about the RootNamespace value
            // in order to look for the XamlGeneratedNamespace.GeneratedInternalTypeHelper
            // type whose full type name would have been implicitly by the VB comopiler to
            // RootNS.XamlGeneratedNamespace.GeneratedInternalTypeHelper

            if (IsLanguageVB && !string.IsNullOrEmpty(DefaultNamespace))
            {
                // [assembly: RootNamespaceAttribute("RootNS")]
                CodeAttributeDeclaration cad = new CodeAttributeDeclaration(
                             "System.Windows.Markup.RootNamespaceAttribute",
                             new CodeAttributeArgument(new CodePrimitiveExpression(DefaultNamespace)));

                ccu.AssemblyCustomAttributes.Add(cad);
            }

            MemoryStream codeMemStream = new MemoryStream();

            // using Disposes the StreamWriter when it ends.  Disposing the StreamWriter
            // also closes the underlying MemoryStream.  Furthermore, don't add BOM here since
            // TaskFileService.WriteGeneratedCodeFile adds it.
            using (StreamWriter codeStreamWriter = new StreamWriter(codeMemStream, new UTF8Encoding(false)))
            {
                CodeGeneratorOptions o = new CodeGeneratorOptions();
                CodeDomProvider codeProvider = EnsureCodeProvider();
                codeProvider.GenerateCodeFromCompileUnit(ccu, codeStreamWriter, o);

                codeStreamWriter.Flush();
                TaskFileService.WriteGeneratedCodeFile(codeMemStream.ToArray(),
                    TargetPath + SharedStrings.GeneratedInternalTypeHelperFileName,
                    SharedStrings.GeneratedExtension, SharedStrings.IntellisenseGeneratedExtension,
                    LanguageSourceExtension);
            }
        }

        internal string StartElement(ref string className, string subClassFullName, ref string modifier, Type elementType, string baseClassFullName)
        {
            string classFullName = null;
            CodeContext cc = null;

            if (_ccRoot == null)
            {
                if (className.Length > 0)
                {
                    IsCodeNeeded = true;
                }
                else if (subClassFullName.Length > 0)
                {
                    ThrowCompilerException(SRID.MissingClassWithSubClass, DefinitionNSPrefix);
                }
                else if (modifier.Length > 0)
                {
                    ThrowCompilerException(SRID.MissingClassWithModifier, DefinitionNSPrefix);
                }
                else if (_typeArgsList != null)
                {
                    string rootClassName = elementType != null ? elementType.Name : baseClassFullName.Substring(baseClassFullName.LastIndexOf(DOT, StringComparison.Ordinal)+1);
                    ThrowCompilerException(SRID.MissingClassDefinitionForTypeArgs, rootClassName, DefinitionNSPrefix);
                }

                // Don't allow subclassing further from markup-subclasses with content
                if (elementType != null && KnownTypes.Types[(int)KnownElements.IComponentConnector].IsAssignableFrom(elementType))
                {
                    ThrowCompilerException(SRID.SubSubClassingNotAllowed, elementType.FullName);
                }

                cc = GenerateSubClass(ref className, ref modifier, elementType, baseClassFullName);
                Debug.Assert(_codeContexts.Count == 0, "mismatched codecontext");
                _ccRoot = cc as CodeContextRoot;
                Debug.Assert(_ccRoot != null);

                if (IsCodeNeeded)
                {
                    if (subClassFullName.Length > 0)
                    {
                        classFullName = subClassFullName;
                        _ccRoot.SubClass = classFullName;
                    }
                    else
                    {
                        classFullName = GetFullClassName(_ccRoot.CodeNS.Name, _ccRoot.CodeClass.Name);
                    }

                    if (IsLanguageVB)
                    {
                        // This classFullName is going to be used to write Root Start Element.
                        // If it is for VB, and DefaultClrName is set, we need to put DefaultClrName
                        // as prefix to any existing full class name.
                        //
                        // if this x:Class is set to "MyNS.MyPage, and RootNamespace (DefaultNamespace)
                        // is set to MyRoot, the finally generated class by VBC would be MyRoot.MyNS.MyPage.
                        // so in the Baml record, it should keep MyRoot.MyNS.MyPage.
                        //

                        classFullName = GetFullClassName(DefaultNamespace, classFullName);
                    }
                }

                if (IsCompilingEntryPointClass)
                {
                    cc.IsAllowedNameScope = false;
                }
            }
            else
            {
                cc = new CodeContext(elementType, null, baseClassFullName);
                CodeContext ccParent = (CodeContext)_codeContexts.Peek();
                cc.IsAllowedNameScope = ccParent.IsAllowedNameScope;
            }

            _codeContexts.Push(cc);

            return classFullName;
        }

        internal void EndElement(bool pass2)
        {
            CodeContext cc = (CodeContext)_codeContexts.Pop();
            Debug.Assert(cc != null);

            if (_codeContexts.Count == 0)
            {
                Debug.Assert(_ccRoot == (cc as CodeContextRoot));
                Debug.Assert(_ccRoot.CodeClass != null);

                if (!pass2)
                {
                    // For entry point class, a sub-class is always needed
                    // even if if wasn't otherwise needed by other markup
                    // like x:Code or events etc. upto this point.
                    if (IsCompilingEntryPointClass)
                    {
                        IsCodeNeeded = true;
                    }

                    if (IsCodeNeeded)
                    {
                        if (IsBamlNeeded)
                        {
                            GenerateInitializeComponent(IsCompilingEntryPointClass);
                            if (!IsCompilingEntryPointClass)
                            {
                                GenerateCreateDelegateHelper();
                            }
                        }
                        else
                        {
                            Debug.Assert(_ccRoot.HookupFn == null);
                        }

                        EndHookups();
                        EndStyleEventConnection();
                    }

                    GenerateSource();
                }
            }
        }

        internal void AddUsing(string clrNS)
        {
            if (String.IsNullOrEmpty(clrNS))
            {
                return;
            }

            if (_usingNS == null)
            {
                _usingNS = new ArrayList();
            }

            _usingNS.Add(clrNS);
        }

#endregion CorePageGen

#region App Entry Point

        // This code block is shared by regular Application and HostInBrowser Application
        private CodeVariableReferenceExpression GenerateAppInstance(CodeMemberMethod cmmMain)
        {
            string appClassName = _ccRoot.SubClass.Length > 0 ? _ccRoot.SubClass
                                               : GetFullClassName(_ccRoot.CodeNS.Name, _ccRoot.CodeClass.Name);

            //  MyNS.MyApplication app = new MyNS.MyApplication();
            //
            CodeObjectCreateExpression coce;
            CodeVariableReferenceExpression cvre = new CodeVariableReferenceExpression(APPVAR);
            CodeExpression[] ctorParams = {};

            coce = new CodeObjectCreateExpression(appClassName, ctorParams);

            CodeVariableDeclarationStatement cvds = new CodeVariableDeclarationStatement(appClassName, APPVAR, coce);

            cmmMain.Statements.Add(cvds);

            return cvre;
        }

        internal void AddApplicationProperty(MemberInfo memberInfo, string attributeValue, int lineNumber)
        {
            Debug.Assert(_ccRoot == (_codeContexts.Peek() as CodeContextRoot));
            Debug.Assert(_ccRoot.ElementType == null ||
                         (memberInfo.DeclaringType.IsAssignableFrom(_ccRoot.ElementType) && (memberInfo is PropertyInfo)));

            TypeConvertContext ctx = new TypeConvertContext(_parserContext, attributeValue);
            CodeExpression ceValue = GetPropertyValueExpression(ctx, typeof(Uri), null, attributeValue);
            CodeThisReferenceExpression ctreTag = new CodeThisReferenceExpression();
            CodePropertyReferenceExpression cprePropSet = new CodePropertyReferenceExpression(ctreTag, memberInfo.Name);

            CodeStatement csPropSet = new CodeAssignStatement(cprePropSet, ceValue);

            AddLinePragma(csPropSet, lineNumber);

            _ccRoot.EnsureInitializeComponentFn.Statements.Add(csPropSet);
        }

        internal void AddApplicationEvent(MarkupEventInfo mei)
        {
            // validate the event handler name per C# grammar for identifiers
            ValidateEventHandlerName(mei.eventName, mei.eventHandler);

            // this.FooEvent += new FooEventHandlerDelegate(this.OnFoo);
            CodeThisReferenceExpression ctre = new CodeThisReferenceExpression();
            CodeStatement csEvent = AddCLREvent(_ccRoot, ctre, mei);

            Debug.Assert(_ccRoot == (_codeContexts.Peek() as CodeContextRoot));
            _ccRoot.EnsureInitializeComponentFn.Statements.Add(csEvent);
        }

        private CodeMemberMethod GenerateEntryPointMethod()
        {
            CodeMemberMethod cmmMain = null;
            CodeDomProvider codeProvider = EnsureCodeProvider();

            if (codeProvider.Supports(GeneratorSupport.EntryPointMethod))
            {
                //
                // [STAThread]
                // public static void Main () {
                //

                cmmMain = new CodeEntryPointMethod();
                cmmMain.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                cmmMain.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(STAThreadAttribute).FullName));
                AddDebuggerNonUserCodeAttribute(cmmMain);
                AddGeneratedCodeAttribute(cmmMain);
                GenerateXmlComments(cmmMain, "Application Entry Point.");
                cmmMain.ReturnType = new CodeTypeReference(typeof(void));
            }

            return cmmMain;
        }

        private void GenerateAppEntryPoint()
        {
            if (ApplicationFile.Length > 0)
            {

                // [STAThread]
                // public static void Main () {
                //
                CodeMemberMethod cmmMain = GenerateEntryPointMethod();

                if (cmmMain != null)
                {
                    CodeVariableReferenceExpression cvreSplashScreen = null;
                    if (!string.IsNullOrEmpty(_splashImage) && !HostInBrowser)
                    {
                        cvreSplashScreen = GenerateSplashScreenInstance(cmmMain);
                    }

                    //   MyApplication app = new MyApplication();
                    //
                    CodeVariableReferenceExpression cvreApp = GenerateAppInstance(cmmMain);

                    if (_ccRoot.InitializeComponentFn != null)
                    {
                        //   app.InitializeComponent();
                        //
                        CodeMethodInvokeExpression cmieIT = new CodeMethodInvokeExpression();
                        cmieIT.Method = new CodeMethodReferenceExpression(cvreApp, INITIALIZE_COMPONENT);
                        cmmMain.Statements.Add(new CodeExpressionStatement(cmieIT));
                    }

                    if (!HostInBrowser)
                    {
                        //   app.Run();
                        //
                        CodeMethodReferenceExpression cmreRun = new CodeMethodReferenceExpression(cvreApp, "Run");
                        CodeMethodInvokeExpression cmieRun = new CodeMethodInvokeExpression();
                        cmieRun.Method = cmreRun;

                        CodeStatement csRun = new CodeExpressionStatement(cmieRun);
                        cmmMain.Statements.Add(csRun);
                    }

                    _ccRoot.CodeClass.Members.Add(cmmMain);
                }
            }
        }

        private void GenerateLooseContentAttributes()
        {
            CodeDomProvider codeProvider = EnsureCodeProvider();

            if (codeProvider.Supports(GeneratorSupport.AssemblyAttributes))
            {
                CodeCompileUnit ccu = new CodeCompileUnit();

                foreach (string file in ContentList)
                {
                    // [assembly: AssemblyAssociatedContentFileAttribute("file")]

                    string normalized = ResourceIDHelper.GetResourceIDFromRelativePath(file);
                    CodeAttributeDeclaration cad = new CodeAttributeDeclaration(
                                 "System.Windows.Resources.AssemblyAssociatedContentFileAttribute",
                                 new CodeAttributeArgument(new CodePrimitiveExpression(normalized)));

                    ccu.AssemblyCustomAttributes.Add(cad);
                }

                MemoryStream codeMemStream = new MemoryStream();

                // using Disposes the StreamWriter when it ends.  Disposing the StreamWriter
                // also closes the underlying MemoryStream.  Furthermore, don't add BOM here since
                // TaskFileService.WriteGeneratedCodeFile adds it.
                using (StreamWriter codeStreamWriter = new StreamWriter(codeMemStream, new UTF8Encoding(false)))
                {
                    CodeGeneratorOptions o = new CodeGeneratorOptions();
                    codeProvider.GenerateCodeFromCompileUnit(ccu, codeStreamWriter, o);

                    codeStreamWriter.Flush();
                    TaskFileService.WriteGeneratedCodeFile(codeMemStream.ToArray(),
                        TargetPath + AssemblyName + SharedStrings.ContentFile,
                        SharedStrings.GeneratedExtension, SharedStrings.IntellisenseGeneratedExtension,
                        LanguageSourceExtension);
                }
            }
        }

#endregion App Entry Point

#region Splash Screen Code Generation

        private CodeVariableReferenceExpression GenerateSplashScreenInstance(CodeMemberMethod cmmMain)
        {
            // SplashScreen splashScreen = new SplashScreen(Assembly.GetExecutingAssembly(), "splash.png");
            CodeObjectCreateExpression coceApplicationSplashScreen = new CodeObjectCreateExpression(SPLASHCLASSNAME, new CodePrimitiveExpression(GetSplashResourceId()));
            // ApplicationSplashScreen splashScreen = ...
            CodeVariableDeclarationStatement cvdsAppSplash = new CodeVariableDeclarationStatement(SPLASHCLASSNAME, SPLASHVAR, coceApplicationSplashScreen);
            cmmMain.Statements.Add(cvdsAppSplash);

            // splashScreen.Show(true);
            CodeVariableReferenceExpression cvreAppSplash = new CodeVariableReferenceExpression(SPLASHVAR);
            CodeMethodInvokeExpression cmieShowSplashScreen = new CodeMethodInvokeExpression(cvreAppSplash, "Show", new CodePrimitiveExpression(true));
            cmmMain.Statements.Add(cmieShowSplashScreen);

            return cvreAppSplash;
        }

        private string GetSplashResourceId()
        {
            // Perform the same resouce string mangle that is done in ResourceGenerator
            string resourceId;
            string fullFilePath = Path.GetFullPath(_splashImage);
            string relPath = TaskHelper.GetRootRelativePath(TargetPath, fullFilePath);

            //
            // If the resFile is relative to the StagingDir (OutputPath here)
            // take the relative path as resource id.
            // If the resFile is not relative to StagingDir, but relative
            // to the project directory, take this relative path as resource id.
            // Otherwise, just take the file name as resource id.
            //

            if (string.IsNullOrEmpty(relPath))
            {
                relPath = TaskHelper.GetRootRelativePath(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, fullFilePath);
            }

            if (string.IsNullOrEmpty(relPath) == false)
            {
                resourceId = relPath;
            }
            else
            {
                resourceId = Path.GetFileName(fullFilePath);
            }

            // Modify resource ID to correspond to canonicalized Uri format
            // i.e. - all lower case, use "/" as separator
            // ' ' is converted to escaped version %20
            //

            resourceId = ResourceIDHelper.GetResourceIDFromRelativePath(resourceId);

            return resourceId;
        }

#endregion

#region CodeContext

        private class CodeContext
        {
            internal CodeContext(Type elementType, string [] typeArgsList, string localElementFullName)
            {
                _elementType = elementType;
                _typeArgsList = typeArgsList;
                _localElementFullName = localElementFullName;
            }

            internal Type ElementType
            {
                get { return _elementType; }
            }

            internal string ElementName
            {
                get { return _elementType != null ? _elementType.Name : _localElementFullName.Substring(_localElementFullName.LastIndexOf(DOT, StringComparison.Ordinal) + 1); }
            }

            internal string [] GenericTypeArgs
            {
                get { return _typeArgsList; }
            }

            internal string LocalElementFullName
            {
                get { return _localElementFullName; }
            }

            internal bool IsAllowedNameScope
            {
                get { return _isAllowedNameScope; }
                set { _isAllowedNameScope = value; }
            }

            internal CodeTypeReference ElementTypeReference
            {
                get
                {
                    if (_ctrElemTypeRef == null)
                    {
                        _ctrElemTypeRef = MarkupCompiler.GenerateConstructedTypeReference(_elementType, _typeArgsList, _localElementFullName);
                    }

                    return _ctrElemTypeRef;
                }

                set { _ctrElemTypeRef = value; }
            }

            private bool _isAllowedNameScope = true;
            private Type _elementType = null;
            private string [] _typeArgsList = null;
            private string _localElementFullName = string.Empty;
            protected CodeTypeReference _ctrElemTypeRef = null;
        }

        private class CodeContextRoot : CodeContext
        {
            internal CodeContextRoot(CodeTypeDeclaration codeClass,
                                     CodeNamespace codeNS,
                                     Type elementType,
                                     string [] typeArgsList,
                                     string localElementFullName) : base (elementType, typeArgsList, localElementFullName)
            {
                _codeNS = codeNS;
                _codeClass = codeClass;
                _ctrElemTypeRef = codeClass.BaseTypes[0];
            }

            internal CodeMemberMethod HookupFn
            {
                get { return _hookupFn; }
                set { _hookupFn = value; }
            }

            internal CodeMemberMethod StyleConnectorFn
            {
                get { return _styleConnectorFn; }
                set { _styleConnectorFn = value; }
            }

            internal CodeMemberMethod InitializeComponentFn
            {
                get { return _initializeComponentFn; }
            }

            internal CodeMemberMethod EnsureInitializeComponentFn
            {
                get
                {
                    if (_initializeComponentFn == null)
                    {
                        _initializeComponentFn = new CodeMemberMethod();
                        _initializeComponentFn.Name = INITIALIZE_COMPONENT;
                        _initializeComponentFn.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                        AddDebuggerNonUserCodeAttribute(_initializeComponentFn);
                        AddGeneratedCodeAttribute(_initializeComponentFn);
                        MarkupCompiler.GenerateXmlComments(_initializeComponentFn, INITIALIZE_COMPONENT);
                        _codeClass.Members.Add(_initializeComponentFn);
                    }

                    return _initializeComponentFn;
                }
            }

            internal CodeTypeDeclaration CodeClass
            {
                get { return _codeClass; }
            }

            internal CodeNamespace CodeNS
            {
                get { return _codeNS; }
            }

            // This is used as the class to instantiate when a language does not support partial
            // classes. A code-behind file needs to derive this class from the generated sub-class
            // that is normally specified by the x:Class attribute.
            internal string SubClass
            {
                get { return _subClass; }
                set { _subClass = value; }
            }

            private CodeTypeDeclaration _codeClass;
            private CodeNamespace _codeNS;
            private CodeMemberMethod _initializeComponentFn = null;
            private CodeMemberMethod _hookupFn = null;
            private CodeMemberMethod _styleConnectorFn = null;
            private string _subClass = string.Empty;
        }

#endregion CodeContext

#endregion Implementation

#region Private Data

        private string                  _targetPath = string.Empty; // Current Dir is default
        private string[]                _contentList = null;
        private ArrayList               _referenceAssemblyList = null;
        private string                  _localXamlApplication = null;
        private string[]                _localXamlPages = null;
        private string []               _typeArgsList = null;
        private ArrayList               _pendingLocalFiles = null;
        private bool                    _hostInBrowser = false;
        private bool                    _xamlDebuggingInformation = false;
        private string                  _splashImage = null;

        private bool                    _isLangCSharp = false;
        private bool                    _isLangVB = false;
        private string                  _language = string.Empty;
        private string                  _languageSourceExtension = string.Empty;
        private static string           _definitionNSPrefix = DEFINITION_PREFIX;
        private CompilerInfo            _ci = null;
        private CodeDomProvider         _codeProvider = null;
        private XamlTypeMapper          _typeMapper = null;

        private bool                    _isCompilingEntryPointClass = false;
        private bool                    _isBamlNeeded = false;
        private bool                    _isCodeNeeded = false;
        private bool                    _hasLocalEvent = false;
        private bool                    _hasGeneratedInternalTypeHelper = false;
        private string                  _assemblyName = string.Empty;
        private string                  _assemblyVersion = string.Empty;
        private string                  _assemblyPublicKeyToken = string.Empty;
        private string                  _applicationFile = string.Empty;
        private string                  _defaultNamespace = string.Empty;
        private ParserHooks             _parserHooks;

        private SourceFileInfo          _sourceFileInfo = null;
        private string                  _compilationUnitSourcePath = string.Empty;
        private ArrayList               _usingNS = null;
        private Assembly                _localAssembly = null;
        private ReferenceAssembly       _localAssemblyFile = null;
        private ParserContext           _parserContext = null;
        private CodeContextRoot         _ccRoot = null;
        private Stack                   _codeContexts = null;
        private ITaskFileService        _taskFileService = null;
        private TaskLoggingHelper       _taskLogger = null;
        private bool                    _hasEmittedEventSetterDeclaration;

        // Per language sccess Modfiers
        private string                  _private = string.Empty;
        private string                  _public = string.Empty;
        private string                  _protected = string.Empty;
        private string                  _internal = string.Empty;
        private string                  _protectedInternal = string.Empty;
        private string                  _privateClass = string.Empty;
        private string                  _publicClass = string.Empty;

        // Prefixes & Tags
        private const string            INDENT12 = "            ";
        private const string            ANONYMOUS_ENTRYCLASS_PREFIX = "Generated";
        private const string            DEFINITION_PREFIX = "x";
        private const char              COMMA = ',';
        private const char              GENERIC_DELIMITER = '`';
        internal const char             DOTCHAR = '.';
        internal const string           DOT = ".";
        internal const string           CODETAG = "Code";

        // Language support
        private const string            XAML = ".xaml";
        private const string            BAML = ".baml";
        private const string            VB = "vb";
        private const string            CSHARP = "c#";
        private const string            JSHARP = "vj#";
        private const string            JSCRIPT = "js";

        // Generated identifiers
        private const string            CREATEDELEGATEHELPER = "_CreateDelegate";
        private const string            CONTENT_LOADED = "_contentLoaded";
        private const string            CONNECT = "Connect";
        private const string            CONNECTIONID = "connectionId";
        private const string            TARGET = "target";
        private const string            EVENTSETTER = "eventSetter";
        private const string            EVENT = "Event";
        private const string            ADDHANDLER = "AddHandler";
        private const string            HELPER = "Helper";
        private const string            HANDLERARG = "handler";
        private const string            TYPE = "type";
        private const string            CULTURE = "culture";
        private const string            DEFAULT = "Default";
        private const string            VALUE = "value";
        private const string            DELEGATETYPE = "delegateType";
        private const string            PROPINFO = "propertyInfo";
        private const string            EVENTINFO = "eventInfo";
        private const string            APPVAR = "app";
        private const string            SPLASHVAR = "splashScreen";
        private const string            SPLASHCLASSNAME = "SplashScreen";
        private const string            ARGS = "args";
        private const string            INITIALIZE_COMPONENT = "InitializeComponent";
        private const string            SWITCH_STATEMENT = INDENT12 + "switch (" + CONNECTIONID + ")\r\n" + INDENT12 + "{";
        private const string            BREAK_STATEMENT = INDENT12 + "break;";
        private const string            CASE_STATEMENT = INDENT12 + "case ";
        private const string            ENDCURLY = "}";
        private const string            COLON = ":";
        private const string            RESOURCE_LOCATER = "resourceLocater";
        private const string            LOADCOMPONENT = "LoadComponent";
        private const string            SETTERS = "Setters";
        private const string            SummaryStartTag = @"<summary>";
        private const string            SummaryEndTag = @"</summary>";
        internal const string           ADD = "Add";
        internal const string           HANDLER = "Handler";

        // Delimiters & Uri processing
        private const string            VER = "V";
        private const string            COMPONENT = "component";
        private const char              COMPONENT_DELIMITER = ';';
        private const string            FORWARDSLASH = "/";
        private const string            URISCHEME_PACK = "pack";
        private const string            PARENTFOLDER = @"..\";

        // For generating pragma checksum data
        private static HashAlgorithm s_hashAlgorithm;
        private static Guid s_hashGuid;

        private static readonly Guid s_hashSHA256Guid = new Guid(0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16);
        private static readonly Guid s_hashSHA1Guid = new Guid(0xff1816ec, 0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60);

        private static string s_generatedCode_ToolName;
        private static string s_generatedCode_ToolVersion;

#endregion Private Data
    }
}

