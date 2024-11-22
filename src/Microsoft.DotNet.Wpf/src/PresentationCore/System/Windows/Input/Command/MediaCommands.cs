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
                case  CommandId.Play: uiText = SR.MediaPlayText; break;
                case  CommandId.Pause: uiText = SR.MediaPauseText; break;
                case  CommandId.Stop: uiText = SR.MediaStopText; break;
                case  CommandId.Record: uiText = SR.MediaRecordText; break;
                case  CommandId.NextTrack: uiText = SR.MediaNextTrackText; break;
                case  CommandId.PreviousTrack: uiText = SR.MediaPreviousTrackText; break;
                case  CommandId.FastForward: uiText = SR.MediaFastForwardText; break;
                case  CommandId.Rewind: uiText = SR.MediaRewindText; break;
                case  CommandId.ChannelUp: uiText = SR.MediaChannelUpText; break;
                case  CommandId.ChannelDown: uiText = SR.MediaChannelDownText; break;
                case  CommandId.TogglePlayPause: uiText = SR.MediaTogglePlayPauseText; break;
                case  CommandId.IncreaseVolume: uiText = SR.MediaIncreaseVolumeText; break;
                case  CommandId.DecreaseVolume: uiText = SR.MediaDecreaseVolumeText; break;
                case  CommandId.MuteVolume: uiText = SR.MediaMuteVolumeText; break;
                case  CommandId.IncreaseTreble: uiText = SR.MediaIncreaseTrebleText; break;
                case  CommandId.DecreaseTreble: uiText = SR.MediaDecreaseTrebleText; break;
                case  CommandId.IncreaseBass: uiText = SR.MediaIncreaseBassText; break;
                case  CommandId.DecreaseBass: uiText = SR.MediaDecreaseBassText; break;
                case  CommandId.BoostBass: uiText = SR.MediaBoostBassText; break;
                case  CommandId.IncreaseMicrophoneVolume: uiText = SR.MediaIncreaseMicrophoneVolumeText; break;
                case  CommandId.DecreaseMicrophoneVolume: uiText = SR.MediaDecreaseMicrophoneVolumeText; break;
                case  CommandId.MuteMicrophoneVolume: uiText = SR.MediaMuteMicrophoneVolumeText; break;
                case  CommandId.ToggleMicrophoneOnOff: uiText = SR.MediaToggleMicrophoneOnOffText; break;
                case  CommandId.Select:uiText = SR.MediaSelectText;break;
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
                        SR.MediaPlayKey,
                        SR.MediaPlayKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Pause:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaPauseKey,
                        SR.MediaPauseKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Stop:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaStopKey,
                        SR.MediaStopKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Record:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaRecordKey,
                        SR.MediaRecordKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.NextTrack:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaNextTrackKey,
                        SR.MediaNextTrackKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.PreviousTrack:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaPreviousTrackKey,
                        SR.MediaPreviousTrackKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.FastForward:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaFastForwardKey,
                        SR.MediaFastForwardKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Rewind:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaRewindKey,
                        SR.MediaRewindKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.ChannelUp:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaChannelUpKey,
                        SR.MediaChannelUpKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.ChannelDown:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaChannelDownKey,
                        SR.MediaChannelDownKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.TogglePlayPause:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaTogglePlayPauseKey,
                        SR.MediaTogglePlayPauseKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.IncreaseVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaIncreaseVolumeKey,
                        SR.MediaIncreaseVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.DecreaseVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaDecreaseVolumeKey,
                        SR.MediaDecreaseVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.MuteVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaMuteVolumeKey,
                        SR.MediaMuteVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.IncreaseTreble:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaIncreaseTrebleKey,
                        SR.MediaIncreaseTrebleKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.DecreaseTreble:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaDecreaseTrebleKey,
                        SR.MediaDecreaseTrebleKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.IncreaseBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaIncreaseBassKey,
                        SR.MediaIncreaseBassKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.DecreaseBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaDecreaseBassKey,
                        SR.MediaDecreaseBassKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.BoostBass:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaBoostBassKey,
                        SR.MediaBoostBassKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.IncreaseMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaIncreaseMicrophoneVolumeKey,
                        SR.MediaIncreaseMicrophoneVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.DecreaseMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaDecreaseMicrophoneVolumeKey,
                        SR.MediaDecreaseMicrophoneVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.MuteMicrophoneVolume:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaMuteMicrophoneVolumeKey,
                        SR.MediaMuteMicrophoneVolumeKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.ToggleMicrophoneOnOff:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaToggleMicrophoneOnOffKey,
                        SR.MediaToggleMicrophoneOnOffKeyDisplayString,
                        gestures);
                    break;
                case  CommandId.Select:
                    KeyGesture.AddGesturesFromResourceStrings(
                        SR.MediaSelectKey,
                        SR.MediaSelectKeyDisplayString,
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
