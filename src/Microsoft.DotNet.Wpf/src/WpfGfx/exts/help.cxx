// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  Abstract:  Display the help information for MIL extensions.
//
//------------------------------------------------------------------------------

#include "precomp.hxx"

//
// Debugger extension help.  If you add any debugger extensions, please
// add a brief description here.  Thanks!
//

char *szHelp = 
"=======================================================================\n"
"MILX debugger extensions:\n"
"-----------------------------------------------------------------------\n"
"\n"
"help                              -- Displays this help page.\n"
"\n"
"Most of the debugger extensions support a -? option for extension\n"
" specific help.\n"
"All of the debugger extensions that expect a pointer (or handle)\n"
" can parse expressions such as:\n"
"    @ebp+8\n"
"\n"
"Switches are case insensitive and can be reordered unless otherwise\n"
"specified in the extension help.\n"
"\n"
"  - failure history analysis -\n"
"\n"
"dumpcaptures [options]            -- dumps N stack captures\n"
"listcaptures [modulename]         -- summarizes stack captures\n"
"\n"
"  - general -\n"
"\n"
"annot [address]                   -- list annotations in module or at address\n"
"cmd <Address>                     -- dumps a MIL command\n"
"lcb                               -- dumps the current batch\n"
"\n"
//"  - type dump extensions -\n"
//"\n"
//"dt <Type> <Offset>                -- MIL Type Dump w/ flag/enums\n"
//"\n"
"  - extension config -\n"
"\n"
"basemodule [modulename]           -- shows/sets current base (default) module\n"
"reinit                            -- reset symbol information\n"
"verbose                           -- toggle extenstion output verbosity\n"
"\n"
"dumptable <address> <type> [fields]       -- dump entries in RTL table\n"
"resource  <hmil_resource> [<mil_channel>] -- hmil_resource -> slave resource\n"
"\n"
"=======================================================================\n";


DECLARE_API( help )
{
    UNREFERENCED_PARAMETER(args);

    OutputControl   OutCtl(Client);

    OutCtl.Output(szHelp);

    return S_OK;
}


