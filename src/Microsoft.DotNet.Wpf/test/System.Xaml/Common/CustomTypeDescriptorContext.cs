// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public Optional<Type[]> ExpectedServiceTypes { get; set; }

        public object[] Services { get; set; }

        private int CurrentIndex = 0;

        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object GetService(Type serviceType)
        {
            if (ExpectedServiceTypes.HasValue)
            {
                Assert.Equal(ExpectedServiceTypes.Value[CurrentIndex], serviceType);
            }

            return Services[CurrentIndex++];
        }

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}