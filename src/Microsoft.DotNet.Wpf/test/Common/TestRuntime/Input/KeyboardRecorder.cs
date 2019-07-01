// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading; using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Collections;
using System.Windows.Documents;
using System.IO;

using System.Windows.Markup;
using System.Reflection;
using System.Windows.Automation;
using Microsoft.Test.Win32;
using System.Xml;
using Microsoft.Test.Modeling;


namespace Microsoft.Test.Input
{

    ///<summary>
    ///
    /// This class record all the keyboard and some mouse messages from a define point
    /// on the Avalon Tree.  This is still experimental
    ///
    ///</summary>        
    public class KeyboardRecorder
    {
        ///<summary>
        ///
        /// Constructor, it takes the start element from the tree that we will
        /// start listening.
        /// 
        ///
        ///</summary>
        public KeyboardRecorder(FrameworkElement elemListener)
        {  
            container = elemListener;
            _eventsListened = new ArrayList();
        }

        ///<summary>
        ///
        /// Save the all the records to an XmlElement structure.
        ///
        ///</summary>
        public XmlElement SaveToXml(XmlDocument xmlDoc)
        {


			XmlElement root = xmlDoc.CreateElement("RecordActions");

            for (int i =0; i < this._eventsListened.Count; i++)
            {               
                Record action = (Record)this._eventsListened[i];
                root.AppendChild(action.SaveToXml(xmlDoc));
                
            }

            return root;
        }

        ///<summary>
        ///
        /// Add the necessary listeners for the Keyboard and Mouse events.
        /// Today we listen for: MouseDownEventID, KeyUpEventID, KeyDown, GotKeyboardFocus and LostKeyboardFocus
        ///
        ///</summary>
        public void  StartRecording()
        {       
            PresentationSource source = PresentationSource.FromVisual(container);


            _wndproc = new HwndSourceHook(wndProc);
        
            if (source != null)
            {
                if (source is HwndSource)
                {
                    ((HwndSource)source).AddHook(_wndproc);
                }
                else
                {
                    throw new InvalidOperationException("HwndSource expected");
                }
            }

            container.AddHandler(System.Windows.Input.Mouse.MouseDownEvent, new MouseButtonEventHandler(mouseDownHandler), true);
            container.AddHandler(System.Windows.Input.Keyboard.KeyUpEvent, new KeyEventHandler(keyUpHandler), true);
            container.AddHandler(System.Windows.Input.Keyboard.KeyDownEvent, new KeyEventHandler(keyDownHandler), true);
            container.AddHandler(System.Windows.Input.Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(gotFocusHandler), true);
            container.AddHandler(System.Windows.Input.Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(lostFocusHandler), true);
            _stopListening = false;
        }

        ///<summary>
        ///
        /// Stop adding records and Remove all the listeners
        /// Today we listen for: MouseDownEventID, KeyUpEventID, KeyDown, GotKeyboardFocus and LostKeyboardFocus
        ///
        ///</summary>
        public void StopRecording()
        {
            _stopListening = true;
            PresentationSource source = PresentationSource.FromVisual(container);


            _wndproc = new HwndSourceHook(wndProc);
        
            if (source != null)
            {
                if (source is HwndSource)
                {
                    ((HwndSource)source).RemoveHook(_wndproc);
                }
                else
                {
                    throw new InvalidOperationException("HwndSource expected");
                }
            }

            container.AddHandler(System.Windows.Input.Mouse.MouseDownEvent, new MouseButtonEventHandler(mouseDownHandler), true);
            container.RemoveHandler(System.Windows.Input.Keyboard.KeyUpEvent, new KeyEventHandler(keyUpHandler));
            container.RemoveHandler(System.Windows.Input.Keyboard.KeyDownEvent, new KeyEventHandler(keyDownHandler));
            container.RemoveHandler(System.Windows.Input.Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(gotFocusHandler));
            container.RemoveHandler(System.Windows.Input.Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(lostFocusHandler));
        }

        IntPtr wndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            return IntPtr.Zero;
        }

        void gotFocusHandler (object o, KeyboardFocusChangedEventArgs args)
        {
            KeyboardRecord kRA = null;

            kRA = new KeyboardRecord();
            kRA.Name = "GotKeyboardFocus";

            string oldFocus = "", newFocus = "";
            if (args.OldFocus != null && args.OldFocus is FrameworkElement)
            {
                oldFocus = ((FrameworkElement)args.OldFocus).Name;
            }
            else if (args.OldFocus == null)
            {
                oldFocus = "null";
            }
            else 
            {
                throw new InvalidOperationException("GotKeyboardFocus OldFocus the value type is not expected");
            }
            if (args.NewFocus != null && args.NewFocus is FrameworkElement)
            {
                newFocus = ((FrameworkElement)args.NewFocus).Name;
            }
            else if (args.NewFocus == null) 
            { 
                newFocus = "null";
            }
            else 
            {
                throw new InvalidOperationException("GotKeyboardFocus NewFocus the value type is not expected");
            }

            kRA.Focus = new FocusInfo(oldFocus,newFocus);
            AddRecord(kRA);
        }

        void lostFocusHandler (object o, KeyboardFocusChangedEventArgs args)
        {
            KeyboardRecord kRA = null;
            kRA = new KeyboardRecord();
            kRA.Name = "LostKeyboardFocus";

            string oldFocus = "", newFocus = "";
            if (args.OldFocus != null && args.OldFocus is FrameworkElement)
            {
                oldFocus = ((FrameworkElement)args.OldFocus).Name;
            }
            else if (args.OldFocus == null)
            {
                oldFocus = "null";
            }
            else 
            {
                throw new InvalidOperationException("LostKeyboardFocus OldFocus the value type is not expected");
            }
            if (args.NewFocus != null && args.NewFocus is FrameworkElement)
            {
                newFocus = ((FrameworkElement)args.NewFocus).Name;
            }
            else if (args.NewFocus == null) 
            { 
                newFocus = "null";
            }
            else 
            {
                throw new InvalidOperationException("LostKeyboardFocus NewFocus the value type is not expected");
            }

            kRA.Focus = new FocusInfo(oldFocus,newFocus);
            AddRecord(kRA);
        }

        void keyDownHandler(object sender, KeyEventArgs args)
        {
            KeyboardRecord kRA = null;

            kRA = new KeyboardRecord();
            kRA.Name = "KeyDown";

            AddRecord(kRA);

            stateKey(args,kRA);
        }

        void keyUpHandler(object sender, KeyEventArgs args)
        {
            KeyboardRecord kRA = null;
            kRA = new KeyboardRecord();
            kRA.Name = "KeyUp";

            AddRecord(kRA);

            stateKey(args,kRA);
        }


        void stateKey(KeyEventArgs args, KeyboardRecord kRA)
        {

            kRA.Key = args.Key.ToString();

            // Store for any character

            if (args.Key == System.Windows.Input.Key.ImeProcessed)
            {
                kRA.ImeProcessedKey = args.ImeProcessedKey.ToString();
            }
            

            // Store State for Shift, Alt and Control Key


            KeyStates states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.RightShift);
            if (states == KeyStates.Down)
                kRA.Shift = true;

            states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.LeftShift);
            if (states == KeyStates.Down)
                kRA.Shift = true;


            states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.LeftAlt);
            if (states == KeyStates.Down)
                kRA.Alt= true;

            states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.RightAlt);
            if (states == KeyStates.Down)
                kRA.Alt = true;

            states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.RightCtrl);
            if (states == KeyStates.Down)
                kRA.Control= true;

            states = System.Windows.Input.Keyboard.GetKeyStates(System.Windows.Input.Key.LeftCtrl);
            if (states == KeyStates.Down)
                kRA.Control = true;
        }

        void mouseDownHandler(object sender, MouseButtonEventArgs args)
        {
            KeyboardRecord kRA = null;
            kRA = new KeyboardRecord();
            kRA.Name = "MouseDown";
            IFrameworkInputElement frameworkElement = (IFrameworkInputElement)args.Source;
            kRA.SourceID = frameworkElement.Name;
            AddRecord(kRA);
        }

        void AddRecord(Record record)
        {
            if (!_stopListening)
                _eventsListened.Add(record);            
        }

        bool _stopListening = false;
        ArrayList _eventsListened;
        FrameworkElement container;
        HwndSourceHook _wndproc;
    }


    ///<summary>
    ///
    /// Base class for all kind of Records
    ///
    ///</summary>
    public abstract class Record
    {
        ///<summary>
        ///
        /// defualt Constructor
        ///
        ///</summary>
        public Record()
        {

        }

        ///<summary>
        ///
        /// This hold the Action Name for this record
        ///
        ///</summary>
        public string Name 
        {
            get 
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        ///<summary>
        ///
        /// 
        ///
        ///</summary>
        internal abstract XmlElement SaveToXml(XmlDocument xmlDoc);
        string _name;
    }



    ///<summary>
    ///
    /// This is the record type for all the data recorded by the KeyboardRecord
    ///
    ///</summary>
    
    public class KeyboardRecord : Record
    {
        ///<summary>
        ///</summary>
        public KeyboardRecord(){}

        ///<summary>
        /// Key that is on the KeyEventArgs
        ///</summary>
        public string Key 
        {
            get 
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        ///<summary>
        /// Source of the Event
        ///</summary>
        public string SourceID
        {

            get 
            {
                return _sourceID;
            }
            set
            {
                _sourceID= value;
            }
            
        }


        ///<summary>
        /// True if the ALT key is pressed
        ///</summary>
        public bool Alt
        {
            get
            {
                return _alt;
            }
            set
            {
                _alt = value;
            }
        }

        ///<summary>
        /// True if the CONTROL key is pressed
        ///</summary>
        public bool Control
        {
            get
            {
                return _control;
            }
            set
            {
                 _control = value;
            }
        }

        ///<summary>
        /// True if the Shift key is pressed
        ///</summary>
        public bool Shift
        {
            get
            {
                return _shift;
            }
            set
            {
                _shift = value;
            }
        }

        ///<summary>
        /// Value for the focus
        ///</summary>
        public FocusInfo Focus 
        {
            get 
            {
                return _focus;
            }
            set
            {
                _focus = value;
            }
        }

        ///<summary>
        /// If the Key value is TextInput, this property will hold the real value
        ///</summary>
        public string TextInputKey
        {
            get 
            {
                return _textInputKey;
            }
            set 
            {
                _textInputKey = value;
            }
        }
        

        ///<summary>
        /// If the Key value is ImeProcessed, this property will hold the real value
        ///</summary>
        public string ImeProcessedKey
        {
            get {return _imeProcessedKey;}
            set {_imeProcessedKey = value;}
        }

        ///<summary>
        /// Serialize the value fo the KeboardRecord
        ///</summary>
        internal override XmlElement SaveToXml(XmlDocument xmlDoc)
        {
           
           
            XmlElement actionXml = xmlDoc.CreateElement("KeyboardRecordAction");


			if (Focus != null)
			{

					if (_focus.OldFocus != "")
						actionXml.SetAttribute("OldFocus", _focus.OldFocus);

					if (_focus.NewFocus != "")
						actionXml.SetAttribute("NewFocus", _focus.NewFocus);
				
			}


            if (this.Name != "")
        		actionXml.SetAttribute("Name",this.Name);

            
            if (this._key != null )
            {
        		actionXml.SetAttribute("Key",this._key.ToString());

                if (_key == "TextInput")
                {
               	    actionXml.SetAttribute("TextInputKey",this.TextInputKey);
                }else if (_key == "ImeProcessedKey")
                {
               	    actionXml.SetAttribute("ImeProcessedKey",this.ImeProcessedKey);
                }
                
                

                if (_shift)
            		actionXml.SetAttribute("ShiftDown","true");
                else
            		actionXml.SetAttribute("ShiftDown","false");

                if (_control)
            		actionXml.SetAttribute("ControlDown","true");
                else
            		actionXml.SetAttribute("ControlDown","false");

                if (_alt)
            		actionXml.SetAttribute("AltDown","true");
                else
            		actionXml.SetAttribute("AltDown","false");
            }

            if (this.SourceID != String.Empty || this.SourceID != "")
            {
            		actionXml.SetAttribute("Source",_sourceID);
            }

			return actionXml;
            
        }

        FocusInfo _focus = null;
        string _key;
        bool _shift = false;
        bool _alt = false;
        bool _control =false;
        string _sourceID = string.Empty;
        string _textInputKey = String.Empty;
        string _imeProcessedKey = String.Empty;

    }

    ///<summary>
    /// This class hold the key pair value for Focus, how is new and old
    ///</summary>
            
    public class FocusInfo
    {
        ///<summary>
        /// This class hold the key pair value for Focus, how is new and old
        ///</summary>
        public FocusInfo(string old, string newFocus)
        {
            _oldFocus = old;
            _newFocus = newFocus;
        }

        ///<summary>
        /// Old Focus value
        ///</summary>        
        public string OldFocus
        {
            get
            {
                return _oldFocus;
            }
        }

        ///<summary>
        /// New Focus value
        ///</summary>      
        public string NewFocus
        {
            get
            {
                return _newFocus;
            }
        }
        
        string _oldFocus  , _newFocus ;
    }


}

