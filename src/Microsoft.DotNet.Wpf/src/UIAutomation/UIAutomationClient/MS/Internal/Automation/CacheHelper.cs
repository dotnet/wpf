// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Static class that manages prefetching and normalization

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using MS.Win32;


// For methods that support prefetching, ClientAPI sends the UiaCoreApi
// a cacherequest that lists the relatives of the element (TreeScope)
// and properties/patterns to return. The API returns with a CacheResponse
// with all this information "flattened out" - this class has the task
// of inflating that response into a tree of AutomationElements.
//
// The response consists of two parts:
// A 2-D array of property values:
// There is one row for every element (usually just the target element
// itself, but can also include rows for children and descendants if
// TreeScope was used).
// Each row then consists of:
// - a value that is the hnode for the element (unless AutomationElementMode
// is empty, in which case this slot is null).
// - values for the requested properties
// - values for the requetsed patterns
// 
// The values in the 2-D array are Variant-based objects when we get them
// back from the unmanaged API - we immediately convert them to the appropriate
// CLR type before we use that array - this conversion is done in the CacheResponse
// ctor in UiaCoreApis. This conversion, for example:
//     converts ints to appropriate enums
//     converts int[]s to Rects for BoundingRectangle
//     converts the inital object representing the hnode to a SafeHandle
//     converts objects representing hpatterns to SafeHandles.
//
// The second part of the response is a string describing the tree structure
// - this is a lisp-like tree description, and it describes a traversal
// of the tree - a '(' every time a node is entered, and  a ')' every time
// a node is left. 
// A simple tree consisting of a single node with two children would be
// represented by the string "(()())".
// The AutomationElement tree structure can be determined by parsing this
// string.
// 
// This string is modified slightly from the description above - if a node
// in the tree also has a row of properties in the table - which is the
// usual case - then the '(' is replaced with a 'P'. The rows in the table
// are stored in "preorder traversal order", so they can easily be matched
// up with successive 'P's from the tree description string.

namespace MS.Internal.Automation
{
    static class CacheHelper
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static AutomationElement BuildAutomationElementsFromResponse(
            UiaCoreApi.UiaCacheRequest cacheRequest,
            UiaCoreApi.UiaCacheResponse response)
        {
            if (response.TreeStructure == null)
            {
                Debug.Assert(response.RequestedData == null, "both RequestedData and TreeStructure should be null or non-null");
                return null;
            }

            // FrozenCacheRequest should not be null one new AE code, but may
            // still be null on old code paths - eg. top level window events - where
            // prefetching is not yet enabled.
            if (cacheRequest == null)
            {
                cacheRequest = CacheRequest.DefaultUiaCacheRequest;
            }

            // ParseTreeDescription is the method that parses the returned data
            // and builds up the tree, setting properties on each node as it goes along...
            // index and propIndex keep track of where it is, and we check afterwards
            // that all are pointing to the end, to ensure that everything matched
            // up as expected.
            int index = 0;
            int propIndex = 0;
            bool askedForChildren = (cacheRequest.TreeScope & TreeScope.Children) != 0;
            bool askedForDescendants = (cacheRequest.TreeScope & TreeScope.Descendants) != 0;
            AutomationElement root = ParseTreeDescription(response.TreeStructure, response.RequestedData,
                                                           ref index,
                                                           ref propIndex,
                                                           cacheRequest,
                                                           askedForChildren,
                                                           askedForDescendants);

            if (index != response.TreeStructure.Length)
            {
                Debug.Assert(false, "Internal error: got malformed tree description string (extra chars at end)");
                return null;
            }

            if (response.RequestedData != null && propIndex != response.RequestedData.GetLength(0))
            {
                Debug.Assert(false, "Internal error: mismatch between count of property buckets and nodes claiming them");
                return null;
            }

            // Properties are wrapped (eg. pattern classes inserted in front of interface) as
            // they are being returned to the caller, in the AutomationElement accessors, not here.

            return root;
        }

        #endregion Internal Methods



        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // Parses the string as returned from ElementSearcher - see ElementSearcher.cs
        // for a description of the format string. Summary is that it is a lisp-like
        // set of parens indicating tree structure (eg. "(()())" indicates a node containing
        // two child nodes), but uses 'P' instead of an open paran to indicate that the
        // corresonding node has a property array that needs to be associated with it.
        //
        // index is the current position in the tree strucure string,
        // propIndex is the current position in the array of property arrays
        // (an array of properties returned for each element that matches the
        // condition specified in the Searcher condition.)
        private static AutomationElement ParseTreeDescription( string treeDescription,
                                                               object[,] properties,
                                                               ref int index,
                                                               ref int propIndex,
                                                               UiaCoreApi.UiaCacheRequest cacheRequest,
                                                               bool askedForChildren,
                                                               bool askedForDescendants )
        {
            // Check that this is a 'begin node' tag (with or without properties)...
            if (string.IsNullOrEmpty(treeDescription))
                return null;
            char c = treeDescription[index];

            if (c != '(' && c != 'P')
            {
                return null;
            }

            bool havePropertiesForThisNode = c == 'P';

            index++;

            SafeNodeHandle hnode = null;

            // If we have information for this node, and we want full remote
            // references back, then extract the hnode from the first slot of that
            // element's property row...
            if (havePropertiesForThisNode && cacheRequest.AutomationElementMode == AutomationElementMode.Full)
            {
                hnode = (SafeNodeHandle)properties[propIndex, 0];
            }

            // Attach properties if present...
            object[,] cachedValues = null;
            int cachedValueIndex = 0;
            if (havePropertiesForThisNode)
            {
                cachedValues = properties;
                cachedValueIndex = propIndex;
                propIndex++;
            }

            AutomationElement node = new AutomationElement(hnode, cachedValues, cachedValueIndex, cacheRequest);

            if( askedForChildren || askedForDescendants )
            {
                // If we did request children or descendants at this level, then set the
                // cached first child to null - it may get overwritten with
                // an actual value later; but the key thing is that it gets
                // set so we can distinguish the "asked, but doesn't have one" from
                // the "didn't ask" case. (Internally, AutomationElement uses
                // 'this' to indicate the later case, and throws an exception if
                // you ask for the children without having previously asked
                // for them in a CacheRequest.)
                node.SetCachedFirstChild(null);
            }

            // Add in children...
            AutomationElement prevChild = null;

            for (; ; )
            {
                // Recursively parse the string...
                AutomationElement child = ParseTreeDescription( treeDescription, properties,
                                                                ref index, ref propIndex, cacheRequest,
                                                                askedForDescendants, askedForDescendants);

                if (child == null)
                    break;

                // Then link child node into tree...
                child.SetCachedParent(node);

                if (prevChild == null)
                {
                    node.SetCachedFirstChild(child);
                }
                else
                {
                    prevChild.SetCachedNextSibling(child);
                }

                prevChild = child;
            }

            // Ensure that end node tag is present...
            if (treeDescription[index] != ')')
            {
                Debug.Assert(false, "Internal error: Got malformed tree description string, missing closing paren");
                return null;
            }

            index++;
            return node;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // Static class - no private fields

        #endregion Private Fields
    }
}
