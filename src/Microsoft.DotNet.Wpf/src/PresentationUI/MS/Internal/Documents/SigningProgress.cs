// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    ProgressDialog invokes a progress notification dialog that runs on
//    its own thread, with its own message pump.  It is used to notify 
//    users when a long, blocking operation on XPSViewer's main thread is
//    running (and blocking).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using System.Windows.TrustUI;
using MS.Internal.Documents.Application;

namespace MS.Internal.Documents
{    
    internal sealed partial class ProgressDialog : DialogBaseForm
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Private Constructor
        /// </summary>
        private ProgressDialog(string title, string message)
        {
            _label.Text = message;
            Text = title;  
        }       

        #endregion Constructors   

        #region Internal Methods

        /// <summary>
        /// Creates an instance of the ProgressDialog in a new thread, returning a
        /// ProgressDialogReference wrapper which will eventually contain a valid
        /// reference to the dialog (when the dialog is actually instantiated.)
        /// </summary>
        /// <param name="title">The title to appear in the Titlebar of the dialog</param>
        /// <param name="message">The message to appear in the dialog</param>
        /// <returns></returns>
        public static ProgressDialogReference CreateThreaded(string title, string message)
        {
            ProgressDialogReference reference = 
                new ProgressDialogReference(title, message);

            // Spin up a new thread
            Thread dialogThread = new Thread(
                new ParameterizedThreadStart(ProgressDialogThreadProc));

            // Start it, passing in our reference
            dialogThread.Start(reference);

            // Return the reference -- the Form inside will eventually point to
            // the dialog created in ProgressDialogThreadProc.
            return reference;            
        }

        /// <summary>
        /// Closes an instance of a ProgressDialog on its thread.
        /// </summary>
        /// <param name="dialog"></param> 
        public static void CloseThreaded(ProgressDialog dialog)
        {           
            dialog.Invoke(new MethodInvoker(dialog.CloseThreadedImpl));           
        }

        #endregion Internal Methods        

        #region Private Methods

        /// <summary>
        /// Creates a new ProgressDialog on its own thread, with its own
        /// message pump.
        /// </summary>
        /// <param name="state"></param>
        private static void ProgressDialogThreadProc(object state)
        {
            ProgressDialogReference reference = state as ProgressDialogReference;
            Invariant.Assert(reference != null);            

            reference.Form = new ProgressDialog(reference.Title, reference.Message);         
            
            //Spawn a new message pump for this dialog
            System.Windows.Forms.Application.Run();
        }

        /// <summary>
        /// Invokes the ProgressDialog (calls ShowDialog on it) when the Timer is elapsed.
        /// We do this so that the dialog only appears after the blocking operation
        /// we're showing status for takes a certain amount of time.  (Prevents UI flicker
        /// in cases where the action doesn't take very long)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param> 
        private void OnTimerTick(object sender, EventArgs e)
        {
            // Disable the timer so this will only occur once
            _timer.Enabled = false;                       
            ShowDialog(null);            
        }

        /// <summary>
        /// Closes the dialog on its own thread.
        /// </summary>
        private void CloseThreadedImpl()
        {
            System.Windows.Forms.Application.ExitThread();          
        }
        
        #endregion Private Methods                         

        /// <summary>
        /// ProgressDialogReference exists to allow passing a reference to a ProgressDialog
        /// created on a new thread back to the main UI thread, and to ensure that the main
        /// thread blocks until the ProgressDialog is ready before using it.
        /// 
        /// This is a mitigation for an unfortunate design limitation because we have to block
        /// on a UI thread and show a dialog on a non-blocking thread, instead of the other
        /// way around.
        /// </summary>
        internal class ProgressDialogReference
        {

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="title"></param>
            /// <param name="message"></param>
            public ProgressDialogReference(string title, string message)
            {
                _title = title;
                _message = message;
                _isReady = new ManualResetEvent(false);
            }

            /// <summary>
            /// The title of the dialog being shown
            /// </summary>
            public String Title
            {
                get { return _title; }
            }

            /// <summary>
            /// The message in the dialog being shown
            /// </summary>
            public String Message
            {
                get { return _message; }
            }

            public ProgressDialog Form
            {
                get
                {
                    // Wait until the form has been set and instantiated...
                    _isReady.WaitOne();
                    return _form;
                }

                set
                {
                    _form = value;
                    // Our form is running on a separate thread, setup an event handler
                    // so that we're notified when it is loaded.
                    _form.Load += new EventHandler(FormLoaded);
                }
            }

            /// <summary>
            /// Handler for the form.Loaded event used to set the ready state.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void FormLoaded(object sender, EventArgs e)
            {
                // Since the form has successfully loaded then update the ready state.
                _isReady.Set();
            }

            private ManualResetEvent _isReady;
            private ProgressDialog _form;

            private string _title;
            private string _message;
        }


        //The amount to wait (in msec) before actually invoking the dialog.
        private const int _timerInterval = 500;
    }
}
