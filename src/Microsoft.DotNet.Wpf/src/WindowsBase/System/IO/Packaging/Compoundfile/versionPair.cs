// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  A major, minor version number pair.
//

// Allow use of presharp warning numbers [6506] unknown to the compiler
#pragma warning disable 1634, 1691

using System;
using System.Globalization;

#if PBTCOMPILER
using MS.Utility;     // For SR.cs
#else
using System.Windows;
using System.Text;
using MS.Internal.WindowsBase; // FriendAccessAllowed
#endif

namespace MS.Internal.IO.Packaging.CompoundFile
{
    ///<summary>Class for a version pair which consists of major and minor numbers</summary>
#if !PBTCOMPILER
    [FriendAccessAllowed]
    internal    class VersionPair : IComparable
#else
    internal  class VersionPair
#endif
    {
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor for VersionPair with given major and minor numbers.
        /// </summary>
        /// <param name="major">major part of version</param>
        /// <param name="minor">minor part of version</param>
        internal VersionPair(Int16 major, Int16 minor)
        {
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException("major",
                            SR.Get(SRID.VersionNumberComponentNegative));
            }

            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException("minor",
                            SR.Get(SRID.VersionNumberComponentNegative));
            }

            _major = major;
            _minor = minor;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Major number of version
        /// </summary>
        public Int16 Major
        {
            get
            {
                return _major;
            }
        }

        /// <summary>
        /// Minor number of version
        /// </summary>
        public Int16 Minor
        {
            get
            {
                return _minor;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public methods

#if !PBTCOMPILER
        /// <summary>
        /// Returns a string that represents the current VersionPair object.
        /// The string is of the form (major,minor).
        /// </summary>
        public override string ToString()
        {
            StringBuilder stringFormBuilder = new StringBuilder("(");
            stringFormBuilder.Append(_major);
            stringFormBuilder.Append(",");
            stringFormBuilder.Append(_minor);
            stringFormBuilder.Append(")");
            
            return stringFormBuilder.ToString();
        }
#endif

        #endregion

        #region Operators

        /// <summary>
        /// == comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator ==(VersionPair v1, VersionPair v2)
        {
            bool result = false;

            // If both v1 & v2 are null they are same
            if ((Object) v1 == null && (Object) v2 == null)
            {
                result = true;
            }
            // Do comparison only if both v1 and v2 are not null
            else if ((Object) v1 != null && (Object) v2 != null)
            {
                if (v1.Major == v2.Major && v1.Minor == v2.Minor)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// != comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator !=(VersionPair v1, VersionPair v2)
        {
            // == is previously define so it can be used
            return !(v1 == v2);
        }

#if !PBTCOMPILER
        /// <summary>
        /// "less than" comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator <(VersionPair v1, VersionPair v2)
        {
            bool result = false;

            if ((Object) v1 == null && (Object) v2 != null)
            {
                result = true;
            }
            else if ((Object) v1 != null && (object) v2 != null)
            {
                // == is previously define so it can be used
                if (v1.Major < v2.Major || ((v1.Major == v2.Major) && (v1.Minor < v2.Minor)))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// "greater than" comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator >(VersionPair v1, VersionPair v2)
        {
            bool result = false;

            if ((Object) v1 != null && (Object) v2 == null)
            {
                result = true;
            }
            // Comare only if neither v1 nor v2 are not null
            else if ((Object) v1 != null && (object) v2 != null)
            {
                // < and == are previously define so it can be used
                if (!(v1 < v2) && v1 != v2)
                {
                    return true;
                }
            }
                
            return result;
        }

        /// <summary>
        /// "less than or equal" comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator <=(VersionPair v1, VersionPair v2)
        {
            if (!(v1 > v2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// "greater than or equal" comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator >=(VersionPair v1, VersionPair v2)
        {
            if (!(v1 < v2))
            {
                return true;
            }

            return false;
        }
#endif

        /// <summary>
        /// Eaual comparison operator
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>true if the object is equal to this instance</returns>
        public override bool Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            VersionPair v = (VersionPair) obj;

            //PRESHARP:Parameter to this public method must be validated:  A null-dereference can occur here. 
            //    Parameter 'v' to this public method must be validated:  A null-dereference can occur here. 
            //This is a false positive as the checks above can gurantee no null dereference will occur  
#pragma warning disable 6506
            if (this != v)
            {
                return false;
            }
#pragma warning restore 6506

            return true;
        }

        /// <summary>
        /// Hash code
        /// </summary>
        public override int GetHashCode()
        {
            return (_major << 16 + _minor);
        }

#if !PBTCOMPILER
        /// <summary>
        /// Compare this instance to the object
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>Less than 0 - This instance is less than obj
        /// Zero - This instance is equal to obj
        /// Greater than 0 - This instance is greater than obj</returns>
        public int CompareTo(Object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (obj.GetType() != GetType())
            {
                throw new ArgumentException(SR.Get(SRID.ExpectedVersionPairObject));
            }

            VersionPair v = (VersionPair) obj;

            //PRESHARP:Parameter to this public method must be validated:  A null-dereference can occur here. 
            //    Parameter 'v' to this public method must be validated:  A null-dereference can occur here. 
            //This is a false positive as the checks above can gurantee no null dereference will occur  
#pragma warning disable 6506
            if (this.Equals(obj))   // equal
            {
                return 0;
            }
            // less than
            else if (this < v)
            {
                return -1;
            }
#pragma warning restore 6506
            // greater than
            return 1;
        }
#endif

        #endregion Operators

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None       
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        // None       
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None       
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Member Variables

        private Int16 _major;             // Major number
        private Int16 _minor;             // Minor number

        #endregion Member Variables
    }
}
