// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of internal PrintCapBuilder class.


--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// PrintCapabilities object builder class
    /// </summary>
    internal class PrintCapBuilder
    {
        #region Constructors

        /// <summary>
        /// Constructs a new builder for the given XML Print Capabilities
        /// </summary>
        /// <param name="xmlStream">readable stream containing the Print Capabilities XML</param>
        /// <exception cref="FormatException">thrown by XmlPrintCapReader if XML Print Capabilities is not well-formed</exception>
        public PrintCapBuilder(Stream xmlStream)
        {
            // Instantiates the reader
            _reader = new XmlPrintCapReader(xmlStream);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Invokes the builder to populate the PrintCapabilities object state
        /// </summary>
        /// <param name="printCap">the PrintCapabilities object to populate</param>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public void Build(InternalPrintCapabilities printCap)
        {
            #if _DEBUG
            Trace.WriteLine("-Trace- Building PrintCapabilities ... " +
                            DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            #endif

            // Loop over root-level schema elements
            while (_reader.MoveToNextSchemaElement(_kRootElementDepth,
                                                   PrintSchemaNodeTypes.RootLevelTypes))
            {
                // PrintCapabilities root-level schema element can't be empty element, so we skip
                // any empty element here.
                if (_reader.CurrentElementIsEmpty)
                {
                    #if _DEBUG
                    Trace.WriteLine("-Warning- skip empty root " + _reader.CurrentElementNodeType
                                    + " " + _reader.CurrentElementNameAttrValue);
                    #endif

                    continue;
                }

                // Build the specific feature or parameter-def or root-level property
                switch (_reader.CurrentElementNodeType)
                {
                    case PrintSchemaNodeTypes.Feature:
                        BuildFeature(printCap, null);
                        break;

                    case PrintSchemaNodeTypes.ParameterDef:
                        BuildParameterDef(printCap);
                        break;

                    case PrintSchemaNodeTypes.Property:
                        BuildRootProperty(printCap);
                        break;

                    default:
                        #if _DEBUG
                        Trace.WriteLine("-Warning- Skip unsupported root element type " +
                                        _reader.CurrentElementNodeType);
                        #endif

                        break;
                }
            }

            #if _DEBUG
            Trace.WriteLine("-Trace- ... PrintCapabilities built " +
                            DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            #endif
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Builds a single feature instance and populates its state
        /// </summary>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        private void BuildFeature(InternalPrintCapabilities printCap, PrintCapabilityFeature parentFeature)
        {
            int featureIndex = LookupFeatureIndex(_reader.CurrentElementNameAttrValue,
                                                  (parentFeature != null));

            // Skip if the feature is unknown to us
            if (featureIndex < 0)
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- Skip unknown feature '" +
                                _reader.CurrentElementNameAttrValue + "'");
                #endif

                return;
            }

            // So it's a known standard feature we want to handle

            NewFeatureHandler newFeatureCallback;

            // Get all the callback functions of this feature.

            // None of the callback functions should throw FormatException. Throwing XmlException
            // (from XmlTextReader) is OK.

            LookupFeatureCallbacks(_reader.CurrentElementNameAttrValue,
                                   (parentFeature != null),
                                   out newFeatureCallback);

            // New-feature callback returns a new, empty feature object derived from PrintCapabilityFeature
            PrintCapabilityFeature newFeature = newFeatureCallback(printCap);

            // Reader can handle generic feature element XML attributes
            // _reader.FeatureAttributeGenericHandler(newFeature);

            // We assume there is no feature level non-generic XML attribute that
            // needs us calling into feature-specific callback.

            int optionDepth = _reader.CurrentElementDepth + 1;

            PrintSchemaNodeTypes typeFilterFlags;

            if (newFeature.HasSubFeature)
            {
                typeFilterFlags = PrintSchemaNodeTypes.FeatureLevelTypesWithSubFeature;
            }
            else
            {
                typeFilterFlags = PrintSchemaNodeTypes.FeatureLevelTypesWithoutSubFeature;
            }

            // This "while" loops over immediate children of the feature element
            while (_reader.MoveToNextSchemaElement(optionDepth, typeFilterFlags))
            {
                if (_reader.CurrentElementNodeType == PrintSchemaNodeTypes.Property)
                {
                    // Process feature property
                    bool handled = false;

                    // call feature-specific callback
                    handled = newFeature.FeaturePropCallback(newFeature, _reader);

                    if (!handled)
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Warning- Skip feature's unknown " + _reader.CurrentElementNodeType +
                                        " '" + _reader.CurrentElementNameAttrValue + "'" +
                                        " at line " + _reader._xmlReader.LineNumber +
                                        ", position " + _reader._xmlReader.LinePosition);
                        #endif
                    }
                }
                else if (_reader.CurrentElementNodeType == PrintSchemaNodeTypes.Option)
                {
                    // Process feature option

                    // New-option callback returns a new, empty option object derived from PrintCapabilityOption
                    PrintCapabilityOption newOption = newFeature.NewOptionCallback(newFeature);

                    // Reader can handle generic option element XML attributes
                    _reader.OptionAttributeGenericHandler(newOption);

                    // Specific feature may also have unique XML attributes to process
                    newFeature.OptionAttrCallback(newOption, _reader);

                    // Go one level deeper if the option is non-empty since it could have
                    // properties as sub-elements.
                    if (!_reader.CurrentElementIsEmpty)
                    {
                        int optionPropertyDepth = optionDepth + 1;

                        // This "while" loops over immediate children of the option element
                        while (_reader.MoveToNextSchemaElement(optionPropertyDepth,
                                                               PrintSchemaNodeTypes.OptionLevelTypes))
                        {
                            bool handled = false;

                            // If it's not generic property, use feature-specific callback
                            handled = newFeature.OptionPropCallback(newOption, _reader);

                            if (!handled)
                            {
                                #if _DEBUG
                                Trace.WriteLine("-Warning- Skip option's unknown " + _reader.CurrentElementNodeType +
                                                " at line " + _reader._xmlReader.LineNumber +
                                                ", position " + _reader._xmlReader.LinePosition);
                                #endif
                            }
                        }
                    }

                    // Finished reading and building this option, so add it to the option collection.
                    // The capability-specific AddOption() function will have logic to check
                    // the completeness of the newOption and based on that decide whether or not to
                    // add the option to the option collection.
                    if (!newFeature.AddOptionCallback(newOption))
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Warning- skip unknown or incomplete option (name='" +
                                        newOption._optionName + "') at line " +
                                        _reader._xmlReader.LineNumber + ", position " +
                                        _reader._xmlReader.LinePosition + ": " + newOption);
                        #endif
                    }
                }
                else if (_reader.CurrentElementNodeType == PrintSchemaNodeTypes.Feature)
                {
                    #if _DEBUG
                    Trace.Assert(newFeature.HasSubFeature,
                                 "THIS SHOULD NOT HAPPEN: BuildFeature() hits sub-feature " +
                                 _reader.CurrentElementNameAttrValue);
                    #endif

                    // Recursively builds the sub-feature
                    BuildFeature(printCap, newFeature);
                }
                else
                {
                    #if _DEBUG
                    Trace.Assert(false, "THIS SHOULD NOT HAPPEN: BuildFeature() hits " +
                                        _reader.CurrentElementNodeType + " node " +
                                        _reader.CurrentElementNameAttrValue);
                    #endif
                }
            }

            // Accept the new feature only if it has valid state
            if (newFeature.IsValid)
            {
                if (parentFeature != null)
                {
                    parentFeature.AddSubFeatureCallback(newFeature);
                }
                else
                {
                    printCap._pcRootFeatures[featureIndex] = newFeature;
                }
            }
            else
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- skip invalid or incomplete feature " + newFeature.FeatureName);
                #endif
            }
        }

        /// <summary>
        /// Builds a single ParameterDefinition and populates its state
        /// </summary>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        private void BuildParameterDef(InternalPrintCapabilities printCap)
        {
            bool isLocalParam;

            int paramIndex = LookupParameterIndex(_reader.CurrentElementNameAttrValue,
                                                  out isLocalParam);

            // Skip if the parameter-def is unknown to us
            if (paramIndex < 0)
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- Skip unknown parameter-def '" +
                                _reader.CurrentElementNameAttrValue + "'");
                #endif

                return;
            }

            // So it's a known standard parameter-def we want to handle
            NewParamDefHandler newParamDefCallback;

            // Get all the callback functions of this feature

            // None of the callback functions should throw FormatException. Throwing XmlException
            // (from XmlTextReader) is OK.

            LookupParameterCallbacks(_reader.CurrentElementNameAttrValue,
                                     isLocalParam,
                                     out newParamDefCallback);

            // New-parameter-def callback returns a new, empty parameter-def object derived from ParameterDefinition
            ParameterDefinition newParam = newParamDefCallback(printCap);
            newParam.ParameterName = _reader.CurrentElementNameAttrValue;

            int propDepth = _reader.CurrentElementDepth + 1;

            // This "while" loops over immediate property children of the parameter-def
            // (ParameterDefinition only contains Property sub-elements)
            while (_reader.MoveToNextSchemaElement(propDepth,
                                                   PrintSchemaNodeTypes.Property))
            {
                bool handled = false;

                // call parameter-specific callback
                handled = newParam.ParamDefPropCallback(newParam, _reader);
            }

            // Accept the new parameter only if it has valid state
            if (newParam.IsValid)
            {
                if (!isLocalParam)
                {
                    printCap._pcRootFeatures[paramIndex] = newParam;
                }
                else
                {
                    printCap._pcLocalParamDefs[paramIndex] = newParam;
                }
            }
            else
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- skip invalid or incomplete parameter-def " + newParam.ParameterName);
                #endif
            }
        }

        /// <summary>
        /// Builds a single root-level property and populates its state
        /// </summary>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        private void BuildRootProperty(InternalPrintCapabilities printCap)
        {
            if (_reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.Self)
            {
                ImageableSizeCapability imageableSizeCap = new ImageableSizeCapability();

                if (imageableSizeCap.BuildProperty(this._reader))
                {
                    printCap._pcRootFeatures[(int)CapabilityName.PageImageableSize] = imageableSizeCap;
                }
            }
            else
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- Skip unknown root-level property '" +
                                 _reader.CurrentElementNameAttrValue + "'");
                #endif
            }
        }

        /// <summary>
        /// Finds the array index of a standard PrintCapabilities feature
        /// </summary>
        /// <returns>non-negative index if matching feature found, otherwise -1</returns>
        /// <exception>none</exception>
        private static int LookupFeatureIndex(string featureName, bool isSubFeature)
        {
            // The mapper returns int-value of the feature's enum, which is what we use
            // as the feature array index.
            return PrintSchemaMapper.SchemaNameToEnumValueWithMap(
                                     isSubFeature ? PrintSchemaTags.Keywords.SubFeatureMapTable :
                                                    PrintSchemaTags.Keywords.FeatureMapTable,
                                     featureName);
        }

        /// <summary>
        /// Finds the array index of a standard PrintCapabilities parameter-def
        /// </summary>
        /// <returns>non-negative index if matching parameter-def found, otherwise -1</returns>
        /// <exception>none</exception>
        private static int LookupParameterIndex(string paramName, out bool isLocalParam)
        {
            isLocalParam = false;

            int index = PrintSchemaMapper.SchemaNameToEnumValueWithMap(
                                          PrintSchemaTags.Keywords.GlobalParameterMapTable,
                                          paramName);
            if (index < 0)
            {
                // We didn't find a match to a global parameter. Now try the local parameters.
                index = PrintSchemaMapper.SchemaNameToEnumValueWithMap(
                                          PrintSchemaTags.Keywords.LocalParameterMapTable,
                                          paramName);

                // If a match is found, we know it must be a local param
                if (index >= 0)
                {
                    isLocalParam = true;
                }
            }

            return index;
        }

        /// <summary>
        /// Finds the callbacks for a specific standard PrintCapabilities feature
        /// </summary>
        /// <exception>none</exception>
        private static void LookupFeatureCallbacks(string featureName,
                                                   bool   isSubFeature,
                                                   out NewFeatureHandler newFeatureCallback)
        {
            FeatureHandlersTableEntry[] handlersTable;

            if (!isSubFeature)
            {
                handlersTable = _fHandlersTable;
            }
            else
            {
                handlersTable = _subfHandlersTable;
            }

            newFeatureCallback = null;

            for (int i=0; i<handlersTable.Length; i++)
            {
                if (handlersTable[i].Name == featureName)
                {
                    newFeatureCallback =  handlersTable[i].NewFeatureCallback;
                    return;
                }
            }

            #if _DEBUG
            Trace.Assert(false, "THIS SHOULD NOT HAPPEN: LookupFeatureCallbacks() doesn't know feature " + featureName);
            #endif

            return;
        }

        /// <summary>
        /// Finds the callbacks for a specific standard PrintCapabilities parameter-def
        /// </summary>
        /// <exception>none</exception>
        private static void LookupParameterCallbacks(string paramName,
                                                     bool   isLocalParam,
                                                     out NewParamDefHandler newParamDefCallback)
        {
            ParamDefHandlersTableEntry[] handlersTable;

            if (!isLocalParam)
            {
                handlersTable = _gpHandlersTable;
            }
            else
            {
                handlersTable = _lpHandlersTable;
            }

            newParamDefCallback = null;

            for (int i=0; i<handlersTable.Length; i++)
            {
                if (handlersTable[i].Name == paramName)
                {
                    newParamDefCallback = handlersTable[i].NewParamDefCallback;
                    return;
                }
            }

            #if _DEBUG
            Trace.Assert(false, "THIS SHOULD NOT HAPPEN: LookupParameterCallbacks() doesn't know parameter " + paramName);
            #endif

            return;
        }

        #endregion Private Methods

        #region Private Types

        /// <summary>
        /// Delegate type for standard PrintCapabilities feature new-feature callback
        /// </summary>
        private delegate PrintCapabilityFeature NewFeatureHandler(InternalPrintCapabilities printCap);

        /// <summary>
        /// Struct of an entry in the feature handlers table
        /// </summary>
        private struct FeatureHandlersTableEntry
        {
            /// <summary>
            /// Constructs one feature handlers table entry
            /// </summary>
            public FeatureHandlersTableEntry(string name,
                                             NewFeatureHandler  newFeatureCallback)
            {
                this.Name = name;
                this.NewFeatureCallback = newFeatureCallback;
            }

            public string             Name;
            public NewFeatureHandler  NewFeatureCallback;
        }

        /// <summary>
        /// Delegate type for standard PrintCapabilities new parameter-def callback
        /// </summary>
        private delegate ParameterDefinition NewParamDefHandler(InternalPrintCapabilities printCap);

        /// <summary>
        /// Struct of an entry in the parameter-def handlers table
        /// </summary>
        private struct ParamDefHandlersTableEntry
        {
            /// <summary>
            /// Constructs one parameter-def handlers table entry
            /// </summary>
            public ParamDefHandlersTableEntry(string name,
                                              NewParamDefHandler  newParamDefCallback)
            {
                this.Name = name;
                this.NewParamDefCallback = newParamDefCallback;
            }

            public string Name;
            public NewParamDefHandler NewParamDefCallback;
        }

        #endregion Private Types

        #region Private Constants

        /// <summary>
        /// XML reader depth of root PrintCapabilities features/parameters
        /// </summary>
        private const int _kRootElementDepth = 1;

        #endregion Private Constants

        #region Private Fields

        // The XML PrintCapabilities reader instance
        private XmlPrintCapReader _reader;

        // Static mapping table between root feature name and its handlers
        private static FeatureHandlersTableEntry[] _fHandlersTable = {
            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.CollateKeys.DocumentCollate,
                    new NewFeatureHandler(DocumentCollateCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.DuplexKeys.JobDuplex,
                    new NewFeatureHandler(JobDuplexCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.NUpKeys.JobNUp,
                    new NewFeatureHandler(JobNUpCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.StapleKeys.JobStaple,
                    new NewFeatureHandler(JobStapleCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageDeviceFontSubstitutionKeys.Self,
                    new NewFeatureHandler(PageDeviceFontSubstitutionCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageMediaSizeKeys.Self,
                    new NewFeatureHandler(PageMediaSizeCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageMediaTypeKeys.Self,
                    new NewFeatureHandler(PageMediaTypeCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageOrientationKeys.Self,
                    new NewFeatureHandler(PageOrientationCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageOutputColorKeys.Self,
                    new NewFeatureHandler(PageOutputColorCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageResolutionKeys.Self,
                    new NewFeatureHandler(PageResolutionCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageScalingKeys.Self,
                    new NewFeatureHandler(PageScalingCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageTrueTypeFontModeKeys.Self,
                    new NewFeatureHandler(PageTrueTypeFontModeCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.JobPageOrderKeys.Self,
                    new NewFeatureHandler(JobPageOrderCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PagePhotoPrintingIntentKeys.Self,
                    new NewFeatureHandler(PagePhotoPrintingIntentCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageBorderlessKeys.Self,
                    new NewFeatureHandler(PageBorderlessCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.PageOutputQualityKeys.Self,
                    new NewFeatureHandler(PageOutputQualityCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.InputBinKeys.JobInputBin,
                    new NewFeatureHandler(JobInputBinCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.InputBinKeys.DocumentInputBin,
                    new NewFeatureHandler(DocumentInputBinCapability.NewFeatureCallback)),

            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.InputBinKeys.PageInputBin,
                    new NewFeatureHandler(PageInputBinCapability.NewFeatureCallback)),
        };

        // Static mapping table between sub-feature name and its handlers
        private static FeatureHandlersTableEntry[] _subfHandlersTable = {
            new FeatureHandlersTableEntry(PrintSchemaTags.Keywords.NUpKeys.PresentationDirection,
                    new NewFeatureHandler(NUpPresentationDirectionCapability.NewFeatureCallback)),
        };

        // Static mapping table between global parameter-def name and its handlers
        private static ParamDefHandlersTableEntry[] _gpHandlersTable = {
            new ParamDefHandlersTableEntry(PrintSchemaTags.Keywords.ParameterDefs.JobCopyCount,
                    new NewParamDefHandler(JobCopyCountCapability.NewParamDefCallback)),
        };

        // Static mapping table between local parameter-def name and its handlers
        private static ParamDefHandlersTableEntry[] _lpHandlersTable = {
            // for scaling
            new ParamDefHandlersTableEntry(PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleWidth,
                    new NewParamDefHandler(ScalingScaleWidthCapability.NewParamDefCallback)),

            new ParamDefHandlersTableEntry(PrintSchemaTags.Keywords.ParameterDefs.PageScalingScaleHeight,
                    new NewParamDefHandler(ScalingScaleHeightCapability.NewParamDefCallback)),

            new ParamDefHandlersTableEntry(PrintSchemaTags.Keywords.ParameterDefs.PageSquareScalingScale,
                    new NewParamDefHandler(ScalingSquareScaleCapability.NewParamDefCallback)),
        };

        #endregion Private Fields
    }
}