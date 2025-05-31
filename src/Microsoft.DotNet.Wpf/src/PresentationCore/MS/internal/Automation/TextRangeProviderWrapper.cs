// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: TextRange provider wrapper for WCP

using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Automation.Peers;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace MS.Internal.Automation
{
    // see comment on InvokeProviderWrapper class for explanation of purpose and organization of these wrapper classes.
    internal sealed class TextRangeProviderWrapper : MarshalByRefObject, ITextRangeProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TextRangeProviderWrapper(AutomationPeer peer, ITextRangeProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITextRangeProvider
        //
        //------------------------------------------------------
 
        #region Interface ITextRangeProvider

        public ITextRangeProvider Clone()
        {
            return ElementUtil.Invoke(_peer, static (state, peer) => WrapArgument(state.Clone(), peer), _iface, _peer);
        }

        public bool Compare(ITextRangeProvider range)
        {
            if (range is not TextRangeProviderWrapper)
                throw new ArgumentException(SR.Format(SR.TextRangeProvider_InvalidRangeProvider, nameof(range)));

            // Note: We always need to unwrap the range argument here.
            return ElementUtil.Invoke(_peer, static (state, range) => state.Compare(UnwrapArgument(range)), _iface, range);
        }

        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (targetRange is not TextRangeProviderWrapper)
                throw new ArgumentException(SR.Format(SR.TextRangeProvider_InvalidRangeProvider, nameof(targetRange)));

            object[] args = [endpoint, targetRange, targetEndpoint];

            // The actual invocation method that gets called on the peer's context.
            static int CompareEndpoints(ITextRangeProvider state, object[] args)
            {
                TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
                ITextRangeProvider targetRange = (ITextRangeProvider)args[1];
                TextPatternRangeEndpoint targetEndpoint = (TextPatternRangeEndpoint)args[2];

                return state.CompareEndpoints(endpoint, UnwrapArgument(targetRange), targetEndpoint);
            }

            return ElementUtil.Invoke(_peer, CompareEndpoints, _iface, args);
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            ElementUtil.Invoke(_peer, static (state, unit) => state.ExpandToEnclosingUnit(unit), _iface, unit);
        }

        public ITextRangeProvider? FindAttribute(int attribute, object val, bool backward)
        {
            object[] args = [attribute, val, backward];

            // The actual invocation method that gets called on the peer's context.
            static ITextRangeProvider? FindAttribute(TextRangeProviderWrapper state, object[] args)
            {
                int attribute = (int)args[0];
                object val = args[1];
                bool backward = (bool)args[2];

                return WrapArgument(state._iface.FindAttribute(attribute, val, backward), state._peer);
            }

            return ElementUtil.Invoke(_peer, FindAttribute, this, args);
        }

        public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
        {
            object[] args = [text, backward, ignoreCase];

            // The actual invocation method that gets called on the peer's context.
            static ITextRangeProvider? FindText(TextRangeProviderWrapper state, object[] args)
            {
                string text = (string)args[0];
                bool backward = (bool)args[1];
                bool ignoreCase = (bool)args[2];

                return WrapArgument(state._iface.FindText(text, backward, ignoreCase), state._peer);
            }

            return ElementUtil.Invoke(_peer, FindText, this, args);
        }

        public object GetAttributeValue(int attribute)
        {
            // Note: If an attribute value is ever a range then we'll need to wrap/unwrap it appropriately here.
            return ElementUtil.Invoke(_peer, static (state, attribute) => state.GetAttributeValue(attribute), _iface, attribute);
        }

        public double[] GetBoundingRectangles()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetBoundingRectangles(), _iface);
        }

        public IRawElementProviderSimple GetEnclosingElement()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetEnclosingElement(), _iface);
        }

        public string GetText(int maxLength)
        {
            return ElementUtil.Invoke(_peer, static (state, maxLength) => state.GetText(maxLength), _iface, maxLength);
        }

        public int Move(TextUnit unit, int count)
        {
            object[] args = [unit, count];

            // The actual invocation method that gets called on the peer's context.
            static int Move(ITextRangeProvider state, object[] args)
            {
                TextUnit unit = (TextUnit)args[0];
                int count = (int)args[1];

                return state.Move(unit, count);
            }

            return ElementUtil.Invoke(_peer, Move, _iface, args);
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            object[] args = [endpoint, unit, count];

            // The actual invocation method that gets called on the peer's context.
            static int MoveEndpointByUnit(ITextRangeProvider state, object[] args)
            {
                TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
                TextUnit unit = (TextUnit)args[1];
                int count = (int)args[2];

                return state.MoveEndpointByUnit(endpoint, unit, count);
            }

            return ElementUtil.Invoke(_peer, MoveEndpointByUnit, _iface, args);
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (targetRange is not TextRangeProviderWrapper)
                throw new ArgumentException(SR.Format(SR.TextRangeProvider_InvalidRangeProvider, nameof(targetRange)));

            object[] args = [endpoint, targetRange, targetEndpoint];

            // The actual invocation method that gets called on the peer's context.
            static void MoveEndpointByRange(ITextRangeProvider state, object[] args)
            {
                TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
                ITextRangeProvider targetRange = (ITextRangeProvider)args[1];
                TextPatternRangeEndpoint targetEndpoint = (TextPatternRangeEndpoint)args[2];

                state.MoveEndpointByRange(endpoint, UnwrapArgument(targetRange), targetEndpoint);
            }

            ElementUtil.Invoke(_peer, MoveEndpointByRange, _iface, args);
        }

        public void Select()
        {
            ElementUtil.Invoke(_peer, static (state) => state.Select(), _iface);
        }

        public void AddToSelection()
        {
            ElementUtil.Invoke(_peer, static (state) => state.AddToSelection(), _iface);
        }

        public void RemoveFromSelection()
        {
            ElementUtil.Invoke(_peer, static (state) => state.RemoveFromSelection(), _iface);
        }

        public void ScrollIntoView(bool alignToTop)
        {
            ElementUtil.Invoke(_peer, static (state, alignToTop) => state.ScrollIntoView(alignToTop), _iface, alignToTop);
        }

        public IRawElementProviderSimple[]? GetChildren()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetChildren(), _iface);
        }


        #endregion Interface ITextRangeProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // Wrap arguments that are being returned, assuming they're not null or already wrapped.
        [return: NotNullIfNotNull(nameof(argument))]
        internal static ITextRangeProvider? WrapArgument(ITextRangeProvider? argument, AutomationPeer peer)
        {
            if (argument == null)
                return null;

            if (argument is TextRangeProviderWrapper)
                return argument;

            return new TextRangeProviderWrapper(peer, argument);
        }

        [return: NotNullIfNotNull(nameof(argument))]
        internal static ITextRangeProvider[]? WrapArgument(ITextRangeProvider[]? argument, AutomationPeer peer)
        {
            if (argument == null)
                return null;

            if (argument is TextRangeProviderWrapper[])
                return argument;

            ITextRangeProvider[] outArray = new ITextRangeProvider[argument.Length];
            for (int i = 0; i < argument.Length; i++)
            {
                outArray[i] = WrapArgument(argument[i], peer);
            }
            return outArray;
        }

        // Remove the wrapper from the argument if a wrapper exists
        internal static ITextRangeProvider UnwrapArgument(ITextRangeProvider argument)
        {
            return argument is TextRangeProviderWrapper wrapper ? wrapper._iface : argument;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private readonly AutomationPeer _peer;
        private readonly ITextRangeProvider _iface;

        #endregion Private Fields
    }
}

