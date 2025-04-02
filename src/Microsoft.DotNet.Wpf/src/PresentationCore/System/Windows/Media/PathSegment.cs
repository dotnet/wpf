// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

using System.Windows.Media.Animation;

namespace System.Windows.Media
{
    #region PathSegment
    /// <summary>
    /// PathSegment
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]    
    public abstract partial class PathSegment : Animatable
    {
        #region Constructors
        internal PathSegment()
        {
        }

        #endregion

        #region AddToFigure
        internal abstract void AddToFigure(
            Matrix matrix,          // The transformation matrid
            PathFigure figure,      // The figure to add to
            ref Point current);     // In: Segment start point, Out: Segment endpoint
                                    //     not transformed
        #endregion

        #region Internal
        internal virtual bool IsEmpty()
        {
            return false;
        }

        internal abstract bool IsCurved();

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal abstract string ConvertToString(string format, IFormatProvider provider);

        #endregion

        #region Resource
        /// <summary>
        /// SerializeData - Serialize the contents of this Segment to the provided context.
        /// </summary>
        internal abstract void SerializeData(StreamGeometryContext ctx);
        #endregion

        #region Data
        internal const bool c_isStrokedDefault = true;
        #endregion
    }
    #endregion
}

