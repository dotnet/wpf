// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

using System.Collections;
using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for PathFigureCollection
    /// </summary>
    public sealed partial class PathFigureCollection : Animatable, IList, IList<PathFigure>
    {
        /// <summary>
        /// Can serialze "this" to a string.  This returns true iff all of the PathFigures in the collection
        /// return true from their CanSerializeToString methods.
        /// </summary>
        internal bool CanSerializeToString()
        {
            bool canSerializeToString = true;

            for (int i=0; (i<_collection.Count) && canSerializeToString; i++)
            {
                canSerializeToString &= _collection[i].CanSerializeToString();
            }

            return canSerializeToString;
        }
    }
}
