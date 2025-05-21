// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines IHighlightRange interface, used for communication
// between HighlightComponent and AnnotationHighlightLayer
//

using System.Windows.Annotations;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// represents the data for one highlight range -
    /// foreground Color, bachground Color,
    /// selected foreground Color, selected background Color
    /// last modified time
    /// </summary>
    internal interface IHighlightRange
    {
        #region Internal Methods

        /// <summary>
        /// Adds a new shape to the children of this range
        /// </summary>
        /// <param name="child">the new child</param>
        void AddChild(Shape child);


        /// <summary>
        /// Removes a shape from the children of this range
        /// </summary>
        /// <param name="child">the new child</param>
        void RemoveChild(Shape child);


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        #region internal Properties

        /// <summary>
        /// Highlight color
        /// </summary>
        Color Background
        {
            get;
        }

        /// <summary>
        /// Highlight color when selected
        /// </summary>
        Color SelectedBackground
        {
            get;
        }

        /// <summary>
        /// TextSegments of this highlight
        /// </summary>
        TextAnchor Range
        {
            get;
        }

        /// <summary>
        /// Priority of this highlight
        /// </summary>
        int Priority
        {
            get;
        }

        /// <summary>
        /// If true - highlight only the content of tables, figures and floaters
        /// </summary>
        bool HighlightContent
        {
            get;
        }

        #endregion Internal Properties
    }
}
