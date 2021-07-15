// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xaml;
using System.Xaml.Schema;
using System.Diagnostics;
using System.Reflection;
using MS.Internal;

namespace System.Windows.Baml2006
{
    static class Baml6KnownTypes
    {
        public const Int16 BooleanConverter = 46;
        public const Int16 DependencyPropertyConverter = 137;
        public const Int16 EnumConverter = 195;

        public const Int16 XamlBrushSerializer = 744;
        public const Int16 XamlInt32CollectionSerializer = 745;
        public const Int16 XamlPathDataSerializer = 746;
        public const Int16 XamlPoint3DCollectionSerializer = 747;
        public const Int16 XamlPointCollectionSerializer = 748;
        public const Int16 XamlVector3DCollectionSerializer = 752;
    }

    partial class WpfSharedBamlSchemaContext: XamlSchemaContext
    {
        object _syncObject;

        // Data structures for KNOWN types/members.
        private Baml6Assembly[] _knownBamlAssemblies;
        private WpfKnownType[] _knownBamlTypes;
        private WpfKnownMember[] _knownBamlMembers;
        private Dictionary<Type, XamlType> _masterTypeTable;
        private System.Windows.Markup.XmlnsDictionary _wpfDefaultNamespace;
        private List<ThemeKnownTypeHelper> _themeHelpers;

        public WpfSharedBamlSchemaContext()
        {
            Initialize();
        }

        public WpfSharedBamlSchemaContext(XamlSchemaContextSettings settings)
            :base(settings)
        {
            Initialize();
        }

        private void Initialize()
        {
            _syncObject = new object();

            _knownBamlAssemblies = new Baml6Assembly[5];
            // Add 1 to the KnownTypeCount & KnownPropertyCount since we go from -1 - -KnownTypeCount. 
            // 0 is a placeholder
            _knownBamlTypes = new WpfKnownType[KnownTypeCount + 1];
            _masterTypeTable = new Dictionary<Type, XamlType>(256);
            _knownBamlMembers = new WpfKnownMember[KnownPropertyCount + 1];
        }

        internal string GetKnownBamlString(Int16 stringId)
        {
            string result;

            switch (stringId)
            {
                case -1:
                    result = "Name";
                    break;
                case -2:
                    result = "Uid";
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }

        internal Baml6Assembly GetKnownBamlAssembly(Int16 assemblyId)
        {
            if (assemblyId > 0)
            {
                throw new ArgumentException(SR.Get(SRID.AssemblyIdNegative));
            }
            assemblyId = (short)-assemblyId;

            Baml6Assembly assembly = _knownBamlAssemblies[assemblyId];
            if (assembly == null)
            {
                assembly = CreateKnownBamlAssembly(assemblyId);
                _knownBamlAssemblies[assemblyId] = assembly;
            }
            return assembly;
        }

        internal Baml6Assembly CreateKnownBamlAssembly(Int16 assemblyId)
        {
            Baml6Assembly assembly;

            switch (assemblyId)
            {
                case 0: assembly = new Baml6Assembly(typeof(double).Assembly); break;  // never happens ??
                case 1: assembly = new Baml6Assembly(typeof(System.Uri).Assembly); break;
                case 2: assembly = new Baml6Assembly(typeof(System.Windows.DependencyObject).Assembly); break;
                case 3: assembly = new Baml6Assembly(typeof(System.Windows.UIElement).Assembly); break;
                case 4: assembly = new Baml6Assembly(typeof(System.Windows.FrameworkElement).Assembly); break;
                default: assembly = null; break;
            }

            return assembly;
        }

        // Get Known TypesId
        // TypeId's defined in the stream are resolved by the Stream Schema Context
        //
        internal WpfKnownType GetKnownBamlType(short typeId)
        {
            WpfKnownType bamlType;

            if (typeId >= 0)
            {
                throw new ArgumentException(SR.Get(SRID.KnownTypeIdNegative));
            }

            typeId = (short)-typeId;

            lock (_syncObject)
            {
                bamlType = _knownBamlTypes[typeId];
                if(bamlType == null)
                {
                    bamlType = CreateKnownBamlType(typeId, true, true);
                    Debug.Assert(bamlType != null);
                    _knownBamlTypes[typeId] = bamlType;
                    
                    _masterTypeTable.Add(bamlType.UnderlyingType, bamlType);
                }
            }
            return bamlType;
        }

        // Get Known PropertiesId
        // PropertyId's defined in the stream are resolved by the Stream Schema Context
        //
        internal WpfKnownMember GetKnownBamlMember(short memberId)
        {
            WpfKnownMember bamlMember;

            if (memberId >= 0)
            {
                throw new ArgumentException(SR.Get(SRID.KnownTypeIdNegative));
            }

            memberId = (short)-memberId;

            lock (_syncObject)
            {
                bamlMember = _knownBamlMembers[memberId];

                if (bamlMember == null)
                {
                    bamlMember = CreateKnownMember(memberId);
                    Debug.Assert(bamlMember != null);
                    _knownBamlMembers[memberId] = bamlMember;

                    //_masterTypeTable.Add(bamlType.UnderlyingType, bamlType);
                }
            }
            return bamlMember;
        }

        // Maintain our own cache of type -> xamltype so that known types will
        // be found when we lookup XamlType by their underlying Type.
        //
        public override XamlType GetXamlType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            XamlType xamlType = GetKnownXamlType(type);
            if (xamlType == null)
            {
                xamlType = GetUnknownXamlType(type);
            }
            return xamlType;
        }

        // Assumes that GetKnownXamlType was called first
        private XamlType GetUnknownXamlType(Type type)
        {
            XamlType xamlType;

            lock (_syncObject)
            {
                if (!_masterTypeTable.TryGetValue(type, out xamlType))
                {
                    WpfSharedXamlSchemaContext.RequireRuntimeType(type);
                    xamlType = new WpfXamlType(type, this, true, true);
                    _masterTypeTable.Add(type, xamlType);
                }
            }
            return xamlType;
        }

        internal XamlType GetKnownXamlType(Type type)
        {
            XamlType xamlType;

            lock (_syncObject)
            {
                // First;  look for the type in our Main cache.
                if (!_masterTypeTable.TryGetValue(type, out xamlType))
                {
                    // Then check if it is one of the Known Types.
                    // KnowTypes created by name need to be checked for an exact match.
                    xamlType = CreateKnownBamlType(type.Name, true, true);
                    if (xamlType == null && _themeHelpers != null)
                    {
                        foreach (ThemeKnownTypeHelper helper in _themeHelpers)
                        {
                            xamlType = helper.GetKnownXamlType(type.Name);
                            if (xamlType != null && xamlType.UnderlyingType == type)
                            {
                                break;
                            }
                        }
                    }
                    if (xamlType != null && xamlType.UnderlyingType == type)
                    {
                        WpfKnownType bamlType = xamlType as WpfKnownType;
                        if (bamlType != null)
                        {
                            _knownBamlTypes[bamlType.BamlNumber] = bamlType;
                        }
                        _masterTypeTable.Add(type, xamlType);
                    }
                    else
                    {
                        // If we get a known type but the type doesn't match, set xamlType to null
                        xamlType = null;
                    }
                }
            }
            return xamlType;
        }

        internal XamlValueConverter<XamlDeferringLoader> GetDeferringLoader(Type loaderType)
        {
            return base.GetValueConverter<XamlDeferringLoader>(loaderType, null);
        }

        internal XamlValueConverter<TypeConverter> GetTypeConverter(Type converterType)
        {
            if (converterType.IsEnum)
            {
                return base.GetValueConverter<TypeConverter>(typeof(EnumConverter), GetXamlType(converterType));
            }
            else
            {
                return base.GetValueConverter<TypeConverter>(converterType, null);
            }
        }

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            return base.GetXamlType(xamlNamespace, name, typeArguments);
        }

        public XamlType GetXamlTypeExposed(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            return base.GetXamlType(xamlNamespace, name, typeArguments);
        }

        internal Type ResolvePrefixedNameWithAdditionalWpfSemantics(string prefixedName, DependencyObject element)
        {
            // First, get the xmlns dictionary off of the provided Tree Element.
            object dictObject = element.GetValue(System.Windows.Markup.XmlAttributeProperties.XmlnsDictionaryProperty);
            var prefixDictionary = dictObject as System.Windows.Markup.XmlnsDictionary;
            object mapsObject = element.GetValue(System.Windows.Markup.XmlAttributeProperties.XmlNamespaceMapsProperty);
            var namespaceMaps = mapsObject as Hashtable;

            // If there was no xmlns map on the given Tree Element.
            // Then, as a last resort, if the prefix was "" use the Wpf Element URI.
            if (prefixDictionary == null)
            {
                if (_wpfDefaultNamespace == null)
                {
                    var wpfDefaultNamespace = new System.Windows.Markup.XmlnsDictionary();
                    wpfDefaultNamespace.Add(String.Empty, Baml2006SchemaContext.WpfNamespace);
                    _wpfDefaultNamespace = wpfDefaultNamespace;
                }
                prefixDictionary = _wpfDefaultNamespace;
            }
            else
            {
                if (namespaceMaps != null && namespaceMaps.Count > 0)
                {
                    // This DO was loaded with a custom XamlTypeMapper. Try the custom mappings first.
                    Type result = System.Windows.Markup.XamlTypeMapper.GetTypeFromName(prefixedName, element);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            XamlTypeName xamlTypeName;
            if (XamlTypeName.TryParse(prefixedName, prefixDictionary, out xamlTypeName))
            {
                XamlType xamlType = GetXamlType(xamlTypeName);
                if (xamlType != null)
                {
                    return xamlType.UnderlyingType;
                }
            }
            return null;
        }

        internal XamlMember StaticExtensionMemberTypeProperty { get { return _xStaticMemberProperty.Value; } }

        internal XamlMember TypeExtensionTypeProperty { get { return _xTypeTypeProperty.Value; } }

        internal XamlMember ResourceDictionaryDeferredContentProperty { get { return _resourceDictionaryDefContentProperty.Value; } }

        internal XamlType ResourceDictionaryType { get { return _resourceDictionaryType.Value; } }

        internal XamlType EventSetterType { get { return _eventSetterType.Value; } }

        internal XamlMember EventSetterEventProperty { get { return _eventSetterEventProperty.Value; } }

        internal XamlMember EventSetterHandlerProperty { get { return _eventSetterHandlerProperty.Value; } }

        internal XamlMember FrameworkTemplateTemplateProperty { get { return _frameworkTemplateTemplateProperty.Value; } }

        internal XamlType StaticResourceExtensionType { get { return _staticResourceExtensionType.Value; } }

        internal Baml2006ReaderSettings Settings { get; set; }

        internal List<ThemeKnownTypeHelper> ThemeKnownTypeHelpers
        {
            get
            {
                if (_themeHelpers == null)
                {
                    _themeHelpers = new List<ThemeKnownTypeHelper>();
                }
                return _themeHelpers;
            }
        }

        private static readonly Lazy<XamlMember> _xStaticMemberProperty
            = new Lazy<XamlMember>(() => XamlLanguage.Static.GetMember("MemberType"));

        private static readonly Lazy<XamlMember> _xTypeTypeProperty
            = new Lazy<XamlMember>(() => XamlLanguage.Static.GetMember("Type"));

        private static readonly Lazy<XamlMember> _resourceDictionaryDefContentProperty
            = new Lazy<XamlMember>(() => _resourceDictionaryType.Value.GetMember("DeferrableContent"));

        private static readonly Lazy<XamlType> _resourceDictionaryType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(ResourceDictionary)));

        private static readonly Lazy<XamlType> _eventSetterType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(EventSetter)));

        private static readonly Lazy<XamlMember> _eventSetterEventProperty
            = new Lazy<XamlMember>(() => _eventSetterType.Value.GetMember("Event"));

        private static readonly Lazy<XamlMember> _eventSetterHandlerProperty
            = new Lazy<XamlMember>(() => _eventSetterType.Value.GetMember("Handler"));

        private static readonly Lazy<XamlMember> _frameworkTemplateTemplateProperty
            = new Lazy<XamlMember>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(FrameworkTemplate)).GetMember("Template"));

        private static readonly Lazy<XamlType> _staticResourceExtensionType
            = new Lazy<XamlType>(() => System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(typeof(StaticResourceExtension)));
    }
}
