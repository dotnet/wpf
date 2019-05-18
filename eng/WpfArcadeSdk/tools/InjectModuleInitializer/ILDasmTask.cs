using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WpfArcadeSdk.Build.Tasks
{
    /// <summary>
    /// Runs ILDAsm from the path supplied on the assembly supplied.
    /// </summary>
    public class ILDAsmTask : Task
    {
        [Required]
        public string ILDAsm { get; set; }

        [Required]
        public string Assembly { get; set; }

        [Required]
        public string Out { get; set; }

        /// <summary>
        /// Output in HTML format
        ///</summary>
        public bool Html { get; set; }

        /// <summary>
        ///  Output in rich text format
        ///</summary>
        public bool Rtf { get; set; }

        /// <summary>
        ///  Show actual bytes(in hex) as instruction comments
        ///</summary>
        public bool Bytes { get; set; }

        /// <summary>
        /// Show exception handling clauses in raw form
        ///</summary>
        public bool RawEH { get; set; }

        /// <summary>
        /// Show metadata tokens of classes and members
        ///</summary>
        public bool Tokens { get; set; }

        /// <summary>
        /// Show original source lines as comments
        ///</summary>
        public bool Source { get; set; }

        /// <summary>
        /// Include references to original source lines
        ///</summary>
        public bool LineNum { get; set; }

        /// <summary>
        ///  =<vis>[+<vis>...] Only disassemble the items with specified
        ///  visibility. (<vis> = PUB | PRI | FAM | ASM | FAA | FOA | PSC)
        /// </summary>
        public string Visibility { get; set; }

        /// <summary>
        /// Only disassemble the public items(same as /VIS= PUB)
        ///</summary>
        public bool PubOnly { get; set; }

        /// <summary>
        /// Include all names into single quotes
        ///</summary>
        public bool QuoteAllNames { get; set; }

        /// <summary>
        /// Suppress output of custom attributes
        ///</summary>
        public bool NoCA { get; set; }

        /// <summary>
        /// Output CA blobs in verbal form(default - in binary form)
        ///</summary>
        public bool CAVerbal { get; set; }

        /// <summary>
        /// Suppress disassembly progress bar window pop-up
        ///</summary>
        public bool NoBar { get; set; }

        /// <summary>
        /// Use UTF-8 encoding for output(default - ANSI)
        ///</summary>
        public bool Utf8 { get; set; }

        /// <summary>
        /// Use UNICODE encoding for output
        ///</summary>
        public bool Unicode { get; set; }

        /// <summary>
        /// Suppress IL assembler code output
        ///</summary>
        public bool NoIL { get; set; }

        /// <summary>
        /// Use forward class declaration
        ///</summary>
        public bool Forward { get; set; }

        /// <summary>
        /// Output full list of types(to preserve type ordering in round-trip)
        ///</summary>
        public bool TypeList { get; set; }

        /// <summary>
        /// Display.NET projection view if input is a.winmd file
        ///</summary>
        public bool Project { get; set; }

        /// <summary>
        /// Include file headers information in the output
        ///</summary>
        public bool Headers { get; set; }

        /// <summary>
        /// =<class>[::<method>[(< sig >)] Disassemble the specified item only
        ///</summary>
        public string Item { get; set; }

        /// <summary>
        /// Include statistics on the image
        ///</summary>
        public bool Stats { get; set; }

        /// <summary>
        /// Include list of classes defined in the module
        ///</summary>
        public bool ClassList { get; set; }

        /// <summary>
        /// Combination of /HEADER,/BYTES,/STATS,/CLASSLIST,/TOKENS
        ///</summary>
        public bool All { get; set; }

        /// <summary>
        ///  Options for EXE,DLL,OBJ and LIB files:
        ///     /METADATA[=< specifier >] Show MetaData, where<specifier> is:
        ///     /MDHEADER Show MetaData header information and sizes.
        ///     /HEX Show more things in hex as well as words.
        ///     /CSV Show the record counts and heap sizes.
        ///     /UNREX Show unresolved externals.
        ///     /SCHEMA Show the MetaData header and schema information.
        ///     /RAW Show the raw MetaData tables.
        ///     /HEAPS Show the raw heaps.
        ///     /VALIDATE Validate the consistency of the metadata.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Options for LIB files only:
        ///     /OBJECTFILE=< obj_file_name > Show MetaData of a single object file in library
        /// </summary>
        public string ObjectFile { get; set; }

        public override bool Execute()
        {
            try
            {
                string commandLine = "/OUT=" + Out;
                if (Html) commandLine += " /HTML";
                if (Rtf) commandLine += " /RTF";
                if (Bytes) commandLine += " /BYTES";
                if (RawEH) commandLine += " /RAWEH";
                if (Tokens) commandLine += " /TOKENS";
                if (Source) commandLine += " /SOURCE";
                if (LineNum) commandLine += " /LINENUM";
                if (Visibility != null && Visibility.Trim() != string.Empty) commandLine += " /VISIBILITY=" + Visibility;
                if (PubOnly) commandLine += " /PUBONLY";
                if (QuoteAllNames) commandLine += " /QUOTEALLNAMES";
                if (NoCA) commandLine += " /NOCA";
                if (CAVerbal) commandLine += " /CAVERBAL";
                if (NoBar) commandLine += " /NOBAR";
                if (Utf8) commandLine += " /UTF8";
                if (Unicode) commandLine += " /UNICODE";
                if (NoIL) commandLine += " /NOIL";
                if (Forward) commandLine += " /FORWARD";
                if (TypeList) commandLine += " /TYPELIST";
                if (Headers) commandLine += " /HEADERS";
                if (Item != null && Item.Trim() != string.Empty) commandLine += " /ITEM=" + Item;
                if (Stats) commandLine += " /STATS";
                if (ClassList) commandLine += " /CLASSLIST";
                if (All) commandLine += " /ALL";
                if (Metadata != null && Metadata.Trim() != string.Empty) commandLine += " /METADATA=" + Metadata;
                if (ObjectFile != null && ObjectFile.Trim() != string.Empty) commandLine += " /OBJECTFILE=" + ObjectFile;

                ProcessStartInfo startInfo = new ProcessStartInfo(ILDAsm);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = Assembly + " " + commandLine;


                Log.LogMessage("Starting process: " + startInfo.FileName + " " + startInfo.Arguments);

                Process.Start(startInfo).WaitForExit();
                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString() + e.StackTrace);
                return false;
            }
        }
    }
}
