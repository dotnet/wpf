// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// FileHelper wraps common methods for file handling
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class FileHelper
    {
        /// <summary>
        /// Copy
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="overwrite"></param>
        internal static void Copy(string sourcePath, string destinationPath, bool overwrite)
        {
            File.Copy(sourcePath, destinationPath, overwrite);
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="name"></param>
        internal static void Delete(string name)
        {
            File.Delete(name);
        }

        /// <summary>
        /// Exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool Exists(string name)
        {
            return (File.Exists(name));
        }

        /// <summary>
        /// DeleteIfOlderThan
        /// </summary>
        /// <param name="file"></param>
        /// <param name="seconds"></param>
        internal static void DeleteIfOlderThan(string file, int seconds)
        {
            // get file last update
            DateTime fileLastAccess = File.GetLastAccessTime(file);

            // return if not so old file
            TimeSpan slept = DateTime.Now - fileLastAccess;
            TimeSpan limit = new TimeSpan(0, 0, seconds);
            if (slept.CompareTo(limit) < 0)
            {
                return;
            }

            // delete file
            File.Delete(file);
        }
    }

    /// <summary>
    /// DirectoryHelper wraps common methods for directory handling
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class DirectoryHelper
    {
        internal static void Create(string path)
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// FileHandler is a convenient wrapper for reading and writing from and to files in different ways
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class FileHandler: IDisposable
    {
        private FileStream fs = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;

        /// <summary>
        /// Constructs a FileHandler object
        /// </summary>
        /// <param name="path">path of the file</param>
        /// <param name="mode">FileMode</param>
        /// <param name="access">FileAccess</param>
        /// <param name="share">FileShare</param>
        internal FileHandler(string path, FileMode mode, FileAccess access, FileShare share)
        {
            // create the file stream
            fs = File.Open(path, mode, access, share);

            // create the stream reader if read access required
            if (access == FileAccess.Read || access == FileAccess.ReadWrite)
            {
                sr = new StreamReader(fs);
            }

            // create the stream writer if write access required
            if (access == FileAccess.Write || access == FileAccess.ReadWrite)
            {
                sw = new StreamWriter(fs);
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~FileHandler()
        {
            DoDispose();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            DoDispose();

            // supress finalizer
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees resources
        /// </summary>
        void DoDispose()
        {
            // close handles
            if (sw != null)
            {
                sw.Close();
            }

            if (sr != null)
            {
                sr.Close();
            }

            if (fs != null)
            {
                fs.Close();
            }
        }

        /// <summary>
        /// Closes the file
        /// </summary>
        internal void Close()
        {
            // close handles
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
            if (sr != null)
            {
                sr.Close();
                sr = null;
            }
            if (fs != null)
            {
                fs.Close();
                fs = null;
            }
        }

        /// <summary>
        /// Writes a string to a file
        /// </summary>
        /// <param name="s">string</param>
        internal void Write(string s)
        {
            sw.Write(s);
            sw.Flush();
        }


        /// <summary>
        /// Writes a string + CR/LF to a file
        /// </summary>
        /// <param name="s">string</param>
        internal void WriteLine(string s)
        {
            sw.WriteLine(s);
            sw.Flush();
        }


        /// <summary>
        /// Writes count bytes from bytes from the offset position
        /// </summary>
        /// <param name="bytes">byte[]</param>
        /// <param name="offset">int</param>
        /// <param name="count">int</param>
        internal void Write(byte[] bytes, int offset, int count)
        {
            fs.Write(bytes, offset, count);
            fs.Flush();
        }


        /// <summary>
        /// Reads count bytes from the file and puts them beggining in the index position in buffer
        /// </summary>
        /// <param name="buffer">char[]</param>
        /// <param name="index">int</param>
        /// <param name="count">int</param>
        /// <returns></returns>
        internal int Read(char[] buffer, int index, int count)
        {
            return (sr.Read(buffer, index, count));
        }


        /// <summary>
        /// Reads a line from a file
        /// </summary>
        /// <returns>the read line</returns>
        internal string ReadLine()
        {
            return (sr.ReadLine());
        }


        /// <summary>
        /// Reads count bytes from the file and writes them in bytes beggining in the position offset
        /// </summary>
        /// <param name="bytes">byte[]</param>
        /// <param name="offset">int</param>
        /// <param name="count">int</param>
        /// <returns></returns>
        internal int Read(byte[] bytes, int offset, int count)
        {
            return (fs.Read(bytes, offset, count));
        }


        /// <summary>
        /// Returns the entire content of the file
        /// </summary>
        /// <returns>entire content of the file</returns>
        internal string ReadToEnd()
        {
            return(sr.ReadToEnd());
        }


        /// <summary>
        /// Set the current position in the file
        /// </summary>
        /// <param name="offset">long</param>
        /// <param name="origin">SeekOrigin</param>
        /// <returns></returns>
        internal long Seek(long offset, SeekOrigin origin)
        {
            return (fs.Seek(offset, origin));
        }


        /// <summary>
        /// Current position in the file
        /// </summary>
        internal long Position
        {
            get
            {
                return (fs.Position);
            }
        }

        /// <summary>
        /// StreamReader used by this object
        /// </summary>
        /// <value>StreamReader</value>
        internal StreamReader StreamReader
        {
            get
            {
                return (sr);
            }
        }

        /// <summary>
        /// StreamWriter used by this object
        /// </summary>
        /// <value>StreamWriter</value>
        internal StreamWriter StreamWriter
        {
            get
            {
                return (sw);
            }
        }

        /// <summary>
        /// FileStream used by this object
        /// </summary>
        /// <value>FileStream</value>
        internal FileStream FileStream
        {
            get
            {
                return (fs);
            }
        }

        /// <summary>
        /// AppendLine adds a line to the log and closes the handle. It's not good for performance reasons, but enables
        /// multiple processes to log to the same log file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        internal static void AppendLine(string path, string content)
        {
            // define the number of times we will try
            const int attempts = 2000;

            // try to write several times
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    // open a stream to write
                    StreamWriter writer = new StreamWriter(path, true, System.Text.Encoding.ASCII);

                    // write
                    writer.WriteLine(content);

                    // flush the buffer (if not here, althought file is closed, no content is flushed. bug? who knows)
                    writer.Flush();

                    // close the stream
                    writer.Close();

                    // we got here, it means line was written. nothing else to do
                    return;
                }
                catch (Exception e)
                {
                    if ( i == attempts - 1 )
                    {
                        // give up
                        throw (e);
                    }
                }
            }
        }
    }


    /// <summary>
    /// FileSearch: directory walker. Takes a method to process files.
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class FileSearch
    {
        /// <summary>
        /// the constructor of a FileSearch object.
        /// </summary>
        /// <param name="NodeProc">delegate to call for each found node</param>
        /// <param name="StartDir">directory in which search will be started</param>
        internal FileSearch(NodeProcessor NodeProc, string StartDir)
        {
            // make sure that the NodeProcessor is not null
            if (NodeProc == null)
            {
                throw (new ArgumentNullException("NodeProc", "NodeProcessor delegate must be set"));
            }

            nodeProcessor = NodeProc;

            // make sure that the StartDir is not null
            if (StartDir == null)
            {
                throw (new ArgumentNullException("StartDir", "the start directory must be set"));
            }

            // set working variables
            startDir = StartDir;
            this.NodeProcessorProperty = NodeProc;
        }

        /// <summary>
        /// FileSearch default constructor
        /// </summary>
        internal FileSearch()
        {
        }

        /// <summary>
        /// startDir contains the directory to start with.
        /// </summary>
        private string startDir;
        internal string StartDirectory
        {
            set
            {
                startDir = value;
            }
            get
            {
                return (startDir);
            }
        }

        /// <summary>
        /// NodeProcessor is a delegate to call for each file found.
        /// </summary>
        internal delegate bool NodeProcessor(string NodeName);
        private NodeProcessor nodeProcessor;
        internal NodeProcessor NodeProcessorProperty
        {
            set
            {
                nodeProcessor = value;
            }
        }

        /// <summary>
        /// FilePattern property contains the filter of the files to look for.
        /// </summary>
        private string filePattern = "*";
        internal string FilePattern
        {
            set
            {
                filePattern = value;
            }
            get
            {
                return (filePattern);
            }
        }

        /// <summary>
        /// Start launches the search.
        /// </summary>
        internal void Start()
        {
            WalkHelper(startDir);
        }


        /// <summary>
        /// WalkHelper is a recursive function that goes through directories looking for the target files.
        /// </summary>
        /// <param name="Dir"></param>
        private void WalkHelper(string Dir)
        {
            //process files
            string[] files = System.IO.Directory.GetFiles(Dir, filePattern);

            foreach (string file in files)
            {
                nodeProcessor(file);
            }

            //process dirs
            string[] subdirs = System.IO.Directory.GetDirectories(Dir, "*");

            foreach (string subdir in subdirs)
            {
                // process this directory only if the user wants it
                if (nodeProcessor(subdir) == true)
                {
                    WalkHelper(subdir);
                }
            }
        }

        static bool fileFound = false;
        static string nameFound = null;
        internal static bool Find(string Filename, string Dir, out string Name)
        {
            FileSearch.NodeProcessor nodeProc = new FileSearch.NodeProcessor(NodeProc);
            FileSearch search = new FileSearch();

            // set the delegate to be called
            search.NodeProcessorProperty = nodeProc;

            // look for the .cdf file
            search.FilePattern = Filename;
            search.StartDirectory = Dir;
            search.Start();

            // check results
            Name = nameFound;
            return (fileFound);
        }

        static bool NodeProc(string Node)
        {
            if (FileAttributes.Directory == File.GetAttributes(Node))
            {
                // keep searching
                return (true);
            }
            else
            {
                fileFound = true;
                nameFound = Node;
            }

            return (true);
        }
    }
}
