// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * TTFDelta.h: Interface file for TTFDelta.c - Written by Louise Pathe
  *
  *
  * 
  */
  
#ifndef TTFDELTA_DOT_H_DEFINED
#define TTFDELTA_DOT_H_DEFINED        

#ifndef CONST
#define CONST const
#endif     

#ifndef CFP_ALLOCPROC_DEFINED
#define CFP_ALLOCPROC_DEFINED
typedef void *(*CFP_ALLOCPROC)(size_t);
typedef void *(*CFP_REALLOCPROC)(void *, size_t);
typedef void (*CFP_FREEPROC)(void *);
#endif

short TTCOffsetTableOffset(CONST unsigned char * puchSrcBuffer,
            CONST unsigned long ulSrcBufferSize,
            CONST unsigned short usTTCIndex,
            unsigned long *pulOffsetTableOffset);


/* return codes defined in ttferror.h */
short SubsetTTF(CONST unsigned char * puchSrcBuffer,
          unsigned char * puchDestBuffer,
        CONST unsigned long ulBufferSize,
        unsigned long * pulBytesWritten,
        CONST unsigned short usLanguage,
        CONST unsigned short usPlatform,
        CONST unsigned short usEncoding,
        CONST unsigned short *pusKeepCharCodeList,
        CONST unsigned short usListCount,
        CONST unsigned short usTTCIndex);

/* return codes defined in ttferror.h */
short CreateDeltaTTF(CONST unsigned char * puchSrcBuffer,
            CONST unsigned long ulSrcBufferSize,
              unsigned char ** ppuchDestBuffer,
            unsigned long * pulDestBufferSize,
            unsigned long * pulBytesWritten,
            CONST unsigned short usFormat,
            CONST unsigned short usLanguage,
            CONST unsigned short usPlatform,
            CONST unsigned short usEncoding,
            CONST unsigned short usListType,
            CONST unsigned short *pulKeepCodeList,
            CONST unsigned short usKeepListCount,
            CFP_REALLOCPROC lpfnReAllocate,
            CFP_FREEPROC lpfnFree,
            unsigned long ulOffsetTableOffset,  
            void * lpvReserved);
short CreateDeltaTTFEx(CONST unsigned char * puchSrcBuffer,
            CONST unsigned long ulSrcBufferSize,
              unsigned char ** ppuchDestBuffer,
            unsigned long * pulDestBufferSize,
            unsigned long * pulBytesWritten,
            CONST unsigned short usFormat,
            CONST unsigned short usLanguage,
            CONST unsigned short usPlatform,
            CONST unsigned short usEncoding,
            CONST unsigned short usListType,
            CONST unsigned long* pulKeepCodeList,
            CONST unsigned short usKeepListCount,
            CFP_REALLOCPROC lpfnReAllocate,
            CFP_FREEPROC lpfnFree,
            unsigned long ulOffsetTableOffset,  
            void * lpvReserved);


/* for CreateDelta Formats */
#define TTFDELTA_SUBSET 0      /* Straight Subset Font */
#define TTFDELTA_SUBSET1 1      /* Subset font with full TTO and Kern tables. For later merge */
#define TTFDELTA_DELTA 2      /* Delta font */
#define TTFDELTA_MERGE 3      /* already merged font - for checking input */

/* for usListType argument */
#define TTFDELTA_CHARLIST 0
#define TTFDELTA_GLYPHLIST 1

/* for usPlatform ID values */
#define TTFSUB_UNICODE_PLATFORMID 0
#define TTFSUB_APPLE_PLATFORMID   1
#define TTFSUB_ISO_PLATFORMID     2
#define TTFSUB_MS_PLATFORMID      3
#define TTFSUB_NUM_PLATFORMS      4

/* for usEncoding values */
#define TTFSUB_STD_MAC_CHAR_SET  0    /* goes with TTFSUB_APPLE_PLATFORMID */
#define TTFSUB_SYMBOL_CHAR_SET  0    /* goes with TTFSUB_MS_PLATFORMID */
#define TTFSUB_UNICODE_CHAR_SET  1    /* goes with TTFSUB_MS_PLATFORMID */
#define TTFSUB_SURROGATE_CHAR_SET 10 /* "" */
#define TTFSUB_DONT_CARE         0xFFFF

/* for usLanguage values */
#define TTFSUB_LANG_KEEP_ALL 0


#endif /* TTFDELTA_DOT_H_DEFINED */
