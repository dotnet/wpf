// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFCNTRL.C
 *
 *
 * companion to TTFF.h for generic platform independant read and swap handling 
 * if TTFF.h is updated for platform performance reasons, this should be updated
 * as well 
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */


#include "typedefs.h"
#include "ttff.h" /* for TESTPORT definition */
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "GlobalInit.h"

uint8 BYTE_CONTROL[2];
uint8 WORD_CONTROL[2];
uint8 LONG_CONTROL[2];
/* TTC header */
#define TTC_HEADER_CONTROL_COUNT 3
uint8 TTC_HEADER_CONTROL [TTC_HEADER_CONTROL_COUNT+1];

#define OFFSET_TABLE_CONTROL_COUNT 5
uint8 OFFSET_TABLE_CONTROL [OFFSET_TABLE_CONTROL_COUNT+1];

#define DIRECTORY_CONTROL_COUNT 4
uint8 DIRECTORY_CONTROL[DIRECTORY_CONTROL_COUNT+1];

#define DIRECTORY_NO_XLATE_CONTROL_COUNT 4      /* no translation */
uint8 DIRECTORY_NO_XLATE_CONTROL[DIRECTORY_NO_XLATE_CONTROL_COUNT+1];

#define CMAP_HEADER_CONTROL_COUNT 2
uint8 CMAP_HEADER_CONTROL[CMAP_HEADER_CONTROL_COUNT+1]; /*  CMAP_HEADER */

#define CMAP_TABLELOC_CONTROL_COUNT 3
uint8 CMAP_TABLELOC_CONTROL[CMAP_TABLELOC_CONTROL_COUNT+1]; /*  CMAP_TABLELOC */

#define CMAP_SUBHEADER_CONTROL_COUNT 3 
uint8 CMAP_SUBHEADER_CONTROL[CMAP_SUBHEADER_CONTROL_COUNT+1]; /*  CMAP_SUBHEADER */

#define CMAP_FORMAT0_CONTROL_COUNT 3
uint8 CMAP_FORMAT0_CONTROL[CMAP_FORMAT0_CONTROL_COUNT+1]; /*  CMAP_FORMAT0 */

#define CMAP_FORMAT6_CONTROL_COUNT 5
uint8 CMAP_FORMAT6_CONTROL[CMAP_FORMAT6_CONTROL_COUNT+1]; /*  CMAP_FORMAT6 */

#define CMAP_FORMAT4_CONTROL_COUNT 7
uint8 CMAP_FORMAT4_CONTROL[CMAP_FORMAT4_CONTROL_COUNT+1]; /*  CMAP_FORMAT4 */

#define FORMAT4_SEGMENTS_CONTROL_COUNT 4
uint8 FORMAT4_SEGMENTS_CONTROL[FORMAT4_SEGMENTS_CONTROL_COUNT+1]; /*  FORMAT4_SEGMENTS */

#define CMAP_FORMAT12_CONTROL_COUNT 5
uint8 CMAP_FORMAT12_CONTROL[CMAP_FORMAT12_CONTROL_COUNT+1]; /*  CMAP_FORMAT12 */

#define FORMAT12_GROUPS_CONTROL_COUNT 3
uint8 FORMAT12_GROUPS_CONTROL[FORMAT12_GROUPS_CONTROL_COUNT+1]; /*  FORMAT12_GROUPS */

/* 'post' postscript table */

#define POST_CONTROL_COUNT 9
uint8 POST_CONTROL[POST_CONTROL_COUNT+1]; /*  POST */

/* 'glyf' glyph data table */

#define GLYF_HEADER_CONTROL_COUNT 5
uint8 GLYF_HEADER_CONTROL[GLYF_HEADER_CONTROL_COUNT+1]; /*  GLYF_HEADER */

#define SIMPLE_GLYPH_CONTROL_COUNT 5
uint8 SIMPLE_GLYPH_CONTROL[SIMPLE_GLYPH_CONTROL_COUNT+1]; /*  SIMPLE_GLYPH */

#define COMPOSITE_GLYPH_CONTROL_COUNT 1
uint8 COMPOSITE_GLYPH_CONTROL[COMPOSITE_GLYPH_CONTROL_COUNT+1]; /*  COMPOSITE_GLYPH */

#define HEAD_CONTROL_COUNT 19 
uint8 HEAD_CONTROL[HEAD_CONTROL_COUNT+1]; /*  HEAD */

/* 'hhea' horizontal header table */

#define HHEA_CONTROL_COUNT 17
uint8 HHEA_CONTROL[HHEA_CONTROL_COUNT+1]; /*  HHEA */

/* 'hmtx' horizontal metrics table */

#define LONGHORMETRIC_CONTROL_COUNT 2
uint8 LONGHORMETRIC_CONTROL[LONGHORMETRIC_CONTROL_COUNT+1];/*  LONGHORMETRIC */

#define LSB_CONTROL_COUNT 1
uint8 LSB_CONTROL[LSB_CONTROL_COUNT+1];

/* 'vhea' horizontal header table */

#define  VHEA_CONTROL_COUNT 17
uint8 VHEA_CONTROL[VHEA_CONTROL_COUNT+1]; /*  VHEA */

/* 'hmtx' horizontal metrics table */

#define LONGVERMETRIC_CONTROL_COUNT 2
uint8 LONGVERMETRIC_CONTROL[LONGVERMETRIC_CONTROL_COUNT+1]; /*  LONGVERMETRIC */

/* generic 'hmtx', 'vmtx' tables */
#define XHEA_CONTROL_COUNT 17
uint8 XHEA_CONTROL[XHEA_CONTROL_COUNT+1]; /*  XHEA */

#define LONGXMETRIC_CONTROL_COUNT 2
uint8 LONGXMETRIC_CONTROL[LONGXMETRIC_CONTROL_COUNT+1]; /*  LONGXMETRIC */

#define XSB_CONTROL_COUNT 1
uint8 XSB_CONTROL[XSB_CONTROL_COUNT+1];

#define TSB_CONTROL_COUNT 1
uint8 TSB_CONTROL[TSB_CONTROL_COUNT+1];

/* 'LTSH' linear threshold table */

#define LTSH_CONTROL_COUNT 2
uint8 LTSH_CONTROL[LTSH_CONTROL_COUNT+1];  /*  LTSH */

/* 'maxp' maximum profile table */

#define MAXP_CONTROL_COUNT 15
uint8 MAXP_CONTROL[MAXP_CONTROL_COUNT+1]; /*  MAXP */

#define NAME_RECORD_CONTROL_COUNT 6
uint8 NAME_RECORD_CONTROL[NAME_RECORD_CONTROL_COUNT+1]; /*  NAME_RECORD */

#define NAME_HEADER_CONTROL_COUNT 3
uint8 NAME_HEADER_CONTROL[NAME_HEADER_CONTROL_COUNT+1]; /*  NAME_HEADER */

/* 'hdmx' horizontal device metrix table */
          
#define HDMX_DEVICE_REC_CONTROL_COUNT 2
uint8 HDMX_DEVICE_REC_CONTROL[HDMX_DEVICE_REC_CONTROL_COUNT+1]; /*  HDMX_DEVICE_REC */

#define HDMX_CONTROL_COUNT 3
uint8 HDMX_CONTROL[HDMX_CONTROL_COUNT+1]; /*  HDMX */

/* 'VDMX' Vertical Device Metrics Table */
#define VDMXVTABLE_CONTROL_COUNT 4
uint8 VDMXVTABLE_CONTROL[VDMXVTABLE_CONTROL_COUNT+1]; /* VDMXvTable */

#define VDMXGROUP_CONTROL_COUNT 3 
uint8 VDMXGROUP_CONTROL[VDMXGROUP_CONTROL_COUNT+1];  /* VDMXGroup */

#define VDMXRATIO_CONTROL_COUNT 4 
uint8 VDMXRATIO_CONTROL[VDMXRATIO_CONTROL_COUNT+1];  /* VDMXRatio */

#define VDMX_CONTROL_COUNT 3
uint8 VDMX_CONTROL[VDMX_CONTROL_COUNT+1]; /* VDMX */

/* 'dttf' delta ttf table */
#define DTTF_HEADER_CONTROL_COUNT 7
uint8 DTTF_HEADER_CONTROL[DTTF_HEADER_CONTROL_COUNT+1];
/* end 'dttf' delta ttf table */

/* 'kern' kerning table */

#define KERN_HEADER_CONTROL_COUNT 2
uint8 KERN_HEADER_CONTROL[KERN_HEADER_CONTROL_COUNT+1]; /*  KERN_HEADER */

#define KERN_SUB_HEADER_CONTROL_COUNT 4
uint8 KERN_SUB_HEADER_CONTROL[KERN_SUB_HEADER_CONTROL_COUNT+1]; /*  KERN_SUB_HEADER */

#define  KERN_FORMAT_0_CONTROL_COUNT 4
uint8 KERN_FORMAT_0_CONTROL[KERN_FORMAT_0_CONTROL_COUNT+1]; /*  KERN_FORMAT_0 */

#define KERN_PAIR_CONTROL_COUNT 4
uint8 KERN_PAIR_CONTROL[KERN_PAIR_CONTROL_COUNT+1]; /*  KERN_PAIR */

#define SEARCH_PAIRS_CONTROL_COUNT 3
uint8 SEARCH_PAIRS_CONTROL[SEARCH_PAIRS_CONTROL_COUNT+1]; /*  SEARCH_PAIRS */

/* 'OS/2' OS/2 and Windows metrics table */

#define OS2_PANOSE_CONTROL_COUNT 10
uint8 OS2_PANOSE_CONTROL[OS2_PANOSE_CONTROL_COUNT+1]; /*  OS2_PANOSE */

#define OS2_CONTROL_COUNT 43
uint8 OS2_CONTROL[OS2_CONTROL_COUNT+1]; /*  OS2 */

#define NEWOS2_CONTROL_COUNT 45
uint8 NEWOS2_CONTROL[NEWOS2_CONTROL_COUNT+1]; /*  NEWOS2 */

#define VERSION2OS2_CONTROL_COUNT 50
uint8 VERSION2OS2_CONTROL[VERSION2OS2_CONTROL_COUNT+1]; /*  VERSION2OS2 */

/*  EBLC, EBDT and EBSC file constants    */

/*    This first EBLC is common to both EBLC and EBSC tables */

#define EBLCHEADER_CONTROL_COUNT 2
uint8 EBLCHEADER_CONTROL[EBLCHEADER_CONTROL_COUNT+1]; /*  EBLCHEADER */

#define SBITLINEMETRICS_CONTROL_COUNT 12
uint8 SBITLINEMETRICS_CONTROL[SBITLINEMETRICS_CONTROL_COUNT+1]; /*  SBITLINEMETRICS */

#ifdef TESTPORT
#define BITMAPSIZETABLE_CONTROL_COUNT 36
#else
#define BITMAPSIZETABLE_CONTROL_COUNT 34
#endif
uint8 BITMAPSIZETABLE_CONTROL[BITMAPSIZETABLE_CONTROL_COUNT+1]; /*  BITMAPSIZETABLE */

#define BIGGLYPHMETRICS_CONTROL_COUNT 8
uint8 BIGGLYPHMETRICS_CONTROL[BIGGLYPHMETRICS_CONTROL_COUNT+1]; /*  BIGGLYPHMETRICS */

#define SMALLGLYPHMETRICS_CONTROL_COUNT 5
uint8 SMALLGLYPHMETRICS_CONTROL[SMALLGLYPHMETRICS_CONTROL_COUNT+1]; /*  SMALLGLYPHMETRICS */

#ifdef TESTPORT
#define INDEXSUBTABLEARRAY_CONTROL_COUNT 5
#else
#define INDEXSUBTABLEARRAY_CONTROL_COUNT 3
#endif
uint8 INDEXSUBTABLEARRAY_CONTROL[INDEXSUBTABLEARRAY_CONTROL_COUNT+1]; /*  INDEXSUBTABLEARRAY */

#ifdef TESTPORT
#define INDEXSUBHEADER_CONTROL_COUNT 5
#else
#define INDEXSUBHEADER_CONTROL_COUNT 3
#endif
uint8 INDEXSUBHEADER_CONTROL[INDEXSUBHEADER_CONTROL_COUNT+1]; /*  INDEXSUBHEADER */

#ifdef TESTPORT
#define INDEXSUBTABLE1_CONTROL_COUNT 7
#else
#define INDEXSUBTABLE1_CONTROL_COUNT 3
#endif
uint8 INDEXSUBTABLE1_CONTROL[INDEXSUBTABLE1_CONTROL_COUNT+1]; /*  INDEXSUBTABLE1 */

#ifdef TESTPORT
#define INDEXSUBTABLE2_CONTROL_COUNT 16
#else
#define INDEXSUBTABLE2_CONTROL_COUNT 12
#endif
uint8 INDEXSUBTABLE2_CONTROL[INDEXSUBTABLE2_CONTROL_COUNT+1]; /*  INDEXSUBTABLE2 */

#ifdef TESTPORT
#define INDEXSUBTABLE3_CONTROL_COUNT 7
#else
#define INDEXSUBTABLE3_CONTROL_COUNT 3
#endif
uint8 INDEXSUBTABLE3_CONTROL[INDEXSUBTABLE3_CONTROL_COUNT+1]; /*  INDEXSUBTABLE3 */

#ifdef TESTPORT
#define  CODEOFFSETPAIR_CONTROL_COUNT 4
#else
#define  CODEOFFSETPAIR_CONTROL_COUNT 2
#endif
uint8 CODEOFFSETPAIR_CONTROL[CODEOFFSETPAIR_CONTROL_COUNT+1]; /*  CODEOFFSETPAIR */

#ifdef TESTPORT
#define INDEXSUBTABLE4_CONTROL_COUNT 8
#else
#define INDEXSUBTABLE4_CONTROL_COUNT 4
#endif
uint8 INDEXSUBTABLE4_CONTROL[INDEXSUBTABLE4_CONTROL_COUNT+1]; /*  INDEXSUBTABLE4 */

#ifdef TESTPORT
#define INDEXSUBTABLE5_CONTROL_COUNT 17
#else
#define INDEXSUBTABLE5_CONTROL_COUNT 13
#endif
uint8 INDEXSUBTABLE5_CONTROL[INDEXSUBTABLE5_CONTROL_COUNT+1]; /*  INDEXSUBTABLE5 */

#define EBDTHEADER_CONTROL_COUNT 1
uint8 EBDTHEADER_CONTROL[EBDTHEADER_CONTROL_COUNT+1]; /*  EBDTHEADER */

#define EBDTHEADERNOXLATENOPAD_CONTROL_COUNT 1
uint8 EBDTHEADERNOXLATENOPAD_CONTROL[EBDTHEADERNOXLATENOPAD_CONTROL_COUNT+1]; /*  EBDTHEADER */

#define EBDTCOMPONENT_CONTROL_COUNT 3
uint8 EBDTCOMPONENT_CONTROL[EBDTCOMPONENT_CONTROL_COUNT+1]; /*  EBDTCOMPONENT */

#define EBDTFORMAT8SIZE_CONTROL_COUNT 7
uint8 EBDTFORMAT8SIZE_CONTROL[EBDTFORMAT8SIZE_CONTROL_COUNT+1]; /*  EBDTFORMAT8 */

#define  EBDTFORMAT9_CONTROL_COUNT 10
uint8 EBDTFORMAT9_CONTROL[EBDTFORMAT9_CONTROL_COUNT+1]; /*  EBDTFORMAT9 */

/* TrueType Open GSUB Tables, needed for Auto Mapping of unmapped Glyphs. */
#define GSUBFEATURE_CONTROL_COUNT 3
uint8 GSUBFEATURE_CONTROL[GSUBFEATURE_CONTROL_COUNT+1]; /*  GSUBFEATURE */

#define GSUBFEATURERECORD_CONTROL_COUNT 3
uint8 GSUBFEATURERECORD_CONTROL[GSUBFEATURERECORD_CONTROL_COUNT+1]; /*  GSUBFEATURERECORD */

#define   GSUBFEATURELIST_CONTROL_COUNT 2
uint8 GSUBFEATURELIST_CONTROL[GSUBFEATURELIST_CONTROL_COUNT+1]; /*  GSUBFEATURELIST */

#define GSUBLOOKUP_CONTROL_COUNT 3
uint8 GSUBLOOKUP_CONTROL[GSUBLOOKUP_CONTROL_COUNT+1]; /*  GSUBLOOKUP */

#define GSUBLOOKUPLIST_CONTROL_COUNT 1
uint8 GSUBLOOKUPLIST_CONTROL[GSUBLOOKUPLIST_CONTROL_COUNT+1]; /*  GSUBLOOKUPLIST */

#define GSUBCOVERAGEFORMAT1_CONTROL_COUNT 2
uint8 GSUBCOVERAGEFORMAT1_CONTROL[GSUBCOVERAGEFORMAT1_CONTROL_COUNT+1]; /*  GSUBCOVERAGEFORMAT1 */

#define GSUBRANGERECORD_CONTROL_COUNT 4
uint8 GSUBRANGERECORD_CONTROL[GSUBRANGERECORD_CONTROL_COUNT+1]; /*  GSUBRANGERECORD */

#define GSUBCOVERAGEFORMAT2_CONTROL_COUNT 2
uint8 GSUBCOVERAGEFORMAT2_CONTROL[GSUBCOVERAGEFORMAT2_CONTROL_COUNT+1]; /*  GSUBCOVERAGEFORMAT2 */

#define GSUBHEADER_CONTROL_COUNT 4
uint8 GSUBHEADER_CONTROL[GSUBHEADER_CONTROL_COUNT+1]; /*  GSUBHEADER */

#define GSUBSINGLESUBSTFORMAT1_CONTROL_COUNT 3
uint8 GSUBSINGLESUBSTFORMAT1_CONTROL[GSUBSINGLESUBSTFORMAT1_CONTROL_COUNT+1]; /*  GSUBSINGLESUBSTFORMAT1 */

#define GSUBSINGLESUBSTFORMAT2_CONTROL_COUNT 3
uint8 GSUBSINGLESUBSTFORMAT2_CONTROL[GSUBSINGLESUBSTFORMAT2_CONTROL_COUNT+1]; /*  GSUBSINGLESUBSTFORMAT2 */

#define GSUBSEQUENCE_CONTROL_COUNT 1
uint8 GSUBSEQUENCE_CONTROL[GSUBSEQUENCE_CONTROL_COUNT+1]; /*  GSUBSEQUENCE */

#define GSUBMULTIPLESUBSTFORMAT1_CONTROL_COUNT 3
uint8 GSUBMULTIPLESUBSTFORMAT1_CONTROL[GSUBMULTIPLESUBSTFORMAT1_CONTROL_COUNT+1]; /*  GSUBMULTIPLESUBSTFORMAT1 */

#define GSUBALTERNATESET_CONTROL_COUNT 1
uint8 GSUBALTERNATESET_CONTROL[GSUBALTERNATESET_CONTROL_COUNT+1]; /*  GSUBALTERNATESET */

#define GSUBALTERNATESUBSTFORMAT1_CONTROL_COUNT 3
uint8 GSUBALTERNATESUBSTFORMAT1_CONTROL[GSUBALTERNATESUBSTFORMAT1_CONTROL_COUNT+1]; /*  GSUBALTERNATESUBSTFORMAT1 */

#define GSUBLIGATURE_CONTROL_COUNT 2
uint8 GSUBLIGATURE_CONTROL[GSUBLIGATURE_CONTROL_COUNT+1]; /*  GSUBLIGATURE */

#define GSUBLIGATURESET_CONTROL_COUNT 1
uint8 GSUBLIGATURESET_CONTROL[GSUBLIGATURESET_CONTROL_COUNT+1]; /*  GSUBLIGATURESET */

#define GSUBLIGATURESUBSTFORMAT1_CONTROL_COUNT 3
uint8 GSUBLIGATURESUBSTFORMAT1_CONTROL[GSUBLIGATURESUBSTFORMAT1_CONTROL_COUNT+1]; /*  GSUBLIGATURESUBSTFORMAT1 */

#define GSUBSUBSTLOOKUPRECORD_CONTROL_COUNT 2
uint8 GSUBSUBSTLOOKUPRECORD_CONTROL[GSUBSUBSTLOOKUPRECORD_CONTROL_COUNT+1]; /*  GSUBSUBSTLOOKUPRECORD */

#define GSUBSUBRULE_CONTROL_COUNT 2
uint8 GSUBSUBRULE_CONTROL[GSUBSUBRULE_CONTROL_COUNT+1]; /*  GSUBSUBRULE */

#define GSUBSUBRULESET_CONTROL_COUNT 1
uint8 GSUBSUBRULESET_CONTROL[GSUBSUBRULESET_CONTROL_COUNT+1]; /*  GSUBSUBRULESET */

#define GSUBCONTEXTSUBSTFORMAT1_CONTROL_COUNT 3 
uint8 GSUBCONTEXTSUBSTFORMAT1_CONTROL[GSUBCONTEXTSUBSTFORMAT1_CONTROL_COUNT+1]; /*  GSUBCONTEXTSUBSTFORMAT1 */

#define GSUBSUBCLASSRULE_CONTROL_COUNT 2
uint8 GSUBSUBCLASSRULE_CONTROL[GSUBSUBCLASSRULE_CONTROL_COUNT+1]; /*  GSUBSUBCLASSRULE */

#define  GSUBSUBCLASSSET_CONTROL_COUNT 1
uint8 GSUBSUBCLASSSET_CONTROL[GSUBSUBCLASSSET_CONTROL_COUNT+1]; /*  GSUBSUBCLASSSET */

#define GSUBCONTEXTSUBSTFORMAT2_CONTROL_COUNT 4
uint8 GSUBCONTEXTSUBSTFORMAT2_CONTROL[GSUBCONTEXTSUBSTFORMAT2_CONTROL_COUNT+1]; /*  GSUBCONTEXTSUBSTFORMAT2 */


#define  GSUBCONTEXTSUBSTFORMAT3_CONTROL_COUNT 3
uint8 GSUBCONTEXTSUBSTFORMAT3_CONTROL[GSUBCONTEXTSUBSTFORMAT3_CONTROL_COUNT+1]; /*  GSUBCONTEXTSUBSTFORMAT3 */

/* just enough jstf info to get the Automap working for jstf */
#define JSTFSCRIPTRECORD_CONTROL_COUNT 3
uint8 JSTFSCRIPTRECORD_CONTROL[JSTFSCRIPTRECORD_CONTROL_COUNT+1]; /*  JSTFSCRIPTRECORD */

#define JSTFHEADER_CONTROL_COUNT 3
uint8 JSTFHEADER_CONTROL[JSTFHEADER_CONTROL_COUNT+1]; /*  JSTFHEADER */

#define JSTFLANGSYSRECORD_CONTROL_COUNT 3
uint8 JSTFLANGSYSRECORD_CONTROL[JSTFLANGSYSRECORD_CONTROL_COUNT+1]; /*  JSTFLANGSYSRECORD */

#define JSTFSCRIPT_CONTROL_COUNT 4
uint8 JSTFSCRIPT_CONTROL[JSTFSCRIPT_CONTROL_COUNT+1]; /*  JSTFSCRIPT */

#define JSTFEXTENDERGLYPH_CONTROL_COUNT 1
uint8 JSTFEXTENDERGLYPH_CONTROL[JSTFEXTENDERGLYPH_CONTROL_COUNT+1]; /*  JSTFEXTENDERGLYPH */

/* BASE TTO Table, enough to do TTOAutoMap */

#define BASEHEADER_CONTROL_COUNT 3
uint8 BASEHEADER_CONTROL[BASEHEADER_CONTROL_COUNT+1]; /*  BASEHEADER */

#define BASEAXIS_CONTROL_COUNT 2
uint8 BASEAXIS_CONTROL[BASEAXIS_CONTROL_COUNT+1]; /*  BASEAXIS */

#define BASESCRIPTRECORD_CONTROL_COUNT 3
uint8 BASESCRIPTRECORD_CONTROL[BASESCRIPTRECORD_CONTROL_COUNT+1]; /*  BASESCRIPTRECORD */

#define BASESCRIPTLIST_CONTROL_COUNT 2
uint8 BASESCRIPTLIST_CONTROL[BASESCRIPTLIST_CONTROL_COUNT+1]; /*  BASESCRIPTLIST */

#define BASELANGSYSRECORD_CONTROL_COUNT 3
uint8 BASELANGSYSRECORD_CONTROL[BASELANGSYSRECORD_CONTROL_COUNT+1]; /*  BASELANGSYSRECORD */

#define BASESCRIPT_CONTROL_COUNT 4
uint8 BASESCRIPT_CONTROL[BASESCRIPT_CONTROL_COUNT+1]; /*  BASESCRIPT */

#define  BASEVALUES_CONTROL_COUNT 2
uint8 BASEVALUES_CONTROL[BASEVALUES_CONTROL_COUNT+1]; /*  BASEVALUES */

#define BASEFEATMINMAXRECORD_CONTROL_COUNT 3
uint8 BASEFEATMINMAXRECORD_CONTROL[BASEFEATMINMAXRECORD_CONTROL_COUNT+1]; /*  BASEFEATMINMAXRECORD */

#define BASEMINMAX_CONTROL_COUNT 4 
uint8 BASEMINMAX_CONTROL[BASEMINMAX_CONTROL_COUNT+1]; /*  BASEMINMAX */

#define BASECOORDFORMAT2_CONTROL_COUNT 4
uint8 BASECOORDFORMAT2_CONTROL[BASECOORDFORMAT2_CONTROL_COUNT+1]; /*  BASECOORDFORMAT2 */

/* Glyph Metamorphosis table (mort) structures */
#define MORTBINSRCHHEADER_CONTROL_COUNT    5
uint8 MORTBINSRCHHEADER_CONTROL[MORTBINSRCHHEADER_CONTROL_COUNT+1]; /*  BINSRCHHEADER */

#define MORTLOOKUPSINGLE_CONTROL_COUNT 2
uint8 MORTLOOKUPSINGLE_CONTROL[MORTLOOKUPSINGLE_CONTROL_COUNT + 1];  /* LOOKUPSINGLE */

#define MORTHEADER_CONTROL_COUNT 62
uint8 MORTHEADER_CONTROL[MORTHEADER_CONTROL_COUNT+1]; /* MORTTABLE */

// We use this method to initialize some global data. This is done because if we left
// the global initializations to the compiler it will generate some static methods that
// are not properly annotated with security tags. This was causing these complier generated 
// methods to fail NGEN and be Jitted causing significant startup perf regressions.
// This method has to be made SecurityCritical so that NGEN can process it!
// It contains safe code.
void GlobalInit::Init()
{
    if (!_isInitialized)
    {
        System::Threading::Monitor::Enter(_staticLock);
        try
        {
            if (!_isInitialized)
            {
                int i = 0;
                BYTE_CONTROL[i++] = 1;
                BYTE_CONTROL[i++] = TTFACC_BYTE;

                i = 0;
                WORD_CONTROL[i++] = 1;
                WORD_CONTROL[i++] = TTFACC_WORD;

                i = 0;
                LONG_CONTROL[i++] = 1;
                LONG_CONTROL[i++] = TTFACC_LONG;

                /* TTC_HEADER */
                i = 0;
                TTC_HEADER_CONTROL[i++] = TTC_HEADER_CONTROL_COUNT;
                TTC_HEADER_CONTROL[i++] = TTFACC_LONG; /* TTCTag */
                TTC_HEADER_CONTROL[i++] = TTFACC_LONG; /*  version */
                TTC_HEADER_CONTROL[i++] = TTFACC_LONG; /*  DirectoryCount */
                /* ULONG TableDirectoryOffset */

                i = 0;
                OFFSET_TABLE_CONTROL[i++] = OFFSET_TABLE_CONTROL_COUNT;
                OFFSET_TABLE_CONTROL[i++] = TTFACC_LONG; /* Fixed   version */
                OFFSET_TABLE_CONTROL[i++] = TTFACC_WORD; /* TTFACC_WORD, /*  numTables */
                OFFSET_TABLE_CONTROL[i++] = TTFACC_WORD; /* TTFACC_WORD, /*  searchRange */    
                OFFSET_TABLE_CONTROL[i++] = TTFACC_WORD; /* TTFACC_WORD, /*  entrySelector */ 
                OFFSET_TABLE_CONTROL[i++] = TTFACC_WORD; /* TTFACC_WORD, /*  rangeShift */

                i = 0;
                DIRECTORY_CONTROL[i++] = DIRECTORY_CONTROL_COUNT;
                DIRECTORY_CONTROL[i++] = TTFACC_LONG;  /* TTFACC_LONG, /*  tag */
                DIRECTORY_CONTROL[i++] = TTFACC_LONG;  /* TTFACC_LONG, /*  checkSum */
                DIRECTORY_CONTROL[i++] = TTFACC_LONG;  /* TTFACC_LONG, /*  offset */
                DIRECTORY_CONTROL[i++] = TTFACC_LONG;  /* TTFACC_LONG, /*  length */

                i = 0;
                DIRECTORY_NO_XLATE_CONTROL[i++] = DIRECTORY_NO_XLATE_CONTROL_COUNT;
                DIRECTORY_NO_XLATE_CONTROL[i++] = TTFACC_LONG|TTFACC_NO_XLATE;  /* TTFACC_LONG, /*  tag */
                DIRECTORY_NO_XLATE_CONTROL[i++] = TTFACC_LONG|TTFACC_NO_XLATE;  /* TTFACC_LONG, /*  checkSum */
                DIRECTORY_NO_XLATE_CONTROL[i++] = TTFACC_LONG|TTFACC_NO_XLATE;  /* TTFACC_LONG, /*  offset */
                DIRECTORY_NO_XLATE_CONTROL[i++] = TTFACC_LONG|TTFACC_NO_XLATE;  /* TTFACC_LONG, /*  length */

                i = 0;
                CMAP_HEADER_CONTROL[i++] = CMAP_HEADER_CONTROL_COUNT;
                CMAP_HEADER_CONTROL[i++] = TTFACC_WORD; /*  versionNumber */
                CMAP_HEADER_CONTROL[i++] = TTFACC_WORD;  /*  numTables */

                i = 0;
                CMAP_TABLELOC_CONTROL[i++] = CMAP_TABLELOC_CONTROL_COUNT;
                CMAP_TABLELOC_CONTROL[i++] = TTFACC_WORD; /*  platformID */
                CMAP_TABLELOC_CONTROL[i++] = TTFACC_WORD; /*  encodingID */
                CMAP_TABLELOC_CONTROL[i++] = TTFACC_LONG; /*   offset */

                i = 0;
                CMAP_SUBHEADER_CONTROL[i++] = CMAP_SUBHEADER_CONTROL_COUNT;
                CMAP_SUBHEADER_CONTROL[i++] = TTFACC_WORD; /*  format */
                CMAP_SUBHEADER_CONTROL[i++] = TTFACC_WORD; /*  length for tables */
                CMAP_SUBHEADER_CONTROL[i++] = TTFACC_WORD; /*  revision */

                i = 0;
                CMAP_FORMAT0_CONTROL[i++] = CMAP_FORMAT0_CONTROL_COUNT;
                CMAP_FORMAT0_CONTROL[i++] = TTFACC_WORD; /*  format */
                CMAP_FORMAT0_CONTROL[i++] = TTFACC_WORD; /*  length */
                CMAP_FORMAT0_CONTROL[i++] = TTFACC_WORD;  /*  revision */

                i = 0;
                CMAP_FORMAT6_CONTROL[i++] = CMAP_FORMAT6_CONTROL_COUNT;
                CMAP_FORMAT6_CONTROL[i++] = TTFACC_WORD; /*  format */
                CMAP_FORMAT6_CONTROL[i++] = TTFACC_WORD; /*  length */
                CMAP_FORMAT6_CONTROL[i++] = TTFACC_WORD; /*  revision */
                CMAP_FORMAT6_CONTROL[i++] = TTFACC_WORD; /*  firstCode */
                CMAP_FORMAT6_CONTROL[i++] = TTFACC_WORD;  /*  entryCount */

                i = 0;
                CMAP_FORMAT4_CONTROL[i++] = CMAP_FORMAT4_CONTROL_COUNT;
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  format */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  length */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  revision */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  segCountX2 */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  searchRange */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD; /*  entrySelector */
                CMAP_FORMAT4_CONTROL[i++] = TTFACC_WORD;  /*  rangeShift */

                i = 0;
                FORMAT4_SEGMENTS_CONTROL[i++] = FORMAT4_SEGMENTS_CONTROL_COUNT;
                FORMAT4_SEGMENTS_CONTROL[i++] = TTFACC_WORD; /*  endCount */
                FORMAT4_SEGMENTS_CONTROL[i++] = TTFACC_WORD; /*  startCount */
                FORMAT4_SEGMENTS_CONTROL[i++] = TTFACC_WORD; /*   idDelta */
                FORMAT4_SEGMENTS_CONTROL[i++] = TTFACC_WORD;  /*  idRangeOffset */

                i = 0;
                CMAP_FORMAT12_CONTROL[i++] = CMAP_FORMAT12_CONTROL_COUNT;
                CMAP_FORMAT12_CONTROL[i++] = TTFACC_WORD; /*  format */
                CMAP_FORMAT12_CONTROL[i++] = TTFACC_WORD; /*  revision */
                CMAP_FORMAT12_CONTROL[i++] = TTFACC_LONG; /*  length */
                CMAP_FORMAT12_CONTROL[i++] = TTFACC_LONG; /*  language */
                CMAP_FORMAT12_CONTROL[i++] = TTFACC_LONG; /*  nGroups */

                i = 0;
                FORMAT12_GROUPS_CONTROL[i++] = FORMAT12_GROUPS_CONTROL_COUNT;
                FORMAT12_GROUPS_CONTROL[i++] = TTFACC_LONG; /*  startCharCode */
                FORMAT12_GROUPS_CONTROL[i++] = TTFACC_LONG; /*  endCharCode */
                FORMAT12_GROUPS_CONTROL[i++] = TTFACC_LONG; /*  startGlyphCode */

                i = 0;
                POST_CONTROL[i++] = POST_CONTROL_COUNT;
                POST_CONTROL[i++] = TTFACC_LONG; /*  formatType */
                POST_CONTROL[i++] = TTFACC_LONG; /*  italicAngle */
                POST_CONTROL[i++] = TTFACC_WORD; /* underlinePos */
                POST_CONTROL[i++] = TTFACC_WORD; /* underlineThickness */
                POST_CONTROL[i++] = TTFACC_LONG; /*  isTTFACC_LONG, /*Pitch */
                POST_CONTROL[i++] = TTFACC_LONG; /*  minMemType42 */
                POST_CONTROL[i++] = TTFACC_LONG; /*  maxMemType42 */
                POST_CONTROL[i++] = TTFACC_LONG; /*  minMemType1 */
                POST_CONTROL[i++] = TTFACC_LONG;  /*  maxMemType1 */

                i = 0;
                GLYF_HEADER_CONTROL[i++] = GLYF_HEADER_CONTROL_COUNT;
                GLYF_HEADER_CONTROL[i++] = TTFACC_WORD; /*  numberOfContours */
                GLYF_HEADER_CONTROL[i++] = TTFACC_WORD; /* xMin */
                GLYF_HEADER_CONTROL[i++] = TTFACC_WORD; /* yMin */
                GLYF_HEADER_CONTROL[i++] = TTFACC_WORD; /* xMax */
                GLYF_HEADER_CONTROL[i++] = TTFACC_WORD; /* yMax */

                i = 0;
                SIMPLE_GLYPH_CONTROL[i++] = SIMPLE_GLYPH_CONTROL_COUNT;
                SIMPLE_GLYPH_CONTROL[i++] = TTFACC_WORD; /* *endPtsOfContours */
                SIMPLE_GLYPH_CONTROL[i++] = TTFACC_WORD; /* instructionLength */
                SIMPLE_GLYPH_CONTROL[i++] = TTFACC_BYTE; /*   *instructions */
                SIMPLE_GLYPH_CONTROL[i++] = TTFACC_BYTE; /*   *flags */
                SIMPLE_GLYPH_CONTROL[i++] = TTFACC_BYTE; /*   *Coordinates */       /* length of x,y coord's depends on flags */

                i = 0;
                COMPOSITE_GLYPH_CONTROL[i++] = COMPOSITE_GLYPH_CONTROL_COUNT;
                COMPOSITE_GLYPH_CONTROL[i++] = TTFACC_BYTE;  /* TBD */

                i = 0;
                HEAD_CONTROL[i++] = HEAD_CONTROL_COUNT;
                HEAD_CONTROL[i++] = TTFACC_LONG; /*        version */
                HEAD_CONTROL[i++] = TTFACC_LONG; /*        fontRevision */
                HEAD_CONTROL[i++] = TTFACC_LONG; /*        checkSumAdjustment */
                HEAD_CONTROL[i++] = TTFACC_LONG; /*        magicNumber */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       flags */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       unitsPerEm */
                HEAD_CONTROL[i++] = TTFACC_LONG; /* created[0] */
                HEAD_CONTROL[i++] = TTFACC_LONG; /* created[1] */
                HEAD_CONTROL[i++] = TTFACC_LONG; /* modified[0] */
                HEAD_CONTROL[i++] = TTFACC_LONG; /* modified[1] */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       xMin */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       yMin */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       xMax */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*        yMax */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       macStyle */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*       lowestRecPPEM */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*        fontDirectionHint */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*        indexToLocFormat */
                HEAD_CONTROL[i++] = TTFACC_WORD; /*        glyphDataFormat */

                i = 0;
                HHEA_CONTROL[i++] = HHEA_CONTROL_COUNT;
                HHEA_CONTROL[i++] = TTFACC_LONG; /*  version */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* Ascender */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* Descender */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* LineGap */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*advanceWidthMax */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* minLeftSideBearing */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* minRightSideBearing */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* xMaxExtent */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRise */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRun */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved1 */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved2 */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved3 */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved4 */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved5 */
                HHEA_CONTROL[i++] = TTFACC_WORD; /*  metricDataFormat */
                HHEA_CONTROL[i++] = TTFACC_WORD; /* numLongMetrics */

                i = 0;
                LONGHORMETRIC_CONTROL[i++] = LONGHORMETRIC_CONTROL_COUNT;
                LONGHORMETRIC_CONTROL[i++] = TTFACC_WORD; /* advanceWidth */
                LONGHORMETRIC_CONTROL[i++] = TTFACC_WORD;  /*  lsb */

                i = 0;
                LSB_CONTROL[i++] = LSB_CONTROL_COUNT;
                LSB_CONTROL[i++] = TTFACC_WORD;

                i = 0;
                VHEA_CONTROL[i++] = VHEA_CONTROL_COUNT;
                VHEA_CONTROL[i++] = TTFACC_LONG; /*  version */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* Ascender */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* Descender */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* LineGap */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*advanceHeightMax */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* minTopSideBearing */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* minBottomSideBearing */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* yMaxExtent */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRise */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRun */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  caretOffset */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved2 */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved3 */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved4 */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved5 */
                VHEA_CONTROL[i++] = TTFACC_WORD; /*  metricDataFormat */
                VHEA_CONTROL[i++] = TTFACC_WORD; /* numLongMetrics */

                i = 0;
                LONGVERMETRIC_CONTROL[i++] = LONGVERMETRIC_CONTROL_COUNT;
                LONGVERMETRIC_CONTROL[i++] = TTFACC_WORD;  /* advanceHeight */
                LONGVERMETRIC_CONTROL[i++] = TTFACC_WORD;   /* tsb */

                i = 0;
                XHEA_CONTROL[i++] = XHEA_CONTROL_COUNT;
                XHEA_CONTROL[i++] = TTFACC_LONG; /*  version */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* Ascender */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* Descender */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* LineGap */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*advanceWidthHeightMax */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* minLeftTopSideBearing */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* minRightBottomSideBearing */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* xyMaxExtent */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRise */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  caretSlopeRun */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  caretOffset */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved2 */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved3 */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved4 */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  reserved5 */
                XHEA_CONTROL[i++] = TTFACC_WORD; /*  metricDataFormat */
                XHEA_CONTROL[i++] = TTFACC_WORD; /* numLongMetrics */

                i = 0;
                LONGXMETRIC_CONTROL[i++] = LONGXMETRIC_CONTROL_COUNT;
                LONGXMETRIC_CONTROL[i++] = TTFACC_WORD; /* advanceWidth */
                LONGXMETRIC_CONTROL[i++] = TTFACC_WORD;  /*  lsb */

                i = 0;
                XSB_CONTROL[i++] = XSB_CONTROL_COUNT;
                XSB_CONTROL[i++] = TTFACC_WORD;
           
                i = 0;
                TSB_CONTROL[i++] = TSB_CONTROL_COUNT;
                TSB_CONTROL[i++] = TTFACC_WORD;

                i = 0;
                LTSH_CONTROL[i++] = LTSH_CONTROL_COUNT;
                LTSH_CONTROL[i++] = TTFACC_WORD; /*      version */
                LTSH_CONTROL[i++] = TTFACC_WORD; /*      numGlyphs */

                i = 0;
                MAXP_CONTROL[i++] = MAXP_CONTROL_COUNT;
                MAXP_CONTROL[i++] = TTFACC_LONG; /*   version */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  numGlyphs */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxPoints */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxContours */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxCompositePoints */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxCompositeContours */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxElements */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxTwilightPoints */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxStorage */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxFunctionDefs */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxInstructionDefs */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxStackElements */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxSizeOfInstructions */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxComponentElements */
                MAXP_CONTROL[i++] = TTFACC_WORD; /*  maxComponentDepth */

                i = 0;
                NAME_RECORD_CONTROL[i++] = NAME_RECORD_CONTROL_COUNT;
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  platformID */
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  encodingID */
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  languageID */
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  nameID */
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  stringLength */
                NAME_RECORD_CONTROL[i++] = TTFACC_WORD; /*  stringOffset */
           
                i = 0;
                NAME_HEADER_CONTROL[i++] = NAME_HEADER_CONTROL_COUNT;
                NAME_HEADER_CONTROL[i++] = TTFACC_WORD; /*       formatSelector */
                NAME_HEADER_CONTROL[i++] = TTFACC_WORD; /*       numNameRecords */
                NAME_HEADER_CONTROL[i++] = TTFACC_WORD; /*       offsetToStringStorage */   /* from start of table */

                i = 0;
                HDMX_DEVICE_REC_CONTROL[i++] = HDMX_DEVICE_REC_CONTROL_COUNT;
                HDMX_DEVICE_REC_CONTROL[i++] = TTFACC_BYTE; /*  pixelSize */
                HDMX_DEVICE_REC_CONTROL[i++] = TTFACC_BYTE; /*  maxWidth */

                i = 0;
                HDMX_CONTROL[i++] = HDMX_CONTROL_COUNT;
                HDMX_CONTROL[i++] = TTFACC_WORD; /*         formatVersion */
                HDMX_CONTROL[i++] = TTFACC_WORD; /*          numDeviceRecords */
                HDMX_CONTROL[i++] = TTFACC_LONG; /*           sizeDeviceRecord */

                i = 0;
                VDMXVTABLE_CONTROL[i++] = VDMXVTABLE_CONTROL_COUNT;
                VDMXVTABLE_CONTROL[i++] = TTFACC_WORD;  /* yPelHeight */
                VDMXVTABLE_CONTROL[i++] = TTFACC_WORD;  /* yMax */
                VDMXVTABLE_CONTROL[i++] = TTFACC_WORD;      /* yMin */
                VDMXVTABLE_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD;/* PadForRISC */  /* lcp for platform independence */

                i = 0;
                VDMXGROUP_CONTROL[i++] = VDMXGROUP_CONTROL_COUNT;
                VDMXGROUP_CONTROL[i++] = TTFACC_WORD;  /* recs */
                VDMXGROUP_CONTROL[i++] = TTFACC_BYTE;  /* startSize */
                VDMXGROUP_CONTROL[i++] = TTFACC_BYTE;  /* endSize */

                i = 0;
                VDMXRATIO_CONTROL[i++] = VDMXRATIO_CONTROL_COUNT;
                VDMXRATIO_CONTROL[i++] = TTFACC_BYTE;  /* bCharSet */
                VDMXRATIO_CONTROL[i++] = TTFACC_BYTE;  /* xRatio */
                VDMXRATIO_CONTROL[i++] = TTFACC_BYTE;  /* yStartRatio */
                VDMXRATIO_CONTROL[i++] = TTFACC_BYTE;  /* yEndRatio */

                i = 0;
                VDMX_CONTROL[i++] = VDMX_CONTROL_COUNT;
                VDMX_CONTROL[i++] = TTFACC_WORD;  /* version */
                VDMX_CONTROL[i++] = TTFACC_WORD;  /* numRecs */
                VDMX_CONTROL[i++] = TTFACC_WORD;  /* numRatios */

                i = 0;
                DTTF_HEADER_CONTROL[i++] = DTTF_HEADER_CONTROL_COUNT;
                DTTF_HEADER_CONTROL[i++] = TTFACC_LONG;        /* version */
                DTTF_HEADER_CONTROL[i++] = TTFACC_LONG;        /* checkSum */
                DTTF_HEADER_CONTROL[i++] = TTFACC_WORD;        /* OriginalNumGlyphs */
                DTTF_HEADER_CONTROL[i++] = TTFACC_WORD;        /* maxGlyphIndexUsed */
                DTTF_HEADER_CONTROL[i++] = TTFACC_WORD;        /* format */
                DTTF_HEADER_CONTROL[i++] = TTFACC_WORD;        /* fflags */
                DTTF_HEADER_CONTROL[i++] = TTFACC_WORD ;       /* glyphCount */
                /*  USHORT GlyphIndexArray[glyphCount] */

                i = 0;
                KERN_HEADER_CONTROL[i++] = KERN_HEADER_CONTROL_COUNT;
                KERN_HEADER_CONTROL[i++] = TTFACC_WORD; /* format */
                KERN_HEADER_CONTROL[i++] = TTFACC_WORD; /* nTables */

                i = 0;
                KERN_SUB_HEADER_CONTROL[i++] = KERN_SUB_HEADER_CONTROL_COUNT;
                KERN_SUB_HEADER_CONTROL[i++] = TTFACC_WORD; /* format */
                KERN_SUB_HEADER_CONTROL[i++] = TTFACC_WORD; /* length */
                KERN_SUB_HEADER_CONTROL[i++] = TTFACC_WORD; /* coverage */
                KERN_SUB_HEADER_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /* PadForRISC */  /* lcp for platform independence */

                i = 0;
                KERN_FORMAT_0_CONTROL[i++] = KERN_FORMAT_0_CONTROL_COUNT;
                KERN_FORMAT_0_CONTROL[i++] = TTFACC_WORD; /*  nPairs */
                KERN_FORMAT_0_CONTROL[i++] = TTFACC_WORD; /*  searchRange */
                KERN_FORMAT_0_CONTROL[i++] = TTFACC_WORD; /*  entrySelector */
                KERN_FORMAT_0_CONTROL[i++] = TTFACC_WORD;  /*  rangeShift */

                i = 0;
                KERN_PAIR_CONTROL[i++] = KERN_PAIR_CONTROL_COUNT;
                KERN_PAIR_CONTROL[i++] = TTFACC_WORD; /*  left */
                KERN_PAIR_CONTROL[i++] = TTFACC_WORD; /*  right */
                KERN_PAIR_CONTROL[i++] = TTFACC_WORD;  /*  value */
                KERN_PAIR_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /* PadForRISC */  /* lcp for platform independence */

                i = 0;
                SEARCH_PAIRS_CONTROL[i++] = SEARCH_PAIRS_CONTROL_COUNT;
                SEARCH_PAIRS_CONTROL[i++] = TTFACC_LONG; /* leftAndRight */
                SEARCH_PAIRS_CONTROL[i++] = TTFACC_WORD;  /*value */
                SEARCH_PAIRS_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /* PadForRISC */  /* lcp for platform independence */

                i = 0;
                OS2_PANOSE_CONTROL[i++] = OS2_PANOSE_CONTROL_COUNT;
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bFamilyType */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bSerifStyle */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bWeight */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bProportion */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bContrast */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bStrokeVariation */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bArmStyle */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bLetterform */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bMidline */
                OS2_PANOSE_CONTROL[i++] = TTFACC_BYTE; /*  bXHeight */

                i = 0;
                OS2_CONTROL[i++] = OS2_CONTROL_COUNT;
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usVersion */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       xAvgCharWidth */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usWeightClass */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usWidthClass */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       fsTypeFlags */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXSize */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYSize */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXOffset */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYOffset */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXSize */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYSize */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXOffset */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYOffset */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutSize */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutPosition */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       sFamilyClass */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bFamilyType */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bSerifStyle */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bWeight */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bProportion */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bContrast */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bStrokeVariation */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bArmStyle */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bLetterform */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bMidline */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*  bXHeight */
                OS2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                OS2_CONTROL[i++] = TTFACC_LONG; /*       ulCharRange[0] */
                OS2_CONTROL[i++] = TTFACC_LONG; /*       ulCharRange[1] */
                OS2_CONTROL[i++] = TTFACC_LONG; /*       ulCharRange[2] */
                OS2_CONTROL[i++] = TTFACC_LONG; /*       ulCharRange[3] */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[0] */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[1] */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[2] */
                OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[3] */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      fsSelection */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usFirstCharIndex */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usLastCharIndex */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoAscender */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoDescender */
                OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoLineGap */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usWinAscent */
                OS2_CONTROL[i++] = TTFACC_WORD; /*      usWinDescent */

                i = 0;
                NEWOS2_CONTROL[i++] = NEWOS2_CONTROL_COUNT;
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usVersion */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       xAvgCharWidth */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usWeightClass */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usWidthClass */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       fsTypeFlags */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXSize */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYSize */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXOffset */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYOffset */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXSize */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYSize */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXOffset */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYOffset */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutSize */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutPosition */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       sFamilyClass */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bFamilyType */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bSerifStyle */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bWeight */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bProportion */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bContrast */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bStrokeVariation */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bArmStyle */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bLetterform */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bMidline */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*  bXHeight */
                NEWOS2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange1 */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange2 */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange3 */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange4 */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[0] */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[1] */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[2] */
                NEWOS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[3] */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      fsSelection */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usFirstCharIndex */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usLastCharIndex */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoAscender */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoDescender */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoLineGap */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*      usWinAscent */
                NEWOS2_CONTROL[i++] = TTFACC_WORD; /*       usWinDescent */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulCodePageRange1 */
                NEWOS2_CONTROL[i++] = TTFACC_LONG; /*       ulCodePageRange2 */

                i = 0;
                VERSION2OS2_CONTROL[i++] = VERSION2OS2_CONTROL_COUNT;
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usVersion */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       xAvgCharWidth */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usWeightClass */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usWidthClass */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       fsTypeFlags */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXSize */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYSize */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptXOffset */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySubscriptYOffset */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXSize */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYSize */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptXOffset */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       ySuperscriptYOffset */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutSize */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       yStrikeoutPosition */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sFamilyClass */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bFamilyType */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bSerifStyle */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bWeight */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bProportion */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bContrast */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bStrokeVariation */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bArmStyle */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bLetterform */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bMidline */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*  bXHeight */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange1 */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange2 */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange3 */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG; /*       ulUnicodeRange4 */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[0] */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[1] */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[2] */
                VERSION2OS2_CONTROL[i++] = TTFACC_BYTE; /*        achVendID[3] */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      fsSelection */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usFirstCharIndex */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usLastCharIndex */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoAscender */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoDescender */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sTypoLineGap */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*      usWinAscent */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       usWinDescent */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG; /*       ulCodePageRange1 */
                VERSION2OS2_CONTROL[i++] = TTFACC_LONG;  /*       ulCodePageRange2 */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sXHeight */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       sCapHeight */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       usDefaultChar */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       usBreakChar */
                VERSION2OS2_CONTROL[i++] = TTFACC_WORD; /*       usMaxLookups */   
          
                i = 0;
                EBLCHEADER_CONTROL[i++] = EBLCHEADER_CONTROL_COUNT;
                EBLCHEADER_CONTROL[i++] = TTFACC_LONG; /*        fxVersion */
                EBLCHEADER_CONTROL[i++] = TTFACC_LONG;  /*        ulNumSizes */

                i = 0;
                SBITLINEMETRICS_CONTROL[i++] = SBITLINEMETRICS_CONTROL_COUNT;
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cAscender */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cDescender */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byWidthMax */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeNumerator */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeDenominator */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cCaretOffset */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cMinOriginSB */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cMinAdvanceSB */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cMaxBeforeBL */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cMinAfterBL */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cPad1 */
                SBITLINEMETRICS_CONTROL[i++] = TTFACC_BYTE;  /*        cPad2 */


                i = 0;
                BITMAPSIZETABLE_CONTROL[i++] = BITMAPSIZETABLE_CONTROL_COUNT;
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_LONG; /*        ulIndexSubTableArrayOffset */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_LONG; /*        ulIndexTablesSize */
        #ifdef TESTPORT
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_LONG; /*        ulNumberOfIndexSubTables */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_LONG; /*        ulColorRef */
                /* SBITLINEMETRICS hori */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cAscender */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cDescender */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        byWidthMax */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeNumerator */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeDenominator */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretOffset */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinOriginSB */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinAdvanceSB */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMaxBeforeBL */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinAfterBL */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cPad1 */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE;  /*        cPad2 */
                /* SBITLINEMETRICS vert */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cAscender */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cDescender */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        byWidthMax */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeNumerator */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretSlopeDenominator */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cCaretOffset */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinOriginSB */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinAdvanceSB */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMaxBeforeBL */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cMinAfterBL */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        cPad1 */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE;  /*        cPad2 */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_WORD; /*        usStartGlyphIndex */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_WORD; /*        usEndGlyphIndex */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        byPpemX */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        byPpemY */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE; /*        byBitDepth */
                BITMAPSIZETABLE_CONTROL[i++] = TTFACC_BYTE;  /*        fFlags */

                i = 0;
                BIGGLYPHMETRICS_CONTROL[i++] = BIGGLYPHMETRICS_CONTROL_COUNT;
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingX */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingY */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byHoriAdvance */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingX */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingY */
                BIGGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byVertAdvance */

                i = 0;
                SMALLGLYPHMETRICS_CONTROL[i++] = SMALLGLYPHMETRICS_CONTROL_COUNT;
                SMALLGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                SMALLGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                SMALLGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cBearingX */
                SMALLGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        cBearingY */
                SMALLGLYPHMETRICS_CONTROL[i++] = TTFACC_BYTE; /*        byAdvance */


                i = 0;
                INDEXSUBTABLEARRAY_CONTROL[i++] = INDEXSUBTABLEARRAY_CONTROL_COUNT;
                INDEXSUBTABLEARRAY_CONTROL[i++] = TTFACC_WORD; /*        usFirstGlyphIndex */
                INDEXSUBTABLEARRAY_CONTROL[i++] = TTFACC_WORD; /*        usLastGlyphIndex */
        #ifdef TESTPORT
                INDEXSUBTABLEARRAY_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLEARRAY_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLEARRAY_CONTROL[i++] = TTFACC_LONG;  /*        ulAdditionalOffsetToIndexSubtable */


                i = 0;
                INDEXSUBHEADER_CONTROL[i++] = INDEXSUBHEADER_CONTROL_COUNT;
                INDEXSUBHEADER_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBHEADER_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBHEADER_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBHEADER_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBHEADER_CONTROL[i++] = TTFACC_LONG;  /*        ulImageDataOffset */


                i = 0;
                INDEXSUBTABLE1_CONTROL[i++] = INDEXSUBTABLE1_CONTROL_COUNT;
                /* INDEXSUBHEADER    header */
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_LONG;  /*        ulImageDataOffset */
        #ifdef TESTPORT
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE1_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                /* TTFACC_LONG              aulOffsetArray[1] */

                i = 0;
                INDEXSUBTABLE2_CONTROL[i++] = INDEXSUBTABLE2_CONTROL_COUNT;
                /* INDEXSUBHEADER    header */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_LONG;  /*        ulImageDataOffset */
        #ifdef TESTPORT
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_LONG; /*            ulImageSize */
                /* BIGGLYPHMETRICS bigMetrics */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingX */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingY */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        byHoriAdvance */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingX */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingY */
                INDEXSUBTABLE2_CONTROL[i++] = TTFACC_BYTE; /*        byVertAdvance */


                i = 0;
                INDEXSUBTABLE3_CONTROL[i++] = INDEXSUBTABLE3_CONTROL_COUNT;
                /* INDEXSUBHEADER    header */
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_LONG;  /*        ulImageDataOffset */
        #ifdef TESTPORT
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE3_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                /* TTFACC_WORD,             ausOffsetArray[1] */


                i = 0;
                CODEOFFSETPAIR_CONTROL[i++] = CODEOFFSETPAIR_CONTROL_COUNT;
        #ifdef TESTPORT
                CODEOFFSETPAIR_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                CODEOFFSETPAIR_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                CODEOFFSETPAIR_CONTROL[i++] = TTFACC_WORD; /*            usGlyphCode */
                CODEOFFSETPAIR_CONTROL[i++] = TTFACC_WORD;  /*            usOffset */


                i = 0;
                INDEXSUBTABLE4_CONTROL[i++] = INDEXSUBTABLE4_CONTROL_COUNT;
                /* INDEXSUBHEADER    header */
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_LONG; /*        ulImageDataOffset */
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_LONG;  /*            ulNumGlyphs */
        #ifdef TESTPORT
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE4_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                /* CODEOFFSETPAIR    glyphArray[1] */


                i = 0;
                INDEXSUBTABLE5_CONTROL[i++] = INDEXSUBTABLE5_CONTROL_COUNT;
                /* INDEXSUBHEADER    header */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD; /*        usIndexFormat */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD; /*        usImageFormat */
        #ifdef TESTPORT
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_LONG;  /*        ulImageDataOffset */
        #ifdef TESTPORT
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*    pad1 to test portability */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*        pad2;    */
        #endif
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_LONG; /*            ulImageSize */
                /* BIGGLYPHMETRICS bigMetrics */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingX */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingY */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        byHoriAdvance */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingX */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingY */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_BYTE; /*        byVertAdvance */
                INDEXSUBTABLE5_CONTROL[i++] = TTFACC_LONG; /*            ulNumGlyphs */
                /* TTFACC_WORD,              ausGlyphCodeArray[1] */


                i = 0;
                EBDTHEADER_CONTROL[i++] = EBDTHEADER_CONTROL_COUNT;
                EBDTHEADER_CONTROL[i++] = TTFACC_LONG; /*        fxVersion */

                i = 0;
                EBDTHEADERNOXLATENOPAD_CONTROL[i++] = EBDTHEADERNOXLATENOPAD_CONTROL_COUNT;
                EBDTHEADERNOXLATENOPAD_CONTROL[i++] = TTFACC_LONG|TTFACC_NO_XLATE; /*        fxVersion */

                i = 0;
                EBDTCOMPONENT_CONTROL[i++] = EBDTCOMPONENT_CONTROL_COUNT;
                EBDTCOMPONENT_CONTROL[i++] = TTFACC_WORD; /* glyphCode */
                EBDTCOMPONENT_CONTROL[i++] = TTFACC_BYTE; /* xOffset */
                EBDTCOMPONENT_CONTROL[i++] = TTFACC_BYTE; /* yOffset */

                i = 0;
                EBDTFORMAT8SIZE_CONTROL[i++] = EBDTFORMAT8SIZE_CONTROL_COUNT;
                /* SMALLGLYPHMETRICS smallMetrics */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /*        cBearingX */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /*        cBearingY */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /*        byAdvance */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_BYTE; /* pad */
                EBDTFORMAT8SIZE_CONTROL[i++] = TTFACC_WORD; /*     numComponents */
                /*     EBDTCOMPONENT componentArray[1] */

                i = 0;
                EBDTFORMAT9_CONTROL[i++] = EBDTFORMAT9_CONTROL_COUNT;
                /* BIGGLYPHMETRICS bigMetrics */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        byHeight */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        byWidth */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingX */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        cHoriBearingY */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        byHoriAdvance */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingX */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        cVertBearingY */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_BYTE; /*        byVertAdvance */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_WORD;  /*     numComponents */
                EBDTFORMAT9_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                /* EBDTCOMPONENT componentArray[1] */

                i = 0;
                GSUBFEATURE_CONTROL[i++] = GSUBFEATURE_CONTROL_COUNT;
                GSUBFEATURE_CONTROL[i++] = TTFACC_WORD; /* FeatureParamsOffset */  /* dummy, NULL */
                GSUBFEATURE_CONTROL[i++] = TTFACC_WORD; /* FeatureLookupCount */
                GSUBFEATURE_CONTROL[i++] = TTFACC_WORD;  /* LookupListIndexArray[1] */

                i = 0;
                GSUBFEATURERECORD_CONTROL[i++] = GSUBFEATURERECORD_CONTROL_COUNT;
                GSUBFEATURERECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */
                GSUBFEATURERECORD_CONTROL[i++] = TTFACC_WORD;  /* FeatureOffset */
                GSUBFEATURERECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                GSUBFEATURELIST_CONTROL[i++] = GSUBFEATURELIST_CONTROL_COUNT;
                GSUBFEATURELIST_CONTROL[i++] = TTFACC_WORD;  /* FeatureCount */
                GSUBFEATURELIST_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                /*   GSUBFEATURERECORD FeatureRecordArray[1] */ 

                i = 0;
                GSUBLOOKUP_CONTROL[i++] = GSUBLOOKUP_CONTROL_COUNT;
                GSUBLOOKUP_CONTROL[i++] = TTFACC_WORD; /*    LookupType */
                GSUBLOOKUP_CONTROL[i++] = TTFACC_WORD; /*     LookupFlag */
                GSUBLOOKUP_CONTROL[i++] = TTFACC_WORD;  /*     SubTableCount */
                /* TTFACC_WORD       SubstTableOffsetArray[1] */

                i = 0;
                GSUBLOOKUPLIST_CONTROL[i++] = GSUBLOOKUPLIST_CONTROL_COUNT;
                GSUBLOOKUPLIST_CONTROL[i++] = TTFACC_WORD;  /*    LookupCount */
                /* TTFACC_WORD,       LookupTableOffsetArray[1] */

                i = 0;
                GSUBCOVERAGEFORMAT1_CONTROL[i++] = GSUBCOVERAGEFORMAT1_CONTROL_COUNT;
                GSUBCOVERAGEFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBCOVERAGEFORMAT1_CONTROL[i++] = TTFACC_WORD;  /* GlyphCount */
                /* TTFACC_WORD,  GlyphIDArray[1] */

                i = 0;
                GSUBRANGERECORD_CONTROL[i++] = GSUBRANGERECORD_CONTROL_COUNT;
                GSUBRANGERECORD_CONTROL[i++] = TTFACC_WORD; /* RangeStart */
                GSUBRANGERECORD_CONTROL[i++] = TTFACC_WORD; /* RangeEnd */
                GSUBRANGERECORD_CONTROL[i++] = TTFACC_WORD;  /* StartCoverageIndex */
                GSUBRANGERECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /* PadForRISC */  /* lcp for platform independence */

                i = 0;
                GSUBCOVERAGEFORMAT2_CONTROL[i++] = GSUBCOVERAGEFORMAT2_CONTROL_COUNT;
                GSUBCOVERAGEFORMAT2_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBCOVERAGEFORMAT2_CONTROL[i++] = TTFACC_WORD;  /* CoverageRangeCount */
                /* GSUBRANGERECORD RangeRecordArray[1] */

                i = 0;
                GSUBHEADER_CONTROL[i++] = GSUBHEADER_CONTROL_COUNT;
                GSUBHEADER_CONTROL[i++] = TTFACC_LONG; /* Version */
                GSUBHEADER_CONTROL[i++] = TTFACC_WORD; /* ScriptListOffset */
                GSUBHEADER_CONTROL[i++] = TTFACC_WORD; /* FeatureListOffset */
                GSUBHEADER_CONTROL[i++] = TTFACC_WORD; /* LookupListOffset */

                i = 0;
                GSUBSINGLESUBSTFORMAT1_CONTROL[i++] = GSUBSINGLESUBSTFORMAT1_CONTROL_COUNT;
                GSUBSINGLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBSINGLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBSINGLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD;  /* DeltaGlyphID */

                i = 0;
                GSUBSINGLESUBSTFORMAT2_CONTROL[i++] = GSUBSINGLESUBSTFORMAT2_CONTROL_COUNT;
                GSUBSINGLESUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBSINGLESUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBSINGLESUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD;  /* GlyphCount */ 
                /*     TTFACC_WORD, /* GlyphIDArray[1] */

                i = 0;
                GSUBSEQUENCE_CONTROL[i++] = GSUBSEQUENCE_CONTROL_COUNT;
                GSUBSEQUENCE_CONTROL[i++] = TTFACC_WORD; /* SequenceGlyphCount */
                /* TTFACC_WORD, /* GlyphIDArray[1] */

                i = 0;
                GSUBMULTIPLESUBSTFORMAT1_CONTROL[i++] = GSUBMULTIPLESUBSTFORMAT1_CONTROL_COUNT;
                GSUBMULTIPLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBMULTIPLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBMULTIPLESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD;  /* SequenceCount */ 
                /* TTFACC_WORD, /* SequenceOffsetArray[1] */

                i = 0;
                GSUBALTERNATESET_CONTROL[i++] = GSUBALTERNATESET_CONTROL_COUNT;
                GSUBALTERNATESET_CONTROL[i++] = TTFACC_WORD;  /* GlyphCount */
                /* TTFACC_WORD, /* GlyphIDArray[1] */

                i = 0;
                GSUBALTERNATESUBSTFORMAT1_CONTROL[i++] = GSUBALTERNATESUBSTFORMAT1_CONTROL_COUNT;
                GSUBALTERNATESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBALTERNATESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBALTERNATESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* AlternateSetCount */ 
                /* TTFACC_WORD, /* AlternateSetOffsetArray[1] */

                i = 0;
                GSUBLIGATURE_CONTROL[i++] = GSUBLIGATURE_CONTROL_COUNT;
                GSUBLIGATURE_CONTROL[i++] = TTFACC_WORD; /* GlyphID */
                GSUBLIGATURE_CONTROL[i++] = TTFACC_WORD;  /* LigatureCompCount */
                /* TTFACC_WORD, /* GlyphIDArray[1] */

                i = 0;
                GSUBLIGATURESET_CONTROL[i++] = GSUBLIGATURESET_CONTROL_COUNT;
                GSUBLIGATURESET_CONTROL[i++] = TTFACC_WORD;  /* LigatureCount */
                /* TTFACC_WORD, /* LigatureOffsetArray[1] */

                i = 0;
                GSUBLIGATURESUBSTFORMAT1_CONTROL[i++] = GSUBLIGATURESUBSTFORMAT1_CONTROL_COUNT;
                GSUBLIGATURESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBLIGATURESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBLIGATURESUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD;  /* LigatureSetCount */
                /* TTFACC_WORD, /* LigatureSetOffsetArray[1] */

                i = 0;
                GSUBSUBSTLOOKUPRECORD_CONTROL[i++] = GSUBSUBSTLOOKUPRECORD_CONTROL_COUNT;
                GSUBSUBSTLOOKUPRECORD_CONTROL[i++] = TTFACC_WORD; /* SequenceIndex */
                GSUBSUBSTLOOKUPRECORD_CONTROL[i++] = TTFACC_WORD;  /* LookupListIndex */

                i = 0;
                GSUBSUBRULE_CONTROL[i++] = GSUBSUBRULE_CONTROL_COUNT;
                GSUBSUBRULE_CONTROL[i++] = TTFACC_WORD; /* SubRuleGlyphCount */
                GSUBSUBRULE_CONTROL[i++] = TTFACC_WORD;  /* SubRuleSubstCount */
                /* TTFACC_WORD, /* GlyphIDArray[1] */
                /* TTFACC_WORD, /* SubstLookupRecordArray[1] */  /* can't put this here - in code */

                i = 0;
                GSUBSUBRULESET_CONTROL[i++] = GSUBSUBRULESET_CONTROL_COUNT;
                GSUBSUBRULESET_CONTROL[i++] = TTFACC_WORD;  /* SubRuleCount */
                /* TTFACC_WORD, /* SubRuleOffsetArray[1] */

                i = 0;
                GSUBCONTEXTSUBSTFORMAT1_CONTROL[i++] = GSUBCONTEXTSUBSTFORMAT1_CONTROL_COUNT;
                GSUBCONTEXTSUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBCONTEXTSUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBCONTEXTSUBSTFORMAT1_CONTROL[i++] = TTFACC_WORD; /* SubRuleSetCount */
                /* TTFACC_WORD, /* SubRuleSetOffsetArray[1] */

                i = 0;
                GSUBSUBCLASSRULE_CONTROL[i++] = GSUBSUBCLASSRULE_CONTROL_COUNT;
                GSUBSUBCLASSRULE_CONTROL[i++] = TTFACC_WORD; /* SubClassRuleGlyphCount */
                GSUBSUBCLASSRULE_CONTROL[i++] = TTFACC_WORD; /* SubClassRuleSubstCount */
                /* TTFACC_WORD, /* ClassArray[1] */
                /* TTFACC_WORD, /* SubstLookupRecordArray[1] */  /* can't put this here - in code */

                i = 0;
                GSUBSUBCLASSSET_CONTROL[i++] = GSUBSUBCLASSSET_CONTROL_COUNT;
                GSUBSUBCLASSSET_CONTROL[i++] = TTFACC_WORD;  /* SubClassRuleCount */
                /* TTFACC_WORD, /* SubClassRuleOffsetArray[1] */

                i = 0;
                GSUBCONTEXTSUBSTFORMAT2_CONTROL[i++] = GSUBCONTEXTSUBSTFORMAT2_CONTROL_COUNT;
                GSUBCONTEXTSUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBCONTEXTSUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD; /* CoverageOffset */
                GSUBCONTEXTSUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD; /* ClassDefOffset */
                GSUBCONTEXTSUBSTFORMAT2_CONTROL[i++] = TTFACC_WORD;  /* SubClassSetCount */
                /* TTFACC_WORD, /* SubClassSetOffsetArray[1] */

                i = 0;
                GSUBCONTEXTSUBSTFORMAT3_CONTROL[i++] = GSUBCONTEXTSUBSTFORMAT3_CONTROL_COUNT;
                GSUBCONTEXTSUBSTFORMAT3_CONTROL[i++] = TTFACC_WORD; /* Format */
                GSUBCONTEXTSUBSTFORMAT3_CONTROL[i++] = TTFACC_WORD; /* GlyphCount */
                GSUBCONTEXTSUBSTFORMAT3_CONTROL[i++] = TTFACC_WORD;  /* SubstCount */
                /*    TTFACC_WORD, /* CoverageOffsetArray[1] */
                /* TTFACC_WORD, /* SubstLookupRecordArray[1] */

                i = 0;
                JSTFSCRIPTRECORD_CONTROL[i++] = JSTFSCRIPTRECORD_CONTROL_COUNT;
                JSTFSCRIPTRECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */
                JSTFSCRIPTRECORD_CONTROL[i++] = TTFACC_WORD;  /* JstfScriptOffset */
                JSTFSCRIPTRECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                JSTFHEADER_CONTROL[i++] = JSTFHEADER_CONTROL_COUNT;
                JSTFHEADER_CONTROL[i++] = TTFACC_LONG; /* Version */
                JSTFHEADER_CONTROL[i++] = TTFACC_WORD;  /* ScriptCount */
                JSTFHEADER_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                /* JSTFSCRIPTRECORD ScriptRecordArray[1] */

                i = 0;
                JSTFLANGSYSRECORD_CONTROL[i++] = JSTFLANGSYSRECORD_CONTROL_COUNT;
                JSTFLANGSYSRECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */
                JSTFLANGSYSRECORD_CONTROL[i++] = TTFACC_WORD;  /* LangSysOffset */
                JSTFLANGSYSRECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                JSTFSCRIPT_CONTROL[i++] = JSTFSCRIPT_CONTROL_COUNT;
                JSTFSCRIPT_CONTROL[i++] = TTFACC_WORD; /* ExtenderGlyphOffset */
                JSTFSCRIPT_CONTROL[i++] = TTFACC_WORD; /* LangSysOffset */
                JSTFSCRIPT_CONTROL[i++] = TTFACC_WORD; /* LangSysCount */
                JSTFSCRIPT_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                /* JSTFLANGSYSRECORD LangSysRecordArray[1] */

                i = 0;
                JSTFEXTENDERGLYPH_CONTROL[i++] = JSTFEXTENDERGLYPH_CONTROL_COUNT;
                JSTFEXTENDERGLYPH_CONTROL[i++] = TTFACC_WORD;  /* ExtenderGlyphCount */
                /* TTFACC_WORD, /* GlyphIDArray[1] */

                i = 0;
                BASEHEADER_CONTROL[i++] = BASEHEADER_CONTROL_COUNT;
                BASEHEADER_CONTROL[i++] = TTFACC_LONG; /* version */
                BASEHEADER_CONTROL[i++] = TTFACC_WORD; /* HorizAxisOffset */
                BASEHEADER_CONTROL[i++] = TTFACC_WORD; /* VertAxisOffset */

                i = 0;
                BASEAXIS_CONTROL[i++] = BASEAXIS_CONTROL_COUNT;
                BASEAXIS_CONTROL[i++] = TTFACC_WORD; /* BaseTagListOffset */
                BASEAXIS_CONTROL[i++] = TTFACC_WORD; /* BaseScriptListOffset */

                i = 0;
                BASESCRIPTRECORD_CONTROL[i++] = BASESCRIPTRECORD_CONTROL_COUNT;
                BASESCRIPTRECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */                
                BASESCRIPTRECORD_CONTROL[i++] = TTFACC_WORD;  /* BaseScriptOffset */
                BASESCRIPTRECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                BASESCRIPTLIST_CONTROL[i++] = BASESCRIPTLIST_CONTROL_COUNT;
                BASESCRIPTLIST_CONTROL[i++] = TTFACC_WORD;  /* BaseScriptCount */
                BASESCRIPTLIST_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                BASELANGSYSRECORD_CONTROL[i++] = BASELANGSYSRECORD_CONTROL_COUNT;
                BASELANGSYSRECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */                                 
                BASELANGSYSRECORD_CONTROL[i++] = TTFACC_WORD;  /* MinMaxOffset */
                BASELANGSYSRECORD_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                BASESCRIPT_CONTROL[i++] = BASESCRIPT_CONTROL_COUNT;
                BASESCRIPT_CONTROL[i++] = TTFACC_WORD; /* BaseValuesOffset */
                BASESCRIPT_CONTROL[i++] = TTFACC_WORD; /* MinMaxOffset */
                BASESCRIPT_CONTROL[i++] = TTFACC_WORD;  /* BaseLangSysCount */
                BASESCRIPT_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */

                i = 0;
                BASEVALUES_CONTROL[i++] = BASEVALUES_CONTROL_COUNT;
                BASEVALUES_CONTROL[i++] = TTFACC_WORD; /* DefaultIndex */
                BASEVALUES_CONTROL[i++] = TTFACC_WORD; /* BaseCoordCount */
                /* TTFACC_WORD, /* BaseCoordOffsetArray[1] */

                i = 0;
                BASEFEATMINMAXRECORD_CONTROL[i++] = BASEFEATMINMAXRECORD_CONTROL_COUNT;
                BASEFEATMINMAXRECORD_CONTROL[i++] = TTFACC_LONG; /* Tag */
                BASEFEATMINMAXRECORD_CONTROL[i++] = TTFACC_WORD; /* MinCoordOffset */
                BASEFEATMINMAXRECORD_CONTROL[i++] = TTFACC_WORD;  /* MaxCoordOffset */

                i = 0;
                BASEMINMAX_CONTROL[i++] = BASEMINMAX_CONTROL_COUNT;
                BASEMINMAX_CONTROL[i++] = TTFACC_WORD; /* MinCoordOffset */ 
                BASEMINMAX_CONTROL[i++] = TTFACC_WORD; /* MaxCoordOffset */                       
                BASEMINMAX_CONTROL[i++] = TTFACC_WORD;  /* FeatMinMaxCount */
                BASEMINMAX_CONTROL[i++] = TTFACC_WORD|TTFACC_PAD; /*       PadForRISC */  /* lcp for platform independence */
                /* BASEFEATMINMAXRECORD FeatMinMaxRecordArray[1] */

                i = 0;
                BASECOORDFORMAT2_CONTROL[i++] = BASECOORDFORMAT2_CONTROL_COUNT;
                BASECOORDFORMAT2_CONTROL[i++] = TTFACC_WORD; /* Format */ 
                BASECOORDFORMAT2_CONTROL[i++] = TTFACC_WORD; /* Coord */
                BASECOORDFORMAT2_CONTROL[i++] = TTFACC_WORD; /* GlyphID */
                BASECOORDFORMAT2_CONTROL[i++] = TTFACC_WORD; /* BaseCoordPoint */                                

                i = 0;
                MORTBINSRCHHEADER_CONTROL[i++] = MORTBINSRCHHEADER_CONTROL_COUNT;
                MORTBINSRCHHEADER_CONTROL[i++] = TTFACC_WORD; /* entrySize */
                MORTBINSRCHHEADER_CONTROL[i++] = TTFACC_WORD; /* nEntries */
                MORTBINSRCHHEADER_CONTROL[i++] = TTFACC_WORD; /* searchRange */
                MORTBINSRCHHEADER_CONTROL[i++] = TTFACC_WORD; /* entrySelector */
                MORTBINSRCHHEADER_CONTROL[i++] = TTFACC_WORD;  /* rangeShift */

                i = 0;
                MORTLOOKUPSINGLE_CONTROL[i++] = MORTLOOKUPSINGLE_CONTROL_COUNT;
                MORTLOOKUPSINGLE_CONTROL[i++] = TTFACC_WORD; /* glyphid1 */
                MORTLOOKUPSINGLE_CONTROL[i++] = TTFACC_WORD; /* glyphid2 */

                i = 0;
                MORTHEADER_CONTROL[i++] = MORTHEADER_CONTROL_COUNT;
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[0]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[1]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[2]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[3]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[4]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[5]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[6]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[7]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[8]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[9]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[10]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;         /*   constants1[11]; */
                MORTHEADER_CONTROL[i++] = TTFACC_LONG;         /*   length1; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[0]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[1]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[2]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[3]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[4]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[5]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[6]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[7]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[8]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[9]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[10]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[11]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[12]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[13]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[14]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants2[15]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[0]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[1]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[2]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[3]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[4]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[5]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[6]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[7]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[8]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[9]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[10]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[11]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[12]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[13]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[14]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants3[15]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[0]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[1]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[2]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[3]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[4]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[5]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[6]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants4[7]; */
                MORTHEADER_CONTROL[i++] = TTFACC_WORD;          /* length2; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[0]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[1]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[2]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[3]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[4]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[5]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[6]; */
                MORTHEADER_CONTROL[i++] = TTFACC_BYTE;          /* constants5[7]; */
                /*    BinSrchHeader  SearchHeader; */
                /*    LookupSingle   entries[1];  */

                _isInitialized = true;
            }
        }
        finally 
        {
            System::Threading::Monitor::Exit(_staticLock);
        }
    }
}
