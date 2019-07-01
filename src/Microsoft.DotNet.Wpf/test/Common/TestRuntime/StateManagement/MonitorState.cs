// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.Display;
using System.Xml.Serialization;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Reports and allows you to modify the current monitor settings.
    /// The settings include Width, Height, BitsPerPixel and Frequency
    /// </summary>
    public class MonitorState : State<MonitorStateValue, object>
    {
        #region Private Data

        private MonitorIdentifier monitorIdentifier;

        #endregion

        #region Constructor

        /// <summary/>
        public MonitorState()
            : this(MonitorIdentifier.Primary)
        {
        }

        /// <summary/>
        public MonitorState(MonitorIdentifier monitorIdentifier)
            : base()
        {
            this.monitorIdentifier = monitorIdentifier;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Monitor Identifier
        /// </summary>
        [XmlAttribute()]
        public MonitorIdentifier MonitorId
        {
            get { return monitorIdentifier; }
            set { monitorIdentifier = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override MonitorStateValue GetValue()
        {
            int monitorId = (int)monitorIdentifier;
            Monitor[] monitors = Monitor.GetAllEnabled();
            
            if (monitorId >= monitors.Length)
                return new MonitorStateValue();

            return new MonitorStateValue(monitors[monitorId].DisplaySettings.Current);
        }

        /// <summary/>
        public override bool SetValue(MonitorStateValue value, object action)
        {
            int monitorId = (int)monitorIdentifier;
            Monitor[] monitors = Monitor.GetAllEnabled();

            if (monitorId >= monitors.Length)
                return false;

            //If we don't check this, it will throw
            if (value.Width <= 0 || value.Height <= 0 || value.BitsPerPixel <= 0 || value.Frequency <= 0)
                return false;

            //Query to ensure the settings are supported
            DisplaySettingsInfo displaySettingsInfo = monitors[monitorId].DisplaySettings.Query(value.Width, value.Height, value.BitsPerPixel, value.Frequency);
            if (displaySettingsInfo == null)
                return false;

            //Need to impersonate user
            monitors[monitorId].DisplaySettings.Current = displaySettingsInfo;
            return true;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            MonitorState other = obj as MonitorState;
            if (other == null)
                return false;
            return (other.monitorIdentifier == monitorIdentifier);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }

    /// <summary>
    /// Monitor Display Settings
    /// </summary>
    public class MonitorStateValue
    {
        private int width;
        private int height;
        private int bitsPerPixel;
        private int frequency;    

        #region Constructor

        /// <summary/>
        public MonitorStateValue()
        {
        }       

        /// <summary/>
        public MonitorStateValue(int width, int height, int bitsPerPixel, int frequency)
        {
            this.width = width;
            this.height = height;
            this.bitsPerPixel = bitsPerPixel;
            this.frequency = frequency;
        }

        internal MonitorStateValue(DisplaySettingsInfo displaySettingsInfo)
            : this(displaySettingsInfo.Width, displaySettingsInfo.Height, displaySettingsInfo.BitsPerPixel, displaySettingsInfo.Frequency)
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Width
        /// </summary>
        [XmlAttribute()]
        public int Width
        {
            get { return width; }
            set 
            {
                if (value < 0)
                    throw new ArgumentException("Width cannot be negative.");
                width = value; 
            }
        }

        /// <summary>
        /// Height
        /// </summary>
        [XmlAttribute()]
        public int Height
        {
            get { return height; }
            set 
            {
                if (value < 0)
                    throw new ArgumentException("Height cannot be negative.");
                height = value; 
            }
        }

        /// <summary>
        /// Bits per pixel for the resolution (16,32)
        /// </summary>
        [XmlAttribute()]
        public int BitsPerPixel
        {
            get { return bitsPerPixel; }
            set 
            {
                if (value < 0)
                    throw new ArgumentException("BitsPerPixel cannot be negative.");
                bitsPerPixel = value; 
            }
        }

        /// <summary>
        /// Frequency in Hz
        /// </summary>
        [XmlAttribute()]
        public int Frequency
        {
            get { return frequency; }
            set 
            {
                if (value < 0)
                    throw new ArgumentException("Frequency cannot be negative.");
                frequency = value; 
            }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            MonitorStateValue other = obj as MonitorStateValue;
            if (other == null)
                return false;

            return (other.width == width && other.height == height &&
                other.bitsPerPixel == bitsPerPixel && other.frequency == frequency);
        }

        /// <summary/>
        public override int  GetHashCode()
        {
            return width ^ height ^ bitsPerPixel ^ height;
        }

        /// <summary/>
        public static bool operator ==(MonitorStateValue x, MonitorStateValue y)
        {
            if (object.Equals(x, null))
                return object.Equals(y, null);
            else
                return x.Equals(y);
        }

        /// <summary/>
        public static bool operator !=(MonitorStateValue x, MonitorStateValue y)
        {
            if (object.Equals(x, null))
                return !object.Equals(y, null);
            else
                return !x.Equals(y);
        }

        #endregion
    }

    /// <summary>
    /// Monitor Identification
    /// </summary>
    public enum MonitorIdentifier
    {
        /// <summary>
        /// Primary Monitor
        /// </summary>
        Primary = 0,
        /// <summary>
        /// Secondary Monitor
        /// </summary>
        Secondary = 1,
        /// <summary>
        /// Third Monitor
        /// </summary>
        Third = 2,
        /// <summary>
        /// Fourth Monitor
        /// </summary>
        Fourth = 3
    }
}
