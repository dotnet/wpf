// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: MTXCALC.H
 *
 *
 * Function prototypes for MTXCALC.C.
 *
 **************************************************************************/

#ifndef MTXCALC_DOT_H_DEFINED
#define MTXCALC_DOT_H_DEFINED

/* macro definitions ---------------------------------------------------- */

/* function prototypes -------------------------------------------------- */
int16 ComputeMaxPStats( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                        uint16 *  pusMaxContours,
                        uint16 *  pusMaxPoints,
                        uint16 *  pusMaxCompositeContours,
                        uint16 *  pusMaxCompositePoints,
                        uint16 *  pusMaxInstructions,
                        uint16 *  pusMaxComponentElements,
                        uint16 *  pusMaxComponentDepth,
                        uint16 *  pausComponents, 
                        uint16 usnMaxComponents);

#endif /* MTXCALC_DOT_H_DEFINED    */
