// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __LOCALPRINTSERVER_HPP__
#define __LOCALPRINTSERVER_HPP__

namespace System
{
namespace Printing
{
    /// <summary>
    /// Enumeration of properties of the LocalPrintServer object.
    /// </summary>
    /// <ExternalAPI/>    
    public enum class LocalPrintServerIndexedProperty
    {
        DefaultSpoolDirectory       ,
        PortThreadPriority          ,
        DefaultPortThreadPriority   ,
        SchedulerPriority           ,
        DefaultSchedulerPriority    ,
        BeepEnabled                 ,
        NetPopup                    ,
        EventLog                    ,
        MajorVersion                ,
        MinorVersion                ,
        RestartJobOnPoolTimeout     ,
        RestartJobOnPoolEnabled     ,
        DefaultPrintQueue           
    };

    /// <summary>
    /// This class abstracts the functionality of a local print server.
    /// </summary>
    /// <ExternalAPI/>
    public ref class LocalPrintServer sealed :
    public PrintServer
    {
        public:

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class. 
        /// The object is bound to the print server hosted by the current machine.
        /// </summary>        
        LocalPrintServer(
            void
            );

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class. Properties referenced 
        /// in the propertiesFilter are initialized.
        /// The object is bound to the print server hosted by the current machine.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        LocalPrintServer(
            array<LocalPrintServerIndexedProperty>^   propertiesFilter
            );

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class. Properties referenced 
        /// in the propertiesFilter are initialized.
        /// The object is bound to the print server hosted by the current machine.
        /// </summary>  
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        LocalPrintServer(
            array<String^>^                    propertiesFilter
            );

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class. The object is bound to the local print server
        /// and asks for permissions to be granted as specified in the desiredAccess parameter.
        /// </summary>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>
        LocalPrintServer(
            PrintSystemDesiredAccess   desiredAccess
            );

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class.  
        /// Only the properties referenced in the propertiesFilter are initialized. 
        /// The object is bound to the local print server and 
        /// asks for permissions to be granted as specified in the desiredAccess parameter.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>
        LocalPrintServer(
            array<LocalPrintServerIndexedProperty>^    propertiesFilter,
            PrintSystemDesiredAccess                   desiredAccess
            );

        /// <summary>
        /// Creates a new instance of the LocalPrintServer class.  
        /// Only the properties referenced in the propertiesFilter are initialized. 
        /// The object is bound to the local print server and 
        /// asks for permissions to be granted as specified in the desiredAccess parameter.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>
        LocalPrintServer(
            array<String^>^             propertiesFilter,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <value>
        /// Default print queue property.<see cref="PrintQueue"/>
        /// </value>
        property
        PrintQueue^
        DefaultPrintQueue
        {
            void set(PrintQueue^    printQueue);
            PrintQueue^ get();
        }

        /// <summary>
        /// Returns the default print queue
        /// </summary> 
        static
        PrintQueue^
        GetDefaultPrintQueue(
            void
            );

        /// <summary>
        /// Creates a connection to the print queue identified by printQueue object.
        /// </summary>        
        /// <param name="printQueue">
        /// PrintQueue object that identifies the printer to make the connection to.
        /// </param>                
        /// <returns>
        ///  Return true if succeeded.
        /// </returns>
        bool
        ConnectToPrintQueue(
            PrintQueue^                printer
            );

        /// <summary>
        /// Creates a connection to the print queue identified by printQueue name.
        /// </summary>        
        /// <param name="printQueuePath">
        /// String that identifies the printer to make the connection to.
        /// </param>                
        /// <returns>
        ///  Return true if succeeded.
        /// </returns>
        bool
        ConnectToPrintQueue(
            String^                    printQueuePath
            );

        /// <summary>
        /// Deletes an existing to the print queue identified by printQueue name.
        /// </summary>        
        /// <param name="printQueuePath">
        /// String that identifies the printer connection.
        /// </param>                
        /// <returns>
        ///  Return true if succeeded.
        /// </returns>
        bool
        DisconnectFromPrintQueue(
            String^                    printQueuePath
            );

        /// <summary>
        /// Deletes an existing connection to the print queue identified by printQueue object.
        /// </summary>        
        /// <param name="printQueue">
        /// PrintQueue object that identifies the printer connection.
        /// </param>                
        /// <returns>
        ///  Return true if succeeded.
        /// </returns>
        bool
        DisconnectFromPrintQueue(
            PrintQueue^                printer
            );

        /// <summary>
        /// Commits the properties marked as modified to the Print Spooler service.
        /// </summary>        
        /// <see cref="System::Printing::PrintServer"/>
        virtual void
        Commit(
            void
            ) override;

        /// <summary>
        /// Commits the properties marked as modified to the Print Spooler service.
        /// </summary>        
        /// <see cref="System::Printing::PrintServer"/>
        virtual void
        Refresh(
            void
            ) override;

        internal:

        LocalPrintServer(
            PrintServerType            type
            );

        static
        void
        RegisterAttributesNamesTypes(
            void
            );

        static
        PrintProperty^
        CreateAttributeNoValue(
            String^     attributeName
            );

        static
        PrintProperty^
        CreateAttributeValue(
            String^     attributeName,
            Object^     attributeValue
            );

        static
        PrintProperty^
        CreateAttributeNoValueLinked(
            String^             attributeName,
            MulticastDelegate^  delegate
            );

        static
        PrintProperty^
        CreateAttributeValueLinked(
            String^             attributeName,
            Object^             attributeValue,
            MulticastDelegate^  delegate
            );

        private:

        void
        Initialize(
            void
            );

        static
        array<String^>^
        PrimaryAttributeNames(
            void
            )
        {
            return primaryAttributeNames;
        }

        static
        array<Type^>^
        PrimaryAttributeTypes(
            void
            )
        {
            return primaryAttributeTypes;
        }

        static
        array<String^>^
        GetAllPropertiesFilter(
            void
            );

        static
        array<String^>^
        GetAllPropertiesFilter(
            array<String^>^ propertiesFilter
            );

        array<String^>^
        GetAlteredPropertiesFilter(
            void
            );

        void
        GetDataFromServer(
            String^     property,
            Boolean     forceRefresh   
            );


        void
        GetUnInitializedData(
            array<String^>^ properties
            );

        array<MulticastDelegate^>^
        CreatePropertiesDelegates(
            void
            );

        void
        ComitDirtyData(
            array<String^>^             properties
            );

        array<String^>^
        ConvertPropertyFilterToString(
            array<LocalPrintServerIndexedProperty>^    propertiesFilter
            );


        String^
        GetFullPrintQueueName(
            PrintQueue^ queue
            );

        void
        VerifyAccess(
            void
            );


        static
        LocalPrintServer(
            void
            )
        {
            attributeNameTypes = gcnew Hashtable();
        }

        static Hashtable^   attributeNameTypes;

        static array<String^>^ primaryAttributeNames =
        {
            "DefaultPrintQueue"
        };

        static array<Type^>^ primaryAttributeTypes = 
        {
            PrintQueue::typeid
        };

        PrintQueue^             defaultPrintQueue;
        array<String^>^         refreshPropertiesFilter;
        PrintSystemDispatcherObject^    accessVerifier;
    };
}
}

#endif
