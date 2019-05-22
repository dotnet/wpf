// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Section element. 
//

using System.ComponentModel;
using System.Windows.Markup; // ContentProperty

namespace System.Windows.Documents 
{
    /// <summary>
    /// Section element. It is an element which can contain a sequence of Block elements.
    /// </summary>
    [ContentProperty("Blocks")]
    public class Section : Block
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a Section class.
        /// </summary>
        public Section() 
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of a Section class specifying a first Block child for it.
        /// </summary>
        /// <param name="block">
        /// Block element added to a Section as its first child.
        /// </param>
        public Section(Block block)
            : base()
        {
            if (block == null)
            {
                throw new ArgumentNullException("block");
            }

            this.Blocks.Add(block);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The HasTrailingParagraphBreakOnPaste property specifies if paragraph break for the last paragraph 
        /// in serialized clipboard format should be included upon paste or not. 
        /// It is intended for use by clipboard serialization purpose: 
        /// only on wrapping root <see cref="Section"/> element.
        /// Setting this property for regular <see cref="Section"/> elements in documents does not have any effect.
        /// </summary>
        /// <remarks>
        /// This is not a <see cref="DependencyProperty"/>, because mechanisms like data binding, animation, styling
        /// are not supposed to work for it.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DefaultValue(true)]
        public bool HasTrailingParagraphBreakOnPaste
        {
            get 
            {
                return !_ignoreTrailingParagraphBreakOnPaste;
            }
            set 
            {
                _ignoreTrailingParagraphBreakOnPaste = !value;
            }
        }

        internal const string HasTrailingParagraphBreakOnPastePropertyName = "HasTrailingParagraphBreakOnPaste";

        /// <value>
        /// Collection of Blocks contained in this Section.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BlockCollection Blocks
        {
            get
            {
                return new BlockCollection(this, /*isOwnerParent*/true);
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(XamlDesignerSerializationManager manager) 
        {
            return manager != null && manager.XmlWriter == null;
        }

        #endregion

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private bool _ignoreTrailingParagraphBreakOnPaste;

        #endregion Private Fields
    }
}
