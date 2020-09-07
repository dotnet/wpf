// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    /// Enables the user to see the property that threw an exception, and to preview or cancel the exception.
    /// </summary>
    public class PropertyMappingExceptionEventArgs : IntegrationExceptionEventArgs 
    {
        private string _propertyName;
        private object _propertyValue;

        /// <summary>
        /// Initializes a new instance of the PropertyMappingExceptionEventArgs class.
        /// </summary>
        public PropertyMappingExceptionEventArgs(Exception exception, string propertyName, object propertyValue) 
            : base(false, exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_NullArgument), "exception"));
            }
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.WFI_ArgumentNullOrEmpty), "propertyName"));
            }
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        /// <summary>
        /// Identifies the property that was being mapped when the exception occurred.
        /// </summary>
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        /// <summary>
        /// Specifies the value of the property that was being mapped when the exception occurred.
        /// </summary>
        public object PropertyValue
        {
            get
            {
                return _propertyValue;
            }
        }

    }
}
