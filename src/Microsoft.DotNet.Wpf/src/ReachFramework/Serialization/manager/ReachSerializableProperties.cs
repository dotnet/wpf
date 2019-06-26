// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                                      
    Abstract:
        Defining some classes encapsulating some information about 
        the properties contained within an object and used in the 
        serialization process 
                                                                     
--*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;

namespace System.Windows.Xps.Serialization
{
    internal class SerializablePropertyCollection :
                   IEnumerable,
                   IEnumerator
    {
        #region Contstructor

        /// <summary>
        /// Intantiate a SerializablePropertyCollection
        /// </summary>
        internal 
        SerializablePropertyCollection(
            PackageSerializationManager manager, 
            object                      targetObject
            )
        {
            this._simplePropertyCollection  = null;
            this._complexPropertyCollection = null;
            this._simplePropertiesIndex     = -1;
            this._complexPropertiesIndex    = -1;
            this._queueEnumerator           = null;
            this._serializationManager      = manager;
            this._target                    = targetObject;
            this._isSimpleMode              = true;
            
            Initialize(manager, _target);
        }

        #endregion Constructor

        #region IEnumerable implementation

        /// <summary>
        /// return the Enumerator for the collection
        /// </summary>
        public
        IEnumerator 
        GetEnumerator(
            )
        {
            return this;
        }

        #endregion IEnumerable implementation

        #region IEnumerator implementation

        /// <summary>
        /// Current object in the collection
        /// </summary>
        public 
        object
        Current
        {
            get 
            {
                return _queueEnumerator.Current;
            }
        }

        /// <summary>
        /// Move to the next PropertyContext
        /// </summary>
        public 
        bool 
        MoveNext()
        {
            bool canMoveToNext = false;

            if(_isSimpleMode)
            {
                if(_simplePropertiesIndex == -1)
                {
                    _queueEnumerator = _simplePropertyCollection.GetEnumerator();
                }
                if(_simplePropertiesIndex < _simplePropertyCollection.Count-1)
                {
                    _simplePropertiesIndex++;
                    _queueEnumerator.MoveNext();
                    canMoveToNext = true;
                }
                else
                {
                    _isSimpleMode = false;
                }
            }

            if(!_isSimpleMode)
            {
                if(_complexPropertiesIndex == -1)
                {
                    _queueEnumerator = _complexPropertyCollection.GetEnumerator();
                }
                if(_complexPropertiesIndex < _complexPropertyCollection.Count-1)
                {
                    _complexPropertiesIndex++;
                    _queueEnumerator.MoveNext();
                    canMoveToNext = true;
                }
            }

            return canMoveToNext;
        }

        /// <summary>
        /// Reset all necessary indices and pointers to 
        /// the beginning of the collection
        /// </summary>
        public
        void 
        Reset(
            )
        {
            this._simplePropertiesIndex     = -1;
            this._complexPropertiesIndex    = -1;
            this._queueEnumerator           = null;
            this._isSimpleMode              = true;
        }
        
        #endregion IEnumerator implementation

        #region Internal Methods

        /// <summary>
        /// Initialize this instance
        /// </summary>
        internal 
        void 
        Initialize(
            PackageSerializationManager serializationManager, 
            object                      targetObject
            )
        {
            this._serializationManager = serializationManager;
            this._target               = targetObject;

            //
            // Collect all serializable properties on the
            // current object instance. Those could be
            // o clr properties
            // o dependency properties
            //
            if (_simplePropertyCollection == null)
            {
                _simplePropertyCollection = new Queue();
            }

            if (_complexPropertyCollection == null)
            {
                _complexPropertyCollection = new Queue();
            }

            //
            // Collecting information about the CLR properties
            //
            InitializeSerializableClrProperties();
            
            //
            // Now that we are done with the clr serializable properties we need to 
            // iterate through locally set dependency properties on this instance
            // that have not been serialized already. 
            // Note: Dependency Properties can only be set on Dependency Objects
            //
            InitializeSerializableDependencyProperties();

            //
            // Reset the enumerator 
            // to begin enumerating 
            // from the start of the 
            // properties collection            
            //
            Reset();
        }

        /// <summary>
        /// Clear all references for reuse without
        /// instantiation. This is a way of caching
        /// already instantiated through recycling
        /// for future use.
        /// </summary>
        internal void Clear()
        {
            this._simplePropertyCollection.Clear();
            this._complexPropertyCollection.Clear();
            this._simplePropertiesIndex     = -1;
            this._complexPropertiesIndex    = -1;
            this._queueEnumerator           = null;
            this._serializationManager      = null;
            this._target                    = null;
            this._isSimpleMode              = true;
        }

        #endregion Internal Methods
        
        #region Private Methods

        /// <summary>
        /// Fill out the portion of the collection which
        /// points out to the CLR properties
        /// </summary>
        private
        void
        InitializeSerializableClrProperties(
            )
        {
            TypePropertyCache[] clrProperties = _serializationManager.CacheManager.GetClrSerializableProperties(_target);

            if(clrProperties!=null)
            {
                for (int indexInClrSerializableProperties=0;
                     indexInClrSerializableProperties < clrProperties.Length; 
                     indexInClrSerializableProperties++)
                {
                    TypePropertyCache propertyCache = clrProperties[indexInClrSerializableProperties];

                    //
                    // Create SerializablePropertyContext out of the cache retrieved
                    //
                    SerializablePropertyContext propertyContext = new SerializablePropertyContext(_target, 
                                                                                                  propertyCache);

                    propertyContext.Name = propertyContext.TypePropertyCache.PropertyInfo.Name;

                    //
                    // We have to differentiate between simple properties and complex properties.
                    // o Simple properties would be considered as attributes within the markup and
                    //   would not require new writers
                    // o Complex properties would be considered elements within the markup and might
                    //   require a new writer
                    //
                    if(propertyContext.IsComplexProperty(_serializationManager))
                    {
                        propertyContext.IsComplex = true;
                        _complexPropertyCollection.Enqueue(propertyContext);
                    }
                    else
                    {
                        propertyContext.IsComplex = false;
                        _simplePropertyCollection.Enqueue(propertyContext);
                    }                    
                }
            }
        }

        /// <summary>
        /// Fill out the portion of the collection
        /// that points out to the dependency properties
        /// </summary>
        private
        void
        InitializeSerializableDependencyProperties(
            )
        {
            TypeDependencyPropertyCache[] dependencyProperties = _serializationManager.
                                                                 CacheManager.
                                                                 GetSerializableDependencyProperties(_target);

            if(dependencyProperties!=null)
            {
                for (int indexInSerializableDependencyProperties=0;
                     indexInSerializableDependencyProperties < dependencyProperties.Length; 
                     indexInSerializableDependencyProperties++)
                {
                    TypeDependencyPropertyCache dependencyPropertyCache = 
                    dependencyProperties[indexInSerializableDependencyProperties];

                    //
                    // Create SerializableDependencyPropertyContext out of the cache retrieved
                    //
                    SerializableDependencyPropertyContext dependencyPropertyContext = 
                    new SerializableDependencyPropertyContext(_target, 
                                                              dependencyPropertyCache);

                    dependencyPropertyContext.Name = 
                    ((DependencyProperty)((TypeDependencyPropertyCache)dependencyPropertyContext.TypePropertyCache).
                    DependencyProperty).Name;

                    //
                    // We have to differentiate between simple properties and complex properties.
                    // o Simple properties would be considered as attributes within the markup and
                    //   would not require new writers
                    // o Complex properties would be considered elements within the markup and might
                    //   require a new writer
                    //
                    if(dependencyPropertyContext.IsComplexProperty(_serializationManager))
                    {
                        dependencyPropertyContext.IsComplex = true;
                        _complexPropertyCollection.Enqueue(dependencyPropertyContext);
                    }
                    else
                    {
                        dependencyPropertyContext.IsComplex = false;
                        _simplePropertyCollection.Enqueue(dependencyPropertyContext);
                    }                    
                }
            }
        }

        
        #endregion Private Methods
        
        #region Private Data

        private 
        PackageSerializationManager _serializationManager;
        private 
        object                      _target;
        private 
        bool                        _isSimpleMode;
        private                 
        int                         _simplePropertiesIndex;
        private 
        Queue                       _simplePropertyCollection;
        private 
        int                         _complexPropertiesIndex;
        private 
        Queue                       _complexPropertyCollection;
        private 
        IEnumerator                 _queueEnumerator;

        #endregion Private Data
    };


    /// <summary>
    /// A class defining a context for the serializable CLR property
    /// </summary>
    internal class SerializablePropertyContext : 
                   BasicContext
    {
        #region Constructor

        /// <summary>
        /// Constructor for SerializablePropertyContext
        /// </summary>
        public 
        SerializablePropertyContext(
            string            name, 
            string            prefix, 
            object            target, 
            TypePropertyCache propertyCache) : 
        base(name, prefix)
        {
            //
            // Validate Input Arguments
            //
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if(propertyCache == null)
            {
                throw new ArgumentNullException("propertyCache");
            }

            this._targetObject = target;
            this._propertyInfo = propertyCache;
            this._isComplex    = false;
        }

        /// <summary>
        /// Constructor for SerializablePropertyContext
        /// </summary>
        internal 
        SerializablePropertyContext(
            object            target, 
            TypePropertyCache propertyCache) : 
        base()
        {
            //
            // Validate Input Arguments
            //
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if(propertyCache == null)
            {
                throw new ArgumentNullException("propertyCache");
            }

            _targetObject   = target;
            _propertyInfo   = propertyCache;
            this._isComplex = false;
        }

        #endregion Constructor
        
        #region Public Methods

        /// <summary>
        /// Detect whether it is a complex property or not.
        /// </summary>
        virtual
        public
        bool
        IsComplexProperty(
            PackageSerializationManager serializationManager
            )
        {
            bool isComplex = false;

            //
            // Null property value is always serialized 
            // in simple attribute="*null" notation
            //
            if (Value != null)
            {
                //
                // If the property has a DesignerSerializationOptions.SerializeAsAttribute 
                // then obviously we do not use complex notation
                //
                if(!(DesignerSerializationOptionsAttribute != null && 
                     (DesignerSerializationOptionsAttribute.DesignerSerializationOptions == 
                      DesignerSerializationOptions.SerializeAsAttribute)))

                {
                    //
                    // String space preservation is honoured by System.Xml only for contents of a tag not within 
                    // an attribute value. Hence we always emit strings as content within a tag
                    //
                    Type valueType = Value.GetType();

                    if (valueType == typeof(string) && 
                        ((string)Value) != string.Empty)
                    {
                        isComplex = true;
                    }
                    else
                    {
                        bool canConvert;
                        isComplex = IsComplexValue(serializationManager,
                                                   out canConvert);
                    }
                }
            }

            return isComplex;
        }

        virtual
        public
        bool 
        IsComplexValue(
                PackageSerializationManager manager, 
            out bool                        canConvert
            )
        {
            bool isComplex = true;

            canConvert = true;
            
            if(SerializerType!=null)
            {
                isComplex = true;
            }
            else
            {
                TypeConverter converter = this.TypeConverter;
                
                canConvert = converter.CanConvertTo(null,typeof(string)) &&
                             converter.CanConvertFrom(null,typeof(string));

                if(canConvert)
                {
                    isComplex = false;
                }
            }

            return isComplex;
        }

        #endregion Public Methods
        
        #region Public Properties

        /// <summary>
        /// Qyery the Target Object
        /// </summary>
        public
        object 
        TargetObject
        {
            get 
            { 
                return _targetObject; 
            }
        }
        
        /// <summary>
        /// Qyery the CLR propertyInfo structure of this property
        /// </summary>
        public 
        PropertyInfo 
        PropertyInfo
        {
            get 
            { 
                PropertyInfo info = null;

                if (_propertyInfo != null)
                {
                    info = (PropertyInfo)_propertyInfo.PropertyInfo;
                }
                
                return info; 
            }
        }

        // <summary>
        // Query / Set whether the property is Complex / Simple
        // </summary>
        public
        bool
        IsComplex
        {
            get
            {
                return _isComplex;
            }

            set
            {
                _isComplex = value;
            }
        }


        /// <summary>
        /// Query the visibility of the property
        /// </summary>
        public
        DesignerSerializationVisibility 
        Visibility
        {
            get 
            { 
                DesignerSerializationVisibility visibility = DesignerSerializationVisibility.Visible;

                if (_propertyInfo != null)
                {
                    visibility = _propertyInfo.Visibility;
                }
                
                return visibility; 
            }
        }

        /// <summary>
        /// Queries the Serializer type used to serialize this
        /// property if it happens to be a complex property
        /// </summary>
        public
        Type 
        SerializerType
        {
            get 
            { 
                Type type = null;

                if (_propertyInfo != null)
                {
                    type =  _propertyInfo.SerializerTypeForProperty;
                }
                
                return type; 
            }
        }

        /// <summary>
        /// Queries the TypeConverter used to convert this property
        /// to some equivalent string
        /// </summary>
        public
        TypeConverter 
        TypeConverter
        {
            get 
            { 
                TypeConverter converter = null;

                if (_propertyInfo != null)
                {
                    converter = _propertyInfo.TypeConverterForProperty;
                }
                
                return converter; 
            }
        }

        /// <summary>
        /// Query the default value attribute
        /// </summary>
        public
        DefaultValueAttribute
        DefaultValueAttribute
        {
            get 
            { 
                DefaultValueAttribute defValAttr = null;

                if (_propertyInfo != null)
                {
                    defValAttr = _propertyInfo.DefaultValueAttr;
                }
                
                return defValAttr; 
            }
        }

        /// <summary>
        /// Query the serialization attributes
        /// </summary>
        public
        DesignerSerializationOptionsAttribute
        DesignerSerializationOptionsAttribute
        {
            get 
            { 
                DesignerSerializationOptionsAttribute designerSerFlagAttr = null;

                if (_propertyInfo != null)
                {
                    designerSerFlagAttr = _propertyInfo.DesignerSerializationOptionsAttr;
                }
                
                return designerSerFlagAttr; 
            }
        }


        /// <summary>
        /// Query whether this is a read only or a read/write property    
        /// </summary>
        public
        bool 
        IsReadOnly
        {
            get 
            { 
                bool isReadOnly = false;

                if ( (_propertyInfo != null) &&
                     (((PropertyInfo)_propertyInfo.PropertyInfo) != null) )
                {
                    isReadOnly = !((PropertyInfo)_propertyInfo.PropertyInfo).CanWrite;
                }
                
                return isReadOnly; 
            }
        }

        /// <summary>
        /// Query / set the value of this property
        /// </summary>
        public 
        object 
        Value
        {
            get 
            { 
                return _propertyInfo.PropertyValue; 
            }

            set 
            {
                 _propertyInfo.PropertyValue = value; 
            }
        }

        /// <summary>
        /// This is the cache information about the property. Each
        /// property discovered is saved for performance optimization
        /// </summary>
        public 
        TypePropertyCache 
        TypePropertyCache
        {
            get 
            { 
                return _propertyInfo; 
            }
        }

        #endregion Public Properties
        
        #region Private Data

        private 
        object              _targetObject;
        private 
        TypePropertyCache   _propertyInfo;
        private
        bool                _isComplex;

        #endregion Private Data
    };

    /// <summary>
    /// A class defining context for serializable Dependency Properties
    /// </summary>
    internal class SerializableDependencyPropertyContext : 
                   SerializablePropertyContext
    {
        #region Constructor

        /// <summary>
        /// Constructor for SerializableDependencyPropertyContext
        /// </summary>
        public 
        SerializableDependencyPropertyContext(
            string                      name, 
            string                      prefix, 
            object                      target, 
            TypeDependencyPropertyCache propertyCache) : 
        base(name, prefix,target,propertyCache)
        {
        }

        /// <summary>
        /// Constructor for SerializableDependencyPropertyContext
        /// </summary>
        public 
        SerializableDependencyPropertyContext(
            object                      target, 
            TypeDependencyPropertyCache propertyCache) : 
        base(target,propertyCache)
        {
        }

        #endregion Constructor


        #region Public Methods
        
        /// <summary>
        /// Detects whether this property is complex or simple
        /// </summary>
        public
        override
        bool
        IsComplexProperty(
            PackageSerializationManager serializationManager
            )
        {
            bool isComplex = false;

            //
            // Null property value is always serialized 
            // in simple attribute="*null" notation
            //
            if (Value != null)
            {
                //
                // If the property has a DesignerSerializationOptions.SerializeAsAttribute 
                // then obviously we do not use complex notation
                //
                if(!(DesignerSerializationOptionsAttribute != null && 
                     (DesignerSerializationOptionsAttribute.DesignerSerializationOptions == 
                      DesignerSerializationOptions.SerializeAsAttribute)))

                {
                    //
                    // String space preservation is honored by System.Xml only for contents of a tag not within 
                    // an attribute value. Hence we always emit strings as content within a tag
                    //
                    Type valueType = Value.GetType();

                    if (valueType == typeof(string) && 
                        ((string)Value) != string.Empty)
                    {
                        isComplex = true;
                    }
                    else
                    {
                        bool canConvert;
                        isComplex = IsComplexValue(serializationManager,
                                                   out canConvert);

                        if (!canConvert)
                        {
                            Expression expr = this.Value as Expression;

                            if (expr != null)
                            {
                                this.Value = ((DependencyObject)this.TargetObject).GetValue((DependencyProperty)this.DependencyProperty);
                                isComplex = this.IsComplexProperty(serializationManager);
                            }
                        }
                    }
                }
            }

            return isComplex;
        }

        public
        override
        bool 
        IsComplexValue(
                PackageSerializationManager manager, 
            out bool                        canConvert
            )
        {
            bool isComplex = true;

            canConvert = true;
            
            if(SerializerType!=null)
            {
                isComplex = true;
            }
            else
            {
                TypeConverter converter = this.TypeConverter;
                
                canConvert = converter.CanConvertTo(null,typeof(string)) &&
                             converter.CanConvertFrom(null,typeof(string));

                if(canConvert)
                {
                    isComplex = false;
                }
            }

            return isComplex;
        }

        #endregion Public Methods
        
        #region Public Properties

        /// <summary>
        /// return info about the dependency property
        /// </summary>
        public 
        MemberInfo 
        MemberInfo
        {
            get 
            { 
                MemberInfo memberInfo = null;

                if (this.PropertyInfo != null)
                {
                    memberInfo = ((TypeDependencyPropertyCache)TypePropertyCache).MemberInfo;
                }
                
                return memberInfo; 
            }
        }

        /// <summary>
        /// return the dependency property
        /// </summary>
        public 
        Object 
        DependencyProperty
        {
            get 
            { 
                Object dependencyProperty = null;

                if (this.PropertyInfo != null)
                {
                    dependencyProperty = ((TypeDependencyPropertyCache)TypePropertyCache).DependencyProperty;
                }
                
                return dependencyProperty; 
            }
        }

        #endregion Public Properties
    };
}
