// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class ContentElementFocusAction : SimpleDiscoverableAction
    {
        #region Public Members

        public ContentElement Element { get; set; }

        public bool IsFocus { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsFocus)
            {
                Element.Focus();
            }
            else
            {
                Keyboard.Focus(null);//Release focus everywhere.
            }
        }

        #endregion
    }
}
