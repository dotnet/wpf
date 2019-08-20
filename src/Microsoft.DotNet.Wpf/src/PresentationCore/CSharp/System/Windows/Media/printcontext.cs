// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Threading;

using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

using MS.Internal;
using System.Windows.Media;
using System.Windows.Media.Composition;

using System.Printing;
using System.Printing.PrintSubSystem;
using System.Printing.Configuration;
using System.Printing.Interop;
using Microsoft.Printing.JobTicket;

using MS.Win32;

namespace System.Windows.Media
{
    #region internal class Misc
    /// <summary>
    /// internal class Misc
    /// </summary>
    internal class Misc
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="jobTicket"></param>
        /// <param name="printerName"></param>
        public Misc(JobTicket jobTicket, string printerName)
        {
            JobTicketConverter jtc = new JobTicketConverter();

            jtc.ClientPrintSchemaVersion = 1;
            jtc.DeviceName = printerName;

            byte [] devmode = jtc.ConvertJobTicketToDevMode(jobTicket.XmlStream, BaseDevModeType.UserDefault);

            _pDevmode = Marshal.AllocHGlobal(devmode.Length);
            
            Marshal.Copy(devmode, 0, _pDevmode, devmode.Length);

            jtc.Dispose();
        }

        #region IDisposable

        /// <summary>
        /// Implement IDisposable.
        /// Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            this.Cleanup(disposing);
        }

        /// This method is required for the scenario in which the derived class either inadvertently
        /// or intentionally does not defer the Dispose(booldisposing) call to the base class. It ensures
        /// that managed and unmanaged resources are always cleaned up.
        private void Cleanup(bool disposing)
        {
            // Call the appropriate methods to clean up
            // unmanaged resources here.
            if( _pDevmode != IntPtr.Zero )
            {
                Marshal.FreeHGlobal(_pDevmode);
                _pDevmode = IntPtr.Zero;
            }
        }

        #endregion //IDisposable

        /// <summary>
        /// Destructor
        /// </summary>
        ~Misc()
        {
            this.Dispose(false);
            this.Cleanup(false);
        }

        /// <summary>
        /// pDevmode
        /// </summary>
        public IntPtr pDevmode
        {
            get
            {
                return _pDevmode;
            }
        }
        
        IntPtr          _pDevmode;
    }

    #endregion

    // Override GetResourceSystem such that resource can be embedded in PrintSystemEDocmentJob
    internal class ContainerFixedXamlDesigner : FixedXamlDesigner
    {
        private PrintSystemEDocumentJob _eDocJob;
        private string                  _pageUri;
        private PrintSystemImage        _lastImage;

        public ContainerFixedXamlDesigner(PrintSystemEDocumentJob  eDocJob, string uri) : base()
        {
            _eDocJob = eDocJob;
            _pageUri = uri;
        }

        // Add a stream to the container for resource embedding
        override public System.IO.Stream GetResourceStream(string name, out string uri)
        {
            if (_eDocJob != null)
            {
                _lastImage = _eDocJob.AddImage(name);
                uri = _lastImage.RelativeUri(_pageUri);
                return _lastImage.GetStream();
            }
            else
            {
                return base.GetResourceStream(name, out uri);
            }
        }

        override public void CommitLastStream()
        {
            if ((_eDocJob != null) && (_lastImage != null))
            {
                _lastImage.Commit();
                _lastImage = null;
            }
            else
            {
                base.CommitLastStream();
            }
        }
    }

    #region public class PrintContext
    /// <summary>
    /// PrintContext provides visual level printing API
    /// </summary>
    public class PrintContext : DispatcherObject
    {
        /// <summary>
        /// Connect to a specified print queue
        /// </summary>
        /// <param name="queue"></param>
        public PrintContext(PrintQueue queue)
        {
            if( queue == null )
            {
               throw new ArgumentNullException("queue"); 
            }
            
            // enter batching for printing
            MediaContext.CurrentMediaContext.ChannelSyncMode = true;

            _queue     = queue;
            _jobTicket = new JobTicket(queue.UserJobTicket); // Make a new copy
        }

        #region Print()
        /// <summary>
        /// Print a job
        /// </summary>
        public void Print(GetPageCallback getPage, string jobName)
        {
            try
            {
                if (getPage == null)
                {
                    throw new ArgumentNullException("getPage");
                }

                this.StartDoc(jobName);

                int pageNo = 0;
                
                Visual visual = getPage(this);

                while (!_cancel && (visual != null))
                {
                    string uri = this.StartPage(pageNo);

                    this.Render(visual, uri);
                    this.EndPage();
                    
                    pageNo++;
                    
                    visual = getPage(this);
                }

                this.EndDoc();
            }
            finally
            {
                // restore normal batching
                MediaContext.CurrentMediaContext.ChannelSyncMode = false;
                
                _stream = null;
                _writer = null;

                _eDocRendition = null;
                _eDocJob = null;
            }
        }
        #endregion

        /// <summary>
        /// Job cancelling property.
        /// </summary>
        /// <value>Set to true to cancel a job during Print() method call.</value>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }
            set
            {
                _cancel = value;
            }
        }
        
        /// <summary>
        /// JobTicket property
        /// </summary>
        public JobTicket JobTicket
        {
            get
            {
                return _jobTicket;
            }
            set
            {
                if( value == null )
                {
                    throw new ArgumentNullException("value");
                }
                _jobTicket = value;
            }
        }

        private bool IsPremium()
        {
            if (_queue.IsDualHeaded)
            {
                return true;
            }

            // Temp solution to allow premium printing on normal print queue
            if ((_queue.Comment != null) && (_queue.Comment.Length >= 4))
            {
                return (_queue.Comment[0] == 'x') || (_queue.Comment[0] == 'e');
            }

            return false;
        }

        private bool SpoolEdoc()
        {
            if (_queue.IsDualHeaded)
            {
                return true;
            }

            // Temp solution to allow premium printing on normal print queue, using edoc spool file
            // If IsPremium is true and SpoolEdoc is false, xaml file will be generated
            if ((_queue.Comment != null) && (_queue.Comment.Length >= 4))
            {
                return _queue.Comment[0] == 'e';
            }

            return false;
        }

        // Temp solution to get file name to print to
        private string GetOutputFilename()
        {
            // gdi#<filename>
            // xaml#<filename>
            // edoc#<filename>
            if (_queue.Comment != null)
            {
                int index = _queue.Comment.LastIndexOf('#');

                if (index >= 0)
                {
                    return _queue.Comment.Substring(index + 1);
                }
            }

            return null;
        }

        /// <summary>
        /// delegate GetPageCallback
        /// </summary>
        public delegate Visual GetPageCallback(PrintContext printContext);
        
        #region Private methods
        
        /// <summary>
        /// StartDoc
        /// </summary>
        private void StartDoc(string jobName)
        {
            MediaSystem.Startup();

            if (IsPremium())
            {
                if (SpoolEdoc())
                {
                    // Job
                    PrintSystemJobInfo jobinfo = _queue.AddJob(typeof(PrintSystemEDocumentJob));

                    _eDocJob = jobinfo.JobData as PrintSystemEDocumentJob;
                                            
                    _eDocJob.Name = jobName;
                    _eDocJob.Commit();

                    // Document
                    PrintSystemDocument doc = _eDocJob.AddDocument("XAML Document");

                    doc.Name = "XAML Document";
                    doc.JobTicket = _jobTicket.XmlStream;
                    doc.Commit();

                    // Rendition
                    _eDocRendition = doc.AddRendition("Letter");
                    _eDocRendition.Name = "A4 Rendition";
                    _eDocRendition.JobTicket = _jobTicket.XmlStream;
                    _eDocRendition.Commit();
                }
                else
                {
                    string filename = GetOutputFilename();

                    _stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                    _writer = new System.Xml.XmlTextWriter(_stream, System.Text.Encoding.UTF8);
                    _writer.Formatting = System.Xml.Formatting.Indented;
                    _writer.Indentation = 4;
                    _writer.WriteStartElement("Document");
                    _writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                    _writer.WriteAttributeString("xmlns:def", "http://schemas.microsoft.com/winfx/2006/xaml");
                    _writer.WriteStartElement("FixedPanel");
                }
            }
        }

        /// <summary>
        /// EndDoc
        /// </summary>
        private void EndDoc()
        {
            // Need to implement Cancel...

            if (IsPremium())
            {
                if (SpoolEdoc())
                {
                    if (_eDocRendition != null)
                    {
                        _eDocRendition.Commit();
                        _eDocRendition = null;

                        _eDocJob = null;
                    }
                }
                else
                {
                    _writer.WriteEndElement(); // FixedPanel
                    _writer.WriteEndElement(); // Document
                    _writer.Close();
                    _stream.Close();
                    _writer = null;
                    _stream = null;
                }
            }
        }

        /// <summary>
        /// StartPage
        /// </summary>
        private string StartPage(int pageNo)
        {
            string uri = null;

            if (IsPremium())
            {
                if (SpoolEdoc())
                {
                    if (_eDocRendition != null)
                    {
                        PrintSystemPage page = _eDocRendition.AddPage("Page " + (pageNo + 1));

                        uri = page.Uri;

                        page.JobTicket = _jobTicket.XmlStream;

                        _stream = page.GetStream();

                        _writer = new System.Xml.XmlTextWriter(_stream, System.Text.Encoding.UTF8);
                        _writer.Formatting = System.Xml.Formatting.Indented;
                        _writer.Indentation = 4;

                        _writer.WriteStartElement("FixedPanel");
                        _writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                        _writer.WriteAttributeString("xmlns:def", "http://schemas.microsoft.com/winfx/2006/xaml");
                    }
                }

                if (_writer != null)
                {
                    _writer.WriteStartElement("PageContent");
                    _writer.WriteStartElement("FixedPage");
                    _writer.WriteAttributeString("Width",  "" + _jobTicket.PageMediaSize.MediaSizeX + "in");
                    _writer.WriteAttributeString("Height", "" + _jobTicket.PageMediaSize.MediaSizeY + "in");
                    _writer.WriteAttributeString("Background", "White");
                    _writer.WriteStartElement("Canvas");
                }

            }

            return uri;
        }

        /// <summary>
        /// EndPage
        /// </summary>
        private void EndPage()
        {
            if (IsPremium())
            {
                if (_writer != null)
                {
                    _writer.WriteEndElement();
                    _writer.WriteEndElement();
                    _writer.WriteEndElement();

                    if (_eDocRendition != null)
                    {
                        _writer.WriteEndElement();
                    }
                }

                if (SpoolEdoc())
                {
                    _writer.Close();
                    _writer = null;
                    _stream.Close();
                    _stream = null;
                }
            }
        }

        static private void AssertPositive(int val, string name)
        {
            if (val <= 0)
            {
                throw new ArgumentOutOfRangeException(name, SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }
        }

        /// <summary>
        /// Render visual to printer.
        /// </summary>
        private void Render(Visual visual, string uri)
        {
            if (visual == null)
            {
                throw new ArgumentNullException("visual");
            }

            int dpiX  = _jobTicket.PageResolution.ResolutionX;
            int dpiY  = _jobTicket.PageResolution.ResolutionY;

            if (dpiX < 0) { dpiX = 600; }

            if (dpiY < 0) { dpiY = 600; }

            AssertPositive(dpiX, "PageResolution.ResolutionX");
            AssertPositive(dpiY, "PageResolution.ResolutionY");

            int sizeX = (int) (_jobTicket.PageMediaSize.MediaSizeX * dpiX);
            int sizeY = (int) (_jobTicket.PageMediaSize.MediaSizeY * dpiY);

            AssertPositive(sizeX, "PageCanvasSizeCap.CanvasSizeX");
            AssertPositive(sizeY, "PageCanvasSizeCap.CanvasSizeY");
            
            if (IsPremium())
            {
                VisualTreeFlattener.SaveAsXml(visual, _writer, new ContainerFixedXamlDesigner(_eDocJob, uri));
            }
        }

        #endregion

        #region Private Member Variables

        // Member variables associated with PrintContext
        private bool         _cancel;
        private PrintQueue   _queue;
        private JobTicket    _jobTicket;
        
        // Member variables used for premium print job
        private Stream                   _stream;
        private System.Xml.XmlTextWriter _writer;

        private PrintSystemRendition     _eDocRendition;
        private PrintSystemEDocumentJob  _eDocJob;
        #endregion
    }
    #endregion
}
