// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;

    /// <summary>
    ///     A change tracking expression that is used to raise property change events.
    /// </summary>
    internal class PropertyChangeTracker : Expression 
    {
        internal PropertyChangeTracker(DependencyObject obj, DependencyProperty property)
            : base(ExpressionMode.NonSharable | ExpressionMode.ForwardsInvalidations) 
        {
            Debug.Assert(obj != null && property != null);
            _object = obj;
            _property = property;
            ChangeSources(_object, _property, new DependencySource[] { new DependencySource(obj, property) });
        }

        internal override void OnPropertyInvalidation(DependencyObject d, DependencyPropertyChangedEventArgs args) 
        {
            DependencyProperty dp = args.Property;
            if (_object == d && _property == dp && Changed != null) 
            {
                Changed(_object, EventArgs.Empty);
            }
        }

        internal void Close() 
        {
            _object = null;
            _property = null;
            ChangeSources(null, null, null);
        }

        internal bool CanClose 
        {
            get { return Changed == null; }
        }

        internal EventHandler Changed;

        private DependencyObject _object;
        private DependencyProperty _property;
    }
}

