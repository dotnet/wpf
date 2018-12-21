// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Microsoft Windows Client Platform
//
//
//  Contents: Namespace compatiblity support 

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    ///
    /// This attribute allows an assembly to declare that previously published 
    /// XmlnsDefinitions are subsumed by a new version.
    /// 
    /// Such as 
    /// 
    ///    "http://schemas.example.com/2003/mynamespace" 
    /// 
    /// is changed to 
    /// 
    ///    "http://schemas.example.com/2005/mynamespace"
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class XmlnsCompatibleWithAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oldNamespace">old Xml namespce</param>
        /// <param name="newNamespace">new xml namespace</param>
        public XmlnsCompatibleWithAttribute(string oldNamespace, string newNamespace)
        {
            OldNamespace = oldNamespace ?? throw new ArgumentNullException(nameof(oldNamespace));
            NewNamespace = newNamespace ?? throw new ArgumentNullException(nameof(newNamespace));
        }

        /// <summary>
        /// Old Xml Namespace
        /// </summary>
        public string OldNamespace { get; }

        /// <summary>
        /// New Xml Namespace
        /// </summary>
        public string NewNamespace { get; }
   }
}

