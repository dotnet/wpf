// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test
{
    /// <summary>
    /// Information about a bug that is causing the test to fail. Can be useful to failure analysis tools.
    /// </summary>
    public class Bug : ICloneable
    {
        #region Private Data

        private int id;
        private string source;

        #endregion

        #region Public Members

        /// <summary>
        /// Perform a memberwise clone.
        /// </summary>
        /// <returns>Memberwise clone.</returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Bug number.
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Data store that the bug is in.
        /// </summary>
        public String Source
        {
            get { return source; }
            set { source = value; }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Reports the ID of the bug, as our bugs are tracked in TFS.
        /// </summary>
        public override string ToString()
        {
            return Id.ToString();
        }

        /// <summary>
        /// Memberwise equality.
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>Whether object is a bug and is memberwise equal.</returns>
        public override bool Equals(object obj)
        {
            Bug other = obj as Bug;

            if (other == null)
            {
                return false;
            }

            if (!Object.Equals(Id, other.Id))
            {
                return false;
            }
            if (!Object.Equals(Source.ToUpperInvariant(), other.Source.ToUpperInvariant()))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate hash code.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Source.ToUpperInvariant().GetHashCode();
        }

        #endregion
    }
}