// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Printing;
    using System.Security;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Xml Stream base API for writing print capability documents
    /// </summary>
    internal class PrintCapabilitiesWriter
    {
        /// <remarks>
        /// We do not accept an XmlWriter because we need to control specific XML features
        /// Encode with UTF-8
        /// Include XML declaration
        /// </remarks>
        public PrintCapabilitiesWriter(Stream stream, string privateQname, string privateNamespace, bool indent)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CheckCharacters = false;
            settings.Encoding = Encoding.UTF8;
            settings.OmitXmlDeclaration = false;
            settings.CloseOutput = false;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.NamespaceHandling = NamespaceHandling.Default;
            this._writer = XmlWriter.Create(new StreamWriter(stream, Encoding.UTF8), settings);            
            this._privateNamespace = privateNamespace;
            this._privateQName = privateQname;

            // security critical member
            this._strings = new COMPSTUISR();
        }

        public void Release()
        {
            if (this._writer != null)
            {
                this._writer.Flush();
                this._writer.Close();
                this._writer = null;
            }

            if (this._strings != null)
            {
                // Security Critical call
                this._strings.Release();

                this._strings = null;
            }
        }

        public void Flush()
        {
            this._writer.Flush();
        }

        public void WriteStartDocument()
        {
            this._writer.WriteStartDocument();
            this._writer.WriteStartElement(PrintSchemaPrefixes.Framework, PrintSchemaTags.Framework.PrintCapRoot, PrintSchemaNamespaces.Framework);
            this._writer.WriteAttributeString(PrintSchemaPrefixes.xmlns, PrintSchemaPrefixes.Framework, null, PrintSchemaNamespaces.Framework);
            this._writer.WriteAttributeString(PrintSchemaPrefixes.xmlns, PrintSchemaPrefixes.xsi, null, PrintSchemaNamespaces.xsi);
            this._writer.WriteAttributeString(PrintSchemaPrefixes.xmlns, PrintSchemaPrefixes.xsd, null, PrintSchemaNamespaces.xsd);
            this._writer.WriteAttributeString("version", "1");
            if (this._privateNamespace != null && this._privateQName != null)
            {
                this._writer.WriteAttributeString(PrintSchemaPrefixes.xmlns, this._privateQName, null, this._privateNamespace);
            }
            this._writer.WriteAttributeString(PrintSchemaPrefixes.xmlns, PrintSchemaPrefixes.StandardKeywordSet, PrintSchemaNamespaces.xmlns, PrintSchemaNamespaces.StandardKeywordSet);
        }

        public void WriteEndDocument()
        {
            this._writer.WriteEndDocument();
            this._writer.Flush();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Chapter 11.1.2 - Page Imagable Size
        /// </summary>
        public void WritePageImageableSizeProperty(
            int imageableWidth, int imageableHeight, 
            int originWidth, int originHeight, 
            int extentWidth, int extentHeight)
        {
            WriteStartProperty(PrintSchemaNamespaces.StandardKeywordSet, "PageImageableSize");
            {
                WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "ImageableSizeWidth", imageableWidth);
                WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "ImageableSizeHeight", imageableHeight);
                WriteStartProperty(PrintSchemaNamespaces.StandardKeywordSet, "ImageableArea");
                {
                    WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "OriginWidth", originWidth);
                    WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "OriginHeight", originHeight);
                    WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "ExtentWidth", extentWidth);
                    WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "ExtentHeight", extentHeight);
                }
                WriteEndProperty();
            }
            WriteEndProperty();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.1 - Document Collation
        /// </summary>
        public void WriteDocumentCollateFeature() 
        {            
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "DocumentCollate",
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_COLLATE); 
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Collated", COMPSTUISR.IDS_CPSUI_YES, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Uncollated", COMPSTUISR.IDS_CPSUI_NO, "None");
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.2 - Page Color
        /// </summary>
        public void WritePageOutputColorFeature(bool supportsColor)
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageOutputColor",
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_COLOR_APPERANCE);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Monochrome", COMPSTUISR.IDS_CPSUI_MONOCHROME, "None");
                if (supportsColor)
                {
                    WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Color", COMPSTUISR.IDS_CPSUI_COLOR, "None");
                }
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.3 - Job Copies
        /// </summary>
        public void WriteJobCopiesAllDocumentsParameterDef(int minCopies, int maxCopies, int defaultCopies)
        {
            WriteStartParameterDef(PrintSchemaNamespaces.StandardKeywordSet, "JobCopiesAllDocuments", COMPSTUISR.IDS_CPSUI_NUM_OF_COPIES); 
            {
                WriteQNameProperty(PrintSchemaNamespaces.Framework, "DataType", PrintSchemaNamespaces.xsd, "integer");
                WriteProperty(PrintSchemaNamespaces.Framework, "UnitType", "copies");
                WriteProperty(PrintSchemaNamespaces.Framework, "Multiple", 1);
                WriteProperty(PrintSchemaNamespaces.Framework, "MaxValue", maxCopies);
                WriteProperty(PrintSchemaNamespaces.Framework, "MinValue", minCopies);
                WriteProperty(PrintSchemaNamespaces.Framework, "DefaultValue", defaultCopies);
                WriteQNameProperty(PrintSchemaNamespaces.Framework, "Mandatory", PrintSchemaNamespaces.StandardKeywordSet, "Unconditional");
            }
            WriteEndParameterDef();
        }

        public void WriteJobNUpAllDocumentsContiguously(IList<uint> nUps)
        {
            if (nUps.Count > 0)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "JobNUpAllDocumentsContiguously",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_NUP);
                {
                    foreach (uint nUp in nUps)
                    {
                        string nUpString = XmlConvert.ToString(nUp);
                        WriteStartOption(PrintSchemaNamespaces.StandardKeywordSet, null, nUpString, "None");
                        {
                            WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "PagesPerSheet");
                            {
                                WriteValue("xsd:integer", nUpString);
                            }
                            WriteEndScoredProperty();

                            if(nUp == 1)
                            {
                                WriteProperty(PrintSchemaNamespaces.Framework, "IdentityOption", "True");
                            }
                        }
                        WriteEndOption();
                    }
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.4 - Job Duplexing
        /// </summary>
        public void WriteJobDuplexAllDocumentsContiguouslyFeature(bool canDuplex)
        {
            if (canDuplex)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "JobDuplexAllDocumentsContiguously",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_DUPLEX);
                {
                    WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "OneSided", COMPSTUISR.IDS_CPSUI_SIMPLEX, "None");
                    WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "TwoSidedLongEdge", COMPSTUISR.IDS_CPSUI_VERTICAL, "None");
                    WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "TwoSidedShortEdge", COMPSTUISR.IDS_CPSUI_HORIZONTAL, "None");
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.5 - Page ICM Rendering Intent
        /// </summary>
        public void WritePageICMRenderingIntentFeature()
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageICMRenderingIntent",
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_ICMINTENT);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "AbsoluteColorimetric", COMPSTUISR.IDS_NULL, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "RelativeColorimetric", COMPSTUISR.IDS_CPSUI_ICM_COLORMETRIC, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Photographs", COMPSTUISR.IDS_CPSUI_ICM_CONTRAST, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "BusinessGraphics", COMPSTUISR.IDS_CPSUI_ICM_SATURATION, "None");
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.6 - Page Color Management 
        /// </summary>
        public void WritePageColorManagementFeature()
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageColorManagement",
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_ICMMETHOD);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "None", COMPSTUISR.IDS_NULL, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "System", COMPSTUISR.IDS_NULL, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Driver", COMPSTUISR.IDS_NULL, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Device",  COMPSTUISR.IDS_NULL, "None");
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.7 - Job/Document/Page Input Bin
        /// </summary>
        public void WriteJobInputBinFeature(IList<short> bins, IList<string> binDisplayNames)
        {
            if (bins.Count > 0)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "JobInputBin",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_SOURCE);
                {
                    for (int i = 0; i < bins.Count; i++)
                    {
                        string optionLocalName;
                        bool hasStandardKeywordNamespace;
                        uint optionDisplayNameId;
                        string pskFeedType;
                        string pskBinType;

                        bool result = PrintSchemaShim.TryGetPaperSourceOption(
                            bins[i],
                            out optionLocalName,
                            out hasStandardKeywordNamespace,
                            out optionDisplayNameId,
                            out pskFeedType,
                            out pskBinType);

                        if (result)
                        {
                            string optionNamespace = hasStandardKeywordNamespace ? PrintSchemaNamespaces.StandardKeywordSet : this._privateNamespace;
                            WriteStartOption(optionNamespace, optionLocalName, optionDisplayNameId, "None");
                            {
                                if (pskFeedType != null)
                                {
                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "FeedType");
                                    {
                                        WriteQNameValue(PrintSchemaNamespaces.StandardKeywordSet, pskFeedType);
                                    }
                                    WriteEndScoredProperty();
                                }

                                if (pskBinType != null)
                                {
                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "BinType");
                                    {
                                        WriteQNameValue(PrintSchemaNamespaces.StandardKeywordSet, pskBinType);
                                    }
                                    WriteEndScoredProperty();
                                }
                            }
                            WriteEndOption();
                        }
                        else
                        {
                            optionLocalName = string.Format(CultureInfo.InvariantCulture, "User{0:0000000000}", bins[i]);
                            string optionDisplayName = (i < binDisplayNames.Count) ? binDisplayNames[i] : null;
                            WriteStartOption(this._privateNamespace, optionLocalName, optionDisplayName, "None");
                            WriteEndOption();
                        }
                    }
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.8 - Page Media Size
        /// </summary>
        public void WritePageMediaSizeFeature(
            IList<short> paperSizeCodes, 
            IList<string> paperSizeDisplayNames, 
            IList<DC_PAPER_SIZE> paperSizes)
        {
            if (paperSizeCodes.Count > 0)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "PageMediaSize",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_FORMNAME);
                {
                    for (int i = 0; i < paperSizeCodes.Count; i++)
                    {                        
                        string optionNamespace = null;
                        string optionLocalName = null;
                        PageMediaSizeName mediaSizeEnum;
                        if (PrintSchemaShim.TryGetPageMediaSizeNameFromPaperSizeCode(paperSizeCodes[i], out mediaSizeEnum))
                        {
                            optionNamespace = PrintSchemaNamespaces.StandardKeywordSet;
                            optionLocalName = mediaSizeEnum.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            optionNamespace = this._privateNamespace;
                            optionLocalName = null;
                        }


                        if (optionLocalName != null &&
                            ((i < paperSizeDisplayNames.Count) || (i < paperSizes.Count)))
                        {
                            string optionDisplayName = (i < paperSizeDisplayNames.Count) ? paperSizeDisplayNames[i] : null;
                            WriteStartOption(optionNamespace, optionLocalName, optionDisplayName, "None");
                            {
                                if (i < paperSizes.Count)
                                {
                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "MediaSizeWidth");
                                    {
                                        WriteValue("xsd:integer", XmlConvert.ToString(paperSizes[i].Width));
                                    }
                                    WriteEndScoredProperty();

                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "MediaSizeHeight");
                                    {
                                        WriteValue("xsd:integer", XmlConvert.ToString(paperSizes[i].Height));
                                    }
                                    WriteEndScoredProperty();
                                }
                            }
                            WriteEndOption();
                        }                        
                    }
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.9 - Page Media Type
        /// </summary>
        public void WritePageMediaTypeFeature(IList<uint> mediaTypes, IList<string> mediaTypeDisplayNames)
        {
            if (mediaTypes.Count > 0 && mediaTypeDisplayNames.Count > 0)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "PageMediaType",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_MEDIA);
                {
                    for (int i = 0; i < mediaTypes.Count; i++)
                    {
                        string optionLocalName;
                        uint optionDisplayNameId;
                        string pskFrontCoating;
                        string pskBackCoating;
                        string pskMaterial;

                        bool found = PrintSchemaShim.TryGetMediaTypeOption(
                            mediaTypes[i], 
                            out optionLocalName, 
                            out optionDisplayNameId,
                            out pskFrontCoating,
                            out pskBackCoating,
                            out pskMaterial
                            );

                        string optionDisplayName;
                        if (found)
                        {
                            optionDisplayName = GetDisplayString(optionDisplayNameId);
                        }
                        else
                        {
                            optionLocalName = string.Format(CultureInfo.InvariantCulture, "User{0:0000000000}", mediaTypes[i]);
                            optionDisplayName = (i < mediaTypeDisplayNames.Count) ? mediaTypeDisplayNames[i] : null;
                            pskFrontCoating = null;
                            pskBackCoating = null;
                            pskMaterial = null;
                        }

                        WriteStartOption(PrintSchemaNamespaces.StandardKeywordSet, optionLocalName, optionDisplayName, "None");
                        {
                            if (pskFrontCoating != null)
                            {
                                WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "FrontCoating");
                                {
                                    WriteQNameValue(PrintSchemaNamespaces.StandardKeywordSet, pskFrontCoating);
                                }
                                WriteEndScoredProperty();
                            }

                            if (pskBackCoating != null)
                            {
                                WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "BackCoating");
                                {
                                    WriteQNameValue(PrintSchemaNamespaces.StandardKeywordSet, pskBackCoating);
                                }
                                WriteEndScoredProperty();
                            }

                            if (pskMaterial != null)
                            {
                                WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "Material");
                                {
                                    WriteQNameValue(PrintSchemaNamespaces.StandardKeywordSet, pskMaterial);
                                }
                                WriteEndScoredProperty();
                            }
                        }
                        WriteEndOption();
                    }
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.10 - Page Orientation
        /// </summary>
        public void WritePageOrientationFeature(int landscapeOrientation)
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageOrientation",
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_ORIENTATION);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Portrait", COMPSTUISR.IDS_CPSUI_PORTRAIT, "None");
                switch(landscapeOrientation)
                {
                    case 90:
                    {
                        WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Landscape", COMPSTUISR.IDS_CPSUI_LANDSCAPE, "None");
                        break;
                    }

                    case 270:
                    {
                        WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "ReverseLandscape", COMPSTUISR.IDS_CPSUI_ROT_LAND, "None");
                        break;
                    }
                }
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.11 - Page Output Quality
        /// </summary>
        public void WritePageResolutionFeature(IList<DC_RESOLUTION> resolutions)
        {
            if(resolutions.Count > 0)
            {
                WriteStartFeature(
                    PrintSchemaNamespaces.StandardKeywordSet, "PageResolution",
                    PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                    COMPSTUISR.IDS_CPSUI_RESOLUTION);
                {
                    for (int i = 0; i < resolutions.Count; i++)
                    {
                        string optionLocalName;
                        uint optionDisplayNameId;
                        bool result = PrintSchemaShim.TryGetOutputQualityOption(
                            (short)resolutions[i].x,
                            out optionLocalName,
                            out optionDisplayNameId
                            );
                        if (result)
                        {
                            WriteOption(PrintSchemaNamespaces.StandardKeywordSet, optionLocalName, optionDisplayNameId, "None");
                        }
                        else
                        {
                            if (resolutions[i].x > 0 && resolutions[i].y > 0)
                            {
                                string x = XmlConvert.ToString(resolutions[i].x);
                                string y = XmlConvert.ToString(resolutions[i].y);
                                WriteStartOption(this._privateNamespace, null, x + " x " + y, "None");
                                {
                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "ResolutionX");
                                    {
                                        WriteValue("xsd:integer", x);
                                    }
                                    WriteEndScoredProperty();

                                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "ResolutionY");
                                    {
                                        WriteValue("xsd:integer", y);
                                    }
                                    WriteEndScoredProperty();
                                }
                                WriteEndOption();
                            }
                        }
                    }
                }
                WriteEndFeature();
            }
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.12 - Page Scaling
        /// </summary>
        public void WritePageScalingFeature(int minScale, int maxScale, int defaultScale)
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageScaling", 
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_SCALING);
            {
                WriteStartOption(PrintSchemaNamespaces.StandardKeywordSet, "CustomSquare", COMPSTUISR.IDS_CPSUI_ON, "None");
                {
                    WriteStartScoredProperty(PrintSchemaNamespaces.StandardKeywordSet, "Scale");
                    {
                        WriteParameterRef(PrintSchemaNamespaces.StandardKeywordSet, "PageScalingScale");
                    }
                    WriteEndScoredProperty();
                }
                WriteEndOption();

                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "None", COMPSTUISR.IDS_CPSUI_OFF, "None");
            }
            WriteEndFeature();

            WriteStartParameterDef(PrintSchemaNamespaces.StandardKeywordSet, "PageScalingScale", COMPSTUISR.IDS_NULL);
            {
                WriteQNameProperty(PrintSchemaNamespaces.Framework, "DataType", PrintSchemaNamespaces.xsd, "integer");
                WriteProperty(PrintSchemaNamespaces.Framework, "UnitType", "percent");
                WriteProperty(PrintSchemaNamespaces.Framework, "Multiple", 1);
                WriteProperty(PrintSchemaNamespaces.Framework, "MaxValue", maxScale);
                WriteProperty(PrintSchemaNamespaces.Framework, "MinValue", minScale);
                WriteProperty(PrintSchemaNamespaces.Framework, "DefaultValue", defaultScale);
                WriteQNameProperty(PrintSchemaNamespaces.Framework, "Mandatory", PrintSchemaNamespaces.StandardKeywordSet, "Optional");
            }
            WriteEndParameterDef();

            WriteParameterInit(PrintSchemaNamespaces.StandardKeywordSet, "PageScalingScale", "xsd:string", "100");
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.13 Page True Type Font Mode
        /// </summary>
        public void WritePageTrueTypeFontModeFeature()
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageTrueTypeFontMode", 
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_TTOPTION);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "RenderAsBitmap", COMPSTUISR.IDS_CPSUI_TT_PRINTASGRAPHIC, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "DownloadAsRasterFont", COMPSTUISR.IDS_CPSUI_TT_DOWNLOADSOFT, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "DownloadAsOutlineFont", COMPSTUISR.IDS_CPSUI_TT_DOWNLOADVECT, "None");
            }
            WriteEndFeature();
        }

        /// <summary>
        /// Print Schema Reference Guide v1.0 Appendix E.3.13 Page True Type Font Mode
        /// </summary>
        public void WritePageDeviceFontSubstitutionFeature()
        {
            WriteStartFeature(
                PrintSchemaNamespaces.StandardKeywordSet, "PageDeviceFontSubstitution", 
                PrintSchemaNamespaces.StandardKeywordSet, "PickOne",
                COMPSTUISR.IDS_CPSUI_TT_SUBDEV);
            {
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "On", COMPSTUISR.IDS_CPSUI_ON, "None");
                WriteOption(PrintSchemaNamespaces.StandardKeywordSet, "Off", COMPSTUISR.IDS_CPSUI_OFF, "None");
            }
            WriteEndFeature();
        }

        public void WritePageDevmodeSnapshot(byte[] devMode)
        {
            Invariant.Assert(this._privateNamespace != null, "Cannot write PageDevmodeSnapshot without private namespace");
            Invariant.Assert(this._privateQName != null, "Cannot write PageDevmodeSnapshot without private namespace prefix");

            string base64DevMode = Convert.ToBase64String(devMode);

            // Max DEVMODE size in bytes is 2 * (max unsigned short).  
            // Scale the whole thing rounded up for its base64 equivalent.  
            // It takes 4 characters to represent 3 bytes.
            int maxDevModeSize = ((ushort.MaxValue) * 2 * 4 + 2) / 3;
            if (this._privateNamespace != null && this._privateQName != null)
            {
                WriteStartParameterDef(this._privateNamespace, "PageDevmodeSnapshot", COMPSTUISR.IDS_NULL);
                {
                    WriteQNameProperty(PrintSchemaNamespaces.Framework, "DataType", PrintSchemaNamespaces.xsd, "string");
                    WriteProperty(PrintSchemaNamespaces.Framework, "UnitType", "base64");
                    WriteProperty(PrintSchemaNamespaces.Framework, "DefaultValue", base64DevMode);
                    WriteQNameProperty(PrintSchemaNamespaces.Framework, "Mandatory", PrintSchemaNamespaces.StandardKeywordSet, "Optional");
                    WriteProperty(PrintSchemaNamespaces.Framework, "MinLength", 0);
                    WriteProperty(PrintSchemaNamespaces.Framework, "MaxLength", maxDevModeSize);
                }
                WriteEndParameterDef();
            }
        }

        private void WriteStartFeature(
            string featureNamespace, string featureName, 
            string selectionTypeNamespace, string selectionTypeName,
            uint displayNameId)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Feature, PrintSchemaNamespaces.Framework);
            WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, featureNamespace, featureName);
            WriteQNameProperty(PrintSchemaNamespaces.Framework, "SelectionType", selectionTypeNamespace, selectionTypeName);

            WriteDisplayNameProperty(displayNameId);
        }

        private void WriteEndFeature()
        {
            this._writer.WriteEndElement();
        }

        private void WriteStartParameterDef(string paramDefNamespace, string paramDefName, uint displayNameId)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.ParameterDef, PrintSchemaNamespaces.Framework);
            WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, paramDefNamespace, paramDefName);
            WriteDisplayNameProperty(displayNameId);
        }

        private void WriteEndParameterDef()
        {
            this._writer.WriteEndElement();
        }

        private void WriteStartOption(string optionNamespace, string optionName, string displayName, string pskConstraint)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Option, PrintSchemaNamespaces.Framework);
            if (optionName != null)
            {
                WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, optionNamespace, optionName);
            }
            WriteQNameAttribute(null, "constrained", PrintSchemaNamespaces.StandardKeywordSet, pskConstraint);

            WriteDisplayNameProperty(displayName);
        }

        private void WriteStartOption(string optionNamespace, string optionName, uint displayNameId, string pskConstraint)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Option, PrintSchemaNamespaces.Framework);
            if (optionName != null)
            {
                WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, optionNamespace, optionName);
            }
            WriteQNameAttribute(null, "constrained", PrintSchemaNamespaces.StandardKeywordSet, pskConstraint);

            WriteDisplayNameProperty(displayNameId);
        }

        private void WriteEndOption()
        {
            this._writer.WriteEndElement();
        }

        private void WriteOption(string optionNamespace, string optionName, uint displayNameId, string pskConstraint)
        {
            WriteStartOption(optionNamespace, optionName, displayNameId, pskConstraint);
            WriteEndOption();
        }

        private void WriteDisplayNameProperty(uint displayNameId)
        {

            string displayName = GetDisplayString(displayNameId);

            if (displayName != null)
            {
                WriteDisplayNameProperty(displayName);
            }
        }

        private void WriteDisplayNameProperty(string displayName)
        {
            WriteProperty(PrintSchemaNamespaces.StandardKeywordSet, "DisplayName", displayName);
        }

        private void WriteProperty(string propertyNamespace, string propertyName, int propertyValue)
        {
            WriteProperty(propertyNamespace, propertyName, "xsd:integer", XmlConvert.ToString(propertyValue));
        }

        private void WriteProperty(string propertyNamespace, string propertyName, string propertyValue)
        {
            WriteProperty(propertyNamespace, propertyName, "xsd:string", propertyValue);
        }

        private void WriteQNameProperty(string propertyNamespace, string propertyName, string valueNamespace, string valueLocalName)
        {
            WriteStartProperty(propertyNamespace, propertyName);
            {
                WriteQNameValue(valueNamespace, valueLocalName);
            }
            WriteEndProperty();
        }

        private void WriteProperty(string propertyNamespace, string propertyName, string type, string propertyValue)
        {
            WriteStartProperty(propertyNamespace, propertyName);
            {
                WriteValue(type, propertyValue);
            }
            WriteEndProperty();
        }

        private void WriteStartProperty(string propertyNamespace, string propertyName)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Property, PrintSchemaNamespaces.Framework);
            WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, propertyNamespace, propertyName);
        }

        private void WriteEndProperty()
        {
            this._writer.WriteEndElement();
        }

        private void WriteStartScoredProperty(string propertyNamespace, string propertyName)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.ScoredProperty, PrintSchemaNamespaces.Framework);
            WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, propertyNamespace, propertyName);
        }

        private void WriteEndScoredProperty()
        {
            this._writer.WriteEndElement();
        }

        private void WriteValue(string type, string propertyValue)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Value, PrintSchemaNamespaces.Framework);
            {
                this._writer.WriteAttributeString("type", PrintSchemaNamespaces.xsi, type);
                this._writer.WriteString(propertyValue);
            }
            this._writer.WriteEndElement();
        }

        private void WriteQNameValue(string valueNamespace, string valueLocalName)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.Value, PrintSchemaNamespaces.Framework);
            {
                WriteQNameAttribute(PrintSchemaNamespaces.xsi, "type", PrintSchemaNamespaces.xsd, "QName");
                this._writer.WriteQualifiedName(valueLocalName, valueNamespace);
            }
            this._writer.WriteEndElement();
        }

        private void WriteParameterRef(string parameterNamespace, string parameterName)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.ParameterRef, PrintSchemaNamespaces.Framework);
            {
                WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, parameterNamespace, parameterName);
            }
            this._writer.WriteEndElement();
        }

        private void WriteParameterInit(string parameterNamespace, string parameterName, string type, string value)
        {
            this._writer.WriteStartElement(PrintSchemaTags.Framework.ParameterInit, PrintSchemaNamespaces.Framework);
            {
                WriteQNameAttribute(null, PrintSchemaTags.Framework.NameAttr, parameterNamespace, parameterName);
                WriteValue(type, value);
            }
            this._writer.WriteEndElement();
        }

        private void WriteQNameAttribute(string attrNamespace, string attrLocalName, string valueNamespace, string valueLocalName)
        {
            this._writer.WriteStartAttribute(attrLocalName, attrNamespace);
            {
                this._writer.WriteQualifiedName(valueLocalName, valueNamespace);
            }
            this._writer.WriteEndAttribute();
        }

        private string GetDisplayString(uint srid)
        {
            return _strings.Get(srid);
        }

        private COMPSTUISR _strings;

        private XmlWriter _writer;
        private string _privateNamespace;
        private string _privateQName;
    }
}
