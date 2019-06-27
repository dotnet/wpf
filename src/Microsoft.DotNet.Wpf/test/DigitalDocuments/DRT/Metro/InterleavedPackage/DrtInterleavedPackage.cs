// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Text;                      // For StringBuilder
using System.Collections.Generic;

public class DrtInterleavedPackage
{
    //------------------------------------------------------
    //
    //   Test data
    //
    //------------------------------------------------------
    private List<string> _poemPieces = new List<string>(new string[]{
        "Thou, Linnet! in thy green array,\n",
        "Presiding Spirit here to-day,\n",
        "Dost lead the revels of the May;\n",
    });

    private List<string> _specPieces = new List<string>(new string[]{
        " Acording to the guidelines,",
        " a comment should not be followed by a comment",
        " unless it is followed by a comment that is not preceded by a comment."
    });

    private readonly string _poemUri = "/Wordsworth.txt";
    private readonly string _specUri = "/Spec.spec";

    // All relationships are created with the same target Uri.
    // Relationships only differ by name (aka type).

    private readonly Uri _targetUri = new Uri("/dummyUri", UriKind.Relative);

    private List<string> _packageRelationships = new List<string>(new string[]{
        "Bunny"
    });

    private List<string> _specRelationships = new List<string>(new string[]{
        "Introduction",
        "Appendix"
    });


    //------------------------------------------------------
    //
    //   Public methods
    //
    //------------------------------------------------------
    public static int Main(string[] argv)
    {
        Console.WriteLine("Interleaving DRT starting... use -verbose for full output.");
        DrtInterleavedPackage testObject = new DrtInterleavedPackage();

        testObject.Init();
        testObject.ProcessArguments(argv);
        int status = testObject.RunTest();

        if (status == 0)
            Console.WriteLine("SUCCESS");
        else
            Console.WriteLine("FAILED");

        return status;
    }

    //------------------------------------------------------
    //
    //   Private methods
    //
    //------------------------------------------------------
    private static void Assert(bool assertion)
    {
        if (!assertion)
            throw new Exception("Assertion failed.");
    }

    private void ProcessArguments(string[] argv)
    {
        foreach (string arg in argv)
        {
            if (arg[0] != '/' && arg[0] != '-')
            {
                Usage();
            }

            switch (arg.Substring(1).ToLower())
            {
                case "verbose":
                    _verbose = true;
                    break;

                default:
                    Console.Error.WriteLine("Warning: Unknown flag '{0}' is ignored.", arg.Substring(1));
                    break;
            }
        }
    }

    private void Usage()
    {
        Console.Error.WriteLine("Usage: DrtInterleavedPackage [-verbose]\n" +
            " -verbose          Produce loquacious output.");
        ExitWithError(1);
    }

    // Return 0 for no error.
    private int RunTest()
    {
        int status1 = CreateInterleavedPackage();
        Console.WriteLine("\nCreation of an interleaved package {0}.",
            status1 == 0 ? "succeeded" : "failed");

        int status2 = EditInterleavedPackage();
        Console.WriteLine("\nEditing of the interleaved package {0}.",
            status2 == 0 ? "succeeded" : "failed");

        return status1 + status2;
    }

    // Return 0 for no error, greater than 0 for error.
    private int CreateInterleavedPackage()
    {
        if (_verbose)
        {
            Console.WriteLine("\nCreateInterleavedPackage");
        }

        using (var fileStream = new FileStream("interleaved.zip", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        using (Package package = Package.Open(fileStream, FileMode.Create, FileAccess.ReadWrite))
        {
            // Add 2 parts, and fill them in an interleaved fashion.

            PackagePart poemPart = package.CreatePart(new Uri(_poemUri, UriKind.Relative), "text/plain", CompressionOption.Normal);
            Stream partStream1 = poemPart.GetStream(FileMode.Create);
            StreamWriter partWriter1 = new StreamWriter(partStream1);

            PackagePart specPart = package.CreatePart(new Uri(_specUri, UriKind.Relative), "text/plain");
            Stream partStream2 = specPart.GetStream(FileMode.Create);
            StreamWriter partWriter2 = new StreamWriter(partStream2);

            partWriter1.Write(_poemPieces[0]);
            partWriter1.Flush();
            partWriter2.Write(_specPieces[0]);
            partWriter2.Flush();
            partWriter2.Write(_specPieces[1]);
            partWriter2.Flush();
            partWriter2.Write(_specPieces[2]);
            partWriter1.Write(_poemPieces[1]);
            partWriter1.Flush();
            partWriter2.Flush(); // A no-op.
            partWriter1.Flush(); // A no-op.
            partWriter1.Write(_poemPieces[2]);
            partWriter2.Close();
            partWriter1.Close();

            // Add relationships.
            for (int i = 0; i < _packageRelationships.Count; ++i)
            {
                package.CreateRelationship(_targetUri, TargetMode.Internal, _packageRelationships[i]);
            }
            for (int i = 0; i < _specRelationships.Count; ++i)
            {
                specPart.CreateRelationship(_targetUri, TargetMode.Internal, _specRelationships[i]);
            }
        }
        
        if (!CheckContentOfParts())
            return 1;

        if (!CheckRelationships())
            return 1;
        
        return 0;
    }

    // Return 0 for no error, greater than 0 for error.
    private int EditInterleavedPackage()
    {
        if (_verbose)
        {
            Console.WriteLine("\nEditInterleavedPackage");
        }

        // Make a copy of the unedited package.

        // Open the package for edit.
        using (Package package = Package.Open("interleaved.zip", FileMode.Open, FileAccess.ReadWrite))
        {
            // Correct the spelling of "Acording" using the initial extra space.
            PackagePart specPart = package.GetPart(new Uri(_specUri, UriKind.Relative));
            using (Stream specStream = specPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
            {
                specStream.Write(StringToByteArray("According"), 0, "According".Length);
                specStream.Flush();

                // Update _specPieces to reflect the edit.
                _specPieces[0] = "According" + _specPieces[0].Substring("According".Length);

                // Read across contiguous pieces.
                CheckContent(specStream, 10, _specPieces[0].Substring(10) + _specPieces[1].Substring(0, 17));
            }

            // Add to the poem, after using GetParts to retrieve it.
            PackagePart poemPart = null;
            foreach (PackagePart p in package.GetParts())
            {
                if (string.Compare(p.Uri.OriginalString, _poemUri, StringComparison.Ordinal) == 0)
                {
                    poemPart = p;
                    break;
                }
            }
            if (poemPart == null)
            {
                Console.Error.WriteLine("Unable to retrieve part \"/{0}\" from the package.", _poemUri);
                return 1; // error
            }

            Stream poemStream = poemPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            long originalTextSize = poemStream.Length;
            poemStream.Seek(0, SeekOrigin.End);
            string poemSequel = "    And this is thy dominion.\n";

            poemStream.Write(StringToByteArray(poemSequel), 0, poemSequel.Length);
            poemStream.Flush();

            // Update _poemPieces to reflect the append.
            _poemPieces.Add(poemSequel);

            // Add a package relationship.
            _packageRelationships.Add("Equivocation");
            package.CreateRelationship(_targetUri, TargetMode.Internal,
                _packageRelationships[_packageRelationships.Count - 1]);

            // Remove a part relationship.
            string typeToRemove = _specRelationships[0];
            _specRelationships.RemoveAt(0);
            string id = null;
            foreach (PackageRelationship rel in specPart.GetRelationshipsByType(typeToRemove))
            {
                Assert(id == null);
                id = rel.Id;
            }
            specPart.DeleteRelationship(id);
        }

       
        if (!CheckContentOfParts())
            return 1;

        if (!CheckRelationships())
            return 1;

        return 0;
    }

    private bool CheckContent(Stream stream, long startPosition, string expectedString)
    {
        long savedPosition = stream.Position;

        stream.Position = startPosition;
        byte[] IOBuf = new byte[expectedString.Length];
        int bytesRead = stream.Read(IOBuf, 0, expectedString.Length);
        Assert(bytesRead == expectedString.Length);
        string stringRead = ByteArrayToString(IOBuf, expectedString.Length);
        bool success = (string.Compare(stringRead, expectedString, StringComparison.Ordinal) == 0);
        
        if (!success || _verbose)
        {
            Console.WriteLine("Expected string: \"{0}\"", expectedString);
        }
        if (!success)
        {
            Console.WriteLine("Actual string:   \"{0}\"", stringRead);
            Console.WriteLine(" =>> ERROR.");
        }
        else if (_verbose)
        {

            Console.WriteLine(" -> Correctly read.");
        }

        stream.Position = savedPosition;
        return success;
    }

    private bool CheckContentOfParts()
    {
        bool success = true;
        using (var fileStream = new FileStream("interleaved.zip", FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        using (Package package = Package.Open(fileStream, FileMode.Open, FileAccess.ReadWrite))
        {
            // Check the current content of part _poemUri.
            PackagePart poemPart = package.GetPart(new Uri(_poemUri, UriKind.Relative));
            success = CheckPartContent(poemPart, _poemPieces);

            // Check the current content of part _specUri.
            PackagePart specPart = package.GetPart(new Uri(_specUri, UriKind.Relative));
            success &= CheckPartContent(specPart, _specPieces);
        }

        return success;
    }

    private bool CheckRelationships()
    {
        bool success = true;

        using (Package package = Package.Open("interleaved.zip", FileMode.Open, FileAccess.Read))
        {
            PackagePart poemPart = package.GetPart(new Uri(_poemUri, UriKind.Relative));
            PackagePart specPart = package.GetPart(new Uri(_specUri, UriKind.Relative));

            int numRelationships = 0;
            foreach (string name in _packageRelationships)
            {
                ++numRelationships;
                if (Count(package.GetRelationshipsByType(name)) != 1)
                {
                    Console.Error.WriteLine("Found {0} instead of exactly 1 package relationship with name {1}",
                        Count(package.GetRelationshipsByType(name)), name);
                    success = false;
                }
                else if (_verbose)
                {
                    Console.WriteLine("Package contains relationship {0}.", name);
                }
            }
            if (numRelationships != Count(package.GetRelationships()))
            {
                Console.Error.WriteLine("Found {0} instead of {1} package relationships.",
                    Count(package.GetRelationships()), numRelationships);
            }

            numRelationships = 0;
            foreach (string name in _specRelationships)
            {
                ++numRelationships;
                if (Count(specPart.GetRelationshipsByType(name)) != 1)
                {
                    Console.Error.WriteLine("Found {0} instead of exactly 1 specPart relationship with name {1}",
                        Count(specPart.GetRelationshipsByType(name)), name);
                    success = false;
                }
                else if (_verbose)
                {
                    Console.WriteLine("SpecPart contains relationship {0}.", name);
                }
            }
            if (numRelationships != Count(specPart.GetRelationships()))
            {
                Console.Error.WriteLine("Found {0} instead of {1} specPart relationships.",
                    Count(specPart.GetRelationships()), numRelationships);
            }
        }

        return success;
    }

    private bool CheckPartContent(PackagePart part, List<string> expectedContentFragments)
    {
        bool success = true;
        long readOffset = 0;
        using (Stream partStream = part.GetStream(FileMode.Open, FileAccess.Read))
        {
            for (int i = 0; i < expectedContentFragments.Count; ++i)
            {
                success &= CheckContent(partStream, readOffset, expectedContentFragments[i]);
                readOffset += expectedContentFragments[i].Length;
            }
        }

        return success;
    }


    // The strings we use are ANSI, which is a subset of UTF8, which is the default
    // for the text writer; so these conversions are correct.
    private byte[] StringToByteArray(string s)
    {
        byte[] byteArray = new byte[s.Length];
        for (int i = 0; i < s.Length; ++i)
        {
            byteArray[i] = (byte)(s[i] & 0xFF);
        }
        return byteArray;
    }

    private string ByteArrayToString(byte[] byteArray, int numBytesToConvert)
    {
        StringBuilder representation = new StringBuilder(numBytesToConvert);
        for (int i = 0; i < numBytesToConvert; i++)
        {
            char c = (char)byteArray[i];
            representation.Append(c);
        }
        return representation.ToString();
    }

    private void Init()
    {
        // Direct error output to DrtInterleavedPackage.log.
        _stderr = Console.Error;
        Console.SetError(new StreamWriter(new FileStream("DrtInterleavedPackage.log", FileMode.Create, FileAccess.Write, FileShare.Read)));
    }


    private void ExitWithError(int executionStatus)
    {
        Assert(executionStatus != 0);
        DumpLogToConsole();
        Environment.Exit(executionStatus);
    }

    private void DumpLogToConsole()
    {
        Console.Error.Flush();
        using(StreamReader messageStream = new StreamReader(
            new FileStream("DrtInterleavedPackage.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
       {
           string messages = messageStream.ReadToEnd();
           _stderr.Write(messages);
       }
    }

    private static int Count(PackageRelationshipCollection c)
    {
        int count = 0;
        foreach (PackageRelationship rel in c)
            ++count;

        return count;
    }

    //------------------------------------------------------
    //
    //   Fields
    //
    //------------------------------------------------------
    private bool    _verbose = false;
    private TextWriter      _stderr;
}


