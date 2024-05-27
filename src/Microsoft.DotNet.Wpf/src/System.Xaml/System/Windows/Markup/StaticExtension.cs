// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Class for Xaml markup extension for static field and property references.
    /// </summary>
    [TypeForwardedFrom("PresentationFramework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [TypeConverter(typeof(StaticExtensionConverter))]
    [MarkupExtensionReturnType(typeof(object))]
    public class StaticExtension : MarkupExtension
    {
        private string _member;
        private Type _memberType;

        /// <summary>
        /// Constructor that takes no parameters
        /// </summary>
        public StaticExtension()
        {
        }

        /// <summary>
        /// Constructor that takes the member that this is a static reference to.
        /// This string is of the format Prefix:ClassName.FieldOrPropertyName.
        /// The Prefix is optional, and refers to the XML prefix in a Xaml file.
        /// </summary>
        public StaticExtension(string member)
        {
            _member = member ?? throw new ArgumentNullException(nameof(member));
        }

        /// <summary>
        /// Return an object that should be set on the targetObject's targetProperty
        /// for this markup extension. For a StaticExtension this is a static field
        /// or property value.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns> The object to set on this property.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_member == null)
            {
                throw new InvalidOperationException(SR.MarkupExtensionStaticMember);
            }

            Type type = MemberType;
            string fieldString;
            string typeNameForError = null;
            if (type != null)
            {
                fieldString = _member;
                typeNameForError = type.FullName;
            }
            else
            {
                // Validate the _member
                int dotIndex = _member.IndexOf('.');
                if (dotIndex < 0)
                {
                    throw new ArgumentException(SR.Format(SR.MarkupExtensionBadStatic, _member));
                }

                // Pull out the type substring (this will include any XML prefix, e.g. "av:Button")
                string typeString = _member.Substring(0, dotIndex);
                if (string.IsNullOrEmpty(typeString))
                {
                    throw new ArgumentException(SR.Format(SR.MarkupExtensionBadStatic, _member));
                }

                // Get the IXamlTypeResolver from the service provider

                ArgumentNullException.ThrowIfNull(serviceProvider);

                IXamlTypeResolver xamlTypeResolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
                if (xamlTypeResolver == null)
                {
                    throw new ArgumentException(SR.Format(SR.MarkupExtensionNoContext, GetType().Name, nameof(IXamlTypeResolver)));
                }

                // Use the type resolver to get a Type instance.
                type = xamlTypeResolver.Resolve(typeString);

                // Get the member name substring.
                fieldString = _member.Substring(dotIndex + 1, _member.Length - dotIndex - 1);
                if (string.IsNullOrEmpty(typeString))
                {
                    throw new ArgumentException(SR.Format(SR.MarkupExtensionBadStatic, _member));
                }
            }

            // Use the built-in parser for enum types.
            if (type.IsEnum)
            {
                return Enum.Parse(type, fieldString);
            }

            // For other types, reflect.
            if (GetFieldOrPropertyValue(type, fieldString, out object value))
            {
                return value;
            }

            throw new ArgumentException(SR.Format(SR.MarkupExtensionBadStatic, typeNameForError is not null ? $"{typeNameForError}.{_member}" : _member));
        }

        /// <summary>
        /// Return false if a public static field or property with the same
        /// name cannot be found.
        /// <summary>
        private bool GetFieldOrPropertyValue(Type type, string name, out object value)
        {
            Type currentType = type;
            do
            {
                FieldInfo field = currentType.GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    value = field.GetValue(null);
                    return true;
                }

                currentType = currentType.BaseType;
            } while(currentType != null);

            currentType = type;
            do
            {
                PropertyInfo prop = currentType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    value = prop.GetValue(null,null);
                    return true;
                }

                currentType = currentType.BaseType;
            } while(currentType != null);

            value = null;
            return false;
        }

        /// <summary>
        /// The static field or property represented by a string. This string is
        /// of the format Prefix:ClassName.FieldOrPropertyName. The Prefix is
        /// optional, and refers to the XML prefix in a Xaml file.
        /// </summary>
        [ConstructorArgument("member")]
        public string Member
        {
            get => _member;
            set => _member = value ?? throw new ArgumentNullException(nameof(value));
        }

        [DefaultValue(null)]
        public Type MemberType
        {
            get => _memberType;
            set => _memberType = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
