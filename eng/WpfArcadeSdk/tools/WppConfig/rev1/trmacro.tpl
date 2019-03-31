`**********************************************************************`
`* This is an include template file for tracewpp preprocessor.        *`
`*                                                                    *`
`*    Copyright 1999-2005 Microsoft Corporation. All Rights Reserved. *`
`**********************************************************************`

#ifndef WPP_ALREADY_INCLUDED

// template `TemplateFile`
//   expects:
//      WPP_THIS_FILE defined (see header.tpl)
//      WPP_LOGGER_ARG  defined
//      WPP_GET_LOGGER  defined
//      WPP_ENABLED() defined

#ifndef NO_CHECK_FOR_NULL_STRING
#ifndef WPP_CHECK_FOR_NULL_STRING
#define WPP_CHECK_FOR_NULL_STRING 1
#endif
#endif


#define WPP_EVAL(_value_) _value_
#define WPP_(Id) WPP_EVAL(WPP_) ## WPP_EVAL(Id) ## WPP_EVAL(_) ## WPP_EVAL(WPP_THIS_FILE) ## WPP_EVAL(__LINE__)

#if !defined(WPP_INLINE)
#  define WPP_INLINE __inline
#endif

#else   // #ifndef WPP_ALREADY_INCLUDED

#undef WPP_LOCAL_TraceGuids

#endif  // #ifndef WPP_ALREADY_INCLUDED


#if !defined(WPP_NO_ANNOTATIONS)
#if !defined(WPP_ANSI_ANNOTATION)
`FORALL Msg IN Messages`
# define WPP_ANNOTATE_`Msg.Name`    __annotation(L"TMF:", L"`Msg.Guid` `CurrentDir` // SRC=`SourceFile.Name` MJ=`ENV MAJORCOMP` MN=`ENV MINORCOMP`", L"#typev  `Msg.Name` `Msg.MsgNo` \"`Msg.Text`\" // `Msg.Indent` `Msg.GooPairs`", L"{"`FORALL Arg IN Msg.Arguments`, L"`Arg.Name`, `Arg.MofType` -- `Arg.No`" `ENDFOR Arg`, L"}")
`ENDFOR`
#else
`FORALL Msg IN Messages`
# define WPP_ANNOTATE_`Msg.Name`    __annotation("TMF:", "`Msg.Guid` `CurrentDir` // SRC=`SourceFile.Name` MJ=`ENV MAJORCOMP` MN=`ENV MINORCOMP`", "#typev  `Msg.Name` `Msg.MsgNo` \"`Msg.Text`\" // `Msg.Indent` `Msg.GooPairs`", "{"`FORALL Arg IN Msg.Arguments`, "`Arg.Name`, `Arg.MofType` -- `Arg.No`" `ENDFOR Arg`, "}")
`ENDFOR`
#endif
# define WPP_ANNOTATE(x) WPP_ANNOTATE_ ## x,
#else
# define WPP_ANNOTATE(x)
#endif

#define WPP_LOCAL_TraceGuids WPP_`SourceFile.CanonicalName`_Traceguids

#if `TraceGuids.Count`
static const GUID WPP_LOCAL_TraceGuids[] = {`FORALL Guid IN TraceGuids` `Guid.Struct`, `ENDFOR`};
#endif

#ifndef WPP_ALREADY_INCLUDED

#if !defined(WPP_TRACE_OPTIONS)
enum {WPP_TRACE_OPTIONS = TRACE_MESSAGE_SEQUENCE   | TRACE_MESSAGE_GUID
                        | TRACE_MESSAGE_SYSTEMINFO | TRACE_MESSAGE_TIMESTAMP };
#endif

#if !defined(WPP_LOGPAIR)
# define WPP_LOGPAIR(_Size, _Addr)     (_Addr),((SIZE_T)_Size),
#endif

#define WPP_LOGTYPEVAL(_Type, _Value) WPP_LOGPAIR(sizeof(_Type), &(_Value))
#define WPP_LOGTYPEPTR(_Value)        WPP_LOGPAIR(sizeof(*(_Value)), (_Value))

// Marshalling macros.

#if !defined(WPP_LOGASTR)
# if !defined(WPP_CHECK_FOR_NULL_STRING)
#  define WPP_LOGASTR(_value)  WPP_LOGPAIR(strlen(_value) + 1, _value )
# else
#  define WPP_LOGASTR(_value)  WPP_LOGPAIR( (_value)?strlen(_value) + 1:5, (_value)?(_value):"NULL" )
# endif
#endif

#if !defined(WPP_LOGWSTR)
# if !defined(WPP_CHECK_FOR_NULL_STRING)
#  define WPP_LOGWSTR(_value)  WPP_LOGPAIR( (wcslen(_value)+1) * sizeof(WCHAR), _value)
# else
#  define WPP_LOGWSTR(_value)  WPP_LOGPAIR( (_value)?(((_value)[0] == 0)?7 * sizeof(WCHAR):(wcslen(_value) + 1)* sizeof(WCHAR)):5 * sizeof(WCHAR), (_value)?(((_value)[0] == 0) ? L"<NULL>" : (_value)):L"NULL")
# endif
#endif

#if !defined(WPP_LOGPGUID)
# define WPP_LOGPGUID(_value) WPP_LOGPAIR( sizeof(GUID), (_value) )
#endif


#if !defined(WPP_LOGPSID)
# if !defined(WPP_CHECK_FOR_NULL_STRING)
# define WPP_LOGPSID(_value)  WPP_LOGPAIR( GetLengthSid(_value), (_value) )
# else
# define WPP_LOGPSID(_value)  WPP_LOGPAIR( (_value)? (IsValidSid(_value)? \
                                                                        GetLengthSid(_value):5):5, \
                                                                        (_value)? (IsValidSid(_value)?\
                                                                        (_value):"NULL"):"NULL")
#endif
#endif

#if !defined(WPP_LOGCSTR)
# define WPP_LOGCSTR(_x) \
    WPP_LOGPAIR( sizeof((_x).Length), &(_x).Length ) WPP_LOGPAIR( (_x).Length, (_x).Buffer )
#endif

#if !defined(WPP_LOGUSTR)
# define WPP_LOGUSTR(_x) \
    WPP_LOGPAIR( sizeof((_x).Length), &(_x).Length ) WPP_LOGPAIR( (_x).Length, (_x).Buffer )
#endif

#if !defined(WPP_LOGPUSTR)
#if !defined(WPP_CHECK_FOR_NULL_STRING)
# define WPP_LOGPUSTR(_x) WPP_LOGUSTR(*(_x))
#else
# define WPP_LOGPUSTR(_x) WPP_LOGPAIR( sizeof(USHORT), (_x && (*(_x)).Length)? &(*(_x)).Length : L"\5")\
                          WPP_LOGPAIR( (_x && (*(_x)).Buffer)?(*(_x)).Length:5*sizeof(WCHAR), (_x && (*(_x)).Buffer)?(*(_x)).Buffer:L"NULL")
#endif
#endif

#if !defined(WPP_LOGPCSTR)
#if !defined(WPP_CHECK_FOR_NULL_STRING)
# define WPP_LOGPCSTR(_x) WPP_LOGCSTR(*(_x))
#else
# define WPP_LOGPCSTR(_x) WPP_LOGPAIR( sizeof(USHORT), (_x && (*(_x)).Length)? &(*(_x)).Length : L"\5")\
                          WPP_LOGPAIR( (_x && (*(_x)).Buffer)?(*(_x)).Length:5*sizeof(char), (_x && (*(_x)).Buffer)?((const char *)(*(_x)).Buffer):"NULL")
#endif
#endif

#if !defined(WPP_LOGSTDSTR)
#define WPP_LOGSTDSTR(_value)  WPP_LOGPAIR( (_value).size()+1, (_value).c_str() )
#endif

#endif  //  #ifndef WPP_ALREADY_INCLUDED


`FORALL i IN TypeSigSet WHERE !UnsafeArgs`
#ifndef WPP_SF_`i.Name`_def
#       define WPP_SF_`i.Name`_def
WPP_INLINE void WPP_SF_`i.Name`(WPP_LOGGER_ARG unsigned short id, LPCGUID TraceGuid`i.Arguments`)
   { WPP_TRACE(WPP_GET_LOGGER, WPP_TRACE_OPTIONS, (LPGUID)TraceGuid, id, `i.LogArgs` 0); }
#endif  // #ifndef WPP_SF_`i.Name`_def
`ENDFOR`

`FORALL i IN TypeSigSet WHERE UnsafeArgs`
#ifndef WPP_SF_`i.Name`_def
#       define WPP_SF_`i.Name`_def
WPP_INLINE void WPP_SF_`i.Name`(WPP_LOGGER_ARG unsigned short id, LPCGUID TraceGuid, ...) {
   va_list ap; va_start(ap, TraceGuid); UNREFERENCED_PARAMETER(ap);
   {
     `i.DeclVars`
      WPP_TRACE(WPP_GET_LOGGER, WPP_TRACE_OPTIONS, (LPGUID)TraceGuid, id, `i.LogArgs` 0);

   }
}
#endif  // #ifndef WPP_SF_`i.Name`_def
`ENDFOR`
#ifndef WPP_POST
#  define WPP_POST()
#endif

#ifndef WPP_PRE
#  define WPP_PRE()
#endif


#ifdef WPP_DEBUG
`FORALL i IN Messages WHERE !MsgArgs`
#ifndef WPP`i.GooId`_POST
#  define WPP`i.GooId`_POST(`i.GooArgs`)
#endif
#ifndef WPP`i.GooId`_PRE
#  define WPP`i.GooId`_PRE(`i.GooArgs`)
#endif
#define WPP_CALL_`i.Name`(`i.FixedArgs``i.MacroArgs`) \
     WPP`i.GooId`_PRE(`i.GooVals`) \
     WPP_ANNOTATE(`i.Name`) \
     ( ( \
         `i.MsgVal`WPP`i.GooId`_ENABLED(`i.GooVals`) ? \
          WPP_DEBUG((`i.MacroArgs`)), \
          WPP_SF_`i.TypeSig`(WPP`i.GooId`_LOGGER(`i.GooVals`) `i.MsgNo`, \
                             WPP_LOCAL_TraceGuids+0`i.MacroExprs`), 1:0  \
     ) ) \
     WPP`i.GooId`_POST(`i.GooVals`)
`ENDFOR`
#else
`FORALL i IN Messages WHERE !MsgArgs`
#ifndef WPP`i.GooId`_POST
#  define WPP`i.GooId`_POST(`i.GooArgs`)
#endif
#ifndef WPP`i.GooId`_PRE
#  define WPP`i.GooId`_PRE(`i.GooArgs`)
#endif
#define WPP_CALL_`i.Name`(`i.FixedArgs``i.MacroArgs`) \
     WPP`i.GooId`_PRE(`i.GooVals`) \
     WPP_ANNOTATE(`i.Name`) \
     ( ( \
         `i.MsgVal`WPP`i.GooId`_ENABLED(`i.GooVals`) ? \
            WPP_SF_`i.TypeSig`(WPP`i.GooId`_LOGGER(`i.GooVals`) `i.MsgNo`, \
                               WPP_LOCAL_TraceGuids+0`i.MacroExprs`), 1:0  \
     ) ) \
     WPP`i.GooId`_POST(`i.GooVals`)
`ENDFOR`
#endif

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


#ifdef WPP_DEBUG
`FORALL i IN Messages WHERE MsgArgs`
#ifndef WPP`i.GooId`_POST
#  define WPP`i.GooId`_POST(`i.GooArgs`)
#endif
#ifndef WPP`i.GooId`_PRE
#  define WPP`i.GooId`_PRE(`i.GooArgs`)
#endif
#define WPP_CALL_`i.Name`(`i.FixedArgs` MSGARGS) WPP`i.GooId`_PRE(`i.GooVals`) WPP_ANNOTATE(`i.Name`) ((WPP`i.GooId`_ENABLED(`i.GooVals`)?WPP_SF_`i.TypeSig`(WPP`i.GooId`_LOGGER(`i.GooVals`) `i.MsgNo`,WPP_LOCAL_TraceGuids+0 WPP_R`i.ReorderSig` MSGARGS), WPP_DEBUG((MSGARGS),1:0)) \
 WPP`i.GooId`_POST(`i.GooVals`)
`ENDFOR`
#else
`FORALL i IN Messages WHERE MsgArgs`
#ifndef WPP`i.GooId`_POST
#  define WPP`i.GooId`_POST(`i.GooArgs`)
#endif
#ifndef WPP`i.GooId`_PRE
#  define WPP`i.GooId`_PRE(`i.GooArgs`)
#endif
#define WPP_CALL_`i.Name`(`i.FixedArgs` MSGARGS) WPP`i.GooId`_PRE(`i.GooVals`) WPP_ANNOTATE(`i.Name`) (WPP`i.GooId`_ENABLED(`i.GooVals`)?WPP_SF_`i.TypeSig`(WPP`i.GooId`_LOGGER(`i.GooVals`) `i.MsgNo`,WPP_LOCAL_TraceGuids+0 WPP_R`i.ReorderSig` MSGARGS),1:0) WPP`i.GooId`_POST(`i.GooVals`)
`ENDFOR`
#endif


`FORALL r IN Reorder`
#undef  WPP_R`r.Name`
#define WPP_R`r.Name`(`r.Arguments`) `r.Permutation`
`ENDFOR`

/// more stuff

