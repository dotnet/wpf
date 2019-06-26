// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


    Abstract:

        Print System exception objects declaration.
--*/
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Security; //SecurityCritical
using System.Collections.ObjectModel;
using System.Collections.Generic;


using MS.Internal.PrintWin32Thunk.Win32ApiThunk;

namespace System.Printing
{
    abstract internal class  PrinterHResult
    {
        ///<summary>
        ///
        ///</summary>
        public static
        int
        HResultFromWin32(
            int win32ErrorCode
            )
        {
            return (win32ErrorCode <= 0) ?
                       win32ErrorCode :
                       (int)((win32ErrorCode & unchecked((int)0x80000000)) | ((int)Facility.Win32 << 16) | unchecked((int)0x80000000));
        }
        
        ///<summary>
        ///
        ///</summary>
        public static
        int
        HResultCode(
            int errorCode
            )
        {
            return ((errorCode) & 0xFFFF);
        }

        ///<summary>
        ///
        ///</summary>
        public static
        Facility
        HResultFacility(
            int errorCode
            )
        {
            return (Facility)(((errorCode) >> 16) & 0x1fff);
        }
        
        public enum Error
        {
            PrintSystemGenericError = (int)1801, //ERROR_INVALID_PRINTER_NAME,
            PrintingCancelledGenericError = (int)1223, //ERROR_CANCELLED,
        };
        
        public enum Facility
        {
            Win32 = 7,
        };        
    }

    /// <summary>
    /// Print System exception object.
    /// </summary>
    /// <ExternalAPI/>
    [System.Serializable]
    public class PrintSystemException : SystemException
    {

        /// <summary>
        /// PrintSystemException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: Print System exception.
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintSystemException(
            ) : base(GetMessageFromResource("PrintSystemException.Generic"))
        {
            base.HResult = PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintSystemGenericError);
        }

        /// <summary>
        /// PrintSystemException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintSystemException(
            String             message
            ) : base(GetMessageFromResource(PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintSystemGenericError),
                                            message))
        {
            base.HResult = PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintSystemGenericError);
        }

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
        public
        PrintSystemException(
            String             message,
            Exception          innerException
            ) : base (GetMessageFromResource(PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintSystemGenericError),
                                             message),
                                             innerException)
        {
            base.HResult = PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintSystemGenericError);
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override
        void
        GetObjectData(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext    context
            )
        {
            base.GetObjectData(info, context);
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintSystemException(
            int        errorCode,
            String     message
            ) : base(GetMessageFromResource(errorCode,message))
        {
            base.HResult = errorCode;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintSystemException(
            int        errorCode,
            String     message,
            String     printerMessage
            ) : base(GetMessageFromResource(errorCode,message) + printerMessage)
        {
            base.HResult = errorCode;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintSystemException(
            int        errorCode,
            String     message,
            Exception  innerException
            ) : base(GetMessageFromResource(errorCode,message),
                     innerException)
        {
            base.HResult = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintSystemException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext   context
            ) : base(info, context)
        {
        }

        /// <summary>
        /// Loads the resource string for a given resource key.
        /// </summary>
        /// <param name="resourceKey"> Resource key. </param>
        private static
        String
        GetMessageFromResource(
            String     resourceKey
            )
        {
            return printResourceManager.GetString(resourceKey,
                                                  System.Threading.Thread.CurrentThread.CurrentUICulture);
        }

        /// <summary>
        /// Loads the resource string for a Win32 error and
        /// formats an error message given a resource key.
        /// </summary>
        /// <param name="errorCode"> Win32 error code. </param>
        /// <param name="resourceKey"> Resource key. </param>
        private static
        String
        GetMessageFromResource(
            int        errorCode,
            String     resourceKey
            )
        {
            String exceptionMessage = null;

            String resourceString = printResourceManager.GetString(resourceKey,
                                                                   System.Threading.Thread.CurrentThread.CurrentUICulture);

            if (PrinterHResult.HResultFacility(errorCode) == PrinterHResult.Facility.Win32)
            {
                exceptionMessage = String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture,
                                                 resourceString,
                                                 GetFormattedWin32Error(PrinterHResult.HResultCode(errorCode)));
            }
            else
            {
                exceptionMessage = String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture,
                                                 resourceString,
                                                 errorCode);


            }
            return exceptionMessage;
        }

        ///<summary>
        ///
        ///</summary>
        private static
        String
        GetFormattedWin32Error(
            int win32Error
            )
        {
            StringBuilder win32ErrorMessage = new StringBuilder(defaultWin32ErrorMessageLength);

            int charCount = NativeMethodsForPrintExceptions.InvokeFormatMessage(FormatMessageFromSystem,
                                                                IntPtr.Zero,
                                                                win32Error,
                                                                0,
                                                                win32ErrorMessage,
                                                                win32ErrorMessage.Capacity,
                                                                IntPtr.Zero);

            if(charCount < 0)
            {
                win32ErrorMessage.Length = 0;
            }
            else
            {
                if(charCount < win32ErrorMessage.Length)
                {
                    win32ErrorMessage.Length = charCount;
                }
            }
            
            return win32ErrorMessage.ToString();
        }

        ///<summary>
        ///
        ///</summary>
        static
        PrintSystemException(
            )
        {
            printResourceManager = new System.Resources.ResourceManager("System.Printing",
                                                                        (typeof(PrintSystemException)).Assembly);
        }

        ///<summary>
        ///
        ///</summary>
        static
        System.Resources.ResourceManager     printResourceManager;

        const
        int  defaultWin32ErrorMessageLength = 256;

        const
        int  FormatMessageFromSystem = unchecked((int)0x00001000);
    };

    /// <summary>
    /// PrintQueueException exception object.
    /// Exceptions of type PrintQueueException are thrown when operating on a
    /// <c>PrintQueue</c> object.
    /// </summary>
    /// <ExternalAPI/>
    [System.Serializable]
    public class PrintQueueException : PrintSystemException
    {
        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: An exception occurred while creating the PrintQueue object. Win32 error: {0}
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintQueueException(
            ): base("PrintSystemException.PrintQueue.Generic")

        {   this.printerName = null;
        }

        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintQueueException(
            String             message
            ) : base (message)
        {
            this.printerName = null;
        }

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
        public
        PrintQueueException(
            String             message,
            Exception          innerException
            ): base(message,
                    innerException)
            {
                this.printerName = null;
            }

        /// <value>
        /// Printer name property. The name represents the name identifier of
        /// the PrintQueue object that was running the code when the exception was thrown.
        /// </value>
        public
        String PrinterName
        {
            get
            {
                return printerName;
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override
        void
        GetObjectData(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext   context
            )
        {
            if (info != null)
            {
                info.AddValue("PrinterName", printerName);
            }
            base.GetObjectData(info, context);
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintQueueException(
            int        errorCode,
            String     message,
            String     printerName
            ) : base(errorCode, message)
        {
            this.printerName = printerName;
            base.HResult     = errorCode;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintQueueException(
            int        errorCode,
            String     message,
            String     printerName,
            String     printerMessage
            ) : base(errorCode, message, printerMessage)
        {
            this.printerName = printerName;
            base.HResult     = errorCode;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintQueueException(
            int                errorCode,
            String             message,
            String             printerName,
            Exception          innerException
            ) : base(errorCode,
                     message,
                     innerException)
        {
            this.printerName = printerName;
            base.HResult     = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintQueueException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext    context
            ) : base(info, context)
        {
            this.printerName = (String)(info.GetValue("PrinterName", typeof(System.String)));
        }

        private String     printerName;

    };

    /// <summary>
    /// PrintServerException constructor.
    /// Exceptions of type PrintServerException are thrown when operating on a
    /// <c>PrintServer</c> object.
    /// </summary>
    /// <remarks>
    /// Default error code: ERROR_INVALID_PRINTER_NAME.
    /// </remarks>
    [System.Serializable]
    public class PrintServerException : PrintSystemException
    {
        /// <summary>
        /// PrintQueueException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: An exception occurred while creating the PrintServer object. Win32 error is: {0}
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintServerException(
            ): base ("PrintSystemException.PrintServer.Generic")

        {
            this.serverName = null;
        }

        /// <summary>
        /// PrintServerException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintServerException(
            String             message
            ): base(message)
        {
            this.serverName = null;
        }

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
        public
        PrintServerException(
            String             message,
            Exception          innerException
            ): base(message,
                    innerException)
        {
            this.serverName = null;
        }

        /// <value>
        /// Server name property. The name represents the name identifier of
        /// the PrintServer object that was running the code when the exception was thrown.
        /// </value>
        public
        String ServerName
        {
            get
            {
                return serverName;
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override
        void
        GetObjectData(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext   context
            )
        {
            if (info != null)
            {
                info.AddValue("ServerName", serverName);
            }

            base.GetObjectData(info, context);
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintServerException(
            int        errorCode,
            String     message,
            String     serverName
            ): base(errorCode, message)
        {
            this.serverName = serverName;
            base.HResult    = errorCode;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintServerException(
            int                errorCode,
            String             message,
            String             serverName,
            Exception          innerException
            ): base(errorCode,
                             message,
                             innerException)
        {
            this.serverName = serverName;
            base.HResult    = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the PrintServerException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintServerException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext    context
            ): base(info, context)
        {
            this.serverName = (String)(info.GetValue("ServerName", typeof(String)));
        }

        private String     serverName;

    };

    /// <summary>
    /// PrintCommitAttributesException constructor.
    /// Exceptions of type PrintCommitAttributesException are thrown
    /// when Commit method of any of the Print System objects that implement the method.
    /// </summary>
    /// <remarks>
    /// Default error code: ERROR_INVALID_PRINTER_NAME.
    /// </remarks>
    [System.Serializable]
    public class PrintCommitAttributesException : PrintSystemException
    {

        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: Print System exception.
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintCommitAttributesException(
            ): base("PrintSystemException.CommitPrintSystemAttributesException")
        {
            committedAttributes = new Collection<String>();
            failedAttributes   = new Collection<String>();
            printObjectName    = null;
        }

        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintCommitAttributesException(
            String    message
            ): base(message)
        {
            printObjectName    = null;
            committedAttributes = new Collection<String>();
            failedAttributes   = new Collection<String>();
        }

        /// <summary>
        /// PrintCommitAttributesException constructor.
        /// </summary>
        /// <remarks>
        /// Default error code: ERROR_INVALID_PRINTER_NAME.
        /// </remarks>
        public
        PrintCommitAttributesException(
            String                 message,
            Exception              innerException
            ): base(message,
                    innerException)
        {
            printObjectName    = null;
            committedAttributes = new Collection<String>();
            failedAttributes   = new Collection<String>();
        }

        /// <value>
        /// PrintSystemObject name property. The name represents the name identifier of
        /// the PrintSystemObject object that was running the code when the exception was thrown.
        /// </value>
        public
        String
        PrintObjectName
        {
            get
            {
                return printObjectName;
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override
        void
        GetObjectData(
            System.Runtime.Serialization.SerializationInfo      info,
            System.Runtime.Serialization.StreamingContext       context
            )
        {
            if (info != null)
            {
                info.AddValue("CommittedAttributes", committedAttributes);
                info.AddValue("FailedAttributes",   failedAttributes);
                info.AddValue("ObjectName",         printObjectName);
            }

            base.GetObjectData(info, context);
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintCommitAttributesException(
            int                 errorCode,
            Collection<String>  attributesSuccessList,
            Collection<String>  attributesFailList
            ): base(errorCode,
                    "PrintSystemException.CommitPrintSystemAttributesException")
        {
            this.committedAttributes = attributesSuccessList;
            this.failedAttributes   = attributesFailList;
            this.printObjectName    = null;
        }

        ///<summary>
        ///
        ///</summary>
        public
        PrintCommitAttributesException(
            int                 errorCode,
            String              message,
            Collection<String>  attributesSuccessList,
            Collection<String>  attributesFailList,
            String              objectName
            ) : base(errorCode,message)
        {
            this.committedAttributes = attributesSuccessList;
            this.failedAttributes   = attributesFailList;
            this.printObjectName    = null;
        }

        ///<summary>
        ///
        ///</summary>
        public
        Collection<String> CommittedAttributesCollection
        {
            get
            {
                return committedAttributes;
            }
        }

        ///<summary>
        ///
        ///</summary>
        public
        Collection<String> FailedAttributesCollection
        {
            get
            {
                return failedAttributes;
            }
        }

        /// <summary>
        /// Initializes a new instance of the PrintCommitAttributesException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintCommitAttributesException(
            System.Runtime.Serialization.SerializationInfo      info,
            System.Runtime.Serialization.StreamingContext        context
            ): base(info, context)

            {
                committedAttributes = (Collection<String>)(info.GetValue("CommittedAttributes",  committedAttributes.GetType()));
                failedAttributes   = (Collection<String>)(info.GetValue("FailedAttributes",    failedAttributes.GetType()));
                printObjectName    = (String)(info.GetValue("ObjectName",                      printObjectName.GetType()));
            }


        Collection<String>  committedAttributes;
        Collection<String>  failedAttributes;
        String              printObjectName;
    };

    /// <summary>
    /// PrintJobException exception object.
    /// Exceptions of type PrintJobException submitting a print job to a PrintQueue
    /// <c>PrintQueue</c> object.
    /// </summary>
    /// <ExternalAPI/>
    [System.Serializable]
    public class PrintJobException : PrintSystemException
    {
        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        public
        PrintJobException(
            )
        {
            this.jobId          = 0;
            this.printQueueName = null;
            this.jobContainer   = null;
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintJobException(
            String             message
            ): base(message)
        {
            this.jobId          = 0;
            this.printQueueName = null;
            this.jobContainer   = null;
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintJobException(
            String             message,
            Exception          innerException
            ) : base(message,
                     innerException)
        {
            this.jobId          = 0;
            this.printQueueName = null;
            this.jobContainer   = null;
        }

        /// <value>
        /// Job identifier
        /// </value>
        public
        int
        JobId
        {
            get
            {
                return jobId;
            }
        }

        /// <value>
        /// Job name
        /// </value>
        public
        String
        JobName
        {
            get
            {
                return jobContainer;
            }
        }

        /// <value>
        /// Printer name
        /// </value>
        public
        String
        PrintQueueName
        {
            get
            {
                return printQueueName;
            }
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override
        void
        GetObjectData(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext    context
            )
        {
            if( info != null )
            {
                info.AddValue("JobId", jobId );
            }
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintJobException(
            int errorCode,
            String message
            ) : base(errorCode, (message))
        {
            this.jobId = 0;
            this.printQueueName = null;
            this.jobContainer = null;
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="jobId">
        /// Job identifier
        /// </param>
        /// <param name="jobName">
        /// Job name
        /// </param>
        /// <param name="printQueueName">
        /// Printer name
        /// </param>
        public
        PrintJobException(
            int              errorCode,
            String           message,
            String           printQueueName,
            String           jobName,
            int              jobId
            ) : base(errorCode, (message))
        {
            this.printQueueName = printQueueName;
            this.jobContainer = jobName;
            this.jobId = jobId;
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="jobId">
        /// Job identifier
        /// </param>
        /// <param name="jobName">
        /// Job name
        /// </param>
        /// <param name="printQueueName">
        /// Printer name
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintJobException(
            int              errorCode,
            String           message,
            String           printQueueName,
            String           jobName,
            int              jobId,
            Exception        innerException
            ) : base(errorCode, (message), innerException)
        {
            this.printQueueName = printQueueName;
            this.jobContainer = jobName;
            this.jobId = jobId;
        }

        /// <summary>
        /// PrintJobException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintJobException(
            int                errorCode,
            String             message,
            Exception          innerException
            ) : base(errorCode, message, innerException)
        {
            this.jobId          = 0;
            this.printQueueName = null;
            this.jobContainer   = null;
        }

        /// <summary>
        /// Initializes a new instance of the PrintSystemException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintJobException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext    context
            ) : base(info, context)
        {
            this.jobId = (int)(info.GetValue("JobId", typeof(int)));
        }

        int            jobId;
        String         printQueueName;
        String         jobContainer;
    };

    /// <summary>
    /// PrintingCanceledException exception object.
    /// Exceptions of type PrintingCanceledException are thrown when the user cancels
    /// a printing operation.
    /// </summary>
    /// <ExternalAPI/>
    [System.Serializable]
    public class PrintingCanceledException : PrintJobException
    {
        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <remarks>
        /// Default message: Printing has been cancelled. Win32 error is {0}
        /// Default error code: ERROR_CANCELLED.
        /// </remarks>
        public
        PrintingCanceledException(
            ) : base(PrinterHResult.HResultFromWin32((int)PrinterHResult.Error.PrintingCancelledGenericError), "PrintSystemException.PrintingCancelled.Generic")
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintingCanceledException(
            String message
            ) : base(message)
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintingCanceledException(
            String message,
            Exception innerException
            ) : base(message,
                     innerException)
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        public
        PrintingCanceledException(
            int errorCode,
            String message
            ) : base(errorCode, message)
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintingCanceledException(
            int errorCode,
            String message,
            Exception innerException
            ) : base(errorCode, message, innerException)
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="printQueueName">
        /// Printer name
        /// </param>
        /// <param name="jobName">
        /// Job name
        /// </param>
        /// <param name="jobId">
        /// Job identifier
        /// </param>
        public
        PrintingCanceledException(
            int errorCode,
            String message,
            String printQueueName,
            String jobName,
            int jobId
            ) : base(errorCode, message, printQueueName, jobName, jobId)
        {
        }

        /// <summary>
        /// PrintingCanceledException constructor.
        /// </summary>
        /// <param name="errorCode">
        /// HRESULT error code
        /// </param>
        /// <param name="message">
        /// Message that describes the current exception. Must be localized.
        /// </param>
        /// <param name="printQueueName">
        /// Printer name
        /// </param>
        /// <param name="jobName">
        /// Job name
        /// </param>
        /// <param name="jobId">
        /// Job identifier
        /// </param>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public
        PrintingCanceledException(
            int         errorCode,
            String      message,
            String      printQueueName,
            String      jobName,
            int         jobId,
            Exception   innerException

            ) : base(errorCode, message, printQueueName, jobName, jobId, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PrintingCanceledException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected
        PrintingCanceledException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext   context
            ) : base(info, context)
        {
        }
    };

    /// <summary>
    /// Print System exception object.
    /// </summary>
    /// <ExternalAPI/>
    [System.Serializable]
    public class PrintingNotSupportedException : PrintSystemException
    {
        /// <summary>
        /// PrintingNotSupportedException constructor.
        /// </summary>
        public PrintingNotSupportedException()            
        {
        }

        /// <summary>
        /// PrintingNotSupportedException constructor.
        /// </summary>
        /// <param name="message">
        /// Message that describes the current exception.
        /// </param>
        public PrintingNotSupportedException(String message) : base("PrintSystemException.Generic")
        {
        }

        /// <summary>
        /// PrintingNotSupportedException constructor.
        /// </summary>
        /// <param name="innerException">
        /// The exception instance that caused the current exception.
        /// </param>
        public PrintingNotSupportedException(string message, Exception innerException)
            : base("PrintSystemException.Generic", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PrintQueueException class with serialized data.
        /// </summary>
        /// <param name="info"> The object that holds the serialized object data. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        protected PrintingNotSupportedException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context
            )
            : base(info, context)
        {
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <remarks>
        /// Inherited from Exception.
        /// </remarks>
        /// <param name="info"> Holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The contextual information about the source or destination. </param>
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context
            )
        {
            base.GetObjectData(info, context);
        }
    };
}
