// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implements a monitored stream so that all calls to the base stream
// can be intersected and thereby loading progress can be accurately measured.
//


using System;
using System.IO;
using System.Security; // SecurityCritical attribute
using MS.Internal.AppModel;
using System.Net;
using System.Windows.Threading; //DispatcherObject
using System.Windows.Markup;
using System.Reflection;

namespace MS.Internal.Navigation
{
    #region BindStream Class

    // Implements a monitored stream so that all calls to the base stream
    // can be intersected and thereby loading progress can be accurately measured.
    internal class BindStream : Stream, IStreamInfo
    {
        #region Constructors

        internal BindStream(Stream stream, long maxBytes,
                            Uri uri, IContentContainer cc, Dispatcher callbackDispatcher)
        {
            _bytesRead = 0;
            _maxBytes = maxBytes;
            _lastProgressEventByte = 0;
            _stream = stream;
            _uri = uri;
            _cc = cc;
            _callbackDispatcher = callbackDispatcher;
        }
        
        #endregion Constructors

        #region Private Methods
        
        private void UpdateNavigationProgress()
        {            
            for(long numBytes =_lastProgressEventByte + _bytesInterval; 
                numBytes <= _bytesRead; 
                numBytes += _bytesInterval)
            {
                UpdateNavProgressHelper(numBytes);
                _lastProgressEventByte = numBytes;                
            }

            if (_bytesRead == _maxBytes && _lastProgressEventByte < _maxBytes)
            {
                UpdateNavProgressHelper(_maxBytes);
                _lastProgressEventByte = _maxBytes;
            }
        }

        private void UpdateNavProgressHelper(long numBytes)
        {
            if ((_callbackDispatcher != null) && (_callbackDispatcher.CheckAccess() != true))
            {
                _callbackDispatcher.BeginInvoke(
                                DispatcherPriority.Send,
                                (DispatcherOperationCallback)delegate(object unused)
                                {
                                    _cc.OnNavigationProgress(_uri, numBytes, _maxBytes);
                                    return null;
                                },
                                null);
            }
            else
            {
                _cc.OnNavigationProgress(_uri, numBytes, _maxBytes);
            }
        }

        #endregion Private Methods

        #region Overrides
        
        #region Overridden Properties                 
        
        /// <summary>
        /// Overridden CanRead Property
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        /// <summary>
        /// Overridden CanSeek Property
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        /// <summary>
        /// Overridden CanWrite Property
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return _stream.CanWrite;
            }
        }

        /// <summary>
        /// Overridden Length Property
        /// </summary>
        public override long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        /// <summary>
        /// Overridden Position Property
        /// </summary>
        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        #endregion Overridden Properties                 
        
        #region Overridden Public Methods   
        
        /// <summary>
        /// Overridden BeginRead method
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
            )
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Overridden BeginWrite method
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginWrite(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state
            )
        {
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Overridden Close method
        /// </summary>
        public override void Close()
        {
            _stream.Close();

            // if current dispatcher is not the same as the dispatcher we should call back on,
            // post to the call back dispatcher.
            if ((_callbackDispatcher != null) && (_callbackDispatcher.CheckAccess() != true))
            {
                _callbackDispatcher.BeginInvoke(
                                DispatcherPriority.Send,
                                (DispatcherOperationCallback)delegate(object unused)
                                {
                                    _cc.OnStreamClosed(_uri);
                                    return null;
                                },
                                null);
            }
            else
            {
                _cc.OnStreamClosed(_uri);
            }
        }

        /// <summary>
        /// Overridden EndRead method
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override int EndRead(
            IAsyncResult asyncResult
            )
        {
            return _stream.EndRead(asyncResult);
        }

        /// <summary>
        /// Overridden EndWrite method
        /// </summary>
        /// <param name="asyncResult"></param>
        public override void EndWrite(
            IAsyncResult asyncResult
            )
        {
            _stream.EndWrite(asyncResult);
        }

        /// <summary>
        /// Overridden Equals method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(
            object obj
            )
        {
            return _stream.Equals(obj);
        }

        /// <summary>
        /// Overridden Flush method
        /// </summary>
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <summary>
        /// Overridden GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        /*/// <summary>
        /// GetLifetimeService method
        /// </summary>
        /// <returns></returns>
        // Do we want this method here?
        public new object GetLifetimeService()
        {
            return _stream.GetLifetimeService();
        }

        /// <summary>
        /// GetType method
        /// </summary>
        /// <returns></returns>
        // Do we want this method here?
        public new Type GetType()
        {
            return _stream.GetType();
        }*/

        /// <summary>
        /// Overridden InitializeLifetimeService method
        /// </summary>
        /// <returns></returns>
        [ObsoleteAttribute("InitializeLifetimeService is obsolete.", false)]
        public override object InitializeLifetimeService()
        {
            return _stream.InitializeLifetimeService();
        }

        /// <summary>
        /// Overridden Read method
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(
            byte[] buffer,
            int offset,
            int count
            )
        {
            int bytes = _stream.Read(buffer, offset, count);
            _bytesRead += bytes;

            //File, http, stream resources all seem to pass in a valid maxbytes. 
            //Incase loading compressed container or asp files serving out content cause maxbytes
            //to be not known upfront or a webserver does not send a content length(does http spec mandate it), 
            //update the maxbytes dynamically here. Also if we reach the end of the download stream
            //force a NavigationProgress event with the last set of bytes and the final maxbytes value

            _maxBytes = _bytesRead > _maxBytes ? _bytesRead : _maxBytes;

            //Make sure the final notification is sent incase _maxBytes falls below the interval boundary
            if (((_lastProgressEventByte + _bytesInterval) <= _bytesRead) ||
                bytes == 0)
            {
                UpdateNavigationProgress();
            }

            return bytes;
        }

        /// <summary>
        /// Overridden ReadByte method
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            int val = _stream.ReadByte();
            if (val != -1)
            {
                _bytesRead++;
                _maxBytes = _bytesRead > _maxBytes ? _bytesRead : _maxBytes;
            }

            //Make sure the final notification is sent incase _maxBytes falls below the interval boundary
            if (((_lastProgressEventByte + _bytesInterval) <= _bytesRead) ||
                val == -1)
            {
                UpdateNavigationProgress();
            }

            return val;
        }

        /// <summary>
        /// Overridden Seek method
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(
            long offset,
            SeekOrigin origin
            )
        {
            return _stream.Seek(offset, origin);
        }

        /// <summary>
        /// Overridden SetLength method
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(
            long value
            )
        {
            _stream.SetLength(value);
        }

        /// <summary>
        /// Overridden ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _stream.ToString();
        }

        /// <summary>
        /// Overridden Write method
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(
            byte[] buffer,
            int offset,
            int count
            )
        {
            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Overridden WriteByte method
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(
            byte value
            )
        {
            _stream.WriteByte(value);
        }

        #endregion Overridden Public Methods                 

        #endregion Overrides

        #region Properties                 
        
        /// <summary>
        /// Underlying Stream of BindStream
        /// </summary>
        public Stream Stream
        {
            get
            {
                return _stream;
            }
        }

        //
        // Assembly which contains the stream data.
        //
        Assembly IStreamInfo.Assembly
        {
            get
            {
                Assembly assembly = null;
                if (_stream != null)
                {
                    IStreamInfo streamInfo = _stream as IStreamInfo;
                    if (streamInfo != null)
                    {
                        assembly = streamInfo.Assembly;
                    }
                }

                return assembly;
            }
        }

        #endregion Properties                         

        #region Private Data

        long                    _bytesRead;
        long                    _maxBytes;
        long                    _lastProgressEventByte;
        Stream                  _stream;
        Uri                     _uri;
        IContentContainer        _cc;
        Dispatcher              _callbackDispatcher;
        private const long      _bytesInterval = 1024;

        #endregion Private Data
    }

    #endregion BindStream Class
}
