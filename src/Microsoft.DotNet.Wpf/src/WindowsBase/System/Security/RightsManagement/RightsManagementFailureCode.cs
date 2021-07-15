// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Error Code used by Rights Management RightsManagement exceptions
//
//
//
//

using System;

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// This error code is used to communicate reasons for failure from the UseLicense.Bind call        
    /// </summary>
    public enum RightsManagementFailureCode : int
    {
        //---------------------------------    
        // Success //////////////
        //---------------------------------    

        /// <summary>
        /// Success
        /// </summary>
        Success                             =   0,

        //---------------------------------    
        // licenses //////////////
        //---------------------------------    

        /// <summary>    
        /// InvalidLicense                      
        /// </summary>    
        InvalidLicense                      =   unchecked((int)0x8004CF00),

        /// <summary>    
        /// InfoNotInLicense                  
        /// </summary>    
        InfoNotInLicense                    =   unchecked((int)0x8004CF01),

        /// <summary>    
        /// InvalidLicenseSignature      
        /// </summary>    
        InvalidLicenseSignature             =   unchecked((int)0x8004CF02),

        /// <summary>    
        /// EncryptionNotPermitted       
        /// </summary>    
        EncryptionNotPermitted              =   unchecked((int)0x8004CF04),

        /// <summary>    
        /// RightNotGranted                   
        /// </summary>    
        RightNotGranted                     =   unchecked((int)0x8004CF05),

        /// <summary>    
        /// InvalidVersion                      
        /// </summary>    
        InvalidVersion                      =   unchecked((int)0x8004CF06),

        /// <summary>    
        /// InvalidEncodingType           
        /// </summary>    
        InvalidEncodingType                 =   unchecked((int)0x8004CF07),

        /// <summary>    
        /// InvalidNumericalValue         
        /// </summary>    
        InvalidNumericalValue               =   unchecked((int)0x8004CF08),

        /// <summary>    
        /// InvalidAlgorithmType          
        /// </summary>    
        InvalidAlgorithmType                =   unchecked((int)0x8004CF09),

        //---------------------------------    
        // environments /////////
        //---------------------------------    

        /// <summary>    
        /// EnvironmentNotLoaded            
        /// </summary>    
        EnvironmentNotLoaded                =   unchecked((int)0x8004CF0A),

        /// <summary>    
        /// EnvironmentCannotLoad          
        /// </summary>    
        EnvironmentCannotLoad               =   unchecked((int)0x8004CF0B),

        /// <summary>    
        /// TooManyLoadedEnvironments 
        /// </summary>    
        TooManyLoadedEnvironments           =   unchecked((int)0x8004CF0C),

        /// <summary>    
        /// IncompatibleObjects                
        /// </summary>    
        IncompatibleObjects                 =   unchecked((int)0x8004CF0E),


        //---------------------------------    
        // libraries //////////////
        //---------------------------------    

        /// <summary>    
        /// LibraryFail                                
        /// </summary>    
        LibraryFail                         =    unchecked((int)0x8004CF0F),

        //---------------------------------    
        // miscellany ////////////
        //---------------------------------    

        /// <summary>    
        /// EnablingPrincipalFailure           
        /// </summary>    
        EnablingPrincipalFailure            =   unchecked((int)0x8004CF10),

        /// <summary>    
        /// InfoNotPresent                          
        /// </summary>    
        InfoNotPresent                      =   unchecked((int)0x8004CF11),

        /// <summary>    
        /// BadGetInfoQuery                       
        /// </summary>    
        BadGetInfoQuery                     =   unchecked((int)0x8004CF12),

        /// <summary>    
        /// KeyTypeUnsupported                
        /// </summary>    
        KeyTypeUnsupported                  =   unchecked((int)0x8004CF13),

        /// <summary>    
        /// CryptoOperationUnsupported   
        /// </summary>    
        CryptoOperationUnsupported          =   unchecked((int)0x8004CF14),

        /// <summary>    
        /// ClockRollbackDetected              
        /// </summary>    
        ClockRollbackDetected               =   unchecked((int)0x8004CF15),

        /// <summary>    
        /// QueryReportsNoResults             
        /// </summary>    
        QueryReportsNoResults               =   unchecked((int)0x8004CF16),

        /// <summary>    
        /// UnexpectedException                
        /// </summary>    
        UnexpectedException                 =   unchecked((int)0x8004CF17),

        //---------------------------------    
        // binding errors /////////
        //---------------------------------    

        /// <summary>    
        /// BindValidityTimeViolated         
        /// </summary>    
        BindValidityTimeViolated            =   unchecked((int)0x8004CF18),

        /// <summary>    
        /// BrokenCertChain                   
        /// </summary>    
        BrokenCertChain                     =   unchecked((int)0x8004CF19),

        /// <summary>    
        /// BindPolicyViolation               
        /// </summary>    
        BindPolicyViolation                 =   unchecked((int)0x8004CF1B),

        /// <summary>    
        /// ManifestPolicyViolation           
        /// </summary>    
        ManifestPolicyViolation             =   unchecked((int)0x8004930C),

        /// <summary>    
        /// BindRevokedLicense                
        /// </summary>    
        BindRevokedLicense                  =   unchecked((int)0x8004CF1C),

        /// <summary>    
        /// BindRevokedIssuer                 
        /// </summary>    
        BindRevokedIssuer                   =   unchecked((int)0x8004CF1D),

        /// <summary>    
        /// BindRevokedPrincipal              
        /// </summary>    
        BindRevokedPrincipal                =   unchecked((int)0x8004CF1E),

        /// <summary>    
        /// BindRevokedResource               
        /// </summary>    
        BindRevokedResource                 =   unchecked((int)0x8004CF1F),

        /// <summary>    
        /// BindRevokedModule                 
        /// </summary>    
        BindRevokedModule                   =   unchecked((int)0x8004CF20),

        /// <summary>    
        /// BindContentNotInEndUseLicense             
        /// </summary>    
        BindContentNotInEndUseLicense       =   unchecked((int)0x8004CF21),

        /// <summary>    
        /// BindAccessPrincipalNotEnabling  
        /// </summary>    
        BindAccessPrincipalNotEnabling      =   unchecked((int)0x8004CF22),

        /// <summary>    
        /// BindAccessUnsatisfied             
        /// </summary>    
        BindAccessUnsatisfied               =   unchecked((int)0x8004CF23),

        /// <summary>    
        /// BindIndicatedPrincipalMissing    
        /// </summary>    
        BindIndicatedPrincipalMissing       =   unchecked((int)0x8004CF24),

        /// <summary>    
        /// BindMachineNotFoundInGroupIdentity 
        /// </summary>    
        BindMachineNotFoundInGroupIdentity  =   unchecked((int)0x8004CF25),

        /// <summary>    
        /// LibraryUnsupportedPlugIn              
        /// </summary>    
        LibraryUnsupportedPlugIn            =   unchecked((int)0x8004CF26),

        /// <summary>    
        /// BindRevocationListStale          
        /// </summary>    
        BindRevocationListStale             =   unchecked((int)0x8004CF27),

        /// <summary>    
        /// BindNoApplicableRevocationList  
        /// </summary>    
        BindNoApplicableRevocationList      =   unchecked((int)0x8004CF28),

        /// <summary>    
        /// InvalidHandle                      
        /// </summary>    
        InvalidHandle                       =   unchecked((int)0x8004CF2C),

        /// <summary>    
        /// BindIntervalTimeViolated          
        /// </summary>    
        BindIntervalTimeViolated            =   unchecked((int)0x8004CF2F),

        /// <summary>    
        /// BindNoSatisfiedRightsGroup      
        /// </summary>    
        BindNoSatisfiedRightsGroup          =   unchecked((int)0x8004CF30),

        /// <summary>    
        /// BindSpecifiedWorkMissing         
        /// </summary>    
        BindSpecifiedWorkMissing            =   unchecked((int)0x8004CF31),

        //---------------------------------    
        // client SDK error codes
        //---------------------------------    

        /// <summary>    
        /// NoMoreData                        
        /// </summary>    
        NoMoreData                          =   unchecked((int)0x8004CF33),

        /// <summary>    
        /// LicenseAcquisitionFailed            
        /// </summary>    
        LicenseAcquisitionFailed            =   unchecked((int)0x8004CF34),

        /// <summary>    
        /// IdMismatch                         
        /// </summary>    
        IdMismatch                          =   unchecked((int)0x8004CF35),

        /// <summary>    
        /// TooManyCertificates                      
        /// </summary>    
        TooManyCertificates                 =   unchecked((int)0x8004CF36),

        /// <summary>    
        /// NoDistributionPointUrlFound                      
        /// </summary>    
        NoDistributionPointUrlFound         =   unchecked((int)0x8004CF37),

        /// <summary>    
        /// AlreadyInProgress                 
        /// </summary>    
        AlreadyInProgress                   =   unchecked((int)0x8004CF38),

        /// <summary>    
        /// GroupIdentityNotSet                     
        /// </summary>    
        GroupIdentityNotSet                 =   unchecked((int)0x8004CF39),

        /// <summary>    
        /// RecordNotFound                    
        /// </summary>    
        RecordNotFound                      =   unchecked((int)0x8004CF3A),

        /// <summary>    
        /// NoConnect                          
        /// </summary>    
        NoConnect                           =   unchecked((int)0x8004CF3B),

        /// <summary>    
        /// NoLicense                          
        /// </summary>    
        NoLicense                           =   unchecked((int)0x8004CF3C),

        /// <summary>    
        /// NeedsMachineActivation            
        /// </summary>    
        NeedsMachineActivation              =   unchecked((int)0x8004CF3D),

        /// <summary>    
        /// NeedsGroupIdentityActivation      
        /// </summary>    
        NeedsGroupIdentityActivation        =   unchecked((int)0x8004CF3E),

        /// <summary>    
        /// ActivationFailed                    
        /// </summary>    
        ActivationFailed                    =   unchecked((int)0x8004CF40),

        /// <summary>    
        /// Aborted                             
        /// </summary>    
        Aborted                             =   unchecked((int)0x8004CF41),

        /// <summary>    
        /// OutOfQuota                        
        /// </summary>    
        OutOfQuota                          =   unchecked((int)0x8004CF42),

        /// <summary>    
        /// AuthenticationFailed               
        /// </summary>    
        AuthenticationFailed                =   unchecked((int)0x8004CF43),

        /// <summary>    
        /// ServerError                        
        /// </summary>    
        ServerError                         =   unchecked((int)0x8004CF44),

        /// <summary>    
        /// InstallationFailed                 
        /// </summary>    
        InstallationFailed                  =   unchecked((int)0x8004CF45),

        /// <summary>    
        /// HidCorrupted                       
        /// </summary>    
        HidCorrupted                        =   unchecked((int)0x8004CF46),

        /// <summary>    
        /// InvalidServerResponse             
        /// </summary>    
        InvalidServerResponse               =   unchecked((int)0x8004CF47),

        /// <summary>    
        /// ServiceNotFound                   
        /// </summary>    
        ServiceNotFound                     =   unchecked((int)0x8004CF48),

        /// <summary>    
        /// UseDefault                         
        /// </summary>    
        UseDefault                          =   unchecked((int)0x8004CF49),

        /// <summary>    
        /// ServerNotFound                    
        /// </summary>    
        ServerNotFound                      =   unchecked((int)0x8004CF4A),

        /// <summary>    
        /// InvalidEmail                       
        /// </summary>    
        InvalidEmail                        =   unchecked((int)0x8004CF4B),

        /// <summary>    
        /// ValidityTimeViolation              
        /// </summary>    
        ValidityTimeViolation               =   unchecked((int)0x8004CF4C),

        /// <summary>    
        /// OutdatedModule                     
        /// </summary>    
        OutdatedModule                      =   unchecked((int)0x8004CF4D),

        /// <summary>    
        /// ServiceMoved                       
        /// </summary>    
        ServiceMoved                        =   unchecked((int)0x8004CF5B),

        /// <summary>    
        /// ServiceGone                        
        /// </summary>    
        ServiceGone                         =   unchecked((int)0x8004CF5C),

        /// <summary>    
        /// AdEntryNotFound                  
        /// </summary>    
        AdEntryNotFound                     =   unchecked((int)0x8004CF5D),

        /// <summary>    
        /// NotAChain                         
        /// </summary>    
        NotAChain                           =   unchecked((int)0x8004CF5E),

        /// <summary>    
        /// RequestDenied                      
        /// </summary>    
        RequestDenied                       =   unchecked((int)0x8004CF5F),

        //---------------------------------    
        // Publishing SDK Error Codes
        //---------------------------------    

        /// <summary>    
        /// NotSet                             
        /// </summary>    
        NotSet                              =   unchecked((int)0x8004CF4E),

        /// <summary>    
        /// MetadataNotSet                    
        /// </summary>    
        MetadataNotSet                      =   unchecked((int)0x8004CF4F),

        /// <summary>    
        /// RevocationInfoNotSet              
        /// </summary>    
        RevocationInfoNotSet                =   unchecked((int)0x8004CF50),

        /// <summary>    
        /// InvalidTimeInfo                    
        /// </summary>    
        InvalidTimeInfo                     =   unchecked((int)0x8004CF51),

        /// <summary>    
        /// RightNotSet                       
        /// </summary>    
        RightNotSet                         =   unchecked((int)0x8004CF52),

        //---------------------------------    
        // NTLM Credential checking
        //---------------------------------    

        /// <summary>    
        /// LicenseBindingToWindowsIdentityFailed                                                 
        /// </summary>    
        LicenseBindingToWindowsIdentityFailed = unchecked((int)0x8004CF53),

        /// <summary>    
        /// InvalidIssuanceLicenseTemplate                  
        /// </summary>    
        InvalidIssuanceLicenseTemplate      =   unchecked((int)0x8004CF54),

        /// <summary>    
        /// InvalidKeyLength                                          
        /// </summary>    
        InvalidKeyLength                    =   unchecked((int)0x8004CF55),

        /// <summary>    
        /// ExpiredOfficialIssuanceLicenseTemplate    
        /// </summary>    
        ExpiredOfficialIssuanceLicenseTemplate    = unchecked((int)0x8004CF57),

        /// <summary>    
        /// InvalidClientLicensorCertificate                   
        /// </summary>    
        InvalidClientLicensorCertificate    =   unchecked((int)0x8004CF58),

        /// <summary>    
        /// HidInvalid                                              
        /// </summary>    
        HidInvalid                          =   unchecked((int)0x8004CF59),

        /// <summary>    
        /// EmailNotVerified                                   
        /// </summary>    
        EmailNotVerified                    =   unchecked((int)0x8004CF5A),

        /// <summary>    
        /// DebuggerDetected                                
        /// </summary>    
        DebuggerDetected                    =   unchecked((int)0x8004CF60),

        /// <summary>    
        /// InvalidLockboxType                      
        /// </summary>    
        InvalidLockboxType                  =   unchecked((int)0x8004CF70),

        /// <summary>    
        /// InvalidLockboxPath                       
        /// </summary>    
        InvalidLockboxPath                  =   unchecked((int)0x8004CF71),

        /// <summary>    
        /// InvalidRegistryPath                       
        /// </summary>    
        InvalidRegistryPath                 =   unchecked((int)0x8004CF72),

        /// <summary>    
        /// NoAesProvider                               
        /// </summary>    
        NoAesCryptoProvider                 =   unchecked((int)0x8004CF73),

        /// <summary>    
        /// GlobalOptionAlreadySet                
        /// </summary>    
        GlobalOptionAlreadySet              =   unchecked((int)0x8004CF74),

        /// <summary>    
        /// OwnerLicenseNotFound                 
        /// </summary>    
        OwnerLicenseNotFound                =   unchecked((int)0x8004CF75),
    }
}
