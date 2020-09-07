// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************************
 * module: TTFF.H (True Type Font File)
 *
 *
 * Typedefs corresponding to tables and table entries in the
 * true type font files. 
 * Use with TTFCntrl.h for platform independent file access
 * Also, update TTFCntrl.h if this file is updated.      
 * PadForRISC in the OS2 table is the one example of where the 
 * internal data structure does not line up with the image in the file.
 *
 **************************************************************************/

#ifndef TTFF_DOT_H_DEFINED
#define TTFF_DOT_H_DEFINED

#define NEW_OS2

/* This is the amount by which a TTFF structure in memory exceeds the size of that structure in the file */
/* a value of 2 means "At most it will be 2x as large" */
/* Set this to a large value if needed */
/* currently only used by modsbit.c */
#define PORTABILITY_FACTOR 2

/* To ensure that these structures are left as is, and not aligned for */
/* processor access efficiency, use pragma pack(1) */
#pragma pack()

/* True Type Font File defines ------------------------------------------ */

#define UNICODE_PLATFORMID 0
#define APPLE_PLATFORMID   1
#define ISO_PLATFORMID     2
#define MS_PLATFORMID      3
#define NUM_PLATFORMS      4

#define ADOBE_GRID         1000.0

#define HEAD_TAG  "head"
#define CMAP_TAG  "cmap"
#define GLYF_TAG  "glyf"
#define HHEA_TAG  "hhea"
#define VHEA_TAG  "vhea"
#define HMTX_TAG  "hmtx"
#define VMTX_TAG  "vmtx"
#define LOCA_TAG  "loca"
#define MAXP_TAG  "maxp"
#define NAME_TAG  "name"
#define POST_TAG  "post"
#define OS2_TAG   "OS/2"
#define CVT_TAG   "cvt "
#define FPGM_TAG  "fpgm"
#define HDMX_TAG  "hdmx"
#define KERN_TAG  "kern"
#define LTSH_TAG  "LTSH"
#define PREP_TAG  "prep"
#define PCLT_TAG  "PCLT"
#define VDMX_TAG  "VDMX"
#define GASP_TAG  "gasp"
#define EBLC_TAG  "EBLC"
#define EBDT_TAG  "EBDT"
#define EBSC_TAG  "EBSC"
#define BLOC_TAG  "bloc"
#define BDAT_TAG  "bdat"
#define BSCA_TAG  "bsca"
#define GPOS_TAG  "GPOS"
#define GDEF_TAG  "GDEF"
#define GSUB_TAG  "GSUB"
#define JSTF_TAG  "JSTF"
#define BASE_TAG  "BASE"
#define MORT_TAG  "mort"
#define DTTF_TAG  "dttf"  /* private Delta TTF table */
#define DTTF_LONG_TAG 0x64747466 /* 'dttf' need it in this form to SET the value */
#define HHEA_LONG_TAG 0x68686561 /* 'hhea' */
#define VHEA_LONG_TAG 0x76686561 /* "vhea" */
#define HMTX_LONG_TAG 0x686d7478 /* "hmtx" */
#define VMTX_LONG_TAG 0x766d7478 /* "vmtx" */
#define LTSH_LONG_TAG 0x4c545348 /* "LTSH" */
#define HDMX_LONG_TAG 0x68646d78 /* "hdmx" */
#define LOCA_LONG_TAG 0x6c6f6361 /* "loca" */
#define MAXP_LONG_TAG 0x6d617870 /* "maxp" */
#define GLYF_LONG_TAG 0x676c7966 /* 'glyf' */
#define CMAP_LONG_TAG 0x636d6170 /* 'cmap' */
#define EBLC_LONG_TAG 0x45424c43 /* 'EBLC' */
#define EBDT_LONG_TAG 0x45424454 /* 'EBDT' */
#define EBSC_LONG_TAG 0x45425343 /* 'EBSC' */
#define BLOC_LONG_TAG 0x626c6f63 /* 'bloc' */
#define BDAT_LONG_TAG 0x62646174 /* 'bdat' */
#define BSCA_LONG_TAG 0x62736361 /* 'bsca' */
#define HEAD_LONG_TAG 0x68656164 /* 'head' */
#define OS2_LONG_TAG  0x4f532f32 /* 'OS/2' */
#define VDMX_LONG_TAG 0x56444d58 /* "VDMX" */
#define FPGM_LONG_TAG 0x6670676d /* 'fpgm' */
#define PREP_LONG_TAG 0x70726570 /* 'prep' */
#define CVT_LONG_TAG  0x63767420 /* 'cvt ' */
#define KERN_LONG_TAG 0x6b65726e /* 'kern' */
#define NAME_LONG_TAG 0x6e616d65 /* "name" */
#define POST_LONG_TAG 0x706f7374 /* "post" */
#define GASP_LONG_TAG 0x67617370 /* 'gasp' */
#define PCLT_LONG_TAG 0x50434c54 /* 'PCLT' */
#define GPOS_LONG_TAG 0x47504f53 /* 'GPOS' */
#define GSUB_LONG_TAG 0x47535542 /* 'GSUB' */
#define GDEF_LONG_TAG 0x47444546 /* 'GDEF' */
#define JSTF_LONG_TAG 0x4a535446 /* 'JSTF' */
#define BASE_LONG_TAG 0x42415345 /* 'BASE' */
#define TTC_LONG_TAG  0x74746366 /* 'ttcf'

#define WIN_TAG   "win "
#define FOCA_TAG  "foca"
#define BLANK_TAG "    "
#define INFO_TAG  "*   "
#define NULL_TAG ""


/* type definitions --------------------------------------------------- */

/* TTC header */
typedef struct TTC_HEADER
   {
   ULONG  TTCTag;     /* must be ttcf */
   Fixed   version;
   ULONG  DirectoryCount;
   /* ULONG TableDirectoryOffset */
   } TTC_HEADER;
#define SIZEOF_TTC_HEADER 12


/* Table directory */

typedef struct OFFSET_TABLE
   {
   Fixed   version;
   USHORT  numTables;
   USHORT  searchRange;
   USHORT  entrySelector;
   USHORT  rangeShift;

   } OFFSET_TABLE;
#define SIZEOF_OFFSET_TABLE 12


typedef struct DIRECTORY
   {
   ULONG  tag;
   ULONG  checkSum;
   ULONG  offset;
   ULONG  length;

   } DIRECTORY;
#define SIZEOF_DIRECTORY      16


/* 'cmap' table */
     
#define FORMAT0_CMAP_FORMAT  0
#define FORMAT4_CMAP_FORMAT  4
#define FORMAT6_CMAP_FORMAT  6
#define FORMAT12_CMAP_FORMAT 12

typedef struct CMAP_HEADER
   {
   USHORT  versionNumber;
   USHORT  numTables;

   } CMAP_HEADER;
#define SIZEOF_CMAP_HEADER 4

typedef struct CMAP_TABLELOC
   {
   USHORT  platformID;
   USHORT  encodingID;
   ULONG   offset;

   } CMAP_TABLELOC;
#define SIZEOF_CMAP_TABLELOC 8

/* Generic subheader struct - works for old style and new style tables (surragate) */
typedef struct CMAP_SUBHEADER_GEN
   {
   USHORT  format;
   ULONG   length;
   } CMAP_SUBHEADER_GEN;

/* old cmap subheader */
typedef struct CMAP_SUBHEADER
   {
   USHORT  format;
   USHORT  length;
   USHORT  revision;
   } CMAP_SUBHEADER;
#define SIZEOF_CMAP_SUBHEADER 6

#define CMAP_FORMAT0_ARRAYCOUNT 256
typedef struct CMAP_FORMAT0
   {
   USHORT  format;
   USHORT  length;
   USHORT  revision;
   BYTE    glyphIndexArray[CMAP_FORMAT0_ARRAYCOUNT];

   } CMAP_FORMAT0;

#define SIZEOF_CMAP_FORMAT0 6     /* don't include the array */

typedef struct CMAP_FORMAT6
   {
   USHORT  format;
   USHORT  length;
   USHORT  revision;
   USHORT  firstCode;
   USHORT  entryCount;

   } CMAP_FORMAT6;

#define SIZEOF_CMAP_FORMAT6 10


typedef struct CMAP_FORMAT4
   {
   USHORT  format;
   USHORT  length;
   USHORT  revision;
   USHORT  segCountX2;
   USHORT  searchRange;
   USHORT  entrySelector;
   USHORT  rangeShift;
   } CMAP_FORMAT4;

#define SIZEOF_CMAP_FORMAT4 14

typedef struct FORMAT4_SEGMENTS
   {
   USHORT  endCount;
   USHORT  startCount;
   short   idDelta;
   USHORT  idRangeOffset;
   } FORMAT4_SEGMENTS;

#define SIZEOF_FORMAT4_SEGMENTS 8
 

typedef struct CMAP_FORMAT12
{
    USHORT    format;
    USHORT    revision;
    ULONG    length;
    ULONG    language;
    ULONG    nGroups;
} CMAP_FORMAT12;
#define SIZEOF_CMAP_FORMAT12 16

typedef struct FORMAT12_GROUPS
{
    ULONG    startCharCode;
    ULONG    endCharCode;
    ULONG    startGlyphCode;
} FORMAT12_GROUPS;
#define SIZEOF_FORMAT12_GROUPS 12
  
 
typedef unsigned short GLYPH_ID;
typedef unsigned long  CHAR_ID;

#define SIZEOF_GLYPH_ID 2


/* 'post' postscript table */

typedef struct POST
   {
   Fixed  formatType;
   Fixed  italicAngle;
   FWord  underlinePos;
   FWord  underlineThickness;
   ULONG  isFixedPitch;
   ULONG  minMemType42;
   ULONG  maxMemType42;
   ULONG  minMemType1;
   ULONG  maxMemType1;
   } POST;
#define SIZEOF_POST 32


/* 'glyf' glyph data table */

typedef struct GLYF_HEADER
   {
   short  numberOfContours;
   FWord  xMin;
   FWord  yMin;
   FWord  xMax;
   FWord  yMax;
   } GLYF_HEADER;
#define SIZEOF_GLYF_HEADER 10

#define ON_CURVE           0x01
#define X_SHORT            0x02
#define Y_SHORT            0x04
#define REPEAT_FLAG        0x08
#define X_SAME             0x10
#define X_SIGN             0x10
#define Y_SAME             0x20
#define Y_SIGN             0x20
#define GLYF_UNDEF_FLAGS   0xC0


typedef struct SIMPLE_GLYPH
   {
   USHORT *endPtsOfContours;
   USHORT instructionLength;
   BYTE   *instructions;
   BYTE   *flags;
   BYTE   *Coordinates;       /* length of x,y coord's depends on flags */
   } SIMPLE_GLYPH;

#define ARG_1_AND_2_ARE_WORDS     0x0001
#define ARGS_ARE_XY_VALUES        0x0002
#define ROUND_XY_TO_GRID          0x0004
#define WE_HAVE_A_SCALE           0x0008
#define NON_OVERLAPPING           0x0010
#define MORE_COMPONENTS           0x0020
#define WE_HAVE_AN_X_AND_Y_SCALE  0x0040
#define WE_HAVE_A_TWO_BY_TWO      0x0080
#define WE_HAVE_INSTRUCTIONS      0x0100
#define USE_MY_METRICS            0x0200
#define COMPOSITE_RESERVED_BITS   (~( ARG_1_AND_2_ARE_WORDS | \
           ARGS_ARE_XY_VALUES | ROUND_XY_TO_GRID | WE_HAVE_A_SCALE | \
           NON_OVERLAPPING | MORE_COMPONENTS | WE_HAVE_AN_X_AND_Y_SCALE | \
           WE_HAVE_A_TWO_BY_TWO | WE_HAVE_INSTRUCTIONS | USE_MY_METRICS ))

typedef struct COMPOSITE_GLYPH
   {
   BYTE TBD;
   } COMPOSITE_GLYPH;

#define SIZEOF_COMPOSITE_GLYPH 1

/* 'head' font header table */

#define SHORT_OFFSETS 0
#define LONG_OFFSETS  1

#define MACSTYLE_BOLD    0x0001
#define MACSTYLE_ITALIC  0x0002

#define HEADFLAG_OPTICALSCALING   0x0004
#define HEADFLAG_NONLINEARSCALING 0x0010

typedef long longDateTime[2];

typedef struct HEAD
   {
   Fixed        version;
   Fixed        fontRevision;
   ULONG        checkSumAdjustment;
   ULONG        magicNumber;
   USHORT       flags;
   USHORT       unitsPerEm;
   longDateTime created;
   longDateTime modified;
   FWord        xMin;
   FWord        yMin;
   FWord        xMax;
   FWord        yMax;
   USHORT       macStyle;
   USHORT       lowestRecPPEM;
   short        fontDirectionHint;
   short        indexToLocFormat;
   short        glyphDataFormat;

   } HEAD;

#define SIZEOF_HEAD 54

/* 'hhea' horizontal header table */

typedef struct HHEA
   {
   Fixed  version;
   FWord  Ascender;
   FWord  Descender;
   FWord  LineGap;
   uFWord advanceWidthMax;
   FWord  minLeftSideBearing;
   FWord  minRightSideBearing;
   FWord  xMaxExtent;
   short  caretSlopeRise;
   short  caretSlopeRun;
   short  reserved1;
   short  reserved2;
   short  reserved3;
   short  reserved4;
   short  reserved5;
   short  metricDataFormat;
   USHORT numLongMetrics;

   } HHEA;

#define SIZEOF_HHEA 36

/* 'hmtx' horizontal metrics table */

typedef struct LONGHORMETRIC
   {
   uFWord  advanceWidth;
   FWord   lsb;

   } LONGHORMETRIC;

#define SIZEOF_LONGHORMETRIC 4

typedef struct
   {
   LONGHORMETRIC *  hMetrics;
   FWord *          leftSideBearing;
   } HMTX;

/* 'vhea' horizontal header table */

typedef struct VHEA
   {
   Fixed  version;
   FWord  Ascender;
   FWord  Descender;
   FWord  LineGap;
   uFWord advanceHeightMax;
   FWord  minTopSideBearing;
   FWord  minBottomSideBearing;
   FWord  yMaxExtent;
   short  caretSlopeRise;
   short  caretSlopeRun;
   short  caretOffset;
   short  reserved2;
   short  reserved3;
   short  reserved4;
   short  reserved5;
   short  metricDataFormat;
   USHORT numLongMetrics;

   } VHEA;
#define SIZEOF_VHEA 36

/* 'vmtx' horizontal metrics table */

typedef struct LONGVERMETRIC
   {
   uFWord  advanceHeight;
   FWord   tsb;
   } LONGVERMETRIC;

#define SIZEOF_LONGVERMETRIC 4

typedef struct
   {
   LONGVERMETRIC *  vMetrics;
   FWord *          topSideBearing;
   } VMTX;

/* for use when dealing with hmtx or vmtx generically */
typedef struct XHEA
   {
   Fixed  version;
   FWord  Ascender;
   FWord  Descender;
   FWord  LineGap;
   uFWord advanceXMax;
   FWord  minLeftTopSideBearing;
   FWord  minRightBottomSideBearing;
   FWord  xyMaxExtent;
   short  caretSlopeRise;
   short  caretSlopeRun;
   short  caretOffset;
   short  reserved2;
   short  reserved3;
   short  reserved4;
   short  reserved5;
   short  metricDataFormat;
   USHORT numLongMetrics;

   } XHEA;
#define SIZEOF_XHEA 36

typedef struct LONGXMETRIC
   {
   uFWord  advanceX;
   FWord   xsb;
   } LONGXMETRIC;

#define SIZEOF_LONGXMETRIC 4

typedef struct
   {
   LONGXMETRIC *  xMetrics;
   FWord * xSideBearing;
   } XMTX;

/* 'loca' index to location table */

typedef union
   {
   USHORT *usOffsets;
   ULONG  *ulOffsets;
   } LOCA;

/* 'LTSH' linear threshold table */

typedef struct
   {
   USHORT      version;
   USHORT      numGlyphs;
   } LTSH;

#define SIZEOF_LTSH 4

typedef BYTE   LTSH_YPELS;
#define SIZEOF_LTSH_YPELS 1

/* 'maxp' maximum profile table */

typedef struct
   {
   Fixed   version;
   USHORT  numGlyphs;
   USHORT  maxPoints;
   USHORT  maxContours;
   USHORT  maxCompositePoints;
   USHORT  maxCompositeContours;
   USHORT  maxElements;
   USHORT  maxTwilightPoints;
   USHORT  maxStorage;
   USHORT  maxFunctionDefs;
   USHORT  maxInstructionDefs;
   USHORT  maxStackElements;
   USHORT  maxSizeOfInstructions;
   USHORT  maxComponentElements;
   USHORT  maxComponentDepth;
   } MAXP;
#define SIZEOF_MAXP 32

/* 'name' naming table */

#define UNDEF_CHAR_SET    0
#define STD_MAC_CHAR_SET  0
#define UGL_CHAR_SET      1
#define DONT_CARE         0xFFFF
#define NAMES_REQ         7
#define MAC_ENGLISH       0
#define MS_USENGLISH      0x0409

#define COPYRIGHT        0
#define FONT_FAMILY      1
#define FONT_SUBFAMILY   2
#define SUBFAMILY_ID     3
#define FULL_FONT_NAME   4
#define VERSION          5
#define POSTSCRIPT_NAME  6

typedef struct
   {
   USHORT  platformID;
   USHORT  encodingID;
   USHORT  languageID;
   USHORT  nameID;
   USHORT  stringLength;
   USHORT  stringOffset;
   } NAME_RECORD;
#define SIZEOF_NAME_RECORD 12

typedef struct
   {
   USHORT       formatSelector;
   USHORT       numNameRecords;
   USHORT       offsetToStringStorage;   /* from start of table */
   } NAME_HEADER;
#define SIZEOF_NAME_HEADER 6

/* 'cvt ' control value table */

typedef FWord  CVT[];


/* 'fpgm' font program table */

typedef BYTE  FPGM[];


/* 'hdmx' horizontal device metrix table */
          
typedef BYTE HDMX_WIDTHS;

typedef struct
   {
   BYTE  pixelSize;
   BYTE  maxWidth;
   } HDMX_DEVICE_REC;
#define SIZEOF_HDMX_DEVICE_REC 2

typedef struct
   {
   USHORT         formatVersion;
   SHORT          numDeviceRecords;
   LONG           sizeDeviceRecord;
   } HDMX;
#define SIZEOF_HDMX 8

/* 'VDMX' Vertical Device Metrics Table */

typedef struct {
    USHORT yPelHeight;
    SHORT yMax;
    SHORT yMin;
    SHORT PadForRISC;  /* lcp for platform independence */
} VDMXvTable;
#define SIZEOF_VDMXVTABLE 8

typedef struct {
    USHORT recs;
    BYTE startSize;
    BYTE endSize;
/*    VDMXvTable entry[recs];*/
} VDMXGroup;
#define SIZEOF_VDMXGROUP 4

typedef struct {
    BYTE bCharSet;
    BYTE xRatio;
    BYTE yStartRatio;
    BYTE yEndRatio;
} VDMXRatio;
#define SIZEOF_VDMXRATIO 4

typedef struct {
    USHORT version;
    USHORT numRecs;
    USHORT numRatios;
/*    VDMXRatio ratRange[numRatios] */
/*     uint16 offset[numRatios]; */
/*    VDMXGroup groups[numRecs]; */
}VDMX;
#define SIZEOF_VDMX 6


/*** End VDMX ***/
/* 'dttf' delta ttf table */
#define CURRENT_DTTF_VERSION 0x00010000

typedef struct DTTF_HEADER
{
    Fixed version;     /* set to 0x00010000 */
    ULONG checkSum;     /* of original font. Used as unique identifier when merging a font */
    USHORT originalNumGlyphs; /* numGlyphs from Maxp from the original font, used to expand tables */
    USHORT maxGlyphIndexUsed; /* maximum glyph index used in font. same as GlyphIndexArray[glyphCount-1] */
    USHORT format;    /* of font. 0 = regular subset font - no subsequent deltas will be sent. 1 = subset font w/ full tto and kern data - format 2 may merge with this, 2 = delta font, 3 = merged  font (Working TrueType font created by MergeDeltaTTF) */
    USHORT fflags;    /* reserved. Set to 0 */
    USHORT glyphCount;     /* number of glyphs in GlyphIndexArray. If this is set, then the hmtx, hdmx, vmtx, LTSH and loca tables are in Compact form. If this is 0, they are in full subsetted form. This will be 0 for format 3 fonts. */
/*  USHORT GlyphIndexArray[glyphCount];     array of glyphCount glyph indices in ascending order corresponding to the glyph indices in this font. This will be empty for format 3 fonts */
} DTTF_HEADER;

#define SIZEOF_DTTF_HEADER 18
/* end 'dttf' delta ttf table */

/* 'kern' kerning table */

#define MS_KERN_FORMAT  0

typedef struct KERN_HEADER
   {
   USHORT format;
   USHORT nTables;
   } KERN_HEADER;
#define SIZEOF_KERN_HEADER 4

typedef struct KERN_SUB_HEADER
   {
   USHORT format;
   USHORT length;
   struct coverage
      {
      USHORT horizontal  :1;
      USHORT minimum     :1;
      USHORT crossStream :1;
      USHORT override    :1;
      USHORT reserved1   :4;
      USHORT format      :8;
      } coverage;
   SHORT  PadForRISC;  /* lcp for platform independence */
   } KERN_SUB_HEADER;
#define SIZEOF_KERN_SUB_HEADER 8

typedef struct KERN_FORMAT_0
   {
   USHORT  nPairs;
   USHORT  searchRange;
   USHORT  entrySelector;
   USHORT  rangeShift;
   } KERN_FORMAT_0;
#define SIZEOF_KERN_FORMAT_0 8

typedef struct KERN_PAIR
   {
   USHORT  left;
   USHORT  right;
   FWord   value;
   SHORT  PadForRISC;  /* lcp for platform independence */
   } KERN_PAIR;
#define SIZEOF_KERN_PAIR 8

typedef struct SEARCH_PAIRS
   {
   ULONG leftAndRight;
   FWord value;
   SHORT PadForRISC;  /* lcp for platform independence */
   } SEARCH_PAIRS;
#define SIZEOF_SEARCH_PAIRS 8

/* 'prep' anachronistic control value table */

typedef BYTE *PREP;


/* 'OS/2' OS/2 and Windows metrics table */

#define UNICODE_a      0x61
#define UNICODE_SPACE  0x20

typedef union
   {
   BYTE array[10];
   struct fields
      {
      BYTE  bFamilyType;
      BYTE  bSerifStyle;
      BYTE  bWeight;
      BYTE  bProportion;
      BYTE  bContrast;
      BYTE  bStrokeVariation;
      BYTE  bArmStyle;
      BYTE  bLetterform;
      BYTE  bMidline;
      BYTE  bXHeight;
      } fields;
   } OS2_PANOSE;
#define SIZEOF_OS2_PANOSE 10


#define OS2_ITALIC     0x0001
#define OS2_UNDERSCORE 0x0002
#define OS2_NEGATIVE   0x0004
#define OS2_OUTLINED   0x0008
#define OS2_STRIKEOUT  0x0010
#define OS2_BOLD       0x0020
#define OS2_REGULAR    0X0040

#define OS2_PANOSE_BOLD       7
#define OS2_PANOSE_ITALIC     9
#define OS2_PANOSE_UNDEFINED  1

#define OS2_WEIGHTCLASS_SEMIBOLD  6

typedef struct
   { 
   USHORT      usVersion;
   SHORT       xAvgCharWidth;
   USHORT      usWeightClass;
   USHORT      usWidthClass;
   SHORT       fsTypeFlags;
   SHORT       ySubscriptXSize;
   SHORT       ySubscriptYSize;
   SHORT       ySubscriptXOffset;
   SHORT       ySubscriptYOffset;
   SHORT       ySuperscriptXSize;
   SHORT       ySuperscriptYSize;
   SHORT       ySuperscriptXOffset;
   SHORT       ySuperscriptYOffset;
   SHORT       yStrikeoutSize;
   SHORT       yStrikeoutPosition;
   SHORT       sFamilyClass;
   union
   {
      BYTE array[10];
      struct
      {
          BYTE  bFamilyType;
          BYTE  bSerifStyle;
          BYTE  bWeight;
          BYTE  bProportion;
          BYTE  bContrast;
          BYTE  bStrokeVariation;
          BYTE  bArmStyle;
          BYTE  bLetterform;
          BYTE  bMidline;
          BYTE  bXHeight;
      } fields;
   } panose;
   SHORT       PadForRISC;  /* lcp for platform independence */
   ULONG       ulCharRange[4];
   CHAR        achVendID[4];
   USHORT      fsSelection;
   USHORT      usFirstCharIndex;
   USHORT      usLastCharIndex;
   SHORT       sTypoAscender;
   SHORT       sTypoDescender;
   SHORT       sTypoLineGap;
   USHORT      usWinAscent;
   USHORT      usWinDescent;
   } OS2;
#define SIZEOF_OS2 (70+SIZEOF_OS2_PANOSE)

typedef struct
   { 
   USHORT      usVersion;
   SHORT       xAvgCharWidth;
   USHORT      usWeightClass;
   USHORT      usWidthClass;
   SHORT       fsTypeFlags;
   SHORT       ySubscriptXSize;
   SHORT       ySubscriptYSize;
   SHORT       ySubscriptXOffset;
   SHORT       ySubscriptYOffset;
   SHORT       ySuperscriptXSize;
   SHORT       ySuperscriptYSize;
   SHORT       ySuperscriptXOffset;
   SHORT       ySuperscriptYOffset;
   SHORT       yStrikeoutSize;
   SHORT       yStrikeoutPosition;
   SHORT       sFamilyClass;
   union
   {
       BYTE array[10];
       struct
      {
          BYTE  bFamilyType;
          BYTE  bSerifStyle;
          BYTE  bWeight;
          BYTE  bProportion;
          BYTE  bContrast;
          BYTE  bStrokeVariation;
          BYTE  bArmStyle;
          BYTE  bLetterform;
          BYTE  bMidline;
          BYTE  bXHeight;
      } fields;
   } panose;
   SHORT       PadForRISC;  /* lcp for platform independence */
   ULONG       ulUnicodeRange1;
   ULONG       ulUnicodeRange2;
   ULONG       ulUnicodeRange3;
   ULONG       ulUnicodeRange4;
   CHAR        achVendID[4];
   USHORT      fsSelection;
   USHORT      usFirstCharIndex;
   USHORT      usLastCharIndex;
   SHORT       sTypoAscender;
   SHORT       sTypoDescender;
   SHORT       sTypoLineGap;
   USHORT      usWinAscent;
   USHORT       usWinDescent;
   ULONG       ulCodePageRange1;
   ULONG       ulCodePageRange2;
   } NEWOS2;
#define SIZEOF_NEWOS2 (78+SIZEOF_OS2_PANOSE)

typedef struct
   { 
   USHORT      usVersion;
   SHORT       xAvgCharWidth;
   USHORT      usWeightClass;
   USHORT      usWidthClass;
   SHORT       fsTypeFlags;
   SHORT       ySubscriptXSize;
   SHORT       ySubscriptYSize;
   SHORT       ySubscriptXOffset;
   SHORT       ySubscriptYOffset;
   SHORT       ySuperscriptXSize;
   SHORT       ySuperscriptYSize;
   SHORT       ySuperscriptXOffset;
   SHORT       ySuperscriptYOffset;
   SHORT       yStrikeoutSize;
   SHORT       yStrikeoutPosition;
   SHORT       sFamilyClass;
   union
   {
       BYTE array[10];
       struct
      {
          BYTE  bFamilyType;
          BYTE  bSerifStyle;
          BYTE  bWeight;
          BYTE  bProportion;
          BYTE  bContrast;
          BYTE  bStrokeVariation;
          BYTE  bArmStyle;
          BYTE  bLetterform;
          BYTE  bMidline;
          BYTE  bXHeight;
      } fields;
   } panose;
   SHORT       PadForRISC;  /* lcp for platform independence */
   ULONG       ulUnicodeRange1;
   ULONG       ulUnicodeRange2;
   ULONG       ulUnicodeRange3;
   ULONG       ulUnicodeRange4;
   CHAR        achVendID[4];
   USHORT      fsSelection;
   USHORT      usFirstCharIndex;
   USHORT      usLastCharIndex;
   SHORT       sTypoAscender;
   SHORT       sTypoDescender;
   SHORT       sTypoLineGap;
   USHORT      usWinAscent;
   USHORT       usWinDescent;
   ULONG       ulCodePageRange1;
   ULONG       ulCodePageRange2;
   SHORT       sxHeight;
   SHORT       sCapHeight;
   USHORT       usDefaultChar;
   USHORT       usBreakChar;
   USHORT       usMaxLookups;   
   } VERSION2OS2;
#define SIZEOF_VERSION2OS2 (88+SIZEOF_OS2_PANOSE)

/// MAINOS2 will always be most current OS2 we support.
typedef VERSION2OS2 MAINOS2;
#define SIZEOF_MAINOS2 SIZEOF_VERSION2OS2


/*  EBLC, EBDT and EBSC file constants    */

/*    This first EBLC is common to both EBLC and EBSC tables */

typedef struct
   {
   Fixed        fxVersion;
   ULONG        ulNumSizes;
   } EBLCHEADER;
#define SIZEOF_EBLCHEADER 8

typedef struct
{
    CHAR        cAscender;
    CHAR        cDescender;
    BYTE        byWidthMax;
    CHAR        cCaretSlopeNumerator;
    CHAR        cCaretSlopeDenominator;
    CHAR        cCaretOffset;
    CHAR        cMinOriginSB;
    CHAR        cMinAdvanceSB;
    CHAR        cMaxBeforeBL;
    CHAR        cMinAfterBL;
    CHAR        cPad1;
    CHAR        cPad2;
} SBITLINEMETRICS;
#define SIZEOF_SBITLINEMETRICS 12


typedef struct
{
    ULONG        ulIndexSubTableArrayOffset;
    ULONG        ulIndexTablesSize;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG        ulNumberOfIndexSubTables;
    ULONG        ulColorRef;
    SBITLINEMETRICS hori;
    SBITLINEMETRICS vert;
    USHORT        usStartGlyphIndex;
    USHORT        usEndGlyphIndex;
    BYTE        byPpemX;
    BYTE        byPpemY;
    BYTE        byBitDepth;
    CHAR        fFlags;
} BITMAPSIZETABLE;
#ifdef TESTPORT
#define SIZEOF_BITMAPSIZETABLE (28 + SIZEOF_SBITLINEMETRICS + SIZEOF_SBITLINEMETRICS)
#else
#define SIZEOF_BITMAPSIZETABLE (24 + SIZEOF_SBITLINEMETRICS + SIZEOF_SBITLINEMETRICS)
#endif

#define BITMAP_FLAGS_HORIZONTAL 0x01
#define BITMAP_FLAGS_VERTICAL    0x02

typedef struct
{
    BYTE        byHeight;
    BYTE        byWidth;
    CHAR        cHoriBearingX;
    CHAR        cHoriBearingY;
    BYTE        byHoriAdvance;
    CHAR        cVertBearingX;
    CHAR        cVertBearingY;
    BYTE        byVertAdvance;
} BIGGLYPHMETRICS;
#define SIZEOF_BIGGLYPHMETRICS 8

typedef struct
{
    BYTE        byHeight;
    BYTE        byWidth;
    CHAR        cBearingX;
    CHAR        cBearingY;
    BYTE        byAdvance;
} SMALLGLYPHMETRICS;
#define SIZEOF_SMALLGLYPHMETRICS 5

typedef struct
{
    USHORT        usFirstGlyphIndex;
    USHORT        usLastGlyphIndex;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG        ulAdditionalOffsetToIndexSubtable;
} INDEXSUBTABLEARRAY;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLEARRAY 12
#else
#define SIZEOF_INDEXSUBTABLEARRAY 8
#endif

typedef struct
{
    USHORT        usIndexFormat;
    USHORT        usImageFormat;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG        ulImageDataOffset;
} INDEXSUBHEADER;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBHEADER 12
#else
#define SIZEOF_INDEXSUBHEADER 8
#endif

typedef struct
{
    INDEXSUBHEADER    header;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG            aulOffsetArray[1];
} INDEXSUBTABLE1;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLE1 (SIZEOF_INDEXSUBHEADER)+4 /* don't include array entry */
#else
#define SIZEOF_INDEXSUBTABLE1 (SIZEOF_INDEXSUBHEADER) /* don't include array entry */
#endif

/* any padding to format 2 must be the same as format 5 */
typedef struct
{
    INDEXSUBHEADER    header;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG            ulImageSize;
    BIGGLYPHMETRICS bigMetrics;
} INDEXSUBTABLE2;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLE2 (4 + SIZEOF_INDEXSUBHEADER + SIZEOF_BIGGLYPHMETRICS) + 4
#else
#define SIZEOF_INDEXSUBTABLE2 (4 + SIZEOF_INDEXSUBHEADER + SIZEOF_BIGGLYPHMETRICS)
#endif

typedef struct
{
    INDEXSUBHEADER    header;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    USHORT            ausOffsetArray[1];
} INDEXSUBTABLE3;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLE3 (SIZEOF_INDEXSUBHEADER)+4 /* don't include array entry */
#else
#define SIZEOF_INDEXSUBTABLE3 (SIZEOF_INDEXSUBHEADER) /* don't include array entry */
#endif

typedef struct
{
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    USHORT            usGlyphCode;
    USHORT            usOffset;
} CODEOFFSETPAIR;
#ifdef TESTPORT
#define SIZEOF_CODEOFFSETPAIR 4    +4
#else
#define SIZEOF_CODEOFFSETPAIR 4
#endif

typedef struct
{
    INDEXSUBHEADER    header;
    ULONG            ulNumGlyphs;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    CODEOFFSETPAIR    glyphArray[1];
} INDEXSUBTABLE4;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLE4 (SIZEOF_INDEXSUBHEADER + 4 + 4) /* don't include array entry */
#else
#define SIZEOF_INDEXSUBTABLE4 (SIZEOF_INDEXSUBHEADER + 4) /* don't include array entry */
#endif

/* any padding to format 5 must be the same as format 2 */

typedef struct
{
    INDEXSUBHEADER    header;
#ifdef TESTPORT
    USHORT        pad1;       /* to test portability */
    USHORT        pad2;
#endif
    ULONG            ulImageSize;
    BIGGLYPHMETRICS bigMetrics;
    ULONG            ulNumGlyphs;
    USHORT            ausGlyphCodeArray[1];
} INDEXSUBTABLE5;
#ifdef TESTPORT
#define SIZEOF_INDEXSUBTABLE5 (SIZEOF_INDEXSUBHEADER + 8 + SIZEOF_BIGGLYPHMETRICS) + 4 /* don't include array entry */
#else
#define SIZEOF_INDEXSUBTABLE5 (SIZEOF_INDEXSUBHEADER + 8 + SIZEOF_BIGGLYPHMETRICS) /* don't include array entry */
#endif


typedef struct
{
    Fixed        fxVersion;
} EBDTHEADER;
#define SIZEOF_EBDTHEADER 4

typedef struct 
{
    USHORT glyphCode;
    CHAR xOffset;
    CHAR yOffset;
} EBDTCOMPONENT;
#define SIZEOF_EBDTCOMPONENT 4

typedef struct
{
    /* SMALLGLYPHMETRICS */
    BYTE        byHeight;
    BYTE        byWidth;
    CHAR        cBearingX;
    CHAR        cBearingY;
    BYTE        byAdvance;
    /* SMALLGLYPHMETRICS */
    BYTE pad;
    USHORT     numComponents;
    EBDTCOMPONENT componentArray[1];
} EBDTFORMAT8;
#define SIZEOF_EBDTFORMAT8 8 

typedef struct
{
    BIGGLYPHMETRICS bigMetrics;
    USHORT     numComponents;
    SHORT PadForRISC;  /* lcp for platform independence */
    EBDTCOMPONENT componentArray[1];
} EBDTFORMAT9;
#define SIZEOF_EBDTFORMAT9 (SIZEOF_BIGGLYPHMETRICS + 4) 

/* TrueType Open GSUB Tables, needed for Auto Mapping of unmapped Glyphs. */
typedef struct {
    USHORT FeatureParamsOffset;  /* dummy, NULL */
    USHORT FeatureLookupCount;
    USHORT LookupListIndexArray[1];
} GSUBFEATURE;
#define SIZEOF_GSUBFEATURE 4


typedef struct {
    ULONG Tag;
    USHORT FeatureOffset;  
    SHORT PadForRISC;  /* lcp for platform independence */
} GSUBFEATURERECORD;

#define SIZEOF_GSUBFEATURERECORD 8

typedef struct 
{
   USHORT FeatureCount; 
   SHORT PadForRISC;  /* lcp for platform independence */
   GSUBFEATURERECORD FeatureRecordArray[1]; 
} GSUBFEATURELIST;

#define SIZEOF_GSUBFEATURELIST 4

#define GSUBSingleLookupType 1
#define GSUBMultipleLookupType 2
#define GSUBAlternateLookupType 3
#define GSUBLigatureLookupType 4
#define GSUBContextLookupType 5

typedef struct {
    USHORT    LookupType;
    USHORT     LookupFlag;
    USHORT     SubTableCount;
    USHORT    SubstTableOffsetArray[1];
} GSUBLOOKUP;

#define SIZEOF_GSUBLOOKUP 6

typedef struct {
    USHORT    LookupCount;
    USHORT     LookupTableOffsetArray[1];
} GSUBLOOKUPLIST;
#define SIZEOF_GSUBLOOKUPLIST 2

typedef struct {
    USHORT Format;
    USHORT GlyphCount;
    USHORT GlyphIDArray[1];
} GSUBCOVERAGEFORMAT1;

#define SIZEOF_GSUBCOVERAGEFORMAT1 4

typedef struct {
    USHORT RangeStart;
    USHORT RangeEnd;
    USHORT StartCoverageIndex;
    SHORT PadForRISC;  /* lcp for platform independence */
} GSUBRANGERECORD;

#define SIZEOF_GSUBRANGERECORD 8

typedef struct {
    USHORT Format;
    USHORT CoverageRangeCount;
    GSUBRANGERECORD RangeRecordArray[1];
} GSUBCOVERAGEFORMAT2;

#define SIZEOF_GSUBCOVERAGEFORMAT2 4

typedef struct {
    ULONG Version;
    USHORT ScriptListOffset;
    USHORT FeatureListOffset;
    USHORT LookupListOffset;
} GSUBHEADER;

#define SIZEOF_GSUBHEADER 10

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    SHORT DeltaGlyphID;
} GSUBSINGLESUBSTFORMAT1;

#define SIZEOF_GSUBSINGLESUBSTFORMAT1 6

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT GlyphCount; 
    USHORT GlyphIDArray[1];
} GSUBSINGLESUBSTFORMAT2;

#define SIZEOF_GSUBSINGLESUBSTFORMAT2 6

typedef struct { 
    USHORT SequenceGlyphCount;
    USHORT GlyphIDArray[1];
} GSUBSEQUENCE;

#define SIZEOF_GSUBSEQUENCE 2

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT SequenceCount; 
    USHORT SequenceOffsetArray[1];
} GSUBMULTIPLESUBSTFORMAT1;    

#define SIZEOF_GSUBMULTIPLESUBSTFORMAT1    6

typedef struct { 
    USHORT GlyphCount;
    USHORT GlyphIDArray[1];
} GSUBALTERNATESET;

#define SIZEOF_GSUBALTERNATESET 2

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT AlternateSetCount; 
    USHORT AlternateSetOffsetArray[1];
} GSUBALTERNATESUBSTFORMAT1;

#define SIZEOF_GSUBALTERNATESUBSTFORMAT1 6

typedef struct {
    USHORT GlyphID;
    USHORT LigatureCompCount;
    USHORT GlyphIDArray[1];
} GSUBLIGATURE;

#define SIZEOF_GSUBLIGATURE 4

typedef struct {
    USHORT LigatureCount;
    USHORT LigatureOffsetArray[1];
} GSUBLIGATURESET;

#define SIZEOF_GSUBLIGATURESET 2

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT LigatureSetCount;
    USHORT LigatureSetOffsetArray[1];
} GSUBLIGATURESUBSTFORMAT1;

#define SIZEOF_GSUBLIGATURESUBSTFORMAT1 6

typedef struct {
    USHORT SequenceIndex;
    USHORT LookupListIndex;
} GSUBSUBSTLOOKUPRECORD;

#define SIZEOF_GSUBSUBSTLOOKUPRECORD 4

typedef struct {
    USHORT SubRuleGlyphCount;
    USHORT SubRuleSubstCount;
    USHORT GlyphIDArray[1];
/* USHORT SubstLookupRecordArray[1] */  /* can't put this here - in code */
} GSUBSUBRULE;

#define SIZEOF_GSUBSUBRULE 4

typedef struct {
    USHORT SubRuleCount;
    USHORT SubRuleOffsetArray[1];
} GSUBSUBRULESET;

#define SIZEOF_GSUBSUBRULESET 2

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT SubRuleSetCount;
    USHORT SubRuleSetOffsetArray[1];
} GSUBCONTEXTSUBSTFORMAT1;

#define SIZEOF_GSUBCONTEXTSUBSTFORMAT1 6

typedef struct {
    USHORT SubClassRuleGlyphCount;
    USHORT SubClassRuleSubstCount;
    USHORT ClassArray[1];
/* USHORT SubstLookupRecordArray[1] */  /* can't put this here - in code */
} GSUBSUBCLASSRULE;

#define SIZEOF_GSUBSUBCLASSRULE 4

typedef struct {
    USHORT SubClassRuleCount;
    USHORT SubClassRuleOffsetArray[1];
} GSUBSUBCLASSSET;

#define SIZEOF_GSUBSUBCLASSSET 2

typedef struct {
    USHORT Format;
    USHORT CoverageOffset;
    USHORT ClassDefOffset;
    USHORT SubClassSetCount;
    USHORT SubClassSetOffsetArray[1];
} GSUBCONTEXTSUBSTFORMAT2;

#define SIZEOF_GSUBCONTEXTSUBSTFORMAT2 8

typedef struct {
    USHORT Format;
    USHORT GlyphCount;
    USHORT SubstCount;
    USHORT CoverageOffsetArray[1];
/* USHORT SubstLookupRecordArray[1] */
} GSUBCONTEXTSUBSTFORMAT3;

#define SIZEOF_GSUBCONTEXTSUBSTFORMAT3 6


/* just enough jstf info to get the Automap working for jstf */
typedef struct {
    ULONG Tag;
    USHORT JstfScriptOffset; 
    SHORT PadForRISC;  /* lcp for platform independence */
} JSTFSCRIPTRECORD;

#define SIZEOF_JSTFSCRIPTRECORD 8

typedef struct {
    ULONG Version;
    USHORT ScriptCount; /* do we need a pad-for-risc here ? */
    SHORT PadForRISC;  /* lcp for platform independence */
    JSTFSCRIPTRECORD ScriptRecordArray[1];
} JSTFHEADER;

#define SIZEOF_JSTFHEADER 8

typedef struct {
    ULONG Tag;        
    USHORT LangSysOffset;
    SHORT PadForRISC;  /* lcp for platform independence */
} JSTFLANGSYSRECORD;

#define SIZEOF_JSTFLANGSYSRECORD 8

typedef struct {
    USHORT ExtenderGlyphOffset;
    USHORT LangSysOffset;
    USHORT LangSysCount;    
    SHORT PadForRISC;  /* lcp for platform independence */
    JSTFLANGSYSRECORD LangSysRecordArray[1];
} JSTFSCRIPT;

#define SIZEOF_JSTFSCRIPT 8

typedef struct {
USHORT ExtenderGlyphCount;
USHORT GlyphIDArray[1];
} JSTFEXTENDERGLYPH;

#define SIZEOF_JSTFEXTENDERGLYPH 2

/* BASE TTO Table, enough to do TTOAutoMap */

typedef struct {
ULONG version;              
USHORT HorizAxisOffset;                          
USHORT VertAxisOffset;
} BASEHEADER;

#define SIZEOF_BASEHEADER 8

typedef struct {
USHORT BaseTagListOffset;
USHORT BaseScriptListOffset;
} BASEAXIS;
#define SIZEOF_BASEAXIS 4

typedef struct {
ULONG Tag;               
USHORT BaseScriptOffset;
SHORT PadForRISC;  /* lcp for platform independence */
} BASESCRIPTRECORD;
#define SIZEOF_BASESCRIPTRECORD 8

typedef struct {
USHORT BaseScriptCount; 
SHORT PadForRISC;  /* lcp for platform independence */
BASESCRIPTRECORD BaseScriptRecordArray[1];
} BASESCRIPTLIST;
#define SIZEOF_BASESCRIPTLIST 4

typedef struct {
    ULONG Tag;                                 
    USHORT MinMaxOffset; 
    SHORT PadForRISC;  /* lcp for platform independence */
} BASELANGSYSRECORD;
#define SIZEOF_BASELANGSYSRECORD 8

typedef struct {
    USHORT BaseValuesOffset;
    USHORT MinMaxOffset;
    USHORT BaseLangSysCount;  
    SHORT PadForRISC;  /* lcp for platform independence */
    BASELANGSYSRECORD BaseLangSysRecordArray[1];
} BASESCRIPT;
#define SIZEOF_BASESCRIPT 8

typedef struct {
    USHORT DefaultIndex;
    USHORT BaseCoordCount;
    USHORT BaseCoordOffsetArray[1];
} BASEVALUES;
#define SIZEOF_BASEVALUES 4

typedef struct {
    ULONG Tag;
    USHORT MinCoordOffset;
    USHORT MaxCoordOffset;
} BASEFEATMINMAXRECORD;
#define SIZEOF_BASEFEATMINMAXRECORD 8

typedef struct {
    USHORT MinCoordOffset; 
    USHORT MaxCoordOffset;                       
    USHORT FeatMinMaxCount;
    SHORT PadForRISC;  /* lcp for platform independence */
    BASEFEATMINMAXRECORD FeatMinMaxRecordArray[1];
} BASEMINMAX;
#define SIZEOF_BASEMINMAX 8

typedef struct {
USHORT Format; 
USHORT Coord;
USHORT GlyphID;
USHORT BaseCoordPoint;                                
} BASECOORDFORMAT2;
#define SIZEOF_BASECOORDFORMAT2 8


/* Glyph Metamorphosis table (mort) structures */

typedef struct {
    USHORT  entrySize;      // size in bytes of a lookup entry ( should be 4 )
    USHORT  nEntries;       // number of lookup entries to be searched
    USHORT  searchRange;
    USHORT  entrySelector;
    USHORT  rangeShift;
} MORTBINSRCHHEADER;
#define SIZEOF_MORTBINSRCHHEADER 10

typedef struct {
    USHORT  glyphid1;       // the glyph index for the horizontal shape
    USHORT  glyphid2;       // the glyph index for the vertical shape
} MORTLOOKUPSINGLE;
#define SIZEOF_MORTLOOKUPSINGLE 4

typedef struct {
    BYTE           constants1[12];
    ULONG          length1;
    BYTE           constants2[16];
    BYTE           constants3[16];
    BYTE           constants4[8];
    USHORT         length2;
    BYTE           constants5[8];
/*    MORTBINSRCHHEADER  SearchHeader; */
/*    MORTLOOKUPSINGLE   entries[1];  */
} MORTHEADER;
#define SIZEOF_MORTHEADER 66


/* other defines for font file processing ------------------------------- */

#ifndef TRUE
#define TRUE             1
#define FALSE            0
#endif

#define ROMAN          ((BYTE) 0x00)
#define BOLD           ((BYTE) 0x01)
#define ITALIC         ((BYTE) 0x02)
#define BOLDITALIC     ((BYTE) 0x03)
#define BLDIT_MASK     ((BYTE) 0x03)
#define UNDERSCORE     ((BYTE) 0x04)
#define STRIKEOUT      ((BYTE) 0x08)

#pragma pack()

#endif /* TTFF_DOT_H_DEFINED */
