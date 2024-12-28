// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "CPP/precomp.hxx"
#include <shlwapi.h>
#include "Utils.hxx" // from shared\inc
#include "dwriteloader.h" // from shared\inc

// This is how these files are declared in truetype.cpp.
// They end up belonging to namespace MS::Internal::TtfDelta
// because truetype.cpp include the cpp files inside this namespace.
// We cannot simply put this namespace specification in these 2 header files
// or elase we will break the compilation of truetype subsetter.
namespace MS { namespace Internal { namespace TtfDelta { 
#include "CPP\TrueTypeSubsetter\TtfDelta\GlobalInit.h"
#include "CPP\TrueTypeSubsetter\TtfDelta\ControlTableInit.h"
}}} // namespace MS::Internal::TtfDelta

using namespace System;
using namespace System::ComponentModel;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security;
using namespace System::Diagnostics;

[assembly:DependencyAttribute("System,", LoadHint::Always)];
[assembly:DependencyAttribute("WindowsBase,", LoadHint::Always)];

#ifndef ARRAYSIZE
#define ARRAYSIZE RTL_NUMBER_OF_V2 // from DevDiv's WinNT.h
#endif

//
// Add a module-level initialization code here.
//
// The constructor of below class should be called before any other
// code in this Assembly when the assembly is loaded into any AppDomain.
//

 namespace MS { namespace Internal {
private ref class NativeWPFDLLLoader sealed
{
public:
    //
    // Loads the wpfgfx and PresentationNative libraries from the version-specific installation folder.
    // This enables the CLR to resolve DllImport declarations for functions exported from these libraries.
    // The installation folder is not on the normal search path, so its location is found from the registry.
    //
    static void LoadDwrite( )
    {
        // Used to force the compiler to keep LoadDwrite in Release because it is called from PresentationCore.
        m_temp = NULL;
    }

private:    
    static void *m_temp;
}; 
}} // namespace MS.Internal
    
private class CModuleInitialize
{
public:

    // Constructor of class CModuleInitialize
    __declspec(noinline) CModuleInitialize()
    {
        // Initialize some global arrays.
        MS::Internal::TtfDelta::GlobalInit::Init();
        MS::Internal::TtfDelta::ControlTableInit::Init();
    }
};

/// <summary>
/// This method is a workaround to bug in the compiler.
/// The compiler generates a static unsafe method to initialize cmiStartupRunner
/// which is not properly annotated with security tags.
/// To work around this issue we create our own static method that is properly annotated.
/// </summary>
__declspec(noinline) static System::IntPtr CreateCModuleInitialize()
{
    return System::IntPtr(new CModuleInitialize());
}

// Important Note: This variable is declared as System::IntPtr to fool the compiler into creating
// a safe static method that initialzes it. If this variable was declared as CModuleInitialize
// Then the generated method is unsafe, fails NGENing and causes Jitting.
__declspec(appdomain) static System::IntPtr cmiStartupRunner = CreateCModuleInitialize();

