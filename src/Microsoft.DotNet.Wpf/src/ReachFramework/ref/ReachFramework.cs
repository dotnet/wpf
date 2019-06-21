namespace System.Printing
{
    public enum Collation
    {
        Unknown = 0,
        Collated = 1,
        Uncollated = 2,
    }
    public enum ConflictStatus
    {
        NoConflict = 0,
        ConflictResolved = 1,
    }
    public enum DeviceFontSubstitution
    {
        Unknown = 0,
        Off = 1,
        On = 2,
    }
    public enum Duplexing
    {
        Unknown = 0,
        OneSided = 1,
        TwoSidedShortEdge = 2,
        TwoSidedLongEdge = 3,
    }
    public enum InputBin
    {
        Unknown = 0,
        AutoSelect = 1,
        Cassette = 2,
        Tractor = 3,
        AutoSheetFeeder = 4,
        Manual = 5,
    }
    public enum OutputColor
    {
        Unknown = 0,
        Color = 1,
        Grayscale = 2,
        Monochrome = 3,
    }
    public enum OutputQuality
    {
        Unknown = 0,
        Automatic = 1,
        Draft = 2,
        Fax = 3,
        High = 4,
        Normal = 5,
        Photographic = 6,
        Text = 7,
    }
    public enum PageBorderless
    {
        Unknown = 0,
        Borderless = 1,
        None = 2,
    }
    public sealed partial class PageImageableArea
    {
        internal PageImageableArea() { }
        public double ExtentHeight { get { throw null; } }
        public double ExtentWidth { get { throw null; } }
        public double OriginHeight { get { throw null; } }
        public double OriginWidth { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public sealed partial class PageMediaSize
    {
        public PageMediaSize(double width, double height) { }
        public PageMediaSize(System.Printing.PageMediaSizeName mediaSizeName) { }
        public PageMediaSize(System.Printing.PageMediaSizeName mediaSizeName, double width, double height) { }
        public double? Height { get { throw null; } }
        public System.Printing.PageMediaSizeName? PageMediaSizeName { get { throw null; } }
        public double? Width { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public enum PageMediaSizeName
    {
        Unknown = 0,
        ISOA0 = 1,
        ISOA1 = 2,
        ISOA10 = 3,
        ISOA2 = 4,
        ISOA3 = 5,
        ISOA3Rotated = 6,
        ISOA3Extra = 7,
        ISOA4 = 8,
        ISOA4Rotated = 9,
        ISOA4Extra = 10,
        ISOA5 = 11,
        ISOA5Rotated = 12,
        ISOA5Extra = 13,
        ISOA6 = 14,
        ISOA6Rotated = 15,
        ISOA7 = 16,
        ISOA8 = 17,
        ISOA9 = 18,
        ISOB0 = 19,
        ISOB1 = 20,
        ISOB10 = 21,
        ISOB2 = 22,
        ISOB3 = 23,
        ISOB4 = 24,
        ISOB4Envelope = 25,
        ISOB5Envelope = 26,
        ISOB5Extra = 27,
        ISOB7 = 28,
        ISOB8 = 29,
        ISOB9 = 30,
        ISOC0 = 31,
        ISOC1 = 32,
        ISOC10 = 33,
        ISOC2 = 34,
        ISOC3 = 35,
        ISOC3Envelope = 36,
        ISOC4 = 37,
        ISOC4Envelope = 38,
        ISOC5 = 39,
        ISOC5Envelope = 40,
        ISOC6 = 41,
        ISOC6Envelope = 42,
        ISOC6C5Envelope = 43,
        ISOC7 = 44,
        ISOC8 = 45,
        ISOC9 = 46,
        ISODLEnvelope = 47,
        ISODLEnvelopeRotated = 48,
        ISOSRA3 = 49,
        JapanQuadrupleHagakiPostcard = 50,
        JISB0 = 51,
        JISB1 = 52,
        JISB10 = 53,
        JISB2 = 54,
        JISB3 = 55,
        JISB4 = 56,
        JISB4Rotated = 57,
        JISB5 = 58,
        JISB5Rotated = 59,
        JISB6 = 60,
        JISB6Rotated = 61,
        JISB7 = 62,
        JISB8 = 63,
        JISB9 = 64,
        JapanChou3Envelope = 65,
        JapanChou3EnvelopeRotated = 66,
        JapanChou4Envelope = 67,
        JapanChou4EnvelopeRotated = 68,
        JapanHagakiPostcard = 69,
        JapanHagakiPostcardRotated = 70,
        JapanKaku2Envelope = 71,
        JapanKaku2EnvelopeRotated = 72,
        JapanKaku3Envelope = 73,
        JapanKaku3EnvelopeRotated = 74,
        JapanYou4Envelope = 75,
        NorthAmerica10x11 = 76,
        NorthAmerica10x14 = 77,
        NorthAmerica11x17 = 78,
        NorthAmerica9x11 = 79,
        NorthAmericaArchitectureASheet = 80,
        NorthAmericaArchitectureBSheet = 81,
        NorthAmericaArchitectureCSheet = 82,
        NorthAmericaArchitectureDSheet = 83,
        NorthAmericaArchitectureESheet = 84,
        NorthAmericaCSheet = 85,
        NorthAmericaDSheet = 86,
        NorthAmericaESheet = 87,
        NorthAmericaExecutive = 88,
        NorthAmericaGermanLegalFanfold = 89,
        NorthAmericaGermanStandardFanfold = 90,
        NorthAmericaLegal = 91,
        NorthAmericaLegalExtra = 92,
        NorthAmericaLetter = 93,
        NorthAmericaLetterRotated = 94,
        NorthAmericaLetterExtra = 95,
        NorthAmericaLetterPlus = 96,
        NorthAmericaMonarchEnvelope = 97,
        NorthAmericaNote = 98,
        NorthAmericaNumber10Envelope = 99,
        NorthAmericaNumber10EnvelopeRotated = 100,
        NorthAmericaNumber9Envelope = 101,
        NorthAmericaNumber11Envelope = 102,
        NorthAmericaNumber12Envelope = 103,
        NorthAmericaNumber14Envelope = 104,
        NorthAmericaPersonalEnvelope = 105,
        NorthAmericaQuarto = 106,
        NorthAmericaStatement = 107,
        NorthAmericaSuperA = 108,
        NorthAmericaSuperB = 109,
        NorthAmericaTabloid = 110,
        NorthAmericaTabloidExtra = 111,
        OtherMetricA4Plus = 112,
        OtherMetricA3Plus = 113,
        OtherMetricFolio = 114,
        OtherMetricInviteEnvelope = 115,
        OtherMetricItalianEnvelope = 116,
        PRC1Envelope = 117,
        PRC1EnvelopeRotated = 118,
        PRC10Envelope = 119,
        PRC10EnvelopeRotated = 120,
        PRC16K = 121,
        PRC16KRotated = 122,
        PRC2Envelope = 123,
        PRC2EnvelopeRotated = 124,
        PRC32K = 125,
        PRC32KRotated = 126,
        PRC32KBig = 127,
        PRC3Envelope = 128,
        PRC3EnvelopeRotated = 129,
        PRC4Envelope = 130,
        PRC4EnvelopeRotated = 131,
        PRC5Envelope = 132,
        PRC5EnvelopeRotated = 133,
        PRC6Envelope = 134,
        PRC6EnvelopeRotated = 135,
        PRC7Envelope = 136,
        PRC7EnvelopeRotated = 137,
        PRC8Envelope = 138,
        PRC8EnvelopeRotated = 139,
        PRC9Envelope = 140,
        PRC9EnvelopeRotated = 141,
        Roll04Inch = 142,
        Roll06Inch = 143,
        Roll08Inch = 144,
        Roll12Inch = 145,
        Roll15Inch = 146,
        Roll18Inch = 147,
        Roll22Inch = 148,
        Roll24Inch = 149,
        Roll30Inch = 150,
        Roll36Inch = 151,
        Roll54Inch = 152,
        JapanDoubleHagakiPostcard = 153,
        JapanDoubleHagakiPostcardRotated = 154,
        JapanLPhoto = 155,
        Japan2LPhoto = 156,
        JapanYou1Envelope = 157,
        JapanYou2Envelope = 158,
        JapanYou3Envelope = 159,
        JapanYou4EnvelopeRotated = 160,
        JapanYou6Envelope = 161,
        JapanYou6EnvelopeRotated = 162,
        NorthAmerica4x6 = 163,
        NorthAmerica4x8 = 164,
        NorthAmerica5x7 = 165,
        NorthAmerica8x10 = 166,
        NorthAmerica10x12 = 167,
        NorthAmerica14x17 = 168,
        BusinessCard = 169,
        CreditCard = 170,
    }
    public enum PageMediaType
    {
        Unknown = 0,
        AutoSelect = 1,
        Archival = 2,
        BackPrintFilm = 3,
        Bond = 4,
        CardStock = 5,
        Continuous = 6,
        EnvelopePlain = 7,
        EnvelopeWindow = 8,
        Fabric = 9,
        HighResolution = 10,
        Label = 11,
        MultiLayerForm = 12,
        MultiPartForm = 13,
        Photographic = 14,
        PhotographicFilm = 15,
        PhotographicGlossy = 16,
        PhotographicHighGloss = 17,
        PhotographicMatte = 18,
        PhotographicSatin = 19,
        PhotographicSemiGloss = 20,
        Plain = 21,
        Screen = 22,
        ScreenPaged = 23,
        Stationery = 24,
        TabStockFull = 25,
        TabStockPreCut = 26,
        Transparency = 27,
        TShirtTransfer = 28,
        None = 29,
    }
    public enum PageOrder
    {
        Unknown = 0,
        Standard = 1,
        Reverse = 2,
    }
    public enum PageOrientation
    {
        Unknown = 0,
        Landscape = 1,
        Portrait = 2,
        ReverseLandscape = 3,
        ReversePortrait = 4,
    }
    public enum PageQualitativeResolution
    {
        Unknown = 0,
        Default = 1,
        Draft = 2,
        High = 3,
        Normal = 4,
        Other = 5,
    }
    public sealed partial class PageResolution
    {
        public PageResolution(int resolutionX, int resolutionY) { }
        public PageResolution(int resolutionX, int resolutionY, System.Printing.PageQualitativeResolution qualitative) { }
        public PageResolution(System.Printing.PageQualitativeResolution qualitative) { }
        public System.Printing.PageQualitativeResolution? QualitativeResolution { get { throw null; } }
        public int? X { get { throw null; } }
        public int? Y { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public sealed partial class PageScalingFactorRange
    {
        internal PageScalingFactorRange() { }
        public int MaximumScale { get { throw null; } }
        public int MinimumScale { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public enum PagesPerSheetDirection
    {
        Unknown = 0,
        RightBottom = 1,
        BottomRight = 2,
        LeftBottom = 3,
        BottomLeft = 4,
        RightTop = 5,
        TopRight = 6,
        LeftTop = 7,
        TopLeft = 8,
    }
    public enum PhotoPrintingIntent
    {
        Unknown = 0,
        None = 1,
        PhotoBest = 2,
        PhotoDraft = 3,
        PhotoStandard = 4,
    }
    public sealed partial class PrintCapabilities
    {
        public PrintCapabilities(System.IO.Stream xmlStream) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.Collation> CollationCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.DeviceFontSubstitution> DeviceFontSubstitutionCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.Duplexing> DuplexingCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.InputBin> InputBinCapability { get { throw null; } }
        public int? MaxCopyCount { get { throw null; } }
        public double? OrientedPageMediaHeight { get { throw null; } }
        public double? OrientedPageMediaWidth { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.OutputColor> OutputColorCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.OutputQuality> OutputQualityCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageBorderless> PageBorderlessCapability { get { throw null; } }
        public System.Printing.PageImageableArea PageImageableArea { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageMediaSize> PageMediaSizeCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageMediaType> PageMediaTypeCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageOrder> PageOrderCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageOrientation> PageOrientationCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PageResolution> PageResolutionCapability { get { throw null; } }
        public System.Printing.PageScalingFactorRange PageScalingFactorRange { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<int> PagesPerSheetCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PagesPerSheetDirection> PagesPerSheetDirectionCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.PhotoPrintingIntent> PhotoPrintingIntentCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.Stapling> StaplingCapability { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Printing.TrueTypeFontMode> TrueTypeFontModeCapability { get { throw null; } }
    }
    public partial class PrintCommitAttributesException : System.Printing.PrintSystemException
    {
        public PrintCommitAttributesException() { }
        public PrintCommitAttributesException(int errorCode, System.Collections.ObjectModel.Collection<string> attributesSuccessList, System.Collections.ObjectModel.Collection<string> attributesFailList) { }
        public PrintCommitAttributesException(int errorCode, string message, System.Collections.ObjectModel.Collection<string> attributesSuccessList, System.Collections.ObjectModel.Collection<string> attributesFailList, string objectName) { }
        protected PrintCommitAttributesException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintCommitAttributesException(string message) { }
        public PrintCommitAttributesException(string message, System.Exception innerException) { }
        public System.Collections.ObjectModel.Collection<string> CommittedAttributesCollection { get { throw null; } }
        public System.Collections.ObjectModel.Collection<string> FailedAttributesCollection { get { throw null; } }
        public string PrintObjectName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrintingCanceledException : System.Printing.PrintJobException
    {
        public PrintingCanceledException() { }
        public PrintingCanceledException(int errorCode, string message) { }
        public PrintingCanceledException(int errorCode, string message, System.Exception innerException) { }
        public PrintingCanceledException(int errorCode, string message, string printQueueName, string jobName, int jobId) { }
        public PrintingCanceledException(int errorCode, string message, string printQueueName, string jobName, int jobId, System.Exception innerException) { }
        protected PrintingCanceledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintingCanceledException(string message) { }
        public PrintingCanceledException(string message, System.Exception innerException) { }
    }
    public partial class PrintingNotSupportedException : System.Printing.PrintSystemException
    {
        public PrintingNotSupportedException() { }
        protected PrintingNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintingNotSupportedException(string message) { }
        public PrintingNotSupportedException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrintJobException : System.Printing.PrintSystemException
    {
        public PrintJobException() { }
        public PrintJobException(int errorCode, string message) { }
        public PrintJobException(int errorCode, string message, System.Exception innerException) { }
        public PrintJobException(int errorCode, string message, string printQueueName, string jobName, int jobId) { }
        public PrintJobException(int errorCode, string message, string printQueueName, string jobName, int jobId, System.Exception innerException) { }
        protected PrintJobException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintJobException(string message) { }
        public PrintJobException(string message, System.Exception innerException) { }
        public int JobId { get { throw null; } }
        public string JobName { get { throw null; } }
        public string PrintQueueName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrintQueueException : System.Printing.PrintSystemException
    {
        public PrintQueueException() { }
        public PrintQueueException(int errorCode, string message, string printerName) { }
        public PrintQueueException(int errorCode, string message, string printerName, System.Exception innerException) { }
        public PrintQueueException(int errorCode, string message, string printerName, string printerMessage) { }
        protected PrintQueueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintQueueException(string message) { }
        public PrintQueueException(string message, System.Exception innerException) { }
        public string PrinterName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrintServerException : System.Printing.PrintSystemException
    {
        public PrintServerException() { }
        public PrintServerException(int errorCode, string message, string serverName) { }
        public PrintServerException(int errorCode, string message, string serverName, System.Exception innerException) { }
        protected PrintServerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintServerException(string message) { }
        public PrintServerException(string message, System.Exception innerException) { }
        public string ServerName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrintSystemException : System.SystemException
    {
        public PrintSystemException() { }
        public PrintSystemException(int errorCode, string message) { }
        public PrintSystemException(int errorCode, string message, System.Exception innerException) { }
        public PrintSystemException(int errorCode, string message, string printerMessage) { }
        protected PrintSystemException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrintSystemException(string message) { }
        public PrintSystemException(string message, System.Exception innerException) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class PrintTicket : System.ComponentModel.INotifyPropertyChanged
    {
        public PrintTicket() { }
        public PrintTicket(System.IO.Stream xmlStream) { }
        public System.Printing.Collation? Collation { get { throw null; } set { } }
        public int? CopyCount { get { throw null; } set { } }
        public System.Printing.DeviceFontSubstitution? DeviceFontSubstitution { get { throw null; } set { } }
        public System.Printing.Duplexing? Duplexing { get { throw null; } set { } }
        public System.Printing.InputBin? InputBin { get { throw null; } set { } }
        public System.Printing.OutputColor? OutputColor { get { throw null; } set { } }
        public System.Printing.OutputQuality? OutputQuality { get { throw null; } set { } }
        public System.Printing.PageBorderless? PageBorderless { get { throw null; } set { } }
        public System.Printing.PageMediaSize PageMediaSize { get { throw null; } set { } }
        public System.Printing.PageMediaType? PageMediaType { get { throw null; } set { } }
        public System.Printing.PageOrder? PageOrder { get { throw null; } set { } }
        public System.Printing.PageOrientation? PageOrientation { get { throw null; } set { } }
        public System.Printing.PageResolution PageResolution { get { throw null; } set { } }
        public int? PageScalingFactor { get { throw null; } set { } }
        public int? PagesPerSheet { get { throw null; } set { } }
        public System.Printing.PagesPerSheetDirection? PagesPerSheetDirection { get { throw null; } set { } }
        public System.Printing.PhotoPrintingIntent? PhotoPrintingIntent { get { throw null; } set { } }
        public System.Printing.Stapling? Stapling { get { throw null; } set { } }
        public System.Printing.TrueTypeFontMode? TrueTypeFontMode { get { throw null; } set { } }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        public System.Printing.PrintTicket Clone() { throw null; }
        public System.IO.MemoryStream GetXmlStream() { throw null; }
        public void SaveTo(System.IO.Stream outStream) { }
    }
    public enum PrintTicketScope
    {
        PageScope = 0,
        DocumentScope = 1,
        JobScope = 2,
    }
    public enum Stapling
    {
        Unknown = 0,
        SaddleStitch = 1,
        StapleBottomLeft = 2,
        StapleBottomRight = 3,
        StapleDualLeft = 4,
        StapleDualRight = 5,
        StapleDualTop = 6,
        StapleDualBottom = 7,
        StapleTopLeft = 8,
        StapleTopRight = 9,
        None = 10,
    }
    public enum TrueTypeFontMode
    {
        Unknown = 0,
        Automatic = 1,
        DownloadAsOutlineFont = 2,
        DownloadAsRasterFont = 3,
        DownloadAsNativeTrueTypeFont = 4,
        RenderAsBitmap = 5,
    }
    public partial struct ValidationResult
    {
        public System.Printing.ConflictStatus ConflictStatus { get { throw null; } }
        public System.Printing.PrintTicket ValidatedPrintTicket { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Printing.ValidationResult a, System.Printing.ValidationResult b) { throw null; }
        public static bool operator !=(System.Printing.ValidationResult a, System.Printing.ValidationResult b) { throw null; }
    }
}
namespace System.Printing.Interop
{
    public enum BaseDevModeType
    {
        UserDefault = 0,
        PrinterDefault = 1,
    }
    public sealed partial class PrintTicketConverter : System.IDisposable
    {
        public PrintTicketConverter(string deviceName, int clientPrintSchemaVersion) { }
        public static int MaxPrintSchemaVersion { get { throw null; } }
        public System.Printing.PrintTicket ConvertDevModeToPrintTicket(byte[] devMode) { throw null; }
        public System.Printing.PrintTicket ConvertDevModeToPrintTicket(byte[] devMode, System.Printing.PrintTicketScope scope) { throw null; }
        public byte[] ConvertPrintTicketToDevMode(System.Printing.PrintTicket printTicket, System.Printing.Interop.BaseDevModeType baseType) { throw null; }
        public byte[] ConvertPrintTicketToDevMode(System.Printing.PrintTicket printTicket, System.Printing.Interop.BaseDevModeType baseType, System.Printing.PrintTicketScope scope) { throw null; }
        public void Dispose() { }
        void System.IDisposable.Dispose() { }
    }
}
namespace System.Windows.Xps
{
    public partial class XpsException : System.Exception
    {
        public XpsException() { }
        protected XpsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XpsException(string message) { }
        public XpsException(string message, System.Exception innerException) { }
    }
    public partial class XpsPackagingException : System.Windows.Xps.XpsException
    {
        public XpsPackagingException() { }
        protected XpsPackagingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XpsPackagingException(string message) { }
        public XpsPackagingException(string message, System.Exception innerException) { }
    }
    public partial class XpsSerializationException : System.Windows.Xps.XpsException
    {
        public XpsSerializationException() { }
        protected XpsSerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XpsSerializationException(string message) { }
        public XpsSerializationException(string message, System.Exception innerException) { }
    }
}
namespace System.Windows.Xps.Packaging
{
    public partial interface IDocumentStructureProvider
    {
        System.Windows.Xps.Packaging.XpsStructure AddDocumentStructure();
    }
    public partial interface IStoryFragmentProvider
    {
        System.Windows.Xps.Packaging.XpsStructure AddStoryFragment();
    }
    public partial interface IXpsFixedDocumentReader : System.Windows.Xps.Packaging.IDocumentStructureProvider
    {
        int DocumentNumber { get; }
        System.Windows.Xps.Packaging.XpsStructure DocumentStructure { get; }
        System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Xps.Packaging.IXpsFixedPageReader> FixedPages { get; }
        System.Printing.PrintTicket PrintTicket { get; }
        System.Collections.Generic.ICollection<System.Windows.Xps.Packaging.XpsSignatureDefinition> SignatureDefinitions { get; }
        System.Windows.Xps.Packaging.XpsThumbnail Thumbnail { get; }
        System.Uri Uri { get; }
        void AddSignatureDefinition(System.Windows.Xps.Packaging.XpsSignatureDefinition signatureDefinition);
        void CommitSignatureDefinition();
        System.Windows.Xps.Packaging.IXpsFixedPageReader GetFixedPage(System.Uri pageSource);
        void RemoveSignatureDefinition(System.Windows.Xps.Packaging.XpsSignatureDefinition signatureDefinition);
    }
    public partial interface IXpsFixedDocumentSequenceReader
    {
        System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Xps.Packaging.IXpsFixedDocumentReader> FixedDocuments { get; }
        System.Printing.PrintTicket PrintTicket { get; }
        System.Windows.Xps.Packaging.XpsThumbnail Thumbnail { get; }
        System.Uri Uri { get; }
        System.Windows.Xps.Packaging.IXpsFixedDocumentReader GetFixedDocument(System.Uri documentSource);
    }
    public partial interface IXpsFixedDocumentSequenceWriter
    {
        System.Printing.PrintTicket PrintTicket { set; }
        System.Uri Uri { get; }
        System.Windows.Xps.Packaging.IXpsFixedDocumentWriter AddFixedDocument();
        System.Windows.Xps.Packaging.XpsThumbnail AddThumbnail(System.Windows.Xps.Packaging.XpsImageType imageType);
        void Commit();
    }
    public partial interface IXpsFixedDocumentWriter : System.Windows.Xps.Packaging.IDocumentStructureProvider
    {
        int DocumentNumber { get; }
        System.Printing.PrintTicket PrintTicket { set; }
        System.Uri Uri { get; }
        System.Windows.Xps.Packaging.IXpsFixedPageWriter AddFixedPage();
        System.Windows.Xps.Packaging.XpsThumbnail AddThumbnail(System.Windows.Xps.Packaging.XpsImageType imageType);
        void Commit();
    }
    public partial interface IXpsFixedPageReader : System.Windows.Xps.Packaging.IStoryFragmentProvider
    {
        System.Collections.Generic.ICollection<System.Windows.Xps.Packaging.XpsColorContext> ColorContexts { get; }
        System.Collections.Generic.ICollection<System.Windows.Xps.Packaging.XpsFont> Fonts { get; }
        System.Collections.Generic.ICollection<System.Windows.Xps.Packaging.XpsImage> Images { get; }
        int PageNumber { get; }
        System.Printing.PrintTicket PrintTicket { get; }
        System.Collections.Generic.ICollection<System.Windows.Xps.Packaging.XpsResourceDictionary> ResourceDictionaries { get; }
        System.Windows.Xps.Packaging.XpsStructure StoryFragment { get; }
        System.Windows.Xps.Packaging.XpsThumbnail Thumbnail { get; }
        System.Uri Uri { get; }
        System.Xml.XmlReader XmlReader { get; }
        System.Windows.Xps.Packaging.XpsColorContext GetColorContext(System.Uri uri);
        System.Windows.Xps.Packaging.XpsFont GetFont(System.Uri uri);
        System.Windows.Xps.Packaging.XpsImage GetImage(System.Uri uri);
        System.Windows.Xps.Packaging.XpsResource GetResource(System.Uri resourceUri);
        System.Windows.Xps.Packaging.XpsResourceDictionary GetResourceDictionary(System.Uri uri);
    }
    public partial interface IXpsFixedPageWriter : System.Windows.Xps.Packaging.IStoryFragmentProvider
    {
        System.Collections.Generic.IList<string> LinkTargetStream { get; }
        int PageNumber { get; }
        System.Printing.PrintTicket PrintTicket { set; }
        System.Uri Uri { get; }
        System.Xml.XmlWriter XmlWriter { get; }
        System.Windows.Xps.Packaging.XpsColorContext AddColorContext();
        System.Windows.Xps.Packaging.XpsFont AddFont();
        System.Windows.Xps.Packaging.XpsFont AddFont(bool obfuscate);
        System.Windows.Xps.Packaging.XpsFont AddFont(bool obfuscate, bool addRestrictedRelationship);
        System.Windows.Xps.Packaging.XpsImage AddImage(string mimeType);
        System.Windows.Xps.Packaging.XpsImage AddImage(System.Windows.Xps.Packaging.XpsImageType imageType);
        System.Windows.Xps.Packaging.XpsResource AddResource(System.Type resourceType, System.Uri resourceUri);
        System.Windows.Xps.Packaging.XpsResourceDictionary AddResourceDictionary();
        System.Windows.Xps.Packaging.XpsThumbnail AddThumbnail(System.Windows.Xps.Packaging.XpsImageType imageType);
        void Commit();
    }
    public enum PackageInterleavingOrder
    {
        None = 0,
        ResourceFirst = 1,
        ResourceLast = 2,
        ImagesLast = 3,
    }
    public enum PackagingAction
    {
        None = 0,
        AddingDocumentSequence = 1,
        DocumentSequenceCompleted = 2,
        AddingFixedDocument = 3,
        FixedDocumentCompleted = 4,
        AddingFixedPage = 5,
        FixedPageCompleted = 6,
        ResourceAdded = 7,
        FontAdded = 8,
        ImageAdded = 9,
        XpsDocumentCommitted = 10,
    }
    public partial class PackagingProgressEventArgs : System.EventArgs
    {
        public PackagingProgressEventArgs(System.Windows.Xps.Packaging.PackagingAction action, int numberCompleted) { }
        public System.Windows.Xps.Packaging.PackagingAction Action { get { throw null; } }
        public int NumberCompleted { get { throw null; } }
    }
    public delegate void PackagingProgressEventHandler(object sender, System.Windows.Xps.Packaging.PackagingProgressEventArgs e);
    public partial class SpotLocation
    {
        public SpotLocation() { }
        public System.Uri PageUri { get { throw null; } set { } }
        public double StartX { get { throw null; } set { } }
        public double StartY { get { throw null; } set { } }
    }
    public partial class XpsColorContext : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsColorContext() { }
    }
    public partial class XpsDigitalSignature
    {
        public XpsDigitalSignature(System.IO.Packaging.PackageDigitalSignature packageSignature, System.Windows.Xps.Packaging.XpsDocument package) { }
        public bool DocumentPropertiesRestricted { get { throw null; } }
        public System.Guid? Id { get { throw null; } }
        public bool IsCertificateAvailable { get { throw null; } }
        public bool SignatureOriginRestricted { get { throw null; } }
        public string SignatureType { get { throw null; } }
        public byte[] SignatureValue { get { throw null; } }
        public System.Windows.Xps.Packaging.IXpsFixedDocumentSequenceReader SignedDocumentSequence { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate SignerCertificate { get { throw null; } }
        public System.DateTime SigningTime { get { throw null; } }
        public System.IO.Packaging.VerifyResult Verify() { throw null; }
        public System.IO.Packaging.VerifyResult Verify(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509ChainStatusFlags VerifyCertificate() { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509ChainStatusFlags VerifyCertificate(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw null; }
    }
    [System.FlagsAttribute]
    public enum XpsDigSigPartAlteringRestrictions
    {
        None = 0,
        CoreMetadata = 1,
        Annotations = 2,
        SignatureOrigin = 4,
    }
    public partial class XpsDocument : System.Windows.Xps.Packaging.XpsPartBase, System.IDisposable
    {
        public XpsDocument(System.IO.Packaging.Package package) { }
        public XpsDocument(System.IO.Packaging.Package package, System.IO.Packaging.CompressionOption compressionOption) { }
        public XpsDocument(System.IO.Packaging.Package package, System.IO.Packaging.CompressionOption compressionOption, string path) { }
        public XpsDocument(string path, System.IO.FileAccess packageAccess) { }
        public XpsDocument(string path, System.IO.FileAccess packageAccess, System.IO.Packaging.CompressionOption compressionOption) { }
        public System.IO.Packaging.PackageProperties CoreDocumentProperties { get { throw null; } }
        public System.Windows.Xps.Packaging.IXpsFixedDocumentSequenceReader FixedDocumentSequenceReader { get { throw null; } }
        public bool IsReader { get { throw null; } }
        public bool IsSignable { get { throw null; } }
        public bool IsWriter { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Xps.Packaging.XpsDigitalSignature> Signatures { get { throw null; } }
        public System.Windows.Xps.Packaging.XpsThumbnail Thumbnail { get { throw null; } set { } }
        public System.Windows.Xps.Packaging.IXpsFixedDocumentSequenceWriter AddFixedDocumentSequence() { throw null; }
        public System.Windows.Xps.Packaging.XpsThumbnail AddThumbnail(System.Windows.Xps.Packaging.XpsImageType imageType) { throw null; }
        public void Close() { }
        public static System.Windows.Xps.XpsDocumentWriter CreateXpsDocumentWriter(System.Windows.Xps.Packaging.XpsDocument xpsDocument) { throw null; }
        protected virtual void Dispose(bool disposing) { }
        public System.Windows.Documents.FixedDocumentSequence GetFixedDocumentSequence() { throw null; }
        public void RemoveSignature(System.Windows.Xps.Packaging.XpsDigitalSignature signature) { }
        public System.Windows.Xps.Packaging.XpsDigitalSignature SignDigitally(System.Security.Cryptography.X509Certificates.X509Certificate certificate, bool embedCertificate, System.Windows.Xps.Packaging.XpsDigSigPartAlteringRestrictions restrictions) { throw null; }
        public System.Windows.Xps.Packaging.XpsDigitalSignature SignDigitally(System.Security.Cryptography.X509Certificates.X509Certificate certificate, bool embedCertificate, System.Windows.Xps.Packaging.XpsDigSigPartAlteringRestrictions restrictions, System.Guid id) { throw null; }
        public System.Windows.Xps.Packaging.XpsDigitalSignature SignDigitally(System.Security.Cryptography.X509Certificates.X509Certificate certificate, bool embedCertificate, System.Windows.Xps.Packaging.XpsDigSigPartAlteringRestrictions restrictions, System.Guid id, bool testIsSignable) { throw null; }
        void System.IDisposable.Dispose() { }
    }
    public partial class XpsFont : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsFont() { }
        public bool IsObfuscated { get { throw null; } }
        public bool IsRestricted { get { throw null; } set { } }
        public static void ObfuscateFontData(byte[] fontData, System.Guid guid) { }
    }
    public partial class XpsImage : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsImage() { }
    }
    public enum XpsImageType
    {
        PngImageType = 0,
        JpegImageType = 1,
        TiffImageType = 2,
        WdpImageType = 3,
    }
    public abstract partial class XpsPartBase
    {
        internal XpsPartBase() { }
        public System.Uri Uri { get { throw null; } set { } }
    }
    public partial class XpsResource : System.Windows.Xps.Packaging.XpsPartBase, System.IDisposable
    {
        internal XpsResource() { }
        public void Commit() { }
        public virtual System.IO.Stream GetStream() { throw null; }
        public System.Uri RelativeUri(System.Uri inUri) { throw null; }
        void System.IDisposable.Dispose() { }
    }
    public partial class XpsResourceDictionary : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsResourceDictionary() { }
    }
    public enum XpsResourceSharing
    {
        ShareResources = 0,
        NoResourceSharing = 1,
    }
    public partial class XpsSignatureDefinition
    {
        public XpsSignatureDefinition() { }
        public System.Globalization.CultureInfo Culture { get { throw null; } set { } }
        public bool HasBeenModified { get { throw null; } set { } }
        public string Intent { get { throw null; } set { } }
        public string RequestedSigner { get { throw null; } set { } }
        public System.DateTime? SignBy { get { throw null; } set { } }
        public string SigningLocale { get { throw null; } set { } }
        public System.Guid? SpotId { get { throw null; } set { } }
        public System.Windows.Xps.Packaging.SpotLocation SpotLocation { get { throw null; } set { } }
    }
    public partial class XpsStructure : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsStructure() { }
    }
    public partial class XpsThumbnail : System.Windows.Xps.Packaging.XpsResource
    {
        internal XpsThumbnail() { }
    }
}
namespace System.Windows.Xps.Serialization
{
    public abstract partial class BasePackagingPolicy : System.IDisposable
    {
        protected BasePackagingPolicy() { }
        public abstract System.Uri CurrentFixedDocumentUri { get; }
        public abstract System.Uri CurrentFixedPageUri { get; }
        public abstract System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsColorContext(string resourceId);
        public abstract System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsFont();
        public abstract System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsFont(string resourceId);
        public abstract System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsImage(string resourceId);
        public abstract System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsResourceDictionary(string resourceId);
        public abstract System.Collections.Generic.IList<string> AcquireStreamForLinkTargets();
        public abstract System.Xml.XmlWriter AcquireXmlWriterForFixedDocument();
        public abstract System.Xml.XmlWriter AcquireXmlWriterForFixedDocumentSequence();
        public abstract System.Xml.XmlWriter AcquireXmlWriterForFixedPage();
        public abstract System.Xml.XmlWriter AcquireXmlWriterForPage();
        public abstract System.Xml.XmlWriter AcquireXmlWriterForResourceDictionary();
        public abstract void PersistPrintTicket(System.Printing.PrintTicket printTicket);
        public abstract void PreCommitCurrentPage();
        public abstract void RelateResourceToCurrentPage(System.Uri targetUri, string relationshipName);
        public abstract void RelateRestrictedFontToCurrentDocument(System.Uri targetUri);
        public abstract void ReleaseResourceStreamForXpsColorContext();
        public abstract void ReleaseResourceStreamForXpsFont();
        public abstract void ReleaseResourceStreamForXpsFont(string resourceId);
        public abstract void ReleaseResourceStreamForXpsImage();
        public abstract void ReleaseResourceStreamForXpsResourceDictionary();
        public abstract void ReleaseXmlWriterForFixedDocument();
        public abstract void ReleaseXmlWriterForFixedDocumentSequence();
        public abstract void ReleaseXmlWriterForFixedPage();
        void System.IDisposable.Dispose() { }
    }
    public partial class ColorTypeConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public ColorTypeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { throw null; }
        public static string SerializeColorContext(System.IServiceProvider context, System.Windows.Media.ColorContext colorContext) { throw null; }
    }
    [System.FlagsAttribute]
    public enum FontSubsetterCommitPolicies
    {
        None = 0,
        CommitPerPage = 1,
        CommitPerDocument = 2,
        CommitEntireSequence = 3,
    }
    public partial class FontTypeConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public FontTypeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { throw null; }
    }
    public partial class ImageSourceTypeConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public ImageSourceTypeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        [System.Security.SecurityTreatAsSafeAttribute]
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { throw null; }
    }
    public abstract partial class PackageSerializationManager : System.IDisposable
    {
        protected PackageSerializationManager() { }
        public abstract void SaveAsXaml(object serializedObject);
        void System.IDisposable.Dispose() { }
    }
    public enum PrintTicketLevel
    {
        None = 0,
        FixedDocumentSequencePrintTicket = 1,
        FixedDocumentPrintTicket = 2,
        FixedPagePrintTicket = 3,
    }
    public enum SerializationState
    {
        Normal = 0,
        Stop = 1,
    }
    public partial class XpsPackagingPolicy : System.Windows.Xps.Serialization.BasePackagingPolicy
    {
        public XpsPackagingPolicy(System.Windows.Xps.Packaging.XpsDocument xpsPackage) { }
        public XpsPackagingPolicy(System.Windows.Xps.Packaging.XpsDocument xpsPackage, System.Windows.Xps.Packaging.PackageInterleavingOrder interleavingType) { }
        public override System.Uri CurrentFixedDocumentUri { get { throw null; } }
        public override System.Uri CurrentFixedPageUri { get { throw null; } }
        public event System.Windows.Xps.Packaging.PackagingProgressEventHandler PackagingProgressEvent { add { } remove { } }
        public override System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsColorContext(string resourceId) { throw null; }
        public override System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsFont() { throw null; }
        public override System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsFont(string resourceId) { throw null; }
        public override System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsImage(string resourceId) { throw null; }
        public override System.Windows.Xps.Serialization.XpsResourceStream AcquireResourceStreamForXpsResourceDictionary(string resourceId) { throw null; }
        public override System.Collections.Generic.IList<string> AcquireStreamForLinkTargets() { throw null; }
        public override System.Xml.XmlWriter AcquireXmlWriterForFixedDocument() { throw null; }
        public override System.Xml.XmlWriter AcquireXmlWriterForFixedDocumentSequence() { throw null; }
        public override System.Xml.XmlWriter AcquireXmlWriterForFixedPage() { throw null; }
        public override System.Xml.XmlWriter AcquireXmlWriterForPage() { throw null; }
        public override System.Xml.XmlWriter AcquireXmlWriterForResourceDictionary() { throw null; }
        public override void PersistPrintTicket(System.Printing.PrintTicket printTicket) { }
        public override void PreCommitCurrentPage() { }
        public override void RelateResourceToCurrentPage(System.Uri targetUri, string relationshipName) { }
        public override void RelateRestrictedFontToCurrentDocument(System.Uri targetUri) { }
        public override void ReleaseResourceStreamForXpsColorContext() { }
        public override void ReleaseResourceStreamForXpsFont() { }
        public override void ReleaseResourceStreamForXpsFont(string resourceId) { }
        public override void ReleaseResourceStreamForXpsImage() { }
        public override void ReleaseResourceStreamForXpsResourceDictionary() { }
        public override void ReleaseXmlWriterForFixedDocument() { }
        public override void ReleaseXmlWriterForFixedDocumentSequence() { }
        public override void ReleaseXmlWriterForFixedPage() { }
    }
    public partial class XpsResourceStream
    {
        public XpsResourceStream(System.IO.Stream stream, System.Uri uri) { }
        public System.IO.Stream Stream { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        public void Initialize() { }
    }
    public partial class XpsSerializationCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        public XpsSerializationCompletedEventArgs(bool canceled, object state, System.Exception exception) : base (default(System.Exception), default(bool), default(object)) { }
    }
    public delegate void XpsSerializationCompletedEventHandler(object sender, System.Windows.Xps.Serialization.XpsSerializationCompletedEventArgs e);
    public partial class XpsSerializationManager : System.Windows.Xps.Serialization.PackageSerializationManager
    {
        public XpsSerializationManager(System.Windows.Xps.Serialization.BasePackagingPolicy packagingPolicy, bool batchMode) { }
        public bool IsBatchMode { get { throw null; } }
        public event System.Windows.Xps.Serialization.XpsSerializationPrintTicketRequiredEventHandler XpsSerializationPrintTicketRequired { add { } remove { } }
        public event System.Windows.Xps.Serialization.XpsSerializationProgressChangedEventHandler XpsSerializationProgressChanged { add { } remove { } }
        public virtual void Commit() { }
        public override void SaveAsXaml(object serializedObject) { }
        public void SetFontSubsettingCountPolicy(int countPolicy) { }
        public void SetFontSubsettingPolicy(System.Windows.Xps.Serialization.FontSubsetterCommitPolicies policy) { }
    }
    public sealed partial class XpsSerializationManagerAsync : System.Windows.Xps.Serialization.XpsSerializationManager
    {
        public XpsSerializationManagerAsync(System.Windows.Xps.Serialization.BasePackagingPolicy packagingPolicy, bool batchMode) : base (default(System.Windows.Xps.Serialization.BasePackagingPolicy), default(bool)) { }
        public event System.Windows.Xps.Serialization.XpsSerializationCompletedEventHandler XpsSerializationCompleted { add { } remove { } }
        public void CancelAsync() { }
        public override void Commit() { }
        public override void SaveAsXaml(object serializedObject) { }
    }
    public partial class XpsSerializationPrintTicketRequiredEventArgs : System.EventArgs
    {
        public XpsSerializationPrintTicketRequiredEventArgs(System.Windows.Xps.Serialization.PrintTicketLevel printTicketLevel, int sequence) { }
        public System.Printing.PrintTicket PrintTicket { get { throw null; } set { } }
        public System.Windows.Xps.Serialization.PrintTicketLevel PrintTicketLevel { get { throw null; } }
        public int Sequence { get { throw null; } }
    }
    public delegate void XpsSerializationPrintTicketRequiredEventHandler(object sender, System.Windows.Xps.Serialization.XpsSerializationPrintTicketRequiredEventArgs e);
    public partial class XpsSerializationProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        public XpsSerializationProgressChangedEventArgs(System.Windows.Xps.Serialization.XpsWritingProgressChangeLevel writingLevel, int pageNumber, int progressPercentage, object userToken) : base (default(int), default(object)) { }
        public int PageNumber { get { throw null; } }
        public System.Windows.Xps.Serialization.XpsWritingProgressChangeLevel WritingLevel { get { throw null; } }
    }
    public delegate void XpsSerializationProgressChangedEventHandler(object sender, System.Windows.Xps.Serialization.XpsSerializationProgressChangedEventArgs e);
    public sealed partial class XpsSerializerFactory : System.Windows.Documents.Serialization.ISerializerFactory
    {
        public XpsSerializerFactory() { }
        public string DefaultFileExtension { get { throw null; } }
        public string DisplayName { get { throw null; } }
        public string ManufacturerName { get { throw null; } }
        public System.Uri ManufacturerWebsite { get { throw null; } }
        public System.Windows.Documents.Serialization.SerializerWriter CreateSerializerWriter(System.IO.Stream stream) { throw null; }
    }
    public enum XpsWritingProgressChangeLevel
    {
        None = 0,
        FixedDocumentSequenceWritingProgress = 1,
        FixedDocumentWritingProgress = 2,
        FixedPageWritingProgress = 3,
    }
}
