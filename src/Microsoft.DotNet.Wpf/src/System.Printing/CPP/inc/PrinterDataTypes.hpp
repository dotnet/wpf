// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTERDATATYPES_HPP__
#define __PRINTERDATATYPES_HPP__

namespace System
{
namespace Printing
{
    /// <summary>
    /// This is a flag enumeration of the print queue attributes.
    /// <list type="table">
    /// <item>
    /// <term>Queued</term>
    /// <description>If set, the printer spools and starts printing after the last page is spooled.</description>
    /// </item>
    /// <item>
    /// <term>Direct</term>
    /// <description>Job is sent directly to the printer.</description>
    /// </item>
    /// <item>
    /// <term>Shared</term>
    /// <description>Printer is shared.</description>
    /// </item>
    /// <item>
    /// <term>Hidden</term>
    /// <description>Reserved.</description>
    /// </item>
    /// <item>
    /// <term>EnableDevQuery</term>
    /// <description>If set, DevQueryPrint is called.</description>
    /// </item>
    /// <item>
    /// <term>KeepPrintedJobs</term>
    /// <description>If set, jobs are kept after they are printed. If unset, jobs are deleted.</description>
    /// </item>
    /// <item>
    /// <term>CompleteFirst</term>
    /// <description>If set and printer is set for print-while-spooling, any jobs that have completed spooling are scheduled to print before jobs that have not completed spooling.</description>
    /// </item>
    /// <item>
    /// <term>EnableBidi</term>
    /// <description> Indicates whether bi-directional communications are enabled for the printer.</description>
    /// </item>
    /// <item>
    /// <term>RawOnly</term>
    /// <description>Indicates that only raw data type print jobs can be spooled.</description>
    /// </item>
    /// <item>
    /// <term>Published</term>
    /// <description>Indicates whether the printer is published in the directory service.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <ExternalAPI/>
    [FlagsAttribute]
    public enum class PrintQueueAttributes 
    {
        None                = 0x00000000,
        Queued              = 0x00000001,
        Direct              = 0x00000002,
        Shared              = 0x00000008,
        Hidden              = 0x00000020,
        EnableDevQuery      = 0x00000080,
        KeepPrintedJobs     = 0x00000100,
        ScheduleCompletedJobsFirst = 0x00000200,
        EnableBidi          = 0x00000800,
        RawOnly             = 0x00001000,
        Published           = 0x00002000
    };

    /// <summary>
    /// This is a flag enumeration of the print queue status.
    /// <list type="table">
    /// <item>
    /// <term>Uninitialized</term>
    /// <description>Print Queue is in uninitialized state.</description>
    /// </item>
    /// <item>
    /// <term>Paused</term>
    /// <description>The printer is paused.</description>
    /// </item>
    /// <item>
    /// <term>Error</term>
    /// <description>The printer is in an error state.</description>
    /// </item>
    /// <item>
    /// <term>PendingDeletion</term>
    /// <description>The printer is being deleted.</description>
    /// </item>
    /// <item>
    /// <term>PaperJam</term>
    /// <description>Paper is jammed in the printer.</description>
    /// </item>
    /// <item>
    /// <term>PaperOut</term>
    /// <description>The printer is out of paper.</description>
    /// </item>
    /// <item>
    /// <term>ManualFeed</term>
    /// <description>The printer is in a manual feed state.</description>
    /// </item>
    /// <item>
    /// <term>PaperProblem</term>
    /// <description>The printer has a paper problem.</description>
    /// </item>
    /// <item>
    /// <term>Offline</term>
    /// <description>The printer is offline.</description>
    /// </item>
    /// <item>
    /// <term>IOActive</term>
    /// <description>The printer is in an active input/output state.</description>
    /// </item>
    /// <item>
    /// <term>Busy</term>
    /// <description>The printer is busy.</description>
    /// </item>
    /// <item>
    /// <term>Printing</term>
    /// <description>The printer is printing.</description>
    /// </item>
    /// <item>
    /// <term>OutputBinFull</term>
    /// <description>The printer's output bin is full.</description>
    /// </item>
    /// <item>
    /// <term>NotAvailable</term>
    /// <description>The printer is not available for printing.</description>
    /// </item>    
    /// <item>
    /// <term>Waiting</term>
    /// <description>The printer is waiting.</description>
    /// </item>    
    /// <item>
    /// <term>Processing</term>
    /// <description>The printer is processing a print job.</description>
    /// </item>    
    /// <item>
    /// <term>Initializing</term>
    /// <description>The printer is initializing.</description>
    /// </item>    
    /// <item>
    /// <term>WarmingUp</term>
    /// <description>The printer is warming up</description>
    /// </item>    
    /// <item>
    /// <term>TonerLow</term>
    /// <description>The printer is low on toner.</description>
    /// </item>    
    /// <item>
    /// <term>NoToner</term>
    /// <description>The printer is out of toner.</description>
    /// </item>
    /// <item>
    /// <term>PagePunt</term>
    /// <description>The printer cannot print the current page.</description>
    /// </item>
    /// <item>
    /// <term>UserIntervention</term>
    /// <description>The printer has an error that requires the user to do something.</description>
    /// </item>
    /// <item>
    /// <term>OutOfMemory</term>
    /// <description>The printer has run out of memory.</description>
    /// </item>    
    /// <item>
    /// <term>DoorOpen</term>
    /// <description>The printer door is open.</description>
    /// </item>    
    /// <item>
    /// <term>ServerUnknown</term>
    /// <description>The printer status is unknown.</description>
    /// </item>
    /// <item>
    /// <term>PowerSave</term>
    /// <description>The printer is in power save mode.</description>
    /// </item>    
    /// </list>
    /// </summary>
    /// <ExternalAPI/>
    [FlagsAttribute]
    public enum class PrintQueueStatus 
    {
        None                = 0x00000000,
        Paused              = 0x00000001,
        Error               = 0x00000002,
        PendingDeletion     = 0x00000004,
        PaperJam            = 0x00000008,
        PaperOut            = 0x00000010,
        ManualFeed          = 0x00000020,
        PaperProblem        = 0x00000040,
        Offline             = 0x00000080,
        IOActive            = 0x00000100,
        Busy                = 0x00000200,
        Printing            = 0x00000400,
        OutputBinFull       = 0x00000800,
        NotAvailable        = 0x00001000,
        Waiting             = 0x00002000,
        Processing          = 0x00004000,
        Initializing        = 0x00008000,
        WarmingUp           = 0x00010000,
        TonerLow            = 0x00020000,
        NoToner             = 0x00040000,
        PagePunt            = 0x00080000,
        UserIntervention    = 0x00100000,
        OutOfMemory         = 0x00200000,
        DoorOpen            = 0x00400000,
        ServerUnknown       = 0x00800000,
        PowerSave           = 0x01000000
    };

    /// <summary>
    /// This is the enumeration of the attributes that can be used when enumerating <c>PrintQueue</c> objects
    /// on a <c>PrintServer</c>.
    /// <list type="table">
    /// <item>
    /// <term>Local</term>
    /// <description>Enumerates local printers on the target server.</description>
    /// </item>
    /// <item>
    /// <term>Shared</term>
    /// <description>Enumerates shared printers.</description>
    /// </item>    
    /// <item>
    /// <term>Connections</term>
    /// <description>Enumerates printer connections on the target server.</description>
    /// </item>
    /// <item>
    /// <term>TerminalServer</term>
    /// <description>Enumerates printers installed through TS redirection feature.</description>
    /// </item>
    /// <item>
    /// <term>Fax</term>
    /// <description>Enumerates fax queues.</description>
    /// </item>
    /// <item>
    /// <term>KeepPrintedJobs</term>
    /// <description>Enumerates print queues that keep the printed jobs in the queue after done printing.</description>
    /// </item>
    /// <item>
    /// <term>EnableBidi</term>
    /// <description>Enumerates print queues that enable bi-directional communication to the device.</description>
    /// </item>  
    /// <item>
    /// <term>RawOnly</term>
    /// <description>Enumerates print queues that spool only raw data type print jobs.</description>
    /// </item>    
    /// <item>
    /// <term>WorkOffline</term>
    /// <description>Enumerates print queues that work offline.</description>
    /// </item>    
    /// <item>
    /// <term>PublishedInDS</term>
    /// <description>Enumerates print queues that are published in the directory service.</description>
    /// </item>
    /// <item>
    /// <term>DirectPrinting</term>
    /// <description>Enumerates print queues that send the jobs directly to the device, without doing spooling.</description>
    /// </item>
    /// <item>
    /// <term>Queued</term>
    /// <description>Enumerates print queues that spool and start printing after the last page is spooled</description>
    /// </item>
    /// <item>
    /// <term>PushedUserConnection</term>
    /// <description>Enumerates print queues that were installed via Push Printer Connections user policy.</description>
    /// </item>
    /// <item>
    /// <term>PushedMachineConnection</term>
    /// <description>Enumerates print queues that were installed via Push Printer Connections machine policy.</description>
    /// </item>
    /// <item>
    /// <term>AllowEnhancedMetaFilePrinting</term>
    /// <description>Enumerates print queues that allow Enhanced Meta-File (EMF) printing.</description>
    /// </item>
    /// <item>
    /// <term>EnableDevQuery</term>
    /// <description>Enumerates print queues that enables DevQueryPrint calls.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <ExternalAPI/>
    [FlagsAttribute]
    public enum class EnumeratedPrintQueueTypes
    {
        Local                           = PRINTER_ATTRIBUTE_LOCAL,   
        Shared                          = PRINTER_ATTRIBUTE_SHARED,
        Connections                     = PRINTER_ATTRIBUTE_NETWORK,
        TerminalServer                  = PRINTER_ATTRIBUTE_TS,
        Fax                             = PRINTER_ATTRIBUTE_FAX,
        KeepPrintedJobs                 = PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS,
        EnableBidi                      = PRINTER_ATTRIBUTE_ENABLE_BIDI,
        RawOnly                         = PRINTER_ATTRIBUTE_RAW_ONLY,
        WorkOffline                     = PRINTER_ATTRIBUTE_WORK_OFFLINE,
        PublishedInDirectoryServices    = PRINTER_ATTRIBUTE_PUBLISHED,
        DirectPrinting                  = PRINTER_ATTRIBUTE_DIRECT,
        Queued                          = PRINTER_ATTRIBUTE_QUEUED,
        PushedUserConnection            = PRINTER_ATTRIBUTE_PUSHED_USER,
        PushedMachineConnection         = PRINTER_ATTRIBUTE_PUSHED_MACHINE,
        EnableDevQuery                  = PRINTER_ATTRIBUTE_ENABLE_DEVQ
    };

    /// <summary>
    /// This is the enumeration of the Print Spooler error events that are logged. 
    /// <list type="table">
    /// <item>
    /// <term>LogPrintingSuccessEvents</term>
    /// <description>The Print Spooler will log success events in the event log.</description>
    /// </item>
    /// <item>
    /// <term>LogPrintingErrorEvents</term>
    /// <description>The Print Spooler will log error events in the event log.</description>
    /// </item>
    /// <item>
    /// <term>LogPrintingWarningEvents</term>
    /// <description>The Print Spooler will log warning events in the event log.</description>
    /// </item>
    /// <item>
    /// <term>LogPrintingInformationEvents</term>
    /// <description>The Print Spooler will log informational events in the event log.</description>
    /// </item>
    /// <item>
    /// <term>LogAllPrintingEvents</term>
    /// <description>The Print Spooler will log all events in the event log.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <ExternalAPI/>
    [FlagsAttribute]
    public enum class PrintServerEventLoggingTypes
    {
        None                            ,
        LogPrintingSuccessEvents        ,
        LogPrintingErrorEvents          ,
        LogPrintingWarningEvents        ,
        LogPrintingInformationEvents    ,
        LogAllPrintingEvents            
    };

    [FlagsAttribute]
    public enum class PrintJobStatus 
    {
        None                = 0x00000000,
        Paused              = 0x00000001,
        Error               = 0x00000002,
        Deleting            = 0x00000004,
        Spooling            = 0x00000008,
        Printing            = 0x00000010,
        Offline             = 0x00000020,
        PaperOut            = 0x00000040,
        Printed             = 0x00000080,
        Deleted             = 0x00000100,
        Blocked             = 0x00000200,
        UserIntervention    = 0x00000400,
        Restarted           = 0x00000800,
        Completed           = 0x00001000,
        Retained            = 0x00002000
    };

    public enum class PrintJobPriority
    {
        None    = 0,
        Minimum = 1,
        Default = 1,
        Maximum = 99
    };


    public enum class PrintJobType
    {
        None    = 0,
        Xps     = 1,
        Legacy  = 2
    };
}
}

#endif
