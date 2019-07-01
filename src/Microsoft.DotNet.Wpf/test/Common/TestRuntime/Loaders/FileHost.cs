// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Net;
using Microsoft.Test.Logging;
using Microsoft.Test.CrossProcess;

namespace Microsoft.Test.Loaders 
{
    /// <summary>
    /// Uri Scheme supported by FileHost
    /// </summary>
    public enum FileHostUriScheme 
    {
        /// <summary>
        /// The local directory that AppMonitor is executing in
        /// (Useful for overwriting the same file locally)
        /// </summary>
        Local,
        /// <summary>
        /// A network UNC path to the file
        /// </summary>
        Unc,
        /// <summary>
        /// An Http Uri to the file (Intranet)
        /// </summary>
        HttpIntranet,
        /// <summary>
        /// An Http Uri to the file (Internet)
        /// Fully qualifies and appends a . to the end of the host name.
        /// </summary>
        HttpInternet,
        /// <summary>
        /// An Https Uri to the file (Intranet)
        /// </summary>
        HttpsIntranet,
        /// <summary>
        /// An Https Uri to the file (Internet)
        /// Fully qualifies and appends a . to the end of the host name.
        /// </summary>
        HttpsInternet
    }

    /// <summary>
    /// class for Hosting remote files for test automation
    /// </summary>    
    public class FileHost : IDisposable 
    {        
        #region Private Data
        ArrayList customUploadedFiles;

        bool isClosed = false;

        // List of files that have already been uploaded... if we try to do the same one twice, may as well save the b/w.
        static ArrayList alreadyUploadedFiles = new ArrayList();

        // files are copied to this server by default
        const string DEFAULT_SCRATCH_SERVER = @"\\wpf\testscratch";

        // unique directory for this test case's files
        string uniqueWriteablePath;
        string uniqueReadOnlyPath;
        string myDirectoryName = "";

        #endregion
        
        #region Constructors

        /// <summary>
        /// Creates a new FileHost without custom directory or external server
        /// </summary>
        public FileHost() : this(null, false) {}

        /// <summary>
        /// Creates a new FileHost with custom user-named directory using local servers
        /// </summary>
        public FileHost(string userDirectory): this(userDirectory, false) {}

        /// <summary>
        /// Creates a new File Host for remote testing
        /// </summary>
        /// <param name="userDirectory">Custom-named directory to store files</param>
        /// <param name="useExternalServer"></param>
        public FileHost(string userDirectory, bool useExternalServer)
        {
            customUploadedFiles = new ArrayList();

            GlobalLog.LogDebug("Checking server for old / abandoned test directories... ");
            // Get any directory name that starts with the same three chars as the current time (in Ticks)
            // At this level, these #'s only change once every 11 years or so.
            string[] testScratchDirs;
            
            try 
            {
                testScratchDirs = Directory.GetDirectories(GetServerName() + @"\", DateTime.Now.Ticks.ToString().Substring(0, 3) + "*", SearchOption.TopDirectoryOnly);
            }
            catch (System.IO.IOException)
            {
                GlobalLog.LogEvidence("!!!! Error hit while attempting to instantiate FileHost.  Erroring out.  !!!!");
                throw new Exception("Error! The user account running this automation does not have access to the path " + GetServerName() +
                                    "\nPlease load up an explorer window and verify read/write access to the path " + GetServerName() +
                                    "\nFailing that, contact MattGal for assistance in debugging this issue.");
            }
            
            // Delete up to three directories.  Three is an arbitrary amount but it prevents this stage from taking more than a couple seconds of time.
            // Will also always cause testcases running to delete more than they can create, which will keep the scratch dir scrubbed.
            for (int i = 0; i < Math.Min(testScratchDirs.Length, 3); i++) 
            {
                long dirTime = -1;
                if ((long.TryParse(testScratchDirs[i].Replace(GetServerName() + @"\", "").Substring(0, 18), out dirTime)) && dirTime != 0)
                {
                    // 900000000000 = 25 hours in Ticks (Millionths of a second)
                    // At least 24 hours is needed for machines of different regional time zones executing 
                    // tests at the same time, since Ticks seems to be locale dependent.
                    if ((DateTime.Now.Ticks - dirTime) > 900000000000)
                    {
                        GlobalLog.LogDebug("*** Removing > 2 hour old autogenerated directory " + testScratchDirs[i].Trim() + " from external testscratch server");
                        try
                        {
                            Directory.Delete(testScratchDirs[i], true);
                        }
                        catch 
                        {
                            // Do nothing, this can happen occasionally when many testcases start at the exact same time.
                            // Worst case scenario is that the file just doesnt get deleted... this will succeed at some later date.
                        }
                    }
                }
            }

            // set uniqueWriteablePath
            if (userDirectory != null)
            {
                myDirectoryName = userDirectory;
            }
            else
            {
                myDirectoryName = DateTime.Now.Ticks.ToString();
            }

            uniqueWriteablePath = GetServerName() + @"\" + myDirectoryName + @"\";

            // set uniqueReadOnlyPath.  Long ago this was actually different from writeable path, and could change back again some day... 
            uniqueReadOnlyPath = uniqueWriteablePath;

            // create directory
            GlobalLog.LogDebug("Creating directory " + uniqueWriteablePath);
            CreateDirectory(uniqueWriteablePath);
        }

        #endregion
    
        #region Public Members

        // Urls of test servers, used for forcing similar URLs into the correct zones
        /// <summary>
        /// Base Url of HTTP Internet server
        /// </summary>
        public static readonly string HttpInternetBaseUrl = "http://wpf.redmond.corp.microsoft.com/testscratch/";
        /// <summary>
        /// Base Url of HTTPS Internet server
        /// </summary>
        public static readonly string HttpsInternetBaseUrl = "https://wpf.redmond.corp.microsoft.com/testscratch/";
        /// <summary>
        /// Base Url of HTTP Intranet server
        /// </summary>
        public static readonly string HttpIntranetBaseUrl = "http://wpfapps/testscratch/";
        /// <summary>
        /// Base Url of HTTPS Intranet server
        /// </summary>
        public static readonly string HttpsIntranetBaseUrl = "https://wpf:441/testscratch/";
        /// <summary>
        /// Base Url of UNC Server
        /// </summary>
        public static readonly string UncBaseUrl = "file://wpf/testscratch/";  

        /// <summary>
        /// Uploads a file and its dependencies to the FileHost (existing files will be overritten) 
        /// This method knows about support files for .xps, .xbap, .application, and .xaml files.
        /// </summary>
        /// <param name="filename">path to the file to upload</param>
        public void UploadFileWithDependencies(string filename)
        {
            UploadFileWithDependencies(filename, false);
        }

        /// <summary>
        /// If true, will upload files to the server relative to the subdirectories the files are in
        /// compared to the directory AppMonitor is running in.  Default: False
        /// </summary>
        public bool PreserveDirectoryStructure = false;

        /// <summary>
        /// Uploads a file and its dependencies to the FileHost (existing files will be overritten) 
        /// This methods supports .xps, .application, and .xaml files.
        /// </summary>
        /// <param name="filename">path to the file to upload</param>
        /// <param name="isLocal">Whether files should be copied over the local directory or remote</param>
        public void UploadFileWithDependencies(string filename, bool isLocal)
        {
            EnsureState();

            string fileType = Path.GetExtension(filename).ToLowerInvariant();

            switch (fileType) 
            {
                case ".xps": 
                {

                    UploadFile(filename, isLocal);
                    break;
                }
                case ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION:
                case ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION:
                {
                    ArrayList deployDependencies = GetDeployDependencies(filename);

                    foreach (string file in deployDependencies)
                    {
                        UploadFile(file, isLocal);
                    }

                    break;
                }
                case ".xaml": 
                {
                    ArrayList xamlDependencies = GetXamlDependencies(filename);

                    foreach (string file in xamlDependencies)
                    {
                        UploadFile(file, isLocal);
                    }
                
                    break;
                }
                default: 
                {
                    // don't know how to handle this file
                    GlobalLog.LogDebug("Unknown file type, passing to UploadFile().");
                    UploadFile(filename, isLocal);
                    break;
                }
            }
        }

        /// <summary>
        /// Uploads a file and its dependencies to the FileHost (existing files will be overwritten) 
        /// This methods supports .xps, .application, and .xaml files.
        /// </summary>
        /// <param name="fileName">path to the file to upload</param>
        /// <param name="isLocal">Whether files should be copied over the local directory or remote</param>
        public void UploadFile(string fileName, bool isLocal)
        {
            if (isLocal)
            {
                UploadFileLocal(fileName);
            }
            else
            {
                UploadFile(fileName);
            }
        }

        /// <summary>
        /// Uploads a file to the FileHost (existing files will be overwritten)
        /// </summary>
        /// <param name="filename">path to the file to upload</param>
        /// <param name="path">directory path to upload to</param>
        public void UploadFile(string filename, string path) 
        {
            string directorySeparatorStr = Path.DirectorySeparatorChar.ToString();
            
            if (path == null)
            {
                path = string.Empty;
            }

            if (path != string.Empty && !path.EndsWith(directorySeparatorStr))
            {
                path += directorySeparatorStr;
            }            

            EnsureState();

            string origFileName = filename;

            // construct path to destination
            string destination = uniqueWriteablePath + path + Path.GetFileName(filename);
            // we should really use Path.Combine, but I didn't do it yet, b/c this method uses backslashes for directory
            // separators quite a bit
            // string destination = Path.Combine(Path.Combine(uniqueWriteablePath + path), Path.GetFileName(filename);

            // Trim the path if it's being copied from a binplace directory.
            // Might need to fix this later if anyone actually needs a path like this on a server (seriously doubt it)
            if (filename.ToLowerInvariant().StartsWith(@"bin" + directorySeparatorStr + "release" + directorySeparatorStr)) 
            {
                filename = filename.Remove(0, 12);
            }

            if (PreserveDirectoryStructure)
            {
                // "path" overrides "filename"
                if (path != string.Empty)
                {
                    CreateDirectory(Path.GetDirectoryName(destination));
                } 
                else if (filename.LastIndexOf(directorySeparatorStr) > 0)
                {
                    destination = uniqueWriteablePath + filename;
                    CreateDirectory(Path.GetDirectoryName(destination));
                }
            }

            // copy file
            if (!alreadyUploadedFiles.Contains(filename))
            {
                alreadyUploadedFiles.Add(filename);
                GlobalLog.LogDebug("Copying " + filename + " to " + destination);
            }
            File.Copy(origFileName, destination, true);
        }

        /// <summary>
        /// Uploads a file to the FileHost (existing files will be overwritten)
        /// </summary>
        /// <param name="filename">path to the file to upload</param>
        public void UploadFile(string filename)
        {
            UploadFile(filename, string.Empty);
        }

        /// <summary>
        /// Uploads a file to the local directory (existing files will be overwritten)
        /// </summary>
        /// <param name="filename">path to the file to upload</param>
        public void UploadFileLocal(string filename)
        {
            EnsureState();

            // construct path to destination
            string destination = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + Path.GetFileName(filename);

            // copy file
            GlobalLog.LogDebug("Copying " + filename + " to " + destination);
            File.Copy(filename, destination, true);
        }

        /// <summary>
        /// Copies a file to an arbitrary path.  File will not be cleaned up.
        /// </summary>
        /// <param name="filename">File to copy</param>
        /// <param name="copyToUrl">Path to server to copy to</param>
        public void UploadFileNonDefaultServer(string filename, string copyToUrl)
        {
            EnsureState();

            string destination = copyToUrl + @"\" + Path.GetFileName(filename);

            GlobalLog.LogDebug("Copying " + filename + " to " + destination);
            File.Copy(filename, destination, true);
            customUploadedFiles.Add(destination);
        }

        /// <summary>
        /// Gets a Uri of the specified file for the specified Uri scheme
        /// </summary>
        /// <param name="filename">name of the file to get the Uri</param>
        /// <param name="scheme">Uri sheme to use for the Uri</param>
        /// <returns>Uri for the specified file using the specified Uri scheme</returns>
        public Uri GetUri(string filename, FileHostUriScheme scheme) 
        {
            EnsureState();

            // if the file exists
            if (File.Exists(uniqueWriteablePath + filename))
            {
                switch (scheme)
                {
                    case FileHostUriScheme.Local:
                        {
                            return new Uri(System.IO.Directory.GetCurrentDirectory() + @"\" + filename);
                        }
                    case FileHostUriScheme.Unc:
                        {
                            return new Uri(uniqueReadOnlyPath + filename);
                        }
                    case FileHostUriScheme.HttpsIntranet:
                        {
                            return new Uri(HttpsIntranetBaseUrl + myDirectoryName + "/" + filename);
                        }
                    case FileHostUriScheme.HttpsInternet:
                        {
                            return new Uri(HttpsInternetBaseUrl + myDirectoryName + "/" + filename);
                        }
                    case FileHostUriScheme.HttpInternet:
                        {
                            return new Uri(HttpInternetBaseUrl + myDirectoryName + "/" + filename);
                        }
                    default:
                    case FileHostUriScheme.HttpIntranet:
                        {
                            return new Uri(HttpIntranetBaseUrl + myDirectoryName + "/" + filename);
                        }
                }
            }
            else
            {
                //We may need to change this if people want to test the condition where the file does not exist.
                throw new FileNotFoundException("The specified file was not uploaded to the host. You must first call UploadFile", filename);
            }
        }

        /// <summary>
        /// Deletes all files hosted by the FileHost
        /// </summary>
        public void Clear() 
        {
            EnsureState();

            string [] files = Directory.GetFiles(uniqueWriteablePath);

            foreach (string file in files)
            {
                GlobalLog.LogDebug("Deleting file " + file + " from " + uniqueWriteablePath + ".");
                File.Delete(file);
            }
            alreadyUploadedFiles.Clear();
        }

        /// <summary>
        /// Closes the FileHost and delete all files it contains.
        /// </summary>
        public void Close() 
        {
            EnsureState();
            isClosed = true;

            // delete unique directory
            try
            {
                GlobalLog.LogDebug("Deleting folder " + uniqueWriteablePath);
                Directory.Delete(uniqueWriteablePath, true);
            }
            catch (IOException ioe)
            {
                GlobalLog.LogDebug("Could not delete " + uniqueWriteablePath + ".");
                GlobalLog.LogDebug("Exception message: " + ioe.Message);
            }

            // Delete custom uploaded files, if any.
            foreach (object fileToDelete in customUploadedFiles)
            {
                try
                {
                    File.Delete((string)fileToDelete);
                }
                catch (IOException ioe)
                {
                    GlobalLog.LogDebug("Could not delete " + (string)fileToDelete + ".");
                    GlobalLog.LogDebug("Exception message: " + ioe.Message);
                }
            }
            alreadyUploadedFiles.Clear();
        }

        #endregion
        
        #region Private Implementation

        private void CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
				Directory.CreateDirectory(directoryPath);
				GlobalLog.LogDebug("Storing files at " + directoryPath);
            }
            else
            {
				GlobalLog.LogDebug("Path " + directoryPath + " already exists.");
            }
        }

        private string GetServerName()
        {
            //string piperScratchServerRW = DictionaryStore.Current["TestScratch_RW"];

            //if (!String.IsNullOrEmpty(piperScratchServerRW))
            //{
            //    return piperScratchServerRW;
            //}
            //else
            //{
                return DEFAULT_SCRATCH_SERVER;
            //}
        }

        private string GetFullyQualifiedHostName()
        {
            // Trim out the host name...
            string hostName = uniqueReadOnlyPath.Substring(2);
            string restOfAddress = hostName.Substring(hostName.IndexOf('\\', 2));

            hostName = hostName.Remove(hostName.IndexOf('\\', 2));

            // Use DNS to get the whole host namem, append a . to make it Internet Zone
            // hostName = System.Net.Dns.GetHostByName(hostName).HostName + portToUse;
            hostName = System.Net.Dns.GetHostEntry(hostName).HostName;            

            // Add the back part back onto the URI...
            hostName = @"\\" + hostName + restOfAddress;
            return hostName;
        }

        // This is to support the C# using syntax
        void IDisposable.Dispose() 
        {
            if (!isClosed)
                Close();
        }

        void EnsureState() 
        {
            if (isClosed)
                throw new InvalidOperationException("You cannot perform this action on a FileHost that has been closed.");
        }

        ArrayList GetDeployDependencies(string deployFileName)
        {
            string manifestFileName;

            // construct manifest file name
            if (deployFileName.Contains(ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION)) 
            {
                manifestFileName = 
                    deployFileName.Replace(ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION, ".exe.manifest");
            }
            else
            {
                manifestFileName = 
                    deployFileName.Replace(ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION, ".exe.manifest");
            }

            string relativePath = null;
            if (manifestFileName.LastIndexOf('\\') > 0)
            {
                relativePath = manifestFileName.Substring(0, manifestFileName.LastIndexOf('\\') + 1);
            }

            // create ArrayList to hold filenames
            ArrayList fileArrayList = new ArrayList();

            // add .application and .manifest files to fileArrayList
            fileArrayList.Add(deployFileName);
            fileArrayList.Add(manifestFileName);

            // create an XML document
            XmlDocument manifestFile = new XmlDocument();
            
            // load the manifest file
            manifestFile.Load(manifestFileName);

            // get required files references from manifest - append rel. path if one is included.
            if (relativePath == null)
            {
                fileArrayList = GetValuesFromXmlDocument(fileArrayList, manifestFile, "file", "name");
                fileArrayList = GetValuesFromXmlDocument(fileArrayList, manifestFile, "dependentAssembly", "codebase");
            }
            else
            {
                fileArrayList = GetValuesFromXmlDocumentWithPath(fileArrayList, manifestFile, "file", "name", relativePath);
                fileArrayList = GetValuesFromXmlDocumentWithPath(fileArrayList, manifestFile, "dependentAssembly", "codebase", relativePath);
            }
            return fileArrayList;
        }

        ArrayList GetXamlDependencies(string xamlFileName)
        {
            // create ArrayList to hold filenames
            ArrayList fileArrayList = new ArrayList();

            // add xaml file itself to array
            fileArrayList.Add(xamlFileName);

            // creat XmlDocument
            XmlDocument xamlFile = new XmlDocument();
            
            // load xaml file
            xamlFile.Load(xamlFileName);
            
            // get the list of files
            fileArrayList = GetValuesFromXmlDocument(fileArrayList, xamlFile, "Image", "Source");

            return fileArrayList;
        }

        // helps GetXamlDependencies and GetDeployDependencies get values from XML nodes
        ArrayList GetValuesFromXmlDocument(ArrayList list, XmlDocument document, string tagName, string attributeName) 
        {
            XmlNodeList nodeList = document.GetElementsByTagName(tagName);

            foreach (XmlNode node in nodeList) 
            {
                if (node.Attributes[attributeName] != null)
                    if (!list.Contains(node.Attributes[attributeName].Value))
                        list.Add(node.Attributes[attributeName].Value);
            }
            return list;
        }
        ArrayList GetValuesFromXmlDocumentWithPath(ArrayList list, XmlDocument document, string tagName, string attributeName, string relativePath)
        {
            XmlNodeList nodeList = document.GetElementsByTagName(tagName);

            foreach (XmlNode node in nodeList)
            {
                if (node.Attributes[attributeName] != null)
                {
                    if (node.Attributes[attributeName].Value.Contains(relativePath))
                    {
                        if (!list.Contains(node.Attributes[attributeName].Value))
                            list.Add(node.Attributes[attributeName].Value);
                    }
                    else
                    {
                        if (!list.Contains(relativePath + node.Attributes[attributeName].Value))
                            list.Add(relativePath + node.Attributes[attributeName].Value);
                    }
                }
            }
            return list;
        }
        
        #endregion        

    }
}
