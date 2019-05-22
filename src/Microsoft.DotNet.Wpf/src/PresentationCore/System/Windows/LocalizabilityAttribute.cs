// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Localizability attributes
//

using System;
using System.ComponentModel;

namespace System.Windows
{    
    /// <summary>
    /// Specifies the localization preferences for a class or property in Baml
    /// The attribute can be specified on Class, Property and Method
    /// </summary>
    [AttributeUsage(
           AttributeTargets.Class     
         | AttributeTargets.Property 
         | AttributeTargets.Field    
         | AttributeTargets.Enum     
         | AttributeTargets.Struct,
         AllowMultiple = false, 
         Inherited = true)
    ]
    public sealed class LocalizabilityAttribute : Attribute 
    {
        /// <summary>
        /// Construct a LocalizabilityAttribute to describe the localizability of a property.
        /// Modifiability property default to Modifiability.Modifiable, and Readability property
        /// default to Readability.Readable.
        /// </summary>
        /// <param name="category">the string category given to the item</param>
        public LocalizabilityAttribute(LocalizationCategory category)
        {
            if ( category < LocalizationCategory.None
              || category > LocalizationCategory.NeverLocalize)
            {
                throw new InvalidEnumArgumentException(
                    "category", 
                    (int)category, 
                    typeof(LocalizationCategory)
                    );
            }

            _category      = category;
            _readability   = Readability.Readable;
            _modifiability = Modifiability.Modifiable;
        }

      
        /// <summary>
        /// String category
        /// </summary>
        /// <value>gets or sets the string category for the item</value>
        public LocalizationCategory Category
        {
            // should have only getter, because it is a required parameter to the constructor
            get { return _category; }         
        }      

        /// <summary>
        /// Get or set the readability of the attribute's targeted value
        /// </summary>
        /// <value>Readability</value>
        public Readability Readability
        {
            get { return _readability; }
            set 
            { 
                if (  value != Readability.Unreadable 
                   && value != Readability.Readable 
                   && value != Readability.Inherit)
                {
                    throw new InvalidEnumArgumentException("Readability", (int) value, typeof(Readability));
                }

                _readability = value;
            }
        }

        /// <summary>
        /// Get or set the modifiability of the attribute's targeted value
        /// </summary>
        /// <value>Modifiability</value>
        public Modifiability Modifiability
        {
            get { return _modifiability; }
            set 
            {
                if (   value != Modifiability.Unmodifiable
                    && value != Modifiability.Modifiable
                    && value != Modifiability.Inherit)
                {
                    throw new InvalidEnumArgumentException("Modifiability", (int) value, typeof(Modifiability));
                }

                _modifiability = value;
            }            
        }

        //--------------------------------------------
        // Private members
        //--------------------------------------------
        private LocalizationCategory _category;
        private Readability          _readability;
        private Modifiability        _modifiability;
}
}
