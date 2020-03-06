// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSERVER_HPP__
#define __PRINTSERVER_HPP__

namespace System
{
namespace Printing
{
    /// <summary>
    /// Enumeration of properties of the <c>PrintServer</c> object.
    /// <list type="table">
    /// <item>
    /// <term>DefaultSpoolDirectory</term>
    /// <description>Default spool directory property.</description>
    /// </item>
    /// <item>
    /// <term>PortThreadPriority</term>
    /// <description>Port thread priority.</description>
    /// </item>
    /// <item>
    /// <term>SchedulerPriority</term>
    /// <description>Job scheduler thread priority.</description>
    /// </item>
    /// <item>
    /// <term>DefaultSchedulerPriority</term>
    /// <description>Default job scheduler thread priority.</description>
    /// </item>
    /// <item>
    /// <term>BeepEnabled</term>
    /// <description>Beep on errors on remote documents property.</description>
    /// </item>
    /// <item>
    /// <term>NetPopup</term>
    /// <description>Net popup job notifications property.</description>
    /// </item>
    /// <item>
    /// <term>EventLog</term>
    /// <description>Print server event logging configuration property.</description>
    /// </item>
    /// <item>
    /// <term>MajorVersion</term>
    /// <description>Print server OS major version property.</description>
    /// </item>
    /// <item>
    /// <term>MinorVersion</term>
    /// <description>Print server OS minor version property.</description>
    /// </item>    
    /// <item>
    /// <term>RestartJobOnPoolTimeout</term>
    /// <description>Timeout for restarting jobs in print pool property.</description>
    /// </item>
    /// <item>
    /// <term>RestartJobOnPoolEnabled</term>
    /// <description>Enables restarting the jobs in print pool on timeout.</description>
    /// </item>
    /// </list> 
    /// </summary>
    /// <ExternalAPI/>
    public enum class PrintServerIndexedProperty
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
        RestartJobOnPoolEnabled     
    };

    private enum class PrintServerType
    {
        Browsable
    };

    /// <summary>
    /// This class abstracts the functionality of a print server.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintServer :
    public PrintSystemObject
    {
        public:

        /// <summary>
        /// Creates a new instance of the PrintServer class. 
        /// The object is bound to the print server hosted by the current machine.
        /// </summary>
        /// <remarks>
        /// Same as <c>PrintServer(NULL)</c>. Desired access defaults to <c>PrintSystemDesiredAccess::EnumerateServer</c>.
        /// </remarks>      
        /// <exception cref="System::Printing::PrintServerException">Thrown on failure.</exception>
        PrintServer(
            void
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. The object is bound to the print server host 
        /// specified in the path parameter.
        /// </summary>
        /// <remarks>Desired access defaults to <c>PrintSystemDesiredAccess::EnumerateServer</c>.</remarks>
        /// <exception cref="System::Printing::PrintServerException">Thrown on failure.</exception>
        /// <param name="path">
        /// Path identifier of the Print server.
        /// </param>
        PrintServer(
            String^                     path
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. Properties referenced 
        /// in the <c>propertiesFilter</c> are initialized. The object is bound to the print server host 
        /// specified in the path parameter.
        /// </summary>
        /// <param name="path">
        /// Path identifier of the Print server.
        /// </param>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintServer(
            String^                                path,
            array<PrintServerIndexedProperty>^     propertiesFilter
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. Properties referenced in the <c>propertiesFilter</c>
        /// are initialized. The object is bound to the print server host specified in the path parameter.
        /// </summary>
        /// <param name="path">
        /// Path identifier of the Print server.
        /// </param>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in <c>propertiesFilter</c>
        /// array will be initialized on first use.
        /// </param>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintServer(
            String^                     path,
            array<String^>^             propertiesFilter
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. The object is bound to the local print server
        /// and asks for permissions to be granted as specified in the <c>desiredAccess</c> parameter.
        /// </summary>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>        
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintServer(
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. The object is bound to the print server 
        /// host specified in the <c>path</c> parameter and ask for permissions to be granted as specified 
        /// in the <c>desiredAccess</c> parameter.
        /// </summary>
        /// <param name="path">
        /// Path identifier of the Print server.
        /// </param>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>        
        PrintServer(
            String^                     path,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class. 
        /// Only the properties referenced in the <c>propertiesFilter</c> are initialized. 
        /// The object is bound to the print server specified in the <c>path</c> parameter and 
        /// ask for permissions to be granted as specified in the <c>desiredAccess</c> parameter.
        /// </summary>
        /// <param name="path">
        /// Path identifier of the Print server.
        /// </param>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in <c>propertiesFilter</c>
        ///  array will be initialized on first use.
        /// </param>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>        
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintServer(
            String^                             path,
            array<PrintServerIndexedProperty>^  propertiesFilter,
            PrintSystemDesiredAccess            desiredAccess
            );

        /// <summary>
        /// Creates a new instance of the PrintServer class.  
        /// Only the properties referenced in the <c>propertiesFilter</c> are initialized. 
        /// The object is bound to the print server specified in the <c>path</c> parameter and 
        /// ask for permissions to be granted as specified in the <c>desiredAccess</c> parameter.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is created.
        /// The rest of the properties that are not in propertiesFilter 
        ///  array will be initialized on first use.
        /// </param>
        /// <param name="desiredAccess">
        /// Desired access. <see cref="PrintSystemDesiredAccess"/>
        /// </param>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintServer(
            String^                     path,
            array<String^>^             propertiesFilter,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Installs a print queue on the print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be installed.</param>
        /// <param name="driverName">Name of the print driver associated with the printer to be installed.</param>
        /// <param name="portNames">Array of port names associated with the printer to be installed.</param>
        /// <param name="printProcessorName">Name of the print processor associated with the printer to be installed.</param>
        /// <param name="printQueueAttributes">Attributes to be set on the printer.</param>
        /// <returns>Returns a <c>PrintQueue</c> object.</returns>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintQueue^
        InstallPrintQueue(
            String^                     printQueueName,
            String^                     driverName,
            array<String^>^             portNames,
            String^                     printProcessorName,
            PrintQueueAttributes        printQueueAttributes
            );

        /// <summary>
        /// Installs a print queue on the print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be installed.</param>
        /// <param name="driverName">Name of the print driver associated with the printer to be installed.</param>
        /// <param name="portNames">Array of port names associated with the printer to be installed.</param>
        /// <param name="printProcessorName">Name of the print processor associated with the printer to be installed.</param>
        /// <param name="printQueueAttributes">Attributes to be set on the printer.</param>
        /// <param name="printQueueProperty">Property to be set on the printer.</param>
        /// <param name="printQueuePriority">Priority to be set on the printer.</param>
        /// <param name="printQueueDefaultPriority">Default priority to be set on the printer.</param>        
        /// <returns>Returns a PrintQueue object.</returns>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintQueue^
        InstallPrintQueue(
            String^                      printQueueName,
            String^                      driverName,
            array<String^>^              portNames,
            String^                      printProcessorName,
            PrintQueueAttributes         printQueueAttributes,
            PrintQueueStringProperty^    printQueueProperty,
            Int32                        printQueuePriority,
            Int32                        printQueueDefaultPriority
            );

        /// <summary>
        /// Installs a print queue on the print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be installed.</param>
        /// <param name="driverName">Name of the print driver associated with the printer to be installed.</param>
        /// <param name="portNames">Array of port names associated with the printer to be installed.</param>
        /// <param name="printProcessorName">
        ///  Name of the print processor associated with the printer to be installed.
        ///  </param>
        /// <param name="printQueueAttributes">Attributes to be set on the printer.</param>
        /// <param name="printQueueShareName">Share name to be set on the printer.</param>
        /// <param name="printQueueComment">Comment to be set on the printer.</param>
        /// <param name="printQueueLocation">Location to be set on the printer.</param>
        /// <param name="printQueueSeparatorFile">Separator file to be set on the printer.</param>
        /// <param name="printQueuePriority">Priority to be set on the printer.</param>
        /// <param name="printQueueDefaultPriority">Default priority to be set on the printer.</param>        
        /// <returns>Returns a <c>PrintQueue</c> object. </returns>        
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintQueue^
        InstallPrintQueue(
            String^                      printQueueName,
            String^                      driverName,
            array<String^>^              portNames,
            String^                      printProcessorName,
            PrintQueueAttributes         printQueueAttributes,
            String^                      printQueueShareName,
            String^                      printQueueComment,
            String^                      printQueueLocation,
            String^                      printQueueSeparatorFile,
            Int32                        printQueuePriority,
            Int32                        printQueueDefaultPriority
            );

        /// <summary>
        /// Installs a print queue on the print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be installed.</param>
        /// <param name="driverName">Name of the print driver associated with the printer to be installed.</param>
        /// <param name="portNames">Array of port names associated with the printer to be installed.</param>
        /// <param name="printProcessorName">
        ///  Name of the print processor associated with the printer to be installed.
        ///  </param>
        /// <param name="initialParameters">Collection of parameters to be set on the printer.</param>
        /// <returns>Returns a <c>PrintQueue</c> object. </returns>                
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        PrintQueue^
        InstallPrintQueue(
            String^                                 printQueueName,
            String^                                 driverName,
            array<String^>^                         portNames,
            String^                                 printProcessorName,
            PrintPropertyDictionary^                initialParameters
            );

        /// <summary>
        /// Deletes a print queue on the print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be deleted.</param>        
        /// <returns>Returns true if operation succeeded.</returns>                
        static
        bool
        DeletePrintQueue(
            String^                      printQueueName
            );

        /// <summary>
        /// Deletes a print queue on the print server.
        /// </summary>
        /// <param name="printQueue"><c>PrintQueue</c> object to be deleted.</param>        
        /// <returns>Returns true if operation succeeded.</returns>                        
        static
        bool
        DeletePrintQueue(
            PrintQueue^                  printQueue
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue on this print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be instantiated.</param>                
        /// <returns>Returns a PrintQueue object. </returns>                
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue^
        GetPrintQueue(
            String^                      printQueueName
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue on this print server.
        /// </summary>
        /// <param name="printQueueName">Name of the printer to be instantiated.</param>
        /// <param name="propertiesFilter">Array of properties to be initialized when the object is constructed.</param>
        /// <remarks>
        /// Initializing properties requires trips to the Spooler service. 
        /// This method provides a way for the caller to improve the performance by initializing 
        /// only the properties that will be accessed.
        /// The rest of the properties that are not in propertiesFilter array will be initialized on first use.
        /// </remarks>
        /// <returns>Returns a <c>PrintQueue</c> object. </returns>                        
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue^
        GetPrintQueue(
            String^                      printQueueName,
            array<String^>^              propertiesFilter
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <returns>Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing the print queues installed on this print server.</returns>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            void
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <param name="propertiesFilter">Array of properties to be initialized when the object is constructed.</param>
        /// <returns>Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing the print queues installed on this print server.</returns>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            array<PrintQueueIndexedProperty>^           propertiesFilter
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <param name="propertiesFilter">Array of properties to be initialized when the object is constructed.</param>
        /// <returns>
        /// Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing the print 
        /// queues installed on this print server.
        /// </returns>        
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            array<String^>^                      propertiesFilter
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <param name="enumerationFlag">Array of flags specifying the type of the <c>PrintQueue</c> objects to be enumerated.</param>
        /// <returns>Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing the print queues installed on this print server.</returns>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            array<EnumeratedPrintQueueTypes>^     enumerationFlag
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is constructed.
        /// </param>        
        /// <param name="enumerationFlag">
        /// Array of flags specifying the type of the <c>PrintQueue</c> objects to be enumerated. 
        /// </param>
        /// <returns>
        /// Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing 
        /// the print queues installed on this print server. 
        /// </returns>   
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            array<PrintQueueIndexedProperty>^           propertiesFilter,
            array<EnumeratedPrintQueueTypes>^    enumerationFlag
            );

        /// <summary>
        /// Enumerates the <c>PrintQueue</c> objects representing the print queues installed on this print server.
        /// </summary>
        /// <param name="propertiesFilter">
        /// Array of properties to be initialized when the object is constructed.
        /// </param>        
        /// <param name="enumerationFlag">
        /// Array of flags specifying the type of the 
        /// PrintQueue objects to be enumerated. 
        /// </param>
        /// <returns>Returns a <c>PrintQueueCollection</c> of <c>PrintQueue</c> objects representing 
        /// the print queues installed on this print server. <see cref="PrintQueueCollection"/>
        /// </returns>   
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueueCollection^
        GetPrintQueues(
            array<String^>^                      propertiesFilter,
            array<EnumeratedPrintQueueTypes>^    enumerationFlag
            );

        /// <summary>
        /// Commits the properties marked as modified to the Print Spooler service.
        /// </summary>
        /// <remarks>
        /// Inherited from <c>PrintSystemObject</c>.
        /// </remarks>
        /// <exception cref="PrintCommitAttributesException">Thrown on failure or partial success.</exception>
        /// <example> This sample shows how to set properties on the <c>PrintServer</c> object.
        /// <code>
        /// {
        ///    PrintServer* server = new PrintServer();
        ///    server->BeepEnabled = true;
        ///    server->RestartJobOnPoolEnabled = false;
        ///    server->Commit();
        /// }
        /// </code>
        /// </example>
        virtual void
        Commit(
            void
            ) override;

        /// <summary>
        /// Synchronizes the data in the properties with the live data from the Print Spooler service.
        /// </summary>        
        /// <remarks>
        /// When calling Refresh, data in uncommitted properties is lost.
        /// Inherited from <c>PrintSystemObject</c>.
        /// </remarks>
        /// <exception cref="PrintServerException">Thrown on failure.</exception>
        virtual void
        Refresh(
            void
            ) override;

        /// <value>
        /// Default spool directory property.
        /// </value>
        property
        String^
        DefaultSpoolDirectory
        {
            String^ get();
            void set(String^     value);
        }

        /// <value>
        /// Port thread priority.
        /// </value>                
        property
        System::Threading::ThreadPriority
        PortThreadPriority
        {
            System::Threading::ThreadPriority get();
            void set(System::Threading::ThreadPriority     value);
        }

        /// <value>
        /// Default port thread priority.
        /// </value>
        property
        System::Threading::ThreadPriority
        DefaultPortThreadPriority
        {
            System::Threading::ThreadPriority get();
            internal:
            void set(System::Threading::ThreadPriority       value);
        }

        /// <value>
        /// Job scheduler thread priority.
        /// </value>
        property
        System::Threading::ThreadPriority
        SchedulerPriority
        {
            System::Threading::ThreadPriority get();
            void set(System::Threading::ThreadPriority       value);
        }

        /// <value>
        /// Default job scheduler thread priority.
        /// </value>
        property
        System::Threading::ThreadPriority
        DefaultSchedulerPriority
        {
            System::Threading::ThreadPriority get();
            internal:
            void set(System::Threading::ThreadPriority       value);
        }

        /// <value>
        /// Beep on errors on remote documents property.
        /// </value>
        property
        Boolean
        BeepEnabled
        {
            Boolean get();
            void set(Boolean       value);
        }

        /// <value>
        /// Net popup property. Set to true if job notifications should be sent to the client computer, 
        /// and false if job notifications are to be sent to the user.
        /// </value>        
        property
        Boolean
        NetPopup
        {
            Boolean get();
            void set(Boolean       value);
        }

        /// <value>
        /// Print server event logging configuration property.<see cref="PrintServerEventLoggingTypes"/>
        /// </value>                
        property
        PrintServerEventLoggingTypes
        EventLog
        {
            PrintServerEventLoggingTypes get();
            void set(PrintServerEventLoggingTypes       value);
        }

        /// <value>
        /// Print server OS major version property.
        /// </value> 
        property
        Int32
        MajorVersion
        {
            Int32 get();
            internal:
            void set(Int32       value);
        }

        /// <value>
        /// Print server OS minor version property.
        /// </value>
        property
        Int32
        MinorVersion
        {
            Int32 get();
            internal:
            void set(Int32       value);
        }

        /// <value>
        /// Timeout for restarting jobs in print pool property.
        /// </value>
        property
        Int32 
        RestartJobOnPoolTimeout
        {
            Int32 get();
            void set(Int32       value);
        }

        /// <value>
        /// This property enables restarting the jobs in print pool on timeout.
        /// </value>        
        property
        Boolean
        RestartJobOnPoolEnabled
        {
            Boolean get();
            void set(Boolean       value);
        }

        /// <value>
        /// Print server OS version property.
        /// </value>                
        property
        Byte
        SubSystemVersion
        {
            Byte get();
        }
       
        /// <value>
        /// Name identifier of this object.
        /// </value>    
        /// <remarks>
        /// Inherited from PrintSystemObject.
        /// </remarks>
        property
        String^
        Name
        {
            public:
                virtual String^ get() sealed override;
            internal:
                virtual void set(String^ objName) sealed override;
        }

        internal:

        PrintServer(
            String^             path,
            PrintServerType     type
            );
        
        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )
        property
        bool
        IsInternallyInitialized
        {
            bool get();
            void set(bool);
        }
        #pragma warning ( default:4376 )

        virtual PrintPropertyDictionary^
        get_InternalPropertiesCollection(
            String^     attributeName
            ) override;

        static
        void
        RegisterAttributesNamesTypes(
            void
            );

        static
        void
        RegisterAttributesNamesTypes(
            Hashtable^ childAttributeNameTypes
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

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override sealed;


        internal:
        Exception^
        CreatePrintServerException(
            int hresult,
            String^ messageId
            );


        Exception^
        CreatePrintServerException(
            int hresult,
            String^ messageId,
            Exception^ innerException
            );

        private:

        Exception^
        CreatePrintCommitAttributesException(
            int hresult,
            String^ messageId,
            System::Collections::ObjectModel::Collection<String^>^ commitedAttributes,
            System::Collections::ObjectModel::Collection<String^>^ failedAttributes
            );

        static 
        PrintServer(
            void
            )
        {
            attributeNameTypes = gcnew Hashtable();
            internalAttributeNameMapping = gcnew Hashtable();            
            
            for(Int32 numOfMappings = 0;
                numOfMappings < BaseAttributeNames()->Length;
                numOfMappings++)
            {
                internalAttributeNameMapping->Add(BaseAttributeNames()[numOfMappings],
                                                  BaseAttributeNames()[numOfMappings]);
            }

            for(Int32 numOfMappings = 0;
                numOfMappings < primaryAttributeNames->Length;
                numOfMappings++)
            {
                internalAttributeNameMapping->Add(primaryAttributeNames[numOfMappings],
                                                  internalAttributeNames[numOfMappings]);
            }

            BuildInteropAttributesMap();
        }

        delegate
        Object^
        ThunkGetPrinterData(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        delegate
        bool
        ThunkSetPrinterData(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        void
        BuildInteropAttributesMap(
            void
            );

        array<MulticastDelegate^>^
        CreatePropertiesDelegates(
            void
            );

        void
        InitializeInternalCollections(
            void
            );
        
        void
        Initialize(
            String^                     path,
            array<String^>^             propertiesFilter,
            PrinterDefaults^            printerDefaults
            );

        array<String^>^
        ConvertPropertyFilterToString(
            array<PrintServerIndexedProperty>^     propertiesFilter
            );

        array<String^>^
        GetAlteredPropertiesFilter(
            void
            );

        array<String^>^
        GetAllPropertiesFilter(
            array<String^>^         propertiesFilter
            );

        array<String^>^
        GetAllPropertiesFilter(
            void
            );

        void
        GetDataFromServer(
            String^                 property,
            Boolean                 forceRefresh
            );

        Boolean
        GetUnInitializedData(
            array<String^>^         properties
            );

        void
        ComitDirtyData(
            array<String^>^         properties
            );

        void
        VerifyAccess(
            void
            );

        static
        bool
        IsHResultWin32Error(
            int hresult,
            int expectedWin32Error
            );

        String^                             defaultSpoolDirectory; 
        System::Threading::ThreadPriority   portThreadPriority;
        System::Threading::ThreadPriority   defaultPortThreadPriority;
        System::Threading::ThreadPriority   schedulerPriority;
        System::Threading::ThreadPriority   defaultSchedulerPriority;
        Boolean                             beepEnabled;
        Boolean                             netPopup;
        PrintServerEventLoggingTypes        eventLog;
        Int32                               majorVersion;
        Int32                               minorVersion;
        Int32                               restartJobOnPoolTimeout;
        Boolean                             restartJobOnPoolEnabled;
        Byte                                subSystemVersion;

        PrinterThunkHandler^                serverThunkHandler; 

        array<String^>^                     refreshPropertiesFilter;

        bool                                isInternallyInitialized;

        Boolean                             isDelayInitialized;

        PrintSystemDispatcherObject^    accessVerifier;


        internal:

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

        protected:

        property
        Boolean
        IsDelayInitialized
        {
            Boolean get();
            void set(Boolean delayInitialized);
        }

        private :
        //
        // The following is the necessary data members to link the 
        // compile time properties with the named properties in the
        // associated collection
        //
        static array<String^>^ primaryAttributeNames =
        {
            "DefaultSpoolDirectory",
            "PortThreadPriority",
            "DefaultPortThreadPriority",
            "SchedulerPriority",
            "DefaultSchedulerPriority",
            "BeepEnabled",
            "NetPopup",
            "EventLog",
            "MajorVersion",
            "MinorVersion",
            "RestartJobOnPoolTimeout",
            "RestartJobOnPoolEnabled",
        };

        static array<Type^>^ primaryAttributeTypes =
        {
            String::typeid,
            System::Threading::ThreadPriority::typeid,
            System::Threading::ThreadPriority::typeid,
            System::Threading::ThreadPriority::typeid,
            System::Threading::ThreadPriority::typeid,
            Boolean::typeid,
            Boolean::typeid,
            PrintServerEventLoggingTypes::typeid,
            Int32::typeid,
            Int32::typeid,
            Int32::typeid,
            Boolean::typeid
        };

        static array<String^>^ internalAttributeNames =
        {
            "DefaultSpoolDirectory",
            "PortThreadPriority",
            "PortThreadPriorityDefault", 
            "SchedulerThreadPriority", 
            "SchedulerThreadPriorityDefault", 
            "BeepEnabled",
            "NetPopup",
            "EventLog",
            "MajorVersion",
            "MinorVersion",
            "RestartJobOnPoolError",
            "RestartJobOnPoolEnabled",
        };

        static array<Type^>^ attributeInteropTypes =
        {
            PrintStringProperty::typeid             ,
            PrintInt32Property::typeid              ,
            PrintBooleanProperty::typeid            ,
            PrintServerLoggingProperty::typeid      ,
            PrintThreadPriorityProperty::typeid

        };

        static
        array<ThunkGetPrinterData^>^ getAttributeInteropDelegates = 
        {
            gcnew PrintServer::ThunkGetPrinterData(&PrinterThunkHandler::ThunkGetPrinterDataString)             ,
            gcnew PrintServer::ThunkGetPrinterData(&PrinterThunkHandler::ThunkGetPrinterDataInt32)              ,
            gcnew PrintServer::ThunkGetPrinterData(&PrinterThunkHandler::ThunkGetPrinterDataBoolean)            ,
            gcnew PrintServer::ThunkGetPrinterData(&PrinterThunkHandler::ThunkGetPrinterDataServerEventLogging) ,
            gcnew PrintServer::ThunkGetPrinterData(&PrinterThunkHandler::ThunkGetPrinterDataThreadPriority)     ,
        };


        static
        array<ThunkSetPrinterData^>^ setAttributeInteropDelegates = 
        {
            gcnew PrintServer::ThunkSetPrinterData(&PrinterThunkHandler::ThunkSetPrinterDataString)             ,
            gcnew PrintServer::ThunkSetPrinterData(&PrinterThunkHandler::ThunkSetPrinterDataInt32)              ,
            gcnew PrintServer::ThunkSetPrinterData(&PrinterThunkHandler::ThunkSetPrinterDataBoolean)            ,
            gcnew PrintServer::ThunkSetPrinterData(&PrinterThunkHandler::ThunkSetPrinterDataServerEventLogging) ,
            gcnew PrintServer::ThunkSetPrinterData(&PrinterThunkHandler::ThunkSetPrinterDataThreadPriority)     ,
        };

        static Hashtable^                            attributeNameTypes;
        static Hashtable^                            internalAttributeNameMapping;
        static Hashtable^                            getAttributeInteropMap;
        static Hashtable^                            setAttributeInteropMap;

        Hashtable^                                   collectionsTable;
    };
}
}

#endif
