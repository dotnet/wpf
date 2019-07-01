// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Reflection;


namespace Microsoft.Test.Modeling
{

    /// <summary>
    /// Base class for Model class.
    /// </summary>
    public class ModelingBaseObject
    {
        //Static Members


        private string _name = "";
        /// <summary>
        /// Name to identify the object instance
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _description = "";
        /// <summary>
        /// Description of the object instance
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        private int _instance = 0;
        /// <summary>
        /// Number indicating the instance number for the class
        /// </summary>
        public int Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }

        /// <summary>
        /// Creates an BaseObject instance
        /// </summary>
        public ModelingBaseObject()
        {
        }

    }


}
