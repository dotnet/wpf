// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFACC.C
 *
 *
 * Routines to read data in a platform independent way from 
 * a file buffer
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */

#include <string.h> /* for memcpy */
#include <stdlib.h> /* for max */

#include "TypeDefs.h"     /* for uint8 etc definition */
#include "TTFAcc.h"
#include "TTFCntrl.h"
#ifdef _DEBUG
#include <stdio.h>
#endif

#if 0
/* turn back into a macro, because it gets called so much */
#define CheckInOffset(a, b, c) \
    if ((a->puchBuffer == NULL) || (b > a->ulBufferSize) || (b + c > a->ulBufferSize) || (b + c < b))     \
        return ERR_READOUTOFBOUNDS \
        
#endif
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 CheckInOffset(TTFACC_FILEBUFFERINFO *a,  uint32 b, uint32 c)
{
    if (a->puchBuffer == NULL) /* a prior realloc may have failed */
    {    
        return ERR_READOUTOFBOUNDS;
    }

    if ((b > a->ulBufferSize) || (b + c > a->ulBufferSize) || (b + c < b))
    {
        return ERR_READOUTOFBOUNDS;
    }

    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 CheckOutOffset(TTFACC_FILEBUFFERINFO *a, register uint32 b, register uint32 c) 
{
    if (a->puchBuffer == NULL) /* a prior realloc may have failed */
    {
        return ERR_WRITEOUTOFBOUNDS;
    }

    if (b + c < b)
    {
        return ERR_WRITEOUTOFBOUNDS; 
    }

    if (b + c > a->ulBufferSize)  
    { 
        if (a->lpfnReAllocate == NULL) 
        {
            return ERR_WRITEOUTOFBOUNDS; 
        }

        if ((uint32) (a->ulBufferSize * 11/10) > b + c) 
        {  
#ifdef _DEBUG
#if !defined(ARGITERATOR_SUPPORTED) || (defined(ARGITERATOR_SUPPORTED) && ARGITERATOR_SUPPORTED)
			printf("we're reallocating 10 percent (%lu) more bytes\n", (uint32)(a->ulBufferSize * .1));
#endif
#endif
            a->ulBufferSize = (uint32) (a->ulBufferSize * 11/10);
        } 
        else 
        {  
#ifdef _DEBUG
#if !defined(ARGITERATOR_SUPPORTED) || (defined(ARGITERATOR_SUPPORTED) && ARGITERATOR_SUPPORTED)
            printf("we're reallocating (%lu) more bytes\n",(uint32) (b + c - a->ulBufferSize));    
#endif
#endif
            a->ulBufferSize = b + c; 
        } 
            
        if ((a->puchBuffer = (uint8 *)a->lpfnReAllocate(a->puchBuffer, a->ulBufferSize)) == NULL) 
        {
            a->ulBufferSize = 0L;
            return ERR_MEM;
        }
    } 

    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 ReadByte(TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * puchBuffer, uint32 ulOffset)
{
    int16 errCode;

    errCode = CheckInOffset(pInputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint8)));
    if (errCode != NO_ERROR)
    {
        return errCode;
    }
    
    *puchBuffer = *(pInputBufferInfo->puchBuffer + ulOffset);
    return NO_ERROR;    
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) int16 
ReadWord(TTFACC_FILEBUFFERINFO * pInputBufferInfo, UNALIGNED uint16 * pusBuffer, uint32 ulOffset)
{
    int16 errCode;

    errCode = CheckInOffset(pInputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint16)));
    if (errCode != NO_ERROR)
    {
        return errCode;
    }

    *pusBuffer = SWAPW(*(pInputBufferInfo->puchBuffer + ulOffset));
    return NO_ERROR;
}    
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 ReadLong(TTFACC_FILEBUFFERINFO * pInputBufferInfo, UNALIGNED uint32 * pulBuffer, uint32 ulOffset)
{
    int16 errCode;

    errCode = CheckInOffset(pInputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint32)));
    if (errCode != NO_ERROR)
    {
        return errCode;
    }

    *pulBuffer = SWAPL(*(pInputBufferInfo->puchBuffer + ulOffset));
    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 ReadBytes(TTFACC_FILEBUFFERINFO * pInputBufferInfo, __out_ecount(Count) uint8 * puchBuffer, uint32 ulOffset, uint32 Count)
{
    int16 errCode;

    errCode = CheckInOffset(pInputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint8)) * Count);
    if (errCode != NO_ERROR)
    {
        return errCode;
    }

    memcpy(puchBuffer, pInputBufferInfo->puchBuffer + ulOffset, Count); 
    return NO_ERROR;    
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteByte(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint8 uchValue, uint32 ulOffset)
{
    int16 errCode;

    if ((errCode = CheckOutOffset(pOutputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint8)))) != NO_ERROR)
    {
        return errCode;
    }

    *(pOutputBufferInfo->puchBuffer + ulOffset) = uchValue;
    return NO_ERROR;    
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteWord(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint16 usValue, uint32 ulOffset)
{
    int16 errCode;

    if ((errCode = CheckOutOffset(pOutputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint16)))) != NO_ERROR)
    {
        return errCode;
    }

    * (UNALIGNED uint16 *) (pOutputBufferInfo->puchBuffer + ulOffset) = SWAPW(usValue);
    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteLong(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 ulValue, uint32 ulOffset)
{
    int16 errCode;

    if ((errCode = CheckOutOffset(pOutputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint32)))) != NO_ERROR)
    {
        return errCode;
    }

    * (UNALIGNED uint32 *) (pOutputBufferInfo->puchBuffer + ulOffset) = SWAPL(ulValue);
    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteBytes(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint8 * puchBuffer, uint32 ulOffset, uint32 Count)
{
    int16 errCode;

    if ((errCode = CheckOutOffset(pOutputBufferInfo, ulOffset, static_cast<uint32>(sizeof(uint8)) * Count)) != NO_ERROR)
    {
        return errCode;
    }

    memcpy(pOutputBufferInfo->puchBuffer + ulOffset, puchBuffer, Count); 
    return NO_ERROR;    
}
/* ---------------------------------------------------------------------- */
/* ReadGeneric - Generic read of data - Translation buffer provided for Word and Long swapping and RISC alignment handling */
/* 
Output:
puchDestBuffer updated with new data
pusByteRead - number of bytes read 
Return:
0 if OK
error code if not. */
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 ReadGeneric(
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, /* buffer info of file buffer to read from */
    uint8 * puchBuffer,      /* buffer to read into - pad according to pControl data    */
    uint16 usBufferSize,     /* size of buffer */
    uint8 * puchControl,    /* pControl - describes the size of each element in the structure, if a pad byte should be inserted in the output buffer */
    uint32 ulOffset,           /* offset into input TTF Buffer of where to read */
    uint16 * pusBytesRead /* number of bytes read from the file */
)
{
uint32 ulCurrOffset = ulOffset;      /* offset into TTF data buffer */
uint16 usBufferOffset = 0;          /* offset into local read buffer */
uint16 usControlCount;        /* number of elements in the Control array */
UNALIGNED uint16 *pusBuffer;                  /* coerced puchBuffer */
UNALIGNED uint32 *pulBuffer;
uint16 i;
int16 errCode;

    usControlCount = puchControl[0]; 
    for (i = 1; i <= usControlCount; ++i)
    {
        switch (puchControl[i] & TTFACC_DATA)
        {
        case TTFACC_BYTE:
            if (usBufferOffset + sizeof(uint8) > usBufferSize)
                return ERR_READCONTROL;  /* trying to stuff too many bytes into target buffer */ 
            if (puchControl[i] & TTFACC_PAD) /* don't read, just pad */
                *(puchBuffer + usBufferOffset) = 0;
            else
            {
                if ((errCode = ReadByte(pInputBufferInfo, puchBuffer + usBufferOffset, ulCurrOffset))!=NO_ERROR)
                    return errCode;

                ulCurrOffset += sizeof(uint8);
            }
            usBufferOffset += sizeof(uint8);
        break;
        case TTFACC_WORD:
            if (usBufferOffset + sizeof(uint16) > usBufferSize)
                return ERR_READCONTROL;  /* trying to stuff too many bytes into target buffer */ 
            pusBuffer = (uint16 *) (puchBuffer + usBufferOffset);
            if (puchControl[i] & TTFACC_PAD) /* don't read, just pad */
                *pusBuffer = 0;
            else
            {
                if (puchControl[i] & TTFACC_NO_XLATE)
                {
                    if ((errCode = ReadBytes(pInputBufferInfo, puchBuffer + usBufferOffset, ulCurrOffset, 2))!=NO_ERROR)
                        return errCode;
                }
                else
                {
                    if ((errCode = ReadWord(pInputBufferInfo, pusBuffer, ulCurrOffset))!=NO_ERROR)
                        return errCode;
                }
                ulCurrOffset += sizeof(uint16);
            }
            usBufferOffset += sizeof(uint16);
        break;
        case TTFACC_LONG:
            if (usBufferOffset + sizeof(uint32) > usBufferSize)
                return ERR_READCONTROL;  /* trying to stuff too many bytes into target buffer */ 
            pulBuffer = (uint32 *) (puchBuffer + usBufferOffset);
            if (puchControl[i] & TTFACC_PAD) /* don't read, just pad */
                *pulBuffer = 0;
            else
            {
                  if (puchControl[i] & TTFACC_NO_XLATE)
                {
                    /* read as 4 bytes instead */
                    if ((errCode = ReadBytes(pInputBufferInfo, puchBuffer + usBufferOffset, ulCurrOffset, 4))!=NO_ERROR)
                        return errCode;
                }
                else
                {
                    if ((errCode = ReadLong(pInputBufferInfo, pulBuffer, ulCurrOffset))!=NO_ERROR)
                        return errCode;
                }
                ulCurrOffset += sizeof(uint32);
            }
            usBufferOffset += sizeof(uint32);
        break;
        default:
            return ERR_READCONTROL; /* don't read any, bad control */
        }  /* end switch */
    } /* end for i */
    if (usBufferOffset < usBufferSize)  /* didn't fill up the buffer */
        return ERR_READCONTROL;  /* control thing doesn't fit the buffer */
    * pusBytesRead = (uint16) (ulCurrOffset - ulOffset); 
    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
/* read an array of items repeatedly
Output
puchDestBuffer updated with new data
pusByteRead - number of bytes read total 
Return:
0 if OK    or
ErrorCode  */
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 ReadGenericRepeat(
TTFACC_FILEBUFFERINFO * pInputBufferInfo, /* buffer info of file buffer to read from */
uint8 * puchBuffer,     /* buffer to read into - pad according to pControl data    */
uint8 * puchControl,     /* pControl - describes the size of each element in the structure, if a pad byte should be inserted in the output buffer */
uint32 ulOffset,         /* offset into input TTF Buffer of where to read */
uint32 * pulBytesRead,    /* number of bytes read from the file */
uint16 usItemCount,     /* number of times to read into the buffer */
uint16 usItemSize         /* size of item in buffer */
)
{
uint16 i;
int16 errCode;
uint16 usBytesRead;

    for (i = 0; i < usItemCount; ++i)
    {
        errCode = ReadGeneric( pInputBufferInfo, puchBuffer, usItemSize, puchControl, ulOffset, &usBytesRead);
        if (errCode != NO_ERROR)
            return errCode;
        ulOffset += usBytesRead;
        puchBuffer += usItemSize;
    }

    *pulBytesRead = usItemSize * usItemCount;
    return NO_ERROR;
}    
/* ---------------------------------------------------------------------- */
/* WriteGeneric - Generic write of data - Translation buffer provided for Word and Long swapping and RISC alignment handling
Output:
puchDestBuffer updated with new data
pusBytesWritten - Number of bytes written.
Return:
0 or Error Code
*/
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteGeneric(
TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
uint8 * puchBuffer, 
uint16 usBufferSize,
uint8 * puchControl, 
uint32 ulOffset, /* offset into output TTF Buffer of where to write */
uint16 * pusBytesWritten)
{
uint32 ulCurrOffset = ulOffset;      /* offset into TTF data buffer */
uint16 usControlCount;
uint16 usBufferOffset = 0;          /* offset into local read buffer */
UNALIGNED uint16 *pusBuffer;                  /* coerced puchBuffer */
UNALIGNED uint32 *pulBuffer;
uint16 i;
uint32 ulBytesWritten;
int16 errCode;
 
    usControlCount = puchControl[0]; 
    for (i = 1; i <= usControlCount; ++i)
    {
        switch (puchControl[i] & TTFACC_DATA)
        {
        case TTFACC_BYTE:
            if (!(puchControl[i] & TTFACC_PAD))
            {
                if (usBufferOffset + sizeof(uint8) > usBufferSize)
                    return ERR_WRITECONTROL;  /* trying to read too many bytes from source buffer */ 
                if ((errCode = WriteByte(pOutputBufferInfo, *(puchBuffer + usBufferOffset), ulCurrOffset))!=NO_ERROR)
                    return errCode;

                ulCurrOffset += sizeof(uint8);
            }
            usBufferOffset += sizeof(uint8);
            break;
        case TTFACC_WORD:
            if (!(puchControl[i] & TTFACC_PAD))
            {
                if (usBufferOffset + sizeof(uint16) > usBufferSize)
                    return ERR_WRITECONTROL;  /* trying to read too many bytes from source buffer */ 

                 pusBuffer = (uint16 *) (puchBuffer + usBufferOffset);
                if (puchControl[i] & TTFACC_NO_XLATE)
                {
                    if ((errCode = WriteBytes(pOutputBufferInfo, puchBuffer + usBufferOffset, ulCurrOffset, 2))!=NO_ERROR)
                        return errCode;
                }
                else
                {
                    if ((errCode = WriteWord(pOutputBufferInfo, *pusBuffer, ulCurrOffset))!=NO_ERROR)
                        return errCode;
                }
                ulCurrOffset += sizeof(uint16);
            }
            usBufferOffset += sizeof(uint16);
            break;
        case TTFACC_LONG:
            if (!(puchControl[i] & TTFACC_PAD)) 
            {
                if (usBufferOffset + sizeof(uint32) > usBufferSize)
                    return ERR_WRITECONTROL;  /* trying to read too many bytes from source buffer */ 

                 pulBuffer = (uint32 *) (puchBuffer + usBufferOffset);
                if (puchControl[i] & TTFACC_NO_XLATE)
                {
                    if ((errCode = WriteBytes(pOutputBufferInfo, puchBuffer + usBufferOffset, ulCurrOffset, 4))!=NO_ERROR)
                        return errCode;
                }
                else
                {
                    if ((errCode = WriteLong(pOutputBufferInfo, *pulBuffer, ulCurrOffset))!=NO_ERROR)
                        return errCode;
                }
                ulCurrOffset += sizeof(uint32);
            }
            usBufferOffset += sizeof(uint32);
            break;
        default:
            return ERR_WRITECONTROL; /* don't write any, bad control */
        }  /* end switch */
    } /* end for i */
    if (usBufferOffset < usBufferSize)  /* didn't read all the bytes in buffer */
        return ERR_WRITECONTROL;  /* control thing doesn't fit the buffer */
    ulBytesWritten = ulCurrOffset - ulOffset;
    *pusBytesWritten = (uint16)(ulCurrOffset - ulOffset);
    if (ulBytesWritten != *pusBytesWritten)       /* check to see if it fits */
        return ERR_FORMAT;
    return NO_ERROR; 
}
/* ---------------------------------------------------------------------- */
/* write an array of items repeatedly
Output
puchDestBuffer updated with new data
pusByteWritten - number of bytes written total 
Return:
0 if OK    or
ErrorCode  */
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
int16 WriteGenericRepeat(
TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
uint8 * puchBuffer,     /* buffer to read from - pad according to pControl data    */
uint8 * puchControl,     /* pControl - describes the size of each element in the structure, if a pad byte should be inserted in the output buffer */
uint32 ulOffset,         /* offset into output TTF Buffer of where to write */
uint32 * pulBytesWritten,/* number of bytes written to the file */
uint16 usItemCount,     /* number of times to read into the buffer */
uint16 usItemSize         /* size of item in buffer */
)
{
uint16 i;
int16 errCode;
uint16 usBytesWritten;

    for (i = 0; i < usItemCount; ++i)
    {
        errCode = WriteGeneric( pOutputBufferInfo, puchBuffer, usItemSize, puchControl, ulOffset, &usBytesWritten);
        if (errCode != NO_ERROR)
            return errCode;
        ulOffset += usBytesWritten;
        puchBuffer += usItemSize;
    }

    *pulBytesWritten = usItemSize * usItemCount;
    return NO_ERROR;
}    

/* ---------------------------------------------------------------------- */

uint16 GetGenericSize(uint8 * puchControl) 
{
uint16 usCurrOffset = 0;     
uint16 usControlCount;
uint16 i;

    usControlCount = puchControl[0]; 
    for (i = 1; i <= usControlCount; ++i)
    {
        switch (puchControl[i] & TTFACC_DATA)
        {
        case TTFACC_BYTE:
            if (!(puchControl[i] & TTFACC_PAD))
                usCurrOffset += sizeof(uint8);
            break;
        case TTFACC_WORD:
            if (!(puchControl[i] & TTFACC_PAD))
                usCurrOffset += sizeof(uint16);
            break;
        case TTFACC_LONG:
            if (!(puchControl[i] & TTFACC_PAD)) 
                usCurrOffset += sizeof(uint32);
            break;
        default:
            return 0; 
        }  /* end switch */
    } /* end for i */
    return usCurrOffset; 
}
/* ---------------------------------------------------------------------- */
/* next 2 functions moved from ttftabl1.c to allow inline ReadLong access */
/* calc checksum of an as-yet unwritten Directory. */
__checkReturn __success(return==NO_ERROR) 
int16 CalcChecksum( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                  uint32 ulOffset,
                  uint32 ulLength,
                  uint32 * pulChecksum )
{
    uint32 ulWord;
    uint32 ulEndOffset;
    int16  errCode;
    uint32 i;

    *pulChecksum = 0;

    // Check bounds one for the whole table
    if ((errCode = CheckInOffset(pInputBufferInfo, ulOffset, ulLength)) != NO_ERROR)
    {
        return errCode;
    }

    ulEndOffset = ulOffset + (ulLength & ~3); // We will not for now include the tail that is not 4-byte even
    
    for ( ;ulOffset < ulEndOffset; ulOffset+=sizeof(uint32) )
    {
        *pulChecksum += SWAPL(*(pInputBufferInfo->puchBuffer + ulOffset));
    }

    // Now we go for the tail, we have (ulLength & 3) bytes and the rest is virtual zeros
    if (ulLength % 4)
    {
        ulWord = 0;
        
        for(i=0; i < (ulLength % 4); i++)
        {
            ulWord = (ulWord << 8) + *(pInputBufferInfo->puchBuffer + ulOffset + i);
        }

        ulWord = ulWord << ( (4 - i) * 8 );

        *pulChecksum += ulWord;
    }
    
    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
uint16 CalcFileChecksum( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint32 ulLength, uint32 * pulChecksum)
{
    return CalcChecksum(pInputBufferInfo, 0, ulLength, pulChecksum);
}
/* ---------------------------------------------------------------------- */

__checkReturn __success(return==NO_ERROR) 
uint16 UTF16toUCS4(uint16 *pUTF16, uint16 usCountUTF16, uint32 *pUCS4, uint16 usCountUCS4, uint16 *pusChars)
{
    uint16 i;
    uint32 charCode;
    uint32 offset;
    uint16 high, low;

    *pusChars = 0;

    /* Convert the UTF-16 string we got to UCS-4 (32bits) */
    offset = 0x10000 - (0xD800 << 10) - 0xDC00;
    i = 0;
    while (i < usCountUTF16)
    {
        /* is it a High surrogate? */
        high = pUTF16[i++];
        if (i < usCountUTF16 && 0xD800 <= high && high <= 0xDBFF)
        {
            low = pUTF16[i++];
            if (0xDC00 <= low && low <= 0xDFFF)
            {
                charCode = ((high << 10) + low + offset);
            }
            else
            {
                /* ignore the high surrogate and restart processing with this */
                i--;
                continue;
            }
        }
        else
            charCode = (uint32)high;
            
        if (*pusChars < usCountUCS4)
            pUCS4[*pusChars] = charCode;
        (*pusChars)++;
    }

    if (*pusChars > usCountUCS4)
        return ERR_MEM;
    return NO_ERROR;
}

/* Init function. Set the function pointers to the default functions below. */
void InitFileBufferInfo(TTFACC_FILEBUFFERINFO * pBufferInfo, uint8 *puchBuffer, uint32 ulBufferSize, CFP_REALLOCPROC lpfnReAlloc)
{
    pBufferInfo->puchBuffer = puchBuffer;
    pBufferInfo->ulBufferSize = ulBufferSize;
    pBufferInfo->ulOffsetTableOffset = 0;
    pBufferInfo->lpfnReAllocate = lpfnReAlloc;
}

void InitConstFileBufferInfo(CONST_TTFACC_FILEBUFFERINFO * pBufferInfo, CONST uint8 *puchBuffer, uint32 ulBufferSize)
{
    InitFileBufferInfo((TTFACC_FILEBUFFERINFO *)pBufferInfo, (uint8*)puchBuffer, ulBufferSize, NULL /* cant reallocate const */);
}
