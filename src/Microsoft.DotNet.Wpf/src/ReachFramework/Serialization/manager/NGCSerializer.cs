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
using System.Printing;
using MS.Utility;

//
// Ngc = Next Generation Converter. It means to convert the avalon element tree
//  to the downlevel GDI primitives.
//
// NgcSerializationManger will call ReachSerializer base class to invoke
// the Ngc serilizer. NgCSerializationManger.GetSerializerType will link
// the serilizer with each different type.
// Ngc will create the different serializer for the following reach serializer
//      ReachFixedDocumentSerializer
//      ReachFixedPageSerializer
//      ReachVisualSerializer
//      ReachDocumentPageSerializer
//      ReachDocumentSequenceSerializer
//      ReachIDocumentPaginatorSerializer
//      ReachPageContentCollectionSerializer
//      ReachPageContentSerializer
//      ReachUIElementCollectionSerializer

namespace System.Windows.Xps.Serialization
{
    static class NgcSerializerUtil
    {
        internal static String InferJobName(object  o)
        {
            String  jobName = null;

            if(o != null)
            {
                IFrameworkInputElement inputElement = o as IFrameworkInputElement;
                if (inputElement != null)
                {
                    jobName = inputElement.Name;
                }
                if (jobName == null || jobName.Length == 0)
                {
                    jobName = o.ToString();
                }
            }

            return jobName;
        }
    }

    /// <summary>
    ///
    /// </summary>
    internal class NgcFixedDocumentSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcFixedDocumentSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }
            FixedDocument fd = serializedObject as FixedDocument;
            if( fd == null )
            {

               throw new ArgumentException(SR.Get(SRID.ReachSerialization_ExpectedFixedDocument));
            }
            NgcSerializationManager ngcManager = SerializationManager as NgcSerializationManager;

            ngcManager.StartDocument(fd,true);

            ReachSerializer serializer = ngcManager.GetSerializer(fd.Pages);
            serializer.SerializeObject(fd.Pages);

            ngcManager.EndDocument();
        }


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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcFixedPageSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcFixedPageSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            NgcSerializationManager ngcManager = SerializationManager as NgcSerializationManager;
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            FixedPage fp = serializedObject as FixedPage;
            if( fp == null )
            {

               throw new ArgumentException(SR.Get(SRID.ReachSerialization_ExpectedFixedPage));
            }

            bool bManualStartDoc = ngcManager.StartPage();

            Size pageSize = new Size(fp.Width, fp.Height);
            ngcManager.PageSize = pageSize;

            Visual visual = (Visual)serializedObject as Visual;

            if (visual != null)
            {
                ngcManager.WalkVisual(visual);
            }

            ngcManager.EndPage();
            if (bManualStartDoc)
            {
                ngcManager.EndDocument();
            }
        }

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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };


    /// <summary>
    ///
    /// </summary>
    internal class NgcReachVisualSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcReachVisualSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {

            NgcSerializationManager NgcManager = SerializationManager as NgcSerializationManager;

            Visual visual = (Visual)serializedObject as Visual;

            if (visual != null)
            {
                NgcManager.WalkVisual(visual);
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        SerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            //
            // Do nothing here.
            // We do not support serializing visuals that come in as
            // properties out of context of a FixedPage or a DocumentPage
            //
        }

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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };



    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentPageSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentPageSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }


        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {

            DocumentPage dp = serializedObject as DocumentPage;
            if (dp != null)
            {
                Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetVisualStart);
                Visual pageRootVisual = dp.Visual;
                Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetVisualEnd);

                NgcSerializationManager NgcManager = SerializationManager as NgcSerializationManager;

                bool bManualStartDoc = NgcManager.StartPage();

                ReachSerializer serializer = SerializationManager.GetSerializer(pageRootVisual);
                serializer.SerializeObject(pageRootVisual);

                NgcManager.EndPage();
                if (bManualStartDoc)
                {
                    NgcManager.EndDocument();
                }
            }
        }


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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentPaginatorSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentPaginatorSerializer(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            NgcSerializationManager ngcManager = SerializationManager as NgcSerializationManager;
            DocumentPaginator paginator = (DocumentPaginator)serializedObject;

            //
            // For FlowDocument, the application might attach a PrintTicket DP on it.
            //
            DependencyObject dependencyObject = paginator != null ? paginator.Source as DependencyObject : null;
            if (dependencyObject != null)
            {
                if(!ngcManager.IsPrintTicketEventHandlerEnabled)
                {
                    //ngcManager.FdPrintTicket = dependencyObject.GetValue(FixedDocument.PrintTicketProperty) as PrintTicket;
                }
            }

            ngcManager.StartDocument(paginator,true);

            if (paginator != null)
            {
                for (int i = 0; !paginator.IsPageCountValid || (i < paginator.PageCount); i++)
                {
                    DocumentPage page = Toolbox.GetPage(paginator, i);

                    ReachSerializer serializer = SerializationManager.GetSerializer(page);

                    if (serializer != null)
                    {
                        serializer.SerializeObject(page);
                    }
                }
            }

            ngcManager.EndDocument();
        }

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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }

    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentSequenceSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentSequenceSerializer(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }
            FixedDocumentSequence fds = serializedObject as FixedDocumentSequence;
            if( fds == null )
            {

               throw new ArgumentException(SR.Get(SRID.ReachSerialization_ExpectedFixedDocumentSequence));
            }

            NgcSerializationManager ngcManager = SerializationManager as NgcSerializationManager;

            if(!ngcManager.IsPrintTicketEventHandlerEnabled)
            {
                //ngcManager.FdsPrintTicket = fds.PrintTicket as PrintTicket;
            }
            else
            {
                XpsSerializationPrintTicketRequiredEventArgs printTicketEvent =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                ngcManager.OnNGCSerializationPrintTicketRequired(printTicketEvent);
            }

            ngcManager.StartDocument(fds,false);

            ReachSerializer serializer = ngcManager.GetSerializer(fds.References);
            serializer.SerializeObject(fds.References);

            ngcManager.EndDocument();

            XpsSerializationProgressChangedEventArgs e =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentSequenceWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            ngcManager.OnNGCSerializationProgressChagned(e);
        }

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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }

     };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentReferenceCollectionSerializer :
                 ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentReferenceCollectionSerializer(
            PackageSerializationManager manager
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
        SerializeObject(
            Object serializedObject
            )
        {
            SerializeDocumentReferences( serializedObject );
        }

        /// <summary>
        /// This is being called to serialize the Document Reference items
        /// contained within the colleciton
        /// </summary>
        private
        void
        SerializeDocumentReferences(
            object serializableObject
            )
        {
            System.Collections.Generic.IEnumerable<DocumentReference> drList = (System.Collections.Generic.IEnumerable<DocumentReference>)serializableObject;
            //
            // Serialize each PageContent in PageContentColleciton
            //
            if (drList != null)
            {
                foreach (object documentReference in drList)
                {
                    if (documentReference != null)
                    {
                        //
                        // Serialize the current ui element
                        //
                        SerializeDocumentReference((DocumentReference)documentReference);
                    }
                }
            }
        }


        /// <summary>
        ///     Called to serialize a single DocumentReferenceElement
        /// </summary>
        private
        void
        SerializeDocumentReference(
            DocumentReference dre
            )
        {
            IDocumentPaginatorSource idp = dre.GetDocument(false);

            if (idp != null)
            {
                FixedDocument fixedDoc = idp as FixedDocument;

                if (fixedDoc != null)
                {
                    ReachSerializer serializer = SerializationManager.GetSerializer(fixedDoc);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(fixedDoc);
                    }
                }
                else
                {
                    ReachSerializer serializer = SerializationManager.GetSerializer(idp.DocumentPaginator);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(idp);
                    }
                }
            }
        }

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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };

    /// <summary>
    /// Class defining common functionality required to
    /// serialize a PageContentCollectionSerializer.
    /// </summary>
    internal class NGCReachPageContentCollectionSerializer :
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
        NGCReachPageContentCollectionSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        #endregion Constructor


        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            IEnumerable enumerableObject =  serializedObject as IEnumerable;

            if (enumerableObject == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.MustBeOfType, "serializableObjectContext.TargetObject", typeof(IEnumerable)));
            }

            //
            // Serialize the PageContent Items contained within the collection
            //
            SerializePageContents(enumerableObject);
        }

        #region Private Methods

        /// <summary>
        /// This is being called to serialize the Page Content items
        /// contained within the collection
        /// </summary>
        /// <param name="enumerableObject">
        /// The context of the object to be serialized at this time.
        /// </param>
        private
        void
        SerializePageContents(
             IEnumerable enumerableObject
            )
        {
            //
            // Serialize each PageContent in PageContentColleciton
            //
            foreach (object pageContent in enumerableObject)
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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };

    /// <summary>
    /// Class defining common functionality required to
    /// serialize a ReachPageContentSerializer.
    /// </summary>
    internal class NGCReachPageContentSerializer :
                   ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class ReachPageContentSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        NGCReachPageContentSerializer(
            PackageSerializationManager   manager
            ):
        base(manager)
        {

        }

        #endregion Constructor
        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            FixedPage fixedPage = Toolbox.GetPageRoot(serializedObject);

            if(fixedPage != null)
            {
                ReachSerializer serializer = SerializationManager.GetSerializer(fixedPage);

                if(serializer!=null)
                {
                    NgcSerializationManager manager = SerializationManager as NgcSerializationManager;

                    XpsSerializationPrintTicketRequiredEventArgs e =
                        new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                                                                 0);

                    manager.OnNGCSerializationPrintTicketRequired(e);

                    Toolbox.Layout(fixedPage, manager.GetActivePrintTicket());
                    serializer.SerializeObject(fixedPage);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
                }
            }
        }


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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class defining common functionality required to
    /// serialize a UIElementCollection.
    /// </summary>
    internal class NGCReachUIElementCollectionSerializer :
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
        NGCReachUIElementCollectionSerializer(
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
           IEnumerable enumerableObject = serializedObject as IEnumerable;

            if (enumerableObject == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.MustBeOfType, "serializableObjectContext.TargetObject", typeof(IEnumerable)));
            }

            //
            // Serialize the PageContent Items contained within the collection
            //
            SerializeUIElements(enumerableObject);
       }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// This is being called to serialize the Page Content items
        /// contained within the collection
        /// </summary>
        private
        void
        SerializeUIElements(
            IEnumerable enumerableObject
            )
        {
            //
            // Serialize each PageContent in PageContentCollection
            //
            foreach (object uiElement in enumerableObject)
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
            //
            //We should not hit this method.
            //
            throw new NotImplementedException();
        }
    };
}


