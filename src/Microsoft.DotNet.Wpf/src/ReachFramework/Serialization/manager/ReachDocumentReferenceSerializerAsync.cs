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
using System.Windows.Threading;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    internal class ReachDocumentReferenceSerializerAsync :
                   ReachSerializerAsync
    {
        /// <summary>
        /// Creates a new serailizer for a DocumentReference
        /// </summary>
        /// <param name="manager">serialization manager</param>
        public
        ReachDocumentReferenceSerializerAsync(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            
        }

        /// <summary>
        ///
        /// </summary>
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
                case SerializerAction.serializeDocument:
                {
                    SerializeDocument(context.ObjectContext);
                    break;
                }
                
                default:
                {
                    base.AsyncOperation(context);
                    break;
                }
            }
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
            if(serializableObjectContext.IsComplexValue)
            {

                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.serializeDocument);

                ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);

                SerializeObjectCore(serializableObjectContext);
            }
            else
            {
                // What about this case?  Is IsComplexValue something we really want to check for this?
            }
        }

        private object Idle(object sender)
        {
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        private
        void
        SerializeDocument(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Loads the document
            //
            FixedDocument document = 
            ((DocumentReference)serializableObjectContext.TargetObject).GetDocument(false);

            if (document.IsInitialized == false)
            {
                // Give a parser item a kick
                document.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
                    new DispatcherOperationCallback(Idle), null);
            }

            if (document != null)
            {
                ReachSerializer serializer = SerializationManager.GetSerializer(document);

                if(serializer!=null)
                {
                    serializer.SerializeObject(document);
                }
                else
                {
                    //
                    // This shouldn't ever happen.
                    //
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
                }
            }

        }
    };
}
