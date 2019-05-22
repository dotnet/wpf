// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: BlockUIContainer - a wrapper for embedded UIElements in text 
//    flow content block collections
//

using System.ComponentModel;        // DesignerSerializationVisibility
using System.Windows.Markup; // ContentProperty

namespace System.Windows.Documents
{
    /// <summary>
    /// BlockUIContainer - a wrapper for embedded UIElements in text 
    /// flow content block collections
    /// </summary>
    [ContentProperty("Child")]
    public class BlockUIContainer : Block
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of BlockUIContainer element.
        /// </summary>
        /// <remarks>
        /// The purpose of this element is to be a wrapper for UIElements
        /// when they are embedded into text flow - as items of
        /// BlockCollections.
        /// </remarks>
        public BlockUIContainer()
            : base()
        {
        }

        /// <summary>
        /// Initializes an BlockUIContainer specifying its child UIElement
        /// </summary>
        /// <param name="uiElement">
        /// UIElement set as a child of this block item
        /// </param>
        public BlockUIContainer(UIElement uiElement)
            : base()
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }
            this.Child = uiElement;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Properties

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        public UIElement Child
        {
            get
            {
                return this.ContentStart.GetAdjacentElement(LogicalDirection.Forward) as UIElement;
            }

            set
            {
                TextContainer textContainer = this.TextContainer;

                textContainer.BeginChange();
                try
                {
                    TextPointer contentStart = this.ContentStart;

                    UIElement child = Child;
                    if (child != null)
                    {
                        textContainer.DeleteContentInternal(contentStart, this.ContentEnd);
                        ContainerTextElementField.ClearValue(child);
                    }

                    if (value != null)
                    {
                        ContainerTextElementField.SetValue(value, this);
                        contentStart.InsertUIElement(value);
                    }
                }
                finally
                {
                    textContainer.EndChange();
                }
            }
        }

        #endregion
    }
}
