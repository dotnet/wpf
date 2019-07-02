// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Collections;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Command Line Parameter class
    /// </summary>
    public class CLParam
    {

        const string sParamMulErr = "  Parameter error: multiplicity should be ";
        const string sParamMulInst = " instead of ";
        /// <summary>
        /// The type of param passed in
        /// </summary>
        public enum ParamType
        {
            /// <summary>
            /// Param is of type integer
            /// </summary>
            tInt,
            /// <summary>
            /// Param is of type float
            /// </summary>
            tFloat,
            /// <summary>
            /// Param is of type string
            /// </summary>
            tString,
            /// <summary>
            /// Param is of type image
            /// </summary>
            tImage
        };
        /// <summary>
        /// The number of parameter expected
        /// </summary>
        public enum ParamMultiplicity
        {
            /// <summary>
            /// One (and only one) param must be define
            /// </summary>
            m_11,
            /// <summary>
            /// One or no param must be define
            /// </summary>
            m_01,
            /// <summary>
            /// None or many param must be defined
            /// </summary>
            m_0n,
            /// <summary>
            /// One or more param must be defined
            /// </summary>
            m_1n
        };

        /// <summary>
        /// The type of the parameter
        /// </summary>
        public ParamType mType;
        /// <summary>
        /// Placeholder for the param passed in
        /// </summary>
        public object    mParam;
        /// <summary>
        /// Description of the parameter
        /// </summary>
        public string    mHelp="";
        /// <summary>
        /// Regular expression for validation of the param
        /// </summary>
        public Regex     mValid =null;
        /// <summary>
        /// Number of param expected for this name
        /// </summary>
        public ParamMultiplicity mMult=ParamMultiplicity.m_11;
        /// <summary>
        /// Name of the param
        /// </summary>
        public string    mName ="";
        /// <summary>
        /// Default ?
        /// </summary>
        public string    mDefault=null;

        static string ParamMultiplicityToString(ParamMultiplicity mul)
        {
            string vret = "";
            if(mul==ParamMultiplicity.m_01)
            {
            vret = "0..1";
            }
            else if(mul==ParamMultiplicity.m_11)
            {
            vret = "1";
            }
            else if(mul==ParamMultiplicity.m_0n)
            {
            vret = "0..n";
            }
            else if(mul==ParamMultiplicity.m_1n)
            {
            vret = "1..n";
            }
            else
            {
                throw new Exception("<bummer!> "+mul+" not handled");
            }
            return vret;
        }

        static Hashtable mParams=new Hashtable();

        /// <summary>
        /// Error string about misusage
        /// </summary>
        public static string usage = "<!bummer> undefined usage";

        /// <summary>
        /// Constructor for the Command line param
        /// </summary>
        /// <param name="name">name of the param</param>
        /// <param name="type">type of param expected</param>
        /// <param name="help">help string about this param</param>
        
        public CLParam(string name,ParamType type, string help): this(name,type,help,null) {}
        /// <summary>
        /// Constructor for the Command line param
        /// </summary>
        /// <param name="name">name of the param</param>
        /// <param name="type">type of param expected</param>
        /// <param name="help">help string about this param</param>
        /// <param name="valid">A regular expression string to validate the param</param>
        public CLParam(string name,ParamType type, string help,string valid)
        {
            mName=name;
            mType = type;
            mHelp = help;
            
            if(mParams[name]==null)
            {
                if(mMult==ParamMultiplicity.m_0n||mMult==ParamMultiplicity.m_1n)
                {
                    mParam = new ArrayList();
                }
                mParams.Add(name,this);
                if(valid !=null)
                {
                    mValid = new Regex(valid,RegexOptions.Compiled);
                }
            }
            else 
            {
                throw new Exception(name+" Param already exists");
            }
        }

        /// <summary>
        /// Print a string about on to use this class
        /// </summary>
        static public void ShowUsage()
        {
            Console.WriteLine();
            if(usage != null)
            {
                Console.WriteLine(usage);
            }
            Console.WriteLine();
                
            Console.WriteLine("Parameters :\n");
            
            string []tparn = new string[mParams.Count];
            int idx = 0;
            foreach(string str in mParams.Keys)
            {
                tparn[idx++] = str;
            }
            Array.Sort(tparn);

            foreach(string tparname in tparn)
            {
                CLParam clp = (CLParam)mParams[tparname];
                string defaultpv = string.Empty;
                if(clp.mDefault != null)
                {
                    defaultpv = "(default=" + clp.mDefault + ")";
                }
                Console.WriteLine(string.Format("  {0,-12} {1,-4} {2}", clp.mName, ParamMultiplicityToString(clp.mMult), clp.mHelp + " " + defaultpv));
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Retrieve a paramter from the static list
        /// </summary>
        /// <param name="name">the name of the param to be retrieved</param>
        /// <returns>The param(s) retrieved</returns>
        static public object getParam(string name)
        {
            object vret = null;
            CLParam lpa = (CLParam) mParams[name];
            if(lpa != null)
            {
                vret = lpa.mParam;
            }
            return vret;
        }

        static private object ParseArgVal(CLParam lpar,string val)
        {
            if(lpar==null){throw new ArgumentNullException("Param lpar");}
            if(val==null){throw new ArgumentNullException("string val");}
            
            bool goon = true;
            if(lpar.mValid !=null)
            {
                if(lpar.mValid.Matches(val).Count!=1)
                {
                    goon = false;
                }
            } 

            object nobj = null;
            if(goon ==true)
            {
                if(lpar.mType == ParamType.tString)     {nobj=val;}
                else if(lpar.mType == ParamType.tInt)   {nobj=int.Parse(val);}
                else if(lpar.mType == ParamType.tFloat) {nobj=float.Parse(val);}
                else if(lpar.mType == ParamType.tImage) {nobj=new Bitmap(val);}
                else {throw new Exception(lpar.mType.ToString()+" :Type not handled ");}
            }

            return nobj;
        }

        static private Hashtable mParCount=new Hashtable();

        /// <summary>
        /// Parse/extract the arguments
        /// </summary>
        /// <param name="args">the list of arument to be parsed</param>
        /// <returns>true if the argument matched all param defined, false otherwise</returns>
        static public bool ParseArgs(string []args)
        {
            bool valid = true;
            Regex lreg = new Regex("[-/](?<param>.+?)[:=](?<val>.+)",RegexOptions.Compiled);
            
            foreach(CLParam clp in mParams.Values)
            {
                mParCount.Add(clp,0);
            }

            foreach(string str in args)
            {
                Match m = lreg.Match(str);
                if ( m.Success ) 
                {
                    string param=m.Groups["param"].Value;
                    string val=m.Groups["val"].Value;

                    CLParam lpar = (CLParam)mParams[param];
                    if(lpar !=null)
                    {
                        object nobj =ParseArgVal(lpar,val);
                        if(nobj !=null)
                        {
                            ArrayList arl = lpar.mParam as ArrayList;
                            if(arl != null) 
                            {
                                arl.Add(nobj);
                            }
                            else 
                            {
                                lpar.mParam = nobj;
                            }
                                
                            int count = (int)mParCount[lpar];
                            mParCount[lpar]=++count;
                        }
                        else 
                        {
                            Console.WriteLine(val+" value parsing error");
                            valid = false;
                        }
                    }
                    else 
                    {
                        Console.WriteLine(str+" unknown option");
                        valid =false;
                    }
                }
                else
                {
                    Console.WriteLine(str+" not a valid param syntax: "+str);
                    valid =false;
                }
            }

            string errCount="";
            
            foreach(CLParam clp in mParams.Values)
            {
                int lcount = (int)mParCount[clp];

                if(lcount==0)
                {
                    if(clp.mDefault!=null)
                    {
                        object nobj =ParseArgVal(clp,clp.mDefault);
                        if(nobj !=null)
                        {
                            ArrayList arl = clp.mParam as ArrayList;
                            if(arl != null) 
                            {
                                arl.Add(nobj);
                            }
                            else 
                            {
                                clp.mParam = nobj;
                            }
                            lcount=1;
                        } 
                        else
                        {
                            Console.WriteLine(clp.mDefault+" value parsing error");
                            valid = false;
                        }
                    }
                }

                string lerr="";
                if(clp.mMult==ParamMultiplicity.m_01)
                {
                    if(lcount<0||lcount>1){lerr="0..1";}
                }
                else if(clp.mMult==ParamMultiplicity.m_11)
                {
                    if(lcount!=1){lerr="1";}
                }
                else if(clp.mMult==ParamMultiplicity.m_0n)
                {
                    if(lcount<0){lerr="0..n";}
                }
                else if(clp.mMult==ParamMultiplicity.m_1n)
                {
                    if(lcount<1){lerr="1..n";}
                }
                else
                {
                    throw new Exception("<bummer!> "+clp.mMult+" not handled");
                }

                if(lerr !="")
                {
                    errCount+="  "+string.Format("{0,-16}",clp.mName)+sParamMulErr+lerr+sParamMulInst+lcount+"\n";
                }
            }

            if(errCount!="")
            {
                Console.WriteLine("\n\n<Error(s) in the command Line arguments!>\n"+errCount);
                valid = false;
            }

            if(valid == false)
            {
                ShowUsage();
            }
            return valid;
        }
    }
}
