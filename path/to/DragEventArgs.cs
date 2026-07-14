// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace Microsoft.DotNet.Wpf.PresentationCore
{
    public class DragEventArgs : RoutedEventArgs
    {
        public DragEventArgs(RoutedEvent routedEvent, DragDropKeyStates keyStates, DragDropEffects effects, DependencyObject target, Point point)
            : base(routedEvent)
        {
            KeyStates = keyStates;
            Effects = effects;
            Target = target;
            DropPoint = point;
        }

        public DragDropKeyStates KeyStates { get; }
        public DragDropEffects Effects { get; set; }
        public DependencyObject Target { get; }
        public Point DropPoint { get; }
    }
}