// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//      Managed declaration for IVsMSBuildTaskFileManager used for
//      getting HostObject that is implemented by VS project system for 
//      MarkupCompile tasks.
//
//  ***********************IMPORTANT**************************
//
//      The managed side declaration of this interface should match with 
//      the native side declaration which lives in VS.NET project tree.
// 
//---------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Security;

    
namespace MS.Internal
{
    // <summary>
    // Internal interface used for Interop in VS.NET hosting scenarios.
    // VS.NET project system prepares the hostobject instance which implements
    // this interface, The markupcompiler task wants to call this interface
    // to get file content from editor buffer and get last modification time.
    // </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("33372170-A08F-47F9-B1AE-CD9F2C3BB7C9")]
    internal interface IVsMSBuildTaskFileManager
    {
        // Returns the contents of the specified file based on whats in-memory else what's
        // on disk if not in-memory.
        string GetFileContents([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename);
 
        // <summary>
        // Returns the live punkDocData object for the file if it is registered in the RDT,
        // else returns NULL.
        // </summary>
        [return:MarshalAs(UnmanagedType.IUnknown)]
        object GetFileDocData([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename);


        // Returns the time of the last change to the file. If open in memory, then this is the
        // time of the last edit as reported via IVsLastChangeTimeProvider::GetLastChangeTime
        // on the open document. If the file is not open, then the last change time of the file
        // on disk is returned.
        //System.Runtime.InteropServices.ComTypes.FILETIME GetFileLastChangeTime([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename);
        long GetFileLastChangeTime([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename);

        // PutGeneratedFileContents -- puts the contents for the generated file
        // into an in memory TextBuffer and registers it in the RDT with a RDT_ReadLock.
        // This holds the file open in memory until the project is closed (when the
        // project will call IVsMSBuildHostObject::Close). If this is an actual
        // build operation (ie. UICONTEXT_SolutionBuilding is on) then the file will
        // also be saved to disk. If this is only a generation at design time for
        // intellisense purposes then the file contents are only put into memory
        // and the disk is not modified. The in-memory TextBuffer is always marked
        // as clean so the user will not be prompted to save the generated file.
        void PutGeneratedFileContents([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename, [In, MarshalAs(UnmanagedType.LPWStr)] string strFileContents);


        // IsRealBuildOperation -- returns TRUE if this is a real Build operation else
        // if this is a design-time only generation for intellisense purposes it returns
        // FALSE.
        [return:MarshalAs(UnmanagedType.Bool)]
        bool IsRealBuildOperation();


        // Delete -- deletes a file on disk and removes it from the RDT
        void Delete([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename);


        // Exists -- determines whether or not a file exists in the RDT or on disk
        [return:MarshalAs(UnmanagedType.Bool)]
        bool Exists([In, MarshalAs(UnmanagedType.LPWStr)] string wszFilename, [In, MarshalAs(UnmanagedType.Bool)] bool fOnlyCheckOnDisk);
    }
}

