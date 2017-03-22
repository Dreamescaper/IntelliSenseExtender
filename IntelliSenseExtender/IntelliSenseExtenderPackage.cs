//------------------------------------------------------------------------------
// <copyright file="IntelliSenseExtenderPackage.cs" company="EPAM Systems">
//     Copyright (c) EPAM Systems.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using IntelliSenseExtender.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace IntelliSenseExtender
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid("718335A4-0E73-4834-A43E-E64DECE87EAB")]
    [ProvideOptionPage(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]

    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class IntelliSenseExtenderPackage : Package
    {
        /// <summary>
        /// IntelliSenseExtenderPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7c7bcaff-b1be-476c-95bf-ed52ac6a6bee";

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelliSenseExtenderPackage"/> class.
        /// </summary>
        public IntelliSenseExtenderPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        public OptionsPage GetOptionsPage()
        {
            return (OptionsPage)GetDialogPage(typeof(OptionsPage));
        }


        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            OptionsProvider.GetOptionsPageFunc = () => GetOptionsPage();

            base.Initialize();
        }
    }
}
