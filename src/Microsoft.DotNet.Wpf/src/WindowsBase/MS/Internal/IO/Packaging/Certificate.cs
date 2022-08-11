// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Handles serialization to/from X509 Certificate part (X509v3 = ASN.1 DER format)
//
//
//
//

using System;
using System.Diagnostics;                               // for Assert
using System.Security.Cryptography.X509Certificates;
using System.Windows;                                   // For Exception strings - SR
using System.IO.Packaging;      
using System.IO;                                        // for Stream
using MS.Internal;                                      // For ContentType
using MS.Internal.WindowsBase;
using MS.Internal.IO.Packaging.Extensions;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// CertificatePart
    /// </summary>
    internal class CertificatePart
    {
        #region Internal Members
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Type of relationship to a Certificate Part
        /// </summary>
        static internal string RelationshipType
        {
            get
            {
                return _certificatePartRelationshipType;
            }
        }

        /// <summary>
        /// Prefix of auto-generated Certificate Part names
        /// </summary>
        static internal string PartNamePrefix
        {
            get
            {
                return _certificatePartNamePrefix;
            }
        }

        /// <summary>
        /// Extension of Certificate Part file names
        /// </summary>
        static internal string PartNameExtension
        {
            get
            {
                return _certificatePartNameExtension;
            }
        }

        /// <summary>
        /// ContentType of Certificate Parts
        /// </summary>
        static internal ContentType ContentType
        {
            get
            {
                return _certificatePartContentType;
            }
        }

        internal Uri Uri
        {
            get
            {
                return _part.Uri;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Certificate to associate with this Certificate Part
        /// </summary>
        /// <value></value>
        /// <exception cref="FileFormatException">stream is too large</exception>
        internal X509Certificate2 GetCertificate()
        {
            // lazy init
            if (_certificate == null)
            {
                // obtain from the part
                using (Stream s = _part.GetStream())
                {
                    if (s.Length > 0)
                    {
                        // throw if stream is beyond any reasonable length
                        if (s.Length > _maximumCertificateStreamLength)
                            throw new FileFormatException(SR.CorruptedData);

                        // X509Certificate constructor desires a byte array
                        Byte[] byteArray = new Byte[s.Length];
                        PackagingUtilities.ReliableRead(s, byteArray, 0, (int)s.Length);
                        _certificate = new X509Certificate2(byteArray);
                    }
                }
            }
            return _certificate;
        }

        internal void SetCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            _certificate = certificate;

            // persist to the part
            Byte[] byteArray = _certificate.GetRawCertData();

            // FileMode.Create will ensure that the stream will shrink if overwritten
            using (Stream s = _part.GetSeekableStream(FileMode.Create, FileAccess.Write))
            {
                s.Write(byteArray, 0, byteArray.Length);
            }
        }

        /// <summary>
        /// CertificatePart constructor
        /// </summary>
        internal CertificatePart(System.IO.Packaging.Package container, Uri partName)
        {
            if (container == null)
                throw new ArgumentNullException("container");
            
            if (partName == null)
                throw new ArgumentNullException("partName");

            partName = PackUriHelper.ValidatePartUri(partName);
            
            // create if not found
            if (container.PartExists(partName))
            {
                // open the part
                _part = container.GetPart(partName);

                // ensure the part is of the expected type
                if (_part.ValidatedContentType().AreTypeAndSubTypeEqual(_certificatePartContentType) == false)
                    throw new FileFormatException(SR.CertificatePartContentTypeMismatch);
            }
            else
            {
                // create the part
                _part = container.CreatePart(partName, _certificatePartContentType.ToString());
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private PackagePart             _part;          // part that houses the certificate
        private X509Certificate2       _certificate;   // certificate itself

        // certificate part constants
        private static readonly ContentType _certificatePartContentType =
            new ContentType("application/vnd.openxmlformats-package.digital-signature-certificate");
        private static readonly string _certificatePartNamePrefix = "/package/services/digital-signature/certificate/";
        private static readonly string _certificatePartNameExtension = ".cer";
        private static readonly string _certificatePartRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/certificate";
        private static long             _maximumCertificateStreamLength = 0x40000;   // 4MB
        #endregion Private Members
    }
}
