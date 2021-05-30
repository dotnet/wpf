// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests
{
    public class ConstructorArgumentAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("argumentName")]
        public void Ctor_String(string argumentName)
        {
            var attribute = new ConstructorArgumentAttribute(argumentName);
            Assert.Equal(argumentName, attribute.ArgumentName);
        }
    }
}
