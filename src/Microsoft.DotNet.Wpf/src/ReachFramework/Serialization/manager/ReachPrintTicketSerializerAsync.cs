// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++

    Abstract:
        This file contains the definition of a class that defines
        the common functionality required to serialize a PrintTicket.
--*/
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a PrintTicket.
    /// </summary>
    internal class PrintTicketSerializerAsync :
                   ReachSerializerAsync
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
        PrintTicketSerializerAsync(
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
                throw new XpsSerializationException(SR.ReachSerialization_TargetNotPrintTicket);
            }

            ((XpsSerializationManagerAsync)SerializationManager).
            PackagingPolicy.PersistPrintTicket(printTicket);
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
            ArgumentNullException.ThrowIfNull(serializedProperty);

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
