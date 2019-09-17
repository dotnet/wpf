// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Dictionary that holds Resources for Framework components.
*
*
\***************************************************************************/
using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using System.Security;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Diagnostics;
using System.IO.Packaging;
using MS.Internal.IO.Packaging;         // for PackageCacheEntry
using System.Globalization;
using System.Windows.Navigation;

using MS.Internal;
using MS.Internal.Utility;
using MS.Internal.AppModel;
using MS.Utility;
using System.Xaml;
using System.Xaml.Permissions;
using System.Windows.Baml2006;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    ///     Dictionary that holds Resources for Framework components.
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    [Ambient]
    [UsableDuringInitialization(true)]
    public class ResourceDictionary : IDictionary, ISupportInitialize, System.Windows.Markup.IUriContext, System.Windows.Markup.INameScope
    {
        #region Constructor

        /// <summary>
        ///     Constructor for ResourceDictionary
        /// </summary>
        public ResourceDictionary()
        {
            _baseDictionary = new Hashtable();
            IsThemeDictionary = SystemResources.IsSystemResourcesParsing;
        }

        static ResourceDictionary()
        {
            DummyInheritanceContext.DetachFromDispatcher();
        }

        #endregion Constructor

        #region PublicAPIs

        /// <summary>
        ///     Copies the dictionary's elements to a one-dimensional
        ///     Array instance at the specified index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional Array that is the destination of the
        ///     DictionaryEntry objects copied from Hashtable. The Array
        ///     must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        ///     The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(DictionaryEntry[] array, int arrayIndex)
        {
            if (CanBeAccessedAcrossThreads)
            {
                lock(((ICollection)this).SyncRoot)
                {
                    CopyToWithoutLock(array, arrayIndex);
                }
            }
            else
            {
                CopyToWithoutLock(array, arrayIndex);
            }
        }

        private void CopyToWithoutLock(DictionaryEntry[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            _baseDictionary.CopyTo(array, arrayIndex);

            int length = arrayIndex + Count;
            for (int i = arrayIndex; i < length; i++)
            {
                DictionaryEntry entry = array[i];
                object value = entry.Value;
                bool canCache;
                OnGettingValuePrivate(entry.Key, ref value, out canCache);
                entry.Value = value; // refresh the entry value in case it was changed in the previous call
            }
        }

        ///<summary>
        ///     List of ResourceDictionaries merged into this Resource Dictionary
        ///</summary>
        public Collection<ResourceDictionary> MergedDictionaries
        {
            get
            {
                if (_mergedDictionaries == null)
                {
                    _mergedDictionaries = new ResourceDictionaryCollection(this);
                    _mergedDictionaries.CollectionChanged += OnMergedDictionariesChanged;
                }

                return _mergedDictionaries;
            }
        }

        ///<summary>
        ///     Uri to load this resource from, it will clear the current state of the ResourceDictionary
        ///</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Uri Source
        {
            get
            {
                return _source;
            }
            set
            {
                if (value == null || String.IsNullOrEmpty(value.OriginalString))
                {
                    throw new ArgumentException(SR.Get(SRID.ResourceDictionaryLoadFromFailure, value == null ? "''" : value.ToString()));
                }

                ResourceDictionaryDiagnostics.RemoveResourceDictionaryForUri(_source, this);

                ResourceDictionarySourceUriWrapper uriWrapper = value as ResourceDictionarySourceUriWrapper;

                Uri sourceUri;

                // If the Uri we received is a ResourceDictionarySourceUriWrapper it means
                // that it is being passed down by the Baml parsing code, and it is trying to give us more
                // information to avoid possible ambiguities in assembly resolving. Use the VersionedUri
                // to resolve, and the set _source to the OriginalUri so we don't change the return of Source property.
                // The versioned Uri is not stored, if the version info is needed while debugging, once this method 
                // returns _reader should be set, from there BamlSchemaContext.LocalAssembly contains the version info.
                if (uriWrapper == null)
                {
                    _source = value;
                    sourceUri = _source;
                }
                else
                {
                    _source = uriWrapper.OriginalUri;
                    sourceUri = uriWrapper.VersionedUri;
                }
                
                Clear();
                
                
                Uri uri = BindUriHelper.GetResolvedUri(_baseUri, sourceUri);

                WebRequest request = WpfWebRequestHelper.CreateRequest(uri);
                WpfWebRequestHelper.ConfigCachePolicy(request, false);
                ContentType contentType = null;
                Stream s = null;

                try
                {
                     s = WpfWebRequestHelper.GetResponseStream(request, out contentType);
                }
                catch (System.IO.IOException)
                {
                    if (IsSourcedFromThemeDictionary)
                    {
                        switch (_fallbackState)
                        {
                            case FallbackState.Classic:
                                {
                                    _fallbackState = FallbackState.Generic;
                                    Uri classicResourceUri = ThemeDictionaryExtension.GenerateFallbackUri(this, SystemResources.ClassicResourceName);
                                    Debug.Assert(classicResourceUri != null);

                                    Source = classicResourceUri;
                                    // After this recursive call has returned we are sure
                                    // that we have tried all fallback paths and so now
                                    // reset the _fallbackState
                                    _fallbackState = FallbackState.Classic;
                                }
                                break;
                            case FallbackState.Generic:
                                {
                                    _fallbackState = FallbackState.None;
                                    Uri genericResourceUri = ThemeDictionaryExtension.GenerateFallbackUri(this, SystemResources.GenericResourceName);

                                    Debug.Assert(genericResourceUri != null);
                                    Source = genericResourceUri;

                                }
                                break;
                        }
                        return;
                    }
                    else
                    {
                        throw;
                    }
                }

                // MimeObjectFactory.GetObjectAndCloseStream will try to find the object converter basing on the mime type.
                // It can be a sync/async converter. It's the converter's responsiblity to close the stream.
                // If it fails to find a convert, this call will return null.
                System.Windows.Markup.XamlReader asyncObjectConverter;
                ResourceDictionary loadedRD = MimeObjectFactory.GetObjectAndCloseStream(s, contentType, uri, false, false, false /*allowAsync*/, false /*isJournalNavigation*/, out asyncObjectConverter)
                                            as ResourceDictionary;

                if (loadedRD == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryLoadFromFailure, _source.ToString()));
                }

                // ReferenceCopy all the key-value pairs in the _baseDictionary
                _baseDictionary = loadedRD._baseDictionary;

                // ReferenceCopy all the entries in the MergedDictionaries collection
                _mergedDictionaries = loadedRD._mergedDictionaries;

                // ReferenceCopy all of the deferred content state
                CopyDeferredContentFrom(loadedRD);

                // Take over the deferred resource references
                MoveDeferredResourceReferencesFrom(loadedRD);

                // Copy over the HasImplicitStyles flag
                HasImplicitStyles = loadedRD.HasImplicitStyles;

                // Copy over the HasImplicitDataTemplates flag
                HasImplicitDataTemplates = loadedRD.HasImplicitDataTemplates;

                // Copy over the InvalidatesImplicitDataTemplateResources flag
                InvalidatesImplicitDataTemplateResources = loadedRD.InvalidatesImplicitDataTemplateResources;

                // Set inheritance context on the copied values
                if (InheritanceContext != null)
                {
                    AddInheritanceContextToValues();
                }

                // Propagate parent owners to each of the acquired merged dictionaries
                if (_mergedDictionaries != null)
                {
                    for (int i = 0; i < _mergedDictionaries.Count; i++)
                    {
                        PropagateParentOwners(_mergedDictionaries[i]);
                    }
                }

                ResourceDictionaryDiagnostics.AddResourceDictionaryForUri(uri, this);

                if (!IsInitializePending)
                {
                    // Fire Invalidations for the changes made by asigning a new Source
                    NotifyOwners(new ResourcesChangeInfo(null, this));
                }
            }
        }

        #region INameScope
        /// <summary>
        /// Registers the name - element combination
        /// </summary>
        /// <param name="name">name of the element</param>
        /// <param name="scopedElement">Element where name is defined</param>
        public void RegisterName(string name, object scopedElement)
        {
            throw new NotSupportedException(SR.Get(SRID.NamesNotSupportedInsideResourceDictionary));
        }

        /// <summary>
        /// Unregisters the name - element combination
        /// </summary>
        /// <param name="name">Name of the element</param>
        public void UnregisterName(string name)
        {
            // Do Nothing as Names cannot be registered on ResourceDictionary
        }

        /// <summary>
        /// Find the element given name
        /// </summary>
        /// <param name="name">Name of the element</param>
        /// <returns>null always</returns>
        public object FindName(string name)
        {
            return null;
        }

        #endregion INameScope

        #region IUriContext

        /// <summary>
        ///     Accessor for the base uri of the ResourceDictionary
        /// </summary>
        Uri System.Windows.Markup.IUriContext.BaseUri
        {
            get
            {
                return  _baseUri;
            }
            set
            {
                _baseUri = value;
            }
        }

        #endregion IUriContext

        #endregion PublicAPIs

        #region IDictionary

        /// <summary>
        ///     Gets a value indicating whether the IDictionary has a fixed size.
        /// </summary>
        public bool IsFixedSize
        {
            get { return _baseDictionary.IsFixedSize; }
        }

        /// <summary>
        ///     Gets a value indicating whether the ResourceDictionary is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return ReadPrivateFlag(PrivateFlags.IsReadOnly); }
            internal set
            {
                WritePrivateFlag(PrivateFlags.IsReadOnly, value);

                if (value == true)
                {
                    // Seal all the styles and templates in this dictionary
                    SealValues();
                }

                // Set all the merged resource dictionaries as ReadOnly
                if (_mergedDictionaries != null)
                {
                    for (int i = 0; i < _mergedDictionaries.Count; i++)
                    {
                        _mergedDictionaries[i].IsReadOnly = value;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the invalidations fired
        ///     by the ResourceDictionary when an implicit data template resource
        ///     changes will cause ContentPresenters to re-evaluate their choice
        ///     of template.
        /// </summary>
        [DefaultValue(false)]
        public bool InvalidatesImplicitDataTemplateResources
        {
            get { return ReadPrivateFlag(PrivateFlags.InvalidatesImplicitDataTemplateResources); }
            set { WritePrivateFlag(PrivateFlags.InvalidatesImplicitDataTemplateResources, value); }
        }

        /// <summary>
        ///     Gets or sets the value associated with the specified key.
        /// </summary>
        /// <remarks>
        ///     Fire Invalidations only for changes made after the Init Phase
        ///     If the key is not found on this ResourceDictionary, it will look on any MergedDictionaries for it
        /// </remarks>
        public object this[object key]
        {
            get
            {
                bool canCache;
                return GetValue(key, out canCache);
            }

            set
            {
                // Seal styles and templates within App and Theme dictionary
                SealValue(value);

                if (CanBeAccessedAcrossThreads)
                {
                    lock(((ICollection)this).SyncRoot)
                    {
                        SetValueWithoutLock(key, value);
                    }
                }
                else
                {
                    SetValueWithoutLock(key, value);
                }
            }
        }

        // This should only be called in the deferred BAML loading scenario.  We
        // cache all the data that we need away and then get rid of the actual object.
        // No one needs to actually get this property so we're returning null.  This
        // property has to be public since the XAML parser cannot set this internal
        // property in this scenario.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DeferrableContent DeferrableContent
        {
            get
            {
                return null;
            }
            set
            {
                this.SetDeferrableContent(value);
            }
        }

        private void SetValueWithoutLock(object key, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryIsReadOnly));
            }

            object oldValue = _baseDictionary[key];

            if (oldValue != value)
            {
                // We need to validate all the deferred references that refer
                // to the old resource before we overwrite it.
                ValidateDeferredResourceReferences(key);

                if( TraceResourceDictionary.IsEnabled )
                {
                    TraceResourceDictionary.Trace( TraceEventType.Start,
                                                   TraceResourceDictionary.AddResource,
                                                   this,
                                                   key,
                                                   value );
                }


                _baseDictionary[key] = value;

                // Update the HasImplicitStyles flag
                UpdateHasImplicitStyles(key);

                // Update the HasImplicitDataTemplates flag
                UpdateHasImplicitDataTemplates(key);

                // Notify owners of the change and fire invalidate if already initialized
                NotifyOwners(new ResourcesChangeInfo(key));

                if( TraceResourceDictionary.IsEnabled )
                {
                    TraceResourceDictionary.Trace(
                                                  TraceEventType.Stop,
                                                  TraceResourceDictionary.AddResource,
                                                  this,
                                                  key,
                                                  value );
                }


            }
        }

        internal object GetValue(object key, out bool canCache)
        {
            if (CanBeAccessedAcrossThreads)
            {
                lock(((ICollection)this).SyncRoot)
                {
                    return GetValueWithoutLock(key, out canCache);
                }
            }
            else
            {
                return GetValueWithoutLock(key, out canCache);
            }
        }

        private object GetValueWithoutLock(object key, out bool canCache)
        {
            object value = _baseDictionary[key];
            if (value != null)
            {
                OnGettingValuePrivate(key, ref value, out canCache);
            }
            else
            {
                canCache = true;

                //Search for the value in the Merged Dictionaries
                if (_mergedDictionaries != null)
                {
                    for (int i = MergedDictionaries.Count - 1; (i > -1); i--)
                    {
                        // Note that MergedDictionaries collection can also contain null values
                        ResourceDictionary mergedDictionary = MergedDictionaries[i];
                        if (mergedDictionary != null)
                        {
                            value = mergedDictionary.GetValue(key, out canCache);
                            if (value != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return value;
        }

        // Gets the type of the value stored at the given key
        internal Type GetValueType(object key, out bool found)
        {
            found = false;
            Type valueType = null;

            object value = _baseDictionary[key];
            if (value != null)
            {
                found = true;

                KeyRecord keyRecord = value as KeyRecord;
                if (keyRecord != null)
                {
                    Debug.Assert(_numDefer > 0, "The stream was closed before all deferred content was loaded.");
                    valueType = GetTypeOfFirstObject(keyRecord);
                }
                else
                {
                    valueType = value.GetType();
                }

            }
            else
            {
                // Search for the value in the Merged Dictionaries
                if (_mergedDictionaries != null)
                {
                    for (int i = MergedDictionaries.Count - 1; (i > -1); i--)
                    {
                        // Note that MergedDictionaries collection can also contain null values
                        ResourceDictionary mergedDictionary = MergedDictionaries[i];
                        if (mergedDictionary != null)
                        {
                            valueType = mergedDictionary.GetValueType(key, out found);
                            if (found)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return valueType;
        }

        /// <summary>
        ///     Gets a copy of the ICollection containing the keys of the IDictionary.
        /// </summary>
        public ICollection Keys
        {
            get
            {
                object[] keysCollection = new object[Count];
                _baseDictionary.Keys.CopyTo(keysCollection, 0);
                return keysCollection;
            }
        }

        /// <summary>
        ///     Gets an ICollection containing the values in the Hashtable
        /// </summary>
        /// <value>An ICollection containing the values in the Hashtable</value>
        public ICollection Values
        {
            get
            {
                return new ResourceValuesCollection(this);
            }
        }

        /// <summary>
        ///     Adds an entry
        /// </summary>
        /// <remarks>
        ///     Fire Invalidations only for changes made after the Init Phase
        /// </remarks>
        public void Add(object key, object value)
        {
            // Seal styles and templates within App and Theme dictionary
            SealValue(value);

            if (CanBeAccessedAcrossThreads)
            {
                lock(((ICollection)this).SyncRoot)
                {
                    AddWithoutLock(key, value);
                }
            }
            else
            {
                AddWithoutLock(key, value);
            }

        }

        private void AddWithoutLock(object key, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryIsReadOnly));
            }

            // invalid during a VisualTreeChanged event
            System.Windows.Diagnostics.VisualDiagnostics.VerifyVisualTreeChange(InheritanceContext);

            if( TraceResourceDictionary.IsEnabled )
            {
                TraceResourceDictionary.Trace( TraceEventType.Start,
                                               TraceResourceDictionary.AddResource,
                                               this,
                                               key,
                                               value );
            }


            _baseDictionary.Add(key, value);

            // Update the HasImplicitKey flag
            UpdateHasImplicitStyles(key);

            // Update the HasImplicitDataTemplates flag
            UpdateHasImplicitDataTemplates(key);

            // Notify owners of the change and fire invalidate if already initialized
            NotifyOwners(new ResourcesChangeInfo(key));

            if( TraceResourceDictionary.IsEnabled )
            {
                TraceResourceDictionary.Trace( TraceEventType.Stop,
                                               TraceResourceDictionary.AddResource,
                                               this,
                                               key,
                                               value );
            }

        }

        /// <summary>
        ///     Removes all elements from the IDictionary.
        /// </summary>
        public void Clear()
        {
            if (CanBeAccessedAcrossThreads)
            {
                lock(((ICollection)this).SyncRoot)
                {
                    ClearWithoutLock();
                }
            }
            else
            {
                ClearWithoutLock();
            }
        }

        private void ClearWithoutLock()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryIsReadOnly));
            }

            // invalid during a VisualTreeChanged event
            System.Windows.Diagnostics.VisualDiagnostics.VerifyVisualTreeChange(InheritanceContext);

            if (Count > 0)
            {
                // We need to validate all the deferred references that refer
                // to the old resource before we clear it.
                ValidateDeferredResourceReferences(null);

                // remove inheritance context from all values that got it from
                // this dictionary
                RemoveInheritanceContextFromValues();

                _baseDictionary.Clear();

                // Notify owners of the change and fire invalidate if already initialized
                NotifyOwners(ResourcesChangeInfo.CatastrophicDictionaryChangeInfo);
            }
        }

        /// <summary>
        ///     Determines whether the IDictionary contains an element with the specified key.
        ///     if the Key is not contained in this ResourceDictionary, it will check in the MergedDictionaries too
        /// </summary>
        public bool Contains(object key)
        {
            bool result = _baseDictionary.Contains(key);

            if (result)
            {
                KeyRecord keyRecord = _baseDictionary[key] as KeyRecord;
                if (keyRecord != null && _deferredLocationList.Contains(keyRecord))
                {
                    return false;
                }
            }

            //Search for the value in the Merged Dictionaries
            if (_mergedDictionaries != null)
            {
                for (int i = MergedDictionaries.Count - 1; (i > -1) && !result; i--)
                {
                    // Note that MergedDictionaries collection can also contain null values
                    ResourceDictionary mergedDictionary = MergedDictionaries[i];
                    if (mergedDictionary != null)
                    {
                        result = mergedDictionary.Contains(key);
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Determines whether the IDictionary contains a BamlObjectFactory against the specified key.
        ///     if the Key is not contained in this ResourceDictionary, it will check in the MergedDictionaries too
        /// </summary>
        private bool ContainsBamlObjectFactory(object key)
        {
            return GetBamlObjectFactory(key) != null;
        }

        /// <summary>
        ///     Retrieves a KeyRecord from the IDictionary using the specified key.
        ///     If the Key is not contained in this ResourceDictionary, it will check in the MergedDictionaries too
        /// </summary>
        private KeyRecord GetBamlObjectFactory(object key)
        {
            if (_baseDictionary.Contains(key))
            {
                return _baseDictionary[key] as KeyRecord;
            }

            //Search for the value in the Merged Dictionaries
            if (_mergedDictionaries != null)
            {
                for (int i = MergedDictionaries.Count - 1; i > -1; i--)
                {
                    // Note that MergedDictionaries collection can also contain null values
                    ResourceDictionary mergedDictionary = MergedDictionaries[i];
                    if (mergedDictionary != null)
                    {
                        KeyRecord keyRecord = mergedDictionary.GetBamlObjectFactory(key);
                        if (keyRecord != null)
                        {
                            return keyRecord;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Returns an IDictionaryEnumerator that can iterate through the Hashtable
        /// </summary>
        /// <returns>An IDictionaryEnumerator for the Hashtable</returns>
        public IDictionaryEnumerator GetEnumerator()
        {
            return new ResourceDictionaryEnumerator(this);
        }

        /// <summary>
        ///     Removes an entry
        /// </summary>
        /// <remarks>
        ///     Fire Invalidations only for changes made after the Init Phase
        /// </remarks>
        public void Remove(object key)
        {
            if (CanBeAccessedAcrossThreads)
            {
                lock(((ICollection)this).SyncRoot)
                {
                    RemoveWithoutLock(key);
                }
            }
            else
            {
                RemoveWithoutLock(key);
            }
        }

        private void RemoveWithoutLock(object key)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryIsReadOnly));
            }

            // invalid during a VisualTreeChanged event
            System.Windows.Diagnostics.VisualDiagnostics.VerifyVisualTreeChange(InheritanceContext);

            // We need to validate all the deferred references that refer
            // to the old resource before we remove it.
            ValidateDeferredResourceReferences(key);

            // remove the inheritance context from the value, if it came from
            // this dictionary
            RemoveInheritanceContext(_baseDictionary[key]);

            _baseDictionary.Remove(key);

            // Notify owners of the change and fire invalidate if already initialized
            NotifyOwners(new ResourcesChangeInfo(key));
        }

        #endregion IDictionary

        #region ICollection

        /// <summary>
        ///     Gets the number of elements contained in the ICollection.
        /// </summary>
        public int Count
        {
            get { return _baseDictionary.Count; }
        }

        /// <summary>
        ///     Gets a value indicating whether access to the ICollection is synchronized (thread-safe).
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return _baseDictionary.IsSynchronized; }
        }

        /// <summary>
        ///     Gets an object that can be used to synchronize access to the ICollection.
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (CanBeAccessedAcrossThreads)
                {
                    // Notice that we are acquiring the ThemeDictionaryLock. This
                    // is because the _parserContext used for template expansion
                    // shares data-structures such as the BamlMapTable and
                    // XamlTypeMapper with the parent ParserContext that was used
                    // to build the template in the first place. So if this template
                    // is from the App.Resources then the ParserContext that is used for
                    // loading deferred content in the app dictionary and the
                    // _parserContext used to load template content share the same
                    // instances of BamlMapTable and XamlTypeMapper. Hence we need to
                    // make sure that we lock on the same object inorder to serialize
                    // access to these data-structures in multi-threaded scenarios.
                    // Look at comment in Frameworktemplate.LoadContent to understand
                    // why we use the ThemeDictionaryLock for template expansion.

                    return SystemResources.ThemeDictionaryLock;
                }
                else
                {
                    return _baseDictionary.SyncRoot;
                }
            }
        }

        /// <summary>
        ///     Copies the dictionary's elements to a one-dimensional
        ///     Array instance at the specified index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional Array that is the destination of the
        ///     DictionaryEntry objects copied from Hashtable. The Array
        ///     must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        ///     The zero-based index in array at which copying begins.
        /// </param>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            CopyTo(array as DictionaryEntry[], arrayIndex);
        }

        #endregion ICollection

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary)this).GetEnumerator();
        }

        #endregion IEnumerable

        #region ISupportInitialize

        /// <summary>
        ///     Mark the begining of the Init phase
        /// </summary>
        /// <remarks>
        ///     BeginInit and EndInit follow a transaction model. BeginInit marks the
        ///     dictionary uninitialized and EndInit marks it initialized.
        /// </remarks>
        public void BeginInit()
        {
            // Nested BeginInits on the same instance aren't permitted
            if (IsInitializePending)
            {
                throw new InvalidOperationException(SR.Get(SRID.NestedBeginInitNotSupported));
            }

            IsInitializePending = true;
            IsInitialized = false;
        }

        /// <summary>
        ///     Fire Invalidation at the end of Init phase
        /// </summary>
        /// <remarks>
        ///     BeginInit and EndInit follow a transaction model. BeginInit marks the
        ///     dictionary uninitialized and EndInit marks it initialized.
        /// </remarks>
        public void EndInit()
        {
            // EndInit without a BeginInit isn't permitted
            if (!IsInitializePending)
            {
                throw new InvalidOperationException(SR.Get(SRID.EndInitWithoutBeginInitNotSupported));
            }
            Debug.Assert(IsInitialized == false, "Dictionary should not be initialized when EndInit is called");

            IsInitializePending = false;
            IsInitialized = true;

            // Fire Invalidations collectively for all changes made during the Init Phase
            NotifyOwners(new ResourcesChangeInfo(null, this));
        }

        #endregion ISupportInitialize

        #region DeferContent

        private bool CanCache(KeyRecord keyRecord, object value)
        {
            if (keyRecord.SharedSet)
            {
                return keyRecord.Shared;
            }
            else
            {
                return true;
            }
        }

        private void OnGettingValuePrivate(object key, ref object value, out bool canCache)
        {
            // diagnostic agent may want to know when a StaticResource reference
            // resolves.  Do this before calling out to OnGettingValue, as that
            // can inflate deferred content and cause nested requests
            ResourceDictionaryDiagnostics.RecordLookupResult(key, this);

            OnGettingValue(key, ref value, out canCache);

            if (key != null && canCache)
            {
                if (!Object.Equals(_baseDictionary[key], value))
                {
                    // cache the revised value, after setting its InheritanceContext
                    if (InheritanceContext != null)
                    {
                        AddInheritanceContext(InheritanceContext, value);
                    }

                    _baseDictionary[key] = value;
                }
            }
        }

        protected virtual void OnGettingValue(object key, ref object value, out bool canCache)
        {
            KeyRecord keyRecord = value as KeyRecord;

            // If the value is not a key record then
            // it has already been realized, is not deferred and is a "ready to go" value.
            if (keyRecord == null)
            {
                canCache = true;
                return;   /* Not deferred content */
            }

            Debug.Assert(_numDefer > 0, "The stream was closed before all deferred content was loaded.");

            // We want to return null if a resource asks for itself. It should return null
            //  <Style x:Key={x:Type Button} BasedOn={StaticResource {x:Type Button}}/> should not find itself
            if (_deferredLocationList.Contains(keyRecord))
            {
                canCache = false;
                value = null;
                return; /* Not defered content */
            }

            _deferredLocationList.Add(keyRecord);

            try
            {
                if (TraceResourceDictionary.IsEnabled)
                {
                    TraceResourceDictionary.Trace(
                        TraceEventType.Start,
                        TraceResourceDictionary.RealizeDeferContent,
                        this,
                        key,
                        value);
                }

                value = CreateObject(keyRecord);

            }
            finally
            {
                if (TraceResourceDictionary.IsEnabled)
                {
                    TraceResourceDictionary.Trace(
                        TraceEventType.Stop,
                        TraceResourceDictionary.RealizeDeferContent,
                        this,
                        key,
                        value);
                }

            }

            _deferredLocationList.Remove(keyRecord);

            if (key != null)
            {
                canCache = CanCache(keyRecord, value);
                if (canCache)
                {
                    // Seal styles and templates within App and Theme dictionary
                    SealValue(value);

                    _numDefer--;

                    if (_numDefer == 0)
                    {
                            CloseReader();
                    }
                }
            }
            else
            {
                canCache = true;
            }
        }

        /// <summary>
        ///  Add a byte array that contains deferable content
        /// </summary>
        private void SetDeferrableContent(DeferrableContent deferrableContent)
        {
            Debug.Assert(deferrableContent.Stream != null);
            Debug.Assert(deferrableContent.SchemaContext != null);
            Debug.Assert(deferrableContent.ObjectWriterFactory != null);
            Debug.Assert(deferrableContent.ServiceProvider != null);
            Debug.Assert(deferrableContent.RootObject != null);

            Baml2006ReaderSettings settings = new Baml2006ReaderSettings(deferrableContent.SchemaContext.Settings);
            settings.IsBamlFragment = true;
            settings.OwnsStream = true;
            settings.BaseUri = null;    // Base URI can only be set on the root object, not on deferred content.

            Baml2006Reader reader = new Baml2006Reader(deferrableContent.Stream,
                deferrableContent.SchemaContext, settings);
            _objectWriterFactory = deferrableContent.ObjectWriterFactory;
            _objectWriterSettings = deferrableContent.ObjectWriterParentSettings;
            _deferredLocationList = new List<KeyRecord>();
            _rootElement = deferrableContent.RootObject;

            IList<KeyRecord> keys = reader.ReadKeys();

            // If we already have the Source set then we can ignore
            // this deferable content section
            if (_source == null)
            {
                if (_reader == null)
                {
                    _reader = reader;
                    SetKeys(keys, deferrableContent.ServiceProvider);
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryDuplicateDeferredContent));
                }
            }
            else if (keys.Count > 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryDeferredContentFailure));
            }
        }

        private object GetKeyValue(KeyRecord key, IServiceProvider serviceProvider)
        {
            if (key.KeyString != null)
            {
                return key.KeyString;
            }
            else if (key.KeyType != null)
            {
                return key.KeyType;
            }
            else
            {
                System.Xaml.XamlReader reader = key.KeyNodeList.GetReader();
                object value = EvaluateMarkupExtensionNodeList(reader, serviceProvider);
                return value;
            }
        }

        private object EvaluateMarkupExtensionNodeList(System.Xaml.XamlReader reader, IServiceProvider serviceProvider)
        {
            System.Xaml.XamlObjectWriter writer = _objectWriterFactory.GetXamlObjectWriter(null);

            System.Xaml.XamlServices.Transform(reader, writer);

            object value = writer.Result;
            MarkupExtension me = value as MarkupExtension;
            if (me != null)
            {
                value = me.ProvideValue(serviceProvider);
            }
            return value;
        }

        private object GetStaticResourceKeyValue(StaticResource staticResource, IServiceProvider serviceProvider)
        {
            System.Xaml.XamlReader reader = staticResource.ResourceNodeList.GetReader();
            XamlType xamlTypeStaticResourceExtension = reader.SchemaContext.GetXamlType(typeof(StaticResourceExtension));
            XamlMember xamlMemberResourceKey = xamlTypeStaticResourceExtension.GetMember("ResourceKey");
            reader.Read();
            if (reader.NodeType == Xaml.XamlNodeType.StartObject && reader.Type == xamlTypeStaticResourceExtension)
            {
                reader.Read();
                // Skip Members that aren't _PositionalParameters or ResourceKey
                while (reader.NodeType == Xaml.XamlNodeType.StartMember &&
                    (reader.Member != XamlLanguage.PositionalParameters && reader.Member != xamlMemberResourceKey))
                {
                    reader.Skip();
                }

                // Process the Member Value of _PositionParameters or ResourceKey
                if (reader.NodeType == Xaml.XamlNodeType.StartMember)
                {
                    object value = null;
                    reader.Read();
                    if (reader.NodeType == Xaml.XamlNodeType.StartObject)
                    {
                        System.Xaml.XamlReader subReader = reader.ReadSubtree();

                        value = EvaluateMarkupExtensionNodeList(subReader, serviceProvider);
                    }
                    else if (reader.NodeType == Xaml.XamlNodeType.Value)
                    {
                        value = reader.Value;
                    }
                    return value;
                }
            }
            return null;
        }

        private void SetKeys(IList<KeyRecord> keyCollection, IServiceProvider serviceProvider)
        {
            _numDefer = keyCollection.Count;

            // Allocate one StaticResourceExtension object to use as a "worker".
            StaticResourceExtension staticResourceWorker = new StaticResourceExtension();

            // Use the array Count property to avoid range checking inside the loop
            for (int i = 0; i < keyCollection.Count; i++)
            {
                KeyRecord keyRecord = keyCollection[i];
                if (keyRecord != null)
                {
                    object value = GetKeyValue(keyRecord, serviceProvider);

                    // Update the HasImplicitStyles flag
                    UpdateHasImplicitStyles(value);

                    // Update the HasImplicitDataTemplates flag
                    UpdateHasImplicitDataTemplates(value);

                    if (keyRecord != null && keyRecord.HasStaticResources)
                    {
                        SetOptimizedStaticResources(keyRecord.StaticResources, serviceProvider, staticResourceWorker);
                    }

                    _baseDictionary.Add(value, keyRecord);

                    if (TraceResourceDictionary.IsEnabled)
                    {
                        TraceResourceDictionary.TraceActivityItem(
                            TraceResourceDictionary.SetKey,
                            this,
                            value);
                    }

                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.KeyCollectionHasInvalidKey));
                }
            }

            // Notify owners of the HasImplicitStyles flag value
            // but there is not need to fire an invalidation.
            NotifyOwners(new ResourcesChangeInfo(null, this));
        }

        /// <summary>
        /// Convert the OptimizedStaticResource and StaticResource items into StaticResourceHolders.
        /// A StaticResourceHolder is derived from StaticResourceExtension and is a MarkupExtension.
        /// The differences is that it contain a DeferredResourceReference as its "PrefetchedValue".
        /// DeferredResourceReferences hold the dictionary and the key of the resource.  It is a
        /// way of looking up the reference now (for later use) but not expanding the entry.
        /// Also the dictionary has a reference back to the Deferrred Reference.  If dictionary entry
        /// is modifed the DeferredResourceReference is told and it will grab the old value.
        /// StaticResourceHolder is a MarkupExtension and thus can be returned as a "Value" in the Node Stream.
        ///
        /// Issue:  If there is a ResourceDictionary inside the deferred entry, the entries inside that
        /// RD will not be evaluated when resolving DeferredResourceReferences for a key in that same entry.
        /// Thus the OptimizedStaticResource will either be erronously "not found" or may even map to some
        /// incorrect value higher in the parse tree.  So... In StaticResourceExtension.ProvideValue()
        /// when we have a DeferredResourceReference we search the Deferred Content for a better
        /// closer value before using the DeferredReference.
        /// See StaticResourceExtension.FindTheResourceDictionary() for more details.
        /// </summary>

        // As a memory optimization this method is passed a staticResourceExtension instance to use as
        // a worker when calling TryProvideValueInternal, which saves us having to allocate on every call.
        private void SetOptimizedStaticResources(IList<object> staticResources, IServiceProvider serviceProvider, StaticResourceExtension staticResourceWorker)
        {
            Debug.Assert(staticResources != null && staticResources.Count > 0);
            for (int i = 0; i < staticResources.Count; i++)
            {
                object keyValue = null;

                // Process OptimizedStaticResource
                var optimizedStaticResource = staticResources[i] as OptimizedStaticResource;
                if (optimizedStaticResource != null)
                {
                    keyValue = optimizedStaticResource.KeyValue;
                }
                else
                {
                    // Process StaticResource  (it holds the NodeList of the StaticResourceExtension)
                    var staticResource = staticResources[i] as StaticResource;
                    if (staticResource != null)
                    {
                        // find and evaluate the Key value of the SR in the SR's node stream.
                        keyValue = GetStaticResourceKeyValue(staticResource, serviceProvider);
                        Debug.Assert(keyValue != null, "Didn't find the ResourceKey property or x:PositionalParameters directive");
                    }
                    else
                    {
                        Debug.Assert(false, "StaticResources[] entry is not a StaticResource not OptimizedStaticResource");
                        continue;  // other types of entries are not processed.
                    }
                }

                // Lookup the Key in the current context.  [And return a Deferred Reference Holding SR to it]
                // The current context is the Key table at the top of the Compiled Dictionary.
                // We will look at keys above us in this dictionary and in the dictionaries in objects above
                // us on the parse stack.  And then look in the App and System Themems.
                // This isn't always good enough.  The Static Resource referenced inside the entry may refer
                // to a entry in a sub-dictionary inside the deferred entry.   There is other code, later
                // when evaluating StaticResourceHolders, that does an search of the part that is missed here.
                staticResourceWorker.ResourceKey = keyValue;
                object obj = staticResourceWorker.TryProvideValueInternal(serviceProvider, true /*allowDeferredReference*/, true /* mustReturnDeferredResourceReference */);

                Debug.Assert(obj is DeferredResourceReference);
                staticResources[i] = new StaticResourceHolder(keyValue, obj as DeferredResourceReference);
            }
        }

#if false
        // need to call this (and do similar for Template) to cache RDs.

        private void SetStaticResources(object[] staticResourceValues, ParserContext context)
        {
            if (staticResourceValues != null && staticResourceValues.Length > 0)
            {
                bool inDeferredSection = context.InDeferredSection;

                for (int i=0; i<staticResourceValues.Length; i++)
                {
                    // If this dictionary is a top level deferred section then we lookup the parser stack
                    // and then look in the app and theme dictionaries to resolve the current static resource.

                    if (!inDeferredSection)
                    {
                        staticResourceValues[i] = context.BamlReader.FindResourceInParentChain(
                            ((StaticResourceExtension)staticResourceValues[i]).ResourceKey,
                            true /*allowDeferredResourceReference*/,
                            true /*mustReturnDeferredResourceReference*/);
                    }
                    else
                    {
                        // If this dictionary is nested within another deferred section then we try to
                        // resolve the current staticresource against the parser stack and if not
                        // able to resolve then we fallback to the pre-fetched value from the outer
                        // deferred section.

                        StaticResourceHolder srHolder = (StaticResourceHolder)staticResourceValues[i];

                        object value = context.BamlReader.FindResourceInParserStack(
                            srHolder.ResourceKey,
                            true /*allowDeferredResourceReference*/,
                            true /*mustReturnDeferredResourceReference*/);

                        if (value == DependencyProperty.UnsetValue)
                        {
                            // If value wan't found the fallback
                            // to the prefetched value

                            value = srHolder.PrefetchedValue;
                        }

                        staticResourceValues[i] = value;
                    }
                }
            }
        }
#endif
        private Type GetTypeOfFirstObject(KeyRecord keyRecord)
        {
            Type rootType = _reader.GetTypeOfFirstStartObject(keyRecord);
            return rootType ?? typeof(String);
        }

        private object CreateObject(KeyRecord key)
        {
            System.Xaml.XamlReader xamlReader = _reader.ReadObject(key);
            // v3 Markup Compiler will occasionally produce deferred
            // content with keys but no values.  We need to allow returning
            // null in this scenario to match v3 compat and not throw an
            // error.
            if (xamlReader == null)
                return null;

            Uri baseUri = (_rootElement is IUriContext) ? ((IUriContext)_rootElement).BaseUri : _baseUri;
            return WpfXamlLoader.LoadDeferredContent(
                    xamlReader, _objectWriterFactory, false /*skipJournaledProperites*/,
                    _rootElement, _objectWriterSettings, baseUri);
        }

        // Moved "Lookup()" from 3.5 BamlRecordReader to 4.0 ResourceDictionary
        internal object Lookup(object key, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference, bool canCacheAsThemeResource)
        {
            if (allowDeferredResourceReference)
            {
                // Attempt to delay load resources from ResourceDictionaries
                bool canCache;
                return FetchResource(key, allowDeferredResourceReference, mustReturnDeferredResourceReference, canCacheAsThemeResource, out canCache);
            }
            else
            {
                if (!mustReturnDeferredResourceReference)
                {
                    return this[key];
                }
                else
                {
                    return new DeferredResourceReferenceHolder(key, this[key]);
                }
            }
        }

        #endregion DeferContent

        #region HelperMethods

        // Add an owner for this dictionary
        internal void AddOwner(DispatcherObject owner)
        {
            if (_inheritanceContext == null)
            {
                // the first owner gets to be the InheritanceContext for
                // all the values in the dictionary that want one.
                DependencyObject inheritanceContext = owner as DependencyObject;

                if (inheritanceContext != null)
                {
                    _inheritanceContext = new WeakReference(inheritanceContext);

                    // set InheritanceContext for the existing values
                    AddInheritanceContextToValues();
                }
                else
                {
                    // if the first owner is ineligible, use a dummy
                    _inheritanceContext = new WeakReference(DummyInheritanceContext);

                    // do not call AddInheritanceContextToValues -
                    // the owner is an Application, and we'll be
                    // calling SealValues soon, which takes care
                    // of InheritanceContext as well
                }

            }

            FrameworkElement fe = owner as FrameworkElement;
            if (fe != null)
            {
                if (_ownerFEs == null)
                {
                    _ownerFEs = new WeakReferenceList(1);
                }
                else if (_ownerFEs.Contains(fe) && ContainsCycle(this))
                {
                    throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryInvalidMergedDictionary));
                }

                // Propagate the HasImplicitStyles flag to the new owner
                if (HasImplicitStyles)
                {
                    fe.ShouldLookupImplicitStyles = true;
                }

                _ownerFEs.Add(fe);
            }
            else
            {
                FrameworkContentElement fce = owner as FrameworkContentElement;
                if (fce != null)
                {
                    if (_ownerFCEs == null)
                    {
                        _ownerFCEs = new WeakReferenceList(1);
                    }
                    else if (_ownerFCEs.Contains(fce) && ContainsCycle(this))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryInvalidMergedDictionary));
                    }

                    // Propagate the HasImplicitStyles flag to the new owner
                    if (HasImplicitStyles)
                    {
                        fce.ShouldLookupImplicitStyles = true;
                    }

                    _ownerFCEs.Add(fce);
                }
                else
                {
                    Application app = owner as Application;
                    if (app != null)
                    {
                        if (_ownerApps == null)
                        {
                            _ownerApps = new WeakReferenceList(1);
                        }
                        else if (_ownerApps.Contains(app) && ContainsCycle(this))
                        {
                            throw new InvalidOperationException(SR.Get(SRID.ResourceDictionaryInvalidMergedDictionary));
                        }

                        // Propagate the HasImplicitStyles flag to the new owner
                        if (HasImplicitStyles)
                        {
                            app.HasImplicitStylesInResources = true;
                        }

                        _ownerApps.Add(app);

                        // An Application ResourceDictionary can be accessed across threads
                        CanBeAccessedAcrossThreads = true;

                        // Seal all the styles and templates in this app dictionary
                        SealValues();
                    }
                }
            }

            AddOwnerToAllMergedDictionaries(owner);

            // This dictionary will be marked initialized if no one has called BeginInit on it.
            // This is done now because having an owner is like a parenting operation for the dictionary.
            TryInitialize();
        }

        // Remove an owner for this dictionary
        internal void RemoveOwner(DispatcherObject owner)
        {
            FrameworkElement fe = owner as FrameworkElement;
            if (fe != null)
            {
                if (_ownerFEs != null)
                {
                    _ownerFEs.Remove(fe);

                    if (_ownerFEs.Count == 0)
                    {
                        _ownerFEs = null;
                    }
                }
            }
            else
            {
                FrameworkContentElement fce = owner as FrameworkContentElement;
                if (fce != null)
                {
                    if (_ownerFCEs != null)
                    {
                        _ownerFCEs.Remove(fce);

                        if (_ownerFCEs.Count == 0)
                        {
                            _ownerFCEs = null;
                        }
                    }
                }
                else
                {
                    Application app = owner as Application;
                    if (app != null)
                    {
                        if (_ownerApps != null)
                        {
                            _ownerApps.Remove(app);

                            if (_ownerApps.Count == 0)
                            {
                                _ownerApps = null;
                            }
                        }
                    }
                }
            }

            if (owner == InheritanceContext)
            {
                RemoveInheritanceContextFromValues();
                _inheritanceContext = null;
            }

            RemoveOwnerFromAllMergedDictionaries(owner);
        }

        // Check if the given is an owner to this dictionary
        internal bool ContainsOwner(DispatcherObject owner)
        {
            FrameworkElement fe = owner as FrameworkElement;
            if (fe != null)
            {
                return (_ownerFEs != null && _ownerFEs.Contains(fe));
            }
            else
            {
                FrameworkContentElement fce = owner as FrameworkContentElement;
                if (fce != null)
                {
                    return (_ownerFCEs != null && _ownerFCEs.Contains(fce));
                }
                else
                {
                    Application app = owner as Application;
                    if (app != null)
                    {
                        return (_ownerApps != null && _ownerApps.Contains(app));
                    }
                }
            }

            return false;
        }

        // Helper method that tries to set IsInitialized to true if BeginInit hasn't been called before this.
        // This method is called on AddOwner
        private void TryInitialize()
        {
            if (!IsInitializePending &&
                !IsInitialized)
            {
                IsInitialized = true;
            }
        }

        // Call FrameworkElement.InvalidateTree with the right data
        private void NotifyOwners(ResourcesChangeInfo info)
        {
            bool shouldInvalidate   = IsInitialized;
            bool hasImplicitStyles  = info.IsResourceAddOperation && HasImplicitStyles;

            if (shouldInvalidate && InvalidatesImplicitDataTemplateResources)
            {
                info.SetIsImplicitDataTemplateChange();
            }

            if (shouldInvalidate || hasImplicitStyles)
            {
                // Invalidate all FE owners
                if (_ownerFEs != null)
                {
                    foreach (Object o in _ownerFEs)
                    {
                        FrameworkElement fe = o as FrameworkElement;
                        if (fe != null)
                        {
                            // Set the HasImplicitStyles flag on the owner
                            if (hasImplicitStyles)
                                fe.ShouldLookupImplicitStyles = true;

                            // If this dictionary has been initialized fire an invalidation
                            // to let the tree know of this change.
                            if (shouldInvalidate)
                                TreeWalkHelper.InvalidateOnResourcesChange(fe, null, info);
                        }
                    }
                }

                // Invalidate all FCE owners
                if (_ownerFCEs != null)
                {
                    foreach (Object o in _ownerFCEs)
                    {
                        FrameworkContentElement fce = o as FrameworkContentElement;
                        if (fce != null)
                        {
                            // Set the HasImplicitStyles flag on the owner
                            if (hasImplicitStyles)
                                fce.ShouldLookupImplicitStyles = true;

                            // If this dictionary has been initialized fire an invalidation
                            // to let the tree know of this change.
                            if (shouldInvalidate)
                                TreeWalkHelper.InvalidateOnResourcesChange(null, fce, info);
                        }
                    }
                }

                // Invalidate all App owners
                if (_ownerApps != null)
                {
                    foreach (Object o in _ownerApps)
                    {
                        Application app = o as Application;
                        if (app != null)
                        {
                            // Set the HasImplicitStyles flag on the owner
                            if (hasImplicitStyles)
                                app.HasImplicitStylesInResources = true;

                            // If this dictionary has been initialized fire an invalidation
                            // to let the tree know of this change.
                            if (shouldInvalidate)
                                app.InvalidateResourceReferences(info);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fetches the resource corresponding to the given key from this dictionary.
        /// Returns a DeferredResourceReference if the object has not been inflated yet.
        /// </summary>
        internal object FetchResource(
            object      resourceKey,
            bool        allowDeferredResourceReference,
            bool        mustReturnDeferredResourceReference,
            out bool    canCache)
        {
            return FetchResource(
                resourceKey,
                allowDeferredResourceReference,
                mustReturnDeferredResourceReference,
                true /*canCacheAsThemeResource*/,
                out canCache);
        }

        /// <summary>
        /// Fetches the resource corresponding to the given key from this dictionary.
        /// Returns a DeferredResourceReference if the object has not been inflated yet.
        /// </summary>
        private object FetchResource(
            object      resourceKey,
            bool        allowDeferredResourceReference,
            bool        mustReturnDeferredResourceReference,
            bool        canCacheAsThemeResource,
            out bool    canCache)
        {
            Debug.Assert(resourceKey != null, "ResourceKey cannot be null");

            if (allowDeferredResourceReference)
            {
                if (ContainsBamlObjectFactory(resourceKey) ||
                    (mustReturnDeferredResourceReference && Contains(resourceKey)))
                {
                    canCache = false;

                    DeferredResourceReference deferredResourceReference;
                    if (!IsThemeDictionary)
                    {
                        if (_ownerApps != null)
                        {
                            deferredResourceReference = new DeferredAppResourceReference(this, resourceKey);
                        }
                        else
                        {
                            deferredResourceReference = new DeferredResourceReference(this, resourceKey);
                        }

                        // Cache the deferredResourceReference so that it can be validated
                        // in case of a dictionary change prior to its inflation
                        if (_deferredResourceReferences == null)
                        {
                            _deferredResourceReferences = new WeakReferenceList();
                        }

                        _deferredResourceReferences.Add( deferredResourceReference, true /*SkipFind*/);
                    }
                    else
                    {
                        deferredResourceReference = new DeferredThemeResourceReference(this, resourceKey, canCacheAsThemeResource);
                    }

                    ResourceDictionaryDiagnostics.RecordLookupResult(resourceKey, this);

                    return deferredResourceReference;
                }
            }

            return GetValue(resourceKey, out canCache);
        }

        /// <summary>
        /// Validate the deferredResourceReference with the given key. Key could be null meaning
        /// some catastrophic operation occurred so simply validate all DeferredResourceReferences
        /// </summary>
        private void ValidateDeferredResourceReferences(object resourceKey)
        {
            if (_deferredResourceReferences != null)
            {
                foreach (Object o in _deferredResourceReferences)
                {

                    DeferredResourceReference deferredResourceReference = o as DeferredResourceReference;
                    if (deferredResourceReference != null && (resourceKey == null || Object.Equals(resourceKey, deferredResourceReference.Key)))
                    {
                        // This will inflate the deferred reference, causing it
                        // to be removed from the list.  The list may also be
                        // purged of dead references.
                        deferredResourceReference.GetValue(BaseValueSourceInternal.Unknown);
                    }
                }
            }
        }


        /// <summary>
        /// Called when the MergedDictionaries collection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMergedDictionariesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<ResourceDictionary> oldDictionaries = null;
            List<ResourceDictionary> newDictionaries = null;
            ResourceDictionary mergedDictionary;
            ResourcesChangeInfo info;

            if (e.Action != NotifyCollectionChangedAction.Reset)
            {
                Invariant.Assert(
                    (e.NewItems != null && e.NewItems.Count > 0) ||
                    (e.OldItems != null && e.OldItems.Count > 0),
                    "The NotifyCollectionChanged event fired when no dictionaries were added or removed");


                // If one or more resource dictionaries were removed we
                // need to remove the owners they were given by their
                // parent ResourceDictionary.

                if (e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    oldDictionaries = new List<ResourceDictionary>(e.OldItems.Count);

                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        mergedDictionary = (ResourceDictionary)e.OldItems[i];
                        oldDictionaries.Add(mergedDictionary);

                        RemoveParentOwners(mergedDictionary);
                    }
                }

                // If one or more resource dictionaries were added to the merged
                // dictionaries collection we need to send down the parent
                // ResourceDictionary's owners.

                if (e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    newDictionaries = new List<ResourceDictionary>(e.NewItems.Count);

                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        mergedDictionary = (ResourceDictionary)e.NewItems[i];
                        newDictionaries.Add(mergedDictionary);

                        // If the merged dictionary HasImplicitStyle mark the outer dictionary the same.
                        if (!HasImplicitStyles && mergedDictionary.HasImplicitStyles)
                        {
                            HasImplicitStyles = true;
                        }

                        // If the merged dictionary HasImplicitDataTemplates mark the outer dictionary the same.
                        if (!HasImplicitDataTemplates && mergedDictionary.HasImplicitDataTemplates)
                        {
                            HasImplicitDataTemplates = true;
                        }

                        // If the parent dictionary is a theme dictionary mark the merged dictionary the same.
                        if (IsThemeDictionary)
                        {
                            mergedDictionary.IsThemeDictionary = true;
                        }

                        PropagateParentOwners(mergedDictionary);
                    }
                }

                info = new ResourcesChangeInfo(oldDictionaries, newDictionaries, false, false, null);
            }
            else
            {
                // Case when MergedDictionary collection is cleared
                info = ResourcesChangeInfo.CatastrophicDictionaryChangeInfo;
            }

            // Notify the owners of the change and fire
            // invalidation if already initialized

            NotifyOwners(info);
        }

        /// <summary>
        /// Adds the given owner to all merged dictionaries of this ResourceDictionary
        /// </summary>
        /// <param name="owner"></param>
        private void AddOwnerToAllMergedDictionaries(DispatcherObject owner)
        {
            if (_mergedDictionaries != null)
            {
                for (int i = 0; i < _mergedDictionaries.Count; i++)
                {
                    _mergedDictionaries[i].AddOwner(owner);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        private void RemoveOwnerFromAllMergedDictionaries(DispatcherObject owner)
        {
            if (_mergedDictionaries != null)
            {
                for (int i = 0; i < _mergedDictionaries.Count; i++)
                {
                    _mergedDictionaries[i].RemoveOwner(owner);
                }
            }
        }

        /// <summary>
        /// This sends down the owners of this ResourceDictionary into the given
        /// merged dictionary.  We do this because whenever a merged dictionary
        /// changes it should invalidate all owners of its parent ResourceDictionary.
        ///
        /// Note that AddOwners throw if the merged dictionary already has one of the
        /// parent's owners.  This implies that either we're putting a dictionary
        /// into its own MergedDictionaries collection or we're putting the same
        /// dictionary into the collection twice, neither of which are legal.
        /// </summary>
        /// <param name="mergedDictionary"></param>
        private void PropagateParentOwners(ResourceDictionary mergedDictionary)
        {
            if (_ownerFEs != null)
            {
                Invariant.Assert(_ownerFEs.Count > 0);

                if (mergedDictionary._ownerFEs == null)
                {
                    mergedDictionary._ownerFEs = new WeakReferenceList(_ownerFEs.Count);
                }

                foreach (object o in _ownerFEs)
                {
                    FrameworkElement fe = o as FrameworkElement;
                    if (fe != null)
                        mergedDictionary.AddOwner(fe);
                }
            }

            if (_ownerFCEs != null)
            {
                Invariant.Assert(_ownerFCEs.Count > 0);

                if (mergedDictionary._ownerFCEs == null)
                {
                    mergedDictionary._ownerFCEs = new WeakReferenceList(_ownerFCEs.Count);
                }

                foreach (object o in _ownerFCEs)
                {
                    FrameworkContentElement fce = o as FrameworkContentElement;
                    if (fce != null)
                        mergedDictionary.AddOwner(fce);
                }
            }

            if (_ownerApps != null)
            {
                Invariant.Assert(_ownerApps.Count > 0);

                if (mergedDictionary._ownerApps == null)
                {
                    mergedDictionary._ownerApps = new WeakReferenceList(_ownerApps.Count);
                }

                foreach (object o in _ownerApps)
                {
                    Application app = o as Application;
                    if (app != null)
                        mergedDictionary.AddOwner(app);
                }
            }
        }


        /// <summary>
        /// Removes the owners of this ResourceDictionary from the given
        /// merged dictionary.  The merged dictionary will be left with
        /// whatever owners it had before being merged.
        /// </summary>
        /// <param name="mergedDictionary"></param>
        internal void RemoveParentOwners(ResourceDictionary mergedDictionary)
        {
            if (_ownerFEs != null)
            {
                foreach (Object o in _ownerFEs)
                {
                    FrameworkElement fe = o as FrameworkElement;
                    mergedDictionary.RemoveOwner(fe);

                }
            }

            if (_ownerFCEs != null)
            {
                Invariant.Assert(_ownerFCEs.Count > 0);

                foreach (Object o in _ownerFCEs)
                {
                    FrameworkContentElement fec = o as FrameworkContentElement;
                    mergedDictionary.RemoveOwner(fec);

                }
            }

            if (_ownerApps != null)
            {
                Invariant.Assert(_ownerApps.Count > 0);

                foreach (Object o in _ownerApps)
                {
                    Application app = o as Application;
                    mergedDictionary.RemoveOwner(app);

                }
            }
        }

        private bool ContainsCycle(ResourceDictionary origin)
        {
            for (int i=0; i<MergedDictionaries.Count; i++)
            {
                ResourceDictionary mergedDictionary = MergedDictionaries[i];
                if (mergedDictionary == origin || mergedDictionary.ContainsCycle(origin))
                {
                    return true;
                }
            }

            return false;
        }

        // three properties used by ResourceDictionaryDiagnostics

        internal WeakReferenceList FrameworkElementOwners
        {
            get { return _ownerFEs; }
        }

        internal WeakReferenceList FrameworkContentElementOwners
        {
            get { return _ownerFCEs; }
        }

        internal WeakReferenceList ApplicationOwners
        {
            get { return _ownerApps; }
        }

        #endregion HelperMethods

        #region Properties

        internal WeakReferenceList DeferredResourceReferences
        {
            get { return _deferredResourceReferences; }
        }

        #endregion Properties

        #region Enumeration

        /// <summary>
        ///     Iterates the dictionary's entries, handling deferred content.
        /// </summary>
        private class ResourceDictionaryEnumerator : IDictionaryEnumerator
        {
            internal ResourceDictionaryEnumerator(ResourceDictionary owner)
            {
                _owner = owner;
                _keysEnumerator = _owner.Keys.GetEnumerator();
            }

            #region IEnumerator

            object IEnumerator.Current
            {
                get
                {
                    return ((IDictionaryEnumerator)this).Entry;
                }
            }

            bool IEnumerator.MoveNext()
            {
                return _keysEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                _keysEnumerator.Reset();
            }

            #endregion

            #region IDictionaryEnumerator

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    object key = _keysEnumerator.Current;
                    object value = _owner[key];
                    return new DictionaryEntry(key, value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    return _keysEnumerator.Current;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    return _owner[_keysEnumerator.Current];
                }
            }

            #endregion

            #region Data

            private ResourceDictionary _owner;
            private IEnumerator _keysEnumerator;

            #endregion
        }

        /// <summary>
        ///     Iterator for the dictionary's Values collection, handling deferred content.
        /// </summary>
        private class ResourceValuesEnumerator : IEnumerator
        {
            internal ResourceValuesEnumerator(ResourceDictionary owner)
            {
                _owner = owner;
                _keysEnumerator = _owner.Keys.GetEnumerator();
            }

            #region IEnumerator

            object IEnumerator.Current
            {
                get
                {
                    return _owner[_keysEnumerator.Current];
                }
            }

            bool IEnumerator.MoveNext()
            {
                return _keysEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                _keysEnumerator.Reset();
            }

            #endregion

            #region Data

            private ResourceDictionary _owner;
            private IEnumerator _keysEnumerator;

            #endregion
        }

        /// <summary>
        ///     Represents the dictionary's Values collection, handling deferred content.
        /// </summary>
        private class ResourceValuesCollection : ICollection
        {
            internal ResourceValuesCollection(ResourceDictionary owner)
            {
                _owner = owner;
            }

            #region ICollection

            int ICollection.Count
            {
                get
                {
                    return _owner.Count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                foreach (object key in _owner.Keys)
                {
                    array.SetValue(_owner[key], index++);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ResourceValuesEnumerator(_owner);
            }

            #endregion

            #region Data

            private ResourceDictionary _owner;

            #endregion
        }

        #endregion Enumeration

        #region PrivateMethods

        //
        //  This method
        //  1. Seals all the freezables/styles/templates that belong to this App/Theme/Style/Template ResourceDictionary
        //
        private void SealValues()
        {
            Debug.Assert(IsThemeDictionary || _ownerApps != null || IsReadOnly, "This must be an App/Theme/Style/Template ResourceDictionary");

            // sealing can cause DeferredResourceReferences to be replaced by the
            // inflated values.  This changes the Values collection,
            // so we can't iterate it directly.  Instead, iterate over a copy.
            int count = _baseDictionary.Count;
            if (count > 0)
            {
                object[] values = new object[count];
                _baseDictionary.Values.CopyTo(values, 0);

                foreach (object value in values)
                {
                    SealValue(value);
                }
            }
        }

        //
        //  This method
        //  1. Sets the InheritanceContext of the value to the dictionary's principal owner
        //  2. Seals the freezable/style/template that is to be placed in an App/Theme/Style/Template ResourceDictionary
        //
        private void SealValue(object value)
        {
            DependencyObject inheritanceContext = InheritanceContext;
            if (inheritanceContext != null)
            {
                AddInheritanceContext(inheritanceContext, value);
            }

            if (IsThemeDictionary || _ownerApps != null || IsReadOnly)
            {
                // If the value is a ISealable then seal it
                StyleHelper.SealIfSealable(value);
            }
        }

        // add inheritance context to a value
        private void AddInheritanceContext(DependencyObject inheritanceContext, object value)
        {
            // The VisualBrush.Visual property is the "friendliest", i.e. the
            // most likely to be accepted by the resource as FEs need to accept
            // being rooted by a VisualBrush.
            //
            // NOTE:  Freezable.Debug_VerifyContextIsValid() contains a special
            //        case to allow this with the VisualBrush.Visual property.
            //        Changes made here will require updates in Freezable.cs
            if (inheritanceContext.ProvideSelfAsInheritanceContext(value, VisualBrush.VisualProperty))
            {
                // if the assignment was successful, seal the value's InheritanceContext.
                // This makes sure the resource always gets inheritance-related information
                // from its point of definition, not from its point of use.
                DependencyObject doValue = value as DependencyObject;
                if (doValue != null)
                {
                    doValue.IsInheritanceContextSealed = true;
                }
            }
        }

        // add inheritance context to all values that came from this dictionary
        private void AddInheritanceContextToValues()
        {
            DependencyObject inheritanceContext = InheritanceContext;

            // setting InheritanceContext can cause values to be replaced.
            // This changes the Values collection, so we can't iterate it directly.
            // Instead, iterate over a copy.
            int count = _baseDictionary.Count;
            if (count > 0)
            {
                object[] values = new object[count];
                _baseDictionary.Values.CopyTo(values, 0);

                foreach (object value in values)
                {
                    AddInheritanceContext(inheritanceContext, value);
                }
            }
        }

        // remove inheritance context from a value, if it came from this dictionary
        private void RemoveInheritanceContext(object value)
        {
            DependencyObject doValue = value as DependencyObject;
            DependencyObject inheritanceContext = InheritanceContext;

            if (doValue != null && inheritanceContext != null &&
                doValue.IsInheritanceContextSealed &&
                doValue.InheritanceContext == inheritanceContext)
            {
                doValue.IsInheritanceContextSealed = false;
                inheritanceContext.RemoveSelfAsInheritanceContext(doValue, VisualBrush.VisualProperty);
            }
        }

        // remove inheritance context from all values that came from this dictionary
        private void RemoveInheritanceContextFromValues()
        {
            foreach (object value in _baseDictionary.Values)
            {
                RemoveInheritanceContext(value);
            }
        }


        // Sets the HasImplicitStyles flag if the given key is of type Type.
        private void UpdateHasImplicitStyles(object key)
        {
            // Update the HasImplicitStyles flag
            if (!HasImplicitStyles)
            {
                HasImplicitStyles = ((key as Type) != null);
            }
        }

        // Sets the HasImplicitDataTemplates flag if the given key is of type DataTemplateKey.
        private void UpdateHasImplicitDataTemplates(object key)
        {
            // Update the HasImplicitDataTemplates flag
            if (!HasImplicitDataTemplates)
            {
                HasImplicitDataTemplates = (key is DataTemplateKey);
            }
        }

        private DependencyObject InheritanceContext
        {
            get
            {
                return (_inheritanceContext != null)
                    ? (DependencyObject)_inheritanceContext.Target
                    : null;
            }
        }

        private bool IsInitialized
        {
            get { return ReadPrivateFlag(PrivateFlags.IsInitialized); }
            set { WritePrivateFlag(PrivateFlags.IsInitialized, value); }
        }

        private bool IsInitializePending
        {
            get { return ReadPrivateFlag(PrivateFlags.IsInitializePending); }
            set { WritePrivateFlag(PrivateFlags.IsInitializePending, value); }
        }

        private bool IsThemeDictionary
        {
            get { return ReadPrivateFlag(PrivateFlags.IsThemeDictionary); }
            set
            {
                if (IsThemeDictionary != value)
                {
                    WritePrivateFlag(PrivateFlags.IsThemeDictionary, value);
                    if (value)
                    {
                        SealValues();
                    }
                    if (_mergedDictionaries != null)
                    {
                        for (int i=0; i<_mergedDictionaries.Count; i++)
                        {
                            _mergedDictionaries[i].IsThemeDictionary = value;
                        }
                    }
                }
            }
        }

        internal bool HasImplicitStyles
        {
            get { return ReadPrivateFlag(PrivateFlags.HasImplicitStyles); }
            set { WritePrivateFlag(PrivateFlags.HasImplicitStyles, value); }
        }

        internal bool HasImplicitDataTemplates
        {
            get { return ReadPrivateFlag(PrivateFlags.HasImplicitDataTemplates); }
            set { WritePrivateFlag(PrivateFlags.HasImplicitDataTemplates, value); }
        }

        internal bool CanBeAccessedAcrossThreads
        {
            get { return ReadPrivateFlag(PrivateFlags.CanBeAccessedAcrossThreads); }
            set { WritePrivateFlag(PrivateFlags.CanBeAccessedAcrossThreads, value); }
        }

        private void WritePrivateFlag(PrivateFlags bit, bool value)
        {
            if (value)
            {
                _flags |= bit;
            }
            else
            {
                _flags &= ~bit;
            }
        }

        private bool ReadPrivateFlag(PrivateFlags bit)
        {
            return (_flags & bit) != 0;
        }

        private void CloseReader()
        {
            _reader.Close();
            _reader = null;
        }

        private void CopyDeferredContentFrom(ResourceDictionary loadedRD)
        {
            _buffer = loadedRD._buffer;
            _bamlStream = loadedRD._bamlStream;
            _startPosition = loadedRD._startPosition;
            _contentSize = loadedRD._contentSize;
            _objectWriterFactory = loadedRD._objectWriterFactory;
            _objectWriterSettings = loadedRD._objectWriterSettings;
            _rootElement = loadedRD._rootElement;
            _reader = loadedRD._reader;
            _numDefer = loadedRD._numDefer;
            _deferredLocationList = loadedRD._deferredLocationList;
        }

        private void  MoveDeferredResourceReferencesFrom(ResourceDictionary loadedRD)
        {
            // copy the list
            _deferredResourceReferences = loadedRD._deferredResourceReferences;

            // redirect each entry toward its new owner
            if (_deferredResourceReferences != null)
            {
                foreach (DeferredResourceReference drr in _deferredResourceReferences)
                {
                    drr.Dictionary = this;
                }
            }
        }

        #endregion PrivateMethods

        #region PrivateDataStructures

        private enum PrivateFlags : byte
        {
            IsInitialized               = 0x01,
            IsInitializePending         = 0x02,
            IsReadOnly                  = 0x04,
            IsThemeDictionary           = 0x08,
            HasImplicitStyles           = 0x10,
            CanBeAccessedAcrossThreads  = 0x20,
            InvalidatesImplicitDataTemplateResources = 0x40,
            HasImplicitDataTemplates    = 0x80,
        }

        /// <summary>
        /// This wrapper class exists so SourceUriTypeConverterMarkupExtension can pass
        /// a more complete Uri to help resolve to the correct assembly, while also passing 
        /// the original Uri so that ResourceDictionary.Source still returns the original value.
        /// </summary> 
        internal class ResourceDictionarySourceUriWrapper : Uri
        {
            public ResourceDictionarySourceUriWrapper(Uri originalUri, Uri versionedUri) : base(originalUri.OriginalString, UriKind.RelativeOrAbsolute)
            {
                OriginalUri = originalUri;
                VersionedUri = versionedUri;
            }

            internal Uri OriginalUri
            {
                get;
                set;
            }

            internal Uri VersionedUri
            {
                get;
                set;
            }
        }

        #endregion PrivateDataStructures

        // flag set by ThemeDictionaryExtension
        // to know that classic/generic Uri's should be used as fallbacks
        // when themed dictionary is not found
        internal bool IsSourcedFromThemeDictionary = false;
        private FallbackState _fallbackState = FallbackState.Classic;

        private enum FallbackState
        {
            Classic,
            Generic,
            None
        }

        #region Data

        private Hashtable                                 _baseDictionary = null;
        private WeakReferenceList                         _ownerFEs = null;
        private WeakReferenceList                         _ownerFCEs = null;
        private WeakReferenceList                         _ownerApps = null;
        private WeakReferenceList                         _deferredResourceReferences = null;
        private ObservableCollection<ResourceDictionary>  _mergedDictionaries = null;
        private Uri                                       _source = null;
        private Uri                                       _baseUri = null;
        private PrivateFlags                              _flags = 0;
        private List<KeyRecord>                           _deferredLocationList = null;

        // Buffer that contains deferable content.  This may be null if a stream was passed
        // instead of a buffer.  If a buffer was passed, then a memory stream is made on the buffer
        private byte[]          _buffer;

        // Persistent Stream that contains values.
        private Stream          _bamlStream;

        // Start position in the stream where the first value record is located.  All offsets for
        // the keys are relative to this position.
        private Int64           _startPosition;

        // Size of the delay loaded content, which only includes the value section and not the keys.
        private Int32           _contentSize;

        // The root element at the time the deferred content information was given to the dictionary.
        private object          _rootElement;

        // The number of keys that correspond to deferred content. When this reaches 0,
        // the stream can be closed.
        private int             _numDefer;

        // The object that becomes the InheritanceContext of all eligible
        // values in the dictionary - typically the principal owner of the dictionary.
        // We store a weak reference so that the dictionary does not leak the owner.
        private WeakReference   _inheritanceContext;

        // a dummy DO, used as the InheritanceContext when the dictionary's owner is
        // not itself a DO
        private static readonly DependencyObject DummyInheritanceContext = new DependencyObject();

        XamlObjectIds _contextXamlObjectIds  = new XamlObjectIds();

        private IXamlObjectWriterFactory _objectWriterFactory;
        private XamlObjectWriterSettings _objectWriterSettings;

        private Baml2006Reader _reader;

        #endregion Data
    }
}

