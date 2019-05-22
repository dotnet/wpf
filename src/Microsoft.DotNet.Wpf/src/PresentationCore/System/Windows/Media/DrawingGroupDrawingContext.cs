// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: DrawingGroupDrawingContext populates a DrawingGroup 
//              from Draw commands that are called on it.
//
using System.Diagnostics;

namespace System.Windows.Media
{
    internal class DrawingGroupDrawingContext : DrawingDrawingContext
    {
        /// <summary>
        /// DrawingGroupDrawingContext populates a DrawingGroup from the Draw
        /// commands that are called on it.
        /// </summary>
        /// <param name="drawingGroup"> DrawingGroup this context populates </param>
        internal DrawingGroupDrawingContext(DrawingGroup drawingGroup)
        {
            Debug.Assert(null != drawingGroup);

            _drawingGroup = drawingGroup;
        }

        /// <summary>
        /// Called by the base class during Close/Dispose when the content created by 
        /// the DrawingDrawingContext needs to be committed.
        /// </summary>
        /// <param name="rootDrawingGroupChildren"> 
        ///     Collection containing the Drawing elements created with this
        ///     DrawingContext.
        /// </param>
        /// <remarks>
        ///     This will only be called once (at most) per instance.
        /// </remarks>
        protected override void CloseCore(DrawingCollection rootDrawingGroupChildren)
        {
            Debug.Assert(null != _drawingGroup);
                
            _drawingGroup.Close(rootDrawingGroupChildren);
        }        

        private DrawingGroup _drawingGroup;
    }
}

