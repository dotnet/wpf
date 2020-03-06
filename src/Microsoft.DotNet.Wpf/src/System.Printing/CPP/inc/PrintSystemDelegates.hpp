// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMDELEGATES_HPP__
#define __PRINTSYSTEMDELEGATES_HPP__

namespace System
{
namespace Printing
{
    private ref class PrintSystemDelegates abstract
    {
        public:

        delegate
        void
        Int32ValueChanged(
            Int32 newValue
            );

        delegate 
        void
        StringValueChanged(
            String^ newValue
            );

        delegate
        void
        StreamValueChanged(
            Stream^ newValue
            );

        delegate
        void
        BooleanValueChanged(
            Boolean newValue
            );

        delegate
        void
        ThreadPriorityValueChanged(
            System::Threading::ThreadPriority newValue
            );

        delegate
        void
        PrintServerEventLoggingValueChanged(
            PrintServerEventLoggingTypes       newValue
            );

        delegate
        void
        PrintQueueValueChanged(
            PrintQueue^ newValue
            );

        delegate
        void
        PrintQueueAttributePropertyChanged(
            PrintQueueAttributes newValue
            );

        delegate
        void
        PrintQueueStatusValueChanged(
            PrintQueueStatus newValue
            );

        delegate
        void
        DriverValueChanged(
            PrintDriver^ newValue
            );

        delegate
        void
        PortValueChanged(
            PrintPort^ newValue
            );

        delegate
        void
        PrintProcessorValueChanged(
            PrintProcessor^ newValue
            );

        delegate
        void
        PrintServerValueChanged(
            PrintServer^    newValue
            );

        delegate
        void
        PrintTicketValueChanged(
            PrintTicket^    newValue
            );

        delegate
        void
        ByteArrayValueChanged(
            array<Byte>^    newValue
            );

        delegate
        void
        JobPriorityValueChanged(
            PrintJobPriority  newValue
            );

        delegate
        void
        JobTypeValueChanged(
            PrintJobType    newValue
            );

        delegate
        void
        JobStatusValueChanged(
            PrintJobStatus  newValue
            );

        delegate
        void
        SystemDateTimeValueChanged(
            System::DateTime  newValue
            );

        delegate
        void
        ObjectRegistered(
            void
            );

        delegate
        void
        SystemTypeValueChanged(
            System::Type^   newValue
            );
    };
}
}

#endif
