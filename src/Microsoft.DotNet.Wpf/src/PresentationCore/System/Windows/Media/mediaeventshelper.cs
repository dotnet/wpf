// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using MS.Internal;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    #region MediaEventsHelper

    /// <summary>
    /// MediaEventsHelper
    /// Provides helper methods for media event related tasks
    /// </summary>
    internal class MediaEventsHelper : IInvokable
    {
        #region Constructors and Finalizers

        /// <summary>
        /// Constructor
        /// </summary>
        internal MediaEventsHelper(MediaPlayer mediaPlayer)
        {
            _mediaOpened = new DispatcherOperationCallback(OnMediaOpened);
            this.DispatcherMediaOpened += _mediaOpened;

            _mediaFailed = new DispatcherOperationCallback(OnMediaFailed);
            this.DispatcherMediaFailed += _mediaFailed;

            _mediaPrerolled = new DispatcherOperationCallback(OnMediaPrerolled);
            this.DispatcherMediaPrerolled += _mediaPrerolled;

            _mediaEnded = new DispatcherOperationCallback(OnMediaEnded);
            this.DispatcherMediaEnded += _mediaEnded;

            _bufferingStarted = new DispatcherOperationCallback(OnBufferingStarted);
            this.DispatcherBufferingStarted += _bufferingStarted;

            _bufferingEnded = new DispatcherOperationCallback(OnBufferingEnded);
            this.DispatcherBufferingEnded += _bufferingEnded;

            _scriptCommand = new DispatcherOperationCallback(OnScriptCommand);
            this.DispatcherScriptCommand += _scriptCommand;

            _newFrame = new DispatcherOperationCallback(OnNewFrame);
            this.DispatcherMediaNewFrame += _newFrame;

            SetSender(mediaPlayer);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Create
        /// </summary>
        internal static void CreateMediaEventsHelper(MediaPlayer mediaPlayer,
                                                     out MediaEventsHelper eventsHelper,
                                                     out SafeMILHandle unmanagedProxy)
        {
            eventsHelper = new MediaEventsHelper(mediaPlayer);

            // Created with ref count = 1. Since this object does not hold on
            // to the unmanaged proxy, the lifetime is now controlled by whoever
            // called CreateMediaEventsHelper.
            unmanagedProxy = EventProxyWrapper.CreateEventProxyWrapper(eventsHelper);
        }

        #endregion

        #region Internal Methods / Properties / Events

        /// <summary>
        /// Changes the sender of all events
        /// </summary>
        internal void SetSender(MediaPlayer sender)
        {
            Debug.Assert((sender != null), "Sender is null");
            Debug.Assert((sender.Dispatcher != null), "Dispatcher is null");

            _sender = sender;
            _dispatcher = sender.Dispatcher;
        }

        /// <summary>
        /// Raised when error is encountered.
        /// </summary>
        internal event EventHandler<ExceptionEventArgs> MediaFailed
        {
            add
            {
                _mediaFailedHelper.AddEvent(value);
            }
            remove
            {
                _mediaFailedHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when media loading is complete
        /// </summary>
        internal event EventHandler MediaOpened
        {
            add
            {
                _mediaOpenedHelper.AddEvent(value);
            }
            remove
            {
                _mediaOpenedHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when media prerolling is complete
        /// </summary>
        internal event EventHandler MediaPrerolled
        {
            add
            {
                _mediaPrerolledHelper.AddEvent(value);
            }
            remove
            {
                _mediaPrerolledHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when media playback is finished
        /// </summary>
        internal event EventHandler MediaEnded
        {
            add
            {
                _mediaEndedHelper.AddEvent(value);
            }
            remove
            {
                _mediaEndedHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when buffering begins
        /// </summary>
        internal event EventHandler BufferingStarted
        {
            add
            {
                _bufferingStartedHelper.AddEvent(value);
            }
            remove
            {
                _bufferingStartedHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when buffering is complete
        /// </summary>
        internal event EventHandler BufferingEnded
        {
            add
            {
                _bufferingEndedHelper.AddEvent(value);
            }
            remove
            {
                _bufferingEndedHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when a script command that is embedded in the media is reached.
        /// </summary>
        internal event EventHandler<MediaScriptCommandEventArgs> ScriptCommand
        {
            add
            {
                _scriptCommandHelper.AddEvent(value);
            }

            remove
            {
                _scriptCommandHelper.RemoveEvent(value);
            }
        }

        /// <summary>
        /// Raised when a requested media frame is reached.
        /// </summary>
        internal event EventHandler NewFrame
        {
            add
            {
                _newFrameHelper.AddEvent(value);
            }

            remove
            {
                _newFrameHelper.RemoveEvent(value);
            }
        }

        internal void RaiseMediaFailed(Exception e)
        {
            if (DispatcherMediaFailed != null)
            {
                _dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    DispatcherMediaFailed,
                    new ExceptionEventArgs(e));
            }
        }

        #endregion

        #region IInvokable

        /// <summary>
        /// Raises an event
        /// </summary>
        void IInvokable.RaiseEvent(byte[] buffer, int cb)
        {
            const int S_OK = 0;

            AVEvent avEventType = AVEvent.AVMediaNone;
            int failureHr = S_OK;
            int size = 0;

            //
            // Minumum size is the event enum, the error hresult and two
            // string lengths (as integers)
            //
            size = sizeof(int) * 4;

            //
            // The data could be larger, that is benign, in the case that we have
            // strings appended it will be.
            //
            if (cb < size)
            {
                Debug.Assert((cb == size), "Invalid event packet");
                return;
            }

            MemoryStream memStream = new MemoryStream(buffer);

            using (BinaryReader reader = new BinaryReader(memStream))
            {
                //
                // Unpack the event and the errorHResult
                //
                avEventType = (AVEvent)reader.ReadUInt32();
                failureHr   = (int)reader.ReadUInt32();

                switch(avEventType)
                {
                case AVEvent.AVMediaOpened:

                    if (DispatcherMediaOpened != null)
                    {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, DispatcherMediaOpened, null);
                    }
                    break;

                case AVEvent.AVMediaFailed:

                    RaiseMediaFailed(HRESULT.ConvertHRToException(failureHr));
                    break;

                case AVEvent.AVMediaBufferingStarted:

                    if (DispatcherBufferingStarted != null)
                    {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, DispatcherBufferingStarted, null);
                    }
                    break;

                case AVEvent.AVMediaBufferingEnded:

                    if (DispatcherBufferingEnded != null)
                    {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, DispatcherBufferingEnded, null);
                    }
                    break;

                case AVEvent.AVMediaEnded:

                    if (DispatcherMediaEnded != null)
                    {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, DispatcherMediaEnded, null);
                    }
                    break;

                case AVEvent.AVMediaPrerolled:

                    if (DispatcherMediaPrerolled != null)
                    {
                        _dispatcher.BeginInvoke(DispatcherPriority.Normal, DispatcherMediaPrerolled, null);
                    }
                    break;

                case AVEvent.AVMediaScriptCommand:

                    HandleScriptCommand(reader);
                    break;

                case AVEvent.AVMediaNewFrame:

                    if (DispatcherMediaNewFrame != null)
                    {
                        //
                        // We set frame updates to background because media is high frequency and bandwidth enough
                        // to interfere dramatically with input.
                        //
                        _dispatcher.BeginInvoke(DispatcherPriority.Background, DispatcherMediaNewFrame, null);
                    }
                    break;

                default:
                    //
                    // Default case intentionally not handled.
                    //
                    break;
                }
            }
        }

        #endregion

        #region Private Methods / Events

        /// <summary>
        /// Handle the parsing of script commands coming up from media that
        /// supports them. Then binary reader at this point will be positioned
        /// at the first of the string lengths encoded in the structure.
        /// </summary>
        private
        void
        HandleScriptCommand(
            BinaryReader        reader
            )
        {
            int parameterTypeLength = (int)reader.ReadUInt32();
            int parameterValueLength = (int)reader.ReadUInt32();

            if (DispatcherScriptCommand != null)
            {
                string parameterType = GetStringFromReader(reader, parameterTypeLength);
                string parameterValue = GetStringFromReader(reader, parameterValueLength);

                _dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    DispatcherScriptCommand,
                    new MediaScriptCommandEventArgs(
                            parameterType,
                            parameterValue));
            }
        }

        /// <summary>
        /// Reads in a sequence of unicode characters from a binary
        /// reader and inserts them into a StringBuilder. From this
        /// a string is returned.
        /// </summary>
        private
        string
        GetStringFromReader(
            BinaryReader    reader,
            int             stringLength
            )
        {
            //
            // Set the initial capacity of the string builder to stringLength
            //
            StringBuilder stringBuilder = new StringBuilder(stringLength);

            //
            // Also set the actual length, this allows the string to be indexed
            // to that point.
            //
            stringBuilder.Length = stringLength;

            for(int i = 0; i < stringLength; i++)
            {
                stringBuilder[i] = (char)reader.ReadUInt16();
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Media Opened event comes through here
        /// </summary>
        /// <param name="o">Null argument</param>
        private object OnMediaOpened(object o)
        {
            _mediaOpenedHelper.InvokeEvents(_sender, null);

            return null;
        }

        /// <summary>
        /// MediaPrerolled event comes through here
        /// </summary>
        /// <param name="o">Null argument</param>
        private object OnMediaPrerolled(object o)
        {
            _mediaPrerolledHelper.InvokeEvents(_sender, null);

            return null;
        }
        /// <summary>
        /// MediaEnded event comes through here
        /// </summary>
        /// <param name="o">Null argument</param>
        private object OnMediaEnded(object o)
        {
            _mediaEndedHelper.InvokeEvents(_sender, null);

            return null;
        }

        /// <summary>
        /// BufferingStarted event comes through here
        /// </summary>
        /// <param name="o">Null argument</param>
        private object OnBufferingStarted(object o)
        {
            _bufferingStartedHelper.InvokeEvents(_sender, null);

            return null;
        }

        /// <summary>
        /// BufferingEnded event comes through here
        /// </summary>
        /// <param name="o">Null argument</param>
        private object OnBufferingEnded(object o)
        {
            _bufferingEndedHelper.InvokeEvents(_sender, null);

            return null;
        }

        /// <summary>
        /// MediaFailed event comes through here
        /// </summary>
        /// <param name="o">EventArgs</param>
        private object OnMediaFailed(object o)
        {
            ExceptionEventArgs e = (ExceptionEventArgs)o;
            _mediaFailedHelper.InvokeEvents(_sender, e);

            return null;
        }

        /// <summary>
        /// Script commands come through here.
        /// </summary>
        /// <param name ="o">EventArgs</param>
        private object OnScriptCommand(object o)
        {
            MediaScriptCommandEventArgs e = (MediaScriptCommandEventArgs)o;

            _scriptCommandHelper.InvokeEvents(_sender, e);

            return null;
        }

        /// <summary>
        /// New frames come through here.
        /// </summary>
        private object OnNewFrame(object e)
        {
            _newFrameHelper.InvokeEvents(_sender, null);

            return null;
        }

        /// <summary>
        /// Raised by a media when it encounters an error.
        /// </summary>
        /// <remarks>
        /// Argument passed into the callback is a System.Exception
        /// </remarks>
        private event DispatcherOperationCallback DispatcherMediaFailed;

        /// <summary>
        /// Raised by a media when its done loading.
        /// </summary>
        private event DispatcherOperationCallback DispatcherMediaOpened;

        /// <summary>
        /// Raised by a media when its done prerolling.
        /// </summary>
        private event DispatcherOperationCallback DispatcherMediaPrerolled;

        /// <summary>
        /// Raised by a media when its done playback.
        /// </summary>
        private event DispatcherOperationCallback DispatcherMediaEnded;

        /// <summary>
        /// Raised by a media when buffering begins.
        /// </summary>
        private event DispatcherOperationCallback DispatcherBufferingStarted;

        /// <summary>
        /// Raised by a media when buffering finishes.
        /// </summary>
        private event DispatcherOperationCallback DispatcherBufferingEnded;

        /// <summary>
        /// Raised by media when a particular scripting event is received.
        /// </summary>
        private event DispatcherOperationCallback DispatcherScriptCommand;

        /// <summary>
        /// Raised whenever a new frame is displayed that has been requested.
        /// This is only required for effects.
        /// </summary>
        private event DispatcherOperationCallback DispatcherMediaNewFrame;

        #endregion

        #region Private Data Members

        /// <summary>
        /// Sender of all events
        /// </summary>
        private MediaPlayer _sender;

        /// <summary>
        /// Dispatcher of this object
        /// </summary>
        private Dispatcher _dispatcher;

        /// <summary>
        /// for catching MediaOpened events
        /// </summary>
        private DispatcherOperationCallback _mediaOpened;

        /// <summary>
        /// for catching MediaFailed events
        /// </summary>
        private DispatcherOperationCallback _mediaFailed;

        /// <summary>
        /// for catching MediaPrerolled events
        /// </summary>
        private DispatcherOperationCallback _mediaPrerolled;

        /// <summary>
        /// for catching MediaEnded events
        /// </summary>
        private DispatcherOperationCallback _mediaEnded;

        /// <summary>
        /// for catching BufferingStarted events
        /// </summary>
        private DispatcherOperationCallback _bufferingStarted;

        /// <summary>
        /// for catching BufferingEnded events
        /// </summary>
        private DispatcherOperationCallback _bufferingEnded;

        /// <summary>
        /// for catching script command events.
        /// </summary>
        private DispatcherOperationCallback _scriptCommand;

        /// <summary>
        /// For catching new frame notifications.
        /// </summary>
        private DispatcherOperationCallback _newFrame;

        /// <summary>
        /// Helper for MediaFailed events
        /// </summary>
        private UniqueEventHelper<ExceptionEventArgs> _mediaFailedHelper = new UniqueEventHelper<ExceptionEventArgs>();

        /// <summary>
        /// Helper for MediaOpened events
        /// </summary>
        private UniqueEventHelper _mediaOpenedHelper = new UniqueEventHelper();

        /// <summary>
        /// Helper for MediaPrerolled events
        /// </summary>
        private UniqueEventHelper _mediaPrerolledHelper = new UniqueEventHelper();

        /// <summary>
        /// Helper for MediaEnded events
        /// </summary>
        private UniqueEventHelper _mediaEndedHelper = new UniqueEventHelper();

        /// <summary>
        /// Helper for BufferingStarted events
        /// </summary>
        private UniqueEventHelper _bufferingStartedHelper = new UniqueEventHelper();

        /// <summary>
        /// Helper for BufferingEnded events
        /// </summary>
        private UniqueEventHelper _bufferingEndedHelper = new UniqueEventHelper();

        /// <summary>
        /// Helper for the script command events
        /// </summary>
        private UniqueEventHelper<MediaScriptCommandEventArgs> _scriptCommandHelper = new UniqueEventHelper<MediaScriptCommandEventArgs>();

        /// <summary>
        /// Helper for the NewFrame events.
        /// </summary>
        private UniqueEventHelper _newFrameHelper = new UniqueEventHelper();

        #endregion
    }

    #endregion
};

