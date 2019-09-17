// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Storage for the "TraceLevel" attached property.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MS.Internal
{
    /// <summary>
    /// This class stores values for the attached property
    /// PresentationTraceSources.TraceLevel.
    /// </summary>
    internal static class TraceLevelStore
    {
        #region Constructors

        //
        //  Constructors
        //

        #endregion Constructors

        #region Internal Methods

        //
        //  Internal Methods
        //

        /// <summary>
        /// Reads the attached property TraceLevel from the given element.
        /// </summary>
        internal static PresentationTraceLevel GetTraceLevel(object element)
        {
            PresentationTraceLevel result;

            if (element == null || _dictionary.Count == 0)
            {
                result = PresentationTraceLevel.None;
            }
            else
            {
                lock (_dictionary)
                {
                    Key key = new Key(element);
                    if (!_dictionary.TryGetValue(key, out result))
                    {
                        result = PresentationTraceLevel.None;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Writes the attached property TraceLevel to the given element.
        /// </summary>
        internal static void SetTraceLevel(object element, PresentationTraceLevel traceLevel)
        {
            if (element == null)
                return;

            lock (_dictionary)
            {
                Key key = new Key(element, true);

                if (traceLevel > PresentationTraceLevel.None)
                {
                    _dictionary[key] = traceLevel;
                }
                else
                {
                    _dictionary.Remove(key);
                }
            }
        }

        #endregion Internal Methods

        #region Private Fields

        //
        //  Private Fields
        //

        private static Dictionary<Key,PresentationTraceLevel> _dictionary = new Dictionary<Key,PresentationTraceLevel>();

        #endregion Private Fields

        #region Table Keys

        // the key for the dictionary:  <((element)), hashcode>
        private struct Key
        {
            internal Key(object element, bool useWeakRef)
            {
                _element = new WeakReference(element);
                _hashcode = element.GetHashCode();
            }

            internal Key(object element)
            {
                _element = element;
                _hashcode = element.GetHashCode();
            }

            public override int GetHashCode()
            {
#if DEBUG
                WeakReference wr = _element as WeakReference;
                object element = (wr != null) ? wr.Target : _element;
                if (element != null)
                {
                    int hashcode = element.GetHashCode();
                    Debug.Assert(hashcode == _hashcode, "hashcodes disagree");
                }
#endif

                return _hashcode;
            }

            public override bool Equals(object o)
            {
                if (o is Key)
                {
                    WeakReference wr;
                    Key that = (Key)o;

                    if (this._hashcode != that._hashcode)
                        return false;

                    wr = this._element as WeakReference;
                    object s1 = (wr != null) ? wr.Target : this._element;
                    wr = that._element as WeakReference;
                    object s2 = (wr != null) ? wr.Target : that._element;

                    if (s1!=null && s2!=null)
                        return (s1 == s2);
                    else
                        return (this._element == that._element);
                }
                else
                {
                    return false;
                }
            }

            public static bool operator==(Key key1, Key key2)
            {
                return key1.Equals(key2);
            }

            public static bool operator!=(Key key1, Key key2)
            {
                return !key1.Equals(key2);
            }

            object _element;            // lookup: direct ref.  In table: WeakRef
            int _hashcode;              // cached, in case source is GC'd
        }

        #endregion Table Keys
    }
}
