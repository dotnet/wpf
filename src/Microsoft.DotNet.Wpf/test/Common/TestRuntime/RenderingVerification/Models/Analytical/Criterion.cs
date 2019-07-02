// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical.Criteria
{
    #region usings
        using System;
        using System.Runtime.Serialization;
        using System.Text.RegularExpressions;
    #endregion usings

    /// <summary>
    /// Base class for Criterion. Cannot be instantiated.
    /// </summary>
    [SerializableAttribute()]
    public abstract class Criterion : ICriterion, ISerializable
    {
        #region Properties
            #region Private properties
                /// <summary>
                /// The value of the criterion
                /// </summary>
                private object _value = null; // Marked as public for serialization purposes
                private double _descriptorDistance = double.NaN;
                private int _weight = 1;
            #endregion Private properties
            #region Public properties
                /// <summary>
                /// Name of the criterion
                /// </summary>
                /// <value></value>
                public abstract string Name {get;}
                /// <summary>
                /// Self Description of this criterion
                /// </summary>
                /// <value></value>
                public abstract string Description {get;}
                /// <summary>
                /// Type to be passed as value
                /// </summary>
                /// <value></value>
                public abstract Type ExpectedValueType {get;}
                /// <summary>
                /// Value of the Criterion
                /// </summary>
                /// <value></value>
                public virtual object Value
                {
                    get
                    {
                        return _value;
                    }
                    set
                    {
                        CheckValueParam(value, ExpectedValueType);
                        _value = value;
                    }
                }
                /// <summary>
                /// Get/set the weight of this criteria; used for classifiying closest criteria (i.e. : Shape more important than position)
                /// </summary>
                /// <value></value>
                public virtual int Weight
                {
                    get { return _weight; }
                    set { _weight = value; }
                }
                /// <summary>
                /// Returns the value resulting in computing the difference bewtween two descriptors.
                /// Note :  a RenderingVerificationException will be thrown if you try to call this Property before calling the "Pass" Method
                /// </summary>
                /// <value></value>
                public double DistanceBetweenDescriptors
                {
                    get
                    {
                        return _descriptorDistance;
                    }
                    set 
                    {
                        _descriptorDistance = value;
                    }
                }
            #endregion Public properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of the derived type -- needed for serialization, called by the formatter on deserialization
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected Criterion(SerializationInfo info, StreamingContext context)
            {
                Type valueType = (Type)info.GetValue("valueType", typeof(Type));
                _value = info.GetValue("Value", valueType);
                _descriptorDistance = double.NaN;
                _weight = (int)info.GetValue("Weight", _weight.GetType());
            }
            /// <summary>
            /// Create a new instance of the derived type with specified value
            /// </summary>
            /// <param name="initValue"></param>
            protected Criterion(object initValue)
            {
                Value = initValue;
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public abstract bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest);
            private void CheckValueParam(object valueParam, Type expectedType)
            {
                if (valueParam == null)
                {
                    throw new ArgumentNullException("Value", "Cannot pass null as a parameter");
                }
                if (valueParam.GetType() != expectedType)
                {
                    throw new InvalidCastException("The Value passed in must be of type " + expectedType.ToString() + " (type '" + valueParam.GetType().ToString() + "' passed in)");
                }
            }
            /// <summary>
            /// Retrieve a depend object after checking for its existance
            /// Note : Will throw a RenderingVerificationException if the key cannot be found in this IDescriptor type
            /// </summary>
            /// <param name="descriptor">The IDescriptor to query </param>
            /// <param name="key">the Key to retrieve</param>
            /// <returns>The associated value</returns>
            protected object GetDependendObjects(IDescriptor descriptor, string key)
            {
                if (descriptor.DescriptorDependentObjects.Contains(key.Trim().ToLower()) == false)
                {
                    throw new RenderingVerificationException("This descriptor ('" + descriptor.GetType().ToString() + "') does not support this criterion ('" + this.GetType().ToString() + "')");
                }
                return descriptor.DescriptorDependentObjects[key];
            }
            /// <summary>
            /// Implement ISerializable unique method -- needed for serialization, called by the formatter on serialization
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("valueType", _value.GetType());
                info.AddValue("Value", _value);
                info.AddValue("Weight", _weight);
            }
        #endregion Methods
    }

    /// <summary>
    /// The silhouette criterion, compare using the external perimeter only
    /// </summary>
    [SerializableAttribute()]
    public sealed class SilhouetteCriterion : Criterion
    {
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _name = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _description = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private Type _expectedValueType = null;

        #region Constructors
            /// <summary>
            /// Instantiate a new "Texture" criterion
            /// </summary>
            private SilhouetteCriterion(SerializationInfo info, StreamingContext context) : base(info, context) { }
            /// <summary>
            /// Instantiate a new "Texture" criterion with the specified percentage diff
            /// </summary>
            /// <param name="differencePercentageTolerance">The acceptable tolerance (normalized, between 0 and 1)</param>
            public SilhouetteCriterion(double differencePercentageTolerance) : base (differencePercentageTolerance) { }
            static SilhouetteCriterion()
            {
                _name = "SilhouetteCriterion";
                _description = "Match 2 IDescriptor based on their silhouette (regardless of the scaling)";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                double dist = 0.0;

                DescriptorSquareMatrix silhouetteToFind = (DescriptorSquareMatrix)GetDependendObjects(descriptorToFind, "silhouette");
                DescriptorSquareMatrix silhouetteTest = (DescriptorSquareMatrix)GetDependendObjects(descriptorTest, "silhouette");
                // BUGBUG : because of implemented algorithm, using this might lead to diff over 100% !
                // I guess that's ok since the image would be extremely different anyway.
                for (int x = 0; x < DescriptorSquareMatrix.Length; x++)
                {
                    for (int y = 0; y < DescriptorSquareMatrix.Length; y++)
                    {
                        dist += (double)Math.Abs(silhouetteToFind[x, y] - silhouetteTest[x, y]);
                    }
                }
/*
                // BUGBUG : because of implemented algorithm, using this might lead to diff over 100% !
                // I guess that's ok since the image would be extremely different anyway.
                for (int x = 0; x < DescriptorSquareMatrix.Length; x++)
                {
                    for (int y = 0; y < DescriptorSquareMatrix.Length; y++)
                    {
                        dist += (double)Math.Abs(descriptorToFind.Silhouette[x, y] - descriptorTest.Silhouette[x, y]);
                    }
                }
*/
                DistanceBetweenDescriptors = dist;
                return (dist > (double)Value ? false : true);
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class ShapeCriterion: Criterion
    {
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _name = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _description = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private Type _expectedValueType = null;

        #region Constructors
            /// <summary>
            /// Instantiate a new "Shape" criterion
            /// </summary>
            private ShapeCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "Shape" criterion with the specified percentage diff
            /// </summary>
            /// <param name="differencePercentageTolerance">The acceptable tolerance (normalized, between 0 and 1)</param>
            public ShapeCriterion(double differencePercentageTolerance) : base (differencePercentageTolerance) {}
            static ShapeCriterion()
            {
                _name = "ShapeCriterion";
                _description = "Match 2 IDescriptor based on their shape (regardless of the scaling)";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                double dist = 0.0;

                DescriptorSquareMatrix shapeToFind = (DescriptorSquareMatrix)GetDependendObjects(descriptorToFind, "shape");
                DescriptorSquareMatrix shapeTest = (DescriptorSquareMatrix)GetDependendObjects(descriptorTest, "shape");

                // BUGBUG : because of implemented algorithm, using this might lead to diff over 100% !
                // I guess that's ok since the image would be extremely different anyway.
                for (int x = 0; x < DescriptorSquareMatrix.Length; x++)
                {
                    for (int y = 0; y < DescriptorSquareMatrix.Length; y++)
                    {
                        dist += (double)Math.Abs(shapeToFind[x, y] - shapeTest[x, y]);
                    }
                }

                DistanceBetweenDescriptors = dist;
                return (dist > (double)Value ? false : true);
            }
        #endregion Properties
    }

    /// <summary>
    /// The Texture criterion, compare using the Shape and the color of the pixels in the shape
    /// </summary>
    [SerializableAttribute()]
    public sealed class TextureCriterion: Criterion
    {
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _name = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private string _description = string.Empty;
        /// <summary>
        /// This member is internal
        /// </summary>
//        [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
        static private Type _expectedValueType = null;

        #region Constructors
            /// <summary>
            /// Instantiate a new "Texture" criterion
            /// </summary>
            private TextureCriterion(SerializationInfo info, StreamingContext context) : base(info, context) { }
            /// <summary>
            /// Instantiate a new "Texture" criterion with the specified percentage diff
            /// </summary>
            /// <param name="differencePercentageTolerance">The acceptable tolerance (normalized, between 0 and 1)</param>
            public TextureCriterion(double differencePercentageTolerance) : base (differencePercentageTolerance) { }
            static TextureCriterion()
            {
                _name = "TextureCriterion";
                _description = "Match 2 IDescriptor based on their shape and color (regardless of the scaling)";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                double dist = 0.0;

                DescriptorSquareMatrix textureToFind = (DescriptorSquareMatrix)GetDependendObjects(descriptorToFind, "texture");
                DescriptorSquareMatrix textureTest = (DescriptorSquareMatrix)GetDependendObjects(descriptorTest, "texture");

                // BUGBUG : because of implemented algorithm, using this might lead to diff over 100% !
                // I guess that's ok since the image would be extremely different anyway.
                for (int x = 0; x < DescriptorSquareMatrix.Length; x++)
                {
                    for (int y = 0; y < DescriptorSquareMatrix.Length; y++)
                    {
                        dist += (double)Math.Abs(textureToFind[x, y] - textureTest[x, y]);
                    }
                }

                DistanceBetweenDescriptors = dist;
                return (dist > (double)Value ? false : true);
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class XPositionCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private  string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private  Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            /// Instantiate a new "xPosition" criterion.
            /// </summary>
            private XPositionCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "xPosition" criterion with the specified tolerance
            /// </summary>
            /// <param name="xAxisOffsetTolerance">The acceptable tolerance (number of pixels)</param>
            public XPositionCriterion(double xAxisOffsetTolerance) : base (xAxisOffsetTolerance) {}
            static XPositionCriterion()
            {
                _name = "XPositionCriterion";
                _description = "Match 2 IDescriptor based on their abscisse location -- use the top/left position of the descriptor";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                DistanceBetweenDescriptors = (double)Math.Abs(descriptorToFind.BoundingBox.Left - descriptorTest.BoundingBox.Left);
                return (DistanceBetweenDescriptors > (double)Value) ? false : true;
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class YPositionCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            /// Instantiate a new "yPosition" criterion.
            /// </summary>
            private YPositionCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "yPosition" criterion with the specified tolerance
            /// </summary>
            /// <param name="yAxisOffsetTolerance">The acceptable tolerance (in pixels)</param>
            public YPositionCriterion(double yAxisOffsetTolerance) : base (yAxisOffsetTolerance) {}
            static YPositionCriterion()
            {
                _name = "YPositionCriterion";
                _description = "Match 2 IDescriptor based on their yAxis location -- use the top/left position of the descriptor";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                DistanceBetweenDescriptors = (double)Math.Abs(descriptorToFind.BoundingBox.Top - descriptorTest.BoundingBox.Top);
                return ((double)DistanceBetweenDescriptors > (double)Value) ? false : true;
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class ScalingUpCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            ///Instantiate a new "scalingUp" criterion.
            /// </summary>
            private ScalingUpCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "scalingUp" criterion with the specified tolerance
            /// </summary>
            /// <param name="maxScalingUpPercentageTolerance">The acceptable tolerance (percentage -- normalized, between 0.0 and 1.0)</param>
            public ScalingUpCriterion(double maxScalingUpPercentageTolerance) : base (maxScalingUpPercentageTolerance) {}
            static ScalingUpCriterion()
            {
                _name = "ScalingUpCriterion";
                _description = "Match 2 IDescriptor based on the scaling (how big can the found descriptor can be relatively to the original one)";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                System.Drawing.Size sizeFind = new System.Drawing.Size(descriptorToFind.BoundingBox.Right - descriptorToFind.BoundingBox.Left, descriptorToFind.BoundingBox.Right - descriptorToFind.BoundingBox.Left);
                System.Drawing.Size sizeTest = new System.Drawing.Size(descriptorTest.BoundingBox.Right - descriptorTest.BoundingBox.Left, descriptorTest.BoundingBox.Right - descriptorTest.BoundingBox.Left);
                DistanceBetweenDescriptors = Math.Max((double)sizeTest.Width / sizeFind.Width, (double)sizeTest.Height / sizeFind.Height) - 1.0;
                return ((double)DistanceBetweenDescriptors > (double)Value * 100.0) ? false : true;
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class ScalingDownCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            /// Instantiate a new "scalingDown" criterion
            /// </summary>
            private ScalingDownCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "scalingDown" criterion with the specified tolerance
            /// </summary>
            /// <param name="maxScalingDownPercentageTolerance">The acceptable tolerance (percentage -- normalized, between 0.0 and 1.0)</param>
            public ScalingDownCriterion(object maxScalingDownPercentageTolerance) : base (maxScalingDownPercentageTolerance) {}
            static ScalingDownCriterion()
            {
                _name = "ScalingDownCriterion";
                _description = "Match 2 IDescriptor based on the scaling (how small can the found descriptor can be relatively to the original one)";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                System.Drawing.Size sizeFind = new System.Drawing.Size(descriptorToFind.BoundingBox.Right - descriptorToFind.BoundingBox.Left, descriptorToFind.BoundingBox.Bottom - descriptorToFind.BoundingBox.Top);
                System.Drawing.Size sizeTest = new System.Drawing.Size(descriptorTest.BoundingBox.Right - descriptorTest.BoundingBox.Left, descriptorTest.BoundingBox.Bottom - descriptorTest.BoundingBox.Top);
                DistanceBetweenDescriptors = Math.Max((double)sizeFind.Width / sizeTest.Width, (double)sizeFind.Height / sizeTest.Height) - 1.0;
                return ((double)DistanceBetweenDescriptors > (double)Value * 100.0) ? false : true;
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
    [SerializableAttribute()]
    public sealed class ColorAverageCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static private Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            /// Instantiate a new "colorAverage" criterion.
            /// </summary>
            private ColorAverageCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "colorAverage" criterion with the specified tolerance
            /// </summary>
            /// <param name="colorAverage">The amount (per channel) it can derive from the average color (normalized, usually channels between 0.0 and 1.0)</param>
            public ColorAverageCriterion(ColorDouble colorAverage) : base (colorAverage) {}
            static ColorAverageCriterion()
            {
                _name = "ColorAverageCriterion";
                _description = "Match 2 IDescriptor based on their color average";
                _expectedValueType = typeof(ColorDouble);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
//                throw new NotImplementedException("Need to implement this ASAP");
                IColor colorFind = (IColor)descriptorToFind.DescriptorDependentObjects["shapecoloraverage"];
                IColor colorTest = (IColor)descriptorTest.DescriptorDependentObjects["shapecoloraverage"];
                IColor colorDiff  = new ColorDouble();

                colorDiff.ExtendedAlpha = Math.Abs(colorFind.ExtendedAlpha - colorTest.ExtendedAlpha);
                colorDiff.ExtendedRed = Math.Abs(colorFind.ExtendedRed - colorTest.ExtendedRed);
                colorDiff.ExtendedGreen = Math.Abs(colorFind.ExtendedGreen - colorTest.ExtendedGreen);
                colorDiff.ExtendedBlue = Math.Abs(colorFind.ExtendedBlue - colorTest.ExtendedBlue);

                DistanceBetweenDescriptors = colorDiff.ExtendedAlpha + colorDiff.ExtendedRed + colorDiff.ExtendedGreen + colorDiff.ExtendedBlue;

                if (colorDiff.ExtendedAlpha > ((IColor)Value).ExtendedAlpha ||
                     colorDiff.ExtendedRed > ((IColor)Value).ExtendedRed ||
                     colorDiff.ExtendedGreen > ((IColor)Value).ExtendedGreen ||
                     colorDiff.ExtendedBlue > ((IColor)Value).ExtendedBlue
                   )
                {
                    return false;
                }

                return true;
            }
        #endregion Properties
    }

    /// <summary>
    /// Summary description for Criteria2.
    /// </summary>
//    [SerializableAttribute()]
    [ObsoleteAttribute("Experimental criterion, do not use yet")]
    internal class RotationCriterion: Criterion
    {
        #region Static private properties
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static protected internal string _name = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static protected internal string _description = string.Empty;
            /// <summary>
            /// This member is internal
            /// </summary>
//            [CLSCompliantAttribute(false)]  // Needed because razzle complains (although this is not marked as public)
            static protected internal Type _expectedValueType = null;
        #endregion Static private properties

        #region Constructors
            /// <summary>
            /// Instantiate a new "rotation" criterion.
            /// </summary>
            protected RotationCriterion(SerializationInfo info, StreamingContext context) : base(info, context) {}
            /// <summary>
            /// Instantiate a new "rotation" criterion with the specified tolerance
            /// </summary>
            /// <param name="angle">the value of the angle</param>
            public RotationCriterion(double angle) : base (angle) {}
            static RotationCriterion()
            {
                _name = "RotationCriterion";
                _description = "TO BE DONE";
                _expectedValueType = typeof(double);
            }
        #endregion Constructors

        #region ICriterion Implementation (abstract member on Criterion class)
            /// <summary>
            /// Name of the criterion
            /// </summary>
            public override string Name
            {
                get
                {
                    return _name;
                }
            }
            /// <summary>
            /// Self Description of this criterion
            /// </summary>
            /// <value></value>
            public override string Description
            {
                get
                {
                    return _description;
                }
            }
            /// <summary>
            /// Type to be passed as value
            /// </summary>
            /// <value></value>
            public override Type ExpectedValueType
            {
                get { return _expectedValueType; }
            }
            /// <summary>
            /// Check if two descriptor are matching, using this criterion
            /// </summary>
            /// <param name="descriptorToFind">The original descriptor</param>
            /// <param name="descriptorTest">The descriptor to test</param>
            /// <returns>true if within tolerance for this criterion, false otherwise</returns>
            public override bool Pass(IDescriptor descriptorToFind, IDescriptor descriptorTest)
            {
                object angle = GetDependendObjects(descriptorToFind, "rotation");
                throw new NotImplementedException("Rotation not implemented yet. To be done if useful to user");
            }
        #endregion Properties
    }

}
