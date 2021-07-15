// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Parser exceptions
//

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Security;
using System.Runtime.Serialization;
using MS.Utility;
using MS.Internal;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Threading;


namespace System.Windows.Markup
#endif
{
    ///<summary>Exception class for parser specific exceptions</summary>
    [Serializable]
#if PBTCOMPILER
    internal class XamlParseException : SystemException
#else
    public class XamlParseException : SystemException
#endif
    {
        #region Public

        #region Constructors

        ///<summary>
        /// Constructor
        ///</summary>
        public XamlParseException()
            : base()
        {
        }

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        public XamlParseException(string message)
            : base(message)
        {
        }

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">Exception message</param>
        ///<param name="innerException">exception occured</param>
        public XamlParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        ///<param name="lineNumber">
        /// lineNumber the exception occured at
        ///</param>
        ///<param name="linePosition">
        /// LinePosition the Exception occured at.
        ///</param>
        /// <ExternalAPI/> 
        public XamlParseException(string message, int lineNumber, int linePosition)
            : this(message)
        {
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }


        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        ///<param name="lineNumber">
        /// lineNumber the exception occured at
        ///</param>
        ///<param name="linePosition">
        /// LinePosition the Exception occured at.
        ///</param>
        ///<param name="innerException">
        /// original Exception that was thrown.
        ///</param>
        public XamlParseException(string message, int lineNumber, int linePosition, Exception innerException)
            : this(message, innerException)
        {
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        internal XamlParseException(string message, int lineNumber, int linePosition, Uri baseUri, Exception innerException)
            : this(message, innerException)
        {
            _lineNumber = lineNumber;
            _linePosition = linePosition;
            _baseUri = baseUri;
        }

        #endregion Constructors

        #region Properties

        ///<summary>
        /// LineNumber that the exception occured on.
        ///</summary>
        public int LineNumber
        {
            get { return _lineNumber; }
            internal set { _lineNumber = value; }
        }


        ///<summary>
        /// LinePosition that the exception occured on.
        ///</summary>
        public int LinePosition
        {
            get { return _linePosition; }
            internal set { _linePosition = value; }
        }

        ///<summary>
        /// If this is set, it indicates that the Xaml exception occurred
        /// in the context of a dictionary item, and this was the Xaml Key
        /// value of that item.
        ///</summary>

        public object KeyContext
        {
#if PBTCOMPILER
            set { _keyContext= value; }
#else
            get { return _keyContext; }
            internal set { _keyContext = value; }
#endif
        }

        ///<summary>
        /// If this is set, it indicates that the Xaml exception occurred
        /// in the context of an object with a Xaml Uid set, and this was the
        /// value of that Uid.
        ///</summary>
        public string UidContext
        {
#if PBTCOMPILER
            set { _uidContext = value; }
#else
            get { return _uidContext; }
            internal set { _uidContext = value; }
#endif
        }

        ///<summary>
        /// If this is set, it indicates that the Xaml exception occurred
        /// in the context of an object with a Xaml Name set, and this was the
        /// value of that name.
        ///</summary>
        public string NameContext
        {
#if PBTCOMPILER
            set { _nameContext = value; }
#else
            get { return _nameContext; }
            internal set { _nameContext = value; }
#endif

        }

        ///<summary>
        /// The BaseUri in effect at the point of the exception.
        ///</summary>
        public Uri BaseUri
        {
#if PBTCOMPILER
            set { _baseUri = value; }
#else
            get { return _baseUri; }
            internal set { _baseUri = value; }
#endif
        }

        #endregion Properties

        #endregion Public

        #region Private

        #region Serialization

        /// <summary>
        /// Internal constructor used for serialization when marshalling an
        /// exception of this type across and AppDomain or machine boundary.
        /// </summary>
        /// <param name="info">
        /// Contains all the information needed to serialize or deserialize
        /// the object.
        /// </param>
        /// <param name="context">
        /// Describes the source and destination of a given serialized stream,
        /// as well as a means for serialization to retain that context and an
        /// additional caller-defined context.
        /// </param>
        protected XamlParseException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            _lineNumber = info.GetInt32("Line");
            _linePosition = info.GetInt32("Position");
        }

        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">
        /// The SerializationInfo to populate with data.
        /// </param>
        /// <param name="context">
        /// The destination for this serialization.
        /// </param>
        ///

#if ! PBTCOMPILER
#else
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Line", (Int32)_lineNumber);
            info.AddValue("Position", (Int32)_linePosition);
        }

        #endregion Serialization

        #region internal helper methods


#if ! PBTCOMPILER
        //
        // Return the relative file path for current markup stream or file.
        // If the stream comes from assembly resource with .baml extension, this method
        // still reports .xaml.
        // The purpose of this method is to help developer to debug a failed baml stream.
        //
        internal static string GetMarkupFilePath(Uri resourceUri)
        {
            string bamlFilePath = string.Empty;
            string xamlFilePath = string.Empty;

            if (resourceUri != null)
            {
                if (resourceUri.IsAbsoluteUri)
                {
                    bamlFilePath = resourceUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                }
                else
                {
                    bamlFilePath = resourceUri.OriginalString;
                }

                // Replace the .baml with .xaml file extension.
                xamlFilePath = bamlFilePath.Replace(BamlExt, XamlExt);

                // Confirm the result has a .xaml file extension.
                if (-1 == xamlFilePath.LastIndexOf(XamlExt, StringComparison.Ordinal))
                    xamlFilePath = string.Empty;
            }

            return xamlFilePath;
        }


        [Flags]
        private enum ContextBits { Type = 1, File = 2, Line = 4 };

        //
        // Apply the context to the original error message.
        // The context contains :
        //          Current markup file 
        //          Current element name in the markup file.
        // 
        internal static string GenerateErrorMessageContext(
                                                int lineNumber,
                                                int linePosition,
                                                Uri baseUri,
                                                XamlObjectIds xamlObjectIds,
                                                Type objectType)
        {
            string message = " ";
            string xamlFile = GetMarkupFilePath(baseUri);
            string simpleObjectId = null;

            // Calculate the simpleObjectId.  Use the object's name, key, Uid, or
            // type name, whichever is available (in that order).

            if (xamlObjectIds != null)
            {
                if (xamlObjectIds.Name != null)
                {
                    simpleObjectId = xamlObjectIds.Name;
                }
                else if (xamlObjectIds.Key != null)
                {
                    simpleObjectId = xamlObjectIds.Key.ToString();
                }
                else if (xamlObjectIds.Uid != null)
                {
                    simpleObjectId = xamlObjectIds.Uid;
                }
            }

            if (simpleObjectId == null && objectType != null)
            {
                simpleObjectId = objectType.ToString();
            }

            ContextBits flags = 0;

            if (simpleObjectId != null)
                flags |= ContextBits.Type;

            if (!String.IsNullOrEmpty(xamlFile))
                flags |= ContextBits.File;

            if (lineNumber > 0)
                flags |= ContextBits.Line;

            switch (flags)
            {
                case 0:
                    message = String.Empty;
                    break;

                case ContextBits.Type:
                    message = SR.Get(SRID.ParserErrorContext_Type, simpleObjectId);
                    break;

                case ContextBits.File:
                    message = SR.Get(SRID.ParserErrorContext_File, xamlFile);
                    break;

                case ContextBits.Type | ContextBits.File:
                    message = SR.Get(SRID.ParserErrorContext_Type_File, simpleObjectId, xamlFile);
                    break;

                case ContextBits.Line:
                    message = SR.Get(SRID.ParserErrorContext_Line, lineNumber, linePosition);
                    break;

                case ContextBits.Type | ContextBits.Line:
                    message = SR.Get(SRID.ParserErrorContext_Type_Line, simpleObjectId, lineNumber, linePosition);
                    break;

                case ContextBits.File | ContextBits.Line:
                    message = SR.Get(SRID.ParserErrorContext_File_Line, xamlFile, lineNumber, linePosition);
                    break;

                case ContextBits.Type | ContextBits.File | ContextBits.Line:
                    message = SR.Get(SRID.ParserErrorContext_Type_File_Line, simpleObjectId, xamlFile, lineNumber, linePosition);
                    break;
            }

            return message;
        }

#endif


        //
        // This method creates a XamlParseException and throws it.  If provided, it incorporates 
        // the information such as baseUri and elementName into the exception, possibly in the exception
        // message, and always in the XamlParseException itself.  When an inner exception is provided,
        // we incorporate its Message into the XamlParseException's message.
        //
        // When an exception is thrown during xaml load (compile), we simply identify its location with
        // the line number and position.  When an exception is thrown at runtime (baml load),
        // we identify its location with as much information as we can, since we don't have line numbers
        // available.  So the following identifying information is provided, if available:  BaseUri,
        // nearest x:Uid in context, nearest x:Name, and nearest x:Key.
        //

        internal static void ThrowException(
                                            string message,
                                            Exception innerException,
                                            int lineNumber,
                                            int linePosition,
                                            Uri baseUri,
                                            XamlObjectIds currentXamlObjectIds,
                                            XamlObjectIds contextXamlObjectIds,
                                            Type objectType
                                            )
        {
            // If there's an inner exception, we'll append its message to our own.

            if (innerException != null && innerException.Message != null)
            {
                StringBuilder sb = new StringBuilder(message);
                if (innerException.Message != String.Empty)
                {
                    sb.Append(" ");
                }

                sb.Append(innerException.Message);

                message = sb.ToString();
            }

            // If we have line numbers, then we are
            // parsing a xaml file where the line number has been explicitly set, so
            // throw a xaml exception.  Otherwise we are parsing a baml file, or we
            // don't know what the line number is.

            XamlParseException xamlParseException;

#if !PBTCOMPILER
            string contextMessage = XamlParseException.GenerateErrorMessageContext(
                                                        lineNumber,
                                                        linePosition,
                                                        baseUri,
                                                        currentXamlObjectIds,
                                                        objectType);

            message = message + "  " + contextMessage;

#endif

            // If the exception was a XamlParse exception on the other
            // side of a Reflection Invoke, then just pull up the Parse exception.
            if (innerException is TargetInvocationException && innerException.InnerException is XamlParseException)
            {
                xamlParseException = (XamlParseException)innerException.InnerException;
            }
            else
            {
                if (lineNumber > 0)
                {
                    xamlParseException = new XamlParseException(message, lineNumber, linePosition, innerException);
                }
                else
                {
#if PBTCOMPILER
                    throw new InvalidOperationException(message);
#else
                    xamlParseException = new XamlParseException(message, innerException);
#endif
                }
            }

            // Fill in the exception with some more runtime-context information.
            if (contextXamlObjectIds != null)
            {
                xamlParseException.NameContext = contextXamlObjectIds.Name;
                xamlParseException.UidContext = contextXamlObjectIds.Uid;
                xamlParseException.KeyContext = contextXamlObjectIds.Key;
            }
            xamlParseException.BaseUri = baseUri;


#if !PBTCOMPILER
            if (TraceMarkup.IsEnabled)
            {
                TraceMarkup.TraceActivityItem(TraceMarkup.ThrowException,
                                             xamlParseException);
            }
#endif

            throw xamlParseException;
        }

        //
        // Runtime (baml loader) ThrowException wrapper that takes an inner exception and
        // incorporates context from the ParserContext.
        //

#if !PBTCOMPILER
        internal static void ThrowException(ParserContext parserContext, int lineNumber, int linePosition, string message, Exception innerException)
        {
            ThrowException(
                   message,
                   innerException,
                   lineNumber,
                   linePosition);
        }
#endif

        //
        // Convenience ThrowException wrapper that just takes the message, inner exception, and line number.
        //

        internal static void ThrowException(
                                            string message,
                                            Exception innerException,
                                            int lineNumber,
                                            int linePosition)
        {
            ThrowException(message, innerException, lineNumber, linePosition,
                            null, null, null, null);
        }


        



        #endregion internal helper methods

        #region const

        internal const string BamlExt = ".baml";
        internal const string XamlExt = ".xaml";

        #endregion const

        #region Data

        int _lineNumber = 0;
        int _linePosition = 0;
        object _keyContext = null;
        string _uidContext = null;
        string _nameContext = null;
        Uri _baseUri = null;

        #endregion Data

        #endregion Private
    }
}
