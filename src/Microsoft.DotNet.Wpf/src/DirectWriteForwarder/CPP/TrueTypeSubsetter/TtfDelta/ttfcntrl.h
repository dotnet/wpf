// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * TTFCntrl.h: Interface file for TTFCntrl.c - Written by Louise Pathe
  *
  *
  * 
  */
  /* NOTE: must include TYPEDEFS.H before this file */
  
#ifndef TTFCNTRL_DOT_H_DEFINED
#define TTFCNTRL_DOT_H_DEFINED        

extern uint8 BYTE_CONTROL[]; 
extern uint8 WORD_CONTROL[]; 
extern uint8 LONG_CONTROL[]; 

extern uint8 TTC_HEADER_CONTROL [];

extern uint8 OFFSET_TABLE_CONTROL [];

extern uint8 DIRECTORY_CONTROL[];

extern uint8 DIRECTORY_NO_XLATE_CONTROL[];

extern uint8 CMAP_HEADER_CONTROL[];

extern uint8 CMAP_TABLELOC_CONTROL[];

extern uint8 CMAP_SUBHEADER_CONTROL[];

extern uint8 CMAP_FORMAT0_CONTROL[];

extern uint8 CMAP_FORMAT6_CONTROL[];

extern uint8 CMAP_FORMAT4_CONTROL[];

extern uint8 FORMAT4_SEGMENTS_CONTROL[];

extern uint8 CMAP_FORMAT12_CONTROL[];

extern uint8 FORMAT12_GROUPS_CONTROL[];

    
/* 'post' postscript table */

extern uint8 POST_CONTROL[];

/* 'glyf' glyph data table */

extern uint8 GLYF_HEADER_CONTROL[];

extern uint8 SIMPLE_GLYPH_CONTROL[];

extern uint8 COMPOSITE_GLYPH_CONTROL[];

extern uint8 HEAD_CONTROL[];

/* 'hhea' horizontal header table */

extern uint8 HHEA_CONTROL[];

/* 'hmtx' horizontal metrics table */

extern uint8 LONGHORMETRIC_CONTROL[];

extern uint8 LSB_CONTROL[];

/* 'vhea' vertical header table */

extern uint8 VHEA_CONTROL[];

/* 'vmtx' vertical metrics table */

extern uint8 LONGVERMETRIC_CONTROL[];

extern uint8 TSB_CONTROL[];

/* generic 'hmtx', 'vmtx' tables */

extern uint8 XHEA_CONTROL[];

extern uint8 LONGXMETRIC_CONTROL[];

extern uint8 XSB_CONTROL[];

/* 'LTSH' linear threshold table */

extern uint8 LTSH_CONTROL[];

/* 'maxp' maximum profile table */

extern uint8 MAXP_CONTROL[];

extern uint8 NAME_RECORD_CONTROL[];

extern uint8     NAME_HEADER_CONTROL[];

/* 'hdmx' horizontal device metrix table */
          
extern uint8     HDMX_DEVICE_REC_CONTROL[];

extern uint8  HDMX_CONTROL[];

/* 'VDMX' Vertical Device Metrics Table */
extern uint8 VDMXVTABLE_CONTROL[];

extern uint8 VDMXGROUP_CONTROL[]; 

extern uint8 VDMXRATIO_CONTROL[]; 

extern uint8 VDMX_CONTROL[]; 

/* 'dttf' private table */

extern uint8 DTTF_HEADER_CONTROL[]; 

/* 'kern' kerning table */

extern uint8 KERN_HEADER_CONTROL[];

extern uint8 KERN_SUB_HEADER_CONTROL[];

extern uint8 KERN_FORMAT_0_CONTROL[];

extern uint8 KERN_PAIR_CONTROL[];

extern uint8 SEARCH_PAIRS_CONTROL[];

/* 'OS/2' OS/2 and Windows metrics table */

extern uint8 OS2_PANOSE_CONTROL[];

extern uint8 OS2_CONTROL[];

extern uint8 NEWOS2_CONTROL[];

extern uint8 VERSION2OS2_CONTROL[];

/*  EBLC, EBDT and EBSC file constants    */

/*    This first EBLC is common to both EBLC and EBSC tables */

extern uint8     EBLCHEADER_CONTROL[];

extern uint8     SBITLINEMETRICS_CONTROL[];

extern uint8 BITMAPSIZETABLE_CONTROL[];

extern uint8 BIGGLYPHMETRICS_CONTROL[];

extern uint8 SMALLGLYPHMETRICS_CONTROL[];

extern uint8 INDEXSUBTABLEARRAY_CONTROL[];

extern uint8 INDEXSUBHEADER_CONTROL[];

extern uint8 INDEXSUBTABLE1_CONTROL[];

extern uint8 INDEXSUBTABLE2_CONTROL[];

extern uint8 INDEXSUBTABLE3_CONTROL[];

extern uint8 CODEOFFSETPAIR_CONTROL[];

extern uint8 INDEXSUBTABLE4_CONTROL[];

extern uint8 INDEXSUBTABLE5_CONTROL[];

extern uint8 EBDTHEADER_CONTROL[];

extern uint8 EBDTHEADERNOXLATENOPAD_CONTROL[];

extern uint8 EBDTCOMPONENT_CONTROL[];

extern uint8 EBDTFORMAT8SIZE_CONTROL[];

extern uint8 EBDTFORMAT9_CONTROL[];

/* TrueType Open GSUB Tables, needed for Auto Mapping of unmapped Glyphs. */
extern uint8 GSUBFEATURE_CONTROL[];

extern uint8 GSUBFEATURERECORD_CONTROL[];

extern uint8 GSUBFEATURELIST_CONTROL[];

extern uint8 GSUBLOOKUP_CONTROL[];

extern uint8 GSUBLOOKUPLIST_CONTROL[];

extern uint8 GSUBCOVERAGEFORMAT1_CONTROL[];

extern uint8 GSUBRANGERECORD_CONTROL[];

extern uint8 GSUBCOVERAGEFORMAT2_CONTROL[];

extern uint8 GSUBHEADER_CONTROL[];

extern uint8 GSUBSINGLESUBSTFORMAT1_CONTROL[];

extern uint8 GSUBSINGLESUBSTFORMAT2_CONTROL[];

extern uint8 GSUBSEQUENCE_CONTROL[];

extern uint8 GSUBMULTIPLESUBSTFORMAT1_CONTROL[];

extern uint8 GSUBALTERNATESET_CONTROL[];

extern uint8 GSUBALTERNATESUBSTFORMAT1_CONTROL[];

extern uint8 GSUBLIGATURE_CONTROL[];

extern uint8 GSUBLIGATURESET_CONTROL[];

extern uint8 GSUBLIGATURESUBSTFORMAT1_CONTROL[];

extern uint8 GSUBSUBSTLOOKUPRECORD_CONTROL[];

extern uint8 GSUBSUBRULE_CONTROL[];

extern uint8 GSUBSUBRULESET_CONTROL[];

extern uint8 GSUBCONTEXTSUBSTFORMAT1_CONTROL[];

extern uint8 GSUBSUBCLASSRULE_CONTROL[];

extern uint8 GSUBSUBCLASSSET_CONTROL[];

extern uint8 GSUBCONTEXTSUBSTFORMAT2_CONTROL[];

extern uint8 GSUBCONTEXTSUBSTFORMAT3_CONTROL[];

/* just enough jstf info to get the Automap working for jstf */
extern uint8 JSTFSCRIPTRECORD_CONTROL[];

extern uint8 JSTFHEADER_CONTROL[];

extern uint8 JSTFLANGSYSRECORD_CONTROL[];

extern uint8 JSTFSCRIPT_CONTROL[];

extern uint8 JSTFEXTENDERGLYPH_CONTROL[];

/* BASE TTO Table, enough to do TTOAutoMap */

extern uint8 BASEHEADER_CONTROL[];

extern uint8 BASEAXIS_CONTROL[];

extern uint8 BASESCRIPTRECORD_CONTROL[];

extern uint8 BASESCRIPTLIST_CONTROL[];

extern uint8 BASELANGSYSRECORD_CONTROL[];

extern uint8 BASESCRIPT_CONTROL[];

extern uint8 BASEVALUES_CONTROL[];

extern uint8 BASEFEATMINMAXRECORD_CONTROL[];

extern uint8 BASEMINMAX_CONTROL[];

extern uint8 BASECOORDFORMAT2_CONTROL[];

extern uint8 MORTBINSRCHHEADER_CONTROL[];

extern uint8 MORTLOOKUPSINGLE_CONTROL[];

extern uint8 MORTHEADER_CONTROL[];

#endif /* TTFCNTRL_DOT_H_DEFINED */
