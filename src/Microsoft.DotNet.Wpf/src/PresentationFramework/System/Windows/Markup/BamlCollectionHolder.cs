// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Reflection;

namespace System.Windows.Markup
{
    // Helper class that holds a collection for a property.  This class
    // replaces the old BamlDictionaryHolder and BamlArrayHolder classes
    internal class BamlCollectionHolder
    {
        internal BamlCollectionHolder()
        {
        }
        
        internal BamlCollectionHolder(BamlRecordReader reader, object parent, short attributeId) :
            this(reader, parent, attributeId, true)
        {
        }

        internal BamlCollectionHolder(BamlRecordReader reader, object parent, short attributeId, bool needDefault)
        {
            _reader      = reader;
            _parent      = parent;
            _propDef     = new WpfPropertyDefinition(reader, attributeId, parent is DependencyObject);
            _attributeId = attributeId;

            if (needDefault)
            {
                InitDefaultValue();
            }
            
            CheckReadOnly();
        }

        // the collection stored for a given property
        internal object Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        // helper that casts the collection as an IList
        internal IList List
        {
            get { return _collection as IList; }
        }

        // helper that casts the collection as an IDictionary
        internal IDictionary Dictionary
        {
            get { return _collection as IDictionary; }
        }

        // helper that casts the collection as an ArrayExtension
        internal ArrayExtension ArrayExt
        {
            get { return _collection as ArrayExtension; }
        }

        // the default collection to be used in case the property does not have an explicit tag
        internal object DefaultCollection
        {
            get { return _defaultCollection; }
        }

        // the PropertyDefinition associated with the BamlCollectionHolder's property
        internal WpfPropertyDefinition PropertyDefinition
        {
            get { return _propDef; }
        }

        // the return type of the BamlCollectionHolder's property's getter
        internal Type PropertyType
        {
            get
            {
                return _resourcesParent != null ? typeof(ResourceDictionary) : PropertyDefinition.PropertyType;
            }
        }

        // the parent object of this collection holder
        internal object Parent
        {
            get { return _parent; }
        }

        // returns true if the property cannot be set
        internal bool ReadOnly
        {
            get { return _readonly; }
            set { _readonly = value; }
        }

        // returns true if the property has an explicit tag, so items should not be added to it directly
        internal bool IsClosed
        {
            get { return _isClosed; }
            set { _isClosed = value; }
        }

        internal string AttributeName
        {
            get
            {
                return _reader.GetPropertyNameFromAttributeId(_attributeId);
            }
        }

        // set the property associated with the collection holder to the value of the holder's collection
        internal void SetPropertyValue()
        {
            // Do if the property value has not been set before this
            if (!_isPropertyValueSet)
            {
                _isPropertyValueSet = true;
                
                // the order of precedence is the fast-tracked Resources property, then DP, then the attached property
                // setter, then the property info
                if (_resourcesParent != null)
                {
                    _resourcesParent.Resources = (ResourceDictionary)Collection;
                }
                else if (PropertyDefinition.DependencyProperty != null)
                {
                    DependencyObject dpParent = Parent as DependencyObject;
                    if (dpParent == null)
                    {
                        _reader.ThrowException(SRID.ParserParentDO, Parent.ToString());
                    }
                    _reader.SetDependencyValue(dpParent, PropertyDefinition.DependencyProperty, Collection);
                }
                else if (PropertyDefinition.AttachedPropertySetter != null)
                {
                    PropertyDefinition.AttachedPropertySetter.Invoke(null, new object[] { Parent, Collection });
                }
                else if (PropertyDefinition.PropertyInfo != null)
                {
                    PropertyDefinition.PropertyInfo.SetValue(Parent,
                                    Collection, BindingFlags.Instance |
                                    BindingFlags.Public | BindingFlags.FlattenHierarchy,
                                    null,  null, TypeConverterHelper.InvariantEnglishUS);
                }
                else
                {
                    _reader.ThrowException(SRID.ParserCantGetDPOrPi, AttributeName);
                }
            }
        }

        // Initialize the collection holder, by setting its property definition and default value (retrieved
        // from the property definition).
        internal void InitDefaultValue()
        {
            if (AttributeName == "Resources" &&
                Parent is IHaveResources)
            {
                // "Fast Path" handling of Resources non-DP property
                _resourcesParent = ((IHaveResources)Parent);
                _defaultCollection = _resourcesParent.Resources;
            }

            // the order of precedence according to the spec is DP, then attached property, then PropertyInfo
            else if (PropertyDefinition.DependencyProperty != null)
            {
                _defaultCollection = ((DependencyObject)Parent).GetValue(PropertyDefinition.DependencyProperty);
            }
            else if (PropertyDefinition.AttachedPropertyGetter != null)
            {
                _defaultCollection = PropertyDefinition.AttachedPropertyGetter.Invoke(null, new object[] { Parent });
            }
            else if (PropertyDefinition.PropertyInfo != null)
            {
                if (PropertyDefinition.IsInternal)
                {
                    _defaultCollection = XamlTypeMapper.GetInternalPropertyValue(_reader.ParserContext,
                                                                                 _reader.ParserContext.RootElement, 
                                                                                 PropertyDefinition.PropertyInfo,
                                                                                 Parent);
                    
                    if (_defaultCollection == null)
                    {
                        _reader.ThrowException(SRID.ParserCantGetProperty, PropertyDefinition.Name);
                    }
                }
                else
                {
                    _defaultCollection = PropertyDefinition.PropertyInfo.GetValue(
                        Parent, BindingFlags.Instance |
                        BindingFlags.Public | BindingFlags.FlattenHierarchy,
                        null, null, TypeConverterHelper.InvariantEnglishUS);
                }
            }
            else
            {
                _reader.ThrowException(SRID.ParserCantGetDPOrPi, AttributeName);
            }
        }

        private void CheckReadOnly()
        {
            if (_resourcesParent == null &&
                (PropertyDefinition.DependencyProperty == null || PropertyDefinition.DependencyProperty.ReadOnly) &&
                (PropertyDefinition.PropertyInfo == null || !PropertyDefinition.PropertyInfo.CanWrite) &&
                PropertyDefinition.AttachedPropertySetter == null)
            {
                if (DefaultCollection == null)
                {
                    // if the property is read-only and has a null default value, throw an exception
                    _reader.ThrowException(SRID.ParserReadOnlyNullProperty, PropertyDefinition.Name);
                }

                // the property is read-only, so we have to use its default value as the dictionary
                ReadOnly = true;
                Collection = DefaultCollection;
            }
        }

        private object             _collection;
        private object             _defaultCollection;
        private short              _attributeId;
        private WpfPropertyDefinition _propDef;
        private object             _parent;
        private BamlRecordReader   _reader;
        private IHaveResources     _resourcesParent;  // for fast-tracking Resources properties
        private bool               _readonly;
        private bool               _isClosed;
        private bool               _isPropertyValueSet;
    }
}


