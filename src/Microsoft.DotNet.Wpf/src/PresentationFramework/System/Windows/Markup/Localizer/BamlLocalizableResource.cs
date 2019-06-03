// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Contents:  BamlLocalizableResource class, part of Baml Localization API
// 

using System;
using System.Windows;
using MS.Internal;
using System.Diagnostics;

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// Localization resource in Baml
    /// </summary>
    public class BamlLocalizableResource
    {
        //--------------------------------
        // constructor
        //--------------------------------

        /// <summary>
        /// Constructor of LocalizableResource
        /// </summary>
        public BamlLocalizableResource() : this (
            null,
            null,
            LocalizationCategory.None,
            true,
            true
            )
        {
        }

        /// <summary>
        /// Constructor of LocalizableResource
        /// </summary>
        public BamlLocalizableResource(
            string               content,
            string               comments,
            LocalizationCategory category,
            bool                 modifiable,
            bool                 readable
            )
        {
            _content   = content;
            _comments  = comments;
            _category  = category;
            Modifiable = modifiable;
            Readable   = readable;
        }        

        /// <summary>
        /// constructor that creates a deep copy of the other localizable resource
        /// </summary>
        /// <param name="other"> the other localizale resource </param>
        internal BamlLocalizableResource(BamlLocalizableResource other)
        {
            Debug.Assert(other != null);            
            _content        = other._content;
            _comments       = other._comments;
            _flags          = other._flags;            
            _category       = other._category;
        }

        //---------------------------------
        // public properties
        //---------------------------------

        /// <summary>
        /// The localizable value
        /// </summary>
        public string Content
        {
            get 
            {
                return _content;
            }

            set 
            {
                _content = value;
            }
        }

        /// <summary>
        /// The localization comments
        /// </summary>
        public string Comments
        {
            get 
            {
                return _comments;
            }

            set
            {             
                _comments = value;
            }
        }

        /// <summary>
        /// Localization Lock by developer
        /// </summary>
        public bool Modifiable 
        {
            get 
            {
                return (_flags & LocalizationFlags.Modifiable) > 0;
            }

            set 
            {             
                if (value)
                {
                    _flags |= LocalizationFlags.Modifiable;
                }
                else
                {
                    _flags &= (~LocalizationFlags.Modifiable);
                }
            }
        }

        /// <summary>
        /// Visibility of the resource for translation
        /// </summary>
        public bool Readable
        {
            get 
            {
                return (_flags & LocalizationFlags.Readable) > 0;
            }

            set 
            {             
                if (value)
                {
                    _flags |= LocalizationFlags.Readable;
                }
                else
                {
                    _flags &= (~LocalizationFlags.Readable);
                }
            }
        }

        /// <summary>
        /// String category of the resource
        /// </summary>
        public LocalizationCategory Category
        {
            get 
            {
                return _category;
            }

            set
            {             
                _category = value;
            }
        }        

        /// <summary>
        /// compare equality
        /// </summary>
        public override bool Equals(object other)
        {
            BamlLocalizableResource otherResource = other as BamlLocalizableResource;
            if (otherResource == null)
                return false;
                    
            return (_content     == otherResource._content 
                 && _comments    == otherResource._comments 
                 && _flags       == otherResource._flags 
                 && _category    == otherResource._category);
        }

        ///<summary>
        ///Return the hashcode.
        ///</summary>
        public override int GetHashCode()
        {
            return (_content == null ? 0 : _content.GetHashCode()) 
                  ^(_comments == null ? 0 : _comments.GetHashCode())
                  ^ (int) _flags
                  ^ (int) _category;
        }        
        
        //---------------------------------
        // private members
        //---------------------------------
        private string               _content;
        private string               _comments;
        private LocalizationFlags    _flags;        
        private LocalizationCategory _category;     


        //---------------------------------
        // Private type
        //---------------------------------
        [Flags]
        private enum LocalizationFlags : byte
        {
            Readable        = 1,
            Modifiable      = 2,            
        }
    }
}



