// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text modification API
//
//  Spec:      http://avalon/text/DesignDocsAndSpecs/Text%20Formatting%20API.doc
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Specialized text run used to modify properties of text runs in its scope.
    /// The scope extends to the next matching EndOfSegment text run (matching
    /// because text modifiers may be nested), or to the next EndOfParagraph.
    /// </summary>
    public abstract class TextModifier : TextRun
    {
        /// <summary>
        /// Reference to character buffer
        /// </summary>
        public sealed override CharacterBufferReference CharacterBufferReference
        {
            get { return new CharacterBufferReference(); }
        }

        /// <summary>
        /// Modifies the properties of a text run.
        /// </summary>
        /// <param name="properties">Properties of a text run or the return value of
        /// ModifyProperties for a nested text modifier.</param>
        /// <returns>Returns the actual text run properties to be used for formatting,
        /// subject to further modification by text modifiers at outer scopes.</returns>
        public abstract TextRunProperties ModifyProperties(TextRunProperties properties);

        /// <Summary> 
        /// TextFormatter to ask whether directional embedding is
        /// represented by this modifier.
        /// </Summary>
        public abstract bool HasDirectionalEmbedding {get; }

        /// <Summary>
        /// TextFormatter to get the flow direction value for directional
        /// embedding. The value is ignored unless the property 
        /// HasDirectionalEmbedding returns true.
        /// </Summary>
        public abstract FlowDirection FlowDirection  {get; }
    }
}
