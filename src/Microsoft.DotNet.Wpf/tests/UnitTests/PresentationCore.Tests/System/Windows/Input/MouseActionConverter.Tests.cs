// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

namespace System.Windows.Input
{
    public class MouseActionConverterTests
    {
        [Fact]
        public void ConvertTo_ConvertFrom_ReturnsExpected()
        {
            MouseActionConverter converter = new();

            // Test MouseAction.None (Special case)
            string emptyToNone = string.Empty;
            MouseAction constantName = MouseAction.None;

            MouseAction lowerCase = (MouseAction)converter.ConvertFrom(null, null, emptyToNone.ToLowerInvariant());
            MouseAction upperCase = (MouseAction)converter.ConvertFrom(null, null, emptyToNone.ToUpperInvariant());
            MouseAction normalColor = (MouseAction)converter.ConvertFrom(null, null, emptyToNone);

            Assert.Equal<MouseAction>(constantName, lowerCase);
            Assert.Equal<MouseAction>(constantName, upperCase);
            Assert.Equal<MouseAction>(constantName, normalColor);

            // Test the rest of the enum
            foreach (string action in Enum.GetNames<MouseAction>())
            {
                constantName = Enum.Parse<MouseAction>(action);

                lowerCase = (MouseAction)converter.ConvertFrom(null, null, action.ToLowerInvariant());
                upperCase = (MouseAction)converter.ConvertFrom(null, null, action.ToUpperInvariant());
                normalColor = (MouseAction)converter.ConvertFrom(null, null, action);

                Assert.Equal<MouseAction>(constantName, lowerCase);
                Assert.Equal<MouseAction>(constantName, upperCase);
                Assert.Equal<MouseAction>(constantName, normalColor);

                // Back to the original values
                string result = (string)converter.ConvertTo(null, null, constantName, typeof(string));
                result = result == string.Empty ? "None" : result; // Test for MouseAction.None (Special case)
                Assert.Equal(action, result);
            }
        }

        [Fact]
        public void ConvertTo_ThrowsArgumentNullException()
        {
            MouseActionConverter converter = new();

            Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(MouseAction.None, destinationType: null!));
        }

        [Theory]
        [InlineData(null, typeof(string))] // Unsupported value
        [InlineData(MouseAction.None, typeof(int))] // Unsupported destinationType
        public void ConvertTo_ThrowsNotSupportedException(object? value, Type destinationType)
        {
            MouseActionConverter converter = new();

            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(value, destinationType));
        }

        [Fact]
        public void ConvertTo_ThrowsInvalidCastException()
        {
            MouseActionConverter converter = new();

            Assert.Throws<InvalidCastException>(() => converter.ConvertTo(null, null, (int)(MouseAction.MiddleDoubleClick), typeof(string)));
        }

        [Fact]
        public void ConvertTo_ThrowsInvalidEnumArgumentException()
        {
            MouseActionConverter converter = new();

            Assert.Throws<InvalidEnumArgumentException>(() => converter.ConvertTo(null, null, (MouseAction)(MouseAction.MiddleDoubleClick + 1), typeof(string)));
        }

        [Theory]
        // Unsupported values (data type)
        [InlineData(null)]
        [InlineData(MouseAction.None)]
        // Unsupported value (bad string)
        [InlineData("BadString")]
        public void ConvertFrom_ThrowsNotSupportedException(object? value)
        {
            MouseActionConverter converter = new();

            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, null, value));
        }

        [Theory]
        // Supported type
        [InlineData(true, typeof(string))]
        // Unsupported types
        [InlineData(false, typeof(InstanceDescriptor))]
        [InlineData(false, typeof(MouseAction))]
        public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
        {
            MouseActionConverter converter = new();

            Assert.Equal(expected, converter.CanConvertFrom(null, sourceType));
        }

        [Fact]
        public void CanConvertTo_ThrowsInvalidCastException()
        {
            MouseActionConverter converter = new();
            StandardContextImpl context = new();
            context.Instance = 10;

            // NOTE: CanConvert* methods should not throw but the implementation is faulty
            Assert.Throws<InvalidCastException>(() => converter.CanConvertTo(context, typeof(string)));
        }

        [Theory]
        [MemberData(nameof(CanConvertTo_TestData))]
        public void CanConvertTo_ReturnsExpected(bool expected, bool passContext, object? value, Type? destinationType)
        {
            MouseActionConverter converter = new();
            StandardContextImpl context = new();
            context.Instance = value;

            Assert.Equal(expected, converter.CanConvertTo(passContext ? context : null, destinationType));
        }

        public static IEnumerable<object?[]> CanConvertTo_TestData
        {
            get
            {
                // Supported cases
                yield return new object[] { true, true, MouseAction.None, typeof(string) };
                yield return new object[] { true, true, MouseAction.MiddleDoubleClick, typeof(string) };

                // Unsupported case (Value is above MouseAction range)
                yield return new object[] { false, true, MouseAction.MiddleDoubleClick + 1, typeof(string) };

                // Unsupported cases
                yield return new object[] { false, false, MouseAction.None, typeof(string) };
                yield return new object[] { false, false, MouseAction.MiddleDoubleClick, typeof(string) };
                yield return new object?[] { false, true, null, typeof(MouseAction) };
                yield return new object?[] { false, true, null, typeof(string) };
                yield return new object?[] { false, false, MouseAction.MiddleDoubleClick, typeof(string) };
                yield return new object?[] { false, false, null, typeof(string) };
                yield return new object?[] { false, false, null, null };

                yield return new object[] { false, true, MouseAction.MiddleDoubleClick + 1, typeof(string) };
            }
        }

        public sealed class StandardContextImpl : ITypeDescriptorContext
        {
            public IContainer? Container => throw new NotImplementedException();

            public object? Instance { get; set; }

            public PropertyDescriptor? PropertyDescriptor => throw new NotImplementedException();
            public object? GetService(Type serviceType) => throw new NotImplementedException();
            public void OnComponentChanged() => throw new NotImplementedException();
            public bool OnComponentChanging() => throw new NotImplementedException();
        }
    }
}
