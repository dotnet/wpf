// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomServiceProvider : IServiceProvider
    {
        public object Service { get; set; }

        public object GetService(Type serviceType) => Service;
    }
}
