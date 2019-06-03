// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              The WebBrowserPermission controls the ability to create the WebBrowsercontrol. 
//                  In avalon - this control creates the ability for frames to navigate to html. 
//
// 
//
//

using System;
using System.Security;
using System.Security.Permissions;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Windows; 
using MS.Internal.WindowsBase;

namespace System.Security.Permissions 
{
    ///<summary> 
    ///     Enum of permission levels. 
    ///</summary> 
    public enum WebBrowserPermissionLevel 
    {
        /// <summary>
        ///     WebBrowser not allowed 
        /// </summary>
        None,
        /// <summary>
        ///     Safe. Can create webbrowser with some restrictions. 
        /// </summary>
        Safe,
        /// <summary>
        ///     Unrestricted. Can create webbrowser with no restrictions. 
        /// </summary>
        Unrestricted
     }

    ///<summary> 
    ///              The WebBrowserPermission controls the ability to create the WebBrowsercontrol. 
    ///                  In avalon - this permission grants the ability for frames to navigate to html. 
    ///              The levels for this permission are : 
    ///                    None - not able to navigate frames to HTML. 
    ///                    Safe - able to navigate frames to HTML safely. This means that 
    ///                           there are several mitigations in effect. Namely.
    ///                               Popup mitigation - unable to place an avalon popup over the weboc. 
    ///                               SiteLock - the WebOC can only be navigated to site of origin. 
    ///                               Url-Action-lockdown - the security settings of the weboc are reduced. 
    ///                    Unrestricted - able to navigate the weboc with no restrictions. 
    ///</summary>     
    [Serializable()]
    sealed public class WebBrowserPermission : CodeAccessPermission, IUnrestrictedPermission 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary> 
        ///     WebBrowserPermission ctor. 
        ///</summary> 
        public WebBrowserPermission() 
        {
            _webBrowserPermissionLevel = WebBrowserPermissionLevel.Safe; 
        }

        ///<summary> 
        ///     WebBrowserPermission ctor. 
        ///</summary>         
        public WebBrowserPermission(PermissionState state) 
        {
            if (state == PermissionState.Unrestricted) 
            {
                _webBrowserPermissionLevel = WebBrowserPermissionLevel.Unrestricted;
            }
            else if (state == PermissionState.None) 
            {
                _webBrowserPermissionLevel = WebBrowserPermissionLevel.None;
            }
            else 
            {
                throw new ArgumentException( SR.Get(SRID.InvalidPermissionState) );
            }
        }    

        ///<summary> 
        ///     WebBrowserPermission ctor. 
        ///</summary> 
        public WebBrowserPermission(WebBrowserPermissionLevel webBrowserPermissionLevel) 
        {
            WebBrowserPermission.VerifyWebBrowserPermissionLevel(webBrowserPermissionLevel);
            this._webBrowserPermissionLevel = webBrowserPermissionLevel;
        }

        #endregion Constructors 
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods


        //
        // IUnrestrictedPermission implementation
        //

        ///<summary> 
        ///     Is this an unrestricted permisison ? 
        ///</summary> 
        public bool IsUnrestricted() 
        {
            return _webBrowserPermissionLevel == WebBrowserPermissionLevel.Unrestricted ;
        }

        //
        // CodeAccessPermission implementation
        //
 

        ///<summary> 
        ///     Is this a subsetOf the target ? 
        ///</summary> 
        public override bool IsSubsetOf(IPermission target) 
        {
            if (target == null) 
            {
                return _webBrowserPermissionLevel == WebBrowserPermissionLevel.None;
            }

            WebBrowserPermission operand = target as WebBrowserPermission ;
            if ( operand != null ) 
            {
                return this._webBrowserPermissionLevel <= operand._webBrowserPermissionLevel;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotWebBrowserPermissionLevel));
            }
        }

        ///<summary> 
        ///     Return the intersection with the target
        ///</summary> 
        public override IPermission Intersect(IPermission target) 
        {
            if (target == null) 
            {
                return null;
            }

            WebBrowserPermission operand = target as WebBrowserPermission ;
            if ( operand != null ) 
            {
                WebBrowserPermissionLevel intersectLevel = _webBrowserPermissionLevel < operand._webBrowserPermissionLevel
                                                                ? _webBrowserPermissionLevel : operand._webBrowserPermissionLevel;

                if (intersectLevel == WebBrowserPermissionLevel.None)
                {
                    return null;
                }                    
                else
                {
                    return new WebBrowserPermission(intersectLevel);
                }             
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotWebBrowserPermissionLevel));
            }
        }

        ///<summary> 
        ///     Return the Union with the target
        ///</summary> 
        public override IPermission Union(IPermission target) 
        {
            if (target == null) 
            {
                return this.Copy();
            }

            WebBrowserPermission operand = target as WebBrowserPermission ;
            if ( operand != null ) 
            {
                WebBrowserPermissionLevel unionLevel = _webBrowserPermissionLevel > operand._webBrowserPermissionLevel ?

                                                       _webBrowserPermissionLevel : operand._webBrowserPermissionLevel;

                if (unionLevel == WebBrowserPermissionLevel.None)
                {
                    return null;
                }                                        
                else
                {
                    return new WebBrowserPermission(unionLevel);
                }
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotWebBrowserPermissionLevel));
            }
        }        

        ///<summary> 
        ///     Copy this permission. 
        ///</summary> 
        public override IPermission Copy() 
        {
            return new WebBrowserPermission(this._webBrowserPermissionLevel);
        }

         ///<summary> 
        ///     Return an XML instantiation of this permisison. 
        ///</summary> 
        public override SecurityElement ToXml() 
        {
            SecurityElement securityElement = new SecurityElement("IPermission");
            securityElement.AddAttribute("class", this.GetType().AssemblyQualifiedName);
            securityElement.AddAttribute("version", "1");

            if (IsUnrestricted()) 
            {
                securityElement.AddAttribute("Unrestricted", Boolean.TrueString);
            }
            else 
            {
                securityElement.AddAttribute("Level", _webBrowserPermissionLevel.ToString());
            }

            return securityElement;
        }
        
        ///<summary> 
        ///     Create a permission from XML
        ///</summary> 
        public override void FromXml(SecurityElement securityElement) 
        {
            if (securityElement == null) 
            {
                throw new ArgumentNullException("securityElement");
            }


            String className = securityElement.Attribute("class");

            if (className == null || className.IndexOf(this.GetType().FullName, StringComparison.Ordinal) == -1) 
            {
                throw new ArgumentNullException("securityElement");
            }

            String unrestricted = securityElement.Attribute("Unrestricted");
            if (unrestricted != null && Boolean.Parse(unrestricted)) 
            {
                _webBrowserPermissionLevel = WebBrowserPermissionLevel.Unrestricted;
                return;
            }

            this._webBrowserPermissionLevel = WebBrowserPermissionLevel.None;

            String level = securityElement.Attribute("Level");

            if (level != null) 
            {
                _webBrowserPermissionLevel =  (WebBrowserPermissionLevel)Enum.Parse(typeof(WebBrowserPermissionLevel), level);
            }
            else 
            {
                throw new ArgumentException(SR.Get(SRID.BadXml,"level"));     // bad XML
            }
}

 
        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        ///<summary> 
        ///     Current permission level. 
        ///</summary> 
        public WebBrowserPermissionLevel Level 
        {
            get 
            {
                return _webBrowserPermissionLevel;
            }
            set 
            {
                WebBrowserPermission.VerifyWebBrowserPermissionLevel(value);
                _webBrowserPermissionLevel = value;
            }
        }

        #endregion Public Properties
  
       //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        internal static void VerifyWebBrowserPermissionLevel(WebBrowserPermissionLevel level) 
        {
            if (level < WebBrowserPermissionLevel.None || level > WebBrowserPermissionLevel.Unrestricted )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPermissionLevel));
            }
        }


        #endregion Private Methods

        //
        // Private fields:
        //

        private WebBrowserPermissionLevel _webBrowserPermissionLevel;
}


    ///<summary> 
    /// Imperative attribute to create a WebBrowserPermission. 
    ///</summary> 
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    sealed public class WebBrowserPermissionAttribute : CodeAccessSecurityAttribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary> 
        /// Imperative attribute to create a WebBrowserPermission. 
        ///</summary>         
        public WebBrowserPermissionAttribute(SecurityAction action) : base(action) 
        {
}

        #endregion Constructors
 
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        ///<summary> 
        /// Create a WebBrowserPermisison. 
        ///</summary> 
        public override IPermission CreatePermission() 
        {
            if (Unrestricted) 
            {
                return new WebBrowserPermission(PermissionState.Unrestricted);
            }

            else 
            {
                return new WebBrowserPermission(_webBrowserPermissionLevel);
            }
        }
        
        #endregion Public Methods
 
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        ///<summary> 
        /// Current permission level. 
        ///</summary>         
        public WebBrowserPermissionLevel Level 
        {
            get 
            {
                return _webBrowserPermissionLevel;
            }

            set 
            {
                WebBrowserPermission.VerifyWebBrowserPermissionLevel(value);
                _webBrowserPermissionLevel = value;
            }
}
        
        #endregion Public Properties

        //
        // Private fields:
        //

        private WebBrowserPermissionLevel _webBrowserPermissionLevel;
}
}
