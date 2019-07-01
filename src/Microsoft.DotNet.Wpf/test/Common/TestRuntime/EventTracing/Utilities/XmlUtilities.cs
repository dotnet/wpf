// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Diagnostics;
using System.Text;

/// <summary>
/// The important thing about these general utilities is that they have only dependencies on mscorlib and
/// System (they can be used from anywhere).  
/// </summary>
[CLSCompliant(false)] 
public class XmlUtilities
{
    public static string OpenXmlElement(string xmlElement)
    {
        if (xmlElement.EndsWith("/>"))
            return xmlElement.Substring(0, xmlElement.Length - 2) + ">";
        int endTagIndex = xmlElement.LastIndexOf("</");
        Debug.Assert(endTagIndex > 0);
        while (endTagIndex > 0 && Char.IsWhiteSpace(xmlElement[endTagIndex - 1]))
            --endTagIndex;
        return xmlElement.Substring(0, endTagIndex);
    }

    public static string XmlQuote(object obj)
    {
        return XmlEscape(obj, true);
    }

    public static string XmlEscape(object obj, bool quote)
    {
        string str = obj.ToString();
        StringBuilder sb = null;
        string entity = null;
        int copied = 0;
        for (int i = 0; i < str.Length; i++)
        {
            switch (str[i])
            {
                case '&':
                    entity = "&amp;";
                    goto APPEND;
                case '"':
                    entity = "&quot;";
                    goto APPEND;
                case '\'':
                    entity = "&apos;";
                    goto APPEND;
                case '<':
                    entity = "&lt;";
                    goto APPEND;
                case '>':
                    entity = "&gt;";
                    goto APPEND;
                APPEND:
                    {
                        if (sb == null)
                        {
                            sb = new StringBuilder();
                            if (quote)
                                sb.Append('"');
                        }
                        while (copied < i)
                            sb.Append(str[copied++]);
                        sb.Append(entity);
                        copied++;
                    }
                break;
            }
        }

        if (sb != null)
        {
            while (copied < str.Length)
                sb.Append(str[copied++]);
            if (quote)
                sb.Append('"');
            return sb.ToString();
        }
        if (quote)
            str = "\"" + str + "\"";
        return str;
    }
    public static string XmlQuoteHex(uint value)
    {
        return "\"0x" + value.ToString("x").PadLeft(8, '0') + "\"";
    }
    public static string XmlQuoteHex(ulong value)
    {
        return "\"0x" + value.ToString("x").PadLeft(8, '0') + "\"";
    }
    public static string XmlQuoteHex(int value)
    {
        return XmlQuoteHex((uint)value);
    }
    public static string XmlQuoteHex(long value)
    {
        return XmlQuoteHex((ulong)value);
    }
    public static string XmlQuote(int value)
    {
        return "\"" + value + "\"";
    }
}
