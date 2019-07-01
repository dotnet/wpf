// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// Support file for a test case
    /// </summary>
    [Serializable()]
    [ObjectSerializerAttribute(typeof(FastObjectSerializer))]
    public class TestSupportFile : ICloneable
    {

        #region Private Data

        string source = null;
        string destination = null;

        #endregion


        #region Public Members

        /// <summary>
        /// Retuns a string representation of a supportfile
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(destination))
                return source;
            else
                return source + "->" + destination;
        }


        /// <summary>
        ///  Returns a memberwise clone of the TestSupportFileObject
        /// </summary>
        /// <returns>Memberwise clone.</returns>
        public object Clone()
        {
            // MemberwiseClone will make copies of simple types.
            // Currently that's all TestSupportFile has, but you must add manual copying if this changes.
            return (TestSupportFile)this.MemberwiseClone();
        }

        /// <summary>
        /// Relative Source path to the support file
        /// </summary>
        /// <remarks>
        /// This may include * wildcards in the filename or specify a directory.
        /// Direcories trees are copied if specified or if * or *.* is used.
        /// </remarks>
        [XmlAttribute]
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        /// <summary>
        /// Path to a folder relative to the Run direcory where the file will be copied, or an absolute path.
        /// </summary>
        [XmlAttribute]
        public string Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        #endregion


        #region Equality Overloading

        /// <summary/>
        public override bool Equals(object obj)
        {
            TestSupportFile suppFile = obj as TestSupportFile;
            if (suppFile == null)
            {
                return false;
            }
            return suppFile.source == source && suppFile.destination == destination;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int hash1 = (source == null) ? 0 : source.GetHashCode();
            int hash2 = (destination == null) ? 0 : destination.GetHashCode();
            return hash1 ^ hash2;
        }


        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(TestSupportFile first, TestSupportFile second)
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
        public static bool operator !=(TestSupportFile first, TestSupportFile second)
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
    }
}