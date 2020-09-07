// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                                        
    Abstract:
        This file contains the definition of a Base class that defines
        the common functionality required to serialie on type in a 
        graph of types rooted by some object instance
                
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

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Base class defining common functionalities required to
    /// serialize one type.
    /// </summary>
    internal abstract class ReachSerializerAsync :
                            ReachSerializer
    {
        #region Constructor
        
        /// <summary>
        /// Constructor for class ReachSerializer
        /// </summary>
        /// <param name="manager">
        /// The serializtion manager, the services of which are
        /// used later for the serialization process of the type.
        /// </param>
        public
        ReachSerializerAsync(
            PackageSerializationManager   manager
            )
        {
            if(manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            _serializationManager = manager as IXpsSerializationManagerAsync;

            if(_serializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotXpsSerializationManagerAsync));
            }

            _xmlWriter            = null;
        }

        #endregion Constructor

        #region Public Methods


        public
        virtual
        void
        AsyncOperation(
            ReachSerializerContext context
            )
        {
            if(context == null)
            {

            }
           
            switch (context.Action) 
            {
                case SerializerAction.endSerializeObject:
                {
                    EndSerializeObject(context.ObjectContext);
                    break;
                }

                case SerializerAction.serializeNextProperty:
                {
                    SerializeNextProperty(context.ObjectContext);
                    break;
                }
            }
        }

        /// <summary>
        /// The main method that is called to serialize the object of
        /// that given type.
        /// </summary>
        /// <param name="serializedObject">
        /// Instance of object to be serialized.
        /// </param>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            BeginSerializeObject(serializedObject);
        }

        #endregion Public Methods
        
        
        #region Internal Methods

        /// <summary>
        /// The main method that is called to serialize the object of
        /// that given type and that is usually called from within the
        /// serialization manager when a node in the graph of objects is
        /// at a turn where it should be serialized.
        /// </summary>
        /// <param name="serializedProperty">
        /// The context of the property being serialized at this time and
        /// it points internally to the object encapsulated by that node.
        /// </param>
        internal
        override
        void
        SerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            BeginSerializeObject(serializedProperty);
        }

        internal
        virtual
        void
        BeginSerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            if(serializedProperty == null)
            {
                throw new ArgumentNullException("serializedProperty");
            }

            if(SerializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_MustHaveSerializationManager));
            }

            //
            // At this stage discover the graph of properties of the object that
            // need to be serialized
            //                      
            SerializableObjectContext serializableObjectContext = DiscoverObjectData(serializedProperty.Value,
                                                                                     serializedProperty);

            if(serializableObjectContext!=null)
            {
                //
                // Push the object at hand on the context stack
                //
                SerializationManager.GraphContextStack.Push(serializableObjectContext);

                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.endSerializeObject);

                _serializationManager.OperationStack.Push(context);
                //
                // At this stage we should start streaming the markup representing the
                // object graph to the corresponding destination
                //
                PersistObjectData(serializableObjectContext);

            }
        }
        
        /// <summary>
        ///
        /// </summary>
        internal
        virtual
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            if(serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }
            if(SerializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_MustHaveSerializationManager));
            }
            //
            // At this stage discover the graph of properties of the object that
            // need to be serialized
            //                      
            SerializableObjectContext serializableObjectContext = DiscoverObjectData(serializedObject,
                                                                                     null);

            if(serializableObjectContext!=null)
            {
                //
                // Push the object at hand on the context stack
                //
                SerializationManager.GraphContextStack.Push(serializableObjectContext);

                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.endSerializeObject);

                _serializationManager.OperationStack.Push(context);
                //
                // At this stage we should start streaming the markup representing the
                // object graph to the corresponding destination
                //
                PersistObjectData(serializableObjectContext);
            }
         }

        internal
        virtual
        void
        EndSerializeObject(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Pop the object from the context stack
            //
            SerializationManager.GraphContextStack.Pop();

            //
            // Recycle the used SerializableObjectContext
            //
            SerializableObjectContext.RecycleContext(serializableObjectContext);
        }


        /*/// <summary>
        /// The method is called once the object data is discovered at that 
        /// point of the serialization process.
        /// </summary>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        abstract
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            );*/


        /// <summary>
        ///
        /// </summary>
        internal
        virtual
        void
        EndPersistObjectData(
            )
        {
            //
            // Do nothing in the base class
            //
        }


        /// <summary>
        ///     Serialize the properties within the object
        ///     context into METRO
        /// </summary>
        /// <remarks>
        ///     Method follows these steps
        ///     1. Serializes the instance as string content 
        ///         if is not meant to be a complex value. Else ...
        ///     2. Serialize Properties as attributes
        ///     3. Serialize Complex Properties as separate parts
        ///         through calling separate serializers
        ///     Also this is the virtual to override custom attributes or 
        ///     contents need to be serialized
        /// </remarks>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        override
        void
        SerializeObjectCore(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            if (!serializableObjectContext.IsReadOnlyValue && 
                serializableObjectContext.IsComplexValue)
            {
                SerializeProperties(serializableObjectContext);
            }
        }

        /// <summary>
        /// This method is the one that writes out the attribute within
        /// the xml stream when serializing simple properites.
        /// </summary>
        /// <param name="serializablePropertyContext">
        /// The property that is to be serialized as an attribute at this time.
        /// </param>
        internal
        override
        void
        WriteSerializedAttribute(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }
        }

        #endregion Internal Methods
        
        #region Private Methods

        /// <summary>
        /// This method is the one that parses down the object at hand
        /// to discover all the properties that are expected to be serialized
        /// at that object level.
        /// the xml stream when serializing simple properties.
        /// </summary>
        /// <param name="serializedObject">
        /// The instance of the object being serialized.
        /// </param>
        /// <param name="serializedProperty">
        /// The instance of property on the parent object from which this 
        /// object stemmed. This could be null if this is the node object
        /// or the object has no parent.
        /// </param>
        private
        SerializableObjectContext
        DiscoverObjectData(
            Object                      serializedObject,
            SerializablePropertyContext serializedProperty
            )
        {
            //
            // Trying to figure out the parent of this node, which is at this stage
            // the same node previously pushed on the stack or in other words it is
            // the node that is currently on the top of the stack
            //
            SerializableObjectContext 
            serializableObjectParentContext = (SerializableObjectContext)SerializationManager.
                                              GraphContextStack[typeof(SerializableObjectContext)];
            //
            // Create the context for the current object
            //
            SerializableObjectContext serializableObjectContext = 
            SerializableObjectContext.CreateContext(SerializationManager, 
                                                    serializedObject, 
                                                    serializableObjectParentContext,
                                                    serializedProperty);

            //
            // Set the root object to be serialized at the level of the SerializationManager
            //
            if(SerializationManager.RootSerializableObjectContext == null)
            {
                SerializationManager.RootSerializableObjectContext = serializableObjectContext;
            }

            return serializableObjectContext;
        }

        /// <summary>
        /// Trigger all properties serialization
        /// </summary>
        private
        void
        SerializeProperties(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            SerializablePropertyCollection propertyCollection = serializableObjectContext.PropertiesCollection;

            if(propertyCollection!=null)
            {
                //for(propertyCollection.Reset();
                //    propertyCollection.MoveNext();)
                //{
                //    SerializablePropertyContext serializablePropertyContext = 
                //    (SerializablePropertyContext)propertyCollection.Current;
                //
                //    if(serializablePropertyContext!=null)
                //    {
                //        SerializeProperty(serializablePropertyContext);
                //    }
                //}
                propertyCollection.Reset();
                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.serializeNextProperty);

                _serializationManager.OperationStack.Push(context);
            }
        }

        private
        void
        SerializeNextProperty(
            SerializableObjectContext   serializableObjectContext
            )
        {
            SerializablePropertyCollection propertyCollection = serializableObjectContext.PropertiesCollection;

            if(propertyCollection.MoveNext())
            {
                SerializablePropertyContext serializablePropertyContext = 
                (SerializablePropertyContext)propertyCollection.Current;

                if(serializablePropertyContext!=null)
                {

                    ReachSerializerContext context = new ReachSerializerContext(this,
                                                                                serializableObjectContext,
                                                                                SerializerAction.serializeNextProperty);

                    _serializationManager.OperationStack.Push(context);
                    SerializeProperty(serializablePropertyContext);
                }

            }
        }

        /// <summary>
        /// Trigger serializing one property at a time.
        /// </summary>
        private
        void
        SerializeProperty(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            if(!serializablePropertyContext.IsComplex)
            {
                //
                // Non-Complex Properties are serialized as attributes
                //
                WriteSerializedAttribute(serializablePropertyContext);
            }
            else
            {
                //
                // Complex properties could be treated in different ways
                // based on their type. Examples of that are:
                //
                //
                //
                ReachSerializer serializer = SerializationManager.GetSerializer(serializablePropertyContext.Value);

                // If there is no serializer for this type, we won't serialize this property
                if(serializer!=null)
                {
                    serializer.SerializeObject(serializablePropertyContext);
                }
            }
        }

        #endregion Private Methods
        
        #region Public Properties

        /// <summary>
        /// Query / Set Xml Writer for the equivelan part
        /// </summary>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                return _xmlWriter;
            }

            set
            {
                _xmlWriter = value;
            }
        }

        /// <summary>
        /// Query the SerializationManager used by this serializer.
        /// </summary>
        public
        override
        PackageSerializationManager
        SerializationManager
        {
            get
            {
                return (PackageSerializationManager)_serializationManager;
            }
        }

        #endregion Public Properties
        
        #region Private Data members

        private
        IXpsSerializationManagerAsync   _serializationManager;

        private 
        XmlWriter                        _xmlWriter;
        
        #endregion Private Data members
    };

    internal enum SerializerAction
    {
        endSerializeObject             = 1,
        endPersistObjectData           = 2,
        serializeNextProperty          = 3,
        serializeNextDocumentReference = 4,
        serializeDocument              = 5,
        serializeNextPageContent       = 6,
        serializePage                  = 7,
        endSerializeReachFixedPage     = 8,
        serializeNextUIElement         = 9,
        serializeNextTreeNode          = 10,
        serializeNextDocumentPage      = 11,
        endSerializeDocumentPage       = 12
    };

    internal class ReachSerializerContext
    {

        public
        ReachSerializerContext(
            ReachSerializerAsync        serializer,
            SerializerAction            action
            )
        {
            this._action        = action;
            this._serializer    = serializer;
            this._objectContext = null;
        }

        public
        ReachSerializerContext(
            ReachSerializerAsync        serializer,
            SerializableObjectContext   objectContext,
            SerializerAction            action
            )
        {
            this._action        = action;
            this._serializer    = serializer;
            this._objectContext = objectContext;
        }

        public
        virtual
        SerializerAction
        Action
        {
            get
            {
                return _action;
            }
        }

        public
        virtual
        ReachSerializerAsync
        Serializer
        {
            get
            {
                return _serializer;
            }
        }

        public
        virtual
        SerializableObjectContext
        ObjectContext
        {
            get
            {
                return _objectContext;
            }
        }

        private
        SerializerAction            _action;

        private
        ReachSerializerAsync        _serializer;

        private
        SerializableObjectContext   _objectContext;
    };
}
