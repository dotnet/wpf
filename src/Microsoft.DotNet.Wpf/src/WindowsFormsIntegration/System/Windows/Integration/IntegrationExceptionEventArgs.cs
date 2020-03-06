// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    /// Lets the user preview an exception before the exception is thrown.
    /// </summary>
    public class IntegrationExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the IntegrationExceptionEventArgs class.
        /// </summary>
        public IntegrationExceptionEventArgs(bool throwException, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_NullArgument), "exception"));
            }
            _throwException = throwException;
            _exception = exception;
        }

        private bool _throwException;
        private Exception _exception;

        /// <summary>
        /// Determines whether the exception will be thrown.
        /// </summary>
        public bool ThrowException
        {
            get
            {
                return _throwException;
            }
            set
            {
                _throwException = value;
            }
        }

        /// <summary>
        /// Identifies the exception that occurred.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }
    }
}
