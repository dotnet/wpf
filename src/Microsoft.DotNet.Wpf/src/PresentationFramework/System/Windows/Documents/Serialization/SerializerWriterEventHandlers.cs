// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                                                                                               
    Module Name:  

        XPSEventHandlers.hpp
                                                                              
    Abstract:
    
        EventHandlers used with the XpsDocumentWriter and XPSEmitter classes.                                                                                
--*/
#if !DONOTREFPRINTINGASMMETA
using System.Printing;
#endif
using System.Security;

namespace System.Windows.Documents.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    public enum  WritingProgressChangeLevel
    {
        /// <summary>
        ///
        /// </summary>
        None                                 = 0,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentSequenceWritingProgress = 1,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentWritingProgress         = 2,
        /// <summary>
        ///
        /// </summary>
        FixedPageWritingProgress             = 3
    };

    //
    // The following are the event args giving the caller more information 
    // about the previously describes events
    //
    
    /// <summary>
    /// 
    /// </summary>
    public class WritingPrintTicketRequiredEventArgs : EventArgs
    {
#if !DONOTREFPRINTINGASMMETA
        /// <summary>
        /// 
        /// </summary>
        public WritingPrintTicketRequiredEventArgs(
            System.Windows.Xps.Serialization.PrintTicketLevel       printTicketLevel,
            int                                                     sequence
            )
        {
            _printTicketLevel = printTicketLevel;
            _sequence = sequence;
        }
    

        /// <summary>
        /// 
        /// </summary>
       public
        System.Windows.Xps.Serialization.PrintTicketLevel
        CurrentPrintTicketLevel
        {
            get
            {
                return _printTicketLevel;
            }
        }
    
        /// <summary>
        /// 
        /// </summary>
        public          
        int
        Sequence
        {
            get
            {
                return _sequence;
            }
        }
    
        /// <summary>
        /// 
        /// </summary>
        public          
        PrintTicket
        CurrentPrintTicket
        {
            set
            {
                _printTicket = value;
            }

            get
            {
                return _printTicket;
            }
        }



        private System.Windows.Xps.Serialization.PrintTicketLevel _printTicketLevel;
        private int                                                         _sequence;

        private PrintTicket _printTicket;
#endif
    };
    
    /// <summary>
    /// 
    /// </summary>
    public  class WritingCompletedEventArgs : ComponentModel.AsyncCompletedEventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public
        WritingCompletedEventArgs(
            bool        cancelled,
            Object      state,
            Exception   exception): base(exception, cancelled, state)
        {
        }
    };
    
    /// <summary>
    /// 
    /// </summary>
    public class WritingProgressChangedEventArgs : ComponentModel.ProgressChangedEventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public
        WritingProgressChangedEventArgs(
            WritingProgressChangeLevel   	writingLevel,
            int                             number,
            int                             progressPercentage,
            Object                          state): base(progressPercentage, state)
        {
            _number       = number;
            _writingLevel = writingLevel;
        }

        /// <summary>
        /// 
        /// </summary>
        public
        int 
        Number
        {
            get
            {
                return _number;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public
        WritingProgressChangeLevel
        WritingLevel
        {
            get
            {
                return _writingLevel;
            }
        }

        private int                             _number;

        private WritingProgressChangeLevel      _writingLevel;
    };

    /// <summary>
    /// The following are the event args giving the caller more information 
    /// about a cancel occuring event
    /// </summary>
    public class WritingCancelledEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public
        WritingCancelledEventArgs(
            Exception       exception
            )
        {
            _exception = exception;
        }
    
        /// <summary>
        /// 
        /// </summary>
        public
        Exception
        Error
        {
            get
            {
                return _exception;
            }
        }


        private Exception      _exception;
    };



    //
    // The following are the delegates used to represent the following 3 events
    // - Getting the PrintTicket from the calling code
    // - Informing the calling code that the write operation has completed
    // - Informing the calling code of the progress in the write operation
    // - Informing the caller code that the oepration was cancelled
    //
    /// <summary>
    /// 
    /// </summary>
    public
    delegate 
    void 
    WritingPrintTicketRequiredEventHandler(
         Object                                 sender, 
         WritingPrintTicketRequiredEventArgs    e
         );
    
    /// <summary>
    /// 
    /// </summary>
    public
    delegate
    void
    WritingProgressChangedEventHandler(
        Object                              sender,
        WritingProgressChangedEventArgs     e
        );
    
    /// <summary>
    /// 
    /// </summary>
    public
    delegate
    void
    WritingCompletedEventHandler(
        Object                     sender,
        WritingCompletedEventArgs   e
        );
	
    /// <summary>
    /// 
    /// </summary>
    public
    delegate
    void
    WritingCancelledEventHandler(
        Object                     sender,
        WritingCancelledEventArgs   e
        );
}
