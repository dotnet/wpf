// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections.Generic;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Holder for command-line paramters and script variables
    /// </summary>
    public class TokenList : Dictionary<string, string>
    {
        /// <summary/>
        public TokenList(string[] args)
        {
            foreach (string s in args)
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                if (s[0] != '/' && s[0] != '-')
                {
                    continue;
                }

                string s1 = s.Substring(1, s.Length - 1);
                int index = s1.IndexOfAny(new char[] { ':', '=' });

                if (index < 0)
                {
                    // A parameter without a value is enabling something. Set value = true
                    Add(s1.Trim(), "true");
                }
                else
                {
                    Add(s1.Substring(0, index).Trim(),
                         s1.Substring(index + 1, s1.Length - (index + 1)).Trim());
                }
            }
        }

        /// <summary/>
        public TokenList(XmlElement element)
        {
            foreach (XmlAttribute att in element.Attributes)
            {
                Add(att.Name, att.Value.Trim());
            }
        }

        /// <summary/>
        protected TokenList(TokenList otherList)
        {
            foreach (string key in otherList.Keys)
            {
                this.Add(key, otherList[key]);
            }
        }

        /// <summary/>
        public string GetValue(string key)
        {
            if (this.ContainsKey(key))
            {
                return this[key];
            }
            return null;
        }

        /// <summary/>
        public void SetClassName(string name)
        {
            if (this.ContainsKey("Class"))
            {
                this["Class"] = name;
            }
            else
            {
                this.Add("Class", name);
            }
        }

        /// <summary/>
        public void Merge(TokenList otherList)
        {
            foreach (string key in otherList.Keys)
            {
                if (this.ContainsKey(key))
                {
                    this[key] = otherList[key];
                }
                else
                {
                    this.Add(key, otherList[key]);
                }
            }
        }

        /// <summary/>
        public static TokenList operator +(TokenList list1, TokenList list2)
        {
            TokenList result = new TokenList(list1);
            result.Merge(list2);
            return result;
        }
    }
}

