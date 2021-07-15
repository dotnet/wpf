// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    win_mil_graphics_media
//      $Keywords:
//
//  $Description:
//      Provide support for having a set of samples in a circular buffer and
//      having managing the samples. We anticipate queue lengths will remain
//      quite small, so, using a circular vector is probably the cheapest and
//      fastest alternative.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

/*static*/ inline 
SampleQueue::StateViewLogicalSample
SampleQueue::
TranslateViewState(
    __in    LONG                        viewState
    )
{
    StateViewLogicalSample fields =
    {
        static_cast<BYTE>(viewState & msc_fieldMask),
        {  
           static_cast<BYTE>((viewState >> msc_bitsPerField) & msc_fieldMask),
           static_cast<BYTE>((viewState >> (msc_bitsPerField * 2)) & msc_fieldMask)
        },
        static_cast<BYTE>((viewState >> (msc_bitsPerField * 3)) & msc_fieldMask)
    };

    return fields;    
}

/*static*/ inline
LONG
SampleQueue::
TranslateViewState(
    __in    SampleQueue::StateViewLogicalSample      logicalSample
    )
{
    Assert((logicalSample.currentView & msc_fieldMask) == logicalSample.currentView);
    Assert((logicalSample.inUseView[SampleThreads::MixerThread] & msc_fieldMask) == logicalSample.inUseView[SampleThreads::MixerThread]);
    Assert((logicalSample.inUseView[SampleThreads::CompositionThread] & msc_fieldMask) == logicalSample.inUseView[SampleThreads::CompositionThread]);
    
    //
    // The continuity number just wraps around.    
    // 
    logicalSample.continuityNumber = logicalSample.continuityNumber & msc_fieldMask;

    return   logicalSample.currentView 
           | (logicalSample.inUseView[SampleThreads::MixerThread] << msc_bitsPerField)
           | (logicalSample.inUseView[SampleThreads::CompositionThread] << (msc_bitsPerField * 2))
           | (logicalSample.continuityNumber << (msc_bitsPerField * 3));
}

/*static*/ inline
BYTE
SampleQueue::
NextView(
    __in    BYTE                        view
    )
{
    view++;

    return view % (SampleThreads::NumberOfThreads + 1);
}



/*static*/ inline
bool
SampleQueue::
IsPositiveSampleTime(
    __in    LONGLONG            sampleTime
    )
{
    return sampleTime >= 0;
}

/*static*/ inline
bool
SampleQueue::
IsValidSampleIndex(
    __in    BYTE                sampleIndex
    )
{
    //
    // This assertion is valid even with contention because bytes are
    // written atomically
    //
    Assert(sampleIndex < kSamples || sampleIndex == kInvalidSample || sampleIndex == kNoPauseSample);

    return sampleIndex != kInvalidSample && sampleIndex != kNoPauseSample;
}

/*static*/ inline
bool
SampleQueue::
IsExpectedSampleTime(
    __in    LONGLONG            sampleTime
    )
{
    return sampleTime >= 0 || sampleTime == kInvalidTime || sampleTime == kReservedForCompositionTime;
}

