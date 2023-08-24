// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: RightsManagement exceptions
//
//
//
//

using System;
using System.Runtime.Serialization;
using System.Security;
using System.Windows;
using MS.Internal.Security.RightsManagement;

using MS.Internal.WindowsBase;
    
namespace System.Security.RightsManagement 
{
    /// <summary>    
    /// Rights Management exception class is thrown when an RM Operation can not be completed for a variety of reasons.
    /// Some of the failure codes codes application, can potentially, mitigate automatically. Some failure codes  can be 
    /// mitigated by coordinated action of the user and the application. 
    /// </summary>    
    [Serializable()]
    public class RightsManagementException : Exception
    {
        /// <summary>
        /// Creates an new instance of the RightsManagementException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message 
        /// that describes the error. This message takes into account the current system culture.
        /// </summary>
        public RightsManagementException() : 
                                                    base(SR.RmExceptionGenericMessage)
        {}

        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RightsManagementException(string message) : base(message)
        {}

        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance with a specified error message.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RightsManagementException(string message, Exception innerException) : 
                                                    base(message, innerException)
        { }


        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message 
        /// that describes the error. This message takes into account the current system culture.
        /// The FailureCode property is initialized using the failureCode parameter.
        /// </summary>
        /// <param name="failureCode">The FailureCode that indicates specific reason for the exception.</param>
        public RightsManagementException(RightsManagementFailureCode failureCode) : 
                                                    base(Errors.GetLocalizedFailureCodeMessageWithDefault(failureCode))
        {
            _failureCode = failureCode; // we do not check the validity of the failureCode range , as it might contain a generic 
                                                         //  HR code, not covered by the RightsManagementFailureCode enumeration 
            
        }


        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance using the message parameter.
        /// The content of message is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the current system culture.
        /// The FailureCode property is initialized using the failureCode parameter.
        /// </summary>
        /// <param name="failureCode">The FailureCode that indicates specific reason for the exception.</param>
        /// <param name="message">The message that describes the error.</param>
        public RightsManagementException(RightsManagementFailureCode failureCode, string message) : 
                                                    base(message)
        {
            _failureCode = failureCode;// we do not check the validity of the failureCode range , as it might contain a generic 
                                                         //  HR code, not covered by the RightsManagementFailureCode enumeration 
        }
                                                    
        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance to a system-supplied message 
        /// that describes the error. This message takes into account the current system culture.
        /// The FailureCode property is initialized using the failureCode parameter.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="failureCode">The FailureCode that indicates specific reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RightsManagementException(RightsManagementFailureCode failureCode, Exception innerException) : 
                                                    base(Errors.GetLocalizedFailureCodeMessageWithDefault(failureCode), innerException)
        {
            _failureCode = failureCode;// we do not check the validity of the failureCode range , as it might contain a generic 
                                                         //  HR code, not covered by the RightsManagementFailureCode enumeration 
        }
                                                            

        /// <summary>
        /// Creates a new instance of RightsManagementException class.
        /// This constructor initializes the Message property of the new instance using the message parameter.
        /// The content of message is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized for the current system culture.
        /// The FailureCode property is initialized using the failureCode parameter.
        /// The InnerException property is initialized using the innerException parameter.
        /// </summary>
        /// <param name="failureCode">The FailureCode that indicates specific reason for the exception.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RightsManagementException(RightsManagementFailureCode failureCode, string message, Exception innerException) : 
                                                    base(message, innerException)
        {
            _failureCode = failureCode;// we do not check the validity of the failureCode range , as it might contain a generic 
                                                         //  HR code, not covered by the RightsManagementFailureCode enumeration 
        }
                                  
        /// <summary>
        /// Creates a new instance of RightsManagementException class and initializes it with serialized data.
        /// This constructor is called during deserialization to reconstitute the exception object transmitted over a stream.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        protected RightsManagementException(SerializationInfo info, StreamingContext context) : 
                                                    base(info, context)
        {
            _failureCode = (RightsManagementFailureCode)info.GetInt32(_serializationFailureCodeAttributeName);
                                                         // we do not check the validity of the failureCode range , as it might contain a generic 
                                                         //  HR code, not covered by the RightsManagementFailureCode enumeration 
        }
#pragma warning restore SYSLIB0051 // Type or member is obsolete
        /// <summary>
        /// Sets the SerializationInfo object with the Failure Code and additional exception information. 
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
            info.AddValue(_serializationFailureCodeAttributeName, (Int32)_failureCode);
        }
#pragma warning restore SYSLIB0051 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member

        /// <summary>
        /// Returns the specific error code that can be used to indetify and mitigate the reason which caused the exception.
        /// </summary>
        public RightsManagementFailureCode FailureCode
        {
            get
            {
                return _failureCode;
            }
        }
            
        private RightsManagementFailureCode _failureCode;
        private const string _serializationFailureCodeAttributeName = "FailureCode";
    }

/*
    /// <summary>    
    /// NotActivatedException 
    /// </summary>    
    [Serializable()]
    public class NotActivatedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the NotActivatedException class.
        /// </summary>
        public NotActivatedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the NotActivatedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public NotActivatedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the NotActivatedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public NotActivatedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the NotActivatedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NotActivatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ServiceDiscoveryException 
    /// </summary>    
    [Serializable()]
    public class ServiceDiscoveryException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ServiceDiscoveryException class.
        /// </summary>
        public ServiceDiscoveryException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ServiceDiscoveryException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ServiceDiscoveryException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ServiceDiscoveryException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServiceDiscoveryException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ServiceDiscoveryException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ServiceDiscoveryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ServerConnectionFailureException 
    /// </summary>    
    [Serializable()]
    public class ServerConnectionFailureException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ServerConnectionFailureException class.
        /// </summary>
        public ServerConnectionFailureException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ServerConnectionFailureException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ServerConnectionFailureException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ServerConnectionFailureException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServerConnectionFailureException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ServerConnectionFailureException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ServerConnectionFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ServerNotFoundException 
    /// </summary>    
    [Serializable()]
    public class ServerNotFoundException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ServerNotFoundException class.
        /// </summary>
        public ServerNotFoundException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ServerNotFoundException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ServerNotFoundException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ServerNotFoundException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServerNotFoundException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ServerNotFoundException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ServerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InvalidServerResponseException 
    /// </summary>    
    [Serializable()]
    public class InvalidServerResponseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidServerResponseException class.
        /// </summary>
        public InvalidServerResponseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidServerResponseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidServerResponseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidServerResponseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidServerResponseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidServerResponseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidServerResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ActiveDirectoryEntryNotFoundException 
    /// </summary>    
    [Serializable()]
    public class ActiveDirectoryEntryNotFoundException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ActiveDirectoryEntryNotFoundException class.
        /// </summary>
        public ActiveDirectoryEntryNotFoundException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ActiveDirectoryEntryNotFoundException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ActiveDirectoryEntryNotFoundException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ActiveDirectoryEntryNotFoundException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ActiveDirectoryEntryNotFoundException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ActiveDirectoryEntryNotFoundException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ActiveDirectoryEntryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }

    /// <summary>    
    /// ServerErrorException 
    /// </summary>    
    [Serializable()]
    public class ServerErrorException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ServerErrorException class.
        /// </summary>
        public ServerErrorException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ServerErrorException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ServerErrorException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ServerErrorException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServerErrorException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ServerErrorException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ServerErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// HardwareIdCorruptionException 
    /// </summary>    
    [Serializable()]
    public class HardwareIdCorruptionException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the HardwareIdCorruptionException class.
        /// </summary>
        public HardwareIdCorruptionException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the HardwareIdCorruptionException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public HardwareIdCorruptionException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the HardwareIdCorruptionException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public HardwareIdCorruptionException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the HardwareIdCorruptionException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected HardwareIdCorruptionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InstallationFailedException 
    /// </summary>    
    [Serializable()]
    public class InstallationFailedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InstallationFailedException class.
        /// </summary>
        public InstallationFailedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InstallationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InstallationFailedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InstallationFailedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InstallationFailedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InstallationFailedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InstallationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// AuthenticationFailedException 
    /// </summary>    
    [Serializable()]
    public class AuthenticationFailedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class.
        /// </summary>
        public AuthenticationFailedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public AuthenticationFailedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public AuthenticationFailedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the AuthenticationFailedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected AuthenticationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ActivationFailedException 
    /// </summary>    
    [Serializable()]
    public class ActivationFailedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ActivationFailedException class.
        /// </summary>
        public ActivationFailedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ActivationFailedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ActivationFailedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ActivationFailedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ActivationFailedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ActivationFailedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ActivationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// OutOfQuotaException 
    /// </summary>    
    [Serializable()]
    public class OutOfQuotaException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the OutOfQuotaException class.
        /// </summary>
        public OutOfQuotaException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the OutOfQuotaException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public OutOfQuotaException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the OutOfQuotaException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public OutOfQuotaException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the OutOfQuotaException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected OutOfQuotaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    
    /// <summary>    
    /// InvalidLicenseException 
    /// </summary>    
    [Serializable()]
    public class InvalidLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidLicenseException class.
        /// </summary>
        public InvalidLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InvalidLicenseSignatureException 
    /// </summary>    
    [Serializable()]
    public class InvalidLicenseSignatureException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidLicenseSignatureException class.
        /// </summary>
        public InvalidLicenseSignatureException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseSignatureException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidLicenseSignatureException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseSignatureException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidLicenseSignatureException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidLicenseSignatureException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLicenseSignatureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// EncryptionNotPermittedException 
    /// </summary>    
    [Serializable()]
    public class EncryptionNotPermittedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the EncryptionNotPermittedException class.
        /// </summary>
        public EncryptionNotPermittedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the EncryptionNotPermittedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public EncryptionNotPermittedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the EncryptionNotPermittedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public EncryptionNotPermittedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the EncryptionNotPermittedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected EncryptionNotPermittedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// RightNotGrantedException 
    /// </summary>    
    [Serializable()]
    public class RightNotGrantedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the RightNotGrantedException class.
        /// </summary>
        public RightNotGrantedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the RightNotGrantedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public RightNotGrantedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the RightNotGrantedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RightNotGrantedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the RightNotGrantedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected RightNotGrantedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InvalidVersionException 
    /// </summary>    
    [Serializable()]
    public class InvalidVersionException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidVersionException class.
        /// </summary>
        public InvalidVersionException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidVersionException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidVersionException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidVersionException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidVersionException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidVersionException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// SecurityProcessorNotLoadedException 
    /// </summary>    
    [Serializable()]
    public class SecurityProcessorNotLoadedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the SecurityProcessorNotLoadedException class.
        /// </summary>
        public SecurityProcessorNotLoadedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorNotLoadedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public SecurityProcessorNotLoadedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorNotLoadedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public SecurityProcessorNotLoadedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorNotLoadedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SecurityProcessorNotLoadedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// SecurityProcessorAlreadyLoadedException 
    /// </summary>    
    [Serializable()]
    public class SecurityProcessorAlreadyLoadedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the SecurityProcessorAlreadyLoadedException class.
        /// </summary>
        public SecurityProcessorAlreadyLoadedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorAlreadyLoadedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public SecurityProcessorAlreadyLoadedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorAlreadyLoadedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public SecurityProcessorAlreadyLoadedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the SecurityProcessorAlreadyLoadedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SecurityProcessorAlreadyLoadedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ClockRollbackDetectedException 
    /// </summary>    
    [Serializable()]
    public class ClockRollbackDetectedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ClockRollbackDetectedException class.
        /// </summary>
        public ClockRollbackDetectedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ClockRollbackDetectedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ClockRollbackDetectedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ClockRollbackDetectedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ClockRollbackDetectedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ClockRollbackDetectedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ClockRollbackDetectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// UnexpectedErrorException 
    /// </summary>    
    [Serializable()]
    public class UnexpectedErrorException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the UnexpectedErrorException class.
        /// </summary>
        public UnexpectedErrorException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the UnexpectedErrorException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public UnexpectedErrorException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the UnexpectedErrorException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public UnexpectedErrorException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the UnexpectedErrorException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected UnexpectedErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ValidityTimeViolatedException 
    /// </summary>    
    [Serializable()]
    public class ValidityTimeViolatedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ValidityTimeViolatedException class.
        /// </summary>
        public ValidityTimeViolatedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ValidityTimeViolatedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ValidityTimeViolatedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ValidityTimeViolatedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ValidityTimeViolatedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ValidityTimeViolatedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ValidityTimeViolatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// BrokenCertChainException 
    /// </summary>    
    [Serializable()]
    public class BrokenCertChainException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the BrokenCertChainException class.
        /// </summary>
        public BrokenCertChainException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the BrokenCertChainException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public BrokenCertChainException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the BrokenCertChainException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public BrokenCertChainException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the BrokenCertChainException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected BrokenCertChainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// PolicyViolationException 
    /// </summary>    
    [Serializable()]
    public class PolicyViolationException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the PolicyViolationException class.
        /// </summary>
        public PolicyViolationException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the PolicyViolationException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public PolicyViolationException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the PolicyViolationException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public PolicyViolationException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the PolicyViolationException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected PolicyViolationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// RevokedLicenseException 
    /// </summary>    
    [Serializable()]
    public class RevokedLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the RevokedLicenseException class.
        /// </summary>
        public RevokedLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the RevokedLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public RevokedLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the RevokedLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RevokedLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the RevokedLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected RevokedLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ContentNotInUseLicenseException 
    /// </summary>    
    [Serializable()]
    public class ContentNotInUseLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ContentNotInUseLicenseException class.
        /// </summary>
        public ContentNotInUseLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ContentNotInUseLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ContentNotInUseLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ContentNotInUseLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ContentNotInUseLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ContentNotInUseLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ContentNotInUseLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// IndicatedPrincipalMissingException 
    /// </summary>    
    [Serializable()]
    public class IndicatedPrincipalMissingException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the IndicatedPrincipalMissingException class.
        /// </summary>
        public IndicatedPrincipalMissingException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the IndicatedPrincipalMissingException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public IndicatedPrincipalMissingException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the IndicatedPrincipalMissingException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public IndicatedPrincipalMissingException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the IndicatedPrincipalMissingException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected IndicatedPrincipalMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// MachineNotFoundInGroupIdentityException 
    /// </summary>    
    [Serializable()]
    public class MachineNotFoundInGroupIdentityException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the MachineNotFoundInGroupIdentityException class.
        /// </summary>
        public MachineNotFoundInGroupIdentityException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the MachineNotFoundInGroupIdentityException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public MachineNotFoundInGroupIdentityException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the MachineNotFoundInGroupIdentityException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MachineNotFoundInGroupIdentityException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the MachineNotFoundInGroupIdentityException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MachineNotFoundInGroupIdentityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// IntervalTimeViolatedException 
    /// </summary>    
    [Serializable()]
    public class IntervalTimeViolatedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the IntervalTimeViolatedException class.
        /// </summary>
        public IntervalTimeViolatedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the IntervalTimeViolatedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public IntervalTimeViolatedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the IntervalTimeViolatedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public IntervalTimeViolatedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the IntervalTimeViolatedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected IntervalTimeViolatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InvalidHardwareIdException 
    /// </summary>    
    [Serializable()]
    public class InvalidHardwareIdException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidHardwareIdException class.
        /// </summary>
        public InvalidHardwareIdException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidHardwareIdException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidHardwareIdException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidHardwareIdException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidHardwareIdException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidHardwareIdException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidHardwareIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// DebuggerDetectedException 
    /// </summary>    
    [Serializable()]
    public class DebuggerDetectedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the DebuggerDetectedException class.
        /// </summary>
        public DebuggerDetectedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the DebuggerDetectedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public DebuggerDetectedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the DebuggerDetectedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DebuggerDetectedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the DebuggerDetectedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DebuggerDetectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// EmailNotVerifiedException 
    /// </summary>    
    [Serializable()]
    public class EmailNotVerifiedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the EmailNotVerifiedException class.
        /// </summary>
        public EmailNotVerifiedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the EmailNotVerifiedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public EmailNotVerifiedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the EmailNotVerifiedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public EmailNotVerifiedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the EmailNotVerifiedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected EmailNotVerifiedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// NotCertifiedException 
    /// </summary>    
    [Serializable()]
    public class NotCertifiedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the NotCertifiedException class.
        /// </summary>
        public NotCertifiedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the NotCertifiedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public NotCertifiedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the NotCertifiedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public NotCertifiedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the NotCertifiedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NotCertifiedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }    
    /// <summary>    
    /// InvalidUnsignedPublishLicenseException 
    /// </summary>    
    [Serializable()]
    public class InvalidUnsignedPublishLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidUnsignedPublishLicenseException class.
        /// </summary>
        public InvalidUnsignedPublishLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidUnsignedPublishLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidUnsignedPublishLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidUnsignedPublishLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidUnsignedPublishLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidUnsignedPublishLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidUnsignedPublishLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// InvalidContentKeyLengthException 
    /// </summary>    
    [Serializable()]
    public class InvalidContentKeyLengthException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidContentKeyLengthException class.
        /// </summary>
        public InvalidContentKeyLengthException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidContentKeyLengthException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public InvalidContentKeyLengthException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the InvalidContentKeyLengthException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidContentKeyLengthException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the InvalidContentKeyLengthException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidContentKeyLengthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// ExpiredPublishLicenseException 
    /// </summary>    
    [Serializable()]
    public class ExpiredPublishLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the ExpiredPublishLicenseException class.
        /// </summary>
        public ExpiredPublishLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the ExpiredPublishLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ExpiredPublishLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the ExpiredPublishLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ExpiredPublishLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the ExpiredPublishLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ExpiredPublishLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// UseLicenseAcquisitionFailedException 
    /// </summary>    
    [Serializable()]
    public class UseLicenseAcquisitionFailedException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the UseLicenseAcquisitionFailedException class.
        /// </summary>
        public UseLicenseAcquisitionFailedException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the UseLicenseAcquisitionFailedException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public UseLicenseAcquisitionFailedException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the UseLicenseAcquisitionFailedException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public UseLicenseAcquisitionFailedException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the UseLicenseAcquisitionFailedException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected UseLicenseAcquisitionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
    /// <summary>    
    /// IncompatiblePublishLicenseException 
    /// </summary>    
    [Serializable()]
    public class IncompatiblePublishLicenseException : RightsManagementException
    {
        /// <summary>
        /// Initializes a new instance of the IncompatiblePublishLicenseException class.
        /// </summary>
        public IncompatiblePublishLicenseException() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the IncompatiblePublishLicenseException class with a specified error message.
        /// </summary>
        /// <param name="message"></param>
        public IncompatiblePublishLicenseException(string message) : base(message)
        {}

        /// <summary>
        /// Initializes a new instance of the IncompatiblePublishLicenseException class with a specified error message and a reference to 
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public IncompatiblePublishLicenseException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the IncompatiblePublishLicenseException class with serialized data.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected IncompatiblePublishLicenseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
*/
}
