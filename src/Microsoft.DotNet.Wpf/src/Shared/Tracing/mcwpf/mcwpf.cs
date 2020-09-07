// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Collections;
using System.CodeDom.Compiler;
using System.Reflection;

[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace mcwpf
{
    class Program
    {
        private static XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events";
        private static string AccessControl = "internal";
        private const string DefaultIndent = "    ";

        static int Main(string[] cmdline)
        {
            var args = Util.CommandLineParser.Parse(cmdline);

            string filename;
            string templateName;
            if (!args.TryGetValue("man", out filename) ||
                !args.TryGetValue("template", out templateName) ||
                !File.Exists(filename) || !File.Exists(templateName))
            {
                Usage();
                return 0;
            }
            EnsureDefaults(args);

            if (args.ContainsKey("public")) AccessControl = "public";
            if (args.ContainsKey("private")) AccessControl = "private";
            if (args.ContainsKey("protected")) AccessControl = "protected";
            if (args.ContainsKey("internal")) AccessControl = "internal";

            string keyword = null;
            args.TryGetValue("keyword", out keyword);

            XDocument doc = XDocument.Load(XmlReader.Create(File.OpenRead(filename)));

            // Search the file for the token [[MCWPF_GENERATE_CODE_HERE]] and generate code there
            StreamReader template = new StreamReader(File.OpenRead(templateName));
            using (StreamWriter writer = new StreamWriter(args["out"], false))
            {
                string line;
                while ((line = template.ReadLine()) != null)
                {
                    if (line.Contains("[[MCWPF_GENERATE_CODE_HERE]]"))
                    {
                        int indent = 0;
                        while (Char.IsWhiteSpace(line[indent])) indent++;
                        try
                        {
                            GenerateCode(doc, writer, line.Substring(0, indent), keyword);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            Console.WriteLine();
                            using (new ColorConsole(ConsoleColor.Yellow))
                            {
                                Console.WriteLine("Managed code generation failed.  You may need to rebuild mcwpf.exe");
                            }
                            return (-1);
                        }
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            return 0;
        }

        static void GenerateCode(XDocument doc, StreamWriter stream, string indent, string keyword)
        {
            IndentedTextWriter writer = new IndentedTextWriter(stream, indent);
            writer.Indent = 1;
            var providers = doc.Root.Elements(ns + "instrumentation").Elements(ns + "events").Elements(ns + "provider");

            // Built in levels
            var builtInLevels = new XElement[] {
                new XElement("level", new XAttribute("symbol", "LogAlways"), new XAttribute("value", 0)),
                new XElement("level", new XAttribute("symbol", "Critical"), new XAttribute("value", 1)),
                new XElement("level", new XAttribute("symbol", "Error"), new XAttribute("value", 2)),
                new XElement("level", new XAttribute("symbol", "Warning"), new XAttribute("value", 3)),
                new XElement("level", new XAttribute("symbol", "Info"), new XAttribute("value", 4)),
                new XElement("level", new XAttribute("symbol", "Verbose"), new XAttribute("value", 5)) };

            // Write Levels enum
            var levels = providers.Elements(ns + "levels").Elements(ns + "level");
            levels = builtInLevels.Concat(levels);
            WriteEnum("Level : byte", writer, levels, "symbol", "value", false);

            // Write Keywords enum
            var keywords = providers.Elements(ns + "keywords").Elements(ns + "keyword");
            WriteEnum("Keyword", writer, keywords, "symbol", "mask", true);

            var builtInOpcodes = new XElement[] {
                new XElement("opcode", new XAttribute("name", "win:Info"), new XAttribute("symbol", "Info"), new XAttribute("value", 0)),
                new XElement("opcode", new XAttribute("name", "win:Start"), new XAttribute("symbol", "Start"), new XAttribute("value", 1)),
                new XElement("opcode", new XAttribute("name", "win:Stop"), new XAttribute("symbol", "Stop"), new XAttribute("value", 2)),
                new XElement("opcode", new XAttribute("name", "win:DC_Start"), new XAttribute("symbol", "DC_Start"), new XAttribute("value", 3)),
                new XElement("opcode", new XAttribute("name", "win:DC_Stop"), new XAttribute("symbol", "DC_Stop"), new XAttribute("value", 4)),
                new XElement("opcode", new XAttribute("name", "win:Extension"), new XAttribute("symbol", "Extension"), new XAttribute("value", 5)),
                new XElement("opcode", new XAttribute("name", "win:Reply"), new XAttribute("symbol", "Reply"), new XAttribute("value", 6)),
                new XElement("opcode", new XAttribute("name", "win:Resume"), new XAttribute("symbol", "Resume"), new XAttribute("value", 7)),
                new XElement("opcode", new XAttribute("name", "win:Suspend"), new XAttribute("symbol", "Suspend"), new XAttribute("value", 8)),
                new XElement("opcode", new XAttribute("name", "win:Transfer"), new XAttribute("symbol", "Transfer"), new XAttribute("value", 9)),
            };

            // Write Opcode enum
            var opcodes = providers.Descendants(ns + "opcode");
            opcodes = builtInOpcodes.Concat(opcodes);
            // Enum values are used by value and never referenced.
            // WriteEnum("Opcode : byte", writer, opcodes, "symbol", "value", false);

            // Write Event enum
            var eventsFlat = providers.Elements(ns + "events").Elements(ns + "event");
            if (!string.IsNullOrEmpty(keyword))
            {
                eventsFlat = eventsFlat.Where((element) => element.Attribute("keywords").Value.Split(' ').Contains(keyword));
            }
            WriteEnum("Event : ushort", writer, eventsFlat, "symbol", "value", false);

            // A function to map an Event to a GUID
            var tasks = providers.Elements(ns + "tasks").Elements(ns + "task");
            var events = from e in eventsFlat
                          join t in tasks on e.Attribute("task").Value equals t.Attribute("name").Value
                          select new
                          {
                              Symbol     = e.Attribute("symbol").Value,
                              Value      = e.Attribute("value").Value,
                              Guid       = new Guid(t.Attribute("eventGUID").Value),
                              Task       = t.Attribute("value").Value,
                              TaskSymbol = t.Attribute("symbol").Value,
                              Opcode     = e.Attribute("opcode").Value,
                              Version    = e.Attribute("version").Value,
                              Keywords   = e.Attribute("keywords").Value,
                          };

            /*
             * Write out a function to map the Event to the task GUID
             *
             * internal Guid GuidForEvent(Event evt)
             * {
             *  switch(evt)
             *  {
             *      case Event.A: return new Guid(...); break;
             *      default: throw ArgumentException();
             *  }
             * }
             */
            WriteMapFunction("GetGuidForEvent", "Guid", "Event", writer, events, (e) => "Event." + e.Symbol, (e) => "// " + e.Guid, (e) =>
            {
                byte[] bytes = e.Guid.ToByteArray();
                Int32 intPart = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
                short shortPart1 = (short)(bytes[5] << 8 | bytes[4]);
                short shortPart2 = (short)(bytes[7] << 8 | bytes[6]);
                return string.Format("return new Guid(0x{0:X}, 0x{1:X}, 0x{2:X}, 0x{3:X}, 0x{4:X}, 0x{5:X}, 0x{6:X}, 0x{7:X}, 0x{8:X}, 0x{9:X}, 0x{10:X});",
                            intPart, shortPart1, shortPart2, bytes[8], bytes[9], bytes[0xA], bytes[0xB], bytes[0xC], bytes[0xD], bytes[0xE], bytes[0xF]);
            });

            // GetTaskForEvent - Map Event => Task ID
            WriteMapFunction("GetTaskForEvent", "ushort", "Event", writer, events, (e) => "Event." + e.Symbol, (e) => String.Empty, (e) => string.Format("return {0};", e.Task));

            // GetOpcodeForEvent - Map Event => Opcode
            Dictionary<string, int> OpcodeMap = new Dictionary<string, int>();
            foreach (var op in opcodes)
            {
                OpcodeMap[op.Attribute("name").Value] = Int32.Parse(op.Attribute("value").Value);
            }
            WriteMapFunction("GetOpcodeForEvent", "byte", "Event", writer, events.OrderBy(e => OpcodeMap[e.Opcode]), (e) => "Event." + e.Symbol, (e) => String.Empty, (e) => string.Format("return {0};", OpcodeMap[e.Opcode]));

            // GetVersionForEvent - Map Event => Version
            WriteMapFunction("GetVersionForEvent", "byte", "Event", writer, events.OrderBy(e => Int32.Parse(e.Version)), (e) => "Event." + e.Symbol, (e) => String.Empty, (e) => string.Format("return {0};", e.Version));

            // We could generate 1 switch instead of 3, but in my test this resulted in 18k of o code instead of 4k for all 3 methods.
            // Since switching on a consecutive integer range should be close to O(1) smaller code is the better option.  We also avoid the Array allocation.
            // WriteMapFunction("GetEventData", "int[]", "Event", writer, events, (e) => "Event." + e.Symbol, (e) => String.Empty, (e) => string.Format("return new int[] {{{0}, {1}, {2}}};", e.Task, OpcodeMap[e.Opcode], e.Version));



            // Check for a common error.  This can cause build breaks.
            foreach(var error in from e in events where e.Symbol == e.TaskSymbol select e)
            {
                using (new ColorConsole(ConsoleColor.Red))
                {
                    Console.WriteLine("\nError: event {0} (ID:{1}) has the same symbol as task {2} (ID:{3}).",
                        error.Symbol, error.Value, error.TaskSymbol, error.Task);
                }
            }
        }

        static void WriteMapFunction<T>(string funcName, string retType, string argType, TextWriter writer, IEnumerable<T> elements, Func<T, string> caseVal, Func<T, string> commentVal, Func<T, string> retVal)
        {
            writer.WriteLine();
            writer.WriteLine("{0} static {1} {2}({3} arg)", AccessControl, retType, funcName, argType);
            writer.WriteLine("{");
            writer.WriteLine(DefaultIndent + "switch(arg)");
            writer.WriteLine(DefaultIndent + "{");

            var elemArray = elements.ToArray();
            for (int x = 0; x < elemArray.Length; x++)
            {
                var cur = elemArray[x];
                writer.Write(DefaultIndent + DefaultIndent);
                writer.WriteLine("case {0}:", caseVal(cur));

                // Collapse duplicate ret values
                string retString = retVal(cur);
                bool writeRet = true;
                if ((x + 1) < elemArray.Length)
                {
                    // Getting the retVal string for the next element will cause retVal() to get
                    // executed twice as often as necessary.  An optimization would be to have a separate
                    // Func<T, bool> to indicate if the next ret will be the same without going though a string.
                    string nextRetString = retVal(elemArray[x + 1]);
                    if (retString == nextRetString)
                    {
                        writeRet = false;
                    }
                }

                if (writeRet)
                {
                    string comment = commentVal(cur);
                    if (!string.IsNullOrEmpty(comment))
                    {
                        writer.Write(DefaultIndent + DefaultIndent + DefaultIndent);
                        writer.WriteLine(commentVal(cur));
                    }
                    writer.Write(DefaultIndent + DefaultIndent + DefaultIndent);
                    writer.WriteLine(retString);
                }
            }

            writer.Write(DefaultIndent + DefaultIndent);
            writer.WriteLine("default: throw new ArgumentException(SR.Get(SRID.InvalidEvent),\"arg\");");
            writer.WriteLine(DefaultIndent + "}");
            writer.WriteLine("}");
        }

        static void WriteEnum(string enumName, TextWriter writer, IEnumerable<XElement> elements, string name, string value, bool flags)
        {
            writer.WriteLine();
            if (flags) writer.WriteLine("[Flags]");
            writer.Write(AccessControl);
            writer.Write(" enum ");
            writer.WriteLine(enumName);
            writer.WriteLine("{");
            foreach (var e in elements)
            {
                writer.Write(DefaultIndent);
                writer.Write(e.Attribute(name).Value);
                writer.Write(" = ");
                writer.Write(e.Attribute(value).Value);
                writer.WriteLine(",");
            }
            writer.WriteLine("}");
        }

        static void EnsureDefaults(Dictionary<string, string> args)
        {
            if (!args.ContainsKey("out"))
            {
                args["out"] = "wpf_instr.cs";
            }
        }

        static void Usage()
        {
            Console.WriteLine("mcwpf -man wpf_instr.man -template <filename> [-out filename] [-keywords <keyword>] [public|private|internal]");
        }
    }

    class ColorConsole : IDisposable
    {
        private ConsoleColor _originalColor;
        public ColorConsole(ConsoleColor foreGround)
        {
            _originalColor = Console.ForegroundColor;
            Console.ForegroundColor = foreGround;
        }

        public void Dispose()
        {
            Console.ForegroundColor = _originalColor;
        }
    }
}
