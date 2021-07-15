// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Navigation
{
    ///<summary>
    ///     FragmentNavigationEventArgs exposes the fragment being navigated to
    ///     in an event fired from NavigationService to notify a listening client
    ///     that a navigation to fragment is about to occur.  This is used for table
    ///     of contents navigations in fixed format documents.
    ///</summary> 
    public class FragmentNavigationEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FragmentNavigationEventArgs(string fragment, object Navigator)
        {
            _fragment = fragment;
            _navigator = Navigator;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
                
        #region Public Properties 

        /// <summary>
        ///  The fragment part of the URI that was passed to the Navigate() API which initiated this navigation.
        ///  The fragment may be String.Empty to indicate a scroll to the top of the page.
        /// </summary>
        public string Fragment
        {
            get
            {
                return _fragment;
            }
        }

        /// <summary>
        /// If this flag is not set by the called method then NavigationService will attempt to
        /// find an element with name equal to the fragment and bring it into view.
        /// </summary>
        public bool Handled
        {
            get
            {
                return _handled;
            }
            set
            {
                _handled = value;
            }
        }

        /// <summary>
        /// The navigator that raised this event
        /// </summary>
        public object Navigator
        {
            get
            {
                return _navigator;
            }
        }
        #endregion Public Properties 

        //------------------------------------------------------    
        //    
        //  Private Fields    
        //    
        //------------------------------------------------------
        
        #region Private Fields

        private string _fragment;
        private bool _handled;
        object _navigator;

        #endregion Private Fields
    }
}
