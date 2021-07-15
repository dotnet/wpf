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
    /// <summary>
    /// 
    /// </summary>
    internal class ReachDocumentReferenceCollectionSerializer :
                   ReachSerializer
    {
        /// <summary>
        /// Creates new serializer for a DocumentReferenceCollection
        /// </summary>
        /// <param name="manager">serialization manager for this seriaizer</param>
        public
        ReachDocumentReferenceCollectionSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        /// <summary>
        /// 
        /// </summary>
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

            // get DocumentReferenceCollection
            System.Collections.Generic.IEnumerable<DocumentReference> enumerableObject = serializableObjectContext.TargetObject as System.Collections.Generic.IEnumerable<DocumentReference>;

            if (enumerableObject == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.MustBeOfType, "serializableObjectContext.TargetObject", typeof(System.Collections.Generic.IEnumerable<DocumentReference>)));
            }

            SerializeDocumentReferences(serializableObjectContext);
        }

        /// <summary>
        /// This is being called to serialize the DocumentReference items
        /// contained within the colleciton
        /// </summary>
        private
        void
        SerializeDocumentReferences(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Serialize each DocumentReference in DocumentReferenceColleciton
            //
            foreach (object documentReference in (System.Collections.Generic.IEnumerable<DocumentReference>)serializableObjectContext.TargetObject)
            {
                if (documentReference != null)
                {
                    // Serialize the current item
                    SerializeDocumentReference(documentReference);
                }
            }
        }


        /// <summary>
        ///     Called to serialize a single DocumentReference
        /// </summary>
        private 
        void
        SerializeDocumentReference(
            object documentReference
            )
        {
            ReachSerializer serializer = SerializationManager.GetSerializer(documentReference);

            if(serializer!=null)
            {
                serializer.SerializeObject(documentReference);
            }
            else
            {
                // should we throw if this is not a DocumentReference or just not do anything?
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }
        }
    };
}
