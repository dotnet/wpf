namespace System.Collections.Specialized
{
    public partial class CollectionChangedEventManager : System.Windows.WeakEventManager
    {
        internal CollectionChangedEventManager() { }
        public static void AddHandler(System.Collections.Specialized.INotifyCollectionChanged source, System.EventHandler<System.Collections.Specialized.NotifyCollectionChangedEventArgs> handler) { }
        public static void AddListener(System.Collections.Specialized.INotifyCollectionChanged source, System.Windows.IWeakEventListener listener) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.Collections.Specialized.INotifyCollectionChanged source, System.EventHandler<System.Collections.Specialized.NotifyCollectionChangedEventArgs> handler) { }
        public static void RemoveListener(System.Collections.Specialized.INotifyCollectionChanged source, System.Windows.IWeakEventListener listener) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
}
namespace System.ComponentModel
{
    public partial class CurrentChangedEventManager : System.Windows.WeakEventManager
    {
        internal CurrentChangedEventManager() { }
        public static void AddHandler(System.ComponentModel.ICollectionView source, System.EventHandler<System.EventArgs> handler) { }
        public static void AddListener(System.ComponentModel.ICollectionView source, System.Windows.IWeakEventListener listener) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.ComponentModel.ICollectionView source, System.EventHandler<System.EventArgs> handler) { }
        public static void RemoveListener(System.ComponentModel.ICollectionView source, System.Windows.IWeakEventListener listener) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    public partial class CurrentChangingEventArgs : System.EventArgs
    {
        public CurrentChangingEventArgs() { }
        public CurrentChangingEventArgs(bool isCancelable) { }
        public bool Cancel { get { throw null; } set { } }
        public bool IsCancelable { get { throw null; } }
    }
    public delegate void CurrentChangingEventHandler(object sender, System.ComponentModel.CurrentChangingEventArgs e);
    public partial class CurrentChangingEventManager : System.Windows.WeakEventManager
    {
        internal CurrentChangingEventManager() { }
        public static void AddHandler(System.ComponentModel.ICollectionView source, System.EventHandler<System.ComponentModel.CurrentChangingEventArgs> handler) { }
        public static void AddListener(System.ComponentModel.ICollectionView source, System.Windows.IWeakEventListener listener) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.ComponentModel.ICollectionView source, System.EventHandler<System.ComponentModel.CurrentChangingEventArgs> handler) { }
        public static void RemoveListener(System.ComponentModel.ICollectionView source, System.Windows.IWeakEventListener listener) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    public sealed partial class DependencyPropertyDescriptor : System.ComponentModel.PropertyDescriptor
    {
        internal DependencyPropertyDescriptor() : base (default(System.ComponentModel.MemberDescriptor)) { }
        public override System.ComponentModel.AttributeCollection Attributes { get { throw null; } }
        public override string Category { get { throw null; } }
        public override System.Type ComponentType { get { throw null; } }
        public override System.ComponentModel.TypeConverter Converter { get { throw null; } }
        public System.Windows.DependencyProperty DependencyProperty { get { throw null; } }
        public override string Description { get { throw null; } }
        public System.Windows.CoerceValueCallback DesignerCoerceValueCallback { get { throw null; } set { } }
        public override bool DesignTimeOnly { get { throw null; } }
        public override string DisplayName { get { throw null; } }
        public bool IsAttached { get { throw null; } }
        public override bool IsBrowsable { get { throw null; } }
        public override bool IsLocalizable { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public System.Windows.PropertyMetadata Metadata { get { throw null; } }
        public override System.Type PropertyType { get { throw null; } }
        public override bool SupportsChangeEvents { get { throw null; } }
        public override void AddValueChanged(object component, System.EventHandler handler) { }
        public override bool CanResetValue(object component) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public static System.ComponentModel.DependencyPropertyDescriptor FromName(string name, System.Type ownerType, System.Type targetType) { throw null; }
        public static System.ComponentModel.DependencyPropertyDescriptor FromName(string name, System.Type ownerType, System.Type targetType, bool ignorePropertyType) { throw null; }
        public static System.ComponentModel.DependencyPropertyDescriptor FromProperty(System.ComponentModel.PropertyDescriptor property) { throw null; }
        public static System.ComponentModel.DependencyPropertyDescriptor FromProperty(System.Windows.DependencyProperty dependencyProperty, System.Type targetType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetChildProperties(object instance, System.Attribute[] filter) { throw null; }
        public override object GetEditor(System.Type editorBaseType) { throw null; }
        public override int GetHashCode() { throw null; }
        public override object GetValue(object component) { throw null; }
        public override void RemoveValueChanged(object component, System.EventHandler handler) { }
        public override void ResetValue(object component) { }
        public override void SetValue(object component, object value) { }
        public override bool ShouldSerializeValue(object component) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class ErrorsChangedEventManager : System.Windows.WeakEventManager
    {
        internal ErrorsChangedEventManager() { }
        public static void AddHandler(System.ComponentModel.INotifyDataErrorInfo source, System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs> handler) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(System.ComponentModel.INotifyDataErrorInfo source, System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs> handler) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    public abstract partial class GroupDescription : System.ComponentModel.INotifyPropertyChanged
    {
        protected GroupDescription() { }
        public System.Collections.IComparer CustomSort { get { throw null; } set { } }
        public System.Collections.ObjectModel.ObservableCollection<object> GroupNames { get { throw null; } }
        public System.ComponentModel.SortDescriptionCollection SortDescriptions { get { throw null; } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        public abstract object GroupNameFromItem(object item, int level, System.Globalization.CultureInfo culture);
        public virtual bool NamesMatch(object groupName, object itemName) { throw null; }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeGroupNames() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeSortDescriptions() { throw null; }
    }
    public partial interface ICollectionView : System.Collections.IEnumerable, System.Collections.Specialized.INotifyCollectionChanged
    {
        bool CanFilter { get; }
        bool CanGroup { get; }
        bool CanSort { get; }
        System.Globalization.CultureInfo Culture { get; set; }
        object CurrentItem { get; }
        int CurrentPosition { get; }
        System.Predicate<object> Filter { get; set; }
        System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get; }
        System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get; }
        bool IsCurrentAfterLast { get; }
        bool IsCurrentBeforeFirst { get; }
        bool IsEmpty { get; }
        System.ComponentModel.SortDescriptionCollection SortDescriptions { get; }
        System.Collections.IEnumerable SourceCollection { get; }
        event System.EventHandler CurrentChanged;
        event System.ComponentModel.CurrentChangingEventHandler CurrentChanging;
        bool Contains(object item);
        System.IDisposable DeferRefresh();
        bool MoveCurrentTo(object item);
        bool MoveCurrentToFirst();
        bool MoveCurrentToLast();
        bool MoveCurrentToNext();
        bool MoveCurrentToPosition(int position);
        bool MoveCurrentToPrevious();
        void Refresh();
    }
    public partial interface ICollectionViewFactory
    {
        System.ComponentModel.ICollectionView CreateView();
    }
    public partial interface ICollectionViewLiveShaping
    {
        bool CanChangeLiveFiltering { get; }
        bool CanChangeLiveGrouping { get; }
        bool CanChangeLiveSorting { get; }
        bool? IsLiveFiltering { get; set; }
        bool? IsLiveGrouping { get; set; }
        bool? IsLiveSorting { get; set; }
        System.Collections.ObjectModel.ObservableCollection<string> LiveFilteringProperties { get; }
        System.Collections.ObjectModel.ObservableCollection<string> LiveGroupingProperties { get; }
        System.Collections.ObjectModel.ObservableCollection<string> LiveSortingProperties { get; }
    }
    public partial interface IEditableCollectionView
    {
        bool CanAddNew { get; }
        bool CanCancelEdit { get; }
        bool CanRemove { get; }
        object CurrentAddItem { get; }
        object CurrentEditItem { get; }
        bool IsAddingNew { get; }
        bool IsEditingItem { get; }
        System.ComponentModel.NewItemPlaceholderPosition NewItemPlaceholderPosition { get; set; }
        object AddNew();
        void CancelEdit();
        void CancelNew();
        void CommitEdit();
        void CommitNew();
        void EditItem(object item);
        void Remove(object item);
        void RemoveAt(int index);
    }
    public partial interface IEditableCollectionViewAddNewItem : System.ComponentModel.IEditableCollectionView
    {
        bool CanAddNewItem { get; }
        object AddNewItem(object newItem);
    }
    public partial interface IItemProperties
    {
        System.Collections.ObjectModel.ReadOnlyCollection<System.ComponentModel.ItemPropertyInfo> ItemProperties { get; }
    }
    public partial class ItemPropertyInfo
    {
        public ItemPropertyInfo(string name, System.Type type, object descriptor) { }
        public object Descriptor { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Type PropertyType { get { throw null; } }
    }
    public enum NewItemPlaceholderPosition
    {
        None = 0,
        AtBeginning = 1,
        AtEnd = 2,
    }
    public partial class PropertyChangedEventManager : System.Windows.WeakEventManager
    {
        internal PropertyChangedEventManager() { }
        public static void AddHandler(System.ComponentModel.INotifyPropertyChanged source, System.EventHandler<System.ComponentModel.PropertyChangedEventArgs> handler, string propertyName) { }
        public static void AddListener(System.ComponentModel.INotifyPropertyChanged source, System.Windows.IWeakEventListener listener, string propertyName) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        protected override bool Purge(object source, object data, bool purgeAll) { throw null; }
        public static void RemoveHandler(System.ComponentModel.INotifyPropertyChanged source, System.EventHandler<System.ComponentModel.PropertyChangedEventArgs> handler, string propertyName) { }
        public static void RemoveListener(System.ComponentModel.INotifyPropertyChanged source, System.Windows.IWeakEventListener listener, string propertyName) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method | System.AttributeTargets.Property)]
    public sealed partial class PropertyFilterAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.PropertyFilterAttribute Default;
        public PropertyFilterAttribute(System.ComponentModel.PropertyFilterOptions filter) { }
        public System.ComponentModel.PropertyFilterOptions Filter { get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool Match(object value) { throw null; }
    }
    [System.FlagsAttribute]
    public enum PropertyFilterOptions
    {
        None = 0,
        Invalid = 1,
        SetValues = 2,
        UnsetValues = 4,
        Valid = 8,
        All = 15,
    }
    public partial struct SortDescription
    {
        public SortDescription(string propertyName, System.ComponentModel.ListSortDirection direction) { throw null; }
        public System.ComponentModel.ListSortDirection Direction { get { throw null; } set { } }
        public bool IsSealed { get { throw null; } }
        public string PropertyName { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ComponentModel.SortDescription sd1, System.ComponentModel.SortDescription sd2) { throw null; }
        public static bool operator !=(System.ComponentModel.SortDescription sd1, System.ComponentModel.SortDescription sd2) { throw null; }
    }
    public partial class SortDescriptionCollection : System.Collections.ObjectModel.Collection<System.ComponentModel.SortDescription>, System.Collections.Specialized.INotifyCollectionChanged
    {
        public static readonly System.ComponentModel.SortDescriptionCollection Empty;
        public SortDescriptionCollection() { }
        protected event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }
        event System.Collections.Specialized.NotifyCollectionChangedEventHandler System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged { add { } remove { } }
        protected override void ClearItems() { }
        protected override void InsertItem(int index, System.ComponentModel.SortDescription item) { }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.ComponentModel.SortDescription item) { }
    }
}
namespace System.Diagnostics
{
    public enum PresentationTraceLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }
    public static partial class PresentationTraceSources
    {
        public static readonly System.Windows.DependencyProperty TraceLevelProperty;
        public static System.Diagnostics.TraceSource AnimationSource { get { throw null; } }
        public static System.Diagnostics.TraceSource DataBindingSource { get { throw null; } }
        public static System.Diagnostics.TraceSource DependencyPropertySource { get { throw null; } }
        public static System.Diagnostics.TraceSource DocumentsSource { get { throw null; } }
        public static System.Diagnostics.TraceSource FreezableSource { get { throw null; } }
        public static System.Diagnostics.TraceSource HwndHostSource { get { throw null; } }
        public static System.Diagnostics.TraceSource MarkupSource { get { throw null; } }
        public static System.Diagnostics.TraceSource NameScopeSource { get { throw null; } }
        public static System.Diagnostics.TraceSource ResourceDictionarySource { get { throw null; } }
        public static System.Diagnostics.TraceSource RoutedEventSource { get { throw null; } }
        public static System.Diagnostics.TraceSource ShellSource { get { throw null; } }
        public static System.Diagnostics.PresentationTraceLevel GetTraceLevel(object element) { throw null; }
        public static void Refresh() { }
        public static void SetTraceLevel(object element, System.Diagnostics.PresentationTraceLevel traceLevel) { }
    }
}
namespace System.IO.Packaging
{
    public enum CertificateEmbeddingOption
    {
        InCertificatePart = 0,
        InSignaturePart = 1,
        NotEmbedded = 2,
    }
    public partial class EncryptedPackageEnvelope : System.IDisposable
    {
        internal EncryptedPackageEnvelope() { }
        public System.IO.FileAccess FileOpenAccess { get { throw null; } }
        public System.IO.Packaging.PackageProperties PackageProperties { get { throw null; } }
        public System.IO.Packaging.RightsManagementInformation RightsManagementInformation { get { throw null; } }
        public System.IO.Packaging.StorageInfo StorageInfo { get { throw null; } }
        public void Close() { }
        public static System.IO.Packaging.EncryptedPackageEnvelope Create(System.IO.Stream envelopeStream, System.Security.RightsManagement.PublishLicense publishLicense, System.Security.RightsManagement.CryptoProvider cryptoProvider) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope Create(string envelopeFileName, System.Security.RightsManagement.PublishLicense publishLicense, System.Security.RightsManagement.CryptoProvider cryptoProvider) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope CreateFromPackage(System.IO.Stream envelopeStream, System.IO.Stream packageStream, System.Security.RightsManagement.PublishLicense publishLicense, System.Security.RightsManagement.CryptoProvider cryptoProvider) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope CreateFromPackage(string envelopeFileName, System.IO.Stream packageStream, System.Security.RightsManagement.PublishLicense publishLicense, System.Security.RightsManagement.CryptoProvider cryptoProvider) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Flush() { }
        public System.IO.Packaging.Package GetPackage() { throw null; }
        public static bool IsEncryptedPackageEnvelope(System.IO.Stream stream) { throw null; }
        public static bool IsEncryptedPackageEnvelope(string fileName) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope Open(System.IO.Stream envelopeStream) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope Open(string envelopeFileName) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope Open(string envelopeFileName, System.IO.FileAccess access) { throw null; }
        public static System.IO.Packaging.EncryptedPackageEnvelope Open(string envelopeFileName, System.IO.FileAccess access, System.IO.FileShare sharing) { throw null; }
    }
    public delegate void InvalidSignatureEventHandler(object sender, System.IO.Packaging.SignatureVerificationEventArgs e);
    public partial class PackageDigitalSignature
    {
        internal PackageDigitalSignature() { }
        public System.IO.Packaging.CertificateEmbeddingOption CertificateEmbeddingOption { get { throw null; } }
        public System.Security.Cryptography.Xml.Signature Signature { get { throw null; } set { } }
        public System.IO.Packaging.PackagePart SignaturePart { get { throw null; } }
        public string SignatureType { get { throw null; } }
        public byte[] SignatureValue { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Uri> SignedParts { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Packaging.PackageRelationshipSelector> SignedRelationshipSelectors { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate Signer { get { throw null; } }
        public System.DateTime SigningTime { get { throw null; } }
        public string TimeFormat { get { throw null; } }
        public System.Collections.Generic.List<string> GetPartTransformList(System.Uri partName) { throw null; }
        public System.IO.Packaging.VerifyResult Verify() { throw null; }
        public System.IO.Packaging.VerifyResult Verify(System.Security.Cryptography.X509Certificates.X509Certificate signingCertificate) { throw null; }
    }
    public sealed partial class PackageDigitalSignatureManager
    {
        public PackageDigitalSignatureManager(System.IO.Packaging.Package package) { }
        public System.IO.Packaging.CertificateEmbeddingOption CertificateOption { get { throw null; } set { } }
        public static string DefaultHashAlgorithm { get { throw null; } }
        public string HashAlgorithm { get { throw null; } set { } }
        public bool IsSigned { get { throw null; } }
        public System.IntPtr ParentWindow { get { throw null; } set { } }
        public System.Uri SignatureOrigin { get { throw null; } }
        public static string SignatureOriginRelationshipType { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.Packaging.PackageDigitalSignature> Signatures { get { throw null; } }
        public string TimeFormat { get { throw null; } set { } }
        public System.Collections.Generic.Dictionary<string, string> TransformMapping { get { throw null; } }
        public event System.IO.Packaging.InvalidSignatureEventHandler InvalidSignatureEvent { add { } remove { } }
        public System.IO.Packaging.PackageDigitalSignature Countersign() { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Countersign(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Countersign(System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Collections.Generic.IEnumerable<System.Uri> signatures) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature GetSignature(System.Uri signatureUri) { throw null; }
        public void RemoveAllSignatures() { }
        public void RemoveSignature(System.Uri signatureUri) { }
        public System.IO.Packaging.PackageDigitalSignature Sign(System.Collections.Generic.IEnumerable<System.Uri> parts) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Sign(System.Collections.Generic.IEnumerable<System.Uri> parts, System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Sign(System.Collections.Generic.IEnumerable<System.Uri> parts, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Collections.Generic.IEnumerable<System.IO.Packaging.PackageRelationshipSelector> relationshipSelectors) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Sign(System.Collections.Generic.IEnumerable<System.Uri> parts, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Collections.Generic.IEnumerable<System.IO.Packaging.PackageRelationshipSelector> relationshipSelectors, string signatureId) { throw null; }
        public System.IO.Packaging.PackageDigitalSignature Sign(System.Collections.Generic.IEnumerable<System.Uri> parts, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Collections.Generic.IEnumerable<System.IO.Packaging.PackageRelationshipSelector> relationshipSelectors, string signatureId, System.Collections.Generic.IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects, System.Collections.Generic.IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509ChainStatusFlags VerifyCertificate(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { throw null; }
        public System.IO.Packaging.VerifyResult VerifySignatures(bool exitOnFailure) { throw null; }
    }
    public partial class RightsManagementInformation
    {
        internal RightsManagementInformation() { }
        public System.Security.RightsManagement.CryptoProvider CryptoProvider { get { throw null; } set { } }
        public void DeleteUseLicense(System.Security.RightsManagement.ContentUser userKey) { }
        public System.Collections.Generic.IDictionary<System.Security.RightsManagement.ContentUser, System.Security.RightsManagement.UseLicense> GetEmbeddedUseLicenses() { throw null; }
        public System.Security.RightsManagement.PublishLicense LoadPublishLicense() { throw null; }
        public System.Security.RightsManagement.UseLicense LoadUseLicense(System.Security.RightsManagement.ContentUser userKey) { throw null; }
        public void SavePublishLicense(System.Security.RightsManagement.PublishLicense publishLicense) { }
        public void SaveUseLicense(System.Security.RightsManagement.ContentUser userKey, System.Security.RightsManagement.UseLicense useLicense) { }
    }
    public partial class SignatureVerificationEventArgs : System.EventArgs
    {
        internal SignatureVerificationEventArgs() { }
        public System.IO.Packaging.PackageDigitalSignature Signature { get { throw null; } }
        public System.IO.Packaging.VerifyResult VerifyResult { get { throw null; } }
    }
    public partial class StorageInfo
    {
        internal StorageInfo() { }
        public string Name { get { throw null; } }
        public System.IO.Packaging.StreamInfo CreateStream(string name) { throw null; }
        public System.IO.Packaging.StreamInfo CreateStream(string name, System.IO.Packaging.CompressionOption compressionOption, System.IO.Packaging.EncryptionOption encryptionOption) { throw null; }
        public System.IO.Packaging.StorageInfo CreateSubStorage(string name) { throw null; }
        public void DeleteStream(string name) { }
        public void DeleteSubStorage(string name) { }
        public System.IO.Packaging.StreamInfo GetStreamInfo(string name) { throw null; }
        public System.IO.Packaging.StreamInfo[] GetStreams() { throw null; }
        public System.IO.Packaging.StorageInfo GetSubStorageInfo(string name) { throw null; }
        public System.IO.Packaging.StorageInfo[] GetSubStorages() { throw null; }
        public bool StreamExists(string name) { throw null; }
        public bool SubStorageExists(string name) { throw null; }
    }
    public partial class StreamInfo
    {
        internal StreamInfo() { }
        public System.IO.Packaging.CompressionOption CompressionOption { get { throw null; } }
        public System.IO.Packaging.EncryptionOption EncryptionOption { get { throw null; } }
        public string Name { get { throw null; } }
        public System.IO.Stream GetStream() { throw null; }
        public System.IO.Stream GetStream(System.IO.FileMode mode) { throw null; }
        public System.IO.Stream GetStream(System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
    }
    public enum VerifyResult
    {
        Success = 0,
        InvalidSignature = 1,
        CertificateRequired = 2,
        InvalidCertificate = 3,
        ReferenceNotFound = 4,
        NotSigned = 5,
    }
}
namespace System.Security.RightsManagement
{
    public enum AuthenticationType
    {
        Windows = 0,
        Passport = 1,
        WindowsPassport = 2,
        Internal = 3,
    }
    public partial class ContentGrant
    {
        public ContentGrant(System.Security.RightsManagement.ContentUser user, System.Security.RightsManagement.ContentRight right) { }
        public ContentGrant(System.Security.RightsManagement.ContentUser user, System.Security.RightsManagement.ContentRight right, System.DateTime validFrom, System.DateTime validUntil) { }
        public System.Security.RightsManagement.ContentRight Right { get { throw null; } }
        public System.Security.RightsManagement.ContentUser User { get { throw null; } }
        public System.DateTime ValidFrom { get { throw null; } }
        public System.DateTime ValidUntil { get { throw null; } }
    }
    public enum ContentRight
    {
        View = 0,
        Edit = 1,
        Print = 2,
        Extract = 3,
        ObjectModel = 4,
        Owner = 5,
        ViewRightsData = 6,
        Forward = 7,
        Reply = 8,
        ReplyAll = 9,
        Sign = 10,
        DocumentEdit = 11,
        Export = 12,
    }
    public partial class ContentUser
    {
        public ContentUser(string name, System.Security.RightsManagement.AuthenticationType authenticationType) { }
        public static System.Security.RightsManagement.ContentUser AnyoneUser { get { throw null; } }
        public System.Security.RightsManagement.AuthenticationType AuthenticationType { get { throw null; } }
        public string Name { get { throw null; } }
        public static System.Security.RightsManagement.ContentUser OwnerUser { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public bool IsAuthenticated() { throw null; }
    }
    public partial class CryptoProvider : System.IDisposable
    {
        internal CryptoProvider() { }
        public int BlockSize { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Security.RightsManagement.ContentGrant> BoundGrants { get { throw null; } }
        public bool CanDecrypt { get { throw null; } }
        public bool CanEncrypt { get { throw null; } }
        public bool CanMergeBlocks { get { throw null; } }
        public byte[] Decrypt(byte[] cryptoText) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public byte[] Encrypt(byte[] clearText) { throw null; }
        ~CryptoProvider() { }
    }
    public partial class LocalizedNameDescriptionPair
    {
        public LocalizedNameDescriptionPair(string name, string description) { }
        public string Description { get { throw null; } }
        public string Name { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class PublishLicense
    {
        public PublishLicense(string signedPublishLicense) { }
        public System.Guid ContentId { get { throw null; } }
        public string ReferralInfoName { get { throw null; } }
        public System.Uri ReferralInfoUri { get { throw null; } }
        public System.Uri UseLicenseAcquisitionUrl { get { throw null; } }
        public System.Security.RightsManagement.UseLicense AcquireUseLicense(System.Security.RightsManagement.SecureEnvironment secureEnvironment) { throw null; }
        public System.Security.RightsManagement.UseLicense AcquireUseLicenseNoUI(System.Security.RightsManagement.SecureEnvironment secureEnvironment) { throw null; }
        public System.Security.RightsManagement.UnsignedPublishLicense DecryptUnsignedPublishLicense(System.Security.RightsManagement.CryptoProvider cryptoProvider) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class RightsManagementException : System.Exception
    {
        public RightsManagementException() { }
        protected RightsManagementException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public RightsManagementException(System.Security.RightsManagement.RightsManagementFailureCode failureCode) { }
        public RightsManagementException(System.Security.RightsManagement.RightsManagementFailureCode failureCode, System.Exception innerException) { }
        public RightsManagementException(System.Security.RightsManagement.RightsManagementFailureCode failureCode, string message) { }
        public RightsManagementException(System.Security.RightsManagement.RightsManagementFailureCode failureCode, string message, System.Exception innerException) { }
        public RightsManagementException(string message) { }
        public RightsManagementException(string message, System.Exception innerException) { }
        public System.Security.RightsManagement.RightsManagementFailureCode FailureCode { get { throw null; } }
#pragma warning disable CS0672 // Member overrides obsolete member
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#pragma warning restore CS0672 // Member overrides obsolete member
    }
    public enum RightsManagementFailureCode
    {
        ManifestPolicyViolation = -2147183860,
        InvalidLicense = -2147168512,
        InfoNotInLicense = -2147168511,
        InvalidLicenseSignature = -2147168510,
        EncryptionNotPermitted = -2147168508,
        RightNotGranted = -2147168507,
        InvalidVersion = -2147168506,
        InvalidEncodingType = -2147168505,
        InvalidNumericalValue = -2147168504,
        InvalidAlgorithmType = -2147168503,
        EnvironmentNotLoaded = -2147168502,
        EnvironmentCannotLoad = -2147168501,
        TooManyLoadedEnvironments = -2147168500,
        IncompatibleObjects = -2147168498,
        LibraryFail = -2147168497,
        EnablingPrincipalFailure = -2147168496,
        InfoNotPresent = -2147168495,
        BadGetInfoQuery = -2147168494,
        KeyTypeUnsupported = -2147168493,
        CryptoOperationUnsupported = -2147168492,
        ClockRollbackDetected = -2147168491,
        QueryReportsNoResults = -2147168490,
        UnexpectedException = -2147168489,
        BindValidityTimeViolated = -2147168488,
        BrokenCertChain = -2147168487,
        BindPolicyViolation = -2147168485,
        BindRevokedLicense = -2147168484,
        BindRevokedIssuer = -2147168483,
        BindRevokedPrincipal = -2147168482,
        BindRevokedResource = -2147168481,
        BindRevokedModule = -2147168480,
        BindContentNotInEndUseLicense = -2147168479,
        BindAccessPrincipalNotEnabling = -2147168478,
        BindAccessUnsatisfied = -2147168477,
        BindIndicatedPrincipalMissing = -2147168476,
        BindMachineNotFoundInGroupIdentity = -2147168475,
        LibraryUnsupportedPlugIn = -2147168474,
        BindRevocationListStale = -2147168473,
        BindNoApplicableRevocationList = -2147168472,
        InvalidHandle = -2147168468,
        BindIntervalTimeViolated = -2147168465,
        BindNoSatisfiedRightsGroup = -2147168464,
        BindSpecifiedWorkMissing = -2147168463,
        NoMoreData = -2147168461,
        LicenseAcquisitionFailed = -2147168460,
        IdMismatch = -2147168459,
        TooManyCertificates = -2147168458,
        NoDistributionPointUrlFound = -2147168457,
        AlreadyInProgress = -2147168456,
        GroupIdentityNotSet = -2147168455,
        RecordNotFound = -2147168454,
        NoConnect = -2147168453,
        NoLicense = -2147168452,
        NeedsMachineActivation = -2147168451,
        NeedsGroupIdentityActivation = -2147168450,
        ActivationFailed = -2147168448,
        Aborted = -2147168447,
        OutOfQuota = -2147168446,
        AuthenticationFailed = -2147168445,
        ServerError = -2147168444,
        InstallationFailed = -2147168443,
        HidCorrupted = -2147168442,
        InvalidServerResponse = -2147168441,
        ServiceNotFound = -2147168440,
        UseDefault = -2147168439,
        ServerNotFound = -2147168438,
        InvalidEmail = -2147168437,
        ValidityTimeViolation = -2147168436,
        OutdatedModule = -2147168435,
        NotSet = -2147168434,
        MetadataNotSet = -2147168433,
        RevocationInfoNotSet = -2147168432,
        InvalidTimeInfo = -2147168431,
        RightNotSet = -2147168430,
        LicenseBindingToWindowsIdentityFailed = -2147168429,
        InvalidIssuanceLicenseTemplate = -2147168428,
        InvalidKeyLength = -2147168427,
        ExpiredOfficialIssuanceLicenseTemplate = -2147168425,
        InvalidClientLicensorCertificate = -2147168424,
        HidInvalid = -2147168423,
        EmailNotVerified = -2147168422,
        ServiceMoved = -2147168421,
        ServiceGone = -2147168420,
        AdEntryNotFound = -2147168419,
        NotAChain = -2147168418,
        RequestDenied = -2147168417,
        DebuggerDetected = -2147168416,
        InvalidLockboxType = -2147168400,
        InvalidLockboxPath = -2147168399,
        InvalidRegistryPath = -2147168398,
        NoAesCryptoProvider = -2147168397,
        GlobalOptionAlreadySet = -2147168396,
        OwnerLicenseNotFound = -2147168395,
        Success = 0,
    }
    public partial class SecureEnvironment : System.IDisposable
    {
        internal SecureEnvironment() { }
        public string ApplicationManifest { get { throw null; } }
        public System.Security.RightsManagement.ContentUser User { get { throw null; } }
        public static System.Security.RightsManagement.SecureEnvironment Create(string applicationManifest, System.Security.RightsManagement.AuthenticationType authentication, System.Security.RightsManagement.UserActivationMode userActivationMode) { throw null; }
        public static System.Security.RightsManagement.SecureEnvironment Create(string applicationManifest, System.Security.RightsManagement.ContentUser user) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.Security.RightsManagement.ContentUser> GetActivatedUsers() { throw null; }
        public static bool IsUserActivated(System.Security.RightsManagement.ContentUser user) { throw null; }
        public static void RemoveActivatedUser(System.Security.RightsManagement.ContentUser user) { }
    }
    public partial class UnsignedPublishLicense
    {
        public UnsignedPublishLicense() { }
        public UnsignedPublishLicense(string publishLicenseTemplate) { }
        public System.Guid ContentId { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<System.Security.RightsManagement.ContentGrant> Grants { get { throw null; } }
        public System.Collections.Generic.IDictionary<int, System.Security.RightsManagement.LocalizedNameDescriptionPair> LocalizedNameDescriptionDictionary { get { throw null; } }
        public System.Security.RightsManagement.ContentUser Owner { get { throw null; } set { } }
        public string ReferralInfoName { get { throw null; } set { } }
        public System.Uri ReferralInfoUri { get { throw null; } set { } }
        public System.Security.RightsManagement.PublishLicense Sign(System.Security.RightsManagement.SecureEnvironment secureEnvironment, out System.Security.RightsManagement.UseLicense authorUseLicense) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class UseLicense
    {
        public UseLicense(string useLicense) { }
        public System.Collections.Generic.IDictionary<string, string> ApplicationData { get { throw null; } }
        public System.Guid ContentId { get { throw null; } }
        public System.Security.RightsManagement.ContentUser Owner { get { throw null; } }
        public System.Security.RightsManagement.CryptoProvider Bind(System.Security.RightsManagement.SecureEnvironment secureEnvironment) { throw null; }
        public override bool Equals(object x) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public enum UserActivationMode
    {
        Permanent = 0,
        Temporary = 1,
    }
}
namespace System.Windows
{
    public abstract partial class AttachedPropertyBrowsableAttribute : System.Attribute
    {
        protected AttachedPropertyBrowsableAttribute() { }
        internal abstract bool IsBrowsable(System.Windows.DependencyObject d, System.Windows.DependencyProperty dp);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true)]
    public sealed partial class AttachedPropertyBrowsableForTypeAttribute : System.Windows.AttachedPropertyBrowsableAttribute
    {
        public AttachedPropertyBrowsableForTypeAttribute(System.Type targetType) { }
        public System.Type TargetType { get { throw null; } }
        public override object TypeId { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        internal override bool IsBrowsable(System.Windows.DependencyObject d, System.Windows.DependencyProperty dp) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    public sealed partial class AttachedPropertyBrowsableWhenAttributePresentAttribute : System.Windows.AttachedPropertyBrowsableAttribute
    {
        public AttachedPropertyBrowsableWhenAttributePresentAttribute(System.Type attributeType) { }
        public System.Type AttributeType { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        internal override bool IsBrowsable(System.Windows.DependencyObject d, System.Windows.DependencyProperty dp) { throw null; }
    }
    public static partial class BaseCompatibilityPreferences
    {
        public static bool FlowDispatcherSynchronizationContextPriority { get { throw null; } set { } }
        public static System.Windows.BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions HandleDispatcherRequestProcessingFailure { get { throw null; } set { } }
        public static bool InlineDispatcherSynchronizationContextSend { get { throw null; } set { } }
        public static bool ReuseDispatcherSynchronizationContextInstance { get { throw null; } set { } }
        public enum HandleDispatcherRequestProcessingFailureOptions
        {
            Continue = 0,
            Throw = 1,
            Reset = 2,
        }
    }
    public delegate object CoerceValueCallback(System.Windows.DependencyObject d, object baseValue);
    [System.Windows.Markup.NameScopePropertyAttribute("NameScope", typeof(System.Windows.NameScope))]
    public partial class DependencyObject : System.Windows.Threading.DispatcherObject
    {
        public DependencyObject() { }
        public System.Windows.DependencyObjectType DependencyObjectType { get { throw null; } }
        public bool IsSealed { get { throw null; } }
        public void ClearValue(System.Windows.DependencyProperty dp) { }
        public void ClearValue(System.Windows.DependencyPropertyKey key) { }
        public void CoerceValue(System.Windows.DependencyProperty dp) { }
        public sealed override bool Equals(object obj) { throw null; }
        public sealed override int GetHashCode() { throw null; }
        public System.Windows.LocalValueEnumerator GetLocalValueEnumerator() { throw null; }
        public object GetValue(System.Windows.DependencyProperty dp) { throw null; }
        public void InvalidateProperty(System.Windows.DependencyProperty dp) { }
        protected virtual void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        public object ReadLocalValue(System.Windows.DependencyProperty dp) { throw null; }
        public void SetCurrentValue(System.Windows.DependencyProperty dp, object value) { }
        public void SetValue(System.Windows.DependencyProperty dp, object value) { }
        public void SetValue(System.Windows.DependencyPropertyKey key, object value) { }
        protected internal virtual bool ShouldSerializeProperty(System.Windows.DependencyProperty dp) { throw null; }
    }
    public partial class DependencyObjectType
    {
        internal DependencyObjectType() { }
        public System.Windows.DependencyObjectType BaseType { get { throw null; } }
        public int Id { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Type SystemType { get { throw null; } }
        public static System.Windows.DependencyObjectType FromSystemType(System.Type systemType) { throw null; }
        public override int GetHashCode() { throw null; }
        public bool IsInstanceOfType(System.Windows.DependencyObject dependencyObject) { throw null; }
        public bool IsSubclassOf(System.Windows.DependencyObjectType dependencyObjectType) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute("System.Windows.Markup.DependencyPropertyConverter, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, Custom=null")]
    public sealed partial class DependencyProperty
    {
        internal DependencyProperty() { }
        public static readonly object UnsetValue;
        public System.Windows.PropertyMetadata DefaultMetadata { get { throw null; } }
        public int GlobalIndex { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Type OwnerType { get { throw null; } }
        public System.Type PropertyType { get { throw null; } }
        public bool ReadOnly { get { throw null; } }
        public System.Windows.ValidateValueCallback ValidateValueCallback { get { throw null; } }
        public System.Windows.DependencyProperty AddOwner(System.Type ownerType) { throw null; }
        public System.Windows.DependencyProperty AddOwner(System.Type ownerType, System.Windows.PropertyMetadata typeMetadata) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Windows.PropertyMetadata GetMetadata(System.Type forType) { throw null; }
        public System.Windows.PropertyMetadata GetMetadata(System.Windows.DependencyObject dependencyObject) { throw null; }
        public System.Windows.PropertyMetadata GetMetadata(System.Windows.DependencyObjectType dependencyObjectType) { throw null; }
        public bool IsValidType(object value) { throw null; }
        public bool IsValidValue(object value) { throw null; }
        public void OverrideMetadata(System.Type forType, System.Windows.PropertyMetadata typeMetadata) { }
        public void OverrideMetadata(System.Type forType, System.Windows.PropertyMetadata typeMetadata, System.Windows.DependencyPropertyKey key) { }
        public static System.Windows.DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType) { throw null; }
        public static System.Windows.DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata typeMetadata) { throw null; }
        public static System.Windows.DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata typeMetadata, System.Windows.ValidateValueCallback validateValueCallback) { throw null; }
        public static System.Windows.DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType) { throw null; }
        public static System.Windows.DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata defaultMetadata) { throw null; }
        public static System.Windows.DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata defaultMetadata, System.Windows.ValidateValueCallback validateValueCallback) { throw null; }
        public static System.Windows.DependencyPropertyKey RegisterAttachedReadOnly(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata defaultMetadata) { throw null; }
        public static System.Windows.DependencyPropertyKey RegisterAttachedReadOnly(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata defaultMetadata, System.Windows.ValidateValueCallback validateValueCallback) { throw null; }
        public static System.Windows.DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata typeMetadata) { throw null; }
        public static System.Windows.DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, System.Windows.PropertyMetadata typeMetadata, System.Windows.ValidateValueCallback validateValueCallback) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct DependencyPropertyChangedEventArgs
    {
        public DependencyPropertyChangedEventArgs(System.Windows.DependencyProperty property, object oldValue, object newValue) { throw null; }
        public object NewValue { get { throw null; } }
        public object OldValue { get { throw null; } }
        public System.Windows.DependencyProperty Property { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Windows.DependencyPropertyChangedEventArgs args) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.DependencyPropertyChangedEventArgs left, System.Windows.DependencyPropertyChangedEventArgs right) { throw null; }
        public static bool operator !=(System.Windows.DependencyPropertyChangedEventArgs left, System.Windows.DependencyPropertyChangedEventArgs right) { throw null; }
    }
    public delegate void DependencyPropertyChangedEventHandler(object sender, System.Windows.DependencyPropertyChangedEventArgs e);
    public sealed partial class DependencyPropertyKey
    {
        internal DependencyPropertyKey() { }
        public System.Windows.DependencyProperty DependencyProperty { get { throw null; } }
        public void OverrideMetadata(System.Type forType, System.Windows.PropertyMetadata typeMetadata) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.ExpressionConverter))]
    public partial class Expression
    {
        internal Expression() { }
    }
    public partial class ExpressionConverter : System.ComponentModel.TypeConverter
    {
        public ExpressionConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public abstract partial class Freezable : System.Windows.DependencyObject
    {
        protected Freezable() { }
        public bool CanFreeze { get { throw null; } }
        public bool IsFrozen { get { throw null; } }
        public event System.EventHandler Changed { add { } remove { } }
        public System.Windows.Freezable Clone() { throw null; }
        protected virtual void CloneCore(System.Windows.Freezable sourceFreezable) { }
        public System.Windows.Freezable CloneCurrentValue() { throw null; }
        protected virtual void CloneCurrentValueCore(System.Windows.Freezable sourceFreezable) { }
        protected System.Windows.Freezable CreateInstance() { throw null; }
        protected abstract System.Windows.Freezable CreateInstanceCore();
        public void Freeze() { }
        protected internal static bool Freeze(System.Windows.Freezable freezable, bool isChecking) { throw null; }
        protected virtual bool FreezeCore(bool isChecking) { throw null; }
        public System.Windows.Freezable GetAsFrozen() { throw null; }
        protected virtual void GetAsFrozenCore(System.Windows.Freezable sourceFreezable) { }
        public System.Windows.Freezable GetCurrentValueAsFrozen() { throw null; }
        protected virtual void GetCurrentValueAsFrozenCore(System.Windows.Freezable sourceFreezable) { }
        protected virtual void OnChanged() { }
        protected void OnFreezablePropertyChanged(System.Windows.DependencyObject oldValue, System.Windows.DependencyObject newValue) { }
        protected void OnFreezablePropertyChanged(System.Windows.DependencyObject oldValue, System.Windows.DependencyObject newValue, System.Windows.DependencyProperty property) { }
        protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e) { }
        protected void ReadPreamble() { }
        protected void WritePostscript() { }
        protected void WritePreamble() { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Int32RectConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Converters.Int32RectValueSerializer))]
    public partial struct Int32Rect : System.IFormattable
    {
        public Int32Rect(int x, int y, int width, int height) { throw null; }
        public static System.Windows.Int32Rect Empty { get { throw null; } }
        public bool HasArea { get { throw null; } }
        public int Height { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } }
        public int Width { get { throw null; } set { } }
        public int X { get { throw null; } set { } }
        public int Y { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Int32Rect value) { throw null; }
        public static bool Equals(System.Windows.Int32Rect int32Rect1, System.Windows.Int32Rect int32Rect2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Int32Rect int32Rect1, System.Windows.Int32Rect int32Rect2) { throw null; }
        public static bool operator !=(System.Windows.Int32Rect int32Rect1, System.Windows.Int32Rect int32Rect2) { throw null; }
        public static System.Windows.Int32Rect Parse(string source) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
    public sealed partial class Int32RectConverter : System.ComponentModel.TypeConverter
    {
        public Int32RectConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public partial interface IWeakEventListener
    {
        bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e);
    }
    public partial struct LocalValueEntry
    {
        public System.Windows.DependencyProperty Property { get { throw null; } }
        public object Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.LocalValueEntry obj1, System.Windows.LocalValueEntry obj2) { throw null; }
        public static bool operator !=(System.Windows.LocalValueEntry obj1, System.Windows.LocalValueEntry obj2) { throw null; }
    }
    public partial struct LocalValueEnumerator : System.Collections.IEnumerator
    {
        public int Count { get { throw null; } }
        public System.Windows.LocalValueEntry Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public bool MoveNext() { throw null; }
        public static bool operator ==(System.Windows.LocalValueEnumerator obj1, System.Windows.LocalValueEnumerator obj2) { throw null; }
        public static bool operator !=(System.Windows.LocalValueEnumerator obj1, System.Windows.LocalValueEnumerator obj2) { throw null; }
        public void Reset() { }
    }
    public partial class NameScope : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable, System.Windows.Markup.INameScope, System.Windows.Markup.INameScopeDictionary
    {
        public static readonly System.Windows.DependencyProperty NameScopeProperty;
        public NameScope() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public object this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public System.Collections.Generic.ICollection<object> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        public void Add(string key, object value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        public object FindName(string name) { throw null; }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public static System.Windows.Markup.INameScope GetNameScope(System.Windows.DependencyObject dependencyObject) { throw null; }
        public void RegisterName(string name, object scopedElement) { }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        public bool Remove(string key) { throw null; }
        public static void SetNameScope(System.Windows.DependencyObject dependencyObject, System.Windows.Markup.INameScope value) { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out object value) { throw null; }
        public void UnregisterName(string name) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.PointConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Converters.PointValueSerializer))]
    public partial struct Point : System.IFormattable
    {
        public Point(double x, double y) { throw null; }
        public double X { get { throw null; } set { } }
        public double Y { get { throw null; } set { } }
        public static System.Windows.Point Add(System.Windows.Point point, System.Windows.Vector vector) { throw null; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Point value) { throw null; }
        public static bool Equals(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Windows.Point Multiply(System.Windows.Point point, System.Windows.Media.Matrix matrix) { throw null; }
        public void Offset(double offsetX, double offsetY) { }
        public static System.Windows.Point operator +(System.Windows.Point point, System.Windows.Vector vector) { throw null; }
        public static bool operator ==(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public static explicit operator System.Windows.Size (System.Windows.Point point) { throw null; }
        public static explicit operator System.Windows.Vector (System.Windows.Point point) { throw null; }
        public static bool operator !=(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public static System.Windows.Point operator *(System.Windows.Point point, System.Windows.Media.Matrix matrix) { throw null; }
        public static System.Windows.Vector operator -(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public static System.Windows.Point operator -(System.Windows.Point point, System.Windows.Vector vector) { throw null; }
        public static System.Windows.Point Parse(string source) { throw null; }
        public static System.Windows.Vector Subtract(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public static System.Windows.Point Subtract(System.Windows.Point point, System.Windows.Vector vector) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
    public sealed partial class PointConverter : System.ComponentModel.TypeConverter
    {
        public PointConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public delegate void PropertyChangedCallback(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e);
    public partial class PropertyMetadata
    {
        public PropertyMetadata() { }
        public PropertyMetadata(object defaultValue) { }
        public PropertyMetadata(object defaultValue, System.Windows.PropertyChangedCallback propertyChangedCallback) { }
        public PropertyMetadata(object defaultValue, System.Windows.PropertyChangedCallback propertyChangedCallback, System.Windows.CoerceValueCallback coerceValueCallback) { }
        public PropertyMetadata(System.Windows.PropertyChangedCallback propertyChangedCallback) { }
        public System.Windows.CoerceValueCallback CoerceValueCallback { get { throw null; } set { } }
        public object DefaultValue { get { throw null; } set { } }
        protected bool IsSealed { get { throw null; } }
        public System.Windows.PropertyChangedCallback PropertyChangedCallback { get { throw null; } set { } }
        protected virtual void Merge(System.Windows.PropertyMetadata baseMetadata, System.Windows.DependencyProperty dp) { }
        protected virtual void OnApply(System.Windows.DependencyProperty dp, System.Type targetType) { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.RectConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Converters.RectValueSerializer))]
    public partial struct Rect : System.IFormattable
    {
        public Rect(double x, double y, double width, double height) { throw null; }
        public Rect(System.Windows.Point point1, System.Windows.Point point2) { throw null; }
        public Rect(System.Windows.Point location, System.Windows.Size size) { throw null; }
        public Rect(System.Windows.Point point, System.Windows.Vector vector) { throw null; }
        public Rect(System.Windows.Size size) { throw null; }
        public double Bottom { get { throw null; } }
        public System.Windows.Point BottomLeft { get { throw null; } }
        public System.Windows.Point BottomRight { get { throw null; } }
        public static System.Windows.Rect Empty { get { throw null; } }
        public double Height { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } }
        public double Left { get { throw null; } }
        public System.Windows.Point Location { get { throw null; } set { } }
        public double Right { get { throw null; } }
        public System.Windows.Size Size { get { throw null; } set { } }
        public double Top { get { throw null; } }
        public System.Windows.Point TopLeft { get { throw null; } }
        public System.Windows.Point TopRight { get { throw null; } }
        public double Width { get { throw null; } set { } }
        public double X { get { throw null; } set { } }
        public double Y { get { throw null; } set { } }
        public bool Contains(double x, double y) { throw null; }
        public bool Contains(System.Windows.Point point) { throw null; }
        public bool Contains(System.Windows.Rect rect) { throw null; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Rect value) { throw null; }
        public static bool Equals(System.Windows.Rect rect1, System.Windows.Rect rect2) { throw null; }
        public override int GetHashCode() { throw null; }
        public void Inflate(double width, double height) { }
        public static System.Windows.Rect Inflate(System.Windows.Rect rect, double width, double height) { throw null; }
        public static System.Windows.Rect Inflate(System.Windows.Rect rect, System.Windows.Size size) { throw null; }
        public void Inflate(System.Windows.Size size) { }
        public void Intersect(System.Windows.Rect rect) { }
        public static System.Windows.Rect Intersect(System.Windows.Rect rect1, System.Windows.Rect rect2) { throw null; }
        public bool IntersectsWith(System.Windows.Rect rect) { throw null; }
        public void Offset(double offsetX, double offsetY) { }
        public static System.Windows.Rect Offset(System.Windows.Rect rect, double offsetX, double offsetY) { throw null; }
        public static System.Windows.Rect Offset(System.Windows.Rect rect, System.Windows.Vector offsetVector) { throw null; }
        public void Offset(System.Windows.Vector offsetVector) { }
        public static bool operator ==(System.Windows.Rect rect1, System.Windows.Rect rect2) { throw null; }
        public static bool operator !=(System.Windows.Rect rect1, System.Windows.Rect rect2) { throw null; }
        public static System.Windows.Rect Parse(string source) { throw null; }
        public void Scale(double scaleX, double scaleY) { }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public void Transform(System.Windows.Media.Matrix matrix) { }
        public static System.Windows.Rect Transform(System.Windows.Rect rect, System.Windows.Media.Matrix matrix) { throw null; }
        public void Union(System.Windows.Point point) { }
        public void Union(System.Windows.Rect rect) { }
        public static System.Windows.Rect Union(System.Windows.Rect rect, System.Windows.Point point) { throw null; }
        public static System.Windows.Rect Union(System.Windows.Rect rect1, System.Windows.Rect rect2) { throw null; }
    }
    public sealed partial class RectConverter : System.ComponentModel.TypeConverter
    {
        public RectConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.SizeConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Converters.SizeValueSerializer))]
    public partial struct Size : System.IFormattable
    {
        public Size(double width, double height) { throw null; }
        public static System.Windows.Size Empty { get { throw null; } }
        public double Height { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } }
        public double Width { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Size value) { throw null; }
        public static bool Equals(System.Windows.Size size1, System.Windows.Size size2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Size size1, System.Windows.Size size2) { throw null; }
        public static explicit operator System.Windows.Point (System.Windows.Size size) { throw null; }
        public static explicit operator System.Windows.Vector (System.Windows.Size size) { throw null; }
        public static bool operator !=(System.Windows.Size size1, System.Windows.Size size2) { throw null; }
        public static System.Windows.Size Parse(string source) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
    public sealed partial class SizeConverter : System.ComponentModel.TypeConverter
    {
        public SizeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public partial class SplashScreen
    {
        public SplashScreen(System.Reflection.Assembly resourceAssembly, string resourceName) { }
        public SplashScreen(string resourceName) { }
        public void Close(System.TimeSpan fadeoutDuration) { }
        public void Show(bool autoClose) { }
        public void Show(bool autoClose, bool topMost) { }
    }
    public delegate bool ValidateValueCallback(object value);
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.VectorConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Converters.VectorValueSerializer))]
    public partial struct Vector : System.IFormattable
    {
        public Vector(double x, double y) { throw null; }
        public double Length { get { throw null; } }
        public double LengthSquared { get { throw null; } }
        public double X { get { throw null; } set { } }
        public double Y { get { throw null; } set { } }
        public static System.Windows.Point Add(System.Windows.Vector vector, System.Windows.Point point) { throw null; }
        public static System.Windows.Vector Add(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static double AngleBetween(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static double CrossProduct(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static double Determinant(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static System.Windows.Vector Divide(System.Windows.Vector vector, double scalar) { throw null; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Vector value) { throw null; }
        public static bool Equals(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Windows.Vector Multiply(double scalar, System.Windows.Vector vector) { throw null; }
        public static System.Windows.Vector Multiply(System.Windows.Vector vector, double scalar) { throw null; }
        public static System.Windows.Vector Multiply(System.Windows.Vector vector, System.Windows.Media.Matrix matrix) { throw null; }
        public static double Multiply(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public void Negate() { }
        public void Normalize() { }
        public static System.Windows.Point operator +(System.Windows.Vector vector, System.Windows.Point point) { throw null; }
        public static System.Windows.Vector operator +(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static System.Windows.Vector operator /(System.Windows.Vector vector, double scalar) { throw null; }
        public static bool operator ==(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static explicit operator System.Windows.Point (System.Windows.Vector vector) { throw null; }
        public static explicit operator System.Windows.Size (System.Windows.Vector vector) { throw null; }
        public static bool operator !=(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static System.Windows.Vector operator *(double scalar, System.Windows.Vector vector) { throw null; }
        public static System.Windows.Vector operator *(System.Windows.Vector vector, double scalar) { throw null; }
        public static System.Windows.Vector operator *(System.Windows.Vector vector, System.Windows.Media.Matrix matrix) { throw null; }
        public static double operator *(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static System.Windows.Vector operator -(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        public static System.Windows.Vector operator -(System.Windows.Vector vector) { throw null; }
        public static System.Windows.Vector Parse(string source) { throw null; }
        public static System.Windows.Vector Subtract(System.Windows.Vector vector1, System.Windows.Vector vector2) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
    public sealed partial class VectorConverter : System.ComponentModel.TypeConverter
    {
        public VectorConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public abstract partial class WeakEventManager : System.Windows.Threading.DispatcherObject
    {
        protected WeakEventManager() { }
        protected object this[object source] { get { throw null; } set { } }
        protected System.IDisposable ReadLock { get { throw null; } }
        protected System.IDisposable WriteLock { get { throw null; } }
        protected void DeliverEvent(object sender, System.EventArgs args) { }
        protected void DeliverEventToList(object sender, System.EventArgs args, System.Windows.WeakEventManager.ListenerList list) { }
        protected static System.Windows.WeakEventManager GetCurrentManager(System.Type managerType) { throw null; }
        protected virtual System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        protected void ProtectedAddHandler(object source, System.Delegate handler) { }
        protected void ProtectedAddListener(object source, System.Windows.IWeakEventListener listener) { }
        protected void ProtectedRemoveHandler(object source, System.Delegate handler) { }
        protected void ProtectedRemoveListener(object source, System.Windows.IWeakEventListener listener) { }
        protected virtual bool Purge(object source, object data, bool purgeAll) { throw null; }
        protected void Remove(object source) { }
        protected void ScheduleCleanup() { }
        protected static void SetCurrentManager(System.Type managerType, System.Windows.WeakEventManager manager) { }
        protected abstract void StartListening(object source);
        protected abstract void StopListening(object source);
        protected partial class ListenerList
        {
            public ListenerList() { }
            public ListenerList(int capacity) { }
            public int Count { get { throw null; } }
            public static System.Windows.WeakEventManager.ListenerList Empty { get { throw null; } }
            public bool IsEmpty { get { throw null; } }
            public System.Windows.IWeakEventListener this[int index] { get { throw null; } }
            public void Add(System.Windows.IWeakEventListener listener) { }
            public void AddHandler(System.Delegate handler) { }
            public bool BeginUse() { throw null; }
            public virtual System.Windows.WeakEventManager.ListenerList Clone() { throw null; }
            protected void CopyTo(System.Windows.WeakEventManager.ListenerList newList) { }
            public virtual bool DeliverEvent(object sender, System.EventArgs args, System.Type managerType) { throw null; }
            public void EndUse() { }
            public static bool PrepareForWriting(ref System.Windows.WeakEventManager.ListenerList list) { throw null; }
            public bool Purge() { throw null; }
            public void Remove(System.Windows.IWeakEventListener listener) { }
            public void RemoveHandler(System.Delegate handler) { }
        }
        protected partial class ListenerList<TEventArgs> : System.Windows.WeakEventManager.ListenerList where TEventArgs : System.EventArgs
        {
            public ListenerList() { }
            public ListenerList(int capacity) { }
            public override System.Windows.WeakEventManager.ListenerList Clone() { throw null; }
            public override bool DeliverEvent(object sender, System.EventArgs e, System.Type managerType) { throw null; }
        }
    }
    public partial class WeakEventManager<TEventSource, TEventArgs> : System.Windows.WeakEventManager where TEventArgs : System.EventArgs
    {
        internal WeakEventManager() { }
        public static void AddHandler(TEventSource source, string eventName, System.EventHandler<TEventArgs> handler) { }
        protected override System.Windows.WeakEventManager.ListenerList NewListenerList() { throw null; }
        public static void RemoveHandler(TEventSource source, string eventName, System.EventHandler<TEventArgs> handler) { }
        protected override void StartListening(object source) { }
        protected override void StopListening(object source) { }
    }
}
namespace System.Windows.Converters
{
    public partial class Int32RectValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public Int32RectValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    public partial class PointValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public PointValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    public partial class RectValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public RectValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    public partial class SizeValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public SizeValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    public partial class VectorValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public VectorValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
}
namespace System.Windows.Data
{
    public abstract partial class DataSourceProvider : System.ComponentModel.INotifyPropertyChanged, System.ComponentModel.ISupportInitialize
    {
        protected DataSourceProvider() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public object Data { get { throw null; } }
        protected System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } set { } }
        public System.Exception Error { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool IsInitialLoadEnabled { get { throw null; } set { } }
        protected bool IsRefreshDeferred { get { throw null; } }
        public event System.EventHandler DataChanged { add { } remove { } }
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        protected virtual void BeginInit() { }
        protected virtual void BeginQuery() { }
        public virtual System.IDisposable DeferRefresh() { throw null; }
        protected virtual void EndInit() { }
        public void InitialLoad() { }
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e) { }
        protected void OnQueryFinished(object newData) { }
        protected virtual void OnQueryFinished(object newData, System.Exception error, System.Windows.Threading.DispatcherOperationCallback completionWork, object callbackArguments) { }
        public void Refresh() { }
        void System.ComponentModel.ISupportInitialize.BeginInit() { }
        void System.ComponentModel.ISupportInitialize.EndInit() { }
    }
}
namespace System.Windows.Input
{
    public enum FocusNavigationDirection
    {
        Next = 0,
        Previous = 1,
        First = 2,
        Last = 3,
        Left = 4,
        Right = 5,
        Up = 6,
        Down = 7,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Input.KeyConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Input.KeyValueSerializer))]
    public enum Key
    {
        None = 0,
        Cancel = 1,
        Back = 2,
        Tab = 3,
        LineFeed = 4,
        Clear = 5,
        Enter = 6,
        Return = 6,
        Pause = 7,
        Capital = 8,
        CapsLock = 8,
        HangulMode = 9,
        KanaMode = 9,
        JunjaMode = 10,
        FinalMode = 11,
        HanjaMode = 12,
        KanjiMode = 12,
        Escape = 13,
        ImeConvert = 14,
        ImeNonConvert = 15,
        ImeAccept = 16,
        ImeModeChange = 17,
        Space = 18,
        PageUp = 19,
        Prior = 19,
        Next = 20,
        PageDown = 20,
        End = 21,
        Home = 22,
        Left = 23,
        Up = 24,
        Right = 25,
        Down = 26,
        Select = 27,
        Print = 28,
        Execute = 29,
        PrintScreen = 30,
        Snapshot = 30,
        Insert = 31,
        Delete = 32,
        Help = 33,
        D0 = 34,
        D1 = 35,
        D2 = 36,
        D3 = 37,
        D4 = 38,
        D5 = 39,
        D6 = 40,
        D7 = 41,
        D8 = 42,
        D9 = 43,
        A = 44,
        B = 45,
        C = 46,
        D = 47,
        E = 48,
        F = 49,
        G = 50,
        H = 51,
        I = 52,
        J = 53,
        K = 54,
        L = 55,
        M = 56,
        N = 57,
        O = 58,
        P = 59,
        Q = 60,
        R = 61,
        S = 62,
        T = 63,
        U = 64,
        V = 65,
        W = 66,
        X = 67,
        Y = 68,
        Z = 69,
        LWin = 70,
        RWin = 71,
        Apps = 72,
        Sleep = 73,
        NumPad0 = 74,
        NumPad1 = 75,
        NumPad2 = 76,
        NumPad3 = 77,
        NumPad4 = 78,
        NumPad5 = 79,
        NumPad6 = 80,
        NumPad7 = 81,
        NumPad8 = 82,
        NumPad9 = 83,
        Multiply = 84,
        Add = 85,
        Separator = 86,
        Subtract = 87,
        Decimal = 88,
        Divide = 89,
        F1 = 90,
        F2 = 91,
        F3 = 92,
        F4 = 93,
        F5 = 94,
        F6 = 95,
        F7 = 96,
        F8 = 97,
        F9 = 98,
        F10 = 99,
        F11 = 100,
        F12 = 101,
        F13 = 102,
        F14 = 103,
        F15 = 104,
        F16 = 105,
        F17 = 106,
        F18 = 107,
        F19 = 108,
        F20 = 109,
        F21 = 110,
        F22 = 111,
        F23 = 112,
        F24 = 113,
        NumLock = 114,
        Scroll = 115,
        LeftShift = 116,
        RightShift = 117,
        LeftCtrl = 118,
        RightCtrl = 119,
        LeftAlt = 120,
        RightAlt = 121,
        BrowserBack = 122,
        BrowserForward = 123,
        BrowserRefresh = 124,
        BrowserStop = 125,
        BrowserSearch = 126,
        BrowserFavorites = 127,
        BrowserHome = 128,
        VolumeMute = 129,
        VolumeDown = 130,
        VolumeUp = 131,
        MediaNextTrack = 132,
        MediaPreviousTrack = 133,
        MediaStop = 134,
        MediaPlayPause = 135,
        LaunchMail = 136,
        SelectMedia = 137,
        LaunchApplication1 = 138,
        LaunchApplication2 = 139,
        Oem1 = 140,
        OemSemicolon = 140,
        OemPlus = 141,
        OemComma = 142,
        OemMinus = 143,
        OemPeriod = 144,
        Oem2 = 145,
        OemQuestion = 145,
        Oem3 = 146,
        OemTilde = 146,
        AbntC1 = 147,
        AbntC2 = 148,
        Oem4 = 149,
        OemOpenBrackets = 149,
        Oem5 = 150,
        OemPipe = 150,
        Oem6 = 151,
        OemCloseBrackets = 151,
        Oem7 = 152,
        OemQuotes = 152,
        Oem8 = 153,
        Oem102 = 154,
        OemBackslash = 154,
        ImeProcessed = 155,
        System = 156,
        DbeAlphanumeric = 157,
        OemAttn = 157,
        DbeKatakana = 158,
        OemFinish = 158,
        DbeHiragana = 159,
        OemCopy = 159,
        DbeSbcsChar = 160,
        OemAuto = 160,
        DbeDbcsChar = 161,
        OemEnlw = 161,
        DbeRoman = 162,
        OemBackTab = 162,
        Attn = 163,
        DbeNoRoman = 163,
        CrSel = 164,
        DbeEnterWordRegisterMode = 164,
        DbeEnterImeConfigureMode = 165,
        ExSel = 165,
        DbeFlushString = 166,
        EraseEof = 166,
        DbeCodeInput = 167,
        Play = 167,
        DbeNoCodeInput = 168,
        Zoom = 168,
        DbeDetermineString = 169,
        NoName = 169,
        DbeEnterDialogConversionMode = 170,
        Pa1 = 170,
        OemClear = 171,
        DeadCharProcessed = 172,
    }
    public partial class KeyConverter : System.ComponentModel.TypeConverter
    {
        public KeyConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public static partial class KeyInterop
    {
        public static System.Windows.Input.Key KeyFromVirtualKey(int virtualKey) { throw null; }
        public static int VirtualKeyFromKey(System.Windows.Input.Key key) { throw null; }
    }
    public partial class KeyValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public KeyValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Input.ModifierKeysConverter))]
    [System.FlagsAttribute]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Input.ModifierKeysValueSerializer))]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
    }
    public partial class ModifierKeysConverter : System.ComponentModel.TypeConverter
    {
        public ModifierKeysConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public static bool IsDefinedModifierKeys(System.Windows.Input.ModifierKeys modifierKeys) { throw null; }
    }
    public partial class ModifierKeysValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public ModifierKeysValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
    public partial class TraversalRequest
    {
        public TraversalRequest(System.Windows.Input.FocusNavigationDirection focusNavigationDirection) { }
        public System.Windows.Input.FocusNavigationDirection FocusNavigationDirection { get { throw null; } }
        public bool Wrapped { get { throw null; } set { } }
    }
}
namespace System.Windows.Interop
{
    public static partial class ComponentDispatcher
    {
        public static System.Windows.Interop.MSG CurrentKeyboardMessage { get { throw null; } }
        public static bool IsThreadModal { get { throw null; } }
        public static event System.EventHandler EnterThreadModal { add { } remove { } }
        public static event System.EventHandler LeaveThreadModal { add { } remove { } }
        public static event System.Windows.Interop.ThreadMessageEventHandler ThreadFilterMessage { add { } remove { } }
        public static event System.EventHandler ThreadIdle { add { } remove { } }
        public static event System.Windows.Interop.ThreadMessageEventHandler ThreadPreprocessMessage { add { } remove { } }
        public static void PopModal() { }
        public static void PushModal() { }
        public static void RaiseIdle() { }
        public static bool RaiseThreadMessage(ref System.Windows.Interop.MSG msg) { throw null; }
    }
    public partial interface IKeyboardInputSink
    {
        System.Windows.Interop.IKeyboardInputSite KeyboardInputSite { get; set; }
        bool HasFocusWithin();
        bool OnMnemonic(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers);
        System.Windows.Interop.IKeyboardInputSite RegisterKeyboardInputSink(System.Windows.Interop.IKeyboardInputSink sink);
        bool TabInto(System.Windows.Input.TraversalRequest request);
        bool TranslateAccelerator(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers);
        bool TranslateChar(ref System.Windows.Interop.MSG msg, System.Windows.Input.ModifierKeys modifiers);
    }
    public partial interface IKeyboardInputSite
    {
        System.Windows.Interop.IKeyboardInputSink Sink { get; }
        bool OnNoMoreTabStops(System.Windows.Input.TraversalRequest request);
        void Unregister();
    }
    public partial struct MSG
    {
        public System.IntPtr hwnd { get { throw null; } set { } }
        public System.IntPtr lParam { get { throw null; } set { } }
        public int message { get { throw null; } set { } }
        public int pt_x { get { throw null; } set { } }
        public int pt_y { get { throw null; } set { } }
        public int time { get { throw null; } set { } }
        public System.IntPtr wParam { get { throw null; } set { } }
    }
    public delegate void ThreadMessageEventHandler(ref System.Windows.Interop.MSG msg, ref bool handled);
}
namespace System.Windows.Markup
{
    [System.FlagsAttribute]
    public enum DesignerSerializationOptions
    {
        SerializeAsAttribute = 1,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class DesignerSerializationOptionsAttribute : System.Attribute
    {
        public DesignerSerializationOptionsAttribute(System.Windows.Markup.DesignerSerializationOptions designerSerializationOptions) { }
        public System.Windows.Markup.DesignerSerializationOptions DesignerSerializationOptions { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class InternalTypeHelper
    {
        protected InternalTypeHelper() { }
        protected internal abstract void AddEventHandler(System.Reflection.EventInfo eventInfo, object target, System.Delegate handler);
        protected internal abstract System.Delegate CreateDelegate(System.Type delegateType, object target, string handler);
        protected internal abstract object CreateInstance(System.Type type, System.Globalization.CultureInfo culture);
        protected internal abstract object GetPropertyValue(System.Reflection.PropertyInfo propertyInfo, object target, System.Globalization.CultureInfo culture);
        protected internal abstract void SetPropertyValue(System.Reflection.PropertyInfo propertyInfo, object target, object value, System.Globalization.CultureInfo culture);
    }
    [System.ObsoleteAttribute("IReceiveMarkupExtension has been deprecated. This interface is no longer in use.")]
    public partial interface IReceiveMarkupExtension
    {
        void ReceiveMarkupExtension(string property, System.Windows.Markup.MarkupExtension markupExtension, System.IServiceProvider serviceProvider);
    }
    [System.ComponentModel.BrowsableAttribute(false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class ServiceProviders : System.IServiceProvider
    {
        public ServiceProviders() { }
        public void AddService(System.Type serviceType, object service) { }
        public object GetService(System.Type serviceType) { throw null; }
    }
}
namespace System.Windows.Markup.Primitives
{
    public abstract partial class MarkupObject
    {
        internal MarkupObject() { }
        public abstract System.ComponentModel.AttributeCollection Attributes { get; }
        public abstract object Instance { get; }
        public abstract System.Type ObjectType { get; }
        public virtual System.Collections.Generic.IEnumerable<System.Windows.Markup.Primitives.MarkupProperty> Properties { get { throw null; } }
        public abstract void AssignRootContext(System.Windows.Markup.IValueSerializerContext context);
    }
    public abstract partial class MarkupProperty
    {
        internal MarkupProperty() { }
        public abstract System.ComponentModel.AttributeCollection Attributes { get; }
        public virtual System.Windows.DependencyProperty DependencyProperty { get { throw null; } }
        public virtual bool IsAttached { get { throw null; } }
        public virtual bool IsComposite { get { throw null; } }
        public virtual bool IsConstructorArgument { get { throw null; } }
        public virtual bool IsContent { get { throw null; } }
        public virtual bool IsKey { get { throw null; } }
        public virtual bool IsValueAsString { get { throw null; } }
        public abstract System.Collections.Generic.IEnumerable<System.Windows.Markup.Primitives.MarkupObject> Items { get; }
        public abstract string Name { get; }
        public virtual System.ComponentModel.PropertyDescriptor PropertyDescriptor { get { throw null; } }
        public abstract System.Type PropertyType { get; }
        public abstract string StringValue { get; }
        public abstract System.Collections.Generic.IEnumerable<System.Type> TypeReferences { get; }
        public abstract object Value { get; }
    }
}
namespace System.Windows.Media
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false)]
    public sealed partial class DisableDpiAwarenessAttribute : System.Attribute
    {
        public DisableDpiAwarenessAttribute() { }
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Media.MatrixConverter))]
    [System.Windows.Markup.ValueSerializerAttribute(typeof(System.Windows.Media.Converters.MatrixValueSerializer))]
    public partial struct Matrix : System.IFormattable
    {
        public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY) { throw null; }
        public double Determinant { get { throw null; } }
        public bool HasInverse { get { throw null; } }
        public static System.Windows.Media.Matrix Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public double M11 { get { throw null; } set { } }
        public double M12 { get { throw null; } set { } }
        public double M21 { get { throw null; } set { } }
        public double M22 { get { throw null; } set { } }
        public double OffsetX { get { throw null; } set { } }
        public double OffsetY { get { throw null; } set { } }
        public void Append(System.Windows.Media.Matrix matrix) { }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Windows.Media.Matrix value) { throw null; }
        public static bool Equals(System.Windows.Media.Matrix matrix1, System.Windows.Media.Matrix matrix2) { throw null; }
        public override int GetHashCode() { throw null; }
        public void Invert() { }
        public static System.Windows.Media.Matrix Multiply(System.Windows.Media.Matrix trans1, System.Windows.Media.Matrix trans2) { throw null; }
        public static bool operator ==(System.Windows.Media.Matrix matrix1, System.Windows.Media.Matrix matrix2) { throw null; }
        public static bool operator !=(System.Windows.Media.Matrix matrix1, System.Windows.Media.Matrix matrix2) { throw null; }
        public static System.Windows.Media.Matrix operator *(System.Windows.Media.Matrix trans1, System.Windows.Media.Matrix trans2) { throw null; }
        public static System.Windows.Media.Matrix Parse(string source) { throw null; }
        public void Prepend(System.Windows.Media.Matrix matrix) { }
        public void Rotate(double angle) { }
        public void RotateAt(double angle, double centerX, double centerY) { }
        public void RotateAtPrepend(double angle, double centerX, double centerY) { }
        public void RotatePrepend(double angle) { }
        public void Scale(double scaleX, double scaleY) { }
        public void ScaleAt(double scaleX, double scaleY, double centerX, double centerY) { }
        public void ScaleAtPrepend(double scaleX, double scaleY, double centerX, double centerY) { }
        public void ScalePrepend(double scaleX, double scaleY) { }
        public void SetIdentity() { }
        public void Skew(double skewX, double skewY) { }
        public void SkewPrepend(double skewX, double skewY) { }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public System.Windows.Point Transform(System.Windows.Point point) { throw null; }
        public void Transform(System.Windows.Point[] points) { }
        public System.Windows.Vector Transform(System.Windows.Vector vector) { throw null; }
        public void Transform(System.Windows.Vector[] vectors) { }
        public void Translate(double offsetX, double offsetY) { }
        public void TranslatePrepend(double offsetX, double offsetY) { }
    }
    public sealed partial class MatrixConverter : System.ComponentModel.TypeConverter
    {
        public MatrixConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public enum TextFormattingMode
    {
        Ideal = 0,
        Display = 1,
    }
}
namespace System.Windows.Media.Converters
{
    public partial class MatrixValueSerializer : System.Windows.Markup.ValueSerializer
    {
        public MatrixValueSerializer() { }
        public override bool CanConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override bool CanConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override object ConvertFromString(string value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
        public override string ConvertToString(object value, System.Windows.Markup.IValueSerializerContext context) { throw null; }
    }
}
namespace System.Windows.Threading
{
    public sealed partial class Dispatcher
    {
        internal Dispatcher() { }
        public static System.Windows.Threading.Dispatcher CurrentDispatcher { get { throw null; } }
        public bool HasShutdownFinished { get { throw null; } }
        public bool HasShutdownStarted { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Windows.Threading.DispatcherHooks Hooks { get { throw null; } }
        public System.Threading.Thread Thread { get { throw null; } }
        public event System.EventHandler ShutdownFinished { add { } remove { } }
        public event System.EventHandler ShutdownStarted { add { } remove { } }
        public event System.Windows.Threading.DispatcherUnhandledExceptionEventHandler UnhandledException { add { } remove { } }
        public event System.Windows.Threading.DispatcherUnhandledExceptionFilterEventHandler UnhandledExceptionFilter { add { } remove { } }
        public System.Windows.Threading.DispatcherOperation BeginInvoke(System.Delegate method, params object[] args) { throw null; }
        public System.Windows.Threading.DispatcherOperation BeginInvoke(System.Delegate method, System.Windows.Threading.DispatcherPriority priority, params object[] args) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Windows.Threading.DispatcherOperation BeginInvoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Windows.Threading.DispatcherOperation BeginInvoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method, object arg) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Windows.Threading.DispatcherOperation BeginInvoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method, object arg, params object[] args) { throw null; }
        public void BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority priority) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool CheckAccess() { throw null; }
        public System.Windows.Threading.DispatcherProcessingDisabled DisableProcessing() { throw null; }
        public static void ExitAllFrames() { }
        public static System.Windows.Threading.Dispatcher FromThread(System.Threading.Thread thread) { throw null; }
        public void Invoke(System.Action callback) { }
        public void Invoke(System.Action callback, System.Windows.Threading.DispatcherPriority priority) { }
        public void Invoke(System.Action callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken) { }
        public void Invoke(System.Action callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken, System.TimeSpan timeout) { }
        public object Invoke(System.Delegate method, params object[] args) { throw null; }
        public object Invoke(System.Delegate method, System.TimeSpan timeout, params object[] args) { throw null; }
        public object Invoke(System.Delegate method, System.TimeSpan timeout, System.Windows.Threading.DispatcherPriority priority, params object[] args) { throw null; }
        public object Invoke(System.Delegate method, System.Windows.Threading.DispatcherPriority priority, params object[] args) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method, object arg) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.Delegate method, object arg, params object[] args) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.TimeSpan timeout, System.Delegate method) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.TimeSpan timeout, System.Delegate method, object arg) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object Invoke(System.Windows.Threading.DispatcherPriority priority, System.TimeSpan timeout, System.Delegate method, object arg, params object[] args) { throw null; }
        public System.Windows.Threading.DispatcherOperation InvokeAsync(System.Action callback) { throw null; }
        public System.Windows.Threading.DispatcherOperation InvokeAsync(System.Action callback, System.Windows.Threading.DispatcherPriority priority) { throw null; }
        public System.Windows.Threading.DispatcherOperation InvokeAsync(System.Action callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Windows.Threading.DispatcherOperation<TResult> InvokeAsync<TResult>(System.Func<TResult> callback) { throw null; }
        public System.Windows.Threading.DispatcherOperation<TResult> InvokeAsync<TResult>(System.Func<TResult> callback, System.Windows.Threading.DispatcherPriority priority) { throw null; }
        public System.Windows.Threading.DispatcherOperation<TResult> InvokeAsync<TResult>(System.Func<TResult> callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void InvokeShutdown() { }
        public TResult Invoke<TResult>(System.Func<TResult> callback) { throw null; }
        public TResult Invoke<TResult>(System.Func<TResult> callback, System.Windows.Threading.DispatcherPriority priority) { throw null; }
        public TResult Invoke<TResult>(System.Func<TResult> callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken) { throw null; }
        public TResult Invoke<TResult>(System.Func<TResult> callback, System.Windows.Threading.DispatcherPriority priority, System.Threading.CancellationToken cancellationToken, System.TimeSpan timeout) { throw null; }
        public static void PushFrame(System.Windows.Threading.DispatcherFrame frame) { }
        public static void Run() { }
        public static void ValidatePriority(System.Windows.Threading.DispatcherPriority priority, string parameterName) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyAccess() { }
        public static System.Windows.Threading.DispatcherPriorityAwaitable Yield() { throw null; }
        public static System.Windows.Threading.DispatcherPriorityAwaitable Yield(System.Windows.Threading.DispatcherPriority priority) { throw null; }
    }
    public partial class DispatcherEventArgs : System.EventArgs
    {
        internal DispatcherEventArgs() { }
        public System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } }
    }
    public partial class DispatcherFrame : System.Windows.Threading.DispatcherObject
    {
        public DispatcherFrame() { }
        public DispatcherFrame(bool exitWhenRequested) { }
        public bool Continue { get { throw null; } set { } }
    }
    public sealed partial class DispatcherHookEventArgs : System.EventArgs
    {
        public DispatcherHookEventArgs(System.Windows.Threading.DispatcherOperation operation) { }
        public System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } }
        public System.Windows.Threading.DispatcherOperation Operation { get { throw null; } }
    }
    public delegate void DispatcherHookEventHandler(object sender, System.Windows.Threading.DispatcherHookEventArgs e);
    public sealed partial class DispatcherHooks
    {
        internal DispatcherHooks() { }
        public event System.EventHandler DispatcherInactive { add { } remove { } }
        public event System.Windows.Threading.DispatcherHookEventHandler OperationAborted { add { } remove { } }
        public event System.Windows.Threading.DispatcherHookEventHandler OperationCompleted { add { } remove { } }
        public event System.Windows.Threading.DispatcherHookEventHandler OperationPosted { add { } remove { } }
        public event System.Windows.Threading.DispatcherHookEventHandler OperationPriorityChanged { add { } remove { } }
        public event System.Windows.Threading.DispatcherHookEventHandler OperationStarted { add { } remove { } }
    }
    public abstract partial class DispatcherObject
    {
        protected DispatcherObject() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool CheckAccess() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyAccess() { }
    }
    public partial class DispatcherOperation
    {
        internal DispatcherOperation() { }
        public System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } }
        public System.Windows.Threading.DispatcherPriority Priority { get { throw null; } set { } }
        public object Result { get { throw null; } }
        public System.Windows.Threading.DispatcherOperationStatus Status { get { throw null; } }
        public System.Threading.Tasks.Task Task { get { throw null; } }
        public event System.EventHandler Aborted { add { } remove { } }
        public event System.EventHandler Completed { add { } remove { } }
        public bool Abort() { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() { throw null; }
        protected virtual object InvokeDelegateCore() { throw null; }
        public System.Windows.Threading.DispatcherOperationStatus Wait() { throw null; }
        public System.Windows.Threading.DispatcherOperationStatus Wait(System.TimeSpan timeout) { throw null; }
    }
    public delegate object DispatcherOperationCallback(object arg);
    public enum DispatcherOperationStatus
    {
        Pending = 0,
        Aborted = 1,
        Completed = 2,
        Executing = 3,
    }
    public partial class DispatcherOperation<TResult> : System.Windows.Threading.DispatcherOperation
    {
        internal DispatcherOperation() { }
        public new TResult Result { get { throw null; } }
        public new System.Threading.Tasks.Task<TResult> Task { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public new System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter() { throw null; }
        protected override object InvokeDelegateCore() { throw null; }
    }
    public enum DispatcherPriority
    {
        Invalid = -1,
        Inactive = 0,
        SystemIdle = 1,
        ApplicationIdle = 2,
        ContextIdle = 3,
        Background = 4,
        Input = 5,
        Loaded = 6,
        Render = 7,
        DataBind = 8,
        Normal = 9,
        Send = 10,
    }
    public partial struct DispatcherPriorityAwaitable
    {
        public System.Windows.Threading.DispatcherPriorityAwaiter GetAwaiter() { throw null; }
    }
    public partial struct DispatcherPriorityAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { throw null; } }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
    }
    public partial struct DispatcherProcessingDisabled : System.IDisposable
    {
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Windows.Threading.DispatcherProcessingDisabled left, System.Windows.Threading.DispatcherProcessingDisabled right) { throw null; }
        public static bool operator !=(System.Windows.Threading.DispatcherProcessingDisabled left, System.Windows.Threading.DispatcherProcessingDisabled right) { throw null; }
    }
    public sealed partial class DispatcherSynchronizationContext : System.Threading.SynchronizationContext
    {
        public DispatcherSynchronizationContext() { }
        public DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher dispatcher) { }
        public DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher dispatcher, System.Windows.Threading.DispatcherPriority priority) { }
        public override System.Threading.SynchronizationContext CreateCopy() { throw null; }
        public override void Post(System.Threading.SendOrPostCallback d, object state) { }
        public override void Send(System.Threading.SendOrPostCallback d, object state) { }
        #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
        [System.Runtime.ConstrainedExecution.PrePrepareMethodAttribute]
        #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported. 
        public override int Wait(System.IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) { throw null; }
    }
    public partial class DispatcherTimer
    {
        public DispatcherTimer() { }
        public DispatcherTimer(System.TimeSpan interval, System.Windows.Threading.DispatcherPriority priority, System.EventHandler callback, System.Windows.Threading.Dispatcher dispatcher) { }
        public DispatcherTimer(System.Windows.Threading.DispatcherPriority priority) { }
        public DispatcherTimer(System.Windows.Threading.DispatcherPriority priority, System.Windows.Threading.Dispatcher dispatcher) { }
        public System.Windows.Threading.Dispatcher Dispatcher { get { throw null; } }
        public System.TimeSpan Interval { get { throw null; } set { } }
        public bool IsEnabled { get { throw null; } set { } }
        public object Tag { get { throw null; } set { } }
        public event System.EventHandler Tick { add { } remove { } }
        public void Start() { }
        public void Stop() { }
    }
    public sealed partial class DispatcherUnhandledExceptionEventArgs : System.Windows.Threading.DispatcherEventArgs
    {
        internal DispatcherUnhandledExceptionEventArgs() { }
        public System.Exception Exception { get { throw null; } }
        public bool Handled { get { throw null; } set { } }
    }
    public delegate void DispatcherUnhandledExceptionEventHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e);
    public sealed partial class DispatcherUnhandledExceptionFilterEventArgs : System.Windows.Threading.DispatcherEventArgs
    {
        internal DispatcherUnhandledExceptionFilterEventArgs() { }
        public System.Exception Exception { get { throw null; } }
        public bool RequestCatch { get { throw null; } set { } }
    }
    public delegate void DispatcherUnhandledExceptionFilterEventHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionFilterEventArgs e);
}
