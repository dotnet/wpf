// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
using System.Diagnostics;

namespace System.Windows.Baml2006
{
    // XamlType for WPF Known BAML types.
    //
    class WpfKnownType : WpfXamlType, ICustomAttributeProvider
    {
        // We use the same bitField as in WpfXamlType.  If we change WpfXamlType, we need to update the enum here
        [Flags]
        private enum BoolTypeBits
        {
            Frozen                              = 0x0004,
            WhitespaceSignificantCollection     = 0x0008,
            UsableDurintInit                    = 0x0010,
            HasSpecialValueConverter            = 0x0020,
        }

        private static Attribute[] s_EmptyAttributes;
        short _bamlNumber;
        string _name;
        Type _underlyingType;       
        string _contentPropertyName;
        string _runtimeNamePropertyName;
        string _dictionaryKeyPropertyName;
        string _xmlLangPropertyName;
        string _uidPropertyName;
        Func<object> _defaultConstructor;
        Type _deferringLoader;
        Type _typeConverterType;
        XamlCollectionKind _collectionKind;

        bool Frozen
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolTypeBits.Frozen); }
            set { WpfXamlType.SetFlag(ref _bitField, (byte)BoolTypeBits.Frozen, value); }
        }

        public bool WhitespaceSignificantCollection
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolTypeBits.WhitespaceSignificantCollection); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolTypeBits.WhitespaceSignificantCollection, value); }
        }

        public bool IsUsableDuringInit
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolTypeBits.UsableDurintInit); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolTypeBits.UsableDurintInit, value); }
        }
        public bool HasSpecialValueConverter
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolTypeBits.HasSpecialValueConverter); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolTypeBits.HasSpecialValueConverter, value); }
        }

        public WpfKnownType(XamlSchemaContext schema,
            int bamlNumber,
            string name,
            Type underlyingType)
            : this(schema, bamlNumber, name, underlyingType, true, true)
        {
        }

        public WpfKnownType(XamlSchemaContext schema,
            int bamlNumber,
            string name,
            Type underlyingType, 
            bool isBamlType,
            bool useV3Rules)
            : base(underlyingType, schema, isBamlType, useV3Rules)
        {
            _bamlNumber = (short)bamlNumber;
            _name = name;
            _underlyingType = underlyingType;
        }

        public void Freeze()
        {
            Frozen = true;
        }

        private void CheckFrozen()
        {
            if (Frozen)
            {
                throw new InvalidOperationException("Can't Assign to Known Type attributes");
            }
        }

        public short BamlNumber
        {
            get { return _bamlNumber; }
        }

        protected override XamlMember LookupContentProperty()
        {
            return CallGetMember(_contentPropertyName);
        }

        public string ContentPropertyName
        {
            get { return _contentPropertyName; }
            set
            {
                CheckFrozen();
                _contentPropertyName = value;
            }
        }

        protected override XamlMember LookupAliasedProperty(XamlDirective directive)
        {
            if (directive == XamlLanguage.Name)
            {
                return CallGetMember(_runtimeNamePropertyName);
            }
            else if (directive == XamlLanguage.Key && _dictionaryKeyPropertyName != null)
            {
                return LookupMember(_dictionaryKeyPropertyName, true);
            }
            else if (directive == XamlLanguage.Lang)
            {
                return CallGetMember(_xmlLangPropertyName);
            }
            else if (directive == XamlLanguage.Uid)
            {
                return CallGetMember(_uidPropertyName);
            }
            else
            {
                return null;
            }
        }

        public string RuntimeNamePropertyName
        {
            get { return _runtimeNamePropertyName; }
            set
            {
                CheckFrozen();
                _runtimeNamePropertyName = value;
            }
        }

        public string XmlLangPropertyName
        {
            get { return _xmlLangPropertyName; }
            set
            {
                CheckFrozen();
                _xmlLangPropertyName = value;
            }
        }

        public string UidPropertyName
        {
            get { return _uidPropertyName; }
            set
            {
                CheckFrozen();
                _uidPropertyName = value;
            }
        }

        public string DictionaryKeyPropertyName
        {
            get { return _dictionaryKeyPropertyName; }
            set
            {
                CheckFrozen();
                _dictionaryKeyPropertyName = value;
            }
        }

        protected override XamlCollectionKind LookupCollectionKind()
        {
            return _collectionKind;
        }

        public XamlCollectionKind CollectionKind
        {
            get { return _collectionKind; }
            set
            {
                CheckFrozen();
                _collectionKind = value;
            }
        }

        protected override bool LookupIsWhitespaceSignificantCollection()
        {
            return WhitespaceSignificantCollection;
        }

        public Func<object> DefaultConstructor
        {
            get { return _defaultConstructor; }
            set
            {
                CheckFrozen();
                _defaultConstructor = value;
            }
        }

        protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
        {
            WpfSharedBamlSchemaContext schema = System.Windows.Markup.XamlReader.BamlSharedSchemaContext;

            if (_typeConverterType != null)
            {
                return schema.GetTypeConverter(_typeConverterType);
            }
            
            if (HasSpecialValueConverter)
            {
                // This is for "object/string" syntax.
                // "Should" not cause reflection.
                return base.LookupTypeConverter();
            }

            return null;
        }

        public Type TypeConverterType
        {
            get { return _typeConverterType; }
            set
            {
                CheckFrozen();
                _typeConverterType = value;
            }
        }

        public Type DeferringLoaderType
        {
            get { return _deferringLoader; }
            set
            {
                CheckFrozen();
                _deferringLoader = value;
            }
        }

        protected override XamlValueConverter<XamlDeferringLoader> LookupDeferringLoader()
        {
            if (_deferringLoader != null)
            {
                WpfSharedBamlSchemaContext schema = System.Windows.Markup.XamlReader.BamlSharedSchemaContext;
                return schema.GetDeferringLoader(_deferringLoader);
            }
            return null;
        }

        protected override EventHandler<System.Windows.Markup.XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandler()
        {
            if (typeof(Setter).IsAssignableFrom(_underlyingType))
            {
                return Setter.ReceiveMarkupExtension;
            }
            else if (typeof(DataTrigger).IsAssignableFrom(_underlyingType))
            {
                return DataTrigger.ReceiveMarkupExtension;
            }
            else if (typeof(Condition).IsAssignableFrom(_underlyingType))
            {
                return Condition.ReceiveMarkupExtension;
            }
            return null;
        }

        protected override EventHandler<System.Windows.Markup.XamlSetTypeConverterEventArgs> LookupSetTypeConverterHandler()
        {
            if (typeof(Setter).IsAssignableFrom(_underlyingType))
            {
                return Setter.ReceiveTypeConverter;
            }
            else if (typeof(Trigger).IsAssignableFrom(_underlyingType))
            {
                return Trigger.ReceiveTypeConverter;
            }
            else if (typeof(Condition).IsAssignableFrom(_underlyingType))
            {
                return Condition.ReceiveTypeConverter;
            }
            return null;
        }

        protected override bool LookupUsableDuringInitialization()
        {
            return IsUsableDuringInit;
        }

        protected override System.Xaml.Schema.XamlTypeInvoker LookupInvoker()
        {
            return new WpfKnownTypeInvoker(this);
        }

        private XamlMember CallGetMember(string name)
        {
            if (name != null)
            {
                return GetMember(name);
            }
            return null;
        }

        private Dictionary<int, Baml6ConstructorInfo> _constructors;

        public Dictionary<int, Baml6ConstructorInfo> Constructors
        {
            get
            {
                if (_constructors == null)
                {
                    _constructors = new Dictionary<int, Baml6ConstructorInfo>();
                }
                return _constructors;
            }
        }

        protected override IList<XamlType> LookupPositionalParameters(int paramCount)
        {
            if (this.IsMarkupExtension)
            {
                List<XamlType> xTypes = null;
                Baml6ConstructorInfo info = Constructors[paramCount];
                if (Constructors.TryGetValue(paramCount, out info))
                {
                    xTypes = new List<XamlType>();
                    foreach (Type type in info.Types)
                    {
                        xTypes.Add(SchemaContext.GetXamlType(type));
                    }
                }

                return xTypes;
            }
            else
            {
                return base.LookupPositionalParameters(paramCount);
            }
        }

        protected override ICustomAttributeProvider LookupCustomAttributeProvider()
        {
            return this;
        }

        #region ICustomAttributeProvider Members

        object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
        {
            return UnderlyingType.GetCustomAttributes(inherit);
        }

        object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
        {
            Attribute result;
            if (TryGetCustomAttribute(attributeType, out result))
            {
                if (result != null)
                {
                    return new Attribute[] { result };
                }
                if (s_EmptyAttributes == null)
                {
                    s_EmptyAttributes = Array.Empty<Attribute>();
                }
                return s_EmptyAttributes;
            }
            return UnderlyingType.GetCustomAttributes(attributeType, inherit);
        }

        // Perf - Typically attributes shouldn't be looked up on known types, because we override 
        // the Lookup* methods. However, for content property, aliased properties, and XamlSet handlers, 
        // XamlType will look up inherited attributes for types that derive from the known types. 
        // So for just these attributes, we short-circuit reflection and return the known values.
        private bool TryGetCustomAttribute(Type attributeType, out Attribute result)
        {
            bool known = true;
            if (attributeType == typeof(ContentPropertyAttribute))
            {
                result = (_contentPropertyName == null) ? null : new ContentPropertyAttribute(_contentPropertyName);
            }
            else if (attributeType == typeof(RuntimeNamePropertyAttribute))
            {
                result = (_runtimeNamePropertyName == null) ? null : new RuntimeNamePropertyAttribute(_runtimeNamePropertyName);
            }
            else if (attributeType == typeof(DictionaryKeyPropertyAttribute))
            {
                result = (_dictionaryKeyPropertyName == null) ? null : new DictionaryKeyPropertyAttribute(_dictionaryKeyPropertyName);
            }
            else if (attributeType == typeof(XmlLangPropertyAttribute))
            {
                result = (_xmlLangPropertyName == null) ? null : new XmlLangPropertyAttribute(_xmlLangPropertyName);
            }
            else if (attributeType == typeof(UidPropertyAttribute))
            {
                result = (_uidPropertyName == null) ? null : new UidPropertyAttribute(_uidPropertyName);
            }
            else
            {
                result = null;
                known = false;
            }
            return known;
        }

        bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingType.IsDefined(attributeType, inherit);
        }

        #endregion
    }
}
