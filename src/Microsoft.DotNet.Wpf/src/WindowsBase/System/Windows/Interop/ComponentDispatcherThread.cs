// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using MS.Win32;
using System.Globalization;
using System.Security;
using MS.Internal.WindowsBase;

namespace System.Windows.Interop
{
    /// <summary>
    /// This is a class used to implement per-thread instance of the ComponentDispatcher.
    ///</summary>
    internal class ComponentDispatcherThread
    {
        /// <summary>
        /// Returns true if one or more components has gone modal.
        /// Although once one component is modal a 2nd shouldn't.
        ///</summary>
        public bool IsThreadModal
        {
            get
            {
                return (_modalCount > 0);
            }
        }

        /// <summary>
        /// Returns "current" message.   More exactly the last MSG Raised.
        ///</summary>
        public MSG CurrentKeyboardMessage
        {
            get
            {
                return _currentKeyboardMSG;
            }

            set
            {
                _currentKeyboardMSG = value;
            }
        }

        /// <summary>
        /// A component calls this to go modal.  Current thread wide only.
        ///</summary>
        public void PushModal()
        {
            _modalCount += 1;
            if(1 == _modalCount)
            {
                if(null != _enterThreadModal)
                    _enterThreadModal(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// A component calls this to end being modal.
        ///</summary>
        public void PopModal()
        {
            _modalCount -= 1;
            if(0 == _modalCount)
            {
                if(null != _leaveThreadModal)
                    _leaveThreadModal(null, EventArgs.Empty);
            }
            if(_modalCount < 0)
                _modalCount = 0;    // Throwing is also good
        }

        /// <summary>
        /// The message loop pumper calls this when it is time to do idle processing.
        ///</summary>
        public void RaiseIdle()
        {
            if(null != _threadIdle)
                _threadIdle(null, EventArgs.Empty);
        }

        /// <summary>
        /// The message loop pumper calls this for every keyboard message.
        /// </summary>
        public bool RaiseThreadMessage(ref MSG msg)
        {
            bool handled = false;

            if (null != _threadFilterMessage)
                _threadFilterMessage(ref msg, ref handled);

            if (handled)
                return handled;

            if (null != _threadPreprocessMessage)
                _threadPreprocessMessage(ref msg, ref handled);

            return handled;
        }

        /// <summary>
        /// Components register delegates with this event to handle
        /// thread idle processing.
        ///</summary>
        public event EventHandler ThreadIdle
        {
            add
            {
                _threadIdle += value;
            }
            remove
            {
                _threadIdle -= value;
            }
        }
        private event EventHandler _threadIdle;


        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (first chance processing).
        ///</summary>
        public event ThreadMessageEventHandler ThreadFilterMessage
        {
            add
            {
                _threadFilterMessage += value;
            }
            remove
            {
                _threadFilterMessage -= value;
            }
        }
        private event ThreadMessageEventHandler _threadFilterMessage;

        /// <summary>
        /// Components register delegates with this event to handle
        /// Keyboard Messages (second chance processing).
        ///</summary>
        public event ThreadMessageEventHandler ThreadPreprocessMessage
        {
            add
            {
                _threadPreprocessMessage += value;
            }
            remove
            {
                _threadPreprocessMessage -= value;
            }
        }

        /// <summary>
        ///     Adds the specified handler to the front of the invocation list
        ///     of the PreprocessMessage event.
        /// <summary>
        public void AddThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            _threadPreprocessMessage = (ThreadMessageEventHandler)Delegate.Combine(handler, _threadPreprocessMessage);
        }

        /// <summary>
        ///     Removes the first occurance of the specified handler from the
        ///     invocation list of the PreprocessMessage event.
        /// <summary>
        public void RemoveThreadPreprocessMessageHandlerFirst(ThreadMessageEventHandler handler)
        {
            if (_threadPreprocessMessage != null)
            {
                ThreadMessageEventHandler newHandler = null;

                foreach (ThreadMessageEventHandler testHandler in _threadPreprocessMessage.GetInvocationList())
                {
                    if (testHandler == handler)
                    {
                        // This is the handler to remove.  We should not check
                        // for any more occurances.
                        handler = null;
                    }
                    else
                    {
                        newHandler += testHandler;
                    }
                }

                _threadPreprocessMessage = newHandler;
            }
        }

        private event ThreadMessageEventHandler _threadPreprocessMessage;

        /// <summary>
        /// Components register delegates with this event to handle
        /// a component on this thread has "gone modal", when previously none were.
        ///</summary>
        public event EventHandler EnterThreadModal
        {
            add
            {
                _enterThreadModal += value;
            }
            remove
            {
                _enterThreadModal -= value;
            }
        }
        private event EventHandler _enterThreadModal;

        /// <summary>
        /// Components register delegates with this event to handle
        /// all components on this thread are done being modal.
        ///</summary>
        public event EventHandler LeaveThreadModal
        {
            add
            {
                _leaveThreadModal += value;
            }
            remove
            {
                _leaveThreadModal -= value;
            }
        }
        private event EventHandler _leaveThreadModal;

        private int _modalCount;
                
        private MSG _currentKeyboardMSG;
    }
}
