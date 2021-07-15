// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This interface defines methods that are common between XpsSerializationManager and 
    /// XpsOMSerializationManager, but not common to the base class PackageSerializationManger
    /// </summary>
    internal interface IXpsSerializationManager
    {
        void OnXPSSerializationPrintTicketRequired(object operationState);

        void OnXPSSerializationProgressChanged(object operationState);

        void RegisterPageStart();

        void RegisterPageEnd();

        PrintTicket FixedPagePrintTicket
        {
            set;
            get;
        }

        Size FixedPageSize
        {
            set;
            get;
        }

        VisualSerializationService VisualSerializationService
        {
            get;
        }
    }
}
