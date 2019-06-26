// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Diagnostics;
using MS.Internal;
using System.Security;

//------------------------------------------------------------------------------
// This section lists various things that we could improve on the DrawingContxt
// class.
//
// - Remove the isAnimated flag from being propagated everywhere. Rather mark
//   the pail when we add an animated argument.
//------------------------------------------------------------------------------

namespace System.Windows.Media
{
    /// <summary>
    /// Drawing context.
    /// </summary>
    internal partial class RenderDataDrawingContext : DrawingContext, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Creates a drawing context which is associated with a given Dispatcher
        /// </summary>
        internal RenderDataDrawingContext()
        {
        }

        #endregion Constructors

        #region Public Methods
        
        internal RenderData GetRenderData()
        {
            return _renderData;
        }

        /// <summary>
        /// Closes the DrawingContext and flushes the content.
        /// Afterwards the DrawingContext can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        public override void Close()
        {
            VerifyApiNonstructuralChange();

            ((IDisposable)this).Dispose();
        }
        /// <summary>
        /// This is the same as the Close call:
        /// Closes the DrawingContext and flushes the content.
        /// Afterwards the DrawingContext can not be used anymore.
        /// This call does not require all Push calls to have been Popped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        protected override void DisposeCore()
        {
            if (!_disposed)
            {
                EnsureCorrectNesting();
                CloseCore(_renderData);
                _disposed = true;
            }
        }


        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// CloseCore - Implemented be derivees to Close the context.
        /// This will only be called once (if ever) per instance.
        /// </summary>
        /// <param name="renderData"> The render data produced by this RenderDataDrawingContext.  </param>
        protected virtual void CloseCore(RenderData renderData) {}

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// EnsureRenderData - this method ensures that the _renderData variable is initialized.
        /// The render data's _buffer will be lazily instantiated via the Draw* methods which all
        /// call WriteDataRecord.  
        /// </summary>
        private void EnsureRenderData()
        {
            if (_renderData == null)
            {
                _renderData = new RenderData();
            }
        }

        /// <summary>
        /// This verifies that the API can be called for access which doesn't affect structure.
        /// </summary>
        protected override void VerifyApiNonstructuralChange()
        {
            base.VerifyApiNonstructuralChange();

            if (_disposed)
            {
                throw new ObjectDisposedException("RenderDataDrawingContext");
            }
        }

        /// <summary>
        /// This method checks if there were any Push calls that were not matched by
        /// the corresponding Pop. If this is the case, this method adds corresponding
        /// number of Pop instructions to the render data.
        /// </summary>
        private void EnsureCorrectNesting()
        {
            if (_renderData != null && _stackDepth > 0)
            {
                int stackDepth = _stackDepth;
                for (int i = 0; i < stackDepth; i++)
                {
                    Pop();
                }
            }
            
            _stackDepth = 0;
        }

        #endregion Private Methods

        #region Fields

        private RenderData _renderData;
        private bool _disposed;
        private int _stackDepth;

        #endregion Fields
    }
}
