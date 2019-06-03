// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Abstract:

    Definition and implementation of this public property type.


--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;

using System.Printing;
using MS.Internal.Printing.Configuration;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Specifies the imageable area of the canvas.
    /// </summary>
    internal class CanvasImageableArea
    {
        #region Constructors

        internal CanvasImageableArea(ImageableSizeCapability ownerProperty)
        {
            this._ownerProperty = ownerProperty;
            _originWidth = _originHeight = _extentWidth = _extentHeight = PrintSchema.UnspecifiedIntValue;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Specifies the horizontal origin of the imageable area relative to the application media size, in 1/96 inch unit.
        /// </summary>
        public double OriginWidth
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_originWidth);
            }
        }

        /// <summary>
        /// Specifies the vertical origin of the imageable area relative to the application media size, in 1/96 inch unit.
        /// </summary>
        public double OriginHeight
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_originHeight);
            }
        }

        /// <summary>
        /// Specifies the horizontal distance between the origin and
        /// the bounding limit of the application media size, in 1/96 inch unit.
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_extentWidth);
            }
        }

        /// <summary>
        /// Specifies the vertical distance between the origin and
        /// the bounding limit of the application media size, in 1/96 inch unit.
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_extentHeight);
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the imageable area to human-readable string.
        /// </summary>
        /// <returns>A string that represents this imageable area.</returns>
        public override string ToString()
        {
            return "[ImageableArea: Origin (" + OriginWidth + "," + OriginHeight + "), Extent (" + ExtentWidth + "x" + ExtentHeight + ")]";
        }

        #endregion Public Methods

        #region Internal Fields

        internal ImageableSizeCapability _ownerProperty;

        // Integer values are always in micron unit
        internal int _originWidth;
        internal int _originHeight;
        internal int _extentWidth;
        internal int _extentHeight;

        #endregion Internal Fields
    }

    /// <summary>
    /// Represents imageable size capability.
    /// </summary>
    internal class ImageableSizeCapability : PrintCapabilityRootProperty
    {
        #region Constructors

        internal ImageableSizeCapability() : base()
        {
            _imageableSizeWidth = _imageableSizeHeight = PrintSchema.UnspecifiedIntValue;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Specifies the horizontal dimension of application media size relative to the PageOrientation, in 1/96 inch unit.
        /// </summary>
        public double ImageableSizeWidth
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_imageableSizeWidth);
            }
        }

        /// <summary>
        /// Specifies the vertical dimension of the application media size relative to the PageOrientation, in 1/96 inch unit.
        /// </summary>
        public double ImageableSizeHeight
        {
            get
            {
                return UnitConverter.LengthValueFromMicronToDIP(_imageableSizeHeight);
            }
        }

        /// <summary>
        /// Specifies the imageable area of the canvas.
        /// </summary>
        public CanvasImageableArea ImageableArea
        {
            get
            {
                return _imageableArea;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the imageable size capability object to human-readable string.
        /// </summary>
        /// <returns>A string that represents this imageable size capability object.</returns>
        public override string ToString()
        {
            return "ImageableSizeWidth=" + ImageableSizeWidth + ", ImageableSizeHeight=" + ImageableSizeHeight + " " +
                   ((ImageableArea != null) ? ImageableArea.ToString() : "[ImageableArea: null]");
        }

        #endregion Public Methods

        #region Internal Methods

        /// <exception cref="XmlException">XML parser finds non-well-formness of XML</exception>
        internal override sealed bool BuildProperty(XmlPrintCapReader reader)
        {
            #if _DEBUG
            Trace.Assert(reader.CurrentElementNodeType == PrintSchemaNodeTypes.Property,
                    "THIS SHOULD NOT HAPPEN: RootPropertyPropCallback gets non-Property node");
            #endif

            int subPropDepth = reader.CurrentElementDepth + 1;

            // Loops over immediate property children of the root-level property
            while (reader.MoveToNextSchemaElement(subPropDepth,
                                                  PrintSchemaNodeTypes.Property))
            {
                if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.ImageableSizeWidth)
                {
                    try
                    {
                        this._imageableSizeWidth = reader.GetCurrentPropertyIntValueWithException();
                    }
                    // We want to catch internal FormatException to skip recoverable XML content syntax error
                    #pragma warning suppress 56502
                    #if _DEBUG
                    catch (FormatException e)
                    #else
                    catch (FormatException)
                    #endif
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Error- " + e.Message);
                        #endif
                    }
                }
                else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.ImageableSizeHeight)
                {
                    try
                    {
                        this._imageableSizeHeight = reader.GetCurrentPropertyIntValueWithException();
                    }
                    // We want to catch internal FormatException to skip recoverable XML content syntax error
                    #pragma warning suppress 56502
                    #if _DEBUG
                    catch (FormatException e)
                    #else
                    catch (FormatException)
                    #endif
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Error- " + e.Message);
                        #endif
                    }
                }
                else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.ImageableArea)
                {
                    this._imageableArea = new CanvasImageableArea(this);

                    // When need to loop at a deeper depth, the code should cache the desired depth
                    // value and use the cached value in the while-loop. Using CurrentElementDepth
                    // in while-loop won't work correctly since the CurrentElementDepth value is changing.
                    int iaPropDepth = reader.CurrentElementDepth + 1;

                    // loop over one level down to read "ImageableArea" child-element properties
                    while (reader.MoveToNextSchemaElement(iaPropDepth,
                                                          PrintSchemaNodeTypes.Property))
                    {
                        if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.OriginWidth)
                        {
                            try
                            {
                                this._imageableArea._originWidth = reader.GetCurrentPropertyIntValueWithException();
                            }
                            // We want to catch internal FormatException to skip recoverable XML content syntax error
                            #pragma warning suppress 56502
                            #if _DEBUG
                            catch (FormatException e)
                            #else
                            catch (FormatException)
                            #endif
                            {
                                #if _DEBUG
                                Trace.WriteLine("-Error- " + e.Message);
                                #endif
                            }
                        }
                        else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.OriginHeight)
                        {
                            try
                            {
                                this._imageableArea._originHeight = reader.GetCurrentPropertyIntValueWithException();
                            }
                            // We want to catch internal FormatException to skip recoverable XML content syntax error
                            #pragma warning suppress 56502
                            #if _DEBUG
                            catch (FormatException e)
                            #else
                            catch (FormatException)
                            #endif
                            {
                                #if _DEBUG
                                Trace.WriteLine("-Error- " + e.Message);
                                #endif
                            }
                        }
                        else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.ExtentWidth)
                        {
                            try
                            {
                                this._imageableArea._extentWidth = reader.GetCurrentPropertyIntValueWithException();
                            }
                            // We want to catch internal FormatException to skip recoverable XML content syntax error
                            #pragma warning suppress 56502
                            #if _DEBUG
                            catch (FormatException e)
                            #else
                            catch (FormatException)
                            #endif
                            {
                                #if _DEBUG
                                Trace.WriteLine ("-Error- " + e.Message);
                                #endif
                            }
                        }
                        else if (reader.CurrentElementNameAttrValue == PrintSchemaTags.Keywords.PageImageableSizeKeys.ExtentHeight)
                        {
                            try
                            {
                                this._imageableArea._extentHeight = reader.GetCurrentPropertyIntValueWithException();
                            }
                            // We want to catch internal FormatException to skip recoverable XML content syntax error
                            #pragma warning suppress 56502
                            #if _DEBUG
                            catch (FormatException e)
                            #else
                            catch (FormatException)
                            #endif
                            {
                                #if _DEBUG
                                Trace.WriteLine ("-Error- " + e.Message);
                                #endif
                            }
                        }
                        else
                        {
                            #if _DEBUG
                            Trace.WriteLine("-Warning- skip unknown ImageableArea sub-property '" +
                                            reader.CurrentElementNameAttrValue + "' at line " +
                                            reader._xmlReader.LineNumber + ", position " +
                                            reader._xmlReader.LinePosition);
                            #endif
                        }
                    }
                }
                else
                {
                    #if _DEBUG
                    Trace.WriteLine("-Warning- skip unknown PageImageableSize sub-Property '" +
                                    reader.CurrentElementNameAttrValue + "' at line " +
                                    reader._xmlReader.LineNumber + ", position " +
                                    reader._xmlReader.LinePosition);
                    #endif
                }
            }

            bool isValid = false;

            // We require ImageableSizeWidth/Height and ExtentWidth/Height values must be non-negative
            if ((this._imageableSizeWidth >= 0) &&
                (this._imageableSizeHeight >= 0))
            {
                isValid = true;

                // If ImageableArea is present, then its ExtentWidth/Height values must be non-negative.
                if (this.ImageableArea != null)
                {
                    isValid = false;

                    if ((this.ImageableArea._extentWidth >= 0) &&
                        (this.ImageableArea._extentHeight >= 0))
                    {
                        isValid = true;
                    }
                }
            }
            else
            {
                #if _DEBUG
                Trace.WriteLine("-Error- invalid PageImageableSize size values: " + this.ToString());
                #endif
            }

            return isValid;
        }

        #endregion Internal Methods

        #region Internal Fields

        internal int _imageableSizeWidth;
        internal int _imageableSizeHeight;
        internal CanvasImageableArea _imageableArea;

        #endregion Internal Fields
    }
}