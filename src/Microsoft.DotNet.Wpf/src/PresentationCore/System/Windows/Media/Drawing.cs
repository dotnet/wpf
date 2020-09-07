// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Drawing is the base Drawing class that defines the standard
//              interface which Drawing's must implement.  

using MS.Internal;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Resources;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Windows.Threading;

namespace System.Windows.Media 
{        
    /// <summary>
    /// Base class for the enumerable and modifiable Drawing subclasses.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public abstract partial class Drawing : Animatable, IDrawingContent, DUCE.IResource
    {
        #region Constructors
        
        /// <summary>
        /// Constructor for Drawing
        /// </summary>
        /// <remarks>
        /// This constructor is internal to prevent external subclassing.
        /// </remarks>
        internal Drawing()
        {
        }
        
        #endregion Constructors

        #region Public Properties
            
        /// <summary>
        /// Bounds - the axis-aligned bounds of this Drawing.
        /// </summary>
        /// <returns>
        /// Rect - the axis-aligned bounds of this Drawing.
        /// </returns>
        public Rect Bounds
        {            
            get
            {                
                ReadPreamble();

                return GetBounds();
            }
        }        

        #endregion Public Properties
        
        #region Internal Methods       

        /// <summary>
        /// Calls methods on the DrawingContext that are equivalent to the
        /// Drawing with the Drawing's current value.
        /// </summary>
        internal abstract void WalkCurrentValue(DrawingContextWalker ctx);

        #endregion Internal Methods        

        /// <summary>
        /// Returns the bounding box occupied by the content
        /// </summary>
        /// <returns>
        /// Bounding box occupied by the content
        /// </returns>
        Rect IDrawingContent.GetContentBounds(BoundsDrawingContextWalker ctx)
        {     
            Debug.Assert(ctx != null);        
            
            WalkCurrentValue(ctx);
            
            return ctx.Bounds;
        }

        /// <summary>
        /// Forward the current value of the content to the DrawingContextWalker
        /// methods.
        /// </summary>      
        /// <param name="ctx"> DrawingContextWalker to forward content to. </param>        
        void IDrawingContent.WalkContent(DrawingContextWalker ctx)
        {
            ((Drawing)this).WalkCurrentValue(ctx);
        }

        /// <summary>
        /// Determines whether or not a point exists within the content
        /// </summary>    
        /// <param name="point"> Point to hit-test for. </param>                
        /// <returns>
        /// 'true' if the point exists within the content, 'false' otherwise
        /// </returns>                
        bool IDrawingContent.HitTestPoint(Point point)
        {
            return DrawingServices.HitTestPoint(this, point);
        }

        /// <summary>
        /// Hit-tests a geometry against this content
        /// </summary>      
        /// <param name="geometry"> PathGeometry to hit-test for. </param>                
        /// <returns>
        /// IntersectionDetail describing the result of the hit-test
        /// </returns>                     
        IntersectionDetail IDrawingContent.HitTestGeometry(PathGeometry geometry)
        {
            return DrawingServices.HitTestGeometry(this, geometry);           
        }

        /// <summary>
        /// Propagates an event handler to Freezables referenced by 
        /// the content.
        /// </summary>        
        /// <param name="handler"> Event handler to propagate </param>                        
        /// <param name="adding"> 'true' to add the handler, 'false' to remove it </param>                                
        void IDrawingContent.PropagateChangedHandler(EventHandler handler, bool adding)
        {
            if (!IsFrozen) 
            {
                if (adding)
                {
                    ((Drawing)this).Changed += handler;
                }
                else
                {
                    ((Drawing)this).Changed -= handler;
                }
            }
        }            

        /// <summary>
        /// GetBounds to calculate the bounds of this drawing.
        /// </summary>               
        internal Rect GetBounds()
        {
            BoundsDrawingContextWalker ctx = new BoundsDrawingContextWalker();

            WalkCurrentValue(ctx);
        
            return ctx.Bounds;
        }   
}
}
