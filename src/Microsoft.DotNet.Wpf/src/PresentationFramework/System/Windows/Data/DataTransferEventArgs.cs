// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DataTransfer event arguments
//
// Specs:       UIBinding.mht
//

using System;

namespace System.Windows.Data
{
    /// <summary>
    /// Arguments for DataTransfer events such as TargetUpdated or SourceUpdated.
    /// </summary>
    /// <remarks>
    /// <p>The TargetUpdated event is raised whenever a value is transferred from the source to the target,
    /// (but only for bindings that have requested the event, by setting BindFlags.NotifyOnTargetUpdated).</p>
    /// <p>The SourceUpdated event is raised whenever a value is transferred from the target to the source,
    /// (but only for bindings that have requested the event, by setting BindFlags.NotifyOnSourceUpdated).</p>
    /// </remarks>
    public class DataTransferEventArgs : RoutedEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal DataTransferEventArgs(DependencyObject targetObject, DependencyProperty dp) : base()
        {
            _targetObject = targetObject;
            _dp = dp;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The target object of the binding that raised the event.
        /// </summary>
        public DependencyObject TargetObject
        {
            get { return _targetObject; }
        }

        /// <summary>
        /// The target property of the binding that raised the event.
        /// </summary>
        public DependencyProperty Property
        {
            get { return _dp; }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            EventHandler<DataTransferEventArgs> handler = (EventHandler<DataTransferEventArgs>) genericHandler;

            handler(genericTarget, this);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private DependencyObject _targetObject;
        private DependencyProperty _dp;
    }
}

