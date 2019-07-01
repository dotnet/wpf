// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete HeaderedContentControl factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class HeaderedContentControlFactory<T> : ContentControlFactory<T> where T : HeaderedContentControl
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set HeaderedContentControl Header property.
        /// </summary>
        public Object Header { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common HeaderedContentControl properties.
        /// </summary>
        /// <param name="headeredContentControl"></param>
        protected void ApplyHeaderedContentControlProperties(T headeredContentControl)
        {
            ApplyContentControlProperties(headeredContentControl);
            headeredContentControl.Header = Header;
        }

        #endregion
    }
}
