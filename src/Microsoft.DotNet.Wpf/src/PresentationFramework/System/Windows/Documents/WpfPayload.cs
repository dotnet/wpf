// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper class for creating and accessing WPF Payloads in packages
//    This file contains the definition  and implementation
//    for the WpfPayload class.  This class acts as the
//    helper for accessing the content of WPF packages.
//    WPF package is a specialized structure consistent
//    with Open Container Specification, so its content
//    can be accessed via abstract Package class api.
//    This class provides a set of convenience methods
//    specific for xaml content. It allows WPF packages
//    creation from avalon objects; it allows inspecting 
//    a package's structure without instantiating actual 
//    xaml content; it allows loading from WPF content
//    (instantiating avalon objects).
//

namespace System.Windows.Documents
{
    using MS.Internal; // Invariant
    using MS.Internal.IO.Packaging;
    using System;
    using System.Xml;
    using System.Windows.Markup; // TypeConvertContext, ParserContext
    using System.Windows.Controls; // Image
    using System.Collections.Generic; // List
    using System.ComponentModel; // TYpeDescriptor
    using System.Globalization; // CultureInfo

    using System.Windows.Media; // ImageSource
    using System.Windows.Media.Imaging; // BitmapEncoder
    using System.IO; // MemoryStream
    using System.IO.Packaging; // Package
    using System.Threading; // Interlocked.Increment
    using System.Security; // SecurityCritical, SecurityTreatAsSafe attributes
    using MS.Internal.PresentationFramework; // SecurityHelper

    using InternalPackUriHelper = MS.Internal.IO.Packaging.PackUriHelper;
    // An object supporting flow content packaging with images and other resources.
    /// <summary>
    /// WpfPayload is a class providing services for creating,
    /// loading, inspecting and modifying WPF packages.
    /// WPF package stands for "Windows Presentation Foundation package"
    /// and used for combining sets of interrelated WPF resources
    /// such as xaml files, images, fonts, ink, video etc.
    /// </summary>
    /// <example>
    /// <para>
    /// Example 1. Using WpfPayload for saving avalon objects into a single-file container.
    /// </para>
    /// </example>
    internal class WpfPayload
    {
        // -------------------------------------------------------------
        //
        // Constants
        //
        // -------------------------------------------------------------

        // Content types indicate type content for parts
        // containing arbitrary xaml xml. Entry part of xaml package must be of such content type.
        // This string is defined in the WPF Spec.
        private const string XamlContentType = "application/vnd.ms-wpf.xaml+xml";

        // Content types for various kinds of images
        internal const string ImageBmpContentType  = "image/bmp";
        private const string ImageGifContentType  = "image/gif";
        private const string ImageJpegContentType = "image/jpeg";
        private const string ImageTiffContentType = "image/tiff";
        private const string ImagePngContentType  = "image/png";

        private const string ImageBmpFileExtension  = ".bmp";
        private const string ImageGifFileExtension  = ".gif";
        private const string ImageJpegFileExtension = ".jpeg";
        private const string ImageJpgFileExtension  = ".jpg";
        private const string ImageTiffFileExtension = ".tiff";
        private const string ImagePngFileExtension  = ".png";

        // Relationship uri for xaml payload entry part. The relationship is established
        // between the whole package and a part representing an entry point for xaml payload.
        // The reffered part is supposed to have XamlContentType content type.
        // This string is defined in the WPF Spec.
        private const string XamlRelationshipFromPackageToEntryPart = "http://schemas.microsoft.com/wpf/2005/10/xaml/entry";

        // Relationship uri for any secondary part of a xaml payload - images, fonts, ink, xaml pages, etc.
        // This relationship is established betweeb a part with XamlContentType content type
        // and a part with any other appropriate content type (such as image/png).
        // This string is defined in the WPF Spec.
        private const string XamlRelationshipFromXamlPartToComponentPart = "http://schemas.microsoft.com/wpf/2005/10/xaml/component";

        // To separate xaml payload from potential other content (such as fixed)
        // we group all parts belonging to xaml payload under one directory.
        // This is a name of this directory.
        // This directory structure is not required by spec, so it is just
        // a default structure. Applications loading or inspecting WPF packages
        // should not make any assumptions about such directory structure.
        // Using this directory though provides a good defauilt experience
        // for exhcanging data between packages and regular file system:
        // simply part copying creates a file structure separated from other files
        // and actionable without a container.
        // This string is not specified in the WPF Spec.
        private const string XamlPayloadDirectory = "/Xaml"; // This directory must be available as a parameter of Save methods.

        // We use this name for entry part of xaml payload.
        // The application may not make any assumptions about this name 
        // when loading or inspecting a WPF package.
        // This string is not specified in the WPF Spec.
        private const string XamlEntryName = "/Document.xaml"; // This name must be available as a parameter of Save methods

        // We use this name for image part of xaml payload.
        // The application may not make any assumptions about this name 
        // when loading or inspecting a WPF package.
        // This string is not specified in the WPF Spec.
        private const string XamlImageName = "/Image"; // Shouldn't we use original image name instead?

        // -------------------------------------------------------------
        //
        // Constructors
        //
        // -------------------------------------------------------------

        // Public Constructor - initializes an instance of WpfPayload.
        // A new instance of WpfPayload must be created for every copy operation.
        // This instance will maintain a collection of binary resources
        // needed for this act of copying. (The WpfPayload cannot be reused
        // in the subsequent copying).
        // The constructor is designed to be a lightweight, so it does not
        // create a container yet. The instance of WpfPayload
        // maintains only a list of images needed to be serialized.
        // If the list is empty the container creation can be avoided,
        // otherwise it can be created later by CreateContainer method call.
        private WpfPayload(Package package)
        {
            // null package is valid value.
            _package = package;
        }

        // -------------------------------------------------------------
        //
        // Public Methods
        //
        // -------------------------------------------------------------

        /// <summary>
        /// Saves the content of the range in the given stream as a WPF payload.
        /// </summary>
        /// <param name="range">
        /// The range whose content is to be serialized.
        /// </param>
        /// <param name="stream">
        /// When the stream is not null, it is a request to unconditionally
        /// creatte WPF package in this stream.
        /// If this parameter is null, then the package is created
        /// only when necessary - when there are images in the range.
        /// The new MemoryStream is created in this case and assigned to this
        /// parameter on exit.
        /// </param>
        /// <param name="useFlowDocumentAsRoot">
        /// </param>
        /// <returns>
        /// A xaml part of serialized content.
        /// </returns>
        internal static string SaveRange(ITextRange range, ref Stream stream, bool useFlowDocumentAsRoot)
        {
            return SaveRange(range, ref stream, useFlowDocumentAsRoot, false /* preserveTextElements */);
        }

        /// <summary>
        /// Saves the content of the range in the given stream as a WPF payload.
        /// </summary>
        /// <param name="range">
        /// The range whose content is to be serialized.
        /// </param>
        /// <param name="stream">
        /// When the stream is not null, it is a request to unconditionally
        /// creatte WPF package in this stream.
        /// If this parameter is null, then the package is created
        /// only when necessary - when there are images in the range.
        /// The new MemoryStream is created in this case and assigned to this
        /// parameter on exit.
        /// </param>
        /// <param name="useFlowDocumentAsRoot">
        /// </param>
        /// <param name="preserveTextElements">
        /// If set false, custom TextElements will be upcasted to known types.
        /// </param>
        /// <returns>
        /// A xaml part of serialized content.
        /// </returns>
        internal static string SaveRange(ITextRange range, ref Stream stream, bool useFlowDocumentAsRoot, bool preserveTextElements)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            // Create the wpf package in the stream
            WpfPayload wpfPayload = new WpfPayload(/*package:*/null);

            // Create a string representing serialized xaml
            StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
            TextRangeSerialization.WriteXaml(xmlWriter, range, useFlowDocumentAsRoot, wpfPayload, preserveTextElements);
            string xamlText = stringWriter.ToString();

            // Decide whether we need to create a package
            if (stream != null || wpfPayload._images != null)
            {
                // There are images in the content. Need to create a package
                if (stream == null)
                {
                    stream = new MemoryStream();
                }

                // Create a package in the stream
                using (wpfPayload.CreatePackage(stream))
                {
                    // Create the entry part for xaml content of the WPF package
                    PackagePart xamlEntryPart = wpfPayload.CreateWpfEntryPart();

                    // Write the part's content
                    Stream xamlPartStream = xamlEntryPart.GetSeekableStream();
                    using (xamlPartStream)
                    {
                        StreamWriter xamlPartWriter = new StreamWriter(xamlPartStream);
                        using (xamlPartWriter)
                        {
                            xamlPartWriter.Write(xamlText);
                        }
                    }

                    // Write relationships from xaml entry part to all images
                    wpfPayload.CreateComponentParts(xamlEntryPart);
                }

                Invariant.Assert(wpfPayload._images == null); // must have beed cleared in CreateComponentParts
            }

            return xamlText;
        }

        // Creates a WPF container in new MemoryStream and places an image into it
        // with the simplest xaml part referring to it (wrapped into InlineUIContainer).
        internal static MemoryStream SaveImage(BitmapSource bitmapSource, string imageContentType)
        {
            MemoryStream stream = new MemoryStream();

            // Create the wpf package in the stream
            WpfPayload wpfPayload = new WpfPayload(/*package:*/null);

            // Create a package in the stream
            using (wpfPayload.CreatePackage(stream))
            {
                // Define a reference for the image
                int imageIndex = 0;
                string imageReference = GetImageReference(GetImageName(imageIndex, imageContentType));

                // Create the entry part for xaml content of the WPF package
                PackagePart xamlEntryPart = wpfPayload.CreateWpfEntryPart();

                // Write the part's content
                Stream xamlPartStream = xamlEntryPart.GetSeekableStream();
                using (xamlPartStream)
                {
                    StreamWriter xamlPartWriter = new StreamWriter(xamlPartStream);
                    using (xamlPartWriter)
                    {
                        string xamlText = 
                            "<Span xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                            "<InlineUIContainer><Image " +
                            "Width=\"" +
                            bitmapSource.Width + "\" " +
                            "Height=\"" +
                            bitmapSource.Height + "\" " +
                            "><Image.Source><BitmapImage CacheOption=\"OnLoad\" UriSource=\"" +
                            imageReference +
                            "\"/></Image.Source></Image></InlineUIContainer></Span>";
                        xamlPartWriter.Write(xamlText);
                    }
                }

                // Add image to a package
                wpfPayload.CreateImagePart(xamlEntryPart, bitmapSource, imageContentType, imageIndex);
            }

            return stream;
        }

        /// <summary>
        /// Loads xaml content from a WPF package.
        /// </summary>
        /// <param name="stream">
        /// Stream that must be accessible for reading and structured as
        /// a WPF container: part XamlEntryPart is expected as one of
        /// its entry parts.
        /// </param>
        /// <returns>
        /// Returns a xaml element loaded from the entry part of the package.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws parsing exception when the xaml content does not comply with the xaml schema.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws validation exception when the package is not well structured.
        /// </exception>
        /// <exception cref="Exception">
        /// Throws uri exception when the pachageBaseUri is not correct absolute uri.
        /// </exception>
        /// <remarks>
        /// USED IN LEXICON VIA REFLECTION
        /// </remarks>
        internal static object LoadElement(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            object xamlObject;

            try
            {
                WpfPayload wpfPayload = WpfPayload.OpenWpfPayload(stream);

                // Now load the package
                using (wpfPayload.Package)
                {
                    // Validate WPF paypoad and get its entry part
                    PackagePart xamlEntryPart = wpfPayload.ValidatePayload();

                    // Define a unique uri for this instance of PWF payload.
                    // Uniqueness is required to make sure that cached images are not mixed up.
                    int newWpfPayoutCount = Interlocked.Increment(ref _wpfPayloadCount);
                    Uri payloadUri = new Uri("payload://wpf" + newWpfPayoutCount, UriKind.Absolute);
                    Uri entryPartUri = System.IO.Packaging.PackUriHelper.Create(payloadUri, xamlEntryPart.Uri); // gives an absolute uri of the entry part
                    Uri packageUri = System.IO.Packaging.PackUriHelper.GetPackageUri(entryPartUri); // extracts package uri from combined package+part uri
                    PackageStore.AddPackage(packageUri, wpfPayload.Package); // Register the package

                    // Set this temporary uri as a base uri for xaml parser
                    ParserContext parserContext = new ParserContext();
                    parserContext.BaseUri = entryPartUri;

                    // Call xaml parser
                    xamlObject = XamlReader.Load(xamlEntryPart.GetSeekableStream(), parserContext, useRestrictiveXamlReader: true);

                    // Remove the temporary uri from the PackageStore
                    PackageStore.RemovePackage(packageUri);
                }
            }
            catch (XamlParseException e)
            {
                // Incase of xaml parsing or package structure failure
                // we return null.
                Invariant.Assert(e != null); //to make compiler happy about not using a variable e. This variable is useful in debugging process though - to see a reason of a parsing failure
                xamlObject = null;
            }
            catch (System.IO.FileFormatException)
            {
                xamlObject = null;
            }
            catch (System.IO.FileLoadException)
            {
                xamlObject = null;
            }
            catch (System.OutOfMemoryException)
            {
                xamlObject = null;
            }

            return xamlObject;
        }

        // Checks whether the WPF payload meets minimal structural requirements:
        // 1) Entry part exists
        // 2) All components have proper relationships established between source and target
        // Returns an entry part of the payload if it is valid; throws otherwise.
        private PackagePart ValidatePayload()
        {
            // Get the WPF entry part
            PackagePart xamlEntryPart = this.GetWpfEntryPart();
            if (xamlEntryPart == null)
            {
                throw new XamlParseException(SR.Get(SRID.TextEditorCopyPaste_EntryPartIsMissingInXamlPackage));
            }

            //  Add more validation for package structure

            return xamlEntryPart;
        }

        static int _wpfPayloadCount; // used to disambiguate between all acts of loading from different WPF payloads.

        // -------------------------------------------------------------
        //
        // Public Properties
        //
        // -------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns a Package containing this WpfPayload.
        /// </summary>
        public Package Package
        {
            get
            {
                return _package;
            }
        }

        #endregion Public Properties

        // -------------------------------------------------------------
        //
        // Internal Methods
        //
        // -------------------------------------------------------------

        /// <summary>
        /// Gets a BitmapSource from an Image. In the case of a DrawingImage, we must first render
        /// to an offscreen bitmap since the DrawingImage's previously rendered bits are not kept 
        /// in memory.
        /// </summary>
        private BitmapSource GetBitmapSourceFromImage(Image image)
        {
            if (image.Source is BitmapSource)
            {
                return (BitmapSource)image.Source;
            }

            Invariant.Assert(image.Source is DrawingImage);
            DpiScale dpi = image.GetDpi();
            DrawingImage di = (DrawingImage)image.Source;
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(di.Width * dpi.DpiScaleX), (int)(di.Height * dpi.DpiScaleY), 
                96.0, 96.0, PixelFormats.Default);
            rtb.Render(image);

            return rtb;
        }

        // Creates relationships from the given part to all images currently stored in _images array.
        // This method is supposed to be called at the end of each sourcePart processing
        // when _images array still contains a list of all images referenced from this part.
        private void CreateComponentParts(PackagePart sourcePart)
        {
            if (_images != null)
            {
                for (int imageIndex = 0; imageIndex < _images.Count; imageIndex++)
                {
                    Image image = _images[imageIndex];

                    // Define image type
                    string imageContentType = GetImageContentType(image.Source.ToString());

                    CreateImagePart(sourcePart, GetBitmapSourceFromImage(image), imageContentType, imageIndex);
                }

                // Clear _images array - to avoid the temptation of re-usinng it anymore.
                _images = null;
            }
        }

        // Creates a part containing an image with a relationship to it from a sourcePart
        private void CreateImagePart(PackagePart sourcePart, BitmapSource imageSource, string imageContentType, int imageIndex)
        {
            // Generate a new unique image part name
            string imagePartUriString = GetImageName(imageIndex, imageContentType);

            // Define an image part uri
            Uri imagePartUri = new Uri(XamlPayloadDirectory + imagePartUriString, UriKind.Relative);

            // Create a part for the image
            PackagePart imagePart = _package.CreatePart(imagePartUri, imageContentType, CompressionOption.NotCompressed);

            // Create the relationship referring from the enrty part to the image part
            PackageRelationship componentRelationship = sourcePart.CreateRelationship(imagePartUri, TargetMode.Internal, XamlRelationshipFromXamlPartToComponentPart);

            // Encode the image data
            BitmapEncoder bitmapEncoder = GetBitmapEncoder(imageContentType);
            bitmapEncoder.Frames.Add(BitmapFrame.Create(imageSource));

            // Save encoded image data into the image part in the package
            Stream imageStream = imagePart.GetSeekableStream();
            using (imageStream)
            {
                bitmapEncoder.Save(imageStream);
            }
        }

        // Adds an image data to the package.
        // Returns a local Uri that must be used to access this data
        // from the package - from its top level directory.
        internal string AddImage(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            if (image.Source == null)
            {
                throw new ArgumentNullException("image.Source");
            }

            if (string.IsNullOrEmpty(image.Source.ToString()))
            {
                throw new ArgumentException(SR.Get(SRID.WpfPayload_InvalidImageSource));
            }

            if (_images == null)
            {
                _images = new List<Image>();
            }

            // Define the image uri for the new image
            string imagePartUriString = null;

            // Define image type
            string imageContentType = GetImageContentType(image.Source.ToString());

            // Check whether we already have the image with the same BitmapFrame
            for (int i = 0; i < _images.Count; i++)
            {
                if (ImagesAreIdentical(GetBitmapSourceFromImage(_images[i]), GetBitmapSourceFromImage(image)))
                {
                    // Image content types must be consistent
                    Invariant.Assert(imageContentType == GetImageContentType(_images[i].Source.ToString()), "Image content types expected to be consistent: " + imageContentType + " vs. " + GetImageContentType(_images[i].Source.ToString()));

                    // We have this image registered already. Return its part uri
                    imagePartUriString = GetImageName(i, imageContentType);
                }
            }

            // If this is new unique image, add it to our collection
            if (imagePartUriString == null)
            {
                // Generate a new unique image part name
                imagePartUriString = GetImageName(_images.Count, imageContentType);

                _images.Add(image); // this will change _images.Count used for generating image parts names
            }

            // Return the image Part Uri for xaml serializer to use as Image.Source attribute
            return GetImageReference(imagePartUriString);
        }

        // Parses the imageUriString to identify its content type.
        // The decision is made based on file extension.
        // When file extension is not recognized, image/png is choosen.
        private static string GetImageContentType(string imageUriString)
        {
            string imageContentType;
            if (imageUriString.EndsWith(ImageBmpFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                imageContentType = ImageBmpContentType;
            }
            else if (imageUriString.EndsWith(ImageGifFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                imageContentType = ImageGifContentType;
            }
            else if (imageUriString.EndsWith(ImageJpegFileExtension, StringComparison.OrdinalIgnoreCase) || imageUriString.EndsWith(ImageJpgFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                imageContentType = ImageJpegContentType;
            }
            else if (imageUriString.EndsWith(ImageTiffFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                imageContentType = ImageTiffContentType;
            }
            else
            {
                imageContentType = ImagePngContentType;
            }

            return imageContentType;
        }

        // Returns a BitmapEncoder corresponding to a given imageContentType
        private static BitmapEncoder GetBitmapEncoder(string imageContentType)
        {
            BitmapEncoder bitmapEncoder;

            switch (imageContentType)
            {
                case ImageBmpContentType:
                    bitmapEncoder = new BmpBitmapEncoder();
                    break;
                case ImageGifContentType:
                    bitmapEncoder = new GifBitmapEncoder();
                    break;
                case ImageJpegContentType:
                    bitmapEncoder = new JpegBitmapEncoder();
                    // investigate crash when qualitylevel is set to 100.
                    //((JpegBitmapEncoder)bitmapEncoder).QualityLevel = 100; // To minimize data loss; default is 75
                    break;
                case ImageTiffContentType:
                    bitmapEncoder = new TiffBitmapEncoder();
                    break;
                case ImagePngContentType:
                    bitmapEncoder = new PngBitmapEncoder();
                    break;
                default:
                    Invariant.Assert(false, "Unexpected image content type: " + imageContentType);
                    bitmapEncoder = null;
                    break;
            }

            return bitmapEncoder;
        }

        // Returns a file extension corresponding to a given imageContentType
        private static string GetImageFileExtension(string imageContentType)
        {
            string imageFileExtension;
            switch (imageContentType)
            {
                case ImageBmpContentType:
                    imageFileExtension = ImageBmpFileExtension;
                    break;
                case ImageGifContentType:
                    imageFileExtension = ImageGifFileExtension;
                    break;
                case ImageJpegContentType:
                    imageFileExtension = ImageJpegFileExtension;
                    break;
                case ImageTiffContentType :
                    imageFileExtension = ImageTiffFileExtension;
                    break;
                case ImagePngContentType:
                    imageFileExtension = ImagePngFileExtension;
                    break;
                default:
                    Invariant.Assert(false, "Unexpected image content type: " + imageContentType);
                    imageFileExtension = null;
                    break;
            }

            return imageFileExtension;
        }

        // Returns true if image bitmap data in memory aree the same instance for the both images
        private static bool ImagesAreIdentical(BitmapSource imageSource1, BitmapSource imageSource2)
        {
            // First compare images as objects - the luckiest case is when it's the same object
            BitmapFrameDecode imageBitmap1 = imageSource1 as BitmapFrameDecode;
            BitmapFrameDecode imageBitmap2 = imageSource2 as BitmapFrameDecode;
            if (imageBitmap1 != null && imageBitmap2 != null &&
                imageBitmap1.Decoder.Frames.Count == 1 && imageBitmap2.Decoder.Frames.Count == 1 &&
                imageBitmap1.Decoder.Frames[0] == imageBitmap2.Decoder.Frames[0])
            {
                return true; // ImageSources have the same instance of bitmap data. They are obviousely identical.
            }

            if (imageSource1.Format.BitsPerPixel != imageSource2.Format.BitsPerPixel ||
                imageSource1.PixelWidth != imageSource2.PixelWidth ||
                imageSource1.PixelHeight != imageSource2.PixelHeight ||
                imageSource1.DpiX != imageSource2.DpiX ||
                imageSource1.DpiY != imageSource2.DpiY ||
                imageSource1.Palette != imageSource2.Palette)
            {
                return false; // Images have different characteristics
            }

            int stride = ((imageSource1.PixelWidth * imageSource1.Format.BitsPerPixel) + 7) / 8;
            int bufferSize = (stride * (imageSource1.PixelHeight - 1)) + stride;

            Byte[] buffer1 = new Byte[bufferSize];
            Byte[] buffer2 = new Byte[bufferSize];

            imageSource1.CopyPixels(buffer1, stride, /*offset:*/0);
            imageSource2.CopyPixels(buffer2, stride, /*offset:*/0);
            for (int i = 0; i < bufferSize; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false; // Images have different pixels
                }
            }

            return true; // Images are equal
        }

        // ------------------------------------
        // API needed for RTF-to-XAML Converter
        // ------------------------------------

        internal Stream CreateXamlStream()
        {
            PackagePart part = this.CreateWpfEntryPart();

            // Return a stream opened for writing an image data
            return part.GetSeekableStream();
        }

        internal Stream CreateImageStream(int imageCount, string contentType, out string imagePartUriString)
        {
            // Generate a new unique image part name
            imagePartUriString = GetImageName(imageCount, contentType);

            // Add image part to the conntainer
            // Define an image part uri
            Uri imagePartUri = new Uri(XamlPayloadDirectory + imagePartUriString, UriKind.Relative);

            // Create a part for the image
            PackagePart imagePart = _package.CreatePart(imagePartUri, contentType, CompressionOption.NotCompressed);

            // Create the relationship referring from the enrty part to the image part
            //PackageRelationship entryRelationship = _currentXamlPart.CreateRelationship(imagePartUri, TargetMode.Internal, XamlRelationshipFromXamlPartToComponentPart);

            // Return relative name for the image part as out parameter
            imagePartUriString = GetImageReference(imagePartUriString);

            // Return a stream opened for writing an image data
            return imagePart.GetSeekableStream();
        }

        internal Stream GetImageStream(string imageSourceString)
        {
            Invariant.Assert(imageSourceString.StartsWith("./", StringComparison.OrdinalIgnoreCase));
            imageSourceString = imageSourceString.Substring(1); // cut the leading dot out
            Uri imagePartUri = new Uri(XamlPayloadDirectory + imageSourceString, UriKind.Relative);
            PackagePart imagePart = _package.GetPart(imagePartUri);
            return imagePart.GetSeekableStream();
        }

        // -------------------------------------------------------------
        //
        // Private Methods
        //
        // -------------------------------------------------------------

        private Package CreatePackage(Stream stream)
        {
            Invariant.Assert(_package == null, "Package has been already created or open for this WpfPayload");

            _package = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);

            return _package;
        }

        /// <summary>
        /// Creates an instance of WpfPayload object.
        /// </summary>
        /// <param name="stream">
        /// A stream where the package for this wpf payload is contained
        /// </param>
        /// <returns>
        /// Returns an instance of WpfPayload.
        /// </returns>
        /// <remarks>
        /// The instance of WpfPayload created by this method is supposed
        /// to be disposed later (IDispose.Dispose()) or closed by calling 
        /// the Close method - to flush all changes to a persistent storage
        /// and free all temporary resources.
        /// </remarks>
        internal static WpfPayload CreateWpfPayload(Stream stream)
        {
            Package package = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);
            return new WpfPayload(package);
        }

        /// <summary>
        /// Creates an instance of WpfPayload object.
        /// </summary>
        /// <param name="stream">
        /// A stream where the package for this wpf payload is contained
        /// </param>
        /// <returns>
        /// Returns an instance of WpfPayload.
        /// </returns>
        /// <remarks>
        /// The instance of WpfPayload created by this method is supposed
        /// to be disposed later (IDispose.Dispose()) or closed by calling 
        /// the Close method - to flush all changes to a persistent storage
        /// and free all temporary resources.
        /// </remarks>
        internal static WpfPayload OpenWpfPayload(Stream stream)
        {
            Package package = Package.Open(stream, FileMode.Open, FileAccess.Read);
            return new WpfPayload(package);
        }

        private PackagePart CreateWpfEntryPart()
        {
            // Define an entry part uri
            Uri entryPartUri = new Uri(XamlPayloadDirectory + XamlEntryName, UriKind.Relative);

            // Create the main xaml part
            PackagePart part = _package.CreatePart(entryPartUri, XamlContentType, CompressionOption.Normal);
                // Compression is turned off in this mode.
                //NotCompressed = -1,
                // Compression is optimized for a resonable compromise between size and performance. 
                //Normal = 0,
                // Compression is optimized for size. 
                //Maximum = 1,
                // Compression is optimized for performance. 
                //Fast = 2 ,
                // Compression is optimized for super performance. 
                //SuperFast = 3,

            // Create the relationship referring to the entry part
            PackageRelationship entryRelationship = _package.CreateRelationship(entryPartUri, TargetMode.Internal, XamlRelationshipFromPackageToEntryPart);

            return part;
        }

        /// <summary>
        /// Retrieves an entry part marked as a WPF payload entry part
        /// by appropriate package relationship.
        /// </summary>
        /// <returns>
        /// PackagePart containing a Wpf package entry.
        /// Null if such part does not exist in this package.
        /// </returns>
        private PackagePart GetWpfEntryPart()
        {
            PackagePart wpfEntryPart = null;

            // Find a relationship to entry part
            PackageRelationshipCollection entryPartRelationships = _package.GetRelationshipsByType(XamlRelationshipFromPackageToEntryPart);
            PackageRelationship entryPartRelationship = null;
            foreach (PackageRelationship packageRelationship in entryPartRelationships)
            {
                entryPartRelationship = packageRelationship;
                break;
            }

            // Get a part referred by this relationship
            if (entryPartRelationship != null)
            {
                // Get entry part uri
                Uri entryPartUri = entryPartRelationship.TargetUri;

                // Get the enrty part
                wpfEntryPart = _package.GetPart(entryPartUri);
            }

            return wpfEntryPart;
        }

        // Generates a image part Uri for the given image index
        private static string GetImageName(int imageIndex, string imageContentType)
        {
            string imageFileExtension = GetImageFileExtension(imageContentType);

            return XamlImageName + (imageIndex + 1) + imageFileExtension;
        }

        // Generates a relative URL for using from within xaml Image tag.
        private static string GetImageReference(string imageName)
        {
            return "." + imageName; // imageName is supposed to be created by GetImageName method
        }

        // -------------------------------------------------------------
        //
        // Private Fields
        //
        // -------------------------------------------------------------

        #region Private Fields

        // Package used as a storage for thow WPF container
        private Package _package;

        // Hashtable of images added to a package so far.
        // Used during xaml serialization intended for adding as a part of the WPF package.
        private List<Image> _images;

        #endregion Private Fields
    }
}
