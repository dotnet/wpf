// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#ifndef __PRINTDOCUMENTIMAGEABLEAREA_HPP__
#define __PRINTDOCUMENTIMAGEABLEAREA_HPP__

/*++
    Abstract:
        This file includes the declarations of the PrintDocumentImageableArea which
        represents the imageable dimensions used during printing.
--*/
namespace System
{
namespace Printing
{
    /// <summary>
    /// This defines the properties representing the imageable area on paper targeted
    /// by a print operation.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintDocumentImageableArea 
    {
        internal:

        /// <summary>
        /// Instantiates a <c>PrintDocumentImageableArea</c> object representing the different imageable
        /// dimensions of the target device and paper for printing.
        /// </summary>
        PrintDocumentImageableArea(
            );

        public:

        /// <value>
        /// Starting x origin of the imageable area
        /// </value>
        property
        double
        OriginWidth
        {
            public:
            double get();
            internal:
            void set(double originWidth);
        }

        /// <value>
        /// Starting Y origin of the imageable area
        /// </value>
        property
        double
        OriginHeight
        {
            public:
            double get();
            internal:
            void set(double originHeight);
        }

        /// <value>
        /// Imageable Area Width
        /// </value>
        property
        double
        ExtentWidth
        {
            public:
            double get();
            internal:
            void set(double extentWidth);
        }

        /// <value>
        /// Imageable Area Height
        /// </value>
        property
        double
        ExtentHeight
        {
            public:
            double get();
            internal:
            void set(double extentHeight);
        }

        /// <value>
        /// Physical Paper Media Width
        /// </value>
        property
        double
        MediaSizeWidth
        {
            public:
            double get();
            internal:
            void set(double mediaSizeWidth);
        }

        /// <value>
        /// Physical Paper Media Height
        /// </value>
        property
        double
        MediaSizeHeight
        {
            public:
            double get();
            internal:
            void set(double mediaSizeHeight);
        }

        private:

        void
        VerifyAccess(
            void
            );

            

        double      _originWidth;
        double      _originHeight;
        double      _extentWidth;
        double      _extentHeight;
        double      _mediaSizeWidth;
        double      _mediaSizeHeight;
        PrintSystemDispatcherObject^    _accessVerifier;
    };
}
}

#endif
