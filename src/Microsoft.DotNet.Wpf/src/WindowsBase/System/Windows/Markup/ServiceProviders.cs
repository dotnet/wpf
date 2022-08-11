// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Proivde a implementation for IServiceProvider and method to add services
//
//

using System;
using System.ComponentModel;
using System.Collections.Generic;
using MS.Internal.WindowsBase;

/////////////////////////////////////////////////////////////////////////////////////////

namespace System.Windows.Markup
{
    /// <summary>
    /// Proivde a implementation for IServiceProvider and method to add services
    /// </summary>
    /// <internalonly>Restrict public access until M8.2</internalonly>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ComponentModel.Browsable(false)]
    public class ServiceProviders : IServiceProvider
    {
        #region Implement IServiceProvider interface
        /// <summary>
        /// Implement IServiceProvider.GetSevice
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            if (_objDict.ContainsKey(serviceType))
            {
                return _objDict[serviceType];
            }

            return null;
        }
        #endregion

        /// <summary>
        /// Add a new service
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="service"></param>
        public void AddService(Type serviceType, Object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            if (_objDict.ContainsKey(serviceType) == false)
            {
                _objDict.Add(serviceType, service);
            }
            else if (_objDict[serviceType] != service)
            {
                throw new ArgumentException(SR.ServiceTypeAlreadyAdded, "serviceType");
            }
        }

        private Dictionary<Type,Object> _objDict = new Dictionary<Type,Object>();
    }
}
