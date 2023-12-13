// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __PRINTSYSTEMDIRECTINTEROPINC_HPP__
#define __PRINTSYSTEMDIRECTINTEROPINC_HPP__

#ifndef  __INTEROPDEVMODE_HPP__ 
#include <InteropDevMode.hpp>
#endif 

#ifndef  __PRINTSYSTEMSECURITY_HPP__
#include <PrintSystemSecurity.hpp>
#endif

#ifndef  __INTEROPPRINTERDEFAULTS_HPP__
#include <InteropPrinterDefaults.hpp>
#endif

#ifndef  __INTEROPDOCINFO_HPP__
#include <InteropDocInfo.hpp>
#endif

#ifndef  __INTEROPINTERFACES_HPP__
#include <InteropInterfaces.hpp>
#endif 

#ifndef  __INTEROPPRINTERINFO_HPP__
#include <InteropPrinterInfo.hpp>
#endif 

#ifndef  __INTEROPJOBINFO_HPP__
#include <InteropJobInfo.hpp>
#endif 

using namespace System::Windows::Xps::Serialization;

#ifndef  __INTEROPPRINTERHANDLERBASE_HPP__
#include <InteropPrinterHandlerBase.hpp>
#endif 

#ifndef  __INTEROPPRINTERHANDLER_HPP__
#include <InteropPrinterHandler.hpp>
#endif 

#ifndef  __XPSDEVSIMINTEROPPRINTERHANDLER_HPP__
#include <XpsDeviceSimulatingInteropPrinterHandler.hpp>
#endif 

#ifndef  __XPSCOMPATIBLEPRINTER_HPP__
#include <XpsCompatiblePrinter.hpp>
#endif 

using namespace MS::Internal::PrintWin32Thunk;

#ifndef  __INTEROPPRINTERINFOUNMANAGEDBUILDER_HPP__
#include <InteropPrinterInfoUnmanagedBuilder.hpp>
#endif 

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
#endif
