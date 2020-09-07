// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                                                                                                           
    Abstract:
        This file implements the XpsFontSerializationService
        used by the Xps Serialization APIs for serializing
        fonts to a Xps package.
                                                    
                                                                             
--*/
using System;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class implements a support service for serialization
    /// of fonts to a Xps package.
    /// </summary>
    internal class XpsFontSerializationService
    {
        /// <summary>
        /// </summary>
        public
        XpsFontSerializationService( 
            BasePackagingPolicy packagingPolicy
            )
        {

            _fontSubsetter = new XpsFontSubsetter(packagingPolicy);
        }

        public
        bool
        SignalCommit( Type type )
        {
            FontSubsetterCommitPolicies signal;
            if(type == typeof(FixedDocumentSequence))
            {
                signal = FontSubsetterCommitPolicies.CommitEntireSequence;
            }
            else if(type ==  typeof(FixedDocument))
            {
                signal = FontSubsetterCommitPolicies.CommitPerDocument;
            }
            else if(type== typeof(FixedPage))
            {
                signal = FontSubsetterCommitPolicies.CommitPerPage;
            }
            else if(type == typeof(Visual))
            {
                signal = FontSubsetterCommitPolicies.CommitPerPage;
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }
            return _fontSubsetter.CommitFontSubsetsSignal(signal);
        }
        
        /// <summary>
        /// This method retieves a XpsFontSubsetter for
        /// serializing a font to a Xps package. 
        /// This method assumes the font subsetter 
        /// has already been itialized.
        /// </summary>
        /// <returns>
        /// A reference to a XpsFontSubsetter instance.
        /// </returns>
        public 
        XpsFontSubsetter
        FontSubsetter
        {
            get
            {
                return _fontSubsetter;
            }
        }

        private XpsFontSubsetter _fontSubsetter;
    }
}
