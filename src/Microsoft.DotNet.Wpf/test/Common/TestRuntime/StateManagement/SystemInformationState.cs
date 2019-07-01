// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using System.Globalization;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Allows you to query system information of the current machine
    /// </summary>
	public class SystemInformationState : State<SystemInformationStateValue,object>
	{
        #region Constructor

        /// <summary/>
        public SystemInformationState()
            : base()
        {
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool CanTransition
        {
            get { return false; }
        }

        /// <summary/>
        public override SystemInformationStateValue GetValue()
        {
            return new SystemInformationStateValue();
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            SystemInformationState other = obj as SystemInformationState;
            if (other == null)
                return false;

            //No properties to check
            return true;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }

    /// <summary>
    /// Encapsulates system information that cannot be changed through state
    /// transitions
    /// </summary>
    public class SystemInformationStateValue
    {
        #region Private Data        

        private string machineName = Environment.MachineName;
        private Product os = SystemInformation.Current.Product;
        private ProductSuite productSuite = SystemInformation.Current.ProductSuite;
        private OSProductType osProductType = SystemInformation.Current.OSProductType;        
        private CultureInfo osCulture = SystemInformation.Current.OSCulture;
        private CultureInfo uiCulture = SystemInformation.Current.UICulture;
        private string osVersion = SystemInformation.Current.OSVersion;
        private string ieVersion = SystemInformation.Current.IEVersion;        
        private ProcessorArchitecture processorArchitecture = SystemInformation.Current.ProcessorArchitecture;

        #endregion

        #region Constructor

        /// <summary/>
        public SystemInformationStateValue()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// OS Product Information
        /// </summary>
        [XmlAttribute()]
        public Product OS
        {
            get { return os;  }
            set { os = value; }
        }

        /// <summary>
        /// Product Suite
        /// </summary>
        [XmlAttribute()]
        public ProductSuite ProductSuite
        {
            get { return productSuite; }
            set { productSuite = value; }
        }

        /// <summary>
        /// OS Product Type
        /// </summary>
        [XmlAttribute()]
        public OSProductType ProductType
        {
            get { return osProductType; }
            set { osProductType = value; }
        }
        
        /// <summary>
        /// OS Culture
        /// </summary>
        [XmlAttribute()]
        public CultureInfo OSCulture
        {
            get { return osCulture; }
            set { osCulture = value; }
        }

        /// <summary>
        /// UI Culture
        /// </summary>
        [XmlAttribute()]
        public CultureInfo UICulture
        {
            get { return uiCulture; }
            set { uiCulture = value; }
        }

        /// <summary>
        /// OS Version
        /// </summary>
        [XmlAttribute()]
        public string OSVersion
        {
            get { return osVersion; }
            set { osVersion = value; }
        }

        /// <summary>
        /// IE Version
        /// </summary>
        [XmlAttribute()]
        public string IEVersion
        {
            get { return ieVersion; }
            set { ieVersion = value; }
        }

        /// <summary>
        /// Processor Architecture Information
        /// </summary>
        [XmlAttribute()]
        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return processorArchitecture; }
            set { processorArchitecture = value; }
        }

        /// <summary>
        /// Name of the machine
        /// </summary>
        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            SystemInformationStateValue other = obj as SystemInformationStateValue;
            if (other == null)
                return false;

            //HACK: For some reason, a serialized/deserialized culture doesn't return true
            //Ensure neither of the objects are null before checking only the LCID
            if ((osCulture == null && other.osCulture != null) || (osCulture != null && other.osCulture == null))
                return false;
            if ((uiCulture == null && other.uiCulture != null) || (uiCulture != null && other.uiCulture == null))
                return false;

            return other.os == os &&
                    other.productSuite == productSuite &&
                    other.osProductType == osProductType &&
                    other.osCulture.LCID == osCulture.LCID &&
                    other.uiCulture.LCID == uiCulture.LCID &&
                    other.osVersion == osVersion &&
                    other.ieVersion == ieVersion &&
                    other.processorArchitecture == processorArchitecture;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int ieHashCode = String.IsNullOrEmpty(ieVersion) ? 0 : ieVersion.GetHashCode();

            return os.GetHashCode() ^
                    productSuite.GetHashCode() ^
                    osProductType.GetHashCode() ^
                    osCulture.GetHashCode() ^
                    uiCulture.GetHashCode() ^
                    osVersion.GetHashCode() ^
                    ieHashCode.GetHashCode() ^
                    processorArchitecture.GetHashCode();
        }

        /// <summary/>
        public static bool operator ==(SystemInformationStateValue x, SystemInformationStateValue y)
        {
            if (object.Equals(x, null))
                return object.Equals(y, null);
            else
                return x.Equals(y);
        }

        /// <summary/>
        public static bool operator !=(SystemInformationStateValue x, SystemInformationStateValue y)
        {
            if (object.Equals(x, null))
                return !object.Equals(y, null);
            else
                return !x.Equals(y);
        }

        #endregion
    }
}
