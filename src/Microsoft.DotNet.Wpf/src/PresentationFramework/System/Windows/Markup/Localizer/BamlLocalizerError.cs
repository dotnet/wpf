// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// Errors that maybe encountered by BamlLocalizer
    /// </summary>
    public enum BamlLocalizerError
    {
        /// <summary>
        /// More than one elements have the same Uid value.
        /// </summary>    
        DuplicateUid,

        /// <summary>
        /// The localized Baml contains more than one references to 
        /// the same element. 
        /// </summary>
        DuplicateElement,

        /// <summary>
        /// The element's substitution contains incomplete child placeholders.
        /// </summary>
        IncompleteElementPlaceholder,

        /// <summary>
        /// The localization commenting Xml does not have the correct format.
        /// </summary>
        InvalidCommentingXml,

        /// <summary>
        /// The localization commenting text contains invalid attributes. 
        /// </summary>
        InvalidLocalizationAttributes,

        /// <summary>
        /// The localization commenting text contains invalid comments.
        /// </summary>
        InvalidLocalizationComments,

        /// <summary>
        /// The Uid does not corresponding to any element in the Baml.
        /// </summary>
        InvalidUid,

        /// <summary>
        /// Child placeholders mismatch between substitution and source. 
        /// The substitution should contain all the element placeholders in the source. 
        /// </summary>
        MismatchedElements,

        /// <summary>
        /// The substitutuion to an element's content cannot be parsed as Xml, therefore any 
        /// formatting tags in the substitution will not be recognized. The substitution
        /// will be applied as plain text. 
        /// </summary>
        SubstitutionAsPlaintext,

        /// <summary>
        /// A child element does not have a Uid. Thus it cannot be represented 
        /// as a placeholder in the parent's content string. 
        /// </summary>
        UidMissingOnChildElement,

        /// <summary>
        /// A formatting tag in the substitution is not recognized to be a type of elements. 
        /// </summary>
        UnknownFormattingTag,
    }
}

