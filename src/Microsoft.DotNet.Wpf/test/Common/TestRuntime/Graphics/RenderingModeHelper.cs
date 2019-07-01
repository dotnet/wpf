// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Reflection;
using System.Globalization;


namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// Helper class to To set rendering mode. 
    /// </summary>
    public static class RenderingModeHelper
    {
        /// <summary>
        /// Set RenderingMode of the Window contains the dependencyObject 
        /// to Default, Hardware, Software, or HardwareReference. 
        /// Hacky: SetRenderingMode method was removed with changeset  1280265. So, simple reflection on internal method
        /// HwndTarget.SetRenderingMode doesn't work anymore. We need to call SetRenderingMode method on 
        /// System.Windows.Media.Composition.DUCE+CompositionTarget.SetRenderingMode instead.
        /// </summary>        
        public static void SetRenderingMode(DependencyObject dependencyObject, RenderingMode mode)
        {
            Window window = Window.GetWindow(dependencyObject);
            if (window == null)
            {
                throw new ArgumentException("dependencyObject should be in a active window.");
            }

            //Get Source
            WindowInteropHelper winHelper = new WindowInteropHelper(window);
            HwndSource source = HwndSource.FromHwnd(winHelper.Handle);

            //Get HwndTarget of the window.
            HwndTarget hwndTarget = source.CompositionTarget;

            SetRenderingMode(hwndTarget, mode.ToString());
        }

        /// <summary>
        /// Set RenderingMode of a HwndTarget, to a mode defined with a string 
        /// </summary>
        /// <param name="hwndTarget">HwndTarget</param>
        /// <param name="renderingMode">RenderingMode string</param>
        public static void SetRenderingMode(HwndTarget hwndTarget, string mode)
        {
            object renderingMode = ParseRenderingMode(mode);
            SetRenderingMode(hwndTarget, renderingMode);
        }

        /// <summary>
        /// Set RenderingMode of a HwndTarget, to a object represent RenderingMode enum defined in presentation core.
        /// This is to simulate code in HwndTarget.InvalidateRenderMode(), to set RenderingMode. 
        /// </summary>
        /// <param name="hwndTarget">HwndTarget</param>
        /// <param name="renderingMode">RenderingMode</param>
        private static void SetRenderingMode(HwndTarget hwndTarget, object renderingMode)
        {
            Type hwndTargetType = typeof(HwndTarget);
            Assembly assembly = Assembly.GetAssembly(hwndTargetType);

            object channel = GetChannel(assembly, hwndTarget);

            // Simulate DUCE.CompositionTarget.SetRenderingMode(
            //    _compositionTarget.GetHandle(channel),
            //    (MILRTInitializationFlags)mode,
            //    channel);
            Type duceCompositionTargetType = AssertAndGetType(assembly, "System.Windows.Media.Composition.DUCE+CompositionTarget");

            object handle = GetHandleOfChannel(assembly, hwndTarget, channel);
            MethodInfo setRenderingModeMethod = AssertAndGetMethod(duceCompositionTargetType, "SetRenderingMode", BindingFlags.NonPublic | BindingFlags.Static);
            setRenderingModeMethod.Invoke(null, new object[] { handle, renderingMode, channel });
        }

        // Simulate _compositionTarget.GetHandle(channel) in HwndTarget. 
        private static object GetHandleOfChannel(Assembly a, HwndTarget hwndTarget, object channel)
        {
            FieldInfo _compositionTargetField = AssertAndGetField(typeof(HwndTarget), "_compositionTarget", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

            object _compositionTarget = _compositionTargetField.GetValue(hwndTarget);
            Type _compositionTargeType = _compositionTarget.GetType();


            MethodInfo getHandleMethod = AssertAndGetMethod(_compositionTargeType, "GetHandle", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            object handle = getHandleMethod.Invoke(_compositionTarget, new object[] { channel });

            return handle;
        }

        private static object GetChannel(Assembly a, HwndTarget hwndTarget)
        {
            // Simulate DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            System.Windows.Threading.Dispatcher dispatcher = hwndTarget.Dispatcher;

            Type mediaContextType = AssertAndGetType(a, "System.Windows.Media.MediaContext");


            MethodInfo fromMethod = AssertAndGetMethod(mediaContextType, "From", BindingFlags.NonPublic | BindingFlags.Static);

            object mediaContext = fromMethod.Invoke(null, new object[] { dispatcher });

            MethodInfo getChannels = AssertAndGetMethod(mediaContextType, "GetChannels", BindingFlags.NonPublic | BindingFlags.Instance);

            object Channels = getChannels.Invoke(mediaContext, new object[] { });

            // Simulate DUCE.Channel channel = channelSet.Channel;
            Type channelSet = AssertAndGetType(a, "System.Windows.Media.Composition.DUCE+ChannelSet");

            FieldInfo channelField = AssertAndGetField(channelSet, "Channel", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            object channel = channelField.GetValue(Channels);

            return channel;
        }


        //Transform RenderingMode we defined to enum defined in PresentationCore. 
        private static object ParseRenderingMode(string mode)
        {
            Assembly assembly = typeof(HwndTarget).Assembly;

            Type modeType = AssertAndGetType(assembly, "System.Windows.Interop.RenderingMode");

            object modeEnum;
            try
            {
                modeEnum = Enum.Parse(modeType, mode);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "enum type not support : {0}. Please check class System.Windows.Interop.RenderingMode.\n{1}", mode, e.ToString()));
            }

            return modeEnum;
        }

        // Assert the existence of a field and get the FieldInfo
        private static FieldInfo AssertAndGetField(Type type, string fieldName, BindingFlags binding)
        {
            FieldInfo field = type.GetField(fieldName, binding);
            if (field == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Field {0} not found in type {1}.", fieldName, type.FullName));
            }
            return field;
        }

        // Assert the existence of a method and get the MethodInfo
        private static MethodInfo AssertAndGetMethod(Type type, string methodName, BindingFlags binding)
        {
            MethodInfo method = type.GetMethod(methodName, binding);
            if (method == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Method {0} not found in type {1}.", methodName, type.FullName));
            }
            return method;
        }

        // Assert the existence of a type and get the Type
        private static Type AssertAndGetType(Assembly a, string typeName)
        {
            Type type = a.GetType(typeName);

            if (type == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Type {0} not found in Assembly {1}.", typeName, a.FullName));
            }
            return type;
        }
    }
}
