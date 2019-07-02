// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
{
    #region usings
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Security.Permissions;
        using Microsoft.Test.RenderingVerification.UnmanagedProxies;
        using Microsoft.Test.RenderingVerification.Model.Analytical.Criteria;
    #endregion usings

    /// <summary>
    /// Summary description for ModelManager.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class ModelManager2: IModelManager2Unmanaged
    {
        /// <summary>
        /// Callback for conflicting matching descriptor
        /// </summary>
        /// <param name="descriptorTomatch">The Descriptor we are looking for</param>
        /// <param name="conflictingDescriptor">The list if conflicting descriptor</param>
        /// <returns>The descriptor to be used for resolving the conflict</returns>
        public delegate DescriptorInfo ConflictResolveCallback(DescriptorInfo descriptorTomatch, DescriptorInfo[] conflictingDescriptor);

        #region Properties
            #region Private Properties
                private DescriptorManager _descriptorManager = null;
                private ImageToShapeIDMapping _imageToShapeIDMapping = null;
                private XmlNode _modelDifferences = null;
                private Hashtable _relationSet = null;
                private Package _failurePackage = null;
                private ConflictResolveCallback _conflictResolveCallback = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Return the DescriptorManager
                /// </summary>
                /// <value></value>
                public DescriptorManager Descriptors
                {
                    get { return _descriptorManager; }
                }
                /// <summary>
                /// Return the Difference between the two models (populate after a CompareModels occurs)
                /// </summary>
                /// <value></value>
                public XmlNode ModelDifferences
                {
                    get { return _modelDifferences; }
                }
                /// <summary>
                /// Get/set the image that is analyzed
                /// </summary>
                /// <value></value>
                public IImageAdapter Image
                {
                    get 
                    {
                        if (_imageToShapeIDMapping.ImageSource == null)
                        {
                            return null;
                        }
                        return (IImageAdapter)_imageToShapeIDMapping.ImageSource.Clone();
                    }
                    set 
                    {
                        if (_imageToShapeIDMapping.ImageSource == value)
                        {
                            return;
                        }
                        _imageToShapeIDMapping.ImageSource = value;
                        _descriptorManager._isDirty = true;
                    }
                }
                /// <summary>
                /// Return the package created when a compare model fails
                /// </summary>
                /// <value></value>
                public Package GeneratedFailurePackage
                {
                    get
                    {
                        return _failurePackage;
                    }
                }
                /// <summary>
                /// Calback to ber called for resolving descriptor conflicts
                /// </summary>
                public ConflictResolveCallback ConflictResolvedCallback
                {
                    get { return _conflictResolveCallback; }
                    set { _conflictResolveCallback = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of this type
            /// </summary>
            public ModelManager2()
            {
                _conflictResolveCallback = new ConflictResolveCallback(InternalConflictResolver);
                _imageToShapeIDMapping = new ImageToShapeIDMapping();
                _descriptorManager = new DescriptorManager(_imageToShapeIDMapping);
                _relationSet = new Hashtable();
            }
            /// <summary>
            /// Create a new instance of this type and specified extra type
            /// </summary>
            /// <param name="extraDescriptors">Extra types</param>
            public ModelManager2(Type[] extraDescriptors) : this()
            {
                ArrayList list = new ArrayList();
                foreach(Type type in extraDescriptors)
                {
                    if ( ( type.GetInterface( typeof(IDescriptor).ToString() ) ) != null )
                    {
                        list.Add(type);
                    }
                }
                _descriptorManager = new DescriptorManager(_imageToShapeIDMapping, (Type[])list.ToArray(typeof(Type)));
            }
            /// <summary>
            /// Create a new instance of this type using the specified IImageAdapter
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to use as image to be analyzed</param>
            public ModelManager2(IImageAdapter imageAdapter) : this()
            {
                _descriptorManager._isDirty = true;
                _imageToShapeIDMapping.ImageSource = (IImageAdapter)imageAdapter.Clone();
            }
            /// <summary>
            /// Create a new instance of this type using the IImageAdapter and the types specified 
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to use as image to be analyzed</param>
            /// <param name="extraDescriptors">Extra types</param>
            public ModelManager2(IImageAdapter imageAdapter, Type[] extraDescriptors) : this(extraDescriptors)
            {
                _descriptorManager._isDirty = true;
                _imageToShapeIDMapping.ImageSource = (IImageAdapter)imageAdapter.Clone();
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void SaveToFile(string fileName)
                {
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(fileName, FileMode.Create);
                        SaveToStream(fileStream);
                    }
                    finally 
                    {
                        if(fileStream != null) { fileStream.Close(); }
                    }
                }
                private XmlNode SaveToXmlNode()
                {
                    MemoryStream memoryStream = null;
                    try
                    {
                        memoryStream = new MemoryStream();
                        SaveToStream(memoryStream);
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(memoryStream);
                        return xmlDoc.DocumentElement;
                    }
                    finally 
                    {
                        if (memoryStream != null) { memoryStream.Close(); }
                    }
                }
                private void SaveToStream(Stream stream)
                {
                    Analyze();
                    Stream serializedStream = null;
                    try
                    {
                        serializedStream = _descriptorManager.Serialize();
                        serializedStream.Seek(0, SeekOrigin.Begin);
                        byte[] buffer = new byte[serializedStream.Length];
                        serializedStream.Read(buffer, 0, buffer.Length);
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    finally 
                    {
                        if (serializedStream != null) { serializedStream.Close(); }
                    }
                }
                private void CheckPath(string filepath)
                {
                    Path.GetFullPath(filepath);
                }
                // Defaut resolver is non-optimal : return first descriptor from the conflicting list
                private DescriptorInfo InternalConflictResolver(DescriptorInfo descriptorToMatch, DescriptorInfo[] conflictingDescriptors)
                {
                    return conflictingDescriptors[0];
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Save the Package after compare model occured
                /// </summary>
                /// <param name="packagePath"></param>
                public void Save(string packagePath)
                {
                    CheckPath(packagePath);
                    if (Image == null)
                    {
                        throw new RenderingVerificationException("Cannot save: No image associated with this ModelManager");
                    }

                    Bitmap bmp = null;
                    Package package = null;

                    if (_descriptorManager._isDirty)
                    {
                        Analyze();
                    }
                    try
                    {
                        package = Package.Create(packagePath, false);
                        bmp = ImageUtility.ToBitmap(Image);
                        package.MasterBitmap = bmp;
                        package.MasterModel = _descriptorManager.Serialize();
                        package.PackageCompare = PackageCompareTypes.ModelAnalytical;
                        package.Save();
                    }
                    finally 
                    {
                        if (package != null) { package.Dispose(); package = null; }
                        if (bmp != null) { bmp.Dispose(); bmp = null; }
                    }
                }
                /// <summary>
                /// Compare 2 models
                /// </summary>
                /// <param name="packagePath"></param>
                /// <returns></returns>
                public bool CompareModels(string packagePath)
                {
                    ModelManager2 modelManager = ModelManager2.Load(packagePath);
                    return CompareModels(modelManager);
                }
                /// <summary>
                /// Compare 2 models
                /// </summary>
                /// <param name="modelManager"></param>
                /// <returns></returns>
                public bool CompareModels(ModelManager2 modelManager)
                {
                    bool success = true;
                    // BUGBUG (Perf) : Might analyze a image already analyzed
                    Analyze();
                    modelManager.Analyze();
                    StreamWriter streamWriter = null;
                    try
                    {
                        string found = "<Found>";
                        string notFound = "<NotFound>";
                        streamWriter = new StreamWriter(new MemoryStream(), System.Text.ASCIIEncoding.ASCII);
#if DEBUG
int debugCount = 0;
#endif // DEBUG
                        foreach (DescriptorInfo descriptorToFind in modelManager._descriptorManager.SelectedDescriptors)
                        {
                            bool match =  _descriptorManager.IsDescriptorMatch(descriptorToFind, streamWriter);
                            success &= match;
                            streamWriter.Flush ();
                            if (match)
                            {
                                // add to "found" section
                                found += System.Text.ASCIIEncoding.ASCII.GetString (((MemoryStream)(streamWriter.BaseStream)).GetBuffer ()).Trim('\0');
                            }
                            else 
                            {
                                // add to "NotFound" section
                                notFound += System.Text.ASCIIEncoding.ASCII.GetString (((MemoryStream)(streamWriter.BaseStream)).GetBuffer ()).Trim('\0');
                            }
#if DEBUG
debugCount++;
#endif // DEBUG
                        }
                        found += "</Found>";
                        notFound += "</NotFound>";

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml ("<DescriptorsDifferences result='" + success.ToString () + "'>" + found + notFound + "</DescriptorsDifferences>");

                        _modelDifferences = xmlDoc.DocumentElement;
                    }
                    finally 
                    {
                        if (streamWriter != null) { streamWriter.Close(); streamWriter = null; }
                    }
                    if (success == false)
                    {
                        _failurePackage = Package.Create(true);
                        _failurePackage.PackageCompare = PackageCompareTypes.ModelAnalytical;
                        _failurePackage.MasterBitmap = ImageUtility.ToBitmap(modelManager.Image); // if MasterBitmap clonethe bmp, we need to dispose the original here
                        _failurePackage.MasterModel = modelManager.Descriptors.Serialize(); // if MasterModel clone the stream, we need to close it here
                        _failurePackage.CapturedBitmap = ImageUtility.ToBitmap(this.Image); // if MasterBitmap clonethe bmp, we need to dispose the original here
                        _failurePackage.XmlDiff = this.ModelDifferences;
                    }
                    return success;
                }
                /// <summary>
                /// Update the current model with the descriptor contained in the one specified
                /// </summary>
                /// <param name="updatingModel"></param>
                public void UpdateModel(ModelManager2 updatingModel)
                {
                    foreach (DescriptorInfo descriptorToMatch in updatingModel._descriptorManager.ActiveDescriptors)
                    {
                        DescriptorInfo[] matches = this._descriptorManager.GetClosestDescriptors(descriptorToMatch);
                        if (matches.Length > 1)
                        {
                            if (_conflictResolveCallback == null)
                            {
                                throw new ArgumentNullException("ConflictResolvedCallback delegate set to null !");
                            }
                            DescriptorInfo di = _conflictResolveCallback(descriptorToMatch, matches);
                            if (di == null) { throw new ArgumentNullException("ConflictResolvedCallback returned null !"); }
                            matches[0] = di;
                        }
                        matches[0].IDescriptor.Name = descriptorToMatch.IDescriptor.Name;
                        matches[0].IDescriptor.Criteria =  descriptorToMatch.IDescriptor.Criteria;
                        // Add the closest match to the list of active Descriptor (so when trying to seve model it will be serialized)
                        if(this._descriptorManager.SelectedDescriptors.Contains(matches[0]) == false)
                        {
                            this._descriptorManager.SelectedDescriptors.Add(matches[0]);
                        }
                    }
                }
                /// <summary>
                /// Analyze the image and create descriptor for it
                /// </summary>
                public void Analyze()
                {
                    _descriptorManager.Analyze();
                }
            #endregion Public Methods
            #region Static Methods
                /// <summary>
                /// Load an existing model
                /// </summary>
                /// <param name="modelFileName"></param>
                /// <returns></returns>
                private static ModelManager2 Load(string modelFileName)
                {
                    Package package = null;
                    ModelManager2 retVal = new ModelManager2();
                    try
                    {
                        package = Package.Load(modelFileName);
                        if ((package.PackageCompare & PackageCompareTypes.ModelAnalytical) == 0)
                        {
                            // TODO : throw instead or returning null
                            return null;
                        }
                        retVal.Descriptors.Deserialize(package.MasterModel);
                        retVal.Image = new ImageAdapter((package.IsFailureAnalysis) ? package.CapturedBitmap : package.MasterBitmap);
                    }
                    finally 
                    {
                        if (package != null) { package.Dispose(); package = null; }
                    }
                    return retVal;
                }
            #endregion Static Methods
        #endregion Methods

        #region IModelManager2Unmanaged implementation
                /// <summary>
                /// Compares this(Rendered img) to model created for masterFileName
                /// </summary>
                /// <param name="masterImg">Contains master img</param>
                /// <param name="vscanFileName">.vscan file name - cab which will package failures</param>
                /// <param name="silhouetteTolerance">Silhouette tolerance</param>
                /// <param name="xTolerance">x shift tolerance</param>
                /// <param name="yTolerance">y shift tolerance</param>
                /// <param name="imgTolerance">image tolerance</param>
                /// <param name="a">A part of ARGB tolerance</param>
                /// <param name="r">R part of ARGB tolerance</param>
                /// <param name="g">G part of ARGB tolerance</param>
                /// <param name="b">B part of ARGB tolerance</param>
                /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
                /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
                /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
                /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
                /// <returns>Returns true if every descriptor in the model was found (within tolerance), false otherwise</returns>
                bool IModelManager2Unmanaged.CompareModelsSavePackage(IImageAdapterUnmanaged masterImg,
                    string vscanFileName,
                    double silhouetteTolerance,
                    double xTolerance,
                    double yTolerance,
                    double imgTolerance,
                    byte a,
                    byte r,
                    byte g,
                    byte b,
                    int rcToCompareLeft,
                    int rcToCompareTop,
                    int rcToCompareRight,
                    int rcToCompareBottom
                    )
                {

System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage() : Started");
#if DEBUG
ImageUtility.ToImageFile(masterImg, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_CMSP_masterImage.png"));
ImageUtility.ToImageFile(Image, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_CMSP_thisImage.png"));
#endif // debug

//logging

                    bool comparisonResult = false;
                    VScan master = null;
                    VScan rendered = new VScan((IImageAdapter)Image.Clone());
                    XmlNode xmlDiff = null;
                    if (!(rcToCompareLeft == 0 && rcToCompareTop == 0 && rcToCompareRight == 0 && rcToCompareBottom == 0))
                    {
                        Rectangle rcToCompare = new Rectangle(rcToCompareLeft, rcToCompareTop, rcToCompareRight - rcToCompareLeft, rcToCompareBottom - rcToCompareTop);
                        IImageAdapter imgAdapter = ImageUtility.ClipImageAdapter(rendered.OriginalData.Image, rcToCompare);
                        rendered.OriginalData.Image = imgAdapter;
                        master = new VScan(ImageUtility.ClipImageAdapter((ImageAdapter)masterImg, rcToCompare));
                    }
                    else
                    {
                        master = new VScan((IImageAdapter)masterImg);
                    }
#if DEBUG
ImageUtility.ToImageFile(master.OriginalData.Image, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CMSP_master.png"));
ImageUtility.ToImageFile(rendered.OriginalData.Image, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CMSP_rendered.png"));
#endif // debug
System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage(): images created");

System.Diagnostics.Debug.WriteLine (string.Format("color to be created a:{0} r:{1} g:{2} b:{3}", a,r,g,b));
                    IColor color = new ColorByte(a, r, g, b);
System.Diagnostics.Debug.WriteLine ("color created");
                    if (color.ARGB == 0) { color.IsEmpty = true; }
System.Diagnostics.Debug.WriteLine ("coor just reset to empty (if necessary)");

                    System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage(): color created");

                    ((IModelManager2Unmanaged)master.OriginalData).CreateModel(silhouetteTolerance,
                            xTolerance,
                            yTolerance,
                            imgTolerance,
                            color);

System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage(): CreateModel() completed");

System.Diagnostics.Debug.WriteLine("Comparing...");
                    comparisonResult = rendered.OriginalData.CompareModels(master.OriginalData);
System.Diagnostics.Debug.WriteLine ("Compare model returned " + comparisonResult.ToString());

                    // Workaround for the bug #906193
                    if (comparisonResult == false)
                    {
System.Diagnostics.Debug.WriteLine ("\nChanging color threshlod...");
                        master.OriginalData.Descriptors.ColorThreshold = master.OriginalData.Descriptors.ColorThreshold + 1;
                        rendered.OriginalData.Descriptors.ColorThreshold = rendered.OriginalData.Descriptors.ColorThreshold + 1;
System.Diagnostics.Debug.WriteLine ("Analyzing master...");
                        master.OriginalData.Analyze();
System.Diagnostics.Debug.WriteLine ("Analyzing rendered...");
                        rendered.OriginalData.Analyze();
System.Diagnostics.Debug.WriteLine ("Comparing again...");
                        comparisonResult = rendered.OriginalData.CompareModels(master.OriginalData);
System.Diagnostics.Debug.WriteLine ("Getting xml diff...");
                        xmlDiff = rendered.OriginalData.ModelDifferences;
System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage() : CompareToModel() returned " + comparisonResult.ToString () +
                                    "  ColorThreshold = " + master.OriginalData.Descriptors.ColorThreshold.ToString ());
                        if (comparisonResult == false)
                        {
System.Diagnostics.Debug.WriteLine ("\nFailed again...");

                            master.OriginalData.Descriptors.ColorThreshold = master.OriginalData.Descriptors.ColorThreshold - 2;
                            rendered.OriginalData.Descriptors.ColorThreshold = this.Descriptors.ColorThreshold - 2;
                            master.OriginalData.Analyze();
                            rendered.OriginalData.Analyze();
                            comparisonResult = rendered.OriginalData.CompareModels(master.OriginalData);
                            xmlDiff = rendered.OriginalData.ModelDifferences;
System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage() : CompareToModel() returned " + comparisonResult.ToString () +
                                    "  ColorThreshold = " + master.OriginalData.Descriptors.ColorThreshold.ToString ());
                            if (comparisonResult == false)
                            {
System.Diagnostics.Debug.WriteLine ("\nFailed one more time ...");
                                master.OriginalData.Descriptors.ColorThreshold = master.OriginalData.Descriptors.ColorThreshold + 1;
                                rendered.OriginalData.Descriptors.ColorThreshold = this.Descriptors.ColorThreshold + 1; // return original to be saved
                                master.OriginalData.Analyze();
                                rendered.OriginalData.Analyze();
                                comparisonResult = rendered.OriginalData.CompareModels(master.OriginalData);
                                xmlDiff = rendered.OriginalData.ModelDifferences;
System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage() : CompareToModel() returned " + comparisonResult.ToString () +
                                    "  ColorThreshold = " + master.OriginalData.Descriptors.ColorThreshold.ToString ());
                            }
                        }
                    }

                    if (comparisonResult == false && vscanFileName != null && vscanFileName != string.Empty)
                    {
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : before <new Package>. vscanFileName=" + vscanFileName);
                        Package package = Package.Create(vscanFileName, true);
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 1");
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 2");
                        package.MasterBitmap = ImageUtility.ToBitmap(master.OriginalData.Image);
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 3");
                        package.CapturedBitmap = ImageUtility.ToBitmap(rendered.OriginalData.Image);
//System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 4");
//                        byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(xmlDoc.DocumentElement.OuterXml);
                        System.Diagnostics.Debug.WriteLine ("CompareModelsSavePackage() : Saving package 5");
                        Stream memoryStream = master.OriginalData.Descriptors.Serialize ();
                        package.MasterModel = memoryStream;
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 6");
                        package.CapturedBitmap = ImageUtility.ToBitmap(rendered.OriginalData.Image);
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 7");
                        package.XmlDiff = xmlDiff;
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 8");
                        package.PackageCompare = PackageCompareTypes.ImageCompare | PackageCompareTypes.ModelAnalytical;
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : Saving package 9");
                        package.Save();
System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : PackageModel() completed");
                    }

System.Diagnostics.Debug.WriteLine("CompareModelsSavePackage() : completed");
return comparisonResult;
                }

                /// <summary>
                /// Create Model
                /// </summary>
                /// <param name="silhouetteTolerance">Silhouette tolerance</param>
                /// <param name="xTolerance">x shift tolerance</param>
                /// <param name="yTolerance">y shift tolerance</param>
                /// <param name="imgTolerance">image tolerance</param>
                /// <param name="color">The ARGB tolerance</param>
                void IModelManager2Unmanaged.CreateModel(double silhouetteTolerance,
                    double xTolerance,
                    double yTolerance,
                    double imgTolerance,
                    IColor color)
                {
System.Diagnostics.Debug.WriteLine("CreateModel() :" + "\n  shapeTolerance = " + silhouetteTolerance.ToString() + "\n  imgTolerance = " + imgTolerance.ToString() + "\n  (x y) tolerance = (" + xTolerance.ToString() + yTolerance.ToString() + ")" + "\n  (a,r,g,b) tolerance = (" + color.ToString() + ")\n");
 
//logging
System.Diagnostics.Debug.WriteLine("Analyzing...");
                    Analyze();
System.Diagnostics.Debug.WriteLine("Adding Descriptors...");
                    Descriptors.SelectedDescriptors.AddRange(Descriptors.ActiveDescriptors);
                    System.Collections.ArrayList descriptors = Descriptors.SelectedDescriptors;
System.Diagnostics.Debug.WriteLine("Start Looping...");
                    for (int t = 0; t < descriptors.Count; t++)
                    {
                        ArrayList criteria = new ArrayList();
                        DescriptorInfo descr = (DescriptorInfo)descriptors[t];
                        descr.IDescriptor.Name = t.ToString();
                        if (silhouetteTolerance >= 0) { criteria.Add(new Criteria.SilhouetteCriterion(silhouetteTolerance)); }
                        if (xTolerance >= 0) { criteria.Add (new Criteria.XPositionCriterion (xTolerance)); }
                        if (yTolerance >= 0) { criteria.Add(new Criteria.YPositionCriterion(yTolerance)); }
                        if (imgTolerance >= 0) { criteria.Add(new Criteria.TextureCriterion(imgTolerance)); }
                        if ((ColorByte)color != ColorByte.Empty) { criteria.Add(new Criteria.ColorAverageCriterion((ColorDouble)color.ToColor())); }
                        descr.IDescriptor.Criteria = (Criterion[])criteria.ToArray(typeof(Criterion));
                    }
                }

                /// <summary>
                /// Get the internal image
                /// </summary>
                IImageAdapterUnmanaged IModelManager2Unmanaged.ImageAdapter
                {
                    get
                    {
//System.Diagnostics.Debug.WriteLine ("Image GETTER");
                        return (IImageAdapterUnmanaged)Image;
                    }
                    set
                    {
//System.Diagnostics.Debug.WriteLine ("Image SETTER");
                        Image = (IImageAdapter)value;
                    }

                }
        #endregion IModelManager2Unmanaged implementation
    }
}
