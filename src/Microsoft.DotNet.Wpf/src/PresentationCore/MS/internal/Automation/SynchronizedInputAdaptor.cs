// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Synchronized Input pattern adaptor
// 

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Automation
{
    /// <summary>
    /// Represents a synchronized input provider that supports the synchronized input pattern across 
    /// UIElements, ContentElements and UIElement3D.
    /// </summary>
    internal class SynchronizedInputAdaptor : ISynchronizedInputProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owner">UIElement or ContentElement or UIElement3D this adaptor is associated with.</param>
        internal SynchronizedInputAdaptor(DependencyObject owner)
        {
            Invariant.Assert(owner != null);
            _owner = owner;
        }
        /// <summary>
        /// This method is called by automation framework to trigger synchronized input processing.  
        /// </summary>
        /// <param name="inputType"> Synchronized input type</param>
        void ISynchronizedInputProvider.StartListening(SynchronizedInputType inputType)
        {
            if (inputType != SynchronizedInputType.KeyDown &&
                inputType != SynchronizedInputType.KeyUp &&
                inputType != SynchronizedInputType.MouseLeftButtonDown &&
                inputType != SynchronizedInputType.MouseLeftButtonUp &&
                inputType != SynchronizedInputType.MouseRightButtonDown &&
                inputType != SynchronizedInputType.MouseRightButtonUp)
            {
                throw new ArgumentException(SR.Get(SRID.Automation_InvalidSynchronizedInputType, inputType));
            }
            
            UIElement e = _owner as UIElement;
            if (e != null)
            {
                if (!e.StartListeningSynchronizedInput(inputType))
                {
                    throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));
                }
            }
            else
            {
                ContentElement ce = _owner as ContentElement;
                if (ce != null)
                {
                    if (!ce.StartListeningSynchronizedInput(inputType))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));
                    }
                }
                else
                {
                    UIElement3D e3D = (UIElement3D)_owner;
                    if (!e3D.StartListeningSynchronizedInput(inputType))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.Automation_RecursivePublicCall));
                    }
}
            }
        }

        ////<summary>
        /// Cancel synchronized input processing.
        ///</summary>
        void ISynchronizedInputProvider.Cancel()
        {
            UIElement e = _owner as UIElement;
            if (e != null)
            {
                e.CancelSynchronizedInput();
            }
            else
            {
                ContentElement ce = _owner as ContentElement;
                if (ce != null)
                {
                    ce.CancelSynchronizedInput();
                }
                else
                {
                    UIElement3D e3D = (UIElement3D)_owner;
                    e3D.CancelSynchronizedInput();
                }
            }
}

        private readonly DependencyObject _owner;
    }
}  
