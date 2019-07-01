// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ContentControl))]
    public abstract class ContentControlFactory<T> : DiscoverableFactory<T> where T : ContentControl
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Content { get; set; }

        protected void ApplyContentControlProperties(T contentControl)
        {
            contentControl.Content = Content;
        }
    }
}
