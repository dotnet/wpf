// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Security.Permissions;
    using Microsoft.Test.Win32;
    using Microsoft.Test.Display;


    public partial class ImageUtility
    {
        #region Public Methods

        /// <summary>
        /// Captures a bitmap of a speicifed UIElement
        /// </summary>
        /// <param name="target">UIElement to capture</param>
        /// <returns>a Bitmap containing the captured UIElement</returns>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public static System.Drawing.Bitmap CaptureElement(UIElement target) {
            System.Drawing.Rectangle rect = GetScreenBoundingRectangle(target);
            return CaptureScreen(rect);
        }

        /// <summary>
        /// Returns the screen bounding rectangle for an Element relative to the screen origin.
        /// </summary>
        /// <param name="target">UIElement to get screen bounding rectangle</param>
        /// <returns>the screen bounding rectangle relative to the screen origin for the specified Element</returns>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public static System.Drawing.Rectangle GetScreenBoundingRectangle(UIElement target) {
            PresentationSource source = PresentationSource.FromVisual(target);
            if (source == null)
                throw new InvalidOperationException("The specified UiElement is not connected to a rendering Visual Tree.");

            Matrix transform;
            try
            {
                System.Windows.Media.GeneralTransform gt = 
                    target.TransformToAncestor(source.RootVisual);
                System.Windows.Media.Transform t = gt as System.Windows.Media.Transform;
                if(t!=null)
                {
                   transform = t.Value;
                }
                else
                {
                    throw new System.ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                }

            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The specified UiElement is not connected to a rendering Visual Tree.");
            }
            Rect targetRect = new Rect(new Point(), target.RenderSize);
            targetRect.Transform(transform);

            Point rootOffset = targetRect.TopLeft;

            System.Windows.Media.CompositionTarget vm = source.CompositionTarget;
            rootOffset = vm.TransformToDevice.Transform(rootOffset);//vm.DeviceUnitsFromMeasureUnits(rootOffset);
            System.Drawing.Point topLeft = new System.Drawing.Point((int)rootOffset.X, (int)rootOffset.Y);
            User32.ClientToScreen(((System.Windows.Interop.HwndSource)source).Handle, ref topLeft);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(topLeft, new System.Drawing.Size(Convert.ToInt32(Monitor.ConvertLogicalToScreen(Dimension.Width, target.RenderSize.Width)), Convert.ToInt32(Monitor.ConvertLogicalToScreen(Dimension.Height, target.RenderSize.Height))));

            return rect;
        }

        #endregion Public Methods
    }
}
