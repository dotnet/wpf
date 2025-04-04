// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// 
    public class TextBoxAutomationPeer : TextAutomationPeer, IValueProvider
    {
        ///
        public TextBoxAutomationPeer(TextBox owner): base(owner)
        {
            _textPattern = new TextAdaptor(this, ((TextBoxBase)owner).TextContainer);
        }
    
        ///
        protected override string GetClassNameCore()
        {
            return "TextBox";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        /// 
        public override object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;

            if(patternInterface == PatternInterface.Value)
                returnValue = this;

            if (patternInterface == PatternInterface.Text)
            {
                if(_textPattern == null)
                    _textPattern = new TextAdaptor(this, ((TextBoxBase)Owner).TextContainer);

                return _textPattern;
            }

            if (patternInterface == PatternInterface.Scroll)
            {
                TextBox owner = (TextBox)Owner;
                if (owner.ScrollViewer != null)
                {
                    returnValue = owner.ScrollViewer.CreateAutomationPeer();
                    ((AutomationPeer)returnValue).EventsSource = this;
                }
            }

            if (patternInterface == PatternInterface.SynchronizedInput)
            {
                returnValue = base.GetPattern(patternInterface);
            }
            return returnValue;
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                TextBox owner = (TextBox)Owner;
                return owner.IsReadOnly;
            }
        }

        string IValueProvider.Value
        {
            get 
            {
                TextBox owner = (TextBox)Owner;
                return owner.Text;
            }
        }

        void IValueProvider.SetValue(string value)
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            TextBox owner = (TextBox)Owner;

            if (owner.IsReadOnly)
            {
                throw new ElementNotEnabledException();
            }
            ArgumentNullException.ThrowIfNull(value);

            owner.Text = value;
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseIsReadOnlyPropertyChangedEvent(bool oldValue, bool newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.IsReadOnlyProperty, oldValue, newValue);
            }
        }

        /// <summary>
        /// Gets collection of AutomationPeers for given text range.
        /// </summary>
        internal override List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end)
        {
            return new List<AutomationPeer>();
        }

        private TextAdaptor _textPattern;        
    }
}

