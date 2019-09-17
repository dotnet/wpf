// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provide default conversion between source values and
//              target values, for data binding.  The default ValueConverter
//              typically wraps a type converter.
//

using System;
using System.Globalization;
using System.Collections;
using System.ComponentModel;

using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;    // BaseUriHelper
using System.Windows.Baml2006; // WpfKnownType
using System.Windows.Markup; // IUriContext

using MS.Internal;          // Invariant.Assert
using System.Diagnostics;

namespace MS.Internal.Data
{
    #region DefaultValueConverter

    internal class DefaultValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        protected DefaultValueConverter(TypeConverter typeConverter, Type sourceType, Type targetType,
                                        bool shouldConvertFrom, bool shouldConvertTo, DataBindEngine engine)
        {
            _typeConverter = typeConverter;
            _sourceType = sourceType;
            _targetType = targetType;
            _shouldConvertFrom = shouldConvertFrom;
            _shouldConvertTo = shouldConvertTo;
            _engine = engine;
        }

        //------------------------------------------------------
        //
        //  Internal static API
        //
        //------------------------------------------------------

        // static constructor - returns a ValueConverter suitable for converting between
        // the source and target.  The flag indicates whether targetToSource
        // conversions are actually needed.
        // if no Converter is needed, return DefaultValueConverter.ValueConverterNotNeeded marker.
        // if unable to create a DefaultValueConverter, return null to indicate error.
        internal static IValueConverter Create(Type sourceType,
                                                Type targetType,
                                                bool targetToSource,
                                                DataBindEngine engine)
        {
            TypeConverter typeConverter;
            Type innerType;
            bool canConvertTo, canConvertFrom;
            bool sourceIsNullable = false;
            bool targetIsNullable = false;

            // sometimes, no conversion is necessary
            if (sourceType == targetType ||
                (!targetToSource && targetType.IsAssignableFrom(sourceType)))
            {
                return ValueConverterNotNeeded;
            }

            // the type convert for System.Object is useless.  It claims it can
            // convert from string, but then throws an exception when asked to do
            // so.  So we work around it.
            if (targetType == typeof(object))
            {
                // The sourceType here might be a Nullable type: consider using
                // NullableConverter when appropriate. (uncomment following lines)
                //Type innerType = Nullable.GetUnderlyingType(sourceType);
                //if (innerType != null)
                //{
                //    return new NullableConverter(new ObjectTargetConverter(innerType),
                //                                 innerType, targetType, true, false);
                //}

                // BUG: 1109257 ObjectTargetConverter is not the best converter possible.
                return new ObjectTargetConverter(sourceType, engine);
            }
            else if (sourceType == typeof(object))
            {
                // The targetType here might be a Nullable type: consider using
                // NullableConverter when appropriate. (uncomment following lines)
                //Type innerType = Nullable.GetUnderlyingType(targetType);
                // if (innerType != null)
                // {
                //     return new NullableConverter(new ObjectSourceConverter(innerType),
                //                                  sourceType, innerType, false, true);
                // }

                // BUG: 1109257 ObjectSourceConverter is not the best converter possible.
                return new ObjectSourceConverter(targetType, engine);
            }

            // use System.Convert for well-known base types
            if (SystemConvertConverter.CanConvert(sourceType, targetType))
            {
                return new SystemConvertConverter(sourceType, targetType);
            }

            // Need to check for nullable types first, since NullableConverter is a bit over-eager;
            // TypeConverter for Nullable can convert e.g. Nullable<DateTime> to string
            // but it ends up doing a different conversion than the TypeConverter for the
            // generic's inner type, e.g. bug 1361977
            innerType = Nullable.GetUnderlyingType(sourceType);
            if (innerType != null)
            {
                sourceType = innerType;
                sourceIsNullable = true;
            }
            innerType = Nullable.GetUnderlyingType(targetType);
            if (innerType != null)
            {
                targetType = innerType;
                targetIsNullable = true;
            }
            if (sourceIsNullable || targetIsNullable)
            {
                // single-level recursive call to try to find a converter for basic value types
                return Create(sourceType, targetType, targetToSource, engine);
            }

            // special case for converting IListSource to IList
            if (typeof(IListSource).IsAssignableFrom(sourceType) &&
                targetType.IsAssignableFrom(typeof(IList)))
            {
                return new ListSourceConverter();
            }

            // Interfaces are best handled on a per-instance basis.  The type may
            // not implement the interface, but an instance of a derived type may.
            if (sourceType.IsInterface || targetType.IsInterface)
            {
                return new InterfaceConverter(sourceType, targetType);
            }

            // try using the source's type converter
            typeConverter = GetConverter(sourceType);
            canConvertTo = (typeConverter != null) ? typeConverter.CanConvertTo(targetType) : false;
            canConvertFrom = (typeConverter != null) ? typeConverter.CanConvertFrom(targetType) : false;

            if ((canConvertTo || targetType.IsAssignableFrom(sourceType)) &&
                (!targetToSource || canConvertFrom || sourceType.IsAssignableFrom(targetType)))
            {
                return new SourceDefaultValueConverter(typeConverter, sourceType, targetType,
                                                       targetToSource && canConvertFrom, canConvertTo, engine);
            }

            // if that doesn't work, try using the target's type converter
            typeConverter = GetConverter(targetType);
            canConvertTo = (typeConverter != null) ? typeConverter.CanConvertTo(sourceType) : false;
            canConvertFrom = (typeConverter != null) ? typeConverter.CanConvertFrom(sourceType) : false;

            if ((canConvertFrom || targetType.IsAssignableFrom(sourceType)) &&
                (!targetToSource || canConvertTo || sourceType.IsAssignableFrom(targetType)))
            {
                return new TargetDefaultValueConverter(typeConverter, sourceType, targetType,
                                                       canConvertFrom, targetToSource && canConvertTo, engine);
            }

            // nothing worked, give up
            return null;
        }

        internal static TypeConverter GetConverter(Type type)
        {
            TypeConverter typeConverter = null;
            WpfKnownType knownType = XamlReader.BamlSharedSchemaContext.GetKnownXamlType(type) as WpfKnownType;
            if (knownType != null && knownType.TypeConverter != null)
            {
                typeConverter = knownType.TypeConverter.ConverterInstance;
            }
            if (typeConverter == null)
            {
                typeConverter = TypeDescriptor.GetConverter(type);
            }

            return typeConverter;
        }

        // some types have Parse methods that are more successful than their
        // type converters at converting strings.
        // [This code is lifted from WinForms - formatter.cs]
        internal static object TryParse(object o, Type targetType, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;
            string stringValue = o as String;

            if (stringValue != null)
            {
                try
                {
                    MethodInfo mi;

                    if (culture != null && (mi = targetType.GetMethod("Parse",
                                            BindingFlags.Public | BindingFlags.Static,
                                            null,
                                            new Type[] { StringType, typeof(System.Globalization.NumberStyles), typeof(System.IFormatProvider) },
                                            null))
                                != null)
                    {
                        result = mi.Invoke(null, new object[] { stringValue, NumberStyles.Any, culture });
                    }
                    else if (culture != null && (mi = targetType.GetMethod("Parse",
                                            BindingFlags.Public | BindingFlags.Static,
                                            null,
                                            new Type[] { StringType, typeof(System.IFormatProvider) },
                                            null))
                                != null)
                    {
                        result = mi.Invoke(null, new object[] { stringValue, culture });
                    }
                    else if ((mi = targetType.GetMethod("Parse",
                                            BindingFlags.Public | BindingFlags.Static,
                                            null,
                                            new Type[] { StringType },
                                            null))
                                != null)
                    {
                        result = mi.Invoke(null, new object[] { stringValue });
                    }
                }
                catch (TargetInvocationException)
                {
                }
            }

            return result;
        }

        internal static readonly IValueConverter ValueConverterNotNeeded = new ObjectTargetConverter(typeof(object), null);

        //------------------------------------------------------
        //
        //  Protected API
        //
        //------------------------------------------------------

        protected object ConvertFrom(object o, Type destinationType, DependencyObject targetElement, CultureInfo culture)
        {
            return ConvertHelper(o, destinationType, targetElement, culture, false);
        }

        protected object ConvertTo(object o, Type destinationType, DependencyObject targetElement, CultureInfo culture)
        {
            return ConvertHelper(o, destinationType, targetElement, culture, true);
        }

        // for lazy creation of the type converter, since GetConverter is expensive
        protected void EnsureConverter(Type type)
        {
            if (_typeConverter == null)
            {
                _typeConverter = GetConverter(type);
            }
        }

        //------------------------------------------------------
        //
        //  Private API
        //
        //------------------------------------------------------

        private object ConvertHelper(object o, Type destinationType, DependencyObject targetElement, CultureInfo culture, bool isForward)
        {
            object value = DependencyProperty.UnsetValue;
            bool needAssignment = (isForward ? !_shouldConvertTo : !_shouldConvertFrom);
            NotSupportedException savedEx = null;

            if (!needAssignment)
            {
                value = TryParse(o, destinationType, culture);

                if (value == DependencyProperty.UnsetValue)
                {
                    ValueConverterContext ctx = Engine.ValueConverterContext;

                    // The fixed VCContext object is usually available for re-use.
                    // In the rare cases when a second conversion is requested while
                    // a previous conversion is still in progress, we allocate a temporary
                    // context object to handle the re-entrant request. 
                    if (ctx.IsInUse)
                    {
                        ctx = new ValueConverterContext();
                    }

                    try
                    {
                        ctx.SetTargetElement(targetElement);
                        if (isForward)
                        {
                            value = _typeConverter.ConvertTo(ctx, culture, o, destinationType);
                        }
                        else
                        {
                            value = _typeConverter.ConvertFrom(ctx, culture, o);
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        needAssignment = true;
                        savedEx = ex;
                    }
                    finally
                    {
                        ctx.SetTargetElement(null);
                    }
                }
            }

            if (needAssignment &&
                ((o != null && destinationType.IsAssignableFrom(o.GetType())) ||
                  (o == null && !destinationType.IsValueType)))
            {
                value = o;
                needAssignment = false;
            }

            if (TraceData.IsEnabled)
            {
                if ((culture != null) && (savedEx != null))
                {
                    TraceData.Trace(TraceEventType.Error,
                        TraceData.DefaultValueConverterFailedForCulture(
                            AvTrace.ToStringHelper(o),
                            AvTrace.TypeName(o),
                            destinationType.ToString(),
                            culture),
                        savedEx);
                }
                else if (needAssignment)
                {
                    TraceData.Trace(TraceEventType.Error,
                        TraceData.DefaultValueConverterFailed(
                            AvTrace.ToStringHelper(o),
                            AvTrace.TypeName(o),
                            destinationType.ToString()),
                        savedEx);
                }
            }

            if (needAssignment && savedEx != null)
                throw savedEx;

            return value;
        }

        protected DataBindEngine Engine { get { return _engine; } }

        protected Type _sourceType;
        protected Type _targetType;

        private TypeConverter _typeConverter;
        private bool _shouldConvertFrom;
        private bool _shouldConvertTo;
        private DataBindEngine _engine;

        static Type StringType = typeof(String);
    }

    #endregion DefaultValueConverter

    #region SourceDefaultValueConverter

    internal class SourceDefaultValueConverter : DefaultValueConverter, IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        public SourceDefaultValueConverter(TypeConverter typeConverter, Type sourceType, Type targetType,
                                           bool shouldConvertFrom, bool shouldConvertTo, DataBindEngine engine)
            : base(typeConverter, sourceType, targetType, shouldConvertFrom, shouldConvertTo, engine)
        {
        }

        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------


        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertTo(o, _targetType, parameter as DependencyObject, culture);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertFrom(o, _sourceType, parameter as DependencyObject, culture);
        }
    }

    #endregion SourceDefaultValueConverter

    #region TargetDefaultValueConverter

    internal class TargetDefaultValueConverter : DefaultValueConverter, IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        public TargetDefaultValueConverter(TypeConverter typeConverter, Type sourceType, Type targetType,
                                           bool shouldConvertFrom, bool shouldConvertTo, DataBindEngine engine)
            : base(typeConverter, sourceType, targetType, shouldConvertFrom, shouldConvertTo, engine)
        {
        }

        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertFrom(o, _targetType, parameter as DependencyObject, culture);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertTo(o, _sourceType, parameter as DependencyObject, culture);
        }
    }

    #endregion TargetDefaultValueConverter

    #region SystemConvertConverter

    internal class SystemConvertConverter : IValueConverter
    {
        public SystemConvertConverter(Type sourceType, Type targetType)
        {
            _sourceType = sourceType;
            _targetType = targetType;
        }

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return System.Convert.ChangeType(o, _targetType, culture);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            object parsedValue = DefaultValueConverter.TryParse(o, _sourceType, culture);
            return (parsedValue != DependencyProperty.UnsetValue)
                        ? parsedValue
                        : System.Convert.ChangeType(o, _sourceType, culture);
        }

        // ASSUMPTION: sourceType != targetType
        public static bool CanConvert(Type sourceType, Type targetType)
        {
            // This assert is not Invariant.Assert because this will not cause
            // harm; It would just be odd.
            Debug.Assert(sourceType != targetType);

            // DateTime can only be converted to and from String type
            if (sourceType == typeof(DateTime))
                return (targetType == typeof(String));
            if (targetType == typeof(DateTime))
                return (sourceType == typeof(String));

            // Char can only be converted to a subset of supported types
            if (sourceType == typeof(Char))
                return CanConvertChar(targetType);
            if (targetType == typeof(Char))
                return CanConvertChar(sourceType);

            // Using nested loops is up to 40% more efficient than using one loop
            for (int i = 0; i < SupportedTypes.Length; ++i)
            {
                if (sourceType == SupportedTypes[i])
                {
                    ++i;    // assuming (sourceType != targetType), start at next type
                    for (; i < SupportedTypes.Length; ++i)
                    {
                        if (targetType == SupportedTypes[i])
                            return true;
                    }
                }
                else if (targetType == SupportedTypes[i])
                {
                    ++i;    // assuming (sourceType != targetType), start at next type
                    for (; i < SupportedTypes.Length; ++i)
                    {
                        if (sourceType == SupportedTypes[i])
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool CanConvertChar(Type type)
        {
            for (int i = 0; i < CharSupportedTypes.Length; ++i)
            {
                if (type == CharSupportedTypes[i])
                    return true;
            }
            return false;
        }

        Type _sourceType, _targetType;

        // list of types supported by System.Convert (from the SDK)
        static readonly Type[] SupportedTypes = {
            typeof(String),                             // put common types up front
            typeof(Int32),  typeof(Int64),  typeof(Single), typeof(Double),
            typeof(Decimal),typeof(Boolean),
            typeof(Byte),   typeof(Int16),
            typeof(UInt32), typeof(UInt64), typeof(UInt16), typeof(SByte),  // non-CLS compliant types
        };

        // list of types supported by System.Convert for Char Type(from the SDK)
        static readonly Type[] CharSupportedTypes = {
            typeof(String),                             // put common types up front
            typeof(Int32),  typeof(Int64),  typeof(Byte),   typeof(Int16),
            typeof(UInt32), typeof(UInt64), typeof(UInt16), typeof(SByte),  // non-CLS compliant types
        };
    }

    #endregion SystemConvertConverter

    #region ObjectConverter

    // BUG: 1109257 ObjectTargetConverter is not the best converter possible:
    // it'll use the Source type's system converter, but at conversion time,
    // the real target type's converter is another converter that can be tried.
    internal class ObjectTargetConverter : DefaultValueConverter, IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        public ObjectTargetConverter(Type sourceType, DataBindEngine engine) :
            base(null, sourceType, typeof(object),
                 true /* shouldConvertFrom */, false /* shouldConvertTo */, engine)
        {
        }

        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            // conversion from any type to object is easy
            return o;
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            // if types are compatible, just pass the value through
            if (o == null && !_sourceType.IsValueType)
                return o;

            if (o != null && _sourceType.IsAssignableFrom(o.GetType()))
                return o;

            // if source type is string, use String.Format (string's type converter doesn't
            // do it for us - boo!)
            if (_sourceType == typeof(String))
                return String.Format(culture, "{0}", o);

            // otherwise, use system converter
            EnsureConverter(_sourceType);
            return ConvertFrom(o, _sourceType, parameter as DependencyObject, culture);
        }
    }

    // BUG: 1109257 ObjectSourceConverter is not the best converter possible.
    internal class ObjectSourceConverter : DefaultValueConverter, IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        public ObjectSourceConverter(Type targetType, DataBindEngine engine) :
            base(null, typeof(object), targetType,
                 true /* shouldConvertFrom */, false /* shouldConvertTo */, engine)
        {
        }

        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            // if types are compatible, just pass the value through
            if ((o != null && _targetType.IsAssignableFrom(o.GetType())) ||
                (o == null && !_targetType.IsValueType))
                return o;

            // if target type is string, use String.Format (string's type converter doesn't
            // do it for us - boo!)
            if (_targetType == typeof(String))
                return String.Format(culture, "{0}", o);

            // otherwise, use system converter
            EnsureConverter(_targetType);
            return ConvertFrom(o, _targetType, parameter as DependencyObject, culture);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            // conversion from any type to object is easy
            return o;
        }
    }

    #endregion ObjectConverter

    #region ListSourceConverter

    internal class ListSourceConverter : IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            IList il = null;
            IListSource ils = o as IListSource;

            if (ils != null)
            {
                il = ils.GetList();
            }

            return il;
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    #endregion ListSourceConverter

    #region InterfaceConverter

    internal class InterfaceConverter : IValueConverter
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal InterfaceConverter(Type sourceType, Type targetType)
        {
            _sourceType = sourceType;
            _targetType = targetType;
        }

        //------------------------------------------------------
        //
        //  Interfaces (IValueConverter)
        //
        //------------------------------------------------------

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertTo(o, _targetType);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            return ConvertTo(o, _sourceType);
        }

        private object ConvertTo(object o, Type type)
        {
            return type.IsInstanceOfType(o) ? o : null;
        }

        Type _sourceType;
        Type _targetType;
    }

    #endregion InterfaceConverter


    // TypeDescriptor context to provide TypeConverters with the app's BaseUri
    internal class ValueConverterContext : ITypeDescriptorContext, IUriContext
    {
        // redirect to IUriContext service
        virtual public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IUriContext))
            {
                return this as IUriContext;
            }
            return null;
        }

        // call BaseUriHelper.GetBaseUri() if the target element is known.
        // It does a tree walk trying to find a IUriContext implementer or a root element which has BaseUri explicitly set
        // This get_BaseUri is only called from a TypeConverter which in turn
        // is called from one of our DefaultConverters in this source file.
        public Uri BaseUri
        {
            get
            {
                if (_cachedBaseUri == null)
                {
                    if (_targetElement != null)
                    {
                        // GetBaseUri looks for a optional BaseUriProperty attached DP.
                        // This can cause a re-entrancy if that BaseUri is also data bound.
                        // Ideally the BaseUri DP should be flagged as NotDataBindable but
                        // unfortunately that DP is a core DP and not aware of the framework metadata
                        //
                        // GetBaseUri can raise SecurityExceptions if e.g. the app doesn't have
                        // the correct FileIO permission.
                        // Any security exception is initially caught in BindingExpression.ConvertHelper/.ConvertBackHelper
                        // but then rethrown since it is a critical exception.
                        _cachedBaseUri = BaseUriHelper.GetBaseUri(_targetElement);
                    }
                    else
                    {
                        _cachedBaseUri = BaseUriHelper.BaseUri;
                    }
                }
                return _cachedBaseUri;
            }
            set { throw new NotSupportedException(); }
        }


        internal void SetTargetElement(DependencyObject target)
        {
            if (target != null)
                _nestingLevel++;
            else
            {
                if (_nestingLevel > 0)
                    _nestingLevel--;
            }
            Invariant.Assert((_nestingLevel <= 1), "illegal to recurse/reenter ValueConverterContext.SetTargetElement()");
            _targetElement = target;
            _cachedBaseUri = null;
        }

        internal bool IsInUse
        {
            get { return (_nestingLevel > 0); }
        }

        // empty default implementation of interface ITypeDescriptorContext
        public IContainer Container { get { return null; } }
        public object Instance { get { return null; } }
        public PropertyDescriptor PropertyDescriptor { get { return null; } }
        public void OnComponentChanged() { }
        public bool OnComponentChanging() { return false; }

        // fields
        private DependencyObject _targetElement;
        private int _nestingLevel;
        private Uri _cachedBaseUri;
    }
}

