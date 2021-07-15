// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*
\***************************************************************************/
using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

namespace System.Windows.Markup
{
    // Data maintained on the reader's context stack.  The root of the tree is at the bottom
    // of the stack.
    internal class ReaderContextStackData
    {
        //
        // NOTE:  If you add a field here, be sure to update ClearData
        //
        ReaderFlags  _contextFlags;
        object       _contextData;
        object       _contextKey;
        string        _uid;
        string        _name;
        object       _contentProperty;
        Type         _expectedType;
        short        _expectedTypeId;
        bool         _createUsingTypeConverter;
        //
        // NOTE:  If you add a field here, be sure to update ClearData
        //
        



        // Returns just the part of the flags field corresponding to the context type
        internal ReaderFlags ContextType
        {
            get { return (ReaderFlags)(_contextFlags & ReaderFlags.ContextTypeMask); }
        }

        // The data object for this point in the stack.  Typically the element at
        // this scoping level
        internal object ObjectData
        {
            get { return _contextData; }
            set { _contextData = value; }
        }

        // The key attribute defined for the current context, whose parent context is expected
        // to be an IDictionary
        internal object Key
        {
            get { return _contextKey; }
            set { _contextKey = value; }
        }

        // The x:Uid of this object, if it has one and has been read yet.
        internal string Uid
        {
            get { return _uid; }
            set { _uid = value; }
        }

        // The x:Name (or Name) of this object, if it has one and has been read yet.
        // Alternatively if this context object represents a property this member 
        // gives you the name of the property. This is used to find a fallback value 
        // for this property in the event of an exception during property parsing.
        internal string ElementNameOrPropertyName
        {
            get { return _name; }
            set { _name = value; }
        }

        internal object ContentProperty
        {
            get { return _contentProperty; }
            set { _contentProperty = value; }
        }

        // If an object has not yet been created at this point, this is the type of
        // element to created
        internal Type ExpectedType
        {
            get { return _expectedType; }
            set { _expectedType = value; }
        }

        // If an object has not yet been created at this point, this is the Baml type id
        // of the element.  This is used for faster creation of known types.
        internal short ExpectedTypeId
        {
            get { return _expectedTypeId; }
            set { _expectedTypeId = value; }
        }

        // This object is expected to be created using a TypeConverter.  If this
        //  is true, the following are also expected to be true:
        // -ObjectData is null
        // -ExpectedType is non-null
        // -ExpectedTypeId is non-null
        internal bool CreateUsingTypeConverter
        {
            get { return _createUsingTypeConverter; }
            set { _createUsingTypeConverter = value; }
        }

        // Context identifying what this reader stack item represents
        internal ReaderFlags ContextFlags
        {
            get { return _contextFlags; }
            set { _contextFlags = value; }
        }

        // True if this element has not yet been added to the tree, but needs to be.
        internal bool NeedToAddToTree
        {
            get { return CheckFlag(ReaderFlags.NeedToAddToTree); }
        }

        // simple helper method to remove the NeedToAddToTree flag and add the AddedToTree flag
        internal void MarkAddedToTree()
        {
            ContextFlags = ((ContextFlags | ReaderFlags.AddedToTree) & ~ReaderFlags.NeedToAddToTree);
        }

        // simple helper method that returns true if the context contains the given flag or flags.
        // If multiple flags are passed in, the context must contain all the flags.
        internal bool CheckFlag(ReaderFlags flag)
        {
            return (ContextFlags & flag) == flag;
        }

        // simple helper method adds the flag to the context
        internal void SetFlag(ReaderFlags flag)
        {
            ContextFlags |= flag;
        }

        // simple helper method that removes the flag from the context
        internal void ClearFlag(ReaderFlags flag)
        {
            ContextFlags &= ~flag;
        }

        // Helper to determine if this represents an object element.
        internal bool IsObjectElement
        {
            get
            {
                return ContextType == ReaderFlags.DependencyObject
                       ||
                       ContextType == ReaderFlags.ClrObject;
            }
        }

        // Clear all the fields on this instance before it put into the factory cache
        internal void ClearData()
        {
            _contextFlags = 0;
            _contextData = null;
            _contextKey = null;
            _contentProperty = null;
            _expectedType = null;
            _expectedTypeId = 0;
            _createUsingTypeConverter = false;
            _uid = null;
            _name = null;
        }
    }
}
