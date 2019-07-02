// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
{
    #region usings
        using System;
        using System.IO;
        using System.Xml;
        using System.Reflection;
        using System.Collections;
        using System.Xml.Serialization;
        using System.Security.Permissions;
        using System.Runtime.Serialization.Formatters;
        using Microsoft.Test.RenderingVerification.Filters;
        // TODO-Miguep: check that this works the same  (changed from soap to binary formatter)
        using System.Runtime.Serialization.Formatters.Binary;
        using Microsoft.Test.RenderingVerification.Model.Analytical.Criteria;
        using Microsoft.Test.RenderingVerification.Model.Analytical.Descriptors;
    #endregion usings

    /// <summary>
    /// Summary description for DescriptorManager.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class DescriptorManager
    {
        #region Constants
            private const int COLORTHRESHOLD = 20;
            private const int NOISETHRESHOLD = 6;
        #endregion Constants

        #region Properties
            #region Private Properties
                private  Hashtable _IDLookup = null;
                private static ArrayList _defaultTypes = null;
                private ArrayList _activeDescriptors = null;
                private ArrayList _noiseDescriptors = null;
                private ArrayList _rootDescriptors = null;
                private ArrayList _selectedDescriptor = null;
                private int _noiseThreshold = 0;
                private int _colorThreshold = 0;
                private ArrayList _customTypes = null;
                private ImageToShapeIDMapping _imageToShapeIDMapping = null;
                internal bool _isDirty = false;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Returns a copy off all descriptors (Active + Noise)
                /// </summary>
                /// <value></value>
                public DescriptorInfo[] AllDescriptors
                {
                    get
                    {
                        DescriptorInfo[] retVal = new DescriptorInfo[_activeDescriptors.Count + _noiseDescriptors.Count];
                        _activeDescriptors.CopyTo(retVal, 0);
                        _noiseDescriptors.CopyTo (retVal, _activeDescriptors.Count);
                        return retVal;
                    }
                }
                /// <summary>
                /// Returns all Descriptors greater than the noiseDescriptor Threshold
                /// </summary>
                /// <value></value>
                public ArrayList ActiveDescriptors
                {
                    get { return _activeDescriptors; }
                }
                /// <summary>
                /// Returns all Descriptors lower than the noiseDescriptor Threshold
                /// </summary>
                /// <value></value>
                public ArrayList NoiseDescriptors
                {
                    get { return _noiseDescriptors; }
                }
                /// <summary>
                /// Returns a copy of all top most descriptor (Depth = 0).
                /// </summary>
                /// <value></value>
                public ArrayList RootDescriptors
                {
                    get { return _rootDescriptors; }
                }
                /// <summary>
                /// Returns all descriptors selected (to be serialized)
                /// </summary>
                /// <value></value>
                public ArrayList SelectedDescriptors
                {
                    get { return _selectedDescriptor; }
                }
                /// <summary>
                /// Return all known types implementing IDescriptor and ICriterion
                /// </summary>
                /// <value></value>
                public Type[] KnownTypes
                {
                    get 
                    {
                        Type[] retVal = null;
                        Hashtable types = new Hashtable(_defaultTypes.Count + _customTypes.Count);
                        foreach(Type type in _defaultTypes)
                        {
                            if(types.Contains(type) == false)
                            {
                                types.Add(type, type);
                            }
                        }
                        foreach(Type type in _customTypes)
                        {
                            if(types.Contains(type) == false)
                            {
                                types.Add(type, type);
                            }
                        }
                        retVal = new Type[types.Count];
                        types.Keys.CopyTo(retVal, 0);
                        return retVal;
                    }
                }
                /// <summary>
                /// Get/set the noise threshold
                /// </summary>
                /// <value></value>
                public int NoiseThreshold
                {
                    get  { return _noiseThreshold; }
                    set 
                    {
                        if (value < 0) { throw new IndexOutOfRangeException("NoiseThreshold value must be strictly positive"); }
                        _noiseThreshold = value;
                    }
                }
                /// <summary>
                /// Get/set the color threshold
                /// </summary>
                /// <value></value>
                public int ColorThreshold
                {
                    get  { return _colorThreshold; }
                    set 
                    {
                        if (value < 0) { throw new IndexOutOfRangeException("ColorThreshold value must be strictly positive"); }
                        _colorThreshold = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private DescriptorManager()
            {
                _activeDescriptors = new ArrayList();
                _rootDescriptors = new ArrayList();
                _noiseDescriptors = new ArrayList();
                _selectedDescriptor = new ArrayList();
                _customTypes = new ArrayList();
                _IDLookup = new Hashtable();
                _colorThreshold = COLORTHRESHOLD;
                _noiseThreshold = NOISETHRESHOLD;
            }
            internal DescriptorManager(ImageToShapeIDMapping imageToShapeID) : this()
            {
                _imageToShapeIDMapping = imageToShapeID;
            }            
            internal DescriptorManager(ImageToShapeIDMapping imageToShapeID, Type[] customTypes) : this(imageToShapeID)
            {
                _customTypes.AddRange(customTypes);
            }
            static DescriptorManager()
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                Type[] allTypes = thisAssembly.GetTypes();

                _defaultTypes = new ArrayList();
                foreach (Type type in allTypes)
                {
                    if ( (type.IsAbstract == false) && 
                         (  (type.GetInterface(typeof(IDescriptor).ToString()) != null) || 
                            (type.GetInterface(typeof(ICriterion).ToString()) != null)
                         )
                       )
                    {
                        _defaultTypes.Add(type);
                    }
                }
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Post-processes the image by creating an array of descriptors for it.
                /// </summary>
                /// <param name="groupCount">Number of pixel groups identified.</param>
                private void PostProcess(int groupCount)
                {
                    DescriptorBuilder descrBld = new DescriptorBuilder(_imageToShapeIDMapping);

                    descrBld.GroupCount = groupCount;
                    descrBld.NoiseThreshold = _noiseThreshold;

                    _IDLookup = descrBld.Execute(typeof(CartesianMomentDescriptor));
                    _activeDescriptors = descrBld.Descriptors;
                    _noiseDescriptors = descrBld.NoiseDescriptors;
                    _rootDescriptors = descrBld.RootDescriptors;
                }
                private void WriteToStream(Stream stream, string textToWrite)
                {
                    byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(textToWrite);
                    stream.Write(buffer, 0, buffer.Length);
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Analyze the image and create the Descriptors
                /// </summary>
                public void Analyze()
                {
                    if (_imageToShapeIDMapping == null || _imageToShapeIDMapping.ImageSource == null)
                    {
                        throw new RenderingVerificationException("Cannot Analyze at this point, set the image to analyze first");
                    }
                    if (_imageToShapeIDMapping.ImageSource == null)
                    {
                        throw new RenderingVerificationException("No image set : nothing to analyze. Set the Image Property with a new IImageAdapter and try again.");
                    }

//                    DebugLog ("Building groups...");
                    GroupBuilder groupBuilder = new GroupBuilder(_imageToShapeIDMapping);

//                    groupBuilder.Feedback += this.Feedback;
                    groupBuilder.ColorThreshold = ColorThreshold;
                    groupBuilder.Execute();

                    // Compute descriptors.
                    PostProcess(groupBuilder.GroupCount);

//                    DebugLog ("Image analysis complete.");
                    _isDirty = false;
                }
                /// <summary>
                /// Serialize the SelectedDescriptors
                /// </summary>
                /// <returns></returns>
                public Stream Serialize()
                {
                    MemoryStream memoryStream = new MemoryStream();
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.AssemblyFormat = 0; //  FormatterAssemblyStyle.Simple; TODO-Miguep: conflicts with Newtonsoft.Json
                    formatter.Serialize(memoryStream, _selectedDescriptor);

                    return memoryStream;
                }
                /// <summary>
                /// Deserialize the stream into the SelectedDescriptors property
                /// </summary>
                /// <param name="stream"></param>
                public void Deserialize(Stream stream)
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    _selectedDescriptor.Clear();
                    _activeDescriptors.Clear();
                    formatter.AssemblyFormat = 0; // FormatterAssemblyStyle.Simple; TODO-Miguep: conflicts with Newtonsoft.Json
                    formatter.Binder = new ClientTestRuntimeBinder();
                    _selectedDescriptor.AddRange((ArrayList)formatter.Deserialize(stream));
                    _activeDescriptors.AddRange(_selectedDescriptor);
                    foreach(DescriptorInfo descriptorInfo in _selectedDescriptor)
                    {
                        _IDLookup[descriptorInfo.ID] = descriptorInfo;
                    }
                }
                /// <summary>
                /// Retrieve the descriptor at a specified position
                /// </summary>
                /// <param name="x"></param>
                /// <param name="y"></param>
                /// <returns></returns>
                public DescriptorInfo FindDescriptorAtPosition(int x, int y)
                {
                    if (x >= _imageToShapeIDMapping.ImageSource.Width || x < 0)
                    {
                        throw new ArgumentOutOfRangeException("x", x, "parameter passed in is out of bound, should be between 0 and " + (_imageToShapeIDMapping.ImageSource.Width - 1).ToString() + " (both included)");
                    }
                    if (y >= _imageToShapeIDMapping.ImageSource.Height || y < 0)
                    {
                        throw new ArgumentOutOfRangeException("y", y, "parameter passed in is out of bound, should be between 0 and " + (_imageToShapeIDMapping.ImageSource.Height - 1).ToString() + " (both included)");
                    }

                    int index = _imageToShapeIDMapping.ShapeID[y,x];
                    System.Diagnostics.Debug.Assert(_IDLookup.Contains(index));
                    return  (DescriptorInfo)_IDLookup[index];
                }
                /// <summary>
                /// Retrieve a list containing the descriptor closest to the one passed in.
                /// It will use the criteria of the descriptor passed in to determine the closest ones
                /// </summary>
                /// <param name="descriptorToFind">The descriptor to find</param>
                /// <returns></returns>
                public DescriptorInfo[] GetClosestDescriptors(DescriptorInfo descriptorToFind)
                {
                    KeepGreaterKey greaterDescriptors = new KeepGreaterKey();
                    // Retrieve the descriptors that passes one or more criterion
                    foreach(DescriptorInfo descriptorInfo in _activeDescriptors)
                    {
                        Hashtable criteriatypeDistanceMapping = new Hashtable();
                        Hashtable descriptorHashMapping = new Hashtable();
                        foreach(ICriterion criterion in descriptorToFind.IDescriptor.Criteria)
                        {
                            if (criterion.Pass(descriptorToFind.IDescriptor, descriptorInfo.IDescriptor))
                            {
                                criteriatypeDistanceMapping.Add(criterion.GetType(), criterion.DistanceBetweenDescriptors);
                            }
                        }
                        if (criteriatypeDistanceMapping.Count != 0)
                        {
                            // Store only if the number of passing criteria > previous count
                            descriptorHashMapping.Add(descriptorInfo, criteriatypeDistanceMapping);
                            greaterDescriptors.Add(criteriatypeDistanceMapping.Count, descriptorHashMapping);
                        }
                    }

                    // TODO : Find the smallest distance per criterion in every descriptor 

                    ArrayList retVal = new ArrayList();
                    Hashtable[] allDescriptorMappings = (Hashtable[])greaterDescriptors.ToArray(typeof(Hashtable));
                    foreach(Hashtable hash in allDescriptorMappings)
                    {
                        retVal.AddRange(hash.Keys);
                    }
                    return (DescriptorInfo[])retVal.ToArray(typeof(DescriptorInfo));
                }
                /// <summary>
                /// Determine if the descriptor pass all the criteria of the descriptor to find
                /// </summary>
                /// <param name="descriptorToFind">The descriptor to find in this DescriptorManger</param>
                /// <param name="infoTextWriter">The output to use for found / not found descriptor</param>
                /// <returns></returns>
                internal bool IsDescriptorMatch(DescriptorInfo descriptorToFind, TextWriter infoTextWriter)
                {
                    if (descriptorToFind == null || descriptorToFind.IDescriptor == null || descriptorToFind.IDescriptor.Criteria == null)
                    {
                        throw new ArgumentException("The parameter is invalid, could be null or no IDescriptor associated with it or no Criteria associated with the IDescriptor","descriptorToFind");
                    }
                    if (infoTextWriter == null)
                    {
                        throw new ArgumentException("The xmlTextWriter passed in is invalid, null or closed","infoTextWriter");
                    }
                    if (descriptorToFind.IDescriptor.Criteria.Length == 0) 
                    {
                        throw new RenderingVerificationException("The descriptor passed in does not contain any Criteria. Unable to compare."); 
                    }

                    bool descriptorMatch = false;
                    int failingCriteriaCount = 0;
                    SortedList closestDescriptors = new SortedList();
                    closestDescriptors.Add(int.MaxValue, new ArrayList());


                    foreach (DescriptorInfo descriptorTest in ActiveDescriptors)
                    {
                        failingCriteriaCount = 0;
                        foreach (Criterion criterion in descriptorToFind.IDescriptor.Criteria)
                        {
                            if(criterion.Pass(descriptorToFind.IDescriptor, descriptorTest.IDescriptor) == false)
                            {
                                failingCriteriaCount++;
                            }
                        }
                        if (failingCriteriaCount == 0)
                        {
                            descriptorMatch = true;
                        }

                        // if number of failing criteria <= current min, update data
                        if(failingCriteriaCount <= (int)closestDescriptors.GetKey(0))
                        {
                            ArrayList descriptorsList = (ArrayList)closestDescriptors.GetByIndex(0);
                            if (failingCriteriaCount != (int)closestDescriptors.GetKey(0))
                            {
                                descriptorsList.Clear();
                                closestDescriptors.RemoveAt(0);
                                closestDescriptors.Add(failingCriteriaCount, descriptorsList);
                            }
                            descriptorsList.Add(descriptorTest);
                        }
                    }

                    // Write info to Stream

                      infoTextWriter.Write(string.Format("<Descriptor {0}>", descriptorToFind.GetInfo()));
                        infoTextWriter.WriteLine("<CriteriaExpected>");
                            foreach (string criteriaInfo in descriptorToFind.GetCriteriaInfo())
                            {
                                infoTextWriter.WriteLine("<Criterion {0}/>", criteriaInfo);
                            }
                        infoTextWriter.WriteLine("</CriteriaExpected>");

                        infoTextWriter.WriteLine("<ClosestDescriptors>");
                            ArrayList list = ((ArrayList)closestDescriptors.GetByIndex(0));
                            foreach(DescriptorInfo descriptorInfo in list)
                            {
                                infoTextWriter.Write("<Descriptor {0}/>", descriptorInfo.GetInfo());
                                // TODO : Specify which criteria are failing and which one are passing .
                            }
                        infoTextWriter.WriteLine("</ClosestDescriptors>");
                      infoTextWriter.Write(string.Format("</Descriptor>"));
                    return descriptorMatch;
                }
                /// <summary>
                /// Replace the IDescriptor nested by the first DescriptorInfo by the second one
                /// </summary>
                /// <param name="descriptorToReplace">The DescriptorInfo containing the IDescriptor to be replaced</param>
                /// <param name="newDescriptor">The DescriptorInfo containing the replacing IDescriptor </param>
                public void ReplaceDescriptor(DescriptorInfo descriptorToReplace, DescriptorInfo newDescriptor)
                {
                    descriptorToReplace.ReplaceIDescriptor(newDescriptor.IDescriptor);
                }
            #endregion Public Methods
        #endregion Methods

        #region Classes used for building descriptors
            /// <summary>
            /// Encapsulates the algorithm to categorize pixels into
            /// groups.
            /// </summary>
            private class GroupBuilder
            {
                #region Properties
                    #region Private data.
                        private ArrayList activePixels;
                        private int groupCount;
                        private ImageToShapeIDMapping _imageToShapeIDMapping;
                        private int width;
                        private int height;
                        private int colorThreshold;
                    #endregion Private data.
                    #region Public data.
                        /// <summary>
                        /// Color threshold to identify pixels as belonging to a same
                        /// group.
                        /// </summary>
                        public int ColorThreshold
                        {
                            get
                            {
                                return this.colorThreshold;
                            }
                            set
                            {
                                if (value < 0 || value > 256)
                                {
                                    throw new ArgumentOutOfRangeException("GroupBuilder.ColorThreshold", "Value to be set must be between 0 and 256 (both included)");
                                }

                                this.colorThreshold = value;
                            }
                        }
                        /// <summary>
                        /// Number of different groups identified after execution.
                        /// </summary>
                        public int GroupCount
                        {
                            get { return this.groupCount; }
                        }
                    #endregion Public data.
                #endregion Properties

                #region Constructors
                    public GroupBuilder(ImageToShapeIDMapping imageToShapeIDMapping)
                    {
                        _imageToShapeIDMapping = imageToShapeIDMapping;
                        this.width = imageToShapeIDMapping.ImageSource.Width;
                        this.height = imageToShapeIDMapping.ImageSource.Height;
                    }
                #endregion Constructions

                #region Methods 
                    #region Private methods.
                        private void Log(string text)
                        {
                            if (Feedback != null)
                            {
                                Feedback(this, new FeedbackEventArgs(text));
                            }
                        }
                        /// <summary>
                        /// Aggregates pixels adjacent to the pixels in the 
                        /// active list into the given index.
                        /// </summary>
                        /// <param name="groupIndex">Index to aggregate to active indexes.</param>
                        private void AggregatePixelsForGroup(int groupIndex)
                        {
                            ArrayList pendingList = new ArrayList();

                            foreach (System.Drawing.Point p in activePixels)
                            {
                                int y = p.Y;
                                int x = p.X;

                                for (int i = -1; i <= 1; i++)
                                {
                                    for (int j = -1; j <= 1; j++)
                                    {
                                        int xDelta = i;
                                        int yDelta = j;

                                        if (xDelta != 0 || yDelta != 0)
                                        {
                                            int x2 = x + xDelta;
                                            int y2 = y + yDelta;

                                            if (ShouldGroupPixels(x, y, x2, y2))
                                            {
                                                this._imageToShapeIDMapping.ShapeID[y2, x2] = groupIndex;
                                                pendingList.Add(new System.Drawing.Point(x2, y2));
                                            }
                                        }
                                    }
                                }
                            }

                            this.activePixels = pendingList;
                        }
                        /// <summary>
                        /// Groups pixels based on homogeneity
                        /// </summary>
                        private void GroupPixels()
                        {
                            // Track feature elements by scanning the image and 
                            // aggregating new homogeneous regions
                            this.activePixels = new ArrayList();
                            this.groupCount = 0;
                            for (int j = 0; j < this.height; j++)
                            {
                                Log("GroupBuilder is grouping pixels (" + j + " of " + this.height + ")");
                                for (int i = 0; i < this.width; i++)
                                {
                                    // Create a new group from this pixel unless it
                                    // has already been assigned one.
                                    if (_imageToShapeIDMapping.ShapeID[j, i] < 0)
                                    {
                                        _imageToShapeIDMapping.ShapeID[j, i] = groupCount;
                                        activePixels.Add(new System.Drawing.Point(i, j));
                                        while (activePixels.Count > 0)
                                        {
                                            AggregatePixelsForGroup(groupCount);
                                        }

                                        groupCount++;
                                    }
                                }
                            }

                            this.activePixels = null;
                        }
                        /// <summary>
                        /// Get the Color value at the specified location
                        /// </summary>
                        /// <param name="x"></param>
                        /// <param name="y"></param>
                        /// <returns></returns>
                        private unsafe IColor GetPixel(int x, int y)
                        {
                            return _imageToShapeIDMapping.ImageSource[x, y];
                        }
                        /// <summary>
                        /// Verifies whether two pixels are similar enough to be grouped.
                        /// </summary>
                        private bool ShouldGroupPixels(int x, int y, int x2, int y2)
                        {
                            // Verify that the seconds pixel is in bounds and not yet
                            // assigned to a group.
                            if ((x2 >= 0) && (x2 < width) && (y2 >= 0) && (y2 < height) && (_imageToShapeIDMapping.ShapeID[y2, x2] < 0))
                            {
                                IColor lco = GetPixel(x, y);
                                IColor lcp = GetPixel(x2, y2);

                                // Addition of color channel simple distances. This calculation
                                // must be kept in sync with ListSampleColors.
                                int dist = Math.Abs(lco.R - lcp.R) + Math.Abs(lco.G - lcp.G) + Math.Abs(lco.B - lcp.B);

                                // If the color is sufficiently similar, group it.
                                return dist <= ColorThreshold;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    #endregion Private methods.
                    #region Public methods.
                        /// <summary>
                        /// Executes the algorithm and populates the result properties.
                        /// </summary>
                        public void Execute()
                        {
                            // Set the group preID
                            int negativeIndex = -1;

                            for (int j = 0; j < this.height; j++)
                            {
                                for (int i = 0; i < this.width; i++)
                                {
                                    _imageToShapeIDMapping.ShapeID[j, i] = negativeIndex;
                                    negativeIndex--;
                                }
                            }

                            GroupPixels();
                        }
                    #endregion Public methods.
                #endregion Methods 

                #region Events.
                    public event FeedbackEventHandler Feedback;
                #endregion Events.
            }
            /// <summary>
            /// Encapsulates the algorithm to create Descriptors from an image
            /// with grouped pixels.
            /// </summary>
            private class DescriptorBuilder
            {
                #region Properties
                    #region Private data.
                        private Hashtable _descriptorHash = null;
                        private Hashtable _relationSet = null;
                        private int groupCount;
                        private int noiseThreshold;
                        private ImageToShapeIDMapping _imageToShapeIDMapping = null;
                        private int width;
                        private int height;
                        private bool _mergeNoise = true;
                        private Hashtable _noiseDescriptorHash = null;
                    #endregion Private data.
                    #region Public data.
                        /// <summary>
                        /// Objects to describe image, populated after execution.
                        /// </summary>
                        public ArrayList Descriptors
                        {
                            get
                            {
                                ArrayList retVal = new ArrayList(_descriptorHash.Count);
                                retVal.AddRange(_descriptorHash.Values);
                                return retVal;
                            }
                        }
                        /// <summary>
                        /// Number of groups in the pixels.
                        /// </summary>
                        public int GroupCount
                        {
                            get { return this.groupCount; }
                            set { this.groupCount = value; }
                        }
                        /// <summary>
                        /// Array of descriptors removed as because they were
                        /// below the noise threshold.
                        /// </summary>
                        public ArrayList NoiseDescriptors
                        {
                            get 
                            {
                                ArrayList retVal = new ArrayList(_noiseDescriptorHash.Count);
                                retVal.AddRange(_noiseDescriptorHash.Values);
                                return retVal;
                            }
                        }
                        /// <summary>
                        /// Noise threshold for descriptor removal.
                        /// </summary>
                        public int NoiseThreshold
                        {
                            get
                            {
                                return this.noiseThreshold;
                            }
                            set
                            {
                                if (value < 0)
                                {
                                    throw new ArgumentOutOfRangeException("NoiseThreshold", "Value to be set must be positive (or zero)");
                                }

                                this.noiseThreshold = value;
                            }
                        }
                        /// <summary>
                        /// Get the top most descriptors (Depth = 0)
                        /// </summary>
                        /// <value></value>
                        public ArrayList RootDescriptors
                        {
                            get 
                            {
                                ArrayList retVal = new ArrayList();
                                foreach (DescriptorInfo di in Descriptors)
                                {
                                    if (di.Depth == 0)
                                    {
                                        retVal.Add(di);
                                    }
                                }
                                return retVal;
                            }
                        }
                        /// <summary>
                        /// Info about image and pixels to group
                        /// </summary>
                        /// <value></value>
                        public ImageToShapeIDMapping ImageToShapeIDMapping
                        {
                            get { return this._imageToShapeIDMapping; }
                            set { this._imageToShapeIDMapping = value; }
                        }
                        /// <summary>
                        /// Direct the engine to merge the noise with the closest descriptor or not.
                        /// </summary>
                        /// <value></value>
                        public bool MergeNoise
                        {
                            get { return _mergeNoise; }
                            set { _mergeNoise = value; }
                        }
                    #endregion Public data.
                #endregion Properties

                #region Constructors
                    public DescriptorBuilder(ImageToShapeIDMapping imageToShapeIDMapping)
                    {
                        System.Diagnostics.Debug.Assert(imageToShapeIDMapping != null, "ImageToShpaeIDMaaping object passd in is null !");
                        _descriptorHash = new Hashtable();
                        _noiseDescriptorHash = new Hashtable();
                        _imageToShapeIDMapping = imageToShapeIDMapping;
                        _relationSet = new Hashtable();
                    }
                #endregion Constructors

                #region Methods
                    #region Private methods.
                        private void Log(string text)
                        {
                            if (Feedback != null)
                            {
                                Feedback(this, new FeedbackEventArgs(text));
                            }
                        }
                        private void InitializeDescriptors(Type descriptorType)
                        {
                            Log("DescriptorBuilder is initializing descriptors...");
                            Log("<nbelm> " + groupCount);
                            _descriptorHash = new Hashtable(groupCount);

                            ConstructorInfo ci = descriptorType.GetConstructor(new Type[]{});

                            for (int j = 0; j < groupCount; j++)
                            {
                                IDescriptor defaultConstructor = (IDescriptor)descriptorType.InvokeMember(ci.Name, BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                                DescriptorInfo descriptorInfo = new DescriptorInfo (defaultConstructor);
                                descriptorInfo.ID = j;
                                _descriptorHash.Add(j, descriptorInfo);
                            }
                        }
                        /// <summary>
                        /// Assigns pixels to descriptors through groups.
                        /// </summary>
                        private void AssignPixelsToDescriptors()
                        {
                            // Remove pixels to avoid to add them twice (this API is called twice before and after RemoveNoise)
                            foreach (DescriptorInfo di in _descriptorHash.Values)
                            {
                                di.Pixels.Clear();
                            }

                            // Parse the image
                            Hashtable descriptorMapping = new Hashtable();
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    int groupIndex = ImageToShapeIDMapping.ShapeID[y, x];
                                    System.Diagnostics.Debug.Assert(groupIndex >= 0);
                                    System.Diagnostics.Debug.Assert(groupIndex < groupCount);

                                    DescriptorInfo descriptor = (DescriptorInfo)_descriptorHash[groupIndex];

                                    // Can be set to null by "RemoveNoise" function (when function called 2nd time)
                                    if (descriptor == null) { continue; }

                                    descriptor.AddParticipatingPixel(new Pixel(x,y,_imageToShapeIDMapping.ImageSource[x,y]));
                                    if(descriptorMapping.Contains(descriptor) == false)
                                    {
                                        descriptorMapping.Add(descriptor, descriptor);
                                    }
                                }
                            }

                            foreach (DescriptorInfo descriptor in descriptorMapping.Keys)
                            {
                                descriptor.SetParticipatingPixels();
                            }
                        }
                        /// <summary>
                        /// Filter noise by removing descriptors from groups (setting
                        /// to null).
                        /// </summary>
                        /// <returns>The number of descriptors removed</returns>
                        private int RemoveNoise()
                        {
                            Log("DescriptorBuilder is removing noise...");
                            int noiseCount = 0;
                            _noiseDescriptorHash.Clear();
                            // Cannot remove item when iterating, need to clone ( shallow or deep cloning BTW ? )
                            IDictionaryEnumerator dicoIter = ((Hashtable)_descriptorHash.Clone()).GetEnumerator();
                            DescriptorInfo descriptor = null;
                            while(dicoIter.MoveNext())
                            {
                                descriptor = (DescriptorInfo)dicoIter.Value;
                                if (descriptor.Pixels.Count < NoiseThreshold)
                                {
                                    _noiseDescriptorHash.Add(dicoIter.Key, descriptor);
                                    if (_mergeNoise) { MergeNoiseDescriptor(descriptor); }
                                    _descriptorHash.Remove(dicoIter.Key);
                                    noiseCount++;
                                }
                            }

                            Log("<rejected 'too small'>  " + noiseCount);
                            return noiseCount;
                        }
                        private void MergeNoiseDescriptor(DescriptorInfo noise)
                        {
                            if (noise.Pixels != null)
                            {
                                // Find the closest descriptor (closest luminance)
                                int[,] toexp = { { -1, 0 } , { 1, 0 } , { 0, -1 } , { 0, 1 } };

                                int closestDescriptorID = -1;
                                int minDistance = int.MaxValue;
                                for (int pidx = 0; pidx < noise.Pixels.Count; pidx++)
                                {
                                    uint x = (uint)((Pixel)noise.Pixels[pidx]).X;
                                    uint y = (uint)((Pixel)noise.Pixels[pidx]).Y;

                                    for (int tidx = 0; tidx <= toexp.GetUpperBound(0); tidx++)
                                    {
                                        int i = (int)(toexp[tidx, 0] + x);
                                        int j = (int)(toexp[tidx, 1] + y);

                                        if (i >= 0 && i < width && j >= 0 && j < height)
                                        {
                                            int keyID = _imageToShapeIDMapping.ShapeID[j, i];

                                            if (keyID != _imageToShapeIDMapping.ShapeID[y, x])
                                            {
                                                // compute the difference between the 2 luminances
                                                int dist = DeltaRGB(_imageToShapeIDMapping.ImageSource[i, j], _imageToShapeIDMapping.ImageSource[(int)x, (int)y]);

                                                if (dist <= minDistance)
                                                {
                                                    minDistance = dist;
                                                    closestDescriptorID = keyID;
                                                }
                                            }
                                        }
                                    } // end inner for
                                } // end outer for
                                if (closestDescriptorID != -1)
                                {
                                    DescriptorInfo closestDescriptor = (DescriptorInfo)_descriptorHash[closestDescriptorID];
                                    foreach (Pixel lpix in noise.Pixels)
                                    {
                                        _imageToShapeIDMapping.ShapeID[lpix.Y, lpix.X] = closestDescriptorID;
                                    }
                                    foreach (Pixel lpix in noise.AggregatedNoisePixels)
                                    {
                                        _imageToShapeIDMapping.ShapeID[lpix.Y, lpix.X] = closestDescriptorID;
                                    }
                                    closestDescriptor.AggregatedNoisePixels.AddRange(noise.Pixels);
                                    closestDescriptor.AggregatedNoisePixels.AddRange(noise.AggregatedNoisePixels);

                                    return;
                                }
                            }
                        }
                        private int DeltaRGB(IColor color1, IColor color2)
                        {
                            return Math.Abs(color1.R - color2.R) + Math.Abs(color1.G - color2.G) + Math.Abs(color1.B - color2.B);
                        }
                        private void ComputeDescriptors()
                        {
                            Hashtable pixelHash = new Hashtable();
                            Hashtable processedDescriptors = new Hashtable();
                            foreach (DescriptorInfo root in RootDescriptors)
                            {
                                AddSilhouettePixels(root, pixelHash, processedDescriptors);
                            }
                            IEnumerator iter = _descriptorHash.Values.GetEnumerator();
                            DescriptorInfo descriptorInfo = null;
                            // Compute each descriptor
                            while(iter.MoveNext())
                            {
                                descriptorInfo = (DescriptorInfo)iter.Current;
                                Pixel[] silhouetteExtraPixels = null;
                                if (pixelHash.Contains(descriptorInfo))
                                {
                                    silhouetteExtraPixels = (Pixel[])((ArrayList)pixelHash[descriptorInfo]).ToArray(typeof(Pixel));
                                }
                                descriptorInfo.IDescriptor.ComputeDescriptor(silhouetteExtraPixels);
                            }
                        }
                        private void AddSilhouettePixels(DescriptorInfo descriptor, Hashtable pixelHash, Hashtable processedDescriptor) //, SilhouetteDescriptorBuilder sdb)
                        {
                            // Start with most nested descriptors, recurse to get to it.
                            ArrayList childrenList = GetChildrenDescriptors(descriptor);
                            foreach (DescriptorInfo child in childrenList)
                            {
                                AddSilhouettePixels(child, pixelHash, processedDescriptor);
                            }

                            if (processedDescriptor.Contains(descriptor) == true) { return; }


                            ArrayList descriptorAdded = new ArrayList();
                            DescriptorInfo currentDescriptor = descriptor;
                            while (currentDescriptor.Depth > 0)
                            {
                                descriptorAdded.Add(currentDescriptor);
                                ArrayList aggregatedDescriptors = GetSiblingDescriptor(currentDescriptor);
                                ArrayList parents = GetParentDescriptor(currentDescriptor);
                                bool success = (aggregatedDescriptors.Count == 0 && parents.Count != 1) ? false: true;
                                foreach (DescriptorInfo sibling in aggregatedDescriptors)
                                {
                                    descriptorAdded.Add(sibling);
                                    foreach (DescriptorInfo siblingParent in GetParentDescriptor(sibling))
                                    {
                                        if (siblingParent != (DescriptorInfo)parents[0])
                                        {
                                            success = false;
                                        }
                                    }
                                }
                                if (success)
                                {
                                    if (pixelHash.Contains(parents[0]) == false) { pixelHash.Add(parents[0], new ArrayList()); }
                                    if (processedDescriptor.Contains(descriptor) == false)
                                    {
                                        ((ArrayList)pixelHash[parents[0]]).AddRange(descriptor.Pixels);
                                    }
                                }
                                currentDescriptor = (DescriptorInfo)parents[0];
                            }
                            processedDescriptor.Add(descriptor, null);
                        }
                        private ArrayList GetChildrenDescriptors(DescriptorInfo parentDescriptor)
                        {
                            ArrayList retVal = new ArrayList();
                            foreach(DescriptorInfo child in parentDescriptor.Neighbors)
                            {
                                if (child.Depth > parentDescriptor.Depth)
                                {
                                    retVal.Add(child);
                                }
                            }
                            return retVal;
                        }
                        private ArrayList GetParentDescriptor(DescriptorInfo childDescriptor)
                        {
                            ArrayList retVal = new ArrayList();
                            foreach (DescriptorInfo parent in childDescriptor.Neighbors)
                            {
                                if (parent.Depth < childDescriptor.Depth)
                                {
                                    retVal.Add(parent);
                                }
                            }
                            return retVal;

                        }
                        private ArrayList GetSiblingDescriptor(DescriptorInfo descriptor)
                        {
                            ArrayList retVal = new ArrayList();
                            foreach (DescriptorInfo sibling in descriptor.Neighbors)
                            {
                                if (sibling.Depth == descriptor.Depth)
                                {
                                    retVal.Add(sibling);
                                }
                            }
                            return retVal;

                        }
                        private ArrayList GetAllSubDescriptors(DescriptorInfo rootDescriptor)
                        {
                            ArrayList retVal = new ArrayList();
                            foreach (DescriptorInfo child in GetChildrenDescriptors(rootDescriptor))
                            {
                                retVal.Add(child);
                                retVal.AddRange(GetAllSubDescriptors(child));
                            }
                            return retVal;
                        }
                        private void BuildHierarchy()
                        {
                            // Find root descriptors
                            System.Drawing.Rectangle nonRoot = new System.Drawing.Rectangle(1, 1, _imageToShapeIDMapping.ImageSource.Width - 2, _imageToShapeIDMapping.ImageSource.Height - 2);
                            foreach(DescriptorInfo descriptor in _descriptorHash.Values)
                            {
                                RenderRect rect = descriptor.IDescriptor.BoundingBox;
                                System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
                                if(nonRoot.Contains(rectangle) == false)
                                {
                                    descriptor.Depth = 0;
                                }
                                else
                                {
                                    descriptor.Depth = -1;
                                }
                            }

                            // Establish a relation between descriptors
                            for(int y = 0; y < _imageToShapeIDMapping.ImageSource.Height; y++)
                            {
                                for(int x = 0; x < _imageToShapeIDMapping.ImageSource.Width; x++)
                                {
                                    RelateDescriptors (x, y, x + 1, y);
                                    RelateDescriptors (x, y, x, y + 1);
                                    RelateDescriptors (x, y, x + 1, y + 1);
                                    RelateDescriptors (x, y, x - 1, y + 1);
                                }
                            }
                            _relationSet.Clear();

                            // based on the relation, set the depth
                            foreach(DescriptorInfo root in RootDescriptors)
                            {
                                SetDepthRecurse(root);
                            }
                        }
                        private void RelateDescriptors(int x1, int y1, int x2, int y2)
                        {
                            int width = _imageToShapeIDMapping.ImageSource.Width;
                            int height = _imageToShapeIDMapping.ImageSource.Height;

                            System.Diagnostics.Debug.Assert(x1>=0 && y1>=0 && x1<width && y1<height);
                            if (x2 < 0 || y2 < 0 || x2 >= width || y2 >= height)
                            {
                                return;
                            }
                            int group1 =_imageToShapeIDMapping.ShapeID[y1, x1];
                            int group2 = _imageToShapeIDMapping.ShapeID[y2, x2];
                            if ( group1 == group2)
                            {
                                return;
                            }
                            DescriptorInfo d1 = (DescriptorInfo)_descriptorHash[group1];
                            DescriptorInfo d2 = (DescriptorInfo)_descriptorHash[group2];
                            if (d1 == null) { d1 = (DescriptorInfo)_noiseDescriptorHash[group1]; }
                            if (d2 == null) { d2 = (DescriptorInfo)_noiseDescriptorHash[group2]; }


                            if(d1.ID < d2.ID)
                            {
                                DescriptorInfo swap = d1;
                                d1 = d2;
                                d2 = swap;
                            }

                            string id = d1.ID.ToString () + "_" + d2.ID.ToString();
                            if (_relationSet.Contains(id) == false)
                            {
                                _relationSet.Add(id, id);
                                d1.AddRelation(d2);
                            }
                        }
                        private void SetDepthRecurse(DescriptorInfo descriptor)
                        {
                            foreach(DescriptorInfo node in descriptor.Neighbors)
                            {
                                if (node.Depth == -1 || node.Depth > descriptor.Depth + 1)
                                {
                                    node.Depth = descriptor.Depth + 1;
                                    SetDepthRecurse(node);
                                }
                            }
                        }
                    #endregion Private methods.
                    #region Public methods.
                        /// <summary>
                        /// Calculates descriptors for the image and populates the result
                        /// properties.
                        /// </summary>
                        public Hashtable Execute(Type descriptorType)
                        {
                            this.height = _imageToShapeIDMapping.ImageSource.Height;
                            this.width = _imageToShapeIDMapping.ImageSource.Width;
                            InitializeDescriptors(descriptorType);

                            // Assign pixels to primitive
                            AssignPixelsToDescriptors();

                            // Aggregate tiny primitive with main shape
                            int noiseCount = RemoveNoise();

                            if (_mergeNoise)
                            {
                                // Assign fully filtered pixels to primitive
                                AssignPixelsToDescriptors();
                            }

                            BuildHierarchy();
                            ComputeDescriptors();

                            return _descriptorHash;
                        }
                    #endregion Public methods.
                #endregion Methods

                #region Events.
                    public event FeedbackEventHandler Feedback;
                #endregion Events.
            }
            /// <summary>
            /// This is needed because of the source refactoring (sources included in Loader and UI)
            /// Method will try to locate the type in the Executing assembly then default to assembly passed-in if not found.
            /// </summary>
            private class ClientTestRuntimeBinder : System.Runtime.Serialization.SerializationBinder
            {
                public override Type BindToType(string assemblyName, string typeName)
                {
                    Type type = Assembly.GetExecutingAssembly().GetType(typeName, false, true);
                    if (type == null) { type = Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), false, true); }
                    return type;
                }
            }
        #endregion Private classes used internally
    }
}
