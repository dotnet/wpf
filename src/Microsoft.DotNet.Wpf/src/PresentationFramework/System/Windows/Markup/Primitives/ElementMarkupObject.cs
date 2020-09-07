// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  MarkupObject and MarkupProperty implementation for 
//             DependencyObject
//
//
//  Class hierarchy in this file:
//
//      (MarkupObject)
//          ElementMarkupObject
//  
//      (MarkupProperty)
//          ElementPropertyBase
//              ElementObjectPropertyBase
//                  ElementProperty
//                  ElementPseudoPropertyBase
//                      ElementKey
//                      ElementConstructorArgument
//                      ElementItemsPseudoProperty
//                      ElementDictionaryItemsPseudoProperty
//          ElementStringValueProperty
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;

using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Documents;

namespace System.Windows.Markup.Primitives 
{
    /// <summary>
    /// An implementation of MarkupObject for DependencyObjects that works also for CLR only objects
    /// </summary>
    internal class ElementMarkupObject : MarkupObject
    {
        internal ElementMarkupObject(object instance, XamlDesignerSerializationManager manager)
        {
            Debug.Assert(instance != null);
            _instance = instance;
            _context = new ElementObjectContext(this, null);
            _manager = manager;
        }

        public override Type ObjectType
        {
            get { return _instance.GetType(); }
        }

        public override object Instance
        {
            get { return _instance; }
        }

        internal override IEnumerable<MarkupProperty> GetProperties(bool mapToConstructorArgs)
        {
            ValueSerializer valueSerializer = ValueSerializer.GetSerializerFor(ObjectType, Context);
            if (valueSerializer != null && valueSerializer.CanConvertToString(_instance, Context))
            {
                yield return new ElementStringValueProperty(this);
                if (_key != null)
                {
                    yield return _key;
                }
            }
            else 
            {
                Dictionary<string, string> constructorArguments = null;

                // if this markup item is the value of property in attribute form and the instance
                // it represents is a MarkupExtension, then we return any properties that are 
                // constructor arguments as ElementConstructorArgument markup properties
                if (mapToConstructorArgs && _instance is MarkupExtension)
                {
                    ParameterInfo[] parameters;
                    ICollection arguments;
                    if(TryGetConstructorInfoArguments(_instance, out parameters, out arguments))
                    {
                        int i = 0;
                        foreach (object value in arguments)
                        {
                            if (constructorArguments == null)
                            {
                                constructorArguments = new Dictionary<string, string>();
                            }
                            // store a list of the parameters that are constructor arguments
                            // so that we don't return those properties again
                            constructorArguments.Add(parameters[i].Name, parameters[i].Name);

                            yield return new ElementConstructorArgument(value, parameters[i++].ParameterType, this);
                        }
                    }
                }
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(_instance))
                {
                    DesignerSerializationVisibility visibility = descriptor.SerializationVisibility;

                    if (visibility != DesignerSerializationVisibility.Hidden &&
                        (!descriptor.IsReadOnly || visibility == DesignerSerializationVisibility.Content) &&
                        ShouldSerialize(descriptor, _instance, _manager))
                    {
                        if (visibility == DesignerSerializationVisibility.Content)
                        {
                            // Ensure the collection has content
                            object value = descriptor.GetValue(_instance);
                            if (value == null)
                            {
                                continue;
                            }
                            else
                            {
                                ICollection collection = value as ICollection;
                                if (collection != null && collection.Count < 1)
                                {
                                    continue;
                                }
                                IEnumerable enumerable = value as IEnumerable;
                                if (enumerable != null && !enumerable.GetEnumerator().MoveNext())
                                {
                                    continue;
                                }
                            }
                        }
                        if (constructorArguments != null)
                        {
                            ConstructorArgumentAttribute constructorArgumentAttribute = descriptor.Attributes[typeof(ConstructorArgumentAttribute)] as ConstructorArgumentAttribute;
                            if (constructorArgumentAttribute != null && constructorArguments.ContainsKey(constructorArgumentAttribute.ArgumentName))
                            {
                                // Skip this property, it has already been represented by a constructor parameter
                                continue;
                            }
                        }
                        yield return new ElementProperty(this, descriptor);
                    }
                }
                IDictionary dictionary = _instance as IDictionary;
                if (dictionary != null)
                {
                    yield return new ElementDictionaryItemsPseudoProperty(dictionary, typeof(IDictionary), this);
                }
                else
                {
                    IEnumerable enumerable = _instance as IEnumerable;
                    if (enumerable != null && enumerable.GetEnumerator().MoveNext())
                    {
                        yield return new ElementItemsPseudoProperty(enumerable, typeof(IEnumerable), this);
                    }
                }
                    
                if (_key != null)
                {
                    yield return _key;
                }
            }
        }

        public override AttributeCollection Attributes
        {
            get { return TypeDescriptor.GetAttributes(ObjectType); }
        }

        public override void AssignRootContext(IValueSerializerContext context)
        {
            _context = new ElementObjectContext(this, context);
        }

        internal IValueSerializerContext Context
        {
            get { return _context; }
        }

        internal XamlDesignerSerializationManager Manager
        {
            get { return _manager; }
        }

        internal void SetKey(ElementKey key)
        {
            _key = key;
        }

        private sealed class ElementObjectContext : ValueSerializerContextWrapper, IValueSerializerContext
        {
            ElementMarkupObject _object;

            public ElementObjectContext(ElementMarkupObject obj, IValueSerializerContext baseContext): base(baseContext)
            {
                _object = obj;
            }

            object ITypeDescriptorContext.Instance
            {
                get 
                {
                    return _object.Instance;
                }
            }
        }

        private static bool ShouldSerialize(PropertyDescriptor pd, object instance, XamlDesignerSerializationManager manager)
        {
            MethodInfo shouldSerializeMethod;
            object invokeInstance = instance;

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(pd);
            if (dpd != null && dpd.IsAttached) 
            {
                Type ownerType = dpd.DependencyProperty.OwnerType;
                string propertyName = dpd.DependencyProperty.Name;
                string keyName = propertyName + "!";
                if (!TryGetShouldSerializeMethod(new ShouldSerializeKey(ownerType, keyName), out shouldSerializeMethod)) 
                {
                    string methodName = "ShouldSerialize" + propertyName;
                    shouldSerializeMethod = ownerType.GetMethod(methodName, BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsObject, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = ownerType.GetMethod(methodName, BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsManager, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = ownerType.GetMethod(methodName, BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsMode, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = ownerType.GetMethod(methodName, BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsObjectManager, null);
                    if (shouldSerializeMethod != null && shouldSerializeMethod.ReturnType != typeof(bool))
                        shouldSerializeMethod = null;
                    CacheShouldSerializeMethod(new ShouldSerializeKey(ownerType, keyName), shouldSerializeMethod);
                }
                invokeInstance = null; // static method
            }
            else 
            {
                if (!TryGetShouldSerializeMethod(new ShouldSerializeKey(instance.GetType(), pd.Name), out shouldSerializeMethod)) 
                {
                    Type instanceType = instance.GetType();
                    string methodName = "ShouldSerialize" + pd.Name;
                    shouldSerializeMethod = instanceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsObject, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = instanceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsManager, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = instanceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsMode, null);
                    if (shouldSerializeMethod == null)
                        shouldSerializeMethod = instanceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.NonPublic | BindingFlags.Public, null, _shouldSerializeArgsObjectManager, null);
                    if (shouldSerializeMethod != null && shouldSerializeMethod.ReturnType != typeof(bool))
                        shouldSerializeMethod = null;
                    CacheShouldSerializeMethod(new ShouldSerializeKey(instanceType, pd.Name), shouldSerializeMethod);
                }
            }
            if (shouldSerializeMethod != null)
            {
                ParameterInfo[] parameters = shouldSerializeMethod.GetParameters();
                if (parameters != null)
                {
                    object[] args;
                    if (parameters.Length == 1)
                    {
                        if (parameters[0].ParameterType == typeof(DependencyObject))
                        {
                            args = new object[] { instance as DependencyObject };
                        }
                        else if (parameters[0].ParameterType == typeof(XamlWriterMode))
                        {
                            args = new object[] { manager.XamlWriterMode };
                        }
                        else
                        {
                            args = new object[] { manager };
                        }
                    }
                    else
                        args = new object[] { instance as DependencyObject, manager };
                    return (bool)shouldSerializeMethod.Invoke(invokeInstance, args);
                }
            }
            return pd.ShouldSerializeValue(instance);
        }

        private struct ShouldSerializeKey
        {
            public ShouldSerializeKey(Type type, string propertyName)
            {
                _type = type;
                _propertyName = propertyName;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ShouldSerializeKey)) return false;
                ShouldSerializeKey other = (ShouldSerializeKey)obj;
                return other._type == _type && other._propertyName == _propertyName;
            }


            /// <summary>
            ///     Forward to .Equals
            /// </summary>
            public static bool operator ==(ShouldSerializeKey key1, ShouldSerializeKey key2)
            {
                return key1.Equals(key2);
            }

            /// <summary>
            ///     Forward to .Equals
            /// </summary>
            public static bool operator !=(ShouldSerializeKey key1, ShouldSerializeKey key2)
            {
                return !(key1.Equals(key2));
            }

            public override int GetHashCode()
            {
                return _type.GetHashCode() ^ _propertyName.GetHashCode();
            }

            private Type _type;
            private string _propertyName;
        }

        private static bool TryGetShouldSerializeMethod(ShouldSerializeKey key, out MethodInfo methodInfo)
        {
            object value = _shouldSerializeCache[key];
            if (value == null || value == _shouldSerializeCacheLock)
            {
                // The _shouldSerializeCacheLock is used as a sentinal for null
                methodInfo = null;
                return value != null;
            }
            else
            {
                methodInfo = value as MethodInfo;
                return true;
            }
        }

        private static void CacheShouldSerializeMethod(ShouldSerializeKey key, MethodInfo methodInfo)
        {
            // The instance stored in _shouldSerializeCacheLock is used as a sentinal for null
            // The avoids having to perform two lookups in the Hashtable to detect a cached null value.
            object value = methodInfo == null ? _shouldSerializeCacheLock : methodInfo;
            lock (_shouldSerializeCacheLock)
            {
                _shouldSerializeCache[key] = value;
            }
        }
        
        private bool TryGetConstructorInfoArguments(object instance, out ParameterInfo[] parameters, out ICollection arguments)
        {        
            // Detect if the instance should be constructed using constructor parameters by
            // seeing if it can be converted to an instance descriptor that uses a constructor.
            TypeConverter converter = TypeDescriptor.GetConverter(instance);
            if (converter != null && converter.CanConvertTo(Context, typeof(InstanceDescriptor)))
            {
                InstanceDescriptor instanceDescriptor;
                try
                {
                    instanceDescriptor = converter.ConvertTo(_context, TypeConverterHelper.InvariantEnglishUS,
                        instance, typeof(InstanceDescriptor)) as InstanceDescriptor;
                }
                catch (InvalidOperationException)
                {
                    // If we get this just ignore the converter
                    instanceDescriptor = null;
                }
                catch (NotSupportedException)
                {
                    // If we get this just ignore the converter
                    instanceDescriptor = null;
                }
                
                if (instanceDescriptor != null)
                {            
                    ConstructorInfo ctorInfo = instanceDescriptor.MemberInfo as ConstructorInfo;
                    if (ctorInfo != null)
                    {
                        ParameterInfo[] ctorParameters = ctorInfo.GetParameters();
                        
                        if (ctorParameters != null && ctorParameters.Length == instanceDescriptor.Arguments.Count)
                        {
                            parameters = ctorParameters;
                            arguments = instanceDescriptor.Arguments;
                            return true;
                        }
                    }
                }
            }
            
            parameters = null;
            arguments = null;
            return false;
        }

        private static object _shouldSerializeCacheLock = new object();
        private static Hashtable _shouldSerializeCache = new Hashtable();
        private static Type[] _shouldSerializeArgsObject = new Type[] { typeof(DependencyObject) };
        private static Type[] _shouldSerializeArgsManager = new Type[] { typeof(XamlDesignerSerializationManager) };
        private static Type[] _shouldSerializeArgsMode = new Type[] { typeof(XamlWriterMode) };
        private static Type[] _shouldSerializeArgsObjectManager = new Type[] { typeof(DependencyObject), typeof(XamlDesignerSerializationManager) };
        private static Attribute[] _propertyAttributes = new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues) };

        private object _instance;
        private IValueSerializerContext _context;
        private ElementKey _key;
        private XamlDesignerSerializationManager _manager;
    }

    internal abstract class ElementPropertyBase : MarkupProperty
    {
        public ElementPropertyBase(XamlDesignerSerializationManager manager)
        {
            _manager = manager;
        }

        public override bool IsComposite
        {
            get
            {
                if (!_isCompositeCalculated)
                {
                    _isCompositeCalculated = true;
                    object value = Value;
                    
                    if (value == null)
                    {
                        _isComposite = true;
                    }
                    else if (value is string && PropertyType.IsAssignableFrom(typeof(object)))
                    {
                        _isComposite = false;
                    }
                    else if (value is MarkupExtension)
                    {
                        _isComposite = true;
                    }
                    else
                    {
                        _isComposite = !CanConvertToString(value);
                    }
                }
                return _isComposite;
            }
        }

        public override string StringValue
        {
            get
            {
                if (IsComposite)
                {
                    return String.Empty;
                }
                
                object value = Value;
                string stringValue = value as string;
                if (stringValue != null)
                {
                    return stringValue;
                }

                ValueSerializer serializer = GetValueSerializer();
                if (serializer == null)
                {
                    return String.Empty;
                }
                
                return serializer.ConvertToString(value, Context);
            }
        }

        public override IEnumerable<MarkupObject> Items
        {
            get
            {
                object value = Value;
                if (value != null)
                {
                    if (PropertyDescriptor != null && (PropertyDescriptor.IsReadOnly ||
                        (!PropertyIsAttached(PropertyDescriptor) && PropertyType == value.GetType() &&
                            // These types the serializer will synthesize a start element when
                            // reading them in
                            (typeof(IList).IsAssignableFrom(PropertyType) ||
                            typeof(IDictionary).IsAssignableFrom(PropertyType) ||
                            typeof(Freezable).IsAssignableFrom(PropertyType) ||
                            typeof(FrameworkElementFactory).IsAssignableFrom(PropertyType))) &&
                            HasNoSerializableProperties(value) &&
                            !IsEmpty(value)))
                    {
                        IDictionary dictionary = value as IDictionary;
                        if (dictionary != null)
                        {
                            Type keyType = GetDictionaryKeyType(dictionary);

                            // Attempt to emit the contents of the dictionary in a more deterministic
                            // order. This is not guarenteed to be deterministic because it uses ToString
                            // which is not guarenteed to be unique for each value. Keys with non-unique
                            // ToString() values will not be emitted deterministically.
                            DictionaryEntry[] entries = new DictionaryEntry[dictionary.Count];
                            dictionary.CopyTo(entries, 0);
                            Array.Sort(entries, delegate(DictionaryEntry one, DictionaryEntry two)
                            {
                                return String.Compare(one.Key.ToString(), two.Key.ToString());
                            });
                            foreach (DictionaryEntry entry in entries)
                            {
                                ElementMarkupObject subItem 
                                    = new ElementMarkupObject(
                                        ElementProperty.CheckForMarkupExtension(typeof(Object), entry.Value, Context, false), Manager);
                                
                                subItem.SetKey(new ElementKey(entry.Key, keyType, subItem));
                                yield return subItem;
                            }
                        }
                        else
                        {
                            IEnumerable items = value as IEnumerable;
                            if (items != null)
                            {
                                foreach (object o in items)
                                {
                                    MarkupObject subItem 
                                        = new ElementMarkupObject(
                                            ElementProperty.CheckForMarkupExtension(typeof(Object), o, Context, false), Manager);
                                    
                                    yield return subItem;
                                }
                            }
                            else
                            {
                                if (PropertyType == typeof(FrameworkElementFactory) && value is FrameworkElementFactory)
                                {
                                    MarkupObject subItem = new FrameworkElementFactoryMarkupObject(value as FrameworkElementFactory, Manager);
                                    yield return subItem;
                                }
                                else
                                {
                                    MarkupObject subItem 
                                        = new ElementMarkupObject(
                                            ElementProperty.CheckForMarkupExtension(typeof(Object), value, Context, true), Manager);
                                    
                                    yield return subItem;
                                }
                            }
                        }
                    }
                    else
                    {
                        MarkupObject subItem 
                            = new ElementMarkupObject(
                                ElementProperty.CheckForMarkupExtension(typeof(Object), value, Context, true), Manager);
                        
                        yield return subItem;
                    }
                }
                else
                {
                    MarkupObject subItem = new ElementMarkupObject(new System.Windows.Markup.NullExtension(), Manager);
                    yield return subItem;
                }
            }
        }

        private bool PropertyIsAttached(PropertyDescriptor descriptor) 
        {
            DependencyPropertyDescriptor dependencyPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(PropertyDescriptor);
            return dependencyPropertyDescriptor != null && dependencyPropertyDescriptor.IsAttached;
        }

        private bool HasNoSerializableProperties(object value)
        {
            if (value is FrameworkElementFactory)
                return true;
            ElementMarkupObject item = new ElementMarkupObject(value, Manager);
            foreach (MarkupProperty property in item.Properties)
            {
                if (!property.IsContent)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsEmpty(object value)
        {
            IEnumerable collection = value as IEnumerable;
            if (collection != null)
            {
                foreach (object o in collection)
                    return false;
                return true;
            }
            return false;
        }

        protected IValueSerializerContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = new ElementPropertyContext(this, GetItemContext());
                }
                return _context;
            }
        }

        internal XamlDesignerSerializationManager Manager
        {
            get { return _manager; }
        }

        static readonly List<Type> EmptyTypes = new List<Type>();

        public override IEnumerable<Type> TypeReferences
        {
            get
            {
                ValueSerializer serializer = GetValueSerializer();
                if (serializer == null)
                    return EmptyTypes;
                else
                    return serializer.TypeReferences(Value, Context);
            }
        }

        protected bool CanConvertToString( object value )
        {
            // See if this value can be converted to a string by a value serializer or type converter
            
            if( value == null )
            {
                return false;
            }
            
            ValueSerializer serializer = GetValueSerializer();
            return serializer != null && serializer.CanConvertToString(value, Context);
        }

        protected abstract IValueSerializerContext GetItemContext();
        protected abstract Type GetObjectType();

        private sealed class ElementPropertyContext : ValueSerializerContextWrapper, IValueSerializerContext
        {
            ElementPropertyBase _property;

            public ElementPropertyContext(ElementPropertyBase property, IValueSerializerContext baseContext)
                : base(baseContext)
            {
                _property = property;
            }

            PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
            {
                get
                {
                    return _property.PropertyDescriptor;
                }
            }
        }

        private ValueSerializer GetValueSerializer()
        {
            PropertyDescriptor descriptor = this.PropertyDescriptor;
            if (descriptor == null)
            {
                DependencyProperty property = this.DependencyProperty;
                if (property != null)
                    descriptor = DependencyPropertyDescriptor.FromProperty(property, GetObjectType());
            }
            if (descriptor != null)
                return ValueSerializer.GetSerializerFor(descriptor, GetItemContext());
            else
                return ValueSerializer.GetSerializerFor(PropertyType, GetItemContext());
        }

        private static Dictionary<Type, Type> _keyTypeMap;

        private static Type GetDictionaryKeyType(IDictionary value)
        {
            Type type = value.GetType();
            Type result;

            if (_keyTypeMap == null)
                _keyTypeMap = new Dictionary<Type, Type>();
            if (!_keyTypeMap.TryGetValue(type, out result))
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        Type genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
                        if (genericTypeDefinition == typeof(System.Collections.Generic.IDictionary<,>))
                            return interfaceType.GetGenericArguments()[0];
                    }
                }
                result = typeof(object);
                _keyTypeMap[type] = result;
            }
            return result;
        }

        private bool _isComposite;
        private bool _isCompositeCalculated;
        private IValueSerializerContext _context;
        private XamlDesignerSerializationManager _manager;
    }

    internal abstract class ElementObjectPropertyBase : ElementPropertyBase
    {
        protected ElementObjectPropertyBase(ElementMarkupObject obj): base(obj.Manager)
        {
            Debug.Assert(obj != null);
            _object = obj;
        }

        protected override IValueSerializerContext GetItemContext()
        {
            return _object.Context;
        }

        protected override Type GetObjectType()
        {
            return _object.ObjectType;
        }

        protected readonly ElementMarkupObject _object;
    }

    /// <summary>
    /// An implementation of MarkupProperty for DependencyObjects that works also for CLR only objects
    /// </summary>
    internal class ElementProperty : ElementObjectPropertyBase
    {
        internal ElementProperty(ElementMarkupObject obj, PropertyDescriptor descriptor) : base(obj)
        {
            Debug.Assert(descriptor != null);
            _descriptor = descriptor;
        }

        public override string Name
        {
            get { return _descriptor.Name; }
        }

        public override Type PropertyType
        {
            get { return _descriptor.PropertyType; }
        }

        public override PropertyDescriptor PropertyDescriptor
        {
            get { return _descriptor; }
        }

        public override bool IsAttached
        {
            get 
            {
                UpdateDependencyProperty();
                return _isAttached; 
            }
        }

        public override DependencyProperty DependencyProperty
        {
            get
            {
                UpdateDependencyProperty();
                return _dependencyProperty;
            }
        }

        public override object Value
        {
            get {
                object value;

                DependencyProperty DP = DependencyProperty;

                // For DPs, we use ReadLocalValue to find the property, and handle
                // unset values.
                if (DP != null)
                {
                    DependencyObject DO = _object.Instance as DependencyObject;
                    Debug.Assert(DO != null);

                    // get the local value of the DP
                    value = DO.ReadLocalValue(DP);

                    // Expressions require special handling

                    Expression expression = value as Expression;
                    if (expression != null)
                    {
                        // Special-case: Expressions always get converted to an ME if 
                        // possible (and requested), and get de-referenced otherwise.
                        
                        TypeConverter converter = TypeDescriptor.GetConverter(value);
                        
                        if (Manager.XamlWriterMode == XamlWriterMode.Expression &&
                            converter.CanConvertTo(typeof(MarkupExtension)))
                        {
                            value = converter.ConvertTo(expression, typeof(MarkupExtension));
                        }
                        else
                        {
                            value = expression.GetValue(DO, DP);
                        }
                    }

                    // If the expression gave us UnsetValue, fall back to the default.
                    if (value == DependencyProperty.UnsetValue)
                    {
                        value = DP.GetDefaultValue(DO.DependencyObjectType);
                    }
                }

                // If this is the VisualTree property of a template, we have to do special processing.
                // Long-term plan is to replace this with an attribute on the type, that tells us how
                // to serialize its content.
                else if ((Name == "Template" || Name == "VisualTree")
                         &&
                         Context.Instance is FrameworkTemplate
                         &&
                         (Context.Instance as FrameworkTemplate).HasContent )
                {
                    // Instantiate the template, in un-optimized form.  This makes the template content
                    // load as if it were loaded from xaml outside a template.  The only exception is that
                    // TemplateBinding's will show up as a TemplateBindingExtension.
                    
                    value = (Context.Instance as FrameworkTemplate).LoadContent();
                }

                // Otherwise, get the value from the property descriptor.                
                else
                {
                    value = _descriptor.GetValue(_object.Instance);
                }


                // Convert the resulting value from above into a MarkupExtension, if necessary/possible,
                // but only if there is no value serializer; if the type supports conversion to both
                // string and ME, we give preference to the string conversion.

                if( !(value is MarkupExtension) && !CanConvertToString(value) )
                {
                    value = CheckForMarkupExtension(PropertyType, value, Context, true);
                }

                return value;
            }
        }

        public override AttributeCollection Attributes
        {
            get { return _descriptor.Attributes; }
        }

        private void UpdateDependencyProperty()
        {
            if (!_isDependencyPropertyCached)
            {
                DependencyPropertyDescriptor dpDesc = DependencyPropertyDescriptor.FromProperty(_descriptor);
                if (dpDesc != null) 
                {
                    _dependencyProperty = dpDesc.DependencyProperty;
                    _isAttached = dpDesc.IsAttached;
                }
                _isDependencyPropertyCached = true;
            }
        }

        //
        //  Check for values that can be converted into markup extensions, either because
        //  they are a well known type ((null, arrays, enums, and types), or because they
        //  can be type converted into an ME.
        //
        
        internal static object CheckForMarkupExtension(
                                    Type propertyType,
                                    object value, 
                                    IValueSerializerContext context, 
                                    bool convertEnums)
        {
            // null => NullExtension
            
            if (value == null)
            {
                return new NullExtension();
            }

            // See if the type has a type converter that can create a MarkupExtension
            // (Have to do this after the null check so that GetConverter doesn't get
            // an invalid argument.)

            TypeConverter converter = TypeDescriptor.GetConverter(value);
            if (converter.CanConvertTo(context, typeof(MarkupExtension)))
            {
                // The type provides a converter that creates a MarkupExtension.
                return converter.ConvertTo(context, TypeConverterHelper.InvariantEnglishUS, value, typeof(MarkupExtension));
            }

            // System.Type => TypeExtension
            
            Type type = value as Type;
            if (type != null)
            {
                // If the property is declared to be a type already, we don't need to convert it
                // into {x:Type} syntax.
                
                if( propertyType == typeof(Type) )
                {
                    return value;
                }

                return new TypeExtension(type);
            }

            // Enums => StaticExtension
            
            if (convertEnums)
            {
                Enum enumValue = value as Enum;
                if (enumValue != null)
                {
                    ValueSerializer typeSerializer = context.GetValueSerializerFor(typeof(Type));
                    Debug.Assert(typeSerializer != null, "typeSerializer for Enum was null");
                    string typeName = typeSerializer.ConvertToString(enumValue.GetType(), context);
                    return new StaticExtension(typeName + "." + enumValue.ToString());
                }
            }

            // Arrays => ArrayExtension
            
            Array array = value as Array;
            if (array != null)
            {
                return new ArrayExtension(array);
            }

            // Otherwise, value is unchanged.
            
            return value;
        }

        private PropertyDescriptor _descriptor;
        private bool _isDependencyPropertyCached;
        private DependencyProperty _dependencyProperty;
        private bool _isAttached;
    }

    /// <summary>
    /// Pseudo-property for strings passed to a type converter
    /// </summary>
    internal class ElementStringValueProperty : MarkupProperty
    {
        internal ElementStringValueProperty(ElementMarkupObject obj)
        {
            _object = obj;
        }

        public override string Name
        {
            get { return "StringValue"; }
        }

        public override Type PropertyType
        {
            get { return typeof(string); }
        }

        public override bool IsValueAsString
        {
            get { return true; }
        }

        public override object Value
        {
            get { return StringValue; }
        }

        public override string StringValue
        {
            get
            {
                ValueSerializer serializer = ValueSerializer.GetSerializerFor(_object.ObjectType, _object.Context);
                Debug.Assert(serializer != null && serializer.CanConvertToString(_object.Instance, _object.Context), 
                    "StringValue property value created for a type that cannot be converted to string");
                return serializer.ConvertToString(_object.Instance, _object.Context);
            }
        }

        public override IEnumerable<MarkupObject> Items
        {
            get
            {
                Debug.WriteLine("ElementMarkupObject.Items is not implemented");
                return null;
            }
        }

        public override AttributeCollection Attributes
        {
            get { return AttributeCollection.Empty; }
        }

        public override IEnumerable<Type> TypeReferences
        {
            get
            {
                ValueSerializer serializer = ValueSerializer.GetSerializerFor(_object.ObjectType, _object.Context);
                Debug.Assert(serializer != null && serializer.CanConvertToString(_object.Instance, _object.Context), 
                    "StringValue property value created for a type that cannot be converted to string");
                return serializer.TypeReferences(_object.Instance, _object.Context);
            }
        }

        private ElementMarkupObject _object;
    }

    /// <summary>
    /// Pseudo-property for the key of a dictionary element
    /// </summary>
    internal abstract class ElementPseudoPropertyBase : ElementObjectPropertyBase
    {
        internal ElementPseudoPropertyBase(object value, Type type, ElementMarkupObject obj) : base(obj)
        {
            _value = value;
            _type = type;
        }

        public override Type PropertyType
        {
            get { return _type; }
        }

        public override object Value
        {
            get { return ElementProperty.CheckForMarkupExtension(PropertyType, _value, Context, true /*convertEnums*/); }
        }

        public override AttributeCollection Attributes
        {
            get { return AttributeCollection.Empty; }
        }

        public override IEnumerable<Type> TypeReferences
        {
            get { return Array.Empty<Type>(); }
        }

        private object _value;
        private Type _type;
    }

    /// <summary>
    /// Pseudo-property for the key of a dictionary element
    /// </summary>
    internal class ElementKey : ElementPseudoPropertyBase
    {
        internal ElementKey(object value, Type type, ElementMarkupObject obj) : base(value, type, obj) { }

        public override string Name
        {
            get { return "Key"; }
        }

        public override bool IsKey
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Pseudo-property for constructor arguments
    /// </summary>
    internal class ElementConstructorArgument : ElementPseudoPropertyBase
    {
        internal ElementConstructorArgument(object value, Type type, ElementMarkupObject obj) : base(value, type, obj) { }

        public override string Name
        {
            get { return "Argument"; }
        }

        public override bool IsConstructorArgument
        {
            get { return true;  }
        }
    }

    /// <summary>
    /// Pseudo-property for IEnumerable implementations
    /// </summary>
    internal class ElementItemsPseudoProperty : ElementPseudoPropertyBase
    {
        internal ElementItemsPseudoProperty(IEnumerable value, Type type, ElementMarkupObject obj) : base(value, type, obj) 
        {
            _value = value;
        }

        public override string Name
        {
            get { return "Items"; }
        }

        public override bool IsContent
        {
            get { return true; }
        }

        public override bool IsComposite
        {
            get { return true; }
        }

        public override IEnumerable<MarkupObject> Items
        {
            get 
            {
                foreach (object instance in _value)
                {
                    yield return new ElementMarkupObject(instance, Manager);
                }
            }
        }

        IEnumerable _value;
    }

    /// <summary>
    /// Pseudo-property for IDictionary implementation
    /// </summary>
    internal class ElementDictionaryItemsPseudoProperty : ElementPseudoPropertyBase
    {
        internal ElementDictionaryItemsPseudoProperty(IDictionary value, Type type, ElementMarkupObject obj) : base(value, type, obj) 
        {
            _value = value;
        }

        public override string Name
        {
            get { return "Entries"; }
        }

        public override bool IsContent
        {
            get { return true; }
        }

        public override bool IsComposite
        {
            get { return true; }
        }

        public override IEnumerable<MarkupObject> Items
        {
            get
            {
                foreach (DictionaryEntry entry in _value)
                {
                    ElementMarkupObject item = new ElementMarkupObject(entry.Value, Manager);
                    item.SetKey(new ElementKey(entry.Key, typeof(object), item));
                    yield return item;
                }
            }
        }

        IDictionary _value;
    }

    internal class ValueSerializerContextWrapper : IValueSerializerContext
    {
        IValueSerializerContext _baseContext;

        public ValueSerializerContextWrapper(IValueSerializerContext baseContext)
        {
            _baseContext = baseContext;
        }

        public ValueSerializer GetValueSerializerFor(PropertyDescriptor descriptor)
        {
            if (_baseContext != null)
                return _baseContext.GetValueSerializerFor(descriptor);
            else
                return null;
        }

        public ValueSerializer GetValueSerializerFor(Type type)
        {
            if (_baseContext != null)
                return _baseContext.GetValueSerializerFor(type);
            else
                return null;
        }

        public IContainer Container
        {
            get
            {
                if (_baseContext != null)
                    return _baseContext.Container;
                else
                    return null;
            }
        }

        public object Instance
        {
            get
            {
                if (_baseContext != null)
                    return _baseContext.Instance;
                else
                    return null;
            }
        }

        public void OnComponentChanged()
        {
            if (_baseContext != null)
                _baseContext.OnComponentChanged();
        }

        public bool OnComponentChanging()
        {
            if (_baseContext != null)
                return _baseContext.OnComponentChanging();
            else
                return true;
        }

        public PropertyDescriptor PropertyDescriptor
        {
            get 
            {
                if (_baseContext != null)
                    return _baseContext.PropertyDescriptor;
                else
                    return null;
            }
        }

        public object GetService(Type serviceType)
        {
            if (_baseContext != null)
                return _baseContext.GetService(serviceType);
            else
                return null;
        }
    }
}
