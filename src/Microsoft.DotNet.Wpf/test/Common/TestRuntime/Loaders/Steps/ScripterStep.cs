// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;
using S = Microsoft.Test.Utilities.Scripter;

namespace Microsoft.Test.Utilities.StepsEngine
{
  /// <summary>
  /// Directly launches "Scripter" script
  /// </summary>
  public class ScripterStep : LoaderStep
  {
    #region Public members
      /// <summary>
      /// Name of the script file to execute
      /// </summary>
      public string FileName = "";
    #endregion

    #region Step Implementation
    /// <summary>
    /// Modifies Deployment manifest based on "method" property 
    /// </summary>
    public override bool DoStep()
    {
        if (FileName == "")
        {
            throw new InvalidOperationException("Must specify a file name for for ScripterStep!");
        }

        ILog log = null;
        FileStream f = null;
        try
        {
            // create a log
            log = LogFactory.Create();
            log.StatusMessage = "******************************************************************";
            log.StatusMessage = "*** ScripterStep Starting... *************************************";
            log.StatusMessage = "******************************************************************";

            // get a stream to the script file
            f = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            S.Scripter script = new S.Scripter(f, log);
        }
        catch (Exception e)
        {
            log.FailMessage = "ScripterStep hit an exception! \n" + e.Message.ToString() + "\n\n" + e.StackTrace;
        }

        return true;
    }
    
    #endregion

  }

}
