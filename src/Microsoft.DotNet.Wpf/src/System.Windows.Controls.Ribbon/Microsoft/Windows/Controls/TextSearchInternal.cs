// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Diagnostics;
using System.Xml;
#if RIBBON_IN_FRAMEWORK
using System.Windows.Controls.Ribbon;
#else
using Microsoft.Windows.Controls.Ribbon;
#endif

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     Almost same as TextSearch, except for the public properties (i.e. TextPath), which are used from TextSearch.
    ///     Text Search is a feature that allows the user to quickly access items in a set by typing prefixes of the strings.
    /// </summary>
    internal sealed class TextSearchInternal : DependencyObject
	{
        /// <summary>
        ///     Make a new TextSearchInternal instance attached to the given object.
        ///     Create the instance in the same context as the given DO.
        /// </summary>
        /// <param name="itemsControl"></param>
        private TextSearchInternal(ItemsControl itemsControl)
        {
            if (itemsControl == null)
            {
                throw new ArgumentNullException("itemsControl");
            }

            _attachedTo = itemsControl;

            ResetState();
        }

        /// <summary>
        ///     Get the instance of TextSearchInternal attached to the given ItemsControl or make one and attach it if it's not.
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <returns></returns>
        internal static TextSearchInternal EnsureInstance(ItemsControl itemsControl)
        {
            TextSearchInternal instance = (TextSearchInternal)itemsControl.GetValue(TextSearchInternalInstanceProperty);

            if (instance == null)
            {
                instance = new TextSearchInternal(itemsControl);
                itemsControl.SetValue(TextSearchInternalInstancePropertyKey, instance);
            }

            return instance;
        }

#if DEBUG

        #region Properties

        /// <summary>
        ///     Prefix that is currently being used in the algorithm.
        /// </summary>
        private static readonly DependencyProperty CurrentPrefixProperty =
            DependencyProperty.RegisterAttached("CurrentPrefix", typeof(string), typeof(TextSearchInternal),
                                                new FrameworkPropertyMetadata((string)null));

        /// <summary>
        ///     If TextSearchInternal is currently active.
        /// </summary>
        private static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(TextSearchInternal),
                                                new FrameworkPropertyMetadata(false));

        #endregion

#endif

        #region Private Properties

        /// <summary>
        ///     The key needed to set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey TextSearchInternalInstancePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("TextSearchInternalInstance", typeof(TextSearchInternal), typeof(TextSearchInternal),
                                                new FrameworkPropertyMetadata((object)null /* default value */));

        /// <summary>
        ///     Instance of TextSearchInternal -- attached property so that the instance can be stored on the element
        ///     which wants the service.
        /// </summary>
        private static readonly DependencyProperty TextSearchInternalInstanceProperty =
            TextSearchInternalInstancePropertyKey.DependencyProperty;

        #endregion

        #region Private Methods

        /// <summary>
        /// TextSearchInternal in an ItemsControl containing ItemsControls as children (i.e. RibbonGallery)
        /// performs TextSearchInternal on the second level Items.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <returns></returns>
        internal bool DoHierarchicalSearch(string nextChar)
        {
            bool repeatedChar = false;

            // If they pressed the same character as last time, we will do the fallback search.
            //     Fallback search is if they type "bob" and then press "b"
            //     we'll look for "bobb" and when we don't find it we should
            //     find the next item starting with "bob".
            if (_charsEntered.Count > 0
                && (String.Compare(_charsEntered[_charsEntered.Count - 1], nextChar, true, GetCulture(_attachedTo)) == 0))
            {
                repeatedChar = true;
            }

            bool wasNewCharUsed = false;

            int matchedItemIndex = -1;

            int startItemsControlIndex = 0;

            // If TextSearchInternal is not active, then we should start
            // the search from the beginning.  If it is active, we should
            // start the search from the ItemsControl containing the currently-matched item.
            if (IsActive)
            {
                startItemsControlIndex = MatchedItemsControlIndex;
            }

            int itemCount = _attachedTo.Items.Count;
            for (int currentIndex = startItemsControlIndex; currentIndex < itemCount; )
            {
                // Skip over filtered categories
                ItemsControl childItemsControl = _attachedTo.ItemContainerGenerator.ContainerFromIndex(currentIndex) as ItemsControl;
                if (childItemsControl != null && childItemsControl.Visibility == Visibility.Visible)
                {
                    int startItemIndex = 0;

                    // If TextSearchInternal is not active, then we should start
                    // the search from the beginning.  If it is active, we should
                    // start the search from the currently-matched item.
                    if (IsActive)
                    {
                        startItemIndex = MatchedItemIndex;
                    }

                    ItemCollection itemCollection = childItemsControl.Items as ItemCollection;

                    // Get the primary TextPath from the child ItemsControl
                    string primaryTextPath = GetPrimaryTextPath(childItemsControl, true);

                    matchedItemIndex = FindMatchingPrefix(childItemsControl, primaryTextPath, Prefix, nextChar, startItemIndex, repeatedChar, ref wasNewCharUsed, 
                        /* hierarchicalSearch */ true, 
                        /* fallbackFirstItem */ currentIndex != startItemsControlIndex);

                    // If there was an item that matched, move to that item in the collection
                    if (matchedItemIndex != -1)
                    {
                        // Don't have to move currency if it didn't actually move.
                        // startItemIndex is the index of the current item only if IsActive is true,
                        // So, we have to move currency when IsActive is false.
                        if (!IsActive || matchedItemIndex != startItemIndex || currentIndex != startItemsControlIndex)
                        {
                            object matchedItem = itemCollection[matchedItemIndex];
                            
                            // Let the control decide what to do with matched-item
                            RibbonHelper.NavigateToItem(childItemsControl, matchedItemIndex , null);

                            // Store current match
                            MatchedItemIndex = matchedItemIndex;
                            MatchedItemsControlIndex = currentIndex;
                        }

                        // Update the prefix if it changed
                        if (wasNewCharUsed)
                        {
                            AddCharToPrefix(nextChar);
                        }

                        // User has started typing (successfully), so we're active now.
                        if (!IsActive)
                        {
                            IsActive = true;
                        }
                    }

                    // Reset the timeout and remember this character, but only if we're
                    // active -- this is because if we got called but the match failed
                    // we don't need to set up a timeout -- no state needs to be reset.
                    if (IsActive)
                    {
                        ResetTimeout();
                    }

                    // Found a match so exit the loop.
                    if (matchedItemIndex != -1)
                    {
                        return true;
                    }

                    // Reset matchedItemIndex because we are moving to the next childItemsControl
                    MatchedItemIndex = 0;
                }

                // Move next and wrap-around if we pass the end of the container.
                currentIndex++;
                if (currentIndex >= itemCount)
                {
                    currentIndex = 0;
                }

                // Stop where we started but only after the first pass
                // through the loop -- we should process the startItem.
                if (currentIndex == startItemsControlIndex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        ///     Called by consumers of TextSearchInternal when a TextInput event is received
        ///     to kick off the algorithm.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <returns></returns>
        internal bool DoSearch(string nextChar)
        {
            bool repeatedChar = false;

            int startItemIndex = 0;

            ItemCollection itemCollection = _attachedTo.Items as ItemCollection;

            // If TextSearchInternal is not active, then we should start
            // the search from the beginning.  If it is active, we should
            // start the search from the currently-matched item.
            if (IsActive)
            {
                // ISSUE: This falls victim to duplicate elements being in the view.
                //        To mitigate this, we could remember ItemUI ourselves.

                startItemIndex = MatchedItemIndex;
            }

            // If they pressed the same character as last time, we will do the fallback search.
            //     Fallback search is if they type "bob" and then press "b"
            //     we'll look for "bobb" and when we don't find it we should
            //     find the next item starting with "bob".
            if (_charsEntered.Count > 0
                && (String.Compare(_charsEntered[_charsEntered.Count - 1], nextChar, true, GetCulture(_attachedTo))==0))
            {
                repeatedChar = true;
            }

            // Get the primary TextPath from the ItemsControl to which we are attached.
            string primaryTextPath = GetPrimaryTextPath(_attachedTo, false);

            bool wasNewCharUsed = false;

            int matchedItemIndex = FindMatchingPrefix(_attachedTo, primaryTextPath, Prefix,nextChar, startItemIndex, repeatedChar, ref wasNewCharUsed);

            // If there was an item that matched, move to that item in the collection
            if (matchedItemIndex != -1)
            {
                // Don't have to move currency if it didn't actually move.
                // startItemIndex is the index of the current item only if IsActive is true,
                // So, we have to move currency when IsActive is false.
                if (!IsActive || matchedItemIndex != startItemIndex)
                {
                    object matchedItem = itemCollection[matchedItemIndex];
                    // Let the control decide what to do with matched-item

                    RibbonHelper.NavigateToItem(_attachedTo, matchedItemIndex , null);
                    
                    // Store current match
                    MatchedItemIndex = matchedItemIndex;
                }

                // Update the prefix if it changed
                if (wasNewCharUsed)
                {
                    AddCharToPrefix(nextChar);
                }

                // User has started typing (successfully), so we're active now.
                if (!IsActive)
                {
                    IsActive = true;
                }
            }

            // Reset the timeout and remember this character, but only if we're
            // active -- this is because if we got called but the match failed
            // we don't need to set up a timeout -- no state needs to be reset.
            if (IsActive)
            {
                ResetTimeout();
            }

            return (matchedItemIndex != -1);
        }

        /// <summary>
        ///     Called when the user presses backspace.
        /// </summary>
        /// <returns></returns>
        internal bool DeleteLastCharacter()
        {
            if (IsActive)
            {
                // Remove the last character from the prefix string.
                // Get the last character entered and then remove a string of
                // that length off the prefix string.
                if (_charsEntered.Count > 0)
                {
                    string lastChar = _charsEntered[_charsEntered.Count - 1];
                    string prefix = Prefix;

                    _charsEntered.RemoveAt(_charsEntered.Count - 1);
                    Prefix = prefix.Substring(0, prefix.Length - lastChar.Length);

                    ResetTimeout();

                    return true;
                }
            }

            return false;
        }

        private static int FindMatchingPrefix(ItemsControl itemsControl, string primaryTextPath, 
                                               string prefix, string newChar, int startItemIndex, bool lookForFallbackMatchToo, ref bool wasNewCharUsed)
        {
            return FindMatchingPrefix(itemsControl, primaryTextPath, prefix, newChar, startItemIndex, lookForFallbackMatchToo, ref wasNewCharUsed,
                false, false);
        }

        /// <summary>
        ///     Searches through the given itemCollection for the first item matching the given prefix.
        /// </summary>
        /// <remarks>
        ///     --------------------------------------------------------------------------
        ///     Incremental Type Search algorithm
        ///     --------------------------------------------------------------------------
        ///
        ///     Given a prefix and new character, we loop through all items in the collection
        ///     and look for an item that starts with the new prefix.  If we find such an item,
        ///     select it.  If the new character is repeated, we look for the next item after
        ///     the current one that begins with the old prefix**.  We can optimize by
        ///     performing both of these searches in parallel.
        ///
        ///     **NOTE: Win32 will only do this if the old prefix is of length 1 - in other
        ///             words, first-character-only matching.  The algorithm described here
        ///             is an extension of ITS as implemented in Win32.  This variant was
        ///             described to me by JeffBog as what was done in AFC - but I have yet
        ///             to find a listbox which behaves this way.
        ///
        ///     --------------------------------------------------------------------------
        /// </remarks>
        /// <returns>Item that matches the given prefix</returns>
        private static int FindMatchingPrefix(ItemsControl itemsControl, string primaryTextPath, 
                                               string prefix, string newChar, int startItemIndex, bool lookForFallbackMatchToo, ref bool wasNewCharUsed, bool inHierarchicalSearch, bool fallbackFirstItemToo)
        {
            ItemCollection itemCollection = itemsControl.Items;

            // Using indices b/c this is a better way to uniquely
            // identify an element in the collection.
            int matchedItemIndex = -1;
            int fallbackMatchIndex = -1;

            int count = itemCollection.Count;

            // Return immediately with no match if there were no items in the view.
            if (count == 0)
            {
                return -1;
            }

            string newPrefix = prefix + newChar;

            // With an empty prefix, we'd match anything
            if (String.IsNullOrEmpty(newPrefix))
            {
                return -1;
            }

            bool firstItem = true;

            wasNewCharUsed = false;

            CultureInfo cultureInfo = GetCulture(itemsControl);

            // ISSUE: what about changing the collection while this is running?
            for (int currentIndex = startItemIndex; currentIndex < count; )
            {
                object item = itemCollection[currentIndex];

                if (item != null)
                {
                    string itemString = GetPrimaryText(item, primaryTextPath);

                    // See if the current item matches the newPrefix, if so we can
                    // stop searching and accept this item as the match.
                    if (itemString != null && itemString.StartsWith(newPrefix, /* ignoreCase */ true, cultureInfo))
                    {
                        // Accept the new prefix as the current prefix.
                        wasNewCharUsed = true;
                        matchedItemIndex = currentIndex;
                        break;
                    }

                    // Find the next string that matches the last prefix.  This
                    // string will be used in the case that the new prefix isn't
                    // matched. This enables pressing the last character multiple
                    // times and cylcing through the set of items that match that
                    // prefix.
                    //
                    // Unlike the above search, this search must start *after*
                    // the currently selected item.  This search also shouldn't
                    // happen if there was no previous prefix to match against
                    //
                    // In hierarchical Search the first item should also be considered 
                    // for fallbackSearch. 
                    if (lookForFallbackMatchToo)
                    {
                        if ((!firstItem || fallbackFirstItemToo) && !string.IsNullOrEmpty(prefix))
                        {
                            if (itemString != null)
                            {
                                if (fallbackMatchIndex == -1 && itemString.StartsWith(prefix, /* ignoreCase */ true, cultureInfo))
                                {
                                    fallbackMatchIndex = currentIndex;
                                }
                            }
                        }
                        else
                        {
                            firstItem = false;
                        }
                    }
                }

                // Move next and wrap-around if we pass the end of the container.
                // In a hierachical TextSearchInternal we dont want to wrap around Items in a childItemsControl
                // Instead we move to the next childItemsControl.
                currentIndex++;
                if (!inHierarchicalSearch && currentIndex >= count)
                {
                    currentIndex = 0;
                }

                // Stop where we started but only after the first pass
                // through the loop -- we should process the startItem.
                if (currentIndex == startItemIndex)
                {
                    break;
                }
            }

            // In the case that the new prefix didn't match anything and
            // there was a fallback match that matched the old prefix, move
            // to that one.
            if (matchedItemIndex == -1 && fallbackMatchIndex != -1)
            {
                matchedItemIndex = fallbackMatchIndex;
            }

            return matchedItemIndex;
        }

        /// <summary>
        ///     Helper function called by Editable ComboBox to search through items.
        /// </summary>
        internal static int FindMatchingPrefix(ItemsControl itemsControl, string prefix, bool doHierarchicalSearch)
        {
            bool wasNewCharUsed = false;

            return FindMatchingPrefix(itemsControl, GetPrimaryTextPath(itemsControl, doHierarchicalSearch), prefix, String.Empty, 0, false, ref wasNewCharUsed);
        }

        /// <summary>
        ///     Helper function called by Editable ComboBox to search through items.
        /// </summary>
        internal static object FindMatchingPrefix(ItemsControl itemsControl, string prefix, bool doHierarchicalSearch, out ItemsControl matchedChildItemsControl)
        {
            matchedChildItemsControl = null;

            if (doHierarchicalSearch)
            {
                foreach (object item in itemsControl.Items)
                {
                    // Skip over filtered categories
                    ItemsControl childItemsControl = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as ItemsControl;
                    if (childItemsControl != null && childItemsControl.Visibility == Visibility.Visible)
                    {
                        int matchedIndex = FindMatchingPrefix(childItemsControl, prefix, doHierarchicalSearch);
                        if (matchedIndex != -1)
                        {
                            matchedChildItemsControl = childItemsControl;
                            return childItemsControl.Items[matchedIndex];
                        }
                    }
                }
            }
            else
            {
                int matchedIndex = FindMatchingPrefix(itemsControl, prefix, doHierarchicalSearch);
                if (matchedIndex != -1)
                {
                    return itemsControl.Items[matchedIndex];
                }
            }

            return null;
        }

        private void ResetTimeout()
        {
            // Called when we get some input. Start or reset the timer.
            // Queue an inactive priority work item and set its deadline.
            if (_timeoutTimer == null)
            {
                _timeoutTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _timeoutTimer.Tick += new EventHandler(OnTimeout);
            }
            else
            {
                _timeoutTimer.Stop();
            }

            // Schedule this operation to happen a certain number of milliseconds from now.
            _timeoutTimer.Interval = TimeOut;
            _timeoutTimer.Start();
        }

        private void AddCharToPrefix(string newChar)
        {
            Prefix += newChar;
            _charsEntered.Add(newChar);
        }

        private static string GetPrimaryTextPath(ItemsControl itemsControl, bool doHierarchicalSearch)
        {
            string primaryTextPath = (string)itemsControl.GetValue(TextSearch.TextPathProperty);

            if (String.IsNullOrEmpty(primaryTextPath))
            {
                primaryTextPath = itemsControl.DisplayMemberPath;
            }

            if (String.IsNullOrEmpty(primaryTextPath) && doHierarchicalSearch)
            {
                // Fallback on ParentItemsControl
                ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(itemsControl);
                if (parentItemsControl != null)
                {
                    primaryTextPath = (string)parentItemsControl.GetValue(TextSearch.TextPathProperty);
                }

                if (String.IsNullOrEmpty(primaryTextPath) && doHierarchicalSearch)
                {
                    // Fallback on GrandParentItemsControl
                    ItemsControl grandParentItemsControl = ItemsControl.ItemsControlFromItemContainer(parentItemsControl);
                    if (grandParentItemsControl != null)
                    {
                        primaryTextPath = (string)grandParentItemsControl.GetValue(TextSearch.TextPathProperty);
                    }
                }
            }

            return primaryTextPath;
        }

        private static string GetPrimaryText(object item, string primaryTextPath)
        {
            // Order of precedence for getting Primary Text is as follows:
            //
            // 1) PrimaryText
            // 2) PrimaryTextPath (TextSearch.TextPath or ItemsControl.DisplayMemberPath)
            // 3) GetPlainText()
            // 4) ToString()

            DependencyObject itemDO = item as DependencyObject;

            if (itemDO != null)
            {
                string primaryText = (string)itemDO.GetValue(TextSearch.TextProperty);

                if (!String.IsNullOrEmpty(primaryText))
                {
                    return primaryText;
                }
            }

            // Here hopefully they've supplied a path into their object which we can use.
            if (!string.IsNullOrEmpty(primaryTextPath))
            {
                // Take the binding and apply it to a dummy ContentControl with Source as current item.  
                // Then read the value of the Content property,
                // Try to convert the resulting object to a string.

                DummyElement.DataContext = item;
                DummyElement.BindingPath = primaryTextPath;
                object primaryText = DummyElement.Content;
                
                return ConvertToPlainText(primaryText);
            }

            return ConvertToPlainText(item);
        }

        private static string ConvertToPlainText(object o)
        {
            // Alernative to FE.GetPlainText internal method 
            if (o is FrameworkElement)
            {
                HeaderedContentControl hcc;
                ContentControl cc;
                HeaderedItemsControl hic;
                TextBlock textBlock;
                TextBox textBox;

                if ((hcc = o as HeaderedContentControl) != null)
                {
                    return ConvertToPlainText(hcc.Header);
                }
                else if ((cc = o as ContentControl) != null)
                {
                    return ConvertToPlainText(cc.Content);
                }
                else if ((hic = o as HeaderedItemsControl) != null)
                {
                    return ConvertToPlainText(hic.Header);
                }
                else if ((textBlock = o as TextBlock) != null)
                {
                    return textBlock.Text;
                }
                else if ((textBox = o as TextBox) != null)
                {
                    return textBox.Text;
                }
            }

            // Try to convert the item to a string
            return (o != null) ? o.ToString() : String.Empty;
        }

        /// <summary>
        ///     Internal helper method that uses the same primary text lookup steps but doesn't require
        ///     the user passing in all of the bindings that we need.
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static string GetPrimaryTextFromItem(ItemsControl itemsControl, object item, bool doHierarchicalSearch)
        {
            if (item == null)
                return String.Empty;

            return GetPrimaryText(item, GetPrimaryTextPath(itemsControl, doHierarchicalSearch));
        }

        private static Binding CreateBinding(object item, string primaryTextPath)
        {
            Binding binding = new Binding();
            binding.Mode = BindingMode.OneWay;

            // Use xpath for xmlnodes (See Selector.PrepareItemValueBinding)
            if (AssemblyHelper.IsXmlNode(item))
            {
                binding.XPath = primaryTextPath;
                binding.Path = new PropertyPath("/InnerText");
            }
            else
            {
                binding.Path = new PropertyPath(primaryTextPath);
            }

            return binding;
        }

        private void OnTimeout(object sender, EventArgs e)
        {
            ResetState();
        }

        private void ResetState()
        {
            // Reset the prefix string back to empty.
            IsActive = false;
            Prefix = String.Empty;
            MatchedItemIndex = -1;
            MatchedItemsControlIndex = -1;
            if (_charsEntered == null)
            {
                _charsEntered = new List<string>(10);
            }
            else
            {
                _charsEntered.Clear();
            }

            if(_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
            }
            _timeoutTimer = null;

        }

        /// <summary>
        ///     Time until the search engine resets.
        /// </summary>
        private static TimeSpan TimeOut
        {
            get
            {
                // NOTE: NtUser does the following (file: windows/ntuser/kernel/sysmet.c)
                //     gpsi->dtLBSearch = dtTime * 4;            // dtLBSearch   =  4  * gdtDblClk
                //     gpsi->dtScroll = gpsi->dtLBSearch / 5;  // dtScroll     = 4/5 * gdtDblClk
                //
                // 4 * DoubleClickSpeed seems too slow for the search
                // So for now we'll do 2 * DoubleClickSpeed

                return TimeSpan.FromMilliseconds(NativeMethods.GetDoubleClickTime() * 2);
            }
        }

        #endregion

        #region Testing API

        // Being that this is a time-sensitive operation, it's difficult
        // to get the timing right in a DRT.  I'll leave input testing up to BVTs here
        // but this internal API is for the DRT to do basic coverage.
        private static TextSearchInternal GetInstance(DependencyObject d)
        {
            return EnsureInstance(d as ItemsControl);
        }

        private void TypeAKey(string c)
        {
            DoSearch(c);
        }

        private void CauseTimeOut()
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
                OnTimeout(_timeoutTimer, EventArgs.Empty);
            }
        }

        internal string GetCurrentPrefix()
        {
            return Prefix;
        }

        #endregion


        #region Internal Accessibility API

        internal static string GetPrimaryText(FrameworkElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            string text = (string)element.GetValue(TextSearch.TextProperty);

            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return element.ToString();
        }

        #endregion

        #region Private Fields

        private string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value;

#if DEBUG
                // Also need to invalidate the property CurrentPrefixProperty on the instance to which we are attached.
                Debug.Assert(_attachedTo != null);

                _attachedTo.SetValue(CurrentPrefixProperty, _prefix);
#endif
            }
        }

        private bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;

#if DEBUG
                Debug.Assert(_attachedTo != null);

                _attachedTo.SetValue(IsActiveProperty, _isActive);
#endif
            }
        }

        private int MatchedItemIndex
        {
            get { return _matchedItemIndex; }
            set
            {
                _matchedItemIndex = value;
            }
        }

        /// <summary>
        /// For a hierachical TextSearchInternal store the index of the last matched child ItemsControl of _attachedTo.
        /// </summary>
        private int MatchedItemsControlIndex
        {
            get { return _matchedItemsControlIndex; }
            set { _matchedItemsControlIndex = value; }
        }

        private static DummyObject DummyElement
        {
            get
            {
                if (_dummyElement == null)
                {
                    _dummyElement = new DummyObject();
                }
                return _dummyElement;
            }
        }

        private static CultureInfo GetCulture(DependencyObject element)
        {
            object o = element.GetValue(FrameworkElement.LanguageProperty);
            CultureInfo culture = null;

            if (o != null)
            {
                XmlLanguage language = (XmlLanguage) o;
                try
                {
                    culture = language.GetSpecificCulture();
                }
                catch (InvalidOperationException)
                {
                }
            }

            return culture;
        }

        // Element to which this TextSearchInternal instance is attached.
        private ItemsControl _attachedTo;

        // String of characters matched so far.
        private string _prefix;

        private List<string> _charsEntered;

        private bool _isActive;

        private int _matchedItemIndex, _matchedItemsControlIndex;

        private DispatcherTimer _timeoutTimer;

        [ThreadStatic]
        private static DummyObject _dummyElement = new DummyObject();

        #endregion

        #region DummyObject

        private class DummyObject : FrameworkElement
        {
            private static DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(DummyObject));

            public object Content
            {
                get { return GetValue(ContentProperty); }
                set { SetValue(ContentProperty, value); }
            }

            public string BindingPath
            {
                get { return _bindingPath; }
                set
                {
                    if (!string.Equals(_bindingPath, value))
                    {
                        _bindingPath = value;
                        Binding binding = new Binding();
                        if (DataContext != null && AssemblyHelper.IsXmlNode(DataContext))
                        {
                            // Use xpath for xmlnodes (See Selector.PrepareItemValueBinding)
                            binding.XPath = _bindingPath;
                            binding.Path = new PropertyPath("/InnerText");
                        }
                        else
                        {
                            binding.Path = new PropertyPath(_bindingPath);
                        }

                        binding.Mode = BindingMode.OneWay;
                        SetBinding(ContentProperty, binding);
                    }
                }
            }

            private string _bindingPath; 
        }

        #endregion
    }

    internal static class AssemblyHelper
    {
        // return true if the item is an XmlNode
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static bool IsXmlNode(object item)
        {
            return item is XmlNode;
        }
    }
}

