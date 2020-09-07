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

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a PageContentCollectionSerializer.
    /// </summary>
    internal class ReachPageContentCollectionSerializerAsync :
                   ReachSerializerAsync
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
        ReachPageContentCollectionSerializerAsync(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        #endregion Constructor
        
        #region Public Mehods
        
        public
        override
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
                case SerializerAction.endPersistObjectData:
                {
                    EndPersistObjectData();
                    break;
                }

                case SerializerAction.serializeNextPageContent:
                {
                    PageContentCollectionSerializerContext thisContext = 
                    context as PageContentCollectionSerializerContext;

                    if(thisContext != null)
                    {
                        SerializeNextPageContent(thisContext.Enumerator,
                                                 thisContext.ObjectContext);
                    }

                    break;
                }
                
                default:
                {
                    base.AsyncOperation(context);
                    break;
                }
            }
        }


        #endregion

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

        internal
        override
        void
        EndPersistObjectData(
            )
        {
            //
            // do nothing in this stage
            //
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
            IEnumerator enumerator = ((IEnumerable)serializableObjectContext.TargetObject).GetEnumerator();
            enumerator.Reset();

            PageContentCollectionSerializerContext
            context = new PageContentCollectionSerializerContext(this,
                                                                serializableObjectContext,
                                                                enumerator,
                                                                SerializerAction.serializeNextPageContent);

            ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);
        }

       private
       void
       SerializeNextPageContent(
           IEnumerator                 enumerator,
           SerializableObjectContext   serializableObjectContext
           )
       {
           if(enumerator.MoveNext())
           {

               PageContentCollectionSerializerContext
               context = new PageContentCollectionSerializerContext(this,
                                                                   serializableObjectContext,
                                                                   enumerator,
                                                                   SerializerAction.serializeNextPageContent);


               ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);

               object pageContent = enumerator.Current;

               SerializePageContent(pageContent);
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
            ReachSerializer serializer = SerializationManager.GetSerializer(pageContent);

            if(serializer!=null)
            {
                serializer.SerializeObject(pageContent);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }
        }

        #endregion Private Methods
    };

    internal class PageContentCollectionSerializerContext :
                   ReachSerializerContext
    {
        public
        PageContentCollectionSerializerContext(
            ReachSerializerAsync        serializer,
            SerializableObjectContext   objectContext,
            IEnumerator                 enumerator,
            SerializerAction            action
            ):
            base(serializer,objectContext,action)
        {
            this._enumerator = enumerator;
        }


        public
        IEnumerator
        Enumerator
        {
            get
            {
                return _enumerator;
            }
        }

        private
        IEnumerator     _enumerator;
    };
}
