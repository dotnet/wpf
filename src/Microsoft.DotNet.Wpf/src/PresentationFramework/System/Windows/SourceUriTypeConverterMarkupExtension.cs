// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace System.Windows.Baml2006
{
    /// <summary>
    /// This class exists so that Baml2006ReaderInternal can pass additional assembly version information
    /// when setting ResourceDictionary.Source in the form of a wrapper Uri.
    /// </summary> 
    internal class SourceUriTypeConverterMarkupExtension : TypeConverterMarkupExtension
    {
        private Assembly _assemblyInfo;
        
        public SourceUriTypeConverterMarkupExtension(TypeConverter converter, object value, Assembly assemblyInfo) : base(converter, value)
        {
            _assemblyInfo = assemblyInfo;
        }
        
        /// <summary>
        /// Get the value from the base implementation which calls the appropriate type converter,
        /// if it is a Uri process it and try to append the Assembly version from _assemblyInfo.
        /// AppendAssemblyVersion will handle the cases where the Uri cannot be appended with the version
        /// and return null, in which case we should just pass the original value from the converter.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            object convertedValue =  base.ProvideValue(serviceProvider);
            Uri convertedUri = convertedValue as Uri;
            
            if (convertedUri != null)
            {
                Uri appendedVersionUri = BaseUriHelper.AppendAssemblyVersion(convertedUri, _assemblyInfo);
                if (appendedVersionUri != null)
                {
                    return new ResourceDictionary.ResourceDictionarySourceUriWrapper(convertedUri, appendedVersionUri);
                }
            }
            
            return convertedValue;
        }
    }
}
