// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Test.Execution.StateManagement.GacUtilities
{
    /// <summary>
    /// GAC cache operations for use by Gac State Implementation.
    /// </summary>
    [ComVisible(false)]
    static internal class AssemblyCache
    {

        /// <summary>
        /// Check for presence of Assembly
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static string QueryAssembly(string name)
        {
            return AssemblyCache.QueryAssemblyInfo(name);
        }

        /// <summary>
        /// Installs an assembly into the GAC. Adds a reference to the installing applicaiton.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly to be installed</param>		
        /// <param name="flags">see AssemblyCommitFlags</param>
        internal static void InstallAssembly(String assemblyPath, AssemblyCommitFlags flags)
        {
            IAssemblyCache ac = null;
            int hr = 0;
            hr = GacUtils.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.InstallAssembly((int)flags, assemblyPath, null);
            }
            if (hr < 0)
            {
                GacUtils.ThrowMarshalledException(hr);
            }
        }

        /// <summary>
        /// Force remove assembly from GAC, without worrying about missing files.
        /// </summary>
        /// <param name="filename"></param>
        internal static void UninstallAssemblySilently(string filename)
        {
            try
            {
                UninstallAssembly(filename);
            }
            catch (FileNotFoundException )
            {
            }
        }

        /// <summary>
        /// Check the GAC for an assembly with the given identity and uninstall if necessary.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True if assembly was uninstalled, false otherwise.</returns>
        internal static bool UninstallAssembly(string fileName)
        {
            //Get the full assembly name including processor architecure
            AssemblyName asmName = AssemblyName.GetAssemblyName(fileName);
            string fullAsmName = asmName.FullName;
            if (asmName.ProcessorArchitecture != ProcessorArchitecture.None)
            {
                fullAsmName += ", processorArchitecture=" + asmName.ProcessorArchitecture.ToString().ToUpperInvariant();
            }
            else
            {
                // GetAssemblyName is not currently returning an assembly name with arch.
                // Since we only GAC MSIL stuff, this should work 100% fine until the bug fix integrates.
                // If this fails, it will be a silent failure of un-gac'ing, which exists today already for 100% of cases.
                fullAsmName += ", processorArchitecture=MSIL";
            }

            string gacedFileName = AssemblyCache.QueryAssemblyInfo(fullAsmName);
            if (!string.IsNullOrEmpty(gacedFileName))
            {
                //ungac the assembly
                AssemblyCacheUninstallDisposition disp = AssemblyCache.UninstallAssemblyImplementation(fullAsmName);
                return true;
            }
            return false;
        }



        /// <summary>
        /// Uninstalls the assembly, using the application as a reference.
        /// AssemblyName has to be fully specified name.
        /// A.k.a, for v1.0/v1.1 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx".
        /// For v2.0 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx, ProcessorArchitecture=xx".
        /// If assemblyName is not fully specified, a random matching assembly will be uninstalled. 
        /// </summary>
        /// <param name="assemblyName"></param>		
        private static AssemblyCacheUninstallDisposition UninstallAssemblyImplementation(String assemblyName)
        {
            AssemblyCacheUninstallDisposition dispResult = AssemblyCacheUninstallDisposition.Uninstalled;
            IAssemblyCache ac = null;

            int hr = GacUtils.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.UninstallAssembly(0, assemblyName, null, out dispResult);
            }

            if (hr < 0)
            {
                GacUtils.ThrowMarshalledException(hr);
            }

            return dispResult;
        }

        /// <summary>
        /// Gets information about the assembly.
        /// See comments in UninstallAssembly for specifying the assembly name.
        /// </summary>
        /// <param name="assemblyName"></param>        
        /// <returns></returns>
        private static string QueryAssemblyInfo(String assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentException("Invalid name", "assemblyName");
            }

            AssemblyInfo aInfo = new AssemblyInfo();

            aInfo.cchBuf = 1024;
            // Get a string with the desired length
            aInfo.currentAssemblyPath = new string('\0', aInfo.cchBuf);
            IAssemblyCache ac = null;
            int hr = GacUtils.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.QueryAssemblyInfo((int)QueryAssemblyInfoFlags.DEFAULT, assemblyName, ref aInfo);
            }

            if (hr == -2147024894) //File not found - This is a common error which need not be represented as an exception(ruins debug experience, also).
            {
                return null;
            }
            
            else if (hr < 0)
            {
                GacUtils.ThrowMarshalledException(hr);
            }

            return aInfo.currentAssemblyPath;
        }

    }

    /// <summary>
    /// Assembly Commit Flags. Used when invoking the 
    /// IAssemblyCache::InstallAssembly method.
    /// At most one bit can be specified.
    /// </summary>
    internal enum AssemblyCommitFlags
    {
        /// <summary>
        /// Refresh:
        /// If the assembly is already installed in the GAC and the file version 
        /// numbers of the assembly being installed are the same or later, the files are replaced.
        /// The original code called this Default but the docs call it refresh
        /// </summary>
        Refresh = 1, // also called Default
        /// <summary>
        /// The files of an existing assembly are overwritten regardless of their version number.
        /// </summary>
        Force = 2

        //NOTE: According to KB docs, the values ought to be 1 and 2 respectively
    }// enum AssemblyCommitFlags

    /// <summary>
    /// Results of uninstalling a GAC component. Used when calling 
    /// IAssemblyCache::UninstallAssembly 
    /// </summary>
    internal enum AssemblyCacheUninstallDisposition
    {
        /// <summary>
        /// ???
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The assembly files have been removed from the GAC.
        /// </summary>
        Uninstalled = 1,
        /// <summary>
        /// An application is using the assembly. 
        /// This value is returned on Microsoft Windows 95 and Microsoft Windows 98
        /// </summary>
        StillInUse = 2,
        /// <summary>
        /// The assembly does not exist in the GAC
        /// </summary>
        AlreadyUninstalled = 3,
        /// <summary>
        ///  Not used
        /// </summary>
        DeletePending = 4,
        /// <summary>
        /// The assembly has not been removed from the GAC because another application reference exists
        /// </summary>
        HasInstallReference = 5,
        /// <summary>
        /// The reference that is specified in pRefData is not found in the GAC
        /// </summary>
        ReferenceNotFound = 6
    } //    enum AssemblyCacheUninstallDisposition

    /// <summary>
    /// Controls how the GAC is enumerated.
    /// </summary>
    [Flags]
    internal enum AssemblyCacheFlags
    {
        /// <summary>
        /// Enumerates the cache of precompiled assemblies by using Ngen.exe.
        /// </summary>
        ZAP = 0x1,
        /// <summary>
        /// Enumerates the GAC
        /// </summary>
        GAC = 2,
        /// <summary>
        /// Enumerates the assemblies that have been downloaded on-demand or that have been shadow-copied
        /// </summary>
        DOWNLOAD = 0x4,
    } //    AssemblyCacheFlags

    /// <summary>
    /// Flags enumeration for method CreateAssemblyNameObject. This method
    /// obtains an instance of IAssemblyName.
    /// </summary>
    internal enum CreateAssemblyNameObjectFlags
    {
        /// <summary>
        /// Unknown - externally undocumented.
        /// </summary>
        CANOF_DEFAULT = 0,
        /// <summary>
        /// If this flag is specified, the szAssemblyName parameter is a full 
        /// assembly name and is parsed to the individual properties. If the flag is 
        /// not specified, szAssemblyName is the "Name" portion of the assembly name .
        /// </summary>
        CANOF_PARSE_DISPLAY_NAME = 1,
        /// <summary>
        /// If this flag is specified, certain properties, such as processor architecture, 
        /// are set to their default values. What this actually means is not documented.
        /// </summary>
        CANOF_SET_DEFAULT_VALUES = 2
    }

    /// <summary>
    /// Controls how the assembly's name is returned from 
    /// a call to IAssemblyName.GetDisplayName
    /// </summary>
    [Flags]
    internal enum AssemblyNameDisplayFlags
    {
        /// <summary>
        /// Includes the version number as part of the display name
        /// </summary>
        VERSION = 0x01,
        /// <summary>
        /// Includes the Culture
        /// </summary>
        CULTURE = 0x02,
        /// <summary>
        /// Includes the internal key Token
        /// </summary>
        PUBLIC_KEY_TOKEN = 0x04,

        /// <summary>
        /// Includes the internal key 
        /// </summary>
        PUBLIC_KEY = 0x08,

        /// <summary>
        ///  Includes the custom part of the assembly name.
        /// </summary>
        CUSTOM = 0x10,

        /// <summary>
        /// Includes the processor architecture
        /// </summary>
        PROCESSORARCHITECTURE = 0x20,

        /// <summary>
        /// Includes the language ID.
        /// </summary>
        LANGUAGE_ID = 0x40,

        /// <summary>
        /// retargetable name (asm name)
        /// </summary>
        RETARGETABLE = 0x80,
        // This enum will change in the future to include 
        // more attributes.

        /// <summary>
        /// all combos
        /// </summary>
        ALL = VERSION
            | CULTURE
            | PUBLIC_KEY_TOKEN
            | PROCESSORARCHITECTURE
            | RETARGETABLE,

        /// <summary>
        /// All possible flags
        /// </summary>
        COMPLETE = VERSION
            | CULTURE
            | PUBLIC_KEY_TOKEN
            | PROCESSORARCHITECTURE
            | RETARGETABLE
            | PUBLIC_KEY
            | CUSTOM
            | LANGUAGE_ID,
    } //    enum AssemblyNameDisplayFlags

    /// <summary>
    /// Flags used with AssemblyInfo struct
    /// </summary>
    [Flags]
    internal enum AssemblyInfoFlag
    {
        /// <summary>
        ///  Indicates that the assembly is actually installed. 
        /// Always set in current version of the .NET Framework
        /// </summary>
        INSTALLED = 0x1,
        /// <summary>
        /// Never set in the current version of the .NET Framework.
        /// This definition is externally undocumented.
        /// </summary>
        PAYLOAD_RESIDENT = 0x2,
    }

    /// <summary>
    /// Determines how the routine IAssemblyCache::QueryAssemblyInfo works
    /// </summary>
    [Flags]
    internal enum QueryAssemblyInfoFlags
    {
        /// <summary>
        /// Provides default data (path info only).
        /// </summary>
        DEFAULT = 0,
        /// <summary>
        /// Performs validation of the files in the GAC against the assembly manifest, 
        /// including hash verification and strong name signature verification.
        /// </summary>
        VALIDATE = 0x1,
        /// <summary>
        ///  Returns the size of all files in the assembly (disk footprint). 
        /// If this is not specified, the ASSEMBLY_INFO::uliAssemblySizeInKB field is not modified.
        /// </summary>
        GETSIZE = 0x2,
    } //    QueryAssemblyInfoFlags

    /// <summary>
    /// Controls how IAssemblyName::IsEqual behaves. The value consists of
    /// one or more bits as defined here.
    /// </summary>
    [Flags]
    internal enum AssemblyCompareFlags
    {
        /// <summary>
        /// The name
        /// </summary>
        NAME = 0x01,
        /// <summary>
        /// 
        /// </summary>
        MAJOR_VERION = 0x2,
        /// <summary>
        /// 
        /// </summary>
        MINOR_VERSION = 0x4,
        /// <summary>
        /// 
        /// </summary>
        BUILD_NUMBER = 0x8,
        /// <summary>
        /// 
        /// </summary>
        REVISION_NUMBER = 0x10,
        /// <summary>
        /// 
        /// </summary>
        PUBLIC_KEY_TOKEN = 0x20,
        /// <summary>
        /// 
        /// </summary>
        CULTURE = 0x40,
        /// <summary>
        /// 
        /// </summary>
        CUSTOM = 0x80,
        /// <summary>
        /// 
        /// </summary>
        ALL = NAME | MAJOR_VERION | MINOR_VERSION |
            BUILD_NUMBER | REVISION_NUMBER | PUBLIC_KEY_TOKEN |
            CULTURE | CUSTOM,
        /// <summary>
        /// For strongly named assemblies, ASM_CMPF_DEFAULT==ASM_CMPF_ALL.
        /// For simply named assemblies, this is also true. However, when
        /// performing IAssemblyName::IsEqual, the build number/revision 
        /// number will be removed from the comparison.
        /// </summary>
        DEFAULT = 0x100,
    } //    AssemblyCompareFlags



    /// <summary>
    /// The components that make up a trace reference. 
    /// The guid specifies the type of trace reference used.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Needed as struct argument")]
    internal class InstallReference
    {
        /// <summary>
        /// Provide default internal constructor so it can be serialized across
        /// a web service boundary.
        /// </summary>
        internal InstallReference()
            : this(Guid.Empty, string.Empty, string.Empty)
        {
        }



        /// <summary>
        /// Creates an install reference object that can be passed to the 
        /// GAC utilities.
        /// </summary>
        /// <param name="guid">Identifies the trace reference scheme used</param>
        /// <param name="id">depends on the scheme used.</param>
        /// <param name="data">can be used as an informational description</param>
        internal InstallReference(Guid guid, String id, String data)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (data == null)
                throw new ArgumentNullException("data");
            cbSize = (int)(2 * IntPtr.Size + 16 + (id.Length + data.Length) * 2);
            flags = 0;
            if (flags == 0) // quiet compiler warning
            {
            }
            guidScheme = guid;
            identifier = id;
            nonCannonicalData = data;
        }

        /// <summary>
        /// The scheme to use. This can be OPAQUE, UNINSTALL_KEY, or FILEPATH.
        /// This must be one of the predefined guids.
        /// </summary>
        /// <value></value>
        internal Guid GuidScheme
        {
            get { return guidScheme; }
            set { guidScheme = value; } // needed for xml deserializer
        }

        /// <summary>
        /// The size of this struct in bytes
        /// </summary>
        /// <value></value>
        internal int Size
        {
            get { return cbSize; }
            set { cbSize = value; } // needed for xml deserializer
        }

        /// <summary>
        /// A unique string that identifies the application that installed
        /// </summary>
        /// <value></value>
        internal string Identifier
        {
            get { return identifier; }
            set { identifier = value; } // needed for xml deserializer
        }

        /// <summary>
        /// A string that is only understood by the entity that adds the 
        /// reference. The GAC only stores this string.
        /// It is supposed to be a description string.
        /// </summary>
        internal String Description
        {
            get { return nonCannonicalData; }
            set { nonCannonicalData = value; } // needed for xml deserializer
        }

        /// <summary>
        /// Size of structure in bytes
        /// </summary>
        private int cbSize = 0;
        /// <summary>
        /// Reserved = must be set to zero
        /// </summary>
        private int flags = 0;
        /// <summary>
        /// The entity that adds the reference. Possible values are:
        /// FUSION_REFCOUNT_MSI_GUID: 
        ///             The assembly is referenced by an application that has been installed 
        ///             by using Windows Installer. The szIdentifier field is set to MSI, and 
        ///             szNonCannonicalData is set to Windows Installer. This scheme must only be used 
        ///             by Windows Installer itself
        /// FUSION_REFCOUNT_UNINSTALL_SUBKEY_GUID - 
        ///             The assembly is referenced by an application that appears in Add/Remove 
        ///             Programs. The szIdentifier field is the token that is used to register the 
        ///             application with Add/Remove programs
        /// FUSION_REFCOUNT_FILEPATH_GUID - 
        ///             The assembly is referenced by an application that is represented by a file 
        ///             in the file system. The szIdentifier field is the path to this file.
        /// FUSION_REFCOUNT_OPAQUE_STRING_GUID - 
        ///             The assembly is referenced by an application that is only represented by 
        ///             an opaque string. The szIdentifier is this opaque string. The GAC does not 
        ///             perform existence checking for opaque references when you remove this.
        /// </summary>
        private Guid guidScheme = Guid.Empty;
        /// <summary>
        /// A unique string that identifies the application that installed
        /// the assembly
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        private String identifier = string.Empty;

        /// <summary>
        /// A string that is only understood by the entity that adds the 
        /// reference. The GAC only stores this string.
        /// It is intended to be a description.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        private String nonCannonicalData = string.Empty;
    } //    InstallReference


    /// <summary>
    /// Represents information about an assembly in the assembly cache.
    /// </summary>
    /// <remarks>
    /// This was originally declared as a struct. This created problems when
    /// attempting to pass this over a web service because structs do not
    /// support default constructors which is required by the deserializer.
    /// When I changed it to a class the interop stopped working.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfo
    {
        /// <summary>
        /// Size of the structure in bytes. Permits additions to the structure in future version of the .NET Framework.
        /// </summary>
        internal int cbAssemblyInfo; // size of this structure for future expansion
        /// <summary>
        /// Indicates one or more of the ASSEMBLYINFO_FLAG_* bits.
        /// </summary>
        internal int assemblyFlags;
        /// <summary>
        /// The size of the files that make up the assembly in kilobytes (KB).
        /// </summary>
        internal long assemblySizeInKB;
        /// <summary>
        /// A pointer to a string buffer that holds the current path of the directory that 
        /// contains the files that make up the assembly. The path must end with a zero.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal String currentAssemblyPath;
        /// <summary>
        /// Size of the buffer that the pszCurrentAssemblyPathBug field points to.
        /// </summary>
        internal int cchBuf; // size of path buf.
    } //    struct AssemblyInfo

    //-------------------------------------------------------------
    // Interfaces defined by fusion
    //-------------------------------------------------------------

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
        /// <summary>
        /// Uninstalls an assembly from the GAC.
        /// </summary>
        /// <param name="flags">No flags are defined - must be zero</param>
        /// <param name="assemblyName">Name of the assembly - must be zero terminated. This must
        /// be a complete name - partial names are not allowed.</param>
        /// <param name="refData">A trace reference. This can be null but it is not recommended.</param>
        /// <param name="disposition">the outcome, or null if it is not needed</param>
        /// <returns></returns>
        [PreserveSig()]
        int UninstallAssembly(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)]
							String assemblyName,
                            InstallReference refData,
                            out AssemblyCacheUninstallDisposition disposition);

        /// <summary>
        /// Get some basic info about an assembly
        /// </summary>
        /// <param name="flags">See the QueryAssemblyInfoFlag enumerations</param>
        /// <param name="assemblyName">Complete name of the assembly</param>
        /// <param name="assemblyInfo">the output</param>
        /// <returns></returns>
        [PreserveSig()]
        int QueryAssemblyInfo(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)] 
							String assemblyName,
                            ref AssemblyInfo assemblyInfo);
        [PreserveSig()]
        int Reserved(
                            int flags,
                            IntPtr pvReserved,
                            out Object ppAsmItem,
                            [MarshalAs(UnmanagedType.LPWStr)] 
							String assemblyName);
        [PreserveSig()]
        int Reserved(out Object ppAsmScavenger);

        /// <summary>
        /// Installs an assembly into the GAC
        /// </summary>
        /// <param name="flags">Whether to force the installation or not. See AssemblyCommitFlags</param>
        /// <param name="assemblyFilePath">Path to assembly to add to GAC</param>
        /// <param name="refData">A trace reference</param>
        /// <returns></returns>
        [PreserveSig()]
        int InstallAssembly(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)] 
							String assemblyFilePath,
                            InstallReference refData);
    }// IAssemblyCache  

    /// <summary>
    /// Properties associated with an assembly. This includes a predetermined 
    /// set of name-value pairs. An instance of this is obtained by 
    /// calling the CreateAssemblyName API
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
    internal interface IAssemblyName
    {
        /// <summary>
        /// Adds a name-value pair to the assembly name, or, if a name-value pair with the 
        /// same name already exists, modifies or deletes the value of a name-value pair. 
        /// </summary>
        /// <param name="PropertyId">The ID that represents the name part of the name-value pair 
        /// that is to be added or to be modified. Valid property IDs are defined in the ASM_NAME 
        /// enumeration</param>
        /// <param name="pvProperty">A pointer to a buffer that contains the value of the property</param>
        /// <param name="cbProperty">The length of the pvProperty buffer in bytes. If 
        /// cbProperty is zero, the name-value pair is removed from the assembly name</param>
        /// <returns></returns>
        [PreserveSig()]
        int SetProperty(
                        int PropertyId,
                        IntPtr pvProperty,
                        int cbProperty);

        /// <summary>
        /// Retrieves the value of a name-value pair in the assembly name that specifies the name. 
        /// </summary>
        /// <param name="PropertyId">The ID that represents the name of the name-value pair 
        /// whose value is to be retrieved. Specified property IDs are defined in the 
        /// ASM_NAME enumeration</param>
        /// <param name="pvProperty">A pointer to a buffer that is to contain the value of the property.</param>
        /// <param name="pcbProperty">The length of the pvProperty buffer, in bytes.</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetProperty(
                        int PropertyId,
                        IntPtr pvProperty,
                        ref int pcbProperty);

        /// <summary>
        /// Freezes an assembly name. Additional calls to IAssemblyName::SetProperty 
        /// are unsuccessful after this method has been called. 
        /// </summary>
        /// <returns></returns>
        [PreserveSig()]
        int Finalize();

        /// <summary>
        ///  Returns a string representation of the assembly name. 
        /// </summary>
        /// <param name="pDisplayName">A pointer to a buffer that is to contain the display name. The display name is returned in Unicode.</param>
        /// <param name="pccDisplayName">The size of the buffer in characters (on input). The length of the returned display name (on return).</param>
        /// <param name="displayFlags">One or more of the bits defined in the 
        /// AssemblyNameDisplayFlags enumeration</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetDisplayName(
                        StringBuilder pDisplayName,
                        ref int pccDisplayName,
                        int displayFlags);

        [PreserveSig()]
        int Reserved(ref Guid guid,
                Object obj1,
                Object obj2,
                String string1,
                Int64 llFlags,
                IntPtr pvReserved,
                int cbReserved,
                out IntPtr ppv);

        /// <summary>
        /// Gets the name part of an assembly name
        /// </summary>
        /// <param name="pccBuffer">Size of the pwszName buffer (on input). 
        /// Length of the name (on return).</param>
        /// <param name="pwzName">Pointer to the buffer that is to contain the name 
        /// part of the assembly name</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetName(
                        ref int pccBuffer,
                        StringBuilder pwzName);

        /// <summary>
        ///  returns the version part of the assembly name
        /// </summary>
        /// <param name="versionHi">Pointer to a DWORD that contains the upper 32 bits of the version number</param>
        /// <param name="versionLow">Pointer to a DWORD that contain the lower 32 bits of the version number</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetVersion(
                        out int versionHi,
                        out int versionLow);

        /// <summary>
        /// compares the assembly name to another assembly names
        /// </summary>
        /// <param name="pAsmName">The assembly name to compare to</param>
        /// <param name="cmpFlags">Indicates which part of the assembly name to use in the comparison.
        /// See AssemblyCompareFlags enumeration for details.</param>
        /// <returns>S_OK when it matches, otherwise S_FALSE</returns>
        [PreserveSig()]
        int IsEqual(
                        IAssemblyName pAsmName,
                        int cmpFlags);

        /// <summary>
        /// Creates a copy of an IAssemblyName
        /// </summary>
        /// <param name="pAsmName"></param>
        /// <returns></returns>
        [PreserveSig()]
        int Clone(out IAssemblyName pAsmName);
    }// IAssemblyName

    /// <summary>
    /// Enumerates assemblies in GAC. To create one call
    /// the CreateAssemblyNameObject API
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
    internal interface IAssemblyEnum
    {
        /// <summary>
        ///  Enumerates the assemblies in the GAC.
        /// </summary>
        /// <param name="pvReserved">Must be null</param>
        /// <param name="ppName">ptr to location to receive interface ptr to next asm</param>
        /// <param name="flags">Must be zero</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetNextAssembly(
                        IntPtr pvReserved,
                        out IAssemblyName ppName,
                        int flags);
        [PreserveSig()]
        int Reset();
        [PreserveSig()]
        int Clone(out IAssemblyEnum ppEnum);
    }// IAssemblyEnum

    /// <summary>
    /// Identifies the scheme used for trace references. These will be checked when
    /// the assembly is uninstalled from the GAC. When an assembly is installed into the
    /// GAC with a trace reference it must be uninstalled using the exact same trace reference.
    /// If there are more trace references then the TR will be removed but the assembly will be
    /// left in the GAC.
    /// Only when there are no more trace references will the assembly be removed from the GAC.
    /// </summary>
    [ComVisible(false)]
    static internal class InstallReferenceGuid
    {

        /// <summary>
        /// The id is interpreted as the application's uninstall id (what you see when you bring
        /// up the Add/Remove applications). If the uninstall ID cannot be found it assumes the application
        /// has been uninstalled and the trace reference will be removed.
        /// </summary>
        internal readonly static Guid UninstallSubkeyGuid = new Guid("8cedc215-ac4b-488b-93c0-a50a49cb2fb8");
        /// <summary>
        /// The id is specified to be a file path. If the file does not exist then the 
        /// trace reference will be removed.
        /// </summary>
        internal readonly static Guid FilePathGuid = new Guid("b02f9d65-fb77-4f7a-afa5-b391309f11c9");
        /// <summary>
        /// The application specificies the ID and description. The id can be anything and the data 
        /// is an informational description.
        /// </summary>
        internal readonly static Guid OpaqueStringGuid = new Guid("2ec93463-b0c3-45e1-8364-327e96aea856");

        /// <summary>
        /// This is for the use of MSI only - do not use this.
        /// </summary>
        internal readonly static Guid MSIGuid = new Guid("25df0fc1-7f97-4070-add7-4b13bbfd7cb8");

        /// <summary>
        /// Don't yet know what this is used for (9-2004)
        /// </summary>
        internal readonly static Guid OsInstallGuid = new Guid("d16d444c-56d8-11d5-882d-0080c847b195");
    }

    /// <summary>
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("582dac66-e678-449f-aba6-6faaec8a9394")]
    internal interface IInstallReferenceItem
    {
        /// <summary>
        /// Returns an InstallReference (FUSION_INSTALL_REFERENCE) structure.
        /// </summary>
        /// <param name="ppRefData">A pointer to a FUSION_INSTALL_REFERENCE structure. 
        /// The memory is allocated by the GetReference method and is freed when 
        /// IInstallReferenceItem is released. Callers must not hold a reference to this 
        /// buffer after the IInstallReferenceItem object is released.
        /// </param>
        /// <param name="flags">Must be zero</param>
        /// <param name="pvReserved">Must be null</param>
        /// <returns>HRESULT</returns>
        [PreserveSig()]
        int GetReference(
            //out InstallReference ppRefData,   // This cannot be marshaled directly - must use IntPtr
            out IntPtr ppRefData,
            int flags,
            IntPtr pvReserved);

    } //    interface IIstallReferenceItem

    /// <summary>
    /// Enumerates all references that are set on an assembly in the GAC.
    /// NOTE: References that belong to the assembly are locked for changes while those references are being enumerated.
    /// 
    /// To obtain an instance of the CreateInstallReferenceEnum API, call the 
    /// CreateInstallReferenceEnum API, as follows: 
    /// STDAPI CreateInstallReferenceEnum(IInstallReferenceEnum **ppRefEnum, IAssemblyName *pName, DWORD dwFlags, LPVOID pvReserved);
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f")]
    internal interface IInstallReferenceEnum
    {
        /// <summary>
        /// Returns the next reference information for an assembly.
        /// </summary>
        /// <param name="pvReserved">Must be null</param>
        /// <param name="ppRefItem">ptr to location to receive interface ptr to next install reference</param>
        /// <param name="flags">Must be zero</param>
        /// <returns></returns>
        [PreserveSig()]
        int GetNextInstallReferenceItem(
                         out IInstallReferenceItem ppRefItem,
                         int flags,
                         IntPtr pvReserved);

    }

    /// <summary>
    /// GAC-related utilities
    /// </summary>
    internal static class GacUtils
    {
        /// <summary>
        /// A map from HRESULTS to a .net type. Reference file
        /// CorError.h for a complete list.
        /// </summary>
        internal enum GACErrorCodes : uint
        {
            /// <summary>
            ///   This error indicates that during a bind, the assembly reference did 
            /// not match the definition of the assembly found. For strongly-named 
            /// assemblies, if any of the tuple (name, version, internal key token, 
            /// culture) do not match when binding to an assembly using 
            /// Assembly.Load (or implicitly through type resolution), you will get this error. 
            /// For simply-named assemblies mismatches in the tuple (name, culture) 
            /// will cause a ref-def mismatch.
            /// </summary>
            FUSION_E_REF_DEF_MISMATCH = 0x80131040,
            /// <summary>
            /// Private (or "simply named") assemblies are restricted to live only under the 
            /// appbase directory for a given domain. This restriction is necessary to prevent 
            /// unintended sharing of the assembly. Since the identity of simply-named is 
            /// not unique, it is possible name collisions can occur, and therefore, 
            /// it is not safe to share the assembly across apps/domains. If a 
            /// simply-named assembly is ever loaded via Assembly.Load that does not appear 
            /// underneath the appbase, this error will be returned.
            /// </summary>
            FUSION_E_INVALID_PRIVATE_ASM_LOCATION = 0x80131041,

            /// <summary>
            /// When installing an assembly in the GAC, if a module (part of a 
            /// multi-file assembly) listed in the manifest is not found during 
            /// installation time, the install will fail with this error code. Modules 
            /// should be present alongside the manifest file when installing them into the GAC. 
            /// </summary>
            FUSION_E_ASM_MODULE_MISSING = 0x80131042,

            /// <summary>
            /// When installing multi-file assemblies into the GAC, the hash of each module is 
            /// checked against the hash of that file stored in the manifest. If the 
            /// hash of one of the files in the multi-file assembly does not match what is recorded 
            /// in the manifest, FUSION_E_UNEXPECTED_MODULE_FOUND will be returned. 
            /// 
            /// The name of the error, and the text description of it, are somewhat confusing. 
            /// The reason this error code is described this way is that the internally, 
            /// Fusion/CLR implements installation of assemblies in the GAC, by installing 
            /// multiple "streams" that are individually committed. 
            /// 
            /// Each stream has its hash computed, and all the hashes found 
            /// are compared against the hashes in the manifest, at the end of the installation. 
            /// Hence, a file hash mismatch appears as if an "unexpected" module was found. 
            /// </summary>
            FUSION_E_UNEXPECTED_MODULE_FOUND = 0x80131043,

            /// <summary>
            /// This error is returned when a simply-named assembly is encountered, 
            /// where a strongly-named one was expected. This error code could be returned, 
            /// for example, if you try to install a simply-named assembly into the GAC. 
            /// To strongly-name your assembly, use sn.exe. 
            /// </summary>
            FUSION_E_PRIVATE_ASM_DISALLOWED = 0x80131044,

            /// <summary>
            /// Both at bind time, and install-time, strong name signatures of assemblies are 
            /// validated. Every strongly-named assembly has a hash of the bits 
            /// recorded in the manifest (signed with the private key). If the hash of 
            /// the assembly found does not match that recorded in the manifest, signature 
            /// validation fails. 
            /// </summary>
            FUSION_E_SIGNATURE_CHECK_FAILED = 0x80131045,

            /// <summary>
            ///   There are a variety of conditions which can trigger this error, most of 
            /// which are related to either poorly-formed assembly "display names" 
            /// (textual identities), or path-too-long conditions when translating an 
            /// assembly identity into a corresponding location on the file system. 
            /// </summary>
            FUSION_E_INVALID_NAME = 0x80131047,

            /// <summary>
            /// This error indicates that AppDomainSetup.DisallowCodeDownload has been set 
            /// on the domain. Setting his flag prevents any http:// download of 
            /// assemblies (or configuration files). ASP.NET sets this flag by default 
            /// because the internal usage of wininet/urlmon to download bits over 
            /// http:// is not supported under service processes. 
            /// </summary>
            FUSION_E_CODE_DOWNLOAD_DISABLED = 0x80131048,

            /// <summary>
            /// When the CLR is installed as part of the operating system 
            /// (such as in Windows Server 2003), uninstalling CLR or frameworks assemblies 
            /// is disallowed.
            /// </summary>
            FUSION_E_UNINSTALL_DISALLOWED = 0x80131049,

        }

        /// <summary>
        /// There are some specific error codes for GAC-related work. This is a 
        /// compilation of what I know.
        /// </summary>
        /// <param name="errorCode">the specific HRESULT error code</param>
        /// <param name="translated">if true indicates a lookup value was found,
        /// otherwise set to false</param>
        /// <returns></returns>
        static internal string HResultToString(int errorCode, out bool translated)
        {
            translated = true;
            switch ((uint)errorCode)
            {
                case (uint)GACErrorCodes.FUSION_E_REF_DEF_MISMATCH:
                    return "The located assembly's manifest definition does not match the assembly.";
                case (uint)GACErrorCodes.FUSION_E_INVALID_PRIVATE_ASM_LOCATION:
                    return "The private assembly was located outside the appbase directory.";
                case (uint)GACErrorCodes.FUSION_E_ASM_MODULE_MISSING:
                    return "A module specified in the manifest was not found.";
                case (uint)GACErrorCodes.FUSION_E_UNEXPECTED_MODULE_FOUND:
                    return "Modules which are not in the manifest were streamed in.";
                case (uint)GACErrorCodes.FUSION_E_PRIVATE_ASM_DISALLOWED:
                    return "A strongly-named assembly is required.";
                case (uint)GACErrorCodes.FUSION_E_SIGNATURE_CHECK_FAILED:
                    return "The check of the signature failed.";
                case (uint)GACErrorCodes.FUSION_E_INVALID_NAME:
                    return "The given assembly name or codebase was invalid.";
                case (uint)GACErrorCodes.FUSION_E_CODE_DOWNLOAD_DISABLED:
                    return "HTTP download of assemblies has been disabled for this appdomain.";
                case (uint)GACErrorCodes.FUSION_E_UNINSTALL_DISALLOWED:
                    return "Uninstall of given assembly is not allowed.";
                default:
                    translated = false;
                    return errorCode.ToString(CultureInfo.InvariantCulture);

            }
        }

        /// <summary>
        /// Throws an exception with a specific HRESULT error code. This will 
        /// attempt to translate the error code into a useful text display and
        /// then wrap the exception with a translated message. If none is available
        /// it simple invokes the default throw mechanism.
        /// </summary>
        /// <param name="errorCode">The specific HRESULT error code</param>
        static internal void ThrowMarshalledException(int errorCode)
        {
            bool translated;
            string msg = HResultToString(errorCode, out translated);
            try
            {
                Marshal.ThrowExceptionForHR(errorCode);
            }
            catch (Exception ex)
            {
                if (!translated)
                    throw;    // rethrow it unchanged.
                // otherwise add the error message, wrap, and rethrow
                throw new Exception(msg, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ppAsmCache"></param>
        /// <param name="reserved"></param>
        /// <returns></returns>
        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(
                        out IAssemblyCache ppAsmCache,
                        int reserved);

    }
}

