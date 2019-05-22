// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal.Documents;
using MS.Internal;
using MS.Internal.Automation;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    /// 
    public class RichTextBoxAutomationPeer : TextAutomationPeer
    {
        ///
        public RichTextBoxAutomationPeer(RichTextBox owner): base(owner)
        {
            _textPattern = new TextAdaptor(this, owner.TextContainer);
        }
    
        ///
        override protected string GetClassNameCore()
        {
            return "RichTextBox";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Document;
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;
            RichTextBox owner = (RichTextBox)Owner;

            if (patternInterface == PatternInterface.Text)
            {
                if (_textPattern == null)
                    _textPattern = new TextAdaptor(this, owner.TextContainer);

                return _textPattern;
            }

            else
            {
                if (patternInterface == PatternInterface.Scroll)
                {
                    if (owner.ScrollViewer != null)
                    {
                        returnValue = owner.ScrollViewer.CreateAutomationPeer();
                        ((AutomationPeer)returnValue).EventsSource = this;
                    }
                }
                else
                {
                    returnValue = base.GetPattern(patternInterface);
                }
            }
 
            return returnValue;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            RichTextBox owner = (RichTextBox)Owner;
            return TextContainerHelper.GetAutomationPeersFromRange(owner.TextContainer.Start, owner.TextContainer.End, null);
        }

        /// <summary>
        /// Gets collection of AutomationPeers for given text range.
        /// </summary>
        internal override List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end)
        {
            // Force children connection to automation tree.
            GetChildren();

            RichTextBox owner = (RichTextBox)Owner;
            return TextContainerHelper.GetAutomationPeersFromRange(start, end, owner.TextContainer.Start);
        }

        private TextAdaptor _textPattern;        
    }
}

