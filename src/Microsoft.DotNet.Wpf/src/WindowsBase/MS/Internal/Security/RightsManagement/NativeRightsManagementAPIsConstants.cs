// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  These are the Constant declarations for interop services required to call into unmanaged 
//  Promethium Rights Management SDK APIs 
//
//
//
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
    
namespace MS.Internal.Security.RightsManagement
{
    internal enum SecurityProviderType: uint
    {
        SoftwareSecRep = 0
    }

    internal enum SpecType : uint
    {
        Unknown = 0,
        FileName = 1
    }

    internal enum StatusMessage: uint 
    {
        ActivateMachine = 0,
        ActivateGroupIdentity = 1,
        AcquireLicense = 2,
        AcquireAdvisory = 3,
        SignIssuanceLicense = 4,
        AcquireClientLicensor = 5
    }

    [Flags]
    internal enum EnumerateLicenseFlags: uint 
    {
        Machine                 = 0x0001,
        GroupIdentity           = 0x0002,
        GroupIdentityName       = 0x0004,
        GroupIdentityLid        = 0x0008,
        SpecifiedGroupIdentity  = 0x0010,
        Eul                     = 0x0020,
        EulLid                  = 0x0040,
        ClientLicensor          = 0x0080,
        ClientLicensorLid       = 0x0100,
        SpecifiedClientLicensor = 0x0200,
        RevocationList          = 0x0400,
        RevocationListLid       = 0x0800,
        Expired                 = 0x1000,
    }

    [Flags]
    internal enum ActivationFlags: uint 
    {
        Machine              = 0x01,             // Activate machine
        GroupIdentity        = 0x02,         // Activate Group Identity
        Temporary            = 0x04,                // Temporary certificate
        Cancel               = 0x08,                 // Cancel previous request
        Silent               = 0x10,                   // Silent Activation
        SharedGroupIdentity  = 0x20,    // Shared Group Identity certificate
        Delayed              = 0x40,              // Delayed activation
    }

    [Flags]
    internal enum ServiceType: uint
   {
        Activation      = 0x01,
        Certification   = 0x02,
        Publishing      = 0x04,
        ClientLicensor  = 0x08,
    }

    internal enum ServiceLocation : uint
    {
        Internet = 0x01,
        Enterprise = 0x02,
    }

    [Flags]
    internal enum AcquireLicenseFlags: uint
    {
        NonSilent = 0x01,         // Acquire non-silently
        NoPersist =  0x02,        // Don't persist the license
        Cancel    = 0x04,             // Cancel previous request
        FetchAdvisory = 0x08,  // Don't acquire advisories
        NoUI = 0x10,                 // Don't display any Authentication UI
    }

    [Flags]
    internal enum SignIssuanceLicenseFlags: uint
    {
        Online = 0x01, 
        Offline = 0x02,
        Cancel = 0x04,
        ServerIssuanceLicense =   0x08,
        AutoGenerateKey  = 0x10,
        OwnerLicenseNoPersist =   0x20,
    }


    internal enum DistributionPointInfo 
    {
        LicenseAcquisition = 0,
        Publishing = 1,
        ReferralInfo = 2,
    }

    internal enum LicenseAttributeEncoding
    {
        Base64 = 0,
        String = 1,
        Long = 2,
        Time = 3,
        UInt = 4,
        Raw = 5
    };

    internal static class NativeConstants 
    {
        public const uint   DrmCallbackVersion = 1;
    
/////////////////////////////////////////////////////////////////////////////////
//
//The following codes are used to indicate where the various query strings may be used:
//
//for example, GI(*) means that all DRMHANDLES may be asked the indicated question using DRMGetInfo &
//             GI(hEnv) means on environment handle only
//
//GI: DRMGetInfo
//GULA: DRMGetUnboundLicenceAttribute
//GULO: DRMGetUnboundLicenseObject
//GBLA: DRMGetBoundLicenseAttribute
//GBLO: DRMGetBoundLicenseObject
//
/////////////////////////////////////////////////////////////////////////////////

        internal const string TAG_ASCII = "ASCII Tag";
        internal const string TAG_XRML = "XrML Tag";
        internal const string TAG_FILENAME = "filename";
        internal const string TAG_MSGUID = "MS-GUID";

        internal const string PLUG_STANDARDENABLINGPRINCIPAL = "UDStdPlg Enabling Principal";
        internal const string PLUG_STANDARDRIGHTSINTERPRETER = "XrMLv2a";
        internal const string PLUG_STANDARDEBDECRYPTOR = "UDStdPlg Enabling Bits Decryptor";
        internal const string PLUG_STANDARDEBENCRYPTOR = "UDStdPlg Enabling Bits Encryptor";
        internal const string PLUG_STANDARDEBCRYPTOPROVIDER = "UDStdPlg Enabling Bits Crypto Provider";
        internal const string PLUG_STANDARDLIBRARY = "UDStdPlg";

        internal const string ALGORITHMID_DES = "DES";
        internal const string ALGORITHMID_COCKTAIL = "COCKTAIL";
        internal const string ALGORITHMID_AES = "AES";
        internal const string ALGORITHMID_RC4 = "RC4";

        // QUERY CONSTANTS BELOW HERE ////////////////////////////////////////////////
        // GI(*)
        internal const string QUERY_OBJECTIDTYPE = "object-id-type";
        internal const string QUERY_OBJECTID = "object-id";

        // GBLA(on a bound right object), GULA(on a principal object, rights group, right, & work)
        internal const string QUERY_NAME = "name";

        // GBLA(on a bound license)
        internal const string QUERY_CONTENTIDTYPE = "content-id-type";
        internal const string QUERY_CONTENTIDVALUE = "content-id-value";
        internal const string QUERY_CONTENTSKUTYPE = "content-sku-type";
        internal const string QUERY_CONTENTSKUVALUE = "content-sku-value";

        // GI(hEnv)
        internal const string QUERY_MANIFESTSOURCE = "manifest-xrml";
        internal const string QUERY_MACHINECERTSOURCE = "machine-certificate-xrml";

        // GI(hEnv)
        internal const string QUERY_APIVERSION = "api-version";
        internal const string QUERY_SECREPVERSION = "secrep-version";

        // GI(hCrypto)
        internal const string QUERY_BLOCKSIZE  = "block-size";

        // GULO(on a condition list), GBLO(on a bound right)
        internal const string QUERY_ACCESSCONDITION = "access-condition";

        // GULA(on a principal)
        internal const string QUERY_ADDRESSTYPE = "address-type";
        internal const string QUERY_ADDRESSVALUE = "address-value";

        internal const string QUERY_APPDATANAME = "appdata-name";
        internal const string QUERY_APPDATAVALUE = "appdata-value";

        // GULA(on a license, a work, and rights group, or a right)
        internal const string QUERY_CONDITIONLIST = "condition-list";

        // GULO(on a license or revocation condition)
        internal const string QUERY_DISTRIBUTIONPOINT = "distribution-point";

        internal const string QUERY_OBJECTTYPE = "object-type";

        // GBLA(on a bound license)
        internal const string QUERY_ENABLINGPRINCIPALIDTYPE = "enabling-principal-id-type";
        internal const string QUERY_ENABLINGPRINCIPALIDVALUE = "enabling-principal-id-value";

        // GULO(on a license)
        internal const string QUERY_GROUPIDENTITYPRINCIPAL = "group-identity-principal";

        // GULO(on an interval time condition)
        internal const string QUERY_FIRSTUSETAG = "first-use-tag";

        // GULA(on a range time condition)
        internal const string QUERY_FROMTIME = "from-time";

        // GULA(on a license, principal, or work)
        internal const string QUERY_IDTYPE = "id-type";
        internal const string QUERY_IDVALUE = "id-value";

        // GULO(on a license)
        internal const string QUERY_ISSUEDPRINCIPAL = "issued-principal";

        // GULA(on a license)
        internal const string QUERY_ISSUEDTIME = "issued-time";

        // GULO(on a license)
        internal const string QUERY_ISSUER = "issuer";

        // GULO(on a work)
        internal const string QUERY_OWNER = "owner";

        // GULO(on an access condition)
        internal const string QUERY_PRINCIPAL = "principal";

        // GI(hEnablingPrincipal)
        internal const string QUERY_PRINCIPALIDVALUE = "principal-id-value";
        internal const string QUERY_PRINCIPALIDTYPE = "principal-id-type";

        // GULO GBLO (on a condition list)   
        internal const string QUERY_RANGETIMECONDITION = "rangetime-condition";
        internal const string QUERY_OSEXCLUSIONCONDITION = "os-exclusion-condition";

        // GULA
        internal const string QUERY_INTERVALTIMECONDITION = "intervaltime-condition";
        internal const string QUERY_INTERVALTIMEINTERVAL = "intervaltime-interval";
        internal const string QUERY_MAXVERSION = "max-version";
        internal const string QUERY_MINVERSION = "min-version";

        // GULA(on a revocation condition)
        internal const string QUERY_REFRESHPERIOD = "refresh-period";

        // GULO(on a condition list)
        internal const string QUERY_REVOCATIONCONDITION = "revocation-condition";

        // GULO(on a rights group), GBLO(on a bound license)
        internal const string QUERY_RIGHT = "right";

        // GULO(on a work)
        internal const string QUERY_RIGHTSGROUP = "rights-group";

        // GULA(on a right), GBLA(on a bound right)
        internal const string QUERY_RIGHTSPARAMETERNAME = "rights-parameter-name";
        internal const string QUERY_RIGHTSPARAMETERVALUE = "rights-parameter-value";

        // GULA(on a work)
        internal const string QUERY_SKUTYPE = "sku-type";
        internal const string QUERY_SKUVALUE = "sku-value";

        // GULA(on an interval time or metered time condition)
        internal const string QUERY_TIMEINTERVAL = "time-interval";

        // GULA(on a range time condition)
        internal const string QUERY_UNTILTIME = "until-time";

        // GULA(on a license)
        internal const string QUERY_VALIDITYFROMTIME = "valid-from";
        internal const string QUERY_VALIDITYUNTILTIME = "valid-until";

        // GULO(on a license)
        internal const string QUERY_WORK = "work";
    }
}
