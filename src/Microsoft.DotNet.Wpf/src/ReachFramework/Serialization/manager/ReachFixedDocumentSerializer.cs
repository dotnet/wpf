// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                                         
    Abstract:
        This file contains the definition of a class that defines
        the common functionality required to serialize a FixedDocument                                                                    
--*/
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a FixedDocument.
    /// </summary>
    internal class FixedDocumentSerializer :
                   ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class FixedDocumentSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        FixedDocumentSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            
        }

        #endregion Constructor

        #region Public Methods
        
        /// <summary>
        /// The main method that is called to serialize a FixedDocument.
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
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = new Dictionary<int,Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedObject);
        }

        #endregion Public Methods

        #region Internal Methods
        
        /// <summary>
        /// The main method that is called to serialize the FixedDocument
        /// and that is usually called from within the serialization manager 
        /// when a node in the graph of objects is at a turn where it should 
        /// be serialized.
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
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = new Dictionary<int,Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedProperty);
        }

        /// <summary>
        /// The method is called once the object data is discovered at that 
        /// point of the serizlization process.
        /// </summary>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if(serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            String xmlnsForType = SerializationManager.GetXmlNSForType(typeof(FixedDocument));

            if( SerializationManager is XpsSerializationManager)
            {
               (SerializationManager as XpsSerializationManager).RegisterDocumentStart();
            }
            
            if(xmlnsForType == null)
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name);
            }
            else
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name,
                                            xmlnsForType);
            }

            {
                if(serializableObjectContext.IsComplexValue)
                {
                    XpsSerializationPrintTicketRequiredEventArgs e = 
                    new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                     0);

                    ((IXpsSerializationManager)SerializationManager).OnXPSSerializationPrintTicketRequired(e);

                    //
                    // Serialize the data for the PrintTicket
                    //
                    if(e.Modified)
                    {
                        if(e.PrintTicket != null)
                        {
                            PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                            serializer.SerializeObject(e.PrintTicket);
                        }
                    }

                    SerializeObjectCore(serializableObjectContext);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedDocument));
                }
            }

            //
            // Clear off the table from the packaging policy
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = null;

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = null;

            XmlWriter.WriteEndElement();
            XmlWriter = null;

            //
            // Signal to any registered callers that the Document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent = 
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            ((IXpsSerializationManager)SerializationManager).OnXPSSerializationProgressChanged(progressEvent);

            if( SerializationManager is XpsSerializationManager)
            {
               (SerializationManager as XpsSerializationManager).RegisterDocumentEnd();
            }
        }
    
        /// <summary>
        /// This method is the one that writed out the attribute within
        /// the xml stream when serializing simple properties.
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

            String attributeValue = String.Empty;

            attributeValue = GetValueOfAttributeAsString(serializablePropertyContext);

            if ( (attributeValue != null) && 
                 (attributeValue.Length > 0) )
            {
                //
                // Emit name="value" attribute
                //
                XmlWriter.WriteAttributeString(serializablePropertyContext.Name, attributeValue);
            }
        }


        /// <summary>
        /// Converts the Value of the Attribute to a String by calling into 
        /// the appropriate type converters.
        /// </summary>
        /// <param name="serializablePropertyContext">
        /// The property that is to be serialized as an attribute at this time.
        /// </param>
        internal
        String
        GetValueOfAttributeAsString(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            String valueAsString                  = null;
            Object targetObjectContainingProperty = serializablePropertyContext.TargetObject;
            Object propertyValue                  = serializablePropertyContext.Value;

            if(propertyValue != null)
            {
                TypeConverter typeConverter = serializablePropertyContext.TypeConverter;

                valueAsString = typeConverter.ConvertToInvariantString(new XpsTokenContext(SerializationManager,
                                                                                             serializablePropertyContext),
                                                                       propertyValue);


                if (typeof(Type).IsInstanceOfType(propertyValue))
                {
                    int index = valueAsString.LastIndexOf('.');

                    if (index > 0)
                    {
                        valueAsString = valueAsString.Substring(index + 1);
                    }

                    valueAsString = XpsSerializationManager.TypeOfString + valueAsString + "}";
                }
            }
            else
            {
                valueAsString = XpsSerializationManager.NullString;
            }

            return valueAsString;
        }

        #endregion Internal Methods

        #region Public Properties
        
        /// <summary>
        /// Queries / Set the XmlWriter for a FixedDocument
        /// </summary>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                if(base.XmlWriter == null)
                {
                    base.XmlWriter = SerializationManager.AcquireXmlWriter(typeof(FixedDocument));
                }

                return base.XmlWriter;
            }

            set
            {
                base.XmlWriter = null;
                SerializationManager.ReleaseXmlWriter(typeof(FixedDocument));
            }
        }

        #endregion Public Properties

    };
}
    

