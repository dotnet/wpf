// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Parser for the Path of a (CLR) binding
//

using System;
using System.Text;          // StringBuilder
using System.Windows;
using System.Collections.Generic;

using MS.Utility;           // FrugalList

namespace MS.Internal.Data
{
    internal enum SourceValueType { Property, Indexer, Direct };
    internal enum DrillIn { Never, IfNeeded, Always };

    internal struct SourceValueInfo
    {
        public SourceValueType type;
        public DrillIn drillIn;
        public string name;                 // the name the user supplied - could be "(0)"
        public FrugalObjectList<IndexerParamInfo> paramList;    // params for indexer
        public string propertyName;         // the real name - could be "Width"

        public SourceValueInfo(SourceValueType t, DrillIn d, string n)
        {
            type = t;
            drillIn = d;
            name = n;
            paramList = null;
            propertyName = null;
        }

        public SourceValueInfo(SourceValueType t, DrillIn d, FrugalObjectList<IndexerParamInfo> list)
        {
            type = t;
            drillIn = d;
            name = null;
            paramList = list;
            propertyName = null;
        }
    }

    internal struct IndexerParamInfo
    {
        // parse each indexer param "(abc)xyz" into two pieces - either can be empty
        public string parenString;
        public string valueString;

        public IndexerParamInfo(string paren, string value)
        {
            parenString = paren;
            valueString = value;
        }
    }

    internal sealed class PathParser
    {
        private string _error;
        public string Error => _error;
        private void SetError(string id, params object[] args) => _error = SR.Format(SR.GetResourceString(id), args);

        private enum State { Init, DrillIn, Prop, Done };
        private enum IndexerState { BeginParam, ParenString, ValueString, Done }

        // Each level of the path consists of
        //      a property or indexer:
        //                  .propname
        //                  /propname
        //                  [index]
        //                  /[index]
        //          (The . or / is optional in the very first level.)
        // The parser is a finite-state machine with two states corresponding
        // to the two-character lookahead above, plus two more states for the begining
        // and end.  The state transistions are done explicitly in the code below.
        //
        // The parser returns a 0-length array if it finds a syntax error.
        // It sets the Error property, so the caller can find out what happened.

        public SourceValueInfo[] Parse(string path)
        {
            _path = (path != null) ? path.Trim() : string.Empty;
            _n = _path.Length;

            if (_n == 0)
            {
                // When no path string is specified, use value directly and do not drill-in. (same as Path=".")
                // ClrBindingWorker needs this information to tell XmlBindingWorker about collectionMode.
                return new SourceValueInfo[] { new SourceValueInfo(SourceValueType.Direct, DrillIn.Never, (string)null) };
            }

            _index = 0;
            _drillIn = DrillIn.IfNeeded;

            _sourceValueInfos.Clear();
            _error = null;
            _state = State.Init;

            while (_state != State.Done)
            {
                char c = (_index < _n) ? _path[_index] : NullChar;
                if (char.IsWhiteSpace(c))
                {
                    ++_index;
                    continue;
                }

                switch (_state)
                {
                    case State.Init:
                        switch (c)
                        {
                            case '/':
                            case '.':
                            case NullChar:
                                _state = State.DrillIn;
                                break;
                            default:
                                _state = State.Prop;
                                break;
                        }
                        break;

                    case State.DrillIn:
                        switch (c)
                        {
                            case '/':
                                _drillIn = DrillIn.Always;
                                ++_index;
                                break;
                            case '.':
                                _drillIn = DrillIn.Never;
                                ++_index;
                                break;
                            case '[':
                            case NullChar:
                                break;
                            default:
                                SetError(nameof(SR.PathSyntax), _path.Substring(0, _index), _path.Substring(_index));
                                return Array.Empty<SourceValueInfo>();
                        }
                        _state = State.Prop;
                        break;

                    case State.Prop:
                        switch (c)
                        {
                            case '[': // Indexer follows
                                AddIndexer();
                                break;
                            default: // Property follows
                                AddProperty();
                                break;
                        }                           

                        break;
                }
            }

            // If an error has occurred, we return an empty array instead
            return _error is null ? _sourceValueInfos.ToArray() : Array.Empty<SourceValueInfo>();
        }

        private void AddProperty()
        {
            int start = _index;
            int level = 0;

            // include leading dots in the path (for XLinq)
            while (_index < _n && _path[_index] == '.')
                ++_index;

            while (_index < _n && (level > 0 || !IsSpecialChar(_path[_index])))
            {
                if (_path[_index] == '(')
                    ++level;
                else if (_path[_index] == ')')
                    --level;

                ++_index;
            }

            if (level > 0)
            {
                SetError(nameof(SR.UnmatchedParen), _path.Substring(start));
                return;
            }

            if (level < 0)
            {
                SetError(nameof(SR.UnmatchedParen), _path.Substring(0, _index));
                return;
            }

            string name = _path.Substring(start, _index - start).Trim();

            SourceValueInfo info = (name.Length > 0)
                ? new SourceValueInfo(SourceValueType.Property, _drillIn, name)
                : new SourceValueInfo(SourceValueType.Direct, _drillIn, (string)null);

            _sourceValueInfos.Add(info);

            StartNewLevel();
        }

        private void AddIndexer()
        {
            // indexer args are parsed by a (sub-) state machine with four
            // states.  The string is a comma-separated list of params, each
            // of which has two parts:  a "paren string" and a "value string"
            // (both parts are optional).  The character ^ can be used to
            // escape any of the special characters:  comma, parens, ], ^,
            // and white space.

            int start = ++_index;       // skip over initial [
            int level = 1;              // level of nested []

            bool escaped = false;       // true if current char is escaped
            bool trimRight = false;     // true if value string has trailing white space

            StringBuilder parenStringBuilder = new StringBuilder();
            StringBuilder valueStringBuilder = new StringBuilder();

            FrugalObjectList<IndexerParamInfo> paramList = new FrugalObjectList<IndexerParamInfo>();

            IndexerState state = IndexerState.BeginParam;
            while (state != IndexerState.Done)
            {
                if (_index >= _n)
                {
                    SetError(nameof(SR.UnmatchedBracket), _path.Substring(start - 1));
                    return;
                }

                char c = _path[_index++];

                // handle the escape character - set the flag for the next character
                if (c == EscapeChar && !escaped)
                {
                    escaped = true;
                    continue;
                }

                switch (state)
                {
                    case IndexerState.BeginParam:   // look for optional (...)
                        if (escaped)
                        {
                            // no '(', go parse the value
                            state = IndexerState.ValueString;
                            goto case IndexerState.ValueString;
                        }
                        else if (c == '(')
                        {
                            // '(' introduces optional paren string
                            state = IndexerState.ParenString;
                        }
                        else if (char.IsWhiteSpace(c))
                        {
                            // ignore leading white space
                        }
                        else
                        {
                            // no '(', go parse the value
                            state = IndexerState.ValueString;
                            goto case IndexerState.ValueString;
                        }
                        break;

                    case IndexerState.ParenString:  // parse (...)
                        if (escaped)
                        {
                            // add an escaped character without question
                            parenStringBuilder.Append(c);
                        }
                        else if (c == ')')
                        {
                            // end of (...), start to parse value
                            state = IndexerState.ValueString;
                        }
                        else
                        {
                            // add normal characters inside (...)
                            parenStringBuilder.Append(c);
                        }
                        break;

                    case IndexerState.ValueString:  // parse value
                        if (escaped)
                        {
                            // add an escaped character without question
                            valueStringBuilder.Append(c);
                            trimRight = false;
                        }
                        else if (level > 1)
                        {
                            // inside nested [], add characters without question
                            valueStringBuilder.Append(c);
                            trimRight = false;

                            if (c == ']')
                            {
                                --level;
                            }
                        }
                        else if (char.IsWhiteSpace(c))
                        {
                            // add white space, but trim it later if it's trailing
                            valueStringBuilder.Append(c);
                            trimRight = true;
                        }
                        else if (c == ',' || c == ']')
                        {
                            // end of current paramater - assemble the two parts
                            string parenString = parenStringBuilder.ToString();
                            string valueString = valueStringBuilder.ToString();
                            if (trimRight)
                            {
                                valueString = valueString.TrimEnd();
                            }

                            // add the parts to the final result
                            paramList.Add(new IndexerParamInfo(parenString, valueString));

                            // reset for the next parameter
                            parenStringBuilder.Length = 0;
                            valueStringBuilder.Length = 0;
                            trimRight = false;

                            // after ',' parse next parameter;  after ']' we're done
                            state = (c == ']') ? IndexerState.Done : IndexerState.BeginParam;
                        }
                        else
                        {
                            // add normal characters
                            valueStringBuilder.Append(c);
                            trimRight = false;

                            // keep track of nested []
                            if (c == '[')
                            {
                                ++level;
                            }
                        }
                        break;
                }

                // after processing each character, clear the escape flag
                escaped = false;
            }

            // assemble the final result
            SourceValueInfo info = new(SourceValueType.Indexer, _drillIn, paramList);
            _sourceValueInfos.Add(info);

            StartNewLevel();
        }

        private void StartNewLevel()
        {
            _state = (_index < _n) ? State.DrillIn : State.Done;
            _drillIn = DrillIn.Never;
        }

        private static bool IsSpecialChar(char ch)
        {
            return ch == '.' || ch == '/' || ch == '[' || ch == ']';
        }

        private State _state;
        private string _path;
        private int _index;
        private int _n;
        private DrillIn _drillIn;

        private const char NullChar = char.MinValue;
        private const char EscapeChar = '^';

        private readonly List<SourceValueInfo> _sourceValueInfos = new();
    }
}

