// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;

namespace mcwpf.Util
{
    public static class CommandLineParser
    {
        /* command line looks like this:
         * this.exe  -a foo -b /c /d bar -bas foo bar
         * result:
         * [a] = foo
         * [b]
         * [c]
         * [d] = bar
         * [bas] = foo bar
         */

        public static Dictionary<string, string> Parse(string[] commandLine)
        {
            return Parse(commandLine, false);
        }

        public static Dictionary<string, string> Parse(string[] commandLine, bool singleParameterPerArg)
        {
            var dic = new Dictionary<string, string>(commandLine.Length);

            string lastCommand = null;
            foreach (string s in commandLine)
            {
                if (s[0] == '-' ||
                    s[0] == '/')
                {
                    lastCommand = s.Substring(1);
                    dic[lastCommand] = string.Empty;
                }
                else if (lastCommand == null)
                {
                    dic[s] = string.Empty;
                }
                else
                {
                    string lastKey = dic[lastCommand];
                    if (string.IsNullOrEmpty(lastKey))
                    {
                        dic[lastCommand] = s;
                    }
                    else
                    {
                        dic[lastCommand] += string.Format(" {0}", s);
                    }

                    if (singleParameterPerArg)
                    {
                        lastCommand = null;
                    }
                }
            }

            return dic;
        }
    }
}
