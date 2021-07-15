// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Manages Win32 proxies

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.Automation
{
    internal sealed class ProxyManager
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Static class - Private constructor to prevent creation
        private ProxyManager()
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        #region Proxy registration and table management

        // load proxies from specified assembly
        internal static void RegisterProxyAssembly ( AssemblyName assemblyName )
        {
            Assembly a = null;
            try
            {
                a = Assembly.Load( assemblyName );
            }
            catch(System.IO.FileNotFoundException)
            {
                throw new ProxyAssemblyNotLoadedException(SR.Get(SRID.Assembly0NotFound,assemblyName));
            } 
            
            string typeName = assemblyName.Name + ".UIAutomationClientSideProviders";
            Type t = a.GetType( typeName );
            if( t == null )
            {
                throw new ProxyAssemblyNotLoadedException(SR.Get(SRID.CouldNotFindType0InAssembly1, typeName, assemblyName));
            }

            FieldInfo fi = t.GetField("ClientSideProviderDescriptionTable", BindingFlags.Static | BindingFlags.Public);
            if (fi == null || fi.FieldType !=  typeof(ClientSideProviderDescription[]))
            {
                throw new ProxyAssemblyNotLoadedException(SR.Get(SRID.CouldNotFindRegisterMethodOnType0InAssembly1, typeName, assemblyName));
            }

            ClientSideProviderDescription[] table = fi.GetValue(null) as ClientSideProviderDescription[];
            if (table != null)
            {
                ClientSettings.RegisterClientSideProviders(table);
            }
        } 
        
        // register specified proxies
        internal static void RegisterWindowHandlers(ClientSideProviderDescription[] proxyInfo)
        {
            // If a client registers a proxy before the defaults proxies are loaded because of use, 
            // we should load the defaults first.
            LoadDefaultProxies();
            
            lock (_lockObj)
            {
                AddToProxyDescriptionTable( proxyInfo );
            }
        }

        // set proxy table to specified array, clearing any previously registered proxies
        internal static void SetProxyDescriptionTable(ClientSideProviderDescription[] proxyInfo)
        {
            lock (_lockObj)
            {
                // This method replaces the entire table.  So clear all the collections
                for( int i = 0 ; i < _pseudoProxies.Length ; i++ )
                {
                    _pseudoProxies[ i ] = null;
                }

                _classHandlers.Clear(); 
                _partialClassHandlers.Clear();
                _imageOnlyHandlers.Clear();
                _fallbackHandlers.Clear();

                AddToProxyDescriptionTable( proxyInfo );

                // if someone calls this method before the default proxies are 
                // loaded assume they don't want us to add the defaults on top 
                // of the ones they just put into affect here
                _defaultProxiesNeeded = false;
            }
        }

        // return an array representing the currently registered proxies
        internal static ClientSideProviderDescription[] GetProxyDescriptionTable()
        {
            // the ClientSideProviderDescription table is split into four different collections.  Bundle them all back 
            // together to let them be manipulated

            // If a client gets the table before the defaults proxies  are loaded because of use, it should return the default proxies
            LoadDefaultProxies();
            
            lock (_lockObj)
            {
                int count = 0;
                IEnumerable [ ] sourceProxyDescription = {_classHandlers, _partialClassHandlers, _imageOnlyHandlers, _fallbackHandlers};

                // figure out how many there are
                foreach ( IEnumerable e in sourceProxyDescription )
                {
                    foreach ( Object item in e )
                    {
                        Object o = item;
                        if( o is DictionaryEntry )
                            o = ((DictionaryEntry)o).Value;

                        if (o is ClientSideProviderDescription)
                        {
                            count++;
                        }
                        else if (o is ClientSideProviderFactoryCallback)
                        {
                            count++;
                        }
                        else
                        {
                            count += ((ArrayList)o).Count;
                        }
                    }
                }

                ClientSideProviderDescription[] proxyDescriptions = new ClientSideProviderDescription[count];
                count = 0;
                
                // Because the four collections have a simular stucture in common we can treat like they are the same 
                // and build the array in the correct order from each one.
                foreach ( IEnumerable e in sourceProxyDescription )
                {
                    foreach ( Object item in e )
                    {
                        Object o = item;
                        if( o is DictionaryEntry )
                            o = ((DictionaryEntry)o).Value;

                        if (o is ClientSideProviderDescription)
                        {
                            proxyDescriptions[count++] = (ClientSideProviderDescription)o;
                        }
                        else if (o is ClientSideProviderFactoryCallback)
                        {
                            ClientSideProviderFactoryCallback pfc = (ClientSideProviderFactoryCallback)o;
                            proxyDescriptions[count++] = new ClientSideProviderDescription(pfc, null);

                        }
                        else
                        {
                            foreach( Object o1 in (ArrayList) o )
                            {
                                proxyDescriptions[count++] = (ClientSideProviderDescription)o1;
                            }
                        }
                    }
                }
                
                return proxyDescriptions;
            }            
        }
        
        #endregion Proxy registration and table management

        #region Methods that return a proxy or native object

        // helper to return the non-client area provider
        internal static IRawElementProviderSimple GetNonClientProvider( IntPtr hwnd )
        {
            ClientSideProviderFactoryCallback nonClientFactory = ProxyManager.NonClientProxyFactory;
            if( nonClientFactory == null )
                return null;

            return nonClientFactory( hwnd, 0, UnsafeNativeMethods.OBJID_CLIENT );
        }

        // helper to return the User32FocusedMenu provider
        internal static IRawElementProviderSimple GetUser32FocusedMenuProvider( IntPtr hwnd )
        {
            ClientSideProviderFactoryCallback menuFactory = ProxyManager.User32FocusedMenuProxyFactory;
            if( menuFactory == null )
                return null;

            return menuFactory( hwnd, 0, UnsafeNativeMethods.OBJID_CLIENT );
        }

        #endregion Methods that return a proxy or native object

        #region miscellaneous HWND rountines
        internal static string GetClassName( NativeMethods.HWND hwnd )
        {
            StringBuilder str = new StringBuilder( NativeMethods.MAX_PATH );

            int result = SafeNativeMethods.GetClassName(hwnd, str, NativeMethods.MAX_PATH);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                Misc.ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return str.ToString();
        }

        internal static string RealGetWindowClass( NativeMethods.HWND hwnd )
        {
            StringBuilder str = new StringBuilder( NativeMethods.MAX_PATH );

            int result = SafeNativeMethods.RealGetWindowClass(hwnd, str, NativeMethods.MAX_PATH);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                Misc.ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return str.ToString();
        }

        private static string [] BadImplClassnames = new string []
        {
            // The following classes are known to not check the lParam to WM_GETOBJECT, so avoid them:
            // Keep list in sync with UiaNodeFactory.cpp
            "TrayClockWClass",
            "REListBox20W",
            "REComboBox20W",
            "WMP Skin Host",
            "CWmpControlCntr",
            "WMP Plugin UI Host",
        };

        internal static bool IsKnownBadWindow( NativeMethods.HWND hwnd )
        {
            string className = GetClassName( hwnd );

            foreach (string str in BadImplClassnames)
            {
                if (String.Compare(className, str, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }


            // Check for problem minimized WMP window: this class is only used
            // when WMP10 is minimized into the task bar. It's an ATL:... class,
            // so have to check via parent. Structure looks like:
            // ...
            // ReBarWindow32 - taskbar rebar window
            //   WMP9DeskBand
            //     WMP9ActiveXHost122294984
            //       ATL::0754F282     <- this is the bad one

            NativeMethods.HWND hwndParent = SafeNativeMethods.GetAncestor(hwnd, SafeNativeMethods.GA_PARENT);
            if (hwndParent != NativeMethods.HWND.NULL)
            {
                string parentClassName = GetClassName(hwndParent);
                if (parentClassName.StartsWith("WMP9ActiveXHost", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // find the name of the image for this HWND.  If this fails for any reason just return null. 
        // Review: Getting the image name is expessive if the image name starts to be used a lot
        // we could cache it in a static hash using the PID as the key.
        internal static string GetImageName( NativeMethods.HWND hwnd )
        {
            int instance = Misc.GetWindowLong(hwnd, SafeNativeMethods.GWL_HINSTANCE);
            if ( instance == 0 )
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH);
            using (SafeProcessHandle processHandle = new SafeProcessHandle(hwnd))
            {
                if (processHandle.IsInvalid)
                {
                    return null;
                }

                if (Misc.GetModuleFileNameEx(processHandle, (IntPtr)instance, sb, NativeMethods.MAX_PATH) == 0)
                {
                    return null;
                }
            }

            return System.IO.Path.GetFileName(sb.ToString().ToLower(CultureInfo.InvariantCulture));
        }
        #endregion miscellaneous HWND rountines

        internal static void LoadDefaultProxies( )
        {
            //No need to load the default providers if they are already loaded
            if (!_defaultProxiesNeeded)
                return;

            // set this bool before we even know that the default proxies were loaded because if there was
            // a problem we don't want to go thru the following overhead for every hwnd.  We just try this
            // once per process.
            _defaultProxiesNeeded = false;
            
#if (INTERNAL_COMPILE || INTERNALTESTUIAUTOMATION)
            ClientSettings.RegisterClientSideProviders(UIAutomationClientsideProviders.UIAutomationClientSideProviders.ClientSideProviderDescriptionTable);
#else
            Assembly callingAssembly = null;

            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            // Walk up the stack looking for the first assembly that is different than the ExecutingAssembly
            // This would be the assembly that called us.
            StackTrace st = new StackTrace(); 
            for ( int i=0; i < st.FrameCount; i++ ) 
            {
                StackFrame sf = st.GetFrame(i);
                MethodBase mb = sf.GetMethod();
                Type t = mb.ReflectedType;
                Assembly a = t.Assembly;

                if ( a.GetName().Name != currentAssembly.GetName().Name )
                {
                    callingAssembly = a;
                    break;
                }
            }

            AssemblyName ourAssembly = Assembly.GetAssembly(typeof(ProxyManager)).GetName();
            
            // Attempt to discover the version of UIA that the caller is linked against,
            // and then use the correpsonding proxy dll version. If we can't do that,
            // we'll use the default version.
            AssemblyName proxyAssemblyName = new AssemblyName();
            proxyAssemblyName.Name = _defaultProxyAssembly;
            proxyAssemblyName.Version = ourAssembly.Version;
            proxyAssemblyName.CultureInfo = ourAssembly.CultureInfo;
            proxyAssemblyName.SetPublicKeyToken( ourAssembly.GetPublicKeyToken() );

            if ( callingAssembly != null )
            {
                // find the name of the UIAutomation dll referenced by this assembly because it my be different
                // from the one that acually got loaded.  We want to load the proxy dll that matches this one.
                // This simulates behave simular to fusion side-by-side.
                AssemblyName assemblyName = new AssemblyName();
                foreach ( AssemblyName name in callingAssembly.GetReferencedAssemblies() )
                {
                    if ( name.Name == ourAssembly.Name )
                    {
                        assemblyName = name;
                        break;
                    }
                }

                if ( assemblyName.Name != null )
                {
                    proxyAssemblyName.Version = assemblyName.Version;
                    proxyAssemblyName.CultureInfo = assemblyName.CultureInfo;
                    proxyAssemblyName.SetPublicKeyToken( assemblyName.GetPublicKeyToken() );
                }
            }

            RegisterProxyAssembly( proxyAssemblyName );
#endif
        }
        #endregion Internal Methods
        


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Disable use of default proxies.  
        // This is used to stop the proxies frrom being loaded on the server.
        internal static void DisableDefaultProxies()
        {
            _defaultProxiesNeeded = false;
        }


        internal static ClientSideProviderFactoryCallback NonClientProxyFactory
        {
            get
            {
                return _pseudoProxies[ (int)PseudoProxy.NonClient ];
            }
        }

        internal static ClientSideProviderFactoryCallback NonClientMenuBarProxyFactory
        {
            get
            {
                return _pseudoProxies[ (int)PseudoProxy.NonClientMenuBar ];
            }
        }

        internal static ClientSideProviderFactoryCallback NonClientSysMenuProxyFactory
        {
            get
            {
                return _pseudoProxies[ (int)PseudoProxy.NonClientSysMenu ];
            }
        }

        internal static ClientSideProviderFactoryCallback User32FocusedMenuProxyFactory
        {
            get
            {
                return _pseudoProxies[ (int)PseudoProxy.User32FocusedMenu ];
            }
        }


        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // Get a proxy for a given hwnd
        // (Also used as an entry by RESW to get proxy for a parent to check if it supports overrides)
        internal static IRawElementProviderSimple ProxyProviderFromHwnd(NativeMethods.HWND hwnd, int idChild, int idObject)
        {
            // The precedence that proxies are chosen is as follows:
            //
            // All entries in the table passed into RegisterWindowHandlers that do not specify AllowSubstringMatch 
            // are tried first.  If the class name of the current hwnd matches the ClassName in the entery then 
            // the image name is checked for a match if it was specified.  
            //
            // If no match is found then the real class name is checked for a match unless NoBaseMatching flag is on.
            // this allows class name ThunderRT6CommandButton to match Button becuse it subclasses button.
            // If more than one entry has the same ClassName the first one in the table is tried first.
            //
            // If no exact match if found, all the entries that specified AllowSubstringMatch are tried in the order 
            // they occur in the table.  If a match is found and the ImageName was specified that is checked to see 
            // if it matches the current image.
            //       
            // If no substring matches are found entries that have specifed only the ImageName are tried.
            //
            // If there still is no match entries that have no ClassName and no ImageName are tried. 
            //
            // If this fails the default hwnd proxy is used.
            //
            // If RegisterWindowHandlers is called again those entries occur before the earlier ones in the table.
            if (hwnd == NativeMethods.HWND.NULL)
            {
                return null;
            }

            LoadDefaultProxies ();

            string className = GetClassName (hwnd).ToLower (CultureInfo.InvariantCulture);
            object proxyDescOrArrayList = null;

            lock (_lockObj)
            {
                proxyDescOrArrayList = _classHandlers[className];
            }
            string imageName = null;
            IRawElementProviderSimple proxy = FindProxyInEntryOrArrayList(ProxyScoping.ExactMatchApparentClassName, proxyDescOrArrayList, ref imageName, hwnd, idChild, idObject, null);

            // If we don't have a proxy for the class try to match the real class 
            string baseClassName = null;
            if (proxy == null)
            {
                baseClassName = GetBaseClassName(hwnd);
                if (baseClassName == className)
                    baseClassName = null;

                if (!String.IsNullOrEmpty(baseClassName))
                {
                    lock (_lockObj)
                    {
                        proxyDescOrArrayList = _classHandlers[baseClassName];
                    }
                    proxy = FindProxyInEntryOrArrayList(ProxyScoping.ExactMatchRealClassName, proxyDescOrArrayList, ref imageName, hwnd, idChild, idObject, null);
                }
            }

            // If we don't have a proxy yet look for a partial match if there are any
            if (proxy == null && _partialClassHandlers.Count > 0)
            {
                proxy = FindProxyInEntryOrArrayList(ProxyScoping.PartialMatchApparentClassName, _partialClassHandlers, ref imageName, hwnd, idChild, idObject, className);

                if (proxy == null && !String.IsNullOrEmpty(baseClassName))
                {
                    proxy = FindProxyInEntryOrArrayList(ProxyScoping.PartialMatchRealClassName, _partialClassHandlers, ref imageName, hwnd, idChild, idObject, baseClassName);
                }
            }
            
            // There is no match yet look for entry that just specified an image name
            // this is like a fallback proxy for a particular image
            if( proxy == null )
            {
                proxy = FindProxyFromImageFallback(ref imageName, hwnd, idChild, idObject);
            }

            // use the fallback proxy if there is one               
            if (proxy == null)
            {
                proxy = FindProxyInEntryOrArrayList(ProxyScoping.FallbackHandlers, _fallbackHandlers, ref imageName, hwnd, idChild, idObject, null);
            }

            // may be null if no proxy found
            return proxy;
        }


        static private IRawElementProviderSimple FindProxyFromImageFallback(ref string imageName, NativeMethods.HWND hwnd, int idChild, int idObject)
        {
            int count;
            lock (_lockObj)
            {
                count = _imageOnlyHandlers.Count;
            }

            // if there is no _imageOnlyHandlers registered there is no need to look
            if (count > 0)
            {
                // Null and Empty string mean different things here.
#pragma warning suppress 6507
                if (imageName == null)
                    imageName = GetImageName(hwnd);

                // Null and Empty string mean different things here.
#pragma warning suppress 6507
                if (imageName != null)
                {
                    object entryOrArrayList;
                    lock (_lockObj)
                    {
                        entryOrArrayList = _imageOnlyHandlers[imageName];
                    }
                    return FindProxyInEntryOrArrayList(ProxyScoping.ImageOnlyHandlers, entryOrArrayList, ref imageName, hwnd, idChild, idObject, null);
                }
            }

            return null;
        }


        // Given a single entry or arraylist, check if it or each object in it matches.
        // This just handles the arraylist iteration, and calls through to GetProxyFromEntry to do the actual entry checking.
        static private IRawElementProviderSimple FindProxyInEntryOrArrayList(ProxyScoping findType, object entryOrArrayList, ref string imageName, NativeMethods.HWND hwnd, int idChild, int idObject, string classNameForPartialMatch)
        {
            if (entryOrArrayList == null)
                return null;

            ArrayList array = entryOrArrayList as ArrayList;
            if (array == null)
            {
                return GetProxyFromEntry(findType, entryOrArrayList, ref imageName, hwnd, idChild, idObject, classNameForPartialMatch);
            }

            // This the array will only grow in size, it will not shrink. That is the reason why it is 
            // safe to capture the count outside the loop.  The reference we have to the Arraylist is
            // kind of like a snapshot.
            int count;
            lock (_lockObj)
            {
                count = array.Count;
            }

            IRawElementProviderSimple proxy = null;

            // this is a for loop because we need this to be thread safe and ClientSideProviderFactoryCallback calls out
            // so there would have been a lock in force when the call out was made which causes
            // deadlock.  We need to make our locks as narrow as possible.
            for( int i = 0; i < count; i++ )
            {
                object entry;
                lock (_lockObj)
                {
                    entry = array[i];
                }

                proxy = GetProxyFromEntry(findType, entry, ref imageName, hwnd, idChild, idObject, classNameForPartialMatch);
                if( proxy != null )
                    break;
            }

            return proxy;
        }

        // Given an entry from one of the hash-tables or lists, check if it matches the image/classname, and if so, call the
        // factory method to create the proxy.
        // (Because full classname matching is done via hash-table lookup, this only needs to do string comparisons
        // for partial classname matches.)
        static private IRawElementProviderSimple GetProxyFromEntry(ProxyScoping findType, object entry, ref string imageName, NativeMethods.HWND hwnd, int idChild, int idObject, string classNameForPartialMatch)
        {
            // First, determine if the entry matches, and if so, extract the factory callback...
            ClientSideProviderFactoryCallback factoryCallback = null;

            // The entry may be a ClientSideProviderFactoryCallback or ClientSideProviderDescription...
            if (findType == ProxyScoping.ImageOnlyHandlers || findType == ProxyScoping.FallbackHandlers)
            {
                // Handle the fallback and image cases specially. The array for these is an array
                // of ClientSideProviderFactoryCallbacks, not ClientSideProviderDescription.
                factoryCallback = (ClientSideProviderFactoryCallback)entry;
            }
            else
            {
                // Other cases use ClientSideProviderDescription...
                ClientSideProviderDescription pi = (ClientSideProviderDescription)entry;

                // Get the image name if necessary...
#pragma warning suppress 6507 // Null and Empty string mean different things here.
                if (imageName == null && pi.ImageName != null)
                {
                    imageName = GetImageName(hwnd);
                }

                if (pi.ImageName == null || pi.ImageName == imageName)
                {
                    // Check if we have a match for this entry...
                    switch (findType)
                    {
                        case ProxyScoping.ExactMatchApparentClassName:
                            factoryCallback = pi.ClientSideProviderFactoryCallback;
                            break;

                        case ProxyScoping.ExactMatchRealClassName:
                            if ((pi.Flags & ClientSideProviderMatchIndicator.DisallowBaseClassNameMatch) == 0)
                            {
                                factoryCallback = pi.ClientSideProviderFactoryCallback;
                            }
                            break;

                        case ProxyScoping.PartialMatchApparentClassName:
                            if (classNameForPartialMatch.IndexOf(pi.ClassName, StringComparison.Ordinal) >= 0)
                            {
                                factoryCallback = pi.ClientSideProviderFactoryCallback;
                            }
                            break;

                        case ProxyScoping.PartialMatchRealClassName:
                            if (classNameForPartialMatch.IndexOf(pi.ClassName, StringComparison.Ordinal) >= 0
                                && ((pi.Flags & ClientSideProviderMatchIndicator.DisallowBaseClassNameMatch) == 0))
                            {
                                factoryCallback = pi.ClientSideProviderFactoryCallback;
                            }
                            break;

                        default:
                            Debug.Assert(false, "unexpected switch() case:");
                            break;
                    }
                }
            }

            // Second part: did we get a match? If so, use the factory callback to obtain an instance...
            if (factoryCallback == null)
                return null;

            // if we get an exception creating a proxy just don't create the proxy and let the UIAutomation default proxy be used
            // This will still allow the tree to be navigated  and some properties to be made availible.
            // Catching all exceptions here doesn't follow .NET guidelines, but is it ok in this scenario?
            try
            {
                return factoryCallback(hwnd, idChild, idObject);
            }
            catch( Exception e )
            {
                if( Misc.IsCriticalException( e ) )
                    throw;

                return null;
            }
        }

        private static void AddToProxyDescriptionTable(ClientSideProviderDescription[] proxyInfo)
        {
            ClientSideProviderDescription pi;

            // the array that is passed in may have the same className occuring more than once in the table.
            // The way this works it the first occurence in the array is giver the first chance to return a valid
            // proxy.  In order to make that work we go through the array backwards so the the entries first
            // in the table get inserted in front of the ones that came later.  This also works if 
            // RegisterWindowHandlers is called more than once.
            for( int i = proxyInfo.Length - 1;  i >= 0; i-- )
            {
                pi = proxyInfo[i];

                // Check for pseudo-proxy names...
                if( pi.ClassName != null && pi.ClassName.Length > 0 && pi.ClassName[ 0 ] == '#' )
                {
                    for( int j = 0 ; j < _pseudoProxyClassNames.Length ; j++ )
                    {
                        if( pi.ClassName.Equals( _pseudoProxyClassNames[ j ] ) )
                        {
                            if( pi.ImageName != null || pi.Flags != 0 )
                            {
                                throw new ArgumentException(SR.Get(SRID.NonclientClassnameCannotBeUsedWithFlagsOrImagename));
                            }

                            _pseudoProxies[j] = pi.ClientSideProviderFactoryCallback;
                            break;
                        }
                    }
                    // fall through to add to table as usual, that ensures that it appears in a 'get' operation.
                }

                if( pi.ClassName == null && pi.ImageName == null )
                {
                    _fallbackHandlers.Insert(0, pi.ClientSideProviderFactoryCallback);
                }
                else if ( pi.ClassName == null )
                {
                    AddToHashTable(_imageOnlyHandlers, pi.ImageName, pi.ClientSideProviderFactoryCallback);
                }
                else if ((pi.Flags & ClientSideProviderMatchIndicator.AllowSubstringMatch) != 0)
                {
                    _partialClassHandlers.Insert( 0, pi );
                }
                else
                {
                    AddToHashTable( _classHandlers, pi.ClassName, pi );
                }
            }
        }
        
        private static void AddToHashTable( Hashtable table, string key, object data )
        {
            object o = table[ key ];
            if( o == null )
            {
                table.Add( key, data );
            }
            else
            {
                ArrayList l = o as ArrayList;
                if( l == null )
                {
                    l = new ArrayList();
                    l.Insert( 0, o );
                }
                l.Insert( 0, data );

                table[ key ] = l;
            }
        }


        // find the name of the base class for this HWND.  If this fails for any reason just return null. 
        private static string GetBaseClassName( NativeMethods.HWND hwnd )
        {
            const int OBJID_QUERYCLASSNAMEIDX = unchecked(unchecked((int)0xFFFFFFF4));
            const int QUERYCLASSNAME_BASE = 65536;

            if( IsKnownBadWindow( hwnd ) )
            {
                return RealGetWindowClass( hwnd ).ToLower( CultureInfo.InvariantCulture );
            }

            IntPtr result = Misc.SendMessageTimeout(hwnd, UnsafeNativeMethods.WM_GETOBJECT, IntPtr.Zero, (IntPtr)OBJID_QUERYCLASSNAMEIDX);
            int index = (int)result;
            if ( index >= QUERYCLASSNAME_BASE && index - QUERYCLASSNAME_BASE < _classNames.Length )
            {
                return _classNames[index - QUERYCLASSNAME_BASE].ToLower( CultureInfo.InvariantCulture );
            }
            else
            {
                return RealGetWindowClass( hwnd ).ToLower( CultureInfo.InvariantCulture );
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields
        private enum ProxyScoping
        {
            ExactMatchApparentClassName,
            ExactMatchRealClassName,
            PartialMatchApparentClassName,
            PartialMatchRealClassName,
            ImageOnlyHandlers,
            FallbackHandlers,
        }
        
        private static object _lockObj = new object();

        // contains ClientSideProviderDescription structs or an Arraylist of ClientSideProviderDescription structs
        private static Hashtable _classHandlers = new Hashtable(22, 1.0f);
        private static ArrayList _partialClassHandlers  = new ArrayList(12);

        // contains a ClientSideProviderFactoryCallback delagate or an Arraylist of delagates 
        private static Hashtable _imageOnlyHandlers = new Hashtable(0,1.0f);

        // contains ClientSideProviderFactoryCallback delagates 
        private static ArrayList _fallbackHandlers = new ArrayList(1);
        
        private static bool _defaultProxiesNeeded = true;

        // The name of the default proxy assembly this will probably change before we ship
        private const string _defaultProxyAssembly = "UIAutomationClientsideProviders";
        
        // used to plug in the non-client area and user32 focused menu proxy
        private static ClientSideProviderFactoryCallback[] _pseudoProxies = new ClientSideProviderFactoryCallback[(int)PseudoProxy.LAST];

        private enum PseudoProxy
        {
            NonClient = 0,
            NonClientMenuBar,
            NonClientSysMenu,
            User32FocusedMenu,
            LAST
        }

        // Pseudo-proxy names - must be all lowercase, since we convert
        // to lowercase in proxyinfo ctor
        private static string [ ] _pseudoProxyClassNames =
        {
            "#nonclient",
            "#nonclientmenubar",
            "#nonclientsysmenu",
            "#user32focusedmenu"
        };


        private static string [ ] _classNames = 
        {
            "ListBox",
            "#32768",
            "Button",
            "Static",
            "Edit",
            "ComboBox",
            "#32770",
            "#32771",
            "MDIClient",
            "#32769",
            "ScrollBar",
            "msctls_statusbar32",
            "ToolbarWindow32",
            "msctls_progress32",
            "SysAnimate32",
            "SysTabControl32",
            "msctls_hotkey32",
            "SysHeader32",
            "msctls_trackbar32",
            "SysListView32",
            "OpenListView",
            "msctls_updown",
            "msctls_updown32",
            "tooltips_class",
            "tooltips_class32",
            "SysTreeView32",
            "SysMonthCal32",
            "SysDateTimePick32",
            "RICHEDIT",
            "RichEdit20A",
            "RichEdit20W",
            "SysIPAddress32"
        };


        #endregion Private Fields
    }
}
