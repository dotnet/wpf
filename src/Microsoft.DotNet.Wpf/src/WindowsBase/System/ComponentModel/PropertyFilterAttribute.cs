// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.ComponentModel 
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     This attribute is a "query" attribute.  It is
    ///     an attribute that causes the type description provider
    ///     to narrow the scope of returned properties.  It differs
    ///     from normal attributes in that it cannot actually be
    ///     placed on a class as metadata and that the filter mechanism
    ///     is code rather than static metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class PropertyFilterAttribute : Attribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a new attribute.
        /// </summary>
        public PropertyFilterAttribute(PropertyFilterOptions filter) 
        {
            _filter = filter;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        
        /// <summary>
        ///     Override of Object.Equals that returns true if the filters
        ///     contained in both attributes match.
        /// </summary>
        public override bool Equals(object value) 
        {
            PropertyFilterAttribute a = value as PropertyFilterAttribute;
            if (a != null && a._filter.Equals(_filter)) 
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        ///     Override of Object.GetHashCode.
        /// </summary>
        public override int GetHashCode() 
        {
            return _filter.GetHashCode();
        }

        /// <summary>
        ///     Match determines if one attribute "matches" another.  For
        ///     attributes that store flags, a match may be different from
        ///     an equals.  For example, a filter of SetValid matches a 
        ///     filter of All, because All is a merge of all filter values.
        /// </summary>
        public override bool Match(object value) 
        {
            PropertyFilterAttribute a = value as PropertyFilterAttribute;
            if (a == null) return false;
            return ((_filter & a._filter) == _filter);
        }

        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        
        /// <summary>
        ///     The filter value passed into the constructor.
        /// </summary>
        public PropertyFilterOptions Filter 
        {
            get { return _filter; }
        }


        #endregion Public Properties
        
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields
        
        /// <summary>
        ///     Attributes may declare a Default field that indicates
        ///     what should be done if the attribute is not defined.
        ///     Our default is to return all properties.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PropertyFilterAttribute Default = new PropertyFilterAttribute(PropertyFilterOptions.All);

        #endregion Public Fields


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        
        private PropertyFilterOptions _filter;

        #endregion Private Fields
    }
}

