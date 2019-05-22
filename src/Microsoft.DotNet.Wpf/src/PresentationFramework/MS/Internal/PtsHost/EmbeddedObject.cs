// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Definition for embedded object inside a text paragraph.
//

using System;
using System.Windows;
using MS.Internal.Documents;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// EmbeddedObject class stores information about object embedded within 
    /// a text paragraph. 
    /// </summary>
    internal abstract class EmbeddedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">
        /// Embedded object's character position.
        /// </param>
        protected EmbeddedObject(int dcp)
        {
            Dcp = dcp;
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        internal virtual void Dispose() 
        { 
        }

        /// <summary>
        /// Update object using date from another embedded object of the same 
        /// type.
        /// </summary>
        /// <param name="newObject">
        /// Source of updated data.
        /// </param>
        internal abstract void Update(EmbeddedObject newObject);

        /// <summary>
        /// Embedded object's owner.
        /// </summary>
        internal abstract DependencyObject Element 
        { 
            get; 
        }

        /// <summary>
        /// Position within a text paragraph (number of characters from the 
        /// beginning of text paragraph).
        /// </summary>
        internal int Dcp;
    }

    /// <summary>
    /// Stores information about attached object embedded within a text paragraph.
    /// </summary>
    internal class AttachedObject : EmbeddedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">
        /// Attached object's character position.
        /// </param>
        /// <param name="para">
        /// Paragraph associated with attached object.
        /// </param>
        internal AttachedObject(int dcp, BaseParagraph para)
            : base(dcp)
        {
            Para = para;
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        internal override void Dispose()
        {
            Para.Dispose();
            Para = null;
            base.Dispose();
        }

        /// <summary>
        /// Update object using date from another attached object of the same 
        /// type.
        /// </summary>
        /// <param name="newObject">
        /// Source of updated data.
        /// </param>
        internal override void Update(EmbeddedObject newObject)
        {
            AttachedObject newAttachedObject = newObject as AttachedObject;
            ErrorHandler.Assert(newAttachedObject != null, ErrorHandler.EmbeddedObjectTypeMismatch);
            ErrorHandler.Assert(newAttachedObject.Element.Equals(Element), ErrorHandler.EmbeddedObjectOwnerMismatch);
            Dcp = newAttachedObject.Dcp;
            Para.SetUpdateInfo(PTS.FSKCHANGE.fskchInside, false);
        }

        /// <summary>
        /// Attached object's owner.
        /// </summary>
        internal override DependencyObject Element 
        { 
            get 
            { 
                return Para.Element; 
            } 
        }

        /// <summary>
        /// Paragraph associated with attached object.
        /// </summary>
        internal BaseParagraph Para;
    }


    /// <summary>
    /// Stores information about inline object embedded within a text line.
    /// </summary>
    internal sealed class InlineObject : EmbeddedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">
        /// Embedded object's character position.
        /// </param>
        /// <param name="uiElementIsland">
        /// UIElementIsland associated with embedded object.
        /// </param>
        /// <param name="para">
        /// TextParagraph associated with embedded object.
        /// </param>
        internal InlineObject(int dcp, UIElementIsland uiElementIsland, TextParagraph para)
            : base(dcp)
        {
            _para = para;
            _uiElementIsland = uiElementIsland;
            _uiElementIsland.DesiredSizeChanged += new DesiredSizeChangedEventHandler(_para.OnUIElementDesiredSizeChanged);
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        internal override void Dispose()
        {
            if (_uiElementIsland != null)
            {
                _uiElementIsland.DesiredSizeChanged -= new DesiredSizeChangedEventHandler(_para.OnUIElementDesiredSizeChanged);
            }
            base.Dispose();
        }

        /// <summary>
        /// Update object using date from another embedded object of the same 
        /// type. 
        /// </summary>
        /// <param name="newObject">
        /// Source of updated data.
        /// </param>
        internal override void Update(EmbeddedObject newObject)
        {
            // These should definitely be the same
            InlineObject newInline = newObject as InlineObject;

            ErrorHandler.Assert(newInline != null, ErrorHandler.EmbeddedObjectTypeMismatch);
            ErrorHandler.Assert(newInline._uiElementIsland == _uiElementIsland, ErrorHandler.EmbeddedObjectOwnerMismatch);

            // We've ensured ownership is still with the old InlineObject, so we now null this out to prevent dispose from disconnecting.
            newInline._uiElementIsland = null;
        }

        /// <summary>
        /// Embedded object's owner.
        /// </summary>
        internal override DependencyObject Element 
        { 
            get 
            {
                return _uiElementIsland.Root; 
            } 
        }

        private UIElementIsland _uiElementIsland;
        private TextParagraph _para;
    }

    /// <summary>
    /// Stores information about figure embedded within a text paragraph.
    /// </summary>
    internal sealed class FigureObject : AttachedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">
        /// Figure object's character position.
        /// </param>
        /// <param name="para">
        /// Paragraph associated with figure object.
        /// </param>
        internal FigureObject(int dcp, FigureParagraph para)
            : base(dcp, para)
        {
        }
    }

    /// <summary>
    /// Stores information about floater embedded within a text paragraph.
    /// </summary>
    internal sealed class FloaterObject : AttachedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dcp">
        /// Floater object's character position.
        /// </param>
        /// <param name="para">
        /// Paragraph associated with floater object.
        /// </param>
        internal FloaterObject(int dcp, FloaterParagraph para)
            : base(dcp, para)
        {
        }
    }
}

