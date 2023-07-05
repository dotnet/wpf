// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Markup
{
    /// <summary>
    /// Describes what type a markup extension can return.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed class MarkupExtensionReturnTypeAttribute : Attribute
    {
        public MarkupExtensionReturnTypeAttribute()
        {
        }

        public MarkupExtensionReturnTypeAttribute(Type returnType)
        {
            ReturnType = returnType;
        }

        [Obsolete("The expressionType argument is not used by the XAML parser. To specify the expected return type, " +
            "use MarkupExtensionReturnTypeAttribute(Type). To specify custom handling for expression types, use " +
            "XamlSetMarkupExtensionAttribute.")]
        public MarkupExtensionReturnTypeAttribute(Type returnType, Type expressionType)
        {
            ReturnType = returnType;
            ExpressionType = expressionType;
        }

        public Type ReturnType { get; }

        [Obsolete("This is not used by the XAML parser. Please look at XamlSetMarkupExtensionAttribute.")]
        public Type ExpressionType { get; }
    }
}
