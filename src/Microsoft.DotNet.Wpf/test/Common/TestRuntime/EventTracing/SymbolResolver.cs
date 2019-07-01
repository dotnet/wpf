// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
namespace Microsoft.Test.EventTracing
{
    [CLSCompliant(false)]
    public sealed class ETLSymbolResolver : ISymbolResolver
    {
        public ETLSymbolResolver(TraceEventSource source)
        {
            symbolParser = new SymbolTraceEventParser(source);
            symbolParser.DbgIDRSDSTraceData += new Action<DbgIDRSDSData>(symbolParser_DbgIDRSDSTraceData);
        }

        #region ISymbolResolver Implementation

        public bool GetLineFromAddr(Address address)
        {
            return symbolReader.GetLineFromAddr(address, ref contextInfo.lineInfo);
        }
        public void CleanUp()
        {
            // do nothing for now.
        }
        [CLSCompliant(false)]
        public unsafe ulong LoadSymModule(string moduleName, ulong moduleBase)
        {
            // given a module file name *.exe  or .dll, we will try to find a matching .PDB symbol and load it.
            Console.WriteLine("Trying to load symbol for file" + moduleName);
            // lookup the image name 
            string moduleFileName = System.IO.Path.GetFileName(moduleName);
            string pdbFileName = System.IO.Path.ChangeExtension(moduleFileName, ".pdb");
            if (symbolFiles.ContainsKey(pdbFileName))
            {
                PDBInfo info = symbolFiles[pdbFileName];
                if (info != null)
                {
                    return TraceEventNativeMethods.SymLoadModuleExW(contextInfo.currentProcessHandle,
                                            IntPtr.Zero, info.pdbFullPath, null, info.pdbImageBase, (uint)0x10000000, null, (uint)0);
                }

            }
            return 0;
        }
        public bool InitSymbolResolver(SymbolResolverContextInfo context)
        {
            symbolReader = new DefaultSymbolReader(context);
            contextInfo = context;
            TraceEventNativeMethods.SymSetOptions(
                 TraceEventNativeMethods.SymOptions.SYMOPT_DEBUG |
                 TraceEventNativeMethods.SymOptions.SYMOPT_CASE_INSENSITIVE |
                // TraceEventNativeMethods.SymOptions.SYMOPT_DEFERRED_LOADS |
                //TraceEventNativeMethods.SymOptions.SYMOPT_LOAD_LINES |
                TraceEventNativeMethods.SymOptions.SYMOPT_EXACT_SYMBOLS |
                TraceEventNativeMethods.SymOptions.SYMOPT_UNDNAME // undecorated names
                );


            // for testing purpose
            Environment.SetEnvironmentVariable("_NT_SYMBOL_PATH", @"SRV*c:\websymbols*http://msdl.microsoft.com/download/symbols");

            bool bInit = TraceEventNativeMethods.SymInitializeW(contextInfo.currentProcessHandle, null, false);
            if (bInit)
            {
                registerCallback = new TraceEventNativeMethods.SymRegisterCallbackProc(SymRegisterCallbackProcInfo);
                TraceEventNativeMethods.SymRegisterCallbackW64(contextInfo.currentProcessHandle, registerCallback, 0);
            }
            return bInit;

        }

        public IntPtr CurrentProcessHandle
        {
            get
            {
                return contextInfo.currentProcessHandle;
            }
        }
        #endregion
        #region Private

        internal unsafe bool SymRegisterCallbackProcInfo(
            IntPtr hProcess,
            TraceEventNativeMethods.SymCallbackActions ActionCode,
            ulong UserData,
            ulong UserContext)
        {
            //Console.WriteLine(ActionCode);            
            switch (ActionCode)
            {
                // Symbol load has started, to cancel, return true
                case TraceEventNativeMethods.SymCallbackActions.CBA_DEFERRED_SYMBOL_LOAD_CANCEL:
                    return false;

                case TraceEventNativeMethods.SymCallbackActions.CBA_DEFERRED_SYMBOL_LOAD_START:
                    TraceEventNativeMethods.IMAGEHLP_DEFERRED_SYMBOL_LOAD64* pstartLoadEvent = (TraceEventNativeMethods.IMAGEHLP_DEFERRED_SYMBOL_LOAD64*)UserData;
                    Console.WriteLine("loading symbols for file " + new string((char*)pstartLoadEvent->FileName) + " started ....");
                    return true;

                case TraceEventNativeMethods.SymCallbackActions.CBA_DEFERRED_SYMBOL_LOAD_COMPLETE:
                    TraceEventNativeMethods.IMAGEHLP_DEFERRED_SYMBOL_LOAD64* pLoadEvent = (TraceEventNativeMethods.IMAGEHLP_DEFERRED_SYMBOL_LOAD64*)UserData;
                    switch (pLoadEvent->Flags)
                    {
                        case TraceEventNativeMethods.DSLFLAG_MISMATCHED_DBG:
                            Console.WriteLine("Mismatched DBG info");
                            break;
                        case TraceEventNativeMethods.DSLFLAG_MISMATCHED_PDB:
                            Console.WriteLine("Mismatched PDP");
                            break;
                        default:
                            Console.WriteLine("loading symbols for file " + new string((char*)pLoadEvent->FileName) + " Completed successfully");
                            break;
                    }
                    break;

                case TraceEventNativeMethods.SymCallbackActions.CBA_READ_MEMORY:
                    Console.WriteLine("Requesting Read memory operation?!! what should i do");
                    break;

                case TraceEventNativeMethods.SymCallbackActions.CBA_DEBUG_INFO:
                    char* pChar = (char*)UserData;
                    Console.WriteLine(new string(pChar));
                    break;



                case TraceEventNativeMethods.SymCallbackActions.CBA_EVENT:
                    TraceEventNativeMethods.IMAGEHLP_CBA_EVENT* pEvent = (TraceEventNativeMethods.IMAGEHLP_CBA_EVENT*)UserData;
                    Console.WriteLine(new string(pEvent->pStrDesc));
                    return true;


                default:
                    return false; // if we don't know how to handle this, we should return false.
            }

            return false;
        }

        internal unsafe bool SymEnumSymbolsProc(
           TraceEventNativeMethods.SYMBOL_INFO* pSymInfo,
           uint SymbolSize,
           IntPtr UserContext)
        {

            char* p = (char*)&pSymInfo->Name;
            string s = new string(p);
            // Console.WriteLine(s);

            return true;
        }

        private void symbolParser_DbgIDRSDSTraceData(DbgIDRSDSData obj)
        {
            string pdbFile = obj.PdbFileName;
            Guid pdbGuid = obj.GuidSig;

            if (!symbolFiles.ContainsKey(pdbFile))
            {
                // unsafe, but OK to do for now, 
                // other options is to Marshal.AllocHGlobal.
                // other option is to use fixed in unsafe code
                // other option ....?1

                GCHandle handle = GCHandle.Alloc(pdbGuid, GCHandleType.Pinned);

                IntPtr guidPtr = handle.AddrOfPinnedObject();

                //
                StringBuilder pdbFullPath = new StringBuilder(1024);
                // Try To locate the symbol file.

                //tmm.pdbWith Guid2d244915-4bd6-4d74-a496-8877396f5510
                bool foundPDB = TraceEventNativeMethods.SymFindFileInPathW(contextInfo.currentProcessHandle,
                    null,
                    pdbFile,
                    guidPtr,
                    (int)obj.Age,
                    0,
                    TraceEventNativeMethods.SSRVOPT_GUIDPTR,
                    pdbFullPath,
                    null,
                    IntPtr.Zero);
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == 0x2) // path not found
                {
                    Console.WriteLine("Can't find symbol for " + pdbFile);
                    Console.WriteLine("are you sure _NT_SYMBOL_PATH is set?"); // check if _NT_SYMBOL_PATH is set
                }

                if (foundPDB)
                {
                    // Lookup in _NT_SYMBOL_PATH failed, try to get one from the path.
                    PDBInfo info = new PDBInfo() { pdbFullPath = pdbFullPath.ToString(), pdbImageBase = (ulong)obj.ImageBase };
                    symbolFiles.Add(pdbFile, info);
                }
                else
                {
                    symbolFiles.Add(pdbFile, null);
                }
                handle.Free();
            }
        }

#if SYMBOL_DECODE_TESTING
        public void TestSymbolDecoding()
        {
            // Test symbol decoding..
            TracePdbReader reader = new TracePdbReader(null);
            foreach (KeyValuePair<string, PDBInfo> pdbInfo in symbolFiles)
            {
                TracePdbModuleReader moduleReader = reader.LoadSymbolsForModule(pdbInfo.Value.pdbFullPath,
                                                                            (Address)pdbInfo.Value.pdbImageBase);
                Address outAddr;
                string symbol = moduleReader.FindMethodForAddress((Address)0x10000000, out outAddr);
                if (!String.IsNullOrEmpty(symbol))
                    Console.WriteLine(symbol);
            }
        }
#endif

        private ISymbolReader symbolReader;
        private class PDBInfo
        {
            public string pdbFullPath;
            public ulong pdbImageBase;
        }
        private SymbolTraceEventParser symbolParser;
        private Dictionary<string, PDBInfo> symbolFiles = new Dictionary<string, PDBInfo>();
        private TraceEventNativeMethods.SymRegisterCallbackProc registerCallback;
        private SymbolResolverContextInfo contextInfo;
        #endregion
    }
}
