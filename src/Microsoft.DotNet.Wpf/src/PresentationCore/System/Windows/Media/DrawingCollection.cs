// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: This file contains non-generated DrawingCollection 
//              methods.
//

using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Windows.Markup;

namespace System.Windows.Media
{
    /// <summary>
    /// Collection of Drawing objects
    /// </summary>
    public sealed partial class DrawingCollection : Animatable, IList, IList<Drawing>
    {
        /// <summary>
        /// Appends the entire input DrawingCollection, while only firing a single set of
        /// public events.  If an exception is thrown from the public events, the
        /// Append operation is rolled back.
        /// </summary>
        internal void TransactionalAppend(DrawingCollection collectionToAppend)
        {            
            // Use appendCount to avoid inconsistencies & runaway loops when 
            // this == collectionToAppend, and to ensure collectionToAppend.Count  
            // is only evaluated once.
            int appendCount = collectionToAppend.Count;
            
            // First, append the collection
            for(int i = 0; i < appendCount; i++)
            {
                AddWithoutFiringPublicEvents(collectionToAppend.Internal_GetItem(i));
            }

            // Fire the public Changed event after all the elements have been added.
            //
            // If an exception is thrown, then the Append operation is rolled-back without
            // firing additional events.
            try
            {
                FireChanged();
            }
            catch (Exception)
            {
                // Compute the number of elements that existed before the append
                int beforeAppendCount = Count - appendCount;

                // Remove the appended elements in reverse order without firing Changed events.                
                
                for (   int i = Count - 1;              // Start at the current last index
                        i >= beforeAppendCount;         // Until the previous last index
                        i--                             // Move to the preceding index
                    )
                    {       
                        RemoveAtWithoutFiringPublicEvents(i);
                    }

                // Avoid firing WritePostscript events (e.g., OnChanged) after rolling-back
                // the current operation.
                // 
                // This ensures that only a single set of events is fired for both exceptional &
                // typical cases, and it's likely that firing events would cause another exception.

                throw;
            }            
        }        
    }
}
