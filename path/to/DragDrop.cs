// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.DotNet.Wpf.PresentationCore
{
    public class DragDrop
    {
        // ... existing code ...

        private static Point TransformPointToTarget(Point targetPoint, DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var targetTransform = target.TransformToVisual(null);
            return targetTransform.Transform(targetPoint);
        }

        public static void RaiseDragEvent(RoutedEvent routedEvent, DragDropKeyStates keyStates, ref DragDropEffects effects, DependencyObject target, Point point)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var args = new DragEventArgs(routedEvent, keyStates, effects, target, point);
            RaiseEvent(target, routedEvent, args);
        }

        // ... existing code ...
    }
}