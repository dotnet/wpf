// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.Xaml.Context;

namespace System.Xaml
{
    internal enum SavedContextType { Template, ReparseValue, ReparseMarkupExtension }

    internal class XamlSavedContext
    {
        public XamlSavedContext(SavedContextType savedContextType, ObjectWriterContext owContext, XamlContextStack<ObjectWriterFrame> stack)
        {
            //We should harvest all information necessary from the xamlContext so that we can answer all ServiceProvider based questions.
            SaveContextType = savedContextType;
            SchemaContext = owContext.SchemaContext;
            Stack = stack;

            // Null out CurrentFrameValue in case of template to save on survived allocations
            if (savedContextType == SavedContextType.Template)
            {
                stack.CurrentFrame.Instance = null;
            }
            BaseUri = owContext.BaseUri;
        }

        public SavedContextType SaveContextType { get; }
        public XamlContextStack<ObjectWriterFrame> Stack { get; }
        public XamlSchemaContext SchemaContext { get; }
        public Uri BaseUri { get; }
    }
}
