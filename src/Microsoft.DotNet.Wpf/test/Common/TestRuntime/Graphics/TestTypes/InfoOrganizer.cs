// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;
using System.Windows.Media;

using FileMode = System.IO.FileMode;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
#else
using TrustedFileStream = System.IO.FileStream;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Turn command-line params into useful data
    /// </summary>
    public class InfoOrganizer
    {
        /// <summary/>
        public InfoOrganizer(TokenList tokens, string script)
        {
            if (script == null)
            {
                CreateVariationFromTokens(tokens);
            }
            else
            {
                CreateVariationsFromScript(script, tokens);
            }
        }

        /// <summary/>
        public Variation[] Variations { get { return variations; } }
      
        private void CreateVariationFromTokens(TokenList tokens)
        {
            Variation.SetGlobalParameters(tokens);
            variations = new Variation[1];
            variations[0] = new Variation(tokens);
        }

        private void CreateVariationsFromScript(string scriptFile, TokenList commandLineTokens)
        {
            TrustedFileStream stream = new TrustedFileStream(scriptFile, FileMode.Open);
            XmlDocument doc = new XmlDocument();
            doc.Load(PT.Untrust(stream));

            XmlElement init = doc["INIT"] as XmlElement;
            if (init == null)
            {
                throw new InvalidScriptFileException("script file: " + scriptFile + " is missing INIT element");
            }

            TokenList tokens = new TokenList(init);
            tokens.Merge(commandLineTokens);

            Variation.SetGlobalParameters(tokens);
            CreateVariations(init);
        }
        
        private void CreateVariations(XmlElement init)
        {
            ArrayList list = new ArrayList();

            for (XmlNode node = init["VARIATION"]; node != null; node = node.NextSibling)
            {
                XmlElement variation = node as XmlElement;
                if (variation == null || variation.Name != "VARIATION")
                {
                    continue;
                }

                Variation v = new Variation(new TokenList(variation));
                list.Add(v);
            }
            variations = new Variation[list.Count];
            list.CopyTo(variations);
        }

        private Variation[] variations;
    }
}
