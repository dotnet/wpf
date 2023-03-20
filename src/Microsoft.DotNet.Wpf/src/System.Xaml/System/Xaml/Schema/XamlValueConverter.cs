// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading;

namespace System.Xaml.Schema
{
    public class XamlValueConverter<TConverterBase> : IEquatable<XamlValueConverter<TConverterBase>> 
        where TConverterBase : class
    {
        // Assignment should be idempotent
        private TConverterBase _instance;
        private ThreeValuedBool _isPublic;

        private volatile bool _instanceIsSet; // volatile for the same reason as valid flags in TypeReflector/MemberReflector

        public string Name { get; }
        public Type ConverterType { get; }
        public XamlType TargetType { get; }

        public XamlValueConverter(Type converterType, XamlType targetType)
            :this(converterType, targetType, null)
        {
        }

        public XamlValueConverter(Type converterType, XamlType targetType, string name)
        {
            if (converterType == null && targetType == null && name == null)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentRequired, "converterType, targetType, name"));
            }
            ConverterType = converterType;
            TargetType = targetType;
            Name = name ?? GetDefaultName();
        }

        public TConverterBase ConverterInstance
        {
            get
            {
                if (!_instanceIsSet)
                {
                    Interlocked.CompareExchange(ref _instance, CreateInstance(), null);
                    _instanceIsSet = true;
                }
                return _instance;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        internal virtual bool IsPublic
        {
            get
            {
                if (_isPublic == ThreeValuedBool.NotSet)
                {
                    _isPublic = (ConverterType == null || ConverterType.IsVisible) ? ThreeValuedBool.True : ThreeValuedBool.False;
                }
                return _isPublic == ThreeValuedBool.True;
            }
        }

        protected virtual TConverterBase CreateInstance()
        {
            if (ConverterType == typeof(EnumConverter) &&
                TargetType.UnderlyingType != null && TargetType.UnderlyingType.IsEnum)
            {
                return (TConverterBase)(object)new EnumConverter(TargetType.UnderlyingType);
            }
            else if (ConverterType != null)
            {
                if (!typeof(TConverterBase).IsAssignableFrom(ConverterType))
                {
                    throw new XamlSchemaException(SR.Format(SR.ConverterMustDeriveFromBase,
                       ConverterType, typeof(TConverterBase)));
                }
                return (TConverterBase)Activator.CreateInstance(ConverterType, null);
            }
            return null;
        }

        private string GetDefaultName()
        {
            if (ConverterType != null)
            {
                if (TargetType != null)
                {
                    return ConverterType.Name + "(" + TargetType.Name + ")";
                }
                return ConverterType.Name;
            }
            return TargetType.Name;
        }

        #region IEquatable<XamlValueConverter<TConverterBaseType>> Members

        public override bool Equals(object obj)
        {
            XamlValueConverter<TConverterBase> other = obj as XamlValueConverter<TConverterBase>;
            if (other is null)
            {
                return false;
            }
            return this == other;
        }

        public override int GetHashCode()
        {
            int result = Name.GetHashCode();
            if (ConverterType != null)
            {
                result ^= ConverterType.GetHashCode();
            }
            if (TargetType != null)
            {
                result ^= TargetType.GetHashCode();
            }
            return result;
        }

        public bool Equals(XamlValueConverter<TConverterBase> other)
        {
            return this == other;
        }

        public static bool operator ==(XamlValueConverter<TConverterBase> converter1, XamlValueConverter<TConverterBase> converter2)
        {
            if (converter1 is null)
            {
                return converter2 is null;
            }
            if (converter2 is null)
            {
                return false;
            }
            return converter1.ConverterType == converter2.ConverterType &&
                converter1.TargetType == converter2.TargetType &&
                converter1.Name == converter2.Name;
        }

        public static bool operator !=(XamlValueConverter<TConverterBase> converter1, XamlValueConverter<TConverterBase> converter2)
        {
            return !(converter1 == converter2);
        }

        #endregion
    }
}
