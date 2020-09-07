// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains various journaling related internal enums and classes
//

using System;
using System.Security;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

using System.Windows.Navigation;
using MS.Internal.Utility;

//In order to avoid generating warnings about unknown message numbers and 
//unknown pragmas when compiling your C# source code with the actual C# compiler, 
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace MS.Internal.AppModel
{
    [Serializable]
    internal enum JournalEntryType : byte
    {
        Navigable,
        UiLess,
    }

    internal enum JournalReason
    {
        NewContentNavigation = 1, FragmentNavigation, AddBackEntry
    };

    /// <summary>
    /// Encapsulates the custom journal state of an element implementing IJournalState.
    /// </summary>
    [Serializable]
    internal abstract class CustomJournalStateInternal
    {
        /// <summary>
        /// Called when the entire journal is about to be binary-serialized. Non-serializable objects
        /// should be removed or replaced with serializable ones.
        /// </summary>
        internal virtual void PrepareForSerialization() { }
    };

    /// <summary>
    /// Currently used in two contexts:
    /// 1) Allows an element in the logical tree of INavigator.Content to implement "custom" journaling
    ///   of its state, beyond the DP journaling that DataStreams does. 
    ///   [The public IProvideCustomContentState applies only to the root element.]
    ///   journalReason is always NewContentNavigation in this use.
    ///   Initially, the only actual use is for Frame to preserve its own journal.
    /// 2) Root viewer journaling. See NavigationService.MakeJournalEntry()...
    /// </summary>
    internal interface IJournalState
    {
        CustomJournalStateInternal GetJournalState(JournalReason journalReason);
        void RestoreJournalState(CustomJournalStateInternal state);
    };

    /// <summary>
    /// Shared state for all journal entries associated with the same content object ("bind product").
    /// Journal entries created for fragment navigations or CustomContentState navigations within the
    /// current page share one JournalEntryGroupState object. This avoids duplication of state and
    /// the problems described in bug 1216726.
    /// </summary>
    [Serializable]
    internal class JournalEntryGroupState
    {
        internal JournalEntryGroupState() { }

        internal JournalEntryGroupState(Guid navSvcId, uint contentId)
        {
            _navigationServiceId = navSvcId;
            _contentId = contentId;
        }

        internal Guid NavigationServiceId
        {
            get { return _navigationServiceId; }
            set { _navigationServiceId = value; }
        }

        /// <summary> 
        /// Unique identifier (within a NavigationService, not across the entire Journal) of the page
        /// content ("bind product") this journal entry group is associated with. 
        /// One use of this property is to help us distinguish back/fwd navigations 
        /// within the same page from navigations to a different instance of the same page
        /// (same URI, modulo #frament id). (Bugs 1187603 and 1187613). 
        /// Zero is not a valid id.
        /// </summary>
        internal uint ContentId
        {
            get { return _contentId; }
            set
            {
                Debug.Assert(_contentId == 0 || _contentId == value,
                    "Once set, the ContentId for a JournalEntryGroup should not be changed.");
                _contentId = value;
            }
        }

        internal DataStreams JournalDataStreams
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get
            {
                // Do not make this getter create a new DataStreams object. Keep-alive journal entries 
                // don't need it.
                return _journalDataStreams;
            }
            set
            {
                _journalDataStreams = value;
            }
        }

        internal JournalEntry GroupExitEntry
        {
            get { return _groupExitEntry; }
            set
            {
                Debug.Assert(value.JEGroupState == this);
                Debug.Assert(_groupExitEntry == null || _groupExitEntry.ContentId == value.ContentId);
                _groupExitEntry = value;
            }
        }

        private Guid _navigationServiceId;
        private uint _contentId;
        private DataStreams _journalDataStreams;
        private JournalEntry _groupExitEntry;
    };

    /// <summary>
    /// The journal entry when journaling by URI. When navigating away, it will save the form state.
    /// When navigating back/forwards to this entry, it will navigate back to the URI, and then 
    /// restore the form state.
    /// </summary>
    [Serializable]
    internal class JournalEntryUri : JournalEntry, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryUri(JournalEntryGroupState jeGroupState, Uri uri)
            : base(jeGroupState, uri)
        {
        }

        /// <summary>
        /// Serialization constructor. Marked Protected so derived classes can be deserialized correctly
        /// </summary>
        protected JournalEntryUri(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion


        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        internal override void SaveState(object contentObject)
        {
            Invariant.Assert(this.Source != null, "Can't journal by Uri without a Uri.");
            base.SaveState(contentObject); // Save controls state (JournalDataStreams).
        }

        #endregion
    }

    /// <summary>
    /// This is the journal entry class for use when the entire tree is being kept alive. It will be
    /// saved when navigated away from, and when restored, it navigates to the saved tree.
    /// </summary>
    // Not [Serializable], because keep-alive content should not be serialized into the TravelLog.
    internal class JournalEntryKeepAlive : JournalEntry
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryKeepAlive(JournalEntryGroupState jeGroupState, Uri uri, object keepAliveRoot)
            : base(jeGroupState, uri)
        {
            Invariant.Assert(keepAliveRoot != null);
            _keepAliveRoot = keepAliveRoot;
        }

        #endregion

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        /// <summary>
        /// This is how the JournalEntry holds on to the tree itself. There is no need to keep track of other
        /// things c.f. AlivePageFunction.
        /// </summary>
        internal object KeepAliveRoot
        {
            get { return _keepAliveRoot; }
        }

        internal override bool IsAlive()
        {
            return this.KeepAliveRoot != null;
        }

        internal override void SaveState(object contentObject)
        {
            Debug.Assert(this.KeepAliveRoot == contentObject); // set by ctor; shouldn't change
            this._keepAliveRoot = contentObject;
            // No call to base.SaveState() since it saves controls state, and contentObject is KeepAlive.
        }

        internal override bool Navigate(INavigator navigator, NavigationMode navMode)
        {
            Debug.Assert(navMode == NavigationMode.Back || navMode == NavigationMode.Forward);
            Debug.Assert(this.KeepAliveRoot != null);
            return navigator.Navigate(this.KeepAliveRoot, new NavigateInfo(Source, navMode, this));
        }

        #endregion

        #region Private fields

        private object _keepAliveRoot;     // the root of a tree being kept alive

        #endregion
    }

    /// <summary>
    /// The journal entry for page functions. Ideally, these would use the same journal method
    /// that the other entries use, and derive from the other JournalEntry* classes to add the
    /// small bit of functionality that the page functions require (the Finish handler, for one.)
    /// </summary>
    [Serializable]
    internal abstract class JournalEntryPageFunction : JournalEntry, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryPageFunction(JournalEntryGroupState jeGroupState, PageFunctionBase pageFunction)
            : base(jeGroupState, null)
        {
            PageFunctionId = pageFunction.PageFunctionId;
            ParentPageFunctionId = pageFunction.ParentPageFunctionId;
        }

        /// <summary>
        /// Serialization constructor. Marked Protected so derived classes can be deserialized correctly
        /// The base implementation needs to be called if this class is overridden
        /// </summary>
        protected JournalEntryPageFunction(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _pageFunctionId = (Guid)info.GetValue("_pageFunctionId", typeof(Guid));
            _parentPageFunctionId = (Guid)info.GetValue("_parentPageFunctionId", typeof(Guid)); ;
        }

        //
        //  ISerializable implementation
        //
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_pageFunctionId", _pageFunctionId);
            info.AddValue("_parentPageFunctionId", _parentPageFunctionId);
        }

        #endregion

        // Internal Properties
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the GUID for this PageFunction. It will be used as its childrens'
        /// ParentPageFunctionId.
        /// </summary>
        internal Guid PageFunctionId
        {
            get { return _pageFunctionId; }
            set { _pageFunctionId = value; }
        }

        /// <summary>
        /// If this PageFunction is a child PageFunction, this will be the GUID of its
        /// parent PageFunction. If this is Guid.Empty, then the parent is NOT a
        /// PageFunction.
        /// </summary>
        internal Guid ParentPageFunctionId
        {
            get { return _parentPageFunctionId; }
            set { _parentPageFunctionId = value; }
        }

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        internal override bool IsPageFunction()
        {
            return true;
        }

        internal override bool IsAlive()
        {
            return false;
        }

        /// <summary>
        /// Reconstitutes the PageFunction associated with this journal entry so that it can be
        /// re-navigated to. Happens when returning from a child PF or doing journal navigation.
        /// </summary>
        internal abstract PageFunctionBase ResumePageFunction();

        #endregion

        #region internal Static method

        //
        // This method is to get the journal entry index in the Journal List for the parent page 
        // of this PageFunction.
        //
        // The parent page could be a PageFunction or a Non-PageFunction.
        // For the non PageFunction case, it could have a Uri or could not have a Uri.
        //
        // Case 1: The ParentPageFunction has been set, so just go back and look for it.
        // Case 2: The ParentPageFunction has NOT been set, so the parent must be most recent non-PF entry.
        //
        internal static int GetParentPageJournalIndex(NavigationService NavigationService, Journal journal, PageFunctionBase endingPF)
        {
            JournalEntryPageFunction pageFunctionEntry;
            JournalEntry journalEntry;

            for (int index = journal.CurrentIndex - 1; index >= 0; --index)
            {
                journalEntry = journal[index];

                // Be sure that the navigation containers match
                if (journalEntry.NavigationServiceId != NavigationService.GuidId)
                    continue;

                pageFunctionEntry = journalEntry as JournalEntryPageFunction;

                if (endingPF.ParentPageFunctionId == Guid.Empty)
                {
                    // We are looking for a non-PageFunction
                    if (pageFunctionEntry == null)
                    {
                        return index; // found!
                    }
                }
                else
                {
                    // we are looking for a PageFunction
                    if ((pageFunctionEntry != null) && (pageFunctionEntry.PageFunctionId == endingPF.ParentPageFunctionId))
                    {
                        return index; // found!
                    }
                }
            }

            Debug.Assert(endingPF.ParentPageFunctionId == Guid.Empty,
                "Should have been able to find the parent if the ParentPageFunctionId was set. Are they in different NavigationServices? They shouldn't be.");

            return _NoParentPage;
        }

        #endregion

        // Private fields
        //
        // If you add any fields here, check if it needs to be serialized.
        // If yes, then add it to the ISerializable implementation and make
        // corresponding changes in the Serialization constructor.
        //
        /////////////////////////////////////////////////////////////////////

        private Guid _pageFunctionId;
        private Guid _parentPageFunctionId;
        internal const int _NoParentPage = -1;
    }

    /// <summary>
    /// This is the class for page functions that are being kept alive.
    /// </summary>
    // Not [Serializable], because keep-alive content should not be serialized into the TravelLog.
    internal class JournalEntryPageFunctionKeepAlive : JournalEntryPageFunction
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryPageFunctionKeepAlive(JournalEntryGroupState jeGroupState, PageFunctionBase pageFunction)
            : base(jeGroupState, pageFunction)
        {
            Debug.Assert(pageFunction != null && pageFunction.KeepAlive);
            this._keepAlivePageFunction = pageFunction;
        }

        #endregion

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        internal override bool IsPageFunction()
        {
            return true;
        }

        internal override bool IsAlive()
        {
            return this.KeepAlivePageFunction != null;
        }

        internal PageFunctionBase KeepAlivePageFunction
        {
            get { return _keepAlivePageFunction; }
        }

        internal override PageFunctionBase ResumePageFunction()
        {
            PageFunctionBase pageFunction = this.KeepAlivePageFunction;
            pageFunction._Resume = true;
            return pageFunction;
        }

        internal override void SaveState(object contentObject)
        {
            Invariant.Assert(_keepAlivePageFunction == contentObject); // set by ctor
            // No call to base.SaveState() since it saves controls state, and this PF is KeepAlive.
        }

        /// <summary>
        /// This override is used when doing journal navigation to a PF, not when it is resumed after
        /// a child PF finishes.
        /// </summary>
        internal override bool Navigate(INavigator navigator, NavigationMode navMode)
        {
            Debug.Assert(navMode == NavigationMode.Back || navMode == NavigationMode.Forward);
            // When doing fragment navigation within the PF, it should not be marked as Resumed;
            // otherwise, its Start() override may not be called.
            PageFunctionBase pf = (navigator.Content == _keepAlivePageFunction) ?
                _keepAlivePageFunction : ResumePageFunction();
            Debug.Assert(pf != null);
            return navigator.Navigate(pf, new NavigateInfo(this.Source, navMode, this));
        }

        #endregion

        #region Private fields

        PageFunctionBase _keepAlivePageFunction = null;

        #endregion
    }

    /// <summary>
    /// JournalEntryPageFunctionSaver
    /// This is JournalEntry for PageFunction which is not set as KeepAlive. The PageFunction could be 
    /// navigated from a Uri or the instance created from a PageFunction type.
    /// </summary>
    [Serializable]
    internal abstract class JournalEntryPageFunctionSaver : JournalEntryPageFunction, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        //
        // Ctor of JournalEntryPageFunctionSaver
        //
        internal JournalEntryPageFunctionSaver(JournalEntryGroupState jeGroupState, PageFunctionBase pageFunction)
            : base(jeGroupState, pageFunction)
        {
            Debug.Assert(!pageFunction.KeepAlive);
        }


        /// <summary>
        /// Serialization constructor. Marked Protected so derived classes can be deserialized correctly
        /// The base implementation needs to be called if this class is overridden
        /// </summary>
        protected JournalEntryPageFunctionSaver(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _returnEventSaver = (ReturnEventSaver)info.GetValue("_returnEventSaver", typeof(ReturnEventSaver));
        }

        //
        //  ISerializable implementation
        //
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_returnEventSaver", _returnEventSaver);
        }
        #endregion


        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        internal override void SaveState(object contentObject)
        {
            PageFunctionBase pageFunction = (PageFunctionBase)contentObject;
            _returnEventSaver = pageFunction._Saver;
            base.SaveState(contentObject); // Save controls state (JournalDataStreams).
        }

        //
        // Take the PageFunction specific setting, then call the RestoreState in base class
        // to store previous state in a journal navigation.
        //
        internal override void RestoreState(object contentObject)
        {
            if (contentObject == null)
                throw new ArgumentNullException("contentObject");

            PageFunctionBase pageFunction = (PageFunctionBase)contentObject;

            if (pageFunction == null)
            {
                throw new Exception(SR.Get(SRID.InvalidPageFunctionType, contentObject.GetType()));
            }

            pageFunction.ParentPageFunctionId = ParentPageFunctionId;
            pageFunction.PageFunctionId = PageFunctionId;
            pageFunction._Saver = _returnEventSaver; // saved Return event delegate from the *parent* of this PF
            pageFunction._Resume = true;

            base.RestoreState(pageFunction);
        }

        /// <summary>
        /// This override is used when doing journal navigation to a PF, not when it is resumed after
        /// a child PF finishes.
        /// </summary>
        internal override bool Navigate(INavigator navigator, NavigationMode navMode)
        {
            Debug.Assert(navMode == NavigationMode.Back || navMode == NavigationMode.Forward);

            // Resume the PF and navigate to it.
            // Special case: doing fragment navigation or CustomContentState navigation
            // within a PF. Then don't create a new PF object!
            IDownloader idl = navigator as IDownloader;
            NavigationService ns = idl != null ? idl.Downloader : null;
            Debug.Assert(ns != null, "Fragment navigation won't work when the INavigator doesn't have a NavigationService.");

            PageFunctionBase pageFunction =
                (ns != null && ns.ContentId == this.ContentId) ?
                (PageFunctionBase)ns.Content : ResumePageFunction();

            Debug.Assert(pageFunction != null);

            return navigator.Navigate(pageFunction, new NavigateInfo(this.Source, navMode, this));
        }

        #endregion

        // Internal properties
        /////////////////////////////////////////////////////////////////////

        #region Internal properties

        #endregion

        // private methods
        /////////////////////////////////////////////////////////////////////

        // Private fields
        //
        // If you add any fields here, check if it needs to be serialized.
        // If yes, then add it to the ISerializable implementation and make
        // corresponding changes in the Serialization constructor.
        //
        /////////////////////////////////////////////////////////////////////

        #region Private fields

        private ReturnEventSaver _returnEventSaver;

        #endregion
    }


    //
    // When the PageFunction is implemented in a pure code file without Xaml file involved, 
    // and the instance of such PageFunction was passed to Navigiation method, JournalEntryPageFunctionType
    // would be created to save journal data information.
    //
    [Serializable]
    internal class JournalEntryPageFunctionType : JournalEntryPageFunctionSaver, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryPageFunctionType(JournalEntryGroupState jeGroupState, PageFunctionBase pageFunction)
            : base(jeGroupState, pageFunction)
        {
            string typeName = pageFunction.GetType().AssemblyQualifiedName;
            this._typeName = new SecurityCriticalDataForSet<string>(typeName);
        }


        /// <summary>
        /// Serialization constructor. Marked Protected so derived classes can be deserialized correctly
        /// The base implementation needs to be called if this class is overridden
        /// </summary>
        protected JournalEntryPageFunctionType(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _typeName = new SecurityCriticalDataForSet<string>(info.GetString("_typeName"));
        }

        //
        //  ISerializable implementation
        //
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_typeName", _typeName.Value);
        }
        #endregion

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods

        internal override void SaveState(object contentObject)
        {
            Debug.Assert(contentObject.GetType().AssemblyQualifiedName == this._typeName.Value,
                "The type of a PageFunction a journal entry is associated with cannot change.");
            base.SaveState(contentObject); // Save controls state (JournalDataStreams).
        }

        //
        // Reconstitutes the PageFunction associated with this journal entry so that it can be
        // re-navigated to. Happens when returning from a child PF or doing journal navigation.
        //
        // The PageFunction should be implemented in pure code file without xaml file involved.
        //
        internal override PageFunctionBase ResumePageFunction()
        {
            PageFunctionBase pageFunction;

            Invariant.Assert(this._typeName.Value != null, "JournalEntry does not contain the Type for the PageFunction to be created");

            //First try Type.GetType from the saved typename, then try Activator.CreateInstanceFrom
            //Type.GetType - Since the typename is fullyqualified
            //we will end up using the default binding mechanism to locate and bind to the assembly.
            //If the assembly was not a strongly named one nor is present in the APPBASE, this will
            //fail.

            Type pfType = Type.GetType(this._typeName.Value);
            try
            {
                pageFunction = (PageFunctionBase)Activator.CreateInstance(pfType);
            }
            catch (Exception ex)
            {
                throw new Exception(SR.Get(SRID.FailedResumePageFunction, this._typeName.Value), ex);
            }

            InitializeComponent(pageFunction);

            RestoreState(pageFunction);

            return pageFunction;
        }

        #endregion

        #region private method

        private void InitializeComponent(PageFunctionBase pageFunction)
        {
            // Need to explicitly add a call to InitializeComponent() for Page
            IComponentConnector iComponentConnector = pageFunction as IComponentConnector;
            if (iComponentConnector != null)
            {
                iComponentConnector.InitializeComponent();
            }
        }

        #endregion

        // private methods
        /////////////////////////////////////////////////////////////////////

        // Private fields
        //
        // If you add any fields here, check if it needs to be serialized.
        // If yes, then add it to the ISerializable implementation and make
        // corresponding changes in the Serialization constructor.
        //
        /////////////////////////////////////////////////////////////////////

        #region Private fields

        /// AssemblyQualifiedName of the PageFunction Type
        private SecurityCriticalDataForSet<string> _typeName;

        #endregion
    }


    //
    // When the PageFunction is implemented in a xaml file, or it was navigated from a Uri, 
    // JournalEntryPageFunctionUri would be created to save journal data information.
    //
    [Serializable]
    internal class JournalEntryPageFunctionUri : JournalEntryPageFunctionSaver, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntryPageFunctionUri(JournalEntryGroupState jeGroupState, PageFunctionBase pageFunction, Uri markupUri)
            : base(jeGroupState, pageFunction)
        {
            _markupUri = markupUri;
        }


        protected JournalEntryPageFunctionUri(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _markupUri = (Uri)info.GetValue("_markupUri", typeof(Uri));
        }

        //
        //  ISerializable implementation
        //
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_markupUri", _markupUri);
        }
        #endregion

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal methods


        //
        // Create an instance of PageFunction for Resume purpose. this PageFunction should be created
        // from a Uri.
        //
        internal override PageFunctionBase ResumePageFunction()
        {
            PageFunctionBase pageFunction;

            //
            // The page function was compiled from a xaml file.
            //

            // We should pay more attention on this scenario. The instance of PageFunction is created from
            // the baml stream, but it should ignore the value setting for all the Journal-able properties 
            // and elements while the tree is created from the original baml stream.
            //

            Debug.Assert(_markupUri != null, "_markupUri in JournalEntryPageFunctionUri should be set.");

            pageFunction = Application.LoadComponent(_markupUri, true) as PageFunctionBase;

            RestoreState(pageFunction);

            return pageFunction;
        }

        #endregion


        // Private field

        #region private fields

        // 
        // Keep the Uri that is associated with current PageFunction page.
        // It could be Navigator.Source or the Uri of Baml resource for the PageFunction type.
        //
        // Notes: We don't mix this with JournalEntry.Source, since NavigationService has some special usage 
        // for JE.Source, JE.Source could affect the Navigator.Source in some scenario.
        //
        private Uri _markupUri;

        #endregion
    }
}
