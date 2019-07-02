// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// Self describing Filter parameter. Allows all kind of parameters
    /// Useful type wrapper pattern for automatic discovery of services
    /// </summary>
    public class FilterParameter
    {
        #region Properties
            #region Private Properties
                private object _parameter = null;
                private string _description = string.Empty;
                private string _name = string.Empty;
                private bool _inUse = false;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Name of the parameter
                /// </summary>
                public string Name
                {
                    get
                    {
                        return _name;
                    }
                }

                /// <summary>
                /// Parameter - the type has to match with the constructor, the value passed can't be null
                /// </summary>
                public object Parameter
                {
                    get
                    {
                        return _parameter;
                    }
                    set
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("A valid instance of the parameter is expected (null passed in)");
                        }
                        if (value.GetType() != _parameter.GetType())
                        {
                            throw new ArgumentException("Parameter is not of expected type (Expected : '" + _parameter.GetType().ToString() +"' -- passed in : '" + value.GetType().ToString() + "'  )");
                        }

                        _parameter = value;
                    }
                }

                /// <summary>
                /// Description of the parameter usage
                /// </summary>
                public string Description
                {
                    get { return _description; }
                }

                /// <summary>
                /// Set/Get the usage state of the parameter
                /// </summary>
                public bool InUse
                {
                    get
                    {
                        return _inUse;
                    }
                    set
                    {
                        _inUse = value;
                    }
                }
        
            #endregion Public Properties
        #endregion Properties

        #region Constructor
            /// <summary>
            /// Description of the parameter usage
            /// </summary>
            /// <param name="name">The parameter Name</param>
            /// <param name="description">The description of this parameter</param>
            /// <param name="parameterValue">The param value</param>
            public FilterParameter(string name, string description, object parameterValue)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }
                if (description == null)
                {
                    throw new ArgumentNullException("description");
                }
                if (parameterValue == null)
                {
                    throw new ArgumentNullException("value");
                }
                _name = name;
                _description = description;
                _parameter = parameterValue;
            }
                    
        #endregion Constructor
     }
 }
