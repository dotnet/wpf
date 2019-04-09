// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//-----------------------------------------------------------------------------
//
// Description:
//       Keep some constant string or settings which are shared
//       by all the tasks. 
//
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;

namespace MS.Internal.Tasks
{
    #region SharedStrings class

    //
    // This class keeps a list of predefined strings.
    // Some of them must be synced with target file settings.
    //  
    internal static class SharedStrings   
    {

        //
        // Target type settings.
        //
        internal const string       Exe = "exe";
        internal const string       WinExe = "winexe";
        internal const string       Library = "library";
        internal const string       Module = "module";

        //
        // VB Language name for special handling.
        //
        internal const string       VB = "vb";

        // 
        // Some special attribute names for WCP specific Item types.
        //
        internal const string       Localizable="Localizable";
        internal const string       Link="Link";
        internal const string       LogicalName="LogicalName";

        // 
        // externs for generated files
        //
        internal const string       XamlExtension=".xaml";
        internal const string       BamlExtension=".baml";
        internal const string       GeneratedExtension=".g";
        internal const string       IntellisenseGeneratedExtension=".g.i";
        internal const string       MainExtension = ".main";
        internal const string       LocExtension = ".loc";
        internal const string       MetadataDll = ".metadata_dll";
        internal const string       ContentFile = "_Content";

        internal const string CsExtension = ".cs";
        internal const string CsBuildCodeExtension = GeneratedExtension + CsExtension;
        internal const string CsIntelCodeExtension = IntellisenseGeneratedExtension + CsExtension;
 
        // Valid LocalizationDirectivesToLocFile
        internal const string Loc_None = "none";
        internal const string Loc_CommentsOnly = "commentsonly";
        internal const string Loc_All = "all";

        // Incremental build related settings
        internal const string StateFile = "_MarkupCompile.cache";
        internal const string LocalTypeCacheFile = "_MarkupCompile.lref";
        
        internal const string IntellisenseStateFile = "_MarkupCompile.i.cache";
        internal const string IntellisenseLocalTypeCacheFile = "_MarkupCompile.i.lref";
        //
        // File name for a generated class which supports the access of internal 
        // types in local or friend assemblies.
        //
        internal const string GeneratedInternalTypeHelperFileName = "GeneratedInternalTypeHelper";

    }

    #endregion SharedStrings class
}

