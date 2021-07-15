// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of public PrintCapabilities class.



--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Represents a device's printing capabilities.
    /// </summary>
    internal class InternalPrintCapabilities
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the InternalPrintCapabilities class with capabilities based on the XML form of PrintCapabilities.
        /// </summary>
        /// <param name="xmlStream">Stream object containing the XML form of PrintCapabilities.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="xmlStream"/> parameter is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// The stream object specified by <paramref name="xmlStream"/> parameter doesn't contain a well-formed XML PrintCapabilities.
        /// The exception object's <see cref="Exception.Message"/> property describes why the XML is not well-formed. And if not
        /// null, the exception object's <see cref="Exception.InnerException"/> property provides more details.
        /// </exception>
        public InternalPrintCapabilities(Stream xmlStream)
        {
            // Verify input parameter
            if (xmlStream == null)
            {
                throw new ArgumentNullException("xmlStream");
            }

            #if _DEBUG
            // Direct Trace output to console
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            #endif

            // Calculate the read-only counter values (this rely on the internal rules listed at
            // PrintSchema.Features type)
            _countRootFeatures = Enum.GetNames(typeof(CapabilityName)).Length;
            _countLocalParamDefs = Enum.GetNames(typeof(PrintSchemaLocalParameterDefs)).Length;

            try
            {
                // Construct a builder instance
                _builder = new PrintCapBuilder(xmlStream);

                // Construct the feature and local-parameter arrays.
                // (all elements should be null at this stage)
                _pcRootFeatures = new object[_countRootFeatures];
                _pcLocalParamDefs = new ParameterDefinition[_countLocalParamDefs];
                _baLocalParamRequired = new bool[_countLocalParamDefs];
                for (int i=0; i<_countLocalParamDefs; i++)
                {
                    _baLocalParamRequired[i] = false;
                }

                // Ask the builder to populate the PrintCapabilities object states
                _builder.Build(this);

                // Populate aggregated states based on builder's result
                PostBuildProcessing();
            }
            catch (XmlException e)
            {
                // Translate XMLException into FormatException so client only needs
                // to catch FormatException for non-well-formed XML PrintCapabilities (the non-well-formness
                // could be either non-well-formed raw XML or non-well-formed PrintCapabilities content).
                throw NewPrintCapFormatException(e.Message, e);
            }

            // Notice we are not catching FormatException here, so it will surface to the client.
            // (Only the PrintCapBuilder constructor should throw FormatException for invalid PrintCapabilities
            // root element)
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets a Boolean value indicating whether the device supports a specific capability.
        /// </summary>
        /// <param name="feature">Capability feature to check for device support.</param>
        /// <returns>True if the capability is supported. False otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="feature"/> parameter is not a standard feature defined in <see cref="CapabilityName"/>.
        /// </exception>
        public bool SupportsCapability(CapabilityName feature)
        {
            // Verify input parameter
            if (feature < PrintSchema.CapabilityNameEnumMin ||
                feature > PrintSchema.CapabilityNameEnumMax)
            {
                throw new ArgumentOutOfRangeException("feature");
            }

            return (_pcRootFeatures[(int)feature] != null);
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Gets a <see cref="DocumentCollateCapability"/> object that specifies the device's document collate capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public DocumentCollateCapability DocumentCollateCapability
        {
            get
            {
                return (DocumentCollateCapability)_pcRootFeatures[(int)CapabilityName.DocumentCollate];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobDuplexCapability"/> object that specifies the device's job duplex capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobDuplexCapability JobDuplexCapability
        {
            get
            {
                return (JobDuplexCapability)_pcRootFeatures[(int)CapabilityName.JobDuplex];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobNUpCapability"/> object that specifies the device's job NUp capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobNUpCapability JobNUpCapability
        {
            get
            {
                return (JobNUpCapability)_pcRootFeatures[(int)CapabilityName.JobNUp];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobStapleCapability"/> object that specifies the device's job output stapling capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobStapleCapability JobStapleCapability
        {
            get
            {
                return (JobStapleCapability)_pcRootFeatures[(int)CapabilityName.JobStaple];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageDeviceFontSubstitutionCapability"/> object that specifies the device's page device font substitution capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageDeviceFontSubstitutionCapability PageDeviceFontSubstitutionCapability
        {
            get
            {
                return (PageDeviceFontSubstitutionCapability)_pcRootFeatures[(int)CapabilityName.PageDeviceFontSubstitution];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageMediaSizeCapability"/> object that specifies the device's page media size capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageMediaSizeCapability PageMediaSizeCapability
        {
            get
            {
                return (PageMediaSizeCapability)_pcRootFeatures[(int)CapabilityName.PageMediaSize];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageMediaTypeCapability"/> object that specifies the device's page media type capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageMediaTypeCapability PageMediaTypeCapability
        {
            get
            {
                return (PageMediaTypeCapability)_pcRootFeatures[(int)CapabilityName.PageMediaType];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOrientationCapability"/> object that specifies the device's page orientation capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageOrientationCapability PageOrientationCapability
        {
            get
            {
                return (PageOrientationCapability)_pcRootFeatures[(int)CapabilityName.PageOrientation];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOutputColorCapability"/> object that specifies the device's page output color capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageOutputColorCapability PageOutputColorCapability
        {
            get
            {
                return (PageOutputColorCapability)_pcRootFeatures[(int)CapabilityName.PageOutputColor];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageResolutionCapability"/> object that specifies the device's page resolution capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageResolutionCapability PageResolutionCapability
        {
            get
            {
                return (PageResolutionCapability)_pcRootFeatures[(int)CapabilityName.PageResolution];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageScalingCapability"/> object that specifies the device's page scaling capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageScalingCapability PageScalingCapability
        {
            get
            {
                return (PageScalingCapability)_pcRootFeatures[(int)CapabilityName.PageScaling];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageTrueTypeFontModeCapability"/> object that specifies the device's page TrueType font handling mode capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageTrueTypeFontModeCapability PageTrueTypeFontModeCapability
        {
            get
            {
                return (PageTrueTypeFontModeCapability)_pcRootFeatures[(int)CapabilityName.PageTrueTypeFontMode];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobPageOrderCapability"/> object that specifies the device's job page ordering capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobPageOrderCapability JobPageOrderCapability
        {
            get
            {
                return (JobPageOrderCapability)_pcRootFeatures[(int)CapabilityName.JobPageOrder];
            }
        }

        /// <summary>
        /// Gets a <see cref="PagePhotoPrintingIntentCapability"/> object that specifies the device's page photo printing intent capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PagePhotoPrintingIntentCapability PagePhotoPrintingIntentCapability
        {
            get
            {
                return (PagePhotoPrintingIntentCapability)_pcRootFeatures[(int)CapabilityName.PagePhotoPrintingIntent];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageBorderlessCapability"/> object that specifies the device's page borderless capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageBorderlessCapability PageBorderlessCapability
        {
            get
            {
                return (PageBorderlessCapability)_pcRootFeatures[(int)CapabilityName.PageBorderless];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageOutputQualityCapability"/> object that specifies the device's page output quality capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageOutputQualityCapability PageOutputQualityCapability
        {
            get
            {
                return (PageOutputQualityCapability)_pcRootFeatures[(int)CapabilityName.PageOutputQuality];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobInputBinCapability"/> object that specifies the device's job input bins capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobInputBinCapability JobInputBinCapability
        {
            get
            {
                return (JobInputBinCapability)_pcRootFeatures[(int)CapabilityName.JobInputBin];
            }
        }

        /// <summary>
        /// Gets a <see cref="DocumentInputBinCapability"/> object that specifies the device's document input bins capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public DocumentInputBinCapability DocumentInputBinCapability
        {
            get
            {
                return (DocumentInputBinCapability)_pcRootFeatures[(int)CapabilityName.DocumentInputBin];
            }
        }

        /// <summary>
        /// Gets a <see cref="PageInputBinCapability"/> object that specifies the device's page input bins capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public PageInputBinCapability PageInputBinCapability
        {
            get
            {
                return (PageInputBinCapability)_pcRootFeatures[(int)CapabilityName.PageInputBin];
            }
        }

        /// <summary>
        /// Gets a <see cref="JobCopyCountCapability"/> object that specifies the device's job copy count capability.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public JobCopyCountCapability JobCopyCountCapability
        {
            get
            {
                return (JobCopyCountCapability)_pcRootFeatures[(int)CapabilityName.JobCopyCount];
            }
        }

        /// <summary>
        /// Gets a <see cref="ImageableSizeCapability"/> object that describes the imaged canvas for layout and rendering.
        /// </summary>
        /// <remarks>
        /// If the device doesn't support this capability, null reference will be returned.
        /// You should use <see cref="SupportsCapability"/> to check the capability is supported before accessing the returned reference.
        /// </remarks>
        public ImageableSizeCapability PageImageableSizeCapability
        {
            get
            {
                return (ImageableSizeCapability)_pcRootFeatures[(int)CapabilityName.PageImageableSize];
            }
        }

        #endregion Public Properties

        #region Internal Methods

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintCapabilities XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        internal static FormatException NewPrintCapFormatException(string detailMsg)
        {
            return NewPrintCapFormatException(detailMsg, null);
        }

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintCapabilities XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <param name="innerException">the exception that causes the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        internal static FormatException NewPrintCapFormatException(string detailMsg, Exception innerException)
        {
            return new FormatException(String.Format(CultureInfo.CurrentCulture,
                                                     "{0} {1} {2}",
                                                     PrintSchemaTags.Framework.PrintCapRoot,
                                                     PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                                                     detailMsg),
                                       innerException);
        }

        internal void SetLocalParameterDefAsRequired(int paramDefIndex, bool isRequired)
        {
            _baLocalParamRequired[paramDefIndex] = isRequired;
        }

        #endregion internal Methods

        #region Internal Fields

        // array of Print Capabilities features (feature or global-parameter-def or root-level property)
        internal object[]              _pcRootFeatures;

        // array of Print Capabilities local parameter definitions
        internal ParameterDefinition[] _pcLocalParamDefs;

        #endregion Internal Fields

        #region Private Methods

        /// <summary>
        /// Post-process states populated by the builder and populate aggregates states
        /// </summary>
        /// <exception cref="FormatException">thrown if XML PrintCapabilities is not well-formed</exception>
        private void PostBuildProcessing()
        {
            for (int i=0; i<_countLocalParamDefs; i++)
            {
                if (_baLocalParamRequired[i])
                {
                    // If a parameter definition has be referenced, then the parameter definition must be present in the XML.
                    if (_pcLocalParamDefs[i] == null)
                    {
                        throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                                       PTUtility.GetTextFromResource("FormatException.ParameterDefMissOrInvalid"),
                                                                       PrintSchemaTags.Framework.ParameterDef,
                                                                       (PrintSchemaLocalParameterDefs)i));
                    }
                }
            }
        }

        #endregion Private Methods

        #region Private Fields

        // number of root features
        private readonly int _countRootFeatures;

        // number of local parameter definitions
        private readonly int _countLocalParamDefs;

        // array of Boolean values to indicate whether a local parameter-def is required or not
        private bool[] _baLocalParamRequired;

        private PrintCapBuilder _builder;

        #endregion Private Fields
    }
}