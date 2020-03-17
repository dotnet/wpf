// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __XPSEVENTHANDLERS_HPP__
#define __XPSEVENTHANDLERS_HPP__
/*++
                                                                              
    Abstract:
    
        EventHandlers used with the XpsDocumentWriter and XPSEmitter classes.
                                                         
--*/

namespace System
{
namespace Printing
{
    /// <summary>
    /// 
    /// </summary>
    public enum class WritingProgressChangeLevel
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
    public ref class WritingPrintTicketRequiredEventArgs : EventArgs
    {
    public:
        WritingPrintTicketRequiredEventArgs(
            System::Windows::Xps::Serialization::PrintTicketLevel       printTicketLevel,
            int                                                         sequence
            );
    
        property
        System::Windows::Xps::Serialization::PrintTicketLevel
        CurrentPrintTicketLevel
        {
            System::Windows::Xps::Serialization::PrintTicketLevel   get();
        }
    
        property
        int
        Sequence
        {
            int get();
        }
    
        property
        PrintTicket^
        CurrentPrintTicket
        {
            void set(PrintTicket^ printTicket);
            
            PrintTicket^ get();
        }

    private:
        System::Windows::Xps::Serialization::PrintTicketLevel       _printTicketLevel;
    
        int                                                         _sequence;
    
        PrintTicket^                                                _printTicket;
    };
    
    public ref class WritingCompletedEventArgs : ComponentModel::AsyncCompletedEventArgs
    {
    public:

        WritingCompletedEventArgs(
            bool        canceled,
            Object^     state,
            Exception^  exception);
    };
    
    public ref class WritingProgressChangedEventArgs : ComponentModel::ProgressChangedEventArgs
    {
        public:

        WritingProgressChangedEventArgs(
             WritingProgressChangeLevel   	writingLevel,
            int                             number,
            int                             progressPercentage,
            Object^                         state);
    
        property
        int 
        Number
        {
            int get();
        }


        property
        WritingProgressChangeLevel
        WritingLevel
        {
            WritingProgressChangeLevel  get();
        }

        private:

        int                             _number;

        WritingProgressChangeLevel   _writingLevel;
    };

    //
    // The following are the event args giving the caller more information 
    // about a cancel occuring event
    //
    public ref class WritingCancelledEventArgs : EventArgs
    {
        public:

        WritingCancelledEventArgs(
            Exception^       exception
            );
    
        property
        Exception^
        Error
        {
            Exception^   get();
        }
    

        private:

        Exception^      _exception;

    };



    //
    // The following are the delegates used to represent the following 3 events
    // - Getting the PrintTicket from the calling code
    // - Informing the calling code that the write operation has completed
    // - Informing the calling code of the progress in the write operation
    // - Informing the caller code that the oepration was cancelled
    //
    public
    delegate 
    void 
    WritingPrintTicketRequiredEventHandler(
         Object^                                sender, 
         WritingPrintTicketRequiredEventArgs^   e
         );
    
    public
    delegate
    void
    WritingProgressChangedEventHandler(
        Object^                             sender,
        WritingProgressChangedEventArgs^    e
        );
    
    public
    delegate
    void
    WritingCompletedEventHandler(
        Object^                     sender,
        WritingCompletedEventArgs^  e
        );
	
    public
    delegate
    void
    WritingCancelledEventHandler(
        Object^                     sender,
        WritingCancelledEventArgs^  e
        );
}
}

#endif // __XPSEVENTHANDLERS_HPP__
