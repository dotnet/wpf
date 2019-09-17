// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Cache object of paragraph content used to improve performance
//             of optimal paragraph formatting
//
//  Spec:      Text Formatting API.doc
//
//

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security;
using System.Windows;
using System.Windows.Media;

using MS.Internal;
using MS.Internal.TextFormatting;
using MS.Internal.PresentationCore;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Text formatter caches all potential breakpoints within a paragraph in this cache object 
    /// during FormatParagraphContent call. This object is to be managed by text layout client. 
    /// The use of this cache is to improve performance of line construction in optimal paragraph 
    /// line formatting.
    /// </summary>
#if OPTIMALBREAK_API
    public sealed class TextParagraphCache : IDisposable
#else
    [FriendAccessAllowed]
    internal sealed class TextParagraphCache : IDisposable
#endif
    {
        private FullTextState                       _fullText;                  // full text state of the whole paragraph
        private SecurityCriticalDataForSet<IntPtr>  _ploparabreak;              // unmanaged LS resource for parabreak session
        private int                                 _finiteFormatWidth;         // finite formatting ideal width
        private bool                                _penalizedAsJustified;      // flag indicating whether the paragraph should be penalized as fully-justified one


        /// <summary>
        /// Construct a paragraph cache to be used during optimal paragraph formatting
        /// </summary>
        internal TextParagraphCache(
            FormatSettings      settings,
            int                 firstCharIndex,
            int                 paragraphWidth
            )
        {
            Invariant.Assert(settings != null);

            // create full text
            _finiteFormatWidth = settings.GetFiniteFormatWidth(paragraphWidth);
            _fullText = FullTextState.Create(settings, firstCharIndex, _finiteFormatWidth);

            // acquiring LS context
            TextFormatterContext context = settings.Formatter.AcquireContext(_fullText, IntPtr.Zero);

            _fullText.SetTabs(context);

            IntPtr ploparabreakValue = IntPtr.Zero;

            LsErr lserr = context.CreateParaBreakingSession(
                firstCharIndex,
                _finiteFormatWidth,
                // breakrec is not needed before the first cp of para cache
                // since we handle Bidi break ourselves.
                IntPtr.Zero,
                ref ploparabreakValue,
                ref _penalizedAsJustified
                );

            // get the exception in context before it is released
            Exception callbackException = context.CallbackException;
            
            // release the context
            context.Release();

            if(lserr != LsErr.None)
            {
                GC.SuppressFinalize(this);
                if(callbackException != null)
                {                        
                    // rethrow exception thrown in callbacks
                    throw new InvalidOperationException(SR.Get(SRID.CreateParaBreakingSessionFailure, lserr), callbackException);
                }
                else
                {
                    // throw with LS error codes
                    TextFormatterContext.ThrowExceptionFromLsError(SR.Get(SRID.CreateParaBreakingSessionFailure, lserr), lserr);
                }
            }

            _ploparabreak.Value = ploparabreakValue;

            // keep context alive till here
            GC.KeepAlive(context);
        }


        /// <summary>
        /// Finalizing paragraph content cache
        /// </summary>
        ~TextParagraphCache()
        {
            Dispose(false);
        }


        /// <summary>
        /// Disposing paragraph content cache
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// Client to format all feasible breakpoints for a line started at the specified character position.
        /// The number of breakpoints returned is restricted by penalty restr
        /// </summary>
        /// <remarks>
        /// This method is provided for direct access of PTS during optimal paragraph process. The breakpoint
        /// restriction handle is passed as part of the parameter to PTS callback pfnFormatLineVariants. The
        /// value comes from earlier steps in PTS code to compute the accumulated penalty of the entire paragraph
        /// using 'best-fit' algorithm.
        /// </remarks>
        internal IList<TextBreakpoint> FormatBreakpoints(
            int                             firstCharIndex,
            TextLineBreak                   previousLineBreak,
            IntPtr                          breakpointRestrictionHandle,
            double                          maxLineWidth,
            out int                         bestFitIndex                             
            )
        {
            // format all potential breakpoints starting from the specified firstCharIndex.
            // The number of breakpoints returned is restricted by penaltyRestriction.
            return FullTextBreakpoint.CreateMultiple(
                this,
                firstCharIndex,
                VerifyMaxLineWidth(maxLineWidth),
                previousLineBreak,
                breakpointRestrictionHandle,
                out bestFitIndex
                );
        }        


        /// <summary>
        /// Releasing LS unmanaged resource on paragraph content
        /// </summary>
        private void Dispose(bool disposing)
        {
            if(_ploparabreak.Value != IntPtr.Zero)
            {
                UnsafeNativeMethods.LoDisposeParaBreakingSession(_ploparabreak.Value, !disposing);

                _ploparabreak.Value = IntPtr.Zero;
                GC.KeepAlive(this);
            }
        }

        /// <summary>
        /// Verify that the input line format width is within the maximum ideal value
        /// </summary>
        private int VerifyMaxLineWidth(double maxLineWidth)
        {
            if (DoubleUtil.IsNaN(maxLineWidth))
                throw new ArgumentOutOfRangeException("maxLineWidth", SR.Get(SRID.ParameterValueCannotBeNaN));                                        
            
            if (maxLineWidth == 0 || double.IsPositiveInfinity(maxLineWidth))
            {
                // consider 0 or positive infinity as maximum ideal width
                return Constants.IdealInfiniteWidth;
            }
            
            if (    maxLineWidth < 0 
                ||  maxLineWidth > Constants.RealInfiniteWidth)
            {
                throw new ArgumentOutOfRangeException("maxLineWidth", SR.Get(SRID.ParameterMustBeBetween, 0, Constants.RealInfiniteWidth));
            }

            // convert real value to ideal value
            return TextFormatterImp.RealToIdeal(maxLineWidth);
}

        /// <summary>
        /// Full text state of the paragraph
        /// </summary>
        internal FullTextState FullText
        {
            get { return _fullText; }
        }

        /// <summary>
        /// Unmanaged LS parabreak session object
        /// </summary>
        internal SecurityCriticalDataForSet<IntPtr> Ploparabreak
        {
            get { return _ploparabreak; }
        }
    }
}

