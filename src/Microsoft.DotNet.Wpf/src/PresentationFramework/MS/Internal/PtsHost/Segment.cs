// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Interface representing PTS segment. 
//


using System;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Interface representing PTS segment.
    /// </summary>
    internal interface ISegment
    {
        /// <summary>
        /// Get first para 
        /// </summary>
        /// <param name="successful">
        /// OUT: does segment contain any paragraph?
        /// </param>
        /// <param name="firstParaName">
        /// OUT: name of the first paragraph in segment
        /// </param>
        void GetFirstPara(
            out int successful,                 
            out IntPtr firstParaName);           
        
        /// <summary>
        /// Get next para 
        /// </summary>
        /// <param name="currentParagraph">
        /// IN: current para
        /// </param>
        /// <param name="found">
        /// OUT: is there next paragraph?
        /// </param>
        /// <param name="nextParaName">
        /// OUT: name of the next paragraph in section
        /// </param>
        void GetNextPara(
            BaseParagraph currentParagraph,      
            out int found,                       
            out IntPtr nextParaName);           

        /// <summary>
        /// Get first change in segment - part of update process
        /// </summary>
        /// <param name="fFound">
        /// OUT: anything changed?
        /// </param>
        /// <param name="fChangeFirst">
        /// OUT: first paragraph changed?
        /// </param>
        /// <param name="nmpBeforeChange">
        /// OUT: name of paragraph before the change if !fChangeFirst
        /// </param>
        void UpdGetFirstChangeInSegment(
            out int fFound,                     
            out int fChangeFirst,                
            out IntPtr nmpBeforeChange);         
    }
}
