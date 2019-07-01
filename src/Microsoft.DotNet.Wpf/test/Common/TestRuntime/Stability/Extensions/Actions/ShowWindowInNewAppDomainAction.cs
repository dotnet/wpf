// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Test.Threading;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{

    public class ShowWindowInNewAppDomainAction : SimpleDiscoverableAction
    {
        public double OccurrenceIndicator { get; set; }
 
        public override bool CanPerform()
        {
            //Do action only when OccurrenceIndicator is less than 0.002. (1/500)
            return OccurrenceIndicator < 0.002;
        }

        public override void Perform()
        {
            Trace.WriteLine("[ShowWindowInNewAppDomainAction]: Creating an AppDomain...");

            InvokeThreadInNewAppDomain();
        }

        private void InvokeThreadInNewAppDomain()
        {
            Thread t = new Thread(delegate() 
                {
                    AppDomainSetup ads = new AppDomainSetup();
                    ads.ApplicationBase = System.Environment.CurrentDirectory;
            
                    AppDomain a = AppDomain.CreateDomain("Temporary Domain", null, ads);

                    CustomMarshalByRefObject worker = (CustomMarshalByRefObject)a.CreateInstanceAndUnwrap(typeof(CustomMarshalByRefObject).Assembly.FullName, typeof(CustomMarshalByRefObject).FullName);

                    // Show a window with button. 
                    worker.ShowWindow();

                    Trace.WriteLine("[ShowWindowInNewAppDomainAction]: Unloading AppDomain...");

                    // leave the new appdomain for 5 second. 
                    DispatcherHelper.DoEvents(5000);

                    // Unload AppDomain
                    AppDomain.Unload(a);

                });

            t.SetApartmentState(ApartmentState.STA);
            t.Name= " temporary thread";
            t.Start();
            t.Join(2000);
        }

        private class CustomMarshalByRefObject: MarshalByRefObject
        {
            public void ShowWindow()
            {
                Window win = new Window();
                Button button = new Button();
                button.Content = "Simple text";
                win.Content = button;
                //make sure that exceptions are not caught by the dispatcher
                Dispatcher.CurrentDispatcher.UnhandledExceptionFilter += delegate(object sender, DispatcherUnhandledExceptionFilterEventArgs e) { e.RequestCatch = false; };

                win.Show();
                DispatcherHelper.DoEvents(100);
                win.Close();

                // Workaround bug 816087 (http://vstfdevdiv:8080/WorkItemTracking/WorkItem.aspx?artifactMoniker=816087). Without shuting down the 
                // Dispatcher, AppDomain.Unload gets CannotUnloadAppDomainException sometimes. 
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }
    }
}
