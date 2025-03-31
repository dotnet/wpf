// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
                                                                              
                                                                         
    Abstract:
        The file contains the definition of a class defining a context
        object which define the basic properties defining an object.
                                                                         
--*/
namespace System.Windows.Xps.Serialization
{
    internal abstract class BasicContext
    {
        #region Constructor

        /// <summary>
        /// Instantiates a BasicContext
        /// </summary>
        public 
        BasicContext(
            string name, 
            string prefix)
        {
            this._name   = name;
            this._prefix = prefix;
        }

        /// <summary>
        /// Instantiates a BasicContext with 
        /// null Data. Data can be initialized
        /// later.
        /// </summary>
        public 
        BasicContext()
        {
            Initialize();
        }

        #endregion Constructor
        
        #region Public Properties
        
        /// <summary>
        /// Query/Set the Name of the context
        /// </summary>
        public
        string 
        Name
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
        
        /// <summary>
        /// Query/Set the prefix of the context
        /// </summary>
        public 
        string 
        Prefix
        {
            get 
            { 
                return _prefix; 
            }

            set
            {
                _prefix = value;
            }
        }   

        #endregion Public Properties


        #region Public Methods

        /// <summary>
        /// Initialize the internal Properties
        /// </summary>
        public
        void 
        Initialize()
        {
            _name   = null;
            _prefix = null;
        }

        /// <summary>
        /// Clears the internal properties
        /// </summary>
        public
        virtual 
        void 
        Clear()
        {
            _name   = null;
            _prefix = null;
        }

        #endregion Public Methods
        
        #region Private Data
        
        private string _name;
        private string _prefix;        

        #endregion Private Data
    };
}
