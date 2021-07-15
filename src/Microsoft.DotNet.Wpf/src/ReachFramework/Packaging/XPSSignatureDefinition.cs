// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                       
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsSignatureDefinition class.  This class deserializes, provides
        access and serializes the properties associated with a Signature Definition.
        XpsSignatureDefinitions is the means by which document producers provide 
        guidance for signers in the case that they are not themselves the ones 
        that will ultimately sign this document
                                                     
                                                                             
--*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Xml;
using System.Globalization;
using System.Windows.Markup;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// Helper class to group spot location data together
    /// </summary>
    public
    class 
    SpotLocation
    { 
        /// <summary>
        /// Empty contructor
        /// </summary>
        public
        SpotLocation(  )
        {
        }

        /// <summary>
        /// Specifies which page the signature spot should be 
        /// displayed in
        /// </summary>
        public
        Uri 
        PageUri
        {
            get
            {
                return _pageUri;
            }
            
            set
            {
                _pageUri = value;
            }
        }

        /// <summary>
        /// Specifies the x-coordinate of the point in the page where 
        /// the signature spot should be drawn starting from.
        /// </summary>
        public
        double 
        StartX
        {
            get
            {
                return _startX;
            }
            
            set
            {
                _startX = value;
            }
        }

        /// <summary>
        /// Specifies the y-coordinate of the point in the page where 
        /// the signature spot should be drawn starting from.
        /// </summary>
        public
        double 
        StartY
        {
            get
            {
                return _startY;
            }
            
            set
            {
                _startY = value;
            }
        }     

        Uri         _pageUri;
        double      _startX;
        double      _startY;
    }

    /// <summary>
    /// This class provides de-serialization, access and serialization
    /// to the core properties of a Xps Dcoument
    /// </summary>
    public class    XpsSignatureDefinition 
    {
        /// <summary>
        /// Default creator
        /// </summary>
        public
        XpsSignatureDefinition()
        {
            _spotLocation = null;
            _intent = null;
            _signBy = null;
            _signingLocale = null;
            _spotId = null;
            _hasBeenModified = false;
}
        /// <summary>
        /// Any string representing the identity of the individual 
        /// who is requested to sign this document 
        /// This property is OPTIONAL.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        string
        RequestedSigner
        {
            get
            {
                return _requestedSigner;
            }
            
            set
            {
                _requestedSigner = value;
                _hasBeenModified = true;
            }        
        }
        
        /// <summary>
        /// Specifies where the Xps document viewer should place a visual 
        /// to indicate to the user that a signature has been requested
        /// This property is OPTIONAL.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        SpotLocation 
        SpotLocation 
        {
            get
            {
                return _spotLocation;
            }
            
            set
            {
                _spotLocation = value;
                _hasBeenModified = true;
            }
        }

        /// <summary>
        /// It holds the string value of the signature intention agreement 
        /// that the signer is signing against
        /// This property is OPTIONAL.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        String
        Intent
        {
            get
            {
                return _intent;
            }

            set
            {
                _intent = value;
                _hasBeenModified = true;
            }
        }

        /// <summary>
        /// It specifies the date-time by which the requested signer must 
        /// sign the parts of the document specified. 
        /// This property is OPTIONAL.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        DateTime?
        SignBy
        {
            get
            {
                return _signBy;
            }

            set
            {
                _signBy = value;
                _hasBeenModified = true;
            }
        }
        
        /// <summary>
        /// It can be set by the original producer of the document or the 
        /// signer at the time of signing the document. 
        /// It specifies the location under whose legislature this document 
        /// is being signed
        /// This property is OPTIONAL.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        string
        SigningLocale
        {
            get
            {
                return _signingLocale;
            }

            set
            {
                _signingLocale = value;
                _hasBeenModified = true;
            }
        }

        /// <summary>
        /// This attribute MAY be used to link an existing signature 
        /// to this SignatureDefinition element
        /// This property is REQUIRED.
        /// </summary>
        /// <value>
        /// Will return null if it has not been set.
        /// </value>
        public
        Guid?
        SpotId
        {
            get
            {
                return _spotId;
            }

            set
            {
                _spotId = value;
                _hasBeenModified = true;
            }
        }

        /// <summary>
        /// This attribute MAY be used to link a Culture/Language
        /// to the SignatureDefinition element
        /// This property is OPTIONAL
        /// </summary>
        public 
        CultureInfo
        Culture
        {
            get
            {
                return _cultureInfo;
            }

            set
            {
                _cultureInfo = value;
                _hasBeenModified = true;
            }
        }

        /// <summary>
        /// True if this class has been modified but not flushed
        /// </summary>
        public
        bool
        HasBeenModified
        {
            get
            {
                return _hasBeenModified;
            }

            set
            {
                _hasBeenModified = value;
            }
        }
        

        private SpotLocation            _spotLocation;
        private string                  _intent;
        private DateTime?                _signBy;
        private Guid?                    _spotId;
        private string                  _requestedSigner;
        private string                  _signingLocale;
        private CultureInfo         _cultureInfo;
        private bool                    _hasBeenModified;
        
        #region Public methods
        /// <summary>
        /// Writes Signature Definition in XML format
        /// </summary>
        /// <param name="writer">
        /// The XML Writer used to write the data
        /// </param>
        internal
        void
        WriteXML(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement(
             XpsS0Markup.SignatureDefinition,
             XpsS0Markup.SignatureDefinitionNamespace
            );

            //Spot ID, Signer Name and xml:lang are attributes of SignatureDefinition
            //Spot ID is required.  If it is not specified, then we throw.
            if (SpotId != null)
            {
                writer.WriteAttributeString(XpsS0Markup.SpotId, XmlConvert.EncodeName(SpotId.ToString()));
            }
            else
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_SpotIDRequiredAttribute));
            }

            if (RequestedSigner != null)
            {
                writer.WriteAttributeString(XpsS0Markup.RequestedSigner, RequestedSigner);
            }

            if (Culture != null)
            {
                XmlLanguage language = XmlLanguage.GetLanguage(Culture.Name);
                writer.WriteAttributeString(XpsS0Markup.XmlLang, language.ToString());
            }

            //SpotLocation, Intent, Signby, and Signing Location are elements of SignatureDefinition
            if (SpotLocation != null)
            {
                writer.WriteStartElement(XpsS0Markup.SpotLocation);

                Uri pageUri = new Uri(SpotLocation.PageUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped), UriKind.RelativeOrAbsolute);
                string pageUriAsString = pageUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);

                writer.WriteAttributeString(XpsS0Markup.PageUri, pageUriAsString);
                writer.WriteAttributeString(XpsS0Markup.StartX, SpotLocation.StartX.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString(XpsS0Markup.StartY, SpotLocation.StartY.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            if (Intent != null)
            {
                writer.WriteStartElement(XpsS0Markup.Intent);
                writer.WriteString(Intent);
                writer.WriteEndElement();
            }

            if (SignBy != null)
            {
                writer.WriteStartElement(XpsS0Markup.SignBy);
                writer.WriteString(((DateTime)SignBy).ToUniversalTime().ToString("s", DateTimeFormatInfo.InvariantInfo) + "Z");
                writer.WriteEndElement();
            }

            if (SigningLocale != null)
            {
                writer.WriteStartElement(XpsS0Markup.SigningLocale);
                writer.WriteString(SigningLocale);
                writer.WriteEndElement();
            }

            //
            //Signature Definition
            //
            writer.WriteEndElement();
            _hasBeenModified = false;
        }

        /// <summary>
        /// Reads Signature Definition from XML format
        /// </summary>
        /// <param name="reader">
        /// The XML Reader used to read the data
        /// </param>
        internal
        void
        ReadXML( XmlReader reader )
        {
            if( reader == null )
            {
                throw new ArgumentNullException("reader");
            }
                
            //
            // Assume the calling function has already read the 
            // SignatureDefinition start element
            //
            if( reader.NodeType != XmlNodeType.Element || 
                reader.Name != XpsS0Markup.SignatureDefinition 
              )
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotSignatureDefinitionElement));
            }

            bool exitLoop = false;

            //
            // Read the attributes off of SignatureDefinition
            //
            ReadAttributes( reader );
            
            while( !exitLoop && reader.Read() )
            {
                switch( reader.NodeType )
                {
                    case XmlNodeType.Element:
                        ReadElement( reader );
                        break;
                        
                    case XmlNodeType.EndElement:
                        if( ReadEndElement( reader ) )
                        {
                            exitLoop = true;
                        }
                        break;
                }
            }
        }
        #endregion Public methods


        #region Private methods
        private 
        void
        ReadAttributes( XmlReader reader )
        {
            if (reader.HasAttributes)
            {
                string nodeName = reader.Name;
                while (reader.MoveToNextAttribute())
                {
                    if (nodeName == XpsS0Markup.SignatureDefinition)
                    {
                        ValidateSignatureDefinitionAttribute(reader.Name, reader.Value);
                    }
                    else if (nodeName == XpsS0Markup.SpotLocation)
                    {
                        ValidateSpotLocationAttribute(reader.Name, reader.Value);
                    }
                }
                // Move the reader back to the element node. 
                reader.MoveToElement();
            } 
        }
        
        private
        void
        ValidateSignatureDefinitionAttribute( string attributeName, string attributeValue )
        {
            if (attributeName == XpsS0Markup.RequestedSigner)
            {
                RequestedSigner = attributeValue;
            }
            else if (attributeName == XpsS0Markup.SpotId)
            {
                try
                {
                    SpotId = new Guid(XmlConvert.DecodeName(attributeValue));
                }
                catch (ArgumentNullException)
                {
                    SpotId = null;
                }
                catch (FormatException)
                {
                    SpotId = null;
                }
            }
            else if (attributeName == XpsS0Markup.XmlLang)
            {
                Culture = new CultureInfo(attributeValue);
            }
            else
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotValidSignatureDefinitionAttribute, attributeName));
            }
        }

        private
        void
        ValidateSpotLocationAttribute( string attributeName, string attributeValue )
        {
            ConfirmSpotLocation();

            if (attributeName == XpsS0Markup.StartX)
            {
                SpotLocation.StartX = double.Parse(attributeValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (attributeName == XpsS0Markup.StartY)
            {
                SpotLocation.StartY = double.Parse(attributeValue, System.Globalization.CultureInfo.InvariantCulture);   
            }
            else if (attributeName == XpsS0Markup.PageUri)
            {
                SpotLocation.PageUri = new Uri(attributeValue, UriKind.Relative);
            }
            else
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotValidSignatureDefinitionAttribute, attributeName));
            }
        }

        private
        void
        ConfirmSpotLocation()
        {
            if( SpotLocation == null )
            {
                SpotLocation = new SpotLocation();
            }
        }

        private
        void
        ReadElement( XmlReader reader )
        {
            if (reader.Name == XpsS0Markup.SpotLocation)
            {
                ReadAttributes(reader);
            }
            else if (reader.Name == XpsS0Markup.Intent)
            {
                Intent = ReadData(reader);
            }
            else if (reader.Name == XpsS0Markup.SignBy)
            {
                SignBy = DateTime.Parse(ReadData(reader), System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (reader.Name == XpsS0Markup.SigningLocale)
            {
                SigningLocale = ReadData(reader);
            }
            else
            {
                throw new XpsPackagingException(SR.Get(SRID.ReachPackaging_NotValidSignatureDefinitionElement, reader.Name));
            }
        }

        private
        string
        ReadData( XmlReader reader )
        {
            string data = null;
            if (!reader.IsEmptyElement)
            {
                bool endLoop = false;
                while (reader.Read() && !endLoop)
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Text:
                            data = reader.Value;
                            endLoop = true;
                            break;

                        case XmlNodeType.EndElement:
                            //
                            // We have an empty element return null;
                            //
                            endLoop = true;
                            break;
                    }
                }
            }
            return data;
        }

        /// <returns>
        /// Returns true if it is the end of Signature Definition
        /// the requested fixed page.
        /// </returns>
        private
        bool
        ReadEndElement( XmlReader reader )
        {
            bool ret = false;
            if( reader.Name == XpsS0Markup.SignatureDefinition )
            {
                ret =  true;
            }
            return ret;
        }

        #endregion Private methods
    }
}
    

