// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines CollectionViewSource object, the markup-accessible entry
//              point to CollectionView.
//
// See spec at CollectionViewSource.mht
//

using System;
using System.Collections;           // IEnumerable
using System.Collections.ObjectModel;   // ObservableCollection<T>
using System.Collections.Specialized;   // NotifyCollectionChanged*
using System.Globalization;         // CultureInfo
using System.ComponentModel;        // ICollectionView
using System.Windows.Markup;        // XmlLanguage
using MS.Internal;                  // Invariant.Assert
using MS.Internal.Data;             // DataBindEngine

namespace System.Windows.Data
{
    /// <summary>
    ///  Describes a collection view.
    /// </summary>
    public class CollectionViewSource : DependencyObject, ISupportInitialize, IWeakEventListener
    {
        #region Constructors

        //
        //  Constructors
        //

        /// <summary>
        ///     Initializes a new instance of the CollectionViewSource class.
        /// </summary>
        public CollectionViewSource()
        {
            _sort = new SortDescriptionCollection();
            ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(OnForwardedCollectionChanged);

            _groupBy = new ObservableCollection<GroupDescription>();
            ((INotifyCollectionChanged)_groupBy).CollectionChanged += new NotifyCollectionChangedEventHandler(OnForwardedCollectionChanged);
        }

        #endregion Constructors

        #region Public Properties

        //
        //  Public Properties
        //

        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ViewPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "View",
                    typeof(ICollectionView),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((ICollectionView)null));

        /// <summary>
        ///     The DependencyProperty for the View property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ViewProperty
            = ViewPropertyKey.DependencyProperty;

        /// <summary>
        ///     Returns the ICollectionView currently affiliated with this CollectionViewSource.
        /// </summary>
        [ReadOnly(true)]
        public ICollectionView View
        {
            get
            {
                return GetOriginalView(CollectionView);
            }
        }



        /// <summary>
        ///     The DependencyProperty for the Source property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty SourceProperty
            = DependencyProperty.Register(
                    "Source",
                    typeof(object),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata(
                            (object)null,
                            new PropertyChangedCallback(OnSourceChanged)),
                    new ValidateValueCallback(IsSourceValid));

        /// <summary>
        ///     Source is the underlying collection.
        /// </summary>
        public object Source
        {
            get { return (object) GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        ///     Called when SourceProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">Argument.</param>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource ctrl = (CollectionViewSource) d;

            ctrl.OnSourceChanged(e.OldValue, e.NewValue);
            ctrl.EnsureView();
        }

        /// <summary>
        ///     This method is invoked when the Source property changes.
        /// </summary>
        /// <param name="oldSource">The old value of the Source property.</param>
        /// <param name="newSource">The new value of the Source property.</param>
        protected virtual void OnSourceChanged(object oldSource, object newSource)
        {
        }

        private static bool IsSourceValid(object o)
        {
            return (o == null ||
                        o is IEnumerable ||
                        o is IListSource ||
                        o is DataSourceProvider) &&
                    !(o is ICollectionView);
        }

        private static bool IsValidSourceForView(object o)
        {
            return (    o is IEnumerable ||
                        o is IListSource);
        }



        /// <summary>
        ///     The DependencyProperty for the CollectionViewType property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CollectionViewTypeProperty
            = DependencyProperty.Register(
                    "CollectionViewType",
                    typeof(Type),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata(
                            (Type)null,
                            new PropertyChangedCallback(OnCollectionViewTypeChanged)),
                    new ValidateValueCallback(IsCollectionViewTypeValid));

        /// <summary>
        ///     CollectionViewType is the desired type of the View.
        /// </summary>
        /// <remarks>
        ///     This property may only be set during initialization.
        /// </remarks>
        public Type CollectionViewType
        {
            get { return (Type) GetValue(CollectionViewTypeProperty); }
            set { SetValue(CollectionViewTypeProperty, value); }
        }

        private static void OnCollectionViewTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource ctrl = (CollectionViewSource) d;

            Type oldCollectionViewType = (Type) e.OldValue;
            Type newCollectionViewType = (Type) e.NewValue;

            if (!ctrl._isInitializing)
                throw new InvalidOperationException(SR.Get(SRID.CollectionViewTypeIsInitOnly));

            ctrl.OnCollectionViewTypeChanged(oldCollectionViewType, newCollectionViewType);
            ctrl.EnsureView();
        }

        /// <summary>
        ///     This method is invoked when the CollectionViewType property changes.
        /// </summary>
        /// <param name="oldCollectionViewType">The old value of the CollectionViewType property.</param>
        /// <param name="newCollectionViewType">The new value of the CollectionViewType property.</param>
        protected virtual void OnCollectionViewTypeChanged(Type oldCollectionViewType, Type newCollectionViewType)
        {
        }

        private static bool IsCollectionViewTypeValid(object o)
        {
            Type type = (Type)o;

            return type == null ||
                typeof(ICollectionView).IsAssignableFrom(type);
        }

        /// <summary>
        /// CultureInfo used for sorting, comparisons, etc.
        /// This property is forwarded to any collection view created from this source.
        /// </summary>
        [TypeConverter(typeof(System.Windows.CultureInfoIetfLanguageTagConverter))]
        public CultureInfo Culture
        {
            get { return _culture; }
            set { _culture = value; OnForwardedPropertyChanged(); }
        }

        /// <summary>
        /// Collection of SortDescriptions, describing sorting.
        /// This property is forwarded to any collection view created from this source.
        /// </summary>
        public SortDescriptionCollection SortDescriptions
        {
            get { return _sort; }
        }

        /// <summary>
        /// Collection of GroupDescriptions, describing grouping.
        /// This property is forwarded to any collection view created from this source.
        /// </summary>
        public ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return _groupBy; }
        }


        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey CanChangeLiveSortingPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "CanChangeLiveSorting",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        ///     The DependencyProperty for the CanChangeLiveSorting property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty CanChangeLiveSortingProperty
            = CanChangeLiveSortingPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether the underlying view allows
        /// turning live sorting on or off.
        ///</summary>
        [ReadOnly(true)]
        public bool CanChangeLiveSorting
        {
            get { return (bool)GetValue(CanChangeLiveSortingProperty); }
            private set { SetValue(CanChangeLiveSortingPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsLiveSortingRequested property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsLiveSortingRequestedProperty
            = DependencyProperty.Register(
                    "IsLiveSortingRequested",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false,
                        new PropertyChangedCallback(OnIsLiveSortingRequestedChanged)));

        ///<summary>
        /// Gets or sets a value that the CollectionViewSource will use to
        /// set the IsLiveSorting property of its views, when possible.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping and
        /// its CanChangeLiveSortingProperty is true, this property is used to
        /// set the view's IsLiveSorting property.
        ///</notes>
        public bool IsLiveSortingRequested
        {
            get { return (bool)GetValue(IsLiveSortingRequestedProperty); }
            set { SetValue(IsLiveSortingRequestedProperty, value); }
        }

        private static void OnIsLiveSortingRequestedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)d;
            cvs.OnForwardedPropertyChanged();
        }

        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsLiveSortingPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "IsLiveSorting",
                    typeof(bool?),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool?)null));

        /// <summary>
        ///     The DependencyProperty for the IsLiveSorting property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty IsLiveSortingProperty
            = IsLiveSortingPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether live sorting is enabled for the
        /// underlying view.
        ///</summary>
        ///<notes>
        /// The value is null if the view does not implement ICollectionViewLiveShaping,
        /// or if it cannot tell whether it is live-sorting.
        ///</notes>
        [ReadOnly(true)]
        public bool? IsLiveSorting
        {
            get { return (bool?)GetValue(IsLiveSortingProperty); }
            private set { SetValue(IsLiveSortingPropertyKey, value); }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-sorting recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveSortingProperties.
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its SortDescriptions.
        ///</notes>
        public ObservableCollection<string> LiveSortingProperties
        {
            get
            {
                if (_liveSortingProperties == null)
                {
                    _liveSortingProperties = new ObservableCollection<string>();
                    ((INotifyCollectionChanged)_liveSortingProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(OnForwardedCollectionChanged);
                }
                return _liveSortingProperties;
            }
        }


        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey CanChangeLiveFilteringPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "CanChangeLiveFiltering",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        ///     The DependencyProperty for the CanChangeLiveFiltering property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty CanChangeLiveFilteringProperty
            = CanChangeLiveFilteringPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether the underlying view allows
        /// turning live Filtering on or off.
        ///</summary>
        [ReadOnly(true)]
        public bool CanChangeLiveFiltering
        {
            get { return (bool)GetValue(CanChangeLiveFilteringProperty); }
            private set { SetValue(CanChangeLiveFilteringPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsLiveFilteringRequested property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsLiveFilteringRequestedProperty
            = DependencyProperty.Register(
                    "IsLiveFilteringRequested",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false,
                        new PropertyChangedCallback(OnIsLiveFilteringRequestedChanged)));

        ///<summary>
        /// Gets or sets a value that the CollectionViewSource will use to
        /// set the IsLiveFiltering property of its views, when possible.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping and
        /// its CanChangeLiveFilteringProperty is true, this property is used to
        /// set the view's IsLiveFiltering property.
        ///</notes>
        public bool IsLiveFilteringRequested
        {
            get { return (bool)GetValue(IsLiveFilteringRequestedProperty); }
            set { SetValue(IsLiveFilteringRequestedProperty, value); }
        }

        private static void OnIsLiveFilteringRequestedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)d;
            cvs.OnForwardedPropertyChanged();
        }

        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsLiveFilteringPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "IsLiveFiltering",
                    typeof(bool?),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool?)null));

        /// <summary>
        ///     The DependencyProperty for the IsLiveFiltering property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty IsLiveFilteringProperty
            = IsLiveFilteringPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether live Filtering is enabled for the
        /// underlying view.
        ///</summary>
        ///<notes>
        /// The value is null if the view does not implement ICollectionViewLiveShaping,
        /// or if it cannot tell whether it is live-Filtering.
        ///</notes>
        [ReadOnly(true)]
        public bool? IsLiveFiltering
        {
            get { return (bool?)GetValue(IsLiveFilteringProperty); }
            private set { SetValue(IsLiveFilteringPropertyKey, value); }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-filtering recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveFilteringProperties.
        ///</notes>
        public ObservableCollection<string> LiveFilteringProperties
        {
            get
            {
                if (_liveFilteringProperties == null)
                {
                    _liveFilteringProperties = new ObservableCollection<string>();
                    ((INotifyCollectionChanged)_liveFilteringProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(OnForwardedCollectionChanged);
                }
                return _liveFilteringProperties;
            }
        }


        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey CanChangeLiveGroupingPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "CanChangeLiveGrouping",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false));

        /// <summary>
        ///     The DependencyProperty for the CanChangeLiveGrouping property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty CanChangeLiveGroupingProperty
            = CanChangeLiveGroupingPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether the underlying view allows
        /// turning live Grouping on or off.
        ///</summary>
        [ReadOnly(true)]
        public bool CanChangeLiveGrouping
        {
            get { return (bool)GetValue(CanChangeLiveGroupingProperty); }
            private set { SetValue(CanChangeLiveGroupingPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsLiveGroupingRequested property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsLiveGroupingRequestedProperty
            = DependencyProperty.Register(
                    "IsLiveGroupingRequested",
                    typeof(bool),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool)false,
                        new PropertyChangedCallback(OnIsLiveGroupingRequestedChanged)));

        ///<summary>
        /// Gets or sets a value that the CollectionViewSource will use to
        /// set the IsLiveGrouping property of its views, when possible.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping and
        /// its CanChangeLiveGroupingProperty is true, this property is used to
        /// set the view's IsLiveGrouping property.
        ///</notes>
        public bool IsLiveGroupingRequested
        {
            get { return (bool)GetValue(IsLiveGroupingRequestedProperty); }
            set { SetValue(IsLiveGroupingRequestedProperty, value); }
        }

        private static void OnIsLiveGroupingRequestedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)d;
            cvs.OnForwardedPropertyChanged();
        }

        /// <summary>
        ///     The key needed to define a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsLiveGroupingPropertyKey
            = DependencyProperty.RegisterReadOnly(
                    "IsLiveGrouping",
                    typeof(bool?),
                    typeof(CollectionViewSource),
                    new FrameworkPropertyMetadata((bool?)null));

        /// <summary>
        ///     The DependencyProperty for the IsLiveGrouping property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty IsLiveGroupingProperty
            = IsLiveGroupingPropertyKey.DependencyProperty;

        ///<summary>
        /// Gets a value that indicates whether live Grouping is enabled for the
        /// underlying view.
        ///</summary>
        ///<notes>
        /// The value is null if the view does not implement ICollectionViewLiveShaping,
        /// or if it cannot tell whether it is live-Grouping.
        ///</notes>
        [ReadOnly(true)]
        public bool? IsLiveGrouping
        {
            get { return (bool?)GetValue(IsLiveGroupingProperty); }
            private set { SetValue(IsLiveGroupingPropertyKey, value); }
        }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-grouping recalculation.
        /// The strings use the same format as PropertyGroupDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When the underlying view implements ICollectionViewLiveShaping,
        /// this collection is used to set the underlying view's LiveGroupingProperties.
        ///</notes>
        public ObservableCollection<string> LiveGroupingProperties
        {
            get
            {
                if (_liveGroupingProperties == null)
                {
                    _liveGroupingProperties = new ObservableCollection<string>();
                    ((INotifyCollectionChanged)_liveGroupingProperties).CollectionChanged += new NotifyCollectionChangedEventHandler(OnForwardedCollectionChanged);
                }
                return _liveGroupingProperties;
            }
        }

        #endregion Public Properties

        #region Public Events

        /// <summary>
        ///     An event requesting a filter query.
        /// </summary>
        public event FilterEventHandler Filter
        {
            add
            {
                // Get existing event hanlders
                FilterEventHandler handlers = FilterHandlersField.GetValue(this);
                if (handlers != null)
                {
                    // combine to a multicast delegate
                    handlers = (FilterEventHandler)Delegate.Combine(handlers, value);
                }
                else
                {
                    handlers = value;
                }
                // Set the delegate as an uncommon field
                FilterHandlersField.SetValue(this, handlers);

                OnForwardedPropertyChanged();
            }
            remove
            {
                // Get existing event hanlders
                FilterEventHandler handlers = FilterHandlersField.GetValue(this);
                if (handlers != null)
                {
                    // Remove the given handler
                    handlers = (FilterEventHandler)Delegate.Remove(handlers, value);
                    if (handlers == null)
                    {
                        // Clear the value for the uncommon field
                        // cause there are no more handlers
                        FilterHandlersField.ClearValue(this);
                    }
                    else
                    {
                        // Set the remaining handlers as an uncommon field
                        FilterHandlersField.SetValue(this, handlers);
                    }
                }

                OnForwardedPropertyChanged();
            }
        }

        #endregion Public Events

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Return the default view for the given source.  This view is never
        /// affiliated with any CollectionViewSource.
        /// </summary>
        public static ICollectionView GetDefaultView(object source)
        {
            return GetOriginalView(GetDefaultCollectionView(source, true));
        }

        // a version of the previous method that doesn't create the view (bug 108595)
        private static ICollectionView LazyGetDefaultView(object source)
        {
            return GetOriginalView(GetDefaultCollectionView(source, false));
        }

        /// <summary>
        /// Return true if the given view is the default view for its source.
        /// </summary>
        public static bool IsDefaultView(ICollectionView view)
        {
            if (view != null)
            {
                object source = view.SourceCollection;
                return (GetOriginalView(view) == LazyGetDefaultView(source));
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Enter a Defer Cycle.
        /// Defer cycles are used to coalesce changes to the ICollectionView.
        /// </summary>
        public IDisposable DeferRefresh()
        {
            return new DeferHelper(this);
        }

        #endregion Public Methods

        //
        //  Interfaces
        //

        #region ISupportInitialize

        /// <summary>Signals the object that initialization is starting.</summary>
        void ISupportInitialize.BeginInit()
        {
            _isInitializing = true;
        }

        /// <summary>Signals the object that initialization is complete.</summary>
        void ISupportInitialize.EndInit()
        {
            _isInitializing = false;
            EnsureView();
        }

        #endregion ISupportInitialize

        #region IWeakEventListener

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return ReceiveWeakEvent(managerType, sender, e);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        #endregion IWeakEventListener

        #region Internal Properties

        //
        //  Internal Properties
        //

        // Returns the CollectionView currently affiliate with this CollectionViewSource.
        // This may be a CollectionViewProxy over the original view.
        internal CollectionView CollectionView
        {
            get
            {
                ICollectionView view = (ICollectionView)GetValue(ViewProperty);

                if (view != null && !_isViewInitialized)
                {
                    // leak prevention: re-fetch ViewRecord instead of keeping a reference to it,
                    // to be sure that we don't inadvertently keep it alive.
                    object source = Source;
                    DataSourceProvider dataProvider = source as DataSourceProvider;

                    // if the source is DataSourceProvider, use its Data instead
                    if (dataProvider != null)
                    {
                        source = dataProvider.Data;
                    }

                    if (source != null)
                    {
                        DataBindEngine engine = DataBindEngine.CurrentDataBindEngine;
                        ViewRecord viewRecord = engine.GetViewRecord(source, this, CollectionViewType, true, null);
                        if (viewRecord != null)
                        {
                            viewRecord.InitializeView();
                            _isViewInitialized = true;
                        }
                    }
                }

                return (CollectionView)view;
            }
        }

        // Returns the property through which inheritance context was established
        internal DependencyProperty PropertyForInheritanceContext
        {
            get { return _propertyForInheritanceContext; }
        }

        #endregion Internal Properties

        #region Internal Methods

        //
        //  Internal Methods
        //

        // Return the default view for the given source.  This view is never
        // affiliated with any CollectionViewSource.  It may be a
        // CollectionViewProxy over the original view
        static internal CollectionView GetDefaultCollectionView(object source, bool createView, Func<object, object> GetSourceItem=null)
        {
            if (!IsValidSourceForView(source))
                return null;

            DataBindEngine engine = DataBindEngine.CurrentDataBindEngine;
            ViewRecord viewRecord = engine.GetViewRecord(source, DefaultSource, null, createView, GetSourceItem);

            return (viewRecord != null) ? (CollectionView)viewRecord.View : null;
        }

        /// <summary>
        /// Return the default view for the given source.  This view is never
        /// affiliated with any CollectionViewSource.  The internal version sets
        /// the culture on the view from the xml:Lang of the host object.
        /// </summary>
        internal static CollectionView GetDefaultCollectionView(object source, DependencyObject d, Func<object, object> GetSourceItem=null)
        {
            CollectionView view = GetDefaultCollectionView(source, true, GetSourceItem);

            // at first use of a view, set its culture from the xml:lang of the
            // element that's using the view
            if (view != null && view.Culture == null)
            {
                XmlLanguage language = (d != null) ? (XmlLanguage)d.GetValue(FrameworkElement.LanguageProperty) : null;
                if (language != null)
                {
                    try
                    {
                        view.Culture = language.GetSpecificCulture();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            return view;
        }

        // Define the DO's inheritance context
        internal override DependencyObject InheritanceContext
        {
            get { return _inheritanceContext; }
        }

        // Receive a new inheritance context (this will be a FE/FCE)
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            // remember which property caused the context - BindingExpression wants to know
            // (this must happen before calling AddInheritanceContext, so that the answer
            // is ready during the InheritanceContextChanged event)
            if (!_hasMultipleInheritanceContexts && _inheritanceContext == null)
            {
                _propertyForInheritanceContext = property;
            }
            else
            {
                _propertyForInheritanceContext = null;
            }

            InheritanceContextHelper.AddInheritanceContext(context,
                                                              this,
                                                              ref _hasMultipleInheritanceContexts,
                                                              ref _inheritanceContext );
        }

        // Remove an inheritance context (this will be a FE/FCE)
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            InheritanceContextHelper.RemoveInheritanceContext(context,
                                                                  this,
                                                                  ref _hasMultipleInheritanceContexts,
                                                                  ref _inheritanceContext);

            // after removing a context, we don't know which property caused it
            _propertyForInheritanceContext = null;
        }

        // Says if the current instance has multiple InheritanceContexts
        internal override bool HasMultipleInheritanceContexts
        {
            get { return _hasMultipleInheritanceContexts; }
        }

        // Replace this by the extensible version (see remark in
        // OptimizedTemplateContent.ReadSharedRecord.
        internal bool IsShareableInTemplate()
        {
            return false;
        }

        #endregion Internal Methods

        #region Private Methods

        //
        //  Private Methods
        //

        // Obtain the view affiliated with the current source.  This may create
        // a new view, or re-use an existing one.
        void EnsureView()
        {
            EnsureView(Source, CollectionViewType);
        }

        void EnsureView(object source, Type collectionViewType)
        {
            if (_isInitializing || _deferLevel > 0)
                return;

            DataSourceProvider dataProvider = source as DataSourceProvider;

            // listen for DataChanged events from an DataSourceProvider
            if (dataProvider != _dataProvider)
            {
                if (_dataProvider != null)
                {
                    DataChangedEventManager.RemoveHandler(_dataProvider, OnDataChanged);
                }

                _dataProvider = dataProvider;

                if (_dataProvider != null)
                {
                    DataChangedEventManager.AddHandler(_dataProvider, OnDataChanged);
                    _dataProvider.InitialLoad();
                }
            }

            // if the source is DataSourceProvider, use its Data instead
            if (dataProvider != null)
            {
                source = dataProvider.Data;
            }

            // get the view
            ICollectionView view = null;

            if (source != null)
            {
                DataBindEngine engine = DataBindEngine.CurrentDataBindEngine;
                ViewRecord viewRecord = engine.GetViewRecord(source, this, collectionViewType, true,
                    (object x) =>
                    {
                        BindingExpressionBase beb = BindingOperations.GetBindingExpressionBase(this, SourceProperty);
                        return (beb != null) ? beb.GetSourceItem(x) : null;
                    });

                if (viewRecord != null)
                {
                    view = viewRecord.View;
                    _isViewInitialized = viewRecord.IsInitialized;

                    // bring view up to date with the CollectionViewSource
                    if (_version != viewRecord.Version)
                    {
                        ApplyPropertiesToView(view);
                        viewRecord.Version = _version;
                    }
                }
            }

            // update the View property
            SetValue(ViewPropertyKey, view);
        }

        // Forward properties from the CollectionViewSource to the CollectionView
        void ApplyPropertiesToView(ICollectionView view)
        {
            if (view == null || _deferLevel > 0)
                return;

            ICollectionViewLiveShaping liveView = view as ICollectionViewLiveShaping;

            using (view.DeferRefresh())
            {
                int i, n;

                // Culture
                if (Culture != null)
                {
                    view.Culture = Culture;
                }

                // Sort
                if (view.CanSort)
                {
                    view.SortDescriptions.Clear();
                    for (i=0, n=SortDescriptions.Count;  i < n;  ++i)
                    {
                        view.SortDescriptions.Add(SortDescriptions[i]);
                    }
                }
                else if (SortDescriptions.Count > 0)
                    throw new InvalidOperationException(SR.Get(SRID.CannotSortView, view));

                // Filter
                Predicate<object> filter;
                if (FilterHandlersField.GetValue(this) != null)
                {
                    filter = FilterWrapper;
                }
                else
                {
                    filter = null;
                }

                if (view.CanFilter)
                {
                    view.Filter = filter;
                }
                else if (filter != null)
                    throw new InvalidOperationException(SR.Get(SRID.CannotFilterView, view));

                // GroupBy
                if (view.CanGroup)
                {
                    view.GroupDescriptions.Clear();
                    for (i=0, n=GroupDescriptions.Count;  i < n;  ++i)
                    {
                        view.GroupDescriptions.Add(GroupDescriptions[i]);
                    }
                }
                else if (GroupDescriptions.Count > 0)
                    throw new InvalidOperationException(SR.Get(SRID.CannotGroupView, view));

                // Live shaping
                if (liveView != null)
                {
                    ObservableCollection<string> properties;

                    // sorting
                    if (liveView.CanChangeLiveSorting)
                    {
                        liveView.IsLiveSorting = IsLiveSortingRequested;
                        properties = liveView.LiveSortingProperties;
                        properties.Clear();

                        if (IsLiveSortingRequested)
                        {
                            foreach (string s in LiveSortingProperties)
                            {
                                properties.Add(s);
                            }
                        }
                    }

                    CanChangeLiveSorting = liveView.CanChangeLiveSorting;
                    IsLiveSorting = liveView.IsLiveSorting;

                    // filtering
                    if (liveView.CanChangeLiveFiltering)
                    {
                        liveView.IsLiveFiltering = IsLiveFilteringRequested;
                        properties = liveView.LiveFilteringProperties;
                        properties.Clear();

                        if (IsLiveFilteringRequested)
                        {
                            foreach (string s in LiveFilteringProperties)
                            {
                                properties.Add(s);
                            }
                        }
                    }

                    CanChangeLiveFiltering = liveView.CanChangeLiveFiltering;
                    IsLiveFiltering = liveView.IsLiveFiltering;

                    // grouping
                    if (liveView.CanChangeLiveGrouping)
                    {
                        liveView.IsLiveGrouping = IsLiveGroupingRequested;
                        properties = liveView.LiveGroupingProperties;
                        properties.Clear();

                        if (IsLiveGroupingRequested)
                        {
                            foreach (string s in LiveGroupingProperties)
                            {
                                properties.Add(s);
                            }
                        }
                    }

                    CanChangeLiveGrouping = liveView.CanChangeLiveGrouping;
                    IsLiveGrouping = liveView.IsLiveGrouping;
                }
                else
                {
                    CanChangeLiveSorting = false;
                    IsLiveSorting = null;
                    CanChangeLiveFiltering = false;
                    IsLiveFiltering = null;
                    CanChangeLiveGrouping = false;
                    IsLiveGrouping = null;
                }
            }
        }

        // return the original (un-proxied) view for the given view
        static ICollectionView GetOriginalView(ICollectionView view)
        {
            for (   CollectionViewProxy proxy = view as CollectionViewProxy;
                    proxy != null;
                    proxy = view as CollectionViewProxy)
            {
                view = proxy.ProxiedView;
            }

            return view;
        }

        Predicate<object> FilterWrapper
        {
            get
            {
                if (_filterStub == null)
                {
                    _filterStub = new FilterStub(this);
                }

                return _filterStub.FilterWrapper;
            }
        }

        bool WrapFilter(object item)
        {
            FilterEventArgs args = new FilterEventArgs(item);
            FilterEventHandler handlers = FilterHandlersField.GetValue(this);

            if (handlers != null)
            {
                handlers(this, args);
            }

            return args.Accepted;
        }

        void OnDataChanged(object sender, EventArgs e)
        {
            EnsureView();
        }

        // a change occurred in one of the collections that we forward to the view
        void OnForwardedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnForwardedPropertyChanged();
        }

        // a change occurred in one of the properties that we forward to the view
        void OnForwardedPropertyChanged()
        {
            // increment the version number.  This causes the change to get applied
            // to dormant views when they become active.
            unchecked {++ _version;}

            // apply the change to the current view
            ApplyPropertiesToView(View);
        }

        // defer changes
        void BeginDefer()
        {
            ++ _deferLevel;
        }

        void EndDefer()
        {
            if (--_deferLevel == 0)
            {
                EnsureView();
            }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 3; }
        }

        #endregion Private Methods

        #region Private Types

        //
        //  Private Types
        //

        private class DeferHelper : IDisposable
        {
            public DeferHelper(CollectionViewSource target)
            {
                _target = target;
                _target.BeginDefer();
            }

            public void Dispose()
            {
                if (_target != null)
                {
                    CollectionViewSource target = _target;
                    _target = null;
                    target.EndDefer();
                }
                GC.SuppressFinalize(this);
            }

            private CollectionViewSource _target;
        }

        // This class is used to break the reference chain from a collection
        // view to a UI element (typically Window or Page), created when the
        // app adds a handler (belonging to the Window or Page) to the Filter
        // event.  This class uses a weak reference to the CollectionViewSource
        // to break the chain and avoid a leak (bug 123012)
        private class FilterStub
        {
            public FilterStub(CollectionViewSource parent)
            {
                _parent = new WeakReference(parent);
                _filterWrapper = new Predicate<object>(WrapFilter);
            }

            public Predicate<object> FilterWrapper
            {
                get { return _filterWrapper; }
            }

            bool WrapFilter(object item)
            {
                CollectionViewSource parent = (CollectionViewSource)_parent.Target;
                if (parent != null)
                {
                    return parent.WrapFilter(item);
                }
                else
                {
                    return true;
                }
            }

            WeakReference _parent;
            Predicate<object> _filterWrapper;
        }

        #endregion Private Types

        #region Private Data

        //
        //  Private Data
        //

        // properties that get forwarded to the view
        CultureInfo                             _culture;
        SortDescriptionCollection               _sort;
        ObservableCollection<GroupDescription>  _groupBy;
        ObservableCollection<string>            _liveSortingProperties;
        ObservableCollection<string>            _liveFilteringProperties;
        ObservableCollection<string>            _liveGroupingProperties;

        // other state
        bool                _isInitializing;
        bool                _isViewInitialized; // view is initialized when it is first retrieved externally
        int                 _version;       // timestamp of last change to a forwarded property
        int                 _deferLevel;    // counts nested calls to BeginDefer
        DataSourceProvider  _dataProvider;  // DataSourceProvider whose DataChanged event we want
        FilterStub          _filterStub;    // used to support the Filter event

        // Fields to implement DO's inheritance context
        DependencyObject    _inheritanceContext;
        bool                _hasMultipleInheritanceContexts;
        DependencyProperty  _propertyForInheritanceContext;

        // the placeholder source for all default views
        internal static readonly CollectionViewSource DefaultSource = new CollectionViewSource();

        // This uncommon field is used to store the handlers for the Filter event
        private  static readonly UncommonField<FilterEventHandler> FilterHandlersField = new UncommonField<FilterEventHandler>();

        #endregion Private Data
    }
}

