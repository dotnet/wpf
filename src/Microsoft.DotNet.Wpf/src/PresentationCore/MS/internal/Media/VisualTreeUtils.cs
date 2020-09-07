// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Media
{
    /// <summary>
    ///     Helper methods for interacting with the 2D/3D Visual tree
    /// </summary>
    internal static class VisualTreeUtils
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------        

        #region Internal Methods

        /// <summary>
        ///     This walks up the Visual tree setting the given flags starting at the
        ///     given element.
        /// </summary>
        internal static void PropagateFlags(
            DependencyObject element, 
            VisualFlags flags,
            VisualProxyFlags proxyFlags)
        {
            Visual visual;
            Visual3D visual3D;
            
            AsVisualInternal(element, out visual, out visual3D);

            if (visual != null)
            {
                Visual.PropagateFlags(visual, flags, proxyFlags);
            }
            else
            {
                Visual3D.PropagateFlags(visual3D, flags, proxyFlags);
            }
        }

        /// <summary>
        ///     Walks up the Visual tree setting or clearing the given flags.  Unlike
        ///     PropagateFlags this does not terminate when it reaches node with
        ///     the flags already set.  It always walks all the way to the root.
        /// </summary>
        internal static void SetFlagsToRoot(DependencyObject element, bool value, VisualFlags flags)
        {
            Visual visual;
            Visual3D visual3D;
            
            AsVisualInternal(element, out visual, out visual3D);

            if (visual != null)
            {
                visual.SetFlagsToRoot(value, flags);
            }
            else if (visual3D != null)
            {
                visual3D.SetFlagsToRoot(value, flags);
            }
        }

        /// <summary>
        ///     Finds the first ancestor of the given element which has the given
        ///     flags set.
        /// </summary>
        internal static DependencyObject FindFirstAncestorWithFlagsAnd(DependencyObject element, VisualFlags flags)
        {
            Visual visual;
            Visual3D visual3D;
            
            AsVisualInternal(element, out visual, out visual3D);

            if (visual != null)
            {
                return visual.FindFirstAncestorWithFlagsAnd(flags);
            }
            else if (visual3D != null)
            {
                return visual3D.FindFirstAncestorWithFlagsAnd(flags);
            }

            Debug.Assert(element == null);

            return null;
        }

        /// <summary>
        ///     Converts the given point or ray hit test result into a PointHitTestResult.
        ///     In the case of a RayHitTestResult this is done by walking up the
        ///     transforming the 3D intersection into the coordinate space of the
        ///     Viewport3DVisual which contains the Visual3D subtree.
        /// </summary>
        internal static PointHitTestResult AsNearestPointHitTestResult(HitTestResult result)
        {
            if (result == null)
            {
                return null;
            }
            
            PointHitTestResult resultAsPointHitTestResult = result as PointHitTestResult;

            if (resultAsPointHitTestResult != null)
            {
                return resultAsPointHitTestResult;
            }

            RayHitTestResult resultAsRayHitTestResult = result as RayHitTestResult;

            if (resultAsRayHitTestResult != null)
            {
                Visual3D current = (Visual3D) resultAsRayHitTestResult.VisualHit;
                Matrix3D worldTransform = Matrix3D.Identity;

                while (true)
                {
                    if (current.Transform != null)
                    {
                        worldTransform.Append(current.Transform.Value);
                    }

                    Visual3D parent3D = current.InternalVisualParent as Visual3D;

                    if (parent3D == null)
                    {
                        break;
                    }

                    current = parent3D;
                }

                Viewport3DVisual viewport = current.InternalVisualParent as Viewport3DVisual;

                if (viewport != null)
                {
                    Point4D worldPoint = ((Point4D)resultAsRayHitTestResult.PointHit) * worldTransform;
                    Point viewportPoint = viewport.WorldToViewport(worldPoint);
                    
                    return new PointHitTestResult(viewport, viewportPoint);
                }

                Debug.Fail("How did a ray hit a Visual3D not parented to a Viewport3DVisual?");

                return null;
            }

            Debug.Fail(String.Format("Unhandled HitTestResult type '{0}'", result.GetType().Name));

            return null;
        }

        /// <summary>
        ///     Throws if the given element is null or is not a Visual or Visual3D
        ///     or if the Visual is on the wrong thread.
        /// </summary>
        internal static void EnsureNonNullVisual(DependencyObject element)
        {
            EnsureVisual(element, /* allowNull = */ false);
        }

        /// <summary>
        ///     Throws if the given element is null or is not a Visual or Visual3D
        ///     or if the Visual is on the wrong thread.
        /// </summary>
        internal static void EnsureVisual(DependencyObject element)
        {
            EnsureVisual(element, /* allowNull = */ true);
        }

        /// <summary>
        ///     Throws if the given element is not a Visual or Visual3D
        ///     or if the Visual is on the wrong thread.
        /// </summary>
        private static void EnsureVisual(DependencyObject element, bool allowNull)
        {
            if (element == null)
            {
                if (!allowNull)
                {
                    throw new ArgumentNullException("element");
                }

                return;
            }

            // Would DTypes be faster?
            if (!(element is Visual || element is Visual3D))
            {
                throw new ArgumentException(SR.Get(SRID.Visual_NotAVisual));
            }

            element.VerifyAccess();            
        }

        /// <summary>
        ///     Throws if the given element is null or not a visual type, otherwise
        ///     either visual or visual3D will be non-null on exit.
        /// </summary>
        internal static void AsNonNullVisual(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            AsVisual(element, out visual, out visual3D);

            Debug.Assert((visual == null) != (visual3D == null),
                "Either visual or visual3D exclusively should be non-null.");
        }
        
        /// <summary>
        ///     Returns null if the given element is null, otherwise visual or visual3D
        ///     will be the strong visual type on exit.
        ///
        ///     Throws an exception if element is a non-Visual type.
        /// </summary>
        internal static void AsVisual(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            bool castSucceeded = AsVisualHelper(element, out visual, out visual3D);

            if (element != null)
            {
                if (!castSucceeded)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Visual_NotAVisual, element.GetType()));
                }

                Debug.Assert(castSucceeded && ((visual == null) != (visual3D == null)),
                    "Either visual or visual3D exclusively should be non-null.");

                element.VerifyAccess();
            }
            else
            {
                Debug.Assert(!castSucceeded && visual == null && visual3D == null,
                    "How did the cast succeed if the element was null going in?");
            }
        }

        /// <summary>
        ///     Internal helper to cast to DO to Visual or Visual3D.  Asserts
        ///     that the DO is really a Visual type.  Null is allowed.  Does not
        ///     VerifyAccess.
        ///
        ///     Caller is responsible for guaranteeing that element is a Visual type.
        /// </summary>
        internal static bool AsVisualInternal(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            bool castSucceeded = AsVisualHelper(element, out visual, out visual3D);

            if (!(castSucceeded || element == null))
            {
                Debug.Fail(String.Format(
                               "'{0}' is not a Visual or Visual3D. Caller is responsible for guaranteeing that element is a Visual type.",
                               element != null ? element.GetType() : null));
            }

            return castSucceeded;
        }
        
        #endregion Internal Methods        

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------
        
        #region Internal Fields

        public const string BitmapEffectObsoleteMessage = 
            "BitmapEffects are deprecated and no longer function.  Consider using Effects where appropriate instead.";

        #endregion
            
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        #region Private Methods

        // Common code for AsVisual and AsVisualInternal -- Don't call this.
        private static bool AsVisualHelper(DependencyObject element, out Visual visual, out Visual3D visual3D)
        {
            Visual elementAsVisual = element as Visual;

            if (elementAsVisual != null)
            {
                visual = elementAsVisual;
                visual3D = null;
                return true;
            }

            Visual3D elementAsVisual3D = element as Visual3D;

            if (elementAsVisual3D != null)
            {
                visual = null;
                visual3D = elementAsVisual3D;
                return true;
            }            
            
            visual = null;
            visual3D = null;
            return false;
        }

        #endregion Private Methods        
    }
}


