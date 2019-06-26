// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 

using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32; // VK translation.

namespace System.Windows.Input
{
    /// <summary>
    /// The CommandDevice class represents the mouse device to the
    /// members of a context.
    /// </summary>
    internal sealed class CommandDevice : InputDevice
    {
        internal CommandDevice( InputManager inputManager )
        {
            _inputManager = new SecurityCriticalData<InputManager>(inputManager);
            _inputManager.Value.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
            _inputManager.Value.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
        }

        /// <summary>
        /// Returns the element that input from this device is sent to.
        /// </summary>
        public override IInputElement Target
        {
            get
            {
                VerifyAccess();
                return Keyboard.FocusedElement;
            }
        }

        /// <summary>
        /// Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        public override PresentationSource ActiveSource
        {
            get
            {  
                
                return null;
            }
        }

        private void PreProcessInput( object sender, PreProcessInputEventArgs e )
        {
            InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;
            if (input != null)
            {
                if (input.Report.Type == InputType.Command)
                {
                    RawAppCommandInputReport rawAppCommandInputReport = input.Report as RawAppCommandInputReport;
                    if (rawAppCommandInputReport != null)
                    {
                        // Claim the input for the Command.
                        input.Device = this;

                        // Set the proper source
                        input.Source = GetSourceFromDevice(rawAppCommandInputReport.Device);
                    }
                }
            }
        }

        // Used by CommandDevice to send AppCommands to the tree.
        internal static readonly RoutedEvent CommandDeviceEvent =
            EventManager.RegisterRoutedEvent("CommandDevice",
                                             RoutingStrategy.Bubble,
                                             typeof(CommandDeviceEventHandler),
                                             typeof(CommandDevice));

        private void PostProcessInput( object sender, ProcessInputEventArgs e )
        {
            if (e.StagingItem.Input.RoutedEvent == InputManager.InputReportEvent)
            {
                if (!e.StagingItem.Input.Handled)
                {
                    InputReportEventArgs inputReportEventArgs = e.StagingItem.Input as InputReportEventArgs;
                    if (inputReportEventArgs != null)
                    {
                        RawAppCommandInputReport rawAppCommandInputReport = inputReportEventArgs.Report as RawAppCommandInputReport;
                        if (rawAppCommandInputReport != null)
                        {
                            IInputElement commandTarget = e.StagingItem.Input.OriginalSource as IInputElement;
                            if (commandTarget != null)
                            {
                                RoutedCommand command = GetRoutedCommand(rawAppCommandInputReport.AppCommand);
                                if (command != null)
                                {
                                    // Send the app command to the tree to be handled by UIElements and ContentElements
                                    // that will forward the event to CommandManager.
                                    CommandDeviceEventArgs args = new CommandDeviceEventArgs(this, rawAppCommandInputReport.Timestamp, command);
                                    args.RoutedEvent = CommandDeviceEvent;
                                    args.Source = commandTarget;
                                    e.PushInput(args, e.StagingItem);
                                }
                            }
                        }
                    }
                }
            }
            else if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyUpEvent ||
                     e.StagingItem.Input.RoutedEvent == Mouse.MouseUpEvent ||
                     e.StagingItem.Input.RoutedEvent == Keyboard.GotKeyboardFocusEvent ||
                     e.StagingItem.Input.RoutedEvent == Keyboard.LostKeyboardFocusEvent)
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Maps the Windows WM_APPCOMMANDS to regular Commands.
        private RoutedCommand GetRoutedCommand( int appCommandId )
        {
            RoutedCommand appCommand = null;
            switch (appCommandId)
            {                            
                case NativeMethods.APPCOMMAND_BROWSER_BACKWARD:
                    appCommand = NavigationCommands.BrowseBack;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_FORWARD:
                    appCommand = NavigationCommands.BrowseForward;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_REFRESH:
                    appCommand = NavigationCommands.Refresh;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_STOP:
                    appCommand = NavigationCommands.BrowseStop;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_SEARCH:
                    appCommand = NavigationCommands.Search;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_FAVORITES:
                    appCommand = NavigationCommands.Favorites;
                    break;
                case NativeMethods.APPCOMMAND_BROWSER_HOME:
                    appCommand = NavigationCommands.BrowseHome;
                    break;                     
                case NativeMethods.APPCOMMAND_VOLUME_MUTE:
                    appCommand = MediaCommands.MuteVolume;
                    break;
                case NativeMethods.APPCOMMAND_VOLUME_DOWN:
                    appCommand = MediaCommands.DecreaseVolume;
                    break;
                case NativeMethods.APPCOMMAND_VOLUME_UP:
                    appCommand = MediaCommands.IncreaseVolume;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_NEXTTRACK:
                    appCommand = MediaCommands.NextTrack;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_PREVIOUSTRACK:
                    appCommand = MediaCommands.PreviousTrack;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_STOP:
                    appCommand = MediaCommands.Stop;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_PLAY_PAUSE:
                    appCommand = MediaCommands.TogglePlayPause;
                    break;
                case NativeMethods.APPCOMMAND_LAUNCH_APP1:
                case NativeMethods.APPCOMMAND_LAUNCH_APP2:
                case NativeMethods.APPCOMMAND_LAUNCH_MAIL:
                    break;
                case NativeMethods.APPCOMMAND_LAUNCH_MEDIA_SELECT:
                    appCommand = MediaCommands.Select;
                    break;
                case NativeMethods.APPCOMMAND_BASS_DOWN:
                    appCommand = MediaCommands.DecreaseBass;
                    break;
                case NativeMethods.APPCOMMAND_BASS_BOOST:
                    appCommand = MediaCommands.BoostBass;
                    break;
                case NativeMethods.APPCOMMAND_BASS_UP:
                    appCommand = MediaCommands.IncreaseBass;
                    break;
                case NativeMethods.APPCOMMAND_TREBLE_DOWN:
                    appCommand = MediaCommands.DecreaseTreble;
                    break;
                case NativeMethods.APPCOMMAND_TREBLE_UP:
                    appCommand = MediaCommands.IncreaseTreble;
                    break;
                case NativeMethods.APPCOMMAND_MICROPHONE_VOLUME_MUTE:
                    appCommand = MediaCommands.MuteMicrophoneVolume;
                    break;
                case NativeMethods.APPCOMMAND_MICROPHONE_VOLUME_DOWN:
                    appCommand = MediaCommands.DecreaseMicrophoneVolume;
                    break;
                case NativeMethods.APPCOMMAND_MICROPHONE_VOLUME_UP:
                    appCommand = MediaCommands.IncreaseMicrophoneVolume;
                    break;
                case NativeMethods.APPCOMMAND_HELP:
                    appCommand = ApplicationCommands.Help;
                    break;
                case NativeMethods.APPCOMMAND_FIND:
                    appCommand = ApplicationCommands.Find;
                    break;
                case NativeMethods.APPCOMMAND_NEW:
                    appCommand = ApplicationCommands.New;
                    break;
                case NativeMethods.APPCOMMAND_OPEN:
                    appCommand = ApplicationCommands.Open;
                    break;
                case NativeMethods.APPCOMMAND_CLOSE:
                    appCommand = ApplicationCommands.Close;
                    break;
                case NativeMethods.APPCOMMAND_SAVE:
                    appCommand = ApplicationCommands.Save;
                    break;
                case NativeMethods.APPCOMMAND_PRINT:
                    appCommand = ApplicationCommands.Print;
                    break;
                case NativeMethods.APPCOMMAND_UNDO:
                    appCommand = ApplicationCommands.Undo;
                    break;
                case NativeMethods.APPCOMMAND_REDO:
                    appCommand = ApplicationCommands.Redo;
                    break;
                case NativeMethods.APPCOMMAND_COPY:
                    appCommand = ApplicationCommands.Copy;
                    break;
                case NativeMethods.APPCOMMAND_CUT:
                    appCommand = ApplicationCommands.Cut;
                    break;
                case NativeMethods.APPCOMMAND_PASTE:
                    appCommand = ApplicationCommands.Paste;
                    break;
                case NativeMethods.APPCOMMAND_REPLY_TO_MAIL:
                case NativeMethods.APPCOMMAND_FORWARD_MAIL:
                case NativeMethods.APPCOMMAND_SEND_MAIL:
                case NativeMethods.APPCOMMAND_SPELL_CHECK:
                case NativeMethods.APPCOMMAND_DICTATE_OR_COMMAND_CONTROL_TOGGLE:
                    break;
                case NativeMethods.APPCOMMAND_MIC_ON_OFF_TOGGLE:
                    appCommand = MediaCommands.ToggleMicrophoneOnOff;
                    break;
                case NativeMethods.APPCOMMAND_CORRECTION_LIST:
                    appCommand = ApplicationCommands.CorrectionList;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_PLAY:
                    appCommand = MediaCommands.Play;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_PAUSE:
                    appCommand = MediaCommands.Pause;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_RECORD:
                    appCommand = MediaCommands.Record;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_FAST_FORWARD:
                    appCommand = MediaCommands.FastForward;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_REWIND:
                    appCommand = MediaCommands.Rewind;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_CHANNEL_UP:
                    appCommand = MediaCommands.ChannelUp;
                    break;
                case NativeMethods.APPCOMMAND_MEDIA_CHANNEL_DOWN:
                    appCommand = MediaCommands.ChannelDown;
                    break;
            } 
            return appCommand;
        }

        /// <summary>
        /// Takes an InputType enum representing the device and returns the element that
        /// should be the source of the command.
        /// </summary>
        /// <param name="device"></param>
        /// <returns>Returns either Mouse.DirectlyOver or Keyboard.FocusedElement</returns>
        private IInputElement GetSourceFromDevice(InputType device)
        {
            if (device == InputType.Mouse)
            {
                return Mouse.DirectlyOver;
            }
            else
            {
                // All other devices route to the keyboard.
                return Keyboard.FocusedElement;
            }
        }

        private SecurityCriticalData<InputManager> _inputManager;
    }

    /// <summary>
    ///     Used by CommandDevice to send AppCommands to the tree.
    /// </summary>
    internal class CommandDeviceEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of this class.
        /// </summary>
        /// <param name="commandDevice">The logical CommandDevice associated with this event.</param>
        /// <param name="timestamp">The time when the input occured.</param>
        /// <param name="command">Command associated with this event.</param>
        internal CommandDeviceEventArgs(CommandDevice commandDevice, int timestamp, ICommand command)
            : base(commandDevice, timestamp)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            _command = command;
        }

        /// <summary>
        ///     The command that will be executed.
        /// </summary>
        internal ICommand Command
        {
            get { return _command; }
        }

        /// <summary>
        ///     Invokes the handler.
        /// </summary>
        /// <param name="execHandler">delegate</param>
        /// <param name="target">element</param>
        protected override void InvokeEventHandler(Delegate execHandler, object target)
        {
            CommandDeviceEventHandler handler = (CommandDeviceEventHandler)execHandler;
            handler(target, this);
        }

        private ICommand _command;
    }

    /// <summary>
    ///     Used by CommandDevice to send AppCommands to the tree.
    /// </summary>
    internal delegate void CommandDeviceEventHandler(object sender, CommandDeviceEventArgs e);
}
