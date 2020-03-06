// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMEXCEPTION_HPP__
#define __PRINTSYSTEMEXCEPTION_HPP__

/*++                                                         
    Abstract:
        
        Print System exception objects declaration.                                                           
--*/

#pragma once

namespace System
{
namespace Printing
{
    __value private enum PrinterHResult
    {
        PrintSystemGenericError             = ERROR_INVALID_PRINTER_NAME,
        PrintSystemInvalidPrinterNameError  = ERROR_INVALID_PRINTER_NAME,
        PrintSystemInsufficientBufferError  = ERROR_INSUFFICIENT_BUFFER,
    };

    /// <summary>
    /// Print System exception object.
    /// </summary>
    /// <ExternalAPI/>    
    [System::Serializable]
    public __gc class PrintSystemException : 
    public SystemException
    {
        public:

        /// <summary>
        /// PrintSystemException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: Print System exception.
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        PrintSystemException(
            void
            );

        /// <summary>
        /// PrintSystemException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        PrintSystemException(
            String*             message
            );
        
        /// <summary>
        /// PrintSystemException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>        
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param> 
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        PrintSystemException(
            String*             message,
            Exception*          innerException
            );

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>                 
        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );
        
        public private:

        PrintSystemException(
            int         errorCode,
            String*     message       
            );

        PrintSystemException(
            int         errorCode,
            String*     message,
            Exception*  innerException
            );

        protected: 

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary> 
        /// <param name="info"> The object that holds the serialized object data. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param> 
        PrintSystemException(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        /// <summary>
        /// Loads the resource string for a given resource key.
        /// </summary>
        /// <param name="resourceKey"> Resource key. </param> 
        static
        String*
        GetMessageFromResource(
            String*     resourceKey
            );

        /// <summary>
        /// Loads the resource string for a Win32 error and 
        /// formats an error message given a resource key.
        /// </summary>
        /// <param name="errorCode"> Win32 error code. </param> 
        /// <param name="resourceKey"> Resource key. </param> 
        static
        String*
        GetMessageFromResource(
            int         errorCode,
            String*     resourceKey
            );

        private:

        static
        PrintSystemException(
            void
            )
        {
            printResourceManager = new System::Resources::ResourceManager("System.Printing", (__typeof(PrintSystemException))->Assembly);
        }

        static
        System::Resources::ResourceManager*     printResourceManager;
    };

    /// <summary>
    /// PrintQueueException exception object.
    /// Exceptions of type PrintQueueException are thrown when operating on a 
    /// <c>PrintQueue</c> object.
    /// </summary>
    /// <ExternalAPI/>    
    [System::Serializable]
    public __gc class PrintQueueException : 
    public PrintSystemException
    {
        public:

        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: An exception occurred while creating the PrintQueue object. Win32 error: {0}
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>        
        PrintQueueException(
            void
            );

        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        PrintQueueException(
            String*             message
            );
        
        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param> 
        PrintQueueException(
            String*             message,
            Exception*          innerException
            );

        /// <value>
        /// Printer name property. The name represents the name identifier of 
        /// the PrintQueue object that was running the code when the exception was thrown.
        /// </value>        
        __property 
        String* 
        get_PrinterName(
            void
            );

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>                 
        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        public private:

        PrintQueueException(
            int         errorCode,
            String*     message,
            String*     printerName
            );

        PrintQueueException(
            int                 errorCode,
            String*             message,
            String*             printerName,
            Exception*          innerException
            );

        protected:

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary>     
        /// <param name="info"> The object that holds the serialized object data. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>         
        PrintQueueException(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        private:

        String*     printerName;

    };

    /// <summary>
    /// PrintServerException constructor.
    /// Exceptions of type PrintServerException are thrown when operating on a 
    /// <c>PrintServer</c> object. 
    /// </summary>
    /// <remarks>
    /// Default error code: ERROR_INVALID_PRINTER_NAME.
    /// </remarks>
    [System::Serializable]
    public __gc class PrintServerException : 
    public PrintSystemException
    {
        public:

        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: An exception occurred while creating the PrintServer object. Win32 error is: {0}
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>        
        PrintServerException(
            void
            );

        /// <summary>
        /// PrintServerException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        PrintServerException(
            String*             message 
            );

        /// <summary>
        /// PrintServerException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param> 
        PrintServerException(
            String*             message,
            Exception*          innerException
            );

        /// <value>
        /// Server name property. The name represents the name identifier of 
        /// the PrintServer object that was running the code when the exception was thrown.
        /// </value>        
        __property 
        String* 
        get_ServerName(
            void
            );

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>                 
        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        public private:

        PrintServerException(
            int         errorCode,
            String*     message,
            String*     serverName
            );

        PrintServerException(
            int                 errorCode,
            String*             message,
            String*             serverName,
            Exception*          innerException
            );

        protected: 

        /// <summary>
        /// Initializes a new instance of the PrintServerException class with serialized data.
        /// </summary>            
        /// <param name="info"> The object that holds the serialized object data. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>         
        PrintServerException(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        private:

        String*     serverName;

    };

    /// <summary>
    /// PrintCommitAttributesException constructor.
    /// Exceptions of type PrintCommitAttributesException are thrown 
    /// when Commit method of any of the Print System objects that implement the method. 
    /// </summary>
    /// <remarks>
    /// Default error code: ERROR_INVALID_PRINTER_NAME.
    /// </remarks>
    [System::Serializable]
    public __gc class PrintCommitAttributesException : 
    public PrintSystemException
    {
        public:

        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: Print System exception.
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        PrintCommitAttributesException(
            void
            );

        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        PrintCommitAttributesException(
            String*                 message
            );
        
        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        PrintCommitAttributesException(
            String*                 message,
            Exception*              innerException
            );

        /// <value>
        /// List of strings representing the names of the properties that succeeded to commit.
        /// </value>
        __property
        IList*
        get_SucceedToCommitAttributes(
            void
            );

        /// <value>
        /// List of strings representing the names of the properties that failed to commit.
        /// </value>        
        __property
        IList*
        get_FailToCommitAttributes(
            void
            );

        /// <value>
        /// PrintSystemObject name property. The name represents the name identifier of 
        /// the PrintSystemObject object that was running the code when the exception was thrown.
        /// </value>        
        __property
        String*
        get_PrintObjectName(
            void
            );

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>         
        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*      info,
            System::Runtime::Serialization::StreamingContext        context
            );

        public private:

        PrintCommitAttributesException(
            int                                     errorCode,
            System::Collections::ArrayList*         attributesSuccessList,
            System::Collections::ArrayList*         attributesFailList
            );

        PrintCommitAttributesException(
            int                                     errorCode,
            String*                                 message,
            System::Collections::ArrayList*         attributesSuccessList,
            System::Collections::ArrayList*         attributesFailList,
            String*                                 objectName
            );

        __property
        ArrayList*
        get_SucceedToCommitAttributesArrayList(
            void
            );

        __property
        ArrayList*
        get_FailToCommitAttributesArrayList(
            void
            );

        protected:

        /// <summary>
        /// Initializes a new instance of the PrintCommitAttributesException class with serialized data.
        /// </summary>            
        /// <param name="info"> The object that holds the serialized object data. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>         
        PrintCommitAttributesException(
            System::Runtime::Serialization::SerializationInfo*      info,
            System::Runtime::Serialization::StreamingContext        context
            );
        
        private:

        ArrayList*  succeedToCommitAttributes;
        ArrayList*  failToCommitAttributes;      
        String*     printObjectName;
    };


    private __gc class InternalPrintSystemException
    {
        public private:

        InternalPrintSystemException(
            int   lastWin32Error
            );

        __property
        int
        get_HResult(
            void
            );

        static
        void
        ThrowIfErrorIsNot(
            int     lastWin32Error,
            int     expectedLastWin32Error
            );

        static
        void
        ThrowIfLastErrorIsNot(
            int     expectedLastWin32Error
            );

        static
        void
        ThrowIfLastErrorIs(
            int     unexpectedLastWin32Error
            );

        static
        void
        ThrowLastError(
            void
            );

        static
        void
        ThrowIfNotSuccess(
            int     lastWin32Error
            );

        static
        String*
        GetFormattedWin32Error(
            int     lastWin32Error    
            );

        private:
        
        int   win32ErrorCode;

        static
        const 
        int  defaultWin32ErrorMessageLength = 256;
    };

    private __gc class InternalHResultPrintSystemException
    {
        public private:

        InternalHResultPrintSystemException(
            int   hResult
            );

        __property
        int
        get_HResult(
            void
            );

        static
        void
        ThrowIfFailedHResult(
            int hResult
            );

        private:
        
        int   hResult;
    };

    /// <summary>
    /// PrintQueueStreamException exception object.
    /// Exceptions of type PrintQueueStreamException are thrown when writing data to a 
    /// <c>PrintQueue</c> object.
    /// </summary>
    /// <ExternalAPI/>    
    [System::Serializable]
    public __gc class PrintQueueStreamException : 
    public PrintSystemException
    {
        public:

        PrintQueueStreamException(
            void
            );

        PrintQueueStreamException(
            String*             message 
            );

        PrintQueueStreamException(
            String*             message,
            Exception*          innerException
            );

        __property 
        Int32
        get_NumberOfWrittenBytes(
            void
            );

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>                 
        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        public private:

        PrintQueueStreamException(
            int         errorCode,
            String*     message,
            int         numberOfWrittenBytes
            );

        protected:

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary>     
        /// <param name="info"> The object that holds the serialized object data. </param> 
        /// <param name="context"> The contextual information about the source or destination. </param>         
        PrintQueueStreamException(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        private:

        Int32           numberOfWrittenBytes;

    };

    [System::Serializable]
    public __gc class PrintJobException : 
    public PrintSystemException
    {
        public:

        PrintJobException(
            void
            );

        PrintJobException(
            String*             message 
            );

        PrintJobException(
            String*             message,
            Exception*          innerException
            );

        __property 
        PrintJobStatus 
        get_JobStatus(
            void
            );

        void
        GetObjectData(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        public private:

        PrintJobException(
            int                 errorCode,
            String*             message,
            PrintJobStatus      jobStatus
            );

        PrintJobException(
            int                 errorCode,
            String*             message,
            PrintJobStatus      jobStatus,
            Exception*          innerException
            );

        PrintJobException(
            int                 errorCode,
            String*             message
            );

        PrintJobException(
            int                 errorCode,
            String*             message,
            Exception*          innerException
            );

        protected:

        PrintJobException(
            System::Runtime::Serialization::SerializationInfo*  info,
            System::Runtime::Serialization::StreamingContext    context
            );

        private:

        PrintJobStatus          jobStatus;

    };
    

}
}
#endif
