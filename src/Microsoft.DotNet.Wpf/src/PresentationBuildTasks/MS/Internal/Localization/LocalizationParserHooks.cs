// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;

using MS.Internal.Globalization;
using MS.Internal.Markup;
using MS.Internal.Tasks;

namespace MS.Internal
{
    internal sealed class LocalizationParserHooks : ParserHooks
    {
        private MarkupCompiler                  _compiler;
        private ArrayList                       _commentList;
        private LocalizationComment             _currentComment;
        private LocalizationDirectivesToLocFile _directivesToFile;
        private bool                            _isSecondPass;
                
        internal LocalizationParserHooks(
            MarkupCompiler                  compiler ,
            LocalizationDirectivesToLocFile directivesToFile,
            bool                            isSecondPass
            )
        {               
            _compiler = compiler;            
            _directivesToFile = directivesToFile;  
            _isSecondPass = isSecondPass;

            // The arrray list holds all the comments collected while parsing Xaml.
            _commentList = new ArrayList();

            // It is the comments current being processed.
            _currentComment = new LocalizationComment();       
        }

        internal override ParserAction LoadNode(XamlNode tokenNode)
        {   
            switch (tokenNode.TokenType)
            {
                case XamlNodeType.DocumentStart :
                {
                    // A single ParserHooks might be used to parse multiple bamls. 
                    // We need to clear the comments list at the begining of each baml.
                    _commentList.Clear();
                    _currentComment = new LocalizationComment();       
                    return ParserAction.Normal;
                }                
                case XamlNodeType.DefAttribute : 
                {
                    XamlDefAttributeNode node = (XamlDefAttributeNode) tokenNode;
                    if (node.Name == XamlReaderHelper.DefinitionUid)
                    {
                        _currentComment.Uid = node.Value;
                    }

                    return ParserAction.Normal;
                }
                case XamlNodeType.Property : 
                {
                    XamlPropertyNode node = (XamlPropertyNode) tokenNode; 

                    // When this parer hook is invoked, comments is always output to a seperate file.                    
                    if (LocComments.IsLocCommentsProperty(node.TypeFullName, node.PropName))
                    {
                        // try parse the value. Exception will be thrown if not valid.
                        LocComments.ParsePropertyComments(node.Value);
                        _currentComment.Comments = node.Value;
                        return ParserAction.Skip;  // skips outputing this node to baml
                    }

                    if (  _directivesToFile == LocalizationDirectivesToLocFile.All
                       && LocComments.IsLocLocalizabilityProperty(node.TypeFullName, node.PropName))                     
                    {
                        // try parse the value. Exception will be thrown if not valid.
                        LocComments.ParsePropertyLocalizabilityAttributes(node.Value);                        
                        _currentComment.Attributes = node.Value;
                        return ParserAction.Skip;  // skips outputing this node to baml                      
                    }

                    return ParserAction.Normal;                    
                }
                case XamlNodeType.EndAttributes : 
                {
                    FlushCommentToList(ref _currentComment);
                    return ParserAction.Normal;
                }
                case XamlNodeType.DocumentEnd : 
                {
                    //
                    // When reaching document end, we output all the comments we have collected 
                    // so far into a localization comment file. If the parsing was aborted in 
                    // MarkupCompilePass1, we would not out the incomplete set of comments because
                    // it will not reach document end.
                    //
                
                    if (_commentList.Count > 0)
                    {
                        string absoluteOutputPath = _compiler.TargetPath + _compiler.SourceFileInfo.RelativeSourceFilePath + SharedStrings.LocExtension;
                        MemoryStream memStream = new MemoryStream();

                        // TaskFileService.WriteFile adds BOM for UTF8 Encoding, thus don't add here 
                        // when creating XmlTextWriter.
                        using (XmlTextWriter writer = new XmlTextWriter(memStream, new UTF8Encoding(false)))
                        {
                            // output XML for each piece of comment                            
                            writer.Formatting = Formatting.Indented;
                            writer.WriteStartElement(LocComments.LocResourcesElement);    
                            writer.WriteAttributeString(LocComments.LocFileNameAttribute, _compiler.SourceFileInfo.RelativeSourceFilePath);
                            
                            foreach (LocalizationComment comment in _commentList)
                            {
                                writer.WriteStartElement(LocComments.LocCommentsElement);
                                writer.WriteAttributeString(LocComments.LocCommentIDAttribute, comment.Uid);

                                if (comment.Attributes != null)
                                {
                                    writer.WriteAttributeString(LocComments.LocLocalizabilityAttribute, comment.Attributes);                                    
                                }

                                if (comment.Comments != null)
                                {
                                    writer.WriteAttributeString(LocComments.LocCommentsAttribute, comment.Comments);
                                }

                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();

                            writer.Flush();
                            _compiler.TaskFileService.WriteFile(memStream.ToArray(), absoluteOutputPath);
                        }
                    }
                    
                    return ParserAction.Normal;
                }
                default: 
                {
                    return ParserAction.Normal;
                }
            }
        }        

        /// <summary>
        /// Add the existing comment into the cached list and clear the state for the 
        /// next incoming comment. Comments are only collected in pass1. The method is no-op 
        /// in pass2.
        /// </summary>
        private void FlushCommentToList(ref LocalizationComment comment)
        {
            if (_isSecondPass) return;
            
            if (  _currentComment.Uid != null 
               && (  _currentComment.Attributes != null 
                  || _currentComment.Comments != null
                  )
               )
            {
                // add the comments into the list and reset
                _commentList.Add(_currentComment);
                _currentComment = new LocalizationComment();
            }
            else
            {
                // clear all properties
                _currentComment.Uid = _currentComment.Attributes = _currentComment.Comments = null;
            }
        }

        private class LocalizationComment
        {
            internal string Uid;
            internal string Comments;
            internal string Attributes;            
        }
    }    
}
  
