// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The RenderOptions class provides definitions of attached properties
//      which will control rendering.
//

using MS.Win32.PresentationCore;

using System;
using System.Diagnostics;
using System.Security;
using System.Windows.Interop;

namespace System.Windows.Media
{
    /// <summary>
    /// RenderOptions - 
    ///      The RenderOptions class provides definitions of attached properties
    ///      which will control rendering.
    /// </summary>
    public static class RenderOptions
    {
        //
        // EdgeMode
        //
        
        /// <summary>
        /// EdgeModeProperty - Enum which descibes the manner in which we render edges of non-text primitives.
        /// </summary>
        public static readonly DependencyProperty EdgeModeProperty =
            DependencyProperty.RegisterAttached("EdgeMode", 
                                                typeof(EdgeMode), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(EdgeMode.Unspecified),
                                                new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsEdgeModeValid));
        
        /// <summary>
        /// Reads the attached property EdgeMode from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static EdgeMode GetEdgeMode(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (EdgeMode)target.GetValue(EdgeModeProperty);
        }

        /// <summary>
        /// Writes the attached property EdgeMode to the given object.
        /// </summary>
        public static void SetEdgeMode(DependencyObject target, EdgeMode edgeMode)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(EdgeModeProperty, edgeMode);
        }

        //
        // BitmapScaling
        // 

        /// <summary>
        /// BitmapScalingModeProperty - Enum which describes the manner in which we scale the images.
        /// </summary>
        public static readonly DependencyProperty BitmapScalingModeProperty =
            DependencyProperty.RegisterAttached("BitmapScalingMode", 
                                                typeof(BitmapScalingMode), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(BitmapScalingMode.Unspecified),
                                                new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsBitmapScalingModeValid));
        
        /// <summary>
        /// Reads the attached property BitmapScalingMode from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static BitmapScalingMode GetBitmapScalingMode(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (BitmapScalingMode)target.GetValue(BitmapScalingModeProperty);
        }

        /// <summary>
        /// Writes the attached property BitmapScalingMode to the given object.
        /// </summary>
        public static void SetBitmapScalingMode(DependencyObject target, BitmapScalingMode bitmapScalingMode)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(BitmapScalingModeProperty, bitmapScalingMode);
        }       

        //
        // ClearTypeHint
        // 
   
        /// <summary>
        /// ClearTypeHint - Enum which describes the ability to re-enable ClearType rendering in intermediates.
        /// </summary>
        public static readonly DependencyProperty ClearTypeHintProperty =
            DependencyProperty.RegisterAttached("ClearTypeHint", 
                                                typeof(ClearTypeHint), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(ClearTypeHint.Auto),
                                                new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsClearTypeHintValid));
        
        /// <summary>
        /// Reads the attached property ClearTypeHint from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static ClearTypeHint GetClearTypeHint(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (ClearTypeHint)target.GetValue(ClearTypeHintProperty);
        }

        /// <summary>
        /// Writes the attached property ClearTypeHint to the given object.
        /// </summary>
        public static void SetClearTypeHint(DependencyObject target, ClearTypeHint clearTypeHint)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(ClearTypeHintProperty, clearTypeHint);
        } 

        //
        // CachingHint
        //

        /// <summary>
        /// CachingHintProperty - Hints the rendering engine that rendered content should be cached
        /// when possible.  This is currently supported on TileBrush.
        /// </summary>
        public static readonly DependencyProperty CachingHintProperty =
            DependencyProperty.RegisterAttached("CachingHint", 
                                                typeof(CachingHint), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(CachingHint.Unspecified),
                                                new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsCachingHintValid));
        
        /// <summary>
        /// Reads the attached property CachingHint from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(TileBrush))]
        public static CachingHint GetCachingHint(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (CachingHint)target.GetValue(CachingHintProperty);
        }

        /// <summary>
        /// Writes the attached property CachingHint to the given object.
        /// </summary>
        public static void SetCachingHint(DependencyObject target, CachingHint cachingHint)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(CachingHintProperty, cachingHint);
        }

        //
        // CacheInvalidationThresholdMinimum
        //

        /// <summary>
        /// CacheInvalidationThresholdMinimum - 
        /// </summary>
        public static readonly DependencyProperty CacheInvalidationThresholdMinimumProperty =
            DependencyProperty.RegisterAttached("CacheInvalidationThresholdMinimum", 
                                                typeof(double), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(0.707),
                                                /* ValidateValueCallback */ null);
        
        /// <summary>
        /// Reads the attached property CacheInvalidationThresholdMinimum from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(TileBrush))]
        public static double GetCacheInvalidationThresholdMinimum(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (double)target.GetValue(CacheInvalidationThresholdMinimumProperty);
        }

        /// <summary>
        /// Writes the attached property CacheInvalidationThresholdMinimum to the given object.
        /// </summary>
        public static void SetCacheInvalidationThresholdMinimum(DependencyObject target, double cacheInvalidationThresholdMinimum)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(CacheInvalidationThresholdMinimumProperty, cacheInvalidationThresholdMinimum);
        }

        //
        // CacheInvalidationThresholdMaximum
        //

        /// <summary>
        /// CacheInvalidationThresholdMaximum - 
        /// </summary>
        public static readonly DependencyProperty CacheInvalidationThresholdMaximumProperty =
            DependencyProperty.RegisterAttached("CacheInvalidationThresholdMaximum", 
                                                typeof(double), 
                                                typeof(RenderOptions),
                                                new UIPropertyMetadata(1.414),
                                                /* ValidateValueCallback */ null);
        
        /// <summary>
        /// Reads the attached property CacheInvalidationThresholdMaximum from the given object.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(TileBrush))]
        public static double GetCacheInvalidationThresholdMaximum(DependencyObject target)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            return (double)target.GetValue(CacheInvalidationThresholdMaximumProperty);
        }

        /// <summary>
        /// Writes the attached property CacheInvalidationThresholdMaximum to the given object.
        /// </summary>
        public static void SetCacheInvalidationThresholdMaximum(DependencyObject target, double cacheInvalidationThresholdMaximum)
        {
            if (target == null) { throw new ArgumentNullException("target"); }
            target.SetValue(CacheInvalidationThresholdMaximumProperty, cacheInvalidationThresholdMaximum);
        }     

        /// <summary>
        /// Specifies the render mode preference for the process
        /// </summary>
        /// <remarks>
        ///     This property specifies a preference, it does not necessarily change the actual
        ///     rendering mode.  Among other things, this can be trumped by the registry settings.
        ///     <para/>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to set this property.
        /// </remarks>
        public static RenderMode ProcessRenderMode
        {
            get
            {
                return UnsafeNativeMethods.MilCoreApi.RenderOptions_IsSoftwareRenderingForcedForProcess() ?
                    RenderMode.SoftwareOnly : RenderMode.Default;
            }

            set
            {
                if (value != RenderMode.Default && value != RenderMode.SoftwareOnly)
                {
                    throw new System.ComponentModel.InvalidEnumArgumentException("value", (int)value, typeof(RenderMode));
                }
                
                UnsafeNativeMethods.MilCoreApi.RenderOptions_ForceSoftwareRenderingModeForProcess(
                    value == RenderMode.SoftwareOnly
                    );
            }
        }
    }
}
