// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Reflection;
using System.IO;

namespace Microsoft.Test.Stability
{
    /// <summary>
    /// LeakUtil Man Class: provides interfact for Memory Snap Shot.
    /// </summary>
    public class LeakUtility
    {
        private static MethodInfo _miCleanup = _miCleanup =
                typeof(BindingOperations).GetMethod("Cleanup",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        
        [DllImport("LeakUtil32.dll")]
        static extern unsafe void TakeSnapshot();

        private static void DumpHeap()
        {
            try
            {
                string FileName = Environment.CurrentDirectory + "\\ClrProfilerApi.dll";
                Assembly assembly = Assembly.LoadFile(FileName);
                if (assembly != null)
                {
                    Type type = assembly.GetType("ClrProfilerApi.CLRProfilerControlWithInProcess");
                    if (type != null)
                    {
                        //System.Console.WriteLine(type.FullName);
                        MethodInfo mi = type.GetMethod("DumpHeap", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                        Object tempObject = assembly.CreateInstance("ClrProfilerApi.CLRProfilerControlWithInProcess");
                        if (mi == null)
                        {
                            Console.WriteLine("ClrProfilerApi.CLRProfilerControlWithInProcess.DumpHeap method not found");
                        }
                        else
                        {
                            //Console.WriteLine("Calling the function");
                            mi.Invoke(tempObject, null);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Wrong ClrProfilerApi.dll read");
                    }
                }
                else
                {
                    System.Console.WriteLine("ClrProfilerApi.dll not found");
                }
            }
            catch (FileNotFoundException)
            {
                System.Console.WriteLine("ClrProfilerApi.dll not found");
            }
        }

        private static void DumpHeap(string Pid)
        {
            try
            {
                String[] parameters = { Pid };
                string FileName = Environment.CurrentDirectory + "\\ClrProfilerApi.dll";
                Assembly assembly = Assembly.LoadFile(FileName);
                if (assembly != null)
                {

                    Type type = assembly.GetType("ClrProfilerApi.CLRProfilerControlOutSideProcess");

                    if (type != null)
                    {
                        //System.Console.WriteLine(type.FullName);
                        MethodInfo mi = type.GetMethod("TakeSnapShot", BindingFlags.Public | BindingFlags.Instance);
                        Object tempObject = assembly.CreateInstance("ClrProfilerApi.CLRProfilerControlOutSideProcess");
                        //tempObject.
                        if (mi == null)
                        {
                            Console.WriteLine("ClrProfilerApi.CLRProfilerControlOutSideProcess.TakeSnapShot method not found");
                        }
                        else
                        {
                            Console.WriteLine("Calling the function");
                            try
                            {
                                mi.Invoke(tempObject, new string[] { Pid });
                            }
                            catch (Exception e)
                            {
                                System.Console.WriteLine(e.Message);
                                System.Console.WriteLine(e.StackTrace);
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Wrong ClrProfilerApi.dll read");
                    }
                }
                else
                {
                    System.Console.WriteLine("ClrProfilerApi.dll not found");
                }
            }
            catch (FileNotFoundException)
            {
                System.Console.WriteLine("ClrProfilerApi.dll not found");
            }
        }

       /// <summary>
       /// Forces garbage collection and prints out memory in use.
       /// </summary>
        public static void GetMemory()
        {
            long result;
            do
            {
                result = GC.GetTotalMemory(true);
                GC.WaitForPendingFinalizers();
            }while ((bool)_miCleanup.Invoke(null, null)); // while (BindingOperations.Cleanup())
            Console.WriteLine("Memory in Use:: " + result + " bytes");
        }

        /// <summary>
        /// Takes memory snapshot for the current process.
        /// </summary>
        public static void TakeSnapShot()
        {
            GetMemory();
            DumpHeap();
            TakeSnapshot();
        }

        /// <summary>
        /// Takes memory snapshot for the process with ID provided.
        /// </summary>
        /// <param name="pid">Process Id</param>
        public static void TakeSnapShot(string pid)
        {
            GetMemory();
            DumpHeap(pid);
            TakeSnapshot();
        }

    }
}
