// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace System.Windows.Shell
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Markup;
    using MS.Internal;
    using MS.Internal.AppModel;
    using MS.Internal.PresentationFramework;
    using MS.Internal.Interop;
    using MS.Win32;

    using HRESULT = MS.Internal.Interop.HRESULT;

    /// <summary>
    /// The list of possible reasons why a JumpItem would be rejected from a JumpList when applied.
    /// </summary>
    public enum JumpItemRejectionReason
    {
        /// <summary>
        /// Unknown reason.  This should not be used.
        /// </summary>
        None,
        /// <summary>
        /// The item was rejected because it was invalid for a jump list.  E.g. the file path didn't exist.
        /// </summary>
        /// <remarks>
        /// If the application is running on a system where jump lists are not available (like XP or Vista)
        /// items will get rejected with this reason.
        /// </remarks>
        InvalidItem,
        /// <summary>
        /// The item was rejected because the program was not registered to handle the file extension.
        /// </summary>
        NoRegisteredHandler,
        /// <summary>
        /// The item was rejected because the user had explicitly removed it since the last time a JumpList was applied.
        /// </summary>
        RemovedByUser,
    }

    /// <summary>
    /// EventArgs for JumpList.JumpItemsRejected event.
    /// </summary>
    public sealed class JumpItemsRejectedEventArgs : EventArgs
    {
        public JumpItemsRejectedEventArgs()
            : this(null, null)
        { }

        public JumpItemsRejectedEventArgs(IList<JumpItem> rejectedItems, IList<JumpItemRejectionReason> reasons)
        {
            // If one of the collections is null then the other has to be, too.
            if ((rejectedItems == null && reasons != null)
                || (reasons == null && rejectedItems != null)
                || (rejectedItems != null && reasons != null && rejectedItems.Count != reasons.Count))
            {
                throw new ArgumentException(SR.Get(SRID.JumpItemsRejectedEventArgs_CountMismatch));
            }

            // We don't want the contents of the list getting modified in the event handler,
            // so use a read-only copy 
            if (rejectedItems != null)
            {
                RejectedItems = new List<JumpItem>(rejectedItems).AsReadOnly();
                RejectionReasons = new List<JumpItemRejectionReason>(reasons).AsReadOnly();
            }
            else
            {
                RejectedItems = new List<JumpItem>().AsReadOnly();
                RejectionReasons = new List<JumpItemRejectionReason>().AsReadOnly();
            }
        }

        public IList<JumpItem> RejectedItems { get; private set; }
        public IList<JumpItemRejectionReason> RejectionReasons { get; private set; }
    }

    /// <summary>
    /// EventArgs for JumpList.JumpItemsRemovedByUser event.
    /// </summary>
    public sealed class JumpItemsRemovedEventArgs : EventArgs
    {
        public JumpItemsRemovedEventArgs()
            : this(null)
        { }

        public JumpItemsRemovedEventArgs(IList<JumpItem> removedItems)
        {
            if (removedItems != null)
            {
                RemovedItems = new List<JumpItem>(removedItems).AsReadOnly();
            }
            else
            {
                RemovedItems = new List<JumpItem>().AsReadOnly();
            }
        }

        public IList<JumpItem> RemovedItems { get; private set; }
    }

    /// <summary>
    /// Manage the tasks and files that Shell associates with this application.
    /// This allows modification of the Jump List UI in Windows 7 that appears on the Taskbar and Start Menu.
    /// </summary>
    [ContentProperty("JumpItems")]
    public sealed class JumpList : ISupportInitialize
    {
        static JumpList()
        {
            // Passing NULL for the HMODULE returns the running executable path.
            _FullName = UnsafeNativeMethods.GetModuleFileName(new HandleRef());
        }

        /// <summary>
        /// Add the item at the specified file path to the application's JumpList's recent items.
        /// </summary>
        /// <remarks>
        /// This makes the item eligible for inclusion in the special Recent and Frequent categories.
        /// </remarks>
        public static void AddToRecentCategory(string itemPath)
        {
            Verify.FileExists(itemPath, "itemPath");
            itemPath = Path.GetFullPath(itemPath);
            NativeMethods2.SHAddToRecentDocs(itemPath);
        }

        /// <summary>
        /// Add the item to the application's JumpList's recent items.
        /// </summary>
        /// <remarks>
        /// This makes the item eligible for inclusion in the special Recent and Frequent categories.
        /// </remarks>
        public static void AddToRecentCategory(JumpPath jumpPath)
        {
            Verify.IsNotNull(jumpPath, "jumpPath");
            AddToRecentCategory(jumpPath.Path);
        }

        /// <summary>
        /// Add the task at the specified file path to the application's JumpList's recent items.
        /// </summary>
        /// <remarks>
        /// This makes the item eligible for inclusion in the special Recent and Frequent categories.
        /// </remarks>
        public static void AddToRecentCategory(JumpTask jumpTask)
        {
            Verify.IsNotNull(jumpTask, "jumpTask");

            // SHAddToRecentDocs only allows IShellLinks in Windows 7 and later.
            // Silently fail this if that's not the case.
            // We don't give feedback on success here, so this is okay.
            if (Utilities.IsOSWindows7OrNewer)
            {
                IShellLinkW shellLink = CreateLinkFromJumpTask(jumpTask, false);
                try
                {
                    if (shellLink != null)
                    {
                        NativeMethods2.SHAddToRecentDocs(shellLink);
                    }
                }
                finally
                {
                    Utilities.SafeRelease(ref shellLink);
                }
            }
        }


        private class _RejectedJumpItemPair
        {
            public JumpItem JumpItem { get; set; }
            public JumpItemRejectionReason Reason { get; set; }
        }

        private class _ShellObjectPair
        {
            // JumpPath/JumpTask
            public JumpItem JumpItem { get; set; }
            // IShellItem/IShellLink
            public object ShellObject { get; set; }

            /// <summary>
            /// Releases all native references in a list of _ShellObjectPairs.
            /// </summary>
            /// <param name="list">The list from which to release the resources.</param>
            public static void ReleaseShellObjects(List<_ShellObjectPair> list)
            {
                if (list != null)
                {
                    foreach (_ShellObjectPair shellMap in list)
                    {
                        object o = shellMap.ShellObject;
                        shellMap.ShellObject = null;
                        Utilities.SafeRelease(ref o);
                    }
                }
            }
            
        }

        #region Attached Property Methods

        /// <summary>
        /// Set the JumpList attached property on an Application.
        /// </summary>
        public static void SetJumpList(Application application, JumpList value)
        {
            Verify.IsNotNull(application, "application");

            lock (s_lock)
            {
                // If this was associated with a different application, remove the association.
                JumpList oldValue;
                if (s_applicationMap.TryGetValue(application, out oldValue) && oldValue != null)
                {
                    oldValue._application = null;
                }

                // Associate the jumplist with the application so we can retrieve it later.
                s_applicationMap[application] = value;

                if (value != null)
                {
                    value._application = application;
                }
            }

            if (value != null)
            {
                // Changes will only get applied if the list isn't in an ISupportInitialize block.
                value.ApplyFromApplication();
            }
        }

        /// <summary>
        /// Get the JumpList attached property for an Application.
        /// </summary>
        public static JumpList GetJumpList(Application application)
        {
            Verify.IsNotNull(application, "application");

            JumpList value;
            s_applicationMap.TryGetValue(application, out value);
            return value;
        }

        #endregion

        // static lock to ensure integrity when modifying instances as attached properties on Application.
        private static readonly object s_lock = new object();
        private static readonly Dictionary<Application, JumpList> s_applicationMap = new Dictionary<Application, JumpList>();

        private Application _application;

        // Used to enforce the ISupportInitialize contract.  It's not required to BeginInit to use this class,
        // but it is useful for XAML scenarios so we can apply the changes when both EndInit has been called
        // and the Application attached property has been set.
        private bool? _initializing;

        // The internal list of JumpItems in this JumpList
        private List<JumpItem> _jumpItems;

        public JumpList()
            : this(null, false, false)
        { 
            // Restore the ability to use ISupportInitialize.
            _initializing = null;
        }

        public JumpList(IEnumerable<JumpItem> items, bool showFrequent, bool showRecent)
        {
            if (items != null)
            {
                _jumpItems = new List<JumpItem>(items);
            }
            else 
            {
                _jumpItems = new List<JumpItem>();
            }

            ShowFrequentCategory = showFrequent;
            ShowRecentCategory = showRecent;

            // Using this constructor precludes using ISupportInitialize.
            _initializing = false;
        }

        /// <summary>
        /// Whether to show the special "Frequent" category.
        /// </summary>
        /// <remarks>
        /// This category is managed by the Shell and keeps track of items that are frequently accessed by this program.
        /// Applications can request that specific items are included here by calling JumpList.AddToRecentCategory.
        /// Because of duplication, applications generally should not have both ShowRecentCategory and ShowFrequentCategory set at the same time.
        /// </remarks>
        public bool ShowFrequentCategory {
            get;

            set; }

        /// <summary>
        /// Whether to show the special "Recent" category.
        /// </summary>
        /// <remarks>
        /// This category is managed by the Shell and keeps track of items that have been recently accessed by this program.
        /// Applications can request that specific items are included here by calling JumpList.AddToRecentCategory
        /// Because of duplication, applications generally should not have both ShowRecentCategory and ShowFrequentCategory set at the same time.
        /// </remarks>
        public bool ShowRecentCategory {
            get;

            set; 
        }

        /// <summary>
        /// The list of JumpItems to be in the JumpList.  After a call to Apply this list will contain only those items that were successfully added.
        /// </summary>
        /// <remarks>
        /// This object is not guaranteed to retain its identity after a call to Apply or other implicit setting of the JumpList.
        /// It should be requeried at such times.
        /// </remarks>
        public List<JumpItem> JumpItems
        {
            get { return _jumpItems; }
        }

        private bool IsUnmodified
        {
            get
            {
                return _initializing == null
                    && JumpItems.Count == 0
                    && !ShowRecentCategory
                    && !ShowFrequentCategory;
            }
        }
        #region ISupportInitialize Members

        /// <summary>
        /// Prepare the JumpList for modification.
        /// </summary>
        /// <remarks>
        /// This works in concert with the Application.JumpList attached property.  The JumpList will automatically be applied
        /// to the current application when attached and a corresponding call to EndInit is made.
        /// Nested calls to BeginInit are not allowed.
        /// </remarks>
        public void BeginInit()
        {
            if (!IsUnmodified)
            {
                throw new InvalidOperationException(SR.Get(SRID.JumpList_CantNestBeginInitCalls));
            }

            _initializing = true;
        }

        /// <summary>
        /// Signal the end of initialization of this JumpList.  If it is attached to the current Application, apply the contents of the jump list.
        /// </summary>
        /// <remarks>
        /// Calls to EndInit must be paired with calls to BeginInit.
        /// </remarks>
        public void EndInit()
        {
            if (_initializing != true)
            {
                throw new NotSupportedException(SR.Get(SRID.JumpList_CantCallUnbalancedEndInit));
            }

            _initializing = false;

            // EndInit only implicitly applies the list if the current Application has been set as an attached property.
            ApplyFromApplication();
        }

        #endregion

        /// <summary>
        /// Get the AppUserModelId for the running process.
        /// </summary>
        /// <remarks>
        /// This is a Shell property that currently is only used as part of a heuristic
        /// for what taskbar item an HWND should be associated with, e.g. you can put
        /// windows from multiple processes into the same group, or you can prevent glomming
        /// of HWNDs that would otherwise be shown together.
        /// 
        /// Even though this property isn't exposed on the public WPF OM
        /// we still want to make sure that the jump list gets associated with
        /// the current running app even if the client has explicitly changed the id.
        /// 
        /// It's straightforward to p/invoke to set these for the running application or
        /// the HWND.  Not so much for this object.
        /// </remarks>
        private static string _RuntimeId
        {
            get
            {
                string appId;
                HRESULT hr = NativeMethods2.GetCurrentProcessExplicitAppUserModelID(out appId);
                if (hr == HRESULT.E_FAIL)
                {
                    // This is how Shell signals that the app id hasn't been set.
                    hr = HRESULT.S_OK;
                    appId = null;
                }
                hr.ThrowIfFailed();
                return appId;
            }
        }

        public void Apply()
        {
            if (_initializing == true)
            {
                throw new InvalidOperationException(SR.Get(SRID.JumpList_CantApplyUntilEndInit));
            }

            // After this attempting to use ISupportInitialize is invalid.
            _initializing = false;

            ApplyList();
        }

        private void ApplyFromApplication()
        {
            // If we're here and the caller has modified the JumpList without using ISupportInitialize
            // we still want to apply the changes.
            if (_initializing != true && !IsUnmodified)
            {
                _initializing = false;
            }

            if (_application == Application.Current && _initializing == false)
            {
                // If we're applying due to being an attached property, then don't apply
                // unless we're really on the current application and wait until EndInit
                // has been called, or the list has otherwise been modified.
                ApplyList();
            }
        }

        private void ApplyList()
        {
            Debug.Assert(_initializing == false);
            Verify.IsApartmentState(ApartmentState.STA);

            // We don't want to force applications to conditionally check this before constructing a JumpList,
            // but if we're not on 7 then this isn't going to work.  Fail fast.
            if (!Utilities.IsOSWindows7OrNewer)
            {
                RejectEverything();
                return;
            }

            List<JumpItem> successList;
            List<_RejectedJumpItemPair> rejectedList;

            // Declare these outside the try block so we can cleanup native resources in the _ShellObjectPairs.
            List<List<_ShellObjectPair>> categories = null;
            List<_ShellObjectPair> removedList = null;
            var destinationList = (ICustomDestinationList)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.DestinationList)));
            try
            {
                // Even though we're not exposing Shell's AppModelId concept, we'll still respect it
                // since it's easy to clients to p/invoke to set it for Application and Window, but not for JumpLists
                string appId = _RuntimeId;
                if (!string.IsNullOrEmpty(appId))
                {
                    destinationList.SetAppID(appId);
                }

                // The number ot items visible on a jump list is a user setting.  Shell doesn't reject items based on overflow.
                // We don't bother checking against it because the app can query the setting and manage overflow based on it
                // if they really care.  We'll happily add too many items with the hope that if the user changes the setting
                // items will be recovered from the overflow.
                uint slotsVisible;
                Guid removedIid = new Guid(IID.ObjectArray);
                var objectsRemoved = (IObjectArray)destinationList.BeginList(out slotsVisible, ref removedIid);

                // Keep track of the items that were previously removed by the user.
                // We don't want to pend any items that are contained in this list.
                // It's possible that this contains null JumpItems when we were unable to do the conversion.
                removedList = GenerateJumpItems(objectsRemoved);

                // Keep track of the items that have been successfully pended.
                // IMPORTANT: Ensure at the end of this that if the list is applied again that it would
                //     result in items being added to the JumpList in the same order.
                //     This doesn't mean that they'll be ordered the same as how the user added them
                //     (e.g. categories will be coalesced), but the categories should appear in the same order.
                //     Since when we call AddCategory we're doing it in reverse order, AddCategory augments
                //     the items in the list in reverse as well.  At the end the final list is reversed.
                successList = new List<JumpItem>(JumpItems.Count);

                // Keep track of the items that we couldn't pend, and why.
                rejectedList = new List<_RejectedJumpItemPair>(JumpItems.Count);

                // Need to group the JumpItems based on their categories.
                // The special "Tasks" category doesn't actually have a name and should be first so it's unconditionally added.
                categories = new List<List<_ShellObjectPair>>() { new List<_ShellObjectPair>() };

                // This is not thread-safe.
                // We're traversing the original list so we're vulnerable to another thread modifying it during the enumeration.
                foreach (var jumpItem in JumpItems)
                {
                    if (jumpItem == null)
                    {
                        // App added a null jump item?  Just go through the normal failure mechanisms.
                        rejectedList.Add(new _RejectedJumpItemPair{ JumpItem = jumpItem, Reason = JumpItemRejectionReason.InvalidItem });
                        continue;
                    }

                    object shellObject = null;
                    try
                    {
                        shellObject = GetShellObjectForJumpItem(jumpItem);
                        // If for some reason we couldn't create the item add it to the rejected list.
                        if (shellObject == null)
                        {
                            rejectedList.Add(new _RejectedJumpItemPair { Reason = JumpItemRejectionReason.InvalidItem, JumpItem = jumpItem });
                            continue;
                        }

                        // Don't add this item if it's in the list of items previously removed by the user.
                        if (ListContainsShellObject(removedList, shellObject))
                        {
                            rejectedList.Add(new _RejectedJumpItemPair { Reason = JumpItemRejectionReason.RemovedByUser, JumpItem = jumpItem });
                            continue;
                        }

                        var shellMap = new _ShellObjectPair { JumpItem = jumpItem, ShellObject = shellObject };
                        if (string.IsNullOrEmpty(jumpItem.CustomCategory))
                        {
                            // No custom category, so add to the Tasks list.
                            categories[0].Add(shellMap);
                        }
                        else
                        {
                            // Find the appropriate category and add to that list.
                            // If it doesn't exist, add a new category for it.
                            bool categoryExists = false;
                            foreach (var list in categories)
                            {
                                // The first item in the category list can be used to check the name.
                                if (list.Count > 0 && list[0].JumpItem.CustomCategory == jumpItem.CustomCategory)
                                {
                                    list.Add(shellMap);
                                    categoryExists = true;
                                    break;
                                }
                            }
                            if (!categoryExists)
                            {
                                categories.Add(new List<_ShellObjectPair>() { shellMap });
                            }
                        }

                        // Shell interface is now owned by the category list.
                        shellObject = null;
                    }
                    finally
                    {
                        Utilities.SafeRelease(ref shellObject);
                    }
                }

                // Jump List categories get added top-down, except for "Tasks" which is special and always at the bottom.
                // We want the Recent/Frequent to always be at the top so they get added first.
                // Logically the categories are added bottom-up, but their contents are top-down,
                // so we reverse the order we add the categories to the destinationList.
                // To preserve the item ordering AddCategory also adds items in reverse.
                // We need to reverse the final list when everything is done.
                categories.Reverse();

                if (ShowFrequentCategory)
                {
                    destinationList.AppendKnownCategory(KDC.FREQUENT);
                }

                if (ShowRecentCategory)
                {
                    destinationList.AppendKnownCategory(KDC.RECENT);
                }

                // Now that all the JumpItems are grouped add them to the custom destinations list.
                foreach (List<_ShellObjectPair> categoryList in categories)
                {
                    if (categoryList.Count > 0)
                    {
                        string categoryHeader = categoryList[0].JumpItem.CustomCategory;
                        AddCategory(destinationList, categoryHeader, categoryList, successList, rejectedList);
                    }
                }

                destinationList.CommitList();
            }
            catch
            {
                // It's not okay to throw an exception here.  If Shell is rejecting the JumpList for some reason
                // we don't want to be responsible for the app throwing an exception in its startup path.
                // For common use patterns there isn't really any user code on the stack, so there isn't
                // an opportunity to catch this in the app.
                // We can instead handle this.
                
                // Try to notify the developer if they're hitting this. 
                if (TraceShell.IsEnabled)
                {
                    TraceShell.Trace(TraceEventType.Error, TraceShell.RejectingJumpItemsBecauseCatastrophicFailure);
                }
                RejectEverything();
                return;
            }
            finally
            {
                // Deterministically release native resources.

                Utilities.SafeRelease(ref destinationList);

                if (categories != null)
                {
                    foreach (List<_ShellObjectPair> list in categories)
                    {
                        _ShellObjectPair.ReleaseShellObjects(list);
                    }
                }

                // Note that this only clears the ShellObjects, not the JumpItems.
                // We still need the JumpItems out of this list for the JumpItemsRemovedByUser event.
                _ShellObjectPair.ReleaseShellObjects(removedList);
            }

            // Swap the current list with what we were able to successfully place into the JumpList.
            // Reverse it first to ensure that the items are in a repeatable order.
            successList.Reverse();
            _jumpItems = successList;

            // Raise the events for rejected and removed.
            EventHandler<JumpItemsRejectedEventArgs> rejectedHandler = JumpItemsRejected;
            EventHandler<JumpItemsRemovedEventArgs> removedHandler = JumpItemsRemovedByUser;

            if (rejectedList.Count > 0 && rejectedHandler != null)
            {
                var items = new List<JumpItem>(rejectedList.Count);
                var reasons = new List<JumpItemRejectionReason>(rejectedList.Count);

                foreach (_RejectedJumpItemPair rejectionPair in rejectedList)
                {
                    items.Add(rejectionPair.JumpItem);
                    reasons.Add(rejectionPair.Reason);
                }

                rejectedHandler(this, new JumpItemsRejectedEventArgs(items, reasons));
            }

            if (removedList.Count > 0 && removedHandler != null)
            {
                var items = new List<JumpItem>(removedList.Count);
                foreach (_ShellObjectPair shellMap in removedList)
                {
                    // It's possible that not every shell object could be converted to a JumpItem.
                    if (shellMap.JumpItem != null)
                    {
                        items.Add(shellMap.JumpItem);
                    }
                }

                if (items.Count > 0)
                {
                    removedHandler(this, new JumpItemsRemovedEventArgs(items));
                }
            }
        }

        private static bool ListContainsShellObject(List<_ShellObjectPair> removedList, object shellObject)
        {
            Debug.Assert(removedList != null);
            Debug.Assert(shellObject != null);

            if (removedList.Count == 0)
            {
                return false;
            }

            // Casts in .Net don't AddRef.  Don't need to release these.
            var shellItem = shellObject as IShellItem;
            if (shellItem != null)
            {
                foreach (var shellMap in removedList)
                {
                    var removedItem = shellMap.ShellObject as IShellItem;
                    if (removedItem != null)
                    {
                        if (0 == shellItem.Compare(removedItem, SICHINT.CANONICAL | SICHINT.TEST_FILESYSPATH_IF_NOT_EQUAL))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            var shellLink = shellObject as IShellLinkW;
            if (shellLink != null)
            {
                foreach (var shellMap in removedList)
                {
                    var removedLink = shellMap.ShellObject as IShellLinkW;
                    if (removedLink != null)
                    { 
                        // There's no intrinsic comparison function for ShellLinks.
                        // Talking to the Shell guys, the way they compare these is to catenate a string with
                        // a normalized version of the app path and the unmodified args.
                        // If the two strings ordinally compare, they're the same.
                        string removedLinkString = ShellLinkToString(removedLink);
                        string linkString = ShellLinkToString(shellLink);

                        if (removedLinkString == linkString)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            // Unlikely.  It's not a supported shell interface?
            return false;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// This returns a native COM object that should be deterministically released by the caller, when possible.
        /// </remarks>
        private static object GetShellObjectForJumpItem(JumpItem jumpItem)
        {
            var jumpPath = jumpItem as JumpPath;
            var jumpTask = jumpItem as JumpTask;

            // Either of these create functions could return null if the item is invalid but they shouldn't throw.
            if (jumpPath != null)
            {
                return CreateItemFromJumpPath(jumpPath);
            }
            else if (jumpTask != null)
            { 
                return CreateLinkFromJumpTask(jumpTask, true);
            }

            // Unsupported type?
            Debug.Assert(false);
            return null;
        }

        private static List<_ShellObjectPair> GenerateJumpItems(IObjectArray shellObjects)
        {
            Debug.Assert(shellObjects != null);

            var retList = new List<_ShellObjectPair>();

            Guid unknownIid = new Guid(IID.Unknown);
            uint count = shellObjects.GetCount();
            for (uint i = 0; i < count; ++i)
            {
                // This is potentially a heterogenous list, so get as an IUnknown and QI afterwards.
                object unk = shellObjects.GetAt(i, ref unknownIid);
                JumpItem item = null;
                try
                {
                    item = GetJumpItemForShellObject(unk);
                }
                catch (Exception e)
                {
                    if (e is NullReferenceException || e is System.Runtime.InteropServices.SEHException)
                    {
                        throw;
                    }
                    // If we failed the conversion we still want to keep the shell interface for comparision.
                    // Just leave the JumpItem property as null.
                }
                retList.Add(new _ShellObjectPair { ShellObject = unk, JumpItem = item });
            }

            return retList;
        }

        private static void AddCategory(ICustomDestinationList cdl, string category, List<_ShellObjectPair> jumpItems, List<JumpItem> successList, List<_RejectedJumpItemPair> rejectionList)
        {
            AddCategory(cdl, category, jumpItems, successList, rejectionList, true);
        }

        private static void AddCategory(ICustomDestinationList cdl, string category, List<_ShellObjectPair> jumpItems, List<JumpItem> successList, List<_RejectedJumpItemPair> rejectionList, bool isHeterogenous)
        {
            Debug.Assert(jumpItems.Count != 0);
            Debug.Assert(cdl != null);

            HRESULT hr;
            var shellObjectCollection = (IObjectCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.EnumerableObjectCollection)));

            foreach (var itemMap in jumpItems)
            {
                shellObjectCollection.AddObject(itemMap.ShellObject);
            }

            if (string.IsNullOrEmpty(category))
            {
                hr = cdl.AddUserTasks((IObjectArray)shellObjectCollection);
            }
            else
            {
                hr = cdl.AppendCategory(category, (IObjectArray)shellObjectCollection);
            }

            if (hr.Succeeded)
            {
                // Woot! Add these items to the list.
                // Do it in reverse order so Apply has the items in the correct order.
                for (int i = jumpItems.Count; --i >= 0;)
                {
                    successList.Add(jumpItems[i].JumpItem);
                }
            }
            else
            {
                // If the list contained items that could not be added because this object isn't a handler
                // then drop all ShellItems and retry without them.
                if (isHeterogenous && hr == HRESULT.DESTS_E_NO_MATCHING_ASSOC_HANDLER)
                {
                    if (TraceShell.IsEnabled)
                    {
                        TraceShell.Trace(TraceEventType.Error, TraceShell.RejectingJumpListCategoryBecauseNoRegisteredHandler(category));
                    }

                    Utilities.SafeRelease(ref shellObjectCollection);
                    var linksOnlyList = new List<_ShellObjectPair>();
                    foreach (var itemMap in jumpItems)
                    {
                        if (itemMap.JumpItem is JumpPath)
                        {
                            rejectionList.Add(new _RejectedJumpItemPair { JumpItem = itemMap.JumpItem, Reason = JumpItemRejectionReason.NoRegisteredHandler });
                        }
                        else
                        {
                            linksOnlyList.Add(itemMap);
                        }
                    }
                    if (linksOnlyList.Count > 0)
                    {
                        // There's not a reason I know of that we should reject a list of only links...
                        Debug.Assert(jumpItems.Count != linksOnlyList.Count);
                        AddCategory(cdl, category, linksOnlyList, successList, rejectionList, false);
                    }
                }
                else
                {
                    Debug.Assert(HRESULT.DESTS_E_NO_MATCHING_ASSOC_HANDLER != hr);
                    // If we failed for some other reason, just reject everything.
                    foreach (var item in jumpItems)
                    {
                        rejectionList.Add(new _RejectedJumpItemPair { JumpItem = item.JumpItem, Reason = JumpItemRejectionReason.InvalidItem });
                    }
                }
            }
        }

        private static readonly string _FullName;
        
        #region Converter methods

        private static IShellLinkW CreateLinkFromJumpTask(JumpTask jumpTask, bool allowSeparators)
        {
            Debug.Assert(jumpTask != null);

            // Title is generally required.  If it's missing we need to treat this like a separator.
            // Everything else can still appear on separator elements,
            // but separators can only exist in the Tasks category.
            if (string.IsNullOrEmpty(jumpTask.Title))
            {
                if (!allowSeparators || !string.IsNullOrEmpty(jumpTask.CustomCategory))
                {
                    // Just treat this situation as an InvalidItem.
                    return null;
                }
            }

            var link = (IShellLinkW)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.ShellLink)));
            try
            {
                string appPath = _FullName;
                if (!string.IsNullOrEmpty(jumpTask.ApplicationPath))
                {
                    appPath = jumpTask.ApplicationPath;
                }

                link.SetPath(appPath);

                // This is optional.  Don't set it if the app hasn't explicitly requested it.
                if (!string.IsNullOrEmpty(jumpTask.WorkingDirectory))
                {
                    // Don't verify this.  It's possible that the directory doesn't exist now, but it will later.
                    // Shell handles this fine when we try to set an improperly formatted path.
                    link.SetWorkingDirectory(jumpTask.WorkingDirectory);
                }

                if (!string.IsNullOrEmpty(jumpTask.Arguments))
                {
                    link.SetArguments(jumpTask.Arguments);
                }

                // -1 is a sentinel value indicating not to use the icon.
                if (jumpTask.IconResourceIndex != -1)
                {
                    string resourcePath = _FullName;
                    if (!string.IsNullOrEmpty(jumpTask.IconResourcePath))
                    {
                        // Shell bug (Windows 7 595770): IShellLink doesn't correctly limit icon location path to MAX_PATH.
                        // It's really too bad we have to enforce this here.  When the shortcut gets
                        // serialized it streams the full string.  On deserialization it only retrieves
                        // MAX_PATH for this field leaving junk behind for subsequent gets, leading to data corruption.
                        // Because we don't want to allow the app to do create something that we know may
                        // be corrupt we have to enforce this ourselves.  If Shell fixes this later then
                        // we need to remove this check to let them handle this as they see fit.
                        // If they fix it by supporting longer paths, then we're artificially constraining this value...
                        if (jumpTask.IconResourcePath.Length >= Win32Constant.MAX_PATH)
                        {
                            // we could throw the exception here, but we're already globally catching everything.
                            return null;
                        }
                        resourcePath = jumpTask.IconResourcePath;
                    }
                    link.SetIconLocation(resourcePath, jumpTask.IconResourceIndex);
                }

                if (!string.IsNullOrEmpty(jumpTask.Description))
                {
                    link.SetDescription(jumpTask.Description);
                }

                IPropertyStore propStore = (IPropertyStore)link;
                var pv = new PROPVARIANT();
                try
                {
                    PKEY pkey = default(PKEY);

                    if (!string.IsNullOrEmpty(jumpTask.Title))
                    {
                        pv.SetValue(jumpTask.Title);
                        pkey = PKEY.Title;
                    }
                    else
                    {
                        pv.SetValue(true);
                        pkey = PKEY.AppUserModel_IsDestListSeparator;
                    }

                    propStore.SetValue(ref pkey, pv);
                }
                finally
                {
                    Utilities.SafeDispose(ref pv);
                }

                propStore.Commit();

                IShellLinkW retLink = link;
                link = null;
                return retLink;
            }
            catch (Exception)
            {
                // IShellLinkW::Set* methods tend to return E_FAIL when trying to set invalid data.
                // The create methods don't explicitly check for these kinds of errors.

                // If we aren't able to create the item for any reason just return null to indicate an invalid item.
                return null;
            }
            finally
            {
                Utilities.SafeRelease(ref link);
            }
        }

        private static IShellItem2 CreateItemFromJumpPath(JumpPath jumpPath)
        {
            Debug.Assert(jumpPath != null);

            try
            {
                // This will return null if the path doesn't exist.
                return ShellUtil.GetShellItemForPath(Path.GetFullPath(jumpPath.Path));
            }
            catch (Exception)
            {
                // Don't propagate exceptions here.  If we couldn't create the item, it's just invalid.
            }

            return null;
        }

        private static JumpItem GetJumpItemForShellObject(object shellObject)
        {
            var shellItem = shellObject as IShellItem2;
            var shellLink = shellObject as IShellLinkW;

            if (shellItem != null)
            {
                JumpPath path = new JumpPath
                {
                    Path = shellItem.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING),
                };
                return path;
            }

            if (shellLink != null)
            {
                var pathBuilder = new StringBuilder(Win32Constant.MAX_PATH);
                shellLink.GetPath(pathBuilder, pathBuilder.Capacity, null, SLGP.RAWPATH);
                var argsBuilder = new StringBuilder(Win32Constant.INFOTIPSIZE);
                shellLink.GetArguments(argsBuilder, argsBuilder.Capacity);
                var descBuilder = new StringBuilder(Win32Constant.INFOTIPSIZE);
                shellLink.GetDescription(descBuilder, descBuilder.Capacity);
                var iconBuilder = new StringBuilder(Win32Constant.MAX_PATH);
                int iconIndex;
                shellLink.GetIconLocation(iconBuilder, iconBuilder.Capacity, out iconIndex);
                var dirBuilder = new StringBuilder(Win32Constant.MAX_PATH);
                shellLink.GetWorkingDirectory(dirBuilder, dirBuilder.Capacity);

                JumpTask task = new JumpTask
                {
                    // Set ApplicationPath and IconResources, even if they're from the current application.
                    // This means that equivalent JumpTasks won't necessarily compare property-for-property.
                    ApplicationPath = pathBuilder.ToString(),
                    Arguments = argsBuilder.ToString(),
                    Description = descBuilder.ToString(),
                    IconResourceIndex = iconIndex,
                    IconResourcePath = iconBuilder.ToString(),
                    WorkingDirectory = dirBuilder.ToString(),
                };

                using (PROPVARIANT pv = new PROPVARIANT())
                {
                    var propStore = (IPropertyStore)shellLink;
                    PKEY pkeyTitle = PKEY.Title;

                    propStore.GetValue(ref pkeyTitle, pv);

                    // PKEY_Title should be an LPWSTR if it's not empty.
                    task.Title = pv.GetValue() ?? "";
                }

                return task;
            }

            // Unsupported type?
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Generate a unique string for the ShellLink that can be used for equality checks.
        /// </summary>
        private static string ShellLinkToString(IShellLinkW shellLink)
        {
            var pathBuilder = new StringBuilder(Win32Constant.MAX_PATH);
            shellLink.GetPath(pathBuilder, pathBuilder.Capacity, null, SLGP.RAWPATH);

            string title = null;
            // Need to use the property store to get the title for the link.
            using (PROPVARIANT pv = new PROPVARIANT())
            {
                var propStore = (IPropertyStore)shellLink;
                PKEY pkeyTitle = PKEY.Title;

                propStore.GetValue(ref pkeyTitle, pv);

                // PKEY_Title should be an LPWSTR if it's not empty.
                title = pv.GetValue() ?? "";
            }

            var argsBuilder = new StringBuilder(Win32Constant.INFOTIPSIZE);
            shellLink.GetArguments(argsBuilder, argsBuilder.Capacity);

            // Path and title should be case insensitive.
            // Shell treats arguments as case sensitive because apps can handle those differently.
            return pathBuilder.ToString().ToUpperInvariant() + title.ToUpperInvariant() + argsBuilder.ToString();
        }

        #endregion

        private void RejectEverything()
        {
            EventHandler<JumpItemsRejectedEventArgs> handler = JumpItemsRejected;
            if (handler == null)
            {
                _jumpItems.Clear();
                return;
            }

            if (_jumpItems.Count > 0)
            {
                var reasons = new List<JumpItemRejectionReason>(_jumpItems.Count);
                for (int i = 0; i < _jumpItems.Count; ++i)
                {
                    reasons.Add(JumpItemRejectionReason.InvalidItem);
                }
                // We're rejecting everything,
                // so create an event args with the original list and then clear it.
                var args = new JumpItemsRejectedEventArgs(_jumpItems, reasons);
                _jumpItems.Clear();

                handler(this, args);
            }
        }

        public event EventHandler<JumpItemsRejectedEventArgs> JumpItemsRejected;

        public event EventHandler<JumpItemsRemovedEventArgs> JumpItemsRemovedByUser;
    }
}
