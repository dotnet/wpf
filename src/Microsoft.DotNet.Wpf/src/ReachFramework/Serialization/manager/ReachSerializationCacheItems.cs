// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
    internal class TypeDependencyPropertiesCacheItem
    {
        internal
        TypeDependencyPropertiesCacheItem(
            Type                           type,
            TypeDependencyPropertyCache[]  properties
            )
        {
            this.objectType = type;
            this.serializableDependencyProperties = properties;
        }

        internal 
        TypeDependencyPropertyCache[] 
        GetSerializableDependencyProperties(
            )
        {
            return  serializableDependencyProperties;
        }

        private
        Type                           objectType;

        private
        TypeDependencyPropertyCache[]  serializableDependencyProperties;

    };

    internal class TypeCacheItem
    {
        internal 
        TypeCacheItem(
            Type    type
            )
        {

            this.type                   = type;
            this.serializerType         = null;
            this.typeConverter          = null;
            clrSerializableProperties   = null;
        }

        internal 
        TypeCacheItem(
            Type    type,
            Type    serializerType
            )
        {

            this.type                   = type;
            this.serializerType         = serializerType;
            this.typeConverter          = null;
            clrSerializableProperties   = null;
        }

        internal 
        TypeCacheItem(
            Type            type,
            TypeConverter   typeConverter
            )
        {

            this.type                   = type;
            this.serializerType         = null;
            this.typeConverter          = typeConverter;
            clrSerializableProperties   = null;
        }

        internal 
        TypePropertyCache[] 
        GetClrSerializableProperties(
            SerializersCacheManager serializersCacheManager
            )
        {
            if (clrSerializableProperties == null)
            {
                PropertyInfo[] properties = type.GetProperties();

                //
                // Separate out the serializable Clr properties
                //
                int                 IndexOfSerializableProperties  = 0;
                int[]               propertiesIndex                = new int[properties.Length];
                TypePropertyCache[] cachedProperties               = new TypePropertyCache[properties.Length];


                for(int indexInProperties = 0;
                    indexInProperties < properties.Length;
                    indexInProperties++)
                {
                    PropertyInfo propertyInfo = properties[indexInProperties];

                    DesignerSerializationVisibility visibility                  = DesignerSerializationVisibility.Visible;
                    Type                            serializerTypeForProperty   = null;
                    TypeConverter                   typeConverterForProperty    = null;
                    DefaultValueAttribute           defaultValueAttr            = null;
                    DesignerSerializationOptionsAttribute         
                    designerSerializationFlagsAttr                              = null;

                    if(CanSerializeProperty(propertyInfo,
                                            serializersCacheManager,
                                            out visibility,
                                            out serializerTypeForProperty,
                                            out typeConverterForProperty,
                                            out defaultValueAttr,
                                            out designerSerializationFlagsAttr) == true)
                    {
                        //
                        // Figure out the Serializer or TypeConverter associated with the 
                        // type of that property. This would potentially be cached in 2 
                        // different places
                        // 1. The Type Cache 
                        // 2. The TypePropertyCache.
                        //
                        TypeCacheItem typeCacheItem = serializersCacheManager.GetTypeCacheItem(propertyInfo.PropertyType);

                        serializerTypeForProperty = typeCacheItem.SerializerType;
                        typeConverterForProperty  = typeCacheItem.TypeConverter;

                        //
                        // We create a cache of this property and all the information we
                        // deduced about it
                        //
                        TypePropertyCache propertyCache = new TypePropertyCache(propertyInfo,
                                                                                visibility,
                                                                                serializerTypeForProperty,
                                                                                typeConverterForProperty,
                                                                                defaultValueAttr,
                                                                                designerSerializationFlagsAttr);

                        propertiesIndex[IndexOfSerializableProperties]    = indexInProperties;
                        cachedProperties[IndexOfSerializableProperties++] = propertyCache;
                    }
                }

                clrSerializableProperties = new TypePropertyCache[IndexOfSerializableProperties];

                for(int indexInClrProperties=0;
                    indexInClrProperties < IndexOfSerializableProperties;
                    indexInClrProperties++)
                {
                    clrSerializableProperties[indexInClrProperties] = cachedProperties[indexInClrProperties];
                }
            }
            
            return clrSerializableProperties;
        }

        internal
        Type
        SerializerType
        {
            get
            {
                return serializerType;
            }
        }

        internal
        TypeConverter
        TypeConverter
        {
            get
            {
                return typeConverter;
            }
        }

        private
        bool
        CanSerializeProperty(
                PropertyInfo                        propertyInfo,
                SerializersCacheManager             serializersCacheManager,
            out DesignerSerializationVisibility     visibility,                  
            out Type                                serializerTypeForProperty,   
            out TypeConverter                       typeConverterForProperty,    
            out DefaultValueAttribute               defaultValueAttr,            
            out DesignerSerializationOptionsAttribute designerSerializationFlagsAttr
            )
        {

            bool canSerializeProperty      = false;
            visibility                     = DesignerSerializationVisibility.Visible;
            serializerTypeForProperty      = null;
            typeConverterForProperty       = null;
            defaultValueAttr               = null;
            designerSerializationFlagsAttr = null;

            // The conditions that we care about in those properties are as follows
            // 1. Readable properties
            // 2. None Indexable Properteis. 
            //    So we can't deal with properties that take name: Item OR
            //    take the form this[index1, index2, ...]
            // 3. Properties that are not backed up by a dependency property
            // 4. Properties that are decorated with the DesignerSerializationVisibility
            //    and that are not hidden
            //
            if (propertyInfo.CanRead && 
                propertyInfo.GetIndexParameters().GetLength(0) == 0)
            {
                MemberInfo memberInfo          = (MemberInfo) propertyInfo;

                Attribute[] attributes = Attribute.GetCustomAttributes(memberInfo);

                for(int numberOfAttributes = 0;
                     numberOfAttributes < attributes.Length;
                     numberOfAttributes++)
                {
                    //
                    // Based on the attribute type, different properties could be set
                    //
                    Attribute attribute = attributes[numberOfAttributes];


                    if (attribute is DesignerSerializationVisibilityAttribute)
                    {
                        visibility = ((DesignerSerializationVisibilityAttribute)attribute).Visibility;
                    }
                    else if (attribute is DefaultValueAttribute)
                    {
                        defaultValueAttr = (DefaultValueAttribute)attribute;
                    }
                    else if (attribute is DesignerSerializationOptionsAttribute)
                    {
                        designerSerializationFlagsAttr = (DesignerSerializationOptionsAttribute)attribute;
                    }
                }

                object DependencyPropertyORPropertyInfo = 
                       DependencyProperty.FromName(propertyInfo.Name, propertyInfo.DeclaringType);

                if(DependencyPropertyORPropertyInfo == null             &&
                   visibility != DesignerSerializationVisibility.Hidden && 
                   (propertyInfo.CanWrite ||  visibility == DesignerSerializationVisibility.Content))
                {
                    if(visibility != DesignerSerializationVisibility.Hidden)
                    {
                        canSerializeProperty = true;
                    }
                }
            }

            return canSerializeProperty;
        }

        private
        Type                            type;

        private 
        Type                            serializerType;

        private
        TypeConverter                   typeConverter;

        private
        TypePropertyCache[]             clrSerializableProperties;
    };

    internal class TypePropertyCache
    {
        internal
        TypePropertyCache(
            )
        {  
        }

        internal
        TypePropertyCache(
            PropertyInfo propertyInfo
            )
        {  
            if(propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }
            this.propertyInfo                   = propertyInfo;
            this.visibility                     = DesignerSerializationVisibility.Visible;
            this.serializerTypeForProperty      = null;
            this.typeConverterForProperty       = null;
            this.defaultValueAttr               = null;
            this.designerSerializationFlagsAttr = null;
            this.propertyValue                  = null;
        }

        internal
        TypePropertyCache(
            PropertyInfo                            propertyInfo,
            DesignerSerializationVisibility         visibility,
            Type                                    serializerTypeForProperty,
            TypeConverter                           typeConverterForProperty,
            DefaultValueAttribute                   defaultValueAttr,
            DesignerSerializationOptionsAttribute     designerSerializationFlagsAttr
            )
        {  
            if(propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }
            this.propertyInfo                   = propertyInfo;
            this.visibility                     = visibility;
            this.serializerTypeForProperty      = serializerTypeForProperty;
            this.typeConverterForProperty       = typeConverterForProperty;
            this.defaultValueAttr               = defaultValueAttr;
            this.designerSerializationFlagsAttr = designerSerializationFlagsAttr;
            this.propertyValue                  = null;
        }


        internal
        DesignerSerializationVisibility
        Visibility
        {
            get
            {
                return visibility;
            }

            set
            {
                visibility = value;
            }
        }

        internal
        Type
        SerializerTypeForProperty
        {
            get
            {
                return serializerTypeForProperty;
            }

            set
            {
                serializerTypeForProperty = value;
            }
        }

        internal
        TypeConverter
        TypeConverterForProperty
        {
            get
            {
                return typeConverterForProperty;
            }

            set
            {
                typeConverterForProperty = value;
            }
        }

        internal
        DefaultValueAttribute
        DefaultValueAttr
        {
            get
            {
                return defaultValueAttr;
            }
            
            set
            {
                defaultValueAttr = value;
            }
        }

        internal
        DesignerSerializationOptionsAttribute
        DesignerSerializationOptionsAttr
        {
            get
            {
                return designerSerializationFlagsAttr;
            }
            
            set
            {
                designerSerializationFlagsAttr = value;
            }
        }


        internal
        PropertyInfo
        PropertyInfo
        {
            get
            {
                return propertyInfo;
            }
            
            set
            {
                propertyInfo = value;
            }
        }

        internal
        object
        PropertyValue
        {
            get
            {
                return propertyValue;
            }

            set
            {
                propertyValue = value;
            }
        }

        private
        DesignerSerializationVisibility         visibility;
        private
        Type                                    serializerTypeForProperty;
        private
        TypeConverter                           typeConverterForProperty;
        private
        DefaultValueAttribute                   defaultValueAttr;
        private
        DesignerSerializationOptionsAttribute     designerSerializationFlagsAttr;
        private
        PropertyInfo                            propertyInfo;
        private
        object                                  propertyValue;
    };

    internal class TypeDependencyPropertyCache:
                   TypePropertyCache
    {
        internal
        TypeDependencyPropertyCache(
            MemberInfo                              memberInfo,
            Object                                  dependencyProperty,
            DesignerSerializationVisibility         visibility,
            Type                                    serializerTypeForProperty,
            TypeConverter                           typeConverterForProperty,
            DefaultValueAttribute                   defaultValueAttr,
            DesignerSerializationOptionsAttribute     designerSerializationFlagsAttr
            ):
        base()
        {
            this.MemberInfo                        = memberInfo;
            this.DependencyProperty                = dependencyProperty;
            this.PropertyInfo                      = memberInfo as PropertyInfo;
            this.Visibility                        = visibility;
            this.SerializerTypeForProperty         = serializerTypeForProperty;
            this.TypeConverterForProperty          = typeConverterForProperty;
            this.DefaultValueAttr                  = defaultValueAttr;
            this.DesignerSerializationOptionsAttr  = designerSerializationFlagsAttr;
            this.PropertyValue                     = null;
        }

        internal
        static
        bool
        CanSerializeProperty(
                MemberInfo                          memberInfo,
                SerializersCacheManager             serializersCacheManager,
            out DesignerSerializationVisibility     visibility,                  
            out Type                                serializerTypeForProperty,   
            out TypeConverter                       typeConverterForProperty,    
            out DefaultValueAttribute               defaultValueAttr,            
            out DesignerSerializationOptionsAttribute designerSerializationFlagsAttr
            )
        {

            bool canSerializeProperty = false;

            // The conditions that we care about in those properties are as follows
            // 1. Properties that are decorated with the DesignerSerializationVisibility
            //    and that are not hidden
            //
            visibility                     = DesignerSerializationVisibility.Visible;
            serializerTypeForProperty      = null;
            typeConverterForProperty       = null;
            defaultValueAttr               = null;
            designerSerializationFlagsAttr = null;

            Attribute[] attributes = Attribute.GetCustomAttributes(memberInfo);

            for(int numberOfAttributes = 0;
                numberOfAttributes < attributes.Length;
                numberOfAttributes++)
            {
                //
                // Based on the attribute type, different properties could be set
                //
                Attribute attribute = attributes[numberOfAttributes];


                if (attribute is DesignerSerializationVisibilityAttribute)
                {
                    visibility = ((DesignerSerializationVisibilityAttribute)attribute).Visibility;
                }
                else if (attribute is DefaultValueAttribute)
                {
                    defaultValueAttr = (DefaultValueAttribute)attribute;
                }
                else if (attribute is DesignerSerializationOptionsAttribute)
                {
                    designerSerializationFlagsAttr = (DesignerSerializationOptionsAttribute)attribute;
                }
            }

            if(visibility != DesignerSerializationVisibility.Hidden)
            {
                canSerializeProperty = true;
            }

            return canSerializeProperty;
        }

        internal
        static
        bool 
        CanSerializeValue(
            object                       serializableObject,
            TypeDependencyPropertyCache  propertyCache
            )
        {

            bool canSerializeValue = false;
            //
            // For readonly properties check for DesignerSerializationVisibility.Content
            //
            bool isReadOnly = ((DependencyProperty)propertyCache.DependencyProperty).ReadOnly;


            if (isReadOnly && 
                propertyCache.Visibility == DesignerSerializationVisibility.Content)
            {
                //
                // If there is a Visibility.Content attribute honor it and 
                // populate the property value in this data structure
                //
                //
                DependencyObject targetDO   = serializableObject as DependencyObject;
                propertyCache.PropertyValue = targetDO.ReadLocalValue((DependencyProperty)propertyCache.DependencyProperty);

                canSerializeValue = true;
            }
            else if (propertyCache.DefaultValueAttr == null)
            {
                //
                // Populate the property value in this data structure
                //
                DependencyObject targetDO   = serializableObject as DependencyObject;
                propertyCache.PropertyValue = targetDO.ReadLocalValue((DependencyProperty)propertyCache.DependencyProperty);

                canSerializeValue = true;
            }
            else
            {
                //
                // Populate the property value in this data structure 
                // as it is required to evaluate the default value
                //
                DependencyObject targetDO   = serializableObject as DependencyObject;
                propertyCache.PropertyValue = targetDO.ReadLocalValue((DependencyProperty)propertyCache.DependencyProperty);
                //
                // For Clr properties with a DefaultValueAttribute 
                // check if the current value equals the default
                //
                canSerializeValue = !object.Equals(propertyCache.DefaultValueAttr.Value, 
                                                   propertyCache.PropertyValue);

                if(!canSerializeValue)
                {
                    propertyCache.PropertyValue = null;
                }
            }

            if(canSerializeValue)
            {
                if ((propertyCache.PropertyValue == null) ||
                    (propertyCache.PropertyValue == System.Windows.DependencyProperty.UnsetValue))
                {
                    canSerializeValue = false;
                }
            }

            return canSerializeValue;
        }
        
        internal
        MemberInfo
        MemberInfo
        {
            get
            {
                return memberInfo;
            }
            
            set
            {
                memberInfo = value;
            }
        }

        internal
        Object
        DependencyProperty
        {
            get
            {
                return dependencyProperty;
            }
            
            set
            {
                dependencyProperty = value;
            }
        }

        private
        MemberInfo          memberInfo;
        private
        object              dependencyProperty;
    };
}
