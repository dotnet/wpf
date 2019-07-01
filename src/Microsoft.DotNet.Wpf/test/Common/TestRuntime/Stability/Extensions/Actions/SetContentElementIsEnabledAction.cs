// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class SetContentElementIsEnabledAction : SimpleDiscoverableAction
    {
        #region Public Members

        public ContentElement Element { get; set; }

        public bool IsEnabled { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Element.IsEnabled = IsEnabled;
        }

        #endregion
    }
}
