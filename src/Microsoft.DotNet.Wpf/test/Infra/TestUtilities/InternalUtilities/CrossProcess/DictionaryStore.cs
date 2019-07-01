// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ServiceModel;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Test.CrossProcess
{
    /// <summary>
    /// Public Access point for users to interact with Cross Process Dictionary. 
    /// Provides consistent API for interacting with dictionary. Only difference is on starting of Server or Client.
    /// </summary>
    public static class DictionaryStore
    {
        private static object syncLock = new object();
        private static ADictionary dictionary;

        public static ADictionary Current
        {
            get
            {
                lock (syncLock)
                {
                    if (dictionary == null)
                    {   // Auto-create a dictionary for user if they fail to do so.
                        StartUnknown();
                    }
                    return dictionary;
                }
            }
        }

        /// <summary>
        /// Starts a server dictionary, which client dictionaries in other processes can then access.
        /// </summary>
        public static void StartServer()
        {
            lock (syncLock)
            {
                if (dictionary == null)
                {
                    dictionary = new ServerDictionary();                    
                }
            }
        }

        /// <summary>
        /// Starts a client dictionary to connect to the server.
        /// </summary>
        public static void StartClient()
        {
            lock (syncLock)
            {
                if (dictionary == null)
                {
                    dictionary = new ClientDictionary();
                    PipeSmokeTest();                   
                }                
            }
        }

        /// <summary>
        /// WCF doesn't violently fail on connecting client to server. Use this test for sanity, so it doesn't fail in random test code.
        /// </summary>
        private static void PipeSmokeTest()
        {
            string key="PipeTest";
            string value="Yup, the connection is really active!";
            dictionary[key] = value;            
        }

        /// <summary>
        /// Starts a Dictionary when you don't know if you should be server or client.
        /// This is a mechanism to avoid blowing up tests, but it causes issues, such as us having to swallow an exception.
        /// Don't use it if you don't have to.
        /// </summary>
        private static void StartUnknown()
        {
            lock (syncLock)
            {
                if (dictionary == null)
                {                    
                    Trace.WriteLine("Warning to Tester - You should be using the Explicit DictionaryStore.StartServer()/StartClient() API.");                
                    //Connect to a server if one is present, otherwise, take the initiative and be the server!
                    try
                    {                                 
                        StartClient();
                    }
                    catch (Exception) //TODO: Should be using the proper exceptions for failing to connect to server
                    {
                        dictionary.Dispose();
                        dictionary = null;
                        StartServer();
                    }
                }
            }
        }

        /// <summary>
        /// After you are done with a dictionary session, you *should* close it.
        /// Behavior is undefined & unsupported if you leave the pipe open.
        /// </summary>
        public static void Close()
        {
            lock (syncLock)
            {
                if (dictionary == null)
                {
                    throw new InvalidOperationException("A dictionary does not exist.");
                }
                ((IDisposable)dictionary).Dispose();
                dictionary = null;
            }
        }
    }
}