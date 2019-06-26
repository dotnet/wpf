// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++

    Abstract:
        This file contains the definition of a class that defines
        the common functionality required to serialize a PrintTicket.

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
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a PrintTicket.
    /// </summary>
    internal class PrintTicketSerializer :
                   ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class PrintTicketSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        PrintTicketSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// The main method that is called to serialize a PrintTicket.
        /// </summary>
        /// <param name="serializedObject">
        /// Instance of object to be serialized.
        /// </param>
        public
        override
        void
        SerializeObject(
            object serializedObject
            )
        {
            PrintTicket printTicket = serializedObject as PrintTicket;

            if (printTicket == null)
            {
                //
                // Throw a meaningful exception
                //
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_TargetNotPrintTicket));
            }

            //  The new class XpsOMSerializationManager now also interacts with this class
            // the cast below is shorthand for cast to either XpsSerializationManager or XpsOMSerializationManager
            // we want this to throw an InvalidCastException if it fails to mantain compatibility.
            if ((IXpsSerializationManager)SerializationManager != null)
            {
                SerializationManager.
                     PackagingPolicy.PersistPrintTicket(printTicket);
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// The main method that is called to serialize the PrintTicket
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
            if(serializedProperty == null)
            {
                throw new ArgumentNullException("serializedProperty");
            }

            SerializeObject(serializedProperty.Value);
        }


        /// <summary>
        /// Persists the object for the print ticket but in this case it is
        /// not utilized
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Do nothing here
            //
        }

        #endregion Internal Methods
    };
}
