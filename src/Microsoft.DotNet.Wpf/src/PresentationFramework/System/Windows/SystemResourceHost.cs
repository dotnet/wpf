// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: Object supplied as the source when the resource is fetched from the SystemResources
//
namespace System.Windows
{
    ///<summary/>
    internal sealed class SystemResourceHost
    {
        //prevent creation
        private SystemResourceHost()
        {
        }
        
        static internal SystemResourceHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SystemResourceHost();
                }
                return _instance;
            }
        }

        static private SystemResourceHost _instance;
    }
}

