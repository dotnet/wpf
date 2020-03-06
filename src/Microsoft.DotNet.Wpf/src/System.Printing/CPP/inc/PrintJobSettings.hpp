// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTJOBSETTINGS_HPP__
#define __PRINTJOBSETTINGS_HPP__

/*++
    Abstract:
        This file includes the declarations of the PrintJobSettings
--*/
namespace System
{
namespace Printing
{
    /// <summary>
    /// This class abstracts the functionality of a print queue.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintJobSettings 
    {
        internal:

        /// <summary>
        /// Instantiates a <c>PrintJobSettings</c> object representing the current print settings
        /// on the currently printed Print Job.
        /// </summary>
        PrintJobSettings(
            PrintTicket^ userPrintTicket
            );

        public:

        /// <value>
        /// Current PrintTicket for the currently printed Print Job
        /// </value>
        property
        PrintTicket^
        CurrentPrintTicket
        {
            PrintTicket^ get();

            void set(PrintTicket^ printTicket);
        }

        /// <value>
        /// Current Description for the currently printed Print Job
        /// </value>
        property
        String^
        Description
        {
            String^ get();
            void set(String^ description);
        }


        private:

        void
        VerifyAccess(
            void
        );
      
        PrintSystemDispatcherObject^    _accessVerifier;
        
        PrintTicket^    _printTicket;
        String^         _description;
    };
}
}

#endif
