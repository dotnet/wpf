// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AnnotationComponentChooser
//

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Component;

namespace System.Windows.Annotations
{
    /// <summary>
    /// Instances of this class choose an IAnnotationComponent for a given AttachedAnnotation.
    /// The AnnotationService.Chooser DP is used to set such instances on the application tree.
    /// </summary>
    internal sealed class AnnotationComponentChooser
    {
        #region Public Statics

        /*
         *  This member is not used in V1.  Its only used to set no chooser but in V1 we don't 
         *  expose changing the chooser.  We have one and only one chooser.
         * 
        /// <summary>
        /// Singleton to set no chooser to be used.
        /// </summary>
        public static readonly AnnotationComponentChooser None = new NoAnnotationComponentChooser();
         * 
         */

        #endregion Public Statics

        #region Constructors
        
        /// <summary>
        /// Return a default AnnotationComponentChooser
        /// </summary>
        public AnnotationComponentChooser() { }

        #endregion Constructors

        #region Public Methods
        
        /// <summary>
        /// Choose an IAnnotationComponent for a given IAttachedAnnotation.  Implementation in AnnotationComponentChooser knows
        /// about all out-of-box IAnnotationComponents.  The default mapping will be stated here later.
        /// Subclasses can overwrite this method to return application specific mapping.
        /// Note: In future release this method should be made virtual.
        /// </summary>
        /// <param name="attachedAnnotation">The IAttachedAnnotation that needs an IAnnotationComponent </param>
        /// <returns></returns>
        public IAnnotationComponent ChooseAnnotationComponent(IAttachedAnnotation attachedAnnotation)
        {           
            if (attachedAnnotation == null) throw new ArgumentNullException("attachedAnnotation");
                                    
            IAnnotationComponent ac = null;

            // Text StickyNote
            if (attachedAnnotation.Annotation.AnnotationType == StickyNoteControl.TextSchemaName)
            {
                ac = new StickyNoteControl(StickyNoteType.Text) as IAnnotationComponent;
            }
            // Ink StickyNote
            else if (attachedAnnotation.Annotation.AnnotationType == StickyNoteControl.InkSchemaName)
            {
                ac = new StickyNoteControl(StickyNoteType.Ink) as IAnnotationComponent;
            }
            // Highlight
            else if (attachedAnnotation.Annotation.AnnotationType == HighlightComponent.TypeName)
            {
                ac = new HighlightComponent() as IAnnotationComponent;
            }

            return ac;
        }

        #endregion Public Methods

        #region Private Classes
        /*
         * This class won't be used in V1.  We have one and only one chooser.  There's no way to change it.
         * 
        /// <summary>
        /// There is only one instance of this class (in AnnotationComponentChooser.None), it always returns null for any given IAttachedAnnotation.
        /// It does not throw an exception for a null attached annotation.
        /// It indicates that no choosing should be performed in the subtree that the instance is attached to.
        /// </summary>
        private class NoAnnotationComponentChooser : AnnotationComponentChooser
        {
            public override IAnnotationComponent ChooseAnnotationComponent(IAttachedAnnotation attachedAnnotation)
            {
                return null;
            }
        }
         * 
         */
        #endregion Private Classes
    }
}
