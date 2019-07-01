// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// Data that describes a test.
    /// </summary>
    [Serializable()]
    [ObjectSerializerAttribute(typeof(FastObjectSerializer))]
    public class TestInfo : ICloneable
    {
        private int? hashCode = null;
        private int HashCode
        {
            get
            {
                if (hashCode == null)
                {
                    hashCode = GetDeterministicHashCode();
                }
                return (int)hashCode;
            }
        }

        #region Public Members

        //Required Metadata

        /// <summary>
        /// Name of the testcase. Should be unique.
        /// </summary>
        [XmlAttribute()]
        public string Name { get; set; }

        /// <summary>
        /// Priority of a test.
        /// </summary>
        [XmlAttribute()]
        public int? Priority { get; set; }

        /// <summary>
        /// Name of the product.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        [XmlAttribute()]
        //[Obsolete("Remove this if possible. This is not used outside of discovery", false)]
        public string Product { get; set; }

        /// <summary>
        /// Name of the Area.
        /// </summary>
        [XmlAttribute()]
        public string Area { get; set; }

        /// <summary>
        /// Name of the Sub-Area
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        [XmlAttribute()]
        public string SubArea { get; set; }

        /// <summary>
        /// Defines a set of variations that can be run as part of one execution unit. 
        /// An execution group can have N variations, but a variation can only belong to one execution group.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        [XmlAttribute()]
//        [Obsolete("Remove this if possible. This is not used outside of discovery", false)]
        public string ExecutionGroup { get; set; }

        /// <summary>
        /// Specifies the desired level of execution grouping optimizations to
        /// apply for the test.
        /// </summary>
        [XmlAttribute()]
        public ExecutionGroupingLevel? ExecutionGroupingLevel { get; set; }

        /// <summary>
        /// Specifies the UAC elevation to give to a test. In most cases, the default should be used.
        /// </summary>
        [XmlAttribute()]
        public TestUacElevation? UacElevation { get; set; }

        //TODO: Switch to DriverPath
        /// <summary>
        /// Information on the driver which will be used to execute the test
        /// </summary>
        public TestDriverInfo Driver { get; set; }

        /// <summary>
        /// A string dictionary that is to be used by the driver to launch
        /// the test.
        /// </summary>
        [MergableProperty(true)]
        public ContentPropertyBag DriverParameters { get; set; }

        /// <summary>
        /// Collection of versions relative to the Product which this test belongs to
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(false)]
        public Collection<string> Versions { get; set; }

        /// <summary>
        /// Specifies the category of a test.
        /// </summary>
        [XmlAttribute()]
        [SuppressMessage("Microsoft.Naming", "CA1721")]
        public TestType Type { get; set; }

        //Optional Metadata

        /// <summary>
        /// Description of the testcase
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Collection of support files needed for this testcase
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(true)]
        public Collection<TestSupportFile> SupportFiles { get; set; }

        /// <summary>
        /// Collection of deployments files needed by this testcase.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(true)]
        public Collection<string> Deployments { get; set; }

        ///// <summary>
        ///// Dictionary of key/value strings that can be used to categorize a test
        ///// </summary>
        //[MergableProperty(true)]
        //// Disabled by Pantal - Any code depending on this var needs to be fixed.
        //public PropertyBag Variables { get; set; }
        
        /// <summary>
        /// Set to True to ensure this testcase doesn't get executed
        /// </summary>
        [XmlAttribute()]
        public bool? Disabled { get; set; }

        /// <summary>
        /// Timeout for TestInfo.
        /// </summary>
        [XmlAttribute]
        [TypeConverter(typeof(TestInfo.TimeSpanConverter))]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Collection of supported run configurations for tests. 
        /// If the constraints for any of the specified Configuration files match the run's configuration, the test will allowed to run.
        /// When left unspecified, the actual machine configuration has no effect on test filtering.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(true)]
        public Collection<string> Configurations { get; set; }

        //TODO: Remove this if possible. This is not consumed outside of discovery.
        /// <summary>
        /// Collection of bugs associated with this test case.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(false)]
        public Collection<Bug> Bugs { get; set; }

        /// <summary>
        /// Collection of keywords associated with this test
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [MergableProperty(true)]
        public Collection<string> Keywords { get; set; }

        /// <summary/>
        public TestInfo Clone()
        {
            // NOTE:
            // Cloning the Name implies it's going to be discovered as the same
            // test and therefore it is the responsibility of the discovery
            // adaptor to ensure TestInfos it returns have had unique names set.
            TestInfo test = (TestInfo)this.MemberwiseClone();

            // MemberwiseClone will make copies of simple types but will
            // only copy references to objects, therefore we need to clone
            // and set the member object manually.

            if (Deployments != null)
            {
                test.Deployments = new Collection<string>();
                foreach (string deployment in Deployments)
                {
                    test.Deployments.Add(deployment);
                }
            }
            if (Driver != null)
            {
                test.Driver = Driver.Clone();
            }
            if (SupportFiles != null)
            {
                test.SupportFiles = new Collection<TestSupportFile>();
                foreach (TestSupportFile supportFile in SupportFiles)
                {
                    test.SupportFiles.Add((TestSupportFile)supportFile.Clone());
                }
            }
            if (Configurations != null)
            {
                test.Configurations = new Collection<string>();
                foreach (string configuration in Configurations)
                {
                    test.Configurations.Add(configuration);
                }
            }            
            if (Versions != null)
            {
                test.Versions = new Collection<string>();
                foreach (string version in Versions)
                {
                    test.Versions.Add(version);
                }
            }
            if (DriverParameters != null)
            {
                test.DriverParameters = (ContentPropertyBag)((ICloneable)DriverParameters).Clone();
            }
            if (Bugs != null)
            {
                test.Bugs = new Collection<Bug>();
                foreach (Bug bug in Bugs)
                {
                    test.Bugs.Add((Bug)bug.Clone());
                }
            }
            if (Keywords != null)
            {
                test.Keywords = new Collection<string>();
                foreach (string keyword in Keywords)
                {
                    test.Keywords.Add(keyword);
                }
            }

            return test;
        }

        /// <summary>
        /// Merge values from the specified TestInfo into the current TestInfo.
        /// </summary>
        /// <param name="testInfoToMergeFrom">TestInfo to copy values from.</param>
        public void Merge(TestInfo testInfoToMergeFrom)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(typeof(TestInfo)))
            {
                // All TestInfo properties default to null (value types are nullables),
                // so any property that is not null has been explicitly set.
                if (property.GetValue(testInfoToMergeFrom) != null)
                {
                    // MergableProperty is an attribute that specifies whether collections can be combined.
                    if (IsMergableProperty(property) != null)
                    {
                        // Since all properties on TestInfo default to null, we need to make
                        // sure to instantiate the property type if necessary.
                        if (property.GetValue(this) == null)
                        {
                            property.SetValue(this, Activator.CreateInstance(property.PropertyType));
                        }

                        // If the property is not mergable, we need to clear out the collection first.
                        if (!IsMergableProperty(property).Value)
                        {
                            MethodInfo clearMethod = property.PropertyType.GetMethod("Clear");
                            clearMethod.Invoke(property.GetValue(this), null);
                        }

                        if (property.PropertyType == typeof(PropertyBag) || property.PropertyType == typeof(ContentPropertyBag))
                        {
                            MergePropertyBags((PropertyBag)property.GetValue(this), (PropertyBag)property.GetValue(testInfoToMergeFrom));
                        }
                        else if (property.PropertyType.GetInterface("ICollection`1") != null)
                        {
                            // Because we're wanting to invoke a generic method we have to do some reflection
                            // work to invoke MergeCollections<T>.
                            MethodInfo mergeMethod = typeof(TestInfo).GetMethod("MergeCollections", BindingFlags.NonPublic | BindingFlags.Static);
                            MethodInfo mergeMethodConstructed = mergeMethod.MakeGenericMethod(property.PropertyType.GetGenericArguments()[0]);
                            mergeMethodConstructed.Invoke(null, new object[] { property.GetValue(this), property.GetValue(testInfoToMergeFrom) });
                        }
                        else
                        {
                            throw new NotImplementedException("Merge only supports collections of type PropertyBag or ICollection<T>.");
                        }
                    }
                    // There was no MergableProperty attribute on the property, so we simply set the property.
                    else
                    {
                        property.SetValue(this, property.GetValue(testInfoToMergeFrom));
                    }
                }
            }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Simply performs literal equality test - nothing more.
        /// </summary>        
        public override bool Equals(object obj)
        {
            return Object.Equals(this, obj);
        }

        /// <summary>
        /// Provides the hashcode of the object.
        /// This is used for indexing on hashing operations (ie- dictionaries)
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode;                  
        }


        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(TestInfo first, TestInfo second)
        {
            if (object.Equals(first, null))
            {
                return object.Equals(second, null);
            }
            else
            {
                return first.Equals(second);
            }
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator !=(TestInfo first, TestInfo second)
        {
            if (object.Equals(first, null))
            {
                return !object.Equals(second, null);
            }
            else
            {
                return !first.Equals(second);
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Wraps searching and returning the value of the MergableProperty attribute on a property.
        /// </summary>
        /// <param name="property">Property to search for attribute value on.</param>
        /// <returns>MergableProperty.AllowMerge value, or null if there was no MergableProperty attribute specified.</returns>
        private static bool? IsMergableProperty(PropertyDescriptor property)
        {
            foreach (Attribute attribute in property.Attributes)
            {
                if (attribute is MergablePropertyAttribute)
                {
                    return ((MergablePropertyAttribute)attribute).AllowMerge;
                }
            }

            return null;
        }

        /// <summary>
        /// Merge the contents of two property bags together. Due to PropertyBag
        /// semantics, any key/value pair from the source PropertyBag will overwrite
        /// the value for a key/value pair in the target PropertyBag with the same key.
        /// </summary>
        /// <param name="propertyBagToMergeInto">Target PropertyBag.</param>
        /// <param name="propertyBagToMergeFrom">Source ProeprtyBag.</param>
        private static void MergePropertyBags(PropertyBag propertyBagToMergeInto, PropertyBag propertyBagToMergeFrom)
        {
            IEnumerable<KeyValuePair<string, string>> ienumerable = (IEnumerable<KeyValuePair<string, string>>)propertyBagToMergeFrom;
            IEnumerator<KeyValuePair<string, string>> ienumerator = ienumerable.GetEnumerator();

            while (ienumerator.MoveNext())
            {
                propertyBagToMergeInto[ienumerator.Current.Key] = ienumerator.Current.Value;
            }
        }

        /// <summary>
        /// Merge the contents of two collections together. This is a concatenation
        /// where duplication is permitted.
        /// </summary>
        /// <typeparam name="T">Collection type.</typeparam>
        /// <param name="collectionToMergeInto">Target collection.</param>
        /// <param name="collectionToMergeFrom">Source collection.</param>
        private static void MergeCollections<T>(ICollection<T> collectionToMergeInto, ICollection<T> collectionToMergeFrom)
        {
            foreach (T item in collectionToMergeFrom)
            {
                collectionToMergeInto.Add(item);
            }
        }
       
        /// <summary>
        /// Create a deterministic HashCode for the TestInfo.
        /// </summary>        
        private int GetDeterministicHashCode()
        {
            string semiUniqueIdentifier = Area+SubArea+Name+Priority;
            //use MD5 hash to get a 16-byte hash of the string: 
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
            byte[] inputBytes = Encoding.Default.GetBytes(semiUniqueIdentifier);
            byte[] hashBytes = provider.ComputeHash(inputBytes);
            
            //Create a GUID object from the bytes and interpret the hashcode.
            return new Guid(hashBytes).GetHashCode();            
        }

        #endregion

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        class TimeSpanConverter : TypeConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                double doubleValue;
                if (!double.TryParse((string)value, out doubleValue))
                {
                    throw new ArgumentException("Value must be valid double: " + value);
                }
                return TimeSpan.FromSeconds(doubleValue);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string);
            }
        }
    }
}
