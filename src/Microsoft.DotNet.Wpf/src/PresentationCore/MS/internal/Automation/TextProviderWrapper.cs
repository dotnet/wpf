// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Text pattern provider wrapper for WCP

using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // see comment on InvokeProviderWrapper class for explanation of purpose and organization of these wrapper classes.
    internal class TextProviderWrapper : MarshalByRefObject, ITextProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TextProviderWrapper( AutomationPeer peer, ITextProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITextProvider
        //
        //------------------------------------------------------
 
        #region Interface ITextProvider

        public ITextRangeProvider[] GetSelection()
        {
            return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetSelection(), peer), _iface, _peer);
        }

        public ITextRangeProvider[] GetVisibleRanges()
        {
            return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetVisibleRanges(), peer), _iface, _peer);
        }

        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
        {
            if (childElement is not ElementProxy)
                throw new ArgumentException(SR.Format(SR.TextProvider_InvalidChild, "childElement"));

            // The actual invocation method that gets called on the peer's context.
            static ITextRangeProvider RangeFromChild(TextProviderWrapper state, IRawElementProviderSimple childElement)
            {
                return TextRangeProviderWrapper.WrapArgument(state._iface.RangeFromChild(childElement), state._peer);
            }

            return ElementUtil.Invoke(_peer, RangeFromChild, this, childElement);
        }

        public ITextRangeProvider RangeFromPoint(Point screenLocation)
        {
            // The actual invocation method that gets called on the peer's context.
            static ITextRangeProvider RangeFromPoint(TextProviderWrapper state, Point screenLocation)
            {
                return TextRangeProviderWrapper.WrapArgument(state._iface.RangeFromPoint(screenLocation), state._peer);
            }

            return ElementUtil.Invoke(_peer, RangeFromPoint, this, screenLocation);
        }

        public ITextRangeProvider DocumentRange
        {
            get => ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.DocumentRange, peer), _iface, _peer);
        }

        public SupportedTextSelection SupportedTextSelection
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.SupportedTextSelection, _iface);
        }

        #endregion Interface ITextProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new TextProviderWrapper( peer, (ITextProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITextProvider _iface;

        #endregion Private Fields
    }
}

