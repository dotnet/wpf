// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  MarkupObject and MarkupProperty implementations for
//             FrameworkElementFactory
//

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows;
using System.Windows.Markup;

using MS.Utility;                       

namespace System.Windows.Markup.Primitives
{
    /// <summary>
    /// Item implementation for FrameworkElementFactory
    /// </summary>
    internal class FrameworkElementFactoryMarkupObject : MarkupObject
    {
        internal FrameworkElementFactoryMarkupObject(FrameworkElementFactory factory, XamlDesignerSerializationManager manager)
        {
            Debug.Assert(factory != null);
            _factory = factory;
            _manager = manager;
        }

        public override void AssignRootContext(IValueSerializerContext context)
        {
            _context = context;
        }

        public override System.ComponentModel.AttributeCollection Attributes
        {
            get 
            {
                return TypeDescriptor.GetAttributes(ObjectType);
            }
        }

        public override Type ObjectType
        {
            get {
                if (_factory.Type != null)
                    return _factory.Type;
                else
                    return typeof(string);
            }
        }

        public override object Instance
        {
            get { return _factory; }
        }

        internal override IEnumerable<MarkupProperty> GetProperties(bool mapToConstructorArgs)
        {
            // This #if is included to make this implementation easier to test outside the assembly.
            // This is the only place in ElementItem and FrameworkElementItem where internal members
            // are accessed that cannot be easily copied by the host.
            if (_factory.Type == null)
            {
                if (_factory.Text != null)
                {
                    yield return new FrameworkElementFactoryStringContent(_factory, this);
                }
            }
            else
            {
                FrugalStructList<PropertyValue> propertyValues = _factory.PropertyValues;
                for (int i = 0; i < propertyValues.Count; i++)
                {
                    if (propertyValues[i].Property != XmlAttributeProperties.XmlnsDictionaryProperty)
                    {
                        yield return new FrameworkElementFactoryProperty(propertyValues[i], this);
                    }
                }
                ElementMarkupObject item = new ElementMarkupObject(_factory, Manager);
                foreach (MarkupProperty property in item.Properties)
                {
                    if (property.Name == "Triggers" && property.Name == "Storyboard")
                    {
                        yield return property;
                    }
                }

                if (_factory.FirstChild != null)
                {
                    if (_factory.FirstChild.Type == null)
                    {
                        yield return new FrameworkElementFactoryStringContent(_factory.FirstChild, this);
                    }
                    else
                    {
                        yield return new FrameworkElementFactoryContent(_factory, this);
                    }
                }
            }
        }

        internal IValueSerializerContext Context
        {
            get { return _context; }
        }

        internal XamlDesignerSerializationManager Manager
        {
            get { return _manager; }
        }

        private FrameworkElementFactory _factory;
        private IValueSerializerContext _context;
        private XamlDesignerSerializationManager _manager;
    }

    /// <summary>
    /// FrameworkElementFactory implementation of MarkupProperty using PropertyValue.
    /// </summary>
    internal class FrameworkElementFactoryProperty : ElementPropertyBase
    {
        public FrameworkElementFactoryProperty(PropertyValue propertyValue, FrameworkElementFactoryMarkupObject item): base(item.Manager)
        {
            _propertyValue = propertyValue;
            _item = item;
        }

        public override PropertyDescriptor PropertyDescriptor
        {
            get 
            {
                if (!_descriptorCalculated)
                {
                    _descriptorCalculated = true;
                    if (DependencyProperty.FromName(_propertyValue.Property.Name, _item.ObjectType) == _propertyValue.Property)
                    {
                        _descriptor = DependencyPropertyDescriptor.FromProperty(_propertyValue.Property, _item.ObjectType);
                    }
                }
                return _descriptor;
            }
        }

        public override bool IsAttached
        {
            get
            {
                DependencyPropertyDescriptor pdp = PropertyDescriptor as DependencyPropertyDescriptor;
                return (pdp != null) && pdp.IsAttached;
            }
        }

        public override AttributeCollection Attributes
        {
            get 
            {
                if (_descriptor != null)
                {
                    return _descriptor.Attributes;
                }
                else
                {
                    PropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(_propertyValue.Property, _item.ObjectType);
                    return descriptor.Attributes;
                }
            }
        }

        public override string Name
        {
            get { return _propertyValue.Property.Name; }
        }

        public override Type PropertyType
        {
            get { return _propertyValue.Property.PropertyType; }
        }

        public override DependencyProperty DependencyProperty
        {
            get { return _propertyValue.Property; }
        }

        public override object Value
        {
            get {
                switch (_propertyValue.ValueType)
                {
                    case PropertyValueType.Set:
                    case PropertyValueType.TemplateBinding:
                        return _propertyValue.Value;
                    case PropertyValueType.Resource:
                        return new DynamicResourceExtension(_propertyValue.Value);
                    default:
                        Debug.Fail("Unexpected property value type");
                        return null;
                }
            }
        }

        protected override IValueSerializerContext GetItemContext()
        {
            return _item.Context;
        }

        protected override Type GetObjectType()
        {
            return _item.ObjectType;
        }

        private PropertyValue _propertyValue;
        private FrameworkElementFactoryMarkupObject _item;

        private bool _descriptorCalculated;
        private PropertyDescriptor _descriptor;
    }

    /// <summary>
    /// Special content property for content because FrameworkElementFactory hard codes
    /// to call IAddChild and doesn't need to know the name of the collection it is
    /// adding to.
    /// </summary>
    internal class FrameworkElementFactoryContent : ElementPropertyBase
    {
        internal FrameworkElementFactoryContent(FrameworkElementFactory factory, FrameworkElementFactoryMarkupObject item): base(item.Manager)
        {
            _item = item;
            _factory = factory;
        }

        public override string Name
        {
            get { return "Content"; }
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
                FrameworkElementFactory child = _factory.FirstChild;
                while (child != null)
                {
                    yield return new FrameworkElementFactoryMarkupObject(child, Manager);
                    child = child.NextSibling;
                }
            }
        }

        protected override IValueSerializerContext GetItemContext()
        {
            return _item.Context;
        }

        protected override Type GetObjectType()
        {
            return _item.ObjectType;
        }

        public override AttributeCollection Attributes
        {
            get { return new AttributeCollection(); }
        }

        public override Type PropertyType
        {
            get { return typeof(IEnumerable);  }
        }

        public override object Value
        {
            get { return _factory; }
        }

        FrameworkElementFactoryMarkupObject _item;
        FrameworkElementFactory _factory;
    }

    /// <summary>
    /// Special content property for string values in a framework element factory.
    /// </summary>
    internal class FrameworkElementFactoryStringContent : ElementPropertyBase
    {
        internal FrameworkElementFactoryStringContent(FrameworkElementFactory factory, FrameworkElementFactoryMarkupObject item)
            : base(item.Manager)
        {
            _item = item;
            _factory = factory;
        }

        public override string Name
        {
            get { return "Content"; }
        }

        public override bool IsContent
        {
            get { return true; }
        }

        public override bool IsComposite
        {
            get { return false; }
        }

        public override bool IsValueAsString
        {
            get { return true; }
        }

        public override IEnumerable<MarkupObject> Items
        {
            get
            {
                return new MarkupObject[0];
            }
        }

        protected override IValueSerializerContext GetItemContext()
        {
            return _item.Context;
        }

        protected override Type GetObjectType()
        {
            return _item.ObjectType;
        }

        public override AttributeCollection Attributes
        {
            get { return new AttributeCollection(); }
        }

        public override Type PropertyType
        {
            get { return typeof(string); }
        }

        public override object Value
        {
            get { return _factory.Text; }
        }

        FrameworkElementFactoryMarkupObject _item;
        FrameworkElementFactory _factory;
    }
}
