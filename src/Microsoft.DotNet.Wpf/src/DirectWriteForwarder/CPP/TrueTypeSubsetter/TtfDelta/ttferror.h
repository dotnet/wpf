// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * TTFerror.h: error codes returned by ttfdelta and ttfmerge modules - Written by Louise Pathe
  *
  *
  * 
  */

#ifndef TTFERROR_DOT_H_DEFINED
#define TTFERROR_DOT_H_DEFINED        
/* Return codes */
#ifndef NO_ERROR
#define NO_ERROR 0
#endif
#ifndef ERR_GENERIC
#define ERR_GENERIC 1000  
#define ERR_READOUTOFBOUNDS 1001    /* trying to read from memory not allowed - data error? */
#define ERR_WRITEOUTOFBOUNDS 1002    /* trying to write to memory not allowed - data error? */
#define ERR_READCONTROL 1003    /* read control structure does not match data */
#define ERR_WRITECONTROL 1004    /* write control structure does not match data */
#define ERR_MEM 1005   /* error allocating memory */
#define ERR_FORMAT 1006 /* input data format error */
#endif

#define ERR_WOULD_GROW 1007 /* action would cause data to grow. use original data */
#define ERR_VERSION 1008    /* major dttf.version of the input data is greater than the version this program can read */
#define ERR_NO_GLYPHS 1009
#define ERR_INVALID_MERGE_FORMATS 1010 /* trying to merge fonts with the wrong dttf formats */
#define ERR_INVALID_MERGE_CHECKSUMS 1011  /* trying to merge 2 fonts from different mother font */
#define ERR_INVALID_MERGE_NUMGLYPHS 1012  /* trying to merge 2 fonts from different mother font */
#define    ERR_INVALID_DELTA_FORMAT    1013  /* trying to subset a format 1 or 2 font */
#define ERR_NOT_TTC 1014
#define ERR_INVALID_TTC_INDEX 1015


#define ERR_MISSING_CMAP 1030
#define ERR_MISSING_GLYF 1031
#define ERR_MISSING_HEAD 1032
#define ERR_MISSING_HHEA 1033
#define ERR_MISSING_HMTX 1034
#define ERR_MISSING_LOCA 1035
#define ERR_MISSING_MAXP 1036
#define ERR_MISSING_NAME 1037
#define ERR_MISSING_POST 1038
#define ERR_MISSING_OS2  1039
#define ERR_MISSING_VHEA 1040
#define ERR_MISSING_VMTX 1041
#define ERR_MISSING_HHEA_OR_VHEA 1042
#define ERR_MISSING_HMTX_OR_VMTX 1043
#define ERR_MISSING_EBDT 1044

#define ERR_INVALID_CMAP 1060
#define ERR_INVALID_GLYF 1061
#define ERR_INVALID_HEAD 1062
#define ERR_INVALID_HHEA 1063
#define ERR_INVALID_HMTX 1064
#define ERR_INVALID_LOCA 1065
#define ERR_INVALID_MAXP 1066
#define ERR_INVALID_NAME 1067
#define ERR_INVALID_POST 1068
#define ERR_INVALID_OS2 1069
#define ERR_INVALID_VHEA 1070
#define ERR_INVALID_VMTX 1071
#define ERR_INVALID_HHEA_OR_VHEA 1072
#define ERR_INVALID_HMTX_OR_VMTX 1073
                                                                                                                             
#define ERR_INVALID_TTO 1080
#define ERR_INVALID_GSUB 1081
#define ERR_INVALID_GPOS 1082
#define ERR_INVALID_GDEF 1083
#define ERR_INVALID_JSTF 1084
#define ERR_INVALID_BASE 1085
#define ERR_INVALID_EBLC 1086
#define ERR_INVALID_LTSH 1087
#define    ERR_INVALID_VDMX 1088
#define    ERR_INVALID_HDMX 1089

#define ERR_PARAMETER0 1100  /* calling function argument 0 is invalid */
#define ERR_PARAMETER1 1101  /* calling function argument 1 is invalid */
#define ERR_PARAMETER2 1102  /* calling function argument 2 is invalid */
#define ERR_PARAMETER3 1103  /* calling function argument 3 is invalid */
#define ERR_PARAMETER4 1104  /* calling function argument 4 is invalid */
#define ERR_PARAMETER5 1105  /* calling function argument 5 is invalid */
#define ERR_PARAMETER6 1106  /* calling function argument 6 is invalid */
#define ERR_PARAMETER7 1107  /* calling function argument 7 is invalid */
#define ERR_PARAMETER8 1108  /* calling function argument 8 is invalid */
#define ERR_PARAMETER9 1109  /* calling function argument 9 is invalid */
#define ERR_PARAMETER10 1110  /* calling function argument 10 is invalid */
#define ERR_PARAMETER11 1111  /* calling function argument 11 is invalid */
#define ERR_PARAMETER12 1112  /* calling function argument 12 is invalid */
#define ERR_PARAMETER13 1113  /* calling function argument 13 is invalid */
#define ERR_PARAMETER14 1114  /* calling function argument 14 is invalid */
#define ERR_PARAMETER15 1115  /* calling function argument 15 is invalid */
#define ERR_PARAMETER16 1116  /* calling function argument 16 is invalid */


#endif /* TTFERROR_DOT_H_DEFINED */
