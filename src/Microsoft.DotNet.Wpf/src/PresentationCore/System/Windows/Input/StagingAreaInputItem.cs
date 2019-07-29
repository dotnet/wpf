// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Security;

namespace System.Windows.Input 
{
    /// <summary>
    ///     This class encapsulates an input event while it is being
    ///     processed by the input manager.
    /// </summary>
    /// <remarks>
    ///     This class just provides the dictionary-based storage for
    ///     all of the listeners of the various input manager events.
    /// </remarks>
    public class StagingAreaInputItem
    {
        // Only we can make these.
        internal StagingAreaInputItem(bool isMarker)
        {
            _isMarker = isMarker;
        }
        
        // For performace reasons, we try to reuse these event args.
        // Allow an existing item to be promoted by keeping the existing dictionary. 
        internal void Reset(InputEventArgs input, StagingAreaInputItem promote)
        {
            _input = input;

            if(promote != null && promote._dictionary != null)
            {
                _dictionary = (Hashtable) promote._dictionary.Clone();
            }
            else
            {
                if(_dictionary != null)
                {
                    _dictionary.Clear();
                }
                else
                {
                    _dictionary = new Hashtable();
                }
            }
        }

        /// <summary>
        ///     Returns the input event.
        /// </summary>
        public InputEventArgs Input
        {
            get {return _input;}
        }

        /// <summary>
        ///     Provides storage for arbitrary data needed during the
        ///     processing of this input event.
        /// </summary>
        /// <param name="key">
        ///     An arbitrary key for the data.  This cannot be null.
        /// </param>
        /// <returns>
        ///     The data previously set for this key, or null.
        /// </returns>
        public object GetData(object key)
        {
            return _dictionary[key];
        }

        /// <summary>
        ///     Provides storage for arbitrary data needed during the
        ///     processing of this input event.
        /// </summary>
        /// <param name="key">
        ///     An arbitrary key for the data.  This cannot be null.
        /// </param>
        /// <param name="value">
        ///     The data to set for this key.  This can be null.
        /// </param>
        public void SetData(object key, object value)
        {
            _dictionary[key] = value;
        }

        internal bool IsMarker {get {return _isMarker;}}
        
        private bool _isMarker;
        private InputEventArgs _input;
        private Hashtable _dictionary;
    }
}

