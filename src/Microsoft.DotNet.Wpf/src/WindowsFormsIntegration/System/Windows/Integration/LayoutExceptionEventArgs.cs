// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace System.Windows.Forms.Integration
{
    /// <summary>
    /// Notifies the user when there is a layout error, and determines whether an exception 
    /// is thrown.
    /// </summary>
    public class LayoutExceptionEventArgs : IntegrationExceptionEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the LayoutExceptionEventArgs class.
        /// </summary>
        public LayoutExceptionEventArgs(Exception exception)
            : base(true, exception)
        {
        }
    }
}
