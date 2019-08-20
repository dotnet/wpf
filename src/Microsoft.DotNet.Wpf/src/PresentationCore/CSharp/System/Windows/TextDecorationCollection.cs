// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: TextDecorationCollection class
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

using System.Windows.Media.Animation;
using System.Windows.Markup;

using MS.Internal.PresentationCore;

namespace System.Windows
{
    /// <summary>
    /// A collection of text decoration instances
    /// </summary>
    [TypeConverter(typeof(TextDecorationCollectionConverter))]
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    public sealed partial class TextDecorationCollection : Animatable, IList
    {
        /// <summary>
        /// Compare this collection with another TextDecorations.  
        /// </summary>
        /// <param name="textDecorations"> the text decoration collection to be compared </param>
        /// <returns> true if two collections of TextDecorations contain equal TextDecoration objects in the
        /// the same order. false otherwise 
        /// </returns>
        /// <remarks>
        /// The method doesn't check "full" equality as it can not take into account of all the possible 
        /// values associated with the DependencyObject,such as Animation, DataBinding and Attached property. 
        /// It only compares the public properties to serve the specific Framework's needs in inline property 
        /// management and Editing serialization. 
        /// </remarks>        
        [FriendAccessAllowed]   // used by Framework
        internal bool ValueEquals(TextDecorationCollection textDecorations)
        {
            if (textDecorations == null) 
                return false;   // o is either null or not TextDecorations object

            if (this == textDecorations) 
                return true;    // Reference equality.

            if ( this.Count != textDecorations.Count)
                return false;   // Two counts are different.

            // To be considered equal, TextDecorations should be same in the exact order.
            // Order matters because they imply the Z-order of the text decorations on screen.
            // Same set of text decorations drawn with different orders may have different result.
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].ValueEquals(textDecorations[i]))
                    return false;
            }            
            return true;                     
        }
     
        /// <summary>
        /// Add a collection of text decorations into the current collection
        /// </summary>
        /// <param name="textDecorations"> The collection to be added </param>
        [CLSCompliant(false)]
        public void Add(IEnumerable<TextDecoration> textDecorations)
        {
            if (textDecorations == null)
            {
                throw new ArgumentNullException("textDecorations");
            }

            foreach(TextDecoration textDecoration in textDecorations)
            {
                Add(textDecoration);
            }                
        }
        
        /// <summary>
        /// Remove a collection of text decorations from the current collection and return 
        /// the resultant (new) collection. The current collection remains unchanged. If the collection
        /// to be removed is not a subset of the current collection, then no element is removed. If 
        /// the source collection has multiple instances of an item, then all intances of the item is
        /// removed. 
        /// </summary>
        /// <param name="textDecorations">The collection to be removed</param>
        /// <param name="result">
        /// Out parameter containing the result. If no element was removed 
        /// from the current collection, then result is a new (unfrozen) collection 
        /// identical to the original one. 
        /// </param>
        /// <returns>True if at least one item was removed from the current collection, False otherwise</returns>
        public bool TryRemove(IEnumerable<TextDecoration> textDecorations, out TextDecorationCollection result)
        {
            if (textDecorations == null)
            {
                throw new ArgumentNullException(nameof(textDecorations));
            }

            bool removed = false;
            result = this.Clone(); //the current collection might be frozen, so clone it

            foreach (TextDecoration textDecoration in textDecorations)
            {
                for (int i = result.Count -1; i >= 0; --i)
                {
                    if (result[i].ValueEquals(textDecoration))
                    {
                        result.RemoveAt(i);
                        removed = true; 
                    }
                }
            }

            return removed; 
        }
    }
}
