// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __LOCALPRINTQUEUE_HPP__
#define __LOCALPRINTQUEUE_HPP__

namespace System
{
namespace Printing
{
    /// <summary>
    /// This class abstracts the functionality of a print queue 
    //  installed on the local Print Server.
    /// </summary>
    /// <ExternalAPI/>
    __gc public class LocalPrintQueue :
    public PrintQueue
    {
        public:

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Driver*                                 driver,
            Port*                                   ports[],
            PrintProcessor*                         printProcessor,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Driver*                                 driver,
            Port*                                   ports[],
            PrintProcessor*                         printProcessor,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            PrintQueueStringProperty*               requiredPrintQueueProperty,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 pritnQueueName,
            Driver*                                 driver,
            Port*                                   ports[],
            PrintProcessor*                         printProcessor,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            Path*                                   requiredSeparatorFile,             
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Driver*                                 driver,
            Port*                                   ports[],
            PrintProcessor*                         printProcessor,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            String*                                 requiredShareName,
            String*                                 requiredComment,
            String*                                 requiredLocation,
            Path*                                   requiredSeparatorFile,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Port*                                   ports[],
            DriverIdentifier*                       driverID,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Port*                                   ports[],
            DriverIdentifier*                       driverID,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            PrintQueueStringProperty*               requiredPrintQueueProperty,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 pritnQueueName,
            Port*                                   ports[],
            DriverIdentifier*                       driverID,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            Path*                                   requiredSeparatorFile,             
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName,
            Port*                                   ports[],
            DriverIdentifier*                       driverID,
            PrintQueueAttributes                    printQueueAttributes,
            Int32                                   requiredPriority,
            Int32                                   requiredDefaultPriority,
            String*                                 requiredShareName,
            String*                                 requiredComment,
            String*                                 requiredLocation,
            Path*                                   requiredSeparatorFile,
            PrintSystemObjectCreationType           creationType
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 printQueueName
            );

        LocalPrintQueue(
            String*                                 path,
            String*                                 propertyFilter[]
            );

        LocalPrintQueue(
            String*                                 path,
            PrintPropertyDictionary*                initParams,
            PrintSystemObjectCreationType           creationType
            );


        ~LocalPrintQueue(
            );

        static
        PrintSystemObject*
        Create(
            String*,
            PrintPropertyDictionary*
            );

        static
        PrintSystemObject*
        Get(
            String*
            );

        static
        PrintSystemObject*
        Get(
            String*,
            String*[]
            );
    
        static
        bool
        Delete(
            void
            );
    };
}
}

#endif
