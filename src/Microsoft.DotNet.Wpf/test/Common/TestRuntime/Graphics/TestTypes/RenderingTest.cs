// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base class for rendering tests
    /// </summary>
    public abstract class RenderingTest : CoreGraphicsTest
    {
        /// <summary>
        /// Test variation initialization
        /// </summary>
        /// <param name="v"></param>
        public override void Init(Variation v)
        {
            base.Init(v);
            string ml = v["matchlevel"];
            colorTolerance = (ml == null) ? 2 : StringConverter.ToDouble(ml);

            if (Variation.NewWindowPerVariation)
            {
                if (window != null)
                {
                    window.Dispose();
                    window = null;
                }
                window = RenderingWindow.Create(v);
                Log("");
                Log("BitDepth detected: {0}", ColorOperations.BitDepth);
            }
            else
            {
                if (window == null)
                {
                    window = RenderingWindow.Create(Variation.GlobalParameters);
                    Log("");
                    Log("BitDepth detected: {0}", ColorOperations.BitDepth);
                }
            }

            // We need to change background color per variation.
            // Global Variation won't have the right value, so there's no sense in doing this in the constructor.
            window.SetBackgroundColor(v.BackgroundColor);
        }

        /// <summary/>
        public abstract Visual GetWindowContent();

        /// <summary/>
        public abstract void Verify();

        /// <summary/>
        public virtual void ModifyWindowContent(Visual v)
        {
            AddFailure("ModifyWindowContent must be overridden if it is to be used at all");
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected virtual RenderingWindow GetNewWindow()
        {
            AddFailure("CreateNewWindow must be overridden if it is to be used at all");

            return null;
        }

        /// <summary/>
        public override void RunTheTest()
        {
            RenderWindowContent();
            VerifyWithinContext();
        }

        /// <summary/>
        public static object Invoke(DispatcherPriority priority, DispatcherOperationCallback callback, object arg)
        {
            EnsureApplication();

            object result = null;

            if (catchExceptionsOnDispatcherThread)
            {
                result = application.Dispatcher.Invoke(priority, callbackWrapper, new Callback(callback, arg));
                if (result is Exception)
                {
                    // We only throw exceptions on the main thread.
                    throw (Exception)result;
                }
            }
            else
            {
                result = application.Dispatcher.Invoke(priority, callback, arg);
            }
            return result;
        }

        private static object CallbackWrapper(object arg)
        {
            Callback method = arg as Callback;
            if (method == null)
            {
                return new ApplicationException("CallbackWrapper requires an argument of type 'Callback'");
            }

            object result = null;
            try
            {
                result = method.Invoke();
            }
            catch (Exception ex)
            {
                // We're going to lose the exception context anyway since this method
                //  is run by the Dispatcher who catches everything...
                // So there's no harm in saving it and returning it to the main thread to be thrown there.

                return ex;
            }
            return result;
        }

        /// <summary/>
        protected int WindowWidth { get { return variation.WindowWidth; } }

        /// <summary/>
        protected int WindowHeight { get { return variation.WindowHeight; } }

        /// <summary/>
        protected Rect ViewportRect { get { return variation.ViewportRect; } }

        /// <summary/>
        protected int DpiScaledWindowWidth { get { return variation.DpiScaledWindowWidth; } }

        /// <summary/>
        protected int DpiScaledWindowHeight { get { return variation.DpiScaledWindowHeight; } }

        /// <summary/>
        protected Color BackgroundColor { get { return variation.BackgroundColor; } }

        /// <summary/>
        protected void RenderWindowContent()
        {
            if (window == null)
            {
                throw new ApplicationException("The rendering window must be set before the Visual can be rendered.\n" +
                                                "(did you override Init and forget to call base.Init?)");
            }

            Invoke(DispatcherPriority.Normal, window.SetContentDelegate, this);
        }

        /// <summary/>
        protected void ModifyWindowContent()
        {
            if (window == null)
            {
                throw new ApplicationException("The rendering window must be set before the Visual can be modified.\n" +
                                                "(did you override Init and forget to call base.Init?)");
            }

            Invoke(DispatcherPriority.Normal, window.ModifyContentDelegate, this);
        }

        /// <summary/>
        protected Color[,] GetScreenCapture()
        {
            if (window == null)
            {
                throw new ApplicationException("The rendering window must be set before a screen capture can be taken.\n" +
                                                "(did you override Init and forget to call base.Init?)");
            }

            // NOTE:
            //      ApplicationIdle is just a fancy way for saying that we can't do anything about the
            //          race condition here.
            //      We do a "CompleteRender" (testhook) when capturing the rendered content, but this
            //          makes extra sure that we have something valid to capture first.

            return (Color[,])Invoke(DispatcherPriority.ApplicationIdle, window.CaptureContentDelegate, null);
        }

        /// <summary/>
        protected void ExchangeWindows()
        {
            DispatcherOperationCallback callback =
                    delegate(object foo)
                    {
                        return GetNewWindow();
                    };
            RenderingWindow newWindow = (RenderingWindow)Invoke(DispatcherPriority.Normal, callback, null);
            if (newWindow != null)
            {
                window.Dispose();
                window = newWindow;
            }
        }

        /// <summary/>
        protected void VerifyWithinContext()
        {
            DispatcherOperationCallback callback =
                    delegate(object foo)
                    {
                        // WaitForCompleteRender seems to be bogus!  The real saviour is ApplicationIdle...
                        Photographer.WaitForCompleteRender();
                        Verify();
                        return null;
                    };
            Invoke(DispatcherPriority.ApplicationIdle, callback, null);
        }

        /// <summary/>
        protected bool ColorsAreCloseEnough(Color c1, Color c2)
        {
            int a = (int)c1.A - (int)c2.A;
            int r = (int)c1.R - (int)c2.R;
            int g = (int)c1.G - (int)c2.G;
            int b = (int)c1.B - (int)c2.B;

            return (Math.Abs(a) <= colorTolerance && Math.Abs(r) <= colorTolerance &&
                     Math.Abs(g) <= colorTolerance && Math.Abs(b) <= colorTolerance);
        }

        private static void EnsureApplication()
        {
            if (application != null)
            {
                return;
            }

            semaphore = new AutoResetEvent(false);

            // Create the Application for our tests to use
            Thread thread = new Thread(new ThreadStart(CreateApplicationThread));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Wait for the application to initialize
            semaphore.WaitOne();
        }

        private static void CreateApplicationThread()
        {
            application = new Application();
            application.Startup += OnStartup;
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            application.Run();
        }

        private static void OnStartup(object sender, StartupEventArgs args)
        {
            semaphore.Set();
        }

        /// <summary/>
        protected double colorTolerance;

        internal static Application application = null;
        internal static AutoResetEvent semaphore = null;
        internal static RenderingWindow window = null;
        internal static bool catchExceptionsOnDispatcherThread = false;
        private static DispatcherOperationCallback callbackWrapper = new DispatcherOperationCallback(CallbackWrapper);

        private class Callback
        {          
            public Callback(DispatcherOperationCallback callback, object arg)
            {
                this.callback = callback;
                this.arg = arg;
            }            

            public object Invoke()
            {
                return callback(arg);
            }
            
            private DispatcherOperationCallback callback;
            private object arg;
        }
    }
}
