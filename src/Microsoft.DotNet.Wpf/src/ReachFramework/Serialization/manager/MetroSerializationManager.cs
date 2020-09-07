// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file contains the definition of a Base class that controls
        the serialization processo of METRO into S0. It also includes
        a cache manager which helps in caching data about types while the
        tree of objects is being traversed to optimize the permormance of
        the serialization process

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
using System.Windows.Xps.Serialization;
using System.Windows.Xps;
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class defines all necessary interfaces (with a stub
    /// implementation to some of them) that are necessary to provide
    /// serialization services for persisting an AVALON root object
    /// into a package. It glues together all necessary serializers and
    /// type converters for different type of objects to produce the correct
    /// serialized content in the package.
    /// </summary>
    public abstract class PackageSerializationManager : 
                          IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructor to create and initialize the base 
        /// PackageSerializationManager class.
        /// </summary>
        protected 
        PackageSerializationManager(
            )
        {
            _serializersCacheManager            = new SerializersCacheManager(this);
            _graphContextStack                  = new ContextStack();
            this._rootSerializableObjectContext = null;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Saves the given object instance to the underlying packaging
        /// as S0 representation
        /// </summary>
        /// <param name="serializedObject">
        /// object instance to serialize.
        /// </param>
        public
        abstract
        void
        SaveAsXaml(
            Object  serializedObject
            );

        #endregion Public Methods


        #region IDisposable implementation

        void
        IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable implementation


        #region Internal Methods

        /// <summary>
        /// Returns the namespace for the object instance
        /// being serialized
        /// </summary>
        /// <param name="objectType">
        /// The Type of the object instance being serialized.
        /// </param>
        internal
        abstract
        String
        GetXmlNSForType(
            Type    objectType
            );


        /// <summary>
        /// Retrieves the serializer for a given object instance
        /// </summary>
        /// <param name="serializedObject">
        /// The object instance being serialized.
        /// </param>
        internal
        virtual
        ReachSerializer
        GetSerializer(
            Object serializedObject
            )
        {
            ReachSerializer reachSerializer = null;

            reachSerializer = _serializersCacheManager.GetSerializer(serializedObject);

            return reachSerializer;
        }

        /// <summary>
        /// Retrieves the type of the serializer for a given object instance type.
        /// </summary>
        /// <param name="objectType">
        /// The type of the object instance being serialized.
        /// </param>
        internal
        virtual
        Type
        GetSerializerType(
            Type objectType
            )
        {
            Type serializerType = null;

            return serializerType;
        }

        /// <summary>
        /// Retrieves the TypeConverter for a given object instance.
        /// </summary>
        /// <param name="serializedObject">
        /// The object instance being serialized.
        /// </param>
        internal
        virtual
        TypeConverter
        GetTypeConverter (
            Object serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            return TypeDescriptor.GetConverter(serializedObject.GetType());
        }

        /// <summary>
        /// Retrieves the TypeConverter for a given object instance type.
        /// </summary>
        /// <param name="serializedObjectType">
        /// The type of the object instance being serialized.
        /// </param>
        internal
        virtual
        TypeConverter
        GetTypeConverter (
            Type serializedObjectType
            )
        {
            return TypeDescriptor.GetConverter(serializedObjectType);
        }

        /// <summary>
        /// Retrieves the XmlWriter for a given type.
        /// </summary>
        /// <param name="writerType">
        /// The type of object that consequently dictates the type of
        /// writer.
        /// </param>
        internal
        abstract
        XmlWriter
        AcquireXmlWriter(
            Type    writerType
            );

        /// <summary>
        /// Releases the XmlWriter already retreived for a given type.
        /// </summary>
        /// <param name="writerType">
        /// The type of object that consequently dictates the type of
        /// writer.
        /// </param>
        internal
        abstract
        void
        ReleaseXmlWriter(
            Type    writerType
            );

        /// <summary>
        /// Retrieves the stream for a given resource type. This stream
        /// would be filled in by the resource data during the serialization
        /// process
        /// </summary>
        /// <param name="resourceType">
        /// The type of resource that consequently dictates the type of
        /// stream.
        /// </param>
        internal
        abstract
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType
            );

        /// <summary>
        /// Retrieves the stream for a given resource type. This stream
        /// would be filled in by the resource data during the serialization
        /// process
        /// </summary>
        /// <param name="resourceType">
        /// The type of resource that consequently dictates the type of
        /// stream.
        /// </param>
        /// <param name="resourceID">
        /// The ID of resource and that is used internally for caching
        /// and sharing resources across multiple parts.
        /// </param>
        internal
        abstract
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType,
            String  resourceID
            );

        /// <summary>
        /// Releases the stream for a given resource type. Releasing a stream
        /// means that the stream would be flushed and committed to the underlying
        /// packaging infrasturcutre
        /// </summary>
        /// <param name="resourceType">
        /// The type of resource that consequently dictates the type of
        /// part being flushed.
        /// </param>
        internal
        abstract
        void
        ReleaseResourceStream(
            Type    resourceType
            );

        /// <summary>
        /// Releases the stream for a given resource type. Releasing a stream
        /// means that the stream would be flushed and committed to the underlying
        /// packaging infrasturcutre
        /// </summary>
        /// <param name="resourceType">
        /// The type of resource that consequently dictates the type of
        /// part being flushed.
        /// </param>
        /// <param name="resourceID">
        /// The ID of resource and that is used internally for caching
        /// and sharing resources across multiple parts.
        /// </param>
        internal
        abstract
        void
        ReleaseResourceStream(
            Type    resourceType,
            String  resourceID
            );

        internal
        abstract
        void
        AddRelationshipToCurrentPage(
            Uri targetUri,
            string relationshipName
            );

        internal
        virtual
        bool
        CanSerializeDependencyProperty(
            Object                      serializableObject,
            TypeDependencyPropertyCache dependencyProperty
            )
        {
            return true;
        }

        internal
        virtual
        bool
        CanSerializeClrProperty(
            Object              serializableObject,
            TypePropertyCache   property
            )
        {
            return true;
        }

        #endregion Internal Methods

        #region Internal Properties

        internal
        abstract
        BasePackagingPolicy
        PackagingPolicy
        {
            get;
        }

        internal
        abstract
        XpsResourcePolicy
        ResourcePolicy
        {
            get;
        }

        /// <summary>
        /// Queries or Sets the StackContext that hosts all
        /// the nodes within the graph of the serialized object
        /// </summary>
        internal
        ContextStack
        GraphContextStack
        {
            get
            {
                return _graphContextStack;
            }

            set
            {
                _graphContextStack = value;
            }

        }

        /// <summary>
        /// Queries the cache manager
        /// </summary>
        internal
        SerializersCacheManager
        CacheManager
        {
            get
            {
                return  _serializersCacheManager;
            }
        }

        /// <summary>
        /// Queries root object of the current serialization run.
        /// </summary>
        internal
        SerializableObjectContext
        RootSerializableObjectContext
        {
            get
            {
                return _rootSerializableObjectContext;
            }
            set
            {
                _rootSerializableObjectContext = value;
            }
        }

        /// <summary>
        /// Queries the cache manager
        /// </summary>
        internal
        SerializersCacheManager
        SerializersCacheManager
        {
            get
            {
                return _serializersCacheManager;
            }
        }

        internal
        XmlLanguage
        Language
        {
            get
            {
                return _language;
            }

            set
            {
                _language = value;
            }
        }

        internal
        int
        JobIdentifier
        {                
            set
            {
                _jobIdentifier = value;
            }
                    
            get
            {
                return _jobIdentifier;
            }
        }

        #endregion Internal Properties

        #region Private Data Members

        private
        SerializersCacheManager     _serializersCacheManager;

        private
        ContextStack                _graphContextStack;

        private
        SerializableObjectContext   _rootSerializableObjectContext;

        private
        XmlLanguage                 _language;

        private 
        int                         _jobIdentifier;

        #endregion Private Data Members
    };

    /// <summary>
    ///
    /// </summary>
    public
    delegate
    void
    XpsSerializationPrintTicketRequiredEventHandler(
        object                                          sender,
        XpsSerializationPrintTicketRequiredEventArgs    e
        );


    /// <summary>
    /// This class is a cache repository for different items
    /// that couold be repeatedly used in the serialization
    /// process.This actually control the caching for the
    /// following :
    /// - Types
    /// - Serializers on Types
    /// - Type Converters on Types
    /// </summary>
    internal class SerializersCacheManager
    {
        #region Constructors

        /// <summary>
        /// Constructor to create and initialize the base
        /// SerializersCacheManager class.
        /// </summary>
        /// <param name="serializationManager">
        /// The instance of the serilization manager for this current
        /// run that information is being cached for
        /// </param>
        public
        SerializersCacheManager(
            PackageSerializationManager   serializationManager
            )
        {
            this._serializationManager = serializationManager;
            //
            // Allocate all necessary hashtables for storing
            // the cache information
            //
            _typesCacheTable                     = new Hashtable(20);
            _serializersTable                    = new Hashtable(20);
            _typesDependencyPropertiesCacheTable = new Hashtable(20);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Retrieves the serializer for a given object instance
        /// </summary>
        /// <param name="serializedObject">
        /// The object instance being serialized.
        /// </param>
        public
        ReachSerializer
        GetSerializer(
            Object  serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            ReachSerializer reachSerializer = null;

            TypeCacheItem cacheItem = GetTypeCacheItem(serializedObject);

            if(cacheItem != null)
            {
                Type serializerType = cacheItem.SerializerType;

                //
                // Instantiate the metro serializer based on this type
                //
                if(serializerType!=null)
                {
                    reachSerializer = (ReachSerializer)_serializersTable[serializerType];

                    if (reachSerializer == null)
                    {
                        reachSerializer = CreateReachSerializer(serializerType);
                        _serializersTable[serializerType] = reachSerializer;
                    }
                }
            }

            return reachSerializer;
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Retrieves the serializable clr properties for a given object instance
        /// </summary>
        /// <param name="serializableObject">
        /// The object instance being serialized.
        /// </param>
        internal
        TypePropertyCache[]
        GetClrSerializableProperties(
            Object serializableObject
            )
        {
            TypeCacheItem         item                        = GetTypeCacheItem(serializableObject);
            TypePropertyCache[]   clrProperties               = item.GetClrSerializableProperties(this);
            int[]                 serializableIndeces         = new int[clrProperties.Length];
            int                   serializablePropertiesIndex = 0;

            //
            // Not everything we get can be serializable, so we have to figure out which
            // ones are but checking if we can also serialize the value of the property
            // values would be added to the cache as well.
            //
            for(int indexInClrProperties = 0;
                 indexInClrProperties < clrProperties.Length;
                 indexInClrProperties++)
            {
                if(CanSerializeValue(serializableObject,
                                     clrProperties[indexInClrProperties]) &&
                   _serializationManager.CanSerializeClrProperty(serializableObject,
                                                                 clrProperties[indexInClrProperties]))
                {
                    serializableIndeces[serializablePropertiesIndex++] = indexInClrProperties;
                }
            }

            TypePropertyCache[]  clrSerializableProperties = new TypePropertyCache[serializablePropertiesIndex];

            for(int indexInClrSerializableProperties = 0;
                indexInClrSerializableProperties < serializablePropertiesIndex;
                indexInClrSerializableProperties++)
            {
                TypePropertyCache propertyCache = clrProperties[serializableIndeces[indexInClrSerializableProperties]];
                TypePropertyCache serializablePropertyCache = new TypePropertyCache(propertyCache.PropertyInfo,
                                                                                    propertyCache.Visibility,
                                                                                    propertyCache.SerializerTypeForProperty,
                                                                                    propertyCache.TypeConverterForProperty,
                                                                                    propertyCache.DefaultValueAttr,
                                                                                    propertyCache.DesignerSerializationOptionsAttr);

                serializablePropertyCache.PropertyValue = propertyCache.PropertyValue;

                clrSerializableProperties[indexInClrSerializableProperties] = serializablePropertyCache;
            }

            //
            // Clear all set values
            //
            for(int indexInClrProperties = 0;
                indexInClrProperties < clrProperties.Length;
                indexInClrProperties++)
            {
                clrProperties[indexInClrProperties].PropertyValue = null;
            }

            return clrSerializableProperties;
        }

        /// <summary>
        /// Retrieves the serializable dependency properties for a
        /// given object instance
        /// </summary>
        /// <param name="serializableObject">
        /// The object instance being serialized.
        /// </param>
        internal
        TypeDependencyPropertyCache[]
        GetSerializableDependencyProperties(
            Object serializableObject
            )
        {
            TypeDependencyPropertyCache[]  serializableDependencyProperties = null;

            TypeDependencyPropertiesCacheItem item = GetTypeDependencyPropertiesCacheItem(serializableObject);

            if(item != null)
            {
                TypeDependencyPropertyCache[]  dependencyProperties        = item.GetSerializableDependencyProperties();
                int[]                          serializableIndeces         = new int[dependencyProperties.Length];
                int                            serializablePropertiesIndex = 0;

                //
                // Not everything we get can be serializable, so we have to figure out which
                // ones are by checking if we can also serialize the value of the property
                // values would be added to the cache as well.
                //
                for(int indexInDependencyProperties = 0;
                     indexInDependencyProperties < dependencyProperties.Length;
                     indexInDependencyProperties++)
                {
                    if(TypeDependencyPropertyCache.
                       CanSerializeValue(serializableObject,
                                         dependencyProperties[indexInDependencyProperties]) &&
                       _serializationManager.CanSerializeDependencyProperty(serializableObject,
                                                                            dependencyProperties[indexInDependencyProperties]))
                    {
                        serializableIndeces[serializablePropertiesIndex++] = indexInDependencyProperties;
                    }
                }

                serializableDependencyProperties = new TypeDependencyPropertyCache[serializablePropertiesIndex];

                for(int indexInSerializableDependencyProperties = 0;
                    indexInSerializableDependencyProperties < serializablePropertiesIndex;
                    indexInSerializableDependencyProperties++)
                {
                    TypeDependencyPropertyCache propertyCache = dependencyProperties[serializableIndeces[indexInSerializableDependencyProperties]];
                    TypeDependencyPropertyCache serializablePropertyCache =
                    new TypeDependencyPropertyCache(propertyCache.MemberInfo,
                                                    propertyCache.DependencyProperty,
                                                    propertyCache.Visibility,
                                                    propertyCache.SerializerTypeForProperty,
                                                    propertyCache.TypeConverterForProperty,
                                                    propertyCache.DefaultValueAttr,
                                                    propertyCache.DesignerSerializationOptionsAttr);

                    serializablePropertyCache.PropertyValue = propertyCache.PropertyValue;

                    serializableDependencyProperties[indexInSerializableDependencyProperties] = serializablePropertyCache;
                }

                //
                // Clear all set values
                //
                for(int indexInDependencyProperties = 0;
                    indexInDependencyProperties < dependencyProperties.Length;
                    indexInDependencyProperties++)
                {
                    dependencyProperties[indexInDependencyProperties].PropertyValue = null;
                }

            }

            return serializableDependencyProperties;
        }

        /// <summary>
        /// Retrieves the Cache for a certain type. The idea here
        /// is that most of the information for a given type are
        /// cached up the first time the type is encountered and then
        /// reused in consequence type serializaiton.
        /// </summary>
        /// <param name="serializableObject">
        /// The object instance being serialized.
        /// </param>
        internal
        TypeCacheItem
        GetTypeCacheItem(
            Object  serializableObject
            )
        {
            if(serializableObject == null)
            {
                throw new ArgumentNullException("serializableObject");
            }

            Type type = serializableObject.GetType();

            TypeCacheItem typeCacheItem = (TypeCacheItem)_typesCacheTable[type];

            if(typeCacheItem == null)
            {
                //
                // This means that the type was not seen before
                // We have to create a new entry to that type
                //
                Type serializerType = _serializationManager.GetSerializerType(type);

                if(serializerType!=null)
                {
                    typeCacheItem = new TypeCacheItem(type, serializerType);

                }
                else
                {
                    //
                    // if the Type does not have a type serializer, then
                    // we should try getting the type converter for that
                    // type
                    //
                    TypeConverter typeConverter = _serializationManager.GetTypeConverter(serializableObject);

                    if(typeConverter != null)
                    {
                        typeCacheItem = new TypeCacheItem(type, typeConverter);

                    }
                    else
                    {
                        typeCacheItem = new TypeCacheItem(type);
                    }
                }

                _typesCacheTable[type] = typeCacheItem;
            }

            return typeCacheItem;
        }

        /// <summary>
        /// Retrieves the cached dependency properties for a given object instance.
        /// The dependency properties for any type are discovered only once and then
        /// they are reused from the cache later on.
        /// </summary>
        /// <param name="serializableObject">
        /// The object instance being serialized.
        /// </param>
        internal
        TypeDependencyPropertiesCacheItem
        GetTypeDependencyPropertiesCacheItem(
            Object  serializableObject
            )
        {
            if(serializableObject == null)
            {
                throw new ArgumentNullException("serializableObject");
            }

            Type type = serializableObject.GetType();

            TypeDependencyPropertiesCacheItem
            cachedItem = (TypeDependencyPropertiesCacheItem)_typesDependencyPropertiesCacheTable[type];

            if(cachedItem == null)
            {
                //
                // This means that the type was not seen before
                // We have to create a new entry to that type
                //
                DependencyObject objectAsDependencyObject = serializableObject as DependencyObject;

                if (objectAsDependencyObject != null)
                {
                    //
                    // First we have to figure out if this dependency
                    // object has any dependency properties that can be
                    // serializable and this has to happen before creating
                    // any cache
                    //
                    DependencyPropertyList list = new DependencyPropertyList(1);

                    for(LocalValueEnumerator localValues = objectAsDependencyObject.GetLocalValueEnumerator();
                        localValues.MoveNext();)
                    {
                        DependencyProperty dependencyProperty = localValues.Current.Property;

                        list.Add(dependencyProperty);
                    }

                    if(list.Count > 0)
                    {
                        int numOfSerializableDependencyProperties = 0;

                        TypeDependencyPropertyCache[] dependencyPropertiesCache = new TypeDependencyPropertyCache[list.Count];

                        for (int indexInDependencyPropertyList=0;
                             indexInDependencyPropertyList<list.Count;
                             indexInDependencyPropertyList++)
                        {

                              DependencyProperty dependencyProperty = list.List[indexInDependencyPropertyList];

                              DesignerSerializationVisibility visibility                  = DesignerSerializationVisibility.Visible;
                              Type                            serializerTypeForProperty   = null;
                              TypeConverter                   typeConverterForProperty    = null;
                              DefaultValueAttribute           defaultValueAttr            = null;
                              DesignerSerializationOptionsAttribute
                              designerSerializationFlagsAttr                              = null;
                              Type propertyType                                           = dependencyProperty.PropertyType;

                              //
                              // Get the static setter member for the DependencyProperty
                              //
                              MemberInfo memberInfo = dependencyProperty.
                                                    OwnerType.
                                                    GetMethod("Get" + dependencyProperty.Name,
                                                             BindingFlags.Public | BindingFlags.NonPublic |
                                                             BindingFlags.Static | BindingFlags.FlattenHierarchy);

                              // Note: This is because the IService model does not abide
                              // by this pattern of declaring the DependencyProperty on
                              // the OwnerType. That is the only exception case.
                              if (memberInfo == null)
                              {
                                  //
                                  // Create a PropertyInfo
                                  //
                                  
                                  PropertyInfo propertyInfo = null;
                                  
                                  PropertyInfo[] properties = dependencyProperty.OwnerType.GetProperties();
                                  
                                  String name = dependencyProperty.Name;
                                  
                                  for (int i=0;
                                       i<properties.Length && propertyInfo == null;
                                       i++)
                                  {
                                      if (properties[i].Name == name)
                                      {
                                          propertyInfo = properties[i];
                                      }
                                  }

                                  if (propertyInfo != null)
                                  {
                                       Debug.Assert(propertyInfo.PropertyType == dependencyProperty.PropertyType, 
                                        "The property type of the CLR wrapper must match that of the DependencyProperty itself.");
                                       
                                       memberInfo = propertyInfo;
                                    
                                       //
                                       // We have to special case Print Tickets here.
                                       // Print Tickets are defined on as dependency properties on
                                       // fixed objects of types:
                                       // o FixedDocumentSequence
                                       // o FixedDocument
                                       // o FixedPage
                                       // and in order to eliminate the dependency between
                                       // PresentationFramework and System.printing assemblies,
                                       // those dependency properties are defined as of type "object"
                                       // and hence if we are here and we have a property of name
                                       // "PrintTicket" and owned by one of the above mentioned types
                                       // we try to get the serializer for the PrintTicket object
                                       //
                                       if( propertyInfo.Name == XpsNamedProperties.PrintTicketProperty &&
                                           ((dependencyProperty.OwnerType == typeof(System.Windows.Documents.FixedPage)) ||
                                            (dependencyProperty.OwnerType == typeof(System.Windows.Documents.FixedDocument)) ||
                                            (dependencyProperty.OwnerType == typeof(System.Windows.Documents.FixedDocumentSequence))))
                                       {
                                           propertyType = typeof(PrintTicket);
                                       }
                                  }
                              }

                              if(memberInfo != null && 
                                 TypeDependencyPropertyCache.
                                 CanSerializeProperty(memberInfo,
                                                      this,
                                                      out visibility,
                                                      out serializerTypeForProperty,
                                                      out typeConverterForProperty,
                                                      out defaultValueAttr,
                                                      out designerSerializationFlagsAttr) == true)
                              {
                                  TypeCacheItem typeCacheItem = GetTypeCacheItem(propertyType);
                                  serializerTypeForProperty = serializerTypeForProperty == null ? typeCacheItem.SerializerType : serializerTypeForProperty;
                                  typeConverterForProperty  = typeConverterForProperty == null ? typeCacheItem.TypeConverter : typeConverterForProperty;

                                  TypeDependencyPropertyCache
                                  dependencyPropertyCache = new TypeDependencyPropertyCache(memberInfo,
                                                                                           dependencyProperty,
                                                                                           visibility,
                                                                                           serializerTypeForProperty,
                                                                                           typeConverterForProperty,
                                                                                           defaultValueAttr,
                                                                                           designerSerializationFlagsAttr);
                                  
                                  dependencyPropertiesCache[numOfSerializableDependencyProperties++] = dependencyPropertyCache;
                              }
                        }

                        if(numOfSerializableDependencyProperties>0)
                        {
                            TypeDependencyPropertyCache[] serializableDependencyPropertiesCache =
                            new TypeDependencyPropertyCache[numOfSerializableDependencyProperties];

                            for(int indexInSerializableProperties = 0;
                                indexInSerializableProperties < numOfSerializableDependencyProperties;
                                indexInSerializableProperties++)
                            {
                                serializableDependencyPropertiesCache[indexInSerializableProperties] =
                                dependencyPropertiesCache[indexInSerializableProperties];
                            }

                            cachedItem = new TypeDependencyPropertiesCacheItem(type,
                                                                               serializableDependencyPropertiesCache);

                            _typesDependencyPropertiesCacheTable[type] = cachedItem;
                        }
                    }
                }
            }

            return cachedItem;
        }


        /// <summary>
        /// Retrieves the Cache for a certain type. The idea here
        /// is that most of the information for a given type are
        /// cached up the first time the type is encountered and then
        /// reused in consequence type serializaiton.
        /// </summary>
        /// <param name="serializableObjectType">
        /// The type of the object instance being serialized.
        /// </param>
        internal
        TypeCacheItem
        GetTypeCacheItem(
            Type  serializableObjectType
            )
        {
            if(serializableObjectType == null)
            {
                throw new ArgumentNullException("serializableObjectType");
            }

            TypeCacheItem typeCacheItem = (TypeCacheItem)_typesCacheTable[serializableObjectType];

            if(typeCacheItem == null)
            {
                //
                // This means that the type was not seen before
                // We have to create a new entry to that type
                //
                Type serializerType = _serializationManager.GetSerializerType(serializableObjectType);

                if(serializerType!=null)
                {
                    typeCacheItem = new TypeCacheItem(serializableObjectType, serializerType);

                }
                else
                {
                    //
                    // if the Type does not have a type serializer, then
                    // we should try getting the type converter for that
                    // type
                    //
                    TypeConverter typeConverter = _serializationManager.GetTypeConverter(serializableObjectType);

                    if(typeConverter != null)
                    {
                        typeCacheItem = new TypeCacheItem(serializableObjectType, typeConverter);

                    }
                    else
                    {
                        typeCacheItem = new TypeCacheItem(serializableObjectType);
                    }
                }

                _typesCacheTable[serializableObjectType] = typeCacheItem;
            }

            return typeCacheItem;
        }

        #endregion Internal Methods

        #region Internal Properties

        /// <summary>
        /// Queries the SerializationManager for which this
        /// cache is being used
        /// </summary>
        internal
        PackageSerializationManager
        SerializationManger
        {
            get
            {
                return _serializationManager;
            }
        }

        #endregion Internal Properties

        #region Private Methods

        /// <summary>
        ///     This function makes the following checks
        ///     1. If the property is readonly it will not be serialized
        ///         unless ShouldSerialize{PropertyName} or
        ///         DesignerSerializationVisibility.Content override this behavior
        ///     2. If there is no DefaultValue attribute it will always be serialized
        ///          unless ShouldSerialize{PropertyName} overrides this behavior
        ///     3. If there is a DefaultValue attribute it will be serialized if the
        ///         current property value does not equal the specified default.
        /// </summary>
        private
        bool
        CanSerializeValue(
            object                serializableObject,
            TypePropertyCache     propertyCache
            )
        {
            bool canSerializeValue = false;
            //
            // For readonly properties check for DesignerSerializationVisibility.Content
            //
            bool isReadOnly = !propertyCache.PropertyInfo.CanWrite;


            if ((isReadOnly &&
                propertyCache.Visibility == DesignerSerializationVisibility.Content) ||
                propertyCache.DefaultValueAttr == null)
            {
                //
                // Populate the property value in this data structure
                //
                propertyCache.PropertyValue = propertyCache.PropertyInfo.GetValue(serializableObject, null);
                canSerializeValue = true;
            }
            else
            {
                //
                // Populate the property value in this data structure
                // as it is required to evaluate the default value
                //
                propertyCache.PropertyValue = propertyCache.PropertyInfo.GetValue(serializableObject, null);
                // For Clr properties with a DefaultValueAttribute
                // check if the current value equals the default
                canSerializeValue = !object.Equals(propertyCache.DefaultValueAttr.Value,
                                                   propertyCache.PropertyValue);

                if(!canSerializeValue)
                {
                    propertyCache.PropertyValue = null;
                }
            }

            return canSerializeValue;
        }

        private 
        ReachSerializer
        CreateReachSerializer(Type serializerType)
        {
            MS.Internal.Invariant.Assert(serializerType != null);
            MS.Internal.Invariant.Assert(serializerType.IsSubclassOf(typeof(ReachSerializer)));

            object[] args = new object[] { _serializationManager };

            ReachSerializer result = Activator.CreateInstance(serializerType, args) as ReachSerializer;

            if (result == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_UnableToInstantiateSerializer));
            }

            return result;
        }

        #endregion Private Methods

        #region Private Data Members

        private 
        PackageSerializationManager   _serializationManager;

        //
        // The following is being cached for future reference and to
        // optimize performance
        // 1. The Types and their clr properties
        // 2. The Types and their corresponding dependency properties. Since,
        //    not all types have dependency properties, that is why I am
        //    caching this in a separate table
        // 3. All Serializers that have been used so far. This makes instantiating
        //    a new instance of a required serializer much faster
        //

        private
        IDictionary                 _typesCacheTable;

        private
        IDictionary                 _serializersTable;

        private
        IDictionary                 _typesDependencyPropertiesCacheTable;

        #endregion Private Data Members
    };
    /// <summary>
    ///
    /// </summary>
    public enum SerializationState
    {
        /// <summary>
        ///
        /// </summary>
        Normal,
        /// <summary>
        ///
        /// </summary>
        Stop
    };
}

