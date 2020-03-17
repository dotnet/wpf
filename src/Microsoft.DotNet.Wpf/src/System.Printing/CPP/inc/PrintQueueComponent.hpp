// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTQUEUECOMPONENT_HPP__
#define __PRINTQUEUECOMPONENT_HPP__


namespace System
{
namespace Printing
{

    //
    // this is a placeholder for the base 
    // setup class that describes an assembly
    //
    __gc public class Assembly
    {
    };
    
    //
    // this is a placeholder for the base setup class 
    // that describes a collection of assemblies
    //
    __gc public class Assemblies
    {
    };

    //
    // These two type definitions should be 
    // put in a different file
    //
    __value public enum ComponentType
    {
        PrintProcessor    = 0,
        RenderDriver      = 1,
        LanguageMonitor   = 2,
        PortMonitor       = 3
    };

    __gc public struct SystemTime
    {
        public:
         
        __property
        Int16
        get_Year(
            void
            )
        {
            return year;
        }

        __property
        void
        set_Year(
            Int16 inYear
            )
        {
            year = inYear;
        }

        __property
        Int16
        get_Month(
            void
            )
        {
            return month;
        }

        __property
        void
        set_Month(
            Int16 inMonth
            )
        {
            month= inMonth;
        }

        __property
        Int16
        get_Day(
            void
            )
        {
            return day;
        }

        __property
        void
        set_Dat(
            Int16 inDay
            )
        {
            day = inDay;
        }

        __property
        Int16
        get_Hour(
            void
            )
        {
            return hour;
        }

        __property
        void
        set_Hour(
            Int16 inHour
            )
        {
            hour = inHour;
        }

        __property
        Int16
        get_Minute(
            void
            )
        {
            return minute;
        }

        __property
        void
        set_Minute(
            Int16 inMinute
            )
        {
            minute = inMinute;
        }

        __property
        Int16
        get_Millisecond(
            void
            )
        {
            return millisecond;
        }

        __property
        void
        set_Millisecond(
            Int16 inMillisecond
            )
        {
            millisecond = inMillisecond;
        }

        private:

        Int16 year;
        Int16 month;
        Int16 day;
        Int16 hour;
        Int16 minute;
        Int16 second;
        Int16 millisecond;
    };

     //
     // may be replaced by base setup definition when available
     //
     __gc public struct PackageIdentifier
    {
        public:

        __property
        SystemTime*
        get_Version(
            void
            )
        {
            return version;
        }

        __property
        void
        set_Version(
            SystemTime* inVersion
            )
        {
            version = inVersion;
        }

        __property
        String*
        get_PackageGuid(
            void
            )
        {
            return packageGuid;
        }

        __property 
        void
        set_PackageGuid(
            String*  inPackageGuid
            )
        {
            packageGuid = inPackageGuid;
        }

        private:

        SystemTime*   version; 
        String*       packageGuid;
    };

    //
    // may be replaced by base setup definition when available
    //
    __gc public struct DriverIdentifier
    {
        public:

        __property
        String*
        get_StrongName(
            void
            )
        {
            return strongName;
        }

        __property
        void
        set_StrongName(
            String* inName
            )
        {
            strongName = inName;
        }

        private:

        String* strongName;
    };

    __gc public struct ComponentIdentifier : public DriverIdentifier
    {
    };

    __gc public struct DriverDisplayNameAndIdentifier : public DriverIdentifier
    {
         String*        driverDisplayName;
    };

    //
    // This is only for consistency with the other classes - 
    // normally a collection should be a collection and not 
    // a base type
    //
    __gc public class DriverDisplayNameAndIdentifierCollection : 
    public System::Collections::CollectionBase
    {
    };

    __gc public class PackageIdCollection : 
    public System::Collections::CollectionBase
    {
    };


    __gc public __interface IPrintQueueComponent
    {
        __property
        ComponentType
        get_ComponentType(
            void
            );

        __property
    	Assemblies*
    	get_Assemblies(
    		void
    		);

        __property
        PackageIdentifier*
        get_PackageId(
            void
            );

        //
        // retrieves the manifest 
        // of this component
        //
        __property
        String*
        get_ManifestFile(
            void
            );

        //
        // "Strong name"
        //
        __property
        DriverIdentifier*
        get_ComponentIdentifier(
            void
            );

        //
        // retrieves the display name in 
        // the current thread's locale 
        // from the manifest
        //
        __property
        String*
        get_DisplayName(
            void
            );
    };
}
}


#endif
