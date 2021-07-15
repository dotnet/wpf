// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// truetype.h

#pragma once
//using namespace System;
using namespace System::IO;
namespace MS { namespace Internal {

typedef System::UInt16  ushort;

/*
    Note that this class is declared public in order to stop the compiler from optimizing it out during release builds.
    The functions themselves are declared internal so as to stop non-WPF callers from utilizing it.  The reference
    assembly contains a correspondingly blank type as WPF callers directly ProjectReference this assembly.
 */
public ref class TrueTypeSubsetter abstract sealed
{
internal:
    static array<System::Byte> ^ ComputeSubset(void * fontData, int fileSize, System::Uri ^ sourceUri, int directoryOffset, array<System::UInt16> ^ glyphArray);
};

}} // MS::Internal

