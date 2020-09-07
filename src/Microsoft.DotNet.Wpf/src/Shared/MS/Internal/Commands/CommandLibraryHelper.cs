// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
// 
//
// Description: Set of helpers used by all Commands. 
//
//  
//
//
//---------------------------------------------------------------------------

using System; 
using System.Security; 
using System.Windows.Input; 


using MS.Internal.PresentationCore; // for FriendAccessAllowed

namespace MS.Internal
{
    [FriendAccessAllowed]
    internal static class CommandLibraryHelper
    {
        internal static RoutedUICommand CreateUICommand(string name, Type ownerType, byte commandId)
        {
            RoutedUICommand routedUICommand = new RoutedUICommand(name, ownerType, commandId);
            routedUICommand.AreInputGesturesDelayLoaded = true;
            return routedUICommand;
        }                        
     }
}     
