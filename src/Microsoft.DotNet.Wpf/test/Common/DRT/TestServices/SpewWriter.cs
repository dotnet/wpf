//-----------------------------------------------------------------------------
//
// <copyright file="SpewWriter.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// Description:
//  This class helps DRTs to produce minimal spew. A DRT should create an
//  instance of this class:
//
//      using MS.Utility;
//
//      private static SpewWriter Log = new SpewWriter();
//
//  It should check the command line for the /verbose switch, and set
//  Log.Verbose = true if the switch is found.
//
//  It should call Log.Banner(<testName>, <owner>) to produce the standard
//  introductory line
//
// 	    Container DRTs starting [rogerch]... use /verbose for full output.
//
//  Finally, it should call Log.AlwaysWriteLine() for output that must always
//  appear, or Log.WriteLine() for output that appears only if Verbose is true.
//
// History:
//  05/20/2002: RogerCh:  Initial implementation.
//  06/03/2003: LGolding: Split into separate file.
//
//-----------------------------------------------------------------------------

using System;
using System.Text;

namespace MS.Utility
{
    internal class SpewWriter
    {
        internal void Banner(string drtName, string owner)
        {
            StringBuilder sb = new StringBuilder();
            if (drtName != null)
                sb.Append(drtName + " ");
            sb.Append("DRT starting");
            if (owner != null)
                sb.Append(" [" + owner + "]");
            sb.Append("...");
            if (!Verbose)
                sb.Append(" use /verbose for full output.");
            AlwaysWriteLine(sb.ToString());
        }

        internal void WriteLine()
        {
            if (_verbose)
                Console.WriteLine();
        }

        internal void Write(string outText)
        {
            if (_verbose)
                Console.Write(outText);
        }

        internal void WriteLine(string outText)
        {
            if (_verbose)
                Console.WriteLine(outText);
        }

        // Single param covers 99% of cases
        internal void WriteLine(string outText, object outInfo)
        {
            if (_verbose)
                Console.WriteLine(outText, outInfo);
        }
    
        internal void AlwaysWriteLine()
        {
            Console.WriteLine();
        }
    
        internal void AlwaysWriteLine(string outText)
        {
            Console.WriteLine(outText);
        }

        internal void AlwaysWriteLine(string outText, object outInfo)
        {
            Console.WriteLine(outText, outInfo);
        }

        internal bool Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }
    
        private bool _verbose;
    }
}
