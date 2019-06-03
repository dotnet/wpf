// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for SynchronizedInput Pattern

using System;
//using System.Collections;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents objects that support synchronized input events
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class SynchronizedInputPattern: BasePattern
#else
    public class SynchronizedInputPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SynchronizedInputPattern(AutomationElement el, SafePatternHandle hPattern)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>SynchronizedInputPattern pattern</summary>
        public static readonly AutomationPattern Pattern = SynchronizedInputPatternIdentifiers.Pattern;

        /// <summary>
        /// Event ID: InputReachedTarget - indicates input received by the current listening element.
        /// sourceElement  refers to the current listening element.
        /// </summary>
        public static readonly AutomationEvent InputReachedTargetEvent = SynchronizedInputPatternIdentifiers.InputReachedTargetEvent;
        /// <summary>
        /// <summary>
        /// Event ID: InputReachedOtherElement - indicates an input is handled by different element than the one currently listening on.
        /// sourceElement  refers to the current listening element.
        /// </summary>
        public static readonly AutomationEvent InputReachedOtherElementEvent = SynchronizedInputPatternIdentifiers.InputReachedOtherElementEvent;
        /// <summary>
        /// Event ID: InputDiscarded - indicates that input is discarded by the framework.
        /// sourceElement  refers to the current listening element.
        /// </summary>
        public static readonly AutomationEvent InputDiscardedEvent = SynchronizedInputPatternIdentifiers.InputDiscardedEvent;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// The client calls this method to indicate the current element should start listening
        /// for input event of the given type.
        /// </summary>
        
        public void StartListening(SynchronizedInputType inputType)
        {
            UiaCoreApi.SynchronizedInputPattern_StartListening(_hPattern,inputType);
        }
        /// <summary>
        /// If  the element is currently  listening, it will revert back to normal operation
        /// </summary>
        /// 
        
        public void Cancel()
        {
            UiaCoreApi.SynchronizedInputPattern_Cancel(_hPattern);
        }
        
        
        #endregion Public Methods


        

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new SynchronizedInputPattern(el, hPattern);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;

        #endregion Private Fields
    }
}
