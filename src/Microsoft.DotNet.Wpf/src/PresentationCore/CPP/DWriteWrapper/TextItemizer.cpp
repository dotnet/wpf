// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "TextItemizer.h"
#include "ItemProps.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    TextItemizer::TextItemizer(DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>*     pScriptAnalysisListHead,
                               DWriteTextAnalysisNode<IDWriteNumberSubstitution*>* pNumberSubstitutionListHead)
    {
        _pScriptAnalysisListHead     = pScriptAnalysisListHead;
        _pNumberSubstitutionListHead = pNumberSubstitutionListHead;

        _isDigitList           = gcnew List<bool>();
        _isDigitListRanges     = gcnew List<array<UINT32>^>();
    }

    UINT32 TextItemizer::GetNextSmallestPos(
        __deref_inout_ecount(1) DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>**     ppScriptAnalysisCurrent, 
        __inout_ecount(1) UINT32& scriptAnalysisRangeIndex,
        __deref_inout_ecount(1) DWriteTextAnalysisNode<IDWriteNumberSubstitution*>** ppNumberSubstitutionCurrent,
        __inout_ecount(1) UINT32& numberSubstitutionRangeIndex,
        __inout_ecount(1) UINT32& isDigitIndex, 
        __inout_ecount(1) UINT32& isDigitRangeIndex
        )
    {
        
        UINT32 scriptAnalysisPos = (*ppScriptAnalysisCurrent != NULL)?(*ppScriptAnalysisCurrent)->Range[scriptAnalysisRangeIndex] : UInt32::MaxValue;
        UINT32 numberSubPos      = (*ppNumberSubstitutionCurrent != NULL)?(*ppNumberSubstitutionCurrent)->Range[numberSubstitutionRangeIndex] : UInt32::MaxValue;
        UINT32 isDigitPos        = (isDigitIndex < (UINT32)_isDigitListRanges->Count)?_isDigitListRanges[isDigitIndex][isDigitRangeIndex] : UInt32::MaxValue;

        UINT32 smallestPos = Math::Min(scriptAnalysisPos, numberSubPos);
        smallestPos = Math::Min(smallestPos, isDigitPos);
        if (smallestPos == scriptAnalysisPos)
        {
            if ((scriptAnalysisRangeIndex + 1) / 2 == 1)
            {
                (*ppScriptAnalysisCurrent) = (*ppScriptAnalysisCurrent)->Next;
            }
            scriptAnalysisRangeIndex = (scriptAnalysisRangeIndex + 1) % 2;
        }
        else if (smallestPos == numberSubPos)
        {                  
            if ((numberSubstitutionRangeIndex + 1) / 2 == 1)
            {
                (*ppNumberSubstitutionCurrent) = (*ppNumberSubstitutionCurrent)->Next;
            }
            numberSubstitutionRangeIndex = (numberSubstitutionRangeIndex + 1) % 2;
        }
        else
        {         
            isDigitIndex     += (isDigitRangeIndex + 1) / 2;
            isDigitRangeIndex = (isDigitRangeIndex + 1) % 2;            
        }
        return smallestPos;

    }

    __declspec(noinline) IList<Span^>^ TextItemizer::Itemize(CultureInfo^ numberCulture, __in_ecount(textLength) CharAttributeType* pCharAttribute, UINT32 textLength)
    {
        DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>*     pScriptAnalysisListPrevious     = _pScriptAnalysisListHead;
        DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>*     pScriptAnalysisListCurrent      = _pScriptAnalysisListHead;
        UINT32 scriptAnalysisRangeIndex = 0;

        DWriteTextAnalysisNode<IDWriteNumberSubstitution*>* pNumberSubstitutionListPrevious = _pNumberSubstitutionListHead;
        DWriteTextAnalysisNode<IDWriteNumberSubstitution*>* pNumberSubstitutionListCurrent  = _pNumberSubstitutionListHead;
        UINT32 numberSubstitutionRangeIndex = 0;      

        UINT32 isDigitIndex      = 0;
        UINT32 isDigitIndexOld   = 0;
        UINT32 isDigitRangeIndex = 0;

        UINT32 rangeStart;
        UINT32 rangeEnd;

        rangeEnd = GetNextSmallestPos(&pScriptAnalysisListCurrent, scriptAnalysisRangeIndex, 
                                      &pNumberSubstitutionListCurrent, numberSubstitutionRangeIndex,
                                      isDigitIndex, isDigitRangeIndex);

        List<Span^>^ spanVector = gcnew List<Span^>();
        while (
            rangeEnd != textLength 
            && (pScriptAnalysisListCurrent != NULL
            || pNumberSubstitutionListCurrent != NULL
            || isDigitIndex            < (UINT32)_isDigitList->Count)
            )
        {
            rangeStart = rangeEnd;
            while(rangeEnd == rangeStart)
            {
                pScriptAnalysisListPrevious     = pScriptAnalysisListCurrent;
                pNumberSubstitutionListPrevious = pNumberSubstitutionListCurrent;
                isDigitIndexOld                 = isDigitIndex;

                rangeEnd = GetNextSmallestPos(&pScriptAnalysisListCurrent, scriptAnalysisRangeIndex,
                                              &pNumberSubstitutionListCurrent, numberSubstitutionRangeIndex,
                                              isDigitIndex, isDigitRangeIndex);
            }

            IDWriteNumberSubstitution* pNumberSubstitution = NULL;
            if (pNumberSubstitutionListPrevious != NULL
             && rangeEnd >  pNumberSubstitutionListPrevious->Range[0] 
             && rangeEnd <= pNumberSubstitutionListPrevious->Range[1])
            {
                pNumberSubstitution = pNumberSubstitutionListPrevious->Value;
            }

            // Assign HasCombiningMark
            bool hasCombiningMark = false;
            for (UINT32 i = rangeStart; i < rangeEnd; ++i)
            {
                if ((pCharAttribute[i] & CharAttribute::IsCombining) != 0)
                {
                    hasCombiningMark = true;
                    break;
                }
            }

            // Assign NeedsCaretInfo
            // When NeedsCaretInfo is false (and the run does not contain any combining marks)
            // this makes caret navigation happen on the character level 
            // and not the cluster level. When we have an itemized run based on DWrite logic
            // that contains more than one WPF 3.5 scripts (based on unicode 3.x) we might run
            // into a rare scenario where one script allows, for example, ligatures and the other not.
            // In that case we default to false and let the combining marks check (which checks for
            // simple and complex combining marks) decide whether character or cluster navigation
            // will happen for the current run.
            bool needsCaretInfo = true;
            for (UINT32 i = rangeStart; i < rangeEnd; ++i)
            {
                // Does NOT need caret info
                if (((pCharAttribute[i] & CharAttribute::IsStrong) != 0) && ((pCharAttribute[i] & CharAttribute::NeedsCaretInfo) == 0))
                {
                    needsCaretInfo = false;
                    break;
                }
            }

            int strongCharCount = 0;
            int latinCount = 0;
            int indicCount = 0;
            bool hasExtended = false;
            for (UINT32 i = rangeStart; i < rangeEnd; ++i)
            {
                if ((pCharAttribute[i] & CharAttribute::IsExtended) != 0)
                {
                    hasExtended = true;
                }
                

                // If the current character class is Strong.
                if ((pCharAttribute[i] & CharAttribute::IsStrong) != 0)
                {
                    strongCharCount++;

                    if ((pCharAttribute[i] & CharAttribute::IsLatin) != 0)
                    {
                        latinCount++;
                    }
                    else if((pCharAttribute[i] & CharAttribute::IsIndic) != 0)
                    {
                        indicCount++;
                    }
                }
            }

            // Assign isIndic
            // For the isIndic check we mark the run as Indic if it contains atleast
            // one strong Indic character based on the old WPF 3.5 script ids.
            // The isIndic flag is eventually used by LS when checking for the max cluster
            // size that can form for the current run so that it can break the line properly.
            // So our approach is conservative. 1 strong Indic character will make us 
            // communicate to LS the max cluster size possible for correctness.
            bool isIndic = (indicCount > 0);

            // Assign isLatin
            // We mark a run to be Latin iff all the strong characters in it
            // are Latin based on the old WPF 3.5 script ids.
            // This is a conservative approach for correct line breaking behavior.
            // Refer to the comment about isIndic above.
            bool isLatin = (strongCharCount > 0) && (latinCount == strongCharCount);

            ItemProps^ itemProps = ItemProps::Create(
                    &(pScriptAnalysisListPrevious->Value),
                    pNumberSubstitution,
                    _isDigitList[isDigitIndexOld] ? numberCulture : nullptr,
                    hasCombiningMark,
                    needsCaretInfo,
                    hasExtended,
                    isIndic,
                    isLatin
                    );

            spanVector->Add(gcnew Span(itemProps, (int)(rangeEnd - rangeStart)));
        }

        return spanVector;
    }  

    void TextItemizer::SetIsDigit(
                                 UINT32 textPosition,
                                 UINT32 textLength,
                                 bool   isDigit
                                 )
    {
        _isDigitList->Add(isDigit);
        array<UINT32>^ range = gcnew array<UINT32>(2);
        range[0] = textPosition;
        range[1] = textPosition + textLength;
        _isDigitListRanges->Add(range);
    }
}}}}//MS::Internal::Text::TextInterface
