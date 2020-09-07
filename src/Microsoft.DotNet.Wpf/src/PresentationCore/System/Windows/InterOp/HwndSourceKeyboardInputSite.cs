// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;
using System.Collections;
using MS.Win32;
using System.Windows.Media;
using System.Windows.Threading;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Security ; 
using MS.Internal.PresentationCore; 

namespace System.Windows.Interop
{
    internal class HwndSourceKeyboardInputSite : IKeyboardInputSite
    {
        public HwndSourceKeyboardInputSite(HwndSource source, IKeyboardInputSink sink)
        {
            if(source == null)
            {
                throw new ArgumentNullException("source");
            }
            if(sink == null)
            {
                throw new ArgumentNullException("sink");
            }
            if(!(sink is UIElement))
            {
                throw new ArgumentException(SR.Get(SRID.KeyboardSinkMustBeAnElement), "sink");
            }
            
            _source = source;

            _sink = sink;
            _sink.KeyboardInputSite = this;

            _sinkElement = sink as UIElement;
        }
        
#region IKeyboardInputSite
        /// <summary>
        ///     Unregisters a child KeyboardInputSink from this sink.
        /// </summary>
        /// <remarks> 
        ///     Requires unmanaged code permission. 
        /// </remarks> 
        void IKeyboardInputSite.Unregister()
        {
            CriticalUnregister(); 
        }

        /// <summary>
        ///     Unregisters a child KeyboardInputSink from this sink.
        /// </summary>
        internal void CriticalUnregister()
        {
            if(_source != null && _sink != null)
            {
                _source.CriticalUnregisterKeyboardInputSink(this);
                _sink.KeyboardInputSite = null;
            }

            _source = null;
            _sink = null;
        }           
        /// <summary>
        ///     Returns the sink associated with this site (the "child", not
        ///     the "parent" sink that owns the site).  There's no way of
        ///     getting from the site to the parent sink.
        /// </summary> 
        IKeyboardInputSink IKeyboardInputSite.Sink
        {
            get
            {
                return _sink;
            }
        }

        /// <summary>
        ///     Components call this when they want to move focus ("tab") but
        ///     have nowhere further to tab within their own component.  Return
        ///     value is true if the site moved focus, false if the calling
        ///     component still has focus and should wrap around.
        /// </summary> 
        bool IKeyboardInputSite.OnNoMoreTabStops(TraversalRequest request)
        {
            bool traversed = false;

            if(_sinkElement != null)
            {
                traversed = _sinkElement.MoveFocus(request);
            }

            return traversed;
        }

#endregion IKeyboardInputSite
        
        private HwndSource _source;
        private IKeyboardInputSink _sink;
        private UIElement _sinkElement;
    }
}
