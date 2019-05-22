//------------------------------------------------------------------------------
//

//
// Description: Schema resource generator
//
//
//
//
//---------------------------------------------------------------------------

namespace MS.Internal.SchemaPP.Main
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
	using System.Resources;
	using System.Xml;

    public class SchemaPP
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        private SchemaPP()
        {
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        //+-----------------------------------------------------------------------------
        //
        //  Member:    Main
        //
        //  Synopsis:  The Main method for the SchemaPP project.
        //             Does the following:
        //
        //             1. Read the given XSD source file for preprocessing
        //             2. Writes out a preprocessed XSD file
        //
        //------------------------------------------------------------------------------

        public static void Main(string[] args)
        {
//            System.Diagnostics.Debugger.Break();
			if (args.GetLength(0)!= 2 )
			{
				Console.WriteLine("Usage: SchemaPP.exe <input.src> <schema.xsd>");
				return;
			}

            StreamReader input = File.OpenText(args[0]);
            StreamWriter output = File.CreateText(args[1]);
            string line;

            ArrayList al = new ArrayList();

            while ((line = input.ReadLine()) != null)
            {
                line = line.TrimEnd(new char[] { ' ' });
                
                bool lineBreak = false;
                string comment = (string)line.Clone();

                while (line.EndsWith("\\"))
                {
                    string next = input.ReadLine();
                    next = next.TrimEnd(new char[] { ' ' });
                    
                    comment = comment + "\r\n" + next;

                    next = next.TrimStart(new char[] { ' ' });

                    line = line.Substring(0, line.Length - 1) + next;
                    lineBreak = true;
                }

                string org = (string)line.Clone();
                for (int i = 0; i < al.Count; i += 2)
                {
                    line = line.Replace((string)al[i], (string)al[i + 1]);
                }
                if (org.Length != line.Length || lineBreak)
                {
                    output.WriteLine("<!--");
                    output.WriteLine(comment);
                    output.WriteLine("-->");
                }

                if (line.Contains("<!--") && line.Contains("DEFINE"))
                {
                    int start;
                    int end;
                    if ((start = line.IndexOf('[')) > 0 && (end= line.IndexOf(']',start+1))> 0)
                    {
                        string name = line.Substring(start, end - start + 1);

                        if ((start = line.IndexOf('\"',end+1)) > 0 && (end = line.IndexOf('\"', start+1)) > 0)
                        {
                            string value = line.Substring(start+1, end - start - 1);
                            al.Add(name);
                            al.Add(value);
                        }
                    }
                }

                output.WriteLine(line);
            }

            input.Close();
            output.Close();
            
            Console.WriteLine("Schema preprocess done.");
        }
        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        #endregion Private Methods
    }
}

