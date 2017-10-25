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
        }

        public OptionsPage GetOptionsPage()
        {
            return (OptionsPage)GetDialogPage(typeof(OptionsPage));
        }

        protected override void Initialize()
        {
            VsSettingsOptionsProvider.GetOptionsPageFunc = GetOptionsPage;
            base.Initialize();
        }
    }
}
