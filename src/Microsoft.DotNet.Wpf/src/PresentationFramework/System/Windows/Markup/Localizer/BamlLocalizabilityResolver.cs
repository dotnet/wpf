// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: BamlLocalizabilityResolver class
//

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// BamlLocalizabilityResolver class. It is implemented by Baml localization API client to provide 
    /// Localizability settings to Baml content
    /// </summary>  
    public abstract class BamlLocalizabilityResolver
    {
        /// <summary>
        /// Obtain the localizability of an element and 
        /// the whether the element can be formatted inline.
        /// The method is called when extracting localizable resources from baml
        /// </summary>                
        /// <param name="assembly">Full assembly name</param>
        /// <param name="className">Full class name</param>
        /// <returns>ElementLocalizability</returns>
        public abstract ElementLocalizability GetElementLocalizability(
            string                      assembly,
            string                      className
            );

        
        /// <summary>
        /// Obtain the localizability of a property
        /// The method is called when extracting localizable resources from baml 
        /// </summary>
        /// <param name="assembly">Full assembly name</param>
        /// <param name="className">Full class name that contains the property defintion</param>
        /// <param name="property">property name</param>
        /// <returns>LocalizabilityAttribute for the property</returns>
        public abstract LocalizabilityAttribute GetPropertyLocalizability(
            string                      assembly,
            string                      className,
            string                      property
            );

        /// <summary>
        /// Return full class name of a formatting tag that hasn't been encountered in Baml
        /// The method is called when applying translations to the localized baml
        /// </summary>
        /// <param name="formattingTag">formatting tag name</param>
        /// <returns>Full name of the class that is formatted inline</returns>
        public abstract string ResolveFormattingTagToClass(
            string formattingTag
            );

        /// <summary>
        /// Return full name of the assembly that contains the class definition
        /// </summary>
        /// <param name="className">Full class name</param>
        /// <returns>Full name of the assembly containing the class</returns>
        public abstract string ResolveAssemblyFromClass(
            string className
            );        
    }

    /// <summary>
    /// The localizability information for an element
    /// </summary>
    public class ElementLocalizability
    {
        private string                  _formattingTag;
        private LocalizabilityAttribute _attribute;

        /// <summary>
        /// Constructor
        /// </summary>
        public ElementLocalizability()
        {
        }
                
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="formattingTag">formatting tag, give a non-empty value to indicate that the class is formatted inline</param>
        /// <param name="attribute">LocalizabilityAttribute for the class</param>
        public ElementLocalizability(string formattingTag, LocalizabilityAttribute attribute)
        {            
            _formattingTag = formattingTag;
            _attribute     = attribute;
        }

        /// <summary>
        /// Set or Get the formatting tag
        /// </summary>
        public string FormattingTag 
        {
            get { return _formattingTag; }
            set { _formattingTag = value; }
        }

        /// <summary>
        /// Set or get the LocalizabilityAttribute
        /// </summary>
        public LocalizabilityAttribute Attribute
        {
            get { return _attribute; }
            set { _attribute = value; }
        }        
    }    
}

