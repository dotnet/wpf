// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: DocumentsTrace is a tracing utility for Fixed and Flow documents
//              

namespace MS.Internal.Documents
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security;
    using MS.Internal.PresentationFramework; // SecurityHelper

    internal sealed class DocumentsTrace
    {
        internal static class FixedFormat
        {
#if DEBUG
            public static DocumentsTrace FixedDocument = new DocumentsTrace("FixedFormat-FixedDocument");
            public static DocumentsTrace PageContent= new DocumentsTrace("FixedFormat-PageContent");
            public static DocumentsTrace IDF = new DocumentsTrace("FixedFormat-IDFAsync");
#else
            public static DocumentsTrace FixedDocument = null;
            public static DocumentsTrace PageContent = null;
            public static DocumentsTrace IDF = null;
#endif

#if DEBUG
            static FixedFormat()
            {
                // FixedFormat.IDF.Enable();
                // FixedFormat.FixedDocument.Enable();
                // FixedFormat.PageContent.Enable();
            }
#endif
        }


        internal static class FixedTextOM
        {
#if DEBUG
            public static DocumentsTrace TextView       = new DocumentsTrace("FixedTextOM-TextView");
            public static DocumentsTrace TextContainer  = new DocumentsTrace("FixedTextOM-TextContainer"); 
            public static DocumentsTrace Map            = new DocumentsTrace("FixedTextOM-FixedFlowMap");
            public static DocumentsTrace Highlight      = new DocumentsTrace("FixedTextOM-Highlight");
            public static DocumentsTrace Builder        = new DocumentsTrace("FixedTextOM-FixedTextBuilder");
            public static DocumentsTrace FlowPosition   = new DocumentsTrace("FixedTextOM-FlowPosition");
#else
            public static DocumentsTrace TextView = null;
            public static DocumentsTrace TextContainer = null;
            public static DocumentsTrace Map = null;
            public static DocumentsTrace Highlight = null;
            public static DocumentsTrace Builder = null;
            public static DocumentsTrace FlowPosition = null;

#endif


#if DEBUG
            // Note:
            // If you want to enable trace tags without recompiling. This is a good place to put a break point
            // during start-up.
            static FixedTextOM()
            {
                // FixedTextOM.TextView.Enable();
                // FixedTextOM.TextContainer.Enable();
                // FixedTextOM.Map.Enable();
                // FixedTextOM.Highlight.Enable();
                // FixedTextOM.Builder.Enable();
                // FixedTextOM.FlowPosition.Enable();
            }
#endif
        }


        internal static class FixedDocumentSequence
        {
#if DEBUG
            public static DocumentsTrace Content = new DocumentsTrace("FixedDocumentsSequence-Content");
            public static DocumentsTrace IDF = new DocumentsTrace("FixedDocumentSequence-IDFAsync");
            public static DocumentsTrace TextOM = new DocumentsTrace("FixedDocumentSequence-TextOM");
            public static DocumentsTrace Highlights = new DocumentsTrace("FixedDocumentSequence-Highlights");
#else
            public static DocumentsTrace Content = null;
            public static DocumentsTrace IDF = null;
            public static DocumentsTrace TextOM = null;
            public static DocumentsTrace Highlights = null;
#endif

#if DEBUG
            static FixedDocumentSequence()
            {
                //FixedDocumentSequence.Content.Enable();
                //FixedDocumentSequence.IDF.Enable();
                //FixedDocumentSequence.TextOM.Enable();
                //FixedDocumentSequence.Highlights.Enable();
            }
#endif
        }

        static DocumentsTrace()
        {
#if DEBUG
            // we are adding console as a listener
            TextWriterTraceListener consoleListener = new TextWriterTraceListener( Console.Out );

            // get the listeners collection
            TraceListenerCollection listeners = null;
            listeners = System.Diagnostics.Trace.Listeners;

            // add the console listener
            listeners.Add( consoleListener );
#endif
        }

        public DocumentsTrace(string switchName)
        {
#if DEBUG
            string name = SafeSecurityHelper.GetAssemblyPartialName( Assembly.GetCallingAssembly() );
            _switch = new BooleanSwitch(switchName, "[" + name + "]");
#endif
        }

        public DocumentsTrace(string switchName, bool initialState) : this(switchName)
        {
#if DEBUG
            _switch.Enabled = initialState;
#endif
        }

        [Conditional("DEBUG")]
        public void Trace(string message)
        {
#if DEBUG
            if (_switch.Enabled)
            {
                System.Diagnostics.Trace.WriteLine(_switch.Description + " " + _switch.DisplayName + " : " + message);
            }
#endif
        }

        [Conditional("DEBUG")]
        public void TraceCallers(int Depth)
        {
#if DEBUG
            if (_switch.Enabled)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1);
                if (Depth < 0)
                    Depth = st.FrameCount;

                Indent();

                for (int i=0; i < st.FrameCount && i < Depth; i++)
                {
                    System.Diagnostics.StackFrame sf = st.GetFrame(i);
                    Trace(sf.GetMethod()+"+"+sf.GetILOffset().ToString());
                }

                Unindent();
            }
#endif
        }

        [Conditional("DEBUG")]
        public void Indent()
        {
#if DEBUG
            System.Diagnostics.Trace.Indent();
#endif
        }


        [Conditional("DEBUG")]
        public void Unindent()
        {
#if DEBUG
            System.Diagnostics.Trace.Unindent();
#endif
        }


        [Conditional("DEBUG")]
        public void Enable()
        {
#if DEBUG
            _switch.Enabled = true;
#endif
        }

        [Conditional("DEBUG")]
        public void Disable()
        {
#if DEBUG
            _switch.Enabled = false;
#endif
        }

        public bool IsEnabled
        {
            get
            {
#if DEBUG
                return _switch.Enabled; 
#else
                return false;
#endif
            }
        }

#if DEBUG
        private System.Diagnostics.BooleanSwitch _switch;
#endif
    }
}
