// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using MS.Win32;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Imaging
{
    #region ImagingCache

    ///
    /// ImagingCache provides caching for different Imaging objects
    /// Caches are thread-safe.
    ///
    internal static class ImagingCache
    {
        #region Methods

        /// Adds an object to the image cache
        internal static void AddToImageCache(Uri uri, object obj)
        {
            AddToCache(uri, obj, _imageCache);
        }

        /// Removes an object from the image cache
        internal static void RemoveFromImageCache(Uri uri)
        {
            RemoveFromCache(uri, _imageCache);
        }

        /// Get object from the image cache
        internal static object CheckImageCache(Uri uri)
        {
            return CheckCache(uri, _imageCache);
        }

        /// Adds an object to the decoder cache
        internal static void AddToDecoderCache(Uri uri, object obj)
        {
            AddToCache(uri, obj, _decoderCache);
        }

        /// Removes an object from the decoder cache
        internal static void RemoveFromDecoderCache(Uri uri)
        {
            RemoveFromCache(uri, _decoderCache);
        }

        /// Get object from the image cache
        internal static object CheckDecoderCache(Uri uri)
        {
            return CheckCache(uri, _decoderCache);
        }

        /// Adds an object to a given table
        private static void AddToCache(Uri uri, object obj, Hashtable table)
        {
            lock(table)
            {
                // if entry is already there, exit
                if (table.Contains(uri))
                {
                    return;
                }

                // if the table has reached the max size, try to see if we can reduce its size
                if (table.Count == MAX_CACHE_SIZE)
                {
                    ArrayList al = new ArrayList();
                    foreach (DictionaryEntry de in table)
                    {
                        // if the value is a WeakReference that has been GC'd, remove it
                        WeakReference weakRef = de.Value as WeakReference;
                        if ((weakRef != null) && (weakRef.Target == null))
                        {
                            al.Add(de.Key);
                        }
                    }

                    foreach (object o in al)
                    {
                        table.Remove(o);
                    }
                }

                // if table is still maxed out, exit
                if (table.Count == MAX_CACHE_SIZE)
                {
                    return;
                }

                // add it
                table[uri] = obj;
            }
        }

        /// Removes an object from a given table
        private static void RemoveFromCache(Uri uri, Hashtable table)
        {
            lock(table)
            {
                // if entry is there, remove it
                if (table.Contains(uri))
                {
                    table.Remove(uri);
                }
            }
        }

        /// Return an object from a given table
        private static object CheckCache(Uri uri, Hashtable table)
        {
            lock(table)
            {
                return table[uri];
            }
        }

        #endregion

        #region Data Members

        /// image cache
        private static Hashtable _imageCache = new Hashtable();

        /// decoder cache
        private static Hashtable _decoderCache = new Hashtable();

        /// max size to limit the cache
        private static int MAX_CACHE_SIZE = 300;

        #endregion
    }

    #endregion
}

