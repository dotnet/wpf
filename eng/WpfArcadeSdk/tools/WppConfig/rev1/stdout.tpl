`**********************************************************************`
`* This is an include template file for tracewpp preprocessor.        *`
`*                                                                    *`
`*    Copyright (c) Microsoft Corporation. All Rights Reserved.     *`
`**********************************************************************`

// template `TemplateFile`

`FORALL f IN Funcs`
#define WPP`f.GooId`_LOGGER(`f.GooArgs`) // `f.Name`
`ENDFOR`

#ifndef WPP_LOGGER_ARG
#  define WPP_LOGGER_ARG
#endif

#ifndef WPP_GET_LOGGER
  #define WPP_GET_LOGGER WppGetLogger()
  __inline TRACEHANDLE WppGetLogger() 
  {
      static TRACEHANDLE Logger;
      if (Logger) {return Logger;}
      return Logger = WppQueryLogger(0);
  }
#endif

#ifndef WPP_ENABLED
#  define WPP_ENABLED() 1
#endif
