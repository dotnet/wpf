// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file contains the definition of a class that defines
        the common functionality required to serialize a
        PageContentCollection
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
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a PageContentCollectionSerializer.
    /// </summary>
    internal class ReachPageContentCollectionSerializer :
                   ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class ReachPageContentCollectionSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        ReachPageContentCollectionSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        #endregion Constructor

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
            SerializePageContents(serializableObjectContext);
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// This is being called to serialize the Page Content items
        /// contained within the collection
        /// </summary>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        private
        void
        SerializePageContents(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Serialize each PageContent in PageContentColleciton
            //
            foreach (object pageContent in (IEnumerable)serializableObjectContext.TargetObject)
            {
                if (pageContent != null)
                {
                    // Serialize the current item
                    SerializePageContent(pageContent);
                }
            }
        }


        /// <summary>
        /// Called to serialize a single PageContent
        /// </summary>
        /// <param name="pageContent">
        /// The PageContent to be serialized.
        /// </param>
        private
        void
        SerializePageContent(
            object pageContent
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSavePageBegin);

            ReachSerializer serializer = SerializationManager.GetSerializer(pageContent);

            if(serializer!=null)
            {
                serializer.SerializeObject(pageContent);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSavePageEnd);
        }

        #endregion Private Methods
    };
}
