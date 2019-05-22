// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    #region MediaScriptCommandRoutedEventArgs
    
    /// <summary>
    ///
    /// </summary>
    public sealed class MediaScriptCommandRoutedEventArgs : RoutedEventArgs
    {
        internal
        MediaScriptCommandRoutedEventArgs(
            RoutedEvent     routedEvent,
            object          sender,
            string          parameterType,
            string          parameterValue
            )  : base(routedEvent, sender)
        {
            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }

            if (parameterValue == null)
            {
                throw new ArgumentNullException("parameterValue");
            }

            _parameterType = parameterType;
            _parameterValue = parameterValue;
        }

        /// <summary>
        /// The type of the script command embedded in the media.
        /// </summary>
        public string ParameterType
        {
            get
            {
                return _parameterType;
            }
        }

        /// <summary>
        /// The paramter of the script command embedded in the media.
        /// </summary>
        public string ParameterValue
        {
            get
            {
                return _parameterValue;
            }
        }

        private string _parameterType;
        private string _parameterValue;
    }

    #endregion
} // namespace System.Windows

