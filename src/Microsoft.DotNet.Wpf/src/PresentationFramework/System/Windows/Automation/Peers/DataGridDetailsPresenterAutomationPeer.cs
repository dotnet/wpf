// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MS.Internal;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DataGridDetailsPresenter
    /// </summary>
    public sealed class DataGridDetailsPresenterAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridDetailsPresenter
        /// </summary>
        /// <param name="owner">DataGridDetailsPresenter</param>
        public DataGridDetailsPresenterAutomationPeer(DataGridDetailsPresenter owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer Overrides

        ///
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        #endregion
    }
}
