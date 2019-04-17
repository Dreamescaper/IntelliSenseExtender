using System;
using System.Runtime.InteropServices;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace IntelliSenseExtender.Options
{
    /// <summary>
    /// Needed only to check VS version.
    /// TODO: remove when 16.0 support dropped. 
    /// </summary>
    [Guid("1EAA526A-0898-11d3-B868-00C04F79F802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#pragma warning disable IDE1006 // Naming Styles
    internal interface SVsAppId
#pragma warning restore IDE1006 // Naming Styles
    {
    }

    [Guid("1EAA526A-0898-11d3-B868-00C04F79F802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsAppId
    {
        [PreserveSig]
        int SetSite(IOleServiceProvider pSP);

        [PreserveSig]
        int GetProperty(int propid, // VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] out object pvar);

        [PreserveSig]
        int SetProperty(int propid, //[in] VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] object var);

        [PreserveSig]
        int GetGuidProperty(int propid, // VSAPROPID
            out Guid guid);

        [PreserveSig]
        int SetGuidProperty(int propid, // [in] VSAPROPID
            ref Guid rguid);

        [PreserveSig]
        int Initialize();  // called after main initialization and before command executing and entering main loop
    }

    internal enum VSAPropID
    {
        VSAPROPID_ProductSemanticVersion = -8642,     // VT_BSTR. The AppId's product semantic version.
    };
}