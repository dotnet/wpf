`**********************************************************************`
`* This is a template file for tracewpp preprocessor                  *`
`* If you need to use a custom version of this file in your project   *`
`* Please clone it from this one and point WPP to it by specifying    *`
`* -gen:{yourfile} option on RUN_WPP line in your sources file        *`
`*                                                                    *`
`*    Copyright (c) Microsoft Corporation. All Rights Reserved.       *`
`**********************************************************************`
//`Compiler.Checksum` Generated File. Do not edit.
// File created by `Compiler.Name` compiler version `Compiler.Version`-`Compiler.Timestamp`
// on `System.Date` at `System.Time` UTC from a template `TemplateFile`

`INCLUDE um-header.tpl` 
`INCLUDE control.tpl`
`INCLUDE tracemacro.tpl`

`IF FOUND WPP_INIT_TRACING`
#define WPPINIT_EXPORT 
  `INCLUDE um-init.tpl`
`ENDIF`

//
// Tracing Macro name redefinition
//

// NoMsgArgs

`FORALL f IN Funcs WHERE !DoubleP && !MsgArgs`
#undef `f.Name`
#define `f.Name` WPP_(CALL)
`ENDFOR`

`FORALL f IN Funcs WHERE DoubleP && !MsgArgs`
#undef `f.Name`
#define `f.Name`(ARGS) WPP_(CALL) ARGS
`ENDFOR`


// MsgArgs

`FORALL f IN Funcs WHERE MsgArgs`
#undef `f.Name`
#define `f.Name`(`f.FixedArgs` MSGARGS) WPP_(CALL)(`f.FixedArgs` MSGARGS)
`ENDFOR`

`FORALL r IN Reorder`
#undef  WPP_R`r.Name`
#define WPP_R`r.Name`(`r.Arguments`) `r.Permutation`
`ENDFOR`

