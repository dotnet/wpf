// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Reflection;

namespace System.Printing
{
    [System.FlagsAttribute]
    public enum EnumeratedPrintQueueTypes
    {
        Connections = 16,
        DirectPrinting = 2,
        EnableBidi = 2048,
        EnableDevQuery = 128,
        Fax = 16384,
        KeepPrintedJobs = 256,
        Local = 64,
        PublishedInDirectoryServices = 8192,
        PushedMachineConnection = 262144,
        PushedUserConnection = 131072,
        Queued = 1,
        RawOnly = 4096,
        Shared = 8,
        TerminalServer = 32768,
        WorkOffline = 1024,
    }
    public sealed partial class LocalPrintServer : System.Printing.PrintServer
    {
        public LocalPrintServer() { }
        public LocalPrintServer(System.Printing.LocalPrintServerIndexedProperty[] propertiesFilter) { }
        public LocalPrintServer(System.Printing.LocalPrintServerIndexedProperty[] propertiesFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public LocalPrintServer(System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public LocalPrintServer(string[] propertiesFilter) { }
        public LocalPrintServer(string[] propertiesFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public System.Printing.PrintQueue DefaultPrintQueue { get { throw null; } set { } }
        public sealed override void Commit() { }
        public bool ConnectToPrintQueue(System.Printing.PrintQueue printer) { throw null; }
        public bool ConnectToPrintQueue(string printQueuePath) { throw null; }
        public bool DisconnectFromPrintQueue(System.Printing.PrintQueue printer) { throw null; }
        public bool DisconnectFromPrintQueue(string printQueuePath) { throw null; }
        public static System.Printing.PrintQueue GetDefaultPrintQueue() { throw null; }
        public sealed override void Refresh() { }
    }
    public enum LocalPrintServerIndexedProperty
    {
        BeepEnabled = 5,
        DefaultPortThreadPriority = 2,
        DefaultPrintQueue = 12,
        DefaultSchedulerPriority = 4,
        DefaultSpoolDirectory = 0,
        EventLog = 7,
        MajorVersion = 8,
        MinorVersion = 9,
        NetPopup = 6,
        PortThreadPriority = 1,
        RestartJobOnPoolEnabled = 11,
        RestartJobOnPoolTimeout = 10,
        SchedulerPriority = 3,
    }
    public partial class PrintDocumentImageableArea
    {
        internal PrintDocumentImageableArea() { }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        public double MediaSizeHeight { get { throw null; } }
        public double MediaSizeWidth { get { throw null; } }
        public double OriginHeight { get { throw null; } }
        public double OriginWidth { get { throw null; } }
    }
    public sealed partial class PrintDriver : System.Printing.PrintFilter
    {
        internal PrintDriver() { }
        public sealed override void Commit() { }
        protected sealed override void InternalDispose(bool disposing) { }
        public sealed override void Refresh() { }
    }
    public abstract partial class PrintFilter : System.Printing.PrintSystemObject
    {
        internal PrintFilter() { }
        protected override void InternalDispose(bool disposing) { }
    }
    public partial class PrintJobInfoCollection : System.Printing.PrintSystemObjects, System.Collections.Generic.IEnumerable<System.Printing.PrintSystemJobInfo>, System.Collections.IEnumerable, System.IDisposable
    {
        public PrintJobInfoCollection(System.Printing.PrintQueue printQueue, string[] propertyFilter) { }
        public void Add(System.Printing.PrintSystemJobInfo printObject) { }
        protected override void Dispose(bool A_0) { }
        public virtual System.Collections.Generic.IEnumerator<System.Printing.PrintSystemJobInfo> GetEnumerator() { throw null; }
        // manual
        public virtual System.Collections.IEnumerator GetNonGenericEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public enum PrintJobPriority
    {
        Default = 1,
        Maximum = 99,
        Minimum = 1,
        None = 0,
    }
    public partial class PrintJobSettings
    {
        internal PrintJobSettings() { }
        public System.Printing.PrintTicket CurrentPrintTicket { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum PrintJobStatus
    {
        Blocked = 512,
        Completed = 4096,
        Deleted = 256,
        Deleting = 4,
        Error = 2,
        None = 0,
        Offline = 32,
        PaperOut = 64,
        Paused = 1,
        Printed = 128,
        Printing = 16,
        Restarted = 2048,
        Retained = 8192,
        Spooling = 8,
        UserIntervention = 1024,
    }
    public enum PrintJobType
    {
        Legacy = 2,
        None = 0,
        Xps = 1,
    }
    public sealed partial class PrintPort : System.Printing.PrintSystemObject
    {
        internal PrintPort() { }
        public sealed override void Commit() { }
        protected sealed override void InternalDispose(bool disposing) { }
        public sealed override void Refresh() { }
    }
    public sealed partial class PrintProcessor : System.Printing.PrintFilter
    {
        internal PrintProcessor() { }
        public sealed override void Commit() { }
        protected sealed override void InternalDispose(bool disposing) { }
        public sealed override void Refresh() { }
    }
    public partial class PrintQueue : System.Printing.PrintSystemObject
    {
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, int printSchemaVersion) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, int printSchemaVersion, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, System.Printing.PrintQueueIndexedProperty[] propertyFilter) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, System.Printing.PrintQueueIndexedProperty[] propertyFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, string[] propertyFilter) { }
        public PrintQueue(System.Printing.PrintServer printServer, string printQueueName, string[] propertyFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public virtual int AveragePagesPerMinute { get { throw null; } }
        public int ClientPrintSchemaVersion { get { throw null; } }
        public virtual string Comment { get { throw null; } set { } }
        public System.Printing.PrintJobSettings CurrentJobSettings { get { throw null; } }
        public virtual System.Printing.PrintTicket DefaultPrintTicket { get { throw null; } set { } }
        public virtual int DefaultPriority { get { throw null; } set { } }
        public virtual string Description { get { throw null; } }
        public string FullName { get { throw null; } }
        public bool HasPaperProblem { get { throw null; } }
        public bool HasToner { get { throw null; } }
        public virtual System.Printing.PrintServer HostingPrintServer { get { throw null; } protected set { } }
        public bool InPartialTrust { get { throw null; } set { } }
        public bool IsBidiEnabled { get { throw null; } }
        public bool IsBusy { get { throw null; } }
        public bool IsDevQueryEnabled { get { throw null; } }
        public bool IsDirect { get { throw null; } }
        public bool IsDoorOpened { get { throw null; } }
        public bool IsHidden { get { throw null; } }
        public bool IsInError { get { throw null; } }
        public bool IsInitializing { get { throw null; } }
        public bool IsIOActive { get { throw null; } }
        public bool IsManualFeedRequired { get { throw null; } }
        public bool IsNotAvailable { get { throw null; } }
        public bool IsOffline { get { throw null; } }
        public bool IsOutOfMemory { get { throw null; } }
        public bool IsOutOfPaper { get { throw null; } }
        public bool IsOutputBinFull { get { throw null; } }
        public bool IsPaperJammed { get { throw null; } }
        public bool IsPaused { get { throw null; } }
        public bool IsPendingDeletion { get { throw null; } }
        public bool IsPowerSaveOn { get { throw null; } }
        public bool IsPrinting { get { throw null; } }
        public bool IsProcessing { get { throw null; } }
        public bool IsPublished { get { throw null; } }
        public bool IsQueued { get { throw null; } }
        public bool IsRawOnlyEnabled { get { throw null; } }
        public bool IsServerUnknown { get { throw null; } }
        public bool IsShared { get { throw null; } }
        public bool IsTonerLow { get { throw null; } }
        public bool IsWaiting { get { throw null; } }
        public bool IsWarmingUp { get { throw null; } }
        public bool IsXpsDevice { get { throw null; } }
        public bool KeepPrintedJobs { get { throw null; } }
        public virtual string Location { get { throw null; } set { } }
        public static int MaxPrintSchemaVersion { get { throw null; } }
        public sealed override string Name { get { throw null; } internal set { } }
        public bool NeedUserIntervention { get { throw null; } }
        public virtual int NumberOfJobs { get { throw null; } }
        public bool PagePunt { get { throw null; } }
        public bool PrintingIsCancelled { get { throw null; } set { } }
        public virtual int Priority { get { throw null; } set { } }
        public System.Printing.PrintQueueAttributes QueueAttributes { get { throw null; } }
        public virtual System.Printing.PrintDriver QueueDriver { get { throw null; } set { } }
        public virtual System.Printing.PrintPort QueuePort { get { throw null; } set { } }
        public virtual System.Printing.PrintProcessor QueuePrintProcessor { get { throw null; } set { } }
        public System.Printing.PrintQueueStatus QueueStatus { get { throw null; } }
        public bool ScheduleCompletedJobsFirst { get { throw null; } }
        public virtual string SeparatorFile { get { throw null; } set { } }
        public virtual string ShareName { get { throw null; } set { } }
        public virtual int StartTimeOfDay { get { throw null; } set { } }
        public virtual int UntilTimeOfDay { get { throw null; } set { } }
        public virtual System.Printing.PrintTicket UserPrintTicket { get { throw null; } set { } }
        public System.Printing.PrintSystemJobInfo AddJob() { throw null; }
        public System.Printing.PrintSystemJobInfo AddJob(string jobName) { throw null; }
        public System.Printing.PrintSystemJobInfo AddJob(string jobName, System.Printing.PrintTicket printTicket) { throw null; }
        public System.Printing.PrintSystemJobInfo AddJob(string jobName, string documentPath, bool fastCopy) { throw null; }
        public System.Printing.PrintSystemJobInfo AddJob(string jobName, string documentPath, bool fastCopy, System.Printing.PrintTicket printTicket) { throw null; }
        public override void Commit() { }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(ref double width, ref double height) { throw null; }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(ref System.Printing.PrintDocumentImageableArea documentImageableArea) { throw null; }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(ref System.Printing.PrintDocumentImageableArea documentImageableArea, ref System.Windows.Controls.PageRangeSelection pageRangeSelection, ref System.Windows.Controls.PageRange pageRange) { throw null; }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(System.Printing.PrintQueue printQueue) { throw null; }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(string jobDescription, ref System.Printing.PrintDocumentImageableArea documentImageableArea) { throw null; }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(string jobDescription, ref System.Printing.PrintDocumentImageableArea documentImageableArea, ref System.Windows.Controls.PageRangeSelection pageRangeSelection, ref System.Windows.Controls.PageRange pageRange) { throw null; }
        public System.Printing.PrintSystemJobInfo GetJob(int jobId) { throw null; }
        public System.Printing.PrintCapabilities GetPrintCapabilities() { throw null; }
        public System.Printing.PrintCapabilities GetPrintCapabilities(System.Printing.PrintTicket printTicket) { throw null; }
        public System.IO.MemoryStream GetPrintCapabilitiesAsXml() { throw null; }
        public System.IO.MemoryStream GetPrintCapabilitiesAsXml(System.Printing.PrintTicket printTicket) { throw null; }
        public System.Printing.PrintJobInfoCollection GetPrintJobInfoCollection() { throw null; }
        protected sealed override void InternalDispose(bool disposing) { }
        public System.Printing.ValidationResult MergeAndValidatePrintTicket(System.Printing.PrintTicket basePrintTicket, System.Printing.PrintTicket deltaPrintTicket) { throw null; }
        public System.Printing.ValidationResult MergeAndValidatePrintTicket(System.Printing.PrintTicket basePrintTicket, System.Printing.PrintTicket deltaPrintTicket, System.Printing.PrintTicketScope scope) { throw null; }
        public virtual void Pause() { }
        public virtual void Purge() { }
        public override void Refresh() { }
        public virtual void Resume() { }
    }
    [System.FlagsAttribute]
    public enum PrintQueueAttributes
    {
        Direct = 2,
        EnableBidi = 2048,
        EnableDevQuery = 128,
        Hidden = 32,
        KeepPrintedJobs = 256,
        None = 0,
        Published = 8192,
        Queued = 1,
        RawOnly = 4096,
        ScheduleCompletedJobsFirst = 512,
        Shared = 8,
    }
    public partial class PrintQueueCollection : System.Printing.PrintSystemObjects, System.Collections.Generic.IEnumerable<System.Printing.PrintQueue>, System.Collections.IEnumerable, System.IDisposable
    {
        public PrintQueueCollection() { }
        public PrintQueueCollection(System.Printing.PrintServer printServer, string[] propertyFilter) { }
        public PrintQueueCollection(System.Printing.PrintServer printServer, string[] propertyFilter, System.Printing.EnumeratedPrintQueueTypes[] enumerationFlag) { }
        public static object SyncRoot { get { throw null; } }
        public void Add(System.Printing.PrintQueue printObject) { }
        protected override void Dispose(bool A_0) { }
        public virtual System.Collections.Generic.IEnumerator<System.Printing.PrintQueue> GetEnumerator() { throw null; }
        //manual
        public virtual System.Collections.IEnumerator GetNonGenericEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public enum PrintQueueIndexedProperty
    {
        AveragePagesPerMinute = 9,
        Comment = 2,
        DefaultPrintTicket = 19,
        DefaultPriority = 6,
        Description = 4,
        HostingPrintServer = 15,
        Location = 3,
        Name = 0,
        NumberOfJobs = 10,
        Priority = 5,
        QueueAttributes = 11,
        QueueDriver = 12,
        QueuePort = 13,
        QueuePrintProcessor = 14,
        QueueStatus = 16,
        SeparatorFile = 17,
        ShareName = 1,
        StartTimeOfDay = 7,
        UntilTimeOfDay = 8,
        UserPrintTicket = 18,
    }
    [System.FlagsAttribute]
    public enum PrintQueueStatus
    {
        Busy = 512,
        DoorOpen = 4194304,
        Error = 2,
        Initializing = 32768,
        IOActive = 256,
        ManualFeed = 32,
        None = 0,
        NotAvailable = 4096,
        NoToner = 262144,
        Offline = 128,
        OutOfMemory = 2097152,
        OutputBinFull = 2048,
        PagePunt = 524288,
        PaperJam = 8,
        PaperOut = 16,
        PaperProblem = 64,
        Paused = 1,
        PendingDeletion = 4,
        PowerSave = 16777216,
        Printing = 1024,
        Processing = 16384,
        ServerUnknown = 8388608,
        TonerLow = 131072,
        UserIntervention = 1048576,
        Waiting = 8192,
        WarmingUp = 65536,
    }
    public partial class PrintQueueStream : System.IO.Stream
    {
        public PrintQueueStream(System.Printing.PrintQueue printQueue, string printJobName) { }
        public PrintQueueStream(System.Printing.PrintQueue printQueue, string printJobName, bool commitDataOnClose) { }
        public PrintQueueStream(System.Printing.PrintQueue printQueue, string printJobName, bool commitDataOnClose, System.Printing.PrintTicket printTicket) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public int JobIdentifier { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override void Close() { }
        protected override void Dispose(bool A_0) { }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        ~PrintQueueStream() { }
        public override void Flush() { }
        public void HandlePackagingProgressEvent(object sender, System.Windows.Xps.Packaging.PackagingProgressEventArgs e) { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
    public partial class PrintQueueStringProperty
    {
        public PrintQueueStringProperty() { }
        public string Name { get { throw null; } set { } }
        public System.Printing.PrintQueueStringPropertyType Type { get { throw null; } set { } }
    }
    public enum PrintQueueStringPropertyType
    {
        Comment = 1,
        Location = 0,
        ShareName = 2,
    }
    public partial class PrintServer : System.Printing.PrintSystemObject
    {
        public PrintServer() { }
        public PrintServer(System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintServer(string path) { }
        public PrintServer(string path, System.Printing.PrintServerIndexedProperty[] propertiesFilter) { }
        public PrintServer(string path, System.Printing.PrintServerIndexedProperty[] propertiesFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintServer(string path, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public PrintServer(string path, string[] propertiesFilter) { }
        public PrintServer(string path, string[] propertiesFilter, System.Printing.PrintSystemDesiredAccess desiredAccess) { }
        public bool BeepEnabled { get { throw null; } set { } }
        public System.Threading.ThreadPriority DefaultPortThreadPriority { get { throw null; } }
        public System.Threading.ThreadPriority DefaultSchedulerPriority { get { throw null; } }
        public string DefaultSpoolDirectory { get { throw null; } set { } }
        public System.Printing.PrintServerEventLoggingTypes EventLog { get { throw null; } set { } }
        protected bool IsDelayInitialized { get { throw null; } set { } }
        public int MajorVersion { get { throw null; } }
        public int MinorVersion { get { throw null; } }
        public sealed override string Name { get { throw null; } }
        public bool NetPopup { get { throw null; } set { } }
        public System.Threading.ThreadPriority PortThreadPriority { get { throw null; } set { } }
        public bool RestartJobOnPoolEnabled { get { throw null; } set { } }
        public int RestartJobOnPoolTimeout { get { throw null; } set { } }
        public System.Threading.ThreadPriority SchedulerPriority { get { throw null; } set { } }
        public byte SubSystemVersion { get { throw null; } }
        public override void Commit() { }
        public static bool DeletePrintQueue(System.Printing.PrintQueue printQueue) { throw null; }
        public static bool DeletePrintQueue(string printQueueName) { throw null; }
        public System.Printing.PrintQueue GetPrintQueue(string printQueueName) { throw null; }
        public System.Printing.PrintQueue GetPrintQueue(string printQueueName, string[] propertiesFilter) { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues() { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues(System.Printing.EnumeratedPrintQueueTypes[] enumerationFlag) { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues(System.Printing.PrintQueueIndexedProperty[] propertiesFilter) { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues(System.Printing.PrintQueueIndexedProperty[] propertiesFilter, System.Printing.EnumeratedPrintQueueTypes[] enumerationFlag) { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues(string[] propertiesFilter) { throw null; }
        public System.Printing.PrintQueueCollection GetPrintQueues(string[] propertiesFilter, System.Printing.EnumeratedPrintQueueTypes[] enumerationFlag) { throw null; }
        public System.Printing.PrintQueue InstallPrintQueue(string printQueueName, string driverName, string[] portNames, string printProcessorName, System.Printing.IndexedProperties.PrintPropertyDictionary initialParameters) { throw null; }
        public System.Printing.PrintQueue InstallPrintQueue(string printQueueName, string driverName, string[] portNames, string printProcessorName, System.Printing.PrintQueueAttributes printQueueAttributes) { throw null; }
        public System.Printing.PrintQueue InstallPrintQueue(string printQueueName, string driverName, string[] portNames, string printProcessorName, System.Printing.PrintQueueAttributes printQueueAttributes, System.Printing.PrintQueueStringProperty printQueueProperty, int printQueuePriority, int printQueueDefaultPriority) { throw null; }
        public System.Printing.PrintQueue InstallPrintQueue(string printQueueName, string driverName, string[] portNames, string printProcessorName, System.Printing.PrintQueueAttributes printQueueAttributes, string printQueueShareName, string printQueueComment, string printQueueLocation, string printQueueSeparatorFile, int printQueuePriority, int printQueueDefaultPriority) { throw null; }
        protected sealed override void InternalDispose(bool disposing) { }
        public override void Refresh() { }
    }
    [System.FlagsAttribute]
    public enum PrintServerEventLoggingTypes
    {
        LogAllPrintingEvents = 5,
        LogPrintingErrorEvents = 2,
        LogPrintingInformationEvents = 4,
        LogPrintingSuccessEvents = 1,
        LogPrintingWarningEvents = 3,
        None = 0,
    }
    public enum PrintServerIndexedProperty
    {
        BeepEnabled = 5,
        DefaultPortThreadPriority = 2,
        DefaultSchedulerPriority = 4,
        DefaultSpoolDirectory = 0,
        EventLog = 7,
        MajorVersion = 8,
        MinorVersion = 9,
        NetPopup = 6,
        PortThreadPriority = 1,
        RestartJobOnPoolEnabled = 11,
        RestartJobOnPoolTimeout = 10,
        SchedulerPriority = 3,
    }
    public enum PrintSystemDesiredAccess
    {
        AdministratePrinter = 983052,
        AdministrateServer = 983041,
        EnumerateServer = 131074,
        None = 0,
        UsePrinter = 131080,
    }
    public partial class PrintSystemJobInfo : System.Printing.PrintSystemObject
    {
        internal PrintSystemJobInfo() { }
        public System.Printing.PrintQueue HostingPrintQueue { get { throw null; } }
        public System.Printing.PrintServer HostingPrintServer { get { throw null; } }
        public bool IsBlocked { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsDeleted { get { throw null; } }
        public bool IsDeleting { get { throw null; } }
        public bool IsInError { get { throw null; } }
        public bool IsOffline { get { throw null; } }
        public bool IsPaperOut { get { throw null; } }
        public bool IsPaused { get { throw null; } }
        public bool IsPrinted { get { throw null; } }
        public bool IsPrinting { get { throw null; } }
        public bool IsRestarted { get { throw null; } }
        public bool IsRetained { get { throw null; } }
        public bool IsSpooling { get { throw null; } }
        public bool IsUserInterventionRequired { get { throw null; } }
        public int JobIdentifier { get { throw null; } }
        public string JobName { get { throw null; } set { } }
        public int JobSize { get { throw null; } }
        public System.Printing.PrintJobStatus JobStatus { get { throw null; } }
        public System.IO.Stream JobStream { get { throw null; } }
        public int NumberOfPages { get { throw null; } }
        public int NumberOfPagesPrinted { get { throw null; } }
        public int PositionInPrintQueue { get { throw null; } }
        public System.Printing.PrintJobPriority Priority { get { throw null; } }
        public int StartTimeOfDay { get { throw null; } }
        public string Submitter { get { throw null; } }
        public System.DateTime TimeJobSubmitted { get { throw null; } }
        public int TimeSinceStartedPrinting { get { throw null; } }
        public int UntilTimeOfDay { get { throw null; } }
        public void Cancel() { }
        public override void Commit() { }
        public static System.Printing.PrintSystemJobInfo Get(System.Printing.PrintQueue printQueue, int jobIdentifier) { throw null; }
        protected sealed override void InternalDispose(bool disposing) { }
        public void Pause() { }
        public override void Refresh() { }
        public void Restart() { }
        public void Resume() { }
    }
    public abstract partial class PrintSystemObject : System.IDisposable
    {
        protected PrintSystemObject() { }
        protected PrintSystemObject(System.Printing.PrintSystemObjectLoadMode mode) { }
        protected bool IsDisposed { get { throw null; } set { } }
        public virtual string Name { get { throw null; } internal set { } }
        public virtual System.Printing.PrintSystemObject Parent { get { throw null; } }
        public System.Printing.IndexedProperties.PrintPropertyDictionary PropertiesCollection { get { throw null; } }
        protected static string[] BaseAttributeNames() { throw null; }
        public abstract void Commit();
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
        ~PrintSystemObject() { }
        protected void Initialize() { }
        protected virtual void InternalDispose(bool disposing) { }
        public abstract void Refresh();
    }
    public enum PrintSystemObjectLoadMode
    {
        LoadInitialized = 2,
        LoadUninitialized = 1,
        None = 0,
    }
    public partial class PrintSystemObjectPropertiesChangedEventArgs : System.EventArgs, System.IDisposable
    {
        public PrintSystemObjectPropertiesChangedEventArgs(System.Collections.Specialized.StringCollection events) { }
        public System.Collections.Specialized.StringCollection PropertiesNames { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
    }
    public partial class PrintSystemObjectPropertyChangedEventArgs : System.EventArgs, System.IDisposable
    {
        public PrintSystemObjectPropertyChangedEventArgs(string eventName) { }
        public string PropertyName { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
    }
    public abstract partial class PrintSystemObjects : System.IDisposable
    {
        protected PrintSystemObjects() { }
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
    }
}
namespace System.Printing.IndexedProperties
{
    public sealed partial class PrintBooleanProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintBooleanProperty(string attributeName) : base (default(string)) { }
        public PrintBooleanProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator bool (System.Printing.IndexedProperties.PrintBooleanProperty attribRef) { throw null; }
    }
    public sealed partial class PrintByteArrayProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintByteArrayProperty(string attributeName) : base (default(string)) { }
        public PrintByteArrayProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator byte[] (System.Printing.IndexedProperties.PrintByteArrayProperty attribRef) { throw null; }
    }
    public sealed partial class PrintDateTimeProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintDateTimeProperty(string attributeName) : base (default(string)) { }
        public PrintDateTimeProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.ValueType (System.Printing.IndexedProperties.PrintDateTimeProperty attribRef) { throw null; }
    }
    public sealed partial class PrintDriverProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintDriverProperty(string attributeName) : base (default(string)) { }
        public PrintDriverProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintDriver (System.Printing.IndexedProperties.PrintDriverProperty attribRef) { throw null; }
    }
    public sealed partial class PrintInt32Property : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintInt32Property(string attributeName) : base (default(string)) { }
        public PrintInt32Property(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator int (System.Printing.IndexedProperties.PrintInt32Property attribRef) { throw null; }
    }
    public sealed partial class PrintJobPriorityProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintJobPriorityProperty(string attributeName) : base (default(string)) { }
        public PrintJobPriorityProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintJobPriority (System.Printing.IndexedProperties.PrintJobPriorityProperty attribRef) { throw null; }
    }
    public sealed partial class PrintJobStatusProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintJobStatusProperty(string attributeName) : base (default(string)) { }
        public PrintJobStatusProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintJobStatus (System.Printing.IndexedProperties.PrintJobStatusProperty attribRef) { throw null; }
    }
    public sealed partial class PrintPortProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintPortProperty(string attributeName) : base (default(string)) { }
        public PrintPortProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintPort (System.Printing.IndexedProperties.PrintPortProperty attribRef) { throw null; }
    }
    public sealed partial class PrintProcessorProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintProcessorProperty(string attributeName) : base (default(string)) { }
        public PrintProcessorProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintProcessor (System.Printing.IndexedProperties.PrintProcessorProperty attribRef) { throw null; }
    }
    public abstract partial class PrintProperty : System.IDisposable, System.Runtime.Serialization.IDeserializationCallback
    {
        protected PrintProperty(string attributeName) { }
        protected bool IsDisposed { get { throw null; } set { } }
        protected internal bool IsInitialized { get { throw null; } protected set { } }
        public virtual string Name { get { throw null; } }
        public abstract object Value { get; set; }
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
        ~PrintProperty() { }
        protected virtual void InternalDispose(bool disposing) { }
        public virtual void OnDeserialization(object sender) { }
    }
    [DefaultMember("Property")]
    public partial class PrintPropertyDictionary : System.Collections.Hashtable, System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public PrintPropertyDictionary() { }
        protected PrintPropertyDictionary(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Add(System.Printing.IndexedProperties.PrintProperty attributeValue) { }
        public void Dispose() { }
        protected virtual void Dispose(bool A_0) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Printing.IndexedProperties.PrintProperty GetProperty(string attribName) { throw null; }
        public override void OnDeserialization(object sender) { }
        public void SetProperty(string attribName, System.Printing.IndexedProperties.PrintProperty attribValue) { }
    }
    public sealed partial class PrintQueueAttributeProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintQueueAttributeProperty(string attributeName) : base (default(string)) { }
        public PrintQueueAttributeProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintQueueAttributes (System.Printing.IndexedProperties.PrintQueueAttributeProperty attributeRef) { throw null; }
    }
    public sealed partial class PrintQueueProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintQueueProperty(string attributeName) : base (default(string)) { }
        public PrintQueueProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintQueue (System.Printing.IndexedProperties.PrintQueueProperty attribRef) { throw null; }
    }
    public sealed partial class PrintQueueStatusProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintQueueStatusProperty(string attributeName) : base (default(string)) { }
        public PrintQueueStatusProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintQueueStatus (System.Printing.IndexedProperties.PrintQueueStatusProperty attributeRef) { throw null; }
    }
    public sealed partial class PrintServerLoggingProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintServerLoggingProperty(string attributeName) : base (default(string)) { }
        public PrintServerLoggingProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintServerEventLoggingTypes (System.Printing.IndexedProperties.PrintServerLoggingProperty attribRef) { throw null; }
    }
    public sealed partial class PrintServerProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintServerProperty(string attributeName) : base (default(string)) { }
        public PrintServerProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintServer (System.Printing.IndexedProperties.PrintServerProperty attribRef) { throw null; }
    }
    public sealed partial class PrintStreamProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintStreamProperty(string attributeName) : base (default(string)) { }
        public PrintStreamProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.IO.Stream (System.Printing.IndexedProperties.PrintStreamProperty attributeRef) { throw null; }
    }
    public sealed partial class PrintStringProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintStringProperty(string attributeName) : base (default(string)) { }
        public PrintStringProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator string (System.Printing.IndexedProperties.PrintStringProperty attributeRef) { throw null; }
    }
    public sealed partial class PrintSystemTypeProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintSystemTypeProperty(string attributeName) : base (default(string)) { }
        public PrintSystemTypeProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Type (System.Printing.IndexedProperties.PrintSystemTypeProperty attribRef) { throw null; }
    }
    public sealed partial class PrintThreadPriorityProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintThreadPriorityProperty(string attributeName) : base (default(string)) { }
        public PrintThreadPriorityProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Threading.ThreadPriority (System.Printing.IndexedProperties.PrintThreadPriorityProperty attribRef) { throw null; }
    }
    public sealed partial class PrintTicketProperty : System.Printing.IndexedProperties.PrintProperty
    {
        public PrintTicketProperty(string attributeName) : base (default(string)) { }
        public PrintTicketProperty(string attributeName, object attributeValue) : base (default(string)) { }
        public override object Value { get { throw null; } set { } }
        protected sealed override void InternalDispose(bool disposing) { }
        public static implicit operator System.Printing.PrintTicket (System.Printing.IndexedProperties.PrintTicketProperty attribRef) { throw null; }
    }
}
namespace System.Windows.Xps
{
    public partial class VisualsToXpsDocument : System.Windows.Documents.Serialization.SerializerWriterCollator
    {
        internal VisualsToXpsDocument() { }
        public override void BeginBatchWrite() { }
        public override void Cancel() { }
        public override void CancelAsync() { }
        public override void EndBatchWrite() { }
        public override void Write(System.Windows.Media.Visual visual) { }
        public override void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Media.Visual visual) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
    }
    public enum XpsDocumentNotificationLevel
    {
        None = 0,
        ReceiveNotificationDisabled = 2,
        ReceiveNotificationEnabled = 1,
    }
    public partial class XpsDocumentWriter : System.Windows.Documents.Serialization.SerializerWriter
    {
        internal XpsDocumentWriter() { }
        public override event System.Windows.Documents.Serialization.WritingCancelledEventHandler WritingCancelled { add { } remove { } }
        public override event System.Windows.Documents.Serialization.WritingCompletedEventHandler WritingCompleted { add { } remove { } }
        public override event System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventHandler WritingPrintTicketRequired { add { } remove { } }
        public override event System.Windows.Documents.Serialization.WritingProgressChangedEventHandler WritingProgressChanged { add { } remove { } }
        public override void CancelAsync() { }
        public override System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator() { throw null; }
        public override System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator(System.Printing.PrintTicket documentSequencePrintTicket, System.Printing.PrintTicket documentPrintTicket) { throw null; }
        public virtual void raise_WritingCancelled(object sender, System.Windows.Documents.Serialization.WritingCancelledEventArgs args) { }
        public virtual void raise_WritingCompleted(object sender, System.Windows.Documents.Serialization.WritingCompletedEventArgs e) { }
        public virtual void raise_WritingPrintTicketRequired(object sender, System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventArgs e) { }
        public virtual void raise_WritingProgressChanged(object sender, System.Windows.Documents.Serialization.WritingProgressChangedEventArgs e) { }
        public void Write(string documentPath) { }
        public void Write(string documentPath, System.Windows.Xps.XpsDocumentNotificationLevel notificationLevel) { }
        public override void Write(System.Windows.Documents.DocumentPaginator documentPaginator) { }
        public override void Write(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket) { }
        public override void Write(System.Windows.Documents.FixedDocument fixedDocument) { }
        public override void Write(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket) { }
        public override void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence) { }
        public override void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket) { }
        public override void Write(System.Windows.Documents.FixedPage fixedPage) { }
        public override void Write(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket) { }
        public override void Write(System.Windows.Media.Visual visual) { }
        public override void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket) { }
        public void WriteAsync(string documentPath) { }
        public void WriteAsync(string documentPath, System.Windows.Xps.XpsDocumentNotificationLevel notificationLevel) { }
        public override void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator) { }
        public override void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedPage fixedPage) { }
        public override void WriteAsync(System.Windows.Documents.FixedPage fixedPage, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Media.Visual visual) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, object userSuppliedState) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket) { }
        public override void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userSuppliedState) { }
    }
    public partial class XpsWriterException : System.Exception
    {
        public XpsWriterException() { }
        protected XpsWriterException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XpsWriterException(string message) { }
        public XpsWriterException(string message, System.Exception innerException) { }
    }
}
