// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Serialization
{
    using System.IO;
    using System.Text;
    using System.Windows.Markup;
    using Microsoft.Test.Logging;
    using Microsoft.Test.Markup;
    using Microsoft.Test.Windows;

    /// <summary>
    ///  The Microsoft.Test.Serialization.XamlRoundTrip class is provided to 
    ///  test the serialization of any WPF control using a single API. 
    /// </summary>
    public static class XamlRoundTrip
    {
        /// <summary>
        ///  1. Take existing xaml test data 
        ///  2. Generate WPF object using XamlReader.Load 
        ///  3. Use XamlWriter.Save to serialize the Object into Xaml 
        ///  4. Verify this generated Xaml matched the expected xaml provided to the test
        ///  5. Use XamlReader.Load to generate a WPF Object using the serialized XAML
        ///  6. Verify that this new object is identical to object created with the original XAML 
        /// </summary>
        /// <param name="userCreatedXaml">string containing XAML that has to be RoundTripped</param>
        /// <param name="preSerializedXaml">string containing XAML that we should get if the RoundTrip was successful</param>
        /// <returns>True if RoundTrip is successful otherwise False</returns>
        public static bool Verify(string userCreatedXaml, string preSerializedXaml)
        {
            object wpfObject = null;
            using (Stream userCreatedXamlStream = CreateMemoryStream(userCreatedXaml))
            {
                wpfObject = XamlReader.Load(CreateMemoryStream(userCreatedXaml));
                if (wpfObject == null)
                {
                    Variation.Current.LogMessage("UNEXPECTED: Unable to create wpfObject using XamlReader.Load");
                    return false;
                }

                Variation.Current.LogMessage("Created wpfObject"); 
            }

            string newSerializedXaml = XamlWriter.Save(wpfObject);
            Variation.Current.LogMessage("Serialized wpfObject -> newSerializedXaml");

            if (!newSerializedXaml.Equals(preSerializedXaml))
            {
                Variation.Current.LogMessage("UNEXPECTED: newSerializedXaml and test data(preSerializedXaml)do not match");
                SaveFileToLogFolder("Serialized.xaml", newSerializedXaml);
                return false;
            }

            Variation.Current.LogMessage("newSerializedXaml and test data(preSerializedXaml) match");

            object roundTrippedWpfObject = XamlReader.Load(CreateMemoryStream(newSerializedXaml));
            if (roundTrippedWpfObject == null)
            {
                Variation.Current.LogMessage("UNEXPECTED: Unable to create Round Tripped wpfObject XamlReader.Load(newSerializedXaml)");
                return false;
            }

            Variation.Current.LogMessage("Created Round Tripped wpfObject");

            TreeCompareResult objectComparisonResult = TreeComparer.CompareLogical(wpfObject, roundTrippedWpfObject);
            if (objectComparisonResult.Result == CompareResult.Different)
            {
                Variation.Current.LogMessage("UNEXPECTED: RoundTripped WPF Object and Original WPF object no not match");
                return false;
            }

            Variation.Current.LogMessage("RoundTripped WPF Object and Original WPF object match");
            return true;
        }

        /// <summary>
        /// Writes a string into a file and saves the file to disk 
        /// Calls Variation.Current.LogFile to copy the file to Test Logs folder
        /// </summary>
        /// <param name="fileName">Name of file</param>
        /// <param name="str">string containing data that has to be written to the file</param>
        private static void SaveFileToLogFolder(string fileName, string str)
        {
            File.WriteAllText(fileName, str);
            Variation.Current.LogFile(new FileInfo(fileName));
        }

        /// <summary>
        /// Creates a MemoryStream from a string 
        /// </summary>
        /// <param name="str">string containing data that has to be moved into a stream</param>
        /// <returns>MemoryStream or throws an ArgumentNullException</returns>
        private static MemoryStream CreateMemoryStream(string str)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(str);
            MemoryStream stream = new MemoryStream(byteArray);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
