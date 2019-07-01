// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.IO;
using System.Windows.Markup;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create an ObjectType object from xaml files under FactoryXamlFiles.
    /// </summary>
    internal abstract class LoadFromXamlFactory<ObjectType> : DiscoverableFactory<ObjectType> where ObjectType : class
    {
        #region Protected Members

        /// <summary>
        /// Load xaml from .xaml files under the returned directory.
        /// </summary>
        protected virtual string GetXamlDirectoryPath()
        {
            return "FactoryXamlFiles";
        }

        #endregion

        #region Override Members

        public override ObjectType Create(DeterministicRandom random)
        {
            FileInfo xamlFile = ChooseXamlFileFromDirectory(random);
            ObjectType element;
            using (FileStream fileStream = xamlFile.OpenRead())
            {
                element = XamlReader.Load(fileStream) as ObjectType;
            }

            return element;
        }

        #endregion

        #region Private Members

        private FileInfo ChooseXamlFileFromDirectory(DeterministicRandom random)
        {
            DirectoryInfo xamlDirectory = new DirectoryInfo(GetXamlDirectoryPath());
            FileInfo[] xamlFiles = xamlDirectory.GetFiles("*.xaml", SearchOption.AllDirectories);
            if (xamlFiles == null || xamlFiles.Length == 0)
            {
                throw new ArgumentException(string.Format("TestBug: There isn't any xaml file in {0} directory.", xamlDirectory.FullName));
            }

            return xamlFiles[random.Next(xamlFiles.Length)];
        }

        #endregion
    }
}
