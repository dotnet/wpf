// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
// These types are aliased to match the unamanaged names used in interop

namespace System.Windows.Media
{
    abstract partial class ImageSource : Animatable, IFormattable, DUCE.IResource
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ImageSource Clone()
        {
            return (ImageSource)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ImageSource CloneCurrentValue()
        {
            return (ImageSource)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------




        #region Public Properties



        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods





        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods


        internal abstract DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel);

        /// <summary>
        /// AddRefOnChannel
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                return AddRefOnChannelCore(channel);
            }
        }
        internal abstract void ReleaseOnChannelCore(DUCE.Channel channel);

        /// <summary>
        /// ReleaseOnChannel
        /// </summary>
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                ReleaseOnChannelCore(channel);
            }
        }
        internal abstract DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel);

        /// <summary>
        /// GetHandle
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle handle;

            using (CompositionEngineLock.Acquire())
            {
                handle = GetHandleCore(channel);
            }

            return handle;
        }
        internal abstract int GetChannelCountCore();

        /// <summary>
        /// GetChannelCount
        /// </summary>
        int DUCE.IResource.GetChannelCount()
        {
            // must already be in composition lock here
            return GetChannelCountCore();
        }
        internal abstract DUCE.Channel GetChannelCore(int index);

        /// <summary>
        /// GetChannel
        /// </summary>
        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            // must already be in composition lock here
            return GetChannelCore(index);
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties


        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public string ToString(IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            ReadPreamble();
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal virtual string ConvertToString(string format, IFormatProvider provider)
        {
            return base.ToString();
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties



        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields







        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------




        #endregion Constructors
    }
}
