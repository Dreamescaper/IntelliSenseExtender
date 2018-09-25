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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid("718335A4-0E73-4834-A43E-E64DECE87EAB")]
    [ProvideOptionPage(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionsPage),
        "IntelliSense Extender", "General", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class IntelliSenseExtenderPackage : AsyncPackage
    {
        /// <summary>
        /// IntelliSenseExtenderPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7c7bcaff-b1be-476c-95bf-ed52ac6a6bee";

        public static OptionsPage OptionsPage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelliSenseExtenderPackage"/> class.
        /// </summary>
        public IntelliSenseExtenderPackage()
        {
        }

        protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            OptionsPage = (OptionsPage)GetDialogPage(typeof(OptionsPage));
        }
    }
}
