// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
// Description:
//      Attribute definition for loose content files
//
//              
//

namespace System.Windows.Resources
{
    /// <summary>
    /// This attribute is used by the compiler to associate loose content with the application
    /// at compile time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AssemblyAssociatedContentFileAttribute : Attribute
    {
        private string _path;
        /// <summary>
        /// The default constructor recieves a relative path to the content.
        /// </summary>
        /// <param name="relativeContentFilePath"></param>
        public AssemblyAssociatedContentFileAttribute(string relativeContentFilePath)
        {
            _path = relativeContentFilePath;
        }

        /// <summary>
        /// The path to the associated content.
        /// </summary>
        public string RelativeContentFilePath
        {
            get 
            {
                return _path;
            }
        }
}
}
