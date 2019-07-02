// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Object managing the various OSinfo on the system
    /// </summary>
    public class MasterMetadata
    {
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension BuildNumberDimension = new BuildNumberDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension CpuArchitectureDimension = new CpuArchitectureDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension CurrentUICultureDimension = new CurrentUICultureDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension DpiDimension = new DpiDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension DwmDimension = new DwmDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension HostTypeDimension = new HostTypeDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension IeVersionDimension = new IeVersionDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension OsCultureDimension = new OsCultureDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension OsVersionDimension = new OsVersionDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension ThemeDimension = new ThemeDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension VideoCardDimension = new VideoCardDimension();
        /// <summary>
        /// 
        /// </summary>
        public static IMasterDimension CurrentWPFUICultureDimension = new CurrentWPFUICultureDimension();

        private static Dictionary<string, IMasterDimension> _dimensionTable;
        private  Dictionary<IMasterDimension, string> _dimensions;

        internal List<IMasterDimension> _criteria = new List<IMasterDimension>();

        static MasterMetadata()
        {
            _dimensionTable = new Dictionary<string, IMasterDimension>();

            System.Reflection.FieldInfo[] fieldInfos = typeof(MasterMetadata).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            for (int t = 0; t < fieldInfos.Length; t++)
            {
                if (fieldInfos[t].FieldType.IsAssignableFrom(typeof(IMasterDimension)))
                {
                    IMasterDimension metadata = (IMasterDimension)fieldInfos[t].GetValue(null);
                    _dimensionTable.Add(fieldInfos[t].Name.ToLowerInvariant(), metadata);
                }
            }            
        }

        /// <summary>
        /// Create a new instance of the object
        /// </summary>
        public MasterMetadata()
        {
            _dimensions = new Dictionary<IMasterDimension, string>();
            foreach(KeyValuePair<string, IMasterDimension> keyValue in _dimensionTable)
            {
                _dimensions.Add(keyValue.Value, keyValue.Value.GetCurrentValue());
            }
        }

        internal MasterMetadata(List<IMasterDimension> criteria) : this()
        {
            _criteria = criteria;
        }


        internal static IMasterDimension GetDimension(string dimensionName)
        {
            return _dimensionTable[dimensionName.ToLowerInvariant()];
        }

        /// <summary>
        /// Return all Dimensions (and associated value) describing  this master.
        /// </summary>
        public Dictionary<IMasterDimension, string> Description
        { 
            get { return _dimensions; } 
        }

        /// <summary>
        /// Return all Criteria (and associated value) for this master.
        /// </summary>
        public Dictionary<IMasterDimension, string> Criteria
        {
            get 
            { 
                Dictionary<IMasterDimension, string> retVal  = new Dictionary<IMasterDimension, string> ();
                for(int t=0;t<_criteria.Count; t++)
                {
                    retVal.Add(_criteria[t], _dimensions[_criteria[t]]);
                }
                return retVal; 
            }
        }
    }
}
