// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// implements the Avalon Journal Entry Object
//
//
//
//

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security ;
using System.Windows.Markup;

using MS.Internal.AppModel;
using MS.Internal;
using MS.Internal.Utility;

using System.Windows.Controls.Primitives;

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Navigation
{
    /// <summary>
    /// Derived classes encapsulate content/view state associated with custom journal entries.
    /// Object instances are passed to AddBackEntry() and also requested by the framework via the
    /// IProvideCustomContentState interface.
    /// </summary>
    [Serializable]
    public abstract class CustomContentState
    {
        /// <summary>
        /// Provides the display name for the journal entry created for this custom content state.
        /// If the returned string is null or empty, the name will be obtained, in order of
        /// precedence, from either: the JournalEntry.Name or Page.Title attached property set on
        /// the content root; the Window's title; or the current source URI.
        /// </summary>
        public virtual string JournalEntryName
        {
            get { return null; }
        }

        /// <summary>
        /// Called by the framework when navigation to a custom journal entry occurs. The method
        /// re-applies the encapsulated content/view state to the base content (element tree).
        /// </summary>
        public abstract void Replay(NavigationService navigationService, NavigationMode mode);
    };

    /// <summary>
    /// Interface to be implemented by a navigator's Content object (root element). Provides
    /// snapshots of the content state for custom journal entries.
    /// </summary>
    /// <remarks> As an alternative to implementing this interface, an application can handle
    /// the Navigating event and set the NavigatingCancelEventArgs.ContentStateToSave property.
    /// However, this method does not preserve the state of child frames.
    /// </remarks>
    public interface IProvideCustomContentState
    {
        /// <summary>
        /// Override this method to capture the custom content state or the content's view state.
        /// </summary>
        /// <returns></returns>
        CustomContentState GetContentState();
    };

    /// <summary>
    /// The base class for all journal entries.
    /// </summary>
    /// <remarks>
    /// Custom serialization implementation is needed (ISerializable) because the DependencyObject base
    /// is not [Serializable]. The Name DP is saved explicitly. (Even though it is registered as
    /// attached, it is also set on JE. KeepAlive is not saved, because KA entries are pruned before
    /// TravelLog serialization.)
    /// </remarks>
    [Serializable]
    public class JournalEntry : DependencyObject, ISerializable
    {
        // Ctors
        /////////////////////////////////////////////////////////////////////

        #region Ctors

        internal JournalEntry(JournalEntryGroupState jeGroupState, Uri uri)
        {
            _jeGroupState = jeGroupState;
            if (jeGroupState != null)
            {
                // Seems convenient to always do this here.
                jeGroupState.GroupExitEntry = this;
            }
            Source = uri;
        }

        /// <summary>
        /// Serialization constructor. Marked Protected so derived classes can be deserialized correctly
        /// </summary>
        protected JournalEntry(SerializationInfo info, StreamingContext context)
        {
            _id = info.GetInt32("_id");
            _source = (Uri)info.GetValue("_source", typeof(Uri));
            _entryType = (JournalEntryType)info.GetValue("_entryType", typeof(JournalEntryType));
            _jeGroupState = (JournalEntryGroupState)info.GetValue("_jeGroupState", typeof(JournalEntryGroupState));
            _customContentState = (CustomContentState)info.GetValue("_customContentState", typeof(CustomContentState));
            _rootViewerState = (CustomJournalStateInternal)info.GetValue("_rootViewerState", typeof(CustomJournalStateInternal));
            this.Name = info.GetString("Name");
        }

        /// <summary>
        /// ISerializable implementation
        /// </summary>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("_id", _id);
            info.AddValue("_source", _source);
            info.AddValue("_entryType", _entryType);
            // All these have to be written even if they are null. Othewise info.GetValue() would throw
            // in the serialization ctor.
            info.AddValue("_jeGroupState", _jeGroupState);
            info.AddValue("_customContentState", _customContentState);
            info.AddValue("_rootViewerState", _rootViewerState);
            info.AddValue("Name", this.Name); // see Remarks on class declaration
        }
        #endregion

        // Public methods
        /////////////////////////////////////////////////////////////////////

        #region Public Methods

        /// <summary>
        /// Reads the attached property JournalEntry.Name from the given element.
        /// Setting it at the root element of a navigator will update NavWin’s dropdown menu when navigating inside of that navigator.
        /// </summary>
        /// <param name="dependencyObject">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        public static string GetName(DependencyObject dependencyObject)
        {
            // not verifying Context on static method

            return dependencyObject != null ? (string)dependencyObject.GetValue(NameProperty) : null;
        }

        /// <summary>
        /// Writes the attached property JournalEntry.Name to the given element.
        /// </summary>
        /// <param name="dependencyObject">The element to which to write the attached property.</param>
        /// <param name="name">The property value to set</param>
        public static void SetName(DependencyObject dependencyObject, string name)
        {
            // not verifying Context on static method

            if (dependencyObject == null)
            {
                throw new ArgumentNullException("dependencyObject");
            }

            dependencyObject.SetValue(NameProperty, name);
        }

        /// <summary>
        /// Reads the attached property KeepAlive from the given element. This is not a property of the
        /// journal, but rather a flag to indicate that this tree should be keep alived
        /// as part of journaling. If KeepAlive == true, then the entire tree will be saved.
        /// </summary>
        /// <param name="dependencyObject">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="JournalEntry.KeepAliveProperty" />
        public static bool GetKeepAlive(DependencyObject dependencyObject)
        {
            // not verifying Context on static method
            if (dependencyObject == null)
            {
                throw new ArgumentNullException("dependencyObject");
            }

            return (bool)dependencyObject.GetValue(KeepAliveProperty);
        }

        /// <summary>
        /// Writes the attached property KeepAlive to the given element. See above.
        /// </summary>
        /// <param name="dependencyObject">The element to which to write the attached property.</param>
        /// <param name="keepAlive">The property value to set</param>
        /// <seealso cref="JournalEntry.KeepAliveProperty" />
        public static void SetKeepAlive(DependencyObject dependencyObject, bool keepAlive)
        {
            // not verifying Context on static method

            if (dependencyObject == null)
            {
                throw new ArgumentNullException("dependencyObject");
            }

            dependencyObject.SetValue(KeepAliveProperty, keepAlive);
        }
        #endregion

        // Public properties
        /////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The Uri of the stored Page
        /// </summary>
        public Uri Source
        {
            get { return _source; }
            set { _source = BindUriHelper.GetUriRelativeToPackAppBase(value); }
        }

        /// <summary> The custom content state or content view state associated with the entry </summary>
        public CustomContentState CustomContentState
        {
            get { return _customContentState; }
            internal set { _customContentState = value; }
        }

        /// <summary>
        /// The name of the journal entry to be displayed in the drop-down list
        /// on the Back/Forward buttons
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        /// <summary>
        /// Attached DependencyProperty for Name property.
        /// Setting it at the root element of a navigator will update NavWin’s dropdown menu when navigating inside of that navigator.
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(JournalEntry), new PropertyMetadata(String.Empty));

        /// <summary>
        /// DependencyProperty for KeepAlive property.
        /// </summary>
        public static readonly DependencyProperty KeepAliveProperty =
            DependencyProperty.RegisterAttached("KeepAlive", typeof(bool), typeof(JournalEntry), new PropertyMetadata(false));

        #endregion

        // Internal methods
        /////////////////////////////////////////////////////////////////////

        #region Internal Methods

        /// <summary>
        /// Is this journal entry journaling a page function?
        /// </summary>
        internal virtual bool IsPageFunction()
        {
            return false;
        }

        /// <summary>
        /// Is this journal entry holding on to a tree?
        /// </summary>
        /// <returns></returns>
        internal virtual bool IsAlive()
        {
            return false;
        }

        internal virtual void SaveState(object contentObject)
        {
            if (contentObject == null)
                throw new ArgumentNullException("contentObject");
            if (!IsAlive())
            {
                if (_jeGroupState.JournalDataStreams != null)
                {
                    Debug.Assert(!_jeGroupState.JournalDataStreams.HasAnyData,
                        "JournalDataSreams should have been emptied by the last RestoreState() call.");
                }
                else
                {
                    _jeGroupState.JournalDataStreams = new DataStreams();
                }
                _jeGroupState.JournalDataStreams.Save(contentObject);
            }
        }

        internal virtual void RestoreState(object contentObject)
        {
            if (contentObject == null)
                throw new ArgumentNullException("contentObject");
            if (IsAlive())
            {
                Debug.Assert(_jeGroupState.JournalDataStreams == null);
            }
            else
            {
                DataStreams jds = _jeGroupState.JournalDataStreams;
                if (jds != null)
                {
                    jds.Load(contentObject);
                    // DataStreams not needed anymore. Clear for fresh saving when the next navigation
                    // occurs.
                    jds.Clear();
                }
            }
        }

        internal virtual bool Navigate(INavigator navigator, NavigationMode navMode)
        {
            Debug.Assert(navMode == NavigationMode.Back || navMode == NavigationMode.Forward);

            // This is fallback functionality if somebody creates a JournalEntry and gives it a URI.
            if (this.Source != null)
            {
                return navigator.Navigate(Source, new NavigateInfo(Source, navMode, this));
            }
            else
            {
                Invariant.Assert(false, "Cannot navigate to a journal entry that does not have a Source.");
                return false;
            }
        }

        /// <summary>
        /// Returns a string that contains the URI for display in a JournalEntry.
        /// It removes pack://application:,,,/ and any component, and replaces
        /// pack://siteoforigin:,,,/ with the actual site of origin URI.
        /// </summary>
        /// <param name="uri">The URI to display</param>
        /// <param name="siteOfOrigin">The site of origin URI</param>
        /// <returns>display name</returns>
        internal static string GetDisplayName(Uri uri, Uri siteOfOrigin)
        {
            // In case this is a fragment navigation in a tree that was not navigated to by URI...
            if (!uri.IsAbsoluteUri)
            {
                return uri.ToString();
            }

            bool isPack = String.Compare(uri.Scheme, PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase) == 0;
            string displayName;

            if (isPack)
            {
                Uri relative = BaseUriHelper.MakeRelativeToSiteOfOriginIfPossible(uri);
                if (! relative.IsAbsoluteUri)
                {
                    displayName = (new Uri(siteOfOrigin, relative)).ToString();
                }
                else
                {
                    string relativeUri = uri.AbsolutePath + uri.Query + uri.Fragment;
                    string part, assy, assyVers, assyKey;
                    BaseUriHelper.GetAssemblyNameAndPart(new Uri(relativeUri, UriKind.Relative), out part, out assy, out assyVers, out assyKey);
                    if (!string.IsNullOrEmpty(assy))
                    {
                        displayName = part;
                    }
                    else
                    {
                        displayName = relativeUri;
                    }
                }
            }
            else
            {
                displayName = uri.ToString();
            }

            if (!string.IsNullOrEmpty(displayName) && displayName[0] == '/')
            {
                displayName = displayName.Substring(1);
            }

            return displayName;
        }
        #endregion

        // Internal properties
        /////////////////////////////////////////////////////////////////////

        #region Internal properties

        internal JournalEntryGroupState JEGroupState
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _jeGroupState; }
            set { _jeGroupState = value; }
        }

        /// <summary>
        /// We will now store the entire entry data in the travellog so Journaling will
        /// work even if you renavigate back to the app from html pages as long as it is
        /// linear history navigations.
        /// </summary>
        internal int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// The NavigationService(toplevel || frame) where this entry was last displayed.
        /// </summary>
        internal Guid NavigationServiceId
        {
            get { return _jeGroupState.NavigationServiceId; ; }
        }

        /// <summary>
        /// Journal entry type
        /// </summary>
        internal JournalEntryType EntryType
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _entryType; }
            set { _entryType = value; }
        }

        /// <summary>
        /// Can this be navigated to?
        /// </summary>
        internal bool IsNavigable()
        {
            return _entryType == JournalEntryType.Navigable;
        }

        internal uint ContentId
        {
            get { return _jeGroupState.ContentId; }
        }

        internal CustomJournalStateInternal RootViewerState
        {
            get { return _rootViewerState; }
            set { _rootViewerState = value; }
        }

        #endregion

        // Private fields
        //
        // If you add any fields here, check if it needs to be serialized.
        // If yes, then add it to the ISerializable implementation and make
        // corresponding changes in the Serialization constructor.
        //
        /////////////////////////////////////////////////////////////////////

        #region Private fields
        private int _id;
        private JournalEntryGroupState _jeGroupState;
        /// <summary>
        /// Converted relative to pack://application, if possible
        /// </summary>
        private Uri _source;
        private JournalEntryType _entryType = JournalEntryType.Navigable;
        private CustomContentState _customContentState;
        private CustomJournalStateInternal _rootViewerState;
        #endregion
    }

    //**
    //** All derived classes are in MS.Internal.AppModel.
}
