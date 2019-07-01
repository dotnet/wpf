// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Interop;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs changing the compositionMode to Full or None or OutputOnly on a webbrowser
    /// </summary>
    public class ChangeCompositionModeAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public HwndHost Target { get; set; }
        public CompositionMode CompositionMode{ get; set; }
 
        public override void Perform()
        {	
            Target.CompositionMode= CompositionMode;
        }   

        #endregion Public Members    
    }
}
