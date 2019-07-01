// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************* 
 * Purpose: Implements RoundTripTest(), which takes xaml and parses and
 *          serializes multiple times, finally comparing the resultant
 *          trees and serialized xamls.
********************************************************************/

using System;
using System.Windows.Interop;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using System.Text;
using System.Reflection;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Win32;
using Microsoft.Test.Markup;
using Microsoft.Test.Serialization.CustomElements;
using MTD = Microsoft.Test.Display;
using Microsoft.Test.Logging;
using Microsoft.Test.Windows;

namespace Microsoft.Test.Serialization
{

    /// <summary>
    /// Implements RoundTripTest(), which takes xaml and parses and
    /// serializes multiple times, finally comparing the resultant
    /// trees and serialized xamls.
    /// </summary>
    public class SerializationHelper
    {
        /// <summary>
        /// Creates a SerializationHelper.
        /// </summary>
        public SerializationHelper()
        {
        }

        /// <summary>
        /// Reads a file to a stream and passes it to the stream-based round trip method.
        /// </summary> 
        /// <param name="fileName">Xaml file path.</param>
        public void RoundTripTestFile(string fileName)
        {
            RoundTripTestFile(fileName, false);
        }

        /// <summary>
        /// Reads a file to a stream and passes it to the stream-based round trip method.
        /// </summary> 
        /// <param name="fileName">Xaml file path.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        public void RoundTripTestFile(string fileName, bool attemptDisplay)
        {
            RoundTripTestFile(fileName, XamlWriterMode.Value, attemptDisplay);
        }

        /// <summary>
        /// Reads a file to a stream and passes it to the stream-based round trip method.
        /// </summary> 
        /// <param name="fileName">Xaml file path.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        public void RoundTripTestFile(string fileName, XamlWriterMode expressionMode, bool attemptDisplay)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The xaml file does not exist: " + fileName + ".", "fileName");
            }

            // Open file to stream.
            Stream stream = File.OpenRead(fileName);

            try
            {
                this.RoundTripTest(stream, expressionMode, attemptDisplay);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }

        /// <summary>
        /// Converts a xaml string to a stream and passes it to the stream-based round trip method.
        /// </summary> 
        /// <param name="xaml">Xaml string.</param>
        public void RoundTripTest(string xaml)
        {
            RoundTripTest(xaml, false);
        }

        /// <summary>
        /// Using XamlWriterMode.Value and passes to another method to do the round trip.
        /// </summary>
        /// <param name="xaml"></param>
        /// <param name="attemptDisplay"></param>
        public void RoundTripTest(string xaml, bool attemptDisplay)
        {
            RoundTripTest(xaml, XamlWriterMode.Value, attemptDisplay);
        }


        /// <summary>
        /// Converts a xaml string to a stream and passes it to the stream-based round trip method.
        /// </summary> 
        /// <param name="xaml">Xaml string.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        public void RoundTripTest(string xaml, XamlWriterMode expressionMode, bool attemptDisplay)
        {
            if (xaml == null)
            {
                throw new ArgumentNullException("xaml");
            }

            _originalXaml = xaml;

            Stream stream = IOHelper.ConvertTextToStream(xaml);

            try
            {
                this.RoundTripTest(stream, expressionMode, attemptDisplay);
            }
            finally
            {
                if (stream != null)
                    stream.Close();

                _originalXaml = null;
            }
        }

        /// <summary>
        /// Does this:
        /// 1. Parses a xaml stream.
        /// 2. Fires PreFirstDisplay event.
        /// 3. Displays the first object tree if possible and attemptDisplay is true.
        /// 4. Fires FirstDisplay event if the first tree has been displayed.
        /// 5. Fires PreFirstSerialization event.
        /// 6. Serializes the first object tree to a xaml string.
        /// 7. Parses the serialized xaml string to another object tree.
        /// 8. Fires PreSecondDisplay event.
        /// 9. Displays the second object tree if possible and attemptDisplay is true.
        /// 10. Fires SecondDisplay event if the second tree has been displayed.
        /// 11. Fires PreSecondSerialization event.
        /// 12. Serializes the second object tree to a xaml string.
        /// 13. Compares the first tree with the second tree if DoTreeComparison is true.
        /// 14. Compares the first serialized xaml string with the second serialized xaml string if DoXamlComparison is true.       
        /// </summary>
        /// <param name="originalStream">Xaml stream.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        [CLSCompliant(false)]
        public void RoundTripTest(Stream originalStream, bool attemptDisplay)
        {
            RoundTripTest(originalStream, XamlWriterMode.Value, attemptDisplay);
        }

        /// <summary>
        /// Does this:
        /// 1. Parses a xaml stream.
        /// 2. Fires PreFirstDisplay event.
        /// 3. Displays the first object tree if possible and attemptDisplay is true.
        /// 4. Fires FirstDisplay event if the first tree has been displayed.
        /// 5. Fires PreFirstSerialization event.
        /// 6. Serializes the first object tree to a xaml string.
        /// 7. Parses the serialized xaml string to another object tree.
        /// 8. Fires PreSecondDisplay event.
        /// 9. Displays the second object tree if possible and attemptDisplay is true.
        /// 10. Fires SecondDisplay event if the second tree has been displayed.
        /// 11. Fires PreSecondSerialization event.
        /// 12. Serializes the second object tree to a xaml string.
        /// 13. Compares the first tree with the second tree if DoTreeComparison is true.
        /// 14. Compares the first serialized xaml string with the second serialized xaml string if DoXamlComparison is true.       
        /// </summary>
        /// <param name="originalStream">Xaml stream.</param>
        [CLSCompliant(false)]
        public void RoundTripTest(Stream originalStream)
        {
            RoundTripTest(originalStream, XamlWriterMode.Value, false);
        }

        /// <summary>
        /// Does this:
        /// 1. Parses a xaml stream.
        /// 2. Fires PreFirstDisplay event.
        /// 3. Displays the first object tree if possible and attemptDisplay is true.
        /// 4. Fires FirstDisplay event if the first tree has been displayed.
        /// 5. Fires PreFirstSerialization event.
        /// 6. Serializes the first object tree to a xaml string.
        /// 7. Parses the serialized xaml string to another object tree.
        /// 8. Fires PreSecondDisplay event.
        /// 9. Displays the second object tree if possible and attemptDisplay is true.
        /// 10. Fires SecondDisplay event if the second tree has been displayed.
        /// 11. Fires PreSecondSerialization event.
        /// 12. Serializes the second object tree to a xaml string.
        /// 13. Compares the first tree with the second tree if DoTreeComparison is true.
        /// 14. Compares the first serialized xaml string with the second serialized xaml string if DoXamlComparison is true.       
        /// </summary> 
        /// <param name="originalStream">Xaml stream.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        /// <remarks>This helper functions must be called from the Dispatcher Thread.</remarks>
        [CLSCompliant(false)]
        public void RoundTripTest(Stream originalStream, XamlWriterMode expressionMode, bool attemptDisplay)
        {
            if (originalStream == null)
            {
                throw new ArgumentNullException("originalStream");
            }

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;


            if (dispatcher == null)
            {
                throw new InvalidOperationException("The current thread has not entered a Dispatcher.");
            }

            object firstTreeRoot = null;

            //
            // Parse original xaml.
            // We create a ParserContext here. It will be reused by subsequent parsing
            // operations.
            //            

            GlobalLog.LogStatus("Constructing object tree using LoadXml...");
            _parserContext = new ParserContext();
            _parserContext.BaseUri = PackUriHelper.Create(new Uri("siteoforigin://"));
            firstTreeRoot = ParseXaml(originalStream);

            RoundTripTestObject(firstTreeRoot, expressionMode, attemptDisplay);
        }

        /// <summary>
        /// Specify default value for attemDisplay and passes it to the round trip method with more parameters.
        /// </summary> 
        /// <param name="treeRoot">Tree Root.</param>
        /// <remarks>Dispatcher must be entered prior to calling helper functions.</remarks>
        public void RoundTripTestObject(object treeRoot)
        {
            bool attemptDisplay = true;
            RoundTripTestObject(treeRoot, attemptDisplay);
        }
        /// <summary>
        /// Specify default values and passes them to the round trip method with more parameters.
        /// </summary>
        /// <param name="treeRoot"></param>
        /// <param name="attemptDisplay"></param>
        public void RoundTripTestObject(object treeRoot, bool attemptDisplay)
        {
            XamlWriterMode expressionMode = XamlWriterMode.Value;
            RoundTripTestObject(treeRoot, expressionMode, attemptDisplay);
        }

        /// <summary>
        /// Does this:
        /// 1. Fires PreFirstDisplay event.
        /// 2. Displays the first object tree if possible and attemptDisplay is true.
        /// 3. Fires FirstDisplay event if the first tree has been displayed.
        /// 4. Fires PreFirstSerialization event.
        /// 5. Serializes the first object tree to a xaml string.
        /// 6. Parses the serialized xaml string to another object tree.
        /// 7. Fires PreSecondDisplay event.
        /// 8. Displays the second object tree if possible and attemptDisplay is true.
        /// 9. Fires SecondDisplay event if the second tree has been displayed.
        /// 10. Fires PreSecondSerialization event.
        /// 11. Serializes the second object tree to a xaml string.
        /// 12. Compares the first tree with the second tree if DoTreeComparison is true.
        /// 13. Compares the first serialized xaml string with the second serialized xaml string if DoXamlComparison is true.       
        /// </summary> 
        /// <param name="firstTreeRoot">Root of the tree.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        /// <param name="attemptDisplay">Whether or not to display the tree if the root is a FrameworkElement or ICustomElement. Default: false</param> 
        /// <remarks>This helper functions must be called from the Dispatcher Thread.</remarks>
        public void RoundTripTestObject(object firstTreeRoot, XamlWriterMode expressionMode, bool attemptDisplay)
        {

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            //
            // Enqueue the round trip test routine.
            //
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new AsyncRoundTripTestCallback(AsyncRoundTripTest),
                firstTreeRoot, expressionMode, attemptDisplay);

            //
            // Setup ShutDown handler to catch unexpected shutdowns.
            // Set appropriate flag to track our state.
            //
            dispatcher.ShutdownFinished += new EventHandler(_ValidatingExitDispatcher);

            //
            // We do this so we have only one dispatcher during 
            // all the serialization.
            //
            Dispatcher.Run();
        }

        /// <summary>
        /// We use this to validate that no one called InvokeShutdown or 
        /// BeginShutDown unexpectedly. 
        /// </summary>
        private void _ValidatingExitDispatcher(object o, EventArgs args)
        {
            if (!_roundTripTestExit)
            {
                throw new InvalidOperationException("The dispatcher is shutting down on a incorrect state.");
            }
        }

        /// <summary>
        /// Delegate type for the async call to complete the round trip test.
        /// </summary>
        public delegate void AsyncRoundTripTestCallback(object firstTreeRoot, XamlWriterMode expressionMode, bool attemptDisplay);

        /// <summary>
        /// Callback routine to complete the round trip test.
        /// </summary>
        public void AsyncRoundTripTest(object firstTreeRoot, XamlWriterMode expressionMode, bool attemptDisplay)
        {
            try
            {
                object secondTreeRoot = null;
                string firstSerialized = String.Empty;
                string secondSerialized = String.Empty;

                _iteration++;

                if (null == firstTreeRoot)
                {
                    throw new Exception("First tree root is null.");
                }

                _firstTreeRoot = firstTreeRoot;

                // Fire PreFirstDisplay event
                if (PreFirstDisplay != null)
                {
                    PreFirstDisplay(_firstTreeRoot);
                }

                //
                // Display first tree.
                //
                if (attemptDisplay)
                {
                    _isFirst = true;
                    GlobalLog.LogStatus("Attempting to display the first tree root...");
                    DisplayTree(firstTreeRoot, _iteration + " " + " - first tree root", true);
                }

                // Fire PreFirstSerialization event
                if (PreFirstSerialization != null)
                {
                    PreFirstSerialization(_firstTreeRoot);
                }

                //
                // Serialize first tree.
                //
                GlobalLog.LogStatus("Serializing the first tree root...");
                firstSerialized = SerializeObjectTree(firstTreeRoot, expressionMode);
                GlobalLog.LogStatus("Done serializing the first tree root.");

                // Fire an event with the serialized xaml string. 
                _FireXamlSerializedEvent(firstSerialized);

                if (this.CompareOriginalXaml && _originalXaml != null)
                {
                    GlobalLog.LogStatus("Comparing original xaml to first serialized xaml...");
                    _CompareXamls(_originalXaml, firstSerialized);
                }

                //
                // Parse the first serialized xaml.
                // Nullify _parserContext (if it's not null already) so that 
                //  it can't be reused anymore
                //
                secondTreeRoot = ParseXaml(firstSerialized);
                _parserContext = null;

                _secondTreeRoot = secondTreeRoot;

                // Fire PreSecondDisplay event
                if (PreSecondDisplay != null)
                {
                    PreSecondDisplay(_secondTreeRoot);
                }

                //
                // Display the second tree.
                //
                if (attemptDisplay)
                {
                    _isFirst = false;
                    GlobalLog.LogStatus("Attempting to display the second tree root...");
                    DisplayTree(secondTreeRoot, _iteration + " " + " - second tree root");
                }

                // Fire PreSecondSerialization event
                if (PreSecondSerialization != null)
                {
                    PreSecondSerialization(_secondTreeRoot);
                }

                //
                // Serialize the second tree.
                //
                GlobalLog.LogStatus("Serializing the second tree...");
                secondSerialized = SerializeObjectTree(secondTreeRoot, expressionMode);
                GlobalLog.LogStatus("Done serializing the second tree root.");

                // Fire an event with the serialized xaml string. 
                _FireXamlSerializedEvent(secondSerialized);

                //
                // Compare the first and second trees.
                //
                if (DoTreeComparison)
                {
                    GlobalLog.LogStatus("Comparing object trees...");
                    _CompareObjectTree(firstTreeRoot, secondTreeRoot);
                }

                if (XamlWriterMode.Expression != expressionMode && AlwaysTestExpressionMode)
                {
                    // For XamlWriterMode.Value, Expression mode is also verified. 
                    //
                    // Serialize first tree with Expression Mode
                    //
                    GlobalLog.LogStatus("Serializing the first tree root with Expression mode...");
                    secondSerialized = SerializeObjectTree(firstTreeRoot, XamlWriterMode.Expression);
                    GlobalLog.LogStatus("Done serializing the first tree root with Expression mode.");

                    // Fire an event with the serialized xaml string. 
                    _FireXamlSerializedEvent(secondSerialized);

                    //
                    // Parse the serialized xaml. 
                    //
                    secondTreeRoot = ParseXaml(secondSerialized);

                    // Fire PreSecondDisplay event
                    if (PreSecondDisplay != null)
                    {
                        PreSecondDisplay(secondTreeRoot);
                    }

                    //
                    // Display the second tree.
                    //
                    if (attemptDisplay)
                    {
                        _isFirst = false;
                        GlobalLog.LogStatus("Attempting to display the tree loaded from string serialized with Expression mode ...");
                        DisplayTree(secondTreeRoot, _iteration + " " + " - additional tree", false);
                    }

                    //
                    // Compare the first and the additional tree parsed from string serialized with Expression mode.					//
                    if (DoTreeComparison)
                    {
                        GlobalLog.LogStatus("Comparing object trees...");
                        _CompareObjectTree(firstTreeRoot, secondTreeRoot);
                    }
                }

                //
                // Compare first and second serialized xaml files.
                //
                if (DoXamlComparison)
                {
                    GlobalLog.LogStatus("Comparing xamls...");
                    _CompareXamls(firstSerialized, secondSerialized);
                }
            }
            finally
            {
                //
                // Shutdown the dispatcher.
                // Set the appropriate flags to indicates the shutdown
                // is expected.
                //
                _roundTripTestExit = true;

                Dispatcher.CurrentDispatcher.ShutdownFinished -= new EventHandler(_ValidatingExitDispatcher);
                _ShutdownDispatcher();
            }
        }

        /// <summary>
        /// Load from xaml
        /// </summary>
        /// <param name="xaml"></param>
        /// <returns></returns>
        public static object ParseXaml(string xaml)
        {
            Stream stream = null;
            object loadedObj = null;

            try
            {
                stream = IOHelper.ConvertTextToStream(xaml);
                GlobalLog.LogStatus("Constructing object tree using LoadXml()...");
                loadedObj = ParseXaml(stream);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return loadedObj;
        }

        private static object ParseXaml(Stream stream)
        {
            ParserContext pc;
            // If a ParserContext exists, reuse it. Otherwise, create a new one.
            if (_parserContext != null)
            {
                pc = _parserContext;
            }
            else
            {
                pc = new ParserContext();
                pc.BaseUri = System.IO.Packaging.PackUriHelper.Create(new Uri("siteoforigin://"));
            }
            return XamlReader.Load(stream, pc);
        }

        private static ParserContext _parserContext = null;

        /// <summary>
        /// Creates a stream from a filename, and calls XamlReader.Load() with 
        /// it to create an object tree.
        /// </summary>
        /// <param name="fileName">Path to the xaml file.</param>
        public static object ParseXamlFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            Stream stream = null;
            object parsedRoot = null;

            try
            {
                stream = File.OpenRead(fileName);
                parsedRoot = ParseXaml(stream);
            }
            finally
            {
                if (null != stream)
                    stream.Close();
            }

            return parsedRoot;
        }

        /// <summary>
        /// Compares two xaml strings for xaml equivalency.
        /// </summary>
        private bool _CompareXamls(string firstSerialized, string secondSerialized)
        {
            XmlCompareResult result;

            result = XamlComparer.Compare(firstSerialized, secondSerialized,
                    _nodesShouldIgnoreChildrenSequence);

            if (CompareResult.Equivalent != result.Result)
            {
                throw new Exception("Failure occurred while comparing xamls.");
            }

            return true;
        }

        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        public static string SerializeObjectTree(object objectTree)
        {
            return SerializeObjectTree(objectTree, XamlWriterMode.Value);
        }


        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="textWriter"></param>        
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void SerializeObjectTree(object objectTree, TextWriter textWriter)
        {
            // Serialize
            System.Windows.Markup.XamlWriter.Save(objectTree, textWriter);
        }


        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="stream"></param>                
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void SerializeObjectTree(object objectTree, Stream stream)
        {
            // Serialize
            System.Windows.Markup.XamlWriter.Save(objectTree, stream);
        }


        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="xmlWriter"></param>           
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void SerializeObjectTree(object objectTree, XmlWriter xmlWriter)
        {
            if (objectTree == null)
            {
                throw new ArgumentNullException("objectTree");
            }

            // Serialize
            System.Windows.Markup.XamlWriter.Save(objectTree, xmlWriter);

        }

        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="manager"></param>                   
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static void SerializeObjectTree(object objectTree, XamlDesignerSerializationManager manager)
        {
            // Serialize
            System.Windows.Markup.XamlWriter.Save(objectTree, manager);
        }

        /// <summary>
        /// Serializes an object tree to a string.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static string SerializeObjectTree(object objectTree, XamlWriterMode expressionMode)
        {

            StringBuilder sb = new StringBuilder();
            TextWriter writer = new StringWriter(sb);
            XmlTextWriter xmlWriter = null;

            try
            {
                // Create XmlTextWriter
                xmlWriter = new XmlTextWriter(writer);

                // Set serialization mode
                xmlWriter.Formatting = Formatting.Indented;
                XamlDesignerSerializationManager manager = new XamlDesignerSerializationManager(xmlWriter);
                manager.XamlWriterMode = expressionMode;

                // Serialize
                SerializeObjectTree(objectTree, manager);
            }
            finally
            {
                if (xmlWriter != null)
                    xmlWriter.Close();
            }

            return sb.ToString();
        }

        #region Security Wrappers

        /// <summary>
        /// Helper routine to create a Window.  This routine asserts permission
        /// so the caller may create a Window in partial trust.
        /// </summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static Window CreateWindow()
        {
            Window win = new Window();
            Point point = UpdatePosition(100, 100);
            win.Top = point.Y;
            win.Left = point.X;

            return win;
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private void _SetWindowVisibility(Window window, bool show)
        {
            if (show)
            {
                window.Show();
            }
            else
            {
                window.Hide();
            }
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static void _ShutdownDispatcher()
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static void _SetDispatcherFrameContinue(DispatcherFrame dispatcherFrame, bool value)
        {
            dispatcherFrame.Continue = value;
        }

        #endregion

        /// <summary>
        /// Serializes an object tree to a file.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="fileName">The name of the xaml file that will be created.</param>
        public static void SerializeObjectTree(object objectTree, string fileName)
        {
            SerializationHelper.SerializeObjectTree(objectTree, XamlWriterMode.Value, fileName);
        }

        /// <summary>
        /// Serializes an object tree to a file.
        /// </summary>
        /// <param name="objectTree">The tree to serialize.</param>
        /// <param name="expressionMode">The mode of serialization for databinding and style expansion. Default: XamlWriterMode.Value.</param>
        /// <param name="fileName">The name of the xaml file that will be created.</param>
        public static void SerializeObjectTree(object objectTree, XamlWriterMode expressionMode, string fileName)
        {
            if (objectTree == null)
            {
                throw new ArgumentNullException("objectTree");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                string outer = SerializeObjectTree(objectTree, expressionMode);

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.Write(outer);
            }
            finally
            {
                if (null != sw)
                    sw.Close();

                if (null != fs)
                    fs.Close();
            }
        }

        /// <summary>
        /// Event provides serialized xaml to listeners.
        /// </summary>
        public event XamlSerializedEventHandler XamlSerialized;

        /// <summary>
        /// Event to be fired after the first tree has been rendered
        /// </summary>
        public event SerializationCustomerEventHandler FirstDisplay;

        /// <summary>
        /// Event to be fired after the second tree has been rendered
        /// </summary>
        public event SerializationCustomerEventHandler SecondDisplay;

        /// <summary>
        /// Event to be fired just before the first tree has been rendered
        /// </summary>
        public event SerializationCustomerEventHandler PreFirstDisplay;

        /// <summary>
        /// Event to be fired just before the second tree has been rendered
        /// </summary>
        public event SerializationCustomerEventHandler PreSecondDisplay;

        /// <summary>
        /// Event to be fired just before the first tree has been serialized
        /// </summary>
        public event SerializationCustomerEventHandler PreFirstSerialization;

        /// <summary>
        /// Event to be fired just before the second tree has been serialized
        /// </summary>
        public event SerializationCustomerEventHandler PreSecondSerialization;

        /// <summary>
        /// Calls handlers of XamlSerialized.
        /// </summary>
        private void _FireXamlSerializedEvent(string xaml)
        {
            // Call event handlers.
            if (XamlSerialized != null)
            {
                XamlSerialized(null, new XamlSerializedEventArgs(xaml));
            }
        }

        #region DisplayTree
        private bool _AttachRenderedHandler(object root)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            if (dispatcher == null)
            {
                throw new InvalidOperationException("Cannot show the tree. The current thread has not entered a Dispatcher.");
            }

            dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new DispatcherOperationCallback(_OnLoaded), root);

            return true;
        }

        /// <summary>
        /// Displays a tree by putting it in an HwndSource and running a dispatcher.
        /// </summary>
        /// <param name="root">The root of the tree to display.</param>
        /// <remarks>Dispatcher must be entered prior to calling helper functions.</remarks>
        public void DisplayTree(object root)
        {
            DisplayTree(root, String.Empty);
        }

        /// <summary>
        /// Displays a tree by putting it in an HwndSource and running a dispatcher.
        /// </summary>
        /// <param name="root">The root of the tree to display.</param>
        /// <param name="titleSuffix">A string to append to the window title bar.</param>
        /// <remarks>Dispatcher must be entered prior to calling helper functions.</remarks>
        public void DisplayTree(object root, string titleSuffix)
        {
            DisplayTree(root, titleSuffix, false);
        }

        /// <summary>
        /// Displays a tree by putting it in an HwndSource and running a dispatcher. 
        /// </summary>
        /// <param name="root">The root of the tree to display.</param>
        /// <param name="titleSuffix">A string to append to the window title bar.</param>
        /// <param name="continueDispatcher">Whether or not continue dispatcher after display tree. Works only with Custome Elements defined under CustomElements</param>
        public void DisplayTree(object root, string titleSuffix, bool continueDispatcher)
        {
            _continueDispatcher = continueDispatcher;
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            if (root is UIElement && LogicalTreeHelper.GetParent((UIElement)root) != null)
            {
                throw new ArgumentException("root", "The given node has a parent.");
            }

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            if (dispatcher == null)
            {
                throw new InvalidOperationException("Cannot show the tree. The current thread has not entered a Dispatcher.");
            }

            if (!_AttachRenderedHandler(root))
            {
                GlobalLog.LogStatus("Not displaying element tree; root not recognized as framework element or custom test element.");
                return;
            }

            // Get window - create one if necessary.
            Window win = null;

            if (root is Window)
            {
                win = (Window)root;
            }
            else
            {
                win = CreateWindow();
                win.Content = root;
            }


            Point point = UpdatePosition(100, 100);

            // Set window size.
            win.Top = point.Y;
            win.Left = point.X;
            win.Width = 800;
            win.Height = 700;

            // Show window.
            _SetWindowVisibility(win, true);

            // Run dispatcher.
            GlobalLog.LogStatus("Running dispatcher...");
            _dispatcherFrame = new DispatcherFrame();
            Dispatcher.PushFrame(_dispatcherFrame);

            GlobalLog.LogStatus("Dispatcher is done...");
            _dispatcherFrame = null;

            // If an exception happened within the Avalon app,
            // forward that exception now.
            Exception testResultEx = RetrieveException();

            if (testResultEx != null)
            {
                StoreException(null);
                throw new Exception("Exception was logged while displaying tree.\r\n\r\n" + testResultEx);
            }

            // If an failure was "logged" while displaying the tree, 
            // forward the failure now.
            string testResultMsg = testResultStore;

            if (testResultMsg != null)
            {
                testResultStore = null;
                if (testResultMsg.ToLowerInvariant() == "fail")
                    throw new Exception("Fail was logged while displaying tree.");
            }

            if (!_didRender)
                throw new Exception("ICustomElement.RenderedEvent handler did not fire.");
            else
                _didRender = false;

            // Hide the Window so it doesn't serialize with
            // Visibility on.
            _SetWindowVisibility(win, false);
        }


        private static bool IsMultiMonitor()
        {
            return MTD.Monitor.GetAllEnabled().Length > 1;
        }

        /// <summary>
        /// </summary>
        private static Point UpdatePosition(int x, int y)
        {
            if (IsMultiMonitor())
            {

                MTD.Monitor[] monitors = MTD.Monitor.GetAllEnabled();

                
                int monitorIndex = 0;

                if (monitors[monitorIndex].IsPrimary)
                {
                    monitorIndex++;
                }

                MTD.Monitor monitor = monitors[monitorIndex];

                return new Point(monitor.WorkingArea.left + x, monitor.WorkingArea.top + y);

            }
            return new Point(x, y);

        }


        /// <summary>
        /// You should not call InvokeShutdown to close the displayed tree.
        /// Instead use this API
        /// </summary>
        public static void CloseDisplayedTree()
        {
            if (_continueDispatcher)
            {
                if (_dispatcherFrame == null)
                    throw new InvalidOperationException("No tree is displayed.");

                _SetDispatcherFrameContinue(_dispatcherFrame, false);
            }
            else
            {
                _ShutdownDispatcher();
            }
        }

        /// <summary>
        /// Stores an exception in such a way that 
        /// it can be retrieved from another AppDomain, Process,
        /// or even another machine
        /// </summary>
        /// <param name="e"></param>
        public static void StoreException(Exception e)
        {
            exceptionStore = e;
        }

        /// <summary>
        /// Retrieves the exception stored previously by StoreException.
        /// The exception may be previously stored in a different AppDomain,
        /// Process or even different machine.
        /// </summary>
        /// <returns></returns>
        public static Exception RetrieveException()
        {
            return exceptionStore;
        }

        /// <summary>
        /// EventHandler action when OnRender is called.
        /// </summary>

        private object _OnLoaded(object root)
        {
            _didRender = true;

            // Quit the dispatcher if the sender isn't a ICustomElement.
            // ICustomElement will quit the dispatcher itself after it tries
            // to do some custom test verification.  But non-ICustomElements
            // will not quit the dispatcher or close the window, so we need
            // to do it for them.
            if (_isFirst)
            {
                if (FirstDisplay != null)
                {
                    FirstDisplay(_firstTreeRoot);
                }
            }
            else
            {
                if (SecondDisplay != null)
                {
                    SecondDisplay(_secondTreeRoot);
                }
            }

            if (!(root is ICustomElement))
            {
                DispatcherObject dispatcherObject = root as DispatcherObject;

                _SetDispatcherFrameContinue(_dispatcherFrame, false);
            }
            return null;
        }

        #endregion DisplayTree

        /// <summary>
        /// Compares two object trees.
        /// </summary>
        private void _CompareObjectTree(object firstTree, object secondTree)
        {
            TreeCompareResult result = TreeComparer.CompareLogical(firstTree, secondTree);

            if (CompareResult.Different == result.Result)
            {
                throw new Exception("Failure occurred while comparing object trees.");
            }
        }

        #region Properties and Fields

        /// <summary>
        /// Flag to direct the round trip test to compare the
        /// first serialized xaml to the original xaml.
        /// Default: false.
        /// </summary>
        public bool CompareOriginalXaml
        {
            get
            {
                return _compareOriginalXaml;
            }
            set
            {
                _compareOriginalXaml = value;
            }
        }

        /// <summary>
        /// Array of elements whose children sequence should be ignored
        /// when comparing xamls.
        /// </summary>
        public string[] NodesShouldIgnoreChildrenSequence
        {
            get
            {
                return _nodesShouldIgnoreChildrenSequence;
            }
            set
            {
                _nodesShouldIgnoreChildrenSequence = value;
            }
        }

        /// <summary>
        /// To disable the tree comparison and the xaml comparison
        /// </summary>
        public void DisableComparison()
        {
            DoXamlComparison = false;
            DoTreeComparison = false;
        }



        /// <summary>
        /// Property to specify whether or not to compare the xamls
        /// in serialization round trip.
        /// </summary>
        /// <value>Whether or not to compare the xamls.</value>
        public bool DoXamlComparison
        {
            get
            {
                return _doXamlComparison;
            }
            set
            {
                _doXamlComparison = value;
            }
        }

        /// <summary>
        /// Property to specify whether or not to compare the trees
        /// in serialization round trip.
        /// </summary>
        /// <value>Whether or not to compare the trees.</value>
        public bool DoTreeComparison
        {
            get
            {
                return _doTreeComparison;
            }
            set
            {
                _doTreeComparison = value;
            }
        }

        /// <summary>
        /// Property to specify whether where is Tree Comparison 
        /// in the serialization round trip
        /// </summary>
        /// <value>whether or not to Compare the Trees</value>
        public bool AlwaysTestExpressionMode
        {
            get
            {
                return _alwaysTestExpressionMode;
            }
            set
            {
                _alwaysTestExpressionMode = value;
            }
        }

        private string[] _nodesShouldIgnoreChildrenSequence = null;

        private static double _iteration = 0;

        private bool _didRender = false;
        private string _originalXaml = null;
        private bool _compareOriginalXaml = false;
        private object _firstTreeRoot;
        private object _secondTreeRoot;
        private bool _isFirst = true;
        private bool _doXamlComparison = true;
        private bool _doTreeComparison = true;
        private bool _alwaysTestExpressionMode = true;
        private static bool _continueDispatcher = false;
        private bool _roundTripTestExit = false;
        private static DispatcherFrame _dispatcherFrame = null;
        private static Exception exceptionStore = null;
        private static string testResultStore = null;

        #endregion Properties and Fields

    }

    /// <summary>
    /// Event handler to use for XamlSerialized events.
    /// </summary>
    public delegate void XamlSerializedEventHandler(object sender, XamlSerializedEventArgs args);

    /// <summary>
    /// Event handler provide the customer the root of the object tree
    /// </summary>
    public delegate void SerializationCustomerEventHandler(object treeRoot);


    /// <summary>
    /// Holds serialized xaml for round trip function listeners.
    /// </summary>
    public class XamlSerializedEventArgs : EventArgs
    {
        string _xaml = String.Empty;

        /// <summary>
        /// Create args with serialized xaml.
        /// </summary>
        /// <param name="xaml">Serialized xaml.</param>
        public XamlSerializedEventArgs(string xaml)
        {
            _xaml = xaml;
        }

        /// <summary>
        /// Return the serialized xaml.
        /// </summary>
        public string Xaml
        {
            get
            {
                return _xaml;
            }
        }
    }

}
