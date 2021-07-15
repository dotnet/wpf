// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTQUEUE_HPP__
#define __PRINTQUEUE_HPP__

/*++
    Abstract:
        This file includes the declarations of the PrintQueue
--*/

#include "LegacyDevice.hpp"

using namespace System::Windows::Xps;
using namespace System::Windows::Xps::Packaging;
using namespace System::Windows::Xps::Serialization;
using namespace System::Security;

namespace System
{
namespace Printing
{
    //
    // Location, Comment and Share Name are mutualally
    // exclusive properties. So by introducing both
    // PrintStringPropertyType & PrinterStringProperty
    // I am allowing users to exclusively set any one
    // of the three.
    //
    public enum class PrintQueueStringPropertyType
    {
        Location    = 0x00000000,
        Comment     = 0x00000001,
        ShareName   = 0x00000002
    };

    public ref struct PrintQueueStringProperty
    {
        public:

        property PrintQueueStringPropertyType   Type;
        property String^                        Name;
    };

    /// <summary>
    /// Enumeration of properties of the PrintQueue object.
    /// <list type="table">
    /// <item>
    /// <term>Name</term>
    /// <description>Printer name.</description>
    /// </item>
    /// <item>
    /// <term>ShareName</term>
    /// <description>Printer share name.</description>
    /// </item>
    /// <item>
    /// <term>Comment</term>
    /// <description>Brief description of the printer.</description>
    /// </item>
    /// <item>
    /// <term>Location</term>
    /// <description>Physical location of the printer.</description>
    /// </item>
    /// <item>
    /// <term>Description</term>
    /// <description>Windows 95/98/Me: Pointer to a null-terminated string that describes the printer.
    ///  The string contains the printer name, driver name, and comment concatenated and separated by commas.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Priority</term>
    /// <description>Priority value that the Print Spooler uses to route print jobs.</description>
    /// </item>
    /// <item>
    /// <term>DefaultPriority</term>
    /// <description>Default priority value assigned to each print job.</description>
    /// </item>
    /// <item>
    /// <term>StartTimeOfDay</term>
    /// <description>Earliest time at which the printer will print a job.</description>
    /// </item>
    /// <item>
    /// <term>UntilTimeOfDay</term>
    /// <description>Latest time at which the printer will print a job.</description>
    /// </item>
    /// <item>
    /// <term>AveragePagesPerMinute</term>
    /// <description>Average number of pages per minute that have been printed on the printer.</description>
    /// </item>
    /// <item>
    /// <term>NumberOfJobs</term>
    /// <description>Number of print jobs that have been queued for the printer.</description>
    /// </item>
    /// <item>
    /// <term>QueueAttributes</term>
    /// <description>Printer attributes of type <c>PrintQueueAttributes</c>.</description>
    /// </item>
    /// <item>
    /// <term>QueueDriver</term>
    /// <description>Printer driver used by the printer.</description>
    /// </item>
    /// <item>
    /// <term>QueuePort</term>
    /// <description>Port(s) used to transmit data to the printer.</description>
    /// </item>
    /// <item>
    /// <term>QueuePrintProcessor</term>
    /// <description>Print Processor used by the printer..</description>
    /// </item>
    /// <item>
    /// <term>HostingPrintServer</term>
    /// <description>Print server name.</description>
    /// </item>
    /// <item>
    /// <term>QueueStatus</term>
    /// <description>Printer status of type <c>PrintQueueStatus</c>.</description>
    /// </item>
    /// <item>
    /// <term>SeparatorFile</term>
    /// <description>Name of the file used to create the separator page.</description>
    /// </item>
    /// <item>
    /// <term>UserPrintTicket</term>
    /// <description>Per user PrintTicket.</description>
    /// </item>
    /// <item>
    /// <term>DefaultPrintTicket</term>
    /// <description>Printer default PrintTicket.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <ExternalAPI/>
    public enum class PrintQueueIndexedProperty
    {
        Name                        ,
        ShareName                   ,
        Comment                     ,
        Location                    ,
        Description                 ,
        Priority                    ,
        DefaultPriority             ,
        StartTimeOfDay              ,
        UntilTimeOfDay              ,
        AveragePagesPerMinute       ,
        NumberOfJobs                ,
        QueueAttributes             ,
        QueueDriver                 ,
        QueuePort                   ,
        QueuePrintProcessor         ,
        HostingPrintServer          ,
        QueueStatus                 ,
        SeparatorFile               ,
        UserPrintTicket             ,
        DefaultPrintTicket
    };

    /// <summary>
    /// This class abstracts the functionality of a print queue.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintQueue :
    public PrintSystemObject
    {
        public:

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <remarks>
        /// Desired access defaults to to <c>PrintSystemDesiredAccess::UsePrinter</c>.
        /// </remarks>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="printSchemaVersion">JT schema version.</param>
        /// <remarks>
        /// Desired access defaults to <c>PrintSystemDesiredAccess::UsePrinter</c>.
        /// </remarks>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName,
            Int32                       printSchemaVersion
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// Only the properties referenced in the <c>propertyFilter</c> are initialized.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="propertyFilter">Array of properties to be initialized.</param>
        /// <remarks>
        /// Desired access defaults to <c>PrintSystemDesiredAccess::UsePrinter</c>.
        /// </remarks>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName,
            array<String^>^             propertyFilter
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// Only the properties referenced in the <c>propertyFilter</c> are initialized.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="propertyFilter">Array of properties to be initialized.</param>
        /// <remarks>
        /// Desired access defaults to to <c>PrintSystemDesiredAccess::UsePrinter</c>.
        /// </remarks>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                       printServer,
            String^                            printQueueName,
            array<PrintQueueIndexedProperty>^  propertyFilter
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="desiredAccess">Desired access <see cref="PrintSystemDesiredAccess"/></param>
        /// <exception cref="PrintQueueException">Thrown on failure</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="printSchemaVersion">JT schema version.</param>
        /// <param name="desiredAccess">Desired access <see cref="PrintSystemDesiredAccess"/></param>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName,
            Int32                       printSchemaVersion,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="propertyFilter">Array of properties to be initialized.</param>
        /// <param name="desiredAccess">Desired access <see cref="PrintSystemDesiredAccess"/></param>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                printServer,
            String^                     printQueueName,
            array<String^>^             propertyFilter,
            PrintSystemDesiredAccess    desiredAccess
            );

        /// <summary>
        /// Instantiates a <c>PrintQueue</c> object representing a preinstalled print queue
        /// on the print server identified by the <c>printServer</c> parameter.
        /// </summary>
        /// <param name="printServer"><c>PrintServer</c> object that hosts the print queue.</param>
        /// <param name="printQueueName">Name of the print queue.</param>
        /// <param name="propertyFilter">Array of properties to be initialized.</param>
        /// <param name="desiredAccess">Desired access <see cref="PrintSystemDesiredAccess"/></param>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        PrintQueue(
            PrintServer^                        printServer,
            String^                             printQueueName,
            array<PrintQueueIndexedProperty>^   propertyFilter,
            PrintSystemDesiredAccess            desiredAccess
            );

        internal:
        //
        // This class constructor is intended for
        // browsable Print Objects. To give an example,
        // this is instantiated when enumerating
        // PrintQueues on a PrintServer
        //
        PrintQueue(
            array<String^>^             propertyFilter
            );

        PrintQueue(
            PrintServer^                printServer,
            array<String^>^             propertyFilter
            );

        internal:

        PrinterThunkHandlerBase^
        CreatePrintThunkHandler(
            void
            );

        Boolean
        IsXpsDeviceSimulationSupported(
            void
            );

        Boolean
            IsXpsOMPrintingDisabled(
            void
            );

        Boolean
            IsXpsOMPrintingSupported(
            void
            );

        static
        PrintQueue^
        Install(
            PrintServer^                            printServer,
            String^                                 printQueueName,
            String^                                 driverName,
            array<String^>^                         portNames,
            String^                                 printProcessorName,
            PrintQueueAttributes                    printQueueAttributes
            );


        static
        PrintQueue^
        Install(
            PrintServer^                            printServer,
            String^                                 printQueueName,
            String^                                 driverName,
            array<String^>^                         portNames,
            String^                                 printProcessorName,
            PrintQueueAttributes                    printQueueAttributes,
            PrintQueueStringProperty^               requiredPrintQueueProperty,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority
            );

        static
        PrintQueue^
        Install(
            PrintServer^                            printServer,
            String^                                 printQueueName,
            String^                                 driverName,
            array<String^>^                         portNames,
            String^                                 printProcessorName,
            PrintQueueAttributes                    printQueueAttributes,
            String^                                 requiredShareName,
            String^                                 requiredComment,
            String^                                 requiredLocation,
            String^                                 requiredSeparatorFile,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority
            );

        static
        PrintQueue^
        Install(
            PrintServer^                            printServer,
            String^                                 printQueueName,
            String^                                 driverName,
            array<String^>^                         portNames,
            String^                                 printProcessorName,
            PrintPropertyDictionary^                initializationParams
            );

        static
        bool
        Delete(
            String^                                 printQueueName
            );

        public:

        /// <summary>
        /// Acquire device capabilities.
        /// </summary>
        /// <param name="printTicket">
        /// PrintTicket.
        /// </param>
        PrintCapabilities^
        GetPrintCapabilities(
            PrintTicket^                 printTicket
            );

        /// <summary>
        /// Acquire device capabilities.
        /// </summary>
        PrintCapabilities^
        GetPrintCapabilities(
            void
            );

        /// <summary>
        /// Acquire device capabilities as an XML stream.
        /// </summary>
        /// <param name="printTicket">
        /// PrintTicket.
        /// </param>
        MemoryStream^
        GetPrintCapabilitiesAsXml(
            PrintTicket^                 printTicket
            );

        /// <summary>
        /// Acquire device capabilities as an XML stream.
        /// </summary>
        MemoryStream^
        GetPrintCapabilitiesAsXml(
            void
            );

        /// <summary>
        /// Merge and validate PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">
        /// Base PrintTicket.
        /// </param>
        /// <param name="deltaPrintTicket">
        /// Delta PrintTicket.
        /// </param>
        /// <returns>Returns a ValidationResult.</returns>
        ValidationResult
        MergeAndValidatePrintTicket(
            PrintTicket^            basePrintTicket,
            PrintTicket^            deltaPrintTicket
            );

        /// <summary>
        /// Merge and validate PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">
        /// Base PrintTicket.
        /// </param>
        /// <param name="deltaPrintTicket">
        /// Delta PrintTicket.
        /// </param>
        /// <param name="scope">
        /// Scope that delta PrintTicket and result PrintTicket will be limited to.
        /// </param>
        /// <returns>Returns a ValidationResult.</returns>
        ValidationResult
        MergeAndValidatePrintTicket(
            PrintTicket^            basePrintTicket,
            PrintTicket^            deltaPrintTicket,
            PrintTicketScope        scope
            );

        /// <summary>
        /// Pauses printing the print queue.
        /// </summary>
        virtual
        void
        Pause(
            void
            );

        /// <summary>
        /// Resumes printing on the the print queue.
        /// </summary>
        virtual
        void
        Resume(
            void
            );

        PrintSystemJobInfo^
        AddJob(
            void
            );

        PrintSystemJobInfo^
        AddJob(
            String^ jobName
            );

        /// <summary>
        ///     Adds a new print job, specifying the name and the initial print ticket to use.
        /// </summary>
        PrintSystemJobInfo^
        AddJob(
            String^         jobName,
            PrintTicket^    printTicket
            );

        PrintSystemJobInfo^
        AddJob(
            String^        jobName,
            String^        documentPath,
            Boolean        fastCopy
            );

        PrintSystemJobInfo^
        AddJob(
            String^        jobName,
            String^        documentPath,
            Boolean        fastCopy,
            PrintTicket^   printTicket
            );

        property
        Boolean
        PrintingIsCancelled
        {
            void set(Boolean isCancelled);
            Boolean get();
        }

        property
        PrintJobSettings^
        CurrentJobSettings
        {
            PrintJobSettings^ get();
        }


        PrintSystemJobInfo^
        GetJob(
            Int32   jobId
            );

        PrintJobInfoCollection^
        GetPrintJobInfoCollection(
            void
            );

        /// <summary>
        /// Purges the jobs on the the print queue.
        /// </summary>
        virtual
        void
        Purge(
            void
            );

        /// <value>
        /// Priority property the Print Spooler uses to route print jobs.
        /// </value>
        property
        virtual
        Int32
        Priority
        {
            Int32 get();
            void set(Int32 inPriority);
        }

        /// <value>
        /// Name identifier of this object.
        /// </value>
        /// <remarks>
        /// Inherited from PrintSystemObject.
        /// </remarks>
        property
        virtual
        String^
        Name
        {
            String^ get() sealed override;
            void set(String^ objName) sealed override;
        }

        /// <value>
        /// Default priority property the Print Spooler uses to route print jobs.
        /// </value>
        property
        virtual
        Int32
        DefaultPriority
        {
            Int32 get();
            void set(Int32 inDefaultPriority);
        }

        /// <value>
        /// Specifies the earliest time at which the printer will print a job.
        /// </value>
        property
        virtual
        Int32
        StartTimeOfDay
        {
            Int32 get();
            void set(Int32 inStartTime);
        }

        /// <value>
        /// Specifies the latest time at which the printer will print a job.
        /// </value>
        property
        virtual
        Int32
        UntilTimeOfDay
        {
            Int32 get();
            void set(Int32 inUntilTime);
        }

        /// <value>
        /// Specifies the average number of pages per minute that have been printed on the printer.
        /// </value>
        property
        virtual
        Int32
        AveragePagesPerMinute
        {
            Int32 get();
        }

        /// <value>
        /// Specifies the number of print jobs that have been queued for the printer.
        /// This property cannot be set.
        /// </value>
        property
        virtual
        Int32
        NumberOfJobs
        {
            public:
                Int32 get();
            internal:
                void set(Int32 numOfJobs);
        }

        /// <value>
        /// Share name property. NULL if the printer isn't shared.
        /// </value>
        property
        virtual
        String^
        ShareName
        {
            String^ get();
            void set(String^ inShareName);
        }

        /// <value>
        /// Comment property.
        /// </value>
        property
        virtual
        String^
        Comment
        {
            String^ get();
            void set(String^ inComment);
        }

        /// <value>
        /// Physical location property.
        /// </value>
        property
        virtual
        String^
        Location
        {
            String^ get();
            void set(String^ inLocation);
        }

        /// <value>
        /// Description property.
        /// </value>
        property
        virtual
        String^
        Description
        {
            String^ get();
            internal:
            void set(String^ inDescription);
        }

        /// <value>
        /// Specifies the name of the file used to create the separator page.
        /// This page is used to separate print jobs sent to the printer.
        /// </value>
        property
        virtual
        String^
        SeparatorFile
        {
            String^ get();
            void set(String^ inSeparatorFile);
        }

        /// <value>
        /// Per user PrintTicket property.
        /// </value>
        property
        virtual
        PrintTicket^
        UserPrintTicket
        {
            PrintTicket^ get();

            void set(PrintTicket^ newUserPrintTicket);
        }

        /// <value>
        /// Default PrintTicket property.
        /// </value>
        property
        virtual
        PrintTicket^
        DefaultPrintTicket
        {
            PrintTicket^ get();

            void set(PrintTicket^ newDefaultPrintTicket);
        }

        /// <value>
        /// Print queue driver property.<see cref="PrintDriver"/>
        /// </value>
        property
        virtual
        PrintDriver^
        QueueDriver
        {
            PrintDriver^ get();
            void set(PrintDriver^ driver);
        }

        /// <value>
        /// Print queue port property.<see cref="PrintPort"/>
        /// </value>
        property
        virtual
        PrintPort^
        QueuePort
        {
            PrintPort^ get();
            void set(PrintPort^ port);
        }

        /// <value>
        /// Print queue print processor property.<see cref="PrintProcessor"/>
        /// </value>
        property
        virtual
        PrintProcessor^
        QueuePrintProcessor
        {
            PrintProcessor^ get();
            void set(PrintProcessor^ printProcessor);
        }

        /// <value>
        /// Hosting print server property.<see cref="PrintServer"/>
        /// </value>
        property
        virtual
        PrintServer^
        HostingPrintServer
        {
            public:
                PrintServer^ get();
            protected:
                void set(PrintServer^ printServer);
        }

        /// <value>
        /// Printer UNC name property.
        /// </value>
        property
        String^
        FullName
        {
            String^ get();
        }

        /// <value>
        /// Print Queue status property. <see cref="PrintQueueStatus"/>
        /// </value>
        property
        PrintQueueStatus
        QueueStatus
        {
            PrintQueueStatus get();
        }

        /// <value>
        /// Print Queue attributes property. <see cref="PrintQueueAttributes"/>
        /// </value>
        property
        PrintQueueAttributes
        QueueAttributes
        {
            PrintQueueAttributes get();
        }

        /// <value>
        /// Printer is paused property.
        /// </value>
        property
        Boolean
        IsPaused
        {
            Boolean get();
        }

        /// <value>
        /// Printer is in error state property.
        /// </value>
        property
        Boolean
        IsInError
        {
            Boolean get();
        }

        /// <value>
        /// Printer is in pending deletion property.
        /// </value>
        property
        Boolean
        IsPendingDeletion
        {
            Boolean get();
        }

        /// <value>
        /// Printer is jammed property.
        /// </value>
        property
        Boolean
        IsPaperJammed
        {
            Boolean get();
        }

        /// <value>
        /// Printer is out of paper property.
        /// </value>
        property
        Boolean
        IsOutOfPaper
        {
            Boolean get();
        }

        /// <value>
        /// Printer needs manual feed property.
        /// </value>
        property
        Boolean
        IsManualFeedRequired
        {
            Boolean get();
        }

        /// <value>
        /// The printer has paper problem property.
        /// </value>
        property
        Boolean
        HasPaperProblem
        {
            Boolean get();
        }

        /// <value>
        /// Printer is offline property.
        /// </value>
        property
        Boolean
        IsOffline
        {
            Boolean get();
        }

        /// <value>
        /// Printer is IO active property.
        /// </value>
        property
        Boolean
        IsIOActive
        {
            Boolean get();
        }

        /// <value>
        /// Printer is busy property.
        /// </value>
        property
        Boolean
        IsBusy
        {
            Boolean get();
        }

        /// <value>
        /// Printer is printing property.
        /// </value>
        property
        Boolean
        IsPrinting
        {
            Boolean get();
        }

        /// <value>
        /// Print output bin is full property.
        /// </value>
        property
        Boolean
        IsOutputBinFull
        {
            Boolean get();
        }

        /// <value>
        /// Printer is not available property.
        /// </value>
        property
        Boolean
        IsNotAvailable
        {
            Boolean get();
        }

        /// <value>
        /// Printer is waiting for data property.
        /// </value>
        property
        Boolean
        IsWaiting
        {
            Boolean get();
        }

        /// <value>
        /// Printer is processing data property.
        /// </value>
        property
        Boolean
        IsProcessing
        {
            Boolean get();
        }

        /// <value>
        /// Printer is initializing property.
        /// </value>
        property
        Boolean
        IsInitializing
        {
            Boolean get();
        }

        /// <value>
        /// Printer is warming up property.
        /// </value>
        property
        Boolean
        IsWarmingUp
        {
            Boolean get();
        }

        /// <value>
        /// Printer toner is low property.
        /// </value>
        property
        Boolean
        IsTonerLow
        {
            Boolean get();
        }

        /// <value>
        /// Printer has toner property.
        /// </value>
        property
        Boolean
        HasToner
        {
            Boolean get();
        }

        /// <value>
        /// Printer does page punt property.
        /// </value>
        property
        Boolean
        PagePunt
        {
            Boolean get();
        }

        /// <value>
        /// Printer needs user intervention property.
        /// </value>
        property
        Boolean
        NeedUserIntervention
        {
            Boolean get();
        }

        /// <value>
        /// Printer is out of memory property.
        /// </value>
        property
        Boolean
        IsOutOfMemory
        {
            Boolean get();
        }

        /// <value>
        /// Printer has door opened property.
        /// </value>
        property
        Boolean
        IsDoorOpened
        {
            Boolean get();
        }

        /// <value>
        /// Server unknown error state property.
        /// </value>
        property
        Boolean
        IsServerUnknown
        {
            Boolean get();
        }

        /// <value>
        /// Printer power save is on property.
        /// </value>
        property
        Boolean
        IsPowerSaveOn
        {
            Boolean get();
        }

        /// <value>
        /// Printer is queued property.
        /// </value>
        property
        Boolean
        IsQueued
        {
            Boolean get();
        }

        /// <value>
        /// Printer supports direct printing property.
        /// </value>
        property
        Boolean
        IsDirect
        {
            Boolean get();
        }

        /// <value>
        /// Printer is shared property.
        /// </value>
        property
        Boolean
        IsShared
        {
            Boolean get();
        }

        /// <value>
        /// Printer is hidden property.
        /// </value>
        property
        Boolean
        IsHidden
        {
            Boolean get();
        }

        /// <value>
        /// Device query is enabled property.
        /// </value>
        property
        Boolean
        IsDevQueryEnabled
        {
            Boolean get();
        }

        /// <value>
        /// Printer keeps printed jobs property.
        /// </value>
        property
        Boolean
        KeepPrintedJobs
        {
            Boolean get();
        }

        /// <value>
        /// Completed jobs are scheduled first property.
        /// </value>
        property
        Boolean
        ScheduleCompletedJobsFirst
        {
            Boolean get();
        }

        /// <value>
        /// Is bidirectional communication enabled for printer property.
        /// </value>
        property
        Boolean
        IsBidiEnabled
        {
            Boolean get();
        }

        /// <value>
        /// Is raw printing enabled property.
        /// </value>
        property
        Boolean
        IsRawOnlyEnabled
        {
            Boolean get();
        }

        /// <value>
        /// Printer is published in Directory services property.
        /// </value>
        property
        Boolean
        IsPublished
        {
            Boolean get();
        }


        /// <value>
        /// Printer is an xps device or a GDI based device.
        /// </value>
        property
        Boolean
        IsXpsDevice
        {
            public:
                Boolean get();

            internal:
                void set(Boolean isMetroEnabled);
        }

        /// <value>
        /// Maximum print schema version property.
        /// </value>
        property
        static
        Int32
        MaxPrintSchemaVersion
        {
            Int32 get();
        }

        /// <value>
        /// Client print schema version property.
        /// </value>
        property
        Int32
        ClientPrintSchemaVersion
        {
            Int32 get();
        }

        property
        Boolean
        InPartialTrust
        {
            void set(Boolean isPT);

            Boolean get();
        }

        /// <summary>
        /// Commits the properties marked as modified to the Print Spooler service.
        /// </summary>
        /// <remarks>
        /// Inherited from PrintSystemObject.
        /// </remarks>
        /// <exception cref="PrintCommitAttributesException">Thrown on failure or partial success.</exception>
        virtual void
        Commit(
            void
            ) override;

        /// <summary>
        /// Synchronizes the data in the properties with the live data from the Print Spooler service.
        /// </summary>
        /// <remarks>
        /// When calling Refresh, data in uncommitted properties is lost.
        /// Inherited from PrintSystemObject.
        /// </remarks>
        /// <exception cref="PrintQueueException">Thrown on failure.</exception>
        virtual void
        Refresh(
            void
            ) override;

        internal:
        /// <summary>
        /// Return an implementation ot ILegacyDevice implementation for printing to legacy printers
        /// </summary>
        /// <remarks>
        /// This is only used by XpsFramework.DLL. It should be made internal or using link demand.
        /// </remarks>
        [FriendAccessAllowed]
        ILegacyDevice ^
        GetLegacyDevice(
            void
            );

        [FriendAccessAllowed]
        static
        unsigned
        GetDpiX(
            ILegacyDevice ^legacyDevice
            );

        [FriendAccessAllowed]
        static
        unsigned
        GetDpiY(
            ILegacyDevice ^legacyDevice
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override sealed;


        internal:

        /* --------------------------------------------------------------
           The Longhorn API classes are dealing with both new types and
           downlovel types thunked to the Win32 APIs. To give an example:

           The Print Server in win32 == a string
           The Print Server in LAPI  == a PrintServer object

           and so is the case for many other types like Drivers, Port ...
           etc. For that reason I am introducing those internal members
           like HostingPrintServerName and DefaultDevMode to allow this
           conversion when going to downlevel from LAPI and the other way
           around.

           On the level of collections, I have a collection of the LAPI
           properties and a collection of the Win32 properties and the
           InternalPropertiesCollection can pick the right collection
           based on the property name.
        ---------------------------------------------------------------- */

        virtual PrintPropertyDictionary^
        get_InternalPropertiesCollection(
            String^ attributeName
            ) override;

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )
        property
        String^
        HostingPrintServerName
        {
            void set(String^ printServerName);
        }

        property
        array<Byte>^
        DefaultDevMode
        {
            void set(array<Byte>^ devMode);
        }

        property
        array<Byte>^
        UserDevMode
        {
            void set(array<Byte>^ devMode);
        }

        property
        String^
        QueueDriverName
        {
            void set(String^ driverName);
        }

        property
        String^
        QueuePortName
        {
            void set(String^ portName);
        }

        property
        String^
        QueuePrintProcessorName
        {
            void set(String^ printProcessorName);
        }

        property
        Int32
        Status
        {
            void set(Int32 status);
        }

        property
        Int32
        Attributes
        {
            void set(Int32 attributes);
        }

        property
        MS::Internal::PrintWin32Thunk::PrinterThunkHandler^
        PrinterThunkHandler
        {
            MS::Internal::PrintWin32Thunk::PrinterThunkHandler^ get();
        }
        #pragma warning ( default:4376 )

        static
        array<String^>^
        GetAllPropertiesFilter(
            void
            );

        array<String^>^
        GetAlteredPropertiesFilter(
            StringCollection^ collection
            );

        static
        array<String^>^
        GetAllPropertiesFilter(
            array<String^>^ propertiesFilter
            );

        static
        void
        RegisterAttributesNamesTypes(
            void
            );

        static
        PrintSystemObject^
        Instantiate(
            array<String^>^ propertiesFilter
            );

        static
        PrintSystemObject^
        InstantiateOptimized(
            Object^             printServer,
            array<String^>^     propertiesFilter
            );

        static
        PrintProperty^
        CreateAttributeNoValue(
            String^
            );

        static
        PrintProperty^
        CreateAttributeValue(
            String^,
            Object^
            );

        static
        PrintProperty^
        CreateAttributeNoValueLinked(
            String^,
            MulticastDelegate^
            );

        static
        PrintProperty^
        CreateAttributeValueLinked(
            String^,
            Object^,
            MulticastDelegate^
            );

        internal:

        static
        String^
        GetAttributeNamePerPrintQueueObject(
            PrintProperty^ attributeValue
            );

        static
        Object^
        GetAttributeValuePerPrintQueueObject(
            PrintProperty^  attributeValue
            );

        static
        array<String^>^
        ConvertPropertyFilterToString(
            array<PrintQueueIndexedProperty>^          propertiesFilter
            );

        static
        String^
        BuildPortNamesString(
            array<String^>^ portNames
            );

        static
        Stream^
        ClonePrintTicket(
            Stream^ printTicket
            );

        void
        ActivateBrowsableQueue(
            void
            );

        public:

        //
        // XPSEmitter Implementation
        //

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            PrintQueue^     printQueue
            );

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            double%     width,
            double%     height
            );

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            PrintDocumentImageableArea^%  documentImageableArea
            );

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            PrintDocumentImageableArea^%                        documentImageableArea,
            System::Windows::Controls::PageRangeSelection%      pageRangeSelection,
            System::Windows::Controls::PageRange%               pageRange
            );

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            String^                       jobDescription,
            PrintDocumentImageableArea^%  documentImageableArea
            );

        static
        XpsDocumentWriter^
        CreateXpsDocumentWriter(
            String^                                             jobDescription,
            PrintDocumentImageableArea^%                        documentImageableArea,
            System::Windows::Controls::PageRangeSelection%      pageRangeSelection,
            System::Windows::Controls::PageRange%               pageRange
            );

        internal:

        static
        bool
        IsMxdwLegacyDriver(
            PrintQueue^ printQueue
        );

        PackageSerializationManager^
        CreateSerializationManager(
            bool    isBatchMode
            );

        PackageSerializationManager^
        CreateSerializationManager(
            bool    isBatchMode,
            bool    mustSetJobIdentifier
            );

        PackageSerializationManager^
        CreateSerializationManager(
            bool    isBatchMode,
            bool    mustSetJobIdentifier,
            PrintTicket^ printTicket
            );

        PackageSerializationManager^
        CreateAsyncSerializationManager(
            bool    isBatchMode
            );

        PackageSerializationManager^
        CreateAsyncSerializationManager(
            bool         isBatchMode,
            bool         mustSetJobIdentifier,
            PrintTicket^ printTicket
            );

        PackageSerializationManager^
            CreateXpsOMSerializationManager(
            bool         isBatchMode,
            bool         isAsync,
            PrintTicket^ printTicket,
            bool         mustSetPrintJobIdentifier
            );

        void
        DisposeSerializationManager(
            void
            );

        void
        DisposeSerializationManager(
            bool abort
            );

        void
            EnsureJobId(
            PackageSerializationManager^ manager
            );

        static
        String^         defaultXpsJobName;

        
        property
        RCW::IXpsOMPackageWriter^
        XpsOMPackageWriter
        {
            void set(RCW::IXpsOMPackageWriter^ packageWriter);
        }

        internal:

        Int32
        PrintQueue::
        XpsDocumentEvent(
            XpsDocumentEventType    escape,
            SafeHandle^             inputBufferSafeHandle
            );


        Int32
        XpsDocumentEventPrintTicket(
            XpsDocumentEventType                                                            preEscape,
            XpsDocumentEventType                                                            postEscape,
            SafeHandle^                                                                     inputBufferSafeHandle,
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^     e
            );

        void
        XpsDocumentEventCancel(
            void
            );

        private:

        static
        PrintQueue(
            void
            )
        {
            upLevelToDownLevelMapping = gcnew Hashtable();
            attributeNameTypes        = gcnew Hashtable();

            defaultXpsJobName         = gcnew String("Xps Document");

            //
            // Map upLevel properties to downLevel properties
            //
            if(upLevelAttributeName->Length != downLevelAttributeName->Length)
            {
                InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

                String^ resString = manager->GetString("IndexOutOfRangeException.InvalidSize",
                                                       System::Threading::Thread::CurrentThread->CurrentUICulture);

                String^ exceptionMessage = String::Format(System::Threading::Thread::CurrentThread->CurrentUICulture,
                                                          resString,
                                                          "upLevelAttributeName",
                                                          "downLevelAttributeName");

                throw gcnew IndexOutOfRangeException(exceptionMessage);
            }

            for(Int32 numOfMappings = 0;
                numOfMappings < upLevelAttributeName->Length;
                numOfMappings++)
            {
                upLevelToDownLevelMapping->Add(upLevelAttributeName[numOfMappings],
                                               downLevelAttributeName[numOfMappings]);
            }
        }

        array<MulticastDelegate^>^
        CreatePropertiesDelegates(
            void
            );

        void
        VerifyAccess(
            void
            );

        void
        InitializeInternalCollections(
            void
            );

        void
        InitializePrintTickets(
            void
            );

        void
        Initialize(
            PrintServer^                printServer,
            String^                     printQueueName,
            array<String^>^             propertiesFilter,
            PrinterDefaults^            printerDefaults
            );

        void
        GetUnInitializedData(
            String^ upLevelPropertyName,
            String^ downlevelPropertyName
            );

        String^
        PrepareNameForDownLevelConnectivity(
            String^ serverName,
            String^ printerName
            );

        Boolean
        GetIsXpsDevice(
            void
            );

        void
        ForwardXpsDriverDocEvent(
            Object^                                                                        sender,
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    args
            );

        void
        PrintQueue::
        ForwardXpsFixedDocumentSequenceEvent(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );

        void
        ForwardXpsFixedDocumentEvent(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );

        void
        ForwardXpsFixedPageEvent(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );


        void
        ForwardXpsFixedDocumentSequencePrintTicket(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );

        void
        ForwardXpsFixedDocumentPrintTicket(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );

        void
        ForwardXpsFixedPagePrintTicket(
            System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
            );

        Boolean
        IsXpsDocumentEventSupported(
            XpsDocumentEventType    escape
            );

        ref class PartialTrustPrintTicketEventHandler
        {
            public:

            PartialTrustPrintTicketEventHandler(
                PrintTicket^        printTicket
                );

            void
            SetPrintTicketInPartialTrust(
                Object^                                sender,
                WritingPrintTicketRequiredEventArgs^   e
            );

            private:

            PrintTicket^        partialTrustPrintTicket;

            Boolean             isPrintTicketHandedOver;
        };

        static
        bool
        ShowPrintDialog(
            XpsDocumentWriter^%     xpsDocumentWriter,
            PrintTicket^%           partialTrustPrintTicket,
            PrintQueue^%            partialTrustPrintQueue,
            double%                 width,
            double%                 height,
            String^                 jobDescription
            );

        static
        bool
        ShowPrintDialogEnablePageRange(
            XpsDocumentWriter^%                             xpsDocumentWriter,
            PrintTicket^%                                   partialTrustPrintTicket,
            PrintQueue^%                                    partialTrustPrintQueue,
            double%                                         width,
            double%                                         height,
            System::Windows::Controls::PageRangeSelection%  pageRangeSelection,
            System::Windows::Controls::PageRange%           pageRange,
            String^                                         jobDescription
            );

        static
        bool
        PrintQueue::
        GatherDataFromPrintDialog(
            System::Windows::Controls::PrintDialog^ printDialog,
            XpsDocumentWriter^%     writer,
            PrintTicket^%           partialTrustPrintTicket,
            PrintQueue^%            partialTrustPrintQueue,
            double%                 width,
            double%                 height,
            String^                 jobDescription
            );

        static
        PrintDocumentImageableArea^
        CalculateImagableArea(
            PrintTicket^        partialTrustPrintTicket,
            PrintQueue^         partialTrustPrintQueue,
            double              height,
            double              width
            );

        Exception^
        CreatePrintQueueException(
            int hresult,
            String^ messageId
            );

        static
        Exception^
        CreatePrintSystemException(
            int hresult,
            String^ messageId
            );

        //
        //  The following are the set of propeties mapping
        //  the old Win32 properties of a PrintQueue to the
        //  new object types
        //

        bool                                isDisposed;
        Int32                               priority;
        Int32                               defaultPriority;
        Int32                               startTime;
        Int32                               untilTime;
        Int32                               averagePagesPerMinute;
        Int32                               numberOfJobs;

        String^                             shareName;
        String^                             comment;
        String^                             location;
        String^                             description;

        String^                             separatorFile;

        PrintTicket^                        userPrintTicket;

        PrintTicket^                        defaultPrintTicket;

        PrintQueueAttributes                queueAttributes;
        PrintQueueStatus                    queueStatus;

        PrintPort^                          queuePort;
        PrintDriver^                        queueDriver;
        PrintProcessor^                     queuePrintProcessor;
        PrintServer^                        hostingPrintServer;
        String^                             hostingPrintServerName;

        PrintTicketManager^                 printTicketManager;

        PrintJobSettings^                   _currentJobSettings;

        //
        // The following set of boolean properties represent
        // the status of the PrintQueue
        //
        Boolean                             isPaused;
        Boolean                             isInError;
        Boolean                             isPendingDeletion;
        Boolean                             isPaperJammed;
        Boolean                             isOutOfPaper;
        Boolean                             isManualFeedRequired;
        Boolean                             hasPaperProblem;
        Boolean                             isOffline;
        Boolean                             isIOActive;
        Boolean                             isBusy;
        Boolean                             isPrinting;
        Boolean                             isOutputBinFull;
        Boolean                             isNotAvailable;
        Boolean                             isWaiting;
        Boolean                             isProcessing;
        Boolean                             isInitializing;
        Boolean                             isWarmingUp;
        Boolean                             isTonerLow;
        Boolean                             hasNoToner;
        Boolean                             doPagePunt;
        Boolean                             needUserIntervention;
        Boolean                             isOutOfMemory;
        Boolean                             isDoorOpened;
        Boolean                             isServerUnknown;
        Boolean                             isPowerSaveOn;
        Boolean                             printingIsCancelled;

        //
        // The following set of boolean properties represent
        // the Attributes of the PrintQueue
        //
        Boolean                             isQueued;
        Boolean                             isDirect;
        Boolean                             isShared;
        Boolean                             isHidden;
        Boolean                             isDevQueryEnabled;
        Boolean                             arePrintedJobsKept;
        Boolean                             areCompletedJobsScheduledFirst;
        Boolean                             isBidiEnabled;
        Boolean                             isRawOnlyEnabled;
        Boolean                             isPublished;
        Boolean                             isXpsDevice;

        Boolean                             runsInPartialTrust;

        //
        // The following is the necessary data members to link the
        // compile time properties with the named properties in the
        // associated collection
        //
        static array<String^>^ primaryAttributeNames =
        {
            L"ShareName",
            L"Comment",
            L"Location",
            L"Description",
            L"Priority",
            L"DefaultPriority",
            L"StartTimeOfDay",
            L"UntilTimeOfDay",
            L"AveragePagesPerMinute",
            L"NumberOfJobs",
            L"QueueAttributes",
            L"QueueDriver",
            L"QueuePort",
            L"QueuePrintProcessor",
            L"HostingPrintServer",
            L"QueueStatus",
            L"SeparatorFile",
            L"DefaultPrintTicket",
            L"UserPrintTicket",
            L"IsXpsEnabled"
        };

        static array<Type^>^ primaryAttributeTypes =
        {
            String::typeid,
            String::typeid,
            String::typeid,
            String::typeid,
            Int32::typeid,
            Int32::typeid,
            Int32::typeid,
            Int32::typeid,
            Int32::typeid,
            Int32::typeid,
            PrintQueueAttributes::typeid,
            PrintDriver::typeid,
            PrintPort::typeid,
            PrintProcessor::typeid,
            PrintServer::typeid,
            PrintQueueStatus::typeid,
            String::typeid,
            PrintTicket::typeid,
            PrintTicket::typeid,
            Boolean::typeid
        };

        static array<String^>^ secondaryAttributeNames =
        {
            L"HostingPrintServerName",
            L"QueueDriverName",
            L"QueuePrintProcessorName",
            L"QueuePortName",
            L"DefaultDevMode",
            L"UserDevMode",
            L"Status",
            L"Attributes"
        };

        static array<Type^>^ secondaryAttributeTypes=
        {
            String::typeid,
            String::typeid,
            String::typeid,
            String::typeid,
            array<Byte>::typeid,
            array<Byte>::typeid,
            Int32::typeid,
            Int32::typeid
        };

        static array<String^>^ upLevelAttributeName =
        {
            L"HostingPrintServer",
            L"QueueDriver",
            L"QueuePrintProcessor",
            L"QueuePort",
            L"DefaultPrintTicket",
            L"UserPrintTicket",
            L"QueueStatus",
            L"QueueAttributes",
        };

        static array<String^>^ downLevelAttributeName =
        {
            L"HostingPrintServerName",
            L"QueueDriverName",
            L"QueuePrintProcessorName",
            L"QueuePortName",
            L"DefaultDevMode",
            L"UserDevMode",
            L"Status",
            L"Attributes"
        };

        Hashtable^ collectionsTable;

        static Hashtable^ attributeNameTypes;
        static Hashtable^ upLevelToDownLevelMapping;

        //
        // I distinct between Print Objects returned from an
        // object instantiation and those returned from an
        // enumeration
        //
        Boolean     isBrowsable;

        //
        // I mentain the following internal Properties Filter
        // list to know at refresh time which properties should
        // be refreshed
        //
        array<String^>^     refreshPropertiesFilter;

        //
        // The fully qualified printer name is required by PrintTicket
        // methods and classes
        //
        String^ fullQueueName;

        //
        // The following are the members required by the PrintQueue to
        // thunk in the Win32 APIs
        //
        MS::Internal::PrintWin32Thunk::PrinterThunkHandler^    printerThunkHandler;
        PrintPropertyDictionary^                               thunkPropertiesCollection;
        //
        // PrintTicket client specific version
        //
        Int32   clientPrintSchemaVersion;
        //
        // Data specific to XpsDocumentWriter
        //
        bool                            isWriterAttached;

        XpsDocument^                    xpsDocument;
        PrintQueueStream^               writerStream;

        array<Byte>^                    userDevMode;
        array<Byte>^                    defaultDevMode;

        PrintSystemDispatcherObject^    accessVerifier;

        Object^                         _lockObject;

        XpsCompatiblePrinter^           xpsCompatiblePrinter;
    };

    public ref class PrintQueueCollection :
    public PrintSystemObjects,
    public System::Collections::Generic::IEnumerable<PrintQueue^>,
    public IEnumerable,
    public IDisposable
    {
        public:

        PrintQueueCollection(
            void
            );

        PrintQueueCollection(
            PrintServer^                        printServer,
            array<String^>^                     propertyFilter,
            array<EnumeratedPrintQueueTypes>^   enumerationFlag
            );

        PrintQueueCollection(
            PrintServer^        printServer,
            array<String^>^     propertyFilter
            );

        ~PrintQueueCollection(
            void
            );

        virtual System::Collections::IEnumerator^ GetNonGenericEnumerator() = System::Collections::IEnumerable::GetEnumerator;


        virtual
        System::
        Collections::
        Generic::
        IEnumerator<PrintQueue^>^
        GetEnumerator(
            void
            );

        property
        static
        Object^
        SyncRoot
        {
            Object^ get();
        }

        void
        Add(
            PrintQueue^ printObject
            );

        private:

        static
        PrintQueueCollection(
            void
            )
        {
            PrintQueueCollection::syncRoot = gcnew Object();
        }

        array<String^>^
        AddNameAndHostToProperties(
            array<String^>^ propertyFilter
            );

        void
        VerifyAccess(
            void
            );


        static
        volatile
        Object^     syncRoot;

        //
        // This is only for illustrative purposes. It could be another
        // specialized collection that we build and the Enumerator of
        // which would be linked to the PrinterQueuesEnumerator
        //
        System::Collections::Generic::Queue<PrintQueue ^>^      printQueuesCollection;
        PrintSystemDispatcherObject^    accessVerifier;

    };


}
}

#endif

