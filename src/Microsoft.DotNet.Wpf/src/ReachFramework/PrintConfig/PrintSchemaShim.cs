// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Printing;
    using System.Xml;
    using System.Xml.XPath;
    using System.Security;

    /// <summary>
    /// Encapulates all the logic for marshaling PrintTicket documents to DEVMODE
    /// Based on the XPS Print Schema Specification v1.0
    /// http://www.microsoft.com/whdc/xps/printschema.mspx
    /// </summary>
    internal static class PrintSchemaShim
    {
        public static bool TryGetPageMediaSizeNameFromPaperSizeCode(short paperSizeCode, out PageMediaSizeName pageMediaSizeName)
        {
            return dmPaperSizeToPageMediaSize.TryGetValue(paperSizeCode, out pageMediaSizeName);
        }

        public static bool TryGetPageResolutionFromPaperQuality(short paperQualityCode, out PageQualitativeResolution resolution)
        {
            return dmResToQResolution.TryGetValue(paperQualityCode, out resolution);
        }

        public static bool TryGetPaperSourceOption(
            short paperSourceCode,
            out string localName,
            out bool hasStandardKeywordNamespace,
            out uint displayNameId,
            out string pskFeedType,
            out string pskBinType)
        {
            PaperSourceOption option;
            if (paperSourceOptions.TryGetValue(paperSourceCode, out option))
            {
                localName = option.Name;
                hasStandardKeywordNamespace = option.HasStandardKeywordNamespace;
                displayNameId = option.DisplayNameId;
                pskFeedType = option.pskFeedType;
                pskBinType = option.pskBinType;
                return true;
            }
            else
            {
                localName = null;
                hasStandardKeywordNamespace = false;
                displayNameId = COMPSTUISR.IDS_NULL;
                pskFeedType = null;
                pskBinType = null;
                return false;
            }
        }

        public static bool TryGetMediaTypeOption(
            uint mediaTypeCode,
            out string localName,
            out uint displayNameId,
            out string pskFrontCoating,
            out string pskBackCoating,
            out string pskMaterial
            )
        {
            MediaTypeOption option;
            if (mediaTypeOptions.TryGetValue(mediaTypeCode, out option))
            {
                localName = option.Name;
                displayNameId = option.DisplayNameId;
                pskFrontCoating = option.pskFrontCoating;
                pskBackCoating = option.pskBackCoating;
                pskMaterial = option.pskMaterial;
                return true;
            }
            else
            {
                localName = null;
                displayNameId = COMPSTUISR.IDS_NULL;
                pskFrontCoating = null;
                pskBackCoating = null;
                pskMaterial = null;
                return false;
            }
        }

        public static bool TryGetOutputQualityOption(
            short dmResX,
            out string localName,
            out uint displayNameId
            )
        {
            OutputQualityOption option;
            if (outputQualityOptions.TryGetValue(dmResX, out option))
            {
                localName = option.Name;
                displayNameId = option.DisplayNameId;
                return true;
            }
            else
            {
                localName = null;
                displayNameId = COMPSTUISR.IDS_NULL;
                return false;
            }
        }

        public static bool TryEmbedDevMode(InternalPrintTicket ticket, string oemDriverNamespace, DevMode devMode)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException("ticket");
            }

            if (oemDriverNamespace == null)
            {
                throw new ArgumentNullException("ticket");
            }

            if (devMode == null)
            {
                return true;
            }

            string oemDriverPrefix = EnsurePrefixForTicket(ticket, oemDriverNamespace);

            if (!string.IsNullOrEmpty(oemDriverPrefix))
            {

                //<psf:ParameterInit name="oem0000:PageDevmodeSnapshot">
                // <psf:Value xsi:type="xsd:string">base64string</psf:Value>
                //</psf:ParameterInit>                
                XmlElement parameterInitElem = ticket.XmlDoc.CreateElement(PrintSchemaPrefixes.Framework, PrintSchemaTags.Framework.ParameterInit, PrintSchemaNamespaces.Framework);
                {
                    XmlAttribute nameAttr = ticket.XmlDoc.CreateAttribute("name");
                    {
                        nameAttr.Value = oemDriverPrefix + ":PageDevmodeSnapshot";
                    }
                    parameterInitElem.Attributes.Append(nameAttr);

                    XmlElement valueElem = ticket.XmlDoc.CreateElement(PrintSchemaPrefixes.Framework, PrintSchemaTags.Framework.Value, PrintSchemaNamespaces.Framework);
                    {
                        XmlText textNode = ticket.XmlDoc.CreateTextNode(Convert.ToBase64String(devMode.ByteData, Base64FormattingOptions.None));
                        valueElem.AppendChild(textNode);
                    }
                    parameterInitElem.AppendChild(valueElem);
                }
                ticket.XmlDoc.DocumentElement.AppendChild(parameterInitElem);

                return true;
            }

            return false;
        }

        public static DevMode TryGetEmbeddedDevMode(InternalPrintTicket ticket, string oemDriverNamespace)
        {
            string psfPrefix = EnsurePrefixForTicket(ticket, PrintSchemaNamespaces.Framework);
            string oemDriverPrefix = EnsurePrefixForTicket(ticket, oemDriverNamespace);

            if (!string.IsNullOrEmpty(psfPrefix) && !string.IsNullOrEmpty(oemDriverPrefix))
            {
                XPathNavigator docNavigator = ticket.XmlDoc.DocumentElement.CreateNavigator();
                if (docNavigator != null)
                {
                    string xPathString = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        @"{0}:PrintTicket/{0}:ParameterInit[@name='{1}:PageDevmodeSnapshot']/{0}:Value",
                        psfPrefix,
                        oemDriverPrefix
                    );

                    XPathNavigator node = docNavigator.SelectSingleNode(xPathString, ticket.NamespaceManager);
                    if (node != null)
                    {
                        // Embedded DEVMODE found
                        return new DevMode(Convert.FromBase64String(node.Value));
                    }
                }
            }

            // Could not detect or create an xmlns prefix for the driver oem namespace
            return null;
        }

        /// <summary>
        /// Applies fields set on deltaDevMode to baseTicket
        /// </summary>
        /// <param name="baseTicket">Print ticket to apply delta to</param>
        /// <param name="deltaDevMode">DEVMODE containing delta's to apply</param>
        /// <param name="scope">Scopes print ticket properties, useful for disambiguating print ticket properties</param>
        public static void CopyDevModeToTicket(InternalPrintTicket baseTicket, DevMode deltaDevMode, PrintTicketScope scope, DevModeFields supportedFields)
        {
            if (deltaDevMode == null)
            {
                return;
            }

            if (scope >= PrintTicketScope.DocumentScope)
            {
                if (IsSet(DevModeFields.DM_COLLATE, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_COLLATE))
                {
                    SetCollation(baseTicket, deltaDevMode.Collate);
                }
            }

            if (scope >= PrintTicketScope.PageScope)
            {
                if (IsSet(DevModeFields.DM_COLOR, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_COLOR))
                {
                    SetOutputColor(baseTicket, deltaDevMode.Color);
                }
            }


            if (scope >= PrintTicketScope.JobScope)
            {
                if (IsSet(DevModeFields.DM_COPIES, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_COPIES))
                {
                    SetCopies(baseTicket, deltaDevMode.Copies);
                }
            }


            if (scope >= PrintTicketScope.PageScope)
            {

                if (IsSet(DevModeFields.DM_SCALE, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_SCALE))
                {
                    SetScale(baseTicket, deltaDevMode.Scale);
                }

                if (IsSet(DevModeFields.DM_PAPERSIZE, supportedFields) 
                    && IsSet(DevModeFields.DM_PAPERWIDTH, supportedFields) 
                    && IsSet(DevModeFields.DM_PAPERLENGTH, supportedFields) 
                    && deltaDevMode.IsAnyFieldSet(DevModeFields.DM_PAPERSIZE | DevModeFields.DM_PAPERWIDTH | DevModeFields.DM_PAPERLENGTH))
                {
                    SetPageMediaSize(baseTicket, deltaDevMode.Fields, deltaDevMode.PaperSize, deltaDevMode.PaperWidth, deltaDevMode.PaperLength, deltaDevMode.FormName);
                }

                if (IsSet(DevModeFields.DM_PRINTQUALITY, supportedFields) 
                    && IsSet(DevModeFields.DM_YRESOLUTION, supportedFields) 
                    && deltaDevMode.IsAnyFieldSet(DevModeFields.DM_PRINTQUALITY | DevModeFields.DM_YRESOLUTION))
                {
                    SetPageResolution(baseTicket, deltaDevMode.Fields, deltaDevMode.PrintQuality, deltaDevMode.YResolution);
                }

                if (IsSet(DevModeFields.DM_ORIENTATION, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_ORIENTATION))
                {
                    SetOrientation(baseTicket, deltaDevMode.Orientation);
                }

                if (IsSet(DevModeFields.DM_TTOPTION, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_TTOPTION))
                {
                    SetTrueTypeFontOption(baseTicket, deltaDevMode.TTOption);
                }

                if (IsSet(DevModeFields.DM_MEDIATYPE, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_MEDIATYPE))
                {
                    SetPageMediaType(baseTicket, deltaDevMode.MediaType);
                }
            }

            if (IsSet(DevModeFields.DM_DEFAULTSOURCE, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_DEFAULTSOURCE))
            {
                SetInputBin(baseTicket, scope, deltaDevMode.DefaultSource);
            }

            if (scope >= PrintTicketScope.JobScope)
            {
                if (IsSet(DevModeFields.DM_DUPLEX, supportedFields) && deltaDevMode.IsFieldSet(DevModeFields.DM_DUPLEX))
                {
                    SetDuplexing(baseTicket, deltaDevMode.Duplex);
                }
            }
        }

        /// <summary>
        /// Applies any public fields set on deltaTicket to baseDevMode
        /// </summary>
        /// <param name="baseDevMode">DEVMODE to apply delta to</param>
        /// <param name="deltaTicket">Print ticket containing deltas</param>
        /// <param name="scope">Scope of print ticket fields to apply</param>
        public static void CopyTicketToDevMode(DevMode baseDevMode, InternalPrintTicket deltaTicket, PrintTicketScope scope, DevModeFields supportedFields)
        {
            if (deltaTicket == null)
            {
                return;
            }

            if (scope >= PrintTicketScope.PageScope)
            {
                if (IsSet(DevModeFields.DM_DEFAULTSOURCE, supportedFields)
                    && deltaTicket.PageInputBin.Value != InputBin.Unknown)
                {
                    SetDefaultSource(baseDevMode, deltaTicket.PageInputBin.Value);
                }

                if (IsSet(DevModeFields.DM_TTOPTION, supportedFields)
                    && (deltaTicket.PageDeviceFontSubstitution.Value != DeviceFontSubstitution.Unknown || deltaTicket.PageTrueTypeFontMode.Value != TrueTypeFontMode.Unknown))
                {
                    SetTTOption(
                        baseDevMode,
                        deltaTicket.PageDeviceFontSubstitution.Value,
                        deltaTicket.PageTrueTypeFontMode.Value);
                }

                if (IsSet(DevModeFields.DM_COLOR, supportedFields)
                    && deltaTicket.PageOutputColor.Value != OutputColor.Unknown)
                {
                    SetColor(baseDevMode, deltaTicket.PageOutputColor.Value);
                }

                if (deltaTicket.PageResolution.QualitativeResolution != PageQualitativeResolution.Unknown)
                {
                    SetPrintQuality(baseDevMode, deltaTicket.PageResolution.QualitativeResolution, deltaTicket.PageResolution.ResolutionX, deltaTicket.PageResolution.ResolutionY, supportedFields);
                }

                if (IsSet(DevModeFields.DM_PAPERWIDTH, supportedFields)
                    && deltaTicket.PageMediaSize.MediaSizeWidth > 0)
                {
                    SetPageWidth(baseDevMode, deltaTicket.PageMediaSize.MediaSizeWidth);
                }

                if (IsSet(DevModeFields.DM_PAPERLENGTH, supportedFields)
                    && deltaTicket.PageMediaSize.MediaSizeHeight > 0)
                {
                    SetPageHeight(baseDevMode, deltaTicket.PageMediaSize.MediaSizeHeight);
                }

                if (IsSet(DevModeFields.DM_PAPERSIZE, supportedFields)
                    && deltaTicket.PageMediaSize.Value != PageMediaSizeName.Unknown)
                {
                    SetPaperSize(baseDevMode, deltaTicket.PageMediaSize.Value);
                }

                if (IsSet(DevModeFields.DM_MEDIATYPE, supportedFields)
                    && deltaTicket.PageMediaType.Value == PageMediaType.Unknown)
                {
                    SetMediaType(baseDevMode, deltaTicket.PageMediaType.Value);
                }

                if (IsSet(DevModeFields.DM_ORIENTATION, supportedFields)
                    && deltaTicket.PageOrientation.Value != PageOrientation.Unknown)
                {
                    SetOrientation(baseDevMode, deltaTicket.PageOrientation.Value);
                }

                if (IsSet(DevModeFields.DM_SCALE, supportedFields))
                {
                    switch (deltaTicket.PageScaling.Value)
                    {
                        case PageScaling.Custom:
                        {
                            SetScale(baseDevMode, deltaTicket.PageScaling.CustomScaleWidth);
                            break;
                        }

                        case PageScaling.CustomSquare:
                        {
                            SetScale(baseDevMode, deltaTicket.PageScaling.CustomSquareScale);
                            break;
                        }

                        default:
                        {
                            SetScale(baseDevMode, 100);
                            break;
                        }
                    }
                }
            }

            if (scope >= PrintTicketScope.DocumentScope)
            {
                if (IsSet(DevModeFields.DM_DEFAULTSOURCE, supportedFields) 
                    && deltaTicket.DocumentInputBin.Value != InputBin.Unknown)
                {
                    SetDefaultSource(baseDevMode, deltaTicket.DocumentInputBin.Value);
                }

                if (IsSet(DevModeFields.DM_COLLATE, supportedFields) 
                    && deltaTicket.DocumentCollate.Value != Collation.Unknown)
                {
                    SetCollate(baseDevMode, deltaTicket.DocumentCollate.Value);
                }
            }

            if (scope >= PrintTicketScope.JobScope)
            {
                if (IsSet(DevModeFields.DM_DEFAULTSOURCE, supportedFields) 
                    && deltaTicket.JobInputBin.Value != InputBin.Unknown)
                {
                    SetDefaultSource(baseDevMode, deltaTicket.JobInputBin.Value);
                }

                if (IsSet(DevModeFields.DM_COPIES, supportedFields) 
                    && deltaTicket.JobCopyCount.Value > 0)
                {
                    SetCopies(baseDevMode, deltaTicket.JobCopyCount.Value);
                }

                if (IsSet(DevModeFields.DM_DUPLEX, supportedFields) 
                    && deltaTicket.JobDuplex.Value != Duplexing.Unknown)
                {
                    SetDuplex(baseDevMode, deltaTicket.JobDuplex.Value);
                }
            }
        }

        public static bool PruneFeatures(DevMode inDevMode, WinSpoolPrinterCapabilities capabilities)
        {
            bool featurePruned = false;

            IList<short> bins = capabilities.Bins;
            if (bins.Count < 1)
            {
                inDevMode.DefaultSource = 0;
            }
            else
            {
                if (inDevMode.IsFieldSet(DevModeFields.DM_DEFAULTSOURCE) && !bins.Contains(inDevMode.DefaultSource))
                {
                    inDevMode.DefaultSource = bins[0];
                    featurePruned = true;
                }
            }

            IList<uint> mediaTypes = capabilities.MediaTypes;
            if (mediaTypes.Count < 1)
            {
                inDevMode.MediaType = 0;
            }
            else
            {
                if (inDevMode.IsFieldSet(DevModeFields.DM_MEDIATYPE) && !mediaTypes.Contains(inDevMode.MediaType))
                {
                    inDevMode.MediaType = mediaTypes[0];
                    featurePruned = true;
                }
            }

            IList<short> papers = capabilities.Papers;
            if (papers.Count < 1)
            {
                inDevMode.PaperSize = 0;
            }
            else
            {
                if (inDevMode.IsFieldSet(DevModeFields.DM_PAPERSIZE) && !papers.Contains(inDevMode.PaperSize))
                {
                    inDevMode.PaperSize = papers[0];
                    featurePruned = true;
                }
            }

            return featurePruned;
        }

        private static string EnsurePrefixForTicket(InternalPrintTicket ticket, string xmlNamespace)
        {
            XmlNamespaceManager xmlNsMgr = ticket.NamespaceManager;
            string prefix = xmlNsMgr.LookupPrefix(xmlNamespace);

            if (prefix == null)
            {
                prefix = PrintTicketEditor.AddStdNamespaceDeclaration(ticket.XmlDoc.DocumentElement, "ns", xmlNamespace);
            }

            return prefix;
        }

        public static IList<DC_PAPER_SIZE> TenthOfMillimeterToMicrons(IList<DC_PAPER_SIZE> points)
        {
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = TenthOfMillimeterToMicrons(points[i]);
                }
            }

            return points;
        }

        public static DC_PAPER_SIZE TenthOfMillimeterToMicrons(DC_PAPER_SIZE size)
        {
            return new DC_PAPER_SIZE(
                TenthOfMillimeterToMicrons(size.Width),
                TenthOfMillimeterToMicrons(size.Height));
        }


        public static int TenthOfMillimeterToMicrons(int tenthOfMillimeterLength)
        {
            return tenthOfMillimeterLength * MicronsPerTenthOfMillimeter;
        }

        public static long TenthOfMillimeterToMicrons(long tenthOfMillimeterLength)
        {
            return tenthOfMillimeterLength * MicronsPerTenthOfMillimeter;
        }

        public static int DpiToMicrons(int dpiLength, int dpi)
        {
            return (dpiLength * MicronsPerInch) / dpi;
        }

        public const int WpfPixelsPerInch = 96;
        public const int MicronsPerInch = 25400;
        public const int MicronsPerTenthOfMillimeter = 100;

        #region Print Schema Reference Guide v1.0 Appendix E.3.1 - Document Collation

        private static void SetCollation(InternalPrintTicket ticket, DevModeCollate devModeCollate)
        {
            TrySet(dmCollateToQResolution, devModeCollate, (collation) => ticket.DocumentCollate.Value = collation);
        }

        private static void SetCollate(DevMode devMode, Collation collation)
        {
            TrySet(dmCollateToQResolution, collation, (collate) => devMode.Collate = collate);
        }

        private static readonly IDictionary<DevModeCollate, Collation> dmCollateToQResolution = new Dictionary<DevModeCollate, Collation>(2) {
            {DevModeCollate.DMCOLLATE_FALSE, Collation.Uncollated},            
            {DevModeCollate.DMCOLLATE_TRUE, Collation.Collated},            
        };

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.2 - Page Color

        private static void SetOutputColor(InternalPrintTicket ticket, DevModeColor devModeColor)
        {
            TrySet(dmColorToOutputColor, devModeColor, (outputColor) => ticket.PageOutputColor.Value = outputColor);
        }

        private static void SetColor(DevMode devMode, OutputColor outputColor)
        {
            if (!TrySet(dmColorToOutputColor, outputColor, (color) => devMode.Color = color))
            {
                if (outputColor == OutputColor.Grayscale)
                {
                    devMode.Color = DevModeColor.DMCOLOR_MONOCHROME;
                }
            }
        }

        private static readonly IDictionary<DevModeColor, OutputColor> dmColorToOutputColor = new Dictionary<DevModeColor, OutputColor>(2) {
            {DevModeColor.DMCOLOR_COLOR, OutputColor.Color},            
            {DevModeColor.DMCOLOR_MONOCHROME, OutputColor.Monochrome},            
        };

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.3 - Job Copies

        private static void SetCopies(InternalPrintTicket ticket, short devModeCopies)
        {
            ticket.JobCopyCount.Value = Clamp(devModeCopies, 1, short.MaxValue);
        }

        private static void SetCopies(DevMode devMode, int copyCount)
        {
            devMode.Copies = Clamp(copyCount, 1, short.MaxValue);
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.4 - Job Duplexing

        private static void SetDuplexing(InternalPrintTicket ticket, DevModeDuplex devModeDuplex)
        {
            TrySet(dmDuplexToDuplexing, devModeDuplex, (duplexing) => ticket.JobDuplex.Value = duplexing);
        }

        private static void SetDuplex(DevMode devMode, Duplexing duplexing)
        {
            TrySet(dmDuplexToDuplexing, duplexing, (duplex) => devMode.Duplex = duplex);
        }

        private static readonly IDictionary<DevModeDuplex, Duplexing> dmDuplexToDuplexing = new Dictionary<DevModeDuplex, Duplexing>(3) {
            {DevModeDuplex.DMDUP_SIMPLEX, Duplexing.OneSided},            
            {DevModeDuplex.DMDUP_HORIZONTAL, Duplexing.TwoSidedShortEdge},            
            {DevModeDuplex.DMDUP_VERTICAL, Duplexing.TwoSidedLongEdge},            
        };

        #endregion

#if false
        #region Print Schema Reference Guide v1.0 Appendix E.3.5 - Page ICM Rendering Intent

        private static readonly IDictionary<uint, PageICMRenderingIntent> dmICMIntentsToICMRenderingIntent = new Dictionary<uint, PageICMRenderingIntent>(4) {
            {DevModeICMIntents.DMICM_ABS_COLORIMETRIC, PageICMRenderingIntent.AbsoluteColorimetric},            
            {DevModeICMIntents.DMICM_COLORIMETRIC, PageICMRenderingIntent.RelativeColorimetric},            
            {DevModeICMIntents.DMICM_CONTRAST, PageICMRenderingIntent.Photographs},            
            {DevModeICMIntents.DMICM_SATURATE, PageICMRenderingIntent.BusinessGraphics},            
        };

        private static void SetICMIntents(InternalPrintTicket ticket, uint devModeICMIntentsCode)
        {            
            TrySet(dmICMIntentsToICMRenderingIntent, devModeICMIntentsCode, (iCMRenderingIntent) => ticket.PageICMRenderingIntent = iCMRenderingIntent);
        }

        private static void SetICMIntents(DEVMODEW devMode, PageICMRenderingIntent iCMRenderingIntent)
        {
            TrySet(dmICMIntentsToICMRenderingIntent, iCMRenderingIntent, (dmICMIntentCode) => devMode.ICMIntent = dmICMIntentCode);
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.6 - Page Color Management 

        private static readonly IDictionary<DevModeICMMethod, PageColorManagement> dmICMMethodToColorManagement = new Dictionary<DevModeICMMethod, PageColorManagement>(4) {
            {DevModeICMMethod.DMICMMETHOD_NONE, PageColorManagement.None },            
            {DevModeICMMethod.DMICMMETHOD_SYSTEM, PageColorManagement.System},            
            {DevModeICMMethod.DMICMMETHOD_DRIVER, PageColorManagement.Driver},            
            {DevModeICMMethod.DMICMMETHOD_DEVICE, PageColorManagement.Device},            
        };

        private static void SetColorManagement(InternalPrintTicket ticket, DevModeICMMethod devModeICMMethod)
        {            
            TrySet(dmICMMethodToColorManagement, devModeICMMethod, (colorManagement) => ticket.PageColorManagement = colorManagement);
        }

        private static void SetICMMethod(DEVMODEW devMode, PageColorManagement colorManagement)
        {
            TrySet(dmICMMethodToColorManagement, colorManagement, (devModeICMMethod) => devMode.ICMMethod = devModeICMMethod);
        }

        #endregion

#endif

        #region Print Schema Reference Guide v1.0 Appendix E.3.7 - Job/Document/Page Input Bin

        private static void SetInputBin(InternalPrintTicket ticket, PrintTicketScope scope, short devModePaperSouceCode)
        {
            InputBinSetting setting = null;

            switch (scope)
            {
                case PrintTicketScope.JobScope:
                {
                    setting = ticket.JobInputBin;
                    break;
                }

                case PrintTicketScope.DocumentScope:
                {
                    setting = ticket.DocumentInputBin;
                    break;
                }

                case PrintTicketScope.PageScope:
                {
                    setting = ticket.PageInputBin;
                    break;
                }

                default:
                {
                    Debug.Assert(false, "PrintTicketScope enum is out of range");
                    break;
                }
            }

            if (setting != null)
            {
                TrySet(dmPaperSourceToInputBin, devModePaperSouceCode, (inputBin) => setting.Value = inputBin);
            }
        }

        private static void SetDefaultSource(DevMode devMode, InputBin inputBin)
        {
            TrySet(dmPaperSourceToInputBin, inputBin, (defaultSource) => devMode.DefaultSource = defaultSource);
        }

        private static readonly IDictionary<short, InputBin> dmPaperSourceToInputBin = new Dictionary<short, InputBin>(4) {
            {DevModePaperSources.DMBIN_AUTO, InputBin.AutoSelect},
            {DevModePaperSources.DMBIN_CASSETTE, InputBin.Cassette },
            {DevModePaperSources.DMBIN_MANUAL , InputBin.Manual},            
            {DevModePaperSources.DMBIN_TRACTOR , InputBin.Tractor}
        };

        private static IDictionary<short, PaperSourceOption> paperSourceOptions = new Dictionary<short, PaperSourceOption>() {
            {DevModePaperSources.DMBIN_UPPER, new PaperSourceOption("Upper", false, COMPSTUISR.IDS_CPSUI_UPPER_TRAY, null, null)},
            {DevModePaperSources.DMBIN_LOWER, new PaperSourceOption("Lower", false, COMPSTUISR.IDS_CPSUI_LOWER_TRAY, null, null)},
            {DevModePaperSources.DMBIN_MIDDLE, new PaperSourceOption("Middle", false, COMPSTUISR.IDS_CPSUI_MIDDLE_TRAY, null, null)},
            {DevModePaperSources.DMBIN_MANUAL, new PaperSourceOption("Manual", true, COMPSTUISR.IDS_CPSUI_MANUAL_TRAY, "Manual", null)},
            {DevModePaperSources.DMBIN_ENVELOPE, new PaperSourceOption("EnvelopeFeed", false, COMPSTUISR.IDS_CPSUI_ENVELOPE_TRAY, null, null)},
            {DevModePaperSources.DMBIN_ENVMANUAL, new PaperSourceOption("EnvelopeFeed", false, COMPSTUISR.IDS_CPSUI_ENVMANUAL_TRAY, "Manual", null)},
            {DevModePaperSources.DMBIN_AUTO, new PaperSourceOption("Auto", false, COMPSTUISR.IDS_CPSUI_DEFAULT_TRAY, null, null)},
            {DevModePaperSources.DMBIN_TRACTOR, new PaperSourceOption("Tractor", true, COMPSTUISR.IDS_CPSUI_TRACTOR_TRAY, null, "Continuous")},
            {DevModePaperSources.DMBIN_SMALLFMT, new PaperSourceOption("SmallFormat", false, COMPSTUISR.IDS_CPSUI_SMALLFMT_TRAY, null, null)},
            {DevModePaperSources.DMBIN_LARGEFMT, new PaperSourceOption("LargeFormat", false, COMPSTUISR.IDS_CPSUI_LARGEFMT_TRAY, null, null)},
            {DevModePaperSources.DMBIN_LARGECAPACITY, new PaperSourceOption("High", false, COMPSTUISR.IDS_CPSUI_LARGECAP_TRAY, null, null)},
            {DevModePaperSources.DMBIN_CASSETTE, new PaperSourceOption("Cassette", true, COMPSTUISR.IDS_CPSUI_CASSETTE_TRAY, null, null)},
            {DevModePaperSources.DMBIN_FORMSOURCE, new PaperSourceOption("AutoSelect", true, COMPSTUISR.IDS_CPSUI_FORMSOURCE, null, null)}
        };

        private struct PaperSourceOption
        {
            public PaperSourceOption(string name, bool hasStandardKeywordNamespace, uint displayNameId, string pskFeedType, string pskBinType)
            {
                this.HasStandardKeywordNamespace = hasStandardKeywordNamespace;
                this.Name = name;
                this.DisplayNameId = displayNameId;
                this.pskFeedType = pskFeedType;
                this.pskBinType = pskBinType;
            }

            public readonly bool HasStandardKeywordNamespace;
            public readonly string Name;
            public readonly uint DisplayNameId;
            public readonly string pskFeedType;
            public readonly string pskBinType;
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.8 - Page Media Size

        private static void SetPageMediaSize(InternalPrintTicket ticket, DevModeFields devModeFields, short devModePaperSource, short devModePaperWidth, short devModePaperHeight, string devModeFormName)
        {
            PageMediaSizeName pageMediaSizeName = PageMediaSizeName.Unknown;
            double w = 0;
            double h = 0;

            bool hasPaperSize = (devModeFields & DevModeFields.DM_PAPERSIZE) == DevModeFields.DM_PAPERSIZE;
            bool hasFoundPaperSize = false;

            if (hasPaperSize)
            {
                hasFoundPaperSize = TrySet(dmPaperSizeToPageMediaSize, devModePaperSource, (sizeName) => ticket.PageMediaSize.SetFixedMediaSize(sizeName));
                if (hasFoundPaperSize)
                {
                    pageMediaSizeName = ticket.PageMediaSize.Value;
                    w = ticket.PageMediaSize.MediaSizeWidth;
                    h = ticket.PageMediaSize.MediaSizeHeight;
                }
            }

            bool hasWidth = (devModeFields & DevModeFields.DM_PAPERWIDTH) == DevModeFields.DM_PAPERWIDTH;
            bool hasHeight = (devModeFields & DevModeFields.DM_PAPERLENGTH) == DevModeFields.DM_PAPERLENGTH;
            if (hasWidth || hasHeight)
            {
                if (hasWidth)
                {
                    // If there is a width override
                    w = LengthValueFromTenthOfMillimeterToDIP(Clamp(devModePaperWidth, 1, short.MaxValue));
                }

                if (hasHeight)
                {
                    // If there is a height override
                    h = LengthValueFromTenthOfMillimeterToDIP(Clamp(devModePaperHeight, 1, short.MaxValue));
                }

                if (hasFoundPaperSize)
                {
                    ticket.PageMediaSize.SetFixedMediaSize(pageMediaSizeName, w, h);
                }
                else
                {
                    ticket.PageMediaSize.SetFixedMediaSize(w, h);
                }
            }
        }

        private static void SetPaperSize(DevMode devMode, PageMediaSizeName pageMediaSizeName)
        {
            TrySet(dmPaperSizeToPageMediaSize, pageMediaSizeName, (paperSizeCode) => devMode.PaperSize = paperSizeCode);
        }

        private static void SetPageHeight(DevMode devMode, double pageHeightInDIPs)
        {
            devMode.PaperLength = Clamp(LengthValueFromDIPToTenthOfMillimeter(pageHeightInDIPs), 1, short.MaxValue);
        }

        private static void SetPageWidth(DevMode devMode, double pageWidthInDIPs)
        {
            devMode.PaperWidth = Clamp(LengthValueFromDIPToTenthOfMillimeter(pageWidthInDIPs), 1, short.MaxValue);
        }

        private static readonly IDictionary<short, PageMediaSizeName> dmPaperSizeToPageMediaSize = new Dictionary<short, PageMediaSizeName>(101) {
            { DevModePaperSizes.DMPAPER_10X11, PageMediaSizeName.NorthAmerica10x11 },
            { DevModePaperSizes.DMPAPER_10X14, PageMediaSizeName.NorthAmerica10x14 },
            { DevModePaperSizes.DMPAPER_11X17, PageMediaSizeName.NorthAmerica11x17 },
            { DevModePaperSizes.DMPAPER_9X11, PageMediaSizeName.NorthAmerica9x11 },

            { DevModePaperSizes.DMPAPER_A_PLUS, PageMediaSizeName.NorthAmericaSuperA },
            { DevModePaperSizes.DMPAPER_A2, PageMediaSizeName.ISOA2 },
            { DevModePaperSizes.DMPAPER_A3, PageMediaSizeName.ISOA3 },
            { DevModePaperSizes.DMPAPER_A3_EXTRA, PageMediaSizeName.ISOA3Extra },
            { DevModePaperSizes.DMPAPER_A3_ROTATED, PageMediaSizeName.ISOA3Rotated },

            { DevModePaperSizes.DMPAPER_A4, PageMediaSizeName.ISOA4 },
            { DevModePaperSizes.DMPAPER_A4_EXTRA, PageMediaSizeName.ISOA4Extra },
            { DevModePaperSizes.DMPAPER_A4_PLUS, PageMediaSizeName.OtherMetricA4Plus },
            { DevModePaperSizes.DMPAPER_A4_ROTATED, PageMediaSizeName.ISOA4Rotated },

            { DevModePaperSizes.DMPAPER_A5,PageMediaSizeName.ISOA5 },
            { DevModePaperSizes.DMPAPER_A5_EXTRA, PageMediaSizeName.ISOA5Extra },
            { DevModePaperSizes.DMPAPER_A5_ROTATED,PageMediaSizeName.ISOA5Rotated },

            { DevModePaperSizes.DMPAPER_A6,PageMediaSizeName.ISOA6 },
            { DevModePaperSizes.DMPAPER_A6_ROTATED,PageMediaSizeName.ISOA6Rotated },

            { DevModePaperSizes.DMPAPER_B_PLUS,PageMediaSizeName.NorthAmericaSuperB },
            { DevModePaperSizes.DMPAPER_B4,PageMediaSizeName.JISB4 },
            { DevModePaperSizes.DMPAPER_B4_JIS_ROTATED, PageMediaSizeName.JISB4Rotated },
            { DevModePaperSizes.DMPAPER_B5,PageMediaSizeName.JISB5 },
            { DevModePaperSizes.DMPAPER_B5_JIS_ROTATED,PageMediaSizeName.JISB5Rotated },
            { DevModePaperSizes.DMPAPER_B6_JIS,PageMediaSizeName.JISB6 },
            { DevModePaperSizes.DMPAPER_B6_JIS_ROTATED, PageMediaSizeName.JISB6Rotated },

            { DevModePaperSizes.DMPAPER_CSHEET,PageMediaSizeName.NorthAmericaCSheet },

            { DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD,PageMediaSizeName.JapanDoubleHagakiPostcard },
            { DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED,PageMediaSizeName.JapanDoubleHagakiPostcardRotated },
            { DevModePaperSizes.DMPAPER_DSHEET,PageMediaSizeName.NorthAmericaDSheet },

            { DevModePaperSizes.DMPAPER_ENV_10,PageMediaSizeName.NorthAmericaNumber10Envelope },
            { DevModePaperSizes.DMPAPER_ENV_11,PageMediaSizeName.NorthAmericaNumber11Envelope },
            { DevModePaperSizes.DMPAPER_ENV_12,PageMediaSizeName.NorthAmericaNumber12Envelope },
            { DevModePaperSizes.DMPAPER_ENV_14,PageMediaSizeName.NorthAmericaNumber14Envelope },
            { DevModePaperSizes.DMPAPER_ENV_9,PageMediaSizeName.NorthAmericaNumber9Envelope },
            { DevModePaperSizes.DMPAPER_ENV_B4,PageMediaSizeName.ISOB4Envelope },
            { DevModePaperSizes.DMPAPER_ENV_B5,PageMediaSizeName.ISOB5Envelope },
            { DevModePaperSizes.DMPAPER_ENV_C3,PageMediaSizeName.ISOC3Envelope },
            { DevModePaperSizes.DMPAPER_ENV_C4,PageMediaSizeName.ISOC4Envelope },
            { DevModePaperSizes.DMPAPER_ENV_C5,PageMediaSizeName.ISOC5Envelope },
            { DevModePaperSizes.DMPAPER_ENV_C6,PageMediaSizeName.ISOC6Envelope },
            { DevModePaperSizes.DMPAPER_ENV_C65,PageMediaSizeName.ISOC6C5Envelope },
            { DevModePaperSizes.DMPAPER_ENV_DL, PageMediaSizeName.ISODLEnvelope },
            { DevModePaperSizes.DMPAPER_ENV_ITALY,PageMediaSizeName.OtherMetricItalianEnvelope },
            { DevModePaperSizes.DMPAPER_ENV_MONARCH,PageMediaSizeName.NorthAmericaMonarchEnvelope },
            { DevModePaperSizes.DMPAPER_ENV_PERSONAL,PageMediaSizeName.NorthAmericaPersonalEnvelope },
            { DevModePaperSizes.DMPAPER_ESHEET,PageMediaSizeName.NorthAmericaESheet },
            { DevModePaperSizes.DMPAPER_EXECUTIVE,PageMediaSizeName.NorthAmericaExecutive },

            { DevModePaperSizes.DMPAPER_FANFOLD_LGL_GERMAN,PageMediaSizeName.NorthAmericaGermanLegalFanfold },
            { DevModePaperSizes.DMPAPER_FANFOLD_STD_GERMAN,PageMediaSizeName.NorthAmericaGermanStandardFanfold },
            { DevModePaperSizes.DMPAPER_FOLIO,PageMediaSizeName.OtherMetricFolio },

            { DevModePaperSizes.DMPAPER_JAPANESE_POSTCARD,PageMediaSizeName.JapanHagakiPostcard },
            { DevModePaperSizes.DMPAPER_JAPANESE_POSTCARD_ROTATED,PageMediaSizeName.JapanHagakiPostcardRotated },
            { DevModePaperSizes.DMPAPER_JENV_CHOU3,PageMediaSizeName.JapanChou3Envelope },
            { DevModePaperSizes.DMPAPER_JENV_CHOU3_ROTATED,PageMediaSizeName.JapanChou3EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_JENV_CHOU4,PageMediaSizeName.JapanChou4Envelope },
            { DevModePaperSizes.DMPAPER_JENV_CHOU4_ROTATED,PageMediaSizeName.JapanChou4EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_JENV_KAKU2,PageMediaSizeName.JapanKaku2Envelope },
            { DevModePaperSizes.DMPAPER_JENV_KAKU2_ROTATED,PageMediaSizeName.JapanKaku2EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_JENV_KAKU3, PageMediaSizeName.JapanKaku3Envelope },
            { DevModePaperSizes.DMPAPER_JENV_KAKU3_ROTATED,PageMediaSizeName.JapanKaku3EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_JENV_YOU4,PageMediaSizeName.JapanYou4Envelope },
            { DevModePaperSizes.DMPAPER_JENV_YOU4_ROTATED,PageMediaSizeName.JapanYou4EnvelopeRotated },

            { DevModePaperSizes.DMPAPER_LEGAL,PageMediaSizeName.NorthAmericaLegal },
            { DevModePaperSizes.DMPAPER_LEGAL_EXTRA,PageMediaSizeName.NorthAmericaLegalExtra },
            { DevModePaperSizes.DMPAPER_LETTER,PageMediaSizeName.NorthAmericaLetter },
            { DevModePaperSizes.DMPAPER_LETTER_EXTRA,PageMediaSizeName.NorthAmericaLetterExtra },
            { DevModePaperSizes.DMPAPER_LETTER_ROTATED,PageMediaSizeName.NorthAmericaLetterRotated },

            { DevModePaperSizes.DMPAPER_NOTE,PageMediaSizeName.NorthAmericaNote },

            { DevModePaperSizes.DMPAPER_P16K,PageMediaSizeName.PRC16K },
            { DevModePaperSizes.DMPAPER_P16K_ROTATED,PageMediaSizeName.PRC16KRotated },
            { DevModePaperSizes.DMPAPER_P32K,PageMediaSizeName.PRC32K },
            { DevModePaperSizes.DMPAPER_P32K_ROTATED,PageMediaSizeName.PRC32KRotated },
            { DevModePaperSizes.DMPAPER_P32KBIG,PageMediaSizeName.PRC32KBig },
            { DevModePaperSizes.DMPAPER_PENV_1,PageMediaSizeName.PRC1Envelope },
            { DevModePaperSizes.DMPAPER_PENV_1_ROTATED,PageMediaSizeName.PRC1EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_10,PageMediaSizeName.PRC10Envelope },
            { DevModePaperSizes.DMPAPER_PENV_10_ROTATED,PageMediaSizeName.PRC10EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_2,PageMediaSizeName.PRC2Envelope },
            { DevModePaperSizes.DMPAPER_PENV_2_ROTATED,PageMediaSizeName.PRC2EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_3,PageMediaSizeName.PRC3Envelope },
            { DevModePaperSizes.DMPAPER_PENV_3_ROTATED,PageMediaSizeName.PRC3EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_4,PageMediaSizeName.PRC4Envelope },
            { DevModePaperSizes.DMPAPER_PENV_4_ROTATED,PageMediaSizeName.PRC4EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_5,PageMediaSizeName.PRC5Envelope },
            { DevModePaperSizes.DMPAPER_PENV_5_ROTATED,PageMediaSizeName.PRC5EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_6,PageMediaSizeName.PRC6Envelope },
            { DevModePaperSizes.DMPAPER_PENV_6_ROTATED,PageMediaSizeName.PRC6EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_7,PageMediaSizeName.PRC7Envelope },
            { DevModePaperSizes.DMPAPER_PENV_7_ROTATED,PageMediaSizeName.PRC7EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_8,PageMediaSizeName.PRC8Envelope },
            { DevModePaperSizes.DMPAPER_PENV_8_ROTATED,PageMediaSizeName.PRC8EnvelopeRotated },
            { DevModePaperSizes.DMPAPER_PENV_9,PageMediaSizeName.PRC9Envelope },
            { DevModePaperSizes.DMPAPER_PENV_9_ROTATED,PageMediaSizeName.PRC9EnvelopeRotated },

            { DevModePaperSizes.DMPAPER_QUARTO,PageMediaSizeName.NorthAmericaQuarto },

            { DevModePaperSizes.DMPAPER_STATEMENT,PageMediaSizeName.NorthAmericaStatement },
            { DevModePaperSizes.DMPAPER_TABLOID,PageMediaSizeName.NorthAmericaTabloid },
            { DevModePaperSizes.DMPAPER_TABLOID_EXTRA, PageMediaSizeName.NorthAmericaTabloidExtra }
        };

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.9 - Page Media Type

        private static void SetPageMediaType(InternalPrintTicket ticket, uint devModeMediaTypeCode)
        {
            TrySet(dmMediaTypeToMediaType, devModeMediaTypeCode, (pageMediaType) => ticket.PageMediaType.Value = pageMediaType);
        }

        private static void SetMediaType(DevMode devMode, PageMediaType pageMediaType)
        {
            TrySet(dmMediaTypeToMediaType, pageMediaType, (devModeMediaTypeCode) => devMode.MediaType = devModeMediaTypeCode);
        }

        private static readonly IDictionary<uint, PageMediaType> dmMediaTypeToMediaType = new Dictionary<uint, PageMediaType>(3) {
            {DevModeMediaTypes.DMMEDIA_STANDARD, PageMediaType.Plain},            
            {DevModeMediaTypes.DMMEDIA_TRANSPARENCY, PageMediaType.Transparency},            
            {DevModeMediaTypes.DMMEDIA_GLOSSY, PageMediaType.PhotographicGlossy},            
        };

        private static IDictionary<uint, MediaTypeOption> mediaTypeOptions = new Dictionary<uint, MediaTypeOption>() {
            {DevModeMediaTypes.DMMEDIA_STANDARD, new MediaTypeOption("Plain", COMPSTUISR.IDS_CPSUI_STANDARD, "None", "None", "Paper")},
            {DevModeMediaTypes.DMMEDIA_TRANSPARENCY, new MediaTypeOption("Transparency", COMPSTUISR.IDS_CPSUI_GLOSSY, "None", "None", "Transparency")},
            {DevModeMediaTypes.DMMEDIA_GLOSSY, new MediaTypeOption("PhotographicGlossy", COMPSTUISR.IDS_CPSUI_TRANSPARENCY, "None", "Glossy", "Paper")}
        };

        private struct MediaTypeOption
        {
            public MediaTypeOption(string name, uint displayNameId, string pskFrontCoating, string pskBackCoating, string pskMaterial)
            {
                this.Name = name;
                this.DisplayNameId = displayNameId;
                this.pskFrontCoating = pskFrontCoating;
                this.pskBackCoating = pskBackCoating;
                this.pskMaterial = pskMaterial;
            }

            public readonly string Name;
            public readonly uint DisplayNameId;
            public readonly string pskFrontCoating;
            public readonly string pskBackCoating;
            public readonly string pskMaterial;
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.10 - Page Orientation

        private static void SetOrientation(InternalPrintTicket ticket, DevModeOrientation devModeOrientation)
        {
            TrySet(dmOrientationToOrientation, devModeOrientation, (pageOrientation) => ticket.PageOrientation.Value = pageOrientation);
        }

        private static void SetOrientation(DevMode devMode, PageOrientation pageOrientation)
        {
            if (!TrySet(dmOrientationToOrientation, pageOrientation, (orientation) => devMode.Orientation = orientation))
            {
                if (pageOrientation == PageOrientation.ReverseLandscape)
                {
                    devMode.Orientation = DevModeOrientation.DMORIENT_LANDSCAPE;
                }
            }
        }

        private static readonly IDictionary<DevModeOrientation, PageOrientation> dmOrientationToOrientation = new Dictionary<DevModeOrientation, PageOrientation>(2) {
            {DevModeOrientation.DMORIENT_PORTRAIT, PageOrientation.Portrait},            
            {DevModeOrientation.DMORIENT_LANDSCAPE, PageOrientation.Landscape}       
        };

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.11 - Page Output Quality

        private static void SetPageResolution(InternalPrintTicket ticket, DevModeFields devModeFields, short devModeXResolution, short devModeYResolution)
        {
            if (!TrySet(dmResToQResolution, devModeXResolution, (qResolution) => ticket.PageResolution.QualitativeResolution = qResolution))
            {
                if (DevModeResolutions.IsCustom(devModeXResolution))
                {
                    ticket.PageResolution.ResolutionX = devModeXResolution;
                    ticket.PageResolution.ResolutionY = devModeXResolution;
                }

                if ((devModeFields & DevModeFields.DM_YRESOLUTION) != 0 && devModeYResolution > 0)
                {
                    ticket.PageResolution.ResolutionY = devModeYResolution;
                }
            }
        }

        private static void SetPrintQuality(DevMode devMode, PageQualitativeResolution qualitativeResolution, int dpiX, int dpiY, DevModeFields supportedFields)
        {
            if (dpiX > 0)
            {
                short devModXResolution = Clamp(dpiX, 1, short.MaxValue);
                if (IsSet(DevModeFields.DM_PRINTQUALITY, supportedFields))
                {
                    devMode.PrintQuality = devModXResolution;
                }

                if (IsSet(DevModeFields.DM_YRESOLUTION, supportedFields))
                {
                    if (dpiY > 0)
                    {
                        short devModYResolution = Clamp(dpiY, 1, short.MaxValue);
                        devMode.YResolution = devModYResolution;
                    }
                    else
                    {
                        devMode.YResolution = devModXResolution;
                    }
                }
            }
            else if (dpiY > 0)
            {
                short devModYResolution = Clamp(dpiY, 1, short.MaxValue);

                if (IsSet(DevModeFields.DM_PRINTQUALITY, supportedFields))
                {
                    devMode.PrintQuality = devModYResolution;
                }
                if (IsSet(DevModeFields.DM_YRESOLUTION, supportedFields))
                {
                    devMode.YResolution = devModYResolution;
                }
            }
            else
            {
                TrySet(dmResToQResolution, qualitativeResolution, (printQualityCode) => devMode.PrintQuality = printQualityCode);
            }
        }

        private static readonly IDictionary<short, PageQualitativeResolution> dmResToQResolution = new Dictionary<short, PageQualitativeResolution>(4) {
            {DevModeResolutions.DMRES_HIGH, PageQualitativeResolution.High},            
            {DevModeResolutions.DMRES_MEDIUM, PageQualitativeResolution.Normal},            
            {DevModeResolutions.DMRES_LOW, PageQualitativeResolution.Default},            
            {DevModeResolutions.DMRES_DRAFT, PageQualitativeResolution.Draft},            
        };

        private static IDictionary<short, OutputQualityOption> outputQualityOptions = new Dictionary<short, OutputQualityOption>() {
            {DevModeResolutions.DMRES_HIGH, new OutputQualityOption("High", COMPSTUISR.IDS_CPSUI_QUALITY_BEST)},
            {DevModeResolutions.DMRES_MEDIUM, new OutputQualityOption("Normal", COMPSTUISR.IDS_CPSUI_QUALITY_BETTER)},
            {DevModeResolutions.DMRES_DRAFT, new OutputQualityOption("Draft", COMPSTUISR.IDS_CPSUI_QUALITY_DRAFT)}
        };

        private struct OutputQualityOption
        {
            public OutputQualityOption(string name, uint displayNameId)
            {
                this.Name = name;
                this.DisplayNameId = displayNameId;
            }

            public readonly string Name;
            public readonly uint DisplayNameId;
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.12 - Page Scaling

        private static void SetScale(InternalPrintTicket ticket, short devModeScale)
        {
            if (devModeScale == 100)
            {
                ticket.PageScaling.ClearSetting();
                ticket.PageScaling[PrintSchemaTags.Framework.OptionNameProperty] = (int)PageScaling.None;
            }
            else
            {
                ticket.PageScaling.SetCustomSquareScaling(devModeScale);
            }
        }

        private static void SetScale(DevMode devMode, int scale)
        {
            devMode.Scale = Clamp(scale, short.MinValue, short.MaxValue);
        }

        #endregion

        #region Print Schema Reference Guide v1.0 Appendix E.3.13 Page True Type Font Mode

        private static void SetTrueTypeFontOption(InternalPrintTicket ticket, DevModeTrueTypeOption devModeTrueTypeOption)
        {
            switch (devModeTrueTypeOption)
            {
                case DevModeTrueTypeOption.DMTT_SUBDEV:
                {
                    ticket.PageTrueTypeFontMode.Value = TrueTypeFontMode.Automatic;
                    ticket.PageDeviceFontSubstitution.Value = DeviceFontSubstitution.On;
                    break;
                }

                case DevModeTrueTypeOption.DMTT_BITMAP:
                {
                    ticket.PageTrueTypeFontMode.Value = TrueTypeFontMode.RenderAsBitmap;
                    ticket.PageDeviceFontSubstitution.Value = DeviceFontSubstitution.Off;
                    break;
                }

                case DevModeTrueTypeOption.DMTT_DOWNLOAD:
                {
                    ticket.PageTrueTypeFontMode.Value = TrueTypeFontMode.DownloadAsNativeTrueTypeFont;
                    ticket.PageDeviceFontSubstitution.Value = DeviceFontSubstitution.Off;
                    break;
                }

                case DevModeTrueTypeOption.DMTT_DOWNLOAD_OUTLINE:
                {
                    ticket.PageTrueTypeFontMode.Value = TrueTypeFontMode.DownloadAsOutlineFont;
                    ticket.PageDeviceFontSubstitution.Value = DeviceFontSubstitution.Off;
                    break;
                }
            }
        }

        private static void SetTTOption(DevMode devMode, DeviceFontSubstitution fontSubstitution, TrueTypeFontMode trueTypeFontMode)
        {
            if (fontSubstitution == DeviceFontSubstitution.On 
                || trueTypeFontMode == TrueTypeFontMode.Automatic)
            {
                devMode.TTOption = DevModeTrueTypeOption.DMTT_SUBDEV;
            }
            else
            {
                switch (trueTypeFontMode)
                {
                    case TrueTypeFontMode.DownloadAsNativeTrueTypeFont:
                    {
                        devMode.TTOption = DevModeTrueTypeOption.DMTT_DOWNLOAD;
                        break;
                    }
                    case TrueTypeFontMode.DownloadAsOutlineFont:
                    {
                        devMode.TTOption = DevModeTrueTypeOption.DMTT_DOWNLOAD_OUTLINE;
                        break;
                    }
                    case TrueTypeFontMode.RenderAsBitmap:
                    {
                        devMode.TTOption = DevModeTrueTypeOption.DMTT_BITMAP;
                        break;
                    }
                }
            }
        }

        #endregion

        private static bool TrySet<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> setter)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                setter(value);
                return true;
            }

            return false;
        }

        private static bool TrySet<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TValue value, Action<TKey> setter)
        {
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (object.Equals(pair.Value, value))
                {
                    setter(pair.Key);
                    return true;
                }
            }

            return false;
        }

        private static bool IsSet(DevModeFields mask, DevModeFields fields)
        {
            return (mask & fields) == mask;
        }

        private static short Clamp(int value, short min, short max)
        {
            return (short)Math.Max(min, Math.Min(value, max));
        }

        private static double LengthValueFromTenthOfMillimeterToDIP(int tenthOfMillimeterValue)
        {
            return ((double)tenthOfMillimeterValue / 2540.0) * (double)WpfPixelsPerInch;
        }

        private static int LengthValueFromDIPToTenthOfMillimeter(double dipValue)
        {
            return (int)(((dipValue / (double)WpfPixelsPerInch) * 2540) + 0.5);
        }
    }
}
