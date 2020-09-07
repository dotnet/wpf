// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Speller properties for TextBoxBase.
//

namespace System.Windows.Controls
{
    using System.Threading;
    using System.Windows.Documents;
    using System.Windows.Controls.Primitives;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Collections;
    using System.Windows.Markup;

    /// <summary>
    /// Speller properties for TextBoxBase.    
    /// </summary>
    public sealed class SpellCheck
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Ctor.
        internal SpellCheck(TextBoxBase owner)
        {
            _owner = owner;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Enables and disables spell checking within the associated TextBoxBase.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool IsEnabled
        {
            get
            {
                return (bool)_owner.GetValue(IsEnabledProperty);
            }

            set
            {
                _owner.SetValue(IsEnabledProperty, value);
            }
        }
        
        /// <summary>
        /// Enables and disables spell checking within a TextBoxBase.
        /// </summary>
        public static void SetIsEnabled(TextBoxBase textBoxBase, bool value)
        {
            if (textBoxBase == null)
            {
                throw new ArgumentNullException("textBoxBase");
            }

            textBoxBase.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Gets if spell checking is enabled within a TextBoxBase.
        /// </summary>
        public static bool GetIsEnabled(TextBoxBase textBoxBase)
        {
            if (textBoxBase == null)
            {
                throw new ArgumentNullException("textBoxBase");
            }

            return (bool)textBoxBase.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Enables and disables spell checking within the associated TextBoxBase.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public static readonly DependencyProperty IsEnabledProperty =
                DependencyProperty.RegisterAttached(
                        "IsEnabled",
                        typeof(bool),
                        typeof(SpellCheck),
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsEnabledChanged)));

        /// <summary>
        /// The spelling reform mode for the associated TextBoxBase.
        /// </summary>
        /// <remarks>
        /// In languages with reformed spelling rules (such as German or French),
        /// this property specifies whether to apply old (prereform) or new
        /// (postreform) spelling rules to examined text.
        /// </remarks>
        public SpellingReform SpellingReform
        {
            get
            {
                return (SpellingReform)_owner.GetValue(SpellingReformProperty);
            }

            set
            {
                _owner.SetValue(SpellingReformProperty, value);
            }
        }

       
        /// <summary>
        /// Sets the spelling reform mode for a TextBoxBase.
        /// </summary>
        public static void SetSpellingReform(TextBoxBase textBoxBase, SpellingReform value)
        {
            if (textBoxBase == null)
            {
                throw new ArgumentNullException("textBoxBase");
            }

            textBoxBase.SetValue(SpellingReformProperty, value);
        }

        /// <summary>
        /// The spelling reform mode for the associated TextBoxBase.
        /// </summary>
        /// <remarks>
        /// In languages with reformed spelling rules (such as German or French),
        /// this property specifies whether to apply old (prereform) or new
        /// (postreform) spelling rules to examined text.
        /// </remarks>
        public static readonly DependencyProperty SpellingReformProperty =
                DependencyProperty.RegisterAttached(
                        "SpellingReform",
                        typeof(SpellingReform),
                        typeof(SpellCheck),
                        new FrameworkPropertyMetadata(Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName == "de" ? SpellingReform.Postreform : SpellingReform.PreAndPostreform,
                                                      new PropertyChangedCallback(OnSpellingReformChanged)));

        /// <summary>
        /// Custom dictionary locations
        /// </summary>
        public IList CustomDictionaries
        {
            get
            {
                return (IList)_owner.GetValue(CustomDictionariesProperty);
            }
        }
        
        /// <summary>
        /// Gets the collection of custom dictionaries used for spell checking of custom words.
        /// </summary>
        /// <param name="textBoxBase"></param>
        /// <returns></returns>
        public static IList GetCustomDictionaries(TextBoxBase textBoxBase)
        {
            if (textBoxBase == null)
            {
                throw new ArgumentNullException("textBoxBase");
            }
            return (IList)textBoxBase.GetValue(CustomDictionariesProperty);
        }

        private static readonly DependencyPropertyKey CustomDictionariesPropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                        "CustomDictionaries",
                        typeof(IList),
                        typeof(SpellCheck),
                        new FrameworkPropertyMetadata(new DictionaryCollectionFactory()));
        
        /// <summary>
        /// Attached property representing location of custom dicitonaries for given <see cref="TextBoxBase"/>
        /// </summary>
        public static readonly DependencyProperty CustomDictionariesProperty = CustomDictionariesPropertyKey.DependencyProperty;        

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Callback for changes to the IsEnabled property.
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBoxBase = d as TextBoxBase;

            if (textBoxBase != null)
            {
                TextEditor textEditor = TextEditor._GetTextEditor(textBoxBase);

                if (textEditor != null)
                {
                    textEditor.SetSpellCheckEnabled((bool)e.NewValue);
                    if ((bool)e.NewValue != (bool)e.OldValue)
                    {
                        textEditor.SetCustomDictionaries((bool)e.NewValue);
                    }
                }
            }
        }

        // SpellingReformProperty change callback.
        private static void OnSpellingReformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBoxBase = d as TextBoxBase;

            if (textBoxBase != null)
            {
                TextEditor textEditor = TextEditor._GetTextEditor(textBoxBase);

                if (textEditor != null)
                {
                    textEditor.SetSpellingReform((SpellingReform)e.NewValue);
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Internal Types
        //
        //------------------------------------------------------
        internal class DictionaryCollectionFactory : MS.Internal.DefaultValueFactory
        {
            internal DictionaryCollectionFactory()
            { }
            internal override object DefaultValue
            {
                get
                {
                    return null;
                }
            }
            internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
            {
                return new CustomDictionarySources(owner as TextBoxBase);
            }
        }
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields


        // TextBoxBase mapped to this object.
        private readonly TextBoxBase _owner;

        #endregion Private Fields
    }
}
