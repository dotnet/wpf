// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The implementation of GestureRecognizer class
//

using MS.Utility;
using MS.Internal.Ink.GestureRecognition;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System;
using System.Security;
using SecurityHelper=MS.Internal.SecurityHelper;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    /// <summary>
    /// Performs gesture recognition on a StrokeCollection
    /// </summary>
    /// <remarks>
    ///     No finalizer is defined because all unmanaged resources are wrapped in SafeHandle's
    ///     NOTE: this class provides the public APIs that call into unmanaged code to perform 
    ///         recognition.  There are two inputs that are accepted:
    ///         ApplicationGesture[] and StrokeCollection.  
    ///         This class verifies the ApplicationGesture[] and StrokeCollection / Stroke are validated.
    /// </remarks>
    public sealed class GestureRecognizer : DependencyObject, IDisposable 
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// The default constructor which enables all the application gestures.
        /// </summary>
        public GestureRecognizer() : this ( new ApplicationGesture[] { ApplicationGesture.AllGestures } )
        {
        }

        /// <summary>
        /// The constructor which take an array of the enabled application gestures.
        /// </summary>
        /// <param name="enabledApplicationGestures"></param>
        public GestureRecognizer(IEnumerable<ApplicationGesture> enabledApplicationGestures)
        {
            _nativeRecognizer = NativeRecognizer.CreateInstance();
            if (_nativeRecognizer == null)
            {
                //just verify the gestures
                NativeRecognizer.GetApplicationGestureArrayAndVerify(enabledApplicationGestures);
            }
            else
            {
                //
                // we only set this if _nativeRecognizer is non null
                // (available) because there is no way to use this state
                // otherwise.
                //
                SetEnabledGestures(enabledApplicationGestures);
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Public Methods
        //
        //-------------------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Set the enabled gestures
        /// </summary>
        /// <param name="applicationGestures"></param>
        /// <remarks>
        ///     We wrap the System.Runtime.InteropServices.COMException with a more specific one
        /// 
        ///  	Do wrap specific exceptions in a more descriptive exception when appropriate.
        ///     public Int32 GetInt(Int32[] array, Int32 index){
        ///     try{
        ///         return array[index];
        ///     }catch(IndexOutOfRangeException e){
        ///         throw new ArgumentOutOfRangeException(
        ///             Parameter index is out of range.);
        ///     }
        ///     }
        ///
        /// </remarks>
        public void SetEnabledGestures(IEnumerable<ApplicationGesture> applicationGestures)
        {
            VerifyAccess();
            VerifyDisposed();
            VerifyRecognizerAvailable();
            //don't verfify the gestures, NativeRecognizer.SetEnabledGestures does it
            // since it is the TAS boundary

            //
            // we don't wrap the COM exceptions generated from the Recognizer
            // with our own exception
            //
            ApplicationGesture[] enabledGestures = 
                _nativeRecognizer.SetEnabledGestures(applicationGestures);

            //only update the state when SetEnabledGestures succeeds (since it verifies the array)
            _enabledGestures = enabledGestures;
}

        /// <summary>
        /// Get the enabled gestures
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<ApplicationGesture> GetEnabledGestures()
        {
            VerifyAccess();
            VerifyDisposed();
            VerifyRecognizerAvailable();

            //can be null if the call to SetEnabledGestures failed
            if (_enabledGestures == null)
            {
                _enabledGestures = new ApplicationGesture[] { };
            }
            return new ReadOnlyCollection<ApplicationGesture>(_enabledGestures);
        }

        /// <summary>
        /// Performs gesture recognition on the StrokeCollection if a gesture recognizer
        /// is present and installed on the system.  If not, this method throws an InvalidOperationException.
        /// To determine if this method will throw an exception, only call this method if
        /// the RecognizerAvailable property returns true.
        /// 
        /// </summary>
        /// <param name="strokes">The StrokeCollection to perform gesture recognition on</param>
        /// <returns></returns>
        /// <remarks>Callers must have UnmanagedCode permission to call this API.</remarks>
        public ReadOnlyCollection<GestureRecognitionResult> Recognize(StrokeCollection strokes)
        {
            return RecognizeImpl(strokes);
        }

        /// <summary>
        /// Performs gesture recognition on the StrokeCollection if a gesture recognizer
        /// is present and installed on the system.  If not, this method throws an InvalidOperationException.
        /// To determine if this method will throw an exception, only call this method if
        /// the RecognizerAvailable property returns true.
        /// </summary>
        /// <param name="strokes">The StrokeCollection to perform gesture recognition on</param>
        /// <returns></returns>
        // Built into Core, also used by Framework.
        internal ReadOnlyCollection<GestureRecognitionResult> CriticalRecognize(StrokeCollection strokes)
        {
            return RecognizeImpl(strokes);
        }

        /// <summary>
        /// Performs gesture recognition on the StrokeCollection if a gesture recognizer
        /// is present and installed on the system.
        /// </summary>
        /// <param name="strokes">The StrokeCollection to perform gesture recognition on</param>
        /// <returns></returns>
        private ReadOnlyCollection<GestureRecognitionResult> RecognizeImpl(StrokeCollection strokes)
        {
            if (strokes == null)
            {
                throw new ArgumentNullException("strokes"); // Null is not allowed as the argument value
            }
            if (strokes.Count > 2)
            {
                throw new ArgumentException(SR.Get(SRID.StrokeCollectionCountTooBig), "strokes");
            }
            VerifyAccess();
            VerifyDisposed();
            VerifyRecognizerAvailable();

            return new ReadOnlyCollection<GestureRecognitionResult>(_nativeRecognizer.Recognize(strokes));
        }

        

        #endregion Public Methods

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Indicates if a GestureRecognizer is present and installed on the system.
        /// </summary>
        public bool IsRecognizerAvailable
        {
            get
            {
                VerifyAccess();
                VerifyDisposed();

                if (_nativeRecognizer == null)
                {
                    return false;
                }
                return true;
            }
        }
        #endregion Public Properties

        //-------------------------------------------------------------------------------
        //
        // IDisposable
        //
        //-------------------------------------------------------------------------------

        #region IDisposable

        /// <summary>
        /// Dispose this GestureRecognizer
        /// </summary>
        public void Dispose()
        {
            VerifyAccess();

            // A simple pattern of the dispose implementation.
            // There is no finalizer since the SafeHandle in the NativeRecognizer will release the context properly.
            if ( _disposed )
            {
                return;
            }

            // 
            // Since the constructor might create a null _nativeRecognizer, 
            // here we have to make sure we do have some thing to dispose. 
            // Otherwise just no-op.
            if ( _nativeRecognizer != null )
            {
                _nativeRecognizer.Dispose();
                _nativeRecognizer = null;
            }
            
            _disposed = true;
        }

        #endregion IDisposable

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods
        //verify that there is a recognizer available, throw if not
        private void VerifyRecognizerAvailable()
        {
            if (_nativeRecognizer == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.GestureRecognizerNotAvailable));
            }
        }
        
        // Verify whether this object has been disposed.
        private void VerifyDisposed()
        {
            if ( _disposed )
            {
                throw new ObjectDisposedException("GestureRecognizer");
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private ApplicationGesture[]        _enabledGestures;
        private NativeRecognizer            _nativeRecognizer;
        private bool                        _disposed;

        #endregion Private Fields
    }
}
