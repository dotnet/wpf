// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Test.FaultInjection.Constants;

namespace Microsoft.Test.FaultInjection.SignatureParsing
{    
    internal static class Signature
    {
        #region Public Members

        public static string ConvertSignature(string signature)
        {
            return ConvertSignature(signature, SignatureStyle.Formal);
        }

        public static string ConvertSignature(string signature, SignatureStyle style)
        {
            if (signature == null || signature == string.Empty)
            {
                throw new FaultInjectionException(ApiErrorMessages.MethodSignatureNullOrEmpty);
            }

            try
            {
                Lex lex = new Lex(signature);
                Yacc yacc = new Yacc(lex.LexedSignature, lex.Identifiers);
                return yacc.GetSignature(style);
            }
            catch (FaultInjectionException)
            {
                throw new FaultInjectionException(string.Format(CultureInfo.CurrentCulture, ApiErrorMessages.InvalidMethodSignature, signature));
            }
        }

        #endregion 
        
        #region Private Members

        private sealed class Lex
        {
            #region Private Data

            private readonly List<string> identifiers = null;
            private readonly string lexSignature = null;
            private const string separatorPattern = @"[.,<>\[\]\(\)]";
            // "ref", "out", "params" and "static" must be followed with a space in user style signature.
            // Thus they must be the last symbol after I have split the input by spaces.
            // To ensure this, pattern match them should have a '$' at the end.
            private const string staticPattern = @"static$";    // "static" will be lexed as a '!'
            private const string refPattern = @"(ref|out)$";    // "ref" and "out" will be lexed as a '&'
            private const string paramsPattern = @"params$";    // "params" will be lexed as a '%'
            private const string identifierPattern = @"[a-z|A-Z|_]\w*";

            #endregion

            #region Constructors

            // Do lexical parse in constructor
            public Lex(string signature)
            {
                lexSignature = string.Empty;
                identifiers = new List<string>();

                string[] segments = signature.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string segment in segments)
                {
                    string temp = segment;
                    while (temp != string.Empty)
                    {
                        string symbol = null;
                        if (ParseLexSymbol(separatorPattern, ref temp, out symbol))
                        {
                            lexSignature += symbol;
                            continue;
                        }

                        if (ParseLexSymbol(staticPattern, ref temp, out symbol))
                        {
                            lexSignature += '!';
                            continue;
                        }

                        if (ParseLexSymbol(refPattern, ref temp, out symbol))
                        {
                            lexSignature += '&';
                            continue;
                        }

                        if (ParseLexSymbol(paramsPattern, ref temp, out symbol))
                        {
                            lexSignature += '%';
                            continue;
                        }

                        if (ParseLexSymbol(identifierPattern, ref temp, out symbol))
                        {
                            if (BuiltInTypeHelper.AliasToFullName(symbol) != null)
                            {
                                lexSignature += symbol;
                            }
                            else
                            {
                                int i = identifiers.IndexOf(symbol);
                                if (i == -1)
                                {
                                    identifiers.Add(symbol);
                                    i = identifiers.Count - 1;
                                }
                                lexSignature += string.Format(CultureInfo.InvariantCulture, "#{0}#", i);
                            }
                            continue;
                        }

                        throw new FaultInjectionException();
                    }
                }
            }

            #endregion

            #region Public Members

            public string[] Identifiers { get { return identifiers.ToArray(); } }
            public string LexedSignature { get { return lexSignature; } }

            #endregion

            #region Private Members

            // Parse one lexical symbol by matching head of input string using regex pattern argument.
            private bool ParseLexSymbol(string pattern, ref string input, out string symbol)
            {
                symbol = null;

                Regex r = new Regex("^" + pattern);
                Match m = r.Match(input);
                if (m.Success)
                {
                    input = input.Remove(0, m.Length);
                    symbol = m.Value;
                }
                return (symbol != null);
            }

            #endregion  
        }

        private sealed class Yacc
        {
            #region Private Data

            private Dictionary<int, string> identifiers = new Dictionary<int, string>();
            private readonly Signature signature;

            #endregion

            #region Constructors

            public Yacc(string lexedSignature, string[] identifierArray)
            {
                for (int i = 0; i < identifierArray.Length; ++i)
                {
                    identifiers.Add(i, identifierArray[i]);
                }
                signature = new Signature(lexedSignature);
            }

            #endregion

            #region Public Members

            public string GetSignature(SignatureStyle style)
            {
                string result = signature.ToString(style);

                Regex identifiersPattern = new Regex(@"#(?<index>\d+)#");
                Match match = identifiersPattern.Match(result);
                while (match.Success)
                {
                    int index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
                    result = result.Replace(match.Value, identifiers[index]);
                    match = match.NextMatch();
                }

                return result;
            }

            #endregion 

            #region Private Members

            // A bunch of classes for grammar elements. To understand them, see the following pseudo-grammar:
            //
            //     Signature      =    TypeName.MethodName(ParaList) | !TypeName.MethodName(ParaList)
            //                                                        ~~~~~~~~~ Note: "!" for static method
            //     ParaList       =    ParaType,ParaType,ParaType ... ParaType
            //     ParaType       =    TypeName | &TypeName | %TypeName
            //                                   ~~~~~~~~~~~~~~~~~~~~~ Note: "&" for "ref|out" and "%" for params
            //     TypeName       =    PlainTypeName | PlainTypeName[][,,][]...
            //     PlainTypeName  =    Name.Name.Name ... Name | C# alias for built-in type
            //     MethodName     =    Name | .cctor
            //     Name           =    Identifier<TypeList>
            //     TypeList       =    TypeName,TypeName,TypeName ... TypeName
            //
            private sealed class Signature
            {
                #region Private Data

                internal readonly TypeName declaringType = null;
                internal readonly MethodName method = null;
                internal readonly ParaList parameters = null;
                internal readonly bool isStatic = false;
                internal readonly bool isConstructor = false;

                #endregion

                #region Public Members

                public Signature(string signature)
                {
                    Regex regex = new Regex(@"^(?<static>!?)(?<type>.+)\.(?<method>.+)\((?<paras>.*)\)$");
                    Match match = regex.Match(signature);

                    if (!match.Success)
                    {
                        throw new FaultInjectionException();
                    }

                    declaringType = new TypeName(match.Groups["type"].Value);
                    method = new MethodName(match.Groups["method"].Value);
                    if (match.Groups["paras"].Value != string.Empty)
                    {
                        parameters = new ParaList(match.Groups["paras"].Value);
                    }

                    // Is static method?
                    isStatic = (match.Groups["static"].Value != string.Empty);
                    // Is constructor?
                    if (declaringType.arraySuffix == null)
                    {
                        string methodName = method.name.identifier;
                        string className = declaringType.plainTypeName.className;
                        if (className == methodName)
                        {
                            isConstructor = true;
                        }
                    }
                }
                public string ToString(SignatureStyle style)
                {
                    string methodName = method.ToString(style);
                    if (isConstructor)
                    {
                        if (isStatic)
                        {
                            methodName = ".cctor";
                        }
                        else
                            methodName = ".ctor";
                    }

                    string result = declaringType.ToString(style) + "." + methodName;
                    if (style == SignatureStyle.Formal)
                    {
                        result += "(";
                        if (parameters != null)
                        {
                            result += parameters.ToString(style);
                        }
                        result += ")";
                    }
                    return result;
                }

                #endregion
            }

            private sealed class ParaList
            {
                #region Private Data

                internal readonly ParaType[] parameters;

                #endregion

                #region Public Members

                public ParaList(string paraList)
                {
                    string[] paraNames = SplitAtTopLevel(paraList, ',');
                    parameters = Array.ConvertAll<string, ParaType>(paraNames, delegate(string name) { return new ParaType(name); });
                    int i = Array.FindIndex(parameters, delegate(ParaType para) { return para.paramsFlag; });
                    if (i != -1 && i != parameters.Length - 1)
                    {
                        throw new FaultInjectionException();
                    }
                }

                public string ToString(SignatureStyle style)
                {
                    string[] temp = Array.ConvertAll<ParaType, string>(parameters, delegate(ParaType para) { return para.ToString(style); });
                    return ConcatenateList(temp, ",");
                }

                #endregion
            }

            private sealed class ParaType
            {
                #region Private Data

                internal readonly bool refFlag = false;
                internal readonly bool paramsFlag = false;
                internal readonly TypeName parameterTypeName;

                #endregion

                #region Contructors

                public ParaType(string para)
                {
                    refFlag = (para[0] == '&');
                    paramsFlag = (para[0] == '%');
                    if (refFlag || paramsFlag)
                    {
                        para = para.Remove(0, 1);
                    }

                    parameterTypeName = new TypeName(para);

                    if (paramsFlag)
                    {
                        string arraySuffix = parameterTypeName.arraySuffix;
                        if (arraySuffix == null || !arraySuffix.EndsWith("[]", StringComparison.Ordinal))
                        {
                            throw new FaultInjectionException();
                        }
                    }
                }

                #endregion

                #region Public Members

                public string ToString(SignatureStyle style)
                {
                    string result = parameterTypeName.ToString(style);
                    if (refFlag)
                    {
                        result += "&";
                    }
                    return result;
                }

                #endregion
            }

            private sealed class TypeName
            {
                #region Private Data

                internal readonly string arraySuffix;
                internal readonly PlainTypeName plainTypeName;

                #endregion

                #region Constructors

                public TypeName(string type)
                {
                    string plainType = type;

                    Regex arrayPattern = new Regex(@"\[,*\]$");
                    Match match = arrayPattern.Match(plainType);
                    while (match.Success)
                    {
                        plainType = plainType.Substring(0, plainType.Length - match.Length);
                        if (arraySuffix == null)
                        {
                            arraySuffix = string.Empty;
                        }
                        arraySuffix = match.Value + arraySuffix;
                        match = arrayPattern.Match(plainType);
                    }
                    plainTypeName = new PlainTypeName(plainType);
                }

                #endregion

                #region Public Members

                public string ToString(SignatureStyle style)
                {
                    return plainTypeName.ToString(style) + arraySuffix;
                }

                #endregion
            }

            private sealed class PlainTypeName
            {
                #region Private Data

                internal readonly Name[] names;
                internal readonly string builtInTypeFullName;
                internal readonly string className;

                #endregion

                #region Constructors

                public PlainTypeName(string plainType)
                {
                    builtInTypeFullName = BuiltInTypeHelper.AliasToFullName(plainType);
                    if (builtInTypeFullName == null)
                    {
                        string[] temp = SplitAtTopLevel(plainType, '.');
                        names = Array.ConvertAll<string, Name>(temp, delegate(string name) { return new Name(name); });
                        className = names[names.Length - 1].identifier;
                    }
                    else
                    {
                        string[] temp = builtInTypeFullName.Split('.');
                        className = temp[temp.Length - 1];
                    }
                }

                #endregion

                #region Public Membera

                public string ToString(SignatureStyle style)
                {
                    if (builtInTypeFullName != null)
                    {
                        return builtInTypeFullName;
                    }

                    string[] temp = Array.ConvertAll<Name, string>(names, delegate(Name name) { return name.ToString(style); });
                    return ConcatenateList(temp, ".");
                }

                #endregion
            }

            private sealed class MethodName
            {
                #region Private Data

                internal readonly Name name;

                #endregion

                #region Contructors

                public MethodName(string methodName)
                {
                    name = new Name(methodName);
                }

                #endregion

                #region Public Members

                public string ToString(SignatureStyle style)
                {
                    if (style == SignatureStyle.Com)
                    {
                        return name.identifier;
                    }
                    else
                        return name.ToString(style);
                }

                #endregion
            }

            private sealed class Name
            {
                #region Private Data

                internal readonly string identifier;
                internal readonly TypeList genericParameters;

                #endregion

                #region Constructors

                public Name(string name)
                {
                    Regex pattern = new Regex(@"^(?<identifier>#\d+#)(<(?<typelist>.+)>)?$");
                    Match match = pattern.Match(name);
                    if (!match.Success)
                    {
                        throw new FaultInjectionException();
                    }

                    identifier = match.Groups["identifier"].Value;

                    if (match.Groups["typelist"].Value != string.Empty)
                    {
                        genericParameters = new TypeList(match.Groups["typelist"].Value);
                    }
                    else
                    {
                        genericParameters = null;
                    }
                }

                #endregion

                #region Public Members

                public string ToString(SignatureStyle style)
                {
                    string result = identifier;
                    if (genericParameters != null)
                    {
                        if (style == SignatureStyle.Formal)
                        {
                            result += "<" + genericParameters.ToString(style) + ">";
                        }
                        else if (style == SignatureStyle.Com)
                            result += "`" + genericParameters.typeNames.Length.ToString(NumberFormatInfo.InvariantInfo);
                    }
                    return result;
                }

                #endregion
            }

            private sealed class TypeList
            {
                #region Private Data

                internal readonly TypeName[] typeNames;

                #endregion

                #region Constructors

                public TypeList(string typeList)
                {
                    string[] temp = SplitAtTopLevel(typeList, ',');
                    typeNames = Array.ConvertAll<string, TypeName>(temp, delegate(string type) { return new TypeName(type); });
                }

                #endregion

                #region Public Members

                public string ToString(SignatureStyle style)
                {
                    string[] temp = Array.ConvertAll<TypeName, string>(typeNames, delegate(TypeName type) { return type.ToString(style); });
                    return ConcatenateList(temp, ",");
                }

                #endregion
            }

            // String manipulation methods
            private static string[] SplitAtTopLevel(string input, char separator)
            {
                return SplitAtTopLevel(input, separator, "[(<".ToCharArray(), "])>".ToCharArray());
            }
            private static string[] SplitAtTopLevel(string input, char separator, char[] lefts, char[] rights)
            {
                if (lefts.Length != rights.Length)
                {
                    throw new FaultInjectionException();
                }

                List<string> result = new List<string>();

                Stack<int> bracketStack = new Stack<int>();
                int begin = 0;
                for (int i = 0; i < input.Length; ++i)
                {
                    int leftIndex = Array.IndexOf(lefts, input[i]);
                    int rightIndex = Array.IndexOf(rights, input[i]);
                    if (leftIndex != -1)
                    {
                        bracketStack.Push(leftIndex);
                    }
                    else if (rightIndex != -1)
                    {
                        if (bracketStack.Count == 0 || bracketStack.Peek() != rightIndex)
                        {
                            throw new FaultInjectionException();
                        }
                        bracketStack.Pop();
                    }
                    else if (input[i] == separator && bracketStack.Count == 0)
                    {
                        string item = input.Substring(begin, i - begin);
                        if (item == string.Empty)
                        {
                            throw new FaultInjectionException();
                        }
                        result.Add(item);
                        begin = i + 1;
                    }
                }
                if (begin >= input.Length)
                {
                    throw new FaultInjectionException();
                }
                result.Add(input.Substring(begin, input.Length - begin));

                return result.ToArray();
            }
            private static string ConcatenateList(string[] list, string separator)
            {
                if (list.Length == 0)
                {
                    return string.Empty;
                }
                string result = list[0];
                for (int i = 1; i < list.Length; ++i)
                {
                    result = result + separator + list[i];
                }
                return result;
            }

            #endregion 
        }

        #endregion  
    }
}
