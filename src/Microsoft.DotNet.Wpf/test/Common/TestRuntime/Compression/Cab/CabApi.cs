// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if CABMINIMAL
  #if CABEXTRACTONLY
    namespace Microsoft.Test.Compression.Cab.MiniExtract
  #else
    namespace Microsoft.Test.Compression.Cab.Mini
  #endif
#else
  #if CABEXTRACTONLY
    namespace Microsoft.Test.Compression.Cab.Extract
  #else
    namespace Microsoft.Test.Compression.Cab
  #endif
#endif
{

using System;
using System.Text;
using System.Runtime.InteropServices;

using Handle = System.Int32;
using Pointer = System.IntPtr;

#if CLR_VERSION_BELOW_2
    // This custom attribute is a tag recognized by the dccil tool,
    // which edits IL to set the calling convention of delegates.
    [Serializable, AttributeUsage(AttributeTargets.Delegate)]
    internal sealed class UnmanagedFunctionPointerAttribute: Attribute
    {
        private const int Cdecl = (int)CallingConvention.Cdecl;
        private const int FastCall = (int)CallingConvention.FastCall;
        private const int StdCall = (int)CallingConvention.StdCall;
        private const int ThisCall = (int)CallingConvention.ThisCall;
        private const int WinApi = (int)CallingConvention.Winapi;
        private string _callingConvention = string.Empty;
        internal UnmanagedFunctionPointerAttribute(CallingConvention callingConventionType) 
        {
            _callingConvention = callingConventionType.ToString();
        }
    }
#endif //CLR_VERSION_BELOW_2

#if !CABEXTRACTONLY
/// <summary>
/// A direct import of constants, enums, structures, delegates, and functions from fci.h.
/// Refer to comments in fci.h for documentation.
/// </summary>
internal sealed class FCI
{
	private FCI() { } // This class cannot be instantiated.

	internal const int MAX_CHUNK        = 32768;
	internal const int MAX_DISK         = Int32.MaxValue;
	internal const int MAX_FILENAME     = 256;
	internal const int MAX_CABINET_NAME = 256;
	internal const int MAX_CAB_PATH     = 256;
	internal const int MAX_DISK_NAME    = 256;

	internal const int CPU_80386 = 1;


	internal enum ERROR : int
	{
		NONE,
		OPEN_SRC,
		READ_SRC,
		ALLOC_FAIL,
		TEMP_FILE,
		BAD_COMPR_TYPE,
		CAB_FILE,
		USER_ABORT,
		MCI_FAIL,
	}

	internal enum TCOMP : ushort
	{
		MASK_TYPE          = 0x000F,
		TYPE_NONE          = 0x0000,
		TYPE_MSZIP         = 0x0001,
		TYPE_QUANTUM       = 0x0002,
		TYPE_LZX           = 0x0003,
		BAD                = 0x000F,

		MASK_LZX_WINDOW    = 0x1F00,
		LZX_WINDOW_LO      = 0x0F00,
		LZX_WINDOW_HI      = 0x1500,
		SHIFT_LZX_WINDOW   =      8,

		MASK_QUANTUM_LEVEL = 0x00F0,
		QUANTUM_LEVEL_LO   = 0x0010,
		QUANTUM_LEVEL_HI   = 0x0070,
		SHIFT_QUANTUM_LEVEL=      4,

		MASK_QUANTUM_MEM   = 0x1F00,
		QUANTUM_MEM_LO     = 0x0A00,
		QUANTUM_MEM_HI     = 0x1500,
		SHIFT_QUANTUM_MEM  =      8,

		MASK_RESERVED      = 0xE000,
	}

	internal enum STATUS : uint
	{
		FILE    = 0,
		FOLDER  = 1,
		CABINET = 2,
	}

	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	internal class CCAB
	{
		internal uint   cb                    = MAX_DISK;
		internal uint   cbFolderThresh        = MAX_DISK;
		internal uint   cbReserveCFHeader     = 0;
		internal uint   cbReserveCFFolder     = 0;
		internal uint   cbReserveCFData       = 0;
		internal int    iCab                  = 0;
		internal int    iDisk                 = 0;
		internal int    fFailOnIncompressible = 0;
		internal ushort setID                 = 0;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_DISK_NAME   )] internal string szDisk    = "";
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_CABINET_NAME)] internal string szCab     = "";
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_CAB_PATH    )] internal string szCabPath = "";
	}

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]	internal delegate Pointer PFNALLOC(uint cb);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void PFNFREE(Pointer pv);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate Handle PFNOPEN(string pszFile, int oflag, int pmode, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate uint PFNREAD(Handle hf, Pointer memory, uint cb, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate uint PFNWRITE(Handle hf, Pointer memory, uint cb, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNCLOSE(Handle hf, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNSEEK(Handle hf, int dist, int seektype, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNDELETE(string pszFile, out int err, Pointer pv);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNGETNEXTCABINET(Pointer pccab, uint cbPrevCab, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNFILEPLACED(CCAB pccab, string pszFile, long  cbFile,
		int fContinuation, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate Handle PFNGETOPENINFO(string pszName,
		out ushort pdate, out ushort ptime, out ushort pattribs, out int err, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNSTATUS(STATUS typeStatus, uint cb1, uint cb2, Pointer pv);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int PFNGETTEMPFILE(Pointer pszTempName, int cbTempName, Pointer pv);

	[DllImport("cabinet.dll", EntryPoint="FCICreate", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern Pointer Create(Pointer perf, PFNFILEPLACED pfnfcifp, PFNALLOC pfna, PFNFREE pfnf,
		PFNOPEN pfnopen, PFNREAD pfnread, PFNWRITE pfnwrite, PFNCLOSE pfnclose, PFNSEEK pfnseek,
        PFNDELETE pfndelete, PFNGETTEMPFILE pfnfcigtf, [MarshalAs(UnmanagedType.LPStruct)] CCAB pccab, Pointer pv);

	[DllImport("cabinet.dll", EntryPoint="FCIAddFile", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int AddFile(Pointer hfci, string pszSourceFile, string pszFileName, bool fExecute,
		PFNGETNEXTCABINET pfnfcignc, PFNSTATUS pfnfcis, PFNGETOPENINFO pfnfcigoi, TCOMP typeCompress);

	[DllImport("cabinet.dll", EntryPoint="FCIFlushCabinet", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int FlushCabinet(Pointer hfci, bool fGetNextCab, PFNGETNEXTCABINET pfnfcignc, PFNSTATUS pfnfcis);

	[DllImport("cabinet.dll", EntryPoint="FCIFlushFolder", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int FlushFolder(Pointer hfci, PFNGETNEXTCABINET pfnfcignc, PFNSTATUS pfnfcis);

	[DllImport("cabinet.dll", EntryPoint="FCIDestroy", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int Destroy(Pointer hfci);


	[DllImport("kernel32.dll", SetLastError=true)]
	private static extern bool FileTimeToDosDateTime(ref long fileTime, out ushort wFatDate, out ushort wFatTime);
	internal static void DateTimeToCabDateAndTime(DateTime dateTime, out ushort cabDate, out ushort cabTime)
	{
		long filetime = dateTime.ToLocalTime().ToFileTime();
		FileTimeToDosDateTime(ref filetime, out cabDate, out cabTime);
	}
}
#endif // !CABEXTRACTONLY


/// <summary>
/// A direct import of constants, enums, structures, delegates, and functions from fdi.h.
/// Refer to comments in fdi.h for documentation.
/// </summary>
internal sealed class FDI
{
	private FDI() { } // This class cannot be instantiated.

	internal const int MAX_CHUNK        = 32768;
	internal const int MAX_DISK         = 0x7fffffff;
	internal const int MAX_FILENAME     = 256;
	internal const int MAX_CABINET_NAME = 256;
	internal const int MAX_CAB_PATH     = 256;
	internal const int MAX_DISK_NAME    = 256;

	internal const int CPU_80386 = 1;


	internal enum ERROR : int
	{
		NONE,
		CABINET_NOT_FOUND,
		NOT_A_CABINET,
		UNKNOWN_CABINET_VERSION,
		CORRUPT_CABINET,
		ALLOC_FAIL,
		BAD_COMPR_TYPE,
		MDI_FAIL,
		TARGET_FILE,
		RESERVE_MISMATCH,
		WRONG_CABINET,
		USER_ABORT,
	}

	internal enum NOTIFICATIONTYPE : int
	{
		CABINET_INFO,
		PARTIAL_FILE,
		COPY_FILE,
		CLOSE_FILE_INFO,
		NEXT_CABINET,
		ENUMERATE,
	}


	[StructLayout(LayoutKind.Sequential)]
	internal struct CABINFO
	{
		internal int cbCabinet;
		internal ushort cFolders;
		internal ushort cFiles;
		internal ushort setID;
		internal ushort iCabinet;
		internal int fReserve;
		internal int hasprev;
		internal int hasnext;
	}

	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	internal class NOTIFICATION
	{
		internal int cb = 0;
		[MarshalAs(UnmanagedType.LPStr)] internal string psz1 = null;
		[MarshalAs(UnmanagedType.LPStr)] internal string psz2 = null;
		[MarshalAs(UnmanagedType.LPStr)] internal string psz3 = null;
        internal Pointer pv = (Pointer)0;
		internal Handle hf = (Handle)0;
		internal ushort date = 0;
		internal ushort time = 0;
		internal ushort attribs = 0;
		internal ushort setID = 0;
		internal ushort iCabinet = 0;
		internal ushort iFolder = 0;
		internal int fdie = 0;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate Pointer PFNALLOC(uint cb);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void    PFNFREE(Pointer pv);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate Handle PFNOPEN(string pszFile, int oflag, int pmode);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate uint   PFNREAD(Handle hf, Pointer pv, uint cb);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate uint   PFNWRITE(Handle hf, Pointer pv, uint cb);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int    PFNCLOSE(Handle hf);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int    PFNSEEK(Handle hf, int dist, int seektype);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate Handle PFNNOTIFY(NOTIFICATIONTYPE fdint, NOTIFICATION pfdin);


	[DllImport("cabinet.dll", EntryPoint="FDICreate", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern Pointer Create(PFNALLOC pfnalloc, PFNFREE pfnfree, PFNOPEN pfnopen,
		PFNREAD pfnread, PFNWRITE pfnwrite, PFNCLOSE pfnclose, PFNSEEK pfnseek, int cpuType, Pointer perf);
	
	[DllImport("cabinet.dll", EntryPoint="FDICopy", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    private static extern int Copy(Pointer hfdi, string pszCabinet, string pszCabPath,
        int flags, PFNNOTIFY pfnfdin, Pointer pfnfdid, Pointer pvUser);
    internal static int Copy(Pointer hfdi, string pszCabinet, string pszCabPath,
        int flags, PFNNOTIFY pfnfdin, Pointer pvUser)
	{
		return Copy(hfdi, pszCabinet, pszCabPath, flags, pfnfdin, Pointer.Zero, pvUser);
	}

	[DllImport("cabinet.dll", EntryPoint="FDIDestroy", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int Destroy(Pointer hfdi);

	#if !CABMINIMAL
	[DllImport("cabinet.dll", EntryPoint="FDIIsCabinet", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    internal static extern int IsCabinet(Pointer hfdi, Handle hf, out CABINFO pfdici);
	#endif

    /*
	[DllImport("cabinet.dll", EntryPoint="FDITruncateCabinet", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
	public static extern int TruncateCabinet(Pointer hfdi, string pszCabinetName, ushort iFolderToDelete);
	*/

    [DllImport("kernel32.dll", SetLastError=true)]
	private static extern bool DosDateTimeToFileTime(ushort wFatDate, ushort wFatTime, out long fileTime);
	internal static void CabDateAndTimeToDateTime(ushort cabDate, ushort cabTime, out DateTime dateTime)
	{
		if(cabDate == 0 && cabTime == 0)
		{
			dateTime = DateTime.MinValue;
		}
		else
		{
			long fileTime;
			DosDateTimeToFileTime(cabDate, cabTime, out fileTime);
			dateTime = DateTime.FromFileTime(fileTime).ToUniversalTime();
		}
	}
}


internal class ERF : IDisposable
{
	private Pointer memPointer;

	internal ERF()
	{
		this.memPointer = Marshal.AllocHGlobal(12);
		this.Clear();
	}

	~ERF()
	{
		this.Dispose(false);
	}
	public void Dispose() 
	{
		this.Dispose(true);
		GC.SuppressFinalize(this); 
	}
	protected virtual void Dispose(bool disposing) 
	{
		if(this.memPointer != Pointer.Zero)
		{
			Marshal.FreeHGlobal(this.memPointer);
			this.memPointer = Pointer.Zero;
		}
		GC.KeepAlive(this);
	}

	public static implicit operator Pointer(ERF erf)
	{
		Pointer ptr = erf.memPointer;
		GC.KeepAlive(erf);
		return ptr;
	}

	internal void Clear()
	{
		this.erfOper = 0;
		this.erfType = 0;
		this.fError = false;
	}

	internal int erfOper
	{
		get
		{
			int value = Marshal.ReadInt32(this.memPointer, 0);
			GC.KeepAlive(this);
			return value;
		}
		set
		{
			Marshal.WriteInt32(this.memPointer, 0, value);
			GC.KeepAlive(this);
		}
	}

	internal int erfType
	{
		get
		{
			int value = Marshal.ReadInt32(this.memPointer, 4);
			GC.KeepAlive(this);
			return value;
		}
		set
		{
			Marshal.WriteInt32(this.memPointer, 4, value);
			GC.KeepAlive(this);
		}
	}

	internal bool fError
	{
		get
		{
			bool value = (0 != Marshal.ReadInt32(this.memPointer, 8));
			GC.KeepAlive(this);
			return value;
		}
		set
		{
			Marshal.WriteInt32(this.memPointer, 8, value ? 1 : 0);
			GC.KeepAlive(this);
		}
	}
}

}
