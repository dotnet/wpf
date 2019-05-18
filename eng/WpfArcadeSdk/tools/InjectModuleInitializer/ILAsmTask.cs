using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WpfArcadeSdk.Build.Tasks
{
    /// <summary>
    /// Runs ILAsm from the path supplied on the assembly supplied.
    /// </summary>
    public class ILAsmTask : Task
    {
        /// <summary>
        /// The full path to ILAsm.exe
        /// </summary>
        [Required]
        public string ILAsm { get; set; }

        /// <summary>
        /// The source file to assemble
        /// </summary>
        [Required]
        public string SourceFile { get; set; }

        /// <summary>
        /// The output path
        /// </summary>
        [Required]
        public string Out { get; set; }

        /// <summary>
        /// /NOLOGO Don't type the logo
        ///</summary>
        public bool NoLogo { get; set; }

        /// <summary>
        /// /QUIET Don't report assembly progress 
        ///</summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// /NOAUTOINHERIT Disable inheriting from System.Object by default
        ///</summary>
        public bool NoAutoInherit { get; set; }

        /// <summary>
        /// /DLL Compile to.dll
        ///</summary>
        public bool Dll { get; set; }

        /// <summary>
        /// /EXE Compile to.exe(default)
        ///</summary>
        public bool Exe { get; set; }

        /// <summary>
        /// /PDB Create the PDB file without enabling debug info tracking
        ///</summary>
        public bool Pdb { get; set; }

        /// <summary>
        /// /APPCONTAINER Create an AppContainer exe or dll
        ///</summary>
        public bool AppContainer { get; set; }

        /// <summary>
        /// /DEBUG Disable JIT optimization, create PDB file, use sequence points from PDB
        ///</summary>
        public bool Debug { get; set; }

        /// <summary>
        /// /DEBUG=IMPL Disable JIT optimization, create PDB file, use implicit sequence points
        ///</summary>
        public bool DebugDisableJitOptimization { get; set; }


        /// <summary>
        /// / DEBUG = OPT Enable JIT optimization, create PDB file, use implicit sequence points 
        ///</summary>
        public bool DebugEnableJitOptimization { get; set; }


        /// <summary>
        /// / OPTIMIZE Optimize long instructions to short
        ///</summary>
        public bool Optimize { get; set; }


        /// <summary>
        /// / FOLD Fold the identical method bodies into one
        ///</summary>
        public bool Fold { get; set; }


        /// <summary>
        /// / CLOCK Measure and report compilation times
        ///</summary>
        public bool Clock { get; set; }


        /// <summary>
        /// / RESOURCE =< res_file > Link the specified resource file(*.res) into resulting.exe or.dll 
        ///</summary>
        public string Resource { get; set; }


        /// <summary>
        /// / CVRES =< path_to_file > Set path to cvtres tool: /CVR= cvtres.exe / CVR = tool\cvtres.cmd /CVR= D:\tool\
        ///</summary>
        public string CvRes { get; set; }

        /// <summary>
        /// /KEY =< keyfile > Compile with strong signature
        ///     (<keyfile> contains private key)
        /// </summary>
        public string KeyFile { get; set; }

        /// <summary>
        /// /KEY=@<keysource>       Compile with strong signature
        ///     (<keysource> is the private key source name)
        /// </summary>
        public string KeySource { get; set; }

        /// <summary>
        /// /INCLUDE=<path>         Set path to search for #include'd files
        ///</summary>
        public string Include { get; set; }

        /// <summary>
        /// /SUBSYSTEM=<int>        Set Subsystem value in the NT Optional header
        ///</summary>
        public string SubSystem { get; set; }

        /// <summary>
        /// /SSVER=<int>.<int>      Set Subsystem version number in the NT Optional header
        ///</summary>
        public string SubSystemVersion { get; set; }

        /// <summary>
        /// /FLAGS=<int>            Set CLR ImageFlags value in the CLR header
        ///</summary>
        public string Flags { get; set; }

        /// <summary>
        /// /ALIGNMENT=<int>        Set FileAlignment value in the NT Optional header
        ///</summary>
        public string Alignment { get; set; }

        /// <summary>
        /// /BASE=<int>             Set ImageBase value in the NT Optional header(max 2GB for 32-bit images)
        ///</summary>
        public string Base { get; set; }

        /// <summary>
        /// /STACK=<int>            Set SizeOfStackReserve value in the NT Optional header
        ///</summary>
        public string Stack { get; set; }

        /// <summary>
        /// /MDV=<version_string>   Set Metadata version string
        ///</summary>
        public string MetadataVersion { get; set; }

        /// <summary>
        /// /MSV=<int>.<int>        Set Metadata stream version(<major>.<minor>)
        ///</summary>
        public string MetadataStreamVersion { get; set; }

        /// <summary>
        /// /PE64 Create a 64bit image(PE32+)
        ///</summary>
        public string PE64 { get; set; }

        /// <summary>
        /// /HIGHENTROPYVA Set High Entropy Virtual Address capable PE32+ images(default for /APPCONTAINER)
        ///</summary>
        public string HighEntropyVirtualAddress { get; set; }

        /// <summary>
        /// /NOCORSTUB Suppress generation of CORExeMain stub
        ///</summary>
        public string NoCorStub { get; set; }

        /// <summary>
        /// /STRIPRELOC Indicate that no base relocations are needed
        ///</summary>
        public string StripReLoc { get; set; }

        /// <summary>
        /// /ITANIUM Target processor: Intel Itanium
        ///</summary>
        public string Itanium { get; set; }

        /// <summary>
        /// /X64 Target processor: 64bit AMD processor
        ///</summary>
        public string X64 { get; set; }

        /// <summary>
        /// /ARM Target processor: ARM processor
        ///</summary>
        public string Arm { get; set; }

        /// <summary>
        /// /32BITPREFERRED Create a 32BitPreferred image(PE32)
        ///</summary>
        public string Prefer32Bit { get; set; }

        /// <summary>
        /// /ENC=<file>             Create Edit-and-Continue deltas from specified source file
        ///</summary>
        public string ENC { get; set; }

        public override bool Execute()
        {
            try
            {
                string commandLine = "/OUT=" + Out;
                if (NoLogo) commandLine += " /NOLOGO";
                if (Quiet) commandLine += " /QUIET";
                if (NoAutoInherit) commandLine += " /NOAUTOINHERIT";
                if (Dll) commandLine += " /DLL";
                if (Exe) commandLine += " /EXE";
                if (Pdb) commandLine += " /PDB";
                if (AppContainer) commandLine += " /APPCONTAINER";
                if (Debug) commandLine += " /DEBUG";
                if (DebugDisableJitOptimization) commandLine += " /DEBUG=IMPL";
                if (DebugEnableJitOptimization) commandLine += " /DEBUG=OPT";
                if (Optimize) commandLine += " /OPTIMIZE";
                if (Fold) commandLine += " /FOLD";
                if (Clock) commandLine += " /CLOCK";
                if (!string.IsNullOrEmpty(Resource)) commandLine += " /RESOURCE=" + Resource;
                if (!string.IsNullOrEmpty(CvRes)) commandLine += " /CVRES=" + CvRes;
                if (!string.IsNullOrEmpty(KeyFile)) commandLine += " /KeyFile=" + KeyFile;
                if (!string.IsNullOrEmpty(KeySource)) commandLine += " /CVRES=@" + KeySource;
                if (!string.IsNullOrEmpty(Include)) commandLine += " /INCLUDE=" + Include;

                int subSystem = -1;
                if (!string.IsNullOrEmpty(SubSystem) && Int32.TryParse(SubSystem, out subSystem)) commandLine += " /SUBSYSTEM=" + subSystem;
                if (!string.IsNullOrEmpty(SubSystemVersion)) commandLine += " /SUBSYSTEMVERSION=" + SubSystemVersion;

                int flags = -1;
                if (!string.IsNullOrEmpty(Flags) && Int32.TryParse(Flags, out flags)) commandLine += " /FLAGS=" + flags;

                int alignment = -1;
                if (!string.IsNullOrEmpty(Alignment) && Int32.TryParse(Alignment, out alignment)) commandLine += " /ALIGNMENT=" + alignment;

                int @base = -1;
                if (!string.IsNullOrEmpty(Base) && Int32.TryParse(Base, out @base)) commandLine += " /BASE=" + @base;

                int stack = -1;
                if (!string.IsNullOrEmpty(Stack) && Int32.TryParse(Stack, out stack)) commandLine += " /STACK=" + stack;

                if (!string.IsNullOrEmpty(MetadataVersion)) commandLine += " /MDV=" + MetadataVersion;
                if (!string.IsNullOrEmpty(MetadataStreamVersion)) commandLine += " /MSV=" + MetadataStreamVersion;

                bool pe64 = false;
                if (!string.IsNullOrEmpty(PE64) && bool.TryParse(PE64, out pe64)) commandLine += " /PE64=" + pe64;

                bool highEntropyVA = false;
                if (!string.IsNullOrEmpty(HighEntropyVirtualAddress) && bool.TryParse(HighEntropyVirtualAddress, out highEntropyVA)) commandLine += " /HIGHENTROPYVA=" + highEntropyVA;

                bool noCorStub = false;
                if (!string.IsNullOrEmpty(NoCorStub) && bool.TryParse(NoCorStub, out noCorStub)) commandLine += " /NOCORSTUB=" + noCorStub;

                bool stripreloc = false;
                if (!string.IsNullOrEmpty(StripReLoc) && bool.TryParse(StripReLoc, out stripreloc)) commandLine += " /STRIPRELOC=" + stripreloc;

                bool itanium = false;
                if (!string.IsNullOrEmpty(Itanium) && bool.TryParse(Itanium, out itanium)) commandLine += " /ITANIUM=" + itanium;

                bool x64 = false;
                if (!string.IsNullOrEmpty(X64) && bool.TryParse(X64, out x64)) commandLine += " /X64=" + x64;

                bool arm = false;
                if (!string.IsNullOrEmpty(Arm) && bool.TryParse(Arm, out arm)) commandLine += " /ARM=" + arm;

                bool prefer32bit = false;
                if (!string.IsNullOrEmpty(Prefer32Bit) && bool.TryParse(Prefer32Bit, out prefer32bit)) commandLine += " /32BITPREFERRED=" + prefer32bit;

                if (!string.IsNullOrEmpty(ENC)) commandLine += " /ENC=" + ENC;


                ProcessStartInfo startInfo = new ProcessStartInfo(ILAsm);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = SourceFile + " " + commandLine;

                Log.LogMessage("Starting process: " + startInfo.FileName + " " + startInfo.Arguments);

                Process.Start(startInfo).WaitForExit();
                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString() + e.StackTrace);
                return false;
            }
        }
    }
}
