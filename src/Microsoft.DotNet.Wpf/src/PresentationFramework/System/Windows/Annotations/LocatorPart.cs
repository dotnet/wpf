// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     ContentLocatorPart represents a set of name/value pairs that identify a
//     piece of data within a certain context.  The names and values are 
//     strings.
//   
//     Spec: Simplifying Store Cache Model.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

using MS.Internal.Annotations;
using MS.Internal.Annotations.Anchoring;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     ContentLocatorPart represents a set of name/value pairs that identify a
    ///     piece of data within a certain context.  The names and values are
    ///     all strings.
    /// </summary>
    public sealed class ContentLocatorPart : INotifyPropertyChanged2, IOwnedObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a ContentLocatorPart with the specified type name and namespace.
        /// </summary>
        /// <param name="partType">fully qualified locator part's type</param>
        /// <exception cref="ArgumentNullException">partType is null</exception>
        /// <exception cref="ArgumentException">partType.Namespace or partType.Name is null or empty string</exception>
        public ContentLocatorPart(XmlQualifiedName partType) 
        {
            if (partType == null)
            {
                throw new ArgumentNullException("partType");
            }
            if (String.IsNullOrEmpty(partType.Name))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "partType.Name");
            }
            if (String.IsNullOrEmpty(partType.Namespace))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "partType.Namespace");
            }

            _type = partType;
            _nameValues = new ObservableDictionary();
            _nameValues.PropertyChanged += OnPropertyChanged;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Compares two ContentLocatorParts for equality.  They are equal if they
        ///     contain the same set of name/value pairs.
        /// </summary>
        /// <param name="obj">second locator part</param>
        /// <returns>true - the ContentLocatorParts are equal, false - different</returns>
        public override bool Equals(object obj)
        {
            ContentLocatorPart part = obj as ContentLocatorPart;
            string otherValue;

            // We are equal to ourselves
            if (part == this)
            {
                return true;
            }

            // Not a locator part
            if (part == null)
            {
                return false;
            }

            // Have different type names
            if (!_type.Equals(part.PartType))
            {
                return false;
            }

            // Have different number of name/value pairs
            if (part.NameValuePairs.Count != _nameValues.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> k_v in _nameValues)
            {
                // A name/value pair isn't present or has a different value
                if (!part._nameValues.TryGetValue(k_v.Key, out otherValue))
                {
                    return false;
                }

                if (k_v.Value != otherValue)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Returns the hashcode for this ContentLocatorPart.
        /// </summary>
        /// <returns>hashcode</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Create a deep clone of this ContentLocatorPart.  The returned ContentLocatorPart
        ///     is equal to this ContentLocatorPart. 
        /// </summary>
        /// <returns>a deep clone of this ContentLocatorPart; never returns null</returns>
        public object Clone()
        {
            ContentLocatorPart newPart = new ContentLocatorPart(_type);

            foreach (KeyValuePair<string, string> k_v in _nameValues)
            {
                newPart.NameValuePairs.Add(k_v.Key, k_v.Value);
            }

            return newPart;
        }
        
        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, string> NameValuePairs
        {
            get
            {
                return _nameValues;
            }
        }      

        /// <summary>
        ///     Returns the ContentLocatorPart's type name. 
        /// </summary>
        /// <value>qualified type name for this ContentLocatorPart</value>
        public XmlQualifiedName PartType
        {
            get
            {
                return _type;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// 
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add{ _propertyChanged += value; }
            remove{ _propertyChanged -= value; }
        }

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// Determines if a locator part matches this locator part.  Matches is
        /// different from equals because a locator part may be defined to match
        /// a range of locator parts, not just exact replicas. 
        /// </summary>
        internal bool Matches(ContentLocatorPart part)
        {
            bool overlaps = false;
            string overlapsString;

            _nameValues.TryGetValue(TextSelectionProcessor.IncludeOverlaps, out overlapsString);

            // If IncludeOverlaps is true, a match is any locator part
            // whose range overlaps with ours
            if (Boolean.TryParse(overlapsString, out overlaps) && overlaps)
            {
                // We match ourselves
                if (part == this)
                {
                    return true;
                }

                // Have different type names
                if (!_type.Equals(part.PartType))
                {
                    return false;
                }

                int desiredStartOffset;
                int desiredEndOffset;
                TextSelectionProcessor.GetMaxMinLocatorPartValues(this, out desiredStartOffset, out desiredEndOffset);

                int startOffset;
                int endOffset;
                TextSelectionProcessor.GetMaxMinLocatorPartValues(part, out startOffset, out endOffset);

                // Take care of an exact match to us (which may include offset==MinValue
                // which we don't want to handle with the formula below.
                if (desiredStartOffset == startOffset && desiredEndOffset == endOffset)
                {
                    return true;
                }

                // Take care of the special case of no content to match to 
                if (desiredStartOffset == int.MinValue)
                {
                    return false;
                }

                if ((startOffset >= desiredStartOffset && startOffset <= desiredEndOffset)
                   || (startOffset < desiredStartOffset && endOffset >= desiredStartOffset))
                {
                    return true;
                }

                return false;
            }

            return this.Equals(part);            
        }

        /// <summary>
        /// Produces an XPath fragment that selects for matches to this ContentLocatorPart.
        /// </summary>
        /// <param name="namespaceManager">namespace manager used to look up prefixes</param>
        /// <returns>an XPath fragment that selects for matches to this ContentLocatorPart</returns>
        internal string GetQueryFragment(XmlNamespaceManager namespaceManager)
        {
            bool overlaps = false;
            string overlapsString;

            _nameValues.TryGetValue(TextSelectionProcessor.IncludeOverlaps, out overlapsString);            

            if (Boolean.TryParse(overlapsString, out overlaps) && overlaps)
            {
                return GetOverlapQueryFragment(namespaceManager);
            }
            else
            {
                return GetExactQueryFragment(namespaceManager);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties        

        /// <summary>
        /// </summary>
        bool IOwnedObject.Owned
        {
            get
            {
                return _owned;
            }
            set
            {
                _owned = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Notify the owner this ContentLocatorPart has changed.
        /// </summary>
        private void OnPropertyChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("NameValuePairs"));
            }
        }

        /// <summary>
        /// Produces an XPath fragment that selects for ContentLocatorParts with an anchor that
        /// intersects with the range specified by this ContentLocatorPart.
        /// </summary>
        /// <param name="namespaceManager">namespace manager used to look up prefixes</param>
        private string GetOverlapQueryFragment(XmlNamespaceManager namespaceManager)
        {
            string corePrefix = namespaceManager.LookupPrefix(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace); 
            string prefix = namespaceManager.LookupPrefix(this.PartType.Namespace);
            string res = prefix == null ? "" : (prefix + ":");
            res += TextSelectionProcessor.CharacterRangeElementName.Name + "/" + corePrefix + ":"+AnnotationXmlConstants.Elements.Item;

            int startOffset;
            int endOffset;
            TextSelectionProcessor.GetMaxMinLocatorPartValues(this, out startOffset, out endOffset);

            string startStr = startOffset.ToString(NumberFormatInfo.InvariantInfo);
            string endStr = endOffset.ToString(NumberFormatInfo.InvariantInfo);

            // Note: this will never match if offsetStr == 0.  Which makes sense - there
            // is no content to get anchors for.
            res += "[starts-with(@" + AnnotationXmlConstants.Attributes.ItemName + ", \"" + TextSelectionProcessor.SegmentAttribute + "\") and " +
                    " ((substring-before(@" + AnnotationXmlConstants.Attributes.ItemValue + ",\",\") >= " + startStr + " and substring-before(@" + AnnotationXmlConstants.Attributes.ItemValue + ",\",\") <= " + endStr + ") or " +
                    "  (substring-before(@" + AnnotationXmlConstants.Attributes.ItemValue + ",\",\") < " + startStr + " and substring-after(@" + AnnotationXmlConstants.Attributes.ItemValue + ",\",\") >= " + startStr + "))]";

            return res;
        }


        /// <summary>
        /// Produces an XPath fragment that selects ContentLocatorParts of the same type
        /// and containing the exact name/values this ContentLocatorPart contains.
        /// </summary>
        /// <param name="namespaceManager">namespaceManager used to generate the XPath fragment</param>
        private string GetExactQueryFragment(XmlNamespaceManager namespaceManager)
        {
            string corePrefix = namespaceManager.LookupPrefix(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace); 
            string prefix = namespaceManager.LookupPrefix(this.PartType.Namespace);
            string res = prefix == null ? "" : (prefix + ":");
            res += this.PartType.Name;

            bool and = false;

            foreach (KeyValuePair<string, string> k_v in ((ICollection<KeyValuePair<string, string>>)this.NameValuePairs))
            {
                if (and)
                {
                    res += "/parent::*/" + corePrefix + ":" + AnnotationXmlConstants.Elements.Item + "[";
                }
                else
                {
                    and = true;
                    res += "/" + corePrefix + ":" + AnnotationXmlConstants.Elements.Item + "[";
                }
                res += "@" + AnnotationXmlConstants.Attributes.ItemName + "=\"" + k_v.Key + "\" and @" + AnnotationXmlConstants.Attributes.ItemValue + "=\"" + k_v.Value + "\"]";
            }

            if (and)
            {
                res += "/parent::*";
            }

            return res;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        /// <summary>
        /// </summary>
        private bool _owned;

        /// <summary>
        ///     The ContentLocatorPart's type name.
        /// </summary>
        private XmlQualifiedName _type;

        /// <summary>
        ///     The internal data structure.
        /// </summary>
        private ObservableDictionary _nameValues;

        ///
        private event PropertyChangedEventHandler _propertyChanged;

        #endregion Private Fields
    }
}
