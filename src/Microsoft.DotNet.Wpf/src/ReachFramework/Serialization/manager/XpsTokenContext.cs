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
    internal class XpsTokenContext : 
                   System.ComponentModel.ITypeDescriptorContext
    {
        /// <summary>
        ///     Constructor for XpsTokenContext
        /// </summary>
        public
        XpsTokenContext(
            PackageSerializationManager   serializationManager,
            SerializablePropertyContext   propertyContext
            )
        {
            //
            // Make necessary checks and throw necessary exceptions
            //
            this.serializationManager = serializationManager;
            this.targetObject         = propertyContext.TargetObject;
            this.objectValue          = propertyContext.Value;
            this.propertyInfo         = propertyContext.PropertyInfo;
            this.dependencyProperty   = (propertyContext is SerializableDependencyPropertyContext) ?
                                        (DependencyProperty)((SerializableDependencyPropertyContext)propertyContext).DependencyProperty :
                                        null;
        }

        /// <summary>
        ///     Constructor for XpsTokenContext
        /// </summary>
        public
        XpsTokenContext(
            PackageSerializationManager   serializationManager,
            Object                        targetObject,
            Object                        objectValue
            )
        {
            //
            // Make necessary checks and throw necessary exceptions
            //
            this.serializationManager = serializationManager;
            this.targetObject         = targetObject;
            this.objectValue          = objectValue;
            this.propertyInfo         = null;
            this.dependencyProperty   = null;
        }

        /// <summary>
        ///
        /// </summary>
        public 
        void 
        OnComponentChanged()
        {
        }

        // <summary>
        //
        // </summary>
        public
        bool 
        OnComponentChanging()
        {
            return false;
        }


        // <summary>
        //
        // </summary>
        public 
        object 
        GetService(
            Type serviceType
            )
        {
            Object serviceObject = null;

            if (serviceType == typeof(XpsSerializationManager) || 
                serviceType == typeof(XpsSerializationManagerAsync) ||
                serviceType == typeof(ServiceProviders))
            {
                serviceObject = serializationManager;
            }

            return serviceObject;
        }

        // <summary>
        //
        // </summary>
        public 
        System.ComponentModel.IContainer 
        Container
        {
            get 
            {
                return null;
            }
        }

        // <summary>
        //
        // </summary>
        public 
        object 
        Instance 
        {
            get 
            { 
                return objectValue; 
            }
        }


        // <summary>
        //
        // </summary>
        public 
        PropertyInfo 
        PropertyInfo
        {
            get 
            { 
                return propertyInfo; 
            }
        }

        
        // <summary>
        //
        // </summary>
        public
        DependencyProperty 
        DependencyProperty
        {
          get 
          { 
              return dependencyProperty; 
          }
        }

        // <summary>
        //
        // </summary>
        public 
        object 
        TargetObject
        {
            get 
            { 
                return targetObject; 
            }
        }

        // <summary>
        //
        // </summary>
        public
        PropertyDescriptor 
        PropertyDescriptor 
        {
            get
            {
                return null;
            }
        }

        private
        PackageSerializationManager   serializationManager;

        private
        Object                        targetObject;

        private
        Object                        objectValue;


        private
        PropertyInfo                  propertyInfo;

        private
        DependencyProperty            dependencyProperty;
    };
}
