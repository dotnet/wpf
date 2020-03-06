// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Automation.Peers;

namespace System.Windows.Forms.Integration
{
    internal class ElementHostAutomationPeer : FrameworkElementAutomationPeer
    {
        private AvalonAdapter _owner;

        public ElementHostAutomationPeer(AvalonAdapter owner)
            : base(owner)
        {
            _owner = owner;
        }

        protected override bool IsContentElementCore()
        {
            return false;
        }

        protected override bool IsControlElementCore()
        {
            return false;
        }

        protected override string GetNameCore()
        {
            return _owner.Name;
        }
    }
}



