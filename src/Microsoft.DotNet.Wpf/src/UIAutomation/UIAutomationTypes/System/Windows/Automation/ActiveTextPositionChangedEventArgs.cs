// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: ActiveTextPositionChangedEventArgs event args class

using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace System.Windows.Automation
{
    /// <summary>
    /// ActiveTextPositionChangedEventArgs event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class ActiveTextPositionChangedEventArgs  : AutomationEventArgs
#else
    public sealed class ActiveTextPositionChangedEventArgs : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor for ActiveTextPositionChanged event args.
        /// </summary>
        /// <param name="textRangeProvider">Specifies the text range where the change occurred, if applicable.</param>
        public ActiveTextPositionChangedEventArgs(ITextRangeProvider textRange)
            : base(AutomationElementIdentifiers.ActiveTextPositionChangedEvent)
        {
            TextRange = textRange;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns the text range where the change occurred, if applicable.
        /// </summary>
        public ITextRangeProvider TextRange { get; private set; }

        #endregion Public Properties
    }
}
