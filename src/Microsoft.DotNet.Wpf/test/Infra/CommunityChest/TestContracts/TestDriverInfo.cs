// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary/>
    [Serializable()]
    [ObjectSerializerAttribute(typeof(FastObjectSerializer))]
    public class TestDriverInfo : ICloneable
    {
        #region Private Data

        //NOTE: Changing any field implies you must update Clone, GetHashCode and Equal methods.
        private string name;
        private string executable;

        #endregion

        #region Constructor

        /// <summary/>
        public TestDriverInfo()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Name of the driver. This can be specific to a given team but should be consistent.
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// Filename of the executable that will be used to launch the driver process.
        /// </summary>
        [XmlAttribute]
        public string Executable
        {
            get { return executable; }
            set { executable = value; }
        }

        /// <summary/>
        public TestDriverInfo Clone()
        {
            TestDriverInfo clone = (TestDriverInfo)this.MemberwiseClone();
            return clone;
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            TestDriverInfo other = obj as TestDriverInfo;

            if (other == null)
            {
                return false;
            }

            if (!Object.Equals(Name, other.Name))
            {
                return false;
            }
            if (!Object.Equals(Executable, other.Executable))
            {
                return false;
            }

            return true;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int hashCode = 0;
            if (!String.IsNullOrEmpty(Name))
            {
                hashCode = Name.GetHashCode();
            }
            if (!String.IsNullOrEmpty(Executable))
            {
                hashCode ^= Executable.GetHashCode();
            }

            return hashCode;
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(TestDriverInfo first, TestDriverInfo second)
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
        public static bool operator !=(TestDriverInfo first, TestDriverInfo second)
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

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}