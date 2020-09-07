// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: IDrawingContent defines a common interface for representations
//              of content populated by a DrawingContext.  Currently 
//              Drawing content can be represented by either RenderData
//              or DrawingGroups.
//

using System.Windows.Media;
using System.Windows.Media.Composition;

namespace System.Windows.Media 
{
    /// <summary>
    /// IDrawingContent defines a common interface for representations
    /// of content populated by a DrawingContext.  Currently 
    /// Drawing content can be represented by either RenderData
    /// or DrawingGroup graphs.
    /// </summary>    
    internal interface IDrawingContent : DUCE.IResource
    {
        /// <summary>
        /// Returns the bounding box occupied by the content
        /// </summary>
        /// <returns>
        /// Bounding box occupied by the content
        /// </returns>
        Rect GetContentBounds(BoundsDrawingContextWalker ctx);

        /// <summary>
        /// Forward the current value of the content to the DrawingContextWalker
        /// methods.
        /// </summary>      
        /// <param name="walker"> DrawingContextWalker to forward content to. </param>        
        void WalkContent(DrawingContextWalker walker);

        /// <summary>
        /// Determines whether or not a point exists within the content
        /// </summary>    
        /// <param name="point"> Point to hit-test for. </param>                
        /// <returns>
        /// 'true' if the point exists within the content, 'false' otherwise
        /// </returns>        
        bool HitTestPoint(Point point);

        /// <summary>
        /// Hit-tests a geometry against this content
        /// </summary>      
        /// <param name="geometry"> PathGeometry to hit-test for. </param>                
        /// <returns>
        /// IntersectionDetail describing the result of the hit-test
        /// </returns>             
        IntersectionDetail HitTestGeometry(PathGeometry geometry);       

        /// <summary>
        /// Propagates an event handler to Freezables referenced by 
        /// the content.
        /// </summary>        
        /// <param name="handler"> Event handler to propagate </param>                        
        /// <param name="adding"> 'true' to add the handler, 'false' to remove it </param>                                
        void PropagateChangedHandler(EventHandler handler, bool adding);           
}
}

