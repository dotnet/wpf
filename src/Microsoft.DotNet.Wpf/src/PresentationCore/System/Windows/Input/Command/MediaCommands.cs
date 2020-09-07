// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The MediaCommands class defines a standard set of commands that act on Media.
//
//              See spec at : http://avalon/CoreUI/Specs%20%20Eventing%20and%20Commanding/CommandLibrarySpec.mht
//
//
//

using System;
using System.Windows;
using System.Windows.Input;
using System.Collections;
using System.ComponentModel;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// MediaCommands - Set of Standard Commands
    /// </summary>
    public static class MediaCommands
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
#region Public Methods

        /// <summary>
        /// Play Command
        /// </summary>
        public static RoutedUICommand Play
        {
            get { return _EnsureCommand(CommandId.Play); }
        }

        /// <summary>
        /// Pause Command
        /// </summary>
        public static RoutedUICommand Pause
        {
            get { return _EnsureCommand(CommandId.Pause); }
        }

        /// <summary>
        /// Stop Command
        /// </summary>
        public static RoutedUICommand Stop
        {
            get { return _EnsureCommand(CommandId.Stop); }
        }

        /// <summary>
        /// Record Command
        /// </summary>
        public static RoutedUICommand Record
        {
            get { return _EnsureCommand(CommandId.Record); }
        }

        /// <summary>
        /// NextTrack Command
        /// </summary>
        public static RoutedUICommand NextTrack
        {
            get { return _EnsureCommand(CommandId.NextTrack); }
        }

        /// <summary>
        /// PreviousTrack Command
        /// </summary>
        public static RoutedUICommand PreviousTrack
        {
            get { return _EnsureCommand(CommandId.PreviousTrack); }
        }

        /// <summary>
        /// FastForward Command
        /// </summary>
        public static RoutedUICommand FastForward
        {
            get { return _EnsureCommand(CommandId.FastForward); }
        }

        /// <summary>
        /// Rewind Command
        /// </summary>
        public static RoutedUICommand Rewind
        {
            get { return _EnsureCommand(CommandId.Rewind); }
        }

        /// <summary>
        /// ChannelUp Command
        /// </summary>
        public static RoutedUICommand ChannelUp
        {
            get { return _EnsureCommand(CommandId.ChannelUp); }
        }

        /// <summary>
        /// ChannelDown Command
        /// </summary>
        public static RoutedUICommand ChannelDown
        {
            get { return _EnsureCommand(CommandId.ChannelDown); }
        }

        /// <summary>
        /// TogglePlayPause Command
        /// </summary>
        public static RoutedUICommand TogglePlayPause
        {
            get {return _EnsureCommand(CommandId.TogglePlayPause);}
        }

        /// <summary>
        /// Select Command
        /// </summary>
        public static RoutedUICommand Select
        {
            get {return _EnsureCommand(CommandId.Select);}
        }

        /// <summary>
        /// IncreaseVolume Command
        /// </summary>
        public static RoutedUICommand IncreaseVolume
        {
            get { return _EnsureCommand(CommandId.IncreaseVolume); }
        }

        /// <summary>
        /// DecreaseVolume Command
        /// </summary>
        public static RoutedUICommand DecreaseVolume
        {
            get { return _EnsureCommand(CommandId.DecreaseVolume); }
        }
        /// <summary>
        /// MuteVolume Command
        /// </summary>
        public static RoutedUICommand MuteVolume
        {
            get { return _EnsureCommand(CommandId.MuteVolume); }
        }
        /// <summary>
        /// IncreaseTreble Command
        /// </summary>
        public static RoutedUICommand IncreaseTreble
        {
            get { return _EnsureCommand(CommandId.IncreaseTreble); }
        }
        /// <summary>
        /// DecreaseTreble Command
        /// </summary>
        public static RoutedUICommand DecreaseTreble
        {
            get { return _EnsureCommand(CommandId.DecreaseTreble); }
        }
        /// <summary>
        /// IncreaseBass Command
        /// </summary>
        public static RoutedUICommand IncreaseBass
        {
            get { return _EnsureCommand(CommandId.IncreaseBass); }
        }
        /// <summary>
        /// DecreaseBass Command
        /// </summary>
        public static RoutedUICommand DecreaseBass
        {
            get { return _EnsureCommand(CommandId.DecreaseBass); }
        }
        /// <summary>
        /// BoostBass Command
        /// </summary>
        public static RoutedUICommand BoostBass
        {
            get { return _EnsureCommand(CommandId.BoostBass); }
        }
        /// <summary>
        /// IncreaseMicrophoneVolume Command
        /// </summary>
        public static RoutedUICommand IncreaseMicrophoneVolume
        {
            get { return _EnsureCommand(CommandId.IncreaseMicrophoneVolume); }
        }

        /// <summary>
        /// DecreaseMicrophoneVolume Command
        /// </summary>
        public static RoutedUICommand DecreaseMicrophoneVolume
        {
            get { return _EnsureCommand(CommandId.DecreaseMicrophoneVolume); }
        }
        /// <summary>
        /// MuteMicrophoneVolume Command
        /// </summary>
        public static RoutedUICommand MuteMicrophoneVolume
        {
            get { return _EnsureCommand(CommandId.MuteMicrophoneVolume); }
        }
        /// <summary>
        /// ToggleMicrophoneOnOff Command
        /// </summary>
        public static RoutedUICommand ToggleMicrophoneOnOff
        {
            get { return _EnsureCommand(CommandId.ToggleMicrophoneOnOff); }
        }
#endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        private static string GetPropertyName(CommandId commandId)
        {
            string propertyName = String.Empty;

            switch (commandId)
            {
                case CommandId.Play : propertyName = "Play"; break;
                case CommandId.Pause: propertyName = "Pause"; break;
                case CommandId.Stop : propertyName = "Stop"; break;
                case CommandId.Record: propertyName = "Record"; break;
                case CommandId.NextTrack: propertyName = "NextTrack"; break;
                case CommandId.PreviousTrack: propertyName = "PreviousTrack"; break;
                case CommandId.FastForward: propertyName = "FastForward"; break;
                case CommandId.Rewind: propertyName = "Rewind"; break;
                case CommandId.ChannelUp: propertyName = "ChannelUp"; break;
                case CommandId.ChannelDown: propertyName = "ChannelDown"; break;
                case CommandId.TogglePlayPause: propertyName = "TogglePlayPause"; break;
                case CommandId.IncreaseVolume: propertyName = "IncreaseVolume"; break;
                case CommandId.DecreaseVolume: propertyName = "DecreaseVolume"; break;
                case CommandId.MuteVolume: propertyName = "MuteVolume"; break;
                case CommandId.IncreaseTreble: propertyName = "IncreaseTreble"; break;
                case CommandId.DecreaseTreble: propertyName = "DecreaseTreble"; break;
                case CommandId.IncreaseBass: propertyName = "IncreaseBass"; break;
                case CommandId.DecreaseBass: propertyName = "DecreaseBass"; break;
                case CommandId.BoostBass: propertyName = "BoostBass"; break;
                case CommandId.IncreaseMicrophoneVolume: propertyName = "IncreaseMicrophoneVolume"; break;
                case CommandId.DecreaseMicrophoneVolume: propertyName = "DecreaseMicrophoneVolume"; break;
                case CommandId.MuteMicrophoneVolume: propertyName = "MuteMicrophoneVolume"; break;
                case CommandId.ToggleMicrophoneOnOff: propertyName = "ToggleMicrophoneOnOff"; break;
                case CommandId.Select:propertyName = "Select";break;
        }
            return propertyName;
        }

        internal static string GetUIText(byte commandId)
        {
            string uiText = String.Empty;

            switch ((CommandId)commandId)
            {
                case  CommandId.Play: uiText = SR.Get(SRID.MediaPlayText); break;
                case  CommandId.Pause: uiText = SR.Get(SRID.MediaPauseText); break;
                case  CommandId.Stop: uiText = SR.Get(SRID.MediaStopText); break;
                case  CommandId.Record: uiText = SR.Get(SRID.MediaRecordText); break;
                case  CommandId.NextTrack: uiText = SR.Get(SRID.MediaNextTrackText); break;
                case  CommandId.PreviousTrack: uiText = SR.Get(SRID.MediaPreviousTrackText); break;
                case  CommandId.FastForward: uiText = SR.Get(SRID.MediaFastForwardText); break;
                case  CommandId.Rewind: uiText = SR.Get(SRID.MediaRewindText); break;
                case  CommandId.ChannelUp: uiText = SR.Get(SRID.MediaChannelUpText); break;
                case  CommandId.ChannelDown: uiText = SR.Get(SRID.MediaChannelDownText); break;
                case  CommandId.TogglePlayPause: uiText = SR.Get(SRID.MediaTogglePlayPauseText); break;
                case  CommandId.IncreaseVolume: uiText = SR.Get(SRID.MediaIncreaseVolumeText); break;
                case  CommandId.DecreaseVolume: uiText = SR.Get(SRID.MediaDecreaseVolumeText); break;
                case  CommandId.MuteVolume: uiText = SR.Get(SRID.MediaMuteVolumeText); break;
                case  CommandId.IncreaseTreble: uiText = SR.Get(SRID.MediaIncreaseTrebleText); break;
                case  CommandId.DecreaseTreble: uiText = SR.Get(SRID.MediaDecreaseTrebleText); break;
                case  CommandId.IncreaseBass: uiText = SR.Get(SRID.MediaIncreaseBassText); break;
                case  CommandId.DecreaseBass: uiText = SR.Get(SRID.MediaDecreaseBassText); break;
                case  CommandId.BoostBass: uiText = SR.Get(SRID.MediaBoostBassText); break;
                case  CommandId.IncreaseMicrophoneVolume: uiText = SR.Get(SRID.MediaIncreaseMicrophoneVolumeText); break;
                case  CommandId.DecreaseMicrophoneVolume: uiText = SR.Get(SRID.MediaDecreaseMicrophoneVolumeText); break;
                case  CommandId.MuteMicrophoneVolume: uiText = SR.Get(SRID.MediaMuteMicrophoneVolumeText); break;
                case  CommandId.ToggleMicrophoneOnOff: uiText = SR.Get(SRID.MediaToggleMicrophoneOnOffText); break;
                case  CommandId.Select:uiText = SR.Get(SRID.MediaSelectText);break;
            }
            return uiText;
        }

        internal static InputGestureCollection LoadDefaultGestureFromResource(byte commandId)
        {
            InputGestureCollection gestures = new InputGestureCollection();

            //Standard Commands
            switch ((CommandId)commandId)
            {
                case  CommandId.Play:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaPlayKey),
                        SR.Get(SRID.MediaPlayKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.Pause:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaPauseKey),
                        SR.Get(SRID.MediaPauseKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.Stop:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaStopKey),
                        SR.Get(SRID.MediaStopKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.Record:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaRecordKey),
                        SR.Get(SRID.MediaRecordKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.NextTrack:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaNextTrackKey),
                        SR.Get(SRID.MediaNextTrackKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.PreviousTrack:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaPreviousTrackKey),
                        SR.Get(SRID.MediaPreviousTrackKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.FastForward:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaFastForwardKey),
                        SR.Get(SRID.MediaFastForwardKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.Rewind:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaRewindKey),
                        SR.Get(SRID.MediaRewindKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ChannelUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaChannelUpKey),
                        SR.Get(SRID.MediaChannelUpKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ChannelDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaChannelDownKey),
                        SR.Get(SRID.MediaChannelDownKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.TogglePlayPause:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaTogglePlayPauseKey),
                        SR.Get(SRID.MediaTogglePlayPauseKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.IncreaseVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaIncreaseVolumeKey),
                        SR.Get(SRID.MediaIncreaseVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.DecreaseVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaDecreaseVolumeKey),
                        SR.Get(SRID.MediaDecreaseVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MuteVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaMuteVolumeKey),
                        SR.Get(SRID.MediaMuteVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.IncreaseTreble:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaIncreaseTrebleKey),
                        SR.Get(SRID.MediaIncreaseTrebleKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.DecreaseTreble:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaDecreaseTrebleKey),
                        SR.Get(SRID.MediaDecreaseTrebleKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.IncreaseBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaIncreaseBassKey),
                        SR.Get(SRID.MediaIncreaseBassKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.DecreaseBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaDecreaseBassKey),
                        SR.Get(SRID.MediaDecreaseBassKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.BoostBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaBoostBassKey),
                        SR.Get(SRID.MediaBoostBassKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.IncreaseMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaIncreaseMicrophoneVolumeKey),
                        SR.Get(SRID.MediaIncreaseMicrophoneVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.DecreaseMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaDecreaseMicrophoneVolumeKey),
                        SR.Get(SRID.MediaDecreaseMicrophoneVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.MuteMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaMuteMicrophoneVolumeKey),
                        SR.Get(SRID.MediaMuteMicrophoneVolumeKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.ToggleMicrophoneOnOff:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaToggleMicrophoneOnOffKey),
                        SR.Get(SRID.MediaToggleMicrophoneOnOffKeyDisplayString),
                        gestures);
                    break;
                case  CommandId.Select:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.Get(SRID.MediaSelectKey),
                        SR.Get(SRID.MediaSelectKeyDisplayString),
                        gestures);
                    break;
            }
            return gestures;
        }

        private static RoutedUICommand _EnsureCommand(CommandId idCommand)
        {
            if (idCommand >= 0 && idCommand < CommandId.Last)
            {
                lock (_internalCommands.SyncRoot)
                {
                    if (_internalCommands[(int)idCommand] == null)
                    {
                        RoutedUICommand newCommand = new RoutedUICommand(GetPropertyName(idCommand), typeof(MediaCommands), (byte)idCommand);
                        newCommand.AreInputGesturesDelayLoaded = true;
                        _internalCommands[(int)idCommand] = newCommand;
                    }
                }
                return _internalCommands[(int)idCommand];
            }
            return null;
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
        // these constants will go away in future, its just to index into the right one.
        private enum CommandId : byte
        {
            // Formatting
            Play = 1,
            Pause = 2,
            Stop = 3,
            Record = 4,
            NextTrack = 5,
            PreviousTrack = 6,
            FastForward = 7,
            Rewind = 8,
            ChannelUp = 9,
            ChannelDown = 10,
            TogglePlayPause = 11,
            IncreaseVolume = 12,
            DecreaseVolume = 13,
            MuteVolume = 14,
            IncreaseTreble = 15,
            DecreaseTreble = 16,
            IncreaseBass = 17,
            DecreaseBass = 18,
            BoostBass = 19,
            IncreaseMicrophoneVolume = 20,
            DecreaseMicrophoneVolume = 21,
            MuteMicrophoneVolume = 22,
            ToggleMicrophoneOnOff = 23,
            Select = 24,

            // Last
            Last = 25
        }

        private static RoutedUICommand[] _internalCommands = new RoutedUICommand[(int)CommandId.Last];
        #endregion Private Fields
    }
}
