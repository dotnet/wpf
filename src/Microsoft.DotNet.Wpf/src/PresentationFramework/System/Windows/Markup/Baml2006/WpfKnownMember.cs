// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xaml;
using System.Xaml.Schema;
using System.ComponentModel;

namespace System.Windows.Baml2006
{
    // XamlMember for Known BAML Members.
    //
    class WpfKnownMember : WpfXamlMember
    {
        [Flags]
        private enum BoolMemberBits
        {
            Frozen                      = 0x0001,
            HasSpecialTypeConverter     = 0x0002,
            ReadOnly                    = 0x0004,
            Ambient                     = 0x0008,
            ReadPrivate                 = 0x0010,
            WritePrivate                = 0x0020
        }

        Action<object, object> _setDelegate;
        Func<object, object> _getDelegate;
        Type _deferringLoader;
        Type _typeConverterType;
        Type _type;
        byte _bitField;

        bool Frozen
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.Frozen); }
            set { WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.Frozen, value); }
        }
        bool ReadOnly
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.ReadOnly); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.ReadOnly, value); }
        }

        public bool HasSpecialTypeConverter
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.HasSpecialTypeConverter); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.HasSpecialTypeConverter, value); }
        }
        public bool Ambient
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.Ambient); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.Ambient, value); }
        }
        public bool IsReadPrivate
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.ReadPrivate); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.ReadPrivate, value); }
        }
        public bool IsWritePrivate
        {
            get { return WpfXamlType.GetFlag(ref _bitField, (byte)BoolMemberBits.WritePrivate); }
            set { CheckFrozen(); WpfXamlType.SetFlag(ref _bitField, (byte)BoolMemberBits.WritePrivate, value); }
        }

        public WpfKnownMember(XamlSchemaContext schema,
            XamlType declaringType,
            string name,
            DependencyProperty dProperty,
            bool isReadOnly,
            bool isAttachable)
            : base(dProperty, isAttachable)
        {
            DependencyProperty = dProperty;
            ReadOnly = isReadOnly;
        }

        public WpfKnownMember(XamlSchemaContext schema,
            XamlType declaringType,
            string name,
            Type type,
            bool isReadOnly,
            bool isAttachable)
            : base(name, declaringType, isAttachable)
        {
            _type = type;
            ReadOnly = isReadOnly;
        }

        protected override bool LookupIsUnknown()
        {
            return false;
        }

        public void Freeze()
        {
            Frozen = true;
        }

        private void CheckFrozen()
        {
            if (Frozen)
            {
                throw new InvalidOperationException("Can't Assign to Known Member attributes");
            }
        }

        protected override System.Xaml.Schema.XamlMemberInvoker LookupInvoker()
        {
            return new WpfKnownMemberInvoker(this);
        }

        public Action<object, object> SetDelegate
        {
            get
            {
                return _setDelegate;
            }
            set
            {
                CheckFrozen();
                _setDelegate = value;
            }
        }

        public Func<object, object> GetDelegate
        {
            get
            {
                return _getDelegate;
            }
            set
            {
                CheckFrozen();
                _getDelegate = value;
            }
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

        protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
        {
            WpfSharedBamlSchemaContext schema = System.Windows.Markup.XamlReader.BamlSharedSchemaContext;

            if (HasSpecialTypeConverter)
            {
                return schema.GetXamlType(_typeConverterType).TypeConverter;
            }

            if (_typeConverterType != null)
            {
                return schema.GetTypeConverter(_typeConverterType);
            }

            return null;
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

        protected override bool LookupIsReadOnly()
        {
            return ReadOnly;
        }

        protected override XamlType LookupType()
        {
            if (DependencyProperty != null)
            {
                return System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(DependencyProperty.PropertyType);
            }
            return System.Windows.Markup.XamlReader.BamlSharedSchemaContext.GetXamlType(_type);
        }

        protected override MemberInfo LookupUnderlyingMember()
        {
            return base.LookupUnderlyingMember();
        }

        protected override bool LookupIsAmbient()
        {
            return Ambient;
        }

        protected override bool LookupIsWritePublic()
        {
            return !IsWritePrivate;
        }

        protected override bool LookupIsReadPublic()
        {
            return !IsReadPrivate;
        }

        // None of the known members need the content-property fallback
        protected override WpfXamlMember GetAsContentProperty()
        {
            return this;
        }
    }
}
