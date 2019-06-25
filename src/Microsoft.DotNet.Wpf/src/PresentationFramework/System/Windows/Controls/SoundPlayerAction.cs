// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/***************************************************************************\
*
*
* A SoundPlayerAction causes a sound to be played in response to a trigger.
*
*
\***************************************************************************/
using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Utility;
using System;
using System.Collections;
using System.ComponentModel;            // DefaultValueAttribute
using System.IO;
using System.IO.Packaging;
using System.Media;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace System.Windows.Controls
{
    /// <summary>
    ///   A class that describes a sound playback action to perform for a trigger
    /// </summary>
    public class SoundPlayerAction : TriggerAction, IDisposable
    {
       /// <summary>
       ///     Creates an instance of the SoundPlayerAction object.
       /// </summary>
       public SoundPlayerAction()
           : base()
       {
       }

       /// <summary>
       /// Dispose.
       /// </summary>
       public void Dispose()
       {
           if (m_player != null)
           {
               m_player.Dispose();
           }
       }


       /// <summary>
       /// DependencyProperty for Source
       /// </summary>
       public static readonly DependencyProperty SourceProperty =
               DependencyProperty.Register(
                       "Source",
                       typeof(Uri),
                       typeof(SoundPlayerAction),
                       new FrameworkPropertyMetadata(
                           new PropertyChangedCallback(OnSourceChanged)));

       /// <summary>
       ///   A class that describes a sound playback action to perform for a trigger
       /// </summary>
       public Uri Source
       {
           get { return (Uri)GetValue(SourceProperty); }
           set { SetValue(SourceProperty, value); }
       }

       private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
       {
           SoundPlayerAction soundPlayerAction = (SoundPlayerAction)d;

           soundPlayerAction.OnSourceChangedHelper((Uri)e.NewValue);
       }

       // To avoid blocking the UI thread, SoundPlayerAction performs an asynchronous
       // download of the WebResponse and later, the response Stream.  However,
       // PackWebRequest does not support the asynchonous Begin/EndGetResponse pattern.
       // To work around this, we use our own asynchronous worker pool thread,
       // and then use the regular synchronous WebRequest.GetResponse() method.
       // Once we obtain the WebResponse, we will use SoundPlayer's async API
       // to download the content of the stream.
       private void OnSourceChangedHelper(Uri newValue)
       {
           if (newValue == null || newValue.IsAbsoluteUri)
           {
               m_lastRequestedAbsoluteUri = newValue;
           }
           else
           {
               // When we are given a relative Uri path, expand to an absolute Uri by resolving
               // it against the Application's base Uri.  This would typically return a Pack Uri.
               m_lastRequestedAbsoluteUri =
                   BaseUriHelper.GetResolvedUri(BaseUriHelper.BaseUri, newValue);
           }

           // Invalidate items that depend on the Source uri
           m_player = null;
           m_playRequested = false;  // Suppress earlier requests to Play the sound
           
           if (m_streamLoadInProgress)
           {
               // There is already a worker thread downloading the previous URI stream,
               // or the SoundPlayer is copying the stream into its buffer.  Make a note
               // that the URI has been changed and we will have to reload everything.
               m_uriChangedWhileLoadingStream = true;
           }
           else
           {
               BeginLoadStream();
           }
       }

       /// <summary>
       /// Invoke the SoundPlayer action.
       /// </summary>
       internal sealed override void Invoke(FrameworkElement el,
                                            FrameworkContentElement ctntEl,
                                            Style targetStyle,
                                            FrameworkTemplate targetTemplate,
                                            Int64 layer)
       {
           PlayWhenLoaded();
       }

       /// <summary>
       /// invoke the SoundPlayer action.
       /// </summary>
       internal sealed override void Invoke(FrameworkElement el)
       {
           PlayWhenLoaded();
       }

       /// <summary>
       /// Plays the 
       /// </summary>
       private void PlayWhenLoaded()
       {
           if (m_streamLoadInProgress)
           {
               m_playRequested = true;
           }
           else if (m_player != null)
           {
               // If the Player has not yet loaded, m_streamLoadInProgress must be true
               Debug.Assert(m_player.IsLoadCompleted);

               m_player.Play();
           }
       }


       private void BeginLoadStream()
       {
           if (m_lastRequestedAbsoluteUri != null)  // Only reload if the new source is non-null
           {
               m_streamLoadInProgress = true;

                // Step 1: Perform an asynchronous load of the WebResponse and its associated Stream
                Task.Run(() =>
                {
                    Stream result = WpfWebRequestHelper.CreateRequestAndGetResponseStream(m_lastRequestedAbsoluteUri);
                    LoadStreamCallback(result);
                });
            }
       }

       private delegate Stream LoadStreamCaller(Uri uri);

       /// <summary>
       /// This is the actual code that runs in our worker thread.
       /// </summary>
       private Stream LoadStreamAsync(Uri uri)
       {           
           return WpfWebRequestHelper.CreateRequestAndGetResponseStream(uri);
       }

       /// <summary>
       /// This code runs in the worker thread when LoadStreamAsync() finishes.
       /// </summary>
       /// <param name="asyncResult"></param>
       private void LoadStreamCallback(Stream asyncResult)
       {
           // Now have the UI thread regain control and initialize the SoundPlayer
           DispatcherOperationCallback loadStreamCompletedCaller = new DispatcherOperationCallback(OnLoadStreamCompleted);

           Dispatcher.BeginInvoke(DispatcherPriority.Normal, loadStreamCompletedCaller, asyncResult);
       }

       /// <summary>
       /// Called on the UI thread when the Stream object is initialized.
       /// Begins asynchronous download of the Stream content into SoundPlayer's local buffer.
       /// </summary>
       private Object OnLoadStreamCompleted(Object asyncResultArg)
       {
            Stream newStream = (Stream)asyncResultArg;

           if (m_uriChangedWhileLoadingStream)  // The source URI was changed, redo Stream loading
           {
               m_uriChangedWhileLoadingStream = false;
               if (newStream != null)  // Don't hold on to the new stream - it's not needed anymore
               {
                   newStream.Dispose();
               }
               BeginLoadStream();
           }
           else if (newStream != null)  // We loaded the Stream, begin buffering it
           {
               if (m_player == null)
               {
                   m_player = new SoundPlayer((Stream)newStream);
               }
               else
               {
                   m_player.Stream = (Stream)newStream;
               }
               m_player.LoadCompleted += new AsyncCompletedEventHandler(OnSoundPlayerLoadCompleted);
               m_player.LoadAsync();  // Begin preloading the stream into SoundPlayer's local buffer
           }
           return null;
       }

        private void OnSoundPlayerLoadCompleted(Object sender, AsyncCompletedEventArgs e)
        {
            if (Object.ReferenceEquals(m_player, sender))
            {
                Debug.Assert(m_player.IsLoadCompleted);

                if (m_uriChangedWhileLoadingStream)  // The source URI was changed, redo Stream loading again
                {
                    m_player = null;
                    m_uriChangedWhileLoadingStream = false;
                    BeginLoadStream();
                }
                else
                {
                    m_streamLoadInProgress = false;

                    if (m_playRequested)  // URI is correct, m_player is ready, play it if requested
                    {
                        m_playRequested = false;
                        m_player.Play();
                    }
                }
            }
        }


        private SoundPlayer m_player;
        private Uri m_lastRequestedAbsoluteUri;

        private bool m_streamLoadInProgress;
        private bool m_playRequested;
        private bool m_uriChangedWhileLoadingStream;
    }
}
