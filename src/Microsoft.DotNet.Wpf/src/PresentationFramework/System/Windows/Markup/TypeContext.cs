// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   class for the main TypeConverterContext object passed to type converters
//

using System;
using System.ComponentModel;
using System.Xml;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    ///<summary>TypeConverterContext class used for parsing Attributes.</summary>
    internal class TypeConvertContext : ITypeDescriptorContext
    {
#region Public

#region Methods

        ///<summary>
        /// OnComponentChange
        ///</summary>
        ///<internalonly>
        /// member is public only because base class has
        /// this public member declared
        ///</internalonly>
        ///<returns>
        /// void
        ///</returns>
        public void OnComponentChanged()
        {
        }

        ///<summary>
        /// OnComponentChanging
        ///</summary>
        ///<internalonly>
        /// member is public only because base class has
        /// this public member declared
        ///</internalonly>
        ///<returns>
        /// void
        ///</returns>
        public bool OnComponentChanging()
        {
            return false;
        }

        ///<summary>
        /// IServiceProvider GetService implementation
        ///</summary>
        ///<param name="serviceType">
        /// Type of Service to be returned
        ///</param>
        ///<internalonly>
        /// member is public only because base class has
        /// this public member declared
        ///</internalonly>
        ///<returns>
        /// Service object or null if service is not found
        ///</returns>
        virtual public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IUriContext))
            {
                return _parserContext as IUriContext;
            }

            // temporary code to optimize Paints.White etc, until this is done
            // in a more generic fashion in SolidPaint ctor
            else if (serviceType == typeof(string))
            {
                return _attribStringValue;
            }

#if PBTCOMPILER
            return null;
#else
            // Check for the other provided services

            ProvideValueServiceProvider serviceProvider = _parserContext.ProvideValueProvider;
            return serviceProvider.GetService( serviceType );
#endif

        }

#endregion Methods

#region Properties

        ///<summary>Container property</summary>
        ///<internalonly>
        /// property is public only because base class has
        /// this public property declared
        ///</internalonly>
        public IContainer Container
        {
            get {return null;}
        }

        ///<summary>Instance property</summary>
        ///<internalonly>
        /// property is public only because base class has
        /// this public property declared
        ///</internalonly>
        public object Instance
        {
            get { return null; }
        }

        ///<summary>Propert Descriptor</summary>
        ///<internalonly>
        /// property is public only because base class has
        /// this public property declared
        ///</internalonly>
        public PropertyDescriptor PropertyDescriptor
        {
            get { return null;}
        }

#if !PBTCOMPILER
        // Make the ParserContext available internally as an optimization.
        public ParserContext ParserContext
        {
            get { return _parserContext; }
        }
#endif

#endregion Properties

#endregion Public

#region Internal

#region Contructors

#if !PBTCOMPILER
        /// <summary>
        ///
        /// </summary>
        /// <param name="parserContext"></param>
        public TypeConvertContext(ParserContext parserContext)
        {
            _parserContext = parserContext;
        }
#endif

        // temporary code to optimize Paints.White etc, until this is done
        // in a more generic fashion in SolidPaint ctor

#if PBTCOMPILER
        /// <summary>
        ///
        /// </summary>
        /// <param name="parserContext"></param>
        /// <param name="originalAttributeValue"></param>
        public TypeConvertContext(ParserContext parserContext, string originalAttributeValue)
        {
            _parserContext = parserContext;
            _attribStringValue = originalAttributeValue;
        }
#endif

#endregion Constructors

#endregion internal

#region Private

#region Data

        ParserContext _parserContext;

        // _attribStringValue is never set when !PBTCOMPILER
        #pragma warning disable 0649
        string _attribStringValue;
        #pragma warning restore 0649

#endregion Data

#endregion Private

    }
}
