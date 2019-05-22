// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: TextDecoration class
//
//

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    /// A text decoration 
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    public sealed partial class TextDecoration : Animatable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TextDecoration()
        {
        }      

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="location">The location of the text decoration</param>
        /// <param name="pen">The pen used to draw this text decoration</param>
        /// <param name="penOffset">The offset of this text decoration to the location</param>
        /// <param name="penOffsetUnit">The unit of the offset</param>
        /// <param name="penThicknessUnit">The unit of the thickness of the pen</param>
        public TextDecoration(
            TextDecorationLocation location,
            Pen                    pen,
            double                 penOffset,
            TextDecorationUnit     penOffsetUnit,
            TextDecorationUnit     penThicknessUnit
            )
        {
            Location         = location;
            Pen              = pen;
            PenOffset        = penOffset;
            PenOffsetUnit    = penOffsetUnit;
            PenThicknessUnit = penThicknessUnit;        
        }      


        /// <summary>
        /// Compare the values of thhe properties in the two TextDecoration objects
        /// </summary>
        /// <param name="textDecoration">The TextDecoration object to be compared against</param>
        /// <returns>True if their property values are equal. False otherwise</returns>
        /// <remarks>
        /// The method doesn't check "full" equality as it can not take into account of all the possible 
        /// values associated with the DependencyObject,such as Animation, DataBinding and Attached property. 
        /// It only compares the public properties to serve the specific Framework's needs in inline property 
        /// management and Editing serialization. 
        /// </remarks>        
        internal bool ValueEquals(TextDecoration textDecoration)
        {
            if (textDecoration == null)
                return false; // o is either null or not a TextDecoration object.

            if (this == textDecoration) 
                return true; // reference equality.
               
            return (    
               Location         == textDecoration.Location 
            && PenOffset        == textDecoration.PenOffset 
            && PenOffsetUnit    == textDecoration.PenOffsetUnit 
            && PenThicknessUnit == textDecoration.PenThicknessUnit
            && (Pen == null ? textDecoration.Pen == null : Pen.Equals( textDecoration.Pen)) 
            );
        }
    }
}


