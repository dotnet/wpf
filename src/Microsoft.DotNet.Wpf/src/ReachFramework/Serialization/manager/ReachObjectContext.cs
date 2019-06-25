// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file contains the implementation representing
        the context of a Serializable object. This woudl include
        information about the object itself and all the properties
        contained within that object

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
    internal class SerializableObjectContext :
                   BasicContext
    {
        #region Constructor

        static
        SerializableObjectContext(
            )
        {
            _recycableSerializableObjectContexts = new Stack();
        }


        /// <summary>
        /// Instantiates an ObjectContext
        /// </summary>
        /// <param name="name">
        /// The name of type of the object.
        /// </param>
        /// <param name="prefix">
        /// The perfix (namespace) of the type of the object.
        /// </param>
        /// <param name="target">
        /// The instance of the object which contained this object as
        /// one of its properties.
        /// </param>
        /// <param name="serializablePropertyContext">
        /// The property from which this object was driven to serialization.
        /// </param>
        public
        SerializableObjectContext(
            string name,
            string prefix,
            object target,
            SerializablePropertyContext serializablePropertyContext
            ) :
        base(name, prefix)
        {
            //
            // Validate Input Arguments
            //
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            _targetObject            = target;
            _isComplexValue          = false;
            _backingPropertyContext  = serializablePropertyContext;
        }


        /// <summary>
        ///     Instantiates an ObjectContext
        /// </summary>
        /// <param name="target">
        /// The instance of the object which contained this object as
        /// one of its properties.
        /// </param>
        /// <param name="serializablePropertyContext">
        /// The property from which this object was driven to serialization.
        /// </param>
        public
        SerializableObjectContext(
            object                      target,
            SerializablePropertyContext serializablePropertyContext
            )
        {
            Initialize(target, serializablePropertyContext);
        }

        #endregion Constructor

        #region Internal Methods

        /// <summary>
        /// Factory method to create ObjectContexts
        /// </summary>
        /// <param name="serializationManager">
        /// The manager controllig the serialization process.
        /// </param>
        /// <param name="serializableObject">
        /// The instance of the object which contained this object as
        /// one of its properties.
        /// </param>
        /// <param name="serializableObjectParentContext">
        /// The ObjectContext of the parent object of this object.
        /// </param>
        /// <param name="serializablePropertyContext">
        /// The property from which this object was driven to serialization.
        /// </param>
        internal
        static
        SerializableObjectContext
        CreateContext(
            PackageSerializationManager   serializationManager,
            object                      serializableObject,
            SerializableObjectContext   serializableObjectParentContext,
            SerializablePropertyContext serializablePropertyContext
            )
        {
            //
            // Check for element pre-existance to avoid infinite loops
            // in the process of serialization
            //
            int stackIndex = 0;

            object currentObject = null;

            for(currentObject = serializationManager.GraphContextStack[stackIndex];
                currentObject != null;
                currentObject = serializationManager.GraphContextStack[++stackIndex])
            {
                SerializableObjectContext currentObjectContext = currentObject as SerializableObjectContext;

                if(currentObjectContext!=null &&
                   currentObjectContext.TargetObject == serializableObject)
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_CycleDetectedInSerialization));
                }
            }

            SerializableObjectContext serializableObjectContext;
            lock (_stackLock)
            {
                serializableObjectContext =
                    _recycableSerializableObjectContexts.Count == 0 ?
                    null :
                    (SerializableObjectContext)_recycableSerializableObjectContexts.Pop();
            }

            if(serializableObjectContext == null)
            {
                serializableObjectContext = new SerializableObjectContext(serializableObject,
                                                                          serializablePropertyContext);
            }
            else
            {
                serializableObjectContext.Initialize(serializableObject,
                                                     serializablePropertyContext);
            }

            {
                //
                // Namespace related creation within the context
                //
                MetroSerializationNamespaceTable parentNamespaceTable =
                serializableObjectParentContext != null ? serializableObjectParentContext.NamespaceTable : null;

                if (serializableObjectContext.NamespaceTable == null)
                {
                    serializableObjectContext.NamespaceTable = new MetroSerializationNamespaceTable(parentNamespaceTable);
                }
            }

            {
                //
                // Properties related creation within the context
                //
                if(serializableObjectContext.PropertiesCollection == null)
                {
                    serializableObjectContext.PropertiesCollection = new  SerializablePropertyCollection(serializationManager,
                                                                                                         serializableObject);
                }
                else
                {
                    serializableObjectContext.PropertiesCollection.Initialize(serializationManager,
                                                                              serializableObject);
                }
            }

            serializableObjectContext.Name = serializableObjectContext.TargetObject.GetType().Name;

            return serializableObjectContext;
        }

        /// <summary>
        /// To optimize, we build a cache of created contexts
        /// and recycle them instead of desposing them.
        /// </summary>
        /// <param name="serializableObjectContext">
        /// Context to recycle.
        /// </param>
        internal
        static
        void
        RecycleContext(
            SerializableObjectContext   serializableObjectContext
            )
        {
            serializableObjectContext.Clear();
            lock (_stackLock)
            {
                _recycableSerializableObjectContexts.Push(serializableObjectContext);
            }
        }

        #endregion Internal Methods


        #region Public Properties

        /// <summary>
        /// Query target object
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
        /// Query/Set namespace informaiton table
        /// </summary>
        public
        MetroSerializationNamespaceTable
        NamespaceTable
        {
            get
            {
                return _namespaceTable;
            }

            set
            {
                _namespaceTable = value;
            }
        }


        /// <summary>
        /// Query/Set the collection containing all
        /// the properties of the current object
        /// </summary>
        public
        SerializablePropertyCollection
        PropertiesCollection
        {
            get
            {
                return _propertiesCollection;
            }
            set
            {
                _propertiesCollection = value;
            }
        }

        /// <summary>
        /// Query/Set the type of the property / object being
        /// considered for serialization.
        /// </summary>
        public
        bool
        IsComplexValue
        {
            get
            {
                return _isComplexValue;
            }

            set
            {
                _isComplexValue = value;
            }
        }


        /// <summary>
        /// Query / Set the readability type of the object
        /// </summary>
        public
        bool
        IsReadOnlyValue
        {
            get
            {
                return _isReadOnlyValue;
            }

            set
            {
                _isReadOnlyValue = value;
            }
        }

        #endregion Public Properties

        #region Public Methods


        /// <summary>
        /// Initialize the context
        /// </summary>
        public
        void
        Initialize(
            object                      target,
            SerializablePropertyContext serializablePropertyContext
            )
        {
            Initialize();
            _targetObject           = target;
            _isComplexValue         = false;
            _backingPropertyContext = serializablePropertyContext;

            if(_backingPropertyContext!=null)
            {
                _isComplexValue  = _backingPropertyContext.IsComplex;
                _isReadOnlyValue = _backingPropertyContext.IsReadOnly;
            }
            else
            {
                _isComplexValue  = true;
                _isReadOnlyValue = false;
            }
        }

        /// <summary>
        /// Clear the Context
        /// </summary>
        public
        override
        void
        Clear()
        {
            _targetObject    = null;
            _isComplexValue  = false;
            _isReadOnlyValue = false;
            _namespaceTable  = null;

            if (_propertiesCollection != null)
            {
                _propertiesCollection.Clear();
            }

            base.Clear();
        }

        #endregion Public Methods

        #region Private Data

        private
        object                               _targetObject;
        private
        MetroSerializationNamespaceTable     _namespaceTable;
        private
        SerializablePropertyCollection       _propertiesCollection;
        private
        bool                                 _isComplexValue;
        private
        bool                                 _isReadOnlyValue;
        private
        SerializablePropertyContext          _backingPropertyContext;
        static
        Stack                                _recycableSerializableObjectContexts;
        static
        object                               _stackLock = new Object();

        #endregion Private Data
    };
}
