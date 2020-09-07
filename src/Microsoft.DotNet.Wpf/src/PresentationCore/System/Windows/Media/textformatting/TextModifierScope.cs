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
    /// Represents a single "frame" in the stack of text modifiers. The stack 
    /// is represented not as an array, but as a linked structure in which each 
    /// frame points to its parent.
    /// </summary>
    internal sealed class TextModifierScope
    {
        private TextModifierScope _parentScope;
        private TextModifier _modifier;
        private int _cp;

        /// <summary>
        /// Constructs a new text modification state object.
        /// </summary>
        /// <param name="parentScope">Parent scope, i.e., the previous top of the stack.</param>
        /// <param name="modifier">Text modifier run fetched from the client.</param>
        /// <param name="cp">Text source character index of the run.</param>
        internal TextModifierScope(TextModifierScope parentScope, TextModifier modifier, int cp)
        {
            _parentScope = parentScope;
            _modifier = modifier;
            _cp = cp;
        }

        /// <summary>
        /// Next item in the stack of text modifiers.
        /// </summary>
        public TextModifierScope ParentScope
        {
            get { return _parentScope; }
        }

        /// <summary>
        /// Text modifier run fetched from the client.
        /// </summary>
        public TextModifier TextModifier
        {
            get { return _modifier; }
        }

        /// <summary>
        /// Character index of the text modifier run.
        /// </summary>
        public int TextSourceCharacterIndex
        {
            get { return _cp; }
        }

        /// <summary>
        /// Modifies the specified text run properties by invoking the modifier at
        /// the current scope and all containing scopes.
        /// </summary>
        /// <param name="properties">Properties to modify.</param>
        /// <returns>Returns the text run properties after modification.</returns>
        internal TextRunProperties ModifyProperties(TextRunProperties properties)
        {
            for (TextModifierScope scope = this; scope != null; scope = scope._parentScope)
            {
                properties = scope._modifier.ModifyProperties(properties);
            }
            return properties;
        }

        /// <summary>
        /// Performs a deep copy of the stack of TextModifierScope objects.
        /// </summary>
        /// <returns>Returns the top of the new stack.</returns>
        internal TextModifierScope CloneStack()
        {
            TextModifierScope top = new TextModifierScope(null, _modifier, _cp);
            TextModifierScope scope = top;

            for (TextModifierScope source = _parentScope; source != null; source = source._parentScope)
            {
                scope._parentScope = new TextModifierScope(null, source._modifier, source._cp);
                scope = scope._parentScope;
            }

            return top;
        }
    }
}
