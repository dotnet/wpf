// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Xml;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Loaders;
using System.IO;
namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Create a Uri using the a file on FileHost, specified in file ListOfFilesOnFileHost.xml, 
    /// based on Scheme property randomly created. 
    /// The format of ListOfFilesOnFileHost is
    ///     <Files>
    ///         <File Name="file1"/>
    ///         ...
    ///     </Files>
    ///  If ListOfFilesOnFileHost.xml not exist, or contain no File node, throw. 
    ///  Factory assumes that the files are already updated, to avoid uploading files over and over. 
    /// </summary>
    [TargetTypeAttribute(typeof(Uri))]
    class FileHostUriFactory : DiscoverableFactory<Uri>
    {
        private readonly string filesOnFileHost = "ListOfFilesOnFileHost.xml";

        /// <summary>
        /// The syntax of the Uri. 
        /// </summary>
        public FileHostUriScheme Scheme { get; set; }

        /// <summary>
        /// Create a Uri using one of the files specified in ListOfFilesOnFileHost.xml. 
        /// For Uris created with different files, modify ListOfFilesOnFileHost.xml. 
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Uri Create(DeterministicRandom random)
        {
            XmlDocument document = new XmlDocument();

            document.Load(filesOnFileHost);
            XmlNodeList files = document.GetElementsByTagName("File");
            int fileCount = files.Count;

            if (fileCount == 0)
            {
                throw new InvalidOperationException(String.Format("File: {0} doesn't contain any File node.", filesOnFileHost));
            }

            int fileIndex = random.Next(fileCount);

            XmlElement fileNode = files[fileIndex] as XmlElement;
            string filename = fileNode.GetAttribute("Name");
            if (String.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException(String.Format("File node : \n {0} doesn't have Name attribute.", fileNode.OuterXml));
            }

            FileHost fileHost = new FileHost();

            Uri uri = fileHost.GetUri(filename, Scheme);

            return uri;
        }
    }
}
