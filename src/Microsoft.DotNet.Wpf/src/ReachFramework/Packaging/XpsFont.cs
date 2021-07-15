// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                             
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsFont class.  This class inherits from
        XpsResource and controls font specific aspects of
        a resource added to a fixed page.

--*/
using System;
using System.IO.Packaging;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    ///
    /// </summary>
    public class XpsFont : XpsResource
    {
        #region Constructors

        internal
        XpsFont(
            XpsManager    xpsManager,
            INode           parent,
            PackagePart     part
            )
            : base(xpsManager, parent, part)
        {
            _isObfuscated = (part.ContentType == XpsS0Markup.FontObfuscatedContentType.ToString());
        }

        #endregion Constructors
        
        #region Public Properties
        /// <summary>
        /// Is true when font is obfuscated
        /// </summary>
        public
        bool
        IsObfuscated
        {
            get
            {
                return _isObfuscated;
            }
        }
        /// <summary>
        /// Is true when font is obfuscated
        /// </summary>
        public
        bool
        IsRestricted
        {
            get
            {
                return _isResticted;
            }
            set
            {
                _isResticted = value;
            }
        }
        #endregion Public Properties
        
        #region private members
        private bool _isObfuscated;
        private bool _isResticted;
        #endregion
        /// <summary>
        /// Obfuscate font data  
        /// in accordence with 6.2.7.3	Embedded Font Obfuscation
        /// of the metro spec
        /// </summary>
        /// <param name="fontData">
        ///  Data to obfuscate
        /// </param>
        /// <param name="guid">
        /// Guid to be used in XORing the header
        /// </param>
        public
        static
        void
        ObfuscateFontData( byte[] fontData, Guid guid )
        {
            System.Windows.Xps.Serialization.FEMCacheItem.ObfuscateData(fontData, guid );
        }
    }
}
