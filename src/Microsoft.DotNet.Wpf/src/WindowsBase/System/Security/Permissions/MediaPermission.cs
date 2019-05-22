// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              The MediaPermission controls the ability to create rich Media in Avalon. 
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
    ///     Enum of audio permission levels. 
    ///</summary> 
    public enum MediaPermissionAudio
    {
        /// <summary>
        ///     NoAudio - no sound allowed to play. 
        /// </summary>
        NoAudio,

        /// <summary>
        ///     SiteOfOriginAudio - only allow audio from site of origin. 
        /// </summary>
        SiteOfOriginAudio, 
        
        /// <summary>
        ///     SafeAudio - allowed to play audio with some restrictions. 
        /// </summary>
        SafeAudio,
        
        /// <summary>
        ///     Allowed to play audio with no restrictions
        /// </summary>
        AllAudio
}

    ///<summary> 
    ///     Enum of video permission levels. 
    ///</summary> 
    public enum MediaPermissionVideo
    {
        /// <summary>
        ///     NoVideo - no video allowed to play. 
        /// </summary>
        NoVideo,

        /// <summary>
        ///     SiteOfOriginVideo - only allow video from site of origin. 
        /// </summary>
        SiteOfOriginVideo, 
        
        /// <summary>
        ///     SafeVideo - allowed to play video with some restrictions. 
        /// </summary>
        SafeVideo,
        
        /// <summary>
        ///     allowed to play video with no restrictions
        /// </summary>
        AllVideo,
}

    ///<summary> 
    ///     Enum of image permission levels. 
    ///</summary> 
    public enum MediaPermissionImage
    {
        /// <summary>
        ///     NoImage - no images allowed to display
        /// </summary>
        NoImage,

        /// <summary>
        ///     SiteOfOriginImage -only allow image from site of origin. 
        /// </summary>
        SiteOfOriginImage, 
        
        /// <summary>
        ///     SafeImage - allowed to display images with some restrictions. 
        ///                 Only certified codecs allowed. 
        /// </summary>
        SafeImage,
        
        /// <summary>
        ///     Allowed to display images with no restrictions. 
        /// </summary>
        AllImage,
}    
    
    ///<summary> 
    ///              The MediaPermission controls the ability for richMedia to work in partial trust. 
    ///
    ///              There are 3 enum values that control the type of media that can work. 
    ///
    ///              MediaPermissionAudio - controls the level of audio support. 
    ///              MediaPermissionVideo - controls the level of video supported. 
    ///              MeidaPermissionImage - controls the level of image display supported. 
    ///</summary>     
    [Serializable()]
    sealed public class MediaPermission : CodeAccessPermission, IUnrestrictedPermission 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary> 
        public MediaPermission() 
        {
            InitDefaults();         
        }

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary>         
        public MediaPermission(PermissionState state) 
        {
            if (state == PermissionState.Unrestricted) 
            {
                _mediaPermissionAudio = MediaPermissionAudio.AllAudio; 
                _mediaPermissionVideo = MediaPermissionVideo.AllVideo; 
                _mediaPermissionImage = MediaPermissionImage.AllImage;                             
            }
            else if (state == PermissionState.None) 
            {
                _mediaPermissionAudio = MediaPermissionAudio.NoAudio; 
                _mediaPermissionVideo = MediaPermissionVideo.NoVideo; 
                _mediaPermissionImage = MediaPermissionImage.NoImage;                             
            }
            else 
            {
                throw new ArgumentException( SR.Get(SRID.InvalidPermissionState) );
            }
        }    

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary>         
        public MediaPermission(MediaPermissionAudio permissionAudio )
        {
            VerifyMediaPermissionAudio( permissionAudio ) ; 
            InitDefaults(); 
            
            _mediaPermissionAudio = permissionAudio ; 
        }

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary>         
        public MediaPermission(MediaPermissionVideo permissionVideo )
        {
            VerifyMediaPermissionVideo( permissionVideo ) ; 
            InitDefaults(); 
            
            _mediaPermissionVideo = permissionVideo ; 
        }

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary>         
        public MediaPermission(MediaPermissionImage permissionImage )
        {
            VerifyMediaPermissionImage( permissionImage );         
            InitDefaults(); 
            
            _mediaPermissionImage = permissionImage ; 
        }

        ///<summary> 
        ///     MediaPermission ctor. 
        ///</summary>         
        public MediaPermission(MediaPermissionAudio permissionAudio, 
                                      MediaPermissionVideo permissionVideo, 
                                      MediaPermissionImage permissionImage )
        {
            VerifyMediaPermissionAudio( permissionAudio );
            VerifyMediaPermissionVideo( permissionVideo );
            VerifyMediaPermissionImage( permissionImage );
            
            _mediaPermissionAudio = permissionAudio ; 
            _mediaPermissionVideo = permissionVideo ; 
            _mediaPermissionImage = permissionImage ;             
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
            return EqualsLevel( MediaPermissionAudio.AllAudio , 
                                 MediaPermissionVideo.AllVideo, 
                                 MediaPermissionImage.AllImage ) ; 
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
                return EqualsLevel( MediaPermissionAudio.NoAudio, 
                                     MediaPermissionVideo.NoVideo, 
                                     MediaPermissionImage.NoImage ) ; 
            }

            MediaPermission operand = target as MediaPermission ;
            if ( operand != null ) 
            {
                return ( ( this._mediaPermissionAudio <= operand._mediaPermissionAudio) && 
                          ( this._mediaPermissionVideo <= operand._mediaPermissionVideo ) && 
                          ( this._mediaPermissionImage <= operand._mediaPermissionImage ) ) ;                           
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotMediaPermissionLevel));
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

            MediaPermission operand = target as MediaPermission ;
            if ( operand != null ) 
            {
                //
                // Construct a permission that is the aggregate of the 
                // least priveleged level of the 3 enums. 
                //
                MediaPermissionAudio audioIntersectLevel = _mediaPermissionAudio < operand._mediaPermissionAudio
                                                                ? _mediaPermissionAudio : operand._mediaPermissionAudio;

                MediaPermissionVideo videoIntersectLevel = _mediaPermissionVideo < operand._mediaPermissionVideo
                                                                ? _mediaPermissionVideo : operand._mediaPermissionVideo;

                MediaPermissionImage imageIntersectLevel = _mediaPermissionImage < operand._mediaPermissionImage
                                                                ? _mediaPermissionImage : operand._mediaPermissionImage ;

                if ( ( audioIntersectLevel == MediaPermissionAudio.NoAudio ) &&
                     ( videoIntersectLevel == MediaPermissionVideo.NoVideo ) && 
                     ( imageIntersectLevel == MediaPermissionImage.NoImage ) ) 
                     
                {
                    return null;
                }                    
                else
                {
                    return new MediaPermission( audioIntersectLevel, videoIntersectLevel, imageIntersectLevel ) ; 
                }             
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotMediaPermissionLevel));
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

            MediaPermission operand = target as MediaPermission ;
            if ( operand != null ) 
            {
                //
                // Construct a permission that is the aggregate of the 
                // most priveleged level of the 3 enums. 
                //
                MediaPermissionAudio audioUnionLevel = _mediaPermissionAudio > operand._mediaPermissionAudio
                                                                ? _mediaPermissionAudio : operand._mediaPermissionAudio;

                MediaPermissionVideo videoUnionLevel = _mediaPermissionVideo > operand._mediaPermissionVideo
                                                                ? _mediaPermissionVideo : operand._mediaPermissionVideo;

                MediaPermissionImage imageUnionLevel = _mediaPermissionImage > operand._mediaPermissionImage
                                                                ? _mediaPermissionImage : operand._mediaPermissionImage ;

                if ( ( audioUnionLevel == MediaPermissionAudio.NoAudio ) &&
                     ( videoUnionLevel == MediaPermissionVideo.NoVideo ) && 
                     ( imageUnionLevel == MediaPermissionImage.NoImage ) ) 
                {
                    return null;
                }                                        
                else
                {
                    return new MediaPermission( audioUnionLevel, videoUnionLevel, imageUnionLevel ) ; 
                }
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.TargetNotMediaPermissionLevel));
            }
        }        

        ///<summary> 
        ///     Copy this permission. 
        ///</summary> 
        public override IPermission Copy() 
        {
            return new MediaPermission(
                                        this._mediaPermissionAudio, 
                                        this._mediaPermissionVideo, 
                                        this._mediaPermissionImage );
        }

         ///<summary> 
        ///     Return an XML instantiation of this permisson. 
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
                securityElement.AddAttribute("Audio", _mediaPermissionAudio.ToString());
                securityElement.AddAttribute("Video", _mediaPermissionVideo.ToString());                
                securityElement.AddAttribute("Image", _mediaPermissionImage.ToString());                                
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
                _mediaPermissionAudio = MediaPermissionAudio.AllAudio ; 
                _mediaPermissionVideo = MediaPermissionVideo.AllVideo ; 
                _mediaPermissionImage = MediaPermissionImage.AllImage; 
                return;
            }

            InitDefaults(); 

            String audio = securityElement.Attribute("Audio");

            if (audio != null) 
            {
                _mediaPermissionAudio  = (MediaPermissionAudio)Enum.Parse(typeof(MediaPermissionAudio), audio );
            }
            else 
            {
                throw new ArgumentException(SR.Get(SRID.BadXml,"audio"));     // bad XML
            }

            String video = securityElement.Attribute("Video");

            if (video != null) 
            {
                _mediaPermissionVideo = (MediaPermissionVideo)Enum.Parse(typeof(MediaPermissionVideo), video );
            }
            else 
            {
                throw new ArgumentException(SR.Get(SRID.BadXml,"video"));     // bad XML
            }

            String image = securityElement.Attribute("Image");

            if (image != null) 
            {
                _mediaPermissionImage = (MediaPermissionImage)Enum.Parse(typeof(MediaPermissionImage), image );
            }
            else 
            {
                throw new ArgumentException(SR.Get(SRID.BadXml,"image"));     // bad XML
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
        ///     Current value of allowed audio permission level
        ///</summary> 
        public MediaPermissionAudio Audio
        {
            get
            {
                return _mediaPermissionAudio ; 
            }
        }

        ///<summary> 
        ///     Current value of allowed video permission level
        ///</summary> 
        public MediaPermissionVideo Video
        {
            get
            {
                return _mediaPermissionVideo ; 
            }
        }

        ///<summary> 
        ///     Current value of allowed image permission level
        ///</summary> 
        public MediaPermissionImage Image
        {
            get
            {
                return _mediaPermissionImage ; 
            }
        }
        
        #endregion Public Properties 

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static void VerifyMediaPermissionAudio(MediaPermissionAudio level) 
        {
            if (level < MediaPermissionAudio.NoAudio || level > MediaPermissionAudio.AllAudio )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPermissionLevel));
            }
        }

        internal static void VerifyMediaPermissionVideo(MediaPermissionVideo level) 
        {
            if (level < MediaPermissionVideo.NoVideo || level > MediaPermissionVideo.AllVideo )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPermissionLevel));
            }
        }

        internal static void VerifyMediaPermissionImage(MediaPermissionImage level) 
        {
            if (level < MediaPermissionImage.NoImage || level > MediaPermissionImage.AllImage )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPermissionLevel));
            }
        }


        #endregion Internal Methods
        
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void InitDefaults()
        {
            _mediaPermissionAudio = MediaPermissionAudio.SafeAudio; 
            _mediaPermissionVideo = MediaPermissionVideo.SafeVideo; 
            _mediaPermissionImage = MediaPermissionImage.SafeImage;                    
        }

        ///<summary> 
        ///     Private helper to compare the level of the 3 enum fields. 
        ///</summary> 
        private bool EqualsLevel( MediaPermissionAudio audioLevel, 
                                  MediaPermissionVideo videoLevel, 
                                  MediaPermissionImage imageLevel ) 
        {                                  
            return ( ( _mediaPermissionAudio == audioLevel ) && 
                      ( _mediaPermissionVideo == videoLevel ) && 
                      ( _mediaPermissionImage == imageLevel ) ) ;
        }
        
        #endregion Private Methods

        //
        // Private fields:
        //


        private MediaPermissionAudio _mediaPermissionAudio ; 
        private MediaPermissionVideo _mediaPermissionVideo ; 
        private MediaPermissionImage _mediaPermissionImage ; 
}


    ///<summary> 
    /// Imperative attribute to create a MediaPermission. 
    ///</summary> 
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    sealed public class MediaPermissionAttribute : CodeAccessSecurityAttribute 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary> 
        /// Imperative attribute to create a MediaPermission. 
        ///</summary>         
        public MediaPermissionAttribute(SecurityAction action) : base(action) 
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
        /// Create a MediaPermisison. 
        ///</summary> 
        public override IPermission CreatePermission() 
        {
            if (Unrestricted) 
            {
                return new MediaPermission(PermissionState.Unrestricted);
            }

            else 
            {
                return new MediaPermission( _mediaPermissionAudio, 
                                             _mediaPermissionVideo, 
                                             _mediaPermissionImage );
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
        /// Current audio level. 
        ///</summary>         
        public MediaPermissionAudio Audio
        {
            get 
            {
                return _mediaPermissionAudio ;
            }

            set 
            {
                MediaPermission.VerifyMediaPermissionAudio(value);
                _mediaPermissionAudio = value;
            }
        }

        ///<summary> 
        /// Current Video level. 
        ///</summary>         
        public MediaPermissionVideo Video
        {
            get 
            {
                return _mediaPermissionVideo ;
            }

            set 
            {
                MediaPermission.VerifyMediaPermissionVideo(value);
                _mediaPermissionVideo = value;
            }
        }        

        ///<summary> 
        /// Current Image level. 
        ///</summary>         
        public MediaPermissionImage Image
        {
            get 
            {
                return _mediaPermissionImage ;
            }

            set 
            {
                MediaPermission.VerifyMediaPermissionImage(value);
                _mediaPermissionImage = value;
            }
        }        
        
        #endregion Public Properties

        //
        // Private fields:
        //
        
        private MediaPermissionAudio _mediaPermissionAudio ; 
        private MediaPermissionVideo _mediaPermissionVideo ; 
        private MediaPermissionImage _mediaPermissionImage ; 
}
}
