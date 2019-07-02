// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if CABMINIMAL
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.MiniExtract
#else
namespace Microsoft.Test.Compression.Cab.Mini
#endif
#else
#if CABEXTRACTONLY
namespace Microsoft.Test.Compression.Cab.Extract
#else
namespace Microsoft.Test.Compression.Cab
#endif
#endif
{

using System;
using System.IO;
using System.Globalization;
using System.Security.Permissions;
using System.Runtime.Serialization;

/// <summary>
/// Base class for cabinet exceptions.
/// </summary>
[Serializable]
internal class CabinetException : IOException
{
	internal CabinetException(int err, int errorCode, string msg, Exception innerException) : base(msg, innerException)
	{
		this.error = err;
		this.errorCode = errorCode;
	}

	internal CabinetException(int err, int errorCode, string msg)
		: this(err, errorCode, msg, null) { }

	/// <summary>
	/// Creates a new CabinetException with a specified error message and a reference to the
	/// inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception. If the
	/// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
	/// is raised in a catch block that handles the inner exception.</param>
	public CabinetException(string msg, Exception innerException)
		: this(0, 0, msg, innerException) { }

	/// <summary>
	/// Creates a new CabinetException with a specified error message.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	public CabinetException(string msg)
		: this(0, 0, msg, null) { }

	/// <summary>
	/// Creates a new CabinetException.
	/// </summary>
	public CabinetException()
		: this(0, 0, null, null) { }

	/// <summary>
	/// Initializes a new instance of the CabinetException class with serialized data.
	/// </summary>
	/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
	/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
	protected CabinetException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
		this.error = info.GetInt32("cabError");
		this.errorCode = info.GetInt32("cabErrorCode");
	}

	/// <summary>
	/// Sets the SerializationInfo with information about the exception.
	/// </summary>
	/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
	/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
	[SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("cabError", this.error);
		info.AddValue("cabErrorCode", errorCode);
		base.GetObjectData(info, context);
	}

	/// <summary>
	/// Gets the FCI or FDI cabinet engine error number.
	/// </summary>
	public int Error { get { return error; } }
	private int error;

	/// <summary>
	/// Gets the Win32 error code.
	/// </summary>
	public int ErrorCode { get { return errorCode; } }
	private int errorCode;
}

#if !CABEXTRACTONLY
/// <summary>
/// Exception during creating cabinet files.
/// </summary>
[Serializable]
internal class CabinetCreateException : CabinetException
{
	internal CabinetCreateException(int fciError, int errorCode, string msg, Exception innerException)
		: base(fciError, errorCode, msg != null ? msg : GetFciMessage(fciError, errorCode), innerException) { }
	
	internal CabinetCreateException(int fciError, int errorCode, Exception innerException)
		: this(fciError, errorCode, null, innerException) { }
	
	internal CabinetCreateException(int fciError, int errorCode, string msg)
		: this(fciError, errorCode, msg, null) { }
	
	internal CabinetCreateException(int fciError, int errorCode)
		: this(fciError, errorCode, null, null) { }

	/// <summary>
	/// Creates a new CabinetCreateException with a specified error message and a reference to the
	/// inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception. If the
	/// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
	/// is raised in a catch block that handles the inner exception.</param>
	public CabinetCreateException(string msg, Exception innerException) : this(0, 0, msg, innerException) { }
	
	/// <summary>
	/// Creates a new CabinetCreateException with a specified error message.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	public CabinetCreateException(string msg) : this(0, 0, msg, null) { }
	
	/// <summary>
	/// Creates a new CabinetCreateException.
	/// </summary>
	public CabinetCreateException() : this(0, 0, null, null) { }
	
	/// <summary>
	/// Initializes a new instance of the CabinetCreateException class with serialized data.
	/// </summary>
	/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
	/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
	protected CabinetCreateException(SerializationInfo info, StreamingContext context) : base(info, context) {}

	internal static string GetFciMessage(int fciError, int errorCode)
	{
		string msg = null;
		switch((FCI.ERROR) fciError)
		{
			case FCI.ERROR.OPEN_SRC:       msg = "Failure opening file to be stored in cabinet."; break;
			case FCI.ERROR.READ_SRC:       msg = "Failure reading file to be stored in cabinet."; break;
			case FCI.ERROR.ALLOC_FAIL:     msg = "Out of memory.";                                break;
			case FCI.ERROR.TEMP_FILE:      msg = "Could not create a temporary file.";            break;
			case FCI.ERROR.BAD_COMPR_TYPE: msg = "Unknown compression type.";                     break;
			case FCI.ERROR.CAB_FILE:       msg = "Could not create cabinet file.";                break;
			case FCI.ERROR.USER_ABORT:     msg = "Client requested abort.";                       break;
			case FCI.ERROR.MCI_FAIL:       msg = "Failure compressing data.";                     break;
			default:                       msg = "Unknown error creating cabinet.";               break;
		}
		if(errorCode != 0)
		{
			msg = String.Format(CultureInfo.InvariantCulture, "{0} Error code {1}", msg, errorCode);
		}
		return msg;
	}

	//internal FCI.ERROR FciError { get { return (FCI.ERROR) Error; } }
}
#endif // !CABEXTRACTONLY

/// <summary>
/// Exception during extracting or listing cabinet files.
/// </summary>
[Serializable]
internal class CabinetExtractException : CabinetException
{
	internal CabinetExtractException(int fdiError, int errorCode, string msg, Exception innerException)
		: base(fdiError, errorCode, msg != null ? msg : GetFdiMessage(fdiError, errorCode), innerException) { }

	internal CabinetExtractException(int fdiError, int errorCode, Exception innerException)
		: this(fdiError, errorCode, null, innerException) { }
	
	internal CabinetExtractException(int fdiError, int errorCode)
		: this(fdiError, errorCode, null, null) { }
	
	/// <summary>
	/// Creates a new CabinetExtractException with a specified error message and a reference to the
	/// inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception. If the
	/// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
	/// is raised in a catch block that handles the inner exception.</param>
	public CabinetExtractException(string msg, Exception innerException) : this(0, 0, msg, innerException) { }
	
	/// <summary>
	/// Creates a new CabinetExtractException with a specified error message.
	/// </summary>
	/// <param name="msg">The message that describes the error.</param>
	public CabinetExtractException(string msg) : this(0, 0, msg, null) { }
	
	/// <summary>
	/// Creates a new CabinetExtractException.
	/// </summary>
	public CabinetExtractException() : this(0, 0, null, null) { }
	
	/// <summary>
	/// Initializes a new instance of the CabinetExtractException class with serialized data.
	/// </summary>
	/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
	/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
	protected CabinetExtractException(SerializationInfo info, StreamingContext context) : base(info, context) {}

	internal static string GetFdiMessage(int fdiError, int errorCode)
	{
		string msg = null;
		switch((FDI.ERROR) fdiError)
		{
			case FDI.ERROR.CABINET_NOT_FOUND: msg = "Cabinet not found.";                                   break;
			case FDI.ERROR.NOT_A_CABINET:     msg = "Cabinet file does not have the correct format.";       break;
			case FDI.ERROR.UNKNOWN_CABINET_VERSION: msg = "Cabinet file has an unknown version number.";    break;
			case FDI.ERROR.CORRUPT_CABINET:   msg = "Cabinet file is corrupt.";                             break;
			case FDI.ERROR.ALLOC_FAIL:        msg = "Could not allocate enough memory.";                    break;
			case FDI.ERROR.BAD_COMPR_TYPE:    msg = "Unknown compression type in a cabinet folder.";        break;
			case FDI.ERROR.MDI_FAIL:          msg = "Failure decompressing data from a cabinet file.";      break;
			case FDI.ERROR.TARGET_FILE:       msg = "Failure writing to target file.";                      break;
			case FDI.ERROR.RESERVE_MISMATCH:  msg = "Cabinets in a set do not have the same RESERVE sizes.";break;
			case FDI.ERROR.WRONG_CABINET:     msg = "Cabinet returned on fdintNEXT_CABINET is incorrect.";  break;
			case FDI.ERROR.USER_ABORT:        msg = "Client requested abort.";                              break;
			default:                          msg = "Unknown error extracting cabinet.";                    break;
		}
		if(errorCode != 0)
		{
			msg = String.Format(CultureInfo.InvariantCulture, "{0} Error code {1}", msg, errorCode);
		}
		return msg;
	}

	//internal FDI.ERROR FdiError { get { return (FDI.ERROR) Error; } }
}

}
