// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.AppModel
{
    internal static class IID
    {
        /// <summary>IID_IAccessible</summary>
        public const string Accessible = "618736e0-3c3d-11cf-810c-00aa00389b71";
        /// <summary>IID_IEnumIDList</summary>
        public const string EnumIdList = "000214F2-0000-0000-C000-000000000046";
        /// <summary>IID_IEnumObjects</summary>
        public const string EnumObjects = "2c1c7e2e-2d0e-4059-831e-1e6f82335c2e";
        /// <summary>IID_IFileDialog</summary>
        public const string FileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
        /// <summary>IID_IFileDialogEvents</summary>
        public const string FileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
        /// <summary>IID_IFileOpenDialog</summary>
        public const string FileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
        /// <summary>IID_IFileSaveDialog</summary>
        public const string FileSaveDialog = "84bccd23-5fde-4cdb-aea4-af64b83d78ab";
        /// <summary>IID_IModalWindow</summary>
        public const string ModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
        /// <summary>IID_IObjectArray</summary>
        public const string ObjectArray = "92CA9DCD-5622-4bba-A805-5E9F541BD8C9";
        /// <summary>IID_IObjectCollection</summary>
        public const string ObjectCollection = "5632b1a4-e38a-400a-928a-d4cd63230295";
        /// <summary>IID_IPropertyStore</summary>
        public const string PropertyStore = "886d8eeb-8cf2-4446-8d02-cdba1dbdcf99";
        /// <summary>IID_IShellFolder</summary>
        public const string ShellFolder = "000214E6-0000-0000-C000-000000000046";
        /// <summary>IID_IShellLink</summary>
        public const string ShellLink = "000214F9-0000-0000-C000-000000000046";
        /// <summary>IID_IShellLinkDataList</summary>
        public const string ShellLinkDataList = "45E2B4AE-B1C3-11D0-B92F-00A0C90312E1";
        /// <summary>IID_IShellItem</summary>
        public const string ShellItem = "43826d1e-e718-42ee-bc55-a1e261c37bfe";
        /// <summary>IID_IShellItem2</summary>
        public const string ShellItem2 = "7e9fb0d3-919f-4307-ab2e-9b1860310c93";
        /// <summary>IID_IShellItemArray</summary>
        public const string ShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
        /// <summary>IID_ITaskbarList</summary>
        public const string TaskbarList = "56FDF342-FD6D-11d0-958A-006097C9A090";
        /// <summary>IID_ITaskbarList2</summary>
        public const string TaskbarList2 = "602D4995-B13A-429b-A66E-1935E44F4317";
        /// <summary>IID_IUnknown</summary>
        public const string Unknown = "00000000-0000-0000-C000-000000000046";

        #region Windows 7 interfaces

        /// <summary>IID_IApplicationDestinations</summary>
        public const string ApplicationDestinations = "12337d35-94c6-48a0-bce7-6a9c69d4d600";
        /// <summary>IID_IApplicationDocumentLists</summary>
        public const string ApplicationDocumentLists = "3c594f9f-9f30-47a1-979a-c9e83d3d0a06";
        /// <summary>IID_ICustomDestinationList</summary>
        public const string CustomDestinationList = "6332debf-87b5-4670-90c0-5e57b408a49e";
        /// <summary>IID_IObjectWithAppUserModelID</summary>
        public const string ObjectWithAppUserModelId = "36db0196-9665-46d1-9ba7-d3709eecf9ed";
        /// <summary>IID_IObjectWithProgID</summary>
        public const string ObjectWithProgId = "71e806fb-8dee-46fc-bf8c-7748a8a1ae13";
        /// <summary>IID_ITaskbarList3</summary>
        public const string TaskbarList3 = "ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf";
        /// <summary>IID_ITaskbarList4</summary>
        public const string TaskbarList4 = "c43dc798-95d1-4bea-9030-bb99e2983a1a";

        #endregion        
    }

    internal static class CLSID
    {
        /// <summary>CLSID_EnumerableObjectCollection</summary>
        /// <remarks>IID_IEnumObjects.</remarks>
        public const string EnumerableObjectCollection = "2d3468c1-36a7-43b6-ac24-d3f02fd9607a";
        /// <summary>CLSID_FileOpenDialog</summary>
        /// <remarks>IID_IFileOpenDialog</remarks>
        public const string FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";
        /// <summary>CLSID_FileSaveDialog</summary>
        /// <remarks>IID_IFileSaveDialog</remarks>
        public const string FileSaveDialog = "C0B4E2F3-BA21-4773-8DBA-335EC946EB8B";
        /// <summary>CLSID_ShellLink</summary>
        /// <remarks>IID_IShellLink</remarks>
        public const string ShellLink = "00021401-0000-0000-C000-000000000046";
        /// <summary>CLSID_TaskbarList</summary>
        /// <remarks>IID_ITaskbarList</remarks>
        public const string TaskbarList = "56FDF344-FD6D-11d0-958A-006097C9A090";
        /// <summary>CLSID_WebBrowser</summary>
        /// <remarks>IID_IWebBrowser2</remarks>
        public const string WebBrowser = "8856f961-340a-11d0-a96b-00c04fd705a2";

        #region Windows 7 classes

        /// <summary>CLSID_DestinationList</summary>
        /// <remarks>IID_ICustomDestinationList</remarks>
        public const string DestinationList = "77f10cf0-3db5-4966-b520-b7c54fd35ed6";
        /// <summary>CLSID_ApplicationDestinations</summary>
        /// <remarks>IID_IApplicationDestinations</remarks>
        public const string ApplicationDestinations = "86c14003-4d6b-4ef3-a7b4-0506663b2e68";
        /// <summary>CLSID_ApplicationDocumentLists</summary>
        /// <remarks>IID_IApplicationDocumentLists</remarks>
        public const string ApplicationDocumentLists = "86bec222-30f2-47e0-9f25-60d11cd75c28";

        #endregion        
    }
}
