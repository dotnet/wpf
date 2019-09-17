// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32
{
    using System;

    // Guids defined here are copied from the KnownFolders.h file, part of the Windows SDK.
    // Not all folders in that header will work for FileDialogs.  The WPF implementation limits the list to locations that have a physical backing,
    // e.g. not ControlPanel, Computer, etc.  The real file dialogs work at a higher level of abstraction (shell namespaces instead of the file system)
    //than the WPF wrapper.
    //
    // static properties in this class are guaranteed to be thread safe.
    // static properties in this class are not guaranteed to have reference equality when retrieved on multiple calls.
    public static class FileDialogCustomPlaces
    {
        // Computer is always present in the Custom Places list.
        // It's not backed by a physical location, though, so we wouldn't support it if it was specified.
        //public static FileDialogCustomPlace Computer
        //{
        //    get { return new FileDialogCustomPlace(new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7")); }
        //}

        /// <summary>The directory that serves as a common repository for application-specific data for the current roaming user.</summary>
        public static FileDialogCustomPlace RoamingApplicationData
        {
            get { return new FileDialogCustomPlace(new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D")); }
        }

        /// <summary>The directory that serves as a common repository for application-specific data that is used by the current, non-roaming user.</summary>
        public static FileDialogCustomPlace LocalApplicationData
        {
            get { return new FileDialogCustomPlace(new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091")); }
        }

        /// <summary>The directory that serves as a common repository for Internet cookies.</summary>
        public static FileDialogCustomPlace Cookies
        {
            get { return new FileDialogCustomPlace(new Guid("2B0F765D-C0E9-4171-908E-08A611B84FF6")); }
        }

        /// <summary>The user's Contacts folder.</summary>
        public static FileDialogCustomPlace Contacts
        {
            get { return new FileDialogCustomPlace(new Guid("56784854-C6CB-462b-8169-88E350ACB882")); }
        }

        /// <summary>The directory that serves as a common repository for the user's favorite items.</summary>
        public static FileDialogCustomPlace Favorites
        {
            get { return new FileDialogCustomPlace(new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD")); }
        }

        /// <summary>The directory that contains the user's program groups.</summary>
        public static FileDialogCustomPlace Programs
        {
            get { return new FileDialogCustomPlace(new Guid("A77F5D77-2E2B-44C3-A6A2-ABA601054A51")); }
        }

        /// <summary>The user's Music folder.</summary>
        public static FileDialogCustomPlace Music
        {
            get { return new FileDialogCustomPlace(new Guid("4BD8D571-6D19-48D3-BE97-422220080E43")); }
        }

        /// <summary>The user's Pictures folder.</summary>
        public static FileDialogCustomPlace Pictures
        {
            get { return new FileDialogCustomPlace(new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB")); }
        }

        /// <summary>The directory that contains the Send To menu items.</summary>
        public static FileDialogCustomPlace SendTo
        {
            get { return new FileDialogCustomPlace(new Guid("8983036C-27C0-404B-8F08-102D10DCFD74")); }
        }

        /// <summary>The directory that contains the Start menu items.</summary>
        public static FileDialogCustomPlace StartMenu
        {
            get { return new FileDialogCustomPlace(new Guid("625B53C3-AB48-4EC1-BA1F-A1EF4146FC19")); }
        }

        /// <summary>The directory that corresponds to the user's Startup program group.</summary>
        public static FileDialogCustomPlace Startup
        {
            get { return new FileDialogCustomPlace(new Guid("B97D20BB-F46A-4C97-BA10-5E3608430854")); }
        }

        /// <summary>The System directory.</summary>
        public static FileDialogCustomPlace System
        {
            get { return new FileDialogCustomPlace(new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7")); }
        }

        /// <summary>The directory that serves as a common repository for document templates.</summary>
        public static FileDialogCustomPlace Templates
        {
            get { return new FileDialogCustomPlace(new Guid("A63293E8-664E-48DB-A079-DF759E0509F7")); }
        }

        /// <summary>The directory used to physically store file objects on the desktop.</summary>
        public static FileDialogCustomPlace Desktop
        {
            get { return new FileDialogCustomPlace(new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641")); }
        }

        /// <summary>The user's Documents folder</summary>
        public static FileDialogCustomPlace Documents
        {
            get { return new FileDialogCustomPlace(new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7")); }
        }

        /// <summary>The Program files directory.</summary>
        public static FileDialogCustomPlace ProgramFiles
        {
            get { return new FileDialogCustomPlace(new Guid("905E63B6-C1BF-494E-B29C-65B732D3D21A")); }
        }

        /// <summary>The directory for components that are shared across applications</summary>
        public static FileDialogCustomPlace ProgramFilesCommon
        {
            get { return new FileDialogCustomPlace(new Guid("F7F1ED05-9F6D-47A2-AAAE-29D317C6F066")); }
        }
    }
}
