// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Class for Xaml markup extension for a Type reference.
    /// </summary>
    [TypeForwardedFrom("PresentationFramework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [TypeConverter(typeof(TypeExtensionConverter))]
    [MarkupExtensionReturnType(typeof(Type))]
    public class TypeExtension : MarkupExtension
    {
        private string _typeName;
        private Type _type;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TypeExtension()
        {
        }

        /// <summary>
        /// Constructor that takes a type name
        /// </summary>
        public TypeExtension(string typeName)
        {
            _typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        /// <summary>
        /// Constructor that takes a type
        /// </summary>
        public TypeExtension(Type type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Return an object that should be set on the targetObject's targetProperty
        /// for this markup extension. TypeExtension resolves a string into a Type
        /// and returns it.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>The object to set on this property.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // If a type was supplied, no context nor type name are needed
            if (_type != null)
            {
                return _type;
            }

            // Validate the initialization.
            if (_typeName == null)
            {
                throw new InvalidOperationException(SR.MarkupExtensionTypeName);
            }

            // Get the IXamlTypeResolver from the service provider
            ArgumentNullException.ThrowIfNull(serviceProvider);            

            IXamlTypeResolver xamlTypeResolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
            if( xamlTypeResolver == null)
            {
                throw new InvalidOperationException(SR.Format(SR.MarkupExtensionNoContext, GetType().Name, nameof(IXamlTypeResolver)));
            }

            // Get the type
            _type = xamlTypeResolver.Resolve(_typeName);
            if (_type == null)
            {
                throw new InvalidOperationException(SR.Format(SR.MarkupExtensionTypeNameBad, _typeName));
            }

            return _type;
        }

        /// <summary>
        /// The typename represented by this markup extension. The name has the format
        /// Prefix:Typename, where Prefix is the xml namespace prefix for this type.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TypeName
        {
            get => _typeName;
            set
            {
                // Reset the type so ProvideValue does not use the existing type.
                _typeName = value ?? throw new ArgumentNullException(nameof(value));
                _type = null;
            }
        }

        /// <summary>
        /// The type represented by this markup extension.
        /// </summary>
        [DefaultValue(null)]
        [ConstructorArgument("type")]
        public Type Type
        {
            get => _type;
            set
            {
                // Reset the type name so ProvideValue does not use the existing type name.
                _type = value ?? throw new ArgumentNullException(nameof(value));
                _typeName = null;
            }
        }
    }
}
