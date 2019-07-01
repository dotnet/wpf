// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Test.Win32;
using Microsoft.Win32;

namespace Microsoft.Test.Diagnostics
{
    /// <summary>
    /// Provides operating system version information.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public partial class SystemInformation
    {
        #region Private Data

        private Product product;
        private ProductSuite productSuite;
        private VistaProductType vistaProductType;
        private FileVersionInfo kernel32Version;
        private Kernel32.OSVERSIONINFOEX osVersionInfo;

        private static readonly SystemInformation current = new SystemInformation();

        #endregion        

        #region Constructor

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        private SystemInformation()
        {
            osVersionInfo = GetOSVersion();            
            product = GetProduct(osVersionInfo);
            productSuite = GetProductSuite(osVersionInfo);
            vistaProductType = GetVistaProductType(osVersionInfo);            

            // calculate the build revision number by examining the file version of kernel32.dll
            // Environment.SpecialFolder.System
            string kernel32Path = Environment.GetFolderPath (Environment.SpecialFolder.System) + "\\kernel32.dll";
            kernel32Version = FileVersionInfo.GetVersionInfo(kernel32Path);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the major version number (MAJOR.MINOR.BUILD.REVISION) of the operating system as reported by GetVersionEx.
        /// </summary>
        /// <value>
        /// <list>
        ///     <listheader>
        ///         <term>Value</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>4</term>
        ///         <description>Windows NT 4.0.</description>
        ///     </item>
        ///     <item>
        ///         <term>5</term>
        ///         <description>Windows Server 2003, Windows XP, or Windows 2000.</description>
        ///     </item>
        ///     <item>
        ///         <term>6</term>
        ///         <description>Windows Vista or Windows Server "Longhorn".</description>
        ///     </item>
        /// </list>
        /// </value>
        public int MajorVersion
        {
            get { return Environment.OSVersion.Version.Major; }
        }

        /// <summary>
        /// Gets the Minor version number (MAJOR.MINOR.BUILD.REVISION) of the operating system as reported by GetVersionEx.
        /// </summary>
        /// <value>
        /// <list>
        ///     <listheader>
        ///         <term>Value</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>0</term>
        ///         <description>Windows Vista, Windows Server "Longhorn", Windows 2000, or Windows NT 4.0.</description>
        ///     </item>
        ///     <item>
        ///         <term>1</term>
        ///         <description>Windows XP.</description>
        ///     </item>
        ///     <item>
        ///         <term>2</term>
        ///         <description>Windows Server 2003 R2, Windows Server 2003, or Windows XP Professional x64 Edition.</description>
        ///     </item>
        /// </list>
        /// </value>
        public int MinorVersion
        {
            get { return Environment.OSVersion.Version.Minor; }
        }        

        /// <summary>
        /// Gets the Build number (MAJOR.MINOR.BUILD.REVISION) of the operating system as reported by GetVersionEx.
        /// </summary>
        public int BuildNumber
        {
            get { return Environment.OSVersion.Version.Build; }
        }

        /// <summary>
        /// Gets the public revision number
        /// </summary>
        public int RevisionNumber
        {
            get { return Environment.OSVersion.Version.Revision; }
        }

        /// <summary>
        /// Gets the revision number (MAJOR.MINOR.BUILD.REVISION) of the operatiing system by parsing the file version of kernel32.dll
        /// </summary>       
        public int InternalRevisionNumber
        {
            get { return kernel32Version.FilePrivatePart; }
        }

        /// <summary>
        /// Gets the OS version as a string in the format <see cref="MajorVersion"/>.<see cref="MinorVersion"/>.<see cref="BuildNumber"/>.<see cref="RevisionNumber"/>.
        /// </summary>
        public string OSVersion
        {
            get { return string.Format("{0}.{1}.{2}.{3}", MajorVersion, MinorVersion, BuildNumber, InternalRevisionNumber); }
        }        


        /// <summary>
        /// Gets the latest Service Pack installed on the system.
        /// </summary>
        /// <value>A string name for the latest service pack installed on the system; otherwise, an empty string if no service pack has been installed.</value>
        public string ServicePack
        {
            get { return Environment.OSVersion.ServicePack; }
        }

        /// <summary>
        /// Gets the major version number of the latest service pack installed on the system.
        /// </summary>
        /// <value>
        /// The major version number of the latest service pack installed on the system; otherwise, zero if no
        /// service pack has been installed.
        /// </value>
        public int ServicePackMajor
        {
            get { return osVersionInfo.ServicePackMajor; }
        }

        /// <summary>
        /// Gets the major version number of the latest service pack installed on the system.
        /// </summary>
        /// <value>
        /// The major version number of the latest service pack installed on the system; otherwise, zero if no
        /// service pack has been installed.
        /// </value>
        public int ServicePackMinor
        {
            //This is info we can't seem to get easily from 
            get { return osVersionInfo.ServicePackMinor; }
        }

        /// <summary>
        /// Gets the product in the form of an enum
        /// </summary>
        public Product Product
        {
            get { return product; }
        }

        /// <summary>
        /// Gets the <see cref="ProductSuite"/> flags identifying the product suites available on the system.
        /// </summary>
        /// <value>
        /// The <see cref="ProductSuite"/> flags identifying the product suites available on the system; 
        /// otherwise None if the value could not be retrieved.
        /// </value>
        public ProductSuite ProductSuite
        {
            get { return productSuite; }
        }

        /// <summary>
        /// Gets the <see cref="OSProductType"/> of the system.
        /// </summary>
        /// <value>
        /// The <see cref="OSProductType"/> of the system; otherwise, None if the 
        /// value could not be retrieved.
        /// </value>
        public OSProductType OSProductType
        {
            get { return (OSProductType)osVersionInfo.ProductType; }
        }

        /// <summary>
        /// Gets the <see cref="PlatformID"/> for the OS.
        /// </summary>
        /// <value>
        /// The <see cref="PlatformID"/> for the OS.
        /// </value>
        public PlatformID PlatformID
        {
            get { return (PlatformID)osVersionInfo.PlatformId; }
        }

        /// <summary>
        /// Processor Architecture
        /// </summary>
        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return ProcessorInformation.Current.ProcessorArchitecture; }
        }

        /// <summary>
        /// returns true if the current process is running in WOW64 mode, otherwise, false
        /// </summary>
        public bool IsWow64Process
        {
            get { return ProcessorInformation.Current.IsWow64Process; }
        }

        /// <summary>
        /// Returns the IE version in format 7.0.5730.11
        /// </summary>
        public string IEVersion
        {
            get { return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer", "Version", null) as string; }
        }

        /// <summary>
        /// System Locale Information
        /// </summary>
        public CultureInfo UICulture
        {
            get { return CultureInfo.CurrentUICulture; }
        }

        /// <summary>
        /// Operating System Language Information
        /// </summary>
        public CultureInfo OSCulture
        {
            get { return CultureInfo.InstalledUICulture; }
        }

        /// <summary>
        /// Sets the Process to be DPI Aware
        /// </summary>
        /// <remarks>
        /// You should call this API before using GetDeviceCaps if you want to know the actual
        /// System DPI. This API does nothing on builds downlevel from longhorn.
        /// </remarks>
        public void SetProcessDpiAware()
        {
            ProcessorInformation.Current.SetProcessDpiAware();
        }


        /// <summary>
        /// Gets the value indicating if the OS is a server OS.
        /// </summary>
        /// <value>
        /// true if the server is a Server or DomainController; otherwise, false.
        /// </value>
        public bool IsServer
        {
            get { return OSProductType == OSProductType.Server || OSProductType == OSProductType.DomainController; }
        }

        /// <summary>
        /// Gets the value indicating if the OS is an enterprise SKU
        /// </summary>
        /// <value>
        /// true if the os has the Enterprise flag set or the <see cref="VistaProductType"/> is one of the 
        /// datacenter skus; otherwise, false.
        /// </value>
        public bool IsEnterprise
        {
            get
            {
                return 
                (
                    ( (ProductSuite & ProductSuite.Enterprise) == ProductSuite.Enterprise )
                    ||
                    VistaProductType == VistaProductType.Enterprise
                    ||
                    VistaProductType == VistaProductType.EnterpriseServer
                    ||
                    VistaProductType == VistaProductType.EnterpriseServerCore
                    ||
                    VistaProductType == VistaProductType.EnterpriseServerIA64
                );
            }
        }

        /// <summary>
        /// Gets the value indicating if the OS is an data center SKU
        /// </summary>
        /// <value>
        /// true if the os has the DataCenter flag set or the <see cref="VistaProductType"/> is one of the 
        /// datacenter skus; otherwise, false.
        /// </value>
        public bool IsDataCenter
        {
            get
            {
                return
                (
                    ( (ProductSuite & ProductSuite.DataCenter) == ProductSuite.DataCenter )
                    ||
                    VistaProductType == VistaProductType.DataCenterServer
                    ||
                    VistaProductType == VistaProductType.DataCenterCore
                );
            }
        }

        /// <summary>
        /// Gets the value indicating if the OS is a personal SKU.
        /// </summary>
        /// <value>
        /// true if the os has the Personal flag set; otherwise, false.
        /// </value>
        /// <remarks>
        /// This value indicates the OS is Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home.
        /// </remarks>
        public bool IsPersonalEdition
        {
            get { return (ProductSuite & ProductSuite.Personal) == ProductSuite.Personal; }
        }

        /// <summary>
        /// Gets the value indicating if the OS is a starter edition.
        /// </summary>
        /// <value>
        /// true if the os has the StarterEdition flag set; otherwise, false.
        /// </value>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for StartEdition.
        /// </remarks>
        public bool IsStarterEdition
        {
            get { return (ProductSuite & ProductSuite.StarterEdition) == ProductSuite.StarterEdition; }
        }

        /// <summary>
        /// Gets the value indicating if the OS is a Windows Server 2003 R2.
        /// </summary>
        /// <value>
        /// true if the os has the ServerR2 flag set; otherwise, false.
        /// </value>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for <see cref="SystemMetric.ServerR2"/>.
        /// </remarks>
        public bool IsServerR2
        {
            get { return (ProductSuite & ProductSuite.ServerR2) == ProductSuite.ServerR2; }
        }

        /// <summary>
        /// Gets the value indicating if the OS is Windows Tablet PC
        /// </summary>
        /// <value>
        /// true if the os has the TabletPC flag set; otherwise, false.
        /// </value>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for <see cref="SystemMetric.TabletPC"/>.
        /// </remarks>
        public bool IsTabletPC
        {
            get { return (ProductSuite & ProductSuite.TabletPC) == ProductSuite.TabletPC; }
        }

        /// <summary>
        /// Gets the <see cref="VistaProductType"/> for Vista OS installations.
        /// </summary>
        /// <value>The <see cref="VistaProductType"/>; otherwise, None if the OS is not a Vista installation.</value>
        public VistaProductType VistaProductType
        {
            get { return vistaProductType; }
        }

        /// <summary>
        /// Gets the path to the windows\microsoft.net\framework\v<something>\WPF\ folder.
        /// </summary>
        /// <value>The appropriate framework wpf path</value>
        public string FrameworkWpfPath
        {
            get
            {
                if (Environment.Version.Major == 2)  //CLR v2 implies WPF v3
                {
                    // all 3.x versions have the same framework path, just return it
                    return Environment.GetEnvironmentVariable("Windir") + @"\Microsoft.NET\Framework\v3.0\WPF\";
                }
                else  //v4 or newer, just find the path
                {
                    Assembly mscorlib = Assembly.GetAssembly(typeof(object));  // literally, find the assembly that defines "object".  Turns out it's mscorlib, the only managed assembly not in the GAC, but in the Framework dir.
                    return Path.GetDirectoryName(mscorlib.Location) + @"\WPF\";
                }
            }
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Current instance of the
        /// </summary>
        public static SystemInformation Current
        {
            get { return current; }
        }

        #endregion

        #region Private Members

        private Win32.Kernel32.OSVERSIONINFOEX GetOSVersion()
        {
            Kernel32.OSVERSIONINFOEX versionInfo = new Kernel32.OSVERSIONINFOEX();
            versionInfo.Size = Marshal.SizeOf(typeof(Kernel32.OSVERSIONINFOEX));

            if (!Kernel32.GetVersionEx(ref versionInfo))
                throw new Win32Exception();

            return versionInfo;
        }

        private Product GetProduct(Kernel32.OSVERSIONINFOEX osVersion)
        {
            Product product = Product.None;

            //Get the product
            switch (osVersion.MajorVersion)
            {
                case 4:
                    product = Product.WindowsNT4;
                    break;
                case 5:
                    switch (osVersion.MinorVersion)
                    {
                        case 0:
                            product = Product.Windows2000;
                            break;
                        case 1:
                            product = Product.WindowsXP;
                            break;
                        case 2:
                            //Windows XP x64 actually has version 5.2
                            if (OSProductType != OSProductType.Workstation)
                                product = Product.WindowsServer2003;
                            else
                                product = Product.WindowsXP;
                            break;
                        default:
                            product = Product.None;
                            break;
                    }
                    break;
                case 6:
                    switch (osVersion.MinorVersion)
                    {
                        case 0:           
                            //Both Vista and LH Server have major version 6. "LH Server" is Windows Server 2008.
                            if (OSProductType == OSProductType.Workstation && Environment.OSVersion.Platform == PlatformID.Win32NT)
                                product = Product.WindowsVista;
                            else
                                product = Product.LHServer;
                            break;
                        case 1:
                            if (OSProductType == OSProductType.Workstation && Environment.OSVersion.Platform == PlatformID.Win32NT)
                                product = Product.Windows7;
                            else
                                product = Product.WindowsServer2008R2;
                            break;
                        case 2:
                            product = Product.Windows8;
                            break;
                    }
                    break;
                default:
                    product = Product.None;
                    break;
            }

            return product;
        }

        private ProductSuite GetProductSuite(Kernel32.OSVERSIONINFOEX osVersion)
        {
            ProductSuite suite = (ProductSuite)osVersion.SuiteMask;
          
            // Query system metrics to detect other sku data...           
            if (SystemMetrics.GetSystemMetric (SystemMetric.TabletPC) != 0)
                suite |= ProductSuite.TabletPC;

            if (SystemMetrics.GetSystemMetric (SystemMetric.MediaCenter) != 0)
                suite |= ProductSuite.MediaCenter;

            if (SystemMetrics.GetSystemMetric (SystemMetric.StarterEdition) != 0)
                suite |= ProductSuite.StarterEdition;

            if (SystemMetrics.GetSystemMetric (SystemMetric.ServerR2) != 0)
                suite |= ProductSuite.ServerR2;

            return suite;
        }

        private VistaProductType GetVistaProductType(Kernel32.OSVERSIONINFOEX osVersion)
        {
            VistaProductType vistaProduct = VistaProductType.None;

            // if this is a vista or longhorn install, call the new GetProductInfo API to retreive SKU data...
            if (osVersion.MajorVersion >= 6)
            {
                int productType = 0;
                if (Kernel32.GetProductInfo(osVersion.MajorVersion, osVersion.MinorVersion, osVersion.ServicePackMajor, osVersion.ServicePackMinor, ref productType) != 0)
                {
                    if (productType != 0)
                        vistaProduct = (VistaProductType)productType;
                    else
                        vistaProduct = VistaProductType.StorageEnterpriseServer;
                }
            }

            return vistaProduct;
        }

        #endregion
    }

    /// <summary>
    /// Operating System Product
    /// </summary>
    public enum Product
    {
        /// <summary>
        /// Product unknown
        /// </summary>
        None = 0,
        /// <summary>
        /// Vista
        /// </summary>
        WindowsVista = 1,
        /// <summary>
        /// Windows Server 2003 or Windows Server 2003 R2
        /// </summary>
        WindowsServer2003 = 2,
        /// <summary>
        /// Windows XP Pro or Windows XP Pro X64
        /// </summary>
        WindowsXP = 3,
        /// <summary>
        /// Windows 2000
        /// </summary>
        Windows2000 = 4,
        /// <summary>
        /// Windows NT 4.0
        /// </summary>
        WindowsNT4 = 5,
        /// <summary>
        /// Windows NT 3.XX
        /// </summary>
        WindowsNT3 = 6,
        /// <summary>
        /// LH Server  (a.k.a. Windows Server 2008)
        /// </summary>
        LHServer = 7,
        /// <summary>
        /// Windows 7
        /// </summary>
        Windows7 = 8,
        /// <summary>
        /// Windows 8
        /// </summary>
        Windows8 = 9,
        /// <summary>
        /// Windows Server 2008 R2
        /// </summary>
        WindowsServer2008R2 = 10
    }

    /// <summary>
    /// The flags that identify the product suites available on the system.
    /// </summary>
    [Flags]
    public enum ProductSuite : int
    {
        /// <summary>
        /// The value has not been intialized or could not be retrieved.
        /// </summary>
        None = 0,

        /// <summary>
        /// Microsoft BackOffice components.
        /// </summary>
        BackOffice = 0x0004,

        /// <summary>
        /// Windows Server 2003, Web Edition.
        /// </summary>
        BladeServer = 0x0400,

        /// <summary>
        /// Windows Server 2003, Compute Cluster Edition 
        /// </summary>
        ComputeClusterServer = 0x4000,

        /// <summary>
        /// Windows Server "Longhorn", Datacenter Edition, Windows Server 2003, Datacenter Edition or Windows 2000 Datacenter Server.
        /// </summary>
        DataCenter = 0x0080,

        /// <summary>
        /// Windows Server "Longhorn", Enterprise Edition, Windows Server 2003, 
        /// Enterprise Edition, Windows 2000 Advanced Server, 
        /// or Windows NT Server 4.0 Enterprise Edition is installed.
        /// </summary>
        Enterprise = 0x0002,

        /// <summary>
        /// Windows XP Embedded.
        /// </summary>
        Embedded = 0x0040,

        /// <summary>
        /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition
        /// </summary>
        Personal = 0x0200,

        /// <summary>
        /// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
        /// </summary>
        /// <remarks>
        /// Note that you should not solely rely upon the <see cref="SmallBusiness"/> flag to determine whether 
        /// Small Business Server has been installed on the system, 
        /// as both this flag and the <see cref="SmallBusinessRestricted"/> flag are set when 
        /// this product suite is installed. If you upgrade this installation to Windows Server, 
        /// Standard Edition, the <see cref="SmallBusinessRestricted"/> flag will be unset � 
        /// however, the <see cref="SmallBusiness"/> flag will remain set. 
        /// In this case, this indicates that Small Business Server was once installed on 
        /// this system. 
        /// If this installation is further upgraded to Windows Server, Enterprise Edition, 
        /// the <see cref="SmallBusiness"/> key will remain set.
        /// </remarks>
        SmallBusiness = 0x0001,

        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive client license in force.
        /// </summary>
        SmallBusinessRestricted = 0x0020,

        /// <summary>
        /// Windows Storage Server 2003 R2 or Windows Storage Server 2003
        /// </summary>
        StorageServer = 0x2000,

        /// <summary>
        /// Terminal Services is installed. This value is always set.
        /// </summary>
        /// <remarks>
        /// If <see cref="TerminalServices"/> is set but <see cref="TerminalServicesSingleUser"/>
        /// is not set, the system is running in application server mode.
        /// </remarks>
        TerminalServices = 0x0010,

        /// <summary>
        /// Remote Desktop is supported, but only one interactive session is supported. 
        /// </summary>
        /// <remarks>
        /// This value is set unless the system is running in application server mode.
        /// </remarks>
        TerminalServicesSingleUser = 0x0100,

        #region Flags derived from GetSystemMetrics

        /// <summary>
        /// Windows XP Tablet PC
        /// </summary>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for TablePC.
        /// </remarks>
        TabletPC = 0x10000,

        /// <summary>
        /// Windows XP, Media Center Edition.
        /// </summary>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for <see cref="SystemMetric.MediaCenter"/>.
        /// </remarks>
        MediaCenter = 0x20000,

        /// <summary>
        /// Windows XP Starter Edition.
        /// </summary>
        /// <remarks>
        /// This flag is set if <see cref="SystemMetrics.GetSystemMetric"/> returns non-zero for StartEdition.
        /// </remarks>
        StarterEdition = 0x40000,

        /// <summary>
        /// Windows Server 2003 R2.
        /// </summary>
        ServerR2 = 0x80000,

        #endregion Flags derived from GetSystemMetrics

        /// <summary>
        /// Flag for Server
        /// </summary>
        Server = BladeServer | ComputeClusterServer | DataCenter | Enterprise | SmallBusiness | SmallBusinessRestricted | StorageServer,
    }

    /// <summary>
    /// Defines the OS product type retported by ProductType.
    /// </summary>
    public enum OSProductType : byte
    {
        /// <summary>
        /// The value has not been intialized or could not be retrieved.
        /// </summary>
        None,

        /// <summary>
        /// The operating system is Windows Vista, Windows XP Professional, Windows XP Home Edition, Windows 2000 Professional, or Windows NT Workstation 4.0.
        /// </summary>
        Workstation = 1,

        /// <summary>
        /// The system is a domain controller.
        /// </summary>
        DomainController = 2,

        /// <summary>
        /// The system is a server.
        /// </summary>
        /// <remarks>
        /// Note that a server that is also a domain controller is reported as <see cref="DomainController"/>, not <see cref="Server"/>.
        /// </remarks>
        Server = 3
    }

    /// <summary>
    /// Provides Vista product info types.
    /// </summary>
    /// <remarks>
    /// See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/sysinfo/base/getproductinfo.asp.
    /// </remarks>
    public enum VistaProductType : int
    {
        /// <summary>
        /// Used for non-vista OS.
        /// </summary>
        None = 0,
        /// <summary>
        /// Windows Vista Ultimate
        /// </summary>
        Ultimate = 0x0001,
        /// <summary>
        /// Windows Vista Home Basic
        /// </summary>
        HomeBasic = 0x0002,
        /// <summary>
        /// Windows Vista Home Premium
        /// </summary>
        HomePremium = 0x0003,
        /// <summary>
        /// TBD
        /// </summary>
        HomeBasicN = 0x0005,
        /// <summary>
        /// TBD
        /// </summary>
        HomeServer = 0x0013,
        /// <summary>
        /// Windows Vista Business
        /// </summary>
        Business = 0x0006,
        /// <summary>
        /// Windows Vista Starter Edition
        /// </summary>
        Starter = 0x000B,
        /// <summary>
        /// Windows Server "Longhorn" (Full installation)
        /// </summary>
        Server = 0x0007,
        /// <summary>
        /// Windows Server "Longhorn" (Server Core installation)
        /// </summary>
        ServerCore = 0x000D,
        /// <summary/>
        ServerForSmallBusiness = 0x0018,
        /// <summary/>
        SmallBusinessServer = 0x0009,
        /// <summary/>
        SmallBusinessServerPremium = 0x00019,
        /// <summary>
        /// Windows Server "Longhorn", Datacenter Edition (Full installation)
        /// </summary>
        DataCenterServer = 0x0008,
        /// <summary>
        /// Windows Server "Longhorn", Datacenter Edition (Server Core installation)
        /// </summary>
        DataCenterCore = 0x000C,
        /// <summary>
        /// Windows Vista Enterprise
        /// </summary>
        Enterprise = 0x0004,
        /// <summary>
        /// Windows Server "Longhorn", Enterprise Edition (Full installation)
        /// </summary>
        EnterpriseServer = 0x000A,
        /// <summary>
        /// Windows Server "Longhorn", Enterprise Edition (Server Core installation)
        /// </summary>
        EnterpriseServerCore = 0x000E,
        /// <summary>
        /// Windows Server "Longhorn", Enterprise Edition for Itanium-based Systems
        /// </summary>
        EnterpriseServerIA64 = 0x000F,
        /// <summary/>
        BusinessN = 0x0010,
        /// <summary/>
        ClusterServer = 0x0012,
        /// <summary/>
        StorageEnterpriseServer = 0xFFFF, //This should be 0, but we have assigned 0 to None
        /// <summary/>
        StorageExpressServer = 0x0014,
        /// <summary/>
        StorageStandardServer = 0x0015,
        /// <summary/>
        StorageWorkgroupServer = 0x0016,
        /// <summary>
        /// If the software license is invalid or expired.
        /// </summary>
        Unlicensed = unchecked((int)0xABCDABCD)
    }
}
