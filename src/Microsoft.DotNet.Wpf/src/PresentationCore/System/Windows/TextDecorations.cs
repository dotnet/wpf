// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: TextDecorations class
//

namespace System.Windows 
{
    /// <summary>
    /// TextDecorations class contains a set of commonly used text decorations such as underline, 
    /// strikethrough, baseline and over-line.
    /// </summary>

    public static class TextDecorations
    {     
        static TextDecorations()
        {
            // Init Underline            
            TextDecoration td = new TextDecoration();
            td.Location       = TextDecorationLocation.Underline;
            underline         = new TextDecorationCollection();
            underline.Add(td);
            underline.Freeze();

            // Init strikethrough
            td = new TextDecoration();
            td.Location       = TextDecorationLocation.Strikethrough;
            strikethrough     = new TextDecorationCollection();
            strikethrough.Add(td);
            strikethrough.Freeze();
            
            // Init overline
            td = new TextDecoration();
            td.Location       = TextDecorationLocation.OverLine;
            overLine          = new TextDecorationCollection();
            overLine.Add(td);
            overLine.Freeze();

            // Init baseline
            td = new TextDecoration();
            td.Location       = TextDecorationLocation.Baseline;
            baseline          = new TextDecorationCollection();
            baseline.Add(td);
            baseline.Freeze();            
        }
        
        //---------------------------------
        // Public properties
        //---------------------------------
      
        /// <summary>
        /// returns a frozen collection containing an underline
        /// </summary>
        public static TextDecorationCollection Underline
        {
            get 
            {
                return underline;
            }
        }
        

        /// <summary>
        /// returns a frozen collection containing a strikethrough
        /// </summary>
        public static TextDecorationCollection Strikethrough
        {
            get
            {
                return strikethrough;
            }
        }

        /// <summary>
        /// returns a frozen collection containing an overline
        /// </summary>
        public static TextDecorationCollection OverLine
        {
            get
            {
                return overLine;
            }
        }
        
        /// <summary>
        /// returns a frozen collection containing a baseline
        /// </summary>
        public static TextDecorationCollection Baseline
        {
            get
            {
                return baseline;
            }
        }

        //--------------------------------
        // Private members
        //--------------------------------

        private static readonly TextDecorationCollection underline;
        private static readonly TextDecorationCollection strikethrough;
        private static readonly TextDecorationCollection overLine;
        private static readonly TextDecorationCollection baseline;
    }
}
