// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: DrawingGroup represents a collection of Drawing objects, and
//              can apply group-operations such as clip and opacity to it's
//              collections
//

using System;
using System.Windows.Threading;

using MS.Win32;
using System.Security;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
using MS.Internal.Media;
using System.Resources;
using MS.Utility;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;
using System.ComponentModel.Design.Serialization;
using System.ComponentModel;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{    
    /// <summary>
    /// DrawingGroup represents a collection of Drawing objects, and
    /// can apply group-operations such as clip and opacity to it's
    /// collections.
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class DrawingGroup : Drawing
    {
        #region Constructors

        /// <summary>
        /// Default DrawingGroup constructor.  
        /// Constructs an object with all properties set to their default values.
        /// </summary>        
        public DrawingGroup()
        {
        } 

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Opens the DrawingGroup for re-populating it's children, clearing any existing 
        /// children.
        /// </summary>  
        /// <returns>
        /// Returns DrawingContext to populate the DrawingGroup's children.        
        /// </returns>
        public DrawingContext Open()
        {
            VerifyOpen();
            
            _openedForAppend = false;
            
            return new DrawingGroupDrawingContext(this);            
        }

        /// <summary>
        /// Opens the DrawingGroup for populating it's children, appending to
        /// any existing children in the collection.
        /// </summary>  
        /// <returns>
        /// Returns DrawingContext to populate the DrawingGroup's children.        
        /// </returns>
        public DrawingContext Append()
        {
            VerifyOpen();

            _openedForAppend = true;

            return new DrawingGroupDrawingContext(this);
        }

        #endregion Public methods        

        #region Internal methods

        /// <summary>
        /// Called by a DrawingContext returned from Open or Append when the content
        /// created by it needs to be committed (because DrawingContext.Close/Dispose
        /// was called)
        /// </summary>
        /// <param name="rootDrawingGroupChildren"> 
        ///     Collection containing the Drawing elements created by a DrawingContext
        ///     returned from Open or Append.
        /// </param>
        internal void Close(DrawingCollection rootDrawingGroupChildren)
        {         
            WritePreamble();            
            
            Debug.Assert(_open);
            Debug.Assert(rootDrawingGroupChildren != null);

            if (!_openedForAppend)
            {
                // Clear out the previous contents by replacing the current collection with 
                // the new collection.
                //
                // When more than one element exists in rootDrawingGroupChildren, the
                // DrawingContext had to create this new collection anyways.  To behave
                // consistently between the one-element and many-element cases,
                // we always set Children to a new DrawingCollection instance during Close().
                //
                // Doing this also avoids having to protect against exceptions being thrown
                // from user-code, which could be executed if a Changed event was fired when
                // we tried to add elements to a pre-existing collection.
                //
                // The collection created by the DrawingContext will no longer be
                // used after the DrawingContext is closed, so we can take ownership
                // of the reference here to avoid any more unneccesary copies.
                Children = rootDrawingGroupChildren;
            }
            else                
            {
                //
                //
                // Append the collection to the current Children collection                
                //
                //
                DrawingCollection children = Children;

                // 
                // Ensure that we can Append to the Children collection
                //
                
                if (children == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.DrawingGroup_CannotAppendToNullCollection));                                
                }
               
                if (children.IsFrozen)
                {
                    throw new InvalidOperationException(SR.Get(SRID.DrawingGroup_CannotAppendToFrozenCollection));                                                  
                }

                // Append the new collection to our current Children.
                //
                // TransactionalAppend rolls-back the Append operation in the event
                // an exception is thrown from the Changed event.                
                children.TransactionalAppend(rootDrawingGroupChildren);
            }            

            // This DrawingGroup is no longer open
            _open = false;
        }

        /// <summary>
        /// Calls methods on the DrawingContext that are equivalent to the
        /// Drawing with the Drawing's current value.
        /// </summary>        
        internal override void WalkCurrentValue(DrawingContextWalker ctx)
        {            
            int popCount = 0;

            // We avoid unneccessary ShouldStopWalking checks based on assumptions
            // about when ShouldStopWalking is set.  Guard that assumption with an
            // assertion.
            //
            // ShouldStopWalking is currently only set during a hit-test walk after
            // an object has been hit.  Because a DrawingGroup can't be hit until after 
            // the first Drawing is tested, this method doesn't check ShouldStopWalking
            // until after the first child.  
            //
            // We don't need to add this check to other Drawing subclasses for
            // the same reason -- if the Drawing being tested isn't a DrawingGroup,
            // they are always the 'first child'.  
            //
            // If this assumption is ever broken then the ShouldStopWalking
            // check should be done on the first child -- including in the
            // WalkCurrentValue method of other Drawing subclasses.
            Debug.Assert(!ctx.ShouldStopWalking);            

            //
            // Draw the transform property
            //
            
            // Avoid calling PushTransform if the base value is set to the default and
            // no animations have been set on the property.
            if (!IsBaseValueDefault(DrawingGroup.TransformProperty) ||
                (null != AnimationStorage.GetStorage(this, DrawingGroup.TransformProperty)))
            {
                ctx.PushTransform(Transform);

                popCount++;
            }              

            //
            // Draw the clip property
            //

            // Avoid calling PushClip if the base value is set to the default and
            // no animations have been set on the property.
            if (!IsBaseValueDefault(DrawingGroup.ClipGeometryProperty) ||
                (null != AnimationStorage.GetStorage(this, DrawingGroup.ClipGeometryProperty)))
            {    
                ctx.PushClip(ClipGeometry);

                popCount++;
            }                

            //
            // Draw the opacity property
            //
            
            // Avoid calling PushOpacity if the base value is set to the default and
            // no animations have been set on the property.
            if (!IsBaseValueDefault(DrawingGroup.OpacityProperty) ||
                (null != AnimationStorage.GetStorage(this, DrawingGroup.OpacityProperty)))
            {                    
                // Push the current value of the opacity property, which
                // is what Opacity returns.
                ctx.PushOpacity(Opacity);

                popCount++;
            }

            // Draw the opacity mask property
            //
            if (OpacityMask != null)
            {
                ctx.PushOpacityMask(OpacityMask);
                popCount++;
            }

            //
            // Draw the effect property
            //
            
            // Push the current value of the effect property, which
            // is what BitmapEffect returns.
            if (BitmapEffect != null)
            {
                // Disable warning about obsolete method.  This code must remain active 
                // until we can remove the public BitmapEffect APIs.
                #pragma warning disable 0618
                ctx.PushEffect(BitmapEffect, BitmapEffectInput);
                #pragma warning restore 0618
                popCount++;                
            }

            //
            // Draw the Children collection
            // 

            // Get the current value of the children collection
            DrawingCollection collection = Children;

            // Call Walk on each child
            if (collection != null)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    Drawing drawing = collection.Internal_GetItem(i);
                    if (drawing != null)
                    {
                        drawing.WalkCurrentValue(ctx);

                        // Don't visit the remaining children if the previous 
                        // child caused us to stop walking.
                        if (ctx.ShouldStopWalking)
                        {
                            break;
                        }
                    }
                }
            }

            //
            // Call Pop() for every Push
            // 
            // Avoid placing this logic in a finally block because if an exception is
            // thrown, the Walk is simply aborted.  There is no requirement to Walk
            // through Pop instructions when an exception is thrown.
            //
            
            for (int i = 0; i < popCount; i++)
            {
                ctx.Pop();                    
            }            
        }

         
        #endregion Internal methods     

        #region Private Methods

        /// <summary>
        /// Called by both Open() and Append(), this method verifies the
        /// DrawingGroup isn't already open, and set's the open flag.
        /// </summary>
        private void VerifyOpen()
        {
            WritePreamble();
            
            // Throw an exception if we are already opened
            if (_open)
            {
                throw new InvalidOperationException(SR.Get(SRID.DrawingGroup_AlreadyOpen));                                
            }
            
            _open = true;
        }

        #endregion Private Methods        

        #region Private fields
        
        private bool _openedForAppend;
        private bool _open;
        #endregion Private fields        
    }
}

