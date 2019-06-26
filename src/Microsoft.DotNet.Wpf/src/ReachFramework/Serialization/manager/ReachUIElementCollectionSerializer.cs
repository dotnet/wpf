// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                                           
    Abstract:
        This file contains the definition of a class that defines
        the common functionality required to serialize a 
        UIElementCollection

                                                                        
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
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a UIElementCollection.
    /// </summary>
    internal class ReachUIElementCollectionSerializer :
                   ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class ReachUIElementCollectionSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        ReachUIElementCollectionSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// The main method that is called to serialize a UIElementCollection.
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
            base.SerializeObject(serializedObject);
        }

        #endregion Public Methods

        #region Internal Methods
        
        /// <summary>
        /// The method is called once the object data is discovered at that 
        /// point of the serialization process.
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
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            IEnumerable enumerableObject = serializableObjectContext.TargetObject as IEnumerable;

            if (enumerableObject == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.MustBeOfType, "serializableObjectContext.TargetObject", typeof(IEnumerable)));
            }

            //
            // Serialize the PageContent Items contained within the collection 
            //
            SerializeUIElements(serializableObjectContext);
        }

        #endregion Internal Methods


        #region Private Methods

        /// <summary>
        /// This is being called to serialize the Page Content items
        /// contained within the collection
        /// </summary>
        private
        void 
        SerializeUIElements(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Serialize each PageContent in PageContentCollection
            //
            foreach (object uiElement in (IEnumerable)serializableObjectContext.TargetObject)
            {
                if (uiElement != null)
                {
                    //
                    // Serialize the current ui element
                    //
                    SerializeUIElement(uiElement);
                }
            }
        }


        /// <summary>
        /// Called to serialize a single UIElement
        /// </summary>
        private 
        void 
        SerializeUIElement(
            object uiElement
            )
        {
            Visual visual = uiElement as Visual;

            if(visual != null)
            {
                ReachSerializer serializer = SerializationManager.GetSerializer(visual);

                if(serializer!=null)
                {
                    serializer.SerializeObject(visual);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
                }
            }
        }

        #endregion Private Methods
    };
}
