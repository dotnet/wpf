// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose: Class that builds a stream from XAML or BAML and handles
*
\***************************************************************************/

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;

using System.Runtime.InteropServices;
using MS.Utility;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Threading;


namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// internal base class that acts as the Traffic cop for reading BAMl and XMAL.
    /// The XamlTreeBuilder and TreeBuilderBamlTranslator class override this class for each
    /// of there implementations.
    /// </summary>
    internal abstract class TreeBuilder
    {
        #region Constructors

        
        /// <summary>
        /// Constructor
        /// </summary>
        public TreeBuilder()
        {
        }

        #endregion Constructors

        #region Methods

#if !PBTCOMPILER
        /// <summary>
        /// Internal Avalon method. Used to load the file. Does not allow multiple roots.
        /// </summary>
        /// <returns>The root of the loaded file</returns>
        public object Parse ()
        {
            // if there is a BamlRecordReader setup the inherited Properties
            Debug.Assert (null != RecordReader, "No BamlRecordReader at Parse() time");
           
            object root = ParseFragment ();

            // If the root is a DependencyObject, then store the XamlTypeMapper's xmlns hashtable on
            // it so that it can be used for runtime lookups of xaml namespace prefixes.  Make
            // sure to enter the context associated with the root, since the context may change
            // for async parse cases
            if (root is DependencyObject && !RecordReader.IsRootAlreadyLoaded)
            {
                ((DependencyObject)root).SetValue(XmlAttributeProperties.XmlNamespaceMapsProperty, 
                                                  RecordReader.XamlTypeMapper.NamespaceMapHashList);
            }

            // Or, if the root is a MarupExtension, we need to convert it into it's final value.
            else if( root is MarkupExtension )
            {
                root = (root as MarkupExtension).ProvideValue(null);
            }

            return root;
        }
#endif        

        /// <summary>
        /// Internal Avalon method. Used to Load a file.
        /// </summary>
        /// <returns>An array containing the root objects Loaded from the stream stream</returns>
        public virtual object ParseFragment ()
        {
            Debug.Assert (false, "Parse Fragment was not overidden");
            return null;
        }

#if !PBTCOMPILER // No async support for compilation

        /// <summary>
        /// called when in async mode when get a time slice to read and load the Tree
        /// </summary>
        internal virtual void HandleAsyncQueueItem ()
        {
            Debug.Assert (false == RecordReader.EndOfDocument); // shouldn't be called after end

            try
            {
                int startTickCount = MS.Win32.SafeNativeMethods.GetTickCount();
                bool moreData = true;

                // It is possible that we have encountered an exception while parsing/loading 
                // the root element which would have happened prior to this. Hence the additional checks.
                
                if (!_parseStarted && !RecordReader.EndOfDocument && !ParseError && !ParseCancelled)
                {
                    if(RecordReader.BytesAvailible < BamlVersionHeader.BinarySerializationSize)
                    {
                        return;
                    }
                    RecordReader.ReadVersionHeader();
                    _parseStarted = true;
                }

                // for debugging, can set the Maximum Async records to 
                // read via markup
                // x:AsyncRecords="3" would loop three times
                int maxRecords = MaxAsyncRecords;

                // while there is moreData and not at the end of document.
                while (moreData && !RecordReader.EndOfDocument && !ParseError && !ParseCancelled)
                {
                    moreData = RecordReader.Read (true /* single record  mode*/);
                    //Debug.Assert (1 >= RootList.Count, "Multiple roots not supported in async mode");

                    // check the timeout
                    int elapsed = MS.Win32.SafeNativeMethods.GetTickCount() - startTickCount;
                    
                    // check for rollover
                    if (elapsed < 0)
                    {
                        startTickCount = 0; // reset to 0,  
                    }
                    else if (elapsed > AsyncLoopTimeout)
                    {
                        break;
                    }

                    // decrement and compare with zero so the unitialized -1  and zero case
                    // doesn't break the loop.
                    if (--maxRecords == 0)
                    {
                        break;
                    }

                }
            }
            catch (XamlParseException e)
            {
                ParseException = e;
            }
            finally
            {
                if (ParseError || ParseCancelled)
                {
                    Debug.Assert (ParseError == (null != ParseException), "ParseError flag and ParseException must match up");
                    TreeBuildComplete();
                }
                else
                {
                    // if not at the EndOfDocument then post another work item
                    if (false == RecordReader.EndOfDocument)
                    {
                        Post ();
                    }
                    else
                    {
                        // Building of the Tree is complete.
                        TreeBuildComplete ();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Called when Tree has been completely built.
        /// </summary>
        internal virtual void TreeBuildComplete ()
        {
        }


        #endregion Methods
 
        #region Properties
      
#if !PBTCOMPILER
        /// <summary>
        ///  ParserHooks implementation
        /// </summary>
        internal ParserHooks ParserHooks
        {
            get { return _hooks; }
            set { _hooks = value; }
        }

        /// <summary>
        /// Root element
        /// </summary>
        internal object GetRoot()
        {
            ArrayList roots = RecordReader.RootList;                
            object root = (null == roots || 0 == roots.Count) ? null : roots[0];

            if (root != null && roots.Count > 1)
            {
                throw new XamlParseException(SR.Get(SRID.ParserMultiRoot));
            }
            
            return root; 
        }

        /// <summary>
        /// BamlRecordReader used for Loading the Tree
        /// </summary>
        internal BamlRecordReader RecordReader
        {
            get { return _bamlRecordReader; }
            set { _bamlRecordReader = value; }
        }

#endif

#if !PBTCOMPILER
        /// <summary>
        /// Mode we are loading the file in Sync, Async
        /// </summary>
        internal XamlParseMode XamlParseMode
        {
            get { return _xamlParseMode; }
            set { _xamlParseMode = value; }
        }

        /// <summary>
        /// Maximum amount of records allowed to read  within
        /// a HandleAsync call
        /// </summary>
        internal int MaxAsyncRecords
        {
            get { return _maxAsyncRecords; }
            set { _maxAsyncRecords = value; }
        }

        /// <summary>
        /// flag to indicate a ParseError occured
        /// </summary>
        internal bool ParseError
        {
            get { return _parseException != null; }
        }

        /// <summary>
        /// Exception that caused the Parse Error
        /// </summary>
        internal Exception ParseException
        {
            get { return _parseException; }
            set { _parseException = value; }
        }

        /// <summary>
        /// Flag to indicate that the Parse was Cancelled before the tree was
        /// completely loaded.
        /// </summary>
        internal bool ParseCancelled
        {
            get { return _parseCancelled; }
            set { _parseCancelled = value; }
        }
#endif

        #endregion Properties

        #region Queuing


#if !PBTCOMPILER
        /// <summary>
        ///  Post a queue item at default priority
        /// </summary>
        internal void Post ()
        {
            Post (DispatcherPriority.Background);
        }

        /// <summary>
        ///  Post a queue item at the specified priority
        /// </summary>
        internal void Post (DispatcherPriority priority)
        {
            DispatcherOperationCallback callback = new DispatcherOperationCallback (Dispatch);
            Dispatcher.CurrentDispatcher.BeginInvoke(priority, callback, this);
        }


        /// <summary>
        ///  Dispatch delegate
        /// </summary>
        private object Dispatch (object o)
        {
            DispatchParserQueueEvent ((TreeBuilder)o);
            return null;
        }

        private void DispatchParserQueueEvent (TreeBuilder treeBuilder)
        {
            treeBuilder.HandleAsyncQueueItem ();
        }

#endif

        #endregion Queuing
        
        #region Data

        // Timeout after .2 seconds. 
        const int AsyncLoopTimeout = (int)200;

#if !PBTCOMPILER
        BamlRecordReader _bamlRecordReader;
        XamlParseMode _xamlParseMode;
        ParserHooks _hooks;
        bool _parseCancelled;
        bool _parseStarted;
        Exception _parseException;
        int _maxAsyncRecords;
#endif
    #endregion Data
    }
}
