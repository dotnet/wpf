// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Input;

using System.Collections;
using MS.Win32;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using System.Text;
using System.Collections.Generic;
using MS.Internal;
using MS.Internal.Data;

namespace System.Windows.Controls
{
    // NTUser allows multiple selection while SHIFT is down.  We should consider adopting this behavior (or similar).
    //
    // We have thoughts about how to give visual feedback about the text you are typing.  This requires locating
    //       the actual text element within the object.  This is probably more work than we will be able to do in M8.
    //       Need to discuss with MSX before committing to something here.
    //
    // Win32 listbox also seems to reset TypeSearch when selection changes or focus moves out.
    //       This means we need to track SelectionChanged and IsKeyboardFocusWithinChanged (or equivalent).

    /// <summary>
    ///     Text Search is a feature that allows the user to quickly access items in a set by typing prefixes of the strings.
    /// </summary>
    public sealed class TextSearch : DependencyObject
	{
        /// <summary>
        ///     Make a new TextSearch instance attached to the given object.
        ///     Create the instance in the same context as the given DO.
        /// </summary>
        /// <param name="itemsControl"></param>
        private TextSearch(ItemsControl itemsControl)
        {
            if (itemsControl == null)
            {
                throw new ArgumentNullException("itemsControl");
            }

            _attachedTo = itemsControl;

            ResetState();
        }

        /// <summary>
        ///     Get the instance of TextSearch attached to the given ItemsControl or make one and attach it if it's not.
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <returns></returns>
        internal static TextSearch EnsureInstance(ItemsControl itemsControl)
        {
            TextSearch instance = (TextSearch)itemsControl.GetValue(TextSearchInstanceProperty);

            if (instance == null)
            {
                instance = new TextSearch(itemsControl);
                itemsControl.SetValue(TextSearchInstancePropertyKey, instance);
            }

            return instance;
        }

        #region Text and TextPath Properties

        /// <summary>
        ///     Attached property to indicate which property on the item in the items collection to use for the "primary" text,
        ///     or the text against which to search.
        /// </summary>
        public static readonly DependencyProperty TextPathProperty
            = DependencyProperty.RegisterAttached("TextPath", typeof(string), typeof(TextSearch),
                                                  new FrameworkPropertyMetadata(String.Empty /* default value */));

        /// <summary>
        ///     Writes the attached property to the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="path"></param>
        public static void SetTextPath(DependencyObject element, string path)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(TextPathProperty, path);
        }

        /// <summary>
        ///     Reads the attached property from the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static string GetTextPath(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (string)element.GetValue(TextPathProperty);
        }

        /// <summary>
        ///     Attached property to indicate the value to use for the "primary" text of an element.
        /// </summary>
        public static readonly DependencyProperty TextProperty
            = DependencyProperty.RegisterAttached("Text", typeof(string), typeof(TextSearch),
                                                  new FrameworkPropertyMetadata((string)String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        ///     Writes the attached property to the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="text"></param>
        public static void SetText(DependencyObject element, string text)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(TextProperty, text);
        }

        /// <summary>
        ///     Reads the attached property from the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static string GetText(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (string)element.GetValue(TextProperty);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Prefix that is currently being used in the algorithm.
        /// </summary>
        private static readonly DependencyProperty CurrentPrefixProperty =
            DependencyProperty.RegisterAttached("CurrentPrefix", typeof(string), typeof(TextSearch),
                                                new FrameworkPropertyMetadata((string)null));

        /// <summary>
        ///     If TextSearch is currently active.
        /// </summary>
        private static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached("IsActive", typeof(bool), typeof(TextSearch),
                                                new FrameworkPropertyMetadata(false));

        #endregion

        #region Private Properties

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey TextSearchInstancePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("TextSearchInstance", typeof(TextSearch), typeof(TextSearch),
                                                new FrameworkPropertyMetadata((object)null /* default value */));

        /// <summary>
        ///     Instance of TextSearch -- attached property so that the instance can be stored on the element
        ///     which wants the service.
        /// </summary>
        private static readonly DependencyProperty TextSearchInstanceProperty =
            TextSearchInstancePropertyKey.DependencyProperty;


        // used to retrieve the value of an item, according to the TextPath
        private static readonly BindingExpressionUncommonField TextValueBindingExpression = new BindingExpressionUncommonField();

        #endregion

        #region Private Methods

        /// <summary>
        ///     Called by consumers of TextSearch when a TextInput event is received
        ///     to kick off the algorithm.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <returns></returns>
        internal bool DoSearch(string nextChar)
        {
            bool repeatedChar = false;

            int startItemIndex = 0;

            ItemCollection itemCollection = _attachedTo.Items as ItemCollection;

            // If TextSearch is not active, then we should start
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
            string primaryTextPath = GetPrimaryTextPath(_attachedTo);

            bool wasNewCharUsed = false;

            int matchedItemIndex = FindMatchingPrefix(_attachedTo, primaryTextPath, Prefix,
                                                      nextChar, startItemIndex, repeatedChar, ref wasNewCharUsed);

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
                    _attachedTo.NavigateToItem(matchedItem, matchedItemIndex, new ItemsControl.ItemNavigateArgs(Keyboard.PrimaryDevice, ModifierKeys.None));
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

        /// <summary>
        /// Gets the length of the prefix (the prefix of matchedText matched by newText) and the rest of the string from the matchedText
        /// It takes care of compressions or expansions in both matchedText and newText  which could be impacting the length of the string
        /// For example: length of prefix would be 5 and the rest would be 2 if matchedText is "Grosses" and newText is ""Groﬂ"
        /// length of prefix would be 4 and the rest would be 2 if matchedText is ""Groﬂes" and newText is "Gross" as "ﬂ" = "ss"
        /// </summary>
        /// /// <param name="matchedText">string that is assumed to contain prefix which matches newText</param>
        /// <param name="newText">string that is assumed to match a prefix of matchedText</param>
        private static void GetMatchingPrefixAndRemainingTextLength(string matchedText, string newText, CultureInfo cultureInfo,
                                                                bool ignoreCase, out int matchedPrefixLength, out int textExcludingPrefixLength)
        {
            Debug.Assert(String.IsNullOrEmpty(matchedText) == false, "matchedText cannot be null or empty");
            Debug.Assert(String.IsNullOrEmpty(newText) == false, "newText cannot be null or empty");
            Debug.Assert(matchedText.StartsWith(newText, ignoreCase, cultureInfo), "matchedText should start with newText");
            
            matchedPrefixLength = 0;
            textExcludingPrefixLength = 0;

            if (matchedText.Length < newText.Length)
            {
                matchedPrefixLength = matchedText.Length;
                textExcludingPrefixLength = 0;
            }
            else
            {
                // mostly compression or expansion is not involved. So start with length of newText
                int i = newText.Length;
                int j = i + 1;
                
                do
                {
                    string temp;

                    if (i >= 1)
                    {
                        temp = matchedText.Substring(0, i);
                        if (String.Compare(newText, temp, ignoreCase, cultureInfo) == 0)
                        {
                            matchedPrefixLength = i;
                            textExcludingPrefixLength = matchedText.Length - i;
                            break;
                        }
                    }
                    if (j <= matchedText.Length)
                    {
                        temp = matchedText.Substring(0, j);
                        if (String.Compare(newText, temp, ignoreCase, cultureInfo) == 0)
                        {
                            matchedPrefixLength = j;
                            textExcludingPrefixLength = matchedText.Length - j;
                            break;
                        }
                    }

                    i--;
                    j++;
                } while (i >= 1 || j <= matchedText.Length);
            }
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
        private static int FindMatchingPrefix(ItemsControl itemsControl, string primaryTextPath, string prefix,
                                               string newChar, int startItemIndex, bool lookForFallbackMatchToo, ref bool wasNewCharUsed)
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

            // Hook up the binding we will apply to each object.  Get the
            // PrimaryTextPath off of the attached instance and then make
            // a binding with that path.

            BindingExpression primaryTextBinding = null;

            object item0 = itemsControl.Items[0];
            bool useXml = SystemXmlHelper.IsXmlNode(item0);

            if (useXml || !String.IsNullOrEmpty(primaryTextPath))
            {
                primaryTextBinding = CreateBindingExpression(itemsControl, item0, primaryTextPath);
                TextValueBindingExpression.SetValue(itemsControl, primaryTextBinding);
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
                    string itemString = GetPrimaryText(item, primaryTextBinding, itemsControl);
                    bool isTextSearchCaseSensitive = itemsControl.IsTextSearchCaseSensitive;

                    // See if the current item matches the newPrefix, if so we can
                    // stop searching and accept this item as the match.
                    if (itemString != null && itemString.StartsWith(newPrefix, !isTextSearchCaseSensitive, cultureInfo))
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
                    if (lookForFallbackMatchToo)
                    {
                        if (!firstItem && prefix != String.Empty)
                        {
                            if (itemString != null)
                            {
                                if (fallbackMatchIndex == -1 && itemString.StartsWith(prefix, !isTextSearchCaseSensitive, cultureInfo))
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
                currentIndex++;
                if (currentIndex >= count)
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

            if (primaryTextBinding != null)
            {
                // Clean up the binding for the primary text path.
                TextValueBindingExpression.ClearValue(itemsControl);
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
        internal static MatchedTextInfo FindMatchingPrefix(ItemsControl itemsControl, string prefix)
        {
            MatchedTextInfo matchedTextInfo;
            bool wasNewCharUsed = false;
            
            int matchedItemIndex =  FindMatchingPrefix(itemsControl, GetPrimaryTextPath(itemsControl), prefix, String.Empty, 0, false, ref wasNewCharUsed);

            // There could be compressions or expansions in either matched text or inputted text which means
            // length of the prefix in the matched text and length of the inputted text could be different
            // for example: "Grosses" would match for the input text "Groﬂ" where the prefix length in matched text is 5
            // whereas the length of the inputted text is 4. Same matching rule applies for the other way as well with
            // "Groﬂ" in matched text for the inputted text "Gross"
            if (matchedItemIndex >= 0)
            {
                int matchedPrefixLength;
                int textExcludingPrefixLength;
                CultureInfo cultureInfo = GetCulture(itemsControl);
                bool ignoreCase = itemsControl.IsTextSearchCaseSensitive;
                string matchedText = GetPrimaryTextFromItem(itemsControl, itemsControl.Items[matchedItemIndex]);

                GetMatchingPrefixAndRemainingTextLength(matchedText, prefix, cultureInfo, !ignoreCase, out matchedPrefixLength, out textExcludingPrefixLength);
                matchedTextInfo = new MatchedTextInfo(matchedItemIndex, matchedText, matchedPrefixLength, textExcludingPrefixLength);
            }
            else
            {
                matchedTextInfo = MatchedTextInfo.NoMatch;
            }

            return matchedTextInfo;
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

        private static string GetPrimaryTextPath(ItemsControl itemsControl)
        {
            string primaryTextPath = (string)itemsControl.GetValue(TextPathProperty);

            if (String.IsNullOrEmpty(primaryTextPath))
            {
                primaryTextPath = itemsControl.DisplayMemberPath;
            }
            return primaryTextPath;
        }

        private static string GetPrimaryText(object item, BindingExpression primaryTextBinding, DependencyObject primaryTextBindingHome)
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
                string primaryText = (string)itemDO.GetValue(TextProperty);

                if (!String.IsNullOrEmpty(primaryText))
                {
                    return primaryText;
                }
            }

            // Here hopefully they've supplied a path into their object which we can use.
            if (primaryTextBinding != null && primaryTextBindingHome != null)
            {
                // Take the binding that we hooked up at the beginning of the search
                // and apply it to the current item.  Then, read the value of the
                // ItemPrimaryText property (where the binding actually lives).
                // Try to convert the resulting object to a string.
                primaryTextBinding.Activate(item);

                object primaryText = primaryTextBinding.Value;

                return ConvertToPlainText(primaryText);
            }

            return ConvertToPlainText(item);
        }

        private static string ConvertToPlainText(object o)
        {
            FrameworkElement fe = o as FrameworkElement;

            // Try to return FrameworkElement.GetPlainText()
            if (fe != null)
            {
                string text = fe.GetPlainText();

                if (text != null)
                {
                    return text;
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
        internal static string GetPrimaryTextFromItem(ItemsControl itemsControl, object item)
        {
            if (item == null)
                return String.Empty;

            BindingExpression primaryTextBinding = CreateBindingExpression(itemsControl, item, GetPrimaryTextPath(itemsControl));
            TextValueBindingExpression.SetValue(itemsControl, primaryTextBinding);

            string primaryText = GetPrimaryText(item, primaryTextBinding, itemsControl);

            TextValueBindingExpression.ClearValue(itemsControl);

            return primaryText;
        }

        private static BindingExpression CreateBindingExpression(ItemsControl itemsControl, object item, string primaryTextPath)
        {
            Binding binding = new Binding();

            // Use xpath for xmlnodes (See Selector.PrepareItemValueBinding)
            if (SystemXmlHelper.IsXmlNode(item))
            {
                binding.XPath = primaryTextPath;
                binding.Path = new PropertyPath("/InnerText");
            }
            else
            {
                binding.Path = new PropertyPath(primaryTextPath);
            }

            binding.Mode = BindingMode.OneWay;
            binding.Source = null;
            return (BindingExpression)BindingExpression.CreateUntargetedBindingExpression(itemsControl, binding);
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
        private TimeSpan TimeOut
        {
            get
            {
                // NOTE: NtUser does the following (file: windows/ntuser/kernel/sysmet.c)
                //     gpsi->dtLBSearch = dtTime * 4;            // dtLBSearch   =  4  * gdtDblClk
                //     gpsi->dtScroll = gpsi->dtLBSearch / 5;  // dtScroll     = 4/5 * gdtDblClk
                //
                // 4 * DoubleClickSpeed seems too slow for the search
                // So for now we'll do 2 * DoubleClickSpeed

                return TimeSpan.FromMilliseconds(SafeNativeMethods.GetDoubleClickTime() * 2);
            }
        }

        #endregion

        #region Testing API

        // Being that this is a time-sensitive operation, it's difficult
        // to get the timing right in a DRT.  I'll leave input testing up to BVTs here
        // but this internal API is for the DRT to do basic coverage.
        private static TextSearch GetInstance(DependencyObject d)
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

            string text = (string)element.GetValue(TextProperty);

            if (text != null && text != String.Empty)
            {
                return text;
            }

            return element.GetPlainText();
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

        // Element to which this TextSearch instance is attached.
        private ItemsControl _attachedTo;

        // String of characters matched so far.
        private string _prefix;

        private List<string> _charsEntered;

        private bool _isActive;

        private int _matchedItemIndex;

        private DispatcherTimer _timeoutTimer;

        #endregion
    }
}
