// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      The implementation of GestureRecognitionResult class


using System;

namespace System.Windows.Ink
{
    /// <summary>
    /// GestureRecognitionResult
    /// </summary>
    public class GestureRecognitionResult
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        internal GestureRecognitionResult(RecognitionConfidence confidence, ApplicationGesture gesture)
        {
            _confidence = confidence;
            _gesture = gesture;
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// RecognitionConfidence Proeprty
        /// </summary>
        public RecognitionConfidence RecognitionConfidence
        {
            get
            {
                return _confidence;
            }
        }

        /// <summary>
        /// ApplicationGesture Property
        /// </summary>
        public ApplicationGesture ApplicationGesture
        {
            get
            {
                return _gesture;
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private RecognitionConfidence   _confidence;
        private ApplicationGesture      _gesture;

        #endregion Private Fields
    }
}
