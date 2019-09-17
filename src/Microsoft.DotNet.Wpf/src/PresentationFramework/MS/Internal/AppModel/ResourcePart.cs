// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// ResourcePart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
//

using System;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Resources;
using System.IO;
using System.Resources;
using System.Globalization;
using System.Security;

using MS.Internal.Resources;
using MS.Internal;

//In order to avoid generating warnings about unknown message numbers and 
//unknown pragmas when compiling your C# source code with the actual C# compiler, 
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691


namespace MS.Internal.AppModel
{
    /// <summary>
    /// ResourcePart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
    /// </summary>
    internal class ResourcePart : System.IO.Packaging.PackagePart
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        public ResourcePart(Package container, Uri uri, string name, ResourceManagerWrapper rmWrapper) :
            base(container, uri)
        {
            if (rmWrapper == null)
            {
                throw new ArgumentNullException("rmWrapper");
            }

            _rmWrapper.Value = rmWrapper;
            _name = name;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        protected override Stream GetStreamCore(FileMode mode, FileAccess access)
        {
            Stream stream = null;

            stream = EnsureResourceLocationSet();
            // in order to find the resource we might have to open a stream.
            // rather than waste the stream it is returned here and we can use it.
            if (stream == null)
            {
                // Start looking for resources using the current ui culture.
                // The resource manager will fall back to invariant culture automatically.
                stream = _rmWrapper.Value.GetStream(_name);

                if (stream == null)
                {
                    throw new IOException(SR.Get(SRID.UnableToLocateResource, _name));
                }
            }

            // 
            // If this is a Baml stream, it will return BamlStream object, which contains
            // both raw stream and the host assembly.
            //
            ContentType curContent = new ContentType(ContentType);

            if (MimeTypeMapper.BamlMime.AreTypeAndSubTypeEqual(curContent))
            {
                BamlStream bamlStream = new BamlStream(stream, _rmWrapper.Value.Assembly);

                stream = bamlStream;
            }

            return stream;
        }

        protected override string GetContentTypeCore()
        {
            EnsureResourceLocationSet();

            return MS.Internal.MimeTypeMapper.GetMimeTypeFromUri(new Uri(_name, UriKind.RelativeOrAbsolute)).ToString();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private Stream EnsureResourceLocationSet()
        {
            Stream stream = null;

            lock (_globalLock)
            {
                // only need to do this once
                if (_ensureResourceIsCalled)
                {
                    return null;
                }
                _ensureResourceIsCalled = true;

                try
                {
                    // We do not allow the use of .baml in any Avalon public APIs. This is the code pass needed to go through for loading baml file.
                    // Throw here we will catch all those cases.
                    if (String.Compare(Path.GetExtension(_name), ResourceContainer.BamlExt, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        throw new IOException(SR.Get(SRID.UnableToLocateResource, _name));
                    }

                    if (String.Compare(Path.GetExtension(_name), ResourceContainer.XamlExt, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // try baml extension first since it's our most common senario.
                        string newName = Path.ChangeExtension(_name, ResourceContainer.BamlExt);

                        // Get resource from resource manager wrapper.
                        stream = _rmWrapper.Value.GetStream(newName);
                        if (stream != null)
                        {
                            // Remember that we have .baml for next time GetStreamCore is called.
                            _name = newName;
                            return stream;
                        }
                    }
                }
#pragma warning disable 6502 // PRESharp - Catch statements should not have empty bodies
                catch (System.Resources.MissingManifestResourceException)
                {
                    // When the main assembly doesn't contain any resource (all the resources must come from satellite assembly)
                    // then above GetStream( ) just throws exception. We should catch this exception here and let the code continue 
                    // to try with the original file name.

                    // If the main assembly does contain resource, but the resource with above _name does't exist, the above GetStream( )
                    // just returns null without exception.
                }
#pragma warning restore 6502

            }

            // Do not attempt to load the original file name here.  If the .baml does not exist or if this resource not
            // .xaml or .baml then we will follow the normal code path to attempt to load the stream using the original name.

            return null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private SecurityCriticalDataForSet<ResourceManagerWrapper> _rmWrapper;
        private bool _ensureResourceIsCalled = false;
        private string _name;
        private Object _globalLock = new Object();

        #endregion Private Members
    }
}

