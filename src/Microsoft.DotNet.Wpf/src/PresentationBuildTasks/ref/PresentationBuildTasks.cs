namespace Microsoft.Build.Tasks.Windows
{
    public sealed partial class FileClassifier : Microsoft.Build.Utilities.Task
    {
        public FileClassifier() { }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] CLREmbeddedResource { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] CLRResourceFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] CLRSatelliteEmbeddedResource { get { throw null; } set { } }
        public string Culture { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] MainEmbeddedFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputType { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] SatelliteEmbeddedFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] SourceFiles { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class GenerateTemporaryTargetAssembly : Microsoft.Build.Utilities.Task
    {
        public GenerateTemporaryTargetAssembly() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string AssemblyName { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string CompileTargetName { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string CompileTypeName { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string CurrentProject { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] GeneratedCodeFiles { get { throw null; } set { } }
        public bool GenerateTemporaryTargetAssemblyDebuggingInformation { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string IntermediateOutputPath { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string MSBuildBinPath { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] ReferencePath { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string ReferencePathTypeName { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class MarkupCompilePass1 : Microsoft.Build.Utilities.Task
    {
        public MarkupCompilePass1() { }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] AllGeneratedFiles { get { throw null; } set { } }
        public bool AlwaysCompileMarkupFilesInSeparateDomain { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] ApplicationMarkup { get { throw null; } set { } }
        public string[] AssembliesGeneratedDuringBuild { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string AssemblyName { get { throw null; } set { } }
        public string AssemblyPublicKeyToken { get { throw null; } set { } }
        public string AssemblyVersion { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] ContentFiles { get { throw null; } set { } }
        public string DefineConstants { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] ExtraBuildControlFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] GeneratedBamlFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] GeneratedCodeFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] GeneratedLocalizationFiles { get { throw null; } set { } }
        public string HostInBrowser { get { throw null; } set { } }
        public bool IsRunningInVisualStudio { get { throw null; } set { } }
        public string[] KnownReferencePaths { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Language { get { throw null; } set { } }
        public string LanguageSourceExtension { get { throw null; } set { } }
        public string LocalizationDirectivesToLocFile { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputPath { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputType { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] PageMarkup { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] References { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public bool RequirePass2ForMainAssembly { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public bool RequirePass2ForSatelliteAssembly { get { throw null; } set { } }
        public string RootNamespace { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] SourceCodeFiles { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] SplashScreen { get { throw null; } set { } }
        public string UICulture { get { throw null; } set { } }
        public bool XamlDebuggingInformation { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class MarkupCompilePass2 : Microsoft.Build.Utilities.Task
    {
        public MarkupCompilePass2() { }
        public bool AlwaysCompileMarkupFilesInSeparateDomain { get { throw null; } set { } }
        public string[] AssembliesGeneratedDuringBuild { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string AssemblyName { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] GeneratedBaml { get { throw null; } set { } }
        public string[] KnownReferencePaths { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Language { get { throw null; } set { } }
        public string LocalizationDirectivesToLocFile { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputPath { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputType { get { throw null; } set { } }
        public Microsoft.Build.Framework.ITaskItem[] References { get { throw null; } set { } }
        public string RootNamespace { get { throw null; } set { } }
        public bool XamlDebuggingInformation { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class MergeLocalizationDirectives : Microsoft.Build.Utilities.Task
    {
        public MergeLocalizationDirectives() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] GeneratedLocalizationFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputFile { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class ResourcesGenerator : Microsoft.Build.Utilities.Task
    {
        public ResourcesGenerator() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string OutputPath { get { throw null; } set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] OutputResourcesFile { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] ResourceFiles { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class UidManager : Microsoft.Build.Utilities.Task
    {
        public UidManager() { }
        public string IntermediateDirectory { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] MarkupFiles { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Task { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
    public sealed partial class UpdateManifestForBrowserApplication : Microsoft.Build.Utilities.Task
    {
        public UpdateManifestForBrowserApplication() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] ApplicationManifest { get { throw null; } set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public bool HostInBrowser { get { throw null; } set { } }
        public override bool Execute() { throw null; }
    }
}
